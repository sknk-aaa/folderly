using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.Core.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Folderly.App.Views;

public sealed class TagSettingsDialog : Window
{
    // 9 preset colors from the design
    private static readonly string[] PresetHexColors =
    [
        "#2D6BD8", "#1AA6B7", "#2E9E6A",
        "#C9A227", "#E07A2A", "#D9434E",
        "#D14EA8", "#7A5AD6", "#7A8593",
    ];

    private TagColor _selectedTag = TagColors.None;
    private readonly Dictionary<TagColor, string> _pendingNames      = [];
    private readonly Dictionary<TagColor, string> _pendingHexColors  = [];
    private readonly Dictionary<TagColor, int>    _pendingIconIndexes = [];
    private readonly Dictionary<TagColor, Border> _tagRows           = [];
    private readonly Dictionary<TagColor, TextBlock> _tagNameLabels  = [];
    private readonly StackPanel _rightPanel = new() { Margin = new Thickness(20, 16, 20, 16) };
    private TextBox? _nameBox;
    private bool _showTagNameOnIcon;
    private bool _showTagIconOnIcon;

    public TagSettingsDialog()
    {
        var L = AppServices.Localize;

        Title  = L["TagEditTitle"];
        Width  = 720;
        Height = 560;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)(Application.Current.TryFindResource("SurfaceBrush") ?? Brushes.White);
        FontFamily = new FontFamily("Segoe UI Variable");

        _showTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon();
        _showTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon();

        foreach (var tag in TagColors.All.Where(t => !t.IsNone))
        {
            _pendingNames[tag]       = TagSettingsService.GetDisplayName(tag);
            _pendingHexColors[tag]   = TagSettingsService.GetTagHexColor(tag) ?? tag.HexColor ?? "#7A8593";
            _pendingIconIndexes[tag] = TagSettingsService.GetTagIconIndex(tag);
        }

        BuildLayout();
    }

    private void BuildLayout()
    {
        var L    = AppServices.Localize;
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = root;

        // ── ヘッダー ──
        var header = new Border
        {
            BorderBrush     = (Brush)(Application.Current.TryFindResource("BorderBrush") ?? Brushes.LightGray),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background      = Brushes.White,
            Padding         = new Thickness(20, 16, 20, 16),
        };
        var headerStack = new StackPanel();
        headerStack.Children.Add(new TextBlock
        {
            Text       = L["TagEditTitle"],
            FontSize   = 18,
            FontWeight = FontWeights.SemiBold,
        });
        headerStack.Children.Add(new TextBlock
        {
            Text       = L["TagEditDesc"],
            FontSize   = 12,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(0, 3, 0, 0),
        });
        header.Child = headerStack;
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ── コンテンツ: 2 カラム ──
        var contentGrid = new Grid();
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(contentGrid, 1);
        root.Children.Add(contentGrid);

        // 左パネル: タグリスト
        var leftBorder = new Border
        {
            BorderBrush     = (Brush)(Application.Current.TryFindResource("BorderBrush") ?? Brushes.LightGray),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Background      = Brushes.White,
        };
        var leftOuter = new Grid();
        leftOuter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftOuter.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftBorder.Child = leftOuter;

        var leftStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        leftStack.Children.Add(new TextBlock
        {
            Text       = L["TagNamesSection"],
            FontSize   = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(16, 6, 16, 8),
        });
        foreach (var tag in TagColors.All.Where(t => !t.IsNone))
            leftStack.Children.Add(BuildTagListRow(tag));

        var leftScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = leftStack,
        };
        Grid.SetRow(leftScroll, 0);
        leftOuter.Children.Add(leftScroll);

        Grid.SetColumn(leftBorder, 0);
        contentGrid.Children.Add(leftBorder);

        // 右パネル: 編集エリア
        var rightScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _rightPanel,
        };
        Grid.SetColumn(rightScroll, 1);
        contentGrid.Children.Add(rightScroll);

        // ── フッター ──
        var footer = BuildFooter();
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        // 最初のタグを選択
        var firstTag = TagColors.All.FirstOrDefault(t => !t.IsNone);
        if (firstTag is not null)
            SelectTag(firstTag);
    }

    private Border BuildTagListRow(TagColor tag)
    {
        var row = new Border
        {
            Padding = new Thickness(16, 9, 16, 9),
            Cursor  = Cursors.Hand,
        };

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Child = content;

        // 色付き円 + アイコン
        var swatchGrid = new Grid { Width = 24, Height = 24, Margin = new Thickness(0, 0, 10, 0) };
        swatchGrid.Children.Add(new Ellipse
        {
            Width  = 24,
            Height = 24,
            Fill   = new SolidColorBrush(ParseHex(_pendingHexColors.GetValueOrDefault(tag, tag.HexColor!))),
        });
        var iconIndex = _pendingIconIndexes.GetValueOrDefault(tag, -1);
        if (iconIndex >= 0 && iconIndex < IconHelper.IconPaths.Length)
            swatchGrid.Children.Add(IconHelper.CreateIconElement(iconIndex, 13, Brushes.White));
        Grid.SetColumn(swatchGrid, 0);
        content.Children.Add(swatchGrid);

        // タグ名
        var nameLabel = new TextBlock
        {
            Text              = _pendingNames.GetValueOrDefault(tag, tag.Key),
            FontSize          = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(nameLabel, 1);
        content.Children.Add(nameLabel);

        // シェブロン
        var chevron = new TextBlock
        {
            Text              = "›",
            FontSize          = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground        = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
        };
        Grid.SetColumn(chevron, 2);
        content.Children.Add(chevron);

        _tagRows[tag]        = row;
        _tagNameLabels[tag]  = nameLabel;

        row.MouseLeftButtonUp += (_, _) => SelectTag(tag);
        return row;
    }

    private void SelectTag(TagColor tag)
    {
        var selBg = new SolidColorBrush(Color.FromArgb(25, 0, 120, 212));
        foreach (var row in _tagRows.Values)
            row.Background = Brushes.Transparent;
        if (_tagRows.TryGetValue(tag, out var selectedRow))
            selectedRow.Background = selBg;

        _selectedTag = tag;
        RefreshRightPanel(tag);
    }

    private void RefreshRightPanel(TagColor tag)
    {
        var L = AppServices.Localize;
        _rightPanel.Children.Clear();

        // ── ヘッダー ──
        _rightPanel.Children.Add(new TextBlock
        {
            Text       = L["TagEditHeadTitle"],
            FontSize   = 14,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 3),
        });
        _rightPanel.Children.Add(new TextBlock
        {
            Text       = string.Format(L["EditingTag"], _pendingNames.GetValueOrDefault(tag, tag.Key)),
            FontSize   = 12,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(0, 0, 0, 14),
        });

        // ── プレビューチップ ──
        _rightPanel.Children.Add(BuildPreviewChip(tag));
        _rightPanel.Children.Add(new Separator { Margin = new Thickness(0, 14, 0, 14) });

        // ── タグ名 ──
        _rightPanel.Children.Add(new TextBlock
        {
            Text       = L["TagNameLabel"],
            FontSize   = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(0, 0, 0, 6),
        });
        _nameBox = new TextBox
        {
            Text      = _pendingNames.GetValueOrDefault(tag, tag.Key),
            FontSize  = 13,
            Padding   = new Thickness(8, 6, 8, 6),
            MaxLength = 24,
            Margin    = new Thickness(0, 0, 0, 14),
        };
        _nameBox.TextChanged += OnNameBoxTextChanged;
        _rightPanel.Children.Add(_nameBox);

        _rightPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 14) });

        // ── アイコンにタグ名を表示 (トグルスイッチ) ──
        var toggleRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toggleRow.Children.Add(new TextBlock
        {
            Text              = L["ShowTagNameOnIcon"],
            FontSize          = 13,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var toggle = BuildToggleSwitch(
            _showTagNameOnIcon,
            value => _showTagNameOnIcon = value);
        Grid.SetColumn(toggle, 1);
        toggleRow.Children.Add(toggle);
        _rightPanel.Children.Add(toggleRow);

        _rightPanel.Children.Add(new TextBlock
        {
            Text        = L["ShowTagNameOnIconNote"],
            FontSize    = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground  = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin      = new Thickness(0, 0, 0, 14),
        });

        var iconToggleRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        iconToggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        iconToggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        iconToggleRow.Children.Add(new TextBlock
        {
            Text              = L["ShowTagIconOnIcon"],
            FontSize          = 13,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var iconToggle = BuildToggleSwitch(
            _showTagIconOnIcon,
            value => _showTagIconOnIcon = value);
        Grid.SetColumn(iconToggle, 1);
        iconToggleRow.Children.Add(iconToggle);
        _rightPanel.Children.Add(iconToggleRow);

        _rightPanel.Children.Add(new TextBlock
        {
            Text        = L["ShowTagIconOnIconNote"],
            FontSize    = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground  = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin      = new Thickness(0, 0, 0, 14),
        });

        _rightPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 14) });

        // ── カラー選択（9 プリセット 3×3） ──
        _rightPanel.Children.Add(new TextBlock
        {
            Text       = L["TagColorLabel"],
            FontSize   = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(0, 0, 0, 8),
        });
        _rightPanel.Children.Add(BuildColorSwatches(tag));

        _rightPanel.Children.Add(new Separator { Margin = new Thickness(0, 14, 0, 14) });

        // ── アイコン選択（15 アイコン 5 列） ──
        _rightPanel.Children.Add(new TextBlock
        {
            Text       = L["TagIconLabel"],
            FontSize   = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(Application.Current.TryFindResource("TextSecondaryBrush") ?? Brushes.DimGray),
            Margin     = new Thickness(0, 0, 0, 8),
        });
        _rightPanel.Children.Add(BuildIconGrid(tag));
    }

    private FrameworkElement BuildPreviewChip(TagColor tag)
    {
        var hex  = _pendingHexColors.GetValueOrDefault(tag, tag.HexColor ?? "#7A8593");
        var name = _pendingNames.GetValueOrDefault(tag, tag.Key);
        var iconIndex = _pendingIconIndexes.GetValueOrDefault(tag, -1);

        var chip = new Border
        {
            CornerRadius = new CornerRadius(6),
            Background   = new SolidColorBrush(ParseHex(hex)),
            Padding      = new Thickness(10, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var inner = new StackPanel { Orientation = Orientation.Horizontal };

        if (iconIndex >= 0 && iconIndex < IconHelper.IconPaths.Length)
            inner.Children.Add(IconHelper.CreateIconElement(iconIndex, 16, Brushes.White));
        else
        {
            var dot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.White, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            inner.Children.Add(dot);
        }

        inner.Children.Add(new TextBlock
        {
            Text              = name,
            FontSize          = 13,
            FontWeight        = FontWeights.SemiBold,
            Foreground        = Brushes.White,
            Margin            = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        chip.Child = inner;
        return chip;
    }

    private FrameworkElement BuildToggleSwitch(bool isOn, Action<bool> onChanged)
    {
        var onColor  = Color.FromRgb(0, 120, 212);
        var offColor = Color.FromRgb(180, 180, 180);

        var thumb = new Ellipse
        {
            Width               = 18,
            Height              = 18,
            Fill                = Brushes.White,
            HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
            Margin              = new Thickness(3),
        };
        var track = new Border
        {
            Width        = 44,
            Height       = 24,
            CornerRadius = new CornerRadius(12),
            Background   = new SolidColorBrush(isOn ? onColor : offColor),
            Child        = thumb,
            Cursor       = Cursors.Hand,
        };

        track.MouseLeftButtonUp += (_, _) =>
        {
            isOn = !isOn;
            onChanged(isOn);
            track.Background          = new SolidColorBrush(isOn ? onColor : offColor);
            thumb.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        };

        return track;
    }

    private FrameworkElement BuildColorSwatches(TagColor tag)
    {
        var currentHex = _pendingHexColors.GetValueOrDefault(tag, tag.HexColor ?? "#7A8593");
        var grid = new UniformGrid { Columns = 3, Rows = 3 };

        foreach (var hex in PresetHexColors)
        {
            var h         = hex;
            var isSelected = string.Equals(currentHex, h, StringComparison.OrdinalIgnoreCase);
            var swatch    = BuildColorSwatch(h, isSelected, tag);
            grid.Children.Add(swatch);
        }

        return grid;
    }

    private Border BuildColorSwatch(string hex, bool isSelected, TagColor tag)
    {
        var outer = new Border
        {
            Width         = 36,
            Height        = 36,
            CornerRadius  = new CornerRadius(18),
            BorderBrush   = isSelected ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : Brushes.Transparent,
            BorderThickness = new Thickness(isSelected ? 2.5 : 0),
            Padding       = new Thickness(isSelected ? 3 : 4),
            Margin        = new Thickness(4),
            Cursor        = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        outer.Child = new Ellipse
        {
            Fill = new SolidColorBrush(ParseHex(hex)),
        };
        outer.MouseLeftButtonUp += (_, _) => SelectColor(hex, tag);
        return outer;
    }

    private void SelectColor(string hex, TagColor tag)
    {
        _pendingHexColors[tag] = hex;
        RefreshRightPanel(tag);
        RefreshLeftPanelSwatch(tag);
    }

    private void RefreshLeftPanelSwatch(TagColor tag)
    {
        if (!_tagRows.TryGetValue(tag, out var row)) return;
        var hex = _pendingHexColors.GetValueOrDefault(tag, tag.HexColor ?? "#7A8593");
        if (row.Child is Grid g && g.Children.Count > 0 && g.Children[0] is Grid swatchGrid
            && swatchGrid.Children.Count > 0 && swatchGrid.Children[0] is Ellipse ellipse)
        {
            ellipse.Fill = new SolidColorBrush(ParseHex(hex));
        }
    }

    private FrameworkElement BuildIconGrid(TagColor tag)
    {
        var currentIndex = _pendingIconIndexes.GetValueOrDefault(tag, -1);
        var grid = new UniformGrid { Columns = 5 };

        for (var i = 0; i < IconHelper.IconPaths.Length; i++)
        {
            var idx        = i;
            var isSelected = currentIndex == idx;
            var iconCell   = BuildIconCell(idx, isSelected, tag);
            grid.Children.Add(iconCell);
        }

        return grid;
    }

    private Border BuildIconCell(int iconIndex, bool isSelected, TagColor tag)
    {
        var hex = _pendingHexColors.GetValueOrDefault(tag, tag.HexColor ?? "#7A8593");

        var cell = new Border
        {
            Width           = 48,
            Height          = 48,
            CornerRadius    = new CornerRadius(8),
            BorderBrush     = isSelected ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            Background      = isSelected ? new SolidColorBrush(ParseHex(hex)) : new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            Margin          = new Thickness(3),
            Cursor          = Cursors.Hand,
        };

        var iconBrush = isSelected ? Brushes.White : (Brush)new SolidColorBrush(Color.FromRgb(80, 80, 80));
        cell.Child = IconHelper.CreateIconElement(iconIndex, 24, iconBrush);

        cell.MouseLeftButtonUp += (_, _) => SelectIcon(iconIndex, tag);
        return cell;
    }

    private void SelectIcon(int iconIndex, TagColor tag)
    {
        _pendingIconIndexes[tag] = iconIndex;
        RefreshRightPanel(tag);
        RefreshLeftPanelIcon(tag);
    }

    private void RefreshLeftPanelIcon(TagColor tag)
    {
        // Rebuild the left panel row's swatch icon
        if (!_tagRows.TryGetValue(tag, out var row)) return;
        if (row.Child is not Grid rowContent) return;
        if (rowContent.Children.Count == 0 || rowContent.Children[0] is not Grid swatchGrid) return;

        // Remove old icon (index 1+), keep the ellipse at 0
        while (swatchGrid.Children.Count > 1)
            swatchGrid.Children.RemoveAt(swatchGrid.Children.Count - 1);

        var iconIndex = _pendingIconIndexes.GetValueOrDefault(tag, -1);
        if (iconIndex >= 0 && iconIndex < IconHelper.IconPaths.Length)
            swatchGrid.Children.Add(IconHelper.CreateIconElement(iconIndex, 13, Brushes.White));
    }

    private void OnNameBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedTag is null || _selectedTag.IsNone || _nameBox is null) return;
        var text = _nameBox.Text;
        _pendingNames[_selectedTag] = text;
        if (_tagNameLabels.TryGetValue(_selectedTag, out var label))
            label.Text = text;
    }

    private FrameworkElement BuildFooter()
    {
        var L      = AppServices.Localize;
        var border = new Border
        {
            BorderBrush     = (Brush)(Application.Current.TryFindResource("BorderBrush") ?? Brushes.LightGray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding         = new Thickness(16, 10, 16, 10),
            Background      = Brushes.White,
        };

        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        border.Child = footer;

        // 削除ボタン（非機能 UI）
        var deleteBtn = new Button
        {
            Content   = L["TagDeleteBtn"],
            IsEnabled = false,
            Padding   = new Thickness(14, 7, 14, 7),
            MinWidth  = 80,
            Foreground = new SolidColorBrush(Color.FromRgb(196, 43, 28)),
            Style     = (Style?)Application.Current.TryFindResource("SecondaryButton"),
        };
        Grid.SetColumn(deleteBtn, 0);
        footer.Children.Add(deleteBtn);

        var cancel = new Button
        {
            Content  = L["Cancel"],
            Style    = (Style?)Application.Current.TryFindResource("SecondaryButton"),
            Padding  = new Thickness(18, 7, 18, 7),
            Margin   = new Thickness(8, 0, 8, 0),
            MinWidth = 80,
        };
        cancel.Click += (_, _) => Close();
        Grid.SetColumn(cancel, 2);
        footer.Children.Add(cancel);

        var save = new Button
        {
            Content  = L["Save"],
            Style    = (Style?)Application.Current.TryFindResource("PrimaryButton"),
            Padding  = new Thickness(20, 7, 20, 7),
            MinWidth = 80,
        };
        save.Click += (_, _) => SaveAndClose();
        Grid.SetColumn(save, 3);
        footer.Children.Add(save);

        return border;
    }

    private void SaveAndClose()
    {
        foreach (var (tag, name) in _pendingNames)
            TagSettingsService.SetDisplayName(tag, name);

        foreach (var (tag, hex) in _pendingHexColors)
            TagSettingsService.SetTagHexColor(tag, hex);

        foreach (var (tag, idx) in _pendingIconIndexes)
            if (idx >= 0) TagSettingsService.SetTagIconIndex(tag, idx);

        TagSettingsService.SetShowTagNameOnIcon(_showTagNameOnIcon);
        TagSettingsService.SetShowTagIconOnIcon(_showTagIconOnIcon);
        DialogResult = true;
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
