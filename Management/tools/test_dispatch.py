#!/usr/bin/env python3
"""Tests for dispatch.py (the inbox of the on-demand threads) on a throw-away folder.
Run: python Management/tools/test_dispatch.py"""
import io
import json
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import dispatch as dp  # noqa: E402

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
    root = Path(tempfile.mkdtemp(prefix="inbox-"))
    try:
        check("pools: features, services and @Core; the tools; the tester; never a Leader, the human or a bad name",
              [dp.pool_of(x) for x in ("Wallet", "_Services/Popup", "@Core", "@Codex", "@DesignAdvisor", "@Manager",
                                       "@CodeLead", "@human", "@UnityAutoPlay", "wallet", "../x", "", None)],
              ["feature", "feature", "feature", "tool", "tool", None, None, None, "tester", None, None, None, None])
        check("config: the defaults when there is no file (today's behaviour)", dp.config(root),
              {"mode": "always-live", "limits": {"feature": 4, "tool": 4}, "idle_minutes": 20})
        (dp.folder(root)).mkdir(parents=True)
        (dp.folder(root) / dp.CONFIG).write_text('{"mode": "wild", "limits": {"feature": 0, "tool": "9"}, "idle_minutes": 1}')
        check("config: every bad value reads as its default", dp.config(root), dp.DEFAULT_CONFIG)
        (dp.folder(root) / dp.CONFIG).write_text("{ broken")
        check("config: a broken file reads as the defaults", dp.config(root)["mode"], "always-live")
        dp.set_config(root, mode="on-demand", limits={"feature": 2}, idle_minutes=15)
        check("config: saved", dp.config(root), {"mode": "on-demand", "limits": {"feature": 2, "tool": 4}, "idle_minutes": 15})
        for label, kw in (("bad mode", {"mode": "x"}), ("limit 0", {"limits": {"feature": 0}}), ("unknown pool", {"limits": {"x": 2}}),
                          ("limit not a number", {"limits": {"tool": "3"}}), ("idle too short", {"idle_minutes": 1})):
            check("config refused: " + label, refused(dp.set_config, root, **kw))

        a = dp.send(root, "Wallet", "  Làm yêu cầu feature-wallet/x#y  ", "@Manager", now=1000)
        check("send: a queued message, text trimmed, in the feature pool",
              (a["state"], a["pool"], a["text"], a["tries"], bool(dp.ID_RX.match(a["id"]))),
              ("queued", "feature", "Làm yêu cầu feature-wallet/x#y", 0, True))
        for label, args in (("a Leader", ("@CodeLead", "x", "@Manager")), ("the human", ("@human", "x", "@Manager")),
                            ("a bad name", ("../Wallet", "x", "@Manager")),
                            ("no text", ("Wallet", "   ", "@Manager")), ("too long", ("Wallet", "x" * (dp.TEXT_MAX + 1), "@Manager")),
                            ("unknown source", ("Wallet", "x", "@Hacker"))):
            check("send refused: " + label, refused(dp.send, root, *args))
        tester_root = Path(tempfile.mkdtemp(prefix="inbox-tester-"))
        try:
            check("send: a tester takes a message from a Leader, in a pool of its own that has no limit",
                  (dp.send(tester_root, "@UnityAutoPlay", "Chơi tiếp R1", "@CodeLead")["pool"], "tester" in dp.config(tester_root)["limits"],
                   "tester" in dp.ON_DEMAND), ("tester", False, False))
        finally:
            shutil.rmtree(tester_root, ignore_errors=True)
        k1 = dp.send(root, "Wallet", "Yêu cầu bị trả", "gd_req", key="reject:a#b", now=1001)
        k2 = dp.send(root, "Wallet", "Yêu cầu bị trả (lần 2)", "gd_req", key="reject:a#b", now=1002)
        check("send: same key while not delivered gives the same message", (k1["id"] == k2["id"], len(dp.messages(root))), (True, 2))
        dp.mark(root, k1, "delivered")
        k3 = dp.send(root, "Wallet", "Yêu cầu bị trả lại", "gd_req", key="reject:a#b", now=1003)
        check("send: same key after delivery is a new message", k3["id"] != k1["id"])
        check("messages: oldest first, filter by target", ([m["id"] for m in dp.messages(root)] == sorted(m["id"] for m in dp.messages(root)),
                                                            len(dp.messages(root, "Wallet")), len(dp.messages(root, "Shop"))), (True, 3, 0))
        check("pending: only the ones not delivered", sorted(m["id"] for m in dp.pending(root)), sorted([a["id"], k3["id"]]))

        m = dp.mark(root, a, "delivering", tries=1)
        m = dp.failed(root, m, "boom", now=2000)
        check("failed: back to the queue after a wait", (m["state"], m["next_try"], m["error"]), ("queued", 2000 + dp.RETRY_SECONDS[0], "boom"))
        m = dp.failed(root, dp.mark(root, m, "delivering", tries=dp.MAX_TRIES), "boom again", now=3000)
        check("failed: an error after the last try", (m["state"], m["error"]), ("error", "boom again"))
        check("retry: an error goes back to the queue, tries reset", (dp.retry(root, m["id"])["state"], dp.get(root, m["id"])["tries"]), ("queued", 0))
        check("cancel: a waiting message", dp.cancel(root, m["id"])["state"], "cancelled")
        check("cancel refused: a delivered one", refused(dp.cancel, root, k1["id"]))
        check("cancel refused: an unknown id", refused(dp.cancel, root, "m20990101-000000-abcdef"))
        check("get refused: a path", refused(dp.get, root, "../x"))

        p = dp.prompt([dp.get(root, k3["id"])])
        check("prompt: each message with its id and source, and the rule to skip a message already done",
              (k3["id"] in p, "từ gd_req" in p, "bỏ qua" in p, "Yêu cầu bị trả lại" in p), (True, True, True, True))
        (dp.folder(root) / "m20990101-000000-zzzzzz.json").write_text('{"id": "../x", "target": "Wallet", "state": "queued", "text": "x"}')
        (dp.folder(root) / "m20990101-000001-aaaaaa.json").write_text("{ broken")
        check("a broken or forged file is not a message", all(x["id"] != "../x" for x in dp.messages(root)) and len(dp.messages(root)) == 3)
        check("no temp file is left behind", [x.name for x in dp.folder(root).iterdir() if x.name.endswith(".tmp")], [])
        s = dp.summary(root, now=1003 + 31 * 60)
        check("summary: open, error, waiting over 30 minutes", (s["open"], s["error"], s["old"]), (1, 0, 1))

        import subprocess
        held = dp.courier_lock(root)
        other = subprocess.run([sys.executable, "-c", "import sys; sys.path.insert(0, %r); import dispatch; print(dispatch.courier_lock(%r) is None)"
                                % (str(Path(__file__).resolve().parent), str(root))], capture_output=True, text=True).stdout.strip()
        check("courier lock: one process holds it, another one is refused", (held is not None, other), (True, "True"))
        held.close()
        sys.path.insert(0, str(Path(__file__).resolve().parents[2] / ".claude" / "hooks"))
        import bind_session
        here = str(Path(__file__).resolve().parent)

        def server():      # another process that holds the courier lock, as a running setup_server.py does
            p = subprocess.Popen([sys.executable, "-c", "import sys, time; sys.path.insert(0, %r); import dispatch; "
                                  "f = dispatch.courier_lock(%r); print('held' if f else 'busy', flush=True); time.sleep(60)"
                                  % (here, str(root))], stdout=subprocess.PIPE, text=True)
            return p, p.stdout.readline().strip()

        check("courier_alive: no server holds the lock: False, and the lock is free again afterwards",
              (dp.courier_alive(root), dp.courier_alive(root)), (False, False))
        line = bind_session.inbox_line(str(root))
        check("server off with a message waiting: every Leader is told SERVER TẮT and not to give more work",
              ("SERVER TẮT: 1 tin" in line, "setup_server.py" in line, "BÁO CHẶN" in line,
               "SERVER TẮT" in bind_session.inbox_line(str(root), full=False)), (True, True, True, True))
        note = dp.notify(root, "Hud", "Order đã giao", "studio", key="delivered:x", once=True)
        check("notify with the server off: the message is kept, the line says SERVER TẮT",
              ("SERVER TẮT" in note, len(dp.messages(root, "Hud"))), (True, 1))
        out, err = io.StringIO(), io.StringIO()
        dp.HERE = root / "Management" / "tools"
        with redirect_stdout(out), redirect_stderr(err):
            code = dp.main(["dispatch.py", "send", "Shop", "--from", "Manager", "Làm yêu cầu a#b"])
            code2 = dp.main(["dispatch.py", "send", "@CodeLead", "--from", "Manager", "x"])
            code3 = dp.main(["dispatch.py", "list"])
            code4 = dp.main(["dispatch.py", "bogus"])
        check("command with the server off: send keeps the message but fails with 3 and SERVER TẮT; a refused send, list, a bad command",
              (code, "SERVER TẮT" in err.getvalue(), code2, code3, "SERVER TẮT" in out.getvalue(), code4), (3, True, 1, 0, True, 2))
        check("command: send from Manager is recorded as @Manager, kept for later", 
              (dp.messages(root, "Shop")[0]["source"], dp.messages(root, "Shop")[0]["state"]), ("@Manager", "queued"))
        proc, said = server()
        try:
            out2, err2 = io.StringIO(), io.StringIO()
            with redirect_stdout(out2), redirect_stderr(err2):
                code5 = dp.main(["dispatch.py", "send", "Shop", "--from", "Manager", "Việc thứ hai"])
            line = bind_session.inbox_line(str(root))
            check("server on: courier_alive True, send succeeds, the Leaders see the counts, not SERVER TẮT",
                  (said, dp.courier_alive(root), code5, "SERVER TẮT" in line, "tin chưa giao" in line,
                   bind_session.inbox_line(str(root), full=False)), ("held", True, 0, False, True, ""))
            check("notify with the server on: says the server will deliver",
                  "server sẽ giao" in dp.notify(root, "Hud", "Việc khác", "studio", key="delivered:y", once=True))
        finally:
            proc.kill()
            proc.wait()
    finally:
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
