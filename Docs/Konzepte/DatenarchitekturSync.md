# BauProjektManager — Datenarchitektur & Sync-Konzept

**Erstellt:** 10.04.2026
**Version:** 1.0
**Status:** Entschieden (Claude + ChatGPT Cross-Review, 4 Runden)
**Relevante ADRs:** ADR-002, ADR-004, ADR-033, ADR-037, ADR-039, ADR-046
**Verwandte Dokumente:**
- [MultiUserKonzept.md](MultiUserKonzept.md) — Phasenmodell, Rollen, Server-Architektur
- [DB-SCHEMA.md](../Kern/DB-SCHEMA.md) — Tabellen-Definitionen
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Datenschutz-Klassifizierung
- [BauProjektManager_Architektur.md](../Kern/BauProjektManager_Architektur.md) — Gesamtarchitektur

---

## 1. Grundsatz

**State-based lokal, change-based zwischen Clients.**

- SQLite ist die **einzige lokale Wahrheit** (Working Store)
- Änderungen erzeugen **Change Records** in einem lokalen change_log
- Events sind **Replikations- und Audit-Mechanismus**, nicht Source of Truth
- Snapshots dienen Bootstrap und Recovery
- Kein Full Event Sourcing — zu komplex für Solo-Entwickler

---

## 2. Datenklassifizierung (4 Klassen)

### A. Local-only (synct NIE)

| Daten | Warum lokal |
|-------|------------|
| `planmanager.db` (Journal, Undo, Cache) | Import läuft auf einem Gerät |
| Logs | Nur für lokales Debugging |
| `device-settings.json` (Pfade, Fensterzustände) | Pro Gerät unterschiedlich |
| sync_outbox, sync_inbox, sync_applied_events | Lokale Sync-Verwaltung |
| sync_conflicts, sync_checkpoints | Lokale Konflikt-Verwaltung |

### B. Shared Domain (synct zwischen allen Projekt-Mitgliedern)

| Daten | Beispiel |
|-------|---------|
| projects, clients | Stammdaten |
| building_parts, building_levels | Bauwerk |
| project_participants, project_links | Beteiligte, Portale |
| work_packages, work_assignments | Kalkulation, Arbeitseinteilung |
| diary_days, diary_notes | Bautagebuch (aufgeteilt!) |
| Foto-Metadaten | Tags, GPS, Bauteil-Zuordnung |
| Plan-Profile (.bpm/profiles/) | Plantyp-Erkennung |
| project_memberships | Wer sieht welches Projekt |

### C. Shared Reference/Config (globale Kataloge)

| Daten | Speicherort |
|-------|------------|
| ProjectTypes, BuildingTypes | `.AppData/shared-config.json` |
| ParticipantRoles, PortalTypes, LevelNames | `.AppData/shared-config.json` |
| FolderTemplate | `.AppData/shared-config.json` |
| Globale Pattern-Templates | `.AppData/pattern-templates.json` |
| Projektspezifische Overrides | `.bpm/config.json` (optional) |

### D. Restricted Shared (erst mit Server in Phase 3)

| Daten | Warum restricted |
|-------|-----------------|
| `employee_compensation` (Lohnsätze) | Geht nur Lohnbüro an |
| `lv_pricing` (Einheitspreise) | Geht nur Bauleiter an |
| `material_order_prices` (Einkaufspreise) | Geht nur Einkauf an |
| `project_budget_values` | Geht nur Bauleiter an |

**Regel:** Sensitive Finanzdaten leben in **eigenen Tabellen**, nicht als Spalten in der Basistabelle. Beispiel: `employees` (shared) + `employee_compensation` (restricted).

---

## 3. Speicherstruktur

### 3.1 Lokal (%LocalAppData%)

```
%LocalAppData%/BauProjektManager/
  bpm.db                    ← Alle Stamm-/Projektdaten
  device-settings.json      ← Lokale Pfade, UI-Zustände
  Projects/<ProjektID>/
    planmanager.db           ← Cache, Journal, Undo
  Logs/
  Backups/
  Temp/
```

### 3.2 Cloud-Speicher (.AppData/)

```
<CloudRoot>/.AppData/BauProjektManager/
  shared-config.json        ← Globale Kataloge (ProjectTypes etc.)
  pattern-templates.json    ← Globale Musterbibliothek
  registry.json             ← Generierter VBA-Export
```

### 3.3 Projektordner (.bpm/)

```
<Projektordner>/.bpm/
  manifest.json             ← Schlank: Identität + Module-Flags
  project.json              ← Snapshot: Vollständiger Projektexport
  config.json               ← Optional: Projekt-Overrides
  profiles/
    <profilname>.json        ← Plantyp-Profile
  snapshots/
    root-snapshot.json       ← Manifest mit Watermark + Checksums
    project.snapshot.json
    participants.snapshot.json
    diary.snapshot.json
    work.snapshot.json
    plans.snapshot.json
  events/
    2026/04/
      evt_01J....json        ← Change-Events (Full-Document-Payload)
```

---

## 4. Sync-Metadaten auf Shared Tabellen

Jede sync-relevante Tabelle (Klasse B + D) bekommt diese Spalten:

```sql
id TEXT PRIMARY KEY,                    -- ULID
project_id TEXT,                        -- Partition Key (fast überall)
created_at_utc TEXT NOT NULL,
created_by_user_id TEXT NOT NULL,       -- FK → users.id
updated_at_utc TEXT NOT NULL,
updated_by_user_id TEXT NOT NULL,       -- FK → users.id
entity_version INTEGER NOT NULL DEFAULT 1,
is_deleted INTEGER NOT NULL DEFAULT 0,  -- Soft Delete
deleted_at_utc TEXT NULL,
deleted_by_user_id TEXT NULL,
origin_device_id TEXT NOT NULL,         -- FK → user_devices.id
last_change_id TEXT NOT NULL            -- ULID des change_log Eintrags
```

**Local-only Tabellen** (planmanager.db) bekommen KEINE Sync-Metadaten — nur `created_at_utc` und ggf. `updated_at_utc`.

---

## 5. Sync-Mechanismus (Outbox/Inbox Pattern)

### 5.1 Schreiben (lokal)

```
1. Domain-Mutation in SQLite          ← z.B. Bauteil umbenennen
2. change_log schreiben               ← In derselben Transaktion!
3. sync_outbox befüllen               ← In derselben Transaktion!
4. COMMIT
```

Transaktionale Mutation Boundary: Domain-Write + change_log + sync_outbox gehören zusammen. Nie ohne einander.

### 5.2 Exportieren (Outbox → Event-Dateien)

```
5. Separater Exporter-Prozess liest sync_outbox
6. Serialisiert Event-JSON nach .bpm/events/YYYY/MM/
7. Markiert sync_outbox Einträge als "exported"
```

### 5.3 Importieren (Event-Dateien → lokale DB)

```
8. Separater Importer-Prozess scannt .bpm/events/ nach neuen Dateien
9. Validiert JSON + Checksum
10. Prüft sync_applied_events (schon verarbeitet?)
11. Prüft entity_version (Konflikt?)
12. Wendet Änderung transaktional an
13. Markiert als "applied" oder erzeugt Conflict Record
```

### 5.4 Sync-Timing (3 Trigger)

| Trigger | Wann | Was |
|---------|------|-----|
| **App-Start** | Einmal | Import + Export + Snapshot-Check |
| **Timer** | Alle 30-60 Sekunden | Idempotenter Outbox/Inbox Lauf |
| **Manuell** | "Synchronisieren"-Button | Voller Import + Export |

### 5.5 Event-Format (Full-Document-Payload)

```json
{
  "eventId": "01J...",
  "projectId": "01J...",
  "entityType": "DiaryNote",
  "entityId": "01J...",
  "eventType": "EntityUpserted",
  "entityVersion": 4,
  "prevEntityVersion": 3,
  "authorUserId": "01J...",
  "authorDeviceId": "01J...",
  "occurredAtUtc": "2026-04-10T08:12:31Z",
  "schemaVersion": 1,
  "checksum": "sha256:...",
  "payload": {
    "id": "01J...",
    "diary_day_id": "01J...",
    "author_user_id": "01J...",
    "note_type": "work",
    "text": "Schalungsarbeiten Nordseite",
    "entity_version": 4,
    "is_deleted": 0
  }
}
```

**Event-Typen:** Nur `EntityUpserted` + `EntityDeleted` (nicht feiner).
**Payload:** Normalisierte Vollrepräsentation des Entity-Zustands (kein JSON-Patch).

---

## 6. Snapshots

### 6.1 Struktur (modular + Root-Manifest)

`root-snapshot.json` enthält Manifest + Watermark:

```json
{
  "snapshotId": "01J...",
  "projectId": "01J...",
  "createdAtUtc": "2026-04-10T09:00:00Z",
  "schemaVersion": 1,
  "upToEventId": "01J...",
  "modules": [
    { "name": "project", "file": "project.snapshot.json", "checksum": "..." },
    { "name": "participants", "file": "participants.snapshot.json", "checksum": "..." },
    { "name": "diary", "file": "diary.snapshot.json", "checksum": "..." },
    { "name": "work", "file": "work.snapshot.json", "checksum": "..." }
  ]
}
```

### 6.2 Wann Snapshot erzeugen (ereignisgesteuert)

- Nach 100 exportierten Events pro Projekt, ODER
- Wenn letzter Snapshot älter als 7 Tage, ODER
- Nach strukturrelevanten Änderungen (Massenimport), ODER
- Manuell ("Projekt-Snapshot erstellen")

### 6.3 Bootstrap (neuer Client)

1. Snapshot laden (root-snapshot.json → modulare Dateien)
2. In lokale DB importieren
3. Danach nur Events ab `upToEventId` anwenden

### 6.4 Retention

- **Lokal (change_log):** 30 Tage, dann trimmen
- **Shared (events/):** 90 Tage behalten
- **Snapshots:** Letzte 3 pro Projekt behalten

---

## 7. Konflikt-Behandlung

### 7.1 Merge-Strategien pro Entity-Typ

| Strategie | Entity-Typen | Verhalten |
|-----------|-------------|-----------|
| **Append-only** | photo_metadata, diary_notes, Kommentare | Neue IDs → beide bleiben. Gleiche ID → Versionsregel |
| **Set/Membership** | project_participants, work_assignments, Tags | Upsert pro Datensatz. Delete als Tombstone |
| **Nicht auto-mergebar** | Gleiches Textfeld, Mengen, Preise, Planprofile | Konflikt erzeugen → UI zeigt lokal vs. remote → User entscheidet |

### 7.2 Delete-Regeln

- Delete gegen unveränderte ältere Version → übernehmen
- Delete gegen zwischenzeitlich geänderte neuere Version → Konflikt
- Delete gegen bereits gelöscht → idempotent (kein Konflikt)

### 7.3 Aggregate-Design reduziert Konflikte

**Wichtig:** Konflikte werden primär durch besseres Datenmodell vermieden, nicht durch UI.

Beispiel Bautagebuch:
- ❌ `diary_entries` (eine Zeile pro Tag) → 3 Poliere = Konflikte
- ✅ `diary_days` (Tageskopf: Wetter, Bestätigung) + `diary_notes` (viele Notizen pro Tag pro User) → fast keine Konflikte

---

## 8. User- und Rollen-Modell

### 8.1 Tabellen (jetzt anlegen)

```sql
CREATE TABLE users (
  id TEXT PRIMARY KEY,
  username TEXT NOT NULL UNIQUE,
  display_name TEXT NOT NULL,
  email TEXT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  created_at_utc TEXT NOT NULL,
  updated_at_utc TEXT NOT NULL
);

CREATE TABLE user_devices (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  device_name TEXT NOT NULL,
  device_fingerprint TEXT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  created_at_utc TEXT NOT NULL,
  updated_at_utc TEXT NOT NULL,
  FOREIGN KEY(user_id) REFERENCES users(id)
);

CREATE TABLE roles (
  id TEXT PRIMARY KEY,
  code TEXT NOT NULL UNIQUE,    -- bauleiter, polier, disponent, einkauf, lohnbuero
  name TEXT NOT NULL,
  scope_type TEXT NOT NULL      -- global / project
);

CREATE TABLE user_roles (
  user_id TEXT NOT NULL,
  role_id TEXT NOT NULL,
  PRIMARY KEY(user_id, role_id),
  FOREIGN KEY(user_id) REFERENCES users(id),
  FOREIGN KEY(role_id) REFERENCES roles(id)
);

CREATE TABLE project_memberships (
  id TEXT PRIMARY KEY,
  project_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  access_level TEXT NOT NULL,   -- owner/editor/viewer
  is_active INTEGER NOT NULL DEFAULT 1,
  created_at_utc TEXT NOT NULL,
  updated_at_utc TEXT NOT NULL,
  FOREIGN KEY(project_id) REFERENCES projects(id),
  FOREIGN KEY(user_id) REFERENCES users(id)
);
```

### 8.2 Rollen (fachlich)

| Code | Name | Scope |
|------|------|-------|
| `bauleiter` | Bauleiter | project |
| `polier` | Polier | project |
| `disponent` | Disponent | global |
| `einkauf` | Einkäufer | global |
| `lohnbuero` | Lohnbüro | global |
| `admin` | Administrator | global |
| `viewer` | Nur Lesen | project |

Berechtigungs-Mapping (Rolle → Module) lebt im Server-Code (Phase 3), nicht in der DB.

---

## 9. settings.json Split

### Vorher (FALSCH — gemischt)
```
settings.json = lokale Pfade + globale Kataloge + alles zusammen
```

### Nachher (RICHTIG — getrennt)

| Datei | Ort | Inhalt |
|-------|-----|--------|
| `device-settings.json` | Lokal `%LocalAppData%` | BasePath, AppDataPath, letzte Fensterposition, zuletzt geöffnete Projekte, lokale Feature Flags |
| `shared-config.json` | Cloud `.AppData/` | ProjectTypes, BuildingTypes, ParticipantRoles, PortalTypes, LevelNames, FolderTemplate |
| `.bpm/config.json` | Projektordner | Projektspezifische Overrides (optional) |

### Override-Regeln (3 Ebenen)
1. Built-in Defaults aus App-Code
2. Global shared config aus `.AppData/shared-config.json`
3. Project Override aus `.bpm/config.json`

Merge: Replace-by-key, kein Deep-Merge.

---

## 10. Phasen-Modell (bestätigt)

| Phase | User | Transport | Sichtbarkeit | Server |
|-------|------|-----------|-------------|--------|
| **1 Solo** | 1, 2 Geräte | Kein Sync | — | Nein |
| **2 Event-Sync** | 2-3 | FolderSyncTransport (Cloud-Ordner) | Projektbasiert | Nein |
| **3 Server** | 4-10+ | HttpSyncTransport (REST API) | RBAC pro Modul | PostgreSQL |

**Phase 2 ist bewusst temporär.** Keine Sicherheitsarchitektur, keine Skalierungsarchitektur. Nur funktionaler Übergang für kleine Teams.

### Exit-Kriterien Phase 2 → Phase 3

- Mehr als 3-4 aktive Schreiber pro Projekt
- Mehr als 2 Rollen mit Datenrestriktionen
- Bedarf an zentraler Benutzerverwaltung
- Konfliktrate wird untragbar
- Supportaufwand wegen Sync steigt

### Phase 3: PostgreSQL serverseitig

- Client: SQLite (unverändert)
- Server: PostgreSQL + ASP.NET Minimal API
- Gleiches Fachmodell, anderer Betriebsmodus
- Server validiert, autorisiert, verteilt

Minimaler API-Schnitt:
- `POST /sync/push` — Events hochladen
- `GET /sync/bootstrap?projectId=...` — Snapshot für neuen Client
- `GET /sync/pull?projectId=...&afterCheckpoint=...` — Delta-Events
- `POST /sync/checkpoint` — Client-Fortschritt melden

---

## 11. Code-Vorbereitung (Reihenfolge)

| # | Aufgabe | Warum jetzt |
|---|---------|------------|
| 1 | ULID-Migration | Vor Multi-User. Später ist teurer. |
| 2 | users + user_devices | Stabile Identitäten für Audit/Sync |
| 3 | roles + project_memberships | RBAC-ready Schema |
| 4 | Sync-Metadaten auf alle Shared-Tabellen | 12 Spalten, einmal richtig |
| 5 | Soft Deletes | Sonst Delete-Replikation kaputt |
| 6 | settings.json Split | Modellierungsfehler jetzt beheben |
| 7 | Mutation Boundary + IChangeTracker | Transaktionale Konsistenz |
| 8 | change_log + sync_outbox Tabellen | Grundlage für Sync |
| 9 | diary_days + diary_notes (Aggregate-Split) | Konflikte vermeiden |
| 10 | Sensitive Tabellen abspalten | employee_compensation, lv_pricing |
| 11 | ISyncExporter + ISyncImporter | Transport-Logik |
| 12 | FolderSyncTransport | Phase 2 Übergang |

---

## 12. Lokale Sync-Tabellen (in bpm.db)

```sql
CREATE TABLE change_log (
  change_id TEXT PRIMARY KEY,          -- ULID
  project_id TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,             -- insert/update/delete
  entity_version INTEGER NOT NULL,
  changed_at_utc TEXT NOT NULL,
  changed_by_user_id TEXT NOT NULL,
  device_id TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  sync_state TEXT NOT NULL DEFAULT 'pending'  -- pending/exported/conflict
);

CREATE TABLE sync_outbox (
  id TEXT PRIMARY KEY,
  change_id TEXT NOT NULL,
  project_id TEXT NOT NULL,
  created_at_utc TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'pending',     -- pending/exported/failed
  exported_at_utc TEXT NULL,
  retry_count INTEGER NOT NULL DEFAULT 0,
  FOREIGN KEY(change_id) REFERENCES change_log(change_id)
);

CREATE TABLE sync_applied_events (
  event_id TEXT PRIMARY KEY,
  project_id TEXT NOT NULL,
  processed_at_utc TEXT NOT NULL
);

CREATE TABLE sync_conflicts (
  id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL,
  project_id TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  conflict_type TEXT NOT NULL,
  local_version INTEGER,
  incoming_version INTEGER,
  local_payload_json TEXT,
  incoming_payload_json TEXT,
  resolved INTEGER NOT NULL DEFAULT 0,
  resolved_at_utc TEXT NULL,
  resolution TEXT NULL                 -- keep_local/keep_remote/merged
);
```

---

## 13. Transaktionale Mutation Boundary

Jede fachliche Mutation läuft durch einen zentralen Mechanismus:

```csharp
public interface IChangeTrackedDb
{
    Task ExecuteAsync(Func<ChangeContext, Task> action);
}

public sealed class ChangeContext
{
    public SqliteConnection Connection { get; }
    public SqliteTransaction Transaction { get; }
    public IChangeTracker ChangeTracker { get; }
}
```

Bestehende `ProjectDatabase.cs` bleibt, aber Mutationen laufen über diese Boundary. Kein volles UoW/Repository-System — nur eine schmale transaktionale Hülle.

---

## 14. Leitplanken (verbindlich)

1. SQLite bleibt **einzige lokale Wahrheit**
2. Events bleiben **Replikationsartefakte**, kein zweiter Fachzustand
3. Shared vs. restricted bleibt **physisch getrennt** (eigene Tabellen)
4. Aggregates werden **konfliktarm geschnitten** (diary_days + diary_notes)
5. FolderSync bleibt **bewusst temporär** (Phase 2 nur für 2-3 User)
6. Phase 3 (Server + PostgreSQL) wird **als Zielbild mitgeführt**, nicht als "vielleicht irgendwann"

---

*Dokument basiert auf Claude + ChatGPT Cross-Review (4 Runden, 10.04.2026).*
*Ergebnis: Volle Einigkeit, keine offenen Widersprüche.*
