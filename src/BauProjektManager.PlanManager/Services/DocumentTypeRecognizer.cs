using System.Text.RegularExpressions;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Matches file names against RecognitionProfiles to determine document type.
/// BPM-082: segment (Default, positionsgenau via FileNameParser) + regex (Fallback).
/// Tokenisiert pro (Datei, Profil) ueber lokalen Cache; AND-Semantik fuer Multi-Rule-Profile.
/// Returns matched profiles sorted by priority (highest first).
/// </summary>
public class DocumentTypeRecognizer
{
    /// <summary>
    /// Result of a recognition attempt for a single file.
    /// </summary>
    public class RecognitionResult
    {
        public string FileName { get; init; } = string.Empty;
        public RecognitionProfile? MatchedProfile { get; init; }
        public List<RecognitionProfile> AllMatches { get; init; } = [];
        public bool IsConflict => AllMatches.Count > 1;
        public bool IsUnknown => AllMatches.Count == 0;
    }

    /// <summary>
    /// Tokenisierungs-Snapshot einer Datei fuer ein Profil (BPM-082).
    /// Wird in Recognize(...) lokal gecached, einmal pro Datei+Profil.
    /// Wizard und Recognizer teilen sich die Tokenlogik via FileNameParser.
    /// </summary>
    private sealed record RecognitionContext(
        string FileName,
        string FileStem,
        IReadOnlyList<string> Tokens);

    /// <summary>
    /// Recognizes the document type for a single file name.
    /// Checks all profiles, returns best match by priority.
    /// Multiple matches with same priority → CONFLICT.
    /// BPM-082: Lokaler RecognitionContext-Cache pro Recognize-Aufruf
    /// — vermeidet mehrfache Tokenisierung derselben Datei ueber Profile hinweg.
    /// Kein langlebiger Feldcache (kein Threading-/Memory-Thema).
    /// </summary>
    public RecognitionResult Recognize(string fileName, List<RecognitionProfile> profiles)
    {
        var contextCache = new Dictionary<(string FileName, string ProfileId), RecognitionContext>();
        var matches = new List<RecognitionProfile>();

        foreach (var profile in profiles)
        {
            if (MatchesProfile(fileName, profile, contextCache))
                matches.Add(profile);
        }

        // Sort by priority descending (highest priority wins)
        matches.Sort((a, b) => b.RecognitionPriority.CompareTo(a.RecognitionPriority));

        RecognitionProfile? bestMatch = null;
        if (matches.Count == 1)
        {
            bestMatch = matches[0];
        }
        else if (matches.Count > 1)
        {
            // If top two have different priorities → highest wins, no conflict
            if (matches[0].RecognitionPriority > matches[1].RecognitionPriority)
                bestMatch = matches[0];
            // Same priority → conflict, user must decide
        }

        if (bestMatch is not null)
            Log.Debug("Erkannt: {File} → {Type} (Prio {Prio})",
                fileName, bestMatch.DocumentTypeName, bestMatch.RecognitionPriority);
        else if (matches.Count > 1)
            Log.Debug("Konflikt: {File} → {Count} Profile matchen",
                fileName, matches.Count);
        else
            Log.Debug("Unbekannt: {File} → kein Profil matcht", fileName);

        return new RecognitionResult
        {
            FileName = fileName,
            MatchedProfile = bestMatch,
            AllMatches = matches
        };
    }

    /// <summary>
    /// Recognizes document types for multiple files at once.
    /// </summary>
    public List<RecognitionResult> RecognizeAll(
        IEnumerable<string> fileNames, List<RecognitionProfile> profiles)
    {
        return fileNames.Select(f => Recognize(f, profiles)).ToList();
    }

    /// <summary>
    /// Erstellt den Tokenisierungs-Snapshot fuer eine Datei + Profil (BPM-082).
    /// Nutzt denselben FileNameParser wie der Wizard, damit Lern- und Laufzeit-
    /// pfad nicht driften (zentrale Tokenisierungsquelle).
    /// </summary>
    private static RecognitionContext BuildRecognitionContext(
        string fileName,
        RecognitionProfile profile)
    {
        var parsed = FileNameParser.Parse(fileName, profile.Tokenization);

        return new RecognitionContext(
            FileName: fileName,
            FileStem: parsed.BaseName,
            Tokens: parsed.Segments
                .OrderBy(s => s.Position)
                .Select(s => s.RawValue)
                .ToList());
    }

    /// <summary>
    /// Checks if a file name matches a profile's recognition rules (BPM-082).
    /// All rules must match (AND logic). RecognitionContext wird via Cache
    /// wiederverwendet — Tokenisierung einmal pro (fileName, profile.Id).
    /// </summary>
    private static bool MatchesProfile(
        string fileName,
        RecognitionProfile profile,
        Dictionary<(string FileName, string ProfileId), RecognitionContext> contextCache)
    {
        if (profile.Recognition.Count == 0)
            return false;

        var key = (fileName, profile.Id);
        if (!contextCache.TryGetValue(key, out var ctx))
        {
            ctx = BuildRecognitionContext(fileName, profile);
            contextCache[key] = ctx;
        }

        foreach (var rule in profile.Recognition)
        {
            if (!MatchesRule(ctx, rule))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks a single recognition rule against the file context (BPM-082).
    /// IsValid() ist Safety-Net — ProfileManager.Load sollte invalide Profile
    /// bereits verworfen haben (ADR-010, Konsens R3 Punkt 10). Defensiver
    /// Debug-Log dokumentiert, falls trotzdem etwas durchrutscht.
    /// Methoden: "segment" (Default, positionsgenau), "regex" (Fallback).
    /// </summary>
    private static bool MatchesRule(RecognitionContext ctx, RecognitionRule rule)
    {
        if (!rule.IsValid(out var reason))
        {
            Log.Debug("Ungueltige Rule im Recognizer verworfen: {Reason}", reason);
            return false;
        }

        return rule.Method.ToLowerInvariant() switch
        {
            "segment" => MatchesSegment(ctx, rule),
            "regex" => MatchesRegex(ctx.FileName, rule.Pattern),
            _ => false
        };
    }

    /// <summary>
    /// Position-genauer Token-Vergleich (BPM-082, segment-Methode).
    /// Schlank gehalten: kein Trim, kein Strip, keine Sonderlogik.
    /// IsValid() wird in MatchesRule vorgelagert — daher SegmentPosition!.Value sicher.
    /// </summary>
    private static bool MatchesSegment(RecognitionContext ctx, RecognitionRule rule)
    {
        var pos = rule.SegmentPosition!.Value;
        return pos >= 0
            && pos < ctx.Tokens.Count
            && string.Equals(
                ctx.Tokens[pos],
                rule.Pattern,
                StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks a file name against a regex pattern.
    /// Invalid patterns and timeouts are logged and treated as no-match (no crash).
    /// Timeout protects against ReDoS attacks from user-supplied patterns.
    /// </summary>
    private static bool MatchesRegex(string fileName, string pattern)
    {
        try
        {
            return Regex.IsMatch(
                fileName,
                pattern,
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException ex)
        {
            Log.Warning("Ungueltiges Regex-Pattern {Pattern}: {Message}",
                pattern, ex.Message);
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            Log.Warning("Regex-Timeout bei Pattern {Pattern} auf Datei {File}",
                pattern, fileName);
            return false;
        }
    }
}
