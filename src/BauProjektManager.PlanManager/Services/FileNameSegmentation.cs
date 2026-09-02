namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Pure Zerlegung eines Dateinamens in Segment-Atome und Trennzeichen
/// (BPM-126c, Interaktionsmuster des ProfilWizard-Schritts 2).
///
/// Der Name wird an ALLEN Trennzeichen in Atome zerlegt; welche Trenner
/// tatsaechlich trennen, entscheidet der Aktiv-Zustand: ein deaktivierter
/// Trenner verschmilzt seine Nachbarn zu EINEM sichtbaren Segment
/// (Beispiel: "5998-130" = zwei Atome mit deaktiviertem '-').
///
/// Pure Logic: keine Disk-Zugriffe, System.IO.Path nur fuer String-Ops
/// (ADR-060-Praezisierung, Teil 49).
/// </summary>
public static class FileNameSegmentation
{
    /// <summary>Trennzeichen, an denen grundsaetzlich zerlegt wird.</summary>
    public const string SeparatorChars = "-_. ";

    /// <summary>
    /// Zerlegt den Dateinamen. Die **Dateiendung ist KEIN Segment** — sie wird
    /// abgetrennt und separat gefuehrt (der Punkt davor ist kein Trenner):
    /// sie beschreibt das Format, nicht den Inhalt des Namens.
    /// </summary>
    public static SegmentationResult Split(string fileName)
    {
        // Pure String-Operation (ADR-060-Praezisierung): kein Disk-Zugriff.
        var extension = System.IO.Path.GetExtension(fileName);
        var stem = extension.Length > 0 ? fileName[..^extension.Length] : fileName;

        List<string> atoms = [];
        List<char> separators = [];
        var current = new System.Text.StringBuilder();

        foreach (var c in stem)
        {
            if (SeparatorChars.Contains(c))
            {
                atoms.Add(current.ToString());
                separators.Add(c);
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        atoms.Add(current.ToString());
        return new SegmentationResult(atoms, separators, extension);
    }

    /// <summary>
    /// Sichtbare Segmente aus Atomen + Trenner-Zustaenden: benachbarte Atome
    /// mit inaktivem Trenner verschmelzen (das Trennzeichen bleibt im Text).
    /// <paramref name="active"/> hat die Laenge der Trennerliste.
    /// </summary>
    public static List<MergedSegment> Merge(SegmentationResult source, IReadOnlyList<bool> active)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (active.Count != source.Separators.Count)
            throw new ArgumentException(
                $"Erwartet {source.Separators.Count} Trenner-Zustaende, erhalten {active.Count}.",
                nameof(active));

        List<MergedSegment> result = [];
        var text = new System.Text.StringBuilder(source.Atoms[0]);
        var startAtom = 0;

        for (var i = 0; i < source.Separators.Count; i++)
        {
            if (active[i])
            {
                result.Add(new MergedSegment(startAtom, text.ToString()));
                text.Clear();
                text.Append(source.Atoms[i + 1]);
                startAtom = i + 1;
            }
            else
            {
                text.Append(source.Separators[i]);
                text.Append(source.Atoms[i + 1]);
            }
        }
        result.Add(new MergedSegment(startAtom, text.ToString()));
        return result;
    }

    /// <summary>
    /// Startzustand: alle Trenner aktiv, ausser jenen, deren Zeichen nicht in
    /// <paramref name="activeChars"/> steht (globale Trenner-Wahl der UI).
    /// </summary>
    public static List<bool> InitialState(SegmentationResult source, string activeChars)
        => [.. source.Separators.Select(activeChars.Contains)];
}

/// <summary>
/// Zerlegung eines Dateinamens: n Atome, n-1 Trennzeichen dazwischen.
/// <paramref name="Extension"/> ist die abgetrennte Dateiendung inkl. Punkt
/// (leer wenn keine) — sie ist bewusst KEIN Segment.
/// </summary>
public sealed record SegmentationResult(
    IReadOnlyList<string> Atoms,
    IReadOnlyList<char> Separators,
    string Extension = "");

/// <summary>
/// Ein sichtbares Segment nach dem Verschmelzen. <paramref name="StartAtomIndex"/>
/// ist der stabile Anker fuer Typ-Zuweisungen (ueberlebt Merge/Split der Trenner).
/// </summary>
public sealed record MergedSegment(int StartAtomIndex, string Text);
