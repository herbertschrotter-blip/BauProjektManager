using System.Text.Json;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Prueft das entwurf.json von Claude wie eine Eingabe von Hand (Vorbild ha-hausbau
/// logik/entwurf.py): Schema-Felder (planzuordnung.schema.json, von Hand geprueft —
/// keine Schema-Bibliothek), jede ID existiert in den Stammdaten, der
/// DocumentTargetPathResolver loest den Ablageordner auf (ADR-061, Fail-Fast-Meldung
/// wird uebernommen), jede datei_id gehoert zum Auftrag und ist noch Bucket C.
/// Liefert gueltige Zeilen + deutsche Fehler; das Runden-Limit regelt der Aufrufer.
/// </summary>
public sealed partial class ClaudeSortEntwurfPruefer
{
    private static readonly HashSet<string> WurzelFelder = ["zuordnungen", "nicht_zuordenbar", "hinweise"];

    /// <summary>Groesstes entwurf.json, das gelesen wird (500 Zeilen brauchen weit weniger).</summary>
    internal const long MaxEntwurfBytes = 2 * 1024 * 1024;

    private readonly ProjectDatabase _bpmDb;
    private readonly DocumentTargetPathResolver _resolver;
    private readonly IFileSystemReader _reader;
    private readonly IPathService _path;
    private readonly IPlanValueNormalizer _normalizer;
    private readonly ISegmentTypeCatalog? _segmentTypes;

    public ClaudeSortEntwurfPruefer(
        ProjectDatabase bpmDb, DocumentTargetPathResolver resolver,
        IFileSystemReader reader, IPathService path, IPlanValueNormalizer normalizer,
        ISegmentTypeCatalog? segmentTypes = null)
    {
        _bpmDb = bpmDb;
        _resolver = resolver;
        _reader = reader;
        _path = path;
        _normalizer = normalizer;
        _segmentTypes = segmentTypes;
    }

    /// <summary>True, wenn Claude im Auftragsordner ein entwurf.json geschrieben hat.</summary>
    public bool EntwurfVorhanden(ClaudeSortAuftrag auftrag)
        => _reader.FileExists(_path.Combine(auftrag.Ordner, ClaudeSortArten.EntwurfDatei));

    /// <summary>Liest entwurf.json aus dem Auftragsordner und prueft ihn.</summary>
    public ClaudeSortPruefErgebnis PruefeOrdner(ClaudeSortAuftrag auftrag, IReadOnlyList<CaptureItem> aktuelleAnalyse)
    {
        var datei = _path.Combine(auftrag.Ordner, ClaudeSortArten.EntwurfDatei);
        if (!_reader.FileExists(datei))
            return ClaudeSortPruefErgebnis.NurFehler("entwurf.json fehlt im Auftragsordner.");
        if (_reader.GetFileInfo(datei).Length > MaxEntwurfBytes)
            return ClaudeSortPruefErgebnis.NurFehler("entwurf.json ist zu groß (höchstens 2 MB).");
        return Pruefe(auftrag, _reader.ReadAllText(datei), aktuelleAnalyse);
    }

    /// <summary>Prueft den Entwurf gegen Auftrag, Stammdaten und die aktuelle Eingangs-Analyse.</summary>
    public ClaudeSortPruefErgebnis Pruefe(
        ClaudeSortAuftrag auftrag, string entwurfJson, IReadOnlyList<CaptureItem> aktuelleAnalyse)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(entwurfJson);
        }
        catch (JsonException ex)
        {
            return ClaudeSortPruefErgebnis.NurFehler($"entwurf.json ist kein gültiges JSON: {ex.Message}");
        }

        using (doc)
        {
            var lauf = new Lauf(auftrag, aktuelleAnalyse,
                _bpmDb.GetDocumentTypes(auftrag.ProjectId), _bpmDb.GetBuildingParts(auftrag.ProjectId),
                ClaudeSortAuftragBuilder.ErlaubteSegmenttypen(_segmentTypes).ToDictionary(t => t.Id, StringComparer.Ordinal));
            PruefeWurzel(lauf, doc.RootElement);
            var ergebnis = new ClaudeSortPruefErgebnis(lauf.Zeilen, lauf.NichtZuordenbar, lauf.Fehler, lauf.Hinweise);
            Log.Information("Claude-Sortierung: Entwurf {Name} geprueft — {Ok} gueltig, {Nz} nicht zuordenbar, {Err} Fehler",
                auftrag.Name, lauf.Zeilen.Count, lauf.NichtZuordenbar.Count, lauf.Fehler.Count);
            return ergebnis;
        }
    }

    private void PruefeWurzel(Lauf lauf, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            lauf.Fehler.Add("entwurf.json muss ein Objekt sein.");
            return;
        }
        foreach (var p in root.EnumerateObject().Where(p => !WurzelFelder.Contains(p.Name)))
            lauf.Fehler.Add($"Unbekanntes Feld '{p.Name}' — erlaubt sind: zuordnungen, nicht_zuordenbar, hinweise.");

        if (Liste(lauf, root, "zuordnungen", pflicht: true) is { } zuordnungen)
        {
            var nr = 0;
            foreach (var z in zuordnungen)
                PruefeZuordnung(lauf, z, ++nr);
        }
        if (Liste(lauf, root, "nicht_zuordenbar", pflicht: false) is { } nichtZuordenbar)
        {
            var nr = 0;
            foreach (var n in nichtZuordenbar)
                PruefeNichtZuordenbar(lauf, n, ++nr);
        }
        ClaudeSortEntwurfKonsistenz.Pruefe(lauf.Zeilen, lauf.Auftrag, lauf.Fehler, _normalizer);
        if (Liste(lauf, root, "hinweise", pflicht: false) is { } hinweise)
            lauf.Hinweise.AddRange(hinweise
                .Where(h => h.ValueKind == JsonValueKind.String)
                .Select(h => (h.GetString() ?? string.Empty).Trim())
                .Where(h => h.Length > 0));

        PruefeVollstaendig(lauf);
    }

    private static IEnumerable<JsonElement>? Liste(Lauf lauf, JsonElement root, string feld, bool pflicht)
    {
        if (!root.TryGetProperty(feld, out var el))
        {
            if (pflicht)
                lauf.Fehler.Add($"Feld '{feld}' fehlt.");
            return null;
        }
        if (el.ValueKind != JsonValueKind.Array)
        {
            lauf.Fehler.Add($"'{feld}' muss eine Liste sein.");
            return null;
        }
        return el.EnumerateArray();
    }

    private static void PruefeNichtZuordenbar(Lauf lauf, JsonElement el, int nr)
    {
        var ort = $"nicht_zuordenbar[{nr}]";
        if (el.ValueKind != JsonValueKind.Object)
        {
            lauf.Fehler.Add($"{ort}: muss ein Objekt sein.");
            return;
        }
        var feld = new ClaudeSortFeldLeser(lauf.Fehler, ort, el, ["datei_id", "grund", "fehlt"]);
        var dateiId = feld.Text("datei_id", pflicht: true, maxLaenge: 20);
        var grund = feld.Text("grund", pflicht: true, maxLaenge: 300) ?? string.Empty;
        var fehlt = PruefeFehlt(lauf, ort, el);
        if (dateiId is null || PruefeDatei(lauf, dateiId, ort) is not { } item)
            return;
        lauf.NichtZuordenbar.Add(new ClaudeSortNichtZuordenbar(dateiId, item, grund, fehlt));
    }

    /// <summary>
    /// datei_id gehoert zum Auftrag, kommt nur einmal vor und ist in der aktuellen
    /// Analyse noch Bucket C mit gleichem Inhalt (MD5). Liefert das aktuelle Item.
    /// </summary>
    private static CaptureItem? PruefeDatei(Lauf lauf, string dateiId, string ort)
    {
        if (!lauf.Auftrag.Dateien.TryGetValue(dateiId, out var auftragsItem))
        {
            lauf.Fehler.Add($"{ort}: datei_id '{dateiId}' gehört nicht zu diesem Auftrag — nur IDs aus eingangsdateien.");
            return null;
        }
        if (!lauf.Gesehen.Add(dateiId))
        {
            lauf.Fehler.Add($"{ort}: {dateiId} kommt mehrfach im Entwurf vor — jede Datei genau einmal.");
            return null;
        }
        var aktuell = lauf.Aktuell.GetValueOrDefault(auftragsItem.File.Scan.RelativePath);
        if (aktuell is null
            || aktuell.Bucket != CaptureBucket.NewCapture
            || !string.Equals(aktuell.File.Md5, auftragsItem.File.Md5, StringComparison.OrdinalIgnoreCase))
        {
            lauf.Fehler.Add($"{ort}: {dateiId} ({auftragsItem.File.Scan.FileName}) ist nicht mehr als neue Datei im " +
                            "Eingang (entfernt, geändert oder schon zugeordnet) — diese Zeile weglassen.");
            return null;
        }
        return aktuell;
    }

    private static void PruefeVollstaendig(Lauf lauf)
    {
        foreach (var (dateiId, item) in lauf.Auftrag.Dateien)
        {
            if (lauf.Gesehen.Contains(dateiId))
                continue;
            var aktuell = lauf.Aktuell.GetValueOrDefault(item.File.Scan.RelativePath);
            if (aktuell?.Bucket == CaptureBucket.NewCapture)
                lauf.Fehler.Add($"{dateiId} ({item.File.Scan.FileName}) fehlt im Entwurf — in zuordnungen oder " +
                                "nicht_zuordenbar aufnehmen.");
        }
    }

    /// <summary>Zustand eines Pruefdurchlaufs.</summary>
    private sealed class Lauf(
        ClaudeSortAuftrag auftrag, IReadOnlyList<CaptureItem> aktuelleAnalyse,
        IReadOnlyList<PlanDocumentType> types, IReadOnlyList<BuildingPart> parts,
        IReadOnlyDictionary<string, SegmentTypeDefinition> segmenttypen)
    {
        public IReadOnlyDictionary<string, SegmentTypeDefinition> Segmenttypen { get; } = segmenttypen;
        public ClaudeSortAuftrag Auftrag { get; } = auftrag;
        public IReadOnlyList<PlanDocumentType> Types { get; } = types;
        public IReadOnlyList<BuildingPart> Parts { get; } = parts;

        public Dictionary<string, CaptureItem> Aktuell { get; } = aktuelleAnalyse
            .GroupBy(i => i.File.Scan.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Gesehen { get; } = new(StringComparer.Ordinal);

        /// <summary>Resolver-Ergebnis je (Typ, Ring 2, Geschoss) — vermeidet je Zeile neue DB-Abfragen.</summary>
        public Dictionary<(string, string?, string?), (string? Ordner, string? Fehler)> Ziele { get; } = [];
        public List<ClaudeSortZeile> Zeilen { get; } = [];
        public List<ClaudeSortNichtZuordenbar> NichtZuordenbar { get; } = [];
        public List<string> Fehler { get; } = [];
        public List<string> Hinweise { get; } = [];
    }
}
