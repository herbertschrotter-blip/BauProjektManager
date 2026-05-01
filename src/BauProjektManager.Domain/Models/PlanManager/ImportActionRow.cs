namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Eine Zeile aus der Tabelle import_actions, geladen für Recovery-Operationen.
/// Repräsentiert eine geplante oder bereits ausgeführte Datei-Bewegung im Rahmen eines Imports.
/// Siehe BPM-016 / 016.03.
/// </summary>
/// <param name="Id">ULID der Action.</param>
/// <param name="ActionType">"new" / "indexUpdate" / "indexNew" / "fileChange" / "unknown".</param>
/// <param name="ActionStatus">"pending" / "completed" / "failed".</param>
/// <param name="SourcePath">Original-Pfad in Inbox (relativer Pfad zum Project-Root).</param>
/// <param name="DestinationPath">Ziel-Pfad in Plans (relativer Pfad zum Project-Root).</param>
/// <param name="ArchivePath">Optional: Pfad der alten Datei nach _Archiv (bei indexUpdate).</param>
public sealed record ImportActionRow(
    string Id,
    string ActionType,
    string ActionStatus,
    string SourcePath,
    string DestinationPath,
    string? ArchivePath);
