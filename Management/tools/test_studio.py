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
    check("status lists the orders", "codex Wallet/coin-icons: new" in call(root, "status")[1])
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
    check("deliver tells the tool chat what to send", "SendMessage" in out and "take Wallet/coin-icons" in out)
    check("a blender chat cannot deliver a codex order", call(root, "deliver", "blender", "Wallet/coin-icons")[0], 1)

    code, out = call(root, "take", "Wallet/coin-icons")
    gen = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/coin-icons"
    check("take copies the delivery into the feature's assets", (code, sorted(p.name for p in gen.iterdir() if p.name != "QUALITY.md")), (0, ["coin.png", "gem.png"]))
    check("take marks the order taken", studio.read_status(folder)["state"], "taken")

    check("reject needs a new feedback file", "feedback-<n>.md" in call(root, "reject", "Wallet/coin-icons")[1])
    (folder / "feedback-1.md").write_text("gem bị mờ ở 64px", encoding="utf-8")
    check("reject with feedback", call(root, "reject", "Wallet/coin-icons")[0], 0)
    check("a rejected order is not taken again", call(root, "take", "Wallet/coin-icons")[0], 1)
    os.environ["FAKE_MODE"] = "write1"
    code, out = call(root, "codex", "Wallet/coin-icons", "gem")
    check("no new run until lessons.md names the feedback", (code, "codex/orders/Wallet/coin-icons/feedback-1.md" in out), (1, True))
    lessons = studio.studio_dir(root, "codex") / "lessons.md"
    lessons.write_text(lessons.read_text(encoding="utf-8") + "- Icon phải nét ở 64px (codex/orders/Wallet/coin-icons/feedback-1.md).\n",
                       encoding="utf-8")
    check("after the lesson the item is made again", call(root, "codex", "Wallet/coin-icons", "gem")[0], 0)
    check("status shows no unlearnt feedback", "CHƯA RÚT BÀI HỌC" in call(root, "status", "Wallet/coin-icons")[1], False)
    (gen / "coin.png.meta").write_text("guid: 1", encoding="utf-8")
    (gen / "old.png").write_bytes(b"x")
    (gen / "old.png.meta").write_text("guid: 2", encoding="utf-8")
    check("deliver again makes v2, v1 untouched", (call(root, "deliver", "codex", "Wallet/coin-icons")[0],
                                                  (v1 / "gem.png").read_bytes()), (0, b"gem"))
    check("v2 exists", (Path(root) / "Resources/Wallet/codex/coin-icons/v2/gem.png").is_file())
    call(root, "take", "Wallet/coin-icons")
    check("take again keeps the .meta of a file still delivered and drops what is gone",
          sorted(p.name for p in gen.iterdir() if p.name != "QUALITY.md"), ["coin.png", "coin.png.meta", "gem.png"])
    check("a consult is read in Resources, not copied into Assets", (call(root, "deliver", "codex", "Wallet/ask")[0],
                                                                    "không chép vào Assets" in call(root, "take", "Wallet/ask")[1]), (0, True))


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


def design_tree(root, frame="kind: ui\n- panel round-rect x=560 y=190 w=800 h=700\n", style="Phẳng, màu tươi, viền dày."):
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
    check("art: status says it waits for the human", "CHỜ NGƯỜI DUYỆT GEN" in out)
    check("art: the page lists it as waiting", [o["ref"] for o in art_of(root)[1] if not o["approved"]], [ref])
    code, out = call(root, "codex", ref, "main")
    check("art: no approval, no run", (code, "chưa được người duyệt" in out, (folder / "log.jsonl").is_file()), (1, True, False))
    check("art: approve", call(root, "approve", ref)[0], 0)
    approval = json.loads((folder / "approval.json").read_text(encoding="utf-8"))
    check("art: the approval holds the order and the node", (approval["items"], approval["order"] == studio.order_hash(folder)), (["main"], True))
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    code, out = call(root, "codex", ref, "main")
    check("art: approved, it runs", (code, (folder / "work/out/main.png").is_file()), (0, True))
    prompt = Path(os.environ["FAKE_PROMPT"]).read_text(encoding="utf-8")
    check("art: the prompt carries the style and the frame rule", ("Phẳng, màu tươi" in prompt, "Giữ đúng bố cục" in prompt), (True, True))
    attempt = [e for e in log_of(folder) if e["event"] == "attempt"][-1]
    check("art: Codex sees the frame first, then the style picture", attempt["images"][:2], ["main.frame.png", "ui-flat-ref.png"])
    call(root, "codex", ref, "main")
    code, out = call(root, "codex", ref, "main")
    check("art: at most 2 runs per item in the first round", (code, "2/2" in out), (1, True))
    studio.RUNS_FIRST = 99      # the rest checks the other gates
    call(root, "approve", ref)
    old_daily = studio.ART_DAILY_MAX
    studio.ART_DAILY_MAX = 2      # two runs already today
    code, out = call(root, "codex", ref, "main")
    check("art: the daily cap", (code, "trần chung" in out), (1, True))
    studio.ART_DAILY_MAX = old_daily
    (folder / "order.md").write_text((folder / "order.md").read_text(encoding="utf-8") + "\nThêm.\n", encoding="utf-8")
    code, out = call(root, "codex", ref, "main")
    check("art: the order changed after approval", (code, "order.md đã đổi" in out), (1, True))
    call(root, "approve", ref)
    frame_file = Path(root) / "Management/game-design/live" / NODE / ".frame-concept/main.frame"
    frame_file.write_text("kind: ui\n- panel round-rect x=500 y=190 w=800 h=700\n", encoding="utf-8")
    code, out = call(root, "codex", ref, "main")
    check("art: the node changed after approval", (code, "node trong live đã đổi" in out), (1, True))
    design_tree(root)
    code, out = call(root, "take", ref)
    check("art: take is for features, not art", code, 1)
    code, out = call(root, "deliver", "codex", ref)
    vdir = Path(root) / "Resources/_design" / NODE.replace("/", ".") / "codex/lose-art/v1"
    meta = json.loads((vdir / "meta.json").read_text(encoding="utf-8"))
    check("art: delivered with meta", (code, (vdir / "main.png").is_file(), sorted(meta["styles"]), "main" in meta["frames"]),
          (0, True, ["ui-flat"], True))
    check("art: the page labels it waiting", art_of(root)[0]["label"], "chờ nhận")
    check("art: no self-check, so the page marks it below standard", art_of(root)[0]["below"].startswith("chưa tự kiểm"))
    check("art: anchor needs an accepted picture", call(root, "anchor", ref, "main")[0], 1)
    check("art: accept", call(root, "accept", ref)[0], 0)
    check("art: in use", art_of(root)[0]["label"], "đang dùng")
    check("art: anchor", (call(root, "anchor", ref, "main")[0], art_of(root)[0]["anchor"]), (0, True))
    ref2, folder2 = art_order(root, oid="again")
    check("art: no second order for a frame whose picture is still good",
          any("đã có ảnh còn dùng được" in p for p in studio.check_order(root, "codex", studio.gd.design_owner(NODE), "again", folder2)[1]))
    design_tree(root, style="Vẽ tay, màu trầm.")
    check("art: the style changed", art_of(root)[0]["label"], "CŨ · style đã đổi")
    design_tree(root, frame="kind: ui\n- panel round-rect x=400 y=190 w=800 h=700\n", style="Vẽ tay, màu trầm.")
    check("art: the node changed", art_of(root)[0]["label"], "CŨ · node đã đổi")
    check("art: a stale picture lets a new order in", studio.check_order(root, "codex", studio.gd.design_owner(NODE), "again", folder2)[1], [])
    call(root, "approve", ref2)
    Path(os.environ["FAKE_PROMPT"]).write_text("", encoding="utf-8")
    call(root, "codex", ref2, "main")
    attempt = [e for e in log_of(folder2) if e["event"] == "attempt"][-1]
    check("art: the anchor of the style is handed to Codex", "main.png" in attempt["images"][2:])
    call(root, "deliver", "codex", ref2)
    (folder2 / "feedback-1.md").write_text("Màu quá tối.", encoding="utf-8")
    code, out = call(root, "reject", ref2)
    ref1_anchor = json.loads((studio.studio_dir(root, "codex") / "anchors.json").read_text(encoding="utf-8"))
    check("art: the first picture is still an anchor", [x["ref"] for x in ref1_anchor], [ref])
    check("art: reject sets the approval aside", (code, (folder2 / "approval.json").is_file(), (folder2 / "approval-v1.json").is_file()),
          (0, False, True))
    check("art: rejected shows as such", art_of(root)[0]["label"], "đã trả")
    code, out = call(root, "approve", ref2)
    check("art: no new approval before the lesson", (code, "bài học" in out), (1, True))
    (studio.studio_dir(root, "codex") / "lessons.md").write_text(
        f"- Màu đủ sáng (codex/orders/{studio.gd.design_owner(NODE)}/again/feedback-1.md)\n", encoding="utf-8")
    check("art: approved again after the lesson", call(root, "approve", ref2)[0], 0)
    check("art: pending_line hides orders waiting for the human", "again" in studio.pending_line(root, "codex"), True)
    studio.RUNS_FIRST = 2
    (folder / "feedback-1.md").write_text("Không đúng style mới.", encoding="utf-8")
    call(root, "reject", ref)
    check("art: a picture sent back is no anchor any more",
          json.loads((studio.studio_dir(root, "codex") / "anchors.json").read_text(encoding="utf-8")), [])


def run_budget(root):
    """Runs are limited: 2 per item, +1 after each reject, at most 2 redos; the closest result is delivered as
    'chưa chuẩn' with an art debt, which a later passing delivery or the human closes."""
    os.environ["STUDIO_CODEX"] = str(Path(root) / "fake_codex.py")
    os.environ["FAKE_MODE"] = "write1"
    folder = order(root, oid="limits", items=("star",))
    ref = "Wallet/limits"
    check("budget: first run", call(root, "codex", ref, "star")[0], 0)
    check("budget: second run", call(root, "codex", ref, "star")[0], 0)
    code, out = call(root, "codex", ref, "star")
    check("budget: the third run is refused, pointing to pick", (code, "hết lượt (2/2" in out, "pick" in out), (1, True, True))
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
    code, out = call(root, "take", ref)
    note = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/limits/QUALITY.md"
    check("budget: take still copies, with QUALITY.md", (code, note.is_file() and debts[0]["id"] in note.read_text(encoding="utf-8"),
                                                         (note.parent / "quality.json").exists()), (0, True, False))
    snap = studio.snapshot(root)
    o = next(x for x in snap["orders"] if x["ref"] == ref)
    check("budget: snapshot", (o["items"][0]["used"], o["items"][0]["allowed"], o["items"][0]["ok"],
                               len([d for d in snap["debts"] if d["ref"] == ref])), (2, 2, False, 1))
    for n in (1, 2):
        (folder / f"feedback-{n}.md").write_text(f"Lần {n}: chưa đạt.", encoding="utf-8")
        (studio.studio_dir(root, "codex") / "lessons.md").write_text(
            "".join(f"- bài {k} (codex/orders/Wallet/limits/feedback-{k}.md)\n" for k in range(1, n + 1)), encoding="utf-8")
        call(root, "reject", ref)
        check(f"budget: after reject {n}, one more run", (studio.budget(folder, "star")["allowed"], call(root, "codex", ref, "star")[0],
                                                         call(root, "codex", ref, "star")[0]), (2 + n, 0, 1))
        (folder / "work/star.check.md").write_text("chưa đạt: vẫn mỏng", encoding="utf-8")
        call(root, "deliver", "codex", ref)
    (folder / "feedback-3.md").write_text("Lần 3: vẫn mỏng.", encoding="utf-8")
    code, out = call(root, "reject", ref)
    check("budget: the third reject closes the order", (code, studio.read_status(folder)["state"], "đóng" in out), (0, "closed", True))
    open_ = [d for d in studio.read_debts(root) if d["state"] == "open" and d["ref"] == ref]
    check("budget: closed with one debt per item, the earlier ones replaced",
          [(d["item"], d["kind"], "Lần 3" in d["reason"]) for d in open_], [("star", "trả quá số lần", True)])
    (studio.studio_dir(root, "codex") / "lessons.md").write_text("- x (codex/orders/Wallet/limits/feedback-3.md)\n", encoding="utf-8")
    check("budget: a closed order never runs again", call(root, "codex", ref, "star")[0], 1)
    folder2 = order(root, oid="limits-2", items=("star",))
    call(root, "codex", "Wallet/limits-2", "star")
    (folder2 / "work/star.check.md").write_text("đạt", encoding="utf-8")
    code, out = call(root, "deliver", "codex", "Wallet/limits-2")
    closed = [d for d in studio.read_debts(root) if d["state"] == "closed" and d["kind"] == "trả quá số lần"]
    check("budget: a later passing delivery closes the debt", (code, len(closed), "tự đóng" in closed[0]["closedBy"] if closed else ""),
          (0, 1, True))
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


def main():
    root = Path(tempfile.mkdtemp(prefix="studiofx-"))
    try:
        (root / "UnityProject/Assets/Features/Wallet").mkdir(parents=True)
        (root / "UnityProject/Assets/Features/Shop/FeatureAssets/Private").mkdir(parents=True)
        (root / "fake_codex.py").write_text(FAKE_CODEX, encoding="utf-8")
        (root / "fake_blender.py").write_text(FAKE_BLENDER, encoding="utf-8")
        studio.RUNS_FIRST = 99      # the older cases run an item many times; run_budget checks the real limits
        for case in (run_check, run_codex, run_deliver, run_blender):
            case(root)
        studio.RUNS_FIRST = 2
        for case in (run_art, run_budget):
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
