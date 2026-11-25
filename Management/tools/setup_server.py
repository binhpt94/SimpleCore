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
  new one is created with `claude -n <name without a role's @> --session-id <id> --model <m> --effort <e> -p ...` (the page picks
  model and effort; default opus / medium). It is bound before that first turn, which tells it to read the rules
  of its role (REREAD_PROMPT). Binding writes the same
  file bind_session.py writes, so the PIN never goes through a chat (a bind command typed as the first message
  of a chat can end up in the thread's generated title).
- POST /api/sessions/reread {id} (page token, no PIN): one headless turn in an existing bound thread,
  `claude --resume <id> -p <reread_prompt>`, naming only the rule files of its role that changed since it last read
  them (none changed: no turn). The page's "Đọc lại quy trình" buttons (one thread, or all of them a few at a time)
  call it. It binds nothing.
- POST /api/sessions/compact {id} (page token, no PIN): one headless turn that runs /compact in that thread.
- POST /api/sessions/renew {id, pin} (with the PIN, as it binds): empties the thread's context in the same chat (a
  /compact told to keep nothing; the CLI's /clear does not work on a resumed session in a headless turn), binds
  it again to the FEATURE it had, and tells it to read the rules. Three headless turns at most.
- POST /api/setup {pin, mode, model, effort} checks the PIN, then runs the setup in the background and answers at
  once with the job; GET /api/setup/job is that job as it goes (each name: waiting, running, bound, created or
  error), so the page shows how far it is and which thread failed. One setup runs at a time.
- POST /api/sessions/delete {id, pin} (with the PIN, and only while VS Code is closed: it keeps the chat list in
  memory and writes it back): moves the thread's transcript and its folder into
  ~/.claude/projects/<project>/.deleted/<time>-<id>/ (with its binding and an info.json), takes its id out of the
  VS Code state (vscode_groups.forget_session) and removes its binding. Restore: move the files back to
  ~/.claude/projects/<project>/ and run Setup to bind it again.
- Live (other sessions see the thread in ListAgents and can message it): a thread is live while a process runs it;
  a headless -p turn exits when done. GET /api/threads gives each thread `live` ("interactive": open in VS Code or a
  terminal; "background"; "") from `claude agents --json`. POST /api/sessions/live {id} (no PIN) runs a bound,
  not yet live thread on as a background session, `claude --bg --resume <id> <LIVE_PROMPT>` (one short turn);
  /api/sessions/stop {id} ends it (`claude stop`). Setup does that after a new thread's first turn. Reread, compact
  and the last turn of renew go into the background session of a thread that has one (stopped, then started again
  with that prompt), so it stays live. `claude attach <name>` opens a background thread in a terminal (to answer a
  permission question); stop it before opening the thread in VS Code. Delete stops it first.
- Game design tab: GET /api/gamedesign re-reads Management/game-design. Without the PIN (the page token is
  enough): POST /api/gamedesign/promote copies one idea node into live (gamedesign.promote), /answers stores the
  human's answers to @GameDesigner's questions, /review the human's score and feedback per idea, /priority the
  human's importance score of a node, /progress the progress of a leaf. With the PIN: /sync copies every passing node into live at once
  (gamedesign.sync_live), /mode switches Full Auto / User control. Nothing reaches live without promote or sync.
- Art concepts (studio.py), all without the PIN: POST /api/studio/review stores the human's judgement of one
  delivered picture (level 0-3 or null, and a feedback text; studio.save_review) and tells nobody,
  /anchor marks a picture judged "Đạt" as a style anchor, /debt-close accepts an art debt as it is (GET /api/studio
  re-reads the studio for the Studio tab). GET /Resources/<...> serves the delivered pictures (images
  only, read only) so the page can show them.
"""
import argparse
import hashlib
import html
import json
import os
import re
import secrets
import shutil
import sqlite3
import subprocess
import sys
import threading
import time
import urllib.parse
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
import studio  # noqa: E402
import sync_page  # noqa: E402
import validate  # noqa: E402
import vscode_groups  # noqa: E402
from vscode_groups import vscode_state  # noqa: E402

BASIC = ["@human", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@Codex", "@Blender", "@ClaudeImage", "@DesignAdvisor",
         "@UnityAutoPlay", "@Core"]
ROLE_GROUPS = {"@human": "Master", "@Manager": "Leaders", "@CodeLead": "Leaders", "@GameDesigner": "Leaders",
               "@UIDesigner": "Leaders", "@Codex": "Tools", "@Blender": "Tools", "@ClaudeImage": "Tools", "@DesignAdvisor": "Tools",
               "@UnityAutoPlay": "Testers", "@Core": "Basic"}
GROUP_ORDER = ["Master", "Leaders", "Tools", "Testers", "Basic", "Services", "Features"]      # the tiers of CLAUDE.md, top to bottom
STATIC_EXT = (".css", ".js", ".png", ".svg", ".jpg", ".jpeg", ".gif", ".webp", ".ttf")


def thread_name(value):
    """The chat name for a FEATURE value: a role without its '@' (SendMessage reads 'name@team' as a team address,
    so a session named '@Codex' cannot receive messages); a feature keeps its name."""
    return value[1:] if value in ROLE_GROUPS else value


def feature_of(title):
    """The FEATURE value a chat name stands for: 'Codex' and the older '@Codex' are both '@Codex'."""
    return "@" + title if "@" + title in ROLE_GROUPS else title


def group_of(name):
    """Master, Leaders, Tools, Testers, Basic, Services or Features for a session name; "" for a name outside the setup."""
    if name in ROLE_GROUPS:
        return ROLE_GROUPS[name]
    if name.startswith("@"):
        return ""
    if not guard.valid_feature(name, str(ROOT)) and name not in [n for n, _, _ in gamedesign.live_feature_names(ROOT)]:
        return ""      # neither a folder nor named by the fixed design
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
TITLE_RX = re.compile(r'"type"\s*:\s*"custom-title".*?"customTitle"\s*:\s*"((?:[^"\\]|\\.)*)"')
MAX_BODY = 4096
# What a thread is told when it is created or renewed. The root CLAUDE.md and the shared rules are already in its
# context (the CLI loads them); Reading them again only doubles them. Its role rules are not loaded (their `paths:`
# keeps them out of the ~30 other chats), so the guard line tells it which file to Read (bind_session.rules_reminder).
REREAD_PROMPT = ("Đọc lại quy trình: CLAUDE.md gốc và luật chung đã nạp sẵn trong ngữ cảnh, không Read lại. Chỉ Read "
                 "file luật mà dòng guard ở đầu tin bảo đọc (nếu có). Chưa làm việc gì khác. Trả lời ngắn: đã đọc những file nào.")
# The page's "Đọc lại quy trình" button: only the rule files of the thread's role that changed since it last read them
# (rules-seen/<id>.json in the guard store: path -> sha1). Nothing changed: no turn at all, so pressing "tất cả" after
# a change costs one turn only in the threads that follow a changed file.
RULES_SEEN_DIR = "rules-seen"


def reread_prompt(changed):
    return ("Đọc lại quy trình: các file luật sau vừa đổi: " + ", ".join(changed) + ". Read lại toàn bộ đúng các file "
            "đó, không Read file nào khác. Chưa làm việc gì khác. Trả lời ngắn: đã đọc những file nào.")


def rules_state(binding):
    """{path from the root: sha1} of the rule files a thread bound to `binding` follows."""
    out = {}
    for rel in bind_session.rule_files(binding, str(ROOT)):
        try:
            out[rel] = hashlib.sha1((ROOT / rel).read_bytes()).hexdigest()
        except OSError:
            pass
    return out


def rules_seen_path(session_id):
    path = guard.binding_path(session_id, guard.STORE_DIR)
    return path and os.path.join(guard.STORE_DIR, RULES_SEEN_DIR, os.path.basename(path))


def changed_rules(t):
    """(rule files of thread t that changed since it last read them, their current state)."""
    now = rules_state(t["binding"])
    path = rules_seen_path(t["id"])
    seen = bind_session.read_json(path, {}) if path else {}
    seen = seen if isinstance(seen, dict) else {}
    return [rel for rel, sha in now.items() if seen.get(rel) != sha], now


def mark_rules_seen(session_id, state):
    path = rules_seen_path(session_id)
    if path:
        bind_session.write_json(path, state)


COMPACT_PROMPT = "/compact"
# Renew: the CLI's /clear does not empty a resumed session in a headless turn, a /compact told to keep nothing does.
WIPE_PROMPT = ("/compact Session này được làm mới: KHÔNG giữ lại bất kì nội dung, con số, file, việc dở dang hay quyết định nào "
               "của cuộc trò chuyện. Bản tóm tắt chỉ gồm đúng một câu: 'Session đã được làm mới, không còn ngữ cảnh cũ.'")
CLAUDE_SECONDS = 300      # one headless turn that reads the rules
# Live: a thread other sessions see in ListAgents has a running process (the CLI's session list, `claude agents --json`).
# A headless turn (-p) exits when it is done, so the page keeps a thread live with `claude --bg --resume <id> <prompt>`:
# a background session under the same id (`claude attach <name>` opens it in a terminal, `claude stop <id>` ends it).
# A new thread's first turn stays a -p turn: a session started by --bg records the entry point "cli", and the VS Code
# extension may not list it.
LIVE_PROMPT = ("Thread này vừa được bật live từ trang Setup: nó chạy nền để các phiên khác nhắn được (SendMessage). "
               "Chưa làm việc gì khác. Trả lời đúng một từ: sẵn sàng.")
LIVE_SECONDS = 30      # how long to wait for a background session to show up (or to go) in the session list
LIVE_CACHE = {"at": 0.0, "value": {}}
LIVE_LOCK = threading.Lock()
# POST paths and the largest body each takes. Answers carry free text for every question of a node.
POST_PATHS = {"/api/setup": MAX_BODY, "/api/gamedesign/promote": MAX_BODY, "/api/gamedesign/mode": MAX_BODY,
              "/api/gamedesign/answers": 64 * 1024, "/api/gamedesign/review": 64 * 1024, "/api/gamedesign/sync": MAX_BODY,
              "/api/gamedesign/priority": MAX_BODY, "/api/gamedesign/progress": 64 * 1024, "/api/gamedesign/requirement": 64 * 1024, "/api/vscode-groups": MAX_BODY,
              "/api/references/upload": 12 * 1024 * 1024, "/api/references/note": 64 * 1024,
              "/api/references/delete": MAX_BODY,
              "/api/studio/review": 64 * 1024, "/api/studio/anchor": MAX_BODY, "/api/studio/debt-close": MAX_BODY,
              "/api/sessions/reread": MAX_BODY, "/api/sessions/compact": MAX_BODY, "/api/sessions/renew": MAX_BODY,
              "/api/sessions/delete": MAX_BODY, "/api/sessions/live": MAX_BODY, "/api/sessions/stop": MAX_BODY}
TRASH = ".deleted"      # inside the transcripts folder: threads() reads only *.jsonl at its top
# Threads that run a headless turn right now (renew, reread, compact, a thread setup is creating): not deleted.
BUSY = set()
BUSY_LOCK = threading.Lock()
# The setup that runs or ran last: {id, mode, started, ended, done, items: [{name, group, state, ...}]}.
JOB = None
JOB_LOCK = threading.Lock()


def transcripts_dir():
    """~/.claude/projects/<project path with every non-alphanumeric character as '-'>."""
    return Path.home() / ".claude" / "projects" / re.sub(r"[^A-Za-z0-9]", "-", str(ROOT))


def vscode_view():
    """For the Setup page: each thread's group in the VS Code list, and whether VS Code runs (groups are then not
    written; see vscode_groups.py)."""
    return {"groups": vscode_groups.current_groups(ROOT), "running": vscode_groups.vscode_running(),
            "tierNames": vscode_groups.TIER_NAMES}


def group_in_vscode():
    """Puts the threads of the full setup into the tier groups of the VS Code list (refused while VS Code runs)."""
    try:
        key, groups = vscode_groups.apply(ROOT, vscode_groups.setup_assignments())
    except (ValueError, OSError, sqlite3.Error) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "groups": [{"name": g["name"], "count": len(g["sessionIds"])} for g in groups]}


def archived_ids():
    """Session ids hidden (archived) in the VS Code extension; empty when its state cannot be read."""
    try:
        # Percent-encoded: the macOS folder has a space, and '?' or '#' would end the path in a URI.
        con = sqlite3.connect(f"file:{urllib.parse.quote(vscode_state().as_posix(), safe='/:')}?mode=ro", uri=True)
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
    """[{id, title, feature, group, legacy, archived, binding, model, effort, modified}] for this project's sessions
    that have a name. feature: the FEATURE value the name stands for; legacy: a role named with its '@' (rename it).
    model: of the last answer; effort: of the last request (both as the transcript recorded them)."""
    hidden = archived_ids()
    live = live_sessions()
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
            out.append({"id": sid, "title": title, "feature": feature_of(title), "group": group_of(feature_of(title)),
                        "legacy": title in ROLE_GROUPS, "archived": sid in hidden,
                        "unlisted": entries == {HEADLESS_ENTRY},
                        "model": "" if model == "<synthetic>" else model, "effort": effort,
                        "binding": guard.bound_feature(sid, guard.STORE_DIR) or "",
                        "live": (live.get(sid) or {}).get("kind", ""), "live_status": (live.get(sid) or {}).get("status", ""),
                        "modified": time.strftime("%Y-%m-%d %H:%M", time.localtime(p.stat().st_mtime))})
    return sorted(out, key=lambda t: (t["title"].lower(), t["modified"]))


def sets():
    """The names of the basic and the full setup, in the order of the groups (Master, Leaders, Basic, Services,
    Features). The full one adds every service and feature folder, and every feature of the fixed design (live):
    the folder its code.json names, or the name GD will give it from its key when it has no code.json yet
    (`suggested`). Their chats are made and bound now and may edit once @Manager has made the folder (scaffold.py)
    and the design has reached round 4. `missing`: those names without a folder yet."""
    cfg = json.loads((HERE / "rules.json").read_text(encoding="utf-8-sig"))
    ids = validate.Scan(ROOT, cfg)
    ids.discover()
    folders = [f["id"] for f in ids.features]
    designed = gamedesign.live_feature_names(ROOT)
    features = folders + [n for n, _, _ in designed if n not in folders]
    services = sorted((f for f in features if f.startswith("_Services/")), key=str.lower)
    others = sorted((f for f in features if not f.startswith("_Services/")), key=str.lower)
    full = BASIC + services + others
    return {"basic": BASIC, "full": full, "groups": {n: group_of(n) for n in full}, "order": GROUP_ORDER,
            "missing": [n for n, _, _ in designed if n not in folders],
            "suggested": [n for n, _, declared in designed if not declared]}


def bind(session_id, value):
    bind_session.write_json(guard.binding_path(session_id, guard.STORE_DIR),
                            {"feature": value, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S"), "by": "setup_server"})


def live_sessions(fresh=False):
    """{session id: {kind, status, short}} of the Claude sessions that run now (`claude agents --json`: the list
    ListAgents reads): kind "interactive" (VS Code, a terminal) or "background" (--bg). {} when the CLI cannot say.
    Kept for two seconds, as the page asks for every row."""
    with LIVE_LOCK:
        if not fresh and time.time() - LIVE_CACHE["at"] < 2:
            return dict(LIVE_CACHE["value"])
    value = {}
    try:
        p = subprocess.run(["claude", "agents", "--json"], cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=20, shell=(os.name == "nt"))
        for s in json.loads(p.stdout or "[]") if p.returncode == 0 else []:
            if isinstance(s, dict) and isinstance(s.get("sessionId"), str):
                value[s["sessionId"]] = {"kind": str(s.get("kind") or ""), "status": str(s.get("status") or ""),
                                         "short": str(s.get("id") or s["sessionId"][:8])}
    except (OSError, ValueError, subprocess.SubprocessError):
        value = {}
    with LIVE_LOCK:
        LIVE_CACHE.update(at=time.time(), value=value)
    return dict(value)


def wait_live(session_id, present):
    """Waits until the thread is (present) or is no longer (not present) in the session list; True when it got there."""
    end = time.time() + LIVE_SECONDS
    while True:
        if (session_id in live_sessions(fresh=True)) == present:
            return True
        if time.time() > end:
            return False
        time.sleep(1)


def forget_bg(short):
    """`claude rm` of a background job that has ended. `claude stop` keeps the job in the CLI's list (state done), and
    while it is there `claude --bg --resume <id>` takes the thread for running and starts a COPY under a new id
    (unbound, so guard blocks it) instead of running the thread on. Nothing when there is no such job."""
    try:
        run_claude(["rm", short])
    except Exception:
        pass      # "No session matching": never live, or already removed


def drop_copy(short):
    """Undoes a copy the CLI made anyway: stops and removes its background job and moves its transcript into the
    trash of the transcripts folder (it would be listed as a second thread of the same name, with no binding)."""
    try:
        run_claude(["stop", short])
    except Exception:
        pass
    forget_bg(short)
    folder = transcripts_dir()
    for src in folder.glob(f"{short}*.jsonl") if re.fullmatch(r"[0-9a-f]{6,}", short) else []:
        trash = folder / TRASH / f"{time.strftime('%Y%m%d-%H%M%S')}-{src.stem}-copy"
        trash.mkdir(parents=True, exist_ok=True)
        for part in (src, folder / src.stem):
            if part.exists():
                shutil.move(str(part), str(trash / part.name))


# The CLI's background service takes one job change at a time badly: the page runs several threads at once, and a
# `--bg --resume` that overlaps another thread's `stop` / `rm` sometimes finds the thread's ended job still there and
# opens a COPY. Every start and stop of a background job goes through this lock, and a copy is retried once.
BG_LOCK = threading.Lock()
COPY_RETRY_SECONDS = 2


def start_bg(session_id, extra, prompt):
    """Runs the thread on as a background session under the same id, with that first prompt (one turn), and waits
    until the session list shows it. `extra` starts with `-n <thread name>`: without it a resumed background session
    is listed under a name the CLI makes up from the conversation, and SendMessage to the thread's name misses it.
    An ended job of the thread is removed first (forget_bg). Raises when the CLI opened a copy anyway (the copy is
    dropped) or the session does not show up."""
    with BG_LOCK:
        for attempt in (1, 2):
            forget_bg(session_id[:8])
            out = run_claude(["--bg", "--resume", session_id] + extra + [prompt])
            m = re.search(r"backgrounded\s*·\s*([0-9a-f]{6,})", out)
            if not m or session_id.startswith(m.group(1)):
                break
            drop_copy(m.group(1))
            if attempt == 2:
                raise RuntimeError(f"CLI mở bản sao {m.group(1)} thay vì chạy tiếp thread này (thread đang mở ở nơi "
                                   "khác?); bản sao đã được tắt và dời vào thùng rác")
            time.sleep(COPY_RETRY_SECONDS)      # the job of the old process was not gone yet: once more
    if not wait_live(session_id, True):
        raise RuntimeError(f"chưa thấy phiên nền của thread trong `claude agents` sau {LIVE_SECONDS}s")


def stop_bg(session_id):
    """Ends the thread's background session (`claude stop`), waits until it is gone, and removes its ended job
    (forget_bg), so the next `--bg --resume` runs the thread itself; nothing more when it has none."""
    s = live_sessions(fresh=True).get(session_id)
    if not s or s["kind"] != "background":
        return
    with BG_LOCK:
        run_claude(["stop", s["short"]])
        if not wait_live(session_id, False):
            raise RuntimeError(f"phiên nền vẫn chạy sau {LIVE_SECONDS}s: claude stop {s['short']}")
        forget_bg(s["short"])


def turn_args(t):
    """The model and effort a thread last used, as CLI flags."""
    args = []
    if t["model"]:
        args += ["--model", t["model"]]
    if t["effort"] in EFFORTS:
        args += ["--effort", t["effort"]]
    return args


# A background thread (claude --bg) edits the shared checkout: the one Unity has open. Without this the CLI blocks its
# Edit and Write until it calls EnterWorktree (worktree.bgIsolation, also set in .claude/settings.json).
CLAUDE_ENV = {"CLAUDE_CODE_ENTRYPOINT": VSCODE_ENTRY, "CLAUDE_BG_ISOLATION": "none"}


def run_claude(args):
    """Runs the claude CLI in the project (one headless turn) and returns what it printed; raises with its error."""
    p = subprocess.run(["claude"] + args, cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8", errors="replace",
                       env=dict(os.environ, **CLAUDE_ENV),
                       timeout=CLAUDE_SECONDS, shell=(os.name == "nt"))
    if p.returncode != 0:
        raise RuntimeError((p.stderr or p.stdout or f"exit {p.returncode}").strip()[:300])
    return (p.stdout or "").strip()


def create(name, feature, model, effort):
    """New named thread on that model and effort, bound to `feature` BEFORE its first turn, so that turn already
    knows its role and reads the right rules; returns its session id. Costs one turn."""
    sid = str(uuid.uuid4())
    bind(sid, feature)
    try:
        run_claude(["-n", name, "--session-id", sid, "--model", model, "--effort", effort,
                    "-p", f"Thread {name} được tạo bởi trang Setup. {REREAD_PROMPT}"])
        mark_rules_seen(sid, rules_state(feature))
    except Exception:
        try:
            os.remove(guard.binding_path(sid, guard.STORE_DIR))      # no thread: leave no binding behind
        except OSError:
            pass
        raise
    return sid


def eligible(session_id):
    """(thread, None) for an existing, listed, bound thread the page may run a headless turn in; else (None, the
    refusal to send back)."""
    t = next((t for t in threads() if t["id"] == session_id), None) if isinstance(session_id, str) else None
    if t is None:
        return None, {"ok": False, "error": "không có thread này"}
    why = ("thread đã archive" if t["archived"] else "VS Code không hiện thread này" if t["unlisted"]
           else "thread chưa gán FEATURE: chạy Setup trước" if not t["binding"] else "")
    if why:
        return None, {"ok": False, "id": t["id"], "title": t["title"], "error": why}
    return t, None


OPEN_ELSEWHERE = ("thread đang mở trong VS Code (hoặc một terminal): trang không chạy lượt vào nó, vì tiến trình đó không "
                  "thấy lượt này (ngữ cảnh của nó giữ nguyên) và transcript tách nhánh; một lệnh / nhắn qua SendMessage chỉ là chữ. "
                  "Làm ngay trong chat đó: {what}")


def open_elsewhere(t, what):
    """The refusal for a thread a VS Code or terminal chat holds (claude agents: interactive), or None."""
    if t.get("live") != "interactive":
        return None
    return {"ok": False, "id": t["id"], "title": t["title"], "error": OPEN_ELSEWHERE.format(what=what), "open": True}


RELAY_MODEL = "claude-haiku-5-5"
RELAY_DONE = "ĐÃ GỬI"


RELAY_HEAD = ("Tin của người, gửi từ trang Setup qua một phiên trung chuyển đã thoát ngay sau khi gửi: KHÔNG trả lời "
              "bằng SendMessage (địa chỉ đó không còn). Làm việc dưới đây và trả lời ngay trong chat này.\n\n")


def relay(t, text):
    """Delivers `text` to a chat open in VS Code (or a terminal) as a cross-session message, so the turn runs IN that
    chat (same session, no split transcript). The server cannot send one itself: a short headless helper turn
    (cheap model, nothing saved) calls SendMessage to the thread's name. A slash command sent this way is only text
    (/compact is not run), so only plain prompts go through here."""
    order = (f'Gọi đúng một lần tool SendMessage với to="{t["title"]}" và message là toàn bộ đoạn giữa hai dòng '
             f'<<< và >>> dưới đây, giữ nguyên từng chữ. Không làm gì khác. Gửi được thì in đúng: {RELAY_DONE}\n<<<\n{RELAY_HEAD}{text}\n>>>')
    out = run_claude(["-p", "--model", RELAY_MODEL, "--no-session-persistence", order])
    if RELAY_DONE not in out:
        raise RuntimeError("không chuyển được tin vào chat đang mở: " + out[:200])
    return f"đã nhắn vào chat đang mở ở VS Code (SendMessage tới {t['title']}); chat đó tự chạy lượt này"


def busy(session_id, on):
    with BUSY_LOCK:
        (BUSY.add if on else BUSY.discard)(session_id)


BG_ANSWER = "đã gửi vào phiên nền (thread vẫn live; xem câu trả lời: claude attach {title})"


def turn(t, prompt, bg=None):
    """One turn in thread t, on the model and effort it last used. A thread that runs in the background is stopped
    first (two processes must not write one session). bg: run the turn as a background session, so the thread stays
    live (default: when it ran in the background). What it printed, or BG_ANSWER for a background turn."""
    running_bg = t.get("live") == "background"
    bg = running_bg if bg is None else bg
    busy(t["id"], True)
    try:
        if running_bg:
            stop_bg(t["id"])
            t["live"] = ""
        if bg:
            start_bg(t["id"], ["-n", t["title"]] + turn_args(t), prompt)
            t["live"] = "background"
            return BG_ANSWER.format(title=t["title"])
        return run_claude(["--resume", t["id"]] + turn_args(t) + ["-p", prompt])
    finally:
        busy(t["id"], False)


HUMAN = "@human"      # the human's own chat: never run in the background, VS Code could not open it any more


def human_thread(t):
    return t.get("binding") == HUMAN or t.get("feature") == HUMAN


def go_live(session_id):
    """The Bật live button: runs one existing, bound thread on as a background session (one short turn), so other
    sessions see it in ListAgents and can message it. A thread that already runs (VS Code, terminal, background) is
    left as it is."""
    t, no = eligible(session_id)
    if no:
        return no
    if human_thread(t) and not t.get("live"):
        return {"ok": False, "id": t["id"], "title": t["title"],
                "error": "thread của người: không chạy nền (VS Code sẽ không mở được nó); mở nó trong VS Code là live"}
    if t.get("live"):
        where = "nền" if t["live"] == "background" else "VS Code hoặc terminal"
        return {"ok": True, "id": t["id"], "title": t["title"], "answer": f"đã live sẵn ({where})"}
    try:
        turn(t, LIVE_PROMPT, bg=True)
    except Exception as ex:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": "chưa bật live được: " + str(ex)[:260]}
    return {"ok": True, "id": t["id"], "title": t["title"], "answer": f"đã live nền (claude attach {t['title']})"}


def stop_live(session_id):
    """The Tắt live button: ends the thread's background session. A chat open in VS Code or a terminal is not
    touched (close it there)."""
    t = next((t for t in threads() if t["id"] == session_id), None) if isinstance(session_id, str) else None
    if t is None:
        return {"ok": False, "error": "không có thread này"}
    head = {"id": t["id"], "title": t["title"]}
    if t.get("live") != "background":
        return dict(head, ok=True, answer="đang mở ở VS Code hoặc terminal: tắt ở đó" if t.get("live") else "không chạy nền")
    with BUSY_LOCK:
        if t["id"] in BUSY:
            return dict(head, ok=False, error="thread đang chạy một lượt từ trang này: chờ xong")
    try:
        stop_bg(t["id"])
    except Exception as ex:
        return dict(head, ok=False, error=str(ex)[:300])
    return dict(head, ok=True, answer="đã tắt live")


def reread(session_id):
    """Tells one existing, bound thread to read the rules again: one headless turn (`claude --resume <id> -p ...`).
    A chat that is open in VS Code may not show this turn until it is opened again."""
    t, no = eligible(session_id)
    if no:
        return no
    changed, now = changed_rules(t)
    if not changed:
        return {"ok": True, "id": t["id"], "title": t["title"], "skipped": True,
                "answer": "luật của vai trò này không đổi từ lần đọc trước: không chạy lượt nào"}
    prompt = reread_prompt(changed)
    try:
        answer = relay(t, prompt) if t.get("live") == "interactive" else turn(t, prompt)
    except Exception as ex:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": str(ex)[:300]}
    mark_rules_seen(t["id"], now)
    return {"ok": True, "id": t["id"], "title": t["title"], "answer": answer[:300]}


def compact(session_id):
    """Compacts one existing, bound thread: one headless turn that runs /compact. It binds nothing."""
    t, no = eligible(session_id)
    if no:
        return no
    no = open_elsewhere(t, "gõ /compact")
    if no:
        return no
    try:
        turn(t, COMPACT_PROMPT)
    except Exception as ex:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": str(ex)[:300]}
    return {"ok": True, "id": t["id"], "title": t["title"], "answer": "đã compact"}


def renew(session_id, pin):
    """Makes one existing, bound thread new again, in the same chat: its context is emptied, it is bound again to
    the FEATURE it had, and it is told to read the rules. With the PIN, as every binding.
    The claude CLI cannot run /clear on a session in a headless turn (it opens another, empty session and leaves
    this one as it was), so the context is emptied by a /compact that is told to keep nothing: same session id,
    same chat in VS Code. The old messages stay in the transcript above the compact line; the model no longer has
    them."""
    t, no = eligible(session_id)
    if no:
        return no
    no = open_elsewhere(t, "đóng chat đó trong VS Code rồi bấm Renew lại (hoặc gõ /clear trong chat, rồi nhắn nó đọc lại quy trình)")
    if no:      # before the PIN: closing the chat must not cost a try
        return no
    error = bind_session.check_pin(guard.STORE_DIR, pin if isinstance(pin, str) else "")
    if error:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": error, "pin": True}
    # a renewed thread is live; a chat open in VS Code stays there, and the human's own chat is never put in the background
    live_after = t.get("live") != "interactive" and not human_thread(t)
    try:
        turn(t, WIPE_PROMPT, bg=False)
    except Exception as ex:      # nothing was changed
        return {"ok": False, "id": t["id"], "title": t["title"], "error": "chưa làm mới được: " + str(ex)[:260]}
    bind(t["id"], t["binding"])
    try:
        answer = turn(t, f"Thread {t['title']} vừa được làm mới (renew) từ trang Setup: ngữ cảnh cũ đã bỏ. {REREAD_PROMPT}",
                      bg=live_after)
    except Exception as ex:
        return {"ok": False, "id": t["id"], "title": t["title"], "renewed": True,
                "error": "đã làm mới và gán lại, CHƯA đọc quy trình (bấm Đọc lại quy trình): " + str(ex)[:200]}
    mark_rules_seen(t["id"], rules_state(t["binding"]))
    return {"ok": True, "id": t["id"], "title": t["title"], "answer": "đã làm mới, gán lại " + t["binding"] + ". " + answer[:240]}


def setup_check(pin, mode, model, effort):
    """(names of the set, None) when the request may run; else (None, the refusal). The PIN is looked at last."""
    names = sets().get(mode) if mode in ("basic", "full") else None      # sets() also holds order, missing...
    if not names:
        return None, {"ok": False, "error": f"unknown mode '{mode}'"}
    if model not in MODELS or effort not in EFFORTS:      # checked before the PIN: a typo must not cost a try
        return None, {"ok": False, "error": f"model must be one of {MODELS}, effort one of {EFFORTS}"}
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return None, {"ok": False, "error": error}
    return names, None


def new_job(mode, names):
    return {"id": uuid.uuid4().hex[:8], "mode": mode, "started": time.time(), "ended": None, "done": False,
            "items": [{"name": n, "group": group_of(n), "state": "waiting", "ids": [], "detail": "",
                       "started": None, "ended": None} for n in names]}


def run_job(job, model, effort):
    """Binds or creates every name of the job in order; each item shows its state while it goes."""
    current = [t for t in threads() if not t["archived"] and not t["unlisted"]]
    for item in job["items"]:
        name = item["name"]
        with JOB_LOCK:
            item.update(state="running", started=time.time())
        same = [t for t in current if t["feature"] == name]
        try:
            if same:
                for t in same:
                    bind(t["id"], name)
                done = {"state": "bound", "ids": [t["id"] for t in same]}
            else:
                sid = create(thread_name(name), name, model, effort)
                done = {"state": "created", "ids": [sid], "model": model, "effort": effort}
                with JOB_LOCK:
                    item.update(done, detail="đã đọc quy trình, đang bật live…")
                try:      # then run it on in the background, so the other sessions see it (one more short turn)
                    busy(sid, True)
                    start_bg(sid, ["-n", thread_name(name), "--model", model, "--effort", effort], LIVE_PROMPT)
                    done.update(live=True, detail="")
                except Exception as ex:
                    done.update(live=False, detail="chưa bật live (bấm Bật live): " + str(ex)[:240])
                finally:
                    busy(sid, False)
        except Exception as ex:
            done = {"state": "error", "detail": str(ex)[:300]}
        with JOB_LOCK:
            item.update(done, ended=time.time())
    with JOB_LOCK:
        job.update(done=True, ended=time.time())


def job_view():
    """A copy of the setup job that runs or ran last (None before the first), for the page."""
    with JOB_LOCK:
        return json.loads(json.dumps(JOB)) if JOB else None


def setup_start(pin, mode, model=DEFAULT_MODEL, effort=DEFAULT_EFFORT, background=True):
    """The Setup buttons: checks the request and the PIN now, then runs the setup (in the background unless told
    otherwise) and answers with the job; the page reads how far it is from GET /api/setup/job."""
    global JOB
    with JOB_LOCK:
        if JOB and not JOB["done"]:
            return {"ok": False, "error": "Setup đang chạy: chờ lần này xong", "running": True,
                    "job": json.loads(json.dumps(JOB))}
    names, no = setup_check(pin, mode, model, effort)
    if no:
        return no
    job = new_job(mode, names)
    with JOB_LOCK:
        if JOB and not JOB["done"]:      # another one started while the PIN was checked
            return {"ok": False, "error": "Setup đang chạy: chờ lần này xong", "running": True}
        JOB = job
    if background:
        threading.Thread(target=run_job, args=(job, model, effort), daemon=True).start()
    else:
        run_job(job, model, effort)
    return {"ok": True, "job": job_view()}


def setup(pin, mode, model=DEFAULT_MODEL, effort=DEFAULT_EFFORT):
    """The whole setup in one call (no background): {ok, results} with one result per name, in tier order."""
    res = setup_start(pin, mode, model, effort, background=False)
    if not res["ok"]:
        return res
    results = []
    for item in res["job"]["items"]:
        r = {"name": item["name"], "group": item["group"], "ids": item["ids"],
             "action": {"bound": "bound", "created": "created"}.get(item["state"], "error")}
        if item["state"] == "created":
            r.update(model=item["model"], effort=item["effort"])
        if item["state"] == "error":
            r["detail"] = item["detail"]
        results.append(r)
    return {"ok": True, "results": results}


def delete_thread(session_id, pin):
    """The Xoá button of one thread: moves its transcript (and its folder) into the trash of the transcripts folder,
    takes it out of the VS Code state and removes its binding. With the PIN, and only while VS Code is closed (it
    would write its chat list back). Archived and hidden threads may be deleted too."""
    t = next((t for t in threads() if t["id"] == session_id), None) if isinstance(session_id, str) else None
    if t is None:
        return {"ok": False, "error": "không có thread này"}
    head = {"ok": False, "id": t["id"], "title": t["title"]}
    with BUSY_LOCK:
        running_now = t["id"] in BUSY
    with JOB_LOCK:
        setup_runs = bool(JOB and not JOB["done"])
    if setup_runs:
        return dict(head, error="Setup đang chạy: chờ xong rồi xoá")
    if running_now:
        return dict(head, error="thread đang chạy một lượt từ trang này: chờ xong rồi xoá")
    if vscode_groups.vscode_running():      # before the PIN: closing VS Code must not cost a try
        return dict(head, error="VS Code đang chạy: tắt hẳn VS Code (mọi cửa sổ) rồi bấm lại", vscode=True)
    if t.get("live") == "interactive":
        return dict(head, error="thread đang mở trong một terminal: thoát chat đó rồi bấm lại")
    error = bind_session.check_pin(guard.STORE_DIR, pin if isinstance(pin, str) else "")
    if error:
        return dict(head, error=error, pin=True)
    try:
        stop_bg(t["id"])      # a background session would write its transcript back
    except Exception as ex:
        return dict(head, error="chưa xoá: chưa tắt được phiên nền: " + str(ex)[:220])
    try:
        was_in = vscode_groups.forget_session(t["id"], running=vscode_groups.vscode_running)
    except (ValueError, OSError, sqlite3.Error) as ex:      # nothing moved yet
        return dict(head, error="chưa xoá: " + str(ex)[:260])
    folder = transcripts_dir()
    trash = folder / TRASH / f"{time.strftime('%Y%m%d-%H%M%S')}-{t['id']}"
    trash.mkdir(parents=True, exist_ok=True)
    for src in (folder / f"{t['id']}.jsonl", folder / t["id"]):
        if src.exists():
            shutil.move(str(src), str(trash / src.name))
    bpath = guard.binding_path(t["id"], guard.STORE_DIR)
    if bpath and os.path.isfile(bpath):
        shutil.copy2(bpath, trash / "binding.json")
        os.remove(bpath)
    guard.release_lock(guard.STORE_DIR, t["id"])
    bind_session.write_json(str(trash / "info.json"), {
        "id": t["id"], "title": t["title"], "feature": t["binding"], "deleted_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "vscode_groups": was_in,
        "restore": f"chép {t['id']}.jsonl và thư mục {t['id']}/ (nếu có) về {folder}, rồi bấm Setup để gán lại"})
    return {"ok": True, "id": t["id"], "title": t["title"], "trash": str(trash)}


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


def requirement(ref, action, note):
    """The human waives one requirement of a feature's book (with a reason), or takes the waiver back. No PIN."""
    if action not in ("waive", "todo"):
        return {"ok": False, "error": "chỉ nhận waive hoặc todo"}
    try:
        import reqbook
        reqbook.mark_by_ref(ROOT, ref, action, "người", note if isinstance(note, str) else None)
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


def reference_call(action, data):
    """The tab Tham chiếu: upload a picture (base64), change its note, kind and tags, or delete it. No PIN, like the
    scores: the human's own notes."""
    import references
    try:
        if action == "upload":
            references.upload(ROOT, data.get("name"), data.get("data"), data.get("note") or "", data.get("kind") or references.KINDS[0],
                              data.get("tags") or [])
        elif action == "note":
            references.update(ROOT, str(data.get("id") or ""), data.get("note"), data.get("kind"), data.get("tags"))
        elif action == "delete":
            references.delete(ROOT, str(data.get("id") or ""))
        else:
            return {"ok": False, "error": f"không có lệnh '{action}'"}
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "data": sync_page.write_references(ROOT)}


def studio_call(fn, *args):
    """Runs one studio.py command for the page; its printed lines become the message."""
    import contextlib
    import io
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            fn(ROOT, *args)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "message": out.getvalue().strip(), "data": sync_page.write_gamedesign(ROOT),
            "studio": sync_page.write_studio(ROOT)}


def art_review(ref, item, level, text):
    """The slider and the feedback box under an art concept: stores the human's judgement of one picture (level
    0-3, or None for "Chưa duyệt") and its feedback. No PIN, and it tells nobody: @GameDesigner reads it when the
    human asks for the pictures to be made again."""
    if not isinstance(ref, str) or not ref.startswith("_design/") or not isinstance(item, str):
        return {"ok": False, "error": "không phải ảnh của order art"}
    if level is not None and (not isinstance(level, int) or isinstance(level, bool)):
        return {"ok": False, "error": "mức chấm không hợp lệ"}
    try:
        studio.save_review(ROOT, ref, item, level, text if isinstance(text, str) else "")
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "data": sync_page.write_gamedesign(ROOT), "studio": sync_page.write_studio(ROOT)}


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
                                            "defaults": {"model": DEFAULT_MODEL, "effort": DEFAULT_EFFORT},
                                            "vscode": vscode_view()})
            # Each tab of the page asks for its own data when it is first opened: nothing is built when the page is.
            if path == "/api/setup/job":
                return self.send_json(200, {"job": job_view()})
            if path == "/api/project":
                return self.send_json(200, sync_page.write_project(ROOT))
            if path == "/api/gamedesign":
                return self.send_json(200, sync_page.write_gamedesign(ROOT))
            if path == "/api/studio":
                return self.send_json(200, sync_page.write_studio(ROOT))
            if path == "/api/references":
                return self.send_json(200, sync_page.write_references(ROOT))
            return self.send_error(404)
        if path in ("/", "/index.html"):
            page = (ROOT / "Management" / "index.html").read_text(encoding="utf-8")
            inject = f'<script>window.CORESIMPLE_SETUP = {{ token: "{html.escape(self.token)}" }};</script>\n</head>'
            body = page.replace("</head>", inject, 1).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            return self.wfile.write(body)
        if path.startswith("/Resources/"):
            return self.send_resource(urllib.parse.unquote(path))
        if path.startswith("/data/") or path.lower().endswith(STATIC_EXT):
            return super().do_GET()
        return self.send_error(404)

    def send_resource(self, path):
        """A delivered picture under Resources/, read only; anything else is 404."""
        base = (ROOT / "Resources").resolve()
        target = (ROOT / path.lstrip("/")).resolve()
        types = {".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".webp": "image/webp"}
        if base not in target.parents or target.suffix.lower() not in types or not target.is_file():
            return self.send_error(404)
        body = target.read_bytes()
        self.send_response(200)
        self.send_header("Content-Type", types[target.suffix.lower()])
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        return self.wfile.write(body)

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
            if self.path.startswith("/api/references/"):
                return self.send_json(200, reference_call(self.path.rsplit("/", 1)[1], data))
            if self.path == "/api/gamedesign/progress":
                return self.send_json(200, progress(data.get("path"), data.get("phase"), data.get("state"), data.get("note")))
            if self.path == "/api/gamedesign/requirement":
                return self.send_json(200, requirement(data.get("ref"), data.get("action"), data.get("note")))
            if self.path == "/api/gamedesign/priority":
                return self.send_json(200, priority(data.get("path"), data.get("score")))
            if self.path == "/api/gamedesign/sync":
                return self.send_json(200, sync_live(str(data.get("pin") or "")))
            if self.path == "/api/gamedesign/mode":
                return self.send_json(200, set_mode(str(data.get("pin") or ""), data.get("mode")))
            if self.path == "/api/gamedesign/answers":
                return self.send_json(200, answer(data.get("path"), data.get("answers")))
            if self.path == "/api/sessions/reread":
                return self.send_json(200, reread(data.get("id")))
            if self.path == "/api/sessions/compact":
                return self.send_json(200, compact(data.get("id")))
            if self.path == "/api/sessions/renew":
                return self.send_json(200, renew(data.get("id"), data.get("pin")))
            if self.path == "/api/sessions/live":
                return self.send_json(200, go_live(data.get("id")))
            if self.path == "/api/sessions/stop":
                return self.send_json(200, stop_live(data.get("id")))
            if self.path == "/api/sessions/delete":
                return self.send_json(200, delete_thread(data.get("id"), data.get("pin")))
            if self.path == "/api/vscode-groups":
                return self.send_json(200, group_in_vscode())
            if self.path == "/api/studio/review":
                return self.send_json(200, art_review(data.get("ref"), data.get("item"), data.get("level"), data.get("text")))
            if self.path == "/api/studio/debt-close":
                return self.send_json(200, studio_call(studio.cmd_debt_close, str(data.get("id") or "")))
            if self.path == "/api/studio/anchor":
                return self.send_json(200, studio_call(studio.cmd_anchor, str(data.get("ref") or ""), str(data.get("item") or "")))
            pin, mode = str(data.get("pin") or ""), str(data.get("mode") or "")
            model = str(data.get("model") or DEFAULT_MODEL)
            effort = str(data.get("effort") or DEFAULT_EFFORT)
        except Exception:
            return self.send_error(400)
        self.send_json(200, setup_start(pin, mode, model, effort))


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
