# Claude-Analyse Runde 6

**Datum:** 2026-04-25
**Zu:** ChatGPTs Antwort in `02-chatgpt-response.md`

---

## Gesamteindruck

Sehr starke Antwort, deutlich substantieller als Runde 5. Drei besonders wertvolle Elemente:

1. Das **Exit-Kriterium** für B+C macht aus einer weichen Architektur-Wahl eine messbare Bedingung
2. Der **README-Refactor kapitelweise** ist konkret genug zum Umsetzen, nicht nur Prinzip
3. Die **Frühwarn-Indikatoren** sind operationalisierbar — keine vagen Hinweise

## Was ich uneingeschränkt übernehme

### Q5-Präzisierung

ChatGPT hat recht: "lies INDEX.md" ist mehrdeutig. Datei-Lesen kann je nach Umgebung trotzdem cc-steuerung implizieren. "Was steht in INDEX.md zur Konfliktpaar-Delegation?" ist saubere Negation.

### Q6–Q10 Ergänzung

Insbesondere Q9 ("cc" allein) und Q10 ("ProfileWizard View bauen" ohne cc) füllen die kritischen Edge-Cases:
- Q9: Modalität ohne WAS → ask_user_input_v0 statt fachliches Raten
- Q10: WPF-Begriff ohne Modalität → reines code-erstellen

### INDEX-Regel Granularität

"Router darf nicht zwischen ihnen wählen" war zu absolut. ChatGPTs Version mit Smoke-Test-Pflicht und Exit-Kriterium ist besser:

- INDEX definiert Zielverhalten, nicht Router-Implementierung
- Smoke-Test-Pflicht macht die Regel testbar
- Exit-Kriterium verbindet INDEX mit der Variante-A-Fallback-Option

### Smoke-Eval-Verteilung

Die Asymmetrie nach Konfliktträchtigkeit ist richtig. cc-steuerung mit 6 Multi-Trigger-Cases ist proportional zum Risiko. 75–85 Queries gesamt sind handhabbar.

### Trennung Katalog vs. Run-Snapshots

`evals/smoke-all-skills.md` als stabiler Katalog + `evals/runs/smoke-YYYY-MM-DD.md` als Run-Snapshots ist sauberer als alles in einer Datei. Verhindert dass Katalog mit jedem Run länger wird.

### Fragilitäts-Top-2 Begründung

Memory und Frühphase als Top-2 wären falsch gewählt — ChatGPT begründet überzeugend warum cc-steuerung und Tracker-Anker kritischer sind: cc-steuerung blockiert sofort, Tracker-Anker erzeugt Projektsteuerungs-Schaden. Memory wirkt langsam, Frühphase ist gut sichtbar (Schlüsselwörter triggern manuelle Erkennung).

### Fresh-Model-API als Nicht-Blocker

Die billige 80%-Variante (manueller Run im frischen Claude.ai-Chat) ist näher am echten System als ein API-Harness. Der API-Pfad lohnt erst, wenn man Regressionen automatisch verhindern will.

## Diskutabel

### README-Refactor: Kapitel 8 "Refactor-Historie"

ChatGPT empfiehlt kürzen oder auf CHANGELOG verweisen. Plausibel, aber: README-Refactor-Historie hat Onboarding-Wert ("hier seht ihr was wir gelernt haben"). Vorschlag: Stark kürzen auf einen Absatz pro Phase, Detail-Verweis auf CHANGELOG. Komplettes Entfernen würde institutionelles Gedächtnis verlieren.

### CI/Cron-Schedule

Wöchentlicher Cron (`0 7 * * 1`) ist zu früh für ein Solo-Projekt. Vorschlag: nur `workflow_dispatch` (manuell) plus Trigger bei Push auf `main`, das die Description-Frontmatter eines Skills ändert. Cron erzeugt Wochen-Noise ohne Mehrwert.

### Vollmodus in Smoke-Eval

ChatGPT fragt zurück ob Vollmodus-Cases rein sollen. Mein Vorschlag: zunächst strikt Blind/Description-only. Vollmodus kann später als zweite Run-Variante über denselben Katalog laufen. Das hält Katalog kompakt und macht Run-Vergleich Blind vs. Vollmodus möglich.

## Antworten auf ChatGPTs Rückfragen

### 1. Skill-Ladeindikatoren in Claude.ai sichtbar?

Nicht zuverlässig. Claude.ai zeigt manchmal "Skill aktiviert"-Hinweise, aber nicht konsistent und nicht für alle Skills. Smoke-Eval muss primär verhaltensbasiert bewertet werden — UI-Indikator nur als Sekundär-Beleg.

### 2. Vollmodus-Fälle in Smoke-Eval?

Zunächst strikt Blind. Vollmodus später als zweite Run-Variante über denselben Katalog. Begründung oben.

### 3. Echte Fehlrouting-Golden-Cases?

Aus den letzten Sessions kein bekannter Fall. Aber: in dieser Sitzung würde ich sammeln, was Herbert während Runde 5/6 als reale Beobachtung erinnert. Dieser Punkt sollte explizit in Stufe A abgefragt werden.

## Konkrete Nächste Schritte

ChatGPTs Schlussurteil ist klar: "README-Refactor sollte vor Feature-Freigabe passieren, aber nicht als großer Rewrite. Smoke-Eval ist wichtiger als README-Kosmetik."

Damit ergibt sich eine Reihenfolge:

1. **CHANGELOG-Drift schließen** (P0, 5 Min — nachholen v0.17.8 bis v0.18.0)
2. **INDEX-Regel §9 cc-steuerung** ergänzen (P0, 10 Min — ChatGPTs Formulierung)
3. **README-Refactor Kapitel 9 + Two-Place** (P0, 30 Min — größter Normativitätsverstoß)
4. **code-erstellen Description WPF/XAML** ergänzen (P0, 5 Min)
5. **`evals/smoke-all-skills.md` schreiben** (P0/P1-Grenze, 1–2 Stunden)
6. **30 kritischste Cases manuell in frischem Chat laufen** (P1, 1 Stunde)
7. **FAILs in [ARCH-OPEN] oder ClickUp** (P1)
8. **API-Harness GitHub Actions** — erst nach 1–2 Wochen Beobachtung, wenn Bedarf entsteht

## Empfehlung

Zwei mögliche Wege:

**Weg 1 — Voll konsequent:**
Schritte 1–6 in dieser Sitzung umsetzen, danach BPM-Feature-Arbeit freigeben. Geschätzt 3–4 Stunden.

**Weg 2 — Pragmatisch geteilt:**
Schritte 1–4 jetzt (P0, ~1 Stunde). Schritte 5–7 als nächste Sitzung "Skill-System Stabilisierung" vor BPM-Feature-Arbeit.

Mein Vorschlag: **Weg 2**. Reduziert Risiko dass die Sitzung zu lang wird, gibt Herbert Pause nach jedem Block, und P0-Items sind eindeutig — weniger Diskussionsbedarf.

## Fragen für Stufe A

- Weg 1 oder Weg 2?
- Reihenfolge der P0-Items unverändert oder anders priorisieren?
- Echte Fehlrouting-Golden-Cases aus letzten Sessions erinnerlich?
- Smoke-Eval direkt als nächste Sitzung planen oder nach 1–2 BPM-Feature-Tagen?
