using BauProjektManager.Domain.Interfaces;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace BauProjektManager.Infrastructure.Dev;

public sealed class DeveloperToolsService : IDeveloperToolsService
{
    private readonly string _dbPath;
    private readonly string _logDirectory;

    public DeveloperToolsService(string dbPath, string logDirectory)
    {
        _dbPath = dbPath;
        _logDirectory = logDirectory;
    }

    public string DatabasePath => _dbPath;
    public string LogDirectory => _logDirectory;

    public string ReadLogTail(int lineCount = 200)
    {
        try
        {
            var files = Directory.GetFiles(_logDirectory, "BPM_*.log")
                                 .OrderByDescending(f => f)
                                 .ToArray();
            if (files.Length == 0) return "(Keine Log-Datei gefunden)";

            using var stream = new FileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) is not null)
                lines.Add(line);

            var tail = lines.Count <= lineCount
                ? lines
                : lines.GetRange(lines.Count - lineCount, lineCount);

            return string.Join(Environment.NewLine, tail);
        }
        catch (Exception ex)
        {
            Log.Warning("DevTools: Log lesen fehlgeschlagen: {Error}", ex.Message);
            return $"(Fehler beim Lesen: {ex.Message})";
        }
    }

    public void OpenLogDirectory()
    {
        if (!Directory.Exists(_logDirectory)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", _logDirectory)
        {
            UseShellExecute = true
        });
    }

    public void RequestDatabaseResetAndRestart()
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Executable-Pfad nicht ermittelbar.");

        var pid = Environment.ProcessId;
        var bat = Path.Combine(Path.GetTempPath(), $"bpm_reset_{Guid.NewGuid():N}.bat");

        var script = $$"""
            @echo off
            setlocal EnableExtensions EnableDelayedExpansion
            set waitRetries=0
            set deleteRetries=0

            :wait
            tasklist /fi "pid eq {{pid}}" 2>nul | find "{{pid}}" >nul
            if errorlevel 1 goto delete
            set /a waitRetries+=1
            if !waitRetries! geq 30 goto failed
            timeout /t 1 /nobreak >nul
            goto wait

            :delete
            del /f /q "{{_dbPath}}" 2>nul
            del /f /q "{{_dbPath}}-wal" 2>nul
            del /f /q "{{_dbPath}}-shm" 2>nul
            if exist "{{_dbPath}}" goto retryDelete
            if exist "{{_dbPath}}-wal" goto retryDelete
            if exist "{{_dbPath}}-shm" goto retryDelete
            goto restart

            :retryDelete
            set /a deleteRetries+=1
            if !deleteRetries! geq 30 goto failed
            timeout /t 1 /nobreak >nul
            goto delete

            :restart
            start "" "{{exePath}}"
            goto cleanup

            :failed
            echo %date% %time% Reset fehlgeschlagen. waitRetries=!waitRetries! deleteRetries=!deleteRetries! > "%TEMP%\bpm_reset_failed.txt"

            :cleanup
            del /f /q "%~f0"
            """;

        File.WriteAllText(bat, script, Encoding.GetEncoding(850));

        var started = Process.Start(new ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c \"{bat}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (started is null)
            throw new InvalidOperationException("Reset-Script konnte nicht gestartet werden.");

        Application.Current.Shutdown();
    }
}
