---
doc_id: dependency-map
doc_type: reference
authority: secondary
status: active
owner: herbert
topics: [dependencies, solution-struktur, nuget, vba, ökosystem, datenfluss]
read_when: [neue-abhängigkeit, neues-nuget-paket, vba-integration, solution-struktur-prüfen]
related_docs: [architektur, db-schema]
related_code: [src/BauProjektManager.App/BauProjektManager.App.csproj, src/BauProjektManager.Infrastructure/BauProjektManager.Infrastructure.csproj]
supersedes: []
---

## AI-Quickload
- Zweck: Solution-Abhängigkeiten, externes Ökosystem (VBA/PowerShell) und NuGet-Pakete
- Autorität: secondary (Architektur.md ist source_of_truth für Solution-Struktur)
- Lesen wenn: Neue Abhängigkeit, neues NuGet-Paket, VBA-Integration, Solution-Struktur prüfen
- Nicht zuständig für: Architektur-Entscheidungen (→ ADR.md), Schichtgrenzen (→ Architektur.md)
- Kapitel:
  - 1. Interne Solution-Struktur
  - 2. Externes Ökosystem
  - 3. Technologie-Abhängigkeiten
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Dependency-Regel: Domain→NICHTS, Infrastructure→nur Domain, App→alles
  - Keine neuen NuGet-Pakete ohne Freigabe

---

﻿# BauProjektManager — Dependency Map

**Erstellt:** 29.03.2026  
**Aktualisiert:** 08.04.2026  
**Version:** 2.3  
**Basis:** Solution v0.24.14, DB-Schema v2.0, ADR v1.2 (49 ADRs)

---

## 1. Interne Solution-Struktur

### 1.1 Dependency-Regel (eisern)

```
Domain          → referenziert NICHTS
Infrastructure  → referenziert nur Domain
PlanManager     → referenziert Domain + Infrastructure
Settings        → referenziert Domain + Infrastructure
App             → referenziert alles (DI verdrahtet hier)
```

### 1.2 Schichtdiagramm

```
┌─────────────────────────────────────────────┐
│              BauProjektManager.App           │  EXE — Shell, DI Setup
│              (referenziert alles)             │  MainWindow, Navigation
└──────────┬──────────────┬────────────────────┘
           │              │
     ┌─────▼─────┐  ┌─────▼──────┐
     │  Settings  │  │ PlanManager │  Feature-Module (WPF Class Libraries)
     │            │  │             │  ViewModels, Views, Services
     ┌─────┬──┬──┘  └──┬──┬──────┘
           │  │        │  │
           │  └───┬────┘  │
           │      │       │
      ┌────▼──────▼───┐   │
      │ Infrastructure │   │  Technische Umsetzung
      │  SQLite, JSON  │   │  FileSystem, Logging
      │  Serilog       │   │
      │  ──────────────│   │
      │  Privacy Layer │   │  IExternalCommunicationService (ADR-035)
      │  IPrivacyPolicy│   │  RelaxedPolicy / StrictPolicy (ADR-036)
      └───────┬────────┘   │              │            │
         ┌────▼────────────▼──┐
         │      Domain         │  Fachmodell
         │  Modelle, Interfaces │  KEINE Abhängigkeiten
         │  Enums              │
         └─────────────────────┘
```

### 1.3 Projekte im Detail

| Projekt | Typ | NuGet-Pakete | Verantwortung |
|---------|-----|-------------|---------------|
| **App** | WPF EXE | Microsoft.Extensions.DI | Shell, MainWindow, DI-Container, App.xaml, Themes/ (8 Resource Dictionaries inkl. Icons.xaml) |
| **Domain** | Class Library | *keine* | Project, Client, BuildingPart, BuildingLevel, ProjectParticipant, ProjectLink, Location, Timeline, AppSettings (ProjectTypes, BuildingTypes, LevelNames, ParticipantRoles, PortalTypes, FolderTemplate), BpmManifest (+ ManifestClient, ManifestLocation, ManifestTimeline etc.), ManifestV2 (schlank, ADR-046), Enums (inkl. DataClassification, AccessLevel), Interfaces (inkl. IDialogService ✅, IIdGenerator, IPrivacyPolicy, IAccessControlService ⬜, IProjectDataService ⬜, ISyncTransport ⬜, ITaskManagementService ⬜, SyncEnvelope ⬜) — ⬜ = geplant, noch nicht implementiert |
| **Infrastructure** | Class Library | Microsoft.Data.Sqlite, Serilog, System.Text.Json, Ulid (Cysharp) | ProjectDatabase (Schema v2.0 ULID: projects, clients, building_parts, building_levels, project_participants, project_links), RegistryJsonExporter, AppSettingsService, ProjectFolderService, SerilogSetup, UlidIdGenerator (ADR-039 v2), ExternalCommunicationService (ADR-035), RelaxedPrivacyPolicy + StrictPrivacyPolicy (ADR-036). BpmManifestService (Read/Write/ScanFolder, Hidden+ReadOnly Attribute) → wird aufgeteilt in ManifestService (.bpm/manifest.json, schlank) + ProjectExportService (.bpm/project.json, Vollexport) per ADR-046. **Geplant:** SecretStore (ADR-042), StartupHealthCheck (ADR-041), Communication/ Ordner. Für vollständige Tabellenliste (implementiert + geplant) siehe [DB-SCHEMA.md](../Kern/DB-SCHEMA.md). |
| **Settings** | WPF Class Library | CommunityToolkit.Mvvm | SettingsViewModel (IDialogService, CollectionView-Filter, Projekt-Import), ProjectEditViewModel, SettingsView.xaml (Suchfeld, Statusfilter, Popup-Button), ProjectEditDialog.xaml, FolderTemplateControl.xaml |
| **PlanManager** | WPF Class Library | CommunityToolkit.Mvvm | FileNameParser, ProfileWizardViewModel/Dialog, PlanManagerViewModel, ProjectDetailViewModel, ProfileManager (.bpm/profiles/ lesen/schreiben, ADR-046) |

### 1.4 Zukünftige Module (geplant)

Jedes wird ein eigenes WPF Class Library Projekt, referenziert Domain + Infrastructure:

- `BauProjektManager.Dashboard` — Zentrale Übersicht, Widgets
- `BauProjektManager.Bautagebuch` — Tägliches Protokoll, Export
- `BauProjektManager.Zeiterfassung` — WPF-Maske → Excel via ClosedXML
- `BauProjektManager.Foto` — Viewer, Tags, Geodaten
- `BauProjektManager.Outlook` — COM Interop, Anhänge
- `BauProjektManager.Wetter` — API pro Baustelle
- `BauProjektManager.KiAssistent` — LV-Analyse, Dokumentensuche, ChatGPT/Claude API

---

## 2. Externes Ökosystem

### 2.1 Datenfluss-Übersicht

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│    ┌──────────┐          ┌─────────────────────┐                │
│    │ SQLite   │◄────────►│  BauProjektManager  │                │
│    │ bpm.db   │ lesen +  │  Desktop (.exe)     │                │
│    │ (lokal)  │ schreiben│                     │                │
│    └──────────┘          └──┬──┬──┬──┬──┬──────┘                │
│                             │  │  │  │  │                       │
│              ┌──────────────┘  │  │  │  └──────────────┐        │
│              │                 │  │  │                  │        │
│              ▼                 │  │  │                  ▼        │
│    ┌──────────────┐           │  │  │        ┌──────────────┐   │
│    │ registry.json │           │  │  │        │ Projektordner│   │
│    │ (generiert)   │           │  │  │        │ Cloud-Ordner │   │
│    └──────┬───────┘           │  │  │        └──────┬───────┘   │
│           │                   │  │  │               │           │
│     ┌─────▼─────┐            │  │  │        ┌──────▼──────┐    │
│     │ VBA-Makros │            │  │  │        │  Kollegen   │    │
│     │ Outlook    │            │  │  │        │  (Explorer) │    │
│     │ Excel      │            │  │  │        └─────────────┘    │
│     │ PhotoFolder│            │  │  │                           │
│     └────────────┘            │  │  │                           │
│                               │  │  │                           │
│              ┌────────────────┘  │  └───────────────┐           │
│              ▼                   ▼                   ▼           │
│    ┌──────────────┐   ┌──────────────┐   ┌──────────────┐      │
│    │ Cloud-Sync    │   │ Excel        │   │ settings.json│      │
│    │ .AppData/    │   │ (Zeiten)     │   │ profiles.json│      │
│    │ Sync PC↔Lap.│   │ via ClosedXML│   │ templates.json│     │
│    └──────────────┘   └──────┬───────┘   └──────────────┘      │
│                              │                                  │
│                       ┌──────▼───────┐                          │
│                       │  Lohnbüro    │                          │
│                       │  (liest Excel│                          │
│                       │   OneDrive)  │                          │
│                       └──────────────┘                          │
│                                                                 │
│    - - - - - - - - GEPLANT - - - - - - - -                     │
│                                                                 │
│    ┌──────────────┐   ┌──────────────┐   ┌──────────────┐      │
│    │ Outlook COM  │   │ Mobile PWA   │   │ KI-API       │      │
│    │ (Anhänge →   │   │ (Graph API   │   │ (ChatGPT /   │      │
│    │  _Eingang)   │   │  oder LAN)   │   │  Claude)     │      │
│    └──────────────┘   └──────────────┘   └──────────────┘      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Datei-Matrix: Wer schreibt, wer liest

| Datei / System | Format | Schreiber | Leser | Speicherort |
|---------------|--------|-----------|-------|-------------|
| `bpm.db` | SQLite | C#-App | C#-App | Lokal (%LocalAppData%) |
| `planmanager.db` | SQLite | C#-App | C#-App | Lokal (pro Projekt) |
| `registry.json` | JSON | C#-App (auto) | VBA, PhotoFolder | Cloud-Speicher .AppData/ |
| `settings.json` | JSON | C#-App | C#-App | Cloud-Speicher .AppData/ |
| ~~`profiles.json`~~ | — | — | — | → `.bpm/profiles/*.json` im Projektordner (ADR-046) |
| `pattern-templates.json` | JSON | C#-App | C#-App | Cloud-Speicher .AppData/ |
| `.bpm/manifest.json` | JSON | C#-App (ManifestService) | C#-App | Cloud-Speicher Projektordner `.bpm/` (ADR-046) |
| `.bpm/project.json` | JSON | C#-App (ProjectExportService) | C#-App | Cloud-Speicher Projektordner `.bpm/` (ADR-046) |
| `.bpm/profiles/*.json` | JSON | C#-App (ProfileManager) | C#-App | Cloud-Speicher Projektordner `.bpm/` (ADR-046) |
| Projektordner | Dateien | C#-App, User | Alle, Kollegen | Cloud-Speicher |
| Excel (Zeiten) | .xlsx | C#-App (ClosedXML) | Lohnbüro, Excel | Cloud-Speicher |
| Outlook-Ordner | — | VBA (aktuell) | User | Outlook |
| Logs | .log | Serilog | Entwickler | Lokal Logs/ |

### 2.3 Sync-Übersicht: Was syncht, was nicht

| Kategorie | Speicherort | Syncht über Cloud-Speicher? |
|-----------|-------------|----------------------------|
| Nutzdaten (Pläne, Fotos) | Cloud-Speicher Projektordner | ✅ Ja |
| Konfiguration (JSON) | Cloud-Speicher .AppData/ | ✅ Ja |
| Operativer State (SQLite) | %LocalAppData% | ❌ Nein |
| Logs | %LocalAppData%/Logs | ❌ Nein |
| Backups | %LocalAppData%/Backups | ❌ Nein |

---

## 3. Technologie-Abhängigkeiten

### 3.1 NuGet-Pakete

| Paket | Zweck | Verwendet in |
|-------|-------|-------------|
| CommunityToolkit.Mvvm | MVVM Boilerplate | Settings, PlanManager |
| Microsoft.Data.Sqlite | SQLite-Zugriff | Infrastructure |
| Microsoft.Extensions.DependencyInjection | DI-Container | App |
| Serilog + Serilog.Sinks.File + Serilog.Sinks.Console | Logging | Infrastructure, App |
| System.Text.Json | JSON Serialisierung | Infrastructure |
| Ulid (Cysharp) | ULID-Generierung (ADR-039 v2) | Infrastructure |
| ClosedXML | Excel lesen/schreiben | Infrastructure (Zeiterfassung) |
| QuestPDF | PDF-Export | Infrastructure (Planlisten, Bautagebuch) |
| PdfPig | PDF-Parsing | Infrastructure (Planlisten-Import) |

### 3.2 Externe Abhängigkeiten (Laufzeit)

| System | Abhängigkeit | Wann nötig |
|--------|-------------|-----------|
| .NET 10 Runtime | Pflicht | Immer (oder self-contained .exe) |
| Cloud-Speicher | Empfohlen | Für Multi-Device-Sync (OneDrive, Google Drive, Dropbox, Synology Drive, Nextcloud etc.) |
| Microsoft Excel | Optional | Nur für Zeiterfassung (ClosedXML braucht kein Excel) |
| Microsoft Outlook | Optional | Nur für Outlook-COM-Modul (nach V1) |
| Microsoft Word | Optional | Nur für Vorlagen-Modul (nach V1) |
| Internet | Optional | Für Wetter-API, Mobile PWA und KI-API (ChatGPT/Claude) |

---

*Dieses Dokument wird bei neuen Modulen oder Abhängigkeiten aktualisiert.*
