# Settings — Sitemap

Quelle der Wahrheit für Navigationskanten und Fenster-Hierarchie im Settings-Mockup-Modul.
Wird vor jedem neuen Mockup gelesen und nach Mockup-Änderungen aktualisiert.

**Namens-Konvention (BPM):**
- Ordner-pro-Fenster, flache Nummerierung (`01_`, `02_`, …) im Modul-Wurzelverzeichnis
- Dateien innerhalb: `NN_Variante.html`
- Hierarchie/User-Journey wird in der `Aufrufer`-Spalte unten dokumentiert, nicht in Ordnernamen

**Status-Legende:**
- ✅ aktiv — Quelle hat `onclick`, Ziel existiert
- ⚪ geplant — Ziel existiert, `onclick` im Quell-HTML fehlt noch
- 🟡 tot — Ziel-Mockup fehlt, Aufrufer hat `alert('Mockup folgt: X')`
- 🔷 implementiert ohne Mockup — Screen ist im Code umgesetzt, ein Mockup gibt es nicht und wird nicht nachgezogen; der Code gilt als Ist (Audit Review 01, R01-001 E14, entschieden 2026-09-24)
- ❌ kaputt — Pfad-Inkonsistenz

## Fenster

| Ordner | Aufrufer | Trigger | Status | Datei(en) |
|---|---|---|---|---|
| 01_Einstellungen | (Sidebar) / 02_DevTools | "⚙ Einstellungen" / Close-Button | 🟡 in Arbeit | 01_Allgemein.html (Tabs Projekte, Standard-Ordnerstruktur, Über: 🔷 implementiert ohne Mockup; Tab Sync-Defaults: im Code nur Platzhalter, Inhalt mit 079.06) |
| 02_DevTools | 01_Einstellungen | Sidebar "🔧 Dev Tools" | ✅ aktiv | 01_Log.html, 02_Reset.html (Tab System-Info: 🔷 implementiert ohne Mockup) |
| – | 01_Einstellungen (Tab "Projekte") | "＋ Neues Projekt" → "Projekt erstellen" / "Bearbeiten" / Doppelklick auf Zeile | 🔷 implementiert ohne Mockup | – (WPF: `ProjectEditDialog.xaml`, 5 Tabs + Unterdialoge (Bauteil, Geschoß, Geschoß-Bezeichnungen, Beteiligte, Portale/Links)) |

## Navigationskanten

### Intern (Settings)

| Quelle | Ziel | Trigger | Status |
|---|---|---|---|
| 01_Einstellungen/01_Allgemein.html | 02_DevTools/01_Log.html | Sidebar "🔧 Dev Tools" | ✅ aktiv |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/02_Projekte.html | Tab "Projekte" | 🔷 implementiert ohne Mockup |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/03_StandardOrdnerstruktur.html | Tab "Standard-Ordnerstruktur" | 🔷 implementiert ohne Mockup |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/04_SyncDefaults.html | Tab "Sync-Defaults" | 🟡 tot |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/05_Ueber.html | Tab "Über" | 🔷 implementiert ohne Mockup |
| 02_DevTools/01_Log.html | 02_DevTools/02_Reset.html | Tab "🗑 Reset" | ✅ aktiv |
| 02_DevTools/01_Log.html | 02_DevTools/03_SystemInfo.html | Tab "⧉ System-Info" | 🔷 implementiert ohne Mockup |
| 02_DevTools/01_Log.html | 01_Einstellungen/01_Allgemein.html | Footer "Schließen" + rotes X | ✅ aktiv |
| 02_DevTools/02_Reset.html | 02_DevTools/01_Log.html | Tab "📄 Log" | ✅ aktiv |
| 02_DevTools/02_Reset.html | 02_DevTools/03_SystemInfo.html | Tab "⧉ System-Info" | 🔷 implementiert ohne Mockup |
| 02_DevTools/02_Reset.html | 01_Einstellungen/01_Allgemein.html | rotes X (Window-Close) | ✅ aktiv |

### Extern (Cross-Modul nach PlanManager)

| Quelle | Ziel (extern) | Trigger | Status |
|---|---|---|---|
| 01_Einstellungen/01_Allgemein.html | ../PlanManager/01_Projektuebersicht/01_Projektuebersicht.html | Sidebar "📁 PlanManager" | ✅ aktiv |

### Eingehende externe Kanten (aus PlanManager)

| Quelle (extern) | Ziel | Trigger | Status |
|---|---|---|---|
| ../PlanManager/01_Projektuebersicht/01_Projektuebersicht.html | 01_Einstellungen/01_Allgemein.html | Sidebar "⚙ Einstellungen" | ✅ aktiv |
| ../PlanManager/02_Projektdetail/01_Profile.html | 01_Einstellungen/01_Allgemein.html | Sidebar "⚙ Einstellungen" | ✅ aktiv |

## Hinweise

- **DevTools sind als Dialog ohne Sidebar gestylt** — Klick-Navigation erfolgt über Tabs (zwischen Log/Reset) und Schließen-Buttons (zurück zu Einstellungen).
- **Einstellungs-Tabs als gestrichelte Underline** mit Inline-Style — die `.tab`-Klasse hatte `border-bottom:3px solid transparent`, der gestrichelte Override macht den toten Zustand sichtbar.
- **Implementiert ohne Mockup** (Audit Review 01, R01-001 E14, entschieden 2026-09-24): Die Tabs Projekte, Standard-Ordnerstruktur und Über, der Dev-Tools-Tab System-Info und der ProjectEditDialog sind im Code fertig; für sie werden keine Mockups nachgezogen, der Code gilt als Ist. Die Kanten bleiben im Mockup als `alert('Mockup folgt: …')` stehen. Offen bleibt nur der Tab Sync-Defaults (Inhalt mit 079.06).
- **DevTools-Aufruf aus PlanManager:** Aktuell nicht direkt in der PlanManager-Sidebar verlinkt. Indirekter Pfad: PlanManager → Einstellungen → Dev Tools.

## Abgelöst durch die Seitenliste

Diese Mockups sind seit 2026-11-19 durch Seiten im Seitenformat abgelöst (`Docs/Mockups/seiten/`, Wahrheit: `Docs/Mockups/seiten.json`). Die alten Dateien bleiben unverändert als Herkunft liegen; Änderungen nur noch an der neuen Seite.

| Altes Mockup | Seite | Neue Datei |
|---|---|---|
| 01_Einstellungen/01_Allgemein.html | 2.1 Allgemein | `seiten/einstellungen-allgemein.html` |
| 02_DevTools/01_Log.html | 1.5 Entwicklerwerkzeuge – Log | `seiten/dev-log.html` |
| 02_DevTools/02_Reset.html | 1.6 Entwicklerwerkzeuge – Zurücksetzen | `seiten/dev-zuruecksetzen.html` |
