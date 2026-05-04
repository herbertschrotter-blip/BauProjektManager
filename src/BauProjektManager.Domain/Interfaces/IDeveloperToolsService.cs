namespace BauProjektManager.Domain.Interfaces;

public interface IDeveloperToolsService
{
    string DatabasePath { get; }
    string LogDirectory { get; }
    string ReadLogTail(int lineCount = 200);

    /// <summary>
    /// Liest komplette aktuelle Logfile (das neueste BPM_*.log).
    /// </summary>
    string ReadEntireLog();

    /// <summary>
    /// Liest Log-Zeilen ab dem letzten App-Start-Marker bis Ende.
    /// </summary>
    string ReadCurrentSession();

    /// <summary>
    /// Liest die vorherige Session = Session-Nummer (current - 1).
    /// Sucht über alle BPM_*.log Files. Kein Tages-Limit.
    /// </summary>
    string ReadPreviousSession();

    /// <summary>
    /// Aktuelle Session-Nummer aus dem letzten App-Start-Marker im aktuellen Logfile.
    /// 0 wenn keine Marker / Nummer gefunden.
    /// </summary>
    int GetCurrentSessionNumber();

    /// <summary>
    /// Alle Session-Nummern aus allen BPM_*.log Files, absteigend sortiert (neueste zuerst).
    /// Leere Liste wenn keine Marker mit Nummer gefunden.
    /// </summary>
    IReadOnlyList<int> GetAvailableSessionNumbers();

    /// <summary>
    /// Inhalt einer spezifischen Session (über alle Logfiles hinweg).
    /// Hinweis-Text wenn Session-Nummer nicht gefunden.
    /// </summary>
    string ReadSessionByNumber(int sessionNumber);

    /// <summary>
    /// Liefert Größe des aktuellen Logfiles in Bytes.
    /// 0 wenn keine Datei vorhanden.
    /// </summary>
    long GetCurrentLogFileSize();

    /// <summary>
    /// Liefert Dateinamen des aktuellen Logfiles (ohne Pfad), z.B. "BPM_20260504.log".
    /// Leerstring wenn keine Datei vorhanden.
    /// </summary>
    string GetCurrentLogFileName();

    void OpenLogDirectory();
    string SettingsPath { get; }
    string GetSystemInfo();
    string GetDisplayInfo();
    /// <summary>Startet den Batch-Reset und ruft danach shutdownAction() auf.</summary>
    void RequestDatabaseReset(Action shutdownAction);
    /// <summary>Löscht DB + Settings und startet neu — simuliert Ersteinrichtung.</summary>
    void RequestFullReset(Action shutdownAction);
    /// <summary>Löscht nur settings.json und startet neu.</summary>
    void RequestSettingsReset(Action shutdownAction);
    /// <summary>Setzt IsFirstRun = true und startet neu — Ersteinrichtung neu durchlaufen.</summary>
    void RequestFirstRunReset(Action shutdownAction);

    /// <summary>BPM-104.04: Loescht alle Logfiles in %LocalAppData%\BauProjektManager\Logs\. Kein Restart.</summary>
    int DeleteAllLogs();

    /// <summary>BPM-104.04: Loescht eine Liste konkreter Files. Liefert Anzahl erfolgreich geloeschter Files.</summary>
    int DeleteFiles(IEnumerable<string> absolutePaths);
}
