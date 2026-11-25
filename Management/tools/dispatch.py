#!/usr/bin/env python3
"""The inbox of the threads that run on demand: the features, services and @Core (pool "feature") and the tool
chats @Codex, @Blender, @ClaudeImage, @DesignAdvisor (pool "tool"). The Leaders and the testers stay live and are
messaged with SendMessage as before.

A message waits here until setup_server.py (its courier) gives it to the thread as a turn of its own: a thread that
is not running is started with the message as its prompt (claude --bg --resume), one running in the background and
idle is stopped and started again with it. A cross-session message is not used for work: a probe showed a session may
refuse work that comes that way, and the helper that sends it may say "sent" for a message that went nowhere.
A message counts as delivered only when its id is seen in the thread's transcript.

    Management/inbox/
      config.json       {"mode": "always-live" | "on-demand", "limits": {"feature": 4, "tool": 4}, "idle_minutes": 20}
      <id>.json         one message

    python Management/tools/dispatch.py send <target> --from <who> "<text>"
    python Management/tools/dispatch.py list [<target>]
    python Management/tools/dispatch.py cancel <id>

mode "always-live" (the default) keeps today's behaviour: the courier still delivers, but stops no thread and has no
limit. "on-demand" stops a thread that has been idle `idle_minutes` and keeps at most `limits` threads of each pool
running (the human's open chats count).
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

DIR = ("Management", "inbox")
CONFIG = "config.json"
MODES = ("always-live", "on-demand")
DEFAULT_CONFIG = {"mode": "always-live", "limits": {"feature": 4, "tool": 4}, "idle_minutes": 20}
LIMIT_MAX, IDLE_MIN, IDLE_MAX = 32, 5, 240
TOOLS = ("@Codex", "@Blender", "@ClaudeImage", "@DesignAdvisor")
NOT_TARGETS = ("@human", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@UnityAutoPlay")
FEATURE_RX = re.compile(r"^(_Services/)?[A-Z][A-Za-z0-9]*$")
SOURCES = ("người", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "gd_req", "studio")
ID_RX = re.compile(r"^m\d{8}-\d{6}-[0-9a-f]{6}$")
TEXT_MAX = 8000
STATES = ("queued", "waiting", "delivering", "delivered", "error", "cancelled")
OPEN = ("queued", "waiting", "delivering")      # not yet delivered: the courier still works on it
MAX_TRIES = 3
RETRY_SECONDS = (30, 120, 600)      # wait before the 2nd, 3rd... try
VERIFY_SECONDS = 90                 # a delivery not seen in the transcript by then failed


def folder(root):
    return Path(root).joinpath(*DIR)


def pool_of(target):
    """'tool' or 'feature' for a valid target; None for anything else (a Leader, the human, a bad name)."""
    if target in TOOLS:
        return "tool"
    if target in NOT_TARGETS:
        return None
    if target == "@Core" or FEATURE_RX.match(target or ""):
        return "feature"
    return None


def _write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + f".{os.getpid()}.tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    tmp.replace(path)


def _read(path):
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
        return value if isinstance(value, dict) else None
    except (OSError, ValueError):
        return None


LOCK_FILE = ".courier.lock"


def courier_lock(root):
    """Takes the lock that lets one courier work (two servers running would give a message twice); the open file
    while it holds it, None while another process does. Held until the process ends."""
    d = folder(root)
    d.mkdir(parents=True, exist_ok=True)
    f = open(d / LOCK_FILE, "a+")
    try:
        if os.name == "nt":
            import msvcrt
            f.seek(0)
            msvcrt.locking(f.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl
            fcntl.flock(f.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
    except OSError:
        f.close()
        return None
    return f


def courier_alive(root):
    """True while a setup_server.py courier holds the lock (it is delivering); False when none does: the lock is
    taken and let go at once. No server, no delivery: the callers say so and the work waits for the human."""
    f = courier_lock(root)
    if f is None:
        return True
    f.close()      # closing the file lets the lock go
    return False


SERVER_OFF = ("SERVER TẮT: tin {id} đã lưu trong hộp thư nhưng chưa ai giao (setup_server.py không chạy). Dừng việc phụ "
              "thuộc tin này và nhắn Manager BÁO CHẶN: chờ người bật python Management/tools/setup_server.py. Bật lại thì "
              "tin tự được giao, không gửi lại.")


def config(root):
    """The courier's settings, every value checked; a broken or missing file reads as the defaults."""
    raw = _read(folder(root) / CONFIG) or {}
    limits = raw.get("limits") if isinstance(raw.get("limits"), dict) else {}
    out = {"mode": raw.get("mode") if raw.get("mode") in MODES else DEFAULT_CONFIG["mode"], "limits": {},
           "idle_minutes": DEFAULT_CONFIG["idle_minutes"]}
    for pool, default in DEFAULT_CONFIG["limits"].items():
        n = limits.get(pool)
        out["limits"][pool] = n if isinstance(n, int) and not isinstance(n, bool) and 1 <= n <= LIMIT_MAX else default
    n = raw.get("idle_minutes")
    if isinstance(n, int) and not isinstance(n, bool) and IDLE_MIN <= n <= IDLE_MAX:
        out["idle_minutes"] = n
    return out


def set_config(root, mode=None, limits=None, idle_minutes=None):
    cfg = config(root)
    if mode is not None:
        if mode not in MODES:
            raise ValueError(f"chế độ phải là {' hoặc '.join(MODES)}")
        cfg["mode"] = mode
    for pool, n in (limits or {}).items():
        if pool not in cfg["limits"] or not isinstance(n, int) or isinstance(n, bool) or not 1 <= n <= LIMIT_MAX:
            raise ValueError(f"trần của {pool}: số nguyên 1-{LIMIT_MAX}")
        cfg["limits"][pool] = n
    if idle_minutes is not None:
        if not isinstance(idle_minutes, int) or isinstance(idle_minutes, bool) or not IDLE_MIN <= idle_minutes <= IDLE_MAX:
            raise ValueError(f"phút rảnh trước khi tắt: {IDLE_MIN}-{IDLE_MAX}")
        cfg["idle_minutes"] = idle_minutes
    _write(folder(root) / CONFIG, cfg)
    return cfg


def clean(m):
    """One message as the courier may trust it, or None."""
    if not isinstance(m, dict) or not ID_RX.match(str(m.get("id") or "")) or pool_of(m.get("target")) is None:
        return None
    if m.get("state") not in STATES or not isinstance(m.get("text"), str):
        return None
    return m


def messages(root, target=None):
    """Every message (oldest first), or those of one target."""
    d = folder(root)
    out = [clean(_read(p)) for p in sorted(d.glob("m*.json"))] if d.is_dir() else []
    return [m for m in out if m and (target is None or m["target"] == target)]


def pending(root):
    return [m for m in messages(root) if m["state"] in OPEN]


def save(root, m):
    _write(folder(root) / f"{m['id']}.json", m)
    return m


def get(root, mid):
    if not ID_RX.match(str(mid or "")):
        raise ValueError(f"không có tin '{mid}'")
    m = clean(_read(folder(root) / f"{mid}.json"))
    if m is None:
        raise ValueError(f"không có tin '{mid}'")
    return m


def send(root, target, text, source, key=None, check=None, now=None, once=False):
    """Puts one message in the inbox and returns it. A message with the same `key` that is not delivered yet is
    returned instead of a second one (an automatic message sent again, a command run twice). once: also when one
    with that key was ever delivered (an event that happens once: a new order, a delivered version); a cancelled or
    failed one does not count. The returned message has "new": False when it was there already."""
    target = (target or "").strip()
    pool = pool_of(target)
    if pool is None:
        raise ValueError(f"'{target}' không nhận tin qua hộp thư: chỉ feature, service, @Core và tool "
                         f"({', '.join(TOOLS)}); Leader và tester nhắn bằng SendMessage")
    if source not in SOURCES:
        raise ValueError(f"nguồn phải là một trong: {', '.join(SOURCES)}")
    text = (text or "").strip()
    if not text or len(text) > TEXT_MAX:
        raise ValueError(f"nội dung phải có chữ, tối đa {TEXT_MAX} kí tự")
    if key is not None:
        pool_ms = messages(root, target) if once else pending(root)
        same = next((m for m in pool_ms if m.get("key") == key and m["target"] == target
                     and m["state"] not in ("cancelled", "error")), None)
        if same:
            return dict(same, new=False)
    now = time.time() if now is None else now
    m = {"id": "m" + time.strftime("%Y%m%d-%H%M%S", time.localtime(now)) + "-" + secrets.token_hex(3),
         "target": target, "pool": pool, "text": text, "source": source, "key": key, "check": check,
         "state": "queued", "why": "", "tries": 0, "next_try": 0, "created": now, "started": 0,
         "offset": 0, "delivered_at": 0, "error": ""}
    return dict(save(root, m), new=True)


def notify(root, target, text, source, key, check=None, once=False):
    """send() for the commands that message a thread themselves (gd_req.py, studio.py): the line they print. A
    failure never breaks the command: it says the message did not go and to message by hand."""
    try:
        m = send(root, target, text, source, key=key, check=check, once=once)
    except (ValueError, OSError) as ex:
        return f"Chưa đưa được tin vào hộp thư của {target} ({ex}): nhắn {target} bằng tay (dispatch.py send hoặc tab Hộp thư)."
    if not m.get("new"):
        return f"Hộp thư của {target} đã có tin này ({m['id']}, {m['state']}): không gửi lại."
    if not courier_alive(root):
        return f"Đã đưa tin vào hộp thư của {target} ({m['id']}). " + SERVER_OFF.format(id=m["id"])
    return f"Đã đưa tin vào hộp thư của {target} ({m['id']}): server sẽ giao, không cần nhắn."


def mark(root, m, state, why="", **extra):
    m = dict(m, state=state, why=why, **extra)
    return save(root, m)


def failed(root, m, error, now=None):
    """One failed try: back to the queue after a wait, or an error after MAX_TRIES."""
    now = time.time() if now is None else now
    if m["tries"] >= MAX_TRIES:
        return mark(root, m, "error", error=str(error)[:300])
    wait = RETRY_SECONDS[min(m["tries"], len(RETRY_SECONDS)) - 1] if m["tries"] else 0
    return mark(root, m, "queued", error=str(error)[:300], next_try=now + wait)


def cancel(root, mid):
    m = get(root, mid)
    if m["state"] not in OPEN and m["state"] != "error":
        raise ValueError(f"tin '{mid}' đã {m['state']}, không huỷ được")
    return mark(root, m, "cancelled", why="huỷ")


def retry(root, mid):
    m = get(root, mid)
    if m["state"] not in ("error", "cancelled"):
        raise ValueError(f"tin '{mid}' đang {m['state']}: chỉ thử lại tin lỗi hoặc đã huỷ")
    return mark(root, m, "queued", tries=0, next_try=0, error="")


def prompt(ms):
    """The turn a thread gets for its messages: each one with its id, which is how delivery is checked."""
    head = ("Việc giao qua hộp thư của trang Setup (thread này chạy theo việc, không sống suốt). Đây là lượt việc "
            "thật, làm như người giao trong chat: làm theo luật của vai, kết thúc bằng báo cáo nếu có sửa file. Cần "
            "Leader thì nhắn Leader bằng SendMessage. Tin có mã đã làm rồi (giao lại sau sự cố) thì bỏ qua.")
    body = "\n\n".join(f"[tin {m['id']} · từ {m['source']}]\n{m['text']}" for m in ms)
    return head + "\n\n" + body


def summary(root, now=None):
    """{waiting, error, old}: counts for the line at the head of @Manager's and @CodeLead's messages."""
    now = time.time() if now is None else now
    ms = messages(root)
    open_ = [m for m in ms if m["state"] in OPEN]
    return {"open": len(open_), "error": sum(1 for m in ms if m["state"] == "error"),
            "old": sum(1 for m in open_ if now - m["created"] > 30 * 60)}


def line(m):
    when = time.strftime("%m-%d %H:%M", time.localtime(m["created"]))
    state = m["state"] + (f" ({m['why']})" if m.get("why") else "") + (f" · lỗi: {m['error']}" if m.get("error") else "")
    text = m["text"].replace("\n", " ")
    return f"- {m['id']} · {when} · {m['target']} · từ {m['source']} · {state}\n  {text[:160]}{'…' if len(text) > 160 else ''}"


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    try:
        if args[:1] == ["send"] and len(args) == 5 and args[2] == "--from":
            who = args[3] if args[3] == "người" else "@" + args[3].lstrip("@")
            m = send(root, args[1], args[4], who)
            if not courier_alive(root):
                print(SERVER_OFF.format(id=m["id"]), file=sys.stderr)
                return 3
            print(f"Đã đưa vào hộp thư: {m['id']} → {m['target']} ({m['state']}). Server sẽ giao trong vài giây; xem: "
                  "python Management/tools/dispatch.py list.")
            return 0
        if args[:1] == ["list"] and len(args) <= 2:
            ms = messages(root, args[1] if len(args) == 2 else None)
            shown = [m for m in ms if m["state"] in OPEN + ("error",)] + [m for m in ms if m["state"] not in OPEN + ("error",)][-10:]
            cfg = config(root)
            print(f"# Hộp thư · chế độ {cfg['mode']} · trần feature {cfg['limits']['feature']}, tool {cfg['limits']['tool']} · "
                  + ("server đang giao tin" if courier_alive(root) else "SERVER TẮT: không ai giao tin, báo người bật setup_server.py"))
            print("\n".join(line(m) for m in shown) or "(trống)")
            return 0
        if args[:1] == ["cancel"] and len(args) == 2:
            print("Đã huỷ " + cancel(root, args[1])["id"])
            return 0
    except ValueError as ex:
        print(f"Lỗi: {ex}", file=sys.stderr)
        return 1
    print(__doc__.split("\n\n")[3], file=sys.stderr)
    return 2


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
