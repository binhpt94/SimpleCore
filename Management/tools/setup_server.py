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
import guard  # noqa: E402
import sync_page  # noqa: E402
import validate  # noqa: E402

BASIC = ["@human", "@Manager", "@CodeLead", "@Core", "@Composition"]
# Model aliases the claude CLI takes, and its effort levels. New threads default to opus / medium.
MODELS = ["opus", "sonnet", "haiku", "fable"]
EFFORTS = ["low", "medium", "high", "xhigh", "max"]
DEFAULT_MODEL, DEFAULT_EFFORT = "opus", "medium"
EFFORT_RX = re.compile(r'"effort"\s*:\s*"([a-z]+)"')
VSCODE_STATE = Path.home() / "AppData" / "Roaming" / "Code" / "User" / "globalStorage" / "state.vscdb"
TITLE_RX = re.compile(r'"type"\s*:\s*"custom-title".*?"customTitle"\s*:\s*"((?:[^"\\]|\\.)*)"')
MAX_BODY = 4096


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
    """[{id, title, archived, binding, model, effort, modified}] for this project's sessions that have a name.
    model: of the last answer; effort: of the last request (both as the transcript recorded them)."""
    hidden = archived_ids()
    out = []
    folder = transcripts_dir()
    for p in folder.glob("*.jsonl") if folder.is_dir() else []:
        title, model, effort = None, "", ""
        with open(p, encoding="utf-8", errors="replace") as f:
            for line in f:
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
            out.append({"id": sid, "title": title, "archived": sid in hidden,
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
    return {"basic": BASIC, "full": BASIC + services + others}


def bind(session_id, value):
    bind_session.write_json(guard.binding_path(session_id, guard.STORE_DIR),
                            {"feature": value, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S"), "by": "setup_server"})


def create(name, model, effort):
    """New named thread on that model and effort; returns its session id. Costs one short answer."""
    sid = str(uuid.uuid4())
    prompt = f"Thread {name} được tạo bởi trang Setup. Chỉ trả lời đúng một chữ: Sẵn sàng."
    p = subprocess.run(["claude", "-n", name, "--session-id", sid, "--model", model, "--effort", effort, "-p", prompt],
                       cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8", errors="replace",
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
    current = [t for t in threads() if not t["archived"]]
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
        if path.startswith("/data/") or path.endswith((".css", ".js", ".png", ".svg")):
            return super().do_GET()
        return self.send_error(404)

    def do_POST(self):
        if not self.api_ok() or self.path != "/api/setup":
            return self.send_error(403)
        length = int(self.headers.get("Content-Length") or 0)
        if length <= 0 or length > MAX_BODY:
            return self.send_error(400)
        try:
            data = json.loads(self.rfile.read(length).decode("utf-8"))
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
