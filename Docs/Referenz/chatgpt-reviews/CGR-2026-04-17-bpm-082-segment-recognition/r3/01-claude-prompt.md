# Runde 3 — Claude-Prompt an ChatGPT

> **Hinweis:** Dieser Prompt wurde nachträglich aus dem Chat-Verlauf von Teil 20 rekonstruiert. Es existiert eine archivierte Download-Datei `chatgpt-review-runde-3.md`, deren Inhalt teilweise rekonstruiert ist.

---

## Stand nach Runde 2

Wir haben alle 5 Detailfragen geklärt. ProfileManager-Validierung, Cache-Strategie, `MatchesSegment`-Schlankheit, `IFileNameParser`-Zentralisierung und Variable-Warnung sind in trockenen Tüchern.

In dieser Runde möchten wir die letzten 3 Punkte klären, einen Reality-Check mit echten Daten machen und dann den finalen 15-Punkte-Konsens festschreiben.

## Reality-Check mit echten Daten

Aus Herberts Planliste haben wir folgende Beispiele:

```
S-103-C_TG Wämde-Stützen-Träg + Decke ü.TG Grundriss.pdf
5998-003_Wände_KG_Teil_1.pdf
21005_104_AP_H1_GR_E2_05_Grundriss E+2.pdf
21-2094_404_A_Wände 20G_Haus 2_-_Schalung.pdf
PROJ-PROT-2025-01.pdf
RK-PROTOKOLL-EG.pdf
```

**Beobachtungen:**

1. **Delimiter sind gemischt** — Bindestrich, Underscore, Leerzeichen treten in einer Datei kombiniert auf. Der `FileNameParser` muss das können (mit der Liste `["-", "_", " "]` als Delimiter).

2. **Ein Profil pro Plantyp** — z.B. "Statik" mit Profil-ID `5998-` als Präfix. Dafür reicht die `segment`-Methode mit Position 0 = `5998`.

3. **Es gibt Statik-Pläne ohne klares Segment-Schema** (alte Pläne aus 2019, freie Nummerierung). Hierfür ist `regex` legitim als Fallback.

4. **Variable-Position-Warnung wird real wichtig:** Datei `21-2094_404_A_Wände 20G_Haus 2_-_Schalung.pdf` hat ein leeres Segment (zwischen `_-_`). Das könnte den Wizard verwirren, wenn der User Position 5 markiert.

## Drei letzte Rückfragen

### Rückfrage A — `FileNameParser`-Rückgabetyp

In deiner Runde-2-Skizze für `BuildRecognitionContext` hast du angenommen, dass er ein Objekt mit `.BaseName` und `.Segments` (mit `.Position` + `.RawValue`) zurückgibt. Soll ich diesen Typ so voraussetzen, oder vorher den tatsächlichen Rückgabetyp im Branch prüfen?

### Rückfrage B — `Log.Warning` bei ungültiger Regel beim Matchen

Bei jedem `MatchesRule`-Aufruf mit invalider Rule würde sonst pro Datei einmal geloggt — bei 100 Dateien × 10 Profile × 1 invalide Rule = 1000 Logs. Soll das Warning nur **einmal pro Recognize-Aufruf** pro Rule-Id geloggt werden (de-dupliziert), oder reichts wenn `ProfileManager.Load` das Profil eh vorher verwirft und `MatchesRule` nie eine invalide Rule sieht?

Ich tendiere zu Letzterem: Wenn Load ganze Profile verwirft, kommt im Recognizer nie eine invalide Rule an. Der Check in `MatchesRule` wäre dann nur Belt-and-Suspenders und könnte auf Debug-Level loggen.

### Rückfrage C — ADR-010 Update-Tiefe

Für 082.01 wird ADR-010 erweitert. Reicht es, die neue `segment`-Methode und die Entfernung von `prefix/contains` zu dokumentieren, oder sollte ich einen neuen ADR-XXX "Recognition-Methode segment" anlegen und ADR-010 nur querverweisen?

Ich tendiere zu Letzterem: neuer ADR (z.B. ADR-050) nur für diese Architektur-Entscheidung, ADR-010 bleibt als Grundstruktur-ADR und bekommt einen Verweis.

---

## Bitte zum Abschluss

1. Beantworte die drei Rückfragen
2. Bestätige (oder korrigiere) den finalen 15-Punkte-Konsens unten
3. Markiere abschließend mit ✅ / ⚠️ / ❓

## Finaler Konsens-Vorschlag (15 Punkte)

| # | Entscheidung |
|---|---|
| 1 | `segment` als Default-Methode |
| 2 | `regex` als Fallback bleibt |
| 3 | `prefix` und `contains` werden entfernt (keine Legacy) |
| 4 | `RecognitionRule.SegmentPosition: int?` |
| 5 | `SchemaVersion 3` für profiles.json |
| 6 | `RecognitionContext` mit `BaseName` + `Segments` (Position, RawValue) |
| 7 | `IFileNameParser` zentral für Wizard und Recognizer |
| 8 | AND-Semantik bei Multi-Rules |
| 9 | Cache-Granularität pro `(fileName, profileId)` |
| 10 | `ProfileManager.Load` verwirft Profil bei invalider Rule |
| 11 | Variable-Segment-Warnung als UI-Hinweis (kein Hard-Fail) |
| 12 | Profil-Minimum-Validierung erweitert (Id, Name, Tokenization, Rules) |
| 13 | `MatchesSegment` schlank (kein Trim/Strip) |
| 14 | regex operiert auf BaseName + ReDoS-Timeout 100ms |
| 15 | Pflicht-Tests: Bug-Szenario, Out-of-Range, Multi-Rule AND, Regex-Fallback, IsValid |

Canvas-Titel: **"Review Runde 3"**
