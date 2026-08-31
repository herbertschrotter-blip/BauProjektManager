using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Lokaler Dateisystem-Adapter (ADR-060, BPM-112). Der EINZIGE Ort mit direktem
/// System.IO. Implementiert die drei Ports und wird via DI als Singleton an alle
/// Module gereicht.
///
/// DSGVO Klasse A: Pfade koennen Personenbezug tragen (Projektordner nach
/// Kundennamen) — Logging maskiert daher zentral auf den Datei-/Ordnernamen,
/// nie wird der volle Pfad geloggt.
/// </summary>
public sealed class LocalFileSystem : IFileSystemReader, IFileSystemWriter, IPathService
{
    // --- IFileSystemReader ---

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false)
        => Directory.EnumerateFiles(path, searchPattern,
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public IEnumerable<string> EnumerateDirectories(string path, bool recursive = false)
        => Directory.EnumerateDirectories(path, "*",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public FileInfoSnapshot GetFileInfo(string path)
    {
        var info = new FileInfo(path);
        return info.Exists
            ? new FileInfoSnapshot(info.FullName, true, info.Length, info.LastWriteTimeUtc)
            : new FileInfoSnapshot(path, false, 0, default);
    }

    public Stream OpenRead(string path) => File.OpenRead(path);

    // --- IFileSystemWriter ---

    public void CreateDirectory(string path)
    {
        if (Directory.Exists(path))
            return;

        Directory.CreateDirectory(path);
        Log.Information("Verzeichnis angelegt: {Folder}", Mask(path));
    }

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Move(sourcePath, destinationPath, overwrite);
        Log.Information("Datei verschoben: {Source} -> {Dest}", Mask(sourcePath), Mask(destinationPath));
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
        Log.Information("Datei kopiert: {Source} -> {Dest}", Mask(sourcePath), Mask(destinationPath));
    }

    public void DeleteFile(string path)
    {
        if (!File.Exists(path))
            return;

        File.Delete(path);
        Log.Information("Datei geloescht: {File}", Mask(path));
    }

    // --- IPathService (deterministisch, kein Festplattenzugriff) ---

    public string Combine(params string[] paths) => Path.Combine(paths);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public string GetExtension(string path) => Path.GetExtension(path);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string GetRelativePath(string relativeTo, string path) => Path.GetRelativePath(relativeTo, path);

    // --- intern ---

    /// <summary>
    /// DSGVO-Pfadmaskierung fuers Logging: nur der letzte Pfadbestandteil
    /// (Datei-/Ordnername). Der uebergeordnete Pfad (potenziell Kundenname)
    /// wird verworfen.
    /// </summary>
    private static string Mask(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "…";

        var name = Path.GetFileName(
            path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrEmpty(name) ? "…" : "…/" + name;
    }
}
