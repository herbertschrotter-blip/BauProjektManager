## Rolle

Du bist ein erfahrener **Senior Software Architect mit Fokus auf .NET / WPF / SQLite / Domain-Driven Design** und führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic).

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User).

- Sprich direkt zu deinem Kollegen, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- **CANVAS-TITEL:** "Review Runde 1"
- Fasse am Ende JEDER Antwort zusammen:
  ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:

- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Letzter gepushed Stand: **v0.28.42** (heute, 2026-05-12)
- Nutze das aktiv, um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!

## Gesprächsregeln

- Ehrlich und kritisch
- Probleme konkret benennen
- Verbesserungen mit Code/Pseudocode zeigen
- Rückfragen bei fehlendem Kontext
- Fokus halten, keine allgemeinen Exkurse
- Kompakt, Code nur wenn nötig
- **Fokus:** Wechselwirkungen mit bestehenden Konzepten (besonders BPM-082 Segment-Recognition + ADR-053 Sync). Keine Greenfield-Diskussion.

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als gewollter Standardweg

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

Quelle: `INDEX.md` Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads)

### PlanManager.md (source_of_truth)
- **Zweck:** Kernfeature von BPM — sortiert Dokumente aus `_Eingang/` automatisch nach Profilen
- **Fachliche Invarianten:**
  - `document_key` über `identityFields` — nie nur `plan_number` allein
  - Import-Journal VORHER schreiben (pending) — erst dann Dateien verschieben
  - MD5 + file_size IMMER Pflicht
  - Alle Pfade im Journal relativ zum Projektordner
  - Undo nur letzter Import + Preflight-Prüfung
- **Kap. 13 Dateinamen-Parsing (ADR-022):** FileNameParser + Segment-Splitting + TokenizationConfig (v2)
- **Kap. 14 Profil-System v3 (ADR-010, BPM-082):**
  - `RecognitionRule.method = "segment"` (Default) oder `"regex"` (Fallback)
  - `segment`-Rules tragen `segmentPosition: int?` (0-basierte Token-Position)
  - Alte Methoden `prefix`/`contains` entfernt
  - JSON-Schema v3 in `.bpm/profiles/<n>.json`

### DB-SCHEMA.md (source_of_truth)
- **Sync-Felder ADR-050 (Pflicht ab v0.24.3):** Jede Tabelle hat `created_at`, `created_by`, `last_modified_at`, `last_modified_by`, `sync_version`, `is_deleted` (UTC, Soft Delete, Writes über Services)
- **ULID-PK ADR-039 v2 (ab v0.25.1):** Alle Tabellen mit ULID als TEXT PRIMARY KEY

### ADR-010 — RecognitionProfiles und PatternTemplates getrennt
- Profile pro Projekt in `.bpm/profiles/<n>.json`
- PatternTemplates global in `pattern-templates.json`
- v3-Erweiterung (BPM-082): segment+regex statt prefix/contains

### ADR-053 — Server-Sync-Architektur (entschieden 2026-04-30)
- Windows-only Stack: PostgreSQL 17 + ASP.NET Core 10 Worker Service + Caddy
- `IBpmSyncClient` mit Pull/Push, server-gewinnt
- Spike 0 (ProjectDatabase syncfähig) als erster Code-Schritt
- Phase 0/1: 5–10 User parallel, Single-Tenant

## Das Konzept (BPM-108)

### Motivation

Aktuell sind die Segmenttypen (Plannummer, Geschoss, Planart …) und ihre Reihenfolge + Farb-Tokens hardcoded an drei Stellen:
- `BauProjektManager.Domain.Models.PlanManager.FieldType` — Enum mit 17 Werten (PlanNumber, PlanIndex, ProjectNumber, Description, Datum, Geschoss, Haus, Planart, Objekt, Bauteil, Bauabschnitt, Stiege, Achse, Zone, Block, Ignore, Custom)
- `ProfileWizardViewModel.BuildFieldTypeOptions()` — Liste + DisplayName + Reihenfolge
- `Colors.xaml` — Farb-Tokens (BpmFieldPlanNumber, BpmFieldPlanIndex etc.)

User kann **weder eigene Segmenttypen anlegen, noch Reihenfolge ändern, noch Built-ins deaktivieren**. Erkannt im Zuge BPM-080.05 Schritt 2 ("+ Eigenes"-Chip nicht funktional, Farbsystem starr).

### Sprachregelung

UI-Begriff durchgehend **Segmenttyp** (statt "Feldtyp"/"FieldType"). Code-Property `FieldType` bleibt als Enum-Name intern (kein UI-Text).

### DB-Schema (in `bpm.db`, global für alle Projekte)

```sql
-- Gruppen-Tabelle (Identifikation, Räumlich, Inhaltlich, Sonstiges, Eigene-Custom …)
CREATE TABLE segment_type_groups (
    id TEXT PRIMARY KEY,            -- Built-in: fixe Strings ("group_identification"); Custom: ULID
    name TEXT NOT NULL UNIQUE,
    is_builtin INTEGER NOT NULL,    -- 1 = systemfest (nicht löschbar)
    sort_order INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    -- Sync-Felder ADR-050
    created_at INTEGER NOT NULL,
    created_by TEXT,
    last_modified_at INTEGER NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
);

-- Segmenttyp-Tabelle
CREATE TABLE segment_types (
    id TEXT PRIMARY KEY,            -- Built-in: fixe Strings ("plan_number", "geschoss"); Custom: ULID
    name TEXT NOT NULL,             -- "Plannummer", "Akustik-Klasse" — Rename ändert NUR diesen
    color TEXT NOT NULL,            -- "#0F6E56"
    group_id TEXT NOT NULL REFERENCES segment_type_groups(id),
    is_builtin INTEGER NOT NULL,    -- 1 = systemfest (nicht löschbar)
    is_required INTEGER NOT NULL DEFAULT 0,  -- 1 = Pflicht-Marker (★), nur bei Plannummer
    sort_order INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    -- Sync-Felder ADR-050
    created_at INTEGER NOT NULL,
    created_by TEXT,
    last_modified_at INTEGER NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
);
```

### Seed beim App-Start

Alle 17 Built-in-Segmenttypen + 5 Built-in-Gruppen via `INSERT OR IGNORE` mit festen IDs (z.B. `"plan_number"`, `"group_identification"`). Default-Reihenfolge wie aktuell in `BuildFieldTypeOptions`.

### UI: Manager-Dialog (Mockup: `Docs/Mockups/PlanManager/04_FeldtypManager/01_Uebersicht.html`)

- Aufruf via Link "⚙ Segmenttypen verwalten…" in Wizard-Schritt 2
- Liste links: Gruppen-Header (mit Toggle + Sortier-Grip + Caret zum Einklappen) + Items darin (Toggle + Sortier-Grip + Farb-Swatch + Name + Badges)
- Edit-Panel rechts: Name (mit 🗑-Icon rechtsbündig in derselben Zeile) + Gruppe-Dropdown + 12-Farben-Palette
- Toolbar: "+ Neue Gruppe" + "+ Neuer Segmenttyp"

### ID-Referenz von Profilen

`RecognitionProfile` (`.bpm/profiles/<name>.json`) speichert in RecognitionRules statt `FieldType` Enum-Name jetzt **stabile ID**:

```json
{
  "schemaVersion": 3,
  "recognition": [
    {
      "method": "segment",
      "segmentPosition": 3,
      "fieldTypeId": "planart",
      "value": "Polierplan"
    }
  ]
}
```

Display-Lookup zur Render-Zeit: `SELECT name, color FROM segment_types WHERE id = 'planart'`.

### Operationen — Verhalten für Profile

| Operation | DB | Profile-Folge |
|---|---|---|
| Rename (Name-String ändern) | UPDATE segment_types SET name=? WHERE id=? | Profile sehen sofort neuen Namen — **funktional unverändert** |
| Farbe ändern | UPDATE segment_types SET color=? | Profile-Tokens neue Farbe automatisch |
| Gruppe wechseln | UPDATE segment_types SET group_id=? | Profile sehen keinen Unterschied (Gruppe ist nur UI-Org) |
| Toggle off (`is_active=0`) | UPDATE | Wizard zeigt's nicht mehr als Chip, Profile funktionieren weiter |
| Soft-Delete (`is_deleted=1`) | UPDATE | Aus Manager verschwunden, Profile-Lookup ungefiltert → Anzeige bleibt funktional |
| Built-in löschen | **nicht erlaubt** | — |
| Custom-Gruppe löschen | nur wenn leer (`is_deleted=1`) | — |
| **Hard-Delete** | **gibt's nicht** | — |

### Effective-Active in Wizard-Chip-Liste

```sql
SELECT * FROM segment_types st
JOIN segment_type_groups g ON g.id = st.group_id
WHERE st.is_active = 1 AND st.is_deleted = 0
  AND g.is_active = 1 AND g.is_deleted = 0
ORDER BY g.sort_order, st.sort_order;
```

### Profile-Lookup (ungefiltert, auch deleted)

```sql
SELECT * FROM segment_types WHERE id = ?
```

Damit gespeicherte Profile auch nach Soft-Delete weiter rendern.

## Aufgabe

Prüfe diese Architektur auf Risiken, Lücken und Inkonsistenzen, insbesondere:

1. **Wechselwirkung mit BPM-082 Segment-Recognition v3:**
   `DocumentTypeRecognizer` arbeitet derzeit mit `FieldType` Enum (segment-Position-Check). Wenn `RecognitionRule.fieldTypeId` jetzt ein String ist — wie passt das mit der Recognition-Logik? Muss `FieldType`-Enum komplett weg, oder nur in den Profile-Files durch ID ersetzt werden, während der Recognizer mit Enum weiterläuft? Was bei Custom-Field-Types (keine Enum-Entsprechung)?

2. **JSON-Schema-Migration:**
   v3 (BPM-082) hat aktuell `fieldType: "PlanNumber"` als Enum-String. Neue Variante: `fieldTypeId: "plan_number"`. Schema-Versionierung nötig oder Frühphasen-Reset? Wenn Reset: was passiert mit den Test-Profilen (Polierplan in `01KRAT6ASMQ0K0BB6SXTCWZSAD`)?

3. **ProfileManager Load/Save:**
   Aktuell lädt `ProfileManager.Load` mit `Log.Error` Profile aussortiert die ungültige Methoden enthalten. Was wenn ein Profil einen `fieldTypeId` referenziert der nicht in `segment_types` existiert (z.B. Profil vom anderen Gerät, IDs nicht gesynct)? Hard fail oder Soft-Skip?

4. **Sync (ADR-053):**
   `segment_types` und `segment_type_groups` mit `IBpmSyncClient` mitsynchronisieren. Server-gewinnt-Strategie: was wenn auf Gerät A der Segmenttyp gelöscht (`is_deleted=1`), auf Gerät B umbenannt? Konflikt-Resolution durch Server. Sync-Reihenfolge (Groups vor Types? FK-Constraint)?

5. **Built-in-Drift:**
   Wenn neue BPM-Version neue Built-in-Segmenttypen mitbringt (z.B. "Auftragsnummer"), wie kommt das auf bestehende Installationen? Seed-on-Start mit `INSERT OR IGNORE` reicht. Aber: was wenn User einen Built-in deaktiviert hatte und in neuer Version wird der erweitert (z.B. neue Farbe Default)?

6. **DSGVO/DataClassification:**
   Segmenttypen sind UI-Konfiguration (kein Personenbezug). Klasse A intern. Sollte das in DSVGO-Architektur.md erwähnt werden, oder reicht der implizite Default?

7. **Manuell-Sortier-Modus:**
   Aktuell unklar wie der mit Segmenttypen interagiert. Beim Manuell-Sortieren wählt User direkt einen Zielordner — nicht über Profile/RecognitionRules. Ist eine Wechselwirkung mit `segment_types` zu erwarten?

8. **Wizard Schritt 5 Erkennung:**
   In Schritt 5 wählt User klickbare Segmente als Erkennungsmuster. Wenn `RecognitionRule.fieldTypeId` ungültig wird (Soft-Delete), wird der Wizard-Re-Open das Profil korrekt rendern? Was wenn User dann editiert?

9. **Custom-Segmenttyp-IDs und Konfliktfreiheit:**
   ULID generiert ID auf einem Gerät. Wenn ohne Sync zwei Geräte denselben Custom-Type unabhängig anlegen ("Akustik-Klasse"), entstehen 2 IDs. Beim Sync verschmelzen sie? Oder bleiben sie 2 separate Einträge? User-Erlebnis?

10. **Lookup-Performance:**
    Bei jedem Render eines Tokens (Wizard, Profile-Liste, Manuell-Sortieren) wird `segment_types` gelesen. Bei 17 Built-ins + N Custom + Wachstum über Zeit: Caching nötig? In-Memory-Map mit Cache-Invalidation bei Changes?

Bitte priorisiere die Findings (Blocker / wichtig / nice-to-have) und schlage konkrete Lösungen oder Klärungsfragen vor.
