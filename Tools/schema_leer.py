"""Gibt das leere Schema als JSON aus (Reiter „Daten“ der Seitenliste). Baut immer frische Datenbanken, die echten werden
nie gelesen.

BPM: Das Schema von bpm.db und planmanager.db steht in Docs/Mockups/schema-bpm.sql und schema-planmanager.sql. Beide
Dateien schreibt der Test SchemaSnapshotTests aus frisch angelegten Datenbanken der App; ändert sich das Schema, schlägt
er fehl, bis die Dateien neu sind. Tabellen aus planmanager.db, deren Name es in bpm.db schon gibt, heißen hier
`planmanager.<name>`.
"""

from __future__ import annotations

import hashlib
import json
import sqlite3
import tempfile
from pathlib import Path

WURZEL = Path(__file__).resolve().parent.parent
QUELLEN = [("bpm", "Docs/Mockups/schema-bpm.sql"), ("planmanager", "Docs/Mockups/schema-planmanager.sql")]


def lesen(sql: str, pfad: Path) -> dict:
    c = sqlite3.connect(pfad)
    c.executescript(sql)
    tabellen = {}
    for (n,) in c.execute("select name from sqlite_master where type='table' and name not like 'sqlite_%'").fetchall():
        felder = [[r[1], r[2] or "", bool(r[5])] for r in c.execute(f'pragma table_info("{n}")')]
        fk = sorted({r[2] for r in c.execute(f'pragma foreign_key_list("{n}")')})
        tabellen[n] = {"felder": felder, "fk": fk}
    c.close()
    return tabellen


def main() -> None:
    alle: dict = {}
    h = hashlib.sha1(usedforsecurity=False)
    with tempfile.TemporaryDirectory() as ordner:
        for db, datei in QUELLEN:
            sql = (WURZEL / datei).read_text(encoding="utf8")
            h.update(sql.encode("utf8"))
            tabellen = lesen(sql, Path(ordner) / f"{db}.db")
            umbenannt = {n: f"{db}.{n}" for n in tabellen if n in alle}
            for n, t in tabellen.items():
                t["fk"] = sorted(umbenannt.get(z, z) for z in t["fk"])
                alle[umbenannt.get(n, n)] = t
    print(json.dumps({"version": f"bpm.db + planmanager.db, Stand {h.hexdigest()[:8]}", "tabellen": alle}, ensure_ascii=False))


if __name__ == "__main__":
    main()
