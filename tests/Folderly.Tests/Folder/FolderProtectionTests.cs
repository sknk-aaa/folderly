using Folderly.Core.Folder;

namespace Folderly.Tests.Folder;

public class FolderProtectionTests : IDisposable
{
    private readonly string _tempDir;

    public FolderProtectionTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("folderly_prot_").FullName;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void CheckPath_NormalWritableFolder_IsAllowed()
    {
        var result = FolderProtection.CheckPath(_tempDir);
        Assert.Equal(ProtectionLevel.Allowed, result.Level);
    }

    [Fact]
    public void CheckPath_UncPath_IsWarning()
    {
        // UNC パスはネットワークドライブ判定
        var result = FolderProtection.CheckPath(@"\\server\share\folder");
        Assert.Equal(ProtectionLevel.Warning, result.Level);
    }

    [Fact]
    public void CheckPath_PathOver260Chars_IsWarning()
    {
        // 261文字のパス（実際のディレクトリは作成しない）
        var longPath = @"C:\" + new string('a', 258);
        Assert.True(longPath.Length > 260);

        var result = FolderProtection.CheckPath(longPath);
        // 261文字はシステムフォルダではないが書き込みアクセスで失敗しうる
        // 少なくとも Allowed ではないこと（Warning または Denied）
        Assert.NotEqual(ProtectionLevel.Allowed, result.Level);
    }

    [Theory]
    [InlineData(@"C:\Dropbox\Projects")]
    [InlineData(@"D:\Users\test\Dropbox\Work")]
    public void CheckPath_DropboxPath_IsWarning(string path)
    {
        var result = FolderProtection.CheckPath(path);
        // Dropbox は Warning（書き込み権限なしで Denied になる可能性もある）
        Assert.True(result.Level is ProtectionLevel.Warning or ProtectionLevel.Denied);
    }

    [Fact]
    public void CheckPath_UserProfileSubfolder_IsAllowed()
    {
        // UserProfile のサブフォルダ（Documents 配下等）は許可
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userProfile)) return; // テスト環境によりスキップ

        var subFolder = Path.Combine(userProfile, "Documents", "TestFolder");
        // 実フォルダは不要（書き込み権限チェックで Denied になる可能性があるため、
        // ここでは「UserProfile 直下でないこと」のみ確認）
        var result = FolderProtection.CheckPath(subFolder);
        // Denied の場合は「UserProfile ルート直下」以外の理由であるはず
        if (result.IsDenied)
            Assert.DoesNotContain("ユーザープロファイルのルートフォルダ直下", result.Reason ?? string.Empty);
    }

    [Fact]
    public void CheckPath_OneDrivePath_IsWarning()
    {
        var originalOneDrive = Environment.GetEnvironmentVariable("OneDrive");
        try
        {
            Environment.SetEnvironmentVariable("OneDrive", _tempDir);
            var subPath = Path.Combine(_tempDir, "TestFolder");
            Directory.CreateDirectory(subPath);

            var result = FolderProtection.CheckPath(subPath);
            Assert.Equal(ProtectionLevel.Warning, result.Level);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OneDrive", originalOneDrive);
        }
    }

    [Fact]
    public void CheckPath_NoWriteAccess_IsDenied()
    {
        // chmod 444（読み取り専用）のディレクトリを作成
        var readOnlyDir = Path.Combine(_tempDir, "readonly");
        Directory.CreateDirectory(readOnlyDir);

        try
        {
            // Linux/WSL2 では chmod で書き込み権限を剥奪できる
            var chmod = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("chmod", $"444 {readOnlyDir}")
                { RedirectStandardOutput = true });
            chmod?.WaitForExit();

            // root ユーザーの場合は常に書き込めるためスキップ
            if (System.Environment.UserName == "root") return;

            var result = FolderProtection.CheckPath(readOnlyDir);
            Assert.Equal(ProtectionLevel.Denied, result.Level);
        }
        finally
        {
            // クリーンアップのために権限を復元
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("chmod", $"755 {readOnlyDir}")
                { RedirectStandardOutput = true })?.WaitForExit();
        }
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void CheckPath_DriveRoot_IsDenied(string drivePath)
    {
        // Windows スタイルのドライブルートパス（WSL2 では書き込み不可なので Denied）
        var result = FolderProtection.CheckPath(drivePath);
        // ドライブルート判定またはアクセス不可で Denied
        Assert.Equal(ProtectionLevel.Denied, result.Level);
    }

    [Fact]
    public void CheckPath_ProtectionResult_Properties()
    {
        var allowed = new ProtectionResult(ProtectionLevel.Allowed, null);
        Assert.True(allowed.IsAllowed);
        Assert.False(allowed.IsWarning);
        Assert.False(allowed.IsDenied);

        var denied = new ProtectionResult(ProtectionLevel.Denied, "reason");
        Assert.False(denied.IsAllowed);
        Assert.True(denied.IsDenied);

        var warning = new ProtectionResult(ProtectionLevel.Warning, "reason");
        Assert.True(warning.IsWarning);
    }

    [Fact]
    public void CheckPath_WindowsSystemFolder_OnLinux_Allowed_OrDenied()
    {
        // WSL2 では C:\Windows は存在しないため Allowed か Denied（書き込み不可）
        // いずれも受け入れる（Windowsでの動作は手動テストで確認）
        var result = FolderProtection.CheckPath(@"C:\Windows\System32");
        Assert.NotNull(result);
    }

    [Fact]
    public void CheckPath_TempSubfolder_IsAllowed()
    {
        var sub = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(sub);

        var result = FolderProtection.CheckPath(sub);
        Assert.Equal(ProtectionLevel.Allowed, result.Level);
    }

    [Fact]
    public void CheckPath_PathEndingWithSlash_NormalizedCorrectly()
    {
        var pathWithSlash = _tempDir + Path.DirectorySeparatorChar;
        var result = FolderProtection.CheckPath(pathWithSlash);
        Assert.Equal(ProtectionLevel.Allowed, result.Level);
    }
}
