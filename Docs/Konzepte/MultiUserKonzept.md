# BauProjektManager — Konzept: Multi-User Support

**Erstellt:** 30.03.2026  
**Aktualisiert:** 03.04.2026  
**Version:** 2.0  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Keine (kann unabhängig von anderen Modulen implementiert werden)  
**Relevante ADRs:** ADR-016 (Single-Writer Mutex), ADR-020 (Write-Lock mit Heartbeat), ADR-033 (3 Modi), ADR-035 (IExternalCommunicationService), ADR-036 (IPrivacyPolicy), ADR-037 (ISyncTransport — geplant), ADR-038 (IAccessControlService — geplant)  
**Verwandte Dokumente:**
- [DatenarchitekturSync.md](DatenarchitekturSync.md) — **Konkretisierung:** Datenklassifizierung, Sync-Mechanismus, User/Rollen-Schema, Outbox/Inbox, Snapshots (4-Runden Cross-Review, 10.04.2026)
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Rollenmatrix, Datenschutz-Klassifizierung
- [BPM-Mobile-Konzept.md](BPM-Mobile-Konzept.md) — PWA nutzt Server-Modus
- [ModuleAktivierungLizenzierung.md](ModuleAktivierungLizenzierung.md) — Lizenz steuert IPrivacyPolicy

---

## Änderungshistorie

| Version | Datum | Änderung |
|---------|-------|----------|
| 1.0 | 30.03.2026 | Erstversion: 3 Modi (Solo, Geteilte DB, Server) |
| 2.0 | 03.04.2026 | Komplett überarbeitet: Reale Baustellen-Szenarien, Phase 2 JSON Event-Sync, Phase 3 REST Server mit Offline-Cache, rollenbasierte Projektfreigabe, ISyncTransport/IAccessControlService Interfaces, Erkenntnisse aus ChatGPT-Analyse professioneller Bau-Software |

---

## 1. Ziel

Mehrere Personen mit verschiedenen Rollen (Bauleiter, Polier, Disponent, Einkäufer, Lohnbüro) sollen am selben Projekt arbeiten — jeder sieht und bearbeitet nur seinen Teil. Datenfreigabe passiert auf Projektebene, nicht global.

### 1.1 Reale Szenarien auf der Baustelle

| Rolle | Hat eigene Daten | Will freigeben an | Beispiel |
|---|---|---|---|
| **Bauleiter** | Projekte, Pläne, Kalkulationen, LV | Polier: Pläne + Bautagebuch-Zugriff | Polier soll Pläne sehen und Bautagebuch schreiben, aber keine LV-Positionen sehen |
| **Polier** | Bautagebuch, Arbeitseinteilung, Fotos | Bauleiter: Alles | Bauleiter soll das Bautagebuch des Poliers live sehen |
| **Disponent** | Geräteliste, Fahrzeugverfügbarkeit | Bauleiter + Polier: Geräte pro Projekt | Disponent hat zentrale Geräte-DB, gibt pro Projekt frei |
| **Einkäufer** | Lieferantenliste, Bestellungen, Preise | Bauleiter: Bestellstatus. Polier: Liefertermine | Einkäufer hat eigene Daten (Preise) die der Polier nicht sehen soll |
| **Lohnbüro** | Arbeitszeitmodelle, Lohnsätze | Bauleiter: Übersicht | Lohnbüro liest Zeiterfassung, niemand sieht Lohnsätze |

### 1.2 Grundprinzipien (unverändert)

- **Offline-first:** Jede Funktion muss ohne Internet funktionieren
- **Kein Cloud-Zwang:** Funktioniert auch mit lokalem Netzlaufwerk
- **Kein Abo:** Kein externer Dienst, keine laufenden Kosten
- **Einfach:** Ein Polier der kein IT-Experte ist muss es bedienen können
- **Wartbar:** Solo-Entwickler muss den Code allein pflegen können

---

## 2. Wie machen es die Profis?

Analyse professioneller Bau-Software (Quelle: ChatGPT-Recherche, April 2026):

| Produkt | Berechtigungsmodell | Offline-Lösung |
|---|---|---|
| **PlanRadar** | Nutzer erhalten Projekte/Pläne/Tickets nach Rolle | Projekte vorab synchronisieren, offline Tickets erstellen, bei Verbindung automatisch hochladen |
| **Procore** | RBAC mit Permission Templates (None/Read-Only/Standard/Admin) pro Projekt + Modul | Mobile App cached Daten vorab, Änderungen bei Verbindung synchronisiert |
| **Dalux** | Rollen × Module × Regionen (Gebäude/Lose) | „Prepare for offline", Outbox für Änderungen, Auto-Sync |

**Gemeinsames Muster:** Zentraler Server als Single Source of Truth. Clients haben lokalen Cache. Offline-Arbeit über vorab geladene Daten + Outbox für Änderungen. Berechtigungen werden serverseitig erzwungen (projekt- und modulbasiert).

**Fazit für BPM:** Die Zielarchitektur ist klar — zentraler Server mit lokalen Caches. Aber der Weg dahin muss schrittweise sein.

---

## 3. Phasenmodell — 3 Stufen

### Übersicht

| Phase | Für wen | Technologie | Nutzer | Wann |
|---|---|---|---|---|
| **Phase 1: Solo** | Herbert allein, 2 Geräte | Eigene bpm.db pro Gerät, Dateisystem-Sync über Cloud-Speicher | 1 | Jetzt (V1) |
| **Phase 2: Event-Sync** | Polier + Bauleiter, asynchron | Eigene bpm.db pro User, JSON-Events über Cloud-Ordner | 2–3 | Nach V1 |
| **Phase 3: Server** | Team mit Rollen, live | ASP.NET Minimal API, lokaler Offline-Cache pro Client | 5–10+ | Langfristig |

### Entscheidungskriterium: Wann welche Phase?

```
1 Nutzer, 2 Geräte                    → Phase 1 (jetzt)
2–3 Nutzer, asynchron, 1–2 Module     → Phase 2 (JSON Event-Sync)
4+ Nutzer ODER 3+ schreibende Rollen  → Phase 3 (REST Server)
Dispo/Einkauf/Lohnbüro beteiligt      → Phase 3 (REST Server)
```

---

## 4. Phase 1: Solo (Status Quo)

Unverändert gegenüber v1.0 dieses Dokuments. Jeder User hat sein eigenes `bpm.db` in `%LocalAppData%`. Cloud-Speicher synct Projektordner + JSON-Konfiguration. SQLite synct NICHT.

Für Details siehe ADR-004 (Dreistufige Cloud-Sync-Strategie).

---

## 5. Phase 2: JSON Event-Sync (NEU)

### 5.1 Konzept

Jeder User hat seine eigene lokale `bpm.db`. Der Datenaustausch passiert über **JSON-Event-Dateien** in einem geteilten Cloud-Ordner. Kein Server, kein direkter DB-Zugriff durch andere.

**Kernprinzip: Nicht „JSON als Datenbank-Ersatz", sondern „JSON als Transportformat für Änderungen".**

```
Polier (Baustelle)                    Bauleiter (Büro)
├── bpm.db (eigene)                   ├── bpm.db (eigene)
│                                      │
├── Cloud-Speicher/BPM-Sync/          ├── Cloud-Speicher/BPM-Sync/
│   └── project-001/                   │   └── project-001/
│       └── events/                    │       └── events/
│           ├── ...herbert_001.json    │           ├── ...herbert_001.json
│           └── ...polier_002.json     │           └── ...polier_002.json
```

### 5.2 Warum nicht SQLite direkt auf Cloud-Speicher?

SQLite verwendet intern ein Write-Ahead Log (`bpm.db-wal`) und ein Shared-Memory File (`bpm.db-shm`). Wenn OneDrive/Dropbox während eines Schreibvorgangs die Datei synct:

- OneDrive synct Hauptdatei aber nicht WAL → **DB korrupt**
- Zwei Geräte schreiben gleichzeitig → Konflikt-Kopie → **Daten gesplittet**
- Dropbox synct während Lock → **Sync-Fehler**

SQLite warnt offiziell: „Do not use SQLite on a network filesystem." JSON-Dateien können Cloud-Dienste problemlos syncen (atomisches Schreiben: write-to-temp-then-rename).

### 5.3 Event-Ordnerstruktur

```
Cloud-Speicher/BPM-Sync/
└── project-<projectId>/
    ├── manifest.json                    ← Projekt-Teilnehmer (selten geändert)
    └── events/                          ← Append-only Event-Dateien
        ├── 2026-04-03T14-32-00Z_herbert_evt001.json
        ├── 2026-04-03T15-10-12Z_polier1_evt002.json
        └── ...
```

**Warum append-only Events statt Outbox/Inbox:**

- Kein Dateien-Verschieben nötig (wer verschiebt wann?)
- Keine „verarbeitet"-Markierung in geteilten Dateien
- Jeder Client tracked selbst was er schon importiert hat (in seiner lokalen SQLite)
- Robuster bei Cloud-Sync-Verzögerungen

### 5.4 Event-Format

```json
{
  "eventId": "evt_20260403_143200_herbert_001",
  "schemaVersion": 1,
  "projectId": "proj_001",
  "authorUserId": "herbert",
  "deviceId": "herbert-laptop",
  "createdAtUtc": "2026-04-03T14:32:00Z",
  "entityType": "DiaryEntry",
  "entityId": "diary_proj001_2026-04-03",
  "eventType": "DiaryEntryUpdated",
  "baseVersion": 3,
  "newVersion": 4,
  "permissionScope": "diary",
  "payload": {
    "date": "2026-04-03",
    "weather": "bewölkt",
    "notes": "Attika betoniert",
    "workersPresent": 6
  }
}
```

**Pflichtfelder für Sync:**

| Feld | Zweck |
|---|---|
| `eventId` | Eindeutige ID, verhindert doppelte Verarbeitung |
| `entityId` + `entityType` | Was wurde geändert |
| `baseVersion` + `newVersion` | Konflikterkennung |
| `authorUserId` + `deviceId` | Wer hat wann von wo geändert |
| `permissionScope` | Welche Rolle darf das Event sehen |
| `createdAtUtc` | Zeitstempel für Sortierung |

### 5.5 manifest.json (minimal)

```json
{
  "projectId": "proj_001",
  "schemaVersion": 1,
  "participants": [
    { "userId": "herbert", "role": "bauleiter" },
    { "userId": "polier1", "role": "polier" }
  ]
}
```

Kein globaler Status, kein Zustand der ständig überschrieben wird. Nur Teilnehmer-Liste.

### 5.6 Lokale Sync-Tabellen (in bpm.db)

```sql
-- Welche Events wurden schon verarbeitet?
CREATE TABLE sync_processed_events (
    event_id TEXT PRIMARY KEY,
    processed_at TEXT NOT NULL,
    project_id TEXT NOT NULL
);

-- Konflikte die der User entscheiden muss
CREATE TABLE sync_conflicts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_id TEXT NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    local_version INTEGER,
    remote_version INTEGER,
    local_payload TEXT,        -- JSON
    remote_payload TEXT,       -- JSON
    resolved INTEGER DEFAULT 0,
    resolved_at TEXT,
    resolution TEXT            -- "keep_local" | "keep_remote" | "merged"
);
```

### 5.7 Konflikbehandlung

| Datentyp | Strategie |
|---|---|
| Append-only Daten (Fotos, neue Plan-Events, neue Protokolle) | Automatisch übernehmen |
| Objekte mit getrennten Feldern (Bautagebuch: Wetter vs. Freitext) | Feldweises Merge versuchen |
| Gleiches Feld von zwei Nutzern geändert | Konflikt erzeugen → User entscheidet |

**Beispiel Bautagebuch-Konflikt:**

```
┌─────────────────────────────────────────────────────┐
│ ⚠️ Konflikt: Bautagebuch 03.04.2026                  │
│                                                      │
│ Polier (15:10):           Bauleiter (15:32):         │
│ Bemerkungen:              Wetter:                    │
│ "Attika betoniert"        "sonnig → bewölkt"         │
│                                                      │
│ [Polier-Version]  [Bauleiter-Version]  [Zusammenführen] │
└─────────────────────────────────────────────────────┘
```

### 5.8 Berechtigungen bei JSON-Sync

Ohne Server können Berechtigungen **nicht erzwungen** werden — nur organisatorisch abgebildet.

**Pragmatischer Ansatz: Empfängerspezifische Ordner**

```
BPM-Sync/project-001/
├── events/               ← Alle Events (Klasse A — Planfreigaben, Metadaten)
├── to-polier1/           ← Nur für Polier bestimmte Events
├── to-herbert/           ← Nur für Bauleiter bestimmte Events
└── shared-read/          ← Lesbare Stammdaten-Ausschnitte
```

**Regel:** Keine sensiblen Daten (LV, Kalkulationen, Lohnsätze) über JSON-Sync. Diese Daten erst mit Phase 3 (REST Server) sauber freigeben.

### 5.9 Sinnvolle Use-Cases für Phase 2

| Use-Case | Richtung | Datenklasse |
|---|---|---|
| Bautagebuch-Einträge | Polier ↔ Bauleiter | B |
| Fotos / Foto-Metadaten | Polier → Bauleiter | A |
| Plan-Import-Ereignisse | Bauleiter → Polier | A |
| Projektfreigaben | Bauleiter → Polier | A |
| Lesbare Stammdaten-Ausschnitte | Bauleiter → Polier | A/B |

### 5.10 Was Phase 2 NICHT kann

- ❌ Gleichzeitiges Bearbeiten derselben Entität (nur asynchron)
- ❌ Serverseitig erzwungene Berechtigungen
- ❌ Live-Daten (nur bei nächstem Sync)
- ❌ Sensible Daten sicher freigeben (LV, Lohnsätze)
- ❌ 4+ gleichzeitig schreibende Nutzer

### 5.11 Wann wird Phase 2 fragil?

- 4+ aktive Nutzer schreiben regelmäßig
- Mehrere Rollen brauchen verschiedene Sichten auf dieselben Daten
- Dispo, Einkauf, Lohnbüro sind produktiv beteiligt
- Konflikte werden häufiger
- Du baust Sync-Ordner, Verschlüsselung, Conflict-UI und Zustell-Tracking aus → dann bist du faktisch schon beim Server

---

## 6. Phase 3: REST Server (erweitert)

### 6.1 Konzept

Ein ASP.NET Minimal API Server besitzt die Datenbank exklusiv. Alle Clients verbinden per HTTP. Der Server erzwingt Berechtigungen. Offline-Cache auf jedem Client.

**Das ist die Zielarchitektur — entspricht dem Ansatz von PlanRadar, Procore und Dalux.**

```
                    ┌──────────────────┐
                    │   BPM-Server     │
                    │   (Firmen-PC,    │
                    │    Raspi, NAS)   │
                    │                  │
                    │   bpm.db         │
                    │   ASP.NET API    │
                    │   Auth + RBAC    │
                    └────────┬─────────┘
                             │ HTTP (LAN/WLAN)
                ┌────────────┼────────────┐
                │            │            │
          ┌─────▼────┐ ┌────▼─────┐ ┌────▼─────┐
          │ PC Büro  │ │ Laptop   │ │ Handy    │
          │ BPM.exe  │ │ BPM.exe  │ │ PWA      │
          │ (Cache)  │ │ (Cache)  │ │ (Cache)  │
          └──────────┘ └──────────┘ └──────────┘
```

### 6.2 Rollenbasierte Projektfreigabe

#### Berechtigungsmatrix

| Modul | Bauleiter | Polier | Disponent | Einkäufer | Lohnbüro |
|---|---|---|---|---|---|
| Projekte | Vollzugriff | Lesezugriff (zugewiesen) | Lesezugriff (Projektliste) | Lesezugriff (Bestellungen) | Lesezugriff |
| Pläne | Vollzugriff | Lesen | — | — | — |
| Bautagebuch | Lesen & Prüfen | Erstellen/Bearbeiten | — | — | Lesen (Zeitdaten) |
| Geräteliste | Lesen | Lesen | Vollzugriff | — | — |
| Einkauf | Lesen (Status) | Lesen (Liefertermine) | — | Vollzugriff | — |
| Zeiterfassung | Lesen | Eingabe eigener Zeiten | — | — | Vollzugriff |
| Kalkulation | Vollzugriff | — | — | — | — |

#### DB-Schema für Berechtigungen

**Einfaches Modell (Phase 2 — project_shares):**

```sql
CREATE TABLE project_shares (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    shared_with_user TEXT NOT NULL,   -- Username oder Geräte-ID
    permission TEXT NOT NULL,          -- "full" | "read" | "plans_only" | "diary_write"
    shared_at TEXT NOT NULL DEFAULT (datetime('now')),
    valid_until TEXT,                  -- NULL = unbefristet
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

**Erweitertes Modell (Phase 3 — RBAC):**

```sql
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    short_name TEXT,
    windows_username TEXT,            -- Auto-Erkennung
    role TEXT DEFAULT 'Polier',       -- Default-Rolle
    employee_id INTEGER,
    settings_json TEXT,
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);

CREATE TABLE roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,        -- "Bauleiter", "Polier", "Disponent", "Einkäufer", "Lohnbüro", "Admin", "Viewer"
    description TEXT
);

CREATE TABLE project_user_role (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    module_flags TEXT,                -- JSON: {"plans": "read", "diary": "write", "timesheet": "none"}
    valid_from TEXT,
    valid_to TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (role_id) REFERENCES roles(id)
);
```

### 6.3 Offline-Cache mit Sync

Wie bei PlanRadar/Dalux: Client lädt freigegebene Projekte vorab. Arbeitet offline. Synchronisiert bei Verbindung.

```
Polier geht auf Baustelle:
  1. BPM zeigt: "Projekte für Offline vorbereiten?"
  2. Polier wählt Projekte → Cache wird gefüllt
  3. Auf Baustelle: Arbeitet offline (Bautagebuch, Fotos, Arbeitseinteilung)
  4. Unsynchronisierte Änderungen werden markiert (⚠️ Symbol)
  5. Zurück im Büro/WLAN: "Synchronisieren" Button
  6. Client schickt Änderungen → Server prüft Konflikte
  7. Bei Konflikt: User entscheidet
  8. Server schickt Änderungen anderer Nutzer zurück
```

#### Sync-Felder pro Tabelle

Jede Tabelle die synchronisiert werden soll bekommt:

```sql
-- Bestehende Spalten +
version INTEGER NOT NULL DEFAULT 1,
last_modified TEXT NOT NULL DEFAULT (datetime('now')),
modified_by TEXT                    -- User-ID
```

### 6.4 REST API (minimal)

```csharp
// Authentifizierung (JWT oder einfacher API-Key pro User)
app.MapPost("/api/auth/login", (LoginRequest r) => AuthService.Login(r));

// Projekte (gefiltert nach Berechtigung)
app.MapGet("/api/projects", (HttpContext ctx) =>
    db.GetProjectsForUser(ctx.User.Id));

// Sync-Endpunkt
app.MapPost("/api/sync", (SyncRequest r, HttpContext ctx) =>
    syncService.ProcessSync(r, ctx.User));
```

### 6.5 Server-Hardware (unverändert)

| Hardware | Kosten | Für wen |
|---|---|---|
| Raspberry Pi 4/5 | ~50-80€ | Baucontainer, kleines Büro |
| Alter Laptop | 0€ | Büro |
| Mini-PC (NUC) | ~150€ | Dauerbetrieb |
| NAS mit Docker | vorhanden | Synology, QNAP |

---

## 7. Interfaces — Architektur-Vorbereitung

### 7.1 IDataAccessService (pro Aggregat)

```csharp
// In BauProjektManager.Domain/Services/
public interface IProjectDataService
{
    Task<List<Project>> GetAllAsync(CancellationToken ct = default);
    Task<Project?> GetByIdAsync(string id, CancellationToken ct = default);
    Task SaveAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface IPlanDataService { /* ... */ }
public interface IDiaryDataService { /* ... */ }
```

**Zwei Implementierungen (später):**

```csharp
// Lokal — direkter SQLite-Zugriff (wie jetzt)
public class LocalProjectDataService : IProjectDataService { }

// Remote — HTTP-Client zum Server
public class RemoteProjectDataService : IProjectDataService { }
```

### 7.2 ISyncTransport (austauschbar)

```csharp
// In BauProjektManager.Domain/Sync/
public interface ISyncTransport
{
    Task SendAsync(SyncEnvelope envelope, CancellationToken ct = default);
    Task<IReadOnlyList<SyncEnvelope>> ReceiveAsync(string projectId, CancellationToken ct = default);
}

public class SyncEnvelope
{
    public string EventId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string AuthorUserId { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public int BaseVersion { get; init; }
    public int NewVersion { get; init; }
    public string PermissionScope { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string PayloadJson { get; init; } = string.Empty;
}
```

**Zwei Implementierungen:**

```csharp
// Phase 2 — JSON-Dateien im Cloud-Ordner
public class FolderSyncTransport : ISyncTransport { }

// Phase 3 — HTTP POST/GET zum Server
public class HttpSyncTransport : ISyncTransport { }
```

**Der Sync-Kern bleibt gleich — nur der Transport wechselt.**

### 7.3 IAccessControlService

```csharp
// In BauProjektManager.Domain/Security/
public interface IAccessControlService
{
    bool CanAccess(string userId, string projectId, string module, AccessLevel level);
    Task<List<Project>> GetAccessibleProjectsAsync(string userId, CancellationToken ct = default);
    Task ShareProjectAsync(string projectId, string userId, string permission, CancellationToken ct = default);
}

public enum AccessLevel
{
    None,
    Read,
    Write,
    Admin
}
```

**Implementierung in Infrastructure** (wie IPrivacyPolicy):

```csharp
// Phase 1 — alles erlaubt (Solo)
public class NoOpAccessControlService : IAccessControlService
{
    public bool CanAccess(...) => true;
}

// Phase 2/3 — echte Prüfung
public class RbacAccessControlService : IAccessControlService { }
```

---

## 8. Was JETZT in den Code muss (bei PlanManager)

| Vorbereitung | Aufwand | Warum jetzt |
|---|---|---|
| `IProjectDataService` Interface statt direkt `ProjectDatabase.cs` aufrufen | Dünne Abstraktionsschicht | Später austauschbar gegen Remote-Implementierung |
| `version`, `last_modified`, `modified_by` Felder in neuen Tabellen | 3 Spalten pro Tabelle | Vermeidet späteres ALTER TABLE auf gefüllten Tabellen |
| CRUD-Methoden mit „Änderungen seit Timestamp" Parameter vorbereiten | Parameter hinzufügen | Sync braucht Delta-Abfragen, später schwer nachzurüsten |
| Dieses Konzeptdokument | — | Architektur-Entscheidungen dokumentiert |

### Was WARTEN kann

- ISyncTransport Implementierung → erst wenn Phase 2 gebraucht wird
- IAccessControlService Implementierung → erst wenn zweiter User kommt
- REST API Server → erst wenn Phase 3 nötig ist
- project_shares Tabelle → kann leer existieren, UI erst später
- Offline-Cache-Logik → erst mit Server-Modus

---

## 9. Phase 1: Solo (Details — unverändert)

*(Kapitel 2–4 aus v1.0 bleiben gültig und werden hier nicht wiederholt. Siehe Git-History für v1.0.)*

### 9.1 Kurzzusammenfassung

- Jeder User hat eigene `bpm.db` in `%LocalAppData%`
- Cloud-Speicher synct Projektordner + JSON-Konfiguration
- SQLite synct NICHT — wird auf zweitem Gerät rekonstruiert
- Write-Lock mit Heartbeat bei geteilter DB (ADR-020)
- Kein User-Management, kein Login

---

## 10. Integration mit DSGVO-Architektur

Die bestehende DSGVO-Architektur (Kapitel 10.2) definiert bereits eine Rollenmatrix:

| Rolle | Sieht Projektdaten | Sieht Zeiterfassung | Sieht KI-History | Ändert Einstellungen |
|---|---|---|---|---|
| Admin | ✅ | ✅ | ✅ | ✅ |
| Bauleiter | ✅ | ✅ (eigenes Team) | ✅ | ❌ |
| Polier | ✅ (eigene Projekte) | ❌ | ❌ | ❌ |
| Viewer | ✅ (nur lesen) | ❌ | ❌ | ❌ |

Diese Matrix wird in `IAccessControlService` umgesetzt. Die `IPrivacyPolicy` filtert zusätzlich nach Datenklasse (A/B/C) — zwei unabhängige Schichten:

```
Request → IAccessControlService (Darf User X Modul Y in Projekt Z?)
       → IPrivacyPolicy (Darf diese Datenklasse extern übertragen werden?)
       → IExternalCommunicationService (HTTP-Call mit Audit-Log)
```

---

## 11. Abgrenzung

**Dieses Konzept ist NICHT:**
- Ein Cloud-Dienst
- Ein Multi-Mandanten-System
- Ein vollständiges Rechtemanagement mit Passwörtern und Verschlüsselung (Phase 2)

**Dieses Konzept IST:**
- Ein evolutionärer Weg von Solo → Event-Sync → Server
- Offline-fähig auf jeder Stufe
- Kompatibel mit der bestehenden DSGVO-Architektur
- Vorbereitet auf Mobile PWA (Phase 3 Server = gleiche API)

---

*Kernfrage: „Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Die Interface-Vorbereitung (IDataAccessService, Sync-Felder) kommt schon beim PlanManager-Bauen rein.*
