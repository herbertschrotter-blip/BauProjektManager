using System.Globalization;
using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Pruefung von Index-Datum, Segmenten und Tags einer Entwurfszeile (ADR-067). Nichts davon darf Claude
/// erfinden: Segmenttypen nur aus kontext.segmenttypen, Segmentwerte nur woertlich aus dem Dateinamen,
/// Datum gueltig und nicht in der Zukunft.
/// </summary>
public sealed partial class ClaudeSortEntwurfPruefer
{
    internal const int MaxTags = 5;
    internal const int MaxTagLaenge = 40;

    /// <summary>"JJJJ-MM-TT" → ISO-UTC wie die Text-Zuweisung (ManualCaptureView.TryParseDate); NULL = keins.</summary>
    private static string? PruefeDatum(Lauf lauf, string ort, string? text)
    {
        if (text is null)
            return null;
        if (!DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var datum))
        {
            lauf.Fehler.Add($"{ort}: index_datum '{text}' ist kein Datum im Format JJJJ-MM-TT.");
            return null;
        }
        if (datum.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            lauf.Fehler.Add($"{ort}: index_datum {text} liegt in der Zukunft — nur das Datum aus Dateiname oder Plankopf.");
            return null;
        }
        return datum.ToString("o", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// segmente: je Eintrag segmenttyp_id (aus kontext.segmenttypen) und wert, der woertlich im Dateinamen steht.
    /// Je Segmenttyp hoechstens ein Wert (plan_document_segments ist eindeutig je Dokument und Typ).
    /// </summary>
    private static IReadOnlyList<AssignedSegmentValue> PruefeSegmente(Lauf lauf, string ort, JsonElement zeile, CaptureItem item)
    {
        if (!zeile.TryGetProperty("segmente", out var liste) || liste.ValueKind == JsonValueKind.Null)
            return [];
        if (liste.ValueKind != JsonValueKind.Array)
        {
            lauf.Fehler.Add($"{ort}: 'segmente' muss eine Liste sein.");
            return [];
        }
        var stamm = Path.GetFileNameWithoutExtension(item.File.Scan.FileName);
        var ergebnis = new List<AssignedSegmentValue>();
        var nr = 0;
        foreach (var el in liste.EnumerateArray())
        {
            var o = $"{ort} segmente[{++nr}]";
            if (el.ValueKind != JsonValueKind.Object)
            {
                lauf.Fehler.Add($"{o}: muss ein Objekt sein.");
                continue;
            }
            var f = new ClaudeSortFeldLeser(lauf.Fehler, o, el, ["segmenttyp_id", "wert"]);
            var typId = f.Text("segmenttyp_id", pflicht: true, maxLaenge: 100);
            var wert = f.Text("wert", pflicht: true, maxLaenge: 100);
            if (typId is null || string.IsNullOrEmpty(wert))
                continue;
            if (!lauf.Segmenttypen.TryGetValue(typId, out var typ))
            {
                lauf.Fehler.Add($"{o}: segmenttyp_id '{typId}' gibt es nicht — nur IDs aus kontext.segmenttypen.");
                continue;
            }
            if (!stamm.Contains(wert, StringComparison.OrdinalIgnoreCase))
            {
                lauf.Fehler.Add($"{o}: '{wert}' steht nicht im Dateinamen — Segmente nur mit Werten aus dem Dateinamen.");
                continue;
            }
            if (ergebnis.Any(s => s.SegmentTypeId == typ.Id))
            {
                lauf.Fehler.Add($"{o}: Segmenttyp '{typ.Name}' kommt mehrfach vor — je Typ ein Wert.");
                continue;
            }
            ergebnis.Add(new AssignedSegmentValue(typ.Id, string.IsNullOrEmpty(typ.TokenKey) ? typ.Id : typ.TokenKey, wert));
        }
        return ergebnis;
    }

    /// <summary>tags: hoechstens <see cref="MaxTags"/> kurze Schlagworte, doppelte (gross/klein) werden zusammengefasst.</summary>
    private static IReadOnlyList<string> PruefeTags(Lauf lauf, string ort, JsonElement zeile)
    {
        if (!zeile.TryGetProperty("tags", out var liste) || liste.ValueKind == JsonValueKind.Null)
            return [];
        if (liste.ValueKind != JsonValueKind.Array)
        {
            lauf.Fehler.Add($"{ort}: 'tags' muss eine Liste sein.");
            return [];
        }
        var tags = new List<string>();
        foreach (var el in liste.EnumerateArray())
        {
            var tag = el.ValueKind == JsonValueKind.String ? (el.GetString() ?? string.Empty).Trim() : null;
            if (string.IsNullOrEmpty(tag) || tag.Length > MaxTagLaenge)
            {
                lauf.Fehler.Add($"{ort}: Tag '{el}' muss Text mit 1–{MaxTagLaenge} Zeichen sein.");
                continue;
            }
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                tags.Add(tag);
        }
        if (tags.Count > MaxTags)
        {
            lauf.Fehler.Add($"{ort}: höchstens {MaxTags} Tags je Datei.");
            return [];
        }
        return tags;
    }

    /// <summary>Anzeige im Detail-Panel: "Index-Datum 03.04.2026 · Haus: H2, Planart: Polierplan · Tags: Scan".</summary>
    private static string? Details(Lauf lauf, string? indexDatum, IReadOnlyList<AssignedSegmentValue> segmente,
        IReadOnlyList<string> tags)
    {
        var teile = new List<string>();
        if (indexDatum is not null && DateTime.TryParse(indexDatum, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var d))
            teile.Add($"Index-Datum {d:dd.MM.yyyy}");
        if (segmente.Count > 0)
            teile.Add(string.Join(", ", segmente.Select(s =>
                $"{(lauf.Segmenttypen.TryGetValue(s.SegmentTypeId, out var t) ? t.Name : s.SegmentTypeId)}: {s.Value}")));
        if (tags.Count > 0)
            teile.Add("Tags: " + string.Join(", ", tags));
        return teile.Count == 0 ? null : string.Join(" · ", teile);
    }
}
