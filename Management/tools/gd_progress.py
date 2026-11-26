#!/usr/bin/env python3
"""Marks the progress of one leaf node of the game design. For @CodeLead, after it compiled, tested and played.

    python Management/tools/gd_progress.py ai/idea/<leaf node> <idea|prototype|full> <todo|doing|done>
    e.g. python Management/tools/gd_progress.py ai/idea/feature-wave/spawning/spawn-timer idea done

guard.py lets only a @CodeLead session run this command; it is the one thing @CodeLead writes. The human sets
the same thing from the Game design tab. The path must be a leaf of the proposed tree (gamedesign.save_progress).
`done` is refused while the live leaf still has a requirement that is not verified or waived (gd_req.py, reqbook.py).
See what is open with: python Management/tools/gd_status.py
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
BY = "@CodeLead"


def mark(root, arg, phase, state):
    """Returns the message to print; raises ValueError when refused."""
    arg = arg.replace("\\", "/").rstrip("/")
    if arg != PREFIX and not arg.startswith(PREFIX + "/"):
        raise ValueError(f"đường dẫn phải có dạng {PREFIX}/<node lá>")
    path = arg[len(PREFIX) + 1:]
    gd.save_progress(root, path, phase, state, BY)
    return f"Đã ghi tiến độ {path or '(gốc)'}: {phase} = {state}"


def main(argv):
    root = HERE.parent.parent
    if len(argv) == 6 and argv[4] == "--root":      # test-only override
        root, argv = Path(argv[5]), argv[:4]
    if len(argv) != 4:
        print(f"Cách dùng: python Management/tools/gd_progress.py {PREFIX}/<node lá> <{'|'.join(gd.progress_phases())}> "
              f"<{'|'.join(gd.WORK_STATES)}>", file=sys.stderr)
        return 2
    try:
        print(mark(root, argv[1], argv[2], argv[3]))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không ghi: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
