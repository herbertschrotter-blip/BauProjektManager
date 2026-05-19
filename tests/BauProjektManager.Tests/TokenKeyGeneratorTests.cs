using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="TokenKeyGenerator"/> (BPM-108 Phase C Teil 2).
/// </summary>
public class TokenKeyGeneratorTests
{
    [Theory]
    [InlineData("Akustik-Klasse", "akustik_klasse")]
    [InlineData("Brandschutz Klasse", "brandschutz_klasse")]
    [InlineData("ÄÖÜß", "aeoeuess")]
    [InlineData("  spaces  ", "spaces")]
    [InlineData("multi___underscore", "multi_underscore")]
    [InlineData("CamelCase", "camelcase")]
    [InlineData("123abc", "123abc")]
    public void Normalize_GeneratesSnakeCase(string input, string expected)
    {
        Assert.Equal(expected, TokenKeyGenerator.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    [InlineData("_!_")]
    public void Normalize_EmptyOrNoUsefulChars_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, TokenKeyGenerator.Normalize(input));
    }

    [Fact]
    public void EnsureUnique_NoConflict_ReturnsBaseKey()
    {
        var result = TokenKeyGenerator.EnsureUnique("plan_number", _ => false);
        Assert.Equal("plan_number", result);
    }

    [Fact]
    public void EnsureUnique_OneConflict_AppendsSuffix2()
    {
        var taken = new HashSet<string> { "akustik_klasse" };
        var result = TokenKeyGenerator.EnsureUnique("akustik_klasse", taken.Contains);
        Assert.Equal("akustik_klasse_2", result);
    }

    [Fact]
    public void EnsureUnique_MultipleConflicts_KeepsIncrementing()
    {
        var taken = new HashSet<string> { "x", "x_2", "x_3", "x_4" };
        var result = TokenKeyGenerator.EnsureUnique("x", taken.Contains);
        Assert.Equal("x_5", result);
    }

    [Fact]
    public void EnsureUnique_EmptyBase_FallsBackToCustom()
    {
        var result = TokenKeyGenerator.EnsureUnique("", _ => false);
        Assert.Equal("custom", result);
    }
}
