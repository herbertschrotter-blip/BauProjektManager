# BauProjektManager — Backlog & Ideen

**Letzte Aktualisierung:** 29.03.2026  
**Aktuelle Version:** v0.12.4  
**Format:** Priorität → Feature → Beschreibung → Status

---

## V1 — Phase 1: Einstellungen

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
| 10 | Projektordner erstellen | Nummerierte Ordnerstruktur mit Unterordnern, Präfix-Schalter, TreeView im Dialog + Einstellungen, _Eingang, Vorschau-Baum | ✅ Erledigt (v0.11.0–v0.12.4) |
| 11 | **.bpm-manifest** | Versteckte Datei in jedem Projektordner (Ausweis), bei Umbenennung → Auto-Suche + Pfad aktualisieren | ⬜ Nächster Schritt |
| 12 | **Projekt archivieren** | Status → Completed → Ordner in Archiv verschieben, Pfad aktualisieren | ⬜ (Button vorbereitet, disabled) |
| 13 | Pfade änderbar | Gelbe 📁-Buttons mit OpenFolderDialog für Arbeitsordner + Archivordner | ✅ Erledigt (v0.12.3) |
| 14 | **Single-Writer Mutex** | Nur eine App-Instanz gleichzeitig | ⬜ |
| 15 | **Suchfeld Projekte** | Schnellsuche/Filter in der Projektliste | ⬜ |
| 16 | **Versionsnummer im Log** | Automatisch aus Assembly, nicht hardcoded | ⬜ |
| 17 | **Architektur v1.5** | Doku aktualisieren (.NET 10, Client, Adresse, Manifest, Ordnerstruktur, Unterordner, Tabs) | ⬜ |

**Zusätzlich erledigt (nicht im ursprünglichen Backlog):**
- Löschen-Button mit Bestätigungsdialog (v0.11.3)
- 2-Tab-Einstellungsseite: Projekte + Standard-Ordnerstruktur (v0.12.0)
- Status-Anzeige mit Farben: ● Aktiv (grün), ● Abgeschlossen (rot), ● Archiviert (grau) (v0.12.0)
- Standard-Ordnerstruktur mit Unterordnern + Präfix an/aus (v0.12.0)
- Projekt-Refresh nach Bearbeiten (v0.12.1)
- 2-Spalten ProjectEditDialog (1050×780) mit allen Architektur-Feldern (v0.11.1)
- Gleiches GUI für Neu + Bearbeiten — TreeView mit Unterordnern in rechter Spalte (v0.12.4)

---

## V1 — Phase 1.5: Bestehende Projekte importieren

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Bestehende Ordner zuweisen | Projekt erstellen und existierenden Ordner zuweisen statt neue Ordner anzulegen. Bestehende Unterordner werden in TreeView übernommen. | ⬜ |
| Nachträgliche Sortierung | Bei zugewiesenen Ordnern nachträglich Präfix (Nummerierung) auf Hauptordner anwenden und Sortierung ändern — gleiche UI wie bei Neues Projekt | ⬜ |
| Ordner umbenennen auf Disk | Wenn Präfix/Sortierung geändert wird, bestehende Ordner auf der Festplatte entsprechend umbenennen | ⬜ |

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
| 34 | **Plan-Sammler** | Pläne aus bestimmten Ordnern per Checkbox-Auswahl sammeln und nach bekanntem Schema sortieren (z.B. alle Bewehrungspläne aus mehreren Quellen zusammenführen) | ⬜ |

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

## Modul: Foto-Management (Nach V1)

**Konzept:** Professionelles Foto-Management für Baustellenfotos mit Viewer, Tags, Bewertung und Geodaten-Analyse.

**Fotoviewer:**
- Professioneller Viewer mit Vollbild, Zoom, Navigation
- Tag-System: Schlagwörter pro Foto (z.B. "Schalung", "Bewehrung", "Mangel")
- Bewertungssystem: Sterne oder Farbmarkierung
- Filterung und Suche nach Tags, Datum, Bewertung
- Sortierung nach Aufnahmedatum, Name, Bewertung

**Geodaten-Analyse:**
- GPS-Koordinaten aus EXIF-Daten lesen
- Fotos automatisch dem richtigen Projekt zuordnen anhand Geodaten
- Falsch zugeordnete Fotos erkennen (Foto auf Baustelle A aber im Ordner von Baustelle B)
- Fotos anderen Projekten zuweisen oder entfernen per Vorschlag

**Baubericht-Integration:**
- Alle Fotos eines Tages einem Baubericht zuweisen
- Im Baubericht bei einzelnen Punkten Fotos zuordnen
- Auswahl welche Fotos in den Druck kommen (Checkbox pro Foto)
- Restliche Fotos bleiben zugewiesen aber nicht im Druck-PDF
- PDF-Export: zuklappbare Sektionen prüfen (ob PDF das unterstützt oder ob besser fixe Auswahl)

**Auto-Tagging (später):**
- Tags automatisch aus Baubericht-Text vorschlagen
- Wenn Baubericht "Schalung OG" enthält → Fotos vom gleichen Tag mit Tag "Schalung" vorschlagen

| Feature | Status |
|---------|--------|
| Fotoviewer (Vollbild, Zoom, Navigation) | ⬜ |
| Tag-System + Bewertung | ⬜ |
| Filter + Suche nach Tags/Datum | ⬜ |
| Geodaten aus EXIF lesen | ⬜ |
| Falsche Fotos per Geodaten erkennen | ⬜ |
| Fotos anderen Projekten zuweisen | ⬜ |
| Baubericht-Fotozuordnung | ⬜ |
| Druckauswahl (welche Fotos ins PDF) | ⬜ |
| Auto-Tagging aus Baubericht | ⬜ |

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

## Später — Nach V1

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| GIS Steiermark API | Grundstücksdaten + Koordinaten + Grenzen per API abfragen (KG, GST, Koordinaten automatisch befüllen) | ⬜ |
| Google Maps API | Adresse → PLZ, Ort, Gemeinde, Bezirk, Koordinaten automatisch | ⬜ |
| Outlook-Adressbuch | Kontakte aus Outlook importieren/übernehmen | ⬜ |
| Adressbuch-Sync | Eigenes Adressbuch ↔ Outlook abgleichen | ⬜ |
| Outlook COM Integration | Projekt-Ordner in Outlook, Anhänge extrahieren | ⬜ |
| Bautagebuch | Tägliches Protokoll mit Auto-Befüllung + Export | ⬜ |
| Wetter-API | Wetterdaten pro Baustelle | ⬜ |
| Excel/Word Vorlagen | COM Interop, Projektdaten in Vorlagen befüllen | ⬜ |
| PDF-Vorschau | Pläne in der App anzeigen | ⬜ |
| VERALTET-Stempel | Auf alte Plan-PDFs stempeln | ⬜ |
| Auto-Update | App-Update-Mechanismus | ⬜ |

---

## Vision — Zukunftsideen (eventuell geplant, nicht sicher)

Diese Ideen beeinflussen Architektur-Entscheidungen beim aktuellen Coden. Noch nicht committed, aber im Hinterkopf behalten.

### Projekt-Dashboard (zentrale Ansicht nach Projektauswahl)

**Konzept:** Sidebar zeigt Projektliste, Klick auf Projekt öffnet ein Dashboard mit allen projektrelevanten Infos auf einen Blick.

**Dashboard-Mockup (Layout v1):**
```
┌─────────────────────────────────────────────────────────────────────────┐
│ Sidebar          │  OWG-Dobl-Zwaring                    [Aktiv] 202512 │
│ ──────────       │─────────────────────────────────────────────────────│
│ Alle Projekte    │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐│
│ ───────────      │  │Pläne ges.│ │Neu/Woche │ │Fotos/Mon.│ │Fortschr││
│ ► OWG Dobl       │  │   247    │ │    12    │ │    89    │ │  62%   ││
│   EFH Schlogl    │  └──────────┘ └──────────┘ └──────────┘ └────────┘│
│   Sanierung Lb   │                                                    │
│ ───────────      │  ┌─ Letzte Planänderungen ─┐ ┌─ Schnellzugriff ──┐│
│ Einstellungen    │  │ BWP-OG-001_Rev03  heute │ │ [01 Planunterlag] ││
│                  │  │ STP-EG-004_Rev01  gest. │ │ [02 Fotos]        ││
│                  │  │ ARP-KG-002_Rev05  27.03 │ │ [03 Leica]        ││
│                  │  │ FTP-UG-003_Rev02  25.03 │ │ [04 DOKA] [05 LV] ││
│                  │  └─────────────────────────┘ │ [06 Protokolle]   ││
│                  │                               │ → öffnet Explorer ││
│                  │                               └───────────────────┘│
│                  │  ┌─ Externe Portale ────────┐ ┌─ Bestellungen ────┐│
│                  │  │ [InfoRaum] [ClickUp]     │ │ Beton C25 bestellt││
│                  │  │ [PlanRadar]              │ │ Ziegel    offen   ││
│                  │  │ → je Auftraggeber/Firma  │ │ Stahl   geliefert ││
│                  │  └─────────────────────────┘ └───────────────────┘│
│                  │                                                    │
│                  │  ┌─ Toolbar (konfigurierbar) ─────────────────────┐│
│                  │  │ [AutoCAD] [Excel] [Outlook] [Leica] [+ Progr.]││
│                  │  └────────────────────────────────────────────────┘│
│                  │  Auftraggeber: ÖWG | Hauptstr. 12, 8143 Dobl      │
└─────────────────────────────────────────────────────────────────────────┘
```

**Kennzahlen-Karten:**
- Pläne gesamt, neue Pläne diese Woche, Fotos diesen Monat, Baufortschritt %

**Letzte Planänderungen:**
- Liste der zuletzt geänderten/hinzugefügten Pläne mit Datum

**Schnellzugriff Ordner:**
- Buttons pro Projektordner (01 Planunterlagen, 02 Fotos etc.) — Klick öffnet im Explorer
- Beliebteste/häufigste Ordner nach oben sortieren

**Externe Portale + Links:**
- Konfigurierbare Link-Buttons je Auftraggeber/Firma
- Beispiele: InfoRaum (ÖWG), PlanFred, PlanRadar, eigene Bauherren-Portale
- Nur anzeigen wenn konfiguriert (dynamisch ein/ausblenden)
- Portal-Info kommt aus Firmendaten (nicht Projektdaten!)

**Bestellungen + Material:**
- ClickUp-Bestellungen (firmeninterne Materialbestellungen)
- Betonbestellung, Ziegelbestellung, Lieferlisten
- Leistungsverzeichnisse einbinden
- MS Project Anbindung (Baufortschritt, Terminplan)

**Konfigurierbare Toolbar (Ribbon-ähnlich):**
- Programme direkt starten: AutoCAD, Excel, Outlook, Leica Infinity, PDF Viewer etc.
- Konfigurierbar in Einstellungen (welche Programme, Pfade)
- "+ Programm" Button zum Hinzufügen

**Ablagesystem:**
- Möglichkeit Dateien pro Projekt abzulegen (nicht nur in Ordner, sondern auch in der App verwaltet)
- Kategorien/Tags für Dokumente

### Firmendaten-Verwaltung (Auftraggeberdaten)

**Konzept:** Nicht nur Kontaktdaten pro Projekt, sondern eine zentrale Firma/Auftraggeber-Datenbank mit wiederkehrenden Infos.

- Welches Portal benutzt die Firma (InfoRaum, PlanFred, PlanRadar, keines)
- Portal-URLs/Login-Seiten
- Bevorzugte Planformate, Lieferadressen
- Alles änderbar, projektübergreifend nutzbar
- Im Projekt-Dialog: "Auftraggeber wählen" → automatisch Portal-Links, Kontaktdaten etc. übernehmen

### Kalender-Integration

- Projekt-Termine, Liefertermine, Besprechungen
- Sync mit Outlook-Kalender möglich
- Im Dashboard als Widget anzeigen

### Architektur-Implikationen für aktuelles Coden

Diese Zukunftsideen bedeuten für die aktuelle Architektur:
- **Client/Firma als eigene Entität** in der DB (nicht nur eingebettetes Objekt im Projekt) — vorbereiten für Firmendaten-Verwaltung
- **Projekt-ID überall mitführen** — Dashboard, Fotos, Bestellungen referenzieren Projekte
- **Plugin/Modul-Architektur** beibehalten — jedes Modul (Foto, Baubericht, Bestellung) ist ein eigenes WPF-Projekt
- **Externe Links als konfigurierbare Datenstruktur** — nicht hardcoded, sondern in DB/settings.json pro Firma
- **Toolbar-Konfiguration** als eigene Settings-Sektion planen

---

## Ideen / Noch nicht eingeordnet

- VS Code für mehrzeiliges Suchen/Ersetzen (VS Studio kann das nicht gut)
- Rainbow Braces Extension für VS Studio
- DB Browser for SQLite installieren (zum Nachschauen der DB)
- Projekt-Ordner aus App heraus im Explorer öffnen (Button)
- Pfade-Erkennung via .bpm-manifest bei Ordner-Umbenennung

---

*Datei wird laufend von Claude aktualisiert wenn Herbert neue Ideen hat.*
