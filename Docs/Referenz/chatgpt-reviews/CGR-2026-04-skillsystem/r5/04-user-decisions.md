# User-Entscheidungen Runde 5

**Datum:** 2026-04-25
**Entscheider:** Herbert Schrotter

---

## Stufe-A-Antworten

### 1. Reifegrad: Wie weiter?

**Entscheidung:** Stop — ChatGPT in Runde 6 nachbohren.

**Konsequenz:**
- Keine sofortige P0-Umsetzung in dieser Sitzung
- Keine BPM-Feature-Arbeit-Freigabe in dieser Sitzung
- Stattdessen Runde 6 mit Vertiefungsfragen vor der Go-Entscheidung

### 2. cc-steuerung-Asymmetrie — wie auflösen?

**Entscheidung:** Variante B + C — INDEX-Regel als globale Routing-Norm + Smoke-Test, dann beobachten.

**Konsequenz:**
- Keine identische Modalitäts-Spiegelung in 8 Fachskills (Variante A verworfen — würde Asymmetrie aus Phase 5.7 brechen)
- Globale Routing-Regel "cc-steuerung läuft parallel zu Fachskills, nie statt" als Kandidat für INDEX.md
- Smoke-Eval-Cases zur Verifikation der Multi-Trigger-Fähigkeit

### 3. README-Normativität?

**Entscheidung:** ChatGPTs Vorschlag — README rein Onboarding/Verweis.

**Konsequenz:**
- README normative Inhalte werden auf reine Verweise reduziert
- INDEX.md bleibt strikte Source of Truth für alle Invarianten
- Verweis-Banner-Lösung verworfen
- Folge-Refactor: README-Drift-Schutz ist hartes Refactor, nicht weicher Verweis

---

## Konsequenz für Runde 6

ChatGPT bekommt Folgeprompt mit Fokus auf:

1. **Vertiefung des cc-steuerung-Multi-Trigger-Themas:**
   - Wie testet man Multi-Trigger objektiv?
   - Welche konkreten Test-Cases als Smoke-Eval?
   - Welche Formulierung für die globale INDEX-Regel?

2. **README-Refactor-Strategie:**
   - Welche Abschnitte aus README sind normativ und müssen weg?
   - Welche bleiben als Onboarding?
   - Wie sieht ein "rein Verweis-orientierter" README aus?

3. **Smoke-Eval-Konkretisierung:**
   - Was genau in `evals/smoke-all-skills.md`?
   - 3+2+2 pro Skill — wie auswählen?
   - Wie gewichten konfliktträchtige Skills (audit, mockup, doc-pflege, cc-steuerung)?

4. **Verbleibende Reife-Fragen:**
   - Was würde ChatGPT zwischen "Go" und "echtem Stabil-Status" als zusätzliche Kriterien sehen?
   - Welche versteckten Fragilitäten beobachtet ChatGPT in der ersten Woche Feature-Arbeit als kritischste?
   - Risiko-Priorisierung — was zuerst beobachten?

5. **Verifikations-Frage zur Selbst-Simulation:**
   - Wie könnte ein Fresh-Model-API-Run ohne neues Projekt aufgesetzt werden?
   - Lohnt sich der Aufwand vor BPM-Feature-Arbeit oder erst nach 2–4 Wochen Beobachtung?

---

## Nicht entschieden — wandert nach Runde 6

- P0-Konkrete-Umsetzung (CHANGELOG, INDEX-Invariante 8, code-erstellen-Description) wartet
- BPM-Feature-Arbeit-Start wartet
- Smoke-Eval-Inhalt wartet
