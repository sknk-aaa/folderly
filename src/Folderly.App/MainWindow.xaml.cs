using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.App.ViewModels;
using Folderly.App.Views;
using Folderly.Core;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Folderly.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private readonly SettingsViewModel _settingsVm = new();
    private MainTab _activeTab = MainTab.History;
    private bool _hasCheckedFirstRunOnboarding;

    public SettingsViewModel SettingsContext => _settingsVm;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        AppServices.Localize.PropertyChanged += (_, _) =>
        {
            _vm.Notify(nameof(_vm.L));
            _settingsVm.Notify(nameof(_settingsVm.L));
            _settingsVm.Notify(nameof(_settingsVm.LanguageOptions));
            _settingsVm.Notify(nameof(_settingsVm.LicenseText));
        };
        AppServices.License.LicenseChanged += (_, _) => Dispatcher.Invoke(() =>
        {
            _vm.RefreshLicense();
            _settingsVm.Notify(nameof(_settingsVm.LicenseText));
        });
    }

    private enum MainTab
    {
        History,
        Settings,
        Help
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.Refresh();
        _vm.RefreshLicense();
        _settingsVm.Notify(nameof(_settingsVm.LicenseText));
        QueueLicenseRefresh();
        ShowFirstRunOnboardingIfNeeded();
    }

    private void QueueLicenseRefresh()
    {
        _ = Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                await Task.Delay(1500);
                await AppServices.License.InitializeAsync();
                _vm.RefreshLicense();
                _settingsVm.Notify(nameof(_settingsVm.LicenseText));
            }
            catch
            {
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    private void HistoryTab_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(MainTab.History);
        _vm.Refresh();
    }

    public void RefreshHistory() => _vm.Refresh();

    private void SettingsTab_Click(object sender, RoutedEventArgs e) => ShowTab(MainTab.Settings);

    private void HelpTab_Click(object sender, RoutedEventArgs e) => ShowTab(MainTab.Help);

    private void ShowTab(MainTab tab)
    {
        if (_activeTab == MainTab.Settings)
            SaveSettings();

        _activeTab = tab;

        HistoryPanel.Visibility = tab == MainTab.History ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = tab == MainTab.Settings ? Visibility.Visible : Visibility.Collapsed;
        HelpPanel.Visibility = tab == MainTab.Help ? Visibility.Visible : Visibility.Collapsed;

        SetTabSelected(HistoryTabBtn, tab == MainTab.History);
        SetTabSelected(SettingsTabBtn, tab == MainTab.Settings);
        SetTabSelected(HelpTabBtn, tab == MainTab.Help);
    }

    private void SetTabSelected(Button button, bool selected)
    {
        button.Foreground = (Brush)FindResource(selected ? "PrimaryBrush" : "TextSecondaryBrush");
        button.BorderBrush = selected ? (Brush)FindResource("PrimaryBrush") : Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(0, 0, 0, 2) : new Thickness(0);
    }

    private void SaveSettings()
    {
        _settingsVm.Save();
        _vm.RefreshLicense();
        _settingsVm.Notify(nameof(_settingsVm.LicenseText));
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not HistoryItemViewModel item)
            return;

        try { Process.Start("explorer.exe", $"\"{item.FolderPath}\""); }
        catch { }
    }

    private async void Revert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not HistoryItemViewModel item)
            return;

        var L = AppServices.Localize;

        if (!Directory.Exists(item.FolderPath))
        {
            switch (MovedFolderDialog.Show(this, item.FolderPath))
            {
                case MovedFolderChoice.Locate:
                    await LocateAndRevertAsync(item.FolderPath);
                    break;
                case MovedFolderChoice.HistoryOnly:
                    try
                    {
                        AppServices.Revert.DeleteHistoryOnly(item.FolderPath);
                        _vm.Refresh();
                    }
                    catch (Exception ex)
                    {
                        ShowRevertError(ex);
                    }
                    break;
            }
            return;
        }

        var msg = string.Format(L["RevertConfirmMessage"], item.FolderPath);
        var res = MessageBox.Show(msg, L["RevertConfirmTitle"],
            MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (res != MessageBoxResult.OK)
            return;

        try
        {
            await AppServices.Revert.RevertAsync(item.FolderPath);
            _vm.Refresh();
        }
        catch (Exception ex)
        {
            ShowRevertError(ex);
        }
    }

    private async Task LocateAndRevertAsync(string historyKeyPath)
    {
        var L = AppServices.Localize;

        var picker = new OpenFolderDialog { Title = L["FolderMovedLocateTitle"] };
        if (picker.ShowDialog(this) != true)
            return;

        var newPath = picker.FolderName;
        var entry = AppServices.History.GetByPath(Path.GetFullPath(historyKeyPath));
        if (entry is not null && entry.IconHash is { Length: >= 8 })
        {
            var coverPath = Path.Combine(
                newPath, FolderlyConstants.FolderlyDirectoryName, $"cover_{entry.IconHash[..8]}.ico");
            if (!File.Exists(coverPath))
            {
                var warn = MessageBox.Show(L["FolderMismatchMessage"], L["FolderMismatchTitle"],
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (warn != MessageBoxResult.OK)
                    return;
            }
        }

        try
        {
            await AppServices.Revert.RevertAsync(historyKeyPath, newPath);
            _vm.Refresh();
        }
        catch (Exception ex)
        {
            ShowRevertError(ex);
        }
    }

    private void ShowRevertError(Exception ex)
        => MessageBox.Show(string.Format(AppServices.Localize["RevertFailed"], ex.Message),
            "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);

    private async void ClearAllHistory_Click(object sender, RoutedEventArgs e)
    {
        var L = AppServices.Localize;
        var res = MessageBox.Show(L["ClearHistoryConfirmMessage"], L["ClearHistoryConfirmTitle"],
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (res != MessageBoxResult.OK)
            return;

        var result = await AppServices.Revert.RevertAllAsync();

        if (result.FailCount > 0)
            MessageBox.Show(string.Format(L["RevertAllPartialFailed"], result.FailCount),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Warning);

        _vm.Refresh();
    }

    private void BuyNow_Click(object sender, RoutedEventArgs e)
    {
        StoreNavigationService.OpenProductPage();
    }

    private void Support_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        SupportNavigationService.OpenContactForm();
    }

    private void Faq_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        SupportNavigationService.OpenFaq();
    }

    private void Onboarding_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        OnboardingDialog.Show(this);
    }

    private void LicenseInfo_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        StoreNavigationService.OpenProductPage();
    }

    private void Review_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        StoreNavigationService.OpenReviewPage();
    }

    private void EditTagNames_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        var dialog = new TagSettingsDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settingsVm.ShowTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon();
            _settingsVm.ShowTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon();
        }
    }

    public void OpenApplyWindow(string folderPath)
    {
        var win = new ApplyWindow(folderPath) { Owner = this };
        win.Closed += (_, _) => _vm.Refresh();
        win.Show();
        win.Activate();
        win.Topmost = true;
        win.Topmost = false;
        win.Focus();
    }

    private void ShowFirstRunOnboardingIfNeeded()
    {
        if (_hasCheckedFirstRunOnboarding || !OnboardingService.ShouldShowFirstRun())
            return;

        _hasCheckedFirstRunOnboarding = true;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsVisible)
                return;

            OnboardingDialog.Show(this);
            OnboardingService.MarkSeen();
        }, DispatcherPriority.ApplicationIdle);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveSettings();
        base.OnClosing(e);
    }
}
