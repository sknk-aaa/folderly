using Folderly.App.Infrastructure;

namespace Folderly.App.Services;

public static class ReviewPromptService
{
    private const string ApplyCountKey = "review_prompt.apply_count";
    private const string LastPromptCountKey = "review_prompt.last_prompt_count";
    private const string ReviewOpenedKey = "review_prompt.review_opened";
    private const int FirstPromptApplyCount = 3;
    private const int RepeatPromptInterval = 10;

    public static int? RecordSuccessfulApplyAndGetPromptCount()
    {
        var applyCount = GetInt(ApplyCountKey) + 1;
        AppServices.History.SetSetting(ApplyCountKey, applyCount.ToString());

        if (AppServices.History.GetSetting(ReviewOpenedKey) == "true")
            return null;

        if (applyCount < FirstPromptApplyCount)
            return null;

        var lastPromptCount = GetInt(LastPromptCountKey);
        if (lastPromptCount > 0 && applyCount - lastPromptCount < RepeatPromptInterval)
            return null;

        return applyCount;
    }

    public static void MarkPromptSkipped(int applyCount)
    {
        AppServices.History.SetSetting(LastPromptCountKey, applyCount.ToString());
    }

    public static void MarkReviewOpened()
    {
        AppServices.History.SetSetting(ReviewOpenedKey, "true");
    }

    private static int GetInt(string key)
    {
        var raw = AppServices.History.GetSetting(key);
        return int.TryParse(raw, out var value) ? value : 0;
    }
}
