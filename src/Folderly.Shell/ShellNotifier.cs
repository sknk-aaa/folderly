using System.Runtime.InteropServices;
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
        var parentPath = Directory.GetParent(folderPath)?.FullName;
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        var iconPath = Path.Combine(folderPath, ".folderly", "cover.ico");

        NotifyPath(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(desktopIniPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEDIR);
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEITEM);
            NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEDIR);
        }

        NotifyPidl(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(desktopIniPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEDIR);
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEITEM);
            NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEDIR);
        }

        // SHGetFileInfo でシステムイメージリストを強制更新 → 特定インデックスへ UPDATEIMAGE
        // UPDATEDIR だけでは古いキャッシュが残るケースに対応するための直接的なアプローチ
        ForceIconIndexUpdate(folderPath);

        // 自己リネームトリック: Explorer にフォルダのメタデータ（desktop.ini含む）を強制再読み込みさせる
        NotifyRenameFolderToSelf(folderPath);

        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_ASSOCCHANGED,
            NativeMethods.SHCNF_IDLIST | NativeMethods.SHCNF_FLUSH,
            nint.Zero,
            nint.Zero);
    }

    private static void ForceIconIndexUpdate(string path)
    {
        if (!Directory.Exists(path)) return;
        var shfi = new NativeMethods.SHFILEINFOW();
        var result = NativeMethods.SHGetFileInfo(
            path, 0, ref shfi,
            (uint)Marshal.SizeOf<NativeMethods.SHFILEINFOW>(),
            NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SYSICONINDEX);
        if (result == nint.Zero) return;
        if (shfi.hIcon != nint.Zero)
            NativeMethods.DestroyIcon(shfi.hIcon);
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEIMAGE,
            NativeMethods.SHCNF_DWORD | NativeMethods.SHCNF_FLUSH,
            nint.Zero,
            (nint)shfi.iIcon);
    }

    private static unsafe void NotifyPath(string path, uint eventId)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return;

        fixed (char* pathPtr = path)
        {
            NativeMethods.SHChangeNotify(
                eventId,
                NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSH,
                (nint)pathPtr,
                nint.Zero);
        }
    }

    private static unsafe void NotifyRenameFolderToSelf(string path)
    {
        if (!Directory.Exists(path)) return;
        // 異なるオブジェクト（別アドレス）で同一内容の文字列を生成し、
        // p1 != p2 を保証したうえで "自分自身へのリネーム" を通知する
        var pathCopy = new string(path.AsSpan());
        fixed (char* p1 = path)
        fixed (char* p2 = pathCopy)
        {
            NativeMethods.SHChangeNotify(
                NativeMethods.SHCNE_RENAMEFOLDER,
                NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSH,
                (nint)p1, (nint)p2);
        }
    }

    private static void NotifyPidl(string path, uint eventId)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return;

        var pidl = NativeMethods.ILCreateFromPath(path);
        if (pidl == nint.Zero)
            return;

        try
        {
            NativeMethods.SHChangeNotify(
                eventId,
                NativeMethods.SHCNF_IDLIST | NativeMethods.SHCNF_FLUSH,
                pidl,
                nint.Zero);
        }
        finally
        {
            NativeMethods.ILFree(pidl);
        }
    }
}
