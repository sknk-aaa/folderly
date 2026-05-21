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
                var cleanedOriginalContent = RemoveFolderlyIconKeys(originalContent);
                var originalWasFolderlyState = !string.Equals(
                    cleanedOriginalContent, originalContent, StringComparison.Ordinal);

                if (string.IsNullOrWhiteSpace(cleanedOriginalContent))
                {
                    if (File.Exists(iniPath))
                        DeleteFileWithRetry(iniPath, ct);
                }
                else
                {
                    DesktopIniManager.WriteRaw(normalized, cleanedOriginalContent);

                    if (!originalWasFolderlyState && entry.OriginalDesktopIniAttrs is int attrs)
                    {
                        try { File.SetAttributes(iniPath, (FileAttributes)attrs); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to restore desktop.ini attributes"); }
                    }
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
            var folderAttributes = (FileAttributes)entry.OriginalAttributes;
            if (entry.HadDesktopIni &&
                entry.OriginalDesktopIniContent is not null &&
                IsFolderlyManagedDesktopIni(Encoding.Unicode.GetString(entry.OriginalDesktopIniContent)))
            {
                folderAttributes &= ~FileAttributes.System;
                folderAttributes &= ~FileAttributes.ReadOnly;
            }

            FolderAttributesService.RestoreAttributes(
                normalized, folderAttributes);

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

    public async Task<RevertAllResult> RevertAllAsync(
        IEnumerable<string>? cleanupRoots = null,
        CancellationToken ct = default)
    {
        var entries = _history.GetAll().ToList();
        var failCount = 0;

        foreach (var entry in entries)
        {
            try
            {
                await RevertAsync(entry.FolderPath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revert history entry {FolderPath}", entry.FolderPath);
                try { _history.Delete(entry.FolderPath); } catch { }
                failCount++;
            }
        }

        var cleanedOrphans = CleanupOrphanedFolderlyArtifacts(
            cleanupRoots ?? GetDefaultCleanupRoots(), ct);
        return new RevertAllResult(entries.Count, cleanedOrphans, failCount);
    }

    private void TryClearAttributes(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to clear attributes on {Path}", path); }
    }

    private void TryClearFolderCustomizationAttributes(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            attrs &= ~FileAttributes.System;
            attrs &= ~FileAttributes.ReadOnly;
            File.SetAttributes(path, attrs);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to clear folder customization attributes on {Path}", path); }
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

    private static bool IsFolderlyManagedDesktopIni(string content)
        => !string.Equals(RemoveFolderlyIconKeys(content), content, StringComparison.Ordinal);

    private static string RemoveFolderlyIconKeys(string content)
    {
        var lines = content.Split('\n');
        var kept = new List<string>(lines.Length);
        var removedFolderlyIconKey = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                kept.Add(line);
                continue;
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            var isIconPathKey =
                string.Equals(key, "IconResource", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "IconFile", StringComparison.OrdinalIgnoreCase);
            if (isIconPathKey && IsFolderlyIconValue(value))
            {
                removedFolderlyIconKey = true;
                continue;
            }

            if (removedFolderlyIconKey &&
                string.Equals(key, "IconIndex", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            kept.Add(line);
        }

        if (!removedFolderlyIconKey)
            return content;

        return PruneEmptyShellClassInfo(string.Join("\r\n", kept));
    }

    private static bool IsFolderlyIconValue(string value)
    {
        var path = value;
        var comma = path.LastIndexOf(',');
        if (comma >= 0)
            path = path[..comma];

        path = path.Trim().Trim('"').Replace('/', '\\');
        return path.Contains("\\Folderly\\icons\\", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("\\_folderly\\", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("\\.folderly\\", StringComparison.OrdinalIgnoreCase) ||
               Path.GetFileName(path).StartsWith("cover_", StringComparison.OrdinalIgnoreCase);
    }

    private static string PruneEmptyShellClassInfo(string content)
    {
        var lines = content.Split(
            new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var result = new List<string>(lines.Length);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.Trim().Equals("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(line);
                continue;
            }

            var sectionLines = new List<string>();
            var j = i + 1;
            while (j < lines.Length && !lines[j].TrimStart().StartsWith('['))
            {
                sectionLines.Add(lines[j]);
                j++;
            }

            if (sectionLines.Any(l => !string.IsNullOrWhiteSpace(l)))
            {
                result.Add(line);
                result.AddRange(sectionLines);
            }

            i = j - 1;
        }

        return string.Join("\r\n", result).Trim();
    }

    private int CleanupOrphanedFolderlyArtifacts(IEnumerable<string> roots, CancellationToken ct)
    {
        var cleanedCount = 0;
        foreach (var folder in EnumerateCleanupCandidates(roots, ct))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (TryCleanupOrphanedFolderlyArtifacts(folder, ct))
                    cleanedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup orphaned Folderly artifacts in {FolderPath}", folder);
            }
        }

        return cleanedCount;
    }

    private bool TryCleanupOrphanedFolderlyArtifacts(string folderPath, CancellationToken ct)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        var hasFolderlyIni = false;
        string? cleanedIni = null;

        if (File.Exists(iniPath))
        {
            var content = DesktopIniManager.Read(folderPath);
            if (content is not null)
            {
                cleanedIni = RemoveFolderlyIconKeys(content);
                hasFolderlyIni = !string.Equals(cleanedIni, content, StringComparison.Ordinal);
            }
        }

        var folderlyDir = Path.Combine(folderPath, FolderlyConstants.FolderlyDirectoryName);
        var legacyDir = Path.Combine(folderPath, FolderlyConstants.LegacyFolderlyDirectoryName);
        var hasFolderlyDir = Directory.Exists(folderlyDir) || Directory.Exists(legacyDir);

        if (!hasFolderlyIni && !hasFolderlyDir)
            return false;

        if (hasFolderlyIni)
        {
            if (File.Exists(iniPath))
                TryClearAttributes(iniPath);

            if (string.IsNullOrWhiteSpace(cleanedIni))
            {
                if (File.Exists(iniPath))
                    DeleteFileWithRetry(iniPath, ct);
            }
            else
            {
                DesktopIniManager.WriteRaw(folderPath, cleanedIni);
            }
        }

        foreach (var dir in new[] { folderlyDir, legacyDir })
        {
            if (Directory.Exists(dir))
                DeleteFolderlyWithRetry(dir, ct);
        }

        TryClearFolderCustomizationAttributes(folderPath);
        _history.Delete(folderPath);
        _shellNotifier.NotifyFolderChanged(folderPath);
        _logger.LogInformation("Cleaned orphaned Folderly artifacts in {FolderPath}", folderPath);
        return true;
    }

    private static IEnumerable<string> EnumerateCleanupCandidates(IEnumerable<string> roots, CancellationToken ct)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;

            var stack = new Stack<string>();
            stack.Push(Path.GetFullPath(root));

            while (stack.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var current = stack.Pop();
                if (!seen.Add(current))
                    continue;

                yield return current;

                IEnumerable<string> children;
                try { children = Directory.EnumerateDirectories(current); }
                catch { continue; }

                foreach (var child in children)
                {
                    if (ShouldSkipCleanupDirectory(child))
                        continue;
                    stack.Push(child);
                }
            }
        }
    }

    private static bool ShouldSkipCleanupDirectory(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.Equals("AppData", StringComparison.OrdinalIgnoreCase) ||
               name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("packages", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetDefaultCleanupRoots()
    {
        var roots = new List<string>();
        AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddRoot(roots, Environment.GetEnvironmentVariable("OneDrive"));
        AddRoot(roots, Environment.GetEnvironmentVariable("OneDriveConsumer"));
        AddRoot(roots, Environment.GetEnvironmentVariable("OneDriveCommercial"));
        return roots;
    }

    private static void AddRoot(List<string> roots, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        var fullPath = Path.GetFullPath(path);
        if (!roots.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
            roots.Add(fullPath);
    }
}

public sealed record RevertAllResult(
    int HistoryEntryCount,
    int CleanedOrphanCount,
    int FailCount);
