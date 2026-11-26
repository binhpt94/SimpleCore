#!/usr/bin/env python3
"""PostToolUse / PostToolUseFailure hook: a service failed, so the session stops that work in this turn.
Người quyết 2026-11-26: unity-mcp lỗi kết nối, hay Codex lỗi: dừng ngay, không thử lại, không đi đường khác, báo
Leader (Unity) hay bên giao (Codex) MỘT tin; không probe, không báo ai khác.

- A unity-mcp call that failed with a connection error (guard.UNITY_FAIL_RX: the Editor is not there, refused,
  timed out...) sets the session's "unity" flag. Any other failure of a unity-mcp call (a wrong parameter, the
  command's own error) is left alone.
- A Bash command whose output has guard.CODEX_FAIL_MARK (gd_advise.py and studio.py print it when Codex fails) sets
  the "codex" flag.
Then the model is told to stop (additionalContext), and guard.py refuses every retry in the turn; bind_session.py
clears the flags when the next turn starts. The human's own chat is never flagged.
"""
import json
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import guard  # noqa: E402


def text_of_result(data):
    """Everything the tool gave back, as one string (the error of a failure, the output of a success)."""
    parts = []
    for key in ("error", "tool_response", "tool_output", "output"):
        v = data.get(key)
        if v is not None:
            parts.append(v if isinstance(v, str) else json.dumps(v, ensure_ascii=False))
    return "\n".join(parts)


def judge(data, store):
    """(kind, why) of a failure that stops the work, or None; sets the flag."""
    session = data.get("session_id")
    if guard.role_of(guard.session_feature(session, store)) == "@human":
        return None
    tool = guard.unity_name(data.get("tool_name"))
    text = text_of_result(data)
    if tool.startswith(guard.UNITY_PREFIX):
        failed = data.get("hook_event_name") == "PostToolUseFailure" or '"success": false' in text or '"success":false' in text
        m = guard.UNITY_FAIL_RX.search(text) if failed else None
        if m:
            line = next((x for x in text.splitlines() if m.group(0) in x), text)
            why = " ".join(line.split())[:240]
            guard.set_fail(session, store, "unity", why)
            return "unity", why
        return None
    if tool == "Bash" and guard.CODEX_FAIL_MARK in text:
        line = text[text.index(guard.CODEX_FAIL_MARK):].split("\n", 1)[0]
        why = " ".join(line.split())[:240]
        guard.set_fail(session, store, "codex", why)
        return "codex", why
    return None


def main(argv):
    store = guard.STORE_DIR
    if len(argv) == 3 and argv[1] == "--store":      # test-only override
        store = argv[2]
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
        if not isinstance(data, dict):
            return 0
        found = judge(data, store)
        if found:
            kind, why = found
            out = {"hookSpecificOutput": {"hookEventName": data.get("hook_event_name") or "PostToolUse",
                                          "additionalContext": "guard: " + guard.fail_stop(kind, why)}}
            sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
        return 0
    except Exception as ex:      # never break a tool result over this
        sys.stderr.write(f"tool_fail: {ex}\n")
        return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
