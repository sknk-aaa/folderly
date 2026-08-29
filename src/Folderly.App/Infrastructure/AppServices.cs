using Folderly.App.Services;
using Folderly.Core.Application;
using Folderly.Core.History;
using Folderly.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

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
        var sw = Stopwatch.StartNew();
        StartupTrace.Log("AppServices.Initialize begin");
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseDir = Path.Combine(appData, "Folderly");
        Directory.CreateDirectory(baseDir);
        StartupTrace.Log($"AppServices.Initialize base directory ready elapsed={StartupTrace.Elapsed(sw)}");

        var logPath = Path.Combine(baseDir, "logs", "folderly.log");

        LogFactory = LoggerFactory.Create(b => b
            .SetMinimumLevel(LogLevel.Information)
            .AddProvider(new FileLoggerProvider(logPath)));
        StartupTrace.Log($"AppServices.Initialize logger ready elapsed={StartupTrace.Elapsed(sw)}");

        var dbPath = Path.Combine(baseDir, "folderly.db");

        History = new HistoryRepository(dbPath, LogFactory.CreateLogger<HistoryRepository>());
        StartupTrace.Log($"AppServices.Initialize history ready elapsed={StartupTrace.Elapsed(sw)}");

        var notifier = new ShellNotifier();
        Apply = new ApplyService(History, notifier, LogFactory.CreateLogger<ApplyService>());
        Revert = new RevertService(History, notifier, LogFactory.CreateLogger<RevertService>());
        StartupTrace.Log($"AppServices.Initialize core services ready elapsed={StartupTrace.Elapsed(sw)}");

        License = new StoreLicenseService();
        Localize = LocalizationService.Instance;

        var savedLang = History.GetSetting("language") ?? "system";
        Localize.SetLanguage(savedLang);
        StartupTrace.Log($"AppServices.Initialize completed language={Localize.CurrentLang} elapsed={StartupTrace.Elapsed(sw)}");
    }

    public static ILogger<T> Logger<T>() => LogFactory.CreateLogger<T>();

    public static Task<CoreWebView2Environment> GetWebView2EnvironmentAsync()
    {
        lock (WebView2EnvLock)
        {
            if (_webView2EnvTask is not null)
            {
                StartupTrace.Log("WebView2 environment task reused");
                return _webView2EnvTask;
            }

            var webView2DataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Folderly",
                "WebView2");

            var sw = Stopwatch.StartNew();
            StartupTrace.Log($"WebView2 environment creation started userDataFolder={webView2DataFolder}");
            _webView2EnvTask = CoreWebView2Environment.CreateAsync(null, webView2DataFolder);
            _webView2EnvTask = _webView2EnvTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    StartupTrace.Log($"WebView2 environment creation failed elapsed={StartupTrace.Elapsed(sw)} error={t.Exception?.GetBaseException().Message}");
                else
                    StartupTrace.Log($"WebView2 environment creation completed elapsed={StartupTrace.Elapsed(sw)}");

                return t.GetAwaiter().GetResult();
            }, TaskScheduler.Default);
            return _webView2EnvTask;
        }
    }

    public static void PreloadWebView2Environment()
    {
        StartupTrace.Log("WebView2 environment preload requested");
        _ = GetWebView2EnvironmentAsync();
    }
}
