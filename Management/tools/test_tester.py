#!/usr/bin/env python3
"""Tests for tester.py on a throw-away tree in a temp folder. Run: python Management/tools/test_tester.py"""
import io
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import tester  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def call(root, *args):
    out, err = io.StringIO(), io.StringIO()
    with redirect_stdout(out), redirect_stderr(err):
        code = tester.main(["tester.py", *args], root)
    return code, out.getvalue() + err.getvalue()


def main():
    root = Path(tempfile.mkdtemp(prefix="tester-"))
    name = tester.TESTERS[0]
    home = root / tester.BASE / name
    stamps = iter(["20261125-100000", "20261125-110000", "20261125-120000", "20261125-130000"])
    real_now = tester.now
    tester.now = lambda: next(stamps)
    try:
        check("nothing yet: the check passes and the status is empty", (call(root, "check")[0], "0 đợt đang mở" in call(root, "status")[1]), (0, True))
        code, out = call(root, "new", name, "Chơi thử Hud")
        run = home / "20261125-100000"
        text = (run / tester.README).read_text(encoding="utf-8")
        check("new: a run folder named by its time, with a README and images/",
              (code, run.is_dir(), (run / tester.IMAGES).is_dir(), text.startswith("# Chơi thử Hud\n"), f"{tester.BASE}/{name}/20261125-100000/" in out),
              (0, True, True, True, True))
        check("new: the README holds every section", [s for s in tester.SECTIONS if f"## {s}\n" not in text], [])
        check("new: an unknown tester and an empty title are refused", (call(root, "new", "Nobody", "x")[0], call(root, "new", name, " ")[0]), (2, 2))

        problems = tester.check(root)
        check("check: the READMEs of Tester and of the tester are asked for, and the sections not written yet are named",
              (f"{tester.BASE}: thiếu README.md" in problems, f"{tester.BASE}/{name}: thiếu README.md" in problems,
               sum("chưa ghi" in p for p in problems)), (True, True, len(tester.SECTIONS)))
        (root / tester.BASE / tester.README).write_text("# Tester\n", encoding="utf-8")
        (home / tester.README).write_text("# UnityAutoPlay\n", encoding="utf-8")
        filled = "# Chơi thử Hud\n\n" + "\n\n".join(f"## {s}\nđã ghi" for s in tester.SECTIONS) + "\n"
        (run / tester.README).write_text(filled.replace("## Kết quả\nđã ghi", "## Kết quả\nđạt: mọi nút bấm được"), encoding="utf-8")
        check("check: a written run passes", (call(root, "check")[0], tester.check(root)), (0, []))
        (run / "shot.png").write_bytes(b"x")
        (run / tester.IMAGES / "hud.png").write_bytes(b"x")
        (run / "extra").mkdir()
        (home / "notes.txt").write_text("x", encoding="utf-8")
        problems = tester.check(root)
        check("check: data outside images/, a picture the README does not describe, and a stray file are reported",
              (any("shot.png: ảnh và dữ liệu phụ" in p for p in problems), any("hud.png: chưa có dòng mô tả" in p for p in problems),
               any("extra: dữ liệu phụ" in p for p in problems), any("notes.txt: không thuộc cấu trúc" in p for p in problems), call(root, "check")[0]),
              (True, True, True, True, 1))
        (run / "shot.png").unlink(), (run / "extra").rmdir(), (home / "notes.txt").unlink()
        (run / tester.README).write_text((run / tester.README).read_text(encoding="utf-8").replace(
            "## Cấu trúc thư mục\nđã ghi", "## Cấu trúc thư mục\n- images/hud.png: màn Hud giữa trận"), encoding="utf-8")
        check("check: described pictures pass", tester.check(root), [])

        caps = root.joinpath(*tester.CAPTURES)
        caps.mkdir(parents=True)
        (caps / "screenshot-1.png").write_bytes(b"png")
        (caps / "two.jpg").write_bytes(b"jpg")
        (caps / "note.txt").write_text("x", encoding="utf-8")
        code, out = call(root, "shot", name, run.name, "screenshot-1.png")
        check("shot: the picture moves from Captures into images/ of the run, and the line to add is printed",
              (code, (run / tester.IMAGES / "screenshot-1.png").read_bytes(), (caps / "screenshot-1.png").exists(),
               "- images/screenshot-1.png: <là gì>" in out), (0, b"png", False, True))
        code, out = call(root, "shot", name, run.name, "two.jpg", "02-pause.jpg")
        check("shot: with a new name", (code, (run / tester.IMAGES / "02-pause.jpg").is_file(), (caps / "two.jpg").exists()), (0, True, False))
        (caps / "again.png").write_bytes(b"x")
        refused = [call(root, "shot", *a)[0] for a in (
            (name, run.name, "missing.png"), (name, run.name, "note.txt"), (name, run.name, "../x.png"),
            (name, run.name, "again.png", "a/b.png"), (name, run.name, "again.png", ".hidden.png"),
            (name, run.name, "again.png", "screenshot-1.png"), (name, "20991231-000000", "again.png"),
            ("Nobody", run.name, "again.png"), (name, "nope", "again.png"))]
        check("shot: a missing picture, not a picture, a path, a name taken, a run not open and another tester are refused",
              (refused, (caps / "again.png").exists()), ([2] * 9, True))
        if hasattr(Path, "symlink_to"):
            try:
                (caps / "link.png").symlink_to(caps / "again.png")
                check("shot: a link is refused", call(root, "shot", name, run.name, "link.png")[0], 2)
            except OSError:
                pass
        (run / tester.IMAGES / "screenshot-1.png").unlink(), (run / tester.IMAGES / "02-pause.jpg").unlink()
        check("shot: the run still passes the check once the moved pictures are gone again", tester.check(root), [])

        status = call(root, "status", name)[1]
        check("status: the open runs with their purpose and result",
              (f"# {name}: 1 đợt đang mở, 0 đợt đã lưu trữ" in status, "Kết quả: đạt: mọi nút bấm được" in status, "Chơi thử Hud" in status),
              (True, True, True))

        for title in ("Đợt hai", "Đợt ba", "Đợt bốn"):
            call(root, "new", name, title)
        code, out = call(root, "archive", name, "20261125-110000")
        archive = home / tester.ARCHIVE
        check("archive: one run moves to Archive, whole, and the index lists it",
              (code, (archive / "20261125-110000" / tester.README).is_file(), (home / "20261125-110000").exists(),
               "- 20261125-110000: Đợt hai" in (archive / tester.README).read_text(encoding="utf-8")), (0, True, False, True))
        check("archive: a run that is not open is refused", (call(root, "archive", name, "20261125-110000")[0], call(root, "archive", name, "nope")[0]), (2, 2))
        code, out = call(root, "archive", name, "--keep", "1")
        check("archive --keep: all but the newest runs move",
              (code, [p.name for p in tester.runs(home)], [p.name for p in tester.runs(archive)]),
              (0, ["20261125-130000"], ["20261125-100000", "20261125-110000", "20261125-120000"]))
        index = (archive / tester.README).read_text(encoding="utf-8")
        check("archive: the index keeps the result of each run, newest first",
              ("- 20261125-100000: Chơi thử Hud · đạt: mọi nút bấm được" in index, index.index("20261125-120000") < index.index("20261125-100000"),
               (archive / "20261125-100000" / tester.IMAGES / "hud.png").is_file()), (True, True, True))
        check("archive --keep with more than there are moves nothing", (call(root, "archive", name, "--keep", "5")[0], len(tester.runs(home))), (0, 1))
        check("the status counts the archive", f"# {name}: 1 đợt đang mở, 3 đợt đã lưu trữ" in call(root, "status")[1])
        check("a wrong call prints the usage", call(root, "archive", name)[0], 2)
    finally:
        tester.now = real_now
        shutil.rmtree(root, ignore_errors=True)
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
