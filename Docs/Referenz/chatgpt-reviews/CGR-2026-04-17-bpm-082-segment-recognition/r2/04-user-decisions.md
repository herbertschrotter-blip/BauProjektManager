# Runde 2 — User-Entscheidungen (Herbert)

## Bestätigt nach Runde 2

- ✓ Cache-Strategie pro `(fileName, profileId)` akzeptiert
- ✓ ProfileManager.Load verwirft ganzes Profil bei invalider Rule
- ✓ `MatchesSegment` bleibt schlank (kein Trim, kein Strip)
- ✓ `IFileNameParser` als gemeinsame Komponente für Wizard und Recognizer
- ✓ Variable-Segment-Warnung als UI-Hinweis (kein Hard-Fail)
- ✓ regex operiert auf `BaseName`, ReDoS-Schutz via Timeout
- ✓ Profil-Minimum-Validierung wird erweitert (Id, DocumentTypeName, Tokenization, Recognition.Count > 0)

## Offene Punkte für Runde 3

Werden in r3 final geklärt:
- FileNameParser-Rückgabetyp
- Logging-Dedup-Tiefe
- ADR-Struktur (neuer ADR-050 vs. ADR-010 erweitern)
