using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Folderly.ContextMenu;

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    [PreserveSig] int GetParent(out IShellItem ppsi);
    [PreserveSig] int GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    [PreserveSig] int Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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

[ComVisible(true)]
[Guid("A08CE4D0-FA25-44AB-B57C-C7240467C5B0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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
            Process.Start(new ProcessStartInfo(GetFolderlyExePath(), $"\"{path}\"")
            {
                UseShellExecute = false
            });
        }
        catch
        {
        }

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

    private static string GetFolderlyExePath()
    {
        var packageRootExe = Path.Combine(AppContext.BaseDirectory, "Folderly.exe");
        if (File.Exists(packageRootExe))
            return packageRootExe;

        var handlerDir = Path.GetDirectoryName(typeof(FolderlyContextMenuHandler).Assembly.Location);
        var colocatedExe = handlerDir is null ? null : Path.Combine(handlerDir, "Folderly.exe");
        return colocatedExe is not null && File.Exists(colocatedExe)
            ? colocatedExe
            : "Folderly.exe";
    }
}
