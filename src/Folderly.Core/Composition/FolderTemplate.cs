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
    public const float TabXRatio = 0.047f;
    public const float TabYRatio = 0.094f;
    public const float TabTopWidthRatio = 0.37f;
    public const float TabSlopeEndRatio = 0.49f;
    public const float TagHeightRatio = 0.24f;
    public const float ImageCornerRadiusRatio = 0.043f;

    public static readonly RectangleF TagRegion = new(
        x: BaseSize * TabXRatio,
        y: BaseSize * TabYRatio,
        width: BaseSize * TabSlopeEndRatio,
        height: BaseSize * TagHeightRatio);

    public static readonly RectangleF FolderBodyRegion = new(
        x: 0f,
        y: BaseSize * 0.215f,
        width: BaseSize,
        height: BaseSize * 0.755f);

    public static readonly RectangleF ImageRegion = new(
        x: BaseSize * 0.039f,
        y: BaseSize * 0.32f,
        width: BaseSize * 0.922f,
        height: BaseSize * 0.648f);

    private static byte[]? _backTemplateCache;
    private static byte[]? _frontTemplateCache;
    private static readonly object _lock = new();

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

        image.Mutate(ctx =>
        {
            ctx.Fill(Color.Transparent);

            var folderColor = Color.ParseHex("#FFC72C");
            ctx.Fill(folderColor, CreateFolderBackPath(size));
            ctx.Fill(folderColor, CreateTabPath(size));
            ctx.Fill(folderColor, CreateImagePath(size));
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static byte[] GenerateFrontTemplatePng(int size = BaseSize)
    {
        using var image = new Image<Rgba32>(size, size);
        image.Mutate(ctx => ctx.Fill(Color.Transparent));

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
        float tabX = BaseSize * TabXRatio * scale;
        float tabY = BaseSize * TabYRatio * scale;
        float tabTopW = BaseSize * TabTopWidthRatio * scale;
        float tabEndX = BaseSize * TabSlopeEndRatio * scale;
        float tabH = BaseSize * TagHeightRatio * scale;

        return
        [
            new PointF(tabX, tabY),
            new PointF(tabX + tabTopW, tabY),
            new PointF(tabX + tabEndX, tabY + tabH),
            new PointF(tabX, tabY + tabH),
        ];
    }

    public static IPath CreateTabPath(float targetSize)
    {
        var points = GetTabShapePoints(targetSize);
        float radius = Math.Min(targetSize * 0.035f, (points[3].Y - points[0].Y) * 0.45f);

        var builder = new PathBuilder();
        builder.MoveTo(points[3]);
        builder.LineTo(new PointF(points[0].X, points[0].Y + radius));
        builder.QuadraticBezierTo(
            new Vector2(points[0].X, points[0].Y),
            new Vector2(points[0].X + radius, points[0].Y));
        builder.LineTo(points[1]);
        builder.LineTo(points[2]);
        builder.CloseFigure();
        return builder.Build();
    }

    public static IPath CreateImagePath(float targetSize)
    {
        var region = ScaleRegion(ImageRegion, targetSize);
        float radius = BaseSize * ImageCornerRadiusRatio * (targetSize / BaseSize);
        return CreateRoundedRectanglePath(region, radius);
    }

    public static IPath CreateFolderBackPath(float targetSize)
    {
        float scale = targetSize / BaseSize;
        var region = new RectangleF(
            BaseSize * 0.44f * scale,
            BaseSize * 0.25f * scale,
            BaseSize * 0.52f * scale,
            BaseSize * 0.22f * scale);
        float radius = BaseSize * 0.04f * scale;
        return CreateRoundedRectanglePath(region, radius);
    }

    private static IPath CreateRoundedRectanglePath(RectangleF region, float radius)
    {
        float x = region.X;
        float y = region.Y;
        float right = region.Right;
        float bottom = region.Bottom;
        float r = Math.Min(radius, Math.Min(region.Width, region.Height) / 2f);

        var builder = new PathBuilder();
        builder.MoveTo(new PointF(x + r, y));
        builder.LineTo(new PointF(right - r, y));
        builder.QuadraticBezierTo(new Vector2(right, y), new Vector2(right, y + r));
        builder.LineTo(new PointF(right, bottom - r));
        builder.QuadraticBezierTo(new Vector2(right, bottom), new Vector2(right - r, bottom));
        builder.LineTo(new PointF(x + r, bottom));
        builder.QuadraticBezierTo(new Vector2(x, bottom), new Vector2(x, bottom - r));
        builder.LineTo(new PointF(x, y + r));
        builder.QuadraticBezierTo(new Vector2(x, y), new Vector2(x + r, y));
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
