# Runde 3 — User-Entscheidungen (Herbert)

## Bestätigt nach Runde 3 (Review-Abschluss)

- ✓ Alle 15 Punkte des finalen Konsenses übernommen
- ✓ **ADR-010 wird erweitert** statt neuer ADR-050 (ChatGPT-Empfehlung übernommen)
- ✓ `RecognitionContext` als Record mit `BaseName` und `IReadOnlyList<Segment>`
- ✓ Profil-Minimum-Validierung erweitert (Id, DocumentTypeName, Tokenization, Recognition)
- ✓ Logging-Dedup nicht nötig — `ProfileManager.Load` deckt den Fall ab
- ✓ Doc-Pflege wird als eigene Sub 082.07 angelegt (nach 082.01–082.06c)

## BPM-082 Subs angepasst nach Runde 3

Die ursprünglich 6 Subs wurden auf **9 Subs** erweitert:

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

## Status nach Review

✓ Cross-Review abgeschlossen
✓ 082.01 einsatzbereit
✓ Implementation kann starten

Wartet auf Implementation in folgenden Sessions.
