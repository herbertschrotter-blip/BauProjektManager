using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Bestaetigt Pending Assignments (BPM-111.04): baut aus den vom User
/// bestaetigten Zuordnungen ImportDecisions und schickt sie durch die
/// BESTEHENDE Execute-Strecke (<see cref="ImportExecutionService"/>) —
/// damit gelten alle Invarianten unveraendert: Journal VOR Dateioperationen,
/// Supersede-Logik, Drei-Ebenen-Schreibpfad (Schema v2.0).
/// </summary>
public class CaptureConfirmService
{
    private static readonly IPlanValueNormalizer _normalizer = new PlanValueNormalizer();
    private readonly ImportExecutionService _execution;
    private readonly PendingAssignmentStore _store;

    // BPM-120 T1 (ADR-064/AK 3): Executor kommt per Constructor Injection —
    // eine Instanz-Welt, fault-faehig testbar. Die statischen Mapper unten
    // nutzen weiterhin System.IO.Path (pure Stringoperationen, kein Disk-Zugriff).
    public CaptureConfirmService(ImportExecutionService execution, PendingAssignmentStore store)
    {
        _execution = execution;
        _store = store;
    }

    /// <summary>
    /// Fuehrt den Import aller Pending Assignments aus und leert den Store
    /// bei Erfolg. Gibt das Execute-Ergebnis zurueck (Journal-basiert).
    /// </summary>
    public ImportExecutionResult ConfirmAll(string projectRootPath, string inboxRelativePath)
    {
        var pending = _store.Snapshot();
        if (pending.Count == 0)
        {
            Log.Information("Bestaetigung: keine Pending Assignments");
            return new ImportExecutionResult(0, 0, 0, []);
        }

        var decisions = BuildDecisions(pending, _normalizer);
        Log.Information("Bestaetigung: {Count} Pending Assignments werden importiert", decisions.Count);

        var result = _execution.Execute(decisions, projectRootPath, inboxRelativePath);

        if (result.Failed == 0)
            _store.Clear();
        else
            Log.Warning("Bestaetigung: {Failed} Aktionen fehlgeschlagen — Pending bleibt erhalten",
                result.Failed);

        return result;
    }

    /// <summary>
    /// Reine Mapping-Logik Pending -> ImportDecision (testbar ohne DB/Dateisystem).
    /// Update-Uebernahmen (Bucket B) verwenden document_key + Zielordner des
    /// bekannten Dokuments; Erstaufnahmen bekommen einen manuellen Key.
    /// </summary>
    public static List<ImportDecision> BuildDecisions(
        IReadOnlyList<PendingAssignment> pending, IPlanValueNormalizer normalizer)
    {
        // 111.07 Slice A: PDFs zuerst (stabil) — bei PDF/DWG-Paaren mit gleichem
        // document_key legt so IMMER die PDF die Revision an (is_primary) und
        // die DWG dockt in der Execution als Zusatzdatei an (FileLinked-Zweig).
        var ordered = pending
            .OrderBy(p => p.File.Scan.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();
        var decisions = new List<ImportDecision>(ordered.Count);

        foreach (var p in ordered)
        {
            var scan = p.File.Scan;
            var isUpdate = p.Match is not null;
            var documentKey = isUpdate ? p.Match!.DocumentKey : BuildManualDocumentKey(p, normalizer);
            var targetDir = isUpdate ? p.Match!.RelativeDirectory : p.TargetRelativeDirectory;
            var targetPath = Path.Combine(targetDir, scan.FileName);

            // Bestaetigte Werte als ExtractedFields (Keys = SegmentTypeIds, BPM-110)
            var fields = new Dictionary<string, string>();
            if (p.PlanNumber is not null) fields[SegmentTypeIds.PlanNumber] = p.PlanNumber;
            if (p.Index is not null) fields[SegmentTypeIds.PlanIndex] = p.Index;
            if (p.BuildingPart is not null) fields[SegmentTypeIds.Bauteil] = p.BuildingPart;
            if (p.Level is not null) fields[SegmentTypeIds.Geschoss] = p.Level;

            var parsed = new ParsedImportFile(
                RelativePath: scan.RelativePath,
                FileName: scan.FileName,
                Extension: scan.Extension,
                FileSize: scan.FileSize,
                Md5: p.File.Md5,
                MatchedProfile: null,
                ExtractedFields: fields,
                Confidence: ParseConfidence.High,
                Warnings: []);

            var classified = new ClassifiedImportFile(
                Parsed: parsed,
                DocumentTypeId: p.DocumentTypeId,
                DocumentTypeDisplayName: p.DocumentTypeName,
                DocumentKey: documentKey,
                PlanNumber: p.PlanNumber,
                RevisionToken: p.Index,
                RevisionKind: RevisionKindDetector.Detect(p.Index),
                RevisionSource: p.Index is null ? IndexSourceType.None : IndexSourceType.FileName,
                Stage: ImportStage.Unknown,
                IdentityFields: fields,
                Evidence: [],
                Title: p.Title,
                ChangeNote: p.ChangeNote,
                ReleasedAt: p.ReleasedAt,
                AssignedSegments: p.AssignedSegments);

            decisions.Add(new ImportDecision(
                File: classified,
                Status: isUpdate ? ImportStatus.UpdateNewerIndex : ImportStatus.New,
                DocumentKey: documentKey,
                ExistingRevisionId: p.Match?.CurrentRevisionId,
                TargetRelativePath: targetPath,
                Reasons: [isUpdate
                    ? $"Update-Uebernahme (Index {p.Index ?? "—"}, bisher {p.Match!.CurrentIndex ?? "Erstausgabe"})"
                    : "Manuelle Erstaufnahme (ManualConfirmed)"]));
        }

        return decisions;
    }

    /// <summary>
    /// Manueller document_key fuer Erstaufnahmen: Dokumenttyp + Plannummer +
    /// Bauteil/Kategorie [+ Geschoss]. Bei nummernlosen Typen (Protokolle)
    /// ersetzt der Dateiname die Plannummer (Eindeutigkeit).
    /// HINWEIS ADR-059: Ziel ist ID-basiert (building_part_id etc.) — solange
    /// die Stammdaten-IDs fehlen (ADR-Addendum offen, siehe 111.03-Notiz),
    /// werden normalisierte Namen verwendet.
    /// </summary>
    public static string BuildManualDocumentKey(
        PendingAssignment p, IPlanValueNormalizer normalizer)
    {
        var parts = new List<string> { normalizer.NormalizeForKey(p.DocumentTypeId) };

        var number = p.PlanNumber
            ?? Path.GetFileNameWithoutExtension(p.File.Scan.FileName);
        parts.Add(normalizer.NormalizeForKey(number));

        if (!string.IsNullOrWhiteSpace(p.BuildingPart))
            parts.Add(normalizer.NormalizeForKey(p.BuildingPart));
        if (!string.IsNullOrWhiteSpace(p.Level))
            parts.Add(normalizer.NormalizeForKey(p.Level));

        return string.Join("|", parts);
    }
}
