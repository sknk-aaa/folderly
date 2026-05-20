using Folderly.App.Infrastructure;
using Folderly.App.Views;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace Folderly.App;

public partial class App : Application
{
    private const string MutexName = "Folderly_SingleInstance_v1";
    private const string PipeName  = "FolderlyIPC_v1";

    private Mutex?       _mutex;
    private MainWindow?  _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
            _mainWindow = new MainWindow();
            _mainWindow.Show();
            _mainWindow.OpenApplyWindow(e.Args[0]);
        }
        else
        {
            // スタートメニューから起動: MainWindow を表示
            _mainWindow = new MainWindow();
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
                        _mainWindow?.Activate();
                        if (!string.IsNullOrWhiteSpace(path))
                            _mainWindow?.OpenApplyWindow(path);
                        else
                        {
                            _mainWindow?.Show();
                            _mainWindow?.Activate();
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
}
