using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Orchestrates the 7-stage import analysis pipeline.
/// Coordinates: Scan → Fingerprint → Parse → ResolveContext → BuildIdentity → VersionDecision → Plan.
/// Does NOT execute file moves — only produces ImportDecisions for the preview UI.
/// </summary>
public class ImportWorkflowService
{
    private readonly ImportScanService _scan = new();
    private readonly FileFingerprintService _fingerprint = new();
    private readonly FileParseService _parse = new();
    private readonly ImportContextResolver _resolver = new();
    private readonly DocumentKeyBuilder _keyBuilder = new();
    private readonly RevisionDecisionService _decision = new();
    private readonly ImportPlanBuilder _planBuilder = new();
    private readonly IProfileManager _profileManager;
    private readonly PlanManagerDatabase _db;

    public ImportWorkflowService(IProfileManager profileManager, PlanManagerDatabase db)
    {
        _profileManager = profileManager;
        _db = db;
    }

    /// <summary>
    /// Runs the full 7-stage analysis pipeline.
    /// Returns ImportDecisions ready for the preview UI.
    /// </summary>
    public async Task<ImportAnalysisResult> AnalyzeAsync(
        string projectRootPath,
        string inboxRelativePath,
        string plansRelativePath,
        CancellationToken ct = default)
    {
        Log.Information("Import-Analyse gestartet fuer {Path}", projectRootPath);

        // 1. Scan — enumerate files in inbox
        var scanned = await _scan.ScanAsync(projectRootPath, inboxRelativePath, ct);
        if (scanned.Count == 0)
        {
            Log.Information("Keine Dateien im Eingang");
            return ImportAnalysisResult.Empty;
        }

        // 2. Fingerprint — compute MD5 hashes (bounded parallel)
        var fingerprinted = await _fingerprint.FingerprintAsync(scanned, projectRootPath, ct);

        // 3. Parse — split file names + match against profiles
        //    BPM-108 Phase C Teil 5 (Akzeptanzkriterium #17): Profile mit
        //    ProfileHealth != Valid (insb. MissingSegmentTypes) werden vom
        //    Recognizer-Match ausgeschlossen. Files landen damit bei "Unknown"
        //    statt mit fehlerhafter Identity importiert zu werden. Die Profile
        //    bleiben im Manager/Wizard sichtbar — User muss sie reparieren.
        var allProfiles = _profileManager.LoadAll(projectRootPath);
        var unhealthyProfiles = allProfiles
            .Where(p => p.Health != ProfileHealth.Valid)
            .ToList();
        var healthyProfiles = allProfiles
            .Where(p => p.Health == ProfileHealth.Valid)
            .ToList();

        if (unhealthyProfiles.Count > 0)
        {
            foreach (var p in unhealthyProfiles)
            {
                Log.Warning("BPM-108: Profil {Name} ({Id}) ausgeschlossen — Health={Health}, fehlende IDs: {Missing}",
                    p.DocumentTypeName, p.Id, p.Health,
                    string.Join(", ", p.MissingSegmentTypeIds));
            }
        }

        var parsed = _parse.ParseAll(fingerprinted, healthyProfiles);

        // 4. Resolve Context — folder path, stage, document type
        // 5. Build Identity — document_key from resolved fields
        var classified = new List<ClassifiedImportFile>(parsed.Count);
        foreach (var file in parsed)
        {
            ct.ThrowIfCancellationRequested();

            var context = _resolver.Resolve(file, file.RelativePath);
            var documentKey = _keyBuilder.Build(
                context.DocumentTypeId, file.ExtractedFields, file.MatchedProfile);

            // Extract revision token from parsed fields
            string? revisionToken = null;
            var revisionKind = RevisionKind.None;
            var revisionSource = IndexSourceType.None;

            if (file.ExtractedFields.TryGetValue("planindex", out var idx)
                && !string.IsNullOrWhiteSpace(idx))
            {
                revisionToken = idx;
                revisionSource = IndexSourceType.FileName;
                revisionKind = DetectRevisionKind(idx);
            }

            classified.Add(new ClassifiedImportFile(
                Parsed: file,
                DocumentTypeId: context.DocumentTypeId,
                DocumentTypeDisplayName: context.DocumentTypeDisplayName,
                PlanNumber: file.ExtractedFields.GetValueOrDefault("plannumber"),
                RevisionToken: revisionToken,
                RevisionKind: revisionKind,
                RevisionSource: revisionSource,
                Stage: context.Stage,
                IdentityFields: file.ExtractedFields,
                Evidence: context.Evidence));
        }

        // 6. Version Decision — apply 9-status decision matrix
        // Schema v2.0 (BPM-109.03): Lookup über plan_documents/plan_revisions statt altem Cache.
        var existingRevisions = _db.GetCurrentRevisionLookup();
        var decisions = _decision.Decide(classified, existingRevisions);

        // 7. Execution Plan — calculate target paths
        var planned = _planBuilder.BuildPlan(decisions, plansRelativePath);

        Log.Information("Import-Analyse abgeschlossen: {Total} Dateien, {New} neu, {Unknown} unbekannt, {Unhealthy} ausgeschlossene Profile",
            planned.Count,
            planned.Count(d => d.Status == ImportStatus.New),
            planned.Count(d => d.Status == ImportStatus.Unknown),
            unhealthyProfiles.Count);

        return new ImportAnalysisResult(planned, healthyProfiles, unhealthyProfiles);
    }

    private static RevisionKind DetectRevisionKind(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RevisionKind.None;

        var lower = token.ToLowerInvariant();
        if (lower is "vorabzug" or "vorab" or "va")
            return RevisionKind.DraftMarker;
        if (token.All(char.IsDigit))
            return RevisionKind.Numeric;
        if (token.All(char.IsLetter))
            return RevisionKind.Alphabetic;
        return RevisionKind.Unknown;
    }
}

/// <summary>
/// Result of the full import analysis pipeline.
/// </summary>
/// <remarks>
/// BPM-108 Phase C Teil 5: <see cref="UnhealthyProfiles"/> listet Profile, die wegen
/// <see cref="ProfileHealth.MissingSegmentTypes"/> vom Recognizer-Match ausgeschlossen
/// wurden. Die UI kann darauf einen "Reparieren"-Hinweis stuetzen.
/// </remarks>
public sealed record ImportAnalysisResult(
    List<ImportDecision> Decisions,
    List<RecognitionProfile> UsedProfiles,
    List<RecognitionProfile> UnhealthyProfiles)
{
    public static ImportAnalysisResult Empty => new([], [], []);

    public int TotalFiles => Decisions.Count;
    public int NewCount => Decisions.Count(d => d.Status == ImportStatus.New);
    public int SkipCount => Decisions.Count(d => d.Status == ImportStatus.SkipIdentical);
    public int UpdateCount => Decisions.Count(d => d.Status == ImportStatus.UpdateNewerIndex);
    public int UnknownCount => Decisions.Count(d => d.Status == ImportStatus.Unknown);
    public int ConflictCount => Decisions.Count(d => d.Status == ImportStatus.Conflict);
    public int WarningCount => Decisions.Count(d =>
        d.Status is ImportStatus.ChangedSameIndex or ImportStatus.OlderRevision);

    /// <summary>Anzahl Profile mit <c>ProfileHealth.MissingSegmentTypes</c>.</summary>
    public int UnhealthyProfileCount => UnhealthyProfiles.Count;
}
