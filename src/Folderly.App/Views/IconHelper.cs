using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfPath = System.Windows.Shapes.Path;

namespace Folderly.App.Views;

internal static class IconHelper
{
    // 15-icon library: SVG path data (24×24 viewBox, stroke-based). Index 5 = dots (special).
    internal static readonly string[] IconPaths =
    [
        "M14.5,4 L20,9.5 L9,20.5 L3,21.5 L4,15.5 Z",                                        // 0: edit/pen
        "M4,6 L20,6 L20,18 L4,18 Z M10,9 L16,12 L10,15 Z",                                   // 1: video/play
        "M4,7 L20,7 L20,20 L4,20 Z M8,7 L8,5 A2,2,0,0,1,10,3 L14,3 A2,2,0,0,1,16,5 L16,7", // 2: briefcase
        "M14,2 L6,2 L6,22 L18,22 L18,6 Z M14,2 L14,6 L18,6 M9,13 L15,13 M9,17 L13,17",      // 3: document
        "M12,4 V16 M7,11 L12,16 L17,11 M5,20 H19",                                            // 4: download
        "",                                                                                     // 5: dots (3 filled circles)
        "M3,3 L21,3 L21,21 L3,21 Z M3,16 L8,11 L12,15 L16,11 L21,16",                        // 6: photo/image
        "M9,18 L9,5 L21,3 L21,16 M6,21 A3,3,0,0,0,12,21 M18,18 A3,3,0,0,0,24,18",           // 7: music note
        "M7,10 L17,10 A5,5,0,0,1,22,15 L22,17 A5,5,0,0,1,17,22 L7,22 A5,5,0,0,1,2,17 L2,15 A5,5,0,0,1,7,10 Z M8,15 L8,17 M7,16 L9,16 M15,16 H17 M16,15 V17", // 8: game controller
        "M3,5 L3,19 A2,2,0,0,0,5,21 L19,21 A2,2,0,0,0,21,19 L21,5 A2,2,0,0,0,19,3 L5,3 A2,2,0,0,0,3,5 Z M3,10 L21,10 M8,3 L8,10", // 9: book
        "M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z", // 10: star
        "M3,5 A2,2,0,0,1,5,3 L19,3 A2,2,0,0,1,21,5 L21,15 A2,2,0,0,1,19,17 L12,17 L7,22 L7,17 L5,17 A2,2,0,0,1,3,15 Z", // 11: chat bubble
        "M12,22 A10,10,0,0,1,2,12 A10,10,0,0,1,12,2 A10,10,0,0,1,22,12 A4,4,0,0,0,18,16 A4,4,0,0,0,14,20 Z", // 12: palette
        "M5,3 L19,3 L19,21 L12,15.5 L5,21 Z",                                                // 13: bookmark
        "M6,10 L6,21 L18,21 L18,10 Z M8,10 L8,7 A4,4,0,0,1,16,7 L16,10",                    // 14: lock
    ];

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
