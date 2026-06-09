# Review Runde 1 — Claude-Analyse (Stufe A)

## Zu ChatGPT r1
Stark + korrekt. Highlights:
- **Echter Bug** (verifizieren): Feldkey-Bruch `plan_number`/`plan_index` (FileParseService) vs `plannumber`/`planindex` (ImportWorkflowService). Betrifft auch Strategie B (Index-Matching braucht korrekte Plannummer/Index).
- „Ordner = Stammdaten-Name, nicht Alias" + „Auto-Suggest statt Auto-Learn" + „Decide/Preview-Schritt fehlt" → alle richtig.
- Kernpunkt: heutige Feldextraktion ist positionsbasiert (FileParseService nutzt segDef.Position); `RecognitionRule.Method=regex` matcht nur Profil, schreibt keine Named Captures in extractedFields → das ist der technische Bruch.

## Der User-Pivot (das eigentlich Wichtige)
Herbert zweifelt, ob Voll-Auto-Erkennung den Aufwand wert ist (Planbezeichnungen immer unterschiedlich). Vorschlag: **manuelle Erstaufnahme der Pläne, danach nur noch erkennen ob selber Plan / neuer Index**.

**Claude-Einschätzung: Pivot ist klug, Strategie B ist sehr wahrscheinlich der bessere MVP.**

| | A (Auto-Recognition) | B (Erstaufnahme + Matching) |
|---|---|---|
| Schwierigster Teil | Maschine extrahiert Identität aus Chaos — jedes Mal | Mensch setzt Identität — einmal pro Plan |
| Täglicher Schmerz (Updates) | fehleranfällig | Maschine: selber Plan / neuer Index → zuverlässig |
| Zuverlässigkeit | prinzipiell gedeckelt | hoch (Vergleich gg. bekannte Menge + MD5) |
| Aufwand | groß (007.02/.03+109.06+OCR) | klein, nutzt Schema v2.0 |

- Arbeitsteilung: Mensch macht den mehrdeutigen Teil einmal, Maschine den repetitiven jedes Mal. Realer Polier-Schmerz = Wiederholung, nicht Ersteinrichtung.
- Foundation Slice ist Substrat für B: plan_documents=erfasste Pläne; MD5=selber Plan; RevisionDecisionService+Supersede=neuer Index; document_key/released_at unverändert.
- Auto-Extraktion (Regex/Alias/OCR) wird vom Rückgrat zum optionalen **Vorbefüller** der Erstaufnahme (Assist, entscheidet nichts allein). = ChatGPTs „deterministischer Kern + Assist", Kern jetzt = menschlich bestätigte Erstaufnahme.
- Einschränkung: auch B braucht leichtes Matching (Plannummer = stabilster Token); Fehler sind sicher (Fallback manuell).

## Entscheidungspunkte → ask_user_input
1. Strategie A vs B vs „A-vs-B in CGR r2 gegenüberstellen".
2. Feldkey-Bug (plan_number vs plannumber) — sofort verifizieren/fixen?
