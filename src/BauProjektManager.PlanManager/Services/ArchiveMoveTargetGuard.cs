namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Zielordner-Schutz des Archiv-Moves (R01-008.01, ADR-064 P.11, ADR-061 P.6):
/// reine Pfad-String-Pruefung, kein Dateisystemzugriff. Gesperrt sind Ziele
/// ausserhalb des Projekts, Ordner mit fuehrendem '_' (_Eingang, _Archiv) oder
/// '.' (System). Fuer den Explorer-Move zusaetzlich: nur der eigene Ablageordner
/// samt Unterordnern (Umklassifizieren nur ueber das Radial im Archiv-Tab).
/// </summary>
public static class ArchiveMoveTargetGuard
{
    /// <summary>Null = Ziel zulaessig, sonst der Grund fuer die Sperre.</summary>
    public static string? Check(string targetRelativeDirectory, string? allowedArea)
    {
        var target = TrimDirectory(targetRelativeDirectory);
        if (target.Length == 0)
            return "Kein Zielordner gewählt";
        if (target.StartsWith('/') || target.StartsWith('\\') || target.Contains(':'))
            return "Ziel muss im Projektordner liegen";

        var segments = Segments(target);
        if (segments.Any(s => s is "." or ".."))
            return "Ungültiger Zielordner";
        if (segments.Any(s => s.StartsWith('_') || s.StartsWith('.')))
            return "Ziel gesperrt: _Eingang, _Archiv und System-Ordner sind keine Ablage";

        if (allowedArea is not null && !IsWithin(target, allowedArea))
            return $"Ziel liegt außerhalb des Ablageordners „{allowedArea}“ — " +
                   "Umklassifizieren nur über das Radial im Archiv-Tab";
        return null;
    }

    /// <summary>True, wenn <paramref name="directory"/> gleich <paramref name="area"/> oder darunter liegt.</summary>
    public static bool IsWithin(string directory, string area)
    {
        var dir = Segments(directory);
        var root = Segments(area);
        if (dir.Length < root.Length)
            return false;
        for (var i = 0; i < root.Length; i++)
            if (!string.Equals(dir[i], root[i], StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }

    /// <summary>Pfadgleichheit unabhaengig von Trennzeichen und Gross-/Kleinschreibung.</summary>
    public static bool SamePath(string a, string b) =>
        string.Equals(string.Join('/', Segments(a)), string.Join('/', Segments(b)),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Entfernt Leerraum und abschliessende Trennzeichen.</summary>
    public static string TrimDirectory(string directory) =>
        directory.Trim().TrimEnd('/', '\\');

    private static string[] Segments(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
