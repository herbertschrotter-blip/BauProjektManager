# Review Runde 2 — ChatGPT-Antwort (Canvas)

> Quelle: ChatGPT GPT-5.4, Canvas „Review Runde 2", 2026-08-27. 1:1 archiviert (Kernaussagen; Formatierung leicht normalisiert).

---

Kollege, mit den Entscheidungen aus Runde 1 wird das Bild deutlich schärfer. Vor allem die Abschaltung des klassischen Profil-Imports ist wichtig: Wir müssen nicht zwei Importarchitekturen härten, sondern können die V1-Sicherheitsarbeit auf ADR-059 Radial/Buckets → Pending → Confirm → Execution konzentrieren.

## A. Nachlieferung Diagramme 01–09

### 01 — Systemkontext
- Verifiziert: WPF-Desktop, Projektdateien im Windows-Dateisystem; Cloud-Sync = externer FS-Sync, kein BPM-Cloud-API-Pfad; `RegistryJsonExporter` erzeugt `registry.json` als abgeleiteten Vertrag; interne SQLite-DBs bewusst nicht im Systemkontext.
- Nicht in Gesamtauswertung übernommen: `registry.json` als externer Integrationsvertrag; explizite Trennung BPM ↔ Dateisystem ↔ externer Sync-Client.
- Korrektur heute: „BPM → Windows-Dateisystem" zu grob — nach ADR-060 gilt intern Feature → FS-Ports → LocalFileSystem → Dateisystem.

### 02 — Container-/Speicherarchitektur
- Verifiziert: `%LocalAppData%\BauProjektManager` (bpm.db, device-settings.json, Projects/<id>/planmanager.db, Logs); Arbeitsbereich `.AppData/BauProjektManager/` (shared-config.json, registry.json); `<ProjectRoot>/.bpm/` (manifest.json, profiles/*.json). `PersistenceRegistry` kennt `.bpm/project.json` ohne reale ProjectExportService-Implementierung; `.bpm/plan-index.json` dokumentiert aber nicht implementiert.
- Korrektur heute: planmanager.db hatte zu viel Autorität — nach ADR-058-Addendum: bpm.db = SoR, planmanager.db = rebuildbarer bounded Cache + Journal. Wichtigste Korrektur an 02/04.

### 03 — Solution-/Dependency-Architektur
- Verifiziert: Domain → nichts; Infrastructure → Domain; PlanManager/Settings → Domain+Infrastructure; App → alle. Keine Zyklen. Microsoft.Data.Sqlite in Infrastructure, PlanManagerDatabase nutzt es transitiv.
- Korrektur heute: „PlanManagerDatabase zwingend nach Infrastructure" schwächer formulieren — ADR-058 definiert die DB als PlanManager-spezifischen bounded Cache + Journal mit eigenem Lebenszyklus. Verschieben = Hygiene, nicht notwendig, auch post-V1 nicht automatisch zwingend.

### 04 — Persistenz und Datenhoheit
- Größte Fehlinterpretation: planmanager.db als „SoR für Plan-Fachmetadaten" war falsch (in r1 korrigiert). Korrekt: bpm.db = SoR; planmanager.db = rebuildbarer Cache + Journal/Undo/operative Historie; Dateisystem = physische Dateien; profiles/*.json = Profilquelle Mode A.
- Nicht übernommen: Spannung `plan_context_links` (nicht offensichtlich aus FS rebuildbar; ADR verlangt Neubewertung bei Cross-Modul-Nutzung). Kein V1-Problem.

### 05 — bpm.db ERD
- Verifiziert: clients, projects, building_parts, building_levels, document_types, document_type_categories, project_participants, project_links, segment_type_groups, segment_types; harte interne FKs sauber; ADR-061-Felder sichtbar (document_types.key/root_relative_path/folder_name, building_parts.folder_name, building_levels.folder_name). `project_participants.contact_id` ohne Contacts-Tabelle im Schema.
- Nicht übernommen: contact_id als Zukunftsbezug; globale Segmenttypen vs. projektbezogene Dokumenttypen; rename-stabile folder_name-Semantik als Stärke.
- Korrektur heute: ADR-059/061 bestätigen Diagramm 05; document_types ist heute Routing-/Radial-/Ordner-Wahrheit, zentraler als damals.

### 06 — planmanager.db ERD
- Verifiziert: Drei Ebenen + revision_file_links (N:M); Partial Unique Index „eine current Revision pro document_id"; plan_document_segments, plan_revision_events, plan_context_links, import_journal, import_actions, import_action_files; Cross-DB-Bezüge korrekt als TEXT-SoftRefs (building_part_id, building_level_id, segment_type_id).
- Nicht übernommen: Revision↔File N:M obwohl Schreibpfade meist einfacher; plan_revision_events = Audit-Trail, kein Event Sourcing.
- Korrektur heute: gutes relationales Modell ≠ SoR; Journal-Haltbarkeit wichtiger als der Begriff „rebuildbar" suggeriert.

### 07 — PlanManager-Komponenten
- Verifiziert (damals): automatischer Pfad ImportWorkflowService → Scan → Fingerprint → Profiles → Parse/Recognition → Context → DocumentKey → RevisionDecision → Target Resolver → Preview → ImportExecutionService. Daneben ManualFirstCaptureService → PendingAssignmentStore → CaptureConfirmService → ImportExecutionService.
- Nicht übernommen: veralteter Kommentar in RevisionDecisionService (behauptete keine DB-Entscheidung trotz GetCurrentRevisionLookup()) — Kommentarhygiene.
- Korrektur heute: V1-Hauptpfad = ManualFirstCapture → Buckets → Radial+Panel → Pending → CaptureConfirm → ImportExecution. Profil-Pipeline nach Herberts Entscheidung kein V1-Hauptpfad mehr.

### 08 — Fachlicher Importflow
- Basierte auf den 9 klassischen ImportStatus (New/SkipIdentical/UpdateNewerIndex/ChangedNoIndex/ChangedSameIndex/OlderRevision/LearnIndex/Unknown/Conflict) — Quelle der Punkte 5–10.
- Korrektur heute: ADR-059 ersetzt für V1 die Matrix durch Buckets A Duplicate / B UpdateProposal / C NewCapture / D Conflict. ManualFirstCaptureService implementiert bereits: gleicher Index+anderer Inhalt → D; niedrigerer Index → B mit OLDER_REVISION-Warnung. Verbleibend: Bucket A sauber abarbeiten, B-Warnungen bewusst bestätigen, D nicht ohne Auflösung mutieren, Pending = einzige Mutationsfreigabe.

### 09 — Technische Importsequenz (wichtigster Befundblock)
- Verifiziert: CreateImportJournal → pro Datei InsertImportAction → ggf. archivieren → superseden → File.Move → Resolve/Create Document → Revision/File/Event → Action completed → Journal completed. Dabei: Actions nicht vorab; archive_path null; direktes System.IO; kein .bpm_tmp; kein atomarer Rename; Recovery Forward wiederholt nur Datei-Moves; gefangene Fehler → terminal failed; Recovery sucht nur pending.
- Nicht übernommen: die einzelnen Crashfenster (nach Archiv / nach Supersede / nach File.Move / nach DB-Write / vor Action completed) nur zusammengefasst.
- Korrektur heute: Befunde gültig; .bpm_tmp/atomic rename/idempotente Recovery waren ADR-061 P5, nicht neu; eigener Beitrag = DB-/Journal-Präzisierung.

## B. Task-Schnitt

### B1. T0/T1 — Korrektur übernommen
Fault Injection vor der Port-Migration lohnt nicht (Wegwerf-Hooks). Neuer T0 = ausschließlich Characterization + Safety Net (echte Temp-Verzeichnisse): Happy Path New; Happy Path Update (inkl. Zeitinvariante old.superseded_at == new.current_from); CaptureConfirm-Mapping (PendingAssignment → ImportDecision, Update bleibt ExistingDocumentMatch); Journal-Charakterisierung (eine Action pro Datei, Statusübergänge); Undo-Happy-Path. Bekannte Fehler (archive_path=null) NICHT als gewünschtes Verhalten festschreiben. Nach T1 mit FakeFileStore: fail on nth MoveFile/CopyFile/DeleteFile/atomic rename, simulate existing temp/final.

### B2. Undo braucht eigenen Härtungs-Slice (T7) — T2/T3 reichen NICHT
Nach Prüfung von ImportUndoService.cs fünf Probleme: (1) direktes System.IO (muss durch ADR-060); (2) Undo selbst nicht crash-sicher (kein recoverbarer Undo-Fortschritt); (3) **DB-Rollback läuft nach Dateifehlern trotzdem weiter** — catch → errors.Add, danach trotzdem SoftDeleteRevision/RestoreRevisionToCurrent/SoftDeleteDocumentIfNoRevisions/MarkImportUndone → Disk-Rollback teilweise fehlgeschlagen, DB trotzdem vollständig zurückgerollt + `undone` markiert (= ADR-058 „Undo wackelt → sofort Stopp"); (4) keine DB-Transaction für Undo; (5) neues skipDuplicate passt nicht in die Annahme „completed Action → Destination existiert → Destination → Source".
Empfehlung: T7 Undo-Härtung — Preflight aller Actions → Reverse Disk LIFO → NUR bei vollständigem Disk-Erfolg DB-Rollback in Transaction → Journal undone. Bei Disk-Reverse-Fehler: KEIN DB-Rollback, KEIN MarkImportUndone, reparierbar bleiben. Kleiner gemeinsamer Kern ApplyForward(action)/ApplyReverse(action) statt getrennter Pfad-Interpretationen. Kein Framework/Command-Bus.

### B3. DI statt `new` → T1
Als lokale Constructor-Injection-Bereinigung des betroffenen Pfads, NICHT vollständiger Composition Root. Ziel: CaptureConfirmService(ImportExecutionService execution, PendingAssignmentStore store) statt internes new. Projektbezogene PlanManagerDatabase erschwert globale Singleton-Registrierung ohnehin; manuelle äußere Komposition in ProjectDetailView bleibt für V1 ausreichend.

### Finale Task-Empfehlung
**Vor dem neuen Task: H0 — V1 Import Route Cutover:** „Import starten"-Button deaktivieren/entfernen, OnStartImport() nicht mehr V1-erreichbar, ImportPreviewDialog raus aus dem V1-Nutzerpfad. Legacy-Klassen können bleiben (kein Lösch-Refactoring). Einziger zu sichernder Pfad: ManualFirstCapture → Pending → CaptureConfirm → ImportExecution.
**Neuer Task „Import-Transaktions-Härtung"** (BPM-112 Slice 3 als Teilmenge). Revidierte Slice-Folge:
H0 Cutover → T0 Characterization → T1 FS-Ports + Fault-Fake + lokale Constructor Injection → T2 vollständiger Action-Plan vor Mutation (inkl. deterministische source/destination/archive-Pfade + Bucket-A-Actions) → T3 ADR-061-Disk-Protokoll (.bpm_tmp, atomic rename, Retry, deterministische Archive) → T4 DB-Transaction pro Action + idempotenter DB-Apply → T5 Recovery Forward über denselben Apply-Pfad → T6 failed/pending + Rollback/Cleanup-Semantik → T7 Undo-Härtung → T8 Fault-/Crash-Matrix + Integrationssuite.

## C. Bucket-A-Journalmodell

### C1. action_type = "skipDuplicate"
Echte Action (nicht Sondercode nach dem Loop). Journal: source_path, md5, file_size, duplicate-Evidence. `import_actions.destination_path` ist aktuell NOT NULL — für skipDuplicate kein echtes Ziel → destination_path NULL zulassen, Frühphase: DB neu erzeugen, keine Migration. Alternativ Dateidetails in import_action_files (existiert, kaum genutzt).
Recovery-Semantik: pending + Source existiert + Hash = Dublette → löschen. Pending + Source fehlt → prüfen ob Bestand mit diesem MD5 existiert: ja → completed (Endzustand „redundante Inbox-Kopie existiert nicht mehr" erreicht, egal ob BPM oder extern); nein → RecoveryConflict, nicht blind completed.

### C2. Delete statt Papierkorb — Empfehlung: direktes Delete, explizit nicht undo-bar
MD5-identisch = Inhalt nachweislich im Bestand, keine einzigartige Information geht verloren. Delete: Eingang wirklich leer, kein Müll/Sync-Traffic/Cleanup-Lifecycle, einfache Idempotenz; Nachteil: Undo stellt Inbox-Kopie nicht wieder her. Papierkorb: perfektes Inbox-Undo, aber große Dateien, Lifecycle, Cloud-Sync versteckter Dateien, mehr Recovery-Zustände — für V1 unnötig. Undo-Definition explizit: skipDuplicate wird nicht wiederhergestellt (keine fachliche Revision erzeugt, Inhalt identisch im Bestand). Passt zur bestehenden Intention (SKIP war nie undo-bar). Neu: journalisiert ≠ automatisch undo-bar.

### C3. Slice-Zuordnung
Bucket A beginnt in T2 (geplante Action vor erster Mutation); Ausführung in T3/T4 (gemeinsamer Apply-Rahmen), T5 (Recovery), T7 (Undo-Semantik „no reverse"). Kein separater Task/Slice. Pseudomodell: switch(action.Type) → ImportNew/UpdateRevision/SkipDuplicate mit EnsureDuplicateRemoved; Undo-switch: SkipDuplicate = bewusst kein Reverse.

### Konsequenz H0
Alte Quick-Fixes endgültig aus V1-Priorität: Skip-only-Early-Return + DocumentTypeRecognizer.IsConflict (Bucket-A-Pfad ersetzt alten Skip-Code; Recognizer entscheidet im V1-Pfad nicht).

## ✅ Einigkeit
- H0: ein Importweg für V1. T0 nur Characterization, Fault Injection nach T1. BPM-112 Slice 3 = Unterbau, nicht der ganze Task. Vorab-Journalisierung, deterministische Archivpfade, idempotenter Apply, Recovery V1-relevant. ImportUndoService braucht eigenen Slice. Constructor Injection in T1 ohne Composition-Root-Umbau. Bucket A = echte journalisierte Action; skipDuplicate recovery-fähig aber bewusst nicht undo-bar. destination_path nullable → DB-Reset. Alte Preview-/LearnIndex-/Recognizer-Prioritäten überholt.

## ⚠️ Widerspruch
- Kein Papierkorb/Quarantine für Bucket A (Lifecycle für MD5-identische Daten nicht gerechtfertigt).
- ImportActionExecutor = Implementierungsform, kein Architekturvertrag (ADR-Invariante legt Verhalten fest, nicht Klassennamen).
- PlanManagerDatabase → Infrastructure nicht mehr als notwendiges Ziel darstellen.
- Eigenes Diagramm 04 war bzgl. planmanager.db-SoR fachlich falsch.

## ❓ Rückfragen
Keine blockierenden Architekturfragen. Herbert kann den Schnitt festlegen: eigener Task „Import-Transaktions-Härtung", BPM-112 Slice 3 als technischer Teil, nach H0 und direkt nach Abschluss des aktuellen BPM-111.06-Slices.
