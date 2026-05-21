using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace Folderly.Core.Composition;

/// <summary>
/// Defines the 256x256 folder icon geometry and provides the embedded template layers.
/// </summary>
public static class FolderTemplate
{
    public const int BaseSize = 256;
    public const float TabTopWidthRatio = 0.36f;
    public const float TabSlopeEndRatio = 0.48f;
    public const float TagHeightRatio = 0.18f;

    public static readonly RectangleF TagRegion = new(
        x: 0f,
        y: 0f,
        width: BaseSize * TabSlopeEndRatio,
        height: BaseSize * TagHeightRatio);

    public static readonly RectangleF FolderBodyRegion = new(
        x: 0f,
        y: BaseSize * 0.18f,
        width: BaseSize,
        height: BaseSize * 0.78f);

    public static readonly RectangleF ImageRegion = new(
        x: BaseSize * 0.025f,
        y: BaseSize * 0.205f,
        width: BaseSize * 0.95f,
        height: BaseSize * 0.715f);

    public static readonly RectangleF FrontPocketRegion = new(
        x: 0f,
        y: BaseSize * 0.58f,
        width: BaseSize,
        height: BaseSize * 0.38f);

    private static byte[]? _backTemplateCache;
    private static byte[]? _frontTemplateCache;
    private static readonly object _lock = new();

    /// <summary>
    /// Returns the back folder layer. Kept as the historical API used by tests and previews.
    /// </summary>
    public static byte[] GetTemplateBytes()
        => GetBackTemplateBytes();

    public static byte[] GetBackTemplateBytes()
    {
        lock (_lock)
        {
            _backTemplateCache ??= LoadEmbeddedPng(
                "Folderly.Core.Resources.FolderTemplate.png",
                () => GenerateTemplatePng());
            return _backTemplateCache;
        }
    }

    public static byte[] GetFrontTemplateBytes()
    {
        lock (_lock)
        {
            _frontTemplateCache ??= LoadEmbeddedPng(
                "Folderly.Core.Resources.FolderFrontTemplate.png",
                () => GenerateFrontTemplatePng());
            return _frontTemplateCache;
        }
    }

    public static byte[] GenerateTemplatePng(int size = BaseSize)
    {
        using var image = new Image<Rgba32>(size, size);
        float scale = (float)size / BaseSize;
        float bodyTop = FolderBodyRegion.Y * scale;
        float bodyH   = FolderBodyRegion.Height * scale;

        image.Mutate(ctx =>
        {
            ctx.Fill(Color.Transparent);

            // Tab (slightly darker than body)
            ctx.Fill(Color.ParseHex("#1A2240"), CreateTabPath(size));

            // Folder body — subtle two-tone (top lighter, bottom darker)
            ctx.Fill(Color.ParseHex("#273062"),
                new RectangularPolygon(0f, bodyTop, size, bodyH * 0.5f));
            ctx.Fill(Color.ParseHex("#222B58"),
                new RectangularPolygon(0f, bodyTop + bodyH * 0.5f, size, bodyH * 0.5f));

            // Accent line at top of body (separates tab from body visually)
            ctx.Fill(Color.ParseHex("#4A6DC8"),
                new RectangularPolygon(0f, bodyTop, size, 2.5f * scale));
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static byte[] GenerateFrontTemplatePng(int size = BaseSize)
    {
        using var image = new Image<Rgba32>(size, size);
        float scale = (float)size / BaseSize;
        float bodyBottom = (FolderBodyRegion.Y + FolderBodyRegion.Height) * scale;

        image.Mutate(ctx =>
        {
            ctx.Fill(Color.Transparent);

            // Thin bottom edge only — no pocket overlay
            ctx.Fill(Color.ParseHex("#0D1525"),
                new RectangularPolygon(0f, bodyBottom - 4f * scale, size, 4f * scale));
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static RectangleF ScaleRegion(RectangleF region, float targetSize)
    {
        float scale = targetSize / BaseSize;
        return new RectangleF(
            region.X * scale,
            region.Y * scale,
            region.Width * scale,
            region.Height * scale);
    }

    public static PointF[] GetTabShapePoints(float targetSize)
    {
        float scale = targetSize / BaseSize;
        float tabTopW = BaseSize * TabTopWidthRatio * scale;
        float tabEndX = BaseSize * TabSlopeEndRatio * scale;
        float tabH = BaseSize * TagHeightRatio * scale;

        return
        [
            new PointF(0f, 0f),
            new PointF(tabTopW, 0f),
            new PointF(tabEndX, tabH),
            new PointF(0f, tabH),
        ];
    }

    public static IPath CreateTabPath(float targetSize)
    {
        var points = GetTabShapePoints(targetSize);
        float radius = Math.Min(targetSize * 0.035f, points[2].Y * 0.45f);

        var builder = new PathBuilder();
        builder.MoveTo(new PointF(0f, points[2].Y));
        builder.LineTo(new PointF(0f, radius));
        builder.QuadraticBezierTo(Vector2.Zero, new Vector2(radius, 0f));
        builder.LineTo(points[1]);
        builder.LineTo(points[2]);
        builder.CloseFigure();
        return builder.Build();
    }

    private static byte[] LoadEmbeddedPng(string resourceName, Func<byte[]> fallback)
    {
        var asm = typeof(FolderTemplate).Assembly;
        using var stream = asm.GetManifestResourceStream(resourceName);

        if (stream is null)
            return fallback();

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
