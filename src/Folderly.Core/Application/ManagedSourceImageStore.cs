using Folderly.Core.History;
using Microsoft.Extensions.Logging;

namespace Folderly.Core.Application;

internal static class ManagedSourceImageStore
{
    public const string DirectoryName = "source-images";

    public static void TryDeleteIfUnreferenced(
        string? sourceImagePath,
        IEnumerable<HistoryEntry> historyEntries,
        ILogger logger)
    {
        if (!IsManagedPath(sourceImagePath))
            return;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(sourceImagePath!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to normalize managed source image path {Path}", sourceImagePath);
            return;
        }

        if (!File.Exists(fullPath))
            return;

        var isReferenced = historyEntries.Any(entry =>
            IsSamePath(entry.SourceImagePath, fullPath));
        if (isReferenced)
            return;

        try
        {
            File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete unreferenced managed source image {Path}", fullPath);
        }
    }

    private static bool IsManagedPath(string? sourceImagePath)
    {
        if (string.IsNullOrWhiteSpace(sourceImagePath))
            return false;

        var parent = Path.GetDirectoryName(sourceImagePath);
        return string.Equals(
            Path.GetFileName(parent),
            DirectoryName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSamePath(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return false;

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                right,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
