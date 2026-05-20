using System.Text;
using Folderly.Core.Folder;

namespace Folderly.Tests.Folder;

public class DesktopIniManagerTests : IDisposable
{
    private readonly string _tempDir;

    public DesktopIniManagerTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("folderly_test_").FullName;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Read_NonExistentFile_ReturnsNull()
    {
        var result = DesktopIniManager.Read(_tempDir);
        Assert.Null(result);
    }

    [Fact]
    public void Write_NewFile_CreatesDesktopIni()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        Assert.True(File.Exists(iniPath));
    }

    [Fact]
    public void Write_NewFile_ContainsIconResource()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");

        var content = DesktopIniManager.Read(_tempDir);
        Assert.NotNull(content);
        Assert.Contains("IconResource", content);
        Assert.Contains(".folderly", content);
    }

    [Fact]
    public void Write_NewFile_HasUtf16LeBom()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        var bytes = File.ReadAllBytes(iniPath);

        Assert.True(bytes.Length >= 2);
        Assert.Equal(0xFF, bytes[0]); // UTF-16 LE BOM
        Assert.Equal(0xFE, bytes[1]);
    }

    [Fact]
    public void Read_ExistingFile_ReturnsContent()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");
        var content = DesktopIniManager.Read(_tempDir);

        Assert.NotNull(content);
        Assert.True(content!.Length > 0);
    }

    [Fact]
    public void Write_ExistingWithOtherSection_PreservesOtherSection()
    {
        var existing = "[ViewState]\r\nMode=\r\nVid=\r\n\r\n[.ShellClassInfo]\r\nOtherKey=value\r\n";
        DesktopIniManager.WriteRaw(_tempDir, existing);

        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico",
            existingContent: existing);

        var content = DesktopIniManager.Read(_tempDir);
        Assert.NotNull(content);
        Assert.Contains("[ViewState]", content);
        Assert.Contains("OtherKey=value", content);
    }

    [Fact]
    public void Write_ExistingWithIconResource_UpdatesIt()
    {
        var existing = "[.ShellClassInfo]\r\nIconResource=old_path.ico,0\r\n";
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico",
            existingContent: existing);

        var content = DesktopIniManager.Read(_tempDir);
        Assert.NotNull(content);
        Assert.Contains(".folderly", content);
        Assert.DoesNotContain("old_path.ico", content);
    }

    [Fact]
    public void Write_TempFileNotRemainingAfterSuccess()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");

        var tmpFile = Path.Combine(_tempDir, "desktop.ini.tmp");
        Assert.False(File.Exists(tmpFile));
    }

    [Fact]
    public void UpdateOrAddKey_NoSection_AddsSection()
    {
        var content = "[Other]\r\nKey=Val\r\n";
        var result = DesktopIniManager.UpdateOrAddKey(
            content, ".ShellClassInfo", "IconResource", @".folderly\cover.ico,0");

        Assert.Contains("[.ShellClassInfo]", result);
        Assert.Contains("IconResource=", result);
    }

    [Fact]
    public void UpdateOrAddKey_ExistingSection_AddsKey()
    {
        var content = "[.ShellClassInfo]\r\nOtherKey=value\r\n";
        var result = DesktopIniManager.UpdateOrAddKey(
            content, ".ShellClassInfo", "IconResource", @".folderly\cover.ico,0");

        Assert.Contains("OtherKey=value", result);
        Assert.Contains("IconResource=", result);
    }

    [Fact]
    public void UpdateOrAddKey_ExistingKey_UpdatesValue()
    {
        var content = "[.ShellClassInfo]\r\nIconResource=old.ico,0\r\n";
        var result = DesktopIniManager.UpdateOrAddKey(
            content, ".ShellClassInfo", "IconResource", @".folderly\cover.ico,0");

        Assert.Contains(@".folderly\cover.ico,0", result);
        Assert.DoesNotContain("old.ico", result);
    }

    [Fact]
    public void Write_OutputHasCrlfLineEnding()
    {
        DesktopIniManager.Write(_tempDir, @".folderly\cover.ico");

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        var bytes = File.ReadAllBytes(iniPath);
        // UTF-16 LE の CRLF は 0x0D 0x00 0x0A 0x00
        // BOM (2 bytes) の後にコンテンツ
        var text = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        Assert.Contains("\r\n", text);
    }
}
