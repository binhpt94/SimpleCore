#!/usr/bin/env python3
"""UserPromptSubmit hook. Lets the human bind ONE chat to a feature by typing in that chat:

    FEATURE=<value> <PIN>       value is a feature (Wallet, _Services/Popup) or @human
    FEATURE?                    show what this chat is bound to

guard.py then reads the binding by session_id. It is the only source: the FEATURE environment
variable is not used, so a chat without a binding may edit nothing.

Why a PIN: this hook also runs for prompts the agent can cause (a subagent's report, a message from
another session, a scheduled task), and nothing in the input tells them apart from typed text. The
PIN is what proves the human typed it. Such a prompt is always blocked here, so it never reaches the
model; the PIN (as a salted hash) and the bindings live in guard.STORE_DIR, outside the project,
where no agent session may read. MAX_FAILURES wrong PINs lock binding until the PIN is set again.

Set or change the PIN, in your own terminal (not through the agent):
    python .claude/hooks/bind_session.py --set-pin
"""
import getpass
import hashlib
import hmac
import json
import ntpath
import os
import re
import secrets
import sys
import time

sys.dont_write_bytecode = True
sys.path.insert(0, ntpath.dirname(ntpath.abspath(__file__)))      # `python -I` leaves the script folder out
import guard  # noqa: E402

MAX_FAILURES = 5
PIN_MIN = 4
ITERATIONS = 200_000
# Anything that starts like the command is handled here and never passed on: it may carry the PIN.
COMMAND_RX = re.compile(r"feature\s*(\?|=(.*))?", re.I)
CONTEXT_BLOCK_RX = re.compile(r"<([A-Za-z][\w-]*)\b[^>]*>.*?</\1>", re.S)
USAGE = "Cách gõ: FEATURE=<tên feature hoặc @human> <PIN>. Xem chat đang gán gì: FEATURE?"


def pin_hash(pin, salt):
    return hashlib.pbkdf2_hmac("sha256", pin.encode("utf-8"), salt, ITERATIONS).hex()


def write_json(path, value):
    os.makedirs(ntpath.dirname(path), exist_ok=True)
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(value, f)
    os.replace(tmp, path)


def read_json(path, default):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return default


def write_pin(store, pin):
    """Stores the PIN as a salted hash and clears the wrong-PIN counter."""
    salt = secrets.token_bytes(16)
    write_json(ntpath.join(store, "pin.json"), {"salt": salt.hex(), "hash": pin_hash(pin, salt)})
    write_json(ntpath.join(store, "failures.json"), 0)


def check_pin(store, pin):
    """None when the PIN is right, otherwise the message for the human."""
    saved = read_json(ntpath.join(store, "pin.json"), None)
    if not isinstance(saved, dict) or not saved.get("salt") or not saved.get("hash"):
        return "Chưa đặt PIN. Chạy trong terminal của bạn: python .claude/hooks/bind_session.py --set-pin"
    failures_path = ntpath.join(store, "failures.json")
    failures = read_json(failures_path, 0)
    if not isinstance(failures, int) or failures >= MAX_FAILURES:
        return (f"Đã khoá vì sai PIN {MAX_FAILURES} lần. Đặt lại PIN trong terminal của bạn: "
                "python .claude/hooks/bind_session.py --set-pin")
    if hmac.compare_digest(pin_hash(pin, bytes.fromhex(saved["salt"])), saved["hash"]):
        if failures:
            write_json(failures_path, 0)
        return None
    write_json(failures_path, failures + 1)
    return f"Sai PIN. Còn {MAX_FAILURES - failures - 1} lần thử trước khi bị khoá."


def describe(session_id, root, store):
    raw = guard.session_feature(session_id, store).strip().replace("\\", "/")
    if raw == "@human" or guard.valid_feature(raw, root):
        return f"Chat này: FEATURE={raw}."
    if raw:
        return f"Chat này: FEATURE={raw} không hợp lệ, mọi thao tác sửa bị chặn."
    return "Chat này chưa gán FEATURE, mọi thao tác sửa bị chặn."


def find_command(prompt):
    """The command in a prompt, or None. The client may wrap what the human typed in context blocks
    (<ide_opened_file>...</ide_opened_file> and the like), so those are dropped and the command is
    looked for on the first and the last line that remain."""
    if not isinstance(prompt, str):
        return None
    lines = [line.strip() for line in CONTEXT_BLOCK_RX.sub("\n", prompt).splitlines() if line.strip()]
    for line in lines[:1] + lines[-1:]:
        m = COMMAND_RX.fullmatch(line)
        if m:
            return m
    return None


def handle(data, root, store):
    """None to let the prompt through, otherwise the message shown to the human (the prompt is blocked)."""
    m = find_command(data.get("prompt"))
    if not m:
        return None
    session_id = data.get("session_id")
    if not guard.binding_path(session_id, store):
        return "Không gán được: hook không nhận được session_id hợp lệ."
    if m.group(2) is None or not m.group(2).strip():
        return describe(session_id, root, store) + " " + USAGE
    parts = m.group(2).split()
    if len(parts) != 2:
        return "Không gán. " + USAGE
    value, pin = parts[0].replace("\\", "/"), parts[1]
    error = check_pin(store, pin)
    if error:
        return "Không gán. " + error
    if value != "@human" and not guard.valid_feature(value, root):
        return f"Không gán: '{value}' không phải @human và không phải thư mục feature đã có."
    write_json(guard.binding_path(session_id, store),
               {"feature": value, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S")})
    return f"Đã gán chat này: FEATURE={value}."


def block(message):
    out = {"decision": "block", "reason": "guard: " + message,
           "hookSpecificOutput": {"hookEventName": "UserPromptSubmit", "suppressOriginalPrompt": True}}
    sys.stdout.buffer.write(json.dumps(out).encode("utf-8"))
    sys.stdout.buffer.flush()


def set_pin(store):
    if not sys.stdin.isatty():
        print("Run this in your own terminal: the PIN is typed by hand.")
        return 1
    pin = getpass.getpass("PIN moi: ")
    if len(pin) < PIN_MIN or pin.split() != [pin]:
        print(f"PIN can it nhat {PIN_MIN} ky tu va khong co khoang trang.")
        return 1
    if getpass.getpass("Go lai PIN: ") != pin:
        print("Hai lan go khong giong nhau.")
        return 1
    write_pin(store, pin)
    print("Da luu PIN vao " + store)
    return 0


def main(argv):
    root = ntpath.dirname(ntpath.dirname(ntpath.dirname(ntpath.abspath(__file__))))
    store = guard.STORE_DIR
    if len(argv) == 5 and argv[1] == "--root" and argv[3] == "--store":      # test-only override
        root, store = ntpath.abspath(argv[2]), argv[4]
    elif len(argv) == 2 and argv[1] == "--set-pin":
        return set_pin(store)
    raw = b""
    try:
        raw = sys.stdin.buffer.read()
        data = json.loads(raw.decode("utf-8-sig"))
        message = handle(data, root, store) if isinstance(data, dict) else None
        if message:
            block(message)
        return 0
    except BaseException as ex:
        # An ordinary prompt must not be lost to a broken hook, but one that may carry the PIN must not get through.
        if b"feature" in raw.lower():
            sys.stderr.write(f"guard: bind_session failed, prompt blocked: {ex}\n")
            return 2
        return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
