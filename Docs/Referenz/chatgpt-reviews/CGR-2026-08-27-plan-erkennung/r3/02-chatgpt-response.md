# Review Runde 3 — ChatGPT-Antwort (finales Sign-off)

> Archiviert 1:1 aus dem ChatGPT-Canvas „Review Runde 3" (via Herbert).

---

Claude, die Serie ist architektonisch aus meiner Sicht abgeschlossen. Ich habe K1–K6 noch einmal gegen den aktuellen Branch `feature/planmanager-v1` geprüft. Vier Punkte trage ich direkt mit; bei K2/K3/K5 schärfe ich die Umsetzung. Zusätzlich sind mir zwei Dinge aufgefallen, die wir jetzt festhalten sollten: der Tokenization-Bootstrap vor Existenz eines Profils und die Identität der globalen `PatternTemplate`s.

## K1 — `proposal_fingerprint`: bestätigt, mit kanonischem Escaping

Die Grundentscheidung ist richtig:

* menschenlesbar statt Hash;
* versioniert;
* identifiziert das Vorschlag erzeugende Muster, nicht den vorgeschlagenen Wert;
* `proposed_value` bleibt separat;
* normalisierte Werte;
* `fieldTypeId` bzw. `document_type` als Zielfeld.

Ich würde nur das Serialisierungsformat härten.

Ein Rohformat wie:

```text
v1:l2a:token_shape:^\d{3}[a-z]$@1:plan_number
```

ist zwar lesbar, aber `:`, `|`, `=` usw. können selbst Bestandteil von Regex oder Feature-Keys sein. Deshalb:

```text
v1|l2a|exact_token_at_pos|pos=2;token=gr|document_type
```

mit kanonischem Escaping reservierter Zeichen in dynamischen Werten. Zum Beispiel sinngemäß: `| ; = %` werden escaped/percent-encoded.

Wichtig: `proposal_fingerprint` ist ein stabiler Audit-/Gruppierungsschlüssel, aber kein Domain-Protokoll, das die Anwendung später wieder parsen muss, um Recognition auszuführen. Recognition bleibt in echten Regeln/Features modelliert.

Für tokenabhängige Features würde ich zusätzlich die Tokenization-Semantik berücksichtigen. Sonst könnten zwei verschiedene Parser-Konfigurationen denselben Fingerprint erzeugen:

```text
pos=2;token=gr
```

obwohl Position 2 durch unterschiedliche Delimiter etwas anderes bedeutet. Daher beispielsweise:

```text
v1|l2a|exact_token_at_pos|tok=-,_;collapse=0;first=~;pos=2;token=gr|document_type
```

Auch dieser Teil wird kanonisch escaped. Kein Hash nötig.

**K1: angenommen mit dieser Schärfung.**

## K2 — Mining-Anker: fachlich ja, technisch leicht anders schneiden

Die UX-Entscheidung unterstütze ich vollständig: Keine Mining-Popups im Radial-/Erfassungsflow. Der Polier, der 40 Pläne sortiert, darf nicht alle paar Dateien von „Ich habe etwas gelernt"-Dialogen unterbrochen werden.

Ich würde aber zwischen **Invalidierung** und **Mining-Ausführung** unterscheiden.

Dein Vorschlag:

```text
1. nach Import-Batch
2. beim Öffnen des Erkennungs-Tabs
```

wird damit:

```text
Import-Batch erfolgreich abgeschlossen
        │
        ▼
Evidence/Mining-Cache = dirty
        │
        │ keine Analyse im Import-Commit
        ▼
Erkennungs-Tab geöffnet
        │
        ▼
Mining bei Bedarf ausführen
        │
        ▼
„2 Regelvorschläge"
```

Wenn der Erkennungs-Tab während des Imports bereits offen ist, kann nach erfolgreichem Batch ebenfalls neu ausgewertet werden.

Die entscheidende Invariante:

**Mining ist niemals Teil der Import-Transaktion und niemals Voraussetzung dafür, dass ein erfolgreich importierter Batch als erfolgreich gilt.**

Das ist nach der gerade erfolgten Import-Transaktions-Härtung besonders wichtig.

Also nicht:

```text
Commit Import
→ Mining
→ Mining Exception
→ Import fehlgeschlagen
```

sondern:

```text
Commit Import
→ Import erfolgreich

danach optional:
→ Mining invalidieren / neu berechnen
```

Kein Timer, kein App-Start-Scan, kein Hintergrunddienst nötig.

Und terminologisch würde ich es nicht „on-demand an zwei Ankern" nennen, sondern: **event-invalidiert, UI-demand-berechnet**. Das beschreibt das Verhalten genauer.

**K2: angenommen mit Trennung von Invalidierung und Berechnung.**

## K3 — Dokumenttyp deaktivieren: hier widerspreche ich dem konkreten Mechanismus

Das gewünschte Verhalten unterstütze ich:

```text
Typ deaktiviert
→ Ring 1 weg
→ Profil bleibt
→ Evidenz bleibt
→ Dokumente bleiben
→ Recognition gestoppt
→ Reaktivierung stellt alles wieder her
```

Aber `is_deleted` ist dafür im aktuellen Schema die falsche Eigenschaft.

Ich habe `ProjectDatabase.cs` im Branch geprüft. `document_types` hat aktuell `is_deleted`, aber kein `is_active`. Bei `segment_types` existieren dagegen bewusst beide: `is_active` und `is_deleted`. Das ist semantisch richtiger.

Soft-Delete bedeutet: fachlich gelöscht, aber aus Sync-/Historiengründen physisch noch vorhanden. Deaktivieren bedeutet: fachlich weiterhin vorhanden, nur momentan nicht verwendbar.

Wenn wir `is_deleted` für „deaktiviert" missbrauchen, entstehen unangenehme Konsequenzen. Der aktuelle Unique-Index für `document_types.key` gilt beispielsweise nur:

```sql
WHERE key <> '' AND is_deleted = 0
```

Damit könnte nach einer vermeintlichen „Deaktivierung" ein neuer Dokumenttyp mit demselben Key erzeugt werden. Spätere Reaktivierung wäre dann konfliktbehaftet.

Deshalb klare Empfehlung: `document_types` bekommt `is_active` UND `is_deleted` mit getrennter Semantik.

**Deaktivieren** (`is_active = 0`, `is_deleted = 0`):

```text
Ring 1             ausblenden
Erkennungs-Tab     anzeigen: „Inaktiv"
Recognition        nicht ausführen
Profil             behalten
Evidenz            behalten
Dokumente          behalten
Key                weiterhin reserviert
```

**Reaktivieren** (`is_active = 1`): Alles wird wieder verfügbar.

**Löschen** (`is_deleted = 1`): bleibt eine separate, bewusst stärkere fachliche Aktion.

Da BPM Frühphase ist: Schemaänderung → DB löschen und neu erzeugen lassen. Keine Migration.

Ich würde außerdem nicht `ProfileHealth.MissingSegmentTypes` für diesen Fall missbrauchen. Besser explizit:

```text
ProfileHealth.Valid
ProfileHealth.MissingSegmentTypes
ProfileHealth.DocumentTypeInactive
```

oder ein darüberliegender Recognition-Availability-State. „Segmenttyp fehlt" und „Dokumenttyp bewusst deaktiviert" sind zwei völlig unterschiedliche Diagnosegründe.

**K3: Zielverhalten angenommen, Mechanismus geändert auf `is_active`.**

## K4 — UI-Bezeichnung „Erkennung"

Ja. Für die Benutzeroberfläche finde ich „Erkennung" besser als „Profile" oder dauerhaft „Erkennungsprofile".

Die UI würde damit sehr natürlich:

```text
Tab: Erkennung

Polierplan
Erkennung aktiv
3 Regeln
[Bearbeiten]

Schalungsplan
Noch nicht angelernt
12 bestätigte Pläne
[Erkennung anlernen]

Fertigteilplan
2 Regelvorschläge
[Prüfen]
```

Auch Aktionen sind verständlich: „Erkennung anlernen", „Erkennung bearbeiten", „Erkennung zurücksetzen", „Erkennung deaktiviert".

Im Code bleibt `RecognitionProfile`, `ProfileManager`, `RecognitionRule`, und in ADR/Dokumentation kann „Erkennungsprofil" verwendet werden. Das trennt UI-Sprache und fachlich-technische Terminologie sauber.

**K4: angenommen.**

## K5 — `DocumentTypeName` entfernen: ja, aber der PatternTemplate-Teil muss anders gelöst werden

Bei `RecognitionProfile` stimme ich dir inzwischen klar zu: `DocumentTypeId` soll die einzige persistierte fachliche Typreferenz sein. `DocumentTypeName` ist dort redundante Anzeigeinformation und sollte beim nächsten ohnehin nötigen Schema-Bump entfernt werden.

Also:

```json
{
  "documentTypeId": "01K..."
}
```

Name, Farbe usw. kommen aus `document_types`. Frühphase: SchemaVersion++, alte Profile löschen, neu erzeugen, keine Migration. Kein eigener Schema-Bump ausschließlich dafür — ebenfalls Zustimmung.

### Aber: dein PatternTemplate-Zusatzbefund braucht eine wichtige Korrektur

Ich habe `PatternTemplateService.cs` aktuell geprüft. Die Inkonsistenz ist im Branch sogar interessanter als beschrieben.

Der Kommentar sagt: „Updates if same DocumentTypeName already exists" — aber `AddOrUpdate()` vergleicht heute tatsächlich schon:

```csharp
t.DocumentTypeId.Equals(template.DocumentTypeId, ...)
```

`GetSuggestions()` dagegen prüft weiterhin:

```csharp
p.DocumentTypeName.Equals(t.DocumentTypeName, ...)
```

Wir haben also aktuell **beide Identitätsmodelle gleichzeitig**.

Und hier kommt der wichtigere Punkt: **`DocumentTypeId` ist projektlokal.** `document_types.id` ist ein ULID eines konkreten Projekts. Damit kann ein globales `PatternTemplate` aus Projekt A nicht sinnvoll über `DocumentTypeId` mit dem semantisch gleichen Typ aus Projekt B identifiziert werden.

Beispiel:

```text
Projekt A
Polierplan → documentTypeId = 01AAA

Projekt B
Polierplan → documentTypeId = 01BBB
```

Fachlich gleicher Typname bzw. vielleicht gleiche Erkennungslinie, aber verschiedene IDs — völlig korrekt.

Daher wäre „PatternTemplate-Abgleich komplett auf DocumentTypeId umstellen" für die globale Templatebibliothek **falsch**.

### K5a — Konsequenz: lokale und globale Identität sauber trennen

Ich würde deshalb jetzt festhalten:

- **RecognitionProfile** (projektlokal): `DocumentTypeId` → korrekt.
- **PatternTemplate** (projektübergreifend): NICHT `DocumentTypeId` als globale Identität.

Die natürliche Lösung ist unsere ohnehin beschlossene: **`profileLineageId`**. Damit wird Stufe C2 gleichzeitig die Bereinigung des alten PatternTemplate-Modells.

Zielbild:

```text
PatternTemplate
    │
    └── ProfileLineageId
            │
            ├── Projekt A / RecognitionProfile
            ├── Projekt B / RecognitionProfile
            └── Projekt C / RecognitionProfile
```

Beim Erstellen eines komplett neuen Profils: neue `profileLineageId`. Beim „Als Vorlage verwenden": dieselbe Lineage wird vererbt. Damit weiß BPM explizit: Diese Recognition-Konfigurationen haben gemeinsame Herkunft. Genau dafür haben wir Lineage beschlossen.

`DocumentTypeId` im globalen Template kann höchstens noch als `SourceDocumentTypeId` für Debug/Provenance dienen, aber nicht als Cross-Project-Identity. `DocumentTypeName` kann im `PatternTemplate` weiterhin als Display-Metadatum sinnvoll sein („Polierplan") — nur eben niemals als Identität.

### Daraus folgt noch eine kleine Bereinigung von ADR-010

ADR-010 sagt derzeit:

```text
RecognitionProfile = verbindlich pro Projekt/Plantyp
PatternTemplate = Vorschlag aus globaler Musterbibliothek
```

Das bleibt grundsätzlich richtig. Aber nach dieser Review-Serie sollten wir präzisieren:

```text
RecognitionProfile
→ projektlokale ausführbare Recognition-Konfiguration
→ referenziert lokalen DocumentTypeId

PatternTemplate
→ wiederverwendbare Recognition-Vorlage
→ projektübergreifend
→ Identität über ProfileLineageId
→ DocumentTypeName nur Anzeige
```

Damit verschmelzen PatternTemplate und RecognitionProfile weiterhin nicht, aber ihre Herkunft ist eindeutig verknüpft. Das ist wesentlich sauberer als das heutige Gemisch aus `DocumentTypeId` und `DocumentTypeName`.

**K5: Entfernung aus RecognitionProfile angenommen; globaler Template-Abgleich NICHT auf DocumentTypeId, sondern künftig auf Lineage.**

## K6 — ADR jetzt: ja, aber Nummer ändern

Das ADR jetzt anzulegen halte ich für richtig. Die Entscheidung beeinflusst schon vorher:

* Wiederaufnahme des ProfileWizards,
* Profil-/Erkennungs-Tab,
* Ring-1-„+ Neu",
* PatternTemplate-Logik,
* künftige Lern-Tickets.

Es wäre ungünstig, diese Arbeit noch gegen das alte mentale Modell „Dokumenttypen hier, Profile dort" weiterzubauen.

Der ADR ändert trotzdem keinen V1-Scope:

```text
Decision: Accepted
Implementation: Not Started / Post-V1
```

passt.

**Aber aktuelle Repo-Prüfung: `ADR-064` ist inzwischen bereits vergeben** (ADR-064: Import-Transaktions-Härtung — idempotente Journal-/Recovery-/Undo-Semantik).

Der neue ADR muss deshalb sein: **ADR-065: Lernende Planerkennung — hierarchisches Evidenz-Scoping + Dokumenttyp als Hauptobjekt.** Keine Nummer wiederverwenden.

Der ADR sollte zusätzlich zu deiner Liste noch die in dieser Runde geschärften Punkte enthalten:

* `document_types.is_active` für echte Deaktivierung;
* `PatternTemplate`-Cross-Project-Identität über `profileLineageId`, nicht `DocumentTypeId`;
* Mining außerhalb des Import-Commitpfads;
* Tokenization-Bootstrap, siehe nächster Abschnitt.

**K6: angenommen als ADR-065.**

## 7. Ein Punkt fehlt noch und sollte JETZT entschieden werden: Tokenization vor dem ersten Profil

Das ist beim Zusammenführen unserer bisherigen Aussagen sichtbar geworden.

Wir haben beschlossen: `L2a = ProjectId + DocumentTypeId`, und Evidenz entsteht bereits durch manuelles Dial-Sortieren, bevor ein RecognitionProfile existiert.

Gleichzeitig haben wir beschlossen: `ExactToken`, `ExactTokenAtPosition` usw. müssen dieselbe `TokenizationConfig` wie `FileNameParser` verwenden.

Aber: `TokenizationConfig` liegt heute im `RecognitionProfile`. Wenn noch kein Profil existiert, existiert auch noch keine profilspezifische Tokenization.

Das müssen wir konzeptionell schließen, sonst wird Stufe A/B beim Ticket-Schnitt uneindeutig.

## 8. Meine Empfehlung für diesen Bootstrap

**TokenizationConfig NICHT in `document_types` verschieben.** Denn: DocumentType = „was ist das fachlich?", RecognitionProfile = „wie erkenne ich es?". Tokenization gehört klar zur zweiten Frage.

Stattdessen: **Bestätigte Evidenz speichert Rohfakten; tokenabhängige Features werden bei Bedarf daraus neu abgeleitet.**

Die manuelle Aufnahme liefert schon:

```text
Dateiname
DocumentTypeId
Plannummer
Bauteil
Geschoss
weitere bestätigte Segmente
```

Diese Daten sind die Wahrheit. Wir müssen nicht beim Capture bereits dauerhaft Token[0], Token[1], Token[2] … speichern. Denn Tokenisierung ist eine Interpretation und kann sich ändern.

Zielablauf:

```text
10 Pläne manuell als Polierplan bestätigt
        │
        ▼
Roh-Evidenz vorhanden
        │
        ▼
„Erkennung anlernen"
        │
        ▼
TokenizationConfig wählen/ableiten
        │
        ▼
historische bestätigte Dateinamen
mit FileNameParser neu parsen
        │
        ▼
Mining
        │
        ▼
Regelvorschläge
        │
        ▼
User bestätigt
        │
        ▼
gültiges RecognitionProfile speichern
```

Damit lernen die Daten vor dem Profil, ohne dass wir künstlich ein leeres Profil benötigen.

## 9. Was Stufe A vor vorhandenem Profil verwenden darf

Es gibt zwei Featureklassen:

**Profilunabhängig** — sofort aus Rohdaten ableitbar:

```text
normalisierter Dateiname
Extension
Zeichen-/Nummernformen
LightweightPlanExtractor-Kandidaten
bereits bestätigte fachliche Werte
einfache Wiederholungsmuster
```

Diese können bereits vor einem Profil L2a unterstützen.

**Tokenization-abhängig:**

```text
ExactTokenAtPosition
ExactToken
TokenPrefix
TokenSuffix
positionsbezogene TokenShape
```

Diese werden erst bewertet, wenn eine konkrete Tokenization für die Recognition vorliegt — zunächst auch nur als ephemerer Wizard-/Anlernzustand. Es ist dafür kein bereits gespeichertes Profil nötig.

Das ist wichtig: Profile-JSON wird weiterhin erst geschrieben, wenn daraus eine valide Recognition-Konfiguration entstanden ist.

## 10. Invariante für ADR-065

Ich würde explizit aufnehmen:

**Lern-Evidenz persistiert bestätigte Rohfakten, nicht abgeleitete Token-Snapshots. Tokenabhängige Features werden mit der jeweils geltenden `TokenizationConfig` über den zentralen `FileNameParser` reproduzierbar neu berechnet. `TokenizationConfig` bleibt Teil der Recognition-Konfiguration und wird nicht in `document_types` verschoben.**

Das verhindert gleich drei spätere Probleme:

1. keine doppelte Token-Wahrheit;
2. kein Reprocessing-/Migrationsproblem bei geänderter Tokenization;
3. kein künstliches Dummy-Profil nur fürs Lernen.

Diesen Punkt würde ich vor Ticket-Schnitt entscheiden, nicht offenlassen.

## 11. Damit würde ich die Zielarchitektur endgültig so festschreiben

```text
                  document_types
             fachliche Typ-Wahrheit
                       │
        ┌──────────────┼───────────────┐
        │              │               │
        ▼              ▼               ▼
    Dial Ring 1    Dokumente       Roh-Evidenz
                                      │
                                      │ ProjectId
                                      │ + DocumentTypeId
                                      ▼
                                   L2a
                                      │
                         ┌────────────┴────────────┐
                         │                         │
                  profilunabhängig          Tokenization
                                             aus Anlern-
                                              kontext
                                                 │
                                                 ▼
                                           FileNameParser
                                                 │
                                                 ▼
                                             Rule Mining
                                                 │
                                         User bestätigt
                                                 │
                                                 ▼
                                      RecognitionProfile
                                          0..1 pro Typ
                                                 │
                                         ProfileLineageId
                                                 │
                                                 ▼
                                        L2b / Template
```

Global daneben:

```text
L2c
kuratierte FORMEN / Lexikon
keine projektlokalen Werte
keine IDs
```

## 12. Finale Roadmap

Damit würde ich die Serie mit dieser Roadmap schließen:

```text
V1
L0 + L1 + Radial-Erfassung
DocumentType = gemeinsame Typ-Wahrheit
manuelle Aufnahme sammelt bestätigte Roh-Evidenz

A
L2a Projekt × DocumentTypeId
profilunabhängige lokale Evidenz
Radial-/Panel-Vorschläge

B
Rule Mining
Tokenization-on-demand über FileNameParser
recognition_feedback
Erkennungs-Tab als View über document_types
+ optionales RecognitionProfile

C1
Segmentwert-Aliasse
exakt + userbestätigt

C2
profileLineageId
L2b Familien-Evidenz
PatternTemplate-Identität auf Lineage bereinigen
DocumentTypeName aus RecognitionProfile entfernen

D
global kuratiertes Lexikon + Tokenformen
L2c nur Kandidaten/Formen

danach
ML-Experiment ausschließlich bei gemessenem Bedarf
offline
```

Die reine UI-Zusammenführung darf natürlich vor B umgesetzt werden, wenn sie für laufende V1-Arbeit ohnehin angefasst wird. Sie ist keine Lernfunktion und muss deshalb nicht künstlich auf post-V1 warten.

## 13. Was bei Umsetzung offen bleiben darf

Nach dieser Runde sehe ich nur noch echte Kalibrierungs-/Implementierungsdetails:

* L2a Support/Purity initial `5 / 0,90`;
* L2b initial `10 / 0,95`;
* lokale Veto-Schwelle wahrscheinlich `2/2` oder konservativer `3/3`;
* konkrete Recent-/LongTerm-Fenster für Drift;
* konkrete UI-Darstellung der Begründungen;
* maximale Länge eines `proposal_fingerprint`;
* genaue Auswahl profilunabhängiger Stufe-A-Features;
* Performance-Caching des Minings;
* Ticketreihenfolge C1/C2 darf pragmatisch angepasst werden.

Das sind keine Architekturblocker.

## Finales beidseitiges Schluss-Statement

Die lernende Planerkennung des BPM wird als erklärbare, hierarchisch gescopte Assistenz aufgebaut und ändert die ADR-059-Grenze nicht: Nur deterministisches Bestandsmatching darf automatisch entscheiden, gelernte Erkennung bleibt `AutoSuggested` und wird vom Benutzer bestätigt. `document_types` ist die gemeinsame fachliche Wahrheit für Dial Ring 1, manuelle Erfassung und Erkennungs-UI; `RecognitionProfile` ist lediglich die optionale ausführbare Erkennungskonfiguration eines Typs, während bestätigte Roh-Evidenz bereits vor einem Profil gesammelt werden kann. Lernen erfolgt zuerst projektlokal, später über explizite `profileLineageId` und zuletzt über kuratiertes globales Formenwissen; projektgebundene Stammdaten-IDs werden niemals scope-übergreifend übertragen. Der bevorzugte Lernmechanismus ist Evidenz → nachvollziehbares Rule Mining → explizite Regeln und Aliasse; ML/Embeddings/LLM bleiben außerhalb des Importpfads und werden nur bei später nachgewiesenem Bedarf neu bewertet.

---

✅ **Einigkeit**

* ADR-059 bleibt vollständig unangetastet.
* `document_types` = fachliche Typ-Wahrheit.
* Ring 1 + Erkennungs-Tab basieren auf denselben Dokumenttypen.
* `RecognitionProfile` = optionale Recognition-Konfiguration, 0..1 je Dokumenttyp.
* Kein leeres Dummy-Profil.
* L2a = `ProjectId + DocumentTypeId`.
* Bestätigte manuelle Aufnahme erzeugt Evidenz vor Profilerstellung.
* Backoff L2a → L2b → L2c ohne Score-Fusion.
* Lokale Gegen-Evidenz darf Familien-Backoff vetoieren.
* WERTE lokal, ROLLEN familienfähig, FORMEN global.
* ID-Auflösung immer lokal.
* `ExactToken` benutzt dieselbe Token-Grenzsemantik wie `FileNameParser`.
* Mining-Ergebnisse müssen in explizite BPM-Regeln übersetzbar sein.
* Alias C1 bleibt eigenständige Stufe.
* Lineage C2 ist explizit, niemals heuristisch.
* `proposal_fingerprint` menschenlesbar, versioniert und kanonisch escaped.
* Mining läuft nicht innerhalb der Import-Transaktion.
* UI-Bezeichnung „Erkennung" ist empfehlenswert.
* `DocumentTypeName` wird beim nächsten ohnehin nötigen Profil-Schema-Bump aus `RecognitionProfile` entfernt.
* Tokenization bleibt Recognition-Konfiguration; Roh-Evidenz wird nicht als Token-Snapshot persistiert.
* Kein ML.NET/Embedding/LLM im Importpfad.
* Frühphase = Reset statt Migration.
* Ergebnis wird jetzt als ADR-065 festgeschrieben.

⚠️ **Widerspruch / Schärfung**

Drei konkrete Korrekturen gegenüber K1–K6:

1. K3: `is_deleted` darf nicht als reversible Deaktivierung verwendet werden. `document_types` braucht dafür analog zu `segment_types` ein eigenes `is_active`; Löschen und Deaktivieren bleiben getrennte Zustände.
2. K5: `DocumentTypeId` darf nicht Identität der globalen `PatternTemplate`-Bibliothek werden, weil die ID projektlokal ist. Der aktuelle Branch ist bereits inkonsistent (`AddOrUpdate` per ID, `GetSuggestions` per Name). Zielidentität wird `profileLineageId`; `DocumentTypeName` bleibt dort höchstens Anzeige.
3. K6: `ADR-064` ist im aktuellen Branch bereits durch die Import-Transaktions-Härtung belegt. Die lernende Erkennung wird ADR-065.

❓ **Rückfragen**

Keine Architektur-Rückfrage mehr. Aus meiner Sicht ist die Review-Serie damit final signiert; die verbleibenden Punkte sind Ticket- und Kalibrierungsdetails, keine offenen Architekturentscheidungen.
