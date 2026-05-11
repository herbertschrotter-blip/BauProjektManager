using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="FileNameParser.Parse"/> — Tokenisierungs-Grundlage
/// für Wizard + Recognizer (BPM-082, ADR-010 / ADR-022).
///
/// Test-Szenarien aus dem Review CGR-2026-04-17-bpm-082-segment-recognition R3
/// (10 reale Baustellenmuster) sowie Edge-Cases für TokenizationConfig.
/// </summary>
public class FileNameParserTests
{
    // === Standard-Delimiter (Default: "-" und "_") ===

    [Fact]
    public void Parse_StandardDelimiter_OWGDoblPolierplan_4Segments()
    {
        // Review-Szenario 1: ÖWG Dobl Polierplan
        var p = FileNameParser.Parse("202401_P_011_Haus64.pdf");

        Assert.Equal("202401_P_011_Haus64", p.BaseName);
        Assert.Equal(".pdf", p.Extension);
        Assert.Equal(4, p.Segments.Count);
        Assert.Equal("202401", p.Segments[0].RawValue);
        Assert.Equal("P", p.Segments[1].RawValue);
        Assert.Equal("011", p.Segments[2].RawValue);
        Assert.Equal("Haus64", p.Segments[3].RawValue);
    }

    [Fact]
    public void Parse_StandardDelimiter_BugSzenario_PROJ_PROT()
    {
        // Bug-Szenario aus BPM-082: PROJ-PROT-2025-01 → Segment 1 = "PROT"
        var p = FileNameParser.Parse("PROJ-PROT-2025-01.pdf");

        Assert.Equal(4, p.Segments.Count);
        Assert.Equal("PROJ", p.Segments[0].RawValue);
        Assert.Equal("PROT", p.Segments[1].RawValue);
        Assert.Equal("2025", p.Segments[2].RawValue);
        Assert.Equal("01", p.Segments[3].RawValue);
    }

    [Fact]
    public void Parse_StandardDelimiter_BugSzenario_RK_PROTOKOLL_EG()
    {
        // Bug-Szenario Gegenprobe: RK-PROTOKOLL-EG → Segment 1 = "PROTOKOLL" (NICHT "PROT")
        var p = FileNameParser.Parse("RK-PROTOKOLL-EG.pdf");

        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("RK", p.Segments[0].RawValue);
        Assert.Equal("PROTOKOLL", p.Segments[1].RawValue);
        Assert.Equal("EG", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_StandardDelimiter_OnlyHyphens()
    {
        var p = FileNameParser.Parse("A-B-C-D.dwg");

        Assert.Equal(4, p.Segments.Count);
        Assert.Equal("A", p.Segments[0].RawValue);
        Assert.Equal("D", p.Segments[3].RawValue);
    }

    [Fact]
    public void Parse_StandardDelimiter_OnlyUnderscores()
    {
        var p = FileNameParser.Parse("A_B_C_D.dwg");

        Assert.Equal(4, p.Segments.Count);
        Assert.Equal("A", p.Segments[0].RawValue);
        Assert.Equal("D", p.Segments[3].RawValue);
    }

    [Fact]
    public void Parse_StandardDelimiter_MixedHyphenAndUnderscore_Review_S6()
    {
        // Review-Szenario 6: S-111-VA-02_2.OG (gemischte Delimiter)
        var p = FileNameParser.Parse("S-111-VA-02_2.pdf");

        Assert.Equal(5, p.Segments.Count);
        Assert.Equal("S", p.Segments[0].RawValue);
        Assert.Equal("111", p.Segments[1].RawValue);
        Assert.Equal("VA", p.Segments[2].RawValue);
        Assert.Equal("02", p.Segments[3].RawValue);
        Assert.Equal("2", p.Segments[4].RawValue);
    }

    // === Extension Handling ===

    [Fact]
    public void Parse_ExtensionStripping_NoExtension()
    {
        var p = FileNameParser.Parse("RK-001");

        Assert.Equal("RK-001", p.BaseName);
        Assert.Equal(string.Empty, p.Extension);
        Assert.Equal(2, p.Segments.Count);
    }

    [Fact]
    public void Parse_ExtensionStripping_UppercaseExtension()
    {
        var p = FileNameParser.Parse("RK-001.DWG");

        Assert.Equal("RK-001", p.BaseName);
        Assert.Equal(".DWG", p.Extension);
    }

    [Fact]
    public void Parse_ExtensionStripping_MultipleDots_OnlyLastIsExtension()
    {
        // "PP_GG_04_Grundriss 2OG_2025-10-14_Index D" mit Punkt im Namen
        var p = FileNameParser.Parse("S-111-VA-02_2.OG.pdf");

        Assert.Equal("S-111-VA-02_2.OG", p.BaseName);
        Assert.Equal(".pdf", p.Extension);
    }

    // === Position 0-basiert ===

    [Fact]
    public void Parse_Position_IsZeroBased()
    {
        var p = FileNameParser.Parse("a-b-c.pdf");

        Assert.Equal(0, p.Segments[0].Position);
        Assert.Equal(1, p.Segments[1].Position);
        Assert.Equal(2, p.Segments[2].Position);
    }

    // === CollapseRepeatedDelimiters ===

    [Fact]
    public void Parse_DoubleUnderscore_DefaultBehavior_LeereSegmenteWerdenUebersprungen()
    {
        // Review-Szenario 8: 24101__301_Bodenplatte
        // Aktuelles Verhalten: leere Parts werden IMMER übersprungen, auch ohne Collapse-Flag
        var p = FileNameParser.Parse("24101__301_Bodenplatte.dwg");

        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("24101", p.Segments[0].RawValue);
        Assert.Equal("301", p.Segments[1].RawValue);
        Assert.Equal("Bodenplatte", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_DoubleUnderscore_WithCollapseFlag()
    {
        // Mit explizitem CollapseRepeatedDelimiters: gleiches Ergebnis
        var config = new TokenizationConfig
        {
            Delimiters = ["_"],
            CollapseRepeatedDelimiters = true
        };
        var p = FileNameParser.Parse("24101__301_Bodenplatte.dwg", config);

        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("24101", p.Segments[0].RawValue);
        Assert.Equal("301", p.Segments[1].RawValue);
    }

    // === Leerzeichen NICHT als Default-Delimiter ===

    [Fact]
    public void Parse_Whitespace_NotASplitDelimiter_ByDefault_Review_S7()
    {
        // Review-Szenario 7: ESS St. Georgen — Leerzeichen darf NICHT Default-Delimiter sein
        var p = FileNameParser.Parse("PP01-1Wohnanlage St. Georgen-17.02.2025.pdf");

        // "PP01", "1Wohnanlage St. Georgen", "17.02.2025"
        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("PP01", p.Segments[0].RawValue);
        Assert.Equal("1Wohnanlage St. Georgen", p.Segments[1].RawValue);
        Assert.Equal("17.02.2025", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_Whitespace_OptInAsDelimiter()
    {
        // User kann Leerzeichen explizit als Delimiter aktivieren
        var config = new TokenizationConfig
        {
            Delimiters = ["-", "_", " "]
        };
        var p = FileNameParser.Parse("PP01 1Wohnanlage Georgen.pdf", config);

        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("PP01", p.Segments[0].RawValue);
        Assert.Equal("1Wohnanlage", p.Segments[1].RawValue);
        Assert.Equal("Georgen", p.Segments[2].RawValue);
    }

    // === FirstTokenDelimiter ===

    [Fact]
    public void Parse_FirstTokenDelimiter_Review_S3()
    {
        // Review-Szenario 3: Statiknummernkreis "5998-201_Wände_EG"
        // FirstTokenDelimiter "-" splittet "5998" als erstes Segment ab,
        // textToParse "201_Wände_EG" wird dann an "_" gesplittet → 4 Segmente gesamt.
        var config = new TokenizationConfig
        {
            Delimiters = ["_"],
            FirstTokenDelimiter = "-"
        };
        var p = FileNameParser.Parse("5998-201_Wände_EG.dwg", config);

        Assert.Equal(4, p.Segments.Count);
        Assert.Equal("5998", p.Segments[0].RawValue);
        Assert.Equal("201", p.Segments[1].RawValue);
        Assert.Equal("Wände", p.Segments[2].RawValue);
        Assert.Equal("EG", p.Segments[3].RawValue);
    }

    // === Edge Cases ===

    [Fact]
    public void Parse_EmptyFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() => FileNameParser.Parse(""));
    }

    [Fact]
    public void Parse_NullFileName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FileNameParser.Parse(null!));
    }

    [Fact]
    public void Parse_WhitespaceOnly_Throws()
    {
        Assert.Throws<ArgumentException>(() => FileNameParser.Parse("   "));
    }

    [Fact]
    public void Parse_OnlyExtension_BaseNameIsEmpty()
    {
        // ".pdf" → BaseName = "", Extension = ".pdf"
        // Path.GetFileNameWithoutExtension(".pdf") liefert ""
        var p = FileNameParser.Parse(".pdf");

        Assert.Equal(string.Empty, p.BaseName);
        Assert.Equal(".pdf", p.Extension);
        Assert.Empty(p.Segments);
    }

    [Fact]
    public void Parse_SingleSegment_NoDelimiter()
    {
        var p = FileNameParser.Parse("README.md");

        Assert.Single(p.Segments);
        Assert.Equal("README", p.Segments[0].RawValue);
        Assert.Equal(".md", p.Extension);
    }

    // === Custom Delimiter via TokenizationConfig ===

    [Fact]
    public void Parse_CustomDelimiter_OnlyDot()
    {
        var config = new TokenizationConfig { Delimiters = ["."] };
        var p = FileNameParser.Parse("a.b.c.pdf", config);

        // Letzter "." ist Extension → BaseName = "a.b.c"
        // Split an "." auf "a.b.c" → ["a", "b", "c"]
        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("a", p.Segments[0].RawValue);
        Assert.Equal("b", p.Segments[1].RawValue);
        Assert.Equal("c", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_NullTokenizationConfig_UsesDefaults()
    {
        var p = FileNameParser.Parse("a-b_c.pdf", (TokenizationConfig?)null);

        Assert.Equal(3, p.Segments.Count);
    }

    // === Legacy Overload (char[]-Variante) ===

    [Fact]
    public void Parse_LegacyOverload_CharArray()
    {
        var p = FileNameParser.Parse("a-b_c.pdf", new[] { '-', '_' });

        Assert.Equal(3, p.Segments.Count);
        Assert.Equal("a", p.Segments[0].RawValue);
        Assert.Equal("b", p.Segments[1].RawValue);
        Assert.Equal("c", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_LegacyOverload_NullDelimiters_UsesDefault()
    {
        var p = FileNameParser.Parse("a-b_c.pdf", (char[]?)null);

        Assert.Equal(3, p.Segments.Count);
    }

    // === UsedDelimiters wird zurückgegeben ===

    [Fact]
    public void Parse_UsedDelimiters_AreReturned()
    {
        var p = FileNameParser.Parse("a-b.pdf");

        Assert.Contains("-", p.UsedDelimiters);
        Assert.Contains("_", p.UsedDelimiters);
    }

    // === OriginalFileName wird durchgereicht ===

    [Fact]
    public void Parse_OriginalFileName_PreservedAsIs()
    {
        var p = FileNameParser.Parse("Original-Name.PDF");

        Assert.Equal("Original-Name.PDF", p.OriginalFileName);
        Assert.Equal(".PDF", p.Extension);
        Assert.Equal("Original-Name", p.BaseName);
    }

    // === Real-Welt-Szenarien aus Review R3 ===

    [Fact]
    public void Parse_RealCase_5998_002a_Bodenplatte_Review_S4()
    {
        // Review-Szenario 4: Index in Segment, Parser darf nicht stolpern
        var p = FileNameParser.Parse("5998-002a_Bodenplatte_Teil_2.pdf");

        // 5998, 002a, Bodenplatte, Teil, 2
        Assert.Equal(5, p.Segments.Count);
        Assert.Equal("5998", p.Segments[0].RawValue);
        Assert.Equal("002a", p.Segments[1].RawValue);
        Assert.Equal("Bodenplatte", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_RealCase_Heiligenkreuz_Review_S9()
    {
        // Review-Szenario 9: Lehrbuchfall
        var p = FileNameParser.Parse("209001_P_PO02_Haus1_Grundriss_EG.pdf");

        Assert.Equal(6, p.Segments.Count);
        Assert.Equal("209001", p.Segments[0].RawValue);
        Assert.Equal("P", p.Segments[1].RawValue);
        Assert.Equal("PO02", p.Segments[2].RawValue);
    }

    [Fact]
    public void Parse_RealCase_SchlossparkSmartCity_Review_S10()
    {
        // Review-Szenario 10a: Schlosspark — viele Unterstriche
        var p = FileNameParser.Parse("21005_101_AP_H1_GR_U1_03.pdf");

        Assert.Equal(7, p.Segments.Count);
        Assert.Equal("21005", p.Segments[0].RawValue);
        Assert.Equal("AP", p.Segments[2].RawValue);
    }
}
