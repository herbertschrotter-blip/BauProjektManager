using System.IO;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Splittet Dateinamen an konfigurierbaren Trennzeichen in Segmente.
/// Supports TokenizationConfig (v2): collapseRepeatedDelimiters, firstTokenDelimiter.
/// Reine Logik, keine Abhängigkeiten. (ADR-022)
/// </summary>
public static class FileNameParser
{
    private static readonly char[] _defaultDelimiters = ['-', '_'];

    /// <summary>
    /// Parst einen Dateinamen in Segmente using TokenizationConfig.
    /// </summary>
    public static ParsedFileName Parse(string fileName, TokenizationConfig? tokenization = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var config = tokenization ?? new TokenizationConfig();
        var delimiters = config.Delimiters.Count > 0
            ? config.Delimiters.Where(d => d.Length == 1).Select(d => d[0]).ToArray()
            : _defaultDelimiters;

        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        // Step 1: FirstTokenDelimiter — split off first token before main delimiters
        string? firstToken = null;
        var textToParse = baseName;

        if (!string.IsNullOrEmpty(config.FirstTokenDelimiter)
            && config.FirstTokenDelimiter.Length == 1)
        {
            var ftDelim = config.FirstTokenDelimiter[0];
            var ftIndex = baseName.IndexOf(ftDelim);
            if (ftIndex > 0)
            {
                firstToken = baseName[..ftIndex].Trim();
                textToParse = baseName[(ftIndex + 1)..].Trim();
            }
        }

        // Step 2: Split at delimiters
        var rawParts = textToParse.Split(delimiters, StringSplitOptions.None);

        // Step 3: Build segments, optionally collapsing repeated delimiters
        var segments = new List<FileNameSegment>();
        var position = 0;

        // Add first token as segment 0 if extracted
        if (firstToken is not null)
        {
            segments.Add(new FileNameSegment
            {
                Position = position,
                RawValue = firstToken
            });
            position++;
        }

        foreach (var part in rawParts)
        {
            // CollapseRepeatedDelimiters: skip empty parts from consecutive delimiters
            if (config.CollapseRepeatedDelimiters && string.IsNullOrWhiteSpace(part))
                continue;

            // Standard: skip truly empty parts
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
            UsedDelimiters = delimiters.Select(c => c.ToString()).ToList()
        };
    }

    /// <summary>
    /// Legacy overload: parst mit char-Array Delimiter (für bestehende Aufrufe).
    /// </summary>
    public static ParsedFileName Parse(string fileName, char[]? delimiters)
    {
        var config = new TokenizationConfig();
        if (delimiters is not null)
            config.Delimiters = delimiters.Select(c => c.ToString()).ToList();
        return Parse(fileName, config);
    }
}
