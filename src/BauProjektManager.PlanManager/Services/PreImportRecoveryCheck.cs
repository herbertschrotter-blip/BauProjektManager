using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Vorprüfung vor dem manuellen "Import bestätigen" (BPM-111.05 Slice 3d):
/// Existiert noch ein unabgeschlossener (pending) Import — z.B. durch App-Crash
/// während des letzten Confirms oder einen via Cloud gesyncten planmanager.db eines
/// anderen Geräts —, darf KEIN neuer Import-Journal geschrieben werden, bis der
/// Altvorgang über die Recovery-Strecke (BPM-016) behandelt wurde. Andernfalls
/// kollidieren die pending Aktionen mit dem neuen Import.
///
/// Pure Logik (keine DB-/Disk-Operationen) — analog zu <see cref="RecoveryDecisionService"/>.
/// Die pending Imports liefert der Aufrufer via PlanManagerDatabase.GetPendingImports().
/// </summary>
public class PreImportRecoveryCheck
{
    /// <summary>
    /// Entscheidet, ob ein manueller Import bestätigt werden darf. Blockiert, sobald
    /// mindestens ein pending Import vorliegt.
    /// </summary>
    public PreImportCheckResult Evaluate(IReadOnlyList<PendingImportInfo> pendingImports)
        => pendingImports.Count == 0
            ? PreImportCheckResult.Clear()
            : PreImportCheckResult.Blocked(pendingImports);
}
