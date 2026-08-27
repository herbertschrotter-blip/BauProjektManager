# Review Runde 1 — ChatGPT-Antwort

> Archiviert 1:1 aus dem ChatGPT-Canvas „Review Runde 1" (via Herbert).
> Herberts eigene Anschlussfrage („aber gewisse dinge wie projektnummer …")
> ist NICHT Teil der ChatGPT-Antwort → siehe 03-claude-analysis.md / 04-user-decisions.md.

---

Claude, bei der ADR-059-Grenze trage ich deine Korrektur vollständig mit. Mein ursprüngliches „Confidence hoch → automatisch" war für BPM falsch. Nach Prüfung von ADR-059, ADR-056, ADR-058/061 und des aktuellen Codes ist die Grenze eindeutig:

Lernen darf die Assistenz verbessern, aber keine neue Schreibberechtigung erzeugen.

`AutoSuggested` bleibt Vorschlag. Nur `ManualConfirmed` oder `ExistingDocumentMatch` dürfen den Import auslösen.

Beim Lern-Scope würde ich deine Position allerdings erweitern. „Projekt × Profil" ist für den ersten Ausbau richtig, aber als endgültiges Modell zu eng. Ich würde nicht zwischen global, Profil oder Projekt genau eine Ebene auswählen, sondern ein hierarchisches Evidenzmodell mit klarer Vorrangregel vorsehen.

## 1. Wichtige Korrektur zum Begriff „Profil = Quelle"

Hier widerspreche ich dir teilweise.

Im aktuellen Code ist ein `RecognitionProfile` ausdrücklich projektlokal:

```csharp
/// Manages RecognitionProfiles per project.
/// Profiles are stored as individual JSON files in .bpm/profiles/
```

`ProfileManager.Save()` registriert das Profil zudem als:

```text
PersistenceScope.ProjectLocal
```

Und fachlich hängt das Profil an `DocumentTypeId` und seinen Recognition-/Segment-Regeln.

Damit ist ein Profil aktuell nicht stabil dasselbe wie „Planungsbüro/Quelle". Das ist wichtig.

Ein Statiker kann innerhalb eines Projekts mehrere Dokumenttypen liefern. Umgekehrt können mehrere Büros denselben Dokumenttyp liefern. Und dasselbe Büro kann über mehrere Projekte hinweg dieselbe Namenskonvention verwenden.

Deshalb würde ich nicht sagen:

```text
Profil = Absender
```

sondern eher:

```text
Profilinstanz = projektlokale Recognition-Konfiguration
```

Wenn wir später Wissen projektübergreifend übertragen wollen, brauchen wir eine explizite Herkunft/Lineage und dürfen nicht zufällig Profile gleichen Namens zusammenwerfen.

## 2. Meine Empfehlung: hierarchisches Lernen statt genau eines Scopes

Ich würde drei Ebenen unterscheiden:

```text
                 neue Datei
                     │
                     ▼
        ┌─────────────────────────┐
        │ Projekt + Profilinstanz │  höchste Relevanz
        │ lokale Evidenz          │
        └────────────┬────────────┘
                     │ falls nicht ausreichend
                     ▼
        ┌─────────────────────────┐
        │ Profil-Familie/Lineage  │  mittlere Relevanz
        │ mehrere Projekte        │
        └────────────┬────────────┘
                     │ falls nicht ausreichend
                     ▼
        ┌─────────────────────────┐
        │ Globales Basiswissen    │  niedrigste Relevanz
        │ Kürzel / Tokenformen    │
        └────────────┬────────────┘
                     │
                     ▼
                 Vorschlag
                     │
                     ▼
             User bestätigt
```

Aber wichtig: Ich würde diese Ebenen nicht mit beliebigen Prozentwerten zusammenschütten.

Also nicht:

```text
Projekt       60 %
Profil        30 %
Global        10 %
--------------
Score         91 %
```

Das wäre wieder schwer erklärbare Scheinpräzision.

Stattdessen würde ich Backoff mit Vorrang verwenden:

```text
1. Gibt es ausreichend starke projektlokale Evidenz?
   → diese gewinnt.

2. Sonst: Gibt es starke Evidenz aus derselben Profil-Familie?
   → diese verwenden.

3. Sonst: Gibt es globales generisches Wissen?
   → schwachen Vorschlag liefern.

4. Sonst:
   → unbekannt / manuell.
```

Damit bekommt Herbert genau die gewünschte „Wertigkeit", ohne ein undurchsichtiges Scoring-System einzubauen.

## 3. Welche Information darf auf welcher Ebene lernen?

Das ist aus meiner Sicht die eigentliche Lösung der Scope-Frage. Nicht jedes Wissen ist gleich übertragbar.

### A. Projektlokal

Hier dürfen sehr spezifische Dinge gelernt werden:

```text
H64 → building_part_id 01...
EG  → building_level_id 01...
GR  → Dokumenttyp Grundriss in diesem Projekt
5998 → Statiker-Nummernkreis dieses Projekts
```

Besonders:

* `building_part_id`
* `building_level_id`
* `category_id`
* konkrete Plan-Nummernkreise
* projektspezifische Kürzel
* lokale Ausnahmen

gehören niemals ungeprüft in globales Lernen.

ADR-061 verlangt am Ende konkrete DB-IDs. Diese IDs sind projektspezifisch.

### B. Profil-Familie / Lineage

Hier sehe ich den größten späteren Mehrwert.

Beispiel: Ein Statikbüro liefert in Projekt A:

```text
5998-003_Wände_KG.pdf
5998-004_Decke_EG.pdf
5998-105_Wände_OG.pdf
```

Dann beginnt Projekt B vom selben Büro mit:

```text
6142-003_Wände_KG.pdf
```

Die Projektnummer `5998 → 6142` hat sich geändert, aber die Struktur ist geblieben.

Das Wissen:

```text
Token 0 = Projektnummer
Token 1 = Plannummer
Token 2 = Beschreibung/Bauteil
Token 3 = Geschoss
```

ist übertragbar. Die konkreten IDs dagegen nicht.

Genau deshalb wäre eine Profil-Familie interessanter als globales Lernen.

Ich würde dafür später etwas wie eine stabile:

```text
profileLineageId
```

oder

```text
templateOriginId
```

vorsehen.

Wenn Herbert ein Profil kopiert bzw. als Vorlage für ein neues Projekt verwendet:

```text
Statiker Müller / Schema 2026
             │
       ┌─────┴─────┐
       ▼           ▼
Projekt A       Projekt B
Profilinstanz   Profilinstanz
```

wissen beide Profile: Wir stammen aus derselben Recognition-Familie. Dann kann BPM bewusst Evidenz übertragen. Nicht über Namensgleichheit. Nicht über DocumentTypeId. Nicht implizit.

Das wäre mein Gegenentwurf zu „Profil exportieren und danach sind es einfach unabhängige Profile".

### C. Global

Global würde ich deutlich restriktiver sein als Herbert zunächst vielleicht erwartet.

Global sinnvoll:

```text
rev03    → wahrscheinlich Revision
Index B  → mögliche Revision
20260827 → wahrscheinlich Datum
EG       → häufig Geschoss
OG       → häufig Geschoss
RCP      → gebräuchliches Plan-Kürzel
MEP      → gebräuchliches Gewerke-Kürzel
```

Außerdem Token-Formen:

```text
^\d{8}$        → Datumskandidat
^rev\d+$       → Revisionskandidat
^[A-Z]\d?$     → möglicher Index
^\d{3,5}$      → Nummernkandidat
```

Global nicht:

```text
GR → immer Grundriss
BA → immer Bauabschnitt
H1 → immer Haus 1
```

Denn genau diese Kürzel sind kontextabhängig. Globales Wissen ist für mich deshalb eher ein Prior/Lexikon, kein globaler Klassifikator.

## 4. Daraus ergibt sich eine leicht veränderte Zielarchitektur

Deine L0–L3-Struktur gefällt mir. Ich würde L2 nur differenzieren:

```text
L0  Deterministisches Bestands-Matching
    MD5 / bestehender document_key
    → darf gemäß ADR-059 entscheiden

L1  Explizite Profilregeln
    RecognitionRule / später FieldExtractionRule
    → AutoSuggested

L2a Projekt-/Profil-Evidenz
    bestätigte Dokumente dieses Projekts
    → stärkster lernender Vorschlag

L2b Profil-Familien-Evidenz
    bestätigte Muster aus explizit verwandten Profilinstanzen
    → Fallback

L2c Globales Basiswissen
    kuratierte Kürzel + allgemeine Tokenformen
    → schwächster Fallback

L3  Rule Mining
    stabile Evidenz → expliziter Regelvorschlag
    User übernimmt → normale L1-Regel
```

Damit bleibt deine zentrale Idee erhalten: Lernen kristallisiert am Ende möglichst in explizite Regeln. Das halte ich nach dem Repo-Review weiterhin für die richtige Richtung.

## 5. Zu deiner Frage 1 — ADR-059-Grenze

Ja, vollständig getragen.

Mein früheres:

```text
Confidence hoch → automatisch
```

streiche ich.

Richtig ist:

```text
MD5 / ExistingDocumentMatch
    → deterministische Auto-Stufe

alles Gelernte
    → AutoSuggested
    → User bestätigt
```

Selbst ein Muster mit:

```text
127 / 127 Treffern
```

bekommt dadurch keine neue Berechtigung.

Wenn wir Zero-Touch später irgendwann neu diskutieren wollen, wäre das eine bewusste Wiedereröffnung von ADR-059 und kein Nebenprodukt des Lernsystems.

## 6. Zu deiner Frage 2 — reicht Token-/Präfix-Mining?

Nur positionsbasiertes Token-Mining reicht mir nicht. Gerade ADR-059 entstand, weil variable Tokenpositionen in realen Statikdateien gescheitert sind.

Ich würde einen kleinen, kontrollierten Feature-Katalog minen:

```text
ExactTokenAtPosition
ExactToken
TokenPrefix
TokenSuffix
TokenShape
TokenCount
TokenOrderPair
DelimiterPattern
```

Beispiele:

```text
Token[2] == "GR"

Token "GR" kommt irgendwo vor

Token beginnt mit "REV"

Token entspricht \d{3}[A-Z]

"AR" erscheint vor "GR"

Dateiname enthält 6 Tokens
```

Wichtig ist aber eine harte Designregel:

**Mining darf nur Muster erzeugen, die anschließend in eine explizite BPM-Regel übersetzbar sind.** Kein verstecktes Feature-Modell.

Wenn unsere aktuelle `RecognitionRule` nur `segment` und `regex` kann, müssen gemined-te Ergebnisse entweder darin ausdrückbar sein oder auf die bereits geplante `FieldExtractionRule` warten.

Ich würde insbesondere nicht heimlich `contains` wieder einführen. Diese Methode wurde bewusst entfernt.

## 7. Zu deiner Frage 3 — Feedback-Tabelle

Hier entscheide ich mich für deine schlanke Feedback-Tabelle.

Nicht als Trainingsdatensatz. `plan_document_segments` bleibt die Wahrheit über bestätigte Dokumentwerte.

Aber nur daraus können wir nicht beantworten: „Welche automatische Vermutung war falsch?"

Beispiel:

```text
Vorschlag:
GR → Grundriss

Bestätigung:
Grundleitung
```

Der Endzustand sagt lediglich:

```text
Grundleitung
```

Wir verlieren sonst die wichtige Information:

```text
welches Muster welchen falschen Vorschlag erzeugt hat
```

Diese Information brauchen wir für:

* Rule-Review
* False-Positive-Rate
* Drift-Erkennung
* Akzeptanzrate
* Vergleich der Ausbaustufen

Daher:

```text
recognition_feedback
```

aber wirklich schlank. Zum Beispiel konzeptionell:

```text
project_id
profile_id
field_type_id
proposal_source
proposal_fingerprint
proposed_value
confirmed_value
outcome
created_at
```

`outcome`:

```text
confirmed
corrected
rejected
```

Kein Sample-Duplikat, kein Modellzustand.

Wenn dafür später eine DB-Änderung erfolgt, gilt selbstverständlich die Frühphasen-Regel: DB löschen und neu erzeugen, keine Migration.

## 8. Die Feedback-Tabelle wird bei Cross-Project-Lernen sogar wichtiger

Hier sehe ich noch einen Grund, warum wir die Scope-Frage jetzt architektonisch sauber denken sollten.

Wenn Projekt A sagt:

```text
GR → Grundriss   24/24
```

Projekt B aber sagt:

```text
GR → Grundleitung   8/8
```

darf ein globaler Aggregator daraus nicht einfach:

```text
Grundriss 75 %
```

machen. Das wäre fachlich falsch.

Stattdessen sehen wir:

```text
Global:
GR = mehrdeutig

Profil-Familie Statiker X:
GR → Grundleitung

Profil-Familie Architekt Y:
GR → Grundriss

Projekt A:
GR → Grundriss
```

Genau deshalb halte ich hierarchische Evidenz für wesentlich sauberer als „eine globale KI lernt alles".

## 9. Zu deiner Frage 4 — Quellen-Dimension

Heute: YAGNI.

Ich würde noch keine:

```text
source_id
issuer_id
mail_sender
portal_id
```

in die Lernarchitektur einbauen.

Aber ich würde die Services so schneiden, dass später ein optionaler Evidence Context möglich ist:

```text
RecognitionContext
- ProjectId
- ProfileId
- ProfileLineageId?
- SourceId?          // später
```

Der heutige Code hat bereits einen `RecognitionContext` im `DocumentTypeRecognizer`, allerdings nur intern für Filename/Tokens. Den Begriff würde ich nicht zwingend wiederverwenden, aber das Architekturprinzip passt.

Erst wenn BPM tatsächlich mehrere Eingangskanäle kennt und wir feststellen: „Dateien von Portal X sehen immer anders aus als Dateien von Mail Y" bekommt `SourceId` Persistenz.

## 10. Zu deiner Frage 5 — Concept Drift

Kein automatischer Evidenz-Reset und kein stilles Recency-Umschreiben.

Ich würde zwei Fenster beobachten:

```text
LongTerm
Recent
```

Beispiel:

```text
Regel:
Token 2 = GR → Grundriss

historisch:
41 / 43 = Grundriss

letzte 8:
3 / 8 = Grundriss
```

Dann nicht:

```text
Regel automatisch ändern
```

sondern: „Namensschema scheint sich geändert zu haben. Profilregel prüfen."

Das ist genau dieselbe Philosophie wie beim Rest des PlanManagers.

Mögliche Zustände:

```text
Stable
DriftSuspected
ReviewRequired
```

Eine bestehende L1-Regel wird nie automatisch gelöscht oder verändert.

Wenn Herbert bestätigt: „Ja, das Büro hat auf ein neues Namensschema umgestellt" würde ich eher eine neue Regelgeneration/Profilgeneration beginnen als historische Evidenz zu vernichten.

Aber das ist klar post-V1; jetzt reicht es, diese Richtung nicht zu verbauen.

## 11. ML.NET / Embeddings / LLM nach dem Repo-Review

Hier bin ich inzwischen näher bei dir als bei meinem ursprünglichen Vorschlag.

**ML.NET:** Für den geplanten ersten Lernschritt: nein. Wir haben zu wenig Daten, stark strukturierte Dateinamen und benötigen Explainability.

**Embeddings:** Für Dateinamenerkennung: nein. Für späteren freien Plankopftext könnte man sie neu bewerten, aber nicht für:

```text
GR
H64
E2
5998
rev03
```

**LLM:** Nicht im Importpfad. Ein LLM könnte später on-demand helfen bei: „Was bedeutet vermutlich RCP?" aber das ist Assistenzwissen und keine fachliche Identität.

**Rule Mining:** Ja — Priorität 1.

## 12. Wo ich deine Roadmap ändern würde

Ich würde deine Ausbaustufen leicht anpassen:

**V1** — Unverändert:

```text
L0 + L1 + Radial
```

Kein neuer Lern-Scope.

**Post-V1 A**

```text
Projektlokale Evidenz
```

Nur aus dem aktuellen Projekt. Hier können wir das Konzept beweisen.

**Post-V1 B**

```text
Rule Mining + recognition_feedback
```

Damit messen wir erstmals sauber Akzeptanz und Fehler.

**Post-V1 C** — Nicht sofort „global", sondern:

```text
Profile Lineage / wiederverwendbare Profil-Familien
```

Beispiel:

```text
Profil als Vorlage für neues Projekt übernehmen
→ Lineage bleibt erhalten
→ strukturelles Wissen kann übertragen werden
```

Das ist für Herberts Praxis wahrscheinlich wesentlich wertvoller als echtes Global-Lernen.

**Post-V1 D**

```text
Globales Lexikon + generische Tokenformen
```

bewusst konservativ.

**Erst danach** — Falls reale Daten zeigen, dass diese Architektur nicht genügt:

```text
ML-Experiment
```

## 13. Mein konkreter Vorschlag zur Scope-Entscheidung

Ich würde den ADR später nicht formulieren als: „BPM lernt pro Projekt." und auch nicht: „BPM lernt global."

Sondern:

**Recognition-Evidenz ist hierarchisch gescoped. Lokale Projekt-/Profil-Evidenz besitzt Vorrang vor explizit verknüpfter Profil-Familien-Evidenz; globales Wissen dient ausschließlich als schwacher, nicht projektidentitätsbildender Fallback. Projektgebundene Stammdaten-IDs werden niemals scope-übergreifend gelernt oder übertragen.**

Das ist aus meiner Sicht die robuste Antwort auf Herberts Unsicherheit.

Und praktisch bedeutet das:

```text
Projektwissen       █████  höchste Wertigkeit
Profil-Familie      ███    mittlere Wertigkeit
Globales Wissen     █      niedrige Wertigkeit
```

aber als Vorrang-/Backoff-System, nicht als mathematische Prozentmischung.

## 14. Ein Beispiel, warum das besser ist

Neue Datei in Projekt C:

```text
7351-204_GR_EG_B.pdf
```

**Projekt C:** Bisher nur zwei Dateien. Keine ausreichende lokale Evidenz.

**Profil-Familie „Architekt Müller"** — Aus drei früheren Projekten:

```text
GR → document type Grundriss
34 / 35
```

und:

```text
EG → Spatial/Geschoss
47 / 47
```

BPM kann vorfüllen:

```text
Planart: Grundriss
Geschoss: EG
```

aber die lokalen IDs werden erst gegen Projekt C aufgelöst:

```text
"EG"
    │
    ▼
building_levels des gewählten Bauteils
    │
    ▼
exakter Treffer
    │
    ▼
building_level_id = 01J...
```

Wenn Projekt C später fünfmal zeigt:

```text
GR → Grundleitung
```

dann gewinnt die lokale Evidenz sofort gegenüber der Profil-Familie.

Das ist genau die Art „Intelligenz", die ich für BPM möchte: adaptiv, aber kontrollierbar.

---

✅ **Einigkeit**

* ADR-059 bleibt unangetastet: Lernen verbessert Assistenz, nicht Schreibrechte.
* `AutoSuggested` bleibt Vorschlag.
* L0 ist die einzige deterministische Auto-Entscheidung.
* `plan_document_segments` ist der bestehende bestätigte Datensatz; kein separater Sample-Store.
* Rule Mining ist derzeit wertvoller als ML.NET/Embeddings/LLM.
* Gemined-te Regeln werden normale explizite Profilregeln; keine zweite Regel-Wahrheit.
* Positionsreine Erkennung allein genügt wegen der Praxisprobleme aus ADR-059 nicht.
* Eine schlanke `recognition_feedback`-Historie ist sinnvoll.
* Drift führt zu Review, nie zu stiller Regeländerung.
* Keine Migrationen; Frühphase = löschen und neu erzeugen.

⚠️ **Widerspruch**

* „Projekt × Profil" sehe ich als ersten Implementierungsscope, aber nicht als endgültige Architektur.
* Ein aktuelles `RecognitionProfile` ist laut Code projektlokale Recognition-Konfiguration und nicht automatisch eine Quelle/ein Planungsbüro.
* Für sinnvolles Cross-Project-Lernen brauchen wir später eine explizite Profil-Lineage/Profil-Familie.
* Globales Lernen würde ich nicht komplett ausschließen, aber stark begrenzen: generische Tokenformen/Lexikon ja, projektgebundene Bedeutungen und IDs nein.
* Reines Präfix-/Positions-Mining ist zu eng; wir brauchen einen kleinen kontrollierten Feature-Katalog, dessen Ergebnisse immer in explizite Regeln übersetzbar bleiben.

❓ **Rückfragen**

1. Trägst du das hierarchische Backoff-Modell `Projekt/Profil → Profil-Lineage → globales Basiswissen` grundsätzlich mit, statt den Scope dauerhaft auf Projekt × Profil festzulegen?
2. Siehst du ebenfalls das Problem, dass ein aktuelles `RecognitionProfile` fachlich nicht dieselbe Entität wie Planungsbüro/Quelle ist?
3. Würdest du eine stabile `profileLineageId` für kopierte/wiederverwendete Profile bevorzugen, oder siehst du einen schlankeren Mechanismus, mit dem BPM zweifelsfrei erkennen kann: „Diese beiden Profile repräsentieren dieselbe Namenskonvention"?
4. Bist du mit der Regel einverstanden, dass scope-übergreifend nur strukturelles/semantisches Wissen übertragen wird, niemals konkrete projektlokale Stammdaten-IDs?
5. Beim Mining: Würdest du zusätzlich zu `ExactTokenAtPosition` auch `ExactToken`, `TokenShape` und einfache Reihenfolgebeziehungen aufnehmen, oder ist dir das bereits zu breit für Ausbaustufe A/B?
