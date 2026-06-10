using System.Text.Json.Serialization;
using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Anlernbares Profil fuer einen Dokumenttyp (z.B. Polierplan, Schalungsplan, Bauprotokoll).
/// Wird als JSON in .bpm/profiles/{id}.json pro Projekt gespeichert (ADR-010, ADR-046).
/// </summary>
/// <remarks>
/// <para>Schema v2 (Cross-Review 15.04.2026): documentTypeId, tokenization, indexExtraction, includeInIdentity.</para>
/// <para>Schema v3 (BPM-082, 2026-04-17): segment-basierte Erkennung via RecognitionRule.SegmentPosition.</para>
/// <para>Schema v4 (BPM-108, 2026-05-18, ADR-056): <c>ProfileSegment.FieldTypeId</c> (statt Enum-String),
/// <c>identityFields</c>/<c>folderHierarchy</c>/<c>renameSchema</c> referenzieren <c>segment_types.id</c>
/// bzw. <c>token_key</c>. Recognition unveraendert. Strict Reset — kein Migrations-Code.</para>
/// </remarks>
public class RecognitionProfile
{
    public int SchemaVersion { get; set; } = 4;
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

    /// <summary>
    /// IDs der Felder die die Dokument-Identitaet bilden.
    /// V4: Eintraege sind entweder System-Keys (z. B. <c>"documentType"</c>) oder
    /// stabile <c>segment_types.id</c>-Strings (z. B. <c>"plan_number"</c>, <c>"haus"</c>).
    /// Reservierter System-Key: <c>documentType</c>.
    /// </summary>
    public List<string> IdentityFields { get; set; } = [SegmentTypeIds.DocumentTypeField, SegmentTypeIds.PlanNumber];

    // --- Erkennung ---
    public List<RecognitionRule> Recognition { get; set; } = [];
    public int RecognitionPriority { get; set; } = 100;
    public string ConflictPolicy { get; set; } = "askUser";

    // --- Gruppierung + Ordner ---
    public GroupingConfig Grouping { get; set; } = new();

    /// <summary>
    /// Hierarchie der Zielordner-Ebenen. V4: Eintraege sind <c>segment_types.id</c>-Strings.
    /// </summary>
    public List<string> FolderHierarchy { get; set; } = [];

    /// <summary>
    /// Rename-Template mit Token-Platzhaltern (V4: <c>{token_key}</c>, z. B. <c>{plan_number}-{plan_index}_{geschoss}</c>).
    /// </summary>
    public string RenameSchema { get; set; } = string.Empty;

    // --- Zeitstempel ---
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Health-Status. Wird beim Laden ermittelt und NICHT persistiert.
    /// Bei <see cref="ProfileHealth.MissingSegmentTypes"/> sind die fehlenden IDs in
    /// <see cref="MissingSegmentTypeIds"/> aufgefuehrt.
    /// </summary>
    [JsonIgnore]
    public ProfileHealth Health { get; set; } = ProfileHealth.Valid;

    /// <summary>
    /// Bei <see cref="ProfileHealth.MissingSegmentTypes"/>: Liste der unbekannten
    /// <c>fieldTypeId</c>s. Wird beim Laden befuellt und NICHT persistiert.
    /// </summary>
    [JsonIgnore]
    public List<string> MissingSegmentTypeIds { get; set; } = [];
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
/// Index extraction via Regex per profile (v2). For cases like "002a"
/// (number+index without delimiter).
/// </summary>
/// <remarks>
/// V4 (BPM-108): <see cref="SegmentSelector"/> ist eine <c>segment_types.id</c>
/// (z. B. <c>"plan_number"</c>) — nicht mehr camelCase Enum-Name.
/// </remarks>
public class IndexExtractionConfig
{
    /// <summary>"segment", "filename", or "suffix".</summary>
    public string Source { get; set; } = "segment";
    /// <summary>Welcher Segmenttyp wird gematcht (v4: <c>segment_types.id</c>, z. B. <c>"plan_number"</c>). Nur fuer source=segment.</summary>
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
/// Segment-Definition im Profil.
/// </summary>
/// <remarks>
/// V2: <c>includeInIdentity</c>.
/// V4 (BPM-108, ADR-056): <c>FieldType</c> (Enum-String) ersetzt durch <c>FieldTypeId</c>
/// (stabile <c>segment_types.id</c>, z. B. <c>"plan_number"</c> oder Custom-ULID).
/// <c>Label</c> entfaellt — Anzeige kommt zur Laufzeit aus <see cref="Enums.PlanManager.SegmentSemanticRole"/>/<c>segment_types.name</c>.
/// </remarks>
public class ProfileSegment
{
    public int Position { get; set; }

    /// <summary>
    /// Stabile Referenz auf <c>segment_types.id</c> (v4).
    /// Built-in: snake_case (z. B. <c>"plan_number"</c>). Custom: ULID.
    /// </summary>
    public string FieldTypeId { get; set; } = string.Empty;

    public bool Required { get; set; }

    /// <summary>
    /// Whether this segment contributes to the document_key.
    /// V4: gesetzt aus <c>SemanticRole == PlanNumber | Spatial</c>.
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
