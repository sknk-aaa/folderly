using Folderly.App.Infrastructure;
using Folderly.Core.Composition;

namespace Folderly.App.Services;

public static class TagSettingsService
{
    public const string ShowTagNameOnIconKey = "show_tag_name_on_icon";

    public static string GetDisplayName(TagColor tag)
    {
        if (tag.IsNone)
            return AppServices.Localize["TagNone"];

        var saved = AppServices.History.GetSetting(GetNameKey(tag));
        return string.IsNullOrWhiteSpace(saved) ? GetDefaultName(tag) : saved.Trim();
    }

    public static void SetDisplayName(TagColor tag, string name)
    {
        if (tag.IsNone) return;

        var clean = string.IsNullOrWhiteSpace(name)
            ? GetDefaultName(tag)
            : name.Trim();
        AppServices.History.SetSetting(GetNameKey(tag), clean);
    }

    public static bool GetShowTagNameOnIcon()
        => AppServices.History.GetSetting(ShowTagNameOnIconKey) == "true";

    public static void SetShowTagNameOnIcon(bool value)
        => AppServices.History.SetSetting(ShowTagNameOnIconKey, value ? "true" : "false");

    public static string GetDefaultName(TagColor tag)
    {
        var key = tag.Key switch
        {
            "blue" => "TagBlue",
            "green" => "TagGreen",
            "orange" => "TagOrange",
            "purple" => "TagPurple",
            "red" => "TagRed",
            "gray" => "TagGray",
            _ => "TagNone",
        };

        var label = AppServices.Localize[key];
        return label.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim()
               ?? tag.Key;
    }

    private static string GetNameKey(TagColor tag)
        => $"tag_name.{tag.Key}";
}
