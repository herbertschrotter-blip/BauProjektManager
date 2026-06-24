using BauProjektManager.Domain.Models;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Lesender Dateisystem-Port (ADR-060, BPM-112). Kapselt alle lesenden
/// System.IO-Zugriffe, damit Module testbar bleiben (In-Memory-Fake) und
/// kein direktes System.IO ausserhalb des Infrastructure-Adapters existiert.
/// </summary>
public interface IFileSystemReader
{
    /// <summary>Ob eine Datei am Pfad existiert.</summary>
    bool FileExists(string path);

    /// <summary>Ob ein Verzeichnis am Pfad existiert.</summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Dateien in einem Verzeichnis. <paramref name="recursive"/> = true bezieht
    /// alle Unterverzeichnisse ein.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false);

    /// <summary>
    /// Unterverzeichnisse eines Verzeichnisses. <paramref name="recursive"/> = true
    /// bezieht alle Ebenen ein.
    /// </summary>
    IEnumerable<string> EnumerateDirectories(string path, bool recursive = false);

    /// <summary>
    /// Metadaten-Schnappschuss einer Datei. Liefert immer ein Ergebnis;
    /// bei fehlender Datei ist <see cref="FileInfoSnapshot.Exists"/> = false.
    /// </summary>
    FileInfoSnapshot GetFileInfo(string path);

    /// <summary>Oeffnet einen lesenden Stream auf die Datei (z. B. fuer Hashing).</summary>
    Stream OpenRead(string path);
}
