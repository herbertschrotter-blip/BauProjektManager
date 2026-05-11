using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="RecognitionRule.IsValid"/> — Modellinvariante
/// für das Recognition-System (BPM-082.01, ADR-010, Konsens R1).
///
/// IsValid ist die zentrale Validierungsquelle: ProfileManager.Load verwirft
/// Profile mit invalider Rule, Recognizer hat IsValid als Safety-Net.
/// </summary>
public class RecognitionRuleValidationTests
{
    // === segment-Methode (Default) ===

    [Fact]
    public void IsValid_Segment_Position0_Pattern_ReturnsTrue()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "PROT",
            SegmentPosition = 0
        };

        Assert.True(rule.IsValid(out var reason));
        Assert.Equal("", reason);
    }

    [Fact]
    public void IsValid_Segment_PositionN_Pattern_ReturnsTrue()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "EG",
            SegmentPosition = 5
        };

        Assert.True(rule.IsValid(out _));
    }

    [Fact]
    public void IsValid_Segment_PositionNull_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "PROT",
            SegmentPosition = null
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("SegmentPosition", reason);
    }

    [Fact]
    public void IsValid_Segment_NegativePosition_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "PROT",
            SegmentPosition = -1
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("SegmentPosition", reason);
    }

    [Fact]
    public void IsValid_Segment_EmptyPattern_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "",
            SegmentPosition = 0
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Pattern", reason);
    }

    [Fact]
    public void IsValid_Segment_WhitespacePattern_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "segment",
            Pattern = "   ",
            SegmentPosition = 0
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Pattern", reason);
    }

    // === regex-Methode (Fallback) ===

    [Fact]
    public void IsValid_Regex_WithPattern_ReturnsTrue()
    {
        var rule = new RecognitionRule
        {
            Method = "regex",
            Pattern = @"^5998-2\d{2}_"
        };

        Assert.True(rule.IsValid(out _));
    }

    [Fact]
    public void IsValid_Regex_EmptyPattern_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "regex",
            Pattern = ""
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Pattern", reason);
    }

    [Fact]
    public void IsValid_Regex_SegmentPositionIrrelevant_StillValid()
    {
        // SegmentPosition wird bei method=regex ignoriert
        var rule = new RecognitionRule
        {
            Method = "regex",
            Pattern = @"^X",
            SegmentPosition = 42 // bewusst absurder Wert
        };

        Assert.True(rule.IsValid(out _));
    }

    [Fact]
    public void IsValid_Regex_PatternSyntaxNotCheckedAtValidation()
    {
        // Syntax-Check erst zur Match-Zeit (mit ReDoS-Timeout im Recognizer)
        var rule = new RecognitionRule
        {
            Method = "regex",
            Pattern = "([unclosed"
        };

        Assert.True(rule.IsValid(out _));
    }

    // === Legacy "prefix" / "contains" sind invalid (BPM-082, Konsens R1) ===

    [Fact]
    public void IsValid_LegacyPrefix_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "prefix",
            Pattern = "PROT"
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Unbekannte Methode", reason);
        Assert.Contains("prefix", reason);
    }

    [Fact]
    public void IsValid_LegacyContains_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "contains",
            Pattern = "PROT"
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Unbekannte Methode", reason);
        Assert.Contains("contains", reason);
    }

    // === Unbekannte Methoden ===

    [Theory]
    [InlineData("startswith")]
    [InlineData("endswith")]
    [InlineData("equals")]
    [InlineData("wildcard")]
    [InlineData("foo")]
    public void IsValid_UnknownMethods_ReturnFalse(string method)
    {
        var rule = new RecognitionRule
        {
            Method = method,
            Pattern = "X"
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Unbekannte Methode", reason);
    }

    [Fact]
    public void IsValid_EmptyMethod_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "",
            Pattern = "X"
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Unbekannte Methode", reason);
    }

    [Fact]
    public void IsValid_WhitespaceMethod_ReturnsFalse()
    {
        var rule = new RecognitionRule
        {
            Method = "   ",
            Pattern = "X"
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Unbekannte Methode", reason);
    }

    // === Case-Insensitivity ===

    [Theory]
    [InlineData("SEGMENT")]
    [InlineData("Segment")]
    [InlineData("SeGmEnT")]
    [InlineData("segment")]
    public void IsValid_SegmentMethod_CaseInsensitive(string method)
    {
        var rule = new RecognitionRule
        {
            Method = method,
            Pattern = "PROT",
            SegmentPosition = 0
        };

        Assert.True(rule.IsValid(out _));
    }

    [Theory]
    [InlineData("REGEX")]
    [InlineData("Regex")]
    [InlineData("regex")]
    public void IsValid_RegexMethod_CaseInsensitive(string method)
    {
        var rule = new RecognitionRule
        {
            Method = method,
            Pattern = @"^X"
        };

        Assert.True(rule.IsValid(out _));
    }

    // === Pattern-Validierung greift VOR Methode ===

    [Fact]
    public void IsValid_EmptyPattern_TakesPriorityOverUnknownMethod()
    {
        // Pattern fehlt → erste Fehlerquelle, "Pattern fehlt" wird gemeldet
        var rule = new RecognitionRule
        {
            Method = "foo",
            Pattern = ""
        };

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Pattern", reason);
        // "Unbekannte Methode" wird gar nicht erst geprüft
        Assert.DoesNotContain("Unbekannte", reason);
    }

    // === Default-Werte einer frischen Rule ===

    [Fact]
    public void IsValid_DefaultRule_FailsBecausePatternEmpty()
    {
        // Default: Method = "segment", Pattern = "", SegmentPosition = null
        var rule = new RecognitionRule();

        Assert.False(rule.IsValid(out var reason));
        Assert.Contains("Pattern", reason);
    }
}
