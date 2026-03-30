# BauProjektManager — Modul: Task-Management & Materialbestellung

**Erstellt:** 30.03.2026  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Kalkulation (work_packages), Einstellungen (projects, building_parts)  
**Erste Implementierung:** ClickUp (Herberts bestehendes System)  
**Architektur:** Interface-basiert — nicht an einen Anbieter gebunden

---

## 1. Ziel

Materialbedarfe auf der Baustelle transparent koordinieren — vom Bedarf bis zur Lieferung. BPM ersetzt dabei nicht das Task-Management-Tool, sondern **verknüpft sich damit**: strukturierte Daten (Bauteil, Geschoß, Menge) kommen aus BPM, Kommunikation und Status-Tracking laufen im externen Tool.

**Prinzip:** BPM = Daten + Berechnung, Task-Tool = Kommunikation + Koordination.

---

## 2. Herberts bestehendes ClickUp-System

### 2.1 Workflow

Ein Task = eine Materialbestellung. Alle Beteiligten arbeiten im selben Task. Status = Verantwortungswechsel.

**Rollen:**
- **Polier** — erstellt Bedarf, prüft Lieferung
- **Bauleiter** — entscheidet (intern/extern)
- **Dispo** — organisiert Umsetzung
- **Lager** — prüft Verfügbarkeit
- **Einkauf** — bestellt extern

**Status-Flow:**

```
BEDARF ANGELEGT (Polier)
    │
    ▼
BAULEITER PRÜFT (Bauleiter entscheidet: intern/extern)
    │
    ├──→ LAGER PRÜFT (intern)
    │       │
    │       ├──→ verfügbar → DISPO ORGANISIERT
    │       └──→ nicht verfügbar → zurück zu BAULEITER
    │
    └──→ EINKAUF BESTELLT (extern)
            │
            ├──→ Direktlieferung → Einkauf setzt Termin
            └──→ über Lager → DISPO ORGANISIERT
                    │
                    ▼
            LIEFERUNG UNTERWEGS
                    │
                    ▼
            LIEFERUNG GEPRÜFT (Polier)
                    │
                    ▼
            VORGANG ABGESCHLOSSEN

Sonderstatus: VORGANG HINFÄLLIG, KLÄRUNG ERFORDERLICH
```

### 2.2 Analyse — Stärken

- Klare Verantwortungskette über Status
- Alle sehen denselben Task → Transparenz
- Funktioniert bereits in der Praxis

### 2.3 Analyse — Schwachstellen

**Stehende Tasks:** Ohne Eskalationslogik bleibt ein Task bei "BAULEITER PRÜFT" hängen wenn der Bauleiter 30 offene Tasks hat. Der Polier steht ohne Material da.

**Doppelbestellungen:** Kein Duplikat-Check. Wenn zwei Poliere "Bewehrungsstahl Ø12" bestellen, wird doppelt bestellt.

**Teillieferungen:** Material kommt in 2 Tranchen — 60% heute, Rest nächste Woche. Kein sauberer Weg das im Status abzubilden.

**Mengenänderungen:** Polier bestellt 50 m³ Beton, nach Plan-Änderung braucht er 65 m³. Im Kommentar steht die Korrektur → wird überlesen.

**Zu viele Status:** 10 Status sind für Baustellen-Personal zu komplex. Real denken die Leute in 5 Zuständen: Offen → In Bearbeitung → Unterwegs → Geliefert → Erledigt.

### 2.4 Verbesserungsvorschläge

**Status vereinfachen (10 → 5+2):**

| Vereinfacht | Ersetzt | Wer ist dran |
|---|---|---|
| **OFFEN** | Bedarf angelegt | Polier |
| **IN BEARBEITUNG** | Bauleiter prüft + Dispo + Lager + Einkauf | Büro |
| **UNTERWEGS** | Lieferung unterwegs | Niemand (warten) |
| **GELIEFERT** | Lieferung geprüft | Polier (prüft) |
| **ERLEDIGT** | Vorgang abgeschlossen | — |
| **HINFÄLLIG** | Vorgang hinfällig | — |
| **KLÄRUNG** | Klärung erforderlich | Zugewiesene Person |

Der Detailschritt (Lager prüft, Einkauf bestellt) kann über ein Custom Field "Bearbeitungsschritt" abgebildet werden.

**Custom Fields statt Kommentare:**
- Material → Dropdown oder Text-Field
- Menge → Number-Field mit Einheit
- Liefertermin gewünscht → Date-Field
- Liefertermin bestätigt → Date-Field
- Dringlichkeit → Dropdown (Hoch/Normal/Niedrig)
- Intern/Extern → Dropdown
- Lagerstatus → Dropdown (Verfügbar/Teil/Nicht verfügbar)

**Automationen:**
- Task > 24h in "BAULEITER PRÜFT" → Benachrichtigung
- Status auf "IN BEARBEITUNG" → automatisch zuständigem Bauleiter zuweisen
- Liefertermin < 2 Tage UND noch nicht unterwegs → Warnung
- "GELIEFERT" → nach 3 Tagen automatisch "ERLEDIGT"

---

## 3. BPM-Integration

### 3.1 Was BPM liefert

BPM kennt die **strukturierten Daten** die dem Task-Tool fehlen:

| BPM weiß | Task-Tool braucht |
|-----------|------------------|
| Projekt (ÖWG Dobl) | Projekt-Zuordnung |
| Bauteil (H5) + Geschoß (EG) | Wo wird das Material gebraucht |
| Arbeitspaket (Mauerwerk 38er) | Wofür ist das Material |
| Soll-Menge (198 m² → 45 Pal Ziegel) | Wie viel wird gebraucht |
| Zeitplan (Mauerwerk startet 11.3.) | Wann wird es gebraucht |

### 3.2 Szenarien

**Szenario 1: BPM → Task-Tool (Bestellung anlegen)**

Polier sieht in BPM: "Arbeitspaket Mauerwerk H5/EG startet nächste Woche, 198 m² = 45 Paletten 38er Objekt Plan". Klickt "Material bestellen" → BPM erstellt automatisch einen Task im Task-Tool:

```
Titel: [ÖWG Dobl] Ziegel 38er Objekt Plan — H5/EG
Material: Ziegel 38er Wienerberger Objekt Plan
Menge: 45 Paletten
Bauteil: Haus 5, EG
Liefertermin: 10.03.2025
Dringlichkeit: Normal
Arbeitspaket: Mauerwerk 38er H5/EG
```

**Szenario 2: Task-Tool → BPM (Status zurückmelden)**

Wenn im Task-Tool der Status auf "GELIEFERT" wechselt, aktualisiert BPM die `material_orders` Tabelle. Dashboard zeigt: "45 Pal Ziegel für H5/EG geliefert ✅".

**Szenario 3: BPM berechnet Bedarf automatisch**

Aus dem Arbeitspaket "Mauerwerk 38er H5/EG, 198 m²" und der Ziegelberechnung (16 Stk/m², 54 Stk/Pal) berechnet BPM: 198 × 16 = 3.168 Stk ÷ 54 = 59 Paletten. Minus Verschnitt-Zuschlag → Bestellung vorschlagen.

### 3.3 Datenfluss

```
BPM                                    Task-Tool
───                                    ─────────
work_packages ──→ Bedarf erkennen
                  │
                  ▼
material_orders ──→ Task erstellen ──→ ClickUp/Asana/Trello
                                       │
                  ◄── Status-Update ◄──┘
                  │
                  ▼
              Dashboard zeigt:
              "45 Pal geliefert ✅"
```

---

## 4. Interface-Architektur — Nicht an ClickUp gebunden

### 4.1 Warum Interface?

Herbert verwendet ClickUp. Ein anderer Polier nutzt vielleicht Asana, Trello oder Microsoft Planner. BPM soll mit jedem dieser Tools funktionieren — genau wie BPM mit jedem Cloud-Speicher funktioniert (nicht nur OneDrive).

### 4.2 ITaskManagementService

```csharp
public interface ITaskManagementService
{
    // Task erstellen
    Task<string> CreateOrderTask(MaterialOrder order);
    
    // Status abrufen
    Task<string> GetTaskStatus(string externalTaskId);
    
    // Status aktualisieren
    Task UpdateTaskStatus(string externalTaskId, string status);
    
    // Offene Bestellungen abrufen
    Task<List<MaterialOrder>> GetOpenOrders(string projectId);
    
    // Sync: Task-Tool → BPM (Status-Updates holen)
    Task SyncFromExternal(string projectId);
}
```

### 4.3 Implementierungen

```csharp
// Erste Implementierung (Herberts Setup)
public class ClickUpTaskService : ITaskManagementService
{
    // REST API: https://api.clickup.com/api/v2/
    // API-Key in Windows Credential Manager
}

// Zukünftige Implementierungen
public class AsanaTaskService : ITaskManagementService { ... }
public class TrelloTaskService : ITaskManagementService { ... }
public class MicrosoftPlannerTaskService : ITaskManagementService { ... }
public class MondayTaskService : ITaskManagementService { ... }

// Offline-Fallback (kein externes Tool)
public class LocalTaskService : ITaskManagementService
{
    // Nur material_orders in SQLite, kein externer Sync
    // Für Solo-Poliere ohne Team-Tool
}
```

### 4.4 Systemeinstellungen

In den BPM-Einstellungen: "Welches Projektmanagement-Tool verwendest du?"
- Dropdown: ClickUp / Asana / Trello / Microsoft Planner / Keins (lokal)
- API-Key eingeben
- Workspace/Board/Liste auswählen
- Testen-Button

### 4.5 Unterstützte Tools und ihre APIs

| Tool | API | Auth | Besonderheiten |
|------|-----|------|---------------|
| ClickUp | REST v2 | API Key oder OAuth2 | Spaces/Folders/Lists Hierarchie, Custom Fields |
| Asana | REST | Personal Access Token | Workspaces/Projects/Tasks, Custom Fields |
| Trello | REST | API Key + Token | Boards/Lists/Cards, Labels statt Custom Fields |
| Monday.com | GraphQL | API Key | Boards/Items/Columns |
| Microsoft Planner | Graph API | OAuth2 (Azure AD) | Plans/Buckets/Tasks, braucht Office 365 |
| Notion | REST v1 | Integration Token | Databases/Pages, flexibelstes Datenmodell |

---

## 5. DB-Schema

### 5.1 material_orders

Siehe DB-SCHEMA.md Kapitel 5.10. Zusammenfassung:

```sql
CREATE TABLE material_orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    work_package_id INTEGER,              -- FK → work_packages (optional)
    building_part_id TEXT,                -- FK → building_parts
    level_id TEXT,                        -- FK → building_levels
    material TEXT NOT NULL,               -- "Ziegel 38er Objekt Plan"
    quantity REAL NOT NULL,               -- 45
    unit TEXT NOT NULL,                   -- "Pal"
    delivery_date_requested TEXT,
    delivery_date_confirmed TEXT,
    urgency TEXT DEFAULT 'Normal',        -- "Hoch" | "Normal" | "Niedrig"
    source TEXT DEFAULT 'Intern',         -- "Intern" | "Extern"
    status TEXT DEFAULT 'open',           -- "open" | "ordered" | "delivered" | "cancelled"
    external_task_id TEXT,                -- ClickUp Task-ID etc.
    external_system TEXT,                 -- "ClickUp" | "Asana" | "Trello"
    notes TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id),
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id)
);
```

### 5.2 Mapping: BPM-Status ↔ Tool-Status

Jedes Tool hat eigene Status-Bezeichnungen. BPM mappt intern:

| BPM-Status | ClickUp | Asana | Trello |
|-----------|---------|-------|--------|
| open | BEDARF ANGELEGT | Not Started | Offen |
| in_progress | IN BEARBEITUNG | In Progress | In Arbeit |
| ordered | EINKAUF BESTELLT | In Progress | Bestellt |
| shipping | LIEFERUNG UNTERWEGS | In Progress | Unterwegs |
| delivered | GELIEFERT | Completed | Geliefert |
| cancelled | HINFÄLLIG | Cancelled | Archiv |

Das Mapping wird in `settings.json` konfigurierbar gespeichert (pro Tool eigene Status-Namen).

---

## 6. GUI-Mockups

### 6.1 Materialbestellung aus BPM

```
┌─────────────────────────────────────────────────────────────────┐
│ Material bestellen                                               │
│                                                                  │
│ Projekt:      ÖWG Dobl-Zwaring                                  │
│ Bauteil:      [H5 ▾]  Geschoß: [EG ▾]                          │
│ Arbeitspaket: [Mauerwerk 38er ▾]  (optional)                    │
│                                                                  │
│ Material:     [Ziegel 38er Objekt Plan              ]            │
│ Menge:        [45    ]  Einheit: [Pal ▾]                        │
│ Liefertermin: [📅 10.03.2025]                                   │
│ Dringlichkeit:[Normal ▾]                                        │
│                                                                  │
│ Bemerkung:    [Rampe für LKW nötig, Abladeort BT5  ]            │
│                                                                  │
│ Senden an:    [ClickUp ▾]  ← aus Einstellungen                 │
│                                                                  │
│ [Bestellen & Task erstellen]                    [Abbrechen]      │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Bestellübersicht im Dashboard

```
┌─────────────────────────────────────────────────────────────────┐
│ Materialbestellungen: ÖWG Dobl                                   │
│ [Alle ▾]  [Offen ▾]  [Haus 5 ▾]                                │
├────┬─────────────────┬──────┬──────┬───────────┬────────────────┤
│ BT │ Material        │Menge │Status│ Lieferung │ Aktion         │
├────┼─────────────────┼──────┼──────┼───────────┼────────────────┤
│ H5 │ Ziegel 38er OP  │45 Pal│ 🚛   │ 10.03.    │ [ClickUp ↗]   │
│ H5 │ Beton C25/30    │24 m³ │ ✅   │ 08.03. ✓  │                │
│ H5 │ Bewehrung Ø12   │3,2 to│ 🔵   │ 12.03.    │ [ClickUp ↗]   │
│ H6 │ Ziegel 38er OP  │52 Pal│ ⬜   │ 17.03.    │ [ClickUp ↗]   │
├────┴─────────────────┴──────┴──────┴───────────┴────────────────┤
│ 4 Bestellungen: 1 geliefert, 1 unterwegs, 1 in Arbeit, 1 offen │
└─────────────────────────────────────────────────────────────────┘

Legende: ⬜ Offen  🔵 In Bearbeitung  🚛 Unterwegs  ✅ Geliefert
```

### 6.3 Einstellungen — Task-Management

```
┌─────────────────────────────────────────────────────────────────┐
│ Einstellungen → Task-Management                                  │
│                                                                  │
│ Tool:          [ClickUp ▾]                                      │
│ API-Key:       [ck_****************************] [Testen]        │
│                                              ✅ Verbindung OK    │
│                                                                  │
│ Workspace:     [BauFirma GmbH ▾]                                │
│ Space:         [Baustellen ▾]                                    │
│ Standard-Liste:[Materialbestellungen ▾]                          │
│                                                                  │
│ Status-Mapping:                                                  │
│   BPM "open"      → ClickUp [BEDARF ANGELEGT     ▾]            │
│   BPM "in_progress"→ ClickUp [IN BEARBEITUNG      ▾]            │
│   BPM "ordered"   → ClickUp [EINKAUF BESTELLT    ▾]            │
│   BPM "shipping"  → ClickUp [LIEFERUNG UNTERWEGS ▾]            │
│   BPM "delivered" → ClickUp [GELIEFERT           ▾]            │
│   BPM "cancelled" → ClickUp [HINFÄLLIG           ▾]            │
│                                                                  │
│ Auto-Sync:     [☑ Alle 5 Minuten Status prüfen]                │
│                                                                  │
│ [Speichern]                                                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Verbindung zu anderen Modulen

```
KALKULATION                         TASK-MANAGEMENT
───────────                         ───────────────
work_packages ──→ Bedarf erkennen
  (Soll-Mengen)   │                 
                  ▼                 
              "Material bestellen"  
                  │                 
                  ├──→ material_orders (BPM DB)
                  │                 
                  └──→ Task erstellen (ClickUp/Asana/...)
                                    │
BAUTAGEBUCH ◄── Status-Updates ◄───┘
  (Material geliefert)              
                                    
DASHBOARD ◄──── Bestellübersicht    
  (offene/gelieferte Bestellungen)  
```

- **Kalkulation:** Arbeitspakete liefern den Kontext (was, wo, wieviel)
- **Bautagebuch:** Materiallieferungen werden automatisch als Tageseintrag vorgeschlagen
- **Dashboard:** Zeigt offene Bestellungen, überfällige Lieferungen, Materialstatus
- **Ziegelberechnung:** Kann direkt Paletten-Bedarf berechnen → Bestellung vorschlagen

---

## 8. Implementierungsreihenfolge

| Phase | Was | Abhängigkeit |
|-------|-----|-------------|
| 1 | **Interface definieren** (ITaskManagementService) | — |
| 2 | **LocalTaskService** (nur SQLite, kein externes Tool) | Interface |
| 3 | **ClickUpTaskService** (erste externe Implementierung) | Interface |
| 4 | **Bestelldialog in BPM** (GUI) | LocalTaskService |
| 5 | **Dashboard-Widget** (Bestellübersicht) | material_orders |
| 6 | **Auto-Sync** (Status-Updates vom externen Tool holen) | ClickUpTaskService |
| 7 | **Weitere Implementierungen** (Asana, Trello etc.) | Bei Bedarf |

---

## 9. Abgrenzung

**Dieses Modul ist NICHT:**
- Ein vollständiges Einkaufssystem (keine Preise, keine Lieferantenverwaltung, keine Rechnungsprüfung)
- Ein Lagerverwaltungssystem (keine Bestände, keine Lagerplätze)
- Ein Ersatz für ClickUp/Asana/Trello (die Kommunikation bleibt dort)

**Dieses Modul IST:**
- Eine Brücke zwischen BPM-Daten und dem externen Task-Tool
- Ein Bestellauslöser mit Kontext (Bauteil, Geschoß, Menge, Termin)
- Eine Übersicht über den Materialstatus pro Projekt/Bauteil
- Tool-agnostisch (funktioniert mit jedem Task-Management-Tool über Interface)

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Materialkoordination ist einer der größten Zeitfresser auf der Baustelle.*