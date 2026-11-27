#!/usr/bin/env python3
"""Tests of bugs.py on a temp project: add, the same bug twice, fix sends one inbox message, done and drop.

    python Management/tools/test_bugs.py
"""
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import bugs  # noqa: E402
import dispatch  # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def refused(fn):
    try:
        fn()
    except ValueError:
        return True
    return False


def main():
    root = Path(tempfile.mkdtemp(prefix="bugs-"))
    try:
        for name in ("Hud", "HudUpgrade"):
            (root / "UnityProject/Assets/Features" / name).mkdir(parents=True)
        b = bugs.add(root, "State Giữa trận chỉ ra màn bắt đầu", "Hud,HudUpgrade", "@UnityAutoPlay", seen="Tester/UnityAutoPlay/x", now=1)
        check("add: a bug waits, nobody is given it", (b["new"], b["state"], b["features"], b["message"]), (True, "open", ["Hud", "HudUpgrade"], ""))
        check("add: nothing goes to the inbox", dispatch.messages(root) if hasattr(dispatch, "messages") else [], [])
        check("add: the same bug again is not written twice", (bugs.add(root, "State Giữa trận  chỉ ra màn bắt đầu", "Hud", "người")["id"],
              len(bugs.items(root))), (b["id"], 1))
        check("add: an unknown feature is refused", refused(lambda: bugs.add(root, "x", "Nope", "người")))
        check("add: an unknown source is refused", refused(lambda: bugs.add(root, "x", "Hud", "@Codex")))
        check("list: by feature", ([x["id"] for x in bugs.items(root, feature="HudUpgrade")], bugs.items(root, feature="Run")), ([b["id"]], []))
        f = bugs.fix(root, b["id"], "người", now=2)
        m = dispatch.get(root, f["message"])
        check("fix: one message to the first feature, the bug marked fixing",
              (f["state"], m["target"], b["id"] in m["text"], "HudUpgrade" in m["text"]), ("fixing", "Hud", True, True))
        check("fix: a bug already given is not given twice", refused(lambda: bugs.fix(root, b["id"])))
        d = bugs.close(root, b["id"], "fixed", "worker w1 sửa, test qua", now=3)
        check("done: the bug is fixed with its note", (d["state"], d["note"]), ("fixed", "worker w1 sửa, test qua"))
        check("done: not twice", refused(lambda: bugs.close(root, b["id"], "dropped", "x")))
        x = bugs.add(root, "bug khác", "Hud", "người")
        check("drop: a waiting bug can be dropped", bugs.close(root, x["id"], "dropped", "không cần")["state"], "dropped")
        check("list: open and fixing only by default", bugs.items(root, ("open", "fixing")), [])
        check("main: a wrong command is refused", bugs.main(["bugs.py", "nope"]), 2)
    finally:
        shutil.rmtree(root, ignore_errors=True)
    print(f"TOTAL: {sum(RESULTS)} ok, {len(RESULTS) - sum(RESULTS)} failed")
    return 0 if all(RESULTS) else 1


if __name__ == "__main__":
    sys.exit(main())
