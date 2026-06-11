using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="LightweightPlanExtractor"/> (BPM-111.02, ADR-059).
/// Praxis-Dateinamen aus den OEWG-/5998er-Importen als Testdaten.
/// Extractor liefert NUR Kandidaten (Assist) — kein Test prueft Persistenz.
/// </summary>
public class LightweightPlanExtractorTests
{
    private readonly LightweightPlanExtractor _sut = new();

    [Theory]
    [InlineData("5998-203_OG3_Polierplan.dwg", "5998-203", null, "OG3")]
    [InlineData("5998-200_EG_Polierplan.dwg", "5998-200", null, "EG")]
    [InlineData("B-101_Bewehrung_OG1.pdf", "B-101", null, "OG1")]
    [InlineData("S-190_Schalung_EG.pdf", "S-190", null, "EG")]
    [InlineData("21005_104_AP_H1_GR_E2_05_Grundriss E+2.pdf", "21005", null, null)]
    public void ExtractCandidates_PlanNumberAndLevel(
        string fileName, string? expectedNr, string? expectedIdx, string? expectedLevel)
    {
        var c = _sut.ExtractCandidates(fileName);

        Assert.Equal(expectedNr, c.PlanNumber);
        Assert.Equal(expectedIdx, c.Index);
        Assert.Equal(expectedLevel, c.Level);
    }

    [Fact]
    public void ExtractCandidates_IndexViaDash_KnownPlanNewIndex()
    {
        // Bucket-B-Treiber: bekannter Plan, neuer Index
        var c = _sut.ExtractCandidates("5998-100-B_KG_Polierplan.dwg");

        Assert.Equal("5998-100", c.PlanNumber);
        Assert.Equal("B", c.Index);
        Assert.Equal(RevisionKind.Alphabetic, c.RevisionKind);
        Assert.Equal("KG", c.Level);
    }

    [Fact]
    public void ExtractCandidates_PrefixedNumberWithIndex()
    {
        var c = _sut.ExtractCandidates("S-103-C_TG Wände.pdf");

        Assert.Equal("S-103", c.PlanNumber);
        Assert.Equal("C", c.Index);
        Assert.Equal(RevisionKind.Alphabetic, c.RevisionKind);
    }

    [Theory]
    [InlineData("011vorab_EG_Polierplan.pdf", "011", "vorab", RevisionKind.DraftMarker)]
    [InlineData("002a_Schalung.pdf", "002", "a", RevisionKind.Alphabetic)]
    public void ExtractCandidates_GluedIndex(
        string fileName, string nr, string idx, RevisionKind kind)
    {
        // Edge-Case: Index ohne Trenner an die Plannummer geklebt
        var c = _sut.ExtractCandidates(fileName);

        Assert.Equal(nr, c.PlanNumber);
        Assert.Equal(idx, c.Index);
        Assert.Equal(kind, c.RevisionKind);
    }

    [Fact]
    public void ExtractCandidates_StandaloneIndexTokenAfterNumber()
    {
        var c = _sut.ExtractCandidates("5998-100_B_KG.pdf");

        Assert.Equal("5998-100", c.PlanNumber);
        Assert.Equal("B", c.Index);
    }

    [Fact]
    public void ExtractCandidates_CopyMarker_StrippedAndFlagged()
    {
        // Edge-Case: Windows-Kopiermarker ist KEIN Index
        var c = _sut.ExtractCandidates("Plan_011_EG_(1).pdf");

        Assert.True(c.HasCopyMarker);
        Assert.Equal("011", c.PlanNumber);
        Assert.Null(c.Index);
        Assert.Equal("EG", c.Level);
    }

    [Fact]
    public void ExtractCandidates_HausIsNotLevel()
    {
        // Edge-Case: Haus-vs-Geschoss-Verwechslung
        var c = _sut.ExtractCandidates("5998-300_H2_OG2_Polierplan.pdf");

        Assert.Equal("OG2", c.Level);
        Assert.Equal("H2", c.BuildingPartHint);
    }

    [Fact]
    public void ExtractCandidates_PlanWithoutBuildingPart_HasEmptyHint()
    {
        // Edge-Case: Plaene ohne Haus (Lageplan etc.) — kein Fehler, Hint leer
        var c = _sut.ExtractCandidates("Lageplan_2026.pdf");

        Assert.Null(c.BuildingPartHint);
        Assert.Null(c.Level);
        Assert.Contains("Lageplan", c.TypeKeywords);
    }

    [Fact]
    public void ExtractCandidates_CombiFile_Flagged()
    {
        // Edge-Case: Kombi-Datei — V1 kein Auto-Split, nur Hinweis
        var c = _sut.ExtractCandidates("Schalung+Bewehrung_OG1.pdf");

        Assert.True(c.IsCombi);
        Assert.Contains("Schalung", c.TypeKeywords);
        Assert.Contains("Bewehrung", c.TypeKeywords);
        Assert.Equal("OG1", c.Level);
    }

    [Fact]
    public void ExtractCandidates_Protocol_DateAndKeyword()
    {
        var c = _sut.ExtractCandidates("BB_2026-04-15_Baubesprechung.pdf");

        Assert.Equal("2026-04-15", c.DateCandidate);
        Assert.Contains("Baubesprechung", c.TypeKeywords);
        Assert.False(c.IsCombi);
    }

    [Fact]
    public void ExtractCandidates_Sicherheitsprotokoll_NoDoubleProtokollKeyword()
    {
        // "sicherheitsprotokoll" enthaelt "protokoll" — laengstes Keyword gewinnt
        var c = _sut.ExtractCandidates("SiPro_2026-05-02_Geruest.pdf");

        Assert.Contains("Sicherheitsprotokoll", c.TypeKeywords);
        Assert.DoesNotContain("Protokoll", c.TypeKeywords);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractCandidates_EmptyInput_ReturnsEmptyCandidates(string fileName)
    {
        var c = _sut.ExtractCandidates(fileName);

        Assert.Null(c.PlanNumber);
        Assert.Empty(c.TypeKeywords);
        Assert.Equal(RevisionKind.None, c.RevisionKind);
    }
}
