namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Unterteilungs-Schema eines Dokumenttyps (ADR-059-Addendum):
/// bestimmt, was Ring 2 des Radials zeigt. Ring 3 (Geschoss) existiert
/// implizit nur bei <see cref="BuildingParts"/>.
/// </summary>
public enum Ring2Source
{
    /// <summary>Raeumlich: Ring 2 = Bauteile (bpm.db building_parts), Ring 3 = Geschosse.</summary>
    BuildingParts = 0,

    /// <summary>Kategorial: Ring 2 = typgebundene Kategorien (z. B. Protokollarten), kein Ring 3.</summary>
    Categories = 1,

    /// <summary>Keine Unterteilung: Zuordnung endet beim Dokumenttyp.</summary>
    None = 2
}
