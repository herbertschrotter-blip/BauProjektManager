using System.Text;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Erzeugt stabile <c>token_key</c>-Werte fuer Segmenttypen (BPM-108).
/// Konvention: snake_case ohne Umlaute, Sonderzeichen oder Mehrfach-Underscores.
/// </summary>
public static class TokenKeyGenerator
{
    /// <summary>
    /// Normalisiert einen freien Namen (z. B. "Akustik-Klasse") auf snake_case ("akustik_klasse").
    /// </summary>
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var lower = name.ToLowerInvariant().Trim();
        lower = lower
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        var sb = new StringBuilder(lower.Length);
        var lastUnderscore = false;
        foreach (var c in lower)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastUnderscore = false;
            }
            else
            {
                if (!lastUnderscore && sb.Length > 0) sb.Append('_');
                lastUnderscore = true;
            }
        }
        return sb.ToString().Trim('_');
    }

    /// <summary>
    /// Liefert einen einzigartigen token_key. Wenn <paramref name="isTaken"/> einen Konflikt
    /// meldet, wird ein numerischer Suffix angehaengt (z. B. <c>akustik_klasse_2</c>).
    /// </summary>
    public static string EnsureUnique(string baseKey, Func<string, bool> isTaken)
    {
        if (string.IsNullOrEmpty(baseKey)) baseKey = "custom";
        if (!isTaken(baseKey)) return baseKey;

        for (var i = 2; i < int.MaxValue; i++)
        {
            var candidate = $"{baseKey}_{i}";
            if (!isTaken(candidate)) return candidate;
        }
        // Theoretisch unerreichbar — Fallback
        return $"{baseKey}_{Guid.NewGuid():N}";
    }
}
