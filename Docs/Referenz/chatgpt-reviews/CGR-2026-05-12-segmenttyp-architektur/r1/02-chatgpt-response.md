# Review Runde 1 — ChatGPT-Antwort

**Modell:** GPT-5.4
**Datum:** 2026-05-18
**Canvas-Titel:** Review Runde 1

---

Kollege, ich habe den Vorschlag gegen den aktuellen Stand auf Branch `feature/planmanager-v1` geprüft. Meine wichtigste Korrektur vorweg: Die These „`DocumentTypeRecognizer` arbeitet derzeit mit `FieldType` Enum" stimmt im aktuellen Code nicht mehr sauber. Der Recognizer matcht in BPM-082 bereits ausschließlich über `RecognitionRule.Method`, `Pattern` und `SegmentPosition`. Das `FieldType`-Enum hängt vor allem noch an Wizard, Segment-Zuweisung, Hierarchie, Pflichtfeld-/Identity-Ableitung und Profil-Segmenten.

## Gesamturteil

Die Architekturidee ist richtig, aber noch nicht schnittfest. Der kritische Punkt ist nicht die Recognition selbst, sondern die zweite Semantikschicht: `FieldType` wird aktuell zugleich als UI-Katalog, fachlicher Schlüsselname, Pflichtfeld-Trigger, Identity-Trigger, Hierarchie-Trigger, Rename/Folder-Template-Token und Wizard-Validierungsbasis verwendet. BPM-108 darf diese Rollen nicht alle blind durch frei definierbare Segmenttypen ersetzen.

Meine Empfehlung: `FieldType` als Enum nicht sofort ersatzlos entfernen, sondern durch ein zweistufiges Modell ersetzen:

```csharp
public sealed class SegmentTypeDefinition
{
    public string Id { get; init; } = "";              // stable: plan_number, geschoss, custom ULID
    public string Name { get; init; } = "";            // UI label: Plannummer
    public string Color { get; init; } = "";
    public string? SemanticRole { get; init; }          // PlanNumber, PlanIndex, Date, Spatial, Description, Ignore, Custom
        = null;
    public bool IsBuiltin { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
}
```

`fieldTypeId` ist die persistierte Referenz. `SemanticRole` ist die fachliche Brücke für bestehende Logik, die wirklich besondere Semantik braucht. Custom-Segmenttypen haben typischerweise `SemanticRole = null` oder optional eine grobe Rolle wie `Spatial`/`Description`, aber keine harte Enum-Entsprechung.

## Blocker

### 1. `FieldType` ist derzeit mehr als ein Display-Katalog

Im aktuellen `ProfileWizardViewModel` wird `FieldType` aktiv verwendet für:

* Schritt-2-Validierung: `PlanNumber` muss vorhanden sein.
* Schritt-3-Validierung: bei `IndexSource.FileName` muss `PlanIndex` vorhanden sein.
* Schritt-4-Hierarchie: feste Liste aus `Geschoss`, `Haus`, `Bauteil`, `Bauabschnitt`, `Stiege`, `Zone`, `Block`.
* Schritt-5-Warnung: `PlanNumber`, `PlanIndex`, `Datum` gelten als variable Segmente.
* `BuildFromWizard`: `Required = PlanNumber`; `IncludeInIdentity = PlanNumber/Haus/Bauteil/Bauabschnitt`.

Das heißt: Wenn du nur `ProfileSegment.FieldType` von Enum-String auf `fieldTypeId` umstellst, verlierst du zentrale Fachlogik oder baust sie implizit in String-Vergleiche nach. Das wäre schlechter als heute.

Konkrete Lösung:

```csharp
public sealed class ProfileSegment
{
    public int Position { get; set; }
    public string FieldTypeId { get; set; } = "";      // statt FieldType
    public string LabelOverride { get; set; } = "";    // optional; normalerweise leer
    public bool Required { get; set; }
    public bool IncludeInIdentity { get; set; }
}
```

Und im Wizard nie direkt gegen `FieldType` prüfen, sondern über den Katalog:

```csharp
bool HasRole(IEnumerable<FileNameSegment> segments, SegmentSemanticRole role) =>
    segments.Any(s => s.FieldTypeId is { Length: > 0 }
        && segmentTypeCatalog.Get(s.FieldTypeId)?.SemanticRole == role);

private bool ValidateStep2() =>
    Segments.Count > 0 && HasRole(Segments, SegmentSemanticRole.PlanNumber);

private bool ValidateStep3()
{
    if (SelectedIndexSource != IndexSourceType.FileName)
        return SelectedIndexSource != IndexSourceType.PlanHeader;

    ShowIndexWarning = !HasRole(Segments, SegmentSemanticRole.PlanIndex);
    return !ShowIndexWarning;
}
```

Wichtig: Nicht jeder Segmenttyp braucht eine Semantikrolle. Aber die bestehenden Spezialfälle brauchen eine maschinenlesbare Rolle, sonst wird die Importlogik fragil.

### 2. BPM-082 Recognition braucht kein `FieldType`, aber Profile-Segmente schon

Der aktuelle `DocumentTypeRecognizer` tokenisiert pro Profil und matcht segmentweise über Position + Pattern. Die Rule enthält aktuell kein `fieldType` und auch keinen `fieldTypeId`. Das ist gut. Für Recognition ist `fieldTypeId` nicht nötig.

Das vorgeschlagene JSON-Beispiel:

```json
{
  "method": "segment",
  "segmentPosition": 3,
  "fieldTypeId": "planart",
  "value": "Polierplan"
}
```

würde den BPM-082-Recognizer unnötig ändern. Aktuell heißt das Feld `pattern`, nicht `value`, und der Recognizer braucht nur:

```json
{
  "method": "segment",
  "segmentPosition": 3,
  "pattern": "Polierplan"
}
```

Meine Empfehlung: `RecognitionRule` unverändert lassen. `fieldTypeId` gehört in `segments[]`, nicht in `recognition[]`.

Falls du `fieldTypeId` in `recognition[]` trotzdem aufnehmen willst, dann nur als optionales UI-Hilfsfeld, nicht als Match-Kriterium. Sonst erzeugst du ein Problem bei Custom-Segmenttypen, gelöschten Segmenttypen und importierten Profilen.

### 3. Frühphase: keine JSON-Migration bauen, sondern Schema v4 + Reset

BPM-082 hat `schemaVersion = 3`. BPM-108 ändert die Bedeutung von `segments[].fieldType` substanziell zu `segments[].fieldTypeId`. Das ist kein kleines v3-Patchfeld, sondern ein neues Profilformat.

Ich würde Schema v4 setzen:

```json
{
  "schemaVersion": 4,
  "segments": [
    { "position": 1, "fieldTypeId": "plan_number", "required": true, "includeInIdentity": true }
  ],
  "recognition": [
    { "method": "segment", "pattern": "S", "segmentPosition": 0 }
  ]
}
```

Und `ProfileManager.Load` strikt machen:

```csharp
if (profile.SchemaVersion != 4)
{
    Log.Error("Profil verworfen: {File} — SchemaVersion {Version}, erwartet 4. Datei löschen und neu anlegen.", file, profile.SchemaVersion);
    continue;
}
```

Keine v3→v4 Migration, keine Toleranz für `fieldType`, kein Fallback auf alte Enum-Namen. Das entspricht der Projektphase. Betroffene Testprofile wie Polierplan in `01KRAT6ASMQ0K0BB6SXTCWZSAD` sollten gelöscht und im Wizard neu angelegt werden. Das muss in der BPM-108-Umsetzung explizit als Reset-Hinweis stehen.

### 4. Fehlende `fieldTypeId`: nicht Import-Hardfail, aber Profil nicht automatisch aktiv

Wenn ein Profil eine `fieldTypeId` referenziert, die lokal nicht in `segment_types` existiert, gibt es zwei unterschiedliche Use Cases:

1. Recognition beim Import: Kann weiter funktionieren, weil Recognition nur `segmentPosition + pattern` braucht.
2. Parsing, Identity, FolderHierarchy, Rename, Wizard-Reopen: Kann fachlich kaputt sein, wenn die fehlende ID in `identityFields`, `folderHierarchy` oder Templates verwendet wird.

Ein pauschaler Hardfail beim Laden wäre zu hart, weil ein Profil vom anderen Gerät nach Sync-Lag kurzfristig vor dem Segmenttyp ankommen kann. Ein pauschales Soft-Skip wäre gefährlich, weil Import mit falscher Identity passieren könnte.

Konkrete Regel:

* `ProfileManager.LoadAll`: Profil laden, aber mit `ProfileHealth = MissingSegmentTypeReferences` markieren.
* `DocumentTypeRecognizer`: darf es matchen.
* Import-Pipeline nach Recognition: blockiert Auto-Import, wenn fehlende Segmenttypen in `segments`, `identityFields`, `folderHierarchy` oder `renameSchema` relevant sind.
* UI: Profilkarte zeigt „Segmenttyp fehlt: — Sync/Reset prüfen".
* Wizard-Reopen: erlaubt Anzeige mit Fallback-Label, aber Speichern erst nach Reparatur.

Pseudocode:

```csharp
var missing = profile.Segments
    .Select(s => s.FieldTypeId)
    .Where(id => segmentTypeCatalog.GetIncludingDeleted(id) is null)
    .Distinct()
    .ToList();

profile.Health = missing.Count == 0
    ? ProfileHealth.Valid
    : ProfileHealth.MissingSegmentTypes;

profile.MissingSegmentTypeIds = missing;
```

Damit bleibt Sync robust, ohne falsche Auto-Sortierung zu erlauben.

## Wichtig

### 5. Soft-Delete-Lookup ungefiltert ist fachlich richtig, aber UI braucht Zustand

Der Vorschlag „Profile-Lookup ungefiltert, auch deleted" ist richtig. Sonst würden bestehende Profile nach Soft-Delete nicht mehr rendern. Aber der Lookup darf nicht nur `name/color` liefern, sondern auch `is_active/is_deleted`.

Empfohlene UI-Logik:

```csharp
public sealed record SegmentTypeDisplay(
    string Id,
    string Name,
    string Color,
    bool IsActive,
    bool IsDeleted,
    bool Exists);
```

Rendering:

* exists + active: normal.
* exists + inactive: normal rendern, Badge „deaktiviert".
* exists + deleted: rendern, Badge „gelöscht", nicht als neuer Chip auswählbar.
* missing: grauer Fallback-Token `Unbekannt (<id>)`, Import blockieren, wenn fachlich relevant.

### 6. Built-in-Seeding braucht Update-Policy, nicht nur `INSERT OR IGNORE`

`INSERT OR IGNORE` löst nur „neuer Built-in kommt hinzu". Es löst nicht:

* Built-in-Farbe im neuen Release soll geändert werden.
* Built-in-Gruppe wird neu sortiert.
* Built-in-Default-Name wird verbessert.
* User hat Built-in bewusst deaktiviert.

Wenn Built-ins editierbar sind, brauchst du eine klare Ownership-Regel pro Feld.

Empfehlung: Built-in-Defaults versionieren und User-Overrides getrennt behandeln.

Minimal ohne Overengineering:

```sql
ALTER TABLE segment_types ADD COLUMN builtin_version INTEGER NOT NULL DEFAULT 1;
ALTER TABLE segment_types ADD COLUMN user_modified_name INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_color INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_sort INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_active INTEGER NOT NULL DEFAULT 0;
```

Seed-on-start:

```sql
-- Neue Built-ins: insert
INSERT OR IGNORE INTO segment_types (...);

-- Bestehende Built-ins: nur nicht-user-modifizierte Felder aktualisieren
UPDATE segment_types
SET color = @defaultColor, builtin_version = @version
WHERE id = @id AND is_builtin = 1 AND user_modified_color = 0;
```

Wenn dir diese Flags zu viel sind, dann nimm die harte Frühphasen-Variante: Built-in-Updates überschreiben Name/Farbe/Sortierung, aber niemals `is_active`. Das muss dokumentiert sein.

### 7. Sync-Reihenfolge: Groups vor Types, aber Pull/Apply muss topologisch sein

`segment_types.group_id` referenziert `segment_type_groups.id`. Damit ist die Sync-Reihenfolge nicht egal.

Für ADR-053 ist die richtige Reihenfolge:

1. Pull/apply `segment_type_groups`
2. Pull/apply `segment_types`
3. Pull/apply `recognition_profiles`, sobald diese später in DB wandern

Bei Push analog. Bei Server-gewinnt-Konflikten brauchst du keine Merge-UI, aber du brauchst deterministische FK-Anwendung.

Konfliktbeispiel:

* Gerät A: `is_deleted = 1`
* Gerät B: `name = "Akustikklasse"`
* Server entscheidet nach `server_version`/letztem akzeptierten Write.

Wenn Server Delete gewinnt, ist das Ergebnis:

```text
name kann technisch trotzdem aktualisiert sein, aber UI blendet wegen is_deleted aus.
Profile-Lookup findet den Typ weiterhin und rendert ihn als gelöscht.
```

Wichtig: Soft-Delete darf keine referenzierten `segment_types` hart entfernen. Hard-Delete gibt es nicht, auch nicht durch Cleanup.

### 8. Custom-Segmenttyp-Duplikate werden nicht verschmolzen

Wenn Gerät A und Gerät B offline jeweils „Akustik-Klasse" anlegen, entstehen zwei ULIDs. Beim Sync dürfen sie nicht automatisch gemerged werden. Name ist kein fachlicher Schlüssel; automatische Verschmelzung würde Profile brechen, weil Profile auf IDs referenzieren.

Erwartetes Verhalten: Zwei Einträge bleiben zwei Einträge. UI zeigt sie ggf. als Duplikate.

Optionaler späterer Komfort:

```text
„Ähnliche Segmenttypen gefunden: Akustik-Klasse (2). Zusammenführen?"
```

Das wäre ein expliziter Merge-Use-Case, der alle referenzierenden Profile von alter ID auf neue ID umschreibt. Für jetzt: nicht bauen.

### 9. `identityFields` als Strings müssen ebenfalls auf IDs umgestellt werden

Aktuell sind `identityFields` Strings wie `documentType`, `planNumber`. Wenn `ProfileSegment` auf `fieldTypeId` umgestellt wird, muss diese Liste entweder:

* weiterhin semantische Rollen enthalten (`documentType`, `plan_number`, `haus`), oder
* sauber auf Segmenttyp-IDs referenzieren.

Ich würde differenzieren:

```json
"identityFields": [
  { "kind": "system", "id": "documentType" },
  { "kind": "segmentType", "id": "plan_number" },
  { "kind": "segmentType", "id": "haus" }
]
```

Minimalvariante:

```json
"identityFields": ["documentType", "plan_number", "haus"]
```

Dann muss aber dokumentiert sein: `documentType` ist ein reservierter System-Key, alle anderen Werte sind `segment_type.id`. Nicht `FieldType.ToLowerInvariant()` verwenden, sonst entstehen Drift-Keys wie `plannumber` vs. `plan_number`.

### 10. FolderHierarchy und RenameSchema brauchen dieselbe ID-Strategie

Aktuell baut der Wizard `folderHierarchy` aus `FieldType.ToString().ToLowerInvariant()`. Bei IDs muss das geändert werden.

Empfohlen:

```json
"folderHierarchy": ["geschoss", "bauteil"],
"renameSchema": "{project_number}-{plan_number}-{plan_index}_{geschoss}"
```

Also: Template-Tokens sind Segmenttyp-IDs. Built-in IDs müssen template-tauglich sein: lowercase snake_case, stabil, keine Leerzeichen. Custom-IDs als ULID sind schlecht lesbar für Templates. Deshalb brauchen Custom-Typen einen separaten `tokenKey` oder eine Template-Auswahl über UI statt manuelle Texteingabe.

Schema-Erweiterung:

```sql
ALTER TABLE segment_types ADD COLUMN token_key TEXT NOT NULL;
CREATE UNIQUE INDEX ux_segment_types_token_key_active
ON segment_types(token_key)
WHERE is_deleted = 0;
```

Für Built-ins: `plan_number`, `geschoss`. Für Custom: aus Name generieren, bei Konflikt Suffix: `akustik_klasse_2`. Persistierte Profile sollten aber weiterhin über `fieldTypeId` referenzieren; `token_key` ist nur Template-Syntax.

### 11. Wizard Schritt 5: Reopen braucht Snapshot oder Fallback-Resolver

Beim Profil-Neuöffnen müssen die gespeicherten Segment-Zuweisungen wieder farbig/benannt angezeigt werden. Wenn ein Segmenttyp gelöscht ist, soll das Profil funktional rendern. Dafür reicht ein DB-Lookup, solange der Typ soft-deleted existiert. Bei Missing-ID brauchst du Fallback.

Ich würde im Profil keinen kompletten Snapshot von Name/Farbe speichern, weil Rename/Farbe ja live wirken sollen. Aber ein optionales `labelOverride` oder `lastKnownLabel` kann für Missing-Fälle nützlich sein.

Minimal:

```json
{ "position": 4, "fieldTypeId": "01H...", "lastKnownLabel": "Akustik-Klasse" }
```

Regel beim Rendern:

```csharp
var type = catalog.GetIncludingDeleted(segment.FieldTypeId);
var label = type?.Name ?? segment.LastKnownLabel ?? $"Unbekannt ({segment.FieldTypeId})";
var color = type?.Color ?? "#777777";
```

Beim Speichern eines geöffneten Profils:

* Missing-ID bleibt erhalten, solange User das Segment nicht neu zuweist.
* Wenn User editiert und speichern will, blockieren, falls Missing-ID in Identity/Hierarchy/Rename relevant ist.

### 12. Manuell-Sortieren: keine direkte Recognition-Kopplung, aber Token-Anzeige kann Segmenttypen verwenden

Der manuelle Sortiermodus wählt Zielordner direkt. Er muss nicht über Profile oder RecognitionRules laufen. Eine harte Wechselwirkung mit `segment_types` sehe ich nicht.

Aber sobald der manuelle Dialog Vorschläge, Rename-Preview, Segment-Tokens oder „Felder eingeben" anbietet, sollte er denselben Segmenttyp-Katalog nutzen. Sonst hast du wieder zwei UI-Vokabulare.

Für V1 reicht:

```text
Manuell-Sortieren bleibt ordnerorientiert. Segmenttypen werden nur verwendet, wenn der Dialog segmentbasierte Umbenennung oder Metadaten-Eingabe anbietet.
```

## Nice-to-have / spätere Abrundung

### 13. Lookup-Performance: Repository + kleiner Cache reicht

Bei 17 Built-ins plus Custom-Typen ist DB-Performance kein echtes Risiko. Das Risiko ist eher UI-Drift und unnötiger Resolver-Code überall.

Ich würde trotzdem einen zentralen Katalog-Service bauen:

```csharp
public interface ISegmentTypeCatalog
{
    IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive();
    SegmentTypeDefinition? GetIncludingDeleted(string id);
    IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted();
    event EventHandler Changed;
}
```

Implementierung:

* Lazy Load in Memory.
* Invalidierung nach Manager-Dialog-Save/Toggle/Delete.
* Kein komplexer TTL-Cache.
* Im Sync-Apply nach Änderung `Changed` feuern.

Das verhindert N+1-Lookups in Token-Renderern und macht UI konsistent.

### 14. DSGVO: explizit als Klasse A dokumentieren

Segmenttypen sind UI-/Profilkonfiguration und grundsätzlich Klasse A. Personenbezug sehe ich nicht, außer User schreibt selbst personenbezogene Bezeichnungen hinein. Das ist aber nicht Zweck der Funktion.

Ich würde es kurz in `DSVGO-Architektur.md` oder im PlanManager-Datenschutzabschnitt erwähnen, weil ADR-053 Sync-DTOs explizit DataClassification + Whitelist pro DTO fordert. Für Sync braucht `SegmentTypeDto` dann klar:

```csharp
[DataClassification(DataClass.A)]
public sealed record SegmentTypeDto(...);
```

Keine DSFA, keine External-Communication-Relevanz.

### 15. Farb-Tokens: DB-Farbe ja, aber Theme-Fallback definieren

`Colors.xaml`-Tokens entfallen für dynamische Segmenttypen, aber XAML braucht robuste Fallbacks.

Empfohlen:

* DB speichert Hex `#RRGGBB`.
* ViewModel konvertiert zu `SolidColorBrush`.
* Ungültige Farbe wird beim Speichern verhindert.
* Beim Lesen: ungültige Farbe → neutraler Fallback + Log.Warning.

Keine Legacy-Toleranz für alte Profile, aber defensive DB-Anzeige ist okay, falls User/Sync defekte Daten erzeugt.

## Konkreter Zielzuschnitt für BPM-108

Ich würde BPM-108 in drei klar getrennte Schichten schneiden:

### A. Domain/Persistence

* Neue Tabellen `segment_type_groups`, `segment_types` in `bpm.db`.
* Sync-Spalten nach ADR-050 vollständig.
* Built-in Seed beim App-Start.
* Keine Migration; Reset-Hinweis für `bpm.db`/Profile.
* `SegmentTypeRepository` + `SegmentTypeCatalog`.

### B. Profilformat v4

* `ProfileSegment.FieldType` → `FieldTypeId`.
* `identityFields`, `folderHierarchy`, `renameSchema` auf stabile IDs bzw. `token_key` umstellen.
* `RecognitionRule` unverändert lassen: `method`, `pattern`, `segmentPosition`.
* `ProfileManager.Load` strikt auf `schemaVersion == 4`.
* Testprofile löschen und neu anlegen.

### C. Wizard/UI

* `BuildFieldTypeOptions()` durch `ISegmentTypeCatalog.GetEffectiveActive()` ersetzen.
* Pflicht-/Index-/Variable-Segment-Logik über `SemanticRole` statt Enum.
* Manager-Dialog für Gruppen/Typen.
* Soft-deleted/inactive/missing Zustände rendern.
* Cache invalidieren nach Manager-Änderungen.

## Meine Antworten auf deine 10 Prüfpunkte

1. **BPM-082 Segment-Recognition:** `DocumentTypeRecognizer` braucht kein `FieldType`. Er arbeitet aktuell bereits positions-/patternbasiert. `fieldTypeId` gehört in `segments[]`, nicht in `recognition[]`. Enum nicht sofort komplett wegwerfen; durch `fieldTypeId + SemanticRole` ersetzen.
2. **JSON-Schema:** Schema v4. Frühphasen-Reset, keine Migration. Alte v3-Testprofile löschen und neu anlegen, inklusive Polierplan-Testprofil im genannten Projektordner.
3. **ProfileManager Load/Save:** Missing `fieldTypeId` nicht beim JSON-Load komplett verwerfen. Profil als unhealthy laden; Auto-Import blockieren, wenn Missing-ID fachlich relevant ist. UI zeigt Reparaturhinweis.
4. **Sync ADR-053:** Groups vor Types vor Profiles. Server-gewinnt bleibt ausreichend. Soft-Delete bleibt referenzierbar. Keine Merge-UI. FK-Anwendung topologisch und transaktional.
5. **Built-in-Drift:** `INSERT OR IGNORE` reicht nur für neue Built-ins. Für geänderte Built-in-Defaults brauchst du Feld-Ownership oder harte Regel „Defaults überschreiben alles außer `is_active`".
6. **DSGVO:** Klasse A, explizit dokumentieren, weil Sync-DTOs nach ADR-053 klassifiziert/whitelisted werden sollen.
7. **Manuell-Sortier-Modus:** Keine zwingende Wechselwirkung. Nur bei Rename-/Metadaten-/Token-UI denselben Katalog verwenden.
8. **Wizard Schritt 5 Reopen:** Soft-deleted funktioniert mit ungefiltertem Lookup. Missing-ID braucht Fallback-Label und Save-Blockade bei fachlicher Relevanz.
9. **Custom-ID-Konflikte:** Zwei offline erzeugte gleichnamige Custom-Typen bleiben zwei IDs. Kein Auto-Merge. Optional später expliziter Merge-Assistent.
10. **Lookup-Performance:** Kein echtes DB-Performance-Risiko. Trotzdem zentraler In-Memory-Katalog mit Invalidierung, damit UI und Import dieselbe Sicht verwenden.

## Minimaler Code-Patch als Richtung

```csharp
public enum SegmentSemanticRole
{
    None,
    PlanNumber,
    PlanIndex,
    ProjectNumber,
    Description,
    Date,
    Spatial,
    Ignore
}

public sealed class FileNameSegment : INotifyPropertyChanged
{
    public int Position { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public string? FieldTypeId { get; set; }
    public string DisplayName =>
        FieldTypeId is null
            ? "—"
            : _segmentTypeCatalog.GetIncludingDeleted(FieldTypeId)?.Name
              ?? $"Unbekannt ({FieldTypeId})";
}
```

```csharp
private bool IsLikelyVariableSegment(RecognitionSegment seg)
{
    var fieldTypeId = Segments.FirstOrDefault(s => s.Position == seg.Position)?.FieldTypeId;
    var role = fieldTypeId is null
        ? SegmentSemanticRole.None
        : _segmentTypeCatalog.GetIncludingDeleted(fieldTypeId)?.SemanticRole ?? SegmentSemanticRole.None;

    if (role is SegmentSemanticRole.PlanNumber or SegmentSemanticRole.PlanIndex or SegmentSemanticRole.Date)
        return true;

    var value = seg.RawValue?.Trim() ?? "";
    return value.All(char.IsDigit) || DateTime.TryParse(value, out _);
}
```

```csharp
// RecognitionRule bleibt unverändert
public class RecognitionRule
{
    public string Method { get; set; } = "segment";
    public string Pattern { get; set; } = string.Empty;
    public int? SegmentPosition { get; set; }
}
```

## Schlussbewertung

BPM-108 ist architektonisch sinnvoll, aber nur, wenn wir die harte Trennung einziehen:

```text
Segmenttyp-ID = persistente, user-erweiterbare Klassifikation
SemanticRole = wenige eingebaute fachliche Sonderbedeutungen
RecognitionRule = positionsbasierter Match, unabhängig vom Segmenttyp
```

Ohne diese Trennung wird das System entweder zu starr für Custom-Typen oder zu weich für Import-Identity und Wizard-Validierung.

## ✅ Einigkeit

* Segmenttypen müssen aus Hardcoding in DB/Katalog wandern.
* Profile sollen stabile IDs statt Enum-Displaynamen speichern.
* Soft-Delete statt Hard-Delete ist richtig.
* Profile müssen gelöschte/deaktivierte Typen weiterhin rendern können.
* Sync-Spalten nach ADR-050 sind Pflicht.
* Frühphase bedeutet Reset statt Migration.
* Custom-Typ-IDs per ULID sind richtig.

## ⚠️ Widerspruch

* `fieldTypeId` sollte nicht in `RecognitionRule` als Match-Bestandteil eingeführt werden. BPM-082 braucht das nicht.
* `FieldType` kann nicht ersatzlos durch freie IDs ersetzt werden, solange PlanNumber/PlanIndex/Datum/Hierarchy/Identity Semantik tragen.
* `INSERT OR IGNORE` allein reicht für Built-in-Drift nicht.
* Missing `fieldTypeId` darf nicht zu blindem Auto-Import führen.
* Custom-Typen gleichen Namens dürfen nicht automatisch verschmolzen werden.

## ❓ Rückfragen

1. Sollen Built-in-Segmenttypen vom User umbenennbar/farbänderbar sein, oder nur deaktivierbar/sortierbar?
2. Sollen Custom-Segmenttypen eine `SemanticRole` auswählen können, z.B. „räumlich" oder „beschreibend", oder bleiben sie rein dekorativ?
3. Soll `identityFields` künftig UI-geführt über Segmentauswahl gepflegt werden, oder weiterhin als implizite Regel aus Rollen entstehen?
4. Gibt es bereits eine geplante `recognition_profiles`-DB-Tabelle für ADR-053, oder soll BPM-108 diese vorbereiten?
5. Darf `bpm.db` für BPM-108 in der Frühphase komplett gelöscht/neu aufgebaut werden, inklusive bestehender Settings, oder braucht der Reset granular nur Segmenttyp-/Profil-Dateien?
