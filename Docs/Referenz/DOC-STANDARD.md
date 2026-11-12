---
doc_id: doc-standard
doc_type: policy
authority: source_of_truth
status: active
owner: herbert
topics: [dokumentation, frontmatter, ai-quickload, authority, kapitelvorlagen, skill-laderegel]
read_when: [neue-doc-erstellen, doc-pflege, audit, init-projekt, doc-refactoring]
related_docs: []
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Verbindlicher Standard für Frontmatter, Quickload, Authority und Kapitelvorlagen
- Autorität: source_of_truth
- Lesen wenn: Neue Doc erstellen, Doc-Pflege, Audit, Projekt-Init, Doc-Refactoring
- Nicht zuständig für: Inhaltliche Fachregeln einzelner Module
- Kapitel:
  - 1. Zweck
  - 2. Ordnerstruktur
  - 3. Standard-Doc-Set bei Projekt-Init
  - 4. Frontmatter-Standard
  - 5. Authority-Level
  - 6. AI-Quickload-Block
  - 7. Kapitelvorlagen pro Doc-Typ
  - 8. Wie Skills Docs lesen (verbindliche Ladereihenfolge)
  - 9. INDEX.md als Router
  - 10. Generierter Katalog (geplant)
  - 11. Validierungsregeln
  - 12. CLAUDE.md
- Pflichtlesen:
  - Kapitel 6 (Quickload) bei jeder Doc-Erstellung
  - Kapitel 7 (Kapitelvorlagen) bei jeder Doc-Erstellung und Refactoring
  - Kapitel 8 (Skill-Laderegel) bei jeder Skill-Änderung
- Fachliche Invarianten:
  - Frontmatter Pflicht für Kern/, Module/, Referenz/, Test/
  - Quickload Pflicht für source_of_truth und secondary
  - Kapitelvorlagen pro doc_type verbindlich nur für neue Docs; Bestand behält thematische Gliederung
  - Einheitliche Skill-Ladereihenfolge für alle Doc-lesenden Skills
  - Skill = Enforcement, diese Doc = Norm

---

# DOC-STANDARD — Dokumentations-Norm

## 1. Zweck

Definiert den verbindlichen Standard für:
- YAML-Frontmatter in Projektdokumentationen
- AI-Quickload-Blöcke für KI-optimiertes Routing
- Authority-Level und Doc-Typen
- Kapitelvorlagen pro Doc-Typ
- Einheitliche Ladereihenfolge für Skills
- Ordnerstruktur und Pflege-Regeln
- Standard-Doc-Set bei Projekt-Init

**Prinzip:** Diese Doc dokumentiert WAS gilt. Die Skills erzwingen die Einhaltung.

---

## 2. Ordnerstruktur

```
Docs/
├── Kern/           ← Autoritativ, bei jeder Code-Änderung relevant
├── Referenz/       ← Ergänzend, laden wenn Thema aufkommt
├── Konzepte/       ← Zukunft, erst relevant wenn Modul gebaut wird
├── Module/         ← Implementiert oder im Bau
└── Test/           ← Manuelle Test-Anleitungen (doc_type test-guide)
```

**Ordnerregel für die doc_types `vision` und `test-guide`:** `vision` liegt in `Referenz/`,
`test-guide` in `Test/`. (Audit Review 01, R01-001 E19, entschieden 2026-09-24)

---

## 3. Standard-Doc-Set bei Projekt-Init

| Datei | Ordner | doc_type | Pflicht |
|-------|--------|----------|---------|
| INDEX.md | Root | index | Ja |
| CLAUDE.md | Root | — | Ja (kein Frontmatter) |
| DOC-STANDARD.md | Referenz | policy | Ja |
| Architektur.md | Kern | architecture | Ja |
| DB-SCHEMA.md | Kern | schema | Wenn DB vorhanden |
| BACKLOG.md | Kern | backlog | Ja |
| CHANGELOG.md | Referenz | changelog | Ja |
| ADR.md | Referenz | adr | Ja |
| DEPENDENCY-MAP.md | Referenz | reference | Ja |

Alle Skelett-Docs bekommen Frontmatter, Quickload und Kapitelstruktur ihres doc_type.

---

## 4. Frontmatter-Standard

### 4.1 Pflichtfelder

```yaml
---
doc_id: [eindeutige-id]
doc_type: [typ]
authority: [level]
status: [status]
owner: [name]
topics: [liste]
read_when: [liste]
related_docs: [liste]
related_code: [liste]
supersedes: [liste]
---
```

Optionales Feld: `superseded_by: [liste]` — doc_ids bzw. ADR-IDs, die dieses Doc ablösen
(siehe 4.5).

`authority` und `status` tragen ausschließlich einen Enum-Wert (Kap. 5 bzw. 4.3) — kein
Fließtext, keine Klammerzusätze. (Audit Review 01, R01-001 E19, entschieden 2026-09-24)

### 4.2 Erlaubte Doc-Typen

| Typ | Bedeutung |
|-----|-----------|
| index | Projekt-Router |
| architecture | Systemarchitektur |
| module | Implementiertes oder aktiv gebautes Modul |
| concept | Geplantes Modul, noch nicht gebaut |
| schema | Technisch verbindliche Datenstruktur |
| adr | Architecture Decision Records |
| reference | Hilfswissen, nicht Source of Truth |
| policy | Verbindliche Regel/Norm |
| backlog | Feature-Tracking, Planung |
| changelog | Versionshistorie |
| vision | Produktvision, Zielgruppe, strategische Positionierung |
| test-guide | Manuelle Test-Anleitung (Szenarien mit Setup und Cleanup) |

`vision` und `test-guide` sind neu (Audit Review 01, R01-001 E19, entschieden 2026-09-24);
Ordner siehe Kap. 2.

### 4.3 Erlaubte Status-Werte

| Status | Bedeutung |
|--------|-----------|
| active | Aktuell gültig |
| draft | In Arbeit, nicht verbindlich |
| deprecated | Veraltet, ersetzt |

### 4.4 Ausnahmen

- Docs in Konzepte/ KÖNNEN Frontmatter haben, MÜSSEN aber nicht bis Bau-Start
- CLAUDE.md, INDEX.md und AGENTS.md im Root bekommen KEIN Frontmatter

### 4.5 Abgelöste Docs

- Teilweise abgelöst → `status: active`, `authority: secondary`, Banner oben + optionales
  Frontmatter-Feld `superseded_by: [...]`
- Vollständig abgelöst → `status: deprecated`, `authority: historical`
- Andere Werte (z. B. `superseded`, `partially-superseded` oder `deprecated` als authority)
  sind nicht erlaubt — es gelten nur die Enums aus 4.3 und Kap. 5

(Audit Review 01, R01-001 E19, entschieden 2026-09-24)

---

## 5. Authority-Level

| Level | Bedeutung | Routing |
|-------|-----------|---------|
| source_of_truth | Verbindliche Primärquelle für die eigenen topics | Primary oder Secondary im Router |
| secondary | Ergänzende Quelle | Primary (erste Anlaufstelle) oder Secondary im Router |
| historical | Nur Nachvollziehbarkeit | NICHT als Primary |

Regeln:
- authority gilt für die eigenen `topics` des Docs
- Jedes Thema höchstens EINE source_of_truth
- historical (bzw. status deprecated) nicht als Primary
- Bei Widerspruch: source_of_truth gewinnt
- Die Router-Rolle Primary ist von authority entkoppelt (Kap. 9.3)
  (Audit Review 01, R01-001 E19, entschieden 2026-09-24)

---

## 6. AI-Quickload-Block

### 6.1 Pflicht

Jede Doc mit authority source_of_truth oder secondary MUSS einen
Quickload-Block haben. Direkt nach Frontmatter.

### 6.2 Schema

```markdown
## AI-Quickload
- Zweck: [1 Satz, max 15 Wörter]
- Autorität: source_of_truth | secondary | historical
- Lesen wenn: [Aufgabentypen]
- Nicht zuständig für: [Abgrenzung]
- Kapitel:
  - 1. [H2-Titel]
  - 2. [H2-Titel]
  - ...
- Pflichtlesen:
  - [Kapitel N bei Bedingung X]
- Fachliche Invarianten:
  - [Regeln die NIE gebrochen werden dürfen]
```

### 6.3 Feldregeln

- Autorität: nur der Enum-Wert (source_of_truth | secondary | historical) — kein Fließtext,
  keine Klammerzusätze. Abgrenzungen gehören in „Nicht zuständig für“.
  (Audit Review 01, R01-001 E19, entschieden 2026-09-24)
- Nicht zuständig für: PFLICHT bei source_of_truth
- Lesen wenn: NICHT leer bei source_of_truth und secondary
- Kapitel: Listet alle H2-Überschriften auf.
  Ausnahme doc_type `backlog`, `changelog`, `adr`: eine Sammelzeile statt Einzelliste,
  z. B. `- Kapitel: Versionseinträge absteigend (eine H2 je Version) + Dokumentations-Versionen`.
  (Audit Review 01, R01-001 E19, entschieden 2026-09-24)
- Pflichtlesen: Kapitel die IMMER geladen werden wenn Modul betroffen ist.
  Skills behandeln das als Blocking Condition.
- Fachliche Invarianten: Kernregeln die nie gebrochen werden dürfen.
  Werden im First-Pass gelesen, ohne Langform zu laden.
  Max 5 Punkte.
- Kein Fließtext — nur strukturierte Felder

### 6.4 Quickload-First-Pass

KI liest beim ersten Zugriff NUR Frontmatter + Quickload.
Aus dem Kapitel-Feld und Pflichtlesen entscheidet sie was nachgeladen wird.

---

## 7. Kapitelvorlagen pro Doc-Typ

Die Kapitelvorlagen gelten verbindlich nur für neue Docs: H2-Überschriften nummeriert,
Reihenfolge fest. Bestands-Docs (vor dem 2026-09-24 angelegt) behalten ihre thematische
Gliederung — auch der Regel-Teil darf thematisch gegliedert sein — und werden nicht
dagegen refactort. Bei neuen Docs sind Abweichungen von Vorlage oder Kapitelreihenfolge
eine Warnung (Kap. 11.2); Bestands-Docs werden dafür nicht gemeldet.
(Audit Review 01, R01-001 E19, entschieden 2026-09-24)

### 7.1 module

```markdown
## 1. Zweck und Scope
## 2. Datenmodell
## 3. Fachlogik / Workflow
## 4. UI-Verhalten
## 5. Schnittstellen
## 6. No-Gos / Einschränkungen
## 7. Offene Punkte
```

### 7.2 concept

```markdown
## 1. Zweck und Zielzustand
## 2. Datenmodell (geplant)
## 3. Workflow
## 4. Technische Umsetzung
## 5. Abhängigkeiten
## 6. No-Gos / Einschränkungen
## 7. Offene Fragen
```

### 7.3 architecture

```markdown
## 1. Solution-Struktur
## 2. Schichten und Abhängigkeiten
## 3. DI-Setup
## 4. Datenfluss
## 5. Querschnittsthemen
## 6. No-Gos
```

### 7.4 schema

```markdown
## 1. Tabellenübersicht
## 2. Tabellen-Definitionen
## 3. Beziehungen / Foreign Keys
## 4. Migrations-Logik
## 5. Offene Punkte
```

### 7.5 policy

```markdown
## 1. Zweck und Geltungsbereich
## 2. Regeln
## 3. Validierung / Enforcement
## 4. Ausnahmen
```

### 7.6 reference

```markdown
## 1. Zweck
## 2. Konventionen / Regeln
## 3. Beispiele
## 4. Offene Punkte
```

### 7.7 adr

Bestehendes Format: Status, Kontext, Entscheidung, Konsequenzen.

### 7.8 backlog / changelog / vision

Freie Struktur.

### 7.9 test-guide

```markdown
## 1. Zweck
## 2. Voraussetzungen
## 3. Setup
## 4. Szenarien
## 5. Cleanup
```

### 7.10 Regeln

Gelten für neue Docs (siehe Einleitung Kap. 7):

- H2 IMMER nummeriert
- Reihenfolge fest
- Leere Kapitel weglassen erlaubt
- Zusätzliche Kapitel am Ende erlaubt
- Jedes Kapitel steht für sich — keine impliziten Verweise
- Jedes Kapitel beginnt mit 1-2 zusammenfassenden Sätzen

---

## 8. Wie Skills Docs lesen (verbindliche Ladereihenfolge)

Alle Skills die Docs lesen MÜSSEN diese Reihenfolge einhalten:

```
1. INDEX.md laden → Routing (welche Doc?)
2. Frontmatter + AI-Quickload lesen → Filter (relevant? welches Kapitel?)
3. Fachliche Invarianten prüfen → Sofort sichtbar ohne Langform
4. Pflichtlesen-Kapitel laden → Immer wenn Modul betroffen
5. Weitere Kapitel nur bei Bedarf nachladen
```

### Betroffene Skills:
- **code-erstellen** — Hauptnutzer, vollständiger First-Pass
- **doc-pflege** — Beim Validieren, bei neuen Docs, bei Refactoring
- **audit** — Beim systematischen Prüfen aller Docs
- **chatgpt-review** — Quickloads verwandter Docs als Kontext im Prompt

### Nicht betroffen:
- git-commit-helper, chat-wechsel, cc-steuerung, suche-ersetze

### Blocking Condition:
Wenn ein Pflichtlesen-Kapitel nicht geladen wurde und das Modul betroffen ist
→ NICHT weitermachen, nachfragen.

---

## 9. INDEX.md als Router

### 9.1 Routing-Format

```markdown
### [Thema]
- Primary: [Pfad zur ersten Anlaufstelle für das Thema – nie historical/deprecated, siehe 9.3]
- Secondary: [Pfad zur ergänzenden Doc]
- Reference: [Pfad zur Hintergrund-Doc]
```

### 9.2 Was NICHT in INDEX.md gehört

- Doc-Katalog, Langbeschreibungen, Dateigrößen, Schlagwort-Listen

### 9.3 Rolle Primary (Router-Regel)

Primary im INDEX = erste Anlaufstelle für das Thema; muss nicht authority source_of_truth
sein, darf aber nie historical/deprecated sein.
(Audit Review 01, R01-001 E19, entschieden 2026-09-24)

---

## 10. Generierter Katalog (geplant)

**Status: geplant** — `docs.catalog.json` und ein Generator existieren noch nicht.
(Audit Review 01, R01-001 E19, entschieden 2026-09-24)

Zielbild: docs.catalog.json aus Frontmatter generiert. Nie manuell editiert.

---

## 11. Validierungsregeln

Jede Prüfregel steht in genau einer Stufe.
(Audit Review 01, R01-001 E19, entschieden 2026-09-24)

### 11.1 Stufe A — Formal (Blocker)

- Frontmatter vorhanden bei Kern/, Module/, Referenz/, Test/
- Alle Pflichtfelder vorhanden
- doc_id eindeutig, Enums korrekt (doc_type, authority, status nur als Enum-Wert, auch im
  Quickload-Feld „Autorität“)
- Quickload vorhanden bei source_of_truth und secondary
- Quickload Kapitel-Liste stimmt mit H2s überein (Sammelzeile bei backlog, changelog, adr —
  Kap. 6.3)
- Fachliche Invarianten max 5
- Pflichtlesen verweist auf existierende Kapitel

### 11.2 Stufe B — Semantisch (Warnung)

- historical/deprecated Doc als Primary im Router
- related_docs nicht existierend
- Kapitelvorlage oder Kapitelreihenfolge nicht eingehalten — nur bei neuen Docs (Kap. 7);
  Bestands-Docs mit thematischer Gliederung werden dafür nicht gemeldet
- Fachliche Invarianten leer bei großem Modul

---

## 12. CLAUDE.md

Spezial-Datei für Claude Code Terminal. Kein Frontmatter, kein Quickload.
Bei Init als Skelett mit Projektname, Stack, Coding-Standards, Git-Regeln,
Verweisen auf INDEX.md und DOC-STANDARD.md.

**AGENTS.md** (Root, versioniert): kurze Weiterleitung auf CLAUDE.md für andere Agenten —
nennt nur die Einstiegspunkte (CLAUDE.md, INDEX.md, DOC-STANDARD.md) und wiederholt keine
Regeln. Kein Frontmatter, kein Quickload. Maßgeblich bleibt CLAUDE.md.
(Audit Review 01, R01-001 E18, entschieden 2026-09-24)
