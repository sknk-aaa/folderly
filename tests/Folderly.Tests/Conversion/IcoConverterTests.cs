using Folderly.Core.Conversion;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Folderly.Tests.Conversion;

public class IcoConverterTests
{
    private static Image<Rgba32> CreateTestImage(int size = 256)
    {
        var img = new Image<Rgba32>(size, size);
        img.Mutate(ctx => ctx.BackgroundColor(new Rgba32(0, 120, 212, 255)));
        return img;
    }

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Convert_ReturnsNonEmptyBytes()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Convert_OutputHasValidIcoHeader()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        short reserved = reader.ReadInt16();
        short type = reader.ReadInt16();
        short count = reader.ReadInt16();

        Assert.Equal(0, reserved);
        Assert.Equal(1, type);
        Assert.Equal(4, count); // 16, 32, 48, 256
    }

    [Fact]
    public void Convert_OutputContains4SizeEntries()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        reader.ReadBytes(4); // skip reserved + type
        short count = reader.ReadInt16();

        Assert.Equal(4, count);
    }

    [Fact]
    public void Convert_SmallSizeEntries_HaveCorrectDimensions()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        reader.ReadBytes(6); // skip ICONDIR header

        // 最初のエントリ（16px）
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();

        Assert.Equal(16, width);
        Assert.Equal(16, height);
    }

    [Fact]
    public void Convert_256pxEntry_HasZeroInWidthHeight()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        reader.ReadBytes(6); // ICONDIR
        reader.ReadBytes(16 * 3); // skip first 3 entries (16, 32, 48px)

        // 4番目のエントリ（256px）
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();

        Assert.Equal(0, width);  // ICO 規約: 256 → 0
        Assert.Equal(0, height);
    }

    [Fact]
    public void Convert_OutputContains4PngSignatures()
    {
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        int pngCount = 0;
        for (int i = 0; i <= bytes.Length - PngSignature.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < PngSignature.Length; j++)
            {
                if (bytes[i + j] != PngSignature[j]) { match = false; break; }
            }
            if (match) pngCount++;
        }

        Assert.Equal(4, pngCount); // 4 サイズ分
    }

    [Fact]
    public void Convert_SmallInputImage_DoesNotThrow()
    {
        using var src = CreateTestImage(16);
        var bytes = IcoConverter.Convert(src);

        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Convert_LargeInputImage_DoesNotThrow()
    {
        using var src = CreateTestImage(512);
        var bytes = IcoConverter.Convert(src);

        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Convert_TotalBytesExceedMinimum()
    {
        // ICONDIR(6) + 4*ICONDIRENTRY(16) + 4 PNG files > 100 bytes
        using var src = CreateTestImage();
        var bytes = IcoConverter.Convert(src);

        Assert.True(bytes.Length > 6 + 16 * 4 + 100);
    }
}
