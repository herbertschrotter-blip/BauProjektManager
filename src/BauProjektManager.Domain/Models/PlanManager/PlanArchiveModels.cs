namespace BauProjektManager.Domain.Models.PlanManager;

// Schema v2.0 Drei-Ebenen-Modell (BPM-109). Siehe DB-SCHEMA.md Kap. 6.7 + ADR-058 + ADR-058-Addendum.
// Cross-DB-Bezüge (BuildingPartId/BuildingLevelId/SegmentTypeId → bpm.db) sind Soft References:
// reine String-IDs ohne erzwungenen FK. Validierung service-seitig (Import-Resolve / Lookup).

/// <summary>
/// Logisches Plan-Dokument über alle Revisionen hinweg (Tabelle plan_documents).
/// Stabile Entität, auf die Cross-Modul-Links zeigen.
/// </summary>
/// <param name="Id">ULID.</param>
/// <param name="ProjectId">Projekt-ID (redundant zum DB-Pfad, bewusst für Sync/Export).</param>
/// <param name="DocumentKey">Natural Key vom DocumentKeyBuilder (UNIQUE).</param>
/// <param name="DocumentTypeId">Profil-ID (SoftRef, post-V1 FK auf recognition_profiles).</param>
/// <param name="PlanNumber">Plannummer.</param>
/// <param name="DocumentType">Dokumenttyp-Anzeigename.</param>
/// <param name="Title">Titel (optional, Default leer).</param>
/// <param name="TargetFolder">Zielordner-Wurzel.</param>
/// <param name="RelativeDirectory">Relatives Zielverzeichnis.</param>
/// <param name="BuildingPartId">SoftRef bpm.db.building_parts(id), NULL wenn nicht gemappt.</param>
/// <param name="BuildingLevelId">SoftRef bpm.db.building_levels(id), NULL wenn nicht gemappt.</param>
public sealed record PlanDocument(
    string Id,
    string ProjectId,
    string DocumentKey,
    string DocumentTypeId,
    string PlanNumber,
    string DocumentType,
    string Title,
    string TargetFolder,
    string RelativeDirectory,
    string? BuildingPartId,
    string? BuildingLevelId);

/// <summary>
/// Versionierte Revision eines Plan-Dokuments mit Zeitstempeln für Zeitreise (Tabelle plan_revisions).
/// </summary>
/// <param name="Id">ULID.</param>
/// <param name="DocumentId">FK plan_documents.id (Innen-FK, hart).</param>
/// <param name="PlanIndex">Plan-Index, NULL bei Erstausgabe / IndexSource=None.</param>
/// <param name="IndexSource">"FileName" / "None" / "PlanHeader".</param>
/// <param name="RevisionStatus">"current" / "superseded" / "rejected" (siehe <see cref="PlanArchive"/>).</param>
/// <param name="CurrentFrom">UTC ISO 8601 — wann diese Revision aktuell wurde.</param>
/// <param name="SupersededAt">UTC — wann ersetzt (NULL solange current).</param>
/// <param name="ReceivedAt">UTC — wann importiert (Hinzufügedatum).</param>
/// <param name="ReleasedAt">UTC — Freigabedatum des Index (NULL wenn unbekannt). Quelle: Plankopf-OCR / manuell (post-V1), Dateiname selten. Fürs Bautagebuch bevorzugt vor ReceivedAt (BPM-109.04b).</param>
/// <param name="LastImportId">Optional FK import_journal.id.</param>
public sealed record PlanRevision(
    string Id,
    string DocumentId,
    string? PlanIndex,
    string IndexSource,
    string RevisionStatus,
    string CurrentFrom,
    string? SupersededAt,
    string ReceivedAt,
    string? ReleasedAt,
    string? LastImportId,
    string ChangeNote = "");

/// <summary>
/// Extrahierter Segmentwert eines Dokuments (Tabelle plan_document_segments).
/// </summary>
/// <param name="Id">ULID.</param>
/// <param name="DocumentId">FK plan_documents.id (Innen-FK, hart).</param>
/// <param name="SegmentTypeId">SoftRef bpm.db.segment_types(id), kein FK (Cross-DB).</param>
/// <param name="SegmentKey">Denormalisierung für Debug/Export (token_key).</param>
/// <param name="RawValue">Original aus FileNameParser (z.B. "H1").</param>
/// <param name="NormalizedValue">Lowercase/normalisiert für Filter (z.B. "h1").</param>
public sealed record PlanDocumentSegment(
    string Id,
    string DocumentId,
    string SegmentTypeId,
    string SegmentKey,
    string RawValue,
    string NormalizedValue);

/// <summary>
/// Minimaler Audit-Trail-Eintrag für einen Revisions-Statuswechsel (Tabelle plan_revision_events).
/// </summary>
/// <param name="Id">ULID.</param>
/// <param name="RevisionId">FK plan_revisions.id (Innen-FK, hart).</param>
/// <param name="ImportId">Optional FK import_journal.id.</param>
/// <param name="EventType">"created" / "made_current" / "superseded" / "file_linked" / "manual_override".</param>
/// <param name="EventAt">UTC ISO 8601.</param>
/// <param name="Note">Freitext-Notiz (Default leer).</param>
public sealed record PlanRevisionEvent(
    string Id,
    string RevisionId,
    string? ImportId,
    string EventType,
    string EventAt,
    string Note);

/// <summary>
/// Cross-Modul-Verknüpfung von z.B. Bautagebuch/Foto auf eine konkrete Plan-Revision
/// (Tabelle plan_context_links). Aktiv genutzt post-V1 (BPM-056). PFLICHT: fixed_revision.
/// </summary>
/// <param name="Id">ULID.</param>
/// <param name="SourceModule">z.B. "bautagebuch", "foto", "vorlage".</param>
/// <param name="SourceId">ID im Source-Modul.</param>
/// <param name="TargetDocumentId">FK plan_documents.id (Innen-FK, hart).</param>
/// <param name="TargetRevisionId">FK plan_revisions.id — PFLICHT bei fixed_revision.</param>
/// <param name="ResolutionMode">Derzeit nur "fixed_revision" (ADR-058 fachliche Invariante).</param>
/// <param name="ContextTime">UTC — Berichtsdatum/Erstellzeitpunkt des Links.</param>
/// <param name="LinkType">"auto_reference" / "manual_reference" / "attachment".</param>
public sealed record PlanContextLink(
    string Id,
    string SourceModule,
    string SourceId,
    string TargetDocumentId,
    string? TargetRevisionId,
    string ResolutionMode,
    string ContextTime,
    string LinkType);

/// <summary>
/// String-Konstanten für die CHECK-Enums des Schema-v2.0 (vermeidet Magic Strings im Repository).
/// </summary>
public static class PlanArchive
{
    public static class Status
    {
        public const string Current = "current";
        public const string Superseded = "superseded";
        public const string Rejected = "rejected";
    }

    public static class EventType
    {
        public const string Created = "created";
        public const string MadeCurrent = "made_current";
        public const string Superseded = "superseded";
        public const string FileLinked = "file_linked";
        public const string ManualOverride = "manual_override";
    }

    public static class ResolutionMode
    {
        public const string FixedRevision = "fixed_revision";
    }

    public static class LinkType
    {
        public const string AutoReference = "auto_reference";
        public const string ManualReference = "manual_reference";
        public const string Attachment = "attachment";
    }
}
