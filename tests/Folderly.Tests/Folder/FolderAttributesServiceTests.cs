using Folderly.Core.Folder;

namespace Folderly.Tests.Folder;

public class FolderAttributesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public FolderAttributesServiceTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("folderly_attr_test_").FullName;
        _tempFile = Path.Combine(_tempDir, "test.ini");
        File.WriteAllText(_tempFile, "test");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.SetAttributes(_tempFile, FileAttributes.Normal);
            File.Delete(_tempFile);
        }
        if (Directory.Exists(_tempDir))
        {
            File.SetAttributes(_tempDir, FileAttributes.Normal);
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetAttributes_ReturnsFileAttributes()
    {
        var attrs = FolderAttributesService.GetAttributes(_tempDir);
        // ディレクトリ属性が含まれているはず
        Assert.True(attrs.HasFlag(FileAttributes.Directory));
    }

    [Fact]
    public void ApplyFolderAttributes_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            FolderAttributesService.ApplyFolderAttributes(_tempDir));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyDesktopIniAttributes_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            FolderAttributesService.ApplyDesktopIniAttributes(_tempFile));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyHiddenFolderlyAttributes_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            FolderAttributesService.ApplyHiddenFolderlyAttributes(_tempDir));
        Assert.Null(ex);
    }

    [Fact]
    public void RestoreAttributes_DoesNotThrow()
    {
        var original = FolderAttributesService.GetAttributes(_tempFile);
        var ex = Record.Exception(() =>
            FolderAttributesService.RestoreAttributes(_tempFile, original));
        Assert.Null(ex);
    }
}
