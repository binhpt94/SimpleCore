#!/usr/bin/env python3
"""Tests for scaffold.py, on a throw-away project in a temp folder.

    python Management/tools/test_scaffold.py
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import scaffold  # noqa: E402
import validate  # noqa: E402

results = []
WIN = os.name == "nt"


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def code(root, key, *names):
    p = gd.table_dir(root, "live") / key / gd.CODE_FILE
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps({"unity": list(names)}), encoding="utf-8")


def tree(root):
    """Every folder and file under UnityProject/Assets, as sorted '/' paths."""
    assets = Path(root) / "UnityProject" / "Assets"
    return sorted(p.relative_to(assets).as_posix() for p in assets.rglob("*")) if assets.is_dir() else []


def refused(root):
    try:
        scaffold.scaffold(root)
        return False
    except ValueError:
        return True


def cli(root, *args):
    p = subprocess.run([sys.executable, str(HERE / "scaffold.py"), *args, "--root", str(root)], capture_output=True,
                       text=True, encoding="utf-8")
    return p.returncode, p.stdout + p.stderr


def run_layout(root):
    code(root, "feature-hero", "Hero")
    code(root, "feature-popup", "_Services/Popup")
    (gd.table_dir(root, "live") / ".history").mkdir()      # hidden folders of the table are no nodes
    check("dry run creates nothing", (scaffold.scaffold(root, True), tree(root)),
          (([("Core", "@Core"), ("_Composition", "@Composition"), ("_Services/Popup", "live/feature-popup"),
             ("Hero", "live/feature-hero")], []), []))
    created, existing = scaffold.scaffold(root)
    check("creates Core, _Composition and every feature the live design names",
          [label for label, _ in created], ["Core", "_Composition", "_Services/Popup", "Hero"])
    hero = [p for p in tree(root) if p.startswith("Features/Hero/")]
    want = sorted(["Features/Hero/" + d for d in ("Scripts", "FeatureAssets")] +
                  ["Features/Hero/" + d for d in scaffold.FEATURE_LAYOUT] +
                  ["Features/Hero/" + d + "/.gitkeep" for d in scaffold.FEATURE_LAYOUT])
    check("a feature gets the standard layout with a .gitkeep in each leaf", hero, want)
    check("a service goes under Features/_Services",
          (Path(root) / "UnityProject/Assets/Features/_Services/Popup/Scripts/Internal/.gitkeep").is_file())
    check("Core and _Composition get an empty folder with a .gitkeep",
          [p for p in tree(root) if p.split("/")[0] in ("Core", "_Composition")],
          ["Core", "Core/.gitkeep", "_Composition", "_Composition/.gitkeep"])
    check("no code, asmdef or CLAUDE.md", [p for p in tree(root) if p.endswith((".cs", ".asmdef", ".md"))], [])
    rules = json.loads((HERE / "rules.json").read_text(encoding="utf-8-sig"))
    check("validate.py finds 0 violations in the result", validate.validate(root, rules)["violations"], [])
    before = tree(root)
    check("running again changes nothing", (scaffold.scaffold(root)[0], tree(root)), ([], before))


def run_suggested(root):
    live = gd.table_dir(root, "live")
    for key in ("feature-popup-lose", "feature-hero"):
        (live / key).mkdir(parents=True)
        (live / key / gd.DATA_FILE).write_text("# x\n", encoding="utf-8")
    code(root, "feature-hero", "Hero")
    created = scaffold.scaffold(root)[0]
    check("a live feature without code.json gets its folder under the name GD will give it",
          [(l, "tên đề xuất" in o) for l, o in created], [("Core", False), ("_Composition", False), ("Hero", False), ("PopupLose", True)])
    check("with the standard layout", (Path(root) / "UnityProject/Assets/Features/PopupLose/Scripts/View/.gitkeep").is_file())


def run_existing(root):
    own = Path(root) / "UnityProject/Assets/Features/Wallet"
    (own / "Scripts" / "Internal").mkdir(parents=True)
    (own / "Scripts" / "Internal" / "W.cs").write_text("class W {}", encoding="utf-8")
    code(root, "feature-wallet", "Wallet")
    created, existing = scaffold.scaffold(root)
    check("an existing feature folder is listed, not created", ([l for l, _ in created], [l for l, _ in existing]),
          (["Core", "_Composition"], ["Wallet"]))
    check("and nothing is added inside it", sorted(p.relative_to(own).as_posix() for p in own.rglob("*")),
          ["Scripts", "Scripts/Internal", "Scripts/Internal/W.cs"])


def run_refusals(root):
    for label, names in (("lower case", ["hero"]), ("reserved", ["Core"]), ("reserved service", ["_Services/Services"]),
                         ("second bad", ["Hero", "wallet"])):
        shutil.rmtree(gd.table_dir(root, "live"), ignore_errors=True)
        code(root, "feature-x", *names)
        check(f"refuses a {label} name and creates nothing", (refused(root), tree(root)), (True, []))
    shutil.rmtree(gd.table_dir(root, "live"))
    code(root, "feature-x", "Hero_X")      # not the code.json shape: gamedesign drops it (gd_status shows it as a problem)
    check("a code.json gamedesign rejects names no folder", [l for l, _ in scaffold.scaffold(root, True)[0]], ["Core", "_Composition"])
    shutil.rmtree(gd.table_dir(root, "live"))
    code(root, "feature-hero", "Hero")
    outside = Path(tempfile.mkdtemp(prefix="scafout-"))
    try:
        features = Path(root) / "UnityProject/Assets/Features"
        features.parent.mkdir(parents=True)
        if WIN:
            import _winapi
            _winapi.CreateJunction(str(outside), str(features))
        else:
            os.symlink(outside, features, target_is_directory=True)
        check("refuses a path through a link and creates nothing", (refused(root), sorted(outside.iterdir())), (True, []))
        os.rmdir(features) if WIN else os.unlink(features)
    finally:
        shutil.rmtree(outside, ignore_errors=True)


def run_cli(root):
    code(root, "feature-hero", "Hero")
    rc, out = cli(root, "--dry-run")
    check("CLI --dry-run: exit 0, says what it would make, makes nothing", (rc, "Sẽ tạo 3" in out, tree(root)), (0, True, []))
    rc, out = cli(root)
    check("CLI: exit 0 and lists what it made", (rc, "Đã tạo 3" in out and "Hero  (live/feature-hero)" in out), (0, True))
    rc, out = cli(root, "Hero")
    check("CLI: an unknown argument is refused", rc, 2)
    code(root, "feature-bad", "bad")
    rc, out = cli(root)
    check("CLI: a refused name exits 1", (rc, "Không tạo" in out), (1, True))


def main():
    for case in (run_layout, run_suggested, run_existing, run_refusals, run_cli):
        root = tempfile.mkdtemp(prefix="scaffx-")
        try:
            case(root)
        finally:
            shutil.rmtree(root, ignore_errors=True)
    failed = [x for x in results if not x[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
