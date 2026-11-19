using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Baut ein <see cref="RecognitionProfile"/> aus dem Stand des Profil-Wizards (BPM-130, ex BPM-087): Domain-Logik
/// raus aus dem ProfileManager (der nur noch laedt und speichert). Das Profil haengt am Dokumenttyp (ADR-065 P.7):
/// Id und Name kommen aus <c>document_types</c>, es gibt keinen eigenen Profilnamen. Reine Funktion ohne Zustand.
/// </summary>
public static class ProfileBuilder
{
    /// <summary>
    /// Profil aus dem Wizard. Pflicht und Identitaet ueber die Rolle des Segmenttyps: Plannummer = Pflicht,
    /// Plannummer und raeumliche Typen = identitaetsbildend.
    /// </summary>
    /// <param name="existingProfileId">Vorhandenes Profil desselben Dokumenttyps — wird ersetzt statt verdoppelt.</param>
    public static RecognitionProfile Build(
        ISegmentTypeCatalog? segmentTypeCatalog,
        PlanDocumentType documentType,
        IndexSourceType indexSource,
        bool indexModeOptional,
        bool indexCaseInsensitive,
        IReadOnlyList<FileNameSegment> segments,
        IReadOnlyList<string> delimiters,
        IReadOnlyList<RecognitionRule> recognition,
        int recognitionPriority = 100,
        string? existingProfileId = null)
    {
        var snapshot = segmentTypeCatalog?.SnapshotIncludingDeleted();

        var profileSegments = segments
            .Where(s => !string.IsNullOrEmpty(s.FieldTypeId))
            .Select(s =>
            {
                var role = LookupRole(snapshot, s.FieldTypeId);
                return new ProfileSegment
                {
                    Position = s.Position,
                    FieldTypeId = s.FieldTypeId!,
                    Required = role == SegmentSemanticRole.PlanNumber,
                    IncludeInIdentity = role is SegmentSemanticRole.PlanNumber or SegmentSemanticRole.Spatial
                };
            })
            .ToList();

        var identityFields = new List<string> { ProfileManager.DocumentTypeIdentityKey };
        identityFields.AddRange(profileSegments.Where(s => s.IncludeInIdentity)
            .OrderBy(s => s.Position).Select(s => s.FieldTypeId));

        return new RecognitionProfile
        {
            Id = existingProfileId ?? string.Empty,
            SchemaVersion = ProfileManager.CurrentSchemaVersion,
            DocumentTypeId = documentType.Id,
            DocumentTypeName = documentType.Name,
            IndexSource = indexSource,
            IndexMode = indexModeOptional ? "optional" : "required",
            IndexComparison = new IndexComparisonConfig { Mode = "alphabetic", CaseInsensitive = indexCaseInsensitive },
            Tokenization = new TokenizationConfig { Delimiters = [.. delimiters] },
            Segments = profileSegments,
            IdentityFields = identityFields,
            Recognition = [.. recognition],
            RecognitionPriority = recognitionPriority,
            Grouping = new GroupingConfig { Mode = "identity" }
        };
    }

    private static SegmentSemanticRole LookupRole(
        IReadOnlyDictionary<string, SegmentTypeDefinition>? snapshot, string? fieldTypeId)
    {
        if (snapshot is null || string.IsNullOrEmpty(fieldTypeId))
            return SegmentSemanticRole.None;
        return snapshot.TryGetValue(fieldTypeId, out var def)
            ? def.SemanticRole ?? SegmentSemanticRole.None
            : SegmentSemanticRole.None;
    }
}
