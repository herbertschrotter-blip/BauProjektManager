# Claude-Analyse Runde 2 — Sanity-Check „vor V1"

## Kernergebnis

ChatGPT trägt Herberts „vor V1"-Entscheidung **mit klarem Warnschild** mit. Das ist die saubere Antwort: Architektonisch vertretbar, aber nur als **Foundation Slice**, nicht als kompletter Plan-Archiv-Build.

ChatGPTs Antwort korrigiert meine Roadmap-Skizze in zwei wichtigen Punkten:
1. **Nicht alle 7 Subtasks** sind V1-blockierend — nur `.01–.04`
2. **BPM-080.05 muss persistenznah pausiert werden** — nicht volle Parallelisierung

## Volle Einigkeit mit ChatGPT

- „Vor V1" architektonisch vertretbar (Frühphasen-Reset macht Zeitpunkt günstiger)
- BPM-080.05 darf nicht gegen altes Persistenzmodell fertiggebaut werden
- Scope muss begrenzt werden — kein „Bautagebuch-ready"-Ziel für V1
- Aufwand realistisch 8–10 PT (nicht 6–8 PT)
- Stop-Punkt: wenn Journal/Undo/Dateioperationen wackeln → sofort zurück

## ChatGPT-Korrekturen, die ich übernehme

### Korrektur 1: Foundation Slice statt voller Build

**Mein Erstvorschlag (Runde 2):** alle 7 Subtasks vor V1, parallel mit 080.05/081/006

**ChatGPTs Korrektur:**

| Vor V1 zwingend (V1-blockierend) | Nicht V1-blockierend |
|---|---|
| `.01 Schema v2` | `.05 IPlanLookupService` voll |
| `.02 Domain/Repository` | `.06 Stammdaten-Mapping mit Preview-UI` |
| `.03 Pipeline-Grundgerüst` | `plan_context_links` aktiv nutzen |
| `.04 Revision-Zeitlogik` | Alias-Verwaltung-UI |
| Tests für Importfälle grün | Foto-/Bautagebuch-Integration |
| DB-Reset-Anweisung dokumentiert | |
| **Doku/ADR (.07 teilweise)** | Komfort-Doku kann nach V1 |

→ Damit wird der V1-Endsprint **nicht zum Architektur-Release**.

### Korrektur 2: BPM-080.05 persistenznah pausieren

**Mein Erstvorschlag:** Wizard parallel weiterbauen mit Schema-v2-Bewusstsein

**ChatGPTs Korrektur:**

| Teil von BPM-080.05 | Weiterbauen während Schema entsteht? |
|---|---|
| UI-Struktur / Layout / Texte | ✅ Ja |
| Segmentauswahl / IndexSource-Felder | ✅ Ja, wenn DTO-neutral |
| Speichern in Profile/DB/Importmodell | ❌ Pausieren bis `.01–.03` steht |
| Preview-/Execution-Anbindung | ❌ Pausieren bis `.01–.03` steht |

→ Verhindert Wegwerfware bei ViewModels, Preview-DTOs, Validierung, Import-Command, Tests.

### Korrektur 3: Aufwand 8–10 PT statt 6–8 PT

ChatGPTs detaillierte Aufschlüsselung:
- Schema + Repository + Models: 2–3 PT
- Pipeline-Anpassung: 2 PT
- Revision-Zeitlogik: 1 PT
- Test-Refactor 10–40 Tests: 1,5–2 PT
- Doku/ADR/Reset-Hinweis: 0,5–1 PT
- Puffer für lokale ungepushte Abweichungen: 1 PT

→ **Roadmap-Kommunikation:** 8–10 PT angeben, nicht 6–8.

### Korrektur 4: Klare Stop-Punkte definieren

| Stop-Trigger | Aktion |
|---|---|
| Schema-v2 erfordert >30% Re-Design von BPM-080.05 | Stopp, Plan-Archiv nach V1 |
| >40 Tests gebrochen + Ursachen nicht lokal | Stopp, Plan-Archiv nach V1 |
| Import-Journal/Undo wackelt | **Sofort** Stopp |
| Dateiverschiebung + DB-Commit nicht mehr konsistent | **Sofort** Stopp |
| `.01–.04` dauern >10 PT | Stopp, Foundation Slice gescheitert |

## ChatGPT-Rückfragen — meine Antworten

**RF 1: Wie viel von BPM-080.05 ist lokal umgesetzt und wie stark am alten Modell?**

Aus Handover bekannt: Schritt 1+2 fertig, Schritte 3–5 offen. Schritt 1 (Beispieldatei laden) ist persistenz-neutral. Schritt 2 (Segmentauswahl) ist DTO-neutral, hängt nicht am alten `plan_revisions`-Modell. Damit ist die lokale Pause-Schicht klar: ab Schritt 3 (IndexSource speichern) abwärts geht's Richtung Persistenz.

Konsequenz: Schritte 1+2 bleiben unverändert. Schritte 3–5 warten auf `.01–.03`.

**RF 2: Lokale Tests die altes Schema verankern?**

10 ungepushte Commits enthalten möglicherweise neue Wizard-Tests. Aus dem Handover: 238/238 Tests grün nach BPM-108. Wahrscheinlich keine 080.05-Tests, die Persistenz-Verhalten testen — der Wizard ist UI/ViewModel-Layer. ImportPipeline-Tests sind das Hauptrisiko.

**RF 3: V1 = „Import stabil" oder „Modulplattform vorbereitet"?**

Das ist die strategische Hauptfrage. ChatGPTs implizite Empfehlung: **„Import stabil + Foundation für Module"**, nicht „Modulplattform fertig".

**RF 4: Herbert bereit auf vollständigen Lookup-Service zu verzichten?**

Wenn ja, ist ChatGPTs Foundation-Slice akzeptabel. Wenn nein (= IPlanLookupService muss V1-fertig sein), dann eher nach V1 schieben.

## Meine Bewertung

**ChatGPTs Foundation-Slice-Empfehlung ist sauber und realistisch.** Drei Vorteile:

1. **Verzögerung 1–2 Wochen statt 4–6 Wochen** — V1 bleibt erreichbar
2. **Klare V1-Definition:** Import sauber, Module vorbereitet (nicht fertig)
3. **Klare Stop-Bedingungen** — wenn's schiefläuft, gibt's ein Rollback-Kriterium

Ich würde Herbert raten, der Foundation-Slice-Begrenzung zu folgen.

## Bereit für Phase 2 Stufe B (Folgeprompt R3) oder Sign-off?

**Ich sehe keinen Bedarf für Runde 3.** ChatGPT hat klar geantwortet, alle Risiken benannt, einen sauberen Scope-Schnitt vorgeschlagen. Ein Sign-off mit Foundation-Slice + Stop-Punkte wäre angemessen.

Drei Entscheidungen brauche ich von Herbert:

1. **Foundation Slice akzeptieren** (`.01–.04` vor V1) oder **alle 7 Subtasks vor V1** durchziehen?
2. **BPM-080.05 persistenznah pausieren** oder voll parallel weiterbauen (Wegwerfware-Risiko)?
3. **V1-Definition:** „Import stabil + Foundation" oder „Modulplattform vorbereitet"?
