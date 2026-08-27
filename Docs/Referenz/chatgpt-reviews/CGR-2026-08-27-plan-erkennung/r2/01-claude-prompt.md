# Review Runde 2 — Lernende Planerkennung: Antworten + Sign-off

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (Herbert). Du bist ChatGPT und führst ein
technisches Review-Gespräch mit deinem Kollegen Claude (Anthropic) fort.

- Sprich direkt zu Claude, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 2"
- Fasse am Ende zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!
- Runde 1 ist archiviert unter
  `Docs/Referenz/chatgpt-reviews/CGR-2026-08-27-plan-erkennung/r1/`

---

Hallo ChatGPT — starke Runde 1, danke. Deine Korrektur „Profil ≠ Quelle" habe ich im
Code verifiziert (`ProfileManager.cs` registriert `PersistenceScope.ProjectLocal`,
Profile sind projektlokale Recognition-Konfigurationen pro Dokumenttyp, ADR-046/061) —
mein r1-Kurzschluss war unsauber, ich übernehme deine Formulierung „Profilinstanz =
projektlokale Recognition-Konfiguration". Herbert hat inzwischen drei Entscheidungen
getroffen (unten). Damit sind aus meiner Sicht alle strittigen Punkte aufgelöst; diese
Runde ist als Sign-off-Runde gedacht.

## Herberts Entscheidungen (verbindlich)

1. **Hierarchisches Scope-Modell übernommen** — Zielarchitektur = dein Backoff
   `Projekt-Evidenz (L2a) → Profil-Familie/Lineage (L2b) → globales Basiswissen (L2c)`
   samt `profileLineageId`. Umsetzung gestaffelt: Ausbaustufe A implementiert NUR L2a.
2. **Mining-Katalog: schmal starten** — Details in Antwort 5.
3. **Alias-Stufe kommt zurück in die Roadmap** — als eigene Ausbaustufe (Begründung
   in „Roadmap final" unten).

## Antworten auf deine 5 Rückfragen

**1. Hierarchisches Backoff-Modell — ja, mitgetragen.** Als Zielarchitektur, nicht als
Big-Bang: Stufe A bleibt rein projektlokal, L2b kommt erst mit Lineage-Persistenz,
L2c ist kuratiertes Repo-Wissen (kein Lernen). Dein Vorrang-statt-Prozentmischung-Prinzip
ist exakt meine Anti-Scheinpräzisions-Linie aus r1 — wir sind uns einig, dass es NIE
eine ebenen-übergreifende Score-Fusion gibt. **Ein Punkt muss in den späteren ADR:**
die harte Definition von „ausreichend starke Evidenz" pro Ebene. Mein Vorschlag als
Startwert (bewusst konservativ, bei Umsetzung kalibrierbar):

```text
L2a projektlokal:    Support n ≥ 5,  Purity ≥ 0,90
L2b Profil-Familie:  Support n ≥ 10, Purity ≥ 0,95   (strenger — fremdes Projekt)
L2c global:          markiert nur Kandidaten (Formen), macht NIE Wert-Vorschläge
```

Backoff-Regel: eine Ebene wird nur befragt, wenn die höhere Ebene ihre Schwelle nicht
erreicht — nicht, wenn sie widerspricht (lokale Gegen-Evidenz unterhalb der Schwelle
blockiert den Familien-Vorschlag trotzdem nicht komplett, wird aber im Begründungstext
ausgewiesen: „Familie: 34/35 Grundriss · dieses Projekt: 2/2 abweichend"). Einverstanden?

**2. Profil ≠ Quelle — ja, bestätigt und verifiziert.** Siehe oben. Konsequenz
mitgenommen: Cross-Project-Übertragung läuft ausschließlich über explizite Lineage,
nie über Namens- oder DocumentTypeId-Gleichheit.

**3. `profileLineageId` — ja, und genau so schlank:** stabile ULID, beim ersten Anlegen
eines Profils erzeugt, bei „Kopieren" / „Als Vorlage für neues Projekt verwenden"
**vererbt**. Keine nachträgliche Heuristik, kein Fingerprint-Matching, kein
Namensvergleich — alles Implizite wäre genau die Magie, die wir vermeiden wollen.
Schlanker geht es aus meiner Sicht nicht: eine einzige additive Eigenschaft im
Profil-JSON, deren einzige Semantik „gemeinsame Herkunft" ist. Kommt erst mit
Ausbaustufe C; falls dann noch Frühphase gilt, regulärer SchemaVersion-Bump + Reset
statt Migration.

**4. Scope-Invariante — ja, wortwörtlich ADR-tauglich.** Deine Formulierung
(„Recognition-Evidenz ist hierarchisch gescoped … Projektgebundene Stammdaten-IDs
werden niemals scope-übergreifend gelernt oder übertragen") übernehme ich unverändert.
Herbert hat dieselbe Frage unabhängig gestellt („Projektnummer/Plannummer/Index doch
nicht global?") — als Merksatz für den ADR schlage ich die Dreiteilung vor:

```text
WERTE   (5998, H64, Index "B")            → nur projektlokal
ROLLEN  ("Token 0 = Projektnummer")       → nur Profil-Familie (Wert wechselt
                                            pro Projekt, Struktur bleibt)
FORMEN  (^\d{3,5}$ = Nummernkandidat)     → global erlaubt (markiert Kandidaten,
                                            stiftet nie Identität)
```

Dein Beispiel 14 illustriert die zugehörige Auflösungs-Regel, die ich als zweite
Invariante festhalten würde: **Familien-/Global-Evidenz liefert nur Wert-Kandidaten;
die Auflösung auf Stammdaten-IDs passiert immer lokal gegen das Zielprojekt**
(ADR-061-konform, Fail-Fast bei fehlendem Treffer).

**5. Mining-Katalog — Herbert hat „schmal" entschieden, mit einer Schärfung deiner
Übersetzbarkeits-Regel.** Deine Regel („nur Muster, die in explizite BPM-Regeln
übersetzbar sind") sortiert den Katalog nämlich von selbst:

- **Stufe B (sofort):** `ExactTokenAtPosition`, `TokenPrefix`, `TokenSuffix`,
  `TokenShape` — alle heute in `RecognitionRule` (`segment`/`regex`) ausdrückbar.
- **`ExactToken` (positionsunabhängig): ja, aber NUR mit Token-Grenzen-Semantik** —
  übersetzt als Regex mit Delimiter-Klasse (`(^|[_\-\s.])GR([_\-\s.]|$)`), niemals als
  Substring. Ohne diese Bedingung wäre es das bewusst entfernte `contains` durch die
  Hintertür — du hast die Gefahr selbst benannt, das ist die explizite Absicherung.
- **Warten auf `FieldExtractionRule` (post-V1):** `TokenOrderPair`, `TokenCount`,
  `DelimiterPattern` — in `segment`/`regex` nicht sauber ausdrückbar, fallen per
  deiner eigenen Regel raus und kommen wieder, wenn das Regelmodell sie tragen kann.

## Roadmap final (mit Herberts Alias-Entscheidung)

In deiner r1-Roadmap ist meine Alias-Stufe stillschweigend verschwunden — Herbert hat
entschieden, sie kommt als **eigene Stufe** zurück. Begründung: Aliasse
(„Grundr."/„GR"/„Floor Plan" → derselbe Segmentwert) sind orthogonal zu Lineage
(Schreibweisen-Normalisierung vs. Struktur-Übertragung) und teilweise bereits
beschlossen (`building_part_aliases`, ADR-058 / BPM-109.06 — exakte Normalisierung,
kein Fuzzy, User-Bestätigung). Die Stufe generalisiert dieses Muster auf Segmentwerte.

```text
V1        L0 + L1 + Radial-Erfassung           (unverändert, sammelt Labels)
Stufe A   Projektlokale Evidenz (L2a)          Vorfüllung Radial/Panel
Stufe B   Rule Mining (schmaler Katalog)
          + recognition_feedback               Messbarkeit: Akzeptanzrate
Stufe C1  Segmentwert-Aliasse                  eigene Stufe, dockt an BPM-109.06 an
Stufe C2  Profil-Lineage (L2b)                 profileLineageId + Familien-Evidenz
Stufe D   Globales Lexikon + Tokenformen (L2c) kuratiert, konservativ
danach    ML-Experiment                        nur bei gemessenem Bedarf, offline
```

Zeitliche Anmerkung: C1 vor C2 gereiht, weil BPM-109.06 ohnehin ansteht und Aliasse
keinerlei neue Architektur brauchen; wenn du C2 vor C1 für wertvoller hältst,
argumentiere — Herbert kann das bei der Ticket-Planung frei schieben, die Stufen sind
unabhängig.

## Übernommen aus deiner r1 (ohne Einwand)

- `recognition_feedback`-Schema inkl. `proposal_fingerprint` + `outcome`
  (`confirmed`/`corrected`/`rejected`) — dein Zusatz `proposal_fingerprint` ist die
  richtige Ergänzung meines Entwurfs (welches Muster hat den Vorschlag erzeugt).
- Drift-Modell: LongTerm/Recent-Fenster, Zustände `Stable`/`DriftSuspected`/
  `ReviewRequired`, nie stille Regeländerung, bei bestätigtem Schemawechsel neue
  Regelgeneration statt Evidenz-Vernichtung.
- Quellen-Dimension: YAGNI heute, Service-Schnitt mit optionalem Evidence Context
  (`ProfileLineageId?`, `SourceId?` später).
- Feedback bei Cross-Scope: kein globaler Aggregator über widersprüchliche Projekte
  („GR = mehrdeutig" statt „Grundriss 75 %").

## Aufgabe für diese Runde

1. Prüfe meine Antworten 1–5 — insbesondere die Schwellen-Startwerte (Antwort 1),
   die Token-Grenzen-Bedingung für `ExactToken` (Antwort 5) und die zweite Invariante
   „Auflösung auf IDs immer lokal" (Antwort 4).
2. Bewerte die finale Roadmap inkl. Alias-Stufe C1 und der Reihung C1 → C2.
3. Wenn nichts Substanzielles mehr offen ist: **explizites Sign-off** mit der
   Kernaussage der Serie in 3–5 Sätzen (wird ins Serien-README übernommen).
   Offene Detailpunkte, die erst bei der Umsetzung entschieden werden müssen
   (z. B. Schwellen-Kalibrierung), bitte als Liste „Bei Umsetzung festzuziehen"
   ausweisen statt als Blocker.

---

✅ **Einigkeit (Stand nach r1 + Herberts Entscheidungen):** ADR-059-Grenze unangetastet
(L0 einzige Auto-Stufe, alles Gelernte = `AutoSuggested`); kein ML.NET/Embeddings/LLM
im Importpfad, Rule Mining Priorität 1; `plan_document_segments` als Sample-Store,
gemined-te Regeln werden normale Profilregeln; hierarchisches Evidenz-Backoff statt
Score-Fusion; Profil = projektlokale Config, Lineage nur explizit; Stammdaten-IDs nie
scope-übergreifend; schlanke `recognition_feedback`-Tabelle; Drift → Review, nie stille
Änderung; Frühphase = Reset statt Migration.

⚠️ **Widerspruch:** Aus meiner Sicht keiner mehr — nur zwei Schärfungen zur Bestätigung:
Token-Grenzen-Bedingung für `ExactToken` (statt implizitem `contains`-Revival) und
Mining-Katalog-Split nach Übersetzbarkeit (OrderPair/Count/Delimiter erst mit
`FieldExtractionRule`).

❓ **Fragen an dich:** (1) Schwellen-Startwerte L2a/L2b tragfähig oder hast du bessere
Startpunkte? (2) Backoff-Detail aus Antwort 1 (untschwellige lokale Gegen-Evidenz
blockiert nicht, wird aber ausgewiesen) — einverstanden oder würdest du hart blocken?
(3) Reihung C1 (Alias) vor C2 (Lineage) okay? (4) Sign-off?
