using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Builds execution plan: calculates target paths for each import decision.
/// Step 7 of the 7-stage analysis pipeline.
/// Uses profile's targetFolder + folderHierarchy to build destination paths.
/// </summary>
public class ImportPlanBuilder
{
    /// <summary>
    /// Enriches import decisions with target paths.
    /// Returns new ImportDecision records with TargetRelativePath filled in.
    /// </summary>
    public List<ImportDecision> BuildPlan(
        List<ImportDecision> decisions,
        string plansRelativePath)
    {
        var planned = new List<ImportDecision>(decisions.Count);

        foreach (var decision in decisions)
        {
            var targetPath = CalculateTargetPath(decision, plansRelativePath);

            planned.Add(decision with { TargetRelativePath = targetPath });
        }

        Log.Information("ImportPlanBuilder: {Count} Aktionen geplant", planned.Count);
        return planned;
    }

    private static string? CalculateTargetPath(
        ImportDecision decision,
        string plansRelativePath)
    {
        // No target for skipped or unknown files
        if (decision.Status is ImportStatus.SkipIdentical or ImportStatus.Unknown
            or ImportStatus.Conflict)
            return null;

        var profile = decision.File.Parsed.MatchedProfile;
        if (profile is null)
            return null;

        // Build path: plans/ + targetFolder/ + hierarchy levels/ + filename
        var parts = new List<string> { plansRelativePath };

        // Target folder from profile (e.g. "01 Planunterlagen", "Polierpläne")
        if (!string.IsNullOrEmpty(profile.TargetFolder))
            parts.Add(profile.TargetFolder);

        // Folder hierarchy levels (e.g. Geschoss, Haus)
        var extractedFields = decision.File.Parsed.ExtractedFields;
        foreach (var level in profile.FolderHierarchy)
        {
            if (extractedFields.TryGetValue(level.ToLowerInvariant(), out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        var targetDir = Path.Combine(parts.ToArray());
        var targetPath = Path.Combine(targetDir, decision.File.Parsed.FileName);

        return targetPath;
    }
}
