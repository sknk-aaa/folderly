using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.Core.Composition;
using Folderly.Core.History;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Folderly.App.ViewModels;

/// <summary>管理画面の ViewModel（SPEC Section 4.2, F-11, F-13）。</summary>
public sealed class MainViewModel : ViewModelBase
{
    public LocalizationService L => LocalizationService.Instance;

    // ─── 履歴一覧 ─────────────────────────────────────────────────────────────

    public ObservableCollection<HistoryItemViewModel> Items { get; } = [];

    private bool _hasItems;
    public bool HasItems
    {
        get => _hasItems;
        private set => SetField(ref _hasItems, value);
    }

    // ─── 試用版バナー ─────────────────────────────────────────────────────────

    private bool _showTrialBanner;
    public bool ShowTrialBanner
    {
        get => _showTrialBanner;
        set => SetField(ref _showTrialBanner, value);
    }

    private string _trialText = string.Empty;
    public string TrialText
    {
        get => _trialText;
        set => SetField(ref _trialText, value);
    }

    // ─── 初期化 ───────────────────────────────────────────────────────────────

    public void Refresh()
    {
        Items.Clear();
        var entries = AppServices.History.GetAll();
        foreach (var entry in entries)
            Items.Add(new HistoryItemViewModel(entry));
        HasItems = Items.Count > 0;
    }

    public void RefreshLicense()
    {
        var lic = AppServices.License;
        ShowTrialBanner = lic.IsTrial && lic.IsActive;
        TrialText = string.Format(L["TrialBanner"], lic.DaysRemaining);
    }
}

/// <summary>履歴一覧の 1 行分を表す ViewModel。</summary>
public sealed class HistoryItemViewModel : ViewModelBase
{
    public HistoryEntry Entry { get; }

    public string FolderPath => Entry.FolderPath;
    public string AppliedAtText => Entry.AppliedAt.ToLocalTime().ToString("yyyy/MM/dd  HH:mm");
    public string FolderName => Path.GetFileName(Entry.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    public bool HasTag => !string.IsNullOrWhiteSpace(Entry.TagColor);
    public string TagName => ResolveTagName();
    public Brush? TagBrush => string.IsNullOrWhiteSpace(Entry.TagColor)
        ? null
        : new SolidColorBrush(ParseHex(Entry.TagColor));

    private BitmapSource? _thumbnail;
    public BitmapSource? Thumbnail
    {
        get
        {
            if (_thumbnail == null)
                _thumbnail = LoadThumbnail(Entry);
            return _thumbnail;
        }
    }

    public HistoryItemViewModel(HistoryEntry entry) => Entry = entry;

    private string ResolveTagName()
    {
        if (!string.IsNullOrWhiteSpace(Entry.TagName))
            return Entry.TagName;

        var tag = !string.IsNullOrWhiteSpace(Entry.TagKey)
            ? TagColors.FromKey(Entry.TagKey)
            : TagColors.All.FirstOrDefault(t => string.Equals(t.HexColor, Entry.TagColor, StringComparison.OrdinalIgnoreCase));

        return tag is null || tag.IsNone
            ? string.Empty
            : TagSettingsService.GetDisplayName(tag);
    }

    private static BitmapSource? LoadThumbnail(HistoryEntry entry)
    {
        foreach (var path in GetIconCandidates(entry))
        {
            if (!File.Exists(path)) continue;

            var thumbnail = LoadThumbnailFromIco(path);
            if (thumbnail is not null)
                return thumbnail;
        }

        return null;
    }

    private static IEnumerable<string> GetIconCandidates(HistoryEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.IconStoragePath))
            yield return entry.IconStoragePath;

        // フォルダ内 _folderly\cover_*.ico を動的に列挙（旧 .folderly\cover.ico フォールバックも残す）
        foreach (var dirName in new[] { "_folderly", ".folderly" })
        {
            var dir = Path.Combine(entry.FolderPath, dirName);
            if (Directory.Exists(dir))
            {
                foreach (var ico in Directory.EnumerateFiles(dir, "cover*.ico"))
                    yield return ico;
            }
        }
    }

    private static BitmapSource? LoadThumbnailFromIco(string icoPath)
    {
        var thumbnail = LoadWithBitmapDecoder(icoPath);
        return thumbnail ?? LoadEmbeddedPngFrame(icoPath);
    }

    private static BitmapSource? LoadWithBitmapDecoder(string icoPath)
    {
        try
        {
            var decoder = BitmapDecoder.Create(
                new Uri(icoPath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            // 48px に最も近いフレームを選択
            var frame = decoder.Frames
                .OrderBy(f => Math.Abs(f.PixelWidth - 48))
                .First();
            frame.Freeze();
            return frame;
        }
        catch { return null; }
    }

    private static BitmapSource? LoadEmbeddedPngFrame(string icoPath)
    {
        try
        {
            using var stream = File.OpenRead(icoPath);
            using var reader = new BinaryReader(stream);

            if (reader.ReadUInt16() != 0) return null;
            if (reader.ReadUInt16() != 1) return null;

            var count = reader.ReadUInt16();
            if (count == 0) return null;

            var entries = new List<(int Size, int Bytes, int Offset)>(count);
            for (var i = 0; i < count; i++)
            {
                var width = reader.ReadByte();
                var height = reader.ReadByte();
                reader.ReadBytes(4); // color count, reserved, planes
                reader.ReadUInt16(); // bit count
                var bytes = reader.ReadInt32();
                var offset = reader.ReadInt32();

                var size = width == 0 ? 256 : width;
                if (height != 0)
                    size = Math.Min(size, height);
                entries.Add((size, bytes, offset));
            }

            foreach (var entry in entries.OrderBy(e => Math.Abs(e.Size - 48)))
            {
                if (entry.Bytes <= 0 || entry.Offset < 0) continue;
                if (entry.Offset + entry.Bytes > stream.Length) continue;

                stream.Position = entry.Offset;
                var data = reader.ReadBytes(entry.Bytes);
                if (!IsPng(data)) continue;

                using var pngStream = new MemoryStream(data);
                var decoder = new PngBitmapDecoder(
                    pngStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = decoder.Frames[0];
                frame.Freeze();
                return frame;
            }
        }
        catch { return null; }

        return null;
    }

    private static bool IsPng(byte[] data)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return data.Length >= signature.Length &&
               data.Take(signature.Length).SequenceEqual(signature);
    }

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }
}
