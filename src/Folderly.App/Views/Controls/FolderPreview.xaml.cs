using Folderly.Core.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CoreCropMode = Folderly.Core.Composition.CropMode;

namespace Folderly.App.Views.Controls;

/// <summary>
/// フォルダ型プレビューコントロール。
/// FolderTemplate PNG をベースに、ユーザー画像とタグ色を WPF トランスフォームで合成表示する。
/// ドラッグで画像位置を調整でき、OffsetX/Y が ICO 座標系（256px 基準）で PositionChanged に通知される。
/// </summary>
public partial class FolderPreview : UserControl
{
    // プレビューサイズと ICO 座標系（256px 基準）の変換比率
    private const double PreviewSize = 320.0;
    private static readonly double PreviewScale = PreviewSize / FolderTemplate.BaseSize; // 1.25

    // ImageRegion をプレビュー座標（320px）に変換した値（静的初期化）
    private static readonly Rect ImageRegionPx;

    static FolderPreview()
    {
        var imageRegion = FolderTemplate.ScaleRegion(FolderTemplate.ImageRegion, (float)PreviewSize);
        ImageRegionPx = new Rect(imageRegion.X, imageRegion.Y, imageRegion.Width, imageRegion.Height);
    }

    // ─── Dependency Properties ───────────────────────────────────────────────

    public static readonly DependencyProperty SourceImageProperty =
        DependencyProperty.Register(nameof(SourceImage), typeof(BitmapSource), typeof(FolderPreview),
            new PropertyMetadata(null, OnRenderPropertyChanged));

    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(nameof(Scale), typeof(double), typeof(FolderPreview),
            new PropertyMetadata(1.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty OffsetXProperty =
        DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(FolderPreview),
            new PropertyMetadata(0.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty OffsetYProperty =
        DependencyProperty.Register(nameof(OffsetY), typeof(double), typeof(FolderPreview),
            new PropertyMetadata(0.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty SelectedTagColorProperty =
        DependencyProperty.Register(nameof(SelectedTagColor), typeof(TagColor), typeof(FolderPreview),
            new PropertyMetadata(TagColors.None, OnRenderPropertyChanged));

    public static readonly DependencyProperty CropModeProperty =
        DependencyProperty.Register(nameof(CropMode), typeof(CoreCropMode), typeof(FolderPreview),
            new PropertyMetadata(CoreCropMode.Center, OnRenderPropertyChanged));

    // ─── Public Properties ───────────────────────────────────────────────────

    /// <summary>表示する画像。null の場合はプレースホルダーを表示。</summary>
    public BitmapSource? SourceImage
    {
        get => (BitmapSource?)GetValue(SourceImageProperty);
        set => SetValue(SourceImageProperty, value);
    }

    /// <summary>ユーザー拡大率（1.0 = デフォルト）。</summary>
    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    /// <summary>X オフセット（ICO 座標系 256px 基準）。</summary>
    public double OffsetX
    {
        get => (double)GetValue(OffsetXProperty);
        set => SetValue(OffsetXProperty, value);
    }

    /// <summary>Y オフセット（ICO 座標系 256px 基準）。</summary>
    public double OffsetY
    {
        get => (double)GetValue(OffsetYProperty);
        set => SetValue(OffsetYProperty, value);
    }

    /// <summary>選択中のタグ色。</summary>
    public TagColor SelectedTagColor
    {
        get => (TagColor)GetValue(SelectedTagColorProperty);
        set => SetValue(SelectedTagColorProperty, value);
    }

    /// <summary>クロップモード（Center / Pad）。</summary>
    public CoreCropMode CropMode
    {
        get => (CoreCropMode)GetValue(CropModeProperty);
        set => SetValue(CropModeProperty, value);
    }

    // ─── Events ──────────────────────────────────────────────────────────────

    /// <summary>ドラッグで画像位置が変化したときに発生する。値は ICO 座標系。</summary>
    public event EventHandler<(double OffsetX, double OffsetY)>? PositionChanged;
    public event EventHandler<double>? ScaleChanged;

    // ─── Drag state ──────────────────────────────────────────────────────────

    private bool _isDragging;
    private Point _dragStart;
    private double _dragStartOffsetX;
    private double _dragStartOffsetY;

    // ─── Constructor ─────────────────────────────────────────────────────────

    public FolderPreview()
    {
        InitializeComponent();
        PlaceRegions();
        LoadTemplateImage();
        UpdateVisuals();
    }

    // ─── Initialization ──────────────────────────────────────────────────────

    private void PlaceRegions()
    {
        // ImageCanvas を ImageRegion の位置・サイズに配置
        Canvas.SetLeft(ImageCanvas, ImageRegionPx.X);
        Canvas.SetTop(ImageCanvas, ImageRegionPx.Y);
        ImageCanvas.Width  = ImageRegionPx.Width;
        ImageCanvas.Height = ImageRegionPx.Height;
        ImageCanvas.Clip = CreateImageClipGeometry();

        TagPath.Data = CreateTabGeometry();
        ImageBasePath.Data = CreateImageBaseGeometry();
    }

    private void LoadTemplateImage()
    {
        TemplateImage.Source = LoadPng(FolderTemplate.GetBackTemplateBytes());
    }

    private static BitmapImage LoadPng(byte[] bytes)
    {
        var bitmap = new BitmapImage();
        using var ms = new MemoryStream(bytes);
        bitmap.BeginInit();
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    // ─── Rendering ───────────────────────────────────────────────────────────

    private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FolderPreview)d).UpdateVisuals();

    private void UpdateVisuals()
    {
        var src = SourceImage;
        if (src is null)
        {
            Placeholder.Visibility = Visibility.Visible;
            UserImage.Source = null;
            RootCanvas.Cursor = Cursors.Arrow;
        }
        else
        {
            Placeholder.Visibility = Visibility.Collapsed;
            UserImage.Source = src;
            RootCanvas.Cursor = Cursors.SizeAll;
            UpdateImageTransform(src);
        }

        UpdateTagColor();
    }

    private void UpdateImageTransform(BitmapSource src)
    {
        double imgW = src.PixelWidth;
        double imgH = src.PixelHeight;
        double regionW = ImageRegionPx.Width;
        double regionH = ImageRegionPx.Height;

        // CropMode に応じたベーススケール（ImageRegion をちょうど埋める or 収める）
        double baseScale = CropMode == CoreCropMode.Center
            ? Math.Max(regionW / imgW, regionH / imgH)
            : regionW / imgW;

        double totalScale = baseScale * Scale;

        // 画像を ImageCanvas 内の中央に配置し、ICO 座標系のオフセットをプレビュー座標に変換して加算
        double tx = (regionW - imgW * totalScale) / 2.0 + OffsetX * PreviewScale;
        double ty = (regionH - imgH * totalScale) / 2.0 + OffsetY * PreviewScale;

        ImgScale.ScaleX = totalScale;
        ImgScale.ScaleY = totalScale;
        ImgTranslate.X  = tx;
        ImgTranslate.Y  = ty;
    }

    private void UpdateTagColor()
    {
        var tag = SelectedTagColor;
        if (tag is null || tag.IsNone)
        {
            TagPath.Visibility = Visibility.Collapsed;
            return;
        }

        TagPath.Visibility = Visibility.Visible;
        TagPath.Fill = new SolidColorBrush(ParseHexColor(tag.HexColor!));
    }

    private static Geometry CreateTabGeometry()
    {
        var points = FolderTemplate.GetTabShapePoints((float)PreviewSize);
        var radius = Math.Min(PreviewSize * 0.035, (points[3].Y - points[0].Y) * 0.45);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var slope = new Vector(points[2].X - points[1].X, points[2].Y - points[1].Y);
            slope.Normalize();
            var rightRadius = PreviewSize * 0.09;
            var topCurveStart = new Point(points[1].X - rightRadius, points[1].Y);
            var slopeCurveEnd = new Point(
                points[1].X + slope.X * rightRadius,
                points[1].Y + slope.Y * rightRadius);

            ctx.BeginFigure(new Point(points[3].X, points[3].Y), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(points[0].X, points[0].Y + radius), isStroked: true, isSmoothJoin: true);
            ctx.QuadraticBezierTo(
                new Point(points[0].X, points[0].Y),
                new Point(points[0].X + radius, points[0].Y),
                isStroked: true,
                isSmoothJoin: true);
            ctx.LineTo(topCurveStart, isStroked: true, isSmoothJoin: true);
            ctx.QuadraticBezierTo(
                new Point(points[1].X, points[1].Y),
                slopeCurveEnd,
                isStroked: true,
                isSmoothJoin: true);
            ctx.LineTo(new Point(points[2].X, points[2].Y), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateImageClipGeometry()
    {
        var radius = FolderTemplate.BaseSize
            * FolderTemplate.ImageCornerRadiusRatio
            * PreviewScale;
        var geometry = new RectangleGeometry(
            new Rect(0, 0, ImageRegionPx.Width, ImageRegionPx.Height),
            radius,
            radius);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateImageBaseGeometry()
    {
        var radius = FolderTemplate.BaseSize
            * FolderTemplate.ImageCornerRadiusRatio
            * PreviewScale;
        var geometry = new RectangleGeometry(ImageRegionPx, radius, radius);
        geometry.Freeze();
        return geometry;
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        return Color.FromRgb(r, g, b);
    }

    // ─── Mouse / Drag ────────────────────────────────────────────────────────

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SourceImage is null) return;

        _isDragging = true;
        _dragStart = e.GetPosition(RootCanvas);
        _dragStartOffsetX = OffsetX;
        _dragStartOffsetY = OffsetY;
        RootCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var pos = e.GetPosition(RootCanvas);
        // プレビュー座標のデルタを ICO 座標系に変換（÷ PreviewScale）
        double newOffsetX = _dragStartOffsetX + (pos.X - _dragStart.X) / PreviewScale;
        double newOffsetY = _dragStartOffsetY + (pos.Y - _dragStart.Y) / PreviewScale;

        SetCurrentValue(OffsetXProperty, newOffsetX);
        SetCurrentValue(OffsetYProperty, newOffsetY);
        PositionChanged?.Invoke(this, (newOffsetX, newOffsetY));
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SourceImage is null) return;

        var pos = e.GetPosition(RootCanvas);
        if (!ImageRegionPx.Contains(pos)) return;

        var step = e.Delta > 0 ? 0.05 : -0.05;
        var newScale = Math.Clamp(Scale + step, 0.5, 3.0);
        SetCurrentValue(ScaleProperty, newScale);
        ScaleChanged?.Invoke(this, newScale);
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        RootCanvas.ReleaseMouseCapture();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        // ウィンドウ外にカーソルが出たらドラッグ終了（MouseCapture があれば継続するが安全策として）
        if (_isDragging && e.LeftButton != MouseButtonState.Pressed)
        {
            _isDragging = false;
            RootCanvas.ReleaseMouseCapture();
        }
    }
}
