using System.IO;
using System.Text;
using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Standard-Implementierung der zentralen Wert-Normalisierung (BPM-111.02).
/// Seit BPM-111.05 in Infrastructure (statt PlanManager), weil auch
/// ProjectDatabase/Seed die folder_name-Erzeugung brauchen (ADR-059-Addendum).
/// NormalizeForKey ist die unveraenderte fruehere DocumentKeyBuilder-Logik
/// (Schluessel-Stabilitaet!).
/// </summary>
public class PlanValueNormalizer : IPlanValueNormalizer
{
    private const int MaxFolderNameLength = 100;

    public string NormalizeForKey(string value)
    {
        var normalized = Transliterate(value.ToLowerInvariant().Trim());
        normalized = normalized
            .Replace(" ", "_").Replace("-", "_")
            .Replace(".", "_").Replace(",", "");
        while (normalized.Contains("__"))
            normalized = normalized.Replace("__", "_");
        return normalized.Trim('_');
    }

    public string NormalizeForMatch(string value)
    {
        var normalized = Transliterate(value.ToLowerInvariant().Trim());
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (ch is ' ' or '-' or '_' or '.' or ',')
                continue;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    public string NormalizeForFolderName(string value)
    {
        // BPM-112.02: bewusst System.IO.Path — reine Zeichensatz-Abfrage ohne
        // Disk-Zugriff, zudem Infrastructure-Schicht (adapter-nah).
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        var lastWasSpace = false;
        foreach (var ch in value.Trim())
        {
            if (invalid.Contains(ch))
                continue;
            if (char.IsWhiteSpace(ch))
            {
                if (lastWasSpace) continue;
                sb.Append(' ');
                lastWasSpace = true;
                continue;
            }
            sb.Append(ch);
            lastWasSpace = false;
        }
        var result = sb.ToString().Trim(' ', '.');
        if (result.Length > MaxFolderNameLength)
            result = result[..MaxFolderNameLength].Trim(' ', '.');
        return result;
    }

    private static string Transliterate(string value) => value
        .Replace("ä", "ae").Replace("ö", "oe")
        .Replace("ü", "ue").Replace("ß", "ss");
}
