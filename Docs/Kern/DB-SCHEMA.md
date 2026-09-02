---
doc_id: db-schema
doc_type: schema
authority: source_of_truth
status: active
owner: herbert
topics: [sqlite, bpm-db, planmanager-db, tabellen, migration, ulid, json-dateien]
read_when: [neue-tabelle, spalte-ändern, fk-ändern, migration, db-query, json-konfiguration]
related_docs: [architektur, dsvgo-architektur, planmanager]
related_code: [src/BauProjektManager.Infrastructure/Persistence/ProjectDatabase.cs, src/BauProjektManager.PlanManager/Services/PlanManagerDatabase.cs]
supersedes: []
---

## AI-Quickload
- Zweck: Zentrale Referenz für alle SQLite-Tabellen und JSON-Konfigurationsdateien in BPM
- Autorität: source_of_truth
- Lesen wenn: Neue Tabelle, Spalte ändern, FK ändern, Migration, DB-Query, JSON-Konfiguration
- Nicht zuständig für: Domain-Modell-Logik (→ Architektur.md), Datenschutz-Klassifikation (→ DSVGO-Architektur.md)
- Kapitel:
  - 1. Überblick
  - 2. Beziehungsdiagramm
  - 3. Modul-Zuordnung
  - 4. Tabellen-Schema (v2.2 — segment_types + segment_type_groups ab v0.28.44)
  - 5. Geplante Tabellen (nach V1)
  - 6. PlanManager-Datenbank (separat, implementiert)
  - 7. Schema-Migration
  - 8. Datenfluss zwischen Modulen
  - 9. Naming-Konventionen
  - 10. JSON-Konfigurationsdateien (kein SQLite)
    - 10.1 Übersicht aller JSON-Konfig-Dateien
    - 10.2 device-settings.json — Feldschema
    - 10.3 shared-config.json — Feldschema
    - 10.4 settings.json (legacy) — Feldschema
    - 10.5 Hilfsklassen für strukturierte Listen
    - 10.6 Default-Werte für SharedConfig
    - 10.7 Migration von settings.json (legacy) zu Split-Format
    - 10.8 Geplante Schema-Erweiterungen (gemäß ADR-053)
- Pflichtlesen:
  - Kapitel 4 (Tabellen-Schema) bei jeder Tabellen-/Spaltenänderung
  - Kapitel 9 (Naming-Konventionen) bei neuer Tabelle
  - Kapitel 9.3 (Sync-Felder) bei neuer Tabelle (ADR-050)
  - Kapitel 7 (Schema-Migration) bei Schemaänderung
  - Kapitel 10.2/10.3 (settings-Feldschema) bei Änderung an `DeviceSettings` oder `SharedConfig`
- Fachliche Invarianten:
  - **Schema v2.1 (Sync) implementiert ab v0.25.23:** ULID als TEXT PRIMARY KEY, Sync-Spalten (created_by, last_modified_at, last_modified_by, sync_version, is_deleted) auf allen Entitätstabellen, UTC-Timestamps
  - schema_version Tabelle in jeder DB
  - Neue fachliche Tabellen: ULID + 6 Sync-Spalten + UTC + Soft Delete (Kapitel 9.3, ADR-050)

---

# BauProjektManager — Datenbank-Schema

**Version:** 2.1 (Sync-Spalten implementiert, v0.25.23)  
**Datum:** 16.04.2026  
**DB-Engine:** SQLite  
**Speicherort:** `%LocalAppData%\BauProjektManager\bpm.db`

---

## 1. Überblick

Dieses Dokument ist die **zentrale Referenz** für alle Datenbanktabellen in BPM — bestehende und geplante. Jedes Modul-Konzept referenziert hierher statt eigene Schema-Entwürfe zu wiederholen.

### 1.1 Grundprinzipien

- **Eine Datenbank, viele Module:** Alle Module greifen auf `bpm.db` zu
- **PlanManager:** Eigene `planmanager.db` pro Projekt (Cache, Journal, Undo)
- **ID-Schema (ADR-039 v2):** ULID als `TEXT PRIMARY KEY` für ALLE Tabellen. Keine `seq` Spalte, keine INTEGER IDs, keine Ausnahmen.
- **ID-Generierung:** Zentral über `IIdGenerator` Interface (Domain), implementiert als `UlidIdGenerator` (Infrastructure). Nie direkt `Ulid.NewUlid()` im Code.
- **SQLite als System of Record (Modus A):** JSON-Dateien (registry.json, settings.json) sind generierte Exporte oder Konfiguration (ADR-002). In Modus C (Server): PostgreSQL ist SoR, SQLite = Offline-Cache (ADR-050)
- **Schema-Migration:** Versioniert, automatisch bei App-Start, rückwärtskompatibel (ADR-040)

### 1.2 Datenbank-Dateien

| DB | Speicherort | Inhalt | Synct? |
|----|------------|--------|--------|
| `bpm.db` | `%LocalAppData%\BauProjektManager\` | Alle Stamm- und Projektdaten | Nein (Event-Sync über ADR-037) |
| `planmanager.db` | `%LocalAppData%\...\Projects\<ProjektID>\` | Plan-Cache, Import-Journal, Undo | Nein (Event-Sync über ADR-037) |

---

## 2. Beziehungsdiagramm

### 2.1 Implementiert (v2.0 ULID)

```
clients ◄──────────── projects
                       │  │  │  │
                       │  │  │  └──── project_links (1:n)
                       │  │  │
                       │  │  └─────── project_participants (1:n)
                       │  │
                       │  └────────── building_parts (1:n)
                       │                    │
                       │               building_levels (1:n)
                       │
                       └───────────── buildings (Legacy)
```

### 2.2 Geplant (alle Module)

```
                              projects
                           /   │   │   \    \      \        \
                          /    │   │    \    \      \        \
                   clients  b_parts  partic. links  diary   difficulty
                              │                    entries
                         b_levels
                              │
                        work_packages ◄──────── lv_positions
                         │         │
                  work_assign.    completed → performance_catalog
                      │
                  employees ◄──── time_entries
                      │
                 material_orders (ClickUp/Task-Mgmt)
                      │
                  contacts (Adressbuch)
```

### 2.3 Foreign-Key Übersicht

| Von | Nach | FK-Spalte | Cascade | Status |
|-----|------|----------|---------|--------|
| projects | clients | client_id | Nein | ✅ Implementiert |
| building_parts | projects | project_id | CASCADE | ✅ Implementiert |
| building_levels | building_parts | building_part_id | CASCADE | ✅ Implementiert |
| document_types | projects | project_id | CASCADE | ⬜ Geplant (BPM-111.05, ADR-059-Addendum) |
| document_type_categories | document_types | document_type_id | CASCADE | ⬜ Geplant (BPM-111.05, ADR-059-Addendum) |
| project_participants | projects | project_id | CASCADE | ✅ Implementiert |
| project_links | projects | project_id | CASCADE | ✅ Implementiert |
| buildings | projects | project_id | CASCADE | ✅ Legacy |
| work_packages | projects | project_id | CASCADE | ⬜ Geplant |
| work_packages | building_parts | building_part_id | — | ⬜ Geplant |
| work_packages | building_levels | level_id | — | ⬜ Geplant |
| work_packages | lv_positions | lv_position_id | — | ⬜ Geplant |
| work_assignments | work_packages | work_package_id | CASCADE | ⬜ Geplant |
| work_assignments | employees | employee_id | — | ⬜ Geplant |
| time_entries | employees | employee_id | — | ⬜ Geplant |
| time_entries | projects | project_id | — | ⬜ Geplant |
| lv_positions | projects | project_id | CASCADE | ⬜ Geplant |
| performance_catalog | projects | project_id | — | ⬜ Geplant |
| project_difficulty | projects | project_id | CASCADE | ⬜ Geplant |
| diary_days | projects | project_id | CASCADE | ⬜ Geplant |
| diary_notes | diary_days | diary_day_id | CASCADE | ⬜ Geplant |
| material_orders | work_packages | work_package_id | — | ⬜ Geplant |
| project_participants | contacts | contact_id | — | ⬜ Vorbereitet (FK leer) |
| building_part_aliases | projects | project_id | CASCADE | ⬜ Geplant (BPM-109) |
| building_part_aliases | building_parts | building_part_id | CASCADE | ⬜ Geplant (BPM-109) |

> **Cross-DB-Soft-References (kein FK, ADR-058-Addendum):** Die Bezüge von `planmanager.db`-Tabellen auf `bpm.db` (`plan_documents.building_part_id`/`building_level_id`, `plan_document_segments.segment_type_id`) sind **logische Referenzen ohne FK** — SQLite erzwingt keine FK über getrennte DB-Dateien. Service-seitige Validierung. Siehe DB-SCHEMA Kap. 6.7 + ADR-058-Addendum.

**FK-Regel (verbindlich, ADR-039 v2):**
Alle Fremdschlüssel referenzieren die `id`-Spalte der Zieltabelle (`TEXT`, ULID). Alle FK-Spalten sind `TEXT`.

---

## 3. Modul-Zuordnung

Welches Modul "besitzt" welche Tabelle (schreibt), und welche Module lesen.

| Tabelle | Besitzer (schreibt) | Leser | Status |
|---------|-------------------|-------|--------|
| clients | Einstellungen | Registry-Export | ✅ |
| projects | Einstellungen | Alle Module, Registry-Export | ✅ |
| buildings | (Legacy) | — | ✅ Legacy |
| building_parts | Einstellungen (Tab 2) | Kalkulation, Ziegelberechnung | ✅ |
| building_levels | Einstellungen (Tab 2) | Kalkulation, Ziegelberechnung | ✅ |
| project_participants | Einstellungen (Tab 3) | Bautagebuch, Dashboard | ✅ |
| project_links | Einstellungen (Tab 4) | Dashboard | ✅ |
| schema_version | Infrastructure | — | ✅ |
| employees | Zeiterfassung | Kalkulation, Bautagebuch | ⬜ |
| time_entries | Zeiterfassung | Kalkulation, Bautagebuch | ⬜ |
| work_packages | Kalkulation | Bautagebuch, Dashboard | ⬜ |
| work_assignments | Kalkulation (Arbeitseinteilung) | Bautagebuch | ⬜ |
| lv_positions | Kalkulation (LV-Import) | Dashboard | ⬜ |
| performance_catalog | Kalkulation (Nachkalk) | Bauzeitprognose | ⬜ |
| project_difficulty | Kalkulation | Bauzeitprognose | ⬜ |
| diary_days | Bautagebuch | Dashboard, Export | ⬜ |
| diary_notes | Bautagebuch | Dashboard, Export | ⬜ |
| contacts | Adressbuch | Einstellungen (Tab 3) | ⬜ |
| material_orders | Task-Management | Dashboard | ⬜ |
| external_call_log | Infrastructure (ExternalCommunicationService) | Einstellungen (Datenschutz-Tab) | ⬜ |

---

## 4. Tabellen-Schema (v2.2 — Segment-Type-Verwaltung ab v0.28.44)

Alle Tabellen verwenden `id TEXT PRIMARY KEY` mit ULID. Keine `seq` Spalte.

### 4.1 clients

Auftraggeber/Bauherr. Aktuell 1:1 mit Projekt. Später zentrale Firmendatenbank (ADR-021).

```sql
CREATE TABLE clients (
    id TEXT PRIMARY KEY,                   -- ULID
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,              -- UTC (ISO 8601)
    created_by TEXT NOT NULL DEFAULT '',   -- IUserContext.DisplayName
    last_modified_at TEXT NOT NULL,        -- UTC (ISO 8601)
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
);
```

### 4.2 projects

Kernentität. Jedes Bauprojekt ist eine Zeile.

```sql
CREATE TABLE projects (
    id TEXT PRIMARY KEY,                   -- ULID
    project_number TEXT NOT NULL DEFAULT '',  -- YYYYMM (aus Startdatum)
    name TEXT NOT NULL DEFAULT '',         -- Kurzname "ÖWG-Dobl"
    full_name TEXT NOT NULL DEFAULT '',    -- Langname
    status TEXT NOT NULL DEFAULT 'Active', -- "Active" | "Completed" (ADR-025)
    project_type TEXT NOT NULL DEFAULT '', -- aus AppSettings.ProjectTypes
    client_id TEXT,                        -- FK → clients.id (ULID)
    -- Adresse (aufgeteilt für Google Maps API — ADR-003)
    street TEXT NOT NULL DEFAULT '',
    house_number TEXT NOT NULL DEFAULT '',
    postal_code TEXT NOT NULL DEFAULT '',
    city TEXT NOT NULL DEFAULT '',
    municipality TEXT NOT NULL DEFAULT '',
    district TEXT NOT NULL DEFAULT '',
    state TEXT NOT NULL DEFAULT 'Steiermark',
    -- Koordinaten (für GIS-Integration)
    coordinate_system TEXT NOT NULL DEFAULT 'EPSG:31258',
    coordinate_east REAL NOT NULL DEFAULT 0,
    coordinate_north REAL NOT NULL DEFAULT 0,
    -- Kataster
    cadastral_kg TEXT NOT NULL DEFAULT '',
    cadastral_kg_name TEXT NOT NULL DEFAULT '',
    cadastral_gst TEXT NOT NULL DEFAULT '',
    -- Zeitraum
    project_start TEXT,                    -- YYYY-MM-DD
    construction_start TEXT,
    planned_end TEXT,
    actual_end TEXT,
    -- Pfade
    root_path TEXT NOT NULL DEFAULT '',
    plans_path TEXT NOT NULL DEFAULT '',
    inbox_path TEXT NOT NULL DEFAULT '',
    photos_path TEXT NOT NULL DEFAULT '',
    documents_path TEXT NOT NULL DEFAULT '',
    protocols_path TEXT NOT NULL DEFAULT '',
    invoices_path TEXT NOT NULL DEFAULT '',
    -- Globales Nullniveau (v0.24.2)
    use_global_zero_level INTEGER NOT NULL DEFAULT 0,  -- 0=pro Bauteil, 1=global
    global_zero_level REAL NOT NULL DEFAULT 0,          -- m ü.A., nur wenn use_global=1
    -- Sonstiges
    tags TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,              -- UTC (ISO 8601)
    created_by TEXT NOT NULL DEFAULT '',   -- IUserContext.DisplayName
    last_modified_at TEXT NOT NULL,        -- UTC (ISO 8601)
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (client_id) REFERENCES clients(id)
);
```

### 4.3 buildings (Legacy)

Altes Building-Modell. Ersetzt durch building_parts + building_levels seit v0.13.1. Wird bei nächstem Major-Update entfernt.

```sql
CREATE TABLE buildings (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    name TEXT NOT NULL DEFAULT '',
    short_name TEXT NOT NULL DEFAULT '',
    type TEXT NOT NULL DEFAULT '',
    levels TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

### 4.4 building_parts

Bauteile eines Projekts (z.B. "Haus 5", "Haus 6"). Seit v0.13.1.

```sql
CREATE TABLE building_parts (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    short_name TEXT NOT NULL DEFAULT '',
    folder_name TEXT NOT NULL DEFAULT '',  -- physischer Ordnername, EINMAL beim Anlegen erzeugt (ADR-059-Addendum; Frühphase: bpm.db-Reset)
    description TEXT NOT NULL DEFAULT '',
    building_type TEXT NOT NULL DEFAULT '',
    zero_level_absolute REAL NOT NULL DEFAULT 0,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_building_parts_project_id ON building_parts(project_id);
```

**Gelesen von:** Kalkulation (work_packages.building_part_id), Ziegelberechnung

### 4.5 building_levels

Geschoße eines Bauteils. Seit v0.13.1.

```sql
CREATE TABLE building_levels (
    id TEXT PRIMARY KEY,                   -- ULID
    building_part_id TEXT NOT NULL,
    prefix INTEGER NOT NULL DEFAULT 0,
    name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    rdok REAL NOT NULL DEFAULT 0,
    fbok REAL NOT NULL DEFAULT 0,
    rduk REAL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    folder_name TEXT NOT NULL DEFAULT '',  -- ADR-061: physischer Geschoss-Ordner "{PrefixString} {Name}" (z.B. "-01 KG"/"00 EG"/"01 OG1"), EINMAL beim Anlegen gesetzt, danach rename-stabil
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE
);

CREATE INDEX idx_building_levels_part_id ON building_levels(building_part_id);
```

> **ADR-061 / BPM-113.02 (Frühphase, keine Migration):** `folder_name` neu.
> Betroffene Datei: `bpm.db` → löschen, BPM legt sie beim nächsten Start neu an.
> Befüllt wird `folder_name` ab Slice 0.3 (`InsertBuildingLevel`).
```

**Berechnete Werte (im Code, NICHT in DB gespeichert):**
- Geschosshöhe = FBOK(n+1) − FBOK(n)
- Rohbauhöhe = RDOK(n+1) − RDOK(n)
- Deckenstärke = RDOK(n+1) − RDUK(n) ← korrigiert v0.24.2, war vorher RDOK−RDUK gleiche Zeile
- Fußbodenaufbau = FBOK − RDOK
**Gelesen von:** Kalkulation (work_packages.level_id), Ziegelberechnung

### 4.6 project_participants

Beteiligte am Projekt. Seit v0.14.0.

```sql
CREATE TABLE project_participants (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT '',
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    contact_id TEXT NOT NULL DEFAULT '',
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_participants_project_id ON project_participants(project_id);
```

**Zukunft:** `contact_id` verknüpft mit zentralem Adressbuch

### 4.7 project_links

Portal-Links und eigene Links pro Projekt. Seit v0.15.0.

```sql
CREATE TABLE project_links (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    name TEXT NOT NULL DEFAULT '',
    url TEXT NOT NULL DEFAULT '',
    link_type TEXT NOT NULL DEFAULT 'Custom',
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_links_project_id ON project_links(project_id);
```

### 4.8 schema_version

```sql
CREATE TABLE schema_version (
    version TEXT NOT NULL
);
```

### 4.9 segment_type_groups (BPM-108, Schema 2.2)

Gruppen für PlanManager-Segmenttypen (Identifikation, Räumlich, Inhaltlich, Sonstiges). Built-ins sind editierbar in `name`, `sort_order`, `is_active`; nicht-modifizierte Felder werden bei App-Update aus dem Seed übernommen (`user_modified_*`-Flags). Soft-Delete only.

```sql
CREATE TABLE segment_type_groups (
    id TEXT PRIMARY KEY,                   -- snake_case String für Built-ins (z. B. "grp_identifikation"), ULID für Custom
    name TEXT NOT NULL,                    -- "Identifikation"
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    is_builtin INTEGER NOT NULL DEFAULT 0,
    -- Built-in Update-Policy
    builtin_version INTEGER NOT NULL DEFAULT 1,
    user_modified_name INTEGER NOT NULL DEFAULT 0,
    user_modified_sort INTEGER NOT NULL DEFAULT 0,
    user_modified_active INTEGER NOT NULL DEFAULT 0,
    -- Sync-Felder ADR-050
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
);
```

**Built-in IDs:** `grp_identifikation`, `grp_raeumlich`, `grp_inhaltlich`, `grp_sonstiges`.

### 4.10 segment_types (BPM-108, Schema 2.2)

Segmenttypen für Dateinamen-Klassifikation im PlanManager (Plannummer, Geschoss, Akustik-Klasse, …). Zwei-Schichten-Modell: `token_key` für Templates, `semantic_role` für Wizard-Validierung. Custom-Typen haben immer `semantic_role = NULL`.

```sql
CREATE TABLE segment_types (
    id TEXT PRIMARY KEY,                   -- Built-in: snake_case (z. B. "plan_number"), Custom: ULID
    name TEXT NOT NULL,                    -- UI-Label (editierbar)
    color TEXT NOT NULL,                   -- Hex #RRGGBB
    token_key TEXT NOT NULL,               -- snake_case, stabil für renameSchema/folderHierarchy
    semantic_role TEXT,                    -- NULL für Custom; "PlanNumber", "PlanIndex", "ProjectNumber",
                                           --   "Date", "Description", "Spatial", "Ignore", "None" für Built-in
    group_id TEXT NOT NULL,                -- FK → segment_type_groups.id
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    is_builtin INTEGER NOT NULL DEFAULT 0,
    -- Built-in Update-Policy
    builtin_version INTEGER NOT NULL DEFAULT 1,
    user_modified_name INTEGER NOT NULL DEFAULT 0,
    user_modified_color INTEGER NOT NULL DEFAULT 0,
    user_modified_sort INTEGER NOT NULL DEFAULT 0,
    user_modified_active INTEGER NOT NULL DEFAULT 0,
    user_modified_group INTEGER NOT NULL DEFAULT 0,
    -- Sync-Felder ADR-050
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (group_id) REFERENCES segment_type_groups(id)
);

CREATE INDEX idx_segment_types_group_id ON segment_types(group_id);
CREATE UNIQUE INDEX ux_segment_types_token_key_active
    ON segment_types(token_key) WHERE is_deleted = 0;
```

**Built-in IDs (16 Typen):**
- Identifikation: `plan_number` (PlanNumber), `plan_index` (PlanIndex), `project_number` (ProjectNumber)
- Räumlich (`Spatial`): `geschoss`, `haus`, `bauteil`, `bauabschnitt`, `stiege`, `achse`, `zone`, `block`, `objekt`
- Inhaltlich: `planart` (None), `description` (Description)
- Sonstiges: `datum` (Date), `ignore` (Ignore)

**Immutable nach Anlage:** `id`, `token_key`, `semantic_role` (bei Built-ins), `is_builtin`.

**Referenz:** ADR-056 (Zwei-Schichten-Modell), CGR-2026-05-12-segmenttyp-architektur (3-Runden-Review).

### 4.11 building_part_aliases (BPM-109, ⚠ geplant — Foundation Slice)

Auto-Learn-Mapping: merkt sich, welche Dateinamen-Schreibweise (z.B. `H1`, `Haus 1`, `H 1`) auf welches Bauteil zeigt. Wird beim Plan-Import zum Auflösen von Segmentwerten → `building_part_id` genutzt (aktiv erst post-V1, BPM-109.06).

Liegt bewusst in `bpm.db` statt `planmanager.db` (ADR-058-Addendum, CGR r3): zentral, gesynct, mit **hartem FK** auf `building_parts(id)` (gleiche DB-Datei) — reduziert die Cross-DB-Soft-References von 4 auf 3.

```sql
CREATE TABLE building_part_aliases (
    id TEXT PRIMARY KEY,                    -- ULID
    project_id TEXT NOT NULL,               -- FK projects (Aliase sind projektgebunden)
    building_part_id TEXT NOT NULL,         -- FK building_parts (hart, Innen-FK)
    alias_value TEXT NOT NULL,              -- Original-Schreibweise (z.B. "Haus 1")
    normalized_alias_value TEXT NOT NULL,   -- Lowercase/normalisiert (z.B. "haus_1")
    -- Sync-Felder ADR-050
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE,
    UNIQUE (project_id, normalized_alias_value)
);

CREATE INDEX idx_building_part_aliases_part_id ON building_part_aliases(building_part_id);
```

Analog später möglich: `building_level_aliases` — nicht Teil des Foundation Slice (YAGNI).

**Referenz:** ADR-058-Addendum (Cross-DB Soft References), CGR-2026-06-08-plan-archiv-architektur r3.

---

### 4.12 document_types (ADR-059-Addendum, BPM-111.05)

Dokumenttyp-Stammdaten je Projekt — Quelle für Ring 1 des Radials und das
typabhängige Unterteilungs-Schema. `ring2_source` bestimmt Ring 2
(`building_parts` = räumlich mit Ring 3 Geschoss, `categories` = typgebundene
Kategorien ohne Ring 3, `none` = keine Unterteilung).

```sql
CREATE TABLE document_types (
    id TEXT PRIMARY KEY,                   -- ULID (projekt-scoped; Built-ins via is_builtin + name identifiziert)
    project_id TEXT NOT NULL,
    name TEXT NOT NULL,                    -- Anzeigename (z.B. "Polierplan")
    folder_name TEXT NOT NULL,             -- physischer Typordner unter dem Root, EINMAL erzeugt; LEER bei Root-Typ (ADR-061)
    key TEXT NOT NULL DEFAULT '',          -- ADR-061: stabiler Schlüssel, != UI-Name, nach Anlage gesperrt (Default '' bis Seed 0.4)
    root_relative_path TEXT NOT NULL DEFAULT '', -- ADR-061: echter Ablage-Root je Typ ("01 Planunterlagen"/"06 Protokolle"); CHECK<>'' folgt in 0.4
    color_hex TEXT,                        -- Radial-Segmentfarbe (Seed: Mockup-Palette)
    ring2_source TEXT NOT NULL DEFAULT 'building_parts'
        CHECK (ring2_source IN ('building_parts', 'categories', 'none')),
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_builtin INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_document_types_project_id ON document_types(project_id);

-- ADR-061: key eindeutig je Projekt. Partiell, damit leere Permissive-Keys
-- (vor Seed 0.4) und soft-deletete Typen nicht kollidieren.
CREATE UNIQUE INDEX idx_document_types_project_key
    ON document_types(project_id, key) WHERE key <> '' AND is_deleted = 0;
```

> **ADR-061 / BPM-113.02 (Frühphase, keine Migration):** `key` + `root_relative_path`
> neu (permissive Defaults; `CHECK(root_relative_path<>'')` + voller Unique kommen in
> Slice 0.4 mit dem Seed). Betroffene Datei: `bpm.db` → löschen, BPM seedet neu.

**Seed bei Projektanlage (ADR-061 Slice 0.4):** aus dem `FolderTemplate` — ein
Node wird Dokumenttyp gdw `CreatesDocumentType == true`. Default-Set: Ausschreibungsplan,
Polierplan, Schalung, Bewehrung, Fertigteile, Baustelleneinrichtung (alle unter Root
„01 Planunterlagen", `folder_name` = nummerierter Unterordner) · Protokolle (Root-Typ
„06 Protokolle", `folder_name` leer, `categories`). `key`/`root_relative_path`/`folder_name`
stammen aus der Template-Struktur. `planmanager.db.plan_documents.document_type_id`
referenziert diese Tabelle als Cross-DB-Soft-Reference (kein FK).

### 4.13 document_type_categories (ADR-059-Addendum, BPM-111.05)

Typgebundene Kategorien (z.B. Protokollarten, Fertigteil-Kategorien) — Quelle
für Ring 2 bei `ring2_source='categories'`. „+ Neu…" im Radial legt hier an
(+ physischer Ordner sofort).

```sql
CREATE TABLE document_type_categories (
    id TEXT PRIMARY KEY,                   -- ULID
    document_type_id TEXT NOT NULL,
    name TEXT NOT NULL,                    -- z.B. "Baubesprechung"
    folder_name TEXT NOT NULL,             -- EINMAL erzeugt, App ist alleiniger Schreiber
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    created_by TEXT NOT NULL DEFAULT '',
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT NOT NULL DEFAULT '',
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_type_id) REFERENCES document_types(id) ON DELETE CASCADE
);

CREATE INDEX idx_doc_type_categories_type_id ON document_type_categories(document_type_id);
```

## 5. Geplante Tabellen (nach V1)

### 5.1 employees (Zeiterfassung)

```sql
CREATE TABLE employees (
    id TEXT PRIMARY KEY,                   -- ULID
    name TEXT NOT NULL,
    short_name TEXT,
    qualification TEXT,
    hourly_rate REAL,
    active INTEGER DEFAULT 1,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

**Konzept:** ModuleZeiterfassung.md, ModuleKalkulation.md

### 5.2 time_entries (Zeiterfassung)

```sql
CREATE TABLE time_entries (
    id TEXT PRIMARY KEY,                   -- ULID
    date TEXT NOT NULL,
    employee_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    hours REAL NOT NULL,
    absence_type TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

CREATE INDEX idx_time_entries_employee ON time_entries(employee_id);
CREATE INDEX idx_time_entries_project ON time_entries(project_id);
CREATE INDEX idx_time_entries_date ON time_entries(date);
```

### 5.3 work_packages (Kalkulation) — ZENTRALE TABELLE

```sql
CREATE TABLE work_packages (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    building_part_id TEXT,
    level_id TEXT,
    activity TEXT NOT NULL,
    lv_position_id TEXT,
    planned_quantity REAL,
    unit TEXT NOT NULL,
    source TEXT,
    track_separately INTEGER DEFAULT 0,
    color TEXT,
    sort_order INTEGER,
    status TEXT DEFAULT 'planned',
    started_at TEXT,
    completed_at TEXT,
    actual_hours REAL DEFAULT 0,
    actual_quantity REAL,
    performance_value REAL,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id),
    FOREIGN KEY (lv_position_id) REFERENCES lv_positions(id)
);

CREATE INDEX idx_work_packages_project ON work_packages(project_id);
```

**Konzept:** ModuleKalkulation.md Kapitel 3

### 5.4 work_assignments (Kalkulation / Arbeitseinteilung)

```sql
CREATE TABLE work_assignments (
    id TEXT PRIMARY KEY,                   -- ULID
    date TEXT NOT NULL,
    employee_id TEXT NOT NULL,
    work_package_id TEXT NOT NULL,
    hours REAL,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id) ON DELETE CASCADE
);

CREATE INDEX idx_work_assign_date ON work_assignments(date);
CREATE INDEX idx_work_assign_employee ON work_assignments(employee_id);
```

### 5.5 lv_positions (Kalkulation / LV-Import)

```sql
CREATE TABLE lv_positions (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    position_number TEXT NOT NULL,
    short_text TEXT NOT NULL,
    quantity REAL,
    unit TEXT,
    unit_price REAL,
    completed_quantity REAL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

### 5.6 performance_catalog (Kalkulation / Nachkalkulation)

```sql
CREATE TABLE performance_catalog (
    id TEXT PRIMARY KEY,                   -- ULID
    activity TEXT NOT NULL,
    unit TEXT NOT NULL,
    hours_per_unit REAL NOT NULL,
    project_id TEXT,
    work_package_id TEXT,
    measured_at TEXT,
    quantity REAL,
    total_hours REAL,
    workers INTEGER,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

### 5.7 project_difficulty (Kalkulation / Bauzeitprognose)

```sql
CREATE TABLE project_difficulty (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    factor_name TEXT NOT NULL,
    factor_value REAL NOT NULL,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

### 5.8 diary_days + diary_notes (Bautagebuch — ADR-047)

Aufgeteilt in Tageskopf + Notizen (statt einer großen `diary_entries`-Tabelle). Ermöglicht dass mehrere Poliere gleichzeitig Notizen zum selben Tag schreiben (weniger Sync-Konflikte).

```sql
CREATE TABLE diary_days (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    date TEXT NOT NULL,
    weather TEXT,
    temperature_min REAL,
    temperature_max REAL,
    personnel_count INTEGER,
    absent_count INTEGER,
    confirmed INTEGER DEFAULT 0,
    confirmed_at TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_diary_day ON diary_days(project_id, date);

CREATE TABLE diary_notes (
    id TEXT PRIMARY KEY,                   -- ULID
    diary_day_id TEXT NOT NULL,
    note_type TEXT NOT NULL,            -- "activity", "remark", "photo"
    content TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (diary_day_id) REFERENCES diary_days(id) ON DELETE CASCADE
);

CREATE INDEX idx_diary_notes_day ON diary_notes(diary_day_id);
```

### 5.9 contacts (Adressbuch)

```sql
CREATE TABLE contacts (
    id TEXT PRIMARY KEY,                   -- ULID
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    address TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    outlook_id TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### 5.10 material_orders (Task-Management)

```sql
CREATE TABLE material_orders (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    work_package_id TEXT,
    building_part_id TEXT,
    level_id TEXT,
    material TEXT NOT NULL,
    quantity REAL NOT NULL,
    unit TEXT NOT NULL,
    delivery_date_requested TEXT,
    delivery_date_confirmed TEXT,
    urgency TEXT DEFAULT 'Normal',
    source TEXT DEFAULT 'Intern',
    status TEXT DEFAULT 'open',
    external_task_id TEXT,
    external_system TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id),
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id)
);
```

### 5.11 external_call_log (Datenschutz / Audit)

```sql
CREATE TABLE external_call_log (
    id TEXT PRIMARY KEY,                   -- ULID
    timestamp TEXT NOT NULL,
    module TEXT NOT NULL,
    target_domain TEXT NOT NULL,
    classification TEXT NOT NULL,
    purpose TEXT,
    status_code INTEGER,
    blocked INTEGER DEFAULT 0,
    decision_reason TEXT
);
```

**Löschung:** Automatisch nach 90 Tagen
**Negativliste:** Kein Request-/Response-Body, keine Headers, keine IPs, keine Personendaten.

**`decision_reason` — Kontrolliertes Vokabular:**

| Code | Bedeutung |
|------|-----------|
| `allowed_class_a` | Klasse A, keine Einschränkung |
| `allowed_user_confirmed` | User hat Klasse B/C explizit bestätigt |
| `allowed_anonymized_payload` | Payload wurde vor Senden anonymisiert |
| `allowed_internal_mode` | RelaxedPrivacyPolicy (interner Betrieb) |
| `blocked_global_killswitch` | Globaler Kill-Switch aktiv |
| `blocked_module_disabled` | Modul in Einstellungen deaktiviert |
| `blocked_auto_calls_not_enabled` | Auto-Calls nicht freigeschaltet |
| `blocked_class_c_requires_override` | Klasse C ohne Override |
| `blocked_dpa_not_confirmed` | KI-Modul ohne DPA-Bestätigung |
| `blocked_policy_denied` | Sonstige Policy-Ablehnung |

### 5.12 project_shares (Multi-User / Projektfreigabe)

```sql
CREATE TABLE project_shares (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    shared_with_user TEXT NOT NULL,
    permission TEXT NOT NULL,
    shared_at TEXT NOT NULL DEFAULT (datetime('now')),
    valid_until TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

---

## 6. PlanManager-Datenbank (separat — ✅ implementiert)

Pro Projekt eine eigene SQLite-DB. Liegt in `%LocalAppData%\BauProjektManager\Projects\<ProjektID>\planmanager.db`.

**Status:** `PlanManagerDatabase.cs` implementiert (v0.25.15). Schema v1.0, 6 Tabellen + schema_version.

**Geplant:** Schema v2.0 (BPM-109, Foundation Slice) — Drei-Ebenen-Modell mit `plan_documents` + erweiterte `plan_revisions` + Segment/Event/ContextLink/Alias-Tabellen. Siehe Kap. 6.7 unten und ADR-058. Reset-Anweisung bei Schema-Wechsel: `planmanager.db` löschen → BPM erstellt sie beim nächsten Start neu (Frühphasen-Regel).

**6 Tabellen (v1.0):** 3 Plan-Revisions-Cache + 3 Import-Journal (PlanManager.md v2.0, Cross-Review 09.04.2026).

### 6.1 plan_revisions (Cache)

```sql
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,                -- ULID
    document_key TEXT NOT NULL,         -- aus identityFields: "Polierplan_103_H5"
    document_type_id TEXT,             -- FK → Profil-ID (welches Profil hat diesen Plan erkannt)
    plan_number TEXT NOT NULL,
    plan_index TEXT,                    -- NULL bei Erstausgabe / IndexSource=None
    document_type TEXT NOT NULL,
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    index_source TEXT NOT NULL,         -- "FileName", "None", "PlanHeader"
    revision_status TEXT NOT NULL,      -- "current", "archived"
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX ux_plan_revision_current
ON plan_revisions(document_key, revision_status)
WHERE revision_status = 'current';
```

### 6.2 plan_files (Cache)

```sql
CREATE TABLE plan_files (
    id TEXT PRIMARY KEY,                -- ULID
    file_name TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    file_type TEXT NOT NULL,            -- "pdf", "dwg", "jpg", "other"
    md5_hash TEXT NOT NULL,             -- IMMER Pflicht (universeller Fingerabdruck)
    file_size INTEGER NOT NULL,
    origin_mode TEXT NOT NULL,          -- "autoGrouped", "manualLinked", "standalone"
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
```

### 6.3 revision_file_links (n:m Verknüpfung)

```sql
CREATE TABLE revision_file_links (
    revision_id TEXT NOT NULL,
    file_id TEXT NOT NULL,
    link_mode TEXT NOT NULL,            -- "auto", "manual"
    is_primary INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (revision_id, file_id),
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (file_id) REFERENCES plan_files(id)
);
```

**n:m Verknüpfung:** Eine Datei kann mehreren Revisionen zugeordnet sein (Sammel-DWG). Eine Datei ohne Links ist standalone.

### 6.4 import_journal

```sql
CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,                   -- ULID
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,
    source_path TEXT NOT NULL,
    file_count INTEGER NOT NULL,
    profile_id TEXT,
    machine_name TEXT,
    error_message TEXT
);
```

### 6.5 import_actions

```sql
CREATE TABLE import_actions (
    id TEXT PRIMARY KEY,                   -- ULID
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,          -- "new", "indexUpdate", "changed", "changedSameIdx",
                                       -- "olderRevision", "skip", "manual", "learnIndex",
                                       -- "skipDuplicate" (BPM-120 T2, Bucket A)
    action_status TEXT NOT NULL,        -- "pending", "completed", "failed"
    document_key TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    source_path TEXT NOT NULL,          -- relativ zum Projektordner
    destination_path TEXT,              -- relativ; NULL bei skipDuplicate (BPM-120 T2)
    archive_path TEXT,                  -- relativ; bei indexUpdate deterministisch VOR
                                       -- der ersten Mutation journalisiert (ADR-064 AK 5)
    md5 TEXT,                           -- BPM-120 T2: Fingerprint der Quelldatei
                                       -- (Pflichtinhalt bei skipDuplicate — Recovery-Verify P.7)
    file_size INTEGER,                  -- BPM-120 T2: Groesse der Quelldatei
    document_type_id TEXT,              -- BPM-120 T5: fuer die vollstaendige
                                       -- Struktur-Herstellung im Recovery Forward (AK 9)
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE INDEX idx_actions_import ON import_actions(import_id);
```

**Vorab-Journalisierung (BPM-120 T2, ADR-064 P.2):** Journal-Header + ALLE Actions
eines Imports werden vollständig geschrieben, BEVOR die erste Datei mutiert wird —
inkl. deterministischer `source_path`/`destination_path`/`archive_path`. Bestätigte
MD5-Dubletten sind echte Actions (`action_type = 'skipDuplicate'`, `destination_path`
NULL, `md5` + `file_size` gesetzt): beim Confirm direktes Delete der Eingangs-Kopie,
journalisiert + recovery-fähig (ADR-064 P.7), bewusst NICHT undo-bar.

**Schema-Änderung mit BPM-120 T2/T5 (Frühphase, keine Migration):**
Betroffene Datei: `%LocalAppData%\BauProjektManager\Projects\<ProjektID>\planmanager.db`.
Aktion: User löscht die Datei → BPM erstellt sie beim nächsten App-Start neu
(`destination_path` nullable, neue Spalten `md5` + `file_size` + `document_type_id`).

### 6.6 import_action_files

```sql
CREATE TABLE import_action_files (
    id TEXT PRIMARY KEY,                   -- ULID
    action_id TEXT NOT NULL,
    file_id TEXT,                       -- FK → plan_files.id (optional, für Cache-Verknüpfung)
    file_name TEXT NOT NULL,
    original_file_name TEXT,            -- vor Umbenennung (NULL wenn nicht umbenannt)
    final_file_name TEXT,               -- nach Umbenennung (NULL wenn nicht umbenannt)
    file_type TEXT NOT NULL,            -- "pdf", "dwg", "jpg", "other"
    source_path TEXT NOT NULL,          -- relativ zum Projektordner
    destination_path TEXT NOT NULL,     -- relativ
    md5_hash TEXT NOT NULL,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);

CREATE INDEX idx_action_files_action ON import_action_files(action_id);
```

---

### 6.7 Schema v2.0 (BPM-109 Plan-Archiv-Persistenz Foundation Slice — 🔄 DDL implementiert)

**Zweck:** Drei-Ebenen-Modell (Document/Revision/File) analog Industrie-Standard (Procore, Aconex, think project!) für zeitbezogene Cross-Modul-Abfragen aus Bautagebuch (BPM-056), Foto (BPM-057), Vorlagen (BPM-061).

**Status:** Definition entschieden (ADR-058 + ADR-058-Addendum). **DDL implementiert in BPM-109.01 (v0.28.55)** — `PlanManagerDatabase.EnsureTables()` erzeugt alle Tabellen unten (Cross-DB-Bezüge als SoftRef ohne FK). Domain Models + Repository folgen in BPM-109.02; die Cache-Repository-Methoden sind bis dahin Fail-Fast. Foundation Slice (`.01–.04 + .05a` Stub) ist V1-Sperrposten — siehe BPM-109.

**Reset-Anweisung bei Einführung (Frühphasen-Regel):**
Betroffene Datei: `%LocalAppData%\BauProjektManager\Projects\<ProjektID>\planmanager.db`.
Aktion: User löscht die Datei → BPM erstellt sie beim nächsten App-Start neu mit Schema v2.0. Keine Migration, keine Backward-Compatibility.

**Bestehende Tabellen aus v1.0 die UNVERÄNDERT bleiben:**
`plan_files` (Kap. 6.2), `revision_file_links` (Kap. 6.3), `import_journal` (Kap. 6.4), `import_action_files` (Kap. 6.6).
`import_actions` (Kap. 6.5) wurde mit **BPM-120 T2** erweitert (destination_path nullable, md5 + file_size) — siehe dort.

**`plan_revisions` (Kap. 6.1) wird UMGEBAUT** — siehe 6.7.2 unten.

> **⚠ Cross-DB-Referenzen (ADR-058-Addendum, CGR r3):**
> `planmanager.db` liegt pro Projekt separat. Spalten, die auf `bpm.db`-Tabellen zeigen
> (`building_parts`, `building_levels`, `segment_types`), sind **Soft References** (`TEXT`-Spalten
> **ohne** `FOREIGN KEY`). SQLite erzwingt keine FK-Constraints über getrennte DB-Dateien.
> Gültigkeit wird **service-seitig** validiert (Import-Resolve, Lookup, Stammdaten-Soft-Delete).
> Harte FKs werden nur **innerhalb** `planmanager.db` definiert (siehe Innen-FKs in 6.7.1–6.7.5).
> `building_part_aliases` lebt **nicht mehr hier**, sondern in `bpm.db` (Kap. 4.11) — mit hartem FK.

#### 6.7.1 plan_documents (NEU)

Logisches Dokument über alle Revisionen hinweg. Ziel für Cross-Modul-FKs.

```sql
CREATE TABLE plan_documents (
    id TEXT PRIMARY KEY,                -- ULID
    project_id TEXT NOT NULL,           -- redundant zu DB-Pfad, bewusst für Sync/Export
    document_key TEXT NOT NULL UNIQUE,  -- Natural Key vom DocumentKeyBuilder
    document_type_id TEXT NOT NULL,     -- Profil-ID (aus JSON .bpm/profiles, post-V1 FK auf recognition_profiles BPM-092)
    plan_number TEXT NOT NULL,
    document_type TEXT NOT NULL,
    title TEXT NOT NULL DEFAULT '',
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    building_part_id TEXT,              -- SoftRef bpm.db.building_parts(id), NULL wenn nicht gemappt (kein FK, Cross-DB)
    building_level_id TEXT,             -- SoftRef bpm.db.building_levels(id), NULL wenn nicht gemappt (kein FK, Cross-DB)
    created_at TEXT NOT NULL,           -- UTC ISO 8601
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
    -- Keine FK auf building_parts/building_levels: Cross-DB Soft Reference (ADR-058-Addendum)
);

CREATE INDEX idx_plan_documents_lookup
ON plan_documents(project_id, building_part_id, building_level_id, document_type_id, is_deleted);

CREATE INDEX idx_plan_documents_key ON plan_documents(document_key);
```

#### 6.7.2 plan_revisions (UMGEBAUT)

Versionierte Revision mit Zeitstempeln für Zeitreise. Ersetzt v1.0-Definition aus Kap. 6.1.

```sql
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,                -- ULID
    document_id TEXT NOT NULL,          -- FK plan_documents
    plan_index TEXT,                    -- NULL bei Erstausgabe / IndexSource=None
    index_source TEXT NOT NULL,         -- "FileName", "None", "PlanHeader"
    revision_status TEXT NOT NULL
        CHECK (revision_status IN ('current', 'superseded', 'rejected')),
    current_from TEXT NOT NULL,         -- UTC, wann diese Revision aktuell wurde (Gültigkeitsfenster)
    superseded_at TEXT,                 -- UTC, wann durch Nächste ersetzt (NULL solange current)
    received_at TEXT NOT NULL,          -- UTC, wann importiert (Hinzufügedatum)
    released_at TEXT,                   -- UTC, Freigabedatum des Index (BPM-109.04b); Quelle: Text-Zuweisung (BPM-118) > OCR/manuell (post-V1); NULL wenn unbekannt
    change_note TEXT NOT NULL DEFAULT '', -- Änderungshinweis der Revision (BPM-118 Text-Zuweisung); '' wenn keiner
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (last_import_id) REFERENCES import_journal(id)
);

CREATE UNIQUE INDEX ux_plan_revisions_current
ON plan_revisions(document_id)
WHERE revision_status = 'current' AND is_deleted = 0;

CREATE INDEX idx_plan_revisions_timetravel
ON plan_revisions(document_id, current_from, superseded_at, is_deleted);
```

**Status-Semantik:**
- `current` — aktuelle gültige Revision (pro `document_id` maximal eine via UNIQUE-Index)
- `superseded` — durch nächste Revision fachlich abgelöst, `superseded_at` gesetzt
- `rejected` — bewusst verworfene Revision (z.B. ungültiger Vorabzug aus `RevisionDecisionService`), kein `current`, `superseded_at` als Verwerfungs-Zeitpunkt

**Drei-Zeiten-Modell (BPM-109.04/.04b):**
- `current_from` / `superseded_at` — technisches **Gültigkeitsfenster** (Supersede-Kette). Invariante: `superseded_at`(alt) == `current_from`(neu) → Zeitreise lückenlos (ein `actionTime` pro Import-Aktion).
- `received_at` — **Hinzufügedatum** (Import), immer bekannt.
- `released_at` — **Freigabedatum** des Index, fachlich präziser. Quelle: **Text-Zuweisung aus der PDF-Vorschau (BPM-118, seit v0.28.122)** > Plankopf-OCR (post-V1) > manuell (post-V1) > Dateiname (selten). NULL solange unbekannt.
- **Bautagebuch-Priorisierung (post-V1, BPM-056):** effektives Datum = `released_at` wenn vorhanden, sonst `received_at` — bei Fallback **visuell markiert** (andere Farbe + Hinweis „Importdatum"). Geliefert über `IPlanLookupService` (`EffectiveDate`/`IsDateFallback`).

**Änderungshinweis (BPM-118, v0.28.119/.122):**
- `change_note` — Freitext-Änderungshinweis der Revision (z. B. aus dem Plankopf markiert und per Text-Zuweisung übernommen). `''` wenn keiner erfasst. Anzeige: Detail-Panel-Historie, Spalte „Änderung" (Fallback: „Erstausgabe" bei Index NULL, sonst „—").
- Schreibpfad: `PendingAssignment.ChangeNote` → `ClassifiedImportFile.ChangeNote` → `InsertRevision(change_note)`. Frühphasen-Hinweis: Spalte kam in v0.28.119 — bestehende `planmanager.db` löschen statt Migration (INDEX.md-Frühphasenregel).

#### 6.7.3 plan_document_segments (NEU)

Extrahierte Segmentwerte als KV-Tabelle, FK auf `segment_types` aus ADR-056.

```sql
CREATE TABLE plan_document_segments (
    id TEXT PRIMARY KEY,                    -- ULID
    document_id TEXT NOT NULL,              -- FK plan_documents (Innen-FK, hart)
    segment_type_id TEXT NOT NULL,          -- SoftRef bpm.db.segment_types(id), BPM-108 (kein FK, Cross-DB)
    segment_key TEXT NOT NULL,              -- Denormalisierung für Debug/Export (token_key aus segment_types)
    raw_value TEXT NOT NULL,                -- Original aus FileNameParser (z.B. "H1")
    normalized_value TEXT NOT NULL,         -- Lowercase/normalisiert für Filter (z.B. "h1")
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    -- Keine FK auf segment_types: Cross-DB Soft Reference (ADR-058-Addendum)
    UNIQUE (document_id, segment_type_id)   -- pro Dokument ein Wert je Segmenttyp
);

CREATE INDEX idx_plan_document_segments_lookup
ON plan_document_segments(segment_type_id, normalized_value, is_deleted);
```

**Verhältnis zu `building_part_id`/`building_level_id` in `plan_documents`:**
Für die häufigen Modul-Filter (Haus + Geschoss) gibt es FK-Spalten direkt am Document. Alle weiteren Segmentwerte (Bauteil, Bauabschnitt, Zone, …) landen in `plan_document_segments`. Beide Wege koexistieren bewusst.

#### 6.7.3b plan_document_tags (NEU — BPM-127)

Freie Schlagworte je Dokument. **Bewusst getrennt von `plan_document_segments`:**
Segmente sind die strukturierte Zerlegung des *Dateinamens* (Segmenttypen aus BPM-108),
Tags sind frei vergebene inhaltliche Auszeichnungen ("Beton C25/30", "Deckendurchbruch").

```sql
CREATE TABLE plan_document_tags (
    id TEXT PRIMARY KEY,                    -- ULID
    document_id TEXT NOT NULL,              -- FK plan_documents (Innen-FK, hart)
    tag TEXT NOT NULL,                      -- Anzeigetext wie eingegeben
    normalized_tag TEXT NOT NULL,           -- trim + lowercase: Duplikatschutz + Vorschläge
    created_at TEXT NOT NULL,
    created_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,  -- Soft Delete (Sync-Nachvollziehbarkeit)
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    UNIQUE (document_id, normalized_tag)    -- ein Tag je Dokument, unabhängig von Schreibweise
);

CREATE INDEX idx_plan_document_tags_lookup
ON plan_document_tags(normalized_tag, is_deleted);
```

**Kein DB-Reset nötig:** Die Tabelle ist rein additiv und wird über `CREATE TABLE IF NOT EXISTS`
beim nächsten Öffnen auch in bestehenden Projekt-Datenbanken angelegt.

**Verhalten:** `AddTag` reaktiviert einen soft-gelöschten Tag (`ON CONFLICT … DO UPDATE`) statt
einen zweiten Eintrag anzulegen; leere Eingaben werden ignoriert. `GetAllTags` liefert die
projektweiten Vorschläge nach Häufigkeit.

#### 6.7.4 plan_revision_events (NEU)

Minimaler Audit-Trail für Statuswechsel. KEIN voller Before/After-Snapshot.

```sql
CREATE TABLE plan_revision_events (
    id TEXT PRIMARY KEY,                    -- ULID
    revision_id TEXT NOT NULL,              -- FK plan_revisions
    import_id TEXT,                         -- optional FK import_journal
    event_type TEXT NOT NULL
        CHECK (event_type IN ('created', 'made_current', 'superseded', 'file_linked', 'manual_override')),
    event_at TEXT NOT NULL,                 -- UTC
    event_by TEXT,
    note TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE INDEX idx_plan_revision_events_revision
ON plan_revision_events(revision_id, event_at);
```

#### 6.7.5 plan_context_links (NEU)

Cross-Modul-Verknüpfung von z.B. Bautagebuch-Einträgen / Fotos / Vorlagen zu einer konkreten Plan-Revision.

```sql
CREATE TABLE plan_context_links (
    id TEXT PRIMARY KEY,                    -- ULID
    source_module TEXT NOT NULL,            -- z.B. "bautagebuch", "foto", "rfi", "vorlage"
    source_id TEXT NOT NULL,                -- ID im Source-Modul
    target_document_id TEXT NOT NULL,       -- FK plan_documents
    target_revision_id TEXT,                -- FK plan_revisions — PFLICHT bei fixed_revision
    resolution_mode TEXT NOT NULL
        CHECK (resolution_mode IN ('fixed_revision')),  -- erweiterbar in Zukunft, derzeit nur ein Wert
    context_time TEXT NOT NULL,             -- UTC, zu welchem Zeitpunkt der Link erstellt wurde (Berichtsdatum)
    link_type TEXT NOT NULL
        CHECK (link_type IN ('auto_reference', 'manual_reference', 'attachment')),
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (target_document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (target_revision_id) REFERENCES plan_revisions(id)
);

CREATE INDEX idx_plan_context_links_source
ON plan_context_links(source_module, source_id, is_deleted);

CREATE INDEX idx_plan_context_links_target
ON plan_context_links(target_document_id, target_revision_id, is_deleted);
```

**Pflicht: `resolution_mode = 'fixed_revision'`** (ADR-058, fachliche Invariante). Beim Erzeugen eines Links wird die zu diesem Zeitpunkt aktuelle Revision festgezogen → alte Bautagesberichte zeigen immer dieselbe Revision, auch nach späterer Korrektur eines Importdatums.

#### 6.7.6 building_part_aliases — verschoben nach `bpm.db` (Kap. 4.11)

Diese Tabelle lebt **nicht** in `planmanager.db`. Per ADR-058-Addendum (CGR r3) liegt das Auto-Learn-Mapping zentral in `bpm.db` — dort mit **hartem FK** auf `building_parts(id)` (gleiche Datei), `project_id` + Sync-Feldern. **Definition siehe Kap. 4.11.**

Begründung: zentral verfügbar, gesynct, FK erzwingbar; reduziert die Cross-DB-Soft-References auf 3. Analog später möglich: `building_level_aliases` — nicht Teil des Foundation Slice (YAGNI).

#### 6.7.7 Beispiel-Query: Zeitreise für Bautagebuch

„Welche Polierpläne waren am Berichtstag (`2025-06-15 09:00:00Z`) für Haus H1, Geschoss EG aktuell?"

```sql
SELECT
    pd.id AS document_id,
    pd.document_key,
    pd.plan_number,
    pd.document_type,
    pd.title,
    pr.id AS revision_id,
    pr.plan_index,
    pf.relative_path
FROM plan_documents pd
JOIN plan_revisions pr ON pr.document_id = pd.id AND pr.is_deleted = 0
JOIN revision_file_links rfl ON rfl.revision_id = pr.id AND rfl.is_primary = 1
JOIN plan_files pf ON pf.id = rfl.file_id AND pf.is_deleted = 0
WHERE pd.project_id = :project_id
  AND pd.building_part_id = :h1_id
  AND pd.building_level_id = :eg_id
  AND pd.document_type_id IN (:polierplan_id, :schalung_id, :bewehrung_id)
  AND pd.is_deleted = 0
  AND pr.current_from <= '2025-06-15T09:00:00Z'
  AND (pr.superseded_at IS NULL OR pr.superseded_at > '2025-06-15T09:00:00Z')
ORDER BY pd.document_type, pd.plan_number;
```

Beim Speichern des Bautagesberichts wird je gefundenem Treffer ein Eintrag in `plan_context_links` mit `resolution_mode = 'fixed_revision'` geschrieben — siehe ADR-058.

#### 6.7.8 Foundation-Slice-Umfang vs. Post-V1

**V1-Sperrposten (Foundation Slice — BPM-109.01–.04 + .05a):**
- Schema v2.0 wie oben definiert
- Domain Models + Repository
- Pipeline schreibt korrekt in alle neuen Tabellen
- Revision-Zeitlogik (current_from / superseded_at + Events)
- `IPlanLookupService` Interface-Stub (nur Vertrag, keine Implementation)

**Post-V1 (BPM-109.05/.06/.07):**
- `IPlanLookupService` Implementation (Query-Logik) — parallel zu BPM-056
- Stammdaten-Mapping-UI mit Preview (Auto-Learn-Bestätigung)
- `plan_context_links` aktiv nutzen (kommt mit BPM-056)
- `building_part_aliases` UI
- Vollständige Doku/GLOSSAR/BACKLOG/Architektur-Update

---

## 7. Schema-Migration

### 7.1 Migrationshistorie

| Version | Datum | Änderung |
|---------|-------|---------|
| 1.0 | März 2026 | clients, projects, buildings, schema_version |
| 1.1 | März 2026 | project_type Spalte zu projects |
| 1.2 | März 2026 | building_parts, building_levels |
| 1.3 | März 2026 | (reserviert) |
| 1.4 | März 2026 | project_participants |
| 1.5 | März 2026 | project_links |
| 2.0 | April 2026 | ULID-Migration: seq entfernt, id TEXT PRIMARY KEY, created_at/updated_at, FK-Indizes, IIdGenerator |
| 2.1 | April 2026 | Sync-Spalten: updated_at→last_modified_at, +created_by, +last_modified_by, +sync_version, +is_deleted, UTC-Timestamps (ADR-050) |
| *2.1* | *geplant* | *employees, time_entries* |
| *2.2* | *geplant* | *work_packages, work_assignments* |
| *2.3* | *geplant* | *lv_positions, performance_catalog, project_difficulty* |
| *2.4* | *geplant* | *diary_days, diary_notes (ADR-047)* |
| *2.5* | *geplant* | *contacts, material_orders, buildings-Tabelle entfernen* |
| *2.6* | *geplant* | *external_call_log (Audit-Log, ADR-035)* |
| *2.7* | *geplant* | *project_shares (Multi-User Phase 2, ADR-038)* |
| *3.0* | *geplant* | *users, user_roles, project_memberships (Multi-User Phase 3)* |

### 7.2 Migrationsregeln

- Schema-Version wird bei App-Start geprüft und automatisch migriert
- Neue Spalten: `ALTER TABLE ... ADD COLUMN` mit DEFAULT-Wert
- Neue Tabellen: `CREATE TABLE IF NOT EXISTS`
- Tabellen löschen: Erst wenn sicher ist dass keine Daten mehr darin stecken
- Rückwärtskompatibel: Ältere App-Versionen ignorieren neue Tabellen/Spalten
- Backup vor Migration: `bpm.db` → `bpm.db.bak` kopieren (ADR-040)
- Harte Abbruchbedingung: DB-Version neuer als App-Version → App startet nicht

### 7.3 ULID-Migration (v1.5 → v2.0)

Die Migration von `seq + id TEXT` auf `id TEXT PRIMARY KEY` (ULID) erfordert für jede bestehende Tabelle:

1. Neue Tabelle mit ULID-Schema erstellen (`_new` Suffix)
2. Daten kopieren (bestehende TEXT-IDs durch neue ULIDs ersetzen)
3. FK-Referenzen in Kindtabellen aktualisieren
4. Alte Tabelle droppen, neue umbenennen
5. Indizes neu erstellen

Da nur wenige Testdaten vorhanden sind, kann alternativ die DB gelöscht und neu erstellt werden.

---

## 8. Datenfluss zwischen Modulen

```
EINSTELLUNGEN (Stammdaten)
│ projects, clients, building_parts, building_levels,
│ project_participants, project_links
│
├──→ KALKULATION
│    │ work_packages, lv_positions, project_difficulty
│    │
│    ├──→ ARBEITSEINTEILUNG (täglich)
│    │    │ work_assignments
│    │    │
│    │    └──→ BAUTAGEBUCH (täglich, auto-befüllt)
│    │         │ diary_days + diary_notes
│    │         │
│    │         └──→ DASHBOARD (Übersicht)
│    │
│    ├──→ NACHKALKULATION (bei Fertigmeldung)
│    │    │ performance_catalog
│    │    │
│    │    └──→ BAUZEITPROGNOSE
│    │
│    └──→ TASK-MANAGEMENT
│         │ material_orders → ClickUp/Asana/Trello
│
├──→ ZEITERFASSUNG
│    │ employees, time_entries
│    │
│    └──→ fließt in: Arbeitseinteilung, Bautagebuch, Nachkalkulation
│
└──→ PLANMANAGER (eigene DB pro Projekt)
     │ planmanager.db (Journal, Undo)
```

---

## 9. Naming-Konventionen

| Konvention | Regel | Beispiel |
|-----------|-------|---------|
| Tabellennamen | snake_case, Plural | `building_parts`, `time_entries` |
| Spaltennamen | snake_case | `project_id`, `short_name`, `created_at` |
| ID-Spalten | `id` (PK) oder `<tabelle>_id` (FK) | `id`, `project_id`, `building_part_id` |
| ID-Typ | TEXT (ULID, 26 Zeichen) | `01HV8M2Q9AJ3W1XK7R4F5N6T8C` |
| Boolean | INTEGER (0/1) | `active`, `confirmed` |
| Datum | TEXT im Format YYYY-MM-DD | `project_start`, `date` |
| Zeitstempel | TEXT im Format datetime('now') | `created_at`, `updated_at` |
| Enums | TEXT mit definierten Werten | `status`: "Active" \| "Completed" |
| NULL | Nur wenn Wert optional ist | `actual_end`, `rduk`, `absence_type` |
| DEFAULT '' | Für Pflicht-Textfelder die leer sein dürfen | `company`, `notes` |

### 9.1 ID-Schema (ADR-039 v2)

Jede Tabelle hat genau eine ID-Spalte:

| Spalte | Typ | Rolle |
|--------|-----|-------|
| `id` | TEXT PRIMARY KEY | ULID — global eindeutig, offline erzeugbar, chronologisch sortierbar |

**Verbindliche Regeln:**
- **ULID für ALLE Tabellen** — `bpm.db` und `planmanager.db`, ohne Ausnahmen
- Alle Fremdschlüssel referenzieren die `id`-Spalte der Zieltabelle
- FK-Spalten sind immer `TEXT`
- ID-Generierung ausschließlich über `IIdGenerator.NewId()`
- Keine `seq` Spalte, kein `INTEGER PRIMARY KEY`, keine Präfix-IDs
- `created_at` und `updated_at` auf jede Tabelle (Pflicht)

### 9.3 Sync-Felder-Konvention (ADR-050, ab v0.24.3)

Jede **neue** fachliche Tabelle bekommt folgende Pflicht-Spalten:

```sql
id                  TEXT PRIMARY KEY,  -- ULID, clientseitig via IIdGenerator
created_at          TEXT NOT NULL,     -- UTC (DateTime.UtcNow)
created_by          TEXT,              -- Modus A: settings.localUserName, Modus C: JWT-Claim
last_modified_at    TEXT NOT NULL,     -- UTC
last_modified_by    TEXT,
sync_version        INTEGER NOT NULL DEFAULT 0,
is_deleted          INTEGER NOT NULL DEFAULT 0
```

**Regeln:**
- Zeitstempel immer UTC — nie `DateTime.Now`, nie lokale Zeitzone
- Soft Delete: `is_deleted = 1` statt `DELETE FROM`
- `sync_version` wird bei jeder Mutation hochgezählt
- Bestehende Tabellen werden bei nächster Migration nachgerüstet
- Identity-Tabellen (ASP.NET, nur Server) folgen eigenen Konventionen

### 9.2 Lesbarkeit ohne Präfix-IDs

ULIDs sind nicht menschenlesbar. Die Lesbarkeit wird über fachliche Felder sichergestellt:

| Entität | Lesbare Identifikation |
|---------|----------------------|
| Projekt | `project_number` + `name` |
| Bauteil | `short_name` + `description` |
| Geschoss | `name` |
| Beteiligter | `role` + `company` |
| Arbeitspaket | `activity` + Bauteil + Geschoss |
| In Logs | Fachlicher Kontext + ULID-Kurzform |

---

## 10. JSON-Konfigurationsdateien (kein SQLite)

> **Hinweis (BPM-104, ADR-055):** Diese Liste ist die historische Doku-Sicht. Die **vollständige Persistenz-Übersicht zur Laufzeit** liefert `IPersistenceRegistry` (Domain) + Filesystem-Scan im DevTools-Reset-Tab. Auch SQLite-DBs (`bpm.db`, `planmanager.db`) und Logs (`BPM_*.log`) sind dort gelistet — die Tabelle hier zeigt nur die JSON-Konfig-Dateien.

### 10.1 Übersicht aller JSON-Konfig-Dateien

| Datei | Speicherort | Synct? | Beschreibung | Schreiber |
|-------|------------|--------|-------------|-----------|
| `device-settings.json` | Lokal `%LocalAppData%\BauProjektManager\` | **Nein** | Geräte-spezifische Einstellungen (Pfade, DeviceId, MachineName) — ADR-052 | `AppSettingsService` |
| `shared-config.json` | Cloud `<basePath>/.AppData/BauProjektManager/` | **Ja** | Geteilte Konfiguration (FolderTemplate, Listen, Rollen) — ADR-052 | `AppSettingsService` |
| `settings.json` *(legacy)* | Lokal `%LocalAppData%\BauProjektManager\` | Nein | **Veraltet** — wird beim ersten Start nach Update automatisch in `device-settings.json` + `shared-config.json` migriert | `AppSettingsService` (nur Migration-Lesen) |
| `registry.json` | Cloud `<basePath>/.AppData/BauProjektManager/` | Ja | Generierter VBA-Export (read-only für VBA) | `RegistryJsonExporter` |
| `pattern-templates.json` | Cloud `<basePath>/.AppData/BauProjektManager/` | Ja | Globale Musterbibliothek für Plan-Profile | PlanManager |
| `.bpm/manifest.json` | Cloud Projektordner `.bpm/` (ADR-046) | Ja | Schlanker Projekt-Ausweis (`ProjectManifest`, SchemaVersion 2) | `ManifestService` |
| `.bpm/project.json` | Cloud Projektordner `.bpm/` (ADR-046) | Ja | Vollständiger Projektexport | `ProjectExportService` |
| `.bpm/profiles/*.json` | Cloud Projektordner `.bpm/profiles/` (ADR-046) | Ja | RecognitionProfile pro Dokumenttyp pro Projekt | PlanManager |

> **Status `settings.json` Split:** ✅ Implementiert ab v0.25.x (ADR-052). Der `AppSettingsService` lädt zuerst `device-settings.json`, im Fehlerfall einer Migration aus `settings.json` (legacy). `shared-config.json` wird beim ersten Bind eines Workspaces erstellt (oder aus Legacy migriert).

### 10.2 device-settings.json — Feldschema

**Klasse:** `BauProjektManager.Domain.Models.DeviceSettings` 
**Speicherort:** `%LocalAppData%\BauProjektManager\device-settings.json` 
**Sync:** Nein (gerätespezifisch) 
**JSON-Naming:** camelCase

| Feld | Typ | Default | Pflicht | Zweck |
|------|-----|---------|---------|-------|
| `schemaVersion` | string | `"1.1"` | ja | Migrations-Anker für Schema-Änderungen |
| `deviceId` | string | (Guid 12 Zeichen, einmalig generiert) | ja (auto) | Stabile Geräte-ID. Wird beim Erststart einmalig generiert und nie geändert. Identifiziert das Gerät im Multi-Device-Betrieb. |
| `machineName` | string | `Environment.MachineName` | ja (auto) | Aktueller Windows-Computername. Wird bei jedem Laden überschrieben. |
| `workspaceId` | string | `""` | nein | WorkspaceId des zuletzt gebundenen `shared-config.json`. Ermöglicht Erkennung ob sich der Datenbestand geändert hat (Rebind). Wird beim ersten Bind gesetzt. |
| `cloudStoragePath` | string | `""` | ja (Setup) | Pfad zum Cloud-Speicher-Root (z.B. OneDrive, Dropbox, Google Drive). Cloud-neutral — kein bestimmter Anbieter vorausgesetzt. |
| `basePath` | string | `""` | ja (Setup) | Stammverzeichnis für alle Projektordner (z.B. `D:\OneDrive\Projekte\`). |
| `archivePath` | string | `""` | nein | Stammverzeichnis für archivierte Projekte. Optional. |
| `exportPath` | string | `""` | nein | Default-Zielordner für Exporte. Optional. |
| `isFirstRun` | bool | `true` | ja (auto) | Wird nach erfolgreichem Erst-Setup auf `false` gesetzt. Steuert ob der Ersteinrichtungs-Dialog erscheint. |
| `setupCompletedAt` | DateTime? | `null` | nein | UTC-Zeitstempel des abgeschlossenen Erst-Setups. |

### 10.3 shared-config.json — Feldschema

**Klasse:** `BauProjektManager.Domain.Models.SharedConfig` 
**Speicherort:** `<basePath>/.AppData/BauProjektManager/shared-config.json` 
**Sync:** Ja (über Cloud-Speicher) 
**JSON-Naming:** camelCase

| Feld | Typ | Default | Pflicht | Zweck |
|------|-----|---------|---------|-------|
| `schemaVersion` | string | `"1.1"` | ja | Migrations-Anker |
| `workspaceId` | string | (Guid 12 Zeichen, einmalig generiert) | ja (auto) | Stabile Workspace-ID. Identifiziert den gemeinsamen Datenbestand. Wird beim ersten `SaveShared()` generiert. |
| `revision` | int | `0` | ja (auto) | Revisionsnummer. Wird bei jedem `SaveShared()` inkrementiert. Basis für Optimistic Concurrency bei Multi-Device-Zugriff. |
| `updatedAtUtc` | DateTime? | `null` | nein (auto) | UTC-Zeitstempel der letzten Änderung. |
| `updatedByDeviceId` | string | `""` | nein (auto) | DeviceId des Geräts das zuletzt geschrieben hat. |
| `folderTemplate` | List&lt;FolderTemplateEntry&gt; | siehe 10.5 | ja | Ordner-Template für neue Projekte. Reihenfolge bestimmt die Nummerierung (00, 01, 02...). |
| `projectTypes` | List&lt;string&gt; | siehe 10.6 | ja | Editierbare Liste der Projektarten (Dropdown im Projekt-Dialog). |
| `buildingTypes` | List&lt;string&gt; | siehe 10.6 | ja | Editierbare Liste der Bauwerkstypen (Dropdown pro Bauteil). |
| `levelNames` | List&lt;LevelNameEntry&gt; | siehe 10.6 | ja | Editierbare Geschoss-Bezeichnungen: Kurz (EG) + Lang (Erdgeschoss). |
| `participantRoles` | List&lt;string&gt; | siehe 10.6 | ja | Editierbare Rollen-Liste für Projekt-Beteiligte. |
| `portalTypes` | List&lt;string&gt; | siehe 10.6 | ja | Editierbare Liste der Bauherren-Portal-Typen. |

### 10.4 settings.json (legacy) — Feldschema

**Klasse:** `BauProjektManager.Domain.Models.AppSettings` 
**Status:** ⚠️ **Veraltet ab v0.25.x.** Wird nur noch von der Migration in `AppSettingsService.MigrateFromLegacy()` gelesen. Neuer Code verwendet `DeviceSettings` + `SharedConfig`. 
**Speicherort:** `%LocalAppData%\BauProjektManager\settings.json` *(falls vorhanden — wird nach Migration nicht mehr beschrieben)*

Felder die aus `AppSettings` migrieren:

| Feld | Wandert nach | Anmerkung |
|------|--------------|-----------|
| `machineName` | `device-settings.json` → `machineName` | unverändert |
| `oneDrivePath` | `device-settings.json` → `cloudStoragePath` | umbenannt (cloud-neutral) |
| `basePath` | `device-settings.json` → `basePath` | unverändert |
| `archivePath` | `device-settings.json` → `archivePath` | unverändert |
| `exportPath` | `device-settings.json` → `exportPath` | unverändert |
| `isFirstRun` | `device-settings.json` → `isFirstRun` | unverändert |
| `setupCompletedAt` | `device-settings.json` → `setupCompletedAt` | unverändert |
| `localUserId` | (entfernt) | Wandert in `IUserContext` (ADR-052), nicht mehr in JSON |
| `localUserName` | (entfernt) | Wandert in `IUserContext` (ADR-052), nicht mehr in JSON |
| `folderTemplate` | `shared-config.json` → `folderTemplate` | unverändert |
| `projectTypes` | `shared-config.json` → `projectTypes` | unverändert |
| `buildingTypes` | `shared-config.json` → `buildingTypes` | unverändert |
| `levelNames` | `shared-config.json` → `levelNames` | unverändert |
| `participantRoles` | `shared-config.json` → `participantRoles` | unverändert |
| `portalTypes` | `shared-config.json` → `portalTypes` | unverändert |

### 10.5 Hilfsklassen für strukturierte Listen

**`LevelNameEntry`** — Geschoss-Bezeichnung mit Kurz- und Langform.

| Feld | Typ | Beispiel |
|------|-----|----------|
| `shortName` | string | `"EG"` |
| `longName` | string | `"Erdgeschoss"` |

**`FolderTemplateEntry`** — Hauptordner im Ordner-Template. Die Nummer wird NICHT gespeichert — sie entsteht aus der Position in der Liste (`{Position:D2} {Name}` z.B. `02 Fotos`).

| Feld | Typ | Default | Beispiel |
|------|-----|---------|----------|
| `name` | string | `""` | `"Planunterlagen"` |
| `hasInbox` | bool | `false` | `true` für Ordner mit `_Eingang/`-Unterordner (PlanManager-Import) |
| `subFolders` | List&lt;SubFolderEntry&gt; | `[]` | Optionale Unterordner |

**`SubFolderEntry`** — Unterordner innerhalb eines Hauptordners. Rekursiv (kann selbst weitere `SubFolders` haben).

| Feld | Typ | Default | Beispiel |
|------|-----|---------|----------|
| `name` | string | `""` | `"Polierpläne"` |
| `hasPrefix` | bool | `true` | `true` → `01 Polierpläne` (mit Nummer), `false` → `Baustelleneinrichtung` (ohne Nummer) |
| `subFolders` | List&lt;SubFolderEntry&gt; | `[]` | Verschachtelte Unterordner |

### 10.6 Default-Werte für SharedConfig

Definiert in `BauProjektManager.Domain.Models.SharedConfigDefaults`. Bei Reset oder Erstinitialisierung werden diese Listen verwendet.

**`projectTypes` (6 Defaults):**
`Neubau`, `Sanierung`, `Umbau`, `Zubau`, `Abbruch`, `Sonstiges`

**`buildingTypes` (7 Defaults):**
`EFH`, `MFH`, `Wohnanlage`, `Gewerbe`, `Industrie`, `Infrastruktur`, `Sonstiges`

**`participantRoles` (13 Defaults):**
`Bauherr`, `Architekt`, `Statiker`, `Haustechnik`, `Bauphysik`, `ÖBA`, `Vermessung`, `Elektro`, `HKLS`, `Bodengutachter`, `Brandschutz`, `Geotechnik`, `Sonstiges`

**`portalTypes` (5 Defaults):**
`InfoRaum`, `PlanRadar`, `PlanFred`, `Bau-Master`, `Dalux`

**`levelNames` (11 Defaults):**

| ShortName | LongName |
|-----------|----------|
| `FU` | Fundament |
| `UG3` | 3. Untergeschoss |
| `UG2` | 2. Untergeschoss |
| `UG` | Untergeschoss |
| `EG` | Erdgeschoss |
| `OG1` | 1. Obergeschoss |
| `OG2` | 2. Obergeschoss |
| `OG3` | 3. Obergeschoss |
| `OG4` | 4. Obergeschoss |
| `OG5` | 5. Obergeschoss |
| `DG` | Dachgeschoss |

**`folderTemplate` (7 Hauptordner):**

| Pos | Name | HasInbox | Unterordner |
|-----|------|----------|-------------|
| 00 | `Sonstiges` | nein | — |
| 01 | `Planunterlagen` | **ja** | `Ausschreibungspläne` (mit Präfix), `Polierpläne` (mit Präfix), `Statikpläne - Schalung` (mit Präfix), `Statikpläne - Bewehrung` (mit Präfix), `Fertigteilpläne` (mit Präfix), `Baustelleneinrichtung` (ohne Präfix) |
| 02 | `Fotos` | nein | — |
| 03 | `Leica` | nein | `Absteckpläne` (ohne Präfix), `Aufmaß` (ohne Präfix) |
| 04 | `DOKA` | nein | — |
| 05 | `LV` | nein | — |
| 06 | `Protokolle` | nein | — |

### 10.7 Migration von settings.json (legacy) zu Split-Format

`AppSettingsService.LoadDevice()` führt die Migration automatisch beim ersten Start nach Update durch:

1. Wenn `device-settings.json` **nicht** existiert UND `settings.json` (legacy) **existiert** → Migration triggern
2. Aus Legacy-`AppSettings` werden Felder in `DeviceSettings` und `SharedConfig` aufgeteilt (siehe 10.4)
3. `device-settings.json` wird sofort geschrieben
4. `shared-config.json` wird beim ersten `LoadShared()` geschrieben (sobald `basePath` bekannt ist)
5. Legacy `settings.json` bleibt liegen, wird aber nicht mehr beschrieben

**Schreib-Strategie:** Beide Dateien werden atomisch geschrieben (Write-to-Temp → File.Move overwrite). Bei `shared-config.json` wird vor jedem Schreiben `revision++` und `updatedAtUtc = DateTime.UtcNow` gesetzt.

### 10.8 Geplante Schema-Erweiterungen (gemäß ADR-053)

> **Hinweis:** Die frühere "Neue Datenarchitektur" mit 12 Sync-Spalten + Outbox/Inbox-Tabellen aus DatenarchitekturSync.md ist durch **ADR-053** (2026-04-30) **superseded**. Stattdessen: 7-Spalten-Sync-Modell (bereits implementiert in v0.25.23, ADR-050) + Pull/Push-Sync mit Server-Authority.

#### Aktuell implementiert (v0.25.x)

✅ **7 Sync-Metadaten-Spalten** auf allen Shared-Tabellen (ADR-050, v0.25.23):
- `created_by`, `last_modified_at`, `last_modified_by`, `sync_version`, `is_deleted` (+ implizit `created_at`, `updated_at` als Vorgänger)
- Alle Timestamps UTC (ISO 8601), `sync_version` inkrementiert bei jedem Update

✅ **settings.json Split:** `device-settings.json` (lokal) + `shared-config.json` (Cloud)

✅ **IUserContext + LocalUserContext** (ADR-052, v0.25.22) für `created_by`/`last_modified_by`

#### Geplante Erweiterungen für Server-Sync (Phase 0/1, post Spike 0)

**Neue Server-Tabelle (PostgreSQL):**
- `server_change_log` — monotone server_version pro Mutation, BIGSERIAL PK, scope-fähig (global vs project:id)

**Neue Spalten auf bestehenden Server-Tabellen:**
- `server_version BIGINT NOT NULL` — pro synchronisierter Tabelle
- `server_modified_at TEXT NOT NULL`
- `server_modified_by_user_id TEXT NOT NULL`

**Neue lokale Tabellen (SQLite-Client):**
- `sync_state_local` — pro Entity nur wenn `pending|rejected|conflict` (kein "synced"-Eintrag, "no row" = clean)
- `sync_checkpoints` — pro Tabelle/Scope `highest_server_version`, `last_pull_at`, `last_successful_push_at`
- `sync_history` — Audit-Log mit Retention (30 Tage / 1000 Einträge)

**Neue ASP.NET Identity-Tabellen (Server, post Spike 2):**
- ASP.NET Core Identity Standard: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`
- Plus: `project_memberships` (project_id, user_id, project_role) für Projekt-Zuordnung

**Schema-Änderungen für Profile (post Spike 0):**
- Neue Tabelle `recognition_profiles` (id, project_id FK, name, document_type, profile_json, + 7 Sync-Spalten)
- `.bpm/profiles/*.json` wird zu Export/Backup, nicht mehr SoR im Servermodus
- JSON-Schema fuer `profile_json`: siehe [PlanManager.md §14.1](../Module/PlanManager.md#141-recognitionprofile-json-schema-v3--bpm-082-2026-05). Aktuell **SchemaVersion 3** (BPM-082): `recognition[].method` = `"segment"` (Default) oder `"regex"` (Fallback); `segment`-Rules tragen `segmentPosition: int?`. Validierung via `ProfileManager.IsProfileLoadable` (ADR-010 erweitert).

#### Verworfene Tabellen (durch ADR-053)

❌ Folgende Tabellen aus DatenarchitekturSync.md werden **nicht implementiert**:
- `change_log`, `sync_outbox`, `sync_applied_events`, `sync_conflicts` (waren für FolderSync/Outbox-Pattern)
- `users`, `user_devices`, `roles`, `user_roles` als eigene Tabellen (durch ASP.NET Identity ersetzt)
- `diary_days` + `diary_notes` als Konflikt-Vermeidungs-Aggregate (Server-Authority macht das überflüssig)

❌ **12-Sync-Spalten-Modell** ist überholt — 7 Spalten reichen (ADR-050 + ADR-053)

**Cross-Review-Quelle:** [CGR-2026-04-30-datenarchitektur-sync](../Referenz/chatgpt-reviews/CGR-2026-04-30-datenarchitektur-sync/) (7 Runden mit ChatGPT GPT-5.4)

**Datenklassifizierung:** Siehe [DSVGO-Architektur.md](DSVGO-Architektur.md) + [ADR-047](../Referenz/ADR.md) (4-Klassen-Modell bleibt gültig: A local-only, B shared domain, C shared reference, D restricted)

---

*Dieses Dokument wird bei jeder Schema-Änderung aktualisiert. Es ist die einzige Quelle der Wahrheit für die Datenbankstruktur.*