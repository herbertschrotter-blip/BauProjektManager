using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Builds deterministic document_key from resolved classification fields.
/// The document_key uniquely identifies a document across revisions.
/// Step 5 of the 7-stage analysis pipeline.
///
/// Key composition: DocumentTypeId + identity fields from profile (normalized).
/// Excludes: index/revision, extension, date, stage, raw folder path.
/// </summary>
public class DocumentKeyBuilder
{
    /// <summary>
    /// Builds a document_key from a classified file's identity fields.
    /// Returns null if essential fields are missing.
    /// </summary>
    public string? Build(
        string? documentTypeId,
        IReadOnlyDictionary<string, string> extractedFields,
        RecognitionProfile? profile)
    {
        if (string.IsNullOrEmpty(documentTypeId))
        {
            Log.Debug("DocumentKeyBuilder: kein DocumentTypeId — key=null");
            return null;
        }

        var parts = new List<string> { Normalize(documentTypeId) };

        // Add identity fields from profile definition
        if (profile is not null)
        {
            foreach (var fieldName in profile.IdentityFields)
            {
                // Skip documentType — already added as first part
                if (fieldName.Equals(SegmentTypeIds.DocumentTypeField, StringComparison.OrdinalIgnoreCase))
                    continue;

                // BPM-110: IdentityFields und ExtractedFields sind beide mit
                // segment_types.id gekeyt (Built-in snake_case, Custom ULID) —
                // verbatim nachschlagen, KEIN ToLowerInvariant (zerstoert ULIDs).
                if (extractedFields.TryGetValue(fieldName, out var value)
                    && !string.IsNullOrWhiteSpace(value))
                {
                    parts.Add(Normalize(value));
                }
            }
        }
        else
        {
            // No profile — try common identity fields
            if (extractedFields.TryGetValue(SegmentTypeIds.PlanNumber, out var planNr)
                && !string.IsNullOrWhiteSpace(planNr))
                parts.Add(Normalize(planNr));
        }

        // Must have at least type + one identity field
        if (parts.Count < 2)
        {
            Log.Debug("DocumentKeyBuilder: zu wenig Identity-Felder fuer {Type}", documentTypeId);
            return null;
        }

        var key = string.Join("|", parts);
        Log.Debug("DocumentKeyBuilder: {Key} aus {Parts} Teilen", key, parts.Count);
        return key;
    }

    // BPM-111.02: Normalisierung zentral in PlanValueNormalizer (eine Stelle
    // fuer Key/Match/FolderName) — Logik unveraendert, Keys bleiben stabil.
    private static readonly PlanValueNormalizer _normalizer = new();

    private static string Normalize(string value) => _normalizer.NormalizeForKey(value);
}
