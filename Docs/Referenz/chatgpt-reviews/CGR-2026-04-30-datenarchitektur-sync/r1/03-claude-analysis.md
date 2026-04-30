# Claude-Analyse — CGR-2026-04-30-datenarchitektur-sync — Runde 1

**Stand:** 2026-04-30 nach ChatGPT-Antwort

---

## Bewertung der Antwort

ChatGPTs Antwort ist **scharf, fundiert und in einem entscheidenden Punkt klüger als mein Initialprompt**. Drei Beobachtungen die in meinem Material zu kurz kamen oder falsch waren:

### Was ChatGPT besser gesehen hat

**1. Sync-toxische Schreibmuster im bestehenden Code (D-1)**
ChatGPT hat im Branch geprüft und gefunden: `ProjectDatabase.SaveBuildingParts/Participants/Links` löscht Kindlisten komplett und fügt sie neu ein. Plus: `DeleteProject` macht Hard Delete. Das sind **fundamentale Sync-Killer** — egal welche Sync-Engine man danach drauf setzt. **Diese Erkenntnis kommt vor der Engine-Wahl.** Ich hatte das nicht im Initialprompt erwähnt, war mir wahrscheinlich auch nicht voll bewusst.

**2. Supabase Realtime ≠ Offline-Sync**
Mein Initialprompt hat Supabase als "Postgres + Realtime + Auth" beworben und damit suggeriert, Realtime sei die Sync-Engine. Das stimmt nicht. **Realtime ist Change Notification, nicht Reconciliation.** ChatGPT trennt das sauber: Supabase liefert Server/Auth/RLS/Postgres + Realtime-Hinweise — der lokale Offline-Cache, Pull/Push, Konflikt-Behandlung muss BPM selbst designen.

**3. PouchDB.NET ist kein tragfähiger Architekturanker**
Ich hatte CouchDB+PouchDB.NET als "20 Jahre bewährt" beworben. ChatGPT korrigiert: CouchDB ist bewährt, **aber PouchDB.NET ist im .NET-Desktop-Kontext kein gleichwertiger Drop-in**. Im .NET-Ökosystem gibt es eher CouchDB.NET als HTTP-Client, kein vollwertiges eingebettetes Pendant. Der Architekturanker sollte ein **Protokoll** sein (Pull/Push, Soft Delete, Checkpoints), nicht eine konkrete Library.

### Wo ChatGPT eine Library nennt die ich nicht hatte

**Microsoft Datasync Framework** — offizielle Microsoft-Lib für Offline-Row-Sync gegen ASP.NET-Backends mit beliebigem DB. Passt sehr gut zur .NET/WPF/SQLite-Linie von BPM, ist aber lifecycle-mäßig zu prüfen (Aktualität 2026). Sollte als Fallback-Spike neben Supabase laufen.

### Wo ich ChatGPT zustimme — uneingeschränkt

- **Option 1 (Eigenbau β3) ist Wegwerf-Engineering** — was man im Cloud-Drive-Sync baut, wird im Server-Modus weggeworfen
- **Option 2 (CouchDB) erzwingt Datenmodell-Wechsel** von relational zu dokumentorientiert. BPM hat aber bereits SQLite-relational + Phase-3-Postgres-Ziel
- **Option 3 (Server-Pfad mit Postgres) ist die richtige strategische Richtung**, aber nicht als "Realtime-Sync löst offline" missverstehen
- **Cloud-Drive-Sync funktioniert für Notizen (Joplin, Obsidian, Syncthing) — nicht gleichwertig für relationale Fachmodelle** mit FKs und sensiblen Daten
- **Frühphase nutzen** — keine Migration, DB löschen, neu anlegen
- **Phase-3-Pfad: SQLite local-first + PostgreSQL server-authority + Pull/Push mit Checkpoints + Server gewinnt** als sauberster Pfad

### Wo ich nuancieren würde (nicht widersprechen, ergänzen)

**Reihenfolge der Spikes:** ChatGPT empfiehlt Supabase-Spike zuerst. Das ist okay als Lern-Spike, aber wenn der User self-hosted-Präferenz hat, ist der finale Stack ASP.NET + PostgreSQL + Microsoft Datasync (oder dünner Eigenbau-Row-Sync). In dem Fall könnte man auch direkt mit dem Microsoft-Datasync-Spike anfangen, ohne Supabase-Umweg.

**Manueller Projektimport heute:** ChatGPT sagt *"Für heute genügt weiterhin manueller Projektimport/Export"*. Stimmt. Aber: Der User hat den Schmerz `2-3 PCs abwechselnd` heute schon. Manueller Import via `project.json` ist ein workable Workaround **wenn der User es weiß und akzeptiert**. Sollte man als bewusste Übergangslösung erklären, nicht stillschweigend annehmen.

## Neue Erkenntnisse — Konsequenzen für BPM

### Sofort (vor jedem Sync)

1. **`ProjectDatabase` syncfähig machen:**
   - Hard Deletes raus → Soft Delete via `is_deleted = 1`
   - Kindlisten-Replace raus → gezielte Upserts pro Entität (geänderte updaten, neue inserten, gelöschte als `is_deleted` markieren)
   - Stabile ULIDs auch für Kindentitäten (BuildingPart, BuildingLevel, Participant, Link)
   - Tests dass `sync_version` bei jedem Update inkrementiert
   - Audit: in welchen Save-Methoden steckt der Bug?
2. Eigener Task im Tracker dafür — nicht in Sync-Spike vermischen
3. Frühphasen-Konform: keine Migration, Schema-Reset reicht

### Für Phase-3-Vorbereitung (mittel)

4. **Architektur-Ziel klar dokumentieren:**
   - Neuer ADR (z.B. ADR-053): "Sync-Strategie — SQLite local-first + Postgres server-authority + Pull/Push"
   - Supersedet implizit den 12-Schritte-Plan in DatenarchitekturSync.md (oder es wird auf "historisch" gestellt)
5. **Spike-Order entscheiden** je nach User-Antwort:
   - hosted akzeptabel → Spike 1 = Supabase, Spike 2 = ASP.NET+Postgres
   - self-hosted Pflicht → direkt ASP.NET+Postgres, ggf. Microsoft Datasync evaluieren
6. **Profil-Bibliothek-Diskussion** verschieben — gehört nach der Sync-Architektur-Entscheidung. Dann ist klar: Profile sind Klasse-C-Reference oder Klasse-B-Domain, und die Bibliothek liegt entweder im Server oder als globale Datei pro User.

### Heute / Pragmatisch

7. **Manueller Projektimport via `project.json`** als bewusster Workaround dokumentieren bis Server-Spike läuft. User weiß dass automatischer Sync noch nicht da ist.

## Strittige Punkte (für ChatGPT in Runde 2 falls Bedarf)

- Konkrete Spike-Reihenfolge: Supabase first vs Microsoft Datasync first vs eigener Minimal-API first — hängt von User-Antworten ab
- Microsoft Datasync Framework Aktualität 2026 — ChatGPT war nicht 100% sicher, sollte vor Spike geprüft werden
- Profil-Bibliothek-Architektur als Sub-Thema (war für r2/r3 markiert)
- DSGVO-Whitelist konkret pro Tabelle (B vs C Klassifizierung) — sollte in eigenem ADR landen

## Empfehlung an User

**Ich stimme ChatGPTs Empfehlung zu.** Sie ist die fachlich saubere Wahl:

- Kein Eigenbau β3 über OneDrive
- Kein CouchDB/PouchDB-Umbau
- Postgres-Server-Pfad vorbereiten — heute manueller Workaround, Spike als nächster größerer Schritt
- Vor dem Spike: ProjectDatabase syncfähig machen (Soft Delete, Upserts statt Replace)

Die 5 Rückfragen von ChatGPT sind die echten Entscheidungspunkte für den User. Nicht alle 5 muss er heute beantworten — die ersten 2-3 reichen für eine Architektur-Richtungsentscheidung. Die Detail-Fragen (VPS, Konflikt-Strategie) können in einem späteren Spike geklärt werden.

## Stufe-A-Fragen für User

Aus den 5 ChatGPT-Rückfragen kondensiert auf 3 Hauptpunkte:

1. **Hosted vs self-hosted?** → entscheidet über Supabase als ersten Spike vs direkt eigener Server
2. **Sync-Scope: nur Stammdaten oder auch Bautagebuch/Adressbuch sofort?** → entscheidet DSGVO-/Rollen-Aufwand
3. **Wie weiter:** Runde 2 mit Detail-Fragen oder Architektur-Richtungsentscheidung schon klar genug?
