#!/usr/bin/env python3
"""Local server for the management page's Setup tab: creates the chat threads and binds them, with a PIN.

    python Management/tools/setup_server.py [--port 8770]

Then open http://127.0.0.1:8770/ (the script prints the link). Run it yourself; it is not for agents.

- Listens on 127.0.0.1 only. API calls need the token that is put into the page this server serves, so
  another website cannot call it. The PIN is checked with bind_session.check_pin (wrong PINs count toward
  the lock), never logged, never stored.
- A thread is a Claude Code session of this project; its name is its custom title. Archived threads (hidden in
  the VS Code extension) are ignored.
- For each name of the chosen set: every non-archived thread with that name is bound; when there is none, a
  new one is created with `claude -n <name> --session-id <id> --model <m> --effort <e> -p ...` (the page picks
  model and effort; default opus / medium) and then bound. Binding writes the same
  file bind_session.py writes, so the PIN never goes through a chat (a bind command typed as the first message
  of a chat can end up in the thread's generated title).
- Game design tab: GET /api/gamedesign re-reads Management/game-design. Without the PIN (the page token is
  enough): POST /api/gamedesign/promote copies one idea node into live (gamedesign.promote), /answers stores the
  human's answers to @GameDesigner's questions, /review the human's score and feedback per idea, /priority the
  human's importance score of a node, /progress the progress of a leaf. With the PIN: /sync copies every passing node into live at once
  (gamedesign.sync_live), /mode switches Full Auto / User control. Nothing reaches live without promote or sync.
"""
import argparse
import html
import json
import os
import re
import secrets
import sqlite3
import subprocess
import sys
import time
import uuid
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / ".claude" / "hooks"))
import bind_session  # noqa: E402
import gamedesign  # noqa: E402
import guard  # noqa: E402
import sync_page  # noqa: E402
import validate  # noqa: E402

BASIC = ["@human", "@Manager", "@CodeLead", "@GameDesigner", "@Core", "@Composition"]
ROLE_GROUPS = {"@human": "Master", "@Manager": "Leaders", "@CodeLead": "Leaders", "@GameDesigner": "Leaders",
               "@Core": "Basic", "@Composition": "Basic"}
STATIC_EXT = (".css", ".js", ".png", ".svg", ".jpg", ".jpeg", ".gif", ".webp")


def group_of(name):
    """Master, Leaders, Basic, Services or Features for a session name; "" for a name outside the setup."""
    if name in ROLE_GROUPS:
        return ROLE_GROUPS[name]
    if name.startswith("@") or not guard.valid_feature(name, str(ROOT)):
        return ""
    return "Services" if name.startswith("_Services/") else "Features"
# Model aliases the claude CLI takes, and its effort levels. New threads default to opus / medium.
MODELS = ["opus", "sonnet", "haiku", "fable"]
EFFORTS = ["low", "medium", "high", "xhigh", "max"]
DEFAULT_MODEL, DEFAULT_EFFORT = "opus", "medium"
EFFORT_RX = re.compile(r'"effort"\s*:\s*"([a-z]+)"')
# The VS Code extension lists only sessions whose transcript says they were started from it. `claude -p` run from
# a plain terminal records "sdk-cli" and the thread never shows up there, so create() sets the entry point itself
# and a thread recorded only as "sdk-cli" ("unlisted") is skipped like an archived one.
ENTRY_RX = re.compile(r'"entrypoint"\s*:\s*"([^"]+)"')
VSCODE_ENTRY, HEADLESS_ENTRY = "claude-vscode", "sdk-cli"
VSCODE_STATE = Path.home() / "AppData" / "Roaming" / "Code" / "User" / "globalStorage" / "state.vscdb"
TITLE_RX = re.compile(r'"type"\s*:\s*"custom-title".*?"customTitle"\s*:\s*"((?:[^"\\]|\\.)*)"')
MAX_BODY = 4096
# POST paths and the largest body each takes. Answers carry free text for every question of a node.
POST_PATHS = {"/api/setup": MAX_BODY, "/api/gamedesign/promote": MAX_BODY, "/api/gamedesign/mode": MAX_BODY,
              "/api/gamedesign/answers": 64 * 1024, "/api/gamedesign/review": 64 * 1024, "/api/gamedesign/sync": MAX_BODY,
              "/api/gamedesign/priority": MAX_BODY, "/api/gamedesign/progress": 64 * 1024}


def transcripts_dir():
    """~/.claude/projects/<project path with every non-alphanumeric character as '-'>."""
    return Path.home() / ".claude" / "projects" / re.sub(r"[^A-Za-z0-9]", "-", str(ROOT))


def archived_ids():
    """Session ids hidden (archived) in the VS Code extension; empty when its state cannot be read."""
    try:
        con = sqlite3.connect(f"file:{VSCODE_STATE.as_posix()}?mode=ro", uri=True)
        try:
            row = con.execute("select value from ItemTable where key = 'Anthropic.claude-code'").fetchone()
        finally:
            con.close()
        value = row[0] if row else "{}"
        data = json.loads(value if isinstance(value, str) else value.decode("utf-8"))
        return set(data.get("hiddenSessionIds") or [])
    except Exception:
        return set()


def threads():
    """[{id, title, group, archived, binding, model, effort, modified}] for this project's sessions that have a name.
    model: of the last answer; effort: of the last request (both as the transcript recorded them)."""
    hidden = archived_ids()
    out = []
    folder = transcripts_dir()
    for p in folder.glob("*.jsonl") if folder.is_dir() else []:
        title, model, effort, entries = None, "", "", set()
        with open(p, encoding="utf-8", errors="replace") as f:
            for line in f:
                if '"entrypoint"' in line:
                    m = ENTRY_RX.search(line)
                    if m:
                        entries.add(m.group(1))
                if '"custom-title"' in line:
                    m = TITLE_RX.search(line)
                    if m:
                        title = json.loads(f'"{m.group(1)}"')
                elif line.startswith('{"') and '"type":"api-request' in line:
                    m = EFFORT_RX.search(line)
                    effort = m.group(1) if m else effort
                elif '"type":"assistant"' in line and '"model"' in line:
                    try:
                        model = (json.loads(line).get("message") or {}).get("model") or model
                    except ValueError:
                        pass
        if title:
            sid = p.stem
            out.append({"id": sid, "title": title, "group": group_of(title), "archived": sid in hidden,
                        "unlisted": entries == {HEADLESS_ENTRY},
                        "model": "" if model == "<synthetic>" else model, "effort": effort,
                        "binding": guard.bound_feature(sid, guard.STORE_DIR) or "",
                        "modified": time.strftime("%Y-%m-%d %H:%M", time.localtime(p.stat().st_mtime))})
    return sorted(out, key=lambda t: (t["title"].lower(), t["modified"]))


def sets():
    """The names of the basic and the full setup; the full one adds every service and feature folder."""
    cfg = json.loads((HERE / "rules.json").read_text(encoding="utf-8-sig"))
    ids = validate.Scan(ROOT, cfg)
    ids.discover()
    features = [f["id"] for f in ids.features]
    services = sorted(f for f in features if f.startswith("_Services/"))
    others = sorted(f for f in features if not f.startswith("_Services/"))
    full = BASIC + services + others
    return {"basic": BASIC, "full": full, "groups": {n: group_of(n) for n in full}}


def bind(session_id, value):
    bind_session.write_json(guard.binding_path(session_id, guard.STORE_DIR),
                            {"feature": value, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S"), "by": "setup_server"})


def create(name, model, effort):
    """New named thread on that model and effort; returns its session id. Costs one short answer."""
    sid = str(uuid.uuid4())
    prompt = f"Thread {name} được tạo bởi trang Setup. Chỉ trả lời đúng một chữ: Sẵn sàng."
    p = subprocess.run(["claude", "-n", name, "--session-id", sid, "--model", model, "--effort", effort, "-p", prompt],
                       cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8", errors="replace",
                       env=dict(os.environ, CLAUDE_CODE_ENTRYPOINT=VSCODE_ENTRY),
                       timeout=180, shell=(os.name == "nt"))
    if p.returncode != 0:
        raise RuntimeError((p.stderr or p.stdout or f"exit {p.returncode}").strip()[:300])
    return sid


def setup(pin, mode, model=DEFAULT_MODEL, effort=DEFAULT_EFFORT):
    names = sets().get(mode)
    if not names:
        return {"ok": False, "error": f"unknown mode '{mode}'"}
    if model not in MODELS or effort not in EFFORTS:      # checked before the PIN: a typo must not cost a try
        return {"ok": False, "error": f"model must be one of {MODELS}, effort one of {EFFORTS}"}
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return {"ok": False, "error": error}
    current = [t for t in threads() if not t["archived"] and not t["unlisted"]]
    results = []
    for name in names:
        same = [t for t in current if t["title"] == name]
        try:
            if same:
                for t in same:
                    bind(t["id"], name)
                results.append({"name": name, "action": "bound", "ids": [t["id"] for t in same]})
            else:
                sid = create(name, model, effort)
                bind(sid, name)
                results.append({"name": name, "action": "created", "ids": [sid], "model": model, "effort": effort})
        except Exception as ex:
            results.append({"name": name, "action": "error", "detail": str(ex)})
    return {"ok": True, "results": results}


def promote(path, deep):
    """The Chốt button of the Game design tab: copies one idea node (deep: its whole branch) into live. No PIN:
    the page token shows the request comes from the page this server served, and the guard lets no feature or
    role session run or call this server. gamedesign.promote refuses a node that does not pass."""
    try:
        written = gamedesign.promote(ROOT, path, deep)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "written": written, "data": sync_page.write_gamedesign(ROOT)}


def priority(path, score):
    """The importance buttons of a node: the human's score 0-10, or None to remove it. No PIN."""
    try:
        gamedesign.save_priority(ROOT, path, score)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "data": sync_page.write_gamedesign(ROOT)}


def progress(path, phase, state, note):
    """The progress cells of a leaf node: todo | doing | done for one phase, set by the human. No PIN."""
    try:
        gamedesign.save_progress(ROOT, path, phase, state, "người", note if isinstance(note, str) else None)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "data": sync_page.write_gamedesign(ROOT)}


def sync_live(pin):
    """The Sync button: every passing idea node that differs from live is copied into live. Needs the PIN: it
    rewrites many fixed nodes at once."""
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return {"ok": False, "error": error}
    try:
        written = gamedesign.sync_live(ROOT)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "written": written, "data": sync_page.write_gamedesign(ROOT)}


def set_mode(pin, mode):
    """The mode switch of the Game design tab (Full Auto / User control); needs the PIN."""
    if mode not in gamedesign.MODES:      # before the PIN: a bad request must not cost a try
        return {"ok": False, "error": f"mode phải là một trong {gamedesign.MODES}"}
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return {"ok": False, "error": error}
    gamedesign.write_mode(ROOT, mode)
    return {"ok": True, "data": sync_page.write_gamedesign(ROOT)}


def answer(path, answers):
    """The OK button of the questions panel: stores the human's choices beside the node. No PIN: it changes
    nothing in live, and the page token already shows the request comes from the page this server served."""
    try:
        count = gamedesign.save_answers(ROOT, path, answers)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "saved": count, "data": sync_page.write_gamedesign(ROOT)}


def review(path, items, table):
    """The Lưu button of a node's panel: the human's score (0-10) and feedback per idea, given in the idea or the
    live table. No PIN, like answers."""
    try:
        count = gamedesign.save_review(ROOT, path, items, table if isinstance(table, str) else "idea")
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "saved": count, "data": sync_page.write_gamedesign(ROOT)}


class Handler(SimpleHTTPRequestHandler):
    token = ""
    port = 0

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT / "Management"), **kwargs)

    def log_message(self, fmt, *args):      # never print request bodies; paths only
        sys.stderr.write(f"{self.command} {self.path.split('?')[0]}\n")

    def host_ok(self):
        return self.headers.get("Host", "") in (f"127.0.0.1:{self.port}", f"localhost:{self.port}")

    def api_ok(self):
        return self.host_ok() and secrets.compare_digest(self.headers.get("X-Setup-Token", ""), self.token)

    def send_json(self, code, value):
        body = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if not self.host_ok():
            return self.send_error(403)
        path = self.path.split("?")[0]
        if path.startswith("/api/"):
            if not self.api_ok():
                return self.send_error(403)
            if path == "/api/threads":
                return self.send_json(200, {"threads": threads(), "sets": sets(), "models": MODELS, "efforts": EFFORTS,
                                            "defaults": {"model": DEFAULT_MODEL, "effort": DEFAULT_EFFORT}})
            if path == "/api/gamedesign":
                return self.send_json(200, sync_page.write_gamedesign(ROOT))
            return self.send_error(404)
        if path in ("/", "/index.html"):
            sync_page.main()      # fresh project data each time the page is opened
            page = (ROOT / "Management" / "index.html").read_text(encoding="utf-8")
            inject = f'<script>window.CORESIMPLE_SETUP = {{ token: "{html.escape(self.token)}" }};</script>\n</head>'
            body = page.replace("</head>", inject, 1).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            return self.wfile.write(body)
        if path.startswith("/data/") or path.lower().endswith(STATIC_EXT):
            return super().do_GET()
        return self.send_error(404)

    def do_POST(self):
        if not self.api_ok() or self.path not in POST_PATHS:
            return self.send_error(403)
        length = int(self.headers.get("Content-Length") or 0)
        if length <= 0 or length > POST_PATHS[self.path]:
            return self.send_error(400)
        try:
            data = json.loads(self.rfile.read(length).decode("utf-8"))
            if self.path == "/api/gamedesign/promote":
                return self.send_json(200, promote(data.get("path"), data.get("deep") is True))
            if self.path == "/api/gamedesign/review":
                return self.send_json(200, review(data.get("path"), data.get("items"), data.get("table")))
            if self.path == "/api/gamedesign/progress":
                return self.send_json(200, progress(data.get("path"), data.get("phase"), data.get("state"), data.get("note")))
            if self.path == "/api/gamedesign/priority":
                return self.send_json(200, priority(data.get("path"), data.get("score")))
            if self.path == "/api/gamedesign/sync":
                return self.send_json(200, sync_live(str(data.get("pin") or "")))
            if self.path == "/api/gamedesign/mode":
                return self.send_json(200, set_mode(str(data.get("pin") or ""), data.get("mode")))
            if self.path == "/api/gamedesign/answers":
                return self.send_json(200, answer(data.get("path"), data.get("answers")))
            pin, mode = str(data.get("pin") or ""), str(data.get("mode") or "")
            model = str(data.get("model") or DEFAULT_MODEL)
            effort = str(data.get("effort") or DEFAULT_EFFORT)
        except Exception:
            return self.send_error(400)
        self.send_json(200, setup(pin, mode, model, effort))


def main():
    ap = argparse.ArgumentParser(description="Setup server for the management page (127.0.0.1 only).")
    ap.add_argument("--port", type=int, default=8770)
    args = ap.parse_args()
    Handler.token = secrets.token_urlsafe(24)
    Handler.port = args.port
    # No address reuse: on Windows it lets a second server share the port silently with an old one.
    ThreadingHTTPServer.allow_reuse_address = False
    try:
        server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    except OSError as ex:
        print(f"Cổng {args.port} đang bận (có thể server Setup cũ còn chạy): {ex}", file=sys.stderr)
        return 1
    print(f"Mở trang Setup: http://127.0.0.1:{args.port}/#setup  (Ctrl+C để tắt)", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
