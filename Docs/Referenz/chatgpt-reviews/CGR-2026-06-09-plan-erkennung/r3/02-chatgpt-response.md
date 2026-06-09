# Review Runde 3 — ChatGPT-Antwort (GPT-5.4)

**Sign-off der Radial-UI für V1 unter Einschränkung:** Radial = primäre Erfassungs-Geste für Strategie B, aber NICHT die einzige Arbeitsfläche. Radial entscheidet schnell über Identität; Liste/Preview bleibt Kontroll-, Bulk- und Fallback-Fläche. Bestehendes Mockup passt zum Interaktionsmodell, muss aber von „Karten+losem Sub-Menü" zu echtem konzentrischem Ring-/Nautilus-Modell werden (zusammenhängende Segmente, außen expandierender Fächer, keine losen Rechtecke). UI-seitig anspruchsvoller, aber V1-tragfähig mit klaren Caps.

## 1. Tragfähigkeit V1-UI
Plantyp-Menge kein Problem (7 fix + „Mehr"). Kritisch = Bauteile: 10 ok, 20 grenzwertig, 30+ kippen ohne Such-/Listenmechanik.
**Caps:** Plantyp-Ring 6–8 ideal, 9 max, darüber „Mehr…". Bauteil: ≤8 direkt, 9–16 paged/rotierbar, 17–24 Suchfilter+Favoriten+„Alle…", ab 25 Listen-Fallback zwingend. Geschoss: ≤6 direkt, ab 7 Liste/Dropdown im Preview.
Radial darf NICHT 30 Bauteile geometrisch zeigen → stattdessen zuletzt verwendete / Dateiname-Kandidaten / Favoriten / „Suchen…" / „Alle…".
**Such-/Filterfeld im Ring:** kein echtes Textfeld (unterbricht Geste); stattdessen: Tippen bei offenem Ring startet Quick-Filter → Treffer als Außenring, Enter=erster Treffer, Esc=schließen. (Beispiel: halten → „Schalung" → Bauteil-Ring → „64" tippen → reduziert auf Haus 64/164 → bestätigen.)

## 2. Dimensions-Reihenfolge
**Plantyp → Bauteil → Geschoss(optional)** für V1. Plantyp zuerst, weil klein/stabil, bestimmt Zielordner-Hauptbereich, im Dateinamen besser erkennbar, mentales Modell „Was ist das? → Wohin?", filtert sinnvolle Bauteil-Optionen. „Bauteil fixieren"-Modus später möglich.
**Max. 3 Ringe in V1** (Zentrum=Datei+Aktionen, Ring1 Plantyp, Ring2 Bauteil, Ring3 Geschoss optional). PlanNumber/Index/Titel/Kombi/Zielpfad/PDF+DWG → ins Preview-Panel, NICHT radial.

## 3. Bulk
Multi-Select + eine Geste = gleiche Identität, nur für homogene Stapel. Caps: 1 Datei volle Erfassung; 2–8 Radial-Bulk ok (Preview „8 Dateien erhalten dieselbe Identität"); 9–20 nur mit Zusatzbestätigung; >20 Listen-/Gruppen-Fallback. Vorprüfung: Dateitypen kompatibel? PlanNumber-Kandidaten gleich/leer? Index gruppierbar? PDF+DWG plausibel? → ein Dokument oder mehrere? Bei gemischter Gruppe warnen. **Unterschied UI-explizit:** gleiche Identität = eine Revision mit mehreren Dateien; gleiche Kategorie = mehrere Dokumente mit eigenem PlanNumber-Kandidat.

## 4. Capture vs Update
Matched Files überspringen Radial standardmäßig, aber sichtbar. Phasen: Eingang analysieren → Buckets: A Dubletten, B Update-Vorschläge, C manuelle Erstaufnahme, D Konflikte. **Nur Bucket C öffnet Radial; A/B Aktionskarten; D Auswahldialog.** Update-Karte: „Neuer Index B für Polierplan·Haus 64·OG3·5998-203 [Übernehmen][Anderen wählen][Als neu]". Standardpfad: Update erkannt → übernehmen (nicht durchs Radial). Schema trägt das (plan_documents/plan_revisions current/superseded/files).

## 5. PDF+DWG + Kombi
Doku eindeutig: Revision = 1..n Dateien, Zusammenführung über Identity/Typ/Revisionsstand, nicht Dateiname-Stamm; Auto-Link nur bei gleichem document_key+document_type+Revisionsstand+erlaubter Extension-Kombi. V1: Multi-Select + Geste → eine gemeinsame Revision wenn PlanNumber+Index+Extension-Kombi plausibel + User bestätigt. Bei unterschiedlichen PlanNumbers → nicht als eine Revision, sondern „als mehrere Dokumente?". **Kombi-Pläne V1:** kein Auto-Split in mehrere plan_documents; stattdessen Plantyp „Kombiplan/Sonstiges" + Tags/Segmente, oder primärer Plantyp + Zusatzmarkierung. Aufspaltung nur manuell/ausdrücklich, nie automatisch.

## 6. Fehlgriff/Undo
Zwei Ebenen: (A) **vor Import** = Pending Assignment im Preview-State (`PendingManualAssignment`-Record), Undo/Entfernen/Bearbeiten/Zurück-in-Eingang — Datei wird noch NICHT verschoben. (B) **nach Import** = bestehendes Import-/Undo-Journal (Journal vor Dateioperationen). Datei verschwindet nach Geste nicht, sondern ändert sichtbar Status. Fehlgriff-Vermeidung: Zielsegment erst nach Haltezeit (250–350ms) aktiv, Breadcrumbs (Polierplan→Haus 64→OG3), Drop final erst bei Maus-Los, aktives Ziel groß, gefährliche Aktionen (Entfernen/Einstellungen) nicht im Sortier-Bewegungsbereich.

## V1-Radial-Spezifikation
Screen: links/mitte Eingangsliste (gruppiert: Updates/Erstaufnahme/Konflikte/Dubletten); rechts Preview-/Detailpanel (PlanNumber/Index-Kandidaten, Zielpfad, Assignment-Status, Bearbeiten/Zurücksetzen); über Item Radial bei Hold/Drag. Radial schreibt **bestätigte fachliche Identität** (document_type_id, building_part_id, building_level_id?, plan_number, plan_index?, document_key, target_relative_directory). **Zielordner aus Stammdaten (building_parts.name), nicht Alias/Dateiname.**

## Harte Caps für Sign-off
1. Max 3 Kaskadenringe. 2. Plantyp-Ring max 8 sichtbar. 3. Bauteil: ≤8 direkt / 9–16 paginiert / ≥17 Favoriten+Suche / ≥25 Listen-Fallback Pflicht. 4. Bulk: 2–8 direkt / 9–20 Zusatzbestätigung / >20 Fallback. 5. Matched Updates überspringen Radial. 6. Radial schreibt nur Pending Assignments. 7. Jeder Pending Assignment sichtbar/editierbar/rückgängig.

## Listen-Fallback (Sicherheitsnetz, nicht Konkurrenz)
Auslöser: >24 Bauteile, >20 Dateien, Suchfilter nötig, Konflikt mehrerer plan_documents, Kombi-Datei, fehlende PlanNumber bei planpflichtigem Typ, „Alle Bauteile…". Keine Excel-Tabelle nötig — Seitendialog „Manuelle Zuordnung bearbeiten" (Plantyp/Bauteil-Suchdropdown/Geschoss/Plannummer/Index + Modus: eine gemeinsame Revision / mehrere Dokumente).

## ✅ Einigkeit
Radial als V1-Primär-UI tragbar; Plantyp erster Ring (klein/stabil); Bauteil kritisch → Caps+Favoriten+Suche+Fallback; Geschoss optionaler 3. Ring/Detailfeld; matched Updates/Dubletten überspringen Radial; PDF+DWG via Multi-Select+Geste als eine Revision (wenn plausibel); Radial erzeugt nur Pending Assignments, finaler Import nach Preview; Undo vor+nach Import.

## ⚠️ Widerspruch
Kein unbegrenzt expandierendes Radial für 20/30+ Bauteile (ab ~17 Filter/Favoriten, ab ~25 Listen-Fallback); keine 4./5. Radialebene in V1; matched Updates nicht erneut durchs Neuerfassungs-Radial; Kombi nicht automatisch in mehrere plan_documents splitten; Mockup nicht 1:1 (echte Ring-/Fächer-Geometrie statt Kacheln+loses Sub-Menü).

## ❓ Rückfragen
1. Geschoss in V1 im Radial oder nur rechtes Preview-Panel?
2. Bauteil-Sortierung im Außenring: Stammdaten-Sort / zuletzt verwendet / numerisch-natural / kontextbasierter Vorschlag zuerst?
3. „+ Bauteil" im Radial: Inline-Dialog oder Wechsel in Projekt-Einstellungen?
4. Multi-Select PDF+DWG: standardmäßig „eine Revision" vorschlagen oder zuerst fragen?
5. Listen-Fallback als eigener Dialog „Zuordnung bearbeiten" oder dauerhaftes rechtes Detailpanel?
