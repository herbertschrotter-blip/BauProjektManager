# Review-Prompt für ChatGPT — Runde 1

## Rolle
Du bist ein Senior Software Architect mit AI-Tooling-Erfahrung
(Claude Code, MCP, Workflow-Design) und führst ein technisches
Review-Gespräch mit einem Kollegen (Claude/Anthropic).

## Gesprächsformat
Dieses Gespräch läuft über einen Vermittler (Herbert).

- Sprich direkt zu deinem Kollegen Claude, NICHT zu Herbert
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 1"
- Fasse am Ende JEDER Antwort zusammen:
  ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:

- **Repo:** `herbertschrotter-blip/claude-skills-bpm`
- **Branch: `main`** — IMMER diesen Branch verwenden
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen,
  und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht
- Bei JEDEM Dateizugriff den Branch `main` angeben

Relevante Dateien für dieses Review:

- `skills/cc-steuerung/SKILL.md` — der Skill um den es geht
- `skills/cc-steuerung/references/` — bei Bedarf für Detail-Patterns
- `INDEX.md` — Skill-Routing-Übersicht, Invariante 9 zum Modalitäts-Pattern
- `README.md` — Projekt-Onboarding (entnormativiert)

## Gesprächsregeln

- Ehrlich und kritisch — sei nicht nett, sei nützlich
- Probleme konkret benennen
- Verbesserungen mit Code/Pseudocode oder Architektur-Skizze zeigen
- Rückfragen wenn Kontext fehlt
- Fokus halten, keine allgemeinen Exkurse über AI-Tools
- Kompakt, Code/Pseudocode nur wenn nötig

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten. Das Skill-Repo
selbst ist auch noch in aktiver Stabilisierung (CGR-Skillsystem-Serie
gerade abgeschlossen).

Konsequenzen für deine Architektur-Vorschläge:

- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/Skill-Änderungen: stattdessen "Datei löschen,
  neu anlegen lassen" als gewollter Standardweg
- Skill-Aufteilungen oder Umbenennungen sind erlaubt — kein
  Zwang zur Rückwärtskompatibilität

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

## Projektkontext

### Setup

- **User:** Herbert, Polier in Österreich, kein traditioneller
  Programmier-Background, Copy-Paste-and-Test-Workflow
- **Projekt:** BauProjektManager (BPM), C#/.NET 10 LTS, WPF Desktop,
  modularer Monolith, offline-first
- **Skill-Repo:** `claude-skills-bpm`, 11 aktive BPM-Skills, projekt-
  agnostisches Skill-System mit `projects/bpm/` Konfiguration
- **Plattform:** Windows-PC (3 PCs synchronisiert via OneDrive),
  PowerShell-Default-Shell
- **Aktuelle Hauptarbeitsumgebung:** Claude.ai im Chat (Browser oder
  Desktop-App), MIT MCP-Server **Desktop Commander** (DC) für direkte
  Datei-Operationen auf Herberts PC
- **Claude Code (CC):** Installiert (`claude.exe` über WinGet), aber
  **faktisch nie genutzt** — alle Code-Operationen laufen über DC

### Wie der DC-Workflow heute aussieht

1. Herbert formuliert eine Aufgabe im Chat (claude.ai)
2. Claude liest Repo-Dateien via DC `read_file`
3. Claude editiert Dateien via DC `edit_block` / `write_file`
4. Claude liefert Commit-Sequenz als Code-Block
5. Herbert kopiert die Sequenz, führt sie selbst in PowerShell aus,
   pusht selbst — Claude pusht NIE direkt
6. Claude verifiziert Push via GitHub MCP

### Memory-Eintrag, der den aktuellen Default zementiert

> "Default-Ausführungsmodus ist DC (`edit_block` / `write_file`)
> sobald User das Konzept freigegeben hat. SUCHE/ERSETZE-Blöcke im
> Chat NUR wenn: 1) User explizit danach fragt, 2) DC nicht
> verfügbar ist, 3) Pfad außerhalb bekannter Repos liegt."

### Problem (cc-steuerung-004)

Herbert hat ursprünglich gewollt: **DC als Brücke ins Terminal zu
Claude Code**. CC sollte die eigentliche Code-Arbeit machen
(Multi-File-Refactor, Build-Loops, autonome Iteration), DC nur das
Steuern und Starten von CC.

Tatsächlicher Stand: DC hat sich zum alleinigen Code-Werkzeug
entwickelt. CC wird de facto nie aufgerufen.

Herberts Originalzitat:

> "ich wollte immer nur mit dc im terminal claude code steueren und
> nicht mit dc alles erledigen!"

Symptome:

- Build-Loop fehlt: DC kann `dotnet build` starten, aber kein
  Fehler-zurück-an-LLM-Feedback wie CC
- Multi-File-Refactor wird teurer: bei großen Änderungen muss jede
  Datei einzeln per `edit_block` angefasst werden, statt CC autonom
  über mehrere Files iterieren zu lassen
- Chunking-Overhead: DC hat 25-30-Zeilen-Limit pro Write — bei
  großen XAMLs/Klassen entstehen 5-10 Tool-Calls für eine Datei
- Skill-Name `cc-steuerung` ist irreführend: regelt faktisch DC
- Workflow-Schwellen unklar: ab welcher Aufgabengröße lohnt CC?

### Recherche-Befunde (Stand 2026-04-29)

Herbert hat Web-Recherche zur Frage durchführen lassen. Befunde:

1. **Skill-System läuft in CC identisch** zu Claude.ai — gleiches
   `SKILL.md`-Format (offener Standard), Frontmatter, progressive
   disclosure. Alle 11 BPM-Skills würden in CC ohne Änderung laufen.

2. **CC vom Chat steuern ist ungelöst:** Anthropic hat keinen
   offiziellen Mechanismus für Chat→CC-Messaging. GitHub Issue
   `anthropics/claude-code#27441` fordert genau das (External Message
   Injection API). Workarounds:
   - `claude --print` headless mode (verliert Session-State)
   - Agent SDK (`ClaudeSDKClient`) spawnt CC als Subprocess via
     stdin/stdout — funktioniert, aber komplex
   - Claude Code Dispatch (Telegram-Bot als Relay) — Linux/macOS-fokussiert
   - `claude-desktop-mcp` (third-party) für macOS, **nicht Windows**

3. **MCP-Sharing zwischen Claude Desktop und CC:** Bidirektional
   möglich. `claude mcp` importiert Claude-Desktop-Konfig in CC.

4. **CC-Stärken:** Persistente Session, Subagents (geteilter Context),
   Hooks (deterministische Ausführung), bessere Token-Effizienz für
   große Refactors, native Multi-File-Awareness.

5. **CC-Schwächen für Herberts Workflow:** Verliert das
   `ask_user_input_v0`-Pattern (Buttons), kein Conversational
   Branching, schwerer für Nicht-Entwickler zu bedienen.

### Skill-Architektur jetzt

`cc-steuerung` ist als Modalitäts-Skill (parallel zu Fachskills wie
`code-erstellen`) konzipiert. Trigger-Wörter: `cc`, `dc`,
`Claude Code`, `direkt auf den PC`. Body regelt PowerShell-Patterns,
Pfad-Auto-Discovery, Branch-Ermittlung.

Siehe INDEX.md Invariante 9 für Details.

## Aufgabe

Wir müssen entscheiden, **wie der Code-Workflow für BPM mittelfristig
aussehen soll**. Bitte gib uns deine Einschätzung zu drei Punkten:

### Frage 1 — Migrations-Pfad: Chat→CC, Hybrid, oder Status quo?

Drei Optionen stehen im Raum:

**A) Komplett-Migration zu Claude Code:** Herbert arbeitet ab sofort
nur noch in CC im Terminal. Skills laufen dort identisch. Chat
(claude.ai) wird nicht mehr genutzt.

**B) Hybrid:** Chat bleibt für Planung/Diskussion/`ask_user_input_v0`-
Workflows. CC wird für Code-intensive Aufgaben (Multi-File-Refactor,
Build-Loops) explizit zugeschaltet — vom Chat aus oder direkt vom
User gestartet.

**C) Status quo verbessern:** Chat + DC bleiben Hauptwerkzeug, DC-
Workflow wird optimiert (z. B. größere Edit-Blöcke, parallele Tool-
Calls), CC wird nur für extrem große Refactors selten aufgerufen.

Welcher Pfad ist für Herberts konkrete Situation (Polier, kein
Programmier-Background, Copy-Paste-and-Test-Workflow, Windows-PC,
3-PC-Setup) am besten? Begründe.

### Frage 2 — CC vom Chat steuern auf Windows: realistisch oder Fata Morgana?

Falls Hybrid (B) gewählt wird: Wie sieht der konkrete Mechanismus aus,
mit dem Claude im Chat eine CC-Session **mit Feedback-Loop** starten
und steuern kann, auf Windows?

Konkrete Optionen aus der Recherche:

- DC `start_process` mit `claude --print "<prompt>"` → einmalig,
  kein Session-State
- Eigener MCP-Server, der eine persistente CC-Subprocess via stdin/
  stdout am Leben hält (Agent SDK Pattern)
- Externe File-Watch-Brücke: Chat schreibt `inbox.md`, CC liest mit
  Hook
- Telegram-Dispatch oder ähnlicher Relay
- Etwas anderes?

Was ist auf Windows mit Anthropics offizieller Toolchain heute
**realistisch implementierbar**, ohne Custom-Engineering das Herbert
allein nicht stemmt? Was sollte Herbert lieber sein lassen?

### Frage 3 — Skill-Aufteilung `cc-steuerung` → `dc-steuerung` + `cc-launcher`?

Der aktuelle Skill heißt `cc-steuerung`, regelt aber nur DC. Drei
Optionen:

**A) Skill aufteilen:** `dc-steuerung` (DC-Patterns) und
`cc-launcher` (CC starten + Output zurückleiten).

**B) Skill behalten, umbenennen:** `pc-steuerung` als Oberbegriff
für alle PC-Operationen, mit klaren Trigger-Wörtern für DC vs CC.

**C) Skill behalten, präzisieren:** `cc-steuerung` bleibt, aber Body
und Description machen explizit dass es um DC geht. CC wird in
eigenem späteren Skill behandelt.

Welche Option ist sauber, welche pragmatisch? Berücksichtige dass
Skill-Refactors in diesem Repo Two-Place-Pflege erfordern (Repo +
`/mnt/skills/user/`) und Trigger-Stabilität ein wiederkehrendes
Problem ist (siehe `evals/methodik-anleitung.md`).

---

**Bonus (optional):** Falls dir bei der Lektüre des `cc-steuerung/
SKILL.md` strukturelle Schwächen auffallen die unabhängig von obigen
drei Fragen sind, nenne sie kurz. Aber Fokus bleibt auf den drei
Hauptfragen.

---

Antworte in Canvas mit Titel "Review Runde 1". Schließe mit:

✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
