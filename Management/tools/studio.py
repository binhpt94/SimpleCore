#!/usr/bin/env python3
"""Orders from the features to the tool chats (@Codex: images and GPT consults; @Blender: 3D models), and the one
way Codex and Blender are run. Every run, delivery and refusal is kept, so a mistake is learnt once.

    python Management/tools/studio.py check                          every order is well formed (any chat)
    python Management/tools/studio.py status [codex|blender|<F>/<id>] what waits, what is done (any chat)
    python Management/tools/studio.py codex <F>/<id> <item>           @Codex: one image for one item
    python Management/tools/studio.py consult <F>/<id> <item>         @Codex: ask GPT one question
    python Management/tools/studio.py blender <F>/<id> <item>         @Blender: run work/<item>.py headless
    python Management/tools/studio.py deliver <codex|blender> <F>/<id>  the tool chat: hand the order over
    python Management/tools/studio.py take <F>/<id>                   the feature: copy the delivery into its assets
    python Management/tools/studio.py reject <F>/<id>                 the feature: send it back (after feedback-<n>.md)

Layout (see .claude/rules/studio.md):
    Management/studio/<tool>/lessons.md                    mistakes met and how to avoid them; put before every prompt
    Management/studio/<tool>/orders/<F>/<id>/order.md      the order (the feature writes it once)
                                          feedback-<n>.md  the feature's answer to a delivery (written once)
                                          status.json      state, version, feedback taken (this code only)
                                          log.jsonl        every run and delivery (this code only)
                                          refs/            copies of the order's references (this code only)
                                          work/            prompts, Blender scripts; work/out/<item>.<ext> the results
    Resources/<F>/<tool>/<id>/v<n>/<item>.<ext>            every delivery, never overwritten
<F> is the code folder of the feature that ordered (Hero, _Services/Popup); <id> and <item>: lower case, digits, '-'.
Exactly one result per item: a run that makes more takes none of them. A rejected order is run again only after
lessons.md names its feedback file. Standard library only; Windows and Linux.
"""
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent

TOOLS = {"codex": ("image", "consult"), "blender": ("model",)}
DELIVER_TO = ("Private", "Public")
STUDIO = ("Management", "studio")
RESOURCES = "Resources"
LESSONS = "lessons.md"
ORDER_FILE, STATUS_FILE, LOG_FILE = "order.md", "status.json", "log.jsonl"
FEEDBACK_RX = re.compile(r"^feedback-(\d+)\.md$")
ID_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
FEATURE_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
ITEM_LINE_RX = re.compile(r"^\s*-\s*`?([a-z0-9]+(?:-[a-z0-9]+)*)`?\s*:\s*(.*\S)\s*$")
SECTIONS = ("Mô tả", "Danh sách", "Tiêu chí nhận")
FIELDS = ("tool", "kind", "count", "deliver", "format")
MAX_COUNT = 50
IMAGE_EXT = (".png", ".jpg", ".jpeg", ".webp")
RUN_LOCK_HOURS = 2
ONE_IMAGE = ("BẮT BUỘC: tạo đúng MỘT ảnh cho đúng một mục ở trên, không tạo thêm ảnh nào khác (không bản nháp, "
             "không biến thể, không ảnh xem trước). Lưu ảnh đó thành out/{item}.png trong thư mục làm việc.")
TIMEOUT = int(os.environ.get("STUDIO_TIMEOUT") or 1800)


def now():
    return time.strftime("%Y-%m-%d %H:%M:%S")


def read_text(path):
    return Path(path).read_text(encoding="utf-8-sig", errors="replace").replace("\r", "")


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def studio_dir(root, tool=None):
    base = Path(root).joinpath(*STUDIO)
    return base / tool if tool else base


# ---------------------------------------------------------------- orders
def split_ref(ref):
    """(feature, id) of '<F>/<id>'; raises ValueError."""
    feature, _, oid = (ref or "").replace("\\", "/").strip("/").rpartition("/")
    if not FEATURE_RX.match(feature) or not ID_RX.match(oid):
        raise ValueError(f"order phải ghi dạng <Feature>/<id>, ví dụ Hero/hero-icons (nhận được '{ref}')")
    return feature, oid


def parse_order(text):
    """{fields, title, sections, items: [(id, text)], errors} of an order.md."""
    errors, fields = [], {}
    text = text.strip("\n")
    if text.startswith("---\n"):
        head, sep, rest = text[4:].partition("\n---")
        if not sep:
            errors.append("phần đầu '---' chưa đóng")
            rest = ""
        for line in head.split("\n"):
            key, colon, value = line.partition(":")
            if colon:
                fields[key.strip()] = value.strip()
        text = rest.lstrip("-").lstrip("\n")
    else:
        errors.append("thiếu phần đầu '---' (tool, kind, count, deliver, format)")
    lines = text.split("\n")
    title = lines[0][2:].strip() if lines and lines[0].startswith("# ") else ""
    if not title:
        errors.append("thiếu dòng tiêu đề '# <tên order>'")
    sections, current = {}, None
    for line in lines[1:]:
        if line.startswith("## "):
            current = line[3:].strip()
            sections.setdefault(current, [])
        elif current is not None:
            sections[current].append(line)
    for name in SECTIONS:
        if not "\n".join(sections.get(name, [])).strip():
            errors.append(f"thiếu mục '## {name}' hoặc mục đó trống")
    items, seen = [], set()
    for line in sections.get("Danh sách", []):
        if not line.strip():
            continue
        m = ITEM_LINE_RX.match(line)
        if not m:
            errors.append(f"dòng trong Danh sách phải có dạng '- <item-id>: <thứ cần làm>': {line.strip()[:50]}")
            continue
        if m.group(1) in seen:
            errors.append(f"item '{m.group(1)}' trùng")
            continue
        seen.add(m.group(1))
        items.append((m.group(1), m.group(2)))
    for key in FIELDS:
        if not fields.get(key):
            errors.append(f"thiếu '{key}:' ở phần đầu")
    tool, kind = fields.get("tool", ""), fields.get("kind", "")
    if tool and tool not in TOOLS:
        errors.append(f"tool phải là một trong {', '.join(TOOLS)}")
    elif tool and kind and kind not in TOOLS[tool]:
        errors.append(f"{tool} nhận kind {' hoặc '.join(TOOLS[tool])}, không nhận '{kind}'")
    count = fields.get("count", "")
    if count and (not count.isdigit() or not 1 <= int(count) <= MAX_COUNT):
        errors.append(f"count phải là số nguyên từ 1 tới {MAX_COUNT}")
    elif count and int(count) != len(items):
        errors.append(f"count là {count} nhưng Danh sách có {len(items)} dòng: phải bằng nhau")
    if fields.get("deliver") and fields["deliver"] not in DELIVER_TO:
        errors.append("deliver phải là Private hoặc Public")
    refs = [r.strip().strip("`") for r in re.split(r"[,\s]+", fields.get("references", "")) if r.strip()]
    return {"fields": fields, "title": title, "sections": sections, "items": items, "refs": refs, "errors": errors}


def order_dir(root, tool, feature, oid):
    return studio_dir(root, tool) / "orders" / Path(*feature.split("/")) / oid


def find_order(root, ref, tool=None):
    """(tool, feature, id, folder) of an existing order; raises ValueError."""
    feature, oid = split_ref(ref)
    hits = [t for t in ([tool] if tool else TOOLS) if (order_dir(root, t, feature, oid) / ORDER_FILE).is_file()]
    if not hits:
        raise ValueError(f"không có order {ref}" + (f" của {tool}" if tool else ""))
    if len(hits) > 1:
        raise ValueError(f"order {ref} có ở cả {' và '.join(hits)}: đổi id một bên")
    return hits[0], feature, oid, order_dir(root, hits[0], feature, oid)


def all_orders(root):
    """[(tool, feature, id, folder)] of every order folder, sorted."""
    out = []
    for tool in TOOLS:
        base = studio_dir(root, tool) / "orders"
        for order in sorted(base.rglob(ORDER_FILE)) if base.is_dir() else []:
            rel = order.parent.relative_to(base).as_posix()
            feature, _, oid = rel.rpartition("/")
            out.append((tool, feature, oid, order.parent))
    return out


def ref_source(root, feature, ref):
    """The file a reference names, if the ordering feature may hand it over; raises ValueError."""
    r = ref.replace("\\", "/")
    if ".." in r.split("/") or r.startswith("/") or re.match(r"^[A-Za-z]:", r):
        raise ValueError(f"reference '{ref}': ghi đường dẫn tương đối, không có '..'")
    own = f"Assets/Features/{feature}/"
    public = re.match(r"^Assets/Features/(_Services/)?[A-Za-z][A-Za-z0-9]*/FeatureAssets/Public/", r)
    if r.startswith(own) or public:
        path = Path(root) / "UnityProject" / r
    elif r.startswith(RESOURCES + "/"):
        path = Path(root) / r
    else:
        raise ValueError(f"reference '{ref}': chỉ file của chính {feature}, FeatureAssets/Public của feature khác, "
                         f"hoặc trong {RESOURCES}/")
    if not path.is_file():
        raise ValueError(f"reference '{ref}' không có file")
    return path


def check_order(root, tool, feature, oid, folder):
    """The problems of one order (folder, front matter, content)."""
    data = parse_order(read_text(folder / ORDER_FILE))
    problems = list(data["errors"])
    if data["fields"].get("tool") and data["fields"]["tool"] != tool:
        problems.append(f"order nằm trong thư mục {tool} nhưng ghi tool: {data['fields']['tool']}")
    if not FEATURE_RX.match(feature) or not ID_RX.match(oid):
        problems.append("thư mục order phải là orders/<Feature>/<id> (id: chữ thường, số, gạch nối)")
    elif not (Path(root) / "UnityProject" / "Assets" / "Features" / Path(*feature.split("/"))).is_dir():
        problems.append(f"không có feature {feature}")
    for r in data["refs"]:
        try:
            ref_source(root, feature, r)
        except ValueError as ex:
            problems.append(str(ex))
    return data, problems


# ---------------------------------------------------------------- status, log, lessons
def read_status(folder):
    try:
        value = json.loads(read_text(folder / STATUS_FILE))
    except (OSError, ValueError):
        value = None
    value = value if isinstance(value, dict) else {}
    return {"state": value.get("state") or "new", "version": int(value.get("version") or 0),
            "feedback": [f for f in value.get("feedback") or [] if isinstance(f, str)], "at": value.get("at") or ""}


def save_status(folder, **changes):
    status = read_status(folder)
    status.update(changes, at=now())
    write_json(folder / STATUS_FILE, status)
    return status


def log(folder, event, **fields):
    with open(folder / LOG_FILE, "a", encoding="utf-8") as f:
        f.write(json.dumps({"at": now(), "event": event, **fields}, ensure_ascii=False) + "\n")


def feedback_key(tool, feature, oid, name):
    """How lessons.md names a feedback file: the path from Management/studio."""
    return f"{tool}/orders/{feature}/{oid}/{name}"


def unlearnt(root, tool, feature, oid, folder):
    """The feedback files of the order that lessons.md does not name yet."""
    lessons = studio_dir(root, tool) / LESSONS
    text = read_text(lessons) if lessons.is_file() else ""
    return [f for f in read_status(folder)["feedback"] if feedback_key(tool, feature, oid, f) not in text]


# ---------------------------------------------------------------- running a tool
def tool_exe(name):
    """The command that starts a tool, as a list. STUDIO_<NAME> overrides it (a .py file runs with this Python)."""
    exe = os.environ.get(f"STUDIO_{name.upper()}") or shutil.which(name)
    if not exe:
        raise ValueError(f"không tìm thấy lệnh {name}: cài {name} hoặc đi đường dự phòng (.claude/rules/studio.md)")
    return [sys.executable, exe] if exe.endswith(".py") else [exe]


def codex_images_dir():
    if os.environ.get("STUDIO_CODEX_IMAGES"):
        return Path(os.environ["STUDIO_CODEX_IMAGES"])
    home = Path(os.environ.get("CODEX_HOME") or Path.home() / ".codex")
    return home / "generated_images"


class RunLock:
    """One Codex run at a time: images made in read-only mode are told apart only by their time."""
    def __init__(self, root, tool):
        self.path = studio_dir(root, tool) / ".run.lock"

    def __enter__(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        if self.path.exists() and time.time() - self.path.stat().st_mtime > RUN_LOCK_HOURS * 3600:
            self.path.unlink()
        try:
            fd = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        except FileExistsError:
            raise ValueError("đang có một lần chạy khác của tool này; chờ nó xong rồi chạy lại") from None
        os.write(fd, str(os.getpid()).encode())
        os.close(fd)
        return self

    def __exit__(self, *exc):
        try:
            self.path.unlink()
        except OSError:
            pass


def run(cmd, stdin_text=None, cwd=None):
    """(exit code, tail of stderr)."""
    try:
        p = subprocess.run(cmd, input=(stdin_text or "").encode("utf-8"), capture_output=True, cwd=cwd, timeout=TIMEOUT)
        return p.returncode, (p.stderr or b"").decode("utf-8", "replace")[-1500:]
    except subprocess.TimeoutExpired:
        return -1, f"quá {TIMEOUT}s"
    except OSError as ex:
        return -2, str(ex)


def files_in(folder, exts=None, skip=("_log", "_rejected", "_old", "out")):
    """{path: mtime} of the files under folder (not the bookkeeping folders)."""
    out = {}
    for p in folder.rglob("*") if folder.is_dir() else []:
        rel = p.relative_to(folder).parts
        if p.is_file() and not (rel and rel[0] in skip) and (exts is None or p.suffix.lower() in exts):
            out[p] = p.stat().st_mtime
    return out


def set_aside(paths, folder, why):
    """Moves files out of the way (never deletes): work/<why>/<time>/."""
    if not paths:
        return []
    dest = folder / why / time.strftime("%Y%m%d-%H%M%S")
    dest.mkdir(parents=True, exist_ok=True)
    moved = []
    for p in paths:
        target = dest / p.name
        n = 1
        while target.exists():
            target, n = dest / f"{p.stem}-{n}{p.suffix}", n + 1
        shutil.move(str(p), str(target))
        moved.append(target.name)
    return moved


def prepare(root, tool, ref, item, kinds):
    """(feature, id, folder, order, item text, work) for a run; refuses what the rules forbid."""
    _, feature, oid, folder = find_order(root, ref, tool)
    data, problems = check_order(root, tool, feature, oid, folder)
    if problems:
        raise ValueError("order chưa hợp lệ, feature phải sửa: " + "; ".join(problems[:4]))
    if data["fields"]["kind"] not in kinds:
        raise ValueError(f"order này là kind {data['fields']['kind']}, không chạy bằng lệnh này")
    items = dict(data["items"])
    if item not in items:
        raise ValueError(f"order không có item '{item}'. Có: {', '.join(items)}")
    status = read_status(folder)
    if status["state"] == "taken":
        raise ValueError("order đã được feature nhận; việc mới thì feature đặt order mới")
    missing = unlearnt(root, tool, feature, oid, folder)
    if missing:
        raise ValueError(f"order đã bị trả: ghi bài học vào Management/studio/{tool}/{LESSONS} trước, mỗi mục ghi "
                         f"đường dẫn phản hồi: " + ", ".join(feedback_key(tool, feature, oid, f) for f in missing))
    work = folder / "work"
    (work / "out").mkdir(parents=True, exist_ok=True)
    (work / "_log").mkdir(parents=True, exist_ok=True)
    refs = []
    for r in data["refs"]:
        src = ref_source(root, feature, r)
        dst = folder / "refs" / src.name
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)
        refs.append(dst)
    save_status(folder, state="doing")
    return feature, oid, folder, data, items[item], work, refs


def prompt_for(root, tool, data, item, text, work, extra):
    lessons = studio_dir(root, tool) / LESSONS
    parts = []
    if lessons.is_file() and read_text(lessons).strip():
        parts.append("Tránh các lỗi đã gặp (bài học từ các order trước):\n" + read_text(lessons).strip())
    sec = data["sections"]
    parts.append(f"Order: {data['title']}\nĐịnh dạng: {data['fields']['format']}\n"
                 f"Mô tả:\n{chr(10).join(sec.get('Mô tả', [])).strip()}\n"
                 f"Tiêu chí nhận:\n{chr(10).join(sec.get('Tiêu chí nhận', [])).strip()}")
    parts.append(f"Mục cần làm (chỉ mục này): {item}: {text}")
    own = work / f"{item}.md"
    if own.is_file() and read_text(own).strip():
        parts.append(read_text(own).strip())
    parts.append(extra)
    return "\n\n".join(parts)


def take_result(work, item, new, exts):
    """Moves the one new result into out/<item><ext>; (ok, message, names)."""
    new = [p for p in new if p.suffix.lower() in exts]
    if not new:
        return False, "không có kết quả", []
    if len(new) > 1:
        moved = set_aside(new, work, "_rejected")
        return False, f"gen thừa: {len(new)} file cho một item, không lấy file nào (đã dời vào work/_rejected)", moved
    src = new[0]
    dest = work / "out" / f"{item}{src.suffix.lower()}"
    set_aside([p for p in (work / "out").glob(f"{item}.*") if p != src], work, "_old")      # the previous draft
    if src != dest:
        inside = work in src.parents
        (shutil.move if inside else shutil.copy2)(str(src), str(dest))      # Codex's own folder is only read
    return True, f"đã có work/out/{dest.name}", [dest.name]


def cmd_codex(root, ref, item):
    with RunLock(root, "codex"):
        feature, oid, folder, data, text, work, refs = prepare(root, "codex", ref, item, ("image",))
        exe = tool_exe("codex")
        prompt = prompt_for(root, "codex", data, item, text, work, ONE_IMAGE.format(item=item))
        stamp = time.strftime("%Y%m%d-%H%M%S")
        (work / "_log" / f"{item}-{stamp}.prompt.md").write_text(prompt, encoding="utf-8")
        images = [a for r in refs for a in ("-i", str(r))]
        last = str(work / "_log" / f"{item}-{stamp}.last.txt")
        # 1. Codex writes the image itself into its working folder.
        before = files_in(work, IMAGE_EXT) | {p: p.stat().st_mtime for p in (work / "out").glob("*")}
        code, err = run(exe + ["exec", "-C", str(work), "-s", "workspace-write", "--skip-git-repo-check", "-o", last]
                        + images + ["-"], prompt)
        made = [p for p in list(files_in(work, IMAGE_EXT)) + list((work / "out").glob("*"))
                if p.suffix.lower() in IMAGE_EXT and before.get(p) != p.stat().st_mtime]
        made = list(dict.fromkeys(made))
        ok, msg, names = take_result(work, item, made, IMAGE_EXT)
        log(folder, "run", tool="codex", item=item, mode="workspace-write", exit=code, result=msg, files=names,
            error=err if not ok else "")
        if ok:
            print(f"Codex (ghi trực tiếp): {msg}")
            return 0
        if made:      # it made too many: running again in another mode would only hide it
            print(f"Codex (ghi trực tiếp): {msg}. Chạy lại item này.", file=sys.stderr)
            return 1
        # 2. Read only: Codex keeps the images it makes in its own folder; take the new ones from there.
        gen = codex_images_dir()
        seen = files_in(gen, IMAGE_EXT, skip=())
        start = time.time()
        code2, err2 = run(exe + ["exec", "-C", str(work), "-s", "read-only", "--skip-git-repo-check", "-o", last]
                          + images + ["-"], prompt.replace(f"Lưu ảnh đó thành out/{item}.png trong thư mục làm việc.", ""))
        fresh = [p for p, m in files_in(gen, IMAGE_EXT, skip=()).items() if p not in seen and m >= start - 1]
        ok, msg, names = take_result(work, item, fresh, IMAGE_EXT)
        log(folder, "run", tool="codex", item=item, mode="read-only", exit=code2, result=msg, files=names,
            error=err2 if not ok else "")
        if ok:
            print(f"Codex (chỉ đọc, lấy ảnh ở {gen}): {msg}")
            return 0
        print(f"Codex không tạo được ảnh ({msg}). Đi gián tiếp qua unity-mcp theo .claude/rules/studio.md, rồi để "
              f"kết quả ở work/out/{item}.png và chạy deliver.", file=sys.stderr)
        return 1


def cmd_consult(root, ref, item):
    with RunLock(root, "codex"):
        feature, oid, folder, data, text, work, refs = prepare(root, "codex", ref, item, ("consult",))
        exe = tool_exe("codex")
        prompt = prompt_for(root, "codex", data, item, text, work,
                            "Trả lời đúng câu hỏi trên, bằng tiếng Việt, ngắn gọn và cụ thể. Không tạo ảnh hay file nào.")
        out = work / "out" / f"{item}.md"
        set_aside([out] if out.exists() else [], work, "_old")
        images = [a for r in refs for a in ("-i", str(r))]
        code, err = run(exe + ["exec", "-C", str(work), "-s", "read-only", "--skip-git-repo-check", "-o", str(out)]
                        + images + ["-"], prompt)
        ok = out.is_file() and read_text(out).strip() != ""
        log(folder, "run", tool="codex", item=item, mode="consult", exit=code, result="có câu trả lời" if ok else
            "không có câu trả lời", files=[out.name] if ok else [], error=err if not ok else "")
        print(f"Câu trả lời: work/out/{out.name}" if ok else "Codex không trả lời được; xem log.jsonl", file=sys.stdout if ok else sys.stderr)
        return 0 if ok else 1


def cmd_blender(root, ref, item):
    with RunLock(root, "blender"):
        feature, oid, folder, data, text, work, refs = prepare(root, "blender", ref, item, ("model",))
        script = work / f"{item}.py"
        if not script.is_file():
            raise ValueError(f"viết script Blender ở work/{item}.py trước (ghi kết quả thành <out>/{item}.<đuôi>; "
                             f"thư mục out là tham số đầu sau '--')")
        exe = tool_exe("blender")
        out = work / "out"
        before = {p: p.stat().st_mtime for p in out.glob(f"{item}.*")}
        code, err = run(exe + ["-b", "--factory-startup", "--python-exit-code", "1", "-P", str(script), "--", str(out), item])
        made = [p for p in out.glob(f"{item}.*") if before.get(p) != p.stat().st_mtime]
        others = [p for p in out.iterdir() if p.is_file() and not any(p.name.startswith(i + ".") for i in
                                                                      dict(data["items"]))]
        if len(made) == 1 and code == 0:
            ok, msg = True, f"đã có work/out/{made[0].name}"
        elif len(made) > 1:
            set_aside(made, work, "_rejected")
            ok, msg = False, f"gen thừa: {len(made)} file cho item {item}, không lấy file nào"
        else:
            ok, msg = False, f"Blender thoát mã {code}, không có work/out/{item}.<đuôi>"
        if others:
            set_aside(others, work, "_rejected")
            msg += f"; {len(others)} file không thuộc item nào bị dời vào work/_rejected"
        log(folder, "run", tool="blender", item=item, mode="headless", exit=code, result=msg,
            files=[p.name for p in made] if ok else [], error=err if not ok else "")
        print(f"Blender (headless): {msg}" if ok else f"Blender (headless): {msg}. Thử blender MCP, rồi unity-mcp "
              f"(.claude/rules/studio.md); kết quả để ở work/out/{item}.<đuôi> rồi chạy deliver.",
              file=sys.stdout if ok else sys.stderr)
        return 0 if ok else 1


def cmd_deliver(root, tool, ref):
    _, feature, oid, folder = find_order(root, ref, tool)
    data, problems = check_order(root, tool, feature, oid, folder)
    if problems:
        raise ValueError("order chưa hợp lệ: " + "; ".join(problems[:4]))
    out = folder / "work" / "out"
    files = [p for p in out.iterdir() if p.is_file()] if out.is_dir() else []
    ids = [i for i, _ in data["items"]]
    by_item = {i: [p for p in files if p.stem == i] for i in ids}
    stray = [p.name for p in files if p.stem not in ids]
    missing = [i for i, ps in by_item.items() if not ps]
    double = [i for i, ps in by_item.items() if len(ps) > 1]
    if missing or double or stray:
        raise ValueError("chưa giao được: " + "; ".join(
            ([f"thiếu {', '.join(missing)}"] if missing else []) + ([f"nhiều hơn một file cho {', '.join(double)}"] if double else [])
            + ([f"file không thuộc item nào: {', '.join(stray)}"] if stray else []))
            + f". Đúng {len(ids)} file, mỗi item một file tên <item>.<đuôi> trong work/out.")
    status = read_status(folder)
    version = status["version"] + 1
    dest = Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{version}"
    if dest.exists():
        raise ValueError(f"{dest} đã có; không ghi đè bản đã giao")
    dest.mkdir(parents=True)
    for i in ids:
        shutil.copy2(by_item[i][0], dest / by_item[i][0].name)
    save_status(folder, state="delivered", version=version)
    log(folder, "deliver", version=version, files=sorted(p.name for p in dest.iterdir()))
    rel = dest.relative_to(Path(root)).as_posix()
    print(f"Đã giao bản v{version}: {rel} ({len(ids)} file).\nNhắn phiên {feature} bằng SendMessage: "
          f"\"Order {feature}/{oid} đã giao bản v{version} ở {rel}. Nhận: python Management/tools/studio.py take "
          f"{feature}/{oid}; chưa đạt thì viết feedback-<n>.md trong thư mục order rồi chạy reject.\"")
    return 0


def cmd_take(root, ref):
    tool, feature, oid, folder = find_order(root, ref)
    data = parse_order(read_text(folder / ORDER_FILE))
    status = read_status(folder)
    if status["version"] < 1 or status["state"] == "rejected":
        raise ValueError(f"order {ref} chưa có bản giao nào đang chờ nhận (trạng thái: {status['state']})")
    if data["fields"].get("kind") == "consult":
        raise ValueError(f"order tham vấn không chép vào Assets: đọc câu trả lời ở {RESOURCES}/{feature}/{tool}/{oid}/v{status['version']}/")
    src = Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{status['version']}"
    dest = (Path(root) / "UnityProject" / "Assets" / "Features" / Path(*feature.split("/")) / "FeatureAssets"
            / data["fields"]["deliver"] / "Generated" / oid)
    dest.mkdir(parents=True, exist_ok=True)
    names = {p.name for p in src.iterdir() if p.is_file()}
    for p in dest.iterdir():      # what an earlier version had and this one does not (the .meta goes with it)
        base = p.name[:-5] if p.name.endswith(".meta") else p.name
        if p.is_file() and base not in names:
            p.unlink()
    for name in sorted(names):
        shutil.copy2(src / name, dest / name)      # same name: Unity keeps the .meta and its GUID
    save_status(folder, state="taken")
    log(folder, "take", version=status["version"], to=dest.relative_to(Path(root)).as_posix())
    print(f"Đã chép bản v{status['version']} vào {dest.relative_to(Path(root)).as_posix()}; refresh Unity để có .meta.")
    return 0


def cmd_reject(root, ref):
    tool, feature, oid, folder = find_order(root, ref)
    status = read_status(folder)
    if status["state"] not in ("delivered", "taken"):
        raise ValueError(f"order {ref} chưa giao nên chưa trả được (trạng thái: {status['state']})")
    fresh = sorted((p.name for p in folder.iterdir() if FEEDBACK_RX.match(p.name) and p.name not in status["feedback"]),
                   key=lambda n: int(FEEDBACK_RX.match(n).group(1)))
    if not fresh:
        raise ValueError("viết feedback-<n>.md mới trong thư mục order trước: sai ở đâu, item nào, cần thế nào")
    save_status(folder, state="rejected", feedback=status["feedback"] + fresh)
    log(folder, "reject", version=status["version"], feedback=fresh)
    print(f"Đã trả order {ref} ({', '.join(fresh)}). Nhắn @{tool.capitalize()} bằng SendMessage để làm lại.")
    return 0


# ---------------------------------------------------------------- reading
def summary(root, tool=None):
    """[(ref, tool, state, version, count, title, problems, unlearnt)] of the orders."""
    out = []
    for t, feature, oid, folder in all_orders(root):
        if tool and t != tool:
            continue
        data, problems = check_order(root, t, feature, oid, folder)
        status = read_status(folder)
        out.append((f"{feature}/{oid}", t, status["state"], status["version"], len(data["items"]), data["title"],
                    problems, unlearnt(root, t, feature, oid, folder)))
    return out


def pending_line(root, tool):
    """One line for the tool chat's prompt: what waits for it."""
    try:
        rows = [r for r in summary(root, tool) if r[2] in ("new", "doing", "rejected")]
    except OSError:
        return ""
    if not rows:
        return " Không có order nào đang chờ."
    return f" {len(rows)} order đang chờ: " + "; ".join(f"{r[0]} ({r[2]}, {r[4]} item)" for r in rows[:8])


def cmd_status(root, arg=""):
    if arg and arg not in TOOLS:
        tool, feature, oid, folder = find_order(root, arg)
        rows = [r for r in summary(root, tool) if r[0] == f"{feature}/{oid}"]
    else:
        rows = summary(root, arg or None)
    if not rows:
        print("Chưa có order nào.")
    for ref, tool, state, version, count, title, problems, missing in rows:
        print(f"- {tool} {ref}: {state}" + (f", đã giao v{version}" if version else "") + f", {count} item · {title}")
        for p in problems:
            print(f"    LỖI ORDER: {p}")
        for m in missing:
            print(f"    CHƯA RÚT BÀI HỌC: {feedback_key(tool, *ref.rsplit('/', 1), m)} (ghi vào {tool}/{LESSONS})")
    return 0


def cmd_check(root):
    bad = 0
    for ref, tool, _, _, _, _, problems, _ in summary(root):
        for p in problems:
            print(f"{tool} {ref}: {p}")
            bad += 1
    print(f"{bad} lỗi order." if bad else "Mọi order đều hợp lệ.")
    return 1 if bad else 0


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    args = argv[1:]
    usage = __doc__.split("\n\n")[1]
    try:
        if args == ["check"]:
            return cmd_check(root)
        if args[:1] == ["status"] and len(args) <= 2:
            return cmd_status(root, args[1] if len(args) == 2 else "")
        if len(args) == 3 and args[0] in ("codex", "consult", "blender"):
            return {"codex": cmd_codex, "consult": cmd_consult, "blender": cmd_blender}[args[0]](root, args[1], args[2])
        if len(args) == 3 and args[0] == "deliver" and args[1] in TOOLS:
            return cmd_deliver(root, args[1], args[2])
        if len(args) == 2 and args[0] in ("take", "reject"):
            return (cmd_take if args[0] == "take" else cmd_reject)(root, args[1])
        print("Cách dùng:\n" + usage, file=sys.stderr)
        return 2
    except (ValueError, OSError) as ex:
        print(f"Không làm được: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
