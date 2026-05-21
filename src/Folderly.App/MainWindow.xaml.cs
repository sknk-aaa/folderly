using Folderly.App.Infrastructure;
using Folderly.App.ViewModels;
using Folderly.App.Views;
using System.Diagnostics;
using System.Windows;

namespace Folderly.App;

/// <summary>
/// メイン管理画面（SPEC Section 4.2, F-11, F-13）。
/// スタートメニューから起動される。履歴一覧と「元に戻す」を提供する。
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        AppServices.Localize.PropertyChanged += (_, _) => _vm.Notify(nameof(_vm.L));
        AppServices.License.LicenseChanged   += (_, _) => Dispatcher.Invoke(_vm.RefreshLicense);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.Refresh();
        await AppServices.License.InitializeAsync();
        _vm.RefreshLicense();
    }

    // ─── タブ ────────────────────────────────────────────────────────────────

    private void HistoryTab_Click(object sender, RoutedEventArgs e) => _vm.Refresh();

    public void RefreshHistory() => _vm.Refresh();

    private void SettingsTab_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow { Owner = this };
        win.ShowDialog();
        _vm.Refresh();
    }

    private void HelpTab_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Folderly v1.0\n\nFor support, please visit our website.",
            "Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ─── 履歴アクション ──────────────────────────────────────────────────────

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn &&
            btn.Tag is HistoryItemViewModel item)
        {
            try { Process.Start("explorer.exe", $"\"{item.FolderPath}\""); }
            catch { /* サイレント無視 */ }
        }
    }

    private async void Revert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn ||
            btn.Tag is not HistoryItemViewModel item) return;

        var L   = AppServices.Localize;
        var msg = string.Format(L["RevertConfirmMessage"], item.FolderPath);
        var res = MessageBox.Show(msg, L["RevertConfirmTitle"],
            MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (res != MessageBoxResult.OK) return;

        try
        {
            await AppServices.Revert.RevertAsync(item.FolderPath);
            _vm.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(L["RevertFailed"], ex.Message),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClearAllHistory_Click(object sender, RoutedEventArgs e)
    {
        var L   = AppServices.Localize;
        var res = MessageBox.Show(L["ClearHistoryConfirmMessage"], L["ClearHistoryConfirmTitle"],
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (res != MessageBoxResult.OK) return;

        var result = await AppServices.Revert.RevertAllAsync();

        if (result.FailCount > 0)
            MessageBox.Show(string.Format(L["RevertAllPartialFailed"], result.FailCount),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Warning);

        _vm.Refresh();
    }

    private void BuyNow_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-windows-store://") { UseShellExecute = true }); }
        catch { /* サイレント無視 */ }
    }

    // ─── 外部から ApplyWindow を開く（単一インスタンス制御用） ───────────────

    public void OpenApplyWindow(string folderPath)
    {
        var win = new ApplyWindow(folderPath) { Owner = this };
        win.Closed += (_, _) => _vm.Refresh();
        win.Show();
        Activate();
    }
}
