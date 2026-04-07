# CLAUDE.md — BauProjektManager

## Projekt

BauProjektManager (BPM) — modulare WPF Desktop-App für österreichische
Baustellen-Manager (Poliere, Bauleiter). Offline-first, Cloud-Speicher-neutral.

- **Version:** 0.16.1
- **Stack:** C# / .NET 10 LTS, WPF, SQLite, CommunityToolkit.Mvvm, Serilog, ClosedXML
- **Architektur:** Modularer Monolith — Single EXE, Module als separate DLLs

## Solution-Struktur

```
src/
├── BauProjektManager.App/          ← WPF Shell, Themes, Dialoge
├── BauProjektManager.Domain/       ← Models, Interfaces, Enums (KEINE Abhängigkeiten)
├── BauProjektManager.Infrastructure/ ← SQLite, Services, Persistence
├── BauProjektManager.PlanManager/  ← PlanManager Modul (V1 Kernfeature)
└── BauProjektManager.Settings/     ← Einstellungen Modul
```

## Coding Standards (Kurzversion)

### Naming
- Klassen/Methoden/Properties: PascalCase
- Private Felder: _camelCase
- Parameter/Lokale: camelCase
- Interfaces: IName
- Async-Methoden: ...Async Suffix
- Booleans: Verb-Präfix (IsVisible, HasChanges, CanSave)

### Struktur
- Eine Klasse pro Datei, max 300-400 Zeilen
- Methoden max 30-40 Zeilen, max 4-5 Parameter
- Reihenfolge: Fields → Constructor → Properties → Public Methods → Private Methods
- Allman-Style Braces, 4 Spaces Indent, max 120 Zeichen/Zeile

### Nullable & Error Handling
- Nullable Reference Types AKTIV (in Directory.Build.props)
- Null explizit behandeln, kein null-forgiving (!) ohne Grund
- Spezifische Exceptions, nie leere catch-Blöcke, throw statt throw ex

### MVVM
- CommunityToolkit.Mvvm: ObservableObject, RelayCommand
- View: XAML only, minimaler Code-Behind
- ViewModel: Keine Business-Logik, nur UI-State + Commands
- Model: Pure Data, keine UI-Abhängigkeiten

### XAML / UI
- KEINE hardcoded Colors — nur Token-Referenzen (BpmPrimary, BpmSurface, BpmText)
- KEINE hardcoded FontSize — Theme-Tokens verwenden
- Deutsche UI-Labels
- Theme-System in src/BauProjektManager.App/Themes/

### DI / Services
- Constructor Injection, keine new-Instanzen für Services
- Interface + Implementation Pattern
- DI-Setup in App.xaml.cs

### Datenbank
- SQLite als System of Record
- Parametrisierte Queries, NIE String-Building
- ID-Schema: seq INTEGER PK + TEXT-ID mit Präfix (z.B. PRJ-20250328-001)
- IDisposable für Connections

### Logging
- Serilog, KEINE Console.WriteLine
- KEINE Personendaten in Logs — nur IDs
- Structured Logging: Log.Information("Project {ProjectId}", id)

### DSGVO
- Alle externen HTTP-Calls über IExternalCommunicationService
- DataClassification Enum bei jedem externen Call
- DPAPI für API-Keys
- IPrivacyPolicy über DI gesteuert

## Git

- Commit-Format: [vX.Y.Z] Modul, Typ: Kurztitel
- Typen: Feature / Fix / Change / Refactor / Perf / Docs
- NIE git push ausführen — Herbert pusht selbst
- NIE neue Libraries/Packages ohne Freigabe

## Ausführliche Standards (bei Bedarf lesen)

- Architektur: Docs/Kern/BauProjektManager_Architektur.md
- Coding Standards: Docs/Kern/CODING_STANDARDS.md
- DB Schema: Docs/Kern/DB-SCHEMA.md
- DSGVO: Docs/Kern/DSVGO-Architektur.md
- ADRs: Docs/Referenz/ADR.md
- UI/UX: Docs/Referenz/UI_UX_Guidelines.md
- Backlog: Docs/Kern/BACKLOG.md
