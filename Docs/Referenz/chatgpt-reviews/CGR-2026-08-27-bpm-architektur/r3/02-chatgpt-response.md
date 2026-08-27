# Review Runde 3 — ChatGPT-Antwort (Canvas)

> Quelle: ChatGPT GPT-5.4, Canvas „Review Runde 3", 2026-08-27. 1:1 archiviert.

---

Kollege, ich habe die zu signierende Fassung nochmals gegen `feature/planmanager-v1` geprüft, insbesondere `PlanManagerDatabase`, `ImportExecutionService`, `RecoveryExecutorService`, `RecoveryDecisionService`, `ImportUndoService` und den MD5-Lookup.

## 1. Sign-off

**SIGN-OFF: Ja.**

Ich stimme der Slice-Folge H0 + T0–T8 und den 11 Invarianten inhaltlich zu. Der Schnitt ist konsistent mit ADR-058, ADR-060 und ADR-061 und löst genau die bereits identifizierten Stop-Punkte, ohne eine neue Architektur daneben zu erfinden. Zwei kleine Präzisierungen, beide nicht blockierend.

### Präzisierung A — `skipDuplicate` und Undo
Invariante 7 explizit ergänzen:
```text
skipDuplicate wird von Undo nicht reversed.

Gemischter Import:
→ alle undo-fähigen Actions werden rückgängig gemacht
→ skipDuplicate bleibt gelöscht
→ Journal darf danach als undone gelten

Import ausschließlich aus skipDuplicate-Actions:
→ kein Undo anbieten
```
Damit ist eindeutig definiert, was `undone` bei einem gemischten Import bedeutet. Sonst stößt T7 beim heutigen `GetLastCompletedImportId()`/`Preflight()` irgendwann auf einen reinen Dubletten-Import und müsste die Semantik erst dort erfinden.

### Präzisierung B — nullable `destination_path`
Im aktuellen Schema sind BEIDE Spalten NOT NULL: `import_actions.destination_path` UND `import_action_files.destination_path`. Mindestens `import_actions.destination_path` muss für skipDuplicate nullable werden; falls T2 zusätzlich `import_action_files` verwendet, auch dort. Wie beschlossen: planmanager.db löschen → neu erzeugen, keine Migration.

## 2. Akzeptanzkriterien für den ClickUp-Task (15, zur 1:1-Übernahme)

1. **V1-Importweg eindeutig:** Nach H0 ist der klassische Profil-Import über „Import starten" / `OnStartImport` / `ImportPreviewDialog` im normalen V1-UI nicht mehr erreichbar. Der Radial-/Bucket-Workflow ist der einzige produktive V1-Importweg; Legacy-Klassen dürfen im Code verbleiben.
2. **Ist-Verhalten abgesichert:** Vor der Semantikänderung existieren grüne Characterization-Tests für New-Import, Update-Import, `CaptureConfirmService`-Mapping, Journal-Happy-Path, Undo-Happy-Path und die Zeitinvariante `old.superseded_at == new.current_from`. Bekannte Fehler werden nicht als gewünschtes Verhalten festgeschrieben.
3. **FS-Ports im Hochrisikopfad:** `ImportExecutionService`, Recovery-/Undo-Dateioperationen und Pfadoperationen verwenden `IFileSystemReader`/`IFileSystemWriter`/`IPathService`; `CaptureConfirmService` erzeugt `ImportExecutionService` nicht mehr intern per `new`. Der Fake-Dateispeicher kann gezielt Dateioperationen fehlschlagen lassen.
4. **Vollständige Vorab-Journalisierung:** Bei N geplanten Actions existieren Journal-Header und alle N `import_actions` vollständig, bevor die erste Datei erstellt/verschoben/archiviert/umbenannt/gelöscht wird. Ein Abbruch unmittelbar danach hinterlässt einen vollständigen Recovery-Plan.
5. **Deterministischer Archivpfad:** Bei einer Update-Action ist `archive_path` vor der ersten Dateimutation festgelegt und journalisiert. Recovery und Undo verwenden exakt diesen Pfad; kein ad-hoc-Archivname während der Ausführung.
6. **Bucket A ist echte Action:** Bestätigte MD5-Dublette erzeugt `action_type = skipDuplicate` mit Source-Pfad, MD5 und Dateigröße im Journal — nicht mehr unjournalisierter `File.Delete` nach dem Action-Loop. Gemischter Import journalisiert Duplicate-, New- und Update-Actions gemeinsam vor der ersten Mutation.
7. **ADR-061-Disk-Protokoll umgesetzt:** Eingehende Datei zunächst nach `.bpm_tmp` im Zielbereich, dann finaler Rename. Lock-/Sharing-Verletzungen: max. 3× Retry nach beschlossener Regel. Keine 2PC-/Distributed-Lock-Mechanik.
8. **Crash nach Archivierung recoverbar:** Abbruch nachdem der Vorgänger am journalisierten `archive_path` liegt, aber vor Veröffentlichung der neuen Datei → Recovery Forward stellt den Endzustand her, ohne zweite Archivkopie und ohne Verlust des Vorgängers.
9. **Crash nach finalem Rename recoverbar:** Abbruch nach atomic rename, vor DB-Commit → Recovery Forward erkennt den Dateizustand, schreibt die fehlende Dokument-/Revision-/File-/Event-Struktur, setzt Action auf `completed`, ohne die Datei erneut zu verschieben.
10. **DB-Action atomar:** Fachliche DB-Writes einer Action und `action_status = completed` laufen in derselben SQLite-Transaction. Bei injiziertem Fehler bleiben weder partielle Änderungen noch eine `completed`-Action zurück.
11. **Crash nach DB-Commit recoverbar:** Abbruch nach Action-Commit, vor Journal-Abschluss → Recovery erkennt den vollständigen Action-Zustand und finalisiert nur das Journal. Keine zweite Revision, kein zweites `plan_file`, keine doppelten Links/Events.
12. **Recovery Forward idempotent:** Derselbe Forward-Vorgang kann auf denselben unterbrochenen Import mindestens zweimal laufen, ohne zusätzliche Archivdateien, Revisionen, Files, Links oder Statuswechsel. Normaler Import und Recovery Forward verwenden denselben fachlichen Apply-Pfad.
13. **Recovery-pflichtige Fehler bleiben sichtbar:** Nach möglicher Seiteneffekt-Erzeugung bleibt der Import `pending` und blockiert neuen Confirm. `failed` erst terminal nach vollständigem Rollback oder bewusstem Cleanup/Abbruch. Scheitert Rollback, wird der Vorgang nicht fälschlich als sauber markiert.
14. **Undo transaktional sicher:** Preflight → undo-fähige Datei-Reverses in LIFO. Scheitert irgendein erforderlicher Disk-Reverse: keine Revision soft-deleted/restored, kein `MarkImportUndone`. Erst nach vollständig erfolgreicher Disk-Phase DB-Rollback in einer SQLite-Transaction, dann `undone`.
15. **`skipDuplicate` recovery-fähig, nicht undo-bar:** Pending + Source vorhanden + MD5 als getrackter Bestandsinhalt nachweisbar → löschen + abschließen; Source weg + MD5 im getrackten Bestand → idempotent als erfüllt; fehlt auch der Bestandsnachweis → Konflikt, nicht `completed`. Undo stellt skipDuplicate nicht wieder her; reiner skipDuplicate-Import wird nicht als undo-fähig angeboten.

Deckt H0–T8 vollständig ab; prüfbar mit `FakeFileStore` + Temp-Verzeichnis-/SQLite-Integrationstests.

## 3. Vollständigkeits-Check

Kein weiterer unzugeordneter V1-kritischer Recovery-/Persistenzpunkt aus der 12-Diagramm-Analyse.

**Aber ein bereits beschlossener V1-MUSS-Punkt AUSSERHALB dieses Tasks** (beim finalen V1-Sign-off von BPM-111 prüfen): **ADR-059 verlangt ID-basierten `document_key`.** Der gepushte `CaptureConfirmService.BuildManualDocumentKey()` enthält derzeit noch den Übergang `DocumentTypeId + PlanNumber + normalisierte BuildingPart-/Level-NAMEN` — mit Kommentar, dass Stammdaten-IDs das Ziel sind. Gehört nicht in die Import-Transaktions-Härtung, sondern ist bestehender ADR-059/BPM-111-Abnahmepunkt: `PendingAssignment` + `CaptureConfirmService` müssen am Ende `document_type_id + building_part_id [+ building_level_id] + plan_number` verwenden. Ebenso bleibt Bucket-B/D-Warnung/Auflösung BPM-111-UX-Arbeit, nicht dieser Task.

**Vollständigkeitsurteil:** Für den neuen Task fehlt nichts. Kein unowned V1-Blocker aus der 12-Diagramm-/Review-Serie. Der ID-basierte `document_key` bleibt als bestehender BPM-111-V1-Abnahmepunkt offen.

## ✅ Einigkeit
Vollständiges Sign-off für H0+T0–T8 und die 11 Invarianten. Eigener Task richtig; BPM-112 Slice 3 = T1/Teilmenge. Alle Actions vor Mutation; deterministischer archive_path; .bpm_tmp + atomic rename + 3× Retry; DB-Writes + completed in einer Transaction; gemeinsamer Apply-Pfad; pending = recovery-pflichtig; Undo eigener Slice; skipDuplicate journalisiert/recovery-fähig/nicht undo-bar; kein Papierkorb, keine Migration, keine 2PC-/Lock-/Sync-Sonderarchitektur; klassischer Profilimport vor der Härtung stillgelegt.

## ⚠️ Widerspruch
Keine architektonischen Widersprüche. Zwei textuelle Präzisierungen: (A) Undo-Semantik von skipDuplicate explizit festhalten (inkl. reiner Duplicate-Imports); (B) ggf. auch `import_action_files.destination_path` nullable.

## ❓ Rückfragen
Keine. Die Review-Serie ist aus ChatGPT-Sicht sign-off-fähig und abgeschlossen.
