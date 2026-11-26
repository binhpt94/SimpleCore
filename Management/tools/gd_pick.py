#!/usr/bin/env python3
"""Checks @DesignAdvisor's pick of one GPT answer and files it (người quyết 2026-11-27).

    python Management/tools/gd_pick.py <node> <id>

<node>  the advice folder: the node path with '.' for '/' (feature-hero), or _general.
<id>    the id of the question; its answer advice/<node>/<id>.md was written by gd_advise.py.

The pick file advice/<node>/<id>.pick.md (written by @DesignAdvisor):

    ## Giữ
    ### <id> | <target node | Node mới: <parent>/<key>> | <thêm | làm rõ <idea id> | thay <idea id>> | <short title>
    nguồn: <point id>, <point id>
    <one or two short sentences>

    ## Bỏ
    - <point id>: <a precise reason, naming what it repeats>

At most gamedesign.PICK_MAX kept ideas; every point of the answer appears once (as a source or left out); a target
is a node of round 2 or 3 in ai/idea (or a new one there). When the file is right the command writes
advice/<node>/<id>.pick.json: each kept idea with its route, fixed now (node: @GameDesigner writes it into the
target; side: the target is in live, the idea waits beside it for the human; new: @GameDesigner makes the node).
Nothing is written when something is wrong. Run it again after every change to the pick file.
Tests: python Management/tools/test_gd_pick.py
"""
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

ROOT = HERE.parent.parent
ROUTE_NAMES = {gd.ROUTE_NODE: "vào node (@GameDesigner viết)", gd.ROUTE_SIDE: "node phụ (người lọc trên trang)",
               gd.ROUTE_NEW: "node mới (@GameDesigner tạo)"}


def main(argv, root=ROOT):
    if len(argv) != 3 or not re.fullmatch(gd._FOLDER, argv[1]) or not gd.KEY_RX.match(argv[2]):
        print("LỖI: cách dùng: python Management/tools/gd_pick.py <node với '.' thay '/', hoặc _general> <id>")
        return 1
    state, errors = gd.file_pick(root, argv[1], argv[2])
    where = f"{gd.ADVICE_DIR}/{argv[1]}/{argv[2]}"
    if errors:
        print(f"LỖI: {where}.pick.md chưa dùng được, chưa ghi gì:")
        for e in errors:
            print(f"- {e}")
        return 1
    items = state["items"]
    print(f"Đã ghi {where}.pick.json: giữ {len(items)} ý, bỏ {len(state['dropped'])} góp ý.")
    for pid, rec in items.items():
        target = ("Node mới: " if rec.get("new") else "") + rec["target"]
        kind = rec["kind"] + (f" {rec['ref']}" if rec.get("ref") else "")
        done = f" · người đã {'chuyển lên' if rec['state'] == 'promoted' else 'xoá'}" if rec.get("state") else ""
        print(f"- {pid} → {target} ({kind}) · {ROUTE_NAMES.get(rec['route'], rec['route'])}{done}: {rec['title']}")
    by_route = {}
    for rec in items.values():
        by_route[rec["route"]] = by_route.get(rec["route"], 0) + 1
    if by_route.get(gd.ROUTE_NODE) or by_route.get(gd.ROUTE_NEW):
        print(f"@GameDesigner: viết các ý 'vào node' với nhãn by: <id ý>=gpt@{argv[1]}/{argv[2]}#<id ý giữ>; "
              f"node mới khai node-by: gpt@{argv[1]}/{argv[2]}#<id ý giữ>. Ý ở node phụ là của người: không viết vào node.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
