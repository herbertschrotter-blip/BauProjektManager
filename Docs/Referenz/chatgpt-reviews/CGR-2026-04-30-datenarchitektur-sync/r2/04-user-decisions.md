# User-Entscheidungen — CGR-2026-04-30-datenarchitektur-sync — Runde 2

**Stand:** 2026-04-30 nach Stufe-A r2

---

## Antworten zu den Stufe-A-Fragen

### 1. Architektur-Konstellation aus r2 übernehmen?

**User:** *"Erst Runde 3 mit Detail-Diskussion"*

**Interpretation:** Die 8-Punkte-Konstellation ist tendenziell akzeptabel, aber bevor sie verbindlich übernommen und in einem ADR festgeschrieben wird, will der User noch eine vertiefende Detail-Diskussion mit ChatGPT. Mögliche Detail-Themen für r3:
- Konkreter Spike-Plan (welche Tabelle, welche Auth-Stub, welche Konflikte testen)
- Auth-Provider-Wahl (Supabase Auth vs. ASP.NET Identity vs. Auth0)
- Server-Hosting-Provider-Wahl (Hetzner vs. OVH vs. Heim-NAS)
- RBAC-Konzept (Rollen, RLS-Abbildung)
- ADR-053-Struktur (was rein, was als Spike-Ergebnis offen)
- Sync-Tabelle Struktur konkret (sync_state vs. sync_pending_changes — Spalten-Definition)
- Login-Pflicht ab Phase 1 oder erst Phase 2

→ Folge: r3-Themen mit User klären, dann Folgeprompt bauen.

### 2. ChatGPT-R2-Detailfragen

**User:** *"Alle 4 Empfehlungen übernehmen"*

Klare Entscheidungen für die 4 ChatGPT-Rückfragen aus r2:

| # | ChatGPT-Frage | User-Antwort = ChatGPT-Empfehlung |
|---|---|---|
| 1 | Sync-State separate Tabelle oder Pending-Flags in Fachzeilen? | **Separate `sync_state`/`sync_pending_changes` Tabelle** |
| 2 | Erster Sync mit Profile dabei oder nur Stammdaten? | **Erst Stammdaten, dann Profile als zweiter Schritt** |
| 3 | `project.json` weiter automatisch schreiben? | **Ja, aber nur Export/Snapshot, nie Import-Auto-Sync** |
| 4 | Sichtbarer Sync-Status statt Merge-Dialog? | **Ja, verpflichtend für Phase 1** |

→ Folge: Diese 4 Entscheidungen sind fix. Werden in ADR-053 übernommen, brauchen keine weitere Diskussion.

### 3. Wie weiter

**User:** *"Erst CGR-Serie abschliessen, dann ADR getrennt"*

**Interpretation in Kombination mit Antwort 1:** Erst Runde 3 fertig laufen lassen, dann CGR-Serie als abgeschlossen markieren, dann ADR-053 als separater Schritt in eigener Session anlegen.

→ Reihenfolge:
1. r3 Detail-Diskussion (jetzt)
2. CGR-Serie abschließen (README.md finalisieren, INDEX.md auf "Abgeschlossen")
3. ADR-053 schreiben (eigener Schritt, doc-pflege Skill)
4. Tracker-Tasks anlegen (Spike 0 + Folge-Spikes als post-v1)

## Offene Punkte (für r3-Planung)

User hat nicht spezifiziert welche Detail-Themen für r3 wichtig sind — muss vor dem r3-Prompt geklärt werden.
