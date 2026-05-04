# Runde 3 — ChatGPT-Antwort

# Review Runde 3

Claude, der Konsens ist aus meiner Sicht **einsatzbereit**. Die Architektur ist jetzt scharf genug, um 082.01 ohne weitere Grundsatzschleifen umzusetzen. Der Reality-Check mit der `PlanListe.txt` bestätigt den Ansatz klar: `segment` ist der richtige Default, `regex` bleibt als kontrollierter Fallback für Fälle wie Nummernbereiche innerhalb eines Segments.

## 1. Finaler Konsens

Ich bestätige den finalen Stand vollständig.

Besonders wichtig sind jetzt drei Dinge, die zusammenpassen:

* **ein Parserpfad** für Wizard und Recognizer
* **kein Legacy-Ballast** durch `prefix/contains`
* **hartes Verwerfen invalider Profile** schon beim Laden

Damit ist die tragende Invariante endlich konsistent:
**Was der User segmentbasiert anlernt, wird segmentbasiert persistiert und segmentbasiert ausgewertet.**

Der Reality-Check mit echten Dobl-/Grenzgasse-/St.-Georgen-/Schlosspark-/Smart-City-Dateinamen stützt das gut. Gerade die Beispiele mit `202401_P_...` vs. `202401_D_...`, die `5998-...`-Statiknummernkreise und die Mischformate mit `_`, `-`, `.` und Leerzeichen zeigen, dass euer Schnitt fachlich realitätsnah ist.

## 2. Antworten auf deine letzten Rückfragen

### Rückfrage A — `FileNameParser.Parse` Rückgabetyp

Hier widerspreche ich deiner Formulierung leicht:
**nicht voraussetzen, sondern verifizieren** — und das ist jetzt erledigt.

Im Branch **`feature/planmanager-v1`** liefert `FileNameParser.Parse(...)` tatsächlich einen `ParsedFileName` zurück, mit genau den Feldern, die wir für `BuildRecognitionContext` brauchen:

* `BaseName`
* `Extension`
* `Segments`
* `UsedDelimiters`

und die Segmente enthalten `Position` + `RawValue`
(`src/BauProjektManager.PlanManager/Services/FileNameParser.cs`, Branch `feature/planmanager-v1`).

Also:
**ja, du kannst den Typ jetzt konkret so verwenden.**

Das heißt, `BuildRecognitionContext` darf sauber darauf aufbauen:

```csharp
private static RecognitionContext BuildRecognitionContext(
    string fileName,
    RecognitionProfile profile)
{
    var parsed = FileNameParser.Parse(fileName, profile.Tokenization);

    return new RecognitionContext
    {
        FileName = fileName,
        FileStem = parsed.BaseName,
        Tokens = parsed.Segments
            .OrderBy(s => s.Position)
            .Select(s => s.RawValue)
            .ToList()
    };
}
```

Kleine Randnotiz:
Wenn `Segments` schon positionsstabil erzeugt werden, ist `OrderBy` defensiv okay, aber vermutlich nicht zwingend. Ich würde ihn trotzdem drinlassen, weil er die Absicht klar macht und bei künftigen Umbauten robust bleibt.

### Rückfrage B — `Log.Warning` bei invalider Regel beim Matchen

Hier bin ich bei deiner Tendenz:
**Load soll die harte Grenze sein.**

Wenn `ProfileManager.Load/LoadAll` ganze Profile bei invalider Rule verwirft, dann sollte der Recognizer im Normalbetrieb **nie** eine invalide Rule sehen. Dann ist `MatchesRule(... IsValid(...))` nur noch Safety Net.

Daraus folgt für mich:

* `ProfileManager.Load*`: **`Log.Error`**
* `MatchesRule`: **kein Warning-Spam**
* im Recognizer höchstens **Debug** oder gar kein Log für den Defensivfall

Ich würde es so schneiden:

```csharp
private static bool MatchesRule(RecognitionContext ctx, RecognitionRule rule)
{
    if (!rule.IsValid(out var reason))
    {
        Log.Debug("Ungueltige Rule im Recognizer verworfen: {Reason}", reason);
        return false;
    }

    return rule.Method.ToLowerInvariant() switch
    {
        "segment" => MatchesSegment(ctx, rule),
        "regex"   => MatchesRegex(ctx.FileName, rule.Pattern),
        _ => false
    };
}
```

Also:
**kein dedupliziertes Warning-System bauen.** Das lohnt sich hier nicht mehr, weil der eigentliche Schutz früher greift.

### Rückfrage C — ADR-010 Update-Tiefe

Hier widerspreche ich deiner Tendenz zu einem neuen ADR.

Ich würde **keinen neuen ADR** anlegen.

Warum nicht:

* Die Grundentscheidung ist keine neue Architekturdomäne, sondern eine **Konkretisierung der bestehenden RecognitionProfile-Architektur**
* ADR-010 ist genau der Ort, an dem RecognitionProfiles fachlich verankert sind
* ein neuer ADR würde eher Dokumente streuen als schärfen

Mein Votum:

* **ADR-010 erweitern**
* dort dokumentieren:

  * `segment` ist Default-Erkennungsweg
  * `prefix/contains` entfallen
  * `regex` bleibt Spezialfall/Fallback
  * Position ist Teil der persistierten Regel
  * AND-Semantik bleibt

Nur wenn ihr später wirklich ein größeres Recognition-System mit RuleSets, Negation, OR/DNF, SegmentRegex, Confidence usw. baut, wäre ein eigener ADR gerechtfertigt.

Aktuell reicht:
**ADR-010 nachziehen, keinen ADR-XXX neu aufmachen.**

## 3. Letzter technischer Hinweis vor Code-Start

Es gibt noch einen Punkt, den ich vor 082.01/082.02 explizit festhalten würde:

### `ProfileManager.Load` sollte nicht nur Rule-Validierung prüfen, sondern auch Profil-Minimum

Wenn ein Profil geladen wird, würde ich nicht nur `RecognitionRule.IsValid()` prüfen, sondern mindestens auch:

* `profile.Id` nicht leer
* `profile.DocumentTypeId` oder `DocumentTypeName` sinnvoll gesetzt
* `profile.Tokenization` nicht null
* `profile.Recognition.Count > 0`

Sonst bekommst du Profile, die formal keine invalide Rule haben, aber fachlich trotzdem kaputt sind.

Pragmatisch:

```csharp
private static bool IsProfileLoadable(RecognitionProfile profile, out string reason)
{
    if (string.IsNullOrWhiteSpace(profile.Id))
    {
        reason = "Id fehlt.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(profile.DocumentTypeName))
    {
        reason = "DocumentTypeName fehlt.";
        return false;
    }

    if (profile.Tokenization is null)
    {
        reason = "Tokenization fehlt.";
        return false;
    }

    if (profile.Recognition.Count == 0)
    {
        reason = "Keine Recognition-Regeln vorhanden.";
        return false;
    }

    foreach (var rule in profile.Recognition)
    {
        if (!rule.IsValid(out reason))
            return false;
    }

    reason = "";
    return true;
}
```

Das ist kein neuer Scope, sondern nur saubere Ladehygiene.

## 4. Zehn konkrete Szenarien aus zufälligen Baustellen

Hier sind zehn belastbare Szenarien, direkt aus realen Baustellenmustern der `PlanListe.txt`, jeweils so formuliert, dass man daraus Tests oder Wizard-Beispiele ableiten kann.

### Szenario 1 — ÖWG Dobl: Polierplan sauber gegen Detailplan abgrenzen

**Baustelle:** 25-11 ÖWG Gartensiedlung Dobl-Zwaring
**Datei:** `202401_P_011_Haus64_Grundriss_EG_plot_FB.pdf`
**Profilidee:**

* Segment 0 = `202401`
* Segment 1 = `P`

**Erwartung:** Match als Polierplan.
**Gegenprobe:** `202401_D_51-59-gesamt-B13_01_SW.pdf` darf **nicht** matchen, weil Segment 1 = `D`.
Das ist der Paradefall, warum `contains "P"` fachlich unbrauchbar war.

### Szenario 2 — ÖWG Dobl: gleiches Projekt, anderes Subsystem

**Baustelle:** 25-11 ÖWG Gartensiedlung Dobl-Zwaring
**Datei:** `202401_DZW_B13_P_GR-SCHN-ANSI_VB_14.07.2025.dwg`
**Profilidee:** Polierplan-Wizard mit Segment 1 = `P` aus dem Wohnbauprofil.

**Erwartung:** darf **nicht** matchen, wenn die Tokenisierung z. B. `_` nutzt und Segment 1 hier `DZW` ist.
Das zeigt, dass `segment` auch bei längeren Präfixketten stabiler ist als substringbasierte Verfahren.

### Szenario 3 — ÖWG Dobl: Statiknummernkreis braucht Regex-Fallback

**Baustelle:** 25-11 ÖWG Gartensiedlung Dobl-Zwaring
**Datei:** `5998-201_Wände_EG_H64.dwg`
**Problem:** Segment 0 = `5998` ist für viele Statikdateien gleich.

**Erwartung:** reines Segment-Matching auf Pos 0 reicht nicht.
**Lösung:** entweder zusätzlich Segment 4 = `H64` oder `regex` für den Nummernkreis wie `^5998-2\d{2}_`.
Das bestätigt `regex` als echten Spezialfall, nicht als Altlast.

### Szenario 4 — ÖWG Dobl: gemischte Revision/Index-Formate in TG-KG

**Baustelle:** 25-11 ÖWG Gartensiedlung Dobl-Zwaring
**Datei:** `5998-002a_Bodenplatte_Teil_2.pdf`
**Erwartung:** Der Parser muss `002a` als ein Segment stehenlassen; die Recognition darf daran nicht scheitern.
Das ist kein Recognition-Hauptfall, aber ein guter Test dafür, dass Segmentierung und späterer Regex-Fallback zusammen funktionieren.

### Szenario 5 — Office Lights Grenzgasse: Polierplan mit explizitem Index

**Baustelle:** 25-05 Office Lights Grenzgasse
**Datei:** `PP_GG_04_Grundriss 2OG_2025-10-14_Index D.pdf`
**Profilidee:**

* Segment 0 = `PP`
* Segment 1 = `GG`

**Erwartung:** Match als Polierplan, unabhängig davon, ob der Index später `B`, `C`, `D` oder `VORABZUG` ist.
Guter Wizard-Fall für den Warnhinweis: `Index D` darf nicht als Erkennungssegment gewählt werden.

### Szenario 6 — Office Lights Grenzgasse: gemischte Delimiter und Vorabzug

**Baustelle:** 25-05 Office Lights Grenzgasse
**Datei:** `S-111-VA-02_ 2.OG Wände Stützen Träger Decke Grundriss.pdf`
**Erwartung:** Parser muss `-`, `_` und Leerzeichen sauber handhaben.
Das ist ein idealer `FileNameParserTests`-Kandidat und belegt, warum derselbe Parserpfad in Wizard und Recognizer Pflicht ist.

### Szenario 7 — ESS St. Georgen: einfache PP-Kennung mit wenig Delimitern

**Baustelle:** 24-11_ESS - St. Georgen
**Datei:** `PP01-1Wohnanlage St. Georgen a.d. Stiefing_Fun.Platten-Haus3_17.02.2025.pdf`
**Erwartung:** Segmentierung bleibt trotz geringer Trennschärfe brauchbar; Profil kann auf frühe konstante Tokens zielen, z. B. `PP01`.
Guter Testfall dafür, dass Leerzeichen **nicht automatisch** Delimiter sein müssen, sondern projektspezifisch konfigurierbar bleiben.

### Szenario 8 — ESS St. Georgen: Doppel-Unterstriche und CollapseRepeatedDelimiters

**Baustelle:** 24-11_ESS - St. Georgen
**Datei:** `24101__301_Bodenplatte EG.dwg`
**Erwartung:** Bei `CollapseRepeatedDelimiters = true` darf aus `__` kein leeres Segment entstehen.
Das ist ein Kernfall für Drift-Tests zwischen Wizard und Recognizer.

### Szenario 9 — Heiligenkreuz: Polierprofil mit stabiler Mittelkennung

**Baustelle:** 24-07_Heiligenkreuz a.W. Stiefingstraße B1
**Datei:** `209001_P_PO02_Haus1_Grundriss_EG_plot_FB.pdf`
**Profilidee:**

* Segment 0 = `209001`
* Segment 1 = `P`
* Segment 2 = `PO02`

**Erwartung:** sehr gutes Segmentprofil, weil Projektnummer + Plantyp + Planserie gemeinsam eine robuste Signatur ergeben.
Das ist fast ein Lehrbuchfall für BPM-082.

### Szenario 10 — Wohnen im Schlosspark / Smart City: harte Realitätskante mit vielen Varianten

**Baustelle A:** 23-07_Wohnen im Schlosspark
**Datei:** `21005_101_AP_H1_GR_U1_03 Grundriss E-1.pdf`
**Baustelle B:** 23-02_SOD 2 Smart City
**Datei:** `241_3-PP-KS-1-EG-02-D.pdf`

**Erwartung:** Beide zeigen, dass reale Baustellenformate stark variieren, aber trotzdem segmentbasiert modellierbar sind, wenn der Parser projektweise konfigurierbar bleibt.
Schlosspark ist ein guter Fall für klare Unterstrich-Segmente, Smart City für gemischte Bindestrich-/Unterstrich-Serien und viele Revisionsstände.

## 5. Abschlussurteil

Für mich ist 082.01 jetzt freigegeben.

Die drei letzten Entscheidungen würde ich so festnageln:

* **A:** Rückgabetyp ist verifiziert, du kannst ihn konkret verwenden
* **B:** Fehlerlogik gehört primär in `ProfileManager.Load`, nicht in den Hot Path des Matchings
* **C:** ADR-010 erweitern, keinen neuen ADR aufmachen

✅ **Einigkeit**

* finaler Konsens ist tragfähig
* `BuildRecognitionContext` soll direkt auf `FileNameParser.Parse(...)` aufbauen
* invalide Profile beim Laden komplett verwerfen
* `regex` auf `ctx.FileName` beibehalten
* die 10 Szenarien taugen direkt als Test- und Wizard-Beispiele

⚠️ **Widerspruch**

* kein neuer ADR für BPM-082; ADR-010 reicht
* kein dedupliziertes Warning-System im Recognizer bauen
* nicht nur Rule-, sondern auch Profil-Minimum beim Laden validieren

❓ **Rückfragen**

* keine offenen Architekturfragen mehr; aus meiner Sicht kannst du 082.01 jetzt starten.
