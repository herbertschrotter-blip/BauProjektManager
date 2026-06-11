using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Klassifiziert einen Index-/Revisions-Token (BPM-111.02).
/// Zentralisiert die bisher private Logik aus dem ImportWorkflowService —
/// eine Stelle fuer Pipeline UND Lightweight-Extractor.
/// </summary>
public static class RevisionKindDetector
{
    /// <summary>Draft-Marker laut Stage-Konzept (PlanManager.md Kap. 13.6).</summary>
    private static readonly string[] _draftMarkers = ["vorabzug", "vorab", "va"];

    public static RevisionKind Detect(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RevisionKind.None;

        var lower = token.ToLowerInvariant();
        if (_draftMarkers.Contains(lower))
            return RevisionKind.DraftMarker;
        if (token.All(char.IsDigit))
            return RevisionKind.Numeric;
        if (token.All(char.IsLetter))
            return RevisionKind.Alphabetic;
        return RevisionKind.Unknown;
    }

    /// <summary>True wenn der Token ein Draft-Marker ist (vorab/vorabzug/va).</summary>
    public static bool IsDraftMarker(string token) =>
        _draftMarkers.Contains(token.ToLowerInvariant());
}
