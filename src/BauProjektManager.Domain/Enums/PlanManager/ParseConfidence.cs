namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Confidence level of the file name parsing result.
/// High = clear profile match, all fields extracted.
/// Medium = profile matches but some fields from context/fallback.
/// Low = incomplete, ambiguous, manual review needed.
/// </summary>
public enum ParseConfidence
{
    Low,
    Medium,
    High
}
