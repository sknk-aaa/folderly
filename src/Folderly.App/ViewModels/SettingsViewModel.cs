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
            SetField(ref _selectedLang, value);
            Notify(nameof(IsSystemLang));
            Notify(nameof(IsJaLang));
            Notify(nameof(IsEnLang));
        }
    }

    public bool IsSystemLang { get => SelectedLang == "system"; set { if (value) SelectedLang = "system"; } }
    public bool IsJaLang     { get => SelectedLang == "ja";     set { if (value) SelectedLang = "ja";     } }
    public bool IsEnLang     { get => SelectedLang == "en";     set { if (value) SelectedLang = "en";     } }

    // ─── 履歴 ─────────────────────────────────────────────────────────────────

    private int _historyMaxCount;
    public int HistoryMaxCount
    {
        get => _historyMaxCount;
        set => SetField(ref _historyMaxCount, Math.Clamp(value, 1, 1000));
    }

    private bool _forceExplorerRestartAfterApply;
    public bool ForceExplorerRestartAfterApply
    {
        get => _forceExplorerRestartAfterApply;
        set => SetField(ref _forceExplorerRestartAfterApply, value);
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
        _historyMaxCount = int.TryParse(AppServices.History.GetSetting("history_max_count"), out var n) ? n : 100;
        _forceExplorerRestartAfterApply =
            AppServices.History.GetSetting("force_explorer_restart_on_reapply") != "false";
    }

    // ─── 保存 ─────────────────────────────────────────────────────────────────

    public void Save()
    {
        AppServices.History.SetSetting("language", SelectedLang);
        AppServices.History.SetSetting("history_max_count", HistoryMaxCount.ToString());
        AppServices.History.SetSetting(
            "force_explorer_restart_on_reapply",
            ForceExplorerRestartAfterApply ? "true" : "false");
        AppServices.Localize.SetLanguage(SelectedLang);
        AppServices.History.EnforceMaxCount(HistoryMaxCount);
    }
}
