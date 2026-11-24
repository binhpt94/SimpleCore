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
    except Exception:      # a lock left behind expires by itself (guard.LOCK_SECONDS)
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
