#!/usr/bin/env python3
"""Deletes one file or one node (a whole folder) inside Management/game-design/ai. For @GameDesigner.

    python Management/tools/gd_delete.py ai/<path>          run from the project root
    e.g. ai/idea/feature-hero/skills

guard.py lets only a @GameDesigner session run this command. The script checks the path again by itself:
it must stay inside ai, may not be ai or a whole table, may not go through a link or junction, and may not be
something the human or the history code owns (answers.json, .user-review, .history).
Deleting a node keeps its history: every .history folder inside it is moved to the parent node's
.history/removed/, and the parent's log notes the removal.
"""
import os
import shutil
import stat
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign  # noqa: E402

WORK = gamedesign.WORK_DIR
KEPT = (gamedesign.REVIEW_DIR, gamedesign.HISTORY_DIR, gamedesign.ANSWERS_FILE)


def is_link(path):
    try:
        info = os.lstat(path)
    except OSError:
        return False
    return stat.S_ISLNK(info.st_mode) or bool(getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT)


def keep_history(node):
    """Moves every .history folder inside a node that is about to be deleted into the parent's .history/removed/."""
    parent = node.parent
    stamp = time.strftime("%Y%m%d-%H%M%S")
    for history in sorted(node.rglob(gamedesign.HISTORY_DIR)):
        if not history.is_dir() or gamedesign.HISTORY_DIR in history.relative_to(node).parts[:-1]:
            continue
        name = "-".join((node.name,) + history.parent.relative_to(node).parts) + "-" + stamp
        target = parent / gamedesign.HISTORY_DIR / "removed" / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(history), str(target))
    data = gamedesign.read_data(node)
    gamedesign.log(parent, "removed", key=node.name, title=data["title"] if data else "",
                   items=[{"id": it["id"], "text": it["text"]} for it in data["items"]] if data else [])


def delete(root, arg):
    """Returns the message to print; raises ValueError when the path is refused."""
    parts = arg.replace("\\", "/").split("/")
    if len(parts) < 2 or parts[0] != WORK or any(p in ("", ".", "..") or ":" in p for p in parts):
        raise ValueError(f"đường dẫn phải có dạng {WORK}/<...>, không có '..'")
    if tuple(parts) in set(gamedesign.TABLES.values()):
        raise ValueError("không xoá cả một bảng")
    if any(p in KEPT for p in parts):
        raise ValueError("điểm, câu trả lời của người và lịch sử (.user-review, answers.json, .history) không xoá được")
    work = Path(root).joinpath(*gamedesign.BASE, WORK)
    target = work.joinpath(*parts[1:])
    cur = work
    for p in [None] + parts[1:]:
        cur = cur / p if p else cur
        if is_link(cur):
            raise ValueError("đường dẫn đi qua link hoặc junction")
    if not os.path.lexists(target):
        raise ValueError("không có file hay thư mục này")
    if target.is_dir():
        idea = gamedesign.table_dir(root, "idea")
        if idea in target.parents and not any(p.startswith(".") for p in target.relative_to(idea).parts):
            keep_history(target)      # a node: its history moves to the parent
        shutil.rmtree(target)
        return f"Đã xoá thư mục {arg}"
    target.unlink()
    return f"Đã xoá file {arg}"


def main(argv):
    root = HERE.parent.parent
    if len(argv) == 4 and argv[2] == "--root":      # test-only override
        root, argv = Path(argv[3]), argv[:2]
    if len(argv) != 2:
        print(f"Cách dùng: python Management/tools/gd_delete.py {WORK}/<đường dẫn>", file=sys.stderr)
        return 2
    try:
        print(delete(root, argv[1]))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không xoá: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
