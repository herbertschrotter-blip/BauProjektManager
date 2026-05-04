# Runde 2 — Claude-Analyse zu ChatGPTs Antwort

> **Hinweis:** Diese Analyse wurde aus dem Chat-Verlauf von Teil 20 rekonstruiert.

---

## Konsens-Punkte aus Runde 2

ChatGPT hat die 5 Detailfragen beantwortet. Die wichtigsten Klärungen:

### Cache-Granularität (Frage A)

- **Konsens:** Cache pro `(fileName, profileId)`-Tupel.
- Begründung: Profile können unterschiedliche `Tokenization.Delimiters` haben, daher können Tokenisierungs-Ergebnisse nicht zwischen Profilen geteilt werden.
- Implementierung als einfacher `Dictionary<(string, Guid), RecognitionContext>` im Recognizer-Scope.

### Logging-Verhalten (Frage B)

- **Konsens:** ProfileManager.Load verwirft das ganze Profil, wenn auch nur eine Rule invalid ist (z.B. `SegmentPosition` null bei `Method == "segment"`).
- Logging passiert einmalig beim Laden, nicht pro Match-Versuch.
- Im Recognizer wird Belt-and-Suspenders auf Debug-Level geloggt — falls trotzdem eine invalide Rule ankommen sollte (defensives Programmieren).

### MatchesSegment-Toleranz (Frage C)

- **Konsens:** Schlank halten. Kein Trim, kein Strip. Tokens kommen sauber aus dem Parser.
- Case-Insensitive bleibt (`OrdinalIgnoreCase`).
- ChatGPT: "Wenn der User Toleranz braucht, soll er regex nutzen."

### FileNameParser-Zentralisierung (Frage D)

- **Konsens:** `IFileNameParser` als gemeinsame Komponente für Wizard und Recognizer.
- `RecognitionContext` als Hilfstyp mit `BaseName` und `IReadOnlyList<Segment>`.
- `Segment` mit `Position` (int) und `RawValue` (string).
- Wizard nutzt denselben Parser für Live-Vorschau im Schritt 5.

### Variable-Segment-Warnung (Frage E)

- **Konsens:** UI-Hinweis im Wizard, kein Hard-Fail.
- Warnung erscheint, wenn die markierte Segment-Position bei mind. einer Beispiel-Datei nicht vorhanden ist oder einen anderen Wert hat als erwartet.
- User kann trotzdem speichern — er kennt seine Daten.

---

## Neue Punkte von ChatGPT

ChatGPT hat in Runde 2 zusätzlich gebracht:

1. **Profil-Minimum-Validierung erweitern:** Nicht nur Rule-Validierung, sondern auch:
   - `Profile.Id` muss gesetzt sein
   - `Profile.DocumentTypeName` darf nicht leer sein
   - `Profile.Tokenization` darf nicht null sein
   - `Profile.Recognition.Count > 0` (mind. eine Rule)

2. **MatchesSegment soll keine Sonderlogik enthalten** — die Methode bleibt minimal, alle Sonderfälle werden im Lade-Pfad abgefangen.

3. **regex-Methode soll auf `BaseName` operieren** (nicht auf vollem Dateinamen mit Extension), damit das Verhalten konsistent zu `segment` ist.

4. **Empfehlung:** ReDoS-Schutz für regex via Timeout (`TimeSpan.FromMilliseconds(100)` als Default).

---

## Offene Punkte für Runde 3

Drei Rückfragen für die finale Runde:

1. **`FileNameParser`-Rückgabetyp:** ChatGPT hatte in einer Skizze `BuildRecognitionContext` mit `.BaseName` und `.Segments` (mit `.Position` + `.RawValue`) verwendet. Ist dieser Typ stabil, oder soll der tatsächliche Rückgabetyp im Branch geprüft werden?

2. **Logging-Dedup belt-and-suspenders:** Soll das Warning bei invalider Rule im Recognizer wirklich nur Debug-Level sein, oder sollten wir es ganz weglassen, da `ProfileManager.Load` den Fall verhindert?

3. **ADR-Struktur:** Neuer ADR-050 für `segment`-Methode, oder ADR-010 erweitern? Mein Bauchgefühl: neuer ADR.

---

## Position-Tabelle Ende Runde 2

✅ **Einigkeit:** Cache pro (fileName, profileId), ProfileManager.Load verwirft invalide Profile, MatchesSegment schlank, IFileNameParser zentral, Variable-Warnung als UI-Hinweis, regex auf BaseName, ReDoS-Timeout

⚠️ **Widerspruch:** keine offenen

❓ **Rückfragen für Runde 3:** FileNameParser-Rückgabetyp, Logging-Dedup-Tiefe, ADR-Struktur (neuer ADR-050 vs. ADR-010 erweitern)
