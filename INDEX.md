# INDEX.md — Projekt-Router

> **Zweck:** Routing-Matrix für KI-gestützte Code-Erstellung und Doku-Pflege.
> Claude lädt diese Datei ZUERST um zu entscheiden welche Docs und Code-Dateien
> für eine Aufgabe geladen werden müssen.
>
> **Laderegel:** Nach INDEX.md → Quickload der relevanten Docs lesen → nur bei
> Bedarf Langform nachladen. Siehe DOC-STANDARD.md Kapitel 8.
>
> **Norm:** Docs/Referenz/DOC-STANDARD.md definiert Frontmatter, Quickload,
> Kapitelvorlagen und Skill-Ladereihenfolge.

---

## Projekt-Metadaten

- **Projekt:** BauProjektManager (BPM) / baulotse
- **Tech-Stack:** C# / .NET 10 LTS, WPF, SQLite, CommunityToolkit.Mvvm, Serilog, ClosedXML
- **Typ:** WPF Desktop-Anwendung, modularer Monolith, offline-first
- **Repo:** github.com/herbertschrotter-blip/BauProjektManager, Branch: main
- **Version:** In `Directory.Build.props` im Repo-Root

### PCs und Arbeitsverzeichnisse

| PC | COMPUTERNAME | Projekt-Suffix |
|----|-------------|----------------|
| Desktop-PC | Desktop_PC | `Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager` |
| Firmenlaptop | Firmenlaptop | `Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager` |
| Surface | Surface7 | `Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager` |

Basispfad = OneDrive-Pfad + `\` + Projekt-Suffix.

---

## Projekt-Phase (VERBINDLICH)

BPM befindet sich in früher Entwicklung ohne Produktivdaten.

Konsequenzen für Code und Architektur:
- Bei Schema-/Config-/DB-Änderungen wird NIE automatisch eine Migration gebaut
- Kein Backward-Compatibility-Code für alte Datenformate
- Keine Legacy-Tolerance in Parsern / Loadern / Deserializern
- Stattdessen: Claude listet welche Datei(en)/DB-Tabelle(n) betroffen sind
  → User löscht sie → BPM erstellt sie beim nächsten Start neu

Ausnahmen:
- Nur wenn der User explizit "Migration bauen" sagt
- Oder dieses Kapitel offiziell entfernt/umgeschrieben wird
  (= Übergang in Produktivphase)

Für KI (Claude, ChatGPT): Dieses Kapitel ist beim Quickload IMMER zu beachten.
Vorschläge zu Migrations-Logik, Backward-Compatibility oder Legacy-Tolerance
sind ohne explizite User-Freigabe abzulehnen.

---

## 0. Global Mandatory Reads (IMMER laden — Quickload reicht)

Diese Docs werden bei JEDER Code-Änderung mindestens als Quickload gelesen.
Ihre Fachlichen Invarianten gelten immer.

| Doc | Quickload-Zweck | Pfad |
|-----|----------------|------|
| Architektur | Schichtgrenzen, Dependency-Regel, SoR je Modus, Offline-First | `Docs/Kern/BauProjektManager_Architektur.md` |
| CODING_STANDARDS | Naming, MVVM, Sync-Felder, UTC, Soft Delete, IUserContext, Icons | `Docs/Kern/CODING_STANDARDS.md` |
| DSGVO-Architektur | Logging-Regeln, HttpClient-Verbot, DataClassification | `Docs/Kern/DSVGO-Architektur.md` |

Bei UI-Änderungen zusätzlich:

| Doc | Quickload-Zweck | Pfad |
|-----|----------------|------|
| UI_UX_Guidelines | Screen States, Button-Regel, Sprache | `Docs/Referenz/UI_UX_Guidelines.md` |
| WPF_UI_Architecture | Theme-Tokens, ResourceDictionaries, DynamicResource-Regel | `Docs/Referenz/WPF_UI_Architecture.md` |

---

## 1. Task-to-Doc Routing

### Plan-Management
- Primary: `Docs/Module/PlanManager.md`
- Secondary: `Docs/Kern/DB-SCHEMA.md`, `Docs/Kern/BauProjektManager_Architektur.md`
- Reference: `Docs/Referenz/UI_Navigation.md`

### Einstellungen / Projekt-UI
- Primary: `Docs/Module/ModuleProjekt.md`
- Secondary: `Docs/Referenz/UI_UX_Guidelines.md`, `Docs/Referenz/WPF_UI_Architecture.md`

### Wetter / externe API
- Primary: `Docs/Konzepte/ModuleWetter.md`
- Secondary: `Docs/Kern/DSVGO-Architektur.md`, `Docs/Kern/BauProjektManager_Architektur.md`

### Foto / Dateiimport
- Primary: `Docs/Konzepte/ModuleFoto.md`
- Secondary: `Docs/Kern/DSVGO-Architektur.md`

### Zeiterfassung / Excel
- Primary: `Docs/Konzepte/ModuleZeiterfassung.md`
- Secondary: `Docs/Kern/DSVGO-Architektur.md`

### SQLite / Repository / Datenbankänderung
- Primary: `Docs/Kern/DB-SCHEMA.md`
- Secondary: `Docs/Kern/BauProjektManager_Architektur.md`
- Beachte: CODING_STANDARDS Kap. 19 (Sync-Felder, UTC, Soft Delete, IUserContext)

### Neuer Dialog / UI-Element
- Primary: `Docs/Referenz/UI_UX_Guidelines.md`
- Secondary: `Docs/Referenz/WPF_UI_Architecture.md`

### Neues Modul
- Primary: `Docs/Kern/BauProjektManager_Architektur.md`
- Secondary: Zugehöriges Konzept-Doc, `Docs/Kern/DB-SCHEMA.md`

### Navigation / Shell
- Primary: `Docs/Referenz/UI_Navigation.md`
- Secondary: `Docs/Kern/BauProjektManager_Architektur.md`

### DevTools
- Primary: `Docs/Module/ModuleDevTools.md`

### Kalkulation
- Primary: `Docs/Konzepte/ModuleKalkulation.md`
- Secondary: `Docs/Kern/DB-SCHEMA.md`

### KI-Assistent
- Primary: `Docs/Konzepte/ModuleKiAssistent.md`
- Secondary: `Docs/Kern/DSVGO-Architektur.md`

### Mobile / PWA
- Primary: `Docs/Konzepte/BPM-Mobile-Konzept.md`

### Multi-User / Sync
- Primary: `Docs/Konzepte/MultiUserKonzept.md`

### Server-Architektur / Auth / API
- Primary: `Docs/Konzepte/ServerArchitektur.md`
- Secondary: `Docs/Konzepte/MultiUserKonzept.md`, `Docs/Konzepte/DatenarchitekturSync.md`, `Docs/Kern/DSVGO-Architektur.md`

### Lizenzierung
- Primary: `Docs/Konzepte/ModuleAktivierungLizenzierung.md`

### DSGVO / Datenschutz / externe Kommunikation
- Primary: `Docs/Kern/DSVGO-Architektur.md`

### Ordner-Sync
- Primary: `Docs/Konzepte/ModuleOrdnerSync.md`

---

## 2. Code Entry Points

### src/BauProjektManager.App
- **App.xaml.cs** — DI-Setup, Startup. Bei neuen Services.
- **MainWindow.xaml/.cs** — Shell, Navigation. Bei neuen Views.
- **SetupDialog.xaml/.cs** — Ersteinrichtung.
- **Themes/** — Colors, Buttons, DataGrid, Dialogs, Icons, TreeView, Typography.

### src/BauProjektManager.Domain
- **Interfaces/** — Service-Verträge.
- **Models/** — Domain-Entities.
- **Enums/** — Status-Enums.

### src/BauProjektManager.Infrastructure
- **Persistence/** — ProjectDatabase.cs (35KB), AppSettingsService, BpmManifestService, ProjectFolderService, RegistryJsonExporter.

### src/BauProjektManager.Settings
- **SettingsViewModel.cs** (28KB) — Referenz MVVM.
- **ProjectEditDialog.xaml/.cs** (je ~58KB, nur SUCHE/ERSETZE)

### src/BauProjektManager.PlanManager
- **Views/PlanManagerView.xaml/.cs** — Im Aufbau.

---

## 3. Referenzimplementierungen

| Datei | Referenz für |
|-------|-------------|
| DevToolsDialog.xaml/.cs | Dialog-Pattern |
| SettingsView + SettingsViewModel | MVVM-Pattern |
| AppSettingsService.cs | Service-Pattern |

NICHT als Referenz: ProjectEditDialog.xaml.cs (Refactoring geplant)

---

## 4. Risk Triggers → Modus Deep

- Externe API / HTTP / Export
- Personenbezogene Daten / DSGVO
- Neue Tabelle / Schemaänderung
- Neuer Dialog / View / Benutzerfluss
- Neues Interface + Service
- Änderungen in mehreren Projekten
- DI betroffen UND nicht rein lokal

---

## 5. Cross-Cutting Anchors

| Datei | Querschnitt |
|-------|-------------|
| App.xaml.cs | DI — bei jedem neuen Service |
| DSVGO-Architektur.md | Externe Verbindungen |
| Architektur-Doc | Schichtgrenzen |
| Themes/Colors+Buttons | Visuelle Konsistenz |
| DB-SCHEMA.md | Persistenzänderungen |

---

## 6. Doc-Pflege Policy

| Trigger | Aktion |
|---------|--------|
| Neues Doc | INDEX.md Routing + Frontmatter prüfen |
| Neue Tabelle | DB-SCHEMA.md |
| Neues Feature fertig | CHANGELOG.md |
| Neue Architekturentscheidung | ADR.md |
| Neuer Code-Entry-Point | INDEX.md |
| Neue Abhängigkeit | DEPENDENCY-MAP.md |
| Konzept geändert | BACKLOG.md prüfen |
| Doc geändert | Frontmatter + Quickload prüfen |
