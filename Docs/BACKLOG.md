# BauProjektManager — Backlog & Ideen

**Letzte Aktualisierung:** 27.03.2026  
**Format:** Priorität → Feature → Beschreibung → Status

---

## V1 — Phase 1: Einstellungen (fast fertig)

| # | Feature | Beschreibung | Status |
|---|---------|-------------|--------|
| 1 | App-Shell + Navigation | MainWindow, Sidebar, Statusleiste | ✅ Erledigt |
| 2 | Serilog Logging | File + Console, tägl. Rotation, 30 Tage | ✅ Erledigt |
| 3 | Domain-Modelle | Project, Client, Building, Location, Timeline, Paths | ✅ Erledigt |
| 4 | Projektliste + Dialog | DataGrid, Bearbeiten-Dialog mit allen Feldern | ✅ Erledigt |
| 5 | SQLite-Datenbank | bpm.db mit Projects, Clients, Buildings Tabellen | ✅ Erledigt |
| 6 | Auto-Increment IDs | proj_001, client_001, bldg_001 | ✅ Erledigt |
| 7 | registry.json Export | Flat JSON für VBA, atomisch geschrieben | ✅ Erledigt |
| 8 | Git-Aufräumung | Hilfsmodule + Export aus Tracking entfernt | ✅ Erledigt |
| 9 | Ersteinrichtung | OneDrive auto-erkennen, Arbeitsordner, Archivordner, settings.json | ✅ Erledigt (v0.10.0) |
| 10 | **Projektordner erstellen** | **Beim Anlegen automatisch nummerierte Ordnerstruktur erstellen (01_Pläne, 02_Fotos etc.). Ordner-Template änderbar in settings.json. _Eingang Unterordner in 01_Pläne.** | ⬜ Nächster Schritt |
| 11 | **.bpm-manifest** | Versteckte Datei in jedem Projektordner erstellen (Ausweis), bei Umbenennung → Auto-Suche + Pfad aktualisieren | ⬜ |
| 12 | **Projekt archivieren** | Status → Completed → Ordner in Archiv verschieben, Pfad aktualisieren | ⬜ |
| 13 | **Pfade änderbar** | Button in Einstellungen um Setup-Dialog erneut zu öffnen (OneDrive, Arbeitsordner, Archivordner) | ⬜ |
| 14 | **Single-Writer Mutex** | Nur eine App-Instanz gleichzeitig (Architektur Kap. 15) | ⬜ |
| 15 | Suchfeld Projekte | Schnellsuche/Filter in der Projektliste | ⬜ |
| 16 | Versionsnummer im Log | Automatisch aus Assembly, nicht hardcoded | ⬜ |
| 17 | Architektur v1.5 | Doku aktualisieren (.NET 10, Client, Adresse, Manifest, Ordnerstruktur) | ⬜ |

---

## V1 — Phase 2: PlanManager (der große Brocken)

| # | Feature | Beschreibung | Status |
|---|---------|-------------|--------|
| 18 | Dateinamen-Parser | Segmente splitten an Trennzeichen | ⬜ |
| 19 | Segment-Zuweiser GUI | 3-Schritt-Wizard (Typ wählen, Muster, Ordner) | ⬜ |
| 20 | Plantyp-Erkennung | prefix/contains/regex Muster matchen | ⬜ |
| 21 | PatternTemplates | Vorschlagslogik beim Profil-Anlegen | ⬜ |
| 22 | profiles.json | Pro Projekt auf OneDrive speichern | ⬜ |
| 23 | pattern-templates.json | Globale Musterbibliothek auf OneDrive | ⬜ |
| 24 | Import-Workflow Schritt 1-5 | Scan, Parse, Validate, Classify, Plan | ⬜ |
| 25 | Import-Vorschau (Schritt 6) | GUI mit Status, Rechtsklick-Korrektur | ⬜ |
| 26 | Import-Execute (Schritt 7-8) | Dateien verschieben, Journal, Finalize | ⬜ |
| 27 | Index-Archivierung | Alte Indizes → _Archiv/ verschieben | ⬜ |
| 28 | Undo-Journal (SQLite) | 3 Tabellen: journal, actions, action_files | ⬜ |
| 29 | Recovery (Schritt 9) | Beim App-Start: pending → Reparatur anbieten | ⬜ |
| 30 | Undo (Schritt 10) | Journal rückwärts, Dateien zurück | ⬜ |
| 31 | Backup vor Import | SQLite + JSON als .bak kopieren | ⬜ |
| 32 | Unbekannte Dateien Dialog | Profil erweitern / Neues Profil / Skip | ⬜ |
| 33 | Erkennungs-Konflikt Dialog | Mehrere Profile passen → User wählt | ⬜ |

---

## V1.1 — Bald danach

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Adressbuch | Eigene Kontakt-DB in SQLite, projektübergreifend | ⬜ |
| Bauherren-/Firmenliste | Dropdown "Auftraggeber wählen" statt jedes Mal tippen | ⬜ |
| Button "Aus Adressbuch" | Im Projekt-Dialog neben Auftraggeber | ⬜ |
| Planlisten-Import | Excel (.xlsx) + Soll/Ist-Abgleich | ⬜ |
| Planlisten-Export | Excel + PDF (ClosedXML + QuestPDF) | ⬜ |
| Schnellsuche Pläne | Plan finden über alle Projekte | ⬜ |
| CSV-Import | Für Planlisten | ⬜ |

---

## Später — Nach V1

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Outlook-Adressbuch | Kontakte aus Outlook importieren/übernehmen | ⬜ |
| Google Maps API | Adresse → PLZ, Ort, Gemeinde, Bezirk, Koordinaten automatisch | ⬜ |
| Adressbuch-Sync | Eigenes Adressbuch ↔ Outlook abgleichen | ⬜ |
| Outlook COM Integration | Projekt-Ordner in Outlook, Anhänge extrahieren | ⬜ |
| Dashboard | Startseite mit Widgets (Wetter, neue Pläne, Status) | ⬜ |
| Bautagebuch | Tägliches Protokoll mit Auto-Befüllung + Export | ⬜ |
| Wetter-API | Wetterdaten pro Baustelle | ⬜ |
| Foto-Modul | OneDrive-Baustellenfotos nach Projekt/Datum | ⬜ |
| Excel/Word Vorlagen | COM Interop, Projektdaten in Vorlagen befüllen | ⬜ |
| PDF-Vorschau | Pläne in der App anzeigen | ⬜ |
| VERALTET-Stempel | Auf alte Plan-PDFs stempeln | ⬜ |
| Auto-Update | App-Update-Mechanismus | ⬜ |

---

## Modul: Arbeitszeiterfassung (Nach V1)

**Konzept:** WPF-Eingabemaske schreibt direkt in Excel-Datei. Kein eigenes DB-Modul — Excel bleibt die Wahrheitsquelle für Zeitdaten.

**Architektur:**
- WPF = schöne Eingabemaske (Dropdowns, Kalender, Dark Theme)
- Baustellen-Dropdown aus bpm.db / registry.json
- Daten werden per ClosedXML direkt in Excel geschrieben
- Excel behält alle Formeln, Power Query, Pivot, Auswertungen
- Lohnbüro liest Excel auf OneDrive

**Excel-Architektur (Master-Prompt vorhanden):**
- Haupttabelle: tbl_Zeiten (append-only, Single Point of Truth)
- Stammdaten: tbl_Mitarbeiter, tbl_Stundenarten, tbl_Statuscodes
- Arbeitszeitmodelle historisiert
- Überstundenregeln historisiert
- Überstunden werden berechnet, nie gespeichert
- Power Query für Joins, Historienlogik, Soll/Ist

**WPF-Maske schreibt in Excel:**
- Felder: Datum, Arbeiter (Dropdown), Baustelle (Dropdown), Stundenart, Stunden
- Button "Speichern" → neue Zeile in tbl_Zeiten per ClosedXML
- Validierung in WPF (Pflichtfelder, Plausibilität)
- Baustellen aus bpm.db, Rest aus Excel-Stammdaten

**Reihenfolge:**
1. Excel-Architektur fertig (nach ChatGPT Master-Prompt)
2. WPF-Eingabemaske als neues Modul im BauProjektManager
3. ClosedXML-Anbindung (lesen + schreiben)

| Feature | Status |
|---------|--------|
| Excel-Architektur (tbl_Zeiten etc.) | ⬜ Eigenes Projekt |
| WPF-Eingabemaske | ⬜ |
| ClosedXML liest/schreibt Excel | ⬜ |
| Baustellen-Dropdown aus bpm.db | ⬜ |
| Überstunden-Auswertung in Excel | ⬜ |

---

## Ideen / Noch nicht eingeordnet

- VS Code für mehrzeiliges Suchen/Ersetzen (VS Studio kann das nicht gut)
- Rainbow Braces Extension für VS Studio
- DB Browser for SQLite installieren (zum Nachschauen der DB)
- Projekt-Ordner aus App heraus im Explorer öffnen (Button)
- Ordner-Template: User soll Ordner hinzufügen/umbenennen/Reihenfolge ändern können
- Ordner nummeriert: 01_Pläne, 02_Fotos, 03_Dokumente etc.
- Pfade-Erkennung via .bpm-manifest bei Ordner-Umbenennung

---

*Datei wird laufend von Claude aktualisiert wenn Herbert neue Ideen hat.*
