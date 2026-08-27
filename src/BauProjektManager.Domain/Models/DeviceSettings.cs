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

    /// <summary>
    /// DevTools-Einstellungen — Filter und Diagnose-Optionen.
    /// Device-spezifisch; nicht ins shared-config übernehmen.
    /// </summary>
    public DevToolsSettings DevTools { get; set; } = new();

    /// <summary>
    /// Zuletzt gemerkte Fensterlage des Hauptfensters (Win32 WINDOWPLACEMENT).
    /// Null = noch nie gespeichert (Erststart) → App startet maximiert.
    /// Bewusst geräte-lokal: Monitor-Setup unterscheidet sich pro Maschine.
    /// </summary>
    public WindowPlacementSettings? MainWindowPlacement { get; set; }

    /// <summary>
    /// Geräte-lokale UI-Layout-Werte (Panel-Breiten etc.). Bewusst pro Gerät:
    /// Monitor-/Fenstergrößen unterscheiden sich je Maschine.
    /// </summary>
    public UiLayoutSettings UiLayout { get; set; } = new();
}

/// <summary>
/// UI-Layout-Zustand — wird in device-settings.json unter "uiLayout" persistiert.
/// </summary>
public class UiLayoutSettings
{
    /// <summary>
    /// Breite des PDF-Vorschau-Panels im Tab "Manuell sortieren" (BPM-111.06,
    /// per Splitter angepasst). Null = noch nie verändert → Default-Breite.
    /// </summary>
    public double? PlanPreviewWidth { get; set; }

    /// <summary>
    /// Breite des Detail-Panels im Tab "Manuell sortieren" (BPM-111.06 Slice D,
    /// per Splitter angepasst). Null = noch nie verändert → Default-Breite.
    /// </summary>
    public double? PlanDetailWidth { get; set; }
}

/// <summary>
/// Serialisierbare Form der Win32-<c>WINDOWPLACEMENT</c>-Struktur: der
/// Wiederherstellungs-Rahmen (<see cref="Left"/>/<see cref="Top"/>/<see cref="Right"/>/<see cref="Bottom"/>,
/// in Arbeitsbereich-Koordinaten) plus der Anzeigemodus (<see cref="ShowCmd"/>).
/// Windows klemmt beim Wiederherstellen selbst auf einen sichtbaren Bildschirm
/// und behandelt unterschiedliche DPI je Monitor korrekt.
/// </summary>
public class WindowPlacementSettings
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    /// <summary>Win32 SW_-Konstante: 1 = Normal, 3 = Maximiert (Minimiert wird nie gespeichert).</summary>
    public int ShowCmd { get; set; } = 1;
}

/// <summary>
/// DevTools-Konfiguration — wird in device-settings.json unter "devTools" persistiert.
/// </summary>
public class DevToolsSettings
{
    public LogFilterSettings LogFilter { get; set; } = new();
}

/// <summary>
/// Log-Filter-Auswahl im DevToolsDialog. Wird beim Wechsel sofort persistiert.
/// </summary>
public class LogFilterSettings
{
    /// <summary>
    /// Filtermodus: last200Lines | lastNLines | currentSession | previousSession | entireFile | specificSession
    /// </summary>
    public string Mode { get; set; } = "last200Lines";

    /// <summary>
    /// Zeilenanzahl für mode = "lastNLines". Range 10-10000.
    /// </summary>
    public int CustomLineCount { get; set; } = 200;

    /// <summary>
    /// Session-Nummer für mode = "specificSession". 0 = keine ausgewählt.
    /// </summary>
    public int SelectedSessionNumber { get; set; } = 0;
}
