# CGR-2026-04-skillsystem — Skill-System-Refactor

**Thema:** Komplett-Refactor des 10-Skill-Systems — Trigger-Schärfung, Description-Schema, Struktur (tracker-Zerlegung), Memory-Integration, Cross-References.

**Zeitraum:** April 2026
**Ursprungs-Chat:** "Bauprojektmanager (Phase 1) Teil 22" ff.
**Status:** Abgeschlossen (Runden 1–6 done, keine Runde 7 nötig)

---

## Ausgangslage

10 Skills im BPM-System mit heterogenen Descriptions, Catch-all-Triggers, massivem tracker-Skill (911 Zeilen), unscharfer Abgrenzung zwischen code-erstellen / doc-pflege / tracker. Ziel: objektive Trigger-Qualität + saubere Struktur.

**Zusätzlich analysierte Drittanbieter-Systeme (Runde 1):**
- OpenAI Codex Skills
- Cursor Rules
- GitHub Copilot Skills (VS Code)
- wshobson/agents (Community-Framework mit Eval-System)
- mgechev/skills-best-practices (Description-Qualität, Negative Triggers)

---

## Runden-Übersicht

### Runde 1 — Bestandsaufnahme + Diagnose
- **Artefakte:** on-demand aus Chat "Teil 22"
- **Fokus:** Trigger-Problem-Analyse pro Skill + Drittanbieter-Pattern-Vergleich
- **Kernergebnis:** Catch-all-Trigger identifiziert, Skills als zu text-zentriert erkannt, Meta-Architektur-Lücken offengelegt

### Runde 2 — Synthese: Konvergenz/Divergenz + Meta-Architektur
- **Artefakte:** on-demand aus Chat "Teil 22"
- **Fokus:** Gegeneinanderlegen der Runde-1-Analysen von Claude und ChatGPT
- **Kernergebnis:** ChatGPT-Kritik akzeptiert: Claudes Pattern-Analyse war zu textzentriert, zu wenig Routing-Architektur. Meta-Architektur-Fragen explizit adressiert.

### Runde 3 — Umsetzungsplan + Phasenstruktur
- **Artefakte:** on-demand aus Chat "Teil 22"
- **Fokus:** Konkreter Refactor-Plan mit priorisierten Phasen
- **Kernergebnis:** Ursprüngliche Phase 0 (Eval-Matrix) als zu schwer am Start erkannt → verschoben zu Phase 1a. 3 Anmerkungen von Claude als Feinschliff.

### Runde 4 — Feinschliff + Memory-Integration
- **Artefakte:** partiell archiviert
  - ✅ [r4/02-chatgpt-response.md](./r4/02-chatgpt-response.md) — ChatGPTs Antwort
  - ⏳ r4/01-claude-prompt.md — on-demand
  - ⏳ r4/03-claude-analysis.md — on-demand
  - ⏳ r4/04-user-decisions.md — on-demand
- **Kernergebnis:**
  - **5 Phasen final**: 1 / 1a / 2 / 3 / 4 / 5 (Phase 0 → 1a umbenannt)
  - **4 Memory-Rubriken** (statt 5): VERIFY, ARCH-OPEN, INFRA-TODO, REVIEW-PENDING (META-DECISION gestrichen)
  - **Lifecycle-Regel:** "Memory-Eintrag erledigt NUR bei Herbert-Bestätigung" — kein stilles Auto-Remove
  - **Eskalationsregel:** VERIFY/INFRA-TODO bei 3× Handover-Wiederauftauchen → ClickUp-Task-Hinweis im Handover
  - **tracker-Zerlegung:** 10-Schritt-Migration mit Link-Inventar + Shadow-Map
  - **Zwei-Orte-Pflege zweistufig:** Datei-Verifikation sofort + Verhaltens-Verifikation im nächsten Chat
  - **4. betroffener Skill:** tracker (Memory→ClickUp-Eskalation)

### Runde 5 — Abschluss-Audit nach Refactor-Komplettierung
- **Artefakte:**
  - ✅ [r5/01-claude-prompt.md](./r5/01-claude-prompt.md) — Audit-Prompt (5 Fokus-Punkte)
  - ⏳ r5/02-chatgpt-response.md — offen
  - ⏳ r5/03-claude-analysis.md — offen
  - ⏳ r5/04-user-decisions.md — offen
- **Fokus:** Reife-Einschätzung als Go/No-Go vor BPM-Feature-Arbeit
  - 7 Konfliktpaar-Delegationen objektiv scharf?
  - 3 Blind-Misses heilbar oder strukturell?
  - Meta-Doku (README/INDEX/CHANGELOG) Redundanz/Lücken?
  - skill-pflege-007 am richtigen Ort (skill-pflege vs. Meta-Stelle)?
  - Versteckte Fragilitäten unter Dauer-Last?
- **Kernergebnis:** offen (in progress)

### Runde 6 — Vertiefung nach Audit
- **Artefakte:**
  - ✅ [r6/01-claude-prompt.md](./r6/01-claude-prompt.md) — Folgeprompt (5 Vertiefungsfragen)
  - ✅ [r6/02-chatgpt-response.md](./r6/02-chatgpt-response.md) — ChatGPTs Antwort
  - ✅ [r6/03-claude-analysis.md](./r6/03-claude-analysis.md) — Claudes Einschätzung
  - ✅ [r6/04-user-decisions.md](./r6/04-user-decisions.md) — User-Entscheidungen
- **Fokus:** Konkretisierung vor Go-Entscheidung
- **Kernergebnis:**
  - **Q5–Q10 cc-steuerung Test-Cases** ergänzt (Q5 präzisiert: inhaltliche INDEX-Frage statt "lies INDEX.md")
  - **INDEX §9-Formulierung** finalisiert mit Smoke-Test-Pflicht + Exit-Kriterium
  - **README-Refactor-Plan kapitelweise** (Kapitel 9 raus, Two-Place/Memory/Eval auf Verweise)
  - **Smoke-Eval-Verteilung** asymmetrisch nach Konfliktträchtigkeit (~75–85 Cases gesamt)
  - **2 kritischste Fragilitäten:** cc-steuerung-Modalität + Tracker-Anker-Disziplin
  - **API-Eval kein Vorab-Blocker** — manueller 80%-Run reicht initial
  - **Weg 2 gewählt:** P0 in dieser Sitzung, Smoke-Eval als eigene nächste Sitzung

---

## Serie-Abschluss

**Status: Abgeschlossen (2026-04-25)**

Die Serie hat das Skill-System nach dem Komplett-Refactor (v0.18.0) extern validiert
und eine Stabilisierungs-Roadmap erzeugt. Alle Reviewer-Einigkeit, kein offener Streitpunkt.

### Resultierende ClickUp-Roadmap

14 Tasks in ClickUp-Liste `901522935159` (ClaudeSkills-Refactor-Plan):

- **P0 Stabilisierung** (`86c9gmj5t`) — 4 Subtasks, P0.1 done (`59c7d21` v0.18.1), P0.2–P0.4 offen
- **P1 Smoke-Eval-Komplex** (`86c9gmjyd`) — 4 Subtasks, alle offen, eigene Sitzung
- **P2 Fresh-Model-API-Eval** (`86c9gmk2m`) — 3 Subtasks, bedingt aktiviert
- **P3 Doku-Hygiene** (`86c9gmkbz`) — 2 Subtasks (Memory-Eskalation + Frühwarn-Indikatoren)
- **P4.1 Skill-Lade-Indikatoren** (`86c9gmkdt`) — Beobachtungstask
- **P4.2 Golden-Cases laufend** (`86c9gmkea`) — laufender Beobachtungstask

### Versions-Roadmap

| Version | Inhalt |
|---------|--------|
| v0.18.1 ✅ | P0.1 CHANGELOG-Drift |
| v0.19.0 | P0.2/P0.3/P0.4 + P3.1/P3.2 |
| v0.20.0 | P1 Smoke-Eval-Komplex |
| v0.21.0 | P2.1/P2.2 (bedingt) |
| v0.22.0 | P2.3 Variante-A (sehr bedingt) |

### BPM-Feature-Arbeit

Blockiert bis P0 abgeschlossen. Smoke-Eval (P1) folgt parallel oder vorher
in eigener Stabilisierungs-Sitzung. P2/P3/P4 laufen begleitend ohne Blocker.

---

## Finale Phasenstruktur (aus r4)

| Phase | Inhalt | Status (Stand 2026-04-22) |
|-------|--------|----------------------------|
| 1 | Sofort-Fixes an 4 Skills | ✅ done |
| 1a | Eval-Matrix auf bereinigter Basis | ✅ done |
| 2 | Description-Schema-Refactor (alle 10 Skills) | ✅ done |
| 3 | Struktur-Refactor (tracker, doc-pflege, code-erstellen, skill-neu, skill-pflege) | ✅ done |
| 4 | Memory-Integration (chat-wechsel, skill-neu, skill-pflege, tracker, Rubriken) | 🟡 in progress |
| 5 | Konfliktpaar-Cross-References + Abschluss-Eval | 🟡 offen |
| 6 | Anker-PoC (out-of-band hinzugekommen) | ✅ done |

Arbeitsnahe Extrakt-Version: `claude-skills-bpm/docs/skill-refactor-phases.md`

## Resultierende Commits (Skill-Repo)

| Commit | Skill | Phase |
|--------|-------|-------|
| a680cf0 | doc-pflege | 3.1 |
| 5fc3d8e | code-erstellen | 3.2 |
| d935aa4 | tracker (Progressive Disclosure) | 3.3 |
| 3e63339 | skill-neu (Initial) | 3.4 |
| c1277b9 | skill-pflege (Verweis auf skill-neu) | 3.5 |

## Resultierende ClickUp-Tasks

**Liste:** ClaudeSkills (`901522935159`)

- Phase 1 Parent + Subtasks — done
- Phase 1a Parent + Subtasks — done
- Phase 2 Parent + Subtasks — done
- Phase 3 Parent (`86c9eqndg`) + 3.1–3.5 — done
- Phase 4 Parent (`86c9eqnk6`) + 4.1–4.5 — in progress
- Phase 5 Parent (`86c9eqnrf`) + 5.1–5.8 — open
- Phase 6 Parent (`86c9eupbn`) — done

## Offene Punkte

Keine — Serie ist abgeschlossen. Folge-Arbeit läuft als ClickUp-Tasks (P0–P4)
und braucht kein weiteres ChatGPT-Review.
