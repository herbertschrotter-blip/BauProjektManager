# BauProjektManager — DSGVO-Architektur & Privacy Engineering

**Version:** 1.4  
**Erstellt:** 03.04.2026  
**Letzte Überarbeitung:** 03.04.2026  
**Nächste Pflicht-Revision:** Spätestens 03.04.2027 oder bei Gesetzesänderung  
**Status:** Verbindlicher Architekturstandard  
**Gilt für:** Alle Module, alle externen Anbindungen  
**Verwandte Dokumente:**
- [ADR.md](ADR.md) — ADR-035 (IExternalCommunicationService)
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — Kapitel 17 (Datenschutz im Code)
- [BauProjektManager_Architektur.md](BauProjektManager_Architektur.md) — Speicherstrategie, Solution-Struktur
- Modul-Konzepte: [ModuleKiAssistent.md](Konzepte/ModuleKiAssistent.md), [ModuleZeiterfassung.md](Konzepte/ModuleZeiterfassung.md)

---

## Dokumentklassifizierung

Dieses Dokument unterscheidet:

- **PFLICHT** — Verbindliche Grundsätze, müssen vor Implementierung des betreffenden Moduls umgesetzt sein
- **EMPFOHLEN** — Best Practices, die schrittweise eingeführt werden sollen
- **OPTIONAL** — Zusätzliche Maßnahmen für erhöhtes Schutzniveau

---

## 1. Ziel & Geltungsbereich

Dieses Dokument definiert verbindlich:

- wie personenbezogene Daten in BPM klassifiziert werden
- wie externe Datenflüsse kontrolliert werden
- welche Architekturschicht Datenschutz steuert
- welche Regeln für jedes Modul gelten
- wie Schutzmaßnahmen regelmäßig überprüft werden

**Grundsatz:**

> Datenschutz ist kein Feature das später dazukommt.
> Datenschutz ist eine Architekturschicht die vor dem ersten Online-Modul steht.

---

## 2. Kernprinzipien (PFLICHT — nicht verhandelbar)

### 2.1 Privacy by Design & Default (Art. 25 DSGVO)

Jedes Feature wird so gebaut, dass:

- minimale Daten verarbeitet werden
- Datenschutz standardmäßig aktiv ist (Opt-in für externe Dienste, nicht Opt-out)
- keine versteckte Datenverarbeitung erfolgt

### 2.2 Offline-First = Privacy-First

BPMs Offline-first-Architektur (ADR-004) ist gleichzeitig die stärkste Datenschutz-Maßnahme:

- Alle Daten lokal in SQLite (`bpm.db`, `planmanager.db`)
- Cloud-Speicher-Sync ist User-Entscheidung, nicht App-Pflicht
- Kein Account, kein Login, keine zentrale Datensammlung

Online-Funktionen (KI-Assistent, GIS, Wetter, Task-Management) sind **optional und explizit aktivierbar** über Einstellungen → Datenschutz & Externe Dienste.

### 2.3 Datenminimierung (Art. 5 Abs. 1 lit. c DSGVO)

Nur Daten die einen klaren funktionalen Zweck haben dürfen existieren.

**Konkret für BPM:**

| Datenfeld | Zweck | Notwendig? |
|---|---|---|
| Client.ContactPerson | Ansprechpartner für Projekt | ✅ Ja |
| Client.Email | Kommunikation | ✅ Ja |
| Employee.Personalnummer | Lohnbüro-Zuordnung | ✅ Ja |
| Employee.Geburtsdatum | Kein BPM-Nutzen | ❌ Nicht speichern |
| GPS-Koordinaten in Wetter-Abfrage | Wettervorhersage pro Baustelle | ✅ Ja |
| Vollständiges PDF an KI-API | LV-Analyse | ⚠️ Nur reduziert |

### 2.4 Explizite Datenfluss-Kontrolle

Jede Datenbewegung nach außen muss:

- durch eine bewusste User-Aktion ausgelöst werden (kein Auto-Sync an externe APIs)
- über `IExternalCommunicationService` laufen (ADR-035)
- im Audit-Log nachvollziehbar sein

### 2.5 Datenschutz als eigene Architekturschicht

Datenschutz-Logik ist nicht im UI versteckt und nicht über den Code verstreut. Der `IExternalCommunicationService` und der Privacy Control Layer sind zentrale Infrastruktur-Komponenten.

### 2.6 Regelmäßige Überprüfung (Art. 25 Abs. 1 — „Stand der Technik")

Art. 25 DSGVO verlangt Maßnahmen die dem „Stand der Technik" entsprechen. Das bedeutet: Dieses Dokument und die Schutzmaßnahmen werden regelmäßig überprüft.

**Revisions-Pflicht:**

| Auslöser | Aktion |
|---|---|
| Jährlich (spätestens am Jahrestag der letzten Revision) | Prüfen ob Maßnahmen noch dem Stand der Technik entsprechen |
| Neues Online-Modul wird implementiert | DSFA-Checkliste (Kapitel 16) für dieses Modul durcharbeiten |
| KI-Provider ändert AGB/DPA | Prüfen ob bestehende Konfiguration noch konform ist |
| Gesetzesänderung (DSGVO, DSG, ArbVG) | Betroffene Abschnitte aktualisieren |
| BPM soll an Dritte verkauft werden | Vollständige Prüfung: Art. 13/14-Pflichten, Verarbeitungsverzeichnis, AVVs |

---

## 3. Datenklassifizierung

Alle Daten in BPM werden in drei Klassen eingeteilt:

### KLASSE A — Kein Personenbezug

- Technische Metadaten (MD5-Hashes, Dateipfade, Ordnerstruktur)
- Generische Projektstruktur (Plantypen, Ordner-Templates)
- App-Einstellungen (`settings.json` ohne Kontaktdaten)
- Plan-Cache (`planmanager.db`: Dateinamen, Indexe, Hashes)

**Regel:** Kann frei verarbeitet werden, keine Einschränkungen.

### KLASSE B — Personenbezogene Daten (eigene Mitarbeiter / Geschäftskontakte)

| Datenquelle | Betroffene | Rechtsgrundlage | Informationspflicht |
|---|---|---|---|
| `clients` Tabelle (Name, E-Mail, Telefon) | Auftraggeber, Planer | Art. 6 Abs. 1 lit. f DSGVO (berechtigtes Interesse) | Art. 14 — bei Ersterfassung empfohlen |
| `project_participants` (Name, Rolle, Kontakt) | Subunternehmer, Behörden | Art. 6 Abs. 1 lit. f DSGVO | Art. 14 — bei Ersterfassung empfohlen |
| `employees` Tabelle (Name, Personalnummer) | Eigene Mitarbeiter | Art. 6 Abs. 1 lit. b DSGVO (Arbeitsvertrag) | Art. 13 — PFLICHT (Mitarbeiter-Info) |
| `tbl_Zeiten` Excel (Arbeitszeiten, Krankenstand) | Eigene Mitarbeiter | Art. 6 Abs. 1 lit. b + § 96 ArbVG | Art. 13 — PFLICHT + ggf. BV |
| `write_lock` Tabelle (Username, Machine-Name) | BPM-Nutzer | Art. 6 Abs. 1 lit. f DSGVO | Nicht erforderlich (technisch) |
| `registry.json` (Ansprechpartner, E-Mails) | Geschäftskontakte | Art. 6 Abs. 1 lit. f DSGVO | Art. 14 — bei Ersterfassung empfohlen |

**Informationspflichten (Art. 13/14 DSGVO):**

- **Mitarbeiter (Zeiterfassung):** PFLICHT. Vor erstmaliger Erfassung muss der Arbeitgeber informieren: welche Daten, Zweck, Rechtsgrundlage, Speicherdauer, Empfänger (Cloud-Anbieter wenn sync), Auskunfts-/Löschrechte. BPM stellt hierfür eine Vorlage bereit (Vorlagen-Modul, nach V1).
- **Geschäftskontakte:** EMPFOHLEN. Bei berechtigtem Interesse ist Information nicht Pflicht-sofort, aber spätestens innerhalb eines Monats nach Ersterfassung (Art. 14 Abs. 3). Pragmatisch: Info-Text in Angebots-/Vertrags-E-Mails der Firma aufnehmen.
- **Bei Betriebsrat:** § 96 ArbVG verlangt eine Betriebsvereinbarung für automatisierte Personalverarbeitung. Das betrifft das Zeiterfassungs-Modul wenn BPM in einer Firma mit Betriebsrat eingesetzt wird.

**Regel:** Darf lokal verarbeitet werden. Bei Cloud-Sync: User-Verantwortung. Bei externem API-Transfer: Warnung + Opt-in.

### KLASSE C — Sensible Daten / Daten Dritter

| Datenquelle | Inhalt | Warum kritisch |
|---|---|---|
| LVs (Leistungsverzeichnisse) | Namen von Subunternehmern, Ansprechpartner | Fremde Personendaten |
| Baubescheide (PDF) | Grundstückseigentümer, Nachbar-Einwendungen | Sensible Drittdaten |
| Baubeschreibungen | Bauherr-Daten, Architekten | Fremde Personendaten |
| E-Mail-Anhänge (Outlook-Modul) | Absender, Inhalte, CC-Empfänger | Kommunikationsdaten |

**Regel:** Darf NIEMALS ungefiltert an externe APIs gesendet werden. Vor externer Verarbeitung: Anonymisierung oder explizite Freigabe durch User.

---

## 4. Architektur-Übersicht

### 4.1 Logische Schichten

```
┌─────────────────────────────────────────┐
│ UI Layer (WPF Views + ViewModels)       │
│  → Datenschutz-Hinweise, Opt-in Dialoge │
├─────────────────────────────────────────┤
│ Application Logic (Services)            │
│  → IKiAssistentService, IGeocodingService│
├─────────────────────────────────────────┤
│ Privacy Control Layer                    │
│  → IExternalCommunicationService         │
│  → Datenklassifizierung                  │
│  → Audit-Log                             │
│  → Anonymisierung / Redaction            │
├─────────────────────────────────────────┤
│ Data Layer (Local)                       │
│  → SQLite (bpm.db, planmanager.db)       │
│  → JSON (registry.json, settings.json)   │
│  → Excel (Zeiterfassung via ClosedXML)   │
├─────────────────────────────────────────┤
│ External Gateway (optional)              │
│  → KI-APIs (OpenAI, Anthropic)           │
│  → GIS (Google Maps, GIS Steiermark)     │
│  → Wetter (OpenMeteo)                    │
│  → Task-Management (ClickUp, Asana)      │
└─────────────────────────────────────────┘
```

### 4.2 Privacy Control Layer — Kernkomponente

Der `IExternalCommunicationService` (ADR-035) ist der einzige erlaubte Weg für HTTP-Calls an externe Dienste. Er ist kein reiner Logger, sondern ein **Policy Gate** das aktiv entscheidet ob ein Call erlaubt ist.

```csharp
// In BauProjektManager.Domain/Enums/
public enum DataClassification
{
    ClassA,  // Keine Personendaten (Koordinaten, Hashes, Wetter)
    ClassB,  // Personenbezogene Daten (Kontakte, Mitarbeiter)
    ClassC   // Sensible Drittdaten (LVs, Bescheide, Baubeschreibungen)
}

// In BauProjektManager.Infrastructure/Communication/
public interface IExternalCommunicationService
{
    Task<HttpResponseMessage> SendAsync(
        string module,
        HttpRequestMessage request,
        DataClassification classification,
        string purpose,
        CancellationToken ct = default);

    bool IsModuleAllowed(string module);
    List<ExternalCallLogEntry> GetRecentLog(int count = 50);
}
```

**Policy-Regeln (zentral, nicht im Modul):**

| Prüfung | Wer entscheidet | Konsequenz |
|---|---|---|
| Modul erlaubt? | Einstellungen → Externe Dienste | Blockiert wenn deaktiviert |
| Globaler Kill-Switch? | Einstellungen → „Alle ext. Verbindungen sperren" | Blockiert alles |
| Klasse C ohne Anonymisierung? | Policy im Service | **Default: Blockiert.** Nur mit explizitem User-Override |
| Auto-Calls für dieses Modul erlaubt? | Einstellungen pro Modul | Blockiert Hintergrund-Sync wenn nicht freigeschaltet |
| DPA bestätigt? | Einstellungen → KI → „Ja, habe ich" | KI-Modul blockiert bis DPA bestätigt |

**Verantwortlichkeiten:**

- **Entscheidet** ob ein Call erlaubt ist (nicht nur loggen)
- Schreibt Audit-Log mit Entscheidungsgrund (Zeitstempel, Modul, Domain, Klassifizierung, `decision_reason`)
- Blockiert wenn Policy verletzt wird
- Wirft `ExternalCommunicationBlockedException` mit klarer Begründung

**Regel:**

> Kein Modul darf `HttpClient` direkt für externe APIs verwenden.
> Alle externen Calls laufen über `IExternalCommunicationService`.
> Der Service entscheidet — das Modul fragt an.

### 4.3 Privacy Policy — Austauschbare Komponente (PFLICHT)

Die Datenschutz-Entscheidungslogik ist als eigene Komponente vom Service getrennt. Das ermöglicht zwei Betriebsmodi ohne doppelten Code: **Internal** (Herbert allein) und **Commercial** (Verkauf an Dritte).

**Pattern:** Strategy Pattern via DI — die Architektur bleibt identisch, nur die Policy wird ausgetauscht.
```csharp
// In BauProjektManager.Domain/Privacy/
public interface IPrivacyPolicy
{
    PolicyDecision Evaluate(
        string module,
        DataClassification classification,
        string purpose);
}

public class PolicyDecision
{
    public bool IsAllowed { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool RequiresUserConfirmation { get; init; }
}
```

**Zwei Implementierungen in Infrastructure:**
```csharp
// Für Herbert / interne Nutzung — alles erlaubt, nur Logging
public class RelaxedPrivacyPolicy : IPrivacyPolicy
{
    public PolicyDecision Evaluate(string module, DataClassification classification, string purpose)
        => new() { IsAllowed = true, Reason = "internal_mode", RequiresUserConfirmation = false };
}

// Für Verkaufsversion — volle DSGVO-Logik
public class StrictPrivacyPolicy : IPrivacyPolicy
{
    public PolicyDecision Evaluate(string module, DataClassification classification, string purpose)
    {
        // Modul erlaubt? DPA bestätigt? Klasse C ohne Anonymisierung?
        // → Volle Policy-Logik aus Kapitel 4.2
    }
}
```

**DI-Registrierung (in App.xaml.cs):**
```csharp
// Entscheidung über Lizenz, NICHT über settings.json
if (license.IsCommercial)
    services.AddSingleton<IPrivacyPolicy, StrictPrivacyPolicy>();
else
    services.AddSingleton<IPrivacyPolicy, RelaxedPrivacyPolicy>();
```

**Der `ExternalCommunicationService` nutzt die Policy per Injection:**
```csharp
public class ExternalCommunicationService : IExternalCommunicationService
{
    private readonly IPrivacyPolicy _policy;
    private readonly HttpClient _httpClient;

    public async Task<HttpResponseMessage> SendAsync(
        string module, HttpRequestMessage request,
        DataClassification classification, string purpose,
        CancellationToken ct = default)
    {
        var decision = _policy.Evaluate(module, classification, purpose);
        LogDecision(module, classification, purpose, decision);

        if (!decision.IsAllowed)
            throw new ExternalCommunicationBlockedException(module, decision.Reason);

        if (decision.RequiresUserConfirmation)
            // → UI fragt User (über Event/Callback)

        return await _httpClient.SendAsync(request, ct);
    }
}
```

**Wichtige Regeln:**

- Der aktive Modus wird NIEMALS über `settings.json` gesteuert — sonst kann jeder User die DSGVO umgehen
- Die Entscheidung kommt aus der Lizenz (ADR-034: `.bpm-license`) oder aus einem Build-Flag
- `RelaxedPrivacyPolicy` loggt trotzdem ins Audit-Log (mit `decision_reason: "internal_mode"`) — damit ist auch im internen Betrieb nachvollziehbar was nach außen ging
- Beide Policies nutzen denselben `ExternalCommunicationService` — kein doppelter Code

**Session-Override (OPTIONAL — für Commercial-Modus):**

Für Nutzer der Verkaufsversion die temporär schneller arbeiten wollen:
```csharp
// Checkbox: "Für diese Sitzung Klasse-B-Warnungen automatisch bestätigen"
public interface IPrivacyContext
{
    bool IsTrustedSession { get; set; }
}
```

Die `StrictPrivacyPolicy` prüft den Context: Bei `IsTrustedSession = true` werden Klasse-B-Calls ohne Dialog durchgelassen (aber weiterhin geloggt). Klasse C bleibt IMMER blockiert — Session-Override gilt nicht für Klasse C.

### 4.4 External Gateway — Registrierte Dienste

| Modul-ID | Externer Dienst | Datenklasse | Auto-Call? |
|---|---|---|---|
| `ki` | api.openai.com / api.anthropic.com | C (Dokumente) | ❌ Nein — nur User-Aktion |
| `gis_google` | maps.googleapis.com | A (Adressen) | ❌ Nein — nur Button-Klick |
| `gis_stmk` | gis.stmk.gv.at | A (Koordinaten) | ❌ Nein — nur Button-Klick |
| `wetter` | api.open-meteo.com | A (GPS-Koordinaten) | ⚠️ Ja — Dashboard-Widget |
| `task_mgmt` | api.clickup.com (etc.) | B (Projekt + Material) | ⚠️ Ja — Auto-Sync 5 Min |

**Für `wetter` und `task_mgmt` (automatische Calls):**
- Nur aktiv wenn Modul in Einstellungen aktiviert UND externer Dienst freigeschaltet
- Kein Auto-Call beim ersten Start — User muss explizit aktivieren
- Deaktivierung jederzeit möglich über Einstellungen → Datenschutz

### 4.5 Dienststatus-Modell (PFLICHT)

Jeder externe Dienst durchläuft ein definiertes Zustandsmodell:
Disabled (Default)
→ keine externen Calls erlaubt
→ Übergang zu EnabledManual: explizite Aktivierung durch User
EnabledManual
→ nur user-initiierte Calls erlaubt (Button-Klick)
→ Übergang zu EnabledAuto: separate Freigabe für Hintergrundaktualisierung
EnabledAuto
→ user-initiierte + automatische Hintergrund-Calls erlaubt
→ Übergang zu Disabled: jederzeit durch User

**Regeln:**
- Default für alle Dienste: `Disabled`
- Dienste die NUR manuell funktionieren (GIS Steiermark, KI-Assistent): Haben keinen `EnabledAuto`-Zustand
- Dienste mit Hintergrund-Calls (Wetter, Task-Management): Brauchen separate Freigabe für Auto-Calls
- In der GUI: Zwei separate Toggles pro Dienst (wo relevant):
  - `[☐] Dienst aktivieren` (Disabled → EnabledManual)
  - `[☐] Automatische Abfragen erlauben` (EnabledManual → EnabledAuto)

---

## 5. Datenfluss-Architektur

### 5.1 Standard-Fluss (lokal — 95% aller Operationen)

```
User-Eingabe → Lokale Verarbeitung → SQLite/JSON/Excel → Lokale Anzeige
```

Kein externer Kontakt. Kein Datenschutz-Risiko. Das ist der Default für V1 (Einstellungen + PlanManager).

### 5.2 Kontrollierter externer Fluss

```
User-Aktion (explizit, z.B. "Grundstück laden" Button)
    │
    ▼
Datenklassifizierung: Welche Klasse? (A / B / C)
    │
    ├── Klasse A → direkt an External Gateway
    │
    ├── Klasse B → Warnung + Bestätigung → External Gateway
    │
    └── Klasse C → Anonymisierung / Redaction
                    → User-Bestätigung mit Detailansicht
                    → External Gateway
    │
    ▼
IExternalCommunicationService.SendAsync()
    │
    ▼
Audit-Log (Zeitstempel, Modul, Domain, containsPersonalData)
    │
    ▼
Response → Lokale Verarbeitung → Anzeige
```

---

## 6. Kritische Kontrollpunkte

Jeder externe Datenfluss MUSS diese 4 Schritte durchlaufen:

### Schritt 1: Klassifizierung

Welche Datenklasse wird gesendet? Service muss `containsPersonalData` korrekt setzen.

### Schritt 2: Risikoprüfung

Bei Klasse B/C:
- Enthält die Anfrage personenbezogene Daten?
- Enthält sie Daten von Dritten (die keine Einwilligung gegeben haben)?

### Schritt 3: Reduktion

- Nur die minimal nötigen Daten senden
- Bei KI: Textextrakt statt vollständiges PDF wenn möglich
- Bei GIS: Nur Adresse/Koordinaten, keine Projektdetails

### Schritt 4: User-Bestätigung

- Klasse A: Keine Bestätigung nötig (z.B. Wetter-Abfrage mit GPS-Koordinaten)
- Klasse B: Kurze Bestätigung (z.B. „Adresse wird an Google Maps gesendet. [OK]")
- Klasse C: Detaillierte Bestätigung mit Auflistung der erkannten Personendaten

---

## 7. Anonymisierung & Redaction

### 7.1 Anforderungen

Anonymisierung muss:

- irreversibel sein (Personendaten können nicht rekonstruiert werden)
- alle Datenebenen betreffen (Text, Metadaten, eingebettete Bilder)
- nicht nur visuell erfolgen (kein Schwarzbalken über Text der im PDF-Layer noch lesbar ist)

### 7.2 Pflicht für KI-Modul

Klasse C Daten (LVs, Bescheide, Baubeschreibungen) dürfen nur an externe KI-APIs gesendet werden wenn:

- Anonymisierung durchgeführt wurde ODER
- User explizit bestätigt hat: „Ich bin mir bewusst, dass dieses Dokument personenbezogene Daten enthält und an [Provider] gesendet wird."

### 7.3 Umsetzung im KI-Modul (Stufenmodell)

**Stufe 1 — Default-Block + expliziter Override (PFLICHT, ab KI-Modul V1):**

Klasse-C-Dokumente werden vom `IExternalCommunicationService` **standardmäßig blockiert**. Der User muss aktiv freigeben — nicht nur eine Warnung wegklicken, sondern den Zweck angeben.

```
┌─────────────────────────────────────────────────────────┐
│ ⚠️ Dokument enthält möglicherweise Personendaten         │
│                                                          │
│ Das Dokument „LV_Hochbau_Dobl.pdf" wird an              │
│ api.openai.com gesendet.                                 │
│                                                          │
│ Klasse C — Senden standardmäßig gesperrt.               │
│                                                          │
│ Zweck der Übermittlung:                                  │
│ [ LV-Analyse: Positionen und Mengen prüfen          ]   │
│                                                          │
│ [☐] Ich bestätige: Ich bin als Verantwortlicher für     │
│     den Datenschutz dieser Daten zuständig.              │
│                                                          │
│ [Freigeben und senden]  [Abbrechen]                     │
└─────────────────────────────────────────────────────────┘
```

**Warum Default-Block statt nur Warnung:** Eine reine Warnung wird nach dem dritten Mal reflexartig weggeklickt. Der Zweck-Pflichtfeld und die Checkbox erzwingen eine bewusste Entscheidung. Der angegebene Zweck wird im Audit-Log gespeichert.

**Stufe 2 — Lokales Pre-Processing (EMPFOHLEN, mittelfristig):**

```
PDF-Upload
    → Text extrahieren (PdfPig — bereits im Tech-Stack)
    → Personenbezogene Daten erkennen (Regex: E-Mails, Telefon, SV-Nummern)
    → Redaction: Gefundene Daten durch Platzhalter ersetzen
       z.B. „Ing. Franz Müller" → „[PERSON_1]"
    → Mapping lokal speichern
    → Nur reduzierter Text an KI-API senden
    → Nach KI-Antwort: Platzhalter durch Originale zurückersetzen
    → User sieht: „3 personenbezogene Daten wurden anonymisiert"
```

**Stufe 3 — Konfigurierbares Opt-in (OPTIONAL, langfristig):**

Pro Dokumenttyp konfigurierbar: „LVs immer anonymisieren" / „Baubeschreibungen immer warnen"

**Architektonische Einordnung:** Die Anonymisierung wird als eigener Service hinter dem `IExternalCommunicationService` implementiert, nicht inline im KI-Modul. Das konkrete Interface entsteht wenn das KI-Modul gebaut wird — der architektonische Anker (eigener Service in Infrastructure) steht aber fest.

### 7.4 Minimierungsregel für KI

> Sende niemals ein vollständiges Dokument, wenn ein Ausschnitt genügt.

Technisch: PdfPig extrahiert nur die relevanten Seiten/Absätze. Der Rest wird nicht gesendet.

### 7.5 Fallback-Strategie (ADR-027 Phase 1)

Wenn der User keine automatische KI-Verbindung will:

- BPM generiert den Prompt lokal
- User kopiert Prompt manuell zu ChatGPT/Claude im Browser
- User fügt Antwort (JSON) in BPM ein
- BPM parst das JSON lokal

→ Kein Datentransfer durch BPM. User hat volle Kontrolle.

---

## 8. Modul-Regeln

### 8.1 Jedes Modul muss definieren (PFLICHT)

Bevor ein Modul implementiert wird, muss das Konzeptdokument enthalten:

| Pflicht-Abschnitt | Inhalt |
|---|---|
| **Daten** | Welche Daten werden verarbeitet? Welche Tabellen? |
| **Klassifizierung** | Klasse A / B / C für jede Datenart |
| **Externer Kontakt** | Welche externen APIs werden angesprochen? |
| **Datenminimierung** | Was wird NICHT gespeichert? Was wird reduziert? |
| **Löschkonzept** | Wann werden Daten gelöscht? (bei Mitarbeiterdaten: gesetzliche Fristen) |
| **Informationspflicht** | Müssen Betroffene informiert werden? (Art. 13/14) |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. a–f DSGVO + ggf. nationale Normen |

### 8.2 Verboten

- ❌ Versteckte Datenübertragung (Hintergrund-Sync ohne User-Wissen)
- ❌ Automatische API-Calls ohne explizite Aktivierung in Einstellungen
- ❌ Vollständige Dokumente ungefiltert an KI senden
- ❌ Personenbezogene Daten ohne Kontrolle an externe APIs übertragen
- ❌ Feature-Logik mit API-Calls vermischen (alles über `IExternalCommunicationService`)
- ❌ Speicherung unnötiger Daten (kein Feld ohne klaren Zweck)
- ❌ „UI versteckt = sicher" annehmen (Hidden-Attribute ≠ Datenschutz)
- ❌ Personendaten in Serilog-Logs (nur IDs, keine Namen/E-Mails/Telefon)

### 8.3 Modul-Übersicht — Datenschutz-Status

| Modul | Ext. Kontakt | Datenklasse | Status |
|---|---|---|---|
| Einstellungen | Nein | B (Clients, Participants) | ✅ V1 — lokal, sicher |
| PlanManager | Nein | A (Dateinamen, Hashes) | ✅ V1 — lokal, sicher |
| Foto | Nein | A (Metadaten, Geodaten) | ✅ Lokal |
| Zeiterfassung | Nein (Excel auf Cloud-Speicher) | B (Mitarbeiterdaten) | ⚠️ Löschkonzept + Mitarbeiter-Info nötig |
| Bautagebuch | Nein (lokal) | B (Arbeitskräfte) | ✅ Lokal |
| Dashboard | Nein (lokal) | A | ✅ Lokal |
| Outlook | Nein (COM Interop, lokal) | B (E-Mail-Metadaten) | ✅ Lokal |
| Wetter | Ja (OpenMeteo) | A (GPS-Koordinaten) | ✅ Unbedenklich (anonym, kein Key) |
| GIS (Steiermark) | Ja (gis.stmk.gv.at) | A (Koordinaten) | ✅ Öffentliche API |
| GIS (Google) | Ja (Google Maps) | A (Adressen) | ⚠️ API-Key, Google-DPA |
| Task-Management | Ja (ClickUp etc.) | B (Projekt + Material) | ⚠️ US-Anbieter, DPA nötig |
| KI-Assistent | Ja (OpenAI/Anthropic) | C (Dokumente!) | ❌ KRITISCH — DSFA + Anonymisierung |
| Mobile PWA | Ja (Cloud-API oder LAN) | B (Bautagebuch-Daten) | ⚠️ Je nach Sync-Option |

---

## 9. Datenspeicherung & Verschlüsselung

### 9.1 Primärer Speicher: Lokal

| Speicherort | Inhalt | Synct? |
|---|---|---|
| `bpm.db` (%LocalAppData%) | Projekte, Clients, Participants | Nein |
| `planmanager.db` (%LocalAppData%) | Plan-Cache, Import-Journal, Undo | Nein |
| Logs/ (%LocalAppData%) | Serilog-Dateien | Nein |

Kein Cloud-Zwang. Kein Account. Kein externer Datenspeicher.

### 9.2 Cloud-Speicher (User-Entscheidung)

| Datei | Inhalt | Datenklasse |
|---|---|---|
| `registry.json` | Kontaktdaten (Name, E-Mail, Telefon) | B |
| `settings.json` | Pfade, Einstellungen | A |
| `profiles.json` | Plantyp-Muster | A |
| Excel Zeiterfassung | Mitarbeiterdaten, Arbeitszeiten | B |

**Regel:** BPM informiert den User, dass Cloud-Sync die Verantwortung des Users ist. BPM selbst sendet keine Daten an Cloud-Anbieter — der Cloud-Speicher-Client (OneDrive, Dropbox etc.) macht das.

### 9.3 registry.json — Klasse-B-Felder Whitelist (PFLICHT)

`registry.json` synct über den Cloud-Speicher und ist damit außerhalb der lokalen Kontrolle. Deshalb: explizite Whitelist welche Klasse-B-Felder dort landen dürfen.

**Erlaubt in registry.json (Whitelist):**

| Feld | Datenklasse | Begründung |
|---|---|---|
| Projektname, Projektnummer | A | Kein Personenbezug |
| Adresse (Straße, PLZ, Ort) | A | Baustellenadresse, kein Wohnort |
| Koordinaten, KG, GST | A | Öffentliche Katasterdaten |
| Client.Company (Firmenname) | A/B | Firmenname, kein Privatperson-Name |
| Client.ContactPerson | B | ⚠️ Erlaubt — nötig für VBA-Makros |
| Client.Email, Client.Phone | B | ⚠️ Erlaubt — nötig für VBA-Makros |
| Pfade (root, plans, photos) | A | Technisch |
| Buildings (Pipe-String) | A | Kein Personenbezug |

**Verboten in registry.json:**

| Feld | Warum nicht |
|---|---|
| Mitarbeiterdaten (employees) | Klasse B — gehören nicht in Cloud-synced JSON |
| Arbeitszeiten | Klasse B — Excel ist der Speicherort |
| KI-History / Audit-Log | Klasse B/C — nur lokal |
| API-Keys | Sicherheitsrisiko |
| Lizenz-E-Mail | Nicht nötig für VBA |

**Regel:** Wenn ein neues Feld zu `registry.json` hinzugefügt wird, muss geprüft werden ob es in diese Whitelist passt. Klasse-B-Felder nur wenn für VBA-Kompatibilität zwingend nötig.

**Zukunft (EMPFOHLEN):** Wenn VBA-Makros abgelöst werden, eine „Reduced Export"-Variante von `registry.json` ohne Klasse-B-Felder anbieten (nur Pfade und Projektnamen).

### 9.4 Verschlüsselung & Schlüsselverwaltung

**API-Keys (PFLICHT):**

- Speicherung über DPAPI (Windows Data Protection API) in %LocalAppData%
- NIEMALS in: settings.json, registry.json, Git, Logs
- DPAPI bindet den Key an den Windows-User und die Maschine — angemessen für Desktop-App

**SQLite-Verschlüsselung (EMPFOHLEN — ab Verkauf oder Mobile):**

- V1 (internes Tool, Desktop): Keine Verschlüsselung erforderlich (lokale Datei, User-kontrolliert)
- Bei Verkauf an Dritte oder Mobile-Einsatz: SQLCipher (verschlüsseltes SQLite) einführen
- Begründung: Ein Laptop auf der Baustelle kann verloren gehen. Klasse-B-Daten (Mitarbeiternamen, Arbeitszeiten) wären dann ungeschützt

| Szenario | Verschlüsselung | Begründung |
|---|---|---|
| Herbert allein, Desktop | OPTIONAL | Lokaler PC, eigene Kontrolle |
| Verkauf, Multi-User | EMPFOHLEN | Fremde Geräte, größere Verantwortung |
| Mobile PWA (Laptop/Handy) | PFLICHT | Geräteverlust realistisches Risiko |

**Schlüsselverwaltung bei SQLCipher:**

- Schlüssel aus DPAPI ableiten (an Windows-User gebunden)
- Kein hardcoded Key im Code
- Bei Gerätewechsel: DB kann aus Cloud-Speicher-Daten + Dateisystem-Scan rekonstruiert werden (ADR-004)

**Lizenz-Dateien:**

- HMAC-SHA256 signiert (Integritätsschutz, nicht Vertraulichkeit)
- Kein Datenschutz-Risiko (enthalten nur Firmennamen + Lizenz-ID)

### 9.5 Löschkonzept

| Datenart | Aufbewahrungsfrist | Löschung |
|---|---|---|
| Serilog-Logs | 30 Tage | Automatisch (Serilog `retainedFileCountLimit`) |
| Audit-Log (ext. Calls) | 90 Tage | Automatisch |
| Mitarbeiterdaten (Zeiterfassung) | 7 Jahre nach Austritt (§ 132 BAO) | Manuell (AktivBis + 7 Jahre) |
| Projektdaten | Unbegrenzt | Manuell (Projekt archivieren/löschen) |
| Import-Journal | Pro Projekt, unbegrenzt | Manuell (Projekt löschen) |
| Stammdaten (Clients, Participants, Links) | Projektgebunden | Bei Projektlöschung: CASCADE für Participants/Links. Clients: Prüfung ob noch von anderem Projekt referenziert — wenn nein, User wird gefragt ob Client mitgelöscht werden soll. Clients werden bewusst als potenziell wiederverwendbare Entität behandelt (Vorbereitung ADR-021). |

---

## 10. Zugriffskontrolle

### 10.1 V1 — Solo-Betrieb (PFLICHT)

- Single-Writer Mutex (ADR-016): Nur eine App-Instanz schreibt gleichzeitig
- Keine offenen Dateien im System — Zugriff nur über App
- API-Keys in DPAPI — nicht im Klartext lesbar

### 10.2 Multi-User — Rollenbasierte Zugriffskontrolle (PFLICHT ab Modus B/C)

Das Multi-User-Konzept (ADR-033) sieht aktuell keine Zugriffskontrolle vor (Vertrauensbasis). Für den Verkauf und für DSGVO-Konformität muss ergänzt werden:

| Rolle | Sieht Projektdaten | Sieht Zeiterfassung | Sieht KI-History | Ändert Einstellungen |
|---|---|---|---|---|
| **Admin** | ✅ | ✅ | ✅ | ✅ |
| **Bauleiter** | ✅ | ✅ (eigenes Team) | ✅ | ❌ |
| **Polier** | ✅ (eigene Projekte) | ❌ | ❌ | ❌ |
| **Viewer** | ✅ (nur lesen) | ❌ | ❌ | ❌ |

**Umsetzung:** Nicht für V1 nötig (Solo-Betrieb). PFLICHT bevor Zeiterfassungs-Modul im Multi-User-Modus genutzt wird. Rollen werden in der `users` Tabelle (MultiUserKonzept.md, Abschnitt 6.2) gespeichert.

**Cloud-Sync-Granularität — Betriebsmodi und Datenklassen (PFLICHT ab Modus B/C):**

Wenn Multi-User und Cloud-Sync zusammenkommen, müssen die Betriebsmodi klar getrennt werden:

| Betriebsmodus | Klasse-A-Daten | Klasse-B-Daten | Klasse-C-Daten |
|---|---|---|---|
| **Lokal (Modus A)** | ✅ Frei | ✅ Frei | ✅ Nur lokal |
| **Cloud-Sync (Modus A/B)** | ✅ Synct | ⚠️ Nur Whitelist (Kap. 9.3) | ❌ Nicht syncen |
| **Geteilte DB (Modus B)** | ✅ Alle sehen | ⚠️ Nur berechtigte Rollen | ❌ Nicht in DB |
| **Server (Modus C)** | ✅ API liefert | ⚠️ Rollenbasiert gefiltert | ❌ Nicht über API |

**Konkret:** Zeiterfassungsdaten (Klasse B) werden im Server-Modus nur an Clients mit Bauleiter- oder Admin-Rolle ausgeliefert. Die REST-API filtert basierend auf der Rolle des anfragenden Users. Das Klasse-C-Material (Dokumente) wird niemals über die Server-API bereitgestellt — Dokumente bleiben im Dateisystem.

---

## 11. Logging & Nachvollziehbarkeit

### 11.1 Was geloggt wird

| Kategorie | Was | Wo |
|---|---|---|
| App-Log | Projektaktionen, Import, Fehler | Serilog → Logs/ |
| Audit-Log | Externe Datenübertragungen | SQLite → `external_call_log` |

### 11.2 Was NICHT geloggt wird

- Personennamen
- E-Mail-Adressen
- Telefonnummern
- Personalnummern
- Dokumenteninhalte
- API-Keys

**Regel (aus CODING_STANDARDS.md Kapitel 17):**

```csharp
// RICHTIG:
_logger.LogInformation("Client {ClientId} aktualisiert", client.Id);

// FALSCH:
_logger.LogInformation("Client {Name} ({Email}) aktualisiert",
    client.ContactPerson, client.Email);
```

### 11.3 Audit-Log Schema

```sql
CREATE TABLE external_call_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    module TEXT NOT NULL,              -- "ki", "gis_google", "wetter", "task_mgmt"
    target_domain TEXT NOT NULL,       -- "api.openai.com"
    classification TEXT NOT NULL,      -- "ClassA", "ClassB", "ClassC"
    purpose TEXT,                      -- "LV-Analyse", "Adresse suchen"
    status_code INTEGER,              -- HTTP Status (200, 403, 500)
    blocked INTEGER DEFAULT 0,        -- 1 wenn blockiert
    decision_reason TEXT              -- "allowed_class_a", "blocked_global_killswitch",
                                      -- "blocked_module_disabled", "allowed_user_confirmed",
                                      -- "blocked_class_c_no_anonymization",
                                      -- "blocked_dpa_not_confirmed",
                                      -- "allowed_anonymized_payload"
);

**Negativliste (verbindlich) — external_call_log darf NICHT enthalten:**
- Request-Body (Dokumenteninhalte, Prompts)
- Response-Body (KI-Antworten, API-Ergebnisse)
- HTTP-Headers (Authorization, Cookies)
- Query-Parameter mit Personendaten
- IP-Adressen

**`purpose` Regeln:** Max. 100 Zeichen, nur fachlicher Zweck (z.B. "LV-Analyse Mengenprüfung"), keine Personen-/Dokument-/Projektdetails.

**`decision_reason` — Kontrolliertes Vokabular (kein Freitext):** Definierte Codes siehe [DB-SCHEMA.md](DB-SCHEMA.md) Kapitel 5.11. Im Code als `static class ExternalDecisionReasons` mit `const string` Feldern.nymized_payload"
```

---

## 12. KI-Integration — Sonderregeln

### 12.1 Grundsatz

KI ist optional und darf niemals:

- automatisch Daten senden (kein Hintergrund-Indexing)
- ungeprüfte Dokumente verarbeiten (immer Klassifizierung vorher)
- API-Keys im Cloud-Ordner speichern

### 12.2 Minimierungsregel

> Sende niemals ein vollständiges Dokument, wenn ein Ausschnitt genügt.

Technisch: PdfPig extrahiert nur die relevanten Seiten/Absätze. Der Rest wird nicht gesendet.

### 12.3 DPA-Pflicht

Vor Nutzung des KI-Moduls muss der User eine Auftragsverarbeitungs-Vereinbarung (AVV/DPA) mit dem KI-Provider haben:
- OpenAI: DPA unter openai.com/policies
- Anthropic: DPA auf Anfrage

BPM zeigt beim ersten Aktivieren:

```
┌─────────────────────────────────────────────────────────┐
│ KI-Assistent — Ersteinrichtung                           │
│                                                          │
│ Der KI-Assistent sendet Dokumenteninhalte an              │
│ externe Server ([Provider]).                              │
│                                                          │
│ Für die DSGVO-konforme Nutzung benötigst du eine         │
│ Auftragsverarbeitungs-Vereinbarung (AVV) mit              │
│ dem Anbieter.                                             │
│                                                          │
│ [ℹ️ Was ist eine AVV?]  [Ja, habe ich]  [Später]         │
└─────────────────────────────────────────────────────────┘
```

### 12.4 KI-Anbieter-Prüfung (PFLICHT)

Bevor ein KI-Provider in BPM freigeschaltet wird, muss geprüft werden:

| Prüfpunkt | Was | Wo dokumentiert |
|---|---|---|
| DPA/AVV verfügbar? | Auftragsverarbeitungs-Vertrag des Anbieters | In Einstellungen → KI → Provider-Info |
| Datenverwendung für Training? | Verwendet der Anbieter übergebene Daten zum Modell-Training? | DPA des Anbieters prüfen |
| Datenlöschung | Wie lange speichert der Anbieter die Anfragen? | DPA des Anbieters prüfen |
| Serverstandort | EU oder Drittland? Bei Drittland: Standardvertragsklauseln? | DPA des Anbieters prüfen |
| Subprozessoren | An welche Dritte leitet der Anbieter Daten weiter? | DPA des Anbieters prüfen |

**Stand 2026:**
- OpenAI: API-Daten werden nicht für Training verwendet (lt. API Terms), DPA verfügbar, Server primär in den USA (EU-Standardvertragsklauseln)
- Anthropic: API-Daten werden nicht für Training verwendet (lt. Usage Policy), DPA auf Anfrage

BPM zeigt diese Informationen im Einstellungsdialog an (Provider-Info-Button). Bei Änderung der AGB: Hinweis bei App-Start.

### 12.5 Fallback ohne API

Die manuelle Variante (ADR-027 Phase 1) bleibt immer verfügbar:
- BPM generiert Prompt → User kopiert zu ChatGPT/Claude → User fügt JSON-Antwort ein
- Kein Datentransfer durch BPM

---

## 13. Einstellungen — GUI für Datenschutz

### 13.1 Tab „Datenschutz & Externe Dienste"

In den Systemeinstellungen (5-Tab-Dialog → neuer Tab oder Unter-Tab):

```
┌─────────────────────────────────────────────────────────────┐
│ Einstellungen → Datenschutz & Externe Dienste               │
│                                                              │
│ EXTERNE VERBINDUNGEN                                        │
│ ┌───────────────────┬──────┬──────────────────────────────┐│
│ │ Dienst            │ An/Aus│ Ziel                        ││
│ ├───────────────────┼──────┼──────────────────────────────┤│
│ │ KI-Assistent      │ [☐]  │ api.openai.com              ││
│ │ Google Maps       │ [☐]  │ maps.googleapis.com         ││
│ │ GIS Steiermark    │ [☑]  │ gis.stmk.gv.at (öffentlich)││
│ │ Wetter (OpenMeteo)│ [☑]  │ api.open-meteo.com (anonym) ││
│ │ Task-Management   │ [☐]  │ api.clickup.com             ││
│ └───────────────────┴──────┴──────────────────────────────┘│
│                                                              │
│ DATENMINIMIERUNG                                            │
│ [☑] KI-Dokumente vor Upload anonymisieren (wenn verfügbar) │
│ [☑] Keine Personennamen in Logs                             │
│ [☑] Log-Dateien nach 30 Tagen löschen                       │
│                                                              │
│ LETZTE EXTERNE VERBINDUNGEN                                 │
│  03.04.2026 14:32 → gis.stmk.gv.at (Koordinaten-Abfrage)  │
│  03.04.2026 09:15 → api.open-meteo.com (Wetter Dobl)       │
│                                                              │
│ [Audit-Log exportieren]   [Alle ext. Verbindungen sperren] │
└─────────────────────────────────────────────────────────────┘
```

---

## 14. Anti-Patterns (verboten)

- ❌ Komplette Dokumente ungefiltert an KI senden
- ❌ Personenbezogene Daten ohne Kontrolle übertragen
- ❌ Feature-Logik mit API-Calls vermischen (direkter HttpClient)
- ❌ Versteckte Datenfelder im System behalten (kein Feld ohne Zweck)
- ❌ „UI versteckt = sicher" annehmen
- ❌ API-Keys in settings.json, registry.json, Git oder Logs
- ❌ Personennamen in Serilog-Nachrichten
- ❌ Auto-Sync an externe APIs ohne explizite Aktivierung

---

## 15. MVP-Compliance-Strategie

### MUSS vor V1 Launch

| Maßnahme | Aufwand |
|---|---|
| Keine Personendaten in Serilog-Nachrichten (Prüfung bestehender Code) | 1–2h |
| Log-Rotation: 30 Tage (Serilog-Config) | 5 Min |
| CODING_STANDARDS.md: Kapitel 17 „Datenschutz im Code" | 30 Min |

### MUSS vor erstem Online-Modul

| Maßnahme | Aufwand |
|---|---|
| `IExternalCommunicationService` Interface + Implementierung | 1 Tag |
| `external_call_log` Tabelle in bpm.db | 30 Min |
| Einstellungen: „Datenschutz & Externe Dienste" Tab (Basis) | Halber Tag |

### MUSS vor KI-Modul

| Maßnahme | Aufwand |
|---|---|
| Upload-Warnung mit Opt-in pro Dokument | 2–3h |
| Audit-Logging welches Dokument an welche API | 1h |
| DPA-Hinweis bei Erstaktivierung | 1h |
| API-Keys in DPAPI statt settings.json | 2h |
| KI-Anbieter-Prüfung dokumentieren (Kap. 12.4) | 1h |
| DSFA-Checkliste für KI-Modul durcharbeiten (Kap. 16) | 2h |

### MUSS vor Verkauf an Dritte

| Maßnahme | Aufwand |
|---|---|
| Verarbeitungsverzeichnis (Art. 30 DSGVO) | 2–3h |
| Datenschutzerklärung auf Website | 2h |
| Mitarbeiter-Informationsvorlage (Art. 13) | 1h |
| Rollenbasierte Zugriffskontrolle (bei Multi-User + Zeiterfassung) | 1–2 Tage |
| SQLCipher-Verschlüsselung evaluieren | Halber Tag |

### KANN später (EMPFOHLEN)

| Maßnahme | Wann |
|---|---|
| Pre-Processing / Anonymisierung (Regex + PdfPig) | KI-Modul V2 |
| SQLCipher für bpm.db | Bei Mobile oder Verkauf |
| DSGVO-Export (Art. 15 Auskunftsrecht) | Vor Verkauf an Dritte |
| Automatische Stand-der-Technik-Prüfung (Reminder in App) | Jährlich |

### Bewusstes Restrisiko (akzeptabel für Solo-Betrieb)

| Risiko | Begründung |
|---|---|
| Kontaktdaten in registry.json im Cloud-Ordner | User-Entscheidung, berechtigtes Interesse |
| SQLite nicht verschlüsselt (V1) | Lokale Datei, Desktop-PC, User-kontrolliert |
| Kein Passwort bei Multi-User Modus A/B | Vertrauensbasis im Baustellenteam |
| Log-Dateien enthalten Projektpfade | Keine Personendaten, lokal |
| Kein formales Verarbeitungsverzeichnis (V1) | Für interne Solo-Nutzung zunächst zurückgestellt; spätestens vor Verkauf an Dritte erstellen |

---

## 16. Datenschutz-Folgenabschätzung (DSFA) — Checkliste

Art. 35 DSGVO verlangt eine DSFA wenn eine Verarbeitung „voraussichtlich ein hohes Risiko" für Betroffene hat. Für BPM ist das bei folgenden Modulen relevant:

### 16.1 Wann eine DSFA nötig ist

| Modul | DSFA nötig? | Begründung |
|---|---|---|
| Einstellungen, PlanManager | ❌ Nein | Keine systematische Überwachung, nur lokale Daten |
| Zeiterfassung | ⚠️ Prüfen | Systematische Verarbeitung von Beschäftigtendaten — bei größeren Teams: Ja |
| KI-Assistent | ✅ Ja | Drittdaten (Klasse C) an externe KI-APIs, neue Technologie |
| Mobile PWA mit Cloud-Sync | ⚠️ Prüfen | Beschäftigtendaten über Cloud-APIs |

### 16.2 DSFA-Checkliste für neue Module

Bei jedem neuen Modul mit externem Kontakt oder Klasse-B/C-Daten diese Punkte durchgehen:

```
☐ 1. Zweck: Warum werden diese Daten verarbeitet?
☐ 2. Notwendigkeit: Geht es auch mit weniger Daten?
☐ 3. Risiko: Was ist der schlimmste Fall für Betroffene?
     (z.B. Mitarbeiter-Arbeitszeiten werden öffentlich)
☐ 4. Maßnahmen: Welche Schutzmaßnahmen werden getroffen?
     (Verschlüsselung, Anonymisierung, Zugriffskontrolle)
☐ 5. Verhältnismäßigkeit: Überwiegt der Nutzen das Risiko?
☐ 6. Betroffenenrechte: Auskunft, Löschung, Einschränkung möglich?
☐ 7. Dokumentation: Ergebnis in Modul-Konzeptdokument festgehalten?
```

Diese Checkliste wird vor der Implementierung des betreffenden Moduls ausgefüllt und im Konzeptdokument als Abschnitt „Datenschutz-Folgenabschätzung" dokumentiert.

---

## 17. Organisatorische Maßnahmen

Technische Maßnahmen allein reichen nicht. Ergänzend (insbesondere bei Verkauf an Dritte):

### 17.1 Schulung (EMPFOHLEN ab Verkauf)

- **Endnutzer-Hinweis:** Beim ersten Start der App: Kurzer Hinweis „BPM speichert Projektdaten lokal. Für Module mit externen Diensten gilt: [Link zu Datenschutz-Info]"
- **Admin-Schulung:** Bei Firmen mit Multi-User: Wer darf welche Module aktivieren? Wer ist für AVVs mit Cloud-Anbietern verantwortlich?

### 17.2 Prozesse (EMPFOHLEN ab Verkauf)

| Prozess | Wann | Verantwortlich |
|---|---|---|
| Jährliche Revision dieses Dokuments | Jahrestag der letzten Revision | Entwickler (Herbert) |
| Prüfung KI-Provider-AGB bei Änderung | Bei Benachrichtigung durch Provider | App-User (Admin) |
| Mitarbeiter-Info bei Zeiterfassungs-Einführung | Vor erster Nutzung | Arbeitgeber (Firmenkunde) |
| AVV mit Cloud-Anbieter abschließen | Bei Cloud-Sync mit Klasse-B-Daten | Arbeitgeber (Firmenkunde) |

### 17.3 Kosten-Nutzen (pragmatisch)

Art. 25 DSGVO fordert Maßnahmen „unter Berücksichtigung der Implementierungskosten". BPM ist ein Solo-Projekt mit begrenztem Budget. Deshalb gilt:

- **Zuerst organisatorische Maßnahmen** (Warnungen, Dokumentation, Info-Texte) — kosten fast nichts
- **Dann technische Maßnahmen** (IExternalCommunicationService, DPAPI) — einmaliger Aufwand
- **Teure Maßnahmen** (SQLCipher, NER-basierte Anonymisierung, Compliance-Audit) — erst bei Verkauf oder bei konkretem Risiko

---

## 18. Modul-Implementierungs-Checkliste (PFLICHT)

Vor Implementierung jedes Moduls mit externem Kontakt oder Klasse-B/C-Daten diese Checkliste durchgehen. Ergebnis im Konzeptdokument als Abschnitt „Datenschutz" dokumentieren.

```
☐ 1. Hat das Modul externe Kommunikation?
     → Wenn ja: IExternalCommunicationService nutzen (kein direkter HttpClient)
☐ 2. Datenklasse definiert?
     → Jedes Datenfeld: A, B oder C?
☐ 3. Rechtsgrundlage dokumentiert?
     → Art. 6 Abs. 1 lit. a–f + ggf. § 96 ArbVG, § 132 BAO
☐ 4. Informationspflicht relevant?
     → Art. 13 (direkt erhoben) oder Art. 14 (Drittquelle)?
☐ 5. Audit-Log vorgesehen?
     → Alle externen Calls werden mit classification + purpose + decision_reason geloggt
☐ 6. Provider-Freigabe nötig?
     → DPA vorhanden? Training-Policy geprüft? Serverstandort dokumentiert?
☐ 7. Anonymisierung nötig?
     → Klasse C: Default-Block. Ohne Anonymisierung nur mit explizitem User-Override
☐ 8. Löschkonzept definiert?
     → Aufbewahrungsfristen für alle personenbezogenen Daten
☐ 9. Cloud-Sync-Verhalten definiert?
     → Welche Daten synchen über Cloud-Speicher? Welche nicht?
☐ 10. DSFA nötig?
      → Kapitel 16 Checkliste durcharbeiten wenn Klasse B/C + extern
```

---

## 19. Verwandte ADRs

| ADR | Bezug zu Datenschutz |
|---|---|
| ADR-002 | SQLite als System of Record — Daten lokal, kontrolliert |
| ADR-004 | Dreistufige Cloud-Sync-Strategie — Trennung lokal/sync |
| ADR-015 | Serilog — Log-Rotation konfigurierbar |
| ADR-017 | VBA liest nur — kein bidirektionaler Datenfluss |
| ADR-020 | Write-Lock — kontrollierter Zugriff bei Multi-User |
| ADR-027 | KI-API-Import — Fallback ohne API-Transfer |
| ADR-034 | Modul-Aktivierung — Module einzeln deaktivierbar |
| **ADR-035** | **IExternalCommunicationService** (neu, aus diesem Dokument) |

---

## Änderungshistorie

| Version | Datum | Änderung |
|---|---|---|
| 1.0 | 03.04.2026 | Erstversion |
| 1.1 | 03.04.2026 | Ergänzt: Revisionspflicht (Kap. 2.6), Informationspflichten Art. 13/14 (Kap. 3 Klasse B), Rollenbasierte Zugriffskontrolle (Kap. 10.2), Verschlüsselung/SQLCipher (Kap. 9.4), KI-Anbieter-Prüfung inkl. Training/Datenverwendung (Kap. 12.4), DSFA-Checkliste (Kap. 16), Organisatorische Maßnahmen inkl. Kosten-Nutzen (Kap. 17), Dokumentklassifizierung (PFLICHT/EMPFOHLEN/OPTIONAL), Änderungshistorie |
| 1.2 | 03.04.2026 | Überarbeitung auf Basis externer Review: `DataClassification` Enum statt bool (Kap. 4.2), Policy Enforcement im Service (Kap. 4.2), `decision_reason` im Audit-Log (Kap. 11.3), KI Klasse C Default-Block statt Warnung (Kap. 7.3), registry.json Klasse-B-Whitelist (Kap. 9.3), Multi-User Betriebsmodi-Matrix (Kap. 10.2), Verarbeitungsverzeichnis defensiver formuliert (Kap. 15), Modul-Implementierungs-Checkliste (Kap. 18) |
| 1.3 | 03.04.2026 | IPrivacyPolicy als austauschbare Komponente (Kap. 4.3): Strategy Pattern für Internal/Commercial-Modus, RelaxedPrivacyPolicy + StrictPrivacyPolicy, Lizenz-gesteuerte DI-Registrierung, Session-Override für Klasse B |
| 1.4 | 03.04.2026 | Kern-Dokumenten-Review (Claude + ChatGPT, 3 Runden): Dienststatus-Modell Disabled/EnabledManual/EnabledAuto (Kap. 4.5), Anonymisierung als eigener Service (Kap. 7.3), Löschkonzept für Stammdaten (Kap. 9.5), Audit-Log Negativliste + decision_reason-Katalog (Kap. 11.3) |

---

*Dieses Dokument ist ein fachlich-technischer Architekturstandard, keine Rechtsberatung. Für rechtlich verbindliche Aussagen einen auf DSGVO spezialisierten Anwalt konsultieren (z.B. WKO Gründerservice oder UBIT Fachgruppe IT).*
