# Review Runde 2 — Plan-Archiv-Architektur

Kollege, ich würde Herberts Entscheidung **mittragen**, aber nur als **kontrollierten Vor-V1-Schema-Schnitt**, nicht als offenen Architektur-Block.

Der entscheidende Punkt ist: Die Projektregeln erlauben jetzt noch bewusst harte Schema-Resets ohne Migration, Backward-Compatibility und Legacy-Toleranz. Das ist im Branch `feature/planmanager-v1` in `INDEX.md` klar festgelegt. Genau deshalb ist „vor V1" technisch vertretbar. Nach V1 wird derselbe Schnitt politisch und organisatorisch teurer, selbst wenn noch keine echten Kundendaten existieren.

Meine frühere Empfehlung „nach V1" war release-taktisch konservativ. Herberts Entscheidung ist architektonisch sauberer — **wenn ihr den Scope hart begrenzt**.

## 1. Ist „vor V1" vertretbar?

Ja, aber nur unter drei Bedingungen:

| Bedingung | Warum zwingend |
| --- | --- |
| Plan-Archiv wird als **Foundation Slice** gebaut, nicht als komplettes Post-V1-Modul | Sonst frisst es den V1-Endsprint |
| BPM-080.05 pausiert oder wird nur an schemaunabhängigen Teilen weitergeführt | Sonst entsteht Wegwerfware |
| V1-Kriterium bleibt: Import/Sortierung stabil, nicht Bautagebuch-Ready mit allen Komfortfunktionen | Sonst rutscht V1 in Richtung Plattform-Release |

Der Vor-V1-Schnitt ist sinnvoll, weil der PlanManager selbst bereits harte Persistenz-Invarianten hat: Journal vor Dateioperation, MD5 + file_size, relative Pfade und Undo-Preflight. Diese Invarianten berührt ihr ohnehin beim Schema-v2-Umbau. Wenn ihr das nach V1 macht, müsst ihr denselben Kernbereich später wieder anfassen.

**Mein Urteil:** Vor V1 ist vertretbar, aber nicht als „alle 7 Subtasks voll ausbauen". Nur das Fundament muss vor V1 rein.

---

## 2. Empfohlene Reihenfolge

Ich empfehle Variante **(c): Foundation zuerst, UI danach, Komfort später**.

Nicht (a), weil „alle 7 Subtasks zuerst" zu groß ist.
Nicht ganz (b), weil .04–.07 nicht sauber zwischen 080.05 und 081 hineingeschoben werden sollten.

### Roadmap-Vorschlag

| Phase | Task | V1-blockierend? | Ziel |
| ---: | --- | ---: | --- |
| 1 | Plan-Archiv `.01 Schema` | Ja | Neue DB-Struktur finalisieren |
| 2 | Plan-Archiv `.02 Domain/Repository` | Ja | Neue Entitäten technisch nutzbar |
| 3 | Plan-Archiv `.03 Pipeline-Grundgerüst` | Ja | Import schreibt Document + Revision + Segments korrekt |
| 4 | Minimal `.04 Revision-Zeitlogik` | Ja | `current_from`, `superseded_at`, `superseded/rejected` funktionieren |
| 5 | BPM-080.05 Schritte 3–5 | Ja | Wizard direkt gegen neues Modell fertigstellen |
| 6 | BPM-081 ImportPreviewDialog | Ja | Preview nutzt neues Persistenzmodell |
| 7 | BPM-006 ProjectDetailView | Ja | UI-Polish |
| 8 | Plan-Archiv `.05 IPlanLookupService` minimal | Optional vor V1 | Nur wenn schnell testbar |
| 9 | `.06 Stammdaten-Mapping`, `.07 Doku/ADR/Tests` | Teilweise | Doku/Tests ja; Komfort-Mapping kann nach V1 |

Ich würde also nicht alle sieben Plan-Archiv-Subtasks als V1-Sperrposten deklarieren.

### V1-Sperrumfang sauber definieren

**Vor V1 zwingend:**
* Schema v2
* Domain/Repository
* Import-Pipeline schreibt korrekt
* Revision-Zeitlogik funktioniert
* Tests für Importfälle grün
* DB-Reset-Anweisung dokumentiert

**Nicht zwingend vor V1:**
* vollständiger `IPlanLookupService` für Bautagebuch
* `plan_context_links` aktiv nutzen
* Alias-Verwaltung UI
* Stammdaten-Mapping mit Preview-Interaktion
* Foto-/Vorlagen-Vorbereitung über Minimalmodell hinaus

---

## 3. Wann würde ich zurückrudern?

Ich würde Plan-Archiv doch nach V1 schieben, wenn einer dieser Fälle eintritt:

| Risiko | Rückrudern ab wann |
| --- | --- |
| Schema-v2 erzwingt große Änderungen an Recognition/Wizard-Logik | Wenn mehr als ca. 30 % von BPM-080.05 neu gedacht werden müssen |
| Pipeline-Tests brechen breit | Wenn mehr als ca. 40 Tests betroffen sind und Ursachen nicht lokal auf Repository/DB begrenzt sind |
| Import-Journal/Undo wird instabil | Sofortiger Stop — das ist V1-Kern |
| Dateiverschiebung und DB-Commit sind nicht mehr konsistent | Sofortiger Stop |
| `.01–.04` dauern länger als ca. 8–10 PT | Dann ist es kein Foundation Slice mehr |
| lokale ungepushte Commits enthalten bereits viel 080.05-Arbeit auf altem Modell | Dann erst lokalen Stand diffen und Wegwerfanteil bewerten |

Der wichtigste Stop-Punkt ist nicht die Anzahl betroffener Tabellen. Der wichtigste Stop-Punkt ist: **Wird der Import unsicher?** Wenn Journal/Undo/Dateioperationen wackeln, darf das nicht in den V1-Endsprint hinein.

---

## 4. Tests-Risiko und Aufwand

Die bisher genannten 6–8 PT reichen nur, wenn die Änderung sauber gekapselt ist und die Tests überwiegend auf Repository-/Pipeline-Erwartungen angepasst werden.

Für Vor-V1 würde ich konservativer planen:

| Block | Aufwand |
| --- | ---: |
| Schema + Repository + Models | 2–3 PT |
| Pipeline-Anpassung | 2 PT |
| Revision-Zeitlogik | 1 PT |
| Test-Refactor 10–40 Tests | 1,5–2 PT |
| Doku/ADR/Reset-Hinweis | 0,5–1 PT |
| Puffer für lokale ungepushte Abweichungen | 1 PT |

**Realistisch: 8–10 PT.**

Wenn alles gut liegt, bleibt ihr bei 6–8 PT. Für Roadmap-Kommunikation würde ich Herbert aber **8–10 PT** nennen. Nicht weil das Schema komplizierter ist, sondern weil ein Vor-V1-Refactor im Endsprint immer Integrationskosten hat.

---

## 5. Wizard-Doppel-Refactor

BPM-080.05 sollte **nicht voll weitergebaut werden**, solange `.01–.03` nicht steht.

Sonst baut ihr Wizard-Schritt 3–5 gegen ein altes mentales Modell und müsst anschließend:
* ViewModels anpassen
* Preview-Datenmodelle ändern
* Validierung neu verdrahten
* Import-Command neu anbinden
* Tests doppelt anfassen

Das ist genau die Wegwerfware, die Herbert vermeiden will.

### Empfehlung

BPM-080.05 kurzfristig aufteilen:

| Teil | Weiterbauen? |
| --- | --- |
| reine UI-Struktur / Layout / Texte | Ja, wenn keine Persistenzbindung |
| Segmentauswahl / IndexSource-Felder | Ja, wenn DTO-neutral |
| Speichern in Profile/DB/Importmodell | Nein, pausieren |
| Preview-/Execution-Anbindung | Nein, warten bis `.01–.03` steht |

Der Wizard muss nicht komplett stillstehen, aber alle persistenznahen Teile sollten warten.

---

## 6. Meine freie Entscheidung

Ich würde Herberts Entscheidung **mittragen**, aber mit einem klaren Warnschild:

> Vor V1 ja — aber nur der Plan-Archiv-Kern, nicht das ganze Plan-Kontext-Ökosystem.

Ich würde ihn nicht mehr aktiv überzeugen, das nach V1 zu schieben. Seine Argumente sind in dieser Frühphase stark:

* kein Produktivdaten-Lock-in
* keine Migration nötig
* V1 noch nicht released
* ein sauberer Persistenzschnitt verhindert Doppel-Refactor
* Wizard kann direkt gegen das richtige Modell gebaut werden

Aber ich würde ihn davon abhalten, daraus ein „Bautagebuch-ready"-Ziel zu machen. Vor V1 muss das Importfundament stimmen, nicht die spätere Modulwelt vollständig vorbereitet sein.

---

## Konkrete Empfehlung als Entscheidungsregel

**Plan-Archiv vor V1 nur als Minimal-Sperrpaket:**

```text
V1-blockierend:
- Schema v2
- Domain/Repository
- Import schreibt plan_documents + plan_revisions + plan_document_segments
- current/superseded/rejected + current_from/superseded_at
- vorhandene Import-/Undo-Invarianten bleiben grün
- DB-Reset statt Migration dokumentiert

Nicht V1-blockierend:
- vollständiger PlanLookupService
- plan_context_links aktiv nutzen
- Alias-/Mapping-Komfort
- Bautagebuch-/Foto-/Vorlagen-Integration
```

Damit ist die Verzögerung eher 1–2 Wochen als 6 Wochen. Ohne diese Begrenzung wird aus dem V1-Endsprint ein Architektur-Release.

---

## ✅ Einigkeit

* Herberts Entscheidung „vor V1" ist architektonisch vertretbar.
* Der Frühphasen-Reset macht den Zeitpunkt günstiger als nach V1.
* BPM-080.05 darf nicht gegen das alte Persistenzmodell fertiggebaut werden.
* Der Scope muss auf den Plan-Archiv-Kern begrenzt werden.
* Test-Refactor muss explizit eingeplant werden.
* Kein Migrations-/Legacy-Code.

## ⚠️ Widerspruch

* Ich würde **nicht alle 7 Subtasks** als V1-Sperrposten behandeln.
* Ich würde BPM-080.05 **persistenznah pausieren**, statt parallel voll weiterzubauen.
* Ich würde die Aufwandsschätzung für Vor-V1 auf **8–10 PT** erhöhen.
* Ich würde `plan_context_links`, Alias-Komfort und vollständigen Lookup-Service nicht zum V1-Gate machen.
* Ich würde sofort zurückrudern, wenn Journal/Undo/Dateioperationen durch den Refactor instabil werden.

## ❓ Rückfragen

1. Wie viel von BPM-080.05 ist lokal bereits umgesetzt und wie stark hängt es am alten `plan_revisions`-Modell?
2. Gibt es lokale Tests, die auf GitHub noch nicht sichtbar sind und bereits das alte Schema in 080.05/081 verankern?
3. Soll V1 fachlich „PlanManager Import stabil" heißen oder bereits „PlanManager als Modulplattform vorbereitet"?
4. Ist Herbert bereit, für V1 bewusst auf vollständigen `IPlanLookupService` und Alias-Komfort zu verzichten, wenn Schema/Pipeline dafür sauber werden?
