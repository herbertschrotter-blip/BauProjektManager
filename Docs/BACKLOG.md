# BauProjektManager — Backlog & Ideen

**Letzte Aktualisierung:** 29.03.2026  
**Aktuelle Version:** v0.15.0  
**Format:** Priorität → Feature → Beschreibung → Status

**Verwandte Dokumente:**
- [VISION.md](VISION.md) — Nordstern, Schmerzpunkte, Zielgruppe
- [ADR.md](ADR.md) — Alle Architekturentscheidungen
- [CHANGELOG.md](CHANGELOG.md) — Versionshistorie ab v0.0.0
- [DEPENDENCY-MAP.md](DEPENDENCY-MAP.md) — Solution-Struktur + Ökosystem
- [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) — Technische Spezifikation
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — Code-Richtlinien
- Modul-Konzepte: [Docs/Konzepte/](Konzepte/) — Detaillierte Konzeptdokumente pro Modul

---

## V1 — Phase 1: Einstellungen

| # | Feature | Beschreibung | Status |
|---|---------|-------------|--------|
| 1 | App-Shell + Navigation | MainWindow, Sidebar, Statusleiste | ✅ Erledigt |
| 2 | Serilog Logging | File + Console, tägl. Rotation, 30 Tage | ✅ Erledigt |
| 3 | Domain-Modelle | Project, Client, Building, Location, Timeline, Paths | ✅ Erledigt |
| 4 | Projektliste + Dialog | DataGrid, Bearbeiten-Dialog mit allen Feldern | ✅ Erledigt |
| 5 | SQLite-Datenbank | bpm.db mit Projects, Clients, Buildings Tabellen | ✅ Erledigt |
| 6 | Auto-Increment IDs | proj_001, client_001, bldg_001 | ✅ Erledigt |
| 7 | registry.json Export | Flat JSON für VBA, atomisch geschrieben | ✅ Erledigt |
| 8 | Git-Aufräumung | Hilfsmodule + Export aus Tracking entfernt | ✅ Erledigt |
| 9 | Ersteinrichtung | OneDrive auto-erkennen, Arbeitsordner, Archivordner, settings.json | ✅ Erledigt (v0.10.0) |
| 10 | Projektordner erstellen | Nummerierte Ordnerstruktur mit Unterordnern, Präfix-Schalter, TreeView im Dialog + Einstellungen, _Eingang, Vorschau-Baum | ✅ Erledigt (v0.11.0–v0.12.4) |
| 11 | **.bpm-manifest** | Versteckte Datei in jedem Projektordner (Ausweis), bei Umbenennung → Auto-Suche + Pfad aktualisieren | ⬜ Nächster Schritt |
| 12 | **Projekt archivieren** | Status → Completed → Ordner in Archiv verschieben, Pfad aktualisieren | ⬜ (Button vorbereitet, disabled) |
| 13 | Pfade änderbar | Gelbe 📁-Buttons mit OpenFolderDialog für Arbeitsordner + Archivordner | ✅ Erledigt (v0.12.3) |
| 14 | **Single-Writer Mutex** | Nur eine App-Instanz gleichzeitig (siehe ADR-016) | ⬜ |
| 15 | **Suchfeld Projekte** | Schnellsuche/Filter in der Projektliste | ⬜ |
| 16 | **Versionsnummer im Log** | Automatisch aus Assembly, nicht hardcoded | ⬜ |
| 17 | **Architektur v1.5** | Hauptdokument aktualisieren (.NET 10, Ordnerstruktur, Tabs). Teilweise durch ADR.md + DEPENDENCY-MAP.md abgedeckt | ⬜ |

**Zusätzlich erledigt (v0.13.0–v0.15.0):**

- **Tab 1 Stammdaten** komplett neu: 5-Tab-Dialog (Stammdaten, Bauwerk, Beteiligte, Portale+Links, Ordnerstruktur), 2-Spalten-Layout, ProjectType als editierbare Liste, DatePicker, Status vereinfacht (nur Active/Completed) (v0.13.0)
- **Tab 2 Bauwerk** komplett: BuildingPart + BuildingLevel Modelle, Bauteile-DataGrid mit Edit-Dialog, Geschoss-DataGrid direkt editierbar (RDOK/FBOK/RDUK), Live-Berechnung (Geschosshöhe/Rohbauhöhe/Deckenstärke/FB-Aufbau), intelligenter Geschoss-Vorschlag, LevelNames 2-spaltig (Short+Long), BuildingTypes-Dropdown, ± 0,00 pro Bauteil (v0.13.1–v0.13.2)
- **Tab 3 Beteiligte** komplett: ProjectParticipant Modell, project_participants DB-Tabelle, CRUD mit Edit-Dialog, Rolle als editierbares Dropdown (ParticipantRoles), Import-Buttons vorbereitet (ausgegraut), contact_id FK für späteres Adressbuch (v0.14.0)
- **Tab 4 Portale + Links** komplett: ProjectLink Modell, project_links DB-Tabelle, 2-Spalten-Layout (Portale + Eigene Links), PortalTypes editierbar, "Im Browser öffnen", Dashboard-Vorschau (v0.15.0)
- **Tab 5 Ordnerstruktur** bereits vorhanden (aus v0.11.0–v0.12.4)
- DB-Schema auf v1.5 (project_participants + project_links Tabellen)
- Löschen-Button mit Bestätigungsdialog (v0.11.3)
- 2-Tab-Einstellungsseite: Projekte + Standard-Ordnerstruktur (v0.12.0)
- Status-Anzeige mit Farben: ● Aktiv (grün), ● Abgeschlossen (rot) (v0.12.0)
- Docs-Reorganisation: ADR, Vision, Dependency Map, Changelog erstellt (v0.12.5)
- Modul-Konzepte nach Docs/Konzepte/ verschoben (v0.12.6)

---

## V1 — Phase 1.5: Bestehende Projekte importieren

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Bestehende Ordner zuweisen | Projekt erstellen und existierenden Ordner zuweisen statt neue Ordner anzulegen. Bestehende Unterordner werden in TreeView übernommen. | ⬜ |
| Nachträgliche Sortierung | Bei zugewiesenen Ordnern nachträglich Präfix (Nummerierung) auf Hauptordner anwenden und Sortierung ändern — gleiche UI wie bei Neues Projekt | ⬜ |
| Ordner umbenennen auf Disk | Wenn Präfix/Sortierung geändert wird, bestehende Ordner auf der Festplatte entsprechend umbenennen | ⬜ |

---

## V1 — Phase 2: PlanManager (der große Brocken)

Details zur Architektur: siehe [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) Kapitel 4–8.

| # | Feature | Beschreibung | Status |
|---|---------|-------------|--------|
| 18 | Dateinamen-Parser | Segmente splitten an Trennzeichen (siehe ADR-022) | ⬜ |
| 19 | Segment-Zuweiser GUI | 3-Schritt-Wizard (Typ wählen, Muster, Ordner) | ⬜ |
| 20 | Plantyp-Erkennung | prefix/contains/regex Muster matchen | ⬜ |
| 21 | PatternTemplates | Vorschlagslogik beim Profil-Anlegen (siehe ADR-010) | ⬜ |
| 22 | profiles.json | Pro Projekt auf OneDrive speichern | ⬜ |
| 23 | pattern-templates.json | Globale Musterbibliothek auf OneDrive | ⬜ |
| 24 | Import-Workflow Schritt 1-5 | Scan, Parse, Validate, Classify, Plan (siehe ADR-008) | ⬜ |
| 25 | Import-Vorschau (Schritt 6) | GUI mit Status, Rechtsklick-Korrektur | ⬜ |
| 26 | Import-Execute (Schritt 7-8) | Dateien verschieben, Journal, Finalize | ⬜ |
| 27 | Index-Archivierung | Alte Indizes → _Archiv/ verschieben | ⬜ |
| 28 | Undo-Journal (SQLite) | 3 Tabellen: journal, actions, action_files (siehe ADR-009) | ⬜ |
| 29 | Recovery (Schritt 9) | Beim App-Start: pending → Reparatur anbieten | ⬜ |
| 30 | Undo (Schritt 10) | Journal rückwärts, Dateien zurück | ⬜ |
| 31 | Backup vor Import | SQLite + JSON als .bak kopieren | ⬜ |
| 32 | Unbekannte Dateien Dialog | Profil erweitern / Neues Profil / Skip | ⬜ |
| 33 | Erkennungs-Konflikt Dialog | Mehrere Profile passen → User wählt | ⬜ |
| 34 | **Plan-Sammler** | Pläne aus bestimmten Ordnern per Checkbox-Auswahl sammeln und nach bekanntem Schema sortieren | ⬜ |

---

## V1.1 — Bald danach

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Adressbuch | Zentrale Kontakt-DB in SQLite (`contacts` Tabelle), projektübergreifend, Outlook-kompatibel. Getrennt von project_participants — Verknüpfung über contact_id FK (vorbereitet seit v0.14.0) | ⬜ |
| Bauherren-/Firmenliste | Dropdown "Auftraggeber wählen" statt jedes Mal tippen (siehe ADR-021) | ⬜ |
| Button "Aus Adressbuch" | Im Tab 3 Beteiligte — Kontakt aus zentralem Adressbuch übernehmen (Button vorbereitet, disabled) | ⬜ |
| **Firmenliste importieren** | Geführter KI-Ablauf im Tab 3: Prompt anzeigen → User kopiert → gibt PDF an KI → fügt Antwort ein → App parst und übernimmt. Später automatisch per KI-API (siehe KI-API-Import unten). Button vorbereitet (disabled). | ⬜ |
| Planlisten-Import | Excel (.xlsx) + Soll/Ist-Abgleich | ⬜ |
| Planlisten-Export | Excel + PDF (ClosedXML + QuestPDF) | ⬜ |
| Schnellsuche Pläne | Plan finden über alle Projekte | ⬜ |
| CSV-Import | Für Planlisten | ⬜ |

---

## KI-API-Import (Querschnittsfeature)

**Konzept:** Statt manueller Copy-Paste-Workflows werden Daten-Importe automatisch über KI-APIs abgewickelt. Betrifft alle Szenarien wo strukturierte Daten aus unstrukturierten Quellen extrahiert werden.

**Anwendungsfälle:**
- Firmenliste importieren (Tab 3 Beteiligte) — PDF/Bild → strukturierte Kontaktdaten
- Plankopf-Extraktion — PDF → Plannummer, Index, Revisionstabelle, Datum
- Index-Import — PDF-Planlisten → strukturierter Soll/Ist-Abgleich
- Zukünftige Imports — alles wo unstrukturierte Dokumente geparst werden müssen

**Implementierung:**
- **Phase 1 (jetzt):** Geführter manueller Ablauf — App zeigt Prompt, User kopiert zu Claude/ChatGPT, fügt Antwort ein, App parst
- **Phase 2 (später):** Direkter API-Call aus der App — User wählt Datei, App sendet an KI-API, empfängt strukturierte Daten
- **Systemeinstellungen:** Auswahl zwischen ChatGPT API und Claude API (Anthropic API) — konfigurierbar pro User
- **API-Key-Verwaltung:** Sicher gespeichert (nicht in settings.json, sondern in Windows Credential Manager oder separater verschlüsselter Datei)
- **Offline-Fallback:** Wenn keine API verfügbar → manueller Prompt-Ablauf bleibt immer verfügbar

**Architektur-Implikationen:**
- JSON als Standard-Austauschformat zwischen KI und App
- Gemeinsamer Parser für KI-Antworten (egal ob manuell eingefügt oder per API empfangen)
- Service-Interface `IKiImportService` mit Implementierungen für Claude und ChatGPT
- Prompt-Templates als Ressourcen in der App (versioniert, aktualisierbar)

---

## Nach V1 — Module (Konzepte in Docs/Konzepte/)

Jedes Modul hat ein eigenes Konzeptdokument mit Details. Hier nur Kurzübersicht + Status.

| Modul | Kurzbeschreibung | Konzept-Dok | Status |
|-------|-----------------|-------------|--------|
| **Dashboard** | Zentrale Projektansicht mit Widgets, Kennzahlen, Schnellzugriff, Portal-Links (Vorschau in Tab 4) | [ModuleDashboard.md](Konzepte/ModuleDashboard.md) | ⬜ Konzept |
| **Bautagebuch** | Tägliches Protokoll, Auto-Befüllung, Export (Word/Excel/PDF) | [ModuleBautagebuch.md](Konzepte/ModuleBautagebuch.md) | ⬜ Konzept |
| **Foto-Management** | Viewer, Tags, Geodaten, Baubericht-Integration | [ModuleFoto.md](Konzepte/ModuleFoto.md) | ⬜ Konzept |
| **Arbeitszeiterfassung** | WPF-Maske → Excel via ClosedXML (siehe ADR-018) | — (im ADR dokumentiert) | ⬜ Konzept |
| **Outlook** | COM Interop, Projekt-Ordner, Anhänge → _Eingang | [ModuleOutlook.md](Konzepte/ModuleOutlook.md) | ⬜ Konzept |
| **Wetter** | API pro Baustelle, Betonierfreigabe | [ModuleWetter.md](Konzepte/ModuleWetter.md) | ⬜ Konzept |
| **Vorlagen** | Excel/Word mit Projektdaten befüllen (COM Interop) | [ModuleVorlagen.md](Konzepte/ModuleVorlagen.md) | ⬜ Konzept |
| **Plankopf-Extraktion** | Revisionstabelle + Plannummer aus PDF lesen (PdfPig), Template pro Büro. Kann KI-API nutzen (siehe oben) | — (Dok wird erstellt) | ⬜ Idee |
| **Mobile PWA** | Bautagebuch + Plan-Viewer am Handy, offline-fähig (siehe ADR-019, ADR-020) | — (BPM-Mobile-Konzept.md) | ⬜ Konzept |

---

## Später — Einzelfeatures (nach V1)

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| GIS Steiermark API | KG, GST, Koordinaten automatisch befüllen (Buttons in Tab 1 vorbereitet) | ⬜ |
| Google Maps API | Adresse → PLZ, Ort, Gemeinde, Bezirk, Koordinaten | ⬜ |
| Outlook-Adressbuch | Kontakte aus Outlook importieren | ⬜ |
| Adressbuch-Sync | Eigenes Adressbuch ↔ Outlook | ⬜ |
| PDF-Vorschau | Pläne in der App anzeigen | ⬜ |
| VERALTET-Stempel | Auf alte Plan-PDFs stempeln | ⬜ |
| Auto-Update | App-Update-Mechanismus | ⬜ |
| Projekt im Explorer öffnen | Button zum direkten Öffnen des Projektordners | ⬜ |

---

## Vision — Zukunftsideen

Ausführliche Beschreibung in [VISION.md](VISION.md). Hier nur die Kurzliste der Ideen die Architekturentscheidungen beeinflussen:

- **Projekt-Dashboard** — Zentrale Ansicht mit Kennzahlen, Planänderungen, Ordner-Schnellzugriff, Portal-Links (Vorschau bereits in Tab 4), Toolbar → Details in [Konzepte/ModuleDashboard.md](Konzepte/ModuleDashboard.md)
- **Firmendaten-Verwaltung** — Zentrale Auftraggeber-DB mit Portal-URLs, wiederkehrenden Infos → vorbereitet durch ADR-021 (Client als eigene Entität) + contact_id in project_participants
- **KI-API-Import** — Automatische Datenextraktion aus PDFs/Bildern per ChatGPT oder Claude API (konfigurierbar in Systemeinstellungen)
- **Kalender-Integration** — Projekt-Termine, Outlook-Sync, Dashboard-Widget
- **Plankopf-Extraktion** — Revisionsgrund + Plannummer aus PDF auslesen, Template-basiert pro Büro → kann KI-API nutzen
- **Bestellungen + Material** — ClickUp-Anbindung, Betonbestellung, Lieferlisten

**Architektur-Implikationen** (beim aktuellen Coden beachten):
- Client/Firma als eigene Entität vorbereiten (ADR-021)
- Adressbuch getrennt von Projekt-Beteiligten (contact_id FK vorbereitet)
- Projekt-ID überall mitführen
- Modul-Architektur beibehalten (ADR-001)
- Externe Links als konfigurierbare Datenstruktur (nicht hardcoded)
- KI-Import als gemeinsames Service-Interface (IKiImportService) für alle Import-Szenarien
- JSON als Standard-Austauschformat für KI-Antworten

---

## Ideen / Noch nicht eingeordnet

- DB Browser for SQLite installieren (zum Nachschauen der DB)
- Pfade-Erkennung via .bpm-manifest bei Ordner-Umbenennung (→ Feature #11)

---

## Aktuelles DB-Schema (v1.5)

```
clients (seq, id, company, contact_person, phone, email, notes)
projects (seq, id, project_number, name, full_name, status, project_type, client_id,
          street, house_number, postal_code, city, municipality, district, state,
          coordinate_system, coordinate_east, coordinate_north,
          cadastral_kg, cadastral_kg_name, cadastral_gst,
          project_start, construction_start, planned_end, actual_end,
          root_path, plans_path, inbox_path, photos_path, documents_path, protocols_path, invoices_path,
          tags, notes, created_at, updated_at)
buildings (legacy, noch vorhanden)
building_parts (seq, id, project_id, short_name, description, building_type, zero_level_absolute, sort_order)
building_levels (seq, id, building_part_id, prefix, name, description, rdok, fbok, rduk, sort_order)
project_participants (seq, id, project_id, role, company, contact_person, phone, email, contact_id, sort_order)
project_links (seq, id, project_id, name, url, link_type, sort_order)
schema_version (version)
```

---

*Backlog wird bei neuen Features und nach Abschluss von Aufgaben aktualisiert. Herbert committet und pusht selbst.*
