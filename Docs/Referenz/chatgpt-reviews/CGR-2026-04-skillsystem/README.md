# CGR-2026-04-skillsystem — Skill-System-Refactor

**Thema:** Komplett-Refactor des 10-Skill-Systems — Trigger-Schärfung, Description-Schema, Struktur (tracker-Zerlegung), Memory-Integration, Cross-References.

**Zeitraum:** April 2026
**Ursprungs-Chat:** "Bauprojektmanager (Phase 1) Teil 22" ff.
**Status:** Runde 4 abgeschlossen, keine Runde 5 geplant (laut r4-Fazit: "Ab hier ist es Abarbeitung, nicht Analyse")

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

- Keine Runde 5 geplant — r4-Fazit: Analyse abgeschlossen
- Bei neuen Erkenntnissen während Phase 4 / 5 ggf. CGR-2026-MM-skillsystem-r5 als Nachtrag
