#!/usr/bin/env python3
"""Prints the fixed design (live) of one code folder in one piece: what its code follows. Reads only; any chat.

    python Management/tools/gd_pack.py Hero
    python Management/tools/gd_pack.py _Services/Popup

In order: whether the folder may be coded now; the nodes above its feature node (title, summary, ideas); the
feature node and every node below it in full (ideas with detail, CLAUDE.md, progress of the leaves); the nodes
of other branches their refs name (ideas with detail; their CLAUDE.md is named, not printed); the glossary terms
those nodes declare.
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

LIVE = "Management/game-design/live"


def full_lines(node, out, doc=True):
    out.append("")
    out.append(f"## {node['path'] or '(gốc)'}  ·  vòng {node['round']}  ·  {node['title']}")
    if node["summary"]:
        out.append(node["summary"])
    out.append("")
    out.append("### Ý cốt lõi")
    for it in node["items"]:
        out.append(f"- {it['title']}")
        for line in it["detail"].split("\n") if it["detail"] else []:
            out.append(f"    {line}")
    if node["priority"].get("leaf"):
        names = {x["key"]: x["name"] for x in gd.PHASES}
        out.append("- Tiến độ: " + " · ".join(f"{names[k]} {c['state']}" for k, c in node["progress"].items()))
    body = node["body"].split("\n", 1)[1].strip() if "\n" in node["body"] else ""
    if doc and body:
        out.append("")
        out.append("### Diễn giải (CLAUDE.md)")
        out += [("##" + line if line.startswith("#") else line) for line in body.split("\n")]      # below the node heading
    elif not doc:
        out.append(f"- Diễn giải: {LIVE}/{node['path']}/{gd.DOC_FILE}")


def pack(root, name):
    """The text to print; raises ValueError when the live design has no feature node for `name`."""
    names = gd.live_feature_names(root)
    keys = [key for folder, key, _ in names if folder.lower() == name.lower()]
    if not keys:
        raise ValueError(f"live không có feature nào cho thư mục '{name}'. Có: " + ", ".join(f for f, _, _ in names))
    snap = gd.snapshot(root)
    live = snap["live"]
    nodes = {n["path"]: n for n in gd.walk(live)}
    ok, reason = gd.code_allowed(root, name)
    out = [f"# Gói thiết kế cho {name} (bảng đã chốt: {LIVE})",
           "Code: " + ("ĐƯỢC CODE" if ok else "ĐANG KHOÁ, " + reason),
           "Chỉ bám theo nội dung dưới đây; thiếu hay mâu thuẫn thì báo người, không tự quyết thiết kế.",
           "", "## Bối cảnh: node gốc"]
    root_node = nodes.get("")
    if root_node:
        b = gd.brief(root_node)
        out.append(f"{b['title']}. {b['summary']}")
        out += [f"- {t}" for t in b["items"]]
    branch = []
    for key in keys:
        top = nodes.get(key)
        if top:
            branch += list(gd.walk(top))
    for n in branch:
        full_lines(n, out)
    inside = {n["path"] for n in branch}
    refs, terms = [], []
    for n in branch:
        for r in n["refs"] + n["refMissing"]:
            if r["term"]:
                if r["term"] not in terms:
                    terms.append(r["term"])
            elif r["path"] not in inside and r["path"] in nodes and r["path"] not in refs:
                refs.append(r["path"])
    if refs:
        out += ["", "# Node ở nhánh khác mà nhánh này dựa vào (refs)"]
    for path in refs:
        full_lines(nodes[path], out, doc=False)
        for r in nodes[path]["refs"]:
            if r["term"] and r["term"] not in terms:
                terms.append(r["term"])
    glossary = {t["id"]: t for t in (snap["glossary"]["live"] or {}).get("terms", [])}
    found = [glossary[t] for t in terms if t in glossary]
    if found:
        out += ["", "# Thuật ngữ (node glossary)"]
        for t in found:
            out.append(f"- **{t['term']}**: {t['meaning']}" + (f" ({t['detail']})" if t["detail"] else ""))
    stale = [f"{n['path']}: {', '.join(n['refStale'] + [r['label'] for r in n['refMissing']])}"
             for n in branch if n["refStale"] or n["refMissing"]]
    if stale:
        out += ["", "# Cảnh báo: refs chưa khớp (thiết kế có thể lệch giữa các nhánh, hỏi @GameDesigner)"] + [f"- {s}" for s in stale]
    return "\n".join(out)


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    if len(argv) != 2 or not gd.CODE_NAME_RX.match(argv[1]):
        print("Cách dùng: python Management/tools/gd_pack.py <Tên thư mục code, ví dụ Hero hoặc _Services/Popup>", file=sys.stderr)
        return 2
    try:
        print(pack(root, argv[1]))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không đọc được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
