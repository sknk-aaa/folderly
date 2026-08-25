using Folderly.Core.Composition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Tests.Composition;

public class ImageTransparencyTests
{
    [Fact]
    public void Adjust_TransparentSource_FlattensImageTransparencyToWhite()
    {
        using var src = new Image<Rgba32>(40, 40);
        src.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.Transparent);
            ctx.Fill(Color.Black, new Rectangle(10, 10, 20, 20));
        });

        using var result = ImageAdjuster.Adjust(
            src,
            new Size(80, 80),
            new ImageAdjustParams(Mode: CropMode.Center));

        var transparentArea = result[4, 4];
        Assert.Equal(255, transparentArea.A);
        Assert.Equal(255, transparentArea.R);
        Assert.Equal(255, transparentArea.G);
        Assert.Equal(255, transparentArea.B);

        var filledArea = result[40, 40];
        Assert.Equal(255, filledArea.A);
        Assert.True(filledArea.R < 20);
        Assert.True(filledArea.G < 20);
        Assert.True(filledArea.B < 20);
    }

    [Fact]
    public void Adjust_FitWidth_StillUsesFolderColorForOutsideImageArea()
    {
        using var src = new Image<Rgba32>(400, 100);
        src.Mutate(ctx => ctx.BackgroundColor(Color.Black));

        using var result = ImageAdjuster.Adjust(
            src,
            new Size(160, 120),
            new ImageAdjustParams(Mode: CropMode.FitWidth));

        var topGap = result[80, 0];
        Assert.Equal(255, topGap.A);
        Assert.Equal(255, topGap.R);
        Assert.Equal(199, topGap.G);
        Assert.Equal(44, topGap.B);
    }
}
