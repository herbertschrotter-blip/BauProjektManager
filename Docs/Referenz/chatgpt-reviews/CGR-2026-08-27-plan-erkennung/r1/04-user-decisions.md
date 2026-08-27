# Runde 1 — Herberts Entscheidungen

**Datum:** 2026-08-27

## Entscheidungspunkte (Stufe A)

| # | Frage | Entscheidung |
|---|-------|--------------|
| 1 | Hierarchisches Scope-Modell (Projekt-Evidenz → Profil-Familie/Lineage → globales Lexikon als Backoff) samt `profileLineageId` als Zielarchitektur übernehmen? | **Ja, übernehmen.** Zielarchitektur = 3 Ebenen mit Vorrang-Backoff; Umsetzung gestaffelt (Stufe A rein projektlokal, Lineage erst später). Invariante: Stammdaten-IDs wandern nie über Scope-Grenzen. |
| 2 | Mining-Feature-Katalog für Ausbaustufe B? | **Schmal starten.** ExactTokenAtPosition, TokenPrefix, TokenSuffix, TokenShape + ExactToken nur mit Token-Grenzen-Regex. TokenOrderPair/TokenCount warten auf FieldExtractionRule (per Übersetzbarkeits-Regel). |
| 3 | Alias-Ausbaustufe (Segmentwert-Aliasse, Muster `building_part_aliases`) wieder in die Roadmap? | **Ja, als eigene Stufe** — angedockt an das schon beschlossene BPM-109.06 (post-V1). |

## Herberts Anschlussfrage (aus dem Chat)

> „aber gewisse dinge wie projektnummer oder plannummer oder index usw dürften dann
> nicht ins globale lernen oder?"

**Antwort (Claude, von Herbert unwidersprochen übernommen):** Richtig — konkrete
**Werte** (Projektnummer 5998, Plannummern, Index „B") lernen nie global.
Drei-Ebenen-Merksatz: Werte → nur projektlokal · Rollen/Struktur („Token 0 =
Projektnummer dieses Büros") → nur Profil-Familie (Wert ändert sich pro Projekt,
Struktur bleibt) · Formen (`^\d{3,5}$` = Nummernkandidat) → global erlaubt, weil sie
nur Kandidaten markieren, nie Identität stiften. Deckt sich mit ChatGPT Abschnitt 3 + 13.

## Nächster Schritt

Runde 2 als Sign-off-Runde: Antworten auf ChatGPTs 5 Rückfragen + Roadmap-Korrektur
(Alias-Stufe) + offene ADR-Punkte (Schwellen-Definition „ausreichend starke Evidenz").
