using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Resolves classification context from folder path, profile match, and extension.
/// Produces ResolutionEvidence trail for transparency in the import preview.
/// Step 4 of the 7-stage analysis pipeline.
/// </summary>
public class ImportContextResolver
{
    // Known folder patterns for document type derivation
    private static readonly Dictionary<string, string> FolderTypePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["schalung"] = "schalungsplan",
        ["bewehrung"] = "bewehrungsplan",
        ["polierplan"] = "polierplan",
        ["polierpläne"] = "polierplan",
        ["detailplan"] = "detailplan",
        ["detailpläne"] = "detailplan",
        ["fertigteil"] = "fertigteilplan",
        ["fertigteilpläne"] = "fertigteilplan",
        ["statikpläne"] = "statikplan",
        ["statikplanung"] = "statikplan",
        ["architekturplanung"] = "architekturplan",
        ["ausschreibung"] = "ausschreibungsplan",
        ["ausschreibungspläne"] = "ausschreibungsplan",
        ["dachstuhl"] = "dachstuhlplan",
        ["stiegen"] = "stiegenplan",
        ["leitungspläne"] = "leitungsplan",
    };

    // Known stage markers in folder names and file names
    private static readonly string[] DraftFolderMarkers =
        ["vorabzug", "vorabzüge", "_vorabzug", "vorab"];
    private static readonly string[] DraftFileMarkers =
        ["vorab", "va-", "vorabzug"];

    /// <summary>
    /// Resolves context for a parsed file: document type, stage, and evidence.
    /// Folder segments are optional — if empty, only file name and profile are used.
    /// </summary>
    public ResolvedContext Resolve(
        ParsedImportFile parsed,
        string relativePath)
    {
        var evidence = new List<ResolutionEvidence>();
        var warnings = new List<ImportWarning>(parsed.Warnings);

        string? documentTypeId = null;
        string? documentTypeDisplayName = null;
        var stage = ImportStage.Unknown;

        // --- 1. Document type from profile match ---
        if (parsed.MatchedProfile is not null)
        {
            documentTypeId = parsed.MatchedProfile.DocumentTypeId;
            documentTypeDisplayName = parsed.MatchedProfile.DocumentTypeName;
            evidence.Add(new ResolutionEvidence(
                ResolutionSource.Profile, "documentType",
                documentTypeId, IsHard: true));
        }

        // --- 2. Folder-based context (if relativePath has directory segments) ---
        var folderSegments = GetFolderSegments(relativePath);

        if (folderSegments.Count > 0)
        {
            // 2a. Document type from folder name
            if (string.IsNullOrEmpty(documentTypeId))
            {
                foreach (var segment in folderSegments)
                {
                    foreach (var (pattern, typeId) in FolderTypePatterns)
                    {
                        if (segment.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            documentTypeId = typeId;
                            documentTypeDisplayName = typeId;
                            evidence.Add(new ResolutionEvidence(
                                ResolutionSource.FolderPath, "documentType",
                                typeId, IsHard: true));
                            warnings.Add(new ImportWarning(
                                ImportWarningCode.TypeDerivedFromFolder,
                                $"Typ '{typeId}' aus Ordner '{segment}'"));
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(documentTypeId)) break;
                }
            }

            // 2b. Stage from folder name
            foreach (var segment in folderSegments)
            {
                var lower = segment.ToLowerInvariant();
                if (DraftFolderMarkers.Any(m => lower.Contains(m)))
                {
                    stage = ImportStage.Draft;
                    evidence.Add(new ResolutionEvidence(
                        ResolutionSource.FolderPath, "stage",
                        "draft", IsHard: true));
                    warnings.Add(new ImportWarning(
                        ImportWarningCode.StageDerivedFromFolder,
                        $"Stage 'Draft' aus Ordner '{segment}'"));
                    break;
                }
            }
        }
        else
        {
            warnings.Add(new ImportWarning(ImportWarningCode.MissingFolderContext));
        }

        // --- 3. Stage from file name (if not already set from folder) ---
        if (stage == ImportStage.Unknown)
        {
            var lowerName = parsed.FileName.ToLowerInvariant();
            if (DraftFileMarkers.Any(m => lowerName.Contains(m)))
            {
                stage = ImportStage.Draft;
                evidence.Add(new ResolutionEvidence(
                    ResolutionSource.FileName, "stage",
                    "draft", IsHard: false));
            }
        }

        return new ResolvedContext(
            DocumentTypeId: documentTypeId,
            DocumentTypeDisplayName: documentTypeDisplayName,
            Stage: stage,
            Evidence: evidence,
            Warnings: warnings);
    }

    private static List<string> GetFolderSegments(string relativePath)
    {
        var dir = Path.GetDirectoryName(relativePath);
        if (string.IsNullOrEmpty(dir))
            return [];

        return dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}

/// <summary>
/// Result of context resolution for a single file.
/// </summary>
public sealed record ResolvedContext(
    string? DocumentTypeId,
    string? DocumentTypeDisplayName,
    ImportStage Stage,
    IReadOnlyList<ResolutionEvidence> Evidence,
    IReadOnlyList<ImportWarning> Warnings);
