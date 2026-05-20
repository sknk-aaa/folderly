using Folderly.Core.Application;
using Folderly.Core.Composition;
using Folderly.Core.History;
using Folderly.Core.Shell;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Tests.Application;

// IShellNotifier のノーオペレーションスタブ
internal sealed class NoOpShellNotifier : IShellNotifier
{
    public List<string> NotifiedPaths { get; } = new();
    public void NotifyFolderChanged(string folderPath)
        => NotifiedPaths.Add(folderPath);
}

public class ApplyRevertServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly HistoryRepository _repo;
    private readonly NoOpShellNotifier _notifier;
    private readonly ApplyService _applyService;
    private readonly RevertService _revertService;

    public ApplyRevertServiceTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("folderly_apply_test_").FullName;
        _repo = new HistoryRepository(":memory:");
        _notifier = new NoOpShellNotifier();
        _applyService = new ApplyService(_repo, _notifier);
        _revertService = new RevertService(_repo, _notifier);
    }

    public void Dispose()
    {
        _repo.Dispose();
        if (Directory.Exists(_tempDir))
        {
            // 属性を正規化してから削除（_tempDir 自体の System|ReadOnly も外す必要あり）
            foreach (var f in Directory.EnumerateFiles(_tempDir, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, FileAttributes.Normal);
            foreach (var d in Directory.EnumerateDirectories(_tempDir, "*", SearchOption.AllDirectories))
                File.SetAttributes(d, FileAttributes.Normal);
            File.SetAttributes(_tempDir, FileAttributes.Normal);
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private Stream CreateTestImageStream()
    {
        var img = new Image<Rgba32>(100, 100);
        img.Mutate(ctx => ctx.BackgroundColor(new Rgba32(0, 120, 212, 255)));
        var ms = new MemoryStream();
        img.SaveAsPng(ms);
        ms.Position = 0;
        img.Dispose();
        return ms;
    }

    private ApplyRequest MakeRequest(
        string? folderPath = null,
        bool forceApply = false,
        TagColor? tagColor = null)
    {
        return new ApplyRequest(
            FolderPath:       folderPath ?? _tempDir,
            SourceImageStream: CreateTestImageStream(),
            SourceImagePath:  "/test/image.png",
            AdjustParams:     new ImageAdjustParams(),
            TagColor:         tagColor ?? TagColors.None,
            ForceApply:       forceApply);
    }

    [Fact]
    public async Task ApplyAsync_NormalFolder_CreatesDesktopIni()
    {
        await _applyService.ApplyAsync(MakeRequest());

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        Assert.True(File.Exists(iniPath));
    }

    [Fact]
    public async Task ApplyAsync_NormalFolder_CreatesFolderlyDirectory()
    {
        await _applyService.ApplyAsync(MakeRequest());

        var folderlyDir = Path.Combine(_tempDir, ".folderly");
        Assert.True(Directory.Exists(folderlyDir));
        // cover_<hash8>.ico というユニーク名で生成される（Explorer キャッシュ無効化のため）
        var icoFiles = Directory.GetFiles(folderlyDir, "cover_*.ico");
        Assert.Single(icoFiles);
    }

    [Fact]
    public async Task ApplyAsync_Reapply_RegeneratesIcoWithNewName()
    {
        var folderlyDir = Path.Combine(_tempDir, ".folderly");

        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));
        var firstIco = Directory.GetFiles(folderlyDir, "cover_*.ico").Single();

        // タグ色を変えて再適用 → 別のファイル名になり、前のファイルは消える
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Red));
        var icoFiles = Directory.GetFiles(folderlyDir, "cover_*.ico");
        Assert.Single(icoFiles);
        Assert.NotEqual(firstIco, icoFiles[0]);
    }

    [Fact]
    public async Task ApplyAsync_NormalFolder_SavesHistoryEntry()
    {
        await _applyService.ApplyAsync(MakeRequest());

        var entry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task ApplyAsync_NormalFolder_ReturnsSuccess()
    {
        var result = await _applyService.ApplyAsync(MakeRequest());

        Assert.True(result.IsSuccess);
        Assert.False(result.IsWarning);
    }

    [Fact]
    public async Task ApplyAsync_DeniedPath_ThrowsFolderProtectionException()
    {
        // ドライブルートは拒否
        var req = MakeRequest(folderPath: @"C:\");
        await Assert.ThrowsAsync<FolderProtectionException>(
            () => _applyService.ApplyAsync(req));
    }

    [Fact]
    public async Task ApplyAsync_OneDrivePath_WithoutForce_ReturnsWarning()
    {
        var originalOneDrive = Environment.GetEnvironmentVariable("OneDrive");
        try
        {
            // _tempDir を OneDrive パスとして設定
            Environment.SetEnvironmentVariable("OneDrive", _tempDir);
            var subPath = Path.Combine(_tempDir, "OneDriveFolder");
            Directory.CreateDirectory(subPath);

            var req = MakeRequest(folderPath: subPath, forceApply: false);
            var result = await _applyService.ApplyAsync(req);

            Assert.True(result.IsWarning);
            Assert.False(result.IsSuccess);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OneDrive", originalOneDrive);
        }
    }

    [Fact]
    public async Task ApplyAsync_OneDrivePath_WithForce_Succeeds()
    {
        var originalOneDrive = Environment.GetEnvironmentVariable("OneDrive");
        try
        {
            Environment.SetEnvironmentVariable("OneDrive", _tempDir);
            var subPath = Path.Combine(_tempDir, "OneDriveForced");
            Directory.CreateDirectory(subPath);

            var req = MakeRequest(folderPath: subPath, forceApply: true);
            var result = await _applyService.ApplyAsync(req);

            Assert.True(result.IsSuccess);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OneDrive", originalOneDrive);
        }
    }

    [Fact]
    public async Task ApplyAsync_NotifiesShell()
    {
        await _applyService.ApplyAsync(MakeRequest());

        Assert.True(_notifier.NotifiedPaths.Count > 0);
    }

    [Fact]
    public async Task ApplyAsync_WithTagColor_SavesTagInHistory()
    {
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));

        var entry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        Assert.Equal("#0078D4", entry!.TagColor);
    }

    [Fact]
    public async Task RevertAsync_RestoresDesktopIni_WhenOriginallyAbsent()
    {
        // 元々 desktop.ini がない状態で適用→復元
        await _applyService.ApplyAsync(MakeRequest());

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        Assert.True(File.Exists(iniPath)); // 適用後は存在

        await _revertService.RevertAsync(_tempDir);

        Assert.False(File.Exists(iniPath)); // 復元後は削除
    }

    [Fact]
    public async Task RevertAsync_DeletesFolderlyDirectory()
    {
        await _applyService.ApplyAsync(MakeRequest());
        await _revertService.RevertAsync(_tempDir);

        var folderlyDir = Path.Combine(_tempDir, ".folderly");
        Assert.False(Directory.Exists(folderlyDir));
    }

    [Fact]
    public async Task RevertAsync_DeletesHistoryEntry()
    {
        await _applyService.ApplyAsync(MakeRequest());
        await _revertService.RevertAsync(_tempDir);

        var entry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        Assert.Null(entry);
    }

    [Fact]
    public async Task RevertAsync_NotifiesShell()
    {
        await _applyService.ApplyAsync(MakeRequest());
        _notifier.NotifiedPaths.Clear();

        await _revertService.RevertAsync(_tempDir);

        Assert.True(_notifier.NotifiedPaths.Count > 0);
    }

    [Fact]
    public async Task ApplyAsync_ExistingDesktopIni_IsBackedUpInHistory()
    {
        // 既存の desktop.ini がある場合のバックアップ確認
        var existingIni = "[.ShellClassInfo]\r\nInfoTip=MyFolder\r\n";
        File.WriteAllText(Path.Combine(_tempDir, "desktop.ini"), existingIni,
            new System.Text.UnicodeEncoding(false, true));

        await _applyService.ApplyAsync(MakeRequest());

        var entry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        Assert.NotNull(entry);
        Assert.True(entry!.HadDesktopIni);
        Assert.NotNull(entry.OriginalDesktopIniContent);
    }

    [Fact]
    public async Task ApplyAsync_ExistingDesktopIni_Restored_OnRevert()
    {
        var originalContent = "[.ShellClassInfo]\r\nInfoTip=OriginalTip\r\n";
        File.WriteAllText(Path.Combine(_tempDir, "desktop.ini"), originalContent,
            new System.Text.UnicodeEncoding(false, true));

        await _applyService.ApplyAsync(MakeRequest());
        await _revertService.RevertAsync(_tempDir);

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        Assert.True(File.Exists(iniPath));
        var restoredContent = File.ReadAllText(iniPath, new System.Text.UnicodeEncoding(false, true));
        Assert.Contains("InfoTip=OriginalTip", restoredContent);
    }
}
