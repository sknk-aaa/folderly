using Folderly.Core.Composition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Folderly.Tests.Composition;

public class FolderTemplateTests
{
    [Fact]
    public void TagRegion_WidthMatchesTabShape()
    {
        Assert.Equal(FolderTemplate.BaseSize * FolderTemplate.TabSlopeEndRatio, FolderTemplate.TagRegion.Width, precision: 3);
    }

    [Fact]
    public void TagRegion_HeightMatchesTabShape()
    {
        Assert.Equal(FolderTemplate.BaseSize * FolderTemplate.TagHeightRatio, FolderTemplate.TagRegion.Height, precision: 3);
    }

    [Fact]
    public void ImageRegion_IsInsideFolderBodyRegion()
    {
        var img = FolderTemplate.ImageRegion;
        var body = FolderTemplate.FolderBodyRegion;

        Assert.True(img.Y >= body.Y, "ImageRegion should start at or below FolderBodyRegion top");
        Assert.True(img.Bottom <= body.Bottom + 1f, "ImageRegion should end within FolderBodyRegion");
    }

    [Fact]
    public void ScaleRegion_HalvesAllDimensions()
    {
        var original = FolderTemplate.ImageRegion;
        var scaled = FolderTemplate.ScaleRegion(original, FolderTemplate.BaseSize / 2f);

        Assert.Equal(original.X / 2f, scaled.X, precision: 3);
        Assert.Equal(original.Y / 2f, scaled.Y, precision: 3);
        Assert.Equal(original.Width / 2f, scaled.Width, precision: 3);
        Assert.Equal(original.Height / 2f, scaled.Height, precision: 3);
    }

    [Fact]
    public void GetTemplateBytes_ReturnsNonEmptyBytes()
    {
        var bytes = FolderTemplate.GetTemplateBytes();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetTemplateBytes_HasPngSignature()
    {
        var bytes = FolderTemplate.GetTemplateBytes();
        // PNG シグネチャ: 0x89 0x50 0x4E 0x47 ...
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }

    [Fact]
    public void GenerateTemplatePng_TabTopIsTrimmed()
    {
        using var img = Image.Load<Rgba32>(FolderTemplate.GenerateTemplatePng());
        int x = (int)(FolderTemplate.BaseSize * FolderTemplate.TabXRatio + 12);
        int y = (int)(FolderTemplate.BaseSize * FolderTemplate.TabYRatio + 4);

        Assert.Equal(0, img[x, y].A);
    }

    [Fact]
    public void GenerateTemplatePng_TabStillReachesImageTop()
    {
        using var img = Image.Load<Rgba32>(FolderTemplate.GenerateTemplatePng());
        int x = (int)(FolderTemplate.BaseSize * FolderTemplate.TabXRatio + 12);
        int y = (int)(FolderTemplate.ImageRegion.Y - 2);

        Assert.True(img[x, y].A > 0);
    }

    [Fact]
    public void GetFrontTemplateBytes_HasPngSignature()
    {
        var bytes = FolderTemplate.GetFrontTemplateBytes();
        Assert.True(bytes.Length > 0);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public void TagColors_AllContains7Items()
    {
        Assert.Equal(7, TagColors.All.Count);
    }

    [Fact]
    public void TagColors_NoneHasNullHexColor()
    {
        Assert.Null(TagColors.None.HexColor);
        Assert.True(TagColors.None.IsNone);
    }

    [Fact]
    public void TagColors_BlueHasCorrectHexCode()
    {
        Assert.Equal("#0078D4", TagColors.Blue.HexColor);
        Assert.False(TagColors.Blue.IsNone);
    }

    [Fact]
    public void TagColors_FromKey_ReturnsCorrectColor()
    {
        var blue = TagColors.FromKey("blue");
        Assert.NotNull(blue);
        Assert.Equal("#0078D4", blue!.HexColor);
    }

    [Fact]
    public void TagColors_FromKey_UnknownKey_ReturnsNull()
    {
        var result = TagColors.FromKey("unknown_key");
        Assert.Null(result);
    }
}
