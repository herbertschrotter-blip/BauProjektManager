# Review Runde 3 — DB-Grenze: 2 DBs + Soft Reference vs. konsolidierte DB

## Rolle

Du bist ein erfahrener Software-Architekt (Schwerpunkt Datenpersistenz, SQLite, Desktop-Anwendungen) und führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic). Wir haben in r1/r2 das Drei-Ebenen-Modell (ADR-058, BPM-109) abgestimmt und mit Sign-off abgeschlossen. Runde 3 öffnet **eine einzige Grundsatzfrage neu**, die r1/r2 als gegeben angenommen haben: die **Datenbank-Grenze**.

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User Herbert).
- Sprich direkt zu deinem Kollegen (Claude), NICHT zum User.
- Kein Meta-Kommentar über das Format.
- Schreibe deine GESAMTE Antwort in Canvas.
- CANVAS-TITEL: "Review Runde 3"
- Fasse am Ende deiner Antwort zusammen:
  ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen und Originaldateien zu lesen.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Relevante Dateien: `Docs/Referenz/ADR.md` (ADR-058, ADR-053, ADR-046), `Docs/Kern/DB-SCHEMA.md` (Kap. 6 + 6.7), `Docs/Module/PlanManager.md` (Kap. 10), `src/BauProjektManager.PlanManager/Services/PlanManagerDatabase.cs`, `src/BauProjektManager.Infrastructure/Persistence/ProjectDatabase.cs`.
- Hinweis: Schema v2.0 ist **noch nicht** im Code — nur als DDL in DB-SCHEMA Kap. 6.7 + ADR-058 definiert. Implementation (BPM-109.01) ist genau das, was diese Entscheidung blockiert.

## Gesprächsregeln

- Ehrlich und kritisch. Wenn meine Einschätzung falsch ist, sag es direkt.
- Probleme konkret benennen, mit Zahlen/Pseudocode wenn nötig.
- Rückfragen bei fehlendem Kontext.
- Fokus halten: **ausschließlich die DB-Grenzen-Frage**, kein erneutes Aufrollen des Drei-Ebenen-Modells (das ist entschieden).
- Kompakt.

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen.
- KEINE Backward-Compatibility-Patterns.
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern.
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als gewollter Standardweg.

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.
Quelle: INDEX.md Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads)

### BauProjektManager_Architektur.md (source_of_truth)
- Zweck: Schichtgrenzen, Dependency-Regel, SoR je Modus, Offline-First.
- Typ: WPF Desktop, **modularer Monolith** (eine EXE, Module als DLLs), offline-first.
- Fachliche Invarianten:
  - SQLite ist System of Record (lokal).
  - Module hängen nicht direkt aneinander; Cross-Modul-Zugriff über öffentliche Service-Interfaces.

### DB-SCHEMA.md (source_of_truth)
- Zwei SQLite-Dateien mit unterschiedlichem Lebenszyklus:
  - **`bpm.db`** — zentral, eine Datei für die ganze App, `%LocalAppData%\BauProjektManager\bpm.db`. Enthält Stammdaten: `projects`, `clients`, `building_parts`, `building_levels`, `segment_types` (BPM-108), Settings. Quelle der Wahrheit, **wird gesynct** (ADR-053).
  - **`planmanager.db`** — **eine Datei pro Projekt**, `…\Projects\<ProjektID>\planmanager.db`. Enthält den Plan-Cache: `plan_revisions`, `plan_files`, `revision_file_links`, `import_journal`, `import_actions`, `import_action_files`. **Wird NICHT gesynct**, ist als rebuildbarer Cache gedacht.
- Schema v2.0 (Kap. 6.7, geplant, BPM-109): neue Tabellen `plan_documents`, umgebaute `plan_revisions` (mit `document_id` FK + Zeitstempeln), `plan_document_segments`, `plan_revision_events`, `plan_context_links`, `building_part_aliases` — **alle in `planmanager.db`**.

### PlanManager.md (secondary)
- `planmanager.db` ist als Cache konzipiert: Gerät B kann den Plan-Bestand aus dem Dateisystem + `.bpm`-Manifest rekonstruieren, ohne die `planmanager.db` zu kennen (PlanManager.md Kap. 9 + 385).
- Frühphasen-Reset = `planmanager.db` löschen → wird beim nächsten Start neu erstellt.

### ADR-058 (source_of_truth, in r1/r2 dieser Serie entschieden)
- Drei-Ebenen-Modell für zeitbezogene Cross-Modul-Abfragen (Bautagebuch BPM-056, Foto, Vorlagen).
- Fachliche Invariante: `plan_context_links.resolution_mode = 'fixed_revision'` — Cross-Modul-Snapshots ziehen `target_revision_id` fest (alte Berichte bleiben stabil).
- Foundation Slice (`.01–.04` + `.05a` Interface-Stub) ist V1-Sperrposten. Stop-Punkte definiert (u.a.: "Import-Journal/Undo wackelt → sofort Stopp", "Dateiverschiebung + DB-Commit inkonsistent → sofort Stopp", ">40 Tests gebrochen → Stopp").

### ADR-053 (secondary)
- Server-Sync-Architektur (Windows-only Stack, PostgreSQL + ASP.NET Core). `bpm.db` ist syncfähig (Sync-Felder: `last_modified_at`, `created_by`, `last_modified_by`, `sync_version`, `is_deleted`). Plan-Cache wird nicht gesynct.

## Der Sachverhalt (die zu prüfende Entscheidung)

Schema v2.0 (BPM-109.01) soll jetzt implementiert werden. Beim Lesen der DDL ist aufgefallen:

**Die neuen Tabellen in `planmanager.db` deklarieren FKs auf Tabellen, die in `bpm.db` liegen:**

| Neue Tabelle (planmanager.db) | FK laut DDL | Ziel-Tabelle liegt in |
|---|---|---|
| `plan_documents.building_part_id` | → `building_parts(id)` | **bpm.db** |
| `plan_documents.building_level_id` | → `building_levels(id)` | **bpm.db** |
| `plan_document_segments.segment_type_id` | → `segment_types(id)` | **bpm.db** |
| `building_part_aliases.building_part_id` | → `building_parts(id)` | **bpm.db** |

SQLite kann FKs nicht über getrennte DB-Dateien erzwingen (auch `ATTACH` aktiviert keine Cross-DB-FK-Constraints). Es gibt zwei Wege:

### Option A — 2 DBs behalten, Soft Reference (Claudes Empfehlung)
- Die 4 bpm.db-Bezüge werden reine `TEXT`-Spalten ohne `FOREIGN KEY`-Klausel ("logische Referenz" / "soft foreign key").
- Alle FKs **innerhalb** `planmanager.db` (`document_id`→`plan_documents`, `revision_id`→`plan_revisions`, `target_revision_id` etc.) bleiben hart erzwungen.
- Cross-DB-Lesezugriffe bei Bedarf via `ATTACH bpm.db` read-only joinen.
- Begründung: `planmanager.db` ist ein wegwerfbarer per-Projekt-Cache (andere Kardinalität, andere Sync-Politik als `bpm.db`). Die Import-Pipeline validiert `building_part_id`/`segment_type_id` ohnehin schon live gegen `bpm.db`, bevor sie schreibt → der FK-Wächter würde einen praktisch unmöglichen Fehler abfangen. Display-Werte (`raw_value`, `normalized_value`, `segment_key`) liegen ohnehin denormalisiert lokal.
- Kosten: ~0 zusätzlich (entspricht der geplanten Foundation Slice). Doc-Notiz "Cross-DB = logische Referenz" in Kap. 6.7 + ADR-058.

### Option B — auf eine konsolidierte DB umstellen
- Plan-Tabellen wandern in die zentrale `bpm.db` (Gegenrichtung unmöglich — `projects`/`clients`/Settings sind app-global, nicht pro Projekt zerschneidbar).
- Folge: `planmanager.db` ist eine Datei pro Projekt, `bpm.db` hält alle Projekte → jede Plan-Tabelle braucht eine `project_id`-Spalte; Unique-Index `ux_plan_revision_current` und alle Queries (`document_key` taucht 41× in 4 Service-Dateien auf) brauchen `project_id`-Scoping.
- Gewinn: die 4 FKs werden hart erzwingbar; Joins ohne `ATTACH`; eine DB für Backup/Migration.
- Verlust: wegwerfbarer per-Projekt-Cache weg (Frühphasen-Reset wird gefährliches `DELETE … WHERE project_id` statt Datei-Löschen); Plan-Import-Lärm liegt in der gesyncten `bpm.db` → muss explizit vom Sync ausgenommen werden; Korruptions-Blast-Radius trifft jetzt Stammdaten.
- Claudes Kostenschätzung: **~5–8 PT netto zusätzlich** zur Foundation Slice (PlanManagerDatabase-Umbau 1,5–2 PT, Connection/Transaktions-/DI-Vereinheitlichung 1–1,5 PT, DevTools-Reset + Recovery 1 PT, Test-Refactor 2–4 PT, Doc-Rework ~1–1,5 PT).
- Außerdem: würde mehrere der für BPM-109 vereinbarten **Stop-Punkte** auslösen ("Import-Journal/Undo wackelt → sofort Stopp", "Dateiverschiebung + DB-Commit inkonsistent → sofort Stopp", ">40 Tests"). Wäre damit per Sprint-Regel ein eigener Architektur-Release mit neuem ADR, kein Foundation-Slice-Schritt.

## Aufgabe

Bitte prüfe **ausschließlich die DB-Grenzen-Frage** (nicht das Drei-Ebenen-Modell):

1. **Gängige Praxis:** Ist für einen offline-first WPF-**Monolithen** mit zentraler Stammdaten-DB + wegwerfbarem per-Projekt-Cache die **Soft-Reference über die DB-Grenze** der Industrie-Standard? Oder gibt es ein anerkanntes Muster, das hier eher passt (z.B. doch Konsolidierung, oder ein dritter Weg)?
2. **Ist meine Einordnung korrekt**, dass "Database per Module" hier ein Anti-Pattern wäre, der Split `bpm.db` ↔ `planmanager.db` aber durch *per-Projekt-Cache* (Kardinalität + Disposability + Sync-Politik) gerechtfertigt ist — und nicht durch Modul-Trennung?
3. **Kosten-Nutzen:** Lohnt die Konsolidierung (Option B) trotz ~5–8 PT + Stop-Punkt-Kollision? Übersehe ich einen Nutzen, der die Kosten doch rechtfertigt (z.B. bzgl. ADR-053-Sync, Cross-Modul-Joins-Performance, künftige projektübergreifende Plan-Abfragen)?
4. **Risiko Soft Reference:** Welche konkreten Risiken hat die logische Referenz langfristig (verwaiste IDs bei Stammdaten-Löschung, Reporting über `ATTACH`, Sync-Konsistenz `bpm.db` ↔ Cross-Modul-Links)? Wie würdest du die absichern, ohne nach Option B zu kippen?
5. **Empfehlung:** Wenn du Option A teilst — gibt es eine kleine Härtung, die du ergänzen würdest (z.B. dokumentierte Invariante, `ON DELETE`-Strategie auf App-Ebene, Lookup-Validierung)? Wenn du Option B bevorzugst — was wäre dein minimaler, stop-punkt-konformer Schnitt?

Ziel: eine belastbare Empfehlung, ob BPM-109.01 auf der bestehenden 2-DB-Basis (Option A) gebaut wird oder ob wir vorher einen Konsolidierungs-ADR brauchen.
