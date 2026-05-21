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

    // ── IThumbnailCache: サムネイルキャッシュの強制再生成に使用 ─────────────────

    internal const uint WTS_FORCEEXTRACTION = 0x00000004;

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        [PreserveSig] int BindToHandler(nint pbc, ref Guid bhid, ref Guid riid, out nint ppv);
        [PreserveSig] int GetParent(out IShellItem ppsi);
        [PreserveSig] int GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        [PreserveSig] int Compare([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WTS_THUMBNAILID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] rgbKey;
    }

    [ComImport]
    [Guid("F676C15D-596A-4ce2-8234-33996F445DB1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IThumbnailCache
    {
        [PreserveSig]
        int GetThumbnail(
            [MarshalAs(UnmanagedType.Interface)] IShellItem pShellItem,
            uint cxyRequestedThumbSize,
            uint flags,
            out nint ppvThumb,
            out uint pOutFlags,
            out WTS_THUMBNAILID pThumbnailID);
        [PreserveSig]
        int GetThumbnailByID(
            WTS_THUMBNAILID thumbnailID,
            uint cxyRequestedThumbSize,
            out nint ppvThumb,
            out uint pOutFlags);
    }

    // CLSID = {50EF4544-AC9F-4A8E-B21B-8A26180DB13F} (Local Thumbnail Cache)
    [ComImport]
    [Guid("50EF4544-AC9F-4A8E-B21B-8A26180DB13F")]
    internal class LocalThumbnailCache { }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        nint pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
}
