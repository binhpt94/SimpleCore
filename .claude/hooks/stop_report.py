#!/usr/bin/env python3
"""Stop hook. A turn that changed files must end with a report for @Manager.

Applies to the groups Basic (@Core), Features and Services, to @CodeLead (it edits the skeleton and _Composition) and
to @GameDesigner (it edits the design tree: @Manager must know when it is done and what waits for the human) and
to the testers (@UnityAutoPlay: a turn that wrote a test run reports where it is and what it found).
Not to @human, @Manager (it edits nothing), @DesignAdvisor (its work is the advice files) or unbound chats.
@CodeLead and @Manager are both shown new reports (bind_session.py).
"Changed files" = an Edit/Write/MultiEdit/NotebookEdit, or a unity-mcp call that changes something (an asset, the
open scene, a prefab stage, project settings: guard.unity_changes; also inside batch_execute), after the human's last prompt,
read from the transcript. The agent's last message must hold the block in REPORT_FORMAT (see
.claude/rules/report.md). The hook saves it to Management/reports/<time>-<role>.md, where @Manager is told
about it on its next prompt (bind_session.py) and the management page lists it.
Missing report: the agent is sent back once (a "pending" mark in the store keeps the check on even if the
hook's own message starts a new turn in the transcript); if it stops again without one, a "no report" note
is saved so the Manager still sees that the session changed files. A broken transcript lets the agent stop.
A turn starts at a human prompt or at a message from another session (SendMessage, a meta entry of origin "peer"),
so a reply to a Leader's message does not take the report of the turn before it. A block identical to the newest
saved report of the same role is not saved again.
Requirement book (Management/tools/gd_req.py): the saved report gets a line "- Sổ yêu cầu:" with every doing / done /
verify / reject the turn ran. A Features/Services chat that edited its Scripts/ or Tests/ without writing any of them
is sent back once to write doing or done for what it worked on (REQ_REMINDER); stopping again lets it go.
"""
import json
import os.path
import os
import re
import sys
import time

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))      # `python -I` leaves the script folder out
import guard  # noqa: E402

CHANGE_TOOLS = guard.EDIT_TOOLS
HEADER = "## Báo cáo cho @Manager"
FIELDS = ("Phiên", "Việc", "Trạng thái", "Đã đổi", "Công khai đổi", "Cần bên khác", "Kiểm tra", "Ghi chú")
STATES = ("xong", "dở", "bị chặn")
REPORT_FORMAT = HEADER + "\n" + "\n".join([
    "- Phiên: FEATURE=<giá trị của chat này>",
    "- Việc: <1 câu: đã làm gì>",
    "- Trạng thái: xong | dở | bị chặn",
    "- Đã đổi: <file hoặc vùng>",
    "- Công khai đổi: không | <Contracts, FeatureAssets/Public, API của Core... đã đổi>",
    "- Cần bên khác: không | <phiên nào, việc gì>",
    "- Kiểm tra: <kết quả validate.py; test cần người chạy>",
    "- Ghi chú: <rủi ro, câu hỏi cho người; hoặc không>",
])


REQ_CMD_RX = re.compile(r"gd_req\.py (\S+) (doing|done|verify|reject) (\S+)")
REQ_REMINDER = ("This turn changed the code of {f} but wrote nothing in its requirement book. Before you stop, run "
                "python Management/tools/gd_req.py {f} doing <node>#<id> \"<what you are at>\" for each requirement you "
                "worked on, or done <node>#<id> <Class.Method>[,...] when its test is written (see the line 'Sổ yêu cầu' at "
                "the start of the message, and python Management/tools/gd_req.py {f}). A renewed chat learns from the book "
                "where it left off. Then end with the report again.")


def is_prompt(e):
    """Does this transcript entry start a turn? A human prompt (or a task notification), or a message from another
    Claude session (SendMessage: isMeta, origin kind "peer"). Not tool results, nor the meta entries inside a turn
    (skill text, hook feedback, context): those belong to the turn they appear in."""
    if not isinstance(e, dict) or e.get("type") != "user":
        return False
    content = (e.get("message") or {}).get("content")
    if isinstance(content, list) and any(isinstance(x, dict) and x.get("type") == "tool_result" for x in content):
        return False
    origin = e.get("origin")
    return not e.get("isMeta") or (isinstance(origin, dict) and origin.get("kind") == "peer")


def turn_entries(path):
    """The transcript entries of this turn: after the last entry that started a turn (is_prompt)."""
    entries = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            try:
                entries.append(json.loads(line))
            except ValueError:
                continue
    start = 0
    for i, e in enumerate(entries):
        if is_prompt(e):
            start = i + 1
    return entries[start:]


def turn_tools(path):
    """[(tool name, input)] of the agent's tool calls in this turn, from the transcript."""
    entries = turn_entries(path)
    out = []
    for e in entries:
        content = (e.get("message") or {}).get("content") if isinstance(e, dict) and e.get("type") == "assistant" else None
        for x in content if isinstance(content, list) else []:
            if isinstance(x, dict) and x.get("type") == "tool_use":
                out.append((x.get("name") if isinstance(x.get("name"), str) else "",
                            x.get("input") if isinstance(x.get("input"), dict) else {}))
    return out


def req_changes(tools):
    """["Wallet doing wallet-rules#cap-max", ...]: the requirement-book writes the turn ran."""
    out = []
    for name, ti in tools:
        if name in guard.SHELL_TOOLS:
            for m in REQ_CMD_RX.finditer(str(ti.get("command") or "")):
                out.append(" ".join(m.groups()))
    return out


def edited_code(tools, feature, root):
    """Did the turn edit a file under Scripts/ or Tests/ of the feature folder?"""
    base = os.path.join(root, "UnityProject", "Assets", "Features", *feature.split("/"))
    for name, ti in tools:
        if name not in CHANGE_TOOLS:
            continue
        p = str(ti.get("file_path") or ti.get("notebook_path") or "")
        full = os.path.normcase(os.path.abspath(os.path.join(root, p)))
        for sub in ("Scripts", "Tests"):
            if full.startswith(os.path.normcase(os.path.join(base, sub)) + os.sep):
                return True
    return False


def read_turn(path):
    """(changed files in this turn?, text of the agent's messages in this turn) from the transcript."""
    entries = turn_entries(path)
    changed, texts = False, []
    for e in entries:
        if not isinstance(e, dict) or e.get("type") != "assistant":
            continue
        content = (e.get("message") or {}).get("content")
        for x in content if isinstance(content, list) else []:
            if not isinstance(x, dict):
                continue
            if x.get("type") == "tool_use":
                name = x.get("name") if isinstance(x.get("name"), str) else ""
                ti = x.get("input") if isinstance(x.get("input"), dict) else {}
                name = guard.unity_name(name)
                if name in CHANGE_TOOLS or (name.startswith(guard.UNITY_PREFIX) and guard.unity_changes(name, ti)):
                    changed = True
            elif x.get("type") == "text":
                texts.append(x.get("text") or "")
    return changed, "\n".join(texts)


def find_report(text):
    """(report block, None) or (None, what is missing)."""
    i = text.rfind(HEADER)
    if i < 0:
        return None, f"no '{HEADER}' block"
    block = text[i:].strip()
    missing = [n for n in FIELDS if not re.search(rf"^-\s*{n}:\s*\S", block, re.M)]
    if missing:
        return None, "missing lines: " + ", ".join(missing)
    state = re.search(r"^-\s*Trạng thái:\s*(.*)$", block, re.M).group(1).strip().lower()
    if not any(state.startswith(s) for s in STATES):
        return None, "Trạng thái must be one of: " + " | ".join(STATES)
    return block, None


def report_slug(role):
    return re.sub(r"[^A-Za-z0-9]+", "-", role).strip("-") or "unknown"


def same_as_last(root, role, body):
    """Is body the same block as the newest saved report of this role? (a repeat is not saved again)"""
    folder, slug = os.path.join(root, "Management", "reports"), report_slug(role)
    rx = re.compile(r"^(\d{8}-\d{6})-" + re.escape(slug) + r"(?:-(\d+))?\.md$")
    try:
        found = [(m.group(1), int(m.group(2) or 1), n) for n in os.listdir(folder) for m in [rx.match(n)] if m]
    except OSError:
        return False
    if not found:
        return False
    names = [n for _, _, n in sorted(found)]      # same second: <stamp>-<role>.md, then -2, -3...
    try:
        with open(os.path.join(folder, names[-1]), encoding="utf-8") as f:
            text = f.read()
    except OSError:
        return False
    if text.startswith("<!--"):
        text = text.split("\n", 1)[1] if "\n" in text else ""
    return text.strip() == body.strip()


def save(root, role, body):
    folder = os.path.join(root, "Management", "reports")
    slug = report_slug(role)
    stamp = time.strftime("%Y%m%d-%H%M%S")
    path = os.path.join(folder, f"{stamp}-{slug}.md")
    n = 1
    while os.path.exists(path):
        n += 1
        path = os.path.join(folder, f"{stamp}-{slug}-{n}.md")
    os.makedirs(folder, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(f"<!-- {time.strftime('%Y-%m-%d %H:%M:%S')} · FEATURE={role} · ghi bởi stop_report.py -->\n{body}\n")
    return path


def pending_path(session_id, store):
    """Mark that this chat was sent back for a missing report; None when the session id is unusable."""
    p = guard.binding_path(session_id, store)
    return os.path.join(store, "reports-pending", os.path.basename(p)) if p else None


def main(argv):
    try:
        try:
            data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
        except ValueError:
            data = {}
        if not isinstance(data, dict):
            data = {}
        root, store = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), guard.STORE_DIR
        if len(argv) == 5 and argv[1] == "--root" and argv[3] == "--store":      # test-only override
            root, store = os.path.abspath(argv[2]), argv[4]
        raw = guard.session_feature(data.get("session_id"), store).strip().replace("\\", "/")
        role = guard.role_of(raw)
        if role in ("@human", "@Manager", "@DesignAdvisor", guard.WORKER_ROLE) or not (role or guard.valid_feature(raw, root)):
            return 0
        who = role or raw
        pending = pending_path(data.get("session_id"), store)
        was_pending = bool(pending) and os.path.exists(pending)

        def done():
            if was_pending:
                os.remove(pending)

        path = data.get("transcript_path")
        if not isinstance(path, str) or not path:
            return 0
        changed, text = read_turn(path)
        if not changed and not was_pending:
            return 0
        if isinstance(data.get("last_assistant_message"), str):
            text += "\n" + data["last_assistant_message"]
        block, problem = find_report(text)
        if block:
            try:
                tools = turn_tools(path)
            except (OSError, ValueError):
                tools = []
            reqs = req_changes(tools)
            body = block + ("\n- Sổ yêu cầu: " + "; ".join(reqs) if reqs else "")
            if not same_as_last(root, who, body):      # the same block again (a later turn quoting it) is not saved twice
                save(root, who, body)
            done()
            # saved first: the reminder's own message may start a new turn, after which this turn's report is not seen
            if (not role and not reqs and not data.get("stop_hook_active") and not was_pending
                    and edited_code(tools, raw, root)):
                sys.stdout.buffer.write(json.dumps({"decision": "block", "reason": REQ_REMINDER.format(f=raw)},
                                                   ensure_ascii=False).encode("utf-8"))
            return 0
        if data.get("stop_hook_active") or was_pending:
            save(root, who, f"{HEADER}\n- Phiên: FEATURE={who}\n- Việc: (phiên dừng mà không báo cáo)\n"
                            "- Trạng thái: dở\n- Đã đổi: ?\n- Công khai đổi: ?\n- Cần bên khác: ?\n"
                            "- Kiểm tra: ?\n- Ghi chú: lượt này có sửa file nhưng AI không viết báo cáo; "
                            "hỏi lại phiên đó.")
            done()
            return 0
        if pending:
            os.makedirs(os.path.dirname(pending), exist_ok=True)
            with open(pending, "w", encoding="utf-8") as f:
                f.write(time.strftime("%Y-%m-%d %H:%M:%S"))
        reason = (f"This turn changed files, so it must end with a report for @Manager ({problem}). "
                  "End your reply with exactly this block, every line filled in (see .claude/rules/report.md):\n\n"
                  + REPORT_FORMAT)
        sys.stdout.buffer.write(json.dumps({"decision": "block", "reason": reason}, ensure_ascii=False).encode("utf-8"))
        return 0
    except Exception:
        return 0      # a broken check must not trap the agent in a loop


if __name__ == "__main__":
    sys.exit(main(sys.argv))
