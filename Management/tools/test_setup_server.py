#!/usr/bin/env python3
"""Tests for the threads part of setup_server.py: creating a thread and telling one to read the rules again.
The claude CLI is replaced by a fake and the binding store is a temp folder; no real thread is touched.

    python Management/tools/test_setup_server.py
"""
import json
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
    return {"id": sid, "title": title, "named": True, "feature": ss.feature_of(title), "group": "", "legacy": False, "archived": archived,
            "unlisted": unlisted, "model": model, "effort": effort, "binding": binding, "modified": ""}


def page_cases(check):
    """Opening the page builds no data; each tab's data has its own path, built when asked for."""
    import threading
    import urllib.error
    import urllib.request
    from http.server import ThreadingHTTPServer
    built = []
    names = ("main", "write_project", "write_gamedesign", "write_studio", "write_references")
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
        check("the references tab's data is built when asked for", (get("/api/references", "tok")[1], built), ('{"kind": "write_references"}', ["write_references"]))
        built.clear()

        def post(path, body, token="tok"):
            raw = json.dumps(body).encode("utf-8")
            req = urllib.request.Request(f"http://127.0.0.1:{ss.Handler.port}{path}", data=raw, method="POST",
                                         headers={"X-Setup-Token": token, "Content-Type": "application/json"})
            try:
                with urllib.request.urlopen(req, timeout=10) as r:
                    return r.status, json.loads(r.read().decode("utf-8"))
            except urllib.error.HTTPError as ex:
                return ex.code, None
        import base64
        import shutil
        import tempfile
        tmp, real_root = Path(tempfile.mkdtemp(prefix="refsrv-")), ss.ROOT
        ss.ROOT = tmp
        try:
            png = base64.b64encode(b"\x89PNG\r\n\x1a\n" + b"\0" * 16).decode()
            code, res = post("/api/references/upload", {"name": "hud.png", "data": png, "note": "nút to", "kind": "bố cục", "tags": ["HUD"]})
            files = sorted(x.name for x in (tmp / "Management" / "references").iterdir())
            check("references: upload stores the picture and the index, and answers with fresh data",
                  (code, res["ok"], res["data"], len(files), "index.json" in files), (200, True, {"kind": "write_references"}, 2, True))
            rid = [x for x in files if x.endswith(".png")][0][:-4]
            check("references: a note is saved", post("/api/references/note", {"id": rid, "note": "đổi", "kind": "style art", "tags": []})[1]["ok"])
            check("references: a bad upload answers why", post("/api/references/upload", {"name": "a.gif", "data": base64.b64encode(b"GIF89a").decode()})[1],
                  {"ok": False, "error": "chỉ nhận ảnh png, jpg hoặc webp"})
            check("references: an unknown action is refused", post("/api/references/rename", {"id": rid})[0], 403)
            calls, real_user_idea = [], ss.gamedesign.user_idea
            ss.gamedesign.user_idea = lambda *a: calls.append(a[1:]) or "user-1"
            try:
                check("user-idea: the page's Thêm ý reaches gamedesign.user_idea and answers with fresh data",
                      (post("/api/gamedesign/user-idea", {"path": "feature-hero", "action": "add", "title": "Ý", "detail": ""})[1], calls),
                      ({"ok": True, "id": "user-1", "data": {"kind": "write_gamedesign"}}, [("feature-hero", "add", None, "Ý", "")]))
                check("user-idea: needs the token", post("/api/gamedesign/user-idea", {"path": "", "action": "add"}, token="no")[0], 403)
                picks, real_pick = [], ss.gamedesign.gpt_pick
                ss.gamedesign.gpt_pick = lambda *a: picks.append(a[1:]) or "enemy-blocker"
                try:
                    check("gpt-pick: Chuyển lên reaches gamedesign.gpt_pick without a PIN and answers with fresh data",
                          (post("/api/gamedesign/gpt-pick", {"folder": "feature-hero", "aid": "q1", "id": "enemy-blocker",
                                                             "action": "promote"})[1], picks),
                          ({"ok": True, "id": "enemy-blocker", "data": {"kind": "write_gamedesign"}},
                           [("feature-hero", "q1", "enemy-blocker", "promote")]))
                    check("gpt-pick: needs the token", post("/api/gamedesign/gpt-pick", {"folder": "x"}, token="no")[0], 403)
                    ss.gamedesign.gpt_pick = lambda *a: (_ for _ in ()).throw(ValueError("ý này đã được xoá"))
                    check("gpt-pick: a refusal comes back as an error", post("/api/gamedesign/gpt-pick", {"folder": "x"})[1],
                          {"ok": False, "error": "ý này đã được xoá"})
                finally:
                    ss.gamedesign.gpt_pick = real_pick
                dels, real_delete = [], ss.gamedesign.human_delete
                ss.gamedesign.human_delete = lambda *a: dels.append(a[1:]) or {"ideas": 1, "nodes": 1}
                try:
                    check("delete: the floating Save reaches gamedesign.human_delete",
                          (post("/api/gamedesign/delete", {"ideas": [{"path": "", "id": "x"}], "nodes": ["feature-a"]})[1], dels),
                          ({"ok": True, "ideas": 1, "nodes": 1, "data": {"kind": "write_gamedesign"}}, [([{"path": "", "id": "x"}], ["feature-a"])]))
                finally:
                    ss.gamedesign.human_delete = real_delete
                calls2, real_approve, real_passing = [], ss.gamedesign.approve, ss.gamedesign.approve_passing
                ss.gamedesign.approve = lambda *a: calls2.append(("approve",) + a[1:]) or {"approved": ["feature-a"], "build": {"held": []}}
                ss.gamedesign.approve_passing = lambda *a: calls2.append(("sync",)) or {"approved": [], "build": {"held": ["x/data.md"]}}
                try:
                    check("promote: Chốt approves (no PIN) and answers with fresh data",
                          post("/api/gamedesign/promote", {"path": "feature-a", "deep": True})[1],
                          {"ok": True, "approved": ["feature-a"], "held": [], "data": {"kind": "write_gamedesign"}})
                    check("sync: no PIN any more; held files are said",
                          post("/api/gamedesign/sync", {})[1], {"ok": True, "approved": [], "held": ["x/data.md"], "data": {"kind": "write_gamedesign"}})
                    check("promote and sync reach gamedesign", calls2, [("approve", "feature-a", True), ("sync",)])
                finally:
                    ss.gamedesign.approve, ss.gamedesign.approve_passing = real_approve, real_passing
            finally:
                ss.gamedesign.user_idea = real_user_idea
            check("references: the paths need the token", post("/api/references/delete", {"id": rid}, token="no")[0], 403)
            check("references: delete", (post("/api/references/delete", {"id": rid})[1]["ok"],
                                         [x.name for x in (tmp / "Management" / "references").iterdir()]), (True, ["index.json"]))
        finally:
            ss.ROOT = real_root
            shutil.rmtree(tmp, ignore_errors=True)
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
        ss.threads = lambda include_unnamed=False: [thread("aaaa1111", "human", binding="@human")]

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

    def listed(include_unnamed=False):
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
              (True, ["--bg", "--resume", "aaaa1111", "-n", "CodeLead", "--model", "claude-opus-x", "--effort", "medium", "--", ss.LIVE_PROMPT], "background"))
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
              (res["ok"], "chat người dùng trực tiếp" in res["error"], len(calls), "ffff6666" in live), (False, True, before, False))
        check("live: @Manager is a chat the human talks to in VS Code too (người quyết 2026-11-27)",
              (ss.human_thread({"binding": "@Manager"}), ss.human_thread({"binding": "@CodeLead"})), (True, False))
        real_pin = ss.bind_session.check_pin
        ss.bind_session.check_pin = lambda store_, pin: ""
        try:
            res = ss.renew("ffff6666", "1234")
        finally:
            ss.bind_session.check_pin = real_pin
        check("live: renewing the human's chat runs no turn and leaves it not live", (res["ok"], len(calls), "ffff6666" in live), (True, before, False))
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
        check("renew on a background thread: stopped and its job removed, no turn, not started again (người quyết 2026-11-26)",
              (res["ok"], [c[0] for c in calls[before:]], "aaaa1111" in live), (True, ["stop", "rm"], False))
        check("the page may post to the live and stop paths",
              ("/api/sessions/live" in ss.POST_PATHS, "/api/sessions/stop" in ss.POST_PATHS), (True, True))
    finally:
        shutil.rmtree(ss.guard.STORE_DIR, ignore_errors=True)
        (ss.run_claude, ss.threads, ss.live_sessions, ss.LIVE_SECONDS, ss.time.sleep, ss.transcripts_dir, ss.guard.STORE_DIR) = real
        shutil.rmtree(tdir, ignore_errors=True)


def usage_cases(check):
    """thread_usage: tokens a thread spent since its last renew, from its transcript and its subagents'."""
    real = ss.transcripts_dir
    tmp = Path(tempfile.mkdtemp(prefix="setup-usage-"))

    def answer(mid, model="claude-opus-x", **u):
        return json.dumps({"type": "assistant", "message": {"id": mid, "role": "assistant", "model": model, "usage": u}})

    try:
        ss.transcripts_dir = lambda: tmp
        user = json.dumps({"type": "user", "timestamp": "2026-11-20T03:00:00.000Z",
                           "message": {"role": "user", "content": ss.RENEW_NOTE}}, ensure_ascii=False)      # spaces, as a renew writes it
        lines = [user, answer("msg_renew_x", ss.SYNTHETIC_MODEL, input_tokens=0, output_tokens=0),
                 answer("m1", input_tokens=10, output_tokens=100, cache_read_input_tokens=1000, cache_creation_input_tokens=400,
                        cache_creation={"ephemeral_1h_input_tokens": 300, "ephemeral_5m_input_tokens": 100}),
                 answer("m1", input_tokens=10, output_tokens=100, cache_read_input_tokens=1000, cache_creation_input_tokens=400,
                        cache_creation={"ephemeral_1h_input_tokens": 300, "ephemeral_5m_input_tokens": 100})]
        main = tmp / "sid1.jsonl"
        main.write_text("\n".join(lines) + "\n", encoding="utf-8")
        (tmp / ss.RENEWED / "20261126-175006-sid1").mkdir(parents=True)
        u = ss.thread_usage("sid1")
        check("usage: an answer of several lines counts once, the renew answer not at all",
              (u["calls"], u["input"], u["output"], u["cache_read"], u["cache_write"]), (1, 10, 100, 1000, 400))
        check("usage: cost in input-price tokens (cache write 1h x2, 5m x1.25, read x0.1, output x5)",
              u["cost"], round(10 + 300 * 2 + 100 * 1.25 + 1000 * 0.1 + 100 * 5))
        check("usage: counted from the renew, at the time in the name of its folder", (u["renewed"], u["since"]), (True, "11-26 17:50"))
        sub = tmp / "sid1" / "subagents"
        sub.mkdir(parents=True)
        (sub / "agent-a.jsonl").write_text(answer("s1", input_tokens=5, output_tokens=2, cache_creation_input_tokens=8) + "\n", encoding="utf-8")
        u = ss.thread_usage("sid1")
        check("usage: subagents add to the thread and count apart (a write with no split is a 5m write)",
              (u["calls"], u["subagent_calls"], u["input"], u["cost"]), (2, 1, 15, 1335 + 5 + 10 + 10))
        with open(main, "a", encoding="utf-8") as f:
            f.write(answer("m2", input_tokens=1, output_tokens=1) + "\n")
        check("usage: a transcript that changed is read again", ss.thread_usage("sid1")["calls"], 3)
        (tmp / "sid2.jsonl").write_text(json.dumps({"type": "user", "timestamp": "2026-11-20T03:00:00.000Z",
                                                    "message": {"role": "user", "content": "hi"}}) + "\n", encoding="utf-8")
        u = ss.thread_usage("sid2")
        check("usage: a thread never renewed counts from its start, nothing spent yet", (u["renewed"], u["calls"], bool(u["since"])), (False, 0, True))
    finally:
        ss.transcripts_dir = real
        shutil.rmtree(tmp, ignore_errors=True)


def unnamed_cases(check):
    """threads(include_unnamed=True) lists the chats with no name under the title VS Code shows them by, the PIN hidden."""
    real = (ss.transcripts_dir, ss.archived_ids, ss.live_sessions, ss.dispatch.pending, ss.guard.STORE_DIR)
    tmp = Path(tempfile.mkdtemp(prefix="setup-unnamed-"))
    try:
        folder = tmp / "projects"
        folder.mkdir()
        ss.transcripts_dir, ss.archived_ids = (lambda: folder), (lambda: {"u2"})
        ss.live_sessions, ss.dispatch.pending = (lambda fresh=False: {}), (lambda root: [])
        ss.guard.STORE_DIR = str(tmp / "store")
        user = lambda text: {"type": "user", "entrypoint": "claude-vscode", "message": {"role": "user", "content": [{"type": "text", "text": text}]}}
        lines = {
            "named": [{"type": "custom-title", "customTitle": "Codex"}, {"type": "ai-title", "aiTitle": "khác"}, user("hi")],
            "old": [{"type": "custom-title", "customTitle": "Wallet"}, user("cũ")],
            "u1": [user("FEATURE=@human 123@abc"), {"type": "ai-title", "aiTitle": "FEATURE=@human 123@abc"}],
            "u2": [user("đầu"), {"type": "last-prompt", "lastPrompt": "Codex"}],
            "u3": [{"type": "user", "isMeta": True, "message": {"role": "user", "content": "meta"}}, user("tin   đầu\nhai dòng")],
            "u4": [{"type": "queue-operation"}],
        }
        for sid, rows in lines.items():
            (folder / f"{sid}.jsonl").write_text("".join(json.dumps(r, ensure_ascii=False) + "\n" for r in rows), encoding="utf-8")
        check("unnamed: the default list keeps named threads only", sorted(t["id"] for t in ss.threads()), ["named", "old"])
        got = {t["id"]: t for t in ss.threads(include_unnamed=True)}
        check("unnamed: every chat VS Code lists, none with no title", sorted(got), ["named", "old", "u1", "u2", "u3"])
        check("outside: a thread of the setup is not outside", (got["named"]["outside"], got["named"]["group"], got["named"]["archived"]), (False, "Tools", False))
        check("outside: an old feature thread is outside, in no tier, and nothing runs in it",
              (got["old"]["outside"], got["old"]["group"], got["old"]["archived"]), (True, "", True))
        check("outside: a chat with no name is not outside", got["u3"]["outside"], False)
        check("unnamed: custom-title wins over ai-title", (got["named"]["title"], got["named"]["named"], got["named"]["feature"]), ("Codex", True, "@Codex"))
        check("unnamed: ai-title, PIN hidden", (got["u1"]["title"], got["u1"]["named"]), ("FEATURE=@human ••••", False))
        check("unnamed: no PIN anywhere in the row", "123@abc" in json.dumps(got["u1"], ensure_ascii=False), False)
        check("unnamed: last prompt before the first message, no role taken from it",
              (got["u2"]["title"], got["u2"]["feature"], got["u2"]["group"], got["u2"]["legacy"], got["u2"]["archived"]), ("Codex", "", "", False, True))
        check("unnamed: first real message, one line", got["u3"]["title"], "tin đầu hai dòng")
        check("unnamed: a long title is cut", len(ss.redact_title("x" * 200)), ss.TITLE_MAX)
    finally:
        (ss.transcripts_dir, ss.archived_ids, ss.live_sessions, ss.dispatch.pending, ss.guard.STORE_DIR) = real
        shutil.rmtree(tmp, ignore_errors=True)


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
        ss.threads = lambda include_unnamed=False: [thread("aaaa1111", "Wallet", binding="Wallet", archived=True), thread("bbbb2222", "Shop", binding="Shop")]
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


def delete_outside_cases(check):
    """Xoá cả nhóm Ngoài chức năng: every named thread outside the setup, the PIN checked once; the setup's threads
    and unnamed chats stay."""
    real = (ss.threads, ss.delete_thread, ss.bind_session.check_pin, ss.vscode_groups.vscode_running)
    tries, gone = [], []
    try:
        rows = [dict(thread("aaaa1111", "Wallet"), outside=True), dict(thread("bbbb2222", "Hero"), outside=True),
                dict(thread("cccc3333", "CodeLead", binding="@CodeLead"), outside=False),
                dict(thread("dddd4444", "chat lạ"), named=False, outside=False)]
        ss.threads = lambda include_unnamed=False: rows
        ss.bind_session.check_pin = lambda store_, pin: tries.append(pin) or ("" if pin == "1234" else "PIN sai")
        ss.delete_thread = lambda sid, pin, listed=None, pin_ok=False: (gone.append((sid, pin_ok)),
            {"ok": True, "trash": "/x/.deleted/t-" + sid} if sid != "bbbb2222" else {"ok": False, "error": "đang mở"})[1]
        ss.vscode_groups.vscode_running = lambda: True
        res = ss.delete_outside("1234")
        check("delete all: refused while VS Code runs, before the PIN", (res["ok"], res.get("vscode"), tries, gone), (False, True, [], []))
        ss.vscode_groups.vscode_running = lambda: False
        res = ss.delete_outside("0000")
        check("delete all: a wrong PIN deletes nothing", (res["ok"], res.get("pin"), gone), (False, True, []))
        tries.clear()
        res = ss.delete_outside("1234")
        check("delete all: only the outside threads, the PIN checked once", (gone, tries), ([("aaaa1111", True), ("bbbb2222", True)], ["1234"]))
        check("delete all: what was deleted and what failed", (res["ok"], res["deleted"], len(res["failed"]), res["trash"]),
              (False, ["Wallet"], 1, "/x/.deleted"))
        rows[:2] = []
        check("delete all: nothing outside is ok, no PIN asked", (ss.delete_outside("")["ok"], tries), (True, ["1234"]))
        check("the page may post to the delete-all path", "/api/sessions/delete-outside" in ss.POST_PATHS)
        check("sets: only the Leaders and above and the tools, no feature", (ss.sets()["full"], "missing" in ss.sets()), (ss.BASIC, False))
    finally:
        (ss.threads, ss.delete_thread, ss.bind_session.check_pin, ss.vscode_groups.vscode_running) = real


def cli_cases(check):
    """The claude CLI is run directly, never through cmd.exe: there an argument is cut at its first newline, and the
    inbox gave the threads messages without their text."""
    real = (ss.shutil.which, ss.subprocess.run, ss.os.name)
    calls = []

    class Done:
        returncode, stdout, stderr = 0, "ok", ""

    def fake_run(command, **kw):
        calls.append((command, kw.get("shell")))
        return Done()
    prompt = "Việc giao qua hộp thư.\n\n[tin m20261125-222813-ce0fb4 · từ @Manager]\nLàm 8 yêu cầu & 50% (a) \"x\""
    try:
        ss.subprocess.run = fake_run
        ss.shutil.which = lambda name: r"C:\Users\x\.local\bin\claude.exe"
        out = ss.run_claude(["--bg", "--resume", "sid", prompt])
        check("cli: an .exe is run directly, with the prompt whole: every line, the message id, the special characters",
              (out, calls[-1][0][0].endswith("claude.exe"), calls[-1][0][-1] == prompt, bool(calls[-1][1])), ("ok", True, True, False))
        ss.shutil.which = lambda name: None
        ss.run_claude(["-p", prompt])
        check("cli: not found on PATH: still run by name, without the shell", (calls[-1][0][0], calls[-1][0][-1] == prompt, bool(calls[-1][1])),
              ("claude", True, False))
        if os.name == "nt":
            ss.shutil.which = lambda name: r"C:\Users\x\AppData\Roaming\npm\claude.CMD"
            ss.run_claude(["-p", prompt])
            sent = calls[-1][0][-1]
            check("cli: a .cmd shim must go through cmd.exe: the newlines are written as a mark, no text is lost",
                  (bool(calls[-1][1]), "\n" in sent, "m20261125-222813-ce0fb4" in sent, "Làm 8 yêu cầu" in sent, sent.count(ss.NEWLINE_MARK)),
                  (True, False, True, True, 3))
        ss.shutil.which = lambda name: None
        for args in (["--bg", "--resume", "sid", "x"], ["--resume", "sid", "-p", "x"], ["-n", "T", "--session-id", "s", "-p", "x"],
                     ["-p", "--model", "m", "x"]):
            ss.run_claude(args)
            check(f"cli: a turn ({args[0]}) runs without Claude in Chrome", calls[-1][0][1:], ["--no-chrome"] + args)
        for args in (["agents", "--json"], ["stop", "abc"], ["rm", "abc"]):
            ss.run_claude(args)
            check(f"cli: {args[0]} runs no turn and takes no Chrome flag", calls[-1][0][1:], args)
    finally:
        ss.shutil.which, ss.subprocess.run = real[0], real[1]
    import subprocess as sp
    echoed = sp.run([sys.executable, "-c", "import sys; sys.stdout.reconfigure(encoding='utf-8'); print(repr(sys.argv[1]))", prompt],
                    capture_output=True, text=True, encoding="utf-8").stdout.strip()
    check("cli: a real process run without the shell gets every line of the argument", echoed, repr(prompt))


def watch_cases(check):
    """The watch over silent turns: a turn that runs while its transcript gets no new line is told to @Manager, and a
    background thread that runs on demand is stopped and given its work again, once."""
    real = (ss.ROOT, ss.threads, ss.live_sessions, ss.stop_bg, ss.transcripts_dir)
    root, tdir = Path(tempfile.mkdtemp(prefix="watch-")), Path(tempfile.mkdtemp(prefix="watch-t-"))
    live, stopped = {}, []
    listed = [thread("w0000000", "Wallet", binding="Wallet"), thread("h0000000", "Hud", binding="Hud"),
              thread("l0000000", "CodeLead", binding="@CodeLead"), thread("u0000000", "UnityAutoPlay", binding="@UnityAutoPlay"),
              thread("m0000000", "Me", binding="@human")]

    def call(sid, name, inp, done=False):
        rows = [{"type": "assistant", "message": {"content": [{"type": "tool_use", "id": "t1", "name": name, "input": inp}]}}]
        if done:
            rows.append({"type": "user", "message": {"content": [{"type": "tool_result", "tool_use_id": "t1", "content": "ok"}]}})
            rows.append({"type": "api-request-blob", "message": {"content": [rows[0]["message"]["content"][0]]}})      # the CLI's echo
        (tdir / f"{sid}.jsonl").write_text("".join(json.dumps(r, ensure_ascii=False) + "\n" for r in rows), encoding="utf-8")

    def age(sid, minutes):
        os.utime(tdir / f"{sid}.jsonl", (time.time() - minutes * 60, time.time() - minutes * 60))

    def busy(sid, kind="background"):
        live[sid] = {"kind": kind, "status": "busy", "state": "working", "short": sid}

    def notes():
        d = root / "Management" / "reports"
        return sorted(p.read_text(encoding="utf-8") for p in d.glob("*.md")) if d.is_dir() else []

    def fake_stop(sid):
        stopped.append(sid)
        live.pop(sid, None)

    delete = {"action": "delete", "path": "Assets/Features/Wallet/X.cs", "unity_instance": "UnityProject@1ec6164a"}
    try:
        ss.ROOT, ss.threads, ss.live_sessions = root, lambda include_unnamed=False: [dict(t) for t in listed], lambda fresh=False: dict(live)
        ss.stop_bg, ss.transcripts_dir = fake_stop, lambda: tdir
        ss.HUNG.clear()
        for t in listed:
            call(t["id"], "mcp__UnityMCP__manage_asset", delete)
        busy("w0000000")
        age("w0000000", 2)
        check("watch: a turn silent for two minutes is not stuck yet", (ss.watch_tick(), ss.hang_of(listed[0], live["w0000000"], time.time())), ([], None))
        age("w0000000", 4)
        h = ss.hang_of(listed[0], live["w0000000"], time.time())
        check("watch: silent for four minutes: the page says where it hangs, nobody is told yet",
              (ss.hang_text(h), ss.watch_tick(), notes()),
              ("im 4 phút ở lệnh mcp__UnityMCP__manage_asset (action=delete path=Assets/Features/Wallet/X.cs)", [], []))
        check("watch: the tab of the inbox carries it", [x["hung"][:10] for x in ss.inbox_view()["pools"]["feature"]["threads"]], ["im 4 phút "])
        age("w0000000", 6)
        check("watch: silent past hung_note: one note for @Manager", (ss.watch_tick(), len(notes()), "phiên treo: lượt đang chạy im 6 phút" in notes()[0],
                                                                      "FEATURE=Wallet" in notes()[0]), ([("hung-noted", "Wallet")], 1, True, True))
        check("watch: the same silence is told once", (ss.watch_tick(), len(notes()), list(stopped)), ([], 1, []))
        age("w0000000", 11)
        did = ss.watch_tick()
        ms = ss.dispatch.messages(root, "Wallet")
        check("watch: silent past hung_stop: the background thread is stopped and its work is put in the inbox again",
              (did, list(stopped), [(m["source"], m["state"], "bị treo: im 11 phút ở lệnh mcp__UnityMCP__manage_asset" in m["text"],
                               "không làm lại từ đầu" in m["text"]) for m in ms], len(notes())),
              ([("hung-restarted", "Wallet")], ["w0000000"], [("canh phiên", "queued", True, True)], 2))
        ss.watch_tick()      # the stopped thread is gone: its silence is over
        busy("w0000000")
        age("w0000000", 11)
        check("watch: stuck again within the hour: not stopped a second time, the human is told",
              (ss.watch_tick(), list(stopped), len(ss.dispatch.messages(root, "Wallet")), "treo lần nữa" in notes()[-1] or any("treo lần nữa" in n for n in notes())),
              ([("hung-left", "Wallet")], ["w0000000"], 1, True))
        live.clear()
        ss.HUNG.clear()
        n = len(notes())
        for sid, kind in (("l0000000", "background"), ("u0000000", "background"), ("h0000000", "interactive"), ("m0000000", "interactive")):
            busy(sid, kind)
            age(sid, 30)
        did = ss.watch_tick()
        check("watch: a Leader, a tester and a chat open in VS Code are told of, never stopped; the human's own chat is left alone",
              (sorted(did), list(stopped), len(notes()) - n), ([("hung-noted", "@CodeLead"), ("hung-noted", "@UnityAutoPlay"), ("hung-noted", "Hud")], ["w0000000"], 3))
        live.clear()
        ss.HUNG.clear()
        busy("h0000000")
        call("h0000000", "Agent", {"prompt": "x"})
        age("h0000000", 30)
        check("watch: a turn that waits for a subagent is not stuck", ss.watch_tick(), [])
        call("h0000000", "Bash", {"command": "python Management/tools/validate.py"})
        age("h0000000", 11)
        check("watch: a shell command may run for ten minutes", ss.watch_tick(), [])
        age("h0000000", 13)
        check("watch: past that it is stuck like any call", ss.watch_tick(), [("hung-restarted", "Hud")])
        busy("h0000000")
        call("h0000000", "mcp__UnityMCP__manage_asset", delete, done=True)
        age("h0000000", 6)
        ss.HUNG.clear()
        check("watch: no call open (the model does not answer): told so", (ss.watch_tick(), "không ở lệnh nào" in "".join(notes())), ([("hung-noted", "Hud")], True))
        live["h0000000"] = {"kind": "background", "status": "busy", "state": "blocked", "short": "h0000000"}
        ss.HUNG.clear()
        check("watch: a turn that waits for the human is not stuck; an idle thread neither", ss.watch_tick(), [])
        ss.dispatch.set_config(root, hung_stop=0, hung_note=2)
        live.clear()
        ss.HUNG.clear()
        busy("w0000000")
        call("w0000000", "mcp__UnityMCP__manage_asset", delete)
        age("w0000000", 300)
        n_stop = len(stopped)
        check("watch: hung_stop 0: only the note, however long", (ss.watch_tick(), len(stopped) - n_stop), ([("hung-noted", "Wallet")], 0))
        check("watch: the settings are checked", (refused_cfg(root, hung_note=1), refused_cfg(root, hung_stop=500), ss.dispatch.config(root)["hung_note"]), (True, True, 2))
    finally:
        ss.ROOT, ss.threads, ss.live_sessions, ss.stop_bg, ss.transcripts_dir = real
        ss.HUNG.clear()
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(tdir, ignore_errors=True)


def takeover_cases(check):
    """A message for a thread whose chat is open in VS Code: an idle chat of a thread that runs on demand is closed
    and the thread runs in the background; the Giao ngay button does it at once; a message that waits long is told."""
    real = (ss.ROOT, ss.threads, ss.live_sessions, ss.turn, ss.stop_bg, ss.transcripts_dir, ss.close_chat, ss.kill_pid, ss.wait_live)
    root, tdir = Path(tempfile.mkdtemp(prefix="take-")), Path(tempfile.mkdtemp(prefix="take-t-"))
    live, given, closed, killed = {}, [], [], []
    listed = [thread("w0000000", "Blender", binding="@Blender"), thread("h0000000", "DesignAdvisor", binding="@DesignAdvisor"),
              thread("s0000000", "ClaudeImage", binding="@ClaudeImage")]
    for t in listed:
        (tdir / f"{t['id']}.jsonl").write_text("{}\n", encoding="utf-8")

    def chat(sid, status="idle", minutes=0):
        live[sid] = {"kind": "interactive", "status": status, "state": "", "short": sid, "pid": 4242}
        os.utime(tdir / f"{sid}.jsonl", (time.time() - minutes * 60, time.time() - minutes * 60))

    def fake_turn(t, prompt, bg=None):
        given.append((t["id"], prompt))
        live[t["id"]] = {"kind": "background", "status": "busy", "state": "working", "short": t["id"][:8]}

    def fake_close(sid):
        closed.append(sid)
        live.pop(sid, None)

    def notes():
        d = root / "Management" / "reports"
        return sorted(p.read_text(encoding="utf-8") for p in d.glob("*.md")) if d.is_dir() else []

    def msg(mid):
        return ss.dispatch.get(root, mid)

    try:
        ss.ROOT, ss.threads, ss.live_sessions = root, lambda include_unnamed=False: [dict(t) for t in listed], lambda fresh=False: dict(live)
        ss.turn, ss.stop_bg, ss.transcripts_dir, ss.close_chat = fake_turn, lambda sid: None, lambda: tdir, fake_close
        chat("w0000000", minutes=2)
        a = ss.dispatch.send(root, "@Blender", "Việc cho Wallet", "@Manager")
        ss.courier_tick(time.time())
        check("takeover: a chat idle for two minutes is left open; the message says when the server will close it",
              (msg(a["id"])["state"], "rảnh 2 phút: server tự đóng chat và giao khi rảnh đủ 5 phút" in msg(a["id"])["why"], closed, given),
              ("waiting", True, [], []))
        chat("w0000000", status="busy", minutes=30)
        ss.courier_tick(time.time())
        check("takeover: a chat in the middle of a turn is never closed, however long", (closed, "đang chạy một lượt" in msg(a["id"])["why"]), ([], True))
        check("deliver now: refused with the reason while the chat runs a turn",
              (ss.inbox_call("deliver", {"id": a["id"]})["ok"], "đang chạy một lượt" in ss.inbox_call("deliver", {"id": a["id"]})["error"], closed), (False, True, []))
        chat("w0000000", minutes=6)
        did = ss.courier_tick(time.time())
        check("takeover: idle past takeover_minutes with a message waiting: the chat is closed and the thread runs in the background with it",
              (list(closed), [g[0] for g in given], a["id"] in given[-1][1], msg(a["id"])["state"], ("took-over", "@Blender") in did),
              (["w0000000"], ["w0000000"], True, "delivering", True))
        chat("h0000000", minutes=1)
        b = ss.dispatch.send(root, "@DesignAdvisor", "Việc cho Hud", "@Manager")
        ss.courier_tick(time.time())
        n_closed = len(closed)
        res = ss.inbox_call("deliver", {"id": b["id"]})
        check("deliver now: the chat is closed at once, without the wait, and the message is given",
              (res["ok"], res.get("state"), closed[n_closed:], given[-1][0], msg(b["id"]).get("force")), (True, "delivering", ["h0000000"], "h0000000", False))
        ss.dispatch.set_config(root, takeover_minutes=0)
        chat("s0000000", minutes=600)
        c = ss.dispatch.send(root, "@ClaudeImage", "Việc cho Shop", "@Manager")
        n_closed = len(closed)
        ss.courier_tick(time.time())
        check("takeover: takeover_minutes 0: an open chat is never closed by itself; the message says how to send it",
              (closed[n_closed:], "bấm Giao ngay" in msg(c["id"])["why"]), ([], True))
        check("deliver now: still closes it, the human asked", (ss.inbox_call("deliver", {"id": c["id"]})["ok"], closed[n_closed:]), (True, ["s0000000"]))
        ss.dispatch.set_config(root, takeover_minutes=5, limits={"tool": 1})
        live.clear()
        live["w0000000"] = {"kind": "background", "status": "busy", "state": "working", "short": "w0000000"}
        for m in ss.dispatch.pending(root):
            ss.dispatch.mark(root, m, "delivered")
        d = ss.dispatch.send(root, "@DesignAdvisor", "Việc hai", "@Manager", now=time.time() - 400)
        n = len(notes())
        did = ss.courier_tick(time.time())
        check("wait note: a message that has waited five minutes is told to @Manager, with why; once",
              (msg(d["id"])["state"], "hết chỗ" in msg(d["id"])["why"], len(notes()) - n, "chưa giao được sau 6 phút" in notes()[-1] and "hết chỗ" in notes()[-1],
               ss.courier_tick(time.time()) and len(notes()) - n), ("waiting", True, 1, True, 1))
        n_given = len(given)
        res = ss.inbox_call("deliver", {"id": d["id"]})
        check("deliver now: a full pool is passed for this message; the thread is told the request may be old",
              (res["ok"], len(given) - n_given, "LƯU Ý: tin này gửi lúc" in given[-1][1]), (True, 1, True))
        check("deliver now: a message that is not waiting is refused", ss.inbox_call("deliver", {"id": d["id"]})["ok"], False)
        # close_chat itself
        ss.close_chat, ss.kill_pid, ss.wait_live = real[6], lambda pid: killed.append(pid), lambda sid, present: True
        chat("s0000000", status="busy")
        try:
            ss.close_chat("s0000000")
            said = ""
        except RuntimeError as ex:
            said = str(ex)
        chat("s0000000")
        ss.close_chat("s0000000")
        live["s0000000"]["kind"] = "background"
        ss.close_chat("s0000000")
        check("close_chat: ends the process of an idle chat only; never a chat in a turn, never a background session",
              (said, killed), ("chat đang chạy một lượt", [4242]))
    finally:
        ss.ROOT, ss.threads, ss.live_sessions, ss.turn, ss.stop_bg, ss.transcripts_dir, ss.close_chat, ss.kill_pid, ss.wait_live = real
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(tdir, ignore_errors=True)


def refused_cfg(root, **kw):
    try:
        ss.dispatch.set_config(root, **kw)
        return False
    except ValueError:
        return True


def courier_cases(check):
    """The inbox courier: a message is given to its thread as a turn of its own, counted as delivered only when its id
    is in the transcript; on-demand mode stops idle threads and keeps each pool under its limit."""
    real = (ss.ROOT, ss.threads, ss.live_sessions, ss.turn, ss.stop_bg, ss.transcripts_dir)
    root, tdir = Path(tempfile.mkdtemp(prefix="courier-")), Path(tempfile.mkdtemp(prefix="courier-t-"))
    live, given, stopped = {}, [], []
    listed = [thread("w0000000", "Blender", binding="@Blender"), thread("s0000000", "ClaudeImage", binding="@ClaudeImage"),
              thread("h0000000", "DesignAdvisor", binding="@DesignAdvisor"), thread("c0000000", "Codex", binding="@Codex"),
              thread("l0000000", "CodeLead", binding="@CodeLead"), thread("o0000000", "Old", binding="Old", archived=True)]
    for t in listed:
        (tdir / f"{t['id']}.jsonl").write_text("{}\n", encoding="utf-8")
    seen = {"write": True}

    def fake_turn(t, prompt, bg=None):
        given.append((t["id"], prompt, t.get("live"), bg))
        live[t["id"]] = {"kind": "background", "status": "busy", "state": "working", "short": t["id"][:8]}
        if seen["write"]:      # the thread writes the prompt into its transcript, as the CLI does
            with open(tdir / f"{t['id']}.jsonl", "a", encoding="utf-8") as f:
                f.write(json.dumps({"type": "user", "message": {"content": prompt}}, ensure_ascii=False) + "\n")
        return ss.BG_ANSWER

    def fake_stop(sid):
        stopped.append(sid)
        live.pop(sid, None)

    def age(sid, minutes):
        os.utime(tdir / f"{sid}.jsonl", (time.time() - minutes * 60, time.time() - minutes * 60))

    try:
        ss.ROOT, ss.threads, ss.live_sessions = root, lambda include_unnamed=False: [dict(t) for t in listed], lambda fresh=False: dict(live)
        ss.turn, ss.stop_bg, ss.transcripts_dir = fake_turn, fake_stop, lambda: tdir
        now = time.time()
        check("courier: an empty inbox in always-live mode does nothing", ss.courier_tick(now), [])
        a = ss.dispatch.send(root, "@Blender", "Làm yêu cầu x#y", "@Manager")
        did = ss.courier_tick(now)
        m = ss.dispatch.get(root, a["id"])
        check("courier: a thread that is not running is started with the message as its prompt",
              (did[0][:2], given[-1][0], a["id"] in given[-1][1], given[-1][3], m["state"], m["tries"]),
              (("given", "@Blender"), "w0000000", True, True, "delivering", 1))
        ss.courier_tick(now + 1)
        check("courier: delivered once the id is in the transcript", ss.dispatch.get(root, a["id"])["state"], "delivered")

        live["w0000000"]["status"] = "busy"
        b = ss.dispatch.send(root, "@Blender", "Việc thứ hai", "@CodeLead")
        ss.courier_tick(now + 2)
        check("courier: a thread in the middle of a turn: the message waits",
              (ss.dispatch.get(root, b["id"])["state"], ss.dispatch.get(root, b["id"])["why"]), ("waiting", "thread đang chạy một lượt"))
        live["w0000000"]["state"] = "blocked"
        n_blocked = len(given)
        ss.courier_tick(now + 2.5)
        check("courier: a background thread stopped at a question to the human does nothing: the waiting message takes it over",
              (len(given) - n_blocked, given[-1][0], b["id"] in given[-1][1], given[-1][2]), (1, "w0000000", True, "background"))
        ss.courier_tick(now + 2.6)
        live["w0000000"] = {"kind": "background", "status": "busy", "state": "working", "short": "w0000000"}
        b = ss.dispatch.send(root, "@Blender", "Việc thứ ba", "@CodeLead")
        ss.courier_tick(now + 2.7)
        live["w0000000"]["status"] = "idle"
        c = ss.dispatch.send(root, "@Blender", "Việc thứ ba", "người")
        ss.courier_tick(now + 3)
        check("courier: an idle background thread gets every waiting message in one turn, as a background turn",
              (b["id"] in given[-1][1] and c["id"] in given[-1][1], given[-1][2], len([g for g in given if g[0] == "w0000000"])),
              (True, "background", 3))
        ss.courier_tick(now + 4)

        live["s0000000"] = {"kind": "interactive", "status": "busy", "state": "", "short": "s0000000"}
        d = ss.dispatch.send(root, "@ClaudeImage", "Việc cho Shop", "@Manager")
        ss.courier_tick(now + 5)
        check("courier: a chat open in VS Code is never given a turn: the message waits",
              (ss.dispatch.get(root, d["id"])["state"], "VS Code" in ss.dispatch.get(root, d["id"])["why"], given[-1][0] != "s0000000"),
              ("waiting", True, True))
        ss.busy("h0000000", True)
        f = ss.dispatch.send(root, "@DesignAdvisor", "Việc cho Hud", "@Manager")
        ss.courier_tick(now + 7)
        check("courier: a thread the page is running a turn in: waits", "trang đang chạy" in ss.dispatch.get(root, f["id"])["why"])
        ss.busy("h0000000", False)

        seen["write"] = False
        ss.courier_tick(now + 8)
        g = ss.dispatch.get(root, f["id"])
        ss.courier_tick(now + 8 + ss.dispatch.VERIFY_SECONDS + 1)
        g2 = ss.dispatch.get(root, f["id"])
        check("courier: a delivery never seen in the transcript is tried again later, not counted as delivered",
              (g["state"], g2["state"], g2["error"].startswith("không thấy tin"), g2["next_try"] > now + 8), ("delivering", "queued", True, True))
        seen["write"] = True
        for i in range(ss.dispatch.MAX_TRIES):
            live.pop("h0000000", None)
            g2 = ss.dispatch.mark(root, ss.dispatch.get(root, f["id"]), "queued", next_try=0)
            seen["write"] = False
            ss.courier_tick(now + 100 + i * 200)
            ss.courier_tick(now + 100 + i * 200 + ss.dispatch.VERIFY_SECONDS + 1)
        check("courier: after the last try the message is an error", ss.dispatch.get(root, f["id"])["state"], "error")
        seen["write"] = True

        ss.dispatch.cancel(root, d["id"])
        ss.dispatch.set_config(root, mode="on-demand", limits={"tool": 1}, idle_minutes=10)
        live.clear()
        live["w0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "w0000000"}
        age("w0000000", 1)
        h = ss.dispatch.send(root, "@DesignAdvisor", "Việc mới cho Hud", "@Manager")
        ss.courier_tick(time.time())
        check("on-demand: the pool is full and its thread was busy a minute ago: the message waits, nothing is stopped",
              (ss.dispatch.get(root, h["id"])["state"], "hết chỗ" in ss.dispatch.get(root, h["id"])["why"], list(stopped)), ("waiting", True, []))
        age("w0000000", 30)
        ss.courier_tick(time.time())
        check("on-demand: idle past idle_minutes: it is stopped, and the waiting thread gets the slot",
              (stopped, given[-1][0], "w0000000" in live), (["w0000000"], "h0000000", False))
        live["c0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "c0000000"}
        live["l0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "l0000000"}
        live["s0000000"] = {"kind": "interactive", "status": "idle", "state": "", "short": "s0000000"}
        for sid in ("c0000000", "l0000000", "s0000000"):
            age(sid, 60)
        ss.courier_tick(time.time())
        check("on-demand: an idle tool thread is stopped; a Leader and a chat open in VS Code never are",
              ("c0000000" in stopped, "l0000000" in stopped, "s0000000" in stopped), (True, False, False))
        live["c0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "c0000000"}
        age("c0000000", 60)
        ss.dispatch.send(root, "@Codex", "Order mới", "studio")
        n_stop = len(stopped)
        ss.courier_tick(time.time())
        check("on-demand: a thread with a message waiting is given it, not stopped", (len(stopped), given[-1][0]), (n_stop, "c0000000"))
        # a Renew marked on the page runs right before the next job: wipe (headless), bind again, then the job
        real_store, ss.guard.STORE_DIR = ss.guard.STORE_DIR, tempfile.mkdtemp(prefix="courier-store-")
        try:
            ss.bind_session.write_json(ss.renew_mark_path("s0000000"), {"binding": "@ClaudeImage"})
            with open(tdir / "s0000000.jsonl", "a", encoding="utf-8") as f:      # a conversation to drop
                f.write(json.dumps({"type": "user", "message": {"content": "việc cũ"}}, ensure_ascii=False) + "\n")
            live.pop("s0000000", None)
            m = ss.dispatch.send(root, "@ClaudeImage", "Làm yêu cầu shop-list#a", "@CodeLead")
            n = len(given)
            ss.dispatch.mark(root, m, "queued", force=True)      # the Giao ngay button: the pool limit does not hold
            did = ss.courier_tick(time.time() + 3)
            kept = [json.loads(x) for x in (tdir / "s0000000.jsonl").read_text(encoding="utf-8").splitlines()]
            check("renew mark: cleared first with no turn (old transcript in .renewed), then the job; bound again, mark gone",
                  ([(g[0], g[3]) for g in given[n:]], ("renewed", "@ClaudeImage") in did, ss.renew_pending("s0000000"),
                   ss.guard.bound_feature("s0000000", ss.guard.STORE_DIR), len(list((tdir / ss.RENEWED).glob("*-s0000000/s0000000.jsonl"))),
                   kept[0]["message"]["content"]),
                  ([("s0000000", True)], True, False, "@ClaudeImage", 1, ss.RENEW_NOTE))
            check("renew mark: the job prompt is the only thing after the renew turn",
                  (len(kept), kept[1]["type"], "shop-list#a" in kept[-1]["message"]["content"]), (3, "assistant", True))
            ss.courier_tick(time.time() + 4)
            ss.bind_session.write_json(ss.renew_mark_path("s0000000"), {"binding": "@ClaudeImage"})
            live["s0000000"] = {"kind": "background", "status": "idle", "state": "blocked", "short": "s0000000"}
            m = ss.dispatch.send(root, "@ClaudeImage", "Trả lời câu hỏi", "@CodeLead")
            ss.dispatch.mark(root, m, "queued", force=True)
            n = len(given)
            ss.courier_tick(time.time() + 5)
            check("renew mark: a thread stopped at a question to the human keeps its context and its mark",
                  (len(given[n:]), ss.renew_pending("s0000000"), len(list((tdir / ss.RENEWED).glob("*-s0000000")))), (1, True, 1))
            ss.courier_tick(time.time() + 6)
        finally:
            shutil.rmtree(ss.guard.STORE_DIR, ignore_errors=True)
            ss.guard.STORE_DIR = real_store
        ss.dispatch.set_config(root, mode="always-live")
        live["w0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "w0000000"}
        age("w0000000", 300)
        n_stop = len(stopped)
        ss.courier_tick(time.time())
        check("always-live: nothing is ever stopped", len(stopped), n_stop)
        v = ss.inbox_view()
        live["w0000000"] = {"kind": "background", "status": "idle", "state": "blocked", "short": "w0000000"}
        who = ss.inbox_view()["pools"]["tool"]["threads"]
        check("inbox view: who takes the places of a pool: the thread, where it runs, what it does, how long it has been quiet",
              [(x["title"], x["kind"], x["status"], x["state"], x["idle"] >= 0) for x in who if x["id"] == "w0000000"],
              [("Blender", "background", "idle", "blocked", True)])
        live.pop("w0000000")
        check("inbox view: settings, each pool with its limit and running threads, targets without Leaders",
              (v["config"]["mode"], v["pools"]["tool"]["limit"], sorted(t["target"] for t in v["targets"])),
              ("always-live", 1, ["@Blender", "@ClaudeImage", "@Codex", "@DesignAdvisor"]))
        res = ss.inbox_call("send", {"target": "@ClaudeImage", "text": "Việc của người"})
        check("inbox send: from the page the source is the human", (res["ok"], ss.dispatch.get(root, res["id"])["source"]), (True, "người"))
        check("inbox send refused: a Leader", ss.inbox_call("send", {"target": "@CodeLead", "text": "x"})["ok"], False)
        check("inbox cancel and retry", (ss.inbox_call("cancel", {"id": res["id"]})["ok"], ss.inbox_call("retry", {"id": res["id"]})["ok"],
                                         ss.dispatch.get(root, res["id"])["state"]), (True, True, "queued"))
        check("inbox config: saved, a bad value refused", (ss.inbox_call("config", {"mode": "on-demand", "limits": {"tool": 2}})["ok"],
                                                           ss.dispatch.config(root)["limits"]["tool"],
                                                           ss.inbox_call("config", {"mode": "wild"})["ok"]), (True, 2, False))
        check("inbox: an unknown action is refused", ss.inbox_call("delete", {})["ok"], False)
        check("inbox reqs: a tool or @Core has no book", (ss.inbox_reqs("@Codex")["reqs"], ss.inbox_reqs("@Core")["reqs"]), ([], []))
        real_valid = ss.still_valid
        ss.still_valid = lambda check: (False, "đã sửa") if check == "gone" else (True, "")
        live.pop("w0000000", None)
        stale = ss.dispatch.send(root, "@Blender", "Yêu cầu bị trả", "gd_req", key="reject:x", check="gone")
        fresh = ss.dispatch.send(root, "@Blender", "Việc còn đúng", "@Manager", check="ok")
        n = len(given)
        ss.dispatch.set_config(root, mode="always-live", limits={"tool": 4})
        ss.courier_tick(time.time())
        ss.still_valid = real_valid
        check("courier: an automatic message whose event no longer holds is cancelled, not given; the others are",
              (ss.dispatch.get(root, stale["id"])["state"], ss.dispatch.get(root, stale["id"])["why"], fresh["id"] in given[-1][1],
               stale["id"] in given[-1][1], len(given) - n), ("cancelled", "hết hợp lệ: đã sửa", True, False, 1))
        check("still_valid: a check it cannot read lets the message go", (ss.still_valid(None), ss.still_valid("weird x")), ((True, ""), (True, "")))
        ss.dispatch.set_config(root, mode="always-live", limits={"tool": 1})
        live.clear()
        live["w0000000"] = {"kind": "background", "status": "busy", "state": "working", "short": "w0000000"}
        age("w0000000", 300)
        capped = ss.dispatch.send(root, "@DesignAdvisor", "Việc khi nhóm đã đầy", "@Manager")
        n_stop, n_given = len(stopped), len(given)
        ss.courier_tick(time.time())
        check("always-live: the pool is full and its thread is in the middle of a turn: the message waits, nothing is stopped",
              (ss.dispatch.get(root, capped["id"])["state"], "hết chỗ" in ss.dispatch.get(root, capped["id"])["why"],
               len(stopped) - n_stop, len(given) - n_given), ("waiting", True, 0, 0))
        live["w0000000"]["status"] = "idle"
        age("w0000000", 1)
        ss.courier_tick(time.time())
        check("always-live: idle for a minute only: not stopped yet", (len(stopped) - n_stop, len(given) - n_given), (0, 0))
        live["w0000000"]["kind"] = "interactive"
        age("w0000000", 300)
        ss.courier_tick(time.time())
        check("always-live: a chat open in VS Code is never stopped to make room", (len(stopped) - n_stop, len(given) - n_given), (0, 0))
        live["w0000000"]["kind"] = "background"
        ss.courier_tick(time.time())
        check("always-live: the pool is full and a thread has been idle long enough: it is stopped, and a waiting message gets the slot",
              (stopped[n_stop:], len(given) - n_given, "w0000000" in live), (["w0000000"], 1, False))
        for m in ss.dispatch.pending(root):      # nothing waits any more
            ss.dispatch.mark(root, m, "cancelled", why="huỷ")
        live.clear()
        live["w0000000"] = {"kind": "background", "status": "idle", "state": "done", "short": "w0000000"}
        age("w0000000", 300)
        n_stop = len(stopped)
        ss.courier_tick(time.time() + 1)
        check("always-live: with no message waiting nothing is stopped for being idle", (len(stopped) - n_stop, "w0000000" in live), (0, True))
        live.pop("w0000000", None)
        check("agents: a listed job with no pid (retired by the CLI's daemon, or waiting for the human) is not running",
              ss.parse_agents([{"sessionId": "a", "kind": "background", "state": "blocked", "id": "a"},
                               {"sessionId": "b", "pid": 7, "kind": "background", "status": "idle", "state": "done", "id": "b"},
                               {"sessionId": "c", "pid": 8, "kind": "interactive", "status": "busy", "id": "c"}, "junk"]),
              {"b": {"kind": "background", "status": "idle", "pid": 7, "state": "done", "short": "b"},
               "c": {"kind": "interactive", "status": "busy", "pid": 8, "state": "", "short": "c"}})
    finally:
        ss.ROOT, ss.threads, ss.live_sessions, ss.turn, ss.stop_bg, ss.transcripts_dir = real
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(tdir, ignore_errors=True)


def profile_cases(check):
    """The role profile: each role gets its model, only its own skills, only the MCP servers it uses, and the human's
    thread gets nothing."""
    tdir = Path(tempfile.mkdtemp(prefix="profiles-"))
    real = (ss.transcripts_dir, ss.skill_names, ss.PROFILES)
    try:
        ss.PROFILES = True
        ss.transcripts_dir = lambda: tdir
        ss.skill_names = lambda: {"manager-startup-check", "codelead-dispatch", "gd-round-1", "feature-implement-req",
                                  "tester-playtest-session", "studio-fulfill-order", "unity-mcp-skill", "skill-creator",
                                  "anthropic-skills:docx"}

        def flags(feature):
            args = ss.profile_args(feature)
            settings = json.loads(Path(args[args.index("--settings") + 1]).read_text(encoding="utf-8")) if args else {}
            mcp = (json.loads(Path(args[args.index("--mcp-config") + 1]).read_text(encoding="utf-8"))["mcpServers"]
                   if "--mcp-config" in args else None)
            on = sorted(n for n in ss.skill_names() if n not in settings.get("skillOverrides", {}))
            return args, settings, mcp, on

        check("profile: the human's thread runs with no profile", ss.profile_args("@human"), [])
        check("profile: a name outside the setup runs with no profile", ss.profile_args("NoSuchFeature"), [])
        args, settings, mcp, on = flags("@Manager")
        check("profile: Manager lists only its own skills", on, ["manager-startup-check"])
        check("profile: Manager has no MCP server, strictly", (mcp, "--strict-mcp-config" in args), ({}, True))
        check("profile: no bundled skills, no claude.ai connectors, no easel, no Artifact",
              (settings["disableBundledSkills"], settings["disableClaudeAiConnectors"],
               settings["enabledPlugins"].get("cc-plugin-easel@builtin"), ss.OFF_TOOLS in args), (True, True, False, True))
        args, settings, mcp, on = flags("@CodeLead")
        check("profile: CodeLead keeps UnityMCP (its runner subagent uses it) and the Unity skill",
              (sorted(mcp), on), (["UnityMCP"], ["codelead-dispatch", "unity-mcp-skill"]))
        feature = next((n for n, _, _ in ss.gamedesign.live_feature_names(ss.ROOT)), None) or "Wallet"
        args, settings, mcp, on = flags(feature)
        check("profile: a feature thread has UnityMCP and the feature- skills",
              (sorted(mcp or {}), on), (["UnityMCP"], ["feature-implement-req", "unity-mcp-skill"]))
        args, settings, mcp, on = flags("@GameDesigner")
        check("profile: GameDesigner has no MCP and only gd- skills", (mcp, on), ({}, ["gd-round-1"]))
        args, settings, mcp, on = flags("@Blender")
        check("profile: Blender has only the blender server of .mcp.json, strictly", (sorted(mcp), "--strict-mcp-config" in args, on),
              (["blender"], True, ["studio-fulfill-order"]))
        args, settings, mcp, on = flags("@Codex")
        check("profile: another tool has no MCP server", mcp, {})
        check("profile: recommended models by role (người quyết 2026-11-27; workers run on worker.py's)",
              [ss.role_model(f, "haiku", "max") for f in ("@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner",
                                                         "@DesignAdvisor", "@Codex", "@ClaudeImage", "@Blender",
                                                         "@UnityAutoPlay", "@human")],
              [("sonnet", "medium"), ("opus", "medium"), ("opus", "medium"), ("opus", "medium"),
               ("opus", "low"), ("sonnet", "medium"), ("opus", "medium"), ("opus", "low"),
               ("sonnet", "medium"), ("haiku", "max")])
        t = thread("aaaa1111", "Manager", binding="@Manager", model="claude-opus-x", effort="high")
        args = ss.turn_args(t)
        check("profile: a turn of an existing thread runs on its role's model and effort, with the profile",
              (args[:4], "--settings" in args), (["--model", "sonnet", "--effort", "medium"], True))
    finally:
        ss.transcripts_dir, ss.skill_names, ss.PROFILES = real
        shutil.rmtree(tdir, ignore_errors=True)


def model_choice_cases(check):
    """The Model and Effort pickers of one row: the human's pick is saved per thread and wins over the role's."""
    real = (ss.threads, ss.guard.STORE_DIR, ss.run_claude, ss.start_bg, ss.stop_bg, ss.PROFILES)
    store = tempfile.mkdtemp(prefix="model-store-")
    started = []
    try:
        ss.guard.STORE_DIR = store
        ss.PROFILES = True
        ss.run_claude = lambda args: "ok"
        ss.start_bg = lambda sid, extra, prompt: started.append((sid, extra))
        ss.stop_bg = lambda sid: None
        mgr = thread("aaaa1111", "Manager", binding="@Manager", model="claude-opus-x", effort="high")
        idle = dict(thread("bbbb2222", "CodeLead", binding="@CodeLead"), live="background", live_status="idle")
        working = dict(thread("cccc3333", "Codex", binding="@Codex"), live="background", live_status="busy")
        listed = [mgr, idle, working, thread("dddd4444", "Shop")]
        ss.threads = lambda include_unnamed=False: [dict(t) for t in listed]
        for t in listed[:3]:
            ss.bind(t["id"], t["binding"])
        check("model: no pick yet, a turn runs on the role's model", ss.run_model(mgr), ("sonnet", "medium"))
        check("model: a wrong model or effort is refused before anything is saved",
              (ss.set_model("aaaa1111", "gpt", "low")["ok"], ss.set_model("aaaa1111", "opus", "huge")["ok"], ss.model_choice("aaaa1111")),
              (False, False, None))
        check("model: an unbound thread is refused", ss.set_model("dddd4444", "sonnet", "low")["ok"], False)
        res = ss.set_model("aaaa1111", "sonnet", "high")
        check("model: a thread that is not live saves the pick, no turn", (res["ok"], ss.model_choice("aaaa1111"), started), (True, ("sonnet", "high"), []))
        check("model: the pick wins over the role's in every server turn", ss.turn_args(mgr)[:4], ["--model", "sonnet", "--effort", "high"])
        res = ss.set_model("bbbb2222", "haiku", "low")
        check("model: an idle background session is started again on the pick",
              (res["ok"], len(started), started[-1][1][2:6] if started else None), (True, 1, ["--model", "haiku", "--effort", "low"]))
        res = ss.set_model("cccc3333", "opus", "max")
        check("model: a background session in a turn is saved, not restarted",
              (res["ok"], len(started), ss.model_choice("cccc3333")), (True, 1, ("opus", "max")))
        check("model: the alias of a model id", (ss.model_alias("claude-opus-5-5"), ss.model_alias("claude-sonnet-5-5"), ss.model_alias("")),
              ("opus", "sonnet", ""))
        check("the page may post to the model path", "/api/sessions/model" in ss.POST_PATHS, True)
        # Đặt lại model khuyến nghị: chỉ khi không thread nào chạy (trừ chat của người).
        res = ss.reset_models()
        check("reset: refused while a thread runs, picks kept",
              (res["ok"], sorted(res.get("running") or []), ss.model_choice("aaaa1111")),
              (False, ["CodeLead", "Codex"], ("sonnet", "high")))
        human = dict(thread("eeee5555", "human", binding="@human"), live="interactive")
        ss.bind("eeee5555", "@human")
        listed[:] = [mgr, thread("bbbb2222", "CodeLead", binding="@CodeLead"), thread("cccc3333", "Codex", binding="@Codex"), human]
        with ss.BUSY_LOCK:
            ss.BUSY.add("cccc3333")
        try:
            check("reset: refused while the server runs a turn in a thread", ss.reset_models()["ok"], False)
        finally:
            ss.busy("cccc3333", False)
        res = ss.reset_models()
        check("reset: with only the human's chat open, every pick is removed",
              (res["ok"], res["removed"], ss.model_choice("aaaa1111"), ss.model_choice("cccc3333"), ss.run_model(mgr)),
              (True, 3, None, None, ("sonnet", "medium")))
        check("reset: the answer lists the recommended model of each role", res["models"].get("Codex"), "sonnet, medium")
        check("the page may post to the reset path", "/api/sessions/reset-models" in ss.POST_PATHS, True)
    finally:
        (ss.threads, ss.guard.STORE_DIR, ss.run_claude, ss.start_bg, ss.stop_bg, ss.PROFILES) = real
        shutil.rmtree(store, ignore_errors=True)


def worker_server_cases(check):
    """Tiers 4-7 are workers (người quyết 2026-11-27): the inbox makes jobs, the server runs at most WORKER_SLOTS."""
    et, wk = ss.editor_ticket, ss.worker
    root = Path(tempfile.mkdtemp(prefix="worker-server-"))
    started, alive = [], set()
    real = (ss.ROOT, ss.start_worker, ss.running_jobs, ss.threads, ss.live_sessions, dict(ss.WORKER_STARTED), wk.open_requirements,
            ss.runner_turn, ss.guard.STORE_DIR)
    store = tempfile.mkdtemp(prefix="worker-server-store-")
    try:
        ss.ROOT = root
        ss.guard.STORE_DIR = store
        ss.threads, ss.live_sessions = (lambda include_unnamed=False: []), (lambda fresh=False: {})
        ss.start_worker = lambda job_id, kind="code", message=None: started.append((job_id, kind, message)) or ("start", kind, job_id)
        ss.running_jobs = lambda: {j: None for j in alive}
        wk.open_requirements = lambda r, f, refs=None: (None, [])
        for name in ("Run", "Wave", "Hud", "Save"):
            (root / "UnityProject/Assets/Features" / name).mkdir(parents=True)
        now = time.time()
        a = ss.dispatch.send(root, "Run", "Làm yêu cầu run-rules#x", "@CodeLead", now=now - 10)
        b = ss.dispatch.send(root, "Run", "Thêm: sửa nút", "người", now=now - 9)
        did = ss.courier_tick(now)
        jobs = ss.worker_jobs()
        check("inbox: two messages to a feature make one worker job, no thread", (len(jobs), jobs[0]["feature"], jobs[0]["state"],
              a["id"] in jobs[0]["task"] and b["id"] in jobs[0]["task"], [x[0] for x in did if x[0] in ("worker", "given")]),
              (1, "Run", "queued", True, ["worker"]))
        check("inbox: the messages are delivered, naming the job", [ss.dispatch.get(root, x["id"]).get("job") for x in (a, b)], [jobs[0]["id"]] * 2)
        c = ss.dispatch.send(root, "Run", "Một tin nữa", "@CodeLead", now=now + 1)
        ss.courier_tick(now + 2)
        check("inbox: a message while the job still waits for a slot joins it", (len(ss.worker_jobs()), c["id"] in ss.worker_load_task(jobs[0]["id"])),
              (1, True))
        u = ss.dispatch.send(root, "@UnityAutoPlay", "Chơi thử đợt 3", "@CodeLead", now=now + 3)
        ss.courier_tick(now + 4)
        play = next(j for j in ss.worker_jobs() if j["kind"] == "play")
        check("inbox: a message to the tester makes a play job waiting for an Editor ticket of its own",
              (play["state"], et.find(root, "@UnityAutoPlay", job=play["id"])["tasks"], ss.dispatch.get(root, u["id"])["state"]),
              ("waiting-editor", ["chơi thử"], "delivered"))
        for name in ("Wave", "Hud", "Save"):
            wk.queue(root, name, task="việc " + name, now=now + 5)
        wk.queue(root, "Run", task="việc Run thứ hai", now=now + 6)
        ss.WORKER_STARTED["done"] = True
        ss.worker_tick(now + 7)
        names = [wk.load(root, x[0])["feature"] for x in started]
        check("tick: at most three jobs start, never two of the same feature", (len(started), len(set(names)), names.count("Run")), (3, 3, 1))
        started.clear()
        alive.update(j["id"] for j in ss.worker_jobs() if j["kind"] == "code" and j["feature"] in names)
        d = ss.dispatch.send(root, "Run", "Tin khi Run đang chạy", "@CodeLead", now=now + 8)
        ss.courier_tick(now + 9)
        check("inbox: a feature whose worker runs: the message waits for the next job", ss.dispatch.get(root, d["id"])["state"], "waiting")
        alive.clear()
        job = ss.worker_jobs()[0]
        job["state"] = "running"
        wk.save(root, job)
        ss.WORKER_STARTED["done"] = False
        did = ss.worker_tick(now + 10)
        check("tick: the first pass after a server start: a job left running is interrupted", any(d[:2] == ("interrupted", job["id"]) for d in did), True)
        started.clear()
        # fix rounds
        w2 = wk.queue(root, "Wave", task="x", now=now + 11)
        ss.tell({"session": "@Worker", "job": w2["id"]}, "Game.Wave.Tests.A.B: hỏng", "editor-test:t1")
        w2 = wk.load(root, w2["id"])
        check("fix: broken tests after the load give the job one fix round", (w2.get("test_fixes"), "A.B" in w2.get("pending", "")), (1, True))
        ss.tell({"session": "@Worker", "job": w2["id"]}, "Game.Wave.Tests.A.B: vẫn hỏng", "editor-test:t2")
        w2 = wk.load(root, w2["id"])
        check("fix: a second time stops the job with a report for CodeLead",
              (w2["state"], len(list((root / "Management" / "reports").glob("*-Worker-Wave*.md")))), ("needs-codelead", 1))
        w3 = wk.queue(root, "Hud", task="y", now=now + 12)
        t = et.add(root, "@Worker", ["refresh", "clean"], [], [], now=now + 12, job=w3["id"])
        ss.runner_turn = lambda prompt: {"clean": False, "errors": ["Hud.cs:3: CS0103: x"]}
        ss.load_run([t])
        check("fix: Unity's compile errors after a load of workers only: the job fixes them, no CodeLead ticket",
              ("CS0103" in wk.load(root, w3["id"]).get("pending", ""), [x["session"] for x in et.queue(root) if x.get("priority")]), (True, []))
        # a question answered on Manager's side resumes the job
        w4 = wk.queue(root, "Save", task="z", now=now + 14)
        qid = wk.ask_human(root, w4, {"summary": "Save", "question": "A hay B?"})
        ss.ask.answer(root, qid, "A")
        ss.worker_tick(now + 15)
        check("ask: the answer is given to the job, once", ("A" in wk.load(root, w4["id"]).get("answered_text", wk.load(root, w4["id"]).get("pending", "")) or
              any(x[0] == w4["id"] and x[2] and "A" in x[2] for x in started), wk.load(root, w4["id"]).get("answered")), (True, [qid]))
        # the page
        res = ss.worker_call("run", {"feature": "Hud", "task": "làm từ trang"})
        check("page: run queues a job for a feature", (res["ok"], wk.load(root, res["id"])["source"]), (True, "người"))
        check("page: a feature that does not exist is refused", ss.worker_call("run", {"feature": "Nope"})["ok"], False)
        check("page: resume sets the message for the next tick", (ss.worker_call("resume", {"id": res["id"], "message": "tiếp"})["ok"],
              wk.load(root, res["id"])["pending"]), (True, "tiếp"))
        check("page: the view lists the jobs", len(ss.workers_view()["jobs"]) >= 6)
        check("page: the default model is set from the page", (ss.worker_call("defaults", {"model": "sonnet", "effort": "low"})["ok"],
              ss.workers_view()["defaults"]), (True, {"model": "sonnet", "effort": "low"}))
        check("page: a bad model is refused", ss.worker_call("defaults", {"model": "x", "effort": "low"})["ok"], False)
        r = ss.worker_call("run", {"feature": "Hud", "task": "thử model", "model": "opus"})
        check("page: a job run from the page keeps its model, the effort from the default",
              (ss.worker.load(root, r["id"])["model"], ss.worker.load(root, r["id"])["effort"]), ("opus", "low"))
        # the play job starts on its Editor grant
        started.clear()
        for x in et.tickets(root, ("waiting", "granted")):
            if x["session"] != "@UnityAutoPlay":
                et.drop(root, x["session"], job=x.get("job"))
        for x in et.holder(root):
            et.release(root, x["session"])
        ss.editor_tick(now + 20)
        check("play: the grant of its ticket starts the play turn", [(x[0], x[1]) for x in started], [(play["id"], "play")])
    finally:
        (ss.ROOT, ss.start_worker, ss.running_jobs, ss.threads, ss.live_sessions, started_state, wk.open_requirements,
         ss.runner_turn, ss.guard.STORE_DIR) = real
        ss.WORKER_STARTED.update(started_state)
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(store, ignore_errors=True)


def activity_cases(check):
    """Tab "Đang chạy" (người duyệt 2026-11-27): the last turn, the recent calls and the tokens from a transcript; a
    running worker with its subagent; a finished job under done."""
    home = Path(tempfile.mkdtemp(prefix="activity-home-"))
    root = Path(tempfile.mkdtemp(prefix="activity-root-"))
    real = (ss.ROOT, ss.threads, ss.live_sessions, ss.running_jobs, ss.PROJECTS_HOME)
    def ts(sec):
        return time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime(sec))
    def write(p, rows):
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("\n".join(json.dumps(r, separators=(",", ":")) for r in rows) + "\n", encoding="utf-8")
    try:
        ss.ROOT = root
        ss.PROJECTS_HOME = home / "projects"
        now = time.time()
        use = {"input_tokens": 10, "output_tokens": 2, "cache_read_input_tokens": 100, "cache_creation_input_tokens": 0}
        rows = [
            {"type": "user", "timestamp": ts(now - 900), "message": {"role": "user", "content": "lượt cũ"}},
            {"type": "assistant", "timestamp": ts(now - 890), "message": {"id": "a0", "role": "assistant", "model": "claude-opus-5-5", "usage": use,
             "content": [{"type": "tool_use", "id": "t0", "name": "Read", "input": {"file_path": "/x/Old.cs"}}]}},
            {"type": "user", "timestamp": ts(now - 120), "message": {"role": "user", "content": "làm Hero"}},
            {"type": "assistant", "timestamp": ts(now - 100), "message": {"id": "a1", "role": "assistant", "model": "claude-opus-5-5", "usage": use,
             "content": [{"type": "tool_use", "id": "t1", "name": "Edit", "input": {"file_path": "/w/Assets/HeroFakes.cs"}}]}},
            {"type": "user", "timestamp": ts(now - 90), "message": {"role": "user", "content": [{"type": "tool_result", "tool_use_id": "t1", "content": "ok"}]}},
            {"type": "assistant", "timestamp": ts(now - 80), "message": {"id": "a2", "role": "assistant", "model": "claude-opus-5-5", "usage": use,
             "content": [{"type": "tool_use", "id": "t2", "name": "Bash", "input": {"command": "python x.py", "description": "Chạy kiểm"}}]}},
        ]
        wt = home / ".coresimple-work" / "w20261127-100000-hero"
        sid = "11111111-2222-3333-4444-555555555555"
        tp = ss.project_dir(wt) / f"{sid}.jsonl"
        write(tp, rows)
        info = ss.tail_info(tp)
        check("activity: the turn starts at the last prompt, not at a tool result", int(info["start"]), int(now - 120))
        check("activity: the newest call first, the one without a result marked open",
              [(t["text"], t["open"]) for t in info["tools"]], [("Bash Chạy kiểm", True), ("Edit HeroFakes.cs", False)])
        check("activity: the turn's tokens count only the answers since its prompt", info["turn_cost"], 2 * (10 + 10 + 10))
        sub = tp.with_suffix("") / "subagents"
        write(sub / "agent-a1.jsonl", rows[2:4])
        (sub / "agent-a1.meta.json").write_text(json.dumps({"agentType": "Explore", "description": "Tìm test", "model": "claude-sonnet-5-5", "finished": False}), encoding="utf-8")
        write(sub / "agent-a2.jsonl", rows[2:4])
        (sub / "agent-a2.meta.json").write_text(json.dumps({"agentType": "Plan", "finished": True}), encoding="utf-8")
        jobs = root / "Management" / "workers"
        for jid, state, extra in (("w20261127-100000-hero", "running", {"session": sid, "worktree": str(wt)}),
                                  ("w20261127-090000-run", "applied", {"turns": [{"at": now - 3000, "seconds": 300, "cost_usd": 1.5}]})):
            (jobs / jid).mkdir(parents=True)
            (jobs / jid / "job.json").write_text(json.dumps(dict({"id": jid, "feature": jid.split("-")[-1].title(), "kind": "code",
                                                  "state": state, "model": "opus", "effort": "medium", "task": "làm việc",
                                                  "created": now - 3600, "updated": now - 60}, **extra)), encoding="utf-8")
        ss.threads, ss.live_sessions = (lambda include_unnamed=False: []), (lambda fresh=False: {})
        ss.running_jobs = lambda: {"w20261127-100000-hero": None}
        check("activity: done jobs are only counted until the page opens them", (ss.activity_view(now)["done"], ss.activity_view(now)["done_count"]), ([], 1))
        v = ss.activity_view(now, with_done=True)
        w = next((r for r in v["rows"] if r["id"] == "w20261127-100000-hero"), {})
        check("activity: a running worker shows model, what it does, and its live tokens",
              (w.get("name"), w.get("model"), w.get("state"), w.get("doing"), w.get("tokens", 0) > 0), ("W Hero", "opus", "chạy", "Bash Chạy kiểm", True))
        check("activity: only the subagent still running is listed under it", [s["name"] for s in w.get("subs", [])], ["Explore"])
        check("activity: the finished job is under done with its time and cost",
              [(d["name"], d["seconds"], d["cost"]) for d in v["done"]], [("W Run", 300, 1.5)])
        ss.editor_ticket.add(root, "@Worker", ["refresh", "clean", "test"], [], [], now=now, job="w20261127-090000-run")
        v2 = ss.activity_view(now, with_done=True)
        check("activity: a job whose Editor ticket is still open stays in the table, waiting for the load and tests",
              ([r["state"] for r in v2["rows"] if r["id"] == "w20261127-090000-run"], [d["id"] for d in v2["done"]]),
              (["chờ Editor để nạp và test"], []))
        check("activity: today's worker cost sums the turns of today", v["totals"]["cost_today"],
              1.5 if time.strftime("%Y-%m-%d", time.localtime(now - 3000)) == time.strftime("%Y-%m-%d", time.localtime(now)) else 0)
    finally:
        (ss.ROOT, ss.threads, ss.live_sessions, ss.running_jobs, ss.PROJECTS_HOME) = real
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(root, ignore_errors=True)


def recover_cases(check):
    """A server that starts again finds jobs it left running (người quyết 2026-11-27): it gives the Editor back,
    starts nothing again, and writes one letter to Manager."""
    root = Path(tempfile.mkdtemp(prefix="recover-"))
    store = tempfile.mkdtemp(prefix="recover-store-")
    real = (ss.ROOT, ss.guard.STORE_DIR, ss.running_jobs, dict(ss.WORKER_STARTED), ss.threads, ss.relay, ss.threading.Thread)
    sent = []
    class NowThread:      # the relay runs here at once, so the test sees it
        def __init__(self, target, args=(), daemon=None):
            self.go = lambda: target(*args)
        def start(self):
            self.go()
    try:
        ss.ROOT = root
        ss.guard.STORE_DIR = store
        ss.running_jobs = lambda: {}
        ss.threads = lambda include_unnamed=False: [{"id": "m1", "title": "Manager", "binding": "@Manager", "live": "interactive"}]
        ss.relay = lambda t, text: sent.append((t["title"], text)) or "ok"
        ss.threading.Thread = NowThread
        et, wk = ss.editor_ticket, ss.worker
        play = wk.queue_play(root, "chơi thử CheatPanel", source="@CodeLead", now=1)
        et.grant_next(root, now=2)
        wk.save(root, dict(play, state="running", session="s-play"))
        ss.guard.write_lock(store, {"session": "s-play", "who": "@UnityAutoPlay", "until": time.time() + 600, "scene": "", "prefab": ""})
        code = wk.save(root, dict(wk.queue(root, "Run", task="x", now=3), state="running", session="s-code"))
        ss.WORKER_STARTED["done"] = False
        did = ss.worker_tick(now=10)
        jobs = {j["id"]: j for j in ss.worker_jobs()}
        check("recover: the play test's ticket and the guard's lock are given back",
              (et.holder(root), ss.guard.read_lock(store)), ([], None))
        check("recover: nothing is started again: no new play test, no resume, the code job is not run",
              (len(jobs), jobs[play["id"]]["state"], jobs[code["id"]]["state"], jobs[code["id"]].get("pending"),
               [d for d in did if d and d[0] == "start"]), (2, "interrupted", "interrupted", None, []))
        letters = sorted((root / "Management" / "reports").glob("*-Server*.md"))
        text = letters[0].read_text(encoding="utf-8") if letters else ""
        check("recover: one letter to Manager lists both jobs", (len(letters), play["id"] in text, code["id"] in text), (1, True, True))
        check("recover: Manager is shown the server's letters",
              ss.bind_session.report_role(letters[0].name) in ss.bind_session.MANAGER_REPORT_ROLES if letters else False, True)
        check("recover: Manager open in VS Code is messaged at once, naming the letter",
              ([x[0] for x in sent], bool(letters) and letters[0].name in sent[0][1] if sent else False), (["Manager"], True))
    finally:
        (ss.ROOT, ss.guard.STORE_DIR, ss.running_jobs) = real[:3]
        (ss.threads, ss.relay, ss.threading.Thread) = real[4:]
        ss.WORKER_STARTED.clear()
        ss.WORKER_STARTED.update(real[3])
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(store, ignore_errors=True)


def after_tests_cases(check):
    """The end of a worker job's chain: a report after the tests, one message to a live CodeLead (người quyết
    2026-11-27: worker làm xong phải báo, không thì không tự động được)."""
    root = Path(tempfile.mkdtemp(prefix="after-tests-"))
    sent = []
    real = (ss.ROOT, ss.threads, ss.relay, set(ss.NOTIFIED))
    try:
        ss.ROOT = root
        ss.NOTIFIED.clear()
        lead = {"id": "c1", "title": "CodeLead", "binding": "@CodeLead", "live": "background"}
        ss.threads = lambda include_unnamed=False: [lead]
        ss.relay = lambda t, text: sent.append((t["title"], text)) or "ok"
        for jid, state in (("w20261127-100000-run", "applied"), ("w20261127-100001-wave", "checks-failed")):
            ss.worker.save(root, {"id": jid, "feature": jid.split("-")[-1].title(), "kind": "code", "state": state, "task": "x"})
        r = ss.after_tests("w20261127-100000-run", "Test: 3 qua, 0 hỏng · 2/2 yêu cầu ghi done (chờ CodeLead kiểm)", 2, [])
        reports = sorted((root / "Management" / "reports").glob("*Worker-Run*.md"))
        check("after tests: a report 'xong' with the test line", (len(reports), "Trạng thái: xong" in reports[-1].read_text(encoding="utf-8"),
              "2/2 yêu cầu ghi done" in reports[-1].read_text(encoding="utf-8")) if reports else 0, (1, True, True))
        check("after tests: the live CodeLead gets one message to check them", (r[-1], [x[0] for x in sent], "LÔ TEST XONG · Run" in sent[0][1] if sent else False),
              (("notified", "tests-done:w20261127-100000-run"), ["CodeLead"], True))
        ss.after_tests("w20261127-100000-run", "lại", 2, [])
        check("after tests: never twice for the same job", len(sent), 1)
        ss.after_tests("w20261127-100000-run", "Test: 1 qua, 1 hỏng", 1, [{"name": "x"}])
        check("after tests: broken tests (worker fixes once) report 'dở' and wake nobody", len(sent), 1)
        check("stopped: a job stopped at its gates tells CodeLead", (ss.stopped("w20261127-100001-wave")[0], "WORKER DỪNG · Wave" in sent[-1][1]), ("notified", True))
        check("stopped: a job that ended well tells nobody here", ss.stopped("w20261127-100000-run"), None)
        lead["live"] = ""
        ss.NOTIFIED.clear()
        check("notify: CodeLead not live, nothing is sent (the report waits for its next message)",
              ss.notify_codelead("x", "k")[0], "codelead-not-live")
    finally:
        (ss.ROOT, ss.threads, ss.relay) = real[:3]
        ss.NOTIFIED.clear()
        ss.NOTIFIED.update(real[3])
        shutil.rmtree(root, ignore_errors=True)


def worker_ticket_cases(check):
    """A worker's ticket (worker.py): one job, the requirements of several features; nobody is messaged."""
    et = ss.editor_ticket
    root = Path(tempfile.mkdtemp(prefix="worker-ticket-"))
    marks = []
    real = (ss.ROOT, ss.runner_turn, ss.test_assemblies, ss.reqbook.mark)
    try:
        ss.ROOT = root
        ss.test_assemblies = lambda group: ["Game.Run.Tests", "Game.Wave.Tests"]
        ss.reqbook.mark = lambda r, f, ref, action, by, tests=None, **k: marks.append((f, ref, action))
        ss.runner_turn = lambda prompt: {"ok": False, "passed": ["Game.Run.Tests.RunTests.Starts"],
                                         "failed": [{"name": "Game.Wave.Tests.WaveTests.Ends", "message": "sai"}]}
        t = et.add(root, "@Worker", ["refresh", "clean"], [], [
            {"ref": "run-rules#start", "tests": ["RunTests.Starts"], "feature": "Run"},
            {"ref": "wave-rules#end", "tests": ["WaveTests.Ends"], "feature": "Wave"}], now=1, job="w1-run")
        check("worker ticket: the features come from its requirements", ss.ticket_features(t), ["Run", "Wave"])
        def broken_runner(prompt):
            raise RuntimeError("Invalid MCP configuration")
        ss.runner_turn = broken_runner
        et.grant_next(root, now=2)
        ss.load_run([t])
        left = et.find(root, "@Worker")
        check("load: a runner that fails puts the ticket back with every need, no CodeLead fix",
              (left["state"], left["code"] & 0b111, [x["session"] for x in et.queue(root)]), ("waiting", 0b111, ["@Worker"]))
        et.grant_next(root, now=3)
        ss.runner_turn = lambda prompt: {"clean": False, "errors": ["UNITY: không kết nối được"]}
        ss.load_run([left])
        left = et.find(root, "@Worker")
        check("load: Unity unreachable is a runner failure too, not compile errors",
              (left["state"], [x["session"] for x in et.queue(root)]), ("waiting", ["@Worker"]))
        check("load: after a runner failure the server waits before the next load", ss.editor_tick(now=time.time()), [])
        ss.RUNNER["backoff_until"] = 0
        ss.runner_turn = lambda prompt: {"ok": False, "passed": ["Game.Run.Tests.RunTests.Starts"],
                                         "failed": [{"name": "Game.Wave.Tests.WaveTests.Ends", "message": "sai"}]}
        t = left
        did = ss.test_run([t])
        check("worker ticket: the passing requirement is done in the book of its own feature",
              marks, [("Run", "run-rules#start", "done")])
        log = (root / "Management" / "workers" / "w1-run" / "events.log")
        check("worker ticket: the failing test goes to the job's events.log, nobody is messaged",
              (log.is_file() and "WaveTests.Ends" in log.read_text(encoding="utf-8"),
               any(step[0] == "logged" for step in did)), (True, True))
        check("worker ticket: a failing test reports nothing and wakes nobody when there is no such job",
              [d for d in did if d and d[0] in ("reported", "no-job")], [("no-job", "w1-run")])
        check("worker ticket: the test result goes to the job's events.log too (passed and failed, requirements done)",
              any("Test: 1 qua, 1 hỏng · 1/2 yêu cầu ghi done" in e["text"] for e in ss.job_events("w1-run")), True)
    finally:
        (ss.ROOT, ss.runner_turn, ss.test_assemblies, ss.reqbook.mark) = real
        shutil.rmtree(root, ignore_errors=True)


def editor_cases(check):
    """Editor tickets (người quyết 2026-11-27): one grant at a time, loads together, tests at the end of a round."""
    et = ss.editor_ticket
    root = Path(tempfile.mkdtemp(prefix="editor-"))
    store = tempfile.mkdtemp(prefix="editor-store-")
    told, runs, marks = [], [], []
    real = (ss.ROOT, ss.tell, ss.runner_turn, ss.run_in_background, ss.test_assemblies, ss.reqbook.mark, ss.guard.STORE_DIR)
    try:
        ss.ROOT = root
        ss.guard.STORE_DIR = store
        ss.tell = lambda t, text, key: told.append((t["session"], text, key)) or ("told", t["session"])
        ss.run_in_background = lambda fn, group: runs.append(list(fn(group)))
        ss.test_assemblies = lambda group: ["Game.Wallet.Tests"]
        ss.reqbook.mark = lambda r, f, ref, action, by, tests=None, **k: marks.append((f, ref, action, tuple(tests or ())))
        ss.runner_turn = lambda prompt: {"clean": True, "errors": []}      # never a real claude turn in a test
        check("editor: nothing waits, nothing happens", ss.editor_tick(now=1), [])
        et.add(root, "Home", [], ["tạo HomeMain.prefab"], now=1)
        ss.editor_tick(now=2)
        check("editor: a ticket of its own work is granted and its chat told",
              ([t["session"] for t in et.holder(root)], told[-1][0], "tạo HomeMain.prefab" in told[-1][1]), (["Home"], "Home", True))
        check("editor: nothing else while it holds the Editor", ss.editor_tick(now=3), [])
        et.add(root, "Wallet", ["refresh", "clean"], now=4)
        et.add(root, "Hud", ["refresh", "clean"], now=5)
        et.add(root, "Save", reqs=[{"ref": "feature-save/save-io/io-rules#write", "tests": ["Game.Save.Tests.SaveTests.Writes"]}], now=6)
        did = ss.editor_tick(now=2 + ss.GRANT_UNUSED_SECONDS + 1)
        check("editor: a turn granted and never used is given back", ("unused", "Home") in did, True)
        # Home was requeued at the end: the code-only tickets go first as one load
        check("editor: the load of every code-only ticket, compiled once, clean",
              (runs[-1][-1][0], runs[-1][-1][2], sorted(k for _, _, k in told if k.startswith("editor-load"))[:1] != []),
              ("load", "sạch", True))
        check("editor: the load released its tickets", [t["session"] for t in et.holder(root)], [])
        et.drop(root, "Home")
        ss.runner_turn = lambda prompt: {"ok": True, "failed": [], "passed": ["Game.Save.Tests.SaveTests.Writes"]}
        ss.editor_tick(now=10 ** 9)
        check("editor: the test run marks done the requirements whose tests passed",
              marks, [("Save", "feature-save/save-io/io-rules#write", "done", ("SaveTests.Writes",))])
        check("editor: after the test run the queue is empty", (et.queue(root), et.holder(root)), ([], []))
        et.add(root, "Wallet", ["refresh", "clean"], now=10 ** 9 + 1)
        ss.runner_turn = lambda prompt: {"clean": False, "errors": ["UnityProject/Assets/Features/Wallet/W.cs:3: CS0103: x"]}
        ss.editor_tick(now=10 ** 9 + 2)
        q = et.queue(root)
        check("editor: a load left with compile errors makes a CodeLead fix ticket first, with the errors",
              (q[0]["session"], q[0]["priority"], "CS0103" in " ".join(q[0]["notes"])), ("@CodeLead", True, True))
        ss.guard.write_lock(store, {"session": "x", "who": "Wallet", "until": time.time() + 60})
        check("editor: a chat using the Editor directly: no grant meanwhile", ss.editor_tick(now=10 ** 9 + 3), [])
        ss.guard.release_lock(store)
        with open(os.path.join(store, ss.guard.HUMAN_EDITOR_FILE), "w", encoding="utf-8") as f:
            json.dump({"since": 1, "reason": "play"}, f)
        check("editor: the human uses the Editor: no grant", ss.editor_tick(now=10 ** 9 + 4), [])
    finally:
        (ss.ROOT, ss.tell, ss.runner_turn, ss.run_in_background, ss.test_assemblies, ss.reqbook.mark, ss.guard.STORE_DIR) = real
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(store, ignore_errors=True)


def main():
    store = tempfile.mkdtemp(prefix="setup-store-")
    profile_cases(check)
    ss.PROFILES = False      # the cases below check the plain CLI lines

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
        ss.threads = lambda include_unnamed=False: listed
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
              ["CLAUDE.md", ".claude/rules/report.md", ".claude/rules/codelead.md", ".claude/rules/unity.md",
               ".claude/skills/codelead-dispatch/SKILL.md", ".claude/skills/codelead-verify-batch/SKILL.md",
               ".claude/skills/codelead-scaffold-feature/SKILL.md", ".claude/skills/codelead-resolve-issue/SKILL.md",
               ".claude/skills/codelead-wiring/SKILL.md"])
        check("reread: a role with skills is told about them too (each SKILL.md is followed for changes)",
              ss.bind_session.rule_files("@UIDesigner", str(ss.ROOT)),
              ["CLAUDE.md", ".claude/rules/report.md", ".claude/rules/uidesigner.md",
               ".claude/skills/ui-layout-screen/SKILL.md", ".claude/skills/ui-frame-syntax/SKILL.md",
               ".claude/skills/ui-claude-concept-order/SKILL.md", ".claude/skills/gd-round-4/SKILL.md",
               ".claude/skills/gd-node-format/SKILL.md"])
        check("reread: @GameDesigner follows the skills split out of gamedesigner.md",
              [r for r in ss.bind_session.rule_files("@GameDesigner", str(ss.ROOT))
               if any(n in r for n in ("gd-node-format", "gd-scoring", "gd-glossary", "gd-art-concept", "gd-user-control"))],
              [f".claude/skills/{n}/SKILL.md" for n in ("gd-node-format", "gd-scoring", "gd-glossary", "gd-art-concept", "gd-user-control")])
        heads = []
        for n in ("gd-node-format", "gd-scoring", "gd-glossary", "gd-art-concept", "gd-user-control", "codelead-wiring"):
            text = (ss.ROOT / ".claude" / "skills" / n / "SKILL.md").read_text(encoding="utf-8")
            heads.append((text.startswith("---\nname: " + n + "\ndescription: "), "Dùng khi" in text.split("---")[1] or "Dùng trước" in text.split("---")[1]))
        check("skills split out of the rules: each has a name and says when it is used", heads, [(True, True)] * 6)
        check("reread: a feature is a worker now, no skill of its own", [r for r in ss.bind_session.rule_files("Wallet", str(ss.ROOT)) if "skills" in r], [])
        sp = ss.reread_prompt([".claude/rules/uidesigner.md", ".claude/skills/ui-frame-syntax/SKILL.md"])
        check("reread: a changed rule file is Read again, a changed skill is only named (called again when used)",
              ("Read lại toàn bộ đúng các file đó" in sp, ".claude/rules/uidesigner.md" in sp, "Skill sau vừa đổi: ui-frame-syntax" in sp,
               "skills/ui-frame-syntax/SKILL.md" in sp), (True, True, True, False))
        sp = ss.reread_prompt([".claude/skills/gd-round-4/SKILL.md"])
        check("reread: only a skill changed: nothing to Read", ("Read lại" in sp, "gd-round-4" in sp), (False, True))
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
        # renew: with the PIN; a clear the server does itself (no turn), bound again, the next turn reads the rules
        real_pin, real_tdir = ss.bind_session.check_pin, ss.transcripts_dir
        ss.bind_session.check_pin = lambda store_, pin: "" if pin == "1234" else "PIN sai"
        tdir = Path(tempfile.mkdtemp(prefix="renew-t-"))
        ss.transcripts_dir = lambda: tdir
        lines = [{"type": "custom-title", "customTitle": "CodeLead", "sessionId": "aaaa1111"},
                 {"type": "agent-name", "agentName": "CodeLead", "sessionId": "aaaa1111"},
                 {"type": "session-owner", "accountUuid": "x", "sessionId": "aaaa1111"},
                 {"type": "queue-operation", "operation": "enqueue"},
                 {"type": "user", "uuid": "u1", "parentUuid": None, "sessionId": "aaaa1111", "entrypoint": "claude-vscode",
                  "message": {"role": "user", "content": "Thread CodeLead được tạo bởi trang Setup. Bí mật cũ"}},
                 {"type": "assistant", "uuid": "a1", "parentUuid": "u1", "message": {"content": [{"type": "text", "text": "đã đọc"}]}},
                 {"type": "custom-title", "customTitle": "CodeLead", "sessionId": "aaaa1111"}]
        (tdir / "aaaa1111.jsonl").write_text("".join(json.dumps(x, ensure_ascii=False) + "\n" for x in lines), encoding="utf-8")
        (tdir / "aaaa1111").mkdir()
        (tdir / "aaaa1111" / "tool-results.txt").write_text("x", encoding="utf-8")
        try:
            ss.run_claude = fake
            before = len(calls)
            for pin in ("", "0000", None, 1234):
                res = ss.renew("aaaa1111", pin)
                check(f"renew: a wrong PIN is refused ({pin!r})", (res["ok"], res["error"], res.get("pin")), (False, "PIN sai", True))
            check("renew: a wrong PIN runs nothing, binds nothing, moves nothing",
                  (len(calls), ss.guard.bound_feature("aaaa1111", store) or "", (tdir / ss.RENEWED).exists()), (before, "", False))
            res = ss.renew("cccc3333", "1234")
            check("renew: a thread that is not bound is refused before the PIN is looked at",
                  (res["ok"], res["error"], len(calls)), (False, "thread chưa gán FEATURE: chạy Setup trước", before))
            ss.bind_session.write_json(ss.bind_session.rules_read_path("aaaa1111", store), {"role": "@CodeLead"})
            res = ss.renew("aaaa1111", "1234")
            kept = [json.loads(x) for x in (tdir / "aaaa1111.jsonl").read_text(encoding="utf-8").splitlines()]
            moved = list((tdir / ss.RENEWED).glob("*-aaaa1111"))
            check("renew: no model turn at all (no /compact, no reread)", (res["ok"], len(calls) - before), (True, 0))
            check("renew: the old transcript and its folder are moved to .renewed",
                  (len(moved), (moved[0] / "aaaa1111.jsonl").is_file() if moved else False,
                   (moved[0] / "aaaa1111" / "tool-results.txt").is_file() if moved else False, (tdir / "aaaa1111").exists()),
                  (1, True, True, False))
            check("renew: the new transcript keeps only the name lines and the first user message, as the renew note",
                  ([x["type"] for x in kept], kept[3]["message"]["content"], kept[3]["uuid"], kept[3]["sessionId"],
                   "Bí mật cũ" in (tdir / "aaaa1111.jsonl").read_text(encoding="utf-8"),
                   "đã đọc" in (tdir / "aaaa1111.jsonl").read_text(encoding="utf-8")),
                  (["custom-title", "agent-name", "session-owner", "user", "assistant"], ss.RENEW_NOTE, "u1", "aaaa1111", False, False))
            answer = kept[4]
            check("renew: the user message is answered (VS Code opens a new session for a transcript with no answer)",
                  (answer["parentUuid"], answer["sessionId"], answer["entrypoint"], answer["message"]["model"],
                   answer["message"]["stop_reason"], answer["message"]["content"], answer["uuid"] != "a1"),
                  ("u1", "aaaa1111", "claude-vscode", ss.SYNTHETIC_MODEL, "end_turn", [{"type": "text", "text": ss.RENEW_REPLY}], True))
            check("renew: bound again to the FEATURE it had; the guard line will tell it to read the rules on its next turn",
                  (ss.guard.bound_feature("aaaa1111", store), os.path.exists(ss.bind_session.rules_read_path("aaaa1111", store)),
                   "lượt sau" in res["answer"]), ("@CodeLead", False, True))
            forget()
            res = ss.renew("aaaa1111", "1234")
            check("renew: pressing it again keeps the same short transcript and costs nothing",
                  (res["ok"], len(calls) - before, [json.loads(x)["type"] for x in (tdir / "aaaa1111.jsonl").read_text(encoding="utf-8").splitlines()]),
                  (True, 0, ["custom-title", "agent-name", "session-owner", "user", "assistant"]))
            check("renew: the page's reread after a renew runs no turn (the next turn reads every rule file anyway)",
                  (ss.reread("aaaa1111").get("skipped"), len(calls) - before), (True, 0))
            # a thread in the middle of a turn is only marked: cleared by the courier before its next job
            listed[1] = dict(listed[1], live_status="busy")
            res = ss.renew("bbbb2222", "0000")
            check("renew (marked): a wrong PIN marks nothing", (res["ok"], ss.renew_pending("bbbb2222")), (False, False))
            res = ss.renew("bbbb2222", "1234")
            check("renew (marked): a thread in a turn runs no turn, it is marked and told so",
                  (res["ok"], res.get("pending"), len(calls) - before, ss.renew_pending("bbbb2222"), "việc kế tiếp" in res["answer"]),
                  (True, True, 0, True, True))
            ss.clear_renew_mark("bbbb2222")
            check("renew (marked): the mark can be cleared", ss.renew_pending("bbbb2222"), False)
            listed[1] = dict(listed[1], live_status="")
        finally:
            ss.bind_session.check_pin, ss.transcripts_dir = real_pin, real_tdir
            shutil.rmtree(tdir, ignore_errors=True)
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
    delete_outside_cases(check)
    unnamed_cases(check)
    usage_cases(check)
    model_choice_cases(check)
    editor_cases(check)
    worker_ticket_cases(check)
    worker_server_cases(check)
    activity_cases(check)
    after_tests_cases(check)
    recover_cases(check)
    ss.guard.STORE_DIR = tempfile.mkdtemp(prefix="courier-store-")      # never the real Editor lock: a tester may be playing
    cli_cases(check)
    courier_cases(check)
    watch_cases(check)
    takeover_cases(check)
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
