# INDEX.md — Projekt-Index

> **Zweck:** Routing-Matrix für KI-gestützte Code-Erstellung und Doku-Pflege.
> Claude lädt diese Datei ZUERST um zu entscheiden welche Docs und Code-Dateien
> für eine Aufgabe geladen werden müssen.
>
> **Pflege:** Bei neuen Docs, Modulen oder zentralen Code-Einstiegspunkten
> aktualisieren. Keine per-File-Vollständigkeit für normalen Code.
> Wird vom `doc-pflege` Skill automatisch mitgepflegt.

---

## Projekt-Metadaten

- **Projekt:** BauProjektManager (BPM) / baulotse
- **Tech-Stack:** C# / .NET 10 LTS, WPF, SQLite, CommunityToolkit.Mvvm, Serilog, ClosedXML
- **Typ:** WPF Desktop-Anwendung, modularer Monolith, offline-first
- **Repo:** github.com/herbertschrotter-blip/BauProjektManager, Branch: main
- **Version:** In `Directory.Build.props` im Repo-Root
- **Arbeitsverzeichnis:** Dynamisch via PC-Tabelle (siehe unten)

### PCs und Arbeitsverzeichnisse

**Projekt-Suffix** (gleich auf allen PCs): `Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager`

| PC-Name | COMPUTERNAME | Bemerkung |
|---------|-------------|-----------|
| Firmenlaptop | FIRMENLAPTOP | Arbeits-PC |
| Surface | (TODO) | Surface Tablet |
| Standrechner | Desktop_PC | PC zuhause |

**Ermittlung durch Claude (DC):**
```powershell
$pc = hostname; $od = [System.Environment]::GetEnvironmentVariable('OneDrive', 'User'); Write-Host "$pc|$od"
```
**workFolder** = OneDrive-Pfad + `\` + Projekt-Suffix. Unbekannte PCs werden bei Erstverwendung registriert.

---

## 1. Global Mandatory Reads

Dateien die bei vielen Änderungstypen kritisch sind. Im Zweifel laden.

| Datei | Laden wenn | Pfad |
|-------|-----------|------|
| Architektur-Doc | Neue Services, Module, DI, Schichtfragen | `Docs/Kern/BauProjektManager_Architektur.md` |
| DB-Schema | Persistenzänderungen, neue Tabellen, Spalten | `Docs/Kern/DB-SCHEMA.md` |
| DSGVO-Architektur | Externe Kommunikation, personenbezogene Daten, API-Calls | `Docs/Kern/DSVGO-Architektur.md` |
| UI/UX Guidelines | Sichtbare UI-Änderungen, neue Controls | `Docs/Referenz/UI_UX_Guidelines.md` |
| WPF UI Architecture | Neue Views, Styles, ResourceDictionaries, Theme-Tokens | `Docs/Referenz/WPF_UI_Architecture.md` |
| Coding Standards | Naming, Patterns, Anti-Patterns (nur wenn unsicher) | `Docs/Kern/CODING_STANDARDS.md` |

---

## 2. Task-to-Doc Routing

Welche Docs bei welchem Änderungstyp geladen werden müssen.

| Änderungstyp | Pflicht-Docs |
|-------------|-------------|
| Wetter / externe API | ModuleWetter.md, DSVGO-Architektur.md, Architektur-Doc |
| Foto / Dateiimport | ModuleFoto.md, DSVGO-Architektur.md |
| Zeiterfassung / Excel | ModuleZeiterfassung.md, DSVGO-Architektur.md |
| Einstellungen-UI | UI_UX_Guidelines.md, WPF_UI_Architecture.md |
| Plan-Management | PlanManager.md, Architektur-Doc, DB-SCHEMA.md, UI_Navigation.md |
| SQLite / Repository | DB-SCHEMA.md, Architektur-Doc |
| Neuer Dialog | UI_UX_Guidelines.md, WPF_UI_Architecture.md, Dialogs.xaml |
| Neues Modul | Architektur-Doc, zugehöriges Konzept-Doc, DB-SCHEMA.md |
| Navigation / Shell | UI_Navigation.md, Architektur-Doc, MainWindow.xaml/.cs |
| DevTools | Docs/Module/ModuleDevTools.md |
| Kalkulation | ModuleKalkulation.md, DB-SCHEMA.md |
| KI-Assistent | ModuleKiAssistent.md, DSVGO-Architektur.md |
| Mobile/PWA | BPM-Mobile-Konzept.md |
| Multi-User | MultiUserKonzept.md |
| Lizenzierung | ModuleAktivierungLizenzierung.md |

---

## 3. Docs-Verzeichnis

### Docs/Kern/ — Bei JEDER Code-Änderung potenziell relevant

| Datei | Inhalt | Größe |
|-------|--------|-------|
| BauProjektManager_Architektur.md | Solution-Struktur, DI-Setup, Service-Architektur, Schichten, Modulstruktur | 42KB |
| DB-SCHEMA.md | Alle SQLite-Tabellen, Spalten, Constraints, Migrations-Logik | 28KB |
| CODING_STANDARDS.md | BPM-spezifische Coding-Regeln, XAML-Konventionen, Naming | 43KB |
| DSVGO-Architektur.md | Datenschutz, DataClassification, IExternalCommunicationService, Logging-Regeln | 49KB |
| BACKLOG.md | Offene Features, Bugs, Ideen, Prioritäten | 12KB |

### Docs/Referenz/ — Lesen wenn Thema aufkommt

| Datei | Inhalt | Größe |
|-------|--------|-------|
| ADR.md | 45 Architecture Decision Records | 78KB |
| CHANGELOG.md | Versionshistorie | 23KB |
| VISION.md | Produktvision, Zielgruppe, Roadmap | 19KB |
| DEPENDENCY-MAP.md | Projektabhängigkeiten, NuGet-Pakete | 13KB |
| UI_UX_Guidelines.md | Farben, Spacing, Komponenten, Layout-Regeln | 25KB |
| WPF_UI_Architecture.md | Theme-System, 7 ResourceDictionaries, Token-Architektur | 32KB |
| UX_Flows.md | Benutzerflüsse, Dialog-Abfolgen | 17KB |
| GLOSSAR.md | Österreichische Bau-Terminologie, Fachbegriffe | 19KB |
| Setup-ClaudeCode-MCP.md | Claude Code / Desktop Commander Setup-Anleitung | 4KB |
| UI_Navigation.md | Shell-Aufbau, Sidebar, Toolbar, Screen-Hierarchie, Navigationsregeln | 7KB |

### Docs/Konzepte/ — Erst relevant wenn Modul gebaut wird

| Datei | Modul |
|-------|-------|
| ModuleWetter.md | Wetter-Modul (Google Sheets Worker, CSV-Sync) |
| ModuleFoto.md | Foto-Modul |
| ModuleZeiterfassung.md | Zeiterfassung (Excel als SoT) |
| ModuleBautagebuch.md | Bautagebuch |
| ModuleDashboard.md | Dashboard |
| ModuleOutlook.md | Outlook-Integration |
| ModuleKiAssistent.md | KI-Assistent |
| ModuleKalkulation.md | Kalkulation |
| ModuleGIS.md | GIS / Koordinaten |
| ModuleVorlagen.md | Vorlagen-System |
| ModuleTaskManagement.md | Task-Management |
| Moduleplanheader.md | Plankopf-Extraktion |
| BPM-Mobile-Konzept.md | Mobile PWA |
| MultiUserKonzept.md | Multi-User / Sync |
| ModuleAktivierungLizenzierung.md | Lizenzierung |
| ModuleOrdnerSync.md | Ordner-Sync (Firmenserver ↔ OneDrive, bidirektional) |

### Docs/Module/ — Implementierte Module

| Datei | Status |
|-------|--------|
| ModuleProjekt.md | Implementiert (v0.16.x) |
| ModuleDevTools.md | Implementiert (v0.16.x) |
| PlanManager.md | V1 Kernmodul, 21 Kapitel, v2.0 nach ChatGPT Cross-Review |

---

## 4. Code Entry Points

### src/BauProjektManager.App — WPF Shell

- **App.xaml.cs** — DI-Setup, Startup, alle Service-Registrierungen. MUSS laden bei neuen Services, Dialogen, DI-Änderungen.
- **MainWindow.xaml/.cs** — Shell, Navigation, Tab-Steuerung. Laden bei neuen Views, Navigationseinträgen.
- **SetupDialog.xaml/.cs** — Ersteinrichtung. Laden bei Setup-Änderungen.
- **Themes/** — 7 ResourceDictionaries. MUSS laden bei neuen sichtbaren Controls, Dialog-Styling.
  - Colors.xaml, Buttons.xaml, DataGrid.xaml, Dialogs.xaml, Icons.xaml, TreeView.xaml, Typography.xaml
- **Referenz: DevToolsDialog.xaml/.cs** — Neustes Dialog-Muster (3-Tab, Theme-Tokens, Safety-Badges).
- BpmConfirmDialog, BpmInfoDialog, BpmDialogService — Dialog-Infrastruktur.

### src/BauProjektManager.Domain — Entities, Interfaces, Enums

- **Interfaces/** — Service-Verträge. Zuerst laden bei neuen Services, DI-relevanten Änderungen.
  - IDeveloperToolsService.cs, IDialogService.cs
- **Models/** — Domain-Entities. Laden bei neuen Feldern, Validierung, DB-naher Fachlogik.
  - Project.cs, Client.cs, Building.cs, BuildingLevel.cs, BuildingPart.cs
  - ProjectLocation.cs, ProjectParticipant.cs, ProjectLink.cs, ProjectPaths.cs, ProjectTimeline.cs
  - AppSettings.cs, BpmManifest.cs
- **Models/PlanManager/** — PlanManager Domain-Modelle. Laden bei Parser/Profil/Import-Änderungen.
  - FieldType.cs (Enum: 16 Bau-Felder + Custom)
  - FileNameSegment.cs (Segment-Modell)
  - ParsedFileName.cs (Parse-Ergebnis)
  - IndexSourceType.cs (Enum: FileName, None, PlanHeader — ADR-045)
- **Enums/** — Status-Enums. Laden bei Workflow- oder Status-Änderungen.
  - ProjectStatus.cs

### src/BauProjektManager.Infrastructure — Services, SQLite, Persistence

- **Persistence/** — Service-Implementierungen + DB-Zugriff. Laden bei neuen Services oder DB-Änderungen.
  - ProjectDatabase.cs (35KB, SQLite-Zugriff, groß → SUCHE/ERSETZE)
  - AppSettingsService.cs (settings.json lesen/schreiben)
  - BpmManifestService.cs (manifest.json, Projektverwaltung)
  - ProjectFolderService.cs (Ordnerstruktur-Erstellung)
  - RegistryJsonExporter.cs (registry.json für VBA-Kompatibilität)
  - **Referenz: AppSettingsService.cs** — Sauberes Service-Pattern mit Serilog.
- **Dev/** — Developer-only Services.
  - DeveloperToolsService.cs — DB-Reset, Batch-Script-Generierung. Referenz für Debug-only Pattern.

### src/BauProjektManager.Settings — Einstellungen-Modul

- **ViewModels/SettingsViewModel.cs** (28KB) — Referenz für MVVM mit CommunityToolkit.Mvvm, 5-Tab-Dialog.
- **Views/** — Referenz für modulare Tab-Dialog-Struktur.
  - SettingsView.xaml/.cs — 5-Tab-Hauptdialog. **Referenz für neue Modul-Views.**
  - ProjectEditDialog.xaml/.cs (je ~58KB, sehr groß → nur SUCHE/ERSETZE)
  - FolderTemplateControl.xaml/.cs — UserControl-Muster.

### src/BauProjektManager.PlanManager — Plan-Modul (V1-Kernfeature, im Aufbau)

- **ViewModels/** — MVVM ViewModels. Laden bei neuen Commands, Bindings, Navigation.
  - PlanManagerViewModel.cs — Projektliste, Eingangs-Zaehler, PlanProjectItem Wrapper.
  - ProjectDetailViewModel.cs — Projektdetail, Eingangs-Info, GoBack-Navigation.
  - ProfileWizardViewModel.cs — 5-Schritt Profil-Wizard (Datei, Segmente, Index, Zielordner, Erkennung). Helper-Klassen: FieldTypeOption, IndexSourceOption, HierarchyLevelOption, RecognitionSegment.
- **Views/PlanManagerView.xaml/.cs** — Host: Projektliste + DetailHost ContentControl, DynamicResource.
- **Views/ProjectDetailView.xaml/.cs** — Projektdetail: Toolbar + 3 Tabs (Profile, Manuell, Sync).
- **Views/ProfileWizardDialog.xaml/.cs** — 5-Schritt Profil-Wizard (modal, 750x580). Converter: CountToVisInverse, CountToVisZero, InverseBool.
- **Services/** — PlanManager-Logik. Laden bei Parser/Import/Profil-Änderungen.
  - FileNameParser.cs — Segment-Splitting (ADR-022), statisch, keine Abhängigkeiten.

---

## 5. Cross-Cutting Anchors

Dateien die nicht modulär sind, sondern immer querliegend relevant.

| Datei | Querschnitt |
|-------|-------------|
| App.xaml.cs | DI, Startup, Service-Registrierung — bei JEDEM neuen Service |
| DSVGO-Architektur.md | Externe Kommunikation, Klassifikation — bei JEDER externen Verbindung |
| Architektur-Doc | Schichtgrenzen, Modulzuordnung — bei jeder neuen Klasse |
| Themes/Colors.xaml + Buttons.xaml | Visuelle Konsistenz — bei jedem neuen UI-Element |
| DB-SCHEMA.md | Datenstruktur — bei jeder Persistenzänderung |

---

## 6. Referenzimplementierungen

Diese Dateien sind "gute Vorbilder" — bevorzugt als Muster verwenden.

| Datei | Referenz für |
|-------|-------------|
| DevToolsDialog.xaml/.cs | Neustes Dialog-Pattern (Theme-Tokens, 3-Tab, Safety) |
| SettingsView.xaml/.cs + SettingsViewModel.cs | MVVM-Pattern, 5-Tab-Dialog, CommunityToolkit |
| AppSettingsService.cs | Service-Pattern mit Serilog, JSON, Fehlerbehandlung |
| DeveloperToolsService.cs | Debug-only Service, Batch-Script, Interface-Pattern |

**NICHT als Referenz verwenden:**
- ProjectEditDialog.xaml.cs (58KB, historisch gewachsen, Refactoring geplant)

---

## 7. Risk Triggers

Wenn eine Anfrage eines davon enthält → mindestens Modus **Deep** verwenden.

- Externe API / HTTP / Datei-Export
- Personenbezogene Daten / DSGVO
- Neue Tabelle / Schemaänderung / SQLite-Migration
- Neuer Dialog / neue View / neuer Benutzerfluss
- Neues Interface + neue Service-Implementierung
- Änderungen in mehr als einem Projekt (z.B. Domain + Infrastructure + App)
- DI-Registrierung betroffen UND fachliche Änderung nicht rein lokal

---

## 8. Doc-Pflege Policy

Wann welche Begleitdokumente mitgepflegt werden müssen.

| Trigger | Pflicht-Docs aktualisieren |
|---------|---------------------------|
| Neues Modul-Doc erstellt | INDEX.md (neuer Routing-Eintrag) |
| Neue Tabelle / DB-Änderung | DB-SCHEMA.md, INDEX.md |
| Neues Feature fertig | CHANGELOG.md |
| Neue Architekturentscheidung | ADR.md, INDEX.md (falls neuer Entry Point) |
| Neuer zentraler Code-Einstiegspunkt | INDEX.md (Code Entry Points) |
| Neue externe Abhängigkeit | DEPENDENCY-MAP.md, ggf. ADR.md |
| Neue Referenzimplementierung | INDEX.md (Referenzimplementierungen) |
| Konzept wesentlich geändert | BACKLOG.md prüfen |
| Neues Modul implementiert | Docs/Module/ (neues Doc), INDEX.md |
