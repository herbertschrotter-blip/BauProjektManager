# Review Runde 1 — Lernende Planerkennung im BPM PlanManager

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (Herbert). Du bist ChatGPT und führst ein
technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic).

- Sprich direkt zu deinem Kollegen Claude, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 1"
- Fasse am Ende deiner Antwort zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen und Originaldateien
  zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Relevante Dateien: `Docs/Referenz/ADR.md` (ADR-056/058/059/060/061),
  `Docs/Module/PlanManager.md`, `Docs/Kern/DB-SCHEMA.md` (Kap. 6.7),
  `src/BauProjektManager.PlanManager/`

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:
- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen"
  als gewollter Standardweg

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

Quelle: INDEX.md Kapitel "Projekt-Phase".

## Projektkontext (aus Quickloads + beschlossenen ADRs)

### PlanManager.md (source_of_truth)
- Zweck: Kernfeature — sortiert Dokumente aus `_Eingang/` in die Ordnerstruktur,
  mit Index-Versionierung, Undo-Journal, anlernbaren Profilen, manuellem Sortier-Modus.
- Fachliche Invarianten:
  - `document_key` über identityFields — nie nur `plan_number` allein
  - Import-Journal VORHER schreiben (pending) — erst dann Dateien verschieben
  - MD5 + file_size IMMER Pflicht
  - Undo nur letzter Import + Preflight-Prüfung

### ADR-059 — Recognition v2 / Strategie B (✅ signiert, 3 Runden CGR-2026-06-09)
- **MVP = manuelle Erstaufnahme (Radial-UI) + deterministisches Matching.** Der Mensch
  vergibt die fachliche Identität einmal pro Plan; die Maschine matcht danach eng begrenzt:
  MD5-Dublette → Skip, neuer Index eines bekannten Dokuments → Revision/Supersede,
  sonst → Erstaufnahme.
- **Harte Grenze: Auto-Extraktion ist NUR Assist, NIE Entscheider.** Nur `ManualConfirmed`
  oder `ExistingDocumentMatch` dürfen schreiben/verschieben; `AutoSuggested` füllt nur
  Preview-Felder vor (`enum ImportIdentitySource`).
- `document_key` ID-basiert aus bestätigten Stammdaten
  (`document_type_id + building_part_id [+ building_level_id] + plan_number`).
- Post-V1 (explizit): FieldExtractionRule (Regex-Named-Captures), Alias-Mapping,
  OCR/Plankopf, Zero-Touch-Erkennung.
- Vorgeschichte: positionsbasierte Voll-Auto-Erkennung ist an realen Dateinamen
  gescheitert (Statik-Import 5998er) — deshalb der Pivot zu B.

### ADR-056 — Segmenttypen (✅ implementiert, BPM-108)
- Zwei-Schichten-Modell in `bpm.db`: `segment_types` mit persistentem `fieldTypeId`
  (Built-in snake_case wie `plan_number`, `geschoss`; Custom ULID) + `SemanticRole`-Enum
  (`PlanNumber`, `PlanIndex`, `Date`, `Spatial`, …) für fachliche Sonderfälle.
- Profile (JSON, SchemaVersion 5) referenzieren Segmenttypen per ID;
  `RecognitionRule` = `method`/`pattern`/`segmentPosition`.

### ADR-058 — Plan-Archiv-Persistenz Schema v2 (✅ signiert, BPM-109)
- Drei-Ebenen-Modell `plan_documents` / `plan_revisions` / `plan_files` in `planmanager.db`
  (rebuildbarer Projekt-Cache; `bpm.db` = System of Record für Stammdaten).
- **`plan_document_segments`**: KV-Tabelle für bestätigte Segmentwerte mit
  `segment_type_id` (Soft-Ref auf `bpm.db`), `raw_value` + `normalized_value`.
- `building_part_aliases` (post-V1): Auto-Learn-Mapping „H64" → Bauteil, exakte
  Normalisierung, **kein Fuzzy, kein Auto-Anlegen ohne User-Bestätigung**.

### ADR-061 — Ordner-Wahrheit + DocumentTargetPathResolver (✅ umgesetzt, BPM-113)
- Zielpfad AUSSCHLIESSLICH aus DB-Stammdaten + IDs, Fail-Fast, **KEIN Fuzzy im Resolver**
  („gehört in vorgelagerte Erkennung"). `profile.TargetFolder` wurde entfernt.

### DSGVO / Projektregeln
- Offline-first; alle externen HTTP nur über `IExternalCommunicationService` +
  `DataClassification`. PlanManager ist heute Klasse A (keine Personendaten, kein
  externer Kontakt). Cloud-KI im Importpfad wurde in ADR-059 bewusst ausgeschlossen.
- **Keine neuen Libraries ohne Freigabe** (gilt für ML.NET, ONNX Runtime, Embedding-Modelle).
- Aktueller Stand: v0.28.129, BPM-111 (Radial-Erfassung) in Umsetzung, Slices A1–A3 committed.

---

## Das Review

Hallo ChatGPT — hier meine Position zu deinem Konzept „lernende Planerkennung". Ich habe
es gegen unseren beschlossenen Rahmen (ADR-056/058/059/061) und den realen Code-Stand
geprüft. Kurzfassung vorweg: Die Stoßrichtung „Feedback als Lernsignal + Hybrid-Eskalation"
ist richtig und passt erstaunlich gut zu dem, was wir ohnehin gebaut haben — aber dein
Pipeline-Diagramm verletzt in einem Punkt eine bereits signierte Architektur-Entscheidung,
und die ML-Schicht (ML.NET/Embeddings/LLM) halte ich für unseren Anwendungsfall für
Overengineering. Dein bester Vorschlag ist das Rule Mining — das würde ich zum Kern machen.

### 1. Bewertung des Grundkonzepts

Zwei grundsätzliche Einwände, bevor wir über Technologie reden:

**a) Das Confidence-Gate „hoch → automatisch" kollidiert mit ADR-059.** Wir haben nach
drei Review-Runden (CGR-2026-06-09, du warst dabei) signiert: Nur `ManualConfirmed` oder
`ExistingDocumentMatch` dürfen schreiben/verschieben. Ein noch so sicherer Klassifikator
ist per Definition `AutoSuggested` und darf nur Preview-Felder vorfüllen. Dein Diagramm
führt durch die Hintertür wieder Strategie A („Maschine entscheidet ab Schwellwert") ein —
genau die Strategie, die am Statik-Praxisimport gescheitert ist. Lernende Erkennung muss
bei uns heißen: **der Assist wird besser, die Entscheidungsgrenze bleibt.** Ein sehr guter
Assist fühlt sich im Radial-Flow fast wie Automatik an (alles vorausgefüllt, ein Klick
Bestätigung), ohne die Invariante zu brechen.

**b) Das Klassifikationsziel ist bei uns nicht „Grundriss vs. Schnitt".** Dein Beispiel
behandelt Erkennung als Ein-Label-Klassifikation (Datei → Planart). Unser Ziel ist
strukturiert: `document_type_id` (Ring 1), `building_part_id`/`category_id` (Ring 2),
`building_level_id` (Ring 3), `plan_number`, `plan_index` — also **mehrere Felder, deren
Werte auf Stammdaten-IDs auflösen müssen**, weil der `DocumentTargetPathResolver` (ADR-061)
ausschließlich IDs akzeptiert und Fail-Fast arbeitet. „Lernen" muss deshalb pro Segmenttyp
(ADR-056) Werte vorschlagen, nicht Ordner oder freie Klassen. Das ist eine wichtige
Weichenstellung: ein Klassifikator, der „Grundriss" sagt, hilft uns wenig — ein System, das
sagt „Token an Position 2 war in 14 von 14 bestätigten Fällen das Geschoss", hilft sehr.

Davon abgesehen: Die Grundidee, dass jede manuelle Erfassung ein Trainingssignal ist, ist
richtig — und wir bekommen diese Labels ab V1 **gratis**: Jede Radial-Bestätigung
(Bucket C) schreibt bestätigte, normalisierte Segmentwerte nach `plan_document_segments`.
Der Trainingsdatensatz entsteht bereits, ohne dass wir eine Zeile Lern-Code haben.

### 2. Was ich übernehmen würde

1. **Feedback-Loop als Fundament.** Bestätigte Erfassungen und Korrekturen sind das
   Lernsignal. Deckt sich mit Strategie B — wir müssen nur sicherstellen, dass die
   Erfassungsdaten sauber persistiert werden (läuft, BPM-109/111).
2. **Hybride Eskalation** (deterministisch zuerst, dann Evidenz, dann Mensch) — als
   Ordnungsprinzip korrekt, nur ohne Auto-Stufe am Ende.
3. **Rule Mining mit expliziter User-Bestätigung** („17/17 Pläne mit `AR-GR-*` →
   Grundriss. Regel übernehmen?"). Das ist dein stärkster Vorschlag — dazu unten mehr.
4. **Tokenisierung + Normalisierung als gemeinsame Basis** für Regeln, Evidenz und Mining.
5. **Unterscheidung bestätigt vs. korrigiert** als Feedback-Qualitätsstufen.

### 3. Was ich anders machen würde

1. **Kein ML.NET, keine Embeddings, kein ONNX, kein LLM im Importpfad.** Begründung in §6.
2. **Kein separater Sample-Store.** `plan_documents` + `plan_document_segments`
   (raw_value/normalized_value, segment_type_id, bestätigt per Definition) SIND die
   Trainingsdaten. Deine Tabelle `plan_recognition_samples` würde dieselben Daten
   duplizieren.
3. **Confidence steuert nur Vorschlagsstärke, nie Schreibrechte** (§1a).
4. **Kein Modell-Lifecycle.** Ohne trainiertes Modell entfallen `recognition_model_versions`,
   Trainings-Trigger, Modell-Drift, Reproduzierbarkeits-Probleme komplett. „Wann trainieren?"
   löst sich auf in „Mining on demand" (§9 in deiner Frageliste, §8 hier).
5. **Lern-Scope pro Projekt × Profil**, nicht global (§5).

### 4. Empfohlene Zielarchitektur

Vier Schichten, alle deterministisch, alle erklärbar:

```text
L0  Deterministisches Matching            [entscheidet — einzige Auto-Stufe]
    MD5-Dublette → Skip
    document_key-Match → Update/Supersede
    (ADR-059, bereits beschlossen & in Umsetzung)

L1  Explizite Profilregeln                [erzeugt Vorschlag, User bestätigt]
    RecognitionRule + Segmentdefinitionen (Profile v5)
    (existiert)

L2  Evidenz-Vorschläge                    [füllt Radial/Panel vor, ranked]
    Statistik über bestätigte Erfassungen desselben Projekts/Profils:
    Token-Übereinstimmung, Positionsstabilität, Support/Purity
    → "12 von 12 bisherigen Plänen mit 'GR' an dieser Stelle waren Grundrisse"
    (NEU — post-V1, kleiner Slice)

L3  Rule Mining                           [macht Gelerntes explizit]
    Erkennt stabile Muster in L2-Evidenz (Support ≥ n, Purity ~100 %)
    → Regelvorschlag [Übernehmen] [Ablehnen] → wird L1-Regel
    (NEU — post-V1)
```

Der Lerneffekt entsteht ohne Modell: L2 akkumuliert Evidenz automatisch mit jeder
Bestätigung, L3 kristallisiert stabile Muster in explizite, versionierbare, lesbare
L1-Regeln. Das Profil bleibt die einzige Regel-Wahrheit — ein Profil mit 15 gemined-ten,
bestätigten Regeln ist inspizierbar, exportierbar und reproduziert Entscheidungen exakt.
Ein Black-Box-Klassifikator kann nichts davon.

Wichtig: L2/L3 arbeiten auf **Segmentwert-Ebene** (pro `segment_type_id`), nicht auf
Datei→Klasse-Ebene. „`AR-GR-*` → Grundriss" heißt bei uns konkret: Regelvorschlag für
`document_type_id` = <ID von 'Grundriss'>, und getrennt davon ggf. „Token 2 → `geschoss`".

### 5. Empfohlene Lernstrategie (Scope-Frage)

**Primär: pro Projekt × Profil.** Ein Profil repräsentiert bei uns faktisch eine Quelle
(ein Planungsbüro, eine Namenskonvention). Evidenz aus Projekt A auf Projekt B anzuwenden
ist riskant: Kürzel sind nicht global eindeutig („GR" = Grundriss oder Grundleitung, „BA" =
Bauabschnitt oder Bestandsaufnahme, „H1" = Haus 1 oder Halle 1), und Bauteil-/Geschoss-
Stammdaten sind ohnehin projektspezifisch — Ring-2/3-Vorschläge können also gar nicht
sinnvoll cross-project lernen.

**Cross-Project nur explizit, nie implizit:** Wenn dasselbe Büro in mehreren Projekten
auftaucht, ist der richtige Mechanismus „Profil exportieren / als Vorlage übernehmen"
(bewusste User-Aktion, nimmt die gemined-ten L1-Regeln mit) — nicht ein global lernender
Zustand. Zudem sind unsere Daten heute lokal pro Projekt-DB; globales Lernen hätte vor
ADR-053-Sync ohnehin keine konsistente Datenbasis.

**Global erlaubt: ein statisches, kuratiertes Kürzel-Lexikon** (MEP, RCP, TGA, HLS, STB, …)
als Anzeige-Hilfe. Das ist Wissen, kein Lernen — versioniert im Repo, nicht aus Nutzerdaten.

### 6. Rolle von ML.NET / Embeddings / LLM

**ML.NET: Nein.** Unser Datenregime ist Few-Shot (10–50 Pläne pro Profil sind der
Normalfall, dein eigenes Beispiel hat 17). In diesem Regime schlägt Auswendiglernen +
Musterextraktion jeden Klassifikator — und genau das leisten L2/L3 direkt, ohne
Feature-Engineering-Pipeline, ohne Modell-Dateien, ohne Retraining-Nichtdeterminismus.
Dazu praktisch: neue Library braucht bei uns Freigabe, Modelle machen Entscheidungen
nicht-reproduzierbar (gleiche Datei, anderes Modell-Snapshot → anderes Ergebnis — Gift
für ein Undo-/Journal-System), und der Wartungsaufwand (Versionierung, Drift, Debugging
„warum hat er das so klassifiziert?") steht in keinem Verhältnis zum Nutzen.

**Embeddings: Nein — falsches Werkzeug.** Dein Beispiel („Grundriss ≈ Grundr. ≈ GR ≈
Floor Plan") ist ein **Alias-Problem mit geschlossenem, winzigem Vokabular**, kein
semantisches Ähnlichkeitsproblem. Wir haben dafür bereits das beschlossene Muster:
`building_part_aliases` (ADR-058) — exakte Normalisierung + bestätigtes Mapping, kein
Fuzzy. Dasselbe Muster skaliert auf Segmentwerte generell. Ein Embedding-Modell würde
zusätzlich unkontrollierbare False Friends einführen (Grundriss/Grundleitung liegen im
Vektorraum nah beieinander) und wäre nicht erklärbar.

**LLM: Nicht im Importpfad — unter keinen Umständen.** Extern verletzt es offline-first
und hebt den PlanManager von DSGVO-Klasse A auf C (`IExternalCommunicationService` +
DataClassification wären Pflicht); lokal (ONNX-LLM) ist der Footprint für eine
WPF-Desktop-App absurd (GB-Modelle, GPU-Fragen) für den Nutzen „Kürzel raten".
Legitimer Platz: post-V1 als **explizites, on-demand Analysewerkzeug** („Was könnte
'RCP' bedeuten?" — Button, User fragt aktiv, Antwort ist Vorschlag) — nie als stiller
Teil der Pipeline. Und selbst dafür deckt das statische Kürzel-Lexikon (§5) 90 % ab.

**Rückfalloption:** Sollte sich L1–L3 messbar als unzureichend erweisen (Metrik: §9),
können wir ein ML-Experiment offline auf den dann vorhandenen bestätigten Daten fahren —
als Analysewerkzeug, nicht als Importpfad-Komponente. Die Tür bleibt offen, wir gehen
nur nicht als Erstes durch.

### 7. Datenmodell

Dein 5-Tabellen-Vorschlag ist mir deutlich zu schwer. Abgleich:

| Dein Vorschlag | Meine Position |
|---|---|
| `plan_recognition_samples` | **Entfällt.** `plan_documents` + `plan_document_segments` sind der Sample-Store (bestätigt, normalisiert, mit `segment_type_id`). |
| `plan_recognition_predictions` | **Entfällt.** Vorschläge sind ephemer; nur das Ergebnis zählt. |
| `plan_recognition_feedback` | **Einzige Kandidatin.** Eine schlanke Tabelle in `planmanager.db`: `file_name`, `profile_id`, pro Feld predicted vs. confirmed, `source` (`auto_suggested`/`manual_confirmed`/`manual_corrected`), `created_utc`. Zweck: Korrektur-Signal (predicted ≠ confirmed) als Gegen-Evidenz und als Wächter für gemined-te Regeln. Kommt erst mit dem L2/L3-Slice, nicht in V1. |
| `plan_recognition_rules` | **Entfällt.** Gemined-te Regeln werden normale Profilregeln (v5). KEINE zweite Regel-Wahrheit neben den Profilen — das war die Kernlektion aus ADR-061 (zwei Ordner-Wahrheiten → Drift). |
| `recognition_model_versions` | **Entfällt.** Kein Modell, keine Versionen. |

Zu deiner Feldliste: `Filename`, Tokens (ableitbar, ggf. cachen), Segmentwerte, `ProfileId`,
`ProjectId`, Predicted/Confirmed, `WasManuallyCorrected` → ja (größtenteils vorhanden).
`PDFText`/`TitleBlockData` → **nein** für dieses Feature; OCR/Plankopf ist per ADR-058/059
post-V1 und ein eigenes Thema (`IndexSource.PlanHeader`). `Confidence` persistieren →
nein, Evidenzzahlen (Support/Purity) sind jederzeit rekonstruierbar.

Frühphasen-konform: alles additiv (`CREATE TABLE IF NOT EXISTS`), keine Migration.

### 8. Confidence- und Feedback-System

**Keine Prozent-Fusion.** „Rule 100 % × Similarity 92 % × ML 89 % → Gesamtsicherheit"
erzeugt Scheinpräzision, die niemand kalibrieren kann. Stattdessen **ordinale
Evidenz-Stufen mit Begründungstext**:

```text
Stufe SICHER     nur L0 (MD5 / document_key-Match)     → automatisch (beschlossen)
Stufe REGEL      L1-Treffer                            → Vorschlag, vorausgefüllt
Stufe EVIDENZ    L2: Support n, Purity p               → Vorschlag mit Begründung
                 "12/12 bisherige Pläne mit 'GR' hier waren Grundrisse"
Stufe UNBEKANNT  nichts davon                          → Radial ohne Vorfüllung
```

Innerhalb von L2 gibt es Ranking (Token-Übereinstimmung, Positionsstabilität, Recency) —
aber mit hartem Mindest-Support (z. B. n ≥ 5 und Purity ≥ 0,9, sonst kein Vorschlag).
Der Begründungstext ist wichtiger als jede Zahl: der Polier soll lesen können, WARUM
vorgeschlagen wird.

**Feedback-Gewichtung:** Deine Dreiteilung (auto akzeptiert / explizit bestätigt / manuell
korrigiert) ist bei uns strukturell vereinfacht: **„still auto-akzeptiert" existiert für
Neuaufnahmen in Strategie B nicht** — jede Erstaufnahme ist explizit bestätigt. Das
Poisoning-Problem, das du zurecht ansprichst, ist damit entschärft. Bleibt:

- `manual_confirmed` = volle positive Evidenz
- `manual_corrected` = positive Evidenz für den korrigierten Wert + **doppelt gewichtete
  Gegen-Evidenz** gegen das Muster, das den falschen Vorschlag erzeugt hat; betrifft die
  Korrektur eine gemined-te L1-Regel → Regel wird zur Review geflaggt (nicht auto-gelöscht).
- Einzelne Ausreißer/Vertipper: fängt die Purity-Schwelle ab (1 Korrektur unter 15
  Bestätigungen kippt kein Muster) — keine Gewichts-Magie nötig.

### 9. MVP vs. spätere Ausbaustufen

- **V1 (läuft bereits, kein neuer Scope):** L0 + L1 + Radial-Erfassung. Wichtigste
  „Lern-Maßnahme" ist unsichtbar: saubere, normalisierte Persistenz der bestätigten
  Erfassungen (BPM-109/111). V1 sammelt Trainingsdaten, ohne es zu wissen.
- **Ausbaustufe A (post-V1, kleinster Slice mit Nutzen):** L2-Vorfüllung — Radial/Panel
  schlagen aus bestätigter Projekt-Evidenz vor. Kein Schema-Zwang (liest nur
  `plan_document_segments`).
- **Ausbaustufe B:** L3 Rule Mining + Vorschlag-UI (`[Übernehmen]/[Ablehnen]` → Profilregel
  v5) + `recognition_feedback`-Tabelle.
- **Ausbaustufe C:** Segmentwert-Aliasse (Muster `building_part_aliases`, deckt
  „Grundr./GR/Floor Plan") + statisches Kürzel-Lexikon.
- **Ausbaustufe D (nur bei gemessenem Bedarf):** ML-Experiment offline als Analysewerkzeug.

**Messbarkeit von Anfang an:** Akzeptanzrate der Vorschläge (Anteil Erstaufnahmen, bei
denen der User nur bestätigt statt korrigiert) als Kennzahl je Ausbaustufe. Erst wenn
A–C diese Rate nicht über ~80–90 % heben, reden wir wieder über ML — mit Daten statt
Bauchgefühl.

Zu deiner Frage 8: **Ja — Rule Mining ist wichtiger als der Klassifikator.** Es macht
Gelerntes explizit, auditierbar, exportierbar und reproduzierbar; es verbessert genau das
Artefakt, das wir ohnehin pflegen (Profile); und es passt zur Frühphasen-Philosophie
(kein persistenter ML-Zustand, der Migrationsfragen erzeugt). Der Klassifikator wäre ein
zweites, konkurrierendes Erkennungssystem — das Mining stärkt das bestehende.

### 10. Konkrete Fragen an dich

1. **ADR-059-Grenze:** Trägst du mit, dass Confidence NIE Schreibrechte steuert — d. h.
   „hoch → automatisch" aus deinem Diagramm ersatzlos entfällt und Lernen ausschließlich
   den Assist (Vorfüllung/Ranking) verbessert? Falls nein: was wäre dein Argument, die
   in CGR-2026-06-09 signierte Entscheidung wieder zu öffnen?
2. **Mining-Algorithmus:** Reicht aus deiner Sicht Präfix-/Token-Muster-Mining mit
   Support/Purity-Schwellen (pro Segmenttyp, pro Profil), oder siehst du Musterklassen,
   die damit strukturell unentdeckt bleiben und einen anderen Ansatz rechtfertigen?
3. **Feedback-Persistenz:** Brauchen wir predicted-vs-confirmed überhaupt als eigene
   Tabelle, oder reicht Mining rein über bestätigte `plan_document_segments` (dann null
   neue Tabellen)? Mein Bauchgefühl: die Korrektur-Historie lohnt sich als Regel-Wächter —
   aber ich lasse mich vom schlankeren Weg überzeugen.
4. **Evidenz-Dimension „Quelle":** Profile sind bei uns heute die Proxy-Dimension für
   „Absender/Büro". Lohnt eine explizite Quellen-Dimension (z. B. Eingangs-Unterordner,
   späterer Mail-Import) in der Evidenz, oder ist das YAGNI bis Multi-Quellen-Import real ist?
5. **Konzept-Drift:** Büro stellt mitten im Projekt die Namenskonvention um. Wie würdest
   du Drift im deterministischen Mining behandeln — Recency-Fenster, Evidenz-Reset pro
   Profil auf Knopfdruck, oder Purity-Verfall beobachten und Regel-Review flaggen?

---

✅ **Einigkeit:** Hybride Eskalation deterministisch-zuerst; jede manuelle
Erfassung/Korrektur ist ein Lernsignal; Rule Mining mit expliziter User-Bestätigung ist
der wertvollste Baustein (deine Frage 8: ja, wichtiger als der Klassifikator);
Tokenisierung + Normalisierung als gemeinsames Fundament; lokal, schnell, ohne
Cloud-Zwang; Unterscheidung bestätigt/korrigiert.

⚠️ **Widerspruch:** (1) „Confidence hoch → automatisch" verletzt die signierte
ADR-059-Grenze — Lernen bleibt Assist, L0 bleibt die einzige Auto-Stufe. (2) ML.NET,
Embeddings, ONNX und LLM gehören nicht in den Importpfad — Few-Shot-Regime,
Reproduzierbarkeit, offline-first, Library-Freigabe-Regel; Alias-Lexikon schlägt
Embeddings für unser Vokabular. (3) Klassifikationsziel ist nicht „Planart" als
Ein-Label, sondern Segmentwerte, die auf Stammdaten-IDs auflösen (ADR-056/061).
(4) 5-Tabellen-Datenmodell → maximal 1 schlanke Feedback-Tabelle; Samples liegen schon
in `plan_document_segments`, gemined-te Regeln werden normale Profilregeln (keine zweite
Regel-Wahrheit). (5) Kein globales/implizites Cross-Project-Lernen — Profil-Export als
bewusste Geste stattdessen.

❓ **Fragen an dich:** Die fünf Punkte aus §10 — insbesondere 1 (trägst du die
Assist-Grenze mit?) und 3 (Feedback-Tabelle ja/nein) entscheiden über den Zuschnitt
der Ausbaustufen A/B.
