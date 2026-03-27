# BauProjektManager — Backlog & Ideen

**Letzte Aktualisierung:** 27.03.2026  
**Format:** Priorität → Feature → Beschreibung → Status

---

## V1 — Aktuelle Phase

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| SQLite-Datenbank | Projekte dauerhaft speichern in bpm.db | ✅ Erledigt |
| ID-Fix | Auto-Increment IDs (proj_001, client_001, bldg_001) | ✅ Erledigt |
| registry.json Export | SQLite → JSON für VBA-Makros, Export-Ordner | ✅ Erledigt |
| **Ersteinrichtung** | **Basispfad konfigurieren (OneDrive kann D:, E: etc. sein). Beim ersten Start abfragen, in settings.json speichern. MUSS VOR PlanManager kommen!** | ⬜ Nächster Schritt |
| Architektur v1.5 | Doku aktualisieren (.NET 10, Client, Adresse) | ⬜ |
| Suchfeld Projekte | Schnellsuche in der Projektliste | ⬜ |
| **PlanManager** | Dateinamen-Parser, Segment-Zuweiser, Import-Workflow | ⬜ Nach Ersteinrichtung |

---

## V1.1 — Bald danach

| Feature | Beschreibung | Status |
|---------|-------------|--------|
| Adressbuch | Eigene Kontakt-DB in SQLite, projektübergreifend | ⬜ |
| Bauherren-/Firmenliste | Dropdown "Auftraggeber wählen" statt jedes Mal tippen | ⬜ |
| Button "Aus Adressbuch" | Im Projekt-Dialog neben Auftraggeber | ⬜ |

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
- Ersteinrichtung: OneDrive-Pfad automatisch erkennen (Environment Variable?)

---

*Datei wird laufend von Claude aktualisiert wenn Herbert neue Ideen hat.*
