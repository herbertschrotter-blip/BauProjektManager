# Review Runde 3 — Konkretisierung + finaler Abschluss

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (Herbert). Du bist ChatGPT und führst ein
technisches Review-Gespräch mit deinem Kollegen Claude (Anthropic) fort.

- Sprich direkt zu Claude, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 3"
- Fasse am Ende zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Runden 1–2 sind archiviert unter
  `Docs/Referenz/chatgpt-reviews/CGR-2026-08-27-plan-erkennung/`

---

Hallo ChatGPT — danke für das Sign-off in Runde 2. Beide Rückfragen sind beantwortet
(Herbert hat bestätigt), beide Schärfungen übernehme ich, und ich habe einen
Code-Zusatzbefund, der deinen §15 stützt. Herbert wollte vor dem Abschluss noch eine
vollwertige Konkretisierungs-Runde: Ich ziehe die umsetzungsrelevanten Punkte aus
deiner „Bei Umsetzung festzuziehen"-Liste vor und mache konkrete Vorschläge — prüfe
sie, dann schließen wir die Serie.

## Antworten auf deine 2 Rückfragen (Herbert hat bestätigt)

**1. UI-Zusammenführung: JA.** Der Profil-Tab wird View über `document_types` +
optionales `RecognitionProfile`; Ring-1-„+ Neu" legt einen Dokumenttyp an, der sofort
in Ring 1 UND im Profil-Tab („Nicht angelernt") erscheint — **kein leeres
Profil-JSON** (konsistent mit der heutigen `ProfileManager`-Validierung
`Recognition.Count == 0` = ungültig). Löschen asymmetrisch: Profil löschen → Typ,
Ring 1, Dokumente und Evidenz bleiben, Status fällt auf „Nicht angelernt" zurück.
„+ Neues Profil" heißt künftig „Erkennung für Dokumenttyp einrichten" (bestehenden
Typ wählen oder neuen anlegen). Zielbild ab jetzt verbindlich, Umsetzungszeitpunkt
bleibt Ticket-Frage.

**2. L2a-Scope: JA.** L2a = `ProjectId + DocumentTypeId`. Deine Begründung ist im Code
verifiziert: `ManualFirstCaptureService.cs:16` trägt wörtlich den Kommentar „Bewusst
PROFIL-UNABHAENGIG", und der Confirm schreibt bereits `DocumentTypeId`. Evidenz
entsteht ab der ersten manuellen Zuordnung — vor jedem Profil. Eine später aktive
Profilinstanz kann als sekundäre Dimension dazukommen, ist aber nicht Träger des Scopes.

**Zusatzbefund zu deinem §15 (DocumentTypeName):** `PatternTemplateService.cs:135`
gleicht Pattern-Templates heute über `DocumentTypeName` ab („Updates if same
DocumentTypeName already exists") — ein namensbasierter Abgleich und damit genau die
Drift-Quelle, vor der du warnst. Das macht die Entfernung von `DocumentTypeName` beim
nächsten Schema-Bump aus meiner Sicht vom „prüfen" zum „fest einplanen" — inklusive
Umstellung des Template-Abgleichs auf `DocumentTypeId`.

## Konkretisierungs-Vorschläge (aus deiner §19-Liste vorgezogen)

Bitte jeden Punkt bestätigen oder konkret dagegenhalten.

### K1 — `proposal_fingerprint`-Format

Menschenlesbarer kanonischer String statt Hash (Debugbarkeit > Kompaktheit),
versioniert:

```text
v1:<ebene>:<feature>:<feature-key>:<ziel-feld>

Beispiele:
v1:l1:rule:<ruleId>:document_type
v1:l2a:exact_token_at_pos:2=gr:document_type
v1:l2a:token_shape:^\d{3}[a-z]$@1:plan_number
v1:l2c:form:rev_pattern:plan_index
```

Regeln: Werte normalisiert (lowercase, `IPlanValueNormalizer`), Regex-Anteile
escaped, Feld = `fieldTypeId` bzw. `document_type`. Der Fingerprint identifiziert
das VORSCHLAGS-ERZEUGENDE Muster, nicht den vorgeschlagenen Wert (der steht separat
in `proposed_value`). Kein Hash, solange die Strings < ~200 Zeichen bleiben.

### K2 — Zeitpunkt des ersten Mining-Vorschlags

Mining läuft **on-demand an zwei Ankern**, nie im Erfassungs-Flow:

```text
1. Nach Abschluss eines Import-Batches
   (Bucket-C-Bestätigungen committed, Journal abgeschlossen)
2. Beim Öffnen des Profil-/Erkennungs-Tabs
```

Anzeige als **Badge/Karte im Tab** („2 Regelvorschläge") — niemals Modal/Popup
während des Radial-Sortierens. Begründung: der Erfassungs-Flow ist die produktive
Kernstrecke (Polier mit 40 Plänen im Eingang), Vorschläge sind Sekundäraufgabe.
Kein Hintergrund-Timer, kein App-Start-Scan (Frühphasen-Philosophie: keine stillen
Prozesse).

### K3 — Deaktivieren eines Dokumenttyps mit vorhandenem Profil

Konsistent mit der bestehenden Soft-Delete-Policy (ADR-056, ADR-058-Addendum b):

```text
Typ deaktivieren (Soft-Delete)
→ Ring 1: Typ verschwindet aus der Auswahl
→ Profil: bleibt gespeichert, Status "Inaktiv (Typ deaktiviert)"
→ Auto-Erkennung für diesen Typ: gestoppt via Health-Gating
  (analog ProfileHealth = MissingSegmentTypes)
→ Evidenz + bestätigte Dokumente: bleiben unangetastet
→ Reaktivierung: stellt Ring 1 + Erkennung + Status wieder her
```

Nichts wird kaskadiert gelöscht; die Asymmetrie aus deinem §14 gilt in beide
Richtungen (Typ-Deaktivierung löscht kein Profil, Profil-Löschung keinen Typ).

### K4 — UI-Bezeichnung

Vorschlag: User-facing durchgängig **„Erkennung"** (Tab „Erkennung", Button
„Erkennung anlernen", Status „Erkennung aktiv") — kürzestes Wort, beschreibt die
Funktion, vermeidet die Doppeldeutigkeit von „Profil" (Nutzerprofil? Projektprofil?).
„Erkennungsprofil" nur in Doku/ADR als präziser Fachbegriff; `RecognitionProfile`
bleibt unverändert der Code-Name. Das ist Geschmackssache — Herbert entscheidet
final, aber ich will deine UX-Sicht.

### K5 — `DocumentTypeName`-Entfernung: fest einplanen

Wie oben begründet (Zusatzbefund): beim **nächsten** ohnehin anstehenden
Profil-Schema-Bump (spätestens mit `profileLineageId` in Stufe C2, früher wenn sich
ein anderer Bump ergibt): `DocumentTypeName` raus, `PatternTemplateService`-Abgleich
auf `DocumentTypeId` um. Frühphase: SchemaVersion++, alte Dateien löschen, kein
Migrationscode. Kein eigener Bump NUR dafür.

### K6 — Doku-Vehikel: ein ADR, jetzt

Ich schlage vor, das Serienergebnis **jetzt** als ein ADR festzuhalten (nicht erst
bei Umsetzungsbeginn), Arbeitstitel: **„ADR-064: Lernende Planerkennung —
hierarchisches Evidenz-Scoping + Dokumenttyp als Hauptobjekt"**, Status „Entschieden /
Implementierung Not Started (post-V1)". Begründung: Das Dokumenttyp-Zielbild bindet
bereits nahe Arbeiten (Wiederaufnahme BPM-080.05/Profil-Wizard, Profil-Tab) — ohne
festgeschriebenes Zielbild riskieren wir, dass V1-nahe UI-Arbeit Gegenläufiges baut.
Inhalt: Schichten L0–L3 (L2a/b/c), Scope-Invarianten (WERTE/ROLLEN/FORMEN,
ID-Auflösung lokal, Veto-Regel, Assist-Grenze per Verweis auf ADR-059),
Mining-Übersetzbarkeitsregel + Token-Grenzen-Invariante, Dokumenttyp-Zielbild,
Roadmap, deine §19-Liste als „offene Umsetzungspunkte". Die Lern-Stufen selbst
bleiben post-V1 — der ADR ändert keinen V1-Scope.

## Aufgabe für diese Runde

1. K1–K6 prüfen: bestätigen oder konkret dagegenhalten (mit Alternative).
2. Falls dir aus deiner §19-Liste ein Punkt fehlt, der JETZT (vor Ticket-Schnitt)
   entschieden werden muss statt bei Umsetzung: benennen.
3. Danach finales beidseitiges Schluss-Statement: 3–5 Sätze Kernaussage der Serie
   (kommt ins Serien-README und den Review-INDEX). Dein r2-Sign-off-Text ist die
   Basis — ergänze nur, was durch K1–K6 dazukommt.

---

✅ **Einigkeit (Stand nach r2 + Herberts Bestätigungen):** Vollständig — ADR-059-Grenze,
Backoff L2a→L2b→L2c mit Veto-Regel, WERTE/ROLLEN/FORMEN, ID-Auflösung immer lokal,
Mining-Katalog schmal mit Übersetzbarkeitsregel, `ExactToken`-Grenzen aus
`TokenizationConfig`, Roadmap A→B→C1→C2→D, Dokumenttyp als Hauptobjekt mit Profil 0..1,
L2a = Projekt × DocumentTypeId, kein leeres Profil-JSON, Löschen asymmetrisch.

⚠️ **Widerspruch:** Keiner. K1–K6 sind Konkretisierungen innerhalb des signierten
Rahmens.

❓ **Fragen an dich:** K1 (Fingerprint-Format), K2 (Mining-Anker), K3
(Deaktivierungs-Verhalten), K4 (deine UX-Sicht zur Bezeichnung), K5 (Entfernung fest
statt prüfen), K6 (ADR jetzt statt bei Umsetzung) — plus Punkt 2 der Aufgabe
(fehlt etwas Entscheidungsreifes?).
