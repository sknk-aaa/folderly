using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.Core.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Folderly.App.Views;

public sealed class TagSettingsDialog : Window
{
    private readonly Dictionary<TagColor, TextBox> _nameBoxes = [];
    private readonly CheckBox _showOnIconCheck = new();

    public TagSettingsDialog()
    {
        var L = AppServices.Localize;

        Title = L["TagSettingsTitle"];
        Width = 420;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)(Application.Current.TryFindResource("SurfaceBrush") ?? Brushes.White);
        FontFamily = new FontFamily("Segoe UI Variable");

        var root = new StackPanel { Margin = new Thickness(22, 18, 22, 18) };
        Content = root;

        root.Children.Add(new TextBlock
        {
            Text = L["TagNamesSection"],
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin = new Thickness(0, 0, 0, 10),
        });

        foreach (var tag in TagColors.All.Where(t => !t.IsNone))
            AddTagRow(root, tag);

        root.Children.Add(new Separator
        {
            Margin = new Thickness(0, 14, 0, 14),
            Background = (Brush)(Application.Current.TryFindResource("BorderBrush") ?? Brushes.LightGray),
        });

        _showOnIconCheck.Content = L["ShowTagNameOnIcon"];
        _showOnIconCheck.IsChecked = TagSettingsService.GetShowTagNameOnIcon();
        _showOnIconCheck.FontSize = 13;
        _showOnIconCheck.Margin = new Thickness(0, 0, 0, 4);
        root.Children.Add(_showOnIconCheck);

        root.Children.Add(new TextBlock
        {
            Text = L["ShowTagNameOnIconNote"],
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin = new Thickness(22, 0, 0, 0),
        });

        root.Children.Add(new Border
        {
            BorderBrush = (Brush)(Application.Current.TryFindResource("BorderBrush") ?? Brushes.LightGray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(-22, 18, -22, -18),
            Padding = new Thickness(22, 12, 22, 12),
            Child = CreateFooter(),
        });
    }

    private void AddTagRow(Panel root, TagColor tag)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var swatch = new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(ParseHex(tag.HexColor!)),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(swatch, 0);
        row.Children.Add(swatch);

        var colorLabel = new TextBlock
        {
            Text = AppServices.Localize[GetColorLabelKey(tag)],
            Width = 74,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
        };
        Grid.SetColumn(colorLabel, 1);
        row.Children.Add(colorLabel);

        var box = new TextBox
        {
            Text = TagSettingsService.GetDisplayName(tag),
            FontSize = 13,
            Padding = new Thickness(7, 4, 7, 4),
            MaxLength = 24,
        };
        Grid.SetColumn(box, 2);
        row.Children.Add(box);

        _nameBoxes[tag] = box;
        root.Children.Add(row);
    }

    private FrameworkElement CreateFooter()
    {
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var cancel = new Button
        {
            Content = AppServices.Localize["Cancel"],
            Style = (Style?)Application.Current.TryFindResource("SecondaryButton"),
            Padding = new Thickness(18, 7, 18, 7),
            Margin = new Thickness(0, 0, 10, 0),
        };
        cancel.Click += (_, _) => Close();
        footer.Children.Add(cancel);

        var save = new Button
        {
            Content = AppServices.Localize["Save"],
            Style = (Style?)Application.Current.TryFindResource("PrimaryButton"),
            Padding = new Thickness(20, 7, 20, 7),
        };
        save.Click += (_, _) => SaveAndClose();
        footer.Children.Add(save);

        return footer;
    }

    private void SaveAndClose()
    {
        foreach (var (tag, box) in _nameBoxes)
            TagSettingsService.SetDisplayName(tag, box.Text);

        TagSettingsService.SetShowTagNameOnIcon(_showOnIconCheck.IsChecked == true);
        DialogResult = true;
    }

    private static string GetColorLabelKey(TagColor tag)
        => tag.Key switch
        {
            "blue" => "ColorBlue",
            "green" => "ColorGreen",
            "orange" => "ColorOrange",
            "purple" => "ColorPurple",
            "red" => "ColorRed",
            "gray" => "ColorGray",
            _ => "TagNone",
        };

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }
}
