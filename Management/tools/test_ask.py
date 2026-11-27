#!/usr/bin/env python3
"""Tests of ask.py, the questions for the human that only @Manager asks (người quyết 2026-11-27)."""
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import ask      # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def refused(fn, *args, **kw):
    try:
        fn(*args, **kw)
    except ValueError:
        return True
    return False


def main():
    root = tempfile.mkdtemp(prefix="ask-")
    try:
        check("no question yet", ask.items(root), [])
        q = ask.add(root, "CodeLead", "sửa manifest", "guard hỏi", "A thêm gói·B thôi", "A", "Wallet")
        check("a Leader adds, named with or without @", (q["new"], q["source"], q["state"]), (True, "@CodeLead", "open"))
        check("the same open job is not written twice", (ask.add(root, "@CodeLead", "sửa manifest", "x", "y", "z", "w")["new"],
                                                         len(ask.items(root))), (False, 1))
        for who in ("@Manager", "Wallet", "@Core", "@human"):
            check(f"only a Leader adds: {who}", refused(ask.add, root, who, "v", "x", "y", "z", "w"))
        check("every field must have words", refused(ask.add, root, "@GameDesigner", "v", "", "y", "z", "w"))
        check("an id that is not one is refused", refused(ask.answer, root, "../x", "ok"))
        check("--uses out of range", refused(ask.answer, root, q["id"], "ok", allow="UnityProject/Packages", uses=0))
        check("--allow too short", refused(ask.answer, root, q["id"], "ok", allow="a"))
        done = ask.answer(root, q["id"], "cho thêm gói", allow="UnityProject/Packages/manifest.json", uses=2, now=1000)
        check("the answer is written with the permission", (done["state"], done["allow_uses"], done["allow_until"]),
              ("answered", 2, 1000 + ask.ALLOW_MINUTES * 60))
        check("an answered question is not answered again", refused(ask.answer, root, q["id"], "lại"))
        text = '{"file_path": "UnityProject/Packages/manifest.json"}'
        hit = ask.allowance(root, "@CodeLead", text, now=1001)
        check("the permission covers that Leader's call", hit and hit["id"], q["id"])
        check("not another Leader's", ask.allowance(root, "@GameDesigner", text, now=1001), None)
        check("not after its time", ask.allowance(root, "@CodeLead", text, now=1000 + ask.ALLOW_MINUTES * 60 + 1), None)
        check("not another path", ask.allowance(root, "@CodeLead", '{"file_path": "UnityProject/ProjectSettings/x"}', now=1001), None)
        ask.use(root, ask.use(root, hit))
        check("not once its uses are spent", ask.allowance(root, "@CodeLead", text, now=1001), None)
        g = ask.add(root, "@GameDesigner", "chọn tên", "x", "A·B", "A", "GD")
        check("only the asker or Manager cancels", refused(ask.cancel, root, g["id"], "@UIDesigner", "thôi"))
        check("the asker cancels", ask.cancel(root, g["id"], "GameDesigner", "đã có câu trả lời trên trang")["state"], "cancelled")
        s = ask.summary(root)
        check("summary: open, and closed within a day (the answer above is dated 1970)", (len(s["open"]), len(s["recent"])), (0, 1))
        check("main: a bad command prints the usage and is refused", ask.main(["ask.py", "nope"]), 2)
    finally:
        shutil.rmtree(root, ignore_errors=True)
    failed = RESULTS.count(False)
    print(f"TOTAL: {RESULTS.count(True)} ok, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
