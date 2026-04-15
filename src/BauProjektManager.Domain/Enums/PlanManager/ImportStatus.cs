namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Result status of the import version decision (9 types).
/// Maps to the decision matrix in PlanManager.md Kap. 5.3.
/// </summary>
public enum ImportStatus
{
    /// <summary>document_key not in DB — first version.</summary>
    New,
    /// <summary>Same name + same MD5 — identical, skip.</summary>
    SkipIdentical,
    /// <summary>Newer index detected (e.g. C→D) — archive old.</summary>
    UpdateNewerIndex,
    /// <summary>IndexSource=None + different MD5 — changed.</summary>
    ChangedNoIndex,
    /// <summary>⚠ Same index but different MD5 — warning!</summary>
    ChangedSameIndex,
    /// <summary>⚠ Incoming has lower index than existing.</summary>
    OlderRevision,
    /// <summary>Previously no index, now one detected — learn.</summary>
    LearnIndex,
    /// <summary>No profile matched — manual assignment needed.</summary>
    Unknown,
    /// <summary>Multiple profiles matched — user must choose.</summary>
    Conflict
}
