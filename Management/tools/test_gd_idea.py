#!/usr/bin/env python3
"""Tests for gd_idea.py (rm, replace of an idea by <node>#<id>) and gd_impact.py (every place that names a node).
The design tree is a throw-away one in a temp folder.
    python Management/tools/test_gd_idea.py
"""
import io
import json
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import gd_idea  # noqa: E402
import gd_impact  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def write(folder, name, text):
    folder.mkdir(parents=True, exist_ok=True)
    (folder / name).write_text(text, encoding="utf-8")


def run(module, root, *args):
    out, err = io.StringIO(), io.StringIO()
    with redirect_stdout(out), redirect_stderr(err):
        code = module.main(["x.py", *args, "--root", str(root)])
    return code, out.getvalue() + err.getvalue()


DATA = """---
by: wave-b=gpt@w1#p1
user: u1
---
# Wave

Quái ra theo đợt.

## Ý cốt lõi

### wave-a | AI 8 | Quái ra theo đợt
Đợt sau đông hơn đợt trước.

### wave-b | AI 7 | Đợt cuối có boss
Boss ra ở đợt cuối, boss to.

### u1 | AI 10 | Ý của người
Người viết.
"""


def build(root):
    idea = gd.table_dir(root, "idea")
    write(idea, "data.md", "# Game\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### game-a | AI 8 | Game chạy ngang\nVì sao.\n")
    write(idea / "feature-wave", "data.md", DATA)
    write(idea / "feature-wave" / gd.REVIEW_DIR, gd.USER_IDEAS_FILE, json.dumps({"items": {"u1": {"hash": "x"}}}))
    return idea / "feature-wave" / "data.md"


def idea_cases(root):
    data = build(root)
    before = data.read_text(encoding="utf-8")

    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#wave-b", "--old", "boss", "--new", "trùm")
    check("replace: a string found twice is refused", (code, "khớp 2 lần" in text), (1, True))
    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#wave-a", "--old", "không có", "--new", "x")
    check("replace: a string not found is refused", (code, "khớp 0 lần" in text), (1, True))
    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#u1", "--old", "Người", "--new", "AI")
    check("the human's idea is refused", (code, "ý của người" in text), (1, True))
    code, text = run(gd_idea, root, "rm", "ai/idea/feature-wave#wave-a", "ai/idea/feature-wave#nope")
    check("one bad target: nothing is written", (code, data.read_text(encoding="utf-8")), (1, before))

    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#wave-b", "--old", "boss to", "--new", "boss rất to")
    after = gd.parse_data(data.read_text(encoding="utf-8"))
    wave_b = next(it for it in after["items"] if it["id"] == "wave-b")
    check("replace: the detail is rewritten, id, score and by: stay",
          (code, wave_b["detail"], wave_b["ai"], wave_b["by"]), (0, "Boss ra ở đợt cuối, boss rất to.", 7, "gpt"))
    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#wave-a", "--old", "Quái ra", "--new", "Quái tới")
    check("replace: the title", next(it for it in gd.parse_data(data.read_text(encoding="utf-8"))["items"]
                                     if it["id"] == "wave-a")["title"], "Quái tới theo đợt")
    code, text = run(gd_idea, root, "replace", "ai/idea/feature-wave#wave-a", "--old", "đợt", "--new", "a|b")
    check("replace: a | in the title is refused", code, 1)

    gd.write_mode(root, "user")
    code, text = run(gd_idea, root, "rm", "ai/idea/feature-wave#wave-b")
    check("rm: GPT's idea in mode user, not scored low yet: refused", (code, "mode user" in text), (1, True))
    gd.write_mode(root, "auto")
    code, text = run(gd_idea, root, "rm", "ai/idea/feature-wave#wave-b", "ai/idea/feature-wave#wave-a")
    now = gd.parse_data(data.read_text(encoding="utf-8"))
    check("rm: both ideas go, GPT's moves from by: to gpt-dropped:",
          (code, [it["id"] for it in now["items"]], now["dropped"][0]["item"] if now["dropped"] else None,
           "by:" in data.read_text(encoding="utf-8"), now["errors"]), (0, ["u1"], "wave-b", False, []))
    log = (data.parent / gd.HISTORY_DIR / gd.LOG_FILE).read_text(encoding="utf-8")
    check("rm and replace are logged", ('"ai-remove"' in log, '"ai-edit"' in log), (True, True))
    check("live is refused", run(gd_idea, root, "rm", "live/feature-wave#u1")[0], 1)
    check("no target: usage", run(gd_idea, root, "rm")[0], 2)


def impact_cases(root):
    idea = gd.table_dir(root, "idea")
    write(idea, "data.md", "# Game\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### game-a | AI 8 | Game chạy ngang\nVì sao.\n")
    write(idea / "feature-ads", "data.md", "# Ads\n\nQuảng cáo.\n\n## Ý cốt lõi\n\n### ads-a | AI 8 | Xem quảng cáo\nVì sao.\n")
    write(idea / "feature-ads", "code.json", json.dumps({"unity": ["Ads"]}))
    write(idea / "feature-home", "data.md", "---\nbased-on: x\nrefs: feature-ads@abc\n---\n# Home\n\nMàn chính.\n")
    write(idea / "feature-home", "CLAUDE.md", "# Home\n\nNút FEATURE-ADS ở góc.\n")
    write(idea / "feature-home" / "home-ui" / gd.FRAME_DIR, "main.frame", "button ads_btn \"Feature-Ads\"\n")
    write(idea / "feature-home" / "home-ui" / ".excel", "table.csv", "id,src\n1,feature-ads\n")
    write(idea / ".art-style", "style.md", "Biểu tượng của feature-ads màu vàng.\n")
    write(idea / "feature-home" / gd.HISTORY_DIR, "data-1-abc.md", "refs: feature-ads@old\n")
    write(idea / "feature-shop", "data.md", "# Shop\n\nfeature-adsx không phải nó.\nAdsView là nó.\n")
    code, text = run(gd_impact, root, "feature-ads")
    check("refs, any case, frame, csv, .art-style", ("[refs]" in text, "FEATURE-ADS" in text, "main.frame" in text,
                                                    "table.csv" in text, ".art-style/style.md" in text), (True,) * 5)
    check(".history and the node's own files are left out", ("data-1-abc" in text, "## feature-ads" in text), (False, False))
    check("a whole word only, but the Unity names start a word", ("feature-adsx" in text, "AdsView" in text), (False, True))
    shutil.rmtree(idea / "feature-ads")
    code, text = run(gd_impact, root, "ai/idea/feature-ads")
    check("a deleted node: still found, with the hint", ("node không còn" in text, "[refs]" in text, "--drop-dead" in text),
          (True, True, True))


def main():
    tmp = tempfile.mkdtemp(prefix="gd-idea-test-")
    try:
        for name, cases in (("idea", idea_cases), ("impact", impact_cases)):
            root = Path(tmp) / name
            root.mkdir()
            cases(root)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    failed = [r for r in results if not r[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
