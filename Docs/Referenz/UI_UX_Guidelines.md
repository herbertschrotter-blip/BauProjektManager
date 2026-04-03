# BauProjektManager — UI/UX Guidelines

**Version:** 2.1  
**Datum:** 04.04.2026  
**Gültig für:** Alle Module, alle Screens, alle zukünftigen Features  
**Design-Vorbild:** VS Code (Dark Theme, Sidebar, sauber, professionell)  
**Governance:** Dieses Dokument wird bei jeder UI-relevanten Änderung aktualisiert. Abweichungen sind erlaubt, müssen aber dokumentiert und begründet werden.

**Verwandte Dokumente:**
- [WPF_UI_Architecture.md](WPF_UI_Architecture.md) — Technische Umsetzung: Token → WPF Resource Keys, ResourceDictionary-Aufbau
- [CODING_STANDARDS.md](../Kern/CODING_STANDARDS.md) — Kap. 10 XAML-Konventionen, Kap. 17.7 Datenschutz-Logik nie im ViewModel
- [UX_Flows.md](UX_Flows.md) — User Workflows für Einstellungen und PlanManager
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Kap. 13 Datenschutz-GUI, Kap. 4.5 Dienststatus-Modell

**Sprachregelung:** Interne Doku-/Pattern-Begriffe (Loading, Empty, Toast, Breadcrumb etc.) dürfen Englisch sein. Alle sichtbaren UI-Texte in der App  sind Deutsch (siehe Kap. 13).

---

## 0. Aktueller UI-Stand (v0.15.0)

### 0.1 Shell-Aufbau

```
┌────────────────────────────────────────────────────┐
│ Sidebar (200px)  │  Content Area                   │
│ ─────────────── │                                  │
│ BauProjektManager│  ContentControl (wechselt pro   │
│                  │  Modul: SettingsView,            │
│ 📁 Pläne        │  PlanManagerView)                │
│ ⚙ Einstellungen │                                  │
│                  │                                  │
│                  │                                  │
├──────────────────┴──────────────────────────────────┤
│ Statusleiste (#007ACC): "Bereit | Kein Projekt"     │
└─────────────────────────────────────────────────────┘
```

- **Sidebar:** Fest 200px, immer sichtbar, `#2D2D30` Hintergrund
- **Content:** `#1E1E1E` Hintergrund, 20px Margin
- **Statusleiste:** 28px Höhe, `#007ACC` Blau, weißer Text
- **Navigation:** Button-Click → ContentControl wird befüllt

### 0.2 Aktuelle Farben (hardcoded)

| Farbe | Hex | Wo verwendet |
|-------|-----|-------------|
| App-Hintergrund | `#1E1E1E` | Content Area, TreeView, Read-Only Inputs |
| Sidebar/Panels | `#2D2D30` | Sidebar, DataGrid Zeilen |
| Hover | `#3E3E42` | NavButton Hover, Borders |
| Alternating Row | `#333337` | DataGrid Zebrastreifen |
| Akzent Blau | `#007ACC` | Statusleiste, Tab-Unterstrich, Primary Buttons, DataGrid Header |
| Akzent Dunkelblau | `#005A9E` | DataGrid Header Border |
| Text Normal | `#CCCCCC` | Alle Labels, Body-Text |
| Text Sekundär | `#666666` | Pfade, Hilfstexte, inaktive Tabs |
| Text Deaktiviert | `#555555` | Zusatzinfo, Hinweistexte |
| Text Weiß | `White` | Überschriften, Buttons auf Akzent |
| Danger Rot | `#C62828` | Löschen-Button |

### 0.3 Aktuelle Screens

| Screen | Datei | Beschreibung |
|--------|-------|-------------|
| **Shell** | `MainWindow.xaml` | Sidebar + Content + Statusleiste |
| **Einstellungen** | `SettingsView.xaml` | 2-Tab-Seite: Projekte + Ordnerstruktur |
| **Projekt-Dialog** | `ProjectEditDialog.xaml` | 5-Tab-Dialog: Stammdaten, Bauwerk, Beteiligte, Portale+Links, Ordnerstruktur |
| **PlanManager** | (noch leer) | Platzhalter |

### 0.4 Aktuelle Patterns

| Pattern | Umsetzung |
|---------|----------|
| Navigation | Button-Click → ContentControl befüllen |
| Bearbeitung | Modal-Dialog (Window) |
| Tabellen | DataGrid mit Zebrastreifen + Doppelklick |
| Ordnerstruktur | TreeView mit HierarchicalDataTemplate |
| Status-Anzeige | Converter (Farbe + Text) |
| Disabled Features | Button mit IsEnabled="False" + Tooltip |
| Bestätigung | MessageBox bei Löschen |
| Feedback | Statusleiste (Text unten) |

### 0.5 Delta: Ist-Stand → Guideline-Ziel

| Bereich | Aktuell | Ziel (diese Guideline) | Wann |
|---------|---------|----------------------|------|
| Sidebar | Fest 200px | Immer sichtbar, 220px | ✅ Fast gleich |
| Toolbar | Nicht vorhanden | Modul-spezifische Aktionsleiste | UI-Refresh |
| Breadcrumb | Nicht vorhanden | Oben über Content | UI-Refresh |
| Toasts | Nicht vorhanden | Oben rechts, gestapelt | UI-Refresh |
| Farben | Hardcoded Hex in XAML | Resource Dictionary (Token) | UI-Refresh |
| Schriftart | Segoe UI (WPF Default) | Segoe UI (bleibt) | ✅ Passt |
| Styles | 1 global, Rest lokal | Zentrale Themes/ Ordner | UI-Refresh |
| Validierung | Keine | Live + Zusammenfassung | Schrittweise |
| DataGrid Header | Blau (#007ACC) | Dezent (#2D2D30) | UI-Refresh |
| Labels | Neben Feldern (Pfade) | Über Feldern | Neue Module |
| Animationen | Keine | Fade-In/Out, Slide | UI-Refresh |
| Screen States | Nur Happy Path | 5 Pflicht-Zustände | Ab sofort |
| Suche/Filter | Nicht vorhanden | Standard definiert | Bei Bedarf |

---

## 1. Projektkontext

### 1.1 Zielgruppe

| Nutzer | Rolle | Technisch | Nutzung |
|--------|-------|----------|---------|
| **Primär** | Polier / Bauleiter (Steiermark, AT) | Digital versiert, kein Programmierer | Konfiguriert, lernt an, nutzt alle Module |
| **Sekundär** | Kollegen (andere Poliere, Techniker) | Weniger versiert | Nutzen das Ergebnis, GUI muss selbsterklärend sein |
| **Indirekt** | Lohnbüro, Auftraggeber | Kein BPM-Zugang | Bekommen Exporte (Excel, PDF) |

### 1.2 Nutzungskontext

- **Geräte:** PC (Büro, Hauptarbeitsplatz) + Laptop (Baustelle)
- **Bildschirme:** Full HD (1920×1080) bis 4K — **Optimiert ab 1920×1080, unterstützt ab 1366×768**
- **Eingabe:** Maus + Tastatur (kein Touch für Desktop-App)
- **Umgebung:** Büro (ruhig, großer Monitor) und Baucontainer (Laptop)
- **Offline:** App muss komplett ohne Internet funktionieren

### 1.3 Design-Philosophie

```
Klarheit vor Cleverness.
Konsistenz vor Kreativität.
Daten vor Dekoration.
Effizienz vor Ästhetik.
```

BPM ist ein Arbeitswerkzeug, keine Show-App. Jeder Pixel muss einem Zweck dienen.

---

## 2. Design-Prinzipien

Diese 9 Regeln gelten für ALLE Module, ALLE Screens, ALLE Entscheidungen:

### P1: Consistency First
Gleiche Aktionen sehen überall gleich aus. Ein "Speichern"-Button ist in jedem Dialog an derselben Stelle, in derselben Farbe, mit demselben Text.

### P2: One Primary Action Per Context
Pro sichtbarem Kontext genau EINE Hauptaktion die sofort erkennbar ist. Alles andere ist sekundär. Konkret: 1 Primary pro Seite (Hauptansicht), 1 pro Dialog, 1 pro Wizard-Schritt.

### P3: Progressive Disclosure
Zeige nur was jetzt relevant ist. Erweiterte Optionen verstecken sich hinter "Erweitert" oder in Tabs.

### P4: Forgiving Design
Jede destruktive Aktion (Löschen, Überschreiben) hat einen Bestätigungsdialog UND eine Undo-Möglichkeit.

### P5: Data First
BPM ist datenlastig. Tabellen, Listen, Zahlen — das ist der Kern. Dekorative Elemente dürfen die Daten nicht verdrängen.

### P6: Feedback Always
Jede Aktion bekommt sofort Feedback. Speichern → "Gespeichert ✓". Fehler → rote Meldung. Laden → Spinner.

### P7: German First
Alle Texte in Deutsch. Fachbegriffe aus dem Bau verwenden (Geschoß nicht Stockwerk, Polier nicht Vorarbeiter).

### P8: Offline Aware
Keine Funktion darf stillschweigend scheitern weil kein Internet da ist. Wenn Internet nötig → klar anzeigen.

### P9: Module müssen sich gleich anfühlen
Jedes neue Modul folgt denselben Layout-Patterns, Tabellen-Styles und Dialog-Strukturen. Ein User der die Einstellungen kennt, muss sich im PlanManager sofort zurechtfinden.

---

## 3. Farben

### 3.1 Dark Theme (Standard)

#### Hintergrund-Stufen

| Token | Hex | Verwendung |
|-------|-----|-----------|
| `bg-base` | `#1E1E1E` | App-Hintergrund |
| `bg-surface` | `#252526` | Sidebar, Panels, Karten |
| `bg-elevated` | `#2D2D30` | Dialoge, Dropdowns, Tooltips |
| `bg-hover` | `#37373D` | Hover-Zustand in Listen/Tabellen |
| `bg-active` | `#04395E` | Aktive/Selektierte Zeile (blau-getönt) |
| `bg-input` | `#3C3C3C` | Input-Felder, TextBoxen |

#### Text

| Token | Hex | Kontrast auf bg-base | Verwendung |
|-------|-----|---------------------|-----------|
| `text-primary` | `#CCCCCC` | 10.5:1 ✅ AAA | Normaler Text |
| `text-secondary` | `#858585` | 4.6:1 ✅ AA | Labels, Platzhalter |
| `text-bright` | `#FFFFFF` | 14.7:1 ✅ AAA | Überschriften, aktives Menü |
| `text-on-accent` | `#FFFFFF` | — | Text auf Akzentfarbe |

#### Akzentfarben

| Token | Hex | Verwendung |
|-------|-----|-----------|
| `accent-primary` | `#0078D4` | Primäre Buttons, aktive Sidebar-Items, Links, Focus-Ring |
| `accent-hover` | `#1A8AD4` | Hover-Zustand auf Akzent |
| `accent-pressed` | `#005A9E` | Pressed/Active-Zustand |

#### Semantische Farben

| Token | Hex | Verwendung |
|-------|-----|-----------|
| `success` | `#4EC94E` | Erfolg, ✅ Status |
| `warning` | `#F0AD4E` | Warnung, ⚠️ Status |
| `error` | `#F44747` | Fehler, ❌ Status, Validierungsfehler |
| `info` | `#3794FF` | Information, Hinweise |

#### Rand & Trenner

| Token | Hex | Verwendung |
|-------|-----|-----------|
| `border-default` | `#3C3C3C` | Standard-Rahmen |
| `border-focus` | `#0078D4` | Focus-Ring auf Inputs |
| `border-subtle` | `#2D2D30` | Dezente Trennlinien |

#### Status-Farben

| Status | Farbe | Hex |
|--------|-------|-----|
| Aktiv / In Arbeit | 🟢 Grün | `#4EC94E` |
| Abgeschlossen | 🔵 Blau | `#0078D4` |
| Geplant / Offen | ⚪ Grau | `#858585` |
| Überfällig / Fehler | 🔴 Rot | `#F44747` |
| Warnung | 🟡 Gelb | `#F0AD4E` |

### 3.2 Light Theme (später)

Inverse Variante. Farb-Token bleiben gleich, nur Werte ändern sich.

### 3.3 Farbregeln

- **Maximal 2 Akzentfarben** gleichzeitig auf einem Screen
- **Rot nur für Fehler und destruktive Aktionen**
- **Grün nur für Erfolg**
- **Gelb nur für Warnungen**
- **Akzentblau für interaktive Elemente**
- **Keine bunten Hintergründe**

---

## 4. Typografie

### 4.1 Schriftart

**Segoe UI** — Windows-Standard, für WPF-Rendering optimiert, auf jedem Windows-Rechner vorhanden.

### 4.2 Schriftgrößen

| Token | Größe | Gewicht | Verwendung |
|-------|-------|---------|-----------|
| `heading-1` | 24px | SemiBold (600) | Seitenüberschriften |
| `heading-2` | 18px | SemiBold (600) | Abschnittsüberschriften |
| `heading-3` | 15px | Medium (500) | Gruppenüberschriften, Dialog-Titel |
| `body` | 13px | Regular (400) | Standard-Text, Tabellenzellen |
| `body-bold` | 13px | SemiBold (600) | Hervorgehobene Werte, Spaltenüberschriften |
| `label` | 12px | Medium (500) | Feld-Labels, Tabs, Sidebar-Einträge |
| `small` | 11px | Regular (400) | Statusleiste, Tooltips |
| `caption` | 10px | Regular (400) | Fußnoten, Version |

### 4.3 Typografie-Regeln

- **Zeilenhöhe:** 1.4× Schriftgröße
- **Maximal 3 verschiedene Schriftgrößen pro Screen**
- **Kein Unterstreichen** außer bei Links
- **Kein Kursiv** außer bei Platzhalter-Text
- **VERSALIEN** nur für Status-Badges

---

## 5. Spacing & Grid

### 5.1 Basis-Einheit

**8px Grid.** Alle Abstände sind Vielfache von 8px (oder 4px für feine Abstände).

| Token | Wert | Verwendung |
|-------|------|-----------|
| `space-xs` | 4px | Innerhalb kompakter Elemente |
| `space-sm` | 8px | Zwischen eng zusammengehörenden Elementen |
| `space-md` | 16px | Standard-Abstand |
| `space-lg` | 24px | Zwischen Abschnitten/Gruppen |
| `space-xl` | 32px | Zwischen großen Bereichen |
| `space-xxl` | 48px | Seitenränder |

### 5.2 Ecken

| Element | Radius |
|---------|--------|
| Buttons, Input-Felder, Karten/Panels | 3px |
| Dialoge/Modals, Tooltips | 4px |
| Badges/Tags | 2px |

---

## 6. Layout & Navigation

### 6.1 Grundstruktur

```
┌──────────────────────────────────────────────────────────────────┐
│  Breadcrumb: Einstellungen > Projekte > ÖWG Dobl                │
├────────┬─────────────────────────────────────────────────────────┤
│ S      │  ╔══════════════════════════════════════════════════╗   │
│ I      │  ║  TOOLBAR (modul-spezifische Aktionen)           ║   │
│ D      │  ║  [Neues Projekt] [Bearbeiten] [Löschen] │ [Export] ║ │
│ E      │  ╚══════════════════════════════════════════════════╝   │
│ B      │                                                         │
│ A      │  ┌─────────────────────────────────────────────────┐   │
│ R      │  │              CONTENT AREA                       │   │
│        │  │              (Tabellen, Formulare, etc.)        │   │
│ (220px │  │                                                 │   │
│  immer │  └─────────────────────────────────────────────────┘   │
│  sicht │                                                         │
│  bar)  │                                                         │
├────────┴─────────────────────────────────────────────────────────┤
│  Statusleiste: DB: bpm.db | Schema v1.5 | Gespeichert ✓ 14:32   │
└──────────────────────────────────────────────────────────────────┘
```

### 6.2 Sidebar

- **Position:** Links
- **Verhalten:** Immer sichtbar, nicht einklappbar
- **Breite:** 220px
- **Hintergrund:** `bg-surface`
- **Inhalt:** Modul-Icons + Text, nur aktive Module
- **Aktives Modul:** `accent-primary` Hintergrund + `text-bright`
- **Icons:** Emoji (provisorisch), Segoe Fluent Icons (geplant)
- **Unten:** User-Name + App-Version

### 6.3 Toolbar

- **Position:** Oben im Content-Bereich (unter Breadcrumb)
- **Höhe:** 40px
- **Hintergrund:** `bg-surface`
- **Stil:** Flache Buttons mit Icons + Text, getrennt durch vertikale Divider
- **Umsetzung:** WPF-native ToolBar oder StackPanel mit Separatoren (kein Third-Party)

### 6.4 Breadcrumb

- **Position:** Ganz oben, über der Toolbar
- **Höhe:** 28px
- **Schrift:** `label` (12px)
- **Klickbar:** Ja — jede Stufe navigiert zurück

### 6.5 Statusleiste

- **Position:** Ganz unten
- **Höhe:** 24px
- **Hintergrund:** `bg-surface`
- **Inhalt links:** DB-Pfad, Schema-Version
- **Inhalt rechts:** Letzte Aktion, Zeitstempel

---

## 7. Screen States (PFLICHTSTANDARD)

**Jeder Screen und jede Datenliste MUSS alle 5 Zustände implementieren.**

| Zustand | Wann | Darstellung |
|---------|------|------------|
| **Loading** | Daten werden geladen | Spinner + "Lade [Datentyp]..." |
| **Data** | Daten vorhanden | Normale Ansicht |
| **Empty** | Keine Daten vorhanden | Hinweis + Handlungsaufforderung |
| **Error** | Laden/Speichern fehlgeschlagen | Fehlermeldung + "Erneut versuchen" |
| **Offline** | Internet nötig, keines vorhanden | Hinweis + was stattdessen möglich ist |
| **Dirty** 🎯 | Ungespeicherte Änderungen | Visueller Marker (z.B. `*` im Titel), Abbrechen-Dialog bei Escape/Schließen: "Änderungen verwerfen?" |
| **Read-only** 🎯 | Bearbeitung nicht möglich (Lock, Modul deaktiviert, Rolle) | Dezent abgedunkelt, Felder disabled, Hinweisleiste "Nur-Lesen-Modus" |
| **Partial Success** 🎯 | Aktion teilweise erfolgreich | Ergebnisliste mit Status pro Eintrag (✅/⚠️/❌), Zusammenfassungszeile: "7 von 10 importiert" |

🎯 = Zielstandard, kommt mit UI-Refresh oder bei Bedarf im jeweiligen Modul.

### Regeln

- **Loading:** Unter 200ms nichts. 200ms–2s Spinner. Über 2s Fortschrittsbalken + Abbrechen.
- **Empty:** Immer mit Handlungsaufforderung (Button).
- **Error:** Was passiert + was User tun kann. Keine technischen Details.
- **Offline:** Nur für Features die Internet brauchen. Offline-fähige Features zeigen das nie.

---

## 8. Komponenten

### 8.1 Buttons

| Variante | Hintergrund | Verwendung |
|----------|------------|-----------|
| **Primary** | `accent-primary` | Hauptaktion pro Screen |
| **Secondary** | `bg-elevated` | Sekundäre Aktionen |
| **Danger** | `error` | Destruktive Aktionen |
| **Ghost** | Transparent | Tertiäre/Inline-Aktionen |
| **Link** | Transparent, `accent-primary` Text | Navigation |

**Regeln:** Max. 1 Primary pro sichtbarem Kontext (Seite, Dialog, Wizard-Schritt — siehe P2). Primary rechts in Dialogen. Mindesthöhe 32px.

### 8.2 Input-Felder

- **Labels:** Über dem Feld (Standard)
- **Pflichtfelder:** Label endet mit `*`
- **Validierung:** Live beim Verlassen + Zusammenfassung beim Speichern
- **Fehlermeldung:** Unter dem Feld, `error`-Farbe, `small`-Schrift

### 8.3 Tabellen / DataGrid

- **Header:** `bg-surface`, `body-bold`, sticky
- **Hover:** `bg-hover` (ganze Zeile)
- **Selektiert:** `bg-active`
- **Zebrastreifen:** Erlaubt (Entwickler entscheidet pro Tabelle)
- **Sortierbar:** Klick auf Header
- **Zeilenhöhe:** 36px
- **Doppelklick:** Öffnet Dialog
- **Empty State:** Pflicht (Kapitel 7)

### 8.4 Dialoge / Modals

- **Overlay:** 50% schwarz
- **Animation:** Fade-In 200ms + Scale 0.95→1.0
- **Buttons:** Unten rechts, Primary ganz rechts
- **Schließen:** [✕] oder Escape (mit Dirty-State-Check bei Formulardialogen)
- **Overlay-Klick:** Nur bei Info-/Bestätigungsdialogen erlaubt. Bei Bearbeitungsdialogen mit Formularen: KEIN Overlay-Klick (Risiko Datenverlust)
- **Keine verschachtelten Dialoge**

### 8.5 Toast-Benachrichtigungen

- **Position:** Oben rechts
- **Dauer:** Erfolg 3s, Fehler bleibt
- **Max. 3 gestapelt**

### 8.6 Tabs

- Unterstrich-Tabs, aktiv = `accent-primary` Linie unten (2px)

### 8.7 Icons

**Aktuell:** Emoji — **PROVISORISCH.** Werden bei UI-Refresh durch Segoe Fluent Icons ersetzt. Neue Module sollten bereits mit Fluent Icons planen.

---

## 9. Master-Detail Pattern

| Situation | Pattern |
|-----------|---------|
| Komplexes Objekt bearbeiten | Dialog (Modal) |
| Einfaches Objekt erstellen | Dialog (kleiner) |
| Objekt ansehen + navigieren | Seitenwechsel im Content |
| Schnelle Änderung in Zeile | Inline-Editing |

**Regeln:** Nie beides mischen im selben Modul. Dialoge für Erstellen/Bearbeiten (Standard in BPM). Inline-Editing nur für einzelne Felder.

---

## 10. Such- und Filterstandards

- **Suchfeld:** Oben rechts, 250–350px, Platzhalter "Suche in [Datentyp]..."
- **Live-Suche:** 300ms Debounce
- **Filter:** ComboBox-Dropdowns neben Suchfeld
- **Reset:** "Filter zurücksetzen" Link wenn aktiv
- **Aktive Filter:** Immer sichtbar

---

## 11. Datenformate (Österreich)

| Format | Regel | Beispiel |
|--------|-------|---------|
| Datum (Anzeige) | TT.MM.JJJJ | 05.11.2025 |
| Datum (DB) | YYYY-MM-DD | 2025-11-05 |
| Dezimalzeichen | Komma | 198,50 |
| Tausender | Punkt | 1.250 |
| Einheiten | Nach Zahl mit Leerzeichen | 198 m² |
| Währung | Nach Betrag | 45,00 € |
| Prozent | Komma + % | 15,0 % |

**Gängige Bau-Einheiten:** m², m³, lfm, to, kg, Stk, Pal, Ah, h, h/m², m²/Ah

**CultureInfo:** `de-AT` als Standard.

---

## 12. Formulare

- **Labels über dem Feld** (Standard)
- **2-Spalten-Layout** bei vielen Feldern
- **Logische Gruppierung** mit Überschriften
- **Pflichtfelder** mit `*`
- **Validierung:** Live beim Verlassen + Zusammenfassung beim Speichern
- **Mehrtab-Validierung:** Bei Speichern-Versuch werden ALLE Tabs validiert, nicht nur der aktive. Fehlerhafte Tabs bekommen einen Fehler-Marker (⚠️) im Tab-Header. Fehlerzusammenfassung oben im Dialog: "2 Fehler: Tab Stammdaten — Projektname fehlt". Klick auf Fehler → wechselt zum betroffenen Tab und fokussiert das Feld. Speichern blockiert solange Pflichtfehler vorhanden.

---

## 13. UX-Wording

- **Komplett Deutsch** (Ausnahmen: Dashboard, OK)
- **Duzen** (nicht Siezen)
- **Bau-Fachsprache** (Geschoß, Polier, Bewehrung, Schalung, RDOK, LV)
- **Fehlermeldungen:** Was passiert + was User tun kann
- **Sachlich, direkt, keine Ausrufezeichen**

---

## 14. Interaktionsregeln

- **Einfachklick:** Selektieren
- **Doppelklick:** Bearbeiten (Dialog)
- **Rechtsklick:** Kontextmenü
- **Escape:** Dialog schließen (Pflicht)
- **Enter:** Primary-Button ausführen (Pflicht)
- **Animationen:** Max. 300ms, ease-out für Öffnen, ease-in für Schließen

---

## 15. Accessibility

### 15.1 Kontrast (WCAG AA ≥ 4.5:1)

| Kombination | Kontrast | Status |
|------------|---------|--------|
| `text-primary` auf `bg-base` | 10.5:1 | ✅ AAA |
| `text-secondary` auf `bg-base` | 4.6:1 | ✅ AA |
| `text-on-accent` auf `accent-primary` | 4.7:1 | ✅ AA |

Bei neuen Farbkombinationen: Kontrast prüfen bevor verwendet.

### 15.2 Fokus & Navigation

- **Tab-Reihenfolge:** Logisch oben-links → unten-rechts
- **Focus-Ring:** 2px `accent-primary`
- **AutomationProperties.Name** auf Icon-only Buttons

### 15.3 DPI-Skalierung

100%, 125%, 150%, 175%, 200% müssen funktionieren.

---

## 16. Do's & Don'ts

### ✅ DO

- Einheitliche Abstände (8px-Grid)
- Primary Button rechts in Dialogen
- Leere Zustände abfangen (Kapitel 7)
- Feedback bei jeder Aktion
- Labels über Feldern
- Deutsch für alles
- Farb-Token verwenden
- 5 Screen States implementieren
- Tooltips für Icon-only Buttons

### ❌ DON'T

- Mehrere Primary Buttons auf einem Screen
- Farben direkt in XAML ohne Token
- MessageBox.Show() verwenden
- Horizontalen Scroll auf Seitenebene
- Rot für nicht-destruktive Aktionen
- Animationen über 300ms
- Englische Begriffe in der GUI
- Verschachtelte Dialoge
- Dekorative Elemente die Daten verdrängen

---

## 17. Responsive Verhalten

- **Optimiert ab:** 1920 × 1080 (volle Breite für 2-Spalten-Layouts)
- **Unterstützt ab:** 1366 × 768 (Scrollbereiche erlaubt, 1-Spalten-Fallback bei engen Dialogen)
- **Unterstützt bis:** 4K (3840 × 2160)
- **DPI-Skalierung:** 100%–200%
- **Regel:** Kein Feature darf bei 1366×768 unbenutzbar sein. Sidebar darf bei <1600px auf Icon-only wechseln (🎯 Zielstandard).

---

## 18. Feedback-Matrix (PFLICHTSTANDARD)

Welches Feedback-Medium für welchen Anlass. Ersetzt pauschal `MessageBox.Show()`.

| Feedback-Typ | Medium | Wann | Beispiel |
|---|---|---|---|
| **Feldvalidierung** | Inline (rote Border + Text unter Feld) | Sofort bei Verlassen des Feldes | "Projektname darf nicht leer sein" |
| **Erfolgsmeldung** | Toast 🎯 (unten rechts, 3s) / Statusleiste (aktuell) | Nach erfolgreicher Aktion | "Projekt gespeichert ✅" |
| **Destruktive Bestätigung** | Modal-Dialog (zentriert, nicht wegklickbar) | Vor Lösch-/Überschreib-Aktionen | "Projekt 'ÖWG-Dobl' wirklich löschen?" |
| **Kritischer Fehler** | Error-Dialog (Modal, mit Details-Expander) | Bei unerwarteten Fehlern | "Datenbank konnte nicht geladen werden" |
| **Passive Info** | Statusleiste (unten, persistent) | Hintergrundstatus | "3 Pläne im Eingang" |
| **Modus-Hinweis** | Banner 🎯 (oben im Content, persistent) | Systemzustand | "Nur-Lesen-Modus" / "Offline" |

**Verboten:**
- `MessageBox.Show()` für alles
- Technische Fehlermeldungen in User-sichtbaren Dialogen
- Toasts für kritische/destruktive Aktionen
- Inline-Meldungen für systemweite Zustände

🎯 = Zielstandard nach UI-Refresh. Bis dahin: Statusleiste für Erfolg, MessageBox-Ersatz per Modal-Dialog.

---

*Dieses Dokument ist der verbindliche Standard für alle UI-Entscheidungen in BPM. Abweichungen müssen dokumentiert und begründet werden.*

*Änderungen v2.0 → v2.1 (04.04.2026):*
- *Mindestauflösung entschärft: "optimiert 1920×1080, unterstützt 1366×768"*
- *Primary-Action-Regel harmonisiert: "pro sichtbarem Kontext" statt "pro Screen"*
- *3 neue Screen States: Dirty, Read-only, Partial Success (als 🎯 Zielstandard)*
- *Overlay-Klick bei Formulardialogen entfernt*
- *Validierungszusammenfassung für Mehrtab-Dialoge ergänzt (Kap. 12)*
- *Feedback-Matrix als neues Kap. 18 (Ersatz für MessageBox.Show())*
- *Sprache intern vs. App-UI klargestellt*
- *Verwandte Dokumente + Querverweise ergänzt*