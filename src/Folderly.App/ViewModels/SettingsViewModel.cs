using Folderly.App.Infrastructure;
using Folderly.App.Services;
using System.Reflection;

namespace Folderly.App.ViewModels;

/// <summary>設定画面の ViewModel（SPEC Section 4.3, F-14, F-15, F-16）。</summary>
public sealed class SettingsViewModel : ViewModelBase
{
    public LocalizationService L => LocalizationService.Instance;

    // ─── 言語 ─────────────────────────────────────────────────────────────────

    private string _selectedLang;
    public string SelectedLang
    {
        get => _selectedLang;
        set
        {
            if (_selectedLang == value)
                return;

            _selectedLang = value;
            Notify();
            Notify(nameof(IsSystemLang));
            Notify(nameof(IsJaLang));
            Notify(nameof(IsEnLang));
            AppServices.History.SetSetting("language", _selectedLang);
            AppServices.Localize.SetLanguage(_selectedLang);
        }
    }

    public bool IsSystemLang { get => SelectedLang == "system"; set { if (value) SelectedLang = "system"; } }
    public bool IsJaLang     { get => SelectedLang == "ja";     set { if (value) SelectedLang = "ja";     } }
    public bool IsEnLang     { get => SelectedLang == "en";     set { if (value) SelectedLang = "en";     } }

    // ─── 履歴 ─────────────────────────────────────────────────────────────────

    private bool _reopenExplorerWindowsAfterApply;
    public bool ReopenExplorerWindowsAfterApply
    {
        get => _reopenExplorerWindowsAfterApply;
        set => SetField(ref _reopenExplorerWindowsAfterApply, value);
    }

    private bool _showTagNameOnIcon;
    public bool ShowTagNameOnIcon
    {
        get => _showTagNameOnIcon;
        set => SetField(ref _showTagNameOnIcon, value);
    }

    private bool _showTagIconOnIcon;
    public bool ShowTagIconOnIcon
    {
        get => _showTagIconOnIcon;
        set => SetField(ref _showTagIconOnIcon, value);
    }

    // ─── バージョン・ライセンス ───────────────────────────────────────────────

    public string AppVersion
        => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string LicenseText
    {
        get
        {
            var lic = AppServices.License;
            if (!lic.IsTrial) return L["LicenseFull"];
            return string.Format(L["LicenseTrial"], lic.DaysRemaining);
        }
    }

    // ─── コンストラクタ ───────────────────────────────────────────────────────

    public SettingsViewModel()
    {
        _selectedLang    = AppServices.History.GetSetting("language") ?? "system";
        _reopenExplorerWindowsAfterApply =
            AppServices.History.GetSetting("force_explorer_restart_on_reapply") != "false";
        _showTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon();
        _showTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon();
    }

    // ─── 保存 ─────────────────────────────────────────────────────────────────

    public void Save()
    {
        AppServices.History.SetSetting("language", SelectedLang);
        AppServices.History.SetSetting(
            "force_explorer_restart_on_reapply",
            ReopenExplorerWindowsAfterApply ? "true" : "false");
        TagSettingsService.SetShowTagNameOnIcon(ShowTagNameOnIcon);
        TagSettingsService.SetShowTagIconOnIcon(ShowTagIconOnIcon);
        AppServices.Localize.SetLanguage(SelectedLang);
    }
}
