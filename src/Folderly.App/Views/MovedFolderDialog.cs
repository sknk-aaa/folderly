using System.Windows;
using System.Windows.Controls;
using Folderly.App.Infrastructure;

namespace Folderly.App.Views;

public enum MovedFolderChoice { Cancel, Locate, HistoryOnly }

/// <summary>
/// カスタマイズ済みフォルダが移動・改名・削除されていて解除できないときに表示する 3 択ダイアログ。
/// 「フォルダを指定して解除」「履歴のみ削除」「キャンセル」を選ばせる。
/// </summary>
public static class MovedFolderDialog
{
    public static MovedFolderChoice Show(Window owner, string folderPath)
    {
        var L = AppServices.Localize;
        var result = MovedFolderChoice.Cancel;

        var win = new Window
        {
            Title                 = L["FolderMovedTitle"],
            Owner                 = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent         = SizeToContent.WidthAndHeight,
            ResizeMode            = ResizeMode.NoResize,
            ShowInTaskbar         = false,
            MinWidth              = 440,
            MaxWidth              = 560,
        };

        var root = new StackPanel { Margin = new Thickness(22) };

        root.Children.Add(new TextBlock
        {
            Text         = string.Format(L["FolderMovedMessage"], folderPath),
            TextWrapping = TextWrapping.Wrap,
            Margin       = new Thickness(0, 0, 0, 18),
            FontSize     = 13,
        });

        Button MakeChoiceButton(string text, MovedFolderChoice choice, bool primary)
        {
            var b = new Button
            {
                Content                    = text,
                Padding                    = new Thickness(14, 9, 14, 9),
                Margin                     = new Thickness(0, 0, 0, 8),
                FontSize                   = 13,
                FontWeight                 = primary ? FontWeights.SemiBold : FontWeights.Normal,
                HorizontalAlignment        = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            b.Click += (_, _) => { result = choice; win.Close(); };
            return b;
        }

        root.Children.Add(MakeChoiceButton(L["FolderMovedLocate"], MovedFolderChoice.Locate, true));
        root.Children.Add(MakeChoiceButton(L["FolderMovedHistoryOnly"], MovedFolderChoice.HistoryOnly, false));

        var cancel = new Button
        {
            Content             = L["Cancel"],
            Padding             = new Thickness(14, 7, 14, 7),
            Margin              = new Thickness(0, 4, 0, 0),
            FontSize            = 13,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsCancel            = true,
        };
        cancel.Click += (_, _) => { result = MovedFolderChoice.Cancel; win.Close(); };
        root.Children.Add(cancel);

        win.Content = root;
        win.ShowDialog();
        return result;
    }
}
