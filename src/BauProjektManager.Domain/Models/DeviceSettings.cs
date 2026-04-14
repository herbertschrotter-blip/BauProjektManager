namespace BauProjektManager.Domain.Models;

/// <summary>
/// Gerätespezifische Einstellungen — gespeichert in device-settings.json
/// unter %LocalAppData%\BauProjektManager\ (synct NICHT).
/// Enthält Pfade und Maschineninfo die pro Gerät unterschiedlich sind.
/// </summary>
public class DeviceSettings
{
    public string SchemaVersion { get; set; } = "1.1";

    /// <summary>
    /// Stabile Geräte-ID (GUID). Wird beim Erststart einmalig generiert.
    /// Dient zur Identifikation des Geräts im Multi-Device-Betrieb.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// WorkspaceId des zuletzt gebundenen Workspace.
    /// Ermöglicht Erkennung ob sich der Datenbestand geändert hat (Rebind).
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Pfad zum Cloud-Speicher-Root (z.B. OneDrive, Dropbox, Google Drive).
    /// Cloud-neutral — kein bestimmter Anbieter vorausgesetzt.
    /// </summary>
    public string CloudStoragePath { get; set; } = string.Empty;

    public string BasePath { get; set; } = string.Empty;
    public string ArchivePath { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
    public bool IsFirstRun { get; set; } = true;
    public DateTime? SetupCompletedAt { get; set; }
}
