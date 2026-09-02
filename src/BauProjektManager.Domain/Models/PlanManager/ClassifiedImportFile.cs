using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// A parsed file after context resolution — fully classified.
/// Contains document type, plan number, revision, stage, and evidence trail.
/// </summary>
public sealed record ClassifiedImportFile(
    ParsedImportFile Parsed,
    string? DocumentTypeId,
    string? DocumentTypeDisplayName,
    string? DocumentKey,
    string? PlanNumber,
    string? RevisionToken,
    RevisionKind RevisionKind,
    IndexSourceType RevisionSource,
    ImportStage Stage,
    IReadOnlyDictionary<string, string> IdentityFields,
    IReadOnlyList<ResolutionEvidence> Evidence,
    string? Title = null,
    string? ChangeNote = null,
    string? ReleasedAt = null,
    IReadOnlyList<AssignedSegmentValue>? AssignedSegments = null,
    // BPM-126: Stammdaten-IDs der Radial-Zuordnung werden bis in plan_documents
    // durchgereicht (Cross-DB SoftRef, ADR-058-Addendum) — Grundlage fuer die
    // Plandaten-Ansicht und spaetere Cross-Modul-Abfragen (IPlanLookupService).
    string? BuildingPartId = null,
    string? BuildingLevelId = null);

/// <summary>
/// Final import decision for one file — ready for preview UI.
/// Contains status, document key, target path, and human-readable reasons.
/// </summary>
public sealed record ImportDecision(
    ClassifiedImportFile File,
    ImportStatus Status,
    string? DocumentKey,
    string? ExistingRevisionId,
    string? TargetRelativePath,
    IReadOnlyList<string> Reasons);

/// <summary>
/// Normalized document type with internal ID and display name.
/// TypeId is used for document_key, routing, and rules.
/// DisplayName is shown in UI/profiles.
/// </summary>
public sealed record DocumentTypeDescriptor(
    string TypeId,
    string DisplayName);
