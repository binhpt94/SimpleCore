#!/usr/bin/env python3
"""Prints where the proposed game-design tree stands. For @GameDesigner, at the start of a turn. Reads only.

    python Management/tools/gd_status.py                     the whole tree
    python Management/tools/gd_status.py ai/idea/<path>      one branch

For every node: its round, whether it passes and why not, its data hash and the hash to write into the
children's based-on, its refs (matching, changed, mentioned but not declared, and the refs line to write), every
idea with the AI's and the human's score and feedback, unanswered questions, whether it is the same as live, and
what the human turned down here before (from .history).
For one branch it starts with "Bám theo": the nodes above it and the nodes and terms its refs name, read before
writing. For the whole tree it ends with the glossary reminders.
A long report (the whole tree, a big branch) is written to Management/game-design/.pack/ and the command prints that
file's path and a table of contents with line ranges (gamedesign.emit); a short one is printed as it is.
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
STATUS_NOTE = ("Đọc trước: các node mà mục lục ghi Kết luận CHƯA ĐẠT, và các mục cuối file (Lỗi cấu trúc, Chưa hỏi GPT, Thiếu..., "
               "Thuật ngữ). Trước khi viết hay sửa một node vẫn chạy lệnh này cho riêng node đó.")


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
    if node.get("clashOpen"):
        out.append(f"- XUNG ĐỘT CHƯA GIẢI QUYẾT: {node['clashOpen']} nhóm ý trái nhau người chưa chọn. Các node con bị khoá tới khi "
                   "người chấm để mỗi nhóm chỉ còn một ý đạt; không tự chọn thay người, không viết vòng sau. Báo người rồi chờ.")
    for r in node["refs"]:
        state = "khớp" if r["ok"] else ("LỆCH, hash hiện tại " + r["current"] if r["ok"] is False else "KHÔNG CÓ")
        out.append(f"- ref {r['label']}@{r['hash']}" + (f" ({r['title']})" if r["title"] else "") + f": {state}")
    for r in node["refMissing"]:
        out.append(f"- nhắc {r['label']} mà CHƯA KHAI refs")
    for text in node["refNotes"]:
        out.append(f"- nhắc: {text}")
    if node["refMissing"] or node["refStale"]:
        out.append(f"- Sau khi đọc và rà, ghi dòng này vào phần đầu data.md: `{node['refLine']}`")
    if not node["path"] and node["screen"]:
        w, h = gd.SCREENS[node["screen"]]
        out.append(f"- Màn hình: {node['screen']} ({w}x{h} px; frame ui vẽ theo màn này)")
    for s in node.get("styles", []):
        out.append(f"- Style {s['id']}: {s['title']}" + (f" ({len(s['images'])} ảnh tham chiếu)" if s["images"] else ""))
    seen_art = set()
    for a in node.get("art", []):
        tool = a.get("tool", gd.ART_TOOL)
        if (tool, a["frame"]) not in seen_art:
            seen_art.add((tool, a["frame"]))
            out.append(f"- {'Claude concept' if tool == gd.CLAUDE_TOOL else 'Art'} {a['frame']}: {a['label']} (v{a['version']}, {a['ref']})"
                       + (f", {len(a['parts'])} phần" if a.get("parts") else ""))
    if node.get("cheat"):
        out.append(f"- Node cheat: {len(node['items'])}/{gd.CHEAT_MAX} state, mỗi state một frame trùng id; "
                   "state dựng nhanh ở bản dev, và là node duy nhất đặt order art-concept của Codex (mỗi state một ảnh); "
                   "claude-concept của nó do tool dựng từ phần của các node vòng 4")
    for fr in node["frames"]:
        out += ["- " + line.lstrip() if i == 0 else "  " + line for i, line in enumerate(gd.frame_brief(fr))]
    if node["frameNone"]:
        out.append(f"- Frame: không có bố cục (frame: {gd.FRAME_NONE})")
    for text in node["frameNotes"]:
        out.append(f"- Frame (nhắc, không chặn): {text}")
    p = node["priority"]
    if p.get("glossary"):
        out.append(f"- Bảng thuật ngữ chung: {len(node['terms'])} từ (không chấm độ quan trọng, không có tiến độ)")
    phase = next((x["name"] for x in gd.PHASES if x["key"] == p["phase"]), "chưa xếp")
    line = "" if p.get("glossary") else f"- Độ quan trọng: {'chưa có' if p['effective'] is None else p['effective']} ({phase})"
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
    if line:
        out.append(line)
    if p["leaf"]:
        names = {x["key"]: x["name"] for x in gd.PHASES}
        cells = [f"{names[k]} {c['state']}" + (f" ({c['by']} {c['at']})" if c["by"] else "") for k, c in node["progress"].items()]
        out.append("- Tiến độ: " + " · ".join(cells))
    for gate in node["codeGate"]:
        out.append(f"- Thư mục code {gate['unity']}: " + ("ĐƯỢC CODE" if gate["ok"] else "ĐANG KHOÁ, " + gate["reason"]))
    advice = gd.advice_folder(gd.table_dir(root, "idea"), node["path"])
    asked = gd.advice_ids(advice)
    where = "/".join(gd.BASE[1:] + (gd.ADVICE_DIR, advice.name))
    if node.get("nodeBy"):
        out.append(f"- Node do GPT đề xuất (lần hỏi {node['nodeBy']['advice']})")
    if asked:
        out.append("- Góp ý của GPT (ngang hàng ý của bạn: mỗi góp ý phải thành một ý, không được bỏ; xem mục "
                   "'Bên @GameDesigner' của .claude/rules/designadvisor.md): " + ", ".join(f"{where}/{a}.md" for a in asked))
    if node.get("gptOwed"):
        out.append(f"- CHƯA HỎI GPT (bắt buộc ở vòng {' và '.join(map(str, gd.ADVICE_ROUNDS))}): nhắn DesignAdvisor "
                   f"(SendMessage, to: DesignAdvisor) hỏi GPT về node này; câu trả lời sẽ nằm ở {where}/<id>.md")
    gpt = node.get("gpt") or {}
    for m in gpt.get("missing", []):
        out.append(f"- GÓP Ý CHƯA VÀO NODE: {m['advice']}#{m['point']}: {m['title']} "
                   f"-> viết thành một ý, khai by: <id ý>=gpt@{m['advice']}#{m['point']}")
    for d in gpt.get("dropped", []):
        out.append(f"- Ý của GPT đã bị loại: {d['advice']}#{d['point']} (ý {d['item']}): {d['title']}")
    titles = {x["id"]: x["title"] for x in node["items"]}
    for it in node["items"]:
        user = "chưa chấm" if it["user"] is None else str(it["user"])
        out.append(f"- Ý {it['id']}{gd.by_label(it)}: AI {it['ai']}, người {user}" + (f", phản hồi: {it['feedback']}" if it["feedback"] else "")
                   + ("" if it["titled"] else " | THIẾU TIÊU ĐỀ: viết lại dòng thành '### id | AI n | tiêu đề ngắn'")
                   + (" | TRÁI NHAU với " + ", ".join(f"{c} ({titles.get(c, '?')})" for c in it["clash"])
                      + ": các ý này không cùng đúng (chọn 1), chờ chấm rồi loại một bên" if it.get("clash") else "")
                   + (f" | Ý DÀI: một ý một quyết định (tiêu đề tối đa {gd.IDEA_TITLE_MAX} kí tự, chi tiết {gd.IDEA_DETAIL_MAX}); "
                      "tách hoặc đưa phần giải thích sang CLAUDE.md lần tới sửa node" if it.get("long") else ""))
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


def brief_lines(b, out, indent=""):
    out.append(f"{indent}- {b['path']} (vòng {b['round']}): {b['title']}" + (f". {b['summary']}" if b["summary"] else ""))
    for title in b["items"]:
        out.append(f"{indent}    · {title}")


def context_lines(snap, node):
    """The "Bám theo" section: what a node builds on, read before writing it."""
    out = ["", "## Bám theo (đọc trước khi viết node này)", "Chuỗi node phía trên:"]
    for a in gd.ancestors(snap["idea"], node["path"]):
        brief_lines(gd.brief(a), out)
    nodes = {n["path"]: n for n in gd.walk(snap["idea"])}
    terms = {t["id"]: t for t in (snap["glossary"]["idea"] or {}).get("terms", [])}
    listed = [r for r in node["refs"] if r["ok"] is not None] + node["refMissing"]
    if listed:
        out.append("Node và từ mà refs trỏ tới (đọc cả CLAUDE.md của node):")
    for r in listed:
        if r["term"]:
            t = terms.get(r["term"])
            if t:
                out.append(f"- {r['label']}: {t['term']}: {t['meaning']}" + (f" ({t['detail']})" if t["detail"] else ""))
        elif r["path"] in nodes:
            brief_lines(gd.brief(nodes[r["path"]]), out)
    return out


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
    if path:
        out += context_lines(snap, node)
    lines_for(root, node, out)
    problems = [p for p in snap["problems"]["idea"] if not path or p["path"] == path or p["path"].startswith(path + "/")]
    if problems:
        out += ["", "## Lỗi cấu trúc (phải sửa trước)"] + [f"- {p['path']}: {p['text']}" for p in problems]
    owed = [n for n in gd.walk(node) if n.get("gptOwed")]
    if owed:
        out += ["", f"## Chưa hỏi GPT (bắt buộc ở vòng {' và '.join(map(str, gd.ADVICE_ROUNDS))}; nhắn DesignAdvisor, kể cả node đã chốt)"] + [
            f"- {n['path'] or '(gốc)'}: {n['title']}" for n in owed]
    if not path and snap["missing"]:
        out += ["", "## Thiếu feature hạ tầng (vòng 2 luôn phải có)"] + [f"- {p['path']}: {p['text']}" for p in snap["missing"]]
    if not path and snap["missingCode"]:
        out += ["", "## Thiếu code.json (vòng 2: tự đặt theo key, không cần hỏi người)"] + [
            f"- {p['path']}: {p['text']}" for p in snap["missingCode"]]
    if not path and snap["termNotes"]["idea"]:
        out += ["", f"## Thuật ngữ (node {gd.GLOSSARY_KEY}; nhắc, không chặn)"] + [
            f"- {p['path']}: {p['text']}" for p in snap["termNotes"]["idea"]]
    return "\n".join(out)


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    if len(argv) > 2:
        print(f"Cách dùng: python Management/tools/gd_status.py [{PREFIX}/<node>]", file=sys.stderr)
        return 2
    try:
        arg = argv[1] if len(argv) == 2 else ""
        name = arg.replace("\\", "/").rstrip("/")[len(PREFIX) + 1:]
        print(gd.emit(root, "status", name or "all", report(root, arg), STATUS_NOTE))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không đọc được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
