# CGR-2026-08-27-plan-erkennung — Lernende Planerkennung (Profile als lernendes System)

**Thema:** Sollen Erkennungsprofile im PlanManager „lernend" werden — d.h. aus bestätigten
Erfassungen und Benutzerkorrekturen besser werden? Bewertung der von ChatGPT skizzierten
Hybrid-Architektur (Regeln → Similarity → ML-Klassifikator → Confidence-Gate) inkl.
ML.NET / lokale Embeddings / ONNX / LLM vs. deterministisches Pattern Mining.
**Zeitraum:** 2026-08-27
**Ursprungs-Chat:** ChatGPT-Konzeptvorschlag „Lernende Planerkennung" → Claude-Review-Position (diese Serie)
**Status:** ✅ Abgeschlossen (beidseitiges Sign-off r3, 2026-08-27)
**Resultiert in:** ADR-065 (Lernende Planerkennung — hierarchisches Evidenz-Scoping + Dokumenttyp als Hauptobjekt) + ClickUp-Task „Lernende Planerkennung"

---

## Finales beidseitiges Schluss-Statement

Die lernende Planerkennung des BPM wird als erklärbare, hierarchisch gescopte
Assistenz aufgebaut und ändert die ADR-059-Grenze nicht: Nur deterministisches
Bestandsmatching darf automatisch entscheiden, gelernte Erkennung bleibt
`AutoSuggested` und wird vom Benutzer bestätigt. `document_types` ist die gemeinsame
fachliche Wahrheit für Dial Ring 1, manuelle Erfassung und Erkennungs-UI;
`RecognitionProfile` ist lediglich die optionale ausführbare Erkennungskonfiguration
eines Typs, während bestätigte Roh-Evidenz bereits vor einem Profil gesammelt werden
kann. Lernen erfolgt zuerst projektlokal, später über explizite `profileLineageId`
und zuletzt über kuratiertes globales Formenwissen; projektgebundene Stammdaten-IDs
werden niemals scope-übergreifend übertragen. Der bevorzugte Lernmechanismus ist
Evidenz → nachvollziehbares Rule Mining → explizite Regeln und Aliasse;
ML/Embeddings/LLM bleiben außerhalb des Importpfads und werden nur bei später
nachgewiesenem Bedarf neu bewertet.
**Bezug:** ADR-059 (Strategie B, Assist-Grenze), ADR-056 (Segmenttypen), ADR-058 (plan_document_segments), ADR-061 (kein Fuzzy im Resolver), CGR-2026-06-09-plan-erkennung (abgeschlossen)

---

## Runden-Übersicht

### Runde 1 — Claude-Review-Position zur Hybrid-/ML-Architektur
- **Artefakte:** [r1/](./r1/)
- **Fokus:** Brauchen wir ML überhaupt? Zielarchitektur L0–L3 (Deterministik → Regeln →
  Evidenz-Vorschläge → Rule Mining) statt Black-Box-Klassifikator. Lern-Scope
  (Projekt × Profil), Datenmodell schlank (bestehende Segment-Persistenz als Sample-Store),
  Confidence als Evidenz-Stufen statt Prozent-Fusion, ADR-059-Grenze (Learning nur Assist,
  nie Entscheider).
- **Kernergebnis:** Konsens auf ganzer Linie: ChatGPT streicht „Confidence hoch →
  automatisch" (ADR-059-Grenze bestätigt), kein ML/Embeddings/LLM im Importpfad,
  Rule Mining Priorität 1. ChatGPT-Korrekturen übernommen: Profil ≠ Quelle
  (code-verifiziert, `PersistenceScope.ProjectLocal`), hierarchisches Evidenz-Backoff
  Projekt (L2a) → Profil-Lineage (L2b, `profileLineageId` vererbt bei Kopie) →
  globales Lexikon (L2c, nur Tokenformen). Herbert-Entscheidungen: Backoff-Modell ja ·
  Mining-Katalog schmal (AtPosition/Prefix/Suffix/Shape + ExactToken nur
  token-grenzen-basiert; OrderPair/Count/Delimiter warten auf FieldExtractionRule) ·
  Alias-Stufe zurück in Roadmap (eigene Stufe C1, dockt an BPM-109.06 an).
  Merksatz Scope: WERTE nur projektlokal · ROLLEN nur Profil-Familie · FORMEN global.

### Runde 2 — Antworten auf Rückfragen + Sign-off
- **Artefakte:** [r2/](./r2/)
- **Fokus:** Antworten auf ChatGPTs 5 Rückfragen (Backoff ja, Lineage-ULID vererbt,
  Scope-Invariante wörtlich, Mining schmal + Token-Grenzen-Bedingung für ExactToken),
  finale Roadmap (V1 → A → B → C1 Alias → C2 Lineage → D Lexikon → ML nur bei
  gemessenem Bedarf), Schwellen-Startwerte (L2a n≥5/0,90 · L2b n≥10/0,95), Sign-off-Bitte.
- **Kernergebnis:** **ChatGPT-SIGN-OFF.** Zwei Schärfungen übernommen: (1) Veto-Regel —
  positive Vorschlagsschwelle ≠ Veto-Schwelle, unter-schwellige lokale Gegen-Evidenz
  (z. B. 2/2 widersprüchlich) blockiert den Familien-Fallback; (2) `ExactToken`-Grenzen
  aus der profilabhängigen `TokenizationConfig` des `FileNameParser`, nicht aus
  hardcodierter Delimiter-Klasse (Invariante: Mining + Runtime = dieselbe
  Token-Grenzsemantik). Dazu Dokumenttyp-Zielbild aus Herberts Zusatzfrage:
  `document_types` = fachliches Hauptobjekt (Ring 1, Zielpfad, Lern-Evidenz),
  `RecognitionProfile` = optionale Erkennungs-Konfiguration 0..1, Profil-Tab als View
  („Nicht angelernt / Lernend / Aktiv"), kein leeres Profil-JSON bei Ring-1-„+ Neu",
  Löschen asymmetrisch, L2a = Projekt × `DocumentTypeId` (Lernen vor dem ersten
  Profil). Herbert bejaht beide Rückfragen; Claude-Zusatzbefund:
  `PatternTemplateService` matcht heute auf `DocumentTypeName` (Name-Drift-Quelle) →
  stützt Entfernung beim nächsten Schema-Bump.

### Runde 3 — Konkretisierung + finaler Abschluss
- **Artefakte:** [r3/](./r3/)
- **Fokus:** Antworten auf ChatGPTs 2 Rückfragen (beide ja) + Konkretisierungs-Vorschläge
  K1–K6 aus der „Bei Umsetzung festzuziehen"-Liste: Fingerprint-Format (lesbarer
  kanonischer String), Mining-Anker (nach Import-Batch + Tab-Öffnen, nie im
  Erfassungs-Flow), Typ-Deaktivierung (Soft-Delete + Health-Gating, nichts kaskadiert),
  UI-Begriff „Erkennung", `DocumentTypeName`-Entfernung fest einplanen, ADR jetzt
  schreiben (Zielbild bindet nahe UI-Arbeiten). Bitte um finales Schluss-Statement.
- **Kernergebnis:** **Finales beidseitiges SIGN-OFF.** K1/K2/K4 angenommen (K1 mit
  kanonischem `|`/`;`-Escaping + Tokenization-Parametern im Fingerprint; K2 als
  „event-invalidiert, UI-demand-berechnet" mit Invariante „Mining nie Teil der
  Import-Transaktion"). Drei code-verifizierte Korrekturen übernommen:
  (1) `document_types` braucht `is_active` zusätzlich zu `is_deleted` (Key-Index gibt
  Keys nach Soft-Delete frei → Deaktivierung via `is_deleted` wäre konfliktbehaftet;
  + `ProfileHealth.DocumentTypeInactive` als eigener Diagnosegrund);
  (2) PatternTemplate-Cross-Project-Identität via `profileLineageId`, NICHT
  `DocumentTypeId` (projektlokale ULID; Branch heute inkonsistent: `AddOrUpdate` per
  ID, `GetSuggestions` per Name; `SourceDocumentTypeId` nur Provenance, Name nur
  Anzeige; ADR-010-Präzisierung); (3) ADR-Nummer 065 (064 durch Import-Härtung belegt).
  Neu geschlossene Lücke: **Tokenization-Bootstrap** — Lern-Evidenz persistiert
  bestätigte Rohfakten, keine Token-Snapshots; Token-Features werden beim „Anlernen"
  mit der dann gewählten `TokenizationConfig` über den zentralen `FileNameParser`
  reproduzierbar neu berechnet; `TokenizationConfig` bleibt Recognition-Konfiguration
  (wandert nicht in `document_types`). UI-Zusammenführung darf vor Stufe B kommen,
  wenn V1-Arbeit sie ohnehin anfasst. → ADR-065 + ClickUp-Task.
