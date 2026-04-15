using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Parses file names using profiles and recognizes document types.
/// Step 3 of the 7-stage analysis pipeline.
/// Combines FileNameParser + DocumentTypeRecognizer.
/// </summary>
public class FileParseService
{
    private readonly DocumentTypeRecognizer _recognizer = new();

    /// <summary>
    /// Parses and classifies all fingerprinted files against the project's profiles.
    /// </summary>
    public List<ParsedImportFile> ParseAll(
        List<FingerprintResult> fingerprinted,
        List<RecognitionProfile> profiles)
    {
        var results = new List<ParsedImportFile>(fingerprinted.Count);

        foreach (var fp in fingerprinted)
        {
            var file = fp.File;
            var warnings = new List<ImportWarning>();

            // Hash error from fingerprint → low confidence
            if (!string.IsNullOrEmpty(fp.Error))
                warnings.Add(new ImportWarning(ImportWarningCode.NoIndexDetected, fp.Error));

            // Recognize document type
            var recognition = _recognizer.Recognize(file.Scan.FileName, profiles);
            var matchedProfile = recognition.MatchedProfile;
            var confidence = ParseConfidence.High;

            if (recognition.IsConflict)
            {
                warnings.Add(new ImportWarning(ImportWarningCode.MultipleProfileMatches,
                    $"{recognition.AllMatches.Count} Profile matchen"));
                confidence = ParseConfidence.Low;
            }
            else if (recognition.IsUnknown)
            {
                confidence = ParseConfidence.Low;
            }

            // Parse file name with profile's tokenization (or defaults)
            var tokenization = matchedProfile?.Tokenization;
            var parsed = FileNameParser.Parse(file.Scan.FileName, tokenization);

            // Extract fields from segments using profile definition
            var extractedFields = new Dictionary<string, string>();
            if (matchedProfile is not null)
            {
                foreach (var segDef in matchedProfile.Segments)
                {
                    var matchingSeg = parsed.Segments
                        .FirstOrDefault(s => s.Position == segDef.Position);
                    if (matchingSeg is not null)
                    {
                        var fieldKey = segDef.FieldType.ToLowerInvariant();
                        extractedFields[fieldKey] = matchingSeg.RawValue;
                    }
                }

                // Check required fields
                foreach (var segDef in matchedProfile.Segments.Where(s => s.Required))
                {
                    var fieldKey = segDef.FieldType.ToLowerInvariant();
                    if (!extractedFields.ContainsKey(fieldKey)
                        || string.IsNullOrWhiteSpace(extractedFields[fieldKey]))
                    {
                        confidence = ParseConfidence.Medium;
                    }
                }
            }

            results.Add(new ParsedImportFile(
                RelativePath: file.Scan.RelativePath,
                FileName: file.Scan.FileName,
                Extension: file.Scan.Extension,
                FileSize: file.Scan.FileSize,
                Md5: file.Md5,
                MatchedProfile: matchedProfile,
                ExtractedFields: extractedFields,
                Confidence: confidence,
                Warnings: warnings));
        }

        Log.Information("FileParseService: {Count} Dateien geparst, {Matched} mit Profil",
            results.Count, results.Count(r => r.MatchedProfile is not null));
        return results;
    }
}
