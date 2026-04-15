namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Anlernbares Profil fuer einen Dokumenttyp (z.B. Polierplan, Schalungsplan, Bauprotokoll).
/// Wird als JSON in .bpm/profiles/{id}.json pro Projekt gespeichert (ADR-010, ADR-046).
/// </summary>
public class RecognitionProfile
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;

    // --- Index-Konfiguration (ADR-045) ---
    public IndexSourceType IndexSource { get; set; } = IndexSourceType.FileName;
    public string IndexMode { get; set; } = "optional";
    public string IndexPattern { get; set; } = @"^[A-Z0-9]{1,3}$";
    public IndexComparisonConfig IndexComparison { get; set; } = new();

    // --- Parsing ---
    public List<string> Delimiters { get; set; } = ["-", "_"];
    public List<ProfileSegment> Segments { get; set; } = [];
    public List<string> IdentityFields { get; set; } = ["documentType", "planNumber"];

    // --- Erkennung ---
    public List<RecognitionRule> Recognition { get; set; } = [];
    public int RecognitionPriority { get; set; } = 100;
    public string ConflictPolicy { get; set; } = "askUser";

    // --- Gruppierung + Ordner ---
    public GroupingConfig Grouping { get; set; } = new();
    public List<string> FolderHierarchy { get; set; } = [];
    public string RenameSchema { get; set; } = string.Empty;

    // --- Zeitstempel ---
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Index-Vergleichskonfiguration pro Profil.
/// V1: alphabetic. Post-V1: numeric, natural, custom.
/// </summary>
public class IndexComparisonConfig
{
    public string Mode { get; set; } = "alphabetic";
    public bool CaseInsensitive { get; set; } = true;
}

/// <summary>
/// Segment-Definition im Profil.
/// Position + Feldtyp + ob Pflichtfeld.
/// </summary>
public class ProfileSegment
{
    public int Position { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; }
}

/// <summary>
/// Erkennungsregel: prefix oder contains mit Muster.
/// </summary>
public class RecognitionRule
{
    public string Method { get; set; } = "contains";
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// Gruppierungs-Konfiguration: wie Dateien zu Revisionen zusammengefasst werden.
/// V1: baseFileName (gleicher Stamm = gleiche Revision).
/// </summary>
public class GroupingConfig
{
    public string Mode { get; set; } = "baseFileName";
}
