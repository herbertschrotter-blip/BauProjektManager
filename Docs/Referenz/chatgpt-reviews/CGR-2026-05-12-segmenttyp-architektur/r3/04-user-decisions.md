# User-Entscheidungen — Review Runde 3

**Datum:** 2026-05-18
**User:** Herbert

---

## Antwort auf ChatGPTs optionale Rückfrage

### Token-Key als read-only Vorschau im „+ Eigenes"-Popover anzeigen?

**Entscheidung: JA — anzeigen.**

Beim Tippen des Namens erscheint live eine read-only Token-Zeile: `Token: akustik_klasse`.

Begründung: Pflanzt Template-Verständnis früh ein. User versteht warum Rename später den Token nicht ändert. Transparent vorm User welcher Token in Templates verwendet werden kann.

---

## Serie-Abschluss

- **Runde 3: abgeschlossen** mit Sign-off von ChatGPT
- **CGR-2026-05-12-segmenttyp-architektur: ABGESCHLOSSEN**

## Resultierende Architektur-Entscheidungen für BPM-108

### Domain-Modell

```csharp
public sealed class SegmentTypeDefinition
{
    public string Id { get; init; }                  // ULID für Custom, snake_case für Built-in
    public string Name { get; init; }                // editierbar
    public string Color { get; init; }               // editierbar (Hex #RRGGBB)
    public string TokenKey { get; init; }            // immutable nach Anlage
    public SegmentSemanticRole? SemanticRole { get; init; }  // Built-in: seed-fix; Custom: NULL
    public string GroupId { get; init; }             // editierbar (FK auf segment_type_groups)
    public bool IsBuiltin { get; init; }
    public bool IsActive { get; set; }               // toggle
    public int SortOrder { get; set; }               // user-editierbar
}

public enum SegmentSemanticRole
{
    None, PlanNumber, PlanIndex, ProjectNumber, Date, Description, Spatial, Ignore
}
```

### Spatial-Built-ins
`geschoss`, `haus`, `bauteil`, `bauabschnitt`, `stiege`, `zone`, `block`, `achse`, `objekt`

### Implementierungsreihenfolge (3 Commits)
1. **Commit 1:** Segment type catalog persistence (Phase A)
2. **Commit 2:** Profile schema v4 with `fieldTypeId` (Phase B)
3. **Commit 3:** Wizard uses segment type catalog (Phase C)

Danach: Manager-Dialog + DevTool-Archivierung als eigene Commits.

## 17 Akzeptanzkriterien für BPM-108

**Phase A (Domain/Persistence/Katalog):**
1. Neue Tabellen `segment_type_groups` + `segment_types` mit ADR-050-Feldern
2. Zusatzfelder: `semantic_role`, `token_key`, `builtin_version`, `user_modified_name/color/sort/active/group`
3. Built-in Seed-on-start mit festen IDs + versionierten Defaults
4. `ISegmentTypeRepository` für CRUD
5. `ISegmentTypeCatalog` als In-Memory-Snapshot + Invalidierung (Lazy Load)
6. `GetEffectiveActive()` + `GetIncludingDeleted(id)` Methoden

**Phase B (Profilformat v4):**
7. `RecognitionProfile.SchemaVersion = 4`; `ProfileSegment.FieldType` → `FieldTypeId`
8. `IdentityFields` auf IDs normiert; `FolderHierarchy` auf `fieldTypeId`; `RenameSchema` nutzt `token_key`-Tokens
9. `IndexExtractionConfig.SegmentSelector` auf `fieldTypeId`
10. `RecognitionRule` bleibt unverändert (BPM-082-kompatibel)
11. `ProfileManager.Load` strikt `schemaVersion == 4` (alte Migration entfernen)
12. `ProfileHealth`/Validator: `Valid`, `MissingSegmentTypes`, `OutdatedSchema`, `InvalidRecognitionRules`
13. DevTool-Befehl `ArchiveOutdatedProfiles` (kein Loader-Side-Effect)

**Phase C (Wizard/UI):**
14. **Immutable Keys** — `id` und `token_key` nach Anlage unveränderlich; Rename ändert nur `name`
15. **Built-in-Rollen unveränderlich** — `semantic_role` seed-definiert, im Manager read-only; Custom = NULL
16. **Strict Reset für PatternTemplates** — `pattern-templates.json` nicht migriert; nur v4 wird geladen; alte Templates via DevTool archiviert
17. **Health-Gating vor Auto-Import** — Profil mit `MissingSegmentTypes` darf angezeigt/gematcht werden, blockiert aber Auto-Import bei fachlich relevanter Missing-ID
