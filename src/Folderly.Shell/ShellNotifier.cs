using Folderly.Core.Shell;

namespace Folderly.Shell;

/// <summary>
/// SHChangeNotify を使ってシェルのアイコンキャッシュを更新する。
/// </summary>
public sealed class ShellNotifier : IShellNotifier
{
    public void NotifyFolderChanged(string folderPath)
    {
        // グローバルアイコンキャッシュを更新
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEIMAGE,
            NativeMethods.SHCNF_FLUSH,
            nint.Zero,
            nint.Zero);

        // 特定フォルダへの変更通知
        unsafe
        {
            fixed (char* pathPtr = folderPath)
            {
                NativeMethods.SHChangeNotify(
                    NativeMethods.SHCNE_UPDATEDIR,
                    NativeMethods.SHCNF_PATH | NativeMethods.SHCNF_FLUSH,
                    (nint)pathPtr,
                    nint.Zero);
            }
        }
    }
}
