# ChatGPT-Review-Index

Übersicht aller ChatGPT-Review-Serien im BPM-Projekt. Reviews sind strukturierte mehrstufige Diskussionen zwischen Claude und ChatGPT (GPT-5.4) zu Architektur-, Dokumentations- und Methodik-Fragen.

## ID-Schema

```
CGR-<YYYY-MM>-<thema>-r<runde>
```

- `CGR` = ChatGPT-Review
- `YYYY-MM` = Jahr + Monat der Serie (Startdatum)
- `<thema>` = Kurzbezeichnung (kebab-case)
- `r<runde>` = Rundennummer (r1, r2, r3, …)

Themenbezeichnungen (Enum):
- `skillsystem` — Skill-System-Architektur, Trigger, Description-Schema
- `docs-refactor` — Dokumentationsstruktur, Frontmatter, INDEX, Quickloads
- `bpm-architektur` — BPM-Code-Architektur (PlanImport, SQLite-Wahrheit, Domain/Infra)
- `datenschutz-dbschema` — DSGVO, DB-Schema, IDs, Whitelist, external_call_log

## Ablage-Konvention

Pro Serie ein Ordner `CGR-<YYYY-MM>-<thema>/` mit:
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
| CGR-2026-04-skillsystem | Skill-System-Refactor | r1–r6 | Abgeschlossen | Teil 22 ff. | Phase 1–6 Refactor done (v0.18.0). r5+r6 Audit erzeugte 14 ClickUp-Tasks (P0–P4) als Stabilisierungs-Roadmap. P0.1 done v0.18.1. |
| CGR-2026-04-docs-refactor | Docs-System-Refactor | r1–r3 | on-demand | "Docs und Skill refactoring (Teil 1)" | Frontmatter + INDEX-Router + AI-Quickload statt separate Briefs |
| CGR-2026-04-bpm-architektur | PlanImport-Architektur | mind. r2 | on-demand | "Architektur-Dokumentation analysieren" | SQLite-Wahrheit auflösen, ProjectPaths.Root relativ, PlanImportFacade |
| CGR-2026-04-datenschutz-dbschema | DSGVO + DB-Schema | r2–r3 | on-demand | "Skills für Kern-Dokumentation" | ADR-037 einheitliches ID-Schema TEXT mit Präfix, Whitelist registry.json |

## Verweis-Konventionen

**Aus anderen Dokumenten auf ein Review verweisen:**
```
Siehe CGR-2026-04-skillsystem-r4 (Memory-Integration)
```

**Aus Memory-Einträgen (`[REVIEW-PENDING]`-Rubrik):**
```
[REVIEW-PENDING] CGR-2026-04-skillsystem — offen: Runde 5 nötig?
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
