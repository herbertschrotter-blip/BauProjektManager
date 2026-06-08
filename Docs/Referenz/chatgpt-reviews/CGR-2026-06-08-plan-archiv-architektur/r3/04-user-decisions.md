# Review Runde 3 — Herberts Entscheidungen

Konsens: **Option A bestätigt** (2 DBs behalten, Cross-DB-Bezüge als Soft References, harte FKs nur innerhalb derselben SQLite-Datei). Keine Konsolidierung, kein Konsolidierungs-ADR vor BPM-109.01.

## Drei Entscheidungen (Stufe A)

1. **Doc-Vehikel:** **ADR-058-Addendum** (nicht neues ADR-059). Die Soft-Reference-Entscheidung ist eine Präzisierung von ADR-058, kein eigenständiges Architektur-Prinzip-Doc. Plus DDL-Fix in DB-SCHEMA Kap. 6.7.

2. **building_part_aliases-Heimat:** **bpm.db (zentral)** — gegen Claudes Empfehlung (planmanager.db). Herbert wählt die zentrale, gesyncte Variante.
   - **Konsequenz:** Tabelle wandert aus Kap. 6.7 (planmanager.db) in den bpm.db-Teil des Schemas. Bekommt **harten FK** auf `building_parts(id)` (gleiche DB), `project_id`, sowie Sync-Felder (`created_at`/`created_by`/`last_modified_at`/`last_modified_by`/`sync_version`/`is_deleted`) gemäß ADR-050.
   - **Positiver Nebeneffekt:** reduziert die Cross-DB-Soft-References von 4 auf **3** (es bleiben `plan_documents.building_part_id`, `plan_documents.building_level_id`, `plan_document_segments.segment_type_id`).
   - ADR-058 + DB-SCHEMA müssen entsprechend angepasst werden (building_part_aliases nicht mehr in planmanager.db).

3. **Delete-Policy bei Stammdaten mit Planbezug:** **Soft-Delete + Warnbadge** (konsistent mit ADR-050/ADR-056). Löschen erlaubt, aber Warnung + Badge im PlanManager. Guard-Implementierung post-V1; jetzt nur als Invariante dokumentieren.

## Offener Punkt (dokumentiert, nicht jetzt gelöst)

- **plan_context_links-Heimat** (ChatGPT-Rückfrage 2): Die Tabelle ist KEIN rebuildbarer Cache, sondern autorierte Cross-Modul-Verknüpfung. Spannung zum „disposable cache"-Modell von planmanager.db. Für Foundation Slice bleibt sie wie in ADR-058 in planmanager.db (nur angelegt, aktiv erst mit BPM-056). **Markiert als OFFEN: Heimat neu bewerten, wenn BPM-056-Sync kommt.**

## Härtungen — Scope

- **In BPM-109.01:** DDL-Fix (Cross-DB-FK raus, SoftRef-Kommentare + Cross-DB-Hinweis), harte Innen-FKs erhalten.
- **In BPM-109.03:** Import-Time-Validation (Resolve building_part/segment_type, deckt sich mit ADR-056-Health).
- **post-V1:** App-Level Delete Guard, Revalidate Command, ATTACH-Kapselung in IPlanLookupService.
