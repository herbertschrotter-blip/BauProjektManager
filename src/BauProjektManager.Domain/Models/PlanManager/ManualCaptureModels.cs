using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Bekanntes Dokument mit aktueller Revision — Matching-Grundlage fuer
/// ManualFirstCapture (BPM-111.03). Read-only Sicht auf plan_documents +
/// current plan_revision (Schema v2.0).
/// </summary>
/// <param name="DocumentId">plan_documents.id (ULID).</param>
/// <param name="DocumentKey">Kuratierter, index-freier document_key.</param>
/// <param name="PlanNumber">Plannummer des Dokuments.</param>
/// <param name="DocumentType">Anzeigename des Dokumenttyps.</param>
/// <param name="TargetFolder">Kanonischer Zielordner (building_parts.name-basiert).</param>
/// <param name="RelativeDirectory">Relativer Zielpfad der aktuellen Ablage.</param>
/// <param name="CurrentIndex">Index der aktuellen Revision (NULL bei Erstausgabe).</param>
/// <param name="CurrentRevisionId">plan_revisions.id der aktuellen Revision (fuer Supersede in 111.04).</param>
public sealed record KnownPlanDocument(
    string DocumentId,
    string DocumentKey,
    string PlanNumber,
    string DocumentType,
    string TargetFolder,
    string RelativeDirectory,
    string? CurrentIndex,
    string CurrentRevisionId);

/// <summary>
/// Eine klassifizierte Eingangs-Datei des ManualFirstCapture-Workflows.
/// NUR Klassifikation + Kandidaten — es wird nichts persistiert und nichts
/// verschoben (Pending/Journal/Import = BPM-111.04, ADR-059 "B entscheidet").
/// </summary>
/// <param name="File">Datei mit MD5 (Fingerprint-Invariante: MD5 + Size immer Pflicht).</param>
/// <param name="Candidates">Lightweight-Kandidaten aus dem Dateinamen (BPM-111.02).</param>
/// <param name="Bucket">Bucket A/B/C/D.</param>
/// <param name="Match">Getroffenes bekanntes Dokument (Bucket B; bei A wenn aufloesbar).</param>
/// <param name="Reason">Menschlich lesbare Begruendung/Warnung (z. B. OLDER_REVISION-Hinweis, Konfliktgrund).</param>
public sealed record CaptureItem(
    FingerprintedFile File,
    PlanFileCandidates Candidates,
    CaptureBucket Bucket,
    KnownPlanDocument? Match,
    string? Reason);

/// <summary>Ergebnis der ManualFirstCapture-Analyse (BPM-111.03).</summary>
public sealed record ManualCaptureResult(IReadOnlyList<CaptureItem> Items)
{
    public static ManualCaptureResult Empty => new([]);

    public int TotalFiles => Items.Count;
    public int DuplicateCount => Items.Count(i => i.Bucket == CaptureBucket.Duplicate);
    public int UpdateProposalCount => Items.Count(i => i.Bucket == CaptureBucket.UpdateProposal);
    public int NewCaptureCount => Items.Count(i => i.Bucket == CaptureBucket.NewCapture);
    public int ConflictCount => Items.Count(i => i.Bucket == CaptureBucket.Conflict);
}
