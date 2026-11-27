#!/usr/bin/env python3
"""The art of the game, at three tiers (người quyết 2026-11-27):

    GreyBox   no file: the code draws a square (2D) or a box (3D); moves, turns, scales and tints only
    Simple    made by a worker: an SVG drawn into a PNG (`svg` below), a model from Blender run headless
    Detail    the production art, put in by the human

The game shows the best tier there is: Detail, else Simple, else GreyBox. The human may keep one art on Simple
although it has a Detail (the Studio tab of the page): that choice is the only thing anyone judges.

    python Management/tools/art.py list [<F>]                         every art with a Simple or a Detail (any chat)
    python Management/tools/art.py choose <F>/<id> simple|detail      the human (the page): which tier the game uses
    python Management/tools/art.py svg <in.svg> <out.png> <w>x<h>     a worker: draw a Simple picture

Layout (no Resources folder: SRC-04; the game reaches each file through the AssetCatalog of kind "Art" on the Boot,
wired by hand, ids <F>/<id>/Simple and <F>/<id>/Detail, and ArtChoice for the choice file):
    UnityProject/Assets/<feature folder>/FeatureAssets/<Private|Public>/Art/<F>/<id>/Simple.<ext>
                                                                                  /Detail.<ext>
    UnityProject/Assets/Core/Art/Core/<id>/...            the art of Core itself (the smoke of a particle)
    <ext>: png, jpg (a picture) or fbx, obj, prefab (a model; <Tier>.preview.png beside it is what the page shows)
    UnityProject/Assets/_Composition/ArtChoice.json       {"simple": ["<F>/<id>", ...]}  this code only
<F> is the code folder of the feature (Hero, _Services/Popup, Core); <id>: lower case, digits, '-'.
Standard library only; Windows and Linux.
"""
import json
import os
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

ASSETS = ("UnityProject", "Assets")
# An art folder: <feature folder>/FeatureAssets/<Private|Public>/Art/<F>/<id>/, or Core/Art/<F>/<id>/.
ART_RX = re.compile(r"^(?:Features/(?:_Services/)?[A-Za-z][A-Za-z0-9]*/FeatureAssets/(?:Private|Public)|Core)/Art/(.+)$")
CHOICE = ("UnityProject", "Assets", "_Composition", "ArtChoice.json")
TIERS = ("Simple", "Detail")
PICTURES = (".png", ".jpg", ".jpeg")
MODELS = (".fbx", ".obj", ".prefab")
PREVIEW = ".preview.png"
ID_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
FEATURE_RX = re.compile(r"^(_Services/)?[A-Za-z_][A-Za-z0-9_]*$")
SIZE_RX = re.compile(r"^(\d{1,4})x(\d{1,4})$")
SKIP = {"Library", "Temp", "Logs", "obj", "UserSettings"}


def in_art(rel):
    """True for a path under UnityProject/Assets (relative to it) that lies in an art folder."""
    return bool(ART_RX.match(rel))


def tier_file(name):
    """(tier, kind) of a file name of an art folder ('Simple.png' -> ('Simple', 'picture')), or None."""
    stem, ext = os.path.splitext(name)
    ext = ext.lower()
    if stem not in TIERS or name.endswith(PREVIEW):
        return None
    return (stem, "picture") if ext in PICTURES else (stem, "model") if ext in MODELS else None


def art_folders(root):
    """{key: folder} of every art folder under UnityProject/Assets (…/FeatureAssets/<P>/Art/<F>/<id>/, Core/Art/<F>/<id>/)."""
    assets = Path(root).joinpath(*ASSETS)
    found = {}
    for dirpath, dirnames, filenames in os.walk(assets):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIP and not d.startswith("."))
        m = ART_RX.match(Path(dirpath).relative_to(assets).as_posix())
        if not m or not any(tier_file(f) for f in filenames):
            continue
        key = m.group(1)
        feature, _, aid = key.rpartition("/")
        if feature and FEATURE_RX.match(feature) and ID_RX.match(aid):
            found.setdefault(key, Path(dirpath))
    return found


def read_choice(root):
    """The keys the human keeps on Simple."""
    try:
        value = json.loads(Path(root).joinpath(*CHOICE).read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return []
    keys = value.get("simple") if isinstance(value, dict) else None
    return sorted({k for k in keys if isinstance(k, str)}) if isinstance(keys, list) else []


def write_choice(root, keys):
    path = Path(root).joinpath(*CHOICE)
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    tmp.write_text(json.dumps({"simple": sorted(set(keys))}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def items(root, feature=None):
    """Every art with a Simple or a Detail: {key, feature, id, kind, simple, detail, use}, where simple and detail are
    {file, preview} (paths from the project root, preview '' when there is no picture) or None, and use is the tier
    the game shows."""
    root = Path(root)
    keep = set(read_choice(root))
    out = []
    for key, folder in sorted(art_folders(root).items(), key=lambda kv: kv[0].lower()):
        f, _, aid = key.rpartition("/")
        if feature and f != feature:
            continue
        tiers, kind = {}, "picture"
        for name in sorted(os.listdir(folder)):
            hit = tier_file(name)
            if not hit or hit[0] in tiers:
                continue
            rel = (folder / name).relative_to(root).as_posix()
            preview = rel if hit[1] == "picture" else ""
            if hit[1] == "model":
                kind = "model"
                side = folder / f"{hit[0]}{PREVIEW}"
                preview = side.relative_to(root).as_posix() if side.is_file() else ""
            tiers[hit[0]] = {"file": rel, "preview": preview}
        simple, detail = tiers.get("Simple"), tiers.get("Detail")
        use = "detail" if detail and not (simple and key in keep) else "simple"
        out.append({"key": key, "feature": f, "id": aid, "kind": kind, "simple": simple, "detail": detail, "use": use})
    return out


def snapshot(root):
    """What the Studio tab shows."""
    return {"items": items(root)}


def cmd_list(root, feature=None):
    rows = items(root, feature)
    if not rows:
        print("Chưa có art Simple hay Detail nào" + (f" của {feature}" if feature else "") + ": game đang dùng GreyBox.")
        return 0
    for r in rows:
        have = "+".join(t for t, v in (("Simple", r["simple"]), ("Detail", r["detail"])) if v)
        print(f"- {r['key']} ({'model' if r['kind'] == 'model' else 'ảnh'}): có {have}, đang dùng {r['use'].capitalize()}")
    return 0


def cmd_choose(root, key, tier):
    """The game shows `tier` of the art `key`. Detail is what it shows anyway, so choosing it clears the choice."""
    if tier not in ("simple", "detail"):
        raise ValueError("mức là simple hoặc detail")
    row = next((r for r in items(root) if r["key"] == key), None)
    if row is None:
        raise ValueError(f"không có art {key}")
    if not row[tier]:
        raise ValueError(f"art {key} chưa có bản {tier.capitalize()}")
    keys = set(read_choice(root))
    keys.discard(key)
    if tier == "simple" and row["detail"]:
        keys.add(key)
    write_choice(root, keys)
    print(f"{key}: dùng {tier.capitalize()}.")
    return 0


def cmd_svg(src, dst, size):
    import render
    m = SIZE_RX.match(size or "")
    if not m or not all(0 < int(v) <= 4096 for v in m.groups()):
        raise ValueError("cỡ là <rộng>x<cao> (px, tối đa 4096), ví dụ 256x256")
    w, h = int(m.group(1)), int(m.group(2))
    src, dst = Path(src), Path(dst)
    if not src.is_file():
        raise ValueError(f"không có file {src}")
    problems = render.check_svg(render.read_text(src))
    if problems:
        raise ValueError("SVG chưa dùng được: " + "; ".join(problems))
    dst.parent.mkdir(parents=True, exist_ok=True)
    if not render.svg_to_png(src, dst, (w, h)):
        raise ValueError("máy này không đổi được SVG sang PNG (cần rsvg-convert, inkscape, magick, Chrome hoặc Edge)")
    got = render.png_size(dst)
    if got != (w, h):
        raise ValueError(f"PNG ra cỡ {got}, không phải {w}x{h}")
    print(f"Đã vẽ {dst} ({w}x{h}).")
    return 0


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    args = argv[1:]
    try:
        if args[:1] == ["list"] and len(args) <= 2:
            return cmd_list(root, args[1] if len(args) == 2 else None)
        if args[:1] == ["choose"] and len(args) == 3:
            return cmd_choose(root, args[1], args[2])
        if args[:1] == ["svg"] and len(args) == 4:
            return cmd_svg(args[1], args[2], args[3])
    except ValueError as ex:
        print(f"LỖI: {ex}")
        return 1
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
