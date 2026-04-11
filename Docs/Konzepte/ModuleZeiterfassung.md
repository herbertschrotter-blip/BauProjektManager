---
doc_id: konzept-zeiterfassung
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [zeiterfassung, stundenzettel, excel, closedxml, personal, lohn]
read_when: [zeiterfassung-feature, stundenzettel, excel-export, personal-stunden]
related_docs: [architektur, dsvgo-architektur, konzept-kalkulation, konzept-bautagebuch]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für Zeiterfassung — WPF-Eingabemaske, Excel als Single Source of Truth via ClosedXML
- Autorität: secondary
- Lesen wenn: Zeiterfassung-Feature, Stundenzettel, Excel-Export, Personal-Stunden
- Nicht zuständig für: Kalkulation (→ ModuleKalkulation.md), Bautagebuch-Personal (→ ModuleBautagebuch.md)
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Excel bleibt Single Source of Truth für Roh-Zeitbuchungen (ADR-018)
  - DSGVO Klasse B — Mitarbeiterdaten in cloud-synced Excel
  - WPF-Maske schreibt via ClosedXML, kein COM Interop

---

﻿# BauProjektManager — Konzept: Arbeitszeiterfassung

**Erstellt:** 29.03.2026  
**Version:** 0.1 (Konzeptentwurf)  
**Status:** Zukunftsidee — Umsetzung nach V1  
**Abhängigkeit:** ClosedXML (bereits im Tech-Stack), BPM Dashboard (Konzept)  
**Verwandte ADR:** ADR-018 (WPF + ClosedXML → Excel)

---

## 1. Zielsetzung

### Problem

Stunden werden aktuell handschriftlich oder in unübersichtlichen Excel-Tabellen erfasst. Baustellen-Zuordnung fehlt oder ist inkonsistent. Das Lohnbüro braucht die Daten in einem bestimmten Format. Überstundenberechnung ist Formel-Chaos.

### Lösung

Zwei Komponenten die zusammenarbeiten:

1. **Excel-Datei** — Sauberes Datenmodell mit Tabellen, Power Query, Pivot. Bleibt die Single Source of Truth. Lohnbüro liest über OneDrive.
2. **BPM-Modul** — Schöne WPF-Eingabemaske (Dark Theme, Dropdowns, Kalender). Schreibt per ClosedXML in Excel. Liest auch aus Excel und zeigt Auswertungen im Dashboard an.

### Warum nicht alles in BPM/SQLite?

- Excel behält alle Formeln, Power Query, Pivot und Auswertungen
- Lohnbüro liest Excel direkt über OneDrive — kein Export nötig
- Excel Online ermöglicht Lesen/Filtern ohne Installation
- Bestehende Excel-Workflows bleiben intakt

### Warum nicht VBA oder Python?

- BPM ist eine C#-App — VBA wäre ein Medienbruch (ADR-018)
- ClosedXML braucht kein installiertes Excel zum Lesen/Schreiben
- Validierung und UI in WPF sind robuster als in VBA
- Python ist eine zusätzliche Runtime-Abhängigkeit ohne Mehrwert

---

## 2. Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────┐
│  BauProjektManager (WPF)                                │
│                                                         │
│  ┌─────────────────┐   ┌────────────────────────────┐   │
│  │ Zeiterfassungs-  │   │ Dashboard                  │   │
│  │ Eingabemaske     │   │ ┌──────────────────────┐   │   │
│  │                  │   │ │ Stunden diese Woche  │   │   │
│  │ Datum            │   │ │ Überstunden Monat    │   │   │
│  │ Mitarbeiter [▼]  │   │ │ Stunden pro Baustelle│   │   │
│  │ Baustelle   [▼]  │   │ └──────────────────────┘   │   │
│  │ Stundenart  [▼]  │   └──────────┬─────────────────┘   │
│  │ Stunden          │              │ liest               │
│  │ [Speichern]      │              │                     │
│  └────────┬─────────┘              │                     │
│           │ schreibt               │                     │
└───────────┼────────────────────────┼─────────────────────┘
            │                        │
            ▼                        ▼
┌─────────────────────────────────────────────────────────┐
│  Excel-Datei (OneDrive)                via ClosedXML    │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ tbl_Zeiten   │  │ tbl_Mitar-   │  │ tbl_Stunden- │  │
│  │ (append-only)│  │ beiter       │  │ arten        │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ tbl_Arbeits- │  │ tbl_Ueber-   │  │ tbl_Status-  │  │
│  │ zeitmodell   │  │ stundenregel │  │ codes        │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Power Query: vw_Zeiten_Auswertung               │   │
│  │ → Joins, Historienlogik, Soll/Ist, Überstunden  │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Pivot-Tabellen + Slicer (Anzeige, keine Logik)  │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  Lohnbüro liest über OneDrive / Excel Online            │
└─────────────────────────────────────────────────────────┘

Baustellen-Dropdown kommt aus BPM:
┌──────────┐
│ bpm.db   │──→ Baustellen-Liste für Dropdown
│ registry │    (aktive Projekte mit Namen)
└──────────┘
```

---

## 3. Datenmodell (Excel-Tabellen)

### 3.1 Nicht verhandelbare Prinzipien

- **Eine Haupttabelle** für Zeitdaten (`tbl_Zeiten`)
- **Append-only** — keine Löschungen, keine Änderungen an bestehenden Zeilen
- **IDs statt Klartext** in Beziehungen
- **Überstunden werden berechnet**, niemals gespeichert
- **Regeln ≠ Daten** — Überstundenregeln und Arbeitszeitmodelle sind eigene Tabellen
- **UI ≠ Datenbank** — Pivot/Slicer zeigen an, rechnen nicht

### 3.2 Haupttabelle: tbl_Zeiten

Single Point of Truth. Jede Zeile = ein Stundeneintrag.

| Spalte | Typ | Beschreibung | Beispiel |
|--------|-----|-------------|---------|
| ZeitID | Auto-ID | Eindeutige ID, nie wiederverwendet | 1, 2, 3... |
| Datum | Datum | Arbeitstag | 29.03.2026 |
| MitarbeiterID | Integer | FK → tbl_Mitarbeiter | 3 |
| BaustelleID | Text | FK → BPM Projekt-ID oder eigene ID | "proj_001" |
| StundenartID | Integer | FK → tbl_Stundenarten | 1 |
| Stunden | Dezimal | Arbeitsstunden (0.5er Schritte) | 8.5 |
| StatuscodeID | Integer | FK → tbl_Statuscodes (optional) | 1 |
| ErfasstAm | DateTime | Zeitstempel der Erfassung | 29.03.2026 18:30 |
| ErfasstVon | Text | Wer hat erfasst (User) | "Herbert" |

**Keine** Namen, Sollstunden, Überstunden, Formeln oder berechnete Werte in dieser Tabelle.

### 3.3 Stammdaten

**tbl_Mitarbeiter**

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| MitarbeiterID | Auto-ID | Stabil, nie wiederverwendet |
| Vorname | Text | |
| Nachname | Text | |
| Personalnummer | Text | Für Lohnbüro |
| AktivVon | Datum | Eintritt |
| AktivBis | Datum | Austritt (leer = aktiv) |
| ArbeitszeitmodellID | Integer | FK → aktuelles Modell |

**tbl_Stundenarten**

| Spalte | Typ | Beschreibung | Beispiel |
|--------|-----|-------------|---------|
| StundenartID | Auto-ID | | 1 |
| Bezeichnung | Text | | "Normalstunden" |
| Kuerzel | Text | | "N" |
| Aktiv | Boolean | | Ja |

Typische Stundenarten: Normalstunden, Regiestunden, Urlaubstag, Krankenstand, Feiertag, Zeitausgleich, Schlechtwetter.

**tbl_Statuscodes**

| Spalte | Typ | Beschreibung | Beispiel |
|--------|-----|-------------|---------|
| StatuscodeID | Auto-ID | | 1 |
| Bezeichnung | Text | | "Erfasst" |

Typische Statuscodes: Erfasst, Geprüft, Freigegeben, Korrigiert.

**tbl_Baustellen** (optional — oder aus BPM)

BPM liefert die Baustellen-Liste aus `bpm.db`. Für Baustellen die nicht in BPM sind (z.B. Werkstatt, Büro, Lager) kann eine eigene Tabelle ergänzen:

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| BaustelleID | Text | "proj_001" (aus BPM) oder "int_001" (intern) |
| Bezeichnung | Text | "ÖWG Dobl" oder "Büro" |
| Quelle | Text | "BPM" oder "Intern" |
| Aktiv | Boolean | |

### 3.4 Arbeitszeitmodelle (historisiert)

**tbl_Arbeitszeitmodell**

| Spalte | Typ | Beschreibung | Beispiel |
|--------|-----|-------------|---------|
| ModellID | Auto-ID | | 1 |
| Bezeichnung | Text | | "Vollzeit 39h" |
| Mo_Soll | Dezimal | Sollstunden Montag | 8.0 |
| Di_Soll | Dezimal | | 8.0 |
| Mi_Soll | Dezimal | | 8.0 |
| Do_Soll | Dezimal | | 8.0 |
| Fr_Soll | Dezimal | | 7.0 |
| Sa_Soll | Dezimal | | 0.0 |
| So_Soll | Dezimal | | 0.0 |
| WochenSoll | Formel | Summe Mo–So | 39.0 |

**tbl_Mitarbeiter_Arbeitszeitmodell** (Historisierung)

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| MitarbeiterID | Integer | FK |
| ModellID | Integer | FK |
| GueltigAb | Datum | Ab wann gilt dieses Modell |
| GueltigBis | Datum | Bis wann (leer = aktuell) |

Damit kann ein Mitarbeiter im Lauf der Zeit von Vollzeit auf Teilzeit wechseln, ohne dass alte Auswertungen kaputt gehen.

### 3.5 Überstundenregeln (historisiert)

**tbl_Ueberstundenregel**

| Spalte | Typ | Beschreibung | Beispiel |
|--------|-----|-------------|---------|
| RegelID | Auto-ID | | 1 |
| Bezeichnung | Text | | "Standard Bau-KV" |
| Typ | Text | "Tagesregel" oder "Wochenregel" | "Tagesregel" |
| Schwelle_Normal | Dezimal | Ab wann Überstunden | 8.0 |
| Schwelle_Zuschlag50 | Dezimal | Ab wann 50% Zuschlag | 10.0 |
| Schwelle_Zuschlag100 | Dezimal | Ab wann 100% Zuschlag | 12.0 |
| SchlechtwetterRegel | Boolean | Schlechtwetter-Tag = kein ÜS | Ja |
| GueltigAb | Datum | | 01.01.2025 |
| GueltigBis | Datum | Leer = aktuell gültig | |

**Überstunden werden ausschließlich in Power Query berechnet, nie gespeichert.**

---

## 4. Rollenverteilung

### 4.1 WPF (BPM-Modul) — Eingabe + Anzeige

| Aufgabe | Wie |
|---------|-----|
| Eingabemaske | Datum, Mitarbeiter (Dropdown), Baustelle (Dropdown), Stundenart (Dropdown), Stunden |
| Baustellen-Dropdown | Aus `bpm.db` — nur aktive Projekte |
| Mitarbeiter-Dropdown | Aus Excel `tbl_Mitarbeiter` via ClosedXML — nur aktive |
| Stundenarten-Dropdown | Aus Excel `tbl_Stundenarten` via ClosedXML |
| Validierung | Pflichtfelder, Plausibilität (z.B. Stunden 0–24, kein Datum in der Zukunft) |
| Speichern | Neue Zeile in `tbl_Zeiten` per ClosedXML (append-only) |
| Dashboard-Widget | Stunden diese Woche, Überstunden Monat, Stunden pro Baustelle |

### 4.2 Excel (Power Query) — Berechnung + Reporting

| Aufgabe | Wie |
|---------|-----|
| Joins | tbl_Zeiten ← tbl_Mitarbeiter ← tbl_Arbeitszeitmodell |
| Historienlogik | Welches Arbeitszeitmodell galt an welchem Datum? |
| Soll/Ist | Sollstunden (aus Modell) vs. Ist-Stunden (aus tbl_Zeiten) |
| Überstunden | Berechnet aus Regeln + Schwellenwerten, nie gespeichert |
| Finale View | `vw_Zeiten_Auswertung` — fertige Auswertung für Pivot |

### 4.3 Excel (Pivot + Slicer) — Anzeige

| Aufgabe | Wie |
|---------|-----|
| Monatsübersicht | Pivot pro Mitarbeiter, Baustelle, Stundenart |
| Slicer | Filtern nach Monat, Mitarbeiter, Baustelle |
| Lohnbüro-View | Gefiltert, druckfertig |

**Keine Geschäftslogik in Pivot** — nur Anzeige der Power Query Ergebnisse.

---

## 5. BPM-Dashboard Integration

### 5.1 Was BPM aus Excel lesen kann (via ClosedXML)

ClosedXML kann Excel-Tabellen direkt lesen. Für das Dashboard:

| Dashboard-Widget | Datenquelle | Berechnung |
|-----------------|-------------|-----------|
| "Stunden diese Woche" | tbl_Zeiten filtern auf aktuelle Woche | Summe in C# |
| "Stunden pro Baustelle (Monat)" | tbl_Zeiten filtern + gruppieren | Summe/Gruppe in C# |
| "Meine Überstunden" | Power Query Ergebnis-Tabelle | Lesen aus Excel |
| "Fehlende Einträge" | tbl_Zeiten vs. Arbeitstage | Vergleich in C# |

### 5.2 Einschränkung: Power Query Ergebnisse

Power Query aktualisiert sich nur wenn Excel geöffnet wird. BPM kann die **letzte berechnete** Power Query Tabelle lesen, aber nicht selbst aktualisieren. Für einfache Auswertungen (Summen, Filter) rechnet BPM in C# direkt aus `tbl_Zeiten`. Für komplexe Auswertungen (Überstunden mit Historienlogik) zeigt BPM den letzten Stand aus Excel an + Hinweis "Letzte Aktualisierung: [Datum]".

Alternative für die Zukunft: Die Überstundenberechnung auch in C# implementieren, dann ist BPM unabhängig von Power Query.

---

## 6. Multi-User (Zukunft)

Aktuell erfasst nur Herbert die Stunden. Falls später mehrere Nutzer:

| Szenario | Lösung |
|----------|--------|
| Mehrere schreiben gleichzeitig in Excel | ❌ ClosedXML sperrt die Datei beim Schreiben |
| Write-Lock (wie Mobile-Konzept) | ✅ Nur einer schreibt, andere warten (ADR-020) |
| Jeder hat eigene Excel, Zusammenführung | 🟡 Möglich aber aufwändig |
| Migration zu SQLite | ✅ Beste Langfristlösung (siehe Abschnitt 7) |

Für den Start reicht Single-User. Das Datenmodell ist so aufgebaut, dass eine Migration zu SQLite jederzeit möglich ist.

---

## 7. SQL-Migrationsfähigkeit

Das Excel-Datenmodell ist bewusst SQL-kompatibel gestaltet:

| Excel | SQL-Äquivalent |
|-------|---------------|
| tbl_Zeiten | Tabelle mit Primärschlüssel, Fremdschlüssel |
| Auto-ID | AUTOINCREMENT |
| IDs statt Klartext | Foreign Keys |
| Append-only | INSERT-only (kein UPDATE/DELETE) |
| Historisierung (GültigAb/Bis) | Temporal Tables oder manuell |
| Power Query Views | SQL Views |

Migration zu SQLite wäre: Tabellen 1:1 übernehmen, Power Query durch SQL Views ersetzen, ClosedXML durch Microsoft.Data.Sqlite. Das Datenmodell ändert sich nicht.

---

## 8. Excel-Datei Aufbau (Sheets)

| Sheet | Inhalt | Sichtbar? |
|-------|--------|----------|
| **Eingabe** | Manuelle Eingabe (Fallback wenn BPM nicht verfügbar) | Ja |
| **Daten** | tbl_Zeiten (Haupttabelle) | Versteckt |
| **Mitarbeiter** | tbl_Mitarbeiter | Versteckt |
| **Stundenarten** | tbl_Stundenarten | Versteckt |
| **Statuscodes** | tbl_Statuscodes | Versteckt |
| **Arbeitszeitmodelle** | tbl_Arbeitszeitmodell + Zuordnung | Versteckt |
| **Überstundenregeln** | tbl_Ueberstundenregel | Versteckt |
| **Auswertung** | Power Query Ergebnis (vw_Zeiten_Auswertung) | Ja |
| **Monatsübersicht** | Pivot + Slicer | Ja |
| **Lohnbüro** | Gefilterte Ansicht für Lohnbüro | Ja |

---

## 9. Umsetzungsreihenfolge

| Phase | Was | Wer |
|-------|-----|-----|
| **1** | Excel-Datenmodell erstellen (alle Tabellen, Power Query, Pivot) | Claude + Herbert |
| **2** | Excel mit Testdaten befüllen und Formeln/PQ testen | Herbert |
| **3** | WPF-Eingabemaske als BPM-Modul (`BauProjektManager.Zeiterfassung`) | Claude |
| **4** | ClosedXML-Anbindung: Schreiben in tbl_Zeiten, Lesen der Stammdaten | Claude |
| **5** | Baustellen-Dropdown aus bpm.db verbinden | Claude |
| **6** | Dashboard-Widget: Stunden-Übersicht aus Excel lesen | Claude |
| **7** | Überstunden-Anzeige im Dashboard (aus PQ-Ergebnis oder eigene Berechnung) | Claude |
| **8** | Multi-User / SQLite-Migration (wenn nötig) | Später |

---

## 10. Offene Entscheidungen

| Frage | Optionen | Entscheidung |
|-------|---------|-------------|
| Wo liegt die Excel-Datei? | OneDrive (synct) vs. lokal | OneDrive (Lohnbüro muss lesen) |
| Backup der Excel? | BPM macht Backup vor Schreiben | ⬜ Offen |
| Überstunden auch in C# berechnen? | Nur PQ vs. auch C# | ⬜ Offen (PQ erstmal, C# als Zukunft) |
| Korrektur-Workflow? | Storno-Zeile (append) vs. Editieren | Storno-Zeile (append-only bleibt) |
| Schlechtwetter-Erkennung? | Manuell (Stundenart) vs. Wetter-API | ⬜ Offen |

---

*Dieses Konzept kombiniert das saubere Datenmodell (Excel-Architektur) mit der BPM-Integration (WPF + ClosedXML + Dashboard). Details werden bei Umsetzung verfeinert.*
