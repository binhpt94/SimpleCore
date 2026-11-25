#!/usr/bin/env python3
"""The list of tool work: changes to the tools, the guard or the rules that the human decided on and that only the
human's own session (FEATURE=@human) can make.

It replaces Management/proposals (stopped 2026-11-26): a Leader no longer writes a proposal file and waits. It asks
the human in its chat; once the human has decided, work for a Leader, a feature or a tool is handed out as usual, and
work on the tools is put in this list, where the human's session finds it.

    python Management/tools/toolwork.py add "<việc>" --from <vai> --decided "<người quyết gì, ngày nào>"
    python Management/tools/toolwork.py list [--all]
    python Management/tools/toolwork.py done <id> "<đã làm gì, commit nào>"      (the human's session only: guard)
    python Management/tools/toolwork.py drop <id> "<lí do>"                       (the human's session only: guard)

One file per item, Management/toolwork/<id>.json, written by this script only:
    {"id", "text", "source", "decided", "state": open | done | dropped, "note", "created", "closed"}
Exit codes: 0 ok, 2 refused (the reason is printed).
"""
import json
import os
import re
import secrets
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
DIR = ("Management", "toolwork")
SOURCES = ("@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "người")
STATES = ("open", "done", "dropped")
STATE_NAMES = {"open": "chờ làm", "done": "đã làm", "dropped": "đã bỏ"}
ID_RX = re.compile(r"^w[0-9]{8}-[0-9a-f]{4}$")
TEXT_MAX = 2000
RECENT_SECONDS = 24 * 3600      # how long a closed item is still shown to the one who asked


def folder(root):
    return Path(root).joinpath(*DIR)


def _read(path):
    try:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
    except (OSError, ValueError):
        return None
    if not isinstance(data, dict) or not ID_RX.match(str(data.get("id") or "")) or data.get("state") not in STATES:
        return None
    return data


def _write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + ".tmp")
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    os.replace(tmp, path)


def items(root, state=None):
    """Every item, oldest first; or those in one state."""
    d = folder(root)
    out = [_read(p) for p in sorted(d.glob("w*.json"))] if d.is_dir() else []
    return [x for x in out if x and (state is None or x["state"] == state)]


def clean(text, what):
    text = " ".join(str(text or "").split())
    if not text or len(text) > TEXT_MAX:
        raise ValueError(f"{what} phải có chữ, tối đa {TEXT_MAX} kí tự")
    return text


def add(root, text, source, decided, now=None):
    """Puts one item in the list and returns it. The same text still open is returned instead of a second one."""
    if source not in SOURCES:
        raise ValueError(f"--from phải là một trong: {', '.join(s.lstrip('@') for s in SOURCES)}")
    text, decided = clean(text, "việc"), clean(decided, "quyết định của người (--decided)")
    same = next((x for x in items(root, "open") if x["text"] == text), None)
    if same:
        return dict(same, new=False)
    now = time.time() if now is None else now
    item = {"id": "w" + time.strftime("%Y%m%d", time.localtime(now)) + "-" + secrets.token_hex(2), "text": text,
            "source": source, "decided": decided, "state": "open", "note": "", "created": now, "closed": 0}
    _write(folder(root) / f"{item['id']}.json", item)
    return dict(item, new=True)


def close(root, item_id, state, note, now=None):
    """done or dropped, with what was done or why not."""
    if state not in ("done", "dropped"):
        raise ValueError("trạng thái phải là done hoặc dropped")
    item = _read(folder(root) / f"{item_id}.json") if ID_RX.match(str(item_id or "")) else None
    if item is None:
        raise ValueError(f"không có việc '{item_id}'")
    if item["state"] != "open":
        raise ValueError(f"việc '{item_id}' {STATE_NAMES[item['state']]} rồi")
    item.update(state=state, note=clean(note, "ghi chú"), closed=time.time() if now is None else now)
    _write(folder(root) / f"{item['id']}.json", item)
    return item


def summary(root, now=None):
    """{"open": the open items, "recent": the ones closed within RECENT_SECONDS} for the head lines of the chats."""
    now = time.time() if now is None else now
    all_ = items(root)
    return {"open": [x for x in all_ if x["state"] == "open"],
            "recent": [x for x in all_ if x["state"] != "open" and now - x.get("closed", 0) < RECENT_SECONDS]}


def line(item):
    when = time.strftime("%m-%d %H:%M", time.localtime(item["created"]))
    out = f"- {item['id']} · {when} · từ {item['source']} · {STATE_NAMES[item['state']]}\n  Việc: {item['text']}\n  Người quyết: {item['decided']}"
    return out + (f"\n  Kết quả: {item['note']}" if item["note"] else "")


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    try:
        if args[:1] == ["add"] and len(args) == 6 and args[2] == "--from" and args[4] == "--decided":
            who = args[3] if args[3] == "người" else "@" + args[3].lstrip("@")
            item = add(root, args[1], who, args[5])
            print(f"Đã ghi việc công cụ {item['id']}: phiên @human sẽ làm." if item["new"]
                  else f"Việc này đã có trong danh sách ({item['id']}): không ghi lại.")
            return 0
        if args[:1] == ["list"] and args[1:] in ([], ["--all"]):
            shown = items(root) if args[1:] else items(root, "open")
            print(f"# Việc công cụ ({'tất cả' if args[1:] else 'đang chờ làm'}): {len(shown)}")
            for item in shown:
                print(line(item))
            return 0
        if args[:1] in (["done"], ["drop"]) and len(args) == 3:
            item = close(root, args[1], "done" if args[0] == "done" else "dropped", args[2])
            print(f"Đã ghi {item['id']}: {STATE_NAMES[item['state']]}.")
            return 0
    except ValueError as ex:
        print(f"Không ghi: {ex}", file=sys.stderr)
        return 2
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
