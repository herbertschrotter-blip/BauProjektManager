using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Builds execution plan: calculates target paths for each import decision
/// (Stufe 7 der 7-stufigen Analyse-Pipeline).
///
/// ADR-061 Slice 0.6a: Der Zielpfad kommt jetzt aus dem
/// <see cref="DocumentTargetPathResolver"/> (DB = Ordner-Wahrheit) statt aus
/// <c>profile.TargetFolder</c> + roh angehängten Hierarchie-Werten. Das Mapping
/// von erkannten Feldern auf Ring2/Ring3 ist id-basiert (siehe <see cref="MapRings"/>).
/// </summary>
public class ImportPlanBuilder
{
    private readonly DocumentTargetPathResolver _resolver;
    private readonly ProjectDatabase _bpmDb;

    public ImportPlanBuilder(DocumentTargetPathResolver resolver, ProjectDatabase bpmDb)
    {
        _resolver = resolver;
        _bpmDb = bpmDb;
    }

    /// <summary>
    /// Reichert Import-Entscheidungen mit dem Zielpfad an (relativ zum Projektroot).
    /// </summary>
    public List<ImportDecision> BuildPlan(List<ImportDecision> decisions, string projectId)
    {
        var types = _bpmDb.GetDocumentTypes(projectId);
        var planned = new List<ImportDecision>(decisions.Count);

        foreach (var decision in decisions)
        {
            var targetPath = CalculateTargetPath(decision, projectId, types);
            planned.Add(decision with { TargetRelativePath = targetPath });
        }

        Log.Information("ImportPlanBuilder: {Count} Aktionen geplant", planned.Count);
        return planned;
    }

    private string? CalculateTargetPath(
        ImportDecision decision, string projectId, IReadOnlyList<PlanDocumentType> types)
    {
        // Kein Ziel für übersprungene/unbekannte/konfligierende Dateien.
        if (decision.Status is ImportStatus.SkipIdentical or ImportStatus.Unknown or ImportStatus.Conflict)
            return null;

        var profile = decision.File.Parsed.MatchedProfile;
        if (profile is null || string.IsNullOrWhiteSpace(profile.DocumentTypeId))
            return null;

        var type = types.FirstOrDefault(t => t.Id == profile.DocumentTypeId)
            ?? types.FirstOrDefault(t => !string.IsNullOrEmpty(t.Key)
                                         && t.Key.Equals(profile.DocumentTypeId, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            Log.Warning("ImportPlanBuilder: Dokumenttyp '{Type}' nicht in den Stammdaten — kein Zielpfad.",
                profile.DocumentTypeId);
            return null;
        }

        var (ring2, ring3) = MapRings(type.Ring2Source, profile.FolderHierarchy, decision.File.Parsed.ExtractedFields);

        try
        {
            var resolved = _resolver.Resolve(projectId, new DocumentTargetRequest(
                profile.DocumentTypeId, ring2, ring3, decision.File.Parsed.FileName));
            return resolved.RelativePath;
        }
        catch (DocumentTargetResolutionException ex)
        {
            // Fail-Fast des Resolvers → kein Teilpfad. Datei wird ohne Ziel geflaggt
            // (ImportExecutionService meldet "Kein Zielpfad berechnet"), NICHT fehlplatziert.
            Log.Warning("ImportPlanBuilder: Zielpfad nicht auflösbar für {File}: {Reason}",
                decision.File.Parsed.FileName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Segment-Ids, die ein <c>building_parts</c>-Bauteil bezeichnen (Ring 2 bei
    /// BuildingParts). Geschoss ist Ring 3, feinere Spatial-Ids (Achse/Zone) sind
    /// V1 nicht Teil der Ordnerstruktur.
    /// </summary>
    private static readonly HashSet<string> BuildingPartFieldIds = new(StringComparer.OrdinalIgnoreCase)
    {
        SegmentTypeIds.Haus, SegmentTypeIds.Bauteil, SegmentTypeIds.Block,
        SegmentTypeIds.Objekt, SegmentTypeIds.Bauabschnitt, SegmentTypeIds.Stiege,
    };

    /// <summary>
    /// Bildet erkannte Felder auf Ring2/Ring3-Tokens ab (ADR-061 Slice 0.6a, fixierte Regel):
    /// Ring3 = Wert von <c>geschoss</c>; Ring2 bei BuildingParts = erster Bauteil-Id-Wert
    /// aus der FolderHierarchy; Ring2 bei Categories = erster Nicht-<c>geschoss</c>-Wert;
    /// None = keine Ring-Tokens. Tokens werden vom Resolver gegen die Stammdaten aufgelöst.
    /// </summary>
    public static (string? Ring2, string? Ring3) MapRings(
        Ring2Source ring2Source, IReadOnlyList<string> folderHierarchy,
        IReadOnlyDictionary<string, string> extractedFields)
    {
        string? Value(string id)
            => extractedFields.TryGetValue(id, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

        switch (ring2Source)
        {
            case Ring2Source.BuildingParts:
                string? ring2 = null;
                foreach (var id in folderHierarchy)
                {
                    if (!BuildingPartFieldIds.Contains(id)) continue;
                    ring2 = Value(id);
                    if (ring2 is not null) break;
                }
                return (ring2, Value(SegmentTypeIds.Geschoss));

            case Ring2Source.Categories:
                foreach (var id in folderHierarchy)
                {
                    if (string.Equals(id, SegmentTypeIds.Geschoss, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = Value(id);
                    if (value is not null) return (value, null);
                }
                return (null, null);

            default: // None
                return (null, null);
        }
    }
}
