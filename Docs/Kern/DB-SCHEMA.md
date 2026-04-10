# BauProjektManager — Datenbank-Schema

**Version:** 2.0 (ULID-Migration geplant)  
**Datum:** 04.04.2026  
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
- **SQLite als System of Record:** JSON-Dateien (registry.json, settings.json) sind generierte Exporte oder Konfiguration (ADR-002)
- **Schema-Migration:** Versioniert, automatisch bei App-Start, rückwärtskompatibel (ADR-040)

### 1.2 Datenbank-Dateien

| DB | Speicherort | Inhalt | Synct? |
|----|------------|--------|--------|
| `bpm.db` | `%LocalAppData%\BauProjektManager\` | Alle Stamm- und Projektdaten | Nein (Event-Sync über ADR-037) |
| `planmanager.db` | `%LocalAppData%\...\Projects\<ProjektID>\` | Plan-Cache, Import-Journal, Undo | Nein (Event-Sync über ADR-037) |

---

## 2. Beziehungsdiagramm

### 2.1 Implementiert (v1.5 → Migration auf v2.0 ULID ausstehend)

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
| diary_entries | projects | project_id | CASCADE | ⬜ Geplant |
| material_orders | work_packages | work_package_id | — | ⬜ Geplant |
| project_participants | contacts | contact_id | — | ⬜ Vorbereitet (FK leer) |

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
| diary_entries | Bautagebuch | Dashboard, Export | ⬜ |
| contacts | Adressbuch | Einstellungen (Tab 3) | ⬜ |
| material_orders | Task-Management | Dashboard | ⬜ |
| external_call_log | Infrastructure (ExternalCommunicationService) | Einstellungen (Datenschutz-Tab) | ⬜ |

---

## 4. Tabellen-Schema (Ziel: v2.0 ULID)

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
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
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
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
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
    description TEXT NOT NULL DEFAULT '',
    building_type TEXT NOT NULL DEFAULT '',
    zero_level_absolute REAL NOT NULL DEFAULT 0,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
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
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE
);

CREATE INDEX idx_building_levels_part_id ON building_levels(building_part_id);
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
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
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
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
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

---

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

### 5.8 diary_entries (Bautagebuch)

```sql
CREATE TABLE diary_entries (
    id TEXT PRIMARY KEY,                   -- ULID
    project_id TEXT NOT NULL,
    date TEXT NOT NULL,
    weather TEXT,
    temperature_min REAL,
    temperature_max REAL,
    personnel_count INTEGER,
    absent_count INTEGER,
    activities_summary TEXT,
    remarks TEXT,
    confirmed INTEGER DEFAULT 0,
    confirmed_at TEXT,
    photos TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_diary_project_date ON diary_entries(project_id, date);
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

## 6. PlanManager-Datenbank (separat)

Pro Projekt eine eigene SQLite-DB. Liegt in `%LocalAppData%\BauProjektManager\Projects\<ProjektID>\planmanager.db`.

### 6.1 import_journal

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

### 6.2 import_actions

```sql
CREATE TABLE import_actions (
    id TEXT PRIMARY KEY,                   -- ULID
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,
    action_status TEXT NOT NULL,
    started_at TEXT,
    completed_at TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    destination_path TEXT NOT NULL,
    archive_path TEXT,
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE INDEX idx_actions_import ON import_actions(import_id);
```

### 6.3 import_action_files

```sql
CREATE TABLE import_action_files (
    id TEXT PRIMARY KEY,                   -- ULID
    action_id TEXT NOT NULL,
    file_name TEXT NOT NULL,
    file_type TEXT NOT NULL,
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    md5_hash TEXT,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);

CREATE INDEX idx_action_files_action ON import_action_files(action_id);
```

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
| *2.0* | *April 2026* | *ULID-Migration: seq entfernen, id TEXT PRIMARY KEY, created_at/updated_at ergänzen, Indizes* |
| *2.1* | *geplant* | *employees, time_entries* |
| *2.2* | *geplant* | *work_packages, work_assignments* |
| *2.3* | *geplant* | *lv_positions, performance_catalog, project_difficulty* |
| *2.4* | *geplant* | *diary_entries* |
| *2.5* | *geplant* | *contacts, material_orders, buildings-Tabelle entfernen* |
| *2.6* | *geplant* | *external_call_log (Audit-Log, ADR-035)* |
| *2.7* | *geplant* | *project_shares (Multi-User Phase 2, ADR-038)* |
| *3.0* | *geplant* | *users, roles, project_user_role (Multi-User Phase 3)* |

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
│    │         │ diary_entries
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

| Datei | Speicherort | Beschreibung | Geschrieben von |
|-------|------------|-------------|----------------|
| `settings.json` | Cloud .AppData/ | App-Einstellungen, Pfade, Listen | AppSettingsService |
| `registry.json` | Cloud .AppData/ | Generierter VBA-Export (read-only für VBA) | RegistryJsonExporter |
| `profiles.json` | Cloud .AppData/Projects/<P>/ | Plantyp-Profile pro Projekt | PlanManager |
| `pattern-templates.json` | Cloud .AppData/ | Globale Musterbibliothek | PlanManager |
| `.bpm-manifest` | Cloud Projektordner | Versteckter Projekt-Ausweis | ProjectFolderService |

### Neue Datenarchitektur (DatenarchitekturSync.md)

Folgende Tabellen und Konzepte sind im Cross-Review (10.04.2026) entschieden und werden bei der nächsten Schema-Migration umgesetzt:

**Neue Tabellen (bpm.db):**

| Tabelle | Zweck | Sync-Klasse |
|---------|-------|-------------|
| `users` | Benutzer-Identitäten | B (shared) |
| `user_devices` | Geräte pro Benutzer | B (shared) |
| `roles` | Fachliche Rollen (bauleiter, polier etc.) | C (reference) |
| `user_roles` | User ↔ Rolle Zuordnung | B (shared) |
| `project_memberships` | User ↔ Projekt Zugriff | B (shared) |
| `change_log` | Lokales Änderungsprotokoll | A (local-only) |
| `sync_outbox` | Ausstehende Sync-Events | A (local-only) |
| `sync_applied_events` | Verarbeitete Events | A (local-only) |
| `sync_conflicts` | Konflikte zur Auflösung | A (local-only) |
| `diary_days` | Bautagebuch Tageskopf (Wetter, Bestätigung) | B (shared) |
| `diary_notes` | Bautagebuch Notizen (viele pro Tag pro User) | B (shared) |
| `employee_compensation` | Lohnsätze, Überstundensätze | D (restricted) |
| `lv_pricing` | Einheitspreise | D (restricted) |
| `material_order_prices` | Einkaufspreise | D (restricted) |

**Schema-Änderungen an bestehenden Tabellen:**

Alle Shared-Tabellen (Klasse B+D) bekommen 12 Sync-Metadaten-Spalten: `created_by_user_id`, `updated_by_user_id`, `entity_version`, `is_deleted`, `deleted_at_utc`, `deleted_by_user_id`, `origin_device_id`, `last_change_id`. Soft Deletes statt Hard Deletes.

**Datenklassifizierung:** Siehe [DatenarchitekturSync.md](../Konzepte/DatenarchitekturSync.md) Kapitel 2.

**settings.json Split:** `device-settings.json` (lokal) + `shared-config.json` (Cloud). Siehe DatenarchitekturSync.md Kapitel 9.

---

*Dieses Dokument wird bei jeder Schema-Änderung aktualisiert. Es ist die einzige Quelle der Wahrheit für die Datenbankstruktur.*