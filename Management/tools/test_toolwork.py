#!/usr/bin/env python3
"""Tests of toolwork.py (the list of tool work the human decided on). Run: python Management/tools/test_toolwork.py"""
import io
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import toolwork as tw  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def refused(fn, *args, **kw):
    try:
        fn(*args, **kw)
        return False
    except ValueError:
        return True


def main():
    root = Path(tempfile.mkdtemp(prefix="toolwork-"))
    try:
        check("empty: nothing to do", (tw.items(root), tw.summary(root)), ([], {"open": [], "recent": []}))
        a = tw.add(root, "Thêm lệnh  huỷ order\nvào studio.py", "@Manager", "Người chọn A ngày 2026-11-26", now=1_800_000_000)
        check("add: one open item, its text on one line, who asked and what the human decided",
              (a["new"], a["state"], a["text"], a["source"], a["decided"], bool(tw.ID_RX.match(a["id"]))),
              (True, "open", "Thêm lệnh huỷ order vào studio.py", "@Manager", "Người chọn A ngày 2026-11-26", True))
        check("add: the same work still open is not written twice",
              (tw.add(root, "Thêm lệnh huỷ order vào studio.py", "@CodeLead", "x")["new"], len(tw.items(root))), (False, 1))
        check("add: refused without a text, without the decision, from someone who is no Leader",
              (refused(tw.add, root, " ", "@Manager", "x"), refused(tw.add, root, "x", "@Manager", ""),
               refused(tw.add, root, "x", "Wallet", "y"), refused(tw.add, root, "x" * 2001, "@Manager", "y")), (True, True, True, True))
        b = tw.add(root, "Chặn ghi code khi tester chơi", "@Manager", "Người chọn chặn ghi và hoãn giao", now=1_800_000_100)
        done = tw.close(root, a["id"], "done", "Đã thêm studio.py cancel, commit abc1234", now=1_800_000_200)
        check("done: closed with what was done", (done["state"], done["note"], [x["id"] for x in tw.items(root, "open")]),
              ("done", "Đã thêm studio.py cancel, commit abc1234", [b["id"]]))
        check("done: not twice, not without a note, not an unknown id",
              (refused(tw.close, root, a["id"], "done", "x"), refused(tw.close, root, b["id"], "done", ""),
               refused(tw.close, root, "w00000000-0000", "done", "x"), refused(tw.close, root, "../x", "dropped", "x")),
              (True, True, True, True))
        check("summary: what waits, and what was closed in the last day",
              ([x["id"] for x in tw.summary(root, 1_800_000_300)["open"]], [x["id"] for x in tw.summary(root, 1_800_000_300)["recent"]],
               tw.summary(root, 1_800_000_200 + tw.RECENT_SECONDS + 1)["recent"]), ([b["id"]], [a["id"]], []))
        tw.close(root, b["id"], "dropped", "Người đổi ý", now=1_800_000_400)
        check("drop: closed with why", [(x["state"], x["note"]) for x in tw.items(root, "dropped")], [("dropped", "Người đổi ý")])
        (tw.folder(root) / "w20260101-zzzz.json").write_text("{broken", encoding="utf-8")
        check("a broken file is left out", len(tw.items(root)), 2)
        out, err = io.StringIO(), io.StringIO()
        real = tw.HERE
        tw.HERE = root / "Management" / "tools"
        try:
            with redirect_stdout(out), redirect_stderr(err):
                codes = (tw.main(["toolwork.py", "add", "Việc ba", "--from", "Manager", "--decided", "Người chọn C"]),
                         tw.main(["toolwork.py", "list"]), tw.main(["toolwork.py", "list", "--all"]),
                         tw.main(["toolwork.py", "add", "thiếu quyết định", "--from", "Manager"]),
                         tw.main(["toolwork.py", "done", "w00000000-0000", "x"]))
        finally:
            tw.HERE = real
        text = out.getvalue()
        check("command line: add, list, list --all; a wrong call and an unknown id are refused",
              (codes, "Đã ghi việc công cụ" in text, "# Việc công cụ (đang chờ làm): 1" in text, "# Việc công cụ (tất cả): 3" in text,
               "Người quyết: Người chọn C" in text, "không có việc" in err.getvalue()), ((0, 0, 0, 2, 2), True, True, True, True, True))
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
