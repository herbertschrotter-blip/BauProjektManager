using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Compat-Shim fuer BPM-108 Phase B: uebersetzt den Wizard-internen <see cref="FieldType"/>-Enum
/// auf die snake_case <c>segment_types.id</c>-Strings, die Schema v4 erwartet.
/// Wird in Phase C entfernt, sobald der Wizard direkt mit <c>ISegmentTypeCatalog</c> arbeitet.
/// </summary>
internal static class LegacyFieldTypeMapper
{
    /// <summary>
    /// Liefert die <c>segment_types.id</c> fuer ein Wizard-Segment.
    /// Custom-Segmente bekommen einen aus <see cref="FileNameSegment.CustomFieldName"/>
    /// abgeleiteten token_key.
    /// </summary>
    public static string ToFieldTypeId(FileNameSegment segment)
    {
        if (segment.FieldType is null) return string.Empty;

        if (segment.FieldType == FieldType.Custom)
        {
            return string.IsNullOrWhiteSpace(segment.CustomFieldName)
                ? "custom"
                : NormalizeTokenKey(segment.CustomFieldName);
        }

        return EnumToId(segment.FieldType.Value);
    }

    /// <summary>
    /// Liefert die <c>segment_types.id</c> fuer ein bekanntes <see cref="FieldType"/>.
    /// </summary>
    public static string EnumToId(FieldType type) => type switch
    {
        FieldType.PlanNumber => "plan_number",
        FieldType.PlanIndex => "plan_index",
        FieldType.ProjectNumber => "project_number",
        FieldType.Description => "description",
        FieldType.Ignore => "ignore",
        FieldType.Datum => "datum",
        FieldType.Geschoss => "geschoss",
        FieldType.Haus => "haus",
        FieldType.Planart => "planart",
        FieldType.Objekt => "objekt",
        FieldType.Bauteil => "bauteil",
        FieldType.Bauabschnitt => "bauabschnitt",
        FieldType.Stiege => "stiege",
        FieldType.Achse => "achse",
        FieldType.Zone => "zone",
        FieldType.Block => "block",
        FieldType.Custom => "custom",
        _ => type.ToString().ToLowerInvariant()
    };

    /// <summary>
    /// Identitaets-Relevanz (CGR-r2): PlanNumber + alle Spatial-Built-ins.
    /// Custom-Segmente sind nie identitaetsrelevant (SemanticRole = NULL).
    /// </summary>
    public static bool IsIdentityRelevant(FieldType? type) => type is
        FieldType.PlanNumber
        or FieldType.Geschoss
        or FieldType.Haus
        or FieldType.Bauteil
        or FieldType.Bauabschnitt
        or FieldType.Stiege
        or FieldType.Achse
        or FieldType.Zone
        or FieldType.Block
        or FieldType.Objekt;

    /// <summary>
    /// Normalisiert eine Eingabe (z. B. Enum-Name oder bereits snake_case) auf eine stabile ID.
    /// Reservierte System-Keys (z. B. <c>documentType</c>) bleiben unveraendert.
    /// </summary>
    public static string NormalizeFieldTypeId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (raw == "documentType") return raw;

        // Versuche Enum-Name zu interpretieren
        if (Enum.TryParse<FieldType>(raw, ignoreCase: true, out var ft) && ft != FieldType.Custom)
        {
            return EnumToId(ft);
        }

        // Sonst: bereits snake_case oder Custom-Token — passthrough mit Normalisierung
        return NormalizeTokenKey(raw);
    }

    /// <summary>
    /// Erzeugt einen snake_case-token_key aus einem freien Namen (z. B. "Akustik-Klasse" → "akustik_klasse").
    /// </summary>
    public static string NormalizeTokenKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var lower = name.ToLowerInvariant().Trim();
        lower = lower.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var collapsed = new System.Text.StringBuilder(chars.Length);
        var lastUnderscore = false;
        foreach (var c in chars)
        {
            if (c == '_')
            {
                if (!lastUnderscore && collapsed.Length > 0) collapsed.Append('_');
                lastUnderscore = true;
            }
            else
            {
                collapsed.Append(c);
                lastUnderscore = false;
            }
        }
        return collapsed.ToString().Trim('_');
    }
}
