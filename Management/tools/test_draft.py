#!/usr/bin/env python3
"""Tests of draft.py: a feature's code drafts outside Assets, loaded only by the Editor ticket holder."""
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import draft      # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def refused(fn, *a):
    try:
        fn(*a)
    except ValueError:
        return True
    return False


def main():
    root = Path(tempfile.mkdtemp(prefix="draft-"))
    try:
        feat = root / "UnityProject/Assets/Features/Wallet"
        (feat / "Scripts/Internal").mkdir(parents=True)
        (feat / "Scripts/Internal/Wallet.cs").write_text("class Wallet {}\n", encoding="utf-8")
        (feat / "Scripts/Internal/Old.cs").write_text("class Old {}\n", encoding="utf-8")
        (feat / "Scripts/Internal/Old.cs.meta").write_text("guid: x\n", encoding="utf-8")
        d = root / "UnityProject/Drafts/Wallet"

        check("a path outside the folder is refused", (refused(draft.start, root, "Wallet", ["../x.cs"]),
              refused(draft.start, root, "Wallet", ["/abs.cs"]), refused(draft.start, root, "Wallet", ["a.asset"])), (True, True, True))
        check("a name that is no feature is refused", refused(draft.start, root, "../Wallet", ["a.cs"]))
        draft.start(root, "Wallet", ["Scripts/Internal/Wallet.cs"])
        check("start copies the file into the draft", (d / "Scripts/Internal/Wallet.cs").read_text(), "class Wallet {}\n")
        check("start of a missing file is refused (use new)", refused(draft.start, root, "Wallet", ["Scripts/Internal/Nope.cs"]))
        draft.new(root, "Wallet", ["Scripts/Internal/Purse.cs"])
        (d / "Scripts/Internal/Purse.cs").write_text("class Purse {}\n", encoding="utf-8")
        (d / "Scripts/Internal/Wallet.cs").write_text("class Wallet { int coins; }\n", encoding="utf-8")
        draft.delete(root, "Wallet", ["Scripts/Internal/Old.cs"])
        rows = draft.entries(root, "Wallet")
        check("status: what the draft changes", [(p, k, x) for p, k, x in rows],
              [("Scripts/Internal/Old.cs", "xoá", False), ("Scripts/Internal/Purse.cs", "mới", False), ("Scripts/Internal/Wallet.cs", "sửa", False)])
        check("diff shows the change", any("+class Wallet { int coins; }" in x for x in draft.diff(root, "Wallet")))

        (feat / "Scripts/Internal/Wallet.cs").write_text("class Wallet { /* CodeLead fixed */ }\n", encoding="utf-8")
        lines, ok = draft.apply(root, "Wallet")
        check("apply refuses a file of Assets changed since the draft began, writes nothing",
              (ok, "LỆCH" in " ".join(draft.status(root, "Wallet")), (feat / "Scripts/Internal/Purse.cs").exists()), (False, True, False))
        draft.rebase(root, "Wallet", ["Scripts/Internal/Wallet.cs"])
        lines, ok = draft.apply(root, "Wallet")
        check("after rebase, apply loads the draft into Assets", (ok, (feat / "Scripts/Internal/Wallet.cs").read_text(),
              (feat / "Scripts/Internal/Purse.cs").read_text()), (True, "class Wallet { int coins; }\n", "class Purse {}\n"))
        check("apply removes the deleted file and its .meta", ((feat / "Scripts/Internal/Old.cs").exists(),
              (feat / "Scripts/Internal/Old.cs.meta").exists()), (False, False))
        check("apply empties the draft and keeps a log", (draft.entries(root, "Wallet"), len(list((d / ".applied").glob("*.json")))), ([], 1))
        check("apply of an empty draft does nothing", draft.apply(root, "Wallet")[1], False)
        draft.start(root, "Wallet", ["Scripts/Internal/Wallet.cs"])
        draft.drop(root, "Wallet")
        check("drop throws the draft away", d.exists(), False)
        check("Core and services map to their folders", (draft.folder_of(root, "@Core")[1].name, draft.folder_of(root, "_Services/Popup")[1].as_posix().endswith(
            "Features/_Services/Popup")), ("Core", True))
        check("main: a wrong command prints the usage", draft.main(["draft.py", "nope"]), 2)
    finally:
        shutil.rmtree(root, ignore_errors=True)
    failed = RESULTS.count(False)
    print(f"TOTAL: {RESULTS.count(True)} ok, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
