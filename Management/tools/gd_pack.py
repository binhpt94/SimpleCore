#!/usr/bin/env python3
"""Prints the fixed design (live) of one code folder in one piece: what its code follows. Reads only; any chat.

    python Management/tools/gd_pack.py Hero
    python Management/tools/gd_pack.py _Services/Popup

In order: whether the folder may be coded now; the nodes above its feature node (title, summary, ideas); the
feature node and every node below it in full (ideas with detail, CLAUDE.md, progress of the leaves); the nodes
of other branches their refs name (ideas with detail; their CLAUDE.md is named, not printed); the glossary terms
those nodes declare.
The pack is long, so it is written to Management/game-design/.pack/ and the command prints that file's path and a
table of contents with line ranges (gamedesign.emit); a pack short enough is printed as it is.
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import reqbook  # noqa: E402

LIVE = "Management/game-design/live"
PACK_NOTE = ("Trước khi code một phần: đọc đủ các node vòng 4 và mục CHEAT của phần đó, cùng các node refs mà nó nhắc "
             "(mục 'Node ở nhánh khác') và mục Thuật ngữ. Không code chỉ từ mục lục.")


def part_lines(a):
    """One line per part (picture) of a claude concept: its size, whether it is 9-sliced, the shapes it stands for."""
    out = []
    for p in a.get("parts") or []:
        src = p.get("from")
        out.append(f"phần {p['id']}: {p['w']}x{p['h']}px, " + (f"9-slice viền trái/trên/phải/dưới {p['slice']}" if p.get("slice") else "không 9-slice")
                   + f", dùng cho {len(p['shapes'])} hình ({', '.join(p['shapes'])})"
                   + (f", lấy lại từ {src['node']}#{src['part']}" if src else "") + f"; {p['file']}")
    return out


def parts_total(branch):
    """Lines that sum up the pictures a feature needs, from the claude concepts of its nodes (the cheat node only
    puts their parts together, so it is left out)."""
    rows, taken = [], 0
    for n in branch:
        if n.get("cheat"):
            continue
        seen = set()
        for a in n.get("art", []):
            if a.get("tool") != gd.CLAUDE_TOOL or a["frame"] in seen or a.get("state") != "mới nhất":
                continue
            seen.add(a["frame"])
            for p in a.get("parts") or []:
                if p.get("from"):
                    taken += 1
                else:
                    rows.append((n["path"], a["frame"], p))
    if not rows and not taken:
        return []
    out = ["", "# Các hình feature này cần (theo claude-concept; tham chiếu để đặt order sprite, không chép vào Assets)",
           f"- {len(rows)} hình riêng" + (f", {taken} hình lấy lại từ node khác (dùng chung, không tính lại)" if taken else "")
           + f"; 9-slice được: {sum(1 for r in rows if r[2].get('slice'))}; một hình dùng nhiều chỗ: {sum(1 for r in rows if len(r[2]['shapes']) > 1)}"]
    for path, frame, p in rows:
        out.append(f"- {p['id']} ({path}, frame {frame}): {p['w']}x{p['h']}px, "
                   + (f"9-slice viền {p['slice']}" if p.get("slice") else "không 9-slice") + f", {len(p['shapes'])} hình dùng")
    return out


def full_lines(node, out, doc=True, book=None):
    out.append("")
    if node.get("cheat"):
        out.append(f"## {node['path']}  ·  CHEAT (chỉ bản dev)  ·  {node['title']}")
        out.append("Mỗi ý là một state để dựng ngay khi test và Play: ép đúng các giá trị ghi trong chi tiết. "
                   "Code cheat nằm trong thư mục của feature, chỉ chạy ở bản dev (UNITY_EDITOR || DEVELOPMENT_BUILD).")
    else:
        out.append(f"## {node['path'] or '(gốc)'}  ·  vòng {node['round']}  ·  {node['title']}")
    if node["summary"]:
        out.append(node["summary"])
    out.append("")
    out.append("### Ý cốt lõi")
    for it in node["items"]:
        out.append(f"- {gd.shown_title(it)}{gd.by_label(it)}")
        for line in it["detail"].split("\n") if it["detail"] else []:
            out.append(f"    {line}")
    entry = (book or {}).get(node["path"])
    if entry:
        out.append("- Tiến độ: " + reqbook.progress_note(entry))
        out.append("")
        out.append(f"### Yêu cầu ({entry['met']}/{len(entry['reqs'])} đã kiểm; ghi bằng gd_req.py, xem đủ ở gd_req.py <Tên>)")
        for r in sorted(entry["reqs"], key=lambda r: reqbook.ORDER.index(r["state"])):
            out.append(f"- [{reqbook.STATE_NAMES[r['state']]}] {r['ref']}" + (f" ({r['why']})" if r["why"] else ""))
    elif node["priority"].get("leaf"):
        names = {x["key"]: x["name"] for x in gd.PHASES}
        out.append("- Tiến độ: " + " · ".join(f"{names[k]} {c['state']}" for k, c in node["progress"].items()))
    if node.get("ui"):
        out.append("")
        out.append(f"### UI cần có ({LIVE}/{node['path']}/{gd.UI_FILE}: phần tử, tương tác, flow; id trùng id hình trong frame)")
        out += gd.ui_lines(node["ui"])[1:]
    if node["frames"]:
        out.append("")
        out.append(f"### Frame (bố cục ước lượng của @UIDesigner; nguồn {LIVE}/{node['path']}/{gd.FRAME_DIR}/)")
        for fr in node["frames"]:
            out += gd.frame_brief(fr)
    for tool, head in ((gd.ART_TOOL, "### Art concept (ảnh @Codex gen theo frame; xem phong cách, không đo kích thước từ ảnh)"),
                       (gd.CLAUDE_TOOL, "### Claude concept (ảnh ghép của frame và các phần @ClaudeImage vẽ; đủ dùng ở giai đoạn "
                                        "IDEA; kích thước lấy ở frame và ở dòng của từng phần)")):
        latest = {}
        for a in node.get("art", []):
            if a.get("tool", gd.ART_TOOL) == tool:
                latest.setdefault(a["frame"], a)
        if not latest:
            continue
        out.append("")
        out.append(head)
        for frame, a in sorted(latest.items()):
            use = "dùng làm mẫu" if a.get("level") in (2, 3) and not a["stale"] else "chỉ để tham khảo"
            out.append(f"- {frame}: {a['file']} ({a['label']}; {use}" + (f"; CHƯA CHUẨN: {a['below']}" if a.get("below") else "") + ")")
            for line in part_lines(a):
                out.append(f"    {line}")
    if doc and node.get("excel"):      # the config tables of the node (<node>/.excel), as text
        out.append("")
        out.append(f"### Bảng dữ liệu ({LIVE}/{node['path']}/{gd.gd_excel.EXCEL_DIR}/: số liệu config của node; dòng đầu là tên cột, "
                   "dòng hai là kiểu và mô tả)")
        out += gd.gd_excel.markdown(node["excel"], "####")
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
    try:
        book = {n["path"]: n for n in reqbook.view(root, name)["nodes"]}
    except (ValueError, OSError):
        book = {}
    for n in branch:
        full_lines(n, out, book=book)
    out += parts_total(branch)
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
        print(gd.emit(root, "pack", argv[1], pack(root, argv[1]), PACK_NOTE))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không đọc được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
