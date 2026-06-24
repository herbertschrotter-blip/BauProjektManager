# Review Runde 4 (Abschluss) — ChatGPT (GPT-5 Thinking) Antwort

> **SIGN-OFF: Modell ist ADR-reif, keine konzeptionelle Blockade mehr.** Zwei Implementierungsrisiken als ADR-Invarianten festhalten: (1) Resolver darf keine halben Zielpfade erzeugen; (2) „+ Neu…" braucht klare Defaults, damit keine zweite Ordner-Wahrheit entsteht.
> Hinweis: `RadialSelectionController.cs` + `ProfileWizard*` waren per Repo-Suche nicht auffindbar → als Ziel-Schnittstellen behandelt (noch nicht gepusht / Slice 3a uncommitted).

## Teil A — Finale Validierung
**A1. „+ Neu…"-Typen:** Herberts Vorschlag tragbar, ABER **kleiner Pflichtdialog** statt blinder Defaults:
```
Name: [____]  Ablagebereich: [01 Planunterlagen ▼]  Unterteilung: [Bauteil/Geschoss | Kategorien | Keine]  Ordnername: auto aus Name, editierbar
```
Default Root `01 Planunterlagen`; `folder_name = NormalizeForFolderName(name)` **ohne Präfix** (Position unklar, Kollisionsgefahr mit Template-Nummern, kein heimliches Umsortieren); `key = NormalizeKey(name)`, nach Speichern gesperrt. Grund für Root-Auswahl: Multi-Root (Typ könnte unter 06 Protokolle/03 Leica/04 DOKA gehören).
- **+ Neu Bauteil:** `folder_name = NormalizeForFolderName(short_name)`, einmal erzeugt + gesperrt.
- **+ Neu Geschoss:** `folder_name = "{PrefixString} {Name}"` (z.B. `-01 KG`), rename-stabil.
- **+ Neu Kategorie:** `folder_name = NormalizeForFolderName(name)`, `HasPrefix=false` Default; optional „[ ] Kategorie nummerieren". ADR-Regel: Built-in-Kategorien präfixiert nach Template, User-Kategorien default unpräfixiert außer explizit bestätigt.

**A2. Cross-Root-Move** (Eingang `01 Planunterlagen/_Eingang` → `06 Protokolle/…`): bricht NICHTS — gleicher Projektroot = gleiches Volume, kein Cross-Volume-Move. „temp-im-Zielordner + atomic rename" bleibt korrekt; **atomar ist nur der finale Rename Temp→final im Zielordner**, nicht der Gesamttransfer. Ablauf: Quelle → `06 Protokolle/Baubesprechung/.datei.pdf.<importId>.tmp` → Hash/Size → atomic rename → DB/Journal. ADR-Invariante: Atomicity gilt für finalen Rename im Zielordner; Transfer aus Eingang ist journalisiert + recovery-fähig.

**A3. Modell A × Multi-Root:** Modell A unverändert. Reconcile prüft `plan_files.relative_path` (DB kennt konkreten Pfad, egal welcher Root) — keine Änderung. Eingang-Scan weiterhin nur globaler Eingang (asymmetrisch: Quelle = globaler Eingang, Ziel = typabhängiger Root). ADR-Regel: V1 = genau ein Import-Eingang, Ziele in mehrere Roots.

**A4. Risiko-Check (Slice 0 könnte beißen):**
- **R1** `folder_name=""` bei Root-Typen: Resolver muss `if(!IsNullOrWhiteSpace) parts.Add(folderName)`. DB-Check `CHECK(root_relative_path <> '')`. ADR-Invariante: folder_name leer nur wenn Root selbst der Typ ist.
- **R2** `UNIQUE(project_id, key)` Pflicht (sonst Profilimport wieder mehrdeutig). Key unveränderlich nach Anlage, ≠ UI-Name.
- **R3** Profile (`.bpm/profiles/*.json`) referenzieren projektlokale `DocumentTypeId`; bei bpm.db-Reset auch Profile löschen.
- **R4** Resolver braucht **IDs, nicht Namen** — Priorität: Id → key-exact → name/folder_name-exact → Fail-Fast. KEIN Fuzzy im Resolver (gehört in vorgelagerte Erkennung).
- **R5** `BuildingLevel.FolderName` muss ins Domain-Modell UND Load/SaveBuildingLevels UND Schnellanlage — sonst Ring 3 leer.
- **R6** `ProjectPaths` Defaults alt (`Plans="Pläne"`); minimal auf `01 Planunterlagen`/`01 Planunterlagen/_Eingang`/`06 Protokolle` setzen, besser aus FolderTemplate erzeugen. Resolver nutzt sie trotzdem nicht.

## Teil B — Slice-0-Implementierungstiefe
**B1. DocumentTargetPathResolver** in `src/BauProjektManager.PlanManager/Services/` (Import-/Erfassungslogik, hängt an ProjectDatabase; nicht Infrastructure, nicht Domain). Optional `IDocumentTargetPathResolver`.
Records:
```csharp
DocumentTargetRequest(string ProjectId, string DocumentTypeId, IReadOnlyDictionary<string,string> ExtractedFields, string FileName);
ResolvedDocumentTarget(string DocumentTypeId, string DocumentTypeKey, string DocumentTypeName, string RootRelativePath, string TypeFolderName, string TargetFolder, string RelativeDirectory, string RelativePath, string? BuildingPartId, string? BuildingLevelId, string? CategoryId);
```
`TargetFolder` = root + optional folder_name (`01 Planunterlagen/01 Polierpläne`, `06 Protokolle`). `RelativeDirectory` = vollständig ohne Dateiname. `RelativePath` = inkl. Datei.
Auflösung priorisiert per Id → key → exact-normalized-name → Fail-Fast. Bauteil: `building_part_id` > `building_part` (Rohwert). Geschoss: `building_level_id` > … Kategorie: `document_type_category_id`/`category_id` > … Primär über bekannte `SegmentTypeIds`.
**Fail-Fast:** BuildingParts braucht Bauteil + Geschoss; Categories braucht Kategorie; None nichts. Fehlt ein Ring-Wert → Exception (kein Teilpfad). Für UI später `DocumentTargetResolutionResult(Success, Target?, MissingFields, ErrorMessage)`; Slice 0 intern Exception.

**B2. Import-Break:**
- `RecognitionProfile`: `TargetFolder` entfernen, `SchemaVersion=5`, `DocumentTypeId` führend, `DocumentTypeName` nur Snapshot. Loader keine Legacy-Toleranz.
- `ImportDecision`: + `ResolvedDocumentTarget? ResolvedTarget` (zusätzlich zu `TargetRelativePath` für leichtere Test-Stabilität).
- `ImportPlanBuilder.BuildPlan(decisions, projectId)` (kein `plansRelativePath`): pro Decision Resolver aufrufen (außer Skip/Unknown/Conflict, außer Profil ohne DocumentTypeId), `decision with { TargetRelativePath=target.RelativePath, ResolvedTarget=target }`.
- `ImportExecutionService`: schreibt aus `decision.ResolvedTarget` → `ResolveOrCreateDocument(..., target.TargetFolder, target.RelativeDirectory, target.BuildingPartId, target.BuildingLevelId)`, `InsertFileForRevision(target.RelativePath)`. Kein `profile.TargetFolder` mehr.
- `ProfileWizard`: Dokumenttyp wählen (bestehend / + Neu) statt Zielordner; Profil speichert `DocumentTypeId`; Vorschau via Resolver, aber nicht gespeichert. Falls Wizard noch nicht im Code: Slice 0.6 nur Modell+Pipeline ändern, Wizard-Story im ADR.
- **Reihenfolge (jeder Zwischenstand baubar):** 1) additive Felder (Key/RootRelativePath/BuildingLevel.FolderName/ResolvedDocumentTarget/Resolver). 2) ProjectDatabase (Schema/InsertDocumentType/GetDocumentType/GetDocumentTypes). 3) Seed aus Template (_builtins raus). 4) ImportPlanBuilder nutzt Resolver (TargetFolder noch ungenutzt). 5) ImportExecutionService nutzt ResolvedTarget. 6) TargetFolder entfernen + SchemaVersion+ (Profile brechen → Reset). 7) Wizard/UI nachziehen.

**B3. Test-Strategie:** 0.1 Models (PrefixString/FolderName/Category.GetFolderName: `("Wände",true).GetFolderName(1)="01 Wände"`). 0.2 DB (Insert speichert key/root/folder; UNIQUE greift; Root-Typ folder_name="" gültig; building_levels.folder_name). 0.3 Seed (Fixture Default-Template → erwartete Typen/Kategorien; idempotent; Fail bei fehlendem Key/doppeltem Key/Categories ohne Kategorien). 0.4 Resolver-Unit (Polierplan/Fertigteil/Protokoll/Baustelleneinrichtung + alle Fehlerfälle Fail). 0.5 ImportPlanBuilder (Resolver-Aufruf, Skip kein Aufruf). 0.6 Temp-Integration Move (Protokoll cross-root, Polierplan mit Bauteil/Geschoss, Fehler-nach-Temp-vor-Final → Recovery).
**Erwartbar brechende Tests:** SchemaVersion==4, TargetFolder, BuildPlan-Signatur, alter Built-in-Seed, ProjectPaths.Plans="Pläne", building_levels ohne folder_name. Strategie: additive Felder zuerst → neue Tests → dann TargetFolder entfernen + Tests gezielt nachziehen. Keine Legacy-Kompatibilität.

**B4. Frühphasen-Reset (exakt):** 1) `%LocalAppData%\BauProjektManager\bpm.db` löschen. 2) `…\Projects\<ID>\planmanager.db` (oder ganzes `…\Projects\`). 3) `<ProjectRoot>\.bpm\profiles\*.json`. 4) optional `<ProjectRoot>\.bpm\plan-index.json`. 5) optional Testprojekt-Zielordner (`01 Planunterlagen\`, `06 Protokolle\`). 6) **`settings.json`** löschen, falls alte FolderTemplate ohne Typ-Metadaten konserviert (sonst seedet App keine Typen). ADR-Hinweis: settings.json + bpm.db + planmanager.db + .bpm/profiles gemeinsam zurücksetzen bei Template-/Typmodell-Änderung.

**B5. `PlanDocumentType`-Record:** + `Key`, `RootRelativePath`. `InsertDocumentType(projectId, key, name, rootRelativePath, folderName, ring2Source, sortOrder, colorHex=null, isBuiltin=false, id=null)` — defensiv: key/rootRelativePath required, `folderName ??= ""`. Kein stilles Normalisieren in der DB-Methode (gehört in Seed-/Creation-Services). `GetDocumentType(projectId, id)` (lädt Kategorien nur bei Categories), + `GetDocumentTypeByKey(projectId, key)`. `InsertDocumentTypeCategory(documentTypeId, name, folderName, sortOrder, id=null)`. Neuer `DocumentTypeCreationService.CreateUserDocumentType(...)` normalisiert key+folder, ruft InsertDocumentType (key danach gesperrt).

## ✅ Einigkeit
ADR auf Basis R3+R4 schreibbar · root_relative_path final · ProjectPaths.Plans nur Convenience · globaler Eingang funktioniert für Cross-Root-Ziele · temp+atomic-rename korrekt (atomar = finaler Rename) · Reconcile = getrackte Teilmenge unabhängig Multi-Root · TargetFolder entfernen · ResolvedDocumentTarget fließt durch ImportDecision · Resolver in PlanManager-Service · fehlende Ring-Werte Fail-Fast · Reset umfasst bpm.db+planmanager.db+.bpm/profiles+ggf. settings.json.

## ⚠️ Widerspruch
Vollständig unsichtbare Schnellanlage = nein (mind. Root + Unterteilungsart sichtbar) · stille Normalisierung in Low-Level-DB-Methode = nein (in Creation-/Seed-Services) · Resolver-Fallback auf Namen wenn IDs verfügbar = nein (Namen nur 2. Priorität) · Teilpfade bei fehlenden Werten = nein.

## ❓ Rückfragen
Keine blockierenden mehr. Nur eine ADR-Formulierungsentscheidung: Soll der ADR die „+ Neu…"-Schnellanlage als **MVP-Pflichtdialog** festschreiben oder als Default-Regel mit optionaler Root-Auswahl? Empfehlung: **MVP-Pflichtdialog, aber sehr klein.**
