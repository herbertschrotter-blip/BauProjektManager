# Runde 1 — Claude-Analyse zu ChatGPTs Antwort

> **Hinweis:** Diese Analyse wurde aus dem Chat-Verlauf von Teil 20 rekonstruiert. Sie wird nach Eintreffen der ChatGPT-Antwort ggf. ergänzt oder korrigiert.

---

## Kernpunkte aus Runde 1

ChatGPT bestätigte den Architektur-Refactor grundsätzlich. Folgende Punkte aus der Diskussion wurden konsensual entschieden:

### Datenmodell

- `SegmentPosition: int?` als nullable Feld in `RecognitionRule` ist der richtige Weg.
- Keine Sub-Types / Discriminated Unions — die Komplexität wäre für 2 Methoden (`segment`, `regex`) zu hoch.
- Ein zusätzlicher `RecognitionContext` als Hilfstyp soll `BaseName` und `Segments` (mit `Position` + `RawValue`) bündeln, um Tokenisierung zentral zu halten.

### Default-Methode

- `"segment"` wird Default — der Wizard speichert immer `segment` mit Position, außer der User wählt explizit `regex`.
- `prefix` und `contains` werden **komplett entfernt** (keine Legacy-Toleranz).

### Migration

- **Hart:** Beim Laden alter Profile wird `Method` validiert — nur `segment` und `regex` sind erlaubt.
- Profile mit alter Methode werden **vom `ProfileManager.Load` verworfen** (mit Logging-Warnung).
- Begründung: Das Profil-System ist noch jung, der User kennt seine Profile, Best-Effort-Konvertierung wäre fehleranfällig.

### AND-Semantik

- AND bleibt — alle Rules eines Profils müssen matchen.
- Kein expliziter Operator-Layer für jetzt (KISS-Prinzip).

### Test-Strategie

- Pflicht-Tests:
  - **Bug-Szenario:** `PROJ-PROT-2025-01.pdf` matcht, `RK-PROTOKOLL-EG.pdf` matcht nicht
  - **Out-of-Range:** 3-Segment-Datei + Rule mit Position 5
  - **Leerer Dateiname**
  - **Multi-Rule AND:** alle müssen matchen
  - **Regex-Fallback** wie bisher
  - **IsValid-Validierung** auf Rule- und Profile-Ebene

### Offene Punkte für Runde 2

ChatGPT hat einige Detail-Fragen aufgeworfen:

1. **Cache-Granularität:** Pro Profil oder pro Rule?
2. **Logging-Verhalten:** Bei jedem invaliden Match-Versuch loggen oder dedupliziert?
3. **`MatchesSegment`-Komplexität:** Soll die Methode tolerant gegenüber Whitespace, Sonderzeichen, Case sein?
4. **`FileNameParser`-Zentralisierung:** Wizard und Recognizer sollen denselben Parser nutzen — wie wird das technisch sichergestellt?

Diese Punkte werden in Runde 2 weiter verhandelt.

---

## Position-Tabelle Ende Runde 1

✅ **Einigkeit:** segment-Methode als Default, SegmentPosition int?, AND-Semantik, harte Migration ohne Legacy, regex als Fallback

⚠️ **Widerspruch:** keine signifikanten Differenzen mehr in Runde 1

❓ **Rückfragen für Runde 2:** Cache-Granularität, Logging-Dedup, MatchesSegment-Toleranz, FileNameParser-Zentralisierung
