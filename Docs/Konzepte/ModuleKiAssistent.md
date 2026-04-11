---
doc_id: konzept-ki-assistent
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [ki, chatgpt, claude, lv-analyse, dokumentensuche, api]
read_when: [ki-feature, lv-analyse, dokumentensuche, externe-api-ki]
related_docs: [architektur, dsvgo-architektur, konzept-kalkulation]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für KI-Assistent — LV-Analyse, Dokumentensuche, ChatGPT/Claude API
- Autorität: secondary
- Lesen wenn: KI-Feature, LV-Analyse, Dokumentensuche, externe KI-API
- Nicht zuständig für: Plankopf-Extraktion (→ Moduleplanheader.md)
- Pflichtlesen: keine
- Fachliche Invarianten:
  - DSGVO Klasse C — unverarbeitete Dokumente an externe API
  - IExternalCommunicationService Pflicht (ADR-035)
  - User muss explizit zustimmen bevor Daten an API gesendet werden

---

﻿# Modul-Konzept: KI-Assistent für Bauprojekte

**Version:** 0.1.0  
**Erstellt:** 29.03.2026  
**Status:** Konzept  
**Abhängigkeiten:** ADR-027 (KI-API-Import), IKiImportService  
**Priorität:** Nach V1 (eigenständiges Modul)

---

## 1. Ziel

Ein eigenständiges KI-Modul innerhalb von BPM, das als intelligenter Fach-Assistent für Bauprojekte fungiert. Der Assistent arbeitet ausschließlich auf Basis der übergebenen Projektdaten — er erfindet keine Inhalte.

**Kernnutzen:**
- Schnell relevante Informationen in Projektdokumenten finden
- Leistungsverzeichnisse analysieren und bewerten
- Risiken und Unklarheiten erkennen
- Fundierte Entscheidungen bei Ausschreibung, Kalkulation und Nachtragsbewertung unterstützen

---

## 2. Einordnung in BPM

- Eigenes Projekt: `BauProjektManager.KiAssistent` (WPF Class Library)
- Eigener Tab in der Sidebar-Navigation (wie Settings, PlanManager)
- Nutzt gemeinsames `IKiImportService` Interface (ADR-027)
- Gleiche API-Key-Verwaltung und Provider-Auswahl wie KI-Import
- Dokumente kommen aus dem Projektordner (Pfade bereits in der DB)

---

## 3. Funktionsbereiche

### 3.1 Dokumenten-Analyse

Der Assistent kann folgende Projektdokumente analysieren:

- **Leistungsverzeichnisse** (Lang-LV und Kurz-LV)
- **Pläne** (PDF)
- **Baubeschreibungen**
- **Ausschreibungsunterlagen**
- **Nachträge**
- **Bereits strukturierte Daten** (Positionen, Mengen, Preise)

### 3.2 Fachliche Aufgaben

| Aufgabe | Beschreibung |
|---------|-------------|
| LV-Analyse | Positionen, Mengen, Zusammenhänge erkennen; Vorbemerkungen vs. Leistungspositionen trennen |
| Dokumentensuche | Relevante Stellen in übergebenen Dokumenten finden und zitieren |
| Bewertung | Risiko, Vollständigkeit, Plausibilität von Inhalten bewerten |
| Ausschreibung | Unterstützung bei Erstellung und Prüfung |
| Kalkulation | Mengenprüfung, Positionsvergleich |
| Nachtragsbewertung | Nachträge gegen Vertragsbestandteile prüfen |
| Technische Fragen | Fachliche Fragen zu Bauausführung beantworten |

### 3.3 LV-spezifische Funktionen

- Vorbemerkungen und Vertragsbedingungen von Leistungspositionen trennen
- LB-H Strukturen erkennen (österreichische Leistungsbeschreibung Hochbau)
- Wirtschaftlich relevante Positionen hervorheben:
  - Aufzahlungen
  - Nebenleistungen
  - Unklare Leistungsdefinitionen
  - Mögliche Nachtragspositionen

---

## 4. System-Prompt

Der KI-Assistent verwendet einen spezialisierten System-Prompt der als Template in der App gespeichert wird (versioniert, aktualisierbar). Kernregeln:

1. Ausschließlich übergebene Dokumente als Grundlage verwenden
2. Keine Inhalte erfinden (keine Positionen, Mengen oder Normen)
3. Bei fehlender Information klar melden
4. Strikt zwischen Vertrags-/Vorbemerkungstexten und Leistungspositionen unterscheiden
5. Konkrete Textstellen gegenüber Interpretation bevorzugen
6. Fachlich präzise und kompakt antworten

---

## 5. Ausgabeformat

Der Assistent liefert strukturierte JSON-Antworten:

```json
{
  "answer": "Kurze, präzise Antwort",
  "sources": [
    {
      "document": "Dokumentname",
      "page": 0,
      "section": "Abschnitt oder Position",
      "quote": "Relevanter Textauszug"
    }
  ],
  "analysis": [
    "Fachliche Bewertung",
    "z.B. Risiko, Unklarheit, Besonderheit"
  ],
  "uncertainties": [
    "Was ist unklar oder nicht eindeutig"
  ]
}
```

Die App parst dieses JSON und zeigt es formatiert an:
- Antwort als Haupttext
- Quellen als klickbare Verweise (Dokument + Seite)
- Analyse als farbcodierte Hinweise (Risiko = rot, Hinweis = gelb)
- Unsicherheiten als graue Fußnoten

---

## 6. UI-Konzept

### 6.1 Hauptansicht

- **Links:** Projektauswahl + Dokument-Upload-Bereich (Drag&Drop oder aus Projektordner wählen)
- **Mitte:** Chat-Interface (Frage stellen → strukturierte Antwort)
- **Rechts:** Quellen-Panel (zeigt referenzierte Dokumente/Seiten)

### 6.2 Dokument-Kontext

- Aktives Projekt wird automatisch aus BPM-Projektliste gewählt
- Dokumente aus dem Projektordner sind direkt verfügbar (Pfade in DB)
- Zusätzliche Dokumente per Drag&Drop oder Datei-Dialog hinzufügbar
- Hochgeladene Dokumente werden als Base64 an die KI-API gesendet

### 6.3 Antwort-Darstellung

- Strukturierte Antwort (nicht roher JSON-Text)
- Quellen als anklickbare Links (öffnet Dokument an der Stelle)
- Analyse-Punkte als farbcodierte Cards
- History der Fragen/Antworten pro Projekt (in SQLite speicherbar)

---

## 7. Technische Architektur

### 7.1 Provider

- **Primär: ChatGPT API** (OpenAI) — Herbert hat dort den Prompt entwickelt
- **Alternativ: Claude API** (Anthropic) — als Option in Systemeinstellungen
- Provider-Auswahl in den App-Einstellungen (gleicher Mechanismus wie ADR-027)

### 7.2 Service-Interface

```csharp
public interface IKiAssistentService
{
    Task<KiAssistentResponse> AskAsync(
        string question,
        List<ProjectDocument> documents,
        string systemPrompt,
        CancellationToken ct = default);
}
```

Nutzt intern `IKiImportService` für die API-Kommunikation.

### 7.3 Datenmodell

```csharp
public class ProjectDocument
{
    public string Name { get; set; }        // Dateiname
    public string Path { get; set; }        // Pfad im Projektordner
    public string ContentBase64 { get; set; } // Base64-kodierter Inhalt
    public string MimeType { get; set; }    // application/pdf, text/plain etc.
}

public class KiAssistentResponse
{
    public string Answer { get; set; }
    public List<SourceReference> Sources { get; set; }
    public List<string> Analysis { get; set; }
    public List<string> Uncertainties { get; set; }
}

public class SourceReference
{
    public string Document { get; set; }
    public int Page { get; set; }
    public string Section { get; set; }
    public string Quote { get; set; }
}
```

### 7.4 Prompt-Management

- System-Prompt als Template-Datei in der App (`Resources/Prompts/`)
- Versioniert (v1.0, v1.1...) — aktualisierbar ohne Code-Änderung
- Projekt-Kontext wird dynamisch eingefügt (Projektname, Dokumentenliste)
- User kann eigene Prompt-Anpassungen speichern (pro Projekt oder global)

---

## 8. Erweiterungsmöglichkeiten (Zukunft)

| Feature | Beschreibung |
|---------|-------------|
| LV-Vergleich | Zwei LVs gegenüberstellen (Ausschreibung vs. Angebot) |
| Mengenermittlung | KI prüft Mengen aus Plan gegen LV-Positionen |
| Nachtrags-Generator | Aus erkannten Abweichungen automatisch Nachtragspositionen vorschlagen |
| Bautagebuch-Unterstützung | Tagesbericht-Entwurf aus Wetter + Arbeitskräfte + Leistung generieren |
| Normen-Check | Prüfung gegen ÖNORM, Bauordnung (wenn Normentexte als Dokument hinterlegt) |
| Multi-Projekt-Analyse | Kennzahlen und Muster über mehrere Projekte erkennen |
| Sprach-Input | Frage per Mikrofon stellen (Speech-to-Text → KI → Antwort) |

---

## 9. Abgrenzung

**Was das Modul NICHT tut:**
- Keine eigenen Berechnungen (Statik, Kalkulation) — nur Analyse bestehender Daten
- Keine Normentexte generieren — nur auf übergebene Dokumente referenzieren
- Keine automatischen Entscheidungen — immer nur Empfehlungen an den User
- Kein Ersatz für Fachkompetenz — Werkzeug zur schnelleren Informationsfindung

---

## 10. Abhängigkeiten und Voraussetzungen

| Voraussetzung | Status |
|---------------|--------|
| IKiImportService Interface (ADR-027) | ⬜ Konzept |
| API-Key-Verwaltung in Systemeinstellungen | ⬜ Konzept |
| ChatGPT API Integration | ⬜ Konzept |
| Claude API Integration (optional) | ⬜ Konzept |
| Projektordner-Pfade in DB | ✅ Vorhanden |
| PDF-Handling (Base64, Upload) | ⬜ Noch nicht implementiert |

---

*Konzept wird bei Implementierungsbeginn detailliert und in einzelne Features aufgeteilt.*
