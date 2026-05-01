using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Empfiehlt anhand von <see cref="PendingImportInfo"/> eine <see cref="RecoveryAction"/>
/// für einen pending Import-Vorgang. Pure Domain-Logik, keine Disk- oder DB-Operationen.
///
/// Entscheidungsreihenfolge (Priorität von oben nach unten):
/// 1. <see cref="RecoveryAction.Cleanup"/> wenn FailedActions &gt; 0 (Mix-State, manuelle Untersuchung)
/// 2. <see cref="RecoveryAction.Forward"/> wenn IsRollbackTrivial (nichts wurde verschoben — Re-Run risikolos)
/// 3. <see cref="RecoveryAction.Forward"/> wenn IsForwardTrivial (alle Aktionen completed, nur Journal-Finalize)
/// 4. <see cref="RecoveryAction.Forward"/> wenn PendingActions &gt; 0 (Mix Completed+Pending ohne Failed)
/// 5. <see cref="RecoveryAction.Cleanup"/> Safety-Net (sollte nicht erreicht werden)
///
/// Siehe BPM-016 / 016.02. Ausführung erfolgt in 016.03 (RecoveryExecutor).
/// </summary>
public class RecoveryDecisionService
{
    /// <summary>
    /// Liefert die Recovery-Empfehlung für einen pending Import.
    /// </summary>
    public RecoveryRecommendation Recommend(PendingImportInfo info)
    {
        // 1. Mix-State mit Fehlern → manuelle Untersuchung
        if (info.FailedActions > 0)
        {
            return new RecoveryRecommendation(
                Action: RecoveryAction.Cleanup,
                Reason: $"{info.FailedActions} fehlgeschlagene Aktion(en). " +
                        $"Automatische Reparatur nicht sicher — Journal als 'failed' markieren, " +
                        $"manuelle Untersuchung empfohlen.",
                IsAutomaticAllowed: false);
        }

        // 2. Nichts wurde verschoben → Re-Run risikolos
        if (info.IsRollbackTrivial)
        {
            return new RecoveryRecommendation(
                Action: RecoveryAction.Forward,
                Reason: $"{info.PendingActions} Aktion(en) noch ausstehend, keine bereits verschoben. " +
                        $"Import kann sicher fortgesetzt werden.",
                IsAutomaticAllowed: true);
        }

        // 3. Alle Aktionen erledigt → nur Journal finalisieren
        if (info.IsForwardTrivial)
        {
            return new RecoveryRecommendation(
                Action: RecoveryAction.Forward,
                Reason: $"Alle {info.CompletedActions} Aktion(en) erfolgreich abgeschlossen, " +
                        $"nur Journal-Finalisierung fehlt.",
                IsAutomaticAllowed: true);
        }

        // 4. Mix Completed + Pending, keine Fehler → versuchen weiterzumachen
        if (info.PendingActions > 0)
        {
            return new RecoveryRecommendation(
                Action: RecoveryAction.Forward,
                Reason: $"{info.CompletedActions} Aktion(en) bereits erledigt, " +
                        $"{info.PendingActions} ausstehend. Fortsetzen empfohlen — " +
                        $"User-Bestätigung erforderlich da bereits Dateien verschoben wurden.",
                IsAutomaticAllowed: false);
        }

        // 5. Safety-Net — sollte durch Schritt 2/3 abgedeckt sein
        return new RecoveryRecommendation(
            Action: RecoveryAction.Cleanup,
            Reason: "Inkonsistenter Journal-Status. Manuelle Prüfung empfohlen.",
            IsAutomaticAllowed: false);
    }
}
