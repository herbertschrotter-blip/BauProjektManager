# Runde 1 — Herberts Entscheidungen

**Datum:** 2026-08-27

## Entscheidungen zu den Stufe-A-Fragen

1. **Klassischer Profil-Import („Import starten"-Button / ImportPreviewDialog) in V1:** ❌ **Deaktivieren.** Sobald der Radial-Flow (BPM-111) fertig ist, wird der alte Button deaktiviert/zurückgebaut. Nur EIN Import-Weg in V1. Konsequenz: Skip-only-Fix (ImportExecutionService Early-Return) und IsConflict-Fix (DocumentTypeRecognizer) entfallen als V1-Prioritäten — die Dubletten-Frage verlagert sich vollständig auf Bucket A.
   *(Herbert hat vor der Entscheidung eine Erklärung des klassischen Pfads erhalten: „Import starten" in ProjectDetailView → Profil-Automatik → RevisionDecision → ImportPreviewDialog → ImportExecutionService.)*

2. **Bucket-A-Dubletten (ChatGPT-Rückfrage 2):** ✅ **Beim Confirm entfernen.** MD5-identische Dateien werden beim finalen Import aus `_Eingang` gelöscht — der Eingang wird leer (entspricht der bisherigen SkipIdentical-Absicht). Journalisiert, damit undo-/nachvollziehbar.

3. **Task-Schnitt (neuer Task „Import-Transaktions-Härtung" vs. BPM-112-Erweiterung):** 🔄 **ChatGPT fragen** — Schnitt in Runde 2 weiter diskutieren, inkl. Claudes Anmerkungen (T0/T1-Reihenfolge, Undo-Härtung, DI statt `new`).

## Zusatzauftrag von Herbert für Runde 2

ChatGPT damit konfrontieren, dass Claude **nicht die vollständige Diagrammserie** gesehen hat — der geteilte Chat enthielt nur Diagramme 10–12 + Gesamtauswertung + Dateibrowser-Diskussion. ChatGPT soll die verifizierbaren Kernbefunde aus den Diagrammen 01–09 kompakt nachliefern.
