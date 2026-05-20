using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.Core.History;
using System.Collections.ObjectModel;
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

    private BitmapSource? _thumbnail;
    public BitmapSource? Thumbnail
    {
        get
        {
            if (_thumbnail == null && File.Exists(Entry.IconStoragePath))
                _thumbnail = LoadThumbnail(Entry.IconStoragePath);
            return _thumbnail;
        }
    }

    public HistoryItemViewModel(HistoryEntry entry) => Entry = entry;

    private static BitmapSource? LoadThumbnail(string icoPath)
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
}
