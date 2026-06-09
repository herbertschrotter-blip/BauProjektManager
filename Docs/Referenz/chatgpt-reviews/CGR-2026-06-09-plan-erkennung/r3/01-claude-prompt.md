## Gesprächsformat
Antwort komplett in den Canvas. CANVAS-TITEL: "Review Runde 3". Sprich zu Claude, nicht zum User. Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

## Repo-Zugriff
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch, NICHT `main`.
- **Lies dir das bestehende Mockup an:** `Docs/Mockups/PlanManager/02_Projektdetail/02_ManuellSortieren.html` (das ist die Radial-UI, um die es geht). Außerdem `Docs/Module/PlanManager.md`, `Docs/Kern/DB-SCHEMA.md` Kap. 6.7.

## Stand nach Runde 2 (Strategie steht)
Einig: **MVP = Strategie B als Kern (manuelle Erstaufnahme + deterministisches Dubletten-/Revisions-Matching), A nur Assist.** V1-MUSS: Feldkey-Fix, manuelle Erstaufnahme-Workflow, document_key aus Stammdaten-IDs, plan_documents/revisions/files, MD5-Dublette, Lightweight PlanNr/Index-Kandidat, Update-Vorschlag gegen bekanntes Dokument, Supersede/Journal. Post-V1: Regex-FieldExtraction, Alias, OCR.

Offen war nur die **V1-UI**. Du hattest für Bulk einen Tabellen-/Multi-Edit-Editor empfohlen und vor der starren Matrix gewarnt. **Entscheidung des Auftraggebers: die V1-UI wird das Radial-/Marking-Menü** (er hat es bereits als Mockup gebaut). Diese Runde: das Radial als primäre Erfassungs-UI für B realistisch unter Druck setzen — nicht ob, sondern wie es tragfähig wird.

## Die Radial-UI (bestehendes Mockup `02_ManuellSortieren.html` + gewünschter Zielstil)
- Liste der Eingang-/unsortierten Dateien; das **aktive Item** (1..n markierte Dateien) ist Bezugspunkt.
- Um das Item ein **Radial-Menü** mit Segmenten je Plantyp; **Halten/Hover** eines Segments → es klappt **nach außen in einen weiteren Ring/Fächer** auf (kaskadierend, jede Ebene als nächster Ring/Sub-Fächer).
- **Gewünschter Zielstil (Referenz vom Auftraggeber):** ein **mehrringiges Spiral-/Nautilus-Radial** — konzentrische Ringe, das aktive Segment expandiert nach außen in einen Fächer mit den Unterelementen (vgl. Launcher-Radials mit „Menu 1 → Menu 2 → Items"). **Keine losen Kacheln** — alles in der durchgehenden Radial-/Ring-Geometrie. Zentrum = kleiner Hub mit Aktionen (Hinzufügen/Bearbeiten/Entfernen/Einstellungen/Verschieben). Anpassung an BPM Dark Theme.

## Wichtige Mengen-Realität (entschärft die Skalierungs-Sorge teilweise)
Die **erste Ebene (Plantyp) ist beschränkt** — der Auftraggeber nennt aus jahrelanger Praxis ~**7 feste Plantypen**: Polierplan, Bewehrungspläne, Schalungspläne, Fertigteilpläne, Doka-Schalungspläne, Leica-Vermessungspläne, Protokolle. Viel mehr wird die erste Ebene nicht. → Plantyp passt gut auf einen Radial-Ring. **Die wirklich variable Dimension sind die Bauteile** (Haus 64/66/68 … können viele werden) — sie liegen in der Kaskade tiefer.

## Mapping auf Strategie B (das ist neu)
Bei B muss das Radial **fachliche Identität** vergeben, nicht nur einen Ordner. Pro Datei(en) braucht es:
```
building_part_id  (Bauteil, aus Projekt-Stammdaten, Pflicht für die meisten Pläne)
document_type_id  (Plantyp)
building_level_id (Geschoss, optional)
→ daraus document_key (ID-basiert) + Zielordner = building_parts.name (kanonisch)
```
Idee des Auftraggebers für die Kaskade: zuerst **Plantyp**, dann **Bauteil**, dann **Geschoss** — jede Ebene als nächstes Feld neben der vorigen. Bauteile kommen aus der DB (Projekt-Stammdaten), „+ Bauteil" führt direkt in die Projekt-Einstellungen.

## Aufgabe (Runde 3) — Radial-UI für B realistisch machen
1. **Tragfähigkeit als primäre V1-UI:** Kann das mehrringige Spiral-Radial die primäre Erfassungs-UI für B sein? Erste Ebene Plantyp ist mit ~7 fix gut beherrschbar — der kritische Punkt ist die **variable Bauteil-Ebene**: wie löst das Radial viele Bauteile (10, 20, 30+)? Expandierender Außen-Fächer mit Scroll? Such-/Filterfeld im Ring? Ab welcher Bauteil-Anzahl kippt das Radial und ein Listen-Fallback ist nötig?
2. **Dimensions-Reihenfolge + Kaskade:** Plantyp (bounded ~7) zuerst auf dem inneren Ring, dann Bauteil (variabel) als nächster Ring/Fächer, dann Geschoss (optional)? Oder Bauteil zuerst? Welche Reihenfolge minimiert Klicks/Fehlgriffe in der Praxis? Wie viele Kaskadenringe sind bedienbar, bevor es kippt?
3. **Bulk:** Mehrere Dateien markieren → eine Radial-Geste → alle bekommen dieselbe Identität (Bauteil+Plantyp[+Geschoss]). Tragfähig? Wie viele Dateien realistisch pro Geste? Brauchen wir für 30–40 gemischte Dateien trotzdem einen Listen-/Multi-Select-Fallback parallel?
4. **Capture vs Update in EINER UI:** Wenn eine abgelegte Datei (per MD5/PlanNummer) ein bekanntes plan_document trifft, ist es kein Erstaufnahme-Fall, sondern „neuer Index/Dublette". Wie koexistieren in derselben Radial-UI: (a) Neuerfassung über Radial vs (b) automatischer Update-Vorschlag „neuer Index von Plan X — übernehmen?" Soll matched-files das Radial überspringen?
5. **PDF+DWG-Paare + Kombi:** Wie werden Paare (gleiche Identität, 2 Dateien) in der Radial-Geste behandelt — zusammen markieren → eine Revision mit 2 Dateien? Und Kombi-Pläne (mehrere Plantypen in einer Datei)?
6. **Fehlgriff/Undo:** Falscher Drop im Radial — wie sofort erkennbar + rückgängig (ohne Migration, normale Benutzeraktion)?

Sign-off-Ziel der Serie: Radial-UI als tragfähige V1-Erfassungs-UI für Strategie B definiert (inkl. Caps + wo ein Listen-Fallback nötig ist) → danach Strategie + UI gemeinsam signiert.
