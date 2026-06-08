# Claudes Analyse — Runde 1 (parallel zu ChatGPT, vor dessen Antwort)

**Hinweis:** Diese Analyse wurde **vor** ChatGPTs Antwort erstellt, auf expliziten Wunsch des Users („mach das einmal du und einmal chatgpt"). Damit lässt sich später vergleichen, wo Claude und ChatGPT übereinstimmen und wo nicht.

---

## TL;DR

Status Quo reicht **für reines Sortieren** (V1-Scope), aber **nicht für die geplanten Module Bautagebuch / Foto / Vorlagen** (post-V1). Drei harte Lücken müssen geschlossen werden, **bevor BPM-056 (Bautagebuch) angefangen wird** — sonst wird die ganze Modul-Welt um einen String-Match (`LIKE '%h1%'`) gebaut, was technisch fragil und semantisch falsch ist.

Mein Vorschlag: **Schema-Erweiterung in 4 Schritten** (Document-Trennung + Metadata-KV + Status-Zeitstempel + Cross-Link), zeitlich **VOR** BPM-056 aber **NACH** den V1-Sperrposten (BPM-080/081/006). Personen-Tage-Schätzung: 3–5 Tage Code + 1 Tag Doc/Tests.

---

## 1. Status-Quo-Befund — wo der Schuh drückt

### 1.1 Was funktioniert (V1-Scope abgedeckt)

- `plan_revisions` + `plan_files` + `revision_file_links` decken die **Plan-Identität und Datei-Verknüpfung** sauber ab
- `revision_status = current/archived` mit `ux_plan_revision_current`-Index erzwingt **Eindeutigkeit pro Dokument-Key**
- `import_journal` + `import_actions` + `import_action_files` geben **Audit der Imports** (was wurde wann verschoben)
- `DocumentKeyBuilder.Build()` (BPM-082) baut Identity deterministisch — funktioniert

→ **Für den reinen Sortier-Use-Case ist das Schema solide.**

### 1.2 Drei Lücken, die für post-V1-Module hart wehtun

| # | Lücke | Konsequenz |
|---|---|---|
| L1 | **Extrahierte Segmente nicht als Spalten/FKs** | Filter „Haus=H1, Geschoss=EG" geht nur per `LIKE '%h1%'` auf `document_key` — fragil, kein Index nutzbar, kollidiert bei Werten wie `h10`, `h11` |
| L2 | **Keine Zeitstempel für Status-Wechsel** | Frage „was war am 15.06.2025 aktuell" nicht beantwortbar. `created_at`/`updated_at` mischen Erst-Import und spätere Cache-Updates |
| L3 | **Keine Cross-Modul-Verknüpfung** | Bautagesbericht/Foto/Vorlage haben keine saubere FK zu `plan_revisions` — entweder String-Ref auf `document_key` (Lock-in) oder FK auf `revision_id` (broken bei Index-Update) |

### 1.3 Was im aktuellen Schema fehlerhaft konzipiert ist (kleinere Punkte)

- `document_type_id` in `plan_revisions` ist als FK zur Profil-ID dokumentiert — aber Profile sind heute JSON-Dateien, nicht DB-Tabelle. Die FK-Beziehung ist nicht real durchsetzbar (das ist explizit BPM-092 als Aufgabe gelistet, aber post-V1).
- `last_import_id` in `plan_revisions` ohne FK-Constraint — sollte zu `import_journal.id` referenzieren.
- `revision_status` als TEXT mit zwei Werten — wäre als CHECK-Constraint sauberer.

→ Diese drei sind aber **Pflege-Kleinigkeiten**, kein Architektur-Thema.

---

## 2. Was die Industrie macht (Kurzfassung der Vorab-Recherche)

Aus den vorigen Chat-Suchen über Procore/Aconex/think project!/Plangrid:

**Drei-Ebenen-Modell ist Standard:**
```
Document (logisch) ──┬── Revision (Versionsstand) ──┬── File (physisch)
                     │                              │
                     │                              └── (1:n: Sammel-DWG)
                     │
                     └── Attributes (Tags/Metadaten — KV oder feste Spalten)
```

**Lifecycle:** ISO 19650 nutzt Suitability-Codes (S0=WIP, S1=Shared, S2=Coordinated, S3=Authorized, S4=Construction). Procore nutzt schlichter `Current/Superseded/Archived`. Jeder Wechsel mit Zeitstempel.

**Cross-Referenzen:** Procore hat „Linked Drawings to RFIs/Submittals", Aconex hat „Document Relationships", think project! hat „Cross-References".

**Metadaten-Pattern in der Praxis:**
- **Procore:** feste Spalten für TOP-Felder (`drawing_number`, `drawing_title`, `discipline`) + Custom Attributes als KV
- **Aconex:** vollständig flexible Metadata Fields, vom Projekt-Admin konfigurierbar
- **think project!:** Hybrid — Standard-Felder (ISO 19650-Kodierung) + custom Metadaten

→ **Mein Read:** Hybrid (Variante B + A kombiniert) ist die häufigste Industrie-Lösung.

---

## 3. Mein Vorschlag — Schema-Erweiterung v2.3

### 3.1 Vier neue/umgebaute Tabellen

```sql
-- NEU: Logisches Dokument (über Revisionen hinweg)
CREATE TABLE plan_documents (
    id TEXT PRIMARY KEY,                  -- ULID
    project_id TEXT NOT NULL,
    document_key TEXT UNIQUE NOT NULL,    -- ersetzt String in plan_revisions
    document_type_id TEXT,                -- (FK später, wenn BPM-092 done)
    title TEXT,                           -- optional, aus Beschreibung
    target_folder TEXT NOT NULL,
    -- Feste FK-Spalten für die wichtigsten Filter-Dimensionen:
    building_part_id TEXT,                -- FK building_parts (Haus)
    building_level_id TEXT,               -- FK building_levels (EG, OG1...)
    component TEXT,                       -- Bauteil-Bezeichnung (Wände, Decke...) — frei, da nicht in Stammdaten
    created_at TEXT NOT NULL,             -- wann erstmals importiert
    archived_at TEXT,                     -- wann komplett abgelöst (alle Revisionen archiviert)
    -- + 6 Sync-Spalten (ADR-050)
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (building_level_id) REFERENCES building_levels(id)
);

CREATE INDEX idx_plan_documents_filter
ON plan_documents(building_part_id, building_level_id, document_type_id)
WHERE archived_at IS NULL;

-- UMGEBAUT: Revisionen verweisen auf Dokument statt String-Key zu tragen
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,            -- NEU: FK plan_documents
    revision_code TEXT NOT NULL,          -- "0", "A", "B", "05" — war plan_index
    status TEXT NOT NULL CHECK(status IN ('current', 'superseded')),
    current_from TEXT NOT NULL,           -- NEU: wann wurde diese Revision aktuell
    superseded_at TEXT,                   -- NEU: wann durch nächste ersetzt (NULL = aktuell)
    drawing_date TEXT,                    -- Datum aus Plankopf wenn lesbar (heute null)
    received_date TEXT NOT NULL,          -- wann importiert
    last_import_id TEXT,
    relative_directory TEXT NOT NULL,     -- bleibt, aus target_folder + Hierarchie gebaut
    -- + 6 Sync-Spalten
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (last_import_id) REFERENCES import_journal(id)
);

CREATE INDEX idx_revisions_timetravel
ON plan_revisions(document_id, current_from, superseded_at);

-- NEU: Metadaten als KV (für nicht-Stammdaten-Felder)
-- Beispiel: bauabschnitt, stiege, zone, plankategorie (GR/SC/AN), discipline
-- Stammdaten-Felder (haus, geschoss) sind als FK-Spalten in plan_documents — NICHT hier
CREATE TABLE plan_document_attributes (
    document_id TEXT NOT NULL,
    attribute_key TEXT NOT NULL,          -- segment_types.id (z.B. "bauabschnitt", "plankategorie")
    attribute_value TEXT NOT NULL,
    PRIMARY KEY (document_id, attribute_key),
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (attribute_key) REFERENCES segment_types(id)
);

CREATE INDEX idx_attrs_key_value
ON plan_document_attributes(attribute_key, attribute_value);

-- NEU: Cross-Modul-Links
CREATE TABLE plan_document_links (
    id TEXT PRIMARY KEY,
    source_module TEXT NOT NULL,          -- "diary_note", "photo", "rfi", "task", "report"
    source_id TEXT NOT NULL,
    target_document_id TEXT NOT NULL,     -- IMMER auf Document, NICHT Revision
    revision_resolution TEXT NOT NULL,    -- "current_at_time" | "fixed_revision"
    fixed_revision_id TEXT,               -- nur bei "fixed_revision" gesetzt
    snapshot_time TEXT,                   -- bei "current_at_time": wann wurde gelinkt
    link_type TEXT NOT NULL,              -- "reference" | "attachment" | "auto_match"
    linked_at TEXT NOT NULL,
    linked_by TEXT,
    FOREIGN KEY (target_document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (fixed_revision_id) REFERENCES plan_revisions(id)
);

CREATE INDEX idx_links_source ON plan_document_links(source_module, source_id);
CREATE INDEX idx_links_target ON plan_document_links(target_document_id);

-- BLEIBT unverändert: plan_files, revision_file_links, import_journal, import_actions, import_action_files
```

### 3.2 Was bleibt erhalten

- `plan_files` — physische Datei-Tabelle bleibt
- `revision_file_links` — n:m bleibt (jetzt zwischen `plan_revisions.id` und `plan_files.id`)
- `import_journal` + `import_actions` + `import_action_files` — komplett unverändert

### 3.3 Beispiel-Query für Bautagebuch-Use-Case

„Welche Polierpläne waren am 15.06.2025 für Haus 1 EG aktuell?"

```sql
SELECT pd.*, pr.id AS revision_id, pr.revision_code, pf.relative_path
FROM plan_documents pd
JOIN plan_revisions pr ON pr.document_id = pd.id
JOIN revision_file_links rfl ON rfl.revision_id = pr.id AND rfl.is_primary = 1
JOIN plan_files pf ON pf.id = rfl.file_id
WHERE pd.building_part_id = (SELECT id FROM building_parts WHERE short_name = 'Haus 1' AND project_id = ?)
  AND pd.building_level_id = (SELECT id FROM building_levels WHERE name = 'EG' AND building_part_id = pd.building_part_id)
  AND pd.document_type_id = 'polierplan-arch'
  AND pr.current_from <= '2025-06-15T23:59:59'
  AND (pr.superseded_at IS NULL OR pr.superseded_at > '2025-06-15T23:59:59');
```

Lesbar, indexgestützt (`idx_plan_documents_filter` + `idx_revisions_timetravel`), kein LIKE.

### 3.4 Beispiel-Query für Foto-Modul

„Foto wurde an Position X aufgenommen — zeige Pläne für Haus 2 OG3, aktuell zum Foto-Zeitpunkt"

```sql
SELECT pd.*, pr.revision_code
FROM plan_documents pd
JOIN plan_revisions pr ON pr.document_id = pd.id AND pr.status = 'current'
WHERE pd.building_part_id = ? AND pd.building_level_id = ?
  AND pr.current_from <= ? AND (pr.superseded_at IS NULL OR pr.superseded_at > ?);
```

---

## 4. Impact auf bestehenden Code

| Komponente | Impact | Aufwand |
|---|---|---|
| `DocumentKeyBuilder` | bleibt — `document_key` ist weiterhin der UNIQUE-Wert in `plan_documents` | gering |
| `ImportWorkflowService.AnalyzeAsync()` (7-Stufen) | Stage 5+7 erweitert: schreibt nicht nur `plan_revisions`, sondern auch `plan_documents` (upsert) und `plan_document_attributes` | mittel |
| `RevisionDecisionService` | Stage 6 unverändert — operiert weiter auf `document_key` | gering |
| `ImportPlanBuilder` | Stage 7 leicht angepasst: bei NEW erzeugt es Document + Revision; bei UPDATE_NEWER_INDEX setzt es `superseded_at` der alten + erzeugt neue Revision | mittel |
| Repository-Klassen (`PlanManagerDatabase`) | neue Methoden `UpsertDocument`, `WriteAttributes`, `MarkSuperseded` | mittel |
| **Tests** | bestehende 238 Tests müssen grün bleiben, neue Tests für Document/Attribute/Link-Logik dazu | mittel-hoch |
| **Domain-Models** | neue Klassen `PlanDocument`, `PlanDocumentAttribute`, `PlanDocumentLink` | gering |

**Größenordnung:** 3–5 Tage Code + 1 Tag Doc/Tests. Größer wenn Auto-Learn für Stammdaten (siehe 5) mit reinkommt.

---

## 5. Auto-Learn für Stammdaten — meine Empfehlung: „fragen, nicht magisch"

**Szenario:** Importer extrahiert `Haus = "H1"`, in `building_parts` für dieses Projekt gibt es nur `Haus 1` (mit Leerzeichen).

**Drei Optionen:**

| Verhalten | Vorteil | Nachteil |
|---|---|---|
| **Auto-Create** | Stammdaten füllen sich von selbst | Stammdaten verschmutzen mit Varianten („H1", „H 1", „Haus 1") |
| **Fragen im Import-Preview** | User behält Kontrolle, Daten sauber | Mehr Klicks beim ersten Import einer Variante |
| **Gar nichts (FK bleibt NULL)** | Einfachst | Filterung funktioniert nicht — Bautagebuch sieht den Plan nicht |

**Mein Vorschlag:** **Fragen mit Fuzzy-Match-Hint**:
- Wenn extrahierter Wert exakt in Stammdaten → FK auto-setzen, kein Dialog
- Wenn Fuzzy-Match (Levenshtein < 2 oder Normalize-Match) → im Import-Preview Hint anzeigen: „Soll `H1` als `Haus 1` zugeordnet werden? [Ja, Alias merken] [Nein, neu anlegen]"
- Wenn keine Stammdaten → Im Import-Preview „Neu anlegen?" Dialog mit Default „Ja"
- Aliase pro Projekt persistieren in `building_parts.aliases` JSON-Spalte oder `building_part_aliases`-Tabelle

→ Daten sauber, User-Eingabe nur beim erstmaligen Auftauchen einer Variante.

---

## 6. Was wir explizit NICHT bauen sollten

Wo ich Gefahr sehe, Enterprise-Patterns zu übernehmen, die für 5-User-Polier-App ungesund sind:

- ❌ **Suitability-Codes S0-S4 (ISO 19650)** — Procore/Aconex haben das, aber Polier-Welt arbeitet binär (verbindlich/überholt). Status `current/superseded` reicht.
- ❌ **Transmittals als eigene Entität** — wer hat welche Revision wann erhalten. Großbau-Workflow, irrelevant für Innenfirmen-Sync mit 5-6 Usern.
- ❌ **Vollständig flexible Custom Metadata Fields pro Projekt** (Aconex-Stil) — die Felder kommen aus dem `segment_types`-Katalog (BPM-108). Kein neues Konfig-System.
- ❌ **OCR-Plankopf als Persistenz-Treiber** — POST-V1 ohnehin, hier nicht relevant. Auto-Population aus OCR kann später kommen, ohne Schema-Refactor.
- ❌ **Versionssprünge / Branching** wie in Git — Pläne sind linear (current → superseded → next current). Kein Merge.
- ❌ **Workflow-Status-Maschine** (Draft → Review → Approved → Issued) — das ist Großbau-Workflow. Polier importiert, was er bekommt, er reviewt nicht.

---

## 7. Reihenfolge und Tickets

Mein Vorschlag für die Umsetzungs-Reihenfolge:

1. **BPM-080.05 fertigstellen** (Wizard Schritte 3–5) — V1-Sperrposten, läuft schon
2. **BPM-081 fertigstellen** (ImportPreviewDialog) — V1-Sperrposten
3. **BPM-006 ProjectDetailView** — V1-Sperrposten
4. **V1-Release** — sauberer Schnitt
5. **NEW: BPM-NNN Plan-Archiv-Architektur v2.3** (dieser Review) — POST-V1, VOR allen konsumierenden Modulen
   - Sub-Tasks:
     - .01 Schema-Erweiterung + Migrations-Strategie (Frühphase: Datei löschen)
     - .02 Domain-Models (PlanDocument, PlanDocumentAttribute, PlanDocumentLink)
     - .03 ImportWorkflowService-Erweiterung (Stage 5+7)
     - .04 Auto-Learn-Mechanismus für Stammdaten (Fuzzy + Alias)
     - .05 IPlanLookupService als öffentliche API für Module
     - .06 Tests + Doc-Pflege (DB-SCHEMA, PlanManager.md, neues ADR)
6. **BPM-092** (`recognition_profiles` in DB) — kann parallel oder danach
7. **BPM-056 Bautagebuch** — nutzt v2.3
8. **BPM-057 Foto** — nutzt v2.3
9. **BPM-061 Vorlagen** — nutzt v2.3

---

## 8. Was ich von ChatGPT hören will

Konkrete Punkte, wo ich unsicher bin und eine zweite Meinung wichtig:

1. **Reicht Variante B (FK-Spalten für haus/geschoss + KV-Tabelle für Rest), oder ist Variante A (alles in KV-Tabelle) doch sauberer?** Mein Bauchgefühl ist Hybrid, aber Aconex macht alles KV.

2. **`plan_document_links` jetzt einplanen oder erst mit BPM-056?** Mein Bauchgefühl: jetzt, damit Schema-Migration einmalig. Aber YAGNI-Argument greift.

3. **Status-Modell `current/superseded` mit 2 Zeitstempeln vs. eigene `plan_revision_history`-Tabelle** — wann ist welche Variante besser?

4. **Auto-Learn-Stammdaten — fragt ChatGPT auch nach Fuzzy oder hätte ChatGPT einen anderen Ansatz?**

5. **Reihenfolge: VOR oder NACH V1-Release?** Mein Bauchgefühl: nach (V1 ist V1, klare Linie). Aber wenn BPM-080.05 schon Schema v2.3-Felder im Wizard anzeigen würde, könnte das saubererr sein.

6. **Was übersieht Claude?** — Offenes Feedback ist erwünscht.

---

## 9. Risiken

- **Frühphasen-Reset:** Bei Schema v2.3 müssen User die `planmanager.db` löschen — Verlust aller bisherigen Import-Historie. Das ist OK (kein Produktivbetrieb), aber für DevTool-User (Herbert selbst) ärgerlich.
- **Test-Coverage:** 238 Tests grün — bei Schema-Refactor wahrscheinlich 10–30 betroffen. Aufwand nicht unterschätzen.
- **Sync-Vorbereitung:** ADR-050 verlangt 6 Sync-Spalten auf neuen Tabellen. Wenn das hier sauber gemacht wird, ist die Tabelle sync-fähig ohne weiteren Refactor.
- **API-Lock-in:** `IPlanLookupService` als öffentliche Schnittstelle muss durchdacht sein — Module werden sich darauf verlassen.

---

# Phase 2 Stufe A — Vergleich nach ChatGPTs Antwort

ChatGPTs Antwort bestätigt die Kernthese und ergänzt sie an mehreren Stellen konkret. Hier die strukturierte Auswertung.

## Volle Einigkeit (kein Diskussionsbedarf)

| Punkt | Beide |
|---|---|
| Status Quo reicht für V1, nicht für Bautagebuch/Foto/Vorlagen | ✓ |
| Document/Revision/File als Drei-Ebenen-Modell | ✓ |
| Hybrid statt reinem KV/JSON | ✓ |
| Zeitreise zwingend vor BPM-056 | ✓ |
| Keine Migrations-Logik (Frühphase) | ✓ |
| Keine Suitability-Codes / Transmittals / Branching | ✓ |
| Auto-Learn nur mit User-Bestätigung, kein magisches Anlegen | ✓ |
| `document_key` bleibt als Natural Key, aber nicht als Cross-Modul-Identität | ✓ |

## ChatGPT-Verbesserungen — alle stark, ich übernehme sie

| # | Claudes Erstvorschlag | ChatGPT-Verbesserung | Bewertung |
|---|---|---|---|
| 1 | `plan_document_attributes` | **`plan_document_segments`** | ✓ besser — verhindert Custom-Field-Scope-Drift |
| 2 | `revision_status` mit `archived` | **`superseded`** + optional `rejected` | ✓ semantisch sauberer („archived" = Storage, „superseded" = fachlich abgelöst) |
| 3 | Nur 2 Zeitstempel | **+ `plan_revision_events` als Minimal-Audit** | ✓ minimaler Mehrwert (warum-wurde-ersetzt) bei niedrigem Aufwand |
| 4 | `plan_document_links` | **`plan_context_links`** | ✓ Name passt besser (Link ist Kontext-Beziehung) |
| 5 | `revision_resolution = current_at_time` vs. `fixed_revision` als Option | **Snapshot IMMER auf `revision_id` festziehen, `context_time` zusätzlich speichern** | ✓ Argument unwiderlegbar: rückwirkende Korrekturen würden sonst alte Berichte verändern. **Das ist der wichtigste inhaltliche Punkt.** |
| 6 | `building_parts.aliases` als JSON | **`building_part_aliases` als Tabelle** | ✓ relational sauber, indexierbar |
| 7 | `plan_document_segments` ohne `segment_key` | **+ `segment_key` Denormalisierung** | ✓ Debug-/Export-Hilfe ohne Performance-Kosten |
| 8 | `project_id` in pro-Projekt-DB redundant | **Trotzdem mitführen** | ✓ kostet nichts, hilft bei Sync/Export/Debug |
| 9 | Plan-Archiv parallel zu BPM-092 möglich | **Plan-Archiv klar VOR BPM-092** | ✓ Profil-DB ist administrativ, blockiert nicht fachlich |
| 10 | 3–5 PT geschätzt | **6–8 PT** | ✓ ehrlicher mit Tests + Doku + Lookup-Service |
| 11 | Auto-Learn mit Fuzzy-Vorschlag im Importer | **Nur exakte Normalisierung initial, Fuzzy als spätere Stufe** | ✓ schlanker, weniger UI-Komplexität in Stufe 1 |

## Punkte ohne Widerspruch — beide sind gleich

Ein Punkt, den ChatGPT als „Widerspruch" markiert, ist tatsächlich keiner:

- ChatGPT: „building_part_id/building_level_id gehören in `plan_documents`, nicht `plan_revisions`"
- Mein Erstvorschlag in Abschnitt 3.1: hatte das bereits in `plan_documents` stehen

→ Wir waren einig, ChatGPT hat es wahrscheinlich anhand der Variante-B-Formulierung im Prompt missverstanden. **Konsens da.**

## ChatGPT-Rückfragen — meine Position dazu

1. **„Dürfen Haus/Geschoss/Bauteil zwischen Revisionen ändern, oder = neues Dokument?"**
   → Mein Read: ein Plan, der nachträglich für ein anderes Haus umgewidmet wird, ist fachlich ein neuer Plan (neuer `document_key`). Haus/Geschoss/Bauteil sind **dokument-stabil**. → Bestätigt ChatGPTs Schema-Entscheidung (FKs in `plan_documents`).

2. **„Bautagesbericht-Snapshot fix vs. dynamisch?"**
   → ChatGPT empfiehlt fix, ich stimme zu. Aber: Frage an Herbert, weil das ein UX-Entscheid ist.

3. **„`document_type_id` aus DB oder JSON?"**
   → Aktuell aus JSON (`recognition_profiles/<id>.json`). BPM-092 würde das nach DB migrieren. Für Plan-Archiv-Ticket reicht JSON-Referenz vorerst (kein FK-Constraint).

4. **„`planmanager.db` lokal Cache bleiben?"**
   → Ja, gemäß ADR-053 (Sync-Strategie): Daten wandern bei Sync in `bpm.db`, lokale `planmanager.db` bleibt Cache. Plan-Archiv-Persistenz gehört **in `planmanager.db`**.

5. **„Projektspezifische Geschoss-Aliase im aktuellen Ticket?"**
   → Mein Vorschlag: jetzt nur `building_part_aliases`, `building_level_aliases` erst wenn echter Use-Case auftaucht (YAGNI).

## Mein konsolidierter Schema-Vorschlag (Claude + ChatGPT)

Übernehme ChatGPTs Tabellennamen und Schema-Details. Drei Punkte, an denen ich Claude-Position behalte:

- **`rejected`-Status**: Ich plädiere für **weglassen** initial. Binär `current/superseded` ist genug. Wenn später echter Use-Case → einfach erweiterbar via CHECK-Constraint-Update. Aber bin offen.
- **Auto-Learn Stufe 1**: ChatGPTs minimaler Ansatz (exakt + Preview-Warnung, kein Fuzzy) → für Stufe 1 ja. Fuzzy als Stufe 2 in eigenem Subtask, NICHT im Erst-Ticket.
- **Aufwand 6–8 PT**: akzeptiere ChatGPTs Schätzung als ehrlicher.

## Risiken/Konsequenzen, die noch nicht diskutiert wurden

- **Plan-Index-Manifest `.bpm/plan-index.json`** — ChatGPT erwähnt, dass es angepasst werden muss. Konkret: das ist Bestandsmanifest für Sync-Erkennung. Muss zukünftig `document_id` exportieren statt nur `document_key`. Eigener kleiner Subtask.
- **`ImportPlanBuilder` muss zwischen 4 Operationen unterscheiden** (NewDoc / NewRev / Supersede / FileLink). Das ist eine echte Komplexitätssteigerung im Code, der ChatGPT zurecht hervorhebt.

## Bereit für Phase 2 Stufe B (Folgeprompt Runde 2)?

Aus meiner Sicht: **Konsens groß genug für Sign-off**. Drei Punkte würden ich vor Folgeprompt mit Herbert klären (siehe ask-user-input-v0).

