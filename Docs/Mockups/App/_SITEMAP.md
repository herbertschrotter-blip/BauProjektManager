# App (Shell) — Sitemap

Quelle der Wahrheit für Navigationskanten und Fenster-Hierarchie im App-Shell-Mockup-Modul
(MainWindow: Sidebar, Toolbar, Statusleiste). Wird vor jedem neuen Mockup gelesen und nach
Mockup-Änderungen aktualisiert.

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
| 01_Shell | (App-Start, Referenz-Mockup) | – | ✅ aktiv | 01_Sidebar.html (klappbare Sidebar 220px ↔ 56px, BPM-067, Teil 52) |

## Navigationskanten

### Extern (Cross-Modul)

| Quelle | Ziel (extern) | Trigger | Status |
|---|---|---|---|
| 01_Shell/01_Sidebar.html | ../PlanManager/01_Projektuebersicht/01_Projektuebersicht.html | Sidebar "📁 PlanManager" | ✅ aktiv |
| 01_Shell/01_Sidebar.html | ../Settings/01_Einstellungen/01_Allgemein.html | Sidebar "⚙ Einstellungen" | ✅ aktiv |
| 01_Shell/01_Sidebar.html | ../Settings/02_DevTools/01_Log.html | Sidebar "🛠 Dev Tools" | ✅ aktiv |
| 01_Shell/01_Sidebar.html | (Home / Dashboard) | Sidebar "🏠 Home" | 🟡 tot |

### Eingehende Kanten

Keine. Das Shell-Mockup ist ein Referenz-Entwurf für die Sidebar; die bestehenden Modul-Mockups
(PlanManager, Settings) tragen ihre eigene, feste 200px-Sidebar im Markup und verlinken nicht hierher.

## Hinweise

- **Zustände:** Chevron oben oder Doppelklick auf den Titel schaltet A (220px, Emoji + Text) ↔ B (56px, Emoji + Tooltip, Badge als Ecke). Default A. Zustand gerätelokal (`UiLayout.SidebarCollapsed`, WPF-Umsetzung in BPM-067 Schritt 2).
- **Post-V1-Module** (Fotos, Zeiterfassung, Bautagebuch, Kalkulation, Wetter, Outlook, Vorlagen, Aufgaben, GIS, KI-Assistent) sind ausgegraut und nicht klickbar — Sichtbarkeit später über Ansichtsprofile (Architektur Kap. 1.4).
- **Emoji-Quellen:** 📁 ⚙ 🛠 👤 aus `Icons.xaml`; 📷 ⏱ 📓 📧 🌤 📄 aus dem Modulbaum in `BauProjektManager_Architektur.md`; 🏠 🧮 📋 🗺 🤖 neu vorgeschlagen (Icons.xaml-Keys beim WPF-Umbau anlegen).
- **Einstellungen + Dev Tools unten** (Entscheidung Herbert, Teil 52), Home ganz oben übernimmt die Dashboard-Rolle.
