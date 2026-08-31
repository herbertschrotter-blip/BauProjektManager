using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// ManualFirstCapture-Workflow (BPM-111.03, ADR-059 Strategie B):
/// Scan -> MD5 -> Lightweight-Kandidaten -> deterministisches Matching gegen
/// bekannte plan_documents -> Buckets A/B/C/D.
///
/// Reine Analyse-/Service-Schicht: persistiert NICHTS, verschiebt NICHTS
/// ("B entscheidet, A schlaegt vor" — Pending/Journal/Import = BPM-111.04).
/// Bewusst PROFIL-UNABHAENGIG: Matching laeuft ueber Plannummern-Kandidaten
/// und MD5, nicht ueber Recognizer-Profile.
/// </summary>
public class ManualFirstCaptureService
{
    private readonly ImportScanService _scan;
    private readonly FileFingerprintService _fingerprint;
    private readonly LightweightPlanExtractor _extractor = new();
    private static readonly IPlanValueNormalizer _normalizer = new PlanValueNormalizer();
    private readonly PlanManagerDatabase _db;

    // BPM-112.01: Scanner/Fingerprint laufen ueber die FS-Ports. Default =
    // LocalFileSystem (echte Disk) — Tests koennen den FakeFileStore reichen.
    public ManualFirstCaptureService(
        PlanManagerDatabase db,
        IFileSystemReader? reader = null, IPathService? path = null)
    {
        _db = db;
        var fs = new LocalFileSystem();
        var effectiveReader = reader ?? fs;
        var effectivePath = path ?? fs;
        _scan = new ImportScanService(effectiveReader, effectivePath);
        _fingerprint = new FileFingerprintService(effectiveReader, effectivePath);
    }

    /// <summary>
    /// Analysiert den Eingang und klassifiziert jede Datei in Bucket A/B/C/D.
    /// </summary>
    public async Task<ManualCaptureResult> AnalyzeAsync(
        string projectRootPath,
        string inboxRelativePath,
        CancellationToken ct = default)
    {
        Log.Information("ManualFirstCapture-Analyse gestartet fuer {Path}", projectRootPath);

        var scanned = await _scan.ScanAsync(projectRootPath, inboxRelativePath, ct);
        if (scanned.Count == 0)
        {
            Log.Information("Keine Dateien im Eingang");
            return ManualCaptureResult.Empty;
        }

        var fingerprinted = await _fingerprint.FingerprintAsync(scanned, projectRootPath, ct);

        var knownDocs = _db.GetCurrentDocumentLookup();
        var knownMd5 = _db.GetKnownMd5Lookup();

        var result = Classify(fingerprinted, knownDocs, knownMd5, _extractor, _normalizer);

        Log.Information(
            "ManualFirstCapture: {Total} Dateien — {Dup} Dubletten, {Upd} Update-Vorschlaege, {New} Erstaufnahme, {Conf} Konflikte",
            result.TotalFiles, result.DuplicateCount, result.UpdateProposalCount,
            result.NewCaptureCount, result.ConflictCount);
        return result;
    }

    /// <summary>
    /// Reine Klassifikations-Logik (testbar ohne DB/Dateisystem).
    /// Bucket-Reihenfolge: A (MD5) vor B/D (Plannummern-Match) vor C (Rest).
    /// </summary>
    public static ManualCaptureResult Classify(
        List<FingerprintResult> fingerprinted,
        IReadOnlyList<KnownPlanDocument> knownDocuments,
        IReadOnlyDictionary<string, string> knownMd5ToDocumentKey,
        LightweightPlanExtractor extractor,
        IPlanValueNormalizer normalizer)
    {
        var byNumber = BuildNumberLookup(knownDocuments, normalizer);
        var items = new List<CaptureItem>(fingerprinted.Count);

        foreach (var fp in fingerprinted)
        {
            var file = fp.File;
            var candidates = extractor.ExtractCandidates(file.Scan.FileName);

            // Bucket A — exakte Dublette per MD5 (Fingerprint-Invariante)
            if (!string.IsNullOrEmpty(file.Md5)
                && knownMd5ToDocumentKey.TryGetValue(file.Md5, out var dupKey))
            {
                var dupMatch = knownDocuments.FirstOrDefault(d => d.DocumentKey == dupKey);
                items.Add(new CaptureItem(file, candidates, CaptureBucket.Duplicate,
                    dupMatch, $"Inhalt bereits im Bestand ({dupKey})"));
                continue;
            }

            // Bucket B/C/D — deterministisches Matching über die Plannummer
            var (bucket, match, reason) = MatchByNumber(
                candidates.PlanNumber, candidates.Index, byNumber, normalizer);
            items.Add(new CaptureItem(file, candidates, bucket, match, reason));
        }

        return new ManualCaptureResult(items);
    }

    /// <summary>
    /// Einzel-Re-Match nach Panel-Edit der Plannummer/Index (BPM-111.06 Slice A2):
    /// klassifiziert eine bearbeitete Identität neu gegen die bekannten Dokumente.
    /// Nur B/C/D — der Dubletten-Bucket A hängt an MD5 und ändert sich durch einen
    /// Nummern-/Index-Edit nicht.
    /// </summary>
    public (CaptureBucket Bucket, KnownPlanDocument? Match, string? Reason) RematchByNumber(
        string? planNumber, string? index)
    {
        var byNumber = BuildNumberLookup(_db.GetCurrentDocumentLookup(), _normalizer);
        return MatchByNumber(planNumber, index, byNumber, _normalizer);
    }

    /// <summary>
    /// Pure Matching-Logik einer einzelnen Identität gegen die bekannten Dokumente
    /// (Bucket B/C/D). Testbar ohne DB/Dateisystem — Kern des Einzel-Re-Match und
    /// der Batch-Klassifikation.
    /// </summary>
    public static (CaptureBucket Bucket, KnownPlanDocument? Match, string? Reason) MatchByNumber(
        string? planNumber, string? index,
        IReadOnlyDictionary<string, List<KnownPlanDocument>> byNumber,
        IPlanValueNormalizer normalizer)
    {
        // Ohne Plannummern-Kandidat oder ohne Treffer -> Bucket C (Erstaufnahme/Radial)
        if (planNumber is null
            || !byNumber.TryGetValue(normalizer.NormalizeForMatch(planNumber), out var matches))
        {
            return (CaptureBucket.NewCapture, null, null);
        }

        // Bucket D — Plannummer in mehreren Dokumenten (z. B. über Dokumenttypen)
        if (matches.Count > 1)
        {
            return (CaptureBucket.Conflict, null,
                $"Plannummer {planNumber} in {matches.Count} bekannten Dokumenten — Auswahl noetig");
        }

        var match = matches[0];
        var newIdx = NormalizeIndex(index, normalizer);
        var curIdx = NormalizeIndex(match.CurrentIndex, normalizer);

        // Bucket D — gleicher Index, aber anderer Inhalt (Matrix: CHANGED_SAME_INDEX)
        if (newIdx == curIdx)
        {
            return (CaptureBucket.Conflict, match,
                $"Gleicher Index wie aktuelle Revision ({match.CurrentIndex ?? "Erstausgabe"}), aber anderer Inhalt — pruefen");
        }

        // Bucket B — bekannter Plan, anderer Index (Matrix: UPDATE_NEWER_INDEX / OLDER_REVISION)
        var older = IsOlderIndex(index, match.CurrentIndex);
        string? reason = older switch
        {
            true when index is null =>
                $"Achtung: Datei ohne Index, aktuelle Revision ist {match.CurrentIndex} (OLDER_REVISION)",
            true =>
                $"Achtung: Index {index} ist NIEDRIGER als aktuelle Revision {match.CurrentIndex} (OLDER_REVISION)",
            _ => null
        };
        return (CaptureBucket.UpdateProposal, match, reason);
    }

    /// <summary>Normalisierte Plannummer -> alle bekannten Dokumente mit dieser Nummer.</summary>
    private static Dictionary<string, List<KnownPlanDocument>> BuildNumberLookup(
        IReadOnlyList<KnownPlanDocument> knownDocuments, IPlanValueNormalizer normalizer)
        => knownDocuments
            .Where(d => !string.IsNullOrWhiteSpace(d.PlanNumber))
            .GroupBy(d => normalizer.NormalizeForMatch(d.PlanNumber))
            .ToDictionary(g => g.Key, g => g.ToList());

    private static string? NormalizeIndex(string? index, IPlanValueNormalizer normalizer) =>
        string.IsNullOrWhiteSpace(index) ? null : normalizer.NormalizeForMatch(index);

    /// <summary>
    /// Vergleicht zwei Index-Tokens wenn beide vergleichbar sind:
    /// beide numerisch (02 &lt; 03) oder beide einbuchstabig (B &lt; C).
    /// NULL wenn nicht vergleichbar (kein falscher Alarm).
    /// </summary>
    private static bool? IsOlderIndex(string? newIndex, string? currentIndex)
    {
        if (string.IsNullOrWhiteSpace(newIndex) || string.IsNullOrWhiteSpace(currentIndex))
            return newIndex is null && currentIndex is not null ? true : null;

        if (newIndex.All(char.IsDigit) && currentIndex.All(char.IsDigit))
            return int.Parse(newIndex) < int.Parse(currentIndex);

        if (newIndex.Length == 1 && currentIndex.Length == 1
            && char.IsLetter(newIndex[0]) && char.IsLetter(currentIndex[0]))
            return char.ToUpperInvariant(newIndex[0]) < char.ToUpperInvariant(currentIndex[0]);

        return null;
    }
}
