using Folderly.App.Infrastructure;
using Folderly.Core.Composition;

namespace Folderly.App.Services;

public static class TagSettingsService
{
    public const string ShowTagNameOnIconKey = "show_tag_name_on_icon";
    public const string ShowTagIconOnIconKey = "show_tag_icon_on_icon";
    private const string TagNamePrefix = "tag_name.";
    private const string TagHexPrefix  = "tag_hex.";
    private const string TagIconPrefix = "tag_icon.";
    private static readonly object Sync = new();
    private static Dictionary<string, string>? _tagNames;
    private static Dictionary<string, string>? _tagHexColors;
    private static Dictionary<string, int>?    _tagIcons;

    // ─── Display name ────────────────────────────────────────────────────────

    public static string GetDisplayName(TagColor tag)
    {
        if (tag.IsNone)
            return AppServices.Localize["TagNone"];

        var saved = GetSavedTagNames().GetValueOrDefault(GetNameKey(tag));
        return string.IsNullOrWhiteSpace(saved) ? GetDefaultName(tag) : saved.Trim();
    }

    public static void SetDisplayName(TagColor tag, string name)
    {
        if (tag.IsNone) return;

        var clean = string.IsNullOrWhiteSpace(name)
            ? GetDefaultName(tag)
            : name.Trim();
        AppServices.History.SetSetting(GetNameKey(tag), clean);
        lock (Sync)
        {
            _tagNames ??= [];
            _tagNames[GetNameKey(tag)] = clean;
        }
    }

    // ─── Hex color override ──────────────────────────────────────────────────

    public static string? GetTagHexColor(TagColor tag)
    {
        if (tag.IsNone || tag.HexColor is null) return tag.HexColor;
        var saved = GetSavedTagHexColors().GetValueOrDefault(GetHexKey(tag));
        return string.IsNullOrWhiteSpace(saved) ? tag.HexColor : saved;
    }

    public static void SetTagHexColor(TagColor tag, string hexColor)
    {
        if (tag.IsNone) return;
        AppServices.History.SetSetting(GetHexKey(tag), hexColor);
        lock (Sync)
        {
            _tagHexColors ??= [];
            _tagHexColors[GetHexKey(tag)] = hexColor;
        }
    }

    // ─── Icon index ──────────────────────────────────────────────────────────

    public static int GetTagIconIndex(TagColor tag)
    {
        if (tag.IsNone) return -1;
        return GetSavedTagIcons().GetValueOrDefault(GetIconKey(tag), -1);
    }

    public static void SetTagIconIndex(TagColor tag, int index)
    {
        if (tag.IsNone) return;
        AppServices.History.SetSetting(GetIconKey(tag), index.ToString());
        lock (Sync)
        {
            _tagIcons ??= [];
            _tagIcons[GetIconKey(tag)] = index;
        }
    }

    // ─── Show tag name on icon ───────────────────────────────────────────────

    public static bool GetShowTagNameOnIcon()
        => AppServices.History.GetSetting(ShowTagNameOnIconKey) == "true";

    public static void SetShowTagNameOnIcon(bool value)
        => AppServices.History.SetSetting(ShowTagNameOnIconKey, value ? "true" : "false");

    public static bool GetShowTagIconOnIcon()
        => AppServices.History.GetSetting(ShowTagIconOnIconKey) == "true";

    public static void SetShowTagIconOnIcon(bool value)
        => AppServices.History.SetSetting(ShowTagIconOnIconKey, value ? "true" : "false");

    // ─── Default name ────────────────────────────────────────────────────────

    public static string GetDefaultName(TagColor tag)
    {
        var key = tag.Key switch
        {
            "blue"   => "TagBlue",
            "green"  => "TagGreen",
            "orange" => "TagOrange",
            "purple" => "TagPurple",
            "red"    => "TagRed",
            "gray"   => "TagGray",
            _        => "TagNone",
        };

        var label = AppServices.Localize[key];
        return label.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim()
               ?? tag.Key;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private static string GetNameKey(TagColor tag) => $"{TagNamePrefix}{tag.Key}";
    private static string GetHexKey(TagColor tag)  => $"{TagHexPrefix}{tag.Key}";
    private static string GetIconKey(TagColor tag)  => $"{TagIconPrefix}{tag.Key}";

    private static IReadOnlyDictionary<string, string> GetSavedTagNames()
    {
        lock (Sync)
            if (_tagNames is not null)
                return _tagNames;

        var loaded = AppServices.History.GetSettingsByPrefix(TagNamePrefix);
        lock (Sync)
        {
            _tagNames ??= new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
            return _tagNames;
        }
    }

    private static IReadOnlyDictionary<string, string> GetSavedTagHexColors()
    {
        lock (Sync)
            if (_tagHexColors is not null)
                return _tagHexColors;

        var loaded = AppServices.History.GetSettingsByPrefix(TagHexPrefix);
        lock (Sync)
        {
            _tagHexColors ??= new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
            return _tagHexColors;
        }
    }

    private static IReadOnlyDictionary<string, int> GetSavedTagIcons()
    {
        lock (Sync)
            if (_tagIcons is not null)
                return _tagIcons;

        var raw = AppServices.History.GetSettingsByPrefix(TagIconPrefix);
        var parsed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in raw)
            if (int.TryParse(v, out var n)) parsed[k] = n;

        lock (Sync)
        {
            _tagIcons ??= parsed;
            return _tagIcons;
        }
    }
}
