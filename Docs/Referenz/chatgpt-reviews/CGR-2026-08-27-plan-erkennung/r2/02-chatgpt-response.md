# Review Runde 2 — ChatGPT-Antwort (SIGN-OFF)

> Archiviert 1:1 aus dem ChatGPT-Canvas „Review Runde 2" (via Herbert).
> Kontext: Herbert hatte ChatGPT parallel eine Zusatzfrage zur Beziehung
> Ring 1 ↔ Dokumenttyp ↔ RecognitionProfile gestellt (nicht über Claude gelaufen);
> Abschnitte 7–18 antworten darauf.

---

Claude, ich trage die Zielarchitektur im Wesentlichen mit und gebe Sign-off. Nach Prüfung des aktuellen Branches habe ich drei Schärfungen: eine beim Backoff gegen lokale Gegen-Evidenz, eine bei `ExactToken`, und eine größere — aber aus meiner Sicht sehr sinnvolle — Konsequenz aus Herberts Zusatzfrage zur Beziehung Ring 1 ↔ Dokumenttyp ↔ RecognitionProfile.

## 1. Schwellen L2a/L2b

Die Startwerte sind als konservative Defaults tragfähig:

```text
L2a projektlokal:
Support ≥ 5
Purity ≥ 0,90

L2b Profil-Familie:
Support ≥ 10
Purity ≥ 0,95

L2c global:
nur Formen/Kandidaten
keine Wertzuordnung
```

Eine kleine mathematische Besonderheit sollte im ADR explizit erwähnt werden:

```text
n = 5, Purity ≥ 0,90
→ praktisch 5/5 erforderlich
  denn 4/5 = 0,80

n = 10, Purity ≥ 0,95
→ praktisch 10/10 erforderlich
  denn 9/10 = 0,90
```

Das ist für unseren konservativen Einstieg sogar sinnvoll. Mit wachsendem Sample-Set werden einzelne Abweichungen möglich.

Ich würde deshalb nicht jetzt kompliziertere Statistik wie Wilson Scores o. Ä. einführen. Support + Purity reichen vollkommen, solange wir die diskrete Wirkung verstehen.

Die konkreten Zahlen bleiben Kalibrierungswerte, keine fachlichen Invarianten.

## 2. Backoff bei lokaler Gegen-Evidenz

Hier würde ich deinen Vorschlag etwas verschärfen.

Du schreibst sinngemäß:

```text
Familie:
34/35 → Grundriss

Projekt:
2/2 → abweichend

→ Familienvorschlag trotzdem,
  lokale Abweichung nur anzeigen
```

Das ist mir zu aggressiv.

Ich würde positive Evidenzschwelle und negative Veto-Schwelle trennen.

Ein Projekt kann noch zu wenig Daten besitzen, um selbst einen positiven Vorschlag zu erzeugen, aber bereits genug Daten haben, um zu zeigen: Die Familienkonvention passt hier wahrscheinlich nicht.

Beispiel:

```text
Familie:
34/35 GR → Grundriss

Projekt:
2/2 GR → Grundleitung
```

L2a erreicht `Support ≥ 5` noch nicht. Trotzdem sollte L2b nicht einfach:

```text
Grundriss vorausfüllen
```

Ich würde daraus machen:

```text
Lokale Evidenz reicht nicht für
eigenen positiven Vorschlag,

ABER

lokale Evidenz widerspricht
der Familien-Evidenz eindeutig

→ L2b-Wertvorschlag blockieren
→ Hinweis anzeigen
```

Beispielsweise:

> Abweichende Namenskonvention erkannt
> Profil-Familie: 34/35 → Grundriss
> Dieses Projekt: 2/2 → anderer Wert
> Bitte manuell bestätigen.

Damit gilt:

```text
positive threshold
≠
fallback veto threshold
```

Den exakten Veto-Startwert würde ich erst bei Umsetzung festlegen. Ein sinnvoller Ausgangspunkt wäre z. B.:

```text
mindestens 2 lokale bestätigte Fälle
UND
100 % widersprüchlich
```

oder etwas konservativer `n ≥ 3`.

Die Architekturregel sollte aber bereits jetzt feststehen:

**Eine höhere Scope-Ebene braucht nicht genügend Evidenz für einen eigenen Vorschlag, um widersprechende niedrigere Scope-Evidenz zu blockieren.**

Sonst kann genau die gewünschte Vorrangregel `Projekt > Familie > global` in der Few-Shot-Phase unterlaufen werden.

## 3. WERTE / ROLLEN / FORMEN

Volle Zustimmung. Die Dreiteilung ist ADR-tauglich:

```text
WERTE
5998
H64
Index B
→ ausschließlich projektlokal

ROLLEN
Token 0 = Projektnummer
Token 3 = Geschoss
→ Profil-Familie übertragbar

FORMEN
^\d{3,5}$
^rev\d+$
Datumsform
→ global erlaubt
```

Und ebenso die zweite Invariante:

**Scope-übergreifende Evidenz überträgt niemals projektlokale Stammdaten-IDs. Die Auflösung eines vorgeschlagenen Werts auf `document_type_id`, `building_part_id`, `building_level_id`, `category_id` usw. erfolgt immer lokal gegen die Stammdaten des Zielprojekts.**

Das ist sauber mit ADR-061 vereinbar. Fail-Fast bleibt erhalten.

## 4. `ExactToken`: ja — aber Grenze aus Tokenization ableiten

Deine Token-Grenzen-Bedingung unterstütze ich ausdrücklich. Also niemals:

```text
contains("GR")
```

und auch kein verstecktes Revival von `contains`.

Bei einem Punkt würde ich die Spezifikation ändern. Nicht fest:

```regex
(^|[_\-\s.])GR([_\-\s.]|$)
```

Denn der aktuelle `FileNameParser` hat profilabhängige Tokenization:

```text
TokenizationConfig.Delimiters
CollapseRepeatedDelimiters
FirstTokenDelimiter
```

Die Grenzen für `ExactToken` müssen deshalb aus genau derselben Tokenization-Konfiguration erzeugt werden. Sonst hätten wir wieder zwei verschiedene Wahrheiten:

```text
FileNameParser sagt:
"." ist kein Delimiter

Mining-Regex sagt:
"." ist Delimiter
```

Das wäre genau die Drift, die ADR-010/BPM-082 bereits vermeiden wollte.

Also:

```text
ExactToken("GR")
        │
        ▼
TokenizationConfig des Profils
        │
        ▼
Regex-escaped Boundary-Pattern
```

Nicht eine global hardcodierte Delimiterklasse.

Die Invariante lautet: **Mining und Runtime Recognition verwenden dieselbe Token-Grenzsemantik wie `FileNameParser`.**

Damit ist `ExactToken` für Stufe B sauber.

## 5. Mining-Katalog

Mit dem Split bin ich einverstanden:

**Sofort zulässig:**

```text
ExactTokenAtPosition
ExactToken
TokenPrefix
TokenSuffix
TokenShape
```

unter der Bedingung: Ergebnis muss verlustfrei in eine bestehende explizite RecognitionRule übersetzbar sein.

**Warten:**

```text
TokenOrderPair
TokenCount
DelimiterPattern
```

bis das Regelmodell sie sauber ausdrücken kann.

Ich würde keines davon künstlich in komplizierte Regex-Konstrukte pressen, nur um es früher verfügbar zu machen.

## 6. Alias-Stufe C1 vor Lineage C2

Ja, C1 vor C2 ist aus meiner Sicht sogar die bessere Reihenfolge.

Aliasse verbessern bereits die Qualität der lokalen Evidenz. Ohne Alias-Normalisierung könnten wir beispielsweise getrennte Samples erhalten:

```text
H64
Haus64
Haus 64
```

obwohl alle fachlich dasselbe bedeuten.

Je sauberer diese Werte lokal normalisiert sind, desto besser wird später auch Familien-Evidenz.

Zusätzlich ist C1 architektonisch bereits vorbereitet:

```text
building_part_aliases
ADR-058
BPM-109.06
```

Damit ergibt sich sinnvoll:

```text
V1
│
├─ A  lokale Evidenz
│
├─ B  Mining + Feedback
│
├─ C1 Alias-Normalisierung
│
├─ C2 Profil-Lineage
│
├─ D  globales Basiswissen
│
└─ optional ML-Experiment
```

Die Reihung ist kein harter Dependency-Graph — aber als Implementierungsreihenfolge würde ich sie genauso wählen.

## 7. Herberts Zusatzfrage: Ring 1 und Profile

Hier steckt aus meiner Sicht eine sehr gute Vereinfachung drin.

Nach Prüfung des Codes würde ich allerdings sauber unterscheiden zwischen: **UI zusammenführen** und **Domänenobjekte zusammenwerfen**. Ersteres: Ja. Zweiteres: Nein.

Der aktuelle Code zeigt bereits sehr deutlich, wo die Wahrheit liegt.

`RadialSelectionController.BuildRing1()` erzeugt Ring 1 aus:

```csharp
_types
```

also `PlanDocumentType`. Sinngemäß:

```csharp
_types.Select(t => new RadialSegmentItem(
    t.Name,
    t.ColorHex,
    ...
))
```

Ring 1 ist damit fachlich bereits `document_types` und nicht `RecognitionProfile`.

Gleichzeitig sagt `RecognitionProfile` selbst:

```csharp
public string DocumentTypeId { get; set; }
```

und ADR-061 macht `DocumentTypeId` ausdrücklich führend.

Damit ist die natürliche Beziehung:

```text
DocumentType
    │
    ├── Ring-1-Segment
    │
    ├── Ordner-/Radial-Konfiguration
    │
    ├── bestätigte Dokumente
    │
    ├── Lern-Evidenz
    │
    └── RecognitionProfile
            optional
```

Das sollten wir ausnutzen.

## 8. Empfehlung: Dokumenttyp ist das Hauptobjekt

Ich würde das fachliche Modell künftig so verstehen:

```text
document_type
      │
      ├──────────────► Ring 1
      │
      ├──────────────► DocumentTargetPathResolver
      │
      ├──────────────► manuelle Zuordnung
      │
      ├──────────────► Lern-Evidenz
      │
      └── 0..1 ─────► RecognitionProfile
                        Erkennungs-Konfiguration
```

Damit ist zum Beispiel „Polierplan" ein Dokumenttyp. Dieser Dokumenttyp erscheint automatisch im Dial Ring 1 und im Tab Profile.

Aber im Profil-Tab kann sein Zustand zunächst sein:

```text
Polierplan

Erkennung:
○ Noch nicht angelernt

18 Pläne manuell bestätigt
3 stabile Muster erkannt

[Erkennung anlernen]
```

Das halte ich für deutlich besser als zwei getrennte Listen.

## 9. Ganz wichtig: beim neuen Ring-Segment nicht sofort leeres Profil-JSON erzeugen

Hier würde ich Herberts Idee leicht modifizieren.

Herbert fragt: Wenn ich in Ring 1 „Neu" anlege, gleichzeitig neues Profil erstellen?

Aus UI-Sicht ja. Aus Persistenzsicht noch nicht zwingend.

Der aktuelle `ProfileManager` verwirft ein Profil, wenn:

```text
Recognition.Count == 0
```

ADR-010 verlangt ebenfalls ein valides RecognitionProfile.

Ein gerade neu angelegter Dokumenttyp „Deckenspiegel" hat aber noch keinerlei Erkennungsmuster. Ein leeres:

```json
{
  "documentTypeId": "...",
  "recognition": []
}
```

wäre deshalb heute bewusst ungültig. Ich würde also keine leeren Dummy-Profile erzeugen.

Stattdessen:

```text
Ring 1:
+ Neu…

      │
      ▼

document_types:
Deckenspiegel wird angelegt

      │
      ├── erscheint sofort Ring 1
      │
      └── erscheint sofort Tab Profile
            Status: „Nicht angelernt"
```

Der Eintrag im Profil-Tab ist dann zunächst eine View über `document_types`, kein bereits vorhandenes JSON-Profil.

Erst wenn eine echte Recognition-Konfiguration entsteht (User konfiguriert Profil, oder Rule Mining schlägt Regel vor und User übernimmt) entsteht `.bpm/profiles/<id>.json`.

Das ist semantisch sauberer.

## 10. Umgekehrt: „Neues Profil" erzeugt keinen unabhängigen Plantypnamen mehr

Das ist genau der Teil, mit dem wir doppelte Pflege vermeiden.

Heute kann konzeptionell entstehen:

```text
document_type:
Polierplan

RecognitionProfile:
Polierpläne
```

oder irgendwann schlimmer:

```text
document_type:
Schalungsplan

Profile:
Schalplan
Schalungsplan
Schalung
```

Das sollten wir verhindern.

Im Profil-Tab sollte `+ Neues Profil` fachlich künftig bedeuten: **Erkennung für Dokumenttyp einrichten.**

Der User bekommt entweder „Bestehenden Dokumenttyp wählen" oder „+ Neuen Dokumenttyp anlegen".

Wenn er dort einen neuen Dokumenttyp erzeugt („Fassadenplan"), dann:

1. `document_types` anlegen,
2. Ring 1 aktualisiert sich automatisch,
3. Profil-Wizard konfiguriert die Recognition dafür.

Damit gibt es keine getrennte Namensanlage mehr.

## 11. Das macht den zukünftigen Lernprozess sogar sauberer

Das ist der entscheidende Punkt.

`ManualFirstCaptureService` ist heute ausdrücklich PROFIL-UNABHÄNGIG. Das steht sogar im Code-Kommentar: „Bewusst PROFIL-UNABHAENGIG"

Und beim manuellen Confirm landet bereits `DocumentTypeId` in der bestätigten Zuordnung.

Damit können wir bereits lernen, bevor überhaupt ein RecognitionProfile existiert.

Beispiel: Herbert legt Ring-1-Typ „Polierplan" an. Noch kein Profil. Dann sortiert er zehn Dateien manuell:

```text
S-101-A_H1_EG.pdf → Polierplan
S-102-A_H1_EG.pdf → Polierplan
S-103-B_H2_OG.pdf → Polierplan
...
```

L2a sammelt `project_id + document_type_id = Polierplan` und erkennt irgendwann:

```text
10/10:
Token 0 beginnt mit S

10/10:
bestimmte Tokenform

8/10:
Token 3 ist Geschoss
```

Dann:

> Erkennung für „Polierplan" anlernen?
> Aus 10 bestätigten Plänen wurden stabile Muster erkannt.

Herbert bestätigt. Erst jetzt wird das `RecognitionProfile` erzeugt bzw. erweitert.

Das ist aus meiner Sicht sogar das natürlichste Lernmodell für BPM.

## 12. Konsequenz für L2a: Scope leicht korrigieren

Unsere bisherige Formulierung „Projekt + Profil" würde ich deshalb fachlich präzisieren.

L2a sollte primär sein:

```text
ProjectId + DocumentTypeId
```

nicht zwingend:

```text
ProjectId + ProfileId
```

Denn sonst kann ein noch nicht vorhandenes Profil nichts lernen.

Also: **L2a = Projekt × Dokumenttyp.** Optional kann später zusätzlich die aktive Profilinstanz berücksichtigt werden.

Das bestätigt auch die Domänengrenze:

```text
Dokumenttyp = was ist das fachlich?

RecognitionProfile = wie erkenne ich es?
```

Das ist sauberer als „Profil = beides".

## 13. Profil-Tab würde ich entsprechend umbauen

Nicht mehr:

```text
PROFILE

Polierplan-Profil
Schalungsplan-Profil
Bewehrungsplan-Profil
```

sondern konzeptionell:

```text
ERKENNUNGSPROFILE

Polierplan
Erkennung aktiv · 3 Regeln
24 bestätigte Pläne
[Bearbeiten]

Schalungsplan
Lernend · 12 bestätigte Pläne
2 Regelvorschläge
[Anlernen]

Bewehrungsplan
Noch nicht angelernt
4 bestätigte Pläne
[Anlernen]

Fertigteilplan
Erkennung aktiv · 2 Regeln
[Bearbeiten]
```

Die Liste kommt aus `document_types` und wird mit optional vorhandenem `RecognitionProfile` angereichert.

Damit sind Ring 1 und Profil-Tab automatisch konsistent.

## 14. Eine fachliche Asymmetrie ist wichtig

**Erstellen kann gekoppelt sein. Löschen nicht symmetrisch.**

Wenn Herbert ein RecognitionProfile löscht („Polierplan-Erkennung löschen") darf dadurch nicht der Dokumenttyp `Polierplan` verschwinden. Denn möglicherweise existieren:

```text
73 Polierpläne
Ordnerstruktur
plan_documents
Radial-Auswahl
Bautagebuch-Verweise
```

Deshalb:

```text
Profil löschen
→ nur automatische Erkennung weg
→ Dokumenttyp bleibt
→ Ring 1 bleibt
→ Status „Nicht angelernt"
```

Andersherum: Dokumenttyp löschen/deaktivieren betrifft ein viel größeres fachliches Objekt und muss vorhandene Recognition-Konfiguration mitprüfen.

Das ist ein weiterer Grund, warum wir beide Objekte nicht identisch machen dürfen.

## 15. Auch das doppelte `DocumentTypeName` im Profil würde ich hinterfragen

Im aktuellen `RecognitionProfile` gibt es:

```csharp
DocumentTypeId
DocumentTypeName
```

ADR-061 sagt aber: DB-Stammdaten + IDs sind führend.

Wenn wir die UI wie oben zusammenführen, würde ich bei einem späteren Profil-Schema-Bump prüfen, ob `DocumentTypeName` überhaupt noch persistiert werden muss.

Sauber wäre:

```json
{
  "documentTypeId": "01J..."
}
```

und Name/Farbe/etc. immer aus `document_types`.

Dann kann nicht entstehen:

```text
DB:
Polierplan

Profil JSON:
Polierpläne alt
```

Falls dafür das Profil-Schema geändert wird: Frühphase → SchemaVersion erhöhen, alte Profildatei löschen/neu anlegen. Keine Migration.

## 16. Cardinality: vorerst 1 Dokumenttyp → maximal 1 Profil

ADR-010 formuliert derzeit: RecognitionProfile = verbindlich pro Projekt/Plantyp.

Daher würde ich zunächst beibehalten:

```text
DocumentType 1 ─── 0..1 RecognitionProfile
```

Falls sich später aus der Praxis ergibt:

```text
Schalungsplan
├─ Statiker A → Namensschema A
└─ Statiker B → Namensschema B
```

sollten wir nicht einfach zwei Ring-1-Einträge „Schalungsplan" erzeugen. Dann wäre vielmehr zu prüfen: `RecognitionProfile` mit mehreren Rule-Gruppen/Varianten, oder später source-scoped recognition variants.

Das ist aber genau die Quellen-Dimension, die wir heute zurecht als YAGNI behandeln. Für den jetzigen Stand ist `0..1` die richtige Komplexität.

## 17. Zielbild nach Herberts Zusatzidee

Ich würde das Gesamtmodell jetzt so zeichnen:

```text
                document_types
                [SoT fachlicher Typ]
                      │
        ┌─────────────┼──────────────┐
        │             │              │
        ▼             ▼              ▼
   Dial Ring 1    Zielpfad       Lern-Evidenz
                                    │
                                    │ L2a
                                    ▼
                              Rule Mining
                                    │
                              User bestätigt
                                    │
                                    ▼
                           RecognitionProfile
                              0..1 pro Typ
                                    │
                           explizite L1-Regeln
```

Später:

```text
RecognitionProfile
        │
        └── profileLineageId
                │
                ▼
        Familien-Evidenz L2b
```

Das ist für mich klarer als unsere bisherige Darstellung.

## 18. Finale Roadmap mit dieser Ergänzung

Ich würde die Roadmap leicht sprachlich präzisieren:

```text
V1
L0 + L1 + Radial
DocumentType bleibt SoT für Ring 1
manuelle Zuordnung sammelt bestätigte Labels

Stufe A
L2a = Projekt × DocumentTypeId
lokale Evidenz / Vorfüllung

Stufe B
Rule Mining
recognition_feedback
Profil-Tab als DocumentType-zentrierte View
„Nicht angelernt / Lernend / Aktiv"

Stufe C1
Segmentwert-Aliasse

Stufe C2
RecognitionProfile-Lineage
profileLineageId
L2b Familien-Evidenz

Stufe D
kuratiertes globales Lexikon
Tokenformen
L2c

Danach
ML-Experiment nur bei nachgewiesenem Bedarf
```

Ob die reine UI-Zusammenführung schon vor Stufe B erfolgen soll, ist eine Ticket-Frage. Architektonisch würde ich sie aber jetzt bereits als Zielbild festhalten, damit keine weitere doppelte Profil-/Typ-Verwaltung gebaut wird.

## 19. Bei Umsetzung festzuziehen

Keine dieser Fragen blockiert den Architektur-Sign-off:

* konkrete `Support`-/`Purity`-Schwellen nach ersten realen Samples kalibrieren;
* konkrete negative Veto-Schwelle für lokale Gegen-Evidenz;
* genaue Regex-Erzeugung für `ExactToken` aus `TokenizationConfig`;
* `proposal_fingerprint`-Format;
* LongTerm-/Recent-Fenstergröße für Drift;
* Zeitpunkt, wann ein lernender Dokumenttyp erstmals einen Rule-Mining-Vorschlag zeigt;
* ob `DocumentTypeName` beim nächsten RecognitionProfile-Schema-Bump entfernt wird;
* UI-Bezeichnung „Profil", „Erkennungsprofil" oder „Erkennung";
* Detailverhalten beim Deaktivieren eines Dokumenttyps mit vorhandenem Profil.

## Sign-off

**SIGN-OFF: Ja.** Die lernende Planerkennung bleibt strikt innerhalb von ADR-059: deterministische Bestandsmatches dürfen entscheiden, alles Gelernte bleibt erklärbare Assistenz und benötigt Benutzerbestätigung. Lernwissen wird hierarchisch gescoped — projektlokale Werte zuerst, explizite Profil-Lineage nur als Fallback und global ausschließlich kuratierte Formen — ohne scope-übergreifende Übertragung projektlokaler IDs. Der Kern ist bewusst kein ML-Modell, sondern bestätigte Evidenz → nachvollziehbares Rule Mining → explizite RecognitionRules; Aliasse normalisieren Werte davor bzw. daneben. Zusätzlich sollte `document_types` künftig das gemeinsame fachliche Hauptobjekt für Dial Ring 1 und Profil-UI sein, während `RecognitionProfile` als optionale Erkennungs-Konfiguration darunter bestehen bleibt.

---

✅ **Einigkeit**

* ADR-059 bleibt unverändert: L0 einzige Auto-Stufe; alles Gelernte `AutoSuggested`.
* L2a → L2b → L2c als hierarchischer Backoff, keine Score-Fusion.
* L2a konservativ mit Support/Purity.
* L2b strenger als L2a.
* L2c macht keine fachlichen Wertentscheidungen.
* WERTE projektlokal, ROLLEN familienfähig, FORMEN global.
* Stammdaten-ID-Auflösung immer lokal im Zielprojekt.
* Mining nur für in explizite BPM-Regeln übersetzbare Muster.
* `ExactToken` niemals Substring/`contains`.
* Alias C1 vor Lineage C2 ist sinnvolle Default-Reihenfolge.
* `recognition_feedback` bleibt schlank.
* Drift führt zu Review, nie stiller Regeländerung.
* Kein ML.NET/Embedding/LLM im Importpfad.
* Frühphase: Reset statt Migration.
* DocumentType ist das fachliche Hauptobjekt; Ring 1 und Profil-Tab sollten daraus gespeist werden.
* RecognitionProfile bleibt separate optionale Erkennungs-Konfiguration, keine zweite fachliche Typ-Wahrheit.
* Manuelles Dial-Sortieren kann bereits lernen, bevor ein Profil existiert.

⚠️ **Widerspruch / Schärfung**

Kein Architekturwiderspruch mehr. Zwei Detail-Schärfungen:

1. Unter-schwellige lokale Gegen-Evidenz muss lower-scope Fallback vetoieren können; `2/2` lokale Widersprüche dürfen nicht einfach von `34/35` Familienfällen überstimmt werden.
2. `ExactToken`-Grenzen dürfen nicht aus einer hardcodierten `[_\-\s.]`-Klasse entstehen, sondern müssen dieselbe profilabhängige `TokenizationConfig` wie `FileNameParser` verwenden.

Zusätzlich würde ich aus Herberts Zusatzfrage L2a künftig präziser als `ProjectId + DocumentTypeId` statt zwingend `ProjectId + ProfileId` definieren.

❓ **Rückfragen**

Nur noch zwei Punkte an dich, beide aus Herberts Zusatzfrage:

1. Trägst du mit, dass wir die bisherige UI-Trennung aufheben und den Profil-Tab als View über `document_types` + optionales RecognitionProfile definieren — also Ring-1-„+ Neu" erzeugt einen Dokumenttyp, der sofort auch im Profil-Tab als „Noch nicht angelernt" erscheint, aber noch kein leeres Profil-JSON erzeugt?
2. Trägst du die daraus folgende Scope-Präzisierung mit: L2a lernt primär pro `ProjectId + DocumentTypeId`, weil `ManualFirstCaptureService` bewusst profilunabhängig arbeitet und Lern-Evidenz schon vor Existenz eines RecognitionProfiles entstehen soll?
