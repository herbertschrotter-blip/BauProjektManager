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
- 🔷 implementiert ohne Mockup — Screen ist im Code umgesetzt, ein Mockup gibt es nicht und wird nicht nachgezogen; der Code gilt als Ist (Audit Review 01, R01-001 E14, entschieden 2026-09-24)
- ❌ kaputt — Pfad-Inkonsistenz

**Seitenliste (ab 2026-11-16):** Wahrheit für Mockups ist `Docs/Mockups/seiten.json` (Nummer, Versionen, Abnahme; erzeugt `SEITENLISTE.md` und `index.html`, Entscheidung Herbert). Diese Sitemap führt nur noch die Kanten der älteren Mockups.

## Fenster

| Ordner | Aufrufer | Trigger | Status | Datei(en) |
|---|---|---|---|---|
| 01_Projektuebersicht | (Sidebar / App-Start) | – | ✅ aktiv | 01_Projektuebersicht.html |
| 02_Projektdetail | 01_Projektuebersicht | Projekt-Karte (Klick) | 🟡 in Arbeit | 01_Profile.html, 02_ManuellSortieren.html (Radial-Erfassung BPM-111.01), 02_ManuellSortieren_Claude.html („Mit Claude sortieren“, Chat-Panel, abgenommen 2026-11-16 von Herbert, Seitenliste 3.4 v1.0; **abgelöst** 2026-11-19 durch `seiten/mit-claude-sortieren.html`, Seitenliste 3.4 v1.3), 04_Explorer.html (BPM-112.06), 05_Plandaten.html (Teil 50; 03_Sync=BPM-005 fehlt) |
| 03_ProfilWizard | 02_Projektdetail (Profile-Tab) | "✎ Profil" / "+ Neues Profil anlernen" | ✅ aktiv | 01_Datei.html, 02_Segmente.html, 02_Segmente_NeuerCustom.html (Inline-Popover BPM-108), 03_IndexSource.html, 04_Zielordner.html, 05_Erkennung.html — **abgelöst** 2026-11-18 durch `seiten/profil-wizard.html` (Seitenliste 3.8, BPM-130, 4 Schritte) |
| 04_FeldtypManager | 03_ProfilWizard/02_Segmente.html | "⚙ Segmenttypen verwalten…" | ✅ aktiv | 01_Uebersicht.html (CRUD + Gruppen + Drag-Reorder + Farbpalette + Built-in Rollen-Read-only, BPM-108; Drag-Reorder post-V1, Audit Review 01, R01-001 E14, entschieden 2026-09-24) |
| – | 02_Projektdetail (Tab "Manuell sortieren") | Öffnen des Tabs bei nicht abgeschlossenem Import (Recovery) | 🔷 implementiert ohne Mockup | – (WPF: `RecoveryDialog.xaml`) |
| – | 02_Projektdetail/04_Explorer.html | Kontextmenü "➔ Verschieben…" (Ordner-Wahl) | 🔷 implementiert ohne Mockup | – (WPF: `FolderPickerDialog.xaml`) |
| – | 02_Projektdetail/02_ManuellSortieren.html | Radial "+ Neu…" je Ebene (Schnellanlage) | 🔷 implementiert ohne Mockup | – (WPF: Dialoge „Neuer Dokumenttyp“ (PromptNewDocumentType) sowie „Neues Bauteil“, „Neue Kategorie“, „Neues Geschoß“ (PromptName) in `ManualCaptureView.xaml.cs`) |
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
| 02_Projektdetail/01_Profile.html | 02_Projektdetail/04_Explorer.html | Tab "Explorer" | ✅ aktiv |
| 02_Projektdetail/01_Profile.html | 02_Projektdetail/05_Plandaten.html | Tab "Plandaten" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren.html | 02_Projektdetail/04_Explorer.html | Tab "Explorer" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren.html | 02_Projektdetail/05_Plandaten.html | Tab "Plandaten" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren.html | 02_Projektdetail/02_ManuellSortieren_Claude.html | Knopf "✦ Mit Claude sortieren" (Hinweiszeile rechts) | ⚪ geplant (der Knopf steht im abgenommenen 02_ManuellSortieren_Claude.html; das ältere Grund-Mockup bleibt unverändert) |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 02_Projektdetail/02_ManuellSortieren.html | Tab "Manuell sortieren" + Sub-Tab "Archiv" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 02_Projektdetail/04_Explorer.html | Tab "Explorer" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 02_Projektdetail/05_Plandaten.html | Tab "Plandaten" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 02_Projektdetail/01_Profile.html | Tab "Profile" | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 01_Projektuebersicht/01_Projektuebersicht.html | ← Zurück-Pfeil | ✅ aktiv |
| 02_Projektdetail/02_ManuellSortieren_Claude.html | 02_Projektdetail/03_Sync.html | Tab "Sync" | 🟡 tot |
| 02_Projektdetail/04_Explorer.html | 02_Projektdetail/02_ManuellSortieren.html | Tab "Manuell sortieren" + Kontextmenü "Im PlanManager anzeigen" | ✅ aktiv |
| 02_Projektdetail/04_Explorer.html | 02_Projektdetail/05_Plandaten.html | Tab "Plandaten" | ✅ aktiv |
| 02_Projektdetail/04_Explorer.html | 02_Projektdetail/01_Profile.html | Tab "Profile" | ✅ aktiv |
| 02_Projektdetail/04_Explorer.html | 01_Projektuebersicht/01_Projektuebersicht.html | ← Zurück-Pfeil + Sidebar | ✅ aktiv |
| 02_Projektdetail/04_Explorer.html | 02_Projektdetail/03_Sync.html | Tab "Sync" | 🟡 tot |
| 02_Projektdetail/05_Plandaten.html | 02_Projektdetail/04_Explorer.html | Tab "Explorer" + "Im Explorer zeigen" (Button-Sprung in den In-App-Tab post-V1; WPF-Button heißt „Im Windows-Explorer zeigen“ und öffnet den Windows-Explorer, Audit Review 01, R01-001 E14, entschieden 2026-09-24) | ✅ aktiv |
| 02_Projektdetail/05_Plandaten.html | 02_Projektdetail/02_ManuellSortieren.html | Tab "Manuell sortieren" | ✅ aktiv |
| 02_Projektdetail/05_Plandaten.html | 02_Projektdetail/01_Profile.html | Tab "Profile" | ✅ aktiv |
| 02_Projektdetail/05_Plandaten.html | 04_FeldtypManager/01_Uebersicht.html | "⚙ Segmenttypen verwalten…" | ✅ aktiv |
| 02_Projektdetail/05_Plandaten.html | 01_Projektuebersicht/01_Projektuebersicht.html | ← Zurück-Pfeil + Sidebar | ✅ aktiv |
| 02_Projektdetail/05_Plandaten.html | 02_Projektdetail/03_Sync.html | Tab "Sync" | 🟡 tot |
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
| 02_Projektdetail/02_ManuellSortieren_Claude.html | ../Settings/01_Einstellungen/01_Allgemein.html | "Einstellungen" (Zustand 7 nicht erreichbar / 8 Claude aus) | ✅ aktiv |

## Hinweise (Teil 50)

- **Tab-Reihenfolge Projektdetail (Entscheidung Herbert):** Explorer · Manuell sortieren · Plandaten · Profile · Sync — Explorer ist damit der Start-Tab beim Projektöffnen.
- **04_FeldtypManager Rückweg:** "Schließen" führt hardcodiert zum Wizard (03_ProfilWizard/02_Segmente.html); der neue Aufrufer 05_Plandaten hat im Mockup keinen eigenen Rückweg (App: Dialog kehrt zum Aufrufer zurück).

## Hinweise (Audit Review 01, R01-001 E14, entschieden 2026-09-24)

- **Rangfolge:** Für konkrete Screens gilt das neueste freigegebene Mockup, Code, der ihm folgt, gilt als Ist (UI_UX_Guidelines.md Kap. 1.4). Für fertige Screens ohne Mockup wird keines nachgezogen (Status 🔷).
- **Plandaten „Im Explorer zeigen“:** heißt in WPF „Im Windows-Explorer zeigen“ und öffnet den Windows-Explorer; der Sprung in den In-App-Tab „Explorer“ kommt post-V1. Der Button-`onclick` im Mockup `05_Plandaten.html` zeigt damit ein Post-V1-Verhalten.
- **Segmenttyp-Manager:** Drag-Reorder von Gruppen und Typen kommt post-V1.
- **Kosmetische Abweichungen werden ins Mockup übernommen** (offen, Mockups noch nicht angepasst): Radial 460px statt 500px und Ring-Ausblendung ohne Animation (Animation nur beim Erscheinen) in `02_ManuellSortieren.html`; Statistikzeile wie im Code („n Gruppen · n Segmenttypen · n deaktiviert · n eigen“) in `04_FeldtypManager/01_Uebersicht.html`.

## Offene Punkte (Backlog-Referenz)

- **BPM-004 / BPM-111.01** — Mockup `02_Projektdetail/02_ManuellSortieren.html` — ✅ erledigt (Radial-Erfassung nach ADR-059, Teil 43; alte Listenvariante in `_Archiv/`. Spezifikation als Kommentar-Block im HTML-Header: Hold/Dwell-Timing, typabhängige Ringe, + Neu… je Ebene, Pending/Update/Archiv-Tab, angedocktes Vorschau-Fenster)
- **BPM-005** — Mockup `02_Projektdetail/03_Sync.html`
- **BPM-080.01–04** — Mockups `03_ProfilWizard/01_Datei.html` bis `04_Zielordner.html` — ✅ erledigt
- **BPM-007.02** — Mockup `03_ProfilWizard/05_Erkennung.html` (Toggle Segmente/Regex) — ✅ Segmente-Modus erledigt, Regex-Modus-Variante (`05_Erkennung_Regex.html`) noch offen
- **BPM-080.05** — WPF-Umsetzung des kompletten Wizards
- **BPM-108** — Segmenttyp-Verwaltung DB-basiert (Architektur abgeschlossen via CGR-2026-05-12-segmenttyp-architektur). Mockups: `04_FeldtypManager/01_Uebersicht.html` (Manager mit Built-in Fachrolle + Token read-only) + `03_ProfilWizard/02_Segmente_NeuerCustom.html` (Inline-Popover). WPF-Umsetzung: 3-Phasen-Plan (Phase A Domain → B Profilformat v4 → C Wizard/UI)
