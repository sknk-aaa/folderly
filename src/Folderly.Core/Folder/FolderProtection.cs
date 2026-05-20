namespace Folderly.Core.Folder;

public enum ProtectionLevel { Allowed, Warning, Denied }

public record ProtectionResult(ProtectionLevel Level, string? Reason)
{
    public bool IsAllowed => Level == ProtectionLevel.Allowed;
    public bool IsWarning => Level == ProtectionLevel.Warning;
    public bool IsDenied  => Level == ProtectionLevel.Denied;
}

/// <summary>
/// フォルダへの適用可否を判定する（SPEC.md Section 8.3 準拠）。
/// </summary>
public static class FolderProtection
{
    private static readonly Lazy<string[]> DeniedSystemRoots = new(() =>
    {
        var roots = new List<string>();
        AddIfNotEmpty(roots, Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        AddIfNotEmpty(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddIfNotEmpty(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        AddIfNotEmpty(roots, Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));
        return roots.ToArray();

        static void AddIfNotEmpty(List<string> list, string path)
        {
            if (!string.IsNullOrEmpty(path))
                list.Add(path);
        }
    });

    public static ProtectionResult CheckPath(string path)
    {
        // UNC パスは Path.GetFullPath 前にチェック（Linux では変形されるため）
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
            return Warning("ネットワークパスです");

        var normalized = Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // ① ドライブルート（C:\, D:\ 等）
        var root = Path.GetPathRoot(normalized);
        if (root is not null && string.Equals(
                normalized, root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            return Denied("ドライブルートには適用できません");

        // ② システムフォルダ配下（Windows, Program Files 等）
        foreach (var sysRoot in DeniedSystemRoots.Value)
        {
            if (IsSubPathOf(normalized, sysRoot))
                return Denied($"システムフォルダ配下のため適用できません");
        }

        // ③ ユーザープロファイルルート直下（直下のみ拒否、サブフォルダは可）
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile) && IsDirectChildOf(normalized, userProfile))
            return Denied("ユーザープロファイルのルートフォルダ直下には適用できません");

        // ④ 書き込み権限なし
        if (!HasWriteAccess(normalized))
            return Denied("書き込み権限がありません");

        // ⑤ 警告: OneDrive 配下
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        var oneDriveCommercial = Environment.GetEnvironmentVariable("OneDriveCommercial");
        if ((!string.IsNullOrEmpty(oneDrive) && IsSubPathOf(normalized, oneDrive)) ||
            (!string.IsNullOrEmpty(oneDriveCommercial) && IsSubPathOf(normalized, oneDriveCommercial)))
            return Warning("OneDrive フォルダです。変更は他のデバイスにも同期される可能性があります");

        // ⑥ 警告: Dropbox 配下（パス文字列判定、保守的実装）
        if (normalized.Contains("Dropbox", StringComparison.OrdinalIgnoreCase))
            return Warning("Dropbox フォルダです");

        // ⑦ 警告: 260 文字超
        if (normalized.Length > 260)
            return Warning("パスが 260 文字を超えています");

        return new ProtectionResult(ProtectionLevel.Allowed, null);
    }

    private static bool IsSubPathOf(string path, string root)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectChildOf(string path, string parent)
    {
        var parentNorm = Path.GetFullPath(parent)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var pathParent = Path.GetDirectoryName(path);
        return pathParent is not null &&
               string.Equals(pathParent, parentNorm, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasWriteAccess(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".folderly_write_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ProtectionResult Denied(string reason)  => new(ProtectionLevel.Denied,  reason);
    private static ProtectionResult Warning(string reason) => new(ProtectionLevel.Warning, reason);
}
