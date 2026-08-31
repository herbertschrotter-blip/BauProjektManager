namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Schreibender Dateisystem-Port (ADR-060, BPM-112). Bewusst schmal: fuer
/// PLANDATEIEN erzeugt BPM keine Inhalte, sondern legt Ordner an und
/// verschiebt/kopiert/loescht vorhandene Dateien (Import aus dem Eingang).
/// Einzige Inhalts-Schreiboperation ist <see cref="WriteAllText"/> fuer
/// App-eigene JSON-Konfigs (Profile, pattern-templates — BPM-112.01).
/// </summary>
public interface IFileSystemWriter
{
    /// <summary>Legt ein Verzeichnis inkl. fehlender Elternverzeichnisse an. Idempotent.</summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Verschiebt eine Datei. <paramref name="overwrite"/> = false wirft, wenn das
    /// Ziel existiert. Das Zielverzeichnis muss existieren.
    /// </summary>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>
    /// Kopiert eine Datei. <paramref name="overwrite"/> = false wirft, wenn das
    /// Ziel existiert. Das Zielverzeichnis muss existieren.
    /// </summary>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>Loescht eine Datei. Kein Fehler, wenn sie nicht existiert (No-Op).</summary>
    void DeleteFile(string path);

    /// <summary>
    /// Schreibt Text (UTF-8) in eine Datei, ueberschreibt vorhandenen Inhalt.
    /// NUR fuer App-eigene JSON-Konfigs — nie fuer Plandateien (BPM-112.01).
    /// </summary>
    void WriteAllText(string path, string content);
}
