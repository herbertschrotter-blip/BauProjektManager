# BauProjektManager — Vision & Produktstrategie

**Erstellt:** 29.03.2026  
**Version:** 1.0  
**Autor:** Herbert Schrotter + Claude  

---

## 1. Der Nordstern

**Eine einzige Desktop-Anwendung für den gesamten Arbeitsalltag auf der Baustelle und im Büro.**

BauProjektManager (BPM) ersetzt das tägliche Jonglieren zwischen Explorer-Ordnern, Excel-Tabellen, Outlook-Makros, Papier-Bautagebüchern und manuellen Prozessen durch ein einheitliches, offline-fähiges Werkzeug. Alles an einem Ort — Pläne, Projekte, Bautagebuch, Zeiterfassung, Fotos, Kontakte.

---

## 2. Das Problem

### 2.1 Schmerzpunkte im Arbeitsalltag

Ein Polier / Bauleiter in Österreich (Steiermark) verbringt täglich Zeit mit Aufgaben, die repetitiv, fehleranfällig und über viele Programme verstreut sind:

**Pläne sortieren und versionieren kostet zu viel Zeit.**  
Neue Pläne kommen per E-Mail, Portal-Download oder USB-Stick — unsortiert, alle auf einmal. Welcher Plan ist neu? Welcher hat einen neuen Index? Welcher ist identisch mit dem bestehenden? Das manuell zu prüfen und in die richtige Ordnerstruktur zu verschieben, dauert pro Lieferung 15–30 Minuten. Bei mehreren Lieferungen pro Woche summiert sich das.

**Bautagebuch schreiben ist mühsam.**  
Das tägliche Bauprotokoll wird auf Papier, in Excel oder gar nicht geschrieben. Wetterdaten manuell nachschlagen, Arbeitskräfte aufzählen, Tätigkeiten beschreiben — jeden Tag dasselbe Gerüst, nur der Inhalt ändert sich. Viel davon könnte vorausgefüllt werden.

**Projektinfos sind überall verstreut.**  
Adresse? Im Vertrag. Kontakt Auftraggeber? In Outlook. Grundstücksnummern? Im Bescheid. Welche Pläne sind aktuell? Im Ordner nachschauen. Baustart? Im Kalender. Es gibt keinen zentralen Ort, wo alle Projektinformationen auf einen Blick verfügbar sind.

**Ordnerstruktur für neue Projekte jedes Mal neu anlegen.**  
Für jedes neue Projekt wird im Explorer manuell die gleiche Ordnerstruktur angelegt: Planunterlagen, Fotos, Leica, DOKA, LV, Protokolle. Copy-Paste vom letzten Projekt, Umbenennen, Unterordner anlegen. Fehleranfällig und langweilig.

**Arbeitszeiterfassung ist umständlich.**  
Stunden werden handschriftlich oder in unübersichtlichen Excel-Tabellen erfasst. Baustellen-Zuordnung fehlt oder ist inkonsistent. Das Lohnbüro braucht die Daten in einem bestimmten Format. Überstundenberechnung ist Formel-Chaos in Excel.

### 2.2 Was heute verwendet wird

| Aufgabe | Aktuelles Werkzeug | Problem |
|---------|-------------------|---------|
| Pläne sortieren | Windows Explorer (manuell) | Zeitaufwändig, fehleranfällig |
| Planversionen prüfen | Dateinamen vergleichen (Augen) | Übersehen möglich |
| Bautagebuch | Papier / Excel / gar nicht | Keine Vorausfüllung, kein Export |
| Projektdaten | Vertrag, Outlook, Kopf | Verstreut, nicht durchsuchbar |
| Ordner anlegen | Explorer (Copy-Paste) | Repetitiv, inkonsistent |
| Zeiterfassung | Handschrift / Excel | Fehleranfällig, kein Dropdown |
| E-Mail-Anhänge | Outlook + manuelles Speichern | Kein automatisches Einsortieren |
| Fotos zuordnen | Explorer nach Datum | Keine Tags, keine Geodaten-Prüfung |
| Kontakte | Outlook + Kopf | Nicht projektübergreifend nutzbar |

### 2.3 Warum nicht bestehende Software?

Es gibt Bau-Management-Software (PlanRadar, Dalux, BauMaster etc.). Aber:

- **Cloud-Zwang:** Erfordern Internet, Abo, Daten liegen extern
- **Zu komplex:** Für große Firmen gedacht, für einen Polier/Bauleiter Overkill
- **Nicht anpassbar:** Eigene Ordnerstrukturen, eigene Plantyp-Muster nicht möglich
- **Kein Offline:** Auf der Baustelle gibt es oft kein WLAN
- **Kosten:** Monatliche Abos, pro Nutzer, pro Projekt

BPM ist das Gegenteil: lokal, offline, anpassbar, keine laufenden Kosten, eigene Daten auf dem eigenen Cloud-Speicher (OneDrive, Google Drive, Dropbox etc.) oder komplett ohne Cloud.

---

## 3. Die Lösung

### 3.1 Was BPM ist

Ein modulares Desktop-Werkzeug (Windows, WPF) das schrittweise den gesamten Arbeitsalltag eines Poliers/Bauleiters digitalisiert. Eine einzige `.exe`, alle Module integriert, Daten lokal im Dateisystem. Synchronisation über jeden Cloud-Speicher der sich als Ordner im Explorer einblendet (OneDrive, Google Drive, Dropbox, Synology Drive, Nextcloud etc.).

### 3.2 Kern-Prinzipien

**Offline-first.**  
BPM funktioniert komplett ohne Internet. Alle Daten liegen lokal. Cloud-Speicher (OneDrive, Google Drive, Dropbox etc.) synchronisieren im Hintergrund wenn verfügbar — aber die App braucht keinen Cloud-Dienst zum Arbeiten.

**Eigene Daten, eigene Kontrolle.**  
Kein Cloud-Dienst, kein Abo, keine Daten bei Dritten. Projektordner sind saubere Windows-Ordner die jeder Kollege im Explorer öffnen kann. Nichts ist in einer proprietären Datenbank versteckt.

**Anpassbar, nicht starr.**  
Jedes Projekt hat andere Plantyp-Muster, andere Ordnerstrukturen, andere Anforderungen. BPM lernt die Muster pro Projekt an statt ein starres Schema vorzugeben.

**Schrittweise ersetzen, nicht alles auf einmal.**  
BPM ersetzt bestehende Werkzeuge Modul für Modul. Excel-Vorlagen, Outlook-Makros und PowerShell-Tools laufen parallel weiter, bis das entsprechende BPM-Modul sie ablöst. Kein Big-Bang-Umstieg.

**Kollegentauglich.**  
Kollegen in der Firma sollen BPM selbst öffnen und nutzen können — nicht nur die Ergebnisse sehen. Das bedeutet: verständliche GUI, deutsche Oberfläche, kein Konfigurationsaufwand für Endnutzer.

### 3.3 Leitbild (in einem Satz)

> *"Ich öffne eine App, sehe alle meine Baustellen, klicke auf ein Projekt, und habe alles was ich brauche — Pläne, Bautagebuch, Fotos, Kontakte, Zeiterfassung — auf einen Blick."*

---

## 4. Zielgruppe

### 4.1 Primärer Nutzer (Herbert)

- **Rolle:** Polier und Bauleiter, Steiermark, Österreich
- **Geräte:** PC zuhause (Hauptarbeitsplatz), Laptop auf der Baustelle, Smartphone (später)
- **Sync:** Cloud-Speicher (z.B. OneDrive), zwei Geräte
- **Technisch:** Kein Programmierer, aber digital versiert. Baut eigene VBA-Makros, PowerShell-Tools, AutoLISP-Skripte
- **Projekte:** 2–5 aktive Baustellen gleichzeitig (Wohnbau, Reihenhäuser, Sanierungen)
- **Arbeitsweise:** Strukturiert, liebt Ordnung, nummerierte Ordner, klare Systeme

### 4.2 Sekundäre Nutzer (Kollegen in der Firma)

- **Rolle:** Andere Poliere, Bauleiter, Techniker
- **Erwartung:** App installieren, Projektordner sehen, Pläne finden, Bautagebuch schreiben
- **Technisch:** Weniger versiert als Herbert. GUI muss selbsterklärend sein
- **Einschränkung:** Konfiguration und Anlernen (Plantyp-Profile) macht Herbert. Kollegen nutzen das Ergebnis

### 4.3 Indirekte Nutzer

- **Lohnbüro:** Liest Zeiterfassungsdaten aus Excel (über OneDrive)
- **Auftraggeber/Planer:** Sehen saubere Projektordner, bekommen exportierte Planlisten
- **Bestehende VBA-Makros:** Lesen `registry.json` für Outlook-Ordner und Excel-Vorlagen

---

## 5. Module und Entwicklungshorizont

### 5.1 Übersicht

```
┌─────────────────────────────────────────────────────────────┐
│                    BauProjektManager.exe                     │
│                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────────────┐  │
│  │ Einstellungen│ │ PlanManager │ │ Dashboard            │  │
│  │ (Projekte,   │ │ (Sortieren, │ │ (Zentrale Übersicht, │  │
│  │  Ordner,     │ │  Versionen, │ │  Widgets, Schnell-   │  │
│  │  Pfade)      │ │  Profile)   │ │  zugriff)            │  │
│  └─────────────┘ └─────────────┘ └──────────────────────┘  │
│                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────────────┐  │
│  │ Bautagebuch │ │ Zeiterfas-  │ │ Foto-Management      │  │
│  │ (Tägliches  │ │ sung (WPF → │ │ (Viewer, Tags,       │  │
│  │  Protokoll, │ │ Excel,      │ │  Geodaten, Bau-      │  │
│  │  Auto-Fill)  │ │ ClosedXML)  │ │  bericht-Zuordnung) │  │
│  └─────────────┘ └─────────────┘ └──────────────────────┘  │
│                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────────────┐  │
│  │ Outlook     │ │ Wetter      │ │ Vorlagen             │  │
│  │ (COM, Ordner│ │ (API pro    │ │ (Excel/Word mit      │  │
│  │  Anhänge)   │ │  Baustelle) │ │  Projektdaten)       │  │
│  └─────────────┘ └─────────────┘ └──────────────────────┘  │
│                                                             │
│  ┌─────────────┐ ┌──────────────────────────────────────┐   │
│  │ KI-Assistent│ │ Mobile PWA                           │   │
│  │ (LV-Analyse,│ │ (Bautagebuch + Plan-Viewer,          │   │
│  │  Dokumente, │ │  offline)                            │   │
│  │  ChatGPT/   │ └──────────────────────────────────────┘   │
│  │  Claude API)│                                            │
│  └─────────────┘                                            │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Entwicklungshorizont

| Horizont | Zeitraum | Module | Ziel |
|----------|----------|--------|------|
| **Jetzt** (V1) | 2026 | Einstellungen, PlanManager | Kernproblem lösen: Pläne automatisch sortieren und versionieren |
| **Bald** (V1.1) | 2026 | Planlisten Import/Export, Adressbuch, Suche | Vollständiges Planmanagement |
| **Nächste Welle** | 2026–2027 | Dashboard, Bautagebuch, Zeiterfassung, Foto | Täglichen Arbeitsalltag abdecken |
| **Danach** | 2027+ | Outlook, Wetter, Vorlagen, Mobile PWA | Ökosystem vervollständigen |
| **Langfristig** | offen | GIS-Integration, Kalender, Bestellungen, ClickUp | Vision Dashboard mit allem |

### 5.3 Was jedes Modul löst

| Modul | Schmerzpunkt | Lösung |
|-------|-------------|--------|
| **PlanManager** | Pläne sortieren kostet 15–30 Min pro Lieferung | Automatisch einsortieren nach angelerntem Muster, Index-Vergleich, Archivierung |
| **Einstellungen** | Ordnerstruktur manuell anlegen | Nummerierte Ordner per Klick erstellen, Template anpassbar |
| **Dashboard** | Projektinfos überall verstreut | Alles auf einen Blick: Pläne, Fotos, Kontakte, Links, Kennzahlen |
| **Bautagebuch** | Mühsames tägliches Protokoll | Auto-Befüllung (Wetter, Personal, Pläne), schnelle Eingabe, PDF-Export |
| **Zeiterfassung** | Umständliche Excel-Eingabe | Schöne WPF-Maske mit Dropdowns, schreibt direkt in Excel |
| **Foto** | Fotos manuell zuordnen | Tags, Geodaten-Prüfung, Baubericht-Integration |
| **Outlook** | Anhänge manuell speichern | Automatisch in `_Eingang` extrahieren, Projektordner sync |
| **Wetter** | Manuell nachschlagen | Automatisch pro Baustelle, Betonierfreigabe |
| **Vorlagen** | Excel/Word manuell befüllen | Projektdaten automatisch in Vorlagen einfügen |
| **KI-Assistent** | LV-Analyse und Dokumentensuche manuell und langsam | Fach-KI analysiert Projektdokumente, erkennt Risiken, findet Positionen (ChatGPT/Claude API) |
| **Mobile** | Kein Zugriff auf der Baustelle | Bautagebuch + Pläne am Handy, offline-fähig |

---

## 6. Differenzierung

### 6.1 BPM vs. bestehende Bau-Software

| Eigenschaft | PlanRadar / Dalux / BauMaster | BPM |
|-------------|-------------------------------|-----|
| Betrieb | Cloud, Abo, Internet nötig | Lokal, offline, kostenlos |
| Daten | Auf fremden Servern | Eigene Ordner auf eigenem Cloud-Speicher |
| Ordnerstruktur | Vorgegeben | Frei konfigurierbar |
| Plantyp-Muster | Fest oder KI-basiert | Vom User angelernt, transparent |
| Zielgruppe | Große Firmen, Teams | Polier/Bauleiter, kleine Firmen |
| Kosten | €50–200/Monat/Nutzer | Kostenlos (selbst gebaut) |
| Anpassbarkeit | Eingeschränkt | Vollständig (eigener Code) |
| VBA-Integration | Keine | registry.json für Outlook/Excel |

### 6.2 Was BPM bewusst NICHT ist

- **Kein Projektmanagement-Tool** (kein Gantt, kein Kanban — dafür gibt es ClickUp)
- **Kein CAD-Programm** (Pläne werden nur sortiert und angezeigt, nicht bearbeitet)
- **Kein ERP-System** (keine Buchhaltung, keine Angebotserstellung)
- **Kein Multi-Mandanten-System** (eine Installation, eine Firma)
- **Kein Cloud-Service** (bewusste Entscheidung, kein Kompromiss)

---

## 7. Das Dashboard — die langfristige Vision

Das Dashboard ist die zentrale Ansicht, in der alles zusammenläuft. Sidebar zeigt die Projektliste, Klick auf ein Projekt zeigt alles auf einen Blick:

**Kennzahlen:** Pläne gesamt, neue Pläne diese Woche, Fotos diesen Monat, Baufortschritt.

**Schnellzugriff:** Buttons für jeden Projektordner (01 Planunterlagen, 02 Fotos etc.) — Klick öffnet im Explorer. Häufigste Ordner oben.

**Letzte Planänderungen:** Liste der zuletzt importierten/geänderten Pläne mit Datum und Index.

**Externe Portale:** Konfigurierbare Link-Buttons je Auftraggeber (InfoRaum, PlanFred, PlanRadar). Nur anzeigen wenn konfiguriert, Portal-Info kommt aus Firmendaten.

**Konfigurierbare Toolbar:** Programme direkt starten (AutoCAD, Excel, Outlook, Leica Infinity). Konfigurierbar in Einstellungen.

**KI-Assistent:** Frage zu LV, Plänen oder Baubeschreibung stellen — KI durchsucht Projektdokumente und liefert strukturierte Antworten mit Quellenangaben. ChatGPT oder Claude API (konfigurierbar).

**Wetter-Widget:** Aktuelles Wetter und Vorhersage für die Baustellenadresse.

**Bautagebuch-Status:** Heutiger Eintrag begonnen? Letzte Einträge.

---

## 8. Technische Leitplanken

Diese Prinzipien gelten für alle zukünftigen Module und Entscheidungen:

| Prinzip | Bedeutung |
|---------|-----------|
| **Offline-first** | Jede Funktion muss ohne Internet funktionieren |
| **Lokal vor Cloud** | Daten im eigenen Dateisystem, sync über beliebigen Cloud-Speicher (OneDrive, Google Drive, Dropbox etc.) |
| **Saubere Ordner** | Projektordner müssen im Explorer für Kollegen lesbar bleiben — keine proprietären Dateien sichtbar |
| **VBA-kompatibel** | registry.json als Brücke zu bestehenden Makros, bis diese abgelöst werden |
| **Modular** | Jedes Modul ist ein eigenes C#-Projekt, unabhängig entwickelbar |
| **Stabilität vor Features** | Lieber weniger Module die funktionieren als viele halbfertige |
| **GUI-only** | Kein Terminal, keine Kommandozeile. Alles über die Oberfläche |
| **Schrittweise** | Ein Feature nach dem anderen. Testen nach jedem Schritt |
| **KI-gestützt** | Datenextraktion aus Dokumenten per KI-API (ChatGPT/Claude), konfigurierbar in Einstellungen |
| **Datenschutz by Design** | Keine automatischen Daten-Uploads. Jede externe Kommunikation über IExternalCommunicationService (ADR-035). Datenklassifizierung A/B/C. IPrivacyPolicy austauschbar per Lizenz — Relaxed (intern) vs. Strict (kommerziell) (ADR-036). Details: DSVGO-Architektur.md |

---

## 9. Erfolgskriterien

### Wann ist V1 ein Erfolg?

- Pläne sortieren dauert 2 Minuten statt 20 (pro Lieferung)
- Neues Projekt anlegen inkl. Ordnerstruktur: 1 Klick statt 10 Minuten
- Planversion sofort sichtbar (neuer Index, veraltet, fehlend)
- Ein Kollege kann BPM installieren und Pläne sortieren ohne Anleitung von Herbert

### Wann ist die Gesamtvision ein Erfolg?

- Herbert öffnet morgens BPM und hat alles was er braucht — ohne Excel, ohne Outlook-Makros, ohne Explorer-Gefrickel
- Bautagebuch schreiben dauert 5 Minuten statt 20 (durch Vorausfüllung)
- Zeiterfassung: Stunden werden in 30 Sekunden pro Mitarbeiter eingetragen
- Ein neuer Kollege kann BPM nach 15 Minuten Einführung produktiv nutzen

---

## 10. Risiken und Gegenmaßnahmen

| Risiko | Wahrscheinlichkeit | Gegenmaßnahme |
|--------|-------------------|---------------|
| Projekt wird zu groß, Motivation sinkt | Mittel | Schrittweise Entwicklung, V1 radikal fokussiert. Jedes Modul bringt sofort Nutzen |
| Cloud-Speicher-Sync verursacht Konflikte | Niedrig | Dreistufige Strategie (ADR-004), SQLite lokal, atomische JSON-Writes |
| .NET 10 hat Kinderkrankheiten | Niedrig | WPF ist seit .NET 5 stabil, LTS-Support bis 2028 |
| Kollegen nehmen BPM nicht an | Mittel | Einfache GUI, kein Konfigurationsaufwand, sofortiger Nutzen sichtbar |
| VBA-Makros werden langfristig nicht mehr gebraucht | Niedrig | registry.json ist ein einfacher Export — kann jederzeit entfernt werden |
| Claude/KI-generierter Code hat Qualitätsprobleme | Mittel | Herbert testet alles selbst, ChatGPT als Zweit-Reviewer, Coding Standards |

---

## 11. Kommerzialisierung (langfristig, kein aktuelles Ziel)

BPM wird als internes Werkzeug entwickelt. Falls es irgendwann für andere Baufirmen interessant wird, wären folgende Voraussetzungen nötig:

- Mandantenfähigkeit (aktuell nicht geplant)
- Installer / Auto-Update (Feature im Backlog)
- Dokumentation für Endnutzer (nicht nur für Entwicklung)
- Lizenzmodell klären (ADR-034: Offline-Lizenzierung)
- Support-Konzept
- DSGVO-Compliance: StrictPrivacyPolicy automatisch aktiv bei kommerzieller Lizenz (ADR-036)
- Verarbeitungsverzeichnis, Datenschutzerklärung, Mitarbeiter-Informationsvorlage (siehe DSVGO-Architektur.md Kap. 15)

Das ist kein aktuelles Ziel und beeinflusst keine Architekturentscheidung. Die modulare Struktur und saubere Codebasis machen einen späteren Schritt in diese Richtung aber grundsätzlich möglich.

---

## 12. Zusammenfassung

**Was:** Eine Desktop-App die den Arbeitsalltag eines Poliers/Bauleiters von A bis Z digitalisiert.

**Warum:** Zu viel Zeit geht verloren mit manuellen, repetitiven Aufgaben die über zehn verschiedene Programme verstreut sind.

**Wie:** Modular, offline, lokal, schrittweise. Ein Modul nach dem anderen, jedes löst einen konkreten Schmerzpunkt.

**Für wen:** Herbert (Primärnutzer), Kollegen in der Firma (Sekundär), perspektivisch vielleicht andere Baufirmen.

**Nordstern:** *Eine App öffnen, alles haben.*

---

*Dieses Dokument beschreibt die Richtung, nicht den genauen Weg. Konkrete Features und Zeitpläne stehen im BACKLOG.md. Architekturentscheidungen im ADR.md.*
