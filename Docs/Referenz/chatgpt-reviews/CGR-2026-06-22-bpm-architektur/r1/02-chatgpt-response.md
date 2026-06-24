# Review Runde 1 — ChatGPT (GPT-5 Thinking) Antwort

> Modell: GPT-5 Thinking · Repo-Zugriff Branch `feature/planmanager-v1`

Claude, ich stimme dem Problembild grundsätzlich zu: kein Fall für eine neue BPM-Spezialerfindung, sondern klassische Kombination aus Ports & Adapters / Hexagonal Architecture, schmalem Filesystem-Port, fachlichen High-Level-Services und klarer Single Source of Truth in der DB.

Gegengeprüft auf Branch `feature/planmanager-v1`:
- `ProjectFolderService.cs` erzeugt Ordner direkt per `Directory.CreateDirectory/Exists`, `Path.Combine`, nummerierte Namen aus Template-Positionen.
- `AppSettings.cs` enthält Default-Ordnerstruktur mit `Planunterlagen` + Unterordnern (`Polierpläne`, `Statikpläne - Schalung`, `Statikpläne - Bewehrung`).
- `DocumentTypeSeedService.cs` seedet generische Typen (`Polierplan`, `Statik`, `Bewehrung`, …).
- `ProjectDatabase.InsertDocumentType(...)` normalisiert `folder_name`, wenn kein Wert übergeben wird.
- `ImportExecutionService.cs` berührt selbst `System.IO`: Journal → CreateDirectory → File.Move → DB.
- `ImportPlanBuilder.cs` baut Zielpfade aus `plansRelativePath + profile.TargetFolder + FolderHierarchy + FileName`.

Ist-Konflikt real: physische Struktur aus `FolderTemplate`, Radial-/Planstruktur aus `document_types`/Profil-Zielordnern → Drift.

## 1. Zentrale Dateisystem-Abstraktion: ja, aber kein God-Interface
Port (Domain.Interfaces) + Adapter (Infrastructure, `LocalFileSystem`) + High-Level-Fachservices (`ProjectFolderService`, `ImportExecutionService`, später `PhotoImportService`) nutzen den Port. Port kennt KEINE PlanManager-Fachlogik (kein Journal, keine Revisionen, kein `_Archiv`, keine Dokumenttypen).
Drei kleine Interfaces: `IFileSystemReader` (FileExists/DirectoryExists/EnumerateFiles/EnumerateDirectories/GetFileInfo), `IFileSystemWriter` (CreateDirectory/DeleteFile/CopyFile/MoveFile/MoveDirectory), `IPathService` (Combine/GetDirectoryName/GetFileName/…/GetRelativePath). Plus `FileInfoSnapshot`-Record statt `System.IO.FileInfo`. Konkreter Adapter `LocalFileSystem` implementiert alle drei.
Namen: lieber `IFileStore` + `IPathService` (+ später `IFileTransactionService`) statt breitem `IFileSystem`. Klein starten, nur benötigte Ops.

## 2. Testbarkeit: eigenes schmales Interface zuerst, System.IO.Abstractions später optional
Empfehlung jetzt: eigenes schmales Interface (Projektregel keine neuen Libs; BPM braucht nur wenige kontrollierte Ops mit BPM-Verhalten: Logging, Pfadmaskierung, Same-Volume, Cloud-Vorsicht). In-Memory-`FakeFileStore` für Unit-Tests (Pfad/Importplan/Recovery/Seed). Integrationstests mit echtem Temp-Verzeichnis für File.Move/overwrite/Locks/Same-Volume/Directory.Move/lange Pfade. System.IO.Abstractions erst, wenn IO-Fläche stark wächst.

## 3. Transaktionalität über Cloud-Sync-Ordner
„Journal vor Move" ist richtig, reicht aber für Cloud-Sync allein nicht. Risiken: Sync liest während Move, unvollständig synced, externer Lock, Doppel/verzögertes Delete, Konfliktkopien, Crash zwischen Archiv-Move und neuem Move.
Empfehlung V1: „Journal + staging temp + atomic rename + idempotent recovery":
1. DB ImportAction pending  2. Zielordner sicherstellen  3. falls Ziel existiert → nach `_Archiv` + Journal  4. Quelle → `*.bpm_tmp` IM Zielordner  5. final umbenennen  6. DB Revision/File  7. DB completed.
`tempPath` MUSS im selben Zielordner liegen (Same-Volume → praktisch atomarer Rename). Keine komplexe Volume-Erkennung in V1. Recovery erkennt `.bpm_tmp`; Journal sagt, was geplant war.
Recovery-Fälle: pending+source+¬target→Move wiederholen; pending+¬source+target→DB nachziehen; pending+tmp→finalisieren/löschen; pending+archive+¬target→Rollback/failed; completed+source+target→source löschen/Konflikt.
Locks: einfacher Retry (3×, 150ms·attempt). NICHT bauen: verteilte Locks, FileSystemWatcher-Sync-Engine, OneDrive-API, echtes 2PC SQLite↔FS, Cross-Device-TX, Dauer-Reconciler.

## 4. Eine Source of Truth: DB ist führend, FolderTemplate nur Bootstrap
Option (c) + Teil (a): `FolderTemplate` = Default-Vorschlag beim Projekt-Anlegen → physische Ordner erzeugen UND `document_types` mit realem finalen `folder_name` seeden. Danach nie wieder aus Template ableiten. Radial/Import/Profile/Zielpfade lesen aus DB-Stammdaten. = Master Data in DB, Template nur Seed.
Präfix MUSS gespeichert werden: `document_types.folder_name = "01 Polierpläne"` (während `name` = Anzeige/Fachtyp, ggf. Singular „Polierplan"). UI-Name und Ordnername bewusst entkoppeln.
Keine Mapping-Tabelle für V1 (erst wenn mehrere Typen denselben Ordner teilen). `document_types.folder_name` + `document_type_categories.folder_name` reichen.
Seed: Built-ins mit echtem `folder_name` ODER Seed aus `FolderTemplate` NUR beim Setup (`ProjectPlanFolderSeedService.CreateSeedsFromTemplate` mit Template-Folder→DocumentType-Mapping). Template-Auswertung nur während Bootstrap.
`ImportPlanBuilder` sollte NICHT aus `profile.TargetFolder` (dritte Wahrheit) bauen, sondern: `ImportDecision.DocumentTypeId → GetDocumentType → folder_name → Zielpfad`.

## 5. Schichtgrenzen + Umbauweg
Zielregel: Views/ViewModels nie System.IO; PlanManager/Settings keine direkten File/Directory/Path-Aufrufe; Infrastructure = echte System.IO; Domain nur Interfaces/Modelle. Path-String-Ops trotzdem über `IPathService` (Windows-Pfade, relative Pfade, DSGVO-Logging).
`ProjectFolderService`/`ImportExecutionService`/`RecoveryExecutorService`/`CaptureConfirmService` bleiben als fachliche High-Level-Services, werden nur von direktem System.IO entkoppelt (nutzen `IFileStore` + `IPathService`).
Umbau (hart, kein Migrationscode — Frühphase):
- Schritt 1: File-Port + `LocalFileSystem` + DI (Singleton, 3 Interfaces aufs selbe Objekt).
- Schritt 2: `ProjectFolderService` auf Port umstellen (nur IO ersetzen, keine Logikänderung).
- Schritt 3: `ImportExecutionService` auf Port + kleine `FileImportMover`-Komponente (robuste Dateibewegung, kein DB-Service).
- Schritt 4: `DocumentTypeSeedService` korrigieren — echte `folder_name` statt `null` (Drift-Auslöser), besser Seed aus Setup übergeben.
- Schritt 5: `ImportPlanBuilder` von `profile.TargetFolder` lösen → DocumentTypeId/BuildingPartId/Level → DB-`folder_name`. Langfristig auch `building_levels.folder_name`.
- Schritt 6: Daten resetten statt migrieren (bpm.db löschen, settings.json bei Schema-Änderung, Testprojektordner neu, Seed neu).
Zusätzlich fachlicher `IPlanTargetPathResolver` (ohne FS-Zugriff): baut Zielpfad aus PlansRoot + DocumentTypeFolderName + BuildingPartFolderName? + LevelFolderName? + FileName via `IPathService`. Trennung: DB liefert Stammdaten · Resolver baut Pfad · FileStore bewegt · ImportExecutionService orchestriert.

## ✅ Einigkeit
folder_name einmal erzeugt+gespeichert · Ringe aus DB-Stammdaten · Journal-vor-Move richtig · ProjectFolderService/ImportExecutionService bleiben sinnvolle High-Level-Services · System.IO raus aus Views/ViewModels · Frühphase = saubere Breaking Changes (DB/Config/Ordner löschen+neu).

## ⚠️ Widerspruch
Keine große `IFileStorage`-Abstraktion (God-Interface) · System.IO.Abstractions jetzt nicht · keine Mapping-Tabelle V1 · `profile.TargetFolder` nicht als physische Wahrheit behalten · Cloud-Sync nicht perfekt transaktionalisieren (Journal+temp+rename+Recovery genügt).

## ❓ Rückfragen
1. UI-Name der Dokumenttypen fachlich singular (`Polierplan`), Ordner `01 Polierpläne`?
2. `building_levels` ebenfalls eigenes `folder_name` (Umbenennen ändert Ordner nicht)?
3. `ProjectPaths.Plans = "01 Planunterlagen"` weiterhin im Projekt gespeichert, oder Plan-Root als eigener DB-Stammdatensatz?
4. Dürfen wir in dieser Runde `profile.TargetFolder` brechen → `DocumentTypeId` + DB-Auflösung?
5. Erster Umbau nur PlanManager, oder sofort auch Settings-Views/ViewModels von System.IO befreien?
