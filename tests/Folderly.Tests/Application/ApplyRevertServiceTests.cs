using Folderly.Core;
using Folderly.Core.Application;
using Folderly.Core.Composition;
using Folderly.Core.Folder;
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

    private Stream CreateTestImageStream(byte r = 0, byte g = 120, byte b = 212)
    {
        var img = new Image<Rgba32>(100, 100);
        img.Mutate(ctx => ctx.BackgroundColor(new Rgba32(r, g, b, 255)));
        var ms = new MemoryStream();
        img.SaveAsPng(ms);
        ms.Position = 0;
        img.Dispose();
        return ms;
    }

    private ApplyRequest MakeRequest(
        string? folderPath = null,
        bool forceApply = false,
        TagColor? tagColor = null,
        Stream? sourceImageStream = null,
        string sourceImagePath = "/test/image.png")
    {
        return new ApplyRequest(
            FolderPath:       folderPath ?? _tempDir,
            SourceImageStream: sourceImageStream ?? CreateTestImageStream(),
            SourceImagePath:  sourceImagePath,
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

        var folderlyDir = Path.Combine(_tempDir, FolderlyConstants.FolderlyDirectoryName);
        Assert.True(Directory.Exists(folderlyDir));
        // cover_<hash8>.ico というユニーク名で生成される（Explorer キャッシュ無効化のため）
        var icoFiles = Directory.GetFiles(folderlyDir, "cover_*.ico");
        Assert.Single(icoFiles);
    }

    [Fact]
    public async Task ApplyAsync_Reapply_RegeneratesIcoWithNewName()
    {
        var folderlyDir = Path.Combine(_tempDir, FolderlyConstants.FolderlyDirectoryName);

        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));
        var firstIco = Directory.GetFiles(folderlyDir, "cover_*.ico").Single();

        // タグ色を変えて再適用 → 別のファイル名になる。
        // 旧ICOは Explorer が一時的に参照している可能性があるため、適用時には消さない。
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Red));
        var icoFiles = Directory.GetFiles(folderlyDir, "cover_*.ico");

        Assert.Equal(2, icoFiles.Length);
        Assert.Contains(firstIco, icoFiles);
        Assert.Contains(icoFiles, p => !string.Equals(p, firstIco, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyAsync_Reapply_WithSameImagePathButDifferentContent_RegeneratesIcoWithNewName()
    {
        var folderlyDir = Path.Combine(_tempDir, FolderlyConstants.FolderlyDirectoryName);

        await _applyService.ApplyAsync(MakeRequest(
            sourceImageStream: CreateTestImageStream(0, 120, 212),
            sourceImagePath: "/test/same-image.png"));
        var firstIco = Directory.GetFiles(folderlyDir, "cover_*.ico").Single();

        await _applyService.ApplyAsync(MakeRequest(
            sourceImageStream: CreateTestImageStream(196, 43, 28),
            sourceImagePath: "/test/same-image.png"));
        var icoFiles = Directory.GetFiles(folderlyDir, "cover_*.ico");

        Assert.Equal(2, icoFiles.Length);
        Assert.Contains(firstIco, icoFiles);
        Assert.Contains(icoFiles, p => !string.Equals(p, firstIco, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyAsync_Reapply_DesktopIniPointsToLatestIco()
    {
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));
        var firstEntry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        var firstIco = firstEntry!.IconStoragePath;

        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Red));
        var latestEntry = _repo.GetByPath(Path.GetFullPath(_tempDir));
        var latestIco = latestEntry!.IconStoragePath;

        var content = DesktopIniManager.Read(_tempDir);
        var expectedResource = $@"IconResource={latestIco},0";
        var expectedFile = $@"IconFile={latestIco}";
        Assert.NotEqual(firstIco, latestIco);
        Assert.Contains(expectedResource, content);
        Assert.Contains(expectedFile, content);
        Assert.Contains("IconIndex=0", content);
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
    public async Task RevertAsync_AfterReapply_WhenOriginallyAbsent_DeletesDesktopIni()
    {
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Red));

        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        Assert.True(File.Exists(iniPath));

        await _revertService.RevertAsync(_tempDir);

        Assert.False(File.Exists(iniPath));
    }

    [Fact]
    public async Task RevertAsync_WithCorruptedFolderlyBackup_RemovesFolderlyDesktopIni()
    {
        var iniPath = Path.Combine(_tempDir, "desktop.ini");
        var folderlyContent =
            "[.ShellClassInfo]\r\n" +
            @"IconResource=C:\Users\tester\AppData\Local\Folderly\icons\abc.ico,0" + "\r\n" +
            @"IconFile=C:\Users\tester\AppData\Local\Folderly\icons\abc.ico" + "\r\n" +
            "IconIndex=0\r\n";

        File.WriteAllText(iniPath, folderlyContent, new System.Text.UnicodeEncoding(false, true));
        var folderlyDir = Path.Combine(_tempDir, FolderlyConstants.FolderlyDirectoryName);
        Directory.CreateDirectory(folderlyDir);
        File.WriteAllText(Path.Combine(folderlyDir, "cover_abc.ico"), "fake");

        _repo.Upsert(new HistoryEntry(
            Id: null,
            FolderPath: Path.GetFullPath(_tempDir),
            OriginalAttributes: (int)(FileAttributes.Directory | FileAttributes.System | FileAttributes.ReadOnly),
            HadDesktopIni: true,
            OriginalDesktopIniContent: System.Text.Encoding.Unicode.GetBytes(folderlyContent),
            OriginalDesktopIniAttrs: (int)(FileAttributes.Hidden | FileAttributes.System),
            SourceImagePath: "image.png",
            IconHash: "abc",
            IconStoragePath: @"C:\Users\tester\AppData\Local\Folderly\icons\abc.ico",
            CropMode: "center",
            ImageScale: 1,
            ImageOffsetX: 0,
            ImageOffsetY: 0,
            TagColor: null,
            AppliedAt: DateTime.UtcNow));

        await _revertService.RevertAsync(_tempDir);

        Assert.False(File.Exists(iniPath));
        Assert.False(Directory.Exists(folderlyDir));
    }

    [Fact]
    public async Task RevertAsync_DeletesFolderlyDirectory()
    {
        await _applyService.ApplyAsync(MakeRequest());
        await _revertService.RevertAsync(_tempDir);

        var folderlyDir = Path.Combine(_tempDir, FolderlyConstants.FolderlyDirectoryName);
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

    [Fact]
    public async Task ApplyAsync_ExistingDesktopIni_Reapply_RestoresOriginalOnRevert()
    {
        var originalContent = "[.ShellClassInfo]\r\nInfoTip=OriginalTip\r\n";
        File.WriteAllText(Path.Combine(_tempDir, "desktop.ini"), originalContent,
            new System.Text.UnicodeEncoding(false, true));

        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Blue));
        await _applyService.ApplyAsync(MakeRequest(tagColor: TagColors.Red));
        await _revertService.RevertAsync(_tempDir);

        var restoredContent = File.ReadAllText(
            Path.Combine(_tempDir, "desktop.ini"),
            new System.Text.UnicodeEncoding(false, true));
        Assert.Contains("InfoTip=OriginalTip", restoredContent);
        Assert.DoesNotContain("IconResource", restoredContent);
        Assert.DoesNotContain("IconFile", restoredContent);
    }
}
