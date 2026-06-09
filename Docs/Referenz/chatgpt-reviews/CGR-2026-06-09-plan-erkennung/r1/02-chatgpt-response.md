# Review Runde 1 — ChatGPT-Antwort (GPT-5.4)

Recognition v2 grundsätzlich signiert — aber NICHT als „Regex im bestehenden positionsbasierten Modell nachrüsten". Kernpunkt: Recognition v2 muss ein eigenes **Feld-Extraktionsmodell** werden, nicht nur bessere DocumentType-Erkennung.

Branch `feature/planmanager-v1` geprüft:
- `RecognitionProfile` (Schema v4) weiterhin stark segment-/positionsorientiert (`ProfileSegment.Position`, `FieldTypeId`, `RecognitionRule.SegmentPosition`).
- `FileParseService` extrahiert Felder ausschließlich über `segDef.Position → parsed.Segments.Position` → Statik-Problem bleibt, auch mit `RecognitionRule.Method=regex` (das matcht nur den Dokumenttyp).
- `DocumentTypeRecognizer` kann `regex`, aber nur zum Profil-Match, nicht als Feld-Capture-Quelle.
- Datenmodell vorbereitet: `document_key` aus `identityFields`; `plan_documents` mit building_part/level; `plan_document_segments` Raw/Normalized.
- `building_part_aliases` projektgebunden, bpm.db, harter FK, `UNIQUE(project_id, normalized_alias_value)` → richtige Basis.

## 1. Bewertung Extract→Normalize→Alias→Learn
Fachlich richtig, entspricht CDE-Muster. Präzisieren als:
`Classify → Extract Candidates → Normalize → Resolve to Master Data → Decide/Preview → Learn Confirmed Mapping`
**Fehlender Schritt: Decide/Preview.** Alias-Lernen darf nicht direkt aus Extract, sondern nur aus bestätigtem Import-Preview.
**Zentrale Schwäche:** Extract muss wirklich feldbasiert arbeiten. Aktuell ist Feldbefüllung positionsbasiert — das ist der technische Bruch. Vorschlag: `FieldExtractionRule`-Liste im Profil (fieldTypeId, source filename|token|planHeader, method regex|tokenPosition|literal|derived, pattern, groupName, priority, required, normalizer). Regex mit Named Captures. `Segments` darf für V1 bleiben, aber nicht „intelligenter" gemacht werden.

## 2. Alias-Auto-Learn: nur als bestätigtes Lernen
Tabelle gut (projektgebunden verhindert H64-Projekt-A-vs-B-Kollision). Aber „Auto-Learn" gefährlich → **Auto-Suggest: ja, Auto-Learn ohne Bestätigung: nein.**
Risiko: `Haus 6`/`H6`/`H66`/`Haus66` — „alle Nicht-Ziffern entfernen" zu aggressiv. Besser Konfidenz-Modell (AliasCandidate mit DecisionKind: ExactKnownAlias/ExactMasterName/HighConfidenceSuggestion/Ambiguous/Unknown/Rejected).
Harte Regeln: kein Lernen bei Ambiguous; kein Lernen aus OCR ohne Bestätigung; kein Lernen wenn normalized bereits auf anderes building_part zeigt; Alias-Undo als normale Benutzeraktion; Import-Preview muss Alias-Entscheidungen sichtbar machen (H64→Haus 64, Quelle, Vertrauen, Aktion). Tabelle erweitern um source/confidence/is_auto_suggested/confirmed_at/confirmed_by (Frühphase: löschen+neu).

## 3. Deterministischer Kern + KI/OCR nur Assist
Richtig, sogar verschärfen: Ablageentscheidung immer auf erklärbaren Evidence-Objekten. `OEWG Dobl-Zwaring Stiegenschnitt 2 Haus 64.pdf` braucht KEIN LLM (Regex+Alias reicht). OCR früher relevant bei `scan00017.pdf`, `2026-05-14.pdf`, `Plan.pdf`, `WhatsApp Image...`. Einheitliches Candidate-Modell (ExtractedFieldCandidate: fieldTypeId, raw, normalized, source filename|folder|ocr|manual, confidence, evidence) + deterministischer Resolver (ResolvedField mit MasterDataId, RequiresUserConfirmation).

## 4. Sequenzierung V1↔post-V1
**V1/080.05 jetzt:** nicht komplette Recognition v2, aber Datenmodell+Wizard nicht weiter auf reine Segmentpositionen zementieren. Wizard zeigt Segment-Modus als Standard, bekommt intern „Field Extraction"-Konzept, Segmentposition wird als ExtractionRule (method=tokenPosition) gespeichert, Regex-Capture als Advanced vorbereitet, Preview zeigt Felder feldbasiert.
**BPM-007.02/.03:** zuerst, aber Regex muss Named Captures in extractedFields schreiben (sonst Statik nicht gelöst).
**BPM-109.06 Alias:** direkt danach (ohne Alias nur bessere Strings, keine stabile Identität).
**OCR:** post-V1, aber Schnittstelle jetzt vorbereiten (`IPlanFieldExtractionSource`).

## 5. Edge-Cases
A) Kopiermarker `(1)` vor Extract entfernen (nur am Ende: `\s*\(\d+\)$`).
B) **Konkreter Bug:** `FileParseService` schreibt `plan_number`/`plan_index`, `ImportWorkflowService` liest `planindex`/`plannumber` → Feldkey-Bruch. Zentralisieren (SegmentTypeIds-Konstanten).
C) Haus-vs-Geschoss: `EG` darf nie als haus landen → eigene Pattern + SemanticRole, nicht Position 4.
D) Kombi-Dateien `Schalung+Bewehrung` → document_type=combined oder Preview-Konflikt.
E) Pläne ohne Haus (Lageplan etc.) → haus/geschoss required profilabhängig, nicht global.
F) Projekt-/Plannummer-Kollision `5998-101` → Pattern `^(?<project>\d{4})-(?<number>\d{3}[a-z]?)`; document_key konsistent.
G) Umlaute/Normalisierung als zentraler Service `IPlanValueNormalizer` (NormalizeForKey/NormalizeAlias/NormalizeFolderName — nicht identisch).
H) Falsch gelernter Alias → im Alias-Manager sichtbar+löschbar (Soft Delete).
I) **Ordnername darf nicht aus Alias kommen:** Dateiname H64 → building_part_id abc → Ordner = building_parts.name „Haus 64", nicht „H64". Wichtiger UX-Vertrauenspunkt.

## Architektur-Empfehlung
Additive Pipeline: Scan→Fingerprint→ClassifyProfile→ExtractFieldCandidates→NormalizeCandidates→ResolveMasterData→BuildDocumentIdentity→VersionDecision→BuildImportPlan. Für V1 nur TokenPositionExtractionSource + optional RegexFilenameExtractionSource; OCR später als weitere Source.

## Sign-off (3 Bedingungen)
1. Regex-Captures müssen extractedFields befüllen, nicht nur Profil-Matching.
2. Alias-Lernen nur nach User-Bestätigung in Import-Vorschau.
3. 080.05 darf intern nicht mehr rein positionszentriert speichern (Segmentposition nur als eine ExtractionRule).

## ✅ Einigkeit
Extract→Normalize→Alias→Learn richtig; building_part_aliases projektgebunden bpm.db harter FK; document_key aus fachlicher Identity; OCR/LLM nur Assistenz; keine Migration in Frühphase.

## ⚠️ Widerspruch
Aktueller Code löst Problem nicht (Feldextraktion positionsbasiert); Method=regex reicht nicht ohne Named Captures in extractedFields; „Auto-Learn" muss „Auto-Suggest + Confirmed Learn" sein; Feldkey-Bruch plan_number vs plannumber; Alias-Normalisierung nicht zu aggressiv (H6/H66).

## ❓ Rückfragen
1. FieldExtractionRule in RecognitionProfile Schema v5 oder separates RecognitionProfileV2?
2. haus weiterhin automatisch identity-bildend oder profilabhängig?
3. building_parts mit stabiler Nummer/Code-Spalte (64) oder nur Name (Haus 64)?
4. 080.05 V1 bereits Regex-Capture im UI oder intern vorbereiten + manuelle JSON-Config?
5. Alias-Undo nur Stammdaten-UI oder auch aus Import-Vorschau?

---

## ⚡ User-Pivot (Herbert, am Ende von r1)
> „Ich bin mir nicht mehr sicher ob ich nicht zuviel Zeit in ein Tool investiere das nicht wirklich funktionieren kann, da die Planbezeichnungen immer unterschiedlich sind. Wäre es nicht bald besser eine **Erstaufnahme der Pläne manuell** auszuführen und danach wird nur mehr erkannt ob der **selbe Plan nochmal** ist oder ein **neuer Index** des bereits vorhandenen Plans?"

→ Strategischer Fork: Auto-Recognition (Strategie A) vs. Manuelle Erstaufnahme + Revisions-Matching (Strategie B). Wird in r2 / Entscheidung behandelt.
