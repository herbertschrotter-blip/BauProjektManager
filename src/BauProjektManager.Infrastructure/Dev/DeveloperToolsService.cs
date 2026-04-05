using BauProjektManager.Domain.Interfaces;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Text;

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
    public string SettingsPath => System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "BauProjektManager", "settings.json");

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

    public string GetSystemInfo()
    {
        var sb = new System.Text.StringBuilder();

        // App
        var version = System.Reflection.Assembly.GetEntryAssembly()
            ?.GetName().Version?.ToString() ?? "unbekannt";
        sb.AppendLine($"App-Version:       {version}");
        sb.AppendLine($".NET Runtime:      {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Windows:           {Environment.OSVersion}");
        sb.AppendLine($"Rechner:           {Environment.MachineName}");
        sb.AppendLine($"Benutzer:          {Environment.UserName}");

        // DB
        sb.AppendLine($"DB-Pfad:           {_dbPath}");
        if (File.Exists(_dbPath))
        {
            var size = new FileInfo(_dbPath).Length / 1024.0;
            sb.AppendLine($"DB-Größe:          {size:F1} KB");
        }
        else
        {
            sb.AppendLine("DB-Größe:          (nicht vorhanden)");
        }

        // Freier Speicher
        try
        {
            var root = Path.GetPathRoot(_dbPath) ?? "C:\\";
            var drive = new DriveInfo(root);
            sb.AppendLine($"Freier Speicher:   {drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0:F1} GB");
        }
        catch { sb.AppendLine("Freier Speicher:   (nicht ermittelbar)"); }

        return sb.ToString();
    }

    public void OpenLogDirectory()
    {
        if (!Directory.Exists(_logDirectory)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", _logDirectory)
        {
            UseShellExecute = true
        });
    }

    public void RequestFullReset(Action shutdownAction)
    {
        RequestResetInternal(includeSettings: true, shutdownAction);
    }

    public void RequestDatabaseReset(Action shutdownAction)
    {
        RequestResetInternal(includeSettings: false, shutdownAction);
    }

    private void RequestResetInternal(bool includeSettings, Action shutdownAction)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Executable-Pfad nicht ermittelbar.");

        var pid = Environment.ProcessId;
        var bat = Path.Combine(Path.GetTempPath(), $"bpm_reset_{Guid.NewGuid():N}.bat");
        var settingsLine = includeSettings
            ? $"del /f /q \"{SettingsPath}\" 2>nul"
            : "rem keine Settings-Löschung";

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
            {{settingsLine}}
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

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

        shutdownAction();
    }
}
