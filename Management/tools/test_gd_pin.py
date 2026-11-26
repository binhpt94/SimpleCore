#!/usr/bin/env python3
"""Tests for gd_pin.py (pin based-on and refs again after reading what changed) and gd_status.py --hashes.
The design tree is a throw-away one in a temp folder.
    python Management/tools/test_gd_pin.py
"""
import io
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_pin  # noqa: E402
import gd_status  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def node(root, path, body, head=""):
    folder = gd.table_dir(root, "idea").joinpath(*(path.split("/") if path else []))
    folder.mkdir(parents=True, exist_ok=True)
    title = path.split("/")[-1] if path else "Game"
    (folder / "data.md").write_text(f"{head}# {title}\n\nTóm tắt.\n\n## {gd.ITEMS_TITLE}\n{body}", encoding="utf-8")
    (folder / "CLAUDE.md").write_text(f"# {title}\n\nDữ liệu cốt lõi: data.md\n\nChi tiết của {title}.\n", encoding="utf-8")
    return folder


def idea(i, title, detail="Vì sao."):
    return f"\n### {i} | AI 8 | {title}\n{detail}\n"


def h(root, path):
    return gd.read_data(gd.table_dir(root, "idea").joinpath(*(path.split("/") if path else [])))["hash"]


def head(based, refs=""):
    return "---\nbased-on: " + based + "\n" + (f"refs: {refs}\n" if refs else "") + "---\n"


def run(root, *args):
    out, err = io.StringIO(), io.StringIO()
    with redirect_stdout(out), redirect_stderr(err):
        code = gd_pin.main(["gd_pin.py", *args, "--root", str(root)])
    return code, out.getvalue() + err.getvalue()


def node_of(root, path):
    return next(n for n in gd.walk(gd.snapshot(root)["idea"]) if n["path"] == path)


def build(root):
    node(root, "", idea("game-a", "Game chạy ngang"))
    node(root, "feature-hero", idea("hero-a", "Hero đánh gần") + idea("hero-b", "Hero có máu"), head(h(root, "")))
    node(root, "feature-wave", idea("wave-a", "Quái ra theo đợt"), head(h(root, "")))
    node(root, "feature-hero/hero-list", idea("list-a", "Có 5 hero"), head(h(root, "feature-hero")))
    node(root, "feature-hero/hero-list/list-spec", idea("spec-a", "Mỗi hero một ô"),
         head(h(root, "feature-hero/hero-list"), f"feature-wave@{h(root, 'feature-wave')}"))
    gd.record_versions(root)      # the copies in .history the diff reads


def pin_cases(root):
    build(root)
    code, text = run(root, "ai/idea/feature-hero", "--chain")
    check("nothing to pin while everything matches", (code, "không có gì để ghim" in text), (0, True))

    hero = gd.table_dir(root, "idea") / "feature-hero" / "data.md"
    hero.write_text(hero.read_text(encoding="utf-8").replace("Hero đánh gần", "Hero đánh xa").replace(
        idea("hero-b", "Hero có máu"), idea("hero-c", "Hero có giáp")), encoding="utf-8")
    wave = gd.table_dir(root, "idea") / "feature-wave" / "data.md"
    wave.write_text(wave.read_text(encoding="utf-8").replace("Vì sao.", "Đợt sau đông hơn."), encoding="utf-8")
    before = {p: h(root, p) for p in ("feature-hero/hero-list", "feature-hero/hero-list/list-spec")}

    code, text = run(root, "ai/idea/feature-hero", "--chain")
    check("the diff names what changed above: rewritten, added, removed",
          ("viết lại [hero-a] Hero đánh gần -> Hero đánh xa" in text, "thêm [hero-c] Hero có giáp" in text,
           "bỏ [hero-b] Hero có máu" in text), (True, True, True))
    check("a ref's diff: the detail rewritten", "viết lại chi tiết [wave-a]" in text and "Đợt sau đông hơn." in text)
    check("only the nodes that are off", ("## feature-hero/hero-list\n" in text, "## feature-hero\n" in text), (True, False))
    check("the last line is the command with its code", "--chain --write " in text.strip().splitlines()[-1])
    good = text.strip().split("--write ")[-1].strip()

    code, text = run(root, "ai/idea/feature-hero", "--chain", "--write", "deadbeef")
    check("a wrong code is refused, nothing written", (code, "không còn khớp" in text, node_of(root, "feature-hero/hero-list")["stale"]),
          (1, True, True))
    code, text = run(root, "ai/idea/feature-hero", "--chain", "--write", good)
    check("the right code pins the chain", (code, "Đã ghim" in text), (0, True))
    lst, spec = node_of(root, "feature-hero/hero-list"), node_of(root, "feature-hero/hero-list/list-spec")
    check("based-on and refs at the current hashes", (lst["stale"], spec["stale"], spec["refStale"]), (False, False, []))
    check("pinning leaves the data hashes alone", {p: h(root, p) for p in before}, before)
    check("the rest of data.md is as it was", "### list-a | AI 8 | Có 5 hero" in
          (gd.table_dir(root, "idea") / "feature-hero" / "hero-list" / "data.md").read_text(encoding="utf-8"))

    for f in (gd.table_dir(root, "idea") / "feature-hero" / gd.HISTORY_DIR).glob("data-*.md"):
        f.unlink()
    hero.write_text(hero.read_text(encoding="utf-8").replace("Hero đánh xa", "Hero bắn cung"), encoding="utf-8")
    code, text = run(root, "ai/idea/feature-hero/hero-list")
    check("no old copy left: read the node", "không còn bản" in text and "Read ai/idea/feature-hero" in text)

    code, text = run(root, "live/feature-hero")
    check("live is refused", (code, "live" in text), (1, True))
    code, text = run(root, "ai/idea/feature-nowhere")
    check("a missing node is refused", code, 1)
    code, text = run(root)
    check("no path: usage", code, 2)


def hashes_cases(root):
    text = gd_status.hash_lines(root, "ai/idea/feature-hero")
    check("--hashes: one line per node", len([x for x in text.splitlines() if x.startswith("feature-hero")]), 3)
    check("--hashes: an off based-on, with both hashes",
          any(x.startswith("feature-hero/hero-list ") and "based-on LỆCH" in x for x in text.splitlines()))
    check("--hashes: a matching ref", any("ref feature-wave khớp" in x for x in text.splitlines()))
    check("--hashes: the pin command when something is off", "gd_pin.py ai/idea/feature-hero --chain" in text)


def main():
    tmp = tempfile.mkdtemp(prefix="gd-pin-test-")
    root = Path(tmp) / "project"
    root.mkdir()
    try:
        pin_cases(root)
        hashes_cases(root)
    finally:
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
