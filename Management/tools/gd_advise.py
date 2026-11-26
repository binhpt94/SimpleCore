#!/usr/bin/env python3
"""Asks GPT (Codex CLI) one question about the game design. For @DesignAdvisor (and the human).

    python Management/tools/gd_advise.py <node> <id>

<node>  the design node the question is about: its path with '.' for '/' (feature-hero.hero-list), `_root` for the
        round-1 node, or `_general` for a question about no node in particular.
<id>    the id of this advice (lower case, digits, '-').

The question is the file Management/game-design/advice/<node>/<id>.q.md, written before this is run.
What GPT is given: the question, and the node with the nodes above it written out again: title, summary and every
idea with the HUMAN's score, what that score means on the project's scale, and the human's feedback. The AI's own
scores are left out: they are Claude's opinion of its own ideas and would only lead GPT. GPT may read the rest of
Management/game-design itself (Codex runs read-only at the project root: it writes nothing).
So that GPT does not answer one node blind, the command adds by itself (context_text): the map of the features
(every round-2 node: title, summary, the titles of its ideas), the nodes beside the one asked about, everything
decided below it (and whether it is fixed in live), the nodes its refs name, what the human turned down at the node,
and the points GPT gave before about it and the nodes above. Nobody copies those into a question.
GPT is told it may propose new features and nodes (and merging or splitting them) when the node asked about is in
rounds 1-3 or the question is about no node, and that it may not at the last round (new_things).
The question may start with a line `also: <node>, <node>` (at most ALSO_MAX): those nodes are given in full, like the
node asked about. The answer still belongs to the one node: its points name only that node's ideas.
What comes back, in advice/<node>/<id>.md: a head (node, the node's data hash when asked, the time), the question,
and GPT's points, each ONE idea:
    ### <point id> | <thêm | làm rõ <idea id> | thay <idea id>> | <the idea in one sentence>
    <reasons, short>
thêm: an idea the node lacks · làm rõ: an idea that is unclear, said clearly · thay: another way than that idea.
An answer whose points are not of that form, are too long (one point is one decision), or name an idea the node
does not have is kept as <id>.rejected.md and the command fails: run it again. An earlier answer of the same id is
kept as <id>.v<n>.md. There is no cap on runs.
Only nodes of rounds 2 and 3 (gamedesign.ASK_ROUNDS) and _general are asked; the root and round 4 are refused
(người quyết 2026-11-27). The answer carries `filter: pick`: @DesignAdvisor then keeps at most PICK_MAX ideas in
<id>.pick.md, each with the node it belongs to, gives every other point a reason, and runs gd_pick.py; the kept
ideas are what goes into the design (gamedesign.py, "picking GPT's points"). Answers written before that, without
`filter: pick`, keep the earlier rule: every point becomes an idea of the node asked. See
.claude/rules/designadvisor.md.
Tests: python Management/tools/test_gd_advise.py
"""
import re
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import studio  # noqa: E402

ROOT = HERE.parent.parent
ID_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
NODE_RX = re.compile(r"^(_general|_root|[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)*)$")
INLINE_MAX = 60000      # characters of design text put into the prompt; GPT reads the rest itself
ALSO_MAX = 4            # nodes a question may add in full with its `also:` line
ALSO_RX = re.compile(r"^also:[ \t]*(.*)$", re.I)
ASK = f"""Bạn là người thiết kế game thứ hai của project này, ngang hàng với người thiết kế thứ nhất (Claude). Mỗi góp ý
của bạn sẽ thành một ý trong thiết kế và được chấm điểm như ý của Claude; người chủ project quyết định cuối.
Thiết kế nằm ở Management/game-design: `live/` là bảng đã chốt, `ai/idea/` là bảng đang đề xuất; mỗi node có data.md
(các ý cốt lõi) và CLAUDE.md (diễn giải). Bạn chạy ở chế độ chỉ đọc: được đọc thêm các file trong
Management/game-design khi cần, không tạo hay sửa file nào.

Về điểm số: phần thiết kế bên dưới ghi điểm NGƯỜI chấm cho từng ý kèm nghĩa của điểm; đó là ý kiến của người chủ
project, hãy dựa vào nó (ý bị chấm thấp cần phương án khác; ý chấm cao thì đừng cãi lại nếu không có lí do mạnh).
Trong các file bạn tự đọc, số sau chữ `AI` ở dòng `### <id> | AI <n>` là điểm AI tự chấm: KHÔNG đáng tin, bỏ qua.

Việc cần làm, theo thứ tự ưu tiên:
1. BỔ SUNG ý mà node còn thiếu để chơi được và để người code không phải đoán.
2. LÀM RÕ ý còn mơ hồ: viết lại nó thành một câu rõ, đo được, không hiểu được hai cách.
3. Đề xuất phương án THAY cho ý bạn thấy sai hoặc yếu, nhất là ý người chấm thấp.
Ý nào đã ổn thì không viết gì về nó. Ý ghi "Ý CỦA NGƯỜI CHỦ PROJECT" là quyết định của người: KHÔNG `thay`, KHÔNG
`làm rõ` nó; các ý khác phải khớp với nó.

Phần "Ngữ cảnh rộng hơn" cho biết các feature khác, các node bên cạnh, thứ đã quyết ở các node bên dưới, ý người chủ
project đã loại và các góp ý bạn đã cho trước đây. Dùng nó để: KHÔNG đề xuất ngược với thứ đã quyết ở node bên dưới hay
ở feature khác (nếu thấy thứ đó sai thì nói rõ trong lí do là góp ý này đòi sửa node nào); KHÔNG đề xuất lại ý đã bị
loại; KHÔNG lặp lại góp ý đã cho; không đưa vào node này việc thuộc feature khác. Góp ý nào đụng tới feature hay node
khác thì nêu đường dẫn của nó trong phần lí do.

Trả lời bằng tiếng Việt, CHỈ gồm các góp ý rời nhau. Mỗi góp ý là MỘT quyết định duy nhất (hai quyết định thì tách
hai góp ý), đúng dạng:

### <id góp ý: chữ thường, số, gạch nối, không trùng> | <loại> | <ý đó nói gọn trong một câu, tối đa {gd.IDEA_TITLE_MAX} kí tự>
<lí do và hệ quả, tối đa {gd.IDEA_DETAIL_MAX} kí tự>

<loại> là một trong ba: `thêm` · `làm rõ <id ý>` · `thay <id ý>` (id ý là id của một ý trong node được hỏi).
Câu sau dấu `|` cuối phải là chính ý đó, viết như một ý của thiết kế (ví dụ "Mỗi trận tối đa 5 hero"), không viết
"nên xem xét...". Không viết lời mở đầu, kết luận hay dòng `###` nào khác."""


# What GPT may propose beyond the ideas of the node, by the round of the node asked about: new features and nodes in
# rounds 1-3 (and in a question about no node), nothing new at the last round.
NEW_ALLOWED = """Bạn ĐƯỢC đề xuất thứ mới, không chỉ sửa ý đang có: feature mới còn thiếu, node con mới cho node được hỏi, gộp hoặc
tách feature hay node. Viết bằng loại `thêm`, câu nêu ý bắt đầu bằng "Feature mới:", "Node mới:", "Gộp:" hoặc "Tách:".
Danh sách feature và node hiện có không phải là giới hạn. Chỉ khi câu hỏi ghi rõ NGƯỜI CHỦ PROJECT yêu cầu khác thì
theo yêu cầu đó."""
NEW_FORBIDDEN = """Node được hỏi ở vòng cuối (thông số chi tiết): KHÔNG đề xuất feature hay node mới, không gộp hay tách node. Chỉ
thêm, làm rõ hoặc thay ý trong chính node này."""


def new_things(node, found):
    """The paragraph on new features and nodes for a question about `node` (its round decides)."""
    depth = 0 if not found else 1 if node == gd.ADVICE_ROOT else len(node.split(".")) + 1
    return NEW_FORBIDDEN if depth >= len(gd.ROUNDS) else NEW_ALLOWED


def fail(text):
    print("LỖI: " + text)
    return 1


CODEX_STOP = "DỪNG: không chạy lại, không đi đường khác; báo bên giao việc MỘT tin rồi kết thúc lượt."


def codex_fail(text):
    """Codex itself failed (người quyết 2026-11-26: dừng ngay, không cố): the mark the hook tool_fail.py looks for."""
    print(f"CODEX LỖI: {text}. {CODEX_STOP}")
    return 1


def node_folders(root, node):
    """[(table name, folder)] of the node in ai/idea and in live, the ones that exist; [] for _general."""
    if node == gd.ADVICE_GENERAL:
        return []
    parts = [] if node == gd.ADVICE_ROOT else node.split(".")
    found = []
    for table in ("idea", "live"):
        folder = gd.table_dir(root, table).joinpath(*parts)
        if (folder / gd.DATA_FILE).is_file():
            found.append((table, folder))
    return found


def node_text(root, table, parts, role):
    """One node written out for GPT: title, summary, and every idea with the human's score and what it means.
    Never the AI's score."""
    folder = gd.table_dir(root, table).joinpath(*parts)
    data = gd.read_data(folder)
    if data is None:
        return ""
    gd.merge_review(gd.table_dir(root, "idea").joinpath(*parts), data["items"])      # the human's review lives with the idea node
    out = [f"----- node {'/'.join(parts) or '(gốc)'} · bảng {'/'.join(gd.TABLES[table])} · {role}",
           f"Tên: {data['title']}", f"Tóm tắt: {data['summary']}", "Các ý cốt lõi:"]
    for it in data["items"]:
        if it["user"] is None:
            score = "người chưa chấm"
        else:
            score = "người chấm " + gd.score_meaning(it["user"]) + (f" · phản hồi của người: {it['feedback']}" if it["feedback"] else "")
        mine = " · Ý CỦA NGƯỜI CHỦ PROJECT, không thay, không làm rõ, không bỏ" if it.get("by") == gd.BY_USER else ""
        out.append(f"- [{it['id']}] {it['title']} ({score}{mine})")
        out += [f"    {line}" for line in it["detail"].split("\n") if line.strip()]
    if not data["items"]:
        out.append("- (chưa có ý nào)")
    return "\n".join(out)


def one_line(text):
    return " ".join(text.split())


def brief(folder, name, ideas=True, mark=""):
    """A node in a few lines: path, title, summary and (ideas) the titles of its ideas. Never a score."""
    data = gd.read_data(folder)
    if data is None:
        return ""
    out = [f"- {name}{mark}: {one_line(data['title'])}. {one_line(data['summary'])}"]
    if ideas:
        out += [f"    · [{it['id']}] {one_line(it['title'])}" for it in data["items"]]
    return "\n".join(out)


def child_folders(folder):
    return sorted(p for p in folder.iterdir() if p.is_dir() and not p.name.startswith(".") and (p / gd.DATA_FILE).is_file())


def split_question(text):
    """(the question without its `also:` line, the nodes that line names with '.' for '/')."""
    lines = text.strip().split("\n")
    m = ALSO_RX.match(lines[0].strip()) if lines else None
    if not m:
        return text.strip(), []
    names = [n.strip().strip("`").replace("/", ".") for n in m.group(1).split(",") if n.strip()]
    return "\n".join(lines[1:]).strip(), names


def context_text(root, node, found, aid, skip, level):
    """What the command adds so GPT sees more than one branch. skip: node names already given in full.
    level 0: everything; 1: the feature map without ideas; 2: the nodes below without ideas too."""
    root = Path(root)
    table = found[0][0] if found else "idea"
    top = gd.table_dir(root, table)
    parts = node.split(".") if found and node != gd.ADVICE_ROOT else []
    folder = top.joinpath(*parts)
    live_top = gd.table_dir(root, "live")
    out = []

    features = [brief(f, f.name, ideas=level < 1) for f in (child_folders(top) if top.is_dir() else [])
                if f.name not in skip and f.name != (parts[0] if len(parts) == 1 else "")]
    if any(features):
        out.append("--- Bản đồ feature (mọi node vòng 2; node được hỏi và node liên quan đã ghi đầy đủ ở trên)\n"
                   + "\n".join(t for t in features if t))
    if len(parts) >= 2:
        beside = [brief(f, "/".join(parts[:-1] + [f.name]), ideas=False) for f in child_folders(folder.parent) if f != folder]
        if any(beside):
            out.append("--- Node bên cạnh (cùng node cha)\n" + "\n".join(t for t in beside if t))
    if found:
        below = []

        def visit(f, path):
            for c in child_folders(f):
                p = path + [c.name]
                fixed = gd.read_data(live_top.joinpath(*p)) if table == "idea" else None
                same = table == "live" or (fixed is not None and fixed["hash"] == gd.read_data(c)["hash"])
                below.append(brief(c, "/".join(p), ideas=level < 2, mark=" (ĐÃ CHỐT ở live)" if same else " (đề xuất, chưa chốt)"))
                visit(c, p)
        visit(folder, parts)
        if below:
            out.append("--- Đã quyết ở các node bên dưới node được hỏi (đừng đề xuất ngược)\n" + "\n".join(below))
        data = gd.read_data(folder)
        doc = gd.read_text(folder / gd.DOC_FILE) if (folder / gd.DOC_FILE).is_file() else ""
        raw = gd.read_text(folder / gd.DATA_FILE)
        named, seen = [], set()
        for r in gd.ref_issues(top, "/".join(parts), data, raw + "\n" + doc)["refs"]:
            if r["ok"] is None or r["path"] in seen or r["path"] == gd.GLOSSARY_KEY:
                continue
            seen.add(r["path"])
            named.append(brief(top.joinpath(*r["path"].split("/")), r["path"]))
        if any(named):
            out.append("--- Node mà node được hỏi trỏ tới (refs)\n" + "\n".join(t for t in named if t))
        turned = gd.rejected(gd.table_dir(root, "idea").joinpath(*parts))
        if turned:
            out.append("--- Ý người chủ project ĐÃ LOẠI ở node này (đừng đề xuất lại)\n" + "\n".join(
                f"- [{e.get('item', '')}] {one_line(str(e.get('text', '')))[:200]} (người chấm {e.get('score')}"
                + (f"; phản hồi: {one_line(str(e['feedback']))}" if e.get("feedback") else "") + ")" for e in turned))
        dropped_by_human = gd.human_deleted(gd.table_dir(root, "idea").joinpath(*parts))
        if dropped_by_human:
            out.append("--- Ý người chủ project ĐÃ XOÁ ở node này (không tốt không xấu, chỉ là không cần: đừng đề xuất lại)\n"
                       + "\n".join(f"- [{e.get('item', '')}] {one_line(str(e.get('text', '')))[:200]}" for e in dropped_by_human))
        # Kept ideas waiting beside a fixed node that the human deleted: turned down (người quyết 2026-11-27).
        side_deleted = [(p, e) for d in range(len(parts), 0, -1) for p in ["/".join(parts[:d])]
                        for e in gd.pick_deleted(root, p)]
        if side_deleted:
            out.append("--- Ý người chủ project ĐÃ XOÁ ở phần bổ sung từ GPT (ý GPT đề xuất trước đây, người đã loại: đừng "
                       "đề xuất lại)\n" + "\n".join(f"- [{p}] {one_line(e['text'])[:200]}" for p, e in side_deleted))
    asked = []
    names = ([gd.ADVICE_ROOT] + [".".join(parts[:d]) for d in range(1, len(parts) + 1)]) if found else [gd.ADVICE_GENERAL]
    for name in names:
        for other, points in gd.read_advice(root.joinpath(*gd.BASE, gd.ADVICE_DIR, name)).items():
            if name == node and other == aid:
                continue      # the answer this run replaces
            asked += [f"- {name}/{other}#{p['id']} ({p['kind']}{' ' + p['target'] if p['target'] else ''}): {one_line(p['title'])}"
                      for p in points]
    if asked:
        out.append("--- Góp ý bạn (GPT) đã cho trước đây về node này và các node phía trên (đừng lặp lại)\n" + "\n".join(asked))
    return "\n\n".join(out)


def design_text(root, node, found, also=(), aid=""):
    """(design, context). design: the nodes above (from the root down), the node asked about (the proposed one when
    there is one, else the fixed one), its CLAUDE.md, and the `also` nodes in full; never cut. context: what
    context_text adds, made shorter until both fit INLINE_MAX."""
    out = []
    if found:
        table, folder = found[0]
        parts = [] if node == gd.ADVICE_ROOT else node.split(".")
        out = [node_text(root, table, parts[:depth], "node phía trên") for depth in range(len(parts))]
        out.append(node_text(root, table, parts, "NODE ĐƯỢC HỎI"))
        if (folder / gd.DOC_FILE).is_file():
            out.append(f"----- diễn giải của node được hỏi ({gd.DOC_FILE})\n" + gd.read_text(folder / gd.DOC_FILE).strip())
    else:      # a question about no node still sees the game
        table = "idea" if (gd.table_dir(root, "idea") / gd.DATA_FILE).is_file() else "live"
        out.append(node_text(root, table, [], "node gốc"))
    for name, (other_table, _) in also:
        out.append(node_text(root, other_table, name.split("."), "node liên quan (bên hỏi gửi kèm)"))
    design = "\n\n".join(t for t in out if t)
    skip = {name for name, _ in also}
    context = ""
    for level in (0, 1, 2):
        context = context_text(root, node, found, aid, skip, level)
        if len(design) + len(context) <= INLINE_MAX:
            return design, context
    room = max(0, INLINE_MAX - len(design))
    return design, context[:room] + "\n[... cắt bớt: đọc phần còn lại trong file]"


def answer_problems(answer, idea_ids):
    """(points, why the answer cannot be filed): its points must be single, short ideas of a known kind that name
    only ideas the node has."""
    points, errors = gd.advice_points(answer)
    if not points and not errors:
        errors.append("không có góp ý nào dạng '### <id> | <loại> | <ý>'")
    for p in points:
        if len(p["title"]) > gd.IDEA_TITLE_MAX:
            errors.append(f"góp ý '{p['id']}': câu nêu ý dài {len(p['title'])} kí tự (tối đa {gd.IDEA_TITLE_MAX})")
        if len(p["detail"]) > gd.IDEA_DETAIL_MAX:
            errors.append(f"góp ý '{p['id']}': lí do dài {len(p['detail'])} kí tự (tối đa {gd.IDEA_DETAIL_MAX})")
        if p["target"] and p["target"] not in idea_ids:
            errors.append(f"góp ý '{p['id']}': node không có ý '{p['target']}' để {p['kind']}")
    return points, errors


def build_prompt(root, node, found, question, also=(), aid=""):
    """Everything Codex is given on stdin."""
    design, context = design_text(root, node, found, also, aid)
    return (ASK + "\n\n" + new_things(node, found) + "\n\n===== Câu hỏi\n" + question
            + ("" if found else "\n\n(Câu hỏi không gắn node nào: chỉ dùng loại `thêm`.)")
            + "\n\n===== Thiết kế liên quan (bản lúc hỏi; điểm là điểm NGƯỜI chấm)\n" + design
            + ("\n\n===== Ngữ cảnh rộng hơn (lệnh tự kèm, không có điểm; để không đề xuất trùng hay ngược)\n" + context if context else ""))


def main(argv, root=ROOT):
    if len(argv) != 3 or not NODE_RX.match(argv[1]) or not ID_RX.match(argv[2]):
        return fail("cách dùng: python Management/tools/gd_advise.py <node với '.' thay '/', _root hoặc _general> <id>")
    root, node, aid = Path(root), argv[1], argv[2]
    folder = root.joinpath(*gd.BASE, gd.ADVICE_DIR, node)
    question_file = folder / f"{aid}.q.md"
    where = "/".join(folder.relative_to(root).parts)
    if not question_file.is_file() or not gd.read_text(question_file).strip():
        return fail(f"chưa có câu hỏi: viết {where}/{aid}.q.md trước (Write), rồi chạy lại")
    if node == gd.ADVICE_ROOT:
        return fail("vòng 1 (node gốc) không hỏi GPT (người quyết 2026-11-27): chỉ hỏi node vòng 2, vòng 3 hoặc "
                    f"{gd.ADVICE_GENERAL}")
    if node != gd.ADVICE_GENERAL and len(node.split(".")) + 1 not in gd.ASK_ROUNDS:
        return fail(f"vòng {len(node.split('.')) + 1} không hỏi GPT (người quyết 2026-11-27): chỉ hỏi node vòng 2, "
                    f"vòng 3 hoặc {gd.ADVICE_GENERAL}")
    found = node_folders(root, node)
    if node != gd.ADVICE_GENERAL and not found:
        return fail(f"không có node '{node.replace('.', '/')}' trong ai/idea hay live "
                    f"(câu hỏi chung viết {gd.ADVICE_GENERAL})")
    question, also_names = split_question(gd.read_text(question_file))
    if not question:
        return fail(f"{where}/{aid}.q.md chỉ có dòng also: viết câu hỏi bên dưới dòng đó")
    also = []
    for name in also_names:
        other = node_folders(root, name) if NODE_RX.match(name) and name not in (gd.ADVICE_GENERAL, gd.ADVICE_ROOT, node) else []
        if not other:
            return fail(f"also: không dùng được '{name.replace('.', '/')}': phải là một node có trong ai/idea hay live, khác node "
                        "đang hỏi và khác node gốc (node phía trên và node được hỏi đã tự được gửi)")
        also.append((name, other[0]))
    if len(also) > ALSO_MAX:
        return fail(f"also: tối đa {ALSO_MAX} node mỗi lần hỏi (đang có {len(also)}); bản đồ feature đã tự được gửi kèm")
    try:
        exe = studio.tool_exe("codex")
    except ValueError as ex:
        return codex_fail(str(ex))
    prompt = build_prompt(root, node, found, question, also, aid)
    with tempfile.TemporaryDirectory(prefix="gd-advise-") as tmp:
        out = Path(tmp) / "answer.md"
        code, err = studio.run(exe + ["exec", "-C", str(root), "-s", "read-only", "--skip-git-repo-check",
                                      "-o", str(out), "-"], prompt)
        answer = gd.read_text(out).strip() if out.is_file() else ""
    if not answer:
        return codex_fail(f"Codex không trả lời (mã {code}): {' '.join(err.split())[-400:]}")
    node_data = gd.read_data(found[0][1]) if found else None
    points, errors = answer_problems(answer, {it["id"] for it in node_data["items"]} if node_data else set())
    if errors:
        (folder / f"{aid}.rejected.md").write_text(answer + "\n", encoding="utf-8", newline="\n")
        return fail("câu trả lời không dùng được (" + "; ".join(errors[:6]) + f"). Đã giữ ở {where}/{aid}.rejected.md. "
                    "Chạy lại lệnh.")
    target = folder / f"{aid}.md"
    if target.is_file():      # keep what was said before
        n = 1
        while (folder / f"{aid}.v{n}.md").exists():
            n += 1
        target.replace(folder / f"{aid}.v{n}.md")
    head = ["---", f"advice: {aid}"]
    if found:
        head += [f"node: {node.replace('.', '/') if node != gd.ADVICE_ROOT else '(gốc)'}",
                 f"table: {'/'.join(gd.TABLES[found[0][0]])}", f"hash: {node_data['hash']}"]
    else:
        head.append("node: (không gắn node nào)")
    if also:
        head.append("also: " + ", ".join(name.replace(".", "/") for name, _ in also))
    head += [f"asked: {gd.now()}", "by: gpt (Codex CLI)", f"filter: {gd.PICK_FILTER}", "---"]
    target.write_text("\n".join(head) + f"\n# Góp ý {aid}\n\n## Câu hỏi\n\n{question}\n\n## {gd.ADVICE_TITLE}\n\n{answer}\n",
                      encoding="utf-8", newline="\n")
    print(f"Đã ghi {where}/{aid}.md ({len(points)} góp ý). Tiếp theo (@DesignAdvisor): viết {where}/{aid}.pick.md "
          f"(giữ tối đa {gd.PICK_MAX} ý, mỗi ý một node đích, mỗi góp ý bỏ có lí do; skill advisor-ask-gpt), rồi chạy "
          f"python Management/tools/gd_pick.py {node} {aid}")
    for p in points:
        kind = p["kind"] + (f" {p['target']}" if p["target"] else "")
        print(f"- {p['id']} ({kind}): {p['title']}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
