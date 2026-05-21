using Folderly.App.Infrastructure;
using Folderly.App.Views;
using Microsoft.Extensions.Logging;
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
    private MainWindow?  _mainWindow;
    private DispatcherTimer? _idleShutdownTimer;
    private int _applyWindowCount;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Explorer から COM サーバーモードで起動された場合は UI を表示せず COM ループに入る
        if (e.Args.Contains("--com-server", StringComparer.OrdinalIgnoreCase))
        {
            ComServer.Start(this);
            return;
        }

        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // 既存インスタンスにフォルダパスを送信して終了
            var folderArg = e.Args.Length > 0 ? e.Args[0] : string.Empty;
            SendToExistingInstance(folderArg);
            Shutdown();
            return;
        }

        AppServices.Initialize();
        AppServices.Logger<App>().LogInformation("Folderly started. Args: [{Args}]", string.Join(", ", e.Args));

        if (e.Args.Length > 0)
        {
            // 右クリックから起動: ApplyWindow を直接開く
            OpenApplyWindow(e.Args[0]);
        }
        else
        {
            // スタートメニューから起動: MainWindow を表示
            _mainWindow = EnsureMainWindow();
            _mainWindow.Show();
        }

        // 2番目のインスタンスからのパイプ受信を開始
        StartPipeServer();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void StartPipeServer()
    {
        Thread pipeThread = new(() =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    server.WaitForConnection();

                    using var reader = new System.IO.StreamReader(server);
                    var path = reader.ReadLine() ?? string.Empty;

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
                }
                catch
                {
                    break;
                }
            }
        })
        { IsBackground = true, Name = "FolderlyPipeServer" };

        pipeThread.Start();
    }

    private static void SendToExistingInstance(string folderPath)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 1000);
            using var writer = new System.IO.StreamWriter(client);
            writer.WriteLine(folderPath);
        }
        catch { /* 既存インスタンスが応答しない場合は無視 */ }
    }

    private MainWindow EnsureMainWindow()
    {
        if (_mainWindow != null) return _mainWindow;
        _mainWindow = new MainWindow();
        _mainWindow.Closed += (_, _) =>
        {
            _mainWindow = null;
            ScheduleIdleShutdownIfNeeded();
        };
        return _mainWindow;
    }

    private void OpenApplyWindow(string folderPath)
    {
        StopIdleShutdownTimer();
        _applyWindowCount++;

        var win = new ApplyWindow(folderPath);
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
        win.Activate();
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
    }

    private void StopIdleShutdownTimer()
    {
        _idleShutdownTimer?.Stop();
    }

    private void IdleShutdownTimer_Tick(object? sender, EventArgs e)
    {
        StopIdleShutdownTimer();
        Shutdown();
    }
}
