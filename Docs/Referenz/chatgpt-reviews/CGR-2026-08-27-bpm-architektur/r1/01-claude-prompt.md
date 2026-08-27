# Review Runde 1 — BPM-Gesamtauswertung vs. beschlossene Architektur (ADR-058–061)

## Rolle

Du bist ein erfahrener Software-Architekt (C#/.NET, WPF-Desktop, offline-first Systeme mit SQLite) und führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic).

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User).
- Sprich direkt zu deinem Kollegen, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 1"
- Fasse am Ende JEDER Antwort zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** herbertschrotter-blip/BauProjektManager
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Hinweis: Der gepushte Branch steht auf v0.28.107. Lokal existieren Commits bis v0.28.120 (UI-Detailarbeit BPM-111.06 Detail-Panel) — für die hier diskutierten Import-/Recovery-Dateien inhaltlich ohne Bedeutung. Die im Prompt zitierten Zeilennummern können um wenige Zeilen abweichen.

## Gesprächsregeln

- Ehrlich und kritisch — ausdrücklich auch gegenüber deiner eigenen früheren Analyse (siehe Ausgangslage)
- Probleme konkret benennen
- Verbesserungen mit Code/Pseudocode zeigen
- Rückfragen bei fehlendem Kontext
- Fokus halten, keine allgemeinen Exkurse
- Kompakt, Code nur wenn nötig
- Fokus: Einordnung und Priorisierung der Recovery-/Journal-Befunde in den bereits beschlossenen ADR-/Task-Rahmen — NICHT neue Architektur erfinden

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als gewollter Standardweg

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

Quelle: INDEX.md Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads + ADR-Auszügen)

### ADR.md (source_of_truth)
- Zweck: Alle Architecture Decision Records — Kontext, Entscheidung, Konsequenzen
- Statusmodell: Decision Status (Proposed/Accepted/Superseded) getrennt von Implementation Status (Not Started/Partial/Implemented) — eine ADR kann beschlossen, aber nicht umgesetzt sein
- Relevante ADRs für dieses Review: 058 (+Addendum), 059 (+Addendum), 060, 061

### ADR-058: Plan-Archiv-Persistenz (BPM-109) — beschlossen + Foundation umgesetzt
- Drei-Ebenen-Modell `plan_documents` → `plan_revisions` → `plan_files`, Schema v2.0
- Zeitreise via `current_from`/`superseded_at` (Invariante: superseded_at(alt) == current_from(neu), EIN actionTime pro Import-Aktion)
- UNIQUE-Index auf `(document_id) WHERE status='current'`
- Stop-Punkte definiert, u.a.: „Import-Journal/Undo wackelt → sofort Stopp", „Dateiverschiebung + DB-Commit inkonsistent → sofort Stopp"
- Addendum: Cross-DB Soft-References (planmanager.db → bpm.db als TEXT ohne FK), `bpm.db` = System of Record, `planmanager.db` = rebuildbarer per-Projekt-Cache + Journal

### ADR-059: Recognition v2 / Plan-Erfassung — Strategie B + Radial-UI (beschlossen, BPM-111 in Umsetzung)
- MVP = manuelle Erstaufnahme + deterministisches Matching (MD5-Dublette → Skip, neuer Index → Revision/Supersede, sonst Erstaufnahme). Auto-Extraktion nur Assist, nie Entscheider
- V1-UI = Radial-/Nautilus-Menü mit Buckets: A Dubletten / B Update-Karten / C manuelle Erstaufnahme (→ Radial) / D Konflikte. Pending Assignments, finaler Import erst nach Preview/Bestätigung
- WICHTIG: Der klassische Profil-Import-Wizard (BPM-080.05) und der ImportPreviewDialog (BPM-081) haben dadurch für V1 stark an Gewicht verloren; die dokumentierte Erfassungs-UX ist jetzt Radial+Buckets+Detail-Panel, nicht der alte Preview-Dialog

### ADR-060: Vereinheitlichte Dateisystem-Ports (BPM-112) — beschlossen, teilweise umgesetzt
- Ports `IFileSystemReader`/`IFileSystemWriter`/`IPathService` in Domain, Adapter `LocalFileSystem` in Infrastructure, kein direktes System.IO außerhalb des Adapters
- **Slice 0 done (v0.28.85): Ports + Adapter + DI + FakeFileStore/Contract-Tests. Slices 1–6 OFFEN (~29 System.IO-Stellen)**, darunter: Slice 3 = „transaktionaler Import (Hochrisiko, ImportExecutionService)", Slice 6 = In-App-Explorer (erst nach stabilen Ports)
- Zusatz-Ports für In-App-Explorer: `IFileLauncher`, später `IShareService`

### ADR-061: DB als einzige Ordner-Wahrheit + DocumentTargetPathResolver (BPM-113) — beschlossen + umgesetzt (v0.28.86–.98)
- `document_types.key` + `root_relative_path` + `folder_name`; `DocumentTargetPathResolver` Fail-Fast, IDs-vor-Namen, kein Fuzzy; `profile.TargetFolder` gebrochen (SchemaVersion 5)
- **Punkt 5 (Transaktionalität, BESCHLOSSEN aber im Import-Pfad NICHT umgesetzt):** Journal VOR Move + temp-im-Zielordner (`.bpm_tmp`) + atomic rename + idempotente Recovery. Locks: einfacher Retry (3×). NICHT bauen: verteilte Locks, FileSystemWatcher-Sync-Engine, OneDrive-API, 2PC
- **Punkt 6 (In-App-Explorer, Modell A, BESCHLOSSEN):** DB = kuratierter Index NUR bewusst erfasster Pläne, KEIN Vollspiegel. Explorer liest Dateisystem live. Startup-Reconcile nur getrackte Teilmenge (Exists+Size first, Hash bei Bedarf); Drift-Status `MissingOnDisk`/`ChangedOnDisk`/`RelinkCandidate`; MD5-Relink nur Vorschlag. **Getrackte Dateien im Explorer nicht frei verschieb-/löschbar (nur über Journal-Service)**

### Task-Status (heute, 2026-08-27)
- BPM-109 (Schema v2 Foundation): done
- BPM-111 (Radial-Erfassung): in Arbeit, aktuell .06 Detail-Panel (v0.28.107–.120)
- BPM-112 (FS-Ports-Migration): Slice 0 done, Slices 1–6 offen
- BPM-113 (Ordner-Wahrheit/Resolver): done
- Aktuell zusätzlich in Arbeit (uncommitted): PDF-Render/Text-Ports (ADR-062/063, PdfiumPdfService)

## Ausgangslage — deine frühere Analyse

Du hast in einem früheren Gespräch („Softwareentwicklungsdiagramme") 12 Architekturdiagramme zu BPM erstellt und eine Gesamtauswertung mit 37 Punkten geliefert. Die für dieses Review relevanten Kernpunkte (Nummerierung wie in deiner Auswertung):

**🔴 Priorität 1 — V1-Sperrposten (deine Einschätzung):**
1. Import-Recovery unvollständig: Crash zwischen Dateisystem-Änderung und planmanager.db-Update → FS und DB laufen auseinander; RecoveryExecutorService wiederholt nur Dateioperationen, stellt das Planarchiv (Revisionen/Supersede) nicht her. Empfehlung: gemeinsamer idempotenter `ImportActionExecutor` für normalen Import + Recovery Forward (Disk-Zustand prüfen/herstellen → Planarchiv prüfen/herstellen → Action completed)
2. Alle Import-Actions VOR der ersten Mutation journalisieren (heute: Header vorab, Actions einzeln unmittelbar vor jeder Operation)
3. `archive_path` wird beim Anlegen der Action als null journalisiert, echter Archivpfad entsteht erst danach intern → Undo/Recovery kennen die Vorgängerdatei nicht zuverlässig
4. Failed-Imports hinterlassen inkonsistente Seiteneffekte: Recovery-Hook sucht nur `journal.status='pending'`; ein teilweise fehlgeschlagener Import wird auf `failed` gesetzt und beim nächsten Start ignoriert. `failed` darf erst terminal sein, wenn vollständig zurückgerollt oder bewusst als manueller Recovery-Fall markiert
5. Skip-only-Bug: bestehen NUR identische Dateien im Eingang, greift der Early-Return vor der Skip-Bereinigung → Eingang bleibt liegen
6. ChangedSameIndex/OlderRevision brauchen echte User-Entscheidung pro Datei (Preview zeigt sie, bietet aber keine Aktion; `IsActionable` und Executor widersprechen sich)

**🟠 Priorität 2 — fachlich vervollständigen (deine Einschätzung):**
7. CONFLICT nicht durch die Pipeline verdrahtet (RevisionDecisionService erzeugt praktisch keinen Conflict; Gleichstand fällt als Unknown heraus)
8. `IsConflict` semantisch zu grob (`AllMatches.Count > 1` statt „mehrere beste Matches mit gleicher effektiver Priority")
9. ImportPreview deutlich einfacher als dokumentierte UX (UNKNOWN zuweisen, CONFLICT wählen, Warnfälle entscheiden, LEARN_INDEX bestätigen)
10. LearnIndex lernt nicht dauerhaft (Profilmutation fehlt — oder Umbenennen des Status)

**Dein Phasenplan:** A PlanManager sicher machen (Punkte 1–4 + Crash-Fenster-Tests) → B Importentscheidung fertigstellen (5–10, Preview-UX) → C V1 abschließen (SoftRefs, Backup, plan-index.json, Integrationstests, Doku) → D Architekturhygiene (PlanManagerDatabase → Infrastructure, Composition Root, Doku-Drift). Deine Empfehlung: KEINE weiteren PlanManager-Features, bevor der Pfad Analyse → Preview → Journal → Dateioperation → Planarchiv → Recovery → Undo wasserdicht ist.

**Dein Dateibrowser-Vorschlag:** eigenes Modul `BauProjektManager.FileBrowser`, zentraler `ProjectFileOperationService` (Verschieben/Umbenennen/Löschen/Copy/Drag&Drop nur über einen Service), Projekt-Sandbox (nur unterhalb ProjectRoot), `.bpm` versteckt, FileSystemWatcher nur für UI-Refresh, bei planverwalteten Dateien DB-Metadaten mitführen + fachliche Rückfrage, Diagramm 13 dafür.

## Claudes Verifikation deiner Befunde

Ich habe deine 🔴-Behauptungen gegen den Code geprüft (lokaler Stand v0.28.120) — **alle bestätigt:**

| Dein Befund | Code-Beleg |
|---|---|
| Actions einzeln statt vorab journalisiert (Punkt 2) | `src/BauProjektManager.PlanManager/Services/ImportExecutionService.cs` — `InsertImportAction` läuft pro Aktion in `ExecuteSingleAction` (Z. ~126) |
| `archive_path` = null (Punkt 3) | ebd. Z. ~134: `archivePath: null` |
| Skip-only-Bug (Punkt 5) | ebd. Z. ~39: `if (actionable.Count == 0) return` VOR der Skip-Bereinigung Z. ~72 ff. |
| Recovery nur `pending` (Punkt 4) | `PlanManagerDatabase.cs` — `HasPendingImports()`/`GetPendingImports()` filtern `WHERE status = 'pending'` |
| `IsConflict` zu grob (Punkt 8) | `DocumentTypeRecognizer.cs` Z. ~23: `public bool IsConflict => AllMatches.Count > 1;` |

**Mein zusätzlicher Befund:** ADR-061 Punkt 5 (Journal VOR Move + `.bpm_tmp`-Temp + atomic rename + idempotente Recovery) ist BESCHLOSSEN, aber im Import-Pfad NICHT umgesetzt — `ImportExecutionService.cs` Z. ~166 macht direktes `File.Move(sourcePath, targetPath, overwrite: true)`, kein Temp, kein atomic rename. BPM-113 hat Resolver/Schema geliefert, nicht die Transaktions-Härtung. Die deckt sich mit ADR-060 Slice 3 („transaktionaler Import, Hochrisiko"), der noch offen ist.

**Meine Kernkritik an deiner Auswertung:** Sie ist im Code-Befund korrekt, kannte aber ADR-059/060/061 nicht oder nur teilweise. Mehrere deiner Empfehlungen sind bereits beschlossen (dein `ImportActionExecutor` ≈ ADR-061 P5 + ADR-060 Slice 3; dein `ProjectFileOperationService` ≈ ADR-060-Ports + ADR-061 Modell A Journal-Service), und deine 🟠-Punkte 6/9/10 betreffen den klassischen Profil-Import-Preview, dessen UX-Rolle ADR-059 (Buckets + Radial + Detail-Panel) inzwischen neu verteilt hat.

## Aufgabe

Lies bei Bedarf die Originaldateien im Repo (Branch `feature/planmanager-v1`!) und beantworte konkret:

1. **Executor-Einordnung:** Ist dein vorgeschlagener idempotenter `ImportActionExecutor` inhaltlich deckungsgleich mit ADR-060 Slice 3 + ADR-061 Punkt 5 — oder geht er darüber hinaus (z.B. „Planarchiv-Zustand prüfen/herstellen" als DB-seitige Idempotenz)? Formuliere die Delta-Liste: Was liefert ADR-060 Slice 3 + ADR-061 P5 noch NICHT, das dein Phase-A-Plan fordert (vollständige Vorab-Journalisierung, archive_path-Fix, failed-Statusmodell, DB-Transaction pro Action, Recovery mit voller Planarchivlogik)?
2. **Task-Schnitt:** Wie würdest du die Phase-A-Arbeit konkret schneiden — ADR-060 Slice 3 erweitern (BPM-112) oder eigener neuer Task „Import-Transaktions-Härtung" mit ADR-060 Slice 3 als Teilmenge? Schlage eine Slice-Reihenfolge vor, die die ADR-058-Stop-Punkte respektiert („Import-Journal/Undo wackelt → sofort Stopp") und in der jeder Zwischenstand baubar + grün ist.
3. **Reihenfolge-Konflikt:** Deine Empfehlung „keine weiteren PlanManager-Features vor wasserdichtem Import-Pfad" kollidiert mit dem laufenden, V1-signierten BPM-111-Track (Radial-Erfassung, aktuell .06) und der laufenden PDF-Port-Arbeit (ADR-062/063). Harte Empfehlung: BPM-111 pausieren, parallel weiterlaufen lassen, oder Phase A direkt nach Abschluss des aktuellen .06-Slices einschieben? Begründe mit dem tatsächlichen Risiko (wer erzeugt heute reale Imports?).
4. **Quick-Fixes:** Welche deiner Punkte sind isolierte, sofort machbare Fixes UNABHÄNGIG vom Executor-Umbau (Kandidaten: Punkt 5 Skip-only, Punkt 3 archive_path vorab berechnen, Punkt 8 IsConflict-Semantik)? Welche davon lohnen sich vorab, welche würden durch den Umbau ohnehin ersetzt (Doppelarbeit)?
5. **Überholte Punkte:** Prüfe selbstkritisch, welche deiner 🟠-Punkte 6/9/10 (Preview-UX, User-Entscheidungen, LearnIndex) durch ADR-059 (Strategie B: Radial + Buckets A/B/C/D + Pending Assignments + Detail-Panel; Auto-Profile nur Assist) inhaltlich überholt oder verschoben sind. Was bleibt davon als echte V1-Lücke im ADR-059-Workflow übrig, was ist post-V1, was ist obsolet?
6. **Dateibrowser:** Gleiche deinen `FileBrowser`-Vorschlag mit ADR-061 Punkt 6 (Modell A: kuratierter Index, Explorer liest FS live, getrackte Dateien NUR über Journal-Service beweglich) und ADR-060 Slice 6 (Explorer erst nach stabilen Ports) ab. Wo besteht Konsens, wo Delta (z.B. dein „fachliche Zuordnung ändern?"-Dialog beim Verschieben)? Ist dein Diagramm 13 jetzt sinnvoll oder erst bei Feature-Start?

Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.
