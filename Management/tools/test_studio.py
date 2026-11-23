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
prompt = sys.stdin.read()
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
          (code, sorted(p.name for p in v1.iterdir())), (0, ["coin.png", "gem.png"]))
    check("deliver tells the tool chat what to send", "SendMessage" in out and "take Wallet/coin-icons" in out)
    check("a blender chat cannot deliver a codex order", call(root, "deliver", "blender", "Wallet/coin-icons")[0], 1)

    code, out = call(root, "take", "Wallet/coin-icons")
    gen = Path(root) / "UnityProject/Assets/Features/Wallet/FeatureAssets/Private/Generated/coin-icons"
    check("take copies the delivery into the feature's assets", (code, sorted(p.name for p in gen.iterdir())), (0, ["coin.png", "gem.png"]))
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
          sorted(p.name for p in gen.iterdir()), ["coin.png", "coin.png.meta", "gem.png"])
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


def main():
    root = Path(tempfile.mkdtemp(prefix="studiofx-"))
    try:
        (root / "UnityProject/Assets/Features/Wallet").mkdir(parents=True)
        (root / "UnityProject/Assets/Features/Shop/FeatureAssets/Private").mkdir(parents=True)
        (root / "fake_codex.py").write_text(FAKE_CODEX, encoding="utf-8")
        (root / "fake_blender.py").write_text(FAKE_BLENDER, encoding="utf-8")
        for case in (run_check, run_codex, run_deliver, run_blender):
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
