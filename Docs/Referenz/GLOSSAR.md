---
doc_id: glossar
doc_type: reference
authority: secondary
status: active
owner: herbert
topics: [glossar, fachbegriffe, bauwesen, österreich, abkürzungen, bpm-codebasis]
read_when: [fachbegriff-unklar, österreichische-bau-terminologie, codebasis-begriffe]
related_docs: [architektur, planmanager]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Fachbegriffe aus dem österreichischen Bauwesen und der BPM-Codebasis
- Autorität: secondary
- Lesen wenn: Fachbegriff unklar, österreichische Bau-Terminologie, Codebasis-Begriffe nachschlagen
- Nicht zuständig für: Code-Konventionen (→ CODING_STANDARDS.md), Architektur (→ Architektur.md)
- Pflichtlesen: keine (Nachschlagewerk)
- Fachliche Invarianten:
  - Österreichische Terminologie verwenden (Polier, Geschoß, RDOK)
  - Begriffe alphabetisch innerhalb jeder Kategorie

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
| **Bauherr** | Auftraggeber des Bauprojekts (Person oder Firma). In BPM als `Client` modelliert. |
| **Kolonne** | Arbeitsgruppe auf der Baustelle, typisch 3–6 Facharbeiter + eventuell Lehrlinge. |
| **Dispo** | Disposition. Koordiniert Materialtransporte, LKW-Einsatz, Maschinenverfügbarkeit zwischen Büro und Baustelle. |
| **Lager** | Firmeneigenes Materiallager. Prüft Verfügbarkeit, kommissioniert, organisiert interne Lieferungen. |
| **Einkauf** | Beschafft Material extern bei Lieferanten/Baustoffhändlern. |

---

## 2. Bautechnische Begriffe

### 2.1 Geschoss-Höhen (Tab 2 Bauwerk)

| Kürzel | Langform | Erklärung | Im Code |
|--------|----------|-----------|---------|
| **RDOK** | Rohdecken-Oberkante | Oberkante der Betondecke (roh, ohne Estrich/Belag). Beim untersten Geschoss = BPOK. Eingabewert in Meter von ± 0,00. | `BuildingLevel.Rdok` |
| **BPOK** | Bodenplatten-Oberkante | Oberkante der Bodenplatte (nur unterstes Geschoss). Wird im Code als RDOK geführt. | `BuildingLevel.Rdok` (bei unterstem Geschoss) |
| **FBOK** | Fertigfußboden-Oberkante | Oberkante des fertigen Bodens (mit Estrich + Belag). EG hat typisch FBOK = 0,00. Eingabewert. | `BuildingLevel.Fbok` |
| **RDUK** | Rohdecken-Unterkante | Unterkante der Betondecke (Sichtseite von unten). Eingabewert. Beim untersten Geschoss null. | `BuildingLevel.Rduk` |
| **± 0,00** | Bezugsniveau / Meterriss | Referenzhöhe auf der Baustelle, typisch Oberkante Fertigfußboden EG. Alle Höhen werden relativ dazu angegeben. Pro Bauteil kann das absolute Niveau unterschiedlich sein. | `BuildingPart.ZeroLevelAbsolute` |
| **Meterriss** | — | Markierung exakt 1,00 m über ± 0,00, an den Wänden angezeichnet. Bezugslinie für alle Höhenmessungen auf der Baustelle. |

### 2.2 Errechnete Werte (nicht in DB gespeichert)

| Wert | Formel | Erklärung | Im Code |
|------|--------|-----------|---------|
| **Geschosshöhe** | FBOK(oben) − FBOK(unten) | Lichte Höhe von Fertigfußboden zu Fertigfußboden des darüberliegenden Geschosses. | `BuildingLevel.StoryHeight` |
| **Rohbauhöhe** | RDOK(oben) − RDOK(unten) | Höhe von Rohdecke zu Rohdecke. Relevant für Schalungshöhe und Mauerwerk. | `BuildingLevel.RawHeight` |
| **Deckenstärke** | RDOK(n+1) − RDUK(n) | Dicke der Betondecke (Decke darüber minus UK aktuell). Seit v0.24.2 korrigiert — war vorher fälschlich RDOK − RDUK (gleiche Zeile). | `BuildingLevel.DeckThickness` |
| **Fußbodenaufbau** | FBOK − RDOK | Schichtdicke zwischen Rohdecke und Fertigfußboden (Estrich, Dämmung, Belag). | `BuildingLevel.FloorBuildup` |

### 2.3 Geschoss-Bezeichnungen

| Kürzel | Langname | Präfix (Sortierung) |
|--------|----------|---------------------|
| FU | Fundament | −03 (oder tiefer) |
| UG3 | 3. Untergeschoss | −03 |
| UG2 | 2. Untergeschoss | −02 |
| UG | Untergeschoss | −01 |
| **EG** | **Erdgeschoss** | **00** (Bezugsniveau) |
| OG1 | 1. Obergeschoss | 01 |
| OG2 | 2. Obergeschoss | 02 |
| DG | Dachgeschoss | 03 (oder höher) |

Im Code: `LevelNameEntry` mit `ShortName` + `LongName`, editierbar in `settings.json`.

### 2.4 Bauwerk-Struktur

| Begriff | Erklärung | Im Code |
|---------|-----------|---------|
| **Bauteil** (BuildingPart) | Ein eigenständiger Gebäudeabschnitt mit eigenen Geschossen und Höhen. Z.B. "Haus 64", "Tiefgarage", "Stiegenhaus". Ein Projekt kann mehrere Bauteile haben. | `BuildingPart` |
| **Bauabschnitt** | Synonym für Bauteil in manchen Kontexten. |
| **Bauwerkstyp** (BuildingType) | Art des Gebäudes: EFH (Einfamilienhaus), MFH (Mehrfamilienhaus), Wohnanlage, Gewerbe, Industrie. Pro Bauteil separat — ein Projekt kann Wohnen + Gewerbe mischen. | `BuildingPart.BuildingType` |
| **Thermofuß** | Wärmedämmendes Element am Fußpunkt einer Wand (Übergang Bodenplatte/Decke zur Außenwand). Relevant für Höhenberechnung bei Ziegelmauerwerk. |
| **Scharrenrechner** | Berechnet die optimale Kombination von Normal-Scharren (24,9 cm) und Höhenausgleich-Scharren (21,9 cm) für eine gegebene Raumhöhe. Ziel: Wandhöhe exakt treffen mit möglichst wenig Schneiden. |
| **Scharre** | Eine Ziegellage (eine Reihe Ziegel in der Höhe). Normal-Scharre = 24,9 cm, Höhenausgleich-Scharre = 21,9 cm (bei Wienerberger Planziegel). |

### 2.5 Grundstück und Vermessung

| Begriff | Erklärung | Im Code |
|---------|-----------|---------|
| **KG** | Katastralgemeinde. Verwaltungseinheit im österreichischen Grundbuch. Jede KG hat eine Nummer und einen Namen. | `ProjectLocation.CadastralKg` / `.CadastralKgName` |
| **GST** | Grundstücksnummer innerhalb einer KG. Eindeutige Parzelle im Kataster. | `ProjectLocation.CadastralGst` |
| **EPSG:31258** | Koordinatensystem "MGI / Austria GK M31". Standard für Vermessung in der Steiermark. Ostkoordinate + Nordkoordinate in Metern. | `ProjectLocation.CoordinateSystem` |
| **GIS Steiermark** | Öffentliches Geoinformationssystem des Landes Steiermark. ArcGIS REST API, kein API-Key nötig. Liefert KG, GST, Koordinaten zu einer Adresse. |
| **Leica** | Vermessungsgeräte (Totalstation, GPS). Projektordner "04 Leica" enthält Absteckpläne und Aufmaß-Daten. |

---

## 3. Planmanagement

| Begriff | Erklärung | Im Code / Docs |
|---------|-----------|----------------|
| **Plantyp** | Kategorie eines Plans: Einreichplan, Polierplan, Schalplan, Bewehrungsplan, Installationsplan etc. | `RecognitionProfile` |
| **Planindex** / **Revision** | Versionsstand eines Plans. Typisch: A, B, C oder 00, 01, 02. Neuer Index = aktualisierter Plan. | Segment im Dateinamen |
| **Plannummer** | Eindeutige Kennung eines Plans innerhalb eines Projekts. Kommt vom Planer (Architekt, Statiker). | Segment im Dateinamen |
| **_Eingang** | Inbox-Ordner pro Plantyp. Neue Pläne werden hier abgelegt (per E-Mail, Download, USB). BPM sortiert von hier in die Zielordner. | `FolderTemplateEntry.HasInbox` |
| **_Archiv** | Ordner für alte Planversionen. Wenn ein Plan einen neuen Index bekommt, wird der alte Index hierhin verschoben. |
| **RecognitionProfile** | Erlerntes Muster pro Projekt/Plantyp. Definiert welches Segment im Dateinamen was bedeutet (Nummer, Index, Geschoss etc.). Pro Projekt in `profiles.json`. | `profiles.json` |
| **PatternTemplate** | Vorschlag aus der globalen Musterbibliothek. Wenn ein neues Profil angelegt wird, schlägt BPM ähnliche bestehende Templates vor. | `pattern-templates.json` |
| **Segment** | Teil eines Dateinamens, gesplittet an Trennzeichen (-, _, .). Z.B. `AR-H64-EG-GR-01.pdf` → 5 Segmente. | ADR-022 |
| **Import-Workflow** | 10-Schritte-Prozess: Scan → Parse → Validate → Classify → Plan → Preview → Execute → Finalize → Recover → Undo. | ADR-008 |

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
| **Architekt** | Entwurf und Einreichplanung. Liefert Grundrisse, Schnitte, Ansichten. |
| **Statiker** | Tragwerksplanung. Liefert Schal- und Bewehrungspläne. |
| **Haustechnik** | Heizung, Klima, Lüftung, Sanitär (HKLS). |
| **HKLS** | Heizung, Klima, Lüftung, Sanitär — Abkürzung für Haustechnik-Gewerke. |
| **Bauphysik** | Wärmeschutz, Schallschutz, Feuchteschutz. |
| **Vermessung** | Absteckung und Aufmaß auf der Baustelle. |
| **Brandschutz** | Brandschutzkonzept und -planung. |
| **Geotechnik** | Bodengutachten, Gründungsberatung. |
| **Elektro** | Elektroinstallation und -planung. |

Im Code: `ParticipantRoles` in `settings.json`, editierbar über ✎-Button.

---

## 6. Bauherren-Portale

| Portal | Erklärung |
|--------|-----------|
| **InfoRaum** | Dokumentenmanagement-Portal. Bauherren laden hier Pläne und Dokumente hoch. |
| **PlanRadar** | Cloud-basierte Bau-Software für Mängelmanagement und Dokumentation. |
| **PlanFred** | Plan-Verteilungs-Portal. Pläne werden hier versioniert und freigegeben. |
| **Bau-Master** | Österreichische Bau-Software für Bautagebuch und Mängelmanagement. |
| **Dalux** | Dänische Bau-Software für BIM, Mängel und Dokumentation. |

Im Code: `PortalTypes` in `settings.json`, editierbar. Tab 4 im ProjectEditDialog.

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
| **Arbeitspaket** | In BPM: Bauteil + Geschoss + Tätigkeit + Soll-Menge. Verbindet Bautagebuch, Zeiterfassung und LV. Z.B. "Mauerwerk 38er, H5/EG, 198 m²". |
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
| **01 Planunterlagen** | Alle Pläne, sortiert nach Plantyp. Hat `_Eingang` Unterordner. | `HasInbox = true` |
| **02 Fotos** | Baustellenfotos, chronologisch oder nach Gewerk. | |
| **03 Leica** | Vermessungsdaten: Absteckpläne, Aufmaß. | |
| **04 DOKA** | Schalungspläne und -unterlagen (DOKA = Schalungshersteller). | |
| **05 LV** | Leistungsverzeichnis, Angebote, Nachträge. | |
| **06 Protokolle** | Baubesprechungsprotokolle, Abnahmeprotokolle. | |
| **.bpm/** | Versteckter Ordner als Projekt-Identität im Projektordner. Enthält manifest.json (schlank), project.json (Vollexport), profiles/ (Plantyp-Profile). Ermöglicht Wiedererkennung nach Ordner-Umbenennung. | ADR-046 |
| **_Eingang** | Inbox-Unterordner in Plantyp-Ordnern. Neue Pläne landen hier, BPM sortiert sie in die Zielstruktur. | |
| **_Archiv** | Alte Plan-Indizes werden hierhin verschoben wenn ein neuer Index importiert wird. | |

---

## 10. BPM-Dateien und Speicherorte

| Datei | Format | Speicherort | Zweck |
|-------|--------|-------------|-------|
| **bpm.db** | SQLite | `%LocalAppData%\BauProjektManager\` | Haupt-Datenbank (Projekte, Stammdaten). Syncht NICHT. |
| **planmanager.db** | SQLite | Lokal pro Projekt | Import-Journal, Undo, Cache. Syncht NICHT. |
| **registry.json** | JSON | Cloud-Speicher `.AppData/` | Auto-generierter Export für VBA-Makros. Read-only für VBA. |
| **settings.json** | JSON | Cloud-Speicher `.AppData/` | App-Einstellungen (Pfade, Ordner-Template, Listen). Syncht zwischen Geräten. |
| **profiles.json** | JSON | Cloud-Speicher `.bpm/profiles/` im Projektordner | RecognitionProfiles pro Projekt (ADR-046). |
| **pattern-templates.json** | JSON | Cloud-Speicher `.AppData/` | Globale Muster-Bibliothek für Plantyp-Erkennung. |
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
| **ULID** | Universally Unique Lexicographically Sortable Identifier. 26-stellige TEXT-ID, global eindeutig, offline erzeugbar, chronologisch sortierbar. In BPM der Primärschlüssel für ALLE Tabellen (ADR-039 v2). NuGet: Cysharp/Ulid. |
| **Heartbeat** | Regelmäßiges Signal (alle 60 Sekunden) das zeigt dass ein Client noch aktiv ist. Für Write-Lock-Mechanismus. |

---

## 12. Cloud-Speicher

| Begriff | Erklärung |
|---------|-----------|
| **Cloud-Speicher** | Sammelbegriff für Dienste die sich als Ordner im Windows Explorer einblenden: OneDrive, Google Drive, Dropbox, Synology Drive, Nextcloud etc. BPM ist NICHT an einen bestimmten Dienst gebunden. |
| **OneDrive** | Microsofts Cloud-Speicher. Herberts aktueller Sync-Dienst. BPM funktioniert aber mit jedem Cloud-Speicher. |
| **BasePath** | Stammverzeichnis für alle Projektordner. Z.B. `D:\OneDrive\Dokumente\02 Arbeit\Projekte\`. | 
| **.AppData/** | Versteckter Ordner im BasePath für BPM-Konfigurationsdateien. Hidden+System Attribut, für Kollegen unsichtbar. |

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
| DG | Dachgeschoss |
| EG | Erdgeschoss |
| OG | Obergeschoss |
| UG | Untergeschoss |
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
| **IExternalCommunicationService** | Zentrales Privacy Gate (ADR-035). Einziger erlaubter Weg für HTTP-Calls an externe APIs. Entscheidet, loggt, blockiert. |
| **IPrivacyPolicy** | Austauschbare Policy-Komponente (ADR-036). `RelaxedPrivacyPolicy` für intern, `StrictPrivacyPolicy` für Verkaufsversion. Steuerung über Lizenz. |
| **Privacy by Design** | Art. 25 DSGVO: Datenschutz von Anfang an in die Architektur einbauen, nicht nachträglich. |
| **DPAPI** | Windows Data Protection API. Verschlüsselt API-Keys lokal, gebunden an den Windows-User. |
| **SQLCipher** | Verschlüsseltes SQLite. Empfohlen ab Verkauf oder Mobile (Geräteverlust-Risiko). |

---

*Dieses Glossar wird erweitert wenn neue Module oder Fachbegriffe hinzukommen.*