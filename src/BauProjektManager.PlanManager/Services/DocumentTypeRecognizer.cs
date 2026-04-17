using System.Text.RegularExpressions;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Matches file names against RecognitionProfiles to determine document type.
/// Uses prefix/contains rules from profile recognition config.
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
    /// Recognizes the document type for a single file name.
    /// Checks all profiles, returns best match by priority.
    /// Multiple matches with same priority → CONFLICT.
    /// </summary>
    public RecognitionResult Recognize(string fileName, List<RecognitionProfile> profiles)
    {
        var matches = new List<RecognitionProfile>();

        foreach (var profile in profiles)
        {
            if (MatchesProfile(fileName, profile))
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
    /// Checks if a file name matches a profile's recognition rules.
    /// All rules must match (AND logic).
    /// </summary>
    private static bool MatchesProfile(string fileName, RecognitionProfile profile)
    {
        if (profile.Recognition.Count == 0)
            return false;

        foreach (var rule in profile.Recognition)
        {
            if (!MatchesRule(fileName, rule))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks a single recognition rule against a file name.
    /// Supports "prefix", "contains" and "regex" methods.
    /// </summary>
    private static bool MatchesRule(string fileName, RecognitionRule rule)
    {
        if (string.IsNullOrEmpty(rule.Pattern))
            return false;

        return rule.Method.ToLowerInvariant() switch
        {
            "prefix" => fileName.StartsWith(rule.Pattern,
                StringComparison.OrdinalIgnoreCase),
            "contains" => fileName.Contains(rule.Pattern,
                StringComparison.OrdinalIgnoreCase),
            "regex" => MatchesRegex(fileName, rule.Pattern),
            _ => false
        };
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
