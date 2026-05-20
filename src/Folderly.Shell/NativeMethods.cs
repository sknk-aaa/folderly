using System.Runtime.InteropServices;

namespace Folderly.Shell;

internal static partial class NativeMethods
{
    internal const uint SHCNE_UPDATEIMAGE = 0x00008000;
    internal const uint SHCNE_UPDATEITEM  = 0x00002000;
    internal const uint SHCNE_UPDATEDIR   = 0x00001000;
    internal const uint SHCNE_ATTRIBUTES  = 0x00000800;
    internal const uint SHCNE_RENAMEFOLDER = 0x00020000;
    internal const uint SHCNE_ASSOCCHANGED = 0x08000000;
    internal const uint SHCNF_FLUSH       = 0x1000;
    internal const uint SHCNF_IDLIST      = 0x0000;
    internal const uint SHCNF_PATHW       = 0x0005;

    [LibraryImport("shell32.dll")]
    internal static partial void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        nint dwItem1,
        nint dwItem2);

    [LibraryImport("shell32.dll", EntryPoint = "ILCreateFromPathW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint ILCreateFromPath(string path);

    [LibraryImport("shell32.dll")]
    internal static partial void ILFree(nint pidl);
}
