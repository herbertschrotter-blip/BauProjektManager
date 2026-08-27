# Runde 1 — Claude-Analyse der ChatGPT-Antwort

**Datum:** 2026-08-27
**Gesamturteil:** Sehr starke Runde. ChatGPT korrigiert sich an den richtigen Stellen selbst, das Delta zwischen beschlossenen ADRs und offenem Bedarf ist präzise, und der T0–T8-Slice-Plan ist direkt umsetzbar. Ich stimme in fast allen Punkten zu.

## Verifikation der neuen Code-Behauptungen

| ChatGPT-Behauptung | Befund |
|---|---|
| Radial-Confirm läuft über den unsicheren Executor (`ManualCaptureView → CaptureConfirmService → ImportExecutionService.Execute()`) | ✅ Bestätigt — `CaptureConfirmService.cs:26` (`new ImportExecutionService(db, idGenerator)`) + `:46` (`_execution.Execute(decisions, ...)`) |
| `ConfirmAll()` sieht nur Pending Assignments; Bucket-A-Dubletten laufen nicht durch Execute | ✅ Konsistent — `ConfirmAll` baut Decisions ausschließlich aus dem `PendingAssignmentStore` |

Das Kernargument für die Priorisierung trägt also: **Der neue V1-Workflow erzeugt heute reale Imports über den ungehärteten Pfad.**

## Zustimmung (Claude)

1. **Delta-Tabelle Executor (Frage 1):** korrekt und vollständig. Besonders gut: die Formulierung der Architektur-Invariante („journalisierte ImportAction muss aus jedem zulässigen Zwischenzustand idempotent auf den Endzustand gebracht werden — Dateisystem UND Plan-Cache") statt einer Klassen-Vorgabe. Das gehört so als ADR-Ergänzung formuliert (ADR-061-Addendum oder eigenes ADR).
2. **Task-Schnitt (Frage 2):** eigener Task „Import-Transaktions-Härtung" mit BPM-112 Slice 3 als Teilmenge — richtig, hält BPM-112 semantisch sauber. T4 (Action=completed in DERSELBEN SQLite-Transaction wie die fachlichen Writes) ist der wichtigste Einzelpunkt des Plans.
3. **Reihenfolge (Frage 3):** .06 fertigstellen → Härtung als Mainline → BPM-111-Rest. PDF-Arbeit parallel ok (berührt Importpfad nicht). Deckt sich mit meiner Einschätzung.
4. **Quick-Fix-Triage (Frage 4):** differenziert und richtig — insbesondere die Warnung, `archive_path` NICHT vorab zu patchen (Doppelarbeit mit T2/T3) und der Hinweis, dass der Skip-only-Fix das Bucket-A-Verhalten im neuen Flow nicht mitlöst.
5. **Punkte 6/9/10 (Frage 5):** die Selbstkorrektur ist sauber; die 5-Punkte-Restliste („echte V1-Lücken im ADR-059-Workflow") ist präziser als die alte Liste und sollte die alte ersetzen.
6. **Dateibrowser (Frage 6):** Konvergenz auf ADR-061 Modell A inkl. Rücknahme des „fachliche Zuordnung ändern?"-Dialogs (→ eigener Reklassifizierungs-Workflow, später). Diagramm 13 verschieben — einverstanden.

## Kleinere Anmerkungen (Claude)

- **T0 vs. T1 Reihenfolge:** Fault-Injection-Seams (T0) sind gegen echtes `System.IO` kaum sinnvoll injizierbar — praktisch wird T0 erst mit `FakeFileStore` nach T1 voll wirksam. Ich würde T0 auf „Charakterisierungstests des Ist-Verhaltens" begrenzen und die Injektionspunkte in T1 mitnehmen. Kein Widerspruch, nur Präzisierung — ggf. in Runde 2 klären.
- **Undo:** T8 testet Undo, aber kein Slice härtet `ImportUndoService` selbst. Vermutlich reicht T2/T3 (korrekte `archive_path`-Journalisierung) als Zulieferung — sollte in Runde 2 explizit bestätigt werden.
- **Composition-Root-Detail:** `CaptureConfirmService` instanziiert `ImportExecutionService` per `new` — bei der Härtung sollte der gemeinsame Executor via DI kommen, sonst entstehen zwei Instanz-Welten (klassisch vs. Radial).

## Wichtig für Runde 2 (Auftrag von Herbert)

**Offenlegung:** Claude hat NICHT die vollständige Diagrammserie gesehen — der geteilte Chat enthielt nur Diagramme 10–12 + Gesamtauswertung + Dateibrowser-Diskussion. Die Diagramme 01–09 (Systemkontext, Container, Solution, Persistenz, beide ERDs, Komponenten, Importflow, Importsequenz) und deren Einzelbefunde sind Claude nur über Verweise in der Gesamtauswertung bekannt. In Runde 2 wird ChatGPT damit konfrontiert und soll die verifizierbaren Kernbefunde aus 01–09 kompakt nachliefern, damit nichts Relevantes an der Auswertung vorbeigeht.

## Entscheidungspunkte für Herbert (Stufe A)

1. **Klassischer Profil-Import in V1:** deaktivieren (ADR-059-Flow ist die V1-UX) oder als Legacy erreichbar lassen? → bestimmt V1-Priorität von Skip-only-/IsConflict-Fix.
2. **Bucket-A-Dubletten:** beim finalen Confirm aus `_Eingang` entfernen, dort belassen bis der User sie verwirft, oder ChatGPT-Empfehlung einholen? (ChatGPTs Rückfrage 2 — muss als fachliche Invariante VOR T8 feststehen.)
3. **Task-Schnitt:** ChatGPTs Vorschlag (neuer Task „Import-Transaktions-Härtung", T0–T8, BPM-112 Slice 3 als Teilmenge) übernehmen oder BPM-112 erweitern?
