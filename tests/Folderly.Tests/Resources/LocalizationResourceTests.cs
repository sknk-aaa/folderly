using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Folderly.Tests.Resources;

public class LocalizationResourceTests
{
    [Fact]
    public void LocalizedResources_HaveSameKeysAsDefault()
    {
        var resourceDir = GetResourceDirectory();
        var defaultKeys = ReadResourceValues(Path.Combine(resourceDir, "Strings.resx")).Keys;

        foreach (var resourceFile in Directory.EnumerateFiles(resourceDir, "Strings.*.resx"))
        {
            var localizedKeys = ReadResourceValues(resourceFile).Keys;

            Assert.Empty(defaultKeys.Except(localizedKeys));
            Assert.Empty(localizedKeys.Except(defaultKeys));
        }
    }

    [Fact]
    public void LocalizedResources_HaveNonEmptyValues()
    {
        var resourceDir = GetResourceDirectory();

        foreach (var resourceFile in Directory.EnumerateFiles(resourceDir, "Strings.*.resx"))
        {
            var values = ReadResourceValues(resourceFile);
            var emptyKeys = values
                .Where(pair => string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => pair.Key)
                .ToArray();

            Assert.Empty(emptyKeys);
        }
    }

    [Fact]
    public void LocalizedResources_KeepDefaultPlaceholders()
    {
        var resourceDir = GetResourceDirectory();
        var defaultValues = ReadResourceValues(Path.Combine(resourceDir, "Strings.resx"));

        foreach (var resourceFile in Directory.EnumerateFiles(resourceDir, "Strings.*.resx"))
        {
            var values = ReadResourceValues(resourceFile);
            foreach (var (key, defaultValue) in defaultValues)
            {
                Assert.Equal(
                    GetPlaceholders(defaultValue),
                    GetPlaceholders(values[key]));
            }
        }
    }

    private static string GetResourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Folderly.App", "Resources");
            if (File.Exists(Path.Combine(candidate, "Strings.resx")))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Folderly.App resources directory was not found.");
    }

    private static Dictionary<string, string> ReadResourceValues(string path)
    {
        var doc = XDocument.Load(path);
        return doc.Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string[] GetPlaceholders(string value)
        => Regex.Matches(value, @"\{\d+\}")
            .Select(match => match.Value)
            .Distinct()
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
}
