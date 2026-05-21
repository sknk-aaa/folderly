using Folderly.App.Infrastructure;
using Folderly.App.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace Folderly.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm = new();

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        var L   = AppServices.Localize;
        var res = MessageBox.Show(
            L["ClearHistoryConfirmMessage"], L["ClearHistoryConfirmTitle"],
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (res != MessageBoxResult.OK) return;

        var result = await AppServices.Revert.RevertAllAsync();

        if (result.FailCount > 0)
            MessageBox.Show(string.Format(L["RevertAllPartialFailed"], result.FailCount),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Support_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/s-knk/folderly/issues") { UseShellExecute = true }); }
        catch { /* サイレント無視 */ }
    }

    private void LicenseInfo_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-windows-store://") { UseShellExecute = true }); }
        catch { /* サイレント無視 */ }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _vm.Save();
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _vm.Save();
        base.OnClosing(e);
    }
}
