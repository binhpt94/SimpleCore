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
import os.path
import os
import re
import secrets
import sys
import time

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))      # `python -I` leaves the script folder out
import guard  # noqa: E402

MAX_FAILURES = 5
PIN_MIN = 4
ITERATIONS = 200_000
# Anything that starts like the command is handled here and never passed on: it may carry the PIN.
COMMAND_RX = re.compile(r"feature\s*(\?|=(.*))?", re.I)
CONTEXT_BLOCK_RX = re.compile(r"<([A-Za-z][\w-]*)\b[^>]*>.*?</\1>", re.S)
USAGE = ("Cách gõ: FEATURE=<tên feature, @Core, @Manager, @CodeLead, @GameDesigner, @UIDesigner, @UnityAutoPlay hoặc @human> <PIN>. "
         "Xem chat đang gán gì: FEATURE?")
MAX_LISTED_REPORTS = 20
# Role rules that are not loaded into every chat (their frontmatter `paths:` keeps them out; loading the rules of
# every role into all ~30 chats cost each call ~20k tokens). The chat of that role is told to Read its file once:
# after it is bound, and again after a compaction or /clear empties its context (SessionStart hook, --session-start).
RULES_DIR = ".claude/rules"
ROLE_RULES = {"@Manager": ("manager.md",), "@CodeLead": ("codelead.md",), "@UIDesigner": ("uidesigner.md",),
              **{r: ("tester.md",) for r in guard.TESTER_ROLES}, **{r: ("studio.md",) for r in guard.TOOL_ROLES}}
# The skills of each role (.claude/skills/<name>/SKILL.md): a task's procedure, loaded with the Skill tool only when the
# chat does that task, so it does not sit in the context of every turn. The guard line names them; the rule files point
# at them where the procedure used to be.
SKILLS_DIR = ".claude/skills"
GD_ROUNDS = ("gd-round-1", "gd-round-2", "gd-round-3", "gd-round-4")
ROLE_SKILLS = {"@Manager": ("manager-startup-check", "manager-dispatch"),
               "@CodeLead": ("codelead-scaffold-feature", "codelead-resolve-issue"),
               "@GameDesigner": GD_ROUNDS + ("gd-rework-node", "gd-integrate-advice"),
               "@DesignAdvisor": ("advisor-ask-gpt", "gd-integrate-advice") + GD_ROUNDS,
               "@UIDesigner": ("ui-layout-screen", "ui-frame-syntax", "ui-claude-concept-order", "gd-round-4"),
               **{r: ("studio-fulfill-order",) for r in guard.TOOL_ROLES},
               "@ClaudeImage": ("studio-fulfill-order", "studio-claude-concept"),
               **{r: ("tester-playtest-session",) for r in guard.TESTER_ROLES},
               "@Core": ("feature-request-framework-change", "feature-order-asset")}
FEATURE_SKILLS = ("feature-implement-req", "feature-order-asset", "feature-request-framework-change")      # Features, Services
RULES_READ_DIR = "rules-read"      # in the store: <session id>.json, {"role": ...} once the chat was told to read


def pin_hash(pin, salt):
    return hashlib.pbkdf2_hmac("sha256", pin.encode("utf-8"), salt, ITERATIONS).hex()


def write_json(path, value):
    os.makedirs(os.path.dirname(path), exist_ok=True)
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
    write_json(os.path.join(store, "pin.json"), {"salt": salt.hex(), "hash": pin_hash(pin, salt)})
    write_json(os.path.join(store, "failures.json"), 0)


def check_pin(store, pin):
    """None when the PIN is right, otherwise the message for the human."""
    saved = read_json(os.path.join(store, "pin.json"), None)
    if not isinstance(saved, dict) or not saved.get("salt") or not saved.get("hash"):
        return "Chưa đặt PIN. Chạy trong terminal của bạn: python .claude/hooks/bind_session.py --set-pin"
    failures_path = os.path.join(store, "failures.json")
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
    folder = os.path.join(root, "Management", "reports")
    try:
        names = sorted(n for n in os.listdir(folder) if n.endswith(".md"))
    except OSError:
        names = []
    seen_path = os.path.join(store, reader.strip("@").lower() + "-seen.json")
    seen = read_json(seen_path, [])
    seen = set(seen) if isinstance(seen, list) else set()
    new = [n for n in names if n not in seen]
    if not new:
        return " Không có báo cáo mới."
    lines = []
    for n in new[-MAX_LISTED_REPORTS:]:
        try:
            with open(os.path.join(folder, n), encoding="utf-8") as f:
                text = f.read()
        except OSError:
            continue
        lines.append(f"- {n}: {report_field(text, 'Phiên')} | {report_field(text, 'Trạng thái')} | "
                     f"{report_field(text, 'Việc')} | cần bên khác: {report_field(text, 'Cần bên khác')}")
    write_json(seen_path, sorted(seen | set(new)))
    more = f" (chỉ liệt kê {MAX_LISTED_REPORTS} cái mới nhất)" if len(new) > MAX_LISTED_REPORTS else ""
    return f" {len(new)} báo cáo mới{more}, đọc file để xem đủ:\n" + "\n".join(lines)


def editor_status(store):
    """Who holds the Unity Editor lock now (guard.py), in one sentence."""
    lock = guard.read_lock(store)
    if not lock:
        return " Editor Unity: đang rảnh."
    until = time.strftime("%H:%M", time.localtime(lock["until"]))
    return f" Editor Unity: đang do {lock.get('who') or 'một chat khác'} giữ tới {until}" + (
        f" (scene {lock['scene']})" if lock.get("scene") else "") + "."


def studio_pending(root, tool):
    """What waits for a tool chat (studio.py), in one sentence; with no tool, where @GameDesigner's art orders stand."""
    try:
        tools = os.path.join(root, "Management", "tools")
        if tools not in sys.path:
            sys.path.insert(0, tools)
        import studio
        return studio.pending_line(root, tool) if tool else studio.art_line(root)
    except Exception as ex:
        return f" Không đọc được danh sách order ({ex})."


def inbox_line(root, full=True):
    """The inbox of the chats that run on demand (dispatch.py), when it holds anything. Every Leader is told when
    messages wait and no server delivers them (setup_server.py is off); full: also the counts (@Manager, @CodeLead)."""
    try:
        tools = os.path.join(root, "Management", "tools")
        if tools not in sys.path:
            sys.path.insert(0, tools)
        import dispatch
        s = dispatch.summary(root)
        alive = dispatch.courier_alive(root) if s["open"] else True
    except Exception as ex:
        return f" Không đọc được hộp thư ({ex})."
    if not alive:
        return (f" SERVER TẮT: {s['open']} tin trong hộp thư chờ giao, feature và tool chạy theo việc không nhận được việc. "
                "Báo người bật python Management/tools/setup_server.py; tới khi bật không giao thêm việc cho feature, tool "
                "(dispatch.py send sẽ báo lỗi); việc phụ thuộc thì BÁO CHẶN.")
    if not full or not (s["open"] or s["error"]):
        return ""
    return (f" Hộp thư (feature, tool chạy theo việc): {s['open']} tin chưa giao" + (f", {s['old']} chờ quá 30 phút" if s["old"] else "")
            + (f", {s['error']} LỖI" if s["error"] else "") + ": python Management/tools/dispatch.py list.")


def code_gate(root, feature):
    """Whether this Features/Services chat may code now (the gate guard.py applies), in one sentence."""
    try:
        tools = os.path.join(root, "Management", "tools")
        if tools not in sys.path:
            sys.path.insert(0, tools)
        import gamedesign
        ok, reason = gamedesign.code_allowed(root, feature)
    except Exception as ex:
        ok, reason = False, f"không đọc được thiết kế ({ex})"
    if ok:
        return ("Thiết kế của feature này đã tới vòng 4 trong live nên được code; bám theo gói thiết kế: "
                f"python Management/tools/gd_pack.py {feature} (node feature, các node con, node nhánh khác mà refs "
                "trỏ tới, thuật ngữ). Lệnh in đường dẫn file gói và mục lục có số dòng: đọc bằng Read (offset, limit) "
                "hoặc Grep; trước khi code một phần, đọc đủ node vòng 4 và mục CHEAT của phần đó. " + req_brief(root, feature))
    return ("SỬA CODE ĐANG KHOÁ: " + reason + ". Đọc thì vẫn được. Dừng và báo người; thiết kế là việc của "
            "@GameDesigner, người chốt sang live.")


def req_brief(root, feature):
    """The one line of the feature's requirement book (Management/tools/reqbook.py): what is met, what it is doing.
    A chat just renewed learns from it where it left off."""
    try:
        import reqbook
        return reqbook.brief(root, feature)
    except Exception as ex:
        return f"Sổ yêu cầu: không đọc được ({ex}); chạy python Management/tools/gd_req.py {feature}."


def designer_mode(root):
    """What the mode in Management/game-design/mode.json asks of @GameDesigner; read on every prompt."""
    mode = read_json(os.path.join(root, "Management", "game-design", "mode.json"), None)
    if isinstance(mode, dict) and mode.get("mode") == "user":
        return ("Mode hiện tại: User control. Không tự quyết thay người: mọi điểm cần quyết ghi thành câu hỏi trong "
                ".ai-need-user-choose/questions.json của node đó; câu trả lời của người nằm ở answers.json cùng thư "
                "mục (chỉ đọc). Node chỉ đạt khi người chấm mọi ý từ 5 trở lên và hết câu hỏi; node con bị guard khoá "
                "cho tới khi node cha đạt và đã được người chốt sang live.")
    return ("Mode hiện tại: Full Auto. Tự phân tích và tự quyết, không cần hỏi người; điểm đã tự quyết ghi rõ trong "
            "CLAUDE.md của node. Node đạt khi mọi ý có điểm hiệu lực từ 5 trở lên (điểm của người nếu đã chấm, chưa thì "
            "điểm AI); người bấm Sync để chép node đạt sang live; node con bị guard khoá khi node cha chưa đạt. "
            "Người chấm được cả ý đã chốt trong live: điểm dưới 5 ở đó cũng bắt làm lại.")


def advice_mode(root):
    """How GPT's points are scored under the mode in mode.json. In both modes every point becomes an idea."""
    mode = read_json(os.path.join(root, "Management", "game-design", "mode.json"), None)
    if isinstance(mode, dict) and mode.get("mode") == "user":
        return ("Mode User control: ý của GPT do người chấm như mọi ý; @GameDesigner không tự chọn bên nào thắng, và chỉ "
                "loại một ý của GPT sau khi người chấm nó dưới 5.")
    return ("Mode Full Auto: @GameDesigner chấm điểm AI thật cho ý của GPT như cho ý của mình; chỉ ý dưới 5 mới bị viết lại "
            "hoặc loại, và phải khai.")


def context(data, root, store):
    """One line for the model on every prompt that gets through: what this chat is bound to.
    Read from the binding each time, so it stays right after a rebind or a compaction. Never has the PIN."""
    raw = guard.session_feature(data.get("session_id"), store).strip().replace("\\", "/")
    role = guard.role_of(raw)
    if role == "@human":
        return ("guard: chat này gán FEATURE=@human (phiên của người): không giới hạn feature; guard không hỏi ở "
                "vùng nào (cả Core, _Composition, .claude, CLAUDE.md gốc, thiết kế live, toàn bộ Management). "
                ".claude/settings.json chỉ còn hỏi khi sửa Packages, ProjectSettings.")
    worker = (" Khung (Scripts/Contracts, Scripts/Internal/Base, Scripts/View/Base, *.asmdef, AssemblyInfo.cs; ở "
              "Core/_Composition thêm các interface I*.cs và lớp base *Base.cs) chỉ đọc: viết code mới trên lớp con ở phần còn lại của "
              "Internal/View; muốn đổi khung thì nhắn @CodeLead (ngữ cảnh, file, đề xuất) và chờ. Xung đột code do "
              "@CodeLead giải quyết; lỗi @CodeLead báo về thì tự sửa. Lượt có sửa file phải kết thúc bằng báo cáo "
              "(gửi @CodeLead và @Manager) theo .claude/rules/report.md. unity-mcp: đọc (console, asset, scene), tự biên dịch "
              "(refresh_unity) rồi đọc console sửa lỗi của mình, tạo/sửa asset và prefab trong vùng mình; mở, sửa, lưu "
              "scene hay prefab stage thì guard giữ khoá Editor cho chat này tới hết lượt (lưu trước khi dừng); Play, "
              "test, cài đặt project là việc của @CodeLead; code chỉ viết bằng Edit/Write (bảng ở .claude/rules/unity.md)." + (
              " Cần ảnh hay model 3D, hoặc chấm hàng tool đã giao: skill feature-order-asset (giai đoạn IDEA: ảnh @ClaudeImage "
              "là đủ, chưa cần @Codex).")
              + editor_status(store))
    if role in guard.ROLE_AREAS:
        area = guard.ROLE_AREAS[role]
        return (f"guard: chat này gán FEATURE={role} (nhóm Basic). Chỉ sửa UnityProject/Assets/{area}/; đọc phần "
                "công khai của feature." + worker)
    if role == "@CodeLead":
        return ("guard: chat này gán FEATURE=@CodeLead (nhóm Leaders). Gặp lỗi không tự gỡ được hoặc phải dừng chờ người: "
                "trước khi kết thúc lượt nhắn Manager (SendMessage) một tin BÁO CHẶN theo khuôn ở .claude/rules/codelead.md, "
                "hết chặn thì nhắn HẾT CHẶN. Tester UnityAutoPlay giữ khoá Editor: chỉ nhắn nó khi cần giả lập một thao tác "
                "người chơi để xác nhận một tính năng (khuôn XÁC NHẬN ở codelead.md); test rộng hơn thì nhờ Manager. "
                "Việc của vai trò: kiểm code và giải quyết xung đột của nhóm "
                "Basic, Features, Services, và quản cấu trúc khung. Chỉ sửa khung: Scripts/Contracts, Scripts/Internal/"
                "Base, Scripts/View/Base, *.asmdef, AssemblyInfo.cs của feature đã mở khoá code; *.asmdef, "
                "AssemblyInfo.cs, interface I*.cs và lớp base *Base.cs của Core. Thêm toàn bộ UnityProject/Assets/_Composition (nối dây: Boot, "
                "scene, catalog asset; không có vai trò @Composition nữa). Ngoài hai thứ đó không sửa gì: lỗi thì gửi "
                "yêu cầu sửa cụ thể cho phiên sở hữu qua hộp thư (feature chạy theo việc): python Management/tools/dispatch.py "
                "send <F> --from CodeLead \"<lỗi, file, việc>\"; gd_req.py reject tự gửi. Lượt có sửa file phải kết thúc bằng báo cáo theo "
                ".claude/rules/report.md. Đọc được toàn bộ source các nhóm đó; unity-mcp để biên dịch, chạy test, Play, "
                "xem, dùng menu và cài đặt project (giữ khoá Editor tới hết lượt), xoá file khung; tạo, sửa, xoá asset "
                "và lưu scene hay prefab chỉ trong Assets/_Composition. "
                "Thứ còn lại được ghi là tiến độ của node thiết kế lá, sau khi đã biên dịch và test qua: python "
                "Management/tools/gd_progress.py ai/idea/<node lá> <idea|prototype|full> <todo|doing|done> (xem bằng "
                "python Management/tools/gd_status.py). gd_status.py và gd_pack.py in đường dẫn file và mục lục có số "
                "dòng khi bản in dài: đọc phần cần bằng Read (offset, limit) hoặc Grep. Luật ở .claude/rules/codelead.md; dựng khung "
                "theo skill codelead-scaffold-feature, xử lý vấn đề theo skill codelead-resolve-issue."
                + editor_status(store) + inbox_line(root) + unread_reports(root, store, role))
    if role in guard.TESTER_ROLES:
        name = guard.TESTER_ROLES[role]
        return (f"guard: chat này gán FEATURE={role} (nhóm Testers): chơi thử game trong Unity và ghi lại kết quả; KHÔNG "
                "sửa gì trong project (không code, asset, scene, cài đặt). Đọc được toàn bộ UnityProject/Assets (cả phần "
                f"riêng của mọi feature), Management và Tester (cả Archive). Chỉ ghi trong Tester/{name}/: README.md, và "
                "trong một đợt <yyyymmdd-hhmmss>/ thì README.md, ghi chú .md, images/**. Tạo đợt: python "
                f"Management/tools/tester.py new {name} \"<tên đợt>\"; kiểm cấu trúc: tester.py check; dời đợt cũ: "
                f"tester.py archive {name} <đợt> hoặc --keep <N>. unity-mcp: đọc, play/pause/stop, run_tests, manage_scene "
                "load (không save), refresh_unity, execute_menu_item, manage_camera, execute_code; các lệnh chơi giữ khoá "
                "Editor tới hết lượt, phiên khác đang giữ thì chờ. execute_code và menu là cổng mềm: chỉ để thao tác lúc "
                "Play và chụp ảnh vào images/ của đợt đang chạy, cấm dùng để ghi, sửa, xoá file hay asset. Lượt có ghi file "
                "kết thúc bằng khối '## Báo cáo cho @Manager'; bị chặn hoặc chờ người thì nhắn Manager tin BÁO CHẶN. Làm "
                "theo .claude/rules/tester.md; mỗi đợt test theo skill tester-playtest-session. Ai giao việc: đợt chơi thử chỉ nhận từ người hoặc Manager; CodeLead và "
                "GameDesigner chỉ được nhờ một yêu cầu hẹp theo khuôn XÁC NHẬN (một tính năng, thao tác cụ thể, kết quả "
                "mong đợi), rộng hơn thì bảo họ qua Manager; xong thì Stop Play ngay để trả khoá Editor." + editor_status(store))
    if role in guard.TOOL_ROLES:
        tool = guard.TOOL_ROLES[role]
        how = ("ảnh (kind image) và tham vấn GPT (kind consult). Gen ảnh: python Management/tools/studio.py codex "
               "<F>/<id> <item> (tự thử Codex ghi trực tiếp, rồi chỉ đọc ảnh Codex sinh ra); không được thì đi gián tiếp "
               "qua unity-mcp (execute_custom_tool, execute_menu_item). Tham vấn: studio.py consult <F>/<id> <item>."
               if tool == "codex" else
               "ảnh do chính mình vẽ (kind image cho feature, đủ dùng ở giai đoạn IDEA; kind art là claude-concept của một "
               "node thiết kế). Order của feature, mỗi item: viết hình thành SVG vào work/<item>.svg bằng Write (một file tự đủ: có viewBox cùng tỉ lệ "
               "với format của order, không script, không ảnh hay font ngoài file, chữ viết font-family Roboto; nền để trống khi order đòi trong suốt), "
               "rồi python Management/tools/studio.py claude <F>/<id> <item>: lệnh kiểm SVG, đổi ra work/out/<item>.png "
               "đúng kích thước, in số đo và TỰ GHI work/<item>.check.md (kích thước, đúng order): không phải mở ảnh ra xem, "
               "không ghi check.md bằng tay. Được giao việc cho tối đa 5 agent con chạy song song, mỗi agent đúng một frame "
               "hoặc một item và chỉ ghi file của nó (studio.py từ chối lần chạy thứ 6 và hai lần chạy cùng một item); phiên "
               "chính gom lại rồi deliver. Claude-concept (order kind art của node thiết kế, mỗi item một frame): KHÔNG vẽ ảnh cả màn, "
               "làm theo skill studio-claude-concept (vẽ từng phần theo frame, tool ghép; không ai chấm; vẽ lại sau khi giao cần "
               "studio.py redo của một Leader, ghép lại thì không). Không chạy công cụ nào khác, không dùng unity-mcp để gen."
               if tool == "claudeimage" else
               "model 3D (kind model). Viết script vào work/<item>.py rồi chạy headless: python Management/tools/studio.py "
               "blender <F>/<id> <item>; không được thì dùng blender MCP, rồi unity-mcp (execute_custom_tool, "
               "execute_menu_item). Kết quả luôn để ở work/out/<item>.<đuôi>.")
        return (f"guard: chat này gán FEATURE={role} (nhóm Tools): nhận order của các feature và làm {how} Không code, "
                f"không sửa Assets. Chỉ ghi Management/studio/{tool}/lessons.md và work/ của các order. Đầu lượt: python "
                f"Management/tools/studio.py status {tool}; đọc order.md và lessons.md trước khi gen. Phiên chạy theo việc: "
                f"order mới, item làm lại tới ở đầu tin (hộp thư, [tin <id> · từ ...]), không cần báo đã nhận; tin có mã đã "
                f"làm thì bỏ qua. Gen ĐÚNG số "
                f"lượng: mỗi item một kết quả, không tự gen thêm. Xong: studio.py deliver {tool} <F>/<id>: lệnh tự đưa tin "
                f"vào hộp thư của feature, không nhắn feature (guard chặn); order art (_design) thì nhắn Leader lệnh in ra. Lượt gen có trần: mỗi item 2 lượt đầu; sau khi giao chỉ "
                f"chạy lại item bị chấm 'Làm lại ngay' có phản hồi, 1 lượt mỗi lần, tối đa 2 lần ({'đọc phản hồi ở studio.py status rồi sửa SVG theo đó' if tool == 'claudeimage' else 'phản hồi tự vào prompt'}). Ghi tự kiểm "
                f"work/<item>.check.md (kích thước, nền trong suốt, % khung: đo bằng studio.py measure <F>/<id> <item>, "
                f"không ước bằng mắt); hết lượt thì pick bản gần nhất và giao với nhãn chưa chuẩn, không tìm đường vòng. "
                f"Phản hồi là một luật chung thì thêm một mục vào lessons.md. "
                f"Luật ở .claude/rules/studio.md; mỗi order làm theo skill studio-fulfill-order." + studio_pending(root, tool) + editor_status(store))
    if role == "@GameDesigner":
        return ("guard: chat này gán FEATURE=@GameDesigner (nhóm Leaders): thiết kế game, không code. Lượt có sửa file "
                "phải kết thúc bằng khối '## Báo cáo cho @Manager' (8 dòng, xem gamedesigner.md; Stop hook ép): @Manager "
                "dựa vào đó để biết việc đã xong và đang chờ ai. Gặp lỗi không tự gỡ được hoặc phải dừng chờ người (chấm, "
                "trả lời, chọn ý trái nhau, chốt), kể cả lượt không sửa file: trước khi kết thúc lượt nhắn Manager "
                "(SendMessage) một tin BÁO CHẶN theo khuôn ở gamedesigner.md, hết chặn thì nhắn HẾT CHẶN. Tester UnityAutoPlay "
                "giữ khoá Unity Editor: chỉ nhắn nó khi cần giả lập một thao tác người chơi để xác nhận một tính năng (khuôn "
                "XÁC NHẬN), chơi thử rộng hơn thì nhờ Manager. Chỉ đọc "
                "Management/game-design, .claude/rules/gamedesigner.md, designadvisor.md và skill gd-*; chỉ ghi trong Management/game-design/ai "
                "(cây đề xuất ở ai/idea); live là bảng đã chốt, chỉ người chốt. Xoá trong ai: python "
                "Management/tools/gd_delete.py ai/<đường dẫn>. Đầu lượt chạy python Management/tools/gd_status.py "
                "để biết node nào cần làm lại, điểm và phản hồi của người, hash cần ghi vào based-on (cả cây thì lệnh "
                "in đường dẫn file trong Management/game-design/.pack và mục lục có số dòng kèm Kết luận từng node: "
                "đọc phần cần bằng Read offset/limit hoặc Grep). Độ quan trọng "
                "(0-10, theo giai đoạn IDEA/PROTOTYPE/FULL) chỉ chấm ở node lá, ghi vào priority.json của node đó. "
                + designer_mode(root) +
                " GPT cùng thiết kế qua @DesignAdvisor, nhờ bằng hộp thư: python Management/tools/dispatch.py send @DesignAdvisor "
                "--from GameDesigner \"<node> · <hỏi gì>\" (nó chạy theo việc, SendMessage tới nó bị chặn; nó trả lời bạn bằng SendMessage): vòng 2 nhờ hỏi một câu chung về cả bộ feature trước, rồi "
                "từng feature (feature giao nhau thì hỏi kèm nhau); KHÔNG tự dặn GPT đừng thêm feature hay node mới (vòng 1-3 GPT được đề xuất, "
                "chỉ người mới đặt được rào); không chép thiết kế vào tin nhắn, lệnh tự gửi kèm ngữ cảnh. "
                "BẮT BUỘC hỏi GPT cho node gốc (vòng 1) và mọi node "
                "feature (vòng 2), ít nhất một lần mỗi node, node cũ chưa hỏi thì hỏi bù (gd_status.py: CHƯA HỎI GPT); nhắn node nào, hỏi gì; câu trả lời nằm ở "
                "Management/game-design/advice/<node>/<id>.md (chỉ đọc; gd_status.py liệt kê ở từng node). Ý của GPT NGANG HÀNG ý "
                "của bạn: MỖI góp ý phải thành một ý trong '## Ý cốt lõi' (thêm: ý mới; thay <ý>: ý mới đặt cạnh ý cũ, không xoá "
                "ý cũ; làm rõ <ý>: viết lại chính ý đó), chỉ được tóm gọn câu chữ, KHÔNG ĐƯỢC BỎ; khai nguồn ở phần đầu data.md: "
                "by: <id ý>=gpt@<id lần hỏi>#<id góp ý>. Loại một ý của GPT chỉ khi nó bị chấm dưới 5, và khai gpt-dropped: "
                "<id lần hỏi>#<id góp ý>=<id ý>. Node do GPT đề xuất: node-by: gpt@<id lần hỏi>. Mỗi ý một quyết định, không gộp dài. "
                + advice_mode(root) + " Đưa góp ý vào node theo skill gd-integrate-advice."
                " UI: ở node vòng 4 có giao diện bạn viết ui.md (phần tử: id, loại, chính/phụ, dùng để, trạng thái; tương "
                "tác; flow; xem skill gd-round-4); bố cục (frame .frame-concept, vị trí, kích thước) và order "
                "claude-concept là việc của @UIDesigner: viết hoặc đổi ui.md xong thì nhắn UIDesigner (to: \"UIDesigner\"). "
                "Bạn không ghi được frame. Art-concept (codex/orders/_design, tool: codex; check tự đưa vào hộp thư của @Codex; chỉ "
                "node cheat; cho PROTOTYPE/FULL) vẫn do bạn đặt."
                " Viết order xong và python "
                "Management/tools/studio.py check qua thì order tự vào hộp thư của phiên tool, không chờ duyệt, không nhắn. Art-concept: người chấm "
                "từng ảnh trên trang; trang chỉ lưu. Khi người bảo cho làm lại: chạy python Management/tools/studio.py status "
                "codex, giao @Codex các item 'Làm lại ngay' có phản hồi bằng python Management/tools/dispatch.py send @Codex --from "
                "GameDesigner \"Làm lại <order> · <item>: <phản hồi>\", báo người các item thiếu phản hồi; không tự chấm "
                "thay người. Claude-concept (ảnh vẽ từ frame) do @UIDesigner đặt và theo dõi." + studio_pending(root, None) +
                " Làm theo .claude/rules/gamedesigner.md; trước khi viết hay sửa node vòng N dùng skill gd-round-<N> (tool Skill)."
                + inbox_line(root, full=False))
    if role == "@UIDesigner":
        return ("guard: chat này gán FEATURE=@UIDesigner (nhóm Leaders): bố cục UI của thiết kế, không code. @GameDesigner "
                "liệt kê UI ở ui.md của node vòng 4 (phần tử, tương tác, flow); bạn quyết định vị trí, kích thước, nhóm, "
                "khoảng cách, vùng an toàn bằng frame. Ghi: CHỈ Management/game-design/ai/idea/<node>/.frame-concept/<tên>.frame "
                "(cùng cổng khoá node như @GameDesigner) và order.md mới của claude-concept trong "
                "Management/studio/claudeimage/orders/_design/<node, '/' thay bằng '.'>/<id>/; data.md, ui.md và mọi thứ khác "
                "không ghi được. Mỗi frame ghi ở phần đầu: 'ui: <hash ui.md>' (gd_status.py in hash), 'ai: <0-10>' (điểm của "
                "bạn) và vài dòng 'why: <lý do bố cục>'; id hình trùng id phần tử trong ui.md, đủ mọi phần tử chính, nút "
                "nào cũng có trong ui.md. Người chấm từng frame như chấm một ý; dưới 5 là bạn làm lại. ui.md thiếu hay mâu "
                "thuẫn thì nhắn GameDesigner, không tự thêm phần tử. Đầu lượt chạy python Management/tools/gd_status.py "
                "(dòng 'CHỜ @UIDesigner'), đọc Management/game-design/ai/ui-lessons.md (bài học từ phản hồi của người, bạn tự ghi "
                "thêm) và python Management/tools/references.py (ảnh tham chiếu bố cục, Read ảnh cùng tag với màn). Màn mới: 2 "
                "phương án (an toàn và táo bạo, bản sau ghi 'alt: <frame kia>'), người chọn. Viết hay sửa frame: dùng skill "
                "ui-frame-syntax (cú pháp, trang trí, các rect). Trước khi báo xong: python "
                "Management/tools/gd_frame.py ai/idea/<node> --png, Read ảnh, tự hỏi mắt rơi vào đâu, nút chính có nổi nhất, "
                "chỗ nào chật hay trống, nhóm có rõ; sửa 2-3 vòng; khoảng cách theo thang 8/16/24/40/64. Xoá frame: "
                "python Management/tools/gd_delete.py ai/idea/<node>/.frame-concept/<tên>.frame. Claude-concept: đặt cho mọi "
                "node vòng 4 có frame (mỗi frame một ảnh, node vòng 4 trước rồi mới tới cheat), python Management/tools/studio.py "
                "check (order tự vào hộp thư của @ClaudeImage, không nhắn); vẽ lại sau khi giao: python Management/tools/studio.py redo <ref> <frame> \"<lí do>\" (cũng tự gửi); "
                "việc khác cho @ClaudeImage: python Management/tools/dispatch.py send @ClaudeImage --from UIDesigner \"<việc>\". "
                "Lượt có sửa file kết thúc bằng khối '## Báo cáo cho @Manager' (8 dòng; Stop hook ép). Chờ người hay bị chặn: "
                "nhắn Manager BÁO CHẶN, hết thì HẾT CHẶN. Tester UnityAutoPlay chỉ nhắn để xác nhận một thao tác (khuôn XÁC NHẬN)."
                + studio_pending(root, None) + " Làm theo .claude/rules/uidesigner.md." + inbox_line(root, full=False))
    if role == "@DesignAdvisor":
        return ("guard: chat này gán FEATURE=@DesignAdvisor (nhóm Tools): đưa GPT (Codex CLI) vào thiết kế game, theo yêu cầu của "
                "người và @GameDesigner. Không code, không viết thiết kế. CHỈ nhận việc khi người hoặc @GameDesigner nhắn; "
                "không tự rà, phiên khác nhờ thì từ chối và bảo họ nhờ @GameDesigner. Đọc: Management/game-design (ai/idea là "
                "bảng đề xuất, live là bảng đã chốt), CLAUDE.md gốc, .claude/rules/designadvisor.md và gamedesigner*.md. Ghi: "
                "chỉ Management/game-design/advice/<node, '/' thay bằng '.'; node gốc _root; không gắn node _general>/<id>.q.md "
                "(câu hỏi gửi GPT) và <id>.note.md (nhận xét của mình). Hỏi GPT: viết <id>.q.md rồi chạy python "
                "Management/tools/gd_advise.py <node> <id>; lệnh tự gửi kèm ngữ cảnh (bản đồ feature, node cùng cha, node bên dưới, refs, ý "
                "người đã loại, góp ý cũ của GPT): đừng chép thiết kế vào câu hỏi; dòng đầu 'also: <node>, <node>' gửi đầy đủ tối đa 4 "
                "node liên quan; chỉ ghi vào câu hỏi rào do NGƯỜI yêu cầu, rào bên nhờ tự thêm (cấm thêm feature, node...) thì bỏ và báo lại: "
                "vòng 1-3 GPT được đề xuất feature, node mới; vòng 2 hỏi một câu chung ở _general trước rồi mới từng feature. Lệnh gửi kèm node với điểm NGƯỜI chấm và nghĩa của điểm (không gửi "
                "điểm AI, đừng chép điểm AI vào câu hỏi), ghi câu trả lời vào <id>.md: các góp ý rời, mỗi góp ý một ý đơn "
                "'### <id> | <thêm | làm rõ <id ý> | thay <id ý>> | <ý>'; không trần lượt. Xem thiết kế: python "
                "Management/tools/gd_status.py [ai/idea/<node>]. Xong thì nhắn bên hỏi (SendMessage, to: \"GameDesigner\") đường "
                "dẫn file. Ý của GPT ngang hàng ý của Claude: @GameDesigner phải đưa mọi góp ý vào node, không được bỏ; người "
                "chốt lên live. " + advice_mode(root) + " Làm theo .claude/rules/designadvisor.md; mỗi lần hỏi theo skill advisor-ask-gpt.")
    if role == "@Manager":
        return ("guard: chat này gán FEATURE=@Manager (nhóm Leaders): điều phối, không sửa code hay asset; chỉ tạo "
                "file mới trong Management/proposals; nhắn Leader và tester bằng SendMessage, giao feature, service, @Core và tool "
                "bằng hộp thư: python Management/tools/dispatch.py send <tên> --from Manager \"<việc>\" (chúng chạy theo việc, "
                "không có trong ListAgents khi rảnh là bình thường; xem dispatch.py list). Báo cáo của các phiên ở "
                "Management/reports/. Làm theo .claude/rules/manager.md. Lượt làm việc đầu tiên sau khi được tạo mới hoặc "
                "renew: kiểm tra đầu phiên (skill manager-startup-check) trước mọi việc khác, đúng thứ tự: (1) ListAgents, thiếu CodeLead, GameDesigner hoặc "
                "UIDesigner thì báo lỗi cho người và DỪNG; (2) tiến độ của ba Leader (gd_status.py, báo cáo, hỏi từng Leader); (3) rồi "
                "mới tới Tools, Basic, Services, Features; (4) tổng hợp cho người. Điều phối: một feature chỉ được code khi "
                "thiết kế của nó tới vòng 4 trong live (mục 'Cổng code theo feature' ở đầu bản in gd_status.py); feature ĐANG "
                "KHOÁ thì việc thuộc @GameDesigner và người, không giao code hay khung. Việc không phụ thuộc nhau thì nhắn các "
                "phiên CÙNG LÚC trong một lượt, không tuần tự từng phiên (skill manager-dispatch). Nhận tin BÁO CHẶN của một Leader: điều phối lại ngay "
                "(dừng việc phụ thuộc, giao việc không phụ thuộc, gom việc chờ người thành một danh sách cho người). Chỉ bạn và "
                "người giao đợt chơi thử cho tester UnityAutoPlay (nó giữ khoá Unity Editor): gom thành một đợt, không giao "
                "khi CodeLead đang Play hay test." + inbox_line(root) + unread_reports(root, store, role))
    if raw and guard.valid_feature(raw, root):
        group = "Services" if raw.startswith("_Services/") else "Features"
        return (f"guard: chat này gán FEATURE={raw} (nhóm {group}). Sửa được trong UnityProject/Assets/Features/{raw}/ "
                "theo bảng quyền ở CLAUDE.md gốc; không cần dò lại quyền. Phiên chạy theo việc: việc tới ở đầu tin (hộp thư, "
                "[tin <id> · từ ...]); tin có mã đã làm thì bỏ qua; cần Leader thì SendMessage. " + code_gate(root, raw) + worker)
    if raw:
        return f"guard: chat này gán FEATURE={raw}, giá trị không hợp lệ: mọi thao tác sửa bị chặn."
    return ("guard: chat này chưa gán FEATURE: mọi thao tác sửa bị chặn. Chỉ người gán được, "
            "bằng FEATURE=<tên> <PIN>; AI không gán và không hỏi PIN.")


def must_read(raw):
    """The role rule files (paths from the project root) a chat bound to `raw` reads itself, since they are not loaded."""
    return [f"{RULES_DIR}/{n}" for n in ROLE_RULES.get(guard.role_of(raw) or "", ())]


def rule_files(raw, root):
    """Every rule file a chat bound to `raw` follows: the root CLAUDE.md, the shared rules and those of its role.
    The Setup page's 'Đọc lại quy trình' only tells a chat about the ones of these that changed."""
    role = guard.role_of(raw)
    if not raw:
        return []
    names = ["report.md"] + list(ROLE_RULES.get(role or "", ()))
    if role in ("@Core", "@CodeLead") or role in guard.TESTER_ROLES or not role and guard.valid_feature(raw, root):
        names.append("unity.md")
    if role in ("@GameDesigner", "@DesignAdvisor"):
        names += ["gamedesigner.md", "designadvisor.md"]
    return ["CLAUDE.md"] + [f"{RULES_DIR}/{n}" for n in dict.fromkeys(names)] + skill_files(raw, root)


def role_skills(raw):
    """The names of the skills of the role a chat bound to `raw` has."""
    role = guard.role_of(raw)
    if not role:      # a feature or a service (an invalid value has every edit denied anyway)
        return list(FEATURE_SKILLS) if (raw or "").strip() else []
    return list(ROLE_SKILLS.get(role, ()))


def skill_files(raw, root):
    """The SKILL.md of each skill of the role (paths from the project root) that is there."""
    return [f"{SKILLS_DIR}/{n}/SKILL.md" for n in role_skills(raw)
            if os.path.isfile(os.path.join(root, *SKILLS_DIR.split("/"), n, "SKILL.md"))]


def skills_line(raw):
    """The part of the guard line that names the role's skills; '' for a role without any."""
    names = role_skills(raw)
    return (" Skill của vai (gọi bằng tool Skill khi làm đúng việc đó, không Read trước): " + ", ".join(names) + ".") if names else ""


def rules_read_path(session_id, store):
    path = guard.binding_path(session_id, store)
    return path and os.path.join(store, RULES_READ_DIR, os.path.basename(path))


def rules_reminder(session_id, raw, store):
    """The line that tells a chat to Read its role rules, once per binding and per emptied context; else ''."""
    files = must_read(raw)
    path = rules_read_path(session_id, store)
    if not files or not path or read_json(path, {}).get("role") == raw:
        return ""
    write_json(path, {"role": raw, "at": time.strftime("%Y-%m-%d %H:%M:%S")})
    return (" LUẬT VAI TRÒ KHÔNG TỰ NẠP vào ngữ cảnh: trước mọi việc khác, Read toàn bộ " + ", ".join(files) +
            " (một lần; ngữ cảnh bị compact hay clear thì dòng này nhắc lại). CLAUDE.md gốc đã nạp sẵn, không Read lại.")


def forget_rules_read(session_id, store):
    """SessionStart after a compaction or /clear: the chat no longer has its role rules in context."""
    path = rules_read_path(session_id, store)
    if path:
        try:
            os.remove(path)
        except OSError:
            pass


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
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    store = guard.STORE_DIR
    session_start = len(argv) > 1 and argv[1] == "--session-start"      # SessionStart hook (compact, clear)
    if session_start:
        argv = argv[:1] + argv[2:]
    if len(argv) == 5 and argv[1] == "--root" and argv[3] == "--store":      # test-only override
        root, store = os.path.abspath(argv[2]), argv[4]
    elif len(argv) == 2 and argv[1] == "--set-pin":
        return set_pin(store)
    raw = b""
    try:
        raw = sys.stdin.buffer.read()
        data = json.loads(raw.decode("utf-8-sig"))
        if not isinstance(data, dict):
            return 0
        if session_start:
            forget_rules_read(data.get("session_id"), store)
            return 0
        message = handle(data, root, store)
        if message:
            block(message)
        else:
            raw_feature = guard.session_feature(data.get("session_id"), store).strip().replace("\\", "/")
            add_context(context(data, root, store) + skills_line(raw_feature)
                        + rules_reminder(data.get("session_id"), raw_feature, store))
        return 0
    except BaseException as ex:
        # An ordinary prompt must not be lost to a broken hook, but one that may carry the PIN must not get through.
        if b"feature" in raw.lower():
            sys.stderr.write(f"guard: bind_session failed, prompt blocked: {ex}\n")
            return 2
        return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
