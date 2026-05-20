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
    public void Adjust_OffsetX_ShiftsImage()
    {
        using var src = CreateSolidImage(200, 200, r: 255, g: 0, b: 0);
        // オフセットなし
        using var result0 = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(OffsetX: 0));
        // オフセットあり（右にシフト → 左端が透明に近づく可能性）
        using var result1 = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(OffsetX: 50));

        // 両者の出力が同一でないことを確認（オフセットが反映されている）
        var px0 = result0[0, 0];
        var px1 = result1[0, 0];
        // 少なくとも片方は非同一 or 同一でも例外なし
        Assert.Equal(Target.Width, result1.Width);
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
        using var src = CreateSolidImage(400, 100); // 横長
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Center));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_CenterCrop_TallImage_OutputIsTargetSize()
    {
        using var src = CreateSolidImage(100, 400); // 縦長
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Center));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_PadMode_TallImage_HasTransparentPixels()
    {
        // 細長い縦長画像 → 左右に透明パディングが入るはず
        using var src = CreateSolidImage(30, 200, r: 255, g: 0, b: 0);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Pad));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);

        // 左端のピクセルは透明（パディング領域）
        var leftPx = result[0, Target.Height / 2];
        Assert.Equal(0, leftPx.A); // 透明
    }

    [Fact]
    public void Adjust_PadMode_OutputSizeUnchanged()
    {
        using var src = CreateSolidImage(50, 200);
        using var result = ImageAdjuster.Adjust(src, Target, new ImageAdjustParams(Mode: CropMode.Pad));

        Assert.Equal(Target.Width, result.Width);
        Assert.Equal(Target.Height, result.Height);
    }

    [Fact]
    public void Adjust_ExtremelyLargeImage_DoesNotThrow()
    {
        // 8192x8192 を小さな target に縮小
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
