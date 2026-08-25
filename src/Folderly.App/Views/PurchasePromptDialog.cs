using Folderly.App.Infrastructure;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Folderly.App.Views;

public sealed class PurchasePromptDialog : Window
{
    private bool _shouldOpenStore;

    private PurchasePromptDialog()
    {
        var L = AppServices.Localize;

        Title = L["PurchasePromptTitle"];
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brushes.Transparent;
        FontFamily = new FontFamily("Segoe UI Variable, Microsoft YaHei UI, Yu Gothic UI, Meiryo");

        var root = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(14),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24),
        };
        root.MouseLeftButtonDown += DragSurface_MouseLeftButtonDown;
        Content = root;

        var stack = new StackPanel();
        root.Child = stack;

        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stack.Children.Add(header);

        var iconWrap = new Border
        {
            Width = 68,
            Height = 68,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromRgb(244, 248, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(219, 234, 254)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 16, 0),
        };
        var icon = TryLoadAppIcon();
        iconWrap.Child = icon is null
            ? BuildFallbackIcon()
            : new Image { Source = icon, Width = 48, Height = 48, Stretch = Stretch.Uniform };
        Grid.SetColumn(iconWrap, 0);
        header.Children.Add(iconWrap);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = L["PurchasePromptTitle"],
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            TextWrapping = TextWrapping.Wrap,
        });
        textStack.Children.Add(new TextBlock
        {
            Text = L["PurchasePromptThanks"],
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Margin = new Thickness(0, 5, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(textStack, 1);
        header.Children.Add(textStack);

        stack.Children.Add(new TextBlock
        {
            Text = L["PurchasePromptMessage"],
            FontSize = 14,
            LineHeight = 21,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 22),
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        stack.Children.Add(buttons);

        var later = BuildButton(L["ReviewPromptLater"], primary: false);
        later.IsCancel = true;
        later.Click += (_, _) =>
        {
            _shouldOpenStore = false;
            DialogResult = false;
        };
        buttons.Children.Add(later);

        var buy = BuildButton(L["BuyNow"], primary: true);
        buy.IsDefault = true;
        buy.Margin = new Thickness(10, 0, 0, 0);
        buy.Click += (_, _) =>
        {
            _shouldOpenStore = true;
            DialogResult = true;
        };
        buttons.Children.Add(buy);

        Loaded += (_, _) =>
        {
            Activate();
            buy.Focus();
        };
    }

    public static bool Show(Window owner)
    {
        var dialog = new PurchasePromptDialog
        {
            Owner = owner,
            Topmost = true,
        };

        dialog.ShowDialog();
        return dialog._shouldOpenStore;
    }

    private static Button BuildButton(string text, bool primary)
        => new()
        {
            Content = text,
            MinWidth = primary ? 150 : 96,
            Padding = new Thickness(16, 9, 16, 9),
            FontSize = 13.5,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            Background = primary
                ? new SolidColorBrush(Color.FromRgb(219, 234, 254))
                : new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = primary
                ? new SolidColorBrush(Color.FromRgb(147, 197, 253))
                : new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
        };

    private void DragSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            e.OriginalSource is not DependencyObject source ||
            HasAncestor<Button>(source))
        {
            return;
        }

        DragMove();
    }

    private static bool HasAncestor<T>(DependencyObject source)
        where T : DependencyObject
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is T)
                return true;
        }

        return false;
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
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(18, 122, 219)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
}
