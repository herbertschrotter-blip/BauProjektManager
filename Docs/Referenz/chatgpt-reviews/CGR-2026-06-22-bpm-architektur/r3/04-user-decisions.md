# Review Runde 3 — User-Entscheidungen

## Meta
- **Nächster Schritt:** → **Runde 4** (Fokus: **beides kombiniert** — Abschluss-Validierung des Gesamtmodells + Slice-0-Implementierungstiefe), danach ADR.

## Detailfragen (Runde-3-Rückfragen entschieden)
1. **`document_types.key` bei manuell angelegten Typen („+ Neu…")** → **auto-generiert aus dem Namen + danach gesperrt** (stabile Identität wie Built-ins; Anzeigename frei editierbar, Key nicht).
2. **Protokolle-Eingang** → **ein globaler Eingang** (`01 Planunterlagen/_Eingang`); die Erfassung ordnet jeder Datei ihren Typ zu (auch Protokoll → `06 Protokolle/…`). Kein zweiter Scan-/Recovery-Pfad in V1.
3. **Baustelleneinrichtung** → bestätigt: `01 Planunterlagen / Baustelleneinrichtung / datei.pdf` (Subordner-Typ, `Ring2Source.None`, kein Präfix).

## Modell-Stand nach Runde 3 (final, implementierungsreif)
- `document_types`: + `key` (UNIQUE(project_id,key)) + `root_relative_path` (echter Root je Typ) + `folder_name` (leer bei Root-Typ).
- `FolderTemplateEntry` + `SubFolderEntry`: gleiche optionale Typ-Metadaten; Typ nur explizit (`CreatesDocumentType`).
- Kategorie-`HasPrefix` je Kategorie; `building_levels.folder_name = "{PrefixString} {Name}"`.
- `DocumentTargetPathResolver` nutzt nur DB (`root_relative_path/folder_name/Ring2/Ring3/file`).
- Slice 0.1–0.6; Post-V1-Grenze: eine Ring-2-Strategie pro Typ.

## Offener Punkt für Runde 4
- Welcher `root_relative_path`/`folder_name` bekommt ein **per „+ Neu…" (Radial) angelegter** Dokumenttyp? (Built-ins kommen aus dem Template — User-Typen brauchen eine Regel.)

→ Runde 4 in [../r4/01-claude-prompt.md](../r4/01-claude-prompt.md).
