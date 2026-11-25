#!/usr/bin/env python3
"""Tests for the threads part of setup_server.py: creating a thread and telling one to read the rules again.
The claude CLI is replaced by a fake and the binding store is a temp folder; no real thread is touched.

    python Management/tools/test_setup_server.py
"""
import os
import shutil
import sys
import tempfile
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import setup_server as ss  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def RP(binding):
    """What 'Đọc lại quy trình' tells a thread that has not read its rules yet: every rule file of its role."""
    return ss.reread_prompt(ss.bind_session.rule_files(binding, str(ss.ROOT)))


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


def job_cases(check):
    """Setup runs in the background: each name shows waiting, running, then bound, created or error."""
    import threading
    real = (ss.run_claude, ss.threads, ss.sets, ss.bind_session.check_pin, ss.guard.STORE_DIR, ss.JOB, ss.start_bg)
    store = tempfile.mkdtemp(prefix="setup-job-")
    went_live = []
    gate = threading.Event()
    try:
        ss.guard.STORE_DIR = store
        ss.JOB = None
        ss.bind_session.check_pin = lambda store_, pin: "" if pin == "1234" else "PIN sai"
        ss.sets = lambda: {"basic": ["@human", "@CodeLead", "Wallet"], "full": []}
        ss.threads = lambda: [thread("aaaa1111", "human", binding="@human")]

        def slow(args):
            gate.wait(5)
            if args[1] == "Wallet":
                raise RuntimeError("claude: not logged in")
            return "ok"

        ss.run_claude = slow
        ss.start_bg = lambda sid, extra, prompt: went_live.append((sid, extra, prompt))
        res = ss.setup_start("0000", "basic")
        check("job: a wrong PIN starts nothing", (res["ok"], res["error"], ss.JOB), (False, "PIN sai", None))
        res = ss.setup_start("1234", "basic")
        check("job: answers at once with every name waiting or running",
              (res["ok"], [i["name"] for i in res["job"]["items"]], res["job"]["done"]), (True, ["@human", "@CodeLead", "Wallet"], False))
        for _ in range(100):
            states = [i["state"] for i in ss.job_view()["items"]]
            if states[1] == "running":
                break
            time.sleep(0.02)
        check("job: an existing thread is only bound, the next one runs, the rest waits", states, ["bound", "running", "waiting"])
        again = ss.setup_start("1234", "basic")
        check("job: a second setup is refused while one runs", (again["ok"], again.get("running")), (False, True))
        gate.set()
        for _ in range(250):
            if ss.job_view()["done"]:
                break
            time.sleep(0.02)
        view = ss.job_view()
        check("job: ends with created and error, the error kept",
              (view["done"], [i["state"] for i in view["items"]], view["items"][2]["detail"]),
              (True, ["bound", "created", "error"], "claude: not logged in"))
        check("job: a new thread is then run on in the background (live), on the model and effort picked",
              (view["items"][1]["live"], went_live, view["items"][1]["detail"]),
              (True, [(view["items"][1]["ids"][0], ["-n", "CodeLead", "--model", "opus", "--effort", "medium"], ss.LIVE_PROMPT)], ""))
        check("job: the page reads it from GET /api/setup/job", "/api/setup" in ss.POST_PATHS)
        ss.run_claude = lambda args: "ok"
        res = ss.setup("1234", "basic")
        check("setup(): the whole run in one call, results as before",
              [(r["name"], r["action"]) for r in res["results"]], [("@human", "bound"), ("@CodeLead", "created"), ("Wallet", "created")])
    finally:
        gate.set()
        (ss.run_claude, ss.threads, ss.sets, ss.bind_session.check_pin, ss.guard.STORE_DIR, ss.JOB, ss.start_bg) = real
        shutil.rmtree(store, ignore_errors=True)


def live_cases(check):
    """Live: a thread runs on as a background session (claude --bg --resume), so ListAgents shows it."""
    real = (ss.run_claude, ss.threads, ss.live_sessions, ss.LIVE_SECONDS, ss.time.sleep, ss.transcripts_dir, ss.guard.STORE_DIR)
    ss.guard.STORE_DIR = tempfile.mkdtemp(prefix="live-store-")      # what a reread writes stays out of the real store
    live, calls = {}, []
    tdir = Path(tempfile.mkdtemp(prefix="livefx-"))
    (tdir / "99999999-aaaa.jsonl").write_text("{}\n", encoding="utf-8")      # the copy the CLI makes for copy0000
    ss.transcripts_dir = lambda: tdir

    flaky = []

    def fake(args):
        calls.append(args)
        if args[0] == "--bg":
            sid = args[2]
            if not sid.startswith("dead"):      # a thread whose background session never shows up
                live[sid] = {"kind": "background", "status": "busy", "short": sid[:8]}
            if sid.startswith("flak") and not flaky:      # the first try overlaps another job change: a copy
                flaky.append(sid)
                return "Starting background service…\nbackgrounded · 88888888 · x"
            return f"Starting background service…\nbackgrounded · {('99999999' if sid.startswith('copy') else sid[:8])} · x"
        if "--no-session-persistence" in args:      # the helper that relays a message into a chat open in VS Code
            return "ĐÃ GỬI"
        if args[0] == "stop":
            for k in [k for k, v in live.items() if v["short"] == args[1]]:
                del live[k]
            return "stopped " + args[1]
        return "Đã đọc CLAUDE.md"

    def listed():
        out = [thread("aaaa1111", "CodeLead", binding="@CodeLead"), thread("bbbb2222", "Wallet", binding="Wallet"),
               thread("cccc3333", "Shop"), thread("dead0000", "Hud", binding="Hud"), thread("copy0000", "Wave", binding="Wave"),
               thread("flak0000", "Hero", binding="Hero"),
               thread("eeee5555", "human", binding="@human"), thread("ffff6666", "human", binding="@human")]
        for t in out:
            t["live"] = (live.get(t["id"]) or {}).get("kind", "")
        return out
    try:
        ss.run_claude, ss.threads, ss.live_sessions = fake, listed, lambda fresh=False: dict(live)
        ss.LIVE_SECONDS, ss.time.sleep = 0, lambda s: None
        live["eeee5555"] = {"kind": "interactive", "status": "busy", "short": "eeee5555"}
        res = ss.go_live("cccc3333")
        check("live: a thread that is not bound is refused", (res["ok"], calls), (False, []))
        res = ss.go_live("eeee5555")
        check("live: a chat open in VS Code is left as it is", (res["ok"], "live sẵn" in res["answer"], calls), (True, True, []))
        res = ss.reread("eeee5555")
        check("reread: a chat open in VS Code gets the prompt as a message (a relay helper calls SendMessage), nothing else runs",
              (res["ok"], len(calls), calls[0][:4], 'to="human"' in calls[0][-1], RP("@human") in calls[0][-1] and "KHÔNG trả lời" in calls[0][-1], "eeee5555" in live),
              (True, 1, ["-p", "--model", ss.RELAY_MODEL, "--no-session-persistence"], True, True, True))
        calls.clear()
        for name, fn in (("compact", lambda: ss.compact("eeee5555")), ("renew", lambda: ss.renew("eeee5555", "sai-pin"))):
            res = fn()
            check(f"{name}: a chat open in VS Code is refused before anything runs (its process would not see the turn)",
                  (res["ok"], res.get("open"), "mở trong VS Code" in res.get("error", ""), res.get("pin"), calls), (False, True, True, None, []))
        res = ss.go_live("aaaa1111")
        check("live: runs the thread on in the background under its id and its name (else the CLI makes one up), with its model and effort",
              (res["ok"], calls[-1], live["aaaa1111"]["kind"]),
              (True, ["--bg", "--resume", "aaaa1111", "-n", "CodeLead", "--model", "claude-opus-x", "--effort", "medium", ss.LIVE_PROMPT], "background"))
        check("live: an ended job of the thread is removed first (else the CLI starts a copy)", calls[-2], ["rm", "aaaa1111"])
        before = len(calls)
        check("live: pressing it again starts nothing", (ss.go_live("aaaa1111")["ok"], len(calls)), (True, before))
        res = ss.reread("aaaa1111")
        check("live: reread on a background thread stops it, then runs the prompt as a background session again (no -p)",
              (res["ok"], calls[before][0], calls[-1][:3], calls[-1][-1], "-p" in calls[-1], "aaaa1111" in live),
              (True, "stop", ["--bg", "--resume", "aaaa1111"], RP("@CodeLead"), False, True))
        check("live: between stop and resume, the stopped job is removed",
              [c for c in calls[before + 1:-1]], [["rm", "aaaa1111"], ["rm", "aaaa1111"]])
        res = ss.reread("bbbb2222")
        check("live: reread on a thread that is not live stays a headless turn", (res["ok"], calls[-1][-2], "bbbb2222" in live), (True, "-p", False))
        res = ss.go_live("dead0000")
        check("live: a background session that never shows up is reported", (res["ok"], "chưa thấy phiên nền" in res["error"]), (False, True))
        res = ss.go_live("copy0000")
        check("live: a copy under another id is reported", (res["ok"], "bản sao" in res["error"]), (False, True))
        check("live: and the copy is stopped, its job removed, its transcript moved to the trash",
              (calls[-2:], (tdir / "99999999-aaaa.jsonl").exists(), len(list((tdir / ss.TRASH).glob("*-copy/99999999-aaaa.jsonl")))),
              ([["stop", "99999999"], ["rm", "99999999"]], False, 1))
        res = ss.go_live("flak0000")
        check("live: a copy on the first try is dropped and the thread itself runs on the second",
              (res["ok"], [c[:3] for c in calls if c[0] == "--bg" and c[2] == "flak0000"].__len__(), "flak0000" in live,
               ["stop", "88888888"] in calls), (True, 2, True, True))
        before = len(calls)
        res = ss.stop_live("eeee5555")
        check("stop: a chat open in VS Code is not touched", (res["ok"], len(calls), "eeee5555" in live), (True, before, True))
        res = ss.stop_live("aaaa1111")
        check("stop: ends the background session and removes its job", (res["ok"], calls[-2:], "aaaa1111" in live),
              (True, [["stop", "aaaa1111"], ["rm", "aaaa1111"]], False))
        check("stop: a thread with no background session runs nothing", (ss.stop_live("aaaa1111")["ok"], len(calls)), (True, before + 2))
        before = len(calls)
        res = ss.go_live("ffff6666")
        check("live: the human's own chat is never run in the background (VS Code could not open it)",
              (res["ok"], "thread của người" in res["error"], len(calls), "ffff6666" in live), (False, True, before, False))
        real_pin = ss.bind_session.check_pin
        ss.bind_session.check_pin = lambda store_, pin: ""
        try:
            res = ss.renew("ffff6666", "1234")
        finally:
            ss.bind_session.check_pin = real_pin
        check("live: renewing the human's chat keeps its last turn headless", (calls[-1][0], calls[-1][-2], "ffff6666" in live), ("--resume", "-p", False))
        # compact and renew on a thread that runs in the background: stop, remove the ended job, resume under the same id
        live["aaaa1111"] = {"kind": "background", "status": "idle", "short": "aaaa1111"}
        before = len(calls)
        res = ss.compact("aaaa1111")
        check("compact on a background thread: stop, rm, then /compact as a background turn under its id",
              (res["ok"], [c[0] for c in calls[before:]], calls[-1][:3], calls[-1][-1], "aaaa1111" in live),
              (True, ["stop", "rm", "rm", "--bg"], ["--bg", "--resume", "aaaa1111"], ss.COMPACT_PROMPT, True))
        before = len(calls)
        ss.bind_session.check_pin = lambda store_, pin: ""
        try:
            res = ss.renew("aaaa1111", "1234")
        finally:
            ss.bind_session.check_pin = real_pin
        check("renew on a background thread: stop, rm, the wipe headless, then rm and back live under its id",
              (res["ok"], [c[0] if c[0] != "--resume" else c[-2] for c in calls[before:]], calls[-1][:3], "aaaa1111" in live),
              (True, ["stop", "rm", "-p", "rm", "--bg"], ["--bg", "--resume", "aaaa1111"], True))
        check("renew: no copy was made on the way", any(c[0] == "--bg" and not c[2].startswith("aaaa1111") for c in calls[before:]), False)
        check("the page may post to the live and stop paths",
              ("/api/sessions/live" in ss.POST_PATHS, "/api/sessions/stop" in ss.POST_PATHS), (True, True))
    finally:
        shutil.rmtree(ss.guard.STORE_DIR, ignore_errors=True)
        (ss.run_claude, ss.threads, ss.live_sessions, ss.LIVE_SECONDS, ss.time.sleep, ss.transcripts_dir, ss.guard.STORE_DIR) = real
        shutil.rmtree(tdir, ignore_errors=True)


def delete_cases(check):
    """Xoá: to the trash, with the PIN, only while VS Code is closed and the thread is not running a turn."""
    import json
    import sqlite3
    real = (ss.threads, ss.transcripts_dir, ss.bind_session.check_pin, ss.guard.STORE_DIR, ss.JOB,
            ss.vscode_groups.vscode_running, ss.vscode_groups.vscode_state, ss.live_sessions, ss.run_claude)
    tmp = Path(tempfile.mkdtemp(prefix="setup-del-"))
    tries = []
    try:
        folder, store = tmp / "projects", tmp / "store"
        folder.mkdir()
        (folder / "aaaa1111.jsonl").write_text("{}\n", encoding="utf-8")
        (folder / "aaaa1111").mkdir()
        (folder / "aaaa1111" / "sub.txt").write_text("x", encoding="utf-8")
        (folder / "bbbb2222.jsonl").write_text("{}\n", encoding="utf-8")
        db = tmp / "state.vscdb"
        con = sqlite3.connect(db)
        with con:
            con.execute("create table ItemTable (key text, value blob)")
            con.execute("insert into ItemTable values (?, ?)", (ss.vscode_groups.STATE_KEY, json.dumps({
                "hiddenSessionIds": ["aaaa1111", "zz"],
                "sessionGroups:/p": [{"id": "g", "name": "7 · Features", "collapsed": False, "sessionIds": ["aaaa1111", "bbbb2222"]}]})))
        con.close()
        ss.guard.STORE_DIR = str(store)
        ss.JOB = None
        live = {"aaaa1111": {"kind": "background", "status": "idle", "short": "aaaa1111"}}
        stopped = []
        ss.live_sessions = lambda fresh=False: dict(live)
        ss.run_claude = lambda args: (stopped.append(args), live.clear(), "stopped")[2]
        ss.transcripts_dir = lambda: folder
        ss.vscode_groups.vscode_state = lambda: db
        ss.bind_session.check_pin = lambda store_, pin: tries.append(pin) or ("" if pin == "1234" else "PIN sai")
        ss.threads = lambda: [thread("aaaa1111", "Wallet", binding="Wallet", archived=True), thread("bbbb2222", "Shop", binding="Shop")]
        ss.bind("aaaa1111", "Wallet")
        ss.vscode_groups.vscode_running = lambda: True
        res = ss.delete_thread("aaaa1111", "1234")
        check("delete: refused while VS Code runs, before the PIN is looked at",
              (res["ok"], res.get("vscode"), tries, (folder / "aaaa1111.jsonl").exists()), (False, True, [], True))
        ss.vscode_groups.vscode_running = lambda: False
        res = ss.delete_thread("aaaa1111", "0000")
        check("delete: a wrong PIN is refused, nothing moved", (res["ok"], res.get("pin"), (folder / "aaaa1111.jsonl").exists()), (False, True, True))
        check("delete: an unknown thread is refused", ss.delete_thread("nope", "1234")["error"], "không có thread này")
        ss.busy("aaaa1111", True)
        res = ss.delete_thread("aaaa1111", "1234")
        ss.busy("aaaa1111", False)
        check("delete: refused while the thread runs a turn from the page", (res["ok"], "đang chạy" in res["error"]), (False, True))
        ss.JOB = {"done": False, "items": []}
        res = ss.delete_thread("aaaa1111", "1234")
        check("delete: refused while a setup runs", (res["ok"], res["error"].startswith("Setup đang chạy")), (False, True))
        ss.JOB = None
        res = ss.delete_thread("aaaa1111", "1234")
        trash = Path(res.get("trash", tmp / "none"))
        check("delete: an archived thread is moved into the trash, with its folder",
              (res["ok"], trash.parent == folder / ss.TRASH, (trash / "aaaa1111.jsonl").exists(), (trash / "aaaa1111" / "sub.txt").exists(),
               (folder / "aaaa1111.jsonl").exists(), (folder / "aaaa1111").exists()), (True, True, True, True, False, False))
        info = json.loads((trash / "info.json").read_text(encoding="utf-8"))
        check("delete: the binding is kept in the trash and removed from the store",
              ((trash / "binding.json").exists(), ss.guard.bound_feature("aaaa1111", str(store)), info["feature"], info["vscode_groups"]),
              (True, None, "Wallet", ["7 · Features"]))
        con = sqlite3.connect(db)
        state = json.loads(con.execute("select value from ItemTable").fetchone()[0])
        con.close()
        check("delete: its id is out of the VS Code state, the others stay",
              (state["hiddenSessionIds"], state["sessionGroups:/p"][0]["sessionIds"]), (["zz"], ["bbbb2222"]))
        check("delete: its background session was stopped first, and its job removed", stopped, [["stop", "aaaa1111"], ["rm", "aaaa1111"]])
        check("delete: the other thread is untouched", (folder / "bbbb2222.jsonl").exists())
        check("the page may post to the delete path", "/api/sessions/delete" in ss.POST_PATHS)
    finally:
        (ss.threads, ss.transcripts_dir, ss.bind_session.check_pin, ss.guard.STORE_DIR, ss.JOB,
         ss.vscode_groups.vscode_running, ss.vscode_groups.vscode_state, ss.live_sessions, ss.run_claude) = real
        shutil.rmtree(tmp, ignore_errors=True)


def main():
    store = tempfile.mkdtemp(prefix="setup-store-")

    def forget():      # no thread has read the rules yet: a reread names all of its role's files
        shutil.rmtree(os.path.join(store, ss.RULES_SEEN_DIR), ignore_errors=True)

    real = (ss.run_claude, ss.threads, ss.guard.STORE_DIR, ss.start_bg)
    calls = []
    try:
        ss.guard.STORE_DIR = store
        ss.start_bg = lambda sid, extra, prompt: ss.run_claude(["--bg", "--resume", sid] + extra + [prompt])
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
        forget()
        res = ss.reread("aaaa1111")
        check("reread: resumes that thread with its model and effort, and the prompt naming the rule files of its role",
              calls[-1], ["--resume", "aaaa1111", "--model", "claude-opus-x", "--effort", "medium", "-p", RP("@CodeLead")])
        check("reread: a thread not told before is told every file of its role, not those of other roles",
              ss.bind_session.rule_files("@CodeLead", str(ss.ROOT)),
              ["CLAUDE.md", ".claude/rules/report.md", ".claude/rules/codelead.md", ".claude/rules/unity.md"])
        check("reread: answers with the start of what the thread said",
              (res["ok"], res["id"], res["title"], len(res["answer"]), res["answer"].startswith("Đã đọc CLAUDE.md")),
              (True, "aaaa1111", "CodeLead", 300, True))
        before = len(calls)
        res = ss.reread("aaaa1111")
        check("reread: nothing changed since the thread last read its rules: no turn at all",
              (res["ok"], res.get("skipped"), len(calls) - before), (True, True, 0))
        seen = os.path.join(store, ss.RULES_SEEN_DIR, "aaaa1111.json")
        state = ss.bind_session.read_json(seen, {})
        state[".claude/rules/codelead.md"] = "old"
        ss.bind_session.write_json(seen, state)
        ss.reread("aaaa1111")
        check("reread: one rule file changed: only that file is named",
              calls[-1][-1], ss.reread_prompt([".claude/rules/codelead.md"]))
        ss.reread("bbbb2222")
        check("reread: no model or effort known: the CLI's own defaults", calls[-1], ["--resume", "bbbb2222", "-p", RP("Wallet")])
        ss.reread("ffff6666")
        check("reread: an effort the CLI does not know is left out",
              calls[-1], ["--resume", "ffff6666", "--model", "claude-opus-x", "-p", RP("Hud")])
        ss.run_claude = failing
        forget()
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
            check("renew: then it is told it was renewed and to read the rules, as a background session (live)",
                  (second[:3], second[-1].startswith("Thread CodeLead vừa được làm mới"), ss.REREAD_PROMPT in second[-1], len(calls) - before),
                  (["--bg", "--resume", "aaaa1111"], True, True, 2))
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
        ss.run_claude, ss.threads, ss.guard.STORE_DIR, ss.start_bg = real
        shutil.rmtree(store, ignore_errors=True)
    seen = {}
    real_run = ss.subprocess.run
    try:
        ss.subprocess.run = lambda cmd, **kw: (seen.update(kw.get("env") or {}),
                                               ss.subprocess.CompletedProcess(cmd, 0, "ok", ""))[1]
        ss.run_claude(["--bg", "--resume", "x"])
    finally:
        ss.subprocess.run = real_run
    check("every claude run (a --bg thread too) edits the shared checkout: no worktree isolation",
          seen.get("CLAUDE_BG_ISOLATION"), "none")
    page_cases(check)
    job_cases(check)
    live_cases(check)
    delete_cases(check)
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
