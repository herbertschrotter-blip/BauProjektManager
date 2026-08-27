# Runde 2 — Claude-Analyse der ChatGPT-Antwort

**Datum:** 2026-08-27
**Gesamturteil:** Beste Runde bisher. Die Diagramm-Nachlieferung ist ehrlich (inkl. Eingeständnis der SoR-Fehlinterpretation in Diagramm 04), die T0/T1-Korrektur übernimmt meinen Einwand sauber, und der Undo-Befund ist ein NEUER, verifizierter V1-kritischer Fund, der die T7-Entscheidung trägt. Der H0-Cutover-Vorschlag ist ein echter Gewinn.

## Verifikation der neuen Code-Behauptungen

### ImportUndoService.cs — alle 5 Probleme bestätigt

| # | Behauptung | Befund |
|---|---|---|
| 1 | Direktes System.IO | ✅ `File.Move`/`Directory.CreateDirectory` (Z. 92/94/101) |
| 2 | Undo selbst nicht crash-sicher | ✅ Kein journalisierter Undo-Fortschritt; sequentielle Moves ohne Recovery-Spur |
| 3 | **DB-Rollback läuft nach Dateifehlern trotzdem weiter** | ✅ Z. 104–108: `catch → errors.Add`, danach laufen Z. 113–128 (`SoftDeleteRevision`/`RestoreRevisionToCurrent`/`SoftDeleteDocumentIfNoRevisions`/`MarkImportUndone`) **bedingungslos**. Nuance: Der Preflight (Z. 68) fängt viele Fälle vorab, schützt aber nicht gegen Fehler WÄHREND der Schleife (Lock, TOCTOU, Crash). Befund gültig — das ist der ADR-058-Stop-Fall „Undo wackelt" |
| 4 | Keine DB-Transaction | ✅ Mehrere unabhängige `_db`-Calls |
| 5 | skipDuplicate passt nicht in die Annahmen | ✅ Preflight verlangt existierende `DestinationPath` (Z. 44), Undo macht Destination→Source (Z. 94) — für eine Delete-Action sinnlos. Kommentar Z. 13–14 bestätigt die bisherige Intention: „SKIP-Aktionen sind nicht undo-bar (werden gar nicht journaliert)" |

### Weitere Stichproben
- `import_actions.destination_path NOT NULL` — ✅ bestätigt (`PlanManagerDatabase.cs:260`); die vorgeschlagene Nullable-Änderung + DB-Reset ist Frühphasen-konform.
- `project_participants.contact_id` ohne Contacts-Tabelle — ✅ bestätigt (`ProjectDatabase.cs:233`, `ProjectParticipant.cs` Kommentar „Später: zentrales Adressbuch"). Bewusster Zukunftsbezug, kein Fehler.

### Diagramm-Nachlieferung 01–09 (Aufgabe A)
Plausibel und selbstkritisch. Die vorher nicht in die Gesamtauswertung übernommenen Punkte sind sämtlich klein (registry.json-Vertrag, contact_id, veralteter RevisionDecisionService-Kommentar, N:M-Reserve, plan_context_links-Spannung) — **kein übersehener V1-kritischer Befund**. Die drei Selbstkorrekturen (Diagramm 02/04 SoR; Diagramm 03 „PlanManagerDatabase-Verschiebung nicht zwingend"; Diagramm 07/08 Gewichtung auf ADR-059-Pfad) decken sich mit meiner Einschätzung.

## Zustimmung (Claude)

1. **H0 (V1 Import Route Cutover) vor T0** — sehr guter Vorschlag: reduziert den zu härtenden Code auf genau einen Pfad, schrumpft Testmatrix und Risiko. Kleiner Slice, sofort machbar.
2. **T0/T1-Korrektur** — mein Einwand vollständig übernommen: T0 = reine Charakterisierung (Happy Paths, Zeitinvariante, CaptureConfirm-Mapping, Journal-Ist, Undo-Happy-Path), Fault-Injection erst mit FakeFileStore nach T1. Wichtig auch: bekannte Fehler NICHT als Soll-Verhalten festschreiben.
3. **T7 Undo-Härtung als eigener Slice** — durch die 5 verifizierten Probleme klar begründet; „Reverse Disk LIFO → nur bei vollständigem Disk-Erfolg DB-Rollback in Transaction" ist die richtige Reihenfolge. `ApplyForward`/`ApplyReverse` als gemeinsamer Kern ohne Framework: einverstanden.
4. **DI in T1 als lokale Constructor-Injection** — pragmatisch richtig; kein Composition-Root-Großumbau vor V1.
5. **Bucket-A-Journalmodell (C1/C3)** — `skipDuplicate` als echte Action mit MD5-Evidenz, Recovery-Semantik über den fachlichen Endzustand („redundante Inbox-Kopie existiert nicht mehr, Bestand verifiziert") inkl. `RecoveryConflict`-Fall ist sauber. Slice-Zuordnung T2→T3/T4→T5→T7 passt.
6. **Delete statt Papierkorb (C2)** — ich schließe mich ChatGPT an: MD5-identisch heißt, es geht keine einzigartige Information verloren; der Papierkorb-Lifecycle (große Dateien, Cloud-Sync, Cleanup) kauft nur ein kosmetisches Inbox-Undo. „Journalisiert ≠ undo-bar" ist die richtige Trennung und deckt Herberts r1-Intention („nachvollziehbar") ab — nur die Wiederherstellung der Inbox-Kopie entfällt. **Achtung: Das ist eine leichte Präzisierung von Herberts r1-Entscheidung → als Entscheidungspunkt vorgelegt.**

## Anmerkungen (Claude, klein)

- ChatGPTs Recovery-Prüfung „existiert Bestand mit diesem MD5?" braucht einen Hash-Lookup im Planbestand — `plan_files` führt MD5, das ist abgedeckt; nur sollte der Lookup über die getrackte Teilmenge gehen (ADR-061 Modell A), nicht über einen Verzeichnis-Scan.
- Die Slice-Nummerierung hat sich zwischen r1 und r2 verschoben (r1: T7=failed, T8=Undo+Matrix → r2: T6=failed, T7=Undo, T8=Matrix). Für das Ticket gilt die r2-Fassung H0+T0–T8.

## Entscheidungspunkte für Herbert (Stufe A)

1. **Task-Schnitt final:** eigener Task „Import-Transaktions-Härtung" mit H0+T0–T8 (BPM-112 Slice 3 = T1) — jetzt festziehen? ChatGPT und Claude empfehlen das übereinstimmend; Reihenfolge: nach Abschluss BPM-111.06.
2. **Bucket A präzisieren:** direktes Delete + explizit NICHT undo-bar (journalisiert + recovery-fähig) — oder doch Papierkorb-Move für vollständiges Undo?
3. **Wie weiter mit der Serie:** Runde 3 als kurzer beidseitiger Sign-off (Slice-Folge + Invarianten fixieren) und dann abschließen — oder direkt abschließen?
