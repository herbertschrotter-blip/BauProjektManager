namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Pfad-Port (ADR-060, BPM-112). Deterministische, plattform-korrekte
/// Pfadoperationen ohne Festplattenzugriff. Kapselt System.IO.Path, damit
/// Domain/Module keine direkte System.IO-Abhaengigkeit tragen.
/// </summary>
public interface IPathService
{
    /// <summary>Verbindet Pfadsegmente zu einem Pfad.</summary>
    string Combine(params string[] paths);

    /// <summary>Verzeichnisanteil eines Pfads (null bei Wurzel/leer).</summary>
    string? GetDirectoryName(string path);

    /// <summary>Datei- oder letzter Verzeichnisname eines Pfads.</summary>
    string GetFileName(string path);

    /// <summary>Dateiendung inkl. fuehrendem Punkt (z. B. ".pdf").</summary>
    string GetExtension(string path);

    /// <summary>Dateiname ohne Endung (BPM-120 T1, fuer Archivnamen).</summary>
    string GetFileNameWithoutExtension(string path);

    /// <summary>Relativer Pfad von <paramref name="relativeTo"/> nach <paramref name="path"/>.</summary>
    string GetRelativePath(string relativeTo, string path);
}
