#!/usr/bin/env python3
"""Asks GPT (Codex CLI) one question about the game design. For @DesignAdvisor (and the human).

    python Management/tools/gd_advise.py <node> <id>

<node>  the design node the question is about: its path with '.' for '/' (feature-hero.hero-list), `_root` for the
        round-1 node, or `_general` for a question about no node in particular.
<id>    the id of this advice (lower case, digits, '-').

The question is the file Management/game-design/advice/<node>/<id>.q.md, written before this is run. GPT gets the
question, the node's data.md and CLAUDE.md and the data.md of the nodes above it, and may read the rest of
Management/game-design itself (Codex runs read-only at the project root: it writes nothing).
The answer lands in advice/<node>/<id>.md: a head (node, the node's data hash when asked, the time), the question,
and GPT's advice as separate points, each one `### <point id> | <title>` with its reasons below, so that each can
be taken, changed or dropped on its own. An answer without such points is kept as <id>.rejected.md and the command
fails: run it again. An earlier answer of the same id is kept as <id>.v<n>.md. There is no cap on runs.
Advice binds nobody: it becomes design only when @GameDesigner writes it into ai/idea (and labels the idea with
`by:` in data.md), and it reaches live only through the human. See .claude/rules/designadvisor.md.
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
POINT_RX = re.compile(r"^###\s+([a-z0-9]+(?:-[a-z0-9]+)*)\s*\|\s*(\S.*)$", re.M)
INLINE_MAX = 40000      # characters of design text put into the prompt; GPT reads the rest itself
ASK = """Bạn là cố vấn thiết kế game cho project này. Bạn chỉ góp ý: người thiết kế (@GameDesigner) và người chủ project
quyết định dùng hay bỏ từng góp ý. Thiết kế nằm ở Management/game-design: `live/` là bảng đã chốt, `ai/idea/` là bảng
đang đề xuất; mỗi node có data.md (các ý cốt lõi) và CLAUDE.md (diễn giải). Bạn đang chạy ở chế độ chỉ đọc: được đọc
thêm các file trong Management/game-design khi cần, không tạo hay sửa file nào.

Trả lời bằng tiếng Việt, cụ thể, dựa trên thiết kế đã có (nêu node hoặc ý mà góp ý nhắm tới). Không khen chung chung.
Trả lời CHỈ gồm các góp ý rời nhau, mỗi góp ý đúng dạng:

### <id góp ý: chữ thường, số, gạch nối, không trùng> | <góp ý nói gọn trong một câu>
<lí do, hệ quả với các node khác, phương án thay thế nếu có>

Không viết lời mở đầu, kết luận hay tiêu đề `###` nào khác. Thấy thiết kế hiện tại đã ổn ở một điểm được hỏi thì
vẫn viết một góp ý nói rõ "giữ nguyên" và vì sao."""


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


def design_text(root, node, found):
    """What GPT is given of the design: the node (the proposed one, else the fixed one) and the data of the nodes
    above it, cut at INLINE_MAX."""
    if not found:
        return ""
    table, folder = found[0]
    top = gd.table_dir(root, table)
    rel = "/".join(top.relative_to(root).parts)
    parts = [] if node == gd.ADVICE_ROOT else node.split(".")
    out = []
    for depth in range(len(parts)):      # the nodes above, from the root down
        above = top.joinpath(*parts[:depth])
        if (above / gd.DATA_FILE).is_file():
            out.append(f"----- {rel}/{'/'.join(parts[:depth] + [gd.DATA_FILE])} (node phía trên)\n"
                       + gd.read_text(above / gd.DATA_FILE).strip())
    for name in gd.NODE_FILES:
        if (folder / name).is_file():
            out.append(f"----- {rel}/{'/'.join(parts + [name])} (node được hỏi)\n" + gd.read_text(folder / name).strip())
    text = "\n\n".join(out)
    return text if len(text) <= INLINE_MAX else text[:INLINE_MAX] + "\n[... cắt bớt: đọc phần còn lại trong file]"


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
              + ("\n\n===== Thiết kế liên quan (bản lúc hỏi)\n" + design if design else ""))
    with tempfile.TemporaryDirectory(prefix="gd-advise-") as tmp:
        out = Path(tmp) / "answer.md"
        code, err = studio.run(exe + ["exec", "-C", str(root), "-s", "read-only", "--skip-git-repo-check",
                                      "-o", str(out), "-"], prompt)
        answer = gd.read_text(out).strip() if out.is_file() else ""
    if not answer:
        return fail(f"Codex không trả lời (mã {code}): {err.strip()[-600:]}")
    points = POINT_RX.findall(answer)
    ids = [p[0] for p in points]
    if not points or len(set(ids)) != len(ids):
        (folder / f"{aid}.rejected.md").write_text(answer + "\n", encoding="utf-8", newline="\n")
        return fail(f"câu trả lời không chia thành các góp ý '### <id> | <tiêu đề>' (id không trùng); đã giữ ở "
                    f"{where}/{aid}.rejected.md. Chạy lại lệnh.")
    target = folder / f"{aid}.md"
    if target.is_file():      # keep what was said before
        n = 1
        while (folder / f"{aid}.v{n}.md").exists():
            n += 1
        target.replace(folder / f"{aid}.v{n}.md")
    head = ["---", f"advice: {aid}"]
    if found:
        table, node_folder = found[0]
        head += [f"node: {node.replace('.', '/') if node != gd.ADVICE_ROOT else '(gốc)'}",
                 f"table: {'/'.join(gd.TABLES[table])}", f"hash: {gd.read_data(node_folder)['hash']}"]
    else:
        head.append("node: (không gắn node nào)")
    head += [f"asked: {gd.now()}", "by: gpt (Codex CLI)", "binding: no", "---"]
    target.write_text("\n".join(head) + f"\n# Góp ý {aid}\n\n## Câu hỏi\n\n{question}\n\n## Góp ý của GPT\n\n{answer}\n",
                      encoding="utf-8", newline="\n")
    print(f"Đã ghi {where}/{aid}.md ({len(points)} góp ý). Góp ý không ràng buộc: @GameDesigner lọc, người chốt.")
    for pid, title in points:
        print(f"- {pid}: {title.strip()}")
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
