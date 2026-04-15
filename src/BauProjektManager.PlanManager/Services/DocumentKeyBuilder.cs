using BauProjektManager.Domain.Models.PlanManager;
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
                if (fieldName.Equals("documentType", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Map field name to extracted value
                var fieldKey = fieldName.ToLowerInvariant();
                if (extractedFields.TryGetValue(fieldKey, out var value)
                    && !string.IsNullOrWhiteSpace(value))
                {
                    parts.Add(Normalize(value));
                }
            }
        }
        else
        {
            // No profile — try common identity fields
            if (extractedFields.TryGetValue("plannumber", out var planNr)
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

    /// <summary>
    /// Normalizes a value for use in document_key:
    /// lowercase, trim, replace spaces/special chars with underscore.
    /// </summary>
    private static string Normalize(string value)
    {
        var normalized = value.ToLowerInvariant().Trim();
        normalized = normalized
            .Replace("ä", "ae").Replace("ö", "oe")
            .Replace("ü", "ue").Replace("ß", "ss");
        normalized = normalized
            .Replace(" ", "_").Replace("-", "_")
            .Replace(".", "_").Replace(",", "");
        // Collapse multiple underscores
        while (normalized.Contains("__"))
            normalized = normalized.Replace("__", "_");
        return normalized.Trim('_');
    }
}
