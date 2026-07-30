using System.Diagnostics;

namespace Folderly.App.Services;

public static class StoreNavigationService
{
    private const string ProductId = "9N99JH5H91H8";

    public static void OpenProductPage()
    {
        Open($"ms-windows-store://pdp/?ProductId={ProductId}");
    }

    public static void OpenReviewPage()
    {
        Open($"ms-windows-store://review/?ProductId={ProductId}");
    }

    private static void Open(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch { }
    }
}
