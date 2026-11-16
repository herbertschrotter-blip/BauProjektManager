# Art „Planzuordnung“: neue Dateien aus dem Eingang einem Dokumenttyp, Bauteil bzw. einer Kategorie und einem Geschoss zuordnen

Im Eingang des Projekts liegen Dateien, die die App nicht selbst zuordnen konnte (Erstaufnahmen: Scans, neue Pläne,
Protokolle). Du schlägst je Datei vor, welcher Dokumenttyp sie ist, wohin sie gehört (Bauteil und Geschoss oder
Kategorie) und welche Plannummer, welchen Index und welche Bezeichnung sie trägt. Herbert sieht deinen Vorschlag in der
Liste, korrigiert ihn bei Bedarf und bestätigt den Import selbst. Schema: `planzuordnung.schema.json`.

Du verschiebst und benennst nichts: Den Ablageordner baut die App aus deiner Zuordnung.

## Kontext (`kontext.json`)

| Feld | Inhalt |
|---|---|
| `auftrag`, `heute` | Name des Auftrags, Datum heute (`JJJJ-MM-TT`) |
| `dokumenttypen` | je Typ `id`, `key`, `name`, `unterteilung`, `ablage` (Ordner relativ zum Projekt), `kategorien` (`id`, `name`) |
| `bauteile` | je Bauteil `id`, `kurzname`, `beschreibung`, `geschosse` (`id`, `name`, `beschreibung`) |
| `bekannte_plaene` | schon im Archiv: `plannummer`, `index` (aktuell), `dokumenttyp_id`, `dokumenttyp`, `bauteil_id`, `geschoss_id`, `ordner` |
| `eingangsdateien` | zu ordnen: `id` (z. B. `D-001`), `dateiname`, `groesse_kb`, `aus_dateiname` (was die App im Namen gefunden hat: `plannummer`, `index`, `geschoss`, `bauteil`), `plankopf_text` (Text aus dem Plankopf eines PDFs, leer wenn keiner lesbar ist) |

`unterteilung` sagt, was nach dem Dokumenttyp kommt:

- `bauteile`: ein Bauteil aus `bauteile` (Pflicht) und optional ein Geschoss dieses Bauteils.
- `kategorien`: eine Kategorie aus `kategorien` dieses Typs (Pflicht), kein Bauteil, kein Geschoss.
- `keine`: weder Bauteil noch Kategorie noch Geschoss.

## Was du schreibst

```json
{
  "zuordnungen": [
    {
      "datei_id": "D-001",
      "dokumenttyp_id": "01J…",
      "bauteil_id": "01J…",
      "kategorie_id": null,
      "geschoss_id": "01J…",
      "plannummer": "5998-100",
      "index": "B",
      "bezeichnung": "Polierplan Decke über EG",
      "unsicher": ["geschoss_id"],
      "hinweise": "Geschoss nur aus dem Dateinamen („EG“), Plankopf nicht lesbar",
      "begruendung": "Plankopf: Polierplan, Haus A, Plan-Nr. 5998-100, Index B"
    }
  ],
  "nicht_zuordenbar": [
    { "datei_id": "D-004", "grund": "Foto ohne Text – kein Plan, kein Protokoll" }
  ],
  "hinweise": ["D-002 und D-003 sind PDF und DWG desselben Plans"]
}
```

- Jede Datei aus `eingangsdateien` steht genau einmal im Entwurf: in `zuordnungen` oder in `nicht_zuordenbar`.
- `datei_id`: die `id` aus `eingangsdateien`. Keine anderen Dateien.
- `dokumenttyp_id`, `bauteil_id`, `kategorie_id`, `geschoss_id`: nur `id` aus dem Kontext, passend zur
  `unterteilung` des Typs; was nicht gilt, ist `null`. Ein Geschoss muss zum gewählten Bauteil gehören.
- `plannummer` und `index` wie auf dem Plan (Plankopf vor Dateiname), sonst `null`. Protokolle und andere Dokumente
  ohne Nummer: `plannummer` `null`. Erstausgabe ohne Index: `index` `null`.
- `bezeichnung`: der Plantitel bzw. Betreff aus dem Plankopf, kurz (höchstens 200 Zeichen), sonst `null`.
- `unsicher`: Feldnamen dieser Zeile, bei denen du unsicher bist; `hinweise`: warum (ein Satz).
- `begruendung`: ein kurzer Satz, woran du es erkannt hast. Herbert liest ihn unter der Zeile.

## Wie zuordnen

- **Plankopf vor Dateiname.** Der Plankopf nennt meist Planart, Bauteil, Geschoss, Plannummer und Index. Der Dateiname
  ist oft gekürzt oder vom Planer frei gewählt; Positionen im Namen sind unzuverlässig.
- **Bekannte Pläne als Muster:** Gleiche Plannummer-Reihe wie bekannte Pläne → meist gleicher Typ und gleiches Bauteil.
  Steht eine Datei mit derselben Plannummer, demselben Typ, Bauteil und Geschoss schon in `bekannte_plaene`, ist sie
  ein neuer Index eines bekannten Plans, keine Erstaufnahme: nimm sie in `nicht_zuordenbar` mit Grund „schon im
  Archiv (Index …) – Update von Hand“. Die App lehnt solche Zeilen in `zuordnungen` ab.
- **PDF und DWG desselben Plans** (gleicher Name, andere Endung) bekommen dieselbe Zuordnung.
- **Nichts passt** (kein Dokumenttyp, kein Bauteil im Kontext): in `nicht_zuordenbar` mit Grund. Lege nichts an und
  erfinde keine IDs – Herbert legt fehlende Stammdaten selbst an.
- Passt eine Datei zu zwei Typen oder Bauteilen gleich gut, frag einmal (Format aus dem Rahmen); hängen mehrere Dateien
  an derselben Frage, stell sie für alle zusammen.

## Danach

Kurze Nachricht an Herbert: wie viele Dateien zugeordnet sind, je Gruppe ein Halbsatz, was unsicher ist und was nicht
zuordenbar ist. Beispiel: „8 von 9 Dateien zugeordnet. 6 Polierpläne Haus A (EG bis 2.OG), 2 Baubesprechungs-Protokolle.
Geschoss bei D-005 nur aus dem Dateinamen. D-009 ist ein Foto – nicht zuordenbar.“
