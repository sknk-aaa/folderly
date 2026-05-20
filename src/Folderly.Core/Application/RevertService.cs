using System.Text;
using Folderly.Core.Folder;
using Folderly.Core.History;
using Folderly.Core.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Folderly.Core.Application;

/// <summary>
/// フォルダアイコン復元のユースケース。
/// desktop.ini 復元→属性復元→.folderly 削除→Shell 通知→履歴削除 を一括実行する。
/// </summary>
public sealed class RevertService
{
    private readonly HistoryRepository _history;
    private readonly IShellNotifier _shellNotifier;
    private readonly ILogger<RevertService> _logger;

    public RevertService(
        HistoryRepository historyRepository,
        IShellNotifier shellNotifier,
        ILogger<RevertService>? logger = null)
    {
        _history = historyRepository;
        _shellNotifier = shellNotifier;
        _logger = logger ?? NullLogger<RevertService>.Instance;
    }

    public Task RevertAsync(string folderPath, CancellationToken ct = default)
    {
        var normalized = Path.GetFullPath(folderPath);

        // 1. 履歴取得
        var entry = _history.GetByPath(normalized)
            ?? throw new InvalidOperationException(
                $"履歴が見つかりません: {normalized}");

        _logger.LogInformation("Reverting {FolderPath}", normalized);

        var iniPath = Path.Combine(normalized, "desktop.ini");

        // 2. desktop.ini を元に戻す
        if (entry.HadDesktopIni && entry.OriginalDesktopIniContent is not null)
        {
            var originalContent = Encoding.Unicode.GetString(entry.OriginalDesktopIniContent);
            DesktopIniManager.WriteRaw(normalized, originalContent);
        }
        else if (!entry.HadDesktopIni)
        {
            if (File.Exists(iniPath))
                File.Delete(iniPath);
        }

        // 3. フォルダ属性を元に戻す
        FolderAttributesService.RestoreAttributes(
            normalized, (FileAttributes)entry.OriginalAttributes);

        // 4. .folderly ディレクトリを削除
        var folderlyDir = Path.Combine(normalized, ".folderly");
        if (Directory.Exists(folderlyDir))
        {
            // 隠し属性のファイルも削除できるよう属性を正規化してから削除
            foreach (var f in Directory.EnumerateFiles(folderlyDir))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(folderlyDir, recursive: true);
        }

        // 5. Shell 通知
        _shellNotifier.NotifyFolderChanged(normalized);

        // 6. 履歴削除
        _history.Delete(normalized);

        _logger.LogInformation("Reverted {FolderPath} successfully", normalized);
        return Task.CompletedTask;
    }
}
