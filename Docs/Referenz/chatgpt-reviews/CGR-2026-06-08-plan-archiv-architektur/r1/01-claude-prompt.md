# Review Runde 1 — Plan-Archiv-Architektur (PlanManager Persistenz)

## Rolle

Du bist ein erfahrener Software-Architekt mit Spezialisierung auf **Construction Document Management / CDE-Systeme** (Common Data Environment). Du kennst die Architektur und Datenmodelle von Procore, Aconex (Oracle), think project! (CONCLUDE CDE) und Autodesk Construction Cloud aus eigener Erfahrung. Du führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic).

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User, Herbert — selbst Polier und Bauleiter).
- Sprich direkt zu deinem Kollegen, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in **Canvas**
- CANVAS-TITEL: **„Review Runde 1 — Plan-Archiv-Architektur"**
- Fasse am Ende deiner Antwort zusammen:
  - ✅ Einigkeit (was bereits klar gut ist)
  - ⚠️ Widerspruch (wo du den Status Quo oder einen impliziten Plan kritisierst)
  - ❓ Rückfragen (was du noch brauchst um sicher zu antworten)

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben.
- **Hinweis:** Die letzten 10 Commits sind lokal noch nicht gepusht. Stand auf GitHub kann hinter `b741bba` (v0.28.52) zurückliegen.

## Gesprächsregeln

- Ehrlich und kritisch — auch zu Claudes Vorschlägen
- Probleme konkret benennen
- Verbesserungen mit Tabellenstruktur / SQL / Pseudocode zeigen
- Rückfragen bei fehlendem Kontext, nicht raten
- Fokus halten: **Persistenz-Architektur**. Keine UI-Diskussion, kein Wizard-Refactor, keine Recognition-Logik (die ist abgeschlossen).
- Kompakt, Code nur wenn nötig

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung **ohne Produktivdaten**.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen „Datei löschen, neu anlegen lassen" als gewollter Standardweg
- ULID als TEXT PRIMARY KEY für ALLE neuen Tabellen (Sync-Vorbereitung, ADR-039)
- Neue Tabellen brauchen die 6 Sync-Spalten (`created_by`, `last_modified_at`, `last_modified_by`, `sync_version`, `is_deleted`, plus implizit `id`) — ADR-050

Ausnahme: Nur wenn explizit „Migration bauen" im Prompt steht.

Quelle: `INDEX.md` Kapitel „Projekt-Phase (VERBINDLICH)".

## Projektkontext (aus Quickloads)

### PlanManager.md (source_of_truth)
- Zweck: Kern-Feature von BPM — sortiert Pläne aus `_Eingang/` automatisch in Zielordner; Profil-basiert; Index-Versionierung; Undo-Journal.
- Fachliche Invarianten:
  - `document_key` über `identityFields` aus dem Profil — nie nur `plan_number` allein
  - Import-Journal VORHER schreiben (pending) — erst dann Dateien verschieben
  - MD5 + file_size IMMER Pflicht (universeller Fingerabdruck)
  - Alle Pfade im Journal relativ zum Projektordner
  - Undo nur letzter Import + Preflight-Prüfung
- 7-Stufen-Pipeline: Scan → Fingerprint → Parse → Resolve → BuildIdentity → VersionDecision → ExecutionPlan

### DB-SCHEMA.md (source_of_truth)
- Zwei DBs: `bpm.db` (Stamm + Projekt) und pro Projekt eine `planmanager.db` (Cache + Journal + Undo)
- ULID TEXT PRIMARY KEY für alle Tabellen
- Sync-Schema v2.1 ab v0.25.23, Segmenttyp-Erweiterung v2.2 ab v0.28.44
- Stammdaten relevant für Pläne: `buildings`, `building_parts`, `building_levels` — heute manuell vom User angelegt

### Aktuelle PlanManager-DB-Tabellen

```sql
-- aktuell implementiert (Schema v1.0 der planmanager.db):

CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,                -- ULID
    document_key TEXT NOT NULL,         -- z.B. "polierplan|103|h5|gr|e1" — konkatenierte Identity
    document_type_id TEXT,              -- FK → Profil-ID
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    document_type TEXT NOT NULL,
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    index_source TEXT NOT NULL,         -- "FileName" | "None" | "PlanHeader"
    revision_status TEXT NOT NULL,      -- "current" | "archived"
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX ux_plan_revision_current
ON plan_revisions(document_key, revision_status)
WHERE revision_status = 'current';

CREATE TABLE plan_files (
    id TEXT PRIMARY KEY,
    file_name TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    file_type TEXT NOT NULL,            -- "pdf" | "dwg" | "jpg" | "other"
    md5_hash TEXT NOT NULL,
    file_size INTEGER NOT NULL,
    origin_mode TEXT NOT NULL,          -- "autoGrouped" | "manualLinked" | "standalone"
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE revision_file_links (
    revision_id TEXT NOT NULL,
    file_id TEXT NOT NULL,
    link_mode TEXT NOT NULL,            -- "auto" | "manual"
    is_primary INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (revision_id, file_id),
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (file_id) REFERENCES plan_files(id)
);

CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,
    source_path TEXT NOT NULL,
    file_count INTEGER NOT NULL,
    profile_id TEXT,
    machine_name TEXT,
    error_message TEXT
);

CREATE TABLE import_actions (
    id TEXT PRIMARY KEY,
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,          -- "new" | "indexUpdate" | "changed" | ...
    action_status TEXT NOT NULL,
    document_key TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    archive_path TEXT,
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE TABLE import_action_files (
    id TEXT PRIMARY KEY,
    action_id TEXT NOT NULL,
    file_id TEXT,
    file_name TEXT NOT NULL,
    original_file_name TEXT,
    final_file_name TEXT,
    file_type TEXT NOT NULL,
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    md5_hash TEXT NOT NULL,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);

-- Stammdaten in bpm.db (existierend, manuell befüllt):
CREATE TABLE buildings (id TEXT PRIMARY KEY, project_id TEXT, ...);
CREATE TABLE building_parts (id TEXT PRIMARY KEY, project_id TEXT, short_name TEXT, description TEXT, ...);
CREATE TABLE building_levels (id TEXT PRIMARY KEY, building_part_id TEXT, name TEXT, ...);

-- Segmenttyp-Katalog (BPM-108, abgeschlossen):
CREATE TABLE segment_types (id TEXT PRIMARY KEY, token_key TEXT, name TEXT, semantic_role TEXT, group_id TEXT, ...);
CREATE TABLE segment_type_groups (id TEXT PRIMARY KEY, name TEXT, sort_order INTEGER, ...);
```

### Was beim Sortieren passiert (heute)

Der Recognizer extrahiert pro Plan die Segmentwerte gemäß dem aktiven `RecognitionProfile`. Beispiel für die Datei `21005_103_AP_H1_GR_E1_05 Grundriss E+1.pdf`:

| Segmenttyp (aus segment_types) | Extrahierter Wert |
|---|---|
| projectNumber | `21005` |
| planNumber | `103` |
| documentType | `AP` |
| haus | `H1` |
| plankategorie | `GR` |
| geschoss | `E1` |
| planIndex | `05` |

**Diese Einzelwerte landen heute NICHT als eigene Spalten in der DB.** Sie werden:
- In den `document_key`-String konkateniert (über `DocumentKeyBuilder.cs`, Stage 5 der Pipeline)
- In den `relative_directory`-Pfad eingesetzt (Stage 7, Execution Plan)

→ Eine SQL-Filterung „WHERE haus = 'H1' AND geschoss = 'EG'" geht **nur über LIKE-Match** auf `document_key`.

### Status-Logik (heute)

- `revision_status = 'current'` für die jüngste Revision, `'archived'` für überholte
- **Kein Zeitstempel für den Statuswechsel** (kein `current_from`, kein `archived_at`)
- `created_at` = wann importiert, `updated_at` = wann zuletzt geändert (vermischt Erst-Import und spätere Updates)

### Offene Tasks im Backlog mit Bezug zur Persistenz

- **BPM-092** | `recognition_profiles` in DB-Tabelle migrieren — post-V1, Open, low. Aktuell liegen Profile als JSON in `.bpm/profiles/<id>.json`.
- **BPM-082** | Segment-Erkennung — done. Ergebnis: CGR-2026-04-17-bpm-082-segment-recognition.
- **BPM-108** | Segmenttyp-Verwaltung DB-basiert — done. Ergebnis: CGR-2026-05-12-segmenttyp-architektur.

**Es gibt KEINEN bestehenden Task** für „Plan-Metadaten-Persistenz" oder „Cross-Modul-Verknüpfung". Aus diesem Review soll er entstehen.

## Anwendungsfall der die Frage ausgelöst hat

Herbert plant das **Bautagebuch-Modul (BPM-056, post-V1)**. Use Case:

> Beim Schreiben eines Bautagesberichts gibt der Polier ein: Datum, Bauteil (Haus 1), Geschoss (EG). Das System soll als Fußnote automatisch die zu diesem Zeitpunkt aktuellen Pläne anzeigen — Polierplan, Schalung, Bewehrung. Bei Klick auf einen späteren Bericht für denselben Bereich muss die damals aktuelle Revision angezeigt werden, nicht die heutige.

Zwei weitere Module brauchen das gleiche:
- **Foto-Modul (BPM-057):** Foto wird mit GPS/Lokation aufgenommen → relevante Pläne als Kontext.
- **Vorlagen-Modul (BPM-061):** Plan-Querverweise in generierten Berichten.

## Wettbewerbsanalyse (vorab durchgeführt)

Recherche in Runde 0 (im Chat) hat ergeben:

- **Procore** trennt Document / Revision / File mit OCR-Plankopf-Erkennung. Schema in 3 Ebenen.
- **Aconex / think project! / CONCLUDE CDE** nutzen ISO-19650-konformes Dokument-Code-Schema mit Suitability-Status (S0…S4), Status-Historie, Transmittals.
- **Autodesk Construction Cloud (Plangrid)** trennt Drawings / Sheets / Revisions; Multi-Page-PDF-Split; Auto-Sheet-Linking via OCR-Callout-Detection.
- **Gemeinsamer Nenner:**
  - Document (logisch, über Revisionen hinweg) ↔ Revision (Versionsstand) ↔ File (physische Datei) — drei Ebenen
  - Metadaten als flexible Attribute (KV-Tabelle oder JSON) ODER feste Spalten für Top-Felder
  - Status-Lifecycle mit Zeitstempel pro Wechsel
  - Audit-Trail (wer/wann/was)
  - Cross-Referenzen zu anderen Entitäten (RFI/Mängel/Berichte) als eigene Link-Tabelle

DACH-Markt: BauMaster (AT, 79 €/User/Monat), PlanRadar (AT, 26 €), PLANFRED (DE) — keiner hat filename-basierte Auto-Klassifikation in der Tiefe wie BPM angestrebt.

## Die Frage an dich

**Ist die aktuelle PlanManager-Persistenz** (`plan_revisions` + `plan_files` + `revision_file_links` + Journal) **ausreichend für die kommenden Module Bautagebuch/Foto/Vorlagen, oder brauchen wir einen Architektur-Schritt vor BPM-056?**

Konkrete Diskussionspunkte:

1. **Document/Revision-Trennung:** Soll eine neue Tabelle `plan_documents` eingeführt werden (logisches Dokument als eigene Entität), oder reicht der `document_key`-String als implizite Gruppierung? Welche Nachteile hat die String-basierte Identität für Cross-Modul-Lookup?

2. **Metadaten-Persistenz für Filterung:**
   - **Variante A:** Eigene Tabelle `plan_document_attributes` (revision_id, attribute_key, attribute_value) — sehr flexibel, KV-Pattern
   - **Variante B:** Feste FK-Spalten in `plan_revisions` (`building_part_id`, `building_level_id`, `component`) — relational sauber, weniger flexibel
   - **Variante C:** JSON-Spalte `extracted_segments` in `plan_revisions` mit SQLite-JSON1-Operatoren
   - **Variante D:** Status Quo lassen + LIKE-Queries auf `document_key`

   Was empfiehlst du **angesichts der drei konkreten Konsumenten (Bautagebuch, Foto, Vorlagen)** und der Frühphase (kein Daten-Lock-in)?

3. **Status-Historie / Zeitreise:** Reichen zwei Zeitstempel-Spalten in `plan_revisions` (`current_from` + `superseded_at`), oder braucht es eine eigene `plan_revision_history`-Tabelle mit Audit-Trail (changed_by, action, note)? Wie machen das Procore/Aconex?

4. **Cross-Modul-Verknüpfung:** Soll eine generische `plan_document_links`-Tabelle (source_type, source_id, target_revision_id) **jetzt** eingeplant werden, oder erst wenn das erste konsumierende Modul (Bautagebuch) gebaut wird?

5. **Auto-Learn für Stammdaten:** Wenn der Importer einen Segmentwert `Haus = "H1"` extrahiert und in `building_parts` (Stammdaten, manuell befüllt) gibt es noch keinen Eintrag — soll BPM **automatisch** einen anlegen, **fragen** oder **gar nichts** tun? Trade-off zwischen UX und Daten-Sauberkeit?

6. **Reihenfolge:** Wenn ja zu strukturellen Änderungen — welche Sub-Tasks in welcher Reihenfolge? Vor oder nach BPM-092 (`recognition_profiles` als DB-Tabelle)? Vor oder nach den V1-blockierenden Tasks (BPM-080/081/006)?

## Spezifische Prüfpunkte

Bitte gehe konkret durch:

- **Schema-Skizze** für deine empfohlene Lösung (SQL-Tabellen mit Spalten, Typen, FKs, Indizes)
- **Query-Beispiel** für den Bautagebuch-Use-Case (SELECT mit Zeitreise) — wie sieht das idealerweise aus?
- **Impact auf bestehenden Code:** `DocumentKeyBuilder`, `ImportWorkflowService`, `RevisionDecisionService`, `ImportPlanBuilder` — was bricht, was bleibt?
- **Größenordnung:** Aufwand in Personen-Tagen für deine empfohlene Lösung, vor BPM-056 (Bautagebuch) sinnvoll oder nachher?
- **Was wir explizit NICHT bauen sollten** (Over-Engineering-Warnung) — wo läufst du Gefahr Enterprise-Patterns aus Procore/Aconex zu übernehmen, die für eine 5-User-Polier-App ungesund wären?

Antworte ehrlich. Wenn der Status Quo reicht — sag das. Wenn der Industrie-Standard hier übertrieben ist — sag das auch.
