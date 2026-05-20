namespace Folderly.Core;

/// <summary>
/// Folderly が各フォルダ内に配置する隠しサブディレクトリ名と関連定数。
/// OneDrive がドット始まりの隠しディレクトリを sync 対象から外して
/// ローカル側を dehydrate / 削除する挙動を回避するため、ドット無しの "_folderly" を採用。
/// </summary>
public static class FolderlyConstants
{
    public const string FolderlyDirectoryName = "_folderly";

    /// <summary>互換用: 旧バージョンが使用していたディレクトリ名（apply/revert で掃除対象）。</summary>
    public const string LegacyFolderlyDirectoryName = ".folderly";
}
