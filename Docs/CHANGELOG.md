# BauProjektManager — Changelog

Alle Änderungen am Projekt, chronologisch dokumentiert.  
Format: [Keep a Changelog](https://keepachangelog.com/de/1.0.0/), Semantic Versioning.

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
- **Settings:** Gelbe 📁 Folder-Browse-Buttons für BasePath und ArchivePath
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
- Status-Anzeige mit Farbpunkten: ● Aktiv (grün), ● Abgeschlossen (rot), ● Archiviert (grau)

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
- **Settings:** 2-Spalten ProjectEditDialog (1050×780) — links Projektdaten, rechts Ordnerstruktur
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
- `RegistryJsonExporter` + `RegistryJsonMapper` — verschachteltes Domänenmodell → flacher Export
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
- Projekte werden in SQLite gespeichert statt nur im Speicher
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
- Aufgeteilte Adressfelder (Street, HouseNumber, PostalCode, City) für spätere Google Maps API
- Koordinaten, Grundstücksdaten (KG, GST), Verwaltungsdaten (Gemeinde, Bezirk)
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
- `Project`, `ProjectLocation`, `ProjectTimeline`, `ProjectPaths`
- `Client`, `Building`
- `ProjectStatus` Enum (Active, Completed, Archived)
- Projektnummer automatisch aus Projektstart-Datum (YYYYMM)

---

## [v0.5.0] — 2026-03-27

### Hinzugefügt
- **App:** Serilog Logging (Feature #2)
- File + Console Sinks, tägliche Rotation, 30 Tage Aufbewahrung
- Structured Logging mit `{PropertyName}` Platzhaltern
- Log-Dateien in `%LocalAppData%\BauProjektManager\Logs\`

---

## [v0.4.1] — 2026-03-27

### Hinzugefügt
- **App + Settings + PlanManager:** Seitennavigation mit Content-Wechsel
- Sidebar-Buttons wechseln zwischen Einstellungen und PlanManager

---

## [v0.4.0] — 2026-03-27

### Hinzugefügt
- **App:** Hauptfenster (Shell) mit Sidebar-Navigation und Statusleiste (Feature #1)
- Dark Theme Grundlage
- MainWindow mit ContentFrame für Modul-Anzeige

---

## [v0.3.0] — 2026-03-27

### Hinzugefügt
- **App:** NuGet-Pakete eingerichtet
- CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Serilog

---

## [v0.2.2] — 2026-03-27

### Hinzugefügt
- `.editorconfig` für einheitliche Code-Formatierung

---

## [v0.2.1] — 2026-03-27

### Hinzugefügt
- `Directory.Build.props` — zentrale Projektkonfiguration (.NET 10, Nullable, ImplicitUsings)

---

## [v0.2.0] — 2026-03-27

### Hinzugefügt
- **Settings + PlanManager:** Feature-Modul-Projekte als WPF Class Libraries erstellt

---

## [v0.1.1] — 2026-03-27

### Hinzugefügt
- **Infrastructure:** Projekt erstellt (SQLite, JSON, FileSystem, Logging)

---

## [v0.1.0] — 2026-03-27

### Hinzugefügt
- **App + Domain + Infrastructure:** Initiale Solution-Struktur mit .NET 10
- 5 Projekte: App (EXE), Domain, Infrastructure, Settings, PlanManager
- Dependency-Regel etabliert: Domain → nichts, Infrastructure → Domain, Module → Domain + Infrastructure

---

## [v0.0.0] — 2026-03-26

### Hinzugefügt
- Repository erstellt
- Architektur-Dokument v1.2.0 (erster Entwurf)

---

## Dokumentations-Versionen (separate Versionierung)

| Version | Datum | Dokument | Änderung |
|---------|-------|----------|----------|
| v1.2.0 | 2026-03-26 | Architektur | Erster Entwurf |
| v1.4.0 | 2026-03-27 | Architektur | Nach 2 Review-Runden (ChatGPT + Claude), 13 Entscheidungen |
| v1.5.0 | 2026-03-27 | Architektur | .NET 10, Client-Modell, Adressfelder, Manifest |
| v1.0.0 | 2026-03-27 | Coding Standards | Erstellt |
| v1.0.0 | 2026-03-29 | ADR | 23 Entscheidungen dokumentiert |
| v1.0.0 | 2026-03-29 | Vision | Nordstern + Produktstrategie |
| v1.0.0 | 2026-03-29 | Dependency Map | Solution + Ökosystem |
| v1.0.0 | 2026-03-29 | Changelog | Rückwirkend ab v0.0.0 erstellt |

---

*Wird bei jedem Release aktualisiert.*
