# Review Runde 2 — ChatGPT (GPT-5 Thinking) Antwort

> Repo-Zugriff Branch `feature/planmanager-v1`. Gelesen: AppSettings, DocumentTypeSeedService, ProjectDatabase, ImportPlanBuilder, RecognitionProfile, ImportExecutionService, PlanManagerDatabase, ImportScanService, PlanManager.md, DB-SCHEMA.md.

Rahmen mitgetragen: **Template = nur Bootstrap; `document_types`/`document_type_categories` danach einzige fachliche Ordner-/Typ-Wahrheit.**

## 1. FolderTemplate als Typ-Quelle
**1.1 `SubFolderEntry` minimal erweitern** um: `bool CreatesDocumentType`, `string? DocumentTypeKey` (stabiler fachlicher Key, NICHT UI-Name/Ordnername), `string? DocumentTypeDisplayName` (UI-Name, optional), `Ring2Source? Ring2Source`, `List<DocumentTypeCategoryTemplateEntry> Categories` (Name + optional FolderName). `Name` bleibt Ordner-Basisname ohne Präfix; physischer Ordner weiter `GetDisplayName(index)`. Default-Template-Beispiel: Polierpläne→Polierplan(BuildingParts), Statikpläne-Schalung→Schalungsplan, …-Bewehrung→Bewehrungsplan, Fertigteilpläne→Fertigteilplan(Categories: Wände/Decken/Stiegen), Baustelleneinrichtung(None). Protokolle: entweder eigener Unterordner-Trick (Hauptordner+Subfolder gleichnamig, unschön) ODER `FolderTemplateEntry` bekommt dieselben Metadaten (sauberer, aber nicht „minimal"). Für R2: minimal — nur `SubFolderEntry` beschreibt Typen.

**1.2 Regel:** Unterordner → `document_type` **genau dann wenn `CreatesDocumentType==true`**. NICHT aus HasPrefix/Name/Position/Kategorien/Namensliste ableiten. `_Eingang`/`_Archiv`/reine Sammelordner = nein. Der hardcodierte `_builtins`-Array in `DocumentTypeSeedService` muss RAUS.

**1.3 Ring 2/3:** `BuildingParts` → Ring2 aus projektspez. `building_parts`, Ring3 aus `building_levels` (NICHT Template). `Categories` → aus `document_type_categories`. `None` → kein Ring2/3.

**1.4 Seed (Pseudocode):** `EnsureSeeded` liest `FolderTemplate` „Planunterlagen", iteriert SubFolders mit `CreatesDocumentType`, `folderName = sub.GetDisplayName(i+1)` (echter präfixierter Ordner!), `displayName = DocumentTypeDisplayName ?? DeriveSingularName(Name)`, `InsertDocumentType(...folderName...)`, bei Categories `InsertDocumentTypeCategory` mit echtem folder_name. Fail-Fast wenn Key/Ring2Source fehlt. Nur beim Setup/erstem Zugriff; danach DB führend; keine Template-Resync; Frühphase = DB neu erzeugen statt Migration.

## 2. profile.TargetFolder brechen? → JA, jetzt
**2.1** Heute baut `ImportPlanBuilder`: `plansRelativePath + profile.TargetFolder + FolderHierarchy + fileName` = die alte zweite Wahrheit; `ImportExecutionService` schreibt `profile.TargetFolder` in `plan_documents.target_folder`. Zielbild: `recognized file → RecognitionProfile.DocumentTypeId → document_types.folder_name → optional Ring2/Ring3/Kategorie → Zielpfad`. Profil-Import UND Radial nutzen denselben `DocumentTargetPathResolver`.

**2.2 Migrationsfreier Umbau:** `RecognitionProfile`: `TargetFolder` entfernen, `DocumentTypeId` bleibt führend, `DocumentTypeName` nur Snapshot, optional `DocumentTypeCategoryId?` (aber Kategorie besser aus Segment-Erkennung). Profil-JSONs: löschen/neu anlernen (keine Migration). `ImportPlanBuilder`: ruft `DocumentTargetPathResolver.Resolve(projectId, planRoot, documentTypeId, extractedFields, fileName)` — switch über Ring2Source (BuildingParts → part.folder_name + level.folder_name; Categories → category.folder_name; None →). `ImportExecutionService`: keine Pfadrekonstruktion mehr, `ImportDecision` bekommt `ResolvedDocumentTarget`-Record (Ids + RelativeDirectory + RelativePath). `ProfileWizard`: speichert keinen Zielordner mehr, sondern wählt/erzeugt Dokumenttyp.

**2.3 Risiko:** jetzt brechen = mehr Umbau, Profil-JSONs/Tests brechen, ABER keine Produktivdaten, Schema eh in Bewegung, saubere Semantik, keine spätere Migration. Später = zwei produktive Pfadmodelle, target_folder in Profile/Docs/Journal, „Plan in A, DB sagt B"-Risiko. **Empfehlung: jetzt brechen.**

## 3. Voller System.IO-Scope — Slices (kein Monster-PR)
- **Slice 0** — Port-Verträge (`IFileSystemReader`/`IFileSystemWriter`/`IPathService`) + `LocalFileSystem` + Fake + 1 Temp-Integrationstest.
- **Slice 1** — reine Scanner/Reader (`ImportScanService`, Hash/MD5, Profil-Lader). Gut fakebar.
- **Slice 2** — reine Pfadberechnung (`ImportPlanBuilder`, neuer `DocumentTargetPathResolver`). Keine echten Moves; deterministische Pfad-Tests (BuildingParts/Categories/None/Fail-Fast).
- **Slice 3** — transaktionaler Import/Move/Archiv (`ImportExecutionService`). **Hochrisiko.** Op: Journal pending → Zielordner → target.tmp → Hash/Size-Check → atomic rename → DB → completed. temp IM Zielordner. Temp-Integrationstests inkl. Fehler vor/nach Move/DB, Recovery idempotent, Undo, SkipIdentical.
- **Slice 4** — `PlanManagerDatabase`/`ProjectDatabase` Pfadzugriffe (nur Pfad/Ordneranlage auf Port, NICHT die SQLite-Connection).
- **Slice 5** — Settings-Views/ViewModels + `ProjectFolderService` (ViewModel-Tests, Fake-FS).
- **Slice 6** — In-App-Explorer (erst nach stabilen Ports!).
- **Höchstes Risiko:** ImportExecutionService > Undo/Recovery > DocumentTargetPathResolver > Settings-Views > Scanner. Pro Slice erst 346 Tests grün halten, dann neue ergänzen; kein Slice fasst zwei Risikozonen an.

## 4. In-App-Explorer
**4.1** FS-Ports reichen, wenn Reader auch Verzeichnisse + Metadaten kann (`EnumerateDirectories/Files`, `GetFileInfo/GetDirectoryInfo`, Exists). `FileInfoSnapshot` mit Name/FullPath/RelativePath/Extension/Length/LastWriteTimeUtc/IsReadOnly. Lazy-Loading je Ebene, `HasChildren`-Schnellcheck.
**4.2** Eigener `IFileLauncher` (OpenFile/OpenFolder/RevealInExplorer/CopyPathToClipboard) — ShellExecute NICHT in File-Port. **Teilen MVP:** Pfad kopieren / im Explorer zeigen / öffnen / Ordner öffnen / optional Mail-Anhang. **Später:** Win-Share-Sheet, Cloud-Links. **Cloud-Share-Links bewusst out-of-scope** (Provider-APIs, Online, DSGVO, Berechtigungen). Cloud-neutral bleiben.
**4.3 Konsistenz:** Ungetrackte Dateien: Explorer darf move/delete. **Getrackte Plandateien: NICHT direkt move/delete** — nur öffnen/zeigen/Pfad kopieren/„über PlanManager verschieben" (Option b + Weg zu a). Tracked-Move = Journal+DB+Revision/File-Link, also Import-/Archiv-Logik, nicht Explorer. Unterscheidung via DB-Lookup (plan_files↔revisions↔documents). Badges: erfasst/Datei/fehlt/geändert. Tracked-Move später über `PlanFileMoveService`, nicht direkt `IFileSystemWriter`.
**4.4 Fremdänderungen:** Dauer-`FileSystemWatcher` weiter NEIN (Cloud-Sync unzuverlässig: Doppel-/fehlende Events, Locks, Rename=Delete+Create). On-demand-Reconcile bei Projektöffnen + Button „Projektordner prüfen" + nach Import/Undo. Optional späterer Lightweight-Watcher nur als UI-Hinweis.

## 5. DB-Scope & Startup-Reconcile
**5.1 Modell A** (klar): DB = kuratierter Index bewusst erfasster Plandokumente; Explorer liest FS live; Reconcile nur getrackte Teilmenge. B (Vollspiegel) zu schwer (jede Temp-/Lock-/Sync-Datei, Konfliktexplosion, teurer Startup, „halbes DMS"). C (Hybrid) gefährlich (Planunterlagen enthält auch ungetrackte Hilfsdateien → Teil-Vollindex, unscharfe Grenze).
**5.2** Nur bewusst erfasste Plandokumente in DB (Profil-Import / Radial / Explorer-„erfassen" / später Bulk). Explorer bietet „Datei erfassen" (öffnet Capture-Workflow). Badges: Erfasst/Nicht erfasst/Fehlt/Extern geändert/Doppelt.
**5.3 Reconcile ohne Vollindex:** getrackte plan_files laden → pro Eintrag Exists + Size prüfen (Hash nur bei Bedarf) → Status OK/MissingOnDisk/ChangedOnDisk/RelinkCandidate → Eingang-Scan für Neue → KEIN Vollscan. Auto-Relink per MD5 nur als **Vorschlag** (nicht automatisch — Kopien!). MissingOnDisk = markieren nicht löschen; ChangedOnDisk = Warnung; RelinkCandidate = User bestätigt; Delete = erst nach bewusster Aktion soft-delete.

## 6. Detailfragen aus Runde 1
**6.1** `name` = fachlicher Singular/UI-Name (`Polierplan`), `folder_name` = physisch (`01 Polierpläne`). NICHT „Ordnername ohne Präfix" als UI-Name. Trennung Anzeige/Klassifikation vs. Ablagepfad (wie CDE/DMS). Seed-Fallback nur wenn DisplayName fehlt.
**6.2** `building_levels.folder_name` einführen (JA) — heute inkonsistent (parts haben folder_name, levels nicht). Frühphase: Schema neu. Speicherregel analog parts (einmal erzeugen, rename-stabil). Empfehlung: kurz/sprechend `EG`/`OG1`, NICHT `01 EG` — Sortierung steckt in sort_order/prefix.
**6.3** `ProjectPaths.Plans = "01 Planunterlagen"` belassen (= `projects.plans_path`). Kein eigener Plan-Root-Stammdatensatz jetzt (Overengineering). Erweiterung via `document_types.root_relative_path` erst, wenn mehrere Planbereiche (Leica/DOKA) nötig.

## 7. Zielarchitektur
FolderTemplate → Bootstrap → document_types/categories. Danach DB-führend (alle folder_name = real). RecognitionProfile erkennt → DocumentTypeId + Segmentwerte → `DocumentTargetPathResolver`. Radial → DocumentTypeId + Ringwerte → derselbe Resolver. Import/Move → Journal pending → temp-im-Zielordner → atomic rename → DB → completed → Recovery idempotent. Eine Wahrheit: Struktur initial aus Template, ab Projektanlage DB-führend, Profile erkennen aber besitzen keine Zielordner, Explorer liest live + DB trackt nur bewusst Erfasstes.

## ✅ Einigkeit
3 schmale FS-Ports + LocalFileSystem; kein System.IO.Abstractions · DB nach Bootstrap einzige Ordner-Wahrheit · folder_name = echter präfixierter Ordner · kein hardcodierter Built-in-Seed · Ring2/3 aus building_parts/levels · Categories aus document_type_categories · Explorer darf getrackte Dateien nicht unkontrolliert bewegen · kein DB-Vollindex · Reconcile nur getrackt + Eingang.

## ⚠️ Widerspruch
profile.TargetFolder weiterleben = nein (jetzt brechen) · freier Explorer-Move getrackter Dateien = nein · Cloud-Share-Links im MVP = nein · DB-Vollspiegel = nein · automatische Ableitung „Ordnername ohne Präfix = UI-Name" = nein.

## ❓ Rückfragen
1. `Baustelleneinrichtung` erfassbarer Dokumenttyp oder nur manueller Ablageordner?
2. `Protokolle` als Dokumenttyp unter `Planunterlagen` oder eigener Root-Bereich außerhalb?
3. Kategorieordner nummerierbar oder immer unpräfixiert?
4. `DocumentTypeKey` explizit ins Template (Empfehlung) oder aus normalisiertem Anzeigenamen?
5. `building_levels.folder_name` als `EG`/`OG1` oder nummeriert `04 EG`? (Empfehlung: ohne Nummer)
