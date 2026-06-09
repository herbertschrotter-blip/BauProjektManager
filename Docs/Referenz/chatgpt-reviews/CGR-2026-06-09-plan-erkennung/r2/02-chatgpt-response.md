# Review Runde 2 — ChatGPT-Antwort (GPT-5.4)

**MVP-Entscheidung gedreht: Strategie B als Kern (manuelle Erstaufnahme + deterministisches Revisions-/Dubletten-Matching). Strategie A NICHT MVP-Ziel, sondern Assist-Schicht (Vorschläge/Vorbefüllung).** Kein Rückschritt — sauberere Produktentscheidung: Der Polier muss nicht beeindruckt sein, dass BPM 80% der Namen errät; er muss sicher sein, dass einmal sauber erfasste Pläne danach zuverlässig aktualisiert/übersprungen/archiviert/zeitlich nachvollziehbar verwaltet werden. Schema v2.0 trägt B sehr gut.

## 1. A vs B als MVP
A löst das schwierigste Problem zuerst (uneinheitliche Namen), braucht viel Oberfläche (FieldExtractionRule, Named Captures, Normalizer, Alias, Ambiguity, Preview, Lernlogik, Undo, OCR) bevor Nutzen spürbar → als MVP riskant. Aktueller Code bestätigt: Feldextraktion positionsbasiert, Regex nur Profil-Match, + Feldkey-Bug. **Entscheidung: A als MVP nein; B als MVP ja; Hybrid B+A ja, A nur Vorschlagsquelle.** Entspricht CDE-Praxis (klassifizieren beim Upload, dann System macht Versionierung/Status/Historie).

## 2. Zuverlässigkeit B-Matching
- **MD5** beweist exakt dieselbe Datei → Dubletten-Skip. Reicht NICHT für Revision (neue Revision = anderer MD5).
- **Plannummer allein** reicht nicht (je Typ/Bauteil/Fachplaner unterschiedlich eindeutig). Bekannter Plan muss über gespeicherten `document_key` laufen (document_type_id|building_part_id|building_level_id?|plan_number), nicht nur plan_number.
- **Minimum Auto-Extraktion für B:** MD5 + PlanNumber-Kandidat + Index-Kandidat + Kandidaten gegen bestehende plan_documents einschränken + bei Mehrdeutigkeit User. = enger Matching-Assistent, kein Auto-Recognition. PlanNumber-Kandidat = nur Suchanker, kein Beweis.

## 3. Scope
- **Regex (007.02/.03):** nicht komplett raus. V1-Minimum = LightweightPlanTokenExtractor (plan_number/plan_index-Kandidat + Copy-Marker-Strip + Feldkey-Fix + Preview-Bestätigung). Post-V1 = frei konfigurierbare FieldExtractionRule, Named Captures, Wizard-Regex-Editor, Alias, OCR.
- **109.06 Alias:** kann aus V1-Muss raus. Bei B zieht User Datei direkt in Zelle `Haus 64 × Schalung` → building_part_id + document_type_id direkt. Alias erst relevant wenn A automatisch `H64` ableiten soll → Komfort, nicht Grundlage.
- **OCR:** klar post-V1. `released_at` bleibt NULL bis dahin.
- **V1-MUSS:** Feldkey-Fix, manuelle Erstaufnahme als Workflow, document_key aus manuellen Feldern, plan_documents/revisions/files sauber schreiben, MD5-Dublette, Index-Kandidat, Update-Vorschlag gegen bekanntes Dokument, Supersede+Journal. **KANN:** Regex pro Profil, Alias, OCR, released_at-Befüllung.

## 4. Drag&Drop-Matrix-UI
Stark, aber NICHT als starres Excel-Raster für große Projekte. 15 Dateien × 5 Bauteile × 4 Plantypen = ok; 40 Dateien × 30 Bauteile × 12 Plantypen = unbrauchbar (leere Zellen, H-Scroll, schlechte Auffindbarkeit). UX-Fallen: zu viele Bauteile/Plantypen, Kombi-Pläne, Geschoss als 3. Dimension, mehrere Dateien/Dokument, PDF+DWG-Paarung, falscher Drop schwer erkennbar.
**Empfehlung Zwei-Modi:**
- Modus 1 Schnellerfassungs-Board: links Eingang-Dateien (+Filter), rechts Bauteil oben als Tabs/Dropdown, darunter Plantyp-Karten als Drop-Zonen, optional Geschoss-Chips. Skaliert besser als alle Bauteile als Spalten.
- Modus 2 Tabellen-/Listenmodus für Bulk: `Datei | Vorschlag PlanNr | Index | Bauteil | Geschoss | Plantyp | Aktion` + Multi-Select (5 markieren → Bauteil Haus 64; 5 → Plantyp Schalung). Für 15+ Dateien schneller als Drag&Drop.
- **MVP: Listenmodus + Detailpanel + Multi-Edit, optional einfache Drop-Zonen je gewähltem Bauteil. Post-V1: echte Matrix/Board.** Wenn Matrix in V1: max 2 Dimensionen sichtbar, 3. (Geschoss) als Chip/Filter.

## 5. Hybrid B+A
Richtig solange Grenze hart: **B entscheidet, A schlägt vor.** Code: `enum ImportIdentitySource { ManualConfirmed, ExistingDocumentMatch, AutoSuggested }`. Regel: nur ManualConfirmed/ExistingDocumentMatch darf automatisch schreiben/verschieben; AutoSuggested nur Preview-Felder. Neue Dokumente: AutoSuggested → User bestätigt → ManualConfirmed → Import. A nie Eigentümer der fachlichen Identität.

## 6. Feldkey-Bug
Nicht strategisch, aber Hygiene + V1-blockierend, trifft beide. ImportExecutionService schreibt `decision.File.PlanNumber ?? ""` → leere Plannummer ins Archiv. ResolveOrCreateDocument hat plan_number TEXT NOT NULL (leer technisch möglich, fachlich schlecht). Fix: zentrale `SegmentTypeIds`-Konstanten, ImportWorkflowService liest `SegmentTypeIds.PlanIndex/PlanNumber`. Bei fehlender PlanNumber + Auto-Import → UnknownOrManualRequired; bei B darf neuer Plan ohne PlanNummer in manuelle Erstaufnahme, aber nicht automatisch als neues logisches Dokument.

## MVP-Architektur für B
Neuer Workflow **ManualFirstCapture**: Scan → MD5 → Lightweight-Kandidaten → gegen bekannte Dokumente matchen → gruppieren (Dublette / wahrscheinl. Update / manuelle Erstaufnahme) → User bestätigt → ImportExecution schreibt. Neue Modelle `ManualPlanIdentity`, `FirstCaptureAssignment` (FileIds, Identity, CreateAsNewDocument, ConfirmationSource dragdrop|table|existing-match-confirmed). DocumentKeyBuilder weiterverwenden, aber **ID-basiert** (building_part_id statt „Haus 64"-Text). Zusätzlicher Candidate-Suchindex (plan_number+document_type_id, plan_number+building_part_id, md5_hash) — als Candidate-Suche, nicht Unique-Zwang.

## ✅ Einigkeit
B für MVP stärker; Schema v2.0 trägt B; A bleibt Assist/Vorbefüller; Alias+OCR aus V1-Muss raus; Feldkey-Bug sofort zentral fixen; MD5 = Dublettenbeweis nicht Revisionsbeweis; Plannummer nur Suchanker, finale Identität über document_key/manuell bestätigte Stammdaten.

## ⚠️ Widerspruch
Drag&Drop-Matrix nicht als alleinige V1-UI (skaliert schlecht); „Plannummer-Anker + MD5 reicht" nur für Dublette + Kandidatensuche, nicht Update-Entscheidung (braucht document_key/User-Bestätigung); building_part_aliases nicht in V1 erzwingen (B nutzt manuelle ID-Zuordnung); Regex nicht komplett streichen (kleiner PlanNumber/Index-Extractor bleibt); neue Dokumente ohne bestätigte Identität nicht automatisch importieren.

## ❓ Rückfragen
1. Manueller document_key bei B: stabile IDs (building_part_id/building_level_id) oder normalisierte Anzeigenamen?
2. building_level_id im MVP Teil der Erstaufnahme oder V1 nur building_part_id + document_type + plan_number?
3. V1-UI zuerst Tabellen-/Bulk-Editor, Matrix nur später?
4. PDF+DWG-Paare: gemeinsam markieren + als eine Revision verknüpfen, oder nachträglich automatisch über gleichen document_key?
5. Update zu bekanntem Plan automatisch akzeptieren wenn plan_number+document_type+building_part_id eindeutig, oder immer Preview-Bestätigung?

---

## ⚡ User-Input (Herbert, am Ende r2)
Weitere UI-Idee: 1..n Dateien markieren, kurz festhalten → **Ring/Radial-Menü** um den Mauszeiger (mit etwas Abstand) mit Feldern; zuerst Plantyp, dann Bauteil, dann Geschoss…; jeder weitere Unterpunkt taucht neben dem Überpunkt-Feld als neues Feld auf. Vermutung: dazu existiert evtl. schon ein Mockup. → zu prüfen.
