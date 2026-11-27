#!/usr/bin/env python3
"""Editor tickets (người quyết 2026-11-27): who may use the shared Unity Editor next. A chat that needs the Editor
(load its draft, make assets, run its tests) while another uses it puts a ticket here and goes on with other work;
the server (setup_server.editor_tick) grants the Editor to one ticket at a time and tells that chat.

    python Management/tools/editor_ticket.py add --session <Tên> [--common refresh,clean,test] [--task "<việc>"]... [--req <node>#<id> --test <Lớp.Hàm>[,...]]...
    python Management/tools/editor_ticket.py done --session <Tên> <bit hay tên việc>...
    python Management/tools/editor_ticket.py drop --session <Tên> [<bit hay tên việc>...]
    python Management/tools/editor_ticket.py release --session <Tên>
    python Management/tools/editor_ticket.py list

The code: 32 bits. Bits 0..9 are common needs, met once for every ticket: 0 refresh (the newest code of every
feature loaded), 1 clean (the project compiles with no error), 2 test (the EditMode tests ran), 3..9 kept. Bits
10..31 are the ticket's own tasks, in the order given. The holder clears a bit when its task is done; when its turn
ends (release, or the Stop hook), the common bits it met are cleared on every waiting ticket too, and the tickets
that reach 0 are done.

One open ticket per session. While it waits, add puts more bits into it (it keeps its place); drop clears bits (a
ticket at 0 leaves the queue). While it is granted, new tasks go into a new ticket at the end. Order: first come,
first served; only the fix the server makes after a load that left compile errors goes first (priority).
A ticket of only common needs refresh/clean (nothing of its own) is "code only": the server loads every waiting
code-only ticket's draft together, in one turn (người quyết: a feature spread over several folders fails when loaded
one by one). The test bit is met by one EditMode run when no ticket needs the Editor for anything else.

Files: Management/editor-tickets/<id>.json, written by this script and the server only.
Exit codes: 0 ok, 2 refused.
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
DIR = ("Management", "editor-tickets")
COMMON = {"refresh": 0, "clean": 1, "test": 2}
COMMON_BITS = 10
TASK_BITS = 22
COMMON_MASK = (1 << COMMON_BITS) - 1
CODE_ONLY = (1 << COMMON["refresh"]) | (1 << COMMON["clean"])
STATES = ("waiting", "granted", "done", "dropped")
ID_RX = re.compile(r"^t\d{8}-\d{6}-[0-9a-f]{4}$")
SESSION_RX = re.compile(r"^(@[A-Za-z]+|(_Services/)?[A-Za-z][A-Za-z0-9]*)$")
REF_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*(/[a-z0-9]+(-[a-z0-9]+)*)*#[a-z0-9]+(-[a-z0-9]+)*$")
TEXT_MAX = 300


def folder(root):
    return Path(root).joinpath(*DIR)


def _write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + f".{os.getpid()}.tmp")
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    os.replace(tmp, path)


def _read(path):
    try:
        d = json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    return d if isinstance(d, dict) and ID_RX.match(str(d.get("id") or "")) and d.get("state") in STATES else None


def save(root, t):
    _write(folder(root) / f"{t['id']}.json", t)
    return t


def tickets(root, states=None):
    d = folder(root)
    out = [_read(p) for p in sorted(d.glob("t*.json"))] if d.is_dir() else []
    out = [t for t in out if t and (states is None or t["state"] in states)]
    return sorted(out, key=lambda t: (not t.get("priority"), t["created"], t["id"]))


def queue(root):
    """The waiting tickets, in the order they are served."""
    return tickets(root, ("waiting",))


def holder(root):
    """The granted tickets (one, or the tickets of one load together)."""
    return tickets(root, ("granted",))


def session_name(s):
    s = (s or "").strip().replace("\\", "/")
    if not SESSION_RX.match(s):
        raise ValueError(f"session '{s}' không hợp lệ (tên feature, _Services/<Tên>, hay vai có @)")
    return s


def text(t, what):
    t = " ".join(str(t or "").split())
    if not t or len(t) > TEXT_MAX:
        raise ValueError(f"{what} phải có chữ, tối đa {TEXT_MAX} kí tự")
    return t


def bit_of(t, key):
    """The bit a name (refresh, clean, test, a task's text) or a number stands for in ticket t."""
    key = str(key).strip()
    if key in COMMON:
        return COMMON[key]
    if key.isdigit():
        b = int(key)
        if 0 <= b < COMMON_BITS + TASK_BITS:
            return b
    for i, task in enumerate(t.get("tasks", [])):
        if task and task == key:
            return COMMON_BITS + i
    raise ValueError(f"không có việc '{key}' trên phiếu {t['id']}")


def new_id(now):
    return "t" + time.strftime("%Y%m%d-%H%M%S", time.localtime(now)) + "-" + secrets.token_hex(2)


def describe(t):
    common = [n for n, b in COMMON.items() if t["code"] >> b & 1]
    tasks = [task for i, task in enumerate(t.get("tasks", [])) if task and t["code"] >> (COMMON_BITS + i) & 1]
    return ", ".join(common + tasks) or "trống"


def add(root, session, common=(), tasks=(), reqs=(), now=None, priority=False, job=None):
    """Puts needs on the session's open ticket (or a new one). Returns the ticket. A worker's ticket (job: its worker.py
    id) names the feature of each requirement, since one job spans several features."""
    session = session_name(session)
    now = time.time() if now is None else now
    bits = 0
    for c in common:
        if c not in COMMON:
            raise ValueError(f"nhu cầu chung phải là: {', '.join(COMMON)}")
        bits |= 1 << COMMON[c]
    tasks = [text(x, "việc") for x in tasks]
    reqs = [{"ref": r["ref"], "tests": list(r.get("tests") or []), **({"feature": r["feature"]} if r.get("feature") else {})}
            for r in reqs]
    for r in reqs:
        if not REF_RX.match(r["ref"]):
            raise ValueError(f"yêu cầu phải có dạng <node>#<id>: '{r['ref']}'")
    if reqs:
        bits |= 1 << COMMON["test"]
    if not bits and not tasks:
        raise ValueError("phiếu cần ít nhất một nhu cầu (--common) hay một việc (--task)")
    open_ = next((t for t in queue(root) if t["session"] == session and t.get("job") == job), None)
    t = open_ or {"id": new_id(now), "session": session, "code": 0, "tasks": [], "reqs": [], "state": "waiting",
                  "created": now, "granted_at": 0, "priority": priority, "notes": []}
    for task in tasks:
        if task in t["tasks"]:
            i = t["tasks"].index(task)
        elif None in t["tasks"]:
            i = t["tasks"].index(None)
            t["tasks"][i] = task
        else:
            if len(t["tasks"]) >= TASK_BITS:
                raise ValueError(f"một phiếu nhiều nhất {TASK_BITS} việc riêng")
            t["tasks"].append(task)
            i = len(t["tasks"]) - 1
        bits |= 1 << (COMMON_BITS + i)
    known = {r["ref"]: r for r in t["reqs"]}
    for r in reqs:
        entry = known.setdefault(r["ref"], {"ref": r["ref"], "tests": []})
        entry["tests"] = sorted(set(entry["tests"]) | set(r["tests"]))
        if r.get("feature"):
            entry["feature"] = r["feature"]
    t["reqs"] = list(known.values())
    if job:
        t["job"] = job
    t["code"] |= bits
    t["new"] = open_ is None
    return save(root, t)


def find(root, session, states=("granted", "waiting"), job=None):
    """The session's open ticket (granted first). A worker's job (job: its id) has a ticket of its own."""
    session = session_name(session)
    mine = [t for t in tickets(root, states) if t["session"] == session and (job is None or t.get("job") == job)]
    if not mine:
        raise ValueError(f"{session} không có phiếu đang mở")
    return sorted(mine, key=lambda t: (t["state"] != "granted", t["created"]))[0]


def done(root, session, keys):
    """The holder (or a waiting ticket) clears bits; a waiting ticket at 0 is done."""
    t = find(root, session)
    for k in keys:
        b = bit_of(t, k)
        t["code"] &= ~(1 << b)
    if t["state"] == "waiting" and t["code"] == 0:
        t["state"] = "done"
    return save(root, t)


def drop(root, session, keys=None, job=None):
    t = find(root, session, ("waiting", "granted"), job)
    if not keys:
        t["code"] = 0
        t["state"] = "dropped" if t["state"] == "waiting" else t["state"]
        return save(root, t)
    for k in keys:
        b = bit_of(t, k)
        t["code"] &= ~(1 << b)
        if b >= COMMON_BITS:
            t["tasks"][b - COMMON_BITS] = None
    if t["state"] == "waiting" and t["code"] == 0:
        t["state"] = "dropped"
    return save(root, t)


def code_only(t):
    """Common needs only (refresh, clean), nothing of its own. A test bit beside them does not make it one's own: the
    load meets refresh and clean, and the test bit waits for the test run at the end of the round."""
    return bool(t["code"] & CODE_ONLY) and not t["code"] & ~(CODE_ONLY | 1 << COMMON["test"])


def needs_editor(t):
    """A ticket that needs a turn of its own or a load: anything but the test bit alone."""
    return bool(t["code"] & ~(1 << COMMON["test"]))


def grant_next(root, now=None):
    """When nobody holds the Editor: the next grant. Returns (kind, [tickets]) with kind 'fix' | 'load' | 'own' |
    'test', or (None, []). load: every waiting code-only ticket together; test: every ticket waiting for the test bit
    alone, once no ticket needs the Editor for anything else."""
    if holder(root):
        return None, []
    q = queue(root)
    if not q:
        return None, []
    now = time.time() if now is None else now
    head = next((t for t in q if needs_editor(t)), None)
    if head is None:
        group, kind = [t for t in q if t["code"] & (1 << COMMON["test"])], "test"
    elif head.get("priority"):
        group, kind = [head], "fix"
    elif code_only(head):
        group, kind = [t for t in q if code_only(t)], "load"
    else:
        group, kind = [head], "own"
    for t in group:
        t.update(state="granted", granted_at=now, grant=kind, granted_code=t["code"])
        save(root, t)
    return kind, group


def release(root, session=None, met=(), now=None, broken=None):
    """Ends the grant (of `session`, or the whole load / test run when session is None). `met`: common needs met in
    this turn (refresh, clean, test), in addition to the common bits the holders cleared themselves. Returns a dict:
    {"released": [tickets], "satisfied": [waiting tickets that reached 0], "requeued": [new tickets with what was
    left], "fix": the priority ticket made when the project was left with compile errors, or None}."""
    now = time.time() if now is None else now
    held = holder(root)
    if session is not None:
        session = session_name(session)
        held = [t for t in held if t["session"] == session]
    out = {"released": [], "satisfied": [], "requeued": [], "fix": None}
    if not held:
        return out
    met_bits = 0
    for m in met:
        met_bits |= 1 << COMMON[m]
    for t in held:      # the common bits a holder had at its grant and has now cleared were met for everyone
        met_bits |= (t.get("granted_code", 0) & ~t["code"]) & COMMON_MASK
    # Only a turn that used the Editor (or a load, released by the server) can have left compile errors: a grant given
    # back unused changed nothing, so no fix ticket for @CodeLead.
    left_broken = any(t["code"] >> COMMON["clean"] & 1 and t.get("granted_code", 0) >> COMMON["clean"] & 1
                      and (session is None or t.get("used")) for t in held) and not met_bits >> COMMON["clean"] & 1
    if broken is False:      # the caller knows nothing ran (the server's runner failed): no fix ticket
        left_broken = False
    for t in held:
        remaining = t["code"] & ~met_bits
        t.update(state="done", code=remaining, released_at=now)
        save(root, t)
        out["released"].append(t)
        rest = remaining & ~((1 << COMMON["clean"]) if left_broken else 0)
        waiting = next((w for w in queue(root) if w["session"] == t["session"] and w.get("job") == t.get("job")), None)
        if rest and waiting:      # one open ticket per session: what was left joins the one it already queued
            for i, task in enumerate(t.get("tasks", [])):
                if task and rest >> (COMMON_BITS + i) & 1:
                    rest &= ~(1 << (COMMON_BITS + i))
                    if task not in waiting["tasks"]:
                        waiting["tasks"].append(task)
                    waiting["code"] |= 1 << (COMMON_BITS + waiting["tasks"].index(task))
            waiting["code"] |= rest & COMMON_MASK
            waiting.setdefault("notes", []).append(f"thêm phần còn lại của {t['id']}")
            out["requeued"].append(save(root, waiting))
        elif rest:
            again = {"id": new_id(now), "session": t["session"], "code": rest, "tasks": list(t.get("tasks", [])),
                     "reqs": list(t.get("reqs", [])), "state": "waiting", "created": now, "granted_at": 0,
                     "priority": False, "notes": [f"phần còn lại của {t['id']}"], **({"job": t["job"]} if t.get("job") else {})}
            out["requeued"].append(save(root, again))
    if met_bits:
        for w in queue(root):
            before = w["code"]
            w["code"] &= ~met_bits
            if w["code"] != before:
                if w["code"] == 0:
                    w["state"] = "done"
                    out["satisfied"].append(w)
                save(root, w)
    if left_broken:
        fix = {"id": new_id(now), "session": "@CodeLead", "code": 1 << COMMON["clean"], "tasks": [], "reqs": [],
               "state": "waiting", "created": now, "granted_at": 0, "priority": True,
               "notes": ["project còn lỗi biên dịch sau lượt của " + ", ".join(t["session"] for t in held)]}
        out["fix"] = save(root, fix)
    return out


def summary_line(root):
    """One line for a guard line: who holds the Editor, what waits."""
    held, q = holder(root), queue(root)
    if not held and not q:
        return ""
    parts = []
    if held:
        parts.append("Editor: " + ", ".join(f"{t['session']} ({t.get('grant')}: {describe(t)})" for t in held))
    else:
        parts.append("Editor: trống")
    if q:
        parts.append("Hàng chờ: " + "; ".join(f"{t['session']}{' [ưu tiên]' if t.get('priority') else ''} ({describe(t)})" for t in q[:8])
                     + (f" … và {len(q) - 8} phiếu" if len(q) > 8 else ""))
    return " · ".join(parts)


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":
        root, args = Path(args[-1]), args[:-2]
    try:
        if args[:1] == ["list"] and len(args) == 1:
            print(summary_line(root) or "Editor: trống, không có phiếu chờ")
            for t in holder(root) + queue(root):
                print(f"- {t['id']} {t['state']} {t['session']}: {describe(t)}" + (f" · yêu cầu {', '.join(r['ref'] for r in t['reqs'])}" if t["reqs"] else ""))
            return 0
        cmd = args[0] if args else ""
        if len(args) >= 3 and args[1] == "--session":
            session, rest = args[2], args[3:]
            if cmd == "add":
                common, tasks, reqs = [], [], []
                it = iter(rest)
                for a in it:
                    v = next(it, None)
                    if v is None:
                        raise ValueError(f"{a} thiếu giá trị")
                    if a == "--common":
                        common += [x.strip() for x in v.split(",") if x.strip()]
                    elif a == "--task":
                        tasks.append(v)
                    elif a == "--req":
                        reqs.append({"ref": v, "tests": []})
                    elif a == "--test" and reqs:
                        reqs[-1]["tests"] += [x.strip() for x in v.split(",") if x.strip()]
                    else:
                        raise ValueError(f"tham số lạ: {a}")
                t = add(root, session, common, tasks, reqs)
                pos = [x["id"] for x in queue(root)].index(t["id"]) + 1 if t["state"] == "waiting" else 0
                print(f"Phiếu {t['id']} ({describe(t)}), thứ {pos} trong hàng. Làm việc khác trong lúc chờ; server nhắn khi tới lượt.")
                return 0
            if cmd == "done" and rest:
                t = done(root, session, rest)
                print(f"Phiếu {t['id']}: còn {describe(t)}")
                return 0
            if cmd == "drop":
                t = drop(root, session, rest)
                print(f"Phiếu {t['id']}: {t['state']}, còn {describe(t)}")
                return 0
            if cmd == "release" and not rest:
                r = release(root, session)
                print(f"Đã trả Editor ({len(r['released'])} phiếu)." if r["released"] else "Không giữ Editor.")
                return 0
    except ValueError as ex:
        print(f"editor_ticket: {ex}", file=sys.stderr)
        return 2
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
