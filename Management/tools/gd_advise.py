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
What comes back, in advice/<node>/<id>.md: a head (node, the node's data hash when asked, the time), the question,
and GPT's points, each ONE idea:
    ### <point id> | <thêm | làm rõ <idea id> | thay <idea id>> | <the idea in one sentence>
    <reasons, short>
thêm: an idea the node lacks · làm rõ: an idea that is unclear, said clearly · thay: another way than that idea.
An answer whose points are not of that form, are too long (one point is one decision), or name an idea the node
does not have is kept as <id>.rejected.md and the command fails: run it again. An earlier answer of the same id is
kept as <id>.v<n>.md. There is no cap on runs.
GPT's points weigh as much as Claude's ideas: @GameDesigner writes every one of them into the node's ideas (it may
shorten the wording, not leave one out), labelled `by:` in data.md, and they are scored like any idea; what reaches
live is still the human's decision. See .claude/rules/designadvisor.md.
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
INLINE_MAX = 40000      # characters of design text put into the prompt; GPT reads the rest itself
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
Ý nào đã ổn thì không viết gì về nó.

Trả lời bằng tiếng Việt, CHỈ gồm các góp ý rời nhau. Mỗi góp ý là MỘT quyết định duy nhất (hai quyết định thì tách
hai góp ý), đúng dạng:

### <id góp ý: chữ thường, số, gạch nối, không trùng> | <loại> | <ý đó nói gọn trong một câu, tối đa {gd.IDEA_TITLE_MAX} kí tự>
<lí do và hệ quả, tối đa {gd.IDEA_DETAIL_MAX} kí tự>

<loại> là một trong ba: `thêm` · `làm rõ <id ý>` · `thay <id ý>` (id ý là id của một ý trong node được hỏi).
Câu sau dấu `|` cuối phải là chính ý đó, viết như một ý của thiết kế (ví dụ "Mỗi trận tối đa 5 hero"), không viết
"nên xem xét...". Không viết lời mở đầu, kết luận hay dòng `###` nào khác."""


def fail(text):
    print("LỖI: " + text)
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
        out.append(f"- [{it['id']}] {it['title']} ({score})")
        out += [f"    {line}" for line in it["detail"].split("\n") if line.strip()]
    if not data["items"]:
        out.append("- (chưa có ý nào)")
    return "\n".join(out)


def design_text(root, node, found):
    """What GPT is given of the design: the nodes above (from the root down) and the node asked about, the proposed
    one when there is one, else the fixed one; then the node's CLAUDE.md. Cut at INLINE_MAX."""
    if not found:
        return ""
    table, folder = found[0]
    parts = [] if node == gd.ADVICE_ROOT else node.split(".")
    out = [node_text(root, table, parts[:depth], "node phía trên") for depth in range(len(parts))]
    out.append(node_text(root, table, parts, "NODE ĐƯỢC HỎI"))
    if (folder / gd.DOC_FILE).is_file():
        out.append(f"----- diễn giải của node được hỏi ({gd.DOC_FILE})\n" + gd.read_text(folder / gd.DOC_FILE).strip())
    text = "\n\n".join(t for t in out if t)
    return text if len(text) <= INLINE_MAX else text[:INLINE_MAX] + "\n[... cắt bớt: đọc phần còn lại trong file]"


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


def main(argv, root=ROOT):
    if len(argv) != 3 or not NODE_RX.match(argv[1]) or not ID_RX.match(argv[2]):
        return fail("cách dùng: python Management/tools/gd_advise.py <node với '.' thay '/', _root hoặc _general> <id>")
    root, node, aid = Path(root), argv[1], argv[2]
    folder = root.joinpath(*gd.BASE, gd.ADVICE_DIR, node)
    question_file = folder / f"{aid}.q.md"
    where = "/".join(folder.relative_to(root).parts)
    if not question_file.is_file() or not gd.read_text(question_file).strip():
        return fail(f"chưa có câu hỏi: viết {where}/{aid}.q.md trước (Write), rồi chạy lại")
    found = node_folders(root, node)
    if node != gd.ADVICE_GENERAL and not found:
        return fail(f"không có node '{node.replace('.', '/')}' trong ai/idea hay live "
                    f"(node gốc viết {gd.ADVICE_ROOT}, câu hỏi chung viết {gd.ADVICE_GENERAL})")
    question = gd.read_text(question_file).strip()
    try:
        exe = studio.tool_exe("codex")
    except ValueError as ex:
        return fail(str(ex))
    design = design_text(root, node, found)
    prompt = (ASK + "\n\n===== Câu hỏi\n" + question
              + ("\n\n===== Thiết kế liên quan (bản lúc hỏi; điểm là điểm NGƯỜI chấm)\n" + design if design
                 else "\n\n(Câu hỏi không gắn node nào: chỉ dùng loại `thêm`.)"))
    with tempfile.TemporaryDirectory(prefix="gd-advise-") as tmp:
        out = Path(tmp) / "answer.md"
        code, err = studio.run(exe + ["exec", "-C", str(root), "-s", "read-only", "--skip-git-repo-check",
                                      "-o", str(out), "-"], prompt)
        answer = gd.read_text(out).strip() if out.is_file() else ""
    if not answer:
        return fail(f"Codex không trả lời (mã {code}): {err.strip()[-600:]}")
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
    head += [f"asked: {gd.now()}", "by: gpt (Codex CLI)", "---"]
    target.write_text("\n".join(head) + f"\n# Góp ý {aid}\n\n## Câu hỏi\n\n{question}\n\n## {gd.ADVICE_TITLE}\n\n{answer}\n",
                      encoding="utf-8", newline="\n")
    print(f"Đã ghi {where}/{aid}.md ({len(points)} góp ý). Mỗi góp ý phải thành một ý của node: @GameDesigner viết vào "
          f"'## {gd.ITEMS_TITLE}' và khai by: <id ý>=gpt@{aid}#<id góp ý>; không được bỏ góp ý nào.")
    for p in points:
        kind = p["kind"] + (f" {p['target']}" if p["target"] else "")
        print(f"- {p['id']} ({kind}): {p['title']}")
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
