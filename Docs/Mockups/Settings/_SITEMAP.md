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
- ❌ kaputt — Pfad-Inkonsistenz

## Fenster

| Ordner | Aufrufer | Trigger | Status | Datei(en) |
|---|---|---|---|---|
| 01_Einstellungen | (Sidebar) / 02_DevTools | "⚙ Einstellungen" / Close-Button | 🟡 in Arbeit | 01_Allgemein.html (Tabs 02–05 fehlen) |
| 02_DevTools | 01_Einstellungen | Sidebar "🔧 Dev Tools" | 🟡 in Arbeit | 01_Log.html, 02_Reset.html (03_SystemInfo fehlt) |

## Navigationskanten

### Intern (Settings)

| Quelle | Ziel | Trigger | Status |
|---|---|---|---|
| 01_Einstellungen/01_Allgemein.html | 02_DevTools/01_Log.html | Sidebar "🔧 Dev Tools" | ✅ aktiv |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/02_Projekte.html | Tab "Projekte" | 🟡 tot |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/03_StandardOrdnerstruktur.html | Tab "Standard-Ordnerstruktur" | 🟡 tot |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/04_SyncDefaults.html | Tab "Sync-Defaults" | 🟡 tot |
| 01_Einstellungen/01_Allgemein.html | 01_Einstellungen/05_Ueber.html | Tab "Über" | 🟡 tot |
| 02_DevTools/01_Log.html | 02_DevTools/02_Reset.html | Tab "🗑 Reset" | ✅ aktiv |
| 02_DevTools/01_Log.html | 02_DevTools/03_SystemInfo.html | Tab "⧉ System-Info" | 🟡 tot |
| 02_DevTools/01_Log.html | 01_Einstellungen/01_Allgemein.html | Footer "Schließen" + rotes X | ✅ aktiv |
| 02_DevTools/02_Reset.html | 02_DevTools/01_Log.html | Tab "📄 Log" | ✅ aktiv |
| 02_DevTools/02_Reset.html | 02_DevTools/03_SystemInfo.html | Tab "⧉ System-Info" | 🟡 tot |
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
- **DevTools-Aufruf aus PlanManager:** Aktuell nicht direkt in der PlanManager-Sidebar verlinkt. Indirekter Pfad: PlanManager → Einstellungen → Dev Tools.
