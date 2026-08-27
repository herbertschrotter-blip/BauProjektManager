# Runde 2 — Herberts Entscheidungen

**Datum:** 2026-08-27

## Entscheidungspunkte (Stufe A)

| # | Frage | Entscheidung |
|---|-------|--------------|
| 1 | ChatGPT-Rückfrage 1: UI-Zusammenführung als Zielbild — Profil-Tab als View über `document_types` (+ optionales Profil), Ring-1-„+ Neu" legt Dokumenttyp an (sofort Ring 1 UND Profil-Tab „Nicht angelernt"), KEIN leeres Profil-JSON, Löschen asymmetrisch? | **Ja, übernehmen.** `document_types` = fachliches Hauptobjekt, `RecognitionProfile` = optionale Erkennungs-Konfiguration (0..1). Umsetzungszeitpunkt bleibt Ticket-Frage, Zielbild ab jetzt verbindlich. |
| 2 | ChatGPT-Rückfrage 2: L2a-Scope = Projekt × `DocumentTypeId` (statt Projekt × Profil), Lern-Evidenz schon vor Existenz eines Profils? | **Ja, übernehmen.** „Lernen vor dem ersten Profil" — Wizard wird zur Bestätigungs-Geste. |
| 3 | Serienabschluss vs. weitere Runde? | **Runde 3 vollwertig.** Keine reine Schlussnachricht — Runde 3 als Konkretisierungs-Runde (umsetzungsrelevante Punkte aus ChatGPTs „Bei Umsetzung festzuziehen"-Liste vorziehen), danach Abschluss. |

## Damit festgezogen (beidseitig, Stand r2)

- ADR-059-Grenze, hierarchischer Backoff L2a→L2b→L2c, Veto-Regel
  (positive Schwelle ≠ Veto-Schwelle), WERTE/ROLLEN/FORMEN, ID-Auflösung immer lokal.
- Mining-Katalog schmal, `ExactToken` mit Token-Grenzen aus `TokenizationConfig`
  (Invariante: Mining + Runtime = dieselbe Token-Grenzsemantik wie `FileNameParser`).
- Roadmap V1 → A (L2a = Projekt × DocumentTypeId) → B (Mining + `recognition_feedback`
  + Profil-Tab als View) → C1 (Aliasse) → C2 (Lineage) → D (globales Lexikon) → ML nur
  bei gemessenem Bedarf.
- Dokumenttyp-Zielbild: `document_types` Hauptobjekt, Profil 0..1, kein leeres
  Profil-JSON, Löschen asymmetrisch, „+ Neues Profil" = „Erkennung für Dokumenttyp
  einrichten".
- ChatGPT-Sign-off liegt vor (r2); Claude-Sign-off inhaltlich ebenfalls —
  formaler Serienabschluss nach Runde 3.
