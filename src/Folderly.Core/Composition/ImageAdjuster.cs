using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Composition;

public enum CropMode { Center, FitWidth, FitHeight }

public record ImageAdjustParams(
    float Scale = 1.0f,
    float OffsetX = 0.0f,
    float OffsetY = 0.0f,
    CropMode Mode = CropMode.Center);

/// <summary>
/// Adjusts an input image to the requested target size.
/// </summary>
public static class ImageAdjuster
{
    /// <summary>
    /// Returns a new adjusted image. The caller owns the returned image and must dispose it.
    /// </summary>
    public static Image<Rgba32> Adjust(
        Image sourceImage,
        Size targetSize,
        ImageAdjustParams? parameters = null)
    {
        var p = parameters ?? new ImageAdjustParams();
        return p.Mode switch
        {
            CropMode.FitWidth => ApplyFitWidthMode(sourceImage, targetSize, p.Scale, p.OffsetX, p.OffsetY),
            CropMode.FitHeight => ApplyFitHeightMode(sourceImage, targetSize, p.Scale, p.OffsetX, p.OffsetY),
            _ => ApplyCenterCrop(sourceImage, targetSize, p.Scale, p.OffsetX, p.OffsetY),
        };
    }

    private static Image<Rgba32> ApplyCenterCrop(
        Image source, Size target, float scale, float offsetX, float offsetY)
    {
        float baseScale = Math.Max(
            (float)target.Width / source.Width,
            (float)target.Height / source.Height);
        float effectiveScale = Math.Max(baseScale * scale, 0.001f);

        int resizedW = Math.Max(1, (int)Math.Round(source.Width * effectiveScale));
        int resizedH = Math.Max(1, (int)Math.Round(source.Height * effectiveScale));

        var result = new Image<Rgba32>(target.Width, target.Height);

        var resized = source.Clone(ctx => ctx.Resize(resizedW, resizedH));
        try
        {
            int pasteX = (int)Math.Round((target.Width - resizedW) / 2f + offsetX);
            int pasteY = (int)Math.Round((target.Height - resizedH) / 2f + offsetY);
            DrawClipped(result, resized, pasteX, pasteY);
        }
        finally
        {
            resized.Dispose();
        }

        return result;
    }

    private static Image<Rgba32> ApplyFitWidthMode(
        Image source, Size target, float scale, float offsetX, float offsetY)
    {
        float fitScale = (float)target.Width / source.Width;
        float effectiveScale = Math.Max(fitScale * scale, 0.001f);

        int resizedW = Math.Max(1, (int)Math.Round(source.Width * effectiveScale));
        int resizedH = Math.Max(1, (int)Math.Round(source.Height * effectiveScale));

        int pasteX = (target.Width - resizedW) / 2 + (int)offsetX;
        int pasteY = (target.Height - resizedH) / 2 + (int)offsetY;

        var result = new Image<Rgba32>(target.Width, target.Height);

        var resized = source.Clone(ctx => ctx.Resize(resizedW, resizedH));
        try
        {
            DrawClipped(result, resized, pasteX, pasteY);
        }
        finally
        {
            resized.Dispose();
        }

        return result;
    }

    private static Image<Rgba32> ApplyFitHeightMode(
        Image source, Size target, float scale, float offsetX, float offsetY)
    {
        float fitScale = (float)target.Height / source.Height;
        float effectiveScale = Math.Max(fitScale * scale, 0.001f);

        int resizedW = Math.Max(1, (int)Math.Round(source.Width * effectiveScale));
        int resizedH = Math.Max(1, (int)Math.Round(source.Height * effectiveScale));

        int pasteX = (target.Width - resizedW) / 2 + (int)offsetX;
        int pasteY = (target.Height - resizedH) / 2 + (int)offsetY;

        var result = new Image<Rgba32>(target.Width, target.Height);

        var resized = source.Clone(ctx => ctx.Resize(resizedW, resizedH));
        try
        {
            DrawClipped(result, resized, pasteX, pasteY);
        }
        finally
        {
            resized.Dispose();
        }

        return result;
    }

    private static void DrawClipped(Image<Rgba32> target, Image resized, int pasteX, int pasteY)
    {
        int srcX = 0, srcY = 0;
        int destX = pasteX, destY = pasteY;
        int drawW = resized.Width, drawH = resized.Height;

        if (destX < 0) { srcX = -destX; drawW += destX; destX = 0; }
        if (destY < 0) { srcY = -destY; drawH += destY; destY = 0; }
        if (destX + drawW > target.Width) drawW = target.Width - destX;
        if (destY + drawH > target.Height) drawH = target.Height - destY;

        if (drawW <= 0 || drawH <= 0)
        {
            return;
        }

        var visible = resized.Clone(ctx => ctx.Crop(new Rectangle(srcX, srcY, drawW, drawH)));
        try
        {
            target.Mutate(ctx => ctx.DrawImage(visible, new Point(destX, destY), 1f));
        }
        finally
        {
            visible.Dispose();
        }
    }
}
