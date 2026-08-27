# CGR-2026-08-27-bpm-architektur — Gesamtauswertung-Review: Recovery-Befunde vs. ADR-058–061

**Thema:** Review der ChatGPT-Gesamtauswertung (12-Diagramm-Serie, externer ChatGPT-Chat „Softwareentwicklungsdiagramme") gegen den beschlossenen Architektur-Rahmen. Kernfrage: Wie ordnen sich die 🔴-Recovery-/Journal-Befunde in ADR-060 (FS-Ports, BPM-112), ADR-061 (Transaktionalität, BPM-113) und den laufenden BPM-111-Track ein? Plus Dateibrowser-Konzept vs. ADR-061 Modell A.
**Zeitraum:** 2026-08-27
**Ursprungs-Chat:** ChatGPT-Share „Softwareentwicklungsdiagramme" (Diagramme 10–12 + Gesamtauswertung + Dateibrowser-Diskussion); Claude-Verifikation der Befunde gegen Code-Stand v0.28.120
**Status:** Runde 3 (Sign-off) offen

---

## Ausgangslage

ChatGPT hat in einem separaten Chat 12 Architekturdiagramme + eine Gesamtauswertung mit 37 Punkten (🔴 6 V1-Blocker / 🟠 4 fachliche Lücken / 🟡 Aufräumpunkte / 🟢 13 Stärken) erstellt. Claude hat die fünf gewichtigsten 🔴-Behauptungen gegen den Code verifiziert — **alle bestätigt**:

| Befund | Code-Beleg (Stand v0.28.120) |
|--------|------------------------------|
| Actions einzeln statt vorab journalisiert | `ImportExecutionService.cs:126` (InsertImportAction pro Aktion) |
| `archive_path` als null journalisiert | `ImportExecutionService.cs:134` |
| Skip-only-Bug (nur Duplikate → Eingang bleibt liegen) | Early-Return `ImportExecutionService.cs:39` vor Skip-Bereinigung Z. 72 ff. |
| Recovery-Hook sieht nur `status='pending'` | `PlanManagerDatabase.cs:831` + `GetPendingImports()` |
| `IsConflict` zu grob | `DocumentTypeRecognizer.cs:23` (`AllMatches.Count > 1`) |

Zusätzlicher Claude-Befund: **ADR-061 Punkt 5** (Journal VOR Move + temp `.bpm_tmp` + atomic rename + idempotente Recovery) ist **beschlossen, aber im Import-Pfad nicht umgesetzt** — `ImportExecutionService.cs:166` macht direktes `File.Move(source, target, overwrite: true)`. ChatGPTs Analyse kannte ADR-059/060/061 nicht oder nur teilweise.

## Runden-Übersicht

### Runde 1 — Einordnung der Recovery-Befunde in den ADR-/Task-Rahmen
- **Artefakte:** [r1/](./r1/)
- **Fokus:** ImportActionExecutor vs. ADR-060 Slice 3 / ADR-061 P5; Reihenfolge-Konflikt mit BPM-111; Quick-Fixes vorziehen; durch ADR-059 überholte Punkte (Preview-UX, LearnIndex); Dateibrowser vs. Modell A
- **Kernergebnis:** ChatGPT korrigiert sich 3× vorweg (ADR-061 P5 unterschätzt, Preview-Fokus überholt, planmanager.db = Cache+Journal statt SoR). Delta-Tabelle: beschlossen sind FS-Ports + `.bpm_tmp`/atomic rename + Lock-Retry; NICHT spezifiziert sind Vorab-Journalisierung aller Actions, `archive_path`-Persistierung, DB-Idempotenz, DB-Transaction pro Action, Recovery mit voller Planarchivlogik, `failed`-Terminalsemantik. Vorschlag: eigener Task „Import-Transaktions-Härtung" mit Slices T0–T8 (BPM-112 Slice 3 = T1). Claude verifiziert: Radial-Confirm läuft real über ungehärteten `ImportExecutionService` (`CaptureConfirmService.cs:26/46`). Punkte 6/9/10 der alten Auswertung durch ADR-059 überholt; Rest-Lückenliste = Bucket A/B/D + finaler Confirm + transaktionaler Execution-Pfad. Dateibrowser: Konvergenz auf ADR-061 Modell A, „Zuordnung ändern?"-Dialog zurückgezogen, Diagramm 13 vertagt.
- **Entscheidungen (Herbert):** Alt-Import („Import starten"/ImportPreviewDialog) wird mit Abschluss BPM-111 deaktiviert · Bucket-A-Dubletten beim Confirm journalisiert entfernen · Task-Schnitt → Runde 2.

### Runde 2 — Detailfragen Task-Schnitt + Nachlieferung Diagramme 01–09
- **Artefakte:** [r2/](./r2/)
- **Fokus:** Offenlegung, dass Claude nur Diagramme 10–12 aus dem Share kennt → ChatGPT liefert verifizierbare Kernbefunde aus 01–09 nach. Detailfragen: T0/T1-Reihenfolge (Fault-Injection vor/nach FS-Ports), Undo-Härtung (reicht T2/T3 für `ImportUndoService`?), DI statt `new` beim gemeinsamen Executor. Bucket-A-Journal-Modell (action_type, Recovery-Semantik, Delete vs. Papierkorb-Move). Danach finale Task-Schnitt-Empfehlung.
- **Kernergebnis:** Diagramm-Nachlieferung 01–09 ehrlich, kein übersehener V1-kritischer Befund (nur Kleinigkeiten: registry.json-Vertrag, contact_id, Kommentarhygiene). NEUER verifizierter Fund: `ImportUndoService` mit 5 Problemen — kritischster: DB-Rollback + `MarkImportUndone` laufen nach Disk-Fehlern bedingungslos weiter (ADR-058-Stop-Fall) → eigener Slice T7. T0/T1-Korrektur übernommen (T0 = reine Characterization). Neuer Slice H0 (Alt-Import-Cutover VOR der Härtung). Bucket A = `skipDuplicate`-Action, journalisiert + recovery-fähig, Empfehlung Delete statt Papierkorb; `destination_path` nullable via DB-Reset.
- **Entscheidungen (Herbert):** Eigener Task „Import-Transaktions-Härtung" H0+T0–T8 (BPM-112 Slice 3 = T1), Start nach BPM-111.06 · Bucket A: direktes Delete, bewusst nicht undo-bar · Runde 3 = Sign-off.

### Runde 3 — Beidseitiges Sign-off
- **Artefakte:** [r3/](./r3/)
- **Fokus:** Sign-off der Slice-Folge H0+T0–T8 und der 11 Invarianten; 10–15 testbare Akzeptanzkriterien fürs ClickUp-Ticket; finaler Vollständigkeits-Check über die gesamte 12-Diagramm-Analyse.
- **Kernergebnis:** _(offen)_
