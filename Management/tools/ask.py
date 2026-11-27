#!/usr/bin/env python3
"""The questions for the human (người quyết 2026-11-27): only @Manager asks the human. A Leader (@CodeLead,
@GameDesigner, @UIDesigner) that needs the human (a decision, a permission the guard asks for, a choice between
options, a question a lower tier sent it as BÁO CHẶN) writes it here, then messages @Manager one line
`HỎI NGƯỜI · <id> · <việc>`. The file keeps the question when the Manager's chat is closed: its next turn lists every
open one. The Manager asks the human (AskUserQuestion), writes the answer here, and messages the Leader
`TRẢ LỜI · <id> · …`.

    python Management/tools/ask.py add --from <Leader> "<việc>" --blocker "<vướng ở>" --options "<A …·B …>" --suggest "<đề nghị>" --waiting "<ai đang chờ>"
    python Management/tools/ask.py list [--all]
    python Management/tools/ask.py show <id>
    python Management/tools/ask.py answer <id> "<người quyết gì>" [--allow "<đường dẫn hay chữ của lệnh>" [--uses <n>]]   (@Manager only: guard)
    python Management/tools/ask.py cancel <id> "<lí do>"                                                                  (the Leader that asked, or @Manager)

--allow: the human allowed something the guard asks for (an edit of Packages, ProjectSettings, rules.json; deleting a
folder with files). The Leader that asked may then make that call `--uses` times (default 1) within ALLOW_MINUTES:
the guard lets a call of that Leader through when its file path or command holds the --allow text.

One file per question, Management/asks/<id>.json, written by this script only:
    {"id", "source", "job", "blocker", "options", "suggest", "waiting", "state": open | answered | cancelled,
     "answer", "allow", "allow_uses", "allow_until", "created", "closed"}
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
DIR = ("Management", "asks")
SOURCES = ("@CodeLead", "@GameDesigner", "@UIDesigner")
ANSWERER = "@Manager"
STATES = ("open", "answered", "cancelled")
STATE_NAMES = {"open": "chờ người", "answered": "đã trả lời", "cancelled": "đã huỷ"}
ID_RX = re.compile(r"^q[0-9]{8}-[0-9a-f]{4}$")
TEXT_MAX = 2000
ALLOW_MIN, ALLOW_MAX = 3, 500      # length of an --allow text: long enough not to match everything
ALLOW_MINUTES = 30
USES_MAX = 10
RECENT_SECONDS = 24 * 3600      # how long an answer is still shown to the Leader that asked


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
    tmp = path.with_name(path.name + f".{os.getpid()}.tmp")
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    os.replace(tmp, path)


def items(root, state=None):
    """Every question, oldest first; or those in one state."""
    d = folder(root)
    out = [_read(p) for p in sorted(d.glob("q*.json"))] if d.is_dir() else []
    return [x for x in out if x and (state is None or x["state"] == state)]


def get(root, ask_id):
    item = _read(folder(root) / f"{ask_id}.json") if ID_RX.match(str(ask_id or "")) else None
    if item is None:
        raise ValueError(f"không có câu hỏi '{ask_id}'")
    return item


def clean(text, what, empty=False):
    text = " ".join(str(text or "").split())
    if (not text and not empty) or len(text) > TEXT_MAX:
        raise ValueError(f"{what} phải có chữ, tối đa {TEXT_MAX} kí tự")
    return text


def role(name):
    name = str(name or "").strip()
    return name if name.startswith("@") else "@" + name


def add(root, source, job, blocker, options, suggest, waiting, now=None):
    """Puts one question in the list and returns it. The same job still open from the same Leader is returned instead."""
    source = role(source)
    if source not in SOURCES:
        raise ValueError(f"--from phải là một trong: {', '.join(s[1:] for s in SOURCES)} (chỉ Leader ghi câu hỏi; "
                         "tầng dưới báo chặn tới Leader của mình)")
    job = clean(job, "việc")
    fields = {"blocker": clean(blocker, "--blocker"), "options": clean(options, "--options"),
              "suggest": clean(suggest, "--suggest"), "waiting": clean(waiting, "--waiting")}
    same = next((x for x in items(root, "open") if x["source"] == source and x["job"] == job), None)
    if same:
        return dict(same, new=False)
    now = time.time() if now is None else now
    item = {"id": "q" + time.strftime("%Y%m%d", time.localtime(now)) + "-" + secrets.token_hex(2), "source": source,
            "job": job, **fields, "state": "open", "answer": "", "allow": "", "allow_uses": 0, "allow_until": 0,
            "created": now, "closed": 0}
    _write(folder(root) / f"{item['id']}.json", item)
    return dict(item, new=True)


def answer(root, ask_id, text, allow="", uses=1, now=None):
    """The human's answer, written by @Manager; with --allow, the guard's ask the human allowed."""
    item = get(root, ask_id)
    if item["state"] != "open":
        raise ValueError(f"câu hỏi '{ask_id}' {STATE_NAMES[item['state']]} rồi")
    now = time.time() if now is None else now
    allow = clean(allow, "--allow", empty=True)
    if allow and not ALLOW_MIN <= len(allow) <= ALLOW_MAX:
        raise ValueError(f"--allow dài {ALLOW_MIN}..{ALLOW_MAX} kí tự: đường dẫn hay chữ của đúng lệnh người cho phép")
    if not isinstance(uses, int) or not 1 <= uses <= USES_MAX:
        raise ValueError(f"--uses từ 1 tới {USES_MAX}")
    item.update(state="answered", answer=clean(text, "câu trả lời"), closed=now, allow=allow,
                allow_uses=uses if allow else 0, allow_until=now + ALLOW_MINUTES * 60 if allow else 0)
    _write(folder(root) / f"{item['id']}.json", item)
    return item


def cancel(root, ask_id, who, note, now=None):
    item = get(root, ask_id)
    who = role(who)
    if who not in (item["source"], ANSWERER):
        raise ValueError(f"chỉ {item['source']} (bên hỏi) hoặc Manager huỷ câu này")
    if item["state"] != "open":
        raise ValueError(f"câu hỏi '{ask_id}' {STATE_NAMES[item['state']]} rồi")
    item.update(state="cancelled", answer=clean(note, "lí do"), closed=time.time() if now is None else now)
    _write(folder(root) / f"{item['id']}.json", item)
    return item


def norm(text):
    return str(text or "").replace("\\", "/")


def allowance(root, source, text, now=None):
    """The answered question whose --allow lets `source` make a call whose paths and command are in `text` (still in
    time, uses left), or None. The guard calls it for an "ask" of a Leader; use() then spends one use."""
    now = time.time() if now is None else now
    text = norm(text)
    for item in items(root, "answered"):
        if (item["source"] == source and item.get("allow") and item.get("allow_uses", 0) > 0
                and now < item.get("allow_until", 0) and norm(item["allow"]) in text):
            return item
    return None


def use(root, item):
    item = dict(item, allow_uses=max(0, int(item.get("allow_uses", 0)) - 1))
    _write(folder(root) / f"{item['id']}.json", item)
    return item


def summary(root, now=None):
    """{"open": the open questions, "recent": the ones answered or cancelled within RECENT_SECONDS}."""
    now = time.time() if now is None else now
    all_ = items(root)
    return {"open": [x for x in all_ if x["state"] == "open"],
            "recent": [x for x in all_ if x["state"] != "open" and now - x.get("closed", 0) < RECENT_SECONDS]}


def line(item):
    when = time.strftime("%m-%d %H:%M", time.localtime(item["created"]))
    out = (f"- {item['id']} · {when} · từ {item['source']} · {STATE_NAMES[item['state']]}\n  Việc: {item['job']}\n"
           f"  Vướng ở: {item['blocker']}\n  Phương án: {item['options']}\n  Đề nghị: {item['suggest']}\n"
           f"  Ai đang chờ: {item['waiting']}")
    if item["answer"]:
        out += f"\n  {'Trả lời' if item['state'] == 'answered' else 'Lí do huỷ'}: {item['answer']}"
    if item.get("allow"):
        left = max(0, int((item["allow_until"] - time.time()) // 60))
        out += f"\n  Cho phép: \"{item['allow']}\" · còn {item['allow_uses']} lần · {left} phút"
    return out


def options(args, names):
    """{name: value} of `--name value` pairs; ValueError for anything else."""
    out = {}
    it = iter(args)
    for a in it:
        if a not in names or a in out:
            raise ValueError(f"tham số lạ hay trùng: {a}")
        try:
            out[a] = next(it)
        except StopIteration:
            raise ValueError(f"{a} thiếu giá trị") from None
    return out


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    try:
        if args[:2] == ["add", "--from"] and len(args) >= 4:
            o = options(args[4:], ("--blocker", "--options", "--suggest", "--waiting"))
            item = add(root, args[2], args[3], o.get("--blocker"), o.get("--options"), o.get("--suggest"), o.get("--waiting"))
            print(f"Đã ghi câu hỏi {item['id']}. Nhắn Manager (SendMessage): HỎI NGƯỜI · {item['id']} · {item['job'][:80]}"
                  if item["new"] else f"Câu này đang chờ người ({item['id']}): không ghi lại.")
            return 0
        if args[:1] == ["list"] and args[1:] in ([], ["--all"]):
            shown = items(root) if args[1:] else items(root, "open")
            print(f"# Câu hỏi cho người ({'tất cả' if args[1:] else 'đang chờ'}): {len(shown)}")
            for item in shown:
                print(line(item))
            return 0
        if args[:1] == ["show"] and len(args) == 2:
            print(line(get(root, args[1])))
            return 0
        if args[:1] == ["answer"] and len(args) >= 3:
            o = options(args[3:], ("--allow", "--uses"))
            if "--uses" in o and "--allow" not in o:
                raise ValueError("--uses chỉ đi cùng --allow")
            try:
                uses = int(o.get("--uses", "1"))
            except ValueError:
                raise ValueError("--uses là một số") from None
            item = answer(root, args[1], args[2], o.get("--allow", ""), uses)
            print(f"Đã ghi trả lời {item['id']}. Nhắn {item['source'][1:]} (SendMessage): TRẢ LỜI · {item['id']} · "
                  f"{item['job'][:60]} · {item['answer'][:120]}"
                  + (f". Cho phép \"{item['allow']}\" {item['allow_uses']} lần trong {ALLOW_MINUTES} phút." if item["allow"] else ""))
            return 0
        if args[:1] == ["cancel"] and len(args) == 3:
            # who cancels is checked by the guard (the Leader that asked, or @Manager), which knows the chat's role
            item = cancel(root, args[1], get(root, args[1])["source"], args[2])
            print(f"Đã huỷ câu hỏi {item['id']}.")
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
