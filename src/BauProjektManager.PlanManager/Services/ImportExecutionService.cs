using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Executes the import plan: moves files, creates _Archiv/, writes journal.
/// Invariant: Journal is written BEFORE files are moved (pending → completed/failed).
/// BPM-120 T1 (ADR-060/064): alle Datei-/Pfadoperationen laufen ueber die
/// FS-Ports — testbar mit fault-faehigem FakeFileStore.
/// </summary>
public class ImportExecutionService
{
    private static readonly IPlanValueNormalizer _normalizer = new PlanValueNormalizer();
    private readonly PlanManagerDatabase _db;
    private readonly IIdGenerator _idGenerator;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    public ImportExecutionService(
        PlanManagerDatabase db, IIdGenerator idGenerator,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _db = db;
        _idGenerator = idGenerator;
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <summary>
    /// Executes the import: journal first, then move files, then update DB.
    /// BPM-120 T2 (ADR-064 P.2): ZWEIPHASIG — Phase 1 plant alle Actions
    /// (inkl. deterministischem archive_path) und journalisiert Header + ALLE
    /// Actions vor der ersten Mutation; Phase 2 fuehrt aus. Bestaetigte
    /// MD5-Dubletten laufen als echte skipDuplicate-Actions (Bucket A, P.7) —
    /// journalisiert + recovery-faehig, bewusst NICHT undo-bar.
    /// </summary>
    public ImportExecutionResult Execute(
        List<ImportDecision> decisions,
        string projectRootPath,
        string inboxRelativePath)
    {
        var actionable = decisions
            .Where(d => d.Status is ImportStatus.New or ImportStatus.UpdateNewerIndex
                or ImportStatus.ChangedNoIndex or ImportStatus.LearnIndex)
            .ToList();
        var duplicates = decisions
            .Where(d => d.Status == ImportStatus.SkipIdentical)
            .ToList();

        int succeeded = 0;
        int failed = 0;
        int skipped = 0;
        var errors = new List<string>();

        // Planbarkeits-Check VOR der Journalisierung: ohne Zielpfad keine Action
        // (zaehlt wie bisher als Fehler, mutiert nichts, journalisiert nichts).
        var plannable = new List<ImportDecision>();
        foreach (var decision in actionable)
        {
            if (string.IsNullOrEmpty(decision.TargetRelativePath))
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: Kein Zielpfad berechnet");
                continue;
            }
            // R01-007.02: ohne MD5 kein Import (Fingerprint-Pflicht, PlanManager.md Invarianten)
            if (string.IsNullOrEmpty(decision.File.Parsed.Md5))
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: Kein MD5 — Datei nicht importierbar");
                continue;
            }
            plannable.Add(decision);
        }

        if (plannable.Count == 0 && duplicates.Count == 0)
        {
            Log.Information("Import: keine Aktionen auszuführen");
            return new ImportExecutionResult(0, failed, 0, errors);
        }

        Log.Information("Import-Ausführung: {Count} Aktionen, {Skips} Dubletten",
            plannable.Count, duplicates.Count);

        // ── Phase 1: Journal-Header + ALLE Actions VOR der ersten Mutation (AK 4) ──
        var importId = _db.CreateImportJournal(
            inboxRelativePath, plannable.Count + duplicates.Count, profileId: null);

        var planned = new List<(ImportDecision Decision, string ActionId, string? ArchiveRelPath)>();
        var archivedOriginals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 0;
        foreach (var decision in plannable)
        {
            var targetRelPath = decision.TargetRelativePath!;
            var actionType = decision.Status switch
            {
                ImportStatus.New => "new",
                ImportStatus.UpdateNewerIndex => "indexUpdate",
                ImportStatus.ChangedNoIndex => "changed",
                ImportStatus.LearnIndex => "learnIndex",
                _ => "unknown"
            };

            // Deterministischer Archivpfad (AK 5): VOR der ersten Mutation
            // festgelegt und journalisiert — kein ad-hoc-Name bei der Ausfuehrung.
            string? archiveRelPath = null;
            if (decision.Status == ImportStatus.UpdateNewerIndex)
            {
                var (archive, blocked) = PlanArchiveTarget(decision, projectRootPath, targetRelPath, archivedOriginals);
                if (blocked is not null)
                {
                    failed++;
                    errors.Add($"{decision.File.Parsed.FileName}: {blocked}");
                    continue;
                }
                archiveRelPath = archive;
            }

            var actionId = _db.InsertImportAction(
                importId, order++, actionType,
                decision.DocumentKey,
                decision.File.PlanNumber ?? "",
                decision.File.RevisionToken,
                oldIndex: null,
                decision.File.Parsed.RelativePath,
                targetRelPath,
                archiveRelPath,
                decision.File.Parsed.Md5,
                decision.File.Parsed.FileSize,
                decision.File.DocumentTypeId,
                decision.File.DocumentTypeDisplayName,
                decision.File.BuildingPartId,
                decision.File.BuildingLevelId);
            planned.Add((decision, actionId, archiveRelPath));
        }

        var plannedSkips = new List<(ImportDecision Decision, string ActionId)>();
        foreach (var dup in duplicates)
        {
            var actionId = _db.InsertImportAction(
                importId, order++, "skipDuplicate",
                dup.DocumentKey,
                dup.File.PlanNumber ?? "",
                dup.File.RevisionToken,
                oldIndex: null,
                dup.File.Parsed.RelativePath,
                destinationPath: null,
                archivePath: null,
                dup.File.Parsed.Md5,
                dup.File.Parsed.FileSize);
            plannedSkips.Add((dup, actionId));
        }

        // ── Phase 2: Ausfuehrung ──
        foreach (var (decision, actionId, archiveRelPath) in planned)
        {
            var actionResult = ExecuteSingleAction(
                decision, projectRootPath, importId, actionId, archiveRelPath);
            if (actionResult.Success)
                succeeded++;
            else
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: {actionResult.Error}");
            }
        }

        foreach (var (dup, actionId) in plannedSkips)
        {
            var skipResult = ExecuteSkipDuplicate(dup, projectRootPath, actionId);
            if (skipResult.Success)
                skipped++;
            else
            {
                failed++;
                errors.Add($"{dup.File.Parsed.FileName}: {skipResult.Error}");
            }
        }

        // Journal abschliessen (T6/AK 13): mit fehlgeschlagenen Actions bleibt
        // der Import 'pending' — recovery-pflichtig, blockiert neuen Confirm.
        // 'failed' ist erst nach vollem Rollback/bewusstem Cleanup terminal.
        if (failed == 0)
            _db.CompleteImportJournal(importId, success: true);
        else
            _db.SetImportJournalError(importId,
                $"{failed} Aktionen fehlgeschlagen — Recovery erforderlich");

        Log.Information("Import abgeschlossen: {Ok} OK, {Fail} Fehler, {Skip} übersprungen",
            succeeded, failed, skipped);

        return new ImportExecutionResult(succeeded, failed, skipped, errors);
    }

    private ActionResult ExecuteSingleAction(
        ImportDecision decision, string projectRootPath,
        string importId, string actionId, string? archiveRelPath)
    {
        var sourcePath = _path.Combine(projectRootPath, decision.File.Parsed.RelativePath);
        var targetRelPath = decision.TargetRelativePath!;
        var targetPath = _path.Combine(projectRootPath, targetRelPath);

        try
        {
            // R01-006.02 (ADR-064 P.1/P.4): fehlt die Eingangsdatei (zwischen Analyse
            // und Bestaetigen verschwunden), VOR jeder Disk-Mutation abbrechen — kein
            // Archivieren des Vorgaengers, kein DB-Eintrag ohne Datei. Die Action wird
            // failed, das Journal bleibt pending (Recovery entscheidet).
            if (!_reader.FileExists(sourcePath))
                throw new FileNotFoundException(
                    $"RecoveryConflict: Quelldatei fehlt ({decision.File.Parsed.RelativePath})");

            // Create target directory
            var targetDir = _path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                _writer.CreateDirectory(targetDir);

            // --- EIN Zeitstempel pro Aktion (Zeitreise-Konsistenz, BPM-109.04):
            //     superseded_at der alten == current_from der neuen → kein Loch, keine Überlappung. ---
            var actionTime = DateTime.UtcNow.ToString("o");

            // --- Disk-Phase komplett VOR der DB-Phase (BPM-120 T4, ADR-064):
            //     Archiv an den journalisierten Pfad (AK 5) + Publish via .bpm_tmp (T3).
            //     Crash nach der Disk-Phase: Recovery schreibt die DB nach (AK 9). ---
            if (decision.Status == ImportStatus.UpdateNewerIndex && archiveRelPath is not null)
                ArchiveExistingFile(
                    _path.Combine(projectRootPath, ArchivePathScheme.OriginalOf(_path, archiveRelPath)),
                    _path.Combine(projectRootPath, archiveRelPath));

            PublishIncomingFile(sourcePath, targetPath);
            Log.Information("Datei verschoben: {Source} → {Target}",
                decision.File.Parsed.FileName, targetRelPath);

            // --- DB-Phase (T4/AK 10): ALLE fachlichen Writes + action_status =
            //     completed in DERSELBEN SQLite-Transaction. Wirft ein Write,
            //     rollt alles zurueck — weder partielle Aenderungen noch eine
            //     completed-Action bleiben stehen. ---
            _db.ExecuteInTransaction(() =>
                ApplyActionToDatabase(decision, importId, actionId, targetRelPath, actionTime, archiveRelPath));

            return new ActionResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import-Aktion fehlgeschlagen: {File}", decision.File.Parsed.FileName);
            _db.CompleteImportAction(actionId, false, ex.Message);
            return new ActionResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Recovery Forward einer journalisierten Action (BPM-120 T5, ADR-064 P.4):
    /// DERSELBE Disk- und DB-Apply-Pfad wie der normale Import, idempotent aus
    /// jedem zulaessigen Zwischenzustand (AK 8/9/11/12). Liefert null bei
    /// Erfolg, sonst den Fehlertext (Recovery-Fehlermodell).
    /// </summary>
    public string? RecoverActionForward(
        ImportActionRow action, string projectRootPath, string importId)
    {
        try
        {
            var targetRelPath = action.DestinationPath!;
            var targetAbs = _path.Combine(projectRootPath, targetRelPath);
            var sourceAbs = _path.Combine(projectRootPath, action.SourcePath);
            var tmpAbs = targetAbs + TmpSuffix;

            var targetDir = _path.GetDirectoryName(targetAbs);
            if (!string.IsNullOrEmpty(targetDir))
                _writer.CreateDirectory(targetDir);

            // ── Disk-Forward (idempotent) ──
            // AK 8: Vorgaenger nur archivieren, wenn er noch am Ziel liegt, die
            // neue Datei noch aussteht (source/tmp) UND das Archiv fehlt —
            // nie eine zweite Archivkopie, nie Verlust des Vorgaengers.
            if (action.ArchivePath is not null)
            {
                var archiveAbs = _path.Combine(projectRootPath, action.ArchivePath);
                // R01-007.03: der Vorgaenger liegt an SEINEM Pfad (ggf. anderer Name als das Ziel)
                var originalAbs = _path.Combine(projectRootPath,
                    ArchivePathScheme.OriginalOf(_path, action.ArchivePath));
                // R01-006.02: Archiv fehlt, Ziel liegt, aber weder Quelle noch tmp —
                // am Ziel liegt dann der VORGAENGER, nicht die neue Datei (Quelle vor
                // der ersten Mutation verschwunden). Nie als neue Revision uebernehmen.
                if (!_reader.FileExists(archiveAbs) && _reader.FileExists(originalAbs)
                    && !_reader.FileExists(sourceAbs) && !_reader.FileExists(tmpAbs))
                    return $"RecoveryConflict: Quelldatei fehlt, der Vorgaenger liegt noch an seinem Platz ({action.SourcePath})";
                if (!_reader.FileExists(archiveAbs)
                    && _reader.FileExists(originalAbs)
                    && (_reader.FileExists(sourceAbs) || _reader.FileExists(tmpAbs)))
                    ArchiveExistingFile(originalAbs, archiveAbs);
            }

            if (_reader.FileExists(sourceAbs))
                PublishIncomingFile(sourceAbs, targetAbs);
            else if (_reader.FileExists(tmpAbs))
                // Crash zwischen tmp-Move und finalem Rename (T3): Rename nachholen.
                WithLockRetry(() => _writer.MoveFile(tmpAbs, targetAbs, overwrite: true));
            else if (!_reader.FileExists(targetAbs))
                return $"RecoveryConflict: Datei weder an Quelle noch am Ziel ({action.SourcePath})";
            // Datei liegt am Ziel (AK 9): nicht erneut verschieben.

            // ── DB-Forward: gemeinsamer Apply-Pfad (AK 9/11/12) ──
            // Actions ohne journalisierten document_key (Altbestand/Test-Seeds)
            // koennen nur den Datei-Endzustand herstellen — Status finalisieren.
            var actionTime = DateTime.UtcNow.ToString("o");
            if (action.DocumentKey is null)
            {
                _db.CompleteImportAction(action.Id, true);
                return null;
            }

            var decision = BuildRecoveryDecision(action, targetRelPath);
            _db.ExecuteInTransaction(() =>
                ApplyActionToDatabase(decision, importId, action.Id, targetRelPath, actionTime, action.ArchivePath));
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Recovery Forward fehlgeschlagen: {Source}", action.SourcePath);
            return ex.Message;
        }
    }

    /// <summary>
    /// Rekonstruiert die fachliche Decision aus dem Journal (T5). Nicht
    /// journalisierte BPM-118-Metadaten (Title/ChangeNote/ReleasedAt/Segmente)
    /// sind nach einem Crash bewusst nicht wiederherstellbar — der definierte
    /// Recovery-Endzustand ist die Kern-Struktur (Document/Revision/File/Events).
    /// </summary>
    private ImportDecision BuildRecoveryDecision(ImportActionRow action, string targetRelPath)
    {
        var fileName = _path.GetFileName(targetRelPath);
        var parsed = new ParsedImportFile(
            RelativePath: action.SourcePath,
            FileName: fileName,
            Extension: _path.GetExtension(fileName),
            FileSize: action.FileSize ?? 0,
            Md5: action.Md5 ?? "",
            MatchedProfile: null,
            ExtractedFields: new Dictionary<string, string>(),
            Confidence: ParseConfidence.High,
            Warnings: []);
        var classified = new ClassifiedImportFile(
            Parsed: parsed,
            DocumentTypeId: action.DocumentTypeId,
            DocumentTypeDisplayName: action.DocumentType,
            DocumentKey: action.DocumentKey,
            PlanNumber: action.PlanNumber,
            RevisionToken: action.PlanIndex,
            RevisionKind: RevisionKindDetector.Detect(action.PlanIndex),
            RevisionSource: action.PlanIndex is null ? IndexSourceType.None : IndexSourceType.FileName,
            Stage: ImportStage.Unknown,
            IdentityFields: new Dictionary<string, string>(),
            Evidence: [],
            BuildingPartId: action.BuildingPartId,
            BuildingLevelId: action.BuildingLevelId);
        var status = action.ActionType switch
        {
            "indexUpdate" => ImportStatus.UpdateNewerIndex,
            "changed" => ImportStatus.ChangedNoIndex,
            "learnIndex" => ImportStatus.LearnIndex,
            _ => ImportStatus.New
        };
        return new ImportDecision(
            File: classified,
            Status: status,
            DocumentKey: action.DocumentKey,
            ExistingRevisionId: null,
            TargetRelativePath: targetRelPath,
            Reasons: ["Recovery Forward (gemeinsamer Apply-Pfad, T5)"]);
    }

    /// <summary>
    /// Fachlicher DB-Apply einer Action (BPM-120 T4): Supersede + Document/
    /// Revision/File/Events/Segmente + Action-Abschluss. Laeuft IMMER innerhalb
    /// einer Transaction (siehe Aufrufer) und ist idempotent: eine bereits
    /// angewandte Action (Datei haengt schon an der current-Revision) schreibt
    /// nichts erneut, sondern finalisiert nur den Status — Basis fuer AK 11/12;
    /// Recovery Forward nutzt ab T5 denselben Pfad.
    /// </summary>
    private void ApplyActionToDatabase(
        ImportDecision decision, string importId, string actionId,
        string targetRelPath, string actionTime, string? archiveRelPath)
    {
        // Supersede: bei Index-Update die alte current-Revision abloesen — seit
        // T4 NACH der Disk-Phase, innerhalb der Action-Transaction.
        if (decision.Status == ImportStatus.UpdateNewerIndex)
        {
            var existingDoc = _db.GetDocumentByKey(decision.DocumentKey!);
            if (existingDoc is not null)
            {
                var oldCurrent = _db.GetCurrentRevisionForDocument(existingDoc.Id);
                // 111.07 Slice A2: Zweite Datei desselben Dokuments im SELBEN
                // Import (PDF+DWG-Paar) darf die gerade angelegte Revision
                // nicht gleich wieder ablösen — sie dockt unten als
                // Zusatzdatei an (FileLinked-Zweig).
                if (oldCurrent is not null && oldCurrent.LastImportId != importId)
                {
                    _db.SupersedeCurrentRevision(existingDoc.Id, actionTime);
                    _db.InsertRevisionEvent(oldCurrent.Id, importId,
                        PlanArchive.EventType.Superseded, "Durch neue Revision ersetzt");
                }
            }

            // R01-007.03 (ADR-061: DB = Ordner-Wahrheit): die archivierte Vorgaenger-Datei
            // zeigt im Plan-Cache auf ihren _Archiv-Pfad (idempotent, nur nicht-current Dateien).
            if (archiveRelPath is not null)
                _db.RelocateSupersededFile(ArchivePathScheme.OriginalOf(_path, archiveRelPath), archiveRelPath);
        }

        // ADR-061 Slice 0.6c: target_folder kommt aus dem aufgeloesten Zielpfad
        // (Root-Segment = root_relative_path des Dokumenttyps), NICHT mehr aus dem
        // entfernten profile.TargetFolder. relative_directory = voller aufgeloester Ordner.
        var resolvedDir = _path.GetDirectoryName(targetRelPath) ?? "";
        var rootParts = resolvedDir.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var rootFolder = rootParts.Length > 0 ? rootParts[0] : "";

        var documentId = _db.ResolveOrCreateDocument(
            _db.ProjectId,
            decision.DocumentKey!,
            decision.File.DocumentTypeId ?? "",
            decision.File.PlanNumber ?? "",
            decision.File.DocumentTypeDisplayName ?? "unknown",
            decision.File.Title ?? "",                   // title (Panel-Bezeichnung, Slice A3)
            rootFolder,                                  // target_folder = root_relative_path (ADR-061)
            resolvedDir,                                 // relative_directory (voller aufgeloester Ordner)
            // BPM-126: Stammdaten-IDs der Radial-Zuordnung mitschreiben (Cross-DB
            // SoftRef). Recovery Forward liest sie seit R01-006 aus dem Journal.
            decision.File.BuildingPartId,
            decision.File.BuildingLevelId);

        var currentRev = _db.GetCurrentRevisionForDocument(documentId);
        if (currentRev is not null)
        {
            // Idempotenz-Guard (T4): haengt die Datei bereits an der current-
            // Revision (Re-Apply nach Crash hinter dem DB-Commit), nichts
            // erneut schreiben — nur den Action-Status finalisieren.
            var alreadyLinked = _db.GetFilesForRevision(currentRev.Id)
                .Any(f => f.FileName == decision.File.Parsed.FileName);
            if (!alreadyLinked)
            {
                // Zusatzdatei zur bestehenden current-Revision (ChangedNoIndex/LearnIndex, z.B. DWG nach PDF)
                _db.InsertFileForRevision(currentRev.Id,
                    decision.File.Parsed.FileName, targetRelPath,
                    decision.File.Parsed.Extension,
                    decision.File.Parsed.Md5, decision.File.Parsed.FileSize,
                    isPrimary: false);
                _db.InsertRevisionEvent(currentRev.Id, importId,
                    PlanArchive.EventType.FileLinked, decision.File.Parsed.FileName);
            }
        }
        else
        {
            // Neue current-Revision (New oder nach Supersede bei UpdateNewerIndex)
            var revisionId = _db.InsertRevision(
                documentId,
                decision.File.RevisionToken,                 // plan_index
                decision.File.RevisionSource.ToString(),     // index_source
                PlanArchive.Status.Current,
                actionTime,                                  // current_from == actionTime
                null,                                        // superseded_at
                actionTime,                                  // received_at
                importId,                                    // last_import_id
                decision.File.ReleasedAt,                    // released_at (BPM-118 Text-Zuweisung)
                decision.File.ChangeNote ?? "");             // change_note (BPM-118 Text-Zuweisung)
            _db.InsertRevisionEvent(revisionId, importId,
                PlanArchive.EventType.Created, decision.File.FromClaudeSuggestion
                    ? "Neue Revision angelegt · Herkunft: Vorschlag von Claude, bestätigt"
                    : "Neue Revision angelegt");
            _db.InsertFileForRevision(revisionId,
                decision.File.Parsed.FileName, targetRelPath,
                decision.File.Parsed.Extension,
                decision.File.Parsed.Md5, decision.File.Parsed.FileSize,
                isPrimary: true);
        }

        // Vorgemerkte Segmentwerte (BPM-118 Text-Zuweisung) — haengen am
        // Dokument, nicht an der Revision. Upsert: UNIQUE(document_id,
        // segment_type_id), die letzte User-Zuweisung gewinnt.
        foreach (var seg in decision.File.AssignedSegments ?? [])
            _db.UpsertSegment(documentId, seg.SegmentTypeId, seg.TokenKey,
                seg.Value, _normalizer.NormalizeForMatch(seg.Value));

        // Tags aus "Mit Claude sortieren" (ADR-067) — vorhandene werden reaktiviert, nicht verdoppelt.
        foreach (var tag in decision.File.Tags ?? [])
            _db.AddTag(documentId, tag);

        _db.CompleteImportAction(actionId, true);
    }

    /// <summary>
    /// Bucket A (BPM-120 T2, ADR-064 P.7): bestätigte MD5-Dublette — Quelldatei
    /// aus dem Eingang löschen. Journalisiert + recovery-fähig, bewusst NICHT
    /// undo-bar (Inhalt liegt MD5-identisch im Bestand, kein Papierkorb).
    /// </summary>
    private ActionResult ExecuteSkipDuplicate(
        ImportDecision decision, string projectRootPath, string actionId)
    {
        try
        {
            // R01-008.01: eine getrackte Bestandsdatei ist nie eine Inbox-Dublette —
            // nicht loeschen; die Action gilt als erledigt (sonst haengt das Journal).
            var tracked = _db.IsTrackedFilePath(decision.File.Parsed.RelativePath);
            if (tracked)
                Log.Warning("skipDuplicate: getrackte Datei bleibt erhalten: {File}", decision.File.Parsed.FileName);
            var sourcePath = _path.Combine(projectRootPath, decision.File.Parsed.RelativePath);
            if (!tracked && _reader.FileExists(sourcePath))
                WithLockRetry(() => _writer.DeleteFile(sourcePath));
            _db.CompleteImportAction(actionId, true);
            Log.Debug("Dublette aus Eingang entfernt: {File}", decision.File.Parsed.FileName);
            return new ActionResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "skipDuplicate fehlgeschlagen: {File}", decision.File.Parsed.FileName);
            _db.CompleteImportAction(actionId, false, ex.Message);
            return new ActionResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Archivplanung eines Index-Updates (BPM-120 T2/AK 5, R01-007.03 / ADR-059-Addendum P.2):
    /// archiviert wird die Datei der abgeloesten current-Revision mit derselben Endung —
    /// auch wenn die neue Datei anders heisst. Ohne solche Datei schuetzt der Fallback eine
    /// fremde Datei am Ziel vor dem Ueberschreiben. Liegen BEIDE vor (Vorgaenger unter altem
    /// Namen UND eine andere Datei am Ziel), wird die Action nicht geplant. Der Pfad steht
    /// VOR der ersten Mutation fest und wird journalisiert (ArchivePathScheme).
    /// </summary>
    private (string? ArchiveRelPath, string? Blocked) PlanArchiveTarget(
        ImportDecision decision, string projectRootPath, string targetRelPath, HashSet<string> taken)
    {
        string? original = null;
        var doc = decision.DocumentKey is null ? null : _db.GetDocumentByKey(decision.DocumentKey);
        var current = doc is null ? null : _db.GetCurrentRevisionForDocument(doc.Id);
        if (current is not null)
            original = _db.GetFilesForRevision(current.Id)
                .Where(f => string.Equals(_path.GetExtension(f.FileName), decision.File.Parsed.Extension,
                    StringComparison.OrdinalIgnoreCase))
                .Select(f => f.RelativePath)
                .FirstOrDefault(p => !taken.Contains(p) && _reader.FileExists(_path.Combine(projectRootPath, p)));

        var targetOccupied = _reader.FileExists(_path.Combine(projectRootPath, targetRelPath));
        if (original is not null && targetOccupied
            && !ArchiveMoveTargetGuard.SamePath(original, targetRelPath))
            return (null, "Zielpfad ist durch eine andere Datei belegt — Import nicht geplant");
        if (original is null && targetOccupied)
            original = targetRelPath;
        if (original is null)
            return (null, null);

        taken.Add(original);
        return (ArchivePathScheme.Build(_path, original, DateTime.Now), null);
    }

    /// <summary>
    /// Verschiebt die vorhandene Zieldatei an den journalisierten Archivpfad.
    /// No-Op wenn die Zieldatei nicht (mehr) existiert.
    /// </summary>
    private void ArchiveExistingFile(string targetPath, string archiveAbsPath)
    {
        if (!_reader.FileExists(targetPath))
            return;

        _writer.CreateDirectory(_path.GetDirectoryName(archiveAbsPath)!);
        WithLockRetry(() => _writer.MoveFile(targetPath, archiveAbsPath));
        Log.Information("Datei archiviert: {Source} → {Archive}",
            _path.GetFileName(targetPath), _path.GetFileName(archiveAbsPath));
    }

    /// <summary>
    /// ADR-061 P5 / ADR-064 AK 7 (BPM-120 T3): eingehende Datei zunaechst als
    /// <c>&lt;ziel&gt;.bpm_tmp</c> in den Zielbereich holen, dann atomarer Rename
    /// im selben Verzeichnis. Ein Crash zwischen den Schritten hinterlaesst nur
    /// eine tmp-Datei — nie ein halb geschriebenes Ziel; die Recovery holt den
    /// finalen Rename idempotent nach. Eine tmp-Leiche aus einem frueheren
    /// Abbruch wird ueberschrieben (Source ist die Wahrheit).
    /// </summary>
    private void PublishIncomingFile(string sourcePath, string targetPath)
    {
        var tmpPath = targetPath + TmpSuffix;
        WithLockRetry(() => _writer.MoveFile(sourcePath, tmpPath, overwrite: true));
        WithLockRetry(() => _writer.MoveFile(tmpPath, targetPath, overwrite: true));
    }

    /// <summary>Temp-Suffix des Disk-Protokolls (auch von der Recovery verwendet).</summary>
    public const string TmpSuffix = ".bpm_tmp";

    /// <summary>
    /// Lock-Retry nach beschlossener Regel (ADR-061 P5 / ADR-064 AK 7): max. 3
    /// Versuche bei Lock-/Sharing-Verletzungen, kurzer wachsender Backoff.
    /// Fehlende Datei/Verzeichnis wird NICHT wiederholt (kein Lock-Fall).
    /// Keine 2PC, keine verteilten Locks.
    /// </summary>
    internal static void WithLockRetry(Action fileOp)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                fileOp();
                return;
            }
            catch (IOException ex) when (attempt < 3
                && ex is not FileNotFoundException and not DirectoryNotFoundException)
            {
                Log.Warning("Dateioperation gesperrt (Versuch {Attempt}/3): {Message}",
                    attempt, ex.Message);
                Thread.Sleep(150 * attempt);
            }
        }
    }

    private sealed record ActionResult(bool Success, string? Error);
}

/// <summary>
/// Summary of a completed import execution.
/// </summary>
public sealed record ImportExecutionResult(
    int Succeeded,
    int Failed,
    int Skipped,
    List<string> Errors);
