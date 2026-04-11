---
doc_id: ux-flows
doc_type: reference
authority: secondary
status: active
owner: herbert
topics: [ux-flows, user-workflows, app-start, projekt-anlegen, import, navigation]
read_when: [neuer-workflow, user-flow-prüfen, screen-reihenfolge, fehlerfall-klären]
related_docs: [ui-ux-guidelines, ui-navigation, planmanager, module-projekt]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: User-Workflow-Beschreibungen mit Flowcharts für alle BPM-Hauptaktionen
- Autorität: secondary (UI_UX_Guidelines.md = source_of_truth für Design)
- Lesen wenn: Neuer Workflow, User-Flow prüfen, Screen-Reihenfolge, Fehlerfall klären
- Nicht zuständig für: Design-Tokens (→ UI_UX_Guidelines.md), technische XAML-Umsetzung (→ WPF_UI_Architecture.md)
- Pflichtlesen: keine (gezieltes Nachschlagen per Workflow)
- Fachliche Invarianten:
  - Jeder Flow zeigt: Ziel, Schritte, Screens, Fehlerfälle
  - ASCII-Flowcharts als Darstellungsformat

---

# BauProjektManager — UX Flows

**Version:** 1.0  
**Datum:** 30.03.2026  
**Bezug:** UI_UX_Guidelines.md (Design-Regeln), WPF_UI_Architecture.md (technischer Aufbau)

---

## 1. Ziel

Dieses Dokument beschreibt die **Hauptworkflows** die ein User in BPM durchläuft. Jeder Flow zeigt: Was will der User erreichen, welche Schritte durchläuft er, welche Screens sieht er, was kann schiefgehen.

---

## 2. App-Start

### 2.1 Erster Start (kein Setup vorhanden)

```
App öffnet
    │
    ▼
Ersteinrichtungs-Dialog
    │ Cloud-Speicher Auto-Erkennung (OneDrive, Google Drive, Dropbox)
    │ Arbeitsordner wählen (oder Vorschlag bestätigen)
    │ Archivordner wählen (oder Vorschlag bestätigen)
    │
    ▼
[Einrichtung abschließen]
    │
    ▼
Hauptfenster → Einstellungen (Projekte-Tab)
    │
    ▼
Empty State: "Noch keine Projekte angelegt"
    │
    ▼
[+ Neues Projekt anlegen]
```

**Fehlerfall:** Cloud-Speicher nicht erkannt → Manuell Pfad wählen. Kein Abbruch möglich ohne Setup.

### 2.2 Normaler Start (Setup vorhanden)

```
App öffnet
    │
    ├── settings.json laden ✓
    ├── bpm.db öffnen ✓
    ├── registry.json exportieren ✓
    │
    ▼
Hauptfenster → letztes aktives Modul
    │
    ▼
Statusleiste: "Bereit | 5 Projekte geladen"
```

**Fehlerfall:** bpm.db nicht erreichbar → Error State mit Meldung + "Erneut versuchen".

---

## 3. Projekt anlegen

### 3.1 Flow

```
Einstellungen → Projekte-Tab
    │
    ▼
[+ Neues Projekt] (Toolbar oder Button)
    │
    ▼
ProjectEditDialog öffnet (5 Tabs)
    │
    ├── Tab 1: Stammdaten
    │   Projektname * ausfüllen
    │   Kurzname, Langname, Typ, Status
    │   Auftraggeber (Firma, Kontakt)
    │   Zeitraum (Projektstart → wird Projektnummer YYYYMM)
    │
    ├── Tab 2: Bauwerk
    │   [+ Bauteil hinzufügen] → "H5", "H6"
    │   Pro Bauteil: Geschoße definieren (KG, EG, 1.OG...)
    │   RDOK/FBOK Höhen eingeben
    │
    ├── Tab 3: Beteiligte
    │   [+ Beteiligter hinzufügen] → Rolle, Firma, Kontakt
    │
    ├── Tab 4: Portale + Links
    │   [+ Portal hinzufügen] → Name, URL
    │
    ├── Tab 5: Ordnerstruktur
    │   Vorschau der zu erstellenden Ordner
    │   Checkboxen: Welche Ordner anlegen
    │   Pfad-Vorschau: D:\Cloud\Projekte\202406_ÖWG-Dobl\...
    │
    ▼
[Speichern]
    │
    ├── Projekt in bpm.db speichern
    ├── Ordner im Dateisystem erstellen
    ├── .bpm-manifest in Projektordner schreiben
    ├── registry.json aktualisieren
    │
    ▼
Toast: "Projekt 'ÖWG Dobl' gespeichert ✓"
Projektliste aktualisiert
```

### 3.2 Fehlerfall

| Fehler | Reaktion |
|--------|---------|
| Projektname leer | Live-Validierung: rote Umrandung + "Projektname darf nicht leer sein" |
| Ordner existiert bereits | Dialog: "Der Ordner existiert bereits. Trotzdem verknüpfen?" |
| Kein Schreibrecht auf Pfad | Error Toast: "Ordner konnte nicht erstellt werden. Prüfe die Berechtigungen." |
| DB gesperrt (Multi-User) | Error Toast: "Datenbank wird von [Name] bearbeitet. Bitte warten." |

---

## 4. Projekt bearbeiten

### 4.1 Flow

```
Einstellungen → Projekte-Tab
    │
    ├── Projekt in Liste auswählen (Einfachklick)
    │
    ▼
[Bearbeiten] (Toolbar) ODER Doppelklick auf Zeile
    │
    ▼
ProjectEditDialog öffnet (mit Daten befüllt)
    │
    ├── Änderungen in beliebigen Tabs vornehmen
    │
    ▼
[Speichern]
    │
    ├── Projekt in bpm.db aktualisieren
    ├── registry.json aktualisieren
    │
    ▼
Toast: "Änderungen gespeichert ✓"
```

### 4.2 Abbrechen

```
[Abbrechen] ODER [✕] ODER Escape
    │
    ├── Wenn keine Änderungen: Dialog schließt sofort
    ├── Wenn Änderungen vorhanden:
    │       │
    │       ▼
    │   "Ungespeicherte Änderungen verwerfen?"
    │       │
    │       ├── [Verwerfen] → Dialog schließt
    │       └── [Zurück zum Bearbeiten] → Dialog bleibt offen
```

---

## 5. Projekt löschen

### 5.1 Flow

```
Einstellungen → Projekte-Tab
    │
    ├── Projekt auswählen
    │
    ▼
[Löschen] (Toolbar, Danger-Button)
    │
    ▼
Bestätigungsdialog:
    "Soll das Projekt 'ÖWG Dobl' wirklich gelöscht werden?
     Diese Aktion kann nicht rückgängig gemacht werden.
     Die Projektordner im Dateisystem bleiben erhalten."
    │
    ├── [Abbrechen] → nichts passiert
    │
    └── [Löschen] (Danger-Button)
            │
            ├── Projekt aus bpm.db entfernen (CASCADE: Bauteile, Geschoße, Beteiligte, Links)
            ├── registry.json aktualisieren
            ├── Projektordner im Dateisystem bleiben bestehen
            │
            ▼
        Toast: "Projekt 'ÖWG Dobl' gelöscht"
        + Undo-Link im Toast (5 Sekunden)
```

---

## 6. Plan-Import (PlanManager)

### 6.1 Haupt-Flow

```
PlanManager → Projekt auswählen
    │
    ▼
[Import starten] (Toolbar)
    │
    ▼
Eingangsordner scannen (_Eingang)
    │
    ├── Dateien gefunden?
    │   ├── Nein → Toast: "Keine neuen Pläne im Eingang"
    │   └── Ja ↓
    │
    ▼
Dateinamen parsen (Plantyp-Profile anwenden)
    │
    ▼
Import-Vorschau Dialog:
    ┌──────────────────────────────────────────────────────┐
    │ Import-Vorschau: 12 Dateien                          │
    ├──────────┬──────┬──────┬─────────────────────────────┤
    │ Datei    │ Plan │ Index│ Aktion                      │
    ├──────────┼──────┼──────┼─────────────────────────────┤
    │ S-103-D  │  103 │  D   │ 🟢 Index-Update (C → D)    │
    │ S-104-B  │  104 │  B   │ 🟢 Neuer Plan              │
    │ S-103-D  │  103 │  D   │ ⚪ Identisch (Skip)        │
    │ Notiz.pdf│  —   │  —   │ 🔴 Nicht erkannt           │
    ├──────────┴──────┴──────┴─────────────────────────────┤
    │ 2 neue, 1 Update, 1 Skip, 1 nicht erkannt            │
    └──────────────────────────────────────────────────────┘
    │
    ├── "Nicht erkannt" → Manuell zuweisen oder ignorieren
    │
    ▼
[Importieren]
    │
    ├── Dateien in Zielordner verschieben/kopieren
    ├── Alte Indexe in _Archiv verschieben
    ├── Import-Journal in planmanager.db schreiben
    │
    ▼
Toast: "12 Pläne importiert (2 neu, 1 Update)"
    │
    ▼
[↩️ Rückgängig] (in Toolbar, 5 Minuten verfügbar)
```

### 6.2 Fehlerfälle

| Fehler | Reaktion |
|--------|---------|
| Eingangsordner existiert nicht | Error State: "Eingangsordner nicht gefunden. Projekt-Pfade prüfen." |
| Datei gesperrt (OneDrive Sync) | Warnung pro Datei in der Vorschau, überspringbar |
| Zielordner nicht beschreibbar | Error Toast + betroffene Dateien in Vorschau markiert |
| Plantyp nicht erkannt | Gelb markiert in Vorschau, manuell zuweisbar |

---

## 7. Ordnerstruktur anlegen

### 7.1 Flow (im ProjectEditDialog, Tab 5)

```
Tab 5: Ordnerstruktur
    │
    ├── Standard-Vorlage wird angezeigt (aus settings.json)
    │   01 Planunterlagen
    │   ├── 00 Polierpläne
    │   ├── 01 Bewehrungspläne
    │   ├── _Eingang
    │   02 Fotos
    │   03 LV
    │   ...
    │
    ├── Checkboxen pro Ordner (an/aus)
    ├── Pfad-Vorschau: D:\Cloud\Projekte\202406_ÖWG-Dobl\01 Planunterlagen\...
    │
    ▼
[Speichern] (im Dialog)
    │
    ├── Gewählte Ordner im Dateisystem erstellen
    ├── _Eingang-Ordner merken (für PlanManager)
    ├── .bpm-manifest in Root-Ordner schreiben
    │
    ▼
Toast: "Projektordner erstellt ✓"
```

---

## 8. Ordnerstruktur-Vorlage bearbeiten

### 8.1 Flow (Einstellungen → Tab 2)

```
Einstellungen → Standard-Ordnerstruktur Tab
    │
    ▼
TreeView zeigt aktuelle Vorlage
    │
    ├── [＋] Hauptordner hinzufügen (immer nummeriert)
    ├── [＋└] Unterordner zum gewählten Hauptordner
    ├── [▲] [▼] Reihenfolge ändern
    ├── [✕] Ordner entfernen
    ├── [📥] _Eingang ein/aus
    ├── [##] Präfix ein/aus (nur Unterordner)
    │
    ▼
Änderungen werden sofort in settings.json gespeichert
    │
    ▼
Statusleiste: "Ordnerstruktur aktualisiert"
```

---

## 9. Zeiterfassung (Zukunft)

### 9.1 Täglicher Flow

```
Zeiterfassung → Projekt auswählen → Datum (heute)
    │
    ▼
Mitarbeiterliste anzeigen
    │
    ├── Pro Mitarbeiter:
    │   Stunden eingeben [8.0]
    │   ODER Abwesenheit wählen [U / K / Sonstiges]
    │
    ▼
[Speichern]
    │
    ├── time_entries in bpm.db schreiben
    ├── Excel-Datei aktualisieren (ClosedXML)
    │
    ▼
Toast: "Zeiterfassung für 05.11. gespeichert ✓"
```

---

## 10. Arbeitseinteilung (Zukunft)

### 10.1 Täglicher Flow (2 Minuten morgens)

```
Kalkulation → Arbeitseinteilung → Datum (heute)
    │
    ▼
Matrix: Mitarbeiter (Zeilen) × Arbeitspakete (Spalten)
    │
    ├── "x" setzen = Mitarbeiter dem Paket zuweisen
    ├── Abwesende automatisch grau (aus Zeiterfassung)
    ├── [Vom Vortag übernehmen] → spart Zeit
    │
    ▼
[Speichern]
    │
    ├── work_assignments in bpm.db schreiben
    │
    ▼
Toast: "Arbeitseinteilung gespeichert ✓"
```

---

## 11. Arbeitspaket abschließen (Zukunft)

### 11.1 Fertigmeldung

```
Kalkulation → Arbeitspakete → Paket auswählen
    │
    ▼
[Fertig melden]
    │
    ▼
Fertigmeldungs-Dialog:
    │
    │ Paket: Mauerwerk 38er — H5 / EG
    │ Soll-Menge: 198 m² (aus Ziegelberechnung)
    │ Ist-Menge: [198] m² ← bestätigen oder korrigieren
    │ Zeitraum: 11.03. — 14.03. (automatisch)
    │ Arbeitsstunden: 123 Ah (automatisch)
    │
    │ Ergebnis: 198 m² ÷ 123 Ah = 1,61 m²/Ah
    │
    ▼
[Abschließen & in Leistungskatalog übernehmen]
    │
    ├── work_package Status → "completed"
    ├── performance_catalog Eintrag erstellen
    │
    ▼
Toast: "Mauerwerk H5/EG abgeschlossen — 1,61 m²/Ah ✓"
```

---

## 12. Materialbestellung (Zukunft)

### 12.1 Bestellung aus BPM

```
Kalkulation → Arbeitspakete → Paket auswählen
    │
    ▼
[Material bestellen]
    │
    ▼
Bestelldialog:
    │ Projekt: ÖWG Dobl (automatisch)
    │ Bauteil: [H5 ▾] Geschoß: [EG ▾]
    │ Material: [Ziegel 38er Objekt Plan]
    │ Menge: [45] Einheit: [Pal ▾]
    │ Liefertermin: [📅 10.03.2025]
    │ Dringlichkeit: [Normal ▾]
    │
    ▼
[Bestellen & Task erstellen]
    │
    ├── material_orders in bpm.db schreiben
    ├── Task in ClickUp/Asana erstellen (ITaskManagementService)
    │
    ▼
Toast: "Bestellung angelegt — ClickUp Task erstellt ✓"
```

---

## 13. Bautagebuch (Zukunft)

### 13.1 Täglicher Flow (1 Minute abends)

```
Bautagebuch → Projekt → Datum (heute)
    │
    ▼
Auto-Vorschlag wird angezeigt:
    │ Wetter: ☀️ 12°C (aus API)
    │ Tätigkeiten: (aus Arbeitseinteilung)
    │   • Attika: Biskup D., Cahunek I. — 16 Ah
    │   • Bewehrung Decke: Dula A., Krunoslav — 32 Ah
    │ Personal: 10 anwesend, 3 abwesend (aus Zeiterfassung)
    │
    ├── Bemerkungen ergänzen (manuell)
    ├── Fotos hinzufügen (optional)
    │
    ▼
[✓ Bestätigen]
    │
    ├── diary_entries in bpm.db schreiben
    │
    ▼
Toast: "Bautagebuch 05.11. bestätigt ✓"
```

---

## 14. Cross-Modul Navigation

### 14.1 Von überall zu überall

```
Sidebar → Modul klicken → View wechselt
    │
    ├── Einstellungen → SettingsView (Projekte, Ordnerstruktur)
    ├── PlanManager → PlanManagerView (Import, Planlisten)
    ├── Dashboard → DashboardView (Übersicht, Widgets)
    ├── Bautagebuch → DiaryView (Tageseinträge)
    ├── Zeiterfassung → TimeTrackingView (Stundenliste)
    ├── Kalkulation → CalculationView (Arbeitspakete, Prognose)
    │
    ▼
Breadcrumb zeigt: [Modulname] > [Unterseite] > [Detail]
Toolbar zeigt: Modul-spezifische Buttons
```

### 14.2 Projekt-Kontext

Viele Module brauchen ein Projekt als Kontext. Das aktive Projekt wird global gespeichert:

```
Toolbar: [ÖWG Dobl ▾] ← Projekt-Auswahl (immer sichtbar wenn Modul es braucht)

Modulwechsel:
├── Einstellungen → kein Projekt-Kontext nötig (zeigt alle)
├── PlanManager → aktives Projekt muss gewählt sein
├── Bautagebuch → aktives Projekt + Datum
├── Zeiterfassung → aktives Projekt + Datum
├── Kalkulation → aktives Projekt
```

### 14.3 Deep Links zwischen Modulen

```
Bautagebuch Eintrag → "Bewehrung Decke: 32 Ah"
    → Klick → springt zu Kalkulation → Arbeitspaket "Bewehrung Decke H5/EG"

Dashboard → "3 offene Materialbestellungen"
    → Klick → springt zu Kalkulation → Bestellübersicht

PlanManager → "Plan S-103-D importiert"
    → Klick → öffnet Ordner im Explorer
```

---

## 15. Standard-Interaktionsmuster

### 15.1 Listen mit Aktionen

```
┌───────────────────────────────────────────────┐
│ Toolbar: [+ Neu]  [✏️ Bearbeiten]  [🗑 Löschen] │
├───────────────────────────────────────────────┤
│ Zeile 1 (selektiert)                          │
│ Zeile 2                                       │
│ Zeile 3                                       │
└───────────────────────────────────────────────┘

Einfachklick → selektieren
Doppelklick → bearbeiten (Dialog öffnen)
Toolbar-Buttons → auf selektierte Zeile anwenden
Kein Kontextmenü in V1 (später optional)
```

### 15.2 Formulare mit Tabs

```
┌──────────────────────────────────────────────┐
│ Dialog-Titel                            [✕]  │
├──────────────────────────────────────────────┤
│ Tab 1   Tab 2   Tab 3                        │
│ ──────                                       │
│                                              │
│ [Formularfelder]                             │
│                                              │
├──────────────────────────────────────────────┤
│              [Abbrechen]    [Speichern]       │
└──────────────────────────────────────────────┘

Tabs wechseln ohne Speichern (Daten bleiben)
Speichern speichert ALLE Tabs auf einmal
Validierung über ALLE Tabs beim Speichern
Fehler → springt zum Tab mit dem ersten Fehler
```

### 15.3 Import-Workflow (universell)

Dieses Muster gilt für jeden Import in BPM (Pläne, LV, Excel-Daten):

```
1. Quelle wählen        → Datei/Ordner auswählen
2. Vorschau anzeigen     → Was wird importiert? Was passiert?
3. Bestätigen           → [Importieren] Button
4. Ausführen            → Fortschrittsbalken
5. Ergebnis anzeigen    → Toast + Zusammenfassung
6. Rückgängig anbieten  → [↩️ Rückgängig] (zeitlich begrenzt)
```

### 15.4 Export-Workflow (universell)

```
1. Was exportieren?      → Auswahl (ganzes Projekt, Planliste, Bautagebuch-Monat)
2. Format wählen         → Excel / PDF / CSV
3. Speicherort wählen    → SaveFileDialog
4. Exportieren           → Fortschrittsbalken bei großen Exporten
5. Ergebnis              → Toast: "Exportiert nach D:\..."
                            + [Ordner öffnen] Link
```

---

*Dieses Dokument beschreibt WAS der User tut. WIE es aussieht steht in UI_UX_Guidelines.md. WIE es technisch gebaut wird steht in WPF_UI_Architecture.md.*