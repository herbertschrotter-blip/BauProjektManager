using System.Text.Encodings.Web;
using System.Text.Json;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

// kontext.json fuer "Mit Claude sortieren" (Vorschrift planzuordnung.md, Tabelle "Kontext").
// Feldnamen per SnakeCaseLower (BekanntePlaene -> bekannte_plaene). Nur IDs, Namen und
// projekt-RELATIVE Pfade — keine absoluten Pfade, keine Benutzernamen.

internal sealed record ClaudeSortKontext(
    string Auftrag,
    string Heute,
    IReadOnlyList<KontextDokumenttyp> Dokumenttypen,
    IReadOnlyList<KontextBauteil> Bauteile,
    IReadOnlyList<KontextBekannterPlan> BekanntePlaene,
    IReadOnlyList<KontextEingangsdatei> Eingangsdateien);

internal sealed record KontextDokumenttyp(
    string Id,
    string Key,
    string Name,
    string Unterteilung,
    string Ablage,
    IReadOnlyList<KontextKategorie> Kategorien);

internal sealed record KontextKategorie(string Id, string Name);

internal sealed record KontextBauteil(
    string Id,
    string Kurzname,
    string Beschreibung,
    IReadOnlyList<KontextGeschoss> Geschosse);

internal sealed record KontextGeschoss(string Id, string Name, string Beschreibung);

internal sealed record KontextBekannterPlan(
    string Plannummer,
    string? Index,
    string? DokumenttypId,
    string Dokumenttyp,
    string? BauteilId,
    string? GeschossId,
    string Ordner);

internal sealed record KontextEingangsdatei(
    string Id,
    string Dateiname,
    long GroesseKb,
    KontextAusDateiname AusDateiname,
    string PlankopfText);

internal sealed record KontextAusDateiname(
    string? Plannummer,
    string? Index,
    string? Geschoss,
    string? Bauteil);

internal static class ClaudeSortJson
{
    /// <summary>Lesbares JSON fuer Claude: eingerueckt, Umlaute unescaped, nulls bleiben sichtbar.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
