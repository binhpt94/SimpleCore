#!/usr/bin/env python3
"""Tests for gamedesign.py, gd_delete.py, gd_status.py and gd_pack.py, on a throw-away tree in a temp folder.

    python Management/tools/test_gamedesign.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_delete  # noqa: E402
import gd_pack  # noqa: E402
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
    table = [x for x in gd_status.report(root).split("\n") if x.startswith("- ") and ("ĐƯỢC CODE" in x or "ĐANG KHOÁ" in x)
             and "(feature-" in x]
    check("the whole status lists the code gate of every feature: open at round 4 in live, locked below it",
          ([x.split(":")[0] + ":" + x.split(":")[1].split(",")[0].split(" ·")[0] for x in table if x.startswith(("- Hero (", "- Wallet ("))],
           "## Cổng code theo feature" in gd_status.report(root), "## Cổng code theo feature" in gd_status.report(root, "ai/idea/feature-hero")),
          (["- Hero (feature-hero): ĐƯỢC CODE", "- Wallet (feature-wallet): ĐANG KHOÁ"], True, False))
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

    # ---- one row per feature folder: the page's table and the gate lines of gd_status
    mk(root, "feature-shop")
    rows = gd.snapshot(root)["features"]
    by = {r["unity"]: r for r in rows}
    check("feature rows: one per folder named in code.json, and one for a feature that names none",
          sorted(r["unity"] for r in rows), ["", "Hero", "Wallet", "_Services/Popup"])
    check("feature rows: the gate and how far each tree reached",
          ((by["Hero"]["gate"]["ok"], by["Hero"]["roundIdea"], by["Hero"]["roundLive"]),
           (by["Wallet"]["roundIdea"], by["Wallet"]["roundLive"]), (by[""]["declared"], by[""]["gate"]["ok"], by[""]["roundLive"])),
          ((True, 4, 4), (4, 4), (False, False, 0)))
    check("feature rows: the leaves done in the first phase, the design state, the requirement book",
          (by["Hero"]["leaves"], by["Hero"]["design"] in ("redo", "scoring", "lock", "live"),
           (by["Hero"]["reqs"]["met"], by["Hero"]["reqs"]["total"], by["Hero"]["reqs"]["counts"]["todo"]), by[""]["reqs"]),
          ([1, 1], True, (0, 2, 2), None))
    check("feature rows: open requirements of a folder that may be coded wait for its session; a locked one waits for nobody's code",
          ("Hero" in by["Hero"]["waiting"], "Shop" in by[""]["waiting"]), (True, False))
    lines = gd_status.gate_lines(gd.snapshot(root))
    check("the gate lines are the rows, one each", (len(lines), any(x.startswith("- (chưa khai thư mục code) (feature-shop)") for x in lines)), (4, True))


def run_infra(root):
    """Round 2 always owes the infrastructure features; the reminder blocks nothing."""
    owed = lambda: [(m["path"], "chưa có node" in m["text"]) for m in gd.snapshot(root)["missing"]]      # noqa: E731
    mk(root, "", items=[("one", 3, "Ý kém.")])
    check("nothing is owed while round 1 does not pass", owed(), [])
    mk(root, "")
    check("round 1 passes: all three are owed", owed(), [("feature-scene-flow", True), ("feature-session-scene", True), ("feature-popup", True)])
    check("status lists them", all(k in gd_status.report(root) for k in gd.INFRA_FEATURES) and "Thiếu feature hạ tầng" in gd_status.report(root))
    mk(root, "feature-popup"), mk(root, "feature-scene-flow"), mk(root, "feature-session-scene")
    put(root, "idea/feature-popup/code.json", '{"unity": ["_Services/Popup"]}')
    put(root, "idea/feature-scene-flow/code.json", '{"unity": ["Popup"]}')
    check("a node without the right code.json is still owed, by another text", owed(), [("feature-scene-flow", False), ("feature-session-scene", False)])
    put(root, "idea/feature-scene-flow/code.json", '{"unity": ["SceneFlow"]}')
    put(root, "idea/feature-session-scene/code.json", '{"unity": ["_Services/SessionScene"]}')
    check("all three there with their folders: nothing owed", (owed(), "Thiếu feature hạ tầng" in gd_status.report(root)), ([], False))
    check("the reminder is no structure problem and blocks no promote", (gd.snapshot(root)["problems"]["idea"], gd.promote(root, "", False)), ([], ["(gốc)"]))
    check("an infrastructure feature is locked like any other until round 4 is in live", gd.code_allowed(root, "SceneFlow")[0], False)


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


def run_code_names(root):
    """GD names the code folder from the key; setup and scaffold read the names from the live table."""
    for key, want in (("feature-hero", "Hero"), ("feature-popup-lose", "PopupLose"), ("feature-team-select", "TeamSelect"),
                      ("feature-popup", "_Services/Popup"), ("feature-session-scene", "_Services/SessionScene"),
                      ("hud", "Hud"), ("feature-2d", None), ("feature-", None)):
        check(f"code name from key {key}", gd.suggested_code_name(key), want)
    owed = lambda: [(m["path"], m["text"]) for m in gd.snapshot(root)["missingCode"]]      # noqa: E731
    mk(root, "", items=[("one", 3, "Ý kém.")])
    mk(root, "feature-hero")
    check("nothing is suggested while round 1 does not pass", owed(), [])
    mk(root, "")
    mk(root, "feature-hero"), mk(root, "feature-wallet"), mk(root, "feature-popup")
    check("round-2 nodes without code.json get the code.json to write; infra is left to its own reminder", owed(),
          [("feature-hero", 'ghi code.json: {"unity": ["Hero"]}'), ("feature-wallet", 'ghi code.json: {"unity": ["Wallet"]}')])
    check("status lists them", "Thiếu code.json" in gd_status.report(root) and '{"unity": ["Hero"]}' in gd_status.report(root))
    put(root, "idea/feature-hero/code.json", '{"unity": ["Hero"]}')
    put(root, "idea/feature-popup/code.json", '{"unity": ["_Services/Popup"]}')
    check("written code.json is no longer suggested", [p for p, _ in owed()], ["feature-wallet"])
    check("the reminder blocks nothing", gd.snapshot(root)["problems"]["idea"], [])
    check("live names nothing before the human promotes", gd.live_code_names(root), [])
    gd.promote(root, "", True)
    check("live names each code folder with its node, sorted", gd.live_code_names(root),
          [("_Services/Popup", "feature-popup"), ("Hero", "feature-hero")])
    put(root, "live/feature-hero/skills/code.json", '{"unity": ["Skills"]}')
    check("code.json below round 2 does not count", [n for n, _ in gd.live_code_names(root)], ["_Services/Popup", "Hero"])
    check("live features: declared names, and the suggested name of a node without code.json", gd.live_feature_names(root),
          [("_Services/Popup", "feature-popup", True), ("Hero", "feature-hero", True), ("Wallet", "feature-wallet", False)])
    put(root, "live/feature-wallet/code.json", "{ broken")
    put(root, "live/feature-lost/code.json", '{"unity": ["Lost"]}')      # a folder without data.md is no node
    check("a broken code.json suggests nothing; code.json without a node still declares",
          [n for n, _, _ in gd.live_feature_names(root)], ["_Services/Popup", "Hero", "Lost"])
    check("the code gate still needs code.json in live", gd.code_allowed(root, "Wallet")[0], False)


def add_refs(root, path, line, table="idea"):
    """Puts `refs: <line>` into the front matter of the node's data.md."""
    p = folder(root, f"{table}/{path}/data.md")
    p.write_text(p.read_text(encoding="utf-8").replace("\n---\n", f"\nrefs: {line}\n---\n", 1), encoding="utf-8")


def glossary_node(root, items, table="idea"):
    """Writes the glossary node: items are (id, title, detail)."""
    based = gd.read_data(folder(root, table))["hash"]
    body = "".join(f"\n### {i} | AI 8 | {title}\n{detail}\n" for i, title, detail in items)
    put(root, f"{table}/glossary/data.md", f"---\nbased-on: {based}\n---\n# Thuật ngữ\n\nTừ dùng chung.\n\n## {gd.ITEMS_TITLE}\n{body}")
    put(root, f"{table}/glossary/CLAUDE.md", "# Thuật ngữ\n\nDữ liệu cốt lõi: data.md\n")


def term_hash(root, tid, table="idea"):
    return next(it["hash"] for it in gd.read_data(folder(root, f"{table}/glossary"))["items"] if it["id"] == tid)


def run_refs(root):
    """refs: nodes of other branches a node builds on, each with the hash it was checked against."""
    text = data_text("T", [("a", 8, "B.")], "abc")
    with_refs = text.replace("\n---\n", "\nrefs: [feature-x/y@0123456789ab, glossary#wave@abcdefabcdef]\n---\n", 1)
    d = gd.parse_data(with_refs)
    check("refs are read, with an optional term", d["refs"], [{"path": "feature-x/y", "term": "", "hash": "0123456789ab"},
                                                            {"path": "glossary", "term": "wave", "hash": "abcdefabcdef"}])
    check("refs are not in the data hash", (d["hash"] == gd.parse_data(text)["hash"], d["errors"]), (True, []))
    check("refs separated by spaces too", len(gd.parse_data(text.replace("\n---\n", "\nrefs: a/b@0123456789ab c/d@0123456789ab\n---\n", 1))["refs"]), 2)
    check("a malformed ref is an error", bool(gd.parse_data(text.replace("\n---\n", "\nrefs: a/b@zz\n---\n", 1))["errors"]))

    mk(root, "")
    for path in ("feature-a", "feature-a/a1", "feature-a/a1/a1x", "feature-b"):
        mk(root, path)
    mk(root, "feature-b/b1", doc="Dựa trên `feature-a/a1`, thuộc `feature-b`, chính nó `feature-b/b1`, feature `feature-a`.")
    a1 = gd.read_data(folder(root, "idea/feature-a/a1"))["hash"]
    n = snap_node(root, "feature-b/b1")
    check("a round-3/4 node of another branch mentioned without a ref: redo",
          (n["passed"], n["redo"], [r["label"] for r in n["refMissing"]]), (False, True, ["feature-a/a1"]))
    check("its own lineage and round-2 nodes are not asked for", len(n["refMissing"]), 1)
    check("the reason says what to declare", any(f"feature-a/a1@{a1}" in r for r in n["reasons"]))
    check("the refs line to write", n["refLine"], f"refs: feature-a/a1@{a1}")
    mk(root, "feature-b/b1/b1x")
    check("its children are locked while the ref is missing", gd.can_write_node(root, ["feature-b", "b1", "b1x"])[0], False)
    add_refs(root, "feature-b/b1", f"feature-a/a1@{a1}")
    n = snap_node(root, "feature-b/b1")
    check("a matching ref passes", (n["passed"], n["refs"][0]["ok"], n["refs"][0]["title"]), (True, True, "a1"))
    check("children open again", gd.can_write_node(root, ["feature-b", "b1", "b1x"])[0], True)
    check("declaring refs does not touch the data hash, so no child goes stale", snap_node(root, "feature-b/b1/b1x")["stale"], False)
    mk(root, "feature-a/a1", items=[("core", 8, "Ý đã đổi.")])
    n = snap_node(root, "feature-b/b1")
    check("the target changed: the ref is stale, redo", (n["passed"], n["redo"], n["refStale"]), (False, True, ["feature-a/a1"]))
    check("the stale reason names both hashes", any(a1 in r and "đã đổi" in r for r in n["reasons"]))
    check("the write gate sees the stale ref", gd.can_write_node(root, ["feature-b", "b1", "b1x"])[0], False)

    mk(root, "feature-b/b2", doc="Xem `ai/idea/feature-a/a1/` và `live/feature-a/a1/a1x/CLAUDE.md`.")
    check("table prefixes and file names are normalized",
          sorted(r["label"] for r in snap_node(root, "feature-b/b2")["refMissing"]), ["feature-a/a1", "feature-a/a1/a1x"])
    mk(root, "feature-b/b3", doc="Xem `feature-a/none`.")
    n = snap_node(root, "feature-b/b3")
    check("a mention of a node that does not exist is only a reminder", (n["passed"], len(n["refNotes"])), (True, 1))
    mk(root, "feature-b/b4")
    add_refs(root, "feature-b/b4", "feature-z/q@0123456789ab, feature-b@0123456789ab")
    probs = [p["text"] for p in gd.snapshot(root)["problems"]["idea"] if p["path"] == "feature-b/b4"]
    check("a ref to no node, or to its own ancestor, is a structure problem", len(probs), 2)
    check("ancestors from the root down", [a["path"] for a in gd.ancestors(gd.read_tree(root, "idea")[0], "feature-a/a1/a1x")],
          ["", "feature-a", "feature-a/a1"])
    check("no ancestors for the root", gd.ancestors(gd.read_tree(root, "idea")[0], ""), [])
    report = gd_status.report(root, "ai/idea/feature-b/b1")
    check("status of one node starts with what it builds on", "## Bám theo" in report and "- feature-a/a1 (vòng 3)" in report)
    check("status prints the refs line to write", "ghi dòng này vào phần đầu data.md: `refs: feature-a/a1@" in report)
    check("no glossary node yet: a reminder", any("chưa có node glossary" in t["text"] for t in gd.snapshot(root)["termNotes"]["idea"]))
    check("the page gets no second copy of the text", "refText" in snap_node(root, "feature-b/b1"), False)


def run_glossary(root):
    """The glossary node: one idea per term, refs per term, not a feature."""
    mk(root, "")
    glossary_node(root, [("wave", "Đợt: một nhóm quái vào cùng lúc", "Ví dụ đợt 3."), ("hp", "Máu: lượng sát thương chịu được", "")])
    snap = gd.snapshot(root)
    check("terms are read from the ideas", [(t["id"], t["term"], t["meaning"]) for t in snap["glossary"]["idea"]["terms"]],
          [("wave", "Đợt", "một nhóm quái vào cùng lúc"), ("hp", "Máu", "lượng sát thương chịu được")])
    check("a clean glossary has no problem", snap["problems"]["idea"], [])
    check("the glossary is not a feature: no code.json asked", snap["missingCode"], [])
    g = snap_node(root, "glossary")
    check("the glossary has no importance", (g["priority"].get("glossary"), g["priority"]["leaf"]), (True, False))
    check("status does not ask the glossary for a priority", "BẠN CHƯA CHẤM" in gd_status.report(root, "ai/idea/glossary"), False)
    check("the glossary has no progress", refused(gd.save_progress, root, "glossary", "idea", "done", "@CodeLead"))

    mk(root, "feature-a")
    mk(root, "feature-a/a1", doc="Dùng `glossary#wave`.")
    n = snap_node(root, "feature-a/a1")
    check("a term mentioned without a ref: redo", (n["passed"], [r["label"] for r in n["refMissing"]]), (False, ["glossary#wave"]))
    add_refs(root, "feature-a/a1", f"glossary#wave@{term_hash(root, 'wave')}")
    check("a matching term ref passes", snap_node(root, "feature-a/a1")["passed"], True)
    check("the glossary lists who uses a term", gd.snapshot(root)["glossary"]["idea"]["terms"][0]["usedBy"], ["feature-a/a1"])
    glossary_node(root, [("wave", "Đợt: một nhóm quái vào cùng lúc", "Ví dụ đợt 3."), ("hp", "Máu: phần máu còn lại", "")])
    check("rewriting another term leaves the ref matching", snap_node(root, "feature-a/a1")["passed"], True)
    glossary_node(root, [("wave", "Đợt: các quái vào trong một lượt gọi", ""), ("hp", "Máu: phần máu còn lại", "")])
    check("rewriting the term makes the ref stale", snap_node(root, "feature-a/a1")["refStale"], ["glossary#wave"])
    add_refs(root, "feature-a/a1", "glossary#nope@0123456789ab")
    check("a ref to a term that does not exist is a structure problem",
          any("glossary#nope" in p["text"] for p in gd.snapshot(root)["problems"]["idea"]))

    mk(root, "feature-a/a2", doc="## Định nghĩa\n- **Đợt**: khác\n- **Tầm**: xa\n")
    mk(root, "feature-a/a3", summary="Hero mất Máu khi bị đánh.", doc="## Định nghĩa\n- **Tầm**: gần\n")
    notes = [t["text"] for t in gd.snapshot(root)["termNotes"]["idea"]]
    check("redefining a glossary term is a reminder", any("định nghĩa lại 'Đợt'" in t for t in notes))
    check("a term defined outside the glossary in two places with two meanings",
          any("'Tầm' còn định nghĩa ngoài glossary" in t and "2 nơi" in t for t in notes))
    check("a term written out without a ref", any("có dùng từ 'Máu'" in t for t in notes))
    check("reminders block nothing", snap_node(root, "feature-a/a3")["passed"], True)
    check("status lists the glossary reminders", "## Thuật ngữ" in gd_status.report(root))

    glossary_node(root, [("wave", "Đợt không có dấu hai chấm", ""), ("w2", "Đợt: lặp lại", "")])
    probs = [p["text"] for p in gd.snapshot(root)["problems"]["idea"] if p["path"] == "glossary"]
    check("a term title without ':' is a problem", any("<từ>: <nghĩa>" in t for t in probs))
    glossary_node(root, [("wave", "Đợt: a", ""), ("w2", "đợt : b", "")])
    check("the same term twice is a problem", any("có ở cả ý" in p["text"] for p in gd.snapshot(root)["problems"]["idea"]))
    glossary_node(root, [("wave", "Đợt: a", "")])
    put(root, "idea/glossary/code.json", '{"unity": ["Glossary"]}')
    mk(root, "glossary/child")
    probs = [p["text"] for p in gd.snapshot(root)["problems"]["idea"] if p["path"] == "glossary"]
    check("the glossary has no code.json and no children", (any("code.json" in t for t in probs), any("node con" in t for t in probs)), (True, True))
    shutil.rmtree(folder(root, "idea/glossary/child"))
    folder(root, "idea/glossary/code.json").unlink()
    for path in ("", "glossary", "feature-a"):
        gd.promote(root, path, False)
    check("live features leave the glossary out", [k for _, k, _ in gd.live_feature_names(root)], ["feature-a"])
    check("the glossary never opens a code folder", gd.code_allowed(root, "Glossary")[0], False)


def run_pack(root):
    """gd_pack prints the fixed design of one code folder: lineage, branch, refs, terms."""
    mk(root, "")
    glossary_node(root, [("wave", "Đợt: một nhóm quái vào cùng lúc", "Ví dụ đợt 3.")])
    mk(root, "feature-b"), mk(root, "feature-b/b1", items=[("b-core", 8, "Ý của b1.\nChi tiết b1.")])
    mk(root, "feature-a")
    put(root, "idea/feature-a/code.json", '{"unity": ["Alpha"]}')
    mk(root, "feature-a/a1", doc="## Thông số\nDựa trên `feature-b/b1` và `glossary#wave`.")
    add_refs(root, "feature-a/a1", f"feature-b/b1@{gd.read_data(folder(root, 'idea/feature-b/b1'))['hash']}, glossary#wave@{term_hash(root, 'wave')}")
    mk(root, "feature-a/a1/a1x")
    gd.promote(root, "", True)
    text = gd_pack.pack(root, "alpha")
    check("pack: the code gate", "Code: ĐƯỢC CODE" in text)
    check("pack: the root as context", "## Bối cảnh: node gốc" in text)
    check("pack: every node of the branch, in full", all(f"## {p}  ·" in text for p in ("feature-a", "feature-a/a1", "feature-a/a1/a1x")))
    check("pack: CLAUDE.md headings sit below the node heading", "#### Thông số" in text)
    check("pack: the node a ref names, with its detail", "## feature-b/b1  ·" in text and "Chi tiết b1." in text
          and "live/feature-b/b1/CLAUDE.md" in text)
    check("pack: other nodes stay out", "## feature-b  ·" in text, False)
    check("pack: the terms the branch uses", "**Đợt**: một nhóm quái vào cùng lúc (Ví dụ đợt 3.)" in text)
    check("pack: no warning when refs match", "Cảnh báo" in text, False)
    check("pack: a folder live does not name", refused(gd_pack.pack, root, "Nope"))
    check("pack: exit 1 for an unknown folder, 2 for a bad name",
          (gd_pack.main(["gd_pack.py", "Nope", "--root", root]), gd_pack.main(["gd_pack.py", "../x", "--root", root])), (1, 2))

    # A long report goes to a file named by its hash; the tool prints the path and a table of contents.
    store = Path(root, "Management", "game-design", gd.PACK_DIR)
    files = lambda: sorted(p.name for p in store.glob("*")) if store.exists() else []
    check("emit: a short text is printed as it is, no file", (gd.emit(root, "pack", "Alpha", text, "note"), files()), (text, []))
    long = "# Gói\nCode: ĐƯỢC CODE\n\n## a  ·  vòng 3  ·  A\nTóm tắt a\n" + "x\n" * 12000 + "## a/b  ·  vòng 4  ·  B\n\nTóm tắt b\ny"
    shown = gd.emit(root, "pack", "_Services/Popup", long, "NHẮC")
    name = files()[0] if files() else ""
    check("emit: a long text is written once, under the name of its hash",
          (len(files()), bool(re.fullmatch(r"pack-_Services\.Popup-[0-9a-f]{12}\.md", name))), (1, True))
    check("emit: the file holds the text", gd.read_text(store / name), long + "\n")
    check("emit: prints the head, the path and the note, not the text",
          (shown.startswith("# Gói\nCode: ĐƯỢC CODE\n\n"), f"Management/game-design/{gd.PACK_DIR}/{name}" in shown, "NHẮC" in shown,
           len(shown) < 1500), (True, True, True, True))
    body = long.split("\n")
    toc = [m for m in re.finditer(r"^(\d+)-(\d+)  (#{1,2} [^\n]*?)(?:  —  ([^\n]*))?$", shown, re.M)]
    check("emit: each contents line points at its heading in the file",
          [(body[int(m.group(1)) - 1] == m.group(3), m.group(4)) for m in toc],
          [(True, "Code: ĐƯỢC CODE"), (True, "Tóm tắt a"), (True, "Tóm tắt b")])
    check("emit: the ranges cover the file without gaps",
          [(int(m.group(1)), int(m.group(2))) for m in toc], [(1, 3), (4, 12005), (12006, 12009)])
    stamp = (store / name).stat().st_mtime_ns
    check("emit: the same text is the same file, not written again",
          (gd.emit(root, "pack", "_Services/Popup", long, "NHẮC") == shown, files(), (store / name).stat().st_mtime_ns), (True, [name], stamp))
    for i in range(4):
        os.utime(store / sorted(files())[-1], (1000 + i, 1000 + i))      # older than the next one
        gd.emit(root, "pack", "_Services/Popup", long + f"\n{i}", "")
    gd.emit(root, "status", "all", long, "")
    check("emit: a changed text is a new file; only the newest few of one report are kept",
          (len([f for f in files() if f.startswith("pack-")]), name in files(), len([f for f in files() if f.startswith("status-all-")])),
          (gd.PACK_KEEP, False, 1))
    check("emit: no temp file is left", [f for f in files() if f.endswith(".tmp")], [])


UI_FRAME = """kind: ui
title: Popup thua   # chú thích
let tap = 120
- panel round-rect x=560 y=190 w=800 h=700 label="Thua # không phải chú thích"
- replay button x=60 y=560 w=320 h=tap parent=panel label="Chơi lại"
- home button x=420 y=560 w=320 h=80 parent=panel
- slots grid x=40 y=100 cols=3 rows=2 cell=150 gap=20 parent=panel
"""
FIELD_FRAME = """kind: field
view: x=-20u..20u y=-2u..8u
use reach = feature-b/b1/b1x#archer-range
- castle rect x=-1.5u y=0 w=3u h=4u
- spawn arrow from=-17u,0 to=-2u,0
- archer ring cx=-6u cy=1u r=reach
"""


def front(root, path, line, table="idea"):
    """Adds `line` to the front matter of the node's data.md (creating it on the root)."""
    p = folder(root, f"{table}/{path}/data.md" if path else f"{table}/data.md")
    text = p.read_text(encoding="utf-8")
    p.write_text(text.replace("---\n", f"---\n{line}\n", 1) if text.startswith("---\n") else f"---\n{line}\n---\n{text}",
                 encoding="utf-8")


def run_frames(root):
    """Frames: parsed, resolved, checked, drawn, hashed, copied to live, printed by gd_frame and gd_pack."""
    import gd_frame
    f = gd.parse_frame(UI_FRAME)
    check("frame: a good ui frame parses", (f["errors"], f["kind"], f["title"], len(f["shapes"])), ([], "ui", "Popup thua", 4))
    check("frame: a quoted '#' is not a comment", f["shapes"][0]["params"]["label"][1], "Thua # không phải chú thích")
    r = gd.resolve_frame(f, gd.SCREENS["landscape"], lambda p, n: (None, "x"))
    by = {s["id"]: s for s in r["shapes"]}
    check("frame: parent makes positions absolute", (by["replay"]["x"], by["replay"]["y"], by["replay"]["h"]), (620, 750, 120))
    check("frame: a grid's size from its cells", (by["slots"]["w"], by["slots"]["h"]), (490, 320))
    check("frame: a button under the tap size is a warning", any(w.startswith("home: nút") for w in r["warnings"]))
    check("frame: a button at the tap size is not", any(w.startswith("replay") for w in r["warnings"]), False)
    shown = lambda shape, label: gd.display_text({"shape": shape, "label": label})      # noqa: E731
    check("text: what the player reads is the label of a label or a button, without the note in brackets",
          (shown("button", "Chơi lại (nút chính)"), shown("label", "Kỉ lục: 15 đợt"), shown("label", "A (b) (c)"), shown("rect", "Nhóm hero"),
           shown("icon", "xu"), shown("button", "")), ("Chơi lại", "Kỉ lục: 15 đợt", "A", "", "", ""))
    texts = lambda svg: re.findall(r"<text[^>]*>([^<]*)</text>", svg)      # noqa: E731
    plain, concept = texts(gd.frame_svg(r)), texts(gd.frame_svg(r, concept=True))
    wanted = [gd._esc(t) for t in (gd.display_text(s) for s in r["shapes"]) if t]
    check("text: the frame view keeps every label with its numbers", (len(plain), all("x" in t or "=" in t for t in plain)), (len(r["shapes"]), True))
    check("text: a concept shows only what the player reads, no numbers and no notes", concept, wanted)
    check("text: the font of frames and concepts is Roboto, carried inside what is drawn",
          ("font-family: Roboto" in gd.FRAME_CSS, "font-family: 'Roboto'" in gd.font_css(), "data:font/ttf;base64," in gd.font_css(),
           "url(view/fonts/Roboto-Regular.ttf)" in gd.font_css(embed=False)), (True, True, True, True))
    kept, gd.FONT_DIR = gd.FONT_DIR, Path(root) / "no-fonts"
    check("text: without the font files nothing is added (the system font is used)", gd.font_css(), "")
    gd.FONT_DIR = kept
    check("frame: no overlap warning between a parent and its children", any("chồng" in w for w in r["warnings"]), False)
    bad = {
        "unknown shape": "kind: ui\n- a star x=1 y=1 w=1 h=1",
        "duplicate id": "kind: ui\n- a rect x=1 y=1 w=1 h=1\n- a rect x=1 y=1 w=1 h=1",
        "no kind": "- a rect x=1 y=1 w=1 h=1",
        "a let that is not a number": "kind: ui\nlet a = big",
        "a bad use": "kind: ui\nuse a = nowhere",
        "an unknown flag": "kind: ui\n- a rect x=1 y=1 w=1 h=1 sideways",
        "an unknown line": "kind: ui\nwhat is this",
    }
    for label, text in bad.items():
        check("frame error: " + label, bool(gd.parse_frame(text)["errors"]))
    resolve_bad = {
        "a missing parameter": "kind: ui\n- a rect x=1 y=1 w=1",
        "u in a ui frame": "kind: ui\n- a rect x=1u y=1 w=1 h=1",
        "an unknown name": "kind: ui\n- a rect x=gap y=1 w=1 h=1",
        "a parent below it": "kind: ui\n- a rect x=1 y=1 w=1 h=1 parent=b\n- b rect x=1 y=1 w=5 h=5",
        "a field without view": "kind: field\n- a rect x=1 y=1 w=1 h=1",
        "a zero size": "kind: ui\n- a rect x=1 y=1 w=0 h=1",
        "a fill above 1": "kind: ui\n- a bar x=1 y=1 w=10 h=1 fill=2",
    }
    for label, text in resolve_bad.items():
        check("frame error: " + label, bool(gd.resolve_frame(gd.parse_frame(text), (1920, 1080), lambda p, n: (None, "x"))["errors"]))
    warned = gd.resolve_frame(gd.parse_frame("kind: ui\n- a rect x=1800 y=1 w=200 h=10\n- b rect x=0 y=0 w=50 h=50\n"
                                             "- c rect x=40 y=40 w=50 h=50\n- d rect x=40 y=40 w=50 h=50 overlap"), (1920, 1080), None)["warnings"]
    check("frame: off the screen, overlap, and overlap allowed",
          (any(w.startswith("a: tràn") for w in warned), any(w.startswith("b và c") for w in warned), any("d " in w for w in warned)),
          (True, True, False))

    # in a tree: the root's screen, a use through refs, the hash, the reminders
    mk(root, "")
    mk(root, "feature-a"), mk(root, "feature-a/a1"), mk(root, "feature-a/a1/a1x")
    mk(root, "feature-b"), mk(root, "feature-b/b1"), mk(root, "feature-b/b1/b1x")
    leaf = snap_node(root, "feature-a/a1/a1x")
    check("frame: a round-4 node without frame is reminded", any("chưa có frame" in n for n in leaf["frameNotes"]))
    check("frame: the root without screen is reminded", any("hướng màn hình" in n for n in snap_node(root)["frameNotes"]))
    front(root, "", "screen: landscape")
    check("frame: screen is read and not in the hash", (snap_node(root)["screen"], snap_node(root)["frameNotes"]), ("landscape", []))
    check("frame: a wrong screen is a data.md error", bool(gd.parse_data("---\nscreen: square\n---\n# T\n")["errors"]))
    front(root, "feature-b/b1/b1x", "frame: none")
    b = snap_node(root, "feature-b/b1/b1x")
    check("frame: 'frame: none' silences the reminder", (b["frameNone"], b["frameNotes"]), (True, []))
    before = gd.read_data(folder(root, "idea/feature-b/b1/b1x"))["hash"]
    put(root, "idea/feature-b/b1/b1x/.frame-concept/range.frame", "kind: field\nview: x=-20u..20u y=-2u..8u\nlet archer-range = 5u\n")
    after = gd.read_data(folder(root, "idea/feature-b/b1/b1x"))["hash"]
    check("frame: adding a frame changes the data hash", before != after)
    check("frame: a node without frames keeps its hash", gd.read_data(folder(root, "idea/feature-a/a1/a1x"))["hash"],
          gd.parse_data(folder(root, "idea/feature-a/a1/a1x/data.md").read_text(encoding="utf-8"))["hash"])
    put(root, "idea/feature-a/a1/a1x/.frame-concept/main.frame", UI_FRAME)
    put(root, "idea/feature-a/a1/a1x/.frame-concept/field.frame", FIELD_FRAME)
    leaf = snap_node(root, "feature-a/a1/a1x")
    check("frame: frames are read, by name", [fr["file"] for fr in leaf["frames"]], ["field.frame", "main.frame"])
    field = leaf["frames"][0]
    check("frame: a use reads the other node's let", field["values"].get("reach"), 5.0)
    check("frame: a use asks for the node in refs", [m["label"] for m in leaf["refMissing"]], ["feature-b/b1/b1x"])
    check("frame: so the node must redo", leaf["redo"])
    add_refs(root, "feature-a/a1/a1x", f"feature-b/b1/b1x@{after}")
    check("frame: declared, the use is fine", (snap_node(root, "feature-a/a1/a1x")["refMissing"], snap_node(root, "feature-a/a1/a1x")["redo"]), ([], False))
    put(root, "idea/feature-b/b1/b1x/.frame-concept/range.frame", "kind: field\nview: x=-20u..20u y=-2u..8u\nlet archer-range = 6u\n")
    check("frame: the other node's frame changed, the ref is stale", snap_node(root, "feature-a/a1/a1x")["refStale"], ["feature-b/b1/b1x"])
    check("frame: a field frame is drawn y up, with numbers", ("<svg" in field["svg"] and 'data-id="archer"' in field["svg"]
                                                              and "r=5u" in field["svg"]))
    check("frame: labels are escaped in the drawing", "&lt;" in gd.frame_svg(gd.resolve_frame(
        gd.parse_frame('kind: ui\n- a rect x=1 y=1 w=9 h=9 label="<b>"'), (1920, 1080), None)), True)
    put(root, "idea/feature-a/a1/a1x/.frame-concept/broken.frame", "kind: ui\n- a star")
    tree, problems = gd.read_tree(root, "idea")
    check("frame: a broken frame is a structure problem", any("broken.frame" in p["text"] for p in problems))
    folder(root, "idea/feature-a/a1/a1x/.frame-concept/broken.frame").unlink()
    put(root, "idea/feature-a/.frame-concept/x.frame", "kind: ui\n")
    check("frame: only round 4 has frames", any("chỉ dùng ở node vòng" in n for n in snap_node(root, "feature-a")["frameNotes"]))
    folder(root, "idea/feature-a/.frame-concept/x.frame").unlink()

    # status, gd_frame, promote, pack
    status = gd_status.report(root, "ai/idea/feature-a/a1/a1x")
    check("frame: gd_status prints the resolved frame", "replay button x=620 y=750 w=320 h=120" in status and "Màn hình" not in status)
    check("frame: gd_status prints the screen at the root", "- Màn hình: landscape (1920x1080" in gd_status.report(root))
    table, node = gd_frame.find(root, "ai/idea/feature-a/a1/a1x")
    text = gd_frame.text_report(table, node)
    check("frame: gd_frame prints both frames and the warning", ("frame field.frame" in text, "frame main.frame" in text,
                                                                 "! home: nút" in text), (True, True, True))
    out = Path(root) / "f.html"
    check("frame: gd_frame --html writes one page", (gd_frame.main(["gd_frame.py", "ai/idea/feature-a/a1/a1x", "--html", str(out),
                                                                   "--root", root]), "<svg" in out.read_text(encoding="utf-8")), (0, True))
    check("frame: gd_frame refuses a path outside the tables", gd_frame.main(["gd_frame.py", "ai/other/x", "--root", root]), 2)
    d = folder(root, "idea/feature-a/a1/a1x/data.md")
    d.write_text(d.read_text(encoding="utf-8").replace(after, gd.read_data(folder(root, "idea/feature-b/b1/b1x"))["hash"]), encoding="utf-8")
    put(root, "idea/feature-a/code.json", '{"unity": ["Alpha"]}')
    try:
        gd.promote(root, "", True)
        refused = ""
    except ValueError as ex:
        refused = str(ex)
    check("cheat: a ui frame without the feature's cheat node blocks Chốt", "chưa có node cheat" in refused)
    check("cheat: a field-only node does not ask for it",
          any("chưa có node cheat" in p["text"] for p in gd.read_tree(root, "idea")[1] if p["path"] == "feature-b/b1/b1x"), False)
    mk(root, "feature-a/cheat")
    put(root, "idea/feature-a/cheat/.frame-concept/core.frame", "kind: ui\n- panel rect x=560 y=190 w=800 h=700\n")
    gd.promote(root, "", True)
    check("frame: a deep Chốt copies frames", folder(root, "live/feature-a/a1/a1x/.frame-concept/main.frame").is_file())
    put(root, "idea/feature-a/a1/a1x/.frame-concept/main.frame", UI_FRAME.replace("w=320 h=80", "w=320 h=100"))
    check("frame: a changed frame makes the node differ from live", snap_node(root, "feature-a/a1/a1x")["state"], "changed")
    gd.promote(root, "feature-a/a1/a1x", False)
    check("frame: a single Chốt copies frames", "h=100" in folder(root, "live/feature-a/a1/a1x/.frame-concept/main.frame").read_text(encoding="utf-8"))
    pack = gd_pack.pack(root, "Alpha")
    check("frame: gd_pack prints the frames of the branch", "### Frame (bố cục ước lượng" in pack and "home button x=980 y=750 w=320 h=100" in pack)
    check("frame: the snapshot carries the frame CSS", "fr-button" in gd.snapshot(root)["frameCss"])


def run_cheat(root):
    """The cheat node: right under a feature, no children, at most 3 states, one frame per state named after it."""
    mk(root, "")
    mk(root, "feature-a")
    states = (("lose", 8, "Thua."), ("record", 8, "Kỉ lục."))
    mk(root, "feature-a/cheat", items=states)
    put(root, "idea/feature-a/cheat/.frame-concept/lose.frame", "kind: ui\n- a rect x=1 y=1 w=9 h=9\n")

    def errs():
        return [p["text"] for p in gd.read_tree(root, "idea")[1] if p["path"].endswith("cheat")]
    check("cheat: a state without its frame is a problem", any("'record' chưa có frame" in t for t in errs()))
    put(root, "idea/feature-a/cheat/.frame-concept/record.frame", "kind: ui\n- a rect x=1 y=1 w=9 h=9\n")
    check("cheat: one frame per state is fine", errs(), [])
    n = snap_node(root, "feature-a/cheat")
    check("cheat: flagged, frames allowed off round 4, a frame without use reminded",
          (n["cheat"], any("chỉ dùng ở node vòng" in t for t in n["frameNotes"]), any("chưa use" in t for t in n["frameNotes"])),
          (True, False, True))
    put(root, "idea/feature-a/cheat/.frame-concept/extra.frame", "kind: ui\n")
    check("cheat: a frame that is no state is a problem", any("'extra.frame' không trùng" in t for t in errs()))
    folder(root, "idea/feature-a/cheat/.frame-concept/extra.frame").unlink()
    mk(root, "feature-a/cheat", items=states + (("c", 8, "C."), ("d", 8, "D.")))
    check("cheat: more than 3 states is a problem", any("tối đa 3 state" in t for t in errs()))
    mk(root, "feature-a/cheat", items=states)
    front(root, "feature-a/cheat", "frame: none")
    check("cheat: 'frame: none' is a problem", any("frame: none" in t for t in errs()))
    mk(root, "feature-a/cheat", items=states)
    mk(root, "feature-a/cheat/sub")
    check("cheat: a child node is a problem", any("không có node con" in t for t in errs()))
    shutil.rmtree(folder(root, "idea/feature-a/cheat/sub"))
    mk(root, "feature-a/a1")
    mk(root, "feature-a/a1/cheat")
    check("cheat: only right under a feature", any(p["path"] == "feature-a/a1/cheat" and "ngay dưới node feature" in p["text"]
                                                   for p in gd.read_tree(root, "idea")[1]))


def run_styles(root):
    """Styles (.art-style of the root): read, checked, in the root's data hash, copied to live, only at the root."""
    mk(root, "")
    mk(root, "feature-a")
    before = gd.read_data(folder(root, "idea"))["hash"]
    put(root, "idea/.art-style/ui-flat.md", "# UI phẳng\n\nPhẳng, viền dày, màu tươi.\n")
    after = gd.read_data(folder(root, "idea"))["hash"]
    check("style: adding a style changes the root's data hash", before != after)
    check("style: so the children are stale", snap_node(root, "feature-a")["stale"])
    put(root, "idea/.art-style/ui-flat-ref.png", "png1")
    with_pic = gd.read_data(folder(root, "idea"))["hash"]
    check("style: a reference picture is in the hash too", with_pic != after)
    put(root, "idea/.art-style/ui-flat-ref.png", "png2")
    check("style: changing the picture changes it again", gd.read_data(folder(root, "idea"))["hash"] != with_pic)
    styles, errors = gd.read_styles(folder(root, "idea"))
    check("style: read", ([(s["id"], s["title"], s["images"]) for s in styles], errors), ([("ui-flat", "UI phẳng", ["ui-flat-ref.png"])], []))
    put(root, "idea/.art-style/orphan.png", "x")
    put(root, "idea/.art-style/Bad.md", "# X\n\ny\n")
    put(root, "idea/.art-style/empty.md", "# Rỗng\n")
    errors = gd.read_styles(folder(root, "idea"))[1]
    check("style: a picture of no style, a bad id, an empty style", (any("orphan.png" in e for e in errors),
          any("Bad.md" in e for e in errors), any("empty.md" in e for e in errors)), (True, True, True))
    for name in ("orphan.png", "Bad.md", "empty.md"):
        folder(root, f"idea/.art-style/{name}").unlink()
    check("style: the root node carries its styles", [s["id"] for s in snap_node(root)["styles"]], ["ui-flat"])
    put(root, "idea/feature-a/.art-style/x.md", "# X\n\ny\n")
    check("style: only at the root", any(".art-style chỉ đặt ở node gốc" in p["text"] for p in gd.read_tree(root, "idea")[1]))
    shutil.rmtree(folder(root, "idea/feature-a/.art-style"))
    mk(root, "feature-a")      # based-on the new root hash
    gd.promote(root, "", False)
    check("style: Chốt copies the styles", folder(root, "live/.art-style/ui-flat-ref.png").is_file())
    check("style: the root is the same as live after Chốt", snap_node(root)["state"], "same")
    check("style: gd_status prints the styles", "- Style ui-flat: UI phẳng (1 ảnh tham chiếu)" in gd_status.report(root))


def run_diff(root):
    """What the page shows against live: new, rewritten and removed ideas, title, summary, frames, when."""
    mk(root, "", items=(("loop", 8, "Vòng lặp."), ("win", 6, "Thắng sau 10 đợt."), ("map", 7, "Một bản đồ.")))
    mk(root, "feature-shop", items=(("buy", 9, "Mua bằng xu."),))
    gd.record_versions(root), gd.sync_live(root)
    node = snap_node(root)
    check("diff: a node the same as live has no change", (node["diff"]["added"], node["diff"]["rewritten"], node["diff"]["removed"],
                                                         node["diff"]["title"], node["diff"]["summary"], node["diff"]["frames"]), (0, 0, [], None, None, False))
    check("diff: every idea of it is the same", [it["change"] for it in node["items"]], ["same"] * 3)
    mk(root, "", title="Game mới", summary="Tóm tắt khác.",
       items=(("loop", 8, "Vòng lặp."), ("win", 7, "Chơi vô tận."), ("score", 6, "Tính điểm theo đợt.")))
    gd.record_versions(root)
    node = snap_node(root)
    check("diff: each idea against live", [(it["id"], it["change"]) for it in node["items"]],
          [("loop", "same"), ("win", "rewritten"), ("score", "new")])
    check("diff: a rewritten idea carries the live text", next(it for it in node["items"] if it["id"] == "win")["live"],
          {"title": "Thắng sau 10 đợt.", "detail": ""})
    check("diff: counts and the removed idea", (node["diff"]["added"], node["diff"]["rewritten"], [r["id"] for r in node["diff"]["removed"]]),
          (1, 1, ["map"]))
    check("diff: title and summary as they are in live", (node["diff"]["title"], node["diff"]["summary"]), ("Game", "Tóm tắt."))
    check("diff: when an idea was last added or rewritten", [bool(it["changedAt"]) for it in node["items"]], [True, True, True])
    shop = snap_node(root, "feature-shop")
    check("diff: a child that did not change stays the same", (shop["state"], [it["change"] for it in shop["items"]]), ("same", ["same"]))
    mk(root, "feature-new")
    check("diff: a node live has not", [it["change"] for it in snap_node(root, "feature-new")["items"]], ["new"])
    put(root, "idea/feature-shop/.frame-concept/main.frame", "kind: ui\n- a rect x=0 y=0 w=10 h=10\n")
    check("diff: a frame changed against live", snap_node(root, "feature-shop")["diff"]["frames"], True)
    files = {f["name"]: f for f in snap_node(root, "feature-shop")["diff"]["files"]}
    check("diff: the new frame file with its text", (files[".frame-concept/main.frame"]["status"], files[".frame-concept/main.frame"]["live"],
                                                    files[".frame-concept/main.frame"]["idea"].startswith("kind: ui")), ("added", None, True))
    before = gd.own_hash(folder(root, "idea/feature-shop"))
    put(root, "idea/feature-shop/CLAUDE.md", "# feature-shop\n\nDữ liệu cốt lõi: data.md\n\nGiải thích mới.\n")
    put(root, "idea/feature-shop/code.json", '{"unity": ["Shop2"]}')
    img = folder(root, "idea/feature-shop/.images/a.png")
    img.parent.mkdir(parents=True, exist_ok=True)
    img.write_bytes(b"\x89PNG\x00\xff")
    put(root, "idea/feature-shop/.images/index.md", "- a.png: ảnh thử\n")
    mk(root, "feature-shop", items=(("buy", 7, "Mua bằng xu."),), doc="Giải thích mới.")
    shop = snap_node(root, "feature-shop")
    files = {f["name"]: f for f in shop["diff"]["files"]}
    check("diff: CLAUDE.md has the live and the new text", ("Chi tiết." in files["CLAUDE.md"]["live"], "Giải thích mới." in files["CLAUDE.md"]["idea"]), (True, True))
    check("diff: code.json added with its text", (files["code.json"]["status"], files["code.json"]["idea"]), ("added", '{"unity": ["Shop2"]}'))
    check("diff: a picture is binary: no text", (files[".images/a.png"]["status"], files[".images/a.png"]["live"], files[".images/a.png"]["idea"]), ("added", None, None))
    check("diff: data.md differs (the AI score)", files["data.md"]["status"], "changed")
    check("diff: an AI score changed: the idea stays the same, the live score is kept", [(it["change"], it["liveAi"]) for it in shop["items"]], [("same", 9)])
    check("own_hash still hashes the node's files", before != gd.own_hash(folder(root, "idea/feature-shop")), True)
    check("the page carries no raw files", any("_files" in n for t in ("idea", "live") for n in gd.walk(gd.snapshot(root)[t])), False)


def main():
    for case in (run_data, run_diff, run_scores, run_live_scores, run_priority, run_progress, run_infra, run_code_names, run_flow, run_refs, run_glossary, run_pack, run_frames, run_cheat, run_styles):
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
