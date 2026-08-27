# Review Runde 1 — ChatGPT-Antwort (Canvas)

> Quelle: ChatGPT GPT-5.4, Canvas „Review Runde 1", 2026-08-27. 1:1 archiviert.

---

# Review Runde 1
Kollege, nach Abgleich von ADR-058–061 und dem aktuellen Branch korrigiere ich meine frühere Gesamtauswertung an drei Stellen vorweg:
1. Ich habe **ADR-061 Punkt 5 unterschätzt**. Temp-im-Zielordner, atomic rename und idempotente Recovery sind bereits beschlossen; das war keine neue Empfehlung meinerseits.
2. Ich habe zu stark auf den alten `ImportPreviewDialog` fokussiert. **ADR-059 hat die V1-UX faktisch auf Buckets + Radial + Detail-Panel verschoben.**
3. Ich habe `planmanager.db` zu stark als PlanManager-SoR bezeichnet. ADR-058 Addendum ist eindeutig: **rebuildbarer bounded Cache + Journal**, während `bpm.db` SoR bleibt. Die Recovery-Probleme bleiben trotzdem V1-kritisch, weil Journal, Undo, Matching und Cachezustand während eines Imports konsistent sein müssen.
## 1. Executor-Einordnung
Mein vorgeschlagener idempotenter `ImportActionExecutor` ist **nicht vollständig deckungsgleich**, sondern eine mögliche konkrete Umsetzung der bereits beschlossenen ADRs plus einiger noch nicht explizit spezifizierter Invarianten.
ADR-060 Slice 3 sagt im Kern:
> `ImportExecutionService` von direktem `System.IO` auf die FS-Ports umstellen.
ADR-061 P5 sagt:
> Journal vor Move, `.bpm_tmp`, finaler atomic rename, idempotente Recovery, 3× Lock-Retry.
Damit sind bereits beschlossen:
```text
IFileSystemReader/Writer/IPathService
        +
Journal vor Dateimutationen
        +
.bpm_tmp
        +
atomic temp → final
        +
Recovery muss wiederholbar sein
```
Mein `ImportActionExecutor` geht bei der **DB-Seite und beim Journal-Protokoll** weiter.
### Delta
| Thema | ADR-060/061 | Mein Phase-A-Bedarf |
| --- | --- | --- |
| FS-Ports | explizit beschlossen | identisch |
| `.bpm_tmp` + atomic rename | explizit beschlossen | identisch |
| Lock-Retry 3× | explizit beschlossen | identisch |
| Recovery idempotent | beschlossen | konkretisieren |
| alle Actions komplett vor erster Mutation persistieren | **nicht eindeutig festgelegt** | ja |
| `archive_path` vor Archiv-Move persistieren | nicht spezifiziert | ja |
| DB-seitige Idempotenz | nicht konkret spezifiziert | ja |
| DB-Transaction pro Action | nicht konkret spezifiziert | ja |
| Recovery führt vollständige Revision-/File-/Event-Logik aus | nur implizit durch Konsistenzforderung | ja |
| `failed`-Terminalsemantik | nicht spezifiziert | ja |
| gemeinsamer Executor für Normal + Forward | nicht vorgeschrieben | Implementierungsoption |
Insbesondere ADR-058 macht den zusätzlichen Bedarf zwingend:
> „Import-Journal/Undo wackelt → sofort Stopp"
> „Dateiverschiebung + DB-Commit inkonsistent → sofort Stopp"
Das heißt für mich: **DB-seitige Wiederholbarkeit ist keine neue Architekturentscheidung, sondern notwendige Ausformulierung der bestehenden Stop-Invariante.**
Ich würde aber nicht in den ADR schreiben:
> Es MUSS eine Klasse `ImportActionExecutor` geben.
Das wäre zu konkret.
Die Architektur-Invariante sollte heißen:
```text
Eine journalisierte ImportAction muss aus jedem zulässigen
Zwischenzustand idempotent auf den definierten Endzustand
gebracht werden können – Dateisystem UND Plan-Cache.
```
Ob die Klasse am Ende `ImportActionExecutor`, `ImportTransactionService` oder anders heißt, ist Implementierungsdetail.

## 2. Task-Schnitt
Ich würde **BPM-112 Slice 3 nicht aufblasen**.
BPM-112 ist ein Infrastruktur-/Port-Refactoring. Wenn wir dort plötzlich Journal-Zustandsmodell, Recovery-Semantik, DB-Transactions und Revision-Idempotenz hineinpacken, wird der Task semantisch unscharf.
Meine Empfehlung:
> **Eigener Task „Import-Transaktions-Härtung"**, verknüpft mit ADR-058 + ADR-061.
> BPM-112 Slice 3 wird darin als technische Teilmenge erledigt.

### Slice-Reihenfolge
- **Slice T0 – Charakterisierung + Fault-Injection-Seams:** Tests für Journal angelegt / Action angelegt / Archiv-Move / Temp-Move / Final-Rename / DB-Supersede / DB-NewRevision / Action complete / Journal complete. Definierte Abbruchpunkte injizierbar machen. Kein fachliches Verhalten ändern. Build grün.
- **Slice T1 – BPM-112 Slice 3: FS-Ports:** `ImportExecutionService` + Recovery-Dateilogik auf `IFileSystemReader`/`IFileSystemWriter`/`IPathService`. Keine Semantikänderung. Hochrisikopfad wird mit `FakeFileStore` testbar. Build grün.
- **Slice T2 – vollständiger Action-Plan vor Mutation:** `CreateImportJournal()` + alle `InsertImportAction(...)` VOR dem ersten Dateizugriff; `source_path`/`destination_path`/`archive_path` deterministisch vorab berechnen. Schließt Crashfenster „file_count=5, Journal kennt nur Action 1". Build grün.
- **Slice T3 – ADR-061 Dateiseite:** Source → `.bpm_tmp` → final atomic rename; deterministischer Archivpfad bei Revision-Update; 3× Lock-Retry. Keine 2PC, keine verteilten Locks. Build grün.
- **Slice T4 – DB-Transaction pro Action:** BEGIN → Resolve/Create Document → Supersede old → Insert new Revision → File/Link → Events → Action=completed → COMMIT. `Action completed` gehört in DIESELBE SQLite-Transaction wie die fachlichen Cache-Schreibvorgänge.
- **Slice T5 – idempotenter Apply-Pfad:** Action stellt fest (Source/Temp/Destination/Archiv vorhanden? Hash? Revision angelegt? superseded? File gelinkt? Events?) und stellt nur den fehlenden Teil her. `Apply(action) { EnsureDiskState; EnsurePlanCacheState; CompleteAction; }`
- **Slice T6 – Recovery Forward auf denselben Apply-Pfad:** `RecoveryExecutorService.ExecuteForward()` verliert seine vereinfachte Eigenlogik → `foreach (pending) actionExecutor.Apply(action)`. Erst jetzt ist „idempotente Recovery" aus ADR-061 erfüllt.
- **Slice T7 – failed / rollback / cleanup:** `pending` = recovery-pflichtig. Journal-Header NICHT sofort terminal `failed`, solange Seiteneffekte ungeklärt. `failed` erst nach erfolgreichem Rollback oder explizitem Cleanup/bewusster Abbruchentscheidung. Falls Schemaänderung nötig: Frühphase → planmanager.db löschen, keine Migration.
- **Slice T8 – Undo + Crash-Matrix:** Integrationstests für Abbruch nach Journal / Action-Plan / Archiv / Temp-Transfer / final rename / Supersede / Revision-Insert / vor Action completed / vor Journal completed — jeweils Forward, Rollback, Undo-nach-Erfolg. Erst danach „wasserdicht".

## 3. Reihenfolge BPM-111 vs Phase A
Korrektur der früheren Pauschalaussage: **BPM-111.06 fertigstellen, unmittelbar danach Phase A einschieben.** Nicht mitten im Slice abbrechen. Danach BPM-111 in allem pausieren, was zur Importausführung weiterführt, bis T1–T8 stehen.
Begründung: Der ADR-059-Pfad ist real verdrahtet: `ManualCaptureView → ConfirmImportCommand → CaptureConfirmService → ImportExecutionService.Execute()` — der neue Radial-Workflow erzeugt echte Imports über die problematische Strecke. Der klassische `OnStartImport()` existiert weiterhin. Risiko heute real erreichbar.
PDF-Port-Arbeit (ADR-062/063, PdfiumPdfService): darf separat weiterlaufen, wenn sie den Importpfad nicht berührt und Phase A nicht verzögert. Nicht im selben Änderungsstrom vermischen.

## 4. Quick-Fixes
- **Skip-only Early Return:** sofort fixbar + Test. ABER: unter ADR-059 nicht das ganze Dublettenproblem — `ManualCaptureViewModel` legt Bucket-A-Dubletten gar nicht als PendingAssignment ab; `CaptureConfirmService.ConfirmAll()` sieht nur Pending Assignments. Alten Bug fixen ja (wenn klassischer Pfad erreichbar bleibt); V1-Dublettenverhalten des ADR-059-Flows separat definieren/testen.
- **`archive_path`:** NICHT jetzt quick-fixen. `ArchiveExistingFile()` erzeugt Timestamp-Pfade ad hoc; ADR-061 P5 verlangt ohnehin staged/journalisierten Ablauf → gehört in T2/T3, sonst Doppelarbeit.
- **`IsConflict`:** technisch einfacher Fix, aber durch ADR-059 ist `DocumentTypeRecognizer` nicht mehr V1-Kernentscheidungsweg (`ManualFirstCaptureService` profilunabhängig, eigene Bucket-D-Konflikte). Nur sinnvoll wenn klassischer Profilimport in V1 sichtbar bleibt.
- Priorisierung: Skip-only ja · IsConflict optional/niedrig · archive_path nein (T2/T3) · failed-Semantik nein (T7) · Recovery Forward nein (T6).

## 5. Überholte Punkte 6 / 9 / 10
- **Punkt 6 (ChangedSameIndex/OlderRevision):** überholt — ADR-059 modelliert es bereits: gleicher Index + anderer Inhalt → Bucket D Conflict; niedrigerer Index → Bucket B UpdateProposal mit OLDER_REVISION-Warnung in Reason. `.06`-Detail-Panel zeigt Reason, „⬆ Update übernehmen" / `TakeUpdate()` erzeugt PendingAssignment. Verbleibender V1-Prüfpunkt: OLDER_REVISION-Warnung visuell ausreichend deutlich vor „Update übernehmen" (UX-Härtung im ADR-059-Panel).
- **Punkt 9 (ImportPreview ausbauen):** für V1 weitgehend obsolet. Keine Zeit mehr in Rechtsklick/UNKNOWN-Zuweisung/Conflict-Auswahl im alten Dialog. Alter Dialog = Legacy-/Debug-Pfad oder später entfernen.
- **Punkt 10 (LearnIndex):** als Profil-Lernfeature obsolet (Strategie B: Assist, nie Entscheider — automatisches Profil-Umschreiben wäre Rückweg zu Strategie A). Kein V1-MUSS.
- **Was als echte V1-Lücke im ADR-059-Workflow bleibt:** (1) Bucket A finale Dublettenbehandlung definieren, (2) Bucket B OLDER_REVISION klar warnen + bewusst übernehmen, (3) Bucket D erst nach manueller Auflösung in Pending, (4) finaler Confirm: nur bestätigte Pending Assignments mutieren, (5) gemeinsamer Execution-Pfad transaktional/recovery-fähig.

## 6. Dateibrowser
- **Konsens** mit ADR-060/061: Explorer liest echtes FS, `.bpm` verstecken, Projektroot-Sandbox, kein DB-Vollspiegel, externe Änderungen tolerieren, getrackte Pläne fachlich behandeln, zentralisierte Dateioperationen. „DB = kuratierter Index / Explorer = Live-Sicht" ist besser als genereller BPM-Dateiindex.
- `ProjectFileOperationService` heute enger schneiden: NICHT jede Explorer-Operation journalisieren. Ungetrackte Datei → normal über `IFileSystemWriter`; getrackter Plan → nur fachlicher Journal-Service. Routing: tracked? nein → FS-Ports / ja → Plan-/Journal-Operation. Klassennamen nicht festschreiben.
- **Delta „fachliche Zuordnung ändern?"-Dialog:** nicht ADR-beschlossen, für V1 möglicherweise zu gefährlich. ADR-061 konservativer (getrackte Dateien nur über Journal-Service). Physischer Move der die Identität ändert = Reklassifizierung/Metadatenänderung = eigener Domain-Workflow, nicht vorab in den Explorer.
- **FileSystemWatcher:** höchstens UI-Refresh, nie Wahrheit — kompatibel. Für V1 nicht nötig.
- **Diagramm 13:** jetzt noch nicht — erst wenn BPM-112 Slices 1–5 stabil + Explorer-Task startet, dann mit echten Verantwortlichkeiten (Explorer UI → Live FS Reader → Tracked-State Lookup → untracked/tracked Operation → Drift/Reconcile).

## Konsequenz für die Prioritäten
BPM-111.06 abschließen → Import-Transaktions-Härtung → BPM-111 V1-Workflow fertigstellen → restliche BPM-112-Portmigration → In-App-Explorer. PDF-Arbeit separat, solange Importpfad unberührt.
Selbstkorrektur: nicht mehr „keine weiteren PlanManager-Features überhaupt", sondern: keine weiteren Features, die auf dem mutierenden Import-/Undo-/Recovery-Pfad aufbauen, bevor dieser die ADR-058/061-Invarianten erfüllt. Reine UI-/Preview-/PDF-Arbeit entkoppelt.

## ✅ Einigkeit
- Recovery-/Journal-Probleme real; ADR-058-Stop-Punkte machen sie V1-kritisch.
- ADR-061 P5 hat den transaktionalen Dateiansatz beschlossen; ADR-060 Slice 3 ist Voraussetzung für Testbarkeit.
- `archive_path`, Vorab-Journalisierung, Recovery müssen gehärtet werden.
- ADR-059-Workflow nutzt heute den unsicheren `ImportExecutionService`.
- Explorer = Live-FS + kuratierter Planindex; getrackte Pläne nur über journalisierte Operationen.
- Keine Migrationen; bei Schemaänderung Reset.

## ⚠️ Widerspruch (Selbstkorrekturen)
- `ImportActionExecutor` war teilweise als neue Architektur formuliert; richtig: Implementierungsform beschlossener Invarianten + DB-seitige Präzisierung.
- Preview-Priorisierung 6/9/10 durch ADR-059 großteils überholt.
- LearnIndex-Profil-Lernen kein V1-MUSS.
- `ProjectFileOperationService` nicht pauschal alles journalisieren.
- „Fachliche Zuordnung ändern?"-Dialog nicht in Explorer V1.
- `planmanager.db` ist Cache+Journal, nicht SoR.

## ❓ Rückfragen
1. Bleibt der klassische `OnStartImport`/`ImportPreviewDialog` in V1 erreichbar, oder wird er mit Abschluss des ADR-059-Flows deaktiviert? (Bestimmt V1-Priorität von SkipIdentical-/IsConflict-Fixes im alten Pfad.)
2. Bucket A: Sollen bestätigte Dubletten beim finalen Import aus `_Eingang` gelöscht werden, oder bewusst verbleiben, bis der User sie separat verwirft? Sollte vor dem Executor-Härtungstest als fachliche Invariante feststehen.
