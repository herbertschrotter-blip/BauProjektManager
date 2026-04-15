using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// A fingerprinted file after parsing and profile matching.
/// Contains extracted fields, matched profile, and confidence.
/// </summary>
public sealed record ParsedImportFile(
    string RelativePath,
    string FileName,
    string Extension,
    long FileSize,
    string Md5,
    RecognitionProfile? MatchedProfile,
    IReadOnlyDictionary<string, string> ExtractedFields,
    ParseConfidence Confidence,
    IReadOnlyList<ImportWarning> Warnings);

/// <summary>
/// Typed import warning with code and optional detail message.
/// Logic works with codes, UI builds localized text from them.
/// </summary>
public sealed record ImportWarning(
    ImportWarningCode Code,
    string? Message = null);

/// <summary>
/// Tracks where a classification field value came from.
/// IsHard=true means the source is a definitive classifier (not just a hint).
/// </summary>
public sealed record ResolutionEvidence(
    ResolutionSource Source,
    string Field,
    string Value,
    bool IsHard);
