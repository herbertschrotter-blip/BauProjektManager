namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ergebnis der Vorprüfung vor dem manuellen "Import bestätigen"
/// (BPM-111.05 Slice 3d). Prüft, ob noch ein unabgeschlossener (pending)
/// Import existiert, der zuerst über die Recovery-Strecke (BPM-016) behandelt
/// werden muss. Pure Datenstruktur — keine Disk-/DB-Operationen.
/// </summary>
/// <param name="CanConfirm">True wenn kein pending Import blockiert und bestätigt werden darf.</param>
/// <param name="BlockingImports">Die pending Imports, die das Bestätigen blockieren (leer bei CanConfirm=true).</param>
/// <param name="Message">User-lesbarer Hinweis bei Blockade (null wenn CanConfirm=true).</param>
public sealed record PreImportCheckResult(
    bool CanConfirm,
    IReadOnlyList<PendingImportInfo> BlockingImports,
    string? Message)
{
    /// <summary>Freigabe — kein pending Import vorhanden.</summary>
    public static PreImportCheckResult Clear()
        => new(CanConfirm: true, BlockingImports: [], Message: null);

    /// <summary>Blockade — mind. ein pending Import muss zuerst behandelt werden.</summary>
    public static PreImportCheckResult Blocked(IReadOnlyList<PendingImportInfo> pending)
        => new(
            CanConfirm: false,
            BlockingImports: pending,
            Message: $"{pending.Count} nicht abgeschlossene(r) Import(e) gefunden. " +
                     "Bitte zuerst die Wiederherstellung abschließen, " +
                     "bevor ein neuer Import bestätigt wird.");
}
