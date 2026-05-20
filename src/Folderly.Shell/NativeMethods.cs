using System.Runtime.InteropServices;

namespace Folderly.Shell;

internal static partial class NativeMethods
{
    internal const uint SHCNE_UPDATEIMAGE = 0x00008000;
    internal const uint SHCNE_UPDATEDIR   = 0x00001000;
    internal const uint SHCNF_FLUSH       = 0x1000;
    internal const uint SHCNF_PATH        = 0x0001;

    [LibraryImport("shell32.dll")]
    internal static partial void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        nint dwItem1,
        nint dwItem2);
}
