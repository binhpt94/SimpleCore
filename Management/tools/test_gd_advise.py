#!/usr/bin/env python3
"""Tests for gd_advise.py (asking GPT about the design) and for the labels that say which AI proposed an idea
(`by:` / `node-by:` in data.md, `by` of a question's option). Codex CLI is replaced by a fake; the design tree is a
throw-away one in a temp folder.

    python Management/tools/test_gd_advise.py
"""
import io
import json
import os
import shutil
import sys
import tempfile
from contextlib import redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_advise  # noqa: E402
import gd_status  # noqa: E402

results = []
FAKE = '''import sys
args = sys.argv[1:]
prompt = sys.stdin.buffer.read().decode("utf-8")
open(args[args.index("-o") + 1] + ".prompt", "w", encoding="utf-8").write(prompt)
import os
answer = os.environ.get("FAKE_ANSWER", "")
if answer:
    open(args[args.index("-o") + 1], "w", encoding="utf-8").write(answer)
open(os.environ["FAKE_LOG"], "a", encoding="utf-8").write(" ".join(args) + "\\n" + prompt + "\\n=====\\n")
'''


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def node(root, table, path, body, head=""):
    folder = gd.table_dir(root, table).joinpath(*(path.split("/") if path else []))
    folder.mkdir(parents=True, exist_ok=True)
    title = path.split("/")[-1] if path else "Game"
    (folder / "data.md").write_text(f"{head}# {title}\n\nTóm tắt.\n\n## {gd.ITEMS_TITLE}\n{body}", encoding="utf-8")
    (folder / "CLAUDE.md").write_text(f"# {title}\n\nDữ liệu cốt lõi: data.md\n\nChi tiết của {title}.\n", encoding="utf-8")
    return folder


def run(root, *args):
    out = io.StringIO()
    with redirect_stdout(out):
        code = gd_advise.main(["gd_advise.py", *args], root)
    return code, out.getvalue()


def advise_cases(root, tmp):
    advice = Path(root).joinpath(*gd.BASE, gd.ADVICE_DIR)
    log = Path(tmp) / "fake.log"
    fake = Path(tmp) / "fake_codex.py"
    fake.write_text(FAKE, encoding="utf-8")
    os.environ.update(STUDIO_CODEX=str(fake), FAKE_LOG=str(log))
    node(root, "idea", "", "\n### loop | AI 8 | Vòng lặp chính\nĐánh, nhặt, nâng cấp.\n")
    hero = node(root, "idea", "feature-hero", "\n### hero-count | AI 8 | Tối đa 5 hero\nĐủ đông.\n")
    good = "### keep-five | Giữ nguyên 5 hero\nĐủ đọc trên điện thoại.\n\n### add-bench | Thêm ghế dự bị\nĐổi hero giữa trận.\n"

    for args in (("feature-hero",), ("Feature-Hero", "q1"), ("feature-hero", "Q1"), ("a/b", "q1"), ("../x", "q1")):
        check(f"bad arguments are refused: {args}", run(root, *args)[0], 1)
    os.environ["FAKE_ANSWER"] = good
    code, text = run(root, "feature-hero", "q1")
    check("no question file: refused, Codex is not run", (code, "chưa có câu hỏi" in text, log.exists()), (1, True, False))
    (advice / "feature-none").mkdir(parents=True)
    (advice / "feature-none" / "q1.q.md").write_text("Hỏi gì đó?", encoding="utf-8")
    code, text = run(root, "feature-none", "q1")
    check("a node that is in neither table: refused, Codex is not run", (code, "không có node" in text, log.exists()), (1, True, False))

    (advice / "feature-hero").mkdir(parents=True)
    (advice / "feature-hero" / "q1.q.md").write_text("5 hero có nhiều quá không?\n", encoding="utf-8")
    code, text = run(root, "feature-hero", "q1")
    answer = advice / "feature-hero" / "q1.md"
    said = answer.read_text(encoding="utf-8") if answer.is_file() else ""
    check("a good answer is written beside the question", (code, answer.is_file()), (0, True))
    check("the answer file names the node, its hash, the question and GPT's points",
          all(s in said for s in ("node: feature-hero", "table: ai/idea", f"hash: {gd.read_data(hero)['hash']}", "binding: no",
                                  "5 hero có nhiều quá không?", "### keep-five | Giữ nguyên 5 hero", "### add-bench |")))
    check("the command prints the points", ("2 góp ý" in text, "- keep-five: Giữ nguyên 5 hero" in text), (True, True))
    logged = log.read_text(encoding="utf-8")
    first_line = logged.split("\n")[0].split(" ")
    check("Codex runs read-only at the project root, the prompt on stdin",
          (first_line[0], first_line[first_line.index("-C") + 1] == str(Path(root)), first_line[first_line.index("-s") + 1], first_line[-1]),
          ("exec", True, "read-only", "-"))
    check("the prompt holds the question, the node and the node above, and asks for separate points",
          all(s in logged for s in ("5 hero có nhiều quá không?", "Tối đa 5 hero", "Chi tiết của feature-hero", "Vòng lặp chính",
                                    "### <id góp ý")))

    os.environ["FAKE_ANSWER"] = "### keep-five | Vẫn giữ\nLần hai.\n"
    code, _ = run(root, "feature-hero", "q1")
    check("asked again: the earlier answer is kept as v1", (code, (advice / "feature-hero" / "q1.v1.md").read_text(encoding="utf-8") == said,
                                                            "Lần hai." in answer.read_text(encoding="utf-8")), (0, True, True))
    before = answer.read_text(encoding="utf-8")
    for bad in ("Tôi nghĩ ổn rồi.\n", "### a | Một\nx\n### a | Hai\ny\n"):
        os.environ["FAKE_ANSWER"] = bad
        code, text = run(root, "feature-hero", "q1")
        check(f"an answer without separate points is refused and kept aside: {bad[:12]!r}",
              (code, (advice / "feature-hero" / "q1.rejected.md").read_text(encoding="utf-8").strip() == bad.strip(),
               answer.read_text(encoding="utf-8") == before), (1, True, True))
    os.environ["FAKE_ANSWER"] = ""
    code, text = run(root, "feature-hero", "q1")
    check("Codex says nothing: refused, the answer stays", (code, "không trả lời" in text, answer.read_text(encoding="utf-8") == before), (1, True, True))

    os.environ["FAKE_ANSWER"] = good
    for name in (gd.ADVICE_ROOT, gd.ADVICE_GENERAL):
        (advice / name).mkdir(parents=True, exist_ok=True)
        (advice / name / "g1.q.md").write_text("Game này thiếu gì?", encoding="utf-8")
    code, _ = run(root, gd.ADVICE_ROOT, "g1")
    check("the root node is asked as _root", (code, "node: (gốc)" in (advice / gd.ADVICE_ROOT / "g1.md").read_text(encoding="utf-8")), (0, True))
    code, _ = run(root, gd.ADVICE_GENERAL, "g1")
    check("a question about no node", (code, "node: (không gắn node nào)" in (advice / gd.ADVICE_GENERAL / "g1.md").read_text(encoding="utf-8")), (0, True))
    live = node(root, "live", "feature-live", "\n### a | AI 8 | Chỉ có trong live\n")
    (advice / "feature-live").mkdir(parents=True)
    (advice / "feature-live" / "q1.q.md").write_text("Hỏi về node đã chốt?", encoding="utf-8")
    code, _ = run(root, "feature-live", "q1")
    check("a node that is only in live", (code, f"hash: {gd.read_data(live)['hash']}" in (advice / "feature-live" / "q1.md").read_text(encoding="utf-8")
                                          and "table: live" in (advice / "feature-live" / "q1.md").read_text(encoding="utf-8")), (0, True))
    shutil.rmtree(live)
    tree, problems = gd.read_tree(root, "idea")
    check("the advice folder is not read as part of the design tree", ([n["path"] for n in gd.walk(tree)], problems), (["", "feature-hero"], []))


def label_cases(root):
    plain = "\n### hero-count | AI 8 | Tối đa 5 hero\nĐủ đông.\n\n### hero-bench | AI 7 | Có ghế dự bị\nĐổi giữa trận.\n"
    base = gd.read_data(gd.table_dir(root, "idea"))["hash"]
    head = f"---\nbased-on: {base}\n" + "{}---\n"
    hero = node(root, "idea", "feature-hero", plain, head.format(""))
    data = gd.read_data(hero)
    check("an idea without a label is Claude's", [(it["by"], it["advice"]) for it in data["items"]], [("claude", ""), ("claude", "")])
    labelled = node(root, "idea", "feature-hero", plain, head.format("by: hero-bench=gpt@q1, hero-count=claude+gpt@q1\nnode-by: gpt@q1\n"))
    data2 = gd.read_data(labelled)
    check("by: labels each idea, node-by: the node", ([(it["id"], it["by"], it["advice"]) for it in data2["items"]], data2["nodeBy"], data2["errors"]),
          ([("hero-count", "claude+gpt", "q1"), ("hero-bench", "gpt", "q1")], {"who": "gpt", "advice": "q1"}, []))
    check("labels do not change the data hash or the ideas' hashes",
          (data2["hash"], [it["hash"] for it in data2["items"]]), (data["hash"], [it["hash"] for it in data["items"]]))
    tree, problems = gd.read_tree(root, "idea")
    n = next(x for x in gd.walk(tree) if x["path"] == "feature-hero")
    check("the node counts its ideas by AI and has no problem when the advice exists",
          (n["by"], n["nodeBy"], problems), ({"claude": 0, "gpt": 1, "claude+gpt": 1}, {"who": "gpt", "advice": "q1"}, []))
    for bad, why in (("by: hero-bench=gpt\n", "phải có dạng"), ("by: hero-bench=codex@q1\n", "phải có dạng"),
                     ("by: nope=gpt@q1\n", "không có ý 'nope'"), ("by: hero-bench=gpt@q1, hero-bench=gpt@q1\n", "mỗi ý một lần"),
                     ("node-by: claude@q1\n", "node-by"), ("by: hero-bench=gpt@missing\n", "trỏ tới góp ý 'missing' không có"),
                     ("node-by: gpt@missing\n", "trỏ tới góp ý 'missing' không có")):
        node(root, "idea", "feature-hero", plain, head.format(bad))
        _, problems = gd.read_tree(root, "idea")
        check(f"a wrong label is a structure problem: {bad.strip()}", any(why in p["text"] for p in problems))
    node(root, "idea", "feature-hero", plain, head.format("node-by: gpt@g1\n"))      # g1 was asked about the root
    check("node-by may name advice asked about the node above", gd.read_tree(root, "idea")[1], [])
    node(root, "idea", "feature-hero", plain, head.format("by: hero-bench=gpt@q1\n"))
    status = "\n".join(gd_status_lines(root, "feature-hero"))
    check("gd_status prints the label beside the idea",
          ("- Ý hero-bench · GPT (góp ý q1): AI 7" in status, "- Ý hero-count: AI 8" in status), (True, True))
    check("gd_status lists the node's advice", "advice/feature-hero/q1.md" in status)
    qdir = hero / gd.QUESTIONS_DIR
    qdir.mkdir(exist_ok=True)

    def ask(options):
        (qdir / gd.QUESTIONS_FILE).write_text(json.dumps({"questions": [{"id": "bench", "text": "Có ghế dự bị?", "options": options}]}), encoding="utf-8")
        problems = []
        return gd.read_questions(hero, "feature-hero", problems), problems

    qs, problems = ask([{"id": "yes", "label": "Có", "by": "gpt"}, {"id": "no", "label": "Không", "by": "claude"}, {"id": "later", "label": "Để sau"}])
    check("an option of a question says which AI proposed it", ([o["by"] for o in qs[0]["options"]], problems), (["gpt", "claude", ""], []))
    qs, problems = ask([{"id": "yes", "label": "Có", "by": "human"}])
    check("an unknown proposer of an option is a problem, the option stays", ([o["by"] for o in qs[0]["options"]], len(problems)), ([""], 1))
    (qdir / gd.QUESTIONS_FILE).unlink()


def gd_status_lines(root, path):
    return gd_status.report(root, "ai/idea/" + path).split("\n")


def main():
    tmp = tempfile.mkdtemp(prefix="gd-advise-test-")
    root = Path(tmp) / "project"
    root.mkdir()
    keep = {k: os.environ.get(k) for k in ("STUDIO_CODEX", "FAKE_LOG", "FAKE_ANSWER")}
    try:
        advise_cases(root, tmp)
        label_cases(root)
    finally:
        for k, v in keep.items():
            os.environ.pop(k, None) if v is None else os.environ.__setitem__(k, v)
        shutil.rmtree(tmp, ignore_errors=True)
    failed = [r for r in results if not r[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
