using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

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
        string? tagName = null,
        int tagIconIndex = -1,
        bool showTagIcon = false)
    {
        using var backTemplate = Image.Load<Rgba32>(FolderTemplate.GetBackTemplateBytes());
        backTemplate.Mutate(ctx => ctx.Resize(outputSize, outputSize));

        var result = backTemplate.Clone();

        result.Mutate(ctx =>
        {
            var imageRegionSize = FolderTemplate.GetImageRegionPixelSize(outputSize);
            var imageW = imageRegionSize.Width;
            var imageH = imageRegionSize.Height;
            var destPoint = FolderTemplate.GetImageRegionPixelOrigin(outputSize);

            ctx.Fill(Color.ParseHex(FolderTemplate.FolderColorHex), FolderTemplate.CreateImagePath(outputSize));

            if (tagColor is not null && !tagColor.IsNone)
            {
                ctx.Fill(tagColor.ToImageSharpColor(), FolderTemplate.CreateVisibleTagPath(outputSize));
                DrawTagContent(ctx, tagColor, tagName, tagIconIndex, showTagIcon, outputSize);
            }

            using var resizedAdjusted = adjustedImage.Clone(
                c => c.Resize(imageW + 2, imageH + 2));
            ctx.Clip(
                FolderTemplate.CreateImagePath(outputSize),
                clipped => clipped.DrawImage(resizedAdjusted, new Point(destPoint.X - 1, destPoint.Y - 1), 1f));
        });

        return result;
    }

    private static void DrawTagContent(
        IImageProcessingContext ctx,
        TagColor tagColor,
        string? tagName,
        int tagIconIndex,
        bool showTagIcon,
        int outputSize)
    {
        var label = tagName?.Trim();
        var hasText = !string.IsNullOrWhiteSpace(label);
        var hasIcon = showTagIcon && TagIconLibrary.IsValidIndex(tagIconIndex);
        if (!hasText && !hasIcon)
            return;

        var bounds = GetTagTextBounds(outputSize);
        if (bounds.Width <= 0f || bounds.Height <= 0f)
            return;

        var color = GetReadableTextColor(tagColor);
        if (hasIcon)
        {
            var iconBounds = GetTagIconBounds(bounds, hasText);
            DrawTagIcon(ctx, tagIconIndex, color, iconBounds);
            if (hasText)
            {
                var gap = outputSize * 0.018f;
                var left = iconBounds.Right + gap;
                bounds = new RectangleF(
                    left,
                    bounds.Y,
                    Math.Max(1f, bounds.Right - left),
                    bounds.Height);
            }
        }

        if (hasText)
            DrawTagName(ctx, color, label!, bounds, outputSize);
    }

    private static RectangleF GetTagIconBounds(RectangleF bounds, bool hasText)
    {
        var size = Math.Min(bounds.Height * 0.72f, bounds.Width * (hasText ? 0.22f : 0.45f));
        var x = hasText
            ? bounds.X
            : bounds.X + (bounds.Width - size) / 2f;
        var y = bounds.Y + (bounds.Height - size) / 2f;
        return new RectangleF(x, y, size, size);
    }

    private static void DrawTagName(
        IImageProcessingContext ctx,
        Color textColor,
        string label,
        RectangleF bounds,
        int outputSize)
    {
        var font = CreateFittingFont(label, bounds, outputSize, out var measured, out var displayText);
        if (string.IsNullOrWhiteSpace(displayText))
            return;

        var origin = new PointF(
            bounds.X + (bounds.Width - measured.Width) / 2f,
            bounds.Y + (bounds.Height - measured.Height) / 2f - measured.Top);
        ctx.DrawText(displayText, font, textColor, origin);
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

    private static void DrawTagIcon(IImageProcessingContext ctx, int iconIndex, Color color, RectangleF bounds)
    {
        if (iconIndex == 5)
        {
            var cy = bounds.Y + bounds.Height / 2f;
            var r = bounds.Width * 0.09f;
            foreach (var xFactor in new[] { 0.26f, 0.5f, 0.74f })
                ctx.Fill(color, CreateCirclePolygon(bounds.X + bounds.Width * xFactor, cy, r));
            return;
        }

        var pathData = TagIconLibrary.IconPaths[iconIndex];
        if (string.IsNullOrWhiteSpace(pathData))
            return;

        var path = BuildIconPath(pathData, bounds);
        ctx.Draw(color, Math.Max(1f, bounds.Width * 0.075f), path);
    }

    private static IPath CreateCirclePolygon(float cx, float cy, float r)
    {
        var points = new PointF[16];
        for (var i = 0; i < points.Length; i++)
        {
            var a = MathF.PI * 2f * i / points.Length;
            points[i] = new PointF(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
        }
        var builder = new PathBuilder();
        builder.AddLines(points);
        builder.CloseFigure();
        return builder.Build();
    }

    private static IPath BuildIconPath(string data, RectangleF bounds)
    {
        var tokens = TokenizePathData(data);
        var builder = new PathBuilder();
        var i = 0;
        var cmd = ' ';
        var current = new PointF(0, 0);

        while (i < tokens.Count)
        {
            if (IsCommand(tokens[i]))
                cmd = tokens[i++][0];
            if (cmd == ' ')
                break;

            switch (cmd)
            {
                case 'M':
                    current = ReadPoint(tokens, ref i, bounds);
                    builder.MoveTo(current);
                    cmd = 'L';
                    break;
                case 'L':
                    current = ReadPoint(tokens, ref i, bounds);
                    builder.LineTo(current);
                    break;
                case 'H':
                    current = new PointF(MapX(ReadFloat(tokens, ref i), bounds), current.Y);
                    builder.LineTo(current);
                    break;
                case 'V':
                    current = new PointF(current.X, MapY(ReadFloat(tokens, ref i), bounds));
                    builder.LineTo(current);
                    break;
                case 'A':
                {
                    var rx = ReadFloat(tokens, ref i) * bounds.Width / 24f;
                    var ry = ReadFloat(tokens, ref i) * bounds.Height / 24f;
                    var rotation = ReadFloat(tokens, ref i);
                    var largeArc = ReadFloat(tokens, ref i) != 0f;
                    var sweep = ReadFloat(tokens, ref i) != 0f;
                    current = ReadPoint(tokens, ref i, bounds);
                    builder.ArcTo(rx, ry, rotation, largeArc, sweep, current);
                    break;
                }
                case 'Z':
                case 'z':
                    builder.CloseFigure();
                    cmd = ' ';
                    break;
                default:
                    i++;
                    break;
            }
        }

        return builder.Build();
    }

    private static List<string> TokenizePathData(string data)
    {
        var tokens = new List<string>();
        for (var i = 0; i < data.Length;)
        {
            var ch = data[i];
            if (char.IsWhiteSpace(ch) || ch == ',')
            {
                i++;
                continue;
            }
            if (char.IsLetter(ch))
            {
                tokens.Add(ch.ToString());
                i++;
                continue;
            }

            var start = i;
            i++;
            while (i < data.Length)
            {
                ch = data[i];
                if (char.IsWhiteSpace(ch) || ch == ',' || char.IsLetter(ch))
                    break;
                if ((ch == '-' || ch == '+') && data[i - 1] != 'e' && data[i - 1] != 'E')
                    break;
                i++;
            }
            tokens.Add(data[start..i]);
        }
        return tokens;
    }

    private static bool IsCommand(string token)
        => token.Length == 1 && char.IsLetter(token[0]);

    private static PointF ReadPoint(IReadOnlyList<string> tokens, ref int i, RectangleF bounds)
    {
        var x = ReadFloat(tokens, ref i);
        var y = ReadFloat(tokens, ref i);
        return new PointF(MapX(x, bounds), MapY(y, bounds));
    }

    private static float ReadFloat(IReadOnlyList<string> tokens, ref int i)
        => float.Parse(tokens[i++], CultureInfo.InvariantCulture);

    private static float MapX(float x, RectangleF bounds)
        => bounds.X + x / 24f * bounds.Width;

    private static float MapY(float y, RectangleF bounds)
        => bounds.Y + y / 24f * bounds.Height;
}
