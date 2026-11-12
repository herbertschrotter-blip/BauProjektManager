---
doc_id: glossar
doc_type: reference
authority: secondary
status: active
owner: herbert
topics: [glossar, fachbegriffe, bauwesen, österreich, abkürzungen, bpm-codebasis, wording-regel, dokumenttyp]
read_when: [fachbegriff-unklar, österreichische-bau-terminologie, codebasis-begriffe, wording-regel-pruefen]
related_docs: [architektur, planmanager]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Fachbegriffe aus dem österreichischen Bauwesen und der BPM-Codebasis
- Autorität: secondary
- Lesen wenn: Fachbegriff unklar, österreichische Bau-Terminologie, Codebasis-Begriffe nachschlagen
- Nicht zuständig für: Code-Konventionen (→ CODING_STANDARDS.md), Architektur (→ Architektur.md)
- Kapitel:
  - 1. Rollen auf der Baustelle
  - 2. Bautechnische Begriffe
  - 3. Planmanagement
  - 4. Projektarten und -typen
  - 5. Projekt-Beteiligte (Rollen)
  - 6. Bauherren-Portale
  - 7. Kalkulation und Nachkalkulation
  - 8. Zeiterfassung
  - 9. Ordnerstruktur (Projektordner)
  - 10. BPM-Dateien und Speicherorte
  - 11. Technische Begriffe (Software)
  - 12. Cloud-Speicher
  - 13. Abkürzungen (Kurzreferenz)
  - 14. Datenschutz (DSGVO)
- Pflichtlesen: keine (Nachschlagewerk)
- Fachliche Invarianten:
  - Österreichische Terminologie in UI-Texten (Polier, Geschoß, RDOK); technische Keys bleiben ASCII (z. B. `geschoss`)
  - Begriffe alphabetisch innerhalb jeder Kategorie
  - Wording-Regel (Section 3): Dokumenttyp = Oberbegriff und Stammdaten-Hauptobjekt, Plantyp = Synonym im engeren Sinn (vermeiden), RecognitionProfile = optionale Erkennungs-Konfiguration (0..1 pro Dokumenttyp)
  - „Archiv“ nur für abgelöste Versionen; „Klasse A/B/C“ nur für DSGVO-Datenklassen (Sync: Sync-Kategorie)

---

# BauProjektManager — Glossar

**Erstellt:** 30.03.2026  
**Version:** 1.0  
**Zweck:** Fachbegriffe aus dem österreichischen Bauwesen und der BPM-Codebasis für KI-Assistenten und externe Entwickler.

---

## 1. Rollen auf der Baustelle

| Begriff | Erklärung |
|---------|-----------|
| **Polier** | Vorarbeiter auf der Baustelle. Führt die Kolonne, koordiniert die tägliche Arbeit, weist Arbeiter Tätigkeiten zu. Herberts Hauptrolle. |
| **Bauleiter** | Technischer Projektleiter auf der Baustelle. Verantwortlich für Kosten, Termine, Qualität. Herbert ist beides (Polier + Bauleiter). |
| **ÖBA** | Örtliche Bauaufsicht. Vertritt den Bauherrn auf der Baustelle, überwacht die Ausführung. Externe Rolle, nicht die eigene Firma. |
| **Auftraggeber** | Vertragspartner, der die eigene Baufirma beauftragt (Person oder Firma). In BPM als `Client` modelliert (UI-Sektion „Auftraggeber“). Kann der Bauherr sein, bei GU-Aufträgen aber auch ein anderer (z. B. der Generalunternehmer). (Audit Review 01, R01-001 E16, entschieden 2026-09-24) |
| **Bauherr** | Person oder Firma, für die das Bauwerk errichtet wird. In BPM eigene Beteiligtenrolle (`Bauherr` in den Default-Rollen), nicht gleichbedeutend mit `Client`: bei GU-Aufträgen sind Auftraggeber und Bauherr verschieden. (Audit Review 01, R01-001 E16, entschieden 2026-09-24) |
| **Kolonne** | Arbeitsgruppe auf der Baustelle, typisch 3–6 Facharbeiter + eventuell Lehrlinge. |
| **Dispo** | Disposition. Koordiniert Materialtransporte, LKW-Einsatz, Maschinenverfügbarkeit zwischen Büro und Baustelle. |
| **Lager** | Firmeneigenes Materiallager. Prüft Verfügbarkeit, kommissioniert, organisiert interne Lieferungen. |
| **Einkauf** | Beschafft Material extern bei Lieferanten/Baustoffhändlern. |

---

## 2. Bautechnische Begriffe

### 2.1 Geschoß-Höhen (Tab 2 Bauwerk)

| Kürzel | Langform | Erklärung | Im Code |
|--------|----------|-----------|---------|
| **RDOK** | Rohdecken-Oberkante | Oberkante der Betondecke (roh, ohne Estrich/Belag). Beim untersten Geschoß = BPOK. Eingabewert in Meter von ± 0,00. | `BuildingLevel.Rdok` |
| **BPOK** | Bodenplatten-Oberkante | Oberkante der Bodenplatte (nur unterstes Geschoß). Wird im Code als RDOK geführt. | `BuildingLevel.Rdok` (bei unterstem Geschoß) |
| **FBOK** | Fertigfußboden-Oberkante | Oberkante des fertigen Bodens (mit Estrich + Belag). EG hat typisch FBOK = 0,00. Eingabewert. | `BuildingLevel.Fbok` |
| **RDUK** | Rohdecken-Unterkante | Unterkante der Betondecke (Sichtseite von unten). Eingabewert. Beim untersten Geschoß null. | `BuildingLevel.Rduk` |
| **± 0,00** | Bezugsniveau / Meterriss | Referenzhöhe auf der Baustelle, typisch Oberkante Fertigfußboden EG. Alle Höhen werden relativ dazu angegeben. Pro Bauteil kann das absolute Niveau unterschiedlich sein. | `BuildingPart.ZeroLevelAbsolute` |
| **Meterriss** | — | Markierung exakt 1,00 m über ± 0,00, an den Wänden angezeichnet. Bezugslinie für alle Höhenmessungen auf der Baustelle. |

### 2.2 Errechnete Werte (nicht in DB gespeichert)

| Wert | Formel | Erklärung | Im Code |
|------|--------|-----------|---------|
| **Geschoßhöhe** | FBOK(oben) − FBOK(unten) | Lichte Höhe von Fertigfußboden zu Fertigfußboden des darüberliegenden Geschoßes. | `BuildingLevel.StoryHeight` |
| **Rohbauhöhe** | RDOK(oben) − RDOK(unten) | Höhe von Rohdecke zu Rohdecke. Relevant für Schalungshöhe und Mauerwerk. | `BuildingLevel.RawHeight` |
| **Deckenstärke** | RDOK(n+1) − RDUK(n) | Dicke der Betondecke (Decke darüber minus UK aktuell). Seit v0.24.2 korrigiert — war vorher fälschlich RDOK − RDUK (gleiche Zeile). | `BuildingLevel.DeckThickness` |
| **Fußbodenaufbau** | FBOK − RDOK | Schichtdicke zwischen Rohdecke und Fertigfußboden (Estrich, Dämmung, Belag). | `BuildingLevel.FloorBuildup` |

### 2.3 Geschoß-Bezeichnungen

| Kürzel | Langname | Präfix (Sortierung) |
|--------|----------|---------------------|
| FU | Fundament | −03 (oder tiefer) |
| UG3 | 3. Untergeschoß | −03 |
| UG2 | 2. Untergeschoß | −02 |
| UG | Untergeschoß | −01 |
| **EG** | **Erdgeschoß** | **00** (Bezugsniveau) |
| OG1 | 1. Obergeschoß | 01 |
| OG2 | 2. Obergeschoß | 02 |
| DG | Dachgeschoß | 03 (oder höher) |

Im Code: `LevelNameEntry` mit `ShortName` + `LongName` (`SharedConfig.LevelNames`), editierbar in `shared-config.json`.

### 2.4 Bauwerk-Struktur

| Begriff | Erklärung | Im Code |
|---------|-----------|---------|
| **Bauteil** (BuildingPart) | Ein eigenständiger Gebäudeabschnitt mit eigenen Geschoßen und Höhen. Z.B. "Haus 64", "Tiefgarage", "Stiegenhaus". Ein Projekt kann mehrere Bauteile haben. | `BuildingPart` |
| **Bauabschnitt** | Synonym für Bauteil in manchen Kontexten (umgangssprachlich). Im Code eigener Built-in-Segmenttyp `bauabschnitt` — Abbildung siehe Hinweis unter der Tabelle. | `SegmentTypeIds.Bauabschnitt` |
| **Bauwerkstyp** (BuildingType) | Art des Gebäudes: EFH (Einfamilienhaus), MFH (Mehrfamilienhaus), Wohnanlage, Gewerbe, Industrie. Pro Bauteil separat — ein Projekt kann Wohnen + Gewerbe mischen. | `BuildingPart.BuildingType` |
| **Thermofuß** | Wärmedämmendes Element am Fußpunkt einer Wand (Übergang Bodenplatte/Decke zur Außenwand). Relevant für Höhenberechnung bei Ziegelmauerwerk. |
| **Scharrenrechner** | Berechnet die optimale Kombination von Normal-Scharren (24,9 cm) und Höhenausgleich-Scharren (21,9 cm) für eine gegebene Raumhöhe. Ziel: Wandhöhe exakt treffen mit möglichst wenig Schneiden. |
| **Scharre** | Eine Ziegellage (eine Reihe Ziegel in der Höhe). Normal-Scharre = 24,9 cm, Höhenausgleich-Scharre = 21,9 cm (bei Wienerberger Planziegel). |

> **Fachlich festzulegen:** Die Built-in-Segmenttypen `haus`, `bauteil` und `bauabschnitt` sind im Seed getrennt (ADR-056). Wie sie auf die Stammdaten `building_parts` abgebildet werden, ist noch fachlich festzulegen. Ist-Stand: Die Radial-Erfassung schreibt den gewählten Bauteil als Segment `bauteil` (`CaptureConfirmService`). (Audit Review 01, R01-001 E16, entschieden 2026-09-24)

### 2.5 Grundstück und Vermessung

| Begriff | Erklärung | Im Code |
|---------|-----------|---------|
| **KG** | Katastralgemeinde. Verwaltungseinheit im österreichischen Grundbuch. Jede KG hat eine Nummer und einen Namen. | `ProjectLocation.CadastralKg` / `.CadastralKgName` |
| **GST** | Grundstücksnummer innerhalb einer KG. Eindeutige Parzelle im Kataster. | `ProjectLocation.CadastralGst` |
| **EPSG:31258** | Koordinatensystem "MGI / Austria GK M31". Standard für Vermessung in der Steiermark. Ostkoordinate + Nordkoordinate in Metern. | `ProjectLocation.CoordinateSystem` |
| **GIS Steiermark** | Öffentliches Geoinformationssystem des Landes Steiermark. ArcGIS REST API, kein API-Key nötig. Liefert KG, GST, Koordinaten zu einer Adresse. |
| **Leica** | Vermessungsgeräte (Totalstation, GPS). Projektordner "03 Leica" enthält Absteckpläne und Aufmaß-Daten. |

---

## 3. Planmanagement

### Wording-Regel (verbindlich)

In Code, Docs und UI gilt diese Hierarchie:

- **Dokumenttyp** ist der **Oberbegriff** für jede fachliche Dokumentkategorie
  (Polierplan, Schalungsplan, Bauprotokoll, Prüfbericht, Baubesprechung etc.).
  Im Code: Stammdaten `document_types` (bpm.db), `documentTypeId`, `documentTypeName`,
  `DocumentTypeRecognizer`.
  Verbindlich seit PlanManager.md v2.0 ("Nicht nur Pläne") und ADR-054.
  Seit ADR-065 P.7 ist der Dokumenttyp das **Hauptobjekt**: `document_types` ist die
  gemeinsame Typ-Wahrheit für Ring 1 des Radials, Erfassung, Zielpfad und Lern-Evidenz.
- **Plantyp** ist ein **Synonym im engeren Sinn** — nur wenn der Dokumenttyp
  ein Plan ist (Polierplan, Einreichplan, Bewehrungsplan). Umgangssprachlich
  in Gesprächen mit Herbert üblich. In neuem Code/Doc bitte „Dokumenttyp"
  verwenden, „Plantyp" als Begriff vermeiden.
- **RecognitionProfile** ist die **optionale Erkennungs-Konfiguration** eines
  Dokumenttyps im Projekt — **0..1 pro Dokumenttyp** (ADR-065 P.7). Es definiert
  WIE Dateien dieses Typs erkannt werden (Tokenization, Segmente, Identity-Felder,
  IndexSource). Den Zielordner definiert es nicht mehr: der kommt aus den Stammdaten
  über den `DocumentTargetPathResolver` (ADR-061, Profil-Schema v5).
  Datei `.bpm/profiles/<id>.json`, die erst mit einer validen Erkennungs-Konfiguration
  entsteht. UI-Begriff „Erkennung“, in Docs „Erkennungsprofil“.

Ein RecognitionProfile **ist nicht** ein Dokumenttyp — es ist die Bauanleitung
zum Erkennen eines Dokumenttyps. Merkformel (ADR-065 P.7): Dokumenttyp = „was ist
das fachlich?“, Profil = „wie erkenne ich es?“.

### Begriffe

| Begriff | Erklärung | Im Code / Docs |
|---------|-----------|----------------|
| **Dokumenttyp** | Fachliche Kategorie eines Dokuments — Oberbegriff und Hauptobjekt (ADR-065 P.7). Beispiele: Polierplan, Schalungsplan, Bauprotokoll, Prüfbericht, Baubesprechung. | `document_types` (bpm.db), `documentTypeId`, `documentTypeName` (Profil-Feld), `DocumentTypeRecognizer` (Klasse, kein Interface), ADR-054, ADR-065 |
| **Dokumenttyp-Stammdaten** | Projekt-scoped Stammdaten der Dokumenttypen in `bpm.db`: Anzeigename, stabiler `key` (nach Anlage gesperrt), Ablagebereich (`root_relative_path`), Typordner (`folder_name`, leer bei Root-Typ), Radial-Farbe, Unterteilung (`ring2_source`: Bauteil/Geschoß, Kategorien oder keine), Sortierung, `is_builtin`. Typgebundene Kategorien in `document_type_categories`. Seed bei Projektanlage aus der Ordner-Vorlage, danach ist die DB die einzige Ordner-/Typ-Wahrheit. | `document_types`, `document_type_categories`, `PlanDocumentType`, ADR-059-Addendum, ADR-061 |
| **Ablagebereich** | UI-Begriff für den Ablage-Root eines Dokumenttyps relativ zum Projektordner (z. B. „01 Planunterlagen“, „06 Protokolle“). Erste Ebene des Zielpfads `Ablagebereich / Typordner / Ring 2 / Ring 3 / Datei`. | `document_types.root_relative_path`, `DocumentTargetPathResolver`, ADR-061 P.1/P.3/P.7 |
| **Plantyp** | Synonym für Dokumenttyp im engeren Sinn — wenn das Dokument ein Plan ist (Polierplan, Einreichplan, Bewehrungsplan, Schalungsplan). Umgangssprachlich; in neuem Code/Doc bitte „Dokumenttyp" verwenden. ADR-059 meint mit „Plantyp“ den Dokumenttyp in Ring 1 (ADR-065 P.7). | Altbestand im Code: registry.json-Export `PlanTypes`/`CustomPlanTypes`, `LightweightPlanExtractor` (Plantyp-Keywords, Kombiplan-Erkennung), UI-Text „Kombi-Plan erkannt (mehrere Plantypen im Namen)“ in `ManualCaptureView.xaml` |
| **RecognitionProfile** | Optionale Erkennungs-Konfiguration eines Dokumenttyps im Projekt (0..1 pro Dokumenttyp, ADR-065 P.7). Definiert Tokenization, Segmente, Identity-Felder, IndexSource — keinen Zielordner mehr (ADR-061). Eine Datei je Profil unter `.bpm/profiles/<id>.json` (ADR-046). UI-Begriff „Erkennung“. | `.bpm/profiles/`, ADR-010, ADR-022, ADR-045, ADR-061, ADR-065 |
| **PatternTemplate** | Vorschlag aus der globalen Musterbibliothek. Wenn ein neues Profil angelegt wird, schlägt BPM ähnliche bestehende Templates vor. | `pattern-templates.json` |
| **document_key** | Fachliche Identity eines Dokuments (UNIQUE in `plan_documents`). Zwei Bildungswege: (1) **Erstaufnahme** (V1-Weg): ID-basiert aus den bestätigten Stammdaten — Dokumenttyp-ID, Plannummer und Bauteil- bzw. Kategorie-ID [+ Geschoß-ID] (ADR-059 P.3 + Addendum P.6); (2) **Profil-Weg**: aus den `identityFields` des Profils (`DocumentKeyBuilder`, derzeit ohne produktiven Aufrufer). Dokumenttyp-ID und Plannummer (bei nummernlosen Typen der Dateiname) werden normalisiert (`NormalizeForKey`), Bauteil-/Kategorie-/Geschoß-IDs unverändert übernommen; Teile mit `\|` verbunden, z. B. Profil-Weg `<dokumenttyp-id>\|103\|h64`. | `CaptureConfirmService.BuildManualDocumentKey`, `DocumentKeyBuilder`, ADR-054, ADR-059 |
| **identityFields** | Liste der Profil-Felder die in den `document_key` einfließen. Segmenttyp-IDs in snake_case plus Sonderkey `documentType`. Beispiel: `["documentType", "plan_number", "haus"]`. | RecognitionProfile, `SegmentTypeIds`, ADR-054, ADR-056 |
| **Tokenization** | Segmentierung des Dateinamens an Trennzeichen, profilgebunden konfiguriert. Voraussetzung für die profilbasierte `document_key`-Bildung. | `tokenization`-Block im Profil, ADR-022, ADR-054 |
| **Stage** | Lebenszyklus-Phase eines Dokuments: `Unknown` (Default), `Draft` (VORABZUG/Vorab), `Final`. Eigenes Feld, **nicht** Teil des `document_key`. | RecognitionProfile + Erkennung, ADR-054 |
| **Plannummer** | Eindeutige Kennung eines Dokuments innerhalb eines Dokumenttyps. Kommt vom Planer (Architekt, Statiker). | Segment im Dateinamen (`plan_number`) |
| **Planindex** / **Revision** | Versionsstand eines Dokuments. Typisch: A, B, C oder 00, 01, 02. Neuer Index = aktualisiertes Dokument. | Segment im Dateinamen (`plan_index`) |
| **_Eingang** | Globaler Import-Eingang des Projekts unter `01 Planunterlagen/_Eingang` (ein Eingang in V1, ADR-061 P.7). Neue Dateien werden hier abgelegt (per E-Mail, Download, USB). Die Erfassung sortiert von hier in die Zielordner der Dokumenttypen, auch in andere Ablagebereiche (z. B. „06 Protokolle“). | `FolderTemplateEntry.HasInbox` (nur am Hauptordner), `ProjectPaths.Inbox` |
| **_Archiv** | Ordner für alte Dokumentversionen. Wenn ein Dokument einen neuen Index bekommt, wird der alte Index hierhin verschoben. „Archiv“ bezeichnet nur abgelöste Versionen. (Audit Review 01, R01-001 E16, entschieden 2026-09-24) | |
| **Bestand** | Die aktuell erfassten Dokumente eines Projekts (current-Revisionen). Vorgesehener Name des bisherigen UI-Sub-Tabs „Archiv (N)“ in „Manuell sortieren“, der diese Dokumente zeigt. Abzugrenzen von `_Archiv` (abgelöste Versionen) und „Plan-Archiv“ (Eigenname des Persistenzmodells aus ADR-058, kein UI-Begriff). (Audit Review 01, R01-001 E16, entschieden 2026-09-24) | `ManualCaptureViewModel.ArchiveRows` (Ist-Name) |
| **Segment** | Teil eines Dateinamens, gesplittet an den Trennzeichen des Profils (Default `-` und `_`; `.` nur profilgebunden) (Audit Review 01, R01-001 E11, entschieden 2026-09-24). Z.B. `AR-H64-EG-GR-01.pdf` → 5 Segmente. | ADR-022 |
| **Segmenttyp** | Katalogeintrag für die fachliche Bedeutung eines Dateinamen-Segments (z. B. Plannummer, Index, Geschoß, Haus) in `bpm.db`. Stabile `id` (Built-in: snake_case wie `plan_number`, `geschoss`; Custom: ULID), unveränderlicher `token_key` für Templates, Fachrolle `SemanticRole` (bei Built-ins read-only, bei Custom-Typen leer). Built-ins umbenennbar und deaktivierbar, Löschen nur als Soft-Delete. Im Profil referenziert über `fieldTypeId`; Vorgänger war der fest verdrahtete `FieldType`-Enum (ADR-056). | `segment_types`, `segment_type_groups`, `SegmentTypeDefinition`, `SegmentTypeIds`, ADR-056 |
| **Standard** (Badge) | Kennzeichnung mitgelieferter Built-in-Einträge (`is_builtin`) — soll einheitlich „Standard“ heißen (Segmenttyp-Manager und Dokumenttyp-Liste); Ist: „BUILT-IN“ (`SegmentTypeManagerDialog`) bzw. „Vorlage“ (Tab „Profile“, `ProjectDetailView`). (Audit Review 01, R01-001 E16, entschieden 2026-09-24) | `is_builtin` (`segment_types`, `document_types`) |
| **SegmentPosition** | 0-basierte Position eines Segments im tokenisierten Dateinamen. Persistiert in `RecognitionRule.SegmentPosition: int?` bei `method=segment`. Beim Wizard zeigt die UI Pos 1, intern wird 0-basiert gespeichert (kein Off-by-one). | BPM-082, ADR-010 (erweitert), `RecognitionRule` |
| **RecognitionContext** | Tokenisierungs-Snapshot einer Datei fuer ein Profil. Privater Record im Recognizer mit `FileName`, `FileStem`, `Tokens` (IReadOnlyList<string>). Lokaler Cache pro `Recognize(...)`-Aufruf mit Schluessel `(fileName, profile.Id)`. | BPM-082.02, `DocumentTypeRecognizer` |
| **FileStem** | Dateiname **ohne Extension**. Output von `Path.GetFileNameWithoutExtension(fileName)`. Im `RecognitionContext` als Anker fuer Tokens. Beispiel: `PROJ-PROT-2025-01.pdf` → FileStem = `PROJ-PROT-2025-01`. | BPM-082.02, `FileNameParser.Parse(...).BaseName` |
| **Tokens** | Liste der Segment-Werte nach `FileNameParser.Parse`, in Position-Reihenfolge materialisiert. Recognizer und Wizard nutzen dieselbe Tokenisierung — verhindert Drift zwischen Lern- und Laufzeitpfad. | BPM-082.02, `RecognitionContext.Tokens` |
| **Import-Workflow** | 6 Phasen (0–5): Profil anlernen (optional) → Dateien im Eingang → Import-Analyse (Scan, MD5-Fingerprint, Plannummer-/Index-Kandidaten, Bucket-Matching) → Zuordnung (Radial/Panel, Pending Assignments) → Ausführung (Journal vor Dateioperationen) → Sicherheitsnetz (Recovery, Undo). Seit ADR-064 P.9 genau ein Importweg; die frühere 7-Stufen-Pipeline und die Import-Vorschau sind mit BPM-083 entfallen. | ADR-008 (Ursprung), ADR-059, ADR-064, PlanManager.md Kap. 6 |
| **Erfassung** | Vorgang, in dem eingehende Dateien ihre fachliche Identität bekommen (Tab „Manuell sortieren“, ADR-059). Die Analyse verteilt die Eingangsdateien auf Buckets: A Dublette (MD5 im Bestand, überspringen), B Update-Vorschlag (bekannter Plan, anderer Index), C Erstaufnahme (öffnet das Radial), D Konflikt (Auflösung im Panel/Auswahldialog). **Erstaufnahme** = Bucket C: der Mensch vergibt die Identität einmal pro Plan. | `ManualFirstCaptureService`, `CaptureBucket`, `CaptureConfirmService`, ADR-059 P.1/P.5 |
| **Radial** | Kreisförmiges Kaskadenmenü der Erfassung: Ring 1 Dokumenttyp, Ring 2 Bauteil oder Kategorie (je Unterteilung des Dokumenttyps), Ring 3 Geschoß (nur bei Unterteilung Bauteil/Geschoß). Schreibt nur Pending Assignments, keinen Import. Daneben das rechte Detail-Panel als Kontroll-, Bulk- und Fallback-Fläche. | `ManualCaptureView`, ADR-059 P.5/P.6 + Addendum |
| **Pending** (Pending Assignment) | Vorgemerkte, noch nicht bestätigte Zuordnung einer Eingangsdatei aus Radial/Panel (Dokumenttyp, Bauteil bzw. Kategorie, Geschoß, Plannummer, Index, Zielordner). Lebt nur in-memory pro App-Session — kein Move und keine DB bis zur Bestätigung; Verwerfen = Undo vor dem Import. Erst die Bestätigung macht daraus Import-Entscheidungen. | `PendingAssignment`, `PendingAssignmentStore`, `CaptureConfirmService`, ADR-059 P.5 |
| **PlanDocument** | Logisches Dokument über alle Revisionen hinweg (Schema v2.0 / BPM-109). Stabile Entität — Cross-Modul-Verweise (Bautagebuch, Foto, Vorlagen) zeigen auf `PlanDocument`, nicht auf den `document_key`-String. Trägt `building_part_id` / `building_level_id` als Cross-DB-Soft-Reference auf `bpm.db` (TEXT ohne FOREIGN KEY, ADR-058-Addendum). | `plan_documents`, ADR-058 |
| **PlanRevision** | Versionsstand eines `PlanDocument` mit `plan_index` und Zeitstempeln `current_from` / `superseded_at`. Status `current` / `superseded` / `rejected`. UNIQUE-Index garantiert max. eine `current`-Revision pro Document. | `plan_revisions` (Schema v2.0), ADR-058 |
| **PlanFile** | Physische Datei (PDF, DWG, …) einer Plan-Revision — dritte Ebene des Drei-Ebenen-Modells (`PlanDocument` → `PlanRevision` → `PlanFile`). Mit Dateiname, relativem Pfad, Dateityp, MD5 und Größe; Zuordnung zu Revisionen n:m (z. B. Sammel-DWG). | `plan_files`, `revision_file_links`, ADR-058 |
| **Plandaten** | Tab im PlanManager-Projektdetail: read-only DB-Sicht auf den kuratierten Planindex — alle Dokumente mit ihrer current-Revision, Detail-Panel mit Dateien und Segmenten. Zeigt die Daten aus `planmanager.db`, nicht das Dateisystem (dafür Tab „Explorer“). | `PlanDataView`, `PlanDataRow`, BPM-126/127 |
| **superseded** | Status einer abgelösten Revision (eine neuere wurde `current`). Klarer als „archived" (das beschreibt Storage, nicht den fachlichen Revisionszustand). | `revision_status`, ADR-058 |
| **rejected** | Bewusst verworfene Revision (z.B. bewusst verworfener Vorabzug). Kein `current`, `superseded_at` als Verwerfungs-Zeitpunkt. | `revision_status`, ADR-058 |
| **PlanDocumentSegment** | Extrahierter Segmentwert pro `PlanDocument` (z.B. `haus = H1`, `geschoss = EG`). KV-Tabelle; `segment_type_id` ist Cross-DB-Soft-Reference auf `segment_types` (bpm.db, ADR-056) ohne FOREIGN KEY (ADR-058-Addendum). Erlaubt indexierte Modul-Filter ohne `LIKE`-Strings auf `document_key`. | `plan_document_segments`, ADR-058 |
| **PlanRevisionEvent** | Minimaler Audit-Trail für Statuswechsel einer Revision (`created`, `made_current`, `superseded`, `file_linked`, `manual_override`). KEIN Before/After-Snapshot pro Spalte — bewusst schlank, nicht Enterprise-History. | `plan_revision_events`, ADR-058 |
| **PlanContextLink** | Cross-Modul-Verknüpfung: ein Bautagesbericht / Foto / Vorlage zeigt auf eine konkrete `PlanRevision`. Pflicht `resolution_mode = 'fixed_revision'` — `target_revision_id` wird beim Erstellen festgezogen. | `plan_context_links`, ADR-058 |
| **fixed_revision** | Snapshot-Strategie für `PlanContextLink`: Die zum Zeitpunkt der Linkerstellung aktuelle Revision wird in `target_revision_id` persistent festgehalten. Alte Berichte zeigen immer dieselbe Revision — auch nach späterer Korrektur eines Importdatums. Fachliche Invariante aus ADR-058. | `plan_context_links.resolution_mode`, ADR-058 |
| **BuildingPartAlias** | ⚠ Geplant, noch nicht implementiert (BPM-121 Stufe C1, post-V1). Alternative Schreibweise für ein `BuildingPart` (z.B. `Haus 1`, `H1`, `Haus 01`) — relational gespeichert in `bpm.db` (ADR-058-Addendum), nicht JSON. Soll durch User-Bestätigung gefüllt werden (exakte Normalisierung, kein Fuzzy-Match). | `building_part_aliases` (geplant), ADR-058 + Addendum, ADR-065 P.14 |
| **IPlanLookupService** | Öffentliche API von PlanManager für konsumierende Module (Bautagebuch, Foto, Vorlagen). Methoden: `FindCurrentPlansAsync` (Zeitreise-Query) und `CreatePlanContextSnapshotAsync` (schreibt `PlanContextLink` mit `fixed_revision`). Interface-Stub V1, Implementation post-V1. | BPM-109.05a/.05, ADR-058 |

---

## 4. Projektarten und -typen

| Begriff | Erklärung | Im Code |
|---------|-----------|---------|
| **Neubau** | Komplett neues Gebäude auf einer leeren Parzelle. | `ProjectType` |
| **Sanierung** | Instandsetzung eines bestehenden Gebäudes (Fassade, Dach, Elektrik etc.). | `ProjectType` |
| **Umbau** | Funktionale Änderung eines bestehenden Gebäudes (z.B. Büro → Wohnung). | `ProjectType` |
| **Zubau** | Erweiterung eines bestehenden Gebäudes (Anbau, Aufstockung). | `ProjectType` |

---

## 5. Projekt-Beteiligte (Rollen)

| Rolle | Erklärung | Typisch |
|-------|-----------|---------|
| **Bauherr** | Eigene Beteiligtenrolle, nicht gleichbedeutend mit dem Auftraggeber (`Client`) — siehe Kap. 1. |
| **Architekt** | Entwurf und Einreichplanung. Liefert Grundrisse, Schnitte, Ansichten. |
| **Statiker** | Tragwerksplanung. Liefert Schal- und Bewehrungspläne. |
| **Haustechnik** | Heizung, Klima, Lüftung, Sanitär (HKLS). |
| **HKLS** | Heizung, Klima, Lüftung, Sanitär — Abkürzung für Haustechnik-Gewerke. |
| **Bauphysik** | Wärmeschutz, Schallschutz, Feuchteschutz. |
| **Vermessung** | Absteckung und Aufmaß auf der Baustelle. |
| **Brandschutz** | Brandschutzkonzept und -planung. |
| **Geotechnik** | Bodengutachten, Gründungsberatung. |
| **Elektro** | Elektroinstallation und -planung. |

Im Code: `ParticipantRoles` in `shared-config.json` (`SharedConfig`), editierbar über ✎-Button.

---

## 6. Bauherren-Portale

| Portal | Erklärung |
|--------|-----------|
| **InfoRaum** | Dokumentenmanagement-Portal. Bauherren laden hier Pläne und Dokumente hoch. |
| **PlanRadar** | Cloud-basierte Bau-Software für Mängelmanagement und Dokumentation. |
| **PlanFred** | Plan-Verteilungs-Portal. Pläne werden hier versioniert und freigegeben. |
| **Bau-Master** | Österreichische Bau-Software für Bautagebuch und Mängelmanagement. |
| **Dalux** | Dänische Bau-Software für BIM, Mängel und Dokumentation. |

Im Code: `PortalTypes` in `shared-config.json` (`SharedConfig`), editierbar. Tab 4 im ProjectEditDialog.

---

## 7. Kalkulation und Nachkalkulation

| Begriff | Erklärung |
|---------|-----------|
| **LV** | Leistungsverzeichnis. Liste aller zu erbringenden Leistungen mit Positionen, Mengen, Einheiten und Preisen. Basis für Angebot und Abrechnung. |
| **LV-Position** | Eine einzelne Leistung im LV, z.B. "02.03.01 Mauerwerk 38er Planziegel, m²". |
| **LB-HB** | Leistungsbeschreibung Hochbau. Standardisiertes Positionsverzeichnis für österreichische Ausschreibungen. |
| **ÖNORM A 2063** | Österreichische Norm für den elektronischen Datenaustausch im Bauwesen (Ausschreibung, Vergabe, Abrechnung). XML-Format. |
| **GAEB** | Gemeinsamer Ausschuss Elektronik im Bauwesen. Deutsches Pendant zur ÖNORM A 2063. |
| **AVA** | Ausschreibung, Vergabe, Abrechnung. Der gesamte kaufmännische Prozess im Bauwesen. |
| **Nachkalkulation** | Vergleich von geplanten vs. tatsächlichen Kosten/Stunden nach Fertigstellung. Ergibt Erfahrungswerte für zukünftige Projekte. |
| **Arbeitspaket** | In BPM: Bauteil + Geschoß + Tätigkeit + Soll-Menge. Verbindet Bautagebuch, Zeiterfassung und LV. Z.B. "Mauerwerk 38er, H5/EG, 198 m²". |
| **Leistungskatalog** | Sammlung von Erfahrungswerten: h/m², h/m³, h/to für verschiedene Tätigkeiten. Wächst mit jedem Projekt. |
| **m²/Ah** | Quadratmeter pro Arbeitsstunde. Leistungskennzahl für flächige Arbeiten (Mauerwerk, Schalung, Dämmung). |
| **h/EH** | Stunden pro Einheit. Allgemeine Leistungskennzahl (EH = Einheit, z.B. m², m³, to, Stk). |
| **Wandcode** | Eindeutige Kennung einer Wand: "5-0-0-1" = Haus 5, Geschoß 0, Top 0, Wand 1. Aus der Ziegelberechnung. |
| **K7-Blatt** | Kalkulationsblatt nach ÖNORM B 2061. Aufgliederung der Einheitspreise in Material, Lohn, Geräte, Gemeinkosten. BPM ersetzt das NICHT. |
| **ABK / AUER** | Professionelle AVA-Software in Österreich (ABK = Auftrags- und Baukostenmanagement, AUER Success). BPM ersetzt diese NICHT. |

---

## 8. Zeiterfassung

| Begriff | Erklärung | Im Code / Docs |
|---------|-----------|----------------|
| **tbl_Zeiten** | Haupttabelle in Excel für Stundeneinträge. Append-only (nur neue Zeilen, keine Änderungen). | ModuleZeiterfassung.md |
| **Stundenart** | Typ der Arbeitsstunde: Normalstunden, Regiestunden, Urlaub, Krankenstand, Feiertag, Zeitausgleich, Schlechtwetter. | `tbl_Stundenarten` |
| **Arbeitszeitmodell** | Definiert Soll-Stunden pro Wochentag (z.B. Vollzeit 39h: Mo–Do 8h, Fr 7h). Historisiert — ein Mitarbeiter kann das Modell wechseln. | `tbl_Arbeitszeitmodell` |
| **Arbeitseinteilung** | Matrix: Arbeiter × Tätigkeiten. Per "x" wird ein Arbeiter einer Tätigkeit zugewiesen. Grundlage für die Stundenverteilung auf Arbeitspakete. |
| **Schlechtwetter** | Bau-KV Regelung: bei Schlechtwetter keine Überstundenberechnung. In BPM als Stundenart erfasst. |
| **Regiestunden** | Stunden die nicht im LV enthalten sind — Zusatzleistungen, die separat verrechnet werden. |

---

## 9. Ordnerstruktur (Projektordner)

| Ordner | Inhalt | Im Code |
|--------|--------|---------|
| **00 Sonstiges** | Alles was nicht in andere Ordner passt. | `FolderTemplateEntry` |
| **01 Planunterlagen** | Alle Pläne, sortiert nach Dokumenttyp. Hat den globalen `_Eingang`. | `HasInbox = true` |
| **02 Fotos** | Baustellenfotos, chronologisch oder nach Gewerk. | |
| **03 Leica** | Vermessungsdaten: Absteckpläne, Aufmaß. | |
| **04 DOKA** | Schalungspläne und -unterlagen (DOKA = Schalungshersteller). | |
| **05 LV** | Leistungsverzeichnis, Angebote, Nachträge. | |
| **06 Protokolle** | Baubesprechungsprotokolle, Abnahmeprotokolle. | |
| **.bpm/** | Versteckter Ordner als Projekt-Identität im Projektordner. Enthält manifest.json (schlank), project.json (Vollexport), profiles/ (Erkennungsprofile je Dokumenttyp). Ermöglicht Wiedererkennung nach Ordner-Umbenennung. | ADR-046 |
| **_Eingang** | Globaler Import-Eingang unter `01 Planunterlagen` (einer pro Projekt, ADR-061 P.7). Neue Dateien landen hier, die Erfassung sortiert sie in die Zielstruktur. | `ProjectPaths.Inbox` |
| **_Archiv** | Alte Plan-Indizes werden hierhin verschoben wenn ein neuer Index importiert wird. | |

---

## 10. BPM-Dateien und Speicherorte

| Datei | Format | Speicherort | Zweck |
|-------|--------|-------------|-------|
| **bpm.db** | SQLite | `%LocalAppData%\BauProjektManager\` | Haupt-Datenbank (Projekte, Stammdaten). Syncht NICHT. |
| **planmanager.db** | SQLite | Lokal pro Projekt (`%LocalAppData%\BauProjektManager\Projects\<ProjektID>\`) | Plan-Bestand (Dokumente, Revisionen, Dateien, Segmentwerte) + Import-Journal und Undo. Autorierte, zu sichernde Projektdaten — kein rebuildbarer Cache (ADR-066). Syncht NICHT. |
| **registry.json** | JSON | Cloud-Speicher `.AppData/` | Auto-generierter Export für VBA-Makros. Read-only für VBA. |
| **device-settings.json** | JSON | `%LocalAppData%\BauProjektManager\` | Geräte-Einstellungen (Geräte-ID, Pfade, Fenster-/Panel-Größen). Syncht NICHT. |
| **shared-config.json** | JSON | Cloud-Speicher `.AppData/BauProjektManager/` | Geteilte Einstellungen (Ordner-Template, Listen wie Geschoß-Namen, Beteiligtenrollen, Portaltypen). Syncht zwischen Geräten. Eine alte `settings.json` wird nicht mehr gelesen, nur entsorgt (BPM-069). |
| **.bpm/profiles/*.json** | JSON | Cloud-Speicher `.bpm/profiles/` im Projektordner | RecognitionProfiles — höchstens eine Datei pro Dokumenttyp (ADR-046, ADR-065 P.7). |
| **pattern-templates.json** | JSON | Cloud-Speicher `.AppData/` | Globale Muster-Bibliothek für Dokumenttyp-Erkennung. |
| **.bpm/manifest.json** | JSON | Cloud-Speicher Projektordner | Projekt-Identität (schlank: ID, Name, Module-Flags). ADR-046. |
| **.bpm/project.json** | JSON | Cloud-Speicher Projektordner | Vollständiger Projektexport für Import/Übergabe. ADR-046. |

---

## 11. Technische Begriffe (Software)

| Begriff | Erklärung |
|---------|-----------|
| **MVVM** | Model-View-ViewModel. Architektur-Pattern für WPF. Trennt Daten (Model), Oberfläche (View) und Logik (ViewModel). |
| **WPF** | Windows Presentation Foundation. Microsoft-Framework für Desktop-GUIs mit XAML. |
| **XAML** | Extensible Application Markup Language. XML-basierte Sprache für WPF-Oberflächen. |
| **CommunityToolkit.Mvvm** | NuGet-Paket das MVVM-Boilerplate reduziert. `[ObservableProperty]`, `[RelayCommand]` Attribute. |
| **Serilog** | Structured-Logging-Framework. Loggt in Dateien + Konsole. |
| **ClosedXML** | NuGet-Paket zum Lesen/Schreiben von Excel-Dateien (.xlsx) ohne installiertes Excel. |
| **SQLite** | Eingebettete SQL-Datenbank. Eine Datei = eine Datenbank. Kein Server nötig. |
| **DI** | Dependency Injection. Services werden über den Container bereitgestellt, nicht manuell instanziiert. |
| **NuGet** | Paket-Manager für .NET. Libraries werden als NuGet-Pakete eingebunden. |
| **LTS** | Long Term Support. .NET 10 LTS hat Support bis November 2028. |
| **Self-contained** | .exe die die .NET Runtime mitbringt. Größer aber läuft ohne installiertes .NET. |
| **Framework-dependent** | .exe die ein installiertes .NET voraussetzt. Kleiner aber .NET muss am Rechner sein. |
| **PWA** | Progressive Web App. Browser-App die offline funktioniert (Service Worker + IndexedDB). |
| **CRDT** | Conflict-free Replicated Data Type. Automatische Konfliktauflösung (wie in Notion). BPM verwendet stattdessen Write-Lock. |
| **ULID** | Universally Unique Lexicographically Sortable Identifier. 26-stellige TEXT-ID, global eindeutig, offline erzeugbar, chronologisch sortierbar. Primärschlüssel für ALLE Tabellen (ADR-039 v2), implementiert über `UlidIdGenerator` (`IIdGenerator`). Geräteübergreifend stabil (Ziel, ADR-066): Stammdaten-ULIDs sollen über `.bpm/project.json` exportiert und beim Import übernommen werden; derzeit vergibt der Import neue IDs. (Audit Review 01, R01-001 E4, entschieden 2026-09-24) NuGet: Cysharp/Ulid. |
| **Heartbeat** | Regelmäßiges Signal (alle 60 Sekunden) das zeigt dass ein Client noch aktiv ist. Für Write-Lock-Mechanismus. |

---

## 12. Cloud-Speicher

| Begriff | Erklärung |
|---------|-----------|
| **Cloud-Speicher** | Sammelbegriff für Dienste die sich als Ordner im Windows Explorer einblenden: OneDrive, Google Drive, Dropbox, Synology Drive, Nextcloud etc. BPM ist NICHT an einen bestimmten Dienst gebunden. |
| **OneDrive** | Microsofts Cloud-Speicher. Herberts aktueller Sync-Dienst. BPM funktioniert aber mit jedem Cloud-Speicher. |
| **BasePath** | Stammverzeichnis für alle Projektordner. Z.B. `D:\OneDrive\Dokumente\02 Arbeit\Projekte\`. | 
| **.AppData/** | Versteckter Ordner im BasePath für BPM-Konfigurationsdateien. Vorgesehen ist nur das Hidden-Attribut, für Kollegen unsichtbar; das System-Attribut entfällt (ADR-004-Addendum) (Audit Review 01, R01-001 E4, entschieden 2026-09-24). |

---

## 13. Abkürzungen (Kurzreferenz)

| Kürzel | Bedeutung |
|--------|-----------|
| BPM | BauProjektManager |
| EFH | Einfamilienhaus |
| MFH | Mehrfamilienhaus |
| LV | Leistungsverzeichnis |
| KG | Katastralgemeinde |
| GST | Grundstücksnummer |
| ÖBA | Örtliche Bauaufsicht |
| HKLS | Heizung, Klima, Lüftung, Sanitär |
| RDOK | Rohdecken-Oberkante |
| BPOK | Bodenplatten-Oberkante |
| FBOK | Fertigfußboden-Oberkante |
| RDUK | Rohdecken-Unterkante |
| DG | Dachgeschoß |
| EG | Erdgeschoß |
| OG | Obergeschoß |
| UG | Untergeschoß |
| FU | Fundament |
| AVA | Ausschreibung, Vergabe, Abrechnung |
| ADR | Architecture Decision Record |
| DI | Dependency Injection |
| FK | Foreign Key (Fremdschlüssel) |
| DSGVO | Datenschutzgrundverordnung |
| WKO | Wirtschaftskammer Österreich |

---

## 14. Datenschutz (DSGVO)

| Begriff | Erklärung |
|---------|-----------|
| **DSGVO** | Datenschutz-Grundverordnung. EU-Verordnung für den Schutz personenbezogener Daten. Gilt in Österreich seit 25.05.2018. |
| **DSG** | Österreichisches Datenschutzgesetz. Nationale Ergänzung zur DSGVO. |
| **ArbVG** | Arbeitsverfassungsgesetz. § 96 verlangt Betriebsvereinbarung für automatisierte Personalverarbeitung (z.B. Zeiterfassung). |
| **AVV / DPA** | Auftragsverarbeitungs-Vereinbarung / Data Processing Agreement. Vertrag zwischen BPM-Nutzer und externem API-Anbieter (z.B. OpenAI). Pflicht vor KI-Modul. |
| **DSFA** | Datenschutz-Folgenabschätzung (Art. 35 DSGVO). Pflicht bei hohem Risiko — z.B. KI-Modul mit Klasse-C-Daten. |
| **Datenklasse A** | Kein Personenbezug. Koordinaten, Hashes, Wetterdaten, Ordnerstruktur. Kann frei verarbeitet werden. |
| **Datenklasse B** | Personenbezogene Daten eigener Mitarbeiter/Geschäftskontakte. Clients, Participants, Employees. Bei externem API-Transfer: Warnung + Opt-in. |
| **Datenklasse C** | Sensible Drittdaten in Dokumenten: LVs, Baubescheide, Baubeschreibungen. Default: blockiert. Nur mit Anonymisierung oder expliziter User-Freigabe. |
| **Sync-Kategorie** | Einordnung einer Tabelle/Datei für den Sync (ADR-047, ADR-053): `local-only`, `shared-domain`, `shared-reference`, `restricted`. Ersetzt die früheren Buchstaben-Klassen A/B/C/D aus ADR-047; „Klasse A/B/C“ meint ausschließlich die DSGVO-Datenklassen. (Audit Review 01, R01-001 E16, entschieden 2026-09-24) |
| **IExternalCommunicationService** | Zentrales Privacy Gate (ADR-035). Einziger erlaubter Weg für HTTP-Calls an externe APIs. Entscheidet, loggt, blockiert. |
| **IPrivacyPolicy** | Austauschbare Policy-Komponente (ADR-036). `RelaxedPrivacyPolicy` für intern, `StrictPrivacyPolicy` für Verkaufsversion. Steuerung über Lizenz. |
| **Privacy by Design** | Art. 25 DSGVO: Datenschutz von Anfang an in die Architektur einbauen, nicht nachträglich. |
| **DPAPI** | Windows Data Protection API. Verschlüsselt API-Keys lokal, gebunden an den Windows-User. |
| **SQLCipher** | Verschlüsseltes SQLite. Empfohlen ab Verkauf oder Mobile (Geräteverlust-Risiko). |

---

*Dieses Glossar wird erweitert wenn neue Module oder Fachbegriffe hinzukommen.*