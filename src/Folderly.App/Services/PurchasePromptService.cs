using Folderly.App.Infrastructure;

namespace Folderly.App.Services;

public static class PurchasePromptService
{
    private const string TrialApplyCountKey = "purchase_prompt.trial_apply_count";
    private const string LastPromptCountKey = "purchase_prompt.last_prompt_count";
    private const int FirstPromptApplyCount = 3;
    private const int RepeatPromptInterval = 10;

    public static int? RecordTrialSuccessfulApplyAndGetPromptCount()
    {
        var applyCount = GetInt(TrialApplyCountKey) + 1;
        AppServices.History.SetSetting(TrialApplyCountKey, applyCount.ToString());

        if (applyCount < FirstPromptApplyCount)
            return null;

        var lastPromptCount = GetInt(LastPromptCountKey);
        if (lastPromptCount > 0 && applyCount - lastPromptCount < RepeatPromptInterval)
            return null;

        return applyCount;
    }

    public static void MarkPromptHandled(int applyCount)
    {
        AppServices.History.SetSetting(LastPromptCountKey, applyCount.ToString());
    }

    private static int GetInt(string key)
    {
        var raw = AppServices.History.GetSetting(key);
        return int.TryParse(raw, out var value) ? value : 0;
    }
}
