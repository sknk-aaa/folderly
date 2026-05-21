using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.Core.Application;
using Folderly.Core.Composition;
using System.Windows.Media.Imaging;
using CoreCropMode = Folderly.Core.Composition.CropMode;

namespace Folderly.App.ViewModels;

public sealed class ApplyViewModel : ViewModelBase
{
    public LocalizationService L => LocalizationService.Instance;

    // ─── フォルダ ─────────────────────────────────────────────────────────────

    public string FolderPath { get; }

    // ─── 画像 ─────────────────────────────────────────────────────────────────

    private BitmapSource? _sourceImage;
    public BitmapSource? SourceImage
    {
        get => _sourceImage;
        set { SetField(ref _sourceImage, value); Notify(nameof(CanApply)); }
    }

    public string? SourceImagePath { get; set; }

    // ─── 画像調整 ─────────────────────────────────────────────────────────────

    private double _scale = 1.0;
    public double Scale
    {
        get => _scale;
        set => SetField(ref _scale, Math.Clamp(value, 0.5, 3.0));
    }

    private double _offsetX;
    public double OffsetX
    {
        get => _offsetX;
        set => SetField(ref _offsetX, value);
    }

    private double _offsetY;
    public double OffsetY
    {
        get => _offsetY;
        set => SetField(ref _offsetY, value);
    }

    private CoreCropMode _cropMode = CoreCropMode.Center;
    public CoreCropMode CropMode
    {
        get => _cropMode;
        set
        {
            if (_cropMode == value) return;
            _cropMode = value;
            Notify();
            Notify(nameof(IsCropCenter));
            Notify(nameof(IsCropFitWidth));
            Notify(nameof(IsCropFitHeight));
            ResetPosition();
        }
    }

    public bool IsCropCenter
    {
        get => CropMode == CoreCropMode.Center;
        set { if (value) CropMode = CoreCropMode.Center; }
    }

    public bool IsCropFitWidth
    {
        get => CropMode == CoreCropMode.FitWidth;
        set { if (value) CropMode = CoreCropMode.FitWidth; }
    }

    public bool IsCropFitHeight
    {
        get => CropMode == CoreCropMode.FitHeight;
        set { if (value) CropMode = CoreCropMode.FitHeight; }
    }

    // ─── タグ色 ───────────────────────────────────────────────────────────────

    private TagColor _selectedTagColor = TagColors.None;
    public TagColor SelectedTagColor
    {
        get => _selectedTagColor;
        set
        {
            if (EqualityComparer<TagColor>.Default.Equals(_selectedTagColor, value)) return;
            _selectedTagColor = value;
            Notify();
            Notify(nameof(SelectedTagName));
            Notify(nameof(EffectiveSelectedTagColor));
        }
    }

    public string SelectedTagName => TagSettingsService.GetDisplayName(SelectedTagColor);

    // Returns SelectedTagColor with any user-defined hex color override applied.
    public TagColor EffectiveSelectedTagColor =>
        SelectedTagColor.IsNone
            ? SelectedTagColor
            : new TagColor(TagSettingsService.GetTagHexColor(SelectedTagColor), SelectedTagColor.Key);

    public bool ShowTagNameOnIcon => TagSettingsService.GetShowTagNameOnIcon();
    public bool ShowTagIconOnIcon => TagSettingsService.GetShowTagIconOnIcon();

    public void RefreshTagSettings()
    {
        Notify(nameof(SelectedTagName));
        Notify(nameof(ShowTagNameOnIcon));
        Notify(nameof(ShowTagIconOnIcon));
        Notify(nameof(EffectiveSelectedTagColor));
    }

    // ─── 状態 ─────────────────────────────────────────────────────────────────

    private bool _isApplying;
    public bool IsApplying
    {
        get => _isApplying;
        set { SetField(ref _isApplying, value); Notify(nameof(CanApply)); }
    }

    public bool CanApply => SourceImage != null && !IsApplying;

    // ─── タグプリセット（SPEC F-05: 7 種） ────────────────────────────────────

    public IReadOnlyList<TagColor> TagPresets => TagColors.All;

    // ─── コンストラクタ ───────────────────────────────────────────────────────

    public ApplyViewModel(string folderPath)
    {
        FolderPath = folderPath;
    }

    // ─── 操作 ─────────────────────────────────────────────────────────────────

    public void ResetPosition()
    {
        Scale   = 1.0;
        OffsetX = 0.0;
        OffsetY = 0.0;
    }

    public ImageAdjustParams GetAdjustParams()
        => new(
            Scale:   (float)Scale,
            OffsetX: (float)OffsetX,
            OffsetY: (float)OffsetY,
            Mode:    CropMode);
}
