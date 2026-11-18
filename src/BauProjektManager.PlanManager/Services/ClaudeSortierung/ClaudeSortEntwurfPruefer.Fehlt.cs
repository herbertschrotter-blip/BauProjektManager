using System.Text.Json;
using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Pruefung von nicht_zuordenbar[].fehlt (ADR-067 E11): Claude schlaegt vor, was im Projekt fehlt, statt eine ID zu
/// erfinden. Gibt es das Vorgeschlagene schon, ist das ein Fehler — dann soll Claude die vorhandene ID nehmen.
/// </summary>
public sealed partial class ClaudeSortEntwurfPruefer
{
    internal const int MaxFehltName = 40;
    internal const int MaxGeschosse = 20;

    private static readonly string[] FehltFelder = ["art", "name", "beschreibung", "bauteil_id", "geschosse", "unterteilung"];

    private static ClaudeSortFehlt? PruefeFehlt(Lauf lauf, string ort, JsonElement zeile)
    {
        if (!zeile.TryGetProperty("fehlt", out var el) || el.ValueKind == JsonValueKind.Null)
            return null;
        var o = ort + " fehlt";
        if (el.ValueKind != JsonValueKind.Object)
        {
            lauf.Fehler.Add($"{o}: muss ein Objekt sein.");
            return null;
        }
        var fehlerVorher = lauf.Fehler.Count;
        var f = new ClaudeSortFeldLeser(lauf.Fehler, o, el, FehltFelder);
        var art = f.Text("art", pflicht: true, maxLaenge: 20);
        var name = f.Text("name", pflicht: true, maxLaenge: MaxFehltName);
        var beschreibung = f.Text("beschreibung", pflicht: false, maxLaenge: 100);
        var bauteilId = f.Text("bauteil_id", pflicht: false, maxLaenge: 40);
        var unterteilung = f.Text("unterteilung", pflicht: false, maxLaenge: 20);
        var geschosse = Geschosse(lauf, o, el);
        // Ein Vorschlag mit Fehlern wird keine Karte (Claude bekommt die Fehler zurueck)
        if (art is null || string.IsNullOrEmpty(name) || lauf.Fehler.Count > fehlerVorher)
            return null;

        switch (art)
        {
            case "bauteil":
                if (lauf.Parts.FirstOrDefault(p => Gleich(p.ShortName, name)) is { } part)
                    return Gibt(lauf, o, $"Bauteil '{name}' gibt es schon (bauteil_id {part.Id})");
                return new ClaudeSortFehlt(ClaudeSortFehltArt.Bauteil, name, beschreibung, null, geschosse, Ring2Source.None);
            case "geschoss":
                var zu = lauf.Parts.FirstOrDefault(p => p.Id == bauteilId);
                if (zu is null)
                {
                    lauf.Fehler.Add($"{o}: Geschoss braucht bauteil_id eines vorhandenen Bauteils aus kontext.bauteile.");
                    return null;
                }
                if (zu.Levels.FirstOrDefault(l => Gleich(l.Name, name)) is { } level)
                    return Gibt(lauf, o, $"Geschoss '{name}' gibt es bei {zu.ShortName} schon (geschoss_id {level.Id})");
                return new ClaudeSortFehlt(ClaudeSortFehltArt.Geschoss, name, beschreibung, zu.Id, [], Ring2Source.None);
            case "dokumenttyp":
                if (lauf.Types.FirstOrDefault(t => Gleich(t.Name, name)) is { } type)
                    return Gibt(lauf, o, $"Dokumenttyp '{name}' gibt es schon (dokumenttyp_id {type.Id})");
                Ring2Source? quelle = unterteilung switch
                {
                    null or "keine" => Ring2Source.None,
                    "bauteile" => Ring2Source.BuildingParts,
                    "kategorien" => Ring2Source.Categories,
                    _ => null
                };
                if (quelle is null)
                {
                    lauf.Fehler.Add($"{o}: unterteilung '{unterteilung}' — erlaubt: keine, bauteile, kategorien.");
                    return null;
                }
                return new ClaudeSortFehlt(ClaudeSortFehltArt.Dokumenttyp, name, beschreibung, null, [], quelle.Value);
            default:
                lauf.Fehler.Add($"{o}: art '{art}' — erlaubt: bauteil, geschoss, dokumenttyp (Kategorien legt Herbert selbst an).");
                return null;
        }
    }

    private static IReadOnlyList<string> Geschosse(Lauf lauf, string ort, JsonElement el)
    {
        if (!el.TryGetProperty("geschosse", out var liste) || liste.ValueKind == JsonValueKind.Null)
            return [];
        if (liste.ValueKind != JsonValueKind.Array || liste.GetArrayLength() > MaxGeschosse)
        {
            lauf.Fehler.Add($"{ort}: 'geschosse' muss eine Liste mit höchstens {MaxGeschosse} Namen sein.");
            return [];
        }
        var namen = new List<string>();
        foreach (var g in liste.EnumerateArray())
        {
            var text = g.ValueKind == JsonValueKind.String ? (g.GetString() ?? string.Empty).Trim() : null;
            if (string.IsNullOrEmpty(text) || text.Length > MaxFehltName)
                lauf.Fehler.Add($"{ort}: Geschoss '{g}' muss Text mit 1–{MaxFehltName} Zeichen sein.");
            else if (!namen.Contains(text, StringComparer.OrdinalIgnoreCase))
                namen.Add(text);
        }
        return namen;
    }

    private static ClaudeSortFehlt? Gibt(Lauf lauf, string ort, string text)
    {
        lauf.Fehler.Add($"{ort}: {text} — nimm die vorhandene ID und ordne die Datei zu.");
        return null;
    }

    private static bool Gleich(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
}
