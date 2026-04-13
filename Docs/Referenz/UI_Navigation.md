---
doc_id: ui-navigation
doc_type: reference
authority: secondary
status: active
owner: herbert
topics: [navigation, sidebar, toolbar, screen-hierarchie, breadcrumb, shell]
read_when: [neuer-screen, navigation-ändern, sidebar-eintrag, toolbar-frage]
related_docs: [ui-ux-guidelines, wpf-ui-architecture, architektur]
related_code: [src/BauProjektManager.App/MainWindow.xaml]
supersedes: []
---

## AI-Quickload
- Zweck: Shell-Aufbau, Sidebar-/Toolbar-Regeln und Screen-Hierarchie für die BPM-Navigation
- Autorität: secondary (UI_UX_Guidelines.md = source_of_truth für Design)
- Lesen wenn: Neuer Screen, Navigation ändern, Sidebar-Eintrag, Toolbar-Frage
- Nicht zuständig für: Design-Tokens (→ UI_UX_Guidelines.md), XAML-Umsetzung (→ WPF_UI_Architecture.md)
- Kapitel:
  - 1. Shell-Aufbau
  - 2. Sidebar
  - 3. Toolbar
  - 4. Screen-Hierarchie
  - 5. Navigationsregeln
  - 6. Breadcrumb
  - 7. Offene Punkte
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Sidebar links (56px) für Modul-Wechsel — keine Top-Level-Toolbar dafür
  - Toolbar nur innerhalb eines Moduls wenn es mehrere Seiten hat
  - Modale Dialoge (Import, Wizard) überlagern das Hauptfenster

---

# BauProjektManager — UI-Navigation und Screen-Hierarchie

**Version:** 1.0
**Datum:** 10.04.2026
**Status:** Verbindlich ab v0.25.0

---

## 1. Shell-Aufbau

```
┌──────────────────────────────────────────────────┐
│ Sidebar │ Toolbar (nur wenn Modul sie braucht)   │
│  (56px) │────────────────────────────────────────│
│         │                                        │
│  [Mod1] │  Content-Bereich                       │
│  [Mod2] │  (Hauptfenster oder Modal-Dialog)      │
│  [Mod3] │                                        │
│   ...   │                                        │
│         │                                        │
│  v0.25  │────────────────────────────────────────│
│         │ Statusleiste (22px)                    │
└──────────────────────────────────────────────────┘
```

## 2. Sidebar (immer sichtbar, 56px breit)

Vertikale Icon-Leiste links mit Modul-Icons + Text:

| Position | Icon | Text | Badge | Status |
|----------|------|------|-------|--------|
| 1 | 📁 | Pläne | Anzahl Dateien im Eingang (amber) | V1 |
| 2 | ⚙ | Einstell. | — | V1 (existiert) |
| 3 | — | Separator | — | — |
| 4 | 📷 | Fotos | — | Post-V1 (ausgegraut) |
| 5 | ⏲ | Zeiten | — | Post-V1 (ausgegraut) |
| 6 | 📖 | Tagebuch | — | Post-V1 (ausgegraut) |
| unten | — | vX.Y | — | Versionsnummer |

**Design:**
- Breite: 56px fest
- Hintergrund: BpmBgSurface (#252526)
- Aktives Modul: BpmBgActive (#04395E) + BpmAccent Text
- Hover: BpmBgHover (#37373D)
- Badges: BpmWarning (#F0AD4E) Hintergrund, 7px Font, Pill-Form, oben rechts am Icon
- Post-V1 Module: opacity 0.35, nicht klickbar

**Klick auf Sidebar-Icon:** Wechselt das Modul im Content-Bereich.

---

## 3. Toolbar (kontextuell, nur wenn Modul sie braucht)

Die Toolbar erscheint NICHT für Modul-Wechsel. Sie erscheint nur innerhalb eines Moduls
wenn dieses Modul mehrere Seiten hat und Navigation/Aktionen braucht.

### Wann sichtbar:

| Screen | Toolbar? | Inhalt |
|--------|----------|--------|
| PlanManager Projektliste | Minimal | Nur Titel „PlanManager" |
| PlanManager Projektdetail | Ja | ← Zurück, Projektname, ↩ Rückgängig, [Import starten] |
| Einstellungen | Minimal | Nur Titel „Einstellungen" |
| Modal-Dialoge | Nein | Modal hat eigenen Header mit Titel + Buttons |

**Design:**
- Höhe: 34px
- Hintergrund: BpmBgSurface (#252526)
- Zurück-Button: Text „←", kein Icon, BpmTextSecondary, hover BpmBgHover
- Primary Action: BpmButtonPrimary (max. 1 pro Toolbar, P2-Regel)

---

## 4. Screen-Hierarchie

### 4.1 Hauptfenster (3 Stück)

```
SIDEBAR                CONTENT
────────               ───────────────────
[Pläne]    ──────►    Projektliste (PlanManagerView)
                          │
                          │ Klick auf Projekt
                          ▼
                      Projektdetail (ProjectDetailView)
                      ├── Tab: Profile
                      ├── Tab: Manuell sortieren
                      └── Tab: Sync

[Einstell.] ──────►   Einstellungen (SettingsView)
```

### 4.2 Modal-Dialoge (2 Stück)

Öffnen sich über dem aktiven Hauptfenster. Dunkler Overlay-Hintergrund.

```
Projektdetail
    │
    ├── [Import starten] ──►  Import-Vorschau (ImportPreviewDialog)
    │                          Zusammenfassungszeile, DataGrid, 9 Status
    │                          Buttons: Abbrechen + Import ausführen
    │
    ├── [✎ Profil]      ──►  Profil-Wizard (ProfileWizardDialog)
    │                          5 Schritte mit Progress-Bar:
    │                          1. Datei auswählen + Parsen
    │                          2. Segmente zuweisen
    │                          3. IndexSource + Vergleichslogik
    │                          4. Zielordner + Unterordner-Ebenen
    │                          5. Erkennung (prefix/contains + Test)
    │                          Buttons: Abbrechen/Zurück + Weiter/Speichern
    │
    └── [+ Neuer Typ]   ──►  Gleicher Wizard (leer statt vorausgefüllt)
```

### 4.3 Navigation zusammengefasst

| Von | Nach | Wie |
|-----|------|-----|
| Sidebar | Projektliste | Sidebar-Icon „Pläne" |
| Sidebar | Einstellungen | Sidebar-Icon „Einstell." |
| Projektliste | Projektdetail | Klick auf Projekt-Zeile |
| Projektdetail | Projektliste | ← Zurück in Toolbar |
| Projektdetail | Import-Vorschau | Button „Import starten" (Modal) |
| Projektdetail | Profil-Wizard | Button „✎ Profil" oder „+ Neuer Typ" (Modal) |
| Import-Vorschau | Projektdetail | Button „Abbrechen" (Modal schließen) |
| Profil-Wizard | Projektdetail | Button „Abbrechen" oder „Speichern" (Modal schließen) |

---

## 5. Tabs im Projektdetail

Das Projektdetail hat 3 Tabs (WPF TabControl):

| Tab | Name | Inhalt |
|-----|------|--------|
| 0 | Profile | Dokumenttypen gruppiert nach Zielordner, ✎ Profil-Button |
| 1 | Manuell sortieren | Links: nicht zugeordnete Dateien, Rechts: Zuweisungsformular |
| 2 | Sync | Sync-Paare, Einstellungen, Sync-Log |

**Eingang-Banner:** Über den Tabs, immer sichtbar wenn Dateien im Eingang.
Zeigt auch Sync-Herkunft: „15 im Eingang · davon 3 vom Server-Sync"

---

## 6. Statusleiste (immer sichtbar, 22px)

Am unteren Rand der Shell. Zeigt kontextabhängig:

| Screen | Links | Rechts |
|--------|-------|--------|
| Projektliste | bpm.db · 3 Projekte | Bereit |
| Projektdetail | ÖWG · 6 Profile · 119 Dok. | 15 im Eingang |
| Einstellungen | Einstellungen | — |
| Import-Vorschau | Import läuft... | Fortschritt |

---

## 7. Regeln

1. **Sidebar = Modul-Wechsel.** Immer sichtbar, immer gleich.
2. **Toolbar = Seiten-Navigation innerhalb eines Moduls.** Nur wenn nötig.
3. **Tabs = Unter-Ansichten auf derselben Seite.** Kein neues Fenster.
4. **Modals = Temporäre Aufgaben.** Import-Vorschau und Wizard.
5. **Max. 1 Primary Button** pro sichtbarem Kontext (P2-Regel).
6. **Badges in Sidebar** zeigen offene Aufgaben (Dateien im Eingang).
7. **Keine verschachtelten Modals.** Wizard und Import sind eigenständige Dialoge.
8. **Escape schließt Modal** (mit Dirty-Check bei ungespeicherten Änderungen).

---

*Dieses Dokument definiert die verbindliche Navigation für alle BPM-Module.
Neue Module werden als Sidebar-Icon hinzugefügt, nicht als Toolbar-Button.*
