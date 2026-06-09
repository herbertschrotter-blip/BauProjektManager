# CGR-2026-06-09-plan-erkennung — Recognition v2 (Plan-Identitäts-Extraktion + Sortierung)

**Thema:** plan-erkennung — zuverlässige Plan-Identität + Ordner-Sortierung aus unregelmäßigen Dateinamen + variablen Schreibweisen (4-Stufen-Kette + OCR/KI-Abgrenzung).
**Zeitraum:** 2026-06-09
**Ursprungs-Chat:** BauProjektManager Teil 42 (nach Abschluss BPM-109 Foundation Slice, v0.28.65)
**Status:** ✅ Abgeschlossen (r1 Pivot → r2 Strategie B → r3 Radial-UI signiert)

---

## Runden-Übersicht

### Runde 1 — Recognition-v2-Architektur + Sequenzierung
- **Artefakte:** [r1/](./r1/)
- **Fokus:** Bewertung der 4-Stufen-Kette (Extract→Normalize→Alias→Learn), Rolle von lokalem OCR/ONNX (+ `released_at`), Abgrenzung deterministisch ↔ KI, V1/post-V1-Sequenzierung gegenüber 080.05/007.02/109.06.
- **Auslöser:** Praxis-Import Statik (5998er) → positionsbasierte Erkennung sortiert in falsche Ordner (`\1`, `\KG`, `\(1)`); + Problem variabler Schreibweisen (`Haus 64` vs `H64`).
- **Kernergebnis r1:** ChatGPT signiert Recognition v2, aber als eigenes **Feld-Extraktionsmodell** (FieldExtractionRule mit Regex-Named-Captures), nicht positionsbasiert; Alias nur „Auto-Suggest + Confirmed Learn"; Ordner aus Stammdaten-Name; bestätigter **Feldkey-Bug** (`plan_number` vs `plannumber` → PlanNumber/RevisionToken null → Index-Erkennung tot). **User-Pivot:** Zweifel an Voll-Auto-Erkennung → Vorschlag Strategie B (manuelle Erstaufnahme + Revisions-Matching) mit Drag&Drop-Matrix-UI (Bauteile × Plantypen).

### Runde 2 — Strategie A vs B (MVP-Entscheidung)
- **Artefakte:** [r2/](./r2/)
- **Fokus:** A (Auto-Recognition v2) vs B (manuelle Erstaufnahme-Matrix + MD5/Index-Matching) als tragfähiger MVP; minimal nötiger V1-Scope; Drag&Drop-UI-Tragfähigkeit; Hybrid-Grenze; Feldkey-Bug-Fix.
- **Kernergebnis r2:** ChatGPT dreht MVP klar: **Strategie B als Kern, A nur Assist** („B entscheidet, A schlägt vor", harte Grenze via `ImportIdentitySource`). V1-MUSS-Scope definiert; Alias (109.06)+OCR aus V1-Muss raus; Lightweight-PlanNr/Index-Extractor + Feldkey-Fix bleiben V1. MD5=Dublettenbeweis≠Revisionsbeweis; document_key ID-basiert. UI: ChatGPT warnt vor starrer Matrix, empfiehlt Tabelle/Bulk. **Herbert wählt Radial-Menü** (bestehendes Mockup `02_ManuellSortieren.html`) als V1-UI → r3.

### Runde 3 — Radial-UI für Strategie B
- **Artefakte:** [r3/](./r3/)
- **Fokus:** Radial-/Marking-Menü als primäre V1-Erfassungs-UI für B unter Druck setzen (Skalierung/Caps, Dimensions-Kaskade Bauteil/Plantyp/Geschoss, Bulk, Capture-vs-Update-Pfad, PDF+DWG-Paare, Undo).
- **Kernergebnis r3:** Radial-/Nautilus-UI als **V1-Primär-Erfassungsgeste signiert** (B). Bedingungen: Radial erzeugt nur **Pending Assignments** (Import erst nach Bestätigung); **harte Caps** (max 3 Ringe Plantyp→Bauteil→Geschoss; Plantyp ≤8; Bauteil ≤8 direkt/9–16 paginiert/≥17 Favoriten+Suche/≥25 Listen-Pflicht; Bulk 2–8/9–20 Bestätigung/>20 Fallback); **matched Updates/Dubletten überspringen das Radial** (Buckets A Dublette / B Update-Karte / C Erstaufnahme→Radial / D Konflikt); **dauerhaftes rechtes Detail-Panel** als Kontrolle+Fallback; Zielordner aus Stammdaten-Name (nicht Alias); Undo vor+nach Import. Mockup muss zu echter Ring-Geometrie überarbeitet werden. **5 Design-Entscheidungen:** Geschoss als 3. Ring · Bauteil-Sort kontextbasiert · „+Bauteil" inline · PDF+DWG default „eine Revision" · Fallback als Panel.

**Serie-Ergebnis:** Resultiert in **ADR-059** (Recognition v2 / Plan-Erfassung) + Ticket-Umbau (Feldkey-Fix, neuer Radial-Erfassungs-Task V1, 007.02-Split, 109.06/OCR post-V1).

---

## Kontext

- Baut auf abgeschlossenem **BPM-109 Foundation Slice** (Schema v2.0 Persistenz) auf — diese Serie betrifft die **Erkennungs-Schicht davor**, nicht die Persistenz.
- Bezug: ADR-058 + Addendum, ADR-010 (Recognition-Profile), ADR-022 (Dateiname-Parsing), ADR-056 (Segmenttypen); Tickets BPM-007.02/.03 (Regex), 080.05 (Wizard), 109.06 (Stammdaten-Mapping/Alias), KI/Plankopf-OCR-Modul.
