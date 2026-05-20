using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;

namespace Folderly.App;

// ——————————————————————————————————————————————————————
// COM インターフェース定義（Explorer 提供、呼び出し側）
// ——————————————————————————————————————————————————————

[ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    [PreserveSig] int GetParent(out IShellItem ppsi);
    [PreserveSig] int GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    [PreserveSig] int Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid rbhid, ref Guid riid, out IntPtr ppvOut);
    [PreserveSig] int GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
    [PreserveSig] int GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
    [PreserveSig] int GetAttributes(uint dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
    [PreserveSig] int GetCount(out uint pdwNumItems);
    [PreserveSig] int GetItemAt(uint dwIndex, out IShellItem ppsi);
    [PreserveSig] int EnumItems(out IntPtr ppenumShellItems);
}

// ——————————————————————————————————————————————————————
// COM インターフェース定義（Folderly 実装側）
// ——————————————————————————————————————————————————————

[ComVisible(true)]
[Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    [PreserveSig] int GetTitle(IShellItemArray? psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    [PreserveSig] int GetIcon(IShellItemArray? psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszIcon);
    [PreserveSig] int GetToolTip(IShellItemArray? psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszInfotip);
    [PreserveSig] int GetCanonicalName(out Guid pguidCommandName);
    [PreserveSig] int GetState(IShellItemArray? psiItemArray, bool fOkToBeSlow, out uint pCmdState);
    [PreserveSig] int Invoke(IShellItemArray? psiItemArray, IntPtr pbc);
    [PreserveSig] int GetFlags(out uint pFlags);
    [PreserveSig] int EnumSubCommands(out IntPtr ppEnum);
}

[ComVisible(true)]
[Guid("00000001-0000-0000-c000-000000000046"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IClassFactory
{
    [PreserveSig] int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
    [PreserveSig] int LockServer(bool fLock);
}

// ——————————————————————————————————————————————————————
// IExplorerCommand 実装
// ——————————————————————————————————————————————————————

[ComVisible(true)]
[Guid("2A7A05DA-70D8-4302-8B23-AE8D79D801B6")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class FolderlyContextMenuHandler : IExplorerCommand
{
    private static readonly Guid CommandGuid = new("2A7A05DA-70D8-4302-8B23-AE8D79D801B6");
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private const int S_OK = 0;
    private const uint SIGDN_FILESYSPATH = 0x80058000u;

    public int GetTitle(IShellItemArray? _, out string ppszName)
    {
        ppszName = "Folderly でカスタマイズ";
        return S_OK;
    }

    public int GetIcon(IShellItemArray? _, out string ppszIcon)
    {
        ppszIcon = string.Empty;
        return E_NOTIMPL;
    }

    public int GetToolTip(IShellItemArray? _, out string ppszInfotip)
    {
        ppszInfotip = string.Empty;
        return E_NOTIMPL;
    }

    public int GetCanonicalName(out Guid pguidCommandName)
    {
        pguidCommandName = CommandGuid;
        return S_OK;
    }

    public int GetState(IShellItemArray? _, bool fOkToBeSlow, out uint pCmdState)
    {
        pCmdState = 0; // ECS_ENABLED
        return S_OK;
    }

    public int Invoke(IShellItemArray? psiItemArray, IntPtr pbc)
    {
        try
        {
            if (psiItemArray == null) return S_OK;
            psiItemArray.GetItemAt(0, out IShellItem item);
            item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
            ComServer.HandleFolder(path);
        }
        catch { }
        return S_OK;
    }

    public int GetFlags(out uint pFlags)
    {
        pFlags = 0;
        return S_OK;
    }

    public int EnumSubCommands(out IntPtr ppEnum)
    {
        ppEnum = IntPtr.Zero;
        return E_NOTIMPL;
    }
}

// ——————————————————————————————————————————————————————
// IClassFactory 実装
// ——————————————————————————————————————————————————————

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class FolderlyClassFactory : IClassFactory
{
    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    private const int S_OK = 0;

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;
        if (pUnkOuter != IntPtr.Zero)
            return CLASS_E_NOAGGREGATION;
        var handler = new FolderlyContextMenuHandler();
        ppvObject = Marshal.GetComInterfaceForObject(handler, typeof(IExplorerCommand));
        return S_OK;
    }

    public int LockServer(bool fLock) => S_OK;
}

// ——————————————————————————————————————————————————————
// COM サーバー起動・停止
// ——————————————————————————————————————————————————————

internal static class ComServer
{
    private const string PipeName = "FolderlyIPC_v1";

    private static uint _cookie;
    private static Application? _app;
    private static System.Timers.Timer? _timeoutTimer;

    public static void Start(Application app)
    {
        _app = app;
        var clsid = new Guid("2A7A05DA-70D8-4302-8B23-AE8D79D801B6");
        var factory = new FolderlyClassFactory();
        int hr = CoRegisterClassObject(
            ref clsid, factory,
            4  /* CLSCTX_LOCAL_SERVER */,
            1  /* REGCLS_MULTIPLEUSE */,
            out _cookie);
        Marshal.ThrowExceptionForHR(hr);

        _timeoutTimer = new System.Timers.Timer(30_000) { AutoReset = false };
        _timeoutTimer.Elapsed += (_, _) => Stop();
        _timeoutTimer.Start();
    }

    public static void HandleFolder(string folderPath)
    {
        _timeoutTimer?.Stop();
        if (!TrySendViaPipe(folderPath))
            Process.Start(new ProcessStartInfo(GetFolderlyExePath(), $"\"{folderPath}\"")
                { UseShellExecute = false });
        Stop();
    }

    private static string GetFolderlyExePath()
    {
        var packagedExe = Path.Combine(AppContext.BaseDirectory, "Folderly.exe");
        return File.Exists(packagedExe) ? packagedExe : Environment.ProcessPath!;
    }

    public static void Stop() =>
        _app?.Dispatcher.Invoke(() =>
        {
            CoRevokeClassObject(_cookie);
            _app.Shutdown();
        });

    private static bool TrySendViaPipe(string folderPath)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 500);
            using var writer = new System.IO.StreamWriter(client);
            writer.WriteLine(folderPath);
            return true;
        }
        catch { return false; }
    }

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        ref Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        int dwClsContext,
        int flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}
