using Folderly.App.Infrastructure;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Folderly.App.Views;

public sealed class OnboardingDialog : Window
{
    private static readonly OnboardingPage[] Pages =
    [
        new("OnboardingStep1Title", "OnboardingStep1Body", ["lightclick.png"]),
        new("OnboardingStep2Title", "OnboardingStep2Body", ["preview1.png", "preview2.png"]),
        new("OnboardingStep3Title", "OnboardingStep3Body", ["mainmenu.png"]),
    ];

    private readonly TextBlock _stepText;
    private readonly TextBlock _titleText;
    private readonly TextBlock _bodyText;
    private readonly Grid _imageGrid;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly List<StepItem> _stepItems = [];
    private int _pageIndex;

    private OnboardingDialog()
    {
        var L = AppServices.Localize;

        Title = L["OnboardingTitle"];
        Width = 1080;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brush(248, 250, 252);
        FontFamily = new FontFamily("Segoe UI Variable, Microsoft YaHei UI, Yu Gothic UI, Meiryo");

        var root = new Grid { Background = Brush(248, 250, 252) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Content = root;

        var rail = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush(226, 232, 240),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(20),
        };
        root.Children.Add(rail);

        var railStack = new StackPanel();
        rail.Child = railStack;

        var brand = new Grid { Margin = new Thickness(0, 0, 0, 24) };
        brand.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        brand.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        railStack.Children.Add(brand);

        var iconWrap = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(12),
            Background = Brush(244, 248, 255),
            BorderBrush = Brush(219, 234, 254),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 10, 0),
        };
        var icon = TryLoadAppIcon();
        iconWrap.Child = icon is null
            ? BuildFallbackIcon()
            : new Image { Source = icon, Width = 32, Height = 32, Stretch = Stretch.Uniform };
        brand.Children.Add(iconWrap);

        var brandText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        brandText.Children.Add(new TextBlock
        {
            Text = "Folderly",
            FontSize = 15.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextBrush(15, 23, 42),
        });
        brandText.Children.Add(new TextBlock
        {
            Text = L["OnboardingTitle"],
            FontSize = 11,
            Foreground = TextBrush(100, 116, 139),
            Margin = new Thickness(0, 2, 0, 0),
        });
        Grid.SetColumn(brandText, 1);
        brand.Children.Add(brandText);

        var stepList = new StackPanel();
        railStack.Children.Add(stepList);

        for (var i = 0; i < Pages.Length; i++)
        {
            var item = BuildStepItem(i);
            _stepItems.Add(item);
            stepList.Children.Add(item.Root);
        }

        var main = new StackPanel { Margin = new Thickness(38, 34, 38, 30) };
        Grid.SetColumn(main, 1);
        root.Children.Add(main);

        var header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        main.Children.Add(header);

        _titleText = new TextBlock
        {
            FontSize = 27,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextBrush(15, 23, 42),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        header.Children.Add(_titleText);

        _stepText = new TextBlock
        {
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextBrush(37, 99, 235),
            Background = Brush(219, 234, 254),
            Padding = new Thickness(10, 5, 10, 5),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(_stepText, 1);
        header.Children.Add(_stepText);

        _imageGrid = new Grid
        {
            Height = 430,
            Margin = new Thickness(0, 0, 0, 20),
        };
        main.Children.Add(_imageGrid);

        _bodyText = new TextBlock
        {
            FontSize = 15,
            LineHeight = 24,
            Foreground = TextBrush(51, 65, 85),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 0, 2, 22),
        };
        main.Children.Add(_bodyText);

        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        main.Children.Add(footer);

        var skipButton = BuildButton(L["OnboardingSkip"], primary: false);
        skipButton.Click += (_, _) => Close();
        footer.Children.Add(skipButton);

        var pagerButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        Grid.SetColumn(pagerButtons, 2);
        footer.Children.Add(pagerButtons);

        _previousButton = BuildButton(L["OnboardingPrevious"], primary: false);
        _previousButton.Click += (_, _) =>
        {
            if (_pageIndex > 0)
            {
                _pageIndex--;
                RenderPage();
            }
        };
        pagerButtons.Children.Add(_previousButton);

        _nextButton = BuildButton(L["OnboardingNext"], primary: true);
        _nextButton.Margin = new Thickness(10, 0, 0, 0);
        _nextButton.Click += (_, _) =>
        {
            if (_pageIndex >= Pages.Length - 1)
            {
                Close();
                return;
            }

            _pageIndex++;
            RenderPage();
        };
        pagerButtons.Children.Add(_nextButton);

        Loaded += (_, _) =>
        {
            Activate();
            _nextButton.Focus();
        };

        RenderPage();
    }

    public static void Show(Window owner)
    {
        var dialog = new OnboardingDialog { Owner = owner };
        dialog.ShowDialog();
    }

    private StepItem BuildStepItem(int index)
    {
        var L = AppServices.Localize;

        var root = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(9),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
        };
        root.MouseLeftButtonUp += (_, _) =>
        {
            _pageIndex = index;
            RenderPage();
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Child = grid;

        var number = new TextBlock
        {
            Text = (index + 1).ToString(),
            Width = 24,
            Height = 24,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        grid.Children.Add(number);

        var title = new TextBlock
        {
            Text = L[Pages[index].TitleKey],
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
        };
        Grid.SetColumn(title, 1);
        grid.Children.Add(title);

        return new StepItem(root, number, title);
    }

    private void RenderPage()
    {
        var L = AppServices.Localize;
        var page = Pages[_pageIndex];

        _titleText.Text = L[page.TitleKey];
        _bodyText.Text = L[page.BodyKey];
        _stepText.Text = $"{_pageIndex + 1} / {Pages.Length}";
        _previousButton.Visibility = _pageIndex == 0 ? Visibility.Hidden : Visibility.Visible;
        _nextButton.Content = _pageIndex == Pages.Length - 1 ? L["OnboardingStart"] : L["OnboardingNext"];

        for (var i = 0; i < _stepItems.Count; i++)
            SetStepItemActive(_stepItems[i], i == _pageIndex);

        RenderImages(page.Images);
    }

    private static void SetStepItemActive(StepItem item, bool active)
    {
        item.Root.Background = active ? Brush(239, 246, 255) : Brushes.White;
        item.Root.BorderBrush = active ? Brush(147, 197, 253) : Brush(226, 232, 240);
        item.Number.Foreground = active ? Brushes.White : TextBrush(71, 85, 105);
        item.Number.Background = active ? Brush(37, 99, 235) : Brush(241, 245, 249);
        item.Title.Foreground = active ? TextBrush(15, 23, 42) : TextBrush(71, 85, 105);
    }

    private void RenderImages(IReadOnlyList<string> images)
    {
        _imageGrid.Children.Clear();
        _imageGrid.ColumnDefinitions.Clear();
        _imageGrid.RowDefinitions.Clear();

        if (images.Count == 2)
        {
            RenderOverlappedImages(images);
            return;
        }

        for (var i = 0; i < images.Count; i++)
            _imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < images.Count; i++)
        {
            var image = new Image
            {
                Source = LoadOnboardingImage(images[i]),
                Stretch = Stretch.Uniform,
                MaxHeight = 420,
                Margin = new Thickness(i == 0 ? 0 : 8, 0, i == images.Count - 1 ? 0 : 8, 0),
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image.Effect = BuildImageShadow();

            Grid.SetColumn(image, i);
            _imageGrid.Children.Add(image);
        }
    }

    private void RenderOverlappedImages(IReadOnlyList<string> images)
    {
        var first = BuildOnboardingImage(images[0], maxWidth: 650, maxHeight: 408);
        first.HorizontalAlignment = HorizontalAlignment.Left;
        first.VerticalAlignment = VerticalAlignment.Center;
        first.Margin = new Thickness(0, 0, 0, 12);
        Panel.SetZIndex(first, 1);
        _imageGrid.Children.Add(first);

        var second = BuildOnboardingImage(images[1], maxWidth: 545, maxHeight: 352);
        second.HorizontalAlignment = HorizontalAlignment.Right;
        second.VerticalAlignment = VerticalAlignment.Bottom;
        second.Margin = new Thickness(0, 0, 0, 2);
        Panel.SetZIndex(second, 2);
        _imageGrid.Children.Add(second);
    }

    private static Image BuildOnboardingImage(string fileName, double maxWidth, double maxHeight)
    {
        var image = new Image
        {
            Source = LoadOnboardingImage(fileName),
            Stretch = Stretch.Uniform,
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        image.Effect = BuildImageShadow();
        return image;
    }

    private static DropShadowEffect BuildImageShadow()
        => new()
        {
            BlurRadius = 18,
            ShadowDepth = 8,
            Opacity = 0.16,
            Color = Color.FromRgb(30, 64, 120),
        };

    private static Button BuildButton(string text, bool primary)
        => new()
        {
            Content = text,
            MinWidth = primary ? 118 : 94,
            Padding = new Thickness(16, 9, 16, 9),
            FontSize = 13.5,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = TextBrush(15, 23, 42),
            Background = primary ? Brush(219, 234, 254) : Brushes.White,
            BorderBrush = primary ? Brush(147, 197, 253) : Brush(203, 213, 225),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
        };

    private static ImageSource LoadOnboardingImage(string fileName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri($"pack://application:,,,/Resources/Onboarding/{fileName}", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static ImageSource? TryLoadAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Images", "Square150x150Logo.png");
        if (!File.Exists(path))
            return null;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static FrameworkElement BuildFallbackIcon()
        => new TextBlock
        {
            Text = "F",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush(37, 99, 235),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(Color.FromRgb(r, g, b));

    private static SolidColorBrush TextBrush(byte r, byte g, byte b)
        => Brush(r, g, b);

    private sealed record OnboardingPage(string TitleKey, string BodyKey, string[] Images);

    private sealed record StepItem(Border Root, TextBlock Number, TextBlock Title);
}
