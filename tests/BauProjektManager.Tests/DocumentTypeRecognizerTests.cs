using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="DocumentTypeRecognizer"/> (BPM-082.02).
/// Schwerpunkt: segment-basierte Erkennung, regex-Fallback, AND-Semantik,
/// Priority-Auflösung, Cache-Verhalten, Bug-Szenario aus Review R1.
///
/// 10 reale Szenarien aus CGR-2026-04-17-bpm-082-segment-recognition R3.
/// </summary>
public class DocumentTypeRecognizerTests
{
    private readonly DocumentTypeRecognizer _sut = new();

    // === Hilfen ===

    private static RecognitionProfile MakeProfile(
        string typeName,
        int priority,
        params (int position, string pattern)[] segmentRules)
    {
        var profile = new RecognitionProfile
        {
            Id = Guid.NewGuid().ToString(),
            DocumentTypeId = typeName.ToLowerInvariant(),
            DocumentTypeName = typeName,
            RecognitionPriority = priority,
            Tokenization = new TokenizationConfig()
        };
        foreach (var (pos, pat) in segmentRules)
        {
            profile.Recognition.Add(new RecognitionRule
            {
                Method = "segment",
                Pattern = pat,
                SegmentPosition = pos
            });
        }
        return profile;
    }

    private static RecognitionProfile MakeRegexProfile(
        string typeName,
        int priority,
        string pattern)
    {
        return new RecognitionProfile
        {
            Id = Guid.NewGuid().ToString(),
            DocumentTypeId = typeName.ToLowerInvariant(),
            DocumentTypeName = typeName,
            RecognitionPriority = priority,
            Tokenization = new TokenizationConfig(),
            Recognition =
            {
                new RecognitionRule { Method = "regex", Pattern = pattern }
            }
        };
    }

    // === Bug-Szenario aus Review R1 ===

    [Fact]
    public void Recognize_BugSzenario_PROJ_PROT_Matches()
    {
        var bauprotokoll = MakeProfile("Bauprotokoll", 100, (1, "PROT"));

        var r = _sut.Recognize("PROJ-PROT-2025-01.pdf", [bauprotokoll]);

        Assert.NotNull(r.MatchedProfile);
        Assert.Equal("Bauprotokoll", r.MatchedProfile!.DocumentTypeName);
    }

    [Fact]
    public void Recognize_BugSzenario_RK_PROTOKOLL_DoesNotMatch()
    {
        var bauprotokoll = MakeProfile("Bauprotokoll", 100, (1, "PROT"));

        var r = _sut.Recognize("RK-PROTOKOLL-EG.pdf", [bauprotokoll]);

        Assert.Null(r.MatchedProfile);
        Assert.True(r.IsUnknown);
        Assert.Empty(r.AllMatches);
    }

    // === Grund-Verhalten segment-Methode ===

    [Fact]
    public void Recognize_SingleSegmentRule_Position0_Matches()
    {
        var profile = MakeProfile("Polierplan", 100, (0, "PP"));

        var r = _sut.Recognize("PP-001-EG.pdf", [profile]);

        Assert.Equal("Polierplan", r.MatchedProfile?.DocumentTypeName);
    }

    [Fact]
    public void Recognize_SingleSegmentRule_PositionOutOfRange_DoesNotMatch()
    {
        // 3 Tokens, Profil verlangt Position 5
        var profile = MakeProfile("X", 100, (5, "Z"));

        var r = _sut.Recognize("A-B-C.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
        Assert.True(r.IsUnknown);
    }

    [Fact]
    public void Recognize_EmptyRecognitionList_DoesNotMatch()
    {
        var profile = new RecognitionProfile
        {
            Id = "p1",
            DocumentTypeName = "Empty",
            RecognitionPriority = 100,
            Tokenization = new TokenizationConfig()
            // Recognition leer
        };

        var r = _sut.Recognize("A-B-C.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
        Assert.True(r.IsUnknown);
    }

    [Fact]
    public void Recognize_NoProfiles_IsUnknown()
    {
        var r = _sut.Recognize("A-B-C.pdf", []);

        Assert.Null(r.MatchedProfile);
        Assert.True(r.IsUnknown);
    }

    // === Multi-Rule AND-Semantik ===

    [Fact]
    public void Recognize_MultiRule_AllMatch_Matches()
    {
        // Profil: Seg 0 = RK UND Seg 2 = EG
        var profile = MakeProfile("Raumkonzept", 100, (0, "RK"), (2, "EG"));

        var r = _sut.Recognize("RK-001-EG.pdf", [profile]);

        Assert.Equal("Raumkonzept", r.MatchedProfile?.DocumentTypeName);
    }

    [Fact]
    public void Recognize_MultiRule_OneFails_DoesNotMatch()
    {
        // Profil: Seg 0 = RK UND Seg 2 = EG; Datei hat Seg 2 = OG
        var profile = MakeProfile("Raumkonzept", 100, (0, "RK"), (2, "EG"));

        var r = _sut.Recognize("RK-001-OG.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
    }

    // === Case-Insensitivity (OrdinalIgnoreCase) ===

    [Theory]
    [InlineData("RK-001-EG.pdf")]
    [InlineData("rk-001-eg.pdf")]
    [InlineData("Rk-001-Eg.pdf")]
    public void Recognize_TokenMatching_IsOrdinalIgnoreCase(string fileName)
    {
        var profile = MakeProfile("Raumkonzept", 100, (0, "RK"));

        var r = _sut.Recognize(fileName, [profile]);

        Assert.NotNull(r.MatchedProfile);
    }

    // === Priority-Auflösung ===

    [Fact]
    public void Recognize_TwoProfilesMatch_DifferentPriority_HighestWins()
    {
        var low = MakeProfile("Generic", 50, (0, "PROJ"));
        var high = MakeProfile("Specific", 200, (0, "PROJ"));

        var r = _sut.Recognize("PROJ-X-Y.pdf", [low, high]);

        Assert.Equal("Specific", r.MatchedProfile?.DocumentTypeName);
        Assert.Equal(2, r.AllMatches.Count);
    }

    [Fact]
    public void Recognize_TwoProfilesMatch_SamePriority_NoMatch_IsConflict()
    {
        var a = MakeProfile("A", 100, (0, "PROJ"));
        var b = MakeProfile("B", 100, (0, "PROJ"));

        var r = _sut.Recognize("PROJ-X-Y.pdf", [a, b]);

        Assert.Null(r.MatchedProfile);          // keiner gewinnt — Konflikt
        Assert.True(r.IsConflict);
        Assert.Equal(2, r.AllMatches.Count);
    }

    // === regex-Fallback ===

    [Fact]
    public void Recognize_Regex_StatikNummernkreis_Matches()
    {
        // Review-Szenario 3: Statiknummernkreis 5998-2xx
        var statik = MakeRegexProfile("Statik", 100, @"^5998-2\d{2}_");

        var r = _sut.Recognize("5998-201_Wände_EG.dwg", [statik]);

        Assert.Equal("Statik", r.MatchedProfile?.DocumentTypeName);
    }

    [Fact]
    public void Recognize_Regex_NoMatch_DoesNotMatch()
    {
        var statik = MakeRegexProfile("Statik", 100, @"^5998-2\d{2}_");

        var r = _sut.Recognize("Polierplan.pdf", [statik]);

        Assert.Null(r.MatchedProfile);
    }

    [Fact]
    public void Recognize_Regex_InvalidPattern_LoggedAndNoMatch_NoCrash()
    {
        // Unbalanced regex — IsValid lässt es durch (Syntax erst zur Match-Zeit),
        // Recognizer fängt ArgumentException ab → no match, kein Crash
        var profile = MakeRegexProfile("Bad", 100, "([unclosed");

        var r = _sut.Recognize("anything.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
    }

    // === Invalide Rule wird abgewiesen (IsValid-Safety-Net) ===

    [Fact]
    public void Recognize_LegacyContainsMethod_RejectedByIsValid_NoMatch()
    {
        // contains ist nach BPM-082 keine valide Methode mehr
        var profile = new RecognitionProfile
        {
            Id = "legacy",
            DocumentTypeName = "Legacy",
            RecognitionPriority = 100,
            Tokenization = new TokenizationConfig(),
            Recognition =
            {
                new RecognitionRule { Method = "contains", Pattern = "PROT" }
            }
        };

        var r = _sut.Recognize("PROJ-PROT-2025-01.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
        Assert.True(r.IsUnknown);
    }

    [Fact]
    public void Recognize_SegmentRule_MissingPosition_RejectedByIsValid_NoMatch()
    {
        // segment ohne SegmentPosition ist invalid
        var profile = new RecognitionProfile
        {
            Id = "broken",
            DocumentTypeName = "Broken",
            RecognitionPriority = 100,
            Tokenization = new TokenizationConfig(),
            Recognition =
            {
                new RecognitionRule
                {
                    Method = "segment",
                    Pattern = "PROT",
                    SegmentPosition = null
                }
            }
        };

        var r = _sut.Recognize("PROJ-PROT-2025-01.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
    }

    // === Cache-Verhalten (indirekt: viele Profile, gleiche Datei) ===

    [Fact]
    public void Recognize_ManyProfiles_NoCrash_OnlyMatchingWins()
    {
        // 10 Profile, nur eines matcht — Cache verhindert mehrfache Tokenisierung
        var profiles = new List<RecognitionProfile>();
        for (int i = 0; i < 9; i++)
        {
            profiles.Add(MakeProfile($"P{i}", 100, (0, $"X{i}")));
        }
        profiles.Add(MakeProfile("Winner", 100, (1, "PROT")));

        var r = _sut.Recognize("PROJ-PROT-2025-01.pdf", profiles);

        Assert.Equal("Winner", r.MatchedProfile?.DocumentTypeName);
    }

    // === RecognizeAll ===

    [Fact]
    public void RecognizeAll_MultipleFiles_ReturnsOneResultPerFile()
    {
        var profile = MakeProfile("Polierplan", 100, (1, "P"));
        var files = new[]
        {
            "202401_P_011.pdf",  // matcht
            "202401_D_011.pdf",  // matcht nicht (Seg 1 = D)
            "202401_P_022.pdf"   // matcht
        };

        var results = _sut.RecognizeAll(files, [profile]);

        Assert.Equal(3, results.Count);
        Assert.NotNull(results[0].MatchedProfile);
        Assert.Null(results[1].MatchedProfile);
        Assert.NotNull(results[2].MatchedProfile);
    }

    // === Reale Szenarien aus Review R3 ===

    [Fact]
    public void Recognize_Review_S1_OWGDobl_Polierplan_vs_Detailplan()
    {
        // Szenario 1: 202401_P_011_... als Polierplan, 202401_D_... NICHT
        var polierplan = MakeProfile("Polierplan", 100, (1, "P"));

        var match = _sut.Recognize("202401_P_011_Haus64.pdf", [polierplan]);
        var noMatch = _sut.Recognize("202401_D_51-59_gesamt.pdf", [polierplan]);

        Assert.NotNull(match.MatchedProfile);
        Assert.Null(noMatch.MatchedProfile);
    }

    [Fact]
    public void Recognize_Review_S2_OWGDobl_LongPrefix_DoesNotMatchPolier()
    {
        // Szenario 2: 202401_DZW_B13_P_... darf NICHT als Polierplan matchen,
        // weil Seg 1 = DZW (nicht P)
        var polierplan = MakeProfile("Polierplan", 100, (1, "P"));

        var r = _sut.Recognize("202401_DZW_B13_P_GR-SCHN.dwg", [polierplan]);

        Assert.Null(r.MatchedProfile);
    }

    [Fact]
    public void Recognize_Review_S5_OWGOfficeLights_Polierplan_GG()
    {
        // Szenario 5: PP_GG_04_..._Index D
        var profile = MakeProfile("Polierplan-GG", 100, (0, "PP"), (1, "GG"));

        var r = _sut.Recognize("PP_GG_04_Grundriss.pdf", [profile]);

        Assert.NotNull(r.MatchedProfile);
    }

    [Fact]
    public void Recognize_Review_S9_Heiligenkreuz_3Rules_AND()
    {
        // Szenario 9: Lehrbuchfall — Seg 0=209001, Seg 1=P, Seg 2=PO02
        var profile = MakeProfile("Heiligenkreuz-Polier",
            100,
            (0, "209001"),
            (1, "P"),
            (2, "PO02"));

        var r = _sut.Recognize("209001_P_PO02_Haus1_Grundriss_EG.pdf", [profile]);

        Assert.NotNull(r.MatchedProfile);
    }

    [Fact]
    public void Recognize_Review_S9_WrongProjectNumber_DoesNotMatch()
    {
        // Gegenprobe zu S9 — falsche Projektnummer (Seg 0)
        var profile = MakeProfile("Heiligenkreuz-Polier",
            100,
            (0, "209001"),
            (1, "P"));

        var r = _sut.Recognize("999999_P_PO02_Haus1.pdf", [profile]);

        Assert.Null(r.MatchedProfile);
    }
}
