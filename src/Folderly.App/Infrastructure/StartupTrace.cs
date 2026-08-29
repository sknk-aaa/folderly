using System.Diagnostics;
using System.IO;

namespace Folderly.App.Infrastructure;

internal static class StartupTrace
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Folderly",
        "logs",
        "startup-timing.log");

    public static void Log(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(
                    LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} pid={Environment.ProcessId} {message}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }

    public static string Elapsed(Stopwatch stopwatch) => $"{stopwatch.Elapsed.TotalMilliseconds:0}ms";

    public static string ElapsedSince(Stopwatch stopwatch, ref TimeSpan previous)
    {
        var current = stopwatch.Elapsed;
        var elapsed = current - previous;
        previous = current;
        return $"{elapsed.TotalMilliseconds:0}ms";
    }
}
