using Folderly.App.Services;
using Folderly.Core.Application;
using Folderly.Core.History;
using Folderly.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Folderly.App.Infrastructure;

public static class AppServices
{
    private static readonly object WebView2EnvLock = new();
    private static Task<CoreWebView2Environment>? _webView2EnvTask;

    public static HistoryRepository   History    { get; private set; } = null!;
    public static ApplyService        Apply      { get; private set; } = null!;
    public static RevertService       Revert     { get; private set; } = null!;
    public static StoreLicenseService License    { get; private set; } = null!;
    public static LocalizationService Localize   { get; private set; } = null!;
    public static ILoggerFactory      LogFactory { get; private set; } = null!;

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

        History = new HistoryRepository(dbPath, LogFactory.CreateLogger<HistoryRepository>());

        var notifier = new ShellNotifier();
        Apply = new ApplyService(History, notifier, LogFactory.CreateLogger<ApplyService>());
        Revert = new RevertService(History, notifier, LogFactory.CreateLogger<RevertService>());

        License = new StoreLicenseService();
        Localize = LocalizationService.Instance;

        var savedLang = History.GetSetting("language") ?? "system";
        Localize.SetLanguage(savedLang);
    }

    public static ILogger<T> Logger<T>() => LogFactory.CreateLogger<T>();

    public static Task<CoreWebView2Environment> GetWebView2EnvironmentAsync()
    {
        lock (WebView2EnvLock)
        {
            if (_webView2EnvTask is not null)
                return _webView2EnvTask;

            var webView2DataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Folderly",
                "WebView2");

            _webView2EnvTask = CoreWebView2Environment.CreateAsync(null, webView2DataFolder);
            return _webView2EnvTask;
        }
    }

    public static void PreloadWebView2Environment()
    {
        _ = GetWebView2EnvironmentAsync();
    }
}
