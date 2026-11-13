using System.IO;
using System.Text.RegularExpressions;

namespace BauProjektManager.Tests;

/// <summary>
/// R01-009 (Audit Review 01, UT-01..03): jeder Theme-Key (Bpm*), den eine XAML-Datei per
/// StaticResource/DynamicResource referenziert, muss in einer XAML-Datei per x:Key
/// definiert sein. Ein fehlender Key laeuft bei DynamicResource still ins Leere und
/// wirft bei StaticResource eine XamlParseException.
/// </summary>
public partial class ThemeResourceKeyTests
{
    [GeneratedRegex(@"\{(?:Static|Dynamic)Resource\s+(Bpm\w+)\s*\}")]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"x:Key=""(Bpm\w+)""")]
    private static partial Regex DefinitionRegex();

    private static string SrcDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BauProjektManager.slnx")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src");
    }

    private static IEnumerable<string> XamlFiles() =>
        Directory.EnumerateFiles(SrcDirectory(), "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void AllReferencedBpmKeys_AreDefined()
    {
        var files = XamlFiles().ToList();
        Assert.NotEmpty(files);
        var defined = files
            .SelectMany(f => DefinitionRegex().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        var missing = files
            .SelectMany(f => ReferenceRegex().Matches(File.ReadAllText(f))
                .Select(m => (Key: m.Groups[1].Value, File: Path.GetFileName(f))))
            .Where(r => !defined.Contains(r.Key))
            .Select(r => $"{r.Key} ({r.File})")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, "Undefinierte Theme-Keys: " + string.Join(", ", missing));
    }

    [Fact]
    public void FontSizeTokens_MatchGuidelines()
    {
        var typography = File.ReadAllText(Path.Combine(SrcDirectory(),
            "BauProjektManager.App", "Themes", "Typography.xaml"));
        foreach (var (key, size) in new[] { ("H1", 24), ("H2", 18), ("H3", 15), ("Body", 13),
                     ("Label", 12), ("Small", 11), ("Caption", 10) })
            Assert.Contains($"<sys:Double x:Key=\"BpmFontSize{key}\">{size}</sys:Double>", typography);
    }
}
