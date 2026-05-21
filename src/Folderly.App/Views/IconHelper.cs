using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Folderly.Core.Composition;
using WpfPath = System.Windows.Shapes.Path;

namespace Folderly.App.Views;

internal static class IconHelper
{
    internal static readonly string[] IconPaths = TagIconLibrary.IconPaths;

    internal static FrameworkElement CreateIconElement(int iconIndex, double size, Brush stroke)
    {
        if (iconIndex == 5) // dots: 3 filled circles
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            double[] xs = [5, 12, 19];
            foreach (var x in xs)
            {
                const double r = 1.8;
                var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = stroke };
                Canvas.SetLeft(e, x - r);
                Canvas.SetTop(e, 12 - r);
                canvas.Children.Add(e);
            }
            return new Viewbox { Width = size, Height = size, Child = canvas };
        }

        if (iconIndex < 0 || iconIndex >= IconPaths.Length) return new FrameworkElement();

        var pathData = IconPaths[iconIndex];
        if (string.IsNullOrEmpty(pathData)) return new FrameworkElement();

        return new Viewbox
        {
            Width  = size,
            Height = size,
            Child  = new Canvas
            {
                Width  = 24,
                Height = 24,
                Children =
                {
                    new WpfPath
                    {
                        Data               = Geometry.Parse(pathData),
                        Stroke             = stroke,
                        StrokeThickness    = 1.8,
                        StrokeLineJoin     = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap   = PenLineCap.Round,
                        Fill               = Brushes.Transparent,
                    }
                }
            }
        };
    }
}
