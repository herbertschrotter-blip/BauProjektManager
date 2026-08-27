# Runde 3 — Claude-Analyse der ChatGPT-Antwort

**Gesamtbild:** Finales Sign-off von ChatGPT. Alle drei Korrekturen habe ich im Code
verifiziert — **alle drei sind berechtigt und werden übernommen.** Der neu eingebrachte
Tokenization-Bootstrap (§7–10) schließt eine echte Lücke, die wir beide bis r2 übersehen
hatten. Aus meiner Sicht: beidseitiges Sign-off, Serie abschließbar, Schluss-Statement
übernehmbar.

## Code-Verifikation der drei Korrekturen

| Korrektur | Befund |
|---|---|
| K3: `document_types` hat nur `is_deleted`, kein `is_active`; `segment_types` hat beide; Unique-Index `WHERE key <> '' AND is_deleted = 0` | ✅ `ProjectDatabase.cs:196–206` (document_types) vs. `:269/:291` (segment_types `is_active`). Der Key-Reuse-Einwand ist real: nach „Deaktivierung via is_deleted" wäre der Key frei, Reaktivierung konfliktbehaftet. |
| K5: `PatternTemplateService` nutzt beide Identitätsmodelle gleichzeitig | ✅ `AddOrUpdate` per `DocumentTypeId` (`:141`), `GetSuggestions` per `DocumentTypeName` (`:162`). Mein r3-Vorschlag „komplett auf DocumentTypeId umstellen" wäre für die globale Template-Bibliothek falsch gewesen — `document_types.id` ist projektlokal (ULID pro Projekt). ChatGPTs Lineage-Lösung ist die richtige. |
| K6: ADR-064 bereits vergeben | ✅ `ADR.md:3019` — Import-Transaktions-Härtung (heute committed, v0.28.137). Neuer ADR = **ADR-065**. |

## Bewertung der Punkte

1. **K1 (Fingerprint + Escaping + Tokenization-Kontext):** Übernommen. Das
   `|`/`;`-Format mit Percent-Escaping ist die robustere Serialisierung; die Aufnahme
   der Tokenization-Parameter in den Fingerprint ist konsequent (sonst kollidieren
   Fingerprints über Parser-Konfigurationen hinweg). Wichtige Klarstellung bestätigt:
   Fingerprint = Audit-/Gruppierungsschlüssel, kein rückparsbares Domain-Protokoll.
2. **K2 (event-invalidiert, UI-demand-berechnet):** Übernommen. Die Invariante
   „Mining ist niemals Teil der Import-Transaktion" ist nach ADR-064
   (Import-Transaktions-Härtung, heute abgeschlossen) genau richtig platziert —
   dirty-Flag beim Batch-Commit, Berechnung erst beim Tab-Öffnen.
3. **K3 (`is_active` statt `is_deleted`):** Übernommen — ChatGPT hat recht, mein
   Mechanismus-Vorschlag war falsch. Auch die Trennung
   `ProfileHealth.DocumentTypeInactive` vs. `MissingSegmentTypes` (zwei verschiedene
   Diagnosegründe) übernehme ich. Frühphase: Spalte kommt per DB-Reset, keine Migration.
4. **K4 („Erkennung"):** Beidseitig angenommen; finale UI-Wortwahl bleibt bei Herbert.
5. **K5a (PatternTemplate-Identität via `profileLineageId`, `SourceDocumentTypeId`
   nur Provenance, `DocumentTypeName` nur Anzeige; ADR-010-Präzisierung):** Übernommen.
   Elegant: Stufe C2 räumt damit gleichzeitig das heutige Identitäts-Gemisch der
   Template-Bibliothek auf.
6. **K6 (ADR-065):** Übernommen — Nummer war mein Fehler (ADR-064 heute durch die
   parallele Serie belegt).
7. **Tokenization-Bootstrap (§7–10):** Der wertvollste Neubeitrag dieser Runde.
   Die Lücke war real: L2a lernt vor dem ersten Profil, aber `TokenizationConfig`
   lebt im Profil. ChatGPTs Auflösung ist sauber und frühphasen-konform:
   **Roh-Evidenz statt Token-Snapshots** (Dateiname + bestätigte Werte sind die
   Wahrheit; Token-Features werden beim „Anlernen" mit der dann gewählten Config über
   den zentralen `FileNameParser` reproduzierbar neu berechnet; `TokenizationConfig`
   bleibt Recognition-Konfiguration, wandert NICHT in `document_types`).
   Profilunabhängige Features (Formen, Extractor-Kandidaten, bestätigte Werte) tragen
   Stufe A auch ohne Tokenization. Die Invariante aus §10 gehört wörtlich in ADR-065.
8. **§13 „darf offen bleiben":** Zustimmung — reine Kalibrierungs-/Ticketdetails.

## Fazit

Beidseitiges Sign-off liegt vor. Das finale Schluss-Statement von ChatGPT trage ich
unverändert mit — es fasst alle Serien-Ergebnisse korrekt zusammen und geht 1:1 in
README + Review-INDEX. Nächste Schritte außerhalb der Serie: **ADR-065** schreiben
(mit den r3-Schärfungen: `is_active`, Lineage-Identität für Templates,
Mining-außerhalb-Import-Invariante, Tokenization-Bootstrap-Invariante,
WERTE/ROLLEN/FORMEN, Veto-Regel, Roadmap, „bei Umsetzung offen"-Liste) und optional
ClickUp-Ticket(s) für die Lern-Roadmap-Stufen.
