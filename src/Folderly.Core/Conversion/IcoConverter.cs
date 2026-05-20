using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Core.Conversion;

/// <summary>
/// 合成済み画像をマルチサイズ .ico バイト列に変換する。
/// 全サイズ PNG 埋め込み方式（Windows Vista 以降対応）。
/// </summary>
public static class IcoConverter
{
    private static readonly int[] IcoSizes = [16, 32, 48, 256];

    /// <summary>
    /// 入力画像からマルチサイズ .ico バイト列を生成する。
    /// </summary>
    public static byte[] Convert(Image<Rgba32> sourceImage)
    {
        var pngEntries = IcoSizes.Select(size => CreatePngEntry(sourceImage, size)).ToArray();
        return BuildIcoFile(pngEntries);
    }

    private static byte[] CreatePngEntry(Image<Rgba32> source, int size)
    {
        using var resized = source.Clone(ctx =>
        {
            ctx.Resize(size, size);
            // 小サイズではシャープネスを適用してタグを視認しやすくする
            if (size <= 32)
                ctx.GaussianSharpen(1.0f);
        });

        using var ms = new MemoryStream();
        resized.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static byte[] BuildIcoFile(byte[][] pngEntries)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // ICONDIR ヘッダー（6 bytes）
        writer.Write((short)0);                        // idReserved = 0
        writer.Write((short)1);                        // idType = 1 (ICO)
        writer.Write((short)IcoSizes.Length);          // idCount

        // 各エントリのデータオフセットを計算
        int dataOffset = 6 + 16 * IcoSizes.Length;

        // ICONDIRENTRY × N（各 16 bytes）
        for (int i = 0; i < IcoSizes.Length; i++)
        {
            WriteIconDirEntry(writer, IcoSizes[i], pngEntries[i].Length, dataOffset);
            dataOffset += pngEntries[i].Length;
        }

        // PNG データ本体
        foreach (var pngData in pngEntries)
            writer.Write(pngData);

        return ms.ToArray();
    }

    private static void WriteIconDirEntry(
        BinaryWriter writer, int size, int dataLength, int dataOffset)
    {
        // 256px は ICO 規約で Width/Height バイトを 0 とする
        writer.Write((byte)(size == 256 ? 0 : size)); // Width
        writer.Write((byte)(size == 256 ? 0 : size)); // Height
        writer.Write((byte)0);                         // ColorCount（32bpp では 0）
        writer.Write((byte)0);                         // Reserved
        writer.Write((short)1);                        // Planes
        writer.Write((short)32);                       // BitCount（32bpp）
        writer.Write(dataLength);                      // BytesInRes
        writer.Write(dataOffset);                      // ImageOffset
    }
}
