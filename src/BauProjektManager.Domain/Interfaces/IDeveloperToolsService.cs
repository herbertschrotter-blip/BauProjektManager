namespace BauProjektManager.Domain.Interfaces;

public interface IDeveloperToolsService
{
    string DatabasePath { get; }
    string LogDirectory { get; }
    string ReadLogTail(int lineCount = 200);
    void OpenLogDirectory();
    /// <summary>Startet den Batch-Reset und ruft danach shutdownAction() auf.</summary>
    void RequestDatabaseReset(Action shutdownAction);
}
