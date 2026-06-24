# Review Runde 3 — Claude-Analyse

## Gesamturteil
**Final und implementierungsreif.** ChatGPT hat das Datenmodell sauber durchgezogen; ich trage alles mit. Kein offener Dissens mehr — nur drei kleine operative Rückfragen. Der Review ist aus meiner Sicht **abschlussreif → ADR + Slice 0**.

## Volle Zustimmung
1. **`root_relative_path` pro Dokumenttyp** ist der Schlüssel, der Protokolle (Root-Typ) UND die künftigen Roots (Leica/DOKA) sauber löst. `folder_name` leer bei Root-Typ, gefüllt bei Subordner-Typ — einfache, eindeutige Regel.
2. **`document_types.key` als persistierte Spalte** + `UNIQUE(project_id, key)` + Seed-Abbruch bei Key-Kollision — robuste Identität, entkoppelt von Name/Ordner.
3. **Präzedenz-Regel** (Container/Typ/beides nur explizit über `CreatesDocumentType`, keine implizite Ableitung) — verhindert genau die Doppeldeutigkeit, die ich befürchtet hatte.
4. **`ProjectPaths.Plans` bleibt Convenience, raus aus dem Resolver** — pragmatisch, vermeidet Settings-Destabilisierung.
5. **Kategorie-`HasPrefix` je Kategorie**, `building_levels.folder_name = "{PrefixString} {Name}"` — exakt deine Konventionen, rename-stabil.
6. **Slice 0.1–0.6** ist eine saubere, baubare Reihenfolge (Models → Schema → ProjectDatabase → Seed → Resolver → Import-Break).
7. **Benannte Lücke** (Ring2Source single-strategy, kombinierte Hierarchien Post-V1) — wichtig, gehört als explizite Grenze in den ADR. Genau richtig, das jetzt NICHT einzubauen.

## Meine Bewertung der 3 Rückfragen (mit Empfehlung)
1. **`key` bei User-Typen:** beim Anlegen einmal aus dem Namen erzeugen (normalisiert) + danach **gesperrt** (stabile Identität, wie bei den Built-ins). Anzeigename frei editierbar, Key nicht. Empfehlung: auto-generiert + gesperrt.
2. **Protokolle eigener `_Eingang`:** Für V1 **ein globaler Eingang** (`01 Planunterlagen/_Eingang`) — die Erfassung ordnet jeder Datei ihren Typ zu (auch Protokoll → `06 Protokolle/…`). Ein zweiter Eingang verdoppelt Scan/Recovery-Logik ohne klaren Mehrwert jetzt. Empfehlung: ein Eingang, später erweiterbar.
3. **Baustelleneinrichtung:** `01 Planunterlagen / Baustelleneinrichtung / datei.pdf` (Subordner-Typ, `None`, kein Präfix) — schon durch frühere Entscheidung gedeckt. Empfehlung: so lassen.

→ Alle drei sind risikoarm; ich würde sie als ADR-Festlegung übernehmen.

## Empfehlung
Review **abschließen**. Nächster Schritt: **ADR** (Vorschlag ADR-060) „Dateisystem-Ports + DB-als-Ordner-Wahrheit (root_relative_path) + DocumentTargetPathResolver + Slice-Plan", inkl. der Post-V1-Grenze (single Ring-2-Strategie). Danach ClickUp-Tasks je Slice + Slice 0.1 starten. Eine Runde 4 ist nicht nötig.
