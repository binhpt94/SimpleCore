#!/usr/bin/env python3
"""Tests for studio.py on a throw-away project, with fake codex and blender commands.

    python Management/tools/test_studio.py
"""
import io
import json
import os
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import studio  # noqa: E402

results = []
FAKE_CODEX = r'''
import os, re, sys
args = sys.argv[1:]
work = args[args.index("-C") + 1]
mode = args[args.index("-s") + 1]
last = args[args.index("-o") + 1] if "-o" in args else None
prompt = sys.stdin.buffer.read().decode("utf-8")      # studio.py sends UTF-8, whatever the console code page is
open(os.environ["FAKE_PROMPT"], "a", encoding="utf-8").write("=== " + mode + "\n" + prompt + "\n")
how = os.environ.get("FAKE_MODE", "write1")
m = re.search(r"Mục cần làm \(chỉ mục này\): ([a-z0-9-]+):", prompt)
item = m.group(1) if m else "x"
def png(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "wb").write(b"\x89PNG fake")
if "Trả lời đúng câu hỏi" in prompt:
    open(last, "w", encoding="utf-8").write("Câu trả lời.")
elif how == "write1" and mode == "workspace-write":
    png(os.path.join(work, "out", item + ".png"))
elif how == "elsewhere" and mode == "workspace-write":
    png(os.path.join(work, "drafts", "image.png"))
elif how == "extra" and mode == "workspace-write":
    png(os.path.join(work, "a.png")); png(os.path.join(work, "b.png"))
elif how == "ro1" and mode == "read-only":
    png(os.path.join(os.environ["STUDIO_CODEX_IMAGES"], "sess", "exec-1.png"))
elif how == "ro2" and mode == "read-only":
    png(os.path.join(os.environ["STUDIO_CODEX_IMAGES"], "sess", "exec-1.png"))
    png(os.path.join(os.environ["STUDIO_CODEX_IMAGES"], "sess", "exec-2.png"))
sys.exit(1 if how in ("ro1", "ro2", "none") and mode == "workspace-write" else 0)
'''
FAKE_BLENDER = r'''
import os, sys
args = sys.argv[sys.argv.index("--") + 1:]
out, item = args[0], args[1]
how = os.environ.get("FAKE_MODE", "one")
if how in ("one", "two"):
    open(os.path.join(out, item + ".fbx"), "wb").write(b"fbx")
if how == "two":
    open(os.path.join(out, item + ".blend"), "wb").write(b"blend")
if how == "stray":
    open(os.path.join(out, item + ".fbx"), "wb").write(b"fbx")
    open(os.path.join(out, "preview.png"), "wb").write(b"png")
sys.exit(1 if how == "fail" else 0)
'''
ORDER = """---
tool: {tool}
kind: {kind}
count: {count}
deliver: Private
format: png 256x256, nền trong suốt
references: {refs}
---
# Icon xu

## Mô tả
Icon cho ví.

## Danh sách
{items}

## Tiêu chí nhận
Đọc rõ ở 64px.
"""


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def call(root, *args):
    """(exit code, stdout + stderr) of studio.main."""
    buf = io.StringIO()
    with redirect_stdout(buf), redirect_stderr(buf):
        code = studio.main(["studio.py", *args, "--root", str(root)])
    return code, buf.getvalue()


def order(root, tool="codex", feature="Wallet", oid="coin-icons", kind="image", items=("coin", "gem"), count=None, refs=""):
    folder = studio.order_dir(root, tool, feature, oid)
    folder.mkdir(parents=True, exist_ok=True)
    lines = "\n".join(f"- {i}: thứ {i}" for i in items)
    (folder / "order.md").write_text(ORDER.format(tool=tool, kind=kind, count=count or len(items), items=lines, refs=refs),
                                     encoding="utf-8")
    return folder


def log_of(folder):
    return [json.loads(line) for line in (folder / "log.jsonl").read_text(encoding="utf-8").splitlines()]


def run_check(root):
    order(root)
    check("a good order passes check", call(root, "check")[0], 0)
    code, out = call(root, "check")
    import dispatch
    told = [m for m in dispatch.messages(root, "@Codex") if m["key"] == "order:codex/Wallet/coin-icons"]
    check("check: a new valid order is put in its tool chat's inbox once, however often check runs",
          (len(told), told[0]["source"] if told else "", told[0]["check"] if told else "", "đã có tin này" in out),
          (1, "studio", "order-open codex Wallet/coin-icons", True))
    data = studio.parse_order((studio.order_dir(root, "codex", "Wallet", "coin-icons") / "order.md").read_text(encoding="utf-8"))
    check("items are read", data["items"], [("coin", "thứ coin"), ("gem", "thứ gem")])
    bad = order(root, oid="bad-count", count=3)
    check("count must match the list", any("count là 3" in p for p in studio.check_order(root, "codex", "Wallet", "bad-count", bad)[1]))
    bad = order(root, oid="dup", items=("coin", "coin"), count=2)
    check("an item twice", any("trùng" in p for p in studio.check_order(root, "codex", "Wallet", "dup", bad)[1]))
    bad = order(root, oid="wrong-kind", kind="model")
    check("codex does not take a model", any("codex nhận kind" in p for p in studio.check_order(root, "codex", "Wallet", "wrong-kind", bad)[1]))
    bad = order(root, oid="no-feature", feature="Nope")
    check("an order of a feature that does not exist", any("không có feature" in p for p in studio.check_order(root, "codex", "Nope", "no-feature", bad)[1]))
    bad = order(root, oid="bad-ref", refs="Assets/Features/Shop/FeatureAssets/Private/s.png")
    check("a reference to another feature's private file",
          any("chỉ file của chính" in p for p in studio.check_order(root, "codex", "Wallet", "bad-ref", bad)[1]))
    (bad / "order.md").write_text("# chỉ tiêu đề\n", encoding="utf-8")
    probs = studio.check_order(root, "codex", "Wallet", "bad-ref", bad)[1]
    check("missing front matter and sections", any("phần đầu" in p for p in probs) and any("Danh sách" in p for p in probs))
    check("check fails while an order is broken", call(root, "check")[0], 1)
    check("status lists the orders", "codex Wallet/coin-icons: mới" in call(root, "status")[1])
    for name in ("bad-count", "dup", "wrong-kind", "bad-ref"):
        shutil.rmtree(studio.order_dir(root, "codex", "Wallet", name))
    shutil.rmtree(studio.order_dir(root, "codex", "Nope", "no-feature"))


def run_codex(root):
    os.environ["STUDIO_CODEX"] = str(Path(root) / "fake_codex.py")
    os.environ["STUDIO_CODEX_IMAGES"] = str(Path(root) / "gen")
    os.environ["FAKE_PROMPT"] = str(Path(root) / "prompts.txt")
    private = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private"
    private.mkdir(parents=True, exist_ok=True)
    (private / "ref.png").write_bytes(b"ref")
    folder = order(root, refs="Assets/Features/Wallet/FeatureAssets/Private/ref.png")
    (studio.studio_dir(root, "codex") / "lessons.md").parent.mkdir(parents=True, exist_ok=True)
    (studio.studio_dir(root, "codex") / "lessons.md").write_text("- Không để viền trắng quanh icon.\n", encoding="utf-8")
    os.environ["FAKE_MODE"] = "write1"
    code, out = call(root, "codex", "Wallet/coin-icons", "coin")
    check("codex writes the image itself: taken", (code, (folder / "work/out/coin.png").is_file()), (0, True))
    prompt = Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8")
    check("the prompt starts with the lessons", "Không để viền trắng" in prompt.split("Order:")[0])
    check("the prompt asks for exactly one image", "đúng MỘT ảnh" in prompt)
    check("the prompt names only its own item", "coin: thứ coin" in prompt and "gem: thứ gem" not in prompt)
    check("the reference is copied next to the order", (folder / "refs/ref.png").is_file())
    check("the run is logged", log_of(folder)[-1]["mode"], "workspace-write")
    check("the order is now being worked on", studio.read_status(folder)["state"], "doing")
    os.environ["FAKE_MODE"] = "elsewhere"
    code, _ = call(root, "codex", "Wallet/coin-icons", "coin")
    check("an image saved under another name is moved to out/<item>, the old one kept",
          (code, (folder / "work/out/coin.png").is_file(), any((folder / "work/_old").rglob("coin.png"))), (0, True, True))
    os.environ["FAKE_MODE"] = "extra"
    code, out = call(root, "codex", "Wallet/coin-icons", "gem")
    check("two images for one item: none taken", (code, (folder / "work/out/gem.png").exists()), (1, False))
    check("the extra images are set aside, never deleted", len(list((folder / "work/_rejected").rglob("*.png"))), 2)
    check("the log says it made too many", "gen thừa" in log_of(folder)[-1]["result"])
    os.environ["FAKE_MODE"] = "ro1"
    code, out = call(root, "codex", "Wallet/coin-icons", "gem")
    check("writing failed, read-only made one image in Codex's folder: copied", (code, (folder / "work/out/gem.png").is_file()), (0, True))
    check("Codex's own image is left where it is", (Path(root) / "gen/sess/exec-1.png").is_file())
    check("both runs are logged", [e["mode"] for e in log_of(folder)[-2:]], ["workspace-write", "read-only"])
    os.environ["FAKE_MODE"] = "ro2"
    shutil.rmtree(Path(root) / "gen")
    check("read-only with two new images takes none", call(root, "codex", "Wallet/coin-icons", "gem")[0], 1)
    os.environ["FAKE_MODE"] = "none"
    code, out = call(root, "codex", "Wallet/coin-icons", "gem")
    check("nothing either way: points to unity-mcp", (code, "unity-mcp" in out), (1, True))
    check("an item the order does not have", "không có item" in call(root, "codex", "Wallet/coin-icons", "ruby")[1])
    lock = studio.studio_dir(root, "codex") / ".run.lock"
    lock.write_text("1")
    check("one Codex run at a time", "đang có một lần chạy khác" in call(root, "codex", "Wallet/coin-icons", "coin")[1])
    lock.unlink()

    c = order(root, oid="ask", kind="consult", items=("palette",))
    code, _ = call(root, "consult", "Wallet/ask", "palette")
    check("a consult keeps GPT's answer", (code, (c / "work/out/palette.md").read_text(encoding="utf-8")), (0, "Câu trả lời."))
    text = Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8")
    head = text.rfind("=== ", 0, text.rfind("Trả lời đúng"))
    check("consult runs read-only", text[head:head + 13], "=== read-only")
    check("an image order is not a consult", "không chạy bằng lệnh này" in call(root, "consult", "Wallet/coin-icons", "coin")[1])


def run_deliver(root):
    folder = studio.order_dir(root, "codex", "Wallet", "coin-icons")
    for p in (folder / "work/out").glob("gem.*"):
        p.unlink()
    code, out = call(root, "deliver", "codex", "Wallet/coin-icons")
    check("deliver refuses a missing item", (code, "thiếu gem" in out), (1, True))
    (folder / "work/out/gem.png").write_bytes(b"gem")
    (folder / "work/out/extra.png").write_bytes(b"x")
    code, out = call(root, "deliver", "codex", "Wallet/coin-icons")
    check("deliver refuses a file of no item", (code, "không thuộc item nào" in out), (1, True))
    (folder / "work/out/extra.png").unlink()
    (folder / "work/out/gem.webp").write_bytes(b"x")
    check("deliver refuses two files for one item", "nhiều hơn một file cho gem" in call(root, "deliver", "codex", "Wallet/coin-icons")[1])
    (folder / "work/out/gem.webp").unlink()
    code, out = call(root, "deliver", "codex", "Wallet/coin-icons")
    v1 = Path(root) / "Resources/Wallet/codex/coin-icons/v1"
    check("deliver copies exactly the items into Resources/<F>/<tool>/<id>/v1",
          (code, sorted(p.name for p in v1.iterdir())), (0, ["coin.png", "gem.png", "quality.json"]))
    import dispatch
    told = [m for m in dispatch.messages(root, "Wallet") if m["key"] == "delivered:codex/Wallet/coin-icons/v1"]
    check("deliver puts a message in the feature's inbox, with how to judge, once",
          (len(told), "review Wallet/coin-icons" in told[0]["text"] if told else False, "hộp thư của Wallet" in out), (1, True, True))
    check("deliver with no server running still succeeds, and says SERVER TẮT", (code, "SERVER TẮT" in out), (0, True))
    check("a blender chat cannot deliver a codex order", call(root, "deliver", "blender", "Wallet/coin-icons")[0], 1)

    ref = "Wallet/coin-icons"
    gen = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/coin-icons"
    state = lambda: studio.view_state(studio.read_status(folder), ["coin", "gem"], studio.read_reviews(folder))  # noqa: E731
    level = lambda item: (studio.read_reviews(folder).get(item) or {}).get("level")                              # noqa: E731
    copied = lambda: sorted(p.name for p in gen.iterdir() if p.name != "QUALITY.md") if gen.is_dir() else []     # noqa: E731
    check("review: the old commands are gone and point to review",
          (call(root, "take", ref)[0], "review" in call(root, "reject", ref)[1], call(root, "approve", ref)[0]), (1, True, 1))
    check("review: a delivery starts unjudged", (state(), level("coin"), level("gem")), ("review", None, None))
    check("review: the level is 0-3", call(root, "review", ref, "coin", "7")[0], 1)
    check("review: an item the order does not have", call(root, "review", ref, "ruby", "3")[0], 1)
    code, out = call(root, "review", ref, "coin", "3")
    check("review: level 3 copies that item into the feature's assets", (code, copied(), "Còn chưa chấm: gem" in out),
          (0, ["coin.png"], True))
    code, out = call(root, "review", ref, "gem", "0")
    check("review: 'Làm lại ngay' is stored without feedback, but cannot run yet",
          (code, level("gem"), "CHƯA làm lại được" in out, copied(), state()), (0, 0, True, ["coin.png"], "redo"))
    os.environ["FAKE_MODE"] = "write1"
    code, out = call(root, "codex", ref, "gem")
    check("review: no run while the feedback is missing", (code, "chưa có phản hồi" in out), (1, True))
    code, out = call(root, "codex", ref, "coin")
    check("review: an item that was not sent back does not run again", (code, "không phải 'Làm lại ngay'" in out), (1, True))
    code, out = call(root, "review", ref, "gem", "0", "gem bị mờ ở 64px")
    redo_msgs = [m for m in dispatch.messages(root, "@Codex") if m["key"].startswith("redo:codex/" + ref + "/gem/")]
    check("review: with feedback it may be made again, and the tool chat gets a message in its inbox",
          (code, "hộp thư của @Codex" in out, len(redo_msgs), "gem bị mờ ở 64px" in redo_msgs[0]["text"] if redo_msgs else False), (0, True, 1, True))
    check("review: the tool chat sees what to make again", "làm lại: gem" in studio.pending_line(root, "codex"))
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    check("review: the item is made again", call(root, "codex", ref, "gem")[0], 0)
    check("review: the feedback is in the prompt", "gem bị mờ ở 64px" in Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8"))
    check("review: one run for each time it is sent back", call(root, "codex", ref, "gem")[0], 1)
    redo = [e for e in log_of(folder) if e["event"] == "redo"]
    check("review: the round is logged with its feedback", [(e["item"], e["round"], e["text"]) for e in redo], [("gem", 1, "gem bị mờ ở 64px")])
    check("review: nothing is judged while the order is being made again", call(root, "review", ref, "coin", "2")[0], 1)
    (gen / "coin.png.meta").write_text("guid: 1", encoding="utf-8")
    check("deliver again makes v2, v1 untouched", (call(root, "deliver", "codex", ref)[0], (v1 / "gem.png").read_bytes()), (0, b"gem"))
    check("v2 exists", (Path(root) / "Resources/Wallet/codex/coin-icons/v2/gem.png").is_file())
    check("review: the item made again is unjudged, the other keeps its level", (level("coin"), level("gem"), state()), (3, None, "review"))
    code, out = call(root, "review", ref, "gem", "2", "hơi tối")
    note = gen / "QUALITY.md"
    check("review: level 2 copies it and notes the flaw; the .meta of what is there stays",
          (code, copied(), "hơi tối" in note.read_text(encoding="utf-8"), state()), (0, ["coin.png", "coin.png.meta", "gem.png"], True, "done"))
    call(root, "review", ref, "gem", "1", "cần nét hơn")
    debts = lambda: [(d["item"], d["kind"], d["state"], d["reason"]) for d in studio.read_debts(root)      # noqa: E731
                     if d["ref"] == ref and d["kind"] == "người chấm"]
    check("deliver: an item already judged gets no new debt from the tool's own check",
          [d["item"] for d in studio.read_debts(root) if d["ref"] == ref and d["version"] == 2 and d["kind"] == "chưa chuẩn"], ["gem"])
    check("review: level 1 owes the item", debts(), [("gem", "người chấm", "open", "cần nét hơn")])
    call(root, "review", ref, "gem", "1", "cần nét hơn nữa")
    check("review: judging again keeps one debt", debts(), [("gem", "người chấm", "open", "cần nét hơn nữa")])
    call(root, "review", ref, "gem", "3")
    check("review: level 3 closes the debt and drops the note", ([d[2] for d in debts()], note.exists()), (["closed"], False))
    studio.save_review(root, ref, "gem", None, "")
    check("review: back to unjudged (the page's leftmost stop)", (level("gem"), state()), (None, "review"))
    studio.save_review(root, ref, "gem", None, "xem lại sau")
    check("review: a feedback text is kept even before a level is chosen", studio.read_reviews(folder)["gem"]["text"], "xem lại sau")
    call(root, "review", ref, "gem", "3")
    check("a consult is judged but never copied into Assets",
          (call(root, "deliver", "codex", "Wallet/ask")[0], call(root, "review", "Wallet/ask", "palette", "3")[0],
           (gen.parent / "ask").exists()), (0, 0, False))


def run_cancel(root):
    """studio.py cancel: the order is dropped; no tool runs for it, check and the tool's line leave it out."""
    folder = order(root, feature="Shop", oid="drop-me", items=("a", "b"))
    check("cancel: needs a reason", call(root, "cancel", "Shop/drop-me", " ")[0], 1)
    code, out = call(root, "cancel", "Shop/drop-me", "Trùng sprite đã đạt")
    status = studio.read_status(folder)
    check("cancel: the order is cancelled, with why", (code, status["state"], status["reason"], "Đã huỷ order codex Shop/drop-me" in out),
          (0, "cancelled", "Trùng sprite đã đạt", True))
    check("cancel: logged", log_of(folder)[-1].get("event"), "cancel")
    check("cancel: not twice", call(root, "cancel", "Shop/drop-me", "lần hai")[0], 1)
    rows = {r[0]: r for r in studio.summary(root)}
    check("cancel: its state, and none of its items may be made again",
          (rows["Shop/drop-me"][2], [i["redo"] for i in rows["Shop/drop-me"][7]]), ("cancelled", [False, False]))
    check("cancel: the tool chat is no longer told of it", "Shop/drop-me" in studio.pending_line(root, "codex"), False)
    code, out = call(root, "status", "Shop/drop-me")
    check("cancel: status says so", (code, "đã huỷ: Trùng sprite đã đạt" in out), (0, True))
    check("cancel: no tool runs for it, nothing is delivered or judged",
          [call(root, *a)[0] for a in (("codex", "Shop/drop-me", "a"), ("deliver", "codex", "Shop/drop-me"),
                                       ("review", "Shop/drop-me", "a", "3"), ("pick", "Shop/drop-me", "a", "1"))], [1, 1, 1, 1])
    check("cancel: check leaves it out", "Shop/drop-me" in call(root, "check")[1], False)
    snap = next(o for o in studio.snapshot(root)["orders"] if o["ref"] == "Shop/drop-me")
    check("cancel: the Studio tab gets the state and the reason", (snap["state"], snap["stateName"], snap["reason"]),
          ("cancelled", "đã huỷ", "Trùng sprite đã đạt"))
    check("cancel: an unknown order is refused", call(root, "cancel", "Shop/none", "x")[0], 1)


def run_blender(root):
    os.environ["STUDIO_BLENDER"] = str(Path(root) / "fake_blender.py")
    folder = order(root, tool="blender", oid="coin-model", kind="model", items=("coin",))
    code, out = call(root, "blender", "Wallet/coin-model", "coin")
    check("blender needs the script first", (code, "work/coin.py" in out), (1, True))
    (folder / "work").mkdir(exist_ok=True)
    (folder / "work/coin.py").write_text("# script", encoding="utf-8")
    os.environ["FAKE_MODE"] = "one"
    check("headless blender makes the model", (call(root, "blender", "Wallet/coin-model", "coin")[0],
                                               (folder / "work/out/coin.fbx").is_file()), (0, True))
    check("the run is logged as headless", log_of(folder)[-1]["mode"], "headless")
    os.environ["FAKE_MODE"] = "two"
    check("two files for one item: none taken", call(root, "blender", "Wallet/coin-model", "coin")[0], 1)
    os.environ["FAKE_MODE"] = "stray"
    code, out = call(root, "blender", "Wallet/coin-model", "coin")
    check("a file of no item is set aside", (code, (folder / "work/out/preview.png").exists()), (0, False))
    os.environ["FAKE_MODE"] = "fail"
    code, out = call(root, "blender", "Wallet/coin-model", "coin")
    check("blender fails: points to blender MCP, then unity-mcp", (code, "blender MCP" in out and "unity-mcp" in out), (1, True))
    check("blender order runs only through blender", call(root, "codex", "Wallet/coin-model", "coin")[0], 1)
    check("summary for the blender chat", "Wallet/coin-model" in studio.pending_line(root, "blender"))


FAKE_SVG2PNG = r'''
import sys
open(sys.argv[2], "wb").write(b"\x89PNG frame")
'''
ART_ORDER = """---
tool: codex
kind: art
count: {count}
deliver: concept
format: png 1920x1080
style: {style}
---
# Art popup thua

## Mô tả
Popup thua trên nền trận.

## Danh sách
{items}

## Tiêu chí nhận
Giữ bố cục frame.
"""
NODE = "feature-pop/cheat"


def design_tree(root, frame="kind: ui\n- panel round-rect x=560 y=190 w=800 h=700 label=\"Khung popup\"\n"
                            "- title label x=600 y=200 w=400 h=80 overlap label=\"Thua rồi (tiêu đề)\"\n", style="Phẳng, màu tươi, viền dày."):
    """A live table: the root with one style and a picture, a feature with its cheat node: one state, one frame."""
    live = Path(root) / "Management/game-design/live"
    for i, part in enumerate(["", "feature-pop", NODE]):
        d = live / part if part else live
        d.mkdir(parents=True, exist_ok=True)
        head = "---\nscreen: landscape\n---\n" if not part else ""
        item = "main" if part == NODE else "a"
        (d / "data.md").write_text(f"{head}# N{i}\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### {item} | AI 8 | Ý\n", encoding="utf-8")
        (d / "CLAUDE.md").write_text(f"# N{i}\n\nDữ liệu cốt lõi: data.md\n", encoding="utf-8")
    (live / ".art-style").mkdir(exist_ok=True)
    (live / ".art-style/ui-flat.md").write_text(f"# UI phẳng\n\n{style}\n", encoding="utf-8")
    (live / ".art-style/ui-flat-ref.png").write_bytes(b"style ref")
    (live / NODE / ".frame-concept").mkdir(exist_ok=True)
    (live / NODE / ".frame-concept/main.frame").write_text(frame, encoding="utf-8")


def art_order(root, oid="lose-art", items=("main",), style="ui-flat"):
    feature = studio.gd.design_owner(NODE)
    folder = studio.order_dir(root, "codex", feature, oid)
    folder.mkdir(parents=True, exist_ok=True)
    (folder / "order.md").write_text(ART_ORDER.format(count=len(items), style=style,
                                                      items="\n".join(f"- {i}: cảnh {i}" for i in items)), encoding="utf-8")
    return f"{feature}/{oid}", folder


def art_of(root, frame="main"):
    tree, _ = studio.gd.read_tree(root, "live")
    art, pending = studio.gd.read_art(root, tree)
    return next((a for a in art.get(NODE, []) if a["frame"] == frame), None), pending


def run_art(root):
    os.environ["STUDIO_CODEX"] = str(Path(root) / "fake_codex.py")
    os.environ["STUDIO_SVG2PNG"] = str(Path(root) / "fake_svg2png.py")
    (Path(root) / "fake_svg2png.py").write_text(FAKE_SVG2PNG, encoding="utf-8")
    os.environ["FAKE_MODE"] = "write1"
    ref, folder = art_order(root)
    problems = studio.check_order(root, "codex", studio.gd.design_owner(NODE), "lose-art", folder)[1]
    check("art: a node not in live is refused", any("chưa có trong live" in p for p in problems))
    design_tree(root)
    check("art: a good order passes check", studio.check_order(root, "codex", studio.gd.design_owner(NODE), "lose-art", folder)[1], [])
    _, bad = art_order(root, oid="bad-frame", items=("other",))
    check("art: an item must be a state", any("id một state" in p for p in
                                              studio.check_order(root, "codex", studio.gd.design_owner(NODE), "bad-frame", bad)[1]))
    _, bad = art_order(root, oid="bad-style", style="paint")
    check("art: a style must exist in round 1", any("style 'paint'" in p for p in
                                                    studio.check_order(root, "codex", studio.gd.design_owner(NODE), "bad-style", bad)[1]))
    shutil.rmtree(bad.parent / "bad-frame"), shutil.rmtree(bad)
    other = studio.gd.design_owner("feature-pop/pop-lose/lose-spec")
    odir = studio.order_dir(root, "codex", other, "not-cheat")
    odir.mkdir(parents=True, exist_ok=True)
    (odir / "order.md").write_text((folder / "order.md").read_text(encoding="utf-8"), encoding="utf-8")
    check("art: only for a cheat node", any("chỉ đặt cho node cheat" in p for p in
                                            studio.check_order(root, "codex", other, "not-cheat", odir)[1]))
    shutil.rmtree(odir.parent)
    plain = order(root, oid="art-in-feature", kind="art", items=("x",))
    check("art: only for design nodes", any("chỉ dành cho order của node thiết kế" in p for p in
                                            studio.check_order(root, "codex", "Wallet", "art-in-feature", plain)[1]))
    shutil.rmtree(plain)
    code, out = call(root, "status", ref)
    check("art: status lists it as new", f"{ref}: mới" in out)
    check("art: the page lists it as not delivered", [(o["ref"], o["state"]) for o in art_of(root)[1]], [(ref, "new")])
    check("art: the tool chat sees it at once (no approval step)", "lose-art" in studio.pending_line(root, "codex"))
    check("art: @GameDesigner is reminded to tell @Codex", "chưa giao" in studio.art_line(root))
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    code, out = call(root, "codex", ref, "main")
    check("art: it runs without any approval", (code, (folder / "work/out/main.png").is_file(), (folder / "approval.json").exists()),
          (0, True, False))
    prompt = Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8")
    check("art: the prompt carries the style and the frame rule", ("Phẳng, màu tươi" in prompt, "Giữ đúng bố cục" in prompt), (True, True))
    given = (folder / "work/_frames/main.frame.svg").read_text(encoding="utf-8")
    check("art: the prompt keeps the texts the player reads and names them, notes left out",
          ("Giữ nguyên các chữ hiển thị trong frame" in prompt, 'Chữ phải hiện trong ảnh, đúng từng chữ và dấu, ở đúng chỗ của frame: "Thua rồi"' in prompt,
           "tiêu đề" in prompt, "Khung popup" in prompt, "Không thêm chữ chú thích" in prompt), (True, True, False, False, False))
    check("art: the frame Codex sees shows those texts in Roboto, no numbers and no notes",
          (">Thua rồi</text>" in given, "font-family: 'Roboto'" in given, "800x700</text>" in given, "Khung popup</text>" in given),
          (True, True, False, False))
    attempt = [e for e in log_of(folder) if e["event"] == "attempt"][-1]
    check("art: Codex sees the frame first, then the style picture", attempt["images"][:2], ["main.frame.png", "ui-flat-ref.png"])
    call(root, "codex", ref, "main")
    code, out = call(root, "codex", ref, "main")
    check("art: at most 2 runs per item before the first delivery", (code, "hết 2 lượt đầu" in out), (1, True))
    studio.RUNS_FIRST = 99      # the rest checks the other gates
    old_daily = studio.ART_DAILY_MAX
    studio.ART_DAILY_MAX = 2      # two runs already today
    code, out = call(root, "codex", ref, "main")
    check("art: the daily cap", (code, "trần chung" in out), (1, True))
    studio.ART_DAILY_MAX = old_daily
    code, out = call(root, "deliver", "codex", ref)
    vdir = Path(root) / "Resources/_design" / NODE.replace("/", ".") / "codex/lose-art/v1"
    meta = json.loads((vdir / "meta.json").read_text(encoding="utf-8"))
    check("art: delivered with meta", (code, (vdir / "main.png").is_file(), sorted(meta["styles"]), "main" in meta["frames"]),
          (0, True, ["ui-flat"], True))
    check("art: the page labels it not judged", (art_of(root)[0]["label"], art_of(root)[0]["level"], art_of(root)[0]["reviewable"]),
          ("Chưa duyệt", None, True))
    check("art: no self-check, so the page marks it below standard", art_of(root)[0]["below"].startswith("chưa tự kiểm"))
    check("art: @GameDesigner sees it waits for the human", "chờ người chấm" in studio.art_line(root))
    check("art: anchor needs a picture that passed", call(root, "anchor", ref, "main")[0], 1)
    studio.save_review(root, ref, "main", 2, "nút hơi nhỏ")      # what the page does when the slider moves
    check("art: the page shows the level and the feedback", (art_of(root)[0]["label"], art_of(root)[0]["feedback"]),
          ("Đúng phần lớn chi tiết", "nút hơi nhỏ"))
    check("art: level 2 is no anchor", call(root, "anchor", ref, "main")[0], 1)
    studio.save_review(root, ref, "main", 3, "")
    check("art: passed", (art_of(root)[0]["label"], art_of(root)[0]["usable"]), ("Đạt", True))
    check("art: anchor", (call(root, "anchor", ref, "main")[0], art_of(root)[0]["anchor"]), (0, True))
    ref2, folder2 = art_order(root, oid="again")
    check("art: no second order for a frame whose picture is still good",
          any("đã có ảnh còn dùng được" in p for p in studio.check_order(root, "codex", studio.gd.design_owner(NODE), "again", folder2)[1]))
    design_tree(root, style="Vẽ tay, màu trầm.")
    check("art: the style changed", art_of(root)[0]["label"], "CŨ · style đã đổi")
    design_tree(root, frame="kind: ui\n- panel round-rect x=400 y=190 w=800 h=700\n", style="Vẽ tay, màu trầm.")
    check("art: the node changed", art_of(root)[0]["label"], "CŨ · node đã đổi")
    check("art: a stale picture lets a new order in", studio.check_order(root, "codex", studio.gd.design_owner(NODE), "again", folder2)[1], [])
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    call(root, "codex", ref2, "main")
    attempt = [e for e in log_of(folder2) if e["event"] == "attempt"][-1]
    check("art: the anchor of the style is handed to Codex", "main.png" in attempt["images"][2:])
    call(root, "deliver", "codex", ref2)
    studio.save_review(root, ref2, "main", 0, "")
    check("art: 'Làm lại ngay' without feedback: the page stores it, nothing may run, @GameDesigner is told",
          (art_of(root)[0]["label"], art_of(root)[0]["usable"], "THIẾU PHẢN HỒI: main" in studio.art_line(root),
           "again" in studio.pending_line(root, "codex"), call(root, "codex", ref2, "main")[0]),
          ("Làm lại ngay", False, True, False, 1))
    studio.save_review(root, ref2, "main", 0, "Màu quá tối.")
    check("art: with feedback it may be made again", ("làm lại được: main" in studio.art_line(root),
                                                       "again" in studio.pending_line(root, "codex")), (True, True))
    ref1_anchor = json.loads((studio.studio_dir(root, "codex") / "anchors.json").read_text(encoding="utf-8"))
    check("art: the first picture is still an anchor", [x["ref"] for x in ref1_anchor], [ref])
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    check("art: one run, with the human's feedback in the prompt",
          (call(root, "codex", ref2, "main")[0], "Màu quá tối." in Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8"),
           call(root, "codex", ref2, "main")[0]), (0, True, 1))
    check("art: the page lists an order being made again", [(o["ref"], o["state"]) for o in art_of(root)[1]], [(ref2, "doing")])
    studio.RUNS_FIRST = 2
    studio.save_review(root, ref, "main", 1, "Không đúng style mới.")
    check("art: a picture that no longer passes is no anchor any more",
          json.loads((studio.studio_dir(root, "codex") / "anchors.json").read_text(encoding="utf-8")), [])
    check("art: level 1 owes the picture to its node",
          [(d["owner"], d["item"], d["kind"]) for d in studio.read_debts(root) if d["state"] == "open" and d["ref"] == ref],
          [(studio.gd.design_owner(NODE), "main", "người chấm")])


def run_budget(root):
    """Runs are limited: 2 per item, 1 more each time it is judged "Làm lại ngay", at most 2 such rounds; the closest
    result is delivered as 'chưa chuẩn' with an art debt, which a later passing delivery or the human closes."""
    os.environ["STUDIO_CODEX"] = str(Path(root) / "fake_codex.py")
    os.environ["FAKE_MODE"] = "write1"
    folder = order(root, oid="limits", items=("star",))
    ref = "Wallet/limits"
    check("budget: first run", call(root, "codex", ref, "star")[0], 0)
    check("budget: second run", call(root, "codex", ref, "star")[0], 0)
    code, out = call(root, "codex", ref, "star")
    check("budget: the third run is refused, pointing to pick", (code, "hết 2 lượt đầu" in out, "pick" in out), (1, True, True))
    check("budget: every run is kept", sorted(p.name for p in (folder / "work/attempts").iterdir()), ["star-1.png", "star-2.png"])
    check("budget: attempts are logged with their number", [e["n"] for e in log_of(folder) if e["event"] == "attempt"], [1, 2])
    (folder / "work/attempts/star-1.png").write_bytes(b"first")
    code, out = call(root, "pick", ref, "star", "1")
    check("budget: pick puts run 1 back, no run spent",
          (code, (folder / "work/out/star.png").read_bytes(), studio.budget(folder, "star")["used"]), (0, b"first", 2))
    check("budget: pick of a run that does not exist", call(root, "pick", ref, "star", "7")[0], 1)
    (folder / "work/star.check.md").write_text("chưa đạt: viền còn mỏng ở 64px\nLượt 1 gần hơn lượt 2.", encoding="utf-8")
    code, out = call(root, "deliver", "codex", ref)
    quality = json.loads((Path(root) / "Resources/Wallet/codex/limits/v1/quality.json").read_text(encoding="utf-8"))
    check("budget: delivered below standard, said so", (code, "CHƯA CHUẨN: star: viền còn mỏng" in out, quality["star"]["ok"],
                                                        quality["star"]["runs"]), (0, True, False, 2))
    debts = [d for d in studio.read_debts(root) if d["ref"] == ref]
    check("budget: an art debt is opened", [(d["owner"], d["item"], d["kind"], d["state"]) for d in debts],
          [("Wallet", "star", "chưa chuẩn", "open")])
    code, out = call(root, "review", ref, "star", "1", "viền mỏng, dùng tạm")
    note = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/limits/QUALITY.md"
    text = note.read_text(encoding="utf-8") if note.is_file() else ""
    check("budget: what is below standard is still used, with QUALITY.md",
          (code, "viền mỏng, dùng tạm" in text, "tool tự kiểm chưa đạt" in text, (note.parent / "quality.json").exists()),
          (0, True, True, False))
    open_ = lambda: sorted((d["kind"], d["reason"]) for d in studio.read_debts(root) if d["state"] == "open" and d["ref"] == ref)  # noqa: E731
    check("budget: the judgement owes it too", [k for k, _ in open_()], ["chưa chuẩn", "người chấm"])
    reads, real_tree = [], studio.gd.read_tree
    studio.gd.read_tree = lambda *a, **k: reads.append(a[1:]) or real_tree(*a, **k)
    try:
        snap = studio.snapshot(root)
    finally:
        studio.gd.read_tree = real_tree
    check("snapshot reads the live tree once, however many orders there are", reads, [("live",)])
    o = next(x for x in snap["orders"] if x["ref"] == ref)
    check("budget: snapshot", (o["state"], o["items"][0]["used"], o["items"][0]["allowed"], o["items"][0]["ok"], o["items"][0]["level"],
                               o["items"][0]["levelName"], len([d for d in snap["debts"] if d["ref"] == ref])),
          ("done", 2, 2, False, 1, "Lỗi nhưng chưa cần làm ngay", 2))
    for n in (1, 2):
        call(root, "review", ref, "star", "0", f"Lần {n}: chưa đạt.")
        check(f"budget: sent back {n}, one more run", (studio.budget(folder, "star")["left"], call(root, "codex", ref, "star")[0],
                                                      call(root, "codex", ref, "star")[0]), (1, 0, 1))
        (folder / "work/star.check.md").write_text("chưa đạt: vẫn mỏng", encoding="utf-8")
        call(root, "deliver", "codex", ref)
    check("budget: sending it back closed the judgement's own debt", [k for k, _ in open_()], ["chưa chuẩn", "chưa chuẩn", "chưa chuẩn"])
    code, out = call(root, "review", ref, "star", "0", "Lần 3: vẫn mỏng.")
    check("budget: a third time is stored but runs nothing", (code, "CHƯA làm lại được" in out, "2/2" in out,
                                                             call(root, "codex", ref, "star")[0]), (0, True, True, 1))
    check("budget: status says why", "CHƯA LÀM LẠI ĐƯỢC" in call(root, "status", ref)[1])
    call(root, "review", ref, "star", "1", "Lần 3: vẫn mỏng, ghi nợ.")
    check("budget: so it is owed instead", [r for k, r in open_() if k == "người chấm"], ["Lần 3: vẫn mỏng, ghi nợ."])
    folder2 = order(root, oid="limits-2", items=("star",))
    call(root, "codex", "Wallet/limits-2", "star")
    (folder2 / "work/star.check.md").write_text("đạt", encoding="utf-8")
    code, out = call(root, "deliver", "codex", "Wallet/limits-2")
    left = [d for d in studio.read_debts(root) if d["state"] == "open" and d["owner"] == "Wallet" and d["item"] == "star"]
    closed = [d for d in studio.read_debts(root) if d["state"] == "closed" and d["ref"] == ref and "tự đóng" in d["closedBy"]]
    check("budget: a later passing delivery closes every debt of the item", (code, left, len(closed)), (0, [], 4))
    folder3 = order(root, oid="limits-3", items=("moon",))
    call(root, "codex", "Wallet/limits-3", "moon")
    call(root, "deliver", "codex", "Wallet/limits-3")
    debt = next(d for d in studio.read_debts(root) if d["item"] == "moon")
    check("budget: no self-check is below standard", debt["reason"].startswith("chưa tự kiểm"))
    check("budget: the human closes a debt", (call(root, "debt", "close", debt["id"])[0],
                                              next(d for d in studio.read_debts(root) if d["id"] == debt["id"])["state"]), (0, "closed"))
    check("budget: closing twice is refused", call(root, "debt", "close", debt["id"])[0], 1)
    os.environ["STUDIO_BLENDER"] = str(Path(root) / "fake_blender.py")
    os.environ["FAKE_MODE"] = "one"
    bf = order(root, tool="blender", oid="limits-model", kind="model", items=("rock",))
    (bf / "work").mkdir(exist_ok=True)
    (bf / "work/rock.py").write_text("# s", encoding="utf-8")
    runs = [call(root, "blender", "Wallet/limits-model", "rock")[0] for _ in range(3)]
    check("budget: blender has the same limit", (runs, sorted(p.name for p in (bf / "work/attempts").iterdir())),
          ([0, 0, 1], ["rock-1.fbx", "rock-2.fbx"]))


def real_png(path, width, height, pixel, colour=6, depth=8, filters=(0, 1, 2, 3, 4)):
    """Writes a real PNG; pixel(x, y) gives the samples of one pixel. Rows use the filters in turn."""
    import struct
    import zlib
    step = {2: 3, 6: 4, 4: 2}[colour] * (depth // 8)
    rows = [bytearray(b"".join(v.to_bytes(depth // 8, "big") for x in range(width) for v in pixel(x, y)))
            for y in range(height)]
    raw, prev = bytearray(), bytearray(width * step)
    for y, row in enumerate(rows):
        kind = filters[y % len(filters)]
        out = bytearray(len(row))
        for i, v in enumerate(row):
            a = row[i - step] if i >= step else 0
            b, c = prev[i], (prev[i - step] if i >= step else 0)
            p = a + b - c
            paeth = a if abs(p - a) <= abs(p - b) and abs(p - a) <= abs(p - c) else b if abs(p - b) <= abs(p - c) else c
            out[i] = (v - (0, a, b, (a + b) >> 1, paeth)[kind]) & 255
        raw += bytes([kind]) + out
        prev = row

    def chunk(name, body):
        return struct.pack(">I", len(body)) + name + body + struct.pack(">I", zlib.crc32(name + body))
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, depth, colour, 0, 0, 0))
                           + chunk(b"IDAT", zlib.compress(bytes(raw))) + chunk(b"IEND", b""))


def run_measure(root):
    folder = order(root, oid="measured", items=("coin", "gem", "flat", "junk", "none"))
    out = folder / "work/out"
    # a 32x16 picture: only x 8..23, y 2..13 shows (16x12), in colours that change so every filter has work to do
    shape = lambda x, y: (x * 7 % 256, y * 13 % 256, (x + y) * 5 % 256, 200 if 8 <= x <= 23 and 2 <= y <= 13 else 0)
    real_png(out / "coin.png", 32, 16, shape)
    m = studio.measure_png(out / "coin.png")
    check("measure: size, kind and depth of a PNG", (m["width"], m["height"], m["kind"], m["depth"]), (32, 16, "RGBA", 8))
    check("measure: the box of what shows, whatever the row filter", m["alpha"]["box"], (8, 2, 23, 13))
    check("measure: share of clear pixels and clear corners",
          (round(m["alpha"]["clear"], 4), m["alpha"]["corners"]), (round(1 - 16 * 12 / (32 * 16), 4), True))
    real_png(out / "gem.png", 256, 256, lambda x, y: (9, 9, 9, 0 if (x, y) == (0, 0) else 65535), depth=16)
    m = studio.measure_png(out / "gem.png")
    check("measure: 16 bit, one clear pixel, three corners not clear",
          (m["depth"], m["alpha"]["corners"], m["alpha"]["box"], round(m["alpha"]["clear"] * 256 * 256)), (16, False, (0, 0, 255, 255), 1))
    real_png(out / "flat.png", 8, 8, lambda x, y: (1, 2, 3), colour=2)
    check("measure: no alpha channel", studio.measure_png(out / "flat.png")["alpha"], None)
    (out / "junk.png").write_bytes(b"\x89PNG fake")
    code, text = call(root, "measure", "Wallet/measured")
    check("measure: the whole order, one line set per item", code, 0)
    check("measure: size against the order's format",
          ("coin.png (work/out): 32x16 px, PNG RGBA 8 bit" in text, "kích thước LỆCH (256x256)" in text,
           "- gem.png (work/out): 256x256 px, PNG RGBA 16 bit" in text, "kích thước khớp (256x256)" in text), (True,) * 4)
    check("measure: what shows, as a share of the frame and its offset",
          "hình (điểm có alpha trên 0): 16x12 px = 50% x 75% khung; tâm lệch ngang +0 px, dọc +0 px; "
          "cách mép trái 8, trên 2, phải 8, dưới 2 px" in text)
    check("measure: a background that is not transparent is said so",
          ("nền: không có kênh alpha, nền KHÔNG trong suốt" in text, "4 góc KHÔNG trong suốt" in text,
           text.count("nền trong suốt: KHÔNG ĐẠT hoặc không đo được"), text.count("nền trong suốt: 4 góc trong suốt")),
          (True, True, 2, 1))
    check("measure: a broken file and a missing result are reported, the rest still measured",
          ("- junk.png (work/out): không đo được: không phải file PNG" in text, "- none: chưa có kết quả" in text), (True, True))
    code, text = call(root, "measure", "Wallet/measured", "coin")
    check("measure: one item only", (code, "coin.png" in text, "gem.png" in text), (0, True, False))
    check("measure: an item the order does not have", call(root, "measure", "Wallet/measured", "ghost")[0], 1)
    check("measure: an order that does not exist", call(root, "measure", "Wallet/ghost")[0], 1)
    before = sorted(p.name for p in folder.iterdir())
    check("measure writes nothing", (sorted(p.name for p in folder.iterdir()), (folder / "log.jsonl").exists()), (before, False))
    for name in ("junk.png", "flat.png"):
        (out / name).unlink()
    for item in ("coin", "gem"):
        (folder / "work" / f"{item}.check.md").write_text("đạt\n", encoding="utf-8")
    (out / "flat.png").write_bytes((out / "coin.png").read_bytes())
    (out / "junk.png").write_bytes((out / "coin.png").read_bytes())
    (out / "none.png").write_bytes((out / "coin.png").read_bytes())
    check("measure: delivered", call(root, "deliver", "codex", "Wallet/measured")[0], 0)
    shutil.rmtree(out)
    code, text = call(root, "measure", "Wallet/measured", "gem")
    check("measure: nothing waits in work/out: the last delivery", (code, "- gem.png (bản giao v1): 256x256 px" in text), (0, True))


FAKE_SVG2PNG_SIZED = r'''
import struct, sys, zlib
w, h = int(sys.argv[3]), int(sys.argv[4])
def chunk(name, body):
    return struct.pack(">I", len(body)) + name + body + struct.pack(">I", zlib.crc32(name + body))
row = b"\0" + b"\0\0\0\0" * (w // 4) + b"\x10\x20\x30\xff" * (w - 2 * (w // 4)) + b"\0\0\0\0" * (w // 4)
open(sys.argv[2], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(row * h)) + chunk(b"IEND", b""))
'''
SVG = ('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 100 100">'
       '<defs><linearGradient id="g"><stop offset="0" stop-color="#fc0"/></linearGradient></defs>{body}</svg>')
GOOD_SVG = SVG.format(body='<circle cx="50" cy="50" r="40" fill="url(#g)"/><use xlink:href="#g"/>'
                           '<image href="data:image/png;base64,AAAA" width="1" height="1"/>')
BAD_SVGS = {
    "a script": SVG.format(body="<script>1</script>"),
    "an event attribute": SVG.format(body='<rect width="1" height="1" onclick="x()"/>'),
    "a file outside": SVG.format(body='<image href="https://example.com/a.png" width="1" height="1"/>'),
    "a local file": SVG.format(body='<use xlink:href="other.svg#a"/>'),
    "a url() outside": SVG.format(body='<rect width="1" height="1" fill="url(http://example.com/x)"/>'),
    "an import in style": SVG.format(body="<style>@import 'a.css';</style>"),
    "foreignObject": SVG.format(body="<foreignObject><p>x</p></foreignObject>"),
    "no viewBox": '<svg xmlns="http://www.w3.org/2000/svg"><rect width="1" height="1"/></svg>',
    "not an SVG": '<html xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1"/>',
    "no namespace": '<svg viewBox="0 0 1 1"/>',
    "broken XML": "<svg",
    "a DOCTYPE": '<!DOCTYPE svg [<!ENTITY a "b">]>' + SVG.format(body=""),
}


def run_claude(root):
    C = studio.CLAUDE
    studio.RUNS_FIRST = 2
    os.environ["STUDIO_SVG2PNG"] = str(Path(root) / "fake_svg2png_sized.py")
    os.environ["STUDIO_BROWSER"] = ""
    (Path(root) / "fake_svg2png_sized.py").write_text(FAKE_SVG2PNG_SIZED, encoding="utf-8")
    check("claude: a drawing that needs nothing outside its file passes", studio.check_svg(GOOD_SVG), [])
    for label, text in BAD_SVGS.items():
        check(f"claude: an SVG with {label} is refused", bool(studio.check_svg(text)))
    folder = order(root, tool=C, oid="claude-icons", items=("coin", "gem"))
    ref = "Wallet/claude-icons"
    attempts = lambda: [e["item"] for e in log_of(folder) if e["event"] == "attempt"] if (folder / "log.jsonl").is_file() else []  # noqa: E731
    check("claude: the order is well formed and waits for @ClaudeImage",
          (studio.check_order(root, C, "Wallet", "claude-icons", folder)[1], "Wallet/claude-icons" in studio.pending_line(root, C),
           "claude-icons" in studio.pending_line(root, "codex")), ([], True, False))
    code, out = call(root, "claude", ref, "coin")
    check("claude: no SVG written yet: refused, no run counted", (code, "chưa có work/coin.svg" in out, attempts()), (1, True, []))
    (folder / "work").mkdir(exist_ok=True)
    (folder / "work/coin.svg").write_text(BAD_SVGS["a script"], encoding="utf-8")
    code, out = call(root, "claude", ref, "coin")
    check("claude: a refused SVG counts no run", (code, "không dùng thẻ <script>" in out, attempts()), (1, True, []))
    (folder / "work/coin.svg").write_text(GOOD_SVG, encoding="utf-8")
    real = studio.svg_converters
    studio.svg_converters = lambda: []
    code, out = call(root, "claude", ref, "coin")
    studio.svg_converters = real
    check("claude: a machine that cannot draw SVG counts no run", (code, "không đổi được SVG sang PNG" in out, attempts()), (1, True, []))
    code, out = call(root, "claude", ref, "coin")
    m = studio.measure_png(folder / "work/out/coin.png")
    check("claude: the SVG becomes a PNG of the order's size, one run", (code, m["width"], m["height"], attempts()), (0, 256, 256, ["coin"]))
    check("claude: the run prints what it measured",
          ("256x256 px, PNG RGBA 8 bit" in out, "kích thước khớp (256x256)" in out, "nền trong suốt: 4 góc trong suốt" in out), (True,) * 3)
    check("claude: the result and the SVG of the run are kept",
          ((folder / "work/attempts/coin-1.png").is_file(), len(list((folder / "work/_log").glob("coin-1-*.svg")))), (True, 1))
    call(root, "claude", ref, "coin")
    code, out = call(root, "claude", ref, "coin")
    check("claude: the same run limit as the other tools", (code, "hết 2 lượt đầu" in out, attempts()), (1, True, ["coin", "coin"]))
    check("claude: the previous draft is set aside, one file in out",
          (sorted(p.name for p in (folder / "work/out").iterdir()), any((folder / "work/_old").rglob("coin.png"))), (["coin.png"], True))
    check("claude: Codex does not run an order of @ClaudeImage", call(root, "codex", ref, "coin")[0], 1)
    check("claude: it does not run an order of Codex", call(root, "claude", "Wallet/coin-icons", "coin")[0], 1)
    bad = order(root, tool=C, oid="claude-ask", kind="consult", items=("q",))
    check("claude: takes image and art only", any("nhận kind" in p for p in studio.check_order(root, C, "Wallet", "claude-ask", bad)[1]))
    shutil.rmtree(bad)
    (folder / "work/gem.svg").write_text(GOOD_SVG, encoding="utf-8")
    call(root, "claude", ref, "gem")
    for item in ("coin", "gem"):
        (folder / "work" / f"{item}.check.md").write_text("đạt\n", encoding="utf-8")
    code, out = call(root, "deliver", C, ref)
    v1 = Path(root) / "Resources/Wallet" / C / "claude-icons/v1"
    check("claude: delivered like any order", (code, sorted(p.name for p in v1.iterdir())), (0, ["coin.png", "gem.png", "quality.json"]))
    code, out = call(root, "review", ref, "coin", "3")
    check("claude: the feature judges it and gets the file",
          (code, (Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/claude-icons/coin.png").is_file()), (0, True))
    # claude concepts: the parts of a node's frame, and a cheat state put together from them
    design_tree(root)
    live = Path(root) / "Management/game-design/live"
    SPEC = "feature-pop/pop-lose/lose-spec"
    for i, part in enumerate(["feature-pop/pop-lose", SPEC]):
        d = live / part
        d.mkdir(parents=True, exist_ok=True)
        (d / "data.md").write_text(f"# S{i}\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### a | AI 8 | Ý\n", encoding="utf-8")
        (d / "CLAUDE.md").write_text(f"# S{i}\n\nDữ liệu cốt lõi: data.md\n", encoding="utf-8")
    (live / SPEC / ".frame-concept").mkdir(exist_ok=True)
    (live / SPEC / ".frame-concept/main.frame").write_text(
        "kind: ui\nuse px = feature-pop/cheat#px\nlet bx = 60\nlet bw = 320\nlet bh = 120\nlet ic = 80\n"
        "- backdrop rect x=0 y=0 w=1920 h=1080\n"
        "- panel round-rect x=px y=190 w=800 h=700 overlap\n"
        "- title label x=600 y=200 w=bw h=80 label=\"Thua\"\n"
        "- replay button x=bx y=400 w=bw h=bh parent=panel label=\"Chơi lại (chính)\"\n"
        "- home button x=420 y=400 w=bw h=bh parent=panel\n"
        "- big button x=bx y=540 w=680 h=bh parent=panel\n"
        "- slots grid x=bx y=100 cols=3 rows=1 cell=ic gap=20 parent=panel\n"
        "- reach ring cx=bx cy=100 r=50\n", encoding="utf-8")
    (live / NODE / ".frame-concept/main.frame").write_text(
        "kind: ui\nlet px = 560\n"
        f"use bx = {SPEC}#bx\nuse bw = {SPEC}#bw\nuse bh = {SPEC}#bh\nuse ic = {SPEC}#ic\n"
        "- backdrop rect x=0 y=0 w=1920 h=1080\n"
        "- replay button x=bx y=400 w=bw h=bh\n"
        "- again button x=bx y=600 w=bw h=bh\n"
        "- wide button x=bx y=800 w=680 h=bh\n"
        "- pic icon x=900 y=100 w=ic h=ic\n"
        "- free rect x=1200 y=100 w=100 h=100\n", encoding="utf-8")
    tree, problems = studio.gd.read_tree(root, "live")
    nodes = {n["path"]: n for n in studio.gd.walk(tree)}
    spec_fr = nodes[SPEC]["frames"][0]
    check("parts: the test frames have no syntax error", [p["text"] for p in problems if ".frame" in p["text"]], [])
    check("parts: what the node itself talks about, what it borrows, what is left out",
          {r["id"]: r["role"] for r in spec_fr["shapes"]},
          {"backdrop": "backdrop", "panel": "other", "title": "own", "replay": "own", "home": "own", "big": "own", "slots": "own", "reach": "own"})
    check("parts: a cheat frame borrows everything it has numbers for",
          {r["id"]: (r["role"], r["sources"]) for r in nodes[NODE]["frames"][0]["shapes"] if r["id"] in ("replay", "free", "backdrop")},
          {"replay": ("other", [SPEC]), "free": ("own", []), "backdrop": ("backdrop", [])})
    check("parts: a grid is one place per cell", [s[2:] for s in studio.gd.shape_slots(spec_fr, next(r for r in spec_fr["shapes"] if r["id"] == "slots"))],
          [(80, 80)] * 3)
    owner, spec_owner = studio.gd.design_owner(NODE), studio.gd.design_owner(SPEC)

    def concept(owner_, oid, items=("main",)):
        f = studio.order_dir(root, C, owner_, oid)
        f.mkdir(parents=True, exist_ok=True)
        (f / "order.md").write_text(ART_ORDER.format(count=len(items), style="ui-flat", items="\n".join(f"- {i}: cảnh {i}" for i in items))
                                    .replace("tool: codex", f"tool: {C}"), encoding="utf-8")
        (f / "work").mkdir(exist_ok=True)
        return f"{owner_}/{oid}", f

    sref, sfolder = concept(spec_owner, "spec-claude")
    check("claude concept: any node with a frame may have one", studio.check_order(root, C, spec_owner, "spec-claude", sfolder)[1], [])
    cx = studio.order_dir(root, "codex", spec_owner, "spec-codex")
    cx.mkdir(parents=True, exist_ok=True)
    (cx / "order.md").write_text(ART_ORDER.format(count=1, style="ui-flat", items="- main: cảnh main"), encoding="utf-8")
    check("claude concept: an art concept of Codex is still for a cheat node only",
          any("chỉ đặt cho node cheat" in p for p in studio.check_order(root, "codex", spec_owner, "spec-codex", cx)[1]))
    shutil.rmtree(cx.parent)
    cref, cfolder = concept(owner, "pop-claude")
    check("claude concept: @GameDesigner is reminded to tell @ClaudeImage", "đã nhắn @ClaudeImage chưa" in studio.art_line(root))
    code, out = call(root, "claude", cref, "main")
    check("cheat: nothing is drawn and nothing is made while a node it borrows from has no claude concept",
          (code, (cfolder / "work/out/main.png").exists(), "hình replay (320x120px) cần phần của hình 'replay'" in out,
           f"order {sref} chưa giao" in out), (1, False, True, True))
    check("cheat: what it waits for is kept and shown to @GameDesigner",
          (sorted(m["shape"] for m in json.loads(studio.needs_file(cfolder, "main").read_text(encoding="utf-8"))),
           f"cheat đang chờ claude-concept của: {SPEC}" in studio.art_line(root)), (["again", "replay", "wide"], True))
    check("cheat: it cannot be delivered while it waits", call(root, "deliver", C, cref)[0], 1)
    runs = lambda f: [e["item"] for e in log_of(f) if e["event"] == "attempt"] if (f / "log.jsonl").is_file() else []  # noqa: E731

    def table(parts):
        (sfolder / "work/main.parts.json").write_text(json.dumps({"parts": parts}), encoding="utf-8")

    def refused(label, parts, text):
        table(parts)
        code, out = call(root, "claude", sref, "main")
        check(f"parts: {label}: refused, no run counted", (code, text in out, runs(sfolder)), (1, True, []))

    code, out = call(root, "claude", sref, "main")
    check("parts: no table yet: refused, no run counted", (code, "thiếu work/main.parts.json" in out, runs(sfolder)), (1, True, []))
    (sfolder / "work/main").mkdir()
    for name in ("btn", "btn-flat", "slot"):
        (sfolder / "work/main" / f"{name}.svg").write_text(GOOD_SVG, encoding="utf-8")
    BTN = {"id": "btn", "w": 320, "h": 120, "slice": [20, 20, 20, 20], "shapes": ["replay", "home", "big"]}
    SLOT = {"id": "slot", "w": 80, "h": 80, "slice": None, "shapes": ["slots"]}
    refused("a shape of the node without a part", [BTN], "các hình của node chưa có phần: slots (80x80px)")
    refused("a shape the node only borrows", [BTN, SLOT, {"id": "p", "w": 8, "h": 7, "slice": None, "shapes": ["panel"]}], "hình 'panel' là phụ")
    refused("the backdrop", [BTN, SLOT, {"id": "p", "w": 16, "h": 9, "slice": None, "shapes": ["backdrop"]}], "hình 'backdrop' là nền phụ")
    refused("a measuring mark", [BTN, SLOT, {"id": "p", "w": 9, "h": 9, "slice": None, "shapes": ["reach"]}], "là nét đo")
    refused("a shape in two parts", [BTN, SLOT, {"id": "slot2", "w": 80, "h": 80, "slice": None, "shapes": ["slots"]}], "nằm trong hai phần")
    refused("a part with no SVG", [BTN, dict(SLOT, id="nosvg")], "chưa có work/main/nosvg.svg")
    refused("one flat picture for places of another shape",
            [{"id": "btn-flat", "w": 320, "h": 120, "slice": None, "shapes": ["replay", "home", "big"]}, SLOT], "tỉ lệ 320x120 lệch")
    refused("a 9-slice border wider than the picture", [dict(BTN, slice=[200, 20, 200, 20]), SLOT], "slice là null hoặc")
    refused("a same-as that points nowhere", [BTN, {"id": "slot", "same-as": "feature-pop/cheat#nope", "shapes": ["slots"]}], "same-as")
    (sfolder / "work/main/btn.svg").write_text(BAD_SVGS["a script"], encoding="utf-8")
    refused("a part whose SVG is refused", [BTN, SLOT], "phần 'btn': không dùng thẻ <script>")
    (sfolder / "work/main/btn.svg").write_text(GOOD_SVG, encoding="utf-8")
    table([BTN, SLOT])
    old_daily, studio.ART_DAILY_MAX = studio.ART_DAILY_MAX, 0
    code, out = call(root, "claude", sref, "main")
    studio.ART_DAILY_MAX = old_daily
    m = studio.measure_png(sfolder / "work/out/main.png")
    check("parts: a good table makes every part and the whole frame, one run, no daily limit",
          (code, m["width"], m["height"], runs(sfolder), sorted(p.name for p in (sfolder / "work/parts/main").iterdir())),
          (0, 1920, 1080, ["main"], ["btn.png", "btn.svg", "parts.json", "slot.png", "slot.svg"]))
    pm = [studio.measure_png(sfolder / "work/parts/main" / n) for n in ("btn.png", "slot.png")]
    check("parts: each part is a PNG of its own size", [(x["width"], x["height"]) for x in pm], [(320, 120), (80, 80)])
    check("parts: the run says how many pictures, which are 9-sliced, which are shared",
          ("2 phần (vẽ mới 2, lấy lại 0)" in out, "- btn: 320x120px, 9-slice viền [20, 20, 20, 20], dùng ở 3 chỗ (replay, home, big)" in out,
           "- slot: 80x80px, không 9-slice, dùng ở 3 chỗ (slots)" in out, "giữ hình cơ bản của frame: panel, title, reach" in out), (True,) * 4)
    whole = sorted((sfolder / "work/_log").glob("main-*.compose.svg"))[-1].read_text(encoding="utf-8")
    place = lambda x, y, w, h: f'<svg x="{x}" y="{y}" width="{w}" height="{h}" viewBox='      # noqa: E731
    check("parts: the frame is put together at the places the frame gives",
          (whole.count('<image id="part-'), place(980, 590, 20, 20) in whole,          # home: top left corner of the 9-slice
           place(1000, 590, 280, 20) in whole,                                          # home: its top edge stretches
           place(640, 750, 640, 80) in whole,                                           # big: the middle of a wider button
           place(820, 290, 80, 80) in whole,                                            # the third cell of the grid
           'data-id="panel"' in whole, 'data-id="title"' in whole, 'data-id="backdrop"' in whole, 'data-id="replay"' in whole),
          (2, True, True, True, True, True, True, False, False))
    check("text: the frame put together shows what the player reads, in Roboto, over a drawn button too",
          ('data-over="replay"' in whole and ">Chơi lại</text>" in whole, ">Thua</text>" in whole, "font-family: 'Roboto'" in whole,
           "(chính)" in whole.split("</style>")[1].replace("<title>", "\n").split("\n")[0], "320x120</text>" in whole, 'data-over="home"' in whole),
          (True, True, True, False, False, False))
    drawn = {"id": "btn", "w": 320, "h": 120, "slice": None, "png": sfolder / "work/parts/main/btn.png", "text": False}
    check("text: a part that has its own text drawn in gets none from the tool",
          'data-over="replay"' in studio.compose_frame(spec_fr, {"replay": drawn})[0], False)
    check("text: the tool's check lists the texts it printed",
          "Chữ tool in (font Roboto): Thua; Chơi lại." in (sfolder / "work/main.check.md").read_text(encoding="utf-8"))
    (sfolder / "work/main/titled.svg").write_text(GOOD_SVG.replace("</svg>", '<text font-family="Roboto" x="5" y="50">A</text></svg>'), encoding="utf-8")
    fonted = studio.with_font(sfolder / "work/main/titled.svg", Path(root) / "titled.font.svg")
    check("text: an SVG that writes in Roboto gets the font put inside before it is drawn; another is left as it is",
          ("font-family: 'Roboto'" in fonted.read_text(encoding="utf-8"), studio.with_font(sfolder / "work/main/btn.svg", Path(root) / "x.svg")),
          (True, sfolder / "work/main/btn.svg"))
    (sfolder / "work/main/titled.svg").unlink()
    code, out = call(root, "deliver", C, sref)
    vdir = Path(root) / "Resources" / spec_owner / C / "spec-claude/v1"
    check("parts: delivered with the picture of the frame",
          (code, sorted(p.name for p in vdir.iterdir()), sorted(p.name for p in (vdir / "parts/main").iterdir())),
          (0, ["main.png", "meta.json", "parts", "quality.json"], ["btn.png", "btn.svg", "parts.json", "slot.png", "slot.svg"]))
    tree, _ = studio.gd.read_tree(root, "live")
    art, _ = studio.gd.read_art(root, tree)
    mine = next(a for a in art[SPEC] if a["tool"] == C)
    check("parts: the page gets the table of a delivered frame",
          ([(p["id"], p["w"], p["h"], p["slice"], p["shapes"]) for p in mine["parts"]], mine["parts"][0]["file"].endswith("v1/parts/main/btn.png"),
           mine["label"], mine["usable"]),
          ([("btn", 320, 120, [20, 20, 20, 20], ["replay", "home", "big"]), ("slot", 80, 80, None, ["slots"])], True, "Đã giao", True))
    import gd_pack
    check("parts: the design pack lists each part for the feature chat",
          gd_pack.part_lines(mine)[0].startswith("phần btn: 320x120px, 9-slice viền trái/trên/phải/dưới [20, 20, 20, 20], dùng cho 3 hình (replay, home, big); Resources/"))
    total = gd_pack.parts_total([dict(nodes[SPEC], art=art[SPEC]), dict(nodes[NODE], art=art[SPEC])])
    check("parts: the pack sums up the pictures a feature needs, the cheat node left out",
          (total[2], len(total)), ("- 2 hình riêng; 9-slice được: 1; một hình dùng nhiều chỗ: 1", 5))
    code, out = call(root, "claude", cref, "main")
    m = studio.measure_png(cfolder / "work/out/main.png")
    check("cheat: once the node has delivered, the state is put together, no run counted",
          (code, m["width"], m["height"], runs(cfolder), studio.needs_file(cfolder, "main").exists()), (0, 1920, 1080, [], False))
    check("cheat: each shape takes the part of the shape it stands for (same id, else same kind and size)",
          (f"dùng ở 3 chỗ (replay, again, wide), lấy lại từ {SPEC}#btn ({sref} v1)" in out,
           "pic: các node nguồn" in out, "free: không `use` số của node nào" in out), (True, True, True))
    code, out = call(root, "deliver", C, cref)
    cmeta = json.loads((Path(root) / "Resources" / owner / C / "pop-claude/v1/meta.json").read_text(encoding="utf-8"))
    check("cheat: delivered, and it remembers where its parts came from", (code, cmeta["sources"], "GameDesigner" in out), (0, [[sref, 1]], True))
    def label(node_):
        fresh, _ = studio.gd.read_tree(root, "live")
        return next(a["label"] for a in studio.gd.read_art(root, fresh)[0][node_] if a["tool"] == C and a["state"] == "mới nhất")

    def refuses(*args):
        try:
            studio.save_review(root, *args)
        except ValueError:
            return True
        return False

    check("claude concept: nobody judges it, a delivery is the end",
          (label(NODE), label(SPEC), refuses(cref, "main", 3, ""), [r[2] for r in studio.summary(root, C) if r[0] in (cref, sref)],
           "chờ người chấm" in studio.art_line(root)), ("Đã giao", "Đã giao", True, ["done", "done"], False))
    check("check: the tool writes the self check itself (sizes, and which order it is)",
          ((sfolder / "work/main.check.md").read_text(encoding="utf-8").split("\n")[:2],
           "- phần btn: 320x120px, cần 320x120px" in (sfolder / "work/main.check.md").read_text(encoding="utf-8")),
          (["đạt", f"Tự kiểm bằng script (studio.py claude), không xem nội dung ảnh. Order {sref}, item main."], True))
    check("claude concept: only Codex pictures are style anchors", call(root, "anchor", cref, "main")[0], 1)
    again = concept(spec_owner, "spec-claude-2")[1]
    check("claude concept: no second order while its own picture is still good",
          any("đã có ảnh còn dùng được" in p for p in studio.check_order(root, C, spec_owner, "spec-claude-2", again)[1]))
    shutil.rmtree(again)
    # a result of the wrong size does not pass the tool's check
    bad = order(root, tool=C, oid="claude-wrong", items=("coin",))
    (bad / "work").mkdir(exist_ok=True)
    (bad / "work/coin.svg").write_text(GOOD_SVG, encoding="utf-8")
    (Path(root) / "fake_svg2png_small.py").write_text(FAKE_SVG2PNG_SIZED.replace("int(sys.argv[3]), int(sys.argv[4])", "12, 12"), encoding="utf-8")
    os.environ["STUDIO_SVG2PNG"] = str(Path(root) / "fake_svg2png_small.py")
    code, out = call(root, "claude", "Wallet/claude-wrong", "coin")
    os.environ["STUDIO_SVG2PNG"] = str(Path(root) / "fake_svg2png_sized.py")
    check("check: a PNG of another size is written down as not passing",
          (code, (bad / "work/coin.check.md").read_text(encoding="utf-8").split("\n")[0], "tự kiểm: chưa đạt" in out),
          (0, "chưa đạt: sai kích thước hoặc không đọc được: coin.png", True))
    # at most CLAUDE_RUNS runs at a time, and one run for an item
    slots = [studio.studio_dir(root, C) / f".run-{n}.lock" for n in range(1, studio.CLAUDE_RUNS + 1)]
    for s in slots[:-1]:
        s.write_text("1", encoding="utf-8")
    code, out = call(root, "claude", cref, "main")
    check("runs: the last free place is taken and given back", (code, slots[-1].exists()), (0, False))
    slots[-1].write_text("1", encoding="utf-8")
    code, out = call(root, "claude", cref, "main")
    check("runs: no more than five at the same time", (code, f"đang có {studio.CLAUDE_RUNS} lần chạy" in out, (cfolder / ".main.lock").exists()), (1, True, False))
    for s in slots:
        s.unlink()
    (cfolder / ".main.lock").write_text("1", encoding="utf-8")
    code, out = call(root, "claude", cref, "main")
    check("runs: two runs never work on the same item", (code, "đang được một lần chạy khác làm" in out), (1, True))
    (cfolder / ".main.lock").unlink()
    check("runs: nothing is left locked", sorted(p.name for p in studio.studio_dir(root, C).glob(".run-*")), [])
    call(root, "deliver", C, cref)      # the state put together again above
    # another node takes a part over instead of drawing it again
    OTHER = "feature-pop/pop-lose/other-spec"
    d = live / OTHER
    (d / ".frame-concept").mkdir(parents=True)
    (d / "data.md").write_text("# O\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### a | AI 8 | Ý\n", encoding="utf-8")
    (d / "CLAUDE.md").write_text("# O\n\nDữ liệu cốt lõi: data.md\n", encoding="utf-8")
    (d / ".frame-concept/main.frame").write_text("kind: ui\nlet w = 500\n- ok button x=100 y=100 w=w h=150\n", encoding="utf-8")
    oref, ofolder = concept(studio.gd.design_owner(OTHER), "other-claude")
    (ofolder / "work/main.parts.json").write_text(json.dumps({"parts": [{"id": "ok", "same-as": f"{SPEC}#btn", "shapes": ["ok"]}]}), encoding="utf-8")
    code, out = call(root, "claude", oref, "main")
    check("parts: a part taken over from another node is not drawn again",
          (code, "1 phần (vẽ mới 0, lấy lại 1)" in out, (ofolder / "work/parts/main/ok.png").is_file()), (0, True, True))
    # the node is worked on again
    spec_frame = live / SPEC / ".frame-concept/main.frame"
    spec_text = spec_frame.read_text(encoding="utf-8")
    (live / SPEC / "data.md").write_text("# S1\n\nTóm tắt đã viết lại.\n\n## Ý cốt lõi\n\n### a | AI 8 | Ý khác\n", encoding="utf-8")
    check("redo: only the words of the node changed: the claude concept stands", label(SPEC), "Đã giao")
    spec_frame.write_text(spec_text.replace("replay button x=bx y=400", "replay button x=bx y=380"), encoding="utf-8")
    check("redo: the layout moved and every part still fits: it only waits to be put together again",
          (label(SPEC), label(NODE)), ("Đã giao · bố cục đổi, chờ ghép lại", "Đã giao"))
    code, out = call(root, "claude", sref, "main")
    check("redo: putting the delivered parts together again draws nothing: no run, no order from anybody",
          (code, runs(sfolder), "không tính lượt" in out), (0, ["main"], True))
    call(root, "deliver", C, sref)
    check("redo: delivered again; what was put together from its parts waits to be put together too",
          (label(SPEC), label(NODE)), ("Đã giao", "Đã giao · phần nguồn có bản mới, chờ ghép lại"))
    call(root, "claude", cref, "main")
    call(root, "deliver", C, cref)
    check("redo: the cheat state is put together from the new delivery", label(NODE), "Đã giao")
    spec_frame.write_text(spec_frame.read_text(encoding="utf-8") + "- extra button x=bx y=10 w=bw h=60 parent=panel\n", encoding="utf-8")
    check("redo: a new shape has no part: only then it has to be made again", label(SPEC), "CẦN LÀM LẠI · cần vẽ: extra")
    (sfolder / "work/main/extra.svg").write_text(GOOD_SVG, encoding="utf-8")
    table([BTN, SLOT, {"id": "extra", "w": 320, "h": 60, "slice": None, "shapes": ["extra"]}])
    code, out = call(root, "claude", sref, "main")
    check("redo: drawing after a delivery waits for a Leader", (code, "cần lệnh của Leader" in out, "Phần mới hoặc đã đổi: extra" in out, runs(sfolder)),
          (1, True, True, ["main"]))
    check("redo: needs a reason, a delivered order, and a frame that is drawn",
          (call(root, "redo", sref, "main", " ")[0], call(root, "redo", oref, "main", "x")[0], call(root, "redo", cref, "main", "x")[0]), (1, 1, 1))
    code, out = call(root, "redo", sref, "main", "thêm nút extra")
    check("redo: a Leader opens one run and is told to tell @ClaudeImage",
          (code, "hộp thư của @ClaudeImage" in out, "phản hồi: thêm nút extra · LÀM LẠI ĐƯỢC (1 lượt)" in call(root, "status", sref)[1]), (0, True, True))
    code, out = call(root, "claude", sref, "main")
    check("redo: that one run draws the new part", (code, runs(sfolder), "3 phần (vẽ mới 3, lấy lại 0)" in out), (0, ["main", "main"], True))
    (sfolder / "work/main/extra.svg").write_text(GOOD_SVG.replace("#fc0", "#0cf"), encoding="utf-8")
    check("redo: and no more than that one", call(root, "claude", sref, "main")[0], 1)
    (sfolder / "work/main/extra.svg").write_text(GOOD_SVG, encoding="utf-8")
    call(root, "deliver", C, sref)
    check("redo: delivered, the claude concept stands again", label(SPEC), "Đã giao")
    del os.environ["STUDIO_BROWSER"]


def main():
    root = Path(tempfile.mkdtemp(prefix="studiofx-"))
    try:
        (root / "UnityProject/Assets/Features/Wallet").mkdir(parents=True)
        (root / "UnityProject/Assets/Features/Shop/FeatureAssets/Private").mkdir(parents=True)
        (root / "fake_codex.py").write_text(FAKE_CODEX, encoding="utf-8")
        (root / "fake_blender.py").write_text(FAKE_BLENDER, encoding="utf-8")
        studio.RUNS_FIRST = 99      # the older cases run an item many times; run_budget checks the real limits
        for case in (run_check, run_codex, run_deliver, run_blender, run_cancel):
            case(root)
        studio.RUNS_FIRST = 2
        for case in (run_art, run_budget, run_measure, run_claude):
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
