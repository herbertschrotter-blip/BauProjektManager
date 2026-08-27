# Review Runde 3 — Sign-off: Import-Transaktions-Härtung

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User).
- Sprich direkt zu deinem Kollegen (Claude), NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 3"
- Fasse am Ende zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** herbertschrotter-blip/BauProjektManager
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!

## Stand

Herbert hat nach Runde 2 final entschieden. Ich habe deine Runde-2-Befunde vollständig verifiziert — insbesondere alle fünf `ImportUndoService`-Probleme (der bedingungslose DB-Rollback nach Dateifehlern inkl. `MarkImportUndone` ist bestätigt) und `destination_path NOT NULL`. Deine Diagramm-Nachlieferung 01–09 hat keinen übersehenen V1-kritischen Befund ergeben — damit ist auch meine Offenlegung aus Runde 2 abgehakt.

**Herberts Entscheidungen:**
1. **Eigener Task „Import-Transaktions-Härtung"** mit deiner revidierten Slice-Folge H0 + T0–T8, BPM-112 Slice 3 = T1. Start direkt nach Abschluss des laufenden BPM-111.06-Slices.
2. **Bucket A: direktes Delete, bewusst nicht undo-bar** — `action_type = skipDuplicate`, journalisiert + recovery-fähig, `destination_path` wird nullable (planmanager.db-Reset, keine Migration). Kein Papierkorb.
3. Diese Runde ist das **beidseitige Sign-off** — danach wird die Serie geschlossen und das Ergebnis in ADR + ClickUp-Task überführt.

## Zu signierendes Gesamtergebnis

### Slice-Folge (Task „Import-Transaktions-Härtung")

```text
H0  V1 Import Route Cutover
    („Import starten"/OnStartImport/ImportPreviewDialog raus aus V1-Pfad,
     Legacy-Klassen bleiben vorerst im Repo)
T0  Characterization Tests (Happy Path New/Update, Zeitinvariante
    superseded_at==current_from, CaptureConfirm-Mapping, Journal-Ist,
    Undo-Happy-Path; bekannte Fehler NICHT als Soll festschreiben)
T1  ADR-060 Slice 3: FS-Ports + Fault-fähiger FakeFileStore
    + lokale Constructor Injection (kein Composition-Root-Umbau)
T2  vollständiger Action-Plan vor erster Mutation
    (deterministische source/destination/archive-Pfade, inkl. skipDuplicate)
T3  ADR-061-Disk-Protokoll (.bpm_tmp, atomic final rename,
    3× Lock-Retry, deterministische Archivpfade)
T4  DB-Transaction pro fachlicher Action + idempotenter DB-Apply
    (Action completed in DERSELBEN Transaction wie die Fach-Writes)
T5  Recovery Forward über denselben Apply-Pfad
    (RecoveryExecutorService verliert seine Eigenlogik)
T6  failed/pending + Rollback/Cleanup-Semantik
    (failed erst terminal nach Rollback oder bewusstem Cleanup)
T7  Undo-Härtung (Preflight → Reverse Disk LIFO → NUR bei vollem
    Disk-Erfolg DB-Rollback in Transaction → undone;
    ApplyForward/ApplyReverse als gemeinsamer Kern)
T8  Fault-/Crash-Matrix + Integrationssuite
    (Abbruch nach jedem Schritt × Forward/Rollback/Undo)
```

### Invarianten (Vorschlag für ADR-Verankerung)

1. Eine journalisierte ImportAction muss aus jedem zulässigen Zwischenzustand idempotent auf den definierten Endzustand gebracht werden können — Dateisystem UND Plan-Cache. (Klassenname ist Implementierungsdetail.)
2. Alle geplanten Actions stehen VOR der ersten Mutation vollständig im Journal (inkl. `archive_path`).
3. `action_status = completed` wird in derselben SQLite-Transaction gesetzt wie die zugehörigen Fach-Writes.
4. Recovery Forward und normaler Import nutzen denselben Apply-Pfad.
5. Undo: DB-Rollback + `MarkImportUndone` NUR nach vollständig erfolgreichem Disk-Reverse; sonst bleibt der Vorgang reparierbar.
6. `pending` = recovery-pflichtig; `failed` erst terminal nach Rollback oder bewusster Abbruchentscheidung.
7. `skipDuplicate`: journalisiert + recovery-fähig, bewusst nicht undo-bar (journalisiert ≠ undo-bar). Recovery-Endzustand: redundante Inbox-Kopie existiert nicht mehr UND Bestand mit gleichem MD5 verifiziert (Lookup über die getrackte Teilmenge nach ADR-061 Modell A, kein Verzeichnis-Scan); sonst RecoveryConflict.
8. Frühphase: Schemaänderungen (z.B. `destination_path` nullable) via DB-Reset, keine Migration.
9. Skip-only-Fix und `IsConflict`-Fix sind mit H0 aus der V1-Priorität gestrichen.
10. PDF-Port-Arbeit (ADR-062/063) darf parallel laufen, solange sie den Importpfad nicht berührt.
11. Dateibrowser/In-App-Explorer: nach ADR-061 Modell A, erst nach stabilen Ports (ADR-060 Slice 6); Reklassifizierung getrackter Pläne ist ein eigener Domain-Workflow, kein Explorer-Feature; Diagramm 13 erst bei Feature-Start.

## Aufgabe

1. **Sign-off oder konkrete Einwände:** Prüfe die Slice-Folge und die 11 Invarianten. Wenn du zustimmst, gib ein explizites Sign-off. Wenn nicht, benenne den konkreten Punkt mit Alternativvorschlag — keine neuen Baustellen.
2. **Akzeptanzkriterien:** Schlage 10–15 testbar formulierte Akzeptanzkriterien für den neuen Task vor (Muster: „Wenn der Prozess nach dem atomic rename und vor dem DB-Commit abbricht, stellt Recovery Forward die Revision her und markiert die Action completed"). Sie werden 1:1 in das ClickUp-Ticket übernommen.
3. **Vollständigkeits-Check:** Letzte Chance — gibt es aus deiner gesamten 12-Diagramm-Analyse plus beiden Review-Runden noch einen Punkt, der weder in den Task noch in die bewusst-post-V1-Liste gefallen ist und vor V1 geklärt sein muss? Wenn nein, sag das explizit.

Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.
