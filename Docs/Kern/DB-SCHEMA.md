# BauProjektManager — Datenbank-Schema

**Version:** 1.5 (implementiert)  
**Datum:** 30.03.2026  
**DB-Engine:** SQLite  
**Speicherort:** `%LocalAppData%\BauProjektManager\bpm.db`

---

## 1. Überblick

Dieses Dokument ist die **zentrale Referenz** für alle Datenbanktabellen in BPM — bestehende und geplante. Jedes Modul-Konzept referenziert hierher statt eigene Schema-Entwürfe zu wiederholen.

### 1.1 Grundprinzipien

- **Eine Datenbank, viele Module:** Alle Module greifen auf `bpm.db` zu
- **PlanManager-Ausnahme:** Eigene `planmanager.db` pro Projekt (Cache, Journal, Undo)
- **ID-Schema:** Auto-Increment mit Präfix (`proj_001`, `client_001`, `bpart_001`). IDs werden nie wiederverwendet (ADR-006)
- **SQLite als System of Record:** JSON-Dateien (registry.json, settings.json) sind generierte Exporte oder Konfiguration (ADR-002)
- **Schema-Migration:** Versioniert, automatisch bei App-Start, rückwärtskompatibel

### 1.2 Datenbank-Dateien

| DB | Speicherort | Inhalt | Synct? |
|----|------------|--------|--------|
| `bpm.db` | `%LocalAppData%\BauProjektManager\` | Alle Stamm- und Projektdaten | Nein |
| `planmanager.db` | `%LocalAppData%\...\Projects\<ProjektID>\` | Plan-Cache, Import-Journal, Undo | Nein |

---

## 2. Beziehungsdiagramm

### 2.1 Implementiert (v1.5)

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

**FK-Regel (verbindlich, ADR-039):**
Alle Fremdschlüssel referenzieren die fachliche `id`-Spalte der Zieltabelle (`TEXT`). Die `seq`-Spalte darf niemals in Fremdschlüsseln, JSON-Dateien, Logs oder externen Schnittstellen verwendet werden.

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

## 4. Implementierte Tabellen (Schema v1.5)

### 4.1 clients

Auftraggeber/Bauherr. Aktuell 1:1 mit Projekt. Später zentrale Firmendatenbank (ADR-021).

```sql
CREATE TABLE clients (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "client_001"
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT ''
);
```

**ID-Präfix:** `client_`

### 4.2 projects

Kernentität. Jedes Bauprojekt ist eine Zeile.

```sql
CREATE TABLE projects (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "proj_001"
    project_number TEXT NOT NULL DEFAULT '',  -- YYYYMM (aus Startdatum)
    name TEXT NOT NULL DEFAULT '',        -- Kurzname "ÖWG-Dobl"
    full_name TEXT NOT NULL DEFAULT '',   -- Langname
    status TEXT NOT NULL DEFAULT 'Active', -- "Active" | "Completed" (ADR-025)
    project_type TEXT NOT NULL DEFAULT '', -- aus AppSettings.ProjectTypes
    client_id TEXT,                       -- FK → clients.id
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
    project_start TEXT,               -- YYYY-MM-DD
    construction_start TEXT,
    planned_end TEXT,
    actual_end TEXT,
    -- Pfade (relativ zu root_path, außer root_path selbst)
    root_path TEXT NOT NULL DEFAULT '',
    plans_path TEXT NOT NULL DEFAULT '',
    inbox_path TEXT NOT NULL DEFAULT '',
    photos_path TEXT NOT NULL DEFAULT '',
    documents_path TEXT NOT NULL DEFAULT '',
    protocols_path TEXT NOT NULL DEFAULT '',
    invoices_path TEXT NOT NULL DEFAULT '',
    -- Sonstiges
    tags TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (client_id) REFERENCES clients(id)
);
```

**ID-Präfix:** `proj_`

### 4.3 buildings (Legacy)

Altes Building-Modell. Ersetzt durch building_parts + building_levels seit v0.13.1. Wird bei nächstem Major-Update entfernt.

```sql
CREATE TABLE buildings (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "bldg_001"
    project_id TEXT NOT NULL,
    name TEXT NOT NULL DEFAULT '',
    short_name TEXT NOT NULL DEFAULT '',
    type TEXT NOT NULL DEFAULT '',
    levels TEXT NOT NULL DEFAULT '',      -- Komma-separiert
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

### 4.4 building_parts

Bauteile eines Projekts (z.B. "Haus 5", "Haus 6"). Seit v0.13.1.

```sql
CREATE TABLE building_parts (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "bpart_001"
    project_id TEXT NOT NULL,
    short_name TEXT NOT NULL DEFAULT '',   -- "H5"
    description TEXT NOT NULL DEFAULT '',  -- "Haus Nr. 5"
    building_type TEXT NOT NULL DEFAULT '',-- aus AppSettings.BuildingTypes
    zero_level_absolute REAL NOT NULL DEFAULT 0, -- ± 0,00 (m ü.A.)
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**ID-Präfix:** `bpart_`  
**Gelesen von:** Kalkulation (work_packages.building_part_id), Ziegelberechnung

### 4.5 building_levels

Geschoße eines Bauteils. Seit v0.13.1.

```sql
CREATE TABLE building_levels (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "blvl_001"
    building_part_id TEXT NOT NULL,
    prefix INTEGER NOT NULL DEFAULT 0,    -- Geschoß-Nr (0=EG, -1=KG, 1=1.OG)
    name TEXT NOT NULL DEFAULT '',         -- "EG", "1.OG"
    description TEXT NOT NULL DEFAULT '',
    rdok REAL NOT NULL DEFAULT 0,         -- Rohdeckenoberkante (m)
    fbok REAL NOT NULL DEFAULT 0,         -- Fertigfußbodenoberkante (m)
    rduk REAL,                            -- Rohdeckenunterkante (m)
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE
);
```

**ID-Präfix:** `blvl_`  
**Berechnete Werte (im Code):** Geschosshöhe, Rohbauhöhe, Deckenstärke, Fußbodenaufbau  
**Gelesen von:** Kalkulation (work_packages.level_id), Ziegelberechnung (Scharrenrechner)

### 4.6 project_participants

Beteiligte am Projekt. Seit v0.14.0.

```sql
CREATE TABLE project_participants (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "ppart_001"
    project_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT '',         -- aus AppSettings.ParticipantRoles
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    contact_id TEXT NOT NULL DEFAULT '',   -- FK → contacts.id (Zukunft, ADR-024)
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**ID-Präfix:** `ppart_`  
**Zukunft:** `contact_id` verknüpft mit zentralem Adressbuch

### 4.7 project_links

Portal-Links und eigene Links pro Projekt. Seit v0.15.0.

```sql
CREATE TABLE project_links (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "plink_001"
    project_id TEXT NOT NULL,
    name TEXT NOT NULL DEFAULT '',
    url TEXT NOT NULL DEFAULT '',
    link_type TEXT NOT NULL DEFAULT 'Custom', -- "Portal" | "Custom"
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**ID-Präfix:** `plink_`

### 4.8 schema_version

```sql
CREATE TABLE schema_version (
    version TEXT NOT NULL              -- "1.5"
);
```

---

## 5. Geplante Tabellen (nach V1)

### 5.1 employees (Zeiterfassung)

Mitarbeiter-Stammdaten. Basis für Zeiterfassung und Arbeitseinteilung.

```sql
CREATE TABLE employees (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "emp_001"
    name TEXT NOT NULL,                   -- "Biskup Dejan"
    short_name TEXT,                      -- "Biskup D."
    qualification TEXT,                   -- "Facharbeiter" | "Lehrling" | "Polier" | "Kranfahrer"
    hourly_rate REAL,                     -- Bruttomittellohn
    active INTEGER DEFAULT 1,            -- 1 = aktiv, 0 = ausgeschieden
    notes TEXT
);
```

**Konzept:** ModuleZeiterfassung.md, ModuleKalkulation.md  
**Gelesen von:** Arbeitseinteilung, Bautagebuch

### 5.2 time_entries (Zeiterfassung)

Tägliche Anwesenheits-/Stundenerfassung pro Mitarbeiter.

```sql
CREATE TABLE time_entries (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "te_001"
    date TEXT NOT NULL,                   -- "2025-11-05"
    employee_id TEXT NOT NULL,            -- FK → employees.id
    project_id TEXT NOT NULL,             -- FK → projects.id
    hours REAL NOT NULL,                  -- 8.0
    absence_type TEXT,                    -- NULL | "U" (Urlaub) | "K" (Krank) | "Kranfahrer" | "Sonstiges"
    notes TEXT,
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

**Konzept:** ModuleZeiterfassung.md  
**Gelesen von:** Arbeitseinteilung (Stunden pro Mann), Bautagebuch (Anwesenheit)

### 5.3 work_packages (Kalkulation) — ZENTRALE TABELLE

Arbeitspakete = Bauteil + Geschoß + Tätigkeit + Soll-Menge. Die zentrale Einheit auf die alle Leistungsdaten gebucht werden. Verbindet bestehende building_parts/levels mit neuen Modulen.

```sql
CREATE TABLE work_packages (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "wp_001"
    project_id TEXT NOT NULL,             -- FK → projects.id
    building_part_id TEXT,                -- FK → building_parts.id (H5)
    level_id TEXT,                        -- FK → building_levels.id (EG)
    activity TEXT NOT NULL,               -- "Mauerwerk 38er" | "Betonwand komplett"
    lv_position_id TEXT,                  -- FK → lv_positions.id (optional)
    planned_quantity REAL,                -- 198 (Soll-Menge)
    unit TEXT NOT NULL,                   -- "m²" | "m³" | "to" | "Stk" | "lfm"
    source TEXT,                          -- "Ziegelberechnung" | "DokaCad" | "LV" | "Manuell"
    track_separately INTEGER DEFAULT 0,   -- 1 = Einzelschritte (Decke), 0 = Gesamtpaket (Wand)
    color TEXT,                           -- Farbcode für Arbeitseinteilung
    sort_order INTEGER,
    -- Status (automatisch aktualisiert)
    status TEXT DEFAULT 'planned',        -- "planned" | "in_progress" | "completed"
    started_at TEXT,                       -- erster Tag mit Zuordnung
    completed_at TEXT,                     -- Fertig-Markierung
    -- Ist-Werte (automatisch berechnet)
    actual_hours REAL DEFAULT 0,          -- Summe Ah (aus work_assignments × time_entries)
    actual_quantity REAL,                 -- tatsächliche Menge (bei Fertigmeldung)
    performance_value REAL,               -- m²/Ah oder h/m² (berechnet)
    notes TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id),
    FOREIGN KEY (lv_position_id) REFERENCES lv_positions(id)
);

**ID-Präfix:** `wp_`
```

**Konzept:** ModuleKalkulation.md Kapitel 3  
**Warum zentral:** Arbeitseinteilung, Zeiterfassung, Bautagebuch und Nachkalkulation buchen alle auf Arbeitspakete

### 5.4 work_assignments (Kalkulation / Arbeitseinteilung)

Tägliche Zuordnung: welcher Mitarbeiter arbeitet an welchem Arbeitspaket.

```sql
CREATE TABLE work_assignments (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "wa_001"
    date TEXT NOT NULL,                   -- "2025-11-05"
    employee_id TEXT NOT NULL,            -- FK → employees.id
    work_package_id TEXT NOT NULL,        -- FK → work_packages.id
    hours REAL,                           -- aus time_entries (kann manuell überschrieben werden)
    notes TEXT,
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id) ON DELETE CASCADE
);
```

**Konzept:** ModuleKalkulation.md Kapitel 5  
**Herkunft:** Herberts Excel-Arbeitseinteilungsblatt (Matrix Arbeiter × Tätigkeiten)

### 5.5 lv_positions (Kalkulation / LV-Import)

Importierte LV-Positionen (aus Excel, ÖNORM A 2063 oder KI-API).

```sql
CREATE TABLE lv_positions (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "lv_001"
    project_id TEXT NOT NULL,             -- FK → projects.id
    position_number TEXT NOT NULL,        -- "02.03.01"
    short_text TEXT NOT NULL,             -- "Mauerwerk 38er Plan"
    quantity REAL,                        -- 850 (Gesamt-Soll aus LV)
    unit TEXT,                            -- "m²"
    unit_price REAL,                      -- optional (für Kostenvergleich)
    completed_quantity REAL DEFAULT 0,    -- Summe aus verknüpften Arbeitspaketen
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**Konzept:** ModuleKalkulation.md Kapitel 7  
**Import-Wege:** Excel (.xlsx), KI-API (ADR-027), ÖNORM A 2063 XML (optional)

### 5.6 performance_catalog (Kalkulation / Nachkalkulation)

Erfahrungswerte — wächst automatisch mit jedem abgeschlossenen Arbeitspaket.

```sql
CREATE TABLE performance_catalog (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "perf_001"
    activity TEXT NOT NULL,               -- "Mauerwerk 38er"
    unit TEXT NOT NULL,                   -- "m²"
    hours_per_unit REAL NOT NULL,         -- 0.62
    project_id TEXT,                      -- Quelle (welches Projekt)
    work_package_id TEXT,                 -- Quelle (welches Arbeitspaket)
    measured_at TEXT,                      -- Datum der Fertigmeldung
    quantity REAL,                        -- gemessene Menge
    total_hours REAL,                     -- Gesamt-Arbeitsstunden
    workers INTEGER,                      -- Anzahl Arbeitskräfte (Durchschnitt)
    notes TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

**Konzept:** ModuleKalkulation.md Kapitel 8  
**Befüllung:** Automatisch bei Fertigmeldung eines Arbeitspakets. Initial aus Kalkulation_v2.xlsx migrierbar.

### 5.7 project_difficulty (Kalkulation / Bauzeitprognose)

Erschwernisfaktoren pro Projekt für die Bauzeitprognose.

```sql
CREATE TABLE project_difficulty (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "pdiff_001"
    project_id TEXT NOT NULL,             -- FK → projects.id
    factor_name TEXT NOT NULL,            -- "Hanglage" | "Winterarbeit" | "Enge Zufahrt"
    factor_value REAL NOT NULL,           -- 1.15
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**Konzept:** ModuleKalkulation.md Kapitel 8.2

### 5.8 diary_entries (Bautagebuch)

Tägliche Bautagebuch-Einträge. Wird großteils automatisch aus Arbeitseinteilung + Zeiterfassung befüllt.

```sql
CREATE TABLE diary_entries (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "diary_001"
    project_id TEXT NOT NULL,             -- FK → projects.id
    date TEXT NOT NULL,                   -- "2025-11-05"
    weather TEXT,                         -- "sonnig, 12°C" (automatisch von API)
    temperature_min REAL,
    temperature_max REAL,
    personnel_count INTEGER,              -- Anzahl Anwesende (aus time_entries)
    absent_count INTEGER,                 -- Anzahl Abwesende
    activities_summary TEXT,              -- Auto-generiert aus work_assignments
    remarks TEXT,                         -- Polier-Bemerkungen (manuell)
    confirmed INTEGER DEFAULT 0,          -- 1 = vom Polier bestätigt
    confirmed_at TEXT,
    photos TEXT,                          -- JSON-Array mit Foto-Pfaden
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);
```

**Konzept:** ModuleBautagebuch.md  
**Auto-Befüllung:** Tätigkeiten + Personal aus work_assignments + time_entries, Wetter aus API

### 5.9 contacts (Adressbuch)

Zentrale Kontaktdatenbank, projektübergreifend. Getrennt von project_participants (ADR-024).

```sql
CREATE TABLE contacts (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "contact_001"
    company TEXT NOT NULL DEFAULT '',
    contact_person TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT '',         -- "Statiker" | "Architekt" | "ÖBA"
    phone TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    address TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    outlook_id TEXT,                       -- Für Outlook-Sync (Zukunft)
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

**Konzept:** ADR-024  
**Verknüpfung:** project_participants.contact_id → contacts.id

### 5.10 material_orders (Task-Management)

Materialbestellungen, verknüpft mit Arbeitspaketen. Synchronisiert mit externem Task-Management (ClickUp, Asana, Trello etc.) über ITaskManagementService Interface.

```sql
CREATE TABLE material_orders (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "mo_001"
    project_id TEXT NOT NULL,             -- FK → projects.id
    work_package_id TEXT,                 -- FK → work_packages.id (optional)
    building_part_id TEXT,                -- FK → building_parts.id
    level_id TEXT,                        -- FK → building_levels.id
    material TEXT NOT NULL,               -- "Ziegel 38er Objekt Plan"
    quantity REAL NOT NULL,               -- 45 (Paletten)
    unit TEXT NOT NULL,                   -- "Pal" | "m³" | "to" | "Stk"
    delivery_date_requested TEXT,         -- gewünschter Liefertermin
    delivery_date_confirmed TEXT,         -- bestätigter Liefertermin
    urgency TEXT DEFAULT 'Normal',        -- "Hoch" | "Normal" | "Niedrig"
    source TEXT DEFAULT 'Intern',         -- "Intern" (Lager) | "Extern" (Einkauf)
    status TEXT DEFAULT 'open',           -- "open" | "ordered" | "delivered" | "cancelled"
    external_task_id TEXT,                -- ClickUp Task-ID / Asana Task-ID etc.
    external_system TEXT,                 -- "ClickUp" | "Asana" | "Trello"
    notes TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id),
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id)
);
```

**Konzept:** ModuleTaskManagement.md (noch zu erstellen)  
**Integration:** Über ITaskManagementService Interface — nicht an ClickUp gebunden

### 5.11 external_call_log (Datenschutz / Audit)

Audit-Log für alle externen HTTP-Calls über IExternalCommunicationService (ADR-035). Protokolliert ob ein Call erlaubt oder blockiert wurde und warum.```sql
CREATE TABLE external_call_log (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "ecl_001"
    timestamp TEXT NOT NULL,
    module TEXT NOT NULL,              -- "ki", "gis_google", "wetter", "task_mgmt"
    target_domain TEXT NOT NULL,       -- "api.openai.com"
    classification TEXT NOT NULL,      -- "ClassA", "ClassB", "ClassC"
    purpose TEXT,                      -- "LV-Analyse", "Adresse suchen"
    status_code INTEGER,              -- HTTP Status (200, 403, 500)
    blocked INTEGER DEFAULT 0,        -- 1 wenn blockiert
    decision_reason TEXT              -- "allowed_class_a", "blocked_global_killswitch",
                                      -- "blocked_module_disabled", "allowed_user_confirmed",
                                      -- "blocked_class_c_no_anonymization",
                                      -- "blocked_dpa_not_confirmed",
                                      -- "allowed_anonymized_payload", "internal_mode"
);
```

**Konzept:** DSVGO-Architektur.md Kapitel 11.3  
**Besitzer:** Infrastructure (ExternalCommunicationService)  
**Löschung:** Automatisch nach 90 Tagen  
**Hinweis:** Keine Personendaten loggen — nur Modul, Domain, Klassifizierung und Entscheidungsgrund

**Negativliste (verbindlich) — external_call_log darf NICHT enthalten:**
- Request-Body (Dokumenteninhalte, Prompts)
- Response-Body (KI-Antworten, API-Ergebnisse)
- HTTP-Headers (Authorization, Cookies)
- Query-Parameter mit Personendaten
- IP-Adressen

**`purpose` Regeln:** Max. 100 Zeichen, nur fachlicher Zweck (z.B. "LV-Analyse Mengenprüfung"), keine Personen-/Dokument-/Projektdetails.

**`decision_reason` — Kontrolliertes Vokabular (kein Freitext):**

| Code | Bedeutung |
|------|-----------|
| `allowed_class_a` | Klasse A, keine Einschränkung |
| `allowed_user_confirmed` | User hat Klasse B/C explizit bestätigt |
| `allowed_anonymized_payload` | Payload wurde vor Senden anonymisiert |
| `allowed_internal_mode` | RelaxedPrivacyPolicy (interner Betrieb) |
| `blocked_global_killswitch` | Globaler Kill-Switch aktiv |
| `blocked_module_disabled` | Modul in Einstellungen deaktiviert |
| `blocked_auto_calls_not_enabled` | Auto-Calls für dieses Modul nicht freigeschaltet |
| `blocked_class_c_requires_override` | Klasse C ohne Anonymisierung/User-Override |
| `blocked_dpa_not_confirmed` | KI-Modul ohne DPA-Bestätigung |
| `blocked_policy_denied` | Sonstige Policy-Ablehnung |

Im Code als `static class ExternalDecisionReasons` mit `const string` Feldern.

### 5.12 project_shares (Multi-User / Projektfreigabe)

Einfache Projektfreigaben für Phase 2 (JSON Event-Sync). Wird in Phase 3 durch RBAC-Tabellen (users, roles, project_user_role) erweitert.
```sql
CREATE TABLE project_shares (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT UNIQUE NOT NULL,              -- "pshare_001"
    project_id TEXT NOT NULL,
    shared_with_user TEXT NOT NULL,   -- Username oder Geräte-ID
    permission TEXT NOT NULL,          -- "full" | "read" | "plans_only" | "diary_write"
    shared_at TEXT NOT NULL DEFAULT (datetime('now')),
    valid_until TEXT,                  -- NULL = unbefristet
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

**Konzept:** MultiUserKonzept.md Kapitel 6  
**Besitzer:** Settings (Projektfreigabe-Dialog)  
**Migration:** Phase 3 ergänzt users, roles, project_user_role — project_shares bleibt als einfache Variante

---

## 6. PlanManager-Datenbank (separat)

Pro Projekt eine eigene SQLite-DB für den PlanManager. Liegt in `%LocalAppData%\BauProjektManager\Projects\<ProjektID>\planmanager.db`.

### 6.1 import_journal

```sql
CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,                  -- "imp_20260326_143200"
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,                 -- "pending" | "completed" | "failed" | "undone"
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
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,             -- "new" | "indexUpdate" | "overwrite" | "skip"
    action_status TEXT NOT NULL,           -- "pending" | "completed" | "failed"
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
```

### 6.3 import_action_files

```sql
CREATE TABLE import_action_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    action_id INTEGER NOT NULL,
    file_name TEXT NOT NULL,
    file_type TEXT NOT NULL,               -- "pdf" | "dwg" | "other"
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    md5_hash TEXT,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);
```

**Konzept:** BauProjektManager_Architektur.md Kapitel 7

---

## 7. Schema-Migration

### 7.1 Migrationshistorie

| Version | Datum | Änderung |
|---------|-------|---------|
| 1.0 | März 2026 | clients, projects, buildings, schema_version |
| 1.1 | März 2026 | project_type Spalte zu projects |
| 1.2 | März 2026 | building_parts, building_levels (ersetzt buildings) |
| 1.3 | März 2026 | (reserviert) |
| 1.4 | März 2026 | project_participants |
| 1.5 | März 2026 | project_links |
| *1.6* | *geplant* | *employees, time_entries* |
| *1.7* | *geplant* | *work_packages, work_assignments* |
| *1.8* | *geplant* | *lv_positions, performance_catalog, project_difficulty* |
| *1.9* | *geplant* | *diary_entries* |
| *2.0* | *geplant* | *contacts, material_orders, buildings-Tabelle entfernen* |
| *2.1* | *geplant* | *external_call_log (Audit-Log, ADR-035)* |
| *2.2* | *geplant* | *project_shares (Multi-User Phase 2, ADR-038)* |
| *3.0* | *geplant* | *users, roles, project_user_role (Multi-User Phase 3, ADR-038)* |

### 7.2 Migrationsregeln

- Schema-Version wird bei App-Start geprüft und automatisch migriert
- Neue Spalten: `ALTER TABLE ... ADD COLUMN` mit DEFAULT-Wert
- Neue Tabellen: `CREATE TABLE IF NOT EXISTS`
- Tabellen löschen: Erst wenn sicher ist dass keine Daten mehr darin stecken
- Rückwärtskompatibel: Ältere App-Versionen ignorieren neue Tabellen/Spalten
- Backup vor Migration: `bpm.db` → `bpm.db.bak` kopieren

---

## 8. Datenfluss zwischen Modulen

```
EINSTELLUNGEN (Stammdaten)
│ projects, clients, building_parts, building_levels,
│ project_participants, project_links
│
├──→ KALKULATION
│    │ work_packages (Soll-Mengen aus Ziegelber./DokaCad/LV)
│    │ lv_positions (LV-Import)
│    │ project_difficulty (Erschwernisse)
│    │
│    ├──→ ARBEITSEINTEILUNG (täglich)
│    │    │ work_assignments (wer → welches Paket)
│    │    │
│    │    └──→ BAUTAGEBUCH (täglich, auto-befüllt)
│    │         │ diary_entries (Wetter, Bemerkungen, Fotos)
│    │         │
│    │         └──→ DASHBOARD (Übersicht)
│    │
│    ├──→ NACHKALKULATION (bei Fertigmeldung)
│    │    │ performance_catalog (Erfahrungswerte)
│    │    │
│    │    └──→ BAUZEITPROGNOSE (neues Projekt)
│    │
│    └──→ TASK-MANAGEMENT (Materialbestellungen)
│         │ material_orders → ClickUp/Asana/Trello
│
├──→ ZEITERFASSUNG
│    │ employees (Stammdaten)
│    │ time_entries (wer, wann, Stunden, Abwesenheit)
│    │
│    └──→ fließt in: Arbeitseinteilung, Bautagebuch, Nachkalkulation
│
└──→ PLANMANAGER (eigene DB pro Projekt)
     │ planmanager.db (Cache, Journal, Undo)
```

---

## 9. Naming-Konventionen

| Konvention | Regel | Beispiel |
|-----------|-------|---------|
| Tabellennamen | snake_case, Plural | `building_parts`, `time_entries` |
| Spaltennamen | snake_case | `project_id`, `short_name`, `created_at` |
| ID-Spalten | `id` (PK) oder `<tabelle>_id` (FK) | `id`, `project_id`, `building_part_id` |
| ID-Präfix | Kürzel + Unterstrich + 3-stellig | `proj_001`, `bpart_042` |
| Boolean | INTEGER (0/1) | `active`, `confirmed` |
| Datum | TEXT im Format YYYY-MM-DD | `project_start`, `date` |
| Zeitstempel | TEXT im Format datetime('now') | `created_at`, `updated_at` |
| Enums | TEXT mit definierten Werten | `status`: "Active" \| "Completed" |
| NULL | Nur wenn Wert optional ist | `actual_end`, `rduk`, `absence_type` |
| DEFAULT '' | Für Pflicht-Textfelder die leer sein dürfen | `company`, `notes` |

### 9.1 seq vs. id — Rollen (ADR-039)

Jede Tabelle hat zwei Spalten für Identifikation:

| Spalte | Typ | Rolle | Verwendet in |
|--------|-----|-------|-------------|
| `seq` | INTEGER PRIMARY KEY AUTOINCREMENT | Rein interne SQLite-Einfügereihenfolge. ROWID-Alias. | NUR intern für Sortierung ("zeige die letzten 10 Einträge") |
| `id` | TEXT UNIQUE NOT NULL | Fachlich stabile, präfixierte Kennung. | FKs, JSON-Export, Logging, VBA, Debugging, UI |

**Verbindliche Regeln:**
- Alle Fremdschlüssel referenzieren die `id`-Spalte der Zieltabelle, **NIEMALS** `seq`
- `seq` darf in keinem JSON, keinem Log, keinem Export, keiner externen Schnittstelle erscheinen
- FK-Spalten sind immer `TEXT` (nicht `INTEGER`)

### 9.2 Präfix-Tabelle (ADR-039)

| Tabelle | Präfix | Beispiel |
|---------|--------|---------|
| projects | `proj_` | `proj_001` |
| clients | `client_` | `client_042` |
| building_parts | `bpart_` | `bpart_003` |
| building_levels | `blvl_` | `blvl_017` |
| project_participants | `ppart_` | `ppart_005` |
| project_links | `plink_` | `plink_002` |
| employees | `emp_` | `emp_007` |
| time_entries | `te_` | `te_1523` |
| work_packages | `wp_` | `wp_042` |
| work_assignments | `wa_` | `wa_305` |
| lv_positions | `lv_` | `lv_089` |
| performance_catalog | `perf_` | `perf_012` |
| project_difficulty | `pdiff_` | `pdiff_003` |
| diary_entries | `diary_` | `diary_201` |
| contacts | `contact_` | `contact_015` |
| material_orders | `mo_` | `mo_034` |
| external_call_log | `ecl_` | `ecl_4201` |
| project_shares | `pshare_` | `pshare_002` |

ID-Generierung zentral über `EntityIdGenerator` in Infrastructure. Kein Modul darf IDs selbst zusammenbauen.

---

## 10. JSON-Konfigurationsdateien (kein SQLite)

Nicht alles liegt in SQLite. Konfiguration und Sync-Dateien sind JSON:

| Datei | Speicherort | Beschreibung | Geschrieben von |
|-------|------------|-------------|----------------|
| `settings.json` | Cloud .AppData/ | App-Einstellungen, Pfade, Listen | AppSettingsService |
| `registry.json` | Cloud .AppData/ | Generierter VBA-Export (read-only für VBA) | RegistryJsonExporter |
| `profiles.json` | Cloud .AppData/Projects/<P>/ | Plantyp-Profile pro Projekt | PlanManager |
| `pattern-templates.json` | Cloud .AppData/ | Globale Musterbibliothek | PlanManager |
| `.bpm-manifest` | Cloud Projektordner | Versteckter Projekt-Ausweis | ProjectFolderService |

---

*Dieses Dokument wird bei jeder Schema-Änderung aktualisiert. Es ist die einzige Quelle der Wahrheit für die Datenbankstruktur.*