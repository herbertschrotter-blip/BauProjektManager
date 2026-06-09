# Review Runde 2 — Claude-Analyse (Stufe A)

## Konvergenz — Sign-off-reif
ChatGPT + Claude deckungsgleich:
- **MVP = Strategie B als Kern, A nur Assist** ("B entscheidet, A schlägt vor", harte Grenze via `ImportIdentitySource`-Enum: ManualConfirmed/ExistingDocumentMatch dürfen schreiben, AutoSuggested nur Preview).
- **V1-MUSS:** Feldkey-Bug-Fix · manuelle Erstaufnahme-Workflow · document_key aus Stammdaten-IDs (building_part_id, nicht Text) · plan_documents/revisions/files · MD5-Dublette · Lightweight PlanNr/Index-Kandidat · Update-Vorschlag gegen bekanntes Dokument · Supersede/Journal (BPM-109).
- **Post-V1:** FieldExtractionRule/Regex-System, Alias (109.06), OCR, Zero-Touch.
- **Schärfung:** MD5 = Dublettenbeweis ≠ Revisionsbeweis; Plannummer = Suchanker; finale Identität via document_key/manuelle Bestätigung. Neue Dokumente ohne bestätigte Identität nie automatisch importieren.

## Mockup-Fund
HerbERTs Radial-Menü existiert bereits: `Docs/Mockups/PlanManager/02_Projektdetail/02_ManuellSortieren.html` (Tab „Manuell sortieren"). Datei-Liste, aktives Item zentriert, Radial mit 6 Segmenten (Plantypen) + aufklappendes Sub-Menü rechts (Sub-Ordner). Mechanik = Herberts Idee; Dimensionen müssten auf B-Modell (Bauteil + Plantyp [+ Geschoss]) gemappt werden.

## Offener Punkt: V1-UI
3 Kandidaten: (1) Tabellen-/Bulk-Editor + Multi-Edit (ChatGPT-V1-Empfehlung, skaliert am besten), (2) Bauteil×Plantyp-Matrix (Herbert 1. Idee), (3) Radial/Marking-Menü (Herbert-Mockup, elegant, pro-Item). Claude-Synthese: Tabelle/Liste als Rückgrat + Radial als Schnell-Geste auf Selektion. Produkt-Geschmack → Herbert.

## ChatGPTs 5 Rückfragen (Claude-Empfehlungen)
1. document_key bei B: **stabile IDs** (building_part_id/building_level_id), nicht Anzeigenamen. (einig)
2. building_level_id im MVP: optional — V1-Key = building_part_id + document_type + plan_number; Geschoss optional.
3. V1-UI zuerst Tabelle/Bulk, Matrix/Radial später: Tendenz ja (ChatGPT), aber Herberts Radial-Präferenz beachten.
4. PDF+DWG-Paare: gemeinsam markieren → eine Revision (konsistent mit beobachteter .dwg/.pdf-Gruppierung). 
5. Update auto-akzeptieren bei eindeutig vs immer Preview: V1 immer Preview (sicher), Auto später.

## Entscheidungspunkte → ask_user_input
1. r2 als Sign-off der Strategie, oder Runde 3 (z.B. für UI-Detaillierung)?
2. V1-UI-Richtung (Tabelle/Bulk · Matrix · Radial · Hybrid).
