using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Composition;

public enum CropMode { Center, Pad }

public record ImageAdjustParams(
    float Scale = 1.0f,
    float OffsetX = 0.0f,
    float OffsetY = 0.0f,
    CropMode Mode = CropMode.Center);

/// <summary>
/// 入力画像を指定した targetSize に合わせて調整する。
/// </summary>
public static class ImageAdjuster
{
    /// <summary>
    /// 入力画像を targetSize にフィットさせた新しい Image を返す。
    /// 呼び出し元が Dispose する責務を持つ。
    /// </summary>
    public static Image<Rgba32> Adjust(
        Image sourceImage,
        Size targetSize,
        ImageAdjustParams? parameters = null)
    {
        var p = parameters ?? new ImageAdjustParams();
        return p.Mode == CropMode.Center
            ? ApplyCenterCrop(sourceImage, targetSize, p.Scale, p.OffsetX, p.OffsetY)
            : ApplyPadMode(sourceImage, targetSize, p.Scale, p.OffsetX, p.OffsetY);
    }

    private static Image<Rgba32> ApplyCenterCrop(
        Image source, Size target, float scale, float offsetX, float offsetY)
    {
        // スケール後のサイズ
        float scaledW = source.Width * scale;
        float scaledH = source.Height * scale;

        // targetSize に対してスケール後画像が収まらない場合の補正スケール
        // (Center モードではスケール後サイズ < target を許容し、その場合はリサイズのみ)
        float baseScale = Math.Max(
            (float)target.Width / source.Width,
            (float)target.Height / source.Height);
        float effectiveScale = baseScale * scale;

        int resizedW = Math.Max(1, (int)Math.Round(source.Width * effectiveScale));
        int resizedH = Math.Max(1, (int)Math.Round(source.Height * effectiveScale));

        // クロップ原点（中央基準 + オフセット）
        float cropOriginX = (resizedW - target.Width) / 2f - offsetX;
        float cropOriginY = (resizedH - target.Height) / 2f - offsetY;

        // クランプ
        int cropX = Math.Clamp((int)cropOriginX, 0, Math.Max(0, resizedW - target.Width));
        int cropY = Math.Clamp((int)cropOriginY, 0, Math.Max(0, resizedH - target.Height));
        int cropW = Math.Min(target.Width, resizedW - cropX);
        int cropH = Math.Min(target.Height, resizedH - cropY);

        var result = new Image<Rgba32>(target.Width, target.Height); // デフォルトで透明

        var resized = source.Clone(ctx => ctx.Resize(resizedW, resizedH));
        try
        {
            var cropped = resized.Clone(ctx => ctx.Crop(
                new Rectangle(cropX, cropY, cropW, cropH)));
            try
            {
                result.Mutate(ctx => ctx.DrawImage(cropped, new Point(0, 0), 1f));
            }
            finally
            {
                cropped.Dispose();
            }
        }
        finally
        {
            resized.Dispose();
        }

        return result;
    }

    private static Image<Rgba32> ApplyPadMode(
        Image source, Size target, float scale, float offsetX, float offsetY)
    {
        // アスペクト比を保持して target に収まる最大サイズを計算
        float fitScale = Math.Min(
            (float)target.Width / source.Width,
            (float)target.Height / source.Height);
        float effectiveScale = fitScale * scale;
        effectiveScale = Math.Max(effectiveScale, 0.001f);

        int resizedW = Math.Max(1, (int)Math.Round(source.Width * effectiveScale));
        int resizedH = Math.Max(1, (int)Math.Round(source.Height * effectiveScale));

        // 中央配置のオフセット
        int pasteX = (target.Width - resizedW) / 2 + (int)offsetX;
        int pasteY = (target.Height - resizedH) / 2 + (int)offsetY;

        var result = new Image<Rgba32>(target.Width, target.Height); // デフォルトで透明

        var resized = source.Clone(ctx => ctx.Resize(resizedW, resizedH));
        try
        {
            // target 外へのはみ出しを防ぐクリッピング
            int srcX = 0, srcY = 0;
            int destX = pasteX, destY = pasteY;
            int drawW = resizedW, drawH = resizedH;

            if (destX < 0) { srcX = -destX; drawW += destX; destX = 0; }
            if (destY < 0) { srcY = -destY; drawH += destY; destY = 0; }
            if (destX + drawW > target.Width) drawW = target.Width - destX;
            if (destY + drawH > target.Height) drawH = target.Height - destY;

            if (drawW > 0 && drawH > 0)
            {
                var visible = resized.Clone(ctx => ctx.Crop(
                    new Rectangle(srcX, srcY, drawW, drawH)));
                try
                {
                    result.Mutate(ctx => ctx.DrawImage(visible, new Point(destX, destY), 1f));
                }
                finally
                {
                    visible.Dispose();
                }
            }
        }
        finally
        {
            resized.Dispose();
        }

        return result;
    }
}
