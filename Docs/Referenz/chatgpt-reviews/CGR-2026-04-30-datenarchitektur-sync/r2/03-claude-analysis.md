# Claude-Analyse — CGR-2026-04-30-datenarchitektur-sync — Runde 2

**Stand:** 2026-04-30 nach ChatGPT-Antwort r2

---

## Bewertung der Antwort

ChatGPT liefert in Runde 2 eine **scharfe, präzisierende Antwort mit drei wichtigen Korrekturen** an meinem r2-Prompt — und einer **zentralen Architektur-Klärung** zum Hybrid-Hosting.

### Was ChatGPT korrigiert (sachlich richtig)

**1. Microsoft.Datasync → CommunityToolkit.Datasync**
Mein Initialprompt sprach von "Microsoft Datasync Framework". ChatGPT macht klar: das alte `Microsoft.Datasync.*` Repository ist **archiviert/abgelöst**, NuGet-Versionen bei 6.1.0 aus 2024 stehen geblieben. Aktive Linie 2026 ist **CommunityToolkit.Datasync** (10.0.0 aus Feb 2026, .NET 10 / ASP.NET Core 10, WPF + PostgreSQL als getestete Plattformen). Wichtige Fakt-Aktualisierung — sollte in jeden Folge-Architektur-Doc.

**2. Hybrid-Hosting — nicht zwei Backends, sondern Protokoll-Hybrid**
Meine r2-Frage "kann man beides haben?" hatte ChatGPT eindeutig beantwortet: **Nicht zwei vollwertige Backendpfade gleichzeitig produktiv.** Stattdessen:
- **Ein** produktives Backend (Empfehlung: ASP.NET + PostgreSQL)
- **Aber** Client-Architektur von Tag 1 mit austauschbarem Adapter (`IBpmSyncClient`)
- DI-Switch in Infrastructure, nicht ViewModels
- Backend-spezifische Anteile (Auth, Realtime, RLS) gekapselt, generischer Pull/Push-Vertrag stabil

Das ist die saubere Mitte zwischen "single-vendor lock-in" und "two-backends-overkill". Ich stimme zu — das ist die richtige Antwort.

**3. Spike-Reihenfolge: Supabase nach hinten, CommunityToolkit.Datasync zuerst**
Mein Vorschlag hatte Supabase als Spike 1. ChatGPT korrigiert: Wenn das strategische Ziel self-hosted ist, macht es keinen Sinn Supabase zuerst zu spiken — der Spike soll die strategische Frage beantworten ("Gibt es einen .NET-nativen Offline-Sync-Pfad, der BPM Eigenbau spart?"). Daher:
- Spike 0: ProjectDatabase syncfähig
- Spike 1: CommunityToolkit.Datasync
- Spike 2: Eigener Minimal-API Row-Sync (parallel als Benchmark, nicht als Notausgang nach Wochen)
- Spike 3: Supabase nur als Vergleichsspiegel

ChatGPTs Prognose: A oder B (Toolkit oder eigener Row-Sync), nicht C (Supabase produktiv).

### Profile-Strategie: B2 jetzt, B3 später

Klarer Ratschlag: **Profile in DB-Tabelle (`recognition_profiles`)** als nächster Sync-Schritt. Die globale Bibliothek (B3 Library + Instance Pattern) ist **fachlich richtig**, aber Komfort-Etage — kommt nach erfolgreichem B2.

Konsequenz für ADR-046: Pfad bleibt, Rolle ändert sich:
- Heute: `.bpm/profiles/*.json` führend
- Im Servermodus: DB führend, JSON-Pfad ist abgeleiteter Export/Backup
- ADR-046 sollte präzisiert (nicht ersetzt) werden — neuer ADR könnte `.bpm/profiles/` als "Export im Servermodus" definieren

### Konflikt-Strategie: Server-gewinnt OK für Phase 1

Pragmatisch und richtig:
- Solo-Multi-Device "abwechselnd, nicht parallel" → Konflikt-Häufigkeit niedrig
- Keine Merge-UI in Phase 1, nur Sync-Status "Lokale Änderung wurde durch Serverstand ersetzt"
- Server-Version als Authority, nicht client-`last_modified_at`
- Konflikt-UI später nur für Module mit hoher Konfliktrate (Bautagebuch?)

### DSGVO: minimal verdrahten, jetzt

Wichtige Klarstellung: **Auch Stammdaten haben personenbezogene Felder** (clients.contact_person, projects.address, project_participants.email/phone, project_links.url etc). Phase-1-Whitelist:
- `DataClassification` Enum jetzt einführen
- Pro Sync-DTO statische Klassifikation A/B/C
- Sync-Client darf nur whitelisted DTOs pushen/pullen
- Klasse C bleibt gesperrt bis Server-Rollen/Audit existieren
- Aber: Kein Consent-UI, kein Masking, kein Datenschutzbericht-Framework — das ist Overkill für Phase 1

### Vier konkrete Rückfragen — alle mit Empfehlung

ChatGPT stellt vier Detailfragen, gibt aber für jede schon eine klare Empfehlung mit. Das macht es für mich/User einfach, schnell zu entscheiden:

| # | Frage | ChatGPT-Empfehlung |
|---|---|---|
| 1 | Sync-State als separate Tabelle oder Pending-Flags in Fachzeilen? | Separate `sync_state`/`sync_pending_changes` Tabelle |
| 2 | Erster Sync mit Profile dabei oder nur Stammdaten? | Erst Stammdaten, dann Profile als zweiter Schritt |
| 3 | `project.json` weiter automatisch schreiben? | Ja, aber nur Export/Snapshot, nie Import-Auto-Sync |
| 4 | Sichtbarer Sync-Status statt Merge-Dialog OK? | Ja, verpflichtend so entscheiden für Phase 1 |

## Wo ich nuancieren würde

Eigentlich nirgendwo — ich stimme ChatGPT in r2 zu fast 100% zu. Nuancen:

1. **CommunityToolkit.Datasync ist 2026 nicht so etabliert wie EF Core**: ChatGPT erwähnt das selbst ("Spike ja, Architekturanker nein"). Daher ist es wichtig, dass der BPM-Sync-Contract `IBpmSyncClient` **BPM-eigen** bleibt — wenn die Library später bricht oder unmaintained wird, ist nur der Adapter betroffen, nicht das Domänen-Modell. Das ist genau die Empfehlung.

2. **Spike 2 (Eigener Minimal-API) parallel zu Spike 1**: Sehr klug. Reduziert Risiko dass der Toolkit-Spike alleine entscheidet. Wenn Spike 1 wackelig läuft, hat man Spike 2 zur Hand. Aufwand zusammen 3-5 Tage statt 1-2 — aber bessere Entscheidungsgrundlage.

3. **Supabase als Spike 3 nicht streichen**: ChatGPT lässt Supabase drin als 1-Tage-Spike für Auth/RLS/Realtime-Erkenntnis. Das ist sinnvoll, weil RLS-Konzepte später auch in ASP.NET-Identity einfließen können (RLS in Postgres ist nicht Supabase-eigen).

## Architektur-Konstellation — fertig zur Entscheidung

ChatGPTs finale Empfehlung lese ich als Vorschlag den der User abnicken oder modifizieren kann:

```
1. BPM definiert eigenes Sync-Protokoll (IBpmSyncClient)
2. Client-Architektur mit austauschbaren Backend-Adaptern
3. Produktives Backend: ASP.NET + PostgreSQL
4. Spike-Reihenfolge:
   - Spike 0 (Pflicht): ProjectDatabase syncfähig
   - Spike 1: CommunityToolkit.Datasync
   - Spike 2 (parallel): Eigener Minimal-API Row-Sync
   - Spike 3 (kurz): Supabase als Vergleichsspiegel
5. Profile mittelfristig in DB als recognition_profiles
6. Server-gewinnt Konflikt-Strategie + Sync-Status-Anzeige (keine Merge-UI)
7. DSGVO minimal: DataClassification pro DTO, Whitelist
8. .bpm/profiles/*.json bleibt Export/Backup, nicht SoT im Servermodus
```

## Empfehlung an User

Die Architektur-Richtung ist **fertig diskutiert**. Der User sollte jetzt entscheiden:

1. **Architektur-Konstellation übernehmen** (ChatGPT-Vorschlag in 8 Punkten oben)?
2. Antworten auf die **4 ChatGPT-Rückfragen** geben (alle mit Empfehlung)
3. **Wie weiter:** Spike 0 (DB syncfähig) als Tracker-Task anlegen + Architektur in ADR festschreiben? Oder Runde 3 mit Detail-Fragen?

Mein Vorschlag: **Runde 3 ist nicht nötig.** Die Empfehlung ist klar, die Detail-Fragen haben Empfehlungen mit. Alles weitere (Spike-Ergebnisse, konkrete Library-Wahl, RBAC-Modell) wird durch tatsächliche Spike-Arbeit beantwortet, nicht durch weiteren Cross-LLM-Review.

Konkrete Tracker-Tasks die nach Approval angelegt werden sollten:
- **Spike 0:** `BPM-XXX | INF | ProjectDatabase syncfähig (Soft Delete + gezielte Upserts)` — Tag v1-nice oder pre-v1-cleanup, Aufwand L (2-3 Tage)
- **ADR-053 schreiben:** "Sync-Strategie — IBpmSyncClient + ASP.NET/PostgreSQL + austauschbare Adapter" als Doc-Pflege-Task
- **DatenarchitekturSync.md** auf "historisch / superseded by ADR-053" stellen (laut Doc-Stand teilweise schon superseded)
- **BPM-018 Backup vor Import** im PlanManager-Backlog wird durch Sync-Konzept ggf. obsolet — neu bewerten
- **Spike 1 + 2 + 3** als spätere Tasks anlegen, post-v1, nach ProjectDatabase-Refactor

Plus einen separaten Bewusstmachungs-Task für den User: **die existierenden Sync-Spalten v2.1 sind nur die halbe Miete** — der Code muss noch sync-fähig schreiben (das ist Spike 0).
