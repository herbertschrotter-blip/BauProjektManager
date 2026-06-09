# Review Runde 1 — Herberts Entscheidungen + Pivot

## Entscheidungen (Stufe A)
1. **Strategie-Fork A vs B → in CGR r2 gegenüberstellen** (statt jetzt blind eine Richtung zu wählen).
2. **Feldkey-Bug verifizieren → bestätigt.** `FileParseService` schreibt Feldkeys als `segDef.FieldTypeId` (`plan_number`/`plan_index`), `ImportWorkflowService` liest `plannumber`/`planindex` → `ClassifiedImportFile.PlanNumber` + `RevisionToken` bleiben null. Import lief nur, weil `document_key` aus `DocumentKeyBuilder` (liest IdentityFields korrekt) kommt. **Kritisch für Strategie B** (Index-Erkennung hängt an RevisionToken/PlanNumber). Fix nötig, strategie-unabhängig.

## User-Pivot → Strategie B + konkretes UI-Konzept
Herbert zweifelt an Voll-Auto-Erkennung (Namens-Chaos) und schlägt **manuelle Erstaufnahme + danach nur Revisions-Matching** vor (Strategie B).

**UI-Konzept für die manuelle Erstaufnahme (von Herbert):**
- Oben: Liste der losen Eingang-Dateien (z.B. 15).
- Unten: Container als **Matrix** — Spalten = **Bauteile** (aus Projekt-Stammdaten, reine DB-Abfrage; „+ Bauteil"-Button → direkt zu den bestehenden Projekt-Einstellungen/Bauteile), Zeilen = **Plantypen** (Polierplan/Schalung/Bewehrung…, editierbar/erweiterbar).
- 1..n Dateien per **Drag&Drop** in die Zelle [Bauteil × Plantyp] → Identität (building_part_id + document_type) in einer Geste gesetzt. Kein Tippen.
- → Macht manuelle Erfassung **komfortabel statt mühsam** → entkräftet das Haupt-Gegenargument gegen B.

## Nächster Schritt
CGR r2: A (Auto-Recognition v2) vs B (Erstaufnahme-Matrix + MD5/Index-Matching) als MVP-Entscheidung, inkl. Drag&Drop-UI + bestätigtem Feldkey-Bug.
