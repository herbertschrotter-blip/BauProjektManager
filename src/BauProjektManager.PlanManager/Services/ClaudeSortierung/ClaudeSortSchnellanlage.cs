using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>Was im Projekt fehlt (entwurf.json, nicht_zuordenbar[].fehlt — ADR-067 E11).</summary>
public enum ClaudeSortFehltArt
{
    Bauteil,
    Geschoss,
    Dokumenttyp
}

/// <summary>
/// Vorschlag von Claude, etwas anzulegen, das es im Projekt noch nicht gibt. Claude legt nie selbst an;
/// Herbert entscheidet in der Karte im Chat (Mockup 3.4 v1.3).
/// </summary>
/// <param name="Name">Bauteil: Kuerzel; Geschoss: Bezeichnung; Dokumenttyp: Name.</param>
/// <param name="BauteilId">Nur bei Geschoss: das (vorhandene) Bauteil.</param>
/// <param name="Geschosse">Nur bei Bauteil: Geschosse von unten nach oben.</param>
/// <param name="Unterteilung">Nur bei Dokumenttyp.</param>
public sealed record ClaudeSortFehlt(
    ClaudeSortFehltArt Art,
    string Name,
    string? Beschreibung,
    string? BauteilId,
    IReadOnlyList<string> Geschosse,
    Ring2Source Unterteilung)
{
    /// <summary>Gleicher Vorschlag fuer mehrere Dateien = eine Karte.</summary>
    public string Schluessel => $"{Art}|{BauteilId}|{Name.ToUpperInvariant()}";
}

/// <param name="Ok">False = nichts angelegt, <see cref="Anzeige"/> nennt den Grund.</param>
/// <param name="Anzeige">Text fuer den Chat (ohne IDs).</param>
/// <param name="FuerClaude">Meldung an Claude mit den neuen IDs.</param>
public sealed record ClaudeSortAnlageErgebnis(bool Ok, string Anzeige, string FuerClaude);

/// <summary>
/// Schnellanlage aus dem Chat (ADR-067 E11): dieselben Stammdaten-Wege wie „+ Neu…" im Radial. Der Ordner
/// entsteht erst beim journalisierten Import (ADR-059-Addendum Ordneranlage).
/// </summary>
public sealed class ClaudeSortSchnellanlage
{
    /// <summary>Ablagebereich fuer neue Dokumenttypen, wenn das Projekt noch keinen hat.</summary>
    public const string StandardAblage = "01 Planunterlagen";

    private readonly ProjectDatabase _db;
    private readonly DocumentTypeCreationService _creation;

    public ClaudeSortSchnellanlage(ProjectDatabase db, DocumentTypeCreationService creation)
    {
        _db = db;
        _creation = creation;
    }

    /// <summary>Legt an, was Herbert in der Karte bestaetigt hat. Wirft nicht bei Eingabefehlern.</summary>
    public ClaudeSortAnlageErgebnis Anlegen(string projectId, ClaudeSortFehlt anlage)
    {
        var name = anlage.Name.Trim();
        if (name.Length == 0)
            return Fehler("Der Name fehlt.");
        var beschreibung = string.IsNullOrWhiteSpace(anlage.Beschreibung) ? null : anlage.Beschreibung.Trim();
        return anlage.Art switch
        {
            ClaudeSortFehltArt.Bauteil => Bauteil(projectId, name, beschreibung, anlage.Geschosse),
            ClaudeSortFehltArt.Geschoss => Geschoss(projectId, anlage.BauteilId, name, beschreibung),
            _ => Dokumenttyp(projectId, name, anlage.Unterteilung)
        };
    }

    /// <summary>Haeufigster Ablagebereich der vorhandenen Dokumenttypen, sonst <see cref="StandardAblage"/>.</summary>
    public static string AblageFuer(IReadOnlyList<PlanDocumentType> types) =>
        types.Where(t => !string.IsNullOrWhiteSpace(t.RootRelativePath))
            .GroupBy(t => t.RootRelativePath, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? StandardAblage;

    private ClaudeSortAnlageErgebnis Bauteil(string projectId, string name, string? beschreibung,
        IReadOnlyList<string> geschosse)
    {
        if (_db.GetBuildingParts(projectId).Any(p => Gleich(p.ShortName, name)))
            return Fehler($"Ein Bauteil „{name}“ gibt es schon.");
        var id = _db.InsertBuildingPart(projectId, name, description: beschreibung);
        var namen = geschosse.Select(g => g.Trim()).Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var mitIds = namen.Select(g => $"{g} (geschoss_id {_db.InsertBuildingLevel(id, g)})").ToList();
        Log.Information("Claude-Sortierung: Bauteil per Schnellanlage angelegt ({Levels} Geschosse)", namen.Count);
        var titel = beschreibung is null ? name : $"{name} „{beschreibung}“";
        return new ClaudeSortAnlageErgebnis(true,
            $"Bauteil {titel}" + (namen.Count > 0 ? $" mit Geschossen {string.Join(", ", namen)}" : "") + " angelegt",
            $"Bauteil „{name}“ angelegt: bauteil_id {id}" +
            (mitIds.Count > 0 ? $", Geschosse {string.Join(", ", mitIds)}" : "") + ".");
    }

    private ClaudeSortAnlageErgebnis Geschoss(string projectId, string? bauteilId, string name, string? beschreibung)
    {
        var part = _db.GetBuildingParts(projectId).FirstOrDefault(p => p.Id == bauteilId);
        if (part is null)
            return Fehler("Bitte ein Bauteil wählen.");
        if (part.Levels.Any(l => Gleich(l.Name, name)))
            return Fehler($"Geschoss „{name}“ gibt es bei {part.ShortName} schon.");
        var id = _db.InsertBuildingLevel(part.Id, name, description: beschreibung);
        Log.Information("Claude-Sortierung: Geschoss per Schnellanlage angelegt");
        return new ClaudeSortAnlageErgebnis(true, $"Geschoss {name} bei {part.ShortName} angelegt",
            $"Geschoss „{name}“ bei Bauteil {part.ShortName} (bauteil_id {part.Id}) angelegt: geschoss_id {id}.");
    }

    private ClaudeSortAnlageErgebnis Dokumenttyp(string projectId, string name, Ring2Source unterteilung)
    {
        var types = _db.GetDocumentTypes(projectId);
        if (types.Any(t => Gleich(t.Name, name)))
            return Fehler($"Einen Dokumenttyp „{name}“ gibt es schon.");
        // Farbe null = Theme-Standard („Farbe automatisch“, Mockup 3.4 v1.3)
        var type = _creation.Create(projectId, name, AblageFuer(types), unterteilung);
        return new ClaudeSortAnlageErgebnis(true,
            $"Dokumenttyp „{type.Name}“ ({UnterteilungText(unterteilung)}) angelegt",
            $"Dokumenttyp „{type.Name}“ angelegt: dokumenttyp_id {type.Id}, Unterteilung {Schluessel(unterteilung)}.");
    }

    internal static string Schluessel(Ring2Source source) => source switch
    {
        Ring2Source.BuildingParts => "bauteile",
        Ring2Source.Categories => "kategorien",
        _ => "keine"
    };

    internal static string UnterteilungText(Ring2Source source) => source switch
    {
        Ring2Source.BuildingParts => "Bauteile",
        Ring2Source.Categories => "Kategorien",
        _ => "ohne Unterteilung"
    };

    private static bool Gleich(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);

    private static ClaudeSortAnlageErgebnis Fehler(string text) => new(false, text, string.Empty);
}
