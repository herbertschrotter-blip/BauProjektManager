using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Legt den Auftragsordner fuer "Mit Claude sortieren" an (Vorbild ha-hausbau
/// logik/claude.py): auftrag.md (Rahmen + Vorschrift), planzuordnung.schema.json
/// und kontext.json (Stammdaten-IDs, bekannte Plaene, Eingangsdateien).
///
/// Nur Bucket C (Erstaufnahme) wird uebergeben — A/B/D bleiben bei der App.
/// Dateien selbst werden NICHT kopiert: Claude sieht Dateiname, Groesse,
/// Dateinamen-Kandidaten und (wenn ein PDF-Text-Port gereicht wird) den
/// Plankopf-Text. Alle Dateizugriffe ueber die FS-Ports (ADR-060).
/// </summary>
public sealed partial class ClaudeSortAuftragBuilder
{
    private readonly ProjectDatabase _bpmDb;
    private readonly PlanManagerDatabase _planDb;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;
    private readonly IPdfTextService? _pdfText;
    private readonly TimeProvider _time;
    private readonly ISegmentTypeCatalog? _segmentTypes;

    /// <param name="pdfText">Optional: ohne Port bleibt plankopf_text leer; gelesen wird nur mit
    /// <see cref="ClaudeSortAuftragAnfrage.PlankopfTextMitsenden"/>.</param>
    public ClaudeSortAuftragBuilder(
        ProjectDatabase bpmDb, PlanManagerDatabase planDb,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path,
        IPdfTextService? pdfText = null, TimeProvider? time = null, ISegmentTypeCatalog? segmentTypes = null)
    {
        _bpmDb = bpmDb;
        _planDb = planDb;
        _reader = reader;
        _writer = writer;
        _path = path;
        _pdfText = pdfText;
        _time = time ?? TimeProvider.System;
        _segmentTypes = segmentTypes;
    }

    [GeneratedRegex(@"^A-(\d{4,6})$")]
    private static partial Regex AuftragsNameRegex();

    /// <summary>Naechster freier Auftragsname ("A-0001", "A-0002", …).</summary>
    public static string NaechsterAuftragsName(IEnumerable<string> vorhandeneNamen)
    {
        var max = vorhandeneNamen
            .Select(n => AuftragsNameRegex().Match(n))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
        return $"A-{max + 1:0000}";
    }

    /// <summary>Absoluter Pfad des naechsten freien Auftragsordners unter <paramref name="claudeRoot"/>.</summary>
    public string NaechsterAuftragsOrdner(string claudeRoot)
    {
        var vorhanden = _reader.DirectoryExists(claudeRoot)
            ? _reader.EnumerateDirectories(claudeRoot).Select(_path.GetFileName)
            : [];
        return _path.Combine(claudeRoot, NaechsterAuftragsName(vorhanden));
    }

    /// <summary>
    /// Legt den Auftrag an. Wirft <see cref="InvalidOperationException"/>, wenn keine
    /// Erstaufnahme im Eingang liegt, der Auftragsordner schon belegt ist, nicht A-#### heisst oder im
    /// Projektordner liegt (Claude haette dort Zugriff auf Projektdateien).
    /// </summary>
    public async Task<ClaudeSortAuftrag> ErstellenAsync(ClaudeSortAuftragAnfrage anfrage, CancellationToken ct = default)
    {
        var eingang = anfrage.Analyse
            .Where(i => i.Bucket == CaptureBucket.NewCapture)
            .OrderBy(i => i.File.Scan.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (eingang.Count == 0)
            throw new InvalidOperationException("Keine neuen Dateien zum Sortieren — Claude bekommt nur Erstaufnahmen.");
        var name = _path.GetFileName(anfrage.AuftragsOrdner);
        if (!AuftragsNameRegex().IsMatch(name))
            throw new InvalidOperationException("Der Auftragsordner muss A-0001, A-0002 … heißen.");
        if (LiegtIn(anfrage.AuftragsOrdner, anfrage.ProjectRootPath))
            throw new InvalidOperationException("Der Auftragsordner darf nicht im Projektordner liegen.");
        if (_reader.DirectoryExists(anfrage.AuftragsOrdner)
            && (_reader.EnumerateFiles(anfrage.AuftragsOrdner).Any() || _reader.EnumerateDirectories(anfrage.AuftragsOrdner).Any()))
            throw new InvalidOperationException("Der Auftragsordner ist bereits belegt — neuen Auftragsnamen wählen.");
        var dateien = new Dictionary<string, CaptureItem>(StringComparer.Ordinal);
        var eingangsdateien = new List<KontextEingangsdatei>(eingang.Count);
        foreach (var item in eingang)
        {
            var id = $"D-{dateien.Count + 1:000}";
            dateien[id] = item;
            eingangsdateien.Add(await BaueEingangsdateiAsync(id, item, anfrage, ct));
        }

        var bekannte = _planDb.GetCurrentDocumentLookup();
        var kontext = BaueKontext(anfrage.ProjectId, name, bekannte, eingangsdateien);

        var regeln = RegelnDatei(anfrage.AuftragsOrdner);
        _writer.CreateDirectory(anfrage.AuftragsOrdner);
        _writer.WriteAllText(regeln, ClaudeSortArten.AuftragText);
        _writer.WriteAllText(_path.Combine(anfrage.AuftragsOrdner, ClaudeSortArten.AuftragDatei), ClaudeSortArten.AuftragText);
        _writer.WriteAllText(_path.Combine(anfrage.AuftragsOrdner, ClaudeSortArten.SchemaDatei), ClaudeSortArten.Schema);
        _writer.WriteAllText(_path.Combine(anfrage.AuftragsOrdner, ClaudeSortArten.KontextDatei),
            JsonSerializer.Serialize(kontext, ClaudeSortJson.Options));

        // Protokoll ohne Inhalt (DSGVO Kap. 11.2): Zahl der Dateien und ob Plankopf-Text mitging
        Log.Information("Claude-Sortierung: Auftrag {Name} angelegt ({Count} Dateien, {Known} bekannte Plaene, " +
            "Plankopf-Text {Plankopf})", name, dateien.Count, bekannte.Count,
            anfrage.PlankopfTextMitsenden ? "mitgesendet" : "nicht mitgesendet");
        return new ClaudeSortAuftrag(name, anfrage.AuftragsOrdner, regeln, anfrage.ProjectId, dateien, bekannte);
    }

    private ClaudeSortKontext BaueKontext(
        string projectId, string name,
        IReadOnlyList<KnownPlanDocument> bekannte, IReadOnlyList<KontextEingangsdatei> eingangsdateien)
    {
        var typen = _bpmDb.GetDocumentTypes(projectId)
            .Select(t => new KontextDokumenttyp(
                t.Id, t.Key, t.Name, Unterteilung(t.Ring2Source), Ablage(t.RootRelativePath, t.FolderName),
                [.. t.Categories.Select(c => new KontextKategorie(c.Id, c.Name))]))
            .ToList();
        var bauteile = _bpmDb.GetBuildingParts(projectId)
            .Select(p => new KontextBauteil(
                p.Id, p.ShortName, p.Description,
                [.. p.Levels.Select(l => new KontextGeschoss(l.Id, l.Name, l.Description))]))
            .ToList();
        var plaene = bekannte
            .Select(d => new KontextBekannterPlan(
                d.PlanNumber, d.CurrentIndex, d.DocumentTypeId, d.DocumentType,
                d.BuildingPartId, d.BuildingLevelId, d.RelativeDirectory))
            .ToList();
        var segmenttypen = ErlaubteSegmenttypen(_segmentTypes)
            .Select(t => new KontextSegmenttyp(t.Id, t.Name))
            .ToList();
        var tags = _planDb.GetAllTags().Take(100).ToList();
        var heute = _time.GetLocalNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new ClaudeSortKontext(name, heute, typen, bauteile, segmenttypen, tags, plaene, eingangsdateien);
    }

    private async Task<KontextEingangsdatei> BaueEingangsdateiAsync(
        string id, CaptureItem item, ClaudeSortAuftragAnfrage anfrage, CancellationToken ct)
    {
        var scan = item.File.Scan;
        var c = item.Candidates;
        var plankopf = anfrage.PlankopfTextMitsenden
            ? await LesePlankopfAsync(scan, anfrage.ProjectRootPath, ct)
            : string.Empty;
        return new KontextEingangsdatei(
            id, scan.FileName, scan.FileSize / 1024,
            new KontextAusDateiname(c.PlanNumber, c.Index, c.Level, c.BuildingPartHint),
            plankopf);
    }

    /// <summary>Plankopf-Text der ersten PDF-Seite; leer ohne Port, ohne Textebene oder bei Lesefehler.</summary>
    private async Task<string> LesePlankopfAsync(ScannedFile scan, string projectRootPath, CancellationToken ct)
    {
        if (_pdfText is null || !scan.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        try
        {
            await using var stream = _reader.OpenRead(_path.Combine(projectRootPath, scan.RelativePath));
            var words = await _pdfText.GetWordsAsync(stream, 0, ct);
            return ClaudeSortPlankopf.AusWoertern(words);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keine Datei-/Pfadnamen im Log (Datenschutz) — nur der Fehlertyp.
            Log.Warning("Claude-Sortierung: Plankopf-Text nicht lesbar ({Error})", ex.GetType().Name);
            return string.Empty;
        }
    }

    /// <summary>
    /// Loescht die Dateien eines Auftrags (kontext.json mit Plankopf-Text, Entwurf, Auftrag, Schema) und die
    /// Regeln-Datei — Datenminimierung nach Ende der Sitzung (ADR-067 E8). Der leere Ordner bleibt (der
    /// FS-Port kennt kein Ordner-Loeschen); er stoert nicht, die Nummern zaehlen weiter. Wirft nicht.
    /// </summary>
    public void Aufraeumen(string auftragsOrdner)
    {
        try
        {
            if (_reader.DirectoryExists(auftragsOrdner))
                foreach (var datei in _reader.EnumerateFiles(auftragsOrdner).ToList())
                    _writer.DeleteFile(datei);
            var regeln = RegelnDatei(auftragsOrdner);
            if (_reader.FileExists(regeln))
                _writer.DeleteFile(regeln);
            Log.Information("Claude-Sortierung: Auftrag {Name} aufgeraeumt", _path.GetFileName(auftragsOrdner));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Claude-Sortierung: Aufraeumen fehlgeschlagen ({Error})", ex.GetType().Name);
        }
    }

    /// <summary>
    /// Raeumt Auftraege unter <paramref name="claudeRoot"/> auf, deren Dateien alle aelter als
    /// <paramref name="alter"/> sind (z. B. nach einem Absturz liegen gebliebene Plankopf-Texte). Wirft nicht.
    /// </summary>
    /// <returns>Auftragsordner ohne Dateien (jetzt oder frueher aufgeraeumt) — deren Sitzungsdaten bei Claude
    /// koennen weg (ADR-067 E8).</returns>
    public IReadOnlyList<string> AlteAufraeumen(string claudeRoot, TimeSpan alter)
    {
        var erledigt = new List<string>();
        try
        {
            if (!_reader.DirectoryExists(claudeRoot))
                return erledigt;
            var grenze = _time.GetUtcNow().UtcDateTime - alter;
            foreach (var ordner in _reader.EnumerateDirectories(claudeRoot)
                         .Where(o => AuftragsNameRegex().IsMatch(_path.GetFileName(o))).ToList())
            {
                var dateien = _reader.EnumerateFiles(ordner).ToList();
                if (dateien.Count > 0 && dateien.All(d => _reader.GetFileInfo(d).LastWriteTimeUtc < grenze))
                    Aufraeumen(ordner);
                if (!_reader.EnumerateFiles(ordner).Any())
                    erledigt.Add(ordner);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Claude-Sortierung: Aufraeumen alter Auftraege fehlgeschlagen ({Error})", ex.GetType().Name);
        }
        return erledigt;
    }

    /// <summary>Regeln-Datei neben dem Auftragsordner: &lt;Basis&gt;/A-0001.regeln.md.</summary>
    public static string RegelnDatei(string auftragsOrdner)
        => Path.TrimEndingDirectorySeparator(auftragsOrdner) + ".regeln.md";

    /// <summary>True, wenn <paramref name="pfad"/> der Ordner <paramref name="wurzel"/> ist oder darin liegt.
    /// Pure Pfad-String-Operation (ADR-060 Praezisierung).</summary>
    internal static bool LiegtIn(string pfad, string wurzel)
    {
        if (string.IsNullOrWhiteSpace(wurzel))
            return false;
        var p = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pfad));
        var w = Path.TrimEndingDirectorySeparator(Path.GetFullPath(wurzel));
        return p.Equals(w, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith(w + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Segmenttypen, die Claude vergeben darf: alle aktiven ausser "ignorieren" und dem Dokumenttyp-Feld.</summary>
    internal static IEnumerable<SegmentTypeDefinition> ErlaubteSegmenttypen(ISegmentTypeCatalog? katalog) =>
        (katalog?.GetEffectiveActive() ?? [])
            .Where(t => t.Id != SegmentTypeIds.Ignore && t.Id != SegmentTypeIds.DocumentTypeField);

    private static string Unterteilung(Ring2Source source) => source switch
    {
        Ring2Source.BuildingParts => "bauteile",
        Ring2Source.Categories => "kategorien",
        _ => "keine"
    };

    private string Ablage(string root, string folderName)
        => string.IsNullOrEmpty(folderName) ? root : _path.Combine(root, folderName);
}
