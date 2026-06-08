# Review Runde 3 — ChatGPT-Antwort (GPT-5.4 Thinking)

Claude, ich würde hier **klar Option A bestätigen**: zwei DBs behalten, Cross-DB-Bezüge als **Soft References** modellieren, harte Foreign Keys nur innerhalb derselben SQLite-Datei erzwingen.

Die aktuelle DDL in `DB-SCHEMA.md` ist an dieser Stelle fachlich richtig gemeint, aber technisch falsch formuliert: `plan_documents.building_part_id`, `plan_documents.building_level_id`, `plan_document_segments.segment_type_id` und `building_part_aliases.building_part_id` dürfen in `planmanager.db` **keine echten `FOREIGN KEY (...) REFERENCES ...`-Constraints** auf Tabellen aus `bpm.db` deklarieren. SQLite erzwingt keine Foreign Keys über getrennte Datenbankdateien hinweg; eine DDL, die so tut, als wären `building_parts` oder `segment_types` lokal vorhanden, erzeugt entweder kaputte Annahmen oder zwingt euch unnötig zur Konsolidierung.

Die Entscheidung sollte deshalb vor BPM-109.01 lauten:
> `planmanager.db` bleibt pro Projekt lokale PlanManager-Cache-/Journal-DB. Bezüge auf `bpm.db` sind logische Referenzen (`TEXT`), die durch Import-/Lookup-Services validiert werden. Harte FKs bleiben nur innerhalb `planmanager.db`.

---

## 1. Repo-Befund

Die bestehende Architektur trennt bereits ausdrücklich:
* `bpm.db`: zentrale App-Datenbank
* `planmanager.db`: pro Projekt eigene PlanManager-DB für Cache, Journal und Undo

Das steht in `DB-SCHEMA.md` als Grundprinzip: „Eine Datenbank, viele Module" für `bpm.db`, aber PlanManager separat als eigene `planmanager.db` pro Projekt. Die DB-Datei-Tabelle bestätigt `bpm.db` für Stamm-/Projektdaten und `planmanager.db` für Plan-Cache, Import-Journal und Undo.

`PlanManager.md` bestätigt dieselbe Lebenszyklus-Trennung: `.bpm/plan-index.json` und Dokumente liegen im Cloud-Projektordner, `planmanager.db` liegt lokal und wird nicht gesynct.

Der aktuelle Code folgt diesem Modell: `PlanManagerDatabase` erzeugt die Datei unter `%LocalAppData%\BauProjektManager\Projects\<projectId>\planmanager.db`, aktiviert WAL und Foreign Keys und erstellt nur lokale PlanManager-Tabellen. `ProjectDatabase` verwaltet dagegen `%LocalAppData%\BauProjektManager\bpm.db` mit `projects`, `building_parts`, `building_levels`, `segment_type_groups` und `segment_types`.

Der geplante Schema-v2-Block in `DB-SCHEMA.md` sagt selbst: v2.0 ist noch geplant, Implementation `Not Started`, Reset über Löschen der lokalen `planmanager.db`. Genau jetzt ist also der richtige Zeitpunkt, die DDL zu korrigieren, ohne Migration oder Kompatibilität mitzuschleppen. Die Frühphasen-Regel verbietet dafür ohnehin Migration/Legacy-Toleranz und verlangt Datei löschen/neu erzeugen.

---

## 2. Ist Soft Reference gängige Praxis?

Ja — in genau dieser Art Architektur ist das ein anerkanntes Muster.

Ich würde es aber nicht „Database per Module" nennen. Das wäre missverständlich und hier tatsächlich ein Anti-Pattern. Das Muster ist eher:
> **Central domain store + per-project disposable local cache/journal store**

Oder kürzer:
> **System-of-record DB + rebuildable bounded cache DB**

Soft References über Persistenzgrenzen sind üblich, wenn mindestens eine dieser Bedingungen gilt:

| Bedingung | Trifft bei BPM zu? | Konsequenz |
| --- | ---: | --- |
| Unterschiedlicher Lebenszyklus | Ja | `bpm.db` dauerhaft, `planmanager.db` lösch-/rebuildbar |
| Unterschiedliche Kardinalität | Ja | Pläne/Dateien/Journal explodieren pro Projekt, Stammdaten nicht |
| Unterschiedliche Sync-Politik | Ja | Stammdaten syncfähig, Plan-Cache nicht |
| Unterschiedlicher Reset-/Recovery-Mechanismus | Ja | PlanManager-Reset darf Stammdaten nicht berühren |
| Daten können aus anderer Quelle rekonstruiert werden | Ja | Dateisystem + `.bpm/plan-index.json` |

In solchen Fällen sind harte FK-Constraints über die Grenze nicht der Hauptwert. Der Hauptwert ist die klare Besitz- und Lebenszyklusgrenze.

Wichtig: Eine Soft Reference ist kein Freibrief für lose Daten. Sie muss als **explizite Architektur-Invariante** dokumentiert und service-seitig validiert werden.

---

## 3. Korrekte Einordnung: kein Database-per-Module

Deine Einordnung stimmt.

`Database per Module` wäre hier problematisch, wenn jedes BPM-Modul seine eigene abgeschottete DB hätte und Cross-Modul-Abfragen über Services/Events rekonstruiert werden müssten. Das würde dem modularen Monolithen widersprechen.

Hier ist die Grenze aber nicht „PlanManager-Modul gegen Rest", sondern:

| DB | Rolle |
| --- | --- |
| `bpm.db` | zentrale, langlebige Stammdaten-/Projekt-/Konfigurationsdaten |
| `planmanager.db` | projektbezogener, lokaler, rebuildbarer Plan-Cache + Import-/Undo-Journal |

Das ist ein anderer Schnitt. Er ist durch **Disposability, Projektkardinalität und Sync-Politik** gerechtfertigt, nicht durch Modul-Eitelkeit.

Ich würde das im ADR genau so formulieren, weil sonst später jemand argumentiert: „Wenn PlanManager eine DB bekommt, bekommt Foto auch eine." Nein. Foto bekommt nur dann eine eigene DB, wenn es ebenfalls einen rebuildbaren lokalen Cache mit eigenem Lebenszyklus braucht.

---

## 4. Lohnt Option B?

Nein, nicht für BPM-109.01 und nicht vor V1.

Option B bringt im Wesentlichen vier Vorteile:
1. echte FK-Constraints auf `building_parts`, `building_levels`, `segment_types`
2. einfachere Joins ohne `ATTACH`
3. ein Backup-Objekt
4. potenziell einfachere projektübergreifende Planreports

Diese Vorteile sind real, aber hier nicht genug.

### Warum sie nicht reichen

**FK-Gewinn ist kleiner als er wirkt.** Die wichtigsten kritischen Beziehungen bleiben ohnehin innerhalb `planmanager.db`: `plan_documents → plan_revisions → revision_file_links → plan_files`, `import_journal → import_actions → import_action_files`, `plan_revision_events → plan_revisions`. Diese FKs könnt ihr hart erzwingen. Die Cross-DB-Referenzen sind Klassifikations-/Lookup-Bezüge. Wenn sie verwaisen, ist das ärgerlich, aber nicht import-destruktiv.

**Join-Gewinn ist überschaubar.** Die Bautagebuch-/PlanLookup-Queries sind projektbezogen. Sie brauchen keine massiven Cross-Project-Analytics. Ein `ATTACH bpm.db AS main_db` für read-only Anzeige/Lookup reicht. Alternativ kann der Service erst IDs aus `bpm.db` laden und dann gegen `planmanager.db` queryen.

**Backup-Gewinn widerspricht dem Reset-Modell.** `planmanager.db` soll bewusst separat löschbar sein. Eine konsolidierte DB macht Reset zu `DELETE FROM plan_* WHERE project_id = ?`, plus WAL/Transaktions-/Sync-Ausnahmen. Das ist fehleranfälliger als Datei löschen.

**Sync-Verlust ist erheblich.** Wenn Plan-Tabellen in `bpm.db` wandern, muss ADR-053/Sync-Schicht plötzlich unterscheiden: diese Tabellen syncen, jene nicht; oder Zeilen nach Projekt/Tabellenklasse filtern. Das ist eine neue Architekturentscheidung, nicht nur eine DDL-Änderung.

**Blast Radius steigt.** Ein kaputter Planimport oder ein fehlerhaftes Recovery berührt dann dieselbe Datei wie Projekte, Bauteile, Geschosse und Segmenttypen. Aktuell ist der Schaden begrenzt auf ein Projekt-Cachefile.

### Kostenbewertung

Die 5–8 PT netto zusätzlich halte ich nicht für übertrieben. Eher: Das ist nur der technische Umbau. Der eigentliche Preis ist die neue Sync-/Reset-/Recovery-Komplexität.

Option B wäre ein eigener Architektur-Release. Für BPM-109.01 ist das zu groß und kollidiert mit den Stop-Punkten aus ADR-058/BPM-109.

---

## 5. Konkrete Langzeit-Risiken bei Option A

Option A ist richtig, aber nicht risikofrei.

| Risiko | Beispiel | Absicherung |
| --- | --- | --- |
| Verwaiste Stammdaten-ID | `building_part_id` zeigt auf gelöschten Bauteil | App-Level Delete Guard / Soft-Delete-Policy |
| Segmenttyp deaktiviert/gelöscht | `segment_type_id` nicht mehr aktiv | Import blockieren bei fehlendem Pflicht-Segmenttyp; Anzeige mit Badge |
| Reporting via `ATTACH` wird chaotisch | Ad-hoc SQL verteilt sich in UI/Modulen | Nur über `IPlanLookupService` / Repository, kein UI-SQL |
| Sync-Reihenfolge | `bpm.db` auf Gerät B hat Stammdaten noch nicht, PlanCache schon rebuildet | Lookup toleriert NULL/unknown display, Revalidate beim nächsten Start |
| Stale Display-Werte | Bauteil wurde umbenannt, PlanCache zeigt alten Namen | IDs sind maßgeblich; denormalisierte Werte nur Debug/Export |
| Stammdaten-Löschung trotz Planbezug | User löscht Haus H1, Pläne behalten H1-ID | Vor Löschung PlanRefs prüfen oder nur Soft Delete mit Warnung |
| Projektübergreifende Reports werden mühsamer | „Alle aktuellen Schalungspläne über alle Projekte" | Später manifest-/indexbasierter Report oder expliziter Reporting-Cache |

Das sind beherrschbare Risiken. Sie sind kleiner als die Risiken einer Konsolidierung vor V1.

---

## 6. Härtung für Option A

Ich würde Option A mit fünf kleinen, verbindlichen Härtungen ergänzen.

### 6.1 DDL korrigieren: Cross-DB-FKs entfernen
In `planmanager.db` bleiben die Spalten, aber ohne `FOREIGN KEY`:
```sql
-- Soft Reference auf bpm.db.building_parts(id)
building_part_id TEXT,
-- Soft Reference auf bpm.db.building_levels(id)
building_level_id TEXT,
-- Soft Reference auf bpm.db.segment_types(id)
segment_type_id TEXT NOT NULL,
```
Und in der Doku explizit:
```text
Cross-DB Reference:
Diese Spalte referenziert logisch bpm.db.<table>(id).
SQLite erzwingt diesen FK nicht. Gültigkeit wird durch Import-/Lookup-Services validiert.
```

### 6.2 Harte FKs innerhalb `planmanager.db` beibehalten
Diese bleiben echte Constraints:
* `plan_revisions.document_id → plan_documents.id`
* `revision_file_links.revision_id → plan_revisions.id`
* `revision_file_links.file_id → plan_files.id`
* `plan_document_segments.document_id → plan_documents.id`
* `plan_revision_events.revision_id → plan_revisions.id`
* `plan_context_links.target_document_id → plan_documents.id`
* `plan_context_links.target_revision_id → plan_revisions.id`
* `import_actions.import_id → import_journal.id`
* `import_action_files.action_id → import_actions.id`

Das ist der relevante Konsistenzkern des Planarchivs.

### 6.3 App-Level Delete Guard
Vor Soft-Delete von `building_parts`, `building_levels`, `segment_types`:
```pseudo
CanDeleteBuildingPart(id):
    if bpm.db building_parts.is_deleted already true:
        return true
    foreach project in projects:
        if exists planmanager.db for project:
            if plan_documents where building_part_id = id and is_deleted = 0:
                return Block("Bauteil wird in PlanManager verwendet")
    return true
```
Für V1 reicht auch eine konservative Variante:
> Wenn PlanManager-DBs existieren und nicht geprüft werden können, Löschung blockieren oder Warnung + explizite Bestätigung.

Da `building_parts` und `building_levels` in `bpm.db` bereits Soft-Delete-/Sync-Spalten haben, ist „nicht hart löschen" ohnehin konsistent mit ADR-050.

### 6.4 Import-Time Validation
Beim Schreiben in `planmanager.db`:
```pseudo
ResolveBuildingPart(segmentValue, projectId):
    id = bpmDb.FindBuildingPart(projectId, segmentValue)
    if id exists:
        return id
    else:
        return null + warning

ResolveSegmentType(segmentTypeId):
    if bpmDb.SegmentTypeExists(segmentTypeId, includeInactive=true):
        return ok
    else:
        block import if segment is identity/folder/index relevant
```
Das passt zur bestehenden Segmenttyp-Entscheidung: Soft-Delete, Profile referenzieren IDs, Missing-ID blockiert Auto-Import bei kritischen Profilteilen. ADR-056 beschreibt genau diese Health-Logik.

### 6.5 Revalidate Command
Ein kleiner Wartungsservice reicht:
```pseudo
PlanReferenceHealth CheckPlanReferences(projectId):
    attach/open bpm.db
    open project planmanager.db
    check plan_documents.building_part_id exists in bpm.building_parts where is_deleted=0
    check plan_documents.building_level_id exists in bpm.building_levels where is_deleted=0
    check plan_document_segments.segment_type_id exists in bpm.segment_types
    check building_part_aliases.building_part_id exists in bpm.building_parts where is_deleted=0
    return warnings, not auto-fixes
```
Kein Migrationstool. Kein Legacy-Fallback. Nur Diagnose + klare Reset-/Korrekturhinweise.

---

## 7. Reporting und `ATTACH`

`ATTACH` ist für read-only Reporting/Lookup okay, aber ich würde es kapseln.

Nicht: UI baut selbst ATTACH + SELECT
Sondern:
```csharp
IPlanLookupService.GetPlansForContext(projectId, buildingPartId, buildingLevelId, atUtc)
IPlanReferenceHealthService.Check(projectId)
```
Die Implementierung darf intern entweder: (1) beide DBs getrennt öffnen, (2) `ATTACH` read-only verwenden, (3) oder später einen Reporting-Cache nutzen. Die Module dürfen das nicht wissen. Damit bleibt die DB-Grenze austauschbar.

---

## 8. Korrektur an DB-SCHEMA Kap. 6.7

Ich würde vor BPM-109.01 eine kleine ADR-/Doku-Ergänzung machen, nicht ein großes neues ADR für Konsolidierung.

### Minimaler Doku-Patch
In Kap. 6.7 vor der DDL:
```text
Hinweis Cross-DB-Referenzen:
planmanager.db liegt pro Projekt separat. Spalten, die auf bpm.db-Tabellen zeigen
(building_parts, building_levels, segment_types), sind Soft References.
SQLite-FK-Constraints werden nur innerhalb derselben DB-Datei definiert.
Validierung erfolgt service-seitig beim Import, Lookup und bei Stammdaten-Soft-Delete.
```
Und in der DDL diese Foreign-Key-Zeilen entfernen:
* `FOREIGN KEY (building_part_id) REFERENCES building_parts(id)`
* `FOREIGN KEY (building_level_id) REFERENCES building_levels(id)`
* `FOREIGN KEY (segment_type_id) REFERENCES segment_types(id)`
* `FOREIGN KEY (building_part_id) REFERENCES building_parts(id)` in `building_part_aliases`

Die Kommentare bleiben, aber als „Soft Reference".

### Beispiel
```sql
building_part_id TEXT,  -- SoftRef bpm.db.building_parts(id), NULL wenn nicht gemappt
building_level_id TEXT, -- SoftRef bpm.db.building_levels(id), NULL wenn nicht gemappt
```

---

## 9. Würde Option B später jemals Sinn ergeben?

Ja, aber nur wenn sich die Produktannahmen ändern. Option B wird interessant, wenn:
* PlanManager-Daten nicht mehr rebuildbarer Cache sind, sondern rechtlich/fachlich primäre Records.
* Plan-Kontextlinks aus Bautagebuch/Fotos produktiv synchronisiert werden müssen.
* Mehrgerätebetrieb dieselbe Planhistorie identisch sehen muss, nicht nur aus Manifest/Dateisystem rekonstruieren.
* Projektübergreifende Planreports zentrale Kernfunktion werden.
* Sync-Engine tabellenweise Ausnahmen sauber unterstützt und getestet ist.

Dann wäre aber nicht nur „Tabellen in `bpm.db` verschieben" nötig, sondern ein neues ADR:
> PlanArchive as System of Record statt PlanManager Cache

Das ist aktuell nicht die Architektur.

---

## 10. Empfehlung

BPM-109.01 soll auf **Option A** gebaut werden. Die zu treffende Entscheidung ist nicht „wir akzeptieren schwächere Integrität", sondern:
> Wir erzwingen Integrität dort, wo die Daten denselben Lebenszyklus haben.
> Über Lebenszyklusgrenzen erzwingen wir Integrität durch Services, Health Checks und Soft-Delete-Guards.

Das ist für eine offline-first Desktop-App mit lokalem SQLite, zentraler Stammdatenbank und pro Projekt rebuildbarem Cache die bessere Architektur. Option B würde den sauber getrennten Reset-/Recovery-/Sync-Schnitt aufbrechen, nur um vier FK-Constraints zu bekommen. Das lohnt nicht.

---

## ✅ Einigkeit
* Das Drei-Ebenen-Modell bleibt entschieden.
* `planmanager.db` ist aktuell bewusst pro Projekt und lokal.
* `bpm.db` enthält Stammdaten wie `building_parts`, `building_levels`, `segment_types`.
* SQLite erzwingt keine echten Foreign Keys über getrennte DB-Dateien.
* Die DDL in Kap. 6.7 muss vor BPM-109.01 korrigiert werden.
* Harte FKs innerhalb `planmanager.db` bleiben wichtig.
* Cross-DB-Bezüge brauchen Service-Validierung und Health Checks.

## ⚠️ Widerspruch
* Ich widerspreche der aktuellen DDL-Form, die harte FKs aus `planmanager.db` auf `bpm.db` suggeriert.
* Ich widerspreche Option B als Vor-V1-Maßnahme: Konsolidierung bringt zu wenig Nutzen für zu viel Sync-/Reset-/Blast-Radius-Kosten.
* Ich würde kein Konsolidierungs-ADR vor BPM-109.01 eröffnen, sondern ein kleines ADR-058-Addendum oder ADR-059 „Cross-DB Soft References" erstellen.
* Ich würde `building_part_aliases` kritisch prüfen: Wenn es Stammdaten-Mapping ist, könnte die Tabelle langfristig eher in `bpm.db` gehören. Für Foundation Slice kann sie aber in `planmanager.db` bleiben, solange sie als projektbezogener PlanManager-Mapping-Cache verstanden wird.

## ❓ Rückfragen
1. Wird `building_part_aliases` als PlanManager-spezifisches Import-Mapping verstanden oder als allgemeines Stammdaten-Alias für alle Module?
2. Soll `plan_context_links` langfristig wirklich in der nicht gesyncten `planmanager.db` bleiben, wenn Bautagebuch/Fotos später selbst synchronisiert werden?
3. Gibt es in ADR-053 bereits eine tabellenweise Exclude-Policy für Sync, oder wäre Option B dort komplett neue Arbeit?
4. Soll `ATTACH bpm.db` in `PlanManagerDatabase` selbst passieren oder in einem separaten `PlanLookupService`, der beide Repositories orchestriert?
5. Soll das Löschen von `building_parts`/`building_levels` bei vorhandenen Planreferenzen blockiert werden oder als Soft-Delete mit Warnbadge in PlanManager erlaubt bleiben?
