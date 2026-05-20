using SixLabors.ImageSharp;

namespace Folderly.Core.Composition;

/// <summary>
/// タグ色プリセット定義（SPEC.md F-05 準拠、7 種類）。
/// </summary>
public static class TagColors
{
    public static readonly TagColor None   = new(null,       "none");
    public static readonly TagColor Blue   = new("#0078D4",  "blue");
    public static readonly TagColor Green  = new("#107C10",  "green");
    public static readonly TagColor Orange = new("#D83B01",  "orange");
    public static readonly TagColor Purple = new("#8764B8",  "purple");
    public static readonly TagColor Red    = new("#C42B1C",  "red");
    public static readonly TagColor Gray   = new("#7A7574",  "gray");

    public static readonly IReadOnlyList<TagColor> All = [None, Blue, Green, Orange, Purple, Red, Gray];

    public static TagColor? FromKey(string key)
        => All.FirstOrDefault(t => t.Key == key);
}

/// <summary>
/// 単一のタグ色。HexColor が null の場合はタグなし（テンプレート標準色を維持）。
/// </summary>
public record TagColor(string? HexColor, string Key)
{
    public bool IsNone => HexColor is null;

    public Color ToImageSharpColor()
    {
        if (HexColor is null) return Color.Transparent;
        return Color.ParseHex(HexColor);
    }
}
