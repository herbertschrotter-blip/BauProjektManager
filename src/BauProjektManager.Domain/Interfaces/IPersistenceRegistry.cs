using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Zentrales Persistenz-Inventar: Services registrieren ihre Files beim Init,
/// DevTools liest Liste fuer Inventar-Anzeige + Reset-Funktionalitaet.
///
/// Hybrid-Architektur:
/// - In-memory Singleton-Store (Services registrieren beim Init)
/// - Filesystem-Scan ergaenzt um nicht-registrierte / verwaiste Files
///   (siehe RescanFilesystem)
///
/// BPM-104.01.
/// </summary>
public interface IPersistenceRegistry
{
    /// <summary>
    /// Registriert eine Persistenz-Datei. Idempotent (gleicher Pfad ueberschreibt).
    /// </summary>
    void Register(PersistenceEntry entry);

    /// <summary>
    /// Entfernt einen Eintrag (z.B. nach Reset/Delete).
    /// </summary>
    void Unregister(string absolutePath);

    /// <summary>
    /// Alle registrierten Eintraege.
    /// </summary>
    IReadOnlyList<PersistenceEntry> GetAll();

    /// <summary>
    /// Eintraege gefiltert nach Typ.
    /// </summary>
    IEnumerable<PersistenceEntry> GetByType(PersistenceType type);

    /// <summary>
    /// Scannt das Filesystem nach bekannten Persistenz-Patterns und ergaenzt
    /// das Inventar um Files die nicht aktiv registriert sind (z.B. alte Logs,
    /// Profile-Files aus inaktiven Projekten).
    ///
    /// Bekannte Patterns:
    /// - %LocalAppData%\BauProjektManager\* (Configs, DBs)
    /// - %LocalAppData%\BauProjektManager\Logs\BPM_*.log
    /// - %LocalAppData%\BauProjektManager\Projects\*\planmanager.db
    /// - basePath\.AppData\BauProjektManager\* (CloudShared)
    /// - jedes projectRoot\.bpm\ rekursiv (ProjectLocal)
    /// </summary>
    void RescanFilesystem(string? basePath, IEnumerable<string> projectRoots);
}
