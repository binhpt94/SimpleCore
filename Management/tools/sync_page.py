#!/usr/bin/env python3
"""Write Management/data/project.js for the management page, from the project itself.

    python Management/tools/sync_page.py

Runs from the Stop hook after every chat turn; running it by hand is fine too.
- Features, violations and approved exceptions: validate.py.
- Each feature: root CLAUDE.md, .cs files by layer, asset ids it declares (SRC-05 pattern).
- Each scene in _Composition: its Boot variant, installers in boot order, asset catalogs (kind, id -> asset).
- Reports for @Manager in Management/reports (written by .claude/hooks/stop_report.py), newest first.
- Management/data/gamedesign.js: the game-design trees (live and idea), read by gamedesign.py.
- Management/data/studio.js: the studio orders, their runs and quality, the art debts (studio.py).
Unity YAML is read as text. Nothing here is written by an AI.
On error the old project.js is kept and the exit code is 2.
"""
import json
import os
import re
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))      # `python -I` leaves the script folder out
import gamedesign  # noqa: E402
import validate  # noqa: E402

LAYERS = {"Contracts": "Scripts/Contracts", "Internal": "Scripts/Internal", "View": "Scripts/View", "Tests": "Tests"}
REF_RX = re.compile(r"guid:\s*([0-9a-f]{32})")
MOD_RX = re.compile(r"propertyPath:\s*'?(\w+)\.Array\.(size|data\[(\d+)\])'?\s*\n\s*value:\s*(\S*)\s*\n\s*objectReference:\s*\{([^}]*)\}")
ENTRY_RX = re.compile(r"-\s*id:\s*(.*)\n\s*asset:\s*\{([^}]*)\}")


def guid_map(assets):
    """GUID -> path relative to UnityProject (Assets/...)."""
    out = {}
    for base, _, files in os.walk(assets):
        for name in files:
            if name.endswith(".meta"):
                m = validate.META_GUID_RX.search(validate.read_text(Path(base) / name))
                if m:
                    out[m.group(1)] = (Path(base) / name[:-5]).relative_to(assets.parent).as_posix()
    return out


def ref_path(ref_text, guids):
    m = REF_RX.search(ref_text)
    return guids.get(m.group(1)) if m else None


def owner_of(path, feature_ids):
    """Feature id that owns an Assets/... path, or None."""
    parts = path.split("/")
    if len(parts) > 3 and parts[1] == "Features":
        fid = "_Services/" + parts[3] if parts[2] == "_Services" and len(parts) > 4 else parts[2]
        return fid if fid in feature_ids else None
    return None


def prefab_lists(path, guids, root):
    """installers / catalogs of a Boot prefab: the base prefab's lists, then a variant's overrides on top."""
    text = validate.read_text(root / path)
    lists = {"installers": [], "catalogs": []}
    src = re.search(r"m_SourcePrefab:\s*\{[^}]*guid:\s*([0-9a-f]{32})", text)
    if src and guids.get(src.group(1), "").endswith(".prefab"):
        lists = prefab_lists(guids[src.group(1)], guids, root)
    else:
        for key in lists:
            m = re.search(rf"^\s*{key}:\s*\n((?:\s*-\s*\{{[^}}]*\}}\s*\n)*)", text, re.M)
            if m:
                lists[key] = [ref_path(line, guids) for line in m.group(1).strip().splitlines()]
    for key, kind, index, value, ref in MOD_RX.findall(text):
        if key not in lists:
            continue
        if kind == "size":
            n = int(value or 0)
            lists[key] = (lists[key] + [None] * n)[:n]
        else:
            i = int(index)
            while len(lists[key]) <= i:
                lists[key].append(None)
            lists[key][i] = ref_path(ref, guids)
    return lists


def read_catalog(path, guids, root, feature_ids):
    text = validate.read_text(root / path)
    kind = re.search(r"^\s*kind:\s*(.*)$", text, re.M)
    entries = []
    for id_text, ref in ENTRY_RX.findall(text):
        asset = ref_path(ref, guids)
        entries.append({"id": id_text.strip().strip("'\""), "asset": asset,
                        "owner": owner_of(asset, feature_ids) if asset else None})
    return {"path": path, "kind": kind.group(1).strip() if kind else "", "entries": entries}


def scenes(root, guids, feature_ids):
    comp = root / "Assets" / "_Composition"
    build_file = root / "ProjectSettings" / "EditorBuildSettings.asset"
    order = re.findall(r"path:\s*(\S+\.unity)", validate.read_text(build_file)) if build_file.is_file() else []
    rank = lambda p: (order.index(p) if p in order else len(order), p)   # build order first, the rest by name
    out = []
    for scene in sorted(comp.glob("*.unity"), key=lambda s: rank(s.relative_to(root).as_posix())):
        text = validate.read_text(scene)
        boot = None
        for g in re.findall(r"m_SourcePrefab:\s*\{[^}]*guid:\s*([0-9a-f]{32})", text):
            p = guids.get(g, "")
            if p.startswith("Assets/_Composition/Boot") and p.endswith(".prefab"):
                boot = p
        item = {"name": scene.stem, "path": scene.relative_to(root).as_posix(), "boot": boot,
                "installers": [], "catalogs": []}
        if boot:
            lists = prefab_lists(boot, guids, root)
            item["installers"] = [{"asset": p, "feature": owner_of(p, feature_ids) if p else None}
                                  for p in lists["installers"]]
            item["catalogs"] = [read_catalog(p, guids, root, feature_ids) if p else {"path": None, "kind": "", "entries": []}
                                for p in lists["catalogs"]]
        out.append(item)
    return out


def asset_ids(base):
    """Asset ids created in a feature's Contracts/Internal: [{type, value, file, line}]."""
    out = []
    for layer in ("Contracts", "Internal"):
        folder = base / "Scripts" / layer
        if not folder.is_dir():
            continue
        for c in sorted(folder.rglob("*.cs")):
            code = validate.strip_cs(validate.read_text(c).replace("\r", ""), keep_strings=True)
            for m in validate.ID_NEW_RX.finditer(code):
                arg = m.group("arg") if m.group("type") else m.group("targ")
                lit = validate.ID_LITERAL_RX.match(arg)
                if lit:
                    out.append({"type": m.group("type") or m.group("ttype"), "value": lit.group(1),
                                "file": f"{layer}/{c.relative_to(folder).as_posix()}", "line": validate.line_of(code, m.start())})
    return out


REPORT_FIELDS = ("Phiên", "Việc", "Trạng thái", "Đã đổi", "Công khai đổi", "Cần bên khác", "Kiểm tra", "Ghi chú")
MAX_REPORTS = 50


def reports(root):
    """Reports written by .claude/hooks/stop_report.py, newest first."""
    folder = Path(root) / "Management" / "reports"
    out = []
    for p in sorted(folder.glob("*.md"), reverse=True)[:MAX_REPORTS] if folder.is_dir() else []:
        text = validate.read_text(p)
        fields = {}
        for name in REPORT_FIELDS:
            m = re.search(rf"^-\s*{name}:\s*(.*)$", text, re.M)
            fields[name] = m.group(1).strip() if m else ""
        stamp = re.match(r"(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})", p.name)
        when = "{}-{}-{} {}:{}:{}".format(*stamp.groups()) if stamp else ""
        out.append({"file": p.name, "time": when, "fields": fields,
                    "text": re.sub(r"^<!--.*?-->\n", "", text, flags=re.S)})
    return out


def build(root, cfg):
    result = validate.validate(root, cfg)
    unity = Path(root) / "UnityProject"
    ids = result["features"]
    guids = guid_map(unity / "Assets")
    scene_list = scenes(unity, guids, set(ids))

    features = []
    for fid in ids:
        base = unity / "Assets" / "Features" / fid
        doc = base / "CLAUDE.md"
        files = {}
        for name, rel in LAYERS.items():
            folder = base / rel
            files[name] = sorted(p.relative_to(folder).as_posix() for p in folder.rglob("*.cs")) if folder.is_dir() else []
        declared = asset_ids(base)
        for d in declared:
            d["scenes"] = [s["name"] for s in scene_list for c in s["catalogs"] for e in c["entries"] if e["id"] == d["value"]]
        features.append({
            "id": fid,
            "kind": "service" if fid.startswith("_Services/") else "feature",
            "claude": doc.read_text(encoding="utf-8-sig") if doc.is_file() else "",
            "files": files,
            "assetIds": declared,
            "scenes": [s["name"] for s in scene_list if any(i["feature"] == fid for i in s["installers"])],
            "violations": [v for v in result["violations"] if v["owner"] == fid],
            "excepted": [v for v in result["excepted"] if v["owner"] == fid],
        })
    others = [v for v in result["violations"] if v["owner"] not in ids]
    return {"generatedAt": result["generatedAt"], "features": features, "scenes": scene_list,
            "reports": reports(root), "otherViolations": others}


def write_js(out, name, data):
    """Writes `window.<name> = <data>;` whole or not at all."""
    tmp = out.with_suffix(".js.tmp")
    tmp.write_text("// Sinh tự động bởi Management/tools/sync_page.py. Không sửa tay.\n"
                   f"window.{name} = " + json.dumps(data, ensure_ascii=False, indent=1) + ";\n", encoding="utf-8")
    os.replace(tmp, out)


def write_gamedesign(root=None):
    """Writes Management/data/gamedesign.js from Management/game-design (live and idea) and returns the data.
    Also keeps the nodes' history (gamedesign.record_versions). It never changes live."""
    root = Path(root) if root else HERE.parent.parent
    gamedesign.record_versions(root)      # history: what changed in data.md since the last run
    data = gamedesign.snapshot(root)
    data["generatedAt"] = time.strftime("%Y-%m-%dT%H:%M:%S")
    write_js(root / "Management" / "data" / "gamedesign.js", "CORESIMPLE_GAMEDESIGN", data)
    return data


def write_studio(root=None):
    """Writes Management/data/studio.js (orders, runs, quality, art debts) for the Studio tab and returns it."""
    import studio      # here: studio imports gamedesign, which this module already has
    root = Path(root) if root else HERE.parent.parent
    data = studio.snapshot(root)
    write_js(root / "Management" / "data" / "studio.js", "CORESIMPLE_STUDIO", data)
    return data


def main():
    root = HERE.parent.parent
    try:      # its own file and its own try: a broken design tree must not stop the project data, nor the reverse
        write_gamedesign(root)
    except Exception as ex:
        print(f"sync_page.py: game design failed: {type(ex).__name__}: {ex}", file=sys.stderr)
    try:
        write_studio(root)
    except Exception as ex:
        print(f"sync_page.py: studio failed: {type(ex).__name__}: {ex}", file=sys.stderr)
    try:
        cfg = json.loads((HERE / "rules.json").read_text(encoding="utf-8-sig"))
        data = build(root, cfg)
        write_js(root / "Management" / "data" / "project.js", "CORESIMPLE_PROJECT", data)
    except Exception as ex:   # keep the old file; never write half a page
        print(f"sync_page.py failed: {type(ex).__name__}: {ex}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
