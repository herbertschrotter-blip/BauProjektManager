namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Recovery-Aktion für einen pending Import-Vorgang (siehe BPM-016 / 016.02).
/// </summary>
public enum RecoveryAction
{
    /// <summary>Default — keine Empfehlung möglich (sollte in der Praxis nicht auftreten).</summary>
    None,

    /// <summary>
    /// Pending Aktionen ausführen und Journal finalisieren.
    /// Bei IsRollbackTrivial (nichts wurde verschoben) oder IsForwardTrivial (alles
    /// wurde verschoben, nur Journal-Header offen) ist das risikolos.
    /// </summary>
    Forward,

    /// <summary>
    /// Bereits verschobene Dateien zurück in den Inbox-Ordner verschieben,
    /// Archive-Files wiederherstellen, Journal als 'failed' markieren.
    /// User-Wahl wenn Forward nicht gewünscht ist (z.B. Quelldateien wurden inzwischen
    /// extern geändert).
    /// </summary>
    Rollback,

    /// <summary>
    /// Journal als 'failed' markieren, Inbox/Disk unangetastet lassen.
    /// Empfehlung wenn Mix-State mit FailedActions > 0 — manuelle Untersuchung nötig,
    /// keine automatische Reparatur.
    /// </summary>
    Cleanup
}
