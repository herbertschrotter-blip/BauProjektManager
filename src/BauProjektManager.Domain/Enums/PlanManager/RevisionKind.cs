namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// What kind of revision semantics a token carries.
/// Separated from IndexSourceType (WHERE it comes from) — this is WHAT it means.
/// </summary>
public enum RevisionKind
{
    /// <summary>No revision information available.</summary>
    None,
    /// <summary>Numeric revision: 01, 02, 03.</summary>
    Numeric,
    /// <summary>Alphabetic revision: A, B, C, D.</summary>
    Alphabetic,
    /// <summary>Draft marker: VORABZUG, VA, vorab.</summary>
    DraftMarker,
    /// <summary>Composite revision (reserved, not implemented in V1).</summary>
    Composite,
    /// <summary>Revision token found but kind unclear.</summary>
    Unknown
}
