namespace BauProjektManager.Domain.Interfaces;

public interface IDeveloperToolsService
{
    string DatabasePath { get; }
    string LogDirectory { get; }
    string ReadLogTail(int lineCount = 200);
    void OpenLogDirectory();
    void RequestDatabaseResetAndRestart();
}
