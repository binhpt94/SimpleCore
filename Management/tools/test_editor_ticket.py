#!/usr/bin/env python3
"""Tests of editor_ticket.py: tickets for the shared Unity Editor, granted one at a time, common needs met for all."""
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import editor_ticket as et      # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def refused(fn, *a, **k):
    try:
        fn(*a, **k)
    except ValueError:
        return True
    return False


R, C, T = 1 << 0, 1 << 1, 1 << 2


def main():
    root = Path(tempfile.mkdtemp(prefix="ticket-"))
    try:
        check("a ticket needs something", refused(et.add, root, "Wallet"))
        check("a bad session is refused", refused(et.add, root, "../x", ["refresh"]))
        check("an unknown common need is refused", refused(et.add, root, "Wallet", ["coffee"]))
        a = et.add(root, "Wallet", ["refresh", "clean"], now=1)
        check("a code-only ticket: common bits only", (a["code"], et.code_only(a)), (R | C, True))
        a2 = et.add(root, "Wallet", [], ["tạo WalletConfig.asset"], now=5)
        check("one open ticket per session: more needs join it and keep its place",
              (a2["id"], a2["code"], a2["created"], len(et.queue(root))), (a["id"], R | C | (1 << 10), 1, 1))
        et.drop(root, "Wallet", ["tạo WalletConfig.asset"])
        check("drop one task clears its bit", et.find(root, "Wallet")["code"], R | C)
        b = et.add(root, "Hud", ["refresh", "clean"], now=2)
        c = et.add(root, "Home", ["refresh"], ["tạo HomeMain.prefab"], now=3)
        d = et.add(root, "Save", [], [], [{"ref": "feature-save/save-io/io-rules#write", "tests": ["SaveTests.Writes"]}], now=4)
        check("a ticket with requirements waits for the test bit", d["code"], T)
        check("a bad requirement is refused", refused(et.add, root, "Save", reqs=[{"ref": "nope", "tests": []}]))

        kind, group = et.grant_next(root, now=10)
        check("first grant: every code-only ticket loads together", (kind, sorted(t["session"] for t in group)), ("load", ["Hud", "Wallet"]))
        check("nothing else is granted while the load holds the Editor", et.grant_next(root, now=11), (None, []))
        r = et.release(root, met=["refresh", "clean"], now=12)
        check("the load done: its tickets are released, Home's refresh bit met for it",
              (len(r["released"]), et.find(root, "Home")["code"], r["fix"]), (2, 1 << 10, None))

        kind, group = et.grant_next(root, now=13)
        check("next: Home's own task, alone", (kind, [t["session"] for t in group]), ("own", ["Home"]))
        et.add(root, "Home", [], ["thêm task khi đang giữ"], now=14)
        check("a task added while granted goes into a new ticket at the end",
              [(t["session"], t["state"]) for t in et.tickets(root, ("waiting", "granted")) if t["session"] == "Home"],
              [("Home", "granted"), ("Home", "waiting")])
        r = et.release(root, "Home", now=15)
        check("released with its own task undone: what was left joins the ticket it already queued",
              (len(r["requeued"]), sorted(et.describe(r["requeued"][0]).split(", ")), len([t for t in et.queue(root) if t["session"] == "Home"])),
              (1, ["thêm task khi đang giữ", "tạo HomeMain.prefab"], 1))
        e = et.add(root, "Run", ["refresh", "clean"], now=16)
        kind, group = et.grant_next(root, now=17)
        check("first come first served: Home's ticket (queued earlier) before Run's", (kind, [t["session"] for t in group]), ("own", ["Home"]))
        et.done(root, "Home", ["tạo HomeMain.prefab", "thêm task khi đang giữ"])
        r = et.release(root, "Home", now=18)
        check("a holder that did everything leaves nothing behind", r["requeued"], [])
        kind, group = et.grant_next(root, now=21)
        check("a load that leaves compile errors", (kind, [t["session"] for t in group]), ("load", ["Run"]))
        r = et.release(root, now=22)
        check("makes a CodeLead fix ticket that goes first", (r["fix"]["session"], r["fix"]["priority"], et.queue(root)[0]["session"]),
              ("@CodeLead", True, "@CodeLead"))
        kind, group = et.grant_next(root, now=23)
        check("the fix is granted first", (kind, group[0]["session"]), ("fix", "@CodeLead"))
        et.done(root, "@CodeLead", ["clean"])
        r = et.release(root, "@CodeLead", now=24)
        check("the fix met clean for everyone, nothing left", (r["fix"], [t["session"] for t in r["requeued"]]), (None, []))
        while True:
            kind, group = et.grant_next(root, now=25)
            if kind in (None, "test"):
                break
            et.release(root, met=["refresh", "clean"], now=26)
        check("the test run comes when nothing else needs the Editor", (kind, [t["session"] for t in group]), ("test", ["Save"]))
        r = et.release(root, met=["test"], now=27)
        check("the test run done: the queue is empty", (et.queue(root), et.holder(root)), ([], []))
        check("summary line when idle is empty", et.summary_line(root), "")
        check("main: a wrong command is refused", et.main(["editor_ticket.py", "nope"]), 2)
    finally:
        shutil.rmtree(root, ignore_errors=True)
    failed = RESULTS.count(False)
    print(f"TOTAL: {RESULTS.count(True)} ok, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
