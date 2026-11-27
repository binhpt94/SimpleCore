#!/usr/bin/env python3
"""Prints where the proposed game-design tree stands. For @GameDesigner, at the start of a turn. Reads only.

    python Management/tools/gd_status.py                     the whole tree
    python Management/tools/gd_status.py ai/idea/<path>      one branch
    python Management/tools/gd_status.py [ai/idea/<path>] --brief
                                                             only what needs work: a node that passes and waits for
                                                             nobody is left out, and so is "Bám theo"

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
import reqbook  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
STATUS_NOTE = ("Đọc trước: các node mà mục lục ghi Kết luận CHƯA ĐẠT, và các mục cuối file (Lỗi cấu trúc, Chưa hỏi GPT, Thiếu..., "
               "Thuật ngữ). Trước khi viết hay sửa một node vẫn chạy lệnh này cho riêng node đó.")


def quiet(node):
    """A node --brief leaves out: it passes, and nobody has work on it (no frame to draw, no ref to pin again, no
    advice to write in, no clash, no GPT question owed)."""
    gpt = node.get("gpt") or {}
    return bool(node["passed"] and not node.get("uiWait") and not node["refMissing"] and not node.get("refStale")
                and not node["stale"] and not gpt.get("missing") and not gpt.get("newMissing") and not node.get("clashOpen")
                and not node.get("gptOwed"))


def lines_for(root, node, out, brief=False, hidden=None):
    if brief and quiet(node):
        hidden.append(node["path"] or "(gốc)")
        for child in node["children"]:
            lines_for(root, child, out, brief, hidden)
        return
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
               + ("; đã được duyệt" if node.get("accepted") else "; chờ duyệt" if node.get("toApprove") else "")
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
    if node.get("cheat"):
        out.append(f"- Node cheat: {len(node['items'])}/{gd.CHEAT_MAX} state, mỗi state một frame trùng id; "
                   "state dựng nhanh ở bản dev")
    if node.get("ui"):
        out += gd.ui_lines(node["ui"], "- ")
    for text in node.get("uiWait") or []:
        out.append(f"- CHỜ @UIDesigner: {text}")
    for fi in node.get("frameItems") or []:
        out.append(f"- Điểm frame {fi['file']}: AI {fi['ai'] if fi['ai'] is not None else '–'}, người "
                   f"{fi['user'] if fi['user'] is not None else 'chưa chấm'}" + (f" ({fi['feedback']})" if fi.get("feedback") else ""))
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
        nb = node["nodeBy"]
        out.append(f"- Node do GPT đề xuất (lần hỏi "
                   + (f"{nb['folder']}/{nb['advice']}#{nb['pick']}" if nb.get("folder") else nb["advice"]) + ")")
    old = [a for a in asked if not gd.answer_filtered(advice / f"{a}.md")]
    if old:
        out.append("- Góp ý của GPT, lần hỏi cũ chưa lọc (mỗi góp ý phải thành một ý, không được bỏ; xem "
                   "skill gd-integrate-advice): " + ", ".join(f"{where}/{a}.md" for a in old))
    if len(old) < len(asked):
        out.append("- Lần hỏi đã lọc (ý giữ nằm ở node đích của nó): "
                   + ", ".join(f"{where}/{a}.pick.md" for a in asked if a not in old))
    if node.get("group") == "infra":
        out.append("- Nhóm: hạ tầng (không cần hỏi GPT; hỏi khi bạn thấy cần)")
    elif node.get("group") == "test":
        out.append("- Nhóm: test (không hỏi GPT, kể cả node con và node cheat)")
    if node.get("gptOwed"):
        out.append(f"- CHƯA HỎI GPT (bắt buộc ở vòng {' và '.join(map(str, gd.ADVICE_ROUNDS))}, trừ feature hạ tầng, test): nhờ DesignAdvisor "
                   f"qua hộp thư (python Management/tools/dispatch.py send @DesignAdvisor --from GameDesigner \"<node> · <hỏi gì>\") hỏi GPT về node này; câu trả lời sẽ nằm ở {where}/<id>.md")
    gpt = node.get("gpt") or {}
    for m in gpt.get("missing", []):
        out.append(f"- {'Ý GIỮ' if m.get('pick') else 'GÓP Ý'} CHƯA VÀO NODE: {m['advice']}#{m['point']}: {m['title']} "
                   f"-> viết thành một ý, khai by: <id ý>=gpt@{m['advice']}#{m['point']}")
    for p in gpt.get("newMissing", []):
        out.append(f"- NODE MỚI CHƯA TẠO: {p['target']} (ý giữ {p['key']}: {p['title']}) -> tạo node, khai node-by: gpt@{p['key']}")
    if gpt.get("side"):
        out.append(f"- Bổ sung từ GPT chờ người lọc: {len(gpt['side'])} ý (người chuyển lên hay xoá trên trang; bạn không "
                   "viết chúng vào node): " + "; ".join(p["title"] for p in gpt["side"]))
    for d in gpt.get("dropped", []):
        out.append(f"- Ý của GPT đã bị loại: {d['advice']}#{d['point']} (ý {d['item']}): {d['title']}")
    titles = {x["id"]: x["title"] for x in node["items"]}
    for it in node["items"]:
        user = "chưa chấm" if it["user"] is None else str(it["user"])
        out.append(f"- Ý {it['id']}{gd.by_label(it)}{(' ' + gd.USER_IDEA_TAG) if it.get('by') == gd.BY_USER else ''}: AI {it['ai']}, người {user}" + (f", phản hồi: {it['feedback']}" if it["feedback"] else "")
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
    side_deleted = gd.pick_deleted(root, node["path"])
    if side_deleted:
        out.append("- Người đã xoá ở phần bổ sung từ GPT (người đã loại: đừng đề xuất lại):")
        for e in side_deleted:
            out.append(f"    {e['key']}: {' '.join(e['text'].split())[:160]}")
    deleted = gd.human_deleted(folder)
    if deleted:
        out.append("- Người đã xoá ở node này (trung tính: không viết lại, không đưa lại):")
        for e in deleted:
            out.append(f"    [{e.get('at', '')}] {e.get('item', '')}: {' '.join(str(e.get('text', '')).split())[:160]}")
    for child in node["children"]:
        lines_for(root, child, out, brief, hidden)


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


def hash_lines(root, arg=""):
    """--hashes: one line per node of the branch (the whole tree without a path): its data hash, whether based-on and
    each ref still match, what it waits for. Short, so printed as it is."""
    path = ""
    if arg:
        arg = arg.replace("\\", "/").rstrip("/")
        if arg != PREFIX and not arg.startswith(PREFIX + "/"):
            raise ValueError(f"đường dẫn phải có dạng {PREFIX}/<node>")
        path = arg[len(PREFIX) + 1:]
    gd.check_path(path)
    top = next((n for n in gd.walk(gd.snapshot(root)["idea"]) if n["path"] == path), None)
    if top is None:
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    rows, off = [], False
    for n in gd.walk(top):
        parts = []
        if n["path"]:
            parts.append(f"based-on LỆCH (ghi {n['basedOn'] or 'chưa có'}, cha {n['parentHash']})" if n["stale"] else "based-on khớp")
            off = off or n["stale"]
        for r in n["refs"]:
            name = r["label"].rpartition("/")[2]
            parts.append(f"ref {name} " + ("không có node này" if r["ok"] is None else "khớp" if r["ok"]
                                           else f"LỆCH (ghi {r['hash']}, giờ {r['current']})"))
            off = off or r["ok"] is False
        for r in n["refMissing"]:
            parts.append(f"thiếu ref {r['label']}")
            off = True
        if n.get("uiWait"):
            parts.append("CHỜ @UIDesigner")
        if n.get("gptOwed"):
            parts.append("CHƯA HỎI GPT")
        rows.append((n["path"] or "(gốc)", f"hash {n['dataHash']}  " + " · ".join(parts)))
    width = max(len(k) for k, _ in rows)
    out = [f"{k.ljust(width)}  {v}".rstrip() for k, v in rows]
    if off:
        out += ["", f"Ghim: python Management/tools/gd_pin.py {arg or PREFIX} --all (nhánh này và mọi node ref tới nó; "
                    "cả cây: gd_pin.py --all; in diff để rà, rồi lệnh ghim)"]
    return "\n".join(out)


def report(root, arg="", brief=False):
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
    mode = "User control (người chấm hết, mọi điểm từ %d)" if snap["mode"] == "user" else "Full Auto (điểm hiệu lực từ %d; node đạt tự được duyệt và lên live)"
    out = [f"# Game design: {mode % gd.PASS_MIN}",
           "Thang điểm: " + " · ".join(f"{b['min']}-{b['max']} {b['label']}" for b in gd.BANDS),
           "Độ quan trọng (chỉ chấm ở node lá): " + " · ".join(f"{x['min']}-{x['max']} {x['name']}: {x['label']}" for x in gd.PHASES)]
    if path and not brief:
        out += context_lines(snap, node)
    elif not path:
        out += ["", f"## Cổng code theo feature (chỉ code khi thiết kế tới vòng {len(gd.ROUNDS)} trong live)"] + gate_lines(snap, root) + [""]
    hidden = []
    lines_for(root, node, out, brief, hidden)
    if hidden:
        out += ["", f"({len(hidden)} node đạt, không ai còn việc, ẩn bởi --brief; chạy không --brief để xem đủ)"]
    problems = [p for p in snap["problems"]["idea"] if not path or p["path"] == path or p["path"].startswith(path + "/")]
    if problems:
        out += ["", "## Lỗi cấu trúc (phải sửa trước)"] + [f"- {p['path']}: {p['text']}" for p in problems]
    # what waits for the human's approval (mode user) and what still pins an old hash (GD's work)
    pending = [n for n in gd.walk(node) if n.get("toApprove")]
    stale = [n for n in gd.walk(node) if n.get("stale") or any(r["ok"] is False for r in n.get("refs", []))]
    if pending or stale:
        out += ["", "## Chờ duyệt và còn ghim hash cũ"]
        if pending:
            out.append(("- Đạt, chờ người chốt: " if snap["mode"] == "user" else "- Đạt, tự lên live ở lần dựng kế tiếp: ")
                       + ", ".join(n["path"] or "(gốc)" for n in pending))
        if stale:
            out.append("- Còn ghim hash cũ (đọc lại node cha hay node được ref rồi gd_pin.py; nhiều nhánh: gd_pin.py --all): "
                       + ", ".join(n["path"] or "(gốc)" for n in stale))
    # the stage the project is in: which nodes still have to be written down to the last round, which only wait
    stage = snap["stage"]["stage"]
    names = {ph["key"]: ph["name"] for ph in gd.PHASES}
    if not stage:
        out += ["", "## Giai đoạn đang làm: CHƯA ĐẶT (người đặt ở tab Game design; cho tới lúc đó mọi node coi như tới hạn, "
                    "và @CodeLead chưa giao việc dev)"]
    else:
        deepen = [n for n in gd.walk(node) if n.get("deepen")]
        later = [n for n in gd.walk(node) if not n.get("due", True) and not n["children"]]
        out += ["", f"## Giai đoạn đang làm: {names[stage]} (chỉ node của giai đoạn này và trước đó phải tới vòng {len(gd.ROUNDS)})"]
        out.append(f"### Cần thiết kế tới vòng {len(gd.ROUNDS)} ({len(deepen)})")
        out += [f"- {n['path'] or '(gốc)'} (đang ở vòng {n['round']}, {names.get(n['priority'].get('phase'), 'chưa chấm độ quan trọng')}): {n['title']}"
                for n in deepen] or ["- không còn node nào"]
        out.append(f"### Để sau: thuộc giai đoạn sau, không đào sâu thêm, yêu cầu code chưa giao ({len(later)})")
        out += [f"- {n['path']} ({names.get(n['priority'].get('phase'), n['priority'].get('phase'))}, vòng {n['round']}): {n['title']}"
                for n in later] or ["- không có"]
    unpicked = [u for u in gd.pick_index(Path(root).joinpath(*gd.BASE))["unchecked"]
                if not path or u["folder"] == gd.advice_name(path)]
    if unpicked:
        out += ["", f"## Chưa lọc góp ý GPT (việc của @DesignAdvisor: viết <id>.pick.md, chạy gd_pick.py; skill advisor-ask-gpt)"] + [
            f"- {gd.ADVICE_DIR}/{u['folder']}/{u['aid']}: {u['why']}" for u in unpicked]
    owed = [n for n in gd.walk(node) if n.get("gptOwed")]
    if owed:
        out += ["", f"## Chưa hỏi GPT (bắt buộc ở vòng {' và '.join(map(str, gd.ADVICE_ROUNDS))}, trừ feature hạ tầng, test; nhờ DesignAdvisor qua hộp thư dispatch.py send, kể cả node đã chốt)"] + [
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


def req_tail(root, name):
    """' · yêu cầu: 3/29 đã kiểm · ...' from the requirement book of a code folder (reqbook.py)."""
    try:
        v = reqbook.view(root, name)
    except (ValueError, OSError):
        return ""
    return f" · yêu cầu: {reqbook.counts_line(v)}" if v["total"] else ""


DESIGN_TEXT = {"redo": "cần làm lại (@GameDesigner)", "scoring": "chờ người chấm hoặc trả lời",
               "lock": "đạt, chờ người chốt", "live": "đã chốt"}


def gate_lines(snap, root=None):
    """The code gate of every feature, one line each: what @Manager reads before it hands out work. A feature may be
    coded only when its branch reached the last round in live (gamedesign.code_allowed). The rows are
    gamedesign.feature_rows, the same the page's table shows."""
    out = []
    for row in snap.get("features") or []:
        done, total = row["leaves"]
        tail = (f" · thiết kế đề xuất: {DESIGN_TEXT[row['design']]}"
                + (f", {row['clash']} nhóm ý trái nhau chờ người chọn" if row["clash"] else "")
                + (f" · {gd.PHASES[0]['name']}: {done}/{total} node lá done" if total else ""))
        if not row["declared"]:
            out.append(f"- (chưa khai thư mục code) ({row['path']}): ĐANG KHOÁ" + tail)
            continue
        gate = row["gate"]
        out.append(f"- {row['unity']} ({row['path']}): " + ("ĐƯỢC CODE" if gate["ok"] else "ĐANG KHOÁ, " + gate["reason"]) + tail
                   + (req_tail(root, row["unity"]) if root is not None and gate["ok"] else ""))
    return out


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    brief = "--brief" in argv[1:]
    hashes = "--hashes" in argv[1:]
    argv = [a for a in argv if a not in ("--brief", "--hashes")]
    if len(argv) > 2:
        print(f"Cách dùng: python Management/tools/gd_status.py [{PREFIX}/<node>] [--brief | --hashes]", file=sys.stderr)
        return 2
    try:
        arg = argv[1] if len(argv) == 2 else ""
        if hashes:
            print(hash_lines(root, arg))
            return 0
        name = arg.replace("\\", "/").rstrip("/")[len(PREFIX) + 1:]
        print(gd.emit(root, "status-brief" if brief else "status", name or "all", report(root, arg, brief), STATUS_NOTE))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không đọc được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
