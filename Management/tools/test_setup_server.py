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
    finally:
        ss.run_claude, ss.threads, ss.guard.STORE_DIR = real
        shutil.rmtree(store, ignore_errors=True)
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
