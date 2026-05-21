using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Composition;

/// <summary>
/// Renders the final folder icon from the folder template layers, adjusted image, and tag color.
/// </summary>
public static class TemplateRenderer
{
    public static Image<Rgba32> Render(
        Image adjustedImage,
        TagColor? tagColor,
        int outputSize = FolderTemplate.BaseSize)
    {
        using var backTemplate = Image.Load<Rgba32>(FolderTemplate.GetBackTemplateBytes());
        backTemplate.Mutate(ctx => ctx.Resize(outputSize, outputSize));

        using var frontTemplate = Image.Load<Rgba32>(FolderTemplate.GetFrontTemplateBytes());
        frontTemplate.Mutate(ctx => ctx.Resize(outputSize, outputSize));

        var scaledImageRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, outputSize);

        var result = backTemplate.Clone();

        result.Mutate(ctx =>
        {
            var imageW = (int)scaledImageRegion.Width;
            var imageH = (int)scaledImageRegion.Height;
            var destPoint = new Point(
                (int)scaledImageRegion.X,
                (int)scaledImageRegion.Y);

            using var resizedAdjusted = adjustedImage.Clone(
                c => c.Resize(imageW, imageH));
            ctx.DrawImage(resizedAdjusted, destPoint, 1f);

            if (tagColor is not null && !tagColor.IsNone)
                ctx.Fill(tagColor.ToImageSharpColor(), FolderTemplate.CreateTabPath(outputSize));

            ctx.DrawImage(frontTemplate, new Point(0, 0), 1f);
        });

        return result;
    }
}
