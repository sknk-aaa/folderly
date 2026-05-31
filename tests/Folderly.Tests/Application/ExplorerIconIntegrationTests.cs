using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Folderly.Core.Application;
using Folderly.Core.Composition;
using Folderly.Core.Folder;
using Folderly.Core.History;
using Folderly.Shell;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit.Abstractions;

namespace Folderly.Tests.Application;

public sealed class ExplorerIconIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ITestOutputHelper _output;

    public ExplorerIconIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "FolderlyCertificationVerification",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ApplyAsync_LocalFolder_ShellResolvesUpdatedIconOnApplyAndReapply()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var targetFolder = Path.Combine(_tempDir, "StandardLocalFolder");
        Directory.CreateDirectory(targetFolder);
        File.WriteAllText(Path.Combine(targetFolder, "ordinary-user-file.txt"), "standard local folder");
        var dataDir = Path.Combine(_tempDir, "LocalAppData");

        using var history = new HistoryRepository(":memory:");
        var service = new ApplyService(history, new ShellNotifier(), folderlyDataDir: dataDir);

        var defaultIconHash = GetShellIconHash(targetFolder);

        using (var source = CreateTestImageStream(5, 70, 240))
        {
            var firstResult = await service.ApplyAsync(MakeRequest(targetFolder, source, TagColors.Blue));
            Assert.True(firstResult.IsSuccess);
        }

        var firstIconPath = AssertCustomizationFiles(targetFolder);
        var expectedFirstIconHash = GetShellIconHash(firstIconPath);
        var firstIconHash = await WaitForShellIconAsync(targetFolder, expectedFirstIconHash);

        using (var source = CreateTestImageStream(244, 42, 30))
        {
            var secondResult = await service.ApplyAsync(MakeRequest(targetFolder, source, TagColors.Red));
            Assert.True(secondResult.IsSuccess);
        }

        var secondIconPath = AssertCustomizationFiles(targetFolder);
        var expectedSecondIconHash = GetShellIconHash(secondIconPath);
        var secondIconHash = await WaitForShellIconAsync(targetFolder, expectedSecondIconHash);

        Assert.NotEqual(defaultIconHash, firstIconHash);
        Assert.NotEqual(firstIconHash, secondIconHash);
        _output.WriteLine(
            $"Shell icon hashes: default={defaultIconHash}, first={firstIconHash}, second={secondIconHash}");
    }

    [Fact]
    public async Task ApplyAsync_LocalFolder_ShellApiPreservesExistingDesktopIniSettings()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var targetFolder = Path.Combine(_tempDir, "FolderWithExistingSettings");
        Directory.CreateDirectory(targetFolder);
        DesktopIniManager.WriteRaw(
            targetFolder,
            "[.ShellClassInfo]\r\nInfoTip=Keep this existing setting\r\n");

        using var history = new HistoryRepository(":memory:");
        var service = new ApplyService(
            history,
            new ShellNotifier(),
            folderlyDataDir: Path.Combine(_tempDir, "LocalAppData"));

        using var source = CreateTestImageStream(5, 70, 240);
        var result = await service.ApplyAsync(MakeRequest(targetFolder, source, TagColors.Blue));

        Assert.True(result.IsSuccess);
        var updatedIni = DesktopIniManager.Read(targetFolder);
        Assert.Contains("InfoTip=Keep this existing setting", updatedIni);
        Assert.Contains(@"IconResource=_folderly\cover_", updatedIni);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
            return;

        foreach (var file in Directory.EnumerateFiles(_tempDir, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        foreach (var dir in Directory.EnumerateDirectories(_tempDir, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
            File.SetAttributes(dir, FileAttributes.Normal);
        File.SetAttributes(_tempDir, FileAttributes.Normal);
        Directory.Delete(_tempDir, recursive: true);
    }

    private static ApplyRequest MakeRequest(string folderPath, Stream source, TagColor tagColor)
        => new(
            FolderPath: folderPath,
            SourceImageStream: source,
            SourceImagePath: string.Empty,
            AdjustParams: new ImageAdjustParams(),
            TagColor: tagColor);

    private static Stream CreateTestImageStream(byte r, byte g, byte b)
    {
        using var image = new Image<Rgba32>(192, 192);
        image.Mutate(context => context.BackgroundColor(new Rgba32(r, g, b, 255)));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    private static string AssertCustomizationFiles(string folderPath)
    {
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        var folderlyPath = Path.Combine(folderPath, "_folderly");
        var localIcons = Directory.GetFiles(folderlyPath, "cover_*.ico");
        var iniContent = DesktopIniManager.Read(folderPath);
        var folderAttrs = File.GetAttributes(folderPath);
        var iniAttrs = File.GetAttributes(desktopIniPath);

        var referencedIcon = Assert.Single(
            localIcons,
            localIcon => iniContent!.Contains(
                $@"IconResource=_folderly\{Path.GetFileName(localIcon)},0",
                StringComparison.OrdinalIgnoreCase));
        Assert.True(folderAttrs.HasFlag(FileAttributes.ReadOnly));
        Assert.True(iniAttrs.HasFlag(FileAttributes.Hidden));
        Assert.True(iniAttrs.HasFlag(FileAttributes.System));

        return referencedIcon;
    }

    private static async Task<string> WaitForShellIconAsync(string folderPath, string expectedHash)
    {
        var timeout = DateTime.UtcNow.AddSeconds(8);
        var currentHash = GetShellIconHash(folderPath);

        while (DateTime.UtcNow < timeout)
        {
            currentHash = GetShellIconHash(folderPath);
            if (string.Equals(currentHash, expectedHash, StringComparison.Ordinal))
                return currentHash;

            await Task.Delay(200);
        }

        Assert.Equal(expectedHash, currentHash);
        return currentHash;
    }

    private static string GetShellIconHash(string folderPath)
    {
        var info = new ShFileInfo();
        var result = SHGetFileInfo(
            folderPath,
            0,
            ref info,
            (uint)Marshal.SizeOf<ShFileInfo>(),
            ShgfiIcon | ShgfiLargeIcon);
        Assert.NotEqual(nint.Zero, result);
        Assert.NotEqual(nint.Zero, info.IconHandle);

        try
        {
            using var icon = (Icon)Icon.FromHandle(info.IconHandle).Clone();
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
        }
        finally
        {
            DestroyIcon(info.IconHandle);
        }
    }

    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public nint IconHandle;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHGetFileInfo(
        string path,
        uint fileAttributes,
        ref ShFileInfo info,
        uint infoSize,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint iconHandle);
}
