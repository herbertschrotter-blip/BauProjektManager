# CGR-2026-06-08-plan-archiv-architektur — Plan-Archiv-Architektur (Persistenz, Metadaten, Zeitreise)

**Thema:** Reicht das aktuelle PlanManager-Schema (`plan_revisions` + `plan_files` + `revision_file_links`) für künftige Module wie Bautagebuch, Foto, Vorlagen? Müssen wir zu einer Document/Revision-Trennung mit Metadaten-Tags und Status-Historie wechseln (Industrie-Standard Procore/Aconex/think project!)?

**Zeitraum:** 2026-06-08 (r1–r2), 2026-06-08 (r3 Nachzügler)
**Ursprungs-Chat:** BauProjektManager Teil 41 (r1–r2), Teil 42 (r3)
**Bezug:** kein bestehender BPM-Task — Plan-Archiv-Persistenz v2 entsteht aus diesem Review
**Status:** ✅ **Abgeschlossen** (r1–r2 Sign-off Drei-Ebenen-Modell; r3 Sign-off DB-Grenze: 2 DBs + Soft Reference bestätigt)

---

## Hintergrund

Im laufenden Chat ist ein Anwendungsfall aufgetaucht, der die aktuelle Persistenz-Architektur überfordert:

> *„In Zukunft bei Bautagesberichten möchte ich Pläne automatisch zuordnen können. Das, wenn ich z.B. angebe Haus 1 EG, mir als Fußnote die zu diesem Zeitpunkt aktuellen Pläne anzeigt."*

Heute speichert `plan_revisions`:
- `document_key` als verketteter Identitäts-String
- `plan_number`, `plan_index`, `document_type`
- `target_folder`, `relative_directory`
- `revision_status` (`current` / `archived`) — aber ohne Zeitstempel für den Wechsel

**Daraus extrahierte Segmentwerte (Haus, Geschoss, Bauteil, …) landen NUR in `document_key` und im Ordnerpfad — nicht als eigene Spalten oder Tabelle.**

Konsequenz: Eine Query wie „Welche Polierpläne waren am 15.06.2025 für Haus 1 EG aktuell?" geht heute nicht effizient.

---

## Runden-Übersicht

### Runde 1 — Bestandsaufnahme + Industrie-Abgleich + Schema-Skizze

- **Artefakte:** [r1/](./r1/)
- **Fokus:** Reicht das aktuelle Schema? Welche Lücken sind real? Welche Industrie-Patterns übernehmen?
- **Kernergebnis:** Konsens auf Drei-Ebenen-Modell (Document/Revision/File) + KV-Tabelle für Segmentwerte + Zeitstempel für Statuswechsel. **11 ChatGPT-Verbesserungen übernommen** (siehe unten). Herberts drei strategische Entscheidungen:
  1. Snapshot-Strategie: **nur `fixed_revision`** (ChatGPT)
  2. `rejected`-Status: **drin lassen** (ChatGPT)
  3. Reihenfolge: ⚡ **VOR V1-Release** (entgegen beiden Modell-Empfehlungen)

### Runde 2 — Sanity-Check „vor V1"

- **Artefakte:** [r2/](./r2/)
- **Fokus:** Trägt ChatGPT Herberts „vor V1"-Entscheidung mit? Welche Risiken? Konkrete Roadmap-Konsequenzen für V1-Sperrposten?
- **Kernergebnis:** ChatGPT trägt mit, aber **nur als Foundation Slice**. Herberts drei Entscheidungen Runde 2:
  1. V1-Scope: **Foundation Slice** (`.01–.04` blockierend, `.05–.07` post-V1)
  2. BPM-080.05: **komplett pausieren** bis Schema fertig (konservativer als ChatGPT empfahl)
  3. V1-Definition: **„Import stabil + Modulplattform vorbereitet"** → enthält Interface-Stub `.05a` für `IPlanLookupService`

### Runde 3 — DB-Grenze: 2 DBs + Soft Reference vs. Konsolidierung (Nachzügler vor BPM-109.01)

- **Artefakte:** [r3/](./r3/)
- **Fokus:** Beim Vorbereiten von BPM-109.01 aufgefallen: die neuen v2-Tabellen in `planmanager.db` deklarieren FKs auf `building_parts`/`building_levels`/`segment_types`, die in `bpm.db` liegen → Cross-DB-FK, von SQLite nicht erzwingbar. Option A (2 DBs behalten, Soft Reference, Claudes Empfehlung) vs. Option B (auf eine konsolidierte DB umstellen, ~5–8 PT + Stop-Punkt-Kollision). Frage: gängige Praxis + lohnt Konsolidierung?
- **Kernergebnis:** **Option A bestätigt** (ChatGPT + Claude einig). Muster ist *„System-of-record DB + rebuildable bounded cache DB"* (kein Database-per-Module — das wäre Anti-Pattern). Konsolidierung lohnt nicht: zu wenig Nutzen (4 FKs) für zu viel Sync-/Reset-/Blast-Radius-Kosten. **3 Entscheidungen Herbert:** (1) Dokumentation als **ADR-058-Addendum** + DDL-Fix in DB-SCHEMA Kap. 6.7 (Cross-DB-FK-Klauseln raus, SoftRef-Kommentare rein); (2) **`building_part_aliases` → `bpm.db`** statt planmanager.db (zentral, harter FK auf building_parts, project_id + Sync-Felder; reduziert Cross-DB-Soft-Refs von 4 auf 3); (3) Stammdaten-Löschung mit Planbezug = **Soft-Delete + Warnbadge** (Guard post-V1). **Offen markiert:** Heimat von `plan_context_links` (kein Cache, sondern autoriert) neu bewerten, wenn BPM-056-Sync kommt. 5 ChatGPT-Härtungen übernommen (DDL-Fix, harte Innen-FKs, Delete-Guard, Import-Time-Validation, Revalidate-Command + ATTACH-Kapselung in IPlanLookupService).

---

## Kernergebnisse (finaler Konsens)

### Schema-Erweiterung — Plan-Archiv v2

| Tabelle | Status | Zweck |
|---|---|---|
| `plan_documents` | **NEU** | Logisches Dokument über Revisionen hinweg; FKs für `building_part_id`, `building_level_id`; `document_key UNIQUE` |
| `plan_revisions` | **UMGEBAUT** | FK auf `plan_documents`; `revision_status` enum (`current`/`superseded`/`rejected`); `current_from`/`superseded_at` Zeitstempel |
| `plan_document_segments` | **NEU** | KV-Tabelle für extrahierte Segmentwerte (haus, geschoss, …) mit FK auf `segment_types` |
| `plan_revision_events` | **NEU** | Minimaler Audit-Trail für Statuswechsel |
| `plan_context_links` | **NEU** | Cross-Modul-Verknüpfung (Bautagebuch/Foto/Vorlagen → Revision); **immer `revision_id` snapshotten** |
| `building_part_aliases` | **NEU** | Relational (nicht JSON) für Auto-Learn-Mapping |
| `plan_files` | BLEIBT | Physische Datei |
| `revision_file_links` | BLEIBT | n:m Verknüpfung Revision ↔ Datei |
| `import_journal` / `import_actions` / `import_action_files` | BLEIBT | Import-Audit (nicht mit Revisions-Audit verwechseln) |

### V1-Sperrumfang (Foundation Slice)

**Vor V1 zwingend:**
- `.01 Schema v2`
- `.02 Domain Models + Repository`
- `.03 Pipeline-Grundgerüst` (Import schreibt Document + Revision + Segments korrekt)
- `.04 Revision-Zeitlogik` (`current_from`, `superseded_at`, `superseded`/`rejected`)
- `.05a IPlanLookupService Interface-Stub` (Vertrag, keine Implementation)
- Tests für Importfälle grün
- DB-Reset-Anweisung dokumentiert
- Doku: DB-SCHEMA.md Kap. 6, PlanManager.md Pipeline-Update, ADR-058

**Post-V1:**
- `.05 IPlanLookupService Implementation` (mit Query-Logik) → parallel zu BPM-056
- `.06 Stammdaten-Mapping mit Preview-UI`
- `.07 vollständige Doku (GLOSSAR, BACKLOG, Architektur.md)`
- `plan_context_links` aktiv nutzen
- Alias-Verwaltung UI
- Bautagebuch/Foto/Vorlagen-Integration (BPM-056/057/061)

### Stop-Punkte (definierte Rückrudern-Trigger)

| Trigger | Aktion |
|---|---|
| Schema-v2 erfordert >30% Re-Design von BPM-080.05 | Stopp, Plan-Archiv nach V1 |
| >40 Tests gebrochen + Ursachen nicht lokal | Stopp, Plan-Archiv nach V1 |
| Import-Journal/Undo wackelt | **Sofort** Stopp |
| Dateiverschiebung + DB-Commit inkonsistent | **Sofort** Stopp |
| `.01–.04` dauern >10 PT | Foundation Slice gescheitert, nach V1 schieben |

### Aufwand

**8,5–10,5 PT für V1-Foundation-Slice.**

| Block | Aufwand |
|---|---:|
| Schema + Repository + Models | 2–3 PT |
| Pipeline-Anpassung | 2 PT |
| Revision-Zeitlogik | 1 PT |
| Test-Refactor (10–40 Tests) | 1,5–2 PT |
| IPlanLookupService Interface-Stub | 0,5 PT |
| Doku/ADR/Reset-Hinweis | 0,5–1 PT |
| Puffer für lokale ungepushte Abweichungen | 1 PT |

### Übernommene ChatGPT-Verbesserungen (R1)

| # | Verbesserung |
|---|---|
| 1 | Name `plan_document_segments` statt `_attributes` |
| 2 | `superseded` statt `archived` + optional `rejected` |
| 3 | `plan_revision_events` als Minimal-Audit |
| 4 | Name `plan_context_links` statt `_document_links` |
| 5 | **Cross-Modul-Link IMMER `revision_id` snapshotten** (kritisch!) |
| 6 | `building_part_aliases` als Tabelle statt JSON |
| 7 | `+ segment_key` Denormalisierung in plan_document_segments |
| 8 | `+ project_id` in plan_documents (Sync/Debug) |
| 9 | Plan-Archiv klar VOR BPM-092 |
| 10 | Aufwand realistisch 8–10 PT statt 6–8 PT |
| 11 | Auto-Learn Stufe 1: exakt + Preview-Warnung, kein Fuzzy |

### ChatGPT-Korrekturen Runde 2

| # | Korrektur |
|---|---|
| 12 | **Foundation Slice statt voller Build** — nur `.01–.04` V1-blockierend, `.05–.07` post-V1 |
| 13 | **BPM-080.05 persistenznah pausieren** — UI/DTO-neutral kann weiter, Persistenz wartet |
| 14 | Aufwand 8–10 PT (statt 6–8) für Foundation Slice + Stub |
| 15 | **Klare Stop-Punkte definieren** (siehe Tabelle oben) |

### Herbert über ChatGPT hinaus

| # | Herbert konservativer als ChatGPT |
|---|---|
| 16 | BPM-080.05 **komplett pausieren** statt nur persistenznah — maximaler Wegwerfware-Schutz |

---

## Resultierende Tasks

**Neues ClickUp-Issue (noch anzulegen):**

**BPM-NNN — Plan-Archiv-Persistenz v2 (Foundation Slice)**

Subtasks:
- `.01 Schema v2 neu erzeugen` (V1-blockierend)
- `.02 Domain Models + Repository` (V1-blockierend)
- `.03 Pipeline-Grundgerüst` (V1-blockierend)
- `.04 Revision-Zeitlogik` (V1-blockierend)
- `.05a IPlanLookupService Interface-Stub` (V1-blockierend)
- `.05 IPlanLookupService Implementation` (post-V1, parallel zu BPM-056)
- `.06 Stammdaten-Mapping mit Preview-UI` (post-V1)
- `.07 Vollständige Doku/ADR-Erweiterungen/Tests` (post-V1)

**Resultierender ADR:**

**ADR-058 — Plan-Archiv-Persistenz** (Drei-Ebenen-Modell, `fixed_revision`-Snapshot-Pflicht, Foundation-Slice-Definition)

**Abhängigkeiten (umzubauen):**
- BPM-080.05 (Wizard) — blockiert durch `.01–.04`, pausiert komplett
- BPM-081 (ImportPreviewDialog) — blockiert durch `.01–.04`
- BPM-006 (ProjectDetailView) — kann parallel laufen (UI-Polish ohne Persistenz-Bezug)

**Post-V1-Module nutzen Lookup:**
- BPM-056 (Bautagebuch) — referenziert Foundation
- BPM-057 (Foto) — referenziert Foundation
- BPM-061 (Vorlagen) — referenziert Foundation

---

## Bezug zu Architektur-Dokumenten

- **ADR-058** (NEU) — Plan-Archiv-Persistenz, Drei-Ebenen-Modell
- **DB-SCHEMA.md** Kap. 6 — Schema v2 (planmanager.db)
- **PlanManager.md** — 7-Stufen-Pipeline + Document-Resolve-Stage
- **ADR-010** (BPM-082 Recognition) — Hinweis dass `document_key` nun FK-Bezug zu `plan_documents.id` hat
- **ADR-053** (Sync-Strategie) — `project_id`-Redundanz in `planmanager.db` dokumentieren
- **GLOSSAR.md** — Neue Begriffe (PlanDocument, PlanRevision, ContextLink, RevisionEvent, BuildingPartAlias, SegmentValue)
- **BACKLOG.md** — BPM-092 hinter Plan-Archiv reihen

---

## Lessons Learned

1. **Foundation Slice ist ein wertvolles Roadmap-Pattern** — verhindert dass Architektur-Refactors ein V1-Release zum Plattform-Release machen
2. **Cross-Modul-Links müssen Revision snapshotten** — sonst verändert rückwirkende Korrektur historische Berichte (architektur-kritisch)
3. **Wizard-Pause bei Schema-Refactor** ist günstiger als Doppel-Refactor — Herbert's konservative Variante ist die richtige
4. **Stop-Punkte vorher definieren** macht die „vor V1"-Entscheidung reversibel — kein Architektur-Block-Risiko
5. **Frühphase ist Architektur-Chance** — Schema-Bruch ohne Migration ist jetzt fast kostenlos, später teurer
