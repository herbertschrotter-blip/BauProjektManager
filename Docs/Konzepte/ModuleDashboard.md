---
doc_id: konzept-dashboard
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [dashboard, widgets, startseite, projekt-übersicht, quick-actions]
read_when: [dashboard-feature, widget-hinzufügen, startseite]
related_docs: [architektur, planmanager, konzept-wetter, konzept-bautagebuch]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für Dashboard-Startseite mit Widgets (Projekte, Pläne, Wetter, Outlook, Bautagebuch)
- Autorität: secondary
- Lesen wenn: Dashboard-Feature, Widget hinzufügen, Startseite
- Nicht zuständig für: Widget-Datenquellen (→ jeweilige Modul-Docs)
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Widgets laden asynchron — App blockiert nie
  - Widgets die ein nicht-implementiertes Modul brauchen werden ausgeblendet

---

# BauProjektManager — Modul: Dashboard

**Status:** Nach V1 (Phase 3)  
**Version:** 1.1 (Refactoring auf DOC-STANDARD)  
**Abhängigkeiten:** Einstellungen-Modul, PlanManager-Modul  

---

## 1. Zweck und Zielzustand

Das Dashboard ist die Startseite der App. Es zeigt auf einen Blick den Status aller aktiven Projekte: Neue Pläne im Eingang, Wetter auf den Baustellen, offene Outlook-Emails, Bautagebuch-Status.

Jedes Widget ist ein eigenständiges WPF UserControl das Daten aus einem Service holt. Widgets laden asynchron — die App blockiert nicht wenn z.B. die Wetter-API langsam antwortet.

---

## 2. Datenmodell (geplant)

### Widgets

| Widget | Datenquelle | Aktualisierung | Abhängigkeit |
|--------|-------------|---------------|-------------|
| Projekt-Übersicht | Registry (SQLite) + Plan-Cache | Beim Start + manuell | Einstellungen |
| Neue Pläne (Eingang) | Filesystem-Scan (_Eingang) | Beim Start + manuell | PlanManager |
| Wetter | Wetter-API (OpenMeteo o.ä.) | Stündlich / manuell | Wetter-Modul |
| Outlook | Outlook COM (wenn Outlook offen) | Manuell (Sync-Button) | Outlook-Modul |
| Bautagebuch-Status | Bautagebuch-DB | Beim Start | Bautagebuch-Modul |

### C# Datenmodell

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
```

---

## 3. Workflow

### GUI-Mockup (Kurzform)

Dashboard zeigt Kacheln/Widgets in 2-Spalten-Layout: Projekte (links oben), Wetter (rechts oben), Neue Pläne (Mitte), Outlook + Bautagebuch (unten). Quick-Actions:

| Aktion | Wohin |
|--------|-------|
| Klick auf Projekt | → PlanManager Projekt-Detail |
| [Import] Button | → PlanManager Import-Workflow |
| [Sync] Button | → Outlook-Modul Sync |
| [Öffnen] Button | → Bautagebuch für heute |

---

## 4. Technische Umsetzung

Jedes Widget ist ein eigenständiges WPF UserControl mit eigenem ViewModel. Daten werden über Services geladen (DI). Async-Loading mit CancellationToken.

---

## 5. Abhängigkeiten

| Widget | Benötigt | Ohne Modul |
|--------|---------|------------|
| Projekte | Einstellungen (V1) | Immer da |
| Neue Pläne | PlanManager (V1) | Immer da |
| Wetter | Wetter-Modul | Ausgeblendet |
| Outlook | Outlook-Modul | Ausgeblendet |
| Bautagebuch | Bautagebuch-Modul | Ausgeblendet |

---

## 6. No-Gos / Einschränkungen

- Kein automatischer Refresh im Hintergrund (nur beim Start + manuell)
- Keine eigenen Daten — Dashboard ist rein lesend
- Kein Widget-Customizing in V1 (feste Anordnung)

---

## 7. Offene Fragen

- Soll das Dashboard konfigurierbar sein (Widgets ein/aus, Reihenfolge)?
- Soll es einen "Heute"-Zusammenfassungs-Widget geben?

---

*Erstellt: 27.03.2026 | Phase 3 (nach V1)*

*Änderungen v1.0 → v1.1 (11.04.2026):*
*- Frontmatter + AI-Quickload ergänzt (DOC-STANDARD)*
*- Kapitelstruktur auf concept-Vorlage refactort*
*- Kein Inhalt gelöscht — nur umgruppiert*
