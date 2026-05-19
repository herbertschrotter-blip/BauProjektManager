namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// DevTool-/Setup-Befehl zum Archivieren veralteter PlanManager-Profile und Pattern-Templates
/// (BPM-108 Phase B). Verschiebt Dateien mit nicht-aktueller Schema-Version nach
/// <c>_archiv/schema-reset-YYYYMMDD-HHMMSS/</c>. Kein automatischer Side-Effect des
/// normalen App-Starts — wird explizit von DevTools oder Setup ausgeloest.
/// </summary>
public interface IProfileArchiveService
{
    /// <summary>
    /// Archiviert Profile mit <c>schemaVersion != aktuell</c> im Projekt.
    /// </summary>
    /// <param name="projectRootPath">Projekt-Wurzel (enthaelt <c>.bpm/profiles/</c>).</param>
    /// <returns>Anzahl verschobener Profile.</returns>
    int ArchiveOutdatedProfiles(string projectRootPath);

    /// <summary>
    /// Archiviert <c>pattern-templates.json</c> falls die Datei Templates mit
    /// nicht-aktueller Schema-Version enthaelt. Die ganze Datei wird verschoben — nicht
    /// einzelne Templates herausgefiltert (Frühphasen-Reset, kein Merge).
    /// </summary>
    /// <param name="appDataCloudSharedPath">Cloud-AppData-Pfad mit <c>pattern-templates.json</c>.</param>
    /// <returns>true wenn archiviert, false wenn nichts zu tun.</returns>
    bool ArchiveOutdatedPatternTemplates(string appDataCloudSharedPath);
}
