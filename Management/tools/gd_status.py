#!/usr/bin/env python3
"""Prints where the proposed game-design tree stands. For @GameDesigner, at the start of a turn. Reads only.

    python Management/tools/gd_status.py                     the whole tree
    python Management/tools/gd_status.py ai/idea/<path>      one branch

For every node: its round, whether it passes and why not, its data hash and the hash to write into the
children's based-on, every idea with the AI's and the human's score and feedback, unanswered questions, whether
it is the same as live, and what the human turned down here before (from .history).
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])


def lines_for(root, node, out):
    name = node["path"] or "(gốc)"
    folder = gd.table_dir(root, "idea").joinpath(*(node["path"].split("/") if node["path"] else []))
    out.append("")
    out.append(f"## {name}  ·  vòng {node['round']}  ·  {node['title']}")
    out.append("- Kết luận: " + ("ĐẠT" if node["passed"] else "CHƯA ĐẠT, bạn cần làm lại" if node["redo"] else "CHƯA ĐẠT, đang chờ người chấm/trả lời")
               + ("" if node["passed"] else ": " + "; ".join(node["reasons"])))
    out.append(f"- Hash data: {node['dataHash']} (node con ghi `based-on: {node['dataHash']}` sau khi rà theo data này)")
    if node["path"]:
        out.append(f"- based-on đang ghi: {node['basedOn'] or '(chưa có)'}; hash data của cha: {node['parentHash']}"
                   + (" -> LỆCH, rà lại node rồi ghi hash mới" if node["stale"] else " -> khớp"))
    out.append(f"- So với live: {dict(new='chưa có trong live', changed='khác live', same='giống live')[node['state']]}"
               + ("; các node con đang mở cho bạn ghi" if node["approved"] else "; các node con đang bị khoá"))
    p = node["priority"]
    phase = next((x["name"] for x in gd.PHASES if x["key"] == p["phase"]), "chưa xếp")
    line = f"- Độ quan trọng: {'chưa có' if p['effective'] is None else p['effective']} ({phase})"
    if p["user"] is not None:
        line += f"; người chấm {p['user']}"
    elif p["offset"]:
        line += f"; {p['offset']:+d} do người chấm node '{p['from']}'"
    if p["leaf"]:
        line += f"; AI {p['ai']}" if p["ai"] is not None else f"; BẠN CHƯA CHẤM: ghi {gd.PRIORITY_FILE} cho node lá này"
    elif p["ignoredAi"]:
        line += f"; {gd.PRIORITY_FILE} ở node này bị bỏ qua vì nó có node con (chỉ chấm ở node lá)"
    if p["done"]:
        line += "; ĐÃ XONG" + ("" if p["leaf"] else " (mọi node lá bên dưới đã xong)")
    out.append(line)
    if p["leaf"]:
        names = {x["key"]: x["name"] for x in gd.PHASES}
        cells = [f"{names[k]} {c['state']}" + (f" ({c['by']} {c['at']})" if c["by"] else "") for k, c in node["progress"].items()]
        out.append("- Tiến độ: " + " · ".join(cells))
    for gate in node["codeGate"]:
        out.append(f"- Thư mục code {gate['unity']}: " + ("ĐƯỢC CODE" if gate["ok"] else "ĐANG KHOÁ, " + gate["reason"]))
    for it in node["items"]:
        user = "chưa chấm" if it["user"] is None else str(it["user"])
        out.append(f"- Ý {it['id']}: AI {it['ai']}, người {user}" + (f", phản hồi: {it['feedback']}" if it["feedback"] else "")
                   + ("" if it["titled"] else " | THIẾU TIÊU ĐỀ: viết lại dòng thành '### id | AI n | tiêu đề ngắn'"))
        if it["previous"]:
            p = it["previous"]
            out.append(f"    bản trước của ý này người chấm {p['score']}" + (f", phản hồi: {p['feedback']}" if p["feedback"] else ""))
    waiting = [q for q in node["questions"] if not q["answered"]]
    if waiting:
        out.append(f"- Câu hỏi người chưa trả lời: {', '.join(q['id'] for q in waiting)}")
    for q in node["questions"]:
        if q["answered"]:
            labels = {o["id"]: o["label"] for o in q["options"]}
            chosen = ", ".join(labels[c] for c in q["chosen"])
            out.append(f"- Người đã trả lời '{q['id']}': {chosen}" + (f" | ý kiến khác: {q['other']}" if q["other"] else ""))
    turned_down = gd.rejected(folder)
    if turned_down:
        out.append("- Đã bị người loại ở node này (đừng đề xuất lại):")
        for e in turned_down:
            text = " ".join(str(e.get("text", "")).split())
            out.append(f"    [{e.get('at', '')}] {e.get('item', '')} (điểm {e.get('score')}): {text[:160]}"
                       + (f" | phản hồi: {e.get('feedback')}" if e.get("feedback") else ""))
    for child in node["children"]:
        lines_for(root, child, out)


def report(root, arg=""):
    """The text to print; raises ValueError for a path that is not a node of the idea table."""
    path = ""
    if arg:
        arg = arg.replace("\\", "/").rstrip("/")
        if arg != PREFIX and not arg.startswith(PREFIX + "/"):
            raise ValueError(f"đường dẫn phải có dạng {PREFIX}/<node>")
        path = arg[len(PREFIX) + 1:]
    gd.check_path(path)
    snap = gd.snapshot(root)
    node = next((n for n in gd.walk(snap["idea"]) if n["path"] == path), None)
    if node is None:
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    mode = "User control (người chấm hết, mọi điểm từ %d)" if snap["mode"] == "user" else "Full Auto (điểm hiệu lực từ %d; người bấm Sync để chép node đạt sang live)"
    out = [f"# Game design: {mode % gd.PASS_MIN}",
           "Thang điểm: " + " · ".join(f"{b['min']}-{b['max']} {b['label']}" for b in gd.BANDS),
           "Độ quan trọng (chỉ chấm ở node lá): " + " · ".join(f"{x['min']}-{x['max']} {x['name']}: {x['label']}" for x in gd.PHASES)]
    lines_for(root, node, out)
    problems = [p for p in snap["problems"]["idea"] if not path or p["path"] == path or p["path"].startswith(path + "/")]
    if problems:
        out += ["", "## Lỗi cấu trúc (phải sửa trước)"] + [f"- {p['path']}: {p['text']}" for p in problems]
    if not path and snap["missing"]:
        out += ["", "## Thiếu feature hạ tầng (vòng 2 luôn phải có)"] + [f"- {p['path']}: {p['text']}" for p in snap["missing"]]
    if not path and snap["missingCode"]:
        out += ["", "## Thiếu code.json (vòng 2: tự đặt theo key, không cần hỏi người)"] + [
            f"- {p['path']}: {p['text']}" for p in snap["missingCode"]]
    return "\n".join(out)


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    if len(argv) > 2:
        print(f"Cách dùng: python Management/tools/gd_status.py [{PREFIX}/<node>]", file=sys.stderr)
        return 2
    try:
        print(report(root, argv[1] if len(argv) == 2 else ""))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không đọc được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
