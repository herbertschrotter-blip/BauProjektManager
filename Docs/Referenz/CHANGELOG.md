---
doc_id: changelog
doc_type: changelog
authority: source_of_truth
status: active
owner: herbert
topics: [versionshistorie, changelog, releases, semantic-versioning]
read_when: [version-nachschlagen, was-hat-sich-geändert, release-notes]
related_docs: [backlog, adr]
related_code: [Directory.Build.props]
supersedes: []
---

## AI-Quickload
- Zweck: Chronologische Versionshistorie aller Änderungen am BPM-Projekt
- Autorität: source_of_truth
- Lesen wenn: Version nachschlagen, Änderungen prüfen, Release-Notes
- Nicht zuständig für: Feature-Planung (→ BACKLOG.md), Architektur-Entscheidungen (→ ADR.md)
- Pflichtlesen: keine (gezieltes Nachschlagen per Version)
- Fachliche Invarianten:
  - Keep-a-Changelog Format, chronologisch absteigend (neueste oben)
  - Semantic Versioning: PATCH=Fix, MINOR=Feature, MAJOR=Breaking Change
  - Jeder Commit = ein Eintrag mit Version, Datum und Beschreibung

---

﻿# BauProjektManager — Changelog

Alle Änderungen am Projekt, chronologisch dokumentiert.  
Format: [Keep a Changelog](https://keepachangelog.com/de/1.0.0/), Semantic Versioning.

---

## [v0.28.182] — 2026-09-02

### Refactor: BPM-069 — AppSettings-Fassade und settings.json-Legacy abgebaut

Die Übergangsklasse `AppSettings` (mit `OneDrivePath`, ADR-004-Verstoß) ist gelöscht — sie war seit dem Settings-Split (ADR-047 P.8) nur noch eine Fassade, die `AppSettingsService.Load()` bei jedem Aufruf aus `DeviceSettings` + `SharedConfig` zusammenbaute und `Save(AppSettings)` wieder zerlegte. Alle Aufrufer arbeiten jetzt direkt mit den Split-Modellen: `LoadDevice`/`SaveDevice` für Pfade, Gerät und Benutzer, neue Komfortmethoden `LoadSharedOrDefault`/`SaveSharedOrDefault` für FolderTemplate, Listen und Rollen (ohne BasePath Defaults, nichts wird gecacht). Umgestellt: `ProjectEditDialog` (16 Load-/4 Save-Stellen, Signaturen auf `SharedConfig`), `SettingsViewModel`, `App.xaml.cs` (DI ohne `AppSettings`, `InitializePersistenceRegistry(DeviceSettings)`), `SetupDialog` (`DeviceSettings`, `CloudStoragePath`), `ProjectFolderService`, `LocalUserContext`, `DocumentTypeSeedService`. `LocalUserId`/`LocalUserName` leben jetzt in `DeviceSettings` (waren in der Fassade nie persistiert). Die Template-/Listen-Klassen `FolderTemplateEntry`, `SubFolderEntry`, `LevelNameEntry`, `FolderTemplateCategory` sind unverändert nach `FolderTemplateEntry.cs` gezogen. Damit ist auch **BPM-068** (OneDrivePath → CloudStoragePath) erledigt.

**Legacy `settings.json`:** kein Migrationscode mehr (`MigrateFromLegacy`, `LoadLegacySettingsFile`, `TryLoadSharedFromLegacy`, `DetectOneDrivePath`, `ValidatePaths(AppSettings)` entfernt) — eine noch vorhandene Datei wird beim ersten `LoadDevice()` gelöscht (Frühphase). `PersistenceRegistry` kennt „settings.json (legacy)" nicht mehr. Docs: DB-SCHEMA 10.2/10.4/10.7 + Dateitabelle, ModuleProjekt.md, ADR-047 P.8. Solution baut, 516/516 Tests grün.

---
## [v0.28.181] — 2026-09-02

### Feature: BPM-067 — klappbare Sidebar (220px Text ↔ 56px Icons)

Die Shell-Sidebar lässt sich per Chevron im Kopf zwischen **Zustand A** (220px, Emoji + Text, Badge neben dem Text) und **Zustand B** (56px, nur Emoji mit Tooltip, Badge als Ecke oben rechts am Icon) umschalten. Default beim ersten Start: aufgeklappt; der Zustand wird gerätelokal in `DeviceSettings.UiLayout.SidebarCollapsed` gemerkt (gleiches Muster wie die Panel-Breiten). Unten zeigt die aufgeklappte Leiste die App-Version aus der Assembly. `MainWindow.xaml` neu aufgebaut (DockPanel: Kopf mit Titel + Toggle, Navigation, Versionszeile; `ColumnDefinition` per Name umschaltbar), Zustandslogik in `ApplySidebarState`/`ApplyNavButtonState`, Tooltips über `BpmToolTip`. `Icons.xaml` um die Chevrons und die Emoji-Keys aller geplanten Module aus dem Shell-Mockup ergänzt (`IconNav*`), damit Mockup und Code dieselben Zeichen nutzen. Bewusst **nicht** gebaut: Home und die ausgegrauten Platzhalter-Module — sie kommen mit ihren Views bzw. den Ansichtsprofilen (Architektur Kap. 1.4); keine Animation. Docs: UI_Navigation.md Kap. 2 und UI_UX_Guidelines 6.2 + Delta-Tabelle auf „klappbar" gestellt.

---
## [v0.28.180] — 2026-09-02

### Docs: BPM-067 — Mockup klappbare Sidebar (App/01_Shell)

BPM-067 wurde in Teil 52 umgeschnitten: statt eines festen Umbaus auf die 56px-Icon-Leiste aus UI_Navigation.md wird die Sidebar **klappbar** — Zustand A aufgeklappt (220px, Emoji + Text, Badge neben dem Text, Default beim ersten Start) und Zustand B zugeklappt (56px, Emoji mit Tooltip, Badge als Ecke am Icon), umschaltbar per Chevron, Zustand gerätelokal. Neuer Mockup-Modul-Ordner `Docs/Mockups/App/` mit `01_Shell/01_Sidebar.html` (Klick-Umschaltung, Navigation zu PlanManager/Settings-Mockups) und eigener `_SITEMAP.md`. Sidebar-Inhalt nach Entscheidung Herbert: 🏠 Home oben, 📁 PlanManager, dann alle Post-V1-Module ausgegraut (Fotos, Zeiterfassung, Bautagebuch, Kalkulation, Wetter, Outlook, Vorlagen, Aufgaben, GIS, KI-Assistent — Sichtbarkeit später über Ansichtsprofile, Architektur Kap. 1.4), unten ⚙ Einstellungen und 🛠 Dev Tools, darunter Benutzer + Version. Fünf Emoji (🏠 🧮 📋 🗺 🤖) sind neu und beim WPF-Umbau in `Icons.xaml` anzulegen. WPF-Umsetzung folgt als BPM-067 Schritt 2.

---
## [v0.28.179] — 2026-09-02

### Fix: BPM-066 (Teil 1) — versteckter `.bpm/`-Ordner erschien in der Ordnerstruktur

Altlasten-Prüfung Teil 52: Der nie reproduzierte „Editing Bug" im `FolderTemplateControl` hat drei konkrete Ursachen. Die erste ist behoben: Beim Bearbeiten eines bestehenden Projekts las das Control alle Unterordner von der Platte, auch den versteckten `.bpm/`-Ordner (ADR-046). Der stand als Hauptordner „.bpm" auf Position 0 und verschob die Präfixe aller weiteren Ordner um eins — neue Ordner bekamen ein zu hohes Präfix (das „00 .bpm, 01 Sonstiges"-Muster aus dem BPM-094-Befund). Jetzt werden versteckte Ordner und Punkt-Ordner (`.bpm`, `.bpm_tmp`, `.git`) auf allen Ebenen übersprungen. Dafür neue Port-Methode `IFileSystemReader.IsHidden(path)` (ADR-060; `LocalFileSystem` via Attribut, `FakeFileStore.SetHidden` für Tests) — die View bleibt `System.IO`-frei. 3 neue Tests.

**Offen, post-V1 (BPM-066 bleibt open):** Der Live-Watcher des Projektdialogs lädt den Baum bei jeder Disk-Änderung neu und verwirft dabei ungespeicherte Knoten; Hoch/Runter bei bestehenden Ordnern ist wirkungslos, weil Umnummerierung auf der Platte bewusst nicht gebaut ist (BPM-094 Out-of-Scope).

---
## [v0.28.178] — 2026-09-02

### Refactor: BPM-046 — .bpm/ Manifest-Split (ADR-046)

Der versteckte `.bpm/`-Ordner trennt jetzt Ausweis und Vollexport, wie in ADR-046 beschlossen. **`manifest.json`** ist nur noch der schlanke Projekt-Ausweis (`ProjectManifest`, SchemaVersion 2: projectId, projectNumber, name, updatedAtUtc, createdByMachine, Modul-Flags) — keine Stammdaten, keine Personendaten mehr in der Ausweisdatei. **`project.json`** trägt den Vollexport (`ProjectExport`, der bisherige `BpmManifest`-Inhalt: Bauherr, Adresse, Beteiligte, Bauteile, Links, Pfade) und wird bei jedem Speichern erneuert. `BpmManifestService` ist in drei Klassen aufgegangen: `ManifestService` (Ausweis + Migration), `ProjectExportService` (Vollexport, Export → Project) und `ProjectFolderScanner` (Import ohne Ausweis); gemeinsame Pfad-/Schreibhelfer in `BpmFolder`. `SettingsViewModel` schreibt an allen vier Stellen beide Dateien.

**Vorwärtsmigration** (`ManifestService.EnsureMigrated`, idempotent): alte `.bpm-manifest`-Einzeldatei → beide Dateien schreiben, alte Datei löschen (Fallback bleibt damit nicht mehr dauerhaft aktiv); `manifest.json` mit SchemaVersion 1 → in `project.json` + schlankes `manifest.json` aufteilen. Ein vorhandenes `project.json` wird nie überschrieben. Auslöser: Projekt-Import in den Einstellungen und jedes Speichern im Projektdialog. **`plan-index.json` entfällt** — seit ADR-061 ist `planmanager.db` der kuratierte Planindex. Infrastructure bleibt laut ADR-060 P.4 auf `System.IO`. 6 neue Tests (`BpmFolderManifestTests`), 513/513 grün. Docs: Architektur 3.6, INDEX, DB-SCHEMA-Dateitabelle, ADR-046 Implementierungsstatus, BACKLOG #11.

> ⚠️ **Historie:** Die Löschung von `BpmManifest.cs` und `BpmManifestService.cs` ist versehentlich schon im Docs-Commit `7835373` (v0.28.177) gelandet (per `git rm` vorab gestaged, dann mit den Docs committet). Dieser Zwischenstand baut nicht; ab diesem Commit ist die Solution wieder vollständig. Bereits gepusht, bewusst nicht umgeschrieben.

---
## [v0.28.177] — 2026-09-02

### Docs: BACKLOG #30/#31/#33/#34 bereinigt (BPM-017 erledigt; BPM-018, BPM-020, BPM-021 gestrichen)

BPM-017 „Undo (Import rückgängig machen)" war seit BPM-111.04 (v0.28.73, `22ebcc4`) umgesetzt und durch BPM-120 T7 (Undo-Härtung nach ADR-064) sowie BPM-111.07 D (↩ im Archiv-Tab) abgerundet — der ClickUp-Task hing nur noch als Planungsrest. Ohne Code-Änderung geschlossen, BACKLOG-Zeile 30 mit ✅ und Verweisen versehen. Bewusst nicht enthalten (ADR-064): Undo älterer Importe, `skipDuplicate` und Archiv-Moves.

**BPM-018 „Backup vor Import (SQLite + JSON)" gestrichen** (Entscheidung Herbert): nie umgesetzt, Begründung durch ADR-064 weggefallen (Vorab-Journal, Transaktion je Aktion, idempotente Recovery, gehärtetes Undo). Die JSON-Hälfte ist seit ADR-064 P.9 gegenstandslos (kein Profil-Import, kein LearnIndex-Lernen), und ein Restore einer älteren `planmanager.db` widerspräche ADR-061 (Drift gegen die Ordnerwahrheit, Journal weg). Rest-Idee für post-V1: bewusster manueller DB-Snapshot in den DevTools. ADR-040 (Backup vor Migration) bleibt unberührt.

**BPM-020 „Erkennungs-Konflikt (User wählt Profil)" gestrichen** (Entscheidung Herbert): Die Konflikt-Erkennung (`DocumentTypeRecognizer.IsConflict`, Status „Mehrere Profile" in der Vorschau) hing am klassischen Profil-Import, der laut ADR-064 P.9 aus dem V1-Pfad ist; der Auswahlschritt wurde nie gebaut, `ConflictPolicy` am Profil ist ungenutzt. Im Radial (ADR-059) wählt der Nutzer den Dokumenttyp selbst, ein Profil-Konflikt entsteht dort nicht. Der Restbedarf — bei mehrdeutiger Typ-Evidenz keine Ring-1-Vorbelegung, Kandidaten hervorheben (Veto-Regel ADR-065 P.4) — ist als Kommentar an BPM-121 übernommen.

**BPM-021 „Plan-Sammler (Checkbox-Sortierung)" gestrichen** (Entscheidung Herbert): Backlog-Notiz aus v0.12.4 ohne Ausarbeitung. Einzig sinnvolle Auslegung wäre ein Plan-Paket-Sammler (Auswahl im Archiv → Sortierschema → Ordner/ZIP/Druckliste) gewesen — kein Bedarf: Einsortieren ist über Radial-Bulk und Move-Radial gelöst, Planlisten kommen mit BPM-031. BACKLOG Should-Tabelle #34 und PlanManager.md Kap. 19 markiert.

---
## [v0.28.176] — 2026-09-02

### Docs: CHANGELOG-Nachzug v0.28.173–.175 (BPM-129)

Nachtrag der Einträge für den DB-SCHEMA-Docs-Commit, die BPM-126d-Streichung und den BPM-006 Profile-Tab. Keine Code-Änderung.

---

## [v0.28.175] — 2026-09-02

### Feature: BPM-006 — Profile-Tab als Dokumenttyp-Übersicht

Der Tab **Profile** zeigt jetzt die **Dokumenttyp-Übersicht** statt einer eigenen Profil-Verwaltung — Zielbild ADR-065 Punkt 7: `document_types` ist das fachliche Hauptobjekt, ein RecognitionProfile hängt 0..1 daran. Neues `DocumentTypeOverviewViewModel` gruppiert die Typen nach Ablagebereich (`root_relative_path`, alphabetisch) und zeigt je Typ Name, Typordner („— direkt im Ablagebereich" bei Root-Typen wie Protokollen), Kategorien-Anzahl, Builtin-Kennzeichen und einen **Erkennungs-Status als Platzhalter** („nicht angelernt") — die echten Zustände nicht angelernt / lernend / aktiv folgen mit BPM-121 Stufe B. Empty-State verweist auf Projekt-Setup bzw. „+ Neu…" im Radialmenü; dafür wurde `InverseBoolToVisConverter` in der `ProjectDetailView` registriert (StaticResource-Falle: fehlende Registrierung fällt erst zur Laufzeit auf).

**Bewusst ausgeklammert:** Ring 2 (Bauteil/Geschoss) — das sind Stammdaten ohne Erkennungsbedarf (Klarstellung Herbert, Teil 51). (Commit `6e192e3`)

---

## [v0.28.174] — 2026-09-02

### Change: BPM-126d — Excel-Export aus dem Plandaten-Tab gestrichen

Der für Slice d vorgesehene Excel-Export (ClosedXML) der Plandaten-Tabelle entfällt: Planlisten-Export (Excel + PDF) ist ein eigener Task **BPM-031, post-V1** und gehört nicht in die Toolbar des Plandaten-Tabs. Kommentare in `PlanDataView` und Mockup `05_Plandaten.html` angepasst — **BPM-126 damit komplett** (a Tabelle/Filter, b Detail-Panel, c Segment-Editor). (Commit `796714f`)

---

## [v0.28.173] — 2026-09-02

### Docs: DB-SCHEMA `plan_document_tags` + CHANGELOG-Nachzug v0.28.171–.172

Kapitel 6.7.3b in `DB-SCHEMA.md` um die Tag-Tabelle aus BPM-127 ergänzt (Spalten, Soft Delete, Normalisierung, additive Anlage per `CREATE TABLE IF NOT EXISTS`). Dazu die CHANGELOG-Einträge v0.28.171/.172 nachgetragen. (Commit `1dec4e5`)

---

## [v0.28.172] — 2026-09-02

### Feature: BPM-127 Tag-System + Plandaten-Panel im Karten-Layout

**Tag-System (BPM-127):** Neue Tabelle `plan_document_tags` — freie inhaltliche Schlagworte je Plandokument, **bewusst getrennt von den Dateinamens-Segmenten** (BPM-108). Additiv über `CREATE TABLE IF NOT EXISTS`, entsteht also auch in bestehenden Projekt-DBs — **kein Reset nötig**. Operationen in `PlanManagerDatabase`: `AddTag` (Normalisierung trim+lower, leere Eingabe ignoriert, `ON CONFLICT` reaktiviert soft-gelöschte Tags), `RemoveTag` (Soft Delete), `GetTagsForDocument`, `GetAllTags` (projektweite Vorschläge nach Häufigkeit). UI: Tags-Karte im Detail-Panel mit Enter-Eingabe, Chips mit ✕ und klickbaren Vorschlägen; Tag-Spalte in der Übersicht; Tags in der Tab-Suche. 7 neue Tests. Bewusst **kein separater Tag-Service** — die Operationen liegen wie `UpsertSegment` in der DB-Klasse, eine eigene Klasse wäre eine leere Hülle.

**Panel-Layout (Praxis-Feedback Herbert, Mockup-Auswahl Variante A):** Detail-Panel neu aufgeteilt — Info links schmal (Dokument · Ablage und Dateien · Revisionen), Arbeitsbereich rechts breit (Tags über Segment-Editor). Jeder Bereich ist eine abgesetzte Karte mit runden Ecken und Abstand; Schriftgrößen durchgehend eine Stufe höher; Tag-Eingabe mit fester Höhe und vertikal zentriert (war zu klein und oben ausgerichtet).

**Dateiendung ist kein Segment mehr (Befund Herbert):** `FileNameSegmentation` trennt die Endung vorab ab — der Punkt davor ist kein Trenner, `.pdf`/`.dwg` erscheint als reiner Text hinter den Kacheln statt als zuweisbares Segment. Nur der **letzte** Punkt zählt als Endung, Punkte im Namen (`202401_P_014.plot.pdf`) bleiben normale Trenner. 3 neue Tests. (Commit `2617609`)

---

## [v0.28.171] — 2026-09-02

### Docs: CHANGELOG-Nachzug v0.28.168–.170

Nachtrag der BPM-126-Slices. ⚠️ Dieser Commit (`97ba399`) liegt in der Historie **vor** `cc1e75b` (v0.28.170) — die beiden Commit-Blöcke wurden in umgekehrter Reihenfolge ausgeführt, die Versionsnummern laufen dort einmal rückwärts. Inhaltlich vollständig, bewusst nicht umgeschrieben (bereits gepusht).

---

## [v0.28.170] — 2026-09-02

### Feature: BPM-126b/c — Detail-Panel + wiederverwendbarer Segment-Editor

**Detail-Panel** unter der Plandaten-Tabelle mit drei Boxen (Dokument inkl. `document_key` · Ablage und Dateien mit Größe/MD5 je Revisionsdatei · Revisionshistorie mit grün markierter current-Revision), dazu „Vorschau" und „Im Explorer zeigen". Neue DB-Methode `GetFileDetailsForRevision` (Fingerprint-Daten, die der Archiv-Move nicht braucht).

**Segment-Editor** (`SegmentEditorControl`) im Muster des ProfilWizard-Schritts 2 — bewusst als **eigenständiges Control für Plandaten UND Wizard (BPM-080.05)**: EINE Fläche aus Token-Kacheln mit klickbaren Trennzeichen; deaktivierte Trenner bleiben **innerhalb** der verschmolzenen Kachel klickbar (unterstrichen) und trennen dort wieder auf. Globale Trenner-Chips (`-` `_` `.` `␣`), Typ-Zuweisung per Drag & Drop aus der Katalog-Palette (`ISegmentTypeCatalog`, BPM-108) mit Feldtyp-Farbe, Rechtsklick entfernt; „Segmenttypen verwalten…" öffnet den **bestehenden** `SegmentTypeManagerDialog`. Persistenz via `UpsertSegment` (reiner DB-Write, kein Journal — ADR-064). Pure Zerlegungs-Logik in `FileNameSegmentation` (Atome + Trennerzustände, stabiler Atom-Anker für Zuweisungen) mit 8 Tests.

**Panel-Höhe** per `GridSplitter` verstellbar und gerätelokal gemerkt (`UiLayout.PlanDataDetailHeight`, gleiches Muster wie die Vorschau-Breiten).

**Praxis-Befund (Herbert):** Nach jeder Zuweisung klappte das Panel zu — Ursache war das vollständige Neuladen der Liste, bei dem das DataGrid seine Auswahl verwirft. Statt dagegen anzuarbeiten wird jetzt nur die Segment-Anzahl der betroffenen Zeile aktualisiert (`PlanDataRowViewModel` beobachtbar); Auswahl und Panel bleiben stehen.

---

## [v0.28.169] — 2026-09-02

### Feature: BPM-126a — Plandaten-Tab + Stammdaten-IDs im Import

Neuer Tab **Plandaten** (Position 3: Explorer · Manuell sortieren · Plandaten · Profile · Sync): read-only Sicht auf den kuratierten Planindex mit zehn Spalten (Plan-Nr, Index, Bezeichnung, Typ, Bauteil, Geschoss, Index-Datum, Änderungshinweis, Dateitypen, Segment-Anzahl), Suche über Nummer/Bezeichnung/Typ/Änderungshinweis sowie Filter nach Dokumenttyp und Bauteil; aktualisiert sich nach Import und Undo. Neue DB-Abfrage `GetPlanDataRows` (Dateitypen via GROUP_CONCAT, Segment-Anzahl als Subselect).

**Nebenbefund mit Substanz:** Der Import schrieb `building_part_id`/`building_level_id` bewusst als `null` („SoftRef-Auflösung post-V1") — seit BPM-111 sind die IDs zum Zuordnungszeitpunkt aber bekannt und flossen bereits in den `document_key`. Ohne sie blieben Bauteil und Geschoss in der neuen Ansicht leer. `ClassifiedImportFile` trägt die IDs jetzt bis in `plan_documents` (CaptureConfirmService → ImportExecutionService); bei Recovery bleiben sie null, da das Journal sie nicht führt. 4 neue Tests. **Bestehende Dokumente brauchen einen Re-Import**, um die IDs zu erhalten (Frühphase, keine Migration).

---

## [v0.28.168] — 2026-09-01

### Docs: CHANGELOG-Nachzug v0.28.163–.167

Mockups (Explorer + Plandaten), BPM-112.06a/b/c und die Explorer-Live-Aktualisierung nachgetragen. (Commit `1a15987`)

---

## [v0.28.167] — 2026-09-01

### Feature: Explorer-Live-Aktualisierung + Fix: wirkungsloser Refresh

Praxis-Test-Befund Teil 50 (Test 04, Punkt 11): Drift-Zeilen erschienen erst nach erneutem Öffnen des Projekts. Ursache: `Refresh()` baute den Baum komplett neu auf und suchte die vorherige Ordnerauswahl nur auf der Wurzelebene — bei tief liegenden Ordnern (`…\H1\00 EG`) blieb `SelectedFolder` null und die Liste leer. Refresh frischt jetzt nur noch Daten auf (Getrackt-Index, Reconcile, Eingang-Zähler, aktuelle Liste), Baum und Auswahl bleiben; für strukturelle Änderungen gibt es `ReloadTree()` (läuft automatisch nach Import/Undo). Dazu **Live-Überwachung** (Wunsch Herbert): zwei `FileSystemWatcher` auf dem Projektroot — Datei-Events → Daten-Refresh, Ordner-Events → Baum-Neuaufbau, beide durch ein 750-ms-Debounce-Fenster (ein Import erzeugt Dutzende Events; der Reconcile rechnet bei fehlenden Dateien MD5). Puffer-Überlauf → Vollaktualisierung; verweigerte Watcher (Netz/Cloud) werden protokolliert, ⟳ bleibt Fallback; Freigabe beim Verlassen der Projektansicht. Watcher bleibt bewusst `System.IO` (ADR-060-Präzisierung). (Commit `e3e3c0f`)

---

## [v0.28.166] — 2026-09-01

### Feature: BPM-112.06c — Startup-Reconcile + Drift-Anzeige

Neuer `PlanReconcileService` (ADR-061 P.6): prüft **nur die getrackte Teilmenge** (current-Revisionen) gegen die Disk — Exists + `file_size` zuerst, MD5 ausschließlich für die Relink-Suche fehlender Dateien. Drift-Status `MissingOnDisk` / `ChangedOnDisk` / `RelinkCandidate`; fehlende Pläne erscheinen als rote Geisterzeilen im erfassten Ordner, Relink-Fundorte stehen im Tooltip — **nie automatisch reparierend**. Dazu `GetTrackedFilesForReconcile` in `PlanManagerDatabase` und 4 neue Tests (Kein-Drift / Fehlt / Relink / Größenabweichung). **Damit ist BPM-112.06 und die gesamte BPM-112-Kette abgeschlossen.** (Commit `bb15b57`)

---

## [v0.28.165] — 2026-09-01

### Feature: BPM-112.06b — Getrackt-Badges + journalisiertes Verschieben

Der Explorer löst jede Datei gegen den kuratierten Planindex auf und zeigt „Getrackt · Index"; Kontextmenü mit Öffnen / Im Windows-Explorer / Pfad kopieren / Im PlanManager anzeigen / **Verschieben (journalisiert)** über den bestehenden `ArchiveMoveService` (alle Dateien der Revision gemeinsam, Journal vor Move) — Löschen gesperrt, kein freies Verschieben (ADR-061 P.6). Neuer `FolderPickerDialog` für die Zielordner-Wahl; die `planmanager.db` wird beim Projektöffnen geöffnet und mit dem ManuellSortieren-Tab geteilt. (Commits `c2422c6` + `98892f4`)

> ⚠️ **Historie:** `98892f4` trägt versehentlich dieselbe Message/Version, enthält aber bereits die 06c-Drift-Integration in `ExplorerViewModel`/`ExplorerView` (Doppel-Commit, bereits gepusht — bewusst nicht umgeschrieben).

---

## [v0.28.164] — 2026-09-01

### Feature: BPM-112.06a — In-App-Explorer (Basis)

Neuer Explorer-Tab als **Start-Tab** der Projekt-Detailansicht (neue Reihenfolge: Explorer · Manuell sortieren · Profile · Sync; „Plandaten" folgt mit BPM-126). Live-Ansicht des Projektordners über `IFileSystemReader` (ADR-061 Modell A — die DB bleibt kuratierter Index, kein Vollspiegel): Baum mit Lazy-Load, Dateiliste im BPM-DataGrid-Stil, Breadcrumb, Screen States. Toolbar über `IFileLauncher` (Öffnen, Im Windows-Explorer) plus Pfad-in-Zwischenablage (UI-nah gelöst, wie im Port-Kommentar vorgesehen). Der `_Eingang`-Knoten trägt den Dateizähler und zieht bei Import/Undo automatisch nach. (Commit `496bef1`)

---

## [v0.28.163] — 2026-09-01

### Docs: Mockups In-App-Explorer + Plandaten-Tab

`02_Projektdetail/04_Explorer.html` (BPM-112.06, Variante A = Tab statt eigener View) und `05_Plandaten.html` (neu, BPM-126): tabellarische DB-Sicht mit Detail-Panel (Dokument / Ablage+Dateien / Revisionen), Segment-Editor im **Wizard-Muster** (eine kombinierte Fläche: Token-Kacheln mit klickbaren Trennzeichen, Drag & Drop aus der Segmenttyp-Palette, Anbindung an den bestehenden FeldtypManager) sowie **Tags** als neues Konzept (BPM-127, ≠ Dateinamens-Segmente). Neue Tab-Reihenfolge in allen Projektdetail-Mockups, Sitemap um alle Kanten ergänzt. (Commit `32966a0`)

---

## [v0.28.161] — 2026-08-31

### Fix: Eingang-Banner sofort aktuell nach Import/Undo

Praxis-Test-Befund Teil 50 (Runde 1, Block 7): Das gelbe Eingang-Banner der Projekt-Detailansicht zählte nach „Import bestätigen"/Undo erst beim Neu-Betreten der Ansicht um. Neues Event `ManualCaptureViewModel.InboxChanged` nach jeder Eingang-Neuanalyse (`RefreshAsync`) → `ProjectDetailView` stößt `RefreshInbox` an; zusätzlich fehlte `[NotifyPropertyChangedFor(nameof(HasInbox))]` an `InboxCount` (Banner-Sichtbarkeit hätte sonst auch mit Refresh nicht umgeschaltet). (Commit `6390031`)

---

## [v0.28.160] — 2026-08-31

### Fix: Vorschau Text-Markierung — Zentrier-Versatz beim Hit-Test

Praxis-Test-Befund Teil 50 (Runde 1, Block 6): Sobald das Blatt kleiner als der Vorschau-Viewport dargestellt wurde (rausgezoomt), zentrierte WPF das Bild im `SheetHost` — der Wort-Hit-Test rechnete aber ab Container-Ursprung links oben, alle Klicks gingen um den Zentrier-Versatz daneben (BPM-118-Markieren wirkte „tot", auch die Markierungs-Balken wären versetzt gemalt worden). Fix: `PageImage` + `SelectionCanvas` in bildgroßem, zentriertem `SheetContent`-Grid; `GetPosition` misst an `SheetContent`. Engine-Koordinaten (ADR-063) unverändert korrekt — per temporärem Diagnose-Test gegen die echte Testdatei verifiziert. (Commit `580cd63`)

---

## [v0.28.159] — 2026-08-31

### Docs: CHANGELOG-Nachzug v0.28.154–.158

Einträge der BPM-112-Slices 1/2/4/5 (FS-Ports-Migration inkl. ADR-060-Präzisierung Pure-Statics) + BPM-120-Doc-Abschluss. (Commit `70ba582`)

---

## [v0.28.158] — 2026-08-27

### Refactor: BPM-112.05 — Settings/Views + ProjectFolderService auf FS-Ports

`ProjectFolderService` (26 Stellen, ADR-060 P.4) vollständig auf injizierte Ports; Settings (`SettingsViewModel`, `FolderTemplateControl`, `ProjectEditDialog`) und PlanManager-Views/VMs (`PlanPreviewPanel` mit 4× `OpenRead`, `ManualCaptureView`, `PlanManagerView`, drei VMs inkl. der aus 112.02 verschobenen Path-Aufrufe) System.IO-frei. Bewusst belassen: `FileSystemWatcher` (UI-Live-Refresh, kein Port-Äquivalent) und `App.xaml.cs` (Composition Root). **Die ADR-060-Ports-Migration ist damit abgeschlossen — von BPM-112 bleibt nur der In-App-Explorer (112.06, Feature).** (Commit `c8787ab`)

---

## [v0.28.157] — 2026-08-27

### Refactor: BPM-112.04 — DB-Pfadanlage auf FS-Ports

Pfad-/Ordneranlage in den Ctors von `ProjectDatabase` (bpm.db) und `PlanManagerDatabase` (planmanager.db, inkl. BPM-123-Override-Zweig) über die Ports (lokale Adapter-Instanz — DB-Klassen sind selbst Infrastruktur, kein Signatur-Churn); SQLite-Connection bewusst unverändert. (Commit `ac37a3c`)

---

## [v0.28.156] — 2026-08-27

### Docs: BPM-112.02 — Pure-Statics-Linie (ADR-060-Präzisierung)

Entscheidung Herbert: Die Port-Pflicht gilt **Disk-Zugriffen** — pure Pfad-String-Operationen in statischen Pure-Logic-Klassen (`FileNameParser`, `ImportContextResolver`, `CaptureConfirmService`-Mapper, `PlanValueNormalizer`) bleiben bewusst auf `System.IO.Path` (deterministisch, „keine Abhängigkeiten"-Design ADR-022, ADR-065-Reparse-Basis). In ADR-060 verankert, Stellen per Code-Kommentar gekennzeichnet; die mit File-Ops verwobenen VM-Pfadaufrufe wanderten geschlossen zu Slice 5. (Commit `8510059`)

---

## [v0.28.155] — 2026-08-27

### Refactor: BPM-112.01 — Scanner/Reader + Profil-JSON auf FS-Ports

`ImportScanService` + `FileFingerprintService` (MD5 via `OpenRead`-Port) und die Profil-JSON-Persistenz (`ProfileManager`, `PatternTemplateService`, `ProfileArchiveService`) komplett auf die Ports. Dafür Port-Erweiterung `ReadAllText`/`WriteAllText` (einzige Inhalts-Schreiboperation, nur App-eigene JSON-Konfigs) inkl. FakeFileStore-Fault-Op `Write` + 3 neue Contract-Tests × beide Implementierungen. App-DI registriert EINE `LocalFileSystem`-Instanz für alle drei Ports. (Commit `0b98714`)

---

## [v0.28.154] — 2026-08-27

### Docs: BPM-120-Abschluss — CHANGELOG .139–.153 + ADR-064 „Umgesetzt" + DB-SCHEMA

CHANGELOG-Nachzug der kompletten Härtungs-Serie; ADR-064-Implementierungszeile auf ✅ Umgesetzt (alle 15 AKs testverifiziert, ADR-061 P5 erledigt); DB-SCHEMA 6.5 um `document_type_id` (T5) ergänzt, Reset-Anweisung auf T2/T5 erweitert. (Commit `eb82450`)

---

## [v0.28.153] — 2026-08-27

### Refactor: BPM-120 T8 — Fault-/Crash-Matrix

Abschluss-Suite `ImportCrashMatrixTests`: 5 Crash-Punkte einer Update-Action (nach Journal / Archivierung / tmp-Move / finalem Rename / DB-Commit) als Theory — Recovery Forward erreicht aus **jedem** Zwischenzustand denselben Endzustand (Datei am Ziel, genau eine Archivkopie, kein tmp, alte Revision superseded / neue current mit genau einer Datei, Journal completed). Dazu Rollback-aus-C0 (terminal `failed`, Dateien unberührt) und Undo mit scheiterndem Archiv-Restore (DB bleibt auf Import-Endzustand, AK 14). **BPM-120 damit komplett (H0 + T0–T8, alle 15 Akzeptanzkriterien).** (Commit `81b49e8`)

---

## [v0.28.152] — 2026-08-27

### Fix: BPM-120 T7 — Undo-Härtung

CGR-Kernbefund behoben: Scheitert irgendein Disk-Reverse, kehrt `UndoLastImport` sofort zurück — **keine Revision soft-deleted/restored, kein `MarkImportUndone`** (vorher: Disk halb zurück, DB komplett zurück, Import fälschlich „undone"). Abbruch beim ersten Reverse-Fehler (minimale Drift); DB-Rollback + `undone` in **einer** SQLite-Transaction. (Commit `5408dc4`)

---

## [v0.28.151] — 2026-08-27

### Change: BPM-120 T6 — failed/pending-Semantik

`pending` = recovery-pflichtig (AK 13): Ein Import mit fehlgeschlagenen Actions bleibt `pending` (neue `SetImportJournalError`) — blockiert den nächsten Confirm, Recovery-Dialog beim Tab-Öffnen. `failed` erst terminal nach **vollständigem** Rollback oder bewusstem Cleanup; scheitert ein Reverse, bleibt der Vorgang reparierbar. `failed`-Actions sind wiederholbar (Forward verarbeitet pending + failed). (Commit `5c645dd`)

---

## [v0.28.150] — 2026-08-27

### Change: BPM-120 T5 — Recovery Forward über gemeinsamen Apply-Pfad

`RecoveryExecutorService` verliert seine vereinfachte Move-Eigenlogik: Forward läuft über `ImportExecutionService.RecoverActionForward` — idempotenter Disk-Forward (Archiv-Guard = nie zweite Archivkopie, source/tmp/target-Fälle) + derselbe DB-Apply wie der Import (AK 8/9/11/12). Journal um `document_type_id` erweitert (+ `ImportActionRow` trägt DocumentKey/PlanNumber/PlanIndex) → Recovery stellt die volle Dokument-Struktur her. **planmanager.db-Reset.** (Commit `f90b599`)

---

## [v0.28.149] — 2026-08-27

### Change: BPM-120 T4 — DB-Transaction pro Action

Disk-Phase komplett vor DB-Phase; alle fachlichen Writes + `action_status = completed` laufen in **derselben** SQLite-Transaction (`ExecuteInTransaction` via BEGIN IMMEDIATE — Microsoft.Data.Sqlite erzwingt sonst cmd.Transaction überall). Supersede in die Transaction verlegt. `ApplyActionToDatabase` = gemeinsamer Apply-Pfad mit Already-Linked-Guard (idempotenter Re-Apply). Injizierter DB-Fehler rollt Document/Revision/File und sogar den Supersede zurück (AK 10). (Commit `264e696`)

---

## [v0.28.148] — 2026-08-27

### Change: BPM-120 T3 — ADR-061-Disk-Protokoll

Publish-Move zweistufig: Inbox → `<ziel>.bpm_tmp` → atomarer Rename im Zielverzeichnis (Crash hinterlässt nur eine tmp-Datei, nie ein halbes Ziel). `WithLockRetry`: max. 3 Versuche bei Lock-/Sharing-IOExceptions (nicht bei FileNotFound) für Publish/Archiv/Delete. Recovery holt den finalen Rename nach tmp-Crash idempotent nach (AK 7). (Commit `7da97a9`)

---

## [v0.28.147] — 2026-08-27

### Change: BPM-111 — document_key ID-basiert (Abnahmepunkt erfüllt)

`BuildManualDocumentKey` baut den Key jetzt aus Stammdaten-IDs: `document_type_id | plannummer | building_part_id/category_id [| building_level_id]` — umbenennungsstabil (ADR-059 P.3). `PendingAssignment` trägt die IDs (neue Controller-Properties `SelectedBuildingLevel`/`SelectedCategory`). **BPM-111 damit komplett** (alle 7 Subtasks + Abnahmepunkt). (Commit `45b82ea`)

---

## [v0.28.146] — 2026-08-27

### Docs: DB-SCHEMA — import_actions T2-Nachzug

Kap. 6.5 auf Ist-Stand: `destination_path` nullable, neue Spalten `md5`/`file_size`, `action_type` `skipDuplicate`, Vorab-Journalisierungs-Absatz + Reset-Anweisung nach Frühphasen-Regel. (Commit `a41a627`)

---

## [v0.28.145] — 2026-08-27

### Refactor: BPM-123 — Test-DBs nach Temp

`PlanManagerDatabase` bekommt `dbPathOverride` (Muster `ProjectDatabase`); alle 11 Test-Fixtures nutzen den neuen `TempDb`-Helfer → Test-Datenbanken liegen unter `%TEMP%` statt im echten `%LocalAppData%\BauProjektManager\Projects\` (dort hatten sich seit Mai 31 Müll-Ordner aus Testläufen angesammelt — aufgeräumt, PersistenceRegistry/DevTools wieder sauber). Nachweis: kompletter Suite-Lauf erzeugt 0 neue Ordner. (Commit `834ebf4`)

---

## [v0.28.144] — 2026-08-27

### Change: BPM-120 T2 — Vorab-Journalisierung + skipDuplicate (Bucket A)

`Execute` zweiphasig: Phase 1 plant alle Actions (inkl. deterministischem `archive_path`) und journalisiert Header + **alle** Actions vor der ersten Mutation (AK 4/5, bewiesen per Probe-Writer); Phase 2 führt aus. Bestätigte MD5-Dubletten sind echte `skipDuplicate`-Actions (Source, MD5, Größe, kein Ziel) und werden beim Radial-Confirm gelöscht — „✓ N importiert · M Dublette(n) entfernt" (AK 6). Undo-/Recovery-Guards nach ADR-064 P.7 (MD5-Bestandscheck, „RecoveryConflict, nie blind completed"). Schema: `destination_path` nullable + `md5`/`file_size` → **planmanager.db-Reset**. (Commit `b26a9b7`)

---

## [v0.28.143] — 2026-08-27

### Fix: BPM-122 — Bulk-Warnung hängt nach Abwahl

Praxis-Test-Befund: ⚠/⛔-Hinweise aus der Bulk-Vorprüfung blieben in der Statuszeile stehen. Jetzt gelten sie nur für die Auswahl, mit der das Radial gestartet wurde — bei Auswahländerung kehrt die neutrale Zusammenfassung zurück; andere Meldungen bleiben unberührt. (Commit `d0d802c`)

---

## [v0.28.142] — 2026-08-27

### Refactor: BPM-120 T1 — FS-Ports im Importpfad

`ImportExecutionService`, `RecoveryExecutorService`, `ImportUndoService` komplett auf `IFileSystemReader`/`IFileSystemWriter`/`IPathService` (kein `File.`/`Directory.` mehr im Hochrisikopfad); `CaptureConfirmService` bekommt den Executor per Constructor Injection statt internem `new` (CGR-Befund). `FakeFileStore` fault-fähig (`FailNext`). Schließt **BPM-112.03** mit ab (ADR-060 Slice 3). (Commit `05551df`)

---

## [v0.28.141] — 2026-08-27

### Refactor: BPM-120 T0 — Characterization-Tests + Flaky-Fix

Ist-Verhalten des Importpfads gepinnt (New-/Update-Happy-Path inkl. Journal-Asserts, Zeitinvariante `superseded_at == current_from` erstmals E2E, Datenverlust-Schutz bei Namenskollision) — bekannte Fehler bewusst NICHT als Soll. Flaky-Ursache gefunden + behoben: globales `SqliteConnection.ClearAllPools()` in 14 Test-Disposes riss unter xunit-Parallellast fremde Pools mit → gezieltes `ClearPool`. (Commit `af430cb`)

---

## [v0.28.140] — 2026-08-27

### Change: BPM-120 H0 — Alt-Import-Cutover

„Import starten"/`OnStartImport`/`ImportPreviewDialog` aus dem V1-Nutzerpfad entfernt (ADR-064 P.9) — der Radial-/Bucket-Workflow ist der einzige V1-Importweg. Der Recovery-Einstieg (BPM-016) hängt jetzt an der Radial-Strecke: proaktiv beim Tab-Öffnen + Fallback beim blockierten Bestätigen (Event `RecoveryRequested`). Legacy-Klassen bleiben unreferenziert im Repo. (Commit `6d5d44d`)

---

## [v0.28.139] — 2026-08-27

### Docs: CHANGELOG-Nachzug v0.28.136–.138

Einträge für ADR-064/065-Verankerung + CGR-Abschlüsse + BPM-121 nachgezogen. (Commit `23d8acb`)

---

## [v0.28.138] — 2026-08-27

### Docs: ADR-065 Lernende Planerkennung + CGR-2026-08-27-plan-erkennung Abschluss

Neue Review-Serie (3 Runden, beidseitiges Sign-off) zu ChatGPTs Konzept „lernende Profile": Entscheidung **gegen** ML.NET/Embeddings/LLM im Importpfad, **für** erklärbare Assistenz — bestätigte Roh-Evidenz → nachvollziehbares Rule Mining → explizite Profilregeln + Aliasse. **ADR-065** verankert: ADR-059-Grenze unangetastet (nur L0 MD5/`document_key` entscheidet, alles Gelernte `AutoSuggested`); Evidenz-Backoff L2a (Projekt × `DocumentTypeId`) → L2b (`profileLineageId`-Familie) → L2c (kuratierte Formen) mit Veto-Regel statt Score-Fusion; WERTE/ROLLEN/FORMEN-Scoping (Stammdaten-IDs nie scope-übergreifend); Tokenization-Bootstrap (Rohfakten statt Token-Snapshots, Reparse via `FileNameParser`); `document_types` als fachliches Hauptobjekt (Erkennungs-Tab als View, `RecognitionProfile` 0..1, kein leeres Profil-JSON, Löschen asymmetrisch); `document_types.is_active` + PatternTemplate-Identität via Lineage (mit Stufe B/C2); Roadmap A/B/C1/C2/D. Neuer Sammel-Task **BPM-121**. (Commit `dede6a7`)

---

## [v0.28.137] — 2026-08-27

### Docs: ADR-064 Import-Transaktions-Härtung + CGR-2026-08-27-bpm-architektur Abschluss

**ADR-064** verankert (11 verbindliche Invarianten: Idempotenz-Kerninvariante, Vorab-Journalisierung, atomarer Action-Abschluss, gemeinsamer Apply-Pfad Import/Recovery, Undo nur nach vollem Disk-Reverse, `skipDuplicate` journalisiert/recovery-fähig/nicht undo-bar, H0-Cutover, `destination_path` nullable via DB-Reset; Slice-Folge H0+T0–T8 → BPM-120) + Serie mit beidseitigem Sign-off r3 abgeschlossen, BPM-120-Verweise in Serien-README + Review-INDEX. (Commit `5c31cdc`)

---

## [v0.28.136] — 2026-08-27

### Docs: CHANGELOG-Nachzug v0.28.130–.135

Einträge für die BPM-111.07-Slices B/C/D (damit 111.07 komplett) + Mockup-Spez Kombi nachgezogen. (Commit `5723a0d`)

---

## [v0.28.135] — 2026-08-27

### Feature: BPM-111.07 Slice D2 — Archiv-Verschieben per Radial

Kurzes Halten auf einer Archiv-Zeile öffnet das Radial im **Move-Modus** (Zentrum „Verschieben"); die Zuordnung verschiebt sofort — kein Pending. Neuer **`ArchiveMoveService`**: Journal-Action VOR jedem Move (Aktionstyp `moved`), **alle** Dateien der current-Revision ziehen gemeinsam um (PDF+DWG), `plan_files.relative_path` + `plan_documents`-Ablage werden aktualisiert (ADR-061: DB = Ordner-Wahrheit), `manual_override`-Event an der Revision. Journal endet mit Status **`moved`** statt `completed` — Import-Undo und „letzter Import"-Kennzeichnung bleiben bewusst unberührt (testverifiziert). „+ Neu…" im Move-Radial legt Stammdaten an und verschiebt dorthin. BPM-111.07 damit komplett (A–D). (Commit `72a18af`)

---

## [v0.28.134] — 2026-08-27

### Feature: BPM-111.07 Slice D1 — Archiv-Sub-Tabs (read-only)

Sub-Tabs **„Neue Pläne (N) / Archiv (M)"** oberhalb der Tabelle (BpmTabControl-Default-Styles): Der Archiv-Tab zeigt den Bestand aus der DB (neue Query `GetArchiveEntries`: Dokument + current-Revision + Primärdatei, neueste zuerst) mit Spalten Datei | Ordner | Hinzugefügt. **Grün** = letzter Import (`GetLastCompletedImportId`-Abgleich) inkl. ↩-Button (macht den letzten Import komplett rückgängig — Undo-Invariante). Rechtsklick-Kontextmenü mit read-only-Vorschau (bei DWG-Dokumenten die gepaarte PDF), Datei öffnen, Im Explorer zeigen. (Commit `53a1219`)

---

## [v0.28.133] — 2026-08-27

### Feature: BPM-111.07 Slice C — Kombi-Pläne sichtbar

`IsCombi` (mehrere Plantyp-Keywords im Dateinamen, Extractor BPM-111.02) ist jetzt sichtbar: **„⚠ Kombi"-Pill-Badge** in der Tabelle, **Panel-Warnhinweis** mit der V1-Regel (kein Auto-Split — als EIN Dokument erfassen, Typ „Kombiplan/Sonstiges" via „+ Neu…", Inhalte per Text-Zuweisung als Segmente) und **Radial-Zentrum-Hinweis**. Bewusst kein Auto-Seed (ADR-061) und keine Auto-Segmente (ADR-059). (Commit `7473e17`)

---

## [v0.28.132] — 2026-08-27

### Docs: Mockup ManuellSortieren — Kombi-Plan-Spez

Verbindliche Spez um Kombi-Pläne erweitert: Spez-Header-Block, Demo-Datei mit `combi`-Flag, „⚠ Kombi"-Pill-Badge, Panel-Warnhinweis, Radial-Zentrum-Untertitel. (Commit `2634ab8`)

---

## [v0.28.131] — 2026-08-27

### Feature: BPM-111.07 Slice B — Bulk-Vorprüfung („Hinweis + Deckel")

Neue pure **`BulkPrecheck`** beim Radial-Start über die effektive Zuordnungsliste (inkl. Paar-Partner): bis 8 wie bisher, **ab 9** deutliche Mengenwarnung in der Statuszeile (die echte Bestätigung bleibt „Import bestätigen" — Entscheidung: das Pending-Modell ersetzt die Teil-43-Zusatzbestätigung), **über 20** öffnet das Radial nicht (Deckel + Hinweis). Kompatibilitäts-Warnungen: gemischte Nicht-Plan-Dateitypen, gleiche Plannummer bei gleichem Dateityp (würde an EINE Revision andocken; PDF+DWG-Paare ausgenommen). (Commit `b063942`)

---

## [v0.28.130] — 2026-08-27

### Docs: CHANGELOG-Nachzug v0.28.123–.129

Einträge für Slice C3, Paar-Strecke A1–A3, Engine-Addenda und CGR-Paket nachgezogen. (Commit `898ee27`)

---

## [v0.28.129] — 2026-08-27

### Docs: CGR-2026-08-27-bpm-architektur Review-Paket

ChatGPT-Cross-Review-Paket abgelegt (3 Runden r1–r3: Prompts, Responses, Analysen, Entscheidungen + README) + Routing-Zeile im chatgpt-reviews-INDEX. (Commit `70949a2`)

---

## [v0.28.128] — 2026-08-27

### Feature: BPM-111.07 Slice A3 — Paar-UX (Badge + Panel-Hinweis)

Die Paar-Mechanik ist jetzt sichtbar (nach Mockup-Spez v0.28.127): **⛓ „Paar"-Pill-Badge** an beiden Zeilen der Eingangs-Tabelle (Stil der Eingang-Badges der Projektliste: `CornerRadius=8`, `BpmInfo` + `BpmTextBright`), **Paar-Hinweis im Detail-Panel** („Import als EINE Revision mit zwei Dateien — PDF führend, DWG angehängt", blaue Akzentkante). Neu: `CaptureRowViewModel.PairedFileName`/`IsPaired` + zentrales `UpdatePairFlags` nach jeder Rows-Änderung; `PairedExtensionFor` als gemeinsamer Helfer. (Commit `861179d`)

---

## [v0.28.127] — 2026-08-27

### Docs: Mockup ManuellSortieren — PDF+DWG-Paar-Spez

`02_ManuellSortieren.html` um die verbindliche Paar-Spez erweitert (BPM-111.07 Slice A): Spez-Header-Block, Beispiel-Paar `5998-202_OG2` in den Demo-Daten, Pill-Badge in der Tabelle, Panel-Hinweis, interaktive Partner-Mitnahme mit „⛓ …"-Meldung. (Commit `f49a11e`)

---

## [v0.28.126] — 2026-08-27

### Feature: BPM-111.07 Slice A2 — Update-Paar + Import-Guard

„⬆ Update übernehmen" nimmt den PDF/DWG-Partner (gleicher Dateinamens-Stamm) automatisch mit. Dazu **Import-Guard in der Execution**: Bei `UpdateNewerIndex` wird nur supersedet, wenn die current-Revision nicht aus demselben Import stammt (`LastImportId`-Vergleich) — das entschärft einen schon vorher auslösbaren Bug (zweite Update-Aktion desselben Dokuments supersedete die frisch angelegte Revision und legte eine DWG-primäre Zweitrevision an). E2E-Test mit DWG-zuerst-Reihenfolge. (Commit `a673e94`)

---

## [v0.28.125] — 2026-08-27

### Feature: BPM-111.07 Slice A1 — PDF+DWG-Paar-Import

Ein PDF+DWG-Paar (gleicher Dateinamens-Stamm) wird als **EIN Dokument mit EINER Revision und zwei Dateien** importiert: Die Radial-Zuordnung nimmt den nicht selektierten Partner automatisch mit (`ExpandWithPairedRows`, Statushinweis „⛓ N gepaarte Datei(en) automatisch mit zugeordnet"; Duplikate/Updates ausgenommen), `BuildDecisions` sortiert PDFs stabil nach vorn — die PDF legt die Revision an (`is_primary`), die DWG dockt über den bestehenden FileLinked-Zweig an. (Commit `6ea9e77`)

---

## [v0.28.124] — 2026-08-27

### Feature: BPM-111.06 Slice C3 — DWG-Vorschau via gepaarte PDF

Rechtsklick „Vorschau" funktioniert jetzt auch auf DWG-Zeilen: zuerst wird ein **Eingangs-Partner** mit gleichem Dateinamens-Stamm gesucht (Vorschau + Text-Zuweisung laufen dann auf der PDF-Zeile), sonst die PDF der **aktuellen Archiv-Revision** des bekannten Dokuments (read-only — neue Lesemethode `GetPdfPathForRevision`, bevorzugt `is_primary`). Ohne Paar bleibt der Menüpunkt inaktiv. Paar-Findung ohne System.IO (ADR-060-konform). BPM-111.06 damit komplett; Archiv-Sub-Tabs in BPM-111.07 verschoben. (Commit `c2a47f1`)

---

## [v0.28.123] — 2026-08-27

### Docs: CHANGELOG-Nachzug + ADR-Addenda Engine-Konsolidierung + DB-SCHEMA

CHANGELOG v0.28.114–.122, **ADR-062/063-Addenda „Engine-Konsolidierung"** (EINE Engine PDFium/Docnet für beide Ports, BGRA statt PNG, App-TFM-Bump zurückgebaut, PdfPig nur Test-Builder inkl. ⚠ Paket-ID-Warnung), DB-SCHEMA `plan_revisions.change_note` + `released_at`-Quellen, INDEX-Routing ADR-063. (Commit `ae7a98c`)

---

## [v0.28.122] — 2026-08-27

### Feature: BPM-118 Teil 3 — Persistenz der Text-Zuweisungen

Die per Text-Zuweisung erfassten Werte fließen jetzt beim „Import bestätigen" bis in die DB: `PendingAssignment` und `ClassifiedImportFile` tragen **`ChangeNote`/`ReleasedAt`/`AssignedSegments`** (neues Domain-Record `AssignedSegmentValue` mit SegmentTypeId + TokenKey + Wert — TokenKey kommt aus dem Zuweisungs-Menü mit, kein Cross-DB-Lookup). `ImportExecutionService` übergibt `released_at`/`change_note` an `InsertRevision` und schreibt Segmente per neuem **`UpsertSegment`** (ON CONFLICT auf UNIQUE document_id+segment_type_id — letzte Zuweisung gewinnt, auch im Update-Fall); dazu `GetSegmentsForDocument` als Lese-API. Beide Assign-Stellen + Confirm-Sync im `ManualCaptureViewModel` reichen die Row-Werte durch (Muster Slice A3). Wizard-Import unverändert (Defaults + Regressionstest). 414/414 Tests (4 neue). BPM-118 damit komplett. (Commit `07e9efb`)

---

## [v0.28.121] — 2026-08-27

### Feature: BPM-118 Teil 1+2 — Text-Zuweisung aus PDF-Vorschau + Engine-Wechsel PDFium

Text in der PDF-Vorschau **wie in Word markieren** (I-Beam-Cursor, Auswahl in Leserichtung, durchgehender Balken je Zeile, Theme-Blau) und per Rechtsklick zuweisen: Gruppen **REVISION** (Änderungshinweis → `Row.ChangeNote`, Index-Datum → `Row.ReleasedAtIso`) und **ZUWEISEN ALS SEGMENT** dynamisch aus `ISegmentTypeCatalog`; Plannummer/Index füllen die Panel-Edit-Felder (Re-Match), Bezeichnung → Title. **ENGINE-WECHSEL:** Windows.Data.Pdf + PdfPig-Mapping traf daneben → **EINE Engine PDFium via Docnet.Core 2.6.0** — `PdfiumPdfService` (Infrastructure) bedient `IPdfRenderService` UND `IPdfTextService` aus derselben Pipeline; `RenderPageAsync` liefert BGRA32-Pixel statt PNG; App-TFM-Bump zurückgebaut; adaptives Nachrendern ~7 px/mm (Deckel 7200 px), Box-Normalisierung + Alpha-Compositing auf Weiß. PdfPig nur noch Test-Builder (⚠ Paket-ID „PdfPig", NICHT „UglyToad.PdfPig" — gekapert). (Commit `3207834`)

---

## [v0.28.120] — 2026-08-27

### Feature: BPM-111.06 — Detail-Panel-Breite anpassbar + persistent

Zweiter Splitter zwischen Detail-Panel und Vorschau: Detail-Breite per Maus anpassbar (320–900 px) und geräte-lokal persistent (`device-settings.json` → `uiLayout`, wie die Vorschau-Breite). Die Aktions-Buttons im Panel bleiben fix 296 px. (Commit `02e808a`)

---

## [v0.28.119] — 2026-08-27

### Feature: BPM-111.06 Slice D — Detail-Panel-Redesign (Historie immer sichtbar)

Index-Historie im Detail-Panel **immer sichtbar und 3-spaltig** (Revision | Datum | Änderung): erste Zeile = die einlaufende Datei selbst („(neu)", hervorgehoben), Datum bevorzugt `released_at` (dd.MM.yyyy, sonst `current_from`), Spalte „Änderung" aus neuem DB-Feld **`plan_revisions.change_note`** (Frühphase: `planmanager.db` löschen statt Migration). Panel-Grundbreite 320 px. (Commit `b6f46c1`)

---

## [v0.28.118] — 2026-08-26

### Docs: Mockup ManuellSortieren komplett gemergt

`02_ManuellSortieren.html` als verbindliche Gesamt-Spez konsolidiert: 3-Spalten-Layout (Tabelle | Detail | Vorschau), Detail-Panel-Redesign, Vorschau mit Text-Zuweisung, Radial übernommen — interaktiv in einem Mockup. (Commit `c3da74f`)

---

## [v0.28.117] — 2026-08-26

### Docs: ADR-063 PDF-Text-Port + PdfPig-Freigabe

ADR-063 (IPdfTextService: Wort-Text mit mm-Koordinaten, KEIN OCR, `change_note`-Schema) inkl. Library-Freigabe PdfPig (MIT) + Mockup-Spez Detail-Panel-Redesign und Text-Zuweisung. (Commit `f1b11ac`)

---

## [v0.28.116] — 2026-08-26

### Feature: BPM-111.06 — Vorschau-Breite persistent + Fensterplatzierungs-Fix

Vorschau-Panel-Breite geräte-lokal persistent (`device-settings.json` → neue Sektion `uiLayout`), Default 520 px. Dazu Bugfix: `AppSettingsService.Save(AppSettings)` verwarf `MainWindowPlacement` — Fensterposition/-größe überlebt jetzt jeden Settings-Save. (Commit `0f4d575`)

---

## [v0.28.115] — 2026-08-26

### Change: BPM-111.06 Slice C — Vorschau als integriertes Panel (Variante B)

Die PDF-Vorschau ist kein separates Fenster mehr, sondern eine **integrierte Panel-Spalte rechts außen** im Tab „Manuell sortieren" (Tabelle | Detail | Vorschau, per Splitter): `PlanPreviewWindow` → `PlanPreviewPanel`, Ghost-/Secondary-Buttons, ✕ schließt die Spalte. „Andocken ans MainWindow" ist damit entfallen. (Commit `126245f`)

---

## [v0.28.114] — 2026-08-26

### Docs: CHANGELOG v0.28.105–.113 + ADR-062-Addendum + Mockup Variante B

CHANGELOG-Nachzug .105–.113, ADR-062-Addendum (Bearbeitung dauerhaft extern via IFileLauncher, `PdfPageRender` mit Blattgröße in mm) + Mockup-Spez Vorschau als integriertes Panel (Variante B). (Commit `093c187`)

---

## [v0.28.113] — 2026-08-26

### Feature: BPM-111.06 Slice B — Kontextmenü „Datei öffnen" + „Im Explorer zeigen"

Das Rechtsklick-Kontextmenü im Tab „Manuell sortieren" (bei geschlossenem Radial) ist komplett: neben „Vorschau" jetzt **„Datei öffnen"** (Windows-Standard-App) und **„Im Explorer zeigen"** (vorselektiert) — beides über den `IFileLauncher`-Port (ADR-060), Fehler landen als ⚠ in der Statuszeile. Zusätzlich Theme-Fix: `BpmContextMenu` bekommt ein eigenes Template ohne die helle Icon-Spalte (Gutter) des WPF-Default-Templates — Menüs sind jetzt durchgehend im Dark-Theme (gilt app-weit, auch Settings). (Commit `d26b2df`)

---

## [v0.28.112] — 2026-08-26

### Feature: BPM-111.06 Slice C1+C2 — PDF-Vorschau (ADR-062)

Zentraler PDF-Render-Port **`IPdfRenderService`** (Domain) mit einziger Implementierung `WindowsPdfRenderService` via `Windows.Data.Pdf` im Composition Root — **TFM-Bump NUR App** auf `net10.0-windows10.0.19041.0` (Mindest-OS bleibt Win10 1809), Module/Tests unverändert. Neues **`PlanPreviewWindow`** im Tab „Manuell sortieren" (Rechtsklick → „Vorschau"): **Startansicht = Plankopf** (rechte untere Blattecke, A4-Ausschnitt — mm-genau über `PdfPageRender` mit rotationsbereinigter Blattgröße aus der MediaBox), Mausrad = cursorzentrierter Zoom, mittlere Maustaste = Verschieben, Seiten blättern, Buttons „Plankopf"/„Ganzes Blatt" + „↗ In Standard-App öffnen". Dazu **`IFileLauncher`-Port + `LocalFileLauncher`** (ADR-060 P.3, ShellExecute) — PDF-Bearbeitung passiert bewusst extern, nie in-app. (Commit `5c782ef`)

---

## [v0.28.111] — 2026-08-26

### Docs: ADR-062 Zentraler PDF-Render-Port

ADR-062 (IPdfRenderService: Port in Domain, Implementierung im App-Root, TFM-Bump nur App; PDF-Bearbeitung = eigener Port post-V1) + ADR-Inhaltsverzeichnis-Nachzug 059–061 + INDEX-Routing. (Commit `a03f90d`)

---

## [v0.28.110] — 2026-08-26

### Feature: BPM-111.06 Slice A2+A3 — Panel-Edit + Bezeichnung

Detail-Panel: **Plannummer/Index editierbar**; „Re-Match anwenden" behandelt den Edit als Identitätswechsel (Spez 111.06) — Neuklassifikation via `RematchByNumber`, Zeile wird im Grid ersetzt, stale Pending-Zuordnung der alten Identität verworfen. Neu: Feld **„Bezeichnung"** fließt end-to-end (`PendingAssignment.Title` → `ClassifiedImportFile.Title` → `plan_documents.title`, vorher hart `""`) — nur für Zeilen, die ein neues Dokument anlegen; Updates behalten den Bestandstitel. Titel-Edits nach der Radial-Zuordnung werden beim Bestätigen synchronisiert. (Commit `e41b343`)

---

## [v0.28.109] — 2026-08-26

### Refactor: BPM-111.06 Slice A2 — Match-Kern pure

Klassifikations-Kern des `ManualFirstCaptureService` als statische, pure **`MatchByNumber`** extrahiert (Bucket B/C/D) + Instanz-`RematchByNumber` als Einzel-Re-Match-Fundament für den Panel-Edit. Verhalten unverändert (alle Bestandstests grün), 6 neue Unit-Tests. (Commit `81cb399`)

---

## [v0.28.108] — 2026-07-02

### Fix: BPM-111.06 Slice A1 — NRE bei „Update übernehmen"

Nach einem Import konnte „⬆ Update übernehmen" crashen: `CommandParameter="{Binding Row}"` kam beim DataContext-Wechsel des ContentControls als `null` an. `TakeUpdate` arbeitet jetzt parameterlos auf `SelectedDetail.Row` (null-sicher). Hinweis: Commit trägt versehentlich die .107-Message. (Commit `f13301f`)

---

## [v0.28.107] — 2026-07-02

### Feature: BPM-111.06 Slice A1 — Detail-Panel MVVM

Das Detail-Panel im Tab „Manuell sortieren" wechselt von Code-Behind-Text auf MVVM: neues **`CaptureDetailViewModel`** (`SelectedDetail`) mit Dateiname, Zielordner, Plannummer/Index, Reason-Hinweis, **Index-Historie** aus `plan_revisions` (`GetRevisionsForDocument`) und „Update übernehmen"-Sichtbarkeit; XAML bindet via ContentControl-DataTemplate. (Commit `ba87926`)

---

## [v0.28.106] — 2026-07-02

### Feature: BPM-111.05 Slice 3d — Recovery-Check vor „Import bestätigen"

Der manuelle Bestätigen-Pfad (Sticky-Radial) bekommt denselben Schutz wie der Profil-Import: **`PreImportRecoveryCheck`** (pure Gate, keine DB/Disk) + `PreImportCheckResult`; `ConfirmImportAsync` blockiert bei pending Import (App-Crash, gesyncter Fremd-Stand) mit ⛔-Status, bis die Recovery-Strecke (BPM-016) gelaufen ist. 3 Unit-Tests. Damit ist BPM-111.05 funktional komplett (Sticky-Radial ersetzt die alten Slices 3b/3c). (Commit `ea7ab8e`)

---

## [v0.28.105] — 2026-07-02

### Docs: CHANGELOG v0.28.100–.104

CHANGELOG-Einträge .100–.104 (Sticky-Radial A/B/C + Mockup-Spez) nachgezogen. (Commit `cc09c00`)

---

## [v0.28.104] — 2026-07-02

### Feature: BPM-111.05 Sticky-Radial Slice C — Farbrampe

Ring 2/3 im Radial „Manuell sortieren" bekommen die Typfarbe mit einer **feld-stabilen Helligkeitsrampe** (dunkel→hell über die Felder, nach Original-Position), **voll deckend** (keine Transparenz), mit **adaptiver Textfarbe** (dunkel auf hell, weiß auf dunkel/markiert). Rampe im `RadialSelectionController` (`WithRamp`/`RampHex`), Rendering im `RadialCaptureControl`. Ring 1 = Typfarben unverändert. (Commit `89c3411`)

---

## [v0.28.103] — 2026-07-02

### Feature: BPM-111.05 Sticky-Radial Slice B — Mausrad-Rotation

Das Mausrad dreht nur die Ring-Ebene **unter dem Cursor** (Ring 1 Typen / Ring 2 Bauteile bzw. Kategorien / Ring 3 Geschosse) — feld-stabil über einen Rotations-Offset je Ring im `RadialSelectionController` (Datenlisten bleiben unverändert), Offsets bei Ebenenwechsel/Neustart zurückgesetzt. Wheel-Handler im `ManualCaptureView` rendert die getroffene Ebene neu. Ersetzt weitgehend die geplanten Slices 3b (Quick-Filter) / 3c (Pagination/Caps). (Commit `4d7a07d`)

---

## [v0.28.102] — 2026-07-02

### Change: BPM-111.05 Sticky-Radial Gesten-Kern (Slice A)

Das Radial „Manuell sortieren" wechselt vom „halten & loslassen" auf ein **einrastendes** Modell: kurz halten (260 ms) → Radial rastet ein und bleibt offen (Taste darf losgelassen werden, kein `Mouse.Capture` mehr → freie Navigation, Hover/Dwell nativ im Control). Linksklick auf ein Segment = Zuordnung (Pending); Klick ins Leere lässt das Radial offen; Rechtsklick = abbrechen (Dateien lösen + schließen). Das Multi-Select-Verhalten (BPM-115) bleibt erhalten. (Commit `2eb5826`)

---

## [v0.28.101] — 2026-07-02

### Docs: Mockup ManuellSortieren auf Sticky-Radial

Mockup `02_ManuellSortieren.html` + verbindliche Interaktionsspez auf das Sticky-Radial-Modell umgeschrieben (Einrasten, Mausrad dreht die Ebene unter dem Cursor, feld-stabile Helligkeitsrampe, Rechtsklick = Abbruch). Grundlage für den Code-Umbau BPM-111.05. (Commit `9d58184`)

---

## [v0.28.100] — 2026-07-02

### Docs: CHANGELOG v0.28.97–.99 + ADR-061 done

CHANGELOG-Einträge .97/.98/.99 nachgezogen; ADR-061 Implementierungs-Status auf „Umgesetzt" (BPM-113 abgeschlossen). (Commit `d2d61cc`)

---

## [v0.28.99] — 2026-07-02

### Fix: Mehrfachauswahl beim Hold-Verschieben (ManuellSortieren)

Im Tab „Manuell sortieren" ordnete das Hold-Verschieben trotz Mehrfachauswahl nur **eine** Datei zu: Beim Maus-Runter (ohne Modifier) auf eine bereits markierte Zeile kollabierte die WPF-`ListBox` (SelectionMode=Extended) die Selektion auf diese Zeile, bevor der Hold-Timer (260 ms) das Radial öffnete. `ManualCaptureView.OnRowPreviewMouseDown` unterdrückt den Kollaps jetzt (`e.Handled`) bei Klick auf eine mehrfach-markierte Zeile; der reine Klick (ohne Hold) holt die Einzelauswahl im `MouseUp` nach. (BPM-115, Commit `4fc2bd5`)

---

## [v0.28.98] — 2026-07-02

### Change: BPM-113.06 Slice 0.6c — profile.TargetFolder entfernt (RecognitionProfile SchemaVersion 5)

Abschluss von ADR-061: `RecognitionProfile.TargetFolder` und `PatternTemplate.TargetFolder` entfernt, SchemaVersion 4→5 (Strict Reset, kein Migrations-Code). `IProfileManager.BuildFromWizard` ohne `targetFolder`-Parameter — der Zielordner kommt ausschließlich aus den DB-Stammdaten via `DocumentTargetPathResolver`. `ImportExecutionService` schreibt `plan_documents.target_folder` aus dem Root-Segment des aufgelösten Pfads. ProfileWizard-Reste (TargetFolderOptions/SelectedTargetFolder/UseCustomFolder/CustomFolderName) entfernt. 5 Test-Dateien auf v5 nachgezogen, 396/396 grün. Frühphasen-Reset: `.bpm/profiles/*.json` + `pattern-templates.json` + `planmanager.db`. Schließt **BPM-113** (ADR-061 komplett). (Commit `1388bbc`)

---

## [v0.28.97] — 2026-07-02

### Docs: CHANGELOG v0.28.84–.96 + ADR-060/061 Slice-Status

CHANGELOG-Rückstand (.84–.96) nachgezogen; ADR-060/061 Implementierungs-Status auf „In Progress" + Slice-Fortschritt aktualisiert. (Commit `443654b`)

---

## [v0.28.96] — 2026-07-02

### Feature: Fensterlage merken (WINDOWPLACEMENT)

Das Hauptfenster merkt sich Position, Größe und Maximiert-Zustand über die Win32-`WINDOWPLACEMENT`-Struktur (`GetWindowPlacement`/`SetWindowPlacement`), persistiert geräte-lokal in `device-settings.json` (`WindowPlacementSettings` in `DeviceSettings`). Beim Schließen gespeichert, beim Start wiederhergestellt — Windows klemmt selbst auf einen sichtbaren Bildschirm (robust bei Multi-Monitor + unterschiedlichen DPI, passt zu PerMonitorV2). Erststart (noch nichts gespeichert) maximiert; `MainWindow.WindowStartupLocation` auf `Manual`. (Commit `4e2baa1`)

---

## [v0.28.95] — 2026-07-02

### Feature: BPM-113.06 Slice 0.6b — ProfileWizard wählt Dokumenttyp statt Zielordner

ProfileWizard-Schritt 4 bietet jetzt einen Dokumenttyp-Picker aus `ProjectDatabase.GetDocumentTypes` statt der hardcodierten Zielordner-Liste. `SelectedDocumentType` setzt den Anzeigenamen; `SaveProfile` schreibt `documentTypeId` = stabile `type.Id`, die in `ImportPlanBuilder`/`DocumentTargetPathResolver` auflösbar ist — schließt den 0.6a-Bruch, bei dem der normalisierte Freitext-Name nie eine DB-Id/Key traf. `IProfileManager.BuildFromWizard` um trailing-optional `documentTypeId` erweitert (Fallback = alte Normalisierung, Tests unberührt). 396/396 Tests grün, runtime-verifiziert. Offen: Slice 0.6c (`TargetFolder` entfernen + SchemaVersion 5). (Commit `ff08ef7`)

---

## [v0.28.94] — 2026-07-02

### Fix: BPM-114 Eingang-/Plans-Pfad an nummerierte Ordnervorlage koppeln

`ProjectFolderService.CreateProjectFolders` setzt `project.Paths.Plans`/`Inbox` jetzt auf die real angelegten, nummerierten Vorlagen-Ordner (z. B. `01 Planunterlagen\_Eingang`) statt sie auf den Klassen-Defaults `Pläne\_Eingang` zu belassen. Vorher fanden Import, ManuellSortieren und ProfileWizard-Schritt 1 den Eingang nicht (0 Dateien) — bei jedem Projekt mit nummerierter Vorlage, V1-blockierend. Entdeckt bei der Runtime-Verifikation von Slice 0.6b. (Commit `726b93c`)

---

## [v0.28.93] — 2026-06-24

### Feature: BPM-113.06 Slice 0.6a — Import-Zielpfad über DocumentTargetPathResolver

Der Import berechnet den Zielpfad jetzt über den `DocumentTargetPathResolver` statt über `profile.TargetFolder`. `ImportPlanBuilder` mit fixierter `MapRings`-Regel (Ring3 = Geschoss; Ring2 = erster Bauteil-Id-Wert bzw. erster Nicht-Geschoss-Wert), verdrahtet in `ImportWorkflowService`/`ProjectDatabase` und `ProjectDetailView`. Additiv — `TargetFolder` bleibt vorerst im Modell. (Commit `ebe4168`)

---

## [v0.28.92] — 2026-06-24

### Feature: BPM-113.05 Slice 0.5 — DocumentTargetPathResolver

Neuer PlanManager-Service `DocumentTargetPathResolver`: baut den Ablage-Zielpfad ausschließlich aus DB-Stammdaten (`root_relative_path`/`folder_name`/Ring2/Ring3), Fail-Fast ohne Teilpfad, Auflösung je Ebene Id → key → normalisierter Name (kein Fuzzy). 8 Tests. (Commit `f5363a3`)

---

## [v0.28.91] — 2026-06-24

### Feature: BPM-113.04b-2 — Neu-Dokumenttyp-Pflichtdialog

„+ Neu…"-Schnellanlage als MVP-Pflichtdialog (Name/Ablagebereich/Unterteilung/Ordnername), Ring-1 verdrahtet. (Commit `3049a9e`)

---

## [v0.28.90] — 2026-06-24

### Feature: BPM-113.04b-1 — DocumentTypeCreationService

`DocumentTypeCreationService` (key-Erzeugung + Dedup + Normalisierung), `AddDocumentType` verdrahtet. (Commit `feb1a36`)

---

## [v0.28.89] — 2026-06-24

### Feature: BPM-113.04a — Seed aus FolderTemplate

Dokumenttyp-Seed kommt jetzt aus dem FolderTemplate statt aus hardcodierten `_builtins`: Typ-Metadaten + `key`/`root_relative_path`/`folder_name` aus der Ordnerstruktur, Protokolle-Root. (Commit `62ed690`)

---

## [v0.28.88] — 2026-06-24

### Feature: BPM-113.03 Slice 0.3 — ProjectDatabase Insert/Read

`ProjectDatabase` liest/schreibt `document_types.key`/`root_relative_path` und `building_levels.folder_name` (Einmal-Regel), key/root Round-Trip. (Commit `4b86e7e`)

---

## [v0.28.87] — 2026-06-24

### Feature: BPM-113.02 Slice 0.2 — DB-Schema (key/root_relative_path + Unique-Index)

`document_types` um `key` + `root_relative_path` erweitert, partieller Unique-Index (project_id, key), `building_levels.folder_name`. (Commit `9f84fed`)

---

## [v0.28.86] — 2026-06-24

### Feature: BPM-113.01 Slice 0.1 — Domain-Models (key/root_relative_path)

Domain-Models: `document_types` `key`/`root_relative_path`, `BuildingLevel.FolderName`, FolderTemplate-Typmetadaten. (Commit `474f03a`)

---

## [v0.28.85] — 2026-06-24

### Feature: BPM-112 Slice 0 — Dateisystem-Ports (ADR-060)

FS-Ports `IFileSystemReader`/`IFileSystemWriter`/`IPathService` (Domain) + `LocalFileSystem`-Adapter (Infrastructure) + DI-Registrierung + `FakeFileStore` + Contract-Tests. Grundlage für die Migration der ~29 direkten System.IO-Stellen (Slices 1–6 offen). (Commit `e60fa3c`)

---

## [v0.28.84] — 2026-06-24

### Docs: CHANGELOG v0.28.81–.83 nachgezogen

CHANGELOG-Lücke geschlossen: Einträge für Slice 3a (v0.28.83), ADR-060/061 + CGR-2026-06-22-bpm-architektur (v0.28.82) sowie die vorherige CHANGELOG-Nachpflege (v0.28.81). (Commit `528d74f`)

---

## [v0.28.83] — 2026-06-24

### Feature: BPM-111.05 Slice 3a — „+ Neu…"-Segmente im Ring

Schnellanlage neuer Stammdaten direkt aus der Radial-Erfassung: „+ Neu…"-Segmente erzeugen Typ, Bauteil, Geschoss bzw. Kategorie ohne Umweg über die Stammdaten-Dialoge. `ProjectDatabase` um `InsertBuildingPart`/`InsertBuildingLevel` erweitert, Add-Items im `RadialSelectionController`, `TryQuickAdd` + `PromptName`-Dialog in `ManualCaptureViewModel`/`ManualCaptureView`. Bewusster Zwischenstand vor dem ADR-061-Umbau — Slice 3a geht in BPM-113 Slice 0 auf (Typ-Erzeugung wird dort auf das neue Ordner-Wahrheit-Modell gehoben). 346/346 Tests grün. (Commit `87e7162`)

---

## [v0.28.82] — 2026-06-24

### Docs: ADR-060 + ADR-061 + CGR-2026-06-22-bpm-architektur

Architektur-Review-Ergebnis dokumentiert. **ADR-060 (Dateisystem-Ports):** `IFileSystemReader`/`IFileSystemWriter`/`IPathService` (Domain) + `LocalFileSystem`-Adapter (Infrastructure), alle Module via DI, kein direktes System.IO mehr; + `IFileLauncher`/`IShareService`. **ADR-061 (Ordner-Wahrheit + Resolver):** DB führend, FolderTemplate nur Bootstrap; `document_types` + `key` (UNIQUE) + `root_relative_path` (Multi-Root) + `folder_name`; `DocumentTargetPathResolver` (Fail-Fast, IDs vor Namen); `profile.TargetFolder` gebrochen (RecognitionProfile SchemaVersion 5). Voller Review-Verlauf unter `Docs/Referenz/chatgpt-reviews/CGR-2026-06-22-bpm-architektur/` (r1–r4, 4 Runden, beidseitiger Sign-off), INDEX-Routing ergänzt. (Commit `dca9ce4`)

---

## [v0.28.81] — 2026-06-22

### Docs: CHANGELOG v0.28.74–.80 nachgezogen

CHANGELOG-Lücke geschlossen: Einträge für BPM-111.05 Slices 1–2c (RadialCaptureControl, Dokumenttyp-Stammdaten, ManualCaptureView + Gesten-Host, ManuellSortieren-Tab), ADR-059-Addendum sowie den Kürzel-Fix (v0.28.80) nachgetragen. (Commit `6af9d05`)

---

## [v0.28.80] — 2026-06-22

### Fix: Bauteil-Kürzel-Pflicht + Radial-Fallback bei leerem Kürzel

Im Live-Test BPM-111.05 zeigte sich: Bauteile ohne Kürzel (`short_name` leer) erzeugten leere Radial-Segmente (Ring 2/3) und einen leeren `folder_name` — kein Radial- oder DB-Bug, sondern fehlende Stammdaten. Zwei Absicherungen: (1) Kürzel-Pflicht im Bauteil-Editor (`ProjectEditDialog`) — OK blockiert mit Warnhinweis bei leerem Kürzel. (2) Defensiver Fallback im `RadialSelectionController`: neuer `EffectivePartName`-Helper (Kürzel, sonst Beschreibung) als konsistente Identität für Ring-2-Label und Ring-3-/Ziel-Matching; `folder_name` fällt bei Altdaten auf die normalisierte Beschreibung zurück (folder_name-Einmal-Regel gewahrt — nur transientes Pending-Ziel). 343/343 Tests grün. (Commit `63addd2`)

---

## [v0.28.79] — 2026-06-11

### Feature: BPM-111.05 Slice 2c — ManuellSortieren-Tab verdrahtet

Tab „Manuell sortieren" im Projektdetail komplett angebunden: Lazy-Init beim Tab-Wechsel, `ProjectDatabase` (bpm.db-Stammdaten) durchgereicht, `planmanager.db`-Lifecycle (Öffnen/Dispose pro Projekt). Kette geschlossen: Projekt → Tab → Seed → Buckets → Tabelle → Halten → Radial → Pending → Bestätigen → Undo. (Commit `8fbf976`)

---

## [v0.28.78] — 2026-06-11

### Feature: BPM-111.05 Slice 2b — ManualCaptureView + Gesten-Host

`ManualCaptureView` + Code-Behind als Gesten-Host (Hold 260ms / Capture / Ghost-Anker, Abbruch >40px), `ManualCaptureViewModel` (Eingangs-Tabelle aus Buckets, Pending-Anbindung), `RadialSelectionController` als reine, UI-freie Ebenenlogik (Ring 2 je `ring2_source`, Ring 3 Geschosse je Bauteil, Dwell-Commit, Animation nur bei Ring-Erscheinen). (Commit `2ad10b3`)

---

## [v0.28.77] — 2026-06-11

### Feature: BPM-111.05 Slice 2a — Dokumenttyp-Stammdaten

`document_types` + `document_type_categories` in bpm.db (projekt-scoped), `DocumentTypeSeedService` seedet 7 Built-in-Typen (Polierplan/Statik/Bewehrung/Schalung/Architektur = BuildingParts, Fertigteile/Protokolle = Categories), folder_name-Einmal-Regel. `PlanValueNormalizer` von PlanManager nach Infrastructure verschoben (Seed/ProjectDatabase brauchen die folder_name-Erzeugung ebenfalls). (Commit `6ace879`)

---

## [v0.28.76] — 2026-06-11

### Docs: ADR-059-Addendum — typabhängiges Unterteilungs-Schema

ADR-059-Addendum: `document_types` + `document_type_categories` in bpm.db, `ring2_source` je Typ (`building_parts`/`categories`/`none`), folder_name-Einmal-Regel (Feld statt Template), Seed-Definition. DB-SCHEMA Kap. 4.12/4.13 ergänzt. (Commit `d946a5a`)

---

## [v0.28.75] — 2026-06-11

### Feature: BPM-111.05 Slice 1 — RadialCaptureControl

`RadialCaptureControl` (Ring-Geometrie, daten-dummes Rendering) + `RadialGeometry`-Helfer, timerbasierter Dwell-Timer (110ms, nie MouseMove-gekoppelt), Erscheinen-Animation (140ms nur bei Ring-Erscheinen), Theme-Tokens statt Hardcoded-Farben. (Commit `4f715eb`)

---

## [v0.28.74] — 2026-06-11

### Docs: CHANGELOG v0.28.71–.73 + PlanManager.md Kap. 11.4

CHANGELOG-Einträge .71–.73 nachgezogen, PlanManager.md Kap. 11.4 um den Undo-Implementierungsstand (BPM-111.03/.04) ergänzt. (Commit `1fd33da`)

---

## [v0.28.73] — 2026-06-11

### Feature: BPM-111.04 — Pending Assignments + zweistufiges Undo

Stufe 1: `PendingAssignmentStore` (in-memory pro Session, Entscheidung Teil 43) — Radial/Panel schreiben nur Vorschlag, `Discard`/`Clear` verwirft. Bestätigung: `CaptureConfirmService` mappt Pending → `ImportDecision` (manuelle index-freie Keys; Update-Übernahmen nutzen document_key + Zielordner des bekannten Dokuments) und nutzt die bestehende Execute-Strecke (Journal vor Move). Stufe 2: `ImportUndoService` — Undo NUR letzter Import mit Preflight-Trockenlauf (Kap. 11), Dateien zurück in den Eingang, Archiv-Restore, DB-Rollback per Soft Delete + Supersede-Restore (via Audit-Events, `made_current`-Undo-Spur), Journal-Status `undone`. 7 neue Undo-Primitive in PlanManagerDatabase (kein Schema-Change). 11 neue Tests, 316/316 grün. (Commit `22ebcc4`)

---

## [v0.28.72] — 2026-06-11

### Feature: BPM-111.03 — ManualFirstCapture-Workflow (Buckets A/B/C/D)

`ManualFirstCaptureService`: Scan → MD5 → Lightweight-Kandidaten → deterministisches Matching gegen bekannte `plan_documents` → Buckets A Dublette (MD5, Vorrang) / B Update-Vorschlag (bekannter Plan + anderer Index, OLDER_REVISION-Warnung) / C manuelle Erstaufnahme (Radial) / D Konflikt (gleicher Index bzw. mehrdeutige Plannummer). Klassifikation als reine statische Funktion, profil-unabhängig, read-only. Neue Lookups `GetCurrentDocumentLookup` + `GetKnownMd5Lookup`. 12 neue Tests, 305/305 grün. (Commit `19bfef1`)

---

## [v0.28.71] — 2026-06-10

### Docs: CHANGELOG v0.28.68–.70 nachgezogen

Einträge für CHANGELOG-Nachzug (.68), Mockup Radial-Erfassung (.69) und BPM-111.02 Extractor (.70). (Commit `e4e450e`-Folgecommit)

---

## [v0.28.70] — 2026-06-10

### Feature: BPM-111.02 — Lightweight-Extractor + IPlanValueNormalizer

`LightweightPlanExtractor` liest deterministisch Kandidaten aus Dateinamen (PlanNr inkl. Prefix/geklebtem Index `011vorab`/`002a`, Index + RevisionKind, Geschoss mit strikter Tokenliste, BuildingPartHint, Plantyp-/Protokoll-Keywords mit Kombi-Erkennung, Datum, Kopiermarker `_(1)`) — nur Assist nach ADR-059. `IPlanValueNormalizer` (Domain) mit drei Kontexten Key/Match/FolderName; `DocumentKeyBuilder` und `ImportWorkflowService` (`RevisionKindDetector`) delegieren an die zentralen Helfer, Verhalten unverändert. 35 neue Tests, 293/293 grün. (Commit `e4e450e`)

---

## [v0.28.69] — 2026-06-10

### Docs: BPM-111.01 — Mockup Radial-Erfassung

`02_ManuellSortieren.html` komplett neu (Hold-Drag-Geste mit timerbasiertem Dwell, typabhängige Ringe, „+ Neu…" je Ebene, Pending/Update-Markierung, Archiv-Tab mit Undo/Verschieben, angedocktes Vorschau-Fenster mit Plankopf-Ausschnitt; Spezifikation im HTML-Header). Alte Listenvariante nach `_Archiv/`, Tab-Link in `01_Profile.html` aktiviert, Sitemap nachgezogen. (Commit `3e1ee59`)

---

## [v0.28.68] — 2026-06-10

### Docs: CHANGELOG v0.28.65–.67 nachgezogen

Einträge für ADR-059-Sign-off (.66), BPM-109.05a-Status (.65) und BPM-110-Fix (.67) ergänzt. (Commit `dbfd946`)

---

## [v0.28.67] — 2026-06-10

### Fix: BPM-110 — Feldkey-Bruch (Index-Erkennung war tot)

`FileParseService` schreibt `ExtractedFields` mit `segment_types.id` (snake_case: `plan_number`/`plan_index`), `ImportWorkflowService` las aber die toten Keys `plannumber`/`planindex` → `PlanNumber`/`RevisionToken` immer null, Index-/Revisions-Erkennung faktisch tot. Fix: zentrale Konstanten-Klasse `SegmentTypeIds` (Domain, 16 Built-in-IDs + `documentType`-Sonderkey); ImportWorkflowService, DocumentKeyBuilder, SegmentTypeSeedService und `RecognitionProfile`-Default nutzen sie (Single Source). Zusatzbug behoben: `DocumentKeyBuilder` lowercaste IdentityField-Namen → Custom-Segmenttypen (ULID) wurden nie gefunden, jetzt Verbatim-Lookup. 6 neue Regressionstests (`DocumentKeyBuilderTests`), 258/258 grün. (Commit `dbfd946`)

---

## [v0.28.66] — 2026-06-09

### Docs: ADR-059 — Recognition v2 / Plan-Erfassung

ADR-059 (CGR-2026-06-09-plan-erkennung, 3 Runden Sign-off): MVP = manuelle Erstaufnahme (Strategie B) + deterministisches MD5/Index-Matching, Auto-Extraktion nur Assist. V1-UI = Radial-/Nautilus-Menü mit Caps + Pending Assignments + Listen-Fallback, Geschoss als 3. Ring. Neue Tasks BPM-110 (Feldkey-Fix) + BPM-111 (Radial-Erfassung).

---

## [v0.28.65] — 2026-06-09

### Docs: BPM-109.05a Status nachgezogen

Architektur Kap. 4.1 (IPlanLookupService Interface-Stub implementiert) + CHANGELOG v0.28.63/.64.

---

## [v0.28.64] — 2026-06-09

### Feature: BPM-109.05a — IPlanLookupService Interface + Stub (Foundation Slice komplett)

Öffentliche PlanManager-API `IPlanLookupService` (Domain): `FindCurrentPlansAsync` (Zeitreise) + `CreatePlanContextSnapshotAsync` (fixed_revision-Snapshot). DTOs `PlanLookupResult` (mit `EffectiveDate` = `ReleasedAt ?? ReceivedAt` + `IsDateFallback` für Bautagebuch-Datumspriorisierung) + `PlanContextFilter`. Stub `PlanLookupService` wirft `NotImplementedException` (Query-Logik = BPM-109.05, post-V1). 4 Tests, 252/252 grün. **Damit Foundation Slice BPM-109 komplett (.01–.04 + .03b + .04b + .05a) — V1-Sperrposten aufgehoben.**

---

## [v0.28.63] — 2026-06-09

### Docs: BPM-109.03b/.04/.04b nachgezogen

DB-SCHEMA Kap. 6.7.2 (`released_at` + Drei-Zeiten-Semantik), ADR-058-Addendum (Drei-Zeiten-Modell + Bautagebuch-Priorisierung), CHANGELOG v0.28.59–.62, PlanManager.md Kap. 10.

---

## [v0.28.62] — 2026-06-09

### Feature: BPM-109.04b — released_at (Freigabedatum) im Schema v2.0

Drittes Zeit-Konzept: `released_at` (Freigabedatum pro Index) neben `received_at` (Import) und `current_from`/`superseded_at` (Gültigkeitsfenster). Spalte `released_at TEXT` (nullable) auf `plan_revisions`, `PlanRevision.ReleasedAt`, `InsertRevision`-Optionalparameter, Lese-Methoden. Befüllung vorerst NULL (Quelle Plankopf-OCR/manuell = post-V1). Bautagebuch priorisiert `released_at ?? received_at` mit visueller Fallback-Markierung. 248/248 grün.

---

## [v0.28.61] — 2026-06-09

### Feature: BPM-109.04 — Revision-Zeitlogik + Audit-Events

Ein `actionTime` pro Import-Aktion → `superseded_at`(alt) == `current_from`(neu) (Zeitreise lückenlos). `plan_revision_events` verdrahtet (`created`/`superseded`/`file_linked`). Neue Lese-Primitive `GetRevisionEvents` + `GetRevisionsForDocument`. 2 Lifecycle-Tests. 247/247 grün.

---

## [v0.28.60] — 2026-06-09

### Fix: BPM-109.03b — document_key via DocumentKeyBuilder statt All-Fields-Join

Der `DocumentKeyBuilder`-Key (kuratiert, index-frei) wurde berechnet aber verworfen; `RevisionDecisionService` nutzte einen naiven Join über alle `ExtractedFields`. Fix: `DocumentKey` in `ClassifiedImportFile` durchgereicht, `RevisionDecisionService` nutzt ihn → Revisionen gruppieren index-frei (Voraussetzung für Supersede). 245/245 grün.

---

## [v0.28.59] — 2026-06-08

### Feature: BPM-109.03 — Pipeline auf Schema v2.0 (Import reaktiviert)

`ImportExecutionService`/`ImportWorkflowService` auf die Document/Revision/File-Primitive umgestellt (Document-Resolve, Supersede, File-Link, `GetCurrentRevisionLookup`); Reihenfolge Journal→Move→Cache unverändert. Neue Primitive `InsertFileForRevision` + `SupersedeCurrentRevision`, `ProjectId`-Property; die 5 alten Fail-Fast-Methoden entfernt. Live-Smoke-Test: 8 OK / 0 Fehler. 245/245 grün.

---

## [v0.28.57] — 2026-06-08

### Feature: BPM-109.02 — Domain Models + Repository-Primitive (Schema v2.0)

5 neue Domain-Records (`PlanDocument`, `PlanRevision`, `PlanDocumentSegment`, `PlanRevisionEvent`, `PlanContextLink`) in `PlanArchiveModels.cs` + `PlanArchive`-Konstanten (Status/EventType/ResolutionMode/LinkType). `PlanManagerDatabase` um additive Document-zentrische Primitive erweitert: `ResolveOrCreateDocument`, `GetDocumentByKey`, `InsertRevision`, `InsertSegment`, `InsertRevisionEvent`, `GetCurrentRevisionForDocument`, `GetCurrentRevisionLookup`. Cross-DB-Bezüge bleiben Soft References. Die 5 alten Fail-Fast-Methoden + `ImportExecutionService` bewusst unangetastet — Pipeline-Verdrahtung in `.03`. 7 neue Repository-Tests (inkl. Unique-current-Constraint + Unique-Segmenttyp). 245/245 grün.

---

## [v0.28.56] — 2026-06-08

### Docs: BPM-109.01 Status nachgezogen

DB-SCHEMA Kap. 6.7 + PlanManager.md Kap. 10 auf „Schema v2.0 DDL implementiert" gesetzt; CHANGELOG-Einträge v0.28.54/.55 ergänzt.

---

## [v0.28.55] — 2026-06-08

### Feature: BPM-109.01 — Schema v2.0 DDL (Plan-Archiv-Persistenz Foundation Slice)

`PlanManagerDatabase.EnsureTables()` erzeugt jetzt das Drei-Ebenen-Schema v2.0 (11 Tabellen): `plan_documents` (NEU), `plan_revisions` umgebaut (`document_id`-FK + `current_from`/`superseded_at`/`received_at` + Status-CHECK `current/superseded/rejected` + Unique-Index auf `current`), `plan_document_segments` / `plan_revision_events` / `plan_context_links` (NEU); `plan_files` / `revision_file_links` / `import_*` unverändert. `schema_version=2.0`. Cross-DB-Bezüge (`building_part_id`/`building_level_id`/`segment_type_id`) als Soft References ohne FK (ADR-058-Addendum) — harte FKs nur innerhalb `planmanager.db`. Die 5 Cache-Methoden vorläufig Fail-Fast (`NotSupportedException`, BPM-109.02-Marker) — Import bis `.02` bewusst pausiert. Frühphasen-Reset: `planmanager.db` löschen, keine Migration. Build 0/0, Tests 238/238 grün.

---

## [v0.28.54] — 2026-06-08

### Docs: BPM-109 ADR-058-Addendum — Cross-DB Soft References (CGR r3)

Cross-Review r3 (Claude + ChatGPT) bestätigte: zwei DBs behalten (`bpm.db` System of Record + `planmanager.db` rebuildbarer per-Projekt-Cache), Cross-DB-Bezüge als Soft References (kein FK über SQLite-Datei-Grenze). Keine Konsolidierung vor V1. ADR-058-Addendum + DB-SCHEMA Kap. 6.7-Korrektur (FK-Klauseln → SoftRef-Kommentare + Cross-DB-Block) + neue Kap. 4.11 `building_part_aliases` (verschoben nach `bpm.db`, harter FK) + Kap. 2.3 FK-Übersicht + PlanManager.md Kap. 10. CGR-Serie r3 archiviert.

---

## [v0.28.53] — 2026-06-08

### Docs: BPM-109 Plan-Archiv-Persistenz v2 — Foundation-Slice-Doku nach CGR-Sign-off

### Hintergrund
Nach 2 Runden Cross-Review (CGR-2026-06-08-plan-archiv-architektur) wurde die Schema-v2.0-Erweiterung für PlanManager beschlossen: Drei-Ebenen-Modell (`plan_documents` / `plan_revisions` / `plan_files`) mit `plan_document_segments`, `plan_revision_events`, `plan_context_links`, `building_part_aliases`. ChatGPT trug Herberts „vor V1"-Entscheidung mit, korrigierte aber Roadmap auf Foundation Slice (`.01–.04 + .05a`) — Rest post-V1. BPM-080.05/081 komplett pausiert bis Schema steht.

### Hinzugefuegt
- `Docs/Referenz/ADR.md` **ADR-058: Plan-Archiv-Persistenz (BPM-109)** — Drei-Ebenen-Modell + Foundation Slice + 12 Entscheidungspunkte + Stop-Punkte + `fixed_revision`-Snapshot-Pflicht (fachliche Invariante).
- `Docs/Kern/DB-SCHEMA.md` Kap. 6.7 **Schema v2.0 (BPM-109)** — vollständige DDL aller neuen/umgebauten Tabellen + Indizes + Beispiel-Zeitreise-Query + Foundation-Slice-Umfang vs. Post-V1.
- `Docs/Kern/BauProjektManager_Architektur.md` Kap. 4.1 **Öffentliche API IPlanLookupService** — `FindCurrentPlansAsync` + `CreatePlanContextSnapshotAsync` als Vertrag für Bautagebuch/Foto/Vorlagen.
- `Docs/Referenz/GLOSSAR.md` Kap. 3 — 11 neue Begriffe: PlanDocument, PlanRevision, superseded, rejected, PlanDocumentSegment, PlanRevisionEvent, PlanContextLink, fixed_revision, BuildingPartAlias, IPlanLookupService.
- `Docs/Referenz/chatgpt-reviews/CGR-2026-06-08-plan-archiv-architektur/` — kompletter Review-Archiv-Ordner (README + r1/r2 mit je 4 Dateien).
- `INDEX.md` Plan-Management-Routing — DB-SCHEMA Kap. 6.7 verlinkt, ADR-058 als Reference, IPlanLookupService als Cross-Modul-API-Pflicht.

### Geaendert
- `Docs/Referenz/ADR.md` Inhaltsverzeichnis — ADR-056 nachgetragen (Doku-Lücke aus BPM-108), ADR-058 hinzugefügt. Frontmatter Kapitel-Range auf „ADR-001 bis ADR-058".
- `Docs/Referenz/ADR.md` ADR-010 — Anhang „Erweiterung BPM-109: document_key bekommt FK-Bezug zu plan_documents" (Recognition-Logik selbst unverändert).
- `Docs/Referenz/ADR.md` ADR-053 — Anhang „Erweiterung BPM-109: project_id-Redundanz in planmanager.db" mit Begründung.
- `Docs/Kern/DB-SCHEMA.md` Kap. 6 Header — Status-Hinweis auf v2.0 + Reset-Anweisung (Frühphasen-Regel: `planmanager.db` löschen statt Migration).
- `Docs/Module/PlanManager.md` Kap. 10 Header — Hinweis auf v2.0 + Pipeline-Erweiterung (Document-Resolve-Stage) + IPlanLookupService + Reset-Anweisung.
- `Docs/Module/PlanManager.md` Kap. 18 Verwandte ADRs — ADR-056 + ADR-058 ergänzt.
- `Docs/Kern/BACKLOG.md` — BPM-109 als V1-blocker im Schema-Block, BPM-092 nach BPM-109 gereiht.
- `Docs/Referenz/chatgpt-reviews/INDEX.md` — Serie-Status auf „Abgeschlossen" mit BPM-109-Verweis und Kernergebnis-Zeile.

### Tracker
ClickUp-Issue **BPM-109 Plan-Archiv-Persistenz v2 (Foundation Slice)** mit 8 Subtasks angelegt (`.01`–`.07` + `.05a`). BPM-080 + BPM-080.05 + BPM-081 auf `open` zurückgesetzt mit Blockierungs-Hinweis.

### Nicht-Code-Aenderung
Reine Doku-/Architektur-Pflege. Kein Code geändert, keine Tests betroffen. Test-Stand bleibt 238/238 grün.

---

## [v0.28.52] — 2026-05-19

### Fix: BPM-108 Manager-Dialog Fenster-Maße

### Geaendert
- `SegmentTypeManagerDialog.xaml`: Window-Maße auf 660x500 (statt 1100x780). Spaltenbreite je 320px (statt `*`). `MinWidth=MaxWidth=660` — Fenster ist horizontal nicht mehr resizable, damit kein "schwarzer Balken" rechts erscheint wenn Spalten kleiner sind als das Fenster. `MinHeight=440` — vertikal weiterhin resizable.

### Hintergrund
Live-Test in v0.28.51 zeigte: mit 50/50-Spalten (je ~542px) wirkten die Edit-Felder zu breit, und beim Verkleinern auf je 320px blieb das Fenster auf 1100px breit — die freie Fläche rechts war schwarz (Window-Background). Lock auf 660 fixt beides.

---

## [v0.28.51] — 2026-05-19

### Fix: BPM-108 Manager-Dialog UX-Korrekturen + Kosmetik

### Hinzugefuegt
- `SegmentTypeManagerViewModel.CreateNewGroupCommand` — legt neue Custom-Gruppe an (SortOrder = max + 10, IsBuiltin = false). Toolbar bekommt zwei Buttons: "+ Neue Gruppe" und "+ Neuer Segmenttyp".

### Geaendert
- Manager-Dialog Fenster: 780x600 → 1100x780 (MinWidth/Height 900x640). Edit-Panel-ScrollViewer entfernt — alle Felder sind sofort sichtbar.
- Body-Spalten: Liste/Edit-Panel jetzt gleich breit (50/50 statt */300).
- Per-Type-Toggle + Per-Group-Toggle: oval mit Slider-Punkt analog Mockup (30x16 rounded, grün wenn aktiv mit weissem Slider rechts, grau wenn deaktiviert mit hellem Slider links). Ersetzt das alte ●/○-Button-Design.
- Gruppen-Header-Click: Single-Click auf den Toggle-Slider statt Doppelklick auf den ganzen Header. Verhindert versehentliche Toggles beim Scrollen.
- Gruppen-Dropdown im Edit-Panel: explizites `ItemTemplate` mit `{Binding Name}` statt `DisplayMemberPath` — fixt die FQN-Anzeige ("BauProjektManager.Domain.Models.PlanM...") im SelectionBox.
- `ProjectDatabase.cs`: Log-Text + Description + XML-Doc-Kommentar von "Schema v2.1" auf "Schema v2.2 — BPM-108 segment_types/-groups" aktualisiert (Kosmetik, kein funktionaler Impact).

### Hintergrund
Live-Test in v0.28.50 zeigte 5 UX-Issues: kein "+ Neue Gruppe"-Button, falsches Toggle-Design (●/○ statt oval-Slider), ungleiche Spaltenbreite, ScrollViewer im Edit-Panel benötigt, Gruppen-Dropdown zeigt Klassen-FQN. Alle 5 Issues in diesem Patch behoben. 238/238 Tests bleiben grün (kein Test-Refactor nötig).

---

## [v0.28.50] — 2026-05-18

### Feature: BPM-108 Phase C Teil 5 — Auto-Import-Blockade bei ProfileHealth.MissingSegmentTypes

### Hinzugefuegt
- `ImportWorkflowService.AnalyzeAsync`: schliesst Profile mit `ProfileHealth != Valid` vom Recognizer-Match aus. Files matchen nicht gegen unhealthy Profile → landen bei `ImportStatus.Unknown` statt mit fehlerhafter Identity importiert zu werden. Pro unhealthy Profile wird ein Warning-Log mit Name/ID/Health/Missing-IDs geschrieben.
- `ImportAnalysisResult.UnhealthyProfiles` (List<RecognitionProfile>) + `UnhealthyProfileCount` (int) — UI kann darauf eine "Profile reparieren"-Hinweis-Bannermessung stuetzen.
- Log-Eintrag am Ende der Analyse zeigt zusaetzlich `{Unhealthy}` Anzahl ausgeschlossener Profile.
- 3 neue Tests (`ImportAnalysisResultTests`) — Empty/UnhealthyCount/Health-Filter-Contract.

### Hintergrund
Fuenfter und finaler Phase-C-Teil-Commit. Erfuellt CGR-Akzeptanzkriterium #17 (Health-Gating vor Auto-Import). Profile mit Missing-IDs bleiben in Manager-Dialog + Wizard sichtbar und reparierbar — nur die Import-Pipeline ueberspringt sie um stille Identity-Drift zu verhindern.

### BPM-108 Status: ✅ ALLE PHASEN ABGESCHLOSSEN
- Phase A (v0.28.44): Catalog Persistence + Seed
- Phase B (v0.28.45): Profilformat v4 + Health + Archive
- Phase C Teil 1 (v0.28.46): Wizard auf Catalog
- Phase C Teil 2 (v0.28.47): Inline-Popover „+ Eigenes"
- Phase C Teil 3 (v0.28.48): Manager-Dialog
- Phase C Teil 4 (v0.28.49): DevTool-UI fuer Archive
- Phase C Teil 5 (v0.28.50): Health-Gating bei Auto-Import

Alle 17 CGR-Akzeptanzkriterien sind erfuellt. ADR-056 Status: Phase A+B+C Implemented.

---

## [v0.28.49] — 2026-05-18

### Feature: BPM-108 Phase C Teil 4 — DevTool-UI fuer ProfileArchiveService

### Hinzugefuegt
- DevToolsDialog Reset-Tab: neue Quick-Action-Card "BPM-108: Profile/Templates auf v4 archivieren" (blau, kein App-Restart). Klick zeigt Confirm-Dialog, iteriert ueber alle Projekte (via `ProjectDatabase.LoadAllProjects()`), ruft `IProfileArchiveService.ArchiveOutdatedProfiles(projectRoot)` und `ArchiveOutdatedPatternTemplates(sharedDir)` auf. Summary-Dialog zeigt Anzahl bearbeiteter Projekte/Profile + Pattern-Template-Status. Inventar wird nach der Aktion neu geladen.
- `DevToolsDialog`-Konstruktor erweitert um optionalen `IProfileArchiveService`-Parameter.
- `MainWindow`-Konstruktor erweitert um optionalen `IProfileArchiveService` (Default null), wird an DevToolsDialog durchgereicht.
- `App.xaml.cs` DI: `IProfileArchiveService` aus dem Container in MainWindow injiziert.

### Geaendert
- Falls `IProfileArchiveService` nicht injiziert ist (z. B. isolierte Test-Konstellation), zeigt der Click eine Warnung statt zu crashen.

### Hintergrund
Vierter Phase-C-Teil-Commit: User koennen alte Schema-v3-Profile (und veraltete pattern-templates.json) ueber den DevTools-Dialog explizit archivieren, statt sie manuell aus dem Dateisystem zu loeschen. Erfuellt das Frühphasen-Prinzip aus ADR-056 (Reset statt Migration). DevTool-Aufruf ist bewusst NICHT automatischer App-Start-Side-Effect — User muss bewusst klicken + bestaetigen.

### Offen fuer Phase C
- **Commit 5 (Teil 5):** Auto-Import-Blockade im ImportWorkflowService bei `ProfileHealth.MissingSegmentTypes` (Akzeptanzkriterium #17).

---

## [v0.28.48] — 2026-05-18

### Feature: BPM-108 Phase C Teil 3 — Segmenttyp-Manager-Dialog

### Hinzugefuegt
- `SegmentTypeManagerViewModel` (PlanManager.ViewModels) — Listen-/Edit-Drafts, Save/Cancel-Commands, Toggle-Active, Soft-Delete (Custom), Create-New-Custom. Built-in-Aenderungen setzen automatisch `user_modified_*`-Flags.
- `SegmentTypeManagerDialog` (PlanManager.Views) — 2-Spalten-Dialog (Liste links + Edit-Panel rechts). Liste rendert alle 5 Gruppen mit Items, Inhalts-Badge BUILT-IN / EIGEN, Per-Item-Aktivieren/Deaktivieren-Toggle, Doppelklick auf Gruppen-Header toggelt Gruppe. Edit-Panel zeigt Name + Fachrolle (read-only bei Built-ins mit Info-Text wie „Genau ein Segment mit dieser Rolle ist pro Profil erforderlich.") + Token (read-only) + Gruppe-Dropdown + 12er-Farbpalette. Buttons Speichern/Verwerfen/Löschen (Löschen nur bei Custom).
- `GroupBucket` Helper-Klasse — Gruppe + ihre Items fuer XAML-DataTemplate-Bindings.
- Wizard Schritt 2: Link „⚙ Segmenttypen verwalten…" oeffnet Manager-Dialog. Owner = Wizard.
- 10 neue Tests (`SegmentTypeManagerViewModelTests`) — Selection/Save-Roundtrip mit user_modified-Flag-Validierung, Toggle Type/Group, Custom-Create + Auto-Select + Token-Suffix-Konflikt, Soft-Delete, Built-in-Delete-No-Op.

### Geaendert
- `ProfileWizardDialog`: speichert `ISegmentTypeRepository`/`ISegmentTypeCatalog`/`IIdGenerator` als Felder, gibt sie an den Manager-Dialog weiter beim Klick auf den Link.
- Theme-Token-Korrektur: `BpmDanger` (nicht vorhanden) → `BpmError` (existierend) in Wizard-Inline-Popover und Manager-Dialog.

### Hintergrund
Dritter Phase-C-Teil-Commit: Manager-Dialog erfuellt CGR-Akzeptanzkriterium #15 (Built-in-Rollen unveraenderlich, im Manager read-only). Toggle/Edit/Delete-Flows greifen direkt auf Repository, Catalog wird nach jeder Mutation invalidiert — UI-State des Wizards wird via `Changed`-Event automatisch aktualisiert. Drag-Reorder, neue Gruppen anlegen und Gruppen-Soft-Delete folgen bei Bedarf in spaeteren Iterationen.

### Offen fuer Phase C
- **Commit 4 (Teil 4):** DevTool-UI fuer `IProfileArchiveService` (Reset-Knopf im DevToolsDialog).
- **Commit 5 (Teil 5):** Auto-Import-Blockade im ImportWorkflowService bei `ProfileHealth.MissingSegmentTypes` (Akzeptanzkriterium #17).

---

## [v0.28.47] — 2026-05-18

### Feature: BPM-108 Phase C Teil 2 — Inline-Popover "+ Eigenes" im Wizard Schritt 2

### Hinzugefuegt
- `TokenKeyGenerator` (PlanManager.Services, static) — `Normalize(name)` erzeugt snake_case (z. B. "Akustik-Klasse" → "akustik_klasse"), `EnsureUnique(baseKey, isTaken)` haengt numerischen Suffix an (`akustik_klasse_2`) wenn der token_key schon belegt ist.
- `SegmentTypeSeedService.GroupEigene` = `grp_eigene` — neue Built-in-Gruppe (SortOrder 50) als Standard fuer User-erstellte Custom-Segmenttypen. Built-in-Seeding aktualisiert bestehende bpm.db beim naechsten Start.
- `ProfileWizardViewModel` — Inline-Popover State + Commands:
  - `ShowCustomPopover` (bool), `CustomTypeName` (string), `CustomTypeColor` (hex), `CustomTypeTokenPreview` (computed, live), `CustomTypeError`, `CustomTypePalette` (12er).
  - `OpenCustomPopover(activeSegment?)` — Popover oeffnen, optional Pre-Bind auf ein Segment fuer Auto-Assign nach Anlage.
  - `CreateCustomTypeCommand` — Name validieren, token_key generieren + Unique-Check, `IIdGenerator.NewId()` fuer ULID, `ISegmentTypeRepository.SaveType()`, `ISegmentTypeCatalog.Invalidate()`, ggf. Segment-Zuweisung, Popover schliessen. `SemanticRole = NULL` (Custom rein dekorativ, CGR Sign-off).
  - `CancelCustomPopoverCommand` — Popover schliessen + State clearen.
- `ProfileWizardViewModel` Constructor nimmt jetzt zusaetzlich `ISegmentTypeRepository?` + `IIdGenerator?` (beide optional fuer Tests/isolierte ViewModels).
- XAML-Overlay in Wizard Schritt 2 (`CustomPopoverOverlay`) — semi-transparenter Hintergrund + zentrierte Border mit Name-Input, Live-Token-Vorschau, 12er-Farbpalette, Anlegen/Abbrechen-Buttons.
- `HexToColorConverter` + `EmptyToVisInverseConverter` (ProfileWizardDialog.xaml.cs) fuer Palette-Rendering und konditionale Fehleranzeige.
- 18 neue Unit-Tests: `TokenKeyGeneratorTests` (9), `ProfileWizardCustomPopoverTests` (9 — inkl. Token-Konflikt-Suffix, Direkt-Zuweisung, Catalog-Invalidate-Roundtrip, Isolated-VM-Fehlerpfad).

### Geaendert
- `OnChipPreviewMouseDown`: erkennt `IsCustomCreate==true` und ruft `_vm.OpenCustomPopover()` statt Drag-Start.
- `ProjectDetailView` / `PlanManagerView` / `MainWindow` / `App.xaml.cs`: `ISegmentTypeRepository` wird vom DI-Container bis zum Wizard durchgereicht.
- `SegmentTypeSeedServiceTests` / `SegmentTypeCatalogTests`: Asserts auf 4 Gruppen → 5 Gruppen aktualisiert (durch `grp_eigene`).

### Hintergrund
Zweiter Teil-Schritt der Phase C: Custom-Segmenttypen koennen jetzt direkt im Wizard ohne Manager-Dialog-Wechsel angelegt werden. Token_key live als Vorschau sichtbar — User versteht von Anfang an, dass Rename den Token nicht aendert. Manager-Dialog (Phase C Teil 3) und DevTool-Reset-UI (Teil 4) folgen.

---

## [v0.28.46] — 2026-05-18

### Refactor: BPM-108 Phase C Teil 1 — Wizard auf Segmenttyp-Katalog umgestellt

### Hinzugefuegt
- `WizardCatalogContext` (PlanManager.Services, statisch) — Catalog-Halter fuer XAML-Converter. Initialisiert in `App.xaml.cs` nach DI-Build.

### Geaendert
- `FileNameSegment`: `FieldType?`-Enum entfernt; ersetzt durch `string? FieldTypeId` (stabile `segment_types.id`-Referenz). `CustomFieldName` weg. `DisplayName` liefert nur noch die rohe ID — UI-Namen werden ueber den Catalog aufgeloest. INotifyPropertyChanged-Setter aktualisiert.
- `ProfileWizardViewModel`: nimmt optionalen `ISegmentTypeCatalog` im Konstruktor. `FieldTypeOptions` werden aus `GetEffectiveActive()` aufgebaut (statt hardcoded Enum-Liste). `RebuildFieldTypeOptions()` reagiert auf Catalog-`Changed`-Event. `ValidateStep2` prueft genau ein Segment mit `SemanticRole.PlanNumber`. `BuildHierarchyLevels` liest alle Profil-Segmente mit `SemanticRole.Spatial`. `IsLikelyVariableSegment` arbeitet ueber Catalog-Lookup statt FieldType-Enum. `HasPlanIndexSegment`/`HasPlanNumberSegment` ueber Catalog-SnapshotIncludingDeleted.
- `FieldTypeOption`: `Value` (FieldType?) → `FieldTypeId` (string?) + `IsCustomCreate` Flag. „+ Eigenes"-Chip bleibt sichtbar, Inline-Popover folgt in Commit 4.
- `HierarchyLevelOption`: `FieldType` → `string FieldTypeId` + Label kommt aus dem Catalog.
- Wizard-XAML: alle `Binding FieldType,...` → `Binding FieldTypeId,...`. Custom-Chip-DataTrigger ueber `IsCustomCreate`. Reset-Option versteckt via `MultiDataTrigger`.
- Wizard-Converter (`FieldTypeToBrushConverter`, `FieldTypeIsUnsetConverter`, `FieldTypeToLabelConverter`, `FieldTypeToOpacityConverter`): konsumieren jetzt `string fieldTypeId` und loesen ueber `WizardCatalogContext.Catalog` Farbe/Name/SemanticRole auf. Hex-Farbe wird zur Laufzeit in `SolidColorBrush` konvertiert. Fallback: `BpmBgElevated`/`DimGray` bei unbekannter ID.
- `ProfileManager.BuildFromWizard`: arbeitet direkt mit `FileNameSegment.FieldTypeId`. `IncludeInIdentity`/`Required` werden ueber Catalog-`SemanticRole`-Lookup (PlanNumber/Spatial) gesetzt. `LegacyFieldTypeMapper`-Bruecke entfernt.
- `ProfileWizardDialog`/`ProjectDetailView`/`PlanManagerView`/`MainWindow`/`App.xaml.cs`: ISegmentTypeCatalog wird per Constructor-Injection bis zum Wizard durchgereicht.
- `FileParseService`: bereits in Phase B auf `FieldTypeId` umgestellt — keine erneute Aenderung.

### Entfernt
- `LegacyFieldTypeMapper.cs` (PlanManager.Services) — Phase-B-Compat-Shim. Wird in Phase C nicht mehr gebraucht.
- `LegacyFieldTypeMapperTests.cs` (Tests) — 43 Theory-Tests.
- `xmlns:planmgr` aus `ProfileWizardDialog.xaml` (FieldType-Enum-Referenz weg).

### Tests
- `ProfileWizardVariableSegmentTests`: Umgestellt auf `FieldTypeId` + In-Memory-`FakeCatalog`. Alle 22 Tests gruen.
- 201/201 Tests gruen (vorher 244 — Differenz = entfernte LegacyFieldTypeMapper-Tests).

### Offen fuer Phase C Folge-Commits
- **Commit 4:** Inline-Popover „+ Eigenes" mit Live-Token-Vorschau (XAML + ViewModel-Verdrahtung mit `ISegmentTypeRepository.SaveType`).
- **Commit 5:** Manager-Dialog (Segmenttypen verwalten, Built-in-Rolle read-only mit Warntext).
- **Commit 6:** DevTool-UI fuer `IProfileArchiveService` (Reset-Knopf im DevToolsDialog).
- **Commit 7:** Auto-Import-Blockade im ImportWorkflowService bei `ProfileHealth.MissingSegmentTypes` (Akzeptanzkriterium #17).

### Hintergrund
Erster und groesster Schritt von BPM-108 Phase C: der Wizard nutzt jetzt durchgaengig den Segmenttyp-Katalog. Custom-Chip-Klick und Manager-Dialog sind bewusst noch nicht funktional — werden in Folge-Commits ergaenzt, damit die Phase-C-Refactor-Welle nicht zu monolithisch wird.

---

## [v0.28.45] — 2026-05-18

### Feature: BPM-108 Phase B — Profilformat v4 + ProfileHealth + DevTool-Archivierung

### Hinzugefuegt
- `RecognitionProfile.SchemaVersion = 4` mit `ProfileSegment.FieldTypeId` (statt FieldType-Enum-String) und entfaelltem `Label`-Feld. `IdentityFields`, `FolderHierarchy` und `RenameSchema` referenzieren snake_case-IDs bzw. `token_key`-Tokens. `IndexExtractionConfig.SegmentSelector` normiert auf `fieldTypeId`.
- `ProfileHealth` Enum (Domain) — Zustaende `Valid`, `MissingSegmentTypes`, `OutdatedSchema`, `InvalidRecognitionRules`. `RecognitionProfile.Health` + `MissingSegmentTypeIds` werden beim Laden berechnet (nicht persistiert via `[JsonIgnore]`).
- `IProfileArchiveService` + `ProfileArchiveService` (PlanManager.Services) — DevTool-Befehl, verschiebt veraltete RecognitionProfile-JSONs nach `<project>/.bpm/profiles/_archiv/schema-reset-YYYYMMDD-HHMMSS/` und `pattern-templates.json` analog. Kein Loader-Side-Effect.
- `LegacyFieldTypeMapper` (PlanManager.Services, internal) — Compat-Shim fuer Phase B: uebersetzt Wizard-Enum-Werte auf snake_case-IDs. Wird in Phase C entfernt.
- `PatternTemplate.SchemaVersion = 4` + `PatternTemplateService` Loader strikt auf v4. Aeltere Templates werden im Log dokumentiert verworfen.
- ProfileManager-Constructor nimmt optional `ISegmentTypeCatalog` (Default null fuer Tests/isolierte Anwendungen).
- 59 neue Unit-Tests (ProfileHealthTests, ProfileArchiveServiceTests, LegacyFieldTypeMapperTests).

### Geaendert
- `Directory.Build.props`: v0.28.44 → v0.28.45.
- `ProfileManager.Load(All|ById)`: strict `schemaVersion == 4`; alte v1→v2-Migration entfernt; `MigrateIfNeeded` weg.
- `ProfileManager.Save`: setzt `SchemaVersion = 4` zwingend.
- `ProfileManager.BuildFromWizard`: liefert v4-Profile mit `FieldTypeId` (via LegacyFieldTypeMapper) und Spatial-erweiterten `identityFields`-Defaults.
- `FileParseService`: `extractedFields[fieldKey]` nutzt nun `ProfileSegment.FieldTypeId` statt `FieldType` (snake_case statt enum.ToLowerInvariant).
- `App.xaml.cs`: DI-Registrierung `IProfileArchiveService` ergaenzt; `IProfileManager` bekommt `ISegmentTypeCatalog` via Factory-Delegate fuer Health-Berechnung.
- Tests `ProfileManagerSaveLoadTests` / `ProfileManagerLoadToleranceTests`: alle SchemaVersion-Asserts und JSON-Fixtures auf v4 angehoben.

### Hintergrund
Zweite Implementierungs-Phase von BPM-108 nach CGR-Sign-off (Akzeptanzkriterien #7–13 erfuellt). Recognition bleibt BPM-082-kompatibel — der `DocumentTypeRecognizer` wird in Phase B NICHT angefasst. Phase C (Wizard/UI/Manager) folgt im naechsten Commit und entfernt die `LegacyFieldTypeMapper`-Bruecke.

### Migration / Reset
- Frühphase = Reset (ADR-056). Alte v3-Profile werden vom Loader verworfen.
- DevTool-Aufruf: `IProfileArchiveService.ArchiveOutdatedProfiles(projectRoot)` und `.ArchiveOutdatedPatternTemplates(cloudSharedAppData)` — manuelle Trigger, kein App-Start-Side-Effect.

---

## [v0.28.44] — 2026-05-18

### Feature: BPM-108 Phase A — Segmenttyp-Katalog (Domain + Persistence + Seed)

### Hinzugefuegt
- `SegmentSemanticRole` Enum (Domain) — fachliche Sonderfaelle (PlanNumber, PlanIndex, ProjectNumber, Date, Description, Spatial, Ignore, None).
- `SegmentTypeDefinition` + `SegmentTypeGroupDefinition` (Domain) — Zwei-Schichten-Modell mit `fieldTypeId` (immutable), `token_key` (immutable, snake_case fuer Templates), `semantic_role` (bei Built-ins read-only), `user_modified_*`-Flags fuer Built-in Update-Policy.
- `ISegmentTypeRepository` + `SegmentTypeRepository` (Infrastructure) — CRUD auf `segment_types`/`segment_type_groups` in bpm.db. Soft-Delete only. Test-Konstruktor + `CreateTables`-Helper fuer isolierte Tests.
- `ISegmentTypeCatalog` + `SegmentTypeCatalog` (Infrastructure) — In-Memory-Snapshot mit Lazy-Load + `Changed`-Event. Effektiv-aktiv-Liste sortiert nach Gruppen-/Type-SortOrder.
- `SegmentTypeSeedService` (Infrastructure) — 4 Built-in-Gruppen + 16 Built-in-Typen. Update-Policy: nicht user-modifizierte Felder werden bei jedem App-Start aus dem Seed nachgezogen. `semantic_role` und `token_key` bei Built-ins immer korrigierbar (immutable Invariante).
- `bpm.db` Schema v2.2: neue Tabellen `segment_type_groups` + `segment_types` mit FK + UNIQUE(token_key) WHERE NOT deleted.
- ADR-056 `Segmenttyp-Architektur (BPM-108) — fieldTypeId + SemanticRole Zwei-Schichten-Modell` — formaler Entscheidungsanker basierend auf CGR-2026-05-12-segmenttyp-architektur (3 Runden Sign-off).
- DB-SCHEMA.md Kap. 4.9 + 4.10 mit kompletten Tabellen-Definitionen + Built-in-Listen.
- 27 Unit-Tests (SegmentTypeRepositoryTests, SegmentTypeSeedServiceTests, SegmentTypeCatalogTests) — alle gruen.

### Hintergrund
Resultiert aus dem Cross-Review mit ChatGPT (CGR-2026-05-12-segmenttyp-architektur r1–r3, Sign-off 2026-05-18). Erste Implementierungs-Phase von BPM-108. Phase B (Profilformat v4) und Phase C (Wizard/UI/Manager) folgen in separaten Commits.

### Geaendert
- `Directory.Build.props`: v0.28.43 → v0.28.44.
- `ProjectDatabase.cs`: neue Tabellen + Indizes in `EnsureTables()`, Schema-Version 2.1 → 2.2, neue `EnsureInitialized()`-Methode fuer Sub-Repositories.
- `App.xaml.cs`: DI-Registrierung fuer `ISegmentTypeRepository`, `SegmentTypeSeedService`, `ISegmentTypeCatalog`. Built-in-Seed laeuft beim App-Start nach DI-Build.

---

## [v0.27.10] — 2026-04-30

### Docs: ADR-053 Konsistenz-Pflege Phase B+C (Konzepte stillgelegt + ADR-Status)

### Geaendert
- Docs/Konzepte/MultiUserKonzept.md: Frontmatter status auf "superseded", grosser Banner oben mit Verweis auf ADR-053. 3-Phasen-Modell + Modus-B-Write-Lock werden als historisch markiert.
- Docs/Konzepte/DatenarchitekturSync.md: Frontmatter status auf "superseded", grosser Banner. Outbox/Inbox/Snapshots/12-Spalten/12-Schritte-Code-Reihenfolge alle als historisch markiert. 4-Klassen-Datenmodell + Local-First-Prinzip explizit als bleibend genannt.
- Docs/Konzepte/ServerArchitektur.md: Frontmatter authority auf "partially-superseded". Banner: Hosting (Linux-VPS+Docker) + Sync-Library (Microsoft.Datasync) + Multi-Tenant durch ADR-053 ersetzt. Auth/RBAC/PostgreSQL/Nachkalkulation/Audit-Trail bleiben gueltig.
- Docs/Referenz/ADR.md: Status-Updates fuer 4 ADRs in Detailsektionen UND Uebersichtstabelle:
  - ADR-033 "Multi-User 3 Modi": Superseded by ADR-053 (Modus B nie implementiert)
  - ADR-037 "ISyncTransport (Folder/HTTP)": Superseded by ADR-053 (FolderSyncTransport raus, IBpmSyncClient ersetzt HttpSyncTransport)
  - ADR-038 "IAccessControlService": Partially superseded (Phase-3-RBAC bleibt, Phase-2 raus)
  - ADR-047 "Datenarchitektur + Sync": Partially superseded (Punkte 4/5/6/9/11 obsolet, 1/2/3/7/10 bleiben)

### Hintergrund
Phase B+C der Doc-Pflege nach ADR-053-Architektur-Pivot. Verhindert dass Claude bei Server/Sync-Aufgaben obsolete Konzepte aus den Detail-Docs laedt — selbst wenn er sie direkt referenziert findet. Phase D (Tracker-Tasks aufraeumen) folgt separat via tracker-Skill.

---

## [v0.27.9] — 2026-04-30

### Docs: ADR-053 Konsistenz-Pflege Phase A (Authority Docs)

### Geaendert
- INDEX.md Z. 163-168: Routing fuer "Multi-User/Sync/Server-Architektur/Auth/API" auf ADR-053 als Primary umgestellt. MultiUserKonzept.md + DatenarchitekturSync.md als historisch/superseded markiert.
- Docs/Kern/BACKLOG.md "Sync-Infrastruktur"-Block (Z. 149-164): obsolete 12-Tabellen-Liste durch ADR-053-Spike-Reihenfolge ersetzt (Spike 0-5, BPM-088 bis 092). Verworfene Ansaetze explizit aufgelistet (change_log, Outbox/Inbox, FolderSyncTransport, etc.). "Won't have"-Sektion: ServerArchitektur/Datasync/Auth/RBAC-Eintraege auf ADR-053 umgestellt (Microsoft.Datasync raus, IBpmSyncClient rein).
- Docs/Kern/DB-SCHEMA.md "Neue Datenarchitektur" (Z. 966-995): obsolete Tabellen (change_log, sync_outbox, users, user_devices, diary_aggregate-Split) gestrichen. 12-Sync-Spalten-Aussage auf 7 Spalten korrigiert. Geplante Server-Sync-Erweiterungen ergaenzt (server_change_log, sync_state_local, sync_checkpoints, sync_history, recognition_profiles, ASP.NET Identity-Tabellen).
- Docs/Kern/CODING_STANDARDS.md Kap. 19.8 neu: "Server-Sync-Konvention (ADR-053)" mit Pull/Push-Vorgaben, IBpmSyncClient-Pattern, IDeviceContext, Auth-Konvention, Soft-Delete-Pflicht, verworfene Patterns.

### Hintergrund
Phase A der Doc-Pflege nach Architektur-Pivot (CGR-2026-04-30-datenarchitektur-sync, 7 Runden). Verhindert dass Claude bei kuenftigen Code-Aufgaben obsolete Konzepte (Outbox/Inbox, FolderSync, Multi-Tenant-RLS, Linux-Stack) aus Authority-Docs laedt. Phase B (Konzepte stillegen) + Phase C (ADR-Status-Updates) folgen separat.

---

## [v0.27.8] — 2026-04-30

### Docs: ADR-053 Server-Sync-Architektur + CGR-2026-04-30-datenarchitektur-sync abgeschlossen

### Hinzugefuegt
- ADR-053: Server-Sync-Architektur (Windows-only Stack, Phase 0/1 VPS, Phase Verkauf On-Premise) — 28 verbindliche Punkte
- CGR-2026-04-30-datenarchitektur-sync — komplette 7-Runden Cross-Review-Serie mit ChatGPT GPT-5.4 archiviert (28 Dateien: README + 4 Dateien pro Runde r1-r7)
- 5 neue Backlog-Tasks im ClickUp-Tracker: BPM-088 (ASP.NET Worker Service), BPM-089 (ASP.NET Identity + JWT), BPM-090 (Sync-Endpoints Pull/Push), BPM-091 (Windows-VPS Setup), BPM-092 (recognition_profiles in DB)

### Geaendert
- Docs/Referenz/ADR.md: Uebersichtstabelle um ADR-050/051/052/053 ergaenzt (waren vorher nicht aktuell)
- Docs/Referenz/chatgpt-reviews/INDEX.md: CGR-2026-04-30 auf Status "Abgeschlossen" mit Kernergebnis
- Themen-Enum erweitert um "datenarchitektur-sync"

### Architektur-Resultat
- Windows-only Stack: PostgreSQL 17 + ASP.NET Core 10 Worker Service + Caddy for Windows
- Phase 0/1 (5-6 User eigene Firma, 2 Jahre): Windows-VPS in EU (~12 EUR/Monat Strato VC 2-8)
- Phase Verkauf (24+ Monate): On-Premise bei Bauunternehmen, gleiche Architektur, Inno Setup + Lizenz-System
- IBpmSyncClient + Pull/Push + ASP.NET Identity + JWT
- Verworfene Optionen: Eigenbau OneDrive-Sync, CouchDB, Linux-VPS, Synology, Hauptrechner-24/7, Tailscale Premium, Cloudflare Tunnel, Multi-Tenant

---

## [v0.27.7] — 2026-04-30

### PlanManager: IProfileManager Interface + DI-Registrierung (BPM-009 F1+F2+F3)

### Hinzugefuegt
- Domain/Interfaces/IProfileManager.cs — Service-Vertrag mit 5 Public-Methoden (LoadAll, LoadById, Save, Delete, BuildFromWizard)
- App.xaml.cs DI-Registrierung: AddSingleton<IProfileManager, ProfileManager>() — eine Instanz pro App-Lebenszeit

### Geaendert
- ProfileManager implementiert jetzt IProfileManager
- MainWindow Konstruktor erweitert um IProfileManager profileManager Parameter (aus DI)
- PlanManagerView nutzt IProfileManager via Constructor Injection statt new ProfileManager() — letzter new()-Aufruf eines Services in einer View entfernt
- ProjectDetailView, ProfileWizardDialog, ProfileWizardViewModel, ImportWorkflowService: Field- und Parameter-Typen auf IProfileManager geaendert

### Audit-Erkenntnis (BPM-009 Tief-Audit)
- Funktional war ProfileManager seit v0.25.8 vollstaendig (CRUD, atomares Schreiben, ULID, v1-v2 Migration). Die strukturellen Luecken (kein Interface, kein DI) sind mit v0.27.7 geschlossen.
- Verbleibende post-v1 Folge-Tasks: Schema-v3-Migration, ProfileValidator, BuildFromWizard-Extraktion in ProfileBuilder.

---

## [v0.27.6] — 2026-04-29

### Build: .claude/ in gitignore (Worktree-Files + lokale Settings)

### Geaendert
- .gitignore: .claude/ aufgenommen (lokale Claude Code Settings + Worktrees-Artefakte)

---

## [v0.27.5] — 2026-04-29

### PlanManager: DB-Anbindung Orchestrator (BPM-001) + SQL-Ambiguity-Fix

### Geaendert
- ImportWorkflowService nutzt jetzt PlanManagerDatabase.GetAllCurrentRevisions() statt leerem Dictionary-Stub. 9-Status-Decision-Matrix funktioniert vollstaendig (NEW, SkipIdentical, UpdateNewerIndex, ChangedSameIndex, ChangedNoIndex, OlderRevision, Conflict, LearnIndex, Unknown).
- ImportWorkflowService Constructor erweitert um PlanManagerDatabase-Dependency.
- ProjectDetailView.OnStartImport: PlanManagerDatabase wird jetzt VOR der Analyse erstellt (using-Pattern), Workflow + Executor teilen eine Instanz statt doppelter Connections.

### Behoben
- PlanManagerDatabase.GetCurrentRevision: SQL-Bug "ambiguous column 'id'" behoben (Spalten qualifiziert auf pr.id und pf.md5_hash). Bug existierte seit v0.25.13 latent — wurde nach BPM-001 sichtbar, weil Erst-Imports vor BPM-001 nie ueber Stub hinaus kamen. Blockierte alle DB-Inserts beim Execute.

### Audit-Erkenntnis
- Code-Audit gegen ClickUp-Tracker: BPM-009 (ProfileManager), BPM-011 (Import-Workflow 1-5), BPM-012 (Import-Vorschau GUI), BPM-013 (Import-Execute), BPM-014 (Index-Archivierung) sind faktisch erledigt seit v0.25.8-15, ClickUp-Status war veraltet.

---

## [v0.25.23] — 2026-04-16

### DB Schema v2.1 — Sync-Spalten + IUserContext (ADR-050, ADR-052)

### Hinzugefuegt
- Domain/Interfaces/IUserContext.cs — Benutzerkontext-Interface (UserId, DisplayName, Source)
- Domain/Enums/UserContextSource.cs — Local/Server Enum
- Infrastructure/Services/LocalUserContext.cs — liest aus AppSettings (Modus A)
- AppSettings: LocalUserId + LocalUserName Properties
- DB Schema v2.1: Sync-Spalten (created_by, last_modified_at, last_modified_by, sync_version, is_deleted) auf allen 6 Entitaetstabellen
- Alle Timestamps jetzt UTC (DateTime.UtcNow, ISO 8601)
- sync_version inkrementiert bei jedem Update

### Geaendert
- ProjectDatabase: updated_at → last_modified_at in allen CREATE TABLE + INSERT/UPDATE Statements
- ProjectDatabase: SaveProject/SaveClient/SaveParts/SaveLevels/SaveParticipants/SaveLinks mit UTC + User-Parameter

## [v0.25.21] — 2026-04-16

### Docs: ModuleDashboard + ADR-052

### Hinzugefuegt
- ModuleDashboard.md v2.0 — Widget-Host, Layout-Persistenz, Baulotse-Modus
- ADR-052: IUserContext + Auth-Strategie (Local vs Server)

## [v0.25.18] — 2026-04-16

### PlanManager: Karten-Layout + Sidebar-Badge + Active-Filter

### Hinzugefuegt
- PlanManagerView: DataGrid ersetzt durch ListBox Karten-Layout (ChatGPT 2-Runden Cross-Review)
- Sidebar-Badge: Gesamtzahl unsortierter Dateien neben PlanManager in Sidebar
- Suchfeld in PlanManagerView (gleicher Stil wie Einstellungen)
- InverseBoolToVisConverter, EmptyToVisConverter
- FilteredProjects mit Echtzeit-Suche (Delay=300ms)
- Mockups: 01_Projektuebersicht.html, 02_Projektdetail_Profile.html

### Geaendert
- PlanManagerView: Aktualisieren-Button jetzt blau (BpmButtonPrimary)
- PlanManagerViewModel: Nur aktive Projekte laden (ProjectStatus.Active Filter)
- MainWindow: UpdateSidebarBadge() bei Start und Navigation
- Mockup-Konvention: NN_Blatt[_Untermenue].html

## [v0.25.17] — 2026-04-15

### Sidebar Umbenennung

### Geaendert
- Sidebar: "Plaene" umbenannt in "PlanManager"

---

## [v0.25.16] — 2026-04-15

### Server-Architektur Konzept (3-Runden Cross-Review Claude/ChatGPT)

### Hinzugefuegt
- Docs/Konzepte/ServerArchitektur.md — Zielarchitektur Modus C (7 Kapitel, Frontmatter, Quickload)
- ADR-050: Source of Truth je Betriebsmodus (Modus A: SQLite, Modus C: PostgreSQL)
- ADR-051: Client ist local-first — Server nur Auth + Sync + Autoritaet
- CODING_STANDARDS Kapitel 19: Sync-Felder, UTC, Soft Delete, localUserName, Writes ueber Services
- DB-SCHEMA Kapitel 9.3: Sync-Felder-Konvention (ULID + 6 Spalten)
- BACKLOG: Server/Nachkalkulation/Auth/Datasync-Spike Items
- VISION: Server-Modus + Nachkalkulation in Roadmap
- DEPENDENCY-MAP: BauProjektManager.Contracts + .Server (geplant)
- INDEX.md: Routing fuer ServerArchitektur, Surface7 PC-Registrierung

### Geaendert
- BauProjektManager_Architektur.md: Invariante "SQLite ist SoR" auf Modus A eingeschraenkt (ADR-050)
- DSVGO-Architektur.md: Server-Hinweis in Invarianten (JWT, HTTPS, Login-Audit)
- MultiUserKonzept.md: ADR-050/051 + Verweis ServerArchitektur.md
- DatenarchitekturSync.md: ADR-050/051 + Verweis ServerArchitektur.md

## [v0.25.15] — 2026-04-15

### PlanManager V1 — Import-Pipeline komplett

Komplette Import-Pipeline von Scan bis Ausführung implementiert (Cross-Review 15.04.2026).

### Hinzugefuegt
- 6 Domain-Enums: ParseConfidence, ImportStage, RevisionKind, ImportStatus, ImportWarningCode, ResolutionSource
- 8 Domain-Records: ScannedFile, FingerprintedFile, ParsedImportFile, ClassifiedImportFile, ImportDecision, ImportWarning, ResolutionEvidence, DocumentTypeDescriptor
- RecognitionProfile Schema v2: documentTypeId, TokenizationConfig, IndexExtractionConfig, includeInIdentity, GroupingConfig.Mode="identity"
- v1→v2 Profil-Migration automatisch beim Laden (ProfileManager)
- FileNameParser: TokenizationConfig, CollapseRepeatedDelimiters, FirstTokenDelimiter
- 7-Stufen-Analyse-Pipeline: ImportScanService, FileFingerprintService, FileParseService, ImportContextResolver, DocumentKeyBuilder, RevisionDecisionService, ImportPlanBuilder
- ImportWorkflowService: Orchestrator fuer AnalyzeAsync()
- PlanManagerDatabase: planmanager.db mit 6 Tabellen (plan_revisions, plan_files, revision_file_links, import_journal, import_actions, import_action_files)
- ImportExecutionService: Dateien verschieben, _Archiv/ erstellen, Journal, DB-Update
- Import-Vorschau Dialog (DataGrid, 7 Spalten, 9 Status-Typen)
- "Import starten" Button verdrahtet in ProjectDetailView
- AddFileToExistingRevision: DWG+PDF unter gleicher Revision (UNIQUE-Fix)
- PatternTemplateService: Globale Musterbibliothek (Schema v2)
- Tools/Move-FilesFlat.ps1: PowerShell-Hilfstool zum Dateien-Zurücklegen

### Geaendert
- PlanManager.md: Gruppierung nach fachlicher Identity statt Dateinamen-Stamm
- PlanManager.md: Phase 2 als 7-Stufen-Analyse-Pipeline dokumentiert
- PlanManager.md: Profil-Schema v2, tokenization, indexExtraction, Stage-Konzept
- Architektur.md: Import-Eintrag um 7-Stufen-Pipeline-Verweis ergänzt
- Directory.Build.props: Version 0.25.15

## [v0.25.1] — 2026-04-15

### ULID-Migration — Schema v2.0

Komplette Migration von seq+Präfix-IDs auf ULID als TEXT PRIMARY KEY für alle Tabellen (ADR-039 v2).

### Hinzugefuegt
- `IIdGenerator` Interface in Domain/Interfaces (ADR-039 v2)
- `UlidIdGenerator` Implementierung in Infrastructure/Services
- NuGet-Paket `Ulid 1.0.0` in Infrastructure
- `created_at`/`updated_at` auf alle Tabellen (clients, building_parts, building_levels, project_participants, project_links)
- FK-Indizes: `idx_building_parts_project_id`, `idx_building_levels_part_id`, `idx_participants_project_id`, `idx_links_project_id`
- `PRAGMA foreign_keys=ON` aktiviert
- `use_global_zero_level` + `global_zero_level` direkt im Schema (keine Migration nötig)

### Geaendert
- `ProjectDatabase` Constructor nimmt `IIdGenerator` entgegen (kein parameterloser Constructor mehr)
- Alle Aufrufer angepasst: `App.xaml.cs`, `SettingsViewModel`, `PlanManagerViewModel`
- Schema-Version: `1.5` → `2.0`
- ID-Generierung: `GenerateNextId("prefix", "table")` → `_idGenerator.NewId()`
- `SaveClient` schreibt jetzt auch `updated_at`

### Entfernt
- `seq INTEGER PRIMARY KEY AUTOINCREMENT` aus allen Tabellen
- `GenerateNextId()` Methode (Präfix-IDs: `proj_001`, `client_001` etc.)
- `ColumnExists()` Hilfsmethode (keine inkrementelle Migration mehr nötig)
- Inkrementelle `MigrateSchema()` Logik (Schema v2.0 ist Neustart)

---

## [v0.24.14] — 2026-04-11 / 2026-04-13

### Docs-Audit: 20 Widersprüche gefunden und gefixt

Systematischer Konsistenz-Audit über alle Kern- und Referenz-Docs. 19 Commits, alle reine Docs-Fixes (kein Code, kein Version-Bump).

### Hinzugefuegt

- **ADR-048:** Ansichtsprofile (ViewProfiles) als UI-Sichtschicht über Modul-Aktivierung, Resolver-basiert
- **ADR-049:** Pfad-Resolution Option C — relativer folder_name + Manifest-Fallback bei Umbenennung

### Geaendert

- **Architektur** v2.2.0→v3.0.0 — Frontmatter/Quickload, PlanManager-Kapitel ausgelagert nach PlanManager.md, Kapitel neu nummeriert, Cloud-neutral (OneDrive→generisch), rootPath→folderName (ADR-049), ADR-Zähler auf 49
- **DB-SCHEMA** — Frontmatter/Quickload, Schema-Status korrigiert (v1.5 implementiert, v2.0 ULID ausstehend), Kap. 6 um 3 Plan-Cache-Tabellen erweitert, diary_entries aufgeteilt in diary_days + diary_notes (ADR-047)
- **CODING_STANDARDS** — Kap. 1.3 Namespaces an echte 5-Projekte-Solution, Kap. 7 komplett auf CommunityToolkit.Mvvm umgeschrieben (ADR-015)
- **DSVGO-Architektur** — external_call_log auf ULID TEXT PRIMARY KEY, broken relative Links gefixt
- **DEPENDENCY-MAP** — .bpm/ statt .bpm-manifest, profiles.json Pfad korrigiert, ADR-Zähler 49
- **PlanManager.md** — Wizard 4-Schritt auf 5-Schritt korrigiert
- **UI_UX_Guidelines + WPF_UI_Architecture** — Sidebar-Breite überall auf 56px Icon-Leiste
- **VISION** — Modul-Reihenfolge an Architektur-Priorität angepasst (Foto→Zeit→Tagebuch→Dashboard)
- **ADR.md** — ADR-013 Status auf Superseded by ADR-046, ADR-048 + ADR-049 ergänzt, Zähler auf 49
- **BACKLOG** — Archivierung als Aktion klargestellt, settings.json Speicherort korrigiert
- Alle Kern/Referenz/Module-Docs: Frontmatter + Quickload nach DOC-STANDARD.md ergänzt

---

## [v0.24.13] — 2026-04-11

### Hinzugefuegt

- **ADR-048:** Ansichtsprofile als Architekturkonzept in Architektur.md Kap. 1.4 verankert

---

## [v0.24.12] — 2026-04-10

### Hinzugefuegt

- **DatenarchitekturSync.md** (520 Zeilen): Datenklassifizierung (4 Klassen), Sync-Konzept (Outbox/Inbox + Snapshots), User/Rollen-Schema, Event-Format, Konflikt-Behandlung, settings.json Split — Ergebnis aus 4-Runden Cross-Review (Claude + ChatGPT)
- INDEX.md, DB-SCHEMA.md, MultiUserKonzept.md, BACKLOG.md aktualisiert (Verweise + 12 neue Sync-Features)
- **ADR-047:** Datenarchitektur + Sync — State-based lokal, change-based sync. Phase 2 bewusst temporär, Phase 3 PostgreSQL.

---

## [v0.24.11] — 2026-04-10

### Hinzugefuegt

- **ADR-046:** `.bpm/` Ordner — Manifest-Split (manifest.json schlank + project.json Vollexport) und Profilablage im Projektordner statt `.AppData/`. Supersedes ADR-013 v2.
- Architektur.md, BACKLOG.md, DEPENDENCY-MAP.md, PlanManager.md, CHANGELOG.md aktualisiert (8+ Stellen pro Datei)

---

## [v0.24.10] — 2026-04-10

### Hinzugefuegt
- **PlanManager:** 5-Schritt Profil-Wizard (Refactoring von 4 auf 5 Schritte)
  - Schritt 1: Datei auswaehlen + Parsen (Segment-Vorschau als WrapPanel)
  - Schritt 2: Segmente zuweisen (FieldType-Dropdowns, PlanNumber Pflicht)
  - Schritt 3: Index-Konfiguration (IndexSource: FileName/None/PlanHeader, indexMode, caseInsensitive)
  - Schritt 4: Zielordner + Ordner-Hierarchie (Dropdown, Custom, Checkboxen, Pfad-Vorschau)
  - Schritt 5: Erkennung via klickbare Segment-Bloecke (Toggle blau/grau, auto-Muster, auto-Methode prefix/contains, Live-Test)
- **Domain:** `IndexSourceType` Enum (FileName, None, PlanHeader) — ADR-045
- **PlanManager:** `RecognitionSegment` Klasse fuer klickbare Erkennungs-Bloecke
- **PlanManager:** Step-Navigation (GoNext/GoBack Commands, 5 Progress Dots, dynamischer Button-Text "Speichern")
- **PlanManager:** Converter: `CountToVisZeroConverter`, `InverseBoolConverter`

### Geaendert
- **Docs:** INDEX.md — PC-Tabelle mit Auto-Discovery via `hostname` + `[System.Environment]::GetEnvironmentVariable('OneDrive', 'User')`
- **Skill:** cc-steuerung SKILL.md v3 — Abschnitt 4 mit dynamischer Pfad-Ermittlung + Self-Registration

### Build
- **Version:** Directory.Build.props 0.24.6 → 0.24.10

---

## [v0.24.6] — 2026-04-10

### Hinzugefügt
- **PlanManager:** `ProfileWizardDialog.xaml` — 4-Schritt Profil-Wizard (Schritt 1 implementiert: Dateiname parsen, Segmente zuweisen mit FieldType-Dropdown)
- **PlanManager:** `ProfileWizardViewModel` — Wizard-State, FileNameParser-Integration, FieldTypeOption-Dropdown, Validierung (PlanNumber Pflicht)
- **PlanManager:** `CountToVisInverseConverter` — Count>0 → Visible (für Segment-Anzeige)
- **PlanManager:** Button „+ Neuer Dokumenttyp" im Profile-Tab öffnet den Wizard

### Build
- **Version:** Directory.Build.props 0.24.5 → 0.24.6

---

## [v0.24.5] — 2026-04-10

### Hinzugefügt
- **PlanManager:** `ProjectDetailView.xaml` — Projektdetail mit Toolbar (← Zurück, Projektname, Import starten disabled), Eingangs-Banner, 3 Tabs (Profile, Manuell sortieren, Sync)
- **PlanManager:** `ProjectDetailViewModel` — hält gewähltes Projekt, Eingangs-Info, GoBack-Event
- **PlanManager:** Navigation Projektliste ↔ Projektdetail via ContentControl-Wechsel in PlanManagerView

### Geändert
- **PlanManager:** `PlanManagerView.xaml` — umgebaut zu Host mit ProjectListPanel + DetailHost ContentControl
- **PlanManager:** `PlanManagerView.xaml.cs` — NavigateToDetail/NavigateToList Logik, ProjectSelected Event

### Build
- **Version:** Directory.Build.props 0.24.4 → 0.24.5

---

## [v0.24.4] — 2026-04-10

### Hinzugefügt
- **PlanManager:** `PlanManagerViewModel` — Projektliste laden, Eingangs-Zähler (`_Eingang/`-Ordner scannen), `PlanProjectItem` Wrapper
- **PlanManager:** `PlanManagerView.xaml` — DataGrid mit Projektliste, amber Eingangs-Badge (Pill-Form), Empty State, Aktualisieren-Button
- **PlanManager:** `BoolToVisConverter` + `CountToVisConverter` für Badge/Empty-State Sichtbarkeit
- **PlanManager:** CommunityToolkit.Mvvm + Serilog als NuGet-Referenzen in PlanManager.csproj

### Geändert
- **PlanManager:** `PlanManagerView.xaml` — alle `StaticResource` → `DynamicResource` (Modul-Projekte können App-Resources erst zur Laufzeit auflösen)

### Build
- **Version:** Directory.Build.props 0.24.3 → 0.24.4

---

## [v0.24.3] — 2026-04-10

### Hinzugefügt
- **Domain:** `FieldType` Enum — 16 vordefinierte Feldtypen (System + Bau) + Custom für benutzerdefinierte
- **Domain:** `FileNameSegment` Modell — Position, RawValue, FieldType, CustomFieldName, DisplayName
- **Domain:** `ParsedFileName` Modell — OriginalFileName, BaseName, Extension, Segmente, Trennzeichen
- **PlanManager:** `FileNameParser.Parse()` — statischer Service, splittet Dateinamen an konfigurierbaren Trennzeichen in Segmente (ADR-022)

### Architektur
- Domain-Modelle unter `Domain/Models/PlanManager/` (teilbar mit zukünftigen Modulen)
- Parser-Logik in `PlanManager/Services/` (modulspezifisch)

### Build
- **Version:** Directory.Build.props 0.24.2 → 0.24.3

---

## [v0.24.2] — 2026-04-09

### Hinzugefügt
- **Domain:** `UseGlobalZeroLevel` + `GlobalZeroLevel` Properties auf `Project` — optionales globales ± 0,00 Niveau für alle Bauteile
- **Settings:** Ovaler Toggle-Switch im Bauwerk-Tab für globales ± 0,00 Niveau (Custom Border+Ellipse, kein CheckBox)
- **Settings:** Bauteil+Geschoss Eingabe-Workflow — nach Bauteil-OK öffnet sich automatisch Geschoss-Dialog mit Schleife (+ Geschoss / Fertig / Weiteres Bauteil)
- **Settings:** `ShowLevelEditDialogWithContinue` — Geschoss-Dialog mit 2 Buttons statt 1
- **Settings:** `ShowDarkConfirm` — Dark-Theme Ja/Nein-Dialog für Code-behind Dialoge (statt MessageBox)
- **Settings:** `AddLevelsLoop` + `LevelDialogResult` Enum für Geschoss-Eingabeschleife
- **Settings:** FileSystemWatcher im ProjectEditDialog — Ordnerstruktur-Tab aktualisiert sich live bei Änderungen im Explorer
- **Settings:** GridSplitter im Bauwerk-Tab — Bauteile/Geschosse 50/50 Aufteilung, ziehbar

### Geändert
- **App:** `BpmButtonSecondary` — Border hinzugefügt (`BpmBorderDefault`, 1px) für sichtbare Umrandung
- **App:** `MainWindow.xaml.cs` — `HighlightNavButton()` für aktive Sidebar-Hervorhebung (Foreground + Background)
- **Settings:** `SettingsView.xaml` — Überschrift "Einstellungen" höher und links ausgerichtet (Margin angepasst)
- **Settings:** `ProjectEditDialog.xaml` — Geschoss-Liste ✎-Button kompakt neben "Geschosse"-Überschrift statt eigene Zeile
- **Settings:** `ProjectEditDialog.xaml` — Info-Legende aus Geschosse-Bereich in eigene fixe Row verschoben

### Behoben
- **Domain:** `DeckThickness` korrigiert — war `RDOK − RDUK` (gleiche Zeile), jetzt `RDOK(n+1) − RDUK(n)` (Decke darüber minus UK aktuell). Property von berechnet auf gesetzt umgestellt.
- **Settings:** Code-behind Dialoge (Bauteil/Geschoss) erben jetzt XAML-Resources vom Owner-Dialog (`foreach Resources.Keys`) — ComboBox Dark Theme funktioniert korrekt
- **Infrastructure:** Duplikat-Import verhindert — `ProjectExistsByPath()` Prüfung vor Import

### Build
- **Version:** Directory.Build.props 0.23.0 → 0.24.2

---

## [v0.23.4] — 2026-04-09

### Hinzugefügt
- **Settings:** GridSplitter im Bauwerk-Tab — Bauteile und Geschosse 50/50 mit ziehbarer Trennlinie

---

## [v0.23.3] — 2026-04-09

### Hinzugefügt
- **Settings:** FileSystemWatcher im ProjectEditDialog — Ordnerstruktur aktualisiert sich live bei Explorer-Änderungen

---

## [v0.23.2] — 2026-04-09

### Behoben
- **App:** Sidebar-Highlight — aktiver Nav-Button wird visuell hervorgehoben (BpmAccentPrimary + BpmBgActive)
- **Settings:** Einstellungen-Überschrift Position — höher und links ausgerichtet

---

## [v0.23.1] — 2026-04-08

### Behoben
- **Infrastructure:** Duplikat-Import verhindert — `ProjectExistsByPath()` prüft vor ImportFromManifest und ImportFromFolder

---

## [v0.23.0] — 2026-04-08

### Hinzugefügt
- **App:** `Icons.xaml` — zentrale Icon-Registry mit 18 String-Resources (Emoji als Brücke zu Segoe Fluent Icons)
- **App:** `Dialogs.xaml` — 3 neue Styles: `BpmContextMenu`, `BpmMenuItem`, `BpmMenuSeparator` (Dark Theme)

### Geändert
- **Alle Module:** 40 hardcoded Emoji-Referenzen in 10 Dateien durch `StaticResource`/`FindResource` ersetzt
- **Settings:** Kontextmenü auf BpmContextMenu/BpmMenuItem Styles umgestellt
- **Settings:** Pfad-Spalte 📂-Button: Clipping behoben (`Height="20"`, `MinWidth="28"`), Hover-Effekt (BpmAccentPrimary)
- **Build:** `Directory.Build.props` Version 0.19.2 → 0.23.0
- **App:** `App.xaml` — Icons.xaml in MergedDictionaries (8 statt 7 ResourceDictionaries)

---

## [v0.22.2] — 2026-04-08

### Behoben
- **Settings:** Kontextmenü Dark Theme — eigene Styles statt WPF-Defaults
- **Settings:** Pfad-Spalte 📂-Button Clipping — MinWidth + Height gesetzt
- **Build:** `Directory.Build.props` Version 0.19.2 → 0.22.1 nachgezogen

---

## [v0.22.0] — 2026-04-08

### Hinzugefügt
- **Settings:** Projektsuche — Suchfeld mit Platzhalter, durchsucht Name, FullName, Projektnummer, Auftraggeber, Ort, Tags (300ms Debounce)
- **Settings:** Statusfilter — Toggle-Buttons (Alle/Aktiv/Abgeschlossen) mit CollectionView
- **Settings:** Filterinfo-Anzeige ("3 von 4 Projekten")

---

## [v0.21.0] — 2026-04-08

### Hinzugefügt
- **Domain:** `IDialogService` Interface — abstrakte Benutzer-Dialoge (Info/Warnung/Fehler/Bestätigung)
- **App:** `BpmDialogService` Implementation mit Dark Theme Dialogen
- **App:** `BpmInfoDialog.xaml` — eigene Info/Warn/Error MessageBox im BPM-Design
- **App:** `BpmConfirmDialog.xaml` — eigener Ja/Nein-Dialog im BPM-Design
- **Settings:** Popup-Button "＋ Neues Projekt" mit 2 Optionen (Erstellen / Importieren)
- **Settings:** Hinweis-Dialog wenn Bearbeiten/Löschen ohne Projektauswahl

### Geändert
- **Settings:** Alle `MessageBox.Show()` durch `IDialogService` ersetzt
- **App:** `MainWindow.xaml.cs` erstellt `BpmDialogService` und übergibt an SettingsView

---

## [v0.20.0] — 2026-04-08

### Hinzugefügt
- **Domain:** `BpmManifest.cs` — portabler Projekt-Snapshot als .bpm-manifest (ADR-013 v2)
- **Infrastructure:** `BpmManifestService.cs` — Manifest lesen/schreiben/scannen, Hidden+ReadOnly Attribute, Atomic Write
- **Settings:** Projekt-Import — Ordner wählen, Auto-Erkennung (mit/ohne Manifest)
- **Settings:** Manifest wird automatisch bei Projekt-Erstellen und -Bearbeiten geschrieben

### Geändert
- **Docs:** ADR-013 v2 — Manifest erweitert von Ausweis auf vollständigen Projekt-Snapshot
- **Docs:** Architektur Kap. 3.6 — Manifest-Schema mit allen Projektdaten

---

## [v0.17.0] — 2026-04-04

### Changed
- **ID-Schema (ADR-039 v2):** ULID als Primärschlüssel für ALLE Tabellen (bpm.db + planmanager.db). Ersetzt seq + TEXT-Präfix-IDs. Entscheidung aus 4-Runden Claude+ChatGPT Review. **Hinweis:** Entscheidung dokumentiert — Code hat noch v1.5 Schema (seq+Präfix). ULID-Migration steht aus.
- **DB-SCHEMA.md v2.0:** Alle Tabellen auf `id TEXT PRIMARY KEY` (ULID), `seq` Spalte entfällt, `created_at`/`updated_at` ergänzt, Indizes auf FK-Spalten
- **IIdGenerator Interface** in Domain, UlidIdGenerator in Infrastructure (NuGet: Cysharp/Ulid)
- **Docs aktualisiert:** Architektur, DEPENDENCY-MAP, GLOSSAR, BACKLOG, CHANGELOG

### Removed
- Präfix-IDs (`proj_001`, `bpart_042` etc.) — ersetzt durch ULID
- `seq INTEGER PRIMARY KEY AUTOINCREMENT` Spalte aus allen Tabellen
- `EntityIdGenerator` Konzept — ersetzt durch `IIdGenerator` / `UlidIdGenerator`
- `GenerateNextId()` mit `MAX(seq)+1` — ersetzt durch `Ulid.NewUlid()`

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
| v3.0.0 | 2026-04-11 | Architektur | Quickload-Refactor, PlanManager ausgelagert, Cloud-neutral, ADR-049 |
| v1.3.0 | 2026-04-11 | ADR | 49 ADRs (ADR-043 bis ADR-049), ADR-013 Superseded |
| — | 2026-04-11 | DB-SCHEMA | Schema-Status, Plan-Cache-Tabellen, diary Split |
| — | 2026-04-11 | CODING_STANDARDS | Namespaces, CommunityToolkit.Mvvm |
| — | 2026-04-11 | Alle Kern/Referenz/Module | Frontmatter + Quickload nach DOC-STANDARD |
| — | 2026-04-13 | GLOSSAR | Deckenstärke-Formel, .bpm/ Ordner, profiles.json Pfad |
| — | 2026-04-13 | UX_Flows | Cloud-neutral, .bpm/ Ordner |
| — | 2026-04-13 | CHANGELOG | v0.24.13–v0.24.14 nachgetragen |

---

*Wird bei jedem Release aktualisiert.*
