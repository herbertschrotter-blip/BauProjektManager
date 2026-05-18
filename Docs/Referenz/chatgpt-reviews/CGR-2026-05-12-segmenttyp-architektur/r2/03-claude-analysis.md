# Claude-Analyse — Review Runde 2

**Datum:** 2026-05-18

---

## Gesamtbewertung

ChatGPT Runde 2 ist **starkes Material zur direkten Umsetzung**. Die Architektur ist scharfgestellt, ein 3-Phasen-Implementierungsplan mit 3 konkreten Commit-Messages liegt vor. Nur 4 kleine Klärungspunkte sind offen.

## Wichtige Korrekturen/Ergänzungen von ChatGPT

| Punkt | Bewertung |
|---|---|
| **`user_modified_group` ergänzen** | Hatte ich nicht. Wichtig — sonst überschreibt App-Update User-Gruppen-Sortierung. Übernehme ich. |
| **`token_key` bei Built-ins unveränderlich** | Hatte ich nicht explizit. Logisch zwingend — sonst brechen `renameSchema`/`folderHierarchy`. Übernehme ich. |
| **`semantic_role` bei Built-ins read-only mit Warntext** | UX-Detail das ich übersehen hatte. „Geschoss" umbenannt zu „Akustik-Klasse" bleibt fachlich Spatial → Identity-Wirkung. Read-only-Anzeige im Manager nötig. |
| **PlanNumber genau einmal (`== 1`)** | Konkretisiert meinen Vorschlag. Sauber. |
| **identityFields nur tatsächlich verwendete Segmente** | Nicht alle aktiven Spatial-Typen, sondern nur Profilsegmente. Korrekt. |
| **`is_required` nur aus Rolle abgeleitet, nicht user-toggle** | Verhindert das Aushebeln der Pflichtlogik. |
| **`token_key` Custom stabil nach Anlage** | Bei Rename bleibt Token. Sonst brechen Templates. |
| **DevTool archivieren statt löschen** | Sicherer als Löschen (Rollback möglich). |

## Volle Zustimmung

- 3-Phasen-Plan A/B/C
- 3 erste Commits (Catalog Persistence → Profile v4 → Wizard Catalog)
- 13 Refactor-Stellen als Akzeptanzkriterien (deine 5 + 8 zusätzliche)
- Custom-Chip Inline-Schnellanlage (Variante 1)
- Keine Massenreparatur in Schritt 5
- Deaktivierte Typen verborgen + Kontextbanner
- Kein `lastKnownLabel` in Frühphase

## Meine Antworten zu ChatGPTs 4 Rückfragen

### 1. Spatial-Rollen für `objekt` und `achse`?

**Meine Empfehlung:** ChatGPTs konservativem Vorschlag folgen — beide als `Spatial`.

Begründung:
- `Achse` ist in 90% der Fälle räumlich (Achse A-1, A-2, ...). Identity-relevant.
- `Objekt` ist mehrdeutig (Gebäudeobjekt = räumlich; Wohnung 12 = räumlich; abstrakter Objekttyp = nicht-räumlich). Wenn User es nicht braucht, kann er es einfach nicht im Profil verwenden.
- Konservativer Default (more identity, fewer false positives) ist besser als zu wenig Identity.

Wenn User später Probleme hat: Spatial-Rolle bei einzelnem Built-in entfernen ist nachträglich möglich.

### 2. `token_key` unveränderlich nach Anlage (auch Custom-Rename)?

**Meine Empfehlung:** Ja. ChatGPT hat klare Empfehlung gegeben.

Begründung: Templates wie `{plan_number}-{plan_index}_{akustik_klasse}` würden brechen, wenn `token_key` mit Rename mitwandert. Name darf sich ändern, Token bleibt. Falls jemand wirklich den Token wechseln will: nicht in BPM-108, kann später als expliziter „Token umbenennen mit Profil-Update"-Flow kommen.

### 3. DevTool archivieren statt löschen?

**Meine Empfehlung:** Ja, archivieren.

Begründung:
- Frühphase ≠ Datenverlust riskieren
- Archiv-Ordner `_archiv/schema-reset-YYYYMMDD-HHMMSS/` ist klar erkennbar
- User kann später Reste manuell löschen
- Rollback bei Fehler möglich

### 4. PatternTemplateService aktiv weiterverwenden oder deaktivieren?

**Stand prüfen:** [PatternTemplate.cs](src/BauProjektManager.PlanManager/Services/PatternTemplate.cs) enthält `List<ProfileSegment> Segments` und `List<RecognitionRule> Recognition`. Templates spiegeln also Profile-Strukturen wider — bei Schema-v4-Umstieg sind sie genauso betroffen wie Profile.

**Meine Empfehlung:** Deaktivieren bis v4-Anpassung.

Konkret:
- `pattern-templates.json` wird beim Release in `_archiv/` verschoben (genau wie alte Profile)
- PatternTemplateService bleibt im Code, lädt aber nur v4-konforme Templates (schreibt v4-konform)
- Wenn keine v4-Templates da sind: leere Liste (UI zeigt „keine Vorschläge")
- Migration: User erstellt 1-2 Pattern-Templates neu im Wizard, die landen automatisch v4-konform

Das entspricht der Frühphasen-Logik: Reset statt Migration.

## Offene Punkte für User-Entscheidung

ChatGPTs 4 Rückfragen sind alle eher technisch und ich habe klare Empfehlungen. Die wirklich entscheidende Frage für den User ist jetzt nicht „wie", sondern „wann startet die Umsetzung".

**Mögliche Wege:**

A. **Review abschließen + BPM-108 ClickUp updaten, dann pausieren** — Architektur ist fertig validiert. BPM-108-Description bekommt finale Phasen A/B/C + 13 Akzeptanzkriterien + 3 Commits. Implementierung in späterer Session.

B. **Review abschließen + sofort mit Commit 1 (Phase A) starten** — Domain-Modelle + DB-Tabellen + Seed + Repository + Catalog-Service heute angehen.

C. **Runde 3 für letzte Klärung** — Falls User noch Zweifel oder offene Fragen hat (z.B. Mockup-Anpassungen für read-only Rollenanzeige, „+ Eigenes" Inline-Popover als Mockup vor Code).

## Mein Bauchgefühl

Architektur ist scharfgestellt. Eine weitere ChatGPT-Runde würde nur noch Detail-Polishing bringen. Sinnvoll wären jetzt eher zwei konkrete Schritte VOR Commit 1:

1. **BPM-108 ClickUp-Description finalisieren** (Phasen A/B/C, Akzeptanzkriterien, Commits) — 10 Min
2. **Mockups anpassen:** Manager-Dialog um read-only Rollenanzeige für Built-ins erweitern + Inline-Popover für „+ Eigenes" als neues Mockup — 20-30 Min
3. **Dann Commit 1** (Phase A Domain/Persistence)

Aber das ist meine Sicht — Entscheidung beim User.
