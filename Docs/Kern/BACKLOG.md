# BauProjektManager — Backlog

**Letzte Aktualisierung:** 10.04.2026  
**Aktuelle Version:** v0.24.10

**Verwandte Dokumente:**
- [VISION.md](../Referenz/VISION.md) — Nordstern, Schmerzpunkte, Zielgruppe
- [ADR.md](../Referenz/ADR.md) — Architekturentscheidungen (44+ ADRs)
- [CHANGELOG.md](../Referenz/CHANGELOG.md) — Versionshistorie ab v0.0.0
- [DEPENDENCY-MAP.md](../Referenz/DEPENDENCY-MAP.md) — Solution-Struktur + Ökosystem
- [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) — Technische Spezifikation v2.0
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — Code-Richtlinien
- [DSVGO-Architektur.md](DSVGO-Architektur.md) — Datenschutz, Privacy Engineering
- Modul-Konzepte: [Docs/Konzepte/](../Konzepte/)

---

## Priorisierung

### MoSCoW-Methode

| Kategorie | Bedeutung | Beispiel |
|-----------|-----------|---------|
| **Must** | Ohne das ist V1 nicht nutzbar | PlanManager, Projektordner |
| **Should** | Wichtig, aber V1 funktioniert auch ohne | .bpm-manifest, Archivierung |
| **Could** | Nice to have, wenn Zeit da ist | Suchfeld, Adressbuch |
| **Won't** | Bewusst nicht in V1 — kommt später | Dashboard, Bautagebuch, KI-Assistent |

### MVP (Minimum Viable Product)

**Die zentrale Frage:** *Was ist die kleinste Version die echten Nutzen bringt?*

**Antwort:** Pläne automatisch sortieren. Alles davor ist Infrastruktur dafür. Alles danach ist Erweiterung.

**MVP = V1:** Einstellungen ✅ → **PlanManager** ⬜ → Release

**Kernfrage bei jeder neuen Idee:** *"Brauche ich das um Pläne zu sortieren?"*  
Wenn nein → hier aufschreiben, nicht jetzt bauen.

---

## Must — V1 Pflicht

Ohne diese Features ist V1 nicht brauchbar. Der PlanManager ist das Kernprodukt.

### Einstellungen (✅ erledigt)

| # | Feature | Status |
|---|---------|--------|
| 1 | App-Shell + Navigation (MainWindow, Sidebar, Statusleiste) | ✅ v0.4.0 |
| 2 | Serilog Logging (File + Console, 30 Tage) | ✅ v0.5.0 |
| 3 | Domain-Modelle (Project, Client, Location, Timeline, Paths) | ✅ v0.5.1 |
| 4 | Projektliste + Bearbeitungs-Dialog | ✅ v0.7.0 |
| 5 | SQLite-Datenbank (bpm.db) | ✅ v0.8.0 |
| 6 | ~~Auto-Increment IDs~~ → ULID (ADR-039 v2) | ✅ v0.8.2 → v0.17.0 |
| 7 | registry.json Export (flach, für VBA) | ✅ v0.9.0 |
| 9 | Ersteinrichtung (Cloud-Speicher, Pfade, settings.json) | ✅ v0.10.0 |
| 10 | Projektordner erstellen (nummeriert, Template, TreeView) | ✅ v0.11.0 |
| 13 | Pfade änderbar (📁-Buttons) | ✅ v0.12.3 |

### ProjectEditDialog — 5 Tabs (✅ erledigt)

| Tab | Feature | Status |
|-----|---------|--------|
| 1 | Stammdaten (2-Spalten, ProjectType, DatePicker, Status Active/Completed) | ✅ v0.13.0 |
| 2 | Bauwerk (BuildingPart + BuildingLevel, Live-Berechnung, Geschoss-Vorschlag) | ✅ v0.13.2 |
| 3 | Beteiligte (ProjectParticipant, CRUD, Rollen-Dropdown, Import vorbereitet) | ✅ v0.14.0 |
| 4 | Portale + Links (2-Spalten, Portal/Custom, Browser-Öffnen, Vorschau) | ✅ v0.15.0 |
| 5 | Ordnerstruktur (TreeView, Vorschau, Unterordner, Präfix an/aus) | ✅ v0.12.4 |

### PlanManager (⬜ nächste Phase — DAS KERNFEATURE)

Details: [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) Kapitel 4–8.

| # | Feature | Beschreibung |
|---|---------|-------------|
| 18 | Dateinamen-Parser | Segmente splitten an Trennzeichen (ADR-022) ✅ v0.24.3 |
| 19 | Segment-Zuweiser GUI | 5-Schritt-Wizard: Datei, Segmente, Index, Zielordner, Erkennung ✅ v0.24.10 (UI fertig, Speichern offen) |
| 20 | Plantyp-Erkennung | prefix/contains/regex Muster |
| 21 | PatternTemplates | Vorschlagslogik (ADR-010) |
| 22 | profiles.json | Pro Projekt auf Cloud-Speicher — ProfileManager Service |
| 23 | pattern-templates.json | Globale Musterbibliothek |
| 24 | Import-Workflow 1-5 | Scan, Parse, Validate, Classify, Plan (ADR-008) |
| 25 | Import-Vorschau | GUI mit Status, Rechtsklick-Korrektur |
| 26 | Import-Execute | Dateien verschieben, Journal, Finalize |
| 27 | Index-Archivierung | Alte Indizes → _Archiv/ |
| 28 | Undo-Journal | 3 SQLite-Tabellen (ADR-009) |
| 29 | Recovery | Beim App-Start: pending → Reparatur |
| 30 | Undo | Journal rückwärts, Dateien zurück |
| 31 | Backup vor Import | SQLite + JSON als .bak |
| 32 | Unbekannte Dateien | Profil erweitern / Skip |
| 33 | Erkennungs-Konflikt | Mehrere Profile → User wählt |

---

## Should — wichtig, aber V1 geht ohne

Diese Features verbessern V1, sind aber kein Blocker für den Release.

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| .bpm-manifest (#11) | Versteckte Datei als Projektordner-Ausweis + vollständiger Projekt-Snapshot für Import/Übergabe (ADR-013 v2). Hidden + ReadOnly. Auto-Write bei Speichern. | ✅ v0.20.0 |
| Projekt archivieren (#12) | Ordner in Archiv verschieben, Pfad aktualisieren. Button vorbereitet (disabled) | ⬜ |
| Single-Writer Mutex (#14) | Nur eine App-Instanz gleichzeitig (ADR-016) | ⬜ |
| Versionsnummer im Log (#16) | Aus Assembly, nicht hardcoded | ✅ v0.16.1 |
| Bestehende Ordner zuweisen | Existierenden Ordner einem Projekt zuweisen statt neu anlegen. "Projekt importieren" Button mit Auto-Manifest-Erkennung. | ✅ v0.20.0 |
| Ordner umbenennen auf Disk | Bei Sortierung/Präfix-Änderung Ordner auf Festplatte umbenennen | ⬜ |
| Plan-Sammler (#34) | Pläne per Checkbox sammeln und nach Schema sortieren | ⬜ |
| Ordner-Sync (#35) | Bidirektionaler Sync zwischen lokalem Projektordner und Remote (Firmenserver/Netzlaufwerk). Pro Projekt konfigurierbar: welche Ordner, welche Richtung (←→/←/→). Neue Dateien vom Remote landen im _Eingang/ → PlanManager sortiert. Sortierte zurücksynchen. Tab „Sync" im Projektdetail. [ModuleOrdnerSync.md](../Konzepte/ModuleOrdnerSync.md) | ⬜ |

### Datenschutz-Infrastruktur (PFLICHT vor erstem Online-Modul — DSGVO-Architektur Kap. 15)

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| IExternalCommunicationService | Zentrales Privacy Gate für alle externen HTTP-Calls (ADR-035) | ⬜ |
| IPrivacyPolicy + DI | RelaxedPrivacyPolicy (intern) + StrictPrivacyPolicy (kommerziell), Lizenz-gesteuert (ADR-036) | ⬜ |
| DataClassification Enum | ClassA/ClassB/ClassC im Domain-Projekt | ⬜ |
| external_call_log Tabelle | Audit-Log in bpm.db (DSVGO-Architektur Kap. 11.3) | ⬜ |
| Einstellungen: Datenschutz-Tab | Toggle pro externem Dienst, Audit-Log-Anzeige, Kill-Switch | ⬜ |
| Log-Rotation 30 Tage | Serilog retainedFileCountLimit (trivial) | ✅ v0.5.0 |
| ADR-039 v2 ULID-Schema | Alle Tabellen auf ULID PRIMARY KEY umgestellt. IIdGenerator in Domain, UlidIdGenerator in Infrastructure (NuGet: Cysharp/Ulid) | ✅ v0.17.0 |

---

## Could — nice to have

Gut wenn vorhanden, aber kein Grund V1 zu verzögern.

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Suchfeld Projekte (#15) | Schnellfilter in der Projektliste | ✅ v0.22.0 |
| Duplikat-Import verhindern | Prüfung ob Projekt schon in DB existiert (gleicher Pfad). `ProjectExistsByPath()` | ✅ v0.23.1 |
| Adressbuch | Zentrale contacts-Tabelle, projektübergreifend, Outlook-kompatibel (contact_id FK vorbereitet, ADR-024) | ⬜ |
| Button "Aus Adressbuch" | Tab 3: Kontakt aus Adressbuch übernehmen (Button vorbereitet, disabled) | ⬜ |
| Firmenliste importieren | Geführter KI-Ablauf: Prompt → Copy → Paste → Parse. Später per API (ADR-027). Button vorbereitet | ⬜ |
| Bauherren-Dropdown | Auftraggeber wählen statt tippen (ADR-021) | ⬜ |
| Planlisten-Import | Excel (.xlsx) + Soll/Ist-Abgleich | ⬜ |
| Planlisten-Export | Excel + PDF (ClosedXML + QuestPDF) | ⬜ |
| Schnellsuche Pläne | Plan finden über alle Projekte | ⬜ |
| Projekt im Explorer öffnen | Button zum Öffnen des Projektordners | ⬜ |
| CSV-Import | Für Planlisten | ⬜ |
| GIS Steiermark API | KG, GST, Koordinaten auto-befüllen (Buttons in Tab 1 vorbereitet) | ⬜ |
| Google Maps API | Adresse → PLZ, Ort, Gemeinde, Koordinaten | ⬜ |

---

## Won't have (this time) — bewusst nicht in V1

Gute Ideen, aber erst nach einem funktionierenden PlanManager. Konzepte sind dokumentiert.

| Modul / Feature | Konzept-Dok | Warum nicht jetzt |
|-----------------|-------------|-------------------|
| Dashboard | [ModuleDashboard.md](Konzepte/ModuleDashboard.md) | Braucht zuerst Daten aus PlanManager |
| Bautagebuch | [ModuleBautagebuch.md](Konzepte/ModuleBautagebuch.md) | Eigenständiges Modul, Desktop muss stabil sein |
| Foto-Management | [ModuleFoto.md](Konzepte/ModuleFoto.md) | PhotoFolder PS-Tool funktioniert als Übergangslösung |
| Arbeitszeiterfassung | ADR-018 | Excel funktioniert, WPF-Maske ist Komfort |
| KI-Assistent | [ModuleKiAssistent.md](Konzepte/ModuleKiAssistent.md) | Eigenständiges Modul, ChatGPT/Claude API |
| Outlook COM | [ModuleOutlook.md](Konzepte/ModuleOutlook.md) | VBA-Makros funktionieren als Übergangslösung |
| Wetter API | [ModuleWetter.md](Konzepte/ModuleWetter.md) | Kein Kernfeature |
| Vorlagen | [ModuleVorlagen.md](Konzepte/ModuleVorlagen.md) | Excel/Word-Vorlagen funktionieren manuell |
| Plankopf-Extraktion | [ModulePlanHeader.md](Konzepte/Moduleplanheader.md) | Braucht KI-API, kommt nach PlanManager |
| Mobile PWA | BPM-Mobile-Konzept.md | Desktop muss erst stabil sein (ADR-019) |
| KI-API-Import (Phase 2) | ADR-027 | Phase 1 (manuell) reicht erstmal |
| Outlook-Adressbuch Sync | — | Braucht zuerst eigenes Adressbuch |
| PDF-Vorschau | — | Windows PDF-Viewer reicht |
| VERALTET-Stempel | — | Manuell machbar |
| Auto-Update | — | F5 in Visual Studio reicht für Solo-Entwickler |

---

## Querschnittsfeature: KI-API-Import

Betrifft mehrere Module — hier zentral dokumentiert.

**Phase 1 (manuell):** App zeigt Prompt → User kopiert zu Claude/ChatGPT → fügt Antwort ein → App parst JSON  
**Phase 2 (automatisch):** App ruft KI-API direkt auf (ChatGPT oder Claude, konfigurierbar)

**Anwendungsfälle:** Firmenliste (Tab 3), Plankopf-Extraktion, Index-Import, LV-Analyse  
**Architektur:** IKiImportService Interface, JSON-Austausch, Prompt-Templates, API-Keys in Credential Manager  
**Details:** ADR-027, [ModuleKiAssistent.md](Konzepte/ModuleKiAssistent.md)

---

## Aktuelles DB-Schema (v2.0 ULID — ADR-039 v2)

Alle Tabellen verwenden `id TEXT PRIMARY KEY` (ULID). Keine `seq` Spalte. Details: [DB-SCHEMA.md](DB-SCHEMA.md)

clients (id, company, contact_person, phone, email, notes, created_at, updated_at)
projects (id, project_number, name, full_name, status, project_type, client_id,
          street, house_number, postal_code, city, municipality, district, state,
          coordinate_system, coordinate_east, coordinate_north,
          cadastral_kg, cadastral_kg_name, cadastral_gst,
          project_start, construction_start, planned_end, actual_end,
          root_path, plans_path, inbox_path, photos_path, documents_path,
          protocols_path, invoices_path,
          use_global_zero_level, global_zero_level,
          tags, notes, created_at, updated_at)
buildings (legacy)
building_parts (id, project_id, short_name, description, building_type, zero_level_absolute, sort_order, created_at, updated_at)
building_levels (id, building_part_id, prefix, name, description, rdok, fbok, rduk, sort_order, created_at, updated_at)
project_participants (id, project_id, role, company, contact_person, phone, email, contact_id, sort_order, created_at, updated_at)
project_links (id, project_id, name, url, link_type, sort_order, created_at, updated_at)
schema_version (version)
---

## Beim Coden beachten (Architektur-Implikationen)

- Client/Firma als eigene Entität vorbereiten (ADR-021)
- Adressbuch getrennt von Projekt-Beteiligten (contact_id FK vorbereitet, ADR-024)
- Projekt-ID überall mitführen
- **ID-Generierung:** Nur über `IIdGenerator.NewId()` — nie manuell IDs zusammenbauen (ADR-039 v2)
- Modul-Architektur beibehalten (ADR-001)
- Externe Links als konfigurierbare Datenstruktur (nicht hardcoded)
- KI-Import als gemeinsames Service-Interface (IKiImportService) für alle Import-Szenarien
- JSON als Standard-Austauschformat für KI-Antworten
- **Datenschutz:** Keine Personendaten in Logs — nur IDs (CODING_STANDARDS Kap. 17)
- **Datenschutz:** Kein direkter HttpClient für externe APIs — alles über IExternalCommunicationService (ADR-035)
- **Datenschutz:** DataClassification (A/B/C) bei jedem externen Call mitgeben
- **Datenschutz:** IPrivacyPolicy per DI, gesteuert über Lizenz — nicht settings.json (ADR-036)
- **Datenschutz:** API-Keys in DPAPI, NIEMALS in settings.json/registry.json/Git/Logs
- **Datenschutz:** registry.json Whitelist beachten bei neuen Feldern (DSVGO-Architektur Kap. 9.3)
- **Multi-User-Vorbereitung:** `IProjectDataService` Interface statt direkt ProjectDatabase.cs (MultiUserKonzept Kap. 8)
- **Multi-User-Vorbereitung:** `version` + `last_modified` + `modified_by` Felder in neuen Tabellen
- **Multi-User-Vorbereitung:** CRUD-Methoden mit „Änderungen seit Timestamp" Parameter

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Wenn nein → hier notieren, nicht jetzt bauen.*
