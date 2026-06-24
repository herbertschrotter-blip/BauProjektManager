## Review Runde 2

Danke, Kollege — Herbert hat nach Runde 1 entschieden. Wir vertiefen jetzt drei Punkte und
klären die offenen Detailfragen. Schreibe die GESAMTE Antwort in den Canvas, Titel
"Review Runde 2", und schließe mit ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

## Repo-Zugriff
Du hast Zugriff auf das GitHub-Repo:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch, NICHT `main`! Bei JEDEM
  Dateizugriff den Branch angeben.
- Relevante Dateien: `AppSettings.cs` (FolderTemplateEntry/SubFolderEntry), `DocumentTypeSeedService.cs`,
  `ProjectDatabase.cs` (InsertDocumentType/-Category, building_parts/levels), `ImportPlanBuilder.cs`,
  `RecognitionProfile`/Profile-Services, `ProfileWizard*`, `ImportExecutionService.cs`.

## Entschieden nach Runde 1 (Rahmen steht — bitte nicht neu aufrollen)
- Drei schmale FS-Ports + Adapter `LocalFileSystem`; eigenes Interface (kein System.IO.Abstractions jetzt).
- **DB = einzige Ordner-Wahrheit; FolderTemplate nur Bootstrap; `document_types.folder_name` = realer präfixierter Ordner.**
- Journal + temp-im-Zielordner + atomic rename + idempotente Recovery.
- **Seed-Quelle entschieden:** `ring2_source` (+ ggf. Anzeige-Name/Kategorien) wandert IN die FolderTemplate, der Seed leitet die `document_types` daraus ab. KEIN hardcodierter Ordnername→Typ-Switch.
- **Scope entschieden:** voller Umbau — ALLE ~29 System.IO-Stellen inkl. Settings-Views/ViewModels auf den Port heben.

## Vertiefung 1 — FolderTemplate als Typ-Quelle (Schema + Seed)
Aktuell hat die Vorlage nur zwei Ebenen: Hauptordner (`FolderTemplateEntry`) → Unterordner
(`SubFolderEntry`, mit `HasPrefix`). `document_types` haben aber zusätzlich **Kategorien**
(z.B. Fertigteile → Wände/Decken/Stiegen; Protokolle → Baubesprechung/…) und ein
`ring2_source` (BuildingParts / Categories / None).
Bitte konkret entwerfen:
1. Wie erweiterst du `SubFolderEntry` minimal, damit ein Vorlage-Unterordner einen Dokumenttyp
   vollständig beschreibt: `ring2_source`, optionaler Anzeige-Name (UI „Polierplan" vs Ordner
   „01 Polierpläne"), und — für `Categories`-Typen — die Kategorienliste (die selbst wieder
   `folder_name` brauchen)?
2. Welche Vorlage-Unterordner werden zu `document_types`, welche nicht? (z.B.
   „Ausschreibungspläne", „Baustelleneinrichtung" — Plan-Typ oder nur Ordner?) Klare Regel.
3. Bestätige die Aufteilung: **Ring 2/3 für räumliche Typen (BuildingParts/Levels) kommt aus
   den projektspezifischen `building_parts`/`building_levels`** (NICHT aus der Vorlage), während
   `Categories` aus der Vorlage/dem Typ kommen. Stimmt das so, oder schlägst du etwas anderes vor?
4. Wie sieht der Seed konkret aus (Pseudocode): Template → `document_types` + `document_type_categories`
   mit echtem präfixiertem `folder_name`, nur beim Projekt-Setup, danach DB führend?

## Vertiefung 2 — profile.TargetFolder brechen?
`ImportPlanBuilder` baut Zielpfade heute aus `plansRelativePath + profile.TargetFolder + FolderHierarchy + FileName`.
Die neue Radial-/DB-Strecke löst über `DocumentTypeId → document_types.folder_name` auf.
Es gibt also potentiell ZWEI Import-Wege (klassischer Profil-Import via `RecognitionProfile`
+ neue manuelle Radial-Erfassung). Bitte beantworte:
1. Sollen beide Wege auf dieselbe DB-Auflösung (`DocumentTypeId → folder_name`) konvergieren,
   und `profile.TargetFolder` ganz entfallen — oder bleibt der Profil-Import eigenständig?
2. Falls entfallen: konkreter, migrationsfreier Umbau (Frühphase) — welche Felder/Klassen
   (`RecognitionProfile`, `ImportPlanBuilder`, `ProfileWizard`, Profil-JSONs) sind betroffen,
   und wie verbindet ein Profil dann eine erkannte Datei mit einem `document_type` (per Id/Key)?
3. Risiko/Trade-off, wenn wir `profile.TargetFolder` JETZT brechen vs. später.

## Vertiefung 3 — Voller Scope (~29 Dateien) ohne Riesen-PR
Wir heben alle direkten System.IO-Zugriffe auf die Ports, inkl. Settings-Views/ViewModels.
Bitte liefere eine **konkrete Sequenz** (welche Slices/PRs in welcher Reihenfolge), sodass jeder
Schritt für sich baubar, testbar und reviewbar bleibt:
1. Sinnvolle Reihenfolge (Port zuerst → welche Schicht/Datei-Gruppe danach?).
2. Wo ist das Risiko am höchsten (transaktionaler Import vs. reine Pfad-Helfer in Views)?
3. Test-Strategie pro Slice (Fake vs. Temp-Integrationstest), und wie wir Regressionen an der
   bestehenden Import-/Undo-/Recovery-Strecke absichern (aktuell 346 Tests grün).

## Vertiefung 4 — In-App-Datei-Explorer (neue Anforderung)
Herbert möchte in BPM einen integrierten Datei-Explorer je Projekt: Ordner-/Datei-Baum
ansehen, mit Funktionen **Öffnen**, **Verschieben**, **Teilen** (Teilen nur, wenn nicht zu
aufwendig). Das ist ein direkter Konsument der FS-Ports. Bitte bewerte:
1. **Port-Konsum:** Reichen `IFileSystemReader`/`IFileSystemWriter` für einen Tree-Explorer
   (Lazy-Loading je Ordnerebene, Datei-Metadaten via `FileInfoSnapshot`)? Welche zusätzlichen
   Lese-Operationen braucht ein Explorer realistisch?
2. **Öffnen/Teilen als eigene Ports:** „Öffnen" (Standard-App via ShellExecute), „im Explorer
   anzeigen", „Ordner öffnen" → eigener `IFileLauncher`, NICHT im File-Port. „Teilen" →
   Empfehlung für offline-first/Cloud-neutral: Windows-Share-Sheet (`DataTransferManager`) vs.
   „Pfad kopieren"/„an Mail anhängen" vs. echte Cloud-Share-Links (Provider-API, online, Auth).
   Was ist MVP, was bewusst out-of-scope?
3. **⚠️ Konsistenz DB ↔ Dateisystem (Kernpunkt):** Wenn der Explorer eine **getrackte**
   Plandatei (in `plan_documents`/`plan_revisions`) verschiebt oder löscht, darf die DB nicht
   driften. Optionen: (a) Explorer-Move für getrackte Dateien über dieselbe Journal-/Move-/DB-
   Strecke routen; (b) getrackte Dateien im Explorer sperren/nur-lesend; (c) frei verschieben +
   Drift-Reconcile danach. Deine Empfehlung + wie unterscheidet der Explorer getrackt vs.
   ungetrackt?
4. **Fremdänderungen:** Cloud-Sync-Client oder Windows-Explorer können Dateien hinter BPMs
   Rücken verschieben/umbenennen/löschen. Wie sollte In-App-Explorer + DB damit umgehen —
   On-demand-Rescan/Reconcile vs. `FileSystemWatcher`? (In Runde 1 hatten wir Dauer-Watcher
   verworfen — gilt das auch hier, oder rechtfertigt der Explorer eine begrenzte Watch-/
   Rescan-Strategie?)

## Vertiefung 5 — DB-Scope & Startup-Reconcile (Kern-Datenmodell)
Frage ausgelöst durch den Explorer: Was ist das Verhältnis DB ↔ Dateisystem? Heute indiziert
`planmanager.db` nur die importierten Plandokumente (`plan_documents`/`plan_revisions`), nicht
jede Datei. Bitte bewerte und empfiehl:
1. **Grundmodell:** (A) DB = **kuratierter Index nur der getrackten Plandokumente**, Explorer
   liest das Dateisystem live, Startup reconciled nur die getrackte Teilmenge. (B) DB spiegelt
   den **ganzen Projektbaum** (alle Dateien/Ordner). (C) Hybrid (Planunterlagen-Teilbaum voll
   indiziert, Rest live). Welches ist das professionell richtige für offline-first + Cloud-Sync,
   und warum? (Claude tendiert klar zu A.)
2. **Was wird getrackt:** Bestätige/kritisiere: nur bewusst erfasste Plandokumente kommen in
   die DB (Erfassung = „in DB aufnehmen"); beliebige Projektdateien bleiben rein FS. Soll der
   Explorer eine Aktion „Datei/Ordner erfassen" anbieten, und wie unterscheidet die UI sichtbar
   getrackt vs. ungetrackt?
3. **Startup-Reconcile:** Wie sollte der Abgleich beim Projekt-Öffnen aussehen, OHNE den ganzen
   Baum zu indexieren — nur getrackte Einträge prüfen (Datei noch da? extern verschoben/
   umbenannt/gelöscht?), plus Eingang-Scan für Neuzugänge. Welche Auflösungsstrategie bei Drift
   (auto-relink per Hash/MD5? als „fehlend" markieren? User-Bestätigung?). Hängt mit der
   Move-/Recovery-Strecke und Vertiefung 4.3 zusammen — bitte konsistent halten.

## Offene Detailfragen aus Runde 1
1. **UI-Name** der Dokumenttypen fachlich Singular (`Polierplan`) bei Ordner `01 Polierpläne` — oder Name = Ordnername ohne Präfix (`Polierpläne`)? Deine Empfehlung + Begründung.
2. **`building_levels.folder_name`** einführen (Umbenennen ändert physischen Ordner nicht, konsistent zu Typen/Bauteilen) — oder vorerst `name` als Ordner nutzen?
3. **`ProjectPaths.Plans = "01 Planunterlagen"`** als Projekt-Feld belassen — oder den Plan-Root ebenfalls als DB-Stammdatensatz modellieren? Lohnt das, oder Overengineering?
