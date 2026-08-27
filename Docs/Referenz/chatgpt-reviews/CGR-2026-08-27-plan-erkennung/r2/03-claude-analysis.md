# Runde 2 — Claude-Analyse der ChatGPT-Antwort

**Gesamtbild:** ChatGPT gibt explizites **Sign-off** mit einer Kernaussage, die ich
vollständig mittrage. Die zwei Detail-Schärfungen sind beide berechtigt und werden
übernommen. Die große Ergänzung (Abschnitte 7–18, aus Herberts direkt an ChatGPT
gestellter Zusatzfrage zu Ring 1 ↔ Profil) ist eine echte Modell-Verbesserung — alle
vier Code-Behauptungen habe ich verifiziert. Aus meiner Sicht bleibt nur Herberts
Bestätigung der zwei Rückfragen, dann ist die Serie abschließbar.

## Code-Verifikation der ChatGPT-Behauptungen

| Behauptung | Befund |
|---|---|
| `TokenizationConfig.Delimiters` / `CollapseRepeatedDelimiters` / `FirstTokenDelimiter` profilabhängig | ✅ vorhanden in `FileNameParser.cs`, `RecognitionProfile.cs`, `ProfileManager.cs`, `PatternTemplate.cs` |
| `BuildRing1()` speist Ring 1 aus Dokumenttypen, nicht aus Profilen | ✅ `ManualCaptureView.xaml.cs:326/349` → `_controller.BuildRing1(...)` |
| `RecognitionProfile` trägt `DocumentTypeId` + redundanten `DocumentTypeName` | ✅ `RecognitionProfile.cs:27` |
| `ManualFirstCaptureService` bewusst profilunabhängig | ✅ `ManualFirstCaptureService.cs:16` — Kommentar wörtlich „Bewusst PROFIL-UNABHAENGIG" |

**Zusatzbefund (stützt ChatGPT §15):** `PatternTemplateService.cs:135` gleicht
Templates über `DocumentTypeName` ab („Updates if same DocumentTypeName already
exists") — ein namensbasierter Abgleich, also genau die Drift-Quelle, vor der
ChatGPT warnt. Ein weiteres Argument, `DocumentTypeName` beim nächsten
Profil-Schema-Bump zu entfernen und auf IDs umzustellen.

## Übernommen (beide Schärfungen)

1. **Veto-Regel für Backoff.** ChatGPT hat recht — mein r2-Vorschlag („Familie schlägt
   trotzdem vor, lokale Abweichung nur anzeigen") war zu aggressiv. Neue
   Architekturregel, jetzt festgezogen: **positive Vorschlagsschwelle ≠ Veto-Schwelle.**
   Die prioritäre Ebene (Projekt) kann unterhalb ihrer positiven Schwelle bereits
   Fallback-Vorschläge tieferer Prioritäts-Ebenen blockieren (Startpunkt: n ≥ 2 lokal,
   100 % widersprüchlich → L2b-Wertvorschlag blockiert + Hinweis „Abweichende
   Namenskonvention erkannt"). Exakter Veto-Wert = Kalibrierung bei Umsetzung.
   Ohne diese Regel würde die Vorrangordnung Projekt > Familie in der Few-Shot-Phase
   unterlaufen — der Einwand ist stichhaltig.

2. **`ExactToken`-Grenzen aus `TokenizationConfig`.** Meine hartcodierte
   Delimiter-Klasse `[_\-\s.]` hätte eine zweite Token-Wahrheit neben dem
   `FileNameParser` geschaffen — exakt der Fehlertyp aus ADR-010/BPM-082 und ADR-061.
   Invariante übernommen: **Mining und Runtime-Recognition verwenden dieselbe
   Token-Grenzsemantik wie `FileNameParser`** (Boundary-Pattern generiert aus der
   Tokenization-Konfiguration des Profils, regex-escaped).

## Zum Dokumenttyp-Modell (§7–18) — mitgetragen

ChatGPTs Trennung „UI zusammenführen: ja / Domänenobjekte zusammenwerfen: nein" ist
die richtige Linie und liegt exakt auf ADR-061 (`DocumentTypeId` führend):

- **`document_types` = fachliches Hauptobjekt** (Ring 1, Zielpfad, Lern-Evidenz,
  bestätigte Dokumente); **`RecognitionProfile` = optionale Erkennungs-Konfiguration
  (0..1 pro Typ)**. Merkformel: Dokumenttyp = „was ist das fachlich?", Profil =
  „wie erkenne ich es?".
- **Kein leeres Profil-JSON bei Ring-1-„+ Neu"** — konsistent mit heutiger
  `ProfileManager`-Validierung (`Recognition.Count == 0` = ungültig). Profil-Tab wird
  View über `document_types` + optionales Profil („Nicht angelernt / Lernend / Aktiv").
- **Löschen asymmetrisch:** Profil löschen → nur Erkennung weg, Typ + Ring 1 + Dokumente
  bleiben. Konsistent mit Soft-Delete-Policy (ADR-050/056).
- **„+ Neues Profil" = „Erkennung für Dokumenttyp einrichten"** — verhindert
  Doppel-Namensanlage (Polierplan vs. „Polierpläne").
- **L2a = `ProjectId + DocumentTypeId`** statt `ProjectId + ProfileId` — zwingende
  Konsequenz: die Erfassung ist profilunabhängig, Evidenz entsteht vor dem Profil.
  Das „Lernen vor dem ersten Profil"-Modell (10 manuelle Zuordnungen → „Erkennung für
  ‚Polierplan' anlernen?") ist das natürlichste Onboarding für das ganze Lernsystem —
  der Wizard wird vom Pflicht-Einstieg zur Bestätigungs-Geste.
- **Kardinalität 0..1 beibehalten**, Mehrquellen-Varianten = YAGNI (deckt sich mit r1).

Antwort auf ChatGPTs Rückfragen aus meiner Sicht: **2× ja** — vorbehaltlich Herberts
Bestätigung, da beide sein UI-Konzept betreffen (Entscheidungspunkte Stufe A).

## Bewertung „Bei Umsetzung festzuziehen" (§19)

Vollständig und richtig geschnitten — alles Kalibrierungs-/Detailfragen, keine
Architektur-Blocker. Die Liste gehört 1:1 in den späteren ADR als „offene
Umsetzungspunkte".

## Empfehlung

Beide Rückfragen bejahen, Serie abschließen: kurze Schlussnachricht an ChatGPT
(Antwort auf die 2 Rückfragen + beidseitiges Sign-off festhalten), README
finalisieren, INDEX auf Abgeschlossen. Ergebnis-Vehikel: neues ADR „Lernende
Planerkennung / Evidenz-Scoping" erst bei Umsetzungsbeginn (alles post-V1); das
Dokumenttyp-Zielbild (Profil-Tab als View, kein leeres Profil-JSON) sollte als
Zielbild-Notiz festgehalten werden, damit BPM-080.05/Profil-UI-Arbeiten nichts
Gegenläufiges bauen.
