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

    public sealed record LanguageDefinition(
        string Code,
        string CultureName,
        string DisplayNameKey,
        string ContextMenuTitle);

    public static readonly IReadOnlyList<LanguageDefinition> SupportedLanguages =
    [
        new("en", "en", "LanguageEnglish", "Customize with Folderly"),
        new("es", "es", "LanguageSpanish", "Personalizar con Folderly"),
        new("ja", "ja", "LanguageJapanese", "Folderly でカスタマイズ"),
    ];

    private static readonly ResourceManager _rm =
        new("Folderly.App.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en");
    private string _currentLang = "en";

    private LocalizationService() { }

    /// <summary>キーに対応するローカライズ文字列を返す。未定義キーはキー名をそのまま返す。</summary>
    public string this[string key] => _rm.GetString(key, _culture) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 言語を切り替える。"system" または SupportedLanguages の Code を受け付ける。
    /// 切り替え後、全インデクサバインディングが WPF により再評価される。
    /// </summary>
    public void SetLanguage(string lang)
    {
        var definition = ResolveLanguage(NormalizeLanguageSetting(lang), CultureInfo.CurrentUICulture);
        _culture = CultureInfo.GetCultureInfo(definition.CultureName);
        _currentLang = definition.Code;

        // Binding.IndexerName = "Item[]" → WPF がインデクサバインディング全体を再評価する
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }

    public static string NormalizeLanguageSetting(string? lang)
    {
        if (string.Equals(lang, "system", StringComparison.OrdinalIgnoreCase))
            return "system";

        var definition = FindSupportedLanguage(lang);
        return definition?.Code ?? "system";
    }

    public static string GetContextMenuTitle(string? savedLang, CultureInfo currentUiCulture)
        => ResolveLanguage(NormalizeLanguageSetting(savedLang), currentUiCulture).ContextMenuTitle;

    private static LanguageDefinition ResolveLanguage(string normalizedLang, CultureInfo currentUiCulture)
    {
        if (normalizedLang != "system")
            return FindSupportedLanguage(normalizedLang) ?? English;

        return ResolveSystemLanguage(currentUiCulture);
    }

    private static LanguageDefinition ResolveSystemLanguage(CultureInfo currentUiCulture)
    {
        var name = currentUiCulture.Name;
        var twoLetter = currentUiCulture.TwoLetterISOLanguageName;

        if (FindSupportedLanguage(name) is { } exact)
            return exact;

        if (FindSupportedLanguage(twoLetter) is { } twoLetterMatch)
            return twoLetterMatch;

        if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
            && FindSupportedLanguage("pt-BR") is { } portugueseBrazil)
            return portugueseBrazil;

        if ((name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
             || name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
             || name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase))
            && FindSupportedLanguage("zh-Hans") is { } chineseSimplified)
            return chineseSimplified;

        return English;
    }

    private static LanguageDefinition? FindSupportedLanguage(string? lang)
        => SupportedLanguages.FirstOrDefault(l =>
            string.Equals(l.Code, lang, StringComparison.OrdinalIgnoreCase)
            || string.Equals(l.CultureName, lang, StringComparison.OrdinalIgnoreCase));

    private static LanguageDefinition English
        => SupportedLanguages.First(l => l.Code == "en");

    /// <summary>現在有効な言語コードを返す（例: "en" / "ja" / "es"）。</summary>
    public string CurrentLang => _currentLang;
}
