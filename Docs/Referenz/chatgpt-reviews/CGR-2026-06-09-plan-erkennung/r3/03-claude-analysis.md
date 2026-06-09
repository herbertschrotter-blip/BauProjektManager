# Review Runde 3 — Claude-Analyse (Stufe A) + Serie-Sign-off

## Voller Konsens
ChatGPT + Claude deckungsgleich. V1-Spezifikation steht:
- Strategie B Kern, A Assist.
- Radial/Nautilus = primäre Erfassungs-Geste; Preview-Panel = Kontrolle; Listen-Fallback = Sicherheitsnetz.
- Harte Caps: max 3 Ringe (Plantyp→Bauteil→Geschoss optional), Plantyp ≤8, Bauteil ≤8 direkt/9–16 paginiert/≥17 Favoriten+Suche/≥25 Listen-Pflicht, Bulk 2–8/9–20 Bestätigung/>20 Fallback, matched Updates überspringen Radial, Radial schreibt nur Pending Assignments, alles undo-bar (vor Import = Pending-State, nach Import = Journal).
- Echte Ring-/Fächer-Geometrie statt loser Kacheln (Mockup muss überarbeitet werden).
- Capture-vs-Update-Buckets: A Dubletten / B Update-Karten / C Erstaufnahme→Radial / D Konflikt-Dialog.

## Claude-Antworten auf ChatGPTs 5 Rückfragen (Empfehlungen)
1. Geschoss → V1 ins Preview-Panel (Radial 2 Ringe; Geschoss-Ring post-V1).
2. Bauteil-Sort → kontextbasierter Vorschlag zuerst (Kandidat+zuletzt verwendet), dann natural sort.
3. „+ Bauteil" → Inline-Quick-Add (schreibt building_parts) + Link zu Einstellungen.
4. PDF+DWG → default „eine Revision" vorschlagen, im Preview bestätigen.
5. Fallback → dauerhaftes rechtes Detailpanel (Preview + Editor in einem).

## Resultierende Artefakte (festzuschreiben)
- **Neuer ADR** „Recognition v2 / Plan-Erfassung": MVP = manuelle Erstaufnahme (Strategie B) + deterministisches Dubletten-/Revisions-Matching; Auto-Extraktion (A) nur Assist/Vorbefüller; Radial-UI mit Caps + Listen-Fallback + Pending-Assignments; Drei-Zeiten/Schema-v2.0 unverändert.
- **Ticket-Umbau:** Feldkey-Bug-Fix (sofort, V1-blockierend); neuer V1-Task „Manuelle Erstaufnahme + Radial-UI + Pending-Assignments + Bucket-Matching"; 007.02/.03 splitten (LightweightPlanTokenExtractor V1 / FieldExtractionRule post-V1); 109.06 Alias + OCR explizit post-V1; 080.05 Wizard-Scope anpassen.
- **Mockup-Überarbeitung:** 02_ManuellSortieren → echtes konzentrisches Ring-/Nautilus-Modell mit B-Dimensionen.

## Entscheidung
Serie Sign-off-reif. Offen: Festschreibung jetzt (ADR + Tickets) oder Handover.
