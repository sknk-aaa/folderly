using Folderly.Core.Composition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Tests.Composition;

public class TemplateRendererTests
{
    private static Image<Rgba32> CreateTestAdjustedImage(int w = 224, int h = 158)
    {
        var img = new Image<Rgba32>(w, h);
        img.Mutate(ctx => ctx.BackgroundColor(new Rgba32(100, 150, 200, 255)));
        return img;
    }

    private Rgba32 SampleTagRegionPixel(Image<Rgba32> img, int outputSize)
    {
        var region = FolderTemplate.ScaleRegion(FolderTemplate.TagRegion, outputSize);
        int sampleX = (int)(region.X + region.Width * 0.5f);
        int sampleY = (int)(region.Y + region.Height * 0.5f);
        sampleX = Math.Clamp(sampleX, 0, img.Width - 1);
        sampleY = Math.Clamp(sampleY, 0, img.Height - 1);
        return img[sampleX, sampleY];
    }

    [Fact]
    public void Render_NoTag_OutputSizeCorrect()
    {
        using var adj = CreateTestAdjustedImage();
        using var result = TemplateRenderer.Render(adj, TagColors.None);

        Assert.Equal(FolderTemplate.BaseSize, result.Width);
        Assert.Equal(FolderTemplate.BaseSize, result.Height);
    }

    [Fact]
    public void Render_PreviewSize320_OutputIs320x320()
    {
        using var adj = CreateTestAdjustedImage();
        using var result = TemplateRenderer.Render(adj, TagColors.None, outputSize: 320);

        Assert.Equal(320, result.Width);
        Assert.Equal(320, result.Height);
    }

    [Theory]
    [InlineData("#0078D4", 0, 120, 212)]
    [InlineData("#107C10", 16, 124, 16)]
    [InlineData("#D83B01", 216, 59, 1)]
    [InlineData("#8764B8", 135, 100, 184)]
    [InlineData("#C42B1C", 196, 43, 28)]
    [InlineData("#7A7574", 122, 117, 116)]
    public void Render_WithTagColor_TagRegionHasExpectedColor(
        string hexCode, byte expR, byte expG, byte expB)
    {
        var tag = new TagColor(hexCode, "test");
        using var adj = CreateTestAdjustedImage();
        using var result = TemplateRenderer.Render(adj, tag);

        var px = SampleTagRegionPixel(result, FolderTemplate.BaseSize);

        // 誤差 ±10 を許容（リサイズによるアンチエイリアシング等）
        Assert.InRange(px.R, expR - 10, expR + 10);
        Assert.InRange(px.G, expG - 10, expG + 10);
        Assert.InRange(px.B, expB - 10, expB + 10);
    }

    [Fact]
    public void Render_NullTagColor_SameAsNone()
    {
        using var adj1 = CreateTestAdjustedImage();
        using var adj2 = CreateTestAdjustedImage();
        using var result1 = TemplateRenderer.Render(adj1, null);
        using var result2 = TemplateRenderer.Render(adj2, TagColors.None);

        // 出力サイズが一致
        Assert.Equal(result1.Width, result2.Width);
        Assert.Equal(result1.Height, result2.Height);
    }

    [Fact]
    public void Render_OutputIsRgba32()
    {
        using var adj = CreateTestAdjustedImage();
        using var result = TemplateRenderer.Render(adj, TagColors.Blue);

        Assert.IsType<Image<Rgba32>>(result);
    }

    [Fact]
    public void Render_ImageContent_AppearInImageRegion()
    {
        // 画像表示領域に青いピクセルが描画されているか確認
        using var adj = CreateTestAdjustedImage(224, 158); // 青系
        using var result = TemplateRenderer.Render(adj, TagColors.None);

        var scaledImgRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, FolderTemplate.BaseSize);
        int sampleX = (int)(scaledImgRegion.X + scaledImgRegion.Width * 0.5f);
        int sampleY = (int)(scaledImgRegion.Y + scaledImgRegion.Height * 0.5f);
        var px = result[sampleX, sampleY];

        // 青系の色 (100, 150, 200) が見えているはず
        Assert.True(px.B > px.R, "Blue channel should dominate in image region");
    }

    [Fact]
    public void Render_ImageContent_AppearsNearBottomOfRoundedImageRegion()
    {
        using var adj = CreateTestAdjustedImage(224, 158);
        using var result = TemplateRenderer.Render(adj, TagColors.None);

        var scaledImgRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, FolderTemplate.BaseSize);
        int sampleX = (int)(scaledImgRegion.X + scaledImgRegion.Width * 0.5f);
        int sampleY = (int)(scaledImgRegion.Bottom - scaledImgRegion.Height * 0.12f);
        var px = result[sampleX, sampleY];

        Assert.True(px.B > px.R, "Image content should remain visible near the bottom; no front pocket should cover it");
    }

    [Fact]
    public void Render_TagNone_TagRegionNotOverriddenWithSolidColor()
    {
        using var adj = CreateTestAdjustedImage();
        using var resultWithTag = TemplateRenderer.Render(adj, TagColors.Blue);
        using var adj2 = CreateTestAdjustedImage();
        using var resultNoTag = TemplateRenderer.Render(adj2, TagColors.None);

        var pxWithTag = SampleTagRegionPixel(resultWithTag, FolderTemplate.BaseSize);
        var pxNoTag = SampleTagRegionPixel(resultNoTag, FolderTemplate.BaseSize);

        // タグあり → タグなしで TagRegion のピクセルが異なる
        Assert.True(
            pxWithTag.R != pxNoTag.R || pxWithTag.G != pxNoTag.G || pxWithTag.B != pxNoTag.B,
            "Tag region should differ between with-tag and no-tag renders");
    }

    [Fact]
    public void Render_SmallOutputSize_DoesNotThrow()
    {
        using var adj = CreateTestAdjustedImage(16, 10);
        using var result = TemplateRenderer.Render(adj, TagColors.None, outputSize: 32);

        Assert.Equal(32, result.Width);
    }
}
