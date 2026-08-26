using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Eine vom User im Radial/Panel getroffene, noch NICHT bestaetigte Zuordnung
/// (BPM-111.04, ADR-059 Pending Assignments). Lebt in-memory pro Session —
/// kein Move, keine DB bis zur Bestaetigung. Werte sind die vom User
/// BESTAETIGTEN Daten (ImportIdentitySource: ManualConfirmed), nicht die
/// Extractor-Kandidaten.
/// </summary>
/// <param name="File">Datei mit MD5 aus der Capture-Analyse.</param>
/// <param name="SourceBucket">Herkunfts-Bucket (B Update / C Erstaufnahme; D nach Aufloesung).</param>
/// <param name="DocumentTypeId">Dokumenttyp-Id (z. B. "polierplan").</param>
/// <param name="DocumentTypeName">Anzeigename (z. B. "Polierplan").</param>
/// <param name="BuildingPart">Gewaehlter Bauteil-/Kategorie-Name (Ring 2), NULL wenn keiner.</param>
/// <param name="Level">Gewaehltes Geschoss (Ring 3), NULL wenn keines.</param>
/// <param name="PlanNumber">Bestaetigte Plannummer, NULL bei nummernlosen Typen (Protokolle).</param>
/// <param name="Index">Bestaetigter Index, NULL bei Erstausgabe.</param>
/// <param name="TargetRelativeDirectory">Zielordner relativ zum Projekt (z. B. "Pläne/Polierplan/Haus 1/OG3").</param>
/// <param name="Match">Bekanntes Dokument bei Update-Uebernahme (Bucket B), sonst NULL.</param>
/// <param name="Title">Vom User erfasste Bezeichnung (BPM-111.06 Slice A3) — fliesst bei Erstaufnahmen in plan_documents.title. NULL = keine.</param>
public sealed record PendingAssignment(
    FingerprintedFile File,
    CaptureBucket SourceBucket,
    string DocumentTypeId,
    string DocumentTypeName,
    string? BuildingPart,
    string? Level,
    string? PlanNumber,
    string? Index,
    string TargetRelativeDirectory,
    KnownPlanDocument? Match,
    string? Title = null);

/// <summary>Konflikt einer Undo-Preflight-Pruefung (eine Journal-Aktion).</summary>
public sealed record UndoActionConflict(string ActionId, string FileName, string Issue);

/// <summary>
/// Ergebnis des Undo-Trockenlaufs (PlanManager.md Kap. 11.2): erst pruefen,
/// ob alle Dateien noch dort liegen wo erwartet — dann ausfuehren.
/// </summary>
public sealed record UndoPreflightReport(
    string? ImportId,
    int ActionCount,
    IReadOnlyList<UndoActionConflict> Conflicts)
{
    public bool CanUndo => ImportId is not null && Conflicts.Count == 0 && ActionCount > 0;
}

/// <summary>Ergebnis eines ausgefuehrten Undos (letzter Import).</summary>
public sealed record UndoResult(
    bool Success,
    string? ImportId,
    int RestoredFiles,
    IReadOnlyList<string> Errors,
    UndoPreflightReport Preflight);
