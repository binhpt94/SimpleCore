#!/usr/bin/env python3
"""Stop hook: frees the Unity Editor lock (guard.py, LOCK_FILE) when this chat holds it, so the next chat can use
the scene, play mode, tests or settings. Never blocks.

    python .claude/hooks/stop_unlock.py --release      the human frees the lock whoever holds it
"""
import json
import os.path
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import guard  # noqa: E402


def release_ticket(session, store, root=None):
    """The end of a turn gives back the Editor ticket (editor_ticket.py) this chat holds, once it used it (a chat
    granted in the middle of another turn keeps it for the turn the server's message opens)."""
    try:
        who = guard.session_feature(session, store).strip().replace("\\", "/")
        if not who:
            return False
        root = root or os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
        mod = guard.ticket_module()
        mine = [t for t in mod.holder(root) if t.get("session") == who and t.get("grant") in ("own", "fix") and t.get("used")]
        if mine:
            mod.release(root, who)
            return True
    except Exception:      # a turn left granted is given back by the server after a while
        pass
    return False


def main(argv):
    store = guard.STORE_DIR
    if len(argv) >= 3 and argv[-2] == "--store":      # test-only override
        store, argv = argv[-1], argv[:-2]
    if len(argv) == 2 and argv[1] == "--release":
        if hasattr(sys.stdout, "reconfigure"):      # the message is Vietnamese; a cp1252 console or pipe cannot encode it
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        lock = guard.read_lock(store)
        dropped = guard.release_lock(store)
        print(f"Đã gỡ khoá Editor của {lock.get('who')}." if dropped and lock else "Không có khoá Editor nào đang giữ.")
        return 0
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig") or "{}")
        session = data.get("session_id") if isinstance(data, dict) else None
        if isinstance(session, str):
            guard.release_lock(store, session)
            release_ticket(session, store)
    except Exception:      # a lock left behind expires by itself (guard.LOCK_SECONDS)
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
