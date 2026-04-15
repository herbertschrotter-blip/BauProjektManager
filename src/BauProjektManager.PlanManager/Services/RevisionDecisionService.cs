using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Applies the 9-status decision matrix to classified files.
/// Step 6 of the 7-stage analysis pipeline.
/// Compares against existing plan_revisions in planmanager.db.
/// For V1: no DB yet — all files are treated as NEW or UNKNOWN.
/// </summary>
public class RevisionDecisionService
{
    /// <summary>
    /// Decides import status for each classified file.
    /// existingRevisions: document_key → (latestIndex, md5, revisionId) from planmanager.db.
    /// Pass empty dictionary for first import / no DB.
    /// </summary>
    public List<ImportDecision> Decide(
        List<ClassifiedImportFile> classified,
        Dictionary<string, ExistingRevision> existingRevisions)
    {
        var decisions = new List<ImportDecision>(classified.Count);

        foreach (var file in classified)
        {
            var decision = DecideSingle(file, existingRevisions);
            decisions.Add(decision);
        }

        Log.Information("RevisionDecision: {New} neu, {Skip} identisch, {Update} update, {Unknown} unbekannt",
            decisions.Count(d => d.Status == ImportStatus.New),
            decisions.Count(d => d.Status == ImportStatus.SkipIdentical),
            decisions.Count(d => d.Status == ImportStatus.UpdateNewerIndex),
            decisions.Count(d => d.Status == ImportStatus.Unknown));
        return decisions;
    }

    private static ImportDecision DecideSingle(
        ClassifiedImportFile file,
        Dictionary<string, ExistingRevision> existing)
    {
        var reasons = new List<string>();
        var documentKey = file.Parsed.MatchedProfile is not null
            ? string.Join("|", file.IdentityFields.Values)
            : null;

        // No document key → UNKNOWN
        if (string.IsNullOrEmpty(documentKey))
        {
            reasons.Add("Kein Profil erkannt oder document_key nicht bildbar");
            return new ImportDecision(file, ImportStatus.Unknown,
                null, null, null, reasons);
        }

        // Not in DB → NEW
        if (!existing.TryGetValue(documentKey, out var existingRev))
        {
            reasons.Add("Neues Dokument — nicht im Bestand");
            return new ImportDecision(file, ImportStatus.New,
                documentKey, null, null, reasons);
        }

        // Same MD5 → SKIP_IDENTICAL
        if (file.Parsed.Md5 == existingRev.Md5)
        {
            reasons.Add("Identisch — gleicher MD5-Hash");
            return new ImportDecision(file, ImportStatus.SkipIdentical,
                documentKey, existingRev.RevisionId, null, reasons);
        }

        // Has revision token
        if (!string.IsNullOrEmpty(file.RevisionToken))
        {
            if (string.IsNullOrEmpty(existingRev.LatestIndex))
            {
                // Previously no index, now one → LEARN_INDEX
                reasons.Add($"Index '{file.RevisionToken}' erkannt — bisher kein Index");
                return new ImportDecision(file, ImportStatus.LearnIndex,
                    documentKey, existingRev.RevisionId, null, reasons);
            }

            var cmp = string.Compare(file.RevisionToken, existingRev.LatestIndex,
                StringComparison.OrdinalIgnoreCase);

            if (cmp > 0)
            {
                // Higher index → UPDATE
                reasons.Add($"Neuer Index '{file.RevisionToken}' > '{existingRev.LatestIndex}'");
                return new ImportDecision(file, ImportStatus.UpdateNewerIndex,
                    documentKey, existingRev.RevisionId, null, reasons);
            }

            if (cmp < 0)
            {
                // Lower index → OLDER_REVISION (warning)
                reasons.Add($"Älterer Index '{file.RevisionToken}' < '{existingRev.LatestIndex}'");
                return new ImportDecision(file, ImportStatus.OlderRevision,
                    documentKey, existingRev.RevisionId, null, reasons);
            }

            // Same index, different MD5 → CHANGED_SAME_INDEX (warning)
            reasons.Add($"Gleicher Index '{file.RevisionToken}' aber anderer MD5");
            return new ImportDecision(file, ImportStatus.ChangedSameIndex,
                documentKey, existingRev.RevisionId, null, reasons);
        }

        // No revision token, different MD5 → CHANGED_NO_INDEX
        reasons.Add("Geändert — anderer MD5, kein Index");
        return new ImportDecision(file, ImportStatus.ChangedNoIndex,
            documentKey, existingRev.RevisionId, null, reasons);
    }
}

/// <summary>
/// Represents an existing revision in planmanager.db for comparison.
/// </summary>
public sealed record ExistingRevision(
    string RevisionId,
    string? LatestIndex,
    string Md5);
