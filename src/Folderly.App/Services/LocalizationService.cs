using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;

namespace Folderly.App.Services;

/// <summary>
/// 多言語対応サービス。再起動不要で即時切替可能（SPEC F-15）。
///
/// XAML バインディング例:
///   Text="{Binding L[Apply], Source={x:Static svc:LocalizationService.Instance}}"
/// または ViewModel に public LocalizationService L => LocalizationService.Instance; を追加して
///   Text="{Binding L[Apply]}"
///
/// SetLanguage() を呼ぶと PropertyChanged("Item[]") が発火し、
/// WPF がインデクサバインディングを再評価する。
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static readonly LocalizationService Instance = new();

    private static readonly ResourceManager _rm =
        new("Folderly.App.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en");

    private LocalizationService() { }

    /// <summary>キーに対応するローカライズ文字列を返す。未定義キーはキー名をそのまま返す。</summary>
    public string this[string key] => _rm.GetString(key, _culture) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 言語を切り替える。"system" / "ja" / "en" を受け付ける。
    /// 切り替え後、全インデクサバインディングが WPF により再評価される。
    /// </summary>
    public void SetLanguage(string lang)
    {
        _culture = lang switch
        {
            "ja"     => CultureInfo.GetCultureInfo("ja"),
            "en"     => CultureInfo.GetCultureInfo("en"),
            "system" => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja"
                           ? CultureInfo.GetCultureInfo("ja")
                           : CultureInfo.GetCultureInfo("en"),
            _        => CultureInfo.GetCultureInfo("en"),
        };

        // Binding.IndexerName = "Item[]" → WPF がインデクサバインディング全体を再評価する
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }

    /// <summary>現在の言語コードを返す（"en" / "ja"）。</summary>
    public string CurrentLang => _culture.TwoLetterISOLanguageName;
}
