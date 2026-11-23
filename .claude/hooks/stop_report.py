#!/usr/bin/env python3
"""Stop hook. A turn that changed files must end with a report for @Manager.

Applies to the groups Basic (@Core, @Composition), Features and Services, and to @CodeLead (it edits the skeleton).
Not to @human, @Manager (it only creates proposals), @GameDesigner (its work is the design tree) or unbound chats.
@CodeLead and @Manager are both shown new reports (bind_session.py).
"Changed files" = an Edit/Write/MultiEdit/NotebookEdit or a unity-mcp delete after the human's last prompt,
read from the transcript. The agent's last message must hold the block in REPORT_FORMAT (see
.claude/rules/report.md). The hook saves it to Management/reports/<time>-<role>.md, where @Manager is told
about it on its next prompt (bind_session.py) and the management page lists it.
Missing report: the agent is sent back once (a "pending" mark in the store keeps the check on even if the
hook's own message starts a new turn in the transcript); if it stops again without one, a "no report" note
is saved so the Manager still sees that the session changed files. A broken transcript lets the agent stop.
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

CHANGE_TOOLS = guard.EDIT_TOOLS | {guard.UNITY_ASSET_TOOL}
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


def read_turn(path):
    """(changed files in this turn?, text of the agent's messages in this turn) from the transcript."""
    entries = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            try:
                entries.append(json.loads(line))
            except ValueError:
                continue
    start = 0
    for i, e in enumerate(entries):
        if not isinstance(e, dict) or e.get("type") != "user" or e.get("isMeta"):
            continue
        content = (e.get("message") or {}).get("content")
        if isinstance(content, list) and any(isinstance(x, dict) and x.get("type") == "tool_result" for x in content):
            continue
        start = i + 1
    changed, texts = False, []
    for e in entries[start:]:
        if not isinstance(e, dict) or e.get("type") != "assistant":
            continue
        content = (e.get("message") or {}).get("content")
        for x in content if isinstance(content, list) else []:
            if not isinstance(x, dict):
                continue
            if x.get("type") == "tool_use" and x.get("name") in CHANGE_TOOLS:
                ti = x.get("input") if isinstance(x.get("input"), dict) else {}
                if x.get("name") != guard.UNITY_ASSET_TOOL or ti.get("action") == "delete":
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


def save(root, role, body):
    folder = os.path.join(root, "Management", "reports")
    slug = re.sub(r"[^A-Za-z0-9]+", "-", role).strip("-") or "unknown"
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
        if role in ("@human", "@Manager", "@GameDesigner") or not (role or guard.valid_feature(raw, root)):
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
            save(root, who, block)
            done()
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
