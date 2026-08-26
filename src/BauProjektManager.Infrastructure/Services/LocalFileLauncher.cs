using System.Diagnostics;
using System.IO;
using BauProjektManager.Domain.Interfaces;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Shell-Launcher-Adapter (ADR-060 Punkt 3): öffnet Dateien/Ordner über
/// ShellExecute bzw. den Explorer. Loggt nur Dateinamen, nie volle Pfade
/// (DSGVO-Pfadregel aus ADR-060).
/// </summary>
public sealed class LocalFileLauncher : IFileLauncher
{
    public bool OpenFile(string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            Log.Warning("FileLauncher: Datei nicht gefunden: {Name}", Path.GetFileName(absolutePath));
            return false;
        }
        return TryStart(new ProcessStartInfo(absolutePath) { UseShellExecute = true },
            Path.GetFileName(absolutePath));
    }

    public bool OpenFolder(string absoluteDirectory)
    {
        if (!Directory.Exists(absoluteDirectory))
        {
            Log.Warning("FileLauncher: Ordner nicht gefunden: {Name}", Path.GetFileName(absoluteDirectory));
            return false;
        }
        return TryStart(new ProcessStartInfo(absoluteDirectory) { UseShellExecute = true },
            Path.GetFileName(absoluteDirectory));
    }

    public bool RevealInExplorer(string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            Log.Warning("FileLauncher: Datei nicht gefunden: {Name}", Path.GetFileName(absolutePath));
            return false;
        }
        // /select, erwartet den Pfad in Anfuehrungszeichen (Leerzeichen in Pfaden)
        return TryStart(new ProcessStartInfo("explorer.exe", $"/select,\"{absolutePath}\""),
            Path.GetFileName(absolutePath));
    }

    private static bool TryStart(ProcessStartInfo psi, string displayName)
    {
        try
        {
            Process.Start(psi);
            Log.Debug("FileLauncher: gestartet fuer {Name}", displayName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "FileLauncher: Start fehlgeschlagen fuer {Name}", displayName);
            return false;
        }
    }
}
