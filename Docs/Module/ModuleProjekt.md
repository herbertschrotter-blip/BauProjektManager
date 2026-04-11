---
doc_id: module-projekt
doc_type: module
authority: source_of_truth
status: active
owner: herbert
topics: [projekt-crud, auftraggeber, bauteile, geschosse, beteiligte, ordnerstruktur, registry-export, einstellungen]
read_when: [projekt-feature, projektdialog, ordnerstruktur, registry-json, auftraggeber, bauteile-geschosse]
related_docs: [architektur, db-schema, ui-ux-guidelines, wpf-ui-architecture]
related_code: [src/BauProjektManager.Settings/, src/BauProjektManager.Infrastructure/Persistence/ProjectDatabase.cs, src/BauProjektManager.Domain/Models/]
supersedes: []
---

## AI-Quickload
- Zweck: Modul-Dokumentation für Projekt-CRUD, Einstellungen, Ordnerstruktur und registry.json-Export
- Autorität: source_of_truth
- Lesen wenn: Projekt-Feature, ProjectEditDialog, Ordnerstruktur, registry.json, Auftraggeber, Bauteile/Geschosse
- Nicht zuständig für: PlanManager (→ PlanManager.md), DB-Schema-Details (→ DB-SCHEMA.md), Architektur (→ Architektur.md)
- Pflichtlesen:
  - Kapitel 3 (Datenmodell) bei Projekt-Entitäts-Änderung
  - Kapitel 7 (Ordnerstruktur) bei FolderTemplate-Änderung
- Fachliche Invarianten:
  - ProjectNumber wird aus ProjectStart generiert (YYYYMM) — nie manuell
  - registry.json ist generierter Export — VBA liest nur, schreibt nie
  - FolderTemplateEntry-Nummern aus Listenposition, nicht gespeichert
  - Client ist eigene Entität mit ULID — kein einfacher String (ADR-021)

---

﻿# Projekt-Modul — Dokumentation

**Letzte Änderung:** v0.19.2  
**Status:** Implementiert  
**ADRs:** ADR-001, ADR-006, ADR-022, ADR-039

---

## 1. Übersicht

Das Projekt-Modul ist das Herzstück des BauProjektManager. Es verwaltet alle Bauprojekte: Anlegen, Bearbeiten, Löschen, Ordnerstruktur auf Disk, Persistenz in SQLite und Export als `registry.json` für VBA-Makros.

**Zuständigkeit:** Projekt-CRUD, Auftraggeber, Bauteile/Geschosse, Beteiligte, Portale/Links, Ordnerstruktur, Standard-Einstellungen (Pfade, Ordner-Template)

---

## 2. Architektur

### Schichtenverteilung

| Schicht | Datei | Verantwortung |
|---------|-------|---------------|
| Domain | `Models/Project.cs` | Zentrale Entität — Stammdaten, Referenzen, FolderName |
| Domain | `Models/Client.cs` | Auftraggeber/Bauherr |
| Domain | `Models/BuildingPart.cs` | Bauteil/Bauabschnitt mit Geschossen |
| Domain | `Models/BuildingLevel.cs` | Geschoss mit 3 Eingabewerten + 4 errechneten Werten |
| Domain | `Models/ProjectParticipant.cs` | Beteiligter (Rolle, Firma, Kontakt) |
| Domain | `Models/ProjectLink.cs` | Externes Portal / Link |
| Domain | `Models/ProjectLocation.cs` | Adresse, Koordinaten, Grundstück |
| Domain | `Models/ProjectTimeline.cs` | Zeitplan (4 Datumswerte) |
| Domain | `Models/ProjectPaths.cs` | Pfade relativ zu Root |
| Domain | `Models/AppSettings.cs` | Globale Einstellungen, Ordner-Template, editierbare Listen |
| Domain | `Models/BpmManifest.cs` | Portabler Projekt-Snapshot (.bpm-manifest) mit eigenen DTOs |
| Domain | `Enums/ProjectStatus.cs` | Active, Completed |
| Domain | `Interfaces/IDialogService.cs` | Abstraktion für Info/Warn/Error/Confirm Dialoge |
| Infrastructure | `Persistence/ProjectDatabase.cs` | SQLite Schema, CRUD für alle Tabellen |
| Infrastructure | `Persistence/RegistryJsonExporter.cs` | Flacher JSON-Export für VBA |
| Infrastructure | `Persistence/AppSettingsService.cs` | settings.json lesen/schreiben |
| Infrastructure | `Persistence/ProjectFolderService.cs` | Projektordner auf Disk erstellen/synchronisieren |
| Infrastructure | `Persistence/BpmManifestService.cs` | .bpm-manifest lesen/schreiben/scannen, Hidden+ReadOnly |
| App | `BpmDialogService.cs` | Implementation von IDialogService mit Dark Theme Dialogen |
| App | `BpmInfoDialog.xaml` + `.cs` | Eigene Info/Warn/Error MessageBox im BPM-Design |
| App | `BpmConfirmDialog.xaml` + `.cs` | Ja/Nein-Dialog im BPM-Design |
| Settings | `ViewModels/SettingsViewModel.cs` | ViewModel: Projekte, Pfade, Ordner-Template, Suche, Filter, Import |
| Settings | `Views/SettingsView.xaml` + `.cs` | 2-Tab-Seite (Projekte + Ordnerstruktur) |
| Settings | `Views/ProjectEditDialog.xaml` + `.cs` | 5-Tab-Dialog zum Anlegen/Bearbeiten |
| Settings | `Views/FolderTemplateControl.xaml` + `.cs` | Shared UserControl für Ordner-TreeView |

### Abhängigkeiten

```
Settings (Views + ViewModel)
  → Domain (Models, Enums)
  → Infrastructure (ProjectDatabase, AppSettingsService, ProjectFolderService, RegistryJsonExporter)
```

Keine zusätzlichen NuGet-Pakete über die Basis hinaus (Microsoft.Data.Sqlite, CommunityToolkit.Mvvm, Serilog, System.Text.Json).

---

## 3. Domain-Modelle

### Project (zentrale Entität)

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | Eindeutige ID (aktuell Präfix `proj_001`, geplant ULID per ADR-039) |
| `ProjectNumber` | `string` | Automatisch aus Projektstart: `YYYYMM` |
| `Name` | `string` | Kurzname (z.B. "ÖWG-Dobl-Zwaring") |
| `FullName` | `string` | Voller Projektname |
| `Status` | `ProjectStatus` | Active oder Completed |
| `ProjectType` | `string` | Aus editierbarer Liste (settings.json) |
| `Location` | `ProjectLocation` | Adresse + Koordinaten + Grundstück |
| `Timeline` | `ProjectTimeline` | 4 Datumswerte |
| `Client` | `Client` | Auftraggeber |
| `BuildingParts` | `List<BuildingPart>` | Bauteile mit Geschossen |
| `Participants` | `List<ProjectParticipant>` | Beteiligte Firmen/Personen |
| `Links` | `List<ProjectLink>` | Portale und Links |
| `Paths` | `ProjectPaths` | Pfade auf Disk |
| `Tags` | `string` | Freitext-Tags |
| `Notes` | `string` | Freitext-Notizen |
| `FolderName` | `string` (computed) | `{ProjectNumber}_{Name}` |

**Methoden:**
- `UpdateProjectNumberFromStart()` — Generiert Projektnummer aus `Timeline.ProjectStart` als `YYYYMM`

### Client

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Company` | `string` | Firmenname des Auftraggebers |
| `ContactPerson` | `string` | Ansprechperson |
| `Phone` | `string` | Telefon |
| `Email` | `string` | E-Mail |
| `Notes` | `string` | Notizen |

Vorbereitet für späteres Adressbuch und Outlook-Sync. Aktuell eingebettet im Projekt, in der DB als eigene `clients`-Tabelle mit FK.

### BuildingPart

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | ID (z.B. `bpart_001`) |
| `ShortName` | `string` | Kürzel (z.B. "BT-A", "TG") |
| `Description` | `string` | Beschreibung (z.B. "Stiege 1+2") |
| `BuildingType` | `string` | Aus editierbarer Liste (z.B. "Wohnanlage") |
| `ZeroLevelAbsolute` | `double` | ± 0,00 in Meter über Adria |
| `SortOrder` | `int` | Reihenfolge im UI |
| `Levels` | `List<BuildingLevel>` | Geschosse des Bauteils |

### BuildingLevel

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | ID (z.B. `blvl_001`) |
| `Prefix` | `int` | Sortier-Präfix (EG=0, UG=-01, OG1=01) |
| `Name` | `string` | Kurzname (z.B. "EG", "OG1", "DG") |
| `Description` | `string` | Langname (auto aus settings.json) |
| `Rdok` | `double` | Rohdecke Oberkante (von ± 0,00) |
| `Fbok` | `double` | Fertigfußboden Oberkante (von ± 0,00) |
| `Rduk` | `double?` | Rohdecke Unterkante (nullable, z.B. bei Bodenplatte) |
| `SortOrder` | `int` | Reihenfolge |

**Errechnete Werte (nicht in DB):**
- `DeckThickness` — Deckendicke: RDOK − RDUK
- `FloorBuildup` — Fußbodenaufbau: FBOK − RDOK
- `StoryHeight` — Geschosshöhe (berechnet im UI aus dem Geschoss darüber)
- `RawHeight` — Rohbauhöhe (berechnet im UI)

**Statische Methoden:**
- `GetAutoDescription(name, levelNames)` — Langbezeichnung aus Settings-Liste
- `GetNextLevelName(lastName, levelNames)` — Nächstes logisches Geschoss vorschlagen

### ProjectParticipant

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | ID (z.B. `ppart_001`) |
| `Role` | `string` | Rolle (Architekt, Statiker, ÖBA...) — editierbar (settings.json) |
| `Company` | `string` | Firmenname |
| `ContactPerson` | `string` | Ansprechperson |
| `Phone` | `string` | Telefon |
| `Email` | `string` | E-Mail |
| `SortOrder` | `int` | Reihenfolge |
| `ContactId` | `string` | Spätere FK auf zentrales Adressbuch (aktuell leer) |

### ProjectLink

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | ID (z.B. `plink_001`) |
| `Name` | `string` | Bezeichnung (z.B. "InfoRaum", "PlanRadar") |
| `Url` | `string` | URL |
| `LinkType` | `string` | "Portal" oder "Custom" |
| `SortOrder` | `int` | Reihenfolge |
| `IsConfigured` | `bool` (computed) | Ob URL vorhanden |

### ProjectLocation

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| Straße/Hausnummer/PLZ/Ort | `string` | Adresse |
| Municipality/District/State | `string` | Verwaltung (Default: "Steiermark") |
| CoordinateSystem | `string` | Default: "EPSG:31258" (BMN M34) |
| CoordinateEast/North | `double` | Koordinaten |
| CadastralKg/KgName/Gst | `string` | Grundstücks-Daten |
| `FormattedAddress` | `string` (computed) | "Straße Nr, PLZ Ort" |

### ProjectTimeline

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `ProjectStart` | `DateTime?` | Projektstart (generiert Projektnummer) |
| `ConstructionStart` | `DateTime?` | Baubeginn |
| `PlannedEnd` | `DateTime?` | Geplantes Ende |
| `ActualEnd` | `DateTime?` | Tatsächliches Ende |

### ProjectPaths

| Property | Typ | Default |
|----------|-----|---------|
| `Root` | `string` | (leer, wird beim Anlegen gesetzt — ADR-049: wird zu berechnetem Pfad) |
| `Plans` | `string` | Aus FolderTemplate, z.B. "01 Planunterlagen" |
| `Inbox` | `string` | Aus FolderTemplate, z.B. "01 Planunterlagen\\_Eingang" |
| `Photos` | `string` | Aus FolderTemplate, z.B. "02 Fotos" |
| `Documents` | `string` | Aus FolderTemplate, z.B. "03 Dokumente" |
| `Protocols` | `string` | Aus FolderTemplate, z.B. "04 Protokolle" |
| `Invoices` | `string` | Aus FolderTemplate, z.B. "05 Rechnungen" |

---

## 4. Datenbank (ProjectDatabase)

SQLite-Datenbank `bpm.db` in `%LocalAppData%\BauProjektManager\`.

### Tabellen

| Tabelle | Beziehung | Beschreibung |
|---------|-----------|-------------|
| `clients` | 1:N zu projects | Auftraggeber |
| `projects` | Zentral | Alle Projektdaten inkl. Location, Timeline, Paths |
| `building_parts` | N:1 zu projects | Bauteile pro Projekt |
| `building_levels` | N:1 zu building_parts | Geschosse pro Bauteil |
| `project_participants` | N:1 zu projects | Beteiligte pro Projekt |
| `project_links` | N:1 zu projects | Links pro Projekt |
| `schema_version` | Singleton | DB-Schema-Version für Migrationen |

### ID-Schema

Aktuell: Auto-Increment mit Präfix (`proj_001`, `client_001`, `bpart_001`, `blvl_001`, `ppart_001`, `plink_001`).

Geplant (ADR-039 v2): Migration auf ULID als TEXT PRIMARY KEY. NuGet `Cysharp/Ulid`, Erzeugung über `IIdGenerator.NewId()`. Noch nicht implementiert.

### Wichtige Methoden

| Methode | Beschreibung |
|---------|-------------|
| `SaveProject(Project)` | INSERT OR UPDATE (Upsert) — speichert Projekt + Client + alle Kindtabellen |
| `LoadAllProjects()` | Lädt alle Projekte mit allen Relationen |
| `DeleteProject(string id)` | Löscht Projekt und alle zugehörigen Daten (CASCADE) |
| `EnsureTables()` | Erstellt Tabellen beim ersten Start |
| `MigrateSchema()` | Schema-Migrationen (aktuell v1 → v2 für neue Spalten) |

### Einstellungen (WAL-Modus)

Die DB verwendet WAL-Modus (`PRAGMA journal_mode=WAL`) für bessere Concurrency. Wird beim Verbindungsaufbau gesetzt.

---

## 5. Infrastructure-Services

### AppSettingsService

Liest und schreibt `settings.json` in `%LocalAppData%\BauProjektManager\`.

**Inhalt von settings.json:**
- `BasePath` — Arbeitsordner (wo Projektordner erstellt werden)
- `ArchivePath` — Archivordner
- `OneDrivePath` — Cloud-Speicher-Pfad
- `IsFirstRun` — Ersteinrichtung-Flag
- `FolderTemplate` — Standard-Ordnerstruktur (Hauptordner + Unterordner + Präfix-Schalter + Inbox-Flag)
- `ProjectTypes` — Editierbare Liste (Neubau, Sanierung, Umbau...)
- `BuildingTypes` — Editierbare Liste (Wohnanlage, EFH, Gewerbe...)
- `LevelNames` — Geschoss-Kurzname → Langname Mapping
- `ParticipantRoles` — Rollen-Liste (Architekt, Statiker, ÖBA...)
- `PortalTypes` — Portal-Typen (InfoRaum, PlanRadar, SharePoint...)

### ProjectFolderService

Erstellt Projektordner auf Disk basierend auf dem Ordner-Template.

| Methode | Beschreibung |
|---------|-------------|
| `CreateProjectFolders(Project, List<FolderTemplateEntry>?)` | Erstellt den kompletten Ordnerbaum beim Projekt-Anlegen. Gibt Root-Pfad zurück. |
| `SyncNewFolders(Project, List<FolderTemplateEntry>)` | Erstellt nur neue Ordner bei bestehendem Projekt. **Löscht nie** bestehende Ordner. |

**Ordner-Format:** Hauptordner immer nummeriert (`00 Pläne`, `01 Fotos`, ...). Unterordner optional nummeriert (Präfix-Schalter). Inbox-Unterordner heißt `_Eingang`.

### RegistryJsonExporter

Exportiert alle Projekte als flaches JSON nach `{AppRoot}/Export/registry.json`.

**Zweck:** VBA-Makros in Excel und Outlook können die JSON-Datei lesen um Projektdaten zu verwenden (Pfade, Projektnummern, Adressen).

**Format:** Array von flachen Objekten (kein Nesting), alle Felder als Strings. Atomic Write über temp-Datei + Rename.

---

## 6. UI: SettingsView (2-Tab-Seite)

### Tab 1 — Projekte

| Element | Beschreibung |
|---------|-------------|
| Suchfeld | Oben links, Platzhalter "🔍 Suche in Projekten...", durchsucht Name, FullName, Projektnummer, Client, Ort, Tags, Notes (300ms Debounce) |
| Statusfilter | Toggle-Buttons rechts: Alle (blau aktiv) / Aktiv / Abgeschlossen — filtert über CollectionView |
| Filterinfo | Unter der Liste: "4 Projekte geladen" oder "1 von 4 Projekten" |
| Projektliste | DataGrid bindet an gefilterte ProjectsView, Spalten: Projekt, Nr., Status (● Aktiv grün, ● Abgeschlossen rot), Pfad |
| ＋ Neues Projekt | Popup-Button mit 2 Optionen: "📝 Projekt erstellen" und "📂 Projekt importieren" |
| Bearbeiten | Öffnet ProjectEditDialog. Zeigt BPM-Dialog wenn kein Projekt ausgewählt. |
| Archivieren | Vorbereitet, disabled |
| Löschen | Rot, mit BPM-Bestätigungsdialog. Zeigt BPM-Dialog wenn kein Projekt ausgewählt. Löscht aus DB, nicht von Disk. |
| Pfade-Bereich | Unten verankert: Arbeitsordner, Archivordner, Cloud-Speicher (mit 📁-Buttons) |
| Statusleiste | Zeigt Anzahl geladener Projekte / Export-Status |

### Tab 2 — Standard-Ordnerstruktur

| Element | Beschreibung |
|---------|-------------|
| TreeView | Hauptordner mit aufklappbaren Unterordnern |
| ▲▼ Buttons | Hauptordner verschieben |
| ＋ Button | Hauptordner hinzufügen (blau) |
| ＋└ Button | Unterordner hinzufügen (grün) |
| ✕ Button | Ordner entfernen (rot) |
| 📥 Button | _Eingang ein/aus (nur Hauptordner) |
| ## Button | Präfix ein/aus (nur Unterordner) |

Änderungen werden sofort in `settings.json` gespeichert.

---

## 7. UI: ProjectEditDialog (5-Tab-Dialog, 1050×780)

### Tab 1 — Stammdaten (2-Spalten)

| Linke Spalte | Rechte Spalte |
|-------------|--------------|
| Projektstart (DatePicker) → generiert Projektnummer | Projektart (ComboBox, editierbar) |
| Kurzname | Status (Active/Completed) |
| Voller Projektname | |
| Auftraggeber: Firma, Kontakt, Telefon, E-Mail | |
| Adresse: Straße, Nr, PLZ, Ort | |
| Verwaltung: Gemeinde, Bezirk, Bundesland | |
| Koordinaten: System, Ostwert, Nordwert | |
| Grundstück: KG, KG-Name, GST-Nr | |

### Tab 2 — Bauwerk

| Element | Beschreibung |
|---------|-------------|
| Bauteile-Liste | DataGrid: Kürzel, Beschreibung, Typ, ± 0,00 |
| Geschoss-Liste | DataGrid: Name, Beschreibung, RDOK, FBOK, RDUK + errechnete Werte |
| Live-Berechnung | Deckendicke, Fußbodenaufbau, Geschosshöhe, Rohbauhöhe — aktualisiert bei Eingabe |
| Geschoss-Vorschlag | Nächstes logisches Geschoss aus settings.json (UG → EG → OG1 → ...) |
| CRUD-Buttons | Bauteil/Geschoss hinzufügen, entfernen, ▲▼ verschieben |

### Tab 3 — Beteiligte

| Element | Beschreibung |
|---------|-------------|
| Beteiligte-Liste | DataGrid: Rolle (Dropdown), Firma, Kontakt, Telefon, E-Mail |
| CRUD-Buttons | Hinzufügen, Entfernen, ▲▼ verschieben |
| Rollen-Dropdown | Editierbare Liste aus settings.json (Architekt, Statiker, ÖBA, Haustechnik...) |
| Import | Vorbereitet für Adressbuch-Import (noch nicht implementiert) |

### Tab 4 — Portale + Links (2-Spalten)

| Linke Spalte | Rechte Spalte |
|-------------|--------------|
| Portale aus settings.json | Eigene Links (Custom) |
| URL-Eingabe pro Portal | Hinzufügen/Entfernen |
| Browser-Öffnen Button | Vorschau der konfigurierten Links |

### Tab 5 — Ordnerstruktur

| Modus | Verhalten |
|-------|-----------|
| **Neues Projekt** | Zeigt Ordner-Template aus settings.json im FolderTemplateControl. User kann anpassen vor dem Speichern. Ordner werden beim Speichern auf Disk erstellt. |
| **Bestehendes Projekt** | Liest bestehende Ordner von Disk. Zeigt sie im TreeView mit IsExisting-Flag (Löschschutz). Neue Ordner können hinzugefügt werden (SyncNewFolders). Bestehende Ordner können nicht gelöscht werden. |

---

## 8. FolderTemplateControl (Shared UserControl)

Gemeinsame Komponente, verwendet in:
- **SettingsView Tab 2** — globales Standard-Template bearbeiten
- **ProjectEditDialog Tab 5** — pro Projekt anpassen

### Datenmodell

| Klasse | Properties | Beschreibung |
|--------|-----------|-------------|
| `FolderTemplateEntry` | Name, HasInbox, SubFolders | Ein Hauptordner im Template |
| `SubFolderEntry` | Name, HasPrefix | Ein Unterordner (beliebig verschachtelt) |
| `FolderTreeItem` | Name, Position, IsMainFolder, HasPrefix, HasInbox, IsExisting, Children | UI-ViewModel für TreeView |

### IsExisting-Flag (v0.19.2)

Beim Bearbeiten bestehender Projekte werden von Disk gelesene Ordner mit `IsExisting = true` markiert. Diese Ordner:
- Können im TreeView nicht entfernt werden (Löschschutz)
- Werden beim Speichern nicht erneut erstellt
- Nur neue Ordner (IsExisting = false) werden via SyncNewFolders erstellt

---

## 9. Datenfluss

### Neues Projekt anlegen

```
1. User klickt "+ Neues Projekt"
2. SettingsViewModel.AddProject() → ProjectEditDialog öffnet mit leerem Project + FolderTemplate aus settings.json
3. User füllt 5 Tabs aus, passt Ordnerstruktur an
4. Speichern:
   a. ProjectFolderService.CreateProjectFolders() → erstellt Ordner auf Disk
   b. Project.Paths.Root wird gesetzt
   c. ProjectDatabase.SaveProject() → speichert in SQLite
   d. RegistryJsonExporter.Export() → aktualisiert registry.json
```

### Bestehendes Projekt bearbeiten

```
1. User wählt Projekt, klickt "Bearbeiten"
2. SettingsViewModel.EditProject() → ProjectEditDialog öffnet mit geladenem Project
3. Tab 5 liest Ordner von Disk, markiert als IsExisting
4. Speichern:
   a. ProjectFolderService.SyncNewFolders() → erstellt nur neue Ordner
   b. ProjectDatabase.SaveProject() → aktualisiert in SQLite
   c. RegistryJsonExporter.Export() → aktualisiert registry.json
```

### Registry-Export (VBA-Interop)

```
ProjectDatabase (SQLite) → RegistryJsonExporter → Export/registry.json → VBA-Makros (Excel, Outlook)
```

---

## 10. Editierbare Listen (settings.json)

Diese Listen sind vom User anpassbar und werden als ComboBox-Quellen im Dialog verwendet:

| Liste | Default-Werte | Verwendet in |
|-------|--------------|-------------|
| `ProjectTypes` | Neubau, Sanierung, Umbau, Zubau, Abbruch | Tab 1 Projektart |
| `BuildingTypes` | Wohnanlage, EFH, Reihenhaus, Gewerbe, Industrie, Infrastruktur | Tab 2 Bauteil-Typ |
| `LevelNames` | UG2→UG→EG→OG1→...→DG | Tab 2 Geschoss-Vorschlag + Beschreibung |
| `ParticipantRoles` | Architekt, Statiker, ÖBA, Haustechnik, Bauphysik... | Tab 3 Rollen-Dropdown |
| `PortalTypes` | InfoRaum, PlanRadar, SharePoint, Confluence | Tab 4 Portal-Auswahl |

---

## 11. Bekannte Einschränkungen

- **Kein DI-Container** — SettingsViewModel instanziiert Services manuell (`new ProjectDatabase()`). Migration auf DI geplant nach PlanManager.
- **ProjectEditDialog.xaml.cs ist zu groß** (~46 KB, ~1200 Zeilen) — Refactoring/Split geplant aber depriorisiert hinter PlanManager.
- **Code-Behind statt reines MVVM** — Der ProjectEditDialog verwendet Code-Behind für Tab-Logik. Akzeptabler Kompromiss für die Komplexität des 5-Tab-Dialogs.
- **FolderTemplateControl Bug** — beim Bearbeiten bestehender Projekte "funktioniert noch nicht ganz so wie gewollt" (Herbert-Feedback, noch nicht debuggt).
- **Suche durchsucht keine Bauteile/Geschosse** — nur Stammdaten, Auftraggeber, Adresse, Tags.
- **Archivieren nicht implementiert** — Button existiert, ist disabled.
- **Adressbuch fehlt** — Clients sind aktuell pro Projekt eingebettet. Zentrales Adressbuch mit Wiederverwendung geplant.
- **ULID-Migration ausstehend** — Alle IDs sind noch Präfix-basiert (ADR-039 v2 beschlossen, Code-Umbau noch nicht durchgeführt).

---

## 12. Geplante Verbesserungen

| Verbesserung | Beschreibung | Status |
|-------------|-------------|--------|
| DI-Container | Services über DI statt manuellem `new` | Geplant nach PlanManager |
| ProjectEditDialog Split | .xaml.cs aufteilen (Tab-Handler als Partial Classes oder eigene Klassen) | Geplant, depriorisiert |
| ULID-Migration | Alle IDs auf ULID umstellen (IIdGenerator, Cysharp/Ulid) | ADR-039 v2 beschlossen |
| Adressbuch | Zentrale contacts-Tabelle, Wiederverwendung über Projekte | Backlog |
| Suchfeld Projektliste | Suchfeld + Statusfilter mit CollectionView | ✅ v0.22.0 |
| 2-Spalten Layout Dialog | Breitere rechte Spalte, mehr Platz für Ordnerstruktur | Backlog |
| Token-Migration | Hardcoded Colors → DynamicResource Token-Referenzen | Teilweise erledigt, Rest geplant |
| FolderTemplateControl Fix | Bug beim Bearbeiten bestehender Projekte beheben | Offen |

---

## 13. Dateien

```
src/
├── BauProjektManager.Domain/
│   ├── Models/
│   │   ├── Project.cs                    (56 Zeilen)
│   │   ├── Client.cs                     (15 Zeilen)
│   │   ├── BuildingPart.cs               (42 Zeilen)
│   │   ├── BuildingLevel.cs              (90 Zeilen)
│   │   ├── ProjectParticipant.cs         (44 Zeilen)
│   │   ├── ProjectLink.cs               (37 Zeilen)
│   │   ├── ProjectLocation.cs            (46 Zeilen)
│   │   ├── ProjectTimeline.cs            (11 Zeilen)
│   │   ├── ProjectPaths.cs               (15 Zeilen)
│   │   └── AppSettings.cs               (~200 Zeilen, inkl. FolderTemplate + editierbare Listen)
│   └── Enums/
│       └── ProjectStatus.cs              (8 Zeilen)
├── BauProjektManager.Infrastructure/
│   └── Persistence/
│       ├── ProjectDatabase.cs            (~900 Zeilen, Schema + CRUD)
│       ├── RegistryJsonExporter.cs       (~180 Zeilen)
│       ├── AppSettingsService.cs         (~140 Zeilen)
│       └── ProjectFolderService.cs       (~160 Zeilen)
└── BauProjektManager.Settings/
    ├── ViewModels/
    │   └── SettingsViewModel.cs          (~480 Zeilen)
    └── Views/
        ├── SettingsView.xaml             (~290 Zeilen)
        ├── SettingsView.xaml.cs          (~95 Zeilen)
        ├── ProjectEditDialog.xaml        (~1400 Zeilen, 5 Tabs)
        ├── ProjectEditDialog.xaml.cs     (~1200 Zeilen)
        ├── FolderTemplateControl.xaml    (~220 Zeilen)
        └── FolderTemplateControl.xaml.cs (~400 Zeilen)
```

---

## 14. Verwandte Entscheidungen

| ADR | Titel | Relevanz |
|-----|-------|----------|
| ADR-001 | Modularer Monolith | Settings als eigenes Class Library Projekt |
| ADR-006 | ID-Schema mit Präfix | proj_001, client_001 etc. (wird durch ADR-039 abgelöst) |
| ADR-022 | Dateinamen-Konvention | Beeinflusst FolderName-Format |
| ADR-035 | IExternalCommunicationService | Zukünftig für Adressbuch-Sync relevant |
| ADR-036 | Privacy Policy | RelaxedPrivacyPolicy für lokale Daten |
| ADR-039 | ULID als Primärschlüssel | Geplante Migration aller IDs |

---

## 15. Änderungshistorie

| App-Version | Änderung |
|-------------|----------|
| v0.5.1 | Domain-Modelle erstellt (Project, Client, Location, Timeline) |
| v0.6.0 | Projektliste in SettingsView |
| v0.7.0 | ProjectEditDialog (1-Spalte, Stammdaten + Auftraggeber + Adresse) |
| v0.8.0 | SQLite-Datenbank (ProjectDatabase, bpm.db) |
| v0.8.2 | Auto-Increment IDs mit Präfix |
| v0.9.0 | registry.json Export (RegistryJsonExporter) |
| v0.10.0 | Ersteinrichtung (SetupDialog, AppSettingsService, settings.json) |
| v0.11.0 | Projektordner erstellen (ProjectFolderService, FolderTemplateEntry) |
| v0.11.1 | 2-Spalten ProjectEditDialog (1050×780) |
| v0.11.2 | Einheitlicher Dialog für Neu/Bearbeiten |
| v0.11.3 | Löschen-Button mit Bestätigungsdialog |
| v0.12.0 | 2-Tab-Einstellungen, Unterordner mit Präfix-Schalter |
| v0.12.3 | 📁-Buttons für Pfade (OpenFolderDialog) |
| v0.12.4 | TreeView mit Unterordnern im ProjectEditDialog |
| v0.13.0 | Tab 1: 2-Spalten Stammdaten, ProjectType, DatePicker, Status |
| v0.13.2 | Tab 2: BuildingPart + BuildingLevel, Live-Berechnung, Geschoss-Vorschlag |
| v0.14.0 | Tab 3: ProjectParticipant CRUD, Rollen-Dropdown |
| v0.15.0 | Tab 4: Portale + Links, Browser-Öffnen, Vorschau |
| v0.16.0 | Theme-System, Einstellungen-Dialog (5-Tab) |
| v0.16.1 | Token-basiertes Dark Theme |
| v0.19.0 | FolderTemplateControl als Shared UserControl (DRY) |
| v0.19.1 | Button-Größen Fix in FolderTemplateControl |
| v0.19.2 | IsExisting-Flag: Löschschutz für bestehende Ordner |
| v0.20.0 | BpmManifest + BpmManifestService: .bpm-manifest lesen/schreiben/scannen. Projekt-Import (Ordner + Manifest Auto-Erkennung). Manifest wird bei Add/Edit automatisch geschrieben. |
| v0.21.0 | IDialogService + BpmDialogService: eigene Dark Theme Dialoge statt MessageBox. Popup-Button "＋ Neues Projekt" mit 2 Optionen. Hinweis-Dialog bei Bearbeiten/Löschen ohne Auswahl. |
| v0.22.0 | Projektsuche (Suchfeld mit Platzhalter, 300ms Debounce, durchsucht Name/FullName/Nr/Client/Ort/Tags). Statusfilter (Toggle-Buttons Alle/Aktiv/Abgeschlossen, CollectionView). Filterinfo-Anzeige. |
