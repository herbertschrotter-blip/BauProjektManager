---
doc_id: konzept-dashboard
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [dashboard, widgets, startseite, projekt-übersicht, quick-actions, baulotse, widget-host, layout-persistenz]
read_when: [dashboard-feature, widget-hinzufügen, startseite, baulotse-modus]
related_docs: [architektur, planmanager, konzept-wetter, konzept-bautagebuch]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für Dashboard-Startseite mit Widget-Host, Layout-Persistenz und Baulotse-Modus
- Autorität: secondary
- Lesen wenn: Dashboard-Feature, Widget hinzufügen, Startseite, Baulotse-Modus
- Nicht zuständig für: Widget-Datenquellen (→ jeweilige Modul-Docs), PlanManager-Fachlogik
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
  - 8. Zukunftserweiterungen
  - 9. Umsetzungsempfehlung
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Widgets laden asynchron — App blockiert nie
  - Widgets die ein nicht-implementiertes Modul brauchen werden ausgeblendet
  - Dashboard enthält keine Geschäftslogik — nur Anzeige und Navigation
  - Widget-Daten kommen aus Services, nie direkt aus SQLite/Dateisystem
  - PlanManager muss unabhängig vom Dashboard funktionieren

---

# BauProjektManager — Modul: Dashboard

**Status:** Nach V1 (Phase 3)
**Version:** 2.0 (Komplett überarbeitet mit Widget-Host, Baulotse-Modus, Layout-Persistenz)
**Abhängigkeiten:** Einstellungen-Modul, PlanManager-Modul
**Referenz:** Architektur, WPF/MVVM

---

## 1. Zweck und Zielzustand

Das Dashboard ist die zentrale Übersichts- und Startseite der Anwendung. Es zeigt dem Nutzer auf einen Blick den Status aller aktiven Projekte: neue Dateien im _Eingang, Import- und Konfliktstatus des PlanManager, offene Tagesaktionen, später auch Wetter, Outlook und Bautagebuch.

Das Dashboard arbeitet als Widget-Host. Jedes Widget ist ein eigenständiges WPF UserControl mit eigenem ViewModel und bezieht seine Daten aus Services. Widgets laden asynchron — die App blockiert nicht.

### Architekturprinzipien

Fachlogik bleibt in Services, Domain-Modellen und Infrastrukturklassen. Das Dashboard enthält keine Geschäftslogik des PlanManager oder anderer Module. Views und Widgets sind reine Darstellungs- und Interaktionsschicht. Sie zeigen modellierte Ergebnisdaten an und rufen Commands/Services auf.

Widgets arbeiten auf klaren Ergebnisobjekten (DashboardData, ProjectSummary, PlanManagerHealthSummary), nicht auf internen UI-Zwischenzuständen.

Ein Widget wird genau einmal gebaut und kann später im Dashboard angezeigt oder optional in einem separaten Fenster gehostet werden. V1 benötigt nur den Dashboard-Host.

### Modulgrenzen

Das Dashboard liest Status- und Summary-Daten aus anderen Modulen, zeigt sie als Widgets an und bietet Quick Actions zu bestehenden Workflows. Das Dashboard ist nicht verantwortlich für: Parsing von Dateinamen, Plan-Import-Entscheidungen, Outlook-Synchronisation, Wetterberechnung, Bautagebuch-Logik. Diese Logik bleibt in den jeweiligen Modulen/Services.

### Baulotse-Modus (später)

Das Dashboard kann später in zwei Modi verwendet werden: als normale App-Startseite und als Baulotse-Modus mit maximiertem/vollbildnahem Start und gespeichertem Widget-Layout. Der Baulotse-Vollbildmodus ist nicht Voraussetzung für V1.

---

## 2. Datenmodell (geplant)

### V1-Widgets

| Widget | Datenquelle | Status |
|--------|-------------|--------|
| Projekt-Übersicht | Registry / SQLite | V1 Dashboard |
| Neue Pläne im Eingang | Filesystem + PlanManager | V1 Dashboard |
| Letzter Import / Konflikte | PlanManager Summary | V1 Dashboard |
| Schnellaktionen | Navigation / Commands | V1 Dashboard |

### Nicht Teil von V1

| Widget | Voraussetzung | Status |
|--------|--------------|--------|
| Wetter | Wetter-Modul/API | später |
| Outlook | Outlook-Modul | später |
| Bautagebuch-Status | Bautagebuch-Modul | später |
| Floating Widgets | eigenes Host-/Fensterkonzept | später |

### C# Datenmodelle

```csharp
public sealed class DashboardData
{
    public List<ProjectSummary> Projects { get; set; } = new();
    public DateTime LastRefresh { get; set; }
}

public sealed class ProjectSummary
{
    public string ProjectId { get; set; } = "";
    public string Name { get; set; } = "";
    public ProjectStatus Status { get; set; }
    public int InboxFileCount { get; set; }
    public int UnknownImportCount { get; set; }
    public int ConflictCount { get; set; }
    public DateTime? LastImportAt { get; set; }
    public string? LastImportStatus { get; set; }
}

public sealed class PlanManagerHealthSummary
{
    public int InboxFileCount { get; set; }
    public int UnknownCount { get; set; }
    public int ConflictCount { get; set; }
    public DateTime? LastImportAt { get; set; }
    public bool HasPendingImport { get; set; }
}
```

Diese Modelle sind bewusst Summary-orientiert und nicht identisch mit internen Import-Analyseobjekten.

### Widget-Modell

Jedes Widget besteht aus vier Ebenen:

| Ebene | Beispiel (Projekt-Übersicht) |
|-------|------------------------------|
| Datenmodell / DTO | ProjectOverviewSummary |
| Service | IProjectOverviewService |
| ViewModel | ProjectOverviewWidgetViewModel |
| View (UserControl) | ProjectOverviewWidgetView.xaml |

Regeln: Widgets laden Daten asynchron. Widgets blockieren nicht die gesamte Seite. Widgets dürfen Refresh unterstützen. Widgets dürfen eigene Commands anbieten. Widgets greifen nicht direkt auf SQLite/Dateisystem zu, sondern nur über Services.

### Layout-Persistenz

Speicherung in JSON, nicht in SQLite. Begründung: schneller umzusetzen, gut lesbar/debugbar, geringe Komplexität, ausreichend für erste Layoutdefinitionen. Kann später bei Bedarf in SQLite verschoben werden.

```csharp
public sealed class DashboardLayout
{
    public string LayoutId { get; set; } = "default";
    public bool StartFullscreen { get; set; } = false;
    public List<WidgetPlacement> Widgets { get; set; } = new();
}

public sealed class WidgetPlacement
{
    public string WidgetId { get; set; } = "";
    public string WidgetType { get; set; } = "";
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
}
```

---

## 3. Workflow

### Dashboard als Host

Das Dashboard ist ein Host für mehrere Widgets mit folgenden Aufgaben: Layout laden, Widgets instanziieren, Refresh koordinieren, Fehler einzelner Widgets isoliert anzeigen, Quick Actions / Navigation bereitstellen.

V1-Layout: Ein einfaches Raster mit festen Zonen/Spalten. Keine freie Pixel-Positionierung. Kein Drag&Drop-Layoutbearbeitung.

### Quick Actions

| Aktion | Ziel |
|--------|------|
| Projekt öffnen | PlanManager Projekt-Detail |
| Eingang prüfen | PlanManager Import-Vorschau |
| Import starten | PlanManager Import-Workflow |
| Konflikte anzeigen | PlanManager Filteransicht |
| Projektordner öffnen | Explorer / Projektpfad |
| Heute öffnen | später Bautagebuch |

### Startverhalten

V1: Dashboard ist eine normale Start-/Navigationsseite.

Geplante Konfigurationsoptionen (später):

| Option | Werte |
|--------|-------|
| StartPage | Dashboard |
| StartFullscreen | true/false |
| RestoreLastDashboardLayout | true/false |

---

## 4. Technische Umsetzung

### Services

```csharp
public interface IDashboardLayoutService
{
    Task<DashboardLayout> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(DashboardLayout layout, CancellationToken ct = default);
}

public interface IProjectOverviewService
{
    Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(
        CancellationToken ct = default);
}

public interface IPlanManagerSummaryService
{
    Task<PlanManagerHealthSummary> GetSummaryAsync(
        string projectId, CancellationToken ct = default);
}
```

Wichtige Regel: Das Dashboard darf keine fachliche Duplikatlogik zum PlanManager enthalten. Summary-Daten kommen aus dafür vorgesehenen Services.

### Empfohlene Projektstruktur

```
src/
└── BauProjektManager.Dashboard/
    ├── Views/
    │   ├── DashboardView.xaml
    │   └── Widgets/
    │       ├── ProjectOverviewWidgetView.xaml
    │       ├── InboxWidgetView.xaml
    │       ├── PlanManagerStatusWidgetView.xaml
    │       └── QuickActionsWidgetView.xaml
    ├── ViewModels/
    │   ├── DashboardViewModel.cs
    │   └── Widgets/
    │       ├── ProjectOverviewWidgetViewModel.cs
    │       ├── InboxWidgetViewModel.cs
    │       ├── PlanManagerStatusWidgetViewModel.cs
    │       └── QuickActionsWidgetViewModel.cs
    ├── Services/
    │   ├── IDashboardLayoutService.cs
    │   ├── JsonDashboardLayoutService.cs
    │   └── WidgetFactory.cs
    └── BauProjektManager.Dashboard.csproj
```

Falls zunächst kein eigenes Projekt angelegt wird, kann das Dashboard auch vorübergehend innerhalb eines bestehenden Moduls entstehen. Mittelfristig ist ein eigenes Modul sauberer.

---

## 5. Abhängigkeiten

### Widget-Abhängigkeiten

| Widget | Benötigt | Ohne Modul |
|--------|---------|------------|
| Projekte | Einstellungen (V1) | Immer da |
| Neue Pläne | PlanManager (V1) | Immer da |
| Import/Konflikte | PlanManager (V1) | Immer da |
| Wetter | Wetter-Modul | Ausgeblendet |
| Outlook | Outlook-Modul | Ausgeblendet |
| Bautagebuch | Bautagebuch-Modul | Ausgeblendet |

### Abhängigkeitsrichtung

Das Dashboard hängt vom PlanManager nur als Daten- und Aktionsquelle ab. Der PlanManager hängt nicht vom Dashboard ab. Der PlanManager muss vollständig eigenständig funktionieren, auch wenn das Dashboard noch nicht implementiert ist.

Reihenfolge: zuerst PlanManager fertig bauen → Dashboard später darauf aufsetzen.

---

## 6. No-Gos / Einschränkungen

- Kein automatischer Refresh im Hintergrund (nur beim Start + manuell)
- Keine eigenen Daten — Dashboard ist rein lesend
- Kein Widget-Customizing in V1 (feste Anordnung, festes Raster)
- Keine Geschäftslogik im Dashboard (kein Plan-Parsing, kein Import)
- Kein Drag&Drop-Layout in V1
- Keine freie Pixel-Positionierung in V1
- Keine Floating Widgets in V1

---

## 7. Offene Fragen

- Soll das Dashboard konfigurierbar sein (Widgets ein/aus, Reihenfolge)?
  → Geplant für spätere Version, nicht V1
- Soll es einen "Heute"-Zusammenfassungs-Widget geben?
  → Möglich als Quick-Win wenn Bautagebuch-Modul existiert
- Soll die Layout-Persistenz später von JSON nach SQLite migriert werden?
  → Entscheidung vertagt, JSON reicht für V1

---

## 8. Zukunftserweiterungen

### Floating Widgets

Spätere Ausbaustufe: einzelne Widgets als separates WPF-Fenster, Position und Größe speichern, optional TopMost, gleicher Widget-Code wie im Dashboard.

### Benutzerkonfiguration

Später möglich: Widgets ein-/ausblenden, Reihenfolge ändern, Größe anpassen, projektspezifische Layouts.

### Modulwachstum

Mit neuen Modulen kann das Dashboard schrittweise wachsen: Wetter, Outlook, Bautagebuch, Foto-Modul, Zeiterfassung. Widgets für nicht vorhandene Module werden nicht angezeigt.

---

## 9. Umsetzungsempfehlung

### Reihenfolge

1. PlanManager fachlich sauber fertigstellen
2. Summary-Services im PlanManager bereitstellen
3. Dashboard-Modul mit einfachem Rasterlayout bauen
4. Zwei bis vier Widgets anbinden
5. Startseite optional auf Dashboard umstellen
6. Später Vollbild-/Baulotse-Modus ergänzen

### Erste sinnvolle Dashboard-Version

- ProjectOverviewWidget
- InboxWidget
- PlanManagerStatusWidget
- QuickActionsWidget

Damit entsteht sofort echter Nutzen, ohne spätere Module vorwegzunehmen.

### Entscheidungen für die Umsetzung

Jetzt schon festhalten:
- Dashboard bleibt eigenes Modul
- Widgets sind WPF-UserControls
- Daten kommen aus Services
- PlanManager kann unabhängig weiterentwickelt werden
- Ergebnisdaten werden klar modelliert
- V1 nutzt Rasterlayout + JSON-Persistenz

Bewusst noch offen lassen:
- Floating Widgets
- Freie Drag&Drop-Anordnung
- Kompletter Baulotse-Vollbildmodus
- SQLite statt JSON für Dashboard-Layout

---

*Erstellt: 27.03.2026 | Phase 3 (nach V1)*

*Änderungen v1.0 → v1.1 (11.04.2026):*
*- Frontmatter + AI-Quickload ergänzt (DOC-STANDARD)*
*- Kapitelstruktur auf concept-Vorlage refactort*

*Änderungen v1.1 → v2.0 (16.04.2026):*
*- Komplett überarbeitet mit ChatGPT Cross-Review*
*- Widget-Host-Architektur, 4-Ebenen-Widget-Modell*
*- Layout-Persistenz (JSON + DashboardLayout/WidgetPlacement)*
*- Baulotse-Startmodus als Zukunftserweiterung*
*- PlanManagerHealthSummary als eigenes DTO*
*- Service-Interfaces (IDashboardLayoutService, IProjectOverviewService, IPlanManagerSummaryService)*
*- Empfohlene Projektstruktur*
*- Umsetzungsreihenfolge und Entscheidungen*
*- Kein Inhalt aus v1.1 gelöscht — alles übernommen und erweitert*
