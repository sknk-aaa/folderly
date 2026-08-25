using Folderly.App.Infrastructure;
using System.Diagnostics;

namespace Folderly.App.Services;

public static class SupportNavigationService
{
    private const string ContactFormJa = "https://tally.so/r/q48Bqk";
    private const string ContactFormEn = "https://tally.so/r/PdZEN0";
    private const string ContactFormEs = "https://tally.so/r/1AoroW";
    private const string ContactFormPtBr = "https://tally.so/r/yP5l54";
    private const string ContactFormZhHans = "https://tally.so/r/LZLdL1";
    private const string FaqJa = "https://folderlyapp.com/privacy/#faq-ja";
    private const string FaqEn = "https://folderlyapp.com/privacy/#faq";

    public static void OpenContactForm()
        => Open(AppServices.Localize.CurrentLang switch
        {
            "ja" => ContactFormJa,
            "es" => ContactFormEs,
            "pt-BR" => ContactFormPtBr,
            "zh-Hans" => ContactFormZhHans,
            _ => ContactFormEn,
        });

    public static void OpenFaq()
        => Open(AppServices.Localize.CurrentLang switch
        {
            "ja" => FaqJa,
            _ => FaqEn,
        });

    private static void Open(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch { }
    }
}
