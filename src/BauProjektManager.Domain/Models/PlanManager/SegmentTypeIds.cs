namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Zentrale Konstanten fuer die stabilen <c>segment_types.id</c>-Strings der
/// Built-in-Segmenttypen (BPM-110). Diese IDs sind zugleich die Keys in
/// <c>ParsedImportFile.ExtractedFields</c> und in <c>RecognitionProfile.IdentityFields</c>.
///
/// Schreiber (FileParseService via <c>SegmentDefinition.FieldTypeId</c>) und Leser
/// (ImportWorkflowService, DocumentKeyBuilder) MUESSEN dieselben Konstanten verwenden —
/// nie String-Literale. Custom-Segmenttypen verwenden ULIDs und sind hier nicht gelistet.
/// </summary>
public static class SegmentTypeIds
{
    // Identifikation
    public const string PlanNumber = "plan_number";
    public const string PlanIndex = "plan_index";
    public const string ProjectNumber = "project_number";

    // Raeumlich
    public const string Geschoss = "geschoss";
    public const string Haus = "haus";
    public const string Bauteil = "bauteil";
    public const string Bauabschnitt = "bauabschnitt";
    public const string Stiege = "stiege";
    public const string Achse = "achse";
    public const string Zone = "zone";
    public const string Block = "block";
    public const string Objekt = "objekt";

    // Inhaltlich
    public const string Planart = "planart";
    public const string Description = "description";

    // Sonstiges
    public const string Datum = "datum";
    public const string Ignore = "ignore";

    /// <summary>
    /// Sonderkey in <c>IdentityFields</c>: verweist auf den Dokumenttyp des Profils,
    /// KEIN Segmenttyp. DocumentKeyBuilder behandelt ihn separat (erster Key-Teil).
    /// </summary>
    public const string DocumentTypeField = "documentType";
}
