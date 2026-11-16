using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Uebernimmt gepruefte Entwurfszeilen als Pending-Zuordnungen (ADR-059), markiert
/// als "Vorschlag von Claude" (<see cref="PendingAssignmentSource.ClaudeSuggestion"/>).
/// Gleiche Felder wie ManualCaptureViewModel.CompleteCapture (Typ-ID/Name, Bauteil
/// bzw. Kategorie und Geschoss mit IDs und Namen, Nummer, Index, Resolver-Ziel,
/// Title = bezeichnung). Importiert wird NIE hier, sondern erst mit "Import
/// bestaetigen" ueber den journalisierten Import (ADR-064).
/// </summary>
public static class ClaudeSortUebernahme
{
    /// <summary>Eine gepruefte Zeile als Pending-Zuordnung (Vorschlag).</summary>
    public static PendingAssignment ZuPending(ClaudeSortZeile zeile) => new(
        zeile.Item.File, zeile.Item.Bucket,
        zeile.Type.Id, zeile.Type.Name,
        BuildingPart: zeile.Part is not null ? PartName(zeile.Part) : zeile.Category?.Name,
        Level: zeile.Level?.Name,
        PlanNumber: zeile.PlanNumber,
        Index: zeile.Index,
        TargetRelativeDirectory: zeile.TargetRelativeDirectory,
        Match: null,
        Title: zeile.Title,
        BuildingPartId: zeile.Part?.Id,
        BuildingLevelId: zeile.Level?.Id,
        CategoryId: zeile.Category?.Id,
        Source: PendingAssignmentSource.ClaudeSuggestion,
        Suggestion: new ClaudeSuggestionInfo(zeile.Begruendung, zeile.Unsicher, zeile.Hinweise));

    /// <summary>
    /// Setzt die Zeilen in den Store. Eine Datei, die der User schon selbst
    /// zugeordnet hat (Manual), wird NICHT ueberschrieben; ein frueherer
    /// Claude-Vorschlag schon.
    /// </summary>
    public static ClaudeSortUebernahmeErgebnis Uebernehmen(
        IEnumerable<ClaudeSortZeile> zeilen, PendingAssignmentStore store)
    {
        var uebernommen = new List<PendingAssignment>();
        var uebersprungen = new List<(string FileName, string Grund)>();
        foreach (var zeile in zeilen)
        {
            var scan = zeile.Item.File.Scan;
            if (store.Get(scan.RelativePath) is { IsClaudeSuggestion: false })
            {
                uebersprungen.Add((scan.FileName, "bereits von Hand zugeordnet — bleibt unverändert"));
                continue;
            }
            var pending = ZuPending(zeile);
            store.Assign(pending);
            uebernommen.Add(pending);
        }
        Log.Information("Claude-Sortierung: {Count} Vorschlaege uebernommen, {Skipped} uebersprungen",
            uebernommen.Count, uebersprungen.Count);
        return new ClaudeSortUebernahmeErgebnis(uebernommen, uebersprungen);
    }

    // Wie RadialSelectionController.EffectivePartName: Kurzname, sonst Beschreibung.
    private static string PartName(BuildingPart part)
        => !string.IsNullOrWhiteSpace(part.ShortName) ? part.ShortName : part.Description;
}
