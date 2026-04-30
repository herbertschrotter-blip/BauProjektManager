---
doc_id: backlog
doc_type: backlog
authority: source_of_truth
status: active
owner: herbert
topics: [features, roadmap, v1-scope, planmanager, post-v1, bugs]
read_when: [neues-feature, feature-status, priorität-klären, was-kommt-als-nächstes]
related_docs: [architektur, planmanager, changelog]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Feature-Liste mit Status, Prioritäten und V1-Scope für das gesamte BPM-Projekt
- Autorität: source_of_truth
- Lesen wenn: Neues Feature planen, Feature-Status prüfen, Priorität klären, nächsten Schritt bestimmen
- Nicht zuständig für: Versionshistorie (→ CHANGELOG.md), Architektur-Details (→ Architektur.md)
- Pflichtlesen: keine (freie Struktur)
- Fachliche Invarianten:
  - V1-Scope: NUR PlanManager + Einstellungen — alles andere ist Post-V1
  - Feature-Nummern fortlaufend, nie wiederverwenden
  - Status-Werte: ✅ erledigt, ⬜ offen, 🔧 in Arbeit

---

﻿# BauProjektManager — Backlog

**Letzte Aktualisierung:** 15.04.2026  
**Aktuelle Version:** v0.25.16

**Verwandte Dokumente:**
- [VISION.md](../Referenz/VISION.md) — Nordstern, Schmerzpunkte, Zielgruppe
- [ADR.md](../Referenz/ADR.md) — Architekturentscheidungen (51 ADRs)
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
| **Should** | Wichtig, aber V1 funktioniert auch ohne | .bpm/ Ordner (ADR-046), Archivierung |
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
| 6 | ~~Auto-Increment IDs~~ → ULID (ADR-039 v2) | ✅ v0.25.1 (ULID-Migration implementiert) |
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

Details: [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) Kap. 4 (Überblick) + [PlanManager.md](../Module/PlanManager.md).

| # | Feature | Beschreibung |
|---|---------|-------------|
| 18 | Dateinamen-Parser | Segmente splitten an Trennzeichen (ADR-022) ✅ v0.24.3 |
| 19 | Segment-Zuweiser GUI | 5-Schritt-Wizard: Datei, Segmente, Index, Zielordner, Erkennung ✅ v0.24.10 (UI fertig, Speichern offen) |
| 20 | Plantyp-Erkennung | prefix/contains/regex Muster |
| 21 | PatternTemplates | Vorschlagslogik (ADR-010) |
| 22 | .bpm/profiles/*.json | Pro Projekt im .bpm/-Ordner (ADR-046) — ProfileManager Service |
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
| .bpm/ Ordner (#11) | Manifest-Split: `.bpm/manifest.json` (schlank) + `.bpm/project.json` (Vollexport) + `.bpm/profiles/` (Plantyp-Profile). Ersetzt einzelne `.bpm-manifest`-Datei (ADR-046). Migration automatisch. | ⬜ |
| Projekt archivieren (#12) | Status → Completed, Ordner von BasePath nach ArchivePath verschieben. Kein eigener Status „Archived" (ADR-025). Pfad-Resolution: relativer folder_name + Manifest-Fallback (Option C). Button vorbereitet (disabled) | ⬜ |
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
| ADR-039 v2 ULID-Schema | ✅ Implementiert v0.25.1 — alle Tabellen auf ULID migriert | ✅ |

### Sync-Infrastruktur (gemäß ADR-053 — Server-Sync-Architektur)

> **Hinweis:** Der frühere Plan mit Outbox/Inbox + FolderSync (DatenarchitekturSync.md, ADR-037) ist durch ADR-053 (2026-04-30) **superseded**. Stattdessen: BPM-eigenes Pull/Push-Sync-Protokoll (`IBpmSyncClient`) mit Server-Authority + ASP.NET Core 10 Worker Service + PostgreSQL 17 auf Windows-VPS. Dokumentiert in CGR-2026-04-30-datenarchitektur-sync (7 Runden).

**Status der Vorarbeit (✅ implementiert):**

| Feature | Status |
|---------|--------|
| ULID PRIMARY KEY für alle Tabellen (ADR-039 v2) | ✅ v0.25.1 |
| Sync-Metadaten v2.1 (7 Spalten: created_by, last_modified_at, last_modified_by, sync_version, is_deleted, ADR-050) | ✅ v0.25.23 |
| settings.json Split: device-settings.json + shared-config.json | ✅ implementiert |
| IUserContext + LocalUserContext (ADR-052) | ✅ v0.25.22 + DI v0.25.25 |

**Spike-Reihenfolge (laut ADR-053, alle Tasks im Tracker als BPM-088 ff):**

| Spike | Beschreibung | Tracker-ID | Status |
|-------|-------------|------------|--------|
| Spike 0 | ProjectDatabase syncfähig (Soft Delete + gezielte Upserts statt Replace-All-Listen) | bestehender Backlog (siehe BPM-016 etc.) | ⬜ |
| Spike 1 | ASP.NET Core 10 Worker Service Skelett + PostgreSQL 17 lokal + /health | BPM-088 | ⬜ |
| Spike 2 | ASP.NET Identity + JWT + Refresh Token | BPM-089 | ⬜ |
| Spike 3 | Sync-Endpoints Pull/Push für `clients` + `projects` | BPM-090 | ⬜ |
| Spike 4 | Windows-VPS Setup (Strato VC 2-8) + Domain + Caddy + HTTPS | BPM-091 | ⬜ |
| Spike 5 | Multi-Client-Test mit 2 lokalen SQLite-Instanzen + Server | (Sub-Task von Spike 3) | ⬜ |
| Schema | recognition_profiles wandert in DB-Tabelle (post Spike 0) | BPM-092 | ⬜ |

**Verworfene Ansätze (nicht mehr im Backlog, durch ADR-053 obsolet):**

- ❌ change_log + sync_outbox + sync_inbox Tabellen (war für FolderSync)
- ❌ IChangeTracker + Mutation Boundary mit Outbox/Inbox
- ❌ ISyncExporter + ISyncImporter
- ❌ FolderSyncTransport (Phase 2 Cloud-Ordner-Sync)
- ❌ HttpSyncTransport (durch IBpmSyncClient ersetzt)
- ❌ Diary-Aggregate-Split (war Konflikt-Vermeidung für Multi-Writer-Cloud-Sync — entfällt mit Server-Authority)
- ❌ users + user_devices als eigene Tabellen (durch ASP.NET Identity ersetzt)

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
| SQLite-Verschlüsselung | SQLCipher für Encryption at rest. DSGVO-relevant sobald Mitarbeiterdaten (Zeiterfassung) in bpm.db liegen | ⬜ |

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
| Auto-Update / Vertrieb | — | F5 reicht für Entwicklung. Optionen für Release: Velopack/Squirrel, MSIX/Windows Store, eigene Website. Konzeptdokument vor Marktreife erstellen |
| Kontextbezogene Hilfe | — | Fragezeichen-Icon pro Modul/Feature → öffnet Hilfe-Website oder Chat-Bot mit Modul-Kontext als Parameter. Support-Bot (RAG) beantwortet Standardfragen aus Endnutzer-Doku |
| Automatische Fehlerberichte | — | Unbehandelte Exception → Serilog-Logs + Stacktrace + Systeminfo → GitHub Issue via API. Über IExternalCommunicationService, DataClassification ClassA. Explizite User-Zustimmung per Dialog |
| Sicherheitskonzept | — | Docs/Konzepte/Sicherheitskonzept.md erstellen vor Zeiterfassung/KI-Assistent/Multi-User. Themen: SQLCipher, Code Signing, Obfuscation, registry.json Whitelist |
| **Server-Architektur** | **ADR-053** + [CGR-2026-04-30-datenarchitektur-sync](../Referenz/chatgpt-reviews/CGR-2026-04-30-datenarchitektur-sync/) | Windows-only Stack: ASP.NET Core 10 Worker Service + PostgreSQL 17 + Caddy auf Windows-VPS (Strato VC 2-8 ~12€/Mo). Phase 0/1 = eigene Firma 2 Jahre, Phase Verkauf = On-Premise bei Bauunternehmen. Tracker-Tasks: BPM-088 bis BPM-092 |
| **Server-Sync-Spike** | [ADR-053 Punkte 4-13](../Referenz/ADR.md) | IBpmSyncClient + Pull/Push + server_version + Server-gewinnt. Spike 0-5 (siehe oben). Microsoft.Datasync NICHT — eigenes Sync-Protokoll |
| **Nachkalkulation** | — (Konzept noch zu erstellen) | Haupttreiber für Server-Modus. Bestellungen, Lieferscheine, Lohnstunden, Geräte, NU-Rechnungen. Braucht Zeiterfassung + Server |
| **Auth / RBAC** | [ADR-053 Punkte 7-10](../Referenz/ADR.md) | ASP.NET Identity + JWT + Refresh Tokens. Rollen Phase 0/1: admin, bauleiter, polier, gast. AD/LDAP optional in Phase Verkauf |
| **Audit-Trail** | [ADR-053](../Referenz/ADR.md) | audit_log Tabelle für kritische Operationen (Nachkalkulation, Freigaben). Erst mit Nachkalkulation |

---

## Querschnittsfeature: KI-API-Import

Betrifft mehrere Module — hier zentral dokumentiert.

**Phase 1 (manuell):** App zeigt Prompt → User kopiert zu Claude/ChatGPT → fügt Antwort ein → App parst JSON  
**Phase 2 (automatisch):** App ruft KI-API direkt auf (ChatGPT oder Claude, konfigurierbar)

**Anwendungsfälle:** Firmenliste (Tab 3), Plankopf-Extraktion, Index-Import, LV-Analyse  
**Architektur:** IKiImportService Interface, JSON-Austausch, Prompt-Templates, API-Keys in Credential Manager  
**Details:** ADR-027, [ModuleKiAssistent.md](Konzepte/ModuleKiAssistent.md)

---

## DB-Schema (v2.0 — ULID implementiert ab v0.25.1)

ULID-Migration implementiert (ADR-039 v2). Sync-Felder-Konvention ab v0.25.16 (ADR-050). Details: [DB-SCHEMA.md](DB-SCHEMA.md)

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
- **Sync-Vorbereitung (ADR-050/051):** Neue Tabellen: ULID + `created_at`, `created_by`, `last_modified_at`, `last_modified_by`, `sync_version`, `is_deleted` — UTC, Soft Delete, Writes über Services (CODING_STANDARDS Kap. 19)

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Wenn nein → hier notieren, nicht jetzt bauen.*

---

## Docs-Schulden (aus Cross-Review Runde 1–5)

| # | Thema | Betroffene Docs | Aufwand | Status |
|---|-------|-----------------|---------|--------|
| 67 | CODING_STANDARDS: Veraltete Beispiele (ILogService, RegistryService, LogManager) auf Serilog/DI modernisieren | CODING_STANDARDS | Groß | ⬜ |
| 69 | Wording-Vereinheitlichung: Plantyp/Dokumenttyp/RecognitionProfile — Glossar-Regel definieren | GLOSSAR, PlanManager, UI_Navigation | Klein | ⬜ |
| 71 | Geplante DB-Tabellen ohne "geplant"-Label (work_assignments, material_orders, time_entries in UX_Flows etc.) | UX_Flows, GLOSSAR | Klein | ⬜ |
| 73 | settings.json zentrales Feld-Schema fehlt — ein Unterkapitel als source_of_truth definieren | Architektur oder DB-SCHEMA | Mittel | ⬜ |
