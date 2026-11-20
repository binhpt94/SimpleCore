#!/usr/bin/env python3
"""Stop hook. An agent bound to a feature may not finish while its feature has boundary violations.

Human sessions (FEATURE=@human) and sessions without FEATURE are not checked.
The agent is sent back once; if it stops again with violations left, it is let go and the
violations stay visible to validate.py / the management page.
"""
import json
import os
import re
import subprocess
import sys
from pathlib import Path


def main(argv):
    try:
        feature = os.environ.get("FEATURE", "").strip().replace("\\", "/")
        if not re.fullmatch(r"(_Services/)?[A-Za-z][A-Za-z0-9]*", feature):
            return 0
        try:
            data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
        except ValueError:
            data = {}
        if isinstance(data, dict) and data.get("stop_hook_active"):
            return 0

        root = Path(__file__).resolve().parents[2]
        if len(argv) == 3 and argv[1] == "--root":      # test-only override
            root = Path(argv[2])
        p = subprocess.run([sys.executable, str(root / "Management" / "tools" / "validate.py"),
                            "--root", str(root), "--feature", feature],
                           capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=50)
        if p.returncode == 0:
            return 0

        text = (p.stdout + p.stderr).strip()
        if len(text) > 6000:
            text = text[:6000] + "\n... (truncated)"
        reason = (f"Boundary validator found problems in feature {feature}. Fix them inside your feature, or if "
                  f"that is not possible, explain to the human why and stop.\n\n{text}")
        sys.stdout.buffer.write(json.dumps({"decision": "block", "reason": reason}).encode("utf-8"))
        return 0
    except Exception:
        return 0      # a broken check must not trap the agent in a loop


if __name__ == "__main__":
    sys.exit(main(sys.argv))
