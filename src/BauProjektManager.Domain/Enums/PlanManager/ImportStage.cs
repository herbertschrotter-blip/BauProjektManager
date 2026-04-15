namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Stage of a document in the review/release lifecycle.
/// Unknown = no stage info available (default, NOT "Final").
/// Draft = VORABZUG, vorab, VA markers detected.
/// Final = explicitly issued, no draft markers, in main folder.
/// </summary>
public enum ImportStage
{
    Unknown,
    Draft,
    Final
}
