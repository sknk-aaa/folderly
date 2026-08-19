using Folderly.App.Infrastructure;
using System.Diagnostics;

namespace Folderly.App.Services;

public static class SupportNavigationService
{
    private const string ContactFormJa = "https://tally.so/r/PdZEN0";
    private const string ContactFormEn = "https://tally.so/r/PdZEN0";
    private const string FaqJa = "https://folderlyapp.com/privacy/#faq-ja";
    private const string FaqEn = "https://folderlyapp.com/privacy/#faq";

    public static void OpenContactForm()
        => Open(AppServices.Localize.CurrentLang == "ja" ? ContactFormJa : ContactFormEn);

    public static void OpenFaq()
        => Open(AppServices.Localize.CurrentLang == "ja" ? FaqJa : FaqEn);

    private static void Open(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch { }
    }
}
