## Review Runde 4 (Abschluss)

Letzte Runde, Kollege — danach schreiben wir den ADR. Zweiteilig: (A) finale Validierung des
Gesamtmodells vor dem ADR, (B) Slice-0-Implementierungstiefe, damit der erste Code sauber landet.
GESAMTE Antwort in den Canvas, Titel "Review Runde 4", schließe mit
✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

## Repo-Zugriff
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch! Bei JEDEM Dateizugriff angeben.
- Relevant: `AppSettings.cs`, `ProjectDatabase.cs`, `DocumentTypeSeedService.cs`, `ImportPlanBuilder.cs`,
  `ImportExecutionService.cs`, `RecognitionProfile.cs`, `PlanManagerDatabase.cs`, `ImportScanService.cs`,
  `RadialSelectionController.cs` (Radial „+ Neu…"), `ProjectPaths.cs`, `BuildingLevel.cs`.

## Entschieden (final — nicht neu aufrollen)
- Modell aus Runde 3 steht: `document_types.key` + `root_relative_path` + `folder_name` (leer bei Root-Typ);
  `FolderTemplateEntry`+`SubFolderEntry` mit Typ-Metadaten; Kategorie-`HasPrefix` je Kategorie;
  `building_levels.folder_name = "{PrefixString} {Name}"`; `DocumentTargetPathResolver` nur aus DB;
  `ProjectPaths.Plans` bleibt Convenience, raus aus Resolver. Post-V1: eine Ring-2-Strategie pro Typ.
- **`document_types.key` für User-Typen:** beim Anlegen einmal aus dem Namen normalisiert + danach gesperrt.
- **Ein globaler Eingang** `01 Planunterlagen/_Eingang`; Erfassung ordnet Typ zu (auch Protokoll → `06 Protokolle/…`).
- **Baustelleneinrichtung:** `01 Planunterlagen/Baustelleneinrichtung/datei.pdf` (None).

## Teil A — Finale Validierung vor ADR
1. **„+ Neu…"-Typen (Radial-Schnellanlage):** Built-ins kommen aus dem Template. Wenn der User mitten in
   der Erfassung einen NEUEN Dokumenttyp anlegt — welchen `root_relative_path` und `folder_name` bekommt er?
   Vorschlag: Default-Root = `01 Planunterlagen`, `folder_name = NormalizeForFolderName(name)` (ohne Präfix,
   weil Position unklar). Ist das tragbar, oder brauchen wir eine explizite Root-/Ordnerwahl im „+ Neu…"-Dialog?
   Gilt dasselbe für „+ Neu…" Bauteil/Geschoss/Kategorie (folder_name-Erzeugung)?
2. **Cross-Root-Move:** Der globale Eingang liegt unter `01 Planunterlagen/_Eingang`, ein erfasstes Protokoll
   landet in `06 Protokolle/…`. Der Move kreuzt also Root-Ordner (gleicher Projektroot, gleiches Volume).
   Bricht das die „temp-im-Zielordner + atomic rename"-Strategie? Bestätige, dass Same-Volume-Move über
   Root-Grenzen hinweg unkritisch ist.
3. **Modell A × Multi-Root:** Reconcile/getrackte Teilmenge liegt jetzt über mehrere Roots verteilt
   (`01 Planunterlagen`, `06 Protokolle`, …). Ändert das den Startup-Reconcile oder den Eingang-Scan?
4. **Vollständigkeits-/Risiko-Check:** Was würde in Slice 0 beißen, was fehlt im Modell noch (Felder,
   Invarianten, Edge-Cases), bevor wir den ADR festschreiben? Bitte schonungslos.

## Teil B — Slice-0-Implementierungstiefe
1. **`DocumentTargetPathResolver` — finale Signatur + Heimat:** In welcher Schicht/Projekt liegt er
   (Infrastructure? PlanManager?)? Wie werden `extractedFields` auf Bauteil/Geschoss/Kategorie aufgelöst —
   per Id, Key oder Name-Match? Wie verhält er sich bei fehlendem Ring-Wert (Fail-Fast vs. Teilpfad)?
2. **Import-Break (Slice 0.6) konkret:** Welche exakten Änderungen an `RecognitionProfile` (Feld weg,
   SchemaVersion+), `ImportPlanBuilder` (Resolver-Aufruf), `ImportExecutionService` (schreibt DB aus
   `ResolvedDocumentTarget`), `ProfileWizard` (Typwahl statt Ordnerwahl)? Wie fließt `ResolvedDocumentTarget`
   durch `ImportDecision`? Reihenfolge, damit jeder Zwischenstand baubar bleibt.
3. **Test-Strategie pro Slice:** Welche Tests konkret (Resolver-Unit-Tests mit Stamm­daten-Fixtures;
   Seed-Tests Template→DB; Temp-Integrationstest für Move)? Wie halten wir die bestehenden 346 grün,
   wenn `RecognitionProfile.TargetFolder` und der Seed sich ändern (welche Tests brechen erwartbar,
   wie ziehen wir sie nach)?
4. **Frühphasen-Reset — exakte Liste:** Was genau löscht Herbert vor dem ersten Lauf des neuen Modells
   (`bpm.db`? Profil-JSONs unter welchem Pfad? Testprojekt-Ordner? `planmanager.db`)? Bitte konkrete Reset-
   Anleitung, keine Migration.
5. **`PlanDocumentType`-Modell + `ProjectDatabase`-API:** Welche Signatur bekommt `InsertDocumentType`
   nach Hinzunahme von `key` + `root_relative_path` (Reihenfolge der Parameter, Defaults), und wie sieht
   `GetDocumentType(projectId, id)` für den Resolver aus?

## Ziel
Nach dieser Runde: Modell final + Slice 0 klar genug zum Loslegen. ADR-060 schreibe ich danach.
Wenn alles passt, sag das klar (Sign-off), sonst benenne die letzte Lücke.
