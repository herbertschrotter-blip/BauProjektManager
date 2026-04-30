# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 3

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`** — IMMER diesen Branch verwenden, NICHT `main`!
- Bei JEDEM Dateizugriff den Branch `feature/bugfixing` angeben.

## Format-Erinnerung

- Schreibe deine GESAMTE Antwort in Canvas, **Titel: "Review Runde 3 — Datenarchitektur & Sync"**
- Direkt zu Claude sprechen, nicht zum User
- Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- Klar werden, nicht "es kommt darauf an"
- Kompakt, konkrete Zahlen/Modelle/Library-Versionen wo verfügbar
- Pseudocode statt Voll-C# wo möglich

## Stand nach Runde 2

Wir sind uns einig auf der **Architektur-Konstellation**:

1. BPM definiert eigenes Sync-Protokoll (`IBpmSyncClient`)
2. Client-Architektur mit austauschbaren Backend-Adaptern (DI-Switch in Infrastructure, nicht ViewModels)
3. Produktives Backend: ASP.NET + PostgreSQL
4. Spike-Reihenfolge: Spike 0 → 1 → 2 → 3
5. Profile mittelfristig in DB (`recognition_profiles`)
6. Server-gewinnt + Sync-Status-Anzeige (keine Merge-UI)
7. DSGVO: `DataClassification` pro DTO + Whitelist
8. `.bpm/profiles/*.json` bleibt Export/Backup im Servermodus

User hat zusätzlich die 4 R2-Detailfragen mit ChatGPT-Empfehlung übernommen:
- Separate `sync_state`/`sync_pending_changes` Tabelle
- Erst Stammdaten, dann Profile als zweiter Schritt
- `project.json` weiter als Export/Snapshot, nie Import-Auto-Sync
- Sync-Status-Anzeige statt Merge-Dialog

**Architektur-Richtung steht. Runde 3 vertieft 4 konkrete Detail-Themen, bevor wir ADR-053 schreiben.**

## Aufgabe für Runde 3

Vier Blöcke. Klar, mit konkreten Vorschlägen. Keine "es-kommt-darauf-an"-Antworten.

---

### Block A — Server-Hosting + Deployment + Branchen-Praxis am Bau

User Herbert Schrotter ist Solo-Bauleiter/Polier-orientierter Software-Entwickler. Phase 3 wird irgendwann Multi-User mit echten Bau-Mitarbeitern (Bauleiter, Polier, Disponent, Lohnbüro). Frage:

**A1 — Branchen-Praxis am Bau 2026:**
Was nutzen kleine bis mittlere Baufirmen (5-50 Mitarbeiter, Österreich/Deutschland) tatsächlich für Bauverwaltungs-Apps? Konkret:
- Hosting im Firmen-Server-Raum vs. eigener kleiner VPS vs. Cloud (Microsoft 365 Backend) vs. SaaS-Anbieter (Capmo, Kontextwork, etc.)?
- Welche Patterns sind bewährt — wo geht Daten-Hosting in der Bau-Branche typischerweise hin?
- DSGVO-Realität: Was machen kleine Baufirmen praktisch (auch wenn formal nicht 100% korrekt)?

**A2 — Konkrete Optionen für Herbert (Empfehlung mit Pro/Con):**
- **Eigener Firmenserver** (z.B. Windows Server / Linux im Büro): Pro/Con, Wartungsaufwand, was wird benötigt (Hardware, USV, Backup, Internet-Anbindung)?
- **Heim-Server / NAS** (Synology, QNAP, oder kleiner Linux-PC daheim): Pro/Con, DSGVO-Tauglichkeit für Geschäftsdaten?
- **Lokaler Hauptrechner** (BPM läuft auf Herberts Desktop, andere Geräte syncen dorthin via lokales Netz): Pro/Con, was wenn Hauptrechner aus ist?
- **VPS in EU** (Hetzner Cloud 5-10€/Mo, Strato, Netcup): Pro/Con, DSGVO-Standardvertrag, Wartung?
- **Hosted EU-BaaS** (Supabase EU, oder ähnlich): Pro/Con bekannt aus r1/r2

**A3 — Konkrete Empfehlung:**
Welche **Hosting-Variante** für Herbert ist die strategisch beste für die Roadmap "Solo heute → Multi-User mittelfristig"?
- Für jetzt (Solo, 2-3 PCs)
- Für später (Phase 3, 5-10 User)
- Was wird konkret benötigt (Hardware-Liste, Software-Stack, Setup-Schritte)?
- Realistische Kosten pro Monat?

---

### Block B — Konkreter Spike-Plan (Spike 0 + Spike 1)

In r2 hatten wir die Spike-Reihenfolge festgelegt. Jetzt bitte **konkrete Pläne** für die ersten beiden:

**B1 — Spike 0: ProjectDatabase syncfähig machen**
Konkret was muss gemacht werden? Liste in Code-Schritten:
- Welche Methoden in `ProjectDatabase.cs` sind betroffen?
- Welche Methoden brauchen Soft-Delete-Refactor?
- Welche Methoden brauchen Upsert-Diff-Logik (statt Replace-All-Listen)?
- Welche neuen Tabellen-Spalten oder Indexe?
- Welche Tests kann man heute schon schreiben (auch ohne Test-Projekt — manuelle Test-Szenarien)?
- Was sind die Erfolgskriterien (welche Behaviors sind danach zugesichert)?
- Welche Stolperfallen?

**B2 — Spike 1: CommunityToolkit.Datasync gegen ASP.NET + PostgreSQL**
Konkret detaillierter Plan:
- Welches NuGet-Paket genau (Library + Version, was im 2026er Stand)?
- Welche **eine** Tabelle als Spike-Kandidat (Empfehlung mit Begründung)?
- Welche Auth-Stub (kein echtes RBAC im Spike)?
- Welcher Konflikt-Test (z.B. zwei lokale SQLite-Stores gegen einen Server)?
- Welche Erfolgskriterien (Datasync-Toolkit übernehmen ja/nein)?
- Welche Diagnose-Tools (Logs, DB-Inspector, Postman/HTTP-Calls)?
- Welche Code-Beispiele oder Offiziellen Tutorials zeigen als Startpunkt?
- Wie viele PT realistisch für lauffähigen Mini-Sync?

**B3 — Spike-2 + Spike-3 nur kurz:**
- Welche zusätzlichen Schritte für eigenen Minimal-API Row-Sync (Spike 2) — was ist anders als Spike 1?
- Welche Mini-Schritte für Supabase-Spike 3 (1-Tages-Spike) — was prüfen?

---

### Block C — Auth-Provider-Wahl + RBAC-Konzept

**C1 — Auth-Provider-Vergleich:**
- **Supabase Auth** (JWT, Magic Link, OAuth, EU-Hosted) — Pro/Con für BPM
- **ASP.NET Core Identity** (eigenes User-Management, Cookies/JWT) — Pro/Con
- **Auth0** (hosted, polished, kostet) — Pro/Con
- **Microsoft Entra ID** (Azure AD, für Firmen-Login) — Pro/Con für Bau-Branche
- **Eigenbau** (Username + Password + JWT, Minimal-Auth) — Pro/Con

Empfehlung mit Begründung für Herberts Setup.

**C2 — RBAC-Konzept:**
Konkret welche Rollen brauchen wir am Bau? Beispielhaft:
- `admin` (Herbert, alles)
- `bauleiter` (Projekte, Pläne, Bautagebuch — keine Lohndaten)
- `polier` (Projekte zugewiesen, eingeschränkt)
- `disponent` (Lohn, Material — keine Pläne)
- `lohnbuero` (Lohn, Personal — sonst lesend)
- `gast` (read-only auf Projekt zugewiesen)

Wie auf Postgres RLS abbilden? Wie auf ASP.NET Authorization Policies abbilden? Welche Roll-out-Reihenfolge (welche Rolle zuerst implementieren)?

**C3 — Login-Pflicht ab wann?**
- Ab Phase 1 schon Login nötig (auch wenn Herbert solo)? Oder kann Phase 1 Local-User sein und Server-Modus erst Login erzwingen?
- Wie sauber lässt sich später Login nachrüsten?

---

### Block D — ADR-053-Struktur + Sync-Tabelle-Spalten

**D1 — ADR-053 was rein, was offen?**
Konkret welche Aussagen kommen verbindlich in ADR-053 vs. welche bleiben offen für Spike-Ergebnisse?

Vorschlag was REIN soll:
- Konstellation aus 8 Punkten (r2)
- Spike-Reihenfolge
- Konflikt-Strategie "Server gewinnt"
- DSGVO-Whitelist-Prinzip
- Profile in DB als Ziel
- Lokale `sync_state`-Tabelle separat

Vorschlag was OFFEN bleibt:
- Konkrete Library-Wahl (CommunityToolkit.Datasync vs eigener Minimal-API) — entscheidet Spike-Ergebnis
- Konkretes Hosting (entscheidet Block A oben)
- Konkretes RBAC-Modell (entscheidet Phase-3-Bedarf)

Stimmt das oder sollte mehr/weniger fix sein?

**D2 — Was passiert mit DatenarchitekturSync.md?**
Das Doc ist heute "secondary, teilweise superseded". Nach ADR-053:
- Komplett auf "historisch" stellen?
- Teile übernehmen (4-Klassen-Modell ist gültig, Outbox/Inbox-Detail nicht)?
- Soll ADR-053 die DatenarchitekturSync.md offiziell supersede?

**D3 — Sync-Tabelle-Struktur konkret:**

ChatGPT empfahl in r2 separate Tabellen `sync_state` und/oder `sync_pending_changes`. Konkret welche Spalten?

Vorschlag zur Diskussion:

```sql
-- Pro Entitäts-Tabelle: ein Eintrag, wenn pending oder conflict
sync_state (
  entity_table TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  state TEXT NOT NULL,           -- 'pending' | 'synced' | 'conflict' | 'rejected'
  base_server_version INTEGER,    -- wenn pending: Server-Version, gegen die geändert wurde
  last_local_change_at TEXT,      -- wann lokal geändert
  last_sync_attempt_at TEXT,
  retry_count INTEGER DEFAULT 0,
  error_message TEXT,
  PRIMARY KEY (entity_table, entity_id)
);

-- Audit-Log: was wurde gesynct, wann, von welchem Gerät
sync_history (
  id TEXT PRIMARY KEY,
  entity_table TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,        -- 'pull' | 'push' | 'conflict-server-won'
  server_version INTEGER,
  performed_at TEXT NOT NULL,
  device_id TEXT NOT NULL,
  user_id TEXT NOT NULL
);

-- Pro Tabelle: höchste bekannte Server-Version (für Pull-Checkpoint)
sync_checkpoints (
  entity_table TEXT PRIMARY KEY,
  highest_server_version INTEGER NOT NULL DEFAULT 0,
  last_pull_at TEXT
);
```

Bewerte:
- Stimmt das Modell? Was fehlt, was ist zu viel?
- Wie viele dieser Tabellen sind in Spike 0 schon nötig vs erst in Spike 1?
- Wie wird `sync_state.state = conflict` mit dem User kommuniziert (Sync-Status-Anzeige in UI)?

## Bitte als nächstes

Beantworte A-D. Konkret. Mit Library-Versionen, Hardware-Empfehlungen, Code-Schritten. Wenn du eine Branchen-Praxis-Aussage machst, gerne mit Beleg (Anbieter-Namen, übliche Kostenrahmen).

Empfehlung am Ende — eine **konkrete Hosting-Wahl** für Herbert, ein **konkreter Spike-0+1-Plan** mit PT-Aufwand, eine **konkrete Auth-Empfehlung**, eine **konkrete ADR-053-Gliederung**. Damit der User nach dieser Runde direkt loslegen oder mit klarem Stand pausieren kann.

Footer:
- ✅ Einigkeit
- ⚠️ Widerspruch
- ❓ Rückfragen
