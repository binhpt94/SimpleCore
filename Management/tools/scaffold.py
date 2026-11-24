#!/usr/bin/env python3
"""Creates the folders the fixed design (live) asks for and that do not exist yet. For @Manager.

    python Management/tools/scaffold.py [--dry-run]          run from the project root

Names come from the feature nodes (round 2) of Management/game-design/live: the folder its code.json names, or the
name GD will give it from its key while it has no code.json yet (gamedesign.live_feature_names), plus Assets/Core
and Assets/_Composition. The code gate is unchanged: a folder made here is coded only once its node has code.json
in live and has reached round 4. Each missing feature or service folder gets the standard layout:
    Scripts/Contracts  Scripts/Internal  Scripts/View  Tests  FeatureAssets/Public  FeatureAssets/Private
with an empty .gitkeep in each of them (Unity skips hidden files; git keeps the empty folder, so the folder .meta
Unity writes is never left without its folder). Core and _Composition get an empty folder with a .gitkeep.

Folders only: no .cs, no .asmdef, no CLAUDE.md. @Manager does not write code; the owning session fills them in.
A folder that exists is never touched, so running it again changes nothing. Refused: a name validate.py would
refuse (not PascalCase, reserved) and any path that goes through a link or junction.
guard.py lets only @Manager (and the human) run this command. --dry-run prints what it would do.
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import validate  # noqa: E402
from gd_delete import is_link  # noqa: E402      symlink (any OS) or junction (Windows)

FEATURE_LAYOUT = ("Scripts/Contracts", "Scripts/Internal", "Scripts/View", "Tests",
                  "FeatureAssets/Public", "FeatureAssets/Private")
KEEP = ".gitkeep"
AREAS = {"Core": "@Core", "_Composition": "@CodeLead"}      # Assets/<area>: the role that owns it


def check_name(name):
    """Raises ValueError when validate.py would not accept `name` ('Hero', '_Services/Popup') as a feature folder."""
    if not gd.CODE_NAME_RX.match(name):
        raise ValueError(f"'{name}' không phải tên thư mục feature hợp lệ")
    leaf = name.split("/")[-1]
    if not validate.NAME_RX.match(leaf):
        raise ValueError(f"'{name}': tên thư mục phải PascalCase (chữ đầu viết hoa, chỉ chữ và số)")
    if leaf in validate.RESERVED:
        raise ValueError(f"'{name}': '{leaf}' là tên dành riêng")


def targets(root):
    """[(label, folder, [subfolders], owner)] of what the design asks for, in the order it is created.
    owner: the live node that names the folder, or the area session."""
    assets = Path(root) / "UnityProject" / "Assets"
    out = [(area, assets / area, [""], owner) for area, owner in AREAS.items()]
    for name, node, declared in gd.live_feature_names(root):
        check_name(name)
        owner = f"live/{node}" + ("" if declared else ", tên đề xuất: node chưa có code.json")
        out.append((name, assets.joinpath("Features", *name.split("/")), list(FEATURE_LAYOUT), owner))
    return out


def no_link_between(root, path):
    """Raises ValueError when an existing folder between the project root and `path` is a link or junction."""
    root = Path(root)
    cur = root
    for part in path.relative_to(root).parts:
        cur = cur / part
        if is_link(cur):
            raise ValueError(f"đường dẫn đi qua link hoặc junction: {cur.relative_to(root).as_posix()}")


def scaffold(root, dry_run=False):
    """(created, existing): labels of the folders made now and of those already there. Raises ValueError when
    refused; nothing is created then."""
    root = Path(root)
    plan = targets(root)
    for _, folder, subs, _ in plan:      # check everything first: a refusal creates nothing
        for sub in subs:
            no_link_between(root, folder / sub if sub else folder)
    created, existing = [], []
    for label, folder, subs, owner in plan:
        if folder.exists():
            existing.append((label, owner))
            continue
        created.append((label, owner))
        if dry_run:
            continue
        for sub in subs:
            leaf = folder / sub if sub else folder
            leaf.mkdir(parents=True, exist_ok=True)
            (leaf / KEEP).touch()
    return created, existing


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    dry = argv[1:] == ["--dry-run"]
    if argv[1:] and not dry:
        print("Cách dùng: python Management/tools/scaffold.py [--dry-run]", file=sys.stderr)
        return 2
    try:
        created, existing = scaffold(root, dry)
    except (ValueError, OSError) as ex:
        print(f"Không tạo: {ex}", file=sys.stderr)
        return 1
    verb = "Sẽ tạo" if dry else "Đã tạo"
    lines = [f"{verb} {len(created)} thư mục, {len(existing)} thư mục đã có (không đụng tới)."]
    lines += [f"- {verb.lower()}: {label}  ({owner})" for label, owner in created]
    lines += [f"- đã có: {label}  ({owner})" for label, owner in existing]
    if created and not dry:
        lines.append("Unity tạo file .meta cho các thư mục mới ở lần biên dịch tới (@human hoặc @CodeLead). "
                     "Code, asmdef và CLAUDE.md của feature do phiên sở hữu viết.")
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
