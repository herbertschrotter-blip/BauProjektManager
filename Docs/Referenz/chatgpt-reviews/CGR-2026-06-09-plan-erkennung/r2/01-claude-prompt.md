## Gesprächsformat
Antwort komplett in den Canvas. CANVAS-TITEL: "Review Runde 2". Sprich zu Claude, nicht zum User. Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

## Repo-Zugriff
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch, NICHT `main`.
- Relevant: `src/BauProjektManager.PlanManager/Services/` (FileParseService, ImportWorkflowService, RevisionDecisionService, ImportExecutionService, PlanManagerDatabase, DocumentKeyBuilder), `Docs/Kern/DB-SCHEMA.md` Kap. 6.7, `Docs/Module/PlanManager.md`.

## Stand nach Runde 1
Wir waren uns einig: Extract→Normalize→Alias→Learn ist fachlich richtig, heutige Feldextraktion ist positionsbasiert (Bruch), Alias nur „Auto-Suggest + Confirmed Learn", Ordnername aus Stammdaten nicht aus Alias. Deine 3 Sign-off-Bedingungen für Recognition v2 stehen.

**Zwei Updates seither:**

**(1) Dein gemeldeter Feldkey-Bug ist bestätigt.** `FileParseService` schreibt `extractedFields` mit Key = `segDef.FieldTypeId` (snake_case, z.B. `plan_number`/`plan_index`). `ImportWorkflowService` liest aber `"plannumber"`/`"planindex"` (ohne Unterstrich). Folge: `ClassifiedImportFile.PlanNumber` und `RevisionToken` sind **null**. Der Import lief bisher nur, weil `document_key` aus `DocumentKeyBuilder` kommt (der `profile.IdentityFields` korrekt liest). Heißt: **Index-/Revisions-Erkennung ist faktisch tot** (RevisionToken immer null → nie „neuer Index").

**(2) Der Auftraggeber stellt die Grundstrategie infrage** — und das ist der eigentliche Inhalt dieser Runde.

## Die strategische Frage: Strategie A vs Strategie B

**Beobachtung des Auftraggebers:** Planbezeichnungen sind in der Praxis chronisch uneinheitlich (jedes Büro/jede Quelle anders). Voll-Auto-Erkennung ist damit prinzipiell gedeckelt — man jagt ewig Sonderfällen hinterher.

### Strategie A — Auto-Recognition v2 (Runde-1-Ansatz)
Maschine extrahiert Identität (haus/geschoss/plannummer/index) aus dem Dateinamen — via FieldExtractionRule (regex named captures) → Normalize → Alias → Confirmed-Learn → später OCR. Zero-Touch-Ziel.

### Strategie B — Manuelle Erstaufnahme + Revisions-Matching (neuer Vorschlag)
**Kernidee:** Die mehrdeutige Identität vergibt der **Mensch einmal pro Plan** (Erstaufnahme). Danach macht die Maschine nur noch das **eng begrenzte, zuverlässige** Matching: *Ist diese Datei (a) exakt derselbe Plan (MD5-Dublette → Skip), (b) ein neuer Index eines bekannten Plans (→ neue Revision/Supersede), oder (c) neu (→ Erstaufnahme)?*

**Manuelle Erstaufnahme als komfortable Drag&Drop-Matrix (kein Tippen, kein Wizard):**
```
Oben:  Liste loser Eingang-Dateien (z.B. 15)   [Datei][Datei][Datei]…
Unten: Matrix-Container
       Spalten = Bauteile (aus Projekt-Stammdaten, DB-Abfrage)   [+ Bauteil → Projekt-Einstellungen]
       Zeilen  = Plantypen (Polierplan/Schalung/Bewehrung…, erweiterbar)
       Zelle [Bauteil × Plantyp] = Drop-Target
Bedienung: 1..n Dateien in die Zelle ziehen → building_part_id + document_type in EINER Geste gesetzt.
```
Plannummer/Index werden, wo zuverlässig aus dem Namen lesbar, **vorbefüllt/vorgeschlagen** (Assist), aber der Mensch bestätigt durch das Ablegen in die Zelle.

**Reuse:** Das bereits gebaute Schema v2.0 (BPM-109) trägt B vollständig: `plan_documents` = erfasste Pläne; MD5-Fingerprint = „selber Plan"; `RevisionDecisionService` + `plan_revisions` + Supersede = „neuer Index"; `document_key`/`released_at` unverändert. Auto-Extraktion (Regex/Alias/OCR aus Strategie A) wird vom Rückgrat zum **optionalen Vorbefüller** der Erstaufnahme.

**Claudes vorläufige Einschätzung:** B ist wahrscheinlich der bessere MVP — Arbeitsteilung (Mensch: mehrdeutige Identität einmal; Maschine: repetitives Matching jedes Mal), trifft den echten täglichen Polier-Schmerz (Updates, nicht Ersteinrichtung), zuverlässig (Vergleich gegen kleine bekannte Menge + MD5), de-riskt, nutzt das Gebaute. Ich bin aber bewusst offen — überzeuge mich vom Gegenteil, wenn du Schwächen siehst.

## Deine Aufgabe (Runde 2)

1. **A vs B als MVP:** Welche Strategie ist angesichts des Namens-Chaos der tragfähigere MVP? Argumente für/gegen. Gibt es einen klaren Industrie-Präzedenzfall für B (manuelle Erstklassifikation + danach reines Versions-Matching)?
2. **Zuverlässigkeit von B's Matching:** Reicht für „neuer Index eines bekannten Plans" ein Anker auf die **Plannummer** (stabilster Token) + MD5? Wo bricht das (Plannummer variiert auch, fehlt, oder mehrere Pläne teilen Nummer)? Wie viel Auto-Extraktion braucht B *mindestens* (nur Plannummer? auch Index?)?
3. **Scope-Effekt:** Wenn B, können dann 007.02/.03 (Regex-Feldextraktion), 109.06 (Alias-Mapping) und das OCR-Modul **aus V1 raus** und rein optional/post-V1 werden? Oder bleibt ein Minimum zwingend?
4. **Drag&Drop-Matrix-UI:** Trägt das Konzept bei realer Last (15+ Dateien, viele Bauteile × viele Plantypen → breite/große Matrix)? Skalierungs-/UX-Fallen? Bessere Alternativen für „1..n Dateien einer [Bauteil×Plantyp]-Identität zuordnen"?
5. **Hybrid:** Ist „B als Kern + A nur als optionaler Vorbefüller der Zellen" die richtige Synthese — oder verwässert das beides? Wo genau ziehst du die Grenze?
6. **Der bestätigte Feldkey-Bug** (plan_number vs plannumber): zentral fixen (SegmentTypeIds-Konstanten) — beeinflusst das die A/B-Entscheidung, oder ist es nur Hygiene, die beide brauchen?

Sign-off-Ziel: klare MVP-Strategie (A, B oder definierter Hybrid) + minimal nötiger V1-Scope für zuverlässige Plan-Sortierung + Update-Erkennung.
