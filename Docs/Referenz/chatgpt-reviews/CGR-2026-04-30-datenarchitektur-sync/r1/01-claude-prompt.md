# Initial-Prompt — CGR-2026-04-30-datenarchitektur-sync — Runde 1

## Rolle

Du bist erfahrener Software-Architekt mit Schwerpunkt auf verteilten Datenbanksystemen, Sync-Engines und offline-fähigen Multi-Device-Apps. Du führst ein technisches Review-Gespräch mit deinem Kollegen Claude (Anthropic). Vermittler ist der User Herbert Schrotter.

## Gesprächsformat

- Sprich direkt zu deinem Kollegen Claude, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- **Canvas-Titel:** "Review Runde 1 — Datenarchitektur & Sync"
- Fasse am Ende der Antwort zusammen:
  - ✅ Einigkeit (Punkte wo du Claudes Position teilst)
  - ⚠️ Widerspruch (Punkte wo du anderer Meinung bist, mit Begründung)
  - ❓ Rückfragen (Punkte wo dir Kontext fehlt)

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Prompt-Kontext nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/bugfixing` angeben.

Empfohlene Dateien zur eigenen Recherche:
- `Docs/Konzepte/DatenarchitekturSync.md` — vollständiges Phase-2-Konzept mit 12-Schritte-Code-Umbau
- `Docs/Konzepte/ServerArchitektur.md` — Phase-3-Zielbild
- `Docs/Konzepte/MultiUserKonzept.md` — Rollen, Phasen
- `Docs/Referenz/ADR.md` — ADR-046, ADR-047, ADR-050, ADR-051, ADR-052
- `Docs/Kern/DB-SCHEMA.md` — Sync-Spalten v2.1, Tabellen
- `Docs/Kern/BauProjektManager_Architektur.md` — Gesamtarchitektur, Speicher, Module
- `src/BauProjektManager.Infrastructure/Persistence/ProjectDatabase.cs` — heutige DB-Implementierung mit Sync-Spalten
- `src/BauProjektManager.Infrastructure/Services/AppSettingsService.cs` — device-settings.json + shared-config.json Split
- `src/BauProjektManager.PlanManager/Services/PlanManagerDatabase.cs` — pro-Projekt-DB

## Gesprächsregeln

- Ehrlich und kritisch — keine Höflichkeit auf Kosten der Klarheit
- Probleme konkret benennen, nicht abstrakt
- Verbesserungen mit Code/Pseudocode oder konkreten Library-Namen zeigen
- Rückfragen bei fehlendem Kontext (mit verzweigter Antwort: "wenn X, dann …, wenn Y, dann …")
- Fokus halten auf das Hauptthema dieser Runde
- Kompakt, Code nur wo nötig
- **Klare Empfehlung am Ende, nicht "es kommt darauf an"** — wenn doch, mit Konditionen die der User selbst beantworten kann

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als gewollter Standardweg

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

Quelle: INDEX.md Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads)

### BauProjektManager_Architektur.md (source_of_truth)
- Zweck: Gesamtarchitektur, Schichten, Modul-Grenzen, Speicherorte
- Stack: C# / .NET 10 LTS, WPF, SQLite, CommunityToolkit.Mvvm, Serilog, ClosedXML
- Architektur: Modularer Monolith — Single EXE, Module als separate DLLs
- Offline-First, Cloud-Speicher-neutral

### DatenarchitekturSync.md (secondary, teilweise superseded)
- Zweck: Datenarchitektur und Sync-Konzept — Klassifizierung, Outbox/Inbox, Snapshots, Rollen, Phasen
- Status: Teilweise superseded durch ServerArchitektur.md + ADR-050/051 (7-Spalten statt 12, kein Outbox/Inbox, Datasync-Spike)
- Fachliche Invarianten:
  - SQLite = einzige lokale Wahrheit — Events sind Replikationsartefakte
  - Transaktionale Mutation Boundary: Domain-Write + change_log + sync_outbox in einer Transaktion
  - Shared vs. restricted physisch getrennt (eigene Tabellen)
  - Phase 2 (FolderSync) bewusst temporär — Phase 3 (Server) ist Zielbild

### ADR-051 (Local-First)
- Aussage: State-based lokal, change-based zwischen Clients. SQLite ist einzige lokale Wahrheit. Events sind Replikationsmechanismus, nicht Source of Truth. Kein Full Event Sourcing.

### ADR-047 (4-Klassen-Datenmodell)
- Klasse A: Local-only (Logs, Undo, Caches, Device-Settings) — synct nie
- Klasse B: Shared domain (Projekte, Bauteile, Bautagebuch, Arbeitseinteilung) — synct
- Klasse C: Shared reference (ProjectTypes, BuildingTypes, FolderTemplate) — synct
- Klasse D: Restricted (Lohnsätze, Einheitspreise) — erst mit Server Phase 3

### ADR-050 (DB-Schema v2.1)
- Sync-Spalten auf allen 6 Entitätstabellen: created_by, last_modified_at, last_modified_by, sync_version, is_deleted
- Alle Timestamps UTC, ISO 8601
- sync_version inkrementiert bei jedem Update

### ADR-052 (IUserContext)
- IUserContext + LocalUserContext Modus A implementiert (UserId, DisplayName, Source)
- Zukünftiger Modus C (Server-Auth) als Erweiterung vorgesehen

### DSGVO-Architektur.md
- Klasse A (lokal, kein Personenbezug): PlanManager-Daten, Logs
- Klasse B (Personendaten projektintern): Bautagebuch, Foto, Adressbuch
- Klasse C (sensible Lohndaten): Lohnsätze, Personalakten
- Externe Kommunikation: nur über IExternalCommunicationService, mit DataClassification-Whitelist

## Bestandsaufnahme — was heute existiert

### Was funktioniert
- **Lokale `bpm.db`** in `%LOCALAPPDATA%\BauProjektManager\` — gerätelokal, Sync-Spalten v2.1 vorhanden, IUserContext liefert UserId+DisplayName an alle Inserts/Updates
- **PlanManager `planmanager.db`** in `%LOCALAPPDATA%\...\Projects\<projectId>\` — pro Gerät × Projekt, Cache für Plan-Revisionen + Import-Journal
- **`<workspace>\.AppData\BauProjektManager\shared-config.json`** im OneDrive — globale Settings, synct via OneDrive zwischen Geräten desselben Users
- **`<workspace>\.AppData\BauProjektManager\registry.json`** im OneDrive — generierter Export der Projektliste, Import-only von extern
- **`<Projekt>/.bpm/manifest.json`** — schlanker Ausweis (ProjectId, Name, Module-Flags) pro Projektordner
- **`<Projekt>/.bpm/project.json`** — voller Projektexport (alle 5 Tabs aus ProjectEditDialog) pro Projektordner — wird beim Speichern geschrieben
- **`<Projekt>/.bpm/profiles/<id>.json`** — RecognitionProfiles für PlanManager pro Projekt

### Was fehlt
- ❌ `change_log`-Tabelle nicht implementiert
- ❌ `sync_outbox` / `sync_inbox` / `sync_applied_events` / `sync_conflicts` / `sync_checkpoints` nicht implementiert
- ❌ Exporter (DB → JSON-Events) nicht implementiert
- ❌ Importer (JSON-Events → DB) nicht implementiert
- ❌ `project.json` wird zwar geschrieben, aber **nur als Manuell-Import-Tool** genutzt ("Projekt aus Ordner importieren") — kein automatischer Sync beim Start
- ❌ Keine Konflikt-Erkennung
- ❌ Keine globale Profil-Bibliothek

### Setup des Users
- 2-3 PCs (Desktop, Firmenlaptop, Surface), arbeitet **abwechselnd, nicht parallel**
- Aktuell Solo, kein Mitarbeiter-Multi-User
- Multi-User Phase 3 ist langfristiges Ziel (Bauleiter, Polier, Disponent, Lohnbüro), aber kein konkreter Termin
- DSGVO relevant ab dem Moment wo Personendaten in Modulen wie Bautagebuch / Adressbuch / Lohn landen

## Hauptfrage dieser Runde

**Welcher Sync-Mechanismus ist der richtige für BPM heute, mit klarer Roadmap zu Multi-User Phase 3?**

User-Anforderung wörtlich: *"ich will nichts neu erfinden. ich will gängige geprüfte funktionierende technik wenn möglich"*.

## Drei zur Diskussion stehende Optionen

### Option 1 — Eigenbau β3 (laut DatenarchitekturSync.md / ADR-051)

- 12 Schritte Code-Umbau:
  1. `change_log` Tabelle anlegen
  2. `sync_outbox` + `sync_inbox` Tabellen anlegen
  3. `sync_applied_events` (Idempotenz)
  4. `sync_conflicts` + `sync_checkpoints`
  5. Domain-Mutation-Wrapper: jede Save/Update/Delete schreibt parallel in change_log + sync_outbox in derselben Transaktion
  6. Exporter-Service: liest sync_outbox, schreibt JSON-Events in `<workspace>\.AppData\sync\events\`
  7. Importer-Service: liest neue JSON-Events, appliziert sie auf lokale DB, schreibt in sync_applied_events
  8. Snapshot-Generator: pro-Modul-Snapshot (root + diary + work + plans)
  9. Bootstrap-Logik
  10. Konflikt-Detection
  11. UI für Konflikte
  12. Settings-UI: Sync-Status anzeigen
- Sync-Bus: OneDrive (Cloud-Drive) — JSON-Events liegen im OneDrive-Ordner und synchronisieren via OneDrive zwischen Geräten
- Aufwand: realistisch mehrere Wochen, vielleicht zwei Releases
- Bekannte Industrie-Beispiele: Joplin (Notes-App, OneDrive/Dropbox-Sync) — bekannt für Konflikt-Probleme

### Option 2 — Self-Hosted CouchDB + PouchDB.NET

- CouchDB läuft als Service auf einem PC (oder Heim-NAS oder VPS für 5€/Monat)
- PouchDB.NET als Client-SDK auf jedem Gerät
- Bidirektionale Master-Master-Replikation als Library-Feature
- SQLite kann **als Replication-Target** unter PouchDB.NET laufen ODER ersetzt werden durch CouchDB-eigene lokale DB
- Konflikt-Erkennung: built-in (Revision-Tree, application-level resolution)
- Industrie-Reife: seit 2005, wird in Produktion bei vielen Apps eingesetzt
- DSGVO: Self-Hosted = volle Kontrolle, EU-Hosting trivial

### Option 3 — Hosted Supabase

- Supabase = Postgres + Realtime + Auth + Storage als Backend-as-a-Service
- .NET-Client-Library vorhanden
- Free Tier bis ~50K Rows, dann ~25€/Monat
- EU-Hosting möglich (DSGVO)
- Realtime-Sync via Postgres-Replication-Slots, offline-cache via Library
- Multi-User mit Row-Level-Security out-of-the-box
- Vendor Lock-in begrenzt (Postgres ist Standard, Migration zu eigenem Postgres möglich)

## Aufgabe

Liefere eine fundierte Bewertung mit dem Ziel einer **klaren Empfehlung** für den User. Konkret:

### A — Bewertung der drei Optionen

Pro Option:
- **Pro / Con** für das konkrete Setup (Solo, 2-3 PCs abwechselnd, Phase-3-Ziel)
- **Realistischer Implementierungsaufwand** (Personentage)
- **Fragility-Bewertung** — wo sind die Stolperfallen?

### B — Industrie-Praxis-Einordnung

- Welche **vergleichbaren Apps** (Desktop-WPF, offline-first, Multi-Device, später Multi-User) nutzen welchen Ansatz?
- Ist Cloud-Drive-Sync (Option 1) wirklich so unkonventionell wie behauptet — oder gibt es Production-Beispiele die das stabil betreiben?
- Wie ehrlich beworben sind CouchDB/PouchDB.NET und Supabase im 2026er Stand?

### C — Empfehlung

- **Welche Option ist die richtige Wahl?** Begründung in 5-10 Sätzen.
- **Falls "es kommt darauf an":** Welche Bedingungen vom User abfragen, dann mit Pfad pro Bedingung.

### D — Risiken / Edge Cases die übersehen werden könnten

- Z.B. Uhrzeit-Drift zwischen Geräten, halb-applizierte Events nach Crash, OneDrive-Konfliktdateien, Library-Lifecycle (Was wenn PouchDB.NET nicht mehr gepflegt wird?)
- DSGVO-Implikationen je nach Speicherort

### E — Phase-3-Pfad

- Welche Option lässt sich am saubersten zu echtem Multi-User-Server umstellen?
- Sind die Datenstrukturen kompatibel oder muss man neu anfangen?

## Sub-Themen für spätere Runden

Diese sind explizit **NICHT** Teil der ersten Runde — wenn die Empfehlung steht, sprechen wir das in r2/r3 durch:

- Profil-Bibliothek (Library + Instance Pattern) — state-based oder change-based-konform mitsynct
- `planmanager.db` — bleibt Klasse A (lokal) oder synct sie?
- Konflikt-Auflösung-Strategie (Last-Write-Wins, User-fragt, CRDT)
- Snapshot-Granularität (root + Modul-Snapshots oder pro Tabelle)
- Konkrete DSGVO-Whitelist je nach Sync-Pfad
- Kompatibilität zu IExternalCommunicationService (ADR-035) und external_call_log

## Bitte als nächstes

Beantworte A bis E. Halte dich kurz und konkret. Wenn du Code-Beispiele brauchst, nutze Pseudocode, nicht Voll-C#. Wenn du Library-Namen oder konkrete Production-Apps zitierst, gerne mit kurzem Beleg (Versionsstand, Repo-Link, eigene Erfahrung).

Am Ende:
- ✅ Einigkeit
- ⚠️ Widerspruch
- ❓ Rückfragen
