#!/usr/bin/env python3
"""The bug list (người quyết 2026-11-27: một nơi lưu bug để sửa sau). A bug found in a play test, by a Leader or by the
human is written here and waits; nothing is started by writing it. It becomes a worker job only when the human says
so (the page's "Giao sửa" button, or CodeLead on the human's word through Manager): `fix` sends it to the feature's
inbox, which the server turns into a worker job.

    python Management/tools/bugs.py add "<bug: thấy gì, mong gì>" --feature <F>[,<F>...] --from <vai> [--seen "<ở đâu>"]
    python Management/tools/bugs.py list [--all] [--feature <F>]
    python Management/tools/bugs.py fix <id> [--from <vai>]           giao worker sửa (khi người bảo)
    python Management/tools/bugs.py done <id> "<đã sửa ra sao, việc worker nào>"
    python Management/tools/bugs.py drop <id> "<lí do>"

One file per bug, Management/bugs/<id>.json, written by this script (and the page, through it) only:
    {"id", "text", "features", "source", "seen", "state": open | fixing | fixed | dropped, "message", "note",
     "created", "closed"}
Exit codes: 0 ok, 2 refused (the reason is printed).
"""
import json
import os
import re
import secrets
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent.parent / ".claude" / "hooks"))

DIR = ("Management", "bugs")
SOURCES = ("người", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@UnityAutoPlay")
STATES = ("open", "fixing", "fixed", "dropped")
STATE_NAMES = {"open": "chờ sửa", "fixing": "đang sửa", "fixed": "đã sửa", "dropped": "đã bỏ"}
ID_RX = re.compile(r"^b[0-9]{8}-[0-9a-f]{4}$")
TEXT_MAX = 2000


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


def items(root, states=None, feature=None):
    """Every bug, oldest first; or those in some states, or of one feature."""
    d = folder(root)
    out = [_read(p) for p in sorted(d.glob("b*.json"))] if d.is_dir() else []
    return [x for x in out if x and (states is None or x["state"] in states) and (feature is None or feature in x["features"])]


def get(root, bug_id):
    bug = _read(folder(root) / f"{bug_id}.json") if ID_RX.match(str(bug_id or "")) else None
    if bug is None:
        raise ValueError(f"không có bug '{bug_id}'")
    return bug


def clean(text, what):
    text = " ".join(str(text or "").split())
    if not text or len(text) > TEXT_MAX:
        raise ValueError(f"{what} phải có chữ, tối đa {TEXT_MAX} kí tự")
    return text


def who(name):
    name = str(name or "").strip()
    return name if name == "người" else "@" + name.lstrip("@")


def features_of(root, value):
    """'Hud,HudUpgrade' -> ['Hud', 'HudUpgrade'], each a feature, a service (_Services/<Tên>) or @Core."""
    import guard
    names = [x.strip() for x in str(value or "").split(",") if x.strip()]
    if not names:
        raise ValueError("--feature phải có ít nhất một feature")
    for n in names:
        if n != "@Core" and not guard.valid_feature(n, str(root)):
            raise ValueError(f"'{n}' không phải feature, service hay @Core")
    return names


def add(root, text, features, source, seen="", now=None):
    """Writes one bug and returns it; the same text still waiting is returned instead of a second one."""
    if source not in SOURCES:
        raise ValueError(f"--from phải là một trong: {', '.join(s.lstrip('@') for s in SOURCES)}")
    text, names = clean(text, "bug"), features_of(root, features)
    same = next((x for x in items(root, ("open", "fixing")) if x["text"] == text), None)
    if same:
        return dict(same, new=False)
    now = time.time() if now is None else now
    bug = {"id": "b" + time.strftime("%Y%m%d", time.localtime(now)) + "-" + secrets.token_hex(2), "text": text,
           "features": names, "source": source, "seen": " ".join(str(seen or "").split())[:500], "state": "open",
           "message": "", "note": "", "created": now, "closed": 0}
    _write(folder(root) / f"{bug['id']}.json", bug)
    return dict(bug, new=True)


def fix(root, bug_id, source="người", now=None):
    """Sends the bug to its first feature's inbox (the server makes it a worker job) and marks it fixing."""
    import dispatch
    bug = get(root, bug_id)
    if bug["state"] != "open":
        raise ValueError(f"bug '{bug_id}' {STATE_NAMES[bug['state']]} rồi")
    others = [f for f in bug["features"][1:]]
    text = (f"Sửa bug {bug['id']}: {bug['text']}" + (f" · thấy ở: {bug['seen']}" if bug["seen"] else "") +
            (f" · bug chạm cả {', '.join(others)}: sửa ở feature nào đúng thì sửa ở đó" if others else "") +
            " · thêm test bắt được bug này")
    m = dispatch.send(root, bug["features"][0], text, source, now=now)
    bug.update(state="fixing", message=m["id"])
    _write(folder(root) / f"{bug['id']}.json", bug)
    return bug


def close(root, bug_id, state, note, now=None):
    if state not in ("fixed", "dropped"):
        raise ValueError("trạng thái phải là fixed hoặc dropped")
    bug = get(root, bug_id)
    if bug["state"] in ("fixed", "dropped"):
        raise ValueError(f"bug '{bug_id}' {STATE_NAMES[bug['state']]} rồi")
    bug.update(state=state, note=clean(note, "ghi chú"), closed=time.time() if now is None else now)
    _write(folder(root) / f"{bug['id']}.json", bug)
    return bug


def line(bug):
    when = time.strftime("%m-%d %H:%M", time.localtime(bug["created"]))
    out = (f"- {bug['id']} · {', '.join(bug['features'])} · {when} · từ {bug['source']} · {STATE_NAMES[bug['state']]}"
           f"\n  Bug: {bug['text']}")
    out += f"\n  Thấy ở: {bug['seen']}" if bug["seen"] else ""
    out += f"\n  Tin giao sửa: {bug['message']}" if bug["message"] else ""
    return out + (f"\n  Kết quả: {bug['note']}" if bug["note"] else "")


def main(argv):
    import argparse
    root = HERE.parent.parent
    ap = argparse.ArgumentParser(prog="bugs.py", add_help=False)
    ap.add_argument("command", nargs="?")
    ap.add_argument("args", nargs="*")
    ap.add_argument("--feature", default=None)
    ap.add_argument("--from", dest="source", default=None)
    ap.add_argument("--seen", default="")
    ap.add_argument("--all", action="store_true")
    try:
        a = ap.parse_args(argv[1:])
    except SystemExit:
        print(__doc__, file=sys.stderr)
        return 2
    try:
        if a.command == "add" and len(a.args) == 1 and a.feature and a.source:
            bug = add(root, a.args[0], a.feature, who(a.source), a.seen)
            print(f"Đã ghi bug {bug['id']}: chờ sửa, chưa giao ai." if bug["new"] else f"Bug này đã có ({bug['id']}): không ghi lại.")
            return 0
        if a.command == "list" and not a.args:
            shown = items(root, None if a.all else ("open", "fixing"), a.feature)
            print(f"# Bug ({'tất cả' if a.all else 'chờ sửa và đang sửa'}): {len(shown)}")
            for bug in shown:
                print(line(bug))
            return 0
        if a.command == "fix" and len(a.args) == 1:
            bug = fix(root, a.args[0], who(a.source or "người"))
            print(f"Đã giao sửa {bug['id']} cho {bug['features'][0]} (tin {bug['message']}): server làm thành việc worker.")
            return 0
        if a.command in ("done", "drop") and len(a.args) == 2:
            bug = close(root, a.args[0], "fixed" if a.command == "done" else "dropped", a.args[1])
            print(f"Đã ghi {bug['id']}: {STATE_NAMES[bug['state']]}.")
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
