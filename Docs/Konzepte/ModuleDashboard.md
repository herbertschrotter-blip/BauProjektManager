# BauProjektManager — Modul: Dashboard

**Status:** Nach V1 (Phase 3)  
**Abhängigkeiten:** Einstellungen-Modul, PlanManager-Modul  
**Referenz:** Architektur v1.4, Kapitel 11.1  

---

## 1. Konzept

Das Dashboard ist die Startseite der App. Es zeigt auf einen Blick den Status aller aktiven Projekte: Neue Pläne im Eingang, Wetter auf den Baustellen, offene Outlook-Emails, Bautagebuch-Status.

Jedes Widget ist ein eigenständiges WPF UserControl das Daten aus einem Service holt. Widgets laden asynchron — die App blockiert nicht wenn z.B. die Wetter-API langsam antwortet.

---

## 2. Widgets

| Widget | Datenquelle | Aktualisierung | Abhängigkeit |
|--------|-------------|---------------|-------------|
| Projekt-Übersicht | Registry (SQLite) + Plan-Cache | Beim Start + manuell | Einstellungen |
| Neue Pläne (Eingang) | Filesystem-Scan (_Eingang) | Beim Start + manuell | PlanManager |
| Wetter | Wetter-API (OpenMeteo o.ä.) | Stündlich / manuell | Wetter-Modul |
| Outlook | Outlook COM (wenn Outlook offen) | Manuell (Sync-Button) | Outlook-Modul |
| Bautagebuch-Status | Bautagebuch-DB | Beim Start | Bautagebuch-Modul |

---

## 3. GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  BauProjektManager                [Dashboard] [Pläne] [BTB] [⚙]║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ┌─ Projekte ──────────────┐  ┌─ Wetter ────────────────────┐  ║
║  │ 3 Aktiv   1 Fertig      │  │ Dobl-Zwaring:               │  ║
║  │                          │  │ ☀️ 14°C Sonnig, Wind 12km/h │  ║
║  │ 🏗 Dobl-Zwaring         │  │ Morgen: 🌧 8°C Regen        │  ║
║  │   ⚠️ 5 neue Pläne       │  │ → Kein Betonieren morgen!   │  ║
║  │   ⚠️ 2 Pläne veraltet   │  │                              │  ║
║  │ 🏗 Kapfenberg            │  │ Kapfenberg:                 │  ║
║  │   ✅ Alles aktuell       │  │ ⛅ 11°C Bewölkt             │  ║
║  │ 🏗 Leoben (fertig)      │  │ Morgen: ☀️ 15°C Sonnig      │  ║
║  │   🔴 Abgeschlossen       │  │                              │  ║
║  └──────────────────────────┘  └──────────────────────────────┘  ║
║                                                                  ║
║  ┌─ Neue Pläne (Eingang) ───────────────────────────────────┐  ║
║  │ 📥 Dobl-Zwaring:  5 Dateien (3 PDF, 2 DWG)    [Import]  │  ║
║  │ 📥 Kapfenberg:    0 Dateien                               │  ║
║  │ 📥 Leoben:        — (abgeschlossen)                      │  ║
║  └───────────────────────────────────────────────────────────┘  ║
║                                                                  ║
║  ┌─ Outlook ────────────────┐  ┌─ Heute ────────────────────┐  ║
║  │ 📧 3 Emails mit Anhängen │  │ 📓 Bautagebuch:            │  ║
║  │ von: arch.tschom@...     │  │    Dobl: Noch nicht        │  ║
║  │ von: statik@...          │  │    ausgefüllt!             │  ║
║  │ von: eplan@...           │  │    Kapfenberg: ✅ Fertig   │  ║
║  │              [Sync]      │  │               [Öffnen]     │  ║
║  └──────────────────────────┘  └──────────────────────────────┘  ║
║                                                                  ║
║  ── Statusleiste ───────────────────────────────────────────── ║
║  Registry OK | 3 Projekte | Letzte Sync: 14:32                 ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 4. Datenmodell

```csharp
public class DashboardData
{
    public List<ProjectSummary> Projects { get; set; }
    public DateTime LastRefresh { get; set; }
}

public class ProjectSummary
{
    public string ProjectId { get; set; }
    public string Name { get; set; }
    public ProjectStatus Status { get; set; }
    public int InboxFileCount { get; set; }
    public int OutdatedPlanCount { get; set; }
    public WeatherInfo? CurrentWeather { get; set; }
    public bool DiaryCompletedToday { get; set; }
    public int UnprocessedEmailCount { get; set; }
}

public class WeatherInfo
{
    public double Temperature { get; set; }
    public string Condition { get; set; }
    public string Wind { get; set; }
    public double Precipitation { get; set; }
    public string ForecastTomorrow { get; set; }
}
```

---

## 5. Quick-Actions

| Aktion | Wohin |
|--------|-------|
| Klick auf Projekt | → PlanManager Projekt-Detail |
| [Import] Button | → PlanManager Import-Workflow |
| [Sync] Button | → Outlook-Modul Sync |
| [Öffnen] Button | → Bautagebuch für heute |

---

## 6. Widget-Abhängigkeiten

Widgets die ein noch nicht implementiertes Modul benötigen werden nicht angezeigt. Das Dashboard wächst mit jedem neuen Modul.

| Widget | Benötigt | Ohne Modul |
|--------|---------|------------|
| Projekte | Einstellungen (V1) | Immer da |
| Neue Pläne | PlanManager (V1) | Immer da |
| Wetter | Wetter-Modul | Ausgeblendet |
| Outlook | Outlook-Modul | Ausgeblendet |
| Bautagebuch | Bautagebuch-Modul | Ausgeblendet |

---

*Erstellt: 27.03.2026 | Phase 3 (nach V1)*