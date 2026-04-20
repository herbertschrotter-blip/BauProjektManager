namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Anlernbares Profil fuer einen Dokumenttyp (z.B. Polierplan, Schalungsplan, Bauprotokoll).
/// Wird als JSON in .bpm/profiles/{id}.json pro Projekt gespeichert (ADR-010, ADR-046).
/// Schema v2 (Cross-Review 15.04.2026): documentTypeId, tokenization, indexExtraction, includeInIdentity.
/// Schema v3 (BPM-082, 2026-04-17): segment-basierte Erkennung via RecognitionRule.SegmentPosition.
/// </summary>
public class RecognitionProfile
{
    public int SchemaVersion { get; set; } = 3;
    public string Id { get; set; } = string.Empty;

    // --- Dokumenttyp (v2: TypeId + DisplayName getrennt) ---
    public string DocumentTypeId { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;

    // --- Index-Konfiguration (ADR-045) ---
    public IndexSourceType IndexSource { get; set; } = IndexSourceType.FileName;
    public string IndexMode { get; set; } = "optional";
    public string IndexPattern { get; set; } = @"^[A-Z0-9]{1,3}$";
    public IndexComparisonConfig IndexComparison { get; set; } = new();

    // --- Index-Extraktion (v2: Regex pro Profil für zusammengeschriebene Indizes) ---
    public IndexExtractionConfig? IndexExtraction { get; set; }

    // --- Tokenization (v2: erweiterte Delimiter-Konfiguration) ---
    public TokenizationConfig Tokenization { get; set; } = new();

    // --- Parsing ---
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
/// Tokenization configuration per profile (v2).
/// Replaces the flat Delimiters list from v1.
/// </summary>
public class TokenizationConfig
{
    public List<string> Delimiters { get; set; } = ["-", "_"];
    public bool CollapseRepeatedDelimiters { get; set; }
    public string? FirstTokenDelimiter { get; set; }
}

/// <summary>
/// Index extraction via Regex per profile (v2).
/// For cases like "002a" (number+index without delimiter).
/// </summary>
public class IndexExtractionConfig
{
    /// <summary>"segment", "filename", or "suffix".</summary>
    public string Source { get; set; } = "segment";
    /// <summary>Which segment to apply regex to (e.g. "planNumber"). Only for source=segment.</summary>
    public string? SegmentSelector { get; set; }
    /// <summary>Regex with named groups (e.g. "^(?&lt;number&gt;\d{3})(?&lt;index&gt;[A-Za-z])$").</summary>
    public string Pattern { get; set; } = string.Empty;
    /// <summary>Name of the capture group for the number part.</summary>
    public string NumberGroup { get; set; } = "number";
    /// <summary>Name of the capture group for the index part.</summary>
    public string IndexGroup { get; set; } = "index";
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
/// Segment-Definition im Profil (v2: includeInIdentity).
/// </summary>
public class ProfileSegment
{
    public int Position { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; }
    /// <summary>
    /// Whether this segment contributes to the document_key.
    /// Default false for Custom fields (must be explicitly set).
    /// </summary>
    public bool IncludeInIdentity { get; set; }
}

/// <summary>
/// Erkennungsregel: segment (Default) oder regex (Fallback fuer Sonderfaelle).
/// Schema v3 (BPM-082): SegmentPosition ergaenzt fuer positionsgenaue Erkennung.
/// Alte Methoden prefix/contains wurden entfernt (Fruehphase, keine Produktivdaten).
/// </summary>
public class RecognitionRule
{
    /// <summary>Erkennungsmethode: "segment" (Default) oder "regex" (Fallback).</summary>
    public string Method { get; set; } = "segment";

    /// <summary>Das Muster bzw. der erwartete Wert. Bei segment: Token-Wert. Bei regex: Pattern.</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Nur bei method="segment": 0-basierte Position des Tokens im tokenisierten Dateinamen.
    /// Muss gesetzt sein wenn Method=segment. Bei regex null.
    /// </summary>
    public int? SegmentPosition { get; set; }

    /// <summary>
    /// Prueft ob die Regel fachlich valide ist. Wird beim Laden (ProfileManager)
    /// und defensiv im Recognizer verwendet.
    /// </summary>
    /// <param name="reason">Begruendung bei Invalid, leer bei Valid.</param>
    /// <returns>true wenn Regel vollstaendig und konsistent.</returns>
    public bool IsValid(out string reason)
    {
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            reason = "Pattern fehlt.";
            return false;
        }

        switch (Method?.ToLowerInvariant())
        {
            case "segment":
                if (SegmentPosition is null || SegmentPosition < 0)
                {
                    reason = "segment-Regel braucht SegmentPosition >= 0.";
                    return false;
                }
                break;

            case "regex":
                // Pattern-Syntax wird erst zur Match-Zeit geprueft (ReDoS-Schutz dort)
                break;

            default:
                reason = $"Unbekannte Methode: {Method}";
                return false;
        }

        reason = "";
        return true;
    }
}

/// <summary>
/// Gruppierungs-Konfiguration: wie Dateien zu Revisionen zusammengefasst werden.
/// V1: identity (nach document_key + document_type + revision).
/// </summary>
public class GroupingConfig
{
    public string Mode { get; set; } = "identity";
}
