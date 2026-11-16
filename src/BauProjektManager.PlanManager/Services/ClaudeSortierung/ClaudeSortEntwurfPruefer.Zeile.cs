using System.Text.Json;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>Pruefung einer einzelnen Entwurfszeile (zuordnungen[n]).</summary>
public sealed partial class ClaudeSortEntwurfPruefer
{
    private static readonly string[] ZeilenFelder =
    [
        "datei_id", "dokumenttyp_id", "bauteil_id", "kategorie_id", "geschoss_id",
        "plannummer", "index", "bezeichnung", "unsicher", "hinweise", "begruendung"
    ];

    private static readonly HashSet<string> UnsicherFelder =
        ["dokumenttyp_id", "bauteil_id", "kategorie_id", "geschoss_id", "plannummer", "index", "bezeichnung"];

    /// <summary>Aufgeloeste Stammdaten einer Zeile.</summary>
    private sealed record Stamm(
        PlanDocumentType Type, BuildingPart? Part, BuildingLevel? Level, PlanDocumentTypeCategory? Category);

    private void PruefeZuordnung(Lauf lauf, JsonElement el, int nr)
    {
        if (el.ValueKind != JsonValueKind.Object)
        {
            lauf.Fehler.Add($"zuordnungen[{nr}]: muss ein Objekt sein.");
            return;
        }
        var fehlerVorher = lauf.Fehler.Count;
        var ort = el.TryGetProperty("datei_id", out var d) && d.ValueKind == JsonValueKind.String
            ? $"zuordnungen[{nr}] ({d.GetString()})"
            : $"zuordnungen[{nr}]";
        var f = new ClaudeSortFeldLeser(lauf.Fehler, ort, el, ZeilenFelder);

        var dateiId = f.Text("datei_id", pflicht: true, maxLaenge: 20);
        var typId = f.Text("dokumenttyp_id", pflicht: true, maxLaenge: 100);
        var bauteilId = f.Text("bauteil_id", pflicht: false, maxLaenge: 100);
        var kategorieId = f.Text("kategorie_id", pflicht: false, maxLaenge: 100);
        var geschossId = f.Text("geschoss_id", pflicht: false, maxLaenge: 100);
        var plannummer = f.Text("plannummer", pflicht: false, maxLaenge: 100);
        var index = f.Text("index", pflicht: false, maxLaenge: 20);
        var bezeichnung = f.Text("bezeichnung", pflicht: false, maxLaenge: 200);
        var unsicher = f.Werte("unsicher", UnsicherFelder);
        var hinweise = f.Text("hinweise", pflicht: false, maxLaenge: 300);
        var begruendung = f.Text("begruendung", pflicht: true, maxLaenge: 300) ?? string.Empty;

        var item = dateiId is null ? null : PruefeDatei(lauf, dateiId, ort);
        var stamm = typId is null ? null : PruefeStammdaten(lauf, ort, typId, bauteilId, kategorieId, geschossId);
        if (dateiId is null || item is null || stamm is null || lauf.Fehler.Count > fehlerVorher)
            return;
        if (LoeseZiel(lauf, ort, stamm) is not { } zielOrdner)
            return;

        var zeile = new ClaudeSortZeile(
            dateiId, item, stamm.Type, stamm.Part, stamm.Level, stamm.Category,
            plannummer, index, bezeichnung, unsicher, hinweise, begruendung, zielOrdner);
        if (PruefeBekannterPlan(lauf, ort, zeile))
            lauf.Zeilen.Add(zeile);
    }

    private static Stamm? PruefeStammdaten(
        Lauf lauf, string ort, string typId, string? bauteilId, string? kategorieId, string? geschossId)
    {
        var type = lauf.Types.FirstOrDefault(t => t.Id == typId);
        if (type is null)
        {
            lauf.Fehler.Add($"{ort}: dokumenttyp_id '{typId}' gibt es nicht — nur IDs aus kontext.dokumenttypen.");
            return null;
        }
        switch (type.Ring2Source)
        {
            case Ring2Source.BuildingParts:
                return PruefeBauteil(lauf, ort, type, bauteilId, kategorieId, geschossId);
            case Ring2Source.Categories:
                return PruefeKategorie(lauf, ort, type, bauteilId, kategorieId, geschossId);
            default:
                if (bauteilId is not null || kategorieId is not null || geschossId is not null)
                {
                    lauf.Fehler.Add($"{ort}: Typ '{type.Name}' hat keine Unterteilung — bauteil_id, kategorie_id " +
                                    "und geschoss_id müssen null sein.");
                    return null;
                }
                return new Stamm(type, null, null, null);
        }
    }

    private static Stamm? PruefeBauteil(
        Lauf lauf, string ort, PlanDocumentType type, string? bauteilId, string? kategorieId, string? geschossId)
    {
        if (kategorieId is not null)
            lauf.Fehler.Add($"{ort}: Typ '{type.Name}' wird nach Bauteilen unterteilt — kategorie_id muss null sein.");
        if (bauteilId is null)
        {
            lauf.Fehler.Add($"{ort}: bauteil_id fehlt — Typ '{type.Name}' braucht ein Bauteil aus kontext.bauteile.");
            return null;
        }
        var part = lauf.Parts.FirstOrDefault(p => p.Id == bauteilId);
        if (part is null)
        {
            lauf.Fehler.Add($"{ort}: bauteil_id '{bauteilId}' gibt es nicht — nur IDs aus kontext.bauteile.");
            return null;
        }
        if (geschossId is null)
            return new Stamm(type, part, null, null);

        var level = part.Levels.FirstOrDefault(l => l.Id == geschossId);
        if (level is null)
        {
            lauf.Fehler.Add($"{ort}: geschoss_id '{geschossId}' gehört nicht zum Bauteil '{part.ShortName}' — " +
                            "nur Geschosse dieses Bauteils.");
            return null;
        }
        return new Stamm(type, part, level, null);
    }

    private static Stamm? PruefeKategorie(
        Lauf lauf, string ort, PlanDocumentType type, string? bauteilId, string? kategorieId, string? geschossId)
    {
        if (bauteilId is not null || geschossId is not null)
            lauf.Fehler.Add($"{ort}: Typ '{type.Name}' wird nach Kategorien unterteilt — bauteil_id und " +
                            "geschoss_id müssen null sein.");
        if (kategorieId is null)
        {
            lauf.Fehler.Add($"{ort}: kategorie_id fehlt — Typ '{type.Name}' braucht eine seiner Kategorien.");
            return null;
        }
        var category = type.Categories.FirstOrDefault(c => c.Id == kategorieId);
        if (category is null)
        {
            lauf.Fehler.Add($"{ort}: kategorie_id '{kategorieId}' gehört nicht zum Typ '{type.Name}' — nur " +
                            "Kategorien dieses Typs.");
            return null;
        }
        return new Stamm(type, null, null, category);
    }

    /// <summary>Ablageordner ueber den Resolver (ADR-061) — Fail-Fast-Meldung geht an Claude.</summary>
    private string? LoeseZiel(Lauf lauf, string ort, Stamm stamm)
    {
        var ring2 = stamm.Part?.Id ?? stamm.Category?.Id;
        var schluessel = (stamm.Type.Id, ring2, stamm.Level?.Id);
        if (!lauf.Ziele.TryGetValue(schluessel, out var ziel))
        {
            try
            {
                ziel = (_resolver.Resolve(lauf.Auftrag.ProjectId,
                    new DocumentTargetRequest(stamm.Type.Id, ring2, stamm.Level?.Id, FileName: "_")).RelativeDirectory, null);
            }
            catch (DocumentTargetResolutionException ex)
            {
                ziel = (null, ex.Message);
            }
            lauf.Ziele[schluessel] = ziel;
        }
        if (ziel.Fehler is not null)
            lauf.Fehler.Add($"{ort}: Ablageziel nicht auflösbar: {ziel.Fehler}");
        return ziel.Ordner;
    }

    /// <summary>
    /// Gleicher document_key wie ein Archivdokument = neuer Index eines bekannten Plans (Bucket-B-Fall).
    /// Als Erstaufnahme haengte der Import die Datei still als Zusatzdatei an die alte Revision — darum
    /// Fehler an Claude. Gleiche Nummer bei anderem Typ/Bauteil/Geschoss ist nur ein Hinweis.
    /// </summary>
    private bool PruefeBekannterPlan(Lauf lauf, string ort, ClaudeSortZeile zeile)
    {
        var key = ClaudeSortEntwurfKonsistenz.Schluessel(zeile, _normalizer);
        if (lauf.Auftrag.BekanntePlaene.FirstOrDefault(k => k.DocumentKey == key) is { } gleich)
        {
            lauf.Fehler.Add($"{ort}: {zeile.PlanNumber ?? zeile.Item.File.Scan.FileName} ist schon im Archiv (Index " +
                            $"{gleich.CurrentIndex ?? "–"}, gleicher Typ, Bauteil und Geschoss) — das ist ein Update, " +
                            "keine Erstaufnahme. In nicht_zuordenbar mit Grund „schon im Archiv – Update von Hand“.");
            return false;
        }
        if (zeile.PlanNumber is { } nummer)
        {
            var norm = _normalizer.NormalizeForMatch(nummer);
            if (lauf.Auftrag.BekanntePlaene.Any(k =>
                    !string.IsNullOrEmpty(k.PlanNumber) && _normalizer.NormalizeForMatch(k.PlanNumber) == norm))
                lauf.Hinweise.Add($"{zeile.DateiId}: Plannummer {nummer} gibt es im Archiv bei anderem Typ, Bauteil " +
                                  "oder Geschoss — wird als eigenes Dokument angelegt.");
        }
        return true;
    }
}
