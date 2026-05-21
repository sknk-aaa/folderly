using Folderly.Core.Composition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Tests.Composition;

public class ImageAdjusterTests
{
    private static Image<Rgba32> CreateSolidImage(int w, int h, byte r = 200, byte g = 100, byte b = 50)
    {
        var img = new Image<Rgba32>(w, h);
        img.Mutate(ctx => ctx.BackgroundColor(new Rgba32(r, g, b, 255)));
        return img;
    }

    private static readonly Size Target = new(160, 120);

    [Fact]
    public void Adjust_DefaultParams_OutputMatchesTargetSize()
    {
        using var src = CreateSolidImage(200, 150);
        using var result = ImageAdjuster.Adjust(src, Target);

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_Scale2x_OutputSizeUnchanged()
    {
        using var src = CreateSolidImage(100, 100);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Scale: 2.0f));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_ScaleHalf_OutputSizeUnchanged()
    {
        using var src = CreateSolidImage(300, 300);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Scale: 0.5f));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_OffsetX_DoesNotThrow()
    {
        using var src = CreateSolidImage(200, 200, r: 255, g: 0, b: 0);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(OffsetX: 50));

        Assert.Equal(Target.Width, result.Width);
    }

    [Fact]
    public void Adjust_OffsetY_DoesNotThrow()
    {
        using var src = CreateSolidImage(200, 200);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(OffsetY: 40));

        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_CenterCrop_WideImage_OutputIsTargetSize()
    {
        using var src = CreateSolidImage(400, 100);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Center));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_CenterCrop_TallImage_OutputIsTargetSize()
    {
        using var src = CreateSolidImage(100, 400);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Center));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_FitWidthMode_TallImage_FillsWidth()
    {
        using var src = CreateSolidImage(30, 200, r: 255, g: 0, b: 0);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.FitWidth));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);

        var leftPx = result[0, Target.Height / 2];
        var rightPx = result[Target.Width - 1, Target.Height / 2];
        Assert.Equal(255, leftPx.A);
        Assert.Equal(255, rightPx.A);
    }

    [Fact]
    public void Adjust_FitWidthMode_WideImage_HasVerticalTransparentPixels()
    {
        using var src = CreateSolidImage(400, 100);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.FitWidth));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);

        var topPx = result[Target.Width / 2, 0];
        var centerPx = result[Target.Width / 2, Target.Height / 2];
        Assert.Equal(0, topPx.A);
        Assert.Equal(255, centerPx.A);
    }

    [Fact]
    public void Adjust_FitHeightMode_WideImage_FillsHeight()
    {
        using var src = CreateSolidImage(400, 100, r: 255, g: 0, b: 0);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.FitHeight));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);

        var topPx = result[Target.Width / 2, 0];
        var bottomPx = result[Target.Width / 2, Target.Height - 1];
        Assert.Equal(255, topPx.A);
        Assert.Equal(255, bottomPx.A);
    }

    [Fact]
    public void Adjust_FitHeightMode_TallImage_HasHorizontalTransparentPixels()
    {
        using var src = CreateSolidImage(100, 400);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.FitHeight));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);

        var leftPx = result[0, Target.Height / 2];
        var centerPx = result[Target.Width / 2, Target.Height / 2];
        Assert.Equal(0, leftPx.A);
        Assert.Equal(255, centerPx.A);
    }

    [Fact]
    public void Adjust_ExtremelyLargeImage_DoesNotThrow()
    {
        using var src = new Image<Rgba32>(8192, 8192);
        using var result = ImageAdjuster.Adjust(src, new Size(64, 64));

        Assert.Equal(64, result.Width);
    }

    [Fact]
    public void Adjust_1x1Image_DoesNotThrow()
    {
        using var src = CreateSolidImage(1, 1);
        using var result = ImageAdjuster.Adjust(src, Target);

        Assert.Equal(Target.Width, result.Width);
    }

    [Fact]
    public void Adjust_NullParams_UsesDefaults_OutputMatchesTargetSize()
    {
        using var src = CreateSolidImage(100, 100);
        using var result = ImageAdjuster.Adjust(src, Target, null);

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }
}
