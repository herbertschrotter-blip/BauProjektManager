# Herberts Entscheidungen nach Phase 2 Stufe A (Runde 2)

**Kontext:** Sanity-Check der „vor V1"-Entscheidung aus Runde 1. ChatGPT trägt die Entscheidung mit, korrigiert aber Roadmap-Skizze in 2 wichtigen Punkten (Foundation Slice + Wizard pausieren).

## Entscheidung 1 — V1-Scope

**Frage:** Foundation Slice oder voller Plan-Archiv-Build vor V1?

**Entscheidung:** ✅ **Foundation Slice (ChatGPT-Empfehlung)**

**Konsequenz:**
- V1-Sperrposten: `.01 Schema`, `.02 Domain/Repository`, `.03 Pipeline-Grundgerüst`, `.04 Revision-Zeitlogik`
- Nicht V1-Sperrposten: `.05 IPlanLookupService voll`, `.06 Stammdaten-Mapping mit Preview-UI`, vollständiger `.07`
- Schema-Bruch nur einmal, sauberer Schnitt vor V1
- Verzögerung 1–2 Wochen statt 4–6

## Entscheidung 2 — BPM-080.05 Wizard-Status

**Frage:** Wizard Schritte 3–5 wie weitermachen während Schema entsteht?

**Entscheidung:** ✅ **Komplett pausieren bis Schema fertig** (konservativer als ChatGPT empfohlen hatte)

**Konsequenz:**
- BPM-080.05 ruht komplett bis Plan-Archiv `.01–.04` durch
- Auch UI-Layer/Texte werden nicht parallel weitergebaut
- Klare Abhängigkeitskette: erst Schema-Foundation, dann Wizard
- Maximaler Wegwerfware-Schutz
- Konzentration auf Plan-Archiv-Sprint

**Klärung erforderlich (technisch — ich klär das aus dem Repo):**
- Wie weit ist 080.05 lokal? (Handover: Schritte 1+2 fertig, 3–5 offen)
- Welche 080.05-Stände sind in den 10 ungepushten Commits?

## Entscheidung 3 — V1-Definition

**Frage:** Was heißt „V1-ready"?

**Entscheidung:** ✅ **Import stabil + Modulplattform vorbereitet**

**Interpretation und Konflikt-Auflösung:**

Diese Wahl steht in leichter Spannung zu Entscheidung 1 (Foundation Slice nur `.01–.04`). „Modulplattform vorbereitet" könnte heißen, dass auch der `IPlanLookupService` (Subtask `.05`) zumindest als **Interface-Definition** + Stub-Repository da sein muss, damit Module später ohne Re-Architektur darauf bauen können.

**Pragmatische Auflösung:**
- V1 enthält **`.01–.04` voll** (Schema + Pipeline funktional)
- Plus **Interface-Stub für `IPlanLookupService`** (nur Methodensignaturen, keine Implementation) als `.05a`
- Plus **ADR-058** dokumentiert die Modul-API für künftige Module
- `.05 IPlanLookupService voll` (mit Query-Logik) und `.06 Stammdaten-Mapping` kommen post-V1

→ Das ist Q3 Option 2 wörtlich genommen: „Modulplattform vorbereitet" = Interface da, Implementation kommt mit dem ersten Modul (BPM-056).

## Endgültiger V1-Sperrumfang

```text
V1-blockierend (zwingend vor V1):
  .01 Schema v2 (plan_documents, plan_revisions umgebaut, plan_document_segments,
      plan_revision_events, plan_context_links definiert, building_part_aliases)
  .02 Domain Models + Repository
  .03 Pipeline-Grundgerüst (Import schreibt Document + Revision + Segments)
  .04 Revision-Zeitlogik (current_from, superseded_at, current/superseded/rejected)
  .05a IPlanLookupService Interface-Stub (keine Impl, nur Vertrag)
  Tests für Importfälle grün
  DB-Reset-Anweisung dokumentiert
  Doku: DB-SCHEMA.md Kap. 6, PlanManager.md Pipeline-Update, ADR-058

V1-blockierend nach Schema:
  BPM-080.05 Schritte 3-5 (Wizard gegen neues Modell)
  BPM-081 ImportPreviewDialog (gegen neues Persistenzmodell)
  BPM-006 ProjectDetailView (UI-Polish)

NICHT V1-blockierend (post-V1):
  .05 IPlanLookupService voll (Query-Implementation)
  .06 Stammdaten-Mapping mit Preview-UI
  .07 vollständige Doku (GLOSSAR, BACKLOG-Refactor, Architektur.md Tiefen-Update)
  plan_context_links aktiv nutzen
  Alias-Verwaltung UI
  Bautagebuch-/Foto-/Vorlagen-Integration
```

## Aufwand (akzeptiert)

**8–10 PT für Foundation Slice + Interface-Stub** (statt 6–8 PT ohne Stub).

Aufschlüsselung:
- Schema + Repository + Models: 2–3 PT
- Pipeline-Anpassung: 2 PT
- Revision-Zeitlogik: 1 PT
- Test-Refactor 10–40 Tests: 1,5–2 PT
- IPlanLookupService Interface-Stub: 0,5 PT
- Doku/ADR/Reset-Hinweis: 0,5–1 PT
- Puffer für lokale ungepushte Abweichungen: 1 PT

**Total: 8,5–10,5 PT.**

## Stop-Punkte (ChatGPT übernommen)

| Trigger | Aktion |
|---|---|
| Schema-v2 erfordert >30% Re-Design von BPM-080.05 | Stopp, Plan-Archiv nach V1 |
| >40 Tests gebrochen + Ursachen nicht lokal | Stopp, Plan-Archiv nach V1 |
| Import-Journal/Undo wackelt | **Sofort** Stopp |
| Dateiverschiebung + DB-Commit inkonsistent | **Sofort** Stopp |
| `.01–.04` dauern >10 PT | Stopp, Foundation Slice gescheitert |

## Empfohlene nächste Schritte

1. ADR-058 „Plan-Archiv-Persistenz" als Stub anlegen
2. DB-SCHEMA.md Kap. 6 vorbereiten (Schema-v2-Skizze)
3. ClickUp-Issue **BPM-NNN Plan-Archiv-Persistenz v2** anlegen mit Subtasks:
   - `.01 Schema v2 neu erzeugen` (V1-blockierend)
   - `.02 Domain Models + Repository` (V1-blockierend)
   - `.03 Pipeline-Grundgerüst` (V1-blockierend)
   - `.04 Revision-Zeitlogik` (V1-blockierend)
   - `.05a IPlanLookupService Interface-Stub` (V1-blockierend)
   - `.05 IPlanLookupService Implementation` (post-V1, parallel zu BPM-056)
   - `.06 Stammdaten-Mapping mit Preview-UI` (post-V1)
   - `.07 Vollständige Doku/ADR-Erweiterungen/Tests` (post-V1)
4. BPM-080.05/081/006 als „blockiert durch .01–.04" markieren
5. Memory + Handover-Stand aktualisieren

## Status

**Sign-off bereit. Keine Runde 3 nötig.** Konsens über Foundation Slice, Wizard-Pause, Interface-Stub.
