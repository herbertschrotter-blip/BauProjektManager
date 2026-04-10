using System.IO;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Splittet Dateinamen an konfigurierbaren Trennzeichen in Segmente.
/// Reine Logik, keine Abhängigkeiten. (ADR-022)
/// </summary>
public static class FileNameParser
{
    private static readonly char[] _defaultDelimiters = ['-', '_'];

    /// <summary>
    /// Parst einen Dateinamen in Segmente.
    /// Extension wird abgetrennt, leere Segmente werden ignoriert.
    /// </summary>
    public static ParsedFileName Parse(string fileName, char[]? delimiters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var effectiveDelimiters = delimiters ?? _defaultDelimiters;
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        var rawParts = baseName.Split(effectiveDelimiters, StringSplitOptions.None);

        var segments = new List<FileNameSegment>();
        var position = 0;

        foreach (var part in rawParts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            segments.Add(new FileNameSegment
            {
                Position = position,
                RawValue = part.Trim()
            });
            position++;
        }

        return new ParsedFileName
        {
            OriginalFileName = fileName,
            BaseName = baseName,
            Extension = extension,
            Segments = segments,
            UsedDelimiters = effectiveDelimiters.Select(c => c.ToString()).ToList()
        };
    }
}
