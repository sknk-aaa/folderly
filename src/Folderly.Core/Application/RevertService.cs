using System.Text;
using Folderly.Core.Folder;
using Folderly.Core.History;
using Folderly.Core.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Folderly.Core.Application;

/// <summary>
/// フォルダアイコン復元のユースケース。
/// desktop.ini 復元→属性復元→_folderly 削除→Shell 通知→履歴削除 を一括実行する。
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

        try
        {
            var iniPath = Path.Combine(normalized, "desktop.ini");

            // フォルダ自体の System | ReadOnly を一旦外す（desktop.ini の書換/削除を阻まないため）
            TryClearAttributes(normalized);

            // 2. desktop.ini を元に戻す
            if (entry.HadDesktopIni && entry.OriginalDesktopIniContent is not null)
            {
                if (File.Exists(iniPath))
                    TryClearAttributes(iniPath);

                var originalContent = Encoding.Unicode.GetString(entry.OriginalDesktopIniContent);
                DesktopIniManager.WriteRaw(normalized, originalContent);

                if (entry.OriginalDesktopIniAttrs is int attrs)
                {
                    try { File.SetAttributes(iniPath, (FileAttributes)attrs); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to restore desktop.ini attributes"); }
                }
            }
            else if (!entry.HadDesktopIni)
            {
                if (File.Exists(iniPath))
                {
                    TryClearAttributes(iniPath);
                    DeleteFileWithRetry(iniPath, ct);
                }
            }

            // 3. フォルダ属性を元に戻す
            FolderAttributesService.RestoreAttributes(
                normalized, (FileAttributes)entry.OriginalAttributes);

            // 4. _folderly ディレクトリを削除（旧 .folderly が残っていれば合わせて掃除）
            foreach (var dirName in new[] {
                FolderlyConstants.FolderlyDirectoryName,
                FolderlyConstants.LegacyFolderlyDirectoryName })
            {
                var folderlyDir = Path.Combine(normalized, dirName);
                if (Directory.Exists(folderlyDir))
                {
                    DeleteFolderlyWithRetry(folderlyDir, ct);
                }
            }

            // 5. Shell 通知
            _shellNotifier.NotifyFolderChanged(normalized);

            // 6. 履歴削除
            _history.Delete(normalized);

            _logger.LogInformation("Reverted {FolderPath} successfully", normalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revert failed for {FolderPath}", normalized);
            throw;
        }

        return Task.CompletedTask;
    }

    private void TryClearAttributes(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to clear attributes on {Path}", path); }
    }

    private void DeleteFileWithRetry(string path, CancellationToken ct)
    {
        const int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (i < maxAttempts - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
            catch (UnauthorizedAccessException) when (i < maxAttempts - 1)
            {
                TryClearAttributes(path);
                Thread.Sleep(100 * (i + 1));
            }
        }
    }

    private void DeleteFolderlyWithRetry(string folderlyDir, CancellationToken ct)
    {
        // 隠し / 読み取り専用属性のファイル・サブディレクトリも削除できるよう属性を正規化
        foreach (var f in Directory.EnumerateFiles(folderlyDir, "*", SearchOption.AllDirectories))
            TryClearAttributes(f);
        foreach (var d in Directory.EnumerateDirectories(folderlyDir, "*", SearchOption.AllDirectories))
            TryClearAttributes(d);
        TryClearAttributes(folderlyDir);

        const int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                Directory.Delete(folderlyDir, recursive: true);
                return;
            }
            catch (IOException) when (i < maxAttempts - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
            catch (UnauthorizedAccessException) when (i < maxAttempts - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
        }
    }
}
