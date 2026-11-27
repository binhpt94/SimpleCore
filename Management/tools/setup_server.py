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
- POST /api/sessions/renew {id, pin} (with the PIN, as it binds): a clear done by the server, no model turn: the
  transcript is moved to the .renewed folder and a new one keeps only its name and a neutral finished first turn (same
  session id, same chat in VS Code), the thread is bound again to the FEATURE it had and its next turn is told by the
  guard line to Read its role rules. A thread in the middle of a turn is only marked; the courier clears it right
  before its next job. A background session is stopped and not started again.
- POST /api/setup {pin, mode, model, effort} checks the PIN, then runs the setup in the background and answers at
  once with the job; GET /api/setup/job is that job as it goes (each name: waiting, running, bound, created or
  error), so the page shows how far it is and which thread failed. One setup runs at a time.
- POST /api/sessions/delete-outside {pin}: the same for every thread of the group Ngoài chức năng (named, outside the
  setup), the PIN checked once.
- POST /api/sessions/delete {id, pin} (with the PIN, and only while VS Code is closed: it keeps the chat list in
  memory and writes it back): moves the thread's transcript and its folder into
  ~/.claude/projects/<project>/.deleted/<time>-<id>/ (with its binding and an info.json), takes its id out of the
  VS Code state (vscode_groups.forget_session) and removes its binding. Restore: move the files back to
  ~/.claude/projects/<project>/ and run Setup to bind it again.
- Live (other sessions see the thread in ListAgents and can message it): a thread is live while a process runs it;
  a headless -p turn exits when done. GET /api/threads gives each thread `live` ("interactive": open in VS Code or a
  terminal; "background"; "") from `claude agents --json`, and `tokens`: what the thread spent since its last
  renew (thread_usage: its transcript and its subagents', read from the files, no model turn). POST /api/sessions/live {id} (no PIN) runs a bound,
  not yet live thread on as a background session, `claude --bg --resume <id> <LIVE_PROMPT>` (one short turn);
  /api/sessions/stop {id} ends it (`claude stop`). Setup does that after a new thread's first turn. Reread and compact
  go into the background session of a thread that has one (stopped, then started again
  with that prompt), so it stays live. `claude attach <name>` opens a background thread in a terminal (to answer a
  permission question); stop it before opening the thread in VS Code. Delete stops it first.
- POST /api/sessions/model {id, model, effort} (no PIN): the human's pick of model and effort for one thread
  (model-choice/<id>.json in the guard store), over the role's for every turn the server runs in it; an idle
  background session is started again on it. GET /api/threads gives each thread `run` {model, effort}: what its next
  server turn uses, and `choice`: whether that is the human's pick. POST /api/sessions/reset-models {} (no PIN)
  removes every pick, so each thread runs on its role's recommended model (ROLE_MODELS) again; refused while any
  thread but the human's runs.
- Inbox courier (a thread of its own while the server runs; dispatch.py holds the messages and settings): every few
  seconds it gives each waiting message of a feature, service, @Core or tool to that thread as a turn of its own
  (start it with the message as its prompt, or stop and start an idle background one), never as a cross-session
  message (a probe showed a session may refuse work that comes that way, and the relay helper may claim a message went
  through when it did not). Delivered = the message id is in the thread's transcript; not seen in time = tried again,
  an error after the last try. A chat open in VS Code or a terminal is never given a turn. In "on-demand" mode a thread
  idle `idle_minutes` is stopped; in both modes a thread that is not running is started only while its pool is under
  its limit (many messages at once must not start dozens of threads); "always-live" (default) stops nothing.
- Game design tab: GET /api/gamedesign re-reads Management/game-design. Without the PIN (the page token is
  enough): POST /api/gamedesign/promote approves one idea node (or a branch) and rebuilds live (gamedesign.approve), /sync
  approves every passing node and rebuilds live (gamedesign.approve_passing), /user-idea adds, edits or
  removes one of the human's own ideas of a proposed node (gamedesign.user_idea), /gpt-pick moves a GPT idea waiting
  beside a fixed node up into it or deletes it (gamedesign.gpt_pick), /delete deletes the ideas and
  nodes the human marked (gamedesign.human_delete), /answers stores the
  human's answers to @GameDesigner's questions, /review the human's score and feedback per idea, /priority the
  human's importance score of a node, /progress the progress of a leaf. With the PIN: /mode switches Full Auto / User control,
  /stage the phase. live is only ever rebuilt from approved.json (gamedesign.build_live).
- Art concepts (studio.py), all without the PIN: POST /api/studio/review stores the human's judgement of one
  delivered picture (level 0-3 or null, and a feedback text; studio.save_review) and tells nobody,
  /anchor marks a picture judged "Đạt" as a style anchor, /debt-close accepts an art debt as it is (GET /api/studio
  re-reads the studio for the Studio tab). GET /Resources/<...> serves the delivered pictures (images
  only, read only) so the page can show them.
"""
import argparse
import calendar
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
import ask  # noqa: E402
import editor_ticket
import worker  # noqa: E402
import reqbook  # noqa: E402
import dispatch  # noqa: E402
import gamedesign  # noqa: E402
import guard  # noqa: E402
import reqbook  # noqa: E402
import studio  # noqa: E402
import sync_page  # noqa: E402
import vscode_groups  # noqa: E402
from vscode_groups import vscode_state  # noqa: E402

# The threads of the setup (người quyết 2026-11-27): the Leaders and above, and the tools. Features, services, @Core
# and the tester are workers (worker.py), started by the server for each job, never a thread.
BASIC = ["@human", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@Codex", "@Blender", "@ClaudeImage", "@DesignAdvisor"]
# Every other named thread (an old feature, service, @Core or tester thread, or any other name) is "outside": the page
# shows it in the group Ngoài chức năng, where it can only be deleted, and nothing runs, binds or delivers to it.
ROLE_GROUPS = {"@human": "Master", "@Manager": "Managers", "@CodeLead": "Leaders", "@GameDesigner": "Leaders",
               "@UIDesigner": "Leaders", "@Codex": "Tools", "@Blender": "Tools", "@ClaudeImage": "Tools", "@DesignAdvisor": "Tools",
               "@UnityAutoPlay": "Testers", "@Core": "Basic"}
GROUP_ORDER = [g for g in bind_session.TIERS if g in ROLE_GROUPS.values() and g != "Testers" and g != "Basic"]      # Master … Tools
STATIC_EXT = (".css", ".js", ".png", ".svg", ".jpg", ".jpeg", ".gif", ".webp", ".ttf")


def thread_name(value):
    """The chat name for a FEATURE value: a role without its '@' (SendMessage reads 'name@team' as a team address,
    so a session named '@Codex' cannot receive messages); a feature keeps its name."""
    return value[1:] if value in ROLE_GROUPS else value


def feature_of(title):
    """The FEATURE value a chat name stands for: 'Codex' and the older '@Codex' are both '@Codex'."""
    return "@" + title if "@" + title in ROLE_GROUPS else title


def group_of(name):
    """Master, Managers, Leaders, Tools, Testers, Basic, Services or Features for a session name; "" for a name outside the setup."""
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
    """The rule files are Read again; a changed skill is not (it is loaded only when used): the thread is told that a
    copy of it already in its context is old, so it calls the skill again the next time it does that task."""
    skills = [c.split("/")[2] for c in changed if c.startswith(bind_session.SKILLS_DIR + "/")]
    rules = [c for c in changed if not c.startswith(bind_session.SKILLS_DIR + "/")]
    told = ("Skill sau vừa đổi: " + ", ".join(skills) + ". Không Read nó bây giờ; bản đã nạp trong ngữ cảnh (nếu có) "
            "đã cũ, lần tới làm việc đó thì gọi lại skill bằng tool Skill. ") if skills else ""
    if not rules:
        return "Đọc lại quy trình: " + told + "Chưa làm việc gì khác. Trả lời ngắn: đã biết."
    return ("Đọc lại quy trình: các file luật sau vừa đổi: " + ", ".join(rules) + ". Read lại toàn bộ đúng các file "
            "đó, không Read file nào khác. " + told + "Chưa làm việc gì khác. Trả lời ngắn: đã đọc những file nào.")


def rules_state(binding):
    """{path from the root: sha1} of the rule files a thread bound to `binding` follows."""
    out = {}
    for rel in bind_session.rule_files(binding, str(ROOT)):
        try:
            out[rel] = hashlib.sha1((ROOT / rel).read_bytes()).hexdigest()
        except OSError:
            pass
    return out


# Renew is a clear the server does itself (người quyết 2026-11-26: no /compact, no turn; the next turn binds and reads
# the rules). `claude --resume <id> -p /clear` does not empty the session (it opens another one), and a transcript
# with no message cannot be resumed ("No conversation found"), so the first user message stays with a neutral text.
# The VS Code extension cannot go on with a transcript whose only message is a user one with no answer: clicking the
# thread opened a new, unbound session (2026-11-26). So the user message is followed by a finished assistant answer,
# written by the server ("<synthetic>" model, as the CLI marks the messages it makes itself).
# A thread in the middle of a turn is only marked (renew-pending); the courier clears it right before its next job.
RENEW_PENDING_DIR = "renew-pending"
RENEWED = ".renewed"      # inside the transcripts folder: the transcripts a renew took away
RENEW_NOTE = "Thread này vừa được làm mới (renew) từ trang Setup: ngữ cảnh cũ đã bỏ."
RENEW_REPLY = "Sẵn sàng."
SYNTHETIC_MODEL = "<synthetic>"
HEAD_TYPES = ("custom-title", "agent-name", "session-owner")      # transcript lines that are not the conversation
# fields of the user line the answer shares (where and how the session ran), never its content
TURN_FIELDS = ("isSidechain", "userType", "entrypoint", "cwd", "sessionId", "version", "gitBranch")


def renew_turn(first):
    """The conversation a renewed transcript keeps: the first user line of the old one with RENEW_NOTE as its only
    content, and a finished assistant answer RENEW_REPLY right after it (same uuid chain, same session fields)."""
    user = dict(first, message={"role": "user", "content": RENEW_NOTE}, parentUuid=None)
    user.pop("toolUseResult", None)
    answer = {k: first[k] for k in TURN_FIELDS if k in first}
    answer.update(type="assistant", parentUuid=user.get("uuid"), uuid=str(uuid.uuid4()), timestamp=first.get("timestamp"),
                  message={"id": "msg_renew_" + uuid.uuid4().hex[:24], "type": "message", "role": "assistant",
                           "model": SYNTHETIC_MODEL, "content": [{"type": "text", "text": RENEW_REPLY}],
                           "stop_reason": "end_turn", "stop_sequence": None,
                           "usage": {"input_tokens": 0, "output_tokens": 0}})
    return [user, answer]


def renew_mark_path(session_id):
    path = guard.binding_path(session_id, guard.STORE_DIR)
    return path and os.path.join(guard.STORE_DIR, RENEW_PENDING_DIR, os.path.basename(path))


def renew_pending(session_id):
    path = renew_mark_path(session_id)
    return bool(path and os.path.isfile(path))


def clear_renew_mark(session_id):
    path = renew_mark_path(session_id)
    if path:
        try:
            os.remove(path)
        except OSError:
            pass


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
              "/api/gamedesign/stage": MAX_BODY,
              "/api/gamedesign/answers": 64 * 1024, "/api/gamedesign/review": 64 * 1024, "/api/gamedesign/user-idea": 64 * 1024, "/api/gamedesign/gpt-pick": 16 * 1024, "/api/gamedesign/delete": 64 * 1024, "/api/gamedesign/sync": MAX_BODY,
              "/api/gamedesign/priority": MAX_BODY, "/api/gamedesign/progress": 64 * 1024, "/api/gamedesign/requirement": 64 * 1024, "/api/vscode-groups": MAX_BODY,
              "/api/references/upload": 12 * 1024 * 1024, "/api/references/note": 64 * 1024,
              "/api/references/delete": MAX_BODY,
              "/api/inbox/send": 16 * 1024, "/api/inbox/cancel": MAX_BODY, "/api/inbox/retry": MAX_BODY,
              "/api/inbox/remove": MAX_BODY, "/api/inbox/clear": MAX_BODY, "/api/inbox/deliver": MAX_BODY,
              "/api/inbox/config": MAX_BODY,
              "/api/studio/review": 64 * 1024, "/api/studio/anchor": MAX_BODY, "/api/studio/debt-close": MAX_BODY,
              "/api/sessions/reread": MAX_BODY, "/api/sessions/compact": MAX_BODY, "/api/sessions/renew": MAX_BODY,
              "/api/sessions/delete": MAX_BODY, "/api/sessions/delete-outside": MAX_BODY, "/api/sessions/live": MAX_BODY, "/api/sessions/stop": MAX_BODY,
              "/api/sessions/model": MAX_BODY, "/api/sessions/reset-models": MAX_BODY,
              "/api/workers/run": 16 * 1024, "/api/workers/resume": 16 * 1024, "/api/workers/drop": MAX_BODY,
              "/api/workers/defaults": MAX_BODY}
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


# A chat with no name shows in VS Code under a title the CLI or the extension made: its ai-title, else its last prompt,
# else its first message (extension 2.1.332). The first message of a chat is often the human's "FEATURE=<value> <PIN>",
# so such a title may hold the PIN: redact_title() hides it before the page sees it.
AI_TITLE_RX = re.compile(r'"type"\s*:\s*"ai-title".*?"aiTitle"\s*:\s*"((?:[^"\\]|\\.)*)"')
LAST_PROMPT_RX = re.compile(r'"type"\s*:\s*"last-prompt".*?"lastPrompt"\s*:\s*"((?:[^"\\]|\\.)*)"')
PIN_RX = re.compile(r"(FEATURE\s*=\s*\S+)\s+\S+", re.I)
TITLE_MAX = 80


def redact_title(text):
    """A made-up title as the page shows it: one line, at most TITLE_MAX characters, the PIN of a FEATURE line hidden."""
    text = PIN_RX.sub(r"\1 ••••", " ".join(str(text).split()))
    return text if len(text) <= TITLE_MAX else text[:TITLE_MAX - 1] + "…"


def first_text(o):
    """The text of a transcript's first real user message (not a meta line, not a tool result); "" otherwise."""
    if o.get("type") != "user" or o.get("isMeta") or o.get("isSidechain"):
        return ""
    c = (o.get("message") or {}).get("content")
    if isinstance(c, str):
        return c
    return " ".join(b.get("text", "") for b in c if isinstance(b, dict) and b.get("type") == "text") if isinstance(c, list) else ""


def threads(include_unnamed=False):
    """[{id, title, named, feature, group, legacy, archived, binding, model, effort, modified}] for this project's
    sessions that have a name. feature: the FEATURE value the name stands for; legacy: a role named with its '@' (rename it).
    model: of the last answer; effort: of the last request (both as the transcript recorded them).
    include_unnamed: also the sessions with no name, under the title VS Code shows them by (named False, no feature):
    the page lists every chat VS Code lists; every other caller works on named threads only."""
    hidden = archived_ids()
    live = live_sessions()
    waiting = {}
    for m in dispatch.pending(ROOT):
        waiting[m["target"]] = waiting.get(m["target"], 0) + 1
    out = []
    folder = transcripts_dir()
    for p in folder.glob("*.jsonl") if folder.is_dir() else []:
        title, model, effort, entries = None, "", "", set()
        made, last_prompt, first = None, None, None
        with open(p, encoding="utf-8", errors="replace") as f:
            for line in f:
                if include_unnamed and title is None:
                    if '"ai-title"' in line and (m := AI_TITLE_RX.search(line)):
                        made = m.group(1)
                    elif '"last-prompt"' in line and (m := LAST_PROMPT_RX.search(line)):
                        last_prompt = m.group(1)
                    elif first is None and '"user"' in line:
                        try:
                            first = first_text(json.loads(line)) or None
                        except ValueError:
                            pass
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
        named = bool(title)
        if not named and include_unnamed:
            raw = next((x for x in (made, last_prompt) if x), None)
            try:
                raw = json.loads(f'"{raw}"') if raw else first
            except ValueError:
                raw = first
            title = redact_title(raw) if raw and raw.strip() else None
        if title:
            sid = p.stem
            feature = feature_of(title) if named else ""
            outside = named and feature not in BASIC
            out.append({"id": sid, "title": title, "named": named, "feature": feature, "group": "" if outside or not named else group_of(feature),
                        "legacy": named and title in ROLE_GROUPS, "outside": outside, "archived": sid in hidden or outside,
                        "unlisted": entries == {HEADLESS_ENTRY},
                        "model": "" if model == "<synthetic>" else model, "effort": effort,
                        "binding": (binding := guard.bound_feature(sid, guard.STORE_DIR) or ""),
                        "choice": bool(model_choice(sid)),
                        "run": dict(zip(("model", "effort"), (lambda m: (model_alias(m[0]), m[1]))(run_model(
                            {"id": sid, "binding": binding, "feature": feature, "model": model, "effort": effort})))),
                        "profiled": profiled(binding or feature),
                        "live": (live.get(sid) or {}).get("kind", ""), "live_status": (live.get(sid) or {}).get("status", ""),
                        "live_state": (live.get(sid) or {}).get("state", ""),
                        "hung": hang_text(h) if (h := hang_of({"id": sid}, live.get(sid), time.time())) else "",
                        "tokens": thread_usage(sid),
                        "needs": job_needs(sid), "renew_pending": renew_pending(sid), "inbox": waiting.get(guard.bound_feature(sid, guard.STORE_DIR) or "", 0),
                        "modified": time.strftime("%Y-%m-%d %H:%M", time.localtime(p.stat().st_mtime))})
    return sorted(out, key=lambda t: (t["title"].lower(), t["modified"]))


# Tokens a thread spent, from the `usage` the CLI writes on each answer. cost: input-price tokens, the price ratios being
# the same for every model (cache write 1h ×2, 5m ×1.25, cache read ×0.1, output ×5), so threads compare by one number.
USAGE_KEYS = ("calls", "input", "cache_write", "cache_read", "output", "cost")
USAGE_CACHE = {}      # transcript path -> ((mtime, size), its usage): the page asks for the list every few seconds
USAGE_LOCK = threading.Lock()


def usage_of(path):
    """{calls, input, cache_write, cache_read, output, cost, first, renewed} of one transcript. An answer written as
    several lines (thinking, text, tool use) carries the same usage on each: counted once by its message id. The
    synthetic answer of a renew costs nothing. first: time of the first user line; renewed: that line is RENEW_NOTE."""
    try:
        st = os.stat(path)
    except OSError:
        return dict.fromkeys(USAGE_KEYS, 0)
    stamp = (st.st_mtime_ns, st.st_size)
    with USAGE_LOCK:
        hit = USAGE_CACHE.get(str(path))
    if hit and hit[0] == stamp:
        return dict(hit[1])
    out = dict.fromkeys(USAGE_KEYS, 0)
    out.update(first="", renewed=False)
    seen = set()
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            if not out["first"] and '"user"' in line:      # a renew writes its line with spaces: "type": "user"
                try:
                    o = json.loads(line)
                except ValueError:
                    continue
                if o.get("type") == "user" and not o.get("isMeta") and isinstance(o.get("message"), dict):
                    out["first"] = str(o.get("timestamp") or "")
                    out["renewed"] = (o["message"].get("content") == RENEW_NOTE)
                continue
            if '"assistant"' not in line or '"usage"' not in line:
                continue
            try:
                m = json.loads(line).get("message") or {}
            except ValueError:
                continue
            u = m.get("usage")
            if not isinstance(u, dict) or m.get("role") != "assistant" or m.get("model") == SYNTHETIC_MODEL or m.get("id") in seen:
                continue
            seen.add(m.get("id"))
            write = int(u.get("cache_creation_input_tokens") or 0)
            split = u.get("cache_creation") if isinstance(u.get("cache_creation"), dict) else {}
            hour = int(split.get("ephemeral_1h_input_tokens") or 0)
            inp, read, outp = (int(u.get(k) or 0) for k in ("input_tokens", "cache_read_input_tokens", "output_tokens"))
            out["calls"] += 1
            out["input"] += inp
            out["cache_write"] += write
            out["cache_read"] += read
            out["output"] += outp
            out["cost"] += inp + hour * 2 + max(write - hour, 0) * 1.25 + read * 0.1 + outp * 5
    out["cost"] = int(round(out["cost"]))
    with USAGE_LOCK:
        USAGE_CACHE[str(path)] = (stamp, dict(out))
    return out


def thread_usage(session_id):
    """What thread session_id spent since its last renew (a renew moves the transcript and its folder away): its
    transcript plus its subagents' (<id>/subagents/*.jsonl), with subagent_calls apart. since: time of its first
    line, or of the last renew (local, "MM-DD HH:MM"); renewed: counted from a renew, else from the thread's start."""
    folder = transcripts_dir()
    main = usage_of(folder / f"{session_id}.jsonl")
    total = {k: main.get(k, 0) for k in USAGE_KEYS}
    total["subagent_calls"] = 0
    sub = folder / session_id / "subagents"
    for p in sorted(sub.glob("*.jsonl")) if sub.is_dir() else []:
        u = usage_of(p)
        for k in USAGE_KEYS:
            total[k] += u.get(k, 0)
        total["subagent_calls"] += u.get("calls", 0)
    since = ""
    try:
        moved = sorted(x.name for x in (folder / RENEWED).glob(f"*-{session_id}")) if main.get("renewed") else []
        if moved:      # the renew line keeps the old first line's time: the time of the renew is in its folder's name
            since = time.strftime("%m-%d %H:%M", time.strptime(moved[-1][:15], "%Y%m%d-%H%M%S"))
        elif main.get("first"):
            since = time.strftime("%m-%d %H:%M", time.localtime(
                calendar.timegm(time.strptime(main["first"][:19], "%Y-%m-%dT%H:%M:%S"))))      # the CLI writes UTC
    except (ValueError, OverflowError):
        since = ""
    total.update(since=since, renewed=bool(main.get("renewed")))
    return total


def job_needs(session_id):
    """What the CLI's daemon says a background job waits for from the human (its state.json: state "blocked" and a
    `needs` line, e.g. a proposal to approve), or ''. The job keeps it after its process was retired."""
    base = Path(os.environ.get("CLAUDE_CONFIG_DIR") or Path.home() / ".claude")
    try:
        st = json.loads((base / "jobs" / session_id[:8] / "state.json").read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return ""
    return str(st.get("needs") or "")[:200] if isinstance(st, dict) and st.get("state") == "blocked" else ""


def sets():
    """The names of the setup (người quyết 2026-11-27): the Leaders and above, and the tools, in the order of the
    groups. Features, services, @Core and the tester are workers, never a thread. `full` is the same list (the page
    has one Setup button; the API still takes mode basic or full)."""
    return {"basic": BASIC, "full": list(BASIC), "groups": {n: group_of(n) for n in BASIC}, "order": GROUP_ORDER}


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
    try:
        p = subprocess.run(["claude", "agents", "--json"], cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=20, shell=(os.name == "nt"))
        value = parse_agents(json.loads(p.stdout or "[]") if p.returncode == 0 else [])
    except (OSError, ValueError, subprocess.SubprocessError):
        value = {}
    with LIVE_LOCK:
        LIVE_CACHE.update(at=time.time(), value=value)
    return dict(value)


def parse_agents(rows):
    """{session id: {kind, status, state, short}} of the sessions that RUN now. The CLI also lists a background job
    whose process its daemon retired (idle about an hour) or that waits for the human, with no `pid` and no `status`:
    that thread is not running, a message sent to it goes nowhere, so it is left out (probe, 2026-11-25)."""
    value = {}
    for s in rows if isinstance(rows, list) else []:
        if isinstance(s, dict) and isinstance(s.get("sessionId"), str) and s.get("pid") and s.get("status"):
            value[s["sessionId"]] = {"kind": str(s.get("kind") or ""), "status": str(s.get("status") or ""), "pid": s.get("pid"),
                                     "state": str(s.get("state") or ""), "short": str(s.get("id") or s["sessionId"][:8])}
    return value


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
            # `--` before the prompt: --mcp-config (the role profile) takes several values, and a prompt after it
            # was read as one more config file, so the session started with no prompt and never showed up.
            out = run_claude(["--bg", "--resume", session_id] + extra + ["--", prompt])
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


def kill_pid(pid):
    """Ends one process and what it started (the chat's own MCP servers)."""
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(int(pid)), "/T", "/F"], capture_output=True, timeout=20)
    else:
        os.kill(int(pid), 15)


def close_chat(session_id):
    """Ends the process of a chat open in VS Code or a terminal and waits until the session is gone, so the thread
    can run in the background under its own id. Only an idle chat: one in the middle of a turn is left alone. The
    transcript stays; the tab in VS Code says the session ended. Nothing when no such chat runs."""
    s = live_sessions(fresh=True).get(session_id)
    if not s or s["kind"] == "background":
        return
    if s["status"] != "idle":
        raise RuntimeError("chat đang chạy một lượt")
    if not s.get("pid"):
        raise RuntimeError("không biết tiến trình của chat")
    kill_pid(s["pid"])
    if not wait_live(session_id, False):
        raise RuntimeError(f"chat vẫn chạy sau {LIVE_SECONDS}s (pid {s['pid']})")


# ---- the profile of a role (người quyết 2026-11-26: cắt phần khởi đầu cố định theo vai, model theo vai). Every
# turn the server runs carries what its role needs and no more: the MCP servers it uses (--strict-mcp-config, which
# also keeps the claude.ai connectors out), only its own skills (skillOverrides "off" for the others: the project's
# skills of other roles, the user's skills, the claude.ai skills), no bundled skills, no easel, no Artifact /
# Workflow / ScheduleWakeup / ReportFindings tools. Measured on the first call of a turn: 51.7k tokens without it,
# about 20-26k with it. A chat opened by hand in VS Code does not go through the server and gets none of this;
# the human's own thread never does.
ROLE_SKILLS = {"@Manager": "manager-", "@CodeLead": "codelead-", "@GameDesigner": "gd-", "@UIDesigner": "ui-",
               "@Codex": "studio-", "@Blender": "studio-", "@ClaudeImage": "studio-", "@DesignAdvisor": "advisor-",
               "@UnityAutoPlay": "tester-", "@Core": "feature-"}
GROUP_SKILLS = {"Services": "feature-", "Features": "feature-"}
UNITY_ROLES = {"@CodeLead", "@Core", "@UnityAutoPlay"}      # and every Services and Features thread
UNITY_SKILL = "unity-mcp-skill"
# The MCP servers of .mcp.json each role runs with. blender is in .mcp.json but off for the chats opened in VS Code
# (disabledMcpjsonServers in .claude/settings.json): only @Blender loads it (người quyết 2026-11-26).
ROLE_SERVERS = {"@Blender": ("blender",)}
OFF_TOOLS = "Artifact,Workflow,ScheduleWakeup,ReportFindings"
OFF_PLUGINS = ("cc-plugin-easel@builtin", "plugin-authoring@builtin")
CLAUDEAI_SKILL = "anthropic-skills:"
# (model, effort) by role, the recommended ones (người quyết 2026-11-27): a new thread is created on them and every
# server turn runs on them unless the human picked another for that thread (model_choice; the page's "Đặt lại model
# khuyến nghị" removes every pick). A name not in these tables keeps what the page chose.
LOW, MID = ("opus", "low"), ("opus", "medium")
S_MID = ("sonnet", "medium")
ROLE_MODELS = {"@Manager": S_MID, "@CodeLead": MID, "@GameDesigner": MID, "@UIDesigner": MID,
               "@DesignAdvisor": LOW, "@Codex": S_MID, "@ClaudeImage": MID,
               "@Blender": LOW,      # chỉ làm art tạm, tiết kiệm (người quyết 2026-11-27)
               "@UnityAutoPlay": S_MID}      # the tester's profile (server's runner, play jobs); no thread of its own
PROFILE_DIR = ".profiles"      # in the transcripts folder: the generated settings and MCP files of each role
PROFILES = True      # tests of the plain CLI lines turn it off


def config_dir():
    return Path(os.environ.get("CLAUDE_CONFIG_DIR") or Path.home() / ".claude")


def profiled(feature):
    """Whether a thread bound to `feature` runs with a role profile: a role of the setup or a feature/service; not the
    human's thread, not a name outside the setup."""
    return PROFILES and bool(feature) and feature != "@human" and group_of(feature) not in ("", "Master")


def role_model(feature, model, effort):
    """The (model, effort) a thread of that role runs on: the role's own when it has one, else the ones given."""
    pick = recommended_model(feature) if profiled(feature) else None
    return pick or (model, effort)


def recommended_model(feature):
    """The recommended (model, effort) of that role, or None. A worker's model is worker.py's (code opus/medium, play
    sonnet/medium)."""
    return ROLE_MODELS.get(feature)


def skill_names():
    """Every skill a session of this project could list: the project's, the user's, the claude.ai ones (synced)."""
    names = set()
    for folder in (ROOT / ".claude" / "skills", config_dir() / "skills"):
        if folder.is_dir():
            names |= {p.name for p in folder.iterdir() if p.is_dir() and p.name != "synced"}
    synced = config_dir() / "skills" / "synced"
    if synced.is_dir():
        names |= {CLAUDEAI_SKILL + p.name for p in synced.glob("*/*") if p.is_dir()}
    return names


def role_settings(feature):
    """The settings a thread of that role runs with: only its own skills listed, no bundled ones, no claude.ai
    connectors, no easel."""
    keep = ROLE_SKILLS.get(feature) or GROUP_SKILLS.get(group_of(feature)) or "\0"
    unity = uses_unity(feature)
    off = {n: "off" for n in sorted(skill_names()) if not n.startswith(keep) and not (unity and n == UNITY_SKILL)}
    return {"skillOverrides": off, "disableBundledSkills": True, "disableClaudeAiConnectors": True,
            "enabledPlugins": {p: False for p in OFF_PLUGINS}}


def uses_unity(feature):
    return feature in UNITY_ROLES or group_of(feature) in ("Services", "Features")


def role_mcp(feature):
    """The MCP servers of that role, as .mcp.json defines them: UnityMCP for the roles that drive Unity, blender for
    @Blender (ROLE_SERVERS), none else."""
    names = set(ROLE_SERVERS.get(feature, ())) | ({"UnityMCP"} if uses_unity(feature) else set())
    if not names:
        return {"mcpServers": {}}
    try:
        defined = json.loads((ROOT / ".mcp.json").read_text(encoding="utf-8"))["mcpServers"]
    except (OSError, ValueError, KeyError):
        defined = {}
    return {"mcpServers": {k: v for k, v in defined.items() if k in names}}


def profile_args(feature):
    """The CLI flags of the role profile ([] for the human's thread or a name outside the setup). The settings and
    the MCP list go into files (a JSON argument would not survive cmd.exe on Windows)."""
    if not profiled(feature):
        return []
    folder = transcripts_dir() / PROFILE_DIR
    folder.mkdir(parents=True, exist_ok=True)
    stem = re.sub(r"[^A-Za-z0-9]+", "-", feature).strip("-") or "role"
    settings = folder / f"{stem}.settings.json"
    settings.write_text(json.dumps(role_settings(feature), ensure_ascii=False, indent=1), encoding="utf-8")
    args = ["--settings", str(settings), "--disallowedTools", OFF_TOOLS]
    mcp = folder / f"{stem}.mcp.json"
    mcp.write_text(json.dumps(role_mcp(feature), ensure_ascii=False, indent=1), encoding="utf-8")
    args += ["--strict-mcp-config", "--mcp-config", str(mcp)]
    return args


# The model and effort the human picked for one thread on the page (POST /api/sessions/model): model-choice/<id>.json
# in the guard store, {model, effort}. It wins over the role's (ROLE_MODELS) for every turn the server runs in it,
# and stays across a renew (same session). A chat opened by hand in VS Code picks its model there.
MODEL_CHOICE_DIR = "model-choice"


def model_choice_path(session_id):
    path = guard.binding_path(session_id, guard.STORE_DIR) if isinstance(session_id, str) else None
    return path and os.path.join(guard.STORE_DIR, MODEL_CHOICE_DIR, os.path.basename(path))


def model_choice(session_id):
    """(model, effort) the human picked for that thread, or None."""
    path = model_choice_path(session_id)
    got = bind_session.read_json(path, None) if path and os.path.isfile(path) else None
    if isinstance(got, dict) and got.get("model") in MODELS and got.get("effort") in EFFORTS:
        return got["model"], got["effort"]
    return None


def model_alias(model):
    """The alias of MODELS a model id stands for ("claude-opus-5-5" -> "opus"), else the id itself."""
    return next((a for a in MODELS if a in (model or "")), model or "")


def run_model(t):
    """(model, effort) the next turn the server runs in thread t uses: the human's pick, else the role's, else the
    ones it last used."""
    picked = model_choice(t.get("id"))
    if picked:
        return picked
    return role_model(t.get("binding") or t.get("feature") or "", t.get("model") or "", t.get("effort") or "")


def turn_args(t):
    """The model and effort of the thread (run_model: the human's pick, the role's, else the ones it last used), and
    its role profile, as CLI flags."""
    feature = t.get("binding") or t.get("feature") or ""
    model, effort = run_model(t)
    args = []
    if model:
        args += ["--model", model]
    if effort in EFFORTS:
        args += ["--effort", effort]
    return args + profile_args(feature)


# A background thread (claude --bg) edits the shared checkout: the one Unity has open. Without this the CLI blocks its
# Edit and Write until it calls EnterWorktree (worktree.bgIsolation, also set in .claude/settings.json).
CLAUDE_ENV = {"CLAUDE_CODE_ENTRYPOINT": VSCODE_ENTRY, "CLAUDE_BG_ISOLATION": "none"}


NEWLINE_MARK = " ⏎ "


def claude_command(args):
    """(the command line, whether it must go through the shell) for the claude CLI. The CLI is run directly: through
    cmd.exe (shell=True) an argument is cut at its first newline, so a prompt of several lines reached the thread as
    its first line only (the inbox gave messages without their text). Only a CLI that is a .cmd or .bat shim (an npm
    install) still needs cmd.exe; its newlines are then written as a mark so that nothing is lost."""
    exe = shutil.which("claude") or "claude"
    if os.name == "nt" and exe.lower().endswith((".cmd", ".bat")):
        return [exe] + [a.replace("\r\n", "\n").replace("\n", NEWLINE_MARK) for a in args], True
    return [exe] + list(args), False


# Claude in Chrome is off in every thread the server runs (người quyết 2026-11-26: không dùng Chrome trong project):
# while it is on, each time the extension connects or drops, the tool list at the head of the context changes and
# every live session writes its whole cache again. Commands that run no turn take no such flag.
NO_TURN_COMMANDS = ("agents", "stop", "rm")
SESSION_FLAGS = ["--no-chrome"]


def run_claude(args):
    """Runs the claude CLI in the project (one headless turn) and returns what it printed; raises with its error."""
    if args and args[0] not in NO_TURN_COMMANDS:
        args = SESSION_FLAGS + list(args)
    command, shell = claude_command(args)
    p = subprocess.run(command, cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8", errors="replace",
                       env=dict(os.environ, **CLAUDE_ENV),
                       timeout=CLAUDE_SECONDS, shell=shell)
    if p.returncode != 0:
        raise RuntimeError((p.stderr or p.stdout or f"exit {p.returncode}").strip()[:300])
    return (p.stdout or "").strip()


def create(name, feature, model, effort):
    """New named thread on that model and effort, bound to `feature` BEFORE its first turn, so that turn already
    knows its role and reads the right rules; returns its session id. Costs one turn."""
    sid = str(uuid.uuid4())
    bind(sid, feature)
    model, effort = role_model(feature, model, effort)
    try:
        run_claude(["-n", name, "--session-id", sid, "--model", model, "--effort", effort] + profile_args(feature)
                   + ["-p", f"Thread {name} được tạo bởi trang Setup. {REREAD_PROMPT}"])
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


# The chats the human talks to in VS Code's chat panel (người quyết 2026-11-27): the human's own and @Manager. They
# are never run in the background (VS Code could not open them then); @Manager runs there without its role profile
# (about twice the tokens a turn: the panel takes no per-chat flags; người quyết 2026-11-27 to keep the panel).
VSCODE_CHATS = (HUMAN, "@Manager")


def human_thread(t):
    """A chat the human talks to in VS Code (VSCODE_CHATS): never run in the background."""
    return t.get("binding") in VSCODE_CHATS or t.get("feature") in VSCODE_CHATS


def go_live(session_id):
    """The Bật live button: runs one existing, bound thread on as a background session (one short turn), so other
    sessions see it in ListAgents and can message it. A thread that already runs (VS Code, terminal, background) is
    left as it is."""
    t, no = eligible(session_id)
    if no:
        return no
    if human_thread(t) and not t.get("live"):
        return {"ok": False, "id": t["id"], "title": t["title"],
                "error": "chat người dùng trực tiếp (@human, @Manager): không chạy nền (VS Code sẽ không mở được nó); mở nó trong VS Code là live"}
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


def set_model(session_id, model, effort):
    """The confirm button of the Model and Effort pickers of one row: saves the human's pick for that thread
    (model_choice), used by every turn the server runs in it from now on. A background session that is idle is
    started again on it right away (one short turn); one in a turn keeps its model until it is started again. A
    chat open in VS Code or a terminal keeps what it runs on there."""
    if model not in MODELS or effort not in EFFORTS:
        return {"ok": False, "error": f"model phải là một trong {MODELS}, effort một trong {EFFORTS}"}
    t, no = eligible(session_id)
    if no:
        return no
    path = model_choice_path(t["id"])
    if not path:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": "thread chưa gán FEATURE: chạy Setup trước"}
    bind_session.write_json(path, {"model": model, "effort": effort})
    head = {"ok": True, "id": t["id"], "title": t["title"], "model": model, "effort": effort}
    if t.get("live") == "interactive":
        return dict(head, answer=f"đã lưu {model}, {effort} cho lượt trang chạy; chat đang mở ở VS Code giữ model chọn ở đó")
    if t.get("live") != "background":
        return dict(head, answer=f"đã lưu {model}, {effort}: áp từ lượt kế tiếp")
    with BUSY_LOCK:
        held = t["id"] in BUSY
    if held or t.get("live_status") not in ("idle", ""):
        return dict(head, answer=f"đã lưu {model}, {effort}; phiên nền đang chạy lượt nên giữ model cũ tới khi được bật lại")
    try:
        turn(t, LIVE_PROMPT, bg=True)
    except Exception as ex:
        return dict(head, ok=False, error=f"đã lưu {model}, {effort} nhưng chưa bật lại phiên nền được: " + str(ex)[:220])
    return dict(head, answer=f"đã đổi sang {model}, {effort} và bật lại phiên nền")


def asks_view():
    """The questions for the human that wait (ask.py, read only), for the page: the human sees them before opening
    the Manager's chat; Manager asks them and writes the answers."""
    try:
        return [{k: x.get(k) for k in ("id", "source", "job", "blocker", "options", "suggest", "waiting", "created")}
                for x in ask.items(ROOT, "open")]
    except Exception:
        return []


def reset_models():
    """The page's "Đặt lại model khuyến nghị": removes the human's pick of every thread (model-choice/), so each runs
    on its role's recommended model and effort again. Only while no thread runs: none live (background, VS Code or a
    terminal; the human's own chat aside), no turn of this server, no setup job. A running one would keep its old
    model until started again."""
    with BUSY_LOCK:
        held = set(BUSY)
    with JOB_LOCK:
        job_running = bool(JOB and not JOB["done"])
    listed = [t for t in threads() if not t["archived"] and not t["unlisted"]]
    running = [t["title"] for t in listed if (t.get("live") or t["id"] in held) and not human_thread(t)]
    if job_running or running:
        why = "Setup đang chạy" if job_running else "còn thread đang chạy: " + ", ".join(sorted(running))
        return {"ok": False, "error": why + ". Tắt live (hoặc đóng chat trong VS Code) rồi bấm lại.", "running": running}
    folder = os.path.join(guard.STORE_DIR, MODEL_CHOICE_DIR)
    removed = 0
    if os.path.isdir(folder):
        for name in os.listdir(folder):
            if name.endswith(".json"):
                try:
                    os.remove(os.path.join(folder, name))
                    removed += 1
                except OSError:
                    pass
    table = {t["title"]: "{}, {}".format(*recommended_model(t["binding"])) for t in listed
             if t["binding"] and profiled(t["binding"]) and recommended_model(t["binding"])}
    return {"ok": True, "removed": removed, "models": table,
            "answer": f"đã bỏ {removed} lựa chọn riêng; mọi thread chạy theo model khuyến nghị của vai"}


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


def wipe_transcript(t):
    """The clear of a renew, without a model turn: a background session of thread t is stopped (it would write the
    transcript back), its transcript and folder go to <transcripts>/.renewed/<time>-<id>/, and a new transcript keeps
    only the lines that are not the conversation (name, owner) and one neutral finished turn (renew_turn: a transcript
    without a message cannot be resumed, and VS Code opens a new session for one with no answer). Then the thread is bound again to the FEATURE it had and its next
    turn gets the guard line that says to Read its role rules (bind_session.rules_reminder)."""
    sid = t["id"]
    if t.get("live") == "background":
        stop_bg(sid)
        t["live"] = ""
    folder = transcripts_dir()
    src = folder / f"{sid}.jsonl"
    head, first = {}, None
    if src.is_file():
        with open(src, encoding="utf-8", errors="replace") as f:
            for line in f:
                try:
                    o = json.loads(line)
                except ValueError:
                    continue
                if not isinstance(o, dict):
                    continue
                if o.get("type") in HEAD_TYPES:
                    head[o["type"]] = o      # the last one: a thread may have been renamed
                elif o.get("type") == "user" and first is None and isinstance(o.get("message"), dict):
                    first = o
    if first is not None:      # a transcript with no message has no context to drop
        trash = folder / RENEWED / f"{time.strftime('%Y%m%d-%H%M%S')}-{sid}"
        trash.mkdir(parents=True, exist_ok=True)
        for old in (src, folder / sid):
            if old.exists():
                shutil.move(str(old), str(trash / old.name))
        lines = [head[k] for k in HEAD_TYPES if k in head] + renew_turn(first)
        src.write_text("".join(json.dumps(o, ensure_ascii=False) + "\n" for o in lines), encoding="utf-8", newline="\n")
    bind(sid, t["binding"])
    bind_session.forget_rules_read(sid, guard.STORE_DIR)
    # its next turn Reads every role rule file anyway: the page's "Đọc lại quy trình" must not add a turn for them
    mark_rules_seen(sid, rules_state(t["binding"]))


def renew(session_id, pin):
    """Makes one existing, bound thread new again, in the same chat (wipe_transcript): no model turn; its next turn
    is told to read the rules. With the PIN, as every binding. A thread in the middle of a turn is only marked: the
    courier clears it right before its next job. A background session is stopped and not started again (người quyết
    2026-11-26)."""
    t, no = eligible(session_id)
    if no:
        return no
    no = open_elsewhere(t, "gõ /clear trong chat đó (rồi gán lại FEATURE nếu guard báo chưa gán), hoặc đóng chat rồi bấm Renew lại")
    if no:      # before the PIN: closing the chat must not cost a try
        return no
    error = bind_session.check_pin(guard.STORE_DIR, pin if isinstance(pin, str) else "")
    if error:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": error, "pin": True}
    with BUSY_LOCK:
        busy_now = t["id"] in BUSY
    if busy_now or t.get("live_status") == "busy":
        bind_session.write_json(renew_mark_path(t["id"]), {"binding": t["binding"], "at": time.strftime("%Y-%m-%d %H:%M:%S")})
        return {"ok": True, "id": t["id"], "title": t["title"], "pending": True,
                "answer": "thread đang chạy một lượt: đã đánh dấu, server làm mới ngay trước việc kế tiếp hộp thư giao cho nó"}
    try:
        wipe_transcript(t)
    except Exception as ex:
        return {"ok": False, "id": t["id"], "title": t["title"], "error": "chưa làm mới được: " + str(ex)[:260]}
    clear_renew_mark(t["id"])
    return {"ok": True, "id": t["id"], "title": t["title"],
            "answer": "đã làm mới, gán lại " + t["binding"] + "; lượt sau sẽ đọc lại quy trình (không chạy lượt nào)"}


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
                m, e = role_model(name, model, effort)
                done = {"state": "created", "ids": [sid], "model": m, "effort": e}
                if name in VSCODE_CHATS:
                    done.update(live=False, detail="chat người dùng trực tiếp: mở nó trong VS Code, không chạy nền")
                    with JOB_LOCK:
                        item.update(done, ended=time.time())
                    continue
                if dispatch.pool_of(name) in dispatch.ON_DEMAND and dispatch.config(ROOT)["mode"] == "on-demand":
                    done.update(live=False, detail="chạy theo việc (hộp thư): không bật live")
                    with JOB_LOCK:
                        item.update(done, ended=time.time())
                    continue
                with JOB_LOCK:
                    item.update(done, detail="đã đọc quy trình, đang bật live…")
                try:      # then run it on in the background, so the other sessions see it (one more short turn)
                    busy(sid, True)
                    start_bg(sid, ["-n", thread_name(name)] + turn_args({"binding": name, "model": model, "effort": effort}),
                             LIVE_PROMPT)
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


def delete_thread(session_id, pin, listed=None, pin_ok=False):
    """The Xoá button of one thread: moves its transcript (and its folder) into the trash of the transcripts folder,
    takes it out of the VS Code state and removes its binding. With the PIN, and only while VS Code is closed (it
    would write its chat list back). Archived and hidden threads may be deleted too. listed, pin_ok: from
    delete_outside, which reads the list and checks the PIN once."""
    pool = listed if listed is not None else threads(include_unnamed=True)
    t = next((t for t in pool if t["id"] == session_id), None) if isinstance(session_id, str) else None
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
    error = None if pin_ok else bind_session.check_pin(guard.STORE_DIR, pin if isinstance(pin, str) else "")
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


def delete_outside(pin):
    """The button "Xoá cả nhóm": deletes every thread of the group Ngoài chức năng (a named thread outside the setup:
    old feature, service, @Core, tester threads) like delete_thread, the PIN checked once. Chats with no name and
    the threads of the setup are never touched."""
    listed = threads(include_unnamed=True)
    todo = [t for t in listed if t.get("outside")]
    if not todo:
        return {"ok": True, "deleted": [], "failed": [], "trash": ""}
    if vscode_groups.vscode_running():
        return {"ok": False, "error": "VS Code đang chạy: tắt hẳn VS Code (mọi cửa sổ) rồi bấm lại", "vscode": True}
    error = bind_session.check_pin(guard.STORE_DIR, pin if isinstance(pin, str) else "")
    if error:
        return {"ok": False, "error": error, "pin": True}
    deleted, failed, trash = [], [], ""
    for t in todo:
        res = delete_thread(t["id"], None, listed=listed, pin_ok=True)
        if res.get("ok"):
            deleted.append(t["title"])
            trash = str(Path(res["trash"]).parent)
        else:
            failed.append(f'{t["title"]} ({t["id"][:8]}): {res.get("error")}')
    return {"ok": not failed, "deleted": deleted, "failed": failed, "trash": trash,
            "error": "; ".join(failed) if failed else ""}


def promote(path, deep):
    """The Chốt button of the Game design tab: approves one idea node (deep: also what passes below it) and
    rebuilds live (gamedesign.approve). No PIN: the page token shows the request comes from the page this server
    served, and the guard lets no feature or role session run or call this server."""
    try:
        done = gamedesign.approve(ROOT, path, deep)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "approved": done["approved"], "held": done["build"]["held"], "data": sync_page.write_gamedesign(ROOT)}


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


def sync_live():
    """The Sync button: approves every passing idea node and rebuilds live (gamedesign.approve_passing). No PIN
    (người quyết 2026-11-26), like Chốt."""
    try:
        done = gamedesign.approve_passing(ROOT)
    except (ValueError, OSError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "approved": done["approved"], "held": done["build"]["held"], "data": sync_page.write_gamedesign(ROOT)}


def set_mode(pin, mode):
    """The mode switch of the Game design tab (Full Auto / User control); needs the PIN."""
    if mode not in gamedesign.MODES:      # before the PIN: a bad request must not cost a try
        return {"ok": False, "error": f"mode phải là một trong {gamedesign.MODES}"}
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return {"ok": False, "error": error}
    gamedesign.write_mode(ROOT, mode)
    return {"ok": True, "data": sync_page.write_gamedesign(ROOT)}


def set_stage(pin, stage):
    """The "Đang làm" switch of the Game design tab: the phase the project is built for now (IDEA, PROTOTYPE,
    FULL). Needs the PIN: it decides which nodes must reach round 4 and which requirements are work."""
    if stage not in gamedesign.stages():      # before the PIN: a bad request must not cost a try
        return {"ok": False, "error": f"giai đoạn phải là một trong {gamedesign.stages()}"}
    error = bind_session.check_pin(guard.STORE_DIR, pin)
    if error:
        return {"ok": False, "error": error}
    gamedesign.write_stage(ROOT, stage)
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


def user_idea(data):
    """The buttons Thêm ý / Sửa / Xoá of a node's panel: the human's own idea, written into the proposed node as it
    is, labelled Người (gamedesign.user_idea). No PIN, like the scores."""
    try:
        iid = gamedesign.user_idea(ROOT, data.get("path"), data.get("action"), data.get("id"),
                                   data.get("title") or "", data.get("detail") or "")
    except (ValueError, OSError, TypeError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "id": iid, "data": sync_page.write_gamedesign(ROOT)}


def gpt_pick(data):
    """The buttons Chuyển lên / Xoá of an idea GPT proposed for a node already in live (người quyết 2026-11-27): only
    the human moves it into the node (scored PICK_PROMOTE_SCORE) or deletes it (gamedesign.gpt_pick). No PIN."""
    try:
        iid = gamedesign.gpt_pick(ROOT, data.get("folder"), data.get("aid"), data.get("id"), data.get("action"))
    except (ValueError, OSError, TypeError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, "id": iid, "data": sync_page.write_gamedesign(ROOT)}


def human_delete(data):
    """The floating Save of the Game design tab: the ideas and nodes the human marked for deletion, in one go
    (gamedesign.human_delete). No PIN, like the scores."""
    try:
        done = gamedesign.human_delete(ROOT, data.get("ideas") or [], data.get("nodes") or [])
    except (ValueError, OSError, TypeError) as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": True, **done, "data": sync_page.write_gamedesign(ROOT)}


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


# ---------------------------------------------------------------- courier: the inbox of the on-demand threads
# Every few seconds: messages go to their threads as a turn of their own (start, or stop and start again), never as a
# cross-session message; a delivery counts once its id is in the thread's transcript; in "on-demand" mode a thread
# idle long enough is stopped and each pool has a limit. dispatch.py holds the messages and the settings.
COURIER_SECONDS = 5
COURIER_LOCK = threading.Lock()      # one tick at a time (the loop, and a test or the page asking for one)


def transcript_path(session_id):
    return transcripts_dir() / f"{session_id}.jsonl"


def transcript_size(session_id):
    try:
        return transcript_path(session_id).stat().st_size
    except OSError:
        return 0


def transcript_has(session_id, text, offset=0):
    """True when the transcript holds `text` after byte `offset` (where it was when the message was given)."""
    try:
        with open(transcript_path(session_id), "rb") as f:
            f.seek(max(0, offset))
            return text.encode("utf-8") in f.read()
    except OSError:
        return False


def idle_seconds(session_id, now):
    try:
        return now - transcript_path(session_id).stat().st_mtime
    except OSError:
        return 0


# ---- the watch over silent turns: a turn that runs (status busy) while its transcript gets no new line is stuck in
# one call (an MCP tool that never answers, a request that never ends). Nobody sees it: the page says "busy".
HUNG_SHOW_SECONDS = 180      # from here the page says so
HUNG_SLOW = ("Bash", "PowerShell")      # a shell command may rightly run for ten minutes
HUNG_SLOW_SECONDS = 720
HUNG_NEVER = ("Agent", "Task", "Workflow")      # a subagent writes its own transcript: silence here says nothing
HUNG_RETRY_SECONDS = 3600      # a thread stopped for being stuck is not stopped again within this time
HUNG_SOURCE = "canh phiên"
HUNG_TAIL = 400_000      # bytes of the transcript read to find the call
HUNG = {}      # session id -> {"noted", "stopped"} of the silence it is in now


def pending_call(session_id):
    """(tool, a short text of its input) of the last tool call of the thread that has no result yet, or ("", "")."""
    try:
        with open(transcript_path(session_id), "rb") as f:
            f.seek(0, os.SEEK_END)
            f.seek(max(0, f.tell() - HUNG_TAIL))
            lines = f.read().decode("utf-8", errors="replace").splitlines()
    except OSError:
        return "", ""
    calls = {}
    for line in lines:
        if '"tool_use' not in line and '"tool_result"' not in line:
            continue
        try:
            row = json.loads(line)
        except ValueError:
            continue      # the first line of the tail may be cut
        # only the turns themselves: the CLI also writes rows that echo a request (api-request-blob), where a call
        # comes again after its result
        if not isinstance(row, dict) or row.get("type") not in ("assistant", "user"):
            continue
        content = (row.get("message") or {}).get("content")
        for b in content if isinstance(content, list) else []:
            if not isinstance(b, dict):
                continue
            if b.get("type") == "tool_use" and row["type"] == "assistant":
                calls[b.get("id")] = (str(b.get("name") or ""), b.get("input"))
            elif b.get("type") == "tool_result":
                calls.pop(b.get("tool_use_id"), None)
    if not calls:
        return "", ""
    name, inp = list(calls.values())[-1]
    what = " ".join(f"{k}={v}" for k, v in inp.items() if isinstance(v, (str, int, float)) and k != "unity_instance") if isinstance(inp, dict) else ""
    return name, re.sub(r"\s+", " ", what)[:160]


def hang_of(t, s, now):
    """None, or {"seconds", "tool", "what", "slow"} when the session `s` of thread `t` runs a turn that has been
    silent for HUNG_SHOW_SECONDS. A turn that waits for the human (state blocked) or for a subagent is not stuck."""
    if not s or s.get("status") != "busy" or s.get("state") == "blocked":
        return None
    quiet = idle_seconds(t["id"], now)
    if quiet < HUNG_SHOW_SECONDS:
        return None
    tool, what = pending_call(t["id"])
    if tool in HUNG_NEVER:
        return None
    return {"seconds": int(quiet), "tool": tool, "what": what, "slow": tool in HUNG_SLOW}


def hang_text(h):
    """'im 6 phút ở lệnh mcp__UnityMCP__manage_asset (action=delete path=…)' for the page, the note and the message."""
    call = (f"ở lệnh {h['tool']}" + (f" ({h['what']})" if h["what"] else "")) if h["tool"] else "không ở lệnh nào (chờ mô hình trả lời)"
    return f"im {h['seconds'] // 60} phút {call}"


def hung_note(t, h, action):
    """One note in Management/reports (where @Manager and @CodeLead are shown the new reports) about a stuck turn."""
    folder = ROOT / "Management" / "reports"
    folder.mkdir(parents=True, exist_ok=True)
    slug = re.sub(r"[^A-Za-z0-9]+", "-", t["binding"]).strip("-") or "unknown"
    stamp, n = time.strftime("%Y%m%d-%H%M%S"), 1
    path = folder / f"{stamp}-{slug}-treo.md"
    while path.exists():
        n += 1
        path = folder / f"{stamp}-{slug}-treo-{n}.md"
    body = (f"<!-- {time.strftime('%Y-%m-%d %H:%M:%S')} · FEATURE={t['binding']} · ghi bởi setup_server.py (canh phiên) -->\n"
            f"## Báo cáo cho @Manager\n- Phiên: FEATURE={t['binding']}\n- Việc: (phiên treo: lượt đang chạy {hang_text(h)})\n"
            f"- Trạng thái: bị chặn\n- Đã đổi: ?\n- Công khai đổi: ?\n- Cần bên khác: {action}\n- Kiểm tra: ?\n"
            f"- Ghi chú: do server ghi, không phải phiên đó viết; thread {t['title']} ({t['id'][:8]})\n")
    path.write_text(body, encoding="utf-8", newline="\n")
    return path


def watch_tick(now=None):
    """One pass of the watch: every thread whose running turn is silent gets, once per silence, a note for its Leader
    (after hung_note minutes) and, when it is a background thread that runs on demand (feature, service, @Core,
    tool), is stopped and given its work again through the inbox (after hung_stop minutes; once in
    HUNG_RETRY_SECONDS, after that only the note: the human decides). Never stopped: a chat open in VS Code or a
    terminal, a Leader, a tester (it plays: long turns are its work). Returns what it did."""
    now = time.time() if now is None else now
    did = []
    cfg = dispatch.config(ROOT)
    live = live_sessions()
    for t in threads():
        if t["archived"] or t["unlisted"] or not t["binding"] or t["binding"] == "@human":
            continue
        s = live.get(t["id"])
        h = hang_of(t, s, now)
        if not h:
            HUNG.pop(t["id"], None)
            continue
        mark = HUNG.setdefault(t["id"], {"noted": False, "stopped": False})
        floor = HUNG_SLOW_SECONDS if h["slow"] else 0
        target = t["binding"]
        may_stop = bool(cfg["hung_stop"]) and s["kind"] == "background" and dispatch.pool_of(target) in dispatch.ON_DEMAND
        if may_stop and not mark["stopped"] and h["seconds"] >= max(cfg["hung_stop"] * 60, floor):
            mark["stopped"] = True
            again = any(m.get("source") == HUNG_SOURCE and now - m.get("created", 0) < HUNG_RETRY_SECONDS
                        for m in dispatch.messages(ROOT, target))
            with BUSY_LOCK:
                busy_now = t["id"] in BUSY
            if again or busy_now:
                hung_note(t, h, "người: phiên treo lần nữa sau khi server đã tắt và giao lại một lần; xem thread rồi tắt hoặc giao lại bằng tay")
                did.append(("hung-left", target))
                continue
            try:
                with COURIER_LOCK:
                    stop_bg(t["id"])
                    dispatch.send(ROOT, target, (
                        f"Lượt trước của thread này bị treo: {hang_text(h)}, nên server đã tắt nó lúc {time.strftime('%H:%M', time.localtime(now))}. "
                        "Kiểm lại kết quả của lệnh đó trước (nó có thể đã chạy xong hoặc chạy dở), rồi làm tiếp việc đang dở của lượt "
                        "trước, không làm lại từ đầu. Lệnh đó treo lần nữa thì không gọi lại: ghi BÁO CHẶN trong báo cáo và dừng."),
                        HUNG_SOURCE, now=now)
                hung_note(t, h, "không: server đã tắt phiên và giao lại việc qua hộp thư (một lần)")
                did.append(("hung-restarted", target))
            except Exception as ex:
                hung_note(t, h, f"người: server không tắt được phiên ({str(ex)[:120]})")
                did.append(("hung-stop-failed", target, str(ex)[:120]))
            mark["noted"] = True
        elif not mark["noted"] and h["seconds"] >= max(cfg["hung_note"] * 60, floor):
            mark["noted"] = True
            hung_note(t, h, f"không: server tự tắt và giao lại sau {cfg['hung_stop']} phút im" if may_stop
                      else "người hoặc Leader của phần đó (tầng dưới: @CodeLead; Leader: @Manager): xem thread; server không tự tắt phiên này")
            did.append(("hung-noted", target))
    return did


def tester_lock():
    """The Editor lock while a tester holds it (guard.read_lock: missing, broken or expired is no lock), else None.
    While it plays, code work is not handed out (the guard refuses writes into Assets anyway)."""
    lock = guard.read_lock(guard.STORE_DIR)
    return lock if lock and lock.get("who") in guard.TESTER_ROLES else None


def wake_blocked(now=None):
    """The chats the guard refused a write to while a tester played (its marks in the store) get one message to go on,
    once the tester is done. Only the chats that run on demand have an inbox; a Leader tries again itself. Returns
    what it did."""
    did = []
    folder = Path(guard.STORE_DIR) / guard.TESTER_BLOCKED_DIR
    if not folder.is_dir() or tester_lock():
        return did
    now = time.time() if now is None else now
    seen = set()
    for p in sorted(folder.glob("*.json")):
        try:
            target = str(json.loads(p.read_text(encoding="utf-8")).get("target") or "")
        except (OSError, ValueError):
            target = ""
        try:
            p.unlink()
        except OSError:
            continue      # not removed: tried again at the next pass, the message is not sent twice (same key)
        if target in seen or dispatch.pool_of(target) not in dispatch.ON_DEMAND or not can_create(target):
            continue
        seen.add(target)
        try:
            dispatch.send(ROOT, target, "Tester đã chơi thử xong, Unity nhận code lại được. Lượt trước của thread này bị guard chặn "
                          "khi ghi vào UnityProject/Assets: kiểm lại file nào đã ghi, file nào chưa, rồi làm tiếp phần còn lại "
                          "của việc đó, không làm lại từ đầu.", HUNG_SOURCE, key=f"tester-done:{target}", now=now)
            did.append(("woken", target))
        except (ValueError, OSError) as ex:
            did.append(("wake-failed", target, str(ex)[:120]))
    return did


def thread_for(target, listed):
    """The thread bound to `target` that the courier gives messages to (the most recently used one), or None."""
    same = [t for t in listed if t["binding"] == target and not t["archived"] and not t["unlisted"]]
    return max(same, key=lambda t: t["modified"]) if same else None


def can_stop(t, live, open_targets, cfg, now):
    """A running background thread of a pool may be stopped: its turn is over (status idle), its transcript has
    been quiet for idle_minutes, no message waits for it and the page is not running a turn in it. A chat open in VS
    Code or a terminal is never stopped."""
    s = live.get(t["id"])
    with BUSY_LOCK:
        busy_now = t["id"] in BUSY
    return bool(s and s["kind"] == "background" and s["status"] == "idle" and not busy_now
                and dispatch.pool_of(t["binding"]) in dispatch.ON_DEMAND and t["binding"] not in open_targets
                and idle_seconds(t["id"], now) >= cfg["idle_minutes"] * 60)


SLOT_IDLE_SECONDS = 120      # a thread idle this long may be stopped to make room for a waiting message


def can_free(t, live, open_targets, now):
    """A running background thread may be stopped to make room in a full pool (either mode): its turn is over, it
    has been quiet for SLOT_IDLE_SECONDS, no message waits for it and the page is not running a turn in it. A chat
    open in VS Code or a terminal is never stopped."""
    s = live.get(t["id"])
    with BUSY_LOCK:
        busy_now = t["id"] in BUSY
    return bool(s and s["kind"] == "background" and s["status"] == "idle" and not busy_now
                and dispatch.pool_of(t["binding"]) in dispatch.ON_DEMAND and t["binding"] not in open_targets
                and idle_seconds(t["id"], now) >= SLOT_IDLE_SECONDS)


def can_create(target):
    """True when the courier may make a thread for `target`: a role that runs on demand, or a feature or service
    whose folder exists (the guard takes no other FEATURE value)."""
    if dispatch.pool_of(target) is None:
        return False
    return target in ROLE_GROUPS or guard.valid_feature(target, str(ROOT))


def still_valid(check):
    """(True, '') when the event an automatic message tells of still holds, else (False, why). The checks gd_req.py
    and studio.py attach: req-rejected <F> <ref> | order-open <tool> <ref> | item-redo <tool> <ref> <item> |
    order-delivered <tool> <ref> <version>. Anything the courier cannot read is left to be delivered."""
    parts = (check or "").split()
    try:
        if parts[:1] == ["req-rejected"] and len(parts) == 3:
            v = reqbook.view(ROOT, parts[1])
            node, req = reqbook.find_req(v, parts[2])
            return (req["state"] == "rejected", f"yêu cầu {parts[2]} không còn bị trả ({reqbook.STATE_NAMES.get(req['state'], req['state'])})")
        if parts[:1] in (["order-open"], ["item-redo"], ["order-delivered"]):
            row = next((r for r in studio.summary(ROOT, parts[1]) if r[0] == parts[2]), None)
            if row is None:
                return False, f"không còn order {parts[1]} {parts[2]}"
            if parts[0] == "order-open":
                return row[2] in ("new", "doing"), f"order {parts[2]} đã giao"
            if parts[0] == "item-redo" and len(parts) == 4:
                item = next((i for i in row[7] if i["id"] == parts[3]), None)
                return bool(item and item["redo"]), f"item {parts[3]} không còn chờ làm lại"
            if parts[0] == "order-delivered" and len(parts) == 4:
                return str(row[3]) == parts[3], f"order {parts[2]} đã có bản giao mới hơn v{parts[3]}"
    except Exception:      # a book or an order that cannot be read: deliver, the thread will see
        return True, ""
    return True, ""


def courier_tick(now=None):
    """One pass over the inbox; returns what it did (for the tests and the log)."""
    with COURIER_LOCK:
        return _courier_tick(time.time() if now is None else now)


def _courier_tick(now):
    did = []
    cfg = dispatch.config(ROOT)
    open_ = dispatch.pending(ROOT)
    if not open_ and cfg["mode"] != "on-demand":
        return did
    live = live_sessions(fresh=True)
    listed = threads()
    by_id = {t["id"]: t for t in listed}

    # 1. deliveries that should be in the transcript by now
    for m in [m for m in open_ if m["state"] == "delivering"]:
        sid = m.get("thread") or ""
        if transcript_has(sid, m["id"], m.get("offset", 0)):
            dispatch.mark(ROOT, m, "delivered", delivered_at=now)
            did.append(("delivered", m["id"]))
        elif now - m.get("started", 0) > dispatch.VERIFY_SECONDS:
            dispatch.failed(ROOT, m, "không thấy tin trong transcript của thread sau khi giao", now)
            did.append(("not-seen", m["id"]))
    open_ = dispatch.pending(ROOT)
    open_targets = {m["target"] for m in open_}

    made = []      # the targets a thread was made for in this pass (one a pass: making one costs a whole turn)
    playing = tester_lock()

    def running(pool):
        return sum(1 for sid, s in live.items() if sid in by_id and dispatch.pool_of(by_id[sid]["binding"]) == pool)

    def stop(t, why):
        try:
            stop_bg(t["id"])
            live.pop(t["id"], None)
            did.append((why, t["binding"]))
            return True
        except Exception as ex:
            did.append(("stop-failed", t["binding"], str(ex)[:120]))
            return False

    # 2. stop the threads idle long enough (on-demand only)
    if cfg["mode"] == "on-demand":
        for t in listed:
            if can_stop(t, live, open_targets, cfg, now):
                stop(t, "stopped-idle")

    # 3. give each target its waiting messages, oldest target first
    groups = {}
    for m in open_:
        if m["state"] in ("queued", "waiting") and m.get("next_try", 0) <= now:
            groups.setdefault(m["target"], []).append(m)
    for target, ms in sorted(groups.items(), key=lambda kv: min(m["created"] for m in kv[1])):
        if any(m["state"] == "delivering" for m in open_ if m["target"] == target):
            continue      # the last delivery is not confirmed yet
        t = thread_for(target, listed)

        forced = any(m.get("force") for m in ms)      # the Giao ngay button

        def wait(why, tell=True):
            for m in ms:
                extra = {}
                if tell and not m.get("noted") and now - m["created"] >= WAIT_NOTE_SECONDS:      # nobody would know otherwise
                    extra["noted"] = True
                    wait_note(m, why, now)
                    did.append(("wait-noted", m["id"]))
                if m["state"] != "waiting" or m.get("why") != why or extra or m.get("force"):
                    dispatch.mark(ROOT, m, "waiting", why=why, force=False, **extra)
            did.append(("wait", target, why))

        stale = []
        for m in ms:
            ok, why = still_valid(m.get("check"))
            if not ok:
                dispatch.mark(ROOT, m, "cancelled", why="hết hợp lệ: " + why)
                stale.append(m)
                did.append(("stale", m["id"]))
        ms = [m for m in ms if m not in stale]
        if not ms:
            continue
        pool = dispatch.pool_of(target)
        s = None
        if pool in ("feature", "tester"):      # tiers 4-7 are workers now: no thread is made or woken for them
            did += route_to_worker(target, ms, now)
            continue
        if playing and pool == "feature" and not forced:      # features, services, @Core: their work is code
            wait(f"{playing.get('who')} đang chơi thử tới khoảng {time.strftime('%H:%M', time.localtime(playing['until']))}: việc code "
                 "được giữ lại để Unity không nạp lại giữa lúc Play, tự giao khi tester xong (hoặc bấm Giao ngay)", tell=False)
            continue
        if t is None:
            if not can_create(target):
                wait("không có feature hay vai này để tạo thread (thư mục feature chưa có?)")
                continue
            if made:
                wait("chờ tới lượt tạo thread (mỗi vòng tạo một)")
                continue
        else:
            with BUSY_LOCK:
                busy_now = t["id"] in BUSY
            if busy_now:
                wait("trang đang chạy một lượt trong thread")
                continue
            s = live.get(t["id"])
            if s and s["kind"] != "background":
                # A chat left open would hold its messages for ever. One that is idle long enough (or at once, by the
                # Giao ngay button) is closed, and the thread runs in the background with the messages.
                quiet = idle_seconds(t["id"], now)
                takeover = cfg["takeover_minutes"] * 60
                if s["status"] != "idle":
                    wait("chat đang mở ở VS Code hoặc terminal và đang chạy một lượt: tin đi khi lượt xong")
                    continue
                if not forced and (not takeover or pool not in dispatch.ON_DEMAND):
                    wait("chat đang mở ở VS Code hoặc terminal: đóng chat, hoặc bấm Giao ngay để server đóng nó và giao")
                    continue
                if not forced and quiet < takeover:
                    wait(f"chat đang mở ở VS Code hoặc terminal, rảnh {int(quiet // 60)} phút: server tự đóng chat và giao khi rảnh đủ "
                         f"{cfg['takeover_minutes']} phút (hoặc bấm Giao ngay)")
                    continue
                try:
                    close_chat(t["id"])
                except Exception as ex:
                    wait(f"không đóng được chat đang mở: {str(ex)[:120]}")
                    continue
                live.pop(t["id"], None)
                s = None
                did.append(("took-over", target))
            # A background thread stopped at a question to the human (state blocked) is doing nothing and will not go
            # on by itself: the waiting message takes it over (turn() stops it and starts it again with the message).
            if s and s["status"] != "idle" and s.get("state") != "blocked":
                wait("thread đang chạy một lượt")
                continue
        # starting a thread: the limit holds in both modes (a tester has none: a Leader's message always opens it).
        # A full pool: the background thread of the pool that has been idle longest makes room, in both modes.
        if not s and not forced and pool in cfg["limits"] and running(pool) >= cfg["limits"][pool]:
            free = sorted((x for x in listed if dispatch.pool_of(x["binding"]) == pool and can_free(x, live, open_targets, now)),
                          key=lambda x: idle_seconds(x["id"], now), reverse=True)
            if not (free and stop(free[0], "stopped-for-slot")):
                wait(f"hết chỗ: {cfg['limits'][pool]} thread {pool} đang chạy, chưa thread nào rảnh đủ {SLOT_IDLE_SECONDS // 60} phút")
                continue
        if t is None:      # no thread for this target yet: make one (one turn that reads the rules), then give it the message
            made.append(target)
            same = sorted((x for x in listed if dispatch.pool_of(x["binding"]) == pool and x.get("model")),
                          key=lambda x: x["modified"], reverse=True)
            model = same[0]["model"] if same else DEFAULT_MODEL
            effort = same[0]["effort"] if same and same[0].get("effort") in EFFORTS else DEFAULT_EFFORT
            wait("đang tạo thread")
            try:
                sid = create(thread_name(target), target, model, effort)
            except Exception as ex:
                for m in ms:
                    dispatch.failed(ROOT, dispatch.get(ROOT, m["id"]), f"không tạo được thread: {ex}", now)
                did.append(("create-failed", target, str(ex)[:120]))
                continue
            t = {"id": sid, "title": thread_name(target), "binding": target, "feature": target, "model": model, "effort": effort,
                 "archived": False, "unlisted": False, "modified": now}
            listed.append(t)
            by_id[sid] = t
            did.append(("created", target, sid[:8]))
            ms = [dispatch.get(ROOT, m["id"]) for m in ms]
        t = dict(t, live=(s or {}).get("kind", ""))
        # Renew marked on the page (the thread was in a turn): cleared now, before the job (wipe_transcript, no model
        # turn); the job turn itself is told by the guard line to read the role rules.
        # Not when it stopped at a question to the human: the message may be the answer. A failed wipe keeps the mark.
        if renew_pending(t["id"]) and (s or {}).get("state") != "blocked":
            try:
                wipe_transcript(t)
                t["live"] = ""
                live.pop(t["id"], None)
                clear_renew_mark(t["id"])
                did.append(("renewed", target))
            except Exception as ex:      # the job still goes, with the old context
                did.append(("renew-failed", target, str(ex)[:120]))
        offset = transcript_size(t["id"])
        for m in ms:
            dispatch.mark(ROOT, m, "delivering", why="", thread=t["id"], offset=offset, started=now, tries=m["tries"] + 1, force=False)
        try:
            turn(t, dispatch.prompt(ms, now), bg=True)
            live[t["id"]] = {"kind": "background", "status": "busy", "state": "", "short": t["id"][:8]}
            did.append(("given", target, [m["id"] for m in ms]))
        except Exception as ex:
            for m in ms:
                dispatch.failed(ROOT, dispatch.get(ROOT, m["id"]), f"không bật được thread: {ex}", now)
            did.append(("give-failed", target, str(ex)[:120]))
    return did


WAIT_NOTE_SECONDS = 300      # a message that waits this long is told to @CodeLead (its part), once


def wait_note(m, why, now):
    """One note in Management/reports about a message nobody could be given for WAIT_NOTE_SECONDS."""
    folder = ROOT / "Management" / "reports"
    folder.mkdir(parents=True, exist_ok=True)
    slug = re.sub(r"[^A-Za-z0-9]+", "-", m["target"]).strip("-") or "unknown"
    path = folder / f"{time.strftime('%Y%m%d-%H%M%S')}-{slug}-tin-cho-{m['id'][-6:]}.md"
    text = m["text"].replace("\n", " ")
    path.write_text(
        f"<!-- {time.strftime('%Y-%m-%d %H:%M:%S')} · FEATURE={m['target']} · ghi bởi setup_server.py (hộp thư) -->\n"
        f"## Báo cáo cho @Manager\n- Phiên: FEATURE={m['target']}\n"
        f"- Việc: (tin {m['id']} từ {m['source']} chưa giao được sau {int((now - m['created']) // 60)} phút: {text[:160]})\n"
        f"- Trạng thái: bị chặn\n- Đã đổi: không\n- Công khai đổi: không\n- Cần bên khác: người: {why}\n- Kiểm tra: ?\n"
        "- Ghi chú: do server ghi; tab Hộp thư có nút Giao ngay trên tin đang chờ\n", encoding="utf-8", newline="\n")
    return path


def deliver_now(mid):
    """The Giao ngay button of a waiting message: one pass of the courier that gives it whatever it waits for, or the
    reason it still cannot go (its thread is in the middle of a turn). {ok, state, why}."""
    dispatch.force(ROOT, mid)
    courier_tick()
    m = dispatch.get(ROOT, mid)
    if m["state"] in ("delivering", "delivered"):
        return {"ok": True, "state": m["state"], "why": ""}
    return {"ok": False, "error": m.get("why") or m.get("error") or f"tin đang {m['state']}"}


INBOX_DONE_SHOWN = 30      # delivered or cancelled messages the tab still shows


def inbox_view():
    """The Hộp thư tab: settings, messages (every open one and error, the last delivered or cancelled ones), each
    pool's limit and how many of its threads run now (open chats count), and the targets the send box offers."""
    cfg = dispatch.config(ROOT)
    ms = dispatch.messages(ROOT)
    keep = [m for m in ms if m["state"] in dispatch.OPEN + ("error",)]
    done = [m for m in ms if m["state"] not in dispatch.OPEN + ("error",)][-INBOX_DONE_SHOWN:]
    live = live_sessions(fresh=True)
    listed = [t for t in threads() if not t["archived"] and not t["unlisted"]]
    pools = {p: {"limit": n, "running": 0, "open_chats": 0, "threads": []} for p, n in cfg["limits"].items()}
    now = time.time()
    waiting = {}
    for m in keep:
        if m["state"] in dispatch.OPEN:
            waiting[m["target"]] = waiting.get(m["target"], 0) + 1
    targets = {}
    for t in listed:
        pool = dispatch.pool_of(t["binding"])
        if not pool:
            continue
        targets[t["binding"]] = {"target": t["binding"], "pool": pool, "title": t["title"]}
        s = live.get(t["id"])
        if s and pool in pools:
            pools[pool]["running"] += 1
            pools[pool]["open_chats"] += s["kind"] != "background"
            # who takes the place: the thread, where it runs, what it is doing, how long it has been quiet
            pools[pool]["threads"].append({"id": t["id"], "title": t["title"], "target": t["binding"], "kind": s["kind"],
                                           "status": s["status"], "state": s.get("state", ""),
                                           "idle": int(idle_seconds(t["id"], now)), "inbox": waiting.get(t["binding"], 0),
                                           "hung": hang_text(h) if (h := hang_of(t, s, now)) else ""})
    return {"config": cfg, "modes": list(dispatch.MODES), "messages": list(reversed(keep + done)),
            "summary": dispatch.summary(ROOT), "pools": pools,
            "targets": sorted(targets.values(), key=lambda x: (x["pool"], x["target"].lower()))}


def inbox_call(action, data):
    """POST /api/inbox/<action>: send (from the human), cancel, retry, remove, clear, config. {ok, data} or
    {ok: False, error}."""
    try:
        if action == "send":
            m = dispatch.send(ROOT, str(data.get("target") or ""), str(data.get("text") or ""), "người")
            return {"ok": True, "id": m["id"], "data": inbox_view()}
        if action == "cancel":
            dispatch.cancel(ROOT, str(data.get("id") or ""))
        elif action == "retry":
            dispatch.retry(ROOT, str(data.get("id") or ""))
        elif action == "deliver":     # Giao ngay
            res = deliver_now(str(data.get("id") or ""))
            return dict(res, data=inbox_view())
        elif action == "remove":      # one message, for good
            dispatch.remove(ROOT, str(data.get("id") or ""))
        elif action == "clear":       # the whole inbox, or only what is over
            with COURIER_LOCK:        # not while a pass is giving messages
                n = dispatch.clear(ROOT, str(data.get("scope") or "done"))
            return {"ok": True, "removed": n, "data": inbox_view()}
        elif action == "config":
            limits = data.get("limits") if isinstance(data.get("limits"), dict) else None
            dispatch.set_config(ROOT, mode=data.get("mode"), limits=limits, idle_minutes=data.get("idle_minutes"),
                                hung_note=data.get("hung_note"), hung_stop=data.get("hung_stop"),
                                takeover_minutes=data.get("takeover_minutes"))
        else:
            return {"ok": False, "error": "không có thao tác này"}
        return {"ok": True, "data": inbox_view()}
    except ValueError as ex:
        return {"ok": False, "error": str(ex)}


def inbox_reqs(feature):
    """The open requirements of a feature's book (for the send box): [{ref, title, state}]."""
    if dispatch.pool_of(feature) != "feature" or feature == "@Core":
        return {"ok": True, "reqs": []}
    try:
        v = reqbook.view(ROOT, feature)
    except Exception as ex:
        return {"ok": False, "error": str(ex)[:200]}
    out = [{"ref": r["ref"], "title": r["title"], "state": r["state"], "name": reqbook.STATE_NAMES.get(r["state"], r["state"])}
           for n in v["nodes"] for r in n["reqs"] if r["state"] not in reqbook.MET]
    return {"ok": True, "reqs": out, "allowed": v["allowed"], "reason": v["reason"]}


# ---------------------------------------------------------------- Editor tickets (người quyết 2026-11-27)
# The server hands the shared Unity Editor to one ticket at a time (editor_ticket.py). A ticket of CodeLead's work
# (assets, scene) or fix: CodeLead gets one message and works; its Stop hook gives the Editor back. A play job's
# ticket starts its turn. Every waiting code-only ticket (a worker's job, its code already applied by worker.py): one
# short headless run on a cheap model compiles and reads the console; compile errors go back to the jobs (a fix round),
# or make a CodeLead fix ticket that goes first when a load had other tickets. When nothing else
# needs the Editor, one EditMode run serves every ticket waiting for its tests; the requirements whose tests pass are
# marked done in their book, the chats with failures get one message.
EDITOR_SOURCE = "phiếu Editor"
RUNNER_ROLE = "@UnityAutoPlay"      # its role may compile, read the console and run tests; it writes nothing
RUNNER_MODEL = "haiku"
GRANT_UNUSED_SECONDS = 20 * 60      # a granted chat that did not touch the Editor by then gives its turn back
RUNNER = {"busy": False, "last": None, "backoff_until": 0}
RUNNER_BACKOFF_SECONDS = 120      # after the runner failed (Unity unreachable), no load or test run for this long
RUNNER_LOCK = threading.Lock()
JSON_TAIL_RX = re.compile(r"\{[^{}]*(\{[^{}]*\}[^{}]*)*\}\s*$", re.S)

RUNNER_LOAD = ("Bạn là phiên biên dịch của server (vai @UnityAutoPlay, chỉ đọc, không sửa gì, không nhắn ai). Không đọc "
               "luật hay skill. Nạp tool bằng ToolSearch select:mcp__UnityMCP__refresh_unity,mcp__UnityMCP__read_console. "
               "Gọi refresh_unity (scope all, mode force, compile request, wait_for_ready true), rồi read_console lấy lỗi "
               "biên dịch (types error). In DÒNG CUỐI là đúng một JSON: {{\"clean\": true|false, \"errors\": "
               "[\"<file>:<dòng>: <mã>: <câu>\", ...]}} (tối đa 30 lỗi). Unity không kết nối được thì "
               "{{\"clean\": false, \"errors\": [\"UNITY: <câu lỗi>\"]}}. Code vừa nạp của: {files}.")
RUNNER_TEST = ("Bạn là phiên chạy test của server (vai @UnityAutoPlay, chỉ đọc, không sửa gì, không nhắn ai). Không đọc "
               "luật hay skill. Nạp tool bằng ToolSearch select:mcp__UnityMCP__run_tests,mcp__UnityMCP__get_test_job. "
               "Chạy run_tests mode EditMode với assembly_names {assemblies}, chờ get_test_job tới khi xong. In DÒNG CUỐI "
               "là đúng một JSON: {{\"ok\": true|false, \"total_passed\": <số test qua>, \"failed\": [{{\"name\": \"<tên đầy đủ>\", "
               "\"message\": \"<câu lỗi ngắn>\"}}], \"passed\": [\"<tên đầy đủ>\", ...]}}. Trong passed chỉ ghi các test qua có "
               "tên (Lớp.Hàm) nằm trong danh sách này, ghi đủ từng cái: {wanted}. Unity không kết nối được thì "
               "{{\"ok\": false, \"failed\": [{{\"name\": \"UNITY\", \"message\": \"<câu lỗi>\"}}], \"passed\": []}}.")


def ticket_feature(session):
    """The draft / book name of a ticket's session (Core for @Core); None for a Leader."""
    if session == "@Core":
        return "Core"
    return None if session.startswith("@") else session


def runner_turn(prompt):
    """One headless turn of the cheap runner, bound to RUNNER_ROLE, not saved as a thread. Returns its last JSON line."""
    sid = str(uuid.uuid4())
    bind(sid, RUNNER_ROLE)
    try:
        out = run_claude(["-p", "--model", RUNNER_MODEL, "--session-id", sid, "--no-session-persistence"]
                         + profile_args(RUNNER_ROLE) + ["--", prompt])      # --mcp-config takes several values: -- ends them
    finally:
        try:
            os.remove(guard.binding_path(sid, guard.STORE_DIR))
        except OSError:
            pass
    for line in reversed(out.splitlines()):
        line = line.strip()
        if line.startswith("{") and line.endswith("}"):
            try:
                return json.loads(line)
            except ValueError:
                continue
    raise RuntimeError("phiên chạy không in JSON: " + out[-300:])


WORKER_ROLE = "@Worker"


def job_event(job, key, text):
    """One line in a worker job's events.log (Management/workers/<job>/); nobody is woken."""
    if not re.fullmatch(r"[A-Za-z0-9-]+", str(job or "")):
        return
    folder = ROOT / "Management" / "workers" / job
    folder.mkdir(parents=True, exist_ok=True)
    with open(folder / "events.log", "a", encoding="utf-8") as f:
        f.write(json.dumps({"at": time.time(), "key": key, "text": text}, ensure_ascii=False) + "\n")


def job_events(job, n=4):
    """The last n lines of a job's events.log, oldest first: [{at, key, text}]."""
    try:
        lines = (ROOT / "Management" / "workers" / job / "events.log").read_text(encoding="utf-8").splitlines()[-n:]
    except OSError:
        return []
    out = []
    for line in lines:
        try:
            o = json.loads(line)
        except ValueError:
            continue
        if isinstance(o, dict):
            out.append({"at": o.get("at") or 0, "key": str(o.get("key") or ""), "text": str(o.get("text") or "")[:300]})
    return out


def tell(ticket, text, key):
    """One message to a ticket's chat: the inbox of a chat on demand; CodeLead through its open thread; a worker's
    job: its events.log (Management/workers/<job>/), nobody is woken; else nothing."""
    target = ticket["session"]
    if target == WORKER_ROLE:
        job = str(ticket.get("job") or "")
        if not re.fullmatch(r"[A-Za-z0-9-]+", job):
            return ("no-job", target)
        job_event(job, key, text)
        if key.startswith("editor-test:"):      # tests broke after the load: the job fixes them, once
            fix_round(job, "test_fixes", "Lô test của server sau khi nạp có test hỏng. Sửa đúng các test sau (code hay test, "
                      "theo đúng ý của yêu cầu), kiểm lại rồi ghi lại result.json:\n" + text)
        return ("logged", f"{target} {job}")
    try:
        if dispatch.pool_of(target):
            dispatch.send(ROOT, target, text, EDITOR_SOURCE, key=key)
            return ("told", target)
        if target == "@CodeLead":
            lead = next((t for t in threads() if t.get("binding") == "@CodeLead" and t.get("live")), None)
            if lead:
                relay(lead, text)
                return ("told", target)
            return ("not-live", target)
    except Exception as ex:
        return ("tell-failed", target, str(ex)[:120])
    return ("no-inbox", target)


def grant_text(t):
    notes = "; ".join(t.get("notes") or [])
    return (f"Tới lượt bạn dùng Unity Editor (phiếu {t['id']}): {editor_ticket.describe(t)}." + (f" Ghi chú: {notes}." if notes else "")
            + " Làm hết rồi tắt bit bằng editor_ticket.py done --session " + t["session"] + " <việc>; kết thúc lượt là trả Editor. "
            "Không giữ Editor sang việc khác.")


def load_run(group):
    """The load of every waiting code-only ticket: drafts into Assets, one compile, the verdict to everyone."""
    did, loaded, files = [], [], []
    for t in group:      # the code is in Assets already (worker.py applied it): one compile for all
        files += ticket_features(t)
        loaded.append(t)
    if not loaded:
        return did
    try:
        verdict = runner_turn(RUNNER_LOAD.format(files=", ".join(files) or "(không có, chỉ biên dịch)"))
        unity = [str(e) for e in verdict.get("errors") or [] if str(e).startswith("UNITY")]
        if unity and not verdict.get("clean"):      # Unity unreachable or the call refused: nothing was compiled
            raise RuntimeError("; ".join(unity)[:200])
    except Exception as ex:
        # The runner itself failed (CLI, MCP, Unity not connected): nothing is known about the code. The tickets go
        # back to the queue with every need, no fix ticket for @CodeLead; the server log says why.
        editor_ticket.release(ROOT, met=[], broken=False)
        RUNNER["backoff_until"] = time.time() + RUNNER_BACKOFF_SECONDS      # Unity is likely down: do not try every pass
        why = f"phiên biên dịch của server lỗi, phiếu xếp lại, thử lại sau {RUNNER_BACKOFF_SECONDS // 60} phút: {str(ex)[:200]}"
        return did + [tell(t, why, f"editor-runner:{t['id']}") for t in loaded] + [("load", "runner-failed", why)]
    clean = bool(verdict.get("clean"))
    only_workers = all(t["session"] == WORKER_ROLE and t.get("job") for t in loaded)
    result = editor_ticket.release(ROOT, met=["refresh", "clean"] if clean else ["refresh"],
                                   broken=False if only_workers and not clean else None)
    errors = [str(e) for e in verdict.get("errors") or []][:30]
    if only_workers and not clean:      # the workers of this load fix what Unity found, once; CodeLead is not woken
        for t in loaded:
            fix_round(t["job"], "load_fixes", "Unity báo lỗi biên dịch sau khi nạp (compile_check ngoài Unity đã qua). Sửa "
                      "đúng các lỗi sau, kiểm lại rồi ghi lại result.json:\n" + "\n".join(errors))
        return did + [("load", ", ".join(t["job"] for t in loaded), f"{len(errors)} lỗi, worker sửa")]
    for t in loaded:
        text = ("Code của bạn đã ở Unity và project biên dịch sạch." if clean else
                "Code đã nạp nhưng project còn lỗi biên dịch; CodeLead sửa trước. Lỗi: " + "; ".join(errors[:8]))
        did.append(tell(t, text, f"editor-load:{t['id']}"))
    did += [tell(w, "Phiếu của bạn xong: code mới đã nạp, project sạch.", f"editor-met:{w['id']}") for w in result["satisfied"]]
    if result["fix"]:
        fix = result["fix"]
        fix["notes"] = (fix.get("notes") or []) + ["lỗi: " + "; ".join(errors[:12])]
        editor_ticket.save(ROOT, fix)
    did.append(("load", ", ".join(t["session"] for t in loaded), "sạch" if clean else f"{len(errors)} lỗi"))
    return did


def ticket_features(t):
    """Every feature a ticket stands for: its session's, and (a worker's job) the one of each requirement."""
    names = [ticket_feature(t["session"])] + [r.get("feature") for r in t.get("reqs") or []]
    return list(dict.fromkeys(n for n in names if n))


def test_assemblies(group):
    """The EditMode test assemblies of the features in the run, and of the project assemblies depending on them."""
    import compile_check
    ch = compile_check.Checker(ROOT)
    names = set()
    for t in group:
        for name in ticket_features(t):
            try:
                names |= ch.dependents(ch.in_folder(ch.folder_of(name)))
            except ValueError:
                continue
    return sorted(n for n in names if n.endswith(".Tests"))


def test_run(group):
    did = []
    assemblies = test_assemblies(group)
    try:
        wanted = sorted({".".join(str(x).split(".")[-2:]) for t in group for r in t.get("reqs") or [] for x in r.get("tests") or []})
        verdict = runner_turn(RUNNER_TEST.format(assemblies=json.dumps(assemblies), wanted=json.dumps(wanted, ensure_ascii=False)))
    except Exception as ex:
        verdict = {"ok": False, "failed": [{"name": "RUNNER", "message": str(ex)[:200]}], "passed": []}
    try:      # the runner's last answer, kept for when a requirement is not written done as expected
        (ROOT / "Management" / "editor-tickets" / "last-test.json").write_text(
            json.dumps({"assemblies": assemblies, "verdict": verdict}, ensure_ascii=False, indent=1), encoding="utf-8")
    except OSError:
        pass
    passed = [str(x) for x in verdict.get("passed") or []]
    failed = [x for x in verdict.get("failed") or [] if isinstance(x, dict)]
    broken = any(f.get("name") in ("UNITY", "RUNNER") for f in failed)
    short = lambda full: ".".join(str(full).split(".")[-2:])      # noqa: E731  Lớp.Hàm, as the book keeps it
    ok_names = {short(x) for x in passed}
    for t in group:
        mine = ticket_features(t)
        mine_failed = [f for f in failed if f.get("name") and any(
            f"Game.{n.replace('_Services/', 'Services.')}." in str(f["name"]) for n in mine)]
        for r in t.get("reqs") or []:
            name = r.get("feature") or ticket_feature(t["session"])
            tests = [short(x) for x in r.get("tests") or []]
            if name and tests and not broken and all(x in ok_names for x in tests):
                try:
                    reqbook.mark(ROOT, name, r["ref"], "done", name, tests=tests)
                    did.append(("done", name, r["ref"]))
                except ValueError as ex:
                    did.append(("done-refused", name, r["ref"], str(ex)[:120]))
        if t.get("session") == WORKER_ROLE and t.get("job"):      # the job's page line says how its tests went, passed too
            ok_n = sum(1 for r in t.get("reqs") or [] if any(d[:3] == ("done", r.get("feature") or ticket_feature(t["session"]), r["ref"]) for d in did))
            line = (("Unity không chạy được test" if broken else f"Test: {len(passed)} qua, {len(mine_failed)} hỏng") +
                    f" · {ok_n}/{len(t.get('reqs') or [])} yêu cầu ghi done (chờ CodeLead kiểm)")
            job_event(t["job"], f"editor-test-ok:{t['id']}" if not mine_failed and not broken else f"editor-test-result:{t['id']}", line)
            if not broken:      # a broken runner puts the ticket back: the tests run again, the report comes then
                did.append(after_tests(t["job"], line, ok_n, mine_failed))
        if mine_failed:
            did.append(tell(t, "Test hỏng ở lô test của server: " + "; ".join(
                f"{f['name']}: {str(f.get('message') or '')[:160]}" for f in mine_failed[:10]) + ". Sửa rồi ghi lại result.json.",
                f"editor-test:{t['id']}"))
    editor_ticket.release(ROOT, met=[] if broken else ["test"])
    did.append(("test", len(assemblies), f"{verdict.get('total_passed', len(passed))} qua, {len(failed)} hỏng"))
    return did


def after_tests(job_id, line, ok_n, failed):
    """The end of a worker job's chain (the server's tests ran): a report for CodeLead (Management/reports) and one
    message to its live thread, so the requirements written done get checked. Tests that broke send the job back to
    its worker once (tell, fix_round): the report says so and CodeLead is not woken for it."""
    try:
        job = worker.load(ROOT, job_id)
    except ValueError:
        return ("no-job", job_id)
    if failed:
        if job.get("test_fixes", 0) >= 1:      # its fix round was used: fix_round reports and wakes CodeLead
            return ("left-to-fix-round", job_id)
        worker.write_report(ROOT, job, "dở", [line, "worker tự sửa test hỏng một lần, rồi server test lại"])
        return ("reported", job_id, "dở")
    worker.write_report(ROOT, job, "xong", [line])
    if not ok_n:
        return ("reported", job_id, "xong")
    return ("reported", job_id, "xong", notify_codelead(
        f"LÔ TEST XONG · {job['feature']} · việc {job_id} · {line}. Kiểm theo skill codelead-verify-batch.",
        f"tests-done:{job_id}"))


NOTIFIED = set()      # keys already sent to CodeLead (one message per job and kind)


def notify_codelead(text, key):
    """One message to @CodeLead's live thread (relay: SendMessage through a cheap helper turn). Not live: nothing is
    sent, the report waits at the head of its next message."""
    if key in NOTIFIED:
        return ("already", key)
    lead = next((t for t in threads() if t.get("binding") == "@CodeLead" and t.get("live")), None)
    if not lead:
        return ("codelead-not-live", key)
    try:
        relay(lead, text)
    except Exception as ex:
        return ("notify-failed", key, str(ex)[:120])
    NOTIFIED.add(key)
    return ("notified", key)


def run_in_background(fn, group):
    def go():
        try:
            for step in fn(group):
                print("phiếu Editor:", *step, flush=True)
        except Exception as ex:
            print(f"phiếu Editor: lỗi {ex}", file=sys.stderr, flush=True)
            try:
                editor_ticket.release(ROOT)
            except Exception:
                pass
        finally:
            with RUNNER_LOCK:
                RUNNER["busy"] = False
    with RUNNER_LOCK:
        RUNNER["busy"] = True
    threading.Thread(target=go, name="editor-runner", daemon=True).start()


def editor_tick(now=None):
    """One pass: give back a turn granted and never used, then grant the Editor when it is free."""
    now = time.time() if now is None else now
    did = []
    with RUNNER_LOCK:
        if RUNNER["busy"]:
            return did
    for t in editor_ticket.holder(ROOT):
        if t.get("grant") in ("own", "fix") and not t.get("used") and now - t.get("granted_at", now) > GRANT_UNUSED_SECONDS:
            editor_ticket.release(ROOT, t["session"], now=now)
            did.append(("unused", t["session"]))
    if editor_ticket.holder(ROOT) or guard.human_editor(guard.STORE_DIR) or guard.read_lock(guard.STORE_DIR):
        return did
    if now < RUNNER.get("backoff_until", 0):      # the runner failed a moment ago (Unity unreachable): wait
        return did
    kind, group = editor_ticket.grant_next(ROOT, now=now)
    if kind == "own" and group[0]["session"] == worker.PLAY_ROLE and group[0].get("job"):
        did.append(start_worker(group[0]["job"], "play"))      # the play job starts on its grant
    elif kind in ("own", "fix"):
        t = group[0]
        did.append(tell(t, grant_text(t), f"editor-grant:{t['id']}"))
    elif kind in ("load", "test"):
        for t in group:
            t["runner"] = RUNNER_ROLE
            editor_ticket.save(ROOT, t)
        run_in_background(load_run if kind == "load" else test_run, group)
        did.append((kind, ", ".join(t["session"] for t in group)))
    return did


# ---------------------------------------------------------------- workers (người quyết 2026-11-27)
# Tiers 4-7 are workers: a message to a feature, a service or @Core is a code job (worker.py: one turn in its own
# worktree, every feature at once); a message to @UnityAutoPlay is a play job (one turn in the main project, on its
# Editor ticket). At most WORKER_SLOTS jobs run at once, never two of the same feature.
WORKER_SLOTS = 3
WORKER_THREADS = {}      # job id -> thread
WORKER_LOCK = threading.Lock()
WORKER_STARTED = {"done": False}
WORKER_TARGET_RX = re.compile(r"^((_Services/)?[A-Za-z][A-Za-z0-9]*|@Core)$")


def worker_name(target):
    """The worker job's feature for an inbox target: Core for @Core, the feature or service itself."""
    return "Core" if target == "@Core" else target


def worker_jobs():
    d = worker.jobs_dir(ROOT)
    out = []
    for p in sorted(d.iterdir()) if d.is_dir() else []:
        if worker.ID_RX.match(p.name) and (p / "job.json").is_file():
            try:
                out.append(json.loads((p / "job.json").read_text(encoding="utf-8")))
            except (OSError, ValueError):
                continue
    return out


def running_jobs():
    with WORKER_LOCK:
        return {j: t for j, t in WORKER_THREADS.items() if t.is_alive()}


def start_worker(job_id, kind="code", message=None):
    """Starts one job (or a resume, with message) in a thread of its own. Returns what was done, for the log."""
    def go():
        try:
            if kind == "play":
                code, lines = worker.start_play(ROOT, job_id)
            elif message is not None:
                code, lines = worker.resume(ROOT, job_id, message)
            else:
                code, lines = worker.start(ROOT, job_id)
            print(f"worker: {job_id} xong ({code}):", " · ".join(lines[-4:])[:400], flush=True)
            stopped(job_id)
        except Exception as ex:
            print(f"worker: {job_id} lỗi {ex}", file=sys.stderr, flush=True)
            try:
                job = worker.load(ROOT, job_id)
                job["state"] = "error"
                job["error"] = str(ex)[:300]
                worker.save(ROOT, job)
            except Exception:
                pass
    t = threading.Thread(target=go, name=f"worker-{job_id}", daemon=True)
    with WORKER_LOCK:
        WORKER_THREADS[job_id] = t
    t.start()
    return ("start" if message is None else "resume", kind, job_id)


STOP_STATES = ("checks-failed", "drift", "needs-codelead", "error", "interrupted")


def stopped(job_id):
    """A job that ended stopped (its gates failed twice, a drift, an error): CodeLead is told once; worker.py has
    written the report. A job that waits for the human (asks) goes through Manager, not here."""
    try:
        job = worker.load(ROOT, job_id)
    except ValueError:
        return None
    if job.get("state") not in STOP_STATES:
        return None
    return notify_codelead(f"WORKER DỪNG · {job['feature']} · việc {job_id} · {job['state']}. Xem báo cáo mới và làm theo "
                           "skill codelead-resolve-issue.", f"stopped:{job_id}:{job['state']}")


def fix_round(job_id, counter, message):
    """One automatic fix round of a job (tests broke, Unity found compile errors, a drift); a second one in the same
    kind stops the job with a report for CodeLead."""
    try:
        job = worker.load(ROOT, job_id)
    except ValueError:
        return
    if job.get(counter, 0) >= 1:
        job["state"] = "needs-codelead"
        worker.save(ROOT, job)
        worker.write_report(ROOT, job, "bị chặn", [f"tự sửa một lần chưa đủ ({counter}): " + message[:400]])
        notify_codelead(f"WORKER CẦN CODELEAD · {job['feature']} · việc {job_id} · tự sửa một lần chưa đủ ({counter}). "
                        "Xem báo cáo mới và làm theo skill codelead-resolve-issue.", f"needs-codelead:{job_id}")
        return
    job[counter] = job.get(counter, 0) + 1
    job["pending"] = message
    worker.save(ROOT, job)


ACTIVE_JOB_STATES = ("queued", "starting", "running", "waiting-editor")


def worker_tick(now=None):
    """One pass: jobs left running by a server that stopped, answers to the workers' questions, fix rounds, and the
    queued jobs, while slots are free."""
    did = []
    jobs = worker_jobs()
    alive = running_jobs()
    if not WORKER_STARTED["done"]:      # the server just started: no thread runs any job
        WORKER_STARTED["done"] = True
        for j in jobs:
            if j.get("state") in ("starting", "running") and j["id"] not in alive:
                j["state"] = "interrupted"
                worker.save(ROOT, j)
                did.append(("interrupted", j["id"]))
    try:
        answered = {q["id"]: q for q in ask.items(ROOT, "answered") if q.get("source") == WORKER_ROLE}
    except Exception:
        answered = {}
    for j in jobs:      # a question of the worker answered on Manager's side: the job goes on with the answer
        done = set(j.get("answered") or [])
        for qid in [q for q in j.get("asks") or [] if q in answered and q not in done]:
            j.setdefault("answered", []).append(qid)
            j["pending"] = "Người trả lời câu hỏi của bạn (qua Manager): " + str(answered[qid].get("answer") or "") + \
                           "\nLàm tiếp theo câu trả lời đó, kiểm lại rồi ghi lại result.json."
            worker.save(ROOT, j)
            did.append(("answered", j["id"], qid))
    busy = {j["feature"] for j in jobs if j["id"] in alive}
    free = WORKER_SLOTS - len(alive)
    for j in sorted(jobs, key=lambda x: x.get("created", 0)):
        if free <= 0:
            break
        if j["id"] in alive or j["feature"] in busy:
            continue
        if j.get("pending") and j.get("kind", "code") == "code":
            message = j.pop("pending")
            j["state"] = "running"
            worker.save(ROOT, j)
            did.append(start_worker(j["id"], "code", message))
        elif j.get("state") == "queued" and j.get("kind", "code") == "code":
            did.append(start_worker(j["id"], "code"))
        else:
            continue
        busy.add(j["feature"])
        free -= 1
    return did


def route_to_worker(target, ms, now):
    """Inbox messages to a feature, a service, @Core or the tester become a worker job (people quyết 2026-11-27: no
    feature thread any more). Returns the steps done; the messages are marked delivered with the job's id."""
    text = "\n".join(f"[tin {m['id']} · từ {m.get('source') or '?'}] {m.get('text') or ''}" for m in ms)
    ids = [m["id"] for m in ms]
    source = ", ".join(sorted({str(m.get("source") or "") for m in ms}))
    if dispatch.pool_of(target) == "tester":
        job = worker.queue_play(ROOT, text, source=source, messages=ids, now=now)
    else:
        name = worker_name(target)
        jobs = [j for j in worker_jobs() if j.get("feature") == name and j.get("kind", "code") == "code"]
        alive = running_jobs()
        waiting = next((j for j in jobs if j.get("asks") and str(j.get("state", "")).startswith("applied-")
                        and j["id"] not in alive and not j.get("pending")), None)
        queued = next((j for j in jobs if j.get("state") == "queued"), None)
        busy = next((j for j in jobs if j["id"] in alive or j.get("pending")), None)
        if busy:      # the next job takes them, once this one is done
            for m in ms:
                if m["state"] != "waiting" or m.get("why", "").split(" ·")[0] != f"worker đang làm {busy['id']}":
                    dispatch.mark(ROOT, m, "waiting", why=f"worker đang làm {busy['id']} · tin thành việc kế tiếp khi việc đó xong")
            return [("wait", target, "worker đang làm " + busy["id"])]
        if waiting:      # the feature's worker waits for an answer: the message is it (a Leader answered)
            waiting["pending"] = "Tin mới cho việc của bạn:\n" + text + "\nLàm tiếp theo tin đó, kiểm lại rồi ghi lại result.json."
            worker.save(ROOT, waiting)
            job = waiting
        elif queued:
            queued["task"] = (queued.get("task") + "\n" if queued.get("task") else "") + text
            queued["messages"] = (queued.get("messages") or []) + ids
            worker.save(ROOT, queued)
            job = queued
        else:
            job = worker.queue(ROOT, name, task=text, now=now, messages=ids, source=source)
    for m in ms:
        dispatch.mark(ROOT, m, "delivered", delivered_at=now, job=job["id"])
    return [("worker", target, job["id"])]


def worker_load_task(job_id):
    return worker.load(ROOT, job_id).get("task", "")


def workers_view():
    """The jobs for the page, newest first."""
    out = []
    for j in sorted(worker_jobs(), key=lambda x: x.get("created", 0), reverse=True)[:60]:
        r = j.get("result") or {}
        out.append({"id": j["id"], "feature": j.get("feature"), "kind": j.get("kind", "code"), "state": j.get("state"),
                    "requirements": j.get("requirements", 0), "turns": len(j.get("turns") or []),
                    "cost": round(sum(t.get("cost_usd") or 0 for t in j.get("turns") or []), 2),
                    "model": j.get("model") or "", "effort": j.get("effort") or "",
                    "summary": str(r.get("summary") or j.get("task") or "")[:400], "question": r.get("question"),
                    "created": j.get("created"), "running": j["id"] in running_jobs(), "pending": bool(j.get("pending"))})
    model, effort = worker.defaults(ROOT)
    return {"jobs": out, "slots": WORKER_SLOTS, "models": list(worker.MODELS), "efforts": list(worker.EFFORTS),
            "defaults": {"model": model, "effort": effort}}


# ---------------------------------------------------------------- tab "Đang chạy" (người duyệt 2026-11-27)
# Every agent that runs now, in one place: Leaders and tool threads, workers, the tester, their subagents; how long its
# turn runs, its model, what it does (the last tool calls of its transcript), the tokens. Read only, nothing started.
ACTIVITY_TAIL = 400_000      # bytes of a transcript read for the turn and the recent calls
ACTIVITY_TOOLS = 10
SUB_FRESH_SECONDS = 600      # a subagent not marked finished whose transcript is older is left over, not running
TAIL_CACHE = {}
TAIL_LOCK = threading.Lock()
PROJECTS_HOME = Path.home() / ".claude" / "projects"


def project_dir(path):
    """~/.claude/projects/<path with every non-alphanumeric character as '-'>: where the CLI keeps a cwd's transcripts."""
    return PROJECTS_HOME / re.sub(r"[^A-Za-z0-9]", "-", str(path))


def iso_seconds(stamp):
    try:
        return calendar.timegm(time.strptime(str(stamp)[:19], "%Y-%m-%dT%H:%M:%S"))      # the CLI writes UTC
    except (ValueError, OverflowError):
        return 0


def call_text(name, inp):
    """'Edit HeroFakes.cs', 'Bash python …', 'Agent: List files': one tool call, short, for the page."""
    if not isinstance(inp, dict):
        return name
    short = name.replace("mcp__UnityMCP__", "unity:")
    if inp.get("file_path") or inp.get("notebook_path"):
        what = Path(str(inp.get("file_path") or inp.get("notebook_path"))).name
    elif name in ("Bash", "PowerShell"):
        what = str(inp.get("description") or inp.get("command") or "")
    elif name in ("Agent", "Task"):
        what = str(inp.get("description") or inp.get("subagent_type") or "")
    elif name in ("Grep", "Glob"):
        what = str(inp.get("pattern") or "")
    else:
        what = " ".join(f"{k}={v}" for k, v in inp.items() if isinstance(v, (str, int, float)) and k != "unity_instance")
    return (short + " " + re.sub(r"\s+", " ", what).strip())[:140].strip()


def tail_info(path):
    """{start, model, tools: [{at, text, open}], turn_cost} of the last turn in a transcript: start is the time of its
    last prompt (not a tool result), turn_cost the tokens since then (priced as usage_of does). {} with no file."""
    try:
        st = os.stat(path)
    except OSError:
        return {}
    stamp = (st.st_mtime_ns, st.st_size)
    with TAIL_LOCK:
        hit = TAIL_CACHE.get(str(path))
    if hit and hit[0] == stamp:
        return hit[1]
    try:
        with open(path, "rb") as f:
            f.seek(max(0, st.st_size - ACTIVITY_TAIL))
            lines = f.read().decode("utf-8", errors="replace").splitlines()
    except OSError:
        return {}
    start, model, calls, done, cost, seen = 0, "", [], set(), 0, set()
    for line in lines:
        if '"type":"user"' not in line and '"type":"assistant"' not in line and '"type": "user"' not in line:
            continue
        try:
            row = json.loads(line)
        except ValueError:
            continue      # the first line of the tail may be cut
        if not isinstance(row, dict) or row.get("type") not in ("assistant", "user"):
            continue
        m = row.get("message") or {}
        content = m.get("content")
        at = iso_seconds(row.get("timestamp"))
        if row["type"] == "user":
            blocks = content if isinstance(content, list) else []
            results = [b for b in blocks if isinstance(b, dict) and b.get("type") == "tool_result"]
            done.update(b.get("tool_use_id") for b in results)
            if not results and not row.get("isMeta") and (isinstance(content, str) or blocks):
                start, calls, cost, seen = at, [], 0, set()      # a new prompt: a new turn
            continue
        if m.get("model") and m.get("model") != SYNTHETIC_MODEL:
            model = m["model"]
        u = m.get("usage")
        if isinstance(u, dict) and m.get("id") not in seen:
            seen.add(m.get("id"))
            write = int(u.get("cache_creation_input_tokens") or 0)
            hour = int((u.get("cache_creation") or {}).get("ephemeral_1h_input_tokens") or 0) if isinstance(u.get("cache_creation"), dict) else 0
            cost += (int(u.get("input_tokens") or 0) + hour * 2 + max(write - hour, 0) * 1.25 +
                     int(u.get("cache_read_input_tokens") or 0) * 0.1 + int(u.get("output_tokens") or 0) * 5)
        for b in content if isinstance(content, list) else []:
            if isinstance(b, dict) and b.get("type") == "tool_use":
                calls.append({"at": at, "id": b.get("id"), "text": call_text(str(b.get("name") or ""), b.get("input"))})
    tools = [{"at": c["at"], "text": c["text"], "open": c["id"] not in done} for c in calls[-ACTIVITY_TOOLS:]][::-1]
    info = {"start": start, "model": model, "tools": tools, "turn_cost": int(round(cost))}
    with TAIL_LOCK:
        TAIL_CACHE[str(path)] = (stamp, info)
    return info


def model_short(model):
    """'claude-opus-5-5' -> 'opus'; an alias stays as it is."""
    m = re.search(r"(opus|sonnet|haiku|fable)", str(model or ""))
    return m.group(1) if m else str(model or "")


def agent_row(kind, name, path, now, model="", effort="", state="", task="", total=None, ident=""):
    """One row of the tab from a transcript: its last turn, what it does, its tokens."""
    info = tail_info(path) if path else {}
    try:
        quiet = int(now - os.stat(path).st_mtime) if path else 0
    except OSError:
        quiet = 0
    tools = info.get("tools") or []
    return {"kind": kind, "name": name, "id": ident, "model": model_short(info.get("model") or model), "effort": effort,
            "state": state, "task": str(task or "")[:2000], "start": info.get("start") or 0, "quiet": quiet,
            "doing": (tools[0]["text"] if tools else ""), "doing_open": bool(tools and tools[0]["open"]),
            "tools": tools, "turn_tokens": info.get("turn_cost", 0),
            "tokens": (total if total is not None else (usage_of(path).get("cost", 0) if path else 0)), "subs": []}


def subagent_rows(path, now):
    """The subagents of the transcript at `path` that still run (<session>/subagents/*.jsonl, meta not finished)."""
    folder = Path(str(path)[:-len(".jsonl")]) / "subagents"
    rows = []
    for p in sorted(folder.glob("*.jsonl")) if folder.is_dir() else []:
        try:
            meta = json.loads(p.with_suffix(".meta.json").read_text(encoding="utf-8"))
        except (OSError, ValueError):
            meta = {}
        try:
            age = now - p.stat().st_mtime
        except OSError:
            continue
        if meta.get("finished") or age > SUB_FRESH_SECONDS:
            continue
        rows.append(agent_row("subagent", meta.get("agentType") or "subagent", p, now, model=meta.get("model") or "",
                              state="chạy", task=meta.get("description") or "", ident=p.stem))
    return rows


def job_transcript(job):
    if not job.get("session") or not job.get("worktree"):
        return None
    return project_dir(job["worktree"]) / f"{job['session']}.jsonl"


JOB_STATE_NAMES = {"queued": "xếp hàng", "starting": "đang dựng worktree", "running": "chạy", "waiting-editor": "chờ phiếu Editor",
                   "applied": "đã nạp", "applied-partial": "đã nạp, còn dở", "applied-blocked": "chờ người trả lời",
                   "checks-failed": "hỏng cổng kiểm", "drift": "lệch project chính", "needs-codelead": "cần CodeLead",
                   "interrupted": "bị ngắt", "error": "lỗi", "played": "đã chơi thử", "play-failed": "chơi thử lỗi", "dropped": "đã gỡ"}


def activity_view(now=None):
    """GET /api/activity: {now, rows, done, totals}. rows: what runs now (threads that are live or in a server turn,
    workers and the tester that run or wait), each with its running subagents; done: the jobs that ended today."""
    now = now or time.time()
    live = live_sessions()
    with BUSY_LOCK:
        busy_ids = set(BUSY)
    rows = []
    for t in threads():
        s = live.get(t["id"]) or {}
        if not s and t["id"] not in busy_ids:
            continue
        path = transcripts_dir() / f"{t['id']}.jsonl"
        state = "chờ người" if s.get("state") == "blocked" else ("chạy" if s.get("status") == "busy" or t["id"] in busy_ids else "rảnh")
        h = hang_of(t, s, now)
        row = agent_row("thread", t["title"], path, now, model=(t.get("run") or {}).get("model") or t.get("model"),
                        effort=(t.get("run") or {}).get("effort") or t.get("effort"), state=state,
                        total=thread_usage(t["id"]).get("cost", 0), ident=t["id"])
        row["hang"] = hang_text(h) if h else ""
        row["where"] = "VS Code" if s.get("kind") == "interactive" else ("nền" if s else "lượt của server")
        row["subs"] = subagent_rows(path, now)
        rows.append(row)
    alive = running_jobs()
    open_tickets = {}      # job id -> its Editor ticket still open: the server loads, compiles, tests after the turn
    try:
        for tk in editor_ticket.tickets(ROOT, ("waiting", "granted")):
            if tk.get("job"):
                open_tickets[tk["job"]] = tk["state"]
    except Exception:
        pass
    done, cost_today = [], 0.0
    day = time.strftime("%Y-%m-%d", time.localtime(now))
    for j in sorted(worker_jobs(), key=lambda x: x.get("created", 0)):
        turns = j.get("turns") or []
        cost = sum(x.get("cost_usd") or 0 for x in turns)
        cost_today += sum(x.get("cost_usd") or 0 for x in turns if time.strftime("%Y-%m-%d", time.localtime(x.get("at") or 0)) == day)
        name = ("Tester" if j.get("kind") == "play" else "W " + str(j.get("feature") or "?"))
        running = j["id"] in alive
        ticket = open_tickets.get(j["id"])
        if running or j.get("state") in ACTIVE_JOB_STATES or j.get("pending") or ticket:
            path = job_transcript(j) if running else None
            state = ("chạy" if running else "chạy tiếp ở vòng sau" if j.get("pending")
                     else "ở Editor: nạp, biên dịch, test" if ticket == "granted" and j.get("kind", "code") == "code"
                     else "chờ Editor để nạp và test" if ticket == "waiting" and j.get("kind", "code") == "code"
                     else JOB_STATE_NAMES.get(j.get("state"), j.get("state")))
            row = agent_row("worker" if j.get("kind", "code") == "code" else "tester", name, path, now, model=j.get("model"),
                            effort=j.get("effort"), state=state, task=j.get("task"), ident=j["id"],
                            total=(usage_of(path).get("cost", 0) if path else 0))
            row.update(cost=round(cost, 2), job_seconds=sum(x.get("seconds") or 0 for x in turns), asks=j.get("asks") or [],
                       created=j.get("created") or 0, events=job_events(j["id"]))
            row["subs"] = subagent_rows(path, now) if path else []
            rows.append(row)
        elif time.strftime("%Y-%m-%d", time.localtime(j.get("updated") or j.get("created") or 0)) == day:
            done.append({"id": j["id"], "name": name, "state": JOB_STATE_NAMES.get(j.get("state"), j.get("state")),
                         "events": job_events(j["id"]), "seconds": sum(x.get("seconds") or 0 for x in turns),
                         "cost": round(cost, 2), "model": model_short(j.get("model")), "effort": j.get("effort") or "",
                         "summary": str((j.get("result") or {}).get("summary") or "")[:300], "updated": j.get("updated") or 0})
    done.sort(key=lambda d: d["updated"], reverse=True)
    running_n = sum(1 for r in rows if r["state"] == "chạy") + sum(len(r["subs"]) for r in rows)
    return {"now": now, "rows": rows, "done": done,
            "totals": {"running": running_n, "slots": WORKER_SLOTS, "workers": len(alive), "cost_today": round(cost_today, 2),
                       "tokens_live": sum(r["tokens"] for r in rows if r["kind"] == "thread")}}


def worker_call(action, data):
    """The page: run (a feature, an extra task, model and effort: none takes the default), resume (an id and a
    message), drop (an id), defaults (the model and effort of every code job that names none, inbox ones too)."""
    try:
        if action == "defaults":
            model, effort = worker.set_defaults(ROOT, str(data.get("model") or ""), str(data.get("effort") or ""))
            return {"ok": True, "defaults": {"model": model, "effort": effort}}
        if action == "run":
            target = str(data.get("feature") or "").strip()
            if not WORKER_TARGET_RX.match(target) or not (target == "@Core" or guard.valid_feature(target, str(ROOT))):
                return {"ok": False, "error": "feature không hợp lệ"}
            job = worker.queue(ROOT, worker_name(target), task=str(data.get("task") or "").strip()[:4000], source="người",
                               model=str(data.get("model") or "") or None, effort=str(data.get("effort") or "") or None)
            return {"ok": True, "id": job["id"]}
        job = worker.load(ROOT, str(data.get("id") or ""))
        if job["id"] in running_jobs():
            return {"ok": False, "error": "việc đang chạy"}
        if action == "resume":
            job["pending"] = str(data.get("message") or "").strip()[:4000] or "Làm tiếp việc đang dở, rồi ghi lại result.json."
            worker.save(ROOT, job)
            return {"ok": True, "id": job["id"]}
        if action == "drop":
            worker.drop(ROOT, job["id"])
            return {"ok": True, "id": job["id"]}
    except ValueError as ex:
        return {"ok": False, "error": str(ex)}
    return {"ok": False, "error": "lệnh không hợp lệ"}


def courier_loop():
    lock = None
    while lock is None:      # one courier on this machine: another server already delivering keeps the lock
        lock = dispatch.courier_lock(ROOT)
        if lock is None:
            print("hộp thư: một server khác đang giao tin; server này không giao, thử lại sau 30 giây", flush=True)
            time.sleep(30)
    while True:
        try:
            for step in courier_tick():
                print("hộp thư:", *step, flush=True)
            for step in watch_tick() + wake_blocked():
                print("canh phiên:", *step, flush=True)
            for step in editor_tick():
                print("phiếu Editor:", *step, flush=True)
            for step in worker_tick():
                print("worker:", *step, flush=True)
        except Exception as ex:      # the loop must survive anything a tick meets
            print(f"hộp thư: lỗi {ex}", file=sys.stderr, flush=True)
        time.sleep(COURIER_SECONDS)


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
                return self.send_json(200, {"threads": threads(include_unnamed=True), "sets": sets(), "models": MODELS, "efforts": EFFORTS,
                                            "defaults": {"model": DEFAULT_MODEL, "effort": DEFAULT_EFFORT},
                                            "vscode": vscode_view(), "asks": asks_view()})
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
            if path == "/api/workers":
                return self.send_json(200, workers_view())
            if path == "/api/activity":
                return self.send_json(200, activity_view())
            if path == "/api/inbox":
                return self.send_json(200, inbox_view())
            if path == "/api/inbox/reqs":
                query = urllib.parse.parse_qs(urllib.parse.urlsplit(self.path).query)
                return self.send_json(200, inbox_reqs((query.get("feature") or [""])[0]))
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
            if self.path == "/api/gamedesign/gpt-pick":
                return self.send_json(200, gpt_pick(data))
            if self.path == "/api/gamedesign/user-idea":
                return self.send_json(200, user_idea(data))
            if self.path == "/api/gamedesign/delete":
                return self.send_json(200, human_delete(data))
            if self.path.startswith("/api/inbox/"):
                return self.send_json(200, inbox_call(self.path.rsplit("/", 1)[1], data))
            if self.path.startswith("/api/workers/"):
                return self.send_json(200, worker_call(self.path.rsplit("/", 1)[1], data))
            if self.path.startswith("/api/references/"):
                return self.send_json(200, reference_call(self.path.rsplit("/", 1)[1], data))
            if self.path == "/api/gamedesign/progress":
                return self.send_json(200, progress(data.get("path"), data.get("phase"), data.get("state"), data.get("note")))
            if self.path == "/api/gamedesign/requirement":
                return self.send_json(200, requirement(data.get("ref"), data.get("action"), data.get("note")))
            if self.path == "/api/gamedesign/priority":
                return self.send_json(200, priority(data.get("path"), data.get("score")))
            if self.path == "/api/gamedesign/sync":
                return self.send_json(200, sync_live())
            if self.path == "/api/gamedesign/mode":
                return self.send_json(200, set_mode(str(data.get("pin") or ""), data.get("mode")))
            if self.path == "/api/gamedesign/stage":
                return self.send_json(200, set_stage(str(data.get("pin") or ""), data.get("stage")))
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
            if self.path == "/api/sessions/model":
                return self.send_json(200, set_model(data.get("id"), str(data.get("model") or ""), str(data.get("effort") or "")))
            if self.path == "/api/sessions/reset-models":
                return self.send_json(200, reset_models())
            if self.path == "/api/sessions/delete":
                return self.send_json(200, delete_thread(data.get("id"), data.get("pin")))
            if self.path == "/api/sessions/delete-outside":
                return self.send_json(200, delete_outside(data.get("pin")))
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
    threading.Thread(target=courier_loop, name="courier", daemon=True).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
