# CGR-2026-04-17-bpm-082-segment-recognition — Segment-basierte Plantyp-Erkennung

**Thema:** Architektur-Refactor des Recognition-Systems im PlanManager. Ablösung der positionsblinden `prefix`/`contains`-Methoden durch eine neue `segment`-Methode mit explizitem `SegmentPosition`-Feld in der `RecognitionRule`. `regex` bleibt als Fallback für Spezialfälle erhalten.

**Zeitraum:** 2026-04-17 bis 2026-04-21
**Ursprungs-Chat:** BauProjektManager (Phase 1) Teil 20
**Bezug:** [BPM-082](https://app.clickup.com/t/86c9dc5aq) — Parent-Task mit 9 Subs (082.01–082.07)
**Status:** Nachträglich archiviert (Review fand vor Aktivierung des `chatgpt-review`-Skills statt)

---

## Hintergrund

Im `DocumentTypeRecognizer` wurde ein Architektur-Gap entdeckt: `RecognitionRule` speicherte nur `Method` (prefix/contains/regex) + `Pattern`, **aber keine Segment-Position**.

**Bug-Szenario:**
- Profil "Bauprotokoll" mit Rule `{method: "contains", pattern: "PROT"}`
- Match auf `PROJ-PROT-2025-01.pdf` (gewollt) ✓
- Match auf `RK-PROTOKOLL-EG.pdf` (NICHT gewollt) ✗

Der Wizard suggerierte dem User positionsgenaue Erkennung ("Segment 1 = PROT"), der Code prüfte aber nur `fileName.Contains("PROT")` — ein Leak zwischen UX-Versprechen und Code-Implementierung.

Das ursprüngliche Backlog-Item BPM-007 (`prefix/contains/regex`-Muster) wurde pausiert, weil eine UI-Erweiterung das Grundproblem nicht löst. Stattdessen wurde BPM-082 angelegt: Architektur-Refactor mit segment-basierter Erkennung.

---

## Runden-Übersicht

### Runde 1 — Grundsatz: segment-Methode + Datenmodell

- **Artefakte:** [r1/](./r1/)
- **Fokus:** Bewertung des `segment`-Vorschlags, `SegmentPosition: int?` in `RecognitionRule`, AND-Semantik bei Multi-Rules, Default-Methoden-Wahl, Verhältnis zu `regex`
- **Stand:** Claude-Prompt + Claude-Analyse archiviert. ChatGPT-Antwort wird nachträglich ergänzt.

### Runde 2 — Vertiefung: Cache-Key, MatchesSegment, Logging

- **Artefakte:** [r2/](./r2/)
- **Fokus:** Cache-Key-Granularität, schlanke `MatchesSegment`-Implementierung, Logging-Verhalten bei invaliden Rules, Warnung als Hinweis (nicht Hard-Fail)
- **Stand:** Claude-Prompt + Claude-Analyse archiviert. ChatGPT-Antwort wird nachträglich ergänzt.

### Runde 3 — Konsens: Finale Tabelle + offene Detail-Fragen

- **Artefakte:** [r3/](./r3/)
- **Fokus:** `FileNameParser`-Rückgabetyp, Logging-Deduplikation (ProfileManager.Load verwirft ganzes Profil), ADR-Struktur (ADR-010 erweitern statt neuer ADR-050), Profil-Minimum-Validierung, finaler 15-Punkte-Konsens
- **Stand:** Claude-Prompt + Claude-Analyse archiviert. ChatGPT-Antwort wird nachträglich ergänzt.

---

## Kernergebnisse (finaler Konsens nach Runde 3)

| # | Entscheidung |
|---|---|
| 1 | Neue Methode `"segment"` als Default — `prefix`/`contains` werden komplett entfernt (keine Legacy-Tolerierung) |
| 2 | `regex` bleibt als Fallback für Spezialfälle (Statik-Nummernkreise, Dateien ohne saubere Delimiter) |
| 3 | `RecognitionRule.SegmentPosition: int?` als persistiertes Feld |
| 4 | AND-Semantik bei Multi-Rules eines Profils (alle Rules müssen matchen) |
| 5 | `SchemaVersion 3` für `profiles.json` |
| 6 | `RecognitionContext` als Hilfstyp mit `BaseName` und `Segments` (mit `Position` + `RawValue`) |
| 7 | `FileNameParser` als gemeinsame Tokenisierungsquelle für Wizard und Recognizer |
| 8 | Variable-Segment-Warnung im Wizard (Schritt 5) wenn Position als unsicher erkannt wird |
| 9 | ADR-010 erweitern, **kein neuer ADR-050** (ChatGPT-Empfehlung Runde 3) |
| 10 | `ProfileManager.Load` verwirft ganzes Profil bei ungültiger Rule → Recognizer sieht nie invalide Rules → Logging-Dedup nicht nötig |
| 11 | `MatchesSegment` schlank halten: Tokenisieren, Position-Check, Equals(IgnoreCase) — keine Sonderlogik |
| 12 | Profil-Minimum-Validierung erweitert: `Id`, `DocumentTypeName`, `Tokenization != null`, `Recognition.Count > 0` |
| 13 | Cache-Key auf Profil-Ebene (nicht pro Rule) |
| 14 | Warnung bei variablem Segment als UI-Hinweis, kein Hard-Fail |
| 15 | Doc-Pflege als eigener Sub 082.07 nach Code-Subs (082.01–082.06c) |

---

## Resultierende Tasks

**BPM-082 Subs (9 in finaler Reihenfolge):**

| # | Sub | Thema |
|---|---|---|
| 1 | 082.01 | Datenmodell + IsValid + SchemaVersion 3 |
| 2 | 082.02 | Recognizer + RecognitionContext + segment-Methode |
| 3 | 082.06a | Core-Tests (segment, regex, IsValid) |
| 4 | 082.03 | Wizard speichert segment-Rules |
| 5 | 082.04 | Wizard-UI: Segment-Anzeige + Variable-Warnung |
| 6 | 082.05 | Legacy prefix/contains entfernen |
| 7 | 082.06b | Wizard-/Persistence-Tests |
| 8 | 082.06c | Load-Toleranz-Tests |
| 9 | 082.07 | Doc-Pflege (ADR-010, BACKLOG #20, GLOSSAR, DB-SCHEMA, Architektur) |

**Pausiert:** BPM-007 (prefix/contains/regex-Muster) — Regex-Subtask 007.01 bleibt erledigt; 007.02-007.04 werden nach Abschluss von BPM-082 neu bewertet.

---

## Bezug zu Architektur-Dokumenten

- **ADR-010** — wird in Sub 082.07 erweitert: segment als Default, AND-Semantik, SchemaVersion 3
- **DB-SCHEMA.md** — `RecognitionRule`-Definition um `segmentPosition: int?` erweitern
- **GLOSSAR.md** — Neue Einträge: `SegmentPosition`, `RecognitionContext`, `FileStem`, `Tokens`
- **BACKLOG.md #20** — Umformulierung von "prefix/contains/regex Muster" auf "Segment-basierte Erkennung + regex-Fallback"

---

## Hinweis zur Archivierung

Dieser Review wurde nachträglich am 2026-05-04 archiviert. Die ChatGPT-Antworten in r1/r2/r3 (`02-chatgpt-response.md`) sind Volltexte, die der User aus dem ChatGPT-Verlauf manuell nachgeliefert hat. Claude-Prompts und Claude-Analysen sind aus dem Chat-Verlauf von Teil 20 rekonstruiert — der genaue Wortlaut kann leicht abweichen, aber die fachlichen Punkte stimmen mit den ChatGPT-Antworten überein.

## Bonus: 10 Test-Szenarien aus Runde 3

ChatGPT hat in Runde 3 zehn konkrete Test-Szenarien aus echten Baustellen-Dateinamen geliefert. Diese sind direkt als Test-Vorlage für 082.06a/b/c und als Wizard-Beispiel-Repertoire nutzbar:

1. **ÖWG Dobl** — `202401_P_011_...` Polierplan vs. `202401_D_...` Detailplan
2. **ÖWG Dobl** — `202401_DZW_B13_P_...` darf nicht mit Polier-Profil matchen
3. **ÖWG Dobl** — `5998-201_Wände_EG_H64.dwg` braucht Regex-Fallback für Statiknummernkreis
4. **ÖWG Dobl** — `5998-002a_Bodenplatte_Teil_2.pdf` Index in Segment, Parser darf nicht stolpern
5. **Office Lights Grenzgasse** — `PP_GG_04_..._Index D.pdf` Index nicht als Erkennungssegment
6. **Office Lights Grenzgasse** — `S-111-VA-02_ 2.OG Wände...` gemischte Delimiter
7. **ESS St. Georgen** — `PP01-1Wohnanlage St. Georgen...` Leerzeichen NICHT als Delimiter
8. **ESS St. Georgen** — `24101__301_Bodenplatte EG.dwg` Doppel-Unterstrich + CollapseRepeatedDelimiters
9. **Heiligenkreuz** — `209001_P_PO02_Haus1_...` Lehrbuchfall (Projektnummer + Plantyp + Serie)
10. **Schlosspark / Smart City** — `21005_101_AP_H1_GR_U1_03...` und `241_3-PP-KS-1-EG-02-D.pdf` reale Varianten

Volltexte in `r3/02-chatgpt-response.md` Abschnitt "4. Zehn konkrete Szenarien aus zufälligen Baustellen".
