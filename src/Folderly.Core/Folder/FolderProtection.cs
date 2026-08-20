namespace Folderly.Core.Folder;

public enum ProtectionLevel { Allowed, Warning, Denied }

public record ProtectionResult(ProtectionLevel Level, string? Reason)
{
    public bool IsAllowed => Level == ProtectionLevel.Allowed;
    public bool IsWarning => Level == ProtectionLevel.Warning;
    public bool IsDenied  => Level == ProtectionLevel.Denied;
}

public enum FolderLocationKind
{
    Local,
    NetworkUnc,
    NetworkDrive,
}

public sealed record FolderLocationInfo(
    FolderLocationKind Kind,
    bool IsOneDrive,
    bool IsDropbox,
    bool IsLongPath)
{
    public bool IsNetwork => Kind is FolderLocationKind.NetworkUnc or FolderLocationKind.NetworkDrive;
}

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
        var location = GetLocationInfo(path);
        if (location.IsNetwork)
            return Warning("ネットワークフォルダです");

        var normalized = Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var root = Path.GetPathRoot(normalized);
        if (root is not null && string.Equals(
                normalized,
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            return Denied("ドライブのルートには適用できません");

        foreach (var sysRoot in DeniedSystemRoots.Value)
        {
            if (IsSubPathOf(normalized, sysRoot))
                return Denied("システムフォルダ配下のため適用できません");
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile) && IsDirectChildOf(normalized, userProfile))
            return Denied("ユーザープロファイル直下のフォルダには適用できません");

        if (!HasWriteAccess(normalized))
            return Denied("書き込み権限がありません");

        return new ProtectionResult(ProtectionLevel.Allowed, null);
    }

    public static FolderLocationInfo GetLocationInfo(string path)
    {
        var normalized = path;
        try
        {
            normalized = Path.GetFullPath(path).TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            normalized = path.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        var kind = FolderLocationKind.Local;
        if (path.StartsWith(@"\\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\\", StringComparison.Ordinal))
        {
            kind = FolderLocationKind.NetworkUnc;
        }
        else if (IsMappedNetworkDrive(normalized))
        {
            kind = FolderLocationKind.NetworkDrive;
        }

        return new FolderLocationInfo(
            kind,
            IsOneDrivePath(normalized),
            normalized.Contains("Dropbox", StringComparison.OrdinalIgnoreCase),
            normalized.Length > 260);
    }

    public static bool IsNetworkPath(string path)
        => GetLocationInfo(path).IsNetwork;

    public static bool IsOneDrivePath(string path)
    {
        var normalized = Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return IsSameOrSubPathOf(normalized, Environment.GetEnvironmentVariable("OneDrive")) ||
               IsSameOrSubPathOf(normalized, Environment.GetEnvironmentVariable("OneDriveConsumer")) ||
               IsSameOrSubPathOf(normalized, Environment.GetEnvironmentVariable("OneDriveCommercial"));
    }

    private static bool IsSameOrSubPathOf(string path, string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(path, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
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

    private static bool IsMappedNetworkDrive(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root))
                return false;

            return new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }

    private static ProtectionResult Denied(string reason)  => new(ProtectionLevel.Denied,  reason);
    private static ProtectionResult Warning(string reason) => new(ProtectionLevel.Warning, reason);
}
