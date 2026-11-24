#!/usr/bin/env python3
"""Tests for the threads part of setup_server.py: creating a thread and telling one to read the rules again.
The claude CLI is replaced by a fake and the binding store is a temp folder; no real thread is touched.

    python Management/tools/test_setup_server.py
"""
import os
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import setup_server as ss  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def thread(sid, title, binding="", archived=False, unlisted=False, model="claude-opus-x", effort="medium"):
    return {"id": sid, "title": title, "feature": ss.feature_of(title), "group": "", "legacy": False, "archived": archived,
            "unlisted": unlisted, "model": model, "effort": effort, "binding": binding, "modified": ""}


def page_cases(check):
    """Opening the page builds no data; each tab's data has its own path, built when asked for."""
    import threading
    import urllib.error
    import urllib.request
    from http.server import ThreadingHTTPServer
    built = []
    names = ("main", "write_project", "write_gamedesign", "write_studio")
    real = [getattr(ss.sync_page, n) for n in names]
    for n in names:
        setattr(ss.sync_page, n, lambda *a, n=n: built.append(n) or {"kind": n})
    server = ThreadingHTTPServer(("127.0.0.1", 0), ss.Handler)
    old = ss.Handler.token, ss.Handler.port
    ss.Handler.token, ss.Handler.port = "tok", server.server_address[1]
    threading.Thread(target=server.serve_forever, daemon=True).start()

    def get(path, token=None):
        req = urllib.request.Request(f"http://127.0.0.1:{ss.Handler.port}{path}", headers={"X-Setup-Token": token} if token else {})
        try:
            with urllib.request.urlopen(req, timeout=10) as r:
                return r.status, r.read().decode("utf-8")
        except urllib.error.HTTPError as ex:
            return ex.code, ""
    try:
        code, page = get("/")
        check("opening the page builds no data", (code, "CORESIMPLE_SETUP" in page, built), (200, True, []))
        check("the page does not load the large data files up front",
              [f for f in ("data/project.js", "data/gamedesign.js", "data/studio.js") if f'src="{f}"' in page], [])
        check("each tab's data is built when asked for, and only that one",
              [(get(f"/api/{k}", "tok")[1], list(built), built.clear())[:2] for k in ("project", "gamedesign", "studio")],
              [('{"kind": "write_project"}', ["write_project"]), ('{"kind": "write_gamedesign"}', ["write_gamedesign"]),
               ('{"kind": "write_studio"}', ["write_studio"])])
        check("the data paths need the token", (get("/api/project")[0], get("/api/project", "no")[0], built), (403, 403, []))
    finally:
        server.shutdown()
        server.server_close()
        ss.Handler.token, ss.Handler.port = old
        for n, f in zip(names, real):
            setattr(ss.sync_page, n, f)


def main():
    store = tempfile.mkdtemp(prefix="setup-store-")
    real = (ss.run_claude, ss.threads, ss.guard.STORE_DIR)
    calls = []
    try:
        ss.guard.STORE_DIR = store
        bound_at_run = []

        def fake(args):
            calls.append(args)
            if "--session-id" in args:      # was the thread bound before its first turn?
                bound_at_run.append(ss.guard.bound_feature(args[args.index("--session-id") + 1], store))
            return "Đã đọc CLAUDE.md, .claude/rules/codelead.md. " + "x" * 400

        ss.run_claude = fake
        sid = ss.create("CodeLead", "@CodeLead", "opus", "medium")
        args = calls[-1]
        check("create: bound to its role before the first turn", bound_at_run, ["@CodeLead"])
        check("create: still bound afterwards", ss.guard.bound_feature(sid, store), "@CodeLead")
        check("create: the command names the thread, its id, model and effort",
              args[:8], ["-n", "CodeLead", "--session-id", sid, "--model", "opus", "--effort", "medium"])
        check("create: the first message tells it to read the rules",
              (args[8], args[9].startswith("Thread CodeLead được tạo bởi trang Setup. Đọc lại quy trình"), ss.REREAD_PROMPT in args[9]),
              ("-p", True, True))

        def failing(args):
            calls.append(args)
            raise RuntimeError("claude: not logged in")

        ss.run_claude = failing
        try:
            ss.create("Wallet", "Wallet", "opus", "medium")
            raised = False
        except RuntimeError:
            raised = True
        failed_sid = calls[-1][calls[-1].index("--session-id") + 1]
        check("create: a failed run raises and leaves no binding",
              (raised, ss.guard.bound_feature(failed_sid, store) or "", os.path.exists(ss.guard.binding_path(failed_sid, store))),
              (True, "", False))

        listed = [thread("aaaa1111", "CodeLead", binding="@CodeLead"), thread("bbbb2222", "Wallet", binding="Wallet", model="", effort=""),
                  thread("cccc3333", "Shop"), thread("dddd4444", "Hero", binding="Hero", archived=True),
                  thread("eeee5555", "Wave", binding="Wave", unlisted=True), thread("ffff6666", "Hud", binding="Hud", effort="turbo")]
        ss.threads = lambda: listed
        ss.run_claude = fake
        before = len(calls)
        for sid_, why in ((None, "không có thread này"), ("nope", "không có thread này"), (12, "không có thread này"),
                          ("cccc3333", "thread chưa gán FEATURE: chạy Setup trước"), ("dddd4444", "thread đã archive"),
                          ("eeee5555", "VS Code không hiện thread này")):
            res = ss.reread(sid_)
            check(f"reread refused: {sid_}", (res["ok"], res["error"]), (False, why))
        check("reread: a refused thread runs nothing", len(calls), before)
        res = ss.reread("aaaa1111")
        check("reread: resumes that thread with its model and effort, and the prompt",
              calls[-1], ["--resume", "aaaa1111", "--model", "claude-opus-x", "--effort", "medium", "-p", ss.REREAD_PROMPT])
        check("reread: answers with the start of what the thread said",
              (res["ok"], res["id"], res["title"], len(res["answer"]), res["answer"].startswith("Đã đọc CLAUDE.md")),
              (True, "aaaa1111", "CodeLead", 300, True))
        ss.reread("bbbb2222")
        check("reread: no model or effort known: the CLI's own defaults", calls[-1], ["--resume", "bbbb2222", "-p", ss.REREAD_PROMPT])
        ss.reread("ffff6666")
        check("reread: an effort the CLI does not know is left out",
              calls[-1], ["--resume", "ffff6666", "--model", "claude-opus-x", "-p", ss.REREAD_PROMPT])
        ss.run_claude = failing
        res = ss.reread("aaaa1111")
        check("reread: a failed run is reported, not hidden", (res["ok"], res["error"]), (False, "claude: not logged in"))
        check("reread binds nothing", ss.guard.bound_feature("aaaa1111", store) or "", "")
        check("the page may post to the reread path", "/api/sessions/reread" in ss.POST_PATHS)
        # compact: one headless turn that runs /compact; nothing is bound
        ss.run_claude = fake
        before = len(calls)
        for sid_, why in (("nope", "không có thread này"), ("cccc3333", "thread chưa gán FEATURE: chạy Setup trước"),
                          ("dddd4444", "thread đã archive"), ("eeee5555", "VS Code không hiện thread này")):
            res = ss.compact(sid_)
            check(f"compact refused: {sid_}", (res["ok"], res["error"]), (False, why))
        check("compact: a refused thread runs nothing", len(calls), before)
        res = ss.compact("aaaa1111")
        check("compact: runs /compact in that thread, on its model and effort",
              (res["ok"], calls[-1]), (True, ["--resume", "aaaa1111", "--model", "claude-opus-x", "--effort", "medium", "-p", "/compact"]))
        ss.run_claude = failing
        res = ss.compact("aaaa1111")
        check("compact: a failed run is reported", (res["ok"], res["error"]), (False, "claude: not logged in"))
        check("compact binds nothing", ss.guard.bound_feature("aaaa1111", store) or "", "")
        # renew: with the PIN; the context is emptied in the same chat, the thread is bound again, then reads the rules
        real_pin = ss.bind_session.check_pin
        ss.bind_session.check_pin = lambda store_, pin: "" if pin == "1234" else "PIN sai"
        try:
            ss.run_claude = fake
            before = len(calls)
            for pin in ("", "0000", None, 1234):
                res = ss.renew("aaaa1111", pin)
                check(f"renew: a wrong PIN is refused ({pin!r})", (res["ok"], res["error"], res.get("pin")), (False, "PIN sai", True))
            check("renew: a wrong PIN runs nothing and binds nothing",
                  (len(calls), ss.guard.bound_feature("aaaa1111", store) or ""), (before, ""))
            res = ss.renew("cccc3333", "1234")
            check("renew: a thread that is not bound is refused before the PIN is looked at",
                  (res["ok"], res["error"], len(calls)), (False, "thread chưa gán FEATURE: chạy Setup trước", before))
            res = ss.renew("aaaa1111", "1234")
            first, second = calls[-2], calls[-1]
            check("renew: first the context is emptied in the same thread (a /compact told to keep nothing, not /clear)",
                  (first[:2], first[-2], first[-1] == ss.WIPE_PROMPT, first[-1].startswith("/compact "), "KHÔNG giữ lại" in first[-1]),
                  (["--resume", "aaaa1111"], "-p", True, True, True))
            check("renew: then it is told it was renewed and to read the rules",
                  (second[:2], second[-1].startswith("Thread CodeLead vừa được làm mới"), ss.REREAD_PROMPT in second[-1], len(calls) - before),
                  (["--resume", "aaaa1111"], True, True, 2))
            check("renew: it is bound again to the FEATURE it had", (res["ok"], ss.guard.bound_feature("aaaa1111", store)), (True, "@CodeLead"))
            os.remove(ss.guard.binding_path("aaaa1111", store))
            ss.run_claude = failing
            res = ss.renew("aaaa1111", "1234")
            check("renew: when emptying fails nothing is bound", (res["ok"], res["error"].startswith("chưa làm mới được"),
                                                                  ss.guard.bound_feature("aaaa1111", store) or ""), (False, True, ""))
            turns = []

            def second_fails(args):
                turns.append(args)
                if len(turns) == 2:
                    raise RuntimeError("claude: timeout")
                return ""

            ss.run_claude = second_fails
            res = ss.renew("aaaa1111", "1234")
            check("renew: emptied and bound but the rules not read: said so, the binding stays",
                  (res["ok"], res.get("renewed"), "CHƯA đọc quy trình" in res["error"], ss.guard.bound_feature("aaaa1111", store)),
                  (False, True, True, "@CodeLead"))
        finally:
            ss.bind_session.check_pin = real_pin
        check("the page may post to the compact and renew paths",
              ("/api/sessions/compact" in ss.POST_PATHS, "/api/sessions/renew" in ss.POST_PATHS), (True, True))
    finally:
        ss.run_claude, ss.threads, ss.guard.STORE_DIR = real
        shutil.rmtree(store, ignore_errors=True)
    page_cases(check)
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
