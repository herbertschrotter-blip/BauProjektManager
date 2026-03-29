# BauProjektManager — Modul: Plankopf-Erkennung (PlanHeader)

**Erstellt:** 29.03.2026  
**Version:** 0.1 (Konzeptentwurf)  
**Status:** Vision — Umsetzung nach V1 (PlanManager-Basis muss stehen)  
**Abhängigkeit:** PdfPig (bereits im Tech-Stack), PlanManager Import-Workflow

---

## 1. Zielsetzung

Automatisches Auslesen von Plankopf-Daten aus PDF-Bauplänen beim Import. Zwei Kernziele:

1. **Revisions-Info für Dashboard:** Änderungsgrund aus Revisionstabelle extrahieren (z.B. "Schnitt 33-34 ergänzt, Pos 86-88 neu") → im Dashboard bei "Letzte Planänderungen" anzeigen
2. **Validierung gegen Dateinamen:** Plannummer + Index aus Plankopf gegen Dateinamen-Parser prüfen → bei Widerspruch User fragen

---

## 2. Kernkonzept: Claude-gestütztes Anlernen

### Das Problem mit klassischem Template-Anlernen

Jedes Planungsbüro hat ein anderes Plankopf-Layout. Ein GUI-basiertes Anlernen (Zonen klicken, Felder zuweisen) wäre extrem aufwändig zu entwickeln und würde selten benutzt — man lernt ein Büro einmal an, danach läuft es.

### Die Lösung: Claude als Anlern-Assistent

Statt ein komplexes Template-System in der GUI zu bauen, nutzt der User Claude (oder ein anderes LLM) zum Anlernen neuer Layouts. BPM stellt einen **vorkonfigurierten Prompt** bereit, der User lädt den PDF bei Claude hoch, und Claude liefert einen **fertigen Regel-Eintrag** (JSON) der direkt in BPM eingefügt wird.

### Workflow

```
NEUES BÜRO (Layout unbekannt)
    │
    ▼
┌────────────────────────────────────┐
│ BPM: Button "Plankopf anlernen"   │
│ → Zeigt vorkonfigurierten Prompt  │
│ → User kopiert Prompt             │
└──────────────┬─────────────────────┘
               │
               ▼
┌────────────────────────────────────┐
│ Claude: User lädt PDF hoch +      │
│         pastet Prompt             │
│                                    │
│ Claude analysiert:                │
│ - Wo steht der Plankopf?         │
│ - Welche Felder gibt es?         │
│ - Wie heißt das Büro/die Firma?  │
│ - Wo steht die Revisionstabelle? │
│ - Welches Muster haben die Daten?│
│                                    │
│ Claude liefert:                   │
│ → Fertigen JSON-Eintrag          │
│ → Copy-Paste ready               │
└──────────────┬─────────────────────┘
               │
               ▼
┌────────────────────────────────────┐
│ BPM: User fügt JSON ein           │
│ → Button "Regel einfügen"        │
│ → Wird in header-templates.json   │
│   gespeichert                     │
│ → Ab jetzt automatische Erkennung │
└────────────────────────────────────┘

BEKANNTES BÜRO (Layout bereits angelernt)
    │
    ▼
┌────────────────────────────────────┐
│ BPM: Plan-Import läuft            │
│ → PdfPig liest Text + Positionen │
│ → Firmenname im Plankopf erkannt │
│ → Passendes Template angewendet  │
│ → Revisions-Info extrahiert      │
│ → Plannummer/Index validiert     │
│                                    │
│ Ergebnis im Dashboard:           │
│ "B-221-B Index B — Schnitt 33-34 │
│  ergänzt, Pos 86-88 neu"         │
└────────────────────────────────────┘
```

---

## 3. Der Prompt (vorkonfiguriert in BPM)

Diesen Prompt zeigt BPM dem User an, wenn er "Plankopf anlernen" drückt. Der User kopiert ihn und verwendet ihn in Claude zusammen mit dem hochgeladenen PDF.

```
Ich baue ein Bau-Planmanagement-Tool. Ich brauche eine Regel 
um den Plankopf dieses PDF-Plans automatisch auszulesen.

Analysiere den Plankopf (meist unten rechts) und erstelle mir 
einen JSON-Eintrag mit folgendem Format:

{
  "templateId": "<eindeutige-id>",
  "templateName": "<Büroname + Plantyp>",
  "identifyingTexts": ["<Firmenname>", "<weitere erkennbare Texte>"],
  "fields": {
    "planNumber": {
      "searchKeywords": ["<Wörter die neben/über der Plannummer stehen>"],
      "position": "<right|below|sameLine>",
      "regex": "<Muster für den Wert>",
      "example": "<Beispielwert aus diesem Plan>"
    },
    "planIndex": {
      "searchKeywords": ["<Wörter die neben/über dem Index stehen>"],
      "position": "<right|below|sameLine>",
      "regex": "<Muster>",
      "example": "<Beispielwert>"
    },
    "planTitle": {
      "searchKeywords": ["<...>"],
      "position": "<...>",
      "example": "<...>"
    },
    "date": {
      "searchKeywords": ["<...>"],
      "position": "<...>",
      "regex": "\\d{1,2}\\.\\d{2}\\.\\d{4}",
      "example": "<...>"
    },
    "drawnBy": {
      "searchKeywords": ["<...>"],
      "position": "<...>",
      "example": "<...>"
    },
    "project": {
      "searchKeywords": ["<...>"],
      "position": "<...>",
      "example": "<...>"
    },
    "client": {
      "searchKeywords": ["<...>"],
      "position": "<...>",
      "example": "<...>"
    }
  },
  "revisionTable": {
    "exists": true/false,
    "headerKeywords": ["Index", "Datum", "Änderung"],
    "columnOrder": ["index", "date", "author", "description"],
    "example": [
      { "index": "A", "date": "12.09.2025", "description": "Planerstellung" },
      { "index": "B", "date": "26.09.2025", "description": "Schnitt 33-34 ergänzt" }
    ]
  }
}

REGELN:
- Nur Felder ausfüllen die du im Plankopf findest
- Bei "position": "right" = Wert steht rechts vom Keyword, 
  "below" = Wert steht darunter, "sameLine" = gleiche Zeile
- Bei "identifyingTexts": Firmenname, Adresse, oder andere 
  eindeutige Texte die dieses Büro identifizieren
- Bei "regex": nur wenn das Muster klar erkennbar ist
- Liefere NUR den JSON-Block, keine Erklärung drum herum
```

---

## 4. Beispiel: Claude-Analyse des Spörk ZT Plans (B-221-B)

**Input:** PDF von `B-221-B_1_OG_Decke_Teil_1_Bewehrung.pdf` + Prompt

**Output von Claude (direkt einfügbar in BPM):**

```json
{
  "templateId": "spoerk-zt-bewehrung",
  "templateName": "Spörk ZT GmbH — Bewehrungsplan",
  "identifyingTexts": [
    "Spörk ZT GmbH",
    "Brühlgasse 37",
    "A-8230 Hartberg"
  ],
  "fields": {
    "planNumber": {
      "searchKeywords": ["Planummer"],
      "position": "below",
      "regex": "[A-Z]-\\d{3}-[A-Z]",
      "example": "B-221-B"
    },
    "planIndex": {
      "searchKeywords": ["Index"],
      "position": "right",
      "regex": "[A-Z]",
      "example": "B"
    },
    "planTitle": {
      "searchKeywords": ["Planinhalt"],
      "position": "below",
      "example": "Bewehrung Decke 1.Obergeschoss Abschnitt Teil 1"
    },
    "date": {
      "searchKeywords": ["Datum"],
      "position": "right",
      "regex": "\\d{2}\\.\\d{2}\\.\\d{4}",
      "example": "12.09.2025"
    },
    "drawnBy": {
      "searchKeywords": ["Gezeichnet"],
      "position": "right",
      "example": "OB"
    },
    "project": {
      "searchKeywords": ["Projekt"],
      "position": "below",
      "example": "Neubau Bürogebäude Grenzgasse 8054 Seiersberg"
    },
    "client": {
      "searchKeywords": ["Auftraggeber"],
      "position": "below",
      "example": "Arch & Living"
    }
  },
  "revisionTable": {
    "exists": true,
    "headerKeywords": ["Änderungen", "Datum", "Index"],
    "columnOrder": ["description", "author", "date", "index"],
    "example": [
      {
        "index": "A",
        "date": "12.09.2025",
        "author": "OB",
        "description": "Planerstellung"
      },
      {
        "index": "B",
        "date": "26.09.2025",
        "author": "OB",
        "description": "Schnitt 33-33, 34-34 ergänzt, Pos 86, 87, 88 neu"
      }
    ]
  }
}
```

---

## 5. Technische Umsetzung in BPM

### 5.1 Speicherort

`header-templates.json` in OneDrive `.AppData/BauProjektManager/` — syncht zwischen Geräten, selten geändert (wie pattern-templates.json für Dateinamen).

### 5.2 Automatische Erkennung beim Import

```
PDF wird importiert
    │
    ▼
PdfPig: Extrahiere alle Wörter aus unterem 25% der Seite
    │
    ▼
Vergleiche Wörter mit identifyingTexts aller Templates
    │
    ├── Match gefunden (z.B. "Spörk ZT GmbH" erkannt)
    │   │
    │   ▼
    │   Wende Template an:
    │   - Suche searchKeywords für jedes Feld
    │   - Extrahiere Wert je nach position (right/below/sameLine)
    │   - Validiere mit regex wenn vorhanden
    │   - Parse Revisionstabelle wenn exists=true
    │   │
    │   ▼
    │   Ergebnis: PlanHeaderData Objekt
    │   - Validiere gegen Dateinamen-Parser
    │   - Speichere Revisions-Info in SQLite
    │
    └── Kein Match
        │
        ▼
        Stille Warnung im Log: "Kein Plankopf-Template erkannt"
        Plan wird trotzdem normal importiert (nur ohne Plankopf-Daten)
        Optional: Im Dashboard "Plankopf anlernen" Button anzeigen
```

### 5.3 Datenmodell (Erweiterung)

```csharp
// Ergänzung zum bestehenden Plan-Cache in planmanager.db
public class PlanHeaderData
{
    public string PlanNumber { get; set; }       // Aus Plankopf (zur Validierung)
    public string PlanIndex { get; set; }        // Aus Plankopf (zur Validierung)
    public string? PlanTitle { get; set; }
    public string? Date { get; set; }
    public string? DrawnBy { get; set; }
    public string? Project { get; set; }
    public string? Client { get; set; }
    public string? TemplateId { get; set; }      // Welches Template wurde verwendet
    public List<RevisionEntry> Revisions { get; set; } = [];
}

public class RevisionEntry
{
    public string Index { get; set; } = "";
    public string? Date { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }     // DAS ist der Wert fürs Dashboard
}
```

### 5.4 Dashboard-Integration

```
┌─ Letzte Planänderungen ────────────────────────────────────┐
│                                                            │
│ B-221-B  Index B  26.09.2025  Spörk ZT                   │
│ → Schnitt 33-34 ergänzt, Pos 86-88 neu                    │
│                                                            │
│ S-103-D  Index D  15.03.2026  (kein Plankopf-Template)    │
│ → (Änderungsgrund nicht verfügbar)                        │
│                                                            │
│ PP_GG_01  Index B  14.10.2025  Innerwald Architektur      │
│ → generelle Überarbeitung                                 │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 6. GUI in BPM

### 6.1 Button "Plankopf anlernen" (in Einstellungen oder PlanManager)

- Zeigt den vorkonfigurierten Prompt in einem Textfeld (read-only)
- "In Zwischenablage kopieren" Button
- Darunter: Textfeld "JSON hier einfügen" + "Speichern" Button
- Validierung: Prüft ob JSON gültig ist, ob templateId eindeutig ist

### 6.2 Template-Verwaltung (in Einstellungen)

- Liste aller gespeicherten Templates
- Pro Template: Name, Büro, Anzahl erkannter Pläne, letzter Treffer
- Bearbeiten-Button: JSON editierbar (für Korrekturen)
- Löschen-Button
- "Neu scannen" Button: Template auf alle bestehenden PDFs anwenden

### 6.3 Validierungs-Dialog (beim Import)

```
┌─ Plankopf-Validierung ──────────────────────────────────┐
│                                                          │
│ ⚠️ Widerspruch bei Plan S-103:                          │
│                                                          │
│ Dateiname sagt:  Index C                                │
│ Plankopf sagt:   Index D                                │
│                                                          │
│ Was stimmt?                                             │
│ ○ Dateiname (Index C)                                   │
│ ○ Plankopf (Index D)                                    │
│ ○ Überspringen (beide ignorieren)                       │
│                                                          │
│              [ Übernehmen ]    [ Abbrechen ]            │
└──────────────────────────────────────────────────────────┘
```

---

## 7. Fehlerbehandlung

| Situation | Verhalten |
|-----------|-----------|
| Kein Template passt | Plan wird normal importiert, ohne Plankopf-Daten. Stille Log-Warnung. |
| Template passt, aber Feld nicht gefunden | Feld bleibt leer, andere Felder werden trotzdem extrahiert |
| Revisionstabelle nicht parsebar | Nur Plannummer/Index werden extrahiert, Revisionen bleiben leer |
| Widerspruch Dateiname vs. Plankopf | Dialog fragt User (siehe oben) |
| JSON vom User ist ungültig | Validierung mit Fehlermeldung, nicht speichern |
| Template erkennt falsches Büro | User kann im Template-Manager korrigieren, identifyingTexts anpassen |
| Neuer Scan nach Template-Korrektur | Button "Neu scannen" wendet korrigiertes Template auf alle PDFs an |

---

## 8. Vorteile dieses Ansatzes

| Vorteil | Erklärung |
|---------|-----------|
| **Minimaler Entwicklungsaufwand** | Kein komplexes GUI-Anlernsystem nötig. PdfPig + JSON-Matching reicht |
| **Claude macht die schwere Arbeit** | Layout-Analyse, Feld-Erkennung, Regex-Erstellung — alles im Prompt |
| **Editierbar** | User kann JSON jederzeit anpassen wenn Claude was falsch erkannt hat |
| **Skaliert** | Neues Büro = ein neuer Claude-Chat. 5 Minuten statt Stunden Entwicklung |
| **Zukunftssicher** | Wenn Claude besser wird, werden auch die Templates besser |
| **Offline nach Anlernen** | Nur das Anlernen braucht Claude. Die Erkennung läuft offline in BPM |

---

## 9. Architektur-Implikationen

- **Neues JSON-File:** `header-templates.json` auf OneDrive (syncht)
- **PdfPig bereits im Stack:** Kein neues NuGet-Paket nötig
- **Erweiterung planmanager.db:** Neue Tabelle/Spalten für PlanHeaderData + Revisions
- **Dashboard-Integration:** Revisions-Info als Datenquelle für "Letzte Planänderungen"
- **Import-Workflow:** Schritt 2 (Parse) wird erweitert um Plankopf-Analyse (optional, nur wenn Template vorhanden)

---

## 10. Abgrenzung

- **Kein OCR:** Alle PDFs sind Text-basiert (aus CAD exportiert). OCR ist nicht nötig.
- **Keine KI in BPM:** Claude wird nur extern zum Anlernen verwendet. BPM selbst macht nur JSON-Matching + Regex.
- **Kein Zwang:** Plankopf-Erkennung ist optional. Ohne Templates funktioniert der Import wie bisher (nur Dateinamen-Parser).
- **Keine automatische Template-Erstellung:** Templates werden immer von Claude generiert und vom User eingefügt — nie automatisch.

---

*Umsetzung: nach V1 (PlanManager-Import muss funktionieren). Kann schrittweise eingeführt werden — erst Template-Verwaltung, dann Import-Integration, dann Dashboard.*
