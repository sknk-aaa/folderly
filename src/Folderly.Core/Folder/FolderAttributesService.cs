namespace Folderly.Core.Folder;

/// <summary>
/// フォルダ・ファイル属性の設定/復元。
/// .NET 標準 FileAttributes で実装（P/Invoke 不要、クロスプラットフォームテスト対応）。
/// </summary>
public static class FolderAttributesService
{
    public static FileAttributes GetAttributes(string path)
        => File.GetAttributes(path);

    /// <summary>対象フォルダに desktop.ini カスタマイズ用の属性を付与する。</summary>
    public static void ApplyFolderAttributes(string folderPath)
    {
        var current = File.GetAttributes(folderPath);
        File.SetAttributes(folderPath, current | FileAttributes.System | FileAttributes.ReadOnly);
    }

    /// <summary>desktop.ini に HIDDEN | SYSTEM を付与する。</summary>
    public static void ApplyDesktopIniAttributes(string iniPath)
    {
        var current = File.GetAttributes(iniPath);
        File.SetAttributes(iniPath, current | FileAttributes.Hidden | FileAttributes.System);
    }

    /// <summary>_folderly\ サブフォルダに HIDDEN を付与する。</summary>
    public static void ApplyHiddenFolderlyAttributes(string folderlyPath)
    {
        var current = File.GetAttributes(folderlyPath);
        File.SetAttributes(folderlyPath, current | FileAttributes.Hidden);
    }

    /// <summary>属性を指定した値に復元する。</summary>
    public static void RestoreAttributes(string path, FileAttributes attributes)
        => File.SetAttributes(path, attributes);
}
