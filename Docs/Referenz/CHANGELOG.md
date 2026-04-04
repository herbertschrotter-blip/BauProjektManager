# BauProjektManager — Changelog

Alle Änderungen am Projekt, chronologisch dokumentiert.  
Format: [Keep a Changelog](https://keepachangelog.com/de/1.0.0/), Semantic Versioning.

---

## [v0.16.3] — 2026-04-04

### Geändert
- **Settings:** SettingsView.xaml komplett auf Token-Referenzen migriert (alle hardcoded Farben → Themes/)
- **Settings:** ProjectEditDialog.xaml komplett auf Token-Referenzen migriert (5 Tabs, alle Styles)
- **App:** SetupDialog.xaml komplett auf Token-Referenzen migriert
- **Settings:** Label „OneDrive" → „Cloud-Speicher" in SettingsView

### Behoben
- **Settings:** SettingsViewModel implementiert IDisposable für ProjectDatabase

---

## [v0.16.2] — 2026-04-04

### Geändert
- **App:** SetupDialog UI-Labels „OneDrive" → „Cloud-Speicher" (ADR-004)

---

## [v0.16.1] — 2026-04-03 / 2026-04-04

### Behoben (04.04.)
- **Domain:** Dateiname client.cs → Client.cs (CODING_STANDARDS Kap. 1.1)
- **Infrastructure:** Leerer Catch-Block in ReadStringOrDefault durch Log-Warning ersetzt
- **App:** Version-Anzeige aus Assembly statt hardcoded „0.10.0"
- **Build:** Directory.Build.props Version 0.2.0 → 0.16.1

### Dokumentation — DSGVO + Privacy (03.04.)
- **DSGVO-Architektur** v1.3→v1.4 — Privacy Engineering, Datenklassifizierung A/B/C, IExternalCommunicationService, IPrivacyPolicy (Strategy Pattern), Dienststatus-Modell, Löschkonzept, Audit-Negativliste
- **ADR-035** IExternalCommunicationService — zentrales Privacy Gate
- **ADR-036** IPrivacyPolicy — austauschbare Policy, `RequiresStrictCompliance` (nicht `IsCommercial`)
- Docs-Ordnerstruktur reorganisiert: `Kern/` + `Referenz/` + `Konzepte/`
- CODING_STANDARDS: .NET 9 → .NET 10 LTS, neues Kapitel 17 „Datenschutz im Code", 17.7 Datenschutz nie im ViewModel

### Dokumentation — Cross-Review mit ChatGPT (03.–04.04.)

**Kern-Docs Review (5 Docs, 3 Runden, 17 Änderungen):**
- **DSGVO-Architektur** v1.4 — Dienststatus-Modell (Disabled→EnabledManual→EnabledAuto), Anonymisierung als eigener Service, Löschkonzept Stammdaten, Audit-Negativliste + decision_reason Katalog
- **Architektur** v2.1.0 — registry.json als Exportvertrag (registryVersion), Betriebsmodi A/B/C, Privacy Control Layer in Solution-Struktur, OneDrive→Cloud-Speicher, SQLite-Scope (Excel-Ausnahme)
- **CODING_STANDARDS** — Kap. 17.7 Datenschutz-Logik nie im ViewModel
- **DB-SCHEMA** — FK-Regel (alle FKs auf `id` nie `seq`), seq vs. id Rollen, Präfix-Tabelle (17 Tabellen), geplante Tabellen auf TEXT-IDs
- **BACKLOG** — Datenschutz-Infrastruktur "PFLICHT vor erstem Online-Modul", ADR-039 erledigt

**ADR Review (39→42 ADRs, 4 Runden):**
- **ADR-039** NEU — Einheitliches ID-Schema TEXT mit Präfix für alle Tabellen
- **ADR-040** NEU — Migrations- und Versionierungsstrategie (Forward-Only, Backup)
- **ADR-041** NEU — Recovery / Degraded Mode (Normal/Eingeschränkt/Blockiert)
- **ADR-042** NEU — Secrets und Credentials (DPAPI/SecretStore, Lizenz-Ehrlichkeit)
- Statusmodell eingeführt: Decision Status (Proposed→Accepted→Superseded) + Implementation Status (Not Started→Partial→Implemented)
- ADR-002: Scope-Korrektur (SQLite SoR für Kerndaten, Ausnahme Excel ADR-018)
- ADR-006: Modulinteraktionsregeln (keine gegenseitigen Referenzen, Verträge in Domain)
- ADR-020: Titel + Scope auf LAN-Netzlaufwerk eingeschränkt, Ablösung durch ADR-037
- ADR-028: 5→7 ResourceDictionaries (+Inputs.xaml, +Tabs.xaml)
- ADR-033: Cloud-Ordner gestrichen, Event-Sync als Mechanismus eingeordnet
- ADR-036: `IsCommercial` → `RequiresStrictCompliance`
- ADR-042: Lizenz-Secret ehrlich als "manipulationserschwerend, nicht manipulationssicher"

**DEPENDENCY-MAP Review (2 Runden):**
- v2.0→v2.1 — Geplante Services (ISyncTransport, IAccessControlService, ITaskManagementService, EntityIdGenerator, SecretStore, StartupHealthCheck), Cloud-Speicher-neutral, Verweis auf DB-SCHEMA.md

**UI_UX_Guidelines Review (3 Runden, 8 Änderungen):**
- v2.0→v2.1 — Mindestauflösung entschärft (1920×1080 optimiert, 1366×768 unterstützt), Ist/Zielbild mit ✅/🎯/⬜, Overlay-Klick bei Formulardialogen entfernt, Primary-Action harmonisiert, 3 neue States (Dirty, Read-only, Partial Success), Validierungszusammenfassung für Mehrtab-Dialoge, Feedback-Matrix als Kap. 18

**WPF_UI_Architecture Neufassung (2 Runden, 15 Punkte):**
- v1.0→v2.0 — Controls/ als Shell-only, 7 Dictionaries offiziell, CommunityToolkit.Mvvm statt eigener MVVM-Basis, Token→WPF-Key Mapping-Tabelle, ViewState + Operation Flags getrennt, Feedback-Infrastruktur, kein ex.Message zum User, Mehrtab-Validierung, Responsive-Regeln, Navigation als V1-Übergang, Migration hardcoded→tokenisiert, SecretStore statt DPAPI direkt

---

## [v0.16.0] — 2026-03-30

### Hinzugefügt
- **Theme-System** — Zentrales Design-System mit Resource Dictionaries (ADR-028)
- `Themes/Colors.xaml` — Alle Farb-Token als SolidColorBrush (Dark Theme)
- `Themes/Typography.xaml` — Segoe UI, 8 Schriftgrößen-Stufen (XS bis XXL)
- `Themes/Buttons.xaml` — Button-Varianten: Primary, Secondary, Danger, Ghost, Nav
- `Themes/DataGrid.xaml` — Header, Row, Cell Styles, Zebra-Variante
- `Themes/Dialogs.xaml` — Dialog-Basis, TabControl, Cards, Tooltips, Separatoren
- App.xaml merged alle ResourceDictionaries
- MainWindow.xaml verwendet nur noch Token (keine hardcoded Farben)

### Dokumentation
- **UI_UX_Guidelines.md** v2.0 — Komplettes Design-System nach Review
- **WPF_UI_Architecture.md** v1.0 — Technischer UI-Aufbau
- **UX_Flows.md** v1.0 — Hauptworkflows
- **GLOSSAR.md** — Begriffsdefinitionen
- **CODING_STANDARDS.md** — UI-Naming-Konventionen + ResourceDictionary-Regeln ergänzt
- **DB-SCHEMA.md** v1.5 — Zentrales DB-Leitdokument (Ist + geplant, 18 Tabellen)
- 5 neue Konzeptdokumente: ModuleKalkulation, ModuleTaskManagement, MultiUserKonzept, ModuleAktivierungLizenzierung, ModuleKiAssistent
- ADR.md erweitert: 27 → 34 Entscheidungen (ADR-024 bis ADR-034)
- BauProjektManager_Architektur.md v1.5 → v2.0.0
- BACKLOG.md v2.0 mit MoSCoW + MVP-Struktur

---

## [v0.15.0] — 2026-03-29

### Hinzugefügt
- **Tab 4 Portale + Links** — 2-Spalten-Layout: Bauherren-Portale (links) + Eigene Links (rechts)
- `ProjectLink` Domain-Modell (Name, Url, LinkType Portal/Custom, IsConfigured)
- `project_links` DB-Tabelle (Schema v1.5)
- Portal-Typen editierbar (✎ Button, PortalTypes in settings.json: InfoRaum, PlanRadar, PlanFred, Bau-Master, Dalux)
- Edit-Dialog: Portal mit Dropdown, eigene Links mit Freitext
- "Öffnen" Button öffnet URL im Standard-Browser
- Dashboard-Vorschau unten zeigt konfigurierte Links als klickbare Buttons

---

## [v0.14.0] — 2026-03-29

### Hinzugefügt
- **Tab 3 Beteiligte** — Projektbezogene Firmenliste mit CRUD
- `ProjectParticipant` Domain-Modell (Role, Company, ContactPerson, Phone, Email, ContactId)
- `project_participants` DB-Tabelle (Schema v1.4)
- DataGrid mit 5 Spalten (Rolle, Firma, Kontaktperson, Telefon, Email)
- Edit-Dialog mit Rolle als editierbares Dropdown (aus ParticipantRoles in settings.json)
- Rollen-Liste editierbar (✎ Button)
- ▲▼ Sortierung
- Import-Buttons vorbereitet (ausgegraut): "Liste importieren" + "Aus Adressbuch"
- `contact_id` Feld vorbereitet für späteres Adressbuch (FK auf zukünftige contacts-Tabelle)

### Entscheidungen
- Adressbuch als separate Entität (projektübergreifend, Outlook-kompatibel) — getrennt von Projekt-Beteiligten
- Firmenliste-Import: geführter KI-Ablauf geplant (Prompt → Copy → Paste → Parse), später API-basiert

---

## [v0.13.2] — 2026-03-29

### Hinzugefügt
- **Tab 2 Bauwerk** — Bauteile + Geschosse mit Live-Berechnung
- Bauteile-DataGrid mit Edit-Dialog (Kürzel, Beschreibung, Bauwerkstyp, ± 0,00 abs.)
- Geschoss-DataGrid direkt editierbar (RDOK orange, FBOK, RDUK) mit Komma-Eingabe
- + Geschoss öffnet Dialog mit intelligentem Vorschlag (UG→EG→OG1→OG2)
- Prefix automatisch berechnet (EG=00, darunter negativ, darüber positiv)
- Beschreibung automatisch aus 2-spaltiger Geschoss-Liste (ShortName+LongName)
- ✎ Button für Geschoss-Bezeichnungen bearbeiten (2-spaltig: Kurzbezeichnung+Langbezeichnung)
- LevelNames in settings.json als LevelNameEntry (ShortName+LongName)
- BuildingTypes Liste in AppSettings für Bauwerkstyp-Dropdown
- Live-Berechnung: Geschosshöhe, Rohbauhöhe, Deckenstärke, FB-Aufbau

---

## [v0.13.1] — 2026-03-29

### Hinzugefügt
- **Domain:** `BuildingPart` + `BuildingLevel` Modelle
- BuildingPart: ShortName, Description, BuildingType, ZeroLevelAbsolute, SortOrder, Levels
- BuildingLevel: Prefix, Name, Description, Rdok, Fbok, Rduk (nullable), berechnete Properties
- `building_parts` + `building_levels` DB-Tabellen (Schema v1.3)
- Project.BuildingParts ersetzt alte Buildings-Liste
- `GetNextLevelName()` und `GetAutoDescription()` für intelligente Vorschläge

---

## [v0.13.0] — 2026-03-29

### Hinzugefügt
- **Tab 1 Stammdaten** — Komplett neu aufgebaut mit 5-Tab-Dialog
- ProjectEditDialog mit TabControl: Stammdaten, Bauwerk, Beteiligte, Portale+Links, Ordnerstruktur
- Tab 1: 2-Spalten-Layout (links: Projekt+Auftraggeber+Sonstiges, rechts: Adresse+Verwaltung+Grundstück+Laufzeit)
- `ProjectType` als String (editierbare Dropdown-Liste aus settings.json, ✎ Button)
- 📋 und 👤 Icon-Buttons für Firma/Kontakt vorbereitet (disabled)
- GIS-Buttons neben Verwaltung und Grundstück vorbereitet (disabled)
- DatePicker für Laufzeit-Felder (Baustart, Gepl. Ende, Tats. Ende)
- DB-Migration v1.1→v1.2: `project_type` Spalte

### Geändert
- **Status vereinfacht:** Nur noch Active/Completed (Archived entfernt)
- StatusColorConverter: Grau-Brush entfernt, Default-Fallback ist Rot
- Window-Größe auf 900×1100

---

## [v0.12.7] — 2026-03-29

### Dokumentation
- BACKLOG gestrafft — Konzepttexte in eigene Docs ausgelagert, Querverweise eingefügt (~400 → ~180 Zeilen)
- Modul-Konzepte erstellt: ModuleZeiterfassung.md, ModuleGIS.md, ModulePlanHeader.md (von Herbert)
- ModuleFoto.md aktualisiert mit PhotoFolder V2 Referenz (WPF statt Server, Lessons Learned)
- Prio-Liste für Nach-V1-Module festgelegt (Foto → Zeiterfassung → Bautagebuch → Dashboard)

---

## [v0.12.6] — 2026-03-29

### Geändert
- Modul-Konzeptdokumente nach `Docs/Konzepte/` verschoben (neue Ordnerstruktur)
- Betrifft: ModuleBautagebuch, ModuleDashboard, ModuleFoto, ModuleOutlook, ModuleVorlagen, ModuleWetter

---

## [v0.12.5] — 2026-03-29

### Hinzugefügt
- **ADR.md** — 23 Architecture Decision Records aus allen Projekt-Chats
- **VISION.md** — Nordstern, Schmerzpunkte, Zielgruppe, Modulübersicht, Erfolgskriterien
- **DEPENDENCY-MAP.md** — Interne Solution-Struktur + externes Ökosystem mit Datenflüssen
- **CHANGELOG.md** — Komplette Versionshistorie rückwirkend ab v0.0.0

---

## [v0.12.4] — 2026-03-29

### Geändert
- **Settings:** TreeView mit Unterordnern im ProjectEditDialog — gleiches GUI für "Neues Projekt" und "Bearbeiten"
- Bestehende Ordner werden beim Bearbeiten von Disk gelesen und im TreeView angezeigt

### Dokumentation
- BACKLOG: Dashboard-Mockup (ASCII), neue Feature-Ideen, GIS Steiermark, Firmendaten-Verwaltung, Kalender-Integration

---

## [v0.12.3] — 2026-03-29

### Hinzugefügt
- **Settings:** Gelbe Folder-Browse-Buttons für BasePath und ArchivePath
- `Microsoft.Win32.OpenFolderDialog` für Ordnerauswahl (Feature #13 teilweise)

---

## [v0.12.2] — 2026-03-29

### Behoben
- **Settings:** Button-Beschriftungen korrekt neben Buttons ausgerichtet

---

## [v0.12.1] — 2026-03-29

### Behoben
- **Settings:** Projektliste aktualisiert sich jetzt nach dem Bearbeiten eines Projekts

---

## [v0.12.0] — 2026-03-29

### Hinzugefügt
- **Settings:** 2-Tab-Einstellungsseite — Tab 1: Projekte + Pfade, Tab 2: Standard-Ordnerstruktur
- Standard-Ordnerstruktur mit Unterordnern und Präfix ein/aus Schalter
- Status-Anzeige mit Farbpunkten: Aktiv (grün), Abgeschlossen (rot)

---

## [v0.11.3] — 2026-03-29

### Hinzugefügt
- **Settings:** Löschen-Button für Projekte mit Bestätigungsdialog

---

## [v0.11.2] — 2026-03-29

### Geändert
- **Settings:** Einheitlicher Dialog für "Neues Projekt" und "Bearbeiten" — gleiche GUI für beide Aktionen

---

## [v0.11.1] — 2026-03-29

### Geändert
- **Settings:** 2-Spalten ProjectEditDialog (1050x780) — links Projektdaten, rechts Ordnerstruktur
- Einstellungen-Seite Redesign mit klarerer Struktur

---

## [v0.11.0] — 2026-03-28 / 2026-03-29

### Hinzugefügt
- **Settings:** Automatische Projektordner-Erstellung mit konfigurierbarem Template (Feature #10)
- `FolderTemplateEntry` Modell — Nummern aus Listenposition, nicht gespeichert
- `ProjectFolderService` — erstellt nummerierte Ordner (z.B. "01 Planunterlagen") mit optionalen `_Eingang` Unterordnern
- 2-Spalten ProjectEditDialog mit Live-Vorschau TreeView der Ordnerstruktur
- PowerShell-Tool `Get-ProjektOrdner.ps1` im `Tools/`-Ordner zur Analyse bestehender Ordnerstrukturen

---

## [v0.10.1] — 2026-03-28

### Dokumentation
- BACKLOG nach Session-Abschluss aktualisiert

---

## [v0.10.0] — 2026-03-28

### Hinzugefügt
- **App + Infrastructure + Domain:** Ersteinrichtungs-Dialog (Feature #9)
- OneDrive-Pfad automatisch erkennen via `%OneDrive%` Umgebungsvariable
- Arbeitsordner und Archivordner konfigurieren
- `settings.json` wird bei Ersteinrichtung erstellt
- `SettingsService` für Laden/Speichern der Einstellungen

---

## [v0.9.3] — 2026-03-27

### Dokumentation
- Vollständiger V1-BACKLOG mit allen Features und Phasen

---

## [v0.9.2] — 2026-03-27

### Behoben
- **App:** Hilfsmodule und Export-Ordner aus Git-Tracking entfernt (Feature #8)

---

## [v0.9.1] — 2026-03-27

### Dokumentation
- Ersteinrichtung zum BACKLOG hinzugefügt

---

## [v0.9.0] — 2026-03-27

### Hinzugefügt
- **Infrastructure + Settings:** Automatischer registry.json Export (Feature #7)
- Flaches JSON-Format für VBA-Kompatibilität (Outlook/Excel-Makros)
- `RegistryJsonExporter` + `RegistryJsonMapper`
- Atomisches Schreiben (write-to-temp-then-rename)
- Export wird bei jeder Projektänderung automatisch ausgelöst

---

## [v0.8.3] — 2026-03-27

### Behoben
- **App:** Versionsnummer im Log-Output korrigiert auf v0.8.2

---

## [v0.8.2] — 2026-03-27

### Behoben
- **Infrastructure:** Auto-Increment IDs korrekt implementiert (Feature #6)
- Format: `proj_001`, `client_001`, `bldg_001`

---

## [v0.8.1] — 2026-03-27

### Dokumentation
- Arbeitszeiterfassungs-Modul zum BACKLOG hinzugefügt (Konzept: WPF → Excel via ClosedXML)

---

## [v0.8.0] — 2026-03-27

### Hinzugefügt
- **Infrastructure + Settings:** SQLite-Datenbank für persistente Projektspeicherung (Feature #5)
- `bpm.db` in `%LocalAppData%\BauProjektManager\`
- `SqliteConnectionFactory`, `ProjectRepository`
- Auto-Increment IDs für Projekte, Clients, Buildings

---

## [v0.7.1] — 2026-03-27

### Dokumentation
- BACKLOG.md erstellt — zentrale Featureliste mit Priorisierung

---

## [v0.7.0] — 2026-03-27

### Hinzugefügt
- **Domain + Settings:** Projekt-Bearbeitungsdialog mit allen Feldern (Feature #4)
- Client-Modell (Auftraggeber: Company, ContactPerson, Phone, Email)
- Aufgeteilte Adressfelder (Street, HouseNumber, PostalCode, City)
- Koordinaten, Grundstücksdaten, Verwaltungsdaten
- Gebäude-Verwaltung mit Geschoß-Listen
- Timeline (Projektstart, Baustart, Geplantes Ende, Tatsächliches Ende)

---

## [v0.6.0] — 2026-03-27

### Hinzugefügt
- **Settings:** Projektliste mit DataGrid, Testdaten und "Neues Projekt"-Button

---

## [v0.5.1] — 2026-03-27

### Hinzugefügt
- **Domain:** Kern-Domänenmodelle (Feature #3)
- `Project`, `ProjectLocation`, `ProjectTimeline`, `ProjectPaths`, `Client`, `Building`
- `ProjectStatus` Enum (Active, Completed, Archived)
- Projektnummer automatisch aus Projektstart-Datum (YYYYMM)

---

## [v0.5.0] — 2026-03-27

### Hinzugefügt
- **App:** Serilog Logging (Feature #2)
- File + Console Sinks, tägliche Rotation, 30 Tage Aufbewahrung

---

## [v0.4.1] — 2026-03-27

### Hinzugefügt
- **App + Settings + PlanManager:** Seitennavigation mit Content-Wechsel

---

## [v0.4.0] — 2026-03-27

### Hinzugefügt
- **App:** Hauptfenster (Shell) mit Sidebar-Navigation und Statusleiste (Feature #1)
- Dark Theme Grundlage

---

## [v0.3.0] — 2026-03-27

### Hinzugefügt
- NuGet-Pakete: CommunityToolkit.Mvvm, Microsoft.Extensions.DI, Serilog

---

## [v0.2.2] — 2026-03-27

### Hinzugefügt
- `.editorconfig` für einheitliche Code-Formatierung

---

## [v0.2.1] — 2026-03-27

### Hinzugefügt
- `Directory.Build.props` — zentrale Projektkonfiguration (.NET 10, Nullable)

---

## [v0.2.0] — 2026-03-27

### Hinzugefügt
- Feature-Modul-Projekte als WPF Class Libraries (Settings, PlanManager)

---

## [v0.1.1] — 2026-03-27

### Hinzugefügt
- Infrastructure-Projekt erstellt

---

## [v0.1.0] — 2026-03-27

### Hinzugefügt
- Initiale Solution-Struktur mit .NET 10 (5 Projekte)
- Dependency-Regel etabliert

---

## [v0.0.0] — 2026-03-26

### Hinzugefügt
- Repository erstellt
- Architektur-Dokument v1.2.0

---

## Dokumentations-Versionen

| Version | Datum | Dokument | Änderung |
|---------|-------|----------|----------|
| v1.2.0 | 2026-03-26 | Architektur | Erster Entwurf |
| v1.4.0 | 2026-03-27 | Architektur | Nach 2 Review-Runden, 13 Entscheidungen |
| v1.5.0 | 2026-03-27 | Architektur | .NET 10, Client-Modell, Adressfelder |
| v1.0.0 | 2026-03-27 | Coding Standards | Erstellt |
| v1.0.0 | 2026-03-29 | ADR | 23 Entscheidungen |
| v1.0.0 | 2026-03-29 | Vision | Nordstern + Produktstrategie |
| v1.0.0 | 2026-03-29 | Dependency Map | Solution + Ökosystem |
| v1.0.0 | 2026-03-29 | Changelog | Rückwirkend ab v0.0.0 |
| v0.2.0 | 2026-03-29 | ModuleFoto | Erweitert mit PhotoFolder V2 Referenz |
| v0.1.0 | 2026-03-29 | ModuleZeiterfassung | Erstellt |
| v0.1.0 | 2026-03-29 | ModuleGIS | Erstellt |
| v0.1.0 | 2026-03-29 | ModulePlanHeader | Erstellt (von Herbert) |
| v2.0.0 | 2026-03-29 | Changelog | v0.13.0–v0.15.0, Tab 1–4 |
| v2.0.0 | 2026-03-29 | Backlog | v0.15.0, KI-API-Import, Adressbuch-Trennung |
| v2.0.0 | 2026-03-30 | UI_UX_Guidelines | Komplettes Design-System nach Review |
| v1.0.0 | 2026-03-30 | WPF_UI_Architecture | Technischer UI-Aufbau |
| v1.0.0 | 2026-03-30 | UX_Flows | Hauptworkflows |
| v1.0.0 | 2026-03-30 | Glossar | Begriffsdefinitionen |
| v1.5.0 | 2026-03-30 | DB-Schema | Zentrales Leitdokument (Ist + geplant) |
| v1.1.0 | 2026-03-30 | ADR | 7 neue ADRs (028–034) |
| v1.0.0 | 2026-03-30 | CODING_STANDARDS | UI-Ergänzung |
| v2.0.0 | 2026-03-30 | Architektur | v1.5→v2.0.0 |
| v1.3.0 | 2026-04-03 | DSGVO-Architektur | Erstversion + 2 Reviews + IPrivacyPolicy |
| v1.2.0 | 2026-04-03 | ADR | ADR-035 + ADR-036 (36 Entscheidungen) |
| v1.1.0 | 2026-04-03 | CODING_STANDARDS | .NET 10 + Kapitel 17 Datenschutz |
| v1.4.0 | 2026-04-04 | DSGVO-Architektur | Dienststatus, Löschkonzept, Audit-Negativliste |
| v2.1.0 | 2026-04-04 | Architektur | Exportvertrag, Betriebsmodi, Privacy Layer, Cloud-neutral |
| v1.2.0 | 2026-04-04 | ADR | 42 ADRs, Statusmodell, 3 neue (040-042) |
| v2.1.0 | 2026-04-04 | DEPENDENCY-MAP | Geplante Services, Cloud-neutral |
| v2.1.0 | 2026-04-04 | UI_UX_Guidelines | 8 Review-Punkte (Auflösung, States, Feedback) |
| v2.0.0 | 2026-04-04 | WPF_UI_Architecture | Neufassung (15 Review-Punkte) |
| v1.5.1 | 2026-04-04 | DB-SCHEMA | TEXT-IDs, FK-Regel, Präfix-Tabelle (ADR-039) |
| — | 2026-04-04 | Settings/App XAML | Token-Migration: SettingsView, ProjectEditDialog, SetupDialog |

---

*Wird bei jedem Release aktualisiert.*
