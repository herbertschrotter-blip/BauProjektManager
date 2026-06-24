# ChatGPT-Review-Index

Übersicht aller ChatGPT-Review-Serien im BPM-Projekt. Reviews sind strukturierte mehrstufige Diskussionen zwischen Claude und ChatGPT (GPT-5.4) zu Architektur-, Dokumentations- und Methodik-Fragen.

## ID-Schema

```
CGR-<YYYY-MM-DD>-<thema>-r<runde>
```

- `CGR` = ChatGPT-Review
- `YYYY-MM-DD` = Vollständiges Startdatum der Serie (Tag der ersten Runde)
- `<thema>` = Kurzbezeichnung (kebab-case)
- `r<runde>` = Rundennummer (r1, r2, r3, …)

Themenbezeichnungen (Enum):
- `skillsystem` — Skill-System-Architektur, Trigger, Description-Schema
- `docs-refactor` — Dokumentationsstruktur, Frontmatter, INDEX, Quickloads
- `bpm-architektur` — BPM-Code-Architektur (PlanImport, SQLite-Wahrheit, Domain/Infra)
- `datenschutz-dbschema` — DSGVO, DB-Schema, IDs, Whitelist, external_call_log
- `cc-vs-dc` — Trennung Claude Code (CC) vs Desktop Commander (DC), Workflow-Schwellen, Skill-Aufteilung
- `datenarchitektur-sync` — Sync-Mechanismus für Solo-Multi-Device + Multi-User Phase 3, Industrie-Standard vs Eigenbau
- `bpm-082-segment-recognition` — Segment-basierte Plantyp-Erkennung im PlanManager, Refactor des DocumentTypeRecognizer
- `segmenttyp-architektur` — DB-basierte Segmenttyp-Verwaltung mit Gruppen, ID-Referenz, Soft-Delete (BPM-108)
- `plan-archiv-architektur` — Plan-Persistenz-Architektur: Document/Revision-Trennung, Metadata-Tags, Status-Historie/Zeitreise, Cross-Modul-Lookup (Bautagebuch/Foto/Vorlagen)
- `plan-erkennung` — Recognition v2: zuverlässige Plan-Identität + Ordner-Sortierung aus unregelmäßigen Dateinamen + variablen Schreibweisen (Extract→Normalize→Alias→Learn + OCR/KI-Abgrenzung)

## Ablage-Konvention

Pro Serie ein Ordner `CGR-<YYYY-MM-DD>-<thema>/` mit:
- `README.md` — Serie-Übersicht, Runden-Zusammenfassung, finale Entscheidungen
- Pro Runde ein Unterordner `r<N>/` mit 4 nummerierten Dateien:
  - `01-claude-prompt.md` — Claudes Prompt an ChatGPT
  - `02-chatgpt-response.md` — ChatGPTs Antwort im Canvas
  - `03-claude-analysis.md` — Claudes Einschätzung/Reaktion
  - `04-user-decisions.md` — Herberts Antworten und Entscheidungen

**Volltexte sind on-demand.** Nicht jede historische Runde muss initial vorhanden sein. Zukünftige Reviews (gesteuert über `chatgpt-review`-Skill) werden vollständig archiviert.

## Aktueller Stand

| CGR-ID | Thema | Runden | Status | Ursprungs-Chat | Kernergebnis |
|--------|-------|--------|--------|----------------|--------------|
| CGR-2026-04-17-bpm-082-segment-recognition | Segment-basierte Plantyp-Erkennung | r1–r3 | Abgeschlossen (nachträglich archiviert) | Teil 20 | Refactor des `DocumentTypeRecognizer`: neue `segment`-Methode mit `SegmentPosition`, `prefix`/`contains` entfallen, `regex` als Fallback. Resultiert in BPM-082 mit 9 Subs. ADR-010 wird erweitert (kein neuer ADR). Inkl. 10 Test-Szenarien aus realen Baustellen. |
| CGR-2026-04-22-skillsystem | Skill-System-Refactor | r1–r6 | Abgeschlossen | Teil 22 ff. | Phase 1–6 Refactor done (v0.18.0). r5+r6 Audit erzeugte 14 ClickUp-Tasks (P0–P4) als Stabilisierungs-Roadmap. P0.1 done v0.18.1. |
| CGR-2026-04-22-docs-refactor | Docs-System-Refactor | r1–r3 | on-demand | "Docs und Skill refactoring (Teil 1)" | Frontmatter + INDEX-Router + AI-Quickload statt separate Briefs |
| CGR-2026-04-22-bpm-architektur | PlanImport-Architektur | mind. r2 | on-demand | "Architektur-Dokumentation analysieren" | SQLite-Wahrheit auflösen, ProjectPaths.Root relativ, PlanImportFacade |
| CGR-2026-04-22-datenschutz-dbschema | DSGVO + DB-Schema | r2–r3 | on-demand | "Skills für Kern-Dokumentation" | ADR-037 einheitliches ID-Schema TEXT mit Präfix, Whitelist registry.json |
| CGR-2026-04-29-cc-vs-dc | CC vs DC Workflow + Skill-Aufteilung | r1–r2 | Runde 2 abgeschlossen | Teil 34 | _Implementierung offen_ |
| CGR-2026-04-30-datenarchitektur-sync | Datenarchitektur & Sync-Strategie (Windows-only Multi-User) | r1–r7 | Abgeschlossen | Session BPM-009 Tief-Audit | **Resultat:** Windows-only Stack (PostgreSQL 17 + ASP.NET Core 10 + Caddy) auf Windows-VPS für Phase 0/1 (5-6 User in eigener Firma, ~12€/Monat Strato VC 2-8). IBpmSyncClient + Pull/Push + ASP.NET Identity. Spike 0 (ProjectDatabase syncfähig) als erster Code-Schritt. Drei Pivots: R5 Modell-A→B, R6 Linux→Windows, R7 Solo→Multi-User Live-Sync. Resultiert in ADR-053. |
| CGR-2026-06-08-plan-archiv-architektur | Plan-Archiv-Architektur (Document/Revision-Trennung, Metadata-Tags, Zeitreise, Cross-Modul-Lookup) + r3 DB-Grenze | r1–r3 | ✅ **Abgeschlossen** | Teil 41 (v0.28.52) → Teil 42 | **Sign-off r2 + r3.** r3-Ergebnis: DB-Grenze geklärt — 2 DBs behalten (System-of-record `bpm.db` + rebuildbarer per-Projekt-Cache `planmanager.db`), Cross-DB-Bezüge als **Soft References** (kein FK über DB-Datei-Grenze), keine Konsolidierung vor V1. Doku als **ADR-058-Addendum** + DDL-Fix Kap. 6.7. `building_part_aliases` → `bpm.db` (zentral, harter FK). Stammdaten-Delete = Soft-Delete + Warnbadge. Offen: `plan_context_links`-Heimat bei BPM-056-Sync. Drei-Ebenen-Modell (plan_documents/plan_revisions/plan_files) + plan_document_segments (KV) + plan_revision_events (Audit) + plan_context_links (fixed_revision Snapshot pflicht) + building_part_aliases. **Foundation Slice (`.01–.04` + `.05a` Interface-Stub) vor V1**, Rest (`.05`/`.06`/`.07`) post-V1. BPM-080.05 komplett pausiert bis Schema fertig. 11 ChatGPT-Verbesserungen R1 + 4 Korrekturen R2 übernommen. Aufwand 8,5–10,5 PT. Resultiert in ADR-058 + ClickUp-Issue **BPM-109** Plan-Archiv-Persistenz v2 (8 Subtasks angelegt). |
| CGR-2026-05-12-segmenttyp-architektur | Segmenttyp-Verwaltung (BPM-108) — DB-basiert, Gruppen, ID-Referenz, Soft-Delete | r1–r3 | Abgeschlossen | Teil 39 (v0.28.42) | **Sign-off in r3.** Zwei-Schichten-Modell (`fieldTypeId` + `SemanticRole`) statt Enum-Ersatz. Schema v4 + Frühphasen-Reset (DevTool archiviert). `RecognitionRule` BPM-082-kompatibel unverändert. 3-Phasen-Plan A/B/C + 3 erste Commits. 17 Akzeptanzkriterien inkl. Immutable Keys (`id`+`token_key`), Built-in-Rollen seed-fix read-only, Strict Reset für PatternTemplates, Health-Gating vor Auto-Import. Built-ins voll editierbar (Name/Farbe/Gruppe/Sort/Active) mit `user_modified_*`-Flags. Custom rein dekorativ (`semantic_role = NULL`). Spatial-Built-ins: geschoss/haus/bauteil/bauabschnitt/stiege/zone/block/achse/objekt. Inline-Popover „+ Eigenes" mit Token-Vorschau. **Resultierende Implementierung: 7 Commits v0.28.44–v0.28.50 (BPM-108 done 2026-05-18, ADR-056 Phase A+B+C Implemented, 238/238 Tests grün).** |
| CGR-2026-06-22-bpm-architektur | Datei-/Ordner-Verwaltungs-Abstraktion + Konsolidierung der zwei Ordner-Wahrheiten (`document_types` vs `FolderTemplate`) | r1–r4 | ✅ **Abgeschlossen** | Teil 44 (v0.28.81), Live-Test BPM-111.05 | **Beidseitiger Sign-off nach 4 Runden.** Zwei getrennte Ergebnisse → **ADR-060** (Dateisystem-Ports: `IFileSystemReader`/`IFileSystemWriter`/`IPathService` + `LocalFileSystem`, alle Module via DI, kein direktes System.IO; + `IFileLauncher`/`IShareService` Explorer; In-Memory-Fake + Temp-Integrationstests) + **ADR-061** (Ordner-Wahrheit: DB führend, FolderTemplate nur Bootstrap, `document_types.key`+`root_relative_path`+`folder_name`, `DocumentTargetPathResolver` Fail-Fast/IDs-vor-Namen, Multi-Root, `building_levels.folder_name="{PrefixString} {Name}"`, `profile.TargetFolder` gebrochen, Journal+temp+atomic-rename+Recovery, Modell A kuratierter Index). Slice 0.1–0.6 Plan. Post-V1: eine Ring-2-Strategie pro Typ. Slice 3a geht in Slice 0 auf. |
| CGR-2026-06-09-plan-erkennung | Recognition v2 → Strategie-Fork A vs B → Radial-UI für Strategie B | r1–r3 | ✅ **Abgeschlossen** | Teil 42 (v0.28.65, nach BPM-109-Abschluss) | Auslöser: Praxis-Import Statik (5998er) → positionsbasierte Erkennung sortiert falsch (`\1`/`\KG`/`\(1)`). **r1:** Recognition v2 = eigenes Feld-Extraktionsmodell (Regex-Named-Captures statt Position); **Feldkey-Bug bestätigt** (`plan_number` vs `plannumber` → Index-Erkennung tot). **r2 Pivot:** MVP = **Strategie B** (manuelle Erstaufnahme + deterministisches MD5/Index-Matching), **A nur Assist** („B entscheidet, A schlägt vor"); Alias(109.06)+OCR aus V1-Muss raus; Lightweight-PlanNr/Index-Extractor + Feldkey-Fix bleiben V1; document_key ID-basiert. **r3:** **Radial-/Nautilus-UI als V1-Primär-Erfassungsgeste signiert** — Pending Assignments, harte Caps (3 Ringe, Plantyp≤8, Bauteil-Stufen, Bulk-Stufen), matched Updates überspringen Radial (Buckets A/B/C/D), dauerhaftes Detail-Panel + Listen-Fallback, Zielordner aus Stammdaten-Name, Undo vor+nach Import. 5 Design-Entscheidungen (Geschoss 3. Ring · Bauteil-Sort kontextbasiert · +Bauteil inline · PDF+DWG „eine Revision" · Fallback-Panel). **Resultiert in ADR-059 + Ticket-Umbau.** |

## Verweis-Konventionen

**Aus anderen Dokumenten auf ein Review verweisen:**
```
Siehe CGR-2026-04-22-skillsystem-r4 (Memory-Integration)
```

**Aus Memory-Einträgen (`[REVIEW-PENDING]`-Rubrik):**
```
[REVIEW-PENDING] CGR-2026-04-22-skillsystem — offen: Runde 5 nötig?
```

**Aus ClickUp-Tasks:** inline im Task-Text als Referenz, kein eigenes Custom-Field.

## Lifecycle

1. **Neue Runde startet** — `chatgpt-review`-Skill legt Ordner + 4 Platzhalter-Dateien an
2. **Claude-Prompt generiert** — `01-claude-prompt.md` befüllt
3. **ChatGPT antwortet** — `02-chatgpt-response.md` mit Antwort befüllt (Herbert kopiert)
4. **Claude-Analyse** — `03-claude-analysis.md` mit Einschätzung befüllt
5. **Herbert entscheidet** — `04-user-decisions.md` mit Antworten befüllt
6. **Serie abgeschlossen** — README.md der Serie mit Kernergebnissen, Links zu resultierenden ADRs/Commits
7. **Index aktualisiert** — neue Zeile oder Status-Update in dieser Datei

## Retention

- Reviews bleiben dauerhaft im Repo
- Volltexte sind ab Zeitpunkt der Skill-Aktivierung vollständig archiviert
- Vor Skill-Aktivierung durchgeführte Reviews: Kernergebnisse in Serie-README, Volltexte on-demand aus Chat-History nachrüstbar

## Verbindung zum Skill-Repo

Der Review-Prozess selbst wird durch den `chatgpt-review`-Skill im Skill-Repo gesteuert:
- `claude-skills-bpm/skills/chatgpt-review/SKILL.md`

Der Skill ist das WIE (Prozess), dieses Repo ist das WAS (Archiv).
