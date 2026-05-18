# User-Entscheidungen — Review Runde 1

**Datum:** 2026-05-18
**User:** Herbert

---

## Antworten auf ChatGPTs 5 Rückfragen

### 1. Built-in umbenennbar/farbänderbar?

**Entscheidung: JA — auch umbenennen/Farbe**

Built-ins sind voll editierbar wie Custom-Typen. User darf jeden Built-in (z.B. „Plannummer" → „Plan-Nr.") umbenennen und Farbe ändern.

**Konsequenz für DB-Schema:**
- `user_modified_name`, `user_modified_color`, `user_modified_sort`, `user_modified_active` als BOOL-Flags pro Built-in
- App-Update darf User-modifizierte Felder nicht überschreiben
- Built-in-Update-Policy: `UPDATE … WHERE is_builtin = 1 AND user_modified_<feld> = 0`

### 2. Custom SemanticRole wählbar?

**Entscheidung: REIN DEKORATIV — keine SemanticRole für Custom**

Custom-Typen haben niemals eine SemanticRole. Wenn ein User einen Hierarchie-Slot braucht (z.B. „neuer Raumtyp"), muss er einen der Built-in nutzen (Geschoss/Haus/Bauteil/Bauabschnitt/Stiege/Zone/Block) — oder Built-in umbenennen.

**Konsequenz:**
- `segment_types.semantic_role` ist NULL für alle Custom-Einträge
- Manager-Dialog hat KEIN „Semantik"-Dropdown für Custom (nur Built-ins zeigen die Rolle als read-only-Info)
- Wizard Schritt 4 (Hierarchie) zeigt nur Built-ins mit `SemanticRole = Spatial` (Geschoss/Haus/etc)

### 3. identityFields UI-geführt?

**Vorschlag (zu validieren in Runde 2):** Implizit aus SemanticRole

`identityFields` wird automatisch aus SemanticRole abgeleitet: `documentType` + alle Segmente mit `SemanticRole ∈ {PlanNumber, Spatial}`. Kein UI-Override in Frühphase.

### 4. recognition_profiles-Tabelle für ADR-053?

**Vorschlag (zu validieren in Runde 2):** Nicht in BPM-108, separates Task

BPM-108 bleibt fokussiert auf `segment_types` + `segment_type_groups`. Die Migration der JSON-Profile in DB-Tabellen wird ein eigenes Task (vermutlich für ADR-053-Implementation Phase).

### 5. bpm.db Komplett-Reset OK?

**Vorschlag (zu validieren in Runde 2):** Ja — Frühphase, keine Produktivdaten

`bpm.db` kann beim BPM-108-Release komplett gelöscht und neu aufgebaut werden. Auch Settings werden zurückgesetzt. User-Hinweis im Release-Note reicht. Granulares Reset wäre Overengineering in Frühphase.

---

## Status

- **Runde 1: abgeschlossen** mit klaren Entscheidungen zu Built-in/Custom-Editierbarkeit
- **Runde 2 startet:** Validierung der 3 weiteren Antworten + offene Details (lastKnownLabel, Custom-Chip UI-Flow, Wizard Schritt 5 Reopen, Migration bestehender Domain-Konstanten)
