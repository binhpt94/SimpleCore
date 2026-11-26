#!/usr/bin/env python3
"""Every place in the proposed design table that still names a node or a feature: before deleting it, or after the
human deleted it on the page, to know what is left to clean. Reads only.

    python Management/tools/gd_impact.py ai/idea/<node>        a node (also when it was already deleted)
    python Management/tools/gd_impact.py <key>                 a node key or a feature: layout-spec, feature-ads

Looks for the node's key (and its path) in any case, as a whole word, in every node's data.md (refs: and based-on
apart, the rest as text), CLAUDE.md, ui.md, code.json, .frame-concept/*.frame, .excel/*.csv, .art-style and
.images/index.md. For a round-2 node it also looks for the Unity feature names of its code.json (proposed or live
copy). .history, .user-review, .progress and the node's own folder are left out. Prints one block per node, one line
per hit (file:line: text), and the totals.
"""
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
LIVE_PREFIX = "/".join(gd.TABLES["live"])
USAGE = f"Cách dùng: python Management/tools/gd_impact.py {PREFIX}/<node> | <tên node hay feature>"
SKIP_DIRS = {gd.HISTORY_DIR, gd.REVIEW_DIR, gd.PROGRESS_DIR}
TEXT_EXT = {".md", ".json", ".frame", ".csv", ".txt"}
WORD = r"A-Za-z0-9_\-"


def what(root, arg):
    """(node path or None, key, [extra names]) the argument names."""
    arg = arg.replace("\\", "/").strip().rstrip("/")
    for prefix in (PREFIX + "/", LIVE_PREFIX + "/"):
        if arg.startswith(prefix):
            arg = arg[len(prefix):]
            break
    if not arg or arg in (PREFIX, LIVE_PREFIX):
        raise ValueError("cần một node, không phải cả bảng")
    if not all(gd.KEY_RX.match(p) for p in arg.split("/")):
        raise ValueError(f"'{arg}' không phải tên node")
    path = arg if "/" in arg else None
    key = arg.split("/")[-1]
    if path is None:      # a bare key: the node with that key, if one is left
        hits = [p for p in gd.table_dir(root, "idea").rglob(gd.DATA_FILE)
                if p.parent.name == key and not any(x.startswith(".") for x in p.relative_to(gd.table_dir(root, "idea")).parts)]
        if len(hits) == 1:
            path = "/".join(hits[0].parent.relative_to(gd.table_dir(root, "idea")).parts)
    extra = []
    if path and "/" not in path:      # a round-2 node: the Unity feature folders it is coded in
        for table in ("idea", "live"):
            try:
                code = json.loads((gd.table_dir(root, table) / path / gd.CODE_FILE).read_text(encoding="utf-8"))
            except (OSError, ValueError):
                continue
            extra += [n for n in (code.get("unity") or []) if isinstance(n, str) and n not in extra] if isinstance(code, dict) else []
    return path, key, extra


def patterns(path, key, extra):
    out = [re.compile(rf"(?<![{WORD}]){re.escape(key)}(?![{WORD}])", re.I)]
    if path and path != key:
        out.append(re.compile(rf"(?<![{WORD}/]){re.escape(path)}(?![{WORD}])", re.I))
    out += [re.compile(rf"(?<![{WORD}]){re.escape(n)}", re.I) for n in extra]      # WalletView names Wallet too
    return out


def node_of(rel):
    """The node path (folders not starting with ".") a file of the table belongs to."""
    parts = []
    for p in rel.parts[:-1]:
        if p.startswith("."):
            break
        parts.append(p)
    return "/".join(parts)


def scan(root, path, key, extra):
    """{node: [(file, line no, kind, text)]} of every hit, node paths in tree order."""
    table = gd.table_dir(root, "idea")
    rx = patterns(path, key, extra)
    hits = {}
    for file in sorted(table.rglob("*")):
        rel = file.relative_to(table)
        if not file.is_file() or any(p in SKIP_DIRS for p in rel.parts[:-1]):
            continue
        if file.suffix.lower() not in TEXT_EXT and gd.STYLE_DIR not in rel.parts[:-1]:
            continue
        node = node_of(rel)
        if path and (node == path or node.startswith(path + "/")):
            continue      # the node's own files go with it
        try:
            text = file.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for no, line in enumerate(text.split("\n"), 1):
            if any(r.search(line) for r in rx):
                head = line.partition(":")[0].strip()
                kind = head if file.name == gd.DATA_FILE and head in ("refs", "based-on") else ""
                hits.setdefault(node, []).append(("/".join(rel.parts[len(node.split("/")) if node else 0:]), no, kind, line))
    return hits


def short(text, n=160):
    text = " ".join(text.split())
    return text if len(text) <= n else text[:n - 1] + "…"


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":      # test-only override
        root, args = Path(args[-1]), args[:-2]
    if len(args) != 1:
        print(USAGE, file=sys.stderr)
        return 2
    try:
        path, key, extra = what(root, args[0])
    except ValueError as ex:
        print(f"Lỗi: {ex}", file=sys.stderr)
        return 1
    hits = scan(root, path, key, extra)
    names = ", ".join([key] + ([path] if path and path != key else []) + extra)
    alive = path and (gd.table_dir(root, "idea").joinpath(*path.split("/")) / gd.DATA_FILE).is_file()
    print(f"Tìm (không phân biệt hoa thường): {names}" + ("" if alive else " · node không còn trong bảng đề xuất"))
    for node, rows in hits.items():
        print(f"\n## {node or '(gốc)'}")
        for file, no, kind, line in rows:
            print(f"  {file}:{no}: " + (f"[{kind}] " if kind else "") + short(line))
    refs = sum(1 for rows in hits.values() for r in rows if r[2] == "refs")
    print(f"\n{sum(len(r) for r in hits.values())} chỗ ở {len(hits)} node ({refs} dòng refs)." +
          (" Ref tới node đã xoá: gd_pin.py --all --drop-dead." if refs and not alive else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
