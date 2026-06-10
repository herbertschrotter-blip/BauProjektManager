# PlanManager — Sitemap

Quelle der Wahrheit für Navigationskanten und Fenster-Hierarchie im PlanManager-Mockup-Modul.
Wird vor jedem neuen Mockup gelesen und nach Mockup-Änderungen aktualisiert.

**Namens-Konvention (BPM):**
- Ordner-pro-Fenster, flache Nummerierung (`01_`, `02_`, …) im Modul-Wurzelverzeichnis
- Dateien innerhalb des Fenster-Ordners: `NN_Variante.html` (Tab, Schritt, Sub-Ansicht)
- Hierarchie/User-Journey wird **nicht** in Ordnernamen kodiert, sondern in der `Aufrufer`-Spalte unten

**Status-Legende:**
- ✅ aktiv — Quelle hat `onclick`, Ziel existiert
- ⚪ geplant — Ziel existiert, `onclick` im Quell-HTML fehlt noch
- 🟡 tot — Ziel-Mockup fehlt, Aufrufer hat `alert('Mockup folgt: X')`
- ❌ kaputt — Pfad-Inkonsistenz

## Fenster

| Ordner | Aufrufer | Trigger | Status | Datei(en) |
|---|---|---|---|---|
| 01_Projektuebersicht | (Sidebar / App-Start) | – | ✅ aktiv | 01_Projektuebersicht.html |
| 02_Projektdetail | 01_Projektuebersicht | Projekt-Karte (Klick) | 🟡 in Arbeit | 01_Profile.html, 02_ManuellSortieren.html (Radial-Erfassung BPM-111.01; 03_Sync=BPM-005 fehlt) |
| 03_ProfilWizard | 02_Projektdetail (Profile-Tab) | "✎ Profil" / "+ Neues Profil anlernen" | ✅ aktiv | 01_Datei.html, 02_Segmente.html, 02_Segmente_NeuerCustom.html (Inline-Popover BPM-108), 03_IndexSource.html, 04_Zielordner.html, 05_Erkennung.html |
| 04_FeldtypManager | 03_ProfilWizard/02_Segmente.html | "⚙ Segmenttypen verwalten…" | ✅ aktiv | 01_Uebersicht.html (CRUD + Gruppen + Drag-Reorder + Farbpalette + Built-in Rollen-Read-only, BPM-108) |
| _Archiv | – | – | – | 00_Gesamtuebersicht.html (alt, nicht navigierbar) |

## Navigationskanten

### Intern (PlanManager)

| Quelle | Ziel | Trigger | Status |
|---|---|---|---|
| 01_Projektuebersicht/01_Projektuebersicht.html | 02_Projektdetail/01_Profile.html | Projekt-Karte (Klick) | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 01_Projektuebersicht/01_Projektuebersicht.html | ← Zurück-Pfeil | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 01_Projektuebersicht/01_Projektuebersicht.html | Sidebar "📁 PlanManager" | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 02_Projektdetail/02_ManuellSortieren.html | Tab "Manuell sortieren" | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 02_Projektdetail/03_Sync.html | Tab "Sync" | 🟡 tot |
| 02_Projektdetail/02_ManuellSortieren.html | 02_Projektdetail/01_Profile.html | Tab "Profile" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren.html | 01_Projektuebersicht/01_Projektuebersicht.html | ← Zurück-Pfeil | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren.html | 02_Projektdetail/03_Sync.html | Tab "Sync" | 🟡 tot |
| 02_Projektdetail/01_Profile.html | 03_ProfilWizard/01_Datei.html | "+ Neues Profil anlernen" | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 03_ProfilWizard/01_Datei.html | "✎ Profil" pro Profil-Karte (4×) | ✅ aktiv |
| 03_ProfilWizard/01_Datei.html | 03_ProfilWizard/02_Segmente.html | "Weiter →" | ✅ aktiv |
| 03_ProfilWizard/02_Segmente.html | 03_ProfilWizard/01_Datei.html | "← Zurück" | ✅ aktiv |
| 03_ProfilWizard/02_Segmente.html | 03_ProfilWizard/03_IndexSource.html | "Weiter →" | ✅ aktiv |
| 03_ProfilWizard/03_IndexSource.html | 03_ProfilWizard/02_Segmente.html | "← Zurück" | ✅ aktiv |
| 03_ProfilWizard/03_IndexSource.html | 03_ProfilWizard/04_Zielordner.html | "Weiter →" | ✅ aktiv |
| 03_ProfilWizard/04_Zielordner.html | 03_ProfilWizard/03_IndexSource.html | "← Zurück" | ✅ aktiv |
| 03_ProfilWizard/04_Zielordner.html | 03_ProfilWizard/05_Erkennung.html | "Weiter →" | ✅ aktiv |
| 03_ProfilWizard/05_Erkennung.html | 03_ProfilWizard/04_Zielordner.html | "← Zurück" | ✅ aktiv |
| 03_ProfilWizard/05_Erkennung.html | 02_Projektdetail/01_Profile.html | "Profil speichern" | ✅ aktiv |
| 03_ProfilWizard/01-05_*.html | 02_Projektdetail/01_Profile.html | "Abbrechen" / rotes X (alle 5 Schritte) | ✅ aktiv |
| 03_ProfilWizard/05_Erkennung.html | 03_ProfilWizard/05_Erkennung_Regex.html | Modus-Pill "Regex" | 🟡 tot |
| 03_ProfilWizard/02_Segmente.html | 04_FeldtypManager/01_Uebersicht.html | "⚙ Segmenttypen verwalten…" Link | ✅ aktiv |
| 03_ProfilWizard/02_Segmente.html | 03_ProfilWizard/02_Segmente_NeuerCustom.html | Klick auf "+ Eigenes"-Chip | 🟡 tot (Klick noch nicht verdrahtet im 02_Segmente.html) |
| 03_ProfilWizard/02_Segmente_NeuerCustom.html | 03_ProfilWizard/02_Segmente.html | "Abbrechen" / "Anlegen" (Popover schließt) | ✅ aktiv |
| 03_ProfilWizard/02_Segmente_NeuerCustom.html | 04_FeldtypManager/01_Uebersicht.html | "Segmenttypen-Manager" Link im Popover-Hint | ✅ aktiv |
| 04_FeldtypManager/01_Uebersicht.html | 03_ProfilWizard/02_Segmente.html | "Schließen" / rotes X | ✅ aktiv |

### Extern (Cross-Modul nach Settings)

| Quelle | Ziel | Trigger | Status |
|---|---|---|---|
| 01_Projektuebersicht/01_Projektuebersicht.html | ../Settings/01_Einstellungen/01_Allgemein.html | Sidebar "⚙ Einstellungen" | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | ../Settings/01_Einstellungen/01_Allgemein.html | Sidebar "⚙ Einstellungen" | ✅ aktiv |

## Offene Punkte (Backlog-Referenz)

- **BPM-004 / BPM-111.01** — Mockup `02_Projektdetail/02_ManuellSortieren.html` — ✅ erledigt (Radial-Erfassung nach ADR-059, Teil 43; alte Listenvariante in `_Archiv/`. Spezifikation als Kommentar-Block im HTML-Header: Hold/Dwell-Timing, typabhängige Ringe, + Neu… je Ebene, Pending/Update/Archiv-Tab, angedocktes Vorschau-Fenster)
- **BPM-005** — Mockup `02_Projektdetail/03_Sync.html`
- **BPM-080.01–04** — Mockups `03_ProfilWizard/01_Datei.html` bis `04_Zielordner.html` — ✅ erledigt
- **BPM-007.02** — Mockup `03_ProfilWizard/05_Erkennung.html` (Toggle Segmente/Regex) — ✅ Segmente-Modus erledigt, Regex-Modus-Variante (`05_Erkennung_Regex.html`) noch offen
- **BPM-080.05** — WPF-Umsetzung des kompletten Wizards
- **BPM-108** — Segmenttyp-Verwaltung DB-basiert (Architektur abgeschlossen via CGR-2026-05-12-segmenttyp-architektur). Mockups: `04_FeldtypManager/01_Uebersicht.html` (Manager mit Built-in Fachrolle + Token read-only) + `03_ProfilWizard/02_Segmente_NeuerCustom.html` (Inline-Popover). WPF-Umsetzung: 3-Phasen-Plan (Phase A Domain → B Profilformat v4 → C Wizard/UI)
