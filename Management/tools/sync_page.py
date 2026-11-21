#!/usr/bin/env python3
"""Write Management/data/project.js for the management page, from the project itself.

    python Management/tools/sync_page.py

Runs from the Stop hook after every chat turn; running it by hand is fine too.
Feature list, violations and approved exceptions come from validate.py; the page shows
each feature's root CLAUDE.md and its .cs files by layer. Nothing here is written by an AI.
On error the old project.js is kept and the exit code is 2.
"""
import json
import os
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))      # `python -I` leaves the script folder out
import validate  # noqa: E402

LAYERS = {"Contracts": "Scripts/Contracts", "Internal": "Scripts/Internal", "View": "Scripts/View", "Tests": "Tests"}


def build(root, cfg):
    result = validate.validate(root, cfg)
    features = []
    for fid in result["features"]:
        base = Path(root) / "UnityProject" / "Assets" / "Features" / fid
        doc = base / "CLAUDE.md"
        files = {}
        for name, rel in LAYERS.items():
            folder = base / rel
            files[name] = sorted(p.relative_to(folder).as_posix() for p in folder.rglob("*.cs")) if folder.is_dir() else []
        features.append({
            "id": fid,
            "kind": "service" if fid.startswith("_Services/") else "feature",
            "claude": doc.read_text(encoding="utf-8-sig") if doc.is_file() else "",
            "files": files,
            "violations": [v for v in result["violations"] if v["owner"] == fid],
            "excepted": [v for v in result["excepted"] if v["owner"] == fid],
        })
    others = [v for v in result["violations"] if v["owner"] not in result["features"]]
    return {"generatedAt": result["generatedAt"], "features": features, "otherViolations": others}


def main():
    root = HERE.parent.parent
    try:
        cfg = json.loads((HERE / "rules.json").read_text(encoding="utf-8-sig"))
        data = build(root, cfg)
        out = root / "Management" / "data" / "project.js"
        tmp = out.with_suffix(".js.tmp")
        tmp.write_text("// Sinh tự động bởi Management/tools/sync_page.py. Không sửa tay.\n"
                       "window.CORESIMPLE_PROJECT = " + json.dumps(data, ensure_ascii=False, indent=1) + ";\n",
                       encoding="utf-8")
        os.replace(tmp, out)
    except Exception as ex:   # keep the old file; never write half a page
        print(f"sync_page.py failed: {type(ex).__name__}: {ex}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
