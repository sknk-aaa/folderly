using System.Security.Cryptography;
using System.Text;
using Folderly.Core.Composition;
using Folderly.Core.Conversion;
using Folderly.Core.Folder;
using Folderly.Core.History;
using Folderly.Core.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;

namespace Folderly.Core.Application;

public class FolderProtectionException(string reason)
    : InvalidOperationException(reason);

public record ApplyRequest(
    string FolderPath,
    Stream SourceImageStream,
    string SourceImagePath,
    ImageAdjustParams AdjustParams,
    TagColor TagColor,
    bool ForceApply = false,
    string? TagName = null,
    bool ShowTagNameOnIcon = false);

public record ApplyResult(bool IsSuccess, bool IsWarning, string? Message, string? IconPath)
{
    public static ApplyResult Success(string iconPath) =>
        new(true,  false, null,    iconPath);
    public static ApplyResult Warning(string reason) =>
        new(false, true,  reason, null);
}

/// <summary>
/// フォルダアイコン適用のユースケース。
/// 合成→変換→属性設定→ini 書き込み→Shell 通知→履歴保存 を一括実行する。
/// </summary>
public sealed class ApplyService
{
    private readonly HistoryRepository _history;
    private readonly IShellNotifier _shellNotifier;
    private readonly ILogger<ApplyService> _logger;

    public ApplyService(
        HistoryRepository historyRepository,
        IShellNotifier shellNotifier,
        ILogger<ApplyService>? logger = null)
    {
        _history = historyRepository;
        _shellNotifier = shellNotifier;
        _logger = logger ?? NullLogger<ApplyService>.Instance;
    }

    public async Task<ApplyResult> ApplyAsync(
        ApplyRequest request,
        CancellationToken ct = default)
    {
        var folderPath = Path.GetFullPath(request.FolderPath);

        // 1. 保護チェック
        var protection = FolderProtection.CheckPath(folderPath);
        if (protection.IsDenied)
            throw new FolderProtectionException(protection.Reason!);
        if (protection.IsWarning && !request.ForceApply)
            return ApplyResult.Warning(protection.Reason!);

        _logger.LogInformation("Applying to {FolderPath}", folderPath);

        // 2. 既存 desktop.ini と属性のバックアップ
        var existingIniContent = DesktopIniManager.Read(folderPath);
        int originalFolderAttrs = (int)FolderAttributesService.GetAttributes(folderPath);
        int? originalIniAttrs = null;
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (existingIniContent is not null && File.Exists(iniPath))
            originalIniAttrs = (int)FolderAttributesService.GetAttributes(iniPath);

        // 3. 画像調整
        request.SourceImageStream.Position = 0;
        using var sourceImage = await Image.LoadAsync(request.SourceImageStream, ct);
        var imageRegionSize = new SixLabors.ImageSharp.Size(
            (int)FolderTemplate.ImageRegion.Width,
            (int)FolderTemplate.ImageRegion.Height);
        using var adjustedImage = ImageAdjuster.Adjust(
            sourceImage, imageRegionSize, request.AdjustParams);

        // 4. テンプレート合成
        var tagNameForIcon = request.ShowTagNameOnIcon ? request.TagName : null;
        using var composed = TemplateRenderer.Render(
            adjustedImage, request.TagColor, FolderTemplate.BaseSize, tagNameForIcon);

        // 5. ICO 変換
        var icoBytes = IcoConverter.Convert(composed);
        var iconHash = ComputeHash(icoBytes);

        // 6. ICO ファイル保存（パスをハッシュ由来でユニークにし、Explorer アイコンキャッシュ無効化）
        var iconFileName = $"cover_{iconHash[..8]}.ico";
        var (centralIcoPath, _) = await SaveIcoFilesAsync(
            folderPath, iconHash, iconFileName, icoBytes, ct);

        // 7. _folderly ディレクトリ作成（SaveIcoFilesAsync 内で作成済だが念のため）
        var folderlyDir = Path.Combine(folderPath, FolderlyConstants.FolderlyDirectoryName);
        Directory.CreateDirectory(folderlyDir);

        // 8. desktop.ini 書き込み
        // Explorer は OneDrive 配下の ICO をすぐ再読込しないことがあるため、
        // IconResource は常にローカル AppData に保存した ICO を指す。
        DesktopIniManager.Write(
            folderPath,
            centralIcoPath,
            existingIniContent);

        // 9. ファイル属性設定
        FolderAttributesService.ApplyFolderAttributes(folderPath);
        FolderAttributesService.ApplyDesktopIniAttributes(iniPath);
        FolderAttributesService.ApplyHiddenFolderlyAttributes(folderlyDir);

        // 10. Shell 通知
        _shellNotifier.NotifyFolderChanged(folderPath);

        // 11. 履歴保存
        var mode = request.AdjustParams.Mode == CropMode.Center ? "center" : "fit_width";
        var entry = new HistoryEntry(
            Id:                         null,
            FolderPath:                 folderPath,
            OriginalAttributes:         originalFolderAttrs,
            HadDesktopIni:              existingIniContent is not null,
            OriginalDesktopIniContent:  existingIniContent is not null
                                            ? Encoding.Unicode.GetBytes(existingIniContent) : null,
            OriginalDesktopIniAttrs:    originalIniAttrs,
            SourceImagePath:            request.SourceImagePath,
            IconHash:                   iconHash,
            IconStoragePath:            centralIcoPath,
            CropMode:                   mode,
            ImageScale:                 request.AdjustParams.Scale,
            ImageOffsetX:               request.AdjustParams.OffsetX,
            ImageOffsetY:               request.AdjustParams.OffsetY,
            TagColor:                   request.TagColor.HexColor,
            AppliedAt:                  DateTime.UtcNow,
            TagKey:                     request.TagColor.IsNone ? null : request.TagColor.Key,
            TagName:                    request.TagColor.IsNone ? null : request.TagName,
            TagLabelVisible:            !request.TagColor.IsNone && request.ShowTagNameOnIcon);
        _history.Upsert(entry);

        _logger.LogInformation("Applied successfully to {FolderPath}", folderPath);
        return ApplyResult.Success(centralIcoPath);
    }

    private static string ComputeHash(byte[] icoBytes)
        => Convert.ToHexString(SHA256.HashData(icoBytes)).ToLowerInvariant();

    private static async Task<(string central, string local)> SaveIcoFilesAsync(
        string folderPath, string iconHash, string localFileName, byte[] icoBytes, CancellationToken ct)
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var iconsDir = Path.Combine(localAppData, "Folderly", "icons");
        Directory.CreateDirectory(iconsDir);

        var centralPath = Path.Combine(iconsDir, $"{iconHash}.ico");
        if (!File.Exists(centralPath))
            await File.WriteAllBytesAsync(centralPath, icoBytes, ct);

        var folderlyDir = Path.Combine(folderPath, FolderlyConstants.FolderlyDirectoryName);
        Directory.CreateDirectory(folderlyDir);

        // 旧バージョンが残した .folderly ディレクトリも片付ける（OneDrive で消える前提だが
        // 非 OneDrive パスで残っているケースもあるため）
        var legacyDir = Path.Combine(folderPath, FolderlyConstants.LegacyFolderlyDirectoryName);
        if (Directory.Exists(legacyDir))
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(legacyDir, "*", SearchOption.AllDirectories))
                    File.SetAttributes(f, FileAttributes.Normal);
                Directory.Delete(legacyDir, recursive: true);
            }
            catch { /* 掃除失敗は致命ではない */ }
        }

        var localPath = Path.Combine(folderlyDir, localFileName);
        await File.WriteAllBytesAsync(localPath, icoBytes, ct);

        return (centralPath, localPath);
    }
}
