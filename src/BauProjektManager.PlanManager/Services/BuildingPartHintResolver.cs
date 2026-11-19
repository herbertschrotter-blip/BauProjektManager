using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Loest einen Bauteil-Hinweis aus dem Dateinamen (<c>PlanFileCandidates.BuildingPartHint</c>, z. B. „H2“) zu einem
/// Bauteil auf (BPM-121.03, ADR-065 P.14): zuerst die gemerkten Aliasse (building_part_aliases), dann der direkte
/// Vergleich mit Kuerzel und Beschreibung — beides exakt nach <see cref="IPlanValueNormalizer.NormalizeForMatch"/>.
/// Kein Fuzzy.
/// </summary>
public sealed class BuildingPartHintResolver(
    IReadOnlyList<BuildingPart> parts,
    IReadOnlyDictionary<string, string> aliases,
    IPlanValueNormalizer normalizer)
{
    /// <summary>Ohne Stammdaten/Aliasse (z. B. Tests): loest nichts auf.</summary>
    public static BuildingPartHintResolver Empty(IPlanValueNormalizer normalizer) => new([], new Dictionary<string, string>(), normalizer);

    /// <summary>Bauteil zum Hinweis oder NULL.</summary>
    public BuildingPart? Resolve(string? hint)
    {
        var key = Key(hint);
        if (key.Length == 0)
            return null;
        if (aliases.TryGetValue(key, out var partId) && parts.FirstOrDefault(p => p.Id == partId) is { } aliased)
            return aliased;
        return parts.FirstOrDefault(p => Key(p.ShortName) == key || Key(p.Description) == key);
    }

    /// <summary>True, wenn der Hinweis ueber einen gemerkten Alias aufgeloest wird (Anzeige „(Alias)“).</summary>
    public bool IsAlias(string? hint)
    {
        var key = Key(hint);
        return key.Length > 0 && aliases.ContainsKey(key);
    }

    /// <summary>Vergleichsschluessel eines Hinweises (fuer „gleiches Kuerzel“ ueber mehrere Dateien).</summary>
    public string Key(string? value) => string.IsNullOrWhiteSpace(value) ? "" : normalizer.NormalizeForMatch(value);
}
