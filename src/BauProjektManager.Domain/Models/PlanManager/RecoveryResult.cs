using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ergebnis einer Recovery-Operation aus dem
/// <see cref="Services.RecoveryExecutorService"/>. Siehe BPM-016 / 016.03.
/// </summary>
/// <param name="Action">Welche Recovery-Aktion ausgeführt wurde.</param>
/// <param name="ImportId">Referenz auf den behandelten import_journal-Eintrag.</param>
/// <param name="ProcessedCount">Anzahl Aktionen die erfolgreich abgearbeitet wurden.</param>
/// <param name="FailedCount">Anzahl Aktionen die mit Fehler abgebrochen sind.</param>
/// <param name="Errors">Liste der Fehler-Texte für die Diagnose (leer wenn FailedCount=0).</param>
public sealed record RecoveryResult(
    RecoveryAction Action,
    string ImportId,
    int ProcessedCount,
    int FailedCount,
    IReadOnlyList<string> Errors)
{
    /// <summary>True wenn Recovery ohne Fehler durchlief.</summary>
    public bool IsSuccess => FailedCount == 0;
}
