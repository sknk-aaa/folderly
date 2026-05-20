using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Folderly.App.Infrastructure;

/// <summary>16進カラー文字列（"#RRGGBB" or null）→ System.Windows.Media.Color に変換。</summary>
[ValueConversion(typeof(string), typeof(Color))]
public sealed class HexToColorConverter : IValueConverter
{
    public static readonly HexToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string hex || hex.Length < 7)
            return Colors.LightGray;
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            System.Convert.ToByte(hex.Substring(0, 2), 16),
            System.Convert.ToByte(hex.Substring(2, 2), 16),
            System.Convert.ToByte(hex.Substring(4, 2), 16));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>スケール double → "xxx%" 文字列。</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class ScaleToPercentConverter : IValueConverter
{
    public static readonly ScaleToPercentConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? $"{d * 100:F0}%" : "100%";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>bool → Visibility.Visible / Collapsed。</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibility : IValueConverter
{
    public static readonly BoolToVisibility Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>bool → 反転 Visibility（false → Visible, true → Collapsed）。</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToInvisibility : IValueConverter
{
    public static readonly BoolToInvisibility Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
