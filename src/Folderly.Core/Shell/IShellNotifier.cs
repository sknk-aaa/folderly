namespace Folderly.Core.Shell;

/// <summary>
/// Windows Shell へのアイコンキャッシュ更新通知インターフェース。
/// Core 層は OS 非依存を維持するため、実装は Folderly.Shell に委譲する。
/// </summary>
public interface IShellNotifier
{
    /// <summary>
    /// 指定フォルダのシェルアイコンキャッシュを更新する通知を送る。
    /// </summary>
    void NotifyFolderChanged(string folderPath);
}
