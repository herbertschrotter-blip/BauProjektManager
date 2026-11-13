using System.Text.RegularExpressions;
using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Deterministisches Namensschema archivierter Vorgaenger-Dateien (ADR-059-Addendum P.1/P.2,
/// ADR-064 P.2): <c>&lt;Ordner der alten Datei&gt;/_Archiv/&lt;Name&gt;_&lt;yyyyMMdd_HHmmss&gt;&lt;ext&gt;</c>.
/// Weil der Archivpfad den Originalpfad eindeutig enthaelt, koennen Undo, Recovery und
/// Rollback die Datei ohne zusaetzliche Journal-Spalte an ihren alten Ort und Namen
/// zurueckbringen — auch wenn die neue Revision einen anderen Dateinamen hat (R01-007.03).
/// </summary>
public static partial class ArchivePathScheme
{
    public const string FolderName = "_Archiv";

    [GeneratedRegex(@"_\d{8}_\d{6}$")]
    private static partial Regex StampSuffix();

    /// <summary>Archivpfad (relativ) fuer die Datei unter <paramref name="originalRelPath"/>.</summary>
    public static string Build(IPathService path, string originalRelPath, DateTime stamp)
    {
        var dir = path.GetDirectoryName(originalRelPath) ?? "";
        var name = $"{path.GetFileNameWithoutExtension(originalRelPath)}_{stamp:yyyyMMdd_HHmmss}"
                   + path.GetExtension(originalRelPath);
        return path.Combine(dir, FolderName, name);
    }

    /// <summary>Originalpfad (relativ), aus dem <paramref name="archiveRelPath"/> entstanden ist.</summary>
    public static string OriginalOf(IPathService path, string archiveRelPath)
    {
        var archiveDir = path.GetDirectoryName(archiveRelPath) ?? "";
        var dir = path.GetDirectoryName(archiveDir) ?? "";
        var stem = StampSuffix().Replace(path.GetFileNameWithoutExtension(archiveRelPath), "");
        var name = stem + path.GetExtension(archiveRelPath);
        return dir.Length == 0 ? name : path.Combine(dir, name);
    }
}
