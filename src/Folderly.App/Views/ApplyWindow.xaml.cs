using Folderly.App.Infrastructure;
using Folderly.App.ViewModels;
using Folderly.Core.Application;
using Folderly.Core.Composition;
using Folderly.Core.Folder;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Folderly.App.Views;

/// <summary>
/// 画像選択画面（SPEC Section 4.1, F-02 〜 F-05）。
/// 右クリックメニューから起動される。コマンドライン引数でフォルダパスを受け取る。
/// </summary>
public partial class ApplyWindow : Window
{
    private readonly ApplyViewModel _vm;

    public ApplyWindow(string folderPath)
    {
        InitializeComponent();
        _vm = new ApplyViewModel(folderPath);
        DataContext = _vm;

        BuildTagButtons();
        CheckProtectionOnStartup();
    }

    // ─── 起動時保護チェック ──────────────────────────────────────────────────

    private void CheckProtectionOnStartup()
    {
        var result = FolderProtection.CheckPath(_vm.FolderPath);
        if (!result.IsDenied) return;

        var L = AppServices.Localize;
        MessageBox.Show(
            string.Format(L["ProtectionDeniedDetail"], result.Reason),
            L["ProtectionDeniedTitle"],
            MessageBoxButton.OK, MessageBoxImage.Warning);
        Close();
    }

    // ─── タグボタン動的生成 ──────────────────────────────────────────────────

    private void BuildTagButtons()
    {
        var L = AppServices.Localize;
        var labelKeys = new[] { "TagNone", "TagBlue", "TagGreen", "TagOrange", "TagPurple", "TagRed", "TagGray" };

        for (int i = 0; i < TagColors.All.Count; i++)
        {
            var tag = TagColors.All[i];
            var label = L[labelKeys[i]];
            var btn = new Button
            {
                Width    = 54,
                Height   = 54,
                Margin   = new Thickness(0, 0, 8, 0),
                ToolTip  = label,
                Tag      = tag,
                Template = CreateTagButtonTemplate(tag),
            };
            btn.Click += TagButton_Click;
            TagPanel.Children.Add(btn);
        }
    }

    private static ControlTemplate CreateTagButtonTemplate(TagColor tag)
    {
        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "Bd";
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(27));
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Transparent);

        var ellipseFactory = new FrameworkElementFactory(typeof(Ellipse));
        ellipseFactory.SetValue(FrameworkElement.WidthProperty, 42.0);
        ellipseFactory.SetValue(FrameworkElement.HeightProperty, 42.0);

        Color fill = tag.IsNone
            ? Color.FromRgb(220, 220, 220)
            : ParseHex(tag.HexColor!);
        ellipseFactory.SetValue(Shape.FillProperty, new SolidColorBrush(fill));

        borderFactory.AppendChild(ellipseFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

    // ─── 画像選択 ────────────────────────────────────────────────────────────

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = AppServices.Localize["SelectImage"],
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All Files|*.*",
        };
        if (dlg.ShowDialog(this) != true) return;
        LoadImage(dlg.FileName);
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files?.Length > 0)
            LoadImage(files[0]);
    }

    private void LoadImage(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource        = new Uri(path);
            bitmap.CacheOption      = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions    = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();

            _vm.SourceImage     = bitmap;
            _vm.SourceImagePath = path;
            _vm.ResetPosition();
        }
        catch
        {
            MessageBox.Show(AppServices.Localize["ImageLoadError"], "Folderly",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ─── プレビューイベント ──────────────────────────────────────────────────

    private void Preview_PositionChanged(object sender, (double OffsetX, double OffsetY) e)
    {
        _vm.OffsetX = e.OffsetX;
        _vm.OffsetY = e.OffsetY;
    }

    // ─── タグ選択 ────────────────────────────────────────────────────────────

    private void TagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Core.Composition.TagColor tag)
            _vm.SelectedTagColor = tag;
    }

    // ─── スライダー・リセット ────────────────────────────────────────────────

    private void ResetPosition_Click(object sender, RoutedEventArgs e)
        => _vm.ResetPosition();

    // ─── 適用 ────────────────────────────────────────────────────────────────

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.CanApply) return;

        // 警告チェック（OneDrive 等）
        var protection = FolderProtection.CheckPath(_vm.FolderPath);
        if (protection.IsWarning)
        {
            var L = AppServices.Localize;
            var msg = string.Format(L["WarningGenericMessage"], protection.Reason);
            var res = MessageBox.Show(msg, L["WarningGenericTitle"],
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.OK) return;
        }

        _vm.IsApplying = true;
        ApplyBtn.Content = AppServices.Localize["Applying"];

        try
        {
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_vm.SourceImage!));
            encoder.Save(stream);
            stream.Position = 0;

            var request = new ApplyRequest(
                FolderPath:        _vm.FolderPath,
                SourceImageStream: stream,
                SourceImagePath:   _vm.SourceImagePath ?? string.Empty,
                AdjustParams:      _vm.GetAdjustParams(),
                TagColor:          _vm.SelectedTagColor,
                ForceApply:        false);

            var result = await AppServices.Apply.ApplyAsync(request);

            if (result.IsWarning)
            {
                // ForceApply で再試行（OneDrive 等、Apply 内部の Warning）
                var req2 = request with { ForceApply = true };
                result = await AppServices.Apply.ApplyAsync(req2);
            }

            if (result.IsSuccess)
            {
                ShowSuccessToast();
                await Task.Delay(1200);
                Close();
            }
        }
        catch (FolderProtectionException ex)
        {
            MessageBox.Show(ex.Message, AppServices.Localize["ProtectionDeniedTitle"],
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(AppServices.Localize["ApplyFailed"], ex.Message),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _vm.IsApplying = false;
            ApplyBtn.Content = AppServices.Localize["Apply"];
        }
    }

    private void ShowSuccessToast()
    {
        ApplyBtn.Content = AppServices.Localize["ApplySuccess"];
        ApplyBtn.Background = new SolidColorBrush(Color.FromRgb(16, 124, 16));
    }

    // ─── キャンセル ──────────────────────────────────────────────────────────

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
