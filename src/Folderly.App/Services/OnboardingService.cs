using Folderly.App.Infrastructure;

namespace Folderly.App.Services;

public static class OnboardingService
{
    private const string SeenKey = "onboarding_v1_seen";

    public static bool ShouldShowFirstRun()
        => AppServices.History.GetSetting(SeenKey) != "1";

    public static void MarkSeen()
        => AppServices.History.SetSetting(SeenKey, "1");
}
