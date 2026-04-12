---
doc_id: architektur
doc_type: architecture
authority: source_of_truth
status: active
owner: herbert
topics: [architektur, solution-struktur, speicherstrategie, registry, betriebsmodi, v1-scope]
read_when: [neues-modul, schichtgrenzen, di-setup, speicherort, registry-änderung]
related_docs: [db-schema, dsvgo-architektur, coding-standards, backlog, planmanager]
related_code: [src/BauProjektManager.App/App.xaml.cs, src/BauProjektManager.Domain/, src/BauProjektManager.Infrastructure/Persistence/]
supersedes: []
---

## AI-Quickload
- Zweck: Systemarchitektur, Speicherstrategie, Solution-Struktur und V1-Scope des BPM
- Autorität: source_of_truth
- Lesen wenn: Neues Modul, Schichtgrenzen prüfen, DI-Setup, Speicherort klären, Registry-Änderung
- Nicht zuständig für: DB-Schema-Details (→ DB-SCHEMA.md), Datenschutz-Regeln (→ DSVGO-Architektur.md), Code-Style (→ CODING_STANDARDS.md), PlanManager-Fachlogik (→ PlanManager.md)
- Kapitel:
  - 1. Vision & Übersicht (inkl. 1.4 Ansichtsprofile)
  - 2. Speicherstrategie
  - 3. Zentrale Projekt-Registry
  - 4. PlanManager — Überblick
  - 5. Solution-Struktur
  - 6. Technische Entscheidungen
  - 7. Betriebsmodi
  - 8. V1-Scope & Roadmap
  - 9. Coding Standards + Definition of Done
  - 10. Alle Config-Dateien — Übersicht
- Pflichtlesen:
  - Kapitel 2 (Speicherstrategie) bei jeder Persistenz-Änderung
  - Kapitel 5 (Solution-Struktur) bei neuem Service/Interface/Projekt
  - Kapitel 3.4 (VBA-Kompatibilitäts-Regeln) bei registry.json-Änderung
- Fachliche Invarianten:
  - SQLite ist System of Record — JSON ist generierter Export
  - Dependency-Regel: Domain→NICHTS, Infrastructure→nur Domain, App→alles
  - registry.json ist versionierter Exportvertrag — Felder entfernen ist Breaking Change
  - Modularer Monolith — feste DI-Registrierung, kein Plugin-System (ADR-001)
  - Offline-first — keine Cloud-Services, kein Abo

---

﻿# BauProjektManager — Architektur & Spezifikation

**Version:** 3.0.0  
**Datum:** 11.04.2026  
**Sprache:** C# (.NET 10 LTS), WPF (XAML), MVVM  
**Frameworks:** CommunityToolkit.Mvvm, Serilog, ClosedXML, PdfPig, QuestPDF  
**Basis:** v1.4 + Phase 0/1 Implementierung + Herberts Feedback + Docs-Reorganisation  
**Autor:** Herbert + Claude  

**Verwandte Dokumente:**
- [ADR.md](../Referenz/ADR.md) — 49 Architecture Decision Records
- [VISION.md](../Referenz/VISION.md) — Nordstern, Schmerzpunkte, Zielgruppe
- [DEPENDENCY-MAP.md](../Referenz/DEPENDENCY-MAP.md) — Solution-Struktur + Ökosystem
- [CHANGELOG.md](../Referenz/CHANGELOG.md) — Versionshistorie ab v0.0.0
- [BACKLOG.md](BACKLOG.md) — Feature-Liste mit Status
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — Code-Richtlinien
- [DSVGO-Architektur.md](DSVGO-Architektur.md) — Datenschutz, Privacy Engineering, IPrivacyPolicy
- Modul-Konzepte: [Docs/Konzepte/](../Konzepte/) — Detaillierte Konzeptdokumente pro Modul

---

## 1. Vision & Übersicht

### 1.1 Was ist der BauProjektManager?

Ein modulares Desktop-Tool für Baustellen-Management in Österreich (Steiermark). Eine einzige Anwendung mit internen Feature-Modulen. Offline-fähig, lokal im Dateisystem, synchronisiert über beliebigen Cloud-Speicher (OneDrive, Google Drive, Dropbox etc.), kein Cloud-Abo.

Ausführliche Vision, Schmerzpunkte und Zielgruppe: siehe [VISION.md](VISION.md).

### 1.2 Architekturmodell: Modularer Monolith

Eine einzige Anwendung (`BauProjektManager.exe`) mit fest registrierten Feature-Modulen. Kein Plugin-System, kein dynamisches Laden, kein `IBpmModule` Interface. Module sind separate C#-Projekte (DLLs), werden aber direkt im DI-Container als konkrete Typen registriert. (ADR-001)

```csharp
// DI-Registrierung: direkt, kein Interface
services.AddTransient<PlanManagerViewModel>();
services.AddTransient<SettingsViewModel>();
// Navigation fest in MainWindow.xaml definiert
```

```
BauProjektManager.exe
├── ⚙️ Einstellungen      ← V1 KERN (✅ implementiert)
├── 📁 PlanManager        ← V1 KERN (⬜ in Arbeit)
├── 📷 Foto-Modul         ← nach V1, Prio 1 (Konzept)
├── ⏱️ Zeiterfassung      ← nach V1, Prio 2 (Konzept)
├── 📓 Bautagebuch        ← nach V1, Prio 3 (Konzept)
├── 📊 Dashboard          ← nach V1, Prio 4 (Konzept)
├── 📧 Outlook-Modul      ← nach V1 (Konzept)
├── 🌤 Wetter-Modul       ← nach V1 (Konzept)
└── 📄 Vorlagen           ← nach V1 (Konzept)
```

### 1.3 Module und Prioritäten

| Modul | Funktion | Phase | Konzept-Dok |
|-------|----------|-------|-------------|
| **Einstellungen** | Projekte, Registry, Pfade, Ordnerstruktur | V1 Pflicht | Dieses Dokument |
| **PlanManager** | Pläne sortieren, versionieren | V1 Pflicht | Dieses Dokument |
| **Foto-Management** | WPF-Viewer, Tags, Geodaten, Bautagebuch-Integration | Nach V1, Prio 1 | [Konzepte/ModuleFoto.md](Konzepte/ModuleFoto.md) |
| **Zeiterfassung** | WPF-Maske → Excel via ClosedXML | Nach V1, Prio 2 | [Konzepte/ModuleZeiterfassung.md](Konzepte/ModuleZeiterfassung.md) |
| **Bautagebuch** | Tägliches Protokoll, Auto-Befüllung, Export | Nach V1, Prio 3 | [Konzepte/ModuleBautagebuch.md](Konzepte/ModuleBautagebuch.md) |
| **Dashboard** | Übersicht + Widgets | Nach V1, Prio 4 | [Konzepte/ModuleDashboard.md](Konzepte/ModuleDashboard.md) |
| **Outlook** | COM Interop, Anhänge extrahieren | Nach V1 | [Konzepte/ModuleOutlook.md](Konzepte/ModuleOutlook.md) |
| **Wetter** | API-Anbindung pro Baustelle | Nach V1 | [Konzepte/ModuleWetter.md](Konzepte/ModuleWetter.md) |
| **Vorlagen** | Excel/Word mit Projektdaten befüllen | Nach V1 | [Konzepte/ModuleVorlagen.md](Konzepte/ModuleVorlagen.md) |
| **Plankopf-Extraktion** | Revisionstabelle aus PDF lesen (PdfPig), KI-API | Nach V1 | [Konzepte/Moduleplanheader.md](Konzepte/Moduleplanheader.md) |
| **GIS-Integration** | Katasterdaten, Koordinaten automatisch befüllen | Nach V1 | [Konzepte/ModuleGIS.md](Konzepte/ModuleGIS.md) |
| **KI-Assistent** | LV-Analyse, Dokumentensuche, ChatGPT/Claude API | Nach V1 | [Konzepte/ModuleKiAssistent.md](Konzepte/ModuleKiAssistent.md) |
| **Mobile PWA** | Bautagebuch + Plan-Viewer am Handy | Nach V1 | BPM-Mobile-Konzept.md |

### 1.4 Ansichtsprofile für die Sidebar (Post-V1)

Ansichtsprofile sind vordefinierte Arbeitsprofile für die Sidebar-Navigation. Ein Ansichtsprofil definiert, welche Module in der Sidebar standardmäßig sichtbar sind. Ziel ist eine reduzierte, arbeitsnahe Oberfläche (z. B. Polier, Bauleiter, Disponent, Lohnverrechnung), ohne ein Berechtigungs- oder Multi-User-System einzuführen. (ADR-048)

**Wichtige Abgrenzung:**
- Ansichtsprofile sind **keine Berechtigungen**.
- Ansichtsprofile steuern **nur die Sichtbarkeit in der Sidebar**.
- Ansichtsprofile erzwingen **keine** Lese-/Schreibrechte.
- Ansichtsprofile ersetzen **kein** zukünftiges Access-Control- oder RBAC-Modell.

Der Begriff **„Rolle"** bleibt zukünftigen Zugriffskonzepten vorbehalten. Im Code wird daher von `ViewProfile` bzw. Ansichtsprofil gesprochen.

Die effektive Sichtbarkeit eines Moduls ergibt sich aus vier Schichten:
1. **Lizenz / Verfügbarkeit** — welche Module technisch freigeschaltet sind
2. **Ansichtsprofil** — welche Module standardmäßig sichtbar sein sollen
3. **Benutzer-Override** — welche Module der User zusätzlich ein- oder ausblendet
4. **Kernmodule** — unverzichtbare Module bleiben immer sichtbar

Die Shell rendert nicht direkt aus einer einzelnen `activeModules`-Liste, sondern aus der **effektiven Modul-Sichtbarkeit**. Diese wird zentral aufgelöst und berücksichtigt Lizenzstatus, gewähltes Profil, manuelle Überschreibungen und erforderliche Modul-Abhängigkeiten.

Die Berechnung der effektiven Modul-Sichtbarkeit liegt in einem zentralen Resolver-Service (`IModuleVisibilityResolver`) und nicht in der Shell selbst.

Built-in-Profile werden zentral definiert und sind nicht direkt editierbar. Benutzer können sie duplizieren und als eigene Profile anpassen.

Ansichtsprofile sind ein **Post-V1-Feature**. V1 implementiert weiterhin nur die feste Sidebar für die vorhandenen Kernmodule. Die operative Ausgestaltung von UI, Persistenz, Migration und Modul-Metadaten wird erst bei Implementierung konkretisiert.

### 1.5 Externe Anbindungen

Bestehende VBA-Makros lesen die automatisch generierte `registry.json`:

```
  BauProjektManager.exe ──→ SQLite (System of Record)
                               ↓ automatisch generiert
                            registry.json (read-only)
                               ↓
  Outlook VBA ←────────────────┘  (liest nur, schreibt nie)
  Excel VBA  ←─────────────────┘
  PowerShell (PhotoFolder) ←───┘
```

Detailliertes Ökosystem-Diagramm mit Datenflüssen: siehe [DEPENDENCY-MAP.md](DEPENDENCY-MAP.md).

### 1.6 Multi-Device & Cloud-Sync

- **Zwei Geräte:** PC zuhause + Laptop auf der Baustelle
- **Sync:** Cloud-Speicher (OneDrive, Google Drive, Dropbox etc.) synchronisiert Nutzdaten + Konfiguration
- **Operativer State:** Lokal (`%LocalAppData%`), synct NICHT
- **Sortiert auf beiden Geräten:** Ja — Profile synchen über den Cloud-Speicher
- **Cache-Rebuild:** Wenn auf dem zweiten Gerät gearbeitet wird, baut sich der SQLite-Cache beim ersten Scan automatisch aus dem Dateisystem (Cloud-Ordner) neu auf

### 1.7 Projektname-Format

```
Format: YYYYMM_Kurzname
Beispiele:
  202512_ÖWG-Dobl-Zwaring
  202302_Reihenhäuser-Kapfenberg
  202201_Sanierung-Leoben
```

Projektnummer wird automatisch aus dem Projektstart-Datum generiert (YYYYMM).

---

## 2. Speicherstrategie

### 2.1 System of Record: SQLite

SQLite ist die **einzige Wahrheitsquelle** für alle BPM-Kerndaten (Projekte, Pläne, Stammdaten, Kalkulation, Bautagebuch). JSON-Dateien sind generierte Exporte oder selten geänderte Konfiguration. Wenn JSON korrupt wird → aus SQLite neu generiert. Ausnahme: Zeiterfassung — hier bleibt Excel die Single Source of Truth für Roh-Zeitbuchungen (ADR-018). (ADR-002, ADR-004)

### 2.2 Dreistufige Speicherung

| Kategorie | Speicherort | Inhalt | Synct? |
|-----------|-------------|--------|--------|
| **Nutzdaten** | Cloud-Speicher (Projektordner) | Pläne, Fotos, Dokumente, `_Eingang`, `_Archiv` | Ja |
| **Konfiguration** | Cloud-Speicher (`.AppData/`) | `registry.json`, `settings.json`, `profiles.json`, `pattern-templates.json`, `templates.json` | Ja |
| **Operativer State** | Lokal (`%LocalAppData%\BauProjektManager\`) | SQLite-DBs, Logs, Cache, Undo-Journal, Temp | Nein |

### 2.3 Speicher-Matrix (komplett)

| Datei/Daten | Format | Ort | Synct? | Schreiber | Leser | Änderung |
|-------------|--------|-----|--------|-----------|-------|----------|
| Projekt-Stammdaten | SQLite | Lokal `bpm.db` | Nein | C# | C# | Selten |
| `registry.json` | JSON | Cloud-Speicher `.AppData/` | Ja | C# (auto) | VBA, C# | Bei Projekt-Änderung |
| `settings.json` | JSON | Cloud-Speicher `.AppData/` | Ja | C# | C# | Selten | Post-V1: Split in `device-settings.json` (lokal) + `shared-config.json` (Cloud) — ADR-047 |
| ~~`profiles.json`~~ | — | ~~`.AppData/Projects/<P>/`~~ → `.bpm/profiles/*.json` im Projektordner (ADR-046) | Ja | C# | C# | Beim Anlernen |
| `pattern-templates.json` | JSON | Cloud-Speicher `.AppData/` | Ja | C# | C# | Beim Anlernen |
| `templates.json` | JSON | Cloud-Speicher `.AppData/` | Ja | C# | C# | Selten |
| Plan-Cache | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Scan/Import |
| Import-Journal | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Import |
| Undo-Daten | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Import |
| Logs | Dateien | Lokal `Logs/` | Nein | Serilog | Dev | Ständig |
| `.bpm/` Ordner | JSON | Cloud-Speicher Projektordner | Ja | C# | C#, Apps | Bei Speichern |
| Pläne (PDF/DWG) | Dateien | Cloud-Speicher Projektordner | Ja | Import | User | Bei Import |
| Vorlagen | Excel/Word | Cloud-Speicher `Vorlagen/` | Ja | User | User, C# | Selten |

### 2.4 Ordnerstruktur (Cloud-Speicher)
Cloud-Speicher/02Arbeit/

```
OneDrive/02Arbeit/
│
├── .AppData/                                  ← VERSTECKT, synct
│   └── BauProjektManager/
│       ├── registry.json                      ← Generierter VBA-Export
│       ├── settings.json                      ← App-Einstellungen
│       ├── pattern-templates.json             ← Musterbibliothek
│       └── templates.json                     ← Vorlagen-Verzeichnis
│
├── Vorlagen/                                  ← Excel/Word Vorlagen
│   ├── Excel/
│   │   ├── Betontabelle_v3.xlsm
│   │   ├── Ziegeltabelle_v2.xlsm
│   │   ├── Bautagebuch_v1.xlsm
│   │   └── Stundenzettel_v1.xlsm
│   ├── Word/
│   │   ├── Bautagebuch_Vorlage.dotx
│   │   ├── Bauprotokoll.dotx
│   │   └── Briefkopf.dotx
│   └── BPM_Helper.xlam                       ← Excel Add-In (offen)
│
├── 202512_ÖWG-Dobl-Zwaring/                  ← Projektordner (SAUBER!)
│   ├── .bpm/                              ← Versteckter Ordner (ADR-046)
│   │   ├── manifest.json
│   │   ├── project.json
│   │   └── profiles/
│   ├── 01 Planunterlagen/                     ← Nummerierte Ordner
│   │   ├── _Eingang/                          ← Sammelordner
│   │   ├── Polierplan/
│   │   │   ├── TG/
│   │   │   │   ├── S-101-A_TG Bodenplatte.pdf
│   │   │   │   └── _Archiv/
│   │   │   ├── EG/
│   │   │   └── 1OG/
│   │   ├── Schalungsplan/
│   │   └── Bewehrungsplan/
│   ├── 02 Fotos/
│   ├── 03 Dokumente/
│   ├── 04 Protokolle/
│   ├── 05 Rechnungen/
│   ├── 06 Korrespondenz/
│   └── 07 Sonstiges/
│
├── 202302_Reihenhäuser-Kapfenberg/
│   ├── .bpm/
│   └── ...
│
└── BauProjektManager/                         ← Die App
    └── BauProjektManager.exe
```

**Ordner-Namenskonvention:** Nummerierte Präfixe mit Leerzeichen (z.B. "01 Planunterlagen"), konfigurierbar über Einstellungen. Analyse bestehender Projektordner ergab dieses Muster. Präfix-Schalter (an/aus) in den Einstellungen. (ADR-011)

### 2.5 Ordnerstruktur (Lokal)

```
%LocalAppData%\BauProjektManager/
├── bpm.db                                     ← Haupt-SQLite
├── Projects/
│   ├── 202512_OeWG-Dobl/
│   │   └── planmanager.db                     ← Cache, Journal, Undo
│   └── 202302_Kapfenberg/
│       └── planmanager.db
├── Logs/
│   ├── BPM_2026-03-26.log
│   └── BPM_2026-03-27.log
├── Thumbnails/                                ← Foto-Modul Cache
├── Tools/                                     ← FFmpeg etc.
├── Backups/
│   └── pre-import/
│       └── 2026-03-27_1432/
│           ├── bpm.db.bak
│           └── planmanager.db.bak
└── Temp/
```

### 2.6 Hidden-Attribute

| Ordner/Datei | Hidden? | Warum |
|---|---|---|
| `.AppData/` | Ja (Hidden + System) | Nicht für User sichtbar |
| `.bpm/` | Ja (Hidden) | Nicht für Kollegen sichtbar |
| `_Eingang/` | Nein | User muss hier Dateien reinwerfen |
| `_Archiv/` | Nein | User soll alte Pläne finden |

---

## 3. Zentrale Projekt-Registry

### 3.1 SQLite als System of Record

Alle Projektdaten in SQLite (`bpm.db`). Bei jeder Änderung wird `registry.json` automatisch als flacher Export generiert. VBA liest NUR den Export. (ADR-002, ADR-004, ADR-017)

### 3.2 Internes Domänenmodell (C#)

```csharp
public class Project
{
    public string Id { get; set; }                    // ULID (global eindeutig)
    public string ProjectNumber { get; set; }         // "202512" (aus Startdatum)
    public string Name { get; set; }                  // "ÖWG-Dobl-Zwaring"
    public string FullName { get; set; }              // "Gartensiedlung Dobl-Zwaring"
    public ProjectStatus Status { get; set; }         // Active, Completed, Archived
    public ProjectLocation Location { get; set; }
    public ProjectTimeline Timeline { get; set; }
    public Client Client { get; set; }                // Auftraggeber/Bauherr
    public List<Building> Buildings { get; set; }
    public ProjectPaths Paths { get; set; }
    public string Tags { get; set; }
    public string Notes { get; set; }
    
    public string FolderName => $"{ProjectNumber}_{Name}";
    
    public void UpdateProjectNumberFromStart()
    {
        if (Timeline.ProjectStart.HasValue)
            ProjectNumber = Timeline.ProjectStart.Value.ToString("yyyyMM");
    }
}

public class ProjectLocation
{
    // Adresse (aufgeteilt für Google Maps API — ADR-003)
    public string Street { get; set; }                // "Hauptstraße"
    public string HouseNumber { get; set; }           // "15"
    public string PostalCode { get; set; }            // "8143"
    public string City { get; set; }                  // "Dobl"
    // Verwaltung
    public string Municipality { get; set; }          // "Dobl-Zwaring"
    public string District { get; set; }              // "Graz-Umgebung"
    public string State { get; set; }                 // "Steiermark"
    // Koordinaten (für GIS-Integration vorbereitet)
    public string CoordinateSystem { get; set; }      // "EPSG:31258"
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }
    // Grundstück/Kataster
    public string CadastralKg { get; set; }           // "63201"
    public string CadastralKgName { get; set; }       // "Dobl"
    public string CadastralGst { get; set; }          // "123/1, 123/2, 124"
}

public class ProjectTimeline
{
    public DateTime? ProjectStart { get; set; }
    public DateTime? ConstructionStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualEnd { get; set; }
}

public class Building
{
    public string Id { get; set; }                    // ULID
    public string Name { get; set; }                  // "Haus Nr. 64"
    public string ShortName { get; set; }             // "H64"
    public string Type { get; set; }                  // "Reihenhaus"
    public List<string> Levels { get; set; }          // ["KG","EG","1.OG","2.OG","Dach"]
}

public class ProjectPaths
{
    public string Root { get; set; }                  // Absoluter Pfad (→ wird durch ADR-049 zu berechnetem Pfad)
    public string Plans { get; set; }                 // z.B. "01 Planunterlagen" (aus FolderTemplate, relativ zu Root)
    public string Inbox { get; set; }                 // z.B. "01 Planunterlagen\_Eingang"
    public string Photos { get; set; }                // z.B. "02 Fotos"
    public string Documents { get; set; }             // z.B. "03 Dokumente"
    public string Protocols { get; set; }             // z.B. "04 Protokolle"
    public string Invoices { get; set; }              // z.B. "05 Rechnungen"
}

public class Client
{
    public string Id { get; set; }                    // ULID
    public string Company { get; set; }               // "ÖWG Wohnbau"
    public string ContactPerson { get; set; }         // "Ing. Müller"
    public string Phone { get; set; }                 // "0316/12345"
    public string Email { get; set; }                 // "mueller@oewg.at"
    public string Notes { get; set; }
}

public enum ProjectStatus { Active, Completed }
```

- **ID-Schema (ADR-039 v2):** ULID als `TEXT PRIMARY KEY` für ALLE Tabellen. Global eindeutig, offline erzeugbar, keine `seq` Spalte. ID-Generierung über `IIdGenerator` (Domain) / `UlidIdGenerator` (Infrastructure).

**Building-Modell:** Aktuell minimal (Name, ShortName, Type, Levels). Wird später erweitert für Ziegelberechnung/Bauphysik (Geschoßhöhe, Wandstärke etc.) — Design dafür basierend auf realen Excel-Berechnungsblättern.

### 3.3 registry.json (Generierter Export — flach für VBA)

```json
{
  "registryVersion": "1.0",
  "generatedAt": "2026-03-27T10:00:00",
  "settings": {
    "basePath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit",
    "appDataPath": "...\\02Arbeit\\.AppData\\BauProjektManager",
    "templatesPath": "...\\02Arbeit\\Vorlagen",
    "folderTemplate": "{projectNumber}_{projectName}"
  },
  "planTypes": [
    "Polierplan", "Schalungsplan", "Bewehrungsplan", "Elektroplan",
    "HKLS-Plan", "Detailplan", "Architekturplan", "Lageplan",
    "Grundrissplan", "Schnittplan"
  ],
  "customPlanTypes": [],
  "projects": [
    {
      "id": "01HV8M2Q9AJ3W1XK7R4F5N6T8C",
      "projectNumber": "202512",
      "name": "ÖWG-Dobl-Zwaring",
      "fullName": "Gartensiedlung Dobl-Zwaring",
      "status": "active",
      "address": "Hauptstraße 15, 8143 Dobl-Zwaring",
      "municipality": "Dobl-Zwaring",
      "district": "Graz-Umgebung",
      "state": "Steiermark",
      "coordinateSystem": "EPSG:31258",
      "coordinateEast": 450123.45,
      "coordinateNorth": 5210678.90,
      "cadastralKg": "63201",
      "cadastralKgName": "Dobl",
      "cadastralGst": "123/1, 123/2, 124",
      "projectStart": "2024-01-15",
      "constructionStart": "2024-06-01",
      "plannedEnd": "2026-12-31",
      "actualEnd": null,
      "rootPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\202512_ÖWG-Dobl-Zwaring",
      "plansPath": "Pläne",
      "inboxPath": "Pläne\\_Eingang",
      "photosPath": "Fotos",
      "documentsPath": "Dokumente",
      "protocolsPath": "Protokolle",
      "invoicesPath": "Rechnungen",
      "buildings": "H64:Haus Nr. 64:Reihenhaus:KG,EG,1.OG,2.OG,Dach|H66:Haus Nr. 66:Reihenhaus:EG,OG,Dach",
      "tags": "Wohnbau, Reihenhäuser, ÖWGES",
      "notes": "Bauteil B-13, 3 Häuser"
    }
  ]
}
```

### 3.4 VBA-Kompatibilitäts-Regeln

| Regel | Grund |
|-------|-------|
| Keine verschachtelten Objekte im Export | VBA JSON-Parser sind einfach |
| Koordinaten als separate Felder | `coordinateEast` statt `coordinates.east` |
| Pfade relativ zu `rootPath` | VBA baut zusammen mit `rootPath & "\" & plansPath` |
| Buildings als Pipe-String | VBA `Split(buildings, "|")` → Array |
| Tags als Komma-String | VBA `Split(tags, ", ")` → Array |
| Datum als `YYYY-MM-DD` | VBA `CDate("2024-01-15")` |
| VBA liest NUR, schreibt NIE | C#-App ist einziger Writer (ADR-017) |

### 3.5 registry.json — Versionierter Exportvertrag

`registry.json` ist ein **versionierter Exportvertrag**, nicht nur ein Nebenprodukt. VBA-Makros in Outlook und Excel hängen von der Struktur ab.

**Regeln:**
- `registryVersion` Feld definiert das Exportschema (aktuell `"1.0"`)
- Neue Felder hinzufügen: erlaubt (Minor-Version, z.B. 1.0 → 1.1)
- Felder umbenennen/entfernen: **BREAKING CHANGE** (Major-Version, z.B. 1.0 → 2.0)
- Bei Major-Version: VBA-Makros müssen vor Rollout angepasst werden
- `registryVersion` und `schema_version` (DB) sind **unabhängig** — nicht jede DB-Schema-Änderung ändert den VBA-Exportvertrag
- Whitelist für Klasse-B-Felder: siehe [DSGVO-Architektur Kap. 9.3](DSVGO-Architektur.md)

### 3.6 .bpm/ Ordner (ADR-046)

Versteckter Ordner in jedem Projektordner — ersetzt die einzelne `.bpm-manifest`-Datei (ADR-013 v2):

```
Projektordner/
├── .bpm/                          ← Hidden
│   ├── manifest.json              ← Schlank: Identität + Module-Flags
│   ├── project.json               ← Vollständiger Projektexport
│   ├── profiles/                  ← PlanManager: Plantyp-Profile
│   │   └── <profilname>.json
│   └── plan-index.json            ← Bestandsmanifest (später)
```

**manifest.json** (schlank — nur Ausweis): projectId, projectNumber, name, updatedAtUtc, createdByMachine, modules (Flags welche Module aktiv sind).

**project.json** (Vollexport): Alle Projektdaten für Import/Übergabe. Keine DB-IDs, eigene DTOs. Wird bei jedem Speichern aktualisiert.

**profiles/**: Eine JSON-Datei pro Plantyp-Profil. Synct über Cloud-Speicher zwischen Geräten.

Bei Ordner-Umbenennung: BPM sucht über `.bpm/manifest.json` automatisch den neuen Pfad und aktualisiert die DB. Vorwärtsmigration: Alte `.bpm-manifest`-Datei wird automatisch in `.bpm/`-Ordner migriert.

### 3.6 Automatische Projektordner-Erstellung

Beim Anlegen eines neuen Projekts erstellt BPM automatisch die Ordnerstruktur:

- **FolderTemplateEntry-Modell:** Nummern aus Listenposition, nicht gespeichert
- **Nummerierte Ordner:** z.B. "01 Planunterlagen", "02 Fotos" (Leerzeichen, keine Unterstriche — ADR-011)
- **Optionale Unterordner:** z.B. `_Eingang` unter Planunterlagen
- **Präfix-Schalter:** An/Aus in den Einstellungen
- **Standard-Template:** In den Einstellungen konfigurierbar (Tab "Standard-Ordnerstruktur")
- **Live-Vorschau:** TreeView im ProjectEditDialog zeigt die geplante Struktur

---

## 4. PlanManager — Überblick

Der PlanManager ist das Kernfeature von BPM. Er sortiert Dokumente aus dem `_Eingang/`-Ordner
automatisch in die richtige Ordnerstruktur — mit Index-Versionierung, Undo-Journal, anlernbaren
Profilen und manuellem Sortier-Modus.

**Vollständige Dokumentation:** [PlanManager.md](../Module/PlanManager.md)

Dort dokumentiert:
- Workflow (5 Phasen), Import-Analyse, Entscheidungsmatrix (9 Status-Typen)
- Dateinamen-Parsing (Segment-basiert, ADR-022), Feldtypen, Ordner-Hierarchie
- Profil-System (RecognitionProfiles + PatternTemplates, ADR-010)
- IndexSource-Modell (FileName / None / PlanHeader, ADR-045)
- Undo-Journal (3 SQLite-Tabellen), Recovery, Preflight
- Manueller Sortier-Modus + Umbenennung
- Bestandsmanifest (`_plan_index.json`), Cache-Rebuild
- DB-Schema (planmanager.db, 6 Tabellen)
- Planlisten Import/Export (V1.1)
- Solution-Struktur + Implementierungsreihenfolge

---

## 5. Solution-Struktur (implementiert)

```
BauProjektManager.sln
│
├── src/
│   ├── BauProjektManager.App/                ← EXE (Shell, DI, MainWindow)
│   │   ├── App.xaml + App.xaml.cs             ← DI Setup, Startup
│   │   ├── MainWindow.xaml                    ← Shell + Navigation
│   │   └── BauProjektManager.App.csproj
│   │
│   ├── BauProjektManager.Domain/             ← Modelle, Enums
│   │   ├── Models/
│   │   │   ├── Project.cs                     ← ✅ Implementiert
│   │   │   ├── ProjectLocation.cs             ← ✅ Implementiert
│   │   │   ├── ProjectTimeline.cs             ← ✅ Implementiert
│   │   │   ├── ProjectPaths.cs                ← ✅ Implementiert
│   │   │   ├── Building.cs                    ← ✅ (Legacy, noch vorhanden)
│   │   │   ├── BuildingPart.cs                ← ✅ Implementiert (v0.13.1)
│   │   │   ├── BuildingLevel.cs               ← ✅ Implementiert (v0.13.1)
│   │   │   ├── ProjectParticipant.cs          ← ✅ Implementiert (v0.14.0)
│   │   │   ├── ProjectLink.cs                 ← ✅ Implementiert (v0.15.0)
│   │   │   ├── Client.cs                      ← ✅ Implementiert
│   │   │   └── AppSettings.cs                 ← ✅ (ProjectTypes, BuildingTypes, LevelNames, ParticipantRoles, PortalTypes, FolderTemplate)
│   │   ├── Enums/
│   │   │   ├── ProjectStatus.cs               ← ✅ Implementiert
│   │   │   └── DataClassification.cs          ← ⬜ Geplant (ClassA/B/C, ADR-035)
│   │   ├── IIdGenerator.cs                    ← ⬜ ADR-039 v2
│   │   │   ├── IDialogService.cs              ← ✅ Info/Warn/Error/Confirm Dialoge
    │   │   ├── Privacy/
    │   │   │   └── IPrivacyPolicy.cs              ← ⬜ Geplant (ADR-036)│   │   └── BauProjektManager.Domain.csproj    ← KEINE Abhängigkeiten
│   │
│   ├── BauProjektManager.Infrastructure/     ← Technische Umsetzung
│   │   ├── Persistence/
│   │   │   ├── ProjectDatabase.cs             ← ✅ SQLite CRUD Schema v1.5 (seq+Präfix) — ⬜ Migration auf v2.0 ULID ausstehend (ADR-039 v2)
│   │   │   ├── AppSettingsService.cs           ← ✅ settings.json laden/speichern
│   │   │   ├── RegistryJsonExporter.cs        ← ✅ SQLite → JSON Export
│   │   │   ├── ProjectFolderService.cs        ← ✅ Ordner erstellen
│   │   │   ├── ManifestService.cs             ← ⬜ .bpm/manifest.json (schlank, ADR-046)
│   │   │   └── ProjectExportService.cs        ← ⬜ .bpm/project.json (Vollexport, ADR-046)
    │   │   ├── UlidIdGenerator.cs                  ← ⬜ ADR-039 v2 (NuGet: Cysharp/Ulid)
    │   │   ├── Communication/                      ← ⬜ Geplant (vor erstem Online-Modul)
    │   │   │   ├── ExternalCommunicationService.cs ← ⬜ IExternalCommunicationService (ADR-035)
│   │   │   ├── RelaxedPrivacyPolicy.cs        ← ⬜ Interner Modus (ADR-036)
│   │   │   └── StrictPrivacyPolicy.cs         ← ⬜ Kommerzieller Modus (ADR-036)
│   │   └── BauProjektManager.Infrastructure.csproj
│   │
│   ├── BauProjektManager.Settings/           ← ✅ Einstellungen Feature
│   │   ├── ViewModels/
│   │   │   ├── SettingsViewModel.cs           ← ✅ 2-Tab-Seite, IDialogService, Suche+Filter, Import
│   │   │   └── ProjectEditViewModel.cs        ← ✅ 2-Spalten-Dialog
│   │   ├── Views/
│   │   │   ├── SettingsView.xaml              ← ✅ Suchfeld, Statusfilter, Popup-Button, Pfade
│   │   │   ├── FolderTemplateControl.xaml     ← ✅ Shared Ordner-TreeView
│   │   │   └── ProjectEditDialog.xaml         ← ✅ 5-Tab-Dialog
│   │   └── BauProjektManager.Settings.csproj
│   │
│   └── BauProjektManager.PlanManager/        ← ⬜ PlanManager Feature (nächste Phase)
│       ├── ViewModels/
│       ├── Views/
│       ├── Services/
│       └── BauProjektManager.PlanManager.csproj
│
├── Tools/
│   └── Get-ProjektOrdner.ps1                  ← ✅ PowerShell Analyse-Tool
│
└── Docs/
    ├── Kern/                                  ← Bei JEDER Code-Änderung relevant
    │   ├── BauProjektManager_Architektur.md   ← DIESES DOKUMENT
    │   ├── DB-SCHEMA.md                       ← ✅ Schema v2.0 (ULID)
    │   ├── CODING_STANDARDS.md                ← ✅ Code-Richtlinien + Datenschutz (Kap. 17)
    │   ├── DSVGO-Architektur.md               ← ✅ Privacy Engineering, IPrivacyPolicy
    │   └── BACKLOG.md                         ← ✅ Feature-Liste
    ├── Referenz/                              ← Lesen wenn Thema aufkommt
    │   ├── ADR.md                             ← ✅ 49 Entscheidungen
    │   ├── VISION.md                          ← ✅ Nordstern
    │   ├── DEPENDENCY-MAP.md                  ← ✅ Ökosystem
    │   ├── UI_UX_Guidelines.md                ← ✅ Design-System
    │   ├── WPF_UI_Architecture.md             ← ✅ Theme, Tokens
    │   ├── CHANGELOG.md                       ← ✅ Versionshistorie
    │   ├── GLOSSAR.md                         ← ✅ Bau-Begriffe
    │   └── UX_Flows.md                        ← ✅ User Workflows
    └── Konzepte/                              ← Erst relevant wenn Modul gebaut wird
        ├── ModuleKiAssistent.md
        ├── ModuleZeiterfassung.md
        └── ... (13 weitere)
```

**Dependency-Regel (eisern):**
```
Domain          → NICHTS
Infrastructure  → nur Domain
PlanManager     → Domain + Infrastructure
Settings        → Domain + Infrastructure
App             → alles (DI verdrahtet hier)
```

Detailliertes Dependency-Diagramm: siehe [DEPENDENCY-MAP.md](DEPENDENCY-MAP.md).

---

## 6. Technische Entscheidungen

Vollständige Liste aller 42 Architekturentscheidungen mit Kontext, Alternativen und Konsequenzen: siehe [ADR.md](../Referenz/ADR.md).

Zusammenfassung der wichtigsten Entscheidungen:

| Thema | Entscheidung | ADR |
|-------|-------------|-----|
| **Sprache** | C# (.NET 10 LTS) | ADR-003 |
| **GUI** | WPF (XAML) | ADR-001 |
| **Architektur** | Modularer Monolith (feste Registrierung) | ADR-001 |
| **MVVM** | CommunityToolkit.Mvvm | ADR-015 |
| **Logging** | Serilog (File + Console) | ADR-015 |
| **System of Record** | SQLite (`bpm.db`) | ADR-002 |
| **VBA-Export** | registry.json (automatisch, read-only) | ADR-004, ADR-017 |
| **ID-Schema** | ULID als TEXT PRIMARY KEY für alle Tabellen | ADR-039 |
| **Plan-Dateien** | 1..n pro Revision | ADR-007 |
| **Import** | 10-Schritte-Workflow | ADR-008 |
| **Undo** | 3 SQLite-Tabellen (Journal) | ADR-009 |
| **Profile** | RecognitionProfiles + PatternTemplates | ADR-010 |
| **Ordner-Naming** | Nummerierte Präfixe mit Leerzeichen | ADR-011 |
| **Manifest** | .bpm/ Ordner — Manifest-Split + Profilablage | ADR-046 |
| **C# statt PowerShell** | Für Hauptapp | ADR-014 |
| **Zeiterfassung** | WPF + ClosedXML → Excel | ADR-018 |
| **Mobile** | PWA, deferred | ADR-019 |
| **Multi-User** | Write-Lock mit Heartbeat | ADR-020 |
| **Client** | Eigene Entität (nicht nur String) | ADR-021 |
| **Segment-Parsing** | Trennzeichen-basiert | ADR-022 |
| **Adressbuch** | Getrennt von Projekt-Beteiligten | ADR-024 |
| **Status** | Nur Active + Completed | ADR-025 |
| **Portal-Typen** | Editierbare Liste | ADR-026 |
| **KI-API-Import** | ChatGPT/Claude für Datenextraktion | ADR-027 |
| **Datenschutz** | IExternalCommunicationService als zentrales Privacy Gate | ADR-035 |
| **Privacy Policy** | IPrivacyPolicy austauschbar (Relaxed/Strict), Lizenz-gesteuert | ADR-036 |
| **ID-Schema** | ULID als TEXT PRIMARY KEY, global eindeutig, offline erzeugbar | ADR-039 |
| **Ansichtsprofile** | ViewProfiles als UI-Sichtschicht über Modul-Aktivierung, Resolver-basiert | ADR-048 |
| **Pfad-Resolution** | Relativer folder_name + Manifest-Fallback (Option C) — kein absoluter root_path in DB | ADR-049 |

---

## 7. Betriebsmodi

| Modus | Beschreibung | RBAC | Datenschutz-Besonderheiten |
|-------|-------------|------|---------------------------|
| **A — Solo/Cloud-Sync** | Ein User, Files über Cloud-Speicher | Nein | Standard-Regeln |
| **B — Team (Vertrauensbasis)** | Write-Lock, JSON-Event-Sync | Nein | project_shares, Sync-Whitelist (DSGVO-Architektur Kap. 9.3) |
| **C — Server/API** | REST-API, mehrere Clients | Ja (PFLICHT) | Rollenbasierte Datenfilterung, Klasse-C nie über API |

**Aktuell:** Modus A. Details: siehe [MultiUserKonzept.md](../Konzepte/MultiUserKonzept.md) und ADR-033.

---

## 8. V1-Scope & Roadmap

### V1 Phase 1 — Einstellungen (✅ größtenteils erledigt)

| # | Feature | Status |
|---|---------|--------|
| 1 | App-Shell + Navigation | ✅ |
| 2 | Serilog Logging | ✅ |
| 3 | Domain-Modelle | ✅ |
| 4 | Projektliste + Dialog | ✅ |
| 5 | SQLite-Datenbank | ✅ |
| 6 | Auto-Increment IDs | ✅ |
| 7 | registry.json Export | ✅ |
| 8 | Git-Aufräumung | ✅ |
| 9 | Ersteinrichtung | ✅ |
| 10 | Projektordner erstellen | ✅ |
| 11 | .bpm/ Ordner (ADR-046) | ⬜ |
| 12 | Projekt archivieren (Status → Completed, Ordner → ArchivePath, Pfad-Resolution via Option C) | ⬜ |
| 13 | Pfade änderbar | ✅ |
| 14 | Single-Writer Mutex | ⬜ |

### V1 Phase 2 — PlanManager (⬜ nächste Phase)

Features #18–#34, Details siehe [BACKLOG.md](BACKLOG.md).

### V1.1 — Planlisten

Planlisten Import/Export, Adressbuch, Schnellsuche. Details siehe [BACKLOG.md](BACKLOG.md).

### Nach V1 — Module (Prio-Reihenfolge)

1. Foto-Management (PhotoFolder PS-Code existiert)
2. Zeiterfassung (Konzept steht)
3. Bautagebuch
4. Dashboard
5. Outlook COM
6. Plankopf-Extraktion
7. GIS-Integration
8. Wetter API
9. Mobile PWA
10. Vorlagen
11. KI-Assistent (LV-Analyse, Dokumentensuche)

Detaillierte Feature-Liste mit Status: siehe [BACKLOG.md](BACKLOG.md).

---

## 9. Coding Standards + Definition of Done

### V1-Pflicht Standards

| Standard | Umsetzung |
|----------|-----------|
| Nullable Reference Types | `<Nullable>enable</Nullable>` in allen .csproj |
| `.editorconfig` | Einmal erstellen, erzwingt einheitliche Formatierung |
| Logging | Serilog, Structured Logging, `{PropertyName}` Platzhalter |
| CancellationToken | Alle async Methoden bekommen CancellationToken |
| Verbot `async void` | Nur erlaubt für UI Event-Handler |
| IDisposable | `using`-Statement Pflicht |
| Single-Writer | Mutex beim App-Start — nur eine Instanz läuft |
| Schema-Version | `"schemaVersion": "1.0"` in jeder DB und JSON |
| Backup vor Import | SQLite-DB + JSON als .bak kopieren |
| Atomische JSON-Writes | Write-to-temp-then-rename |

Detaillierte Coding Standards: siehe [CODING_STANDARDS.md](CODING_STANDARDS.md).

### Definition of Done (V1)

```
Bevor ein Feature als "fertig" gilt:
☐ Code kompiliert ohne Fehler und Warnungen
☐ Manuelle Tests durchgeführt (Happy Path + ein Fehlerfall)
☐ Logging vorhanden (Info für Hauptaktionen, Error für Fehler)
☐ Nullable Warnings aufgelöst
☐ Kein offenes TODO oder HACK (oder bewusst dokumentiert warum)
☐ Git Commit mit korrektem Format ([vX.Y.Z] Modul, Typ: Titel)
```

---

## 10. Alle Config-Dateien — Übersicht

| Datei | Ort | Format | Zweck | Erstellt von |
|-------|-----|--------|-------|-------------|
| `registry.json` | Cloud-Speicher `.AppData/` | JSON | VBA-Export (generiert) | C#-App (auto) |
| `settings.json` | Cloud-Speicher `.AppData/` | JSON | App-Einstellungen | C#-App |
| `pattern-templates.json` | Cloud-Speicher `.AppData/` | JSON | Musterbibliothek | C#-App |
| `templates.json` | Cloud-Speicher `.AppData/` | JSON | Vorlagen-Verzeichnis | C#-App |
| ~~`profiles.json`~~ | ~~Cloud-Speicher `.AppData/Projects/<P>/`~~ | — | Wandert nach `.bpm/profiles/` (ADR-046) | — |
| `bpm.db` | Lokal | SQLite | Haupt-DB (Projekte) | C#-App |
| `planmanager.db` | Lokal `Projects/<P>/` | SQLite | Cache, Journal, Undo | C#-App |
| `.bpm/manifest.json` | Cloud-Speicher Projektordner | JSON | Projekt-Identität | C#-App |
| `.bpm/project.json` | Cloud-Speicher Projektordner | JSON | Vollständiger Projektexport | C#-App |
| `.bpm/profiles/*.json` | Cloud-Speicher Projektordner | JSON | Plantyp-Profile | C#-App |

---

*Dokument Version 3.0.0 — 11.04.2026*

*Kernänderungen v2.1.0 → v2.2.0:*
- *Kapitel 2.4: .bpm-manifest → .bpm/ Ordner in Projektordner-Diagramm*
- *Kapitel 2.6: Hidden-Attribute Tabelle: .bpm/ statt .bpm-manifest*
- *Kapitel 3.1: Daten-Übersicht Tabelle: .bpm/ Ordner statt .bpm-manifest*
- *Kapitel 3.6: Komplett neu — .bpm/ Ordner mit manifest.json, project.json, profiles/ (ADR-046)*
- *Kapitel 10: Solution-Struktur: ManifestService + ProjectExportService statt BpmManifestService*
- *Kapitel 11: ADR-046 (.bpm/ Ordner) in Referenztabelle*
- *Kapitel 12: Feature #11 umbenannt*
- *Kapitel 14: Config-Übersicht: 3 Zeilen für .bpm/ statt 1 für .bpm-manifest; profiles.json wandert aus .AppData nach .bpm/profiles/*

*Kernänderungen v2.0.0 → v2.1.0:*
- *Kapitel 3.5 NEU: registry.json als versionierter Exportvertrag (registryVersion unabhängig von schema_version, Whitelist-Verweis auf DSGVO-Architektur)*
- *Kapitel 3.6: .bpm-manifest (umbenannt von 3.5) — in v2.2.0 durch .bpm/ Ordner ersetzt (ADR-046)*
- *Kapitel 10: Privacy Control Layer in Solution-Struktur (Domain/Enums/DataClassification, Domain/Privacy/IPrivacyPolicy, Infrastructure/Communication/)*
- *Kapitel 11: ADR-039 (ID-Schema) ergänzt*
- *Kapitel 12 NEU: Betriebsmodi A/B/C (Solo, Team, Server)*
- *Kapitel 13: V1-Scope & Roadmap (umbenannt von 12)*

*Kernänderungen gegenüber v1.4:*
- *Header: Verwandte Dokumente (ADR, Vision, Dependency Map, Changelog, BACKLOG) verlinkt*
- *Kapitel 1.3: Modul-Tabelle aktualisiert — Prio-Reihenfolge, Konzept-Dok-Verweise auf Docs/Konzepte/*
- *Kapitel 2.4: Ordnerstruktur mit nummerierten Präfixen ("01 Planunterlagen") statt unnummeriert*
- *Kapitel 2.5: Thumbnails/ und Tools/ Ordner in lokaler Struktur ergänzt*
- *Kapitel 3.2: ID-Schema auf auto-increment (proj_001) aktualisiert, Building-Erweiterung notiert*
- *Kapitel 3.6: Neuer Abschnitt — Automatische Projektordner-Erstellung (FolderTemplateEntry, TreeView)*
- *Kapitel 9: GUI-Mockups zusammengefasst statt wiederholt (v1.4 Mockups bleiben gültig)*
- *Kapitel 10: Solution-Struktur mit ✅/⬜ Status pro Datei, Docs-Ordner mit neuen Docs*
- *Kapitel 11: Technische Entscheidungen verweisen auf ADR.md statt alles zu wiederholen*
- *Kapitel 12: Roadmap mit ✅ Status für erledigte Features, Nach-V1 Prio-Liste*
- *Kapitel 17: Offene Punkte entfernt (teilweise erledigt, Rest in BACKLOG)*
- *Fußtext: .NET 10 LTS (nicht mehr .NET 8), korrekte Änderungsliste*

*Kernänderungen v1.5.0 → v2.0.0:*
- *Kapitel 1.3: KI-Assistent Modul hinzugefügt*
- *Kapitel 3.2: ProjectStatus vereinfacht (Active + Completed, kein Archived)*
- *Kapitel 10: Neue Domain-Models (BuildingPart, BuildingLevel, ProjectParticipant, ProjectLink), DB Schema v1.5*
- *Kapitel 10: Konzepte/ModuleKiAssistent.md hinzugefügt*
- *Kapitel 11: ADR-024 bis ADR-027 verlinkt*
- *Kapitel 12: KI-Assistent in Nach-V1 Prio-Liste*

*Kernänderungen v2.2.0 → v3.0.0 (Docs-Refactoring auf Quickload):*
- *Frontmatter + AI-Quickload-Block ergänzt (DOC-STANDARD.md)*
- *Kapitel 4–8 (PlanManager-Fachlogik) ausgelagert → PlanManager.md (Inhalt war dort bereits vollständig)*
- *Kapitel 9 (GUI-Mockups) entfernt — Verweis auf PlanManager.md Kap. 15*
- *Kapitel 4 neu: PlanManager — Überblick (Kurzverweis auf PlanManager.md)*
- *Doppelte Kapitelnummer 13 aufgelöst*
- *Kapitelnummern neu: 5=Solution, 6=Entscheidungen, 7=Betriebsmodi, 8=Roadmap, 9=Coding Standards, 10=Config-Dateien*
- *Planlisten-Details (Spalten-Zuordnung, Abgleich-Ergebnis, Export) nach PlanManager.md Kap. 19.1 ergänzt*
