using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using System.Windows.Media;

namespace Folderly.App.Services;

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
        new("pt-BR", "pt-BR", "LanguagePortugueseBrazil", "Personalizar com Folderly"),
        new("zh-Hans", "zh-Hans", "LanguageChineseSimplified", "使用 Folderly 自定义"),
        new("ja", "ja", "LanguageJapanese", "Folderly でカスタマイズ"),
    ];

    private static readonly ResourceManager ResourceManager =
        new("Folderly.App.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en");
    private string _currentLang = "en";

    private LocalizationService() { }

    public string this[string key] => ResourceManager.GetString(key, _culture) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetLanguage(string lang)
    {
        var definition = ResolveLanguage(NormalizeLanguageSetting(lang), CultureInfo.CurrentUICulture);
        _culture = CultureInfo.GetCultureInfo(definition.CultureName);
        _currentLang = definition.Code;

        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _culture;
        CultureInfo.DefaultThreadCurrentCulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;

        NotifyAll();
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

    private void NotifyAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLang)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HtmlLang)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WpfFontFamilyName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WpfFontFamily)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CssFontFamily)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CssMonospaceFontFamily)));
    }

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

    public string CurrentLang => _currentLang;

    public string HtmlLang => _culture.Name;

    public string WpfFontFamilyName => _currentLang switch
    {
        "ja" => "Yu Gothic UI, Meiryo, Segoe UI Variable, Segoe UI, Microsoft YaHei UI",
        "zh-Hans" => "Microsoft YaHei UI, Segoe UI Variable, Segoe UI, Yu Gothic UI, Meiryo",
        _ => "Segoe UI Variable, Segoe UI, Yu Gothic UI, Meiryo, Microsoft YaHei UI",
    };

    public FontFamily WpfFontFamily => new(WpfFontFamilyName);

    public string CssFontFamily => _currentLang switch
    {
        "ja" => "\"Yu Gothic UI\",\"Meiryo\",\"Segoe UI Variable Text\",\"Segoe UI\",\"Microsoft YaHei UI\",system-ui,sans-serif",
        "zh-Hans" => "\"Microsoft YaHei UI\",\"Segoe UI Variable Text\",\"Segoe UI\",\"Yu Gothic UI\",\"Meiryo\",system-ui,sans-serif",
        _ => "\"Segoe UI Variable Text\",\"Segoe UI\",\"Yu Gothic UI\",\"Meiryo\",\"Microsoft YaHei UI\",system-ui,sans-serif",
    };

    public string CssMonospaceFontFamily => _currentLang switch
    {
        "ja" => "\"Cascadia Mono\",\"Yu Gothic UI\",\"Meiryo\",\"Segoe UI Variable Text\",\"Segoe UI\",monospace,system-ui,sans-serif",
        "zh-Hans" => "\"Cascadia Mono\",\"Microsoft YaHei UI\",\"Segoe UI Variable Text\",\"Segoe UI\",monospace,system-ui,sans-serif",
        _ => "\"Cascadia Mono\",\"Segoe UI Variable Text\",\"Segoe UI\",\"Yu Gothic UI\",\"Meiryo\",monospace,system-ui,sans-serif",
    };
}
