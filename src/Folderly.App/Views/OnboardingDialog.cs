using Folderly.App.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private int _pageIndex;

    private OnboardingDialog()
    {
        var L = AppServices.Localize;

        Title = L["OnboardingTitle"];
        Width = 780;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brushes.White;
        FontFamily = new FontFamily("Segoe UI Variable");

        var root = new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(26),
        };
        Content = root;

        var stack = new StackPanel();
        root.Child = stack;

        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stack.Children.Add(header);

        _titleText = new TextBlock
        {
            FontSize = 22,
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
            Foreground = TextBrush(71, 85, 105),
            Background = Brush(241, 245, 249),
            Padding = new Thickness(10, 5, 10, 5),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(_stepText, 1);
        header.Children.Add(_stepText);

        _imageGrid = new Grid
        {
            Height = 330,
            Margin = new Thickness(0, 0, 0, 18),
        };
        stack.Children.Add(_imageGrid);

        _bodyText = new TextBlock
        {
            FontSize = 14.5,
            LineHeight = 23,
            Foreground = TextBrush(51, 65, 85),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 24),
        };
        stack.Children.Add(_bodyText);

        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stack.Children.Add(footer);

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

    private void RenderPage()
    {
        var L = AppServices.Localize;
        var page = Pages[_pageIndex];

        _titleText.Text = L[page.TitleKey];
        _bodyText.Text = L[page.BodyKey];
        _stepText.Text = $"{_pageIndex + 1} / {Pages.Length}";
        _previousButton.Visibility = _pageIndex == 0 ? Visibility.Hidden : Visibility.Visible;
        _nextButton.Content = _pageIndex == Pages.Length - 1 ? L["OnboardingStart"] : L["OnboardingNext"];

        RenderImages(page.Images);
    }

    private void RenderImages(IReadOnlyList<string> images)
    {
        _imageGrid.Children.Clear();
        _imageGrid.ColumnDefinitions.Clear();

        for (var i = 0; i < images.Count; i++)
            _imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < images.Count; i++)
        {
            var image = new Image
            {
                Source = LoadOnboardingImage(images[i]),
                Stretch = Stretch.Uniform,
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            var card = new Border
            {
                Background = Brush(248, 250, 252),
                BorderBrush = Brush(214, 222, 235),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(i == 0 ? 0 : 6, 0, i == images.Count - 1 ? 0 : 6, 0),
                Child = image,
            };
            Grid.SetColumn(card, i);
            _imageGrid.Children.Add(card);
        }
    }

    private static Button BuildButton(string text, bool primary)
        => new()
        {
            Content = text,
            MinWidth = primary ? 118 : 94,
            Padding = new Thickness(16, 9, 16, 9),
            FontSize = 13.5,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = TextBrush(15, 23, 42),
            Background = primary ? Brush(219, 234, 254) : Brush(248, 250, 252),
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

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(Color.FromRgb(r, g, b));

    private static SolidColorBrush TextBrush(byte r, byte g, byte b)
        => Brush(r, g, b);

    private sealed record OnboardingPage(string TitleKey, string BodyKey, string[] Images);
}
