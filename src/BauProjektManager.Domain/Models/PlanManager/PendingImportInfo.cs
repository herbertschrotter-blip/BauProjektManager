namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Detail-Information eines Import-Vorgangs der beim App-Start als "pending"
/// gefunden wurde — d.h. der Import wurde gestartet aber nie auf "completed"
/// oder "failed" gesetzt (App-Crash, Power-Off, OS-Reset während Import).
///
/// Wird von <see cref="Services.PlanManagerDatabase.GetPendingImports"/>
/// geliefert und ist Grundlage für die Recovery-Entscheidungsmatrix
/// (Rollback / Forward / Cleanup) — siehe BPM-016.
/// </summary>
/// <param name="Id">ULID des Import-Journal-Eintrags.</param>
/// <param name="Timestamp">Startzeitpunkt des Imports (UTC, ISO-8601).</param>
/// <param name="SourcePath">Pfad des Inbox-Ordners der gescannt wurde.</param>
/// <param name="FileCount">Anzahl Dateien die bei Start als actionable klassifiziert wurden.</param>
/// <param name="ProfileId">Verwendetes Recognition-Profil (optional).</param>
/// <param name="MachineName">Name des Geräts auf dem der Import gestartet wurde (für Multi-Device-Recovery).</param>
/// <param name="CompletedActions">Anzahl Aktionen die bereits erfolgreich ausgeführt wurden (Datei verschoben).</param>
/// <param name="FailedActions">Anzahl Aktionen die mit Fehler abgebrochen sind.</param>
/// <param name="PendingActions">Anzahl Aktionen die noch nicht ausgeführt wurden (true pending).</param>
public sealed record PendingImportInfo(
    string Id,
    DateTime Timestamp,
    string SourcePath,
    int FileCount,
    string? ProfileId,
    string? MachineName,
    int CompletedActions,
    int FailedActions,
    int PendingActions)
{
    /// <summary>True wenn alle Aktionen entweder completed oder failed sind — d.h. nur der Journal-Header ist pending.</summary>
    public bool IsActionsFinished => PendingActions == 0;

    /// <summary>True wenn keine Aktion erfolgreich war — Rollback ist trivial.</summary>
    public bool IsRollbackTrivial => CompletedActions == 0;

    /// <summary>True wenn alle Aktionen erfolgreich waren — nur Journal-Finalize fehlt.</summary>
    public bool IsForwardTrivial => CompletedActions == FileCount && FailedActions == 0 && PendingActions == 0;
}
