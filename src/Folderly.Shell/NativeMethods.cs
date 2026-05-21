using System.Runtime.InteropServices;

namespace Folderly.Shell;

internal static partial class NativeMethods
{
    internal const uint SHCNE_MKDIR        = 0x00000008;
    internal const uint SHCNE_RMDIR        = 0x00000010;
    internal const uint SHCNE_UPDATEIMAGE  = 0x00008000;
    internal const uint SHCNE_UPDATEITEM   = 0x00002000;
    internal const uint SHCNE_UPDATEDIR    = 0x00001000;
    internal const uint SHCNE_ATTRIBUTES   = 0x00000800;
    internal const uint SHCNE_RENAMEFOLDER = 0x00020000;
    internal const uint SHCNE_ASSOCCHANGED = 0x08000000;
    internal const uint SHCNF_FLUSH       = 0x1000;
    internal const uint SHCNF_IDLIST      = 0x0000;
    internal const uint SHCNF_PATHW       = 0x0005;
    internal const uint SHCNF_DWORD       = 0x0003;

    internal const uint SHGFI_ICON         = 0x0100;
    internal const uint SHGFI_SYSICONINDEX = 0x4000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHFILEINFOW
    {
        internal nint hIcon;
        internal int  iIcon;
        internal uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        internal string szTypeName;
    }

    [LibraryImport("shell32.dll")]
    internal static partial void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        nint dwItem1,
        nint dwItem2);

    // SHGetFileInfo は ByValTStr を持つ struct を受け取るため DllImport を使用
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern nint SHGetFileInfo(
        string pszPath, uint dwFileAttributes,
        ref SHFILEINFOW psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    internal static extern bool DestroyIcon(nint hIcon);

    [LibraryImport("shell32.dll", EntryPoint = "ILCreateFromPathW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint ILCreateFromPath(string path);

    [LibraryImport("shell32.dll")]
    internal static partial void ILFree(nint pidl);
}
