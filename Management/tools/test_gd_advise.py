#!/usr/bin/env python3
"""Tests for gd_advise.py (asking GPT about the design) and for what ties GPT's points to the ideas of a node
(`by:`, `gpt-dropped:`, `node-by:` in data.md): every point becomes an idea, none is left out. Codex CLI is replaced
by a fake; the design tree is a throw-away one in a temp folder.

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
import gd_pack  # noqa: E402
import gd_status  # noqa: E402

results = []
FAKE = '''import os, sys
args = sys.argv[1:]
prompt = sys.stdin.buffer.read().decode("utf-8")
answer = os.environ.get("FAKE_ANSWER", "")
if answer:
    open(args[args.index("-o") + 1], "w", encoding="utf-8").write(answer)
open(os.environ["FAKE_LOG"], "a", encoding="utf-8").write(" ".join(args) + "\\n" + prompt + "\\n=====\\n")
'''
GOOD = ("### add-bench | thêm | Có ghế dự bị 2 hero\nĐổi hero giữa trận.\n\n"
        "### clear-count | làm rõ hero-count | Mỗi trận tối đa 5 hero trên sân cùng lúc\nBản cũ không nói tính hero đã gục.\n\n"
        "### four-heroes | thay hero-count | Mỗi trận tối đa 4 hero\nMàn hình điện thoại hẹp.\n")
PLAIN = "\n### hero-count | AI 8 | Tối đa 5 hero\nĐủ đông.\n\n### hero-speed | AI 3 | Hero chạy nhanh\nChưa rõ.\n"


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


def hero_node(root, by=""):
    base = gd.read_data(gd.table_dir(root, "idea"))["hash"]
    return node(root, "idea", "feature-hero", by[1] if isinstance(by, tuple) else PLAIN,
                f"---\nbased-on: {base}\n{by[0] if isinstance(by, tuple) else by}---\n")


def hero(root):
    tree, problems = gd.read_tree(root, "idea")
    return next(x for x in gd.walk(tree) if x["path"] == "feature-hero"), [p["text"] for p in problems if p["path"] == "feature-hero"]


def advise_cases(root, tmp):
    advice = Path(root).joinpath(*gd.BASE, gd.ADVICE_DIR)
    log = Path(tmp) / "fake.log"
    fake = Path(tmp) / "fake_codex.py"
    fake.write_text(FAKE, encoding="utf-8")
    os.environ.update(STUDIO_CODEX=str(fake), FAKE_LOG=str(log))
    node(root, "idea", "", "\n### loop | AI 8 | Vòng lặp chính\nĐánh, nhặt, nâng cấp.\n")
    folder = hero_node(root)
    gd.save_review(root, "feature-hero", {"hero-count": {"score": 7, "feedback": "Thử 4 xem sao"}})

    for args in (("feature-hero",), ("Feature-Hero", "q1"), ("feature-hero", "Q1"), ("a/b", "q1"), ("../x", "q1")):
        check(f"bad arguments are refused: {args}", run(root, *args)[0], 1)
    os.environ["FAKE_ANSWER"] = GOOD
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
    check("the answer file names the node, its hash, the question and GPT's points; it no longer says the advice binds nobody",
          (all(s in said for s in ("node: feature-hero", "table: ai/idea", f"hash: {gd.read_data(folder)['hash']}",
                                   "5 hero có nhiều quá không?", f"## {gd.ADVICE_TITLE}", "### add-bench | thêm | Có ghế dự bị 2 hero")),
           "binding" in said), (True, False))
    check("the command prints the points with their kind and says none may be left out",
          all(s in text for s in ("3 góp ý", "- add-bench (thêm): Có ghế dự bị 2 hero", "- four-heroes (thay hero-count):", "không được bỏ")))
    logged = log.read_text(encoding="utf-8")
    first_line = logged.split("\n")[0].split(" ")
    check("Codex runs read-only at the project root, the prompt on stdin",
          (first_line[0], first_line[first_line.index("-C") + 1] == str(Path(root)), first_line[first_line.index("-s") + 1], first_line[-1]),
          ("exec", True, "read-only", "-"))
    check("the prompt holds the question, the node, the node above, the three kinds, and asks to add and to make clear",
          all(s in logged for s in ("5 hero có nhiều quá không?", "[hero-count] Tối đa 5 hero", "Chi tiết của feature-hero", "[loop] Vòng lặp chính",
                                    "`thêm` · `làm rõ <id ý>` · `thay <id ý>`", "BỔ SUNG", "LÀM RÕ", "MỘT quyết định")))
    check("the prompt gives the human's score with its meaning and feedback, and says when the human has not scored",
          all(s in logged for s in ("[hero-count] Tối đa 5 hero (người chấm 7: Chấp nhận ở hiện tại", "phản hồi của người: Thử 4 xem sao",
                                    "[hero-speed] Hero chạy nhanh (người chưa chấm)")))
    check("the prompt never gives the AI's scores", [s for s in ("AI 8", "AI 3", "| AI") if s in logged.replace("`### <id> | AI <n>`", "")], [])

    os.environ["FAKE_ANSWER"] = "### add-bench | thêm | Vẫn nên có ghế dự bị\nLần hai.\n"
    code, _ = run(root, "feature-hero", "q1")
    check("asked again: the earlier answer is kept as v1", (code, (advice / "feature-hero" / "q1.v1.md").read_text(encoding="utf-8") == said,
                                                            "Lần hai." in answer.read_text(encoding="utf-8")), (0, True, True))
    before = answer.read_text(encoding="utf-8")
    long_title, long_body = "x" * (gd.IDEA_TITLE_MAX + 1), "y" * (gd.IDEA_DETAIL_MAX + 1)
    for label, bad in (("no points", "Tôi nghĩ ổn rồi.\n"), ("the old form without a kind", "### a | Một ý\nx\n"),
                       ("an unknown kind", "### a | xoá hero-count | Bỏ ý này\nx\n"), ("the same id twice", "### a | thêm | Một\nx\n### a | thêm | Hai\ny\n"),
                       ("an idea the node does not have", "### a | thay hero-none | Khác\nx\n"),
                       ("a title that is too long", f"### a | thêm | {long_title}\nx\n"), ("reasons that are too long", f"### a | thêm | Ngắn\n{long_body}\n"),
                       ("words outside a point", "Mở đầu.\n### a | thêm | Một\nx\n")):
        os.environ["FAKE_ANSWER"] = bad
        code, text = run(root, "feature-hero", "q1")
        check(f"an answer that cannot be used is refused and kept aside: {label}",
              (code, (advice / "feature-hero" / "q1.rejected.md").read_text(encoding="utf-8").strip() == bad.strip(),
               answer.read_text(encoding="utf-8") == before), (1, True, True))
    os.environ["FAKE_ANSWER"] = ""
    code, text = run(root, "feature-hero", "q1")
    check("Codex says nothing: refused, the answer stays", (code, "không trả lời" in text, answer.read_text(encoding="utf-8") == before), (1, True, True))

    os.environ["FAKE_ANSWER"] = "### more-loot | thêm | Quái rơi đồ\nCó thứ để nhặt.\n"
    for name in (gd.ADVICE_ROOT, gd.ADVICE_GENERAL):
        (advice / name).mkdir(parents=True, exist_ok=True)
        (advice / name / "g1.q.md").write_text("Game này thiếu gì?", encoding="utf-8")
    code, _ = run(root, gd.ADVICE_GENERAL, "g1")
    check("a question about no node", (code, "node: (không gắn node nào)" in (advice / gd.ADVICE_GENERAL / "g1.md").read_text(encoding="utf-8")), (0, True))
    os.environ["FAKE_ANSWER"] = "### a | làm rõ loop | Rõ hơn\nx\n"
    check("a question about no node takes only points that add", run(root, gd.ADVICE_GENERAL, "g2")[0], 1)
    live = node(root, "live", "feature-live", "\n### a | AI 8 | Chỉ có trong live\n")
    (advice / "feature-live").mkdir(parents=True)
    (advice / "feature-live" / "q1.q.md").write_text("Hỏi về node đã chốt?", encoding="utf-8")
    os.environ["FAKE_ANSWER"] = "### b | thêm | Thêm một ý\nx\n"
    code, _ = run(root, "feature-live", "q1")
    filed = (advice / "feature-live" / "q1.md").read_text(encoding="utf-8")
    check("a node that is only in live", (code, f"hash: {gd.read_data(live)['hash']}" in filed and "table: live" in filed), (0, True))
    shutil.rmtree(live)
    shutil.rmtree(advice / "feature-live")
    shutil.rmtree(advice / "feature-none")
    # back to the full answer for the label cases
    os.environ["FAKE_ANSWER"] = GOOD
    check("the full answer is filed again", run(root, "feature-hero", "q1")[0], 0)
    context_cases(root, advice, log)


def context_cases(root, advice, log):
    """What the command adds by itself: the other features, the nodes beside and below, what the human turned down,
    GPT's earlier points; and the nodes a question adds with `also:`."""
    idea = gd.table_dir(root, "idea")
    top = f"---\nbased-on: {gd.read_data(idea)['hash']}\n---\n"
    under = f"---\nbased-on: {gd.read_data(idea / 'feature-hero')['hash']}\n---\n"
    made = [node(root, "idea", "feature-shop", "\n### shop-count | AI 9 | Shop có 6 ô\nĐủ chọn.\n", top),
            node(root, "idea", "feature-hero/hero-list", "\n### list-a | AI 9 | Danh sách có 8 hero\n", under),
            node(root, "idea", "feature-hero/hero-skill", "\n### skill-a | AI 9 | Mỗi hero một kĩ năng\n", under),
            node(root, "live", "feature-hero/hero-list", "\n### list-a | AI 9 | Danh sách có 8 hero\n", under)]
    history = idea / "feature-hero" / gd.HISTORY_DIR / gd.LOG_FILE
    kept = history.read_text(encoding="utf-8") if history.is_file() else None
    history.parent.mkdir(parents=True, exist_ok=True)
    with open(history, "a", encoding="utf-8") as f:
        f.write(json.dumps({"event": "review", "rejected": True, "item": "hero-fly", "text": "Hero biết bay", "score": 2,
                            "feedback": "Không hợp"}, ensure_ascii=False) + "\n")

    def ask(name, aid, question, answer="### more | thêm | Thêm một ý\nx\n"):
        os.environ["FAKE_ANSWER"] = answer
        (advice / name).mkdir(parents=True, exist_ok=True)
        (advice / name / f"{aid}.q.md").write_text(question, encoding="utf-8")
        before = len(log.read_text(encoding="utf-8"))
        code, text = run(root, name, aid)
        return code, text, log.read_text(encoding="utf-8")[before:]

    code, _, sent = ask("feature-hero", "q2", "also: feature-shop\nHero và shop có trùng việc không?\n")
    check("the prompt carries the other features, what is decided below (fixed or not), what the human turned down and GPT's earlier points",
          [x for x in ("feature-hero/hero-list (ĐÃ CHỐT ở live): hero-list", "· [list-a] Danh sách có 8 hero",
                       "feature-hero/hero-skill (đề xuất, chưa chốt)", "[hero-fly] Hero biết bay (người chấm 2; phản hồi: Không hợp)",
                       "feature-hero/q1#add-bench (thêm): Có ghế dự bị 2 hero", "feature-hero/q1#four-heroes (thay hero-count)",
                       "KHÔNG đề xuất ngược") if x not in sent], [])
    check("a node named by also: is given in full with the human's score, and the line itself is not part of the question",
          (code, "node feature-shop" in sent and "node liên quan" in sent, "[shop-count] Shop có 6 ô (người chưa chấm)" in sent,
           "also:" in sent.split("===== Thiết kế")[0], "also: feature-shop" in (advice / "feature-hero" / "q2.md").read_text(encoding="utf-8")),
          (0, True, True, False, True))
    check("the added context never gives the AI's scores", [x for x in ("AI 9", "AI 8", "| AI") if x in sent.replace("`### <id> | AI <n>`", "")], [])
    code, _, sent = ask("feature-hero.hero-list", "q1", "Danh sách hero thiếu gì?")
    check("a round-3 node also sees the nodes beside it and the feature map",
          (code, "Node bên cạnh" in sent and "- feature-hero/hero-skill: hero-skill" in sent, "- feature-shop: feature-shop" in sent), (0, True, True))
    for bad in ("also: feature-nowhere\nHỏi?", "also: _root\nHỏi?", "also: feature-hero\nHỏi?", "also: feature-shop\n"):
        code, _, sent = ask("feature-hero", "q3", bad)
        check(f"a bad also: line is refused and Codex is not run: {bad.split(chr(10))[0]!r} {'(no question)' if bad.endswith(chr(10)) else ''}",
              (code, sent), (1, ""))
    code, _, sent = ask(gd.ADVICE_GENERAL, "g3", "Bộ feature này có trùng việc không?")
    check("a question about no node sees the root and every feature",
          (code, "[loop] Vòng lặp chính" in sent, "- feature-hero: feature-hero" in sent and "· [shop-count] Shop có 6 ô" in sent,
           f"{gd.ADVICE_GENERAL}/g1#more-loot" in sent), (0, True, True, True))
    values = node(root, "idea", "feature-hero/hero-list/values", "\n### v-a | AI 9 | 8 hero, mỗi hero 3 chỉ số\n",
                  f"---\nbased-on: {gd.read_data(idea / 'feature-hero' / 'hero-list')['hash']}\n---\n")
    asks = {name: gd_advise.build_prompt(root, name, gd_advise.node_folders(root, name), "Hỏi?")
            for name in (gd.ADVICE_ROOT, gd.ADVICE_GENERAL, "feature-hero", "feature-hero.hero-list", "feature-hero.hero-list.values")}
    shutil.rmtree(values)
    check("GPT is told it may propose new features and nodes in rounds 1-3 and about no node, and may not at the last round",
          {name: ("Feature mới:" in text, "KHÔNG đề xuất feature hay node mới" in text) for name, text in asks.items()},
          {gd.ADVICE_ROOT: (True, False), gd.ADVICE_GENERAL: (True, False), "feature-hero": (True, False),
           "feature-hero.hero-list": (True, False), "feature-hero.hero-list.values": (False, True)})
    found = gd_advise.node_folders(root, "feature-hero")
    full = gd_advise.design_text(root, "feature-hero", found)
    limit, gd_advise.INLINE_MAX = gd_advise.INLINE_MAX, len(full[0]) + 120
    try:
        cut = gd_advise.design_text(root, "feature-hero", found)
    finally:
        gd_advise.INLINE_MAX = limit
    check("over the budget only the added context is cut, never the node asked about",
          (cut[0] == full[0], len(cut[1]) < len(full[1]), "cắt bớt" in cut[1]), (True, True, True))

    for folder in made:      # leave the tree as the label cases expect it
        shutil.rmtree(folder)
    shutil.rmtree(gd.table_dir(root, "live") / "feature-hero", ignore_errors=True)
    shutil.rmtree(advice / "feature-hero.hero-list")
    for name in ("q2.md", "q2.q.md", "q3.q.md"):
        (advice / "feature-hero" / name).unlink()
    for name in ("g3.md", "g3.q.md"):
        (advice / gd.ADVICE_GENERAL / name).unlink()
    if kept is None:
        history.unlink()
    else:
        history.write_text(kept, encoding="utf-8")
    os.environ["FAKE_ANSWER"] = GOOD


def label_cases(root):
    hero_node(root)
    n, problems = hero(root)
    check("an idea without a label is Claude's", [(it["by"], it["sources"]) for it in n["items"]], [("claude", []), ("claude", [])])
    check("every point of GPT's that is not an idea yet is a structure problem",
          sorted(p.split("'")[1] for p in problems if "chưa vào" in p), ["q1#add-bench", "q1#clear-count", "q1#four-heroes"])
    check("the node says what it owes", ([m["point"] for m in n["gpt"]["missing"]], n["gpt"]["dropped"]),
          (["add-bench", "clear-count", "four-heroes"], []))
    plain_hash = (n["dataHash"], [it["hash"] for it in n["items"]][:2])

    full = PLAIN + "\n### hero-bench | AI 6 | Có ghế dự bị 2 hero\nĐổi giữa trận.\n\n### hero-four | AI 5 | Tối đa 4 hero\nMàn hẹp.\n"
    by = "by: hero-bench=gpt@q1#add-bench, hero-count=gpt@q1#clear-count, hero-four=gpt@q1#four-heroes\n"
    hero_node(root, (by, full))
    n, problems = hero(root)
    items = {it["id"]: it for it in n["items"]}
    check("all points are ideas: nothing owed, no problem", (n["gpt"]["missing"], problems), ([], []))
    check("a point that adds or replaces makes the idea GPT's; one that only makes Claude's idea clearer leaves it Claude's",
          {k: (v["by"], v["clarified"]) for k, v in items.items()},
          {"hero-count": ("claude", True), "hero-speed": ("claude", False), "hero-bench": ("gpt", False), "hero-four": ("gpt", False)})
    check("the node counts its ideas by AI", (n["by"], n["gpt"]["clarified"]), ({"claude": 2, "gpt": 2}, 1))
    check("an idea carries GPT's own words", items["hero-bench"]["sources"][0]["text"], "Có ghế dự bị 2 hero\nĐổi hero giữa trận.")
    check("the idea that replaces another and the one it replaces stand against each other",
          {k: v["clash"] for k, v in items.items()}, {"hero-count": ["hero-four"], "hero-speed": [], "hero-bench": [], "hero-four": ["hero-count"]})
    review = lambda scores: gd.save_review(root, "feature-hero", {k: {"score": v, "feedback": ""} for k, v in scores.items()})  # noqa: E731
    gate = lambda: gd.node_approved(root, ["feature-hero"], "auto", None)[:2]      # noqa: E731
    seen = [hero(root)[0]["clashOpen"]]
    review({"hero-count": 8, "hero-four": 8, "hero-speed": 8, "hero-bench": 8})
    seen += [hero(root)[0]["clashOpen"], gate()[0], "nhóm ý trái nhau" in gate()[1]]
    check("gd_status says the clash is open and the nodes below are locked",
          "XUNG ĐỘT CHƯA GIẢI QUYẾT: 1 nhóm" in gd_status.report(root, "ai/idea/feature-hero"), True)
    review({"hero-four": 2})
    seen += [hero(root)[0]["clashOpen"], "nhóm ý trái nhau" in gate()[1]]
    check("ideas that stand against each other lock the nodes below until the human scored them all and at most one passes",
          seen, [1, 1, False, True, 0, False])
    shutil.rmtree(gd.table_dir(root, "idea") / "feature-hero" / gd.REVIEW_DIR, ignore_errors=True)
    hero_node(root, ("", full))
    check("labels do not change the hash of the data or of an idea",
          ([it["hash"] for it in hero(root)[0]["items"]][:2], hero(root)[0]["dataHash"] == n["dataHash"]), (plain_hash[1], True))
    hero_node(root, (by, full))
    status = gd_status.report(root, "ai/idea/feature-hero")
    check("gd_status prints where an idea came from, the clash, and the node's advice",
          all(s in status for s in ("- Ý hero-bench · GPT (góp ý q1#add-bench): AI 6", "- Ý hero-count · GPT làm rõ (góp ý q1#clear-count): AI 8",
                                    "TRÁI NHAU với hero-count (", "TRÁI NHAU với hero-four (Tối đa 4 hero)", "advice/feature-hero/q1.md", "- Ý hero-speed: AI 3")))

    for bad, why in (("by: hero-bench=gpt@q1\n", "phải có dạng"), ("by: hero-bench=claude+gpt@q1#add-bench\n", "phải có dạng"),
                     ("by: nope=gpt@q1#add-bench\n", "không có ý 'nope'"),
                     ("by: hero-bench=gpt@q1#add-bench, hero-four=gpt@q1#add-bench\n", "mỗi góp ý một lần"),
                     ("by: hero-bench=gpt@q1#no-point\n", "trỏ tới góp ý 'q1#no-point' không có"),
                     ("by: hero-bench=gpt@q9#add-bench\n", "trỏ tới góp ý 'q9#add-bench' không có"),
                     ("node-by: claude@q1\n", "node-by"), ("node-by: gpt@missing\n", "node-by trỏ tới lần hỏi 'missing' không có"),
                     ("gpt-dropped: q1#add-bench\n", "gpt-dropped"), ("gpt-dropped: q1#no-point=hero-x\n", "gpt-dropped trỏ tới góp ý"),
                     (by + "gpt-dropped: q1#add-bench=hero-bench\n", "vẫn còn trong node")):
        hero_node(root, (bad, full))
        check(f"a wrong label is a structure problem: {bad.strip()[-60:]}", any(why in p for p in hero(root)[1]))
    general = Path(root).joinpath(*gd.BASE, gd.ADVICE_DIR, gd.ADVICE_GENERAL)
    (general / "g9.md").write_text(f"## {gd.ADVICE_TITLE}\n\n### new-hero | thêm | Feature mới: hero\nx\n", encoding="utf-8")
    hero_node(root, ("node-by: gpt@g9\n" + by, full))
    check("node-by may name advice asked about no node (the question put to all features)",
          (hero(root)[0]["nodeBy"], [p for p in hero(root)[1] if "node-by" in p]), ({"who": "gpt", "advice": "g9"}, []))
    (general / "g9.md").unlink()
    hero_node(root, ("node-by: gpt@g1\n" + by, full))      # g1 was asked about no node; the folder above is _root
    (Path(root).joinpath(*gd.BASE, gd.ADVICE_DIR, gd.ADVICE_ROOT) / "g1.md").write_text(f"## {gd.ADVICE_TITLE}\n\n### split | thêm | Tách node hero\nx\n", encoding="utf-8")
    n, problems = hero(root)
    check("node-by may name advice asked about the node above", (n["nodeBy"], [p for p in problems if "node-by" in p]), ({"who": "gpt", "advice": "g1"}, []))

    # dropping one of GPT's ideas: declared; in mode user only after the human scored it below 5
    kept = "by: hero-count=gpt@q1#clear-count, hero-four=gpt@q1#four-heroes\n"
    without = PLAIN + "\n### hero-four | AI 5 | Tối đa 4 hero\nMàn hẹp.\n"
    drop = kept + "gpt-dropped: q1#add-bench=hero-bench\n"
    gd.write_mode(root, "auto")
    hero_node(root, (kept, without))
    check("an idea of GPT's taken out without a word: the point is owed again", [m["point"] for m in hero(root)[0]["gpt"]["missing"]], ["add-bench"])
    hero_node(root, (drop, without))
    n, problems = hero(root)
    check("mode auto: the declared drop is taken, and shown", (n["gpt"]["missing"], [d["point"] for d in n["gpt"]["dropped"]], problems), ([], ["add-bench"], []))
    check("gd_status shows the dropped idea", "Ý của GPT đã bị loại: q1#add-bench (ý hero-bench)" in gd_status.report(root, "ai/idea/feature-hero"))
    gd.write_mode(root, "user")
    n, problems = hero(root)
    check("mode user: a drop the human did not score below 5 is refused",
          (any("không được bỏ góp ý 'q1#add-bench'" in p for p in problems), [m["point"] for m in n["gpt"]["missing"]]), (True, ["add-bench"]))
    hero_node(root, (by, full))
    gd.save_review(root, "feature-hero", {"hero-bench": {"score": 3, "feedback": "Không cần"}})
    hero_node(root, (drop, without))
    n, problems = hero(root)
    check("mode user: after the human scored it below 5 the drop is taken",
          (n["gpt"]["missing"], [d["point"] for d in n["gpt"]["dropped"]], [p for p in problems if "bỏ" in p]), ([], ["add-bench"], []))
    gd.write_mode(root, "auto")

    # fixed ideas: the ones live holds with the same content
    hero_node(root, (by, full))
    live_root = node(root, "live", "", "\n### loop | AI 8 | Vòng lặp chính\nĐánh, nhặt, nâng cấp.\n")
    node(root, "live", "feature-hero", PLAIN.replace("Hero chạy nhanh", "Hero chạy chậm"), f"---\nbased-on: {gd.read_data(live_root)['hash']}\n---\n")
    snap = gd.snapshot(root)
    n = next(x for x in gd.walk(snap["idea"]) if x["path"] == "feature-hero")
    check("an idea is fixed when live holds it with the same content",
          {it["id"]: it["fixed"] for it in n["items"]}, {"hero-count": True, "hero-speed": False, "hero-bench": False, "hero-four": False})
    check("the live table has no advice owed", [x["gpt"]["missing"] for x in gd.walk(snap["live"])], [[], []])
    owed_cases(root)
    check("gd_pack labels GPT's ideas", "GPT (góp ý q1#add-bench)" in "\n".join(gd_pack_lines(n)))
    long = PLAIN + f"\n### hero-long | AI 6 | {'d' * (gd.IDEA_TITLE_MAX + 1)}\nx\n"
    hero_node(root, ("", long))
    check("an idea that is too long is pointed out, not refused",
          ("- Ý hero-long: AI 6, người chưa chấm | Ý DÀI" in gd_status.report(root, "ai/idea/feature-hero"),
           [p for p in hero(root)[1] if "hero-long" in p]), (True, []))
    qdir = gd.table_dir(root, "idea") / "feature-hero" / gd.QUESTIONS_DIR
    qdir.mkdir(exist_ok=True)
    (qdir / gd.QUESTIONS_FILE).write_text(json.dumps({"questions": [{"id": "bench", "text": "Có ghế dự bị?", "options": [
        {"id": "yes", "label": "Có", "by": "gpt"}, {"id": "no", "label": "Không", "by": "claude"}, {"id": "later", "label": "Để sau"}]}]}), encoding="utf-8")
    problems = []
    qs = gd.read_questions(qdir.parent, "feature-hero", problems)
    check("an option of a question says which AI proposed it", ([o["by"] for o in qs[0]["options"]], problems), (["gpt", "claude", ""], []))


def owed_cases(root):
    """Asking GPT is a must in rounds 1 and 2: said on the node, never a structure problem."""
    idea = gd.table_dir(root, "idea")
    advice = idea.parent.parent / gd.ADVICE_DIR
    top = f"---\nbased-on: {gd.read_data(idea)['hash']}\n---\n"
    under = f"---\nbased-on: {gd.read_data(idea / 'feature-hero')['hash']}\n---\n"
    node(root, "idea", "feature-shop", PLAIN.replace("hero-", "shop-"), top)
    node(root, "idea", "feature-hero/hero-list", PLAIN.replace("hero-", "list-"), under)
    node(root, "idea", gd.GLOSSARY_KEY, "\n### term-a | AI 8 | Một từ\nNghĩa.\n", top)
    for a in gd.advice_ids(advice / gd.ADVICE_ROOT):
        (advice / gd.ADVICE_ROOT / f"{a}.md").unlink()
    (advice / "feature-shop").mkdir(parents=True, exist_ok=True)
    (advice / "feature-shop" / "s1.q.md").write_text("Hỏi gì đó", encoding="utf-8")
    snap = gd.snapshot(root)
    owed = {x["path"]: x["gptOwed"] for x in gd.walk(snap["idea"])}
    check("rounds 1 and 2 owe GPT a question until an answer is filed (a question alone is none); round 3 and the glossary owe none",
          owed, {"": True, "feature-hero": False, "feature-shop": True, "feature-hero/hero-list": False, gd.GLOSSARY_KEY: False})
    check("the live table owes no question", [x["gptOwed"] for x in gd.walk(snap["live"])], [False, False])
    check("not asking is no structure problem", [p for p in snap["problems"]["idea"] if "GPT" in p["text"] and "hỏi" in p["text"]], [])
    text = gd_status.report(root, "")
    check("gd_status says which nodes still owe the question",
          ("- CHƯA HỎI GPT" in text, text.split("## Chưa hỏi GPT")[1].split("\n##")[0].count("\n- ")), (True, 2))
    (advice / "feature-shop" / "s1.md").write_text("## Góp ý của GPT\n", encoding="utf-8")
    check("an answer settles it", next(x for x in gd.walk(gd.snapshot(root)["idea"]) if x["path"] == "feature-shop")["gptOwed"], False)


def gd_pack_lines(n):
    out = []
    gd_pack.node_lines(n, out) if hasattr(gd_pack, "node_lines") else out.extend(f"- {it['title']}{gd.by_label(it)}" for it in n["items"])
    return out


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
