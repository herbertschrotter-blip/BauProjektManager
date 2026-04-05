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
}
