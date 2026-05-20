using Folderly.Core.Application;
using Folderly.Core.History;
using Folderly.App.Services;
using Folderly.Shell;
using Microsoft.Extensions.Logging;

namespace Folderly.App.Infrastructure;

/// <summary>
/// アプリ全体で共有するサービスの静的コンテナ。
/// DI フレームワーク非使用（YAGNI: v1.0 の規模では不要）。
/// </summary>
public static class AppServices
{
    public static HistoryRepository    History     { get; private set; } = null!;
    public static ApplyService         Apply       { get; private set; } = null!;
    public static RevertService        Revert      { get; private set; } = null!;
    public static StoreLicenseService  License     { get; private set; } = null!;
    public static LocalizationService  Localize    { get; private set; } = null!;
    public static ILoggerFactory       LogFactory  { get; private set; } = null!;

    public static void Initialize()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseDir = Path.Combine(appData, "Folderly");
        Directory.CreateDirectory(baseDir);

        var logPath = Path.Combine(baseDir, "logs", "folderly.log");

        LogFactory = LoggerFactory.Create(b => b
            .SetMinimumLevel(LogLevel.Information)
            .AddProvider(new FileLoggerProvider(logPath)));

        var dbPath = Path.Combine(baseDir, "folderly.db");
        History    = new HistoryRepository(dbPath, LogFactory.CreateLogger<HistoryRepository>());

        var notifier = new ShellNotifier();
        Apply  = new ApplyService(History, notifier, LogFactory.CreateLogger<ApplyService>());
        Revert = new RevertService(History, notifier, LogFactory.CreateLogger<RevertService>());

        License  = new StoreLicenseService();
        Localize = LocalizationService.Instance;

        // 言語設定を DB から復元
        var savedLang = History.GetSetting("language") ?? "system";
        Localize.SetLanguage(savedLang);
    }

    public static ILogger<T> Logger<T>() => LogFactory.CreateLogger<T>();
}
