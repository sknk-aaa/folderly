using Folderly.Core.Composition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
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
    public void Render_WithTagColor_ImageRegionStaysInFrontOfTag()
    {
        var tag = new TagColor("#0078D4", "test");
        using var adj = CreateTestAdjustedImage();
        using var result = TemplateRenderer.Render(adj, tag);

        var imageRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, FolderTemplate.BaseSize);
        int sampleX = (int)(imageRegion.X + imageRegion.Width * 0.16f);
        int sampleY = (int)(imageRegion.Y + imageRegion.Height * 0.03f);
        var px = result[sampleX, sampleY];

        Assert.True(px.B > px.R, "Image content should be in front of the tag where they overlap");
    }

    [Fact]
    public void Render_TransparentPadding_RevealsWhiteImageBaseNotTag()
    {
        var tag = new TagColor("#0078D4", "test");
        var region = FolderTemplate.ImageRegion;
        using var adj = new Image<Rgba32>((int)region.Width, (int)region.Height);
        adj.Mutate(ctx =>
        {
            ctx.Clear(Color.Transparent);
            ctx.Fill(
                new Rgba32(100, 150, 200, 255),
                new Rectangle((int)(adj.Width * 0.25f), 0, (int)(adj.Width * 0.5f), adj.Height));
        });
        using var result = TemplateRenderer.Render(adj, tag);

        var imageRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, FolderTemplate.BaseSize);
        int sampleX = (int)(imageRegion.X + imageRegion.Width * 0.08f);
        int sampleY = (int)(imageRegion.Y + imageRegion.Height * 0.03f);
        var px = result[sampleX, sampleY];

        Assert.True(px.R > 245 && px.G > 245 && px.B > 245, "Transparent padding should reveal the white image base");
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
    public void Render_WithTagName_DrawsTextInTagArea()
    {
        using var adj1 = CreateTestAdjustedImage();
        using var adj2 = CreateTestAdjustedImage();
        using var resultWithName = TemplateRenderer.Render(adj1, TagColors.Blue, tagName: "開発");
        using var resultWithoutName = TemplateRenderer.Render(adj2, TagColors.Blue);

        Assert.True(CountDifferentPixels(resultWithName, resultWithoutName) > 0);
    }

    [Fact]
    public void Render_EmptyTagName_DoesNotDrawText()
    {
        using var adj1 = CreateTestAdjustedImage();
        using var adj2 = CreateTestAdjustedImage();
        using var resultNull = TemplateRenderer.Render(adj1, TagColors.Blue);
        using var resultEmpty = TemplateRenderer.Render(adj2, TagColors.Blue, tagName: "");

        Assert.True(ImagesEqual(resultNull, resultEmpty));
    }

    [Fact]
    public void Render_LongTagName_DoesNotAffectImageArea()
    {
        using var adj1 = CreateTestAdjustedImage();
        using var adj2 = CreateTestAdjustedImage();
        using var resultWithName = TemplateRenderer.Render(
            adj1, TagColors.Purple, tagName: "とても長いタグ名テキスト");
        using var resultWithoutName = TemplateRenderer.Render(adj2, TagColors.Purple);

        var imageRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, FolderTemplate.BaseSize);
        int sampleX = (int)(imageRegion.X + imageRegion.Width * 0.5f);
        int sampleY = (int)(imageRegion.Y + imageRegion.Height * 0.5f);

        Assert.Equal(resultWithoutName[sampleX, sampleY], resultWithName[sampleX, sampleY]);
    }

    [Fact]
    public void Render_TagNone_IgnoresTagName()
    {
        using var adj1 = CreateTestAdjustedImage();
        using var adj2 = CreateTestAdjustedImage();
        using var resultWithName = TemplateRenderer.Render(adj1, TagColors.None, tagName: "開発");
        using var resultWithoutName = TemplateRenderer.Render(adj2, TagColors.None);

        Assert.True(ImagesEqual(resultWithoutName, resultWithName));
    }

    [Fact]
    public void Render_SmallOutputSize_DoesNotThrow()
    {
        using var adj = CreateTestAdjustedImage(16, 10);
        using var result = TemplateRenderer.Render(adj, TagColors.None, outputSize: 32);

        Assert.Equal(32, result.Width);
    }

    private static int CountDifferentPixels(Image<Rgba32> a, Image<Rgba32> b)
    {
        var count = 0;
        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            if (!a[x, y].Equals(b[x, y]))
                count++;
        }

        return count;
    }

    private static bool ImagesEqual(Image<Rgba32> a, Image<Rgba32> b)
        => CountDifferentPixels(a, b) == 0;
}
