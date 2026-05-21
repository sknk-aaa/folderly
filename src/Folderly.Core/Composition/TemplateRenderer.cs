using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
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
        int outputSize = FolderTemplate.BaseSize,
        string? tagName = null)
    {
        using var backTemplate = Image.Load<Rgba32>(FolderTemplate.GetBackTemplateBytes());
        backTemplate.Mutate(ctx => ctx.Resize(outputSize, outputSize));

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

            if (tagColor is not null && !tagColor.IsNone)
            {
                ctx.Fill(tagColor.ToImageSharpColor(), FolderTemplate.CreateVisibleTagPath(outputSize));
                DrawTagName(ctx, tagColor, tagName, outputSize);
            }

            ctx.Fill(Color.ParseHex("#FFC72C"), FolderTemplate.CreateImagePath(outputSize));

            using var resizedAdjusted = adjustedImage.Clone(
                c => c.Resize(imageW, imageH));
            ctx.Clip(
                FolderTemplate.CreateImagePath(outputSize),
                clipped => clipped.DrawImage(resizedAdjusted, destPoint, 1f));
        });

        return result;
    }

    private static void DrawTagName(IImageProcessingContext ctx, TagColor tagColor, string? tagName, int outputSize)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return;

        var label = tagName.Trim();
        var bounds = GetTagTextBounds(outputSize);
        if (bounds.Width <= 0f || bounds.Height <= 0f)
            return;

        var font = CreateFittingFont(label, bounds, outputSize, out var measured, out var displayText);
        if (string.IsNullOrWhiteSpace(displayText))
            return;

        var origin = new PointF(
            bounds.X + (bounds.Width - measured.Width) / 2f,
            bounds.Y + (bounds.Height - measured.Height) / 2f - measured.Top);
        ctx.DrawText(displayText, font, GetReadableTextColor(tagColor), origin);
    }

    private static RectangleF GetTagTextBounds(int outputSize)
    {
        var points = FolderTemplate.GetVisibleTagShapePoints(outputSize);
        float padX = outputSize * 0.04f;
        float padY = outputSize * 0.012f;
        float x = points[0].X + padX;
        float y = points[0].Y + padY;
        float right = points[1].X - padX;
        float bottom = points[3].Y - padY;
        return new RectangleF(x, y, right - x, bottom - y);
    }

    private static Font CreateFittingFont(
        string label,
        RectangleF bounds,
        int outputSize,
        out FontRectangle measured,
        out string displayText)
    {
        float maxSize = Math.Max(10f, outputSize * 0.072f);
        float minSize = Math.Max(7f, outputSize * 0.038f);
        var font = CreateFont(maxSize);
        displayText = label;
        measured = Measure(displayText, font);

        for (float size = maxSize; size >= minSize; size -= 0.5f)
        {
            font = CreateFont(size);
            measured = Measure(label, font);
            if (measured.Width <= bounds.Width && measured.Height <= bounds.Height)
            {
                displayText = label;
                return font;
            }
        }

        font = CreateFont(minSize);
        displayText = Ellipsize(label, font, bounds.Width);
        measured = Measure(displayText, font);
        return font;
    }

    private static string Ellipsize(string label, Font font, float maxWidth)
    {
        const string ellipsis = "…";
        if (Measure(label, font).Width <= maxWidth)
            return label;
        if (Measure(ellipsis, font).Width > maxWidth)
            return string.Empty;

        for (int len = label.Length - 1; len > 0; len--)
        {
            var candidate = label[..len] + ellipsis;
            if (Measure(candidate, font).Width <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    private static Font CreateFont(float size)
    {
        foreach (var family in new[] { "Yu Gothic UI", "Meiryo", "Segoe UI" })
        {
            if (SystemFonts.TryGet(family, out var fontFamily))
                return fontFamily.CreateFont(size, FontStyle.Bold);
        }

        return SystemFonts.Collection.Families.First().CreateFont(size, FontStyle.Bold);
    }

    private static FontRectangle Measure(string text, Font font)
        => TextMeasurer.MeasureSize(text, new TextOptions(font));

    private static Color GetReadableTextColor(TagColor tagColor)
    {
        var px = tagColor.ToImageSharpColor().ToPixel<Rgba32>();
        var luminance = (0.299 * px.R + 0.587 * px.G + 0.114 * px.B) / 255.0;
        return luminance > 0.55 ? Color.ParseHex("#1F1F1F") : Color.White;
    }
}
