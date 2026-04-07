namespace BauProjektManager.Domain.Interfaces;

public interface IDeveloperToolsService
{
    string DatabasePath { get; }
    string LogDirectory { get; }
    string ReadLogTail(int lineCount = 200);
    void OpenLogDirectory();
    string SettingsPath { get; }
    string GetSystemInfo();
    /// <summary>Startet den Batch-Reset und ruft danach shutdownAction() auf.</summary>
    void RequestDatabaseReset(Action shutdownAction);
    /// <summary>Löscht DB + Settings und startet neu — simuliert Ersteinrichtung.</summary>
    void RequestFullReset(Action shutdownAction);
    /// <summary>Löscht nur settings.json und startet neu.</summary>
    void RequestSettingsReset(Action shutdownAction);
    /// <summary>Setzt IsFirstRun = true und startet neu — Ersteinrichtung neu durchlaufen.</summary>
    void RequestFirstRunReset(Action shutdownAction);
}
