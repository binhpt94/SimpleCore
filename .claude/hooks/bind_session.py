#!/usr/bin/env python3
"""UserPromptSubmit hook. Lets the human bind ONE chat to a feature by typing in that chat:

    FEATURE=<value> <PIN>       value is a feature (Wallet, _Services/Popup) or @human
    FEATURE?                    show what this chat is bound to

guard.py then reads the binding by session_id. It is the only source: the FEATURE environment
variable is not used, so a chat without a binding may edit nothing.
Every other prompt is let through with one line of context telling the model what the chat is bound to
(the binding command itself never reaches the model, so this is how it learns).

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
USAGE = ("Cách gõ: FEATURE=<tên feature, @Core, @Composition, @Manager, @CodeLead, @GameDesigner hoặc @human> <PIN>. "
         "Xem chat đang gán gì: FEATURE?")
MAX_LISTED_REPORTS = 20


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
    if guard.role_of(raw) or guard.valid_feature(raw, root):
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
    value = guard.canonical_role(value) or value
    if not guard.role_of(value) and not guard.valid_feature(value, root):
        return (f"Không gán: '{value}' không phải vai trò ({', '.join(guard.ROLES)}) "
                "và không phải thư mục feature đã có.")
    write_json(guard.binding_path(session_id, store),
               {"feature": value, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S")})
    return f"Đã gán chat này: FEATURE={value}."


def report_field(text, name):
    m = re.search(rf"^-\s*{name}:\s*(.*)$", text, re.M)
    return m.group(1).strip() if m else "?"


def unread_reports(root, store, reader):
    """Reports in Management/reports `reader` (@Manager or @CodeLead) has not been shown yet; marks them as shown.
    Each reader keeps its own list."""
    folder = ntpath.join(root, "Management", "reports")
    try:
        names = sorted(n for n in os.listdir(folder) if n.endswith(".md"))
    except OSError:
        names = []
    seen_path = ntpath.join(store, reader.strip("@").lower() + "-seen.json")
    seen = read_json(seen_path, [])
    seen = set(seen) if isinstance(seen, list) else set()
    new = [n for n in names if n not in seen]
    if not new:
        return " Không có báo cáo mới."
    lines = []
    for n in new[-MAX_LISTED_REPORTS:]:
        try:
            with open(ntpath.join(folder, n), encoding="utf-8") as f:
                text = f.read()
        except OSError:
            continue
        lines.append(f"- {n}: {report_field(text, 'Phiên')} | {report_field(text, 'Trạng thái')} | "
                     f"{report_field(text, 'Việc')} | cần bên khác: {report_field(text, 'Cần bên khác')}")
    write_json(seen_path, sorted(seen | set(new)))
    more = f" (chỉ liệt kê {MAX_LISTED_REPORTS} cái mới nhất)" if len(new) > MAX_LISTED_REPORTS else ""
    return f" {len(new)} báo cáo mới{more}, đọc file để xem đủ:\n" + "\n".join(lines)


def designer_mode(root):
    """What the mode in Management/game-design/mode.json asks of @GameDesigner; read on every prompt."""
    mode = read_json(ntpath.join(root, "Management", "game-design", "mode.json"), None)
    if isinstance(mode, dict) and mode.get("mode") == "user":
        return ("Mode hiện tại: User control. Không tự quyết thay người: mọi điểm cần quyết ghi thành câu hỏi trong "
                ".ai-need-user-choose/questions.json của node đó; câu trả lời của người nằm ở answers.json cùng thư "
                "mục (chỉ đọc). Node chỉ đạt khi người chấm mọi ý từ 5 trở lên và hết câu hỏi; node con bị guard khoá "
                "cho tới khi node cha đạt và đã được người chốt sang live.")
    return ("Mode hiện tại: Full Auto. Tự phân tích và tự quyết, không cần hỏi người; điểm đã tự quyết ghi rõ trong "
            "CLAUDE.md của node. Node đạt khi mọi ý có điểm hiệu lực từ 5 trở lên (điểm của người nếu đã chấm, chưa thì "
            "điểm AI); người bấm Sync để chép node đạt sang live; node con bị guard khoá khi node cha chưa đạt. "
            "Người chấm được cả ý đã chốt trong live: điểm dưới 5 ở đó cũng bắt làm lại.")


def context(data, root, store):
    """One line for the model on every prompt that gets through: what this chat is bound to.
    Read from the binding each time, so it stays right after a rebind or a compaction. Never has the PIN."""
    raw = guard.session_feature(data.get("session_id"), store).strip().replace("\\", "/")
    role = guard.role_of(raw)
    if role == "@human":
        return ("guard: chat này gán FEATURE=@human (phiên của người): không giới hạn feature; sửa Core, "
                "_Composition, .claude, CLAUDE.md gốc vẫn bị hỏi từng lần.")
    worker = (" Đổi Contracts, .asmdef, InternalsVisibleTo: thống nhất với @CodeLead trước (gửi ngữ cảnh, file, đề "
              "xuất). Xung đột code do @CodeLead giải quyết; lỗi @CodeLead báo về thì tự sửa. Lượt có sửa file phải "
              "kết thúc bằng báo cáo (gửi @CodeLead và @Manager) theo .claude/rules/report.md.")
    if role in guard.ROLE_AREAS:
        area = guard.ROLE_AREAS[role]
        return (f"guard: chat này gán FEATURE={role} (nhóm Basic). Chỉ sửa UnityProject/Assets/{area}/; đọc phần "
                "công khai của feature." + worker)
    if role == "@CodeLead":
        return ("guard: chat này gán FEATURE=@CodeLead (nhóm Leaders): kiểm code và giải quyết xung đột của nhóm "
                "Basic, Features, Services. Không sửa bất kỳ file nào; đọc được toàn bộ source các nhóm đó; unity-mcp "
                "chỉ để biên dịch, chạy test, Play và xem. Lỗi thì gửi yêu cầu sửa cụ thể cho phiên sở hữu. Quy trình "
                "ở .claude/rules/codelead.md." + unread_reports(root, store, role))
    if role == "@GameDesigner":
        return ("guard: chat này gán FEATURE=@GameDesigner (nhóm Leaders): thiết kế game, không code. Chỉ đọc "
                "Management/game-design và .claude/rules/gamedesigner.md; chỉ ghi trong Management/game-design/ai "
                "(cây đề xuất ở ai/idea); live là bảng đã chốt, chỉ người chốt. Xoá trong ai: python "
                "Management/tools/gd_delete.py ai/<đường dẫn>. Đầu lượt chạy python Management/tools/gd_status.py "
                "để biết node nào cần làm lại, điểm và phản hồi của người, hash cần ghi vào based-on. Độ quan trọng "
                "(0-10, theo giai đoạn IDEA/PROTOTYPE/FULL) chỉ chấm ở node lá, ghi vào priority.json của node đó. "
                + designer_mode(root) +
                " Làm theo .claude/rules/gamedesigner.md.")
    if role == "@Manager":
        return ("guard: chat này gán FEATURE=@Manager (nhóm Leaders): điều phối, không sửa code hay asset; chỉ tạo "
                "file mới trong Management/proposals và nhắn các phiên bằng SendMessage. Báo cáo của các phiên ở "
                "Management/reports/." + unread_reports(root, store, role))
    if raw and guard.valid_feature(raw, root):
        group = "Services" if raw.startswith("_Services/") else "Features"
        return (f"guard: chat này gán FEATURE={raw} (nhóm {group}). Sửa được trong UnityProject/Assets/Features/{raw}/ "
                "theo bảng quyền ở CLAUDE.md gốc; không cần dò lại quyền." + worker)
    if raw:
        return f"guard: chat này gán FEATURE={raw}, giá trị không hợp lệ: mọi thao tác sửa bị chặn."
    return ("guard: chat này chưa gán FEATURE: mọi thao tác sửa bị chặn. Chỉ người gán được, "
            "bằng FEATURE=<tên> <PIN>; AI không gán và không hỏi PIN.")


def add_context(text):
    out = {"hookSpecificOutput": {"hookEventName": "UserPromptSubmit", "additionalContext": text}}
    sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
    sys.stdout.buffer.flush()


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
        if not isinstance(data, dict):
            return 0
        message = handle(data, root, store)
        if message:
            block(message)
        else:
            add_context(context(data, root, store))
        return 0
    except BaseException as ex:
        # An ordinary prompt must not be lost to a broken hook, but one that may carry the PIN must not get through.
        if b"feature" in raw.lower():
            sys.stderr.write(f"guard: bind_session failed, prompt blocked: {ex}\n")
            return 2
        return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
