# BauProjektManager — Konzept: Multi-User Support

**Erstellt:** 30.03.2026  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Keine (kann unabhängig von anderen Modulen implementiert werden)  
**Relevante ADRs:** ADR-016 (Single-Writer Mutex), ADR-020 (Write-Lock mit Heartbeat)

---

## 1. Ziel

Mehrere Poliere / Bauleiter sollen BPM auf derselben Baustelle nutzen können — mit gemeinsamen Projektdaten, ohne Konflikte, ohne Cloud-Server.

**Drei Modi die BPM unterstützen soll:**

| Modus | Für wen | Beschreibung |
|-------|---------|-------------|
| **Eigene DB** | Solo-Polier (Standard) | Jeder hat sein eigenes `bpm.db` lokal. So wie jetzt. |
| **Geteilte DB** | Kleines Team (2-5 Leute) | `bpm.db` liegt auf einem gemeinsamen Netzlaufwerk oder Cloud-Ordner. |
| **Server** | Team mit mobilem Zugriff | ASP.NET Minimal API auf einem Rechner/Raspi, alle Clients verbinden sich per HTTP. |

---

## 2. Modus A: Eigene DB (Standard)

### 2.1 So wie jetzt

Jeder User hat sein eigenes `bpm.db` in `%LocalAppData%\BauProjektManager\`. Funktioniert sofort, offline, kein Setup. Das ist der Default für Solo-Poliere.

```
PC Herbert                    Laptop Herbert
├── %LocalAppData%            ├── %LocalAppData%
│   └── bpm.db (eigene)       │   └── bpm.db (eigene)
│                              │
├── Cloud-Speicher             ├── Cloud-Speicher (sync)
│   ├── Projektordner          │   ├── Projektordner
│   ├── .AppData/              │   ├── .AppData/
│   │   ├── registry.json      │   │   ├── registry.json
│   │   └── settings.json      │   │   └── settings.json
```

### 2.2 Synchronisation zwischen eigenen Geräten

Herbert arbeitet auf PC und Laptop. Beide haben ein eigenes `bpm.db`. Die Projektdaten (Ordner, Pläne, Fotos) synchronisieren über den Cloud-Speicher. Die SQLite-DB synchronisiert NICHT — sie wird auf dem zweiten Gerät beim ersten Start aus dem Dateisystem (Cloud-Ordner + .bpm-manifest) rekonstruiert.

### 2.3 Wann reicht Modus A?

- Solo-Polier mit 1-2 Geräten
- Keine Kollegen die gleichzeitig Daten ändern
- Jeder Polier hat "seine" Projekte

---

## 3. Modus B: Geteilte DB (Netzlaufwerk / Cloud-Ordner)

### 3.1 Konzept

Das `bpm.db` liegt auf einem gemeinsamen Pfad den alle im Team sehen — z.B. ein Netzlaufwerk (`\\server\bpm\bpm.db`) oder ein Cloud-Ordner der auf allen Rechnern synchronisiert wird.

```
Netzlaufwerk / Cloud-Ordner (gemeinsam)
├── bpm.db                    ← ALLE lesen und schreiben hier
├── registry.json
├── settings.json
│
PC Polier 1                   PC Polier 2
├── BPM.exe                   ├── BPM.exe
│   └── Verbindung zu         │   └── Verbindung zu
│       \\server\bpm\bpm.db   │       \\server\bpm\bpm.db
```

### 3.2 Einstellung in BPM

In den Systemeinstellungen: "Datenbank-Speicherort"
- **Lokal** (Standard): `%LocalAppData%\BauProjektManager\bpm.db`
- **Benutzerdefiniert**: Pfad zum gemeinsamen Ordner (z.B. `\\server\bpm\` oder `D:\Dropbox\BPM\`)

### 3.3 SQLite-Konkurrenz-Problem

SQLite kann von mehreren Prozessen gleichzeitig GELESEN werden, aber nur EIN Prozess darf gleichzeitig SCHREIBEN. Wenn zwei Poliere gleichzeitig ein Projekt bearbeiten und speichern, gibt es einen Lock-Fehler.

**Lösung: Single-Primary-Writer mit Heartbeat (ADR-020)**

```
┌─────────────────────────────────────────────┐
│              bpm.db (gemeinsam)              │
│                                              │
│  write_lock Tabelle:                         │
│  ┌──────────┬──────────┬────────────────┐   │
│  │ locked_by│ machine  │ last_heartbeat │   │
│  ├──────────┼──────────┼────────────────┤   │
│  │ Herbert  │ PC-BÜRO  │ 2025-11-05     │   │
│  │          │          │ 14:32:15       │   │
│  └──────────┴──────────┴────────────────┘   │
│                                              │
│  Regel:                                      │
│  - Wer schreiben will, holt den Lock         │
│  - Heartbeat alle 60 Sekunden                │
│  - Lock verfällt nach ~3 verpassten          │
│    Heartbeats (180 Sekunden)                 │
│  - Andere können lesen, aber nicht schreiben  │
│  - Bei Lock-Konflikt: Warnung anzeigen       │
└─────────────────────────────────────────────┘
```

### 3.4 DB-Schema Erweiterung

```sql
CREATE TABLE write_lock (
    id INTEGER PRIMARY KEY CHECK (id = 1),  -- nur eine Zeile
    locked_by TEXT,                           -- Username
    machine_name TEXT,                        -- PC-Name
    locked_at TEXT,                           -- Zeitstempel
    last_heartbeat TEXT,                      -- letzter Heartbeat
    app_version TEXT                          -- BPM-Version
);
```

### 3.5 Ablauf

```
Polier 1 öffnet BPM:
  1. Prüfe write_lock → leer → Lock setzen ✅
  2. Heartbeat-Timer starten (alle 60s)
  3. Arbeiten, speichern, ändern → alles geht

Polier 2 öffnet BPM (gleichzeitig):
  1. Prüfe write_lock → gesperrt von Polier 1
  2. Meldung: "Datenbank wird gerade von Herbert (PC-BÜRO) bearbeitet.
     Du kannst Daten ansehen, aber nicht ändern."
  3. Read-Only Modus → alles anzeigen, Buttons zum Speichern deaktiviert
  4. Hintergrund-Check alle 30s: Lock noch aktiv?
  5. Wenn Polier 1 BPM schließt → Lock wird freigegeben
  6. Polier 2 bekommt Meldung: "Datenbank ist jetzt frei. Schreibzugriff aktiviert."

Polier 1 stürzt ab (Lock bleibt stehen):
  1. Heartbeat kommt nicht mehr
  2. Nach 180 Sekunden: Lock gilt als verfallen
  3. Polier 2 kann übernehmen
```

### 3.6 GUI — Lock-Anzeige

```
┌─────────────────────────────────────────────────────┐
│ ⚠️ Datenbank gesperrt                               │
│                                                      │
│ Bearbeitet von: Herbert Schrotter                    │
│ Rechner: PC-BÜRO                                     │
│ Seit: 14:32 (vor 12 Minuten)                         │
│                                                      │
│ Du kannst alle Daten ansehen, aber nicht ändern.      │
│ Sobald Herbert BPM schließt, wirst du benachrichtigt.│
│                                                      │
│ [OK — Read-Only weiterarbeiten]  [Lock übernehmen*]  │
│                                                      │
│ * Nur wenn du sicher bist, dass Herbert nicht         │
│   mehr arbeitet (z.B. PC nicht mehr erreichbar)       │
└─────────────────────────────────────────────────────┘
```

### 3.7 Wann reicht Modus B?

- Kleines Team (2-5 Poliere/Bauleiter)
- Gemeinsames Netzlaufwerk im Büro
- Nicht gleichzeitiges Schreiben (einer arbeitet, andere schauen)
- Kein mobiler Zugriff nötig

### 3.8 Grenzen von Modus B

- Cloud-Sync (Dropbox, OneDrive) kann bei gleichzeitigem Zugriff Konflikte erzeugen
- Netzlaufwerk muss erreichbar sein (kein Offline-Arbeiten auf der geteilten DB)
- Nur ein Schreiber gleichzeitig (Single-Primary-Writer)

---

## 4. Modus C: Server (ASP.NET Minimal API)

### 4.1 Konzept

Ein Rechner im Büro (oder Baucontainer) läuft als Server. Alle BPM-Clients verbinden sich per HTTP. Der Server ist der einzige der auf SQLite zugreift — keine Lock-Probleme mehr.

```
                    ┌──────────────┐
                    │  BPM-Server  │
                    │  (Raspi/PC)  │
                    │              │
                    │  bpm.db      │
                    │  ASP.NET API │
                    │  Port 5000   │
                    └──────┬───────┘
                           │ HTTP (LAN/WLAN)
              ┌────────────┼────────────┐
              │            │            │
        ┌─────▼────┐ ┌────▼─────┐ ┌────▼─────┐
        │ PC Büro  │ │ Laptop   │ │ Handy    │
        │ BPM.exe  │ │ BPM.exe  │ │ PWA      │
        │ (Client) │ │ (Client) │ │ (Client) │
        └──────────┘ └──────────┘ └──────────┘
```

### 4.2 Was der Server macht

Der Server ist ein minimaler HTTP-Dienst, KEIN vollständiger Web-Server:

```csharp
// Program.cs — ASP.NET Minimal API
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Projekte
app.MapGet("/api/projects", () => db.LoadAllProjects());
app.MapGet("/api/projects/{id}", (string id) => db.LoadProject(id));
app.MapPost("/api/projects", (Project p) => db.SaveProject(p));

// Arbeitspakete
app.MapGet("/api/projects/{id}/work-packages", (string id) => db.LoadWorkPackages(id));
app.MapPost("/api/work-packages", (WorkPackage wp) => db.SaveWorkPackage(wp));

// Zeiterfassung
app.MapPost("/api/time-entries", (TimeEntry te) => db.SaveTimeEntry(te));

// Arbeitseinteilung
app.MapPost("/api/work-assignments", (WorkAssignment wa) => db.SaveWorkAssignment(wa));

app.Run("http://0.0.0.0:5000");
```

### 4.3 Was der Server NICHT macht

- Keine Benutzerauthentifizierung (LAN-only, Vertrauensbasis)
- Kein HTTPS (nur im lokalen Netzwerk)
- Keine komplexe Geschäftslogik (die bleibt im Client)
- Kein Web-Frontend (der Client ist die WPF-App oder PWA)

### 4.4 Hardware-Optionen

| Hardware | Kosten | Stromverbrauch | Für wen |
|----------|--------|---------------|---------|
| **Raspberry Pi 4/5** | ~50-80€ | ~5W | Baucontainer, kleines Büro |
| **Alter Laptop** | 0€ (vorhanden) | ~30W | Büro |
| **Büro-PC (nebenbei)** | 0€ | — | Wenn PC sowieso läuft |
| **Mini-PC (NUC)** | ~150€ | ~10W | Dauerbetrieb im Büro |

**Software-Kosten:** Null. .NET 10 und ASP.NET sind kostenlos. SQLite ist kostenlos. Keine Lizenzen.

### 4.5 Raspberry Pi Setup

```
1. SD-Karte mit Raspberry Pi OS beschreiben (Imager-Tool, 5 Min)
2. Booten, WLAN konfigurieren
3. .NET 10 installieren:
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash
4. BPM-Server draufkopieren (USB oder SCP)
5. Starten:
   dotnet BpmServer.dll
6. Fertig — API läuft auf http://raspi:5000
```

Aufwand: ~30 Minuten beim ersten Mal.

### 4.6 Client-Umschaltung

In den BPM-Einstellungen: "Datenbankverbindung"
- **Lokal** (Standard): `bpm.db` in %LocalAppData%
- **Netzwerk-DB**: Pfad zum gemeinsamen Ordner
- **Server**: `http://192.168.1.100:5000` (IP oder Hostname)

```
┌─────────────────────────────────────────────────────┐
│ Einstellungen → Datenbankverbindung                  │
│                                                      │
│ Modus: (●) Lokal (eigene DB)                        │
│        ( ) Netzwerk-DB (gemeinsamer Ordner)          │
│        ( ) Server (HTTP-Verbindung)                  │
│                                                      │
│ ── Netzwerk-DB ──────────────────────────────────    │
│ Pfad: [\\server\bpm\                    ] [📁]      │
│                                                      │
│ ── Server ───────────────────────────────────────    │
│ Adresse: [http://192.168.1.100:5000     ] [Testen]  │
│                                          ✅ Verbunden│
│                                                      │
│ [Speichern]                                          │
└─────────────────────────────────────────────────────┘
```

### 4.7 Offline-Fallback

Was passiert wenn der Server nicht erreichbar ist (WLAN auf der Baustelle ausgefallen)?

```
1. Client merkt: Server nicht erreichbar
2. Wechselt automatisch auf lokale Kopie (bpm_cache.db)
3. Arbeitet offline weiter (Zeiterfassung, Arbeitseinteilung)
4. Wenn Server wieder erreichbar → automatischer Sync
5. Bei Konflikten: "Server-Version übernehmen" oder "Lokale Änderung behalten"
```

### 4.8 Wann braucht man Modus C?

- Team mit > 3 gleichzeitig schreibenden Usern
- Mobile PWA soll Daten mit Desktop teilen
- Baucontainer mit eigenem WLAN
- Mehrere Baustellen die zentral ausgewertet werden sollen

### 4.9 Grenzen von Modus C

- Server muss laufen (Stromausfall → kein Zugriff, Fallback auf Cache)
- WLAN muss im Baucontainer funktionieren
- Kein Internet-Zugriff von außen (nur LAN) — für Remote-Zugriff wäre VPN oder Cloud-Hosting nötig

---

## 5. Architektur — Interface für alle Modi

### 5.1 IDataService Interface

```csharp
public interface IDataService
{
    // Projekte
    Task<List<Project>> LoadAllProjectsAsync();
    Task<Project> LoadProjectAsync(string id);
    Task SaveProjectAsync(Project project);
    Task DeleteProjectAsync(string id);

    // Arbeitspakete
    Task<List<WorkPackage>> LoadWorkPackagesAsync(string projectId);
    Task SaveWorkPackageAsync(WorkPackage wp);

    // Zeiterfassung
    Task<List<TimeEntry>> LoadTimeEntriesAsync(string projectId, string date);
    Task SaveTimeEntryAsync(TimeEntry entry);

    // Arbeitseinteilung
    Task<List<WorkAssignment>> LoadAssignmentsAsync(string projectId, string date);
    Task SaveAssignmentAsync(WorkAssignment assignment);

    // Connection Info
    bool IsConnected { get; }
    string ConnectionMode { get; }  // "Lokal" | "Netzwerk" | "Server"
}
```

### 5.2 Drei Implementierungen

```csharp
// Modus A: Lokale DB (wie jetzt)
public class LocalDataService : IDataService
{
    // Direkter SQLite-Zugriff auf lokales bpm.db
    // Synchrone Aufrufe, kein Netzwerk
}

// Modus B: Geteilte DB (Netzwerk/Cloud-Ordner)
public class SharedDbDataService : IDataService
{
    // SQLite-Zugriff auf gemeinsames bpm.db
    // Write-Lock mit Heartbeat
    // Read-Only Fallback wenn Lock aktiv
}

// Modus C: Server
public class ServerDataService : IDataService
{
    // HTTP-Client → ASP.NET Minimal API
    // Offline-Cache (bpm_cache.db)
    // Auto-Sync wenn Server wieder erreichbar
}
```

### 5.3 Bestehender Code — was sich ändert

Die bestehende `ProjectDatabase` Klasse (direkter SQLite-Zugriff) wird zur Implementierung von `LocalDataService`. Die ViewModels sprechen nicht mehr direkt mit `ProjectDatabase`, sondern mit `IDataService`. Der DI-Container wählt die Implementierung basierend auf der Einstellung.

```csharp
// App.xaml.cs — DI Registrierung
var settings = LoadSettings();
switch (settings.ConnectionMode)
{
    case "Lokal":
        services.AddSingleton<IDataService, LocalDataService>();
        break;
    case "Netzwerk":
        services.AddSingleton<IDataService, SharedDbDataService>();
        break;
    case "Server":
        services.AddSingleton<IDataService, ServerDataService>();
        break;
}
```

**Wichtig:** Das ist ein Refactoring das erst kommt wenn Multi-User tatsächlich implementiert wird. Für V1 bleibt alles wie es ist (direkte `ProjectDatabase`-Nutzung).

---

## 6. Benutzer-Verwaltung

### 6.1 V1: Kein User-Management

BPM kennt keine User. Jeder der die App öffnet, hat vollen Zugriff. Für einen Solo-Polier oder ein kleines Team reicht das.

### 6.2 Später: Einfaches User-Profil

```sql
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,              -- "Herbert Schrotter"
    short_name TEXT,                 -- "Herbert"
    role TEXT DEFAULT 'Polier',      -- "Polier" | "Bauleiter" | "Admin"
    employee_id INTEGER,             -- FK → employees (Verknüpfung zur Zeiterfassung)
    settings_json TEXT,              -- Persönliche Einstellungen (letzte Baustelle, Fensterposition)
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);
```

**Kein Passwort.** Die App läuft im LAN auf Vertrauensbasis. User wird beim Start aus dem Windows-Benutzernamen erkannt oder aus einer Dropdown-Liste gewählt. Das ist kein Sicherheitssystem, sondern eine Personalisierung (meine letzte Baustelle, meine Einstellungen).

### 6.3 Berechtigungen (optional, später)

| Rolle | Darf | Darf nicht |
|-------|------|-----------|
| **Admin** | Alles | — |
| **Bauleiter** | Projekte anlegen/bearbeiten, Einstellungen ändern | — |
| **Polier** | Bautagebuch, Zeiterfassung, Arbeitseinteilung, PlanManager | Projekte anlegen, Systemeinstellungen |
| **Viewer** | Alles ansehen | Nichts ändern |

---

## 7. Mobile PWA — Verbindung zum Server

Der Server-Modus (Modus C) ist die Grundlage für die Mobile PWA (BPM-Mobile-Konzept.md). Die PWA verbindet sich per HTTP auf dieselbe API wie der Desktop-Client.

```
Desktop (WPF) ──→ IDataService ──→ ServerDataService ──→ HTTP ──→ Server
Mobile (PWA)  ──→ fetch()       ──→ HTTP                ──→ Server
```

Beide Clients sprechen dieselbe API. Der Server kümmert sich um SQLite. Kein Conflict, kein Lock-Problem.

---

## 8. Implementierungsreihenfolge

| Phase | Was | Wann |
|-------|-----|------|
| 1 | **IDataService Interface** definieren | Vor Multi-User |
| 2 | **LocalDataService** (Refactoring von ProjectDatabase) | Vor Multi-User |
| 3 | **SharedDbDataService** + Write-Lock | Wenn Team-Nutzung gewünscht |
| 4 | **ServerDataService** + ASP.NET Minimal API | Wenn Mobile PWA kommt |
| 5 | **Einstellungs-GUI** für Verbindungsmodus | Mit Phase 3 oder 4 |
| 6 | **User-Profil** (optional) | Wenn Berechtigungen nötig |

---

## 9. Abgrenzung

**Dieses Konzept ist NICHT:**
- Ein Cloud-Dienst (kein Internet nötig, alles im LAN)
- Ein Multi-Mandanten-System (eine Firma, nicht mehrere)
- Ein Rechtemanagement (Vertrauensbasis im kleinen Team)

**Dieses Konzept IST:**
- Drei Stufen die schrittweise aktiviert werden können
- Offline-fähig auf jeder Stufe (Modus A sofort, Modus B/C mit Fallback)
- Kostenlos (keine Server-Software, keine Lizenzen)

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Sobald ein zweiter Polier BPM nutzen soll, wird Multi-User nötig.*