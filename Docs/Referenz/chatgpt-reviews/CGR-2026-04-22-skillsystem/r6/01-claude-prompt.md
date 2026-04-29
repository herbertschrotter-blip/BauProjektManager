# Folgeprompt Runde 6

## Gesprächsformat

- **CANVAS-TITEL: "Review Runde 6"**
- ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen am Ende JEDER Antwort

## Repo-Zugriff

- **Repo:** `herbertschrotter-blip/claude-skills-bpm`
- **Branch: `main`** — IMMER diesen Branch verwenden!

---

## Wo wir nach Runde 5 stehen

Herberts Entscheidungen aus Stufe A:

1. **Reifegrad-Frage:** Stop — vor BPM-Feature-Arbeit-Freigabe noch eine Vertiefungsrunde mit dir.
2. **cc-steuerung-Asymmetrie:** Variante B + C gewählt — globale INDEX-Regel + Smoke-Test, dann beobachten. Nicht Variante A (Modalitäts-Block in 8 Fachskills).
3. **README-Normativität:** Dein Vorschlag akzeptiert — README rein Onboarding/Verweis, INDEX als strikte Source of Truth. Nicht Verweis-Banner-Lösung.

P0/P1 sind noch nicht umgesetzt. Erst will Herbert Klarheit zu den unten folgenden Fragen.

---

## Aufgabe — 5 Vertiefungsfragen

### 1. cc-steuerung Multi-Trigger objektiv testen

Variante B + C ist gewählt. Konkret:

- Wie testet man **objektiv**, ob der Claude.ai-Router cc-steuerung + Fachskill **gleichzeitig** lädt?
- Welche Beobachtungs-Methode? Im Chat sichtbar (Skill-Lade-Indikator), oder nur indirekt am Verhalten messbar?
- Wenn der Router nur einen Skill lädt: welcher würde gewinnen — der mit höherem Description-Match-Score, alphabetisch erster, zuerst registrierter? Hast du Erkenntnisse aus der Anthropic-Dokumentation oder aus mgechev/skills-best-practices?

**Konkrete Test-Cases für `evals/smoke-all-skills.md` cc-steuerung-Sektion:**

```text
Q1: "cc lies INDEX.md"
    Erwartung: cc-steuerung
Q2: "cc bau die neue View für ProfileWizard"
    Erwartung: cc-steuerung + code-erstellen
Q3: "dc commit bitte für die letzte Änderung"
    Erwartung: cc-steuerung + git-commit-helper
Q4: "Claude Code soll tracker done BPM-082"
    Erwartung: cc-steuerung + tracker
Q5: "lies INDEX.md"  (ohne cc/dc-Prefix)
    Erwartung: KEIN cc-steuerung (nur Fachskill oder Antwort im Chat)
```

Sind die Erwartungen richtig? Fehlt ein wichtiger Test-Case? Hast du eine Meinung zur **Negation** (Q5: "nur lies, ohne cc")?

**Globale INDEX-Regel — Formulierungsvorschlag:**

```markdown
### 9. Modalitäts-Skill cc-steuerung läuft parallel zu Fachskills (Phase 5.7)

cc-steuerung ist KEIN konkurrierender Skill, sondern eine Ausführungs-Modalität.
Bei Triggern wie "cc", "dc", "Claude Code", "direkt auf den PC":
- Fachskill (code-erstellen, tracker, git-commit-helper, etc.) bleibt aktiv für WAS
- cc-steuerung wird ZUSÄTZLICH aktiv für WIE
- Beide Skills laufen parallel — der Router darf nicht zwischen ihnen wählen
```

Ist das die richtige Granularität, oder braucht es mehr/weniger? Was fehlt?

### 2. README-Refactor konkretisieren

README entnormativieren ist gewählt. Konkret:

- Welche Abschnitte aus dem aktuellen README (549 Zeilen, 12 Kapitel) sind **normativ** und müssen entfernt/auf Verweis reduziert werden?
- Welche bleiben **rein deskriptiv/Onboarding** und dürfen wortgleich bleiben?

Du kannst README.md auf `main` direkt lesen.

Bitte ein Vorschlag in dieser Form:

```text
Kapitel 1 "Was ist das"           → bleibt (Onboarding)
Kapitel 2 "Skill-Liste"            → bleibt (Onboarding)
Kapitel 3 "Invarianten"           → ENTFERNEN, Verweis "siehe INDEX.md §X"
Kapitel 4 "Two-Place-Pflege"      → ENTFERNEN, Verweis "siehe skill-pflege/SKILL.md"
...
```

Plus: Wie sähe ein **Verweis-Block-Pattern** aus, das überall identisch ist? Beispiel:

```markdown
> ⚠️ **Verbindlich:** Die operative Regel steht in [INDEX.md §X](INDEX.md#X).
> Dieses Kapitel gibt nur Onboarding-Kontext.
```

Reicht das, oder braucht es eine andere Konvention?

### 3. Smoke-Eval konkretisieren

Du hast 3+2+2 vorgeschlagen (3 should_trigger, 2 should_not_trigger, 2 conflict-near-misses).

- Reicht das für **alle 11 Skills**, oder brauchen die 4 baseliniert-en Skills (git-commit-helper, chatgpt-review, tracker, code-erstellen) weniger und die 7 unbaselinieren-en mehr?
- Wie wählt man die conflict-near-misses **systematisch** statt willkürlich? Aus der Konfliktpaar-Matrix? Oder aus realen Fehlrouting-Beobachtungen?
- Wie gewichten besonders konfliktträchtige Skills (audit, mockup-erstellen, doc-pflege, cc-steuerung)? Mehr Cases? Andere Verteilung (z.B. 2+1+4)?
- Wo speichert man die Ergebnisse? `evals/smoke-results-<datum>.md` als Snapshots, oder direkt im `evals/smoke-all-skills.md`?

### 4. Versteckte Fragilitäten — Risiko-Priorisierung

Du hast 4 Fragilitäten genannt: Memory-Rubriken, Tracker-Anker, cc-steuerung-Pfad-Discovery, Frühphasen-Prinzip.

- Welche zwei sind die **kritischsten** in der ersten Woche Feature-Arbeit? Begründe.
- Welche **Frühwarn-Indikatoren** kann Herbert beobachten, um die Fragilität zu erkennen, bevor sie schaden?

Beispiel: "Memory-Schatten-Backlog erkennt man, wenn ..."

### 5. Fresh-Model-API-Eval — lohnt der Aufwand?

Selbst-Simulation ist die methodische Schwäche. Ein API-Run gegen frisches Modell wäre sauber, aber teuer.

- Wie schätzt du den **Aufwand** ein (Stunden, Code-Aufwand)?
- Was wäre das **minimale Setup** das aussagekräftig wäre?
- Lohnt es sich vor BPM-Feature-Arbeit, oder erst nach 2–4 Wochen Beobachtung?
- Hast du einen Vorschlag, wie man eine **automatisierte Smoke-Eval** als CI/Cron aufsetzt (z.B. GitHub Actions + Anthropic API)?

Wenn du den Aufwand für hoch hältst: gibt es eine **billigere Variante**, die 80% der Aussagekraft liefert?

---

## Format der Antwort

- Alle 5 Fragen beantworten, in der Reihenfolge oben
- Pro Frage: kurze Einschätzung + konkrete Empfehlung + Code/Pseudocode wenn hilfreich
- Am Ende ✅/⚠️/❓-Block

Erwartung wie in Runde 5: keine Höflichkeit, konkrete Kritik. Wenn du an einer Stelle Herberts Entscheidung (Variante B+C, README-Refactor) für falsch hältst, sag es jetzt — die Entscheidung kann revidiert werden.
