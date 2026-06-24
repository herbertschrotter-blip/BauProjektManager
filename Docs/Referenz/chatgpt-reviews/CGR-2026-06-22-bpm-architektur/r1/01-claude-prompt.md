## Rolle

Du bist ein erfahrener .NET-/Software-Architekt und führst ein technisches Review-Gespräch
mit einem Kollegen (Claude/Anthropic). Es geht um eine solide, etablierte Lösung — keine
Eigenerfindungen, sondern bewährte Muster mit klaren Trade-offs.

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User Herbert).
- Sprich direkt zu deinem Kollegen (Claude), NICHT zum User.
- Kein Meta-Kommentar über das Format.
- Schreibe deine GESAMTE Antwort in den Canvas.
- CANVAS-TITEL: "Review Runde 1"
- Fasse am Ende deiner Antwort zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv, um Aussagen zu verifizieren, Querverweise zu prüfen und Originaldateien
  zu lesen, wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Hinweis: Die Radial-„+ Neu…"-Erweiterung (Slice 3a) ist noch **nicht committet** — der
  `RadialSelectionController` auf dem Branch zeigt den Stand vor Slice 3a. Für dieses
  Architektur-Review ist das irrelevant; die zentralen Dateien (siehe unten) sind gepusht.

## Gesprächsregeln

- Ehrlich und kritisch, Probleme konkret benennen.
- Verbesserungen mit Code/Pseudocode zeigen (C#/.NET 10).
- Rückfragen bei fehlendem Kontext.
- Fokus halten, keine allgemeinen Exkurse.
- Kompakt; Code nur wo nötig.
- **Fokus:** dedizierte Datei-/Ordner-Verwaltungs-Abstraktion + Konsolidierung der zwei
  Ordner-Wahrheiten. Nenne bei jeder Empfehlung das etablierte Muster beim Namen
  (z.B. Ports & Adapters, Repository, Unit of Work, `System.IO.Abstractions`) und die
  Trade-offs.

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen.
- KEINE Backward-Compatibility-Patterns.
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern.
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als
  gewollter Standardweg.

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.
Quelle: INDEX.md Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads)

### BauProjektManager_Architektur.md (source_of_truth)
- Zweck: Schichtgrenzen, Dependency-Regel, Source-of-Record, Offline-First.
- Fachliche Invarianten:
  - Schichten: `Domain` (Models/Interfaces, keine Abhängigkeiten) ← `Infrastructure`
    (SQLite, Services, Dateisystem) ← Module (`PlanManager`, `Settings`) ← `App` (Shell/DI).
  - Module hängen von Domain/Infrastructure ab, nie umgekehrt.
  - Offline-first; Constructor Injection, keine `new`-Instanzen für Services.
  - SQLite ist Source-of-Record (bpm.db zentral + per-Projekt planmanager.db als
    rebuildbarer Cache, ADR-058-Addendum).

### ADR-059 + Addendum (Recognition v2 / Plan-Erfassung)
- Strategie B: manuelle Erstaufnahme + deterministisches Matching; Auto-Extraktion nur Assist.
- Ringe der Radial-Erfassung kommen aus **DB-Stammdaten**, NIE aus dem Dateisystem (kein Drift).
- `folder_name` wird **EINMAL** beim Anlegen erzeugt und in der DB gespeichert (Feld, kein
  Template zur Laufzeit); Zuordnung läuft über IDs. Umbenennen des Kürzels ändert den
  physischen Ordner NICHT.
- Addendum-Behauptung (Soll): „Default-Ordnerstruktur ist der Seed der Stammdaten — die DB
  ist ab dann führend." → genau hier klafft Soll/Ist.

### CODING_STANDARDS.md / DSGVO-Architektur.md
- Max 300–400 Zeilen/Klasse, 30–40 Zeilen/Methode; Nullable aktiv.
- Serilog: KEINE Personendaten in Logs (Dateinamen/Pfade können Personenbezug haben → vorsichtig).
- Externe HTTP nur über `IExternalCommunicationService` (für Dateisystem nicht relevant, aber
  zeigt das bevorzugte Port-Muster im Projekt).
- KEINE neuen Libraries ohne ausdrückliche Freigabe.

## Das Konzept / Ist-Zustand

### Problem 1 — Datei-/Ordner-Operationen sind ungekapselt
Es gibt **kein** vereinheitlichendes Interface (kein `IFileSystem`/`IStorage`/`IFolderService`).
`System.IO` (`Directory.CreateDirectory/Move/Exists`, `File.Move/Copy/Delete`, `Path.Combine`)
ist über **~29 Dateien in allen Schichten** verstreut — auch in ViewModels und Views
(z.B. `ProjectEditDialog.xaml.cs`, `FolderTemplateControl.xaml.cs`, `SettingsViewModel.cs`).

Vorhandene Bausteine (Infrastructure + PlanManager):
- **`ProjectFolderService`** (Infrastructure): erzeugt die Projektordner aus
  `FolderTemplate` inkl. Nummerierungslogik — `FolderTemplateEntry.GetNumberedName(pos)` →
  `"{pos:D2} {Name}"` (Position 1 + "Polierpläne" → `01 Polierpläne`).
- **`ImportExecutionService`** (PlanManager): transaktionaler Move mit harter Invariante
  „**Journal VOR Move**": `CreateImportJournal(pending)` → `Directory.CreateDirectory(target)`
  → `File.Move(src, dst, overwrite:true)` → Archiv-on-Overwrite (alte Datei nach `_Archiv`)
  → `CompleteImportJournal(completed/failed)`.
- **`ImportUndoService`**: Undo NUR des letzten Imports (Preflight-Trockenlauf, Dateien
  zurück in den Eingang, DB-Rollback per Soft-Delete + Supersede-Restore).
- **`RecoveryExecutorService`**: Recovery unvollständiger Imports anhand des Journals.
- **`CaptureConfirmService`**: mappt Pending → `ImportDecision` → `ImportExecutionService`.

### Problem 2 — Zwei Ordner-Wahrheiten, die divergieren
1. **`AppSettings.FolderTemplate`** (Domain) → physische Ordner beim Projekt-Setup.
   Plan-Unterordner (unter „Planunterlagen", alle `hasPrefix:true`): `Polierpläne`,
   `Statikpläne - Schalung`, `Statikpläne - Bewehrung`, `Fertigteilpläne`,
   `Baustelleneinrichtung`. Reale Ordner auf Platte: `01 Polierpläne`, `02 …` usw.
2. **`document_types`** (bpm.db, `DocumentTypeSeedService` seedet 7 generische Built-ins):
   `Polierplan`, `Statik`, `Bewehrung`, `Schalung`, `Architektur`, `Fertigteile`,
   `Protokolle`. `folder_name` = `NormalizeForFolderName(name)` → `Polierplan` (ohne Präfix,
   Singular).

Der Radial-Import baut den Zielpfad als
`{ProjectPaths.Plans}/{document_type.folder_name}/{building_part.folder_name}/{level}` →
`01 Planunterlagen/Polierplan/...` und legt damit `Polierplan` **neu** an, statt das
vorhandene `01 Polierpläne` zu treffen.

### Constraints
- C#/.NET 10 LTS, WPF, modularer Monolith (Single EXE; DLLs: App/Domain/Infrastructure/
  PlanManager/Settings).
- **Offline-first, Cloud-Speicher-neutral**: die Projektdateien liegen in OneDrive/Dropbox/
  Google-Drive-Ordnern → ein **externer Sync-Client schreibt parallel** in dieselben Ordner
  (Lock-/Race-/Teilsync-Risiken!).
- SQLite, CommunityToolkit.Mvvm, Serilog, Constructor Injection.
- Frühe Entwicklungsphase (keine Migrationen).
- Multi-User/Server-Sync ist später geplant (ADR-053, PostgreSQL + ASP.NET Core).
- KEINE neuen Libraries ohne Freigabe — falls eine Lib (z.B. `System.IO.Abstractions`) klar
  überlegen ist, benenne sie + Trade-off + eine lib-freie Alternative.

## Aufgabe — bitte konkret Stellung nehmen

1. **Zentrale Dateisystem-Abstraktion: ja/nein und in welcher Form?**
   Lohnt sich ein Filesystem-Port/Adapter (Ports & Adapters/Hexagonal) bzw. ein
   `IFileStorage`-Repository in Infrastructure? Welche Operationen gehören in den Port
   (atomare Move/Create/Exists/Enumerate/Read/Write), welche bleiben modulspezifisch
   (z.B. die Import-Journal-Logik)? Wie grob/fein schneiden, ohne ein „God-Interface" zu bauen?

2. **Testbarkeit ohne echtes Dateisystem.**
   Etabliertes Muster — eigenes schmales Interface mit `System.IO`-Adapter vs.
   `System.IO.Abstractions` (NuGet). Empfehlung + Begründung. Wenn Lib: warum lohnt sie sich
   gegenüber der lib-freien Variante, und reicht ein In-Memory-Fake für unsere Tests?

3. **Transaktionalität/Atomarität über Cloud-Sync-Ordner.**
   Ist unser „Journal-vor-Move"-Pattern der richtige Ansatz? Welche bewährten Ergänzungen
   gegen Halbzustände bei Crash/Abbruch **und** gegen Interferenz des externen Sync-Clients
   (z.B. temp-Datei + atomic rename, Same-Volume-Move-Garantie, Idempotenz, Marker-Dateien,
   Lock-Handling)? Was ist überzogen für einen Single-User-Desktop in dieser Phase?

4. **EINE Source-of-Truth für die Plan-Ordnerstruktur.**
   Wie `document_types` (DB) und `FolderTemplate` konsolidieren? Bewerte:
   (a) Typen aus der Vorlage ableiten/seeden (`folder_name` = realer nummerierter Ordner),
   (b) Mapping `document_type → Zielordner` als explizites Feld/Tabelle,
   (c) DB als alleinige Wahrheit, Vorlage nur Bootstrap beim Setup.
   Wie würde ein professionelles Projekt das lösen — und wie geht man mit dem
   positionsbasierten Präfix (`01 `) um, das die DB als festen `folder_name` speichern soll?

5. **Schichtgrenzen & Migrationsweg.**
   Wer darf das Dateisystem berühren (nur Infrastructure?) und wie kommen
   PlanManager/Settings/(später Foto) sauber per DI dran? Wie verhält sich die neue
   Abstraktion zur bestehenden `ProjectFolderService`/`ImportExecutionService`-Aufteilung —
   diese refactoren (auf den Port heben) oder als High-Level-Service auf dem Port belassen?
   Skizziere einen pragmatischen, schrittweisen Umbauweg (was zuerst), passend zur Frühphase.
