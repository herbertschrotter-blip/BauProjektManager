# Runde 1 — Claude-Analyse der ChatGPT-Antwort

**Gesamtbild:** Sehr konstruktive Runde. ChatGPT hat alle Kernkonflikte aus meiner
Position übernommen (ADR-059-Grenze ohne Wenn und Aber, kein ML/Embeddings/LLM im
Importpfad, Rule Mining Priorität 1, `plan_document_segments` als Sample-Store, eine
Regel-Wahrheit, keine Prozent-Fusion). Die verbleibenden Widersprüche sind keine
Konflikte, sondern echte Verbesserungen meiner Position. Ein Sign-off in Runde 2 ist
realistisch.

## Was ich von ChatGPT übernehme

1. **Korrektur „Profil ≠ Quelle" — akzeptiert, verifiziert.** `ProfileManager.cs:311`
   registriert `PersistenceScope.ProjectLocal`, Profile liegen als JSON in
   `.bpm/profiles/` (ADR-046), fachlich `DocumentTypeId`-geführt (ADR-061). Ein Profil
   ist eine projektlokale Recognition-Konfiguration pro Dokumenttyp — mein r1-Kurzschluss
   „Profil = Absender/Büro" war unsauber. Konsequenz: Cross-Project-Übertragung braucht
   eine explizite Herkunft, nicht Namens- oder Typgleichheit.

2. **Hierarchisches Backoff-Modell (L2a/L2b/L2c) — mitgetragen.** Vorrang statt
   Prozentmischung ist exakt meine Anti-Scheinpräzisions-Linie, nur besser strukturiert.
   Wichtig: als **Zielarchitektur**; implementiert wird gestaffelt (Stufe A = nur L2a).
   „Falls nicht ausreichend" braucht im späteren ADR eine harte Definition
   (Support-/Purity-Schwellen pro Ebene) — notiert als Runde-2-Punkt.

3. **`profileLineageId` — mitgetragen, konkreter Vorschlag:** stabile Lineage-ID (ULID)
   wird beim ersten Anlegen eines Profils erzeugt und beim Kopieren / „Als Vorlage
   verwenden" **vererbt** (nie aus Namen abgeleitet, nie nachträglich gematcht). Additives
   JSON-Feld im Profil v5 — Frühphasen-konform. Das ist bereits der schlankste Mechanismus;
   etwas Schlankeres (Namensgleichheit, Fingerprint-Heuristik) wäre implizit und damit
   genau das, was wir vermeiden wollen.

4. **Scope-Invariante** („projektgebundene Stammdaten-IDs werden niemals
   scope-übergreifend gelernt oder übertragen") — voll mitgetragen, ADR-tauglich
   formuliert. ChatGPTs Beispiel 14 zeigt die saubere Trennung: Familie liefert
   Wert-Vorschlag („EG"), Auflösung auf `building_level_id` passiert immer lokal
   gegen die Stammdaten des Zielprojekts (ADR-061-konform).

5. **Drift-Modell** (LongTerm/Recent-Fenster, Zustände Stable/DriftSuspected/
   ReviewRequired, nie stille Regeländerung, bei bestätigtem Schemawechsel neue
   Regelgeneration statt Evidenz-Vernichtung) — mitgetragen.

6. **Feedback-Tabelle bestätigt** — ChatGPTs Schema-Skizze ist gut;
   `proposal_fingerprint` (welches Muster hat den Vorschlag erzeugt) ist die richtige
   Ergänzung zu meinem Entwurf. `outcome`-Enum um `rejected` erweitert — sinnvoll.

## Wo ich differenziere / für Runde 2 nachschärfe

1. **Mining-Katalog: schmal starten.** ChatGPTs eigene Übersetzbarkeits-Regel („nur
   Muster, die in explizite BPM-Regeln übersetzbar sind") sortiert den Katalog von
   selbst: `TokenOrderPair` und `TokenCount` sind in `RecognitionRule` v5
   (`segment`/`regex`) NICHT ausdrückbar → warten per Definition auf
   `FieldExtractionRule` (post-V1, BPM-007.03). Für Ausbaustufe B daher:
   `ExactTokenAtPosition`, `TokenPrefix`, `TokenSuffix`, `TokenShape` (alle regex-/
   segment-übersetzbar). `ExactToken` (positionsunabhängig) nur mit
   **Token-Grenzen-Semantik** als Regex (`(^|[_\-\s.])GR([_\-\s.]|$)`) — niemals als
   Substring, sonst wäre es das bewusst entfernte `contains` durch die Hintertür.
   ChatGPT sieht die `contains`-Gefahr selbst; die Token-Grenzen-Bedingung macht den
   Unterschied explizit.

2. **Alias-Stufe fehlt in ChatGPTs Roadmap.** Meine Ausbaustufe C (Segmentwert-Aliasse
   nach dem Muster `building_part_aliases`, deckt „Grundr./GR/Floor Plan") ist in
   ChatGPTs Roadmap stillschweigend verschwunden. Aliasse sind orthogonal zu Lineage
   (Schreibweisen-Normalisierung vs. Struktur-Übertragung) und bereits teilweise
   beschlossen (ADR-058, BPM-109.06). Gehört wieder in die Roadmap — als eigene Stufe
   oder in Stufe A integriert (Alias-Lernen IST projektlokale Evidenz in ihrer
   einfachsten Form).

3. **Roadmap-Reihenfolge C/D:** ChatGPTs Reihung (Lineage vor globalem Lexikon) ist
   plausibel — Lineage hat für Herberts Praxis (wiederkehrende Büros) mehr Wert. Das
   statische Kürzel-Lexikon ist aber fast gratis (kuratierte Datei im Repo, kein
   Lern-Code) und kann parallel jederzeit kommen.

## Antwort auf Herberts Anschlussfrage

> „gewisse dinge wie projektnummer oder plannummer oder index usw dürften dann nicht
> ins globale lernen oder?"

Richtig — konkrete **Werte** lernen nie global. Drei-Ebenen-Merksatz:

- **Werte** (5998, H64, konkrete Plannummern, Index „B") → nur projektlokal.
- **Rollen/Struktur** („Token 0 ist die Projektnummer dieses Büros") → nur
  Profil-Familie (Lineage), denn die Projektnummer ändert sich pro Projekt
  (5998 → 6142), die Struktur bleibt.
- **Formen** (`^\d{3,5}$` = Nummernkandidat, `^rev\d+$` = Revisionskandidat,
  `^[A-Z]\d?$` = Index-Kandidat) → global erlaubt, weil sie nichts Projektspezifisches
  verraten und nur Kandidaten markieren, nie Identität stiften.

Genau das deckt ChatGPTs Abschnitt 3 + die Scope-Invariante aus Abschnitt 13 ab —
Herberts Intuition und ChatGPTs Modell sagen dasselbe.

## Empfehlung

Runde 2 als Sign-off-Runde: die 5 Rückfragen beantworten (1 ja / 2 ja / 3 Lineage-ID
mit Vererbungsregel / 4 ja / 5 schmaler Katalog + Token-Grenzen-Bedingung), Alias-Stufe
zurück in die Roadmap, „ausreichend stark"-Schwellen als offenen Punkt für den späteren
ADR markieren. Danach Serie abschließen; Ergebnis-Vehikel wäre ein neues ADR
(„Lernende Erkennung / Evidenz-Scoping") — aber erst, wenn die Umsetzung ansteht
(alles post-V1, kein aktueller Task betroffen).
