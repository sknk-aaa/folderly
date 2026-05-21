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
        // フォルダの LastWriteTime を現在時刻に更新してサムネイルキャッシュを無効化する。
        // 中身があるフォルダは content-preview サムネイルがキャッシュされているため、
        // タイムスタンプを変えることでキャッシュミスを起こし desktop.ini アイコンを再読みさせる。
        TryTouchFolder(folderPath);

        // グローバルアイコンキャッシュを更新
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEIMAGE,
            NativeMethods.SHCNF_FLUSH,
            nint.Zero,
            nint.Zero);

        // 特定フォルダへの変更通知
        var parentPath = Directory.GetParent(folderPath)?.FullName;
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        // ICO 本体のパスは実行時のハッシュで決まる。存在するファイルだけ通知する
        var folderlyDir = Path.Combine(folderPath, "_folderly");
        var iconPath = Directory.Exists(folderlyDir)
            ? Directory.EnumerateFiles(folderlyDir, "cover_*.ico").FirstOrDefault()
              ?? Path.Combine(folderlyDir, "cover.ico")
            : Path.Combine(folderlyDir, "cover.ico");

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

        // 1秒後に2回目の通知を送る。
        // Explorer はコンテンツサムネイルモード→カスタムアイコンモードの切り替えを非同期で行うため、
        // 1回目の通知でモード切替を開始させ、切替完了後に再通知することで初回適用も即時反映させる。
        ScheduleDelayedNotify(folderPath);
    }

    private static void ScheduleDelayedNotify(string folderPath)
    {
        // 350ms・900ms・1800ms の3ラウンドで通知する。
        // Explorer のコンテンツサムネイル→カスタムアイコンモード切替は非同期で、
        // 切替完了タイミングが環境により異なるため複数回通知することで確実に捕捉する。
        foreach (var delayMs in new[] { 350, 900, 1800 })
        {
            var d = delayMs;
            var t = new Thread(() =>
            {
                Thread.Sleep(d);
                RunDelayedNotify(folderPath);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }
    }

    private static void RunDelayedNotify(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        TryTouchFolder(folderPath);
        ForceIconIndexUpdate(folderPath);

        // PATH と PIDL 両方で通知（Explorer の受け取り方が実装依存のため）
        NotifyPath(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEDIR);
        NotifyPidl(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEDIR);

        var parentPath = Directory.GetParent(folderPath)?.FullName;
        if (string.IsNullOrWhiteSpace(parentPath)) return;
        NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEDIR);
        NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEDIR);
    }

    private static void TryTouchFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        try { Directory.SetLastWriteTimeUtc(folderPath, DateTime.UtcNow); }
        catch { }
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
