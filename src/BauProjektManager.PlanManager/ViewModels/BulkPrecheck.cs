namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Ergebnis-Stufe der Bulk-Vorprüfung (111.07 Slice B).</summary>
public enum BulkGate
{
    Allowed,
    Blocked
}

/// <summary>Ergebnis der Bulk-Vorprüfung: Gate + Warnhinweise für die Statuszeile.</summary>
public sealed record BulkPrecheckResult(
    BulkGate Gate,
    IReadOnlyList<string> Warnings,
    string? BlockReason);

/// <summary>
/// Pure Bulk-Vorprüfung vor dem Radial (BPM-111.07 Slice B, Entscheidung
/// Teil 48 „Hinweis + Deckel"): Die eigentliche Bestätigung ist im
/// Pending-Modell „Import bestätigen" — hier gibt es nur eine deutliche
/// Mengenwarnung (ab 9), einen harten Deckel (über 20 öffnet das Radial
/// nicht) und Kompatibilitäts-Warnungen (gemischte Dateitypen,
/// Plannummern-Kollision gleicher Dateitypen).
/// </summary>
public static class BulkPrecheck
{
    /// <summary>Ab dieser Anzahl erscheint die Mengenwarnung.</summary>
    public const int WarnThreshold = 9;

    /// <summary>Harter Deckel: darüber öffnet das Radial nicht.</summary>
    public const int MaxBulk = 20;

    /// <summary>
    /// Prüft die effektiv zuzuordnenden Zeilen (inkl. Paar-Partner,
    /// <see cref="ManualCaptureViewModel.ExpandWithPairedRows"/>).
    /// </summary>
    public static BulkPrecheckResult Evaluate(IReadOnlyList<CaptureRowViewModel> rows)
    {
        if (rows.Count > MaxBulk)
            return new BulkPrecheckResult(BulkGate.Blocked, [],
                $"{rows.Count} Dateien — mehr als {MaxBulk} pro Zuordnung. Auswahl verkleinern (z. B. je Typ/Geschoss).");

        var warnings = new List<string>();
        if (rows.Count >= WarnThreshold)
            warnings.Add($"{rows.Count} Dateien werden gemeinsam zugeordnet — im Panel prüfen, „Import bestätigen“ ist die Bestätigung");

        if (rows.Count >= 2)
        {
            // Dateitypen kompatibel? Fremd-Extensions gemischt mit Plandateien.
            var extensions = rows
                .Select(r => r.Item.File.Scan.Extension.ToLowerInvariant())
                .ToList();
            var foreign = extensions
                .Where(e => e is not (".pdf" or ".dwg"))
                .Distinct()
                .ToList();
            if (foreign.Count > 0 && extensions.Any(e => e is ".pdf" or ".dwg"))
                warnings.Add($"Gemischte Dateitypen ({string.Join(", ", foreign)}) — gehören alle zusammen?");

            // Gleiche Plannummer + gleicher Dateityp: die zweite Datei würde als
            // Zusatzdatei an DIESELBE Revision andocken (kein Paar wie PDF+DWG).
            var collisions = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Item.Candidates.PlanNumber))
                .GroupBy(r => (
                    Nr: r.Item.Candidates.PlanNumber!,
                    Ext: r.Item.File.Scan.Extension.ToLowerInvariant()))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.Nr)
                .Distinct()
                .ToList();
            if (collisions.Count > 0)
                warnings.Add($"Mehrfach gleiche Plannummer + Dateityp ({string.Join(", ", collisions)}) — Dateien würden an EINE Revision andocken");
        }

        return new BulkPrecheckResult(BulkGate.Allowed, warnings, BlockReason: null);
    }
}
