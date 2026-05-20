using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Composition;

/// <summary>
/// フォルダテンプレートの領域定義（256x256px 基準座標）。
/// </summary>
public static class FolderTemplate
{
    public const int BaseSize = 256;

    // タグ領域: フォルダ左上のタブ部分（全幅 35%、高さ 18%）
    public static readonly RectangleF TagRegion = new(
        x: 0f,
        y: 0f,
        width: BaseSize * 0.35f,
        height: BaseSize * 0.18f);

    // フォルダ本体: タグタブより下の全体
    public static readonly RectangleF FolderBodyRegion = new(
        x: 0f,
        y: BaseSize * 0.18f,
        width: BaseSize,
        height: BaseSize * 0.82f);

    // 画像表示領域: フォルダ本体内の内側マージン付きエリア
    public static readonly RectangleF ImageRegion = new(
        x: BaseSize * 0.06f,
        y: BaseSize * 0.24f,
        width: BaseSize * 0.88f,
        height: BaseSize * 0.62f);

    private static byte[]? _templateCache;
    private static readonly object _lock = new();

    /// <summary>
    /// フォルダテンプレート PNG バイト列を返す。
    /// EmbeddedResource があればそれを使用し、なければプログラム生成する。
    /// </summary>
    public static byte[] GetTemplateBytes()
    {
        lock (_lock)
        {
            if (_templateCache is not null)
                return _templateCache;

            var asm = typeof(FolderTemplate).Assembly;
            const string resourceName = "Folderly.Core.Resources.FolderTemplate.png";
            using var stream = asm.GetManifestResourceStream(resourceName);

            if (stream is not null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _templateCache = ms.ToArray();
            }
            else
            {
                _templateCache = GenerateTemplatePng();
            }

            return _templateCache;
        }
    }

    /// <summary>
    /// ImageSharp でフォルダ形状の PNG を生成する。
    /// テンプレート PNG が EmbeddedResource にない環境（テスト等）で使用。
    /// </summary>
    public static byte[] GenerateTemplatePng(int size = BaseSize)
    {
        using var image = new Image<Rgba32>(size, size);

        float scale = (float)size / BaseSize;

        // フォルダ色定義
        var tagColor = Color.ParseHex("#E8A030");       // タグタブ（少し暗めの黄色）
        var bodyColor = Color.ParseHex("#F5C842");      // フォルダ本体（黄色）
        var shadowColor = Color.ParseHex("#D4A020");    // 影

        image.Mutate(ctx =>
        {
            ctx.Fill(Color.Transparent);

            float tagW = BaseSize * 0.35f * scale;
            float tagH = BaseSize * 0.18f * scale;
            float bodyY = BaseSize * 0.18f * scale;
            float bodyH = BaseSize * 0.75f * scale;
            float bodyW = size;

            // タグタブ（左上の台形形状）
            var tagPath = new RectangularPolygon(0f, 0f, tagW, tagH);
            ctx.Fill(tagColor, tagPath);

            // フォルダ本体（全幅の矩形）
            var bodyRect = new RectangularPolygon(0f, bodyY, bodyW, bodyH);
            ctx.Fill(bodyColor, bodyRect);

            // 本体上部の影線（タブとの境界）
            var shadowRect = new RectangularPolygon(0f, bodyY, bodyW, 3f * scale);
            ctx.Fill(shadowColor, shadowRect);
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>スケール係数を適用した領域を返す。</summary>
    public static RectangleF ScaleRegion(RectangleF region, float targetSize)
    {
        float scale = targetSize / BaseSize;
        return new RectangleF(
            region.X * scale,
            region.Y * scale,
            region.Width * scale,
            region.Height * scale);
    }
}
