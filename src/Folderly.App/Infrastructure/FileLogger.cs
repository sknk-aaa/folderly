using Microsoft.Extensions.Logging;

namespace Folderly.App.Infrastructure;

/// <summary>
/// ローテーション付き単純ファイルロガー（SPEC F-17: 5MB × 5世代）。
/// 外部ライブラリを使わずに実装（SPEC Section 2 固定ライブラリ外のため）。
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logPath;
    private readonly long _maxBytes;
    private readonly int _maxFiles;

    public FileLoggerProvider(string logPath, long maxBytes = 5 * 1024 * 1024, int maxFiles = 5)
    {
        _logPath = logPath;
        _maxBytes = maxBytes;
        _maxFiles = maxFiles;
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(_logPath, categoryName, _maxBytes, _maxFiles);

    public void Dispose() { }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _path;
    private readonly string _category;
    private readonly long _maxBytes;
    private readonly int _maxFiles;
    private static readonly object _lock = new();

    public FileLogger(string path, string category, long maxBytes, int maxFiles)
    {
        _path = path;
        _category = category;
        _maxBytes = maxBytes;
        _maxFiles = maxFiles;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;

    public void Log<TState>(LogLevel level, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(level)) return;

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-11}] {_category}: {formatter(state, exception)}";
        if (exception != null)
            line += Environment.NewLine + exception;

        lock (_lock)
        {
            try
            {
                Rotate();
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch { /* ログ失敗はサイレントに無視 */ }
        }
    }

    private void Rotate()
    {
        if (!File.Exists(_path)) return;
        if (new FileInfo(_path).Length < _maxBytes) return;

        for (int i = _maxFiles - 1; i >= 1; i--)
        {
            var src  = $"{_path}.{i}";
            var dest = $"{_path}.{i + 1}";
            if (File.Exists(src))
                File.Move(src, dest, overwrite: true);
        }
        File.Move(_path, $"{_path}.1", overwrite: true);
    }
}
