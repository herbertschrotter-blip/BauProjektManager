# Review Runde 2 — ChatGPT-Antwort

**Modell:** GPT-5.4
**Datum:** 2026-05-18
**Canvas-Titel:** Review Runde 2

---

Kollege, ich bin mit der Richtung nach den User-Entscheidungen grundsätzlich zufrieden. Die Architektur wird damit klarer: Built-ins sind editierbare Katalogeinträge mit stabiler fachlicher Rolle, Custom-Typen sind reine UI-/Profilklassifikation ohne fachliche Sonderlogik. Das ist konsistent, solange wir die Grenze hart halten.

Vorab eine Korrektur zum Repo-Zugriff: Der im Prompt genannte Full Name `SchrotterHerbert/BauProjektManager` war über den Connector nicht abrufbar. Zugreifbar ist weiterhin `herbertschrotter-blip/BauProjektManager`; ich habe dort den Branch `feature/planmanager-v1` gelesen. Inhaltlich bestätigt der Stand die Runde-1-Diagnose: `ProfileWizardViewModel`, `FileNameSegment`, `RecognitionProfile` und `ProfileManager` hängen noch am alten `FieldType`-/Schema-v3-Modell.

## 1. User-Entscheidungen: Bewertung

### 1.1 Built-ins voll editierbar: ja, aber nur Display-/UX-Felder

Einverstanden. Built-ins dürfen umbenannt, umgefärbt, sortiert und deaktiviert werden. Das ist kompatibel mit dem Gesamtkonzept, wenn die fachliche Identität unveränderlich bleibt.

**Unveränderlich bei Built-ins:**

```text
id
semantic_role
token_key?  // siehe Einschränkung unten
is_builtin
is_required, falls Rolle PlanNumber
```

**Editierbar bei Built-ins:**

```text
name
color
group_id
sort_order
is_active
```

Die Ownership-Flags sind dafür richtig:

```sql
builtin_version INTEGER NOT NULL DEFAULT 1,
user_modified_name INTEGER NOT NULL DEFAULT 0,
user_modified_color INTEGER NOT NULL DEFAULT 0,
user_modified_sort INTEGER NOT NULL DEFAULT 0,
user_modified_active INTEGER NOT NULL DEFAULT 0
```

Ich würde zusätzlich `user_modified_group INTEGER NOT NULL DEFAULT 0` ergänzen. Sonst kann ein App-Update die Gruppe eines Built-ins wieder zurücksetzen, obwohl der User es bewusst umsortiert/umorganisiert hat.

Seed-Regel:

```sql
UPDATE segment_types
SET name = @defaultName
WHERE id = @id AND is_builtin = 1 AND user_modified_name = 0;

UPDATE segment_types
SET color = @defaultColor
WHERE id = @id AND is_builtin = 1 AND user_modified_color = 0;

UPDATE segment_types
SET group_id = @defaultGroupId
WHERE id = @id AND is_builtin = 1 AND user_modified_group = 0;
```

Wichtig: `token_key` bei Built-ins nicht editierbar machen. Sonst brechen `renameSchema`, `folderHierarchy` und ggf. alte Profilreferenzen. Name darf „Plan-Nr." heißen, aber Token bleibt `{plan_number}`.

### 1.2 Custom rein dekorativ: ja, das ist die richtige Frühphasen-Grenze

Einverstanden. Custom-Typen immer `semantic_role = NULL`. Damit bleiben sie:

* auswählbar in Schritt 2,
* renderbar/färbbar,
* im Profil persistierbar,
* aber nicht identitätsbildend durch implizite Regeln,
* nicht Index-/PlanNumber-/Datum-Trigger,
* nicht automatisch Hierarchie-Kandidat.

Das verhindert die gefährliche Erwartung „ich lege einen Custom-Typ Geschoss2 an und plötzlich versteht der Import Fachlogik". Wenn ein User fachliche Semantik braucht, muss er einen Built-in umbenennen. Das ist sauber.

Ein Folgeproblem bleibt: Wenn Built-ins voll editierbar sind, könnte der User `Geschoss` in „Akustik-Klasse" umbenennen. Fachlich bleibt es trotzdem `SemanticRole.Spatial`. Das ist erlaubt, kann aber verwirrende Identity-Wirkung haben. Deshalb braucht der Manager bei Built-ins eine read-only-Rollenanzeige mit Warntext:

```text
Fachrolle: Räumlich — wird automatisch in die Dokument-Identität aufgenommen.
```

## 2. Rückfragen aus Runde 1

### 2.1 IdentityFields implizit aus SemanticRole: reicht, mit zwei Korrekturen

Der Vorschlag ist für die Frühphase tragfähig:

```text
identityFields = [
  "documentType",
  ...segments mit SemanticRole == PlanNumber,
  ...segments mit SemanticRole == Spatial
]
```

**Edge-Case: zwei Spatial-Segmente**

Geschoss + Bauteil ist kein Problem, sondern sogar der Normalfall. Beide in die Identity aufzunehmen reduziert false positives. Beispiel:

```text
documentType + plan_number + geschoss + bauteil
```

Das ist strenger und fachlich sicherer als nur `plan_number`. Das Risiko liegt eher anders herum: Wenn ein Profil ein räumliches Segment enthält, das nicht identitätsbildend sein soll, gibt es ohne UI-Häkchen keine Möglichkeit, es auszuschließen.

Für die Frühphase akzeptabel, weil Custom keine SemanticRole bekommt und Spatial-Built-ins bewusst fachliche Raumdimensionen sind.

**Korrektur 1: Nur tatsächlich im Profil verwendete Segmente aufnehmen**

Nicht alle aktiven Spatial-Typen, sondern nur Profilsegmente:

```csharp
var identityFields = new List<string> { "documentType" };
identityFields.AddRange(profileSegments
    .Where(s => s.SemanticRole is SegmentSemanticRole.PlanNumber or SegmentSemanticRole.Spatial)
    .OrderBy(s => s.Position)
    .Select(s => s.FieldTypeId));
```

**Korrektur 2: `PlanNumber` muss genau einmal vorkommen**

```csharp
private bool ValidateStep2()
{
    var planNumberCount = Segments.Count(s => HasRole(s, SegmentSemanticRole.PlanNumber));
    return Segments.Count > 0 && planNumberCount == 1;
}
```

Wenn später Mehrfach-Plannummern unterstützt werden, ist das ein eigener UX-Use-Case. Für V1 verhindert `== 1` unsaubere Identity.

**Brauchen wir ein UI-Häkchen?**

Jetzt noch nicht. Datenstruktur so lassen, dass `IncludeInIdentity` weiterhin im Profil gespeichert wird. Der Wizard setzt es automatisch. Später kann die UI dieses Flag sichtbar/editierbar machen, ohne Schemawechsel.

```text
includeInIdentity wird weiterhin persistiert.
BPM-108 setzt es automatisch aus SemanticRole.
Kein UI-Override in Frühphase.
```

### 2.2 `recognition_profiles`-Tabelle nicht in BPM-108: ja, aber Profil-v4 muss syncfähig vorbereitet sein

Einverstanden: BPM-108 soll `recognition_profiles` nicht implementieren. Sonst explodiert der Scope.

Aber BPM-108 muss das spätere DB-Mapping vorbereiten, indem das JSON-Schema v4 bereits DB-taugliche IDs verwendet:

```json
{
  "schemaVersion": 4,
  "segments": [
    { "position": 1, "fieldTypeId": "plan_number", "required": true, "includeInIdentity": true }
  ],
  "identityFields": ["documentType", "plan_number"],
  "folderHierarchy": ["geschoss"],
  "renameSchema": "{plan_number}-{plan_index}_{geschoss}",
  "recognition": [
    { "method": "segment", "pattern": "S", "segmentPosition": 0 }
  ]
}
```

Das spätere Wandern in eine DB-Tabelle wird dann ein Persistenzort-Wechsel, kein Fachmodell-Wechsel.

### 2.3 Profile-Löschskript: ja, aber als explizites DevTool/Setup-Kommando, nicht als Loader-Automatik

Ich widerspreche der strikten Variante „User muss alles selbst aufräumen" teilweise. Kein Migration-Code heißt nicht, dass wir keine Reset-Hilfe bauen dürfen.

**Nicht bauen:**
* Kein automatisches v3→v4-Konvertieren.
* Kein Loader, der alte Profile toleriert.
* Kein stilles Verschieben beim normalen App-Start.

**Bauen darfst du:** Ein expliziter Reset-/Cleanup-Befehl, der alte Profile archiviert oder löscht.

```text
DevTools / Setup-Skript: „Alte PlanManager-Profile archivieren"
- scannt <project>/.bpm/profiles/*.json
- liest nur schemaVersion
- verschiebt schemaVersion != 4 nach <project>/.bpm/profiles/_archiv/schema-v3-reset-YYYYMMDD-HHMMSS/
- schreibt Log-Eintrag
```

Pseudocode:

```csharp
public int ArchiveOutdatedProfiles(string projectRoot)
{
    var profilesDir = Path.Combine(projectRoot, ".bpm", "profiles");
    var archiveDir = Path.Combine(profilesDir, "_archiv", $"schema-reset-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

    var moved = 0;
    foreach (var file in Directory.GetFiles(profilesDir, "*.json"))
    {
        var version = ReadSchemaVersionOnly(file);
        if (version == 4) continue;

        Directory.CreateDirectory(archiveDir);
        File.Move(file, Path.Combine(archiveDir, Path.GetFileName(file)));
        moved++;
    }
    return moved;
}
```

Der normale `ProfileManager.Load` bleibt strikt: `schemaVersion != 4` → verwerfen + klare Fehlermeldung.

## 3. Offene Detailpunkte A–E

### A. `lastKnownLabel` im Profil-JSON: nein in Frühphase

Ich stimme deiner Tendenz zu: Kein `lastKnownLabel` in BPM-108.

Gründe:
* Zweite Wahrheitsquelle.
* JSON-Profile länger, Fall selten.
* Missing-ID besser als harter UI-Hinweis „Unbekannt ()" statt veraltetem Namen.
* Wenn Sync den Segmenttyp später liefert, Rendering automatisch wieder korrekt.

Falls später Supportfälle es einfordern, kann `lastKnownLabel` additiv in v5 kommen. Jetzt nicht.

### B. Custom-Chip „+ Eigenes": Inline-Schnellanlage plus Link zum Manager

Variante 1 nehmen, bewusst minimal halten.

Flow in Schritt 2:

```text
Klick „+ Eigenes"
→ kleines Inline-Popover, nicht Fullscreen-Manager
→ Name eingeben
→ Farbe aus 12er-Palette oder Default
→ Speichern
→ neuer Custom-Segmenttyp in Gruppe „Eigene" / „Sonstiges"
→ Chip erscheint sofort und wird optional direkt dem aktuell markierten Segment zugewiesen
→ Hinweis: „Gruppe und Sortierung kannst du im Segmenttypen-Manager ändern."
```

Service-Schnitt:

```csharp
Task<SegmentTypeDefinition> CreateCustomQuickAsync(
    string name, string color, string groupId = SegmentTypeBuiltins.CustomGroupId);
```

Validierung:
* Name nicht leer.
* `token_key` aus Name generieren, Konflikt mit Suffix lösen.
* `semantic_role = null`.
* `is_builtin = 0`.
* `sort_order = max(custom group) + 10`.

### C. Wizard Schritt 5: keine Massenreparatur dort

Im aktuellen Code ist Schritt 5 „Erkennung", nicht „Indizes verwalten". BPM-108 betrifft Schritt 5 vor allem Rendering und Variable-Segment-Heuristik.

Was BPM-108 in Schritt 5 tun muss:
* Recognition bleibt `method/pattern/segmentPosition`.
* Segment-Blocks rendern über Segmenttyp-Katalog.
* Variable-Warnung nutzt `SemanticRole` statt `FieldType`.
* Missing-ID anzeigen, aber nicht für Recognition blockieren.
* Save blockieren, wenn Missing-ID in Identity/Hierarchy/Rename relevant.

**Keine „Profile reparieren"-Massenaktion in Schritt 5.** Besser:
* Profilübersicht zeigt Health-Badge: „3 Profile fehlerhaft".
* Segmenttypen-Manager oder DevTools bekommt Filter „Profile mit fehlenden Segmenttypen".
* Wizard kann ein einzelnes Profil reparieren.

### D. Domain-Konstanten-Migration: deine 5 Stellen sind richtig, aber nicht vollständig

Zusätzliche Stellen:

6. **`FileNameSegment` selbst** — `FieldType? FieldType` und `CustomFieldName` raus. `string? FieldTypeId` rein. `DisplayName` darf nicht mehr im Domain-Modell über UI-Katalog auflösen.
7. **`ProfileSegment` in `RecognitionProfile.cs`** — `FieldType` → `FieldTypeId`. `Label` entfernen oder nur als optionaler UI-Override vermeiden. Ich würde `Label` entfernen.
8. **`IdentityFields` Schreibweise** — Weg von `planNumber`/`plannumber`/`ToLowerInvariant()`. Hin zu stabilen IDs: `plan_number`, `haus`, `bauteil`.
9. **`IndexExtractionConfig.SegmentSelector`** — referenziert `fieldTypeId`, also `plan_number`.
10. **`PatternTemplateService`** — Profile werden dorthin extrahiert. Wenn Templates Segmenttypen enthalten, müssen sie v4/IDs verstehen oder bewusst nach BPM-108 deaktiviert/angepasst werden.
11. **Tests** — Alle Tests, die `FieldType.PlanNumber` etc. verwenden, müssen auf Seed-Katalog oder Test-Katalog umgestellt werden.
12. **XAML Bindings** — Bindings auf `FieldType`, `DisplayName`, `CustomFieldName`, `FieldTypeOptions.Value` brechen. BPM-108 braucht ein konkretes UI-DTO, nicht direkte Domain-Entities.
13. **Dokumentation PlanManager.md Kapitel 13/14** — Schema v3 muss auf v4 aktualisiert werden. Reset-Hinweis aktualisieren. `fieldType`-Beispiele ersetzen.

Konkretes Refactor-Schema in den Task aufnehmen: Ja. Die 13 Punkte als Akzeptanzkriterien aufnehmen.

### E. Deaktivierte Typen in Schritt-2-Chip-Liste: verborgen, aber Kontextbanner anzeigen

In der unteren Drag-Quellen-Liste nur `GetEffectiveActive()` zeigen.

Für bestehende Profilsegmente:
```text
Token oben: normal farbig + Badge „deaktiviert"
Chip-Liste unten: nicht sichtbar
Aktion am Token: Reset oder aktiven Typ neu zuweisen
```

**Kontextbanner**, sobald das Profil inaktive/deleted Typen enthält:

```text
Dieses Profil verwendet deaktivierte Segmenttypen. Sie bleiben für dieses Profil erhalten, können aber nicht neu zugewiesen werden.
```

Bei Missing-ID:

```text
Dieses Profil referenziert unbekannte Segmenttypen. Auto-Import ist blockiert, bis das Profil repariert wurde.
```

## 4. Weitere Architektur-Entscheidungen

### 4.1 SegmentSemanticRole genauer schneiden

```csharp
public enum SegmentSemanticRole
{
    None = 0,
    PlanNumber = 1,
    PlanIndex = 2,
    ProjectNumber = 3,
    Date = 4,
    Description = 5,
    Spatial = 6,
    Ignore = 7
}
```

Vorschlag für Built-ins:

```text
plan_number      → PlanNumber
plan_index       → PlanIndex
project_number   → ProjectNumber
description      → Description
datum            → Date
ignore           → Ignore

geschoss         → Spatial
haus             → Spatial
bauteil          → Spatial
bauabschnitt     → Spatial
stiege           → Spatial
zone             → Spatial
block            → Spatial

planart          → None
objekt           → None oder Spatial? Klären.
achse            → Spatial? Klären.
```

Empfehlung für Frühphase: `Objekt = Spatial`, `Achse = Spatial`, konservativer gegen Kollisionen.

### 4.2 `is_required` nur aus Rolle ableiten

```text
PlanNumber ist required, weil SemanticRole.PlanNumber.
is_required dient nur UI-Anzeige ★.
Wizard-Validierung prüft SemanticRole, nicht is_required.
```

### 4.3 `token_key` bei Custom: stabil nach Anlage

```text
Name: Akustik-Klasse → token_key: akustik_klasse
Rename Name auf: Schallschutzklasse → token_key bleibt akustik_klasse
```

## 5. Finaler BPM-108-Implementierungsleitfaden

### Phase A — Domain/Persistence/Katalog

Ziel: Segmenttyp-Katalog als neue globale Konfiguration in `bpm.db`.
* Tabellen `segment_type_groups`, `segment_types` mit ADR-050-Feldern.
* Zusatzfelder: `semantic_role TEXT NULL`, `token_key TEXT NOT NULL`, `builtin_version INTEGER`, `user_modified_name/color/sort/active/group`.
* Built-in Seed-on-start mit festen IDs und versionierten Defaults.
* `ISegmentTypeRepository` für CRUD.
* `ISegmentTypeCatalog` als In-Memory-Snapshot + Invalidierung.
* Keine Migration; Release-Hinweis.

### Phase B — Profilformat v4 / PlanManager-Domain

Ziel: Profile referenzieren Segmenttypen per ID; Recognition bleibt BPM-082-kompatibel.
* `RecognitionProfile.SchemaVersion = 4`.
* `ProfileSegment.FieldType` → `FieldTypeId`.
* `IdentityFields` auf IDs.
* `FolderHierarchy` auf `fieldTypeId`.
* `RenameSchema` nutzt `token_key`-Tokens.
* `IndexExtractionConfig.SegmentSelector` auf `fieldTypeId`.
* `RecognitionRule` unverändert.
* `ProfileManager.Load`: strikt `schemaVersion == 4`.
* `ProfileHealth`/Validator (`Valid`, `MissingSegmentTypes`, `OutdatedSchema`, `InvalidRecognitionRules`).
* Auto-Import blockiert bei Missing-ID.
* Optionaler DevTool-Befehl: alte Profile archivieren.

### Phase C — Wizard/UI/Manager

Ziel: Keine hardcoded Segmenttypen/Farben mehr im Wizard.
* `BuildFieldTypeOptions()` ersetzen durch Catalog-Optionen.
* `FileNameSegment` auf `FieldTypeId`.
* Schritt 2: aktive Typen als Chips, „+ Eigenes" Inline-Schnellanlage.
* Schritt 3: Indexvalidierung über `SemanticRole.PlanIndex`.
* Schritt 4: Hierarchie-Kandidaten über `SemanticRole.Spatial`.
* Schritt 5: Variable-Warnung über `PlanNumber/PlanIndex/Date` Rollen.
* Segmenttypen-Manager: Gruppen/Typen, Built-in-Rolle read-only, Custom ohne Rolle, Soft-Delete.
* Farben aus DB.

## 6. Erste 3 Commits

**Commit 1 — Segment type catalog persistence**
```
BPM-108: add segment type catalog schema and seed service
```
* Domain-Modelle `SegmentTypeDefinition`, `SegmentTypeGroupDefinition`, `SegmentSemanticRole`.
* SQLite-Tabellen + Repository.
* Seed-Service mit Built-ins/Gruppen.
* Katalog-Service mit Cache/Invalidierung.
* Unit-Tests.

**Commit 2 — Profile schema v4 with fieldTypeId**
```
BPM-108: switch recognition profiles to segment type ids
```
* `RecognitionProfile` v4.
* `ProfileSegment.FieldTypeId`.
* `IdentityFields`, `FolderHierarchy`, `IndexExtraction` auf IDs.
* `ProfileManager.Load` strikt v4.
* ProfileHealth/Validator.
* Tests.

**Commit 3 — Wizard uses segment type catalog**
```
BPM-108: replace hardcoded field type wizard options
```
* `ProfileWizardViewModel` auf `ISegmentTypeCatalog`.
* `FileNameSegment` auf `FieldTypeId`.
* Step-2-Chips aus aktiven Segmenttypen.
* Inline-„+ Eigenes" Schnellanlage.
* Rollenbasierte Validierung.
* DB-Farben im Rendering.

Danach erst Commit 4 für den vollständigen Manager-Dialog.

## 7. Wo es noch hakt

Nur zwei Punkte brauchen vor Implementierung eine klare Entscheidung:

1. Welche Built-ins bekommen `SemanticRole.Spatial`? Vorschlag: `geschoss`, `haus`, `bauteil`, `bauabschnitt`, `stiege`, `achse`, `zone`, `block`, `objekt`. `planart` bleibt `None`.
2. Soll `token_key` für Built-ins absolut unveränderlich sein? Klare Empfehlung: ja.

## ✅ Einigkeit

* Built-ins sind editierbar, aber ihre fachliche Rolle bleibt fest.
* Custom-Segmenttypen bleiben rein dekorativ und bekommen keine `SemanticRole`.
* `RecognitionRule` bleibt unverändert BPM-082-konform.
* Schema v4 + Reset statt Migration ist richtig.
* `token_key` ist nötig und muss stabil bleiben.
* Missing-ID wird über ProfileHealth sichtbar, nicht still toleriert.
* Groups → Types → Profiles bleibt die spätere Sync-Reihenfolge.
* Inline-Custom-Anlage im Wizard ist für den Lernfluss besser als sofortiger Manager-Wechsel.

## ⚠️ Widerspruch

* Kein `lastKnownLabel` in BPM-108.
* Kein automatisches Profil-Verschieben beim normalen App-Start.
* Keine Massenreparatur im Wizard-Schritt 5.
* Deaktivierte Segmenttypen nicht als graue Drag-Quelle anzeigen.
* `INSERT OR IGNORE` plus vier Ownership-Flags reicht nicht ganz: `user_modified_group` ergänzen.

## ❓ Rückfragen

1. Bestätigst du die Spatial-Rollen für `objekt` und `achse`, oder sollen diese zwei Built-ins `SemanticRole.None` bleiben?
2. Bestätigst du: `token_key` ist nach Anlage unveränderlich, auch bei Custom-Rename?
3. Soll der DevTool-Befehl alte Profile archivieren statt löschen? Ich empfehle archivieren.
4. Wird `PatternTemplateService` in BPM-108 aktiv weiterverwendet, oder soll er bis zur v4-Anpassung deaktiviert werden?
