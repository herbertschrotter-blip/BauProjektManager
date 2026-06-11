namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Buckets des ManualFirstCapture-Workflows (BPM-111.03, ADR-059).
/// Nur Bucket <see cref="NewCapture"/> oeffnet das Radial — matched Updates
/// und Dubletten ueberspringen es.
/// </summary>
public enum CaptureBucket
{
    /// <summary>A — MD5 bereits im Bestand: Skip-Karte (Matrix: SKIP_IDENTICAL).</summary>
    Duplicate = 0,

    /// <summary>B — bekannter Plan, anderer Index: Update-Vorschlag [Uebernehmen][Anderen waehlen][Als neu] (Matrix: UPDATE_NEWER_INDEX / OLDER_REVISION als Warnung).</summary>
    UpdateProposal = 1,

    /// <summary>C — manuelle Erstaufnahme: oeffnet das Radial (Matrix: NEW / UNKNOWN).</summary>
    NewCapture = 2,

    /// <summary>D — Konflikt, braucht Auswahldialog/Panel (mehrere Dokumente mit gleicher Plannummer, gleicher Index mit anderem Inhalt).</summary>
    Conflict = 3
}
