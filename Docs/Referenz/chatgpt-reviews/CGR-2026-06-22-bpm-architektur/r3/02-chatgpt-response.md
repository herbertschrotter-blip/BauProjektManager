# Review Runde 3 — ChatGPT (GPT-5 Thinking) Antwort

> Repo-Zugriff Branch `feature/planmanager-v1`. Finale Modellierung Protokolle-Root-Typ + Multi-Root + Detail-Konsistenz.

Kernaussage: Mit „Protokolle = eigener Root-Typ" kippt R2-Punkt — `ProjectPaths.Plans` kann nicht mehr Universal-Root sein. Zielpfad-Wahrheit kommt künftig **pro Dokumenttyp** aus der DB.

## A. Hauptordner als Typ + Multi-Root
**A1.** `FolderTemplateEntry` bekommt dieselben optionalen Typ-Metadaten wie `SubFolderEntry` (`CreatesDocumentType`, `DocumentTypeKey`, `DocumentTypeDisplayName`, `Ring2Source?`, `Categories`). `DocumentTypeCategoryTemplateEntry` bekommt `HasPrefix` + optional `FolderName` + `GetFolderName(position)` (`{pos:D2} {Name}` wenn HasPrefix, sonst Name, oder expliziter FolderName).
- Hauptordner: immer nummeriert (Hauptordnerposition). Unterordner: optional (`HasPrefix`). Kategorien: optional (`HasPrefix`).

**A1.1 Protokolle-Default:** `FolderTemplateEntry("Protokolle", hasInbox:false){ CreatesDocumentType=true, DocumentTypeKey="protokoll", DocumentTypeDisplayName="Protokoll", Ring2Source=Categories, Categories=[Baubesprechung, Bautagesbericht, Sicherheit, Abnahme] (hasPrefix:false) }`. Seed → `root_relative_path="06 Protokolle"`, `folder_name=""`, Kategorien unpräfixiert (entspricht Herberts Beispiel `06 Protokolle/Baubesprechung/datei.pdf`). Präfix optional möglich.

**A2. `document_types.root_relative_path`** (empfohlen, NICHT `root_key` — präziser, offline-first-näher). Schema: `document_types` bekommt `key TEXT NOT NULL` + `root_relative_path TEXT NOT NULL` + `folder_name` (leer bei Root-Typ) + `UNIQUE(project_id, key)`.
- Beispiele: Polierplan → root `01 Planunterlagen`, folder `01 Polierpläne`. Protokoll → root `06 Protokolle`, folder ``. Künftig Leica/DOKA generalisierbar.
- **Seed-Regel:** Hauptordner-Typ: `root = main.GetNumberedName(mainIndex)`, `folder_name=""`. Unterordner-Typ: `root = parent.GetNumberedName(parentIndex)`, `folder_name = sub.GetDisplayName(subIndex)`. Regel: „`root_relative_path` = fachlicher Root/Container; `folder_name` = Typordner; wenn Root selbst der Typ ist, `folder_name` leer."

**A2.1 Resolver:** `root_relative_path / folder_name(if!=empty) / Ring2 / Ring3 / fileName`. Pseudocode `DocumentTargetPathResolver.Resolve(...)` mit Fail-Fast wenn Root/Bauteil/Geschoss/Kategorie fehlt. Ring2=BuildingParts → part.FolderName + level.FolderName; Categories → category.FolderName; None → nichts. Kein `ProjectPaths.Plans` mehr.

**A3. Präzedenz-Regel:** Node ist Container ODER Typ ODER beides — aber **nur explizit** via `CreatesDocumentType`. Keine implizite Ableitung aus HasInbox/HasPrefix/Categories/Name/Position. `DocumentTypeKey` Pflicht wenn `CreatesDocumentType=true`; `UNIQUE(project_id,key)`; Seed bricht bei Key-Kollision ab. Default: Planunterlagen=Container(false)+Typ-Subfolders; Protokolle=Typ(true) ohne Typ-Subfolders. Container+Typ technisch erlaubt, im Default vermeiden.

**A4. `ProjectPaths.Plans`:** bleibt als **Convenience/Navigation/Kompatibilität**, ist aber NICHT mehr Resolver-Input. `Inbox` sollte real `01 Planunterlagen/_Eingang` sein. Resolver nutzt ausschließlich `document_types.root_relative_path`. Jetzt löschen würde zu viel UI/Settings destabilisieren.

## B. Seed/Resolver-Konsistenz
**B1. Kategorien:** `HasPrefix` **je Kategorie** (nicht global) — max. Flexibilität, Protokolle unpräfixiert, Fertigteile präfixiert. Seed nutzt `category.GetFolderName(c+1)`. Fertigteile-Default mit `hasPrefix:true` → `01 Wände/02 Decken/03 Stiegen`.
**B2. `building_levels.folder_name = "{PrefixString} {Name}"`** bestätigt. Tabelle: `folder_name TEXT NOT NULL DEFAULT ''` ergänzen. Beispiele: -2/UG2→`-02 UG2`, -1/KG→`-01 KG`, 0/EG→`00 EG`, 1/OG1→`01 OG1`. Beim Anlegen erzeugen, `ON CONFLICT` lässt unangetastet (rename-stabil, analog building_parts). Domain `BuildingLevel.FolderName` ergänzen.
**B3. Pfad-Beispiele bestätigt:**
- Polierplan: `01 Planunterlagen / 01 Polierpläne / BT1 / -01 KG / datei.pdf`
- Fertigteile: `01 Planunterlagen / 04 Fertigteilpläne / 01 Wände / datei.pdf`
- Protokoll: `06 Protokolle / Baubesprechung / datei.pdf`

## Seed-Modell (final)
`SeedMainFolderType` + `SeedSubFolderType` + `InsertTypeAndCategories` + `EnsureUniqueKey`-Validierung. `EnsureSeeded` iteriert FolderTemplate mit Index `i` (Start bei 0 → `00 Sonstiges`, `01 Planunterlagen`, … `06 Protokolle`). Achtung: `GetNumberedName(i)` mit `i` (nicht `i+1`), damit Sonstiges=00.

## plan_documents.target_folder
Neue Semantik: `target_folder` = fachlicher Typ-Basispfad (`root_relative_path` + optional `folder_name`); `relative_directory` = vollständiges Zielverzeichnis (mit Ring2/3, ohne Dateiname). Bsp Polierplan: target_folder=`01 Planunterlagen/01 Polierpläne`, relative_directory=`…/BT1/-01 KG`. Langfristig besser `document_type_id` + `plan_files.relative_path`, target_folder nur Snapshot. Für Slice 0 reicht Semantik festziehen.

## Slice-0-Plan
- **0.1 Domain Models:** FolderTemplateEntry, SubFolderEntry, neuer DocumentTypeCategoryTemplateEntry, BuildingLevel.FolderName, PlanDocumentType um Key+RootRelativePath.
- **0.2 DB-Schema bpm.db:** document_types.key + root_relative_path, building_levels.folder_name. Frühphase: keine Migration, bpm.db löschen/Projekt neu.
- **0.3 ProjectDatabase:** InsertDocumentType um key/rootRelativePath; GetDocumentTypes lädt neue Felder; SaveBuildingLevels setzt folder_name; ON CONFLICT lässt unangetastet.
- **0.4 DocumentTypeSeedService:** `_builtins` raus; Seed aus FolderTemplate (Haupt+Unterordner-Typen, Kategoriepräfixe, Key-Validierung, Root/Folder-Berechnung).
- **0.5 Resolver-Vertrag:** DocumentTargetPathResolver + Tests (Polierplan/Fertigteile/Protokoll/Baustelleneinrichtung).
- **0.6 Import-Break:** RecognitionProfile.TargetFolder entfernen, SchemaVersion+; ImportPlanBuilder nutzt Resolver; ImportExecutionService schreibt DB aus ResolvedTarget.

## Verbleibende Lücke (für ADR benennen)
`Ring2Source` ist single-strategy (building_parts | categories | none). Reicht NICHT für kombinierte Hierarchien (z.B. Fertigteile → Kategorie UND Bauteil/Geschoss: `…/04 Fertigteilpläne/01 Wände/Haus A/00 EG/datei.pdf`). Post-V1: generisches `document_type_folder_segments` oder Enum-Erweiterung (`categories_then_building_parts`). **ADR-Grenze:** „V1 = genau eine Ring-2-Strategie pro Typ; kombinierte Hierarchien Post-V1."

## ✅ Einigkeit
Protokolle = Root-Typ · FolderTemplateEntry+SubFolderEntry gleiche Typ-Metadaten · DocumentTypeKey explizit in `document_types.key` · `root_relative_path` notwendig+richtig · folder_name leer bei Root-Typ · Kategorie-folder_name echt+präfixierbar (HasPrefix je Kategorie) · building_levels.folder_name=`{PrefixString} {Name}` rename-stabil · Resolver nutzt nur DB, nicht TargetFolder/Plans.

## ⚠️ Widerspruch
`root_key` als primär = nein (`root_relative_path` besser) · `ProjectPaths.Plans` löschen = nein (Convenience behalten, aus Resolver raus) · Hauptordner implizit als Typ = nein (nur explizit) · kombinierte Hierarchien jetzt = nein (Post-V1).

## ❓ Rückfragen
1. `document_types.key` für manuell angelegte User-Typen editierbar, oder einmal aus Namen erzeugt + gesperrt?
2. `Protokolle` eigener `_Eingang` (`06 Protokolle/_Eingang`), oder aller Import über `01 Planunterlagen/_Eingang`/globalen Eingang?
3. `Baustelleneinrichtung` unter `01 Planunterlagen / Baustelleneinrichtung / datei.pdf` (None, kein Ring2/3), oder langfristig eigener Root?
