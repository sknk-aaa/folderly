using Folderly.App.Infrastructure;
using Folderly.App.Views;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Folderly.App;

public partial class App : Application
{
    private const string MutexName = "Folderly_SingleInstance_v1";
    private const string PipeName  = "FolderlyIPC_v1";
    private static readonly TimeSpan IdleShutdownDelay = TimeSpan.FromMinutes(5);

    private Mutex?       _mutex;
    private bool         _ownsMutex;
    private MainWindow?  _mainWindow;
    private DispatcherTimer? _idleShutdownTimer;
    private int _applyWindowCount;
    private bool _licenseInitializationQueued;

    protected override void OnStartup(StartupEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"App.OnStartup begin args=[{string.Join(", ", e.Args)}]");
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        StartupTrace.Log($"App.OnStartup base completed elapsed={StartupTrace.Elapsed(sw)}");

        // Explorer から COM サーバーモードで起動された場合は UI を表示せず COM ループに入る
        if (e.Args.Contains("--com-server", StringComparer.OrdinalIgnoreCase))
        {
            StartupTrace.Log($"App.OnStartup entering COM server elapsed={StartupTrace.Elapsed(sw)}");
            ComServer.Start(this);
            return;
        }

        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;
        StartupTrace.Log($"App.OnStartup mutex createdNew={createdNew} elapsed={StartupTrace.Elapsed(sw)}");

        if (!createdNew)
        {
            // 既存インスタンスにフォルダパスを送信して終了
            var folderArg = e.Args.Length > 0 ? e.Args[0] : string.Empty;
            StartupTrace.Log($"App.OnStartup forwarding to existing instance hasFolderArg={!string.IsNullOrWhiteSpace(folderArg)} elapsed={StartupTrace.Elapsed(sw)}");
            SendToExistingInstance(folderArg);
            StartupTrace.Log($"App.OnStartup forwarded to existing instance elapsed={StartupTrace.Elapsed(sw)}");
            Shutdown();
            return;
        }

        AppServices.Initialize();
        StartupTrace.Log($"App.OnStartup services initialized elapsed={StartupTrace.Elapsed(sw)}");
        AppServices.Logger<App>().LogInformation("Folderly started. Args: [{Args}]", string.Join(", ", e.Args));

        if (e.Args.Length > 0)
        {
            // 右クリックから起動: ApplyWindow を直接開く
            StartupTrace.Log($"App.OnStartup opening apply window elapsed={StartupTrace.Elapsed(sw)}");
            OpenApplyWindow(e.Args[0]);
            QueueLicenseInitialization();
        }
        else
        {
            // スタートメニューから起動: MainWindow を表示
            StartupTrace.Log($"App.OnStartup opening main window elapsed={StartupTrace.Elapsed(sw)}");
            _mainWindow = EnsureMainWindow();
            _mainWindow.Show();
        }

        // 2番目のインスタンスからのパイプ受信を開始
        StartPipeServer();
        StartupTrace.Log($"App.OnStartup completed elapsed={StartupTrace.Elapsed(sw)}");
    }

    private void QueueLicenseInitialization()
    {
        if (_licenseInitializationQueued)
            return;

        _licenseInitializationQueued = true;
        StartupTrace.Log("License initialization queued");

        _ = Dispatcher.BeginInvoke(new Action(async () =>
        {
            var sw = Stopwatch.StartNew();
            StartupTrace.Log("License initialization begin");
            try
            {
                await Task.Delay(1500);
                await AppServices.License.InitializeAsync();
                StartupTrace.Log($"License initialization completed elapsed={StartupTrace.Elapsed(sw)}");
            }
            catch (Exception ex)
            {
                StartupTrace.Log($"License initialization failed elapsed={StartupTrace.Elapsed(sw)} error={ex.Message}");
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void StartPipeServer()
    {
        StartupTrace.Log("PipeServer starting");
        Thread pipeThread = new(() =>
        {
            while (true)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    server.WaitForConnection();
                    StartupTrace.Log($"PipeServer connected elapsed={StartupTrace.Elapsed(sw)}");

                    using var reader = new System.IO.StreamReader(server);
                    var path = reader.ReadLine() ?? string.Empty;
                    StartupTrace.Log($"PipeServer received hasPath={!string.IsNullOrWhiteSpace(path)} elapsed={StartupTrace.Elapsed(sw)}");

                    Dispatcher.Invoke(() =>
                    {
                        StopIdleShutdownTimer();

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            OpenApplyWindow(path);
                        }
                        else
                        {
                            var mainWindow = EnsureMainWindow();
                            mainWindow.Show();
                            mainWindow.Activate();
                        }
                    });
                    StartupTrace.Log($"PipeServer dispatched elapsed={StartupTrace.Elapsed(sw)}");
                }
                catch
                {
                    StartupTrace.Log($"PipeServer stopped elapsed={StartupTrace.Elapsed(sw)}");
                    break;
                }
            }
        })
        { IsBackground = true, Name = "FolderlyPipeServer" };

        pipeThread.Start();
        StartupTrace.Log("PipeServer thread started");
    }

    private static void SendToExistingInstance(string folderPath)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 1000);
            using var writer = new System.IO.StreamWriter(client);
            writer.WriteLine(folderPath);
            StartupTrace.Log($"SendToExistingInstance success hasPath={!string.IsNullOrWhiteSpace(folderPath)} elapsed={StartupTrace.Elapsed(sw)}");
        }
        catch
        {
            StartupTrace.Log($"SendToExistingInstance failed hasPath={!string.IsNullOrWhiteSpace(folderPath)} elapsed={StartupTrace.Elapsed(sw)}");
        }
    }

    private MainWindow EnsureMainWindow()
    {
        if (_mainWindow != null) return _mainWindow;
        var sw = Stopwatch.StartNew();
        StartupTrace.Log("EnsureMainWindow begin");
        _mainWindow = new MainWindow();
        _mainWindow.Closed += (_, _) =>
        {
            _mainWindow = null;
            ScheduleIdleShutdownIfNeeded();
        };
        StartupTrace.Log($"EnsureMainWindow completed elapsed={StartupTrace.Elapsed(sw)}");
        return _mainWindow;
    }

    private void OpenApplyWindow(string folderPath)
    {
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"OpenApplyWindow begin path={folderPath}");
        StopIdleShutdownTimer();
        AppServices.PreloadWebView2Environment();
        StartupTrace.Log($"OpenApplyWindow requested WebView2 preload elapsed={StartupTrace.Elapsed(sw)}");
        _applyWindowCount++;

        var win = new ApplyWindow(folderPath);
        StartupTrace.Log($"OpenApplyWindow constructed ApplyWindow elapsed={StartupTrace.Elapsed(sw)}");
        if (_mainWindow?.IsVisible == true)
        {
            win.Owner = _mainWindow;
            win.Closed += (_, _) => _mainWindow?.RefreshHistory();
        }

        win.Closed += (_, _) =>
        {
            if (_applyWindowCount > 0)
                _applyWindowCount--;
            ScheduleIdleShutdownIfNeeded();
        };

        win.Show();
        StartupTrace.Log($"OpenApplyWindow shown elapsed={StartupTrace.Elapsed(sw)}");
        BringToFront(win);
        StartupTrace.Log($"OpenApplyWindow completed elapsed={StartupTrace.Elapsed(sw)}");
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
        window.Dispatcher.BeginInvoke(() =>
        {
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }, DispatcherPriority.ApplicationIdle);
    }

    private void ScheduleIdleShutdownIfNeeded()
    {
        if (_applyWindowCount > 0) return;
        if (_mainWindow?.IsVisible == true) return;

        _idleShutdownTimer ??= new DispatcherTimer
        {
            Interval = IdleShutdownDelay,
        };
        _idleShutdownTimer.Tick -= IdleShutdownTimer_Tick;
        _idleShutdownTimer.Tick += IdleShutdownTimer_Tick;
        _idleShutdownTimer.Stop();
        _idleShutdownTimer.Start();
        StartupTrace.Log($"IdleShutdown scheduled delay={IdleShutdownDelay}");
    }

    private void StopIdleShutdownTimer()
    {
        _idleShutdownTimer?.Stop();
    }

    private void IdleShutdownTimer_Tick(object? sender, EventArgs e)
    {
        StopIdleShutdownTimer();
        StartupTrace.Log("IdleShutdown elapsed; shutting down");
        Shutdown();
    }
}
