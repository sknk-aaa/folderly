using System.Text;

namespace Folderly.Core.Folder;

/// <summary>
/// desktop.ini の読み書き（UTF-16 LE BOM、原子的書き込み）。
/// INI パーサーは外部ライブラリなし、文字列行処理で実装。
/// </summary>
public static class DesktopIniManager
{
    private const string ShellClassInfoSection = ".ShellClassInfo";
    private const string IconResourceKey = "IconResource";
    private const string IconFileKey = "IconFile";
    private const string IconIndexKey = "IconIndex";

    private static readonly Encoding Utf16LeBom =
        new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

    /// <summary>
    /// desktop.ini の内容を読み込む。存在しない場合は null を返す。
    /// </summary>
    public static string? Read(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(iniPath)) return null;

        // UTF-16 LE BOM を含む場合も含まない場合も読める
        var bytes = File.ReadAllBytes(iniPath);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Utf16LeBom.GetString(bytes, 2, bytes.Length - 2);

        return File.ReadAllText(iniPath, Encoding.UTF8);
    }

    /// <summary>
    /// [.ShellClassInfo] セクションに IconResource キーを追加/更新して書き込む。
    /// 既存の他セクション・他キーは保持する。原子的書き込み（temp → Move）。
    /// </summary>
    public static void Write(
        string folderPath,
        string iconRelativePath,
        string? existingContent = null)
    {
        string newContent = existingContent is not null
            ? UpdateIconKeys(existingContent, iconRelativePath)
            : BuildMinimalIni(iconRelativePath);

        WriteRaw(folderPath, newContent);
    }

    /// <summary>
    /// 生文字列を desktop.ini に原子的に書き込む（RevertService 用）。
    /// </summary>
    public static void WriteRaw(string folderPath, string content)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        var tempPath = iniPath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, content, Utf16LeBom);
            File.Move(tempPath, iniPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// INI 文字列の指定セクション・キーの値を追加/更新する。
    /// </summary>
    public static string UpdateOrAddKey(
        string content, string section, string key, string value)
    {
        var lines = content.Split('\n');
        var result = new List<string>(lines.Length + 2);

        bool inTargetSection = false;
        bool keyUpdated = false;
        bool sectionFound = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith('['))
            {
                // 対象セクションを抜けた直後にキーが未追加なら追加
                if (inTargetSection && !keyUpdated)
                {
                    result.Add($"{key}={value}");
                    keyUpdated = true;
                }
                inTargetSection = line.Equals($"[{section}]",
                    StringComparison.OrdinalIgnoreCase);
                if (inTargetSection) sectionFound = true;
            }
            else if (inTargetSection && !keyUpdated)
            {
                // キーが既に存在する場合は上書き
                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    var existingKey = line[..eq].Trim();
                    if (string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add($"{key}={value}");
                        keyUpdated = true;
                        continue;
                    }
                }
            }

            result.Add(rawLine.TrimEnd('\r'));
        }

        // セクション末尾でキーが未追加
        if (inTargetSection && !keyUpdated)
        {
            result.Add($"{key}={value}");
            keyUpdated = true;
        }

        // セクション自体が存在しなかった場合は末尾に追加
        if (!sectionFound)
        {
            if (result.Count > 0 && !string.IsNullOrWhiteSpace(result[^1]))
                result.Add(string.Empty);
            result.Add($"[{section}]");
            result.Add($"{key}={value}");
        }

        return string.Join("\r\n", result);
    }

    private static string UpdateIconKeys(string content, string iconPath)
    {
        var updated = UpdateOrAddKey(
            content, ShellClassInfoSection, IconResourceKey, $"{iconPath},0");
        updated = UpdateOrAddKey(
            updated, ShellClassInfoSection, IconFileKey, iconPath);
        updated = UpdateOrAddKey(
            updated, ShellClassInfoSection, IconIndexKey, "0");
        return updated;
    }

    private static string BuildMinimalIni(string iconRelativePath)
        => $"[{ShellClassInfoSection}]\r\n"
           + $"{IconResourceKey}={iconRelativePath},0\r\n"
           + $"{IconFileKey}={iconRelativePath}\r\n"
           + $"{IconIndexKey}=0\r\n";
}
