using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Seed-on-start fuer Built-in Segmenttyp-Gruppen und -Typen (BPM-108 Phase A).
/// </summary>
/// <remarks>
/// <para>Built-in IDs (snake_case) und SemanticRole sind seed-definiert und unveraenderlich.</para>
/// <para>Update-Policy: nicht user-modifizierte Felder werden bei jeder App-Start-Migration
/// aus dem Seed ueberschrieben (Name/Farbe/Sortierung/Gruppe). User-modifizierte Felder
/// (<c>user_modified_*</c> = 1) bleiben unangetastet.</para>
/// <para>Aktuelle <c>BuiltinVersion</c>: 1.</para>
/// </remarks>
public class SegmentTypeSeedService
{
    private const int CurrentBuiltinVersion = 1;

    private readonly ISegmentTypeRepository _repository;

    public SegmentTypeSeedService(ISegmentTypeRepository repository)
    {
        _repository = repository;
    }

    public void Seed()
    {
        SeedGroups();
        SeedTypes();
        Log.Information("BPM-108: Segmenttyp-Seed abgeschlossen (Built-in-Version {Version})", CurrentBuiltinVersion);
    }

    private void SeedGroups()
    {
        foreach (var seed in BuiltinGroups())
        {
            var existing = _repository.GetGroup(seed.Id, includeDeleted: true);
            if (existing is null)
            {
                _repository.SaveGroup(seed);
                Log.Debug("BPM-108 Seed: Gruppe {Id} neu angelegt", seed.Id);
                continue;
            }

            // Bestehende Built-in-Gruppe: nicht user-modifizierte Felder aus Seed uebernehmen
            var changed = false;
            if (!existing.UserModifiedName && existing.Name != seed.Name)
            {
                existing.Name = seed.Name;
                changed = true;
            }
            if (!existing.UserModifiedSort && existing.SortOrder != seed.SortOrder)
            {
                existing.SortOrder = seed.SortOrder;
                changed = true;
            }
            if (existing.BuiltinVersion != CurrentBuiltinVersion)
            {
                existing.BuiltinVersion = CurrentBuiltinVersion;
                changed = true;
            }

            if (changed) _repository.SaveGroup(existing);
        }
    }

    private void SeedTypes()
    {
        foreach (var seed in BuiltinTypes())
        {
            var existing = _repository.GetType(seed.Id, includeDeleted: true);
            if (existing is null)
            {
                _repository.SaveType(seed);
                Log.Debug("BPM-108 Seed: Typ {Id} neu angelegt", seed.Id);
                continue;
            }

            var changed = false;
            if (!existing.UserModifiedName && existing.Name != seed.Name)
            {
                existing.Name = seed.Name;
                changed = true;
            }
            if (!existing.UserModifiedColor && existing.Color != seed.Color)
            {
                existing.Color = seed.Color;
                changed = true;
            }
            if (!existing.UserModifiedSort && existing.SortOrder != seed.SortOrder)
            {
                existing.SortOrder = seed.SortOrder;
                changed = true;
            }
            if (!existing.UserModifiedGroup && existing.GroupId != seed.GroupId)
            {
                existing.GroupId = seed.GroupId;
                changed = true;
            }
            // semantic_role und token_key sind bei Built-ins unveraenderlich:
            // falls aus irgendeinem Grund verschoben, korrigieren.
            if (existing.SemanticRole != seed.SemanticRole)
            {
                existing.SemanticRole = seed.SemanticRole;
                changed = true;
            }
            if (existing.TokenKey != seed.TokenKey)
            {
                existing.TokenKey = seed.TokenKey;
                changed = true;
            }
            if (existing.BuiltinVersion != CurrentBuiltinVersion)
            {
                existing.BuiltinVersion = CurrentBuiltinVersion;
                changed = true;
            }

            if (changed) _repository.SaveType(existing);
        }
    }

    // === Built-in Definitionen ===

    public const string GroupIdentifikation = "grp_identifikation";
    public const string GroupRaeumlich = "grp_raeumlich";
    public const string GroupInhaltlich = "grp_inhaltlich";
    public const string GroupSonstiges = "grp_sonstiges";

    private static IEnumerable<SegmentTypeGroupDefinition> BuiltinGroups() => new[]
    {
        new SegmentTypeGroupDefinition { Id = GroupIdentifikation, Name = "Identifikation", SortOrder = 10, IsBuiltin = true, BuiltinVersion = CurrentBuiltinVersion },
        new SegmentTypeGroupDefinition { Id = GroupRaeumlich,      Name = "Raeumlich",      SortOrder = 20, IsBuiltin = true, BuiltinVersion = CurrentBuiltinVersion },
        new SegmentTypeGroupDefinition { Id = GroupInhaltlich,     Name = "Inhaltlich",     SortOrder = 30, IsBuiltin = true, BuiltinVersion = CurrentBuiltinVersion },
        new SegmentTypeGroupDefinition { Id = GroupSonstiges,      Name = "Sonstiges",      SortOrder = 40, IsBuiltin = true, BuiltinVersion = CurrentBuiltinVersion }
    };

    /// <summary>16 Built-in Segmenttypen, gruppiert. Farben aus dem aktuellen Theme.</summary>
    private static IEnumerable<SegmentTypeDefinition> BuiltinTypes() => new[]
    {
        // Identifikation
        New("plan_number",     "Plannummer",     "#0F6E56", SegmentSemanticRole.PlanNumber,    GroupIdentifikation, 10),
        New("plan_index",      "Index",          "#993C1D", SegmentSemanticRole.PlanIndex,     GroupIdentifikation, 20),
        New("project_number",  "Projektnummer",  "#534AB7", SegmentSemanticRole.ProjectNumber, GroupIdentifikation, 30),

        // Raeumlich
        New("geschoss",        "Geschoss",       "#185FA5", SegmentSemanticRole.Spatial, GroupRaeumlich, 10),
        New("haus",            "Haus",           "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 20),
        New("bauteil",         "Bauteil",        "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 30),
        New("bauabschnitt",    "Bauabschnitt",   "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 40),
        New("stiege",          "Stiege",         "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 50),
        New("achse",           "Achse",          "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 60),
        New("zone",            "Zone",           "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 70),
        New("block",           "Block",          "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 80),
        New("objekt",          "Objekt",         "#6E6E6E", SegmentSemanticRole.Spatial, GroupRaeumlich, 90),

        // Inhaltlich
        New("planart",         "Planart",        "#1F7280", SegmentSemanticRole.None,        GroupInhaltlich, 10),
        New("description",     "Bezeichnung",    "#555555", SegmentSemanticRole.Description, GroupInhaltlich, 20),

        // Sonstiges
        New("datum",           "Datum",          "#6E6E6E", SegmentSemanticRole.Date,   GroupSonstiges, 10),
        New("ignore",          "Ignorieren",     "#3C3C3C", SegmentSemanticRole.Ignore, GroupSonstiges, 20)
    };

    private static SegmentTypeDefinition New(string id, string name, string color, SegmentSemanticRole role, string groupId, int sortOrder)
    {
        return new SegmentTypeDefinition
        {
            Id = id,
            Name = name,
            Color = color,
            TokenKey = id,
            SemanticRole = role,
            GroupId = groupId,
            SortOrder = sortOrder,
            IsBuiltin = true,
            BuiltinVersion = CurrentBuiltinVersion
        };
    }
}
