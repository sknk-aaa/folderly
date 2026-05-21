using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Folderly.Core.Shell;

namespace Folderly.Shell;

/// <summary>
/// SHChangeNotify を使ってシェルのアイコンキャッシュを更新する。
/// </summary>
public sealed class ShellNotifier : IShellNotifier
{
    public void NotifyFolderChanged(string folderPath)
    {
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
        var iconPath = ResolveCurrentIconPath(folderPath);

        NotifyCurrentIcon(iconPath);
        NotifyPath(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(desktopIniPath, NativeMethods.SHCNE_UPDATEITEM);
        if (iconPath is not null)
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
        if (iconPath is not null)
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

        // Shell.Application 経由で開いている Explorer ウィンドウを直接 Refresh する。
        // SHChangeNotify は非同期処理キューに積むだけだが、Document.Refresh() は
        // 対象ウィンドウの再描画を即座に強制するため、開いているウィンドウがあれば即時反映できる。
        RefreshExplorerWindows(folderPath);

        // 遅延通知: ウィンドウが開いていない場合や Refresh 後もキャッシュが残る場合の保険。
        // 各ラウンドで System+ReadOnly を toggle して Explorer に desktop.ini 再読みを強制する。
        ScheduleDelayedNotify(folderPath);
    }

    private static void RefreshExplorerWindows(string folderPath)
    {
        var parentPath = Directory.GetParent(folderPath)?.FullName;
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return;
            var shell = Activator.CreateInstance(shellType);
            var windows = shellType.InvokeMember("Windows", BindingFlags.InvokeMethod, null, shell, null);
            if (windows == null) return;

            var countObj = windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null);
            var count = countObj is int c ? c : 0;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    var win = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    if (win == null) continue;
                    var locationUrl = win.GetType().InvokeMember("LocationURL", BindingFlags.GetProperty, null, win, null) as string;
                    if (string.IsNullOrWhiteSpace(locationUrl)) continue;
                    var locationPath = Uri.TryCreate(locationUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : null;
                    if (locationPath == null) continue;

                    // 親フォルダまたはフォルダ自身を表示しているウィンドウを対象にする
                    if (!string.Equals(locationPath, parentPath, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(locationPath, folderPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var doc = win.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, win, null);
                    doc?.GetType().InvokeMember("Refresh", BindingFlags.InvokeMethod, null, doc, null);
                }
                catch { }
            }
        }
        catch { }
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
        var iconPath = ResolveCurrentIconPath(folderPath);
        NotifyCurrentIcon(iconPath);
        ForceIconIndexUpdate(folderPath);

        // PATH と PIDL 両方で通知（Explorer の受け取り方が実装依存のため）
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        NotifyPath(desktopIniPath, NativeMethods.SHCNE_UPDATEITEM);
        if (iconPath is not null)
            NotifyPath(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(folderPath, NativeMethods.SHCNE_UPDATEDIR);
        NotifyPidl(desktopIniPath, NativeMethods.SHCNE_UPDATEITEM);
        if (iconPath is not null)
            NotifyPidl(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(folderPath, NativeMethods.SHCNE_ATTRIBUTES);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(folderPath, NativeMethods.SHCNE_UPDATEDIR);

        var parentPath = Directory.GetParent(folderPath)?.FullName;
        if (string.IsNullOrWhiteSpace(parentPath)) return;
        NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPath(parentPath, NativeMethods.SHCNE_UPDATEDIR);
        NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(parentPath, NativeMethods.SHCNE_UPDATEDIR);

        RefreshExplorerWindows(folderPath);
    }

    private static void ToggleSystemReadOnly(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        try
        {
            var attrs = File.GetAttributes(folderPath);
            if ((attrs & (FileAttributes.System | FileAttributes.ReadOnly)) == 0) return;

            // Explorer に通知せず（黄色フォルダを出さないため）サイレントに 2 回 SetAttributes する。
            // これにより NTFS の ChangeTime が 2 回更新され、直後の SHCNE_ATTRIBUTES 通知を
            // Explorer が受け取ったとき「属性メタデータが変化した」と判断して desktop.ini を再評価する。
            File.SetAttributes(folderPath, attrs & ~(FileAttributes.System | FileAttributes.ReadOnly));
            File.SetAttributes(folderPath, attrs);
        }
        catch { }
    }

    private static void TryTouchFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        try { Directory.SetLastWriteTimeUtc(folderPath, DateTime.UtcNow); }
        catch { }
    }

    private static void NotifyCurrentIcon(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath)) return;

        NotifyPath(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        NotifyPidl(iconPath, NativeMethods.SHCNE_UPDATEITEM);
        ForceIconIndexUpdate(iconPath);
    }

    private static string? ResolveCurrentIconPath(string folderPath)
    {
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        var relativeIconPath = ReadIconResourcePath(desktopIniPath);
        if (!string.IsNullOrWhiteSpace(relativeIconPath))
        {
            var candidate = Path.IsPathRooted(relativeIconPath)
                ? relativeIconPath
                : Path.GetFullPath(Path.Combine(folderPath, relativeIconPath));
            if (File.Exists(candidate))
                return candidate;
        }

        var folderlyDir = Path.Combine(folderPath, "_folderly");
        if (!Directory.Exists(folderlyDir))
            return Path.Combine(folderlyDir, "cover.ico");

        return Directory.EnumerateFiles(folderlyDir, "cover_*.ico")
                   .OrderByDescending(File.GetLastWriteTimeUtc)
                   .FirstOrDefault()
               ?? Path.Combine(folderlyDir, "cover.ico");
    }

    private static string? ReadIconResourcePath(string desktopIniPath)
    {
        if (!File.Exists(desktopIniPath)) return null;

        try
        {
            var bytes = File.ReadAllBytes(desktopIniPath);
            var content = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE
                ? Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2)
                : File.ReadAllText(desktopIniPath, Encoding.UTF8);

            foreach (var rawLine in content.Split('\n'))
            {
                var line = rawLine.Trim();
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;

                var key = line[..eq].Trim();
                if (!string.Equals(key, "IconResource", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line[(eq + 1)..].Trim();
                var comma = value.LastIndexOf(',');
                return comma >= 0 ? value[..comma].Trim() : value;
            }
        }
        catch { }

        return null;
    }

    private static void ForceIconIndexUpdate(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path)) return;
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
