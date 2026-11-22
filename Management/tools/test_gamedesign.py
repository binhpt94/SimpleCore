#!/usr/bin/env python3
"""Tests for gamedesign.py, gd_delete.py and gd_status.py, on a throw-away tree in a temp folder.

    python Management/tools/test_gamedesign.py
"""
import json
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_delete  # noqa: E402
import gd_progress  # noqa: E402
import gd_status  # noqa: E402

results = []
GOOD = (("core", 8, "Ý chính của node."),)


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def folder(root, rel):
    """"idea/..." and "live/..." are tables; anything else is under Management/game-design."""
    table, _, rest = rel.partition("/")
    base = gd.table_dir(root, table) if table in gd.TABLES else Path(root).joinpath(*gd.BASE, table)
    return base.joinpath(*rest.split("/")) if rest else base


def put(root, rel, text="x"):
    p = folder(root, rel)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")
    return p


def data_text(title, items, based="", summary="Tóm tắt."):
    head = f"---\nbased-on: {based}\n---\n" if based else ""
    body = "".join(f"\n### {i} | AI {ai}\n{text}\n" for i, ai, text in items)
    return f"{head}# {title}\n\n{summary}\n\n## {gd.ITEMS_TITLE}\n{body}"


def mk(root, path, items=GOOD, title=None, table="idea", based=None, summary="Tóm tắt.", doc="Chi tiết."):
    """Writes a node (data.md + CLAUDE.md). based-on defaults to the parent's current data hash."""
    rel = table + ("/" + path if path else "")
    if based is None and path:
        parent = gd.read_data(folder(root, rel).parent)
        based = parent["hash"] if parent else ""
    title = title or (path.split("/")[-1] if path else "Game")
    put(root, rel + "/data.md", data_text(title, items, based or "", summary))
    put(root, rel + "/CLAUDE.md", f"# {title}\n\nDữ liệu cốt lõi: data.md\n\n{doc}\n")


def refused(fn, *args):
    try:
        fn(*args)
        return False
    except ValueError:
        return True


def snap_node(root, path=""):
    return next(n for n in gd.walk(gd.snapshot(root)["idea"]) if n["path"] == path)


def paths(tree):
    return [n["path"] for n in gd.walk(tree)]


def questions(*items):
    return json.dumps({"questions": list(items)}, ensure_ascii=False)


def q(qid, multi=False, options=("a", "b")):
    return {"id": qid, "text": f"Câu {qid}?", "why": "vì cần", "multi": multi,
            "options": [{"id": o, "label": f"Đề xuất {o}", "detail": ""} for o in options]}


def run_data(root):
    """data.md: parsing, the data hash, structure problems."""
    text = data_text("Hero", (("count", 8, "Tối đa 5 hero.\nKhông hơn."), ("spawn", 6, "Tự sinh.")), based="abc123")
    d = gd.parse_data(text)
    check("title, summary, based-on", (d["title"], d["summary"], d["based_on"]), ("Hero", "Tóm tắt.", "abc123"))
    check("ideas with their AI score", [(i["id"], i["ai"], i["text"]) for i in d["items"]],
          [("count", 8, "Tối đa 5 hero.\nKhông hơn."), ("spawn", 6, "Tự sinh.")])
    check("clean data has no errors", d["errors"], [])
    same = lambda other: gd.parse_data(other)["hash"] == d["hash"]      # noqa: E731
    check("hash ignores the AI score", same(text.replace("AI 8", "AI 2")))
    check("hash ignores based-on", same(text.replace("abc123", "zzz999")))
    check("hash ignores trailing spaces and CR", same(text.replace("Tự sinh.", "Tự sinh.   ")))
    check("hash follows an idea's text", same(text.replace("Tự sinh.", "Bấm để gọi.")), False)
    check("hash follows the summary", same(text.replace("Tóm tắt.", "Tóm tắt khác.")), False)
    check("hash follows the title", same(text.replace("# Hero", "# Anh hùng")), False)
    check("hash follows an added idea", same(text + "\n### extra | AI 5\nThêm.\n"), False)
    check("hash follows an idea's id", same(text.replace("### spawn", "### spawning")), False)
    for label, bad, part in (
            ("no title", "tóm tắt\n", "tiêu đề"),
            ("bad idea line", "# T\n\ns\n\n## Ý cốt lõi\n\n### count\nx\n", "'### <id> | AI <0-10>'"),
            ("score over 10", "# T\n\ns\n\n## Ý cốt lõi\n\n### count | AI 11\nx\n", "0 tới 10"),
            ("duplicate id", "# T\n\ns\n\n## Ý cốt lõi\n\n### a | AI 5\nx\n\n### a | AI 6\ny\n", "không trùng"),
            ("bad id", "# T\n\ns\n\n## Ý cốt lõi\n\n### Bad_Id | AI 5\nx\n", "chữ thường"),
            ("empty idea", "# T\n\ns\n\n## Ý cốt lõi\n\n### a | AI 5\n\n", "chưa có tiêu đề hay nội dung"),
            ("other section", "# T\n\ns\n\n## Luật\nx\n\n## Ý cốt lõi\n\n### a | AI 5\nx\n", "không thuộc data.md"),
            ("open front matter", "---\nbased-on: x\n# T\n", "chưa đóng")):
        check("data problem: " + label, any(part in e for e in gd.parse_data(bad)["errors"]))

    titled = gd.parse_data("# T\n\ns\n\n## Ý cốt lõi\n\n### roster | AI 7 | Danh sách 6 hero\nMỗi hero có tên và chỉ số riêng.\n\n### solo | AI 6 | Chỉ có tiêu đề\n")
    check("an idea with a title: title, detail, both in text", [(i["id"], i["title"], i["detail"], i["titled"], i["text"]) for i in titled["items"]],
          [("roster", "Danh sách 6 hero", "Mỗi hero có tên và chỉ số riêng.", True, "Danh sách 6 hero\nMỗi hero có tên và chỉ số riêng."),
           ("solo", "Chỉ có tiêu đề", "", True, "Chỉ có tiêu đề")])
    check("a titled idea needs no detail", titled["errors"], [])
    it = d["items"][0]
    check("an idea without a title is titled by its first sentence; the whole text is its detail",
          (it["title"], it["detail"], it["titled"]), ("Tối đa 5 hero.", "Tối đa 5 hero.\nKhông hơn.", False))
    check("a one-sentence idea without a title has no separate detail", (d["items"][1]["title"], d["items"][1]["detail"]), ("Tự sinh.", ""))
    check("an untitled idea hashes as before, so earlier scores still apply", it["hash"], gd.short_hash("count", "Tối đa 5 hero.\nKhông hơn."))
    t2 = lambda a, b: gd.parse_data(f"# T\n\ns\n\n## Ý cốt lõi\n\n### x | AI 5 | {a}\n{b}\n")["hash"]      # noqa: E731
    check("hash follows the title and the detail of an idea", (t2("A", "b") != t2("A2", "b"), t2("A", "b") != t2("A", "b2"), t2("A", "b") == t2("A", "b")), (True, True, True))
    check("no folder yet: no tree", gd.read_tree(root, "idea"), (None, []))
    mk(root, "", title="Game thẻ bài", summary="Người chơi gom thẻ\nvà đấu.", doc="## Thêm\nchi tiết")
    mk(root, "feature-hero")
    mk(root, "feature-hero/skills")
    mk(root, "feature-hero/skills/cooldown")
    mk(root, "feature-shop")
    put(root, "idea/feature-hero/.images/hero.png", "png")
    put(root, "idea/feature-hero/.images/index.md", "# Ảnh\n- hero.png: dáng đứng của hero\n")
    tree, problems = gd.read_tree(root, "idea")
    check("clean tree has no problems", problems, [])
    check("nodes in order", paths(tree), ["", "feature-hero", "feature-hero/skills", "feature-hero/skills/cooldown", "feature-shop"])
    check("depth is the round", [n["round"] for n in gd.walk(tree)], [1, 2, 3, 4, 2])
    check("title and summary come from data.md", (tree["title"], tree["summary"]), ("Game thẻ bài", "Người chơi gom thẻ và đấu."))
    check("body is CLAUDE.md", "## Thêm" in tree["body"] and "data.md" in tree["body"])
    check("images with their notes", tree["children"][0]["images"], [{"file": "hero.png", "note": "dáng đứng của hero"}])
    check("dot folders are not nodes", [c["key"] for c in tree["children"][0]["children"]], ["skills"])

    bad = tempfile.mkdtemp(prefix="gdbad-")
    try:
        mk(bad, "")
        put(bad, "idea/no-data/CLAUDE.md", "# x\n\ndata.md\n")
        put(bad, "idea/no-doc/data.md", data_text("x", GOOD, gd.read_data(folder(bad, "idea"))["hash"]))
        mk(bad, "doc-without-ref", doc="x")
        put(bad, "idea/doc-without-ref/CLAUDE.md", "# x\n\nkhông trỏ gì\n")
        mk(bad, "Bad_Name")
        mk(bad, "a"), mk(bad, "a/b"), mk(bad, "a/b/c"), mk(bad, "a/b/c/d")
        put(bad, "idea/a/.images/x.png")
        texts = " | ".join(f"{p['path']}: {p['text']}" for p in gd.read_tree(bad, "idea")[1])
        for label, part in (("missing data.md", "no-data: thiếu data.md"), ("missing CLAUDE.md", "no-doc: thiếu CLAUDE.md"),
                            ("CLAUDE.md without the pointer", "doc-without-ref: CLAUDE.md phải trỏ tới data.md"),
                            ("bad key", "Bad_Name: tên thư mục"), ("too deep", "a/b/c/d: sâu hơn vòng 4"),
                            ("images without index", "a: .images có ảnh nhưng thiếu index.md")):
            check("problem: " + label, part in texts)
        check("promote refuses a branch with problems", refused(gd.promote, bad, "", True))
    finally:
        shutil.rmtree(bad, ignore_errors=True)


def run_scores(root):
    """Scores, the verdict in both modes, staleness, the write gate."""
    mk(root, "", items=(("loop", 8, "Vòng lặp chơi."), ("win", 3, "Thắng sau 10 đợt.")))
    mk(root, "feature-hero")
    hero, skills = ["feature-hero"], ["feature-hero", "skills"]
    n = snap_node(root)
    check("auto: an AI score of 3 fails the node", (n["passed"], "AI chấm 3" in n["reasons"][0]), (False, True))
    check("a child of a failing node is locked", gd.can_write_node(root, hero)[0], False)
    check("the failing node itself is writable", gd.can_write_node(root, []), (True, ""))
    check("promote refuses a failing node", refused(gd.promote, root, "", False))
    for label, items in (("unknown idea", {"nope": {"score": 7}}), ("score 11", {"win": {"score": 11}}),
                         ("score -1", {"win": {"score": -1}}), ("score as text", {"win": {"score": "7"}}),
                         ("score true", {"win": {"score": True}}), ("nothing", {"win": {"score": None, "feedback": " "}}),
                         ("empty", {}), ("feedback too long", {"win": {"score": 5, "feedback": "x" * 2001}})):
        check("review refused: " + label, refused(gd.save_review, root, "", items))
    check("bad path refused", refused(gd.save_review, root, "../x", {"win": {"score": 7}}))
    check("review saved", gd.save_review(root, "", {"win": {"score": 7, "feedback": "được"}}), 1)
    n = snap_node(root)
    check("the human's score overrides the AI's", (n["passed"], n["items"][1]["user"], n["items"][1]["effective"]), (True, 7, 7))
    check("the child opens", gd.can_write_node(root, hero), (True, ""))
    gd.save_review(root, "", {"loop": {"score": 2, "feedback": "lặp lại quá nhàm"}})
    n = snap_node(root)
    check("a human 2 fails the node whatever the AI says", (n["passed"], "người chấm 2" in " ".join(n["reasons"])), (False, True))
    gd.save_review(root, "", {"loop": {"score": None, "feedback": "chỉ góp ý"}})
    check("feedback without a score leaves the AI's score in force", snap_node(root)["items"][0]["effective"], 8)

    # a rewritten idea: the old human score no longer applies
    gd.save_review(root, "", {"loop": {"score": 2, "feedback": "nhàm"}})
    mk(root, "", items=(("loop", 9, "Vòng lặp chơi có biến cố ngẫu nhiên."), ("win", 3, "Thắng sau 10 đợt.")))
    it = snap_node(root)["items"][0]
    check("rewriting an idea drops the human score to 'previous'", (it["user"], it["previous"]["score"], it["effective"]), (None, 2, 9))
    check("an untouched idea keeps its human score", snap_node(root)["items"][1]["user"], 7)

    # staleness
    n = snap_node(root, "feature-hero")
    check("the child is stale after its parent's data changed", (n["stale"], n["passed"], "data của node cha" in n["reasons"][0]), (True, False, True))
    check("a stale node locks what is below it", "feature-hero" in gd.can_write_node(root, skills)[1])
    mk(root, "feature-hero")      # redone: based-on is the parent's new hash
    check("writing the new based-on clears it", (snap_node(root, "feature-hero")["stale"], gd.can_write_node(root, skills)), (False, (True, "")))
    put(root, "idea/CLAUDE.md", "# Game\n\ndata.md\n\nDiễn giải đã đổi.\n")
    check("editing CLAUDE.md does not make the child stale", snap_node(root, "feature-hero")["stale"], False)
    mk(root, "feature-hero", based="")
    check("a child without based-on is stale", snap_node(root, "feature-hero")["stale"])
    mk(root, "feature-hero")
    mk(root, "", items=())
    check("a node without ideas does not pass", (snap_node(root)["passed"], "chưa có ý nào" in snap_node(root)["reasons"][0]), (False, True))

    # ---- mode user
    mk(root, "", items=(("loop", 9, "Vòng lặp."), ("win", 9, "Thắng.")))
    mk(root, "feature-hero")
    gd.write_mode(root, "user")
    n = snap_node(root)
    check("user: an idea the human has not scored fails", (n["passed"], [r for r in n["reasons"] if "chưa chấm" in r] != []), (False, True))
    gd.save_review(root, "", {"loop": {"score": 8}, "win": {"score": 4}})
    check("user: a human 4 fails", snap_node(root)["passed"], False)
    gd.save_review(root, "", {"win": {"score": 5}})
    check("user: every idea scored 5 or more passes", snap_node(root)["passed"])
    put(root, "idea/.ai-need-user-choose/questions.json", questions(q("spawn")))
    check("user: an unanswered question fails", (snap_node(root)["passed"], snap_node(root)["pending"]), (False, 1))
    check("user: promote refused", refused(gd.promote, root, "", False))
    gd.save_answers(root, "", {"spawn": {"chosen": ["a"]}})
    check("user: passing is not yet approved (not in live)", (snap_node(root)["passed"], snap_node(root)["approved"]), (True, False))
    check("user: the child waits for the promote", "chưa được người chốt" in gd.can_write_node(root, hero)[1])
    check("user: reading the tree for the page promotes nothing", (gd.snapshot(root)["live"], gd.record_versions(root) and None), (None, None))
    gd.promote(root, "", False)
    check("user: approved once promoted; the child opens", (snap_node(root)["approved"], gd.can_write_node(root, hero)), (True, (True, "")))
    check("grandchild locked: its parent is unscored", gd.can_write_node(root, skills)[0], False)
    put(root, "idea/CLAUDE.md", "# Game\n\ndata.md\n\nSửa nữa.\n")
    check("user: an edited parent differs from live and locks again", "khác bản đã chốt" in gd.can_write_node(root, hero)[1])
    check("only live's own files were copied", sorted(p.name for p in gd.table_dir(root, "live").iterdir()), ["CLAUDE.md", "data.md"])

    # ---- answers (the rest of the checks)
    Q = "idea/feature-hero/.ai-need-user-choose/questions.json"
    put(root, Q, questions(q("types", multi=True, options=("melee", "ranged", "mage")), q("one")))
    for label, path, answers in (
            ("unknown question", "feature-hero", {"nope": {"chosen": ["a"]}}), ("unknown option", "feature-hero", {"one": {"chosen": ["zzz"]}}),
            ("two for a single choice", "feature-hero", {"one": {"chosen": ["a", "b"]}}), ("empty", "feature-hero", {"one": {"chosen": [], "other": " "}}),
            ("not a dict", "feature-hero", ["one"]), ("other too long", "feature-hero", {"one": {"other": "x" * 2001}}),
            ("unknown node", "feature-none", {"one": {"chosen": ["a"]}}), ("bad path", "..", {"one": {"chosen": ["a"]}})):
        check("answer refused: " + label, refused(gd.save_answers, root, path, answers))
    gd.save_answers(root, "feature-hero", {"types": {"chosen": ["melee", "mage"], "other": " thêm hero bay "}})
    n = snap_node(root, "feature-hero")
    check("multi choice with other text", (n["pending"], n["questions"][0]["chosen"], n["questions"][0]["other"]), (1, ["melee", "mage"], "thêm hero bay"))
    for label, text, part in (("not JSON", "{ broken", "không phải JSON"), ("wrong shape", "[]", "phải có dạng"),
                              ("duplicate id", questions(q("a"), q("a")), "không trùng"), ("no options", questions(q("a", options=())), "ít nhất một")):
        put(root, Q, text)
        check("questions problem: " + label, any(part in p["text"] for p in gd.snapshot(root)["problems"]["idea"]))
    check("mode: broken file is auto; unknown mode refused",
          (put(root, "mode.json", "{ broken") and gd.read_mode(root), refused(gd.write_mode, root, "fast")), ("auto", True))


def run_live_scores(root):
    """One set of scores for both tables: a score belongs to an idea's text."""
    mk(root, "", items=(("loop", 8, "Vòng lặp."), ("win", 8, "Thắng sau 10 đợt.")))
    mk(root, "feature-hero")
    check("nothing reaches live by reading", (gd.record_versions(root) != [], gd.snapshot(root)["live"]), (True, None))
    check("sync in mode auto", gd.sync_live(root), ["(gốc)", "feature-hero"])
    live = lambda: gd.read_tree(root, "live")[0]      # noqa: E731
    check("live ideas start unscored", [i["user"] for i in live()["items"]], [None, None])
    check("a live node that is not in live is refused", refused(gd.save_review, root, "feature-none", {"x": {"score": 5}}, "live"))
    check("unknown table refused", refused(gd.save_review, root, "", {"win": {"score": 5}}, "trash"))
    gd.save_review(root, "", {"win": {"score": 3, "feedback": "quá ngắn"}}, "live")
    check("a score given in live shows in live", [(i["user"], i["feedback"]) for i in live()["items"]], [(None, ""), (3, "quá ngắn")])
    n = snap_node(root)
    check("and in the idea table, where the text is the same: the node must be redone",
          (n["items"][1]["user"], n["passed"], n["redo"]), (3, False, True))
    check("the children of that node are locked", gd.can_write_node(root, ["feature-hero"])[0], False)
    check("history says which table and that it was rejected",
          [(e["table"], e["item"], e["rejected"]) for e in gd.read_history(folder(root, "idea")) if e["event"] == "review"], [("live", "win", True)])
    mk(root, "", items=(("loop", 8, "Vòng lặp."), ("win", 8, "Chơi vô tận.")))
    n = snap_node(root)
    check("the rewritten idea is unscored in the idea table; the old score is 'previous'",
          (n["items"][1]["user"], n["items"][1]["previous"]["score"], n["passed"]), (None, 3, True))
    check("live still shows the 3 on the old text", live()["items"][1]["user"], 3)
    gd.save_review(root, "", {"win": {"score": 9}}, "idea")
    check("both scores are kept, each for its own text", (snap_node(root)["items"][1]["user"], live()["items"][1]["user"]), (9, 3))
    gd.sync_live(root)
    check("after the sync live has the new text and its score", (live()["items"][1]["text"], live()["items"][1]["user"]), ("Chơi vô tận.", 9))
    old = folder(root, "idea/.user-review/review.json")
    item = gd.read_data(folder(root, "idea"))["items"][0]
    old.write_text(json.dumps({"items": {"loop": {"score": 6, "feedback": "cũ", "hash": item["hash"], "at": "2026-01-01 00:00:00"}}}), encoding="utf-8")
    check("the first layout of review.json is still read", (snap_node(root)["items"][0]["user"], snap_node(root)["items"][0]["feedback"]), (6, "cũ"))
    gd.save_review(root, "", {"win": {"score": 7}})
    check("and is kept when another idea is saved", [i["user"] for i in snap_node(root)["items"]], [6, 7])
    gd.write_mode(root, "user")
    put(root, "idea/CLAUDE.md", "# Game\n\ndata.md\n\nSửa.\n")
    check("sync in mode user promotes what the human scored", gd.sync_live(root), ["(gốc)"])
    check("and stops at the node the human has not scored", snap_node(root, "feature-hero")["passed"], False)


def run_priority(root):
    """Importance 0-10 and the phase it means: leaves are scored, parents take the highest below, a human score is
    absolute and shifts the unscored nodes below it by the same offset. Outside every hash."""
    mk(root, "")
    mk(root, "feature-hero"), mk(root, "feature-hero/skills"), mk(root, "feature-hero/skills/cooldown"), mk(root, "feature-hero/level")
    mk(root, "feature-shop"), mk(root, "feature-wallet")
    ai = lambda path, score, why="vì": put(root, f"idea/{path}/priority.json", json.dumps({"ai": score, "why": why}))      # noqa: E731
    eff = lambda: {n["path"]: n["priority"]["effective"] for n in gd.walk(gd.snapshot(root)["idea"])}      # noqa: E731
    pr = lambda path: snap_node(root, path)["priority"]      # noqa: E731
    check("nothing scored: nothing ranked", (set(eff().values()), pr("")["phase"]), ({None}, ""))
    ai("feature-hero/skills/cooldown", 4), ai("feature-hero/level", 6), ai("feature-shop", 3), ai("feature-wallet", 9, "tiền là lõi")
    check("a parent takes the highest leaf below it", eff(),
          {"": 9, "feature-hero": 6, "feature-hero/skills": 4, "feature-hero/skills/cooldown": 4, "feature-hero/level": 6, "feature-shop": 3, "feature-wallet": 9})
    check("phases", [pr(x)["phase"] for x in ("feature-wallet", "feature-hero", "feature-shop")], ["idea", "prototype", "full"])
    check("a leaf keeps the AI's reason", (pr("feature-wallet")["why"], pr("feature-wallet")["leaf"], pr("feature-hero")["leaf"]), ("tiền là lõi", True, False))
    ai("feature-hero", 10)
    check("an AI score on a node with children counts for nothing", (pr("feature-hero")["effective"], pr("feature-hero")["ignoredAi"]), (6, True))
    put(root, "idea/feature-shop/priority.json", "{ broken")
    check("a broken or out-of-range priority.json is no score", pr("feature-shop")["effective"], None)
    ai("feature-shop", 11)
    check("out of range", pr("feature-shop")["effective"], None)
    ai("feature-shop", 1)
    check("0-1 is optional", pr("feature-shop")["phase"], "optional")

    # ---- the human
    for label, path, score in (("11", "feature-shop", 11), ("-1", "feature-shop", -1), ("text", "feature-shop", "7"), ("true", "feature-shop", True),
                               ("bad path", "../x", 5), ("unknown node", "feature-none", 5)):
        check("priority refused: " + label, refused(gd.save_priority, root, path, score))
    gd.save_priority(root, "feature-hero/skills/cooldown", 10)
    check("the human's score on a leaf lifts every parent above it", eff(),
          {"": 10, "feature-hero": 10, "feature-hero/skills": 10, "feature-hero/skills/cooldown": 10, "feature-hero/level": 6, "feature-shop": 1, "feature-wallet": 9})
    gd.save_priority(root, "feature-hero/skills/cooldown", None)
    check("removing it gives the AI's ranking back", (eff()["feature-hero"], pr("feature-hero/skills/cooldown")["user"]), (6, None))
    gd.save_priority(root, "feature-hero/skills/cooldown", None)      # removing twice is fine
    gd.save_priority(root, "feature-hero", 9)
    h = pr("feature-hero/level")
    check("scoring a parent: it gets exactly that; unscored nodes below keep their distance (+3)",
          (eff()["feature-hero"], eff()["feature-hero/level"], eff()["feature-hero/skills"], eff()["feature-hero/skills/cooldown"], h["offset"], h["from"]),
          (9, 9, 7, 7, 3, "feature-hero"))
    check("nodes outside that branch are untouched; the root follows the highest", (eff()["feature-shop"], eff()["feature-wallet"], eff()[""]), (1, 9, 9))
    gd.save_priority(root, "feature-hero/skills", 2)
    check("a scored node below keeps its own score and shifts its own children (nearest scored ancestor counts)",
          (eff()["feature-hero/skills"], eff()["feature-hero/skills/cooldown"], eff()["feature-hero/level"], pr("feature-hero/skills/cooldown")["from"]),
          (2, 2, 9, "skills"))
    gd.save_priority(root, "feature-hero", 0)
    check("the offset is kept inside 0-10", (eff()["feature-hero/level"], pr("feature-hero/level")["offset"]), (0, -6))
    gd.save_priority(root, "feature-hero", 10)
    check("and at the top", eff()["feature-hero/level"], 10)
    gd.save_priority(root, "", 5)
    check("the root can be scored too: unscored branches shift, scored ones do not", (eff()[""], eff()["feature-wallet"], eff()["feature-hero"]), (5, 4, 10))
    gd.save_priority(root, "", None), gd.save_priority(root, "feature-hero", None), gd.save_priority(root, "feature-hero/skills", None)

    # ---- outside every hash, every gate and the history
    before = snap_node(root, "feature-wallet")
    gd.record_versions(root), gd.sync_live(root)
    n_events = len(gd.read_history(folder(root, "idea/feature-wallet")))
    ai("feature-wallet", 2), gd.save_priority(root, "feature-wallet", 7)
    after = snap_node(root, "feature-wallet")
    check("changing importance changes no hash, no verdict and no state against live",
          [(n["dataHash"], n["hash"], n["passed"], n["state"], n["dirty"]) for n in (before, after)][1],
          (before["dataHash"], before["hash"], True, "same", False))
    check("and writes nothing to the history", (gd.record_versions(root), len(gd.read_history(folder(root, "idea/feature-wallet")))), ([], n_events))
    check("nothing about importance is copied to live", [x.name for x in gd.table_dir(root, "live").rglob("priority.json")], [])
    live = {n["path"]: n["priority"]["effective"] for n in gd.walk(gd.snapshot(root)["live"])}
    check("the live table shows the proposed node's importance", (live["feature-wallet"], live["feature-hero"]), (7, 6))
    check("the snapshot carries the phases", [x["name"] for x in gd.snapshot(root)["phases"]], ["IDEA", "PROTOTYPE", "FULL", "TUỲ CHỌN"])
    put(root, "idea/feature-shop/priority.json", "{}")
    text = gd_status.report(root)
    for label, part in (("phase scale", "8-10 IDEA"), ("a node's importance", "Độ quan trọng: 7 (PROTOTYPE); người chấm 7; AI 2"),
                        ("misplaced AI score", "bị bỏ qua vì nó có node con"), ("unscored leaf", "BẠN CHƯA CHẤM")):
        check("status shows: " + label, part in text)


def run_progress(root):
    """Progress on leaves; parents ranked by what is not done; code.json and the round-4 code gate."""
    mk(root, "")
    mk(root, "feature-hero"), mk(root, "feature-hero/skills"), mk(root, "feature-hero/skills/cooldown"), mk(root, "feature-hero/level")
    mk(root, "feature-wallet")
    ai = lambda path, score: put(root, f"idea/{path}/priority.json", json.dumps({"ai": score, "why": "vì"}))      # noqa: E731
    pr = lambda path: snap_node(root, path)["priority"]      # noqa: E731
    ai("feature-hero/skills/cooldown", 9), ai("feature-hero/level", 6), ai("feature-wallet", 3)
    check("a leaf starts with every phase todo", {k: v["state"] for k, v in snap_node(root, "feature-wallet")["progress"].items()},
          {"idea": "todo", "prototype": "todo", "full": "todo"})
    for label, args in (("a node with children", ("feature-hero", "idea", "done", "x")), ("the root", ("", "idea", "done", "x")),
                        ("unknown phase", ("feature-wallet", "optional", "done", "x")), ("unknown state", ("feature-wallet", "full", "finished", "x")),
                        ("unknown node", ("feature-none", "idea", "done", "x")), ("bad path", ("../x", "idea", "done", "x")),
                        ("note too long", ("feature-wallet", "full", "done", "x", "n" * 2001))):
        check("progress refused: " + label, refused(gd.save_progress, root, *args))
    before = snap_node(root, "feature-hero/skills/cooldown")
    gd.record_versions(root)
    events = len(gd.read_history(folder(root, "idea/feature-hero/skills/cooldown")))
    gd.save_progress(root, "feature-hero/skills/cooldown", "prototype", "done", "người", "làm trước")
    check("done for another phase than the leaf's own does not finish it", (pr("feature-hero/skills/cooldown")["done"], pr("feature-hero")["effective"]), (False, 9))
    gd.save_progress(root, "feature-hero/skills/cooldown", "idea", "doing", "@CodeLead")
    check("doing is not done", pr("feature-hero/skills/cooldown")["done"], False)
    check(gd_progress.mark(root, "ai/idea/feature-hero/skills/cooldown", "idea", "done"), True, True) if False else None
    msg = gd_progress.mark(root, "ai/idea/feature-hero/skills/cooldown", "idea", "done")
    cell = snap_node(root, "feature-hero/skills/cooldown")["progress"]
    check("the command marks the leaf, by @CodeLead; other cells keep their note",
          ("idea = done" in msg, cell["idea"]["state"], cell["idea"]["by"], cell["prototype"]["note"]), (True, "done", "@CodeLead", "làm trước"))
    check("a done leaf keeps its own score", (pr("feature-hero/skills/cooldown")["done"], pr("feature-hero/skills/cooldown")["effective"]), (True, 9))
    check("but no longer lifts its parents: they take the highest of what is not done",
          (pr("feature-hero/skills")["done"], pr("feature-hero/skills")["effective"], pr("feature-hero")["effective"], pr("")["effective"]), (True, None, 6, 6))
    after = snap_node(root, "feature-hero/skills/cooldown")
    check("progress changes no hash, no verdict, no state and writes no history",
          ((after["dataHash"], after["hash"], after["passed"], after["state"]), gd.record_versions(root), len(gd.read_history(folder(root, "idea/feature-hero/skills/cooldown")))),
          ((before["dataHash"], before["hash"], before["passed"], before["state"]), [], events))
    gd.save_progress(root, "feature-hero/level", "prototype", "done", "@CodeLead")
    gd.save_progress(root, "feature-wallet", "full", "done", "@CodeLead")
    check("every leaf done: the parents are done", (pr("feature-hero")["done"], pr("")["done"], pr("")["effective"]), (True, True, None))
    gd.save_priority(root, "feature-wallet", 9)
    check("a leaf the human moves to another phase is open again there", (pr("feature-wallet")["done"], pr("")["effective"]), (False, 9))
    gd.save_priority(root, "feature-wallet", None)
    gd.save_progress(root, "feature-wallet", "full", "todo", "người")
    check("the human can reopen a leaf", (pr("feature-wallet")["done"], pr("")["effective"]), (False, 3))
    for arg, phase, state in (("live/feature-wallet", "full", "done"), ("ai/idea/../live", "full", "done"), ("ai/idea/feature-hero", "idea", "done"),
                              ("ai/idea/feature-wallet", "idea", "finished"), ("feature-wallet", "idea", "done")):
        check(f"command refused: {arg} {phase} {state}", refused(gd_progress.mark, root, arg, phase, state))
    text = gd_status.report(root)
    for label, part in (("progress cells", "Tiến độ: IDEA done (@CodeLead"), ("a done branch", "ĐÃ XONG (mọi node lá bên dưới đã xong)")):
        check("status shows: " + label, part in text)
    check("progress cannot be deleted by @GameDesigner's command", refused(gd_delete.delete, root, "ai/idea/feature-wallet/.progress"))

    # ---- code.json and the gate
    code = lambda path, names: put(root, f"idea/{path}/code.json", json.dumps({"unity": names}))      # noqa: E731
    gate = lambda name: gd.code_allowed(root, name)      # noqa: E731
    check("no live table: locked", (gate("Wallet")[0], "chưa có node thiết kế nào" in gate("Wallet")[1]), (False, True))
    code("feature-hero", ["Hero", "_Services/Popup"]), code("feature-wallet", ["Wallet"])
    n = snap_node(root, "feature-hero")
    check("a feature node names its code folders", (n["code"], gd.snapshot(root)["problems"]["idea"]), (["Hero", "_Services/Popup"], []))
    check("named only in the proposal: still locked", gate("Hero")[0], False)
    before_hash = n["dataHash"]
    gd.sync_live(root)
    check("code.json goes to live with the node", folder(root, "live/feature-hero/code.json").is_file())
    check("neither progress nor importance goes to live",
          [x.name for x in gd.table_dir(root, "live").rglob("*") if x.name in (".progress", "priority.json")], [])
    check("a branch that reached round 4 in live may be coded, under each name, in any letter case",
          (gate("Hero"), gate("_Services/Popup"), gate("hero")), ((True, ""), (True, ""), (True, "")))
    check("a feature whose design stops at round 2 is locked, and the reason says so", (gate("Wallet")[0], "tới vòng 2" in gate("Wallet")[1]), (False, True))
    check("a folder no node names is locked", gate("Shop")[0], False)
    mk(root, "feature-wallet/coins"), gd.sync_live(root)
    check("round 3 is not enough", (gate("Wallet")[0], "tới vòng 3" in gate("Wallet")[1]), (False, True))
    mk(root, "feature-wallet/coins/cap")
    check("round 4 only in the proposal is not enough", gate("Wallet")[0], False)
    gd.sync_live(root)
    check("round 4 in live opens it", gate("Wallet"), (True, ""))
    check("the snapshot says so per folder", snap_node(root, "feature-wallet")["codeGate"], [{"unity": "Wallet", "ok": True, "reason": ""}])
    code("feature-wallet", ["Wallet", "Shop"])
    n = snap_node(root, "feature-wallet")
    check("changing code.json makes the node differ from live but changes no data hash", (n["state"], gate("Shop")[0]), ("changed", False))
    check("status shows the gate", "Thư mục code Wallet: ĐƯỢC CODE" in gd_status.report(root) and "Thư mục code Shop: ĐANG KHOÁ" in gd_status.report(root))
    check("hero's data hash did not move through all this", snap_node(root, "feature-hero")["dataHash"], before_hash)
    for label, path, text, part in (
            ("not JSON", "feature-wallet", "{ broken", "không phải JSON"), ("wrong shape", "feature-wallet", '{"unity": "Wallet"}', "phải có dạng"),
            ("bad name", "feature-wallet", '{"unity": ["../x"]}', "phải có dạng"), ("not on a feature node", "feature-hero/skills", '{"unity": ["Skills"]}', "chỉ đặt ở node feature"),
            ("claimed twice", "feature-wallet", '{"unity": ["hero"]}', "đã được node 'feature-hero' khai")):
        saved = {x: folder(root, f"idea/{x}/code.json") for x in ("feature-wallet", "feature-hero/skills")}
        put(root, f"idea/{path}/code.json", text)
        check("code.json problem: " + label, any(part in x["text"] for x in gd.snapshot(root)["problems"]["idea"]))
        if path == "feature-hero/skills":
            saved[path].unlink()
    code("feature-wallet", ["Wallet"])


def run_flow(root):
    """Sync; history; delete; status."""
    mk(root, "", items=(("loop", 8, "Vòng lặp."), ("win", 6, "Thắng sau 10 đợt.")))
    mk(root, "feature-hero")
    mk(root, "feature-hero/skills", items=(("three", 3, "Ba kĩ năng."),))
    mk(root, "feature-hero/skills/cooldown")
    mk(root, "feature-shop")
    check("first versions are recorded", gd.record_versions(root),
          ["(gốc)", "feature-hero", "feature-hero/skills", "feature-hero/skills/cooldown", "feature-shop"])
    check("nothing new: nothing recorded", gd.record_versions(root), [])
    check("sync goes down to the first failing node", gd.sync_live(root), ["(gốc)", "feature-hero", "feature-shop"])
    check("live has the passing nodes only", paths(gd.read_tree(root, "live")[0]), ["", "feature-hero", "feature-shop"])
    check("again: nothing to do", gd.sync_live(root), [])
    check("live holds no review, questions or history",
          [p.name for p in gd.table_dir(root, "live").rglob(".*") if p.is_dir()], [])
    gd.save_review(root, "", {"win": {"score": 2, "feedback": "10 đợt quá ngắn"}})
    mk(root, "feature-shop", items=(("buy", 9, "Mua bằng xu."),))
    check("a failing root stops everything below", gd.sync_live(root), [])
    mk(root, "", items=(("loop", 8, "Vòng lặp."), ("win", 7, "Chơi vô tận, tính điểm theo đợt.")))
    check("the rewrite is recorded with the old text", gd.record_versions(root), ["(gốc)", "feature-shop"])
    events = gd.read_history(folder(root, "idea"))
    check("history: data, promote, review, data", [e["event"] for e in events], ["data", "promote", "review", "data"])
    check("history keeps what was rewritten", events[-1]["rewritten"], [{"id": "win", "old": "Thắng sau 10 đợt.", "new": "Chơi vô tận, tính điểm theo đợt."}])
    check("history: the rejected idea with its feedback",
          [(e["item"], e["score"], e["feedback"]) for e in gd.rejected(folder(root, "idea"))], [("win", 2, "10 đợt quá ngắn")])
    check("history: who promoted", events[1]["by"], "người (Sync)")
    check("history keeps a copy of every version", len(list(folder(root, "idea/.history").glob("data-*.md"))), 2)
    check("the snapshot carries the history, newest first", snap_node(root)["history"][0]["event"], "data")
    check("children are stale after the root's rewrite; only the root is promoted", gd.sync_live(root), ["(gốc)"])
    mk(root, "feature-hero"), mk(root, "feature-shop", items=(("buy", 9, "Mua bằng xu."),))
    check("redone children are promoted again", gd.sync_live(root), ["feature-hero", "feature-shop"])
    check("deep promote refused when a node below fails", refused(gd.promote, root, "feature-hero", True))
    check("child before its parent is refused", refused(gd.promote, root, "feature-hero/skills/cooldown", False))
    for p in ("..", "feature-hero/..", "/feature-hero", "Feature", "a/b/c/d", "feature-hero\\skills", None):
        check(f"bad path refused: {p}", refused(gd.promote, root, p, True))
    mk(root, "feature-hero/skills", items=(("three", 7, "Ba kĩ năng."),))
    mk(root, "feature-hero/skills/cooldown")
    check("deep promote of a passing branch", gd.promote(root, "feature-hero", True),
          ["feature-hero", "feature-hero/skills", "feature-hero/skills/cooldown"])
    states = {n["path"]: (n["state"], n["dirty"]) for n in gd.walk(gd.snapshot(root)["idea"])}
    check("everything is the same as live now", set(states.values()), {("same", False)})

    # ---- status
    gd.record_versions(root)
    text = gd_status.report(root)
    for label, part in (("mode", "Full Auto"), ("scale", "8-10"), ("verdict", "ĐẠT"), ("hash for the children", "based-on: " + snap_node(root)["dataHash"]),
                        ("rejected idea", "10 đợt quá ngắn"), ("scores", "Ý loop: AI 8, người chưa chấm"), ("previous score", "bản trước của ý này người chấm 2")):
        check("status shows: " + label, part in text)
    check("status of one branch leaves the others out", "feature-shop" in gd_status.report(root, "ai/idea/feature-hero"), False)
    for arg in ("live", "ai/idea/../live", "ai/idea/feature-none", "idea/feature-hero", "ai/idea/Bad"):
        check(f"status refused: {arg}", refused(gd_status.report, root, arg))

    # ---- delete
    put(root, "live/keep.md")
    put(root, "outside.md")
    put(root, "idea/feature-hero/.ai-need-user-choose/answers.json", "{}")
    gd.save_review(root, "feature-hero/skills", {"three": {"score": 1, "feedback": "bỏ"}})
    for arg in ("live/keep.md", "ai", "ai/", "ai/idea", "idea/data.md", "ai/../live/keep.md", "ai/../../outside.md", "../x",
                "ai/./idea/data.md", "C:/x", "ai/idea/nope.md", "ai/idea/feature-hero/.ai-need-user-choose/answers.json",
                "ai/idea/.history", "ai/idea/.history/log.jsonl", "ai/idea/feature-hero/skills/.user-review", "ai/idea/feature-shop/.progress",
                "ai/idea/feature-hero/skills/.user-review/review.json"):
        check(f"delete refused: {arg}", refused(gd_delete.delete, root, arg))
    check("live untouched", folder(root, "live/keep.md").is_file())
    gd_delete.delete(root, "ai/idea/feature-hero/.ai-need-user-choose")
    check("deletes a questions folder", folder(root, "idea/feature-hero/.ai-need-user-choose").exists(), False)
    gd_delete.delete(root, "ai/idea/feature-hero/skills")
    check("deletes a node", folder(root, "idea/feature-hero/skills").exists(), False)
    kept = sorted(p.name.rsplit("-", 2)[0] for p in folder(root, "idea/feature-hero/.history/removed").iterdir())
    check("its history and its children's move to the parent", kept, ["skills", "skills-cooldown"])
    removed = [e for e in gd.read_history(folder(root, "idea/feature-hero")) if e["event"] == "removed"][0]
    check("the parent's log notes what was removed", (removed["key"], removed["items"][0]["id"]), ("skills", "three"))
    check("deep promote removes from live what idea dropped", (gd.promote(root, "feature-hero", True), paths(gd.read_tree(root, "live")[0])),
          (["feature-hero"], ["", "feature-hero", "feature-shop"]))
    check("no work folders left", sorted(p.name for p in Path(root).joinpath(*gd.BASE).iterdir()), ["ai", "live", "outside.md"])


def main():
    for case in (run_data, run_scores, run_live_scores, run_priority, run_progress, run_flow):
        root = tempfile.mkdtemp(prefix="gdfx-")
        try:
            case(root)
        finally:
            shutil.rmtree(root, ignore_errors=True)
    failed = [x for x in results if not x[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
