using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Composition;

/// <summary>
/// フォルダテンプレートに調整済み画像とタグ色を合成する。
/// プレビュー用と最終 ico 用で同一ロジックを使用する。
/// </summary>
public static class TemplateRenderer
{
    /// <summary>
    /// フォルダ型の合成画像を生成して返す。呼び出し元が Dispose する責務を持つ。
    /// </summary>
    /// <param name="adjustedImage">ImageAdjuster で調整済みの画像（ImageRegion サイズ）</param>
    /// <param name="tagColor">タグ色（null または IsNone でタグなし）</param>
    /// <param name="outputSize">出力正方形サイズ（プレビュー時 320 等、ico 最終 256）</param>
    public static Image<Rgba32> Render(
        Image adjustedImage,
        TagColor? tagColor,
        int outputSize = FolderTemplate.BaseSize)
    {
        // テンプレート PNG を outputSize にリサイズ
        var templateBytes = FolderTemplate.GetTemplateBytes();
        using var template = Image.Load<Rgba32>(templateBytes);
        template.Mutate(ctx => ctx.Resize(outputSize, outputSize));

        // ImageRegion をスケール変換
        var scaledImageRegion = FolderTemplate.ScaleRegion(
            FolderTemplate.ImageRegion, outputSize);

        // 結果キャンバス（テンプレートのコピー）
        var result = template.Clone();

        result.Mutate(ctx =>
        {
            // 画像表示領域に adjustedImage を描画
            // adjustedImage はすでに ImageRegion サイズに調整済みなので
            // そのまま scaledImageRegion 座標に配置
            var destPoint = new Point(
                (int)scaledImageRegion.X,
                (int)scaledImageRegion.Y);

            // adjustedImage を scaledImageRegion サイズにリサイズして貼り付け
            var imageW = (int)scaledImageRegion.Width;
            var imageH = (int)scaledImageRegion.Height;

            using var resizedAdjusted = adjustedImage.Clone(
                c => c.Resize(imageW, imageH));
            ctx.DrawImage(resizedAdjusted, destPoint, 1f);

            // タグ色をフォルダ左上のタブ形状に上書き描画（タグなしの場合はスキップ）
            if (tagColor is not null && !tagColor.IsNone)
                ctx.Fill(tagColor.ToImageSharpColor(), FolderTemplate.CreateTabPath(outputSize));
        });

        return result;
    }
}
