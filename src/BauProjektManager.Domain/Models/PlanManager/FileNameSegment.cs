namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ein einzelnes Segment eines geparsten Dateinamens.
/// Position 0 = erstes Segment nach dem Split.
/// FieldType ist null solange der User noch nichts zugewiesen hat.
/// </summary>
public class FileNameSegment
{
    public int Position { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public FieldType? FieldType { get; set; }
    public string? CustomFieldName { get; set; }

    /// <summary>
    /// Anzeigename für die UI: Enum-Name, Custom-Name, oder "—".
    /// </summary>
    public string DisplayName =>
        FieldType == PlanManager.FieldType.Custom
            ? CustomFieldName ?? "—"
            : FieldType?.ToString() ?? "—";
}
