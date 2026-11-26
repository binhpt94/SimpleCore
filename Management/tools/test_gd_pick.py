#!/usr/bin/env python3
"""Tests for picking GPT's points (người quyết 2026-11-27): gd_pick.py, the pick in gamedesign.py (check_pick,
file_pick, pick_index, mark_sources, gpt_pick, pick_deleted), what gd_status.py and gd_advise.py say about it.

    python Management/tools/test_gd_pick.py
"""
import contextlib
import io
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_advise  # noqa: E402
import gd_pick  # noqa: E402
import gd_status  # noqa: E402
from test_gamedesign import folder, mk, promote, put, snap_node  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


ANSWER = """---
advice: q1
node: feature-hero
table: ai/idea
asked: 2026-11-27 10:00:00
by: gpt (Codex CLI)
filter: pick
---
# Góp ý q1

## Câu hỏi

Hero có đủ chưa?

## Góp ý của GPT

### quai-chan | thêm | feature-enemy ưu tiên hero chắn đường khi cả hero và thành cùng trong tầm
Rất dài dòng.

### giu-core | thêm | Quái có lõi
x

### quai-cham | thêm | Quái chậm lại khi bị đánh
x

### hero-nhanh | thêm | Hero chạy nhanh hơn
x

### hero-nhanh-2 | thêm | Hero di chuyển nhanh
x

### node-moi | thêm | Node mới: hero-dash cho cú lướt
x

### trung | thêm | Thành không tự hồi máu
x
"""

KEEP = """## Giữ
### enemy-blocker | feature-enemy | thêm | Quái ưu tiên hero chắn đường
nguồn: quai-chan
Khi hero và thành cùng trong tầm.

### core | feature-enemy | thêm | Quái có lõi
nguồn: giu-core

### enemy-slow | feature-enemy | thêm | Quái chậm khi bị đánh
nguồn: quai-cham

### hero-fast | feature-hero | thêm | Hero chạy nhanh
nguồn: hero-nhanh, hero-nhanh-2
Gộp hai góp ý cùng hệ quả.

### hero-dash | Node mới: feature-hero/hero-dash | thêm | Hero lướt
nguồn: node-moi
"""
DROP = """
## Bỏ
- trung: trùng `feature-castle#castle-no-regen`
"""


def advice_dir(root, name="feature-hero"):
    return Path(root).joinpath(*gd.BASE, gd.ADVICE_DIR, name)


def run_pick(root, *args):
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = gd_pick.main(["gd_pick.py", *args], root)
    return code, out.getvalue()


def problems_of(root, path):
    return [p["text"] for p in gd.read_tree(root, "idea")[1] if p["path"] == (path or "(gốc)")]


def setup(root):
    mk(root, "")
    mk(root, "feature-enemy")
    mk(root, "feature-enemy/enemy-behavior")
    mk(root, "feature-hero")
    mk(root, "glossary")
    promote(root, "")
    promote(root, "feature-enemy")
    adv = advice_dir(root)
    adv.mkdir(parents=True, exist_ok=True)
    (adv / "q1.q.md").write_text("Hero có đủ chưa?", encoding="utf-8")
    (adv / "q1.md").write_text(ANSWER, encoding="utf-8")


def bad_picks(root):
    adv = advice_dir(root)
    six = KEEP + "\n### extra | feature-hero | thêm | Thêm nữa\nnguồn: trung\n"
    cases = (
        ("more than five kept", six, "tối đa 5"),
        ("a point left out", KEEP, "góp ý 'trung' chưa có"),
        ("a point twice", KEEP + DROP + "- hero-nhanh: trùng\n", "có mặt 2 lần"),
        ("a reason missing", KEEP + "\n## Bỏ\n- trung:\n", "chưa có lí do"),
        ("a point the answer does not have", KEEP + DROP + "- khong-co: trùng\n", "'khong-co' không phải góp ý"),
        ("a kept idea without its sources", KEEP.replace("nguồn: quai-chan\n", "") + DROP, "thiếu dòng 'nguồn:"),
        ("a round-4 target", KEEP.replace("| feature-enemy | thêm | Quái ưu tiên", "| feature-enemy/enemy-behavior/x | thêm | Quái ưu tiên") + DROP,
         "vòng 2 hoặc 3"),
        ("the glossary as a target", KEEP.replace("| feature-enemy | thêm | Quái có lõi", "| glossary | thêm | Quái có lõi") + DROP, "vòng 2 hoặc 3"),
        ("a target that is no node", KEEP.replace("| feature-enemy | thêm | Quái chậm", "| feature-none | thêm | Quái chậm") + DROP,
         "không có node 'feature-none'"),
        ("an idea the target does not have", KEEP.replace("| feature-hero | thêm | Hero chạy", "| feature-hero | làm rõ nope | Hero chạy") + DROP,
         "không có ý 'nope'"),
        ("a new node that already is one", KEEP.replace("Node mới: feature-hero/hero-dash", "Node mới: feature-enemy/enemy-behavior") + DROP, "đã có"),
        ("a new node that is not an addition", KEEP.replace("| Node mới: feature-hero/hero-dash | thêm |", "| Node mới: feature-hero/hero-dash | thay core |") + DROP,
         "chỉ dùng loại"),
        ("a title with |", KEEP.replace("Hero lướt", "Hero | lướt") + DROP, "dấu |"),
        ("words before ## Giữ", "Mở đầu\n" + KEEP + DROP, "chữ nằm trước"),
    )
    for label, text, why in cases:
        (adv / "q1.pick.md").write_text(text, encoding="utf-8")
        code, out = run_pick(root, "feature-hero", "q1")
        check(f"a pick that cannot be used is refused, nothing written: {label}",
              (code, why in out, (adv / "q1.pick.json").exists()), (1, True, False))
    check("bad arguments are refused", run_pick(root, "feature/hero", "q1")[0], 1)


def run_all(root):
    setup(root)
    adv = advice_dir(root)
    # nothing filed yet: gd_status names the answer that waits for its pick, and the node asked owes nothing
    check("an answer to pick without a pick file is listed for @DesignAdvisor",
          "## Chưa lọc góp ý GPT" in gd_status.report(root) and "advice/feature-hero/q1: chưa có file lọc" in gd_status.report(root))
    check("a picked answer's points are not owed by the node asked", snap_node(root, "feature-hero")["gpt"]["missing"], [])
    bad_picks(root)

    (adv / "q1.pick.md").write_text(KEEP + DROP, encoding="utf-8")
    code, out = run_pick(root, "feature-hero", "q1")
    state = gd.read_json(adv / "q1.pick.json")
    routes = {k: v["route"] for k, v in state["items"].items()}
    check("a good pick is filed: each kept idea gets its route (node in live: side; not yet: node; new node: new)",
          (code, routes), (0, {"enemy-blocker": "side", "core": "side", "enemy-slow": "side", "hero-fast": "node", "hero-dash": "new"}))
    check("gd_pick prints each kept idea with its route and tells @GameDesigner the label",
          all(s in out for s in ("giữ 5 ý, bỏ 1 góp ý", "- hero-fast → feature-hero (thêm) · vào node", "node phụ", "node mới",
                                 "by: <id ý>=gpt@feature-hero/q1#<id ý giữ>")))
    check("the pick is no longer listed as waiting", "## Chưa lọc góp ý GPT" in gd_status.report(root), False)

    hero = snap_node(root, "feature-hero")
    check("the target not in live owes its kept idea", [(m["advice"], m["point"], m["pick"]) for m in hero["gpt"]["missing"]],
          [("feature-hero/q1", "hero-fast", True)])
    check("the new node is owed by its parent", [p["target"] for p in hero["gpt"]["newMissing"]], ["feature-hero/hero-dash"])
    check("both are structure problems", (any("ý giữ 'feature-hero/q1#hero-fast'" in p for p in problems_of(root, "feature-hero")),
                                          any("NODE MỚI CHƯA TẠO" in p for p in problems_of(root, "feature-hero"))), (True, True))
    enemy = snap_node(root, "feature-enemy")
    check("the target in live waits beside the node: nothing owed, no problem",
          ([p["id"] for p in enemy["gpt"]["side"]], enemy["gpt"]["missing"], problems_of(root, "feature-enemy")),
          (["enemy-blocker", "core", "enemy-slow"], [], []))
    side = enemy["gpt"]["side"][0]
    check("a waiting idea carries its short text, GPT's own words and the points left out with their reason",
          (side["title"], side["detail"], [r["point"] for r in side["raw"]], side["dropped"]),
          ("Quái ưu tiên hero chắn đường", "Khi hero và thành cùng trong tầm.", ["quai-chan"],
           [{"point": "trung", "reason": "trùng `feature-castle#castle-no-regen`", "title": "Thành không tự hồi máu"}]))
    check("gd_status says what waits for the human, and that @GameDesigner does not write it",
          "Bổ sung từ GPT chờ người lọc: 3 ý" in gd_status.report(root, "ai/idea/feature-enemy"))

    # @GameDesigner writes the kept idea into its target and makes the new node
    hero_data = folder(root, "idea/feature-hero/data.md")
    text = hero_data.read_text(encoding="utf-8")
    hero_data.write_text(text.replace("---\n", "---\nby: fast=gpt@feature-hero/q1#hero-fast\n", 1)
                         + "\n### fast | AI 7 | Hero chạy nhanh\nGộp hai góp ý.\n", encoding="utf-8")
    mk(root, "feature-hero/hero-dash")
    dash = folder(root, "idea/feature-hero/hero-dash/data.md")
    dash.write_text(dash.read_text(encoding="utf-8").replace("---\n", "---\nnode-by: gpt@feature-hero/q1#hero-dash\n", 1), encoding="utf-8")
    hero = snap_node(root, "feature-hero")
    fast = next(it for it in hero["items"] if it["id"] == "fast")
    check("written with the new label: nothing owed, no problem",
          (hero["gpt"]["missing"], hero["gpt"]["newMissing"], problems_of(root, "feature-hero"), problems_of(root, "feature-hero/hero-dash")),
          ([], [], [], []))
    check("the idea is GPT's, with the kept idea's words and GPT's own", (fast["by"], gd.by_label(fast), len(fast["sources"][0]["raw"])),
          ("gpt", " · GPT (góp ý feature-hero/q1#hero-fast)", 2))
    check("gd_status names the node GPT proposed by its kept idea",
          "Node do GPT đề xuất (lần hỏi feature-hero/q1#hero-dash)" in gd_status.report(root, "ai/idea/feature-hero/hero-dash"))

    # wrong labels
    wrong = text.replace("---\n", "---\nby: fast=gpt@feature-hero/q1#nope\n", 1) + "\n### fast | AI 7 | Hero chạy nhanh\nx\n"
    hero_data.write_text(wrong, encoding="utf-8")
    check("a label naming no kept idea is a structure problem", any("ý giữ 'feature-hero/q1#nope' không có" in p for p in problems_of(root, "feature-hero")))
    hero_data.write_text(text.replace("---\n", "---\nby: fast=gpt@q1#hero-nhanh\n", 1) + "\n### fast | AI 7 | Hero chạy nhanh\nx\n", encoding="utf-8")
    check("a label straight to a point of a picked answer is a structure problem",
          any("lần hỏi đó đã lọc" in p for p in problems_of(root, "feature-hero")))
    hero_data.write_text(text.replace("---\n", "---\nby: fast=gpt@feature-hero/q1#hero-fast\n", 1)
                         + "\n### fast | AI 7 | Hero chạy nhanh\nGộp hai góp ý.\n", encoding="utf-8")
    enemy_data = folder(root, "idea/feature-enemy/data.md")
    enemy_text = enemy_data.read_text(encoding="utf-8")
    enemy_data.write_text(enemy_text.replace("---\n", "---\nby: blocker=gpt@feature-hero/q1#enemy-blocker\n", 1)
                          + "\n### blocker | AI 7 | Quái ưu tiên hero chắn đường\nx\n", encoding="utf-8")
    check("@GameDesigner may not move a waiting idea into a fixed node itself",
          any("AI TỰ CHUYỂN Ý TỪ NODE PHỤ" in p for p in problems_of(root, "feature-enemy")))
    enemy_data.write_text(enemy_text, encoding="utf-8")

    # the route is fixed when the pick was filed
    promote(root, "feature-hero")
    code, _ = run_pick(root, "feature-hero", "q1")
    check("filed again after the target reached live: the route stays", (code, gd.read_json(adv / "q1.pick.json")["items"]["hero-fast"]["route"]),
          (0, "node"))

    # the human moves a waiting idea up, and deletes one
    before = gd.read_data(folder(root, "idea/feature-enemy"))["hash"]
    iid = gd.gpt_pick(root, "feature-hero", "q1", "enemy-blocker", "promote")
    enemy = snap_node(root, "feature-enemy")
    moved = next(it for it in enemy["items"] if it["id"] == iid)
    check("promote: the idea is written into the node as GPT's, scored 8 by the human, the hash changes",
          (iid, moved["by"], moved["user"], moved["title"], moved["detail"], gd.read_data(folder(root, "idea/feature-enemy"))["hash"] != before),
          ("enemy-blocker", "gpt", 8, "Quái ưu tiên hero chắn đường", "Khi hero và thành cùng trong tầm.", True))
    check("promote: it no longer waits, and it is no problem", ([p["id"] for p in enemy["gpt"]["side"]], problems_of(root, "feature-enemy")),
          (["core", "enemy-slow"], []))
    check("promote: an id the node has already gets a number", gd.gpt_pick(root, "feature-hero", "q1", "core", "promote"), "core-2")
    gd.gpt_pick(root, "feature-hero", "q1", "enemy-slow", "delete")
    enemy = snap_node(root, "feature-enemy")
    check("delete: it no longer waits, and is turned down", ([p["id"] for p in enemy["gpt"]["side"]],
                                                             [e["key"] for e in gd.pick_deleted(root, "feature-enemy")]),
          ([], ["feature-hero/q1#enemy-slow"]))
    check("gd_status lists it as turned down", "Người đã xoá ở phần bổ sung từ GPT" in gd_status.report(root, "ai/idea/feature-enemy"))
    found = gd_advise.node_folders(root, "feature-enemy")
    context = gd_advise.context_text(root, "feature-enemy", found, "q5", set(), 0)
    check("the next question to GPT tells it the human deleted it", "ĐÃ XOÁ ở phần bổ sung từ GPT" in context and "Quái chậm khi bị đánh" in context)
    for label, args in (("twice", ("feature-hero", "q1", "enemy-slow", "promote")),
                        ("an idea that goes into its node, not beside it", ("feature-hero", "q1", "hero-fast", "promote")),
                        ("no such idea", ("feature-hero", "q1", "nope", "delete")), ("a wrong action", ("feature-hero", "q1", "core", "keep")),
                        ("a bad folder", ("../x", "q1", "core", "delete"))):
        try:
            gd.gpt_pick(root, *args)
            check(f"gpt_pick refuses {label}", False)
        except ValueError:
            check(f"gpt_pick refuses {label}", True)
    pick = adv / "q1.pick.md"
    pick.write_text(pick.read_text(encoding="utf-8").replace("Quái chậm khi bị đánh", "Quái chậm hẳn"), encoding="utf-8")
    code, out = run_pick(root, "feature-hero", "q1")
    check("an idea the human already deleted may not be changed in the pick file", (code, "đã được người xoá" in out), (1, True))
    check("a pick file changed after it was filed is listed again", "file lọc đã sửa sau lần chạy gd_pick.py" in gd_status.report(root))

    # an answer of before the pick keeps the earlier rule
    (adv / "q0.md").write_text(ANSWER.replace("filter: pick\n", "").replace("advice: q1", "advice: q0"), encoding="utf-8")
    check("an answer without filter: pick still wants every point in the node asked",
          len([m for m in snap_node(root, "feature-hero")["gpt"]["missing"] if not m["pick"]]), 7)


def main():
    root = tempfile.mkdtemp(prefix="gdpick-")
    try:
        run_all(root)
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
