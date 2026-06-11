using BauProjektManager.Infrastructure.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="PlanValueNormalizer"/> (BPM-111.02).
/// NormalizeForKey muss exakt der bisherigen DocumentKeyBuilder-Logik
/// entsprechen (document_key-Stabilitaet ueber den Refactor hinweg).
/// </summary>
public class PlanValueNormalizerTests
{
    private readonly PlanValueNormalizer _sut = new();

    [Theory]
    [InlineData("TG Wände", "tg_waende")]
    [InlineData("Polierplan", "polierplan")]
    [InlineData("Haus 2", "haus_2")]
    [InlineData("  Grundriss E+2  ", "grundriss_e+2")]
    [InlineData("A--B__C", "a_b_c")]
    [InlineData("Größe, Maß", "groesse_mass")]
    public void NormalizeForKey_MatchesLegacyDocumentKeyBuilderBehavior(string input, string expected)
    {
        Assert.Equal(expected, _sut.NormalizeForKey(input));
    }

    [Theory]
    [InlineData("Haus 2", "haus2")]
    [InlineData("haus-2", "haus2")]
    [InlineData("HAUS_2", "haus2")]
    [InlineData("TG Wände", "tgwaende")]
    public void NormalizeForMatch_IsSeparatorAndCaseTolerant(string input, string expected)
    {
        Assert.Equal(expected, _sut.NormalizeForMatch(input));
    }

    [Fact]
    public void NormalizeForMatch_DoesNotAliasMap()
    {
        // "H2" -> "Haus 2" waere Alias-Mapping (BPM-109.06, post-V1)
        Assert.NotEqual(_sut.NormalizeForMatch("H2"), _sut.NormalizeForMatch("Haus 2"));
    }

    [Theory]
    [InlineData("Haus 2: West?", "Haus 2 West")]
    [InlineData("  00 Polierpläne  ", "00 Polierpläne")]
    [InlineData("Bauteil/Nord", "BauteilNord")]
    [InlineData("Name.", "Name")]
    public void NormalizeForFolderName_ProducesValidWindowsName(string input, string expected)
    {
        Assert.Equal(expected, _sut.NormalizeForFolderName(input));
    }

    [Fact]
    public void NormalizeForFolderName_PreservesPrefixAndUmlauts()
    {
        // Praefix-Garantie: "00 " und Umlaute bleiben im Ordnernamen erhalten
        Assert.Equal("00 Polierpläne", _sut.NormalizeForFolderName("00 Polierpläne"));
    }

    [Fact]
    public void NormalizeForFolderName_CapsLength()
    {
        var longName = new string('a', 150);
        Assert.Equal(100, _sut.NormalizeForFolderName(longName).Length);
    }
}
