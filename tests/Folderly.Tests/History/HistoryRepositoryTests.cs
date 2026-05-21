using Folderly.Core.History;

namespace Folderly.Tests.History;

public class HistoryRepositoryTests : IDisposable
{
    private readonly HistoryRepository _repo;

    public HistoryRepositoryTests()
    {
        _repo = new HistoryRepository(":memory:");
    }

    public void Dispose() => _repo.Dispose();

    private static HistoryEntry MakeEntry(
        string path = @"C:\Test\Folder",
        string? tagColor = null,
        DateTime? appliedAt = null,
        int? originalAttributes = null,
        bool hadDesktopIni = false,
        byte[]? originalDesktopIniContent = null,
        int? originalDesktopIniAttrs = null)
    {
        return new HistoryEntry(
            Id:                         null,
            FolderPath:                 path,
            OriginalAttributes:         originalAttributes ?? (int)FileAttributes.Directory,
            HadDesktopIni:              hadDesktopIni,
            OriginalDesktopIniContent:  originalDesktopIniContent,
            OriginalDesktopIniAttrs:    originalDesktopIniAttrs,
            SourceImagePath:            @"C:\Images\cover.jpg",
            IconHash:                   "abc123",
            IconStoragePath:            @"C:\AppData\Folderly\icons\abc123.ico",
            CropMode:                   "center",
            ImageScale:                 1.5,
            ImageOffsetX:               10.0,
            ImageOffsetY:               -5.0,
            TagColor:                   tagColor,
            AppliedAt:                  appliedAt ?? DateTime.UtcNow,
            SchemaVersion:              1);
    }

    [Fact]
    public void InitializeSchema_CreatesAllTables()
    {
        // スキーマが作成されているか（GetAll が例外なし）
        var all = _repo.GetAll();
        Assert.NotNull(all);
        Assert.Empty(all);
    }

    [Fact]
    public void Upsert_NewEntry_CanBeRetrieved()
    {
        var entry = MakeEntry(@"C:\Projects\App");
        _repo.Upsert(entry);

        var result = _repo.GetByPath(@"C:\Projects\App");
        Assert.NotNull(result);
        Assert.Equal(@"C:\Projects\App", result!.FolderPath);
    }

    [Fact]
    public void Upsert_SamePath_Overwrites()
    {
        _repo.Upsert(MakeEntry(@"C:\Folder"));
        _repo.Upsert(MakeEntry(@"C:\Folder", tagColor: "#0078D4"));

        var all = _repo.GetAll();
        Assert.Single(all);
        Assert.Equal("#0078D4", all[0].TagColor);
    }

    [Fact]
    public void Upsert_SamePath_PreservesOriginalBackupFields()
    {
        var originalIni = new byte[] { 0x41, 0x00 };
        _repo.Upsert(MakeEntry(
            @"C:\Folder",
            originalAttributes: (int)FileAttributes.Directory,
            hadDesktopIni: false,
            originalDesktopIniContent: null,
            originalDesktopIniAttrs: null));

        _repo.Upsert(MakeEntry(
            @"C:\Folder",
            tagColor: "#0078D4",
            originalAttributes: (int)(FileAttributes.Directory | FileAttributes.System | FileAttributes.ReadOnly),
            hadDesktopIni: true,
            originalDesktopIniContent: originalIni,
            originalDesktopIniAttrs: (int)(FileAttributes.Hidden | FileAttributes.System)));

        var result = _repo.GetByPath(@"C:\Folder")!;
        Assert.Equal("#0078D4", result.TagColor);
        Assert.Equal((int)FileAttributes.Directory, result.OriginalAttributes);
        Assert.False(result.HadDesktopIni);
        Assert.Null(result.OriginalDesktopIniContent);
        Assert.Null(result.OriginalDesktopIniAttrs);
    }

    [Fact]
    public void GetAll_ReturnsInDescendingOrderByAppliedAt()
    {
        _repo.Upsert(MakeEntry(@"C:\Old", appliedAt: DateTime.UtcNow.AddDays(-2)));
        _repo.Upsert(MakeEntry(@"C:\New", appliedAt: DateTime.UtcNow));

        var all = _repo.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Equal(@"C:\New", all[0].FolderPath); // 新しい方が先
    }

    [Fact]
    public void GetByPath_NotExisting_ReturnsNull()
    {
        var result = _repo.GetByPath(@"C:\DoesNotExist");
        Assert.Null(result);
    }

    [Fact]
    public void Delete_RemovesEntry()
    {
        _repo.Upsert(MakeEntry(@"C:\ToDelete"));
        _repo.Delete(@"C:\ToDelete");

        var result = _repo.GetByPath(@"C:\ToDelete");
        Assert.Null(result);
    }

    [Fact]
    public void SetSetting_GetSetting_RoundTrips()
    {
        _repo.SetSetting("language", "ja");
        var val = _repo.GetSetting("language");

        Assert.Equal("ja", val);
    }

    [Fact]
    public void GetSetting_UnknownKey_ReturnsNull()
    {
        var val = _repo.GetSetting("nonexistent_key");
        Assert.Null(val);
    }

    [Fact]
    public void EnforceMaxCount_RemovesOldEntries()
    {
        for (int i = 0; i < 6; i++)
            _repo.Upsert(MakeEntry(
                $@"C:\Folder{i}",
                appliedAt: DateTime.UtcNow.AddSeconds(i)));

        _repo.EnforceMaxCount(5);

        Assert.Equal(5, _repo.GetAll().Count);
    }

    [Fact]
    public void Upsert_AllFieldsPersisted()
    {
        var entry = new HistoryEntry(
            Id:                         null,
            FolderPath:                 @"C:\AllFields",
            OriginalAttributes:         (int)FileAttributes.Directory,
            HadDesktopIni:              true,
            OriginalDesktopIniContent:  new byte[] { 0xFF, 0xFE, 0x41, 0x00 },
            OriginalDesktopIniAttrs:    (int)(FileAttributes.Hidden | FileAttributes.System),
            SourceImagePath:            @"C:\img.png",
            IconHash:                   "deadbeef",
            IconStoragePath:            @"C:\icons\deadbeef.ico",
            CropMode:                   "pad",
            ImageScale:                 2.5,
            ImageOffsetX:               15.0,
            ImageOffsetY:               -8.0,
            TagColor:                   "#107C10",
            AppliedAt:                  new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc),
            SchemaVersion:              1);

        _repo.Upsert(entry);
        var result = _repo.GetByPath(@"C:\AllFields")!;

        Assert.True(result.HadDesktopIni);
        Assert.NotNull(result.OriginalDesktopIniContent);
        Assert.Equal(4, result.OriginalDesktopIniContent!.Length);
        Assert.Equal("pad", result.CropMode);
        Assert.Equal(2.5, result.ImageScale, precision: 5);
        Assert.Equal(15.0, result.ImageOffsetX, precision: 5);
        Assert.Equal(-8.0, result.ImageOffsetY, precision: 5);
        Assert.Equal("#107C10", result.TagColor);
    }

    [Fact]
    public void Upsert_NullOptionalFields_ArePreservedAsNull()
    {
        var entry = MakeEntry(@"C:\NullFields");
        Assert.Null(entry.OriginalDesktopIniContent);
        Assert.Null(entry.TagColor);

        _repo.Upsert(entry);
        var result = _repo.GetByPath(@"C:\NullFields")!;

        Assert.Null(result.OriginalDesktopIniContent);
        Assert.Null(result.TagColor);
    }
}
