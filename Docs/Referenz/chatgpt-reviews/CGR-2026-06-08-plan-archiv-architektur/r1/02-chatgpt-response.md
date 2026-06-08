# Review Runde 1 — Plan-Archiv-Architektur

Kollege, ich stimme deiner Kernthese zu, aber ich würde die Zielarchitektur an zwei Stellen **schärfer trennen**:

1. **PlanManager-Sortierpersistenz** darf schlank bleiben.
2. **Plan-Archiv-/Lookup-Persistenz** muss vor Bautagebuch entstehen.

Der aktuelle Stand ist für den V1-Import ausreichend, aber nicht für zeitbezogene Cross-Modul-Abfragen. Das ist nicht nur Komfort, sondern eine fachliche Invariante: Ein Bautagesbericht muss später exakt die damals gültige Planrevision referenzieren können. Mit `document_key LIKE '%H1%'` und nur `revision_status = current/archived` ist das nicht belastbar.

Ich habe den Branch `feature/planmanager-v1` geprüft. `INDEX.md` bestätigt ausdrücklich die Frühphase ohne Produktivdaten und den Standard „Datei/DB löschen und neu anlegen lassen" statt Migration/Legacy-Toleranz. Außerdem ist im PlanManager-SoT dokumentiert, dass `document_key` aus `identityFields` fachlich stabil gebaut wird, Import-Journal vor Ausführung geschrieben werden muss und MD5 + file_size Pflicht sind. Die PlanManager-Doku beschreibt die 7-Stufen-Pipeline mit `DocumentKeyBuilder`, `RevisionDecisionService` und `ImportPlanBuilder`. Das aktuelle `planmanager.db`-Schema ist in DB-SCHEMA als Schema v1.0 mit 6 Tabellen dokumentiert. Neue fachliche Tabellen müssen ULID und Sync-Felder erhalten.

## Architekturentscheidung

**Status Quo reicht nicht für BPM-056/BPM-057/BPM-061.**

Er reicht für:
* Import analysieren
* Dateien gruppieren
* aktuelle Revision bestimmen
* Archivieren/Undo
* Manifest/Cache-Rebuild

Er reicht nicht für:
* „Zeige mir alle damals aktuellen Pläne für Haus H1, Geschoss EG, Dokumenttyp Polierplan"
* stabile FK-Verknüpfung aus Bautagebuch/Fotos/Berichten
* auditierbare Statuswechsel
* spätere modulübergreifende Plan-Kontextdienste

Der kritische Fehler wäre, Bautagebuch oder Foto-Modul auf `document_key`-Stringfilter aufzubauen. Damit würdet ihr eine technische Import-ID als semantische Abfrage-API missbrauchen.

---

## 1. Document/Revision-Trennung

Ich empfehle eindeutig eine neue Tabelle `plan_documents`.

Der `document_key` bleibt wichtig, aber nur als **fachlicher Natural Key**, nicht als Primäranker für andere Module.

### Warum `document_key` allein nicht reicht

| Problem | Auswirkung |
| --- | --- |
| String enthält mehrere Bedeutungen | Haus, Geschoss, Planart, Nummer usw. sind nicht einzeln indexierbar |
| Profiländerung kann Key-Logik ändern | Cross-Modul-Referenzen werden semantisch instabil |
| Kein FK-Ziel | Bautagebuch müsste String speichern statt `document_id` |
| Keine saubere Dokument-Entität | Revisionen, Dateien, Attribute und Links hängen an einem zusammengesetzten String |
| LIKE-Filter fehleranfällig | `H1` matcht potenziell `H10`, `EH1`, andere Token |

**Empfehlung:**
`plan_documents.id` wird die stabile Entität.
`plan_documents.document_key` bleibt `UNIQUE`, deterministisch und vom `DocumentKeyBuilder` erzeugt.

---

## 2. Metadaten-Persistenz

Ich widerspreche leicht deiner Formulierung „Hybrid B + A" in einem Punkt:

Ich würde **nicht** `building_part_id` und `building_level_id` direkt in `plan_revisions` speichern, sondern in `plan_documents`.

Grund: Haus/Geschoss/Planart/Plan-Nr beschreiben normalerweise das logische Dokument über alle Revisionen hinweg. Revisionen ändern Index, Datei, Hash, Eingang, Statuszeitraum — aber nicht das Gebäude-Target.

### Empfohlene Variante: Hybrid, aber mit klarer Entitätsgrenze

* `plan_documents`: feste Top-Felder/FKs für häufige Modulfilter
* `plan_document_segments`: normalisierte erkannte Segmentwerte
* optional später: `plan_revision_segments`, nur wenn Plankopf/OCR revisionsabhängige Werte liefert

Ich würde die KV-Tabelle nicht `plan_document_attributes` nennen, sondern `plan_document_segments`. Grund: Die Werte stammen aus dem Recognition-/Segmenttyp-System, nicht aus beliebigen Custom Attributes. Das hält den Scope enger und verhindert Aconex-artiges Enterprise-Metadatenwachstum.

### Variante D verwerfen

`LIKE` auf `document_key` ist nur für Debug/Anzeige akzeptabel, nicht als Modul-API.

### Variante C JSON verwerfen

SQLite JSON1 ist für lokale Tools brauchbar, aber hier unnötig schwächer als relationale Indizes. Sobald Bautagebuch, Foto und Vorlagen darauf filtern, willst du stabile Indizes und FK-Semantik.

### Variante A-only verwerfen

Alles als KV ist flexibel, aber für Polier-App unnötig indirekt. Bautagebuch braucht sehr häufig `Bauteil + Geschoss + Planart + Zeitpunkt`. Diese Felder gehören indexiert in feste Spalten oder zumindest in eine dedizierte Mapping-Struktur.

---

## 3. Empfohlene Schema-Skizze

Wichtig: Keine Migration bauen. In Frühphase: `planmanager.db` löschen, neu erzeugen lassen.

### `plan_documents`

```sql
CREATE TABLE plan_documents (
    id TEXT PRIMARY KEY,
    project_id TEXT NOT NULL,
    document_key TEXT NOT NULL UNIQUE,
    document_type_id TEXT NOT NULL,
    plan_number TEXT NOT NULL,
    document_type TEXT NOT NULL,
    title TEXT NOT NULL DEFAULT '',
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    building_part_id TEXT,
    building_level_id TEXT,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (building_level_id) REFERENCES building_levels(id)
);

CREATE INDEX idx_plan_documents_lookup
ON plan_documents(project_id, building_part_id, building_level_id, document_type_id, is_deleted);

CREATE INDEX idx_plan_documents_key
ON plan_documents(document_key);
```

Hinweis: `project_id` wirkt redundant, weil `planmanager.db` pro Projekt existiert. Ich würde es trotzdem setzen. Es kostet fast nichts und macht spätere Sync-/Export-/Debug-Fälle robuster.

### `plan_revisions`

```sql
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    plan_index TEXT,
    index_source TEXT NOT NULL,
    revision_status TEXT NOT NULL
        CHECK (revision_status IN ('current', 'superseded', 'rejected')),
    current_from TEXT NOT NULL,
    superseded_at TEXT,
    received_at TEXT NOT NULL,
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (last_import_id) REFERENCES import_journal(id)
);

CREATE UNIQUE INDEX ux_plan_revisions_current
ON plan_revisions(document_id)
WHERE revision_status = 'current' AND is_deleted = 0;

CREATE INDEX idx_plan_revisions_timetravel
ON plan_revisions(document_id, current_from, superseded_at, is_deleted);
```

Ich würde `revision_status = archived` durch `superseded` ersetzen. „Archived" beschreibt eher Ablage/Storage, „superseded" beschreibt den fachlichen Revisionszustand.

`rejected` ist optional, aber nützlich für später: ältere oder konfliktbehaftete Pläne, die bewusst nicht als gültige Revision übernommen wurden. Falls ihr maximal schlank bleiben wollt: weglassen.

### `plan_document_segments`

```sql
CREATE TABLE plan_document_segments (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    segment_type_id TEXT NOT NULL,
    segment_key TEXT NOT NULL,
    raw_value TEXT NOT NULL,
    normalized_value TEXT NOT NULL,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (segment_type_id) REFERENCES segment_types(id),
    UNIQUE (document_id, segment_type_id)
);

CREATE INDEX idx_plan_document_segments_lookup
ON plan_document_segments(segment_type_id, normalized_value, is_deleted);
```

Warum sowohl `segment_type_id` als auch `segment_key`?
`segment_type_id` ist sauber für FK. `segment_key` ist praktisch für Debug/Export und schützt gegen schwer lesbare ULID-only-Analysen. Kein Legacy-Parser, kein Fallback — nur bewusst gespeicherte Denormalisierung.

### `plan_revision_events`

Ich würde **keine vollwertige Enterprise-History-Tabelle** bauen, aber eine kleine Event-Tabelle schon.

Zwei Zeitstempel (`current_from`, `superseded_at`) reichen für Zeitreise. Sie reichen aber nicht für Nachvollziehbarkeit, warum eine Revision ersetzt wurde.

```sql
CREATE TABLE plan_revision_events (
    id TEXT PRIMARY KEY,
    revision_id TEXT NOT NULL,
    import_id TEXT,
    event_type TEXT NOT NULL
        CHECK (event_type IN ('created', 'made_current', 'superseded', 'file_linked', 'manual_override')),
    event_at TEXT NOT NULL,
    event_by TEXT,
    note TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE INDEX idx_plan_revision_events_revision
ON plan_revision_events(revision_id, event_at);
```

Das ist kein Aconex-Workflow. Das ist ein minimales technisches Audit für Revisionswechsel.

### `plan_context_links`

Ich würde die Cross-Link-Tabelle **noch nicht als generische Beziehungstabelle für alle Module fertig implementieren**, aber das Schema jetzt festlegen und erst mit BPM-056 aktiv nutzen.

Name besser nicht `plan_document_links`, weil der Link fachlich einen Kontext beschreibt.

```sql
CREATE TABLE plan_context_links (
    id TEXT PRIMARY KEY,
    source_module TEXT NOT NULL,
    source_id TEXT NOT NULL,
    target_document_id TEXT NOT NULL,
    target_revision_id TEXT,
    resolution_mode TEXT NOT NULL
        CHECK (resolution_mode IN ('fixed_revision', 'current_at_time')),
    context_time TEXT NOT NULL,
    link_type TEXT NOT NULL
        CHECK (link_type IN ('auto_reference', 'manual_reference', 'attachment')),
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (target_document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (target_revision_id) REFERENCES plan_revisions(id)
);

CREATE INDEX idx_plan_context_links_source
ON plan_context_links(source_module, source_id, is_deleted);

CREATE INDEX idx_plan_context_links_target
ON plan_context_links(target_document_id, target_revision_id, is_deleted);
```

Wichtige Regel:
* Für Bautagebuch-Fußnote würde ich beim Erstellen des Berichts **konkret `target_revision_id` speichern**.
* Zusätzlich kann `context_time` gespeichert bleiben.
* Damit ist der Bericht reproduzierbar, selbst wenn spätere Korrekturen am Revisionszeitraum passieren.

Nur `current_at_time` dynamisch aufzulösen klingt elegant, ist aber gefährlich: Wenn später ein Importdatum korrigiert wird, ändert sich ein alter Bautagesbericht rückwirkend.

---

## 4. Bautagebuch-Query mit Zeitreise

Use Case: „Welche Pläne waren am Berichtstag für Haus H1, Geschoss EG aktuell?"

```sql
SELECT
    pd.id AS document_id,
    pd.document_key,
    pd.plan_number,
    pd.document_type,
    pd.title,
    pr.id AS revision_id,
    pr.plan_index,
    pr.current_from,
    pr.superseded_at,
    pf.relative_path,
    pf.file_name
FROM plan_documents pd
JOIN plan_revisions pr
    ON pr.document_id = pd.id
   AND pr.is_deleted = 0
JOIN revision_file_links rfl
    ON rfl.revision_id = pr.id
   AND rfl.is_primary = 1
JOIN plan_files pf
    ON pf.id = rfl.file_id
   AND pf.is_deleted = 0
WHERE pd.project_id = :project_id
  AND pd.building_part_id = :building_part_id
  AND pd.building_level_id = :building_level_id
  AND pd.document_type_id IN (:polierplan_id, :schalung_id, :bewehrung_id)
  AND pd.is_deleted = 0
  AND pr.current_from <= :report_time
  AND (pr.superseded_at IS NULL OR pr.superseded_at > :report_time)
ORDER BY pd.document_type, pd.plan_number;
```

Beim Speichern des Bautagesberichts würde ich danach Snapshots setzen:

```sql
INSERT INTO plan_context_links (
    id,
    source_module,
    source_id,
    target_document_id,
    target_revision_id,
    resolution_mode,
    context_time,
    link_type,
    created_at,
    created_by,
    last_modified_at,
    last_modified_by,
    sync_version,
    is_deleted
)
VALUES (
    :id,
    'bautagebuch',
    :diary_note_id,
    :document_id,
    :revision_id,
    'fixed_revision',
    :report_time,
    'auto_reference',
    :now_utc,
    :user,
    :now_utc,
    :user,
    0,
    0
);
```

Damit zeigt ein alter Bericht immer dieselbe Revision, nicht die heutige.

---

## 5. Status-Historie / Zeitreise

Meine Empfehlung:

* Für Zeitreise: `current_from` + `superseded_at` in `plan_revisions`
* Für Audit: kleine `plan_revision_events`
* Kein großer Workflow-Lifecycle
* Keine ISO-19650-Suitability-Codes in V1/post-V1-Polier-App

Procore/Aconex/think project! gehen stärker in Dokument-Workflow, Transmittal, Suitability, Review/Approval. Für BPM wäre das aktuell Overkill. Der notwendige Kern ist nicht „wer hat wann freigegeben", sondern „welche Revision war zu Zeitpunkt X fachlich gültig".

Deine `plan_revision_history`-Idee ist grundsätzlich richtig, aber ich würde sie als Event-Log begrenzen. Nicht als volle History-Tabelle mit Before/After-Snapshot jeder Spalte.

---

## 6. Cross-Modul-Verknüpfung

Ich würde das **Schema jetzt mitdenken**, aber die Tabelle erst im Ticket „Plan Lookup API / Bautagebuch-Integration" wirklich nutzen.

Konkret:
* In Plan-Archiv-Architektur-Ticket: Tabelle definieren, Repository vorbereiten, aber keine UI.
* In BPM-056: Schreiben der Links beim Bautagebuch-Speichern.
* In BPM-057: Wiederverwenden für Fotos.
* In BPM-061: Wiederverwenden für Vorlagen/Berichte.

Wichtig: Cross-Modul-Verknüpfungen sollten **auf Revision speichern**, wenn es um historische Nachweise geht. `target_document_id + target_revision_id` ist besser als nur `target_document_id + resolution_mode`.

Deine Variante mit `revision_resolution = current_at_time` ist für dynamische Vorschläge gut, aber für abgelegte Berichte nicht stabil genug.

---

## 7. Auto-Learn für Stammdaten

Hier stimme ich dir fachlich zu: **nicht magisch automatisch anlegen**.

Aber ich würde die erste Version noch schlanker halten als dein Fuzzy/Alias-Konzept.

### Empfohlene Stufe 1

Beim Import:

| Fall | Verhalten |
| --- | --- |
| Segmentwert matcht eindeutig Stammdatum | FK setzen |
| Segmentwert matcht nicht | Plan trotzdem importieren, FK bleibt NULL, Preview-Warnung |
| User bestätigt Zuordnung in Preview | Mapping speichern |
| User legt neues Stammdatum an | Stammdatensatz erzeugen, FK setzen |

### Zusätzliche Tabelle statt JSON-Alias

Keine `building_parts.aliases` JSON-Spalte. Besser relational:

```sql
CREATE TABLE building_part_aliases (
    id TEXT PRIMARY KEY,
    project_id TEXT NOT NULL,
    building_part_id TEXT NOT NULL,
    alias_value TEXT NOT NULL,
    normalized_alias_value TEXT NOT NULL,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    UNIQUE (project_id, normalized_alias_value)
);
```

Analog später `building_level_aliases`, falls nötig.

Fuzzy-Match als Vorschlag ja, aber nicht als automatische Entscheidung. Gerade bei `H1`, `Haus 1`, `Haus 01`, `BT1`, `Stiege 1` können falsche Zuordnungen später wesentlich schädlicher sein als ein einmaliger Preview-Hinweis.

---

## 8. Impact auf bestehenden Code

Da die letzten lokalen Commits laut Hinweis nicht gepusht sind, kann ich die konkreten Service-Dateien im GitHub-Stand nicht vollständig gegenprüfen. Die PlanManager-Doku nennt aber die relevante Pipeline und Service-Namen verbindlich.

| Komponente | Bricht? | Änderung |
| --- | ---: | --- |
| `DocumentKeyBuilder` | Nein | Bleibt zentrale Natural-Key-Erzeugung. Ergebnis wird in `plan_documents.document_key` gespeichert. |
| `RevisionDecisionService` | Teilweise | Lookup nicht mehr direkt gegen `plan_revisions.document_key`, sondern `plan_documents.document_key` + aktuelle Revision. |
| `ImportWorkflowService` | Ja, mittel | Nach Stage 5 muss Document-Resolution entstehen: `document_key -> document_id`, Segmentwerte -> FK/Segments. |
| `ImportPlanBuilder` | Ja, mittel | Execution Plan muss zwischen `new document`, `new revision`, `supersede current revision`, `file link` unterscheiden. |
| `PlanManagerDatabase` / Repository | Ja | Schema neu, Methoden für Document/Revision/Segments/Events. |
| `revision_file_links` | Nein | Bleibt richtig. |
| `plan_files` | Fast nein | Sollte nur Sync-Spalten/Soft Delete nachziehen, falls noch nicht vorhanden. |
| `import_journal` | Nein | Weiterhin Import-Audit. Nicht mit Revisions-Audit verwechseln. |
| `.bpm/plan-index.json` | Ja | Manifest muss `document_id`, `document_key`, `revision_id`, `current_from`, `superseded_at` optional exportieren. |

Wichtig: Das Import-Journal ersetzt keine Revisionshistorie. Es sagt „was hat dieser Import gemacht". Die Revisionstabelle muss trotzdem selbst zeitlich abfragbar sein.

---

## 9. Reihenfolge

Ich würde die Architekturänderung **nicht vor V1-Sperrposten erzwingen**, aber **zwingend vor BPM-056**.

### Empfohlene Reihenfolge

| Reihenfolge | Task | Begründung |
| ---: | --- | --- |
| 1 | BPM-080/081/006 abschließen | V1-Schnitt sauber halten |
| 2 | V1 einfrieren/releasen | Import/Sortierung als stabiler Kern |
| 3 | Neuer Task: BPM-NNN Plan-Archiv-Persistenz v2 | Architekturfundament für Module |
| 4 | `IPlanLookupService` | Öffentliche API statt SQL-Wildwuchs in Modulen |
| 5 | BPM-056 Bautagebuch | Nutzt stabile Revision-Snapshots |
| 6 | BPM-057 Foto | Nutzt denselben Lookup |
| 7 | BPM-061 Vorlagen | Nutzt denselben Lookup |
| 8 | BPM-092 Profile in DB | Kann davor oder danach, aber nicht blockierend |

### Vor oder nach BPM-092?

Ich würde **Plan-Archiv vor BPM-092** machen.

Grund: `recognition_profiles` in DB ist sauber, aber für den Bautagebuch-Lookup nicht zwingend. `document_type_id` kann vorerst die Profil-ID aus JSON bleiben. Eine echte FK auf Profile kommt später mit BPM-092.

Nicht warten, bis Profile in DB sind — sonst blockiert ihr eine wichtige fachliche Architektur an einer administrativen Profilpersistenz.

---

## 10. Aufwand

Meine realistische Schätzung:

| Block | Aufwand |
| --- | ---: |
| Schema neu + DB löschen/neu erzeugen | 0,5–1 PT |
| Domain Models + Repository-Methoden | 1 PT |
| ImportWorkflow-Anpassung | 1–1,5 PT |
| RevisionDecision/ImportPlanBuilder-Anpassung | 1 PT |
| Segmentpersistenz + Stammdaten-Mapping basic | 1 PT |
| `IPlanLookupService` + Query-Tests | 1 PT |
| Doku/ADR/Tests | 1 PT |

**Gesamt:** 5,5–7,5 Personentage.

Deine 3–5 Tage sind möglich, wenn der bestehende Code sehr sauber gekapselt ist und Auto-Learn nur minimal umgesetzt wird. Mit sauberem Lookup-Service, Tests und Doku halte ich 6–8 PT für ehrlicher.

---

## 11. Was wir explizit nicht bauen sollten

Hier bin ich nahe bei dir, aber ich würde die Verbote noch konkreter machen:

| Nicht bauen | Warum |
| --- | --- |
| ISO-19650 Suitability S0–S4 | Für Polier-/Bauleiter-Alltag zu schwergewichtig |
| Transmittals | Kein Versand-/Freigabeportal bauen |
| Voll flexible Custom Fields | Segmenttypen reichen; sonst baut ihr Aconex light |
| OCR-/Plankopf-Abhängigkeit | Persistenz darf nicht auf späterer Recognition-Logik basieren |
| JSON-Metadaten als Hauptfilter | Für Modulabfragen schlechter als relationale Indizes |
| Generische Entity-Link-Welt für alles | Nur Plan-Kontext-Links, nicht universelles Knowledge Graph |
| Automatisches Stammdaten-Anlegen ohne Bestätigung | Verschmutzt Bauteile/Geschosse nachhaltig |
| Migrationslogik | Widerspricht Frühphase-Regel im Projekt |
| Legacy-Toleranz in Loadern | Ebenfalls explizit nicht gewünscht |
| Revision-Branching | Baupläne sind linear genug: current → superseded |

---

## 12. Konkreter Ticket-Vorschlag

**BPM-NNN — Plan-Archiv-Persistenz v2**

Ziel: PlanManager-Persistenz für zeitbezogene Modulabfragen vorbereiten.

### Subtasks

1. **BPM-NNN.01 — Schema v2 neu erzeugen**
   * `plan_documents`
   * `plan_revisions` angepasst
   * `plan_document_segments`
   * `plan_revision_events`
   * optional `plan_context_links`
   * Sync-Spalten gemäß ADR-050
   * Keine Migration; betroffene Datei: lokale `planmanager.db` löschen

2. **BPM-NNN.02 — Domain Models**
   * `PlanDocument`
   * `PlanRevision`
   * `PlanDocumentSegment`
   * `PlanRevisionEvent`
   * `PlanContextLink`

3. **BPM-NNN.03 — Import-Pipeline anpassen**
   * Stage 5: `document_key` bauen
   * Document upsert/resolve
   * Segmentwerte persistieren
   * Stammdaten-FKs setzen, falls eindeutig

4. **BPM-NNN.04 — Revision-Zeitlogik**
   * alte Current-Revision: `revision_status = superseded`, `superseded_at = now`
   * neue Revision: `revision_status = current`, `current_from = now`
   * Event schreiben

5. **BPM-NNN.05 — IPlanLookupService**
   * `FindCurrentPlansAsync(projectId, buildingPartId, buildingLevelId, documentTypeIds, atUtc)`
   * `CreatePlanContextSnapshotAsync(sourceModule, sourceId, atUtc, filters)`

6. **BPM-NNN.06 — Stammdaten-Mapping minimal**
   * exakte Normalisierung
   * Preview-Warnung bei fehlendem Mapping
   * keine automatische Anlage ohne Bestätigung

7. **BPM-NNN.07 — Doku/ADR/Tests**
   * `DB-SCHEMA.md`
   * `PlanManager.md`
   * neues ADR „Plan-Archiv-Persistenz"
   * Query-Tests für Zeitreise

---

## 13. Mein direktes Feedback zu deiner Analyse

Du liegst richtig bei:

* Status Quo nur für Sortierung ausreichend
* Document/Revision/File als Drei-Ebenen-Modell
* Hybrid statt reinem KV/JSON
* Zeitreise vor Bautagebuch
* keine Enterprise-Suitability/Transmittals
* kein Auto-Create ohne User-Kontrolle

Ich widerspreche bzw. würde ändern bei:

1. **Links auf Document statt Revision**
   Für historische Berichte muss der Link am Ende auf `revision_id` zeigen. `document_id + current_at_time` ist nur zur Vorschlagsberechnung gut.

2. **`building_part_id` / `building_level_id` in `plan_revisions`**
   Diese gehören primär zu `plan_documents`, solange sie nicht revisionsabhängig sind.

3. **`plan_document_attributes` als Name**
   Lieber `plan_document_segments`, weil die Quelle das Segmenttyp-System ist. „Attributes" öffnet unnötig die Tür zu Custom-Field-Overengineering.

4. **Aufwand 3–5 PT**
   Ich würde 6–8 PT planen, wenn Tests, Doku, Lookup-Service und saubere Reset-Anleitung enthalten sind.

5. **Auto-Learn mit Alias-JSON**
   Alias ja, aber relational. Keine JSON-Spalte in `building_parts`, wenn diese Aliaswerte später aktiv für Mapping und Indizes genutzt werden.

---

## ✅ Einigkeit

* Der aktuelle PlanManager-Stand ist für V1-Sortierung ausreichend.
* Für Bautagebuch/Foto/Vorlagen braucht es vorab einen Architektur-Schritt.
* `document_key` bleibt wichtig, darf aber nicht alleinige Cross-Modul-Identität sein.
* Document/Revision/File-Trennung ist sinnvoll.
* Segmentwerte müssen strukturiert persistiert werden.
* Zeitreise braucht `current_from` und `superseded_at`.
* Import-Journal ersetzt keine Plan-Revisions-Zeitlogik.
* Auto-Learn darf Stammdaten nicht ungefragt verschmutzen.
* Keine Migration/Legacy-Patterns in der Frühphase; DB löschen und neu erzeugen lassen.

## ⚠️ Widerspruch

* Cross-Modul-Links sollten für historische Nachweise auf konkrete `revision_id` snapshotten, nicht nur dynamisch `current_at_time` auf `document_id` speichern.
* Feste Filter-FKs gehören primär in `plan_documents`, nicht in `plan_revisions`.
* JSON-Spalten für Segmentfilterung sind hier schlechter als relationale Tabellen.
* Eine volle Enterprise-`plan_revision_history` ist zu viel; ein kleines `plan_revision_events` reicht.
* BPM-092 sollte nicht Voraussetzung für diese Architektur sein.
* Aufwand eher 6–8 PT als 3–5 PT, wenn es sauber mit Tests und Doku gemacht wird.

## ❓ Rückfragen

1. Gibt es Fälle, in denen sich Haus/Geschoss/Bauteil zwischen Revisionen fachlich ändern darf, oder ist das dann immer ein neues logisches Dokument?
2. Soll ein Bautagesbericht beim Speichern die Planfußnoten fix snapshotten, oder sollen sie bei jedem Öffnen dynamisch neu berechnet werden? Meine Empfehlung ist Snapshot.
3. Gibt es bereits eine verbindliche Liste von `document_type_id`-Werten für Polierplan/Schalung/Bewehrung, oder kommen diese ausschließlich aus Profil-JSON?
4. Soll `planmanager.db` langfristig lokale Cache-DB bleiben, oder sollen Teile der Plan-Archiv-Persistenz später in `bpm.db` wandern?
5. Sind `building_levels` global pro Bauteil ausreichend, oder braucht ihr später projektspezifische Alias-/Normalisierungstabellen für Geschosse zwingend schon in diesem Ticket?
