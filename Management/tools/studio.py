#!/usr/bin/env python3
"""Orders from the features to the tool chats (@Codex: images and GPT consults; @Blender: 3D models), and the one
way Codex and Blender are run. Every run, delivery and refusal is kept, so a mistake is learnt once.

    python Management/tools/studio.py check                          every order is well formed (any chat)
    python Management/tools/studio.py status [codex|blender|<F>/<id>] what waits, what is done (any chat)
    python Management/tools/studio.py codex <F>/<id> <item>           @Codex: one image for one item
    python Management/tools/studio.py consult <F>/<id> <item>         @Codex: ask GPT one question
    python Management/tools/studio.py blender <F>/<id> <item>         @Blender: run work/<item>.py headless
    python Management/tools/studio.py deliver <codex|blender> <F>/<id>  the tool chat: hand the order over
    python Management/tools/studio.py review <F>/<id> <item> <0-3> ["<feedback>"]   the one who ordered: judge one item
    python Management/tools/studio.py anchor <_design/...>/<id> <item> the human: an art that passed guides its style (toggle)
    python Management/tools/studio.py pick <F>/<id> <item> <n>         the tool chat: take run n of an item as its result
    python Management/tools/studio.py debt close <n-k>                 the human: accept an art debt as it is

Layout (see .claude/rules/studio.md):
    Management/studio/<tool>/lessons.md                    mistakes met and how to avoid them; put before every prompt
    Management/studio/<tool>/orders/<F>/<id>/order.md      the order (the one who orders writes it once)
                                          review.json      the judgement of each delivered item (this code and the page)
                                          status.json      state (new, doing, delivered), version (this code only)
                                          log.jsonl        every run, delivery and judgement (this code only)
                                          refs/            copies of the order's references (this code only)
                                          work/            prompts, Blender scripts; work/out/<item>.<ext> the results
    Resources/<F>/<tool>/<id>/v<n>/<item>.<ext>            every delivery, never overwritten
<F> is the code folder of the feature that ordered (Hero, _Services/Popup); <id> and <item>: lower case, digits, '-'.

Judging (review.json, REVIEW_LEVELS): each item of a delivery is "Chưa duyệt" until it gets one of four levels, with
an optional feedback text: 0 "Làm lại ngay", 1 "Lỗi nhưng chưa cần làm ngay" (used as it is; an art debt), 2 "Đúng
phần lớn chi tiết", 3 "Đạt". A feature judges its own orders with `review`, which also copies an item judged 1-3
into its FeatureAssets; the human judges art concepts on the management page (setup_server.py), which only stores
the value. Nothing is sent to anyone: whoever ordered tells the tool chat.
Runs are counted (log.jsonl, event attempt): an item has RUNS_FIRST runs before the first delivery. After a
delivery it runs again only while its judgement is level 0 WITH a feedback text: RUNS_REDO run for each such round,
at most MAX_REDOS rounds. The feedback goes into the prompt of that run. Every run's result is kept in
work/attempts/<item>-<n>.<ext>; `pick` puts one back into work/out. The tool chat writes work/<item>.check.md (first
line "đạt" or "chưa đạt: <why>"); deliver marks the other items "chưa chuẩn" in quality.json and opens an art debt
for each (Management/studio/debt.json), which a later delivery that passes the same owner and item closes, or a
judgement of level 3, or the human. Exactly one result per item: a run that makes more takes none of them.
Standard library only; Windows and Linux.

Art concepts (kind: art, @Codex): the owner is a design node, `_design/<node path with '.' for '/'>`, and
@GameDesigner writes the order and tells @Codex. One item per frame of the node in live; style: names styles of the
root's .art-style/. No approval step: the run limits above and ART_DAILY_MAX art runs a day are the brake.
The prompt carries the styles; the images handed to Codex are the frame drawn as PNG, the styles' pictures and up to
ART_ANCHORS accepted pictures marked as anchors. Delivered to Resources/_design/<node>/codex/<id>/v<n>/ with
meta.json (node hash, frame hashes, style hashes), which the page uses to label a picture stale.
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
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

TOOLS = {"codex": ("image", "consult", "art"), "blender": ("model",)}
DELIVER_TO = ("Private", "Public")
ART_DELIVER = "concept"          # an art order is never copied into Assets
REVIEW_FILE = "review.json"      # beside the order: {item: {level, text, version, at}} of what was delivered last
REVIEW_LEVELS = gd.ART_LEVELS    # level = index: Làm lại ngay, Lỗi nhưng chưa cần làm ngay, Đúng phần lớn chi tiết, Đạt
REVIEW_NONE = gd.ART_LEVEL_NONE  # Chưa duyệt
REDO, LATER, PASS = 0, 1, 3
REVIEW_TEXT_MAX = 2000
DEBT_REVIEW = "người chấm"       # the kind of an art debt opened by a judgement of level LATER
RUNS_FIRST = 2                   # runs of an item before the first delivery
RUNS_REDO = 1                    # runs of an item each time it is judged "Làm lại ngay" (with feedback)
MAX_REDOS = 2                    # times one item may be made again
DEBT_FILE = "debt.json"          # Management/studio/debt.json: the art debts (this code and the page only)
QUALITY_FILE = "quality.json"    # beside a delivery: which items pass their own check
CHECK_PASS = "đạt"
ART_DAILY_MAX = 20
ART_ANCHORS = 3
DESIGN_RX = re.compile(r"^_design/[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$")
ONE_ART = ("Ảnh đầu tiên được đính kèm là frame của màn này: các hình cơ bản mang kích thước thật. Giữ đúng bố cục, "
           "vị trí và tỉ lệ của frame, khung hình và hướng màn; chỉ thay hình cơ bản bằng art theo style. Không thêm "
           "chữ chú thích, số đo hay đường kẻ của frame vào ảnh.")
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
    if not (FEATURE_RX.match(feature) or DESIGN_RX.match(feature)) or not ID_RX.match(oid):
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
    if fields.get("deliver") and fields["deliver"] not in DELIVER_TO + (ART_DELIVER,):
        errors.append(f"deliver phải là Private hoặc Public ({ART_DELIVER} với kind: art)")
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
    design = bool(DESIGN_RX.match(feature))
    kind = data["fields"].get("kind", "")
    if design:
        problems += check_art(root, feature, oid, folder, data)
    elif not FEATURE_RX.match(feature) or not ID_RX.match(oid):
        problems.append("thư mục order phải là orders/<Feature>/<id> (id: chữ thường, số, gạch nối)")
    elif not (Path(root) / "UnityProject" / "Assets" / "Features" / Path(*feature.split("/"))).is_dir():
        problems.append(f"không có feature {feature}")
    if kind == "art" and not design:
        problems.append(f"kind: art chỉ dành cho order của node thiết kế (orders/{gd.DESIGN_OWNER}/<node>/<id>, do @GameDesigner đặt)")
    if data["fields"].get("deliver") == ART_DELIVER and kind != "art":
        problems.append(f"deliver: {ART_DELIVER} chỉ dùng với kind: art")
    for r in data["refs"]:
        try:
            ref_source(root, feature, r)
        except ValueError as ex:
            problems.append(str(ex))
    return data, problems


# ---------------------------------------------------------------- art concepts
def node_of(feature):
    """The design node path of an owner '_design/<a.b.c>'."""
    return feature[len(gd.DESIGN_OWNER) + 1:].replace(".", "/")


def live_node(root, path):
    """(node, root node) of the live tree, read the way the page reads it; node None when live lacks it."""
    tree, _ = gd.read_tree(root, "live")
    nodes = {n["path"]: n for n in gd.walk(tree)} if tree else {}
    return nodes.get(path), nodes.get("")


def order_styles(data):
    return [s.strip() for s in re.split(r"[,\s]+", data["fields"].get("style", "")) if s.strip()]


def check_art(root, feature, oid, folder, data):
    """The problems of an art order: a cheat node in live with frames, one item per state (its frame), known styles, and no frame
    that already has a picture still good (unless this order is the one that made it)."""
    problems = []
    if data["fields"].get("tool") not in ("", "codex") or data["fields"].get("kind") not in ("", "art"):
        problems.append("order của node thiết kế chỉ là tool: codex, kind: art")
    if data["fields"].get("deliver") and data["fields"]["deliver"] != ART_DELIVER:
        problems.append(f"order art ghi deliver: {ART_DELIVER} (ảnh không vào Assets)")
    path = node_of(feature)
    parts = path.split("/")
    if len(parts) != gd.CHEAT_ROUND - 1 or parts[-1] != gd.CHEAT_KEY:
        return problems + [f"order art chỉ đặt cho node cheat (<feature>/{gd.CHEAT_KEY}) đã ở live, không cho '{path}'"]
    node, top = live_node(root, path)
    if node is None:
        return problems + [f"node '{path}' chưa có trong live: chỉ gen art cho node đã chốt"]
    frames = {Path(fr["file"]).stem for fr in node["frames"]}
    if not frames:
        problems.append(f"node '{path}' trong live chưa có frame: viết frame cho từng state và chốt trước")
    for item, _ in data["items"]:
        if frames and item not in frames:
            problems.append(f"item '{item}' phải là id một state của node cheat ({', '.join(sorted(frames))})")
    known = {s["id"] for s in (top or {}).get("styles", [])}
    wanted = order_styles(data)
    if not known:
        problems.append(f"vòng 1 trong live chưa có style nào ({gd.STYLE_DIR}/ của node gốc)")
    elif not wanted:
        problems.append("thiếu 'style:' ở phần đầu (một hay nhiều id style của vòng 1)")
    for s in wanted:
        if known and s not in known:
            problems.append(f"style '{s}' không có ở vòng 1 ({', '.join(sorted(known))})")
    if read_status(folder)["version"] == 0:
        tree, _ = gd.read_tree(root, "live")
        art, _ = gd.read_art(root, tree)
        good = {e["frame"]: e["ref"] for e in art.get(path, []) if e["usable"]}
        mine = f"{feature}/{oid}"
        for item, _ in data["items"]:
            if good.get(item) and good[item] != mine:
                problems.append(f"frame '{item}' đã có ảnh còn dùng được ({good[item]}); chỉ xin lại khi ảnh đó cũ "
                                f"hoặc bị chấm '{REVIEW_LEVELS[REDO]}'")
    return problems


def order_hash(folder):
    return gd.short_hash(read_text(folder / ORDER_FILE).strip())


def attempts(root, since="", day=""):
    """[(ref, item, at)] of the art runs logged in every design order, after `since` or on `day` (YYYY-MM-DD)."""
    base = studio_dir(root, "codex") / "orders" / gd.DESIGN_OWNER
    out = []
    for logf in sorted(base.glob(f"*/*/{LOG_FILE}")) if base.is_dir() else []:
        ref = f"{gd.DESIGN_OWNER}/{logf.parent.parent.name}/{logf.parent.name}"
        for line in read_text(logf).split("\n"):
            try:
                e = json.loads(line)
            except ValueError:
                continue
            if isinstance(e, dict) and e.get("event") == "attempt" and e.get("at", "") > since and e.get("at", "").startswith(day):
                out.append((ref, e.get("item"), e["at"]))
    return out


def events(folder):
    out = []
    for line in read_text(folder / LOG_FILE).split("\n") if (folder / LOG_FILE).is_file() else []:
        try:
            e = json.loads(line)
        except ValueError:
            continue
        if isinstance(e, dict):
            out.append(e)
    return out


def read_reviews(folder):
    """{item: {level (0-3 or None), text, version, at}} of the order: the judgement of what was delivered last."""
    value = gd._json(folder / REVIEW_FILE)
    out = {}
    for item, r in (value.items() if isinstance(value, dict) else []):
        if not isinstance(r, dict):
            continue
        level = r.get("level")
        level = level if isinstance(level, int) and not isinstance(level, bool) and 0 <= level < len(REVIEW_LEVELS) else None
        text = str(r.get("text") or "").strip()
        if level is not None or text:
            out[item] = {"level": level, "text": text, "version": int(r.get("version") or 0), "at": str(r.get("at") or "")}
    return out


def level_name(level):
    return REVIEW_NONE if level is None else REVIEW_LEVELS[level]


def budget(folder, item):
    """{used, allowed, left, round, why, opens} of one item. Before the first delivery: RUNS_FIRST runs. After a
    delivery: RUNS_REDO run, and only while the item is judged REDO with a feedback text, MAX_REDOS times at most.
    why: the reason nothing is left. opens: the next run starts a new round (it is then logged as 'redo').
    An order sent back the old way (event reject) counts as one round of every item."""
    log_ = events(folder)
    last = max([i for i, e in enumerate(log_) if e.get("event") == "deliver"] or [-1])
    mine = lambda e, name: e.get("event") == name and e.get("item") == item      # noqa: E731
    used = sum(1 for e in log_ if mine(e, "attempt"))
    since = sum(1 for e in log_[last + 1:] if mine(e, "attempt"))
    rounds = sum(1 for e in log_ if mine(e, "redo") or e.get("event") == "reject")
    left, why, opens = 0, "", False
    if last < 0:
        left = max(0, RUNS_FIRST - since)
        why = "" if left else f"hết {RUNS_FIRST} lượt đầu"
    elif any(mine(e, "redo") or e.get("event") == "reject" for e in log_[last + 1:]):      # a round already open
        left = max(0, RUNS_REDO - since)
        why = "" if left else f"đã chạy {RUNS_REDO} lượt của lần làm lại này"
    else:
        review = read_reviews(folder).get(item) or {"level": None, "text": ""}
        if review["level"] != REDO:
            why = f"bản đã giao đang ở mức '{level_name(review['level'])}', không phải '{REVIEW_LEVELS[REDO]}'"
        elif not review["text"]:
            why = f"item được chấm '{REVIEW_LEVELS[REDO]}' nhưng chưa có phản hồi: bên đặt order phải ghi phản hồi trước"
        elif rounds >= MAX_REDOS:
            why = f"đã làm lại {rounds}/{MAX_REDOS} lần: không chạy thêm (chấm '{REVIEW_LEVELS[LATER]}' để ghi nợ)"
        else:
            left, opens = RUNS_REDO, True
    return {"used": used, "allowed": used + left, "left": left, "round": rounds, "why": why, "opens": opens}


def start_attempt(folder, item, **extra):
    """Logs one run before the tool starts; returns its number for the item (1, 2, ...). The first run after a
    delivery opens a round ('redo', with the feedback that asked for it)."""
    b = budget(folder, item)
    if b["opens"]:
        review = read_reviews(folder).get(item) or {}
        log(folder, "redo", item=item, round=b["round"] + 1, text=review.get("text", ""), version=review.get("version", 0))
    n = b["used"] + 1
    log(folder, "attempt", item=item, n=n, **extra)
    return n


def keep_attempt(work, item, n):
    """Copies the result now in work/out into work/attempts/<item>-<n>.<ext>."""
    for p in (work / "out").glob(f"{item}.*"):
        dest = work / "attempts" / f"{item}-{n}{p.suffix}"
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, dest)


def art_gate(root):
    """Raises ValueError when the art runs of today have reached the limit of the whole project."""
    today = attempts(root, day=time.strftime("%Y-%m-%d"))
    if len(today) >= ART_DAILY_MAX:
        raise ValueError(f"đã chạy {len(today)}/{ART_DAILY_MAX} lần gen art hôm nay (trần chung): để mai, hoặc người nâng trần")


def svg_to_png(svg_path, png_path):
    """Draws an SVG into a PNG with whatever the machine has (STUDIO_SVG2PNG overrides: a command or a .py taking
    <in> <out>). Returns the converter used, or '' when none worked."""
    override = os.environ.get("STUDIO_SVG2PNG")
    tries = []
    if override:
        tries.append(([sys.executable, override] if override.endswith(".py") else [override]) + [str(svg_path), str(png_path)])
    if shutil.which("rsvg-convert"):
        tries.append(["rsvg-convert", "-w", "1920", "-o", str(png_path), str(svg_path)])
    if shutil.which("inkscape"):
        tries.append(["inkscape", str(svg_path), "-o", str(png_path), "-w", "1920"])
    for magick in ("magick", "convert"):
        if shutil.which(magick):
            tries.append([magick, str(svg_path), str(png_path)])
            break
    for cmd in tries:
        code, _ = run(cmd)
        if code == 0 and Path(png_path).is_file():
            return cmd[0]
    return ""


def frame_png(root, path, frame, dest):
    """The frame of the live node drawn into dest/<frame>.frame.png (colours fixed, no page variables)."""
    node, _ = live_node(root, path)
    fr = next((f for f in (node or {}).get("frames", []) if Path(f["file"]).stem == frame), None)
    if fr is None:
        return None
    css = re.sub(r"var\(--[\w-]+, ((?:[^()]|\([^()]*\))*)\)", r"\1", gd.FRAME_CSS)
    svg = fr["svg"].replace("</defs>", f"</defs><style>{css}</style>", 1)
    dest.mkdir(parents=True, exist_ok=True)
    svg_path, png_path = dest / f"{frame}.frame.svg", dest / f"{frame}.frame.png"
    svg_path.write_text(svg, encoding="utf-8")
    return png_path if svg_to_png(svg_path, png_path) else None


def art_inputs(root, feature, data, item, work):
    """(prompt part, images) an art run adds: the styles' text, the frame PNG, the styles' pictures, the anchors."""
    path = node_of(feature)
    _, top = live_node(root, path)
    styles = {s["id"]: s for s in (top or {}).get("styles", [])}
    wanted = [styles[s] for s in order_styles(data) if s in styles]
    text = "\n\n".join(f"Style {s['id']}: {s['title']}\n{s['text']}" for s in wanted)
    images, notes = [], []
    png = frame_png(root, path, item, work / "_frames")
    if png:
        images.append(png)
    else:
        notes.append("không đổi được frame sang PNG (cần rsvg-convert, inkscape hoặc magick): ảnh sẽ không bám bố cục")
    live_root = gd.table_dir(root, "live")
    for s in wanted:
        images += [live_root / gd.STYLE_DIR / n for n in s["images"]]
    anchors = gd._json(studio_dir(root, "codex") / gd.ANCHORS_FILE) or []
    picked = 0
    for a in reversed(anchors):
        if picked >= ART_ANCHORS or not isinstance(a, dict) or not set(a.get("styles") or []) & set(order_styles(data)):
            continue
        f = Path(root) / str(a.get("file", ""))
        if f.is_file():
            images.append(f)
            picked += 1
    return ("Phong cách (vòng 1, bắt buộc theo):\n" + text if text else ""), images, notes


# ---------------------------------------------------------------- status, log, lessons
def read_status(folder):
    try:
        value = json.loads(read_text(folder / STATUS_FILE))
    except (OSError, ValueError):
        value = None
    value = value if isinstance(value, dict) else {}
    version = int(value.get("version") or 0)
    state = value.get("state") or "new"
    if state not in ("new", "doing", "delivered"):      # written before judging replaced take/reject/approve/accept
        state = "delivered" if version else "new"
    return {"state": state, "version": version, "at": value.get("at") or ""}


def view_state(status, items, reviews):
    """What an order waits for, from its status and the judgement of its items:
    new | doing | review (an item is not judged yet) | redo (an item is judged REDO) | done."""
    if status["state"] != "delivered":
        return status["state"]
    levels = [(reviews.get(i) or {}).get("level") for i in items]
    return "redo" if REDO in levels else "review" if None in levels else "done"


VIEW_STATES = {"new": "mới", "doing": "đang làm", "review": "chờ duyệt", "redo": "cần làm lại", "done": "xong"}


def save_status(folder, **changes):
    status = read_status(folder)
    status.update(changes, at=now())
    write_json(folder / STATUS_FILE, status)
    return status


def log(folder, event, **fields):
    with open(folder / LOG_FILE, "a", encoding="utf-8") as f:
        f.write(json.dumps({"at": now(), "event": event, **fields}, ensure_ascii=False) + "\n")


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
    b = budget(folder, item)
    if b["left"] <= 0:
        raise ValueError(f"item '{item}' không được chạy lúc này: {b['why']} (đã chạy {b['used']} lượt, làm lại "
                         f"{b['round']}/{MAX_REDOS} lần). Đang làm dở thì chọn bản gần order nhất bằng `studio.py pick "
                         f"{feature}/{oid} {item} <n>` (các bản ở work/attempts/), ghi work/{item}.check.md ('chưa đạt: "
                         f"<lý do>') rồi deliver; item sẽ mang nhãn chưa chuẩn kèm phiếu nợ")
    if DESIGN_RX.match(feature):
        art_gate(root)
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
    review = read_reviews(work.parent).get(item)
    if review and review["text"]:      # what the one who ordered said about the last delivery of this item
        parts.append(f"Phản hồi về bản đã giao lần trước của mục này (bản v{review['version']}, bị chấm "
                     f"'{level_name(review['level'])}'), phải sửa đúng các điểm này:\n{review['text']}")
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
        feature, oid, folder, data, text, work, refs = prepare(root, "codex", ref, item, ("image", "art"))
        exe = tool_exe("codex")
        extra = ONE_IMAGE.format(item=item)
        if data["fields"]["kind"] == "art":
            style_text, art_images, notes = art_inputs(root, feature, data, item, work)
            refs = art_images + refs
            extra = "\n\n".join(x for x in (style_text, ONE_ART, extra) if x)
            for n in notes:
                print(f"Lưu ý: {n}", file=sys.stderr)
        attempt = start_attempt(folder, item, images=[Path(r).name for r in refs])
        prompt = prompt_for(root, "codex", data, item, text, work, extra)
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
            keep_attempt(work, item, attempt)
            print(f"Codex (ghi trực tiếp, lượt {attempt}): {msg}")
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
            keep_attempt(work, item, attempt)
            print(f"Codex (chỉ đọc, lấy ảnh ở {gen}, lượt {attempt}): {msg}")
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
        attempt = start_attempt(folder, item)
        images = [a for r in refs for a in ("-i", str(r))]
        code, err = run(exe + ["exec", "-C", str(work), "-s", "read-only", "--skip-git-repo-check", "-o", str(out)]
                        + images + ["-"], prompt)
        ok = out.is_file() and read_text(out).strip() != ""
        if ok:
            keep_attempt(work, item, attempt)
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
        attempt = start_attempt(folder, item)
        code, err = run(exe + ["-b", "--factory-startup", "--python-exit-code", "1", "-P", str(script), "--", str(out), item])
        made = [p for p in out.glob(f"{item}.*") if before.get(p) != p.stat().st_mtime]
        others = [p for p in out.iterdir() if p.is_file() and not any(p.name.startswith(i + ".") for i in
                                                                      dict(data["items"]))]
        if len(made) == 1 and code == 0:
            ok, msg = True, f"đã có work/out/{made[0].name} (lượt {attempt})"
            set_aside([p for p in out.glob(f"{item}.*") if p != made[0]], work, "_old")
            keep_attempt(work, item, attempt)
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
    meta = None
    if DESIGN_RX.match(feature):
        node, top = live_node(root, node_of(feature))
        styles = {s["id"]: s["hash"] for s in (top or {}).get("styles", [])}
        meta = {"node": node_of(feature), "nodeHash": node["dataHash"],
                "frames": {Path(f["file"]).stem: gd.frame_text_hash(f["text"]) for f in node["frames"]},
                "styles": {s: styles.get(s, "") for s in order_styles(data)}, "order": f"{feature}/{oid}",
                "version": version, "at": now(), "stamp": time.time()}
    dest.mkdir(parents=True)
    for i in ids:
        shutil.copy2(by_item[i][0], dest / by_item[i][0].name)
    if meta:
        write_json(dest / gd.ART_META, meta)
    quality = {}
    for i in ids:
        ok, note = self_check(folder / "work", i)
        b = budget(folder, i)
        quality[i] = {"ok": ok, "note": note, "runs": b["used"], "round": b["round"], "file": by_item[i][0].name}
    write_json(dest / QUALITY_FILE, quality)
    # A made-again item is "Chưa duyệt" again; the items left alone keep their judgement.
    log_ = events(folder)
    last = max([n for n, e in enumerate(log_) if e.get("event") == "deliver"] or [-1])
    remade = {e.get("item") for e in log_[last + 1:] if e.get("event") in ("attempt", "pick")}
    reviews = {i: r for i, r in read_reviews(folder).items() if i in ids and (last >= 0 and i not in remade)}
    debts = read_debts(root)
    rel_dest = dest.relative_to(Path(root)).as_posix()
    opened, closed = [], []
    for i, q in quality.items():
        if i in reviews and reviews[i]["level"] is not None:      # already judged by the one who ordered: that stands
            continue
        if q["ok"]:
            for d in debts:
                if d["state"] == "open" and d["owner"] == feature and d["item"] == i:
                    d.update(state="closed", closedBy=f"tự đóng: {feature}/{oid} v{version} đạt", closedAt=now())
                    closed.append(d["id"])
        else:
            opened.append(open_debt(debts, tool, f"{feature}/{oid}", feature, i, version, f"{rel_dest}/{q['file']}",
                                    q["note"], "chưa chuẩn")["id"])
    if opened or closed:
        write_debts(root, debts)
    write_json(folder / REVIEW_FILE, reviews)
    save_status(folder, state="delivered", version=version)
    log(folder, "deliver", version=version, files=sorted(p.name for p in dest.iterdir()),
        below=[i for i, q in quality.items() if not q["ok"]])
    for i, q in quality.items():
        if not q["ok"]:
            print(f"CHƯA CHUẨN: {i}: {q['note']}")
    if opened:
        print(f"Đã mở phiếu nợ {', '.join(opened)} (tab Studio).")
    if closed:
        print(f"Đã đóng phiếu nợ {', '.join(closed)}: bản này đạt.")
    rel = dest.relative_to(Path(root)).as_posix()
    if meta:
        print(f"Đã giao bản v{version}: {rel} ({len(ids)} ảnh). Ảnh hiện ở tab Game design (node {meta['node']}); người "
              f"chấm từng ảnh ở đó. Nhắn @GameDesigner bằng SendMessage (to: \"GameDesigner\") rằng order {feature}/{oid} "
              f"đã giao bản v{version}.")
        return 0
    below = "; ".join(f"{i}: {q['note']}" for i, q in quality.items() if not q["ok"])
    below_text = f" Chưa chuẩn (hết lượt, có phiếu nợ): {below}." if below else ""
    print(f"Đã giao bản v{version}: {rel} ({len(ids)} file).\nNhắn phiên {feature} bằng SendMessage: "
          f"\"Order {feature}/{oid} đã giao bản v{version} ở {rel}. Chấm từng item: python Management/tools/studio.py "
          f"review {feature}/{oid} <item> <0-3> \\\"<phản hồi>\\\" (0 làm lại ngay, 1 lỗi nhưng chưa cần làm ngay, 2 đúng "
          f"phần lớn, 3 đạt); mức 1-3 được chép vào FeatureAssets." + below_text + "\"")
    return 0


# ---------------------------------------------------------------- judging a delivery
def version_dir(root, tool, feature, oid, version):
    return Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{version}"


def item_file(vdir, item):
    """The delivered file of an item in a version folder, or None."""
    skip = (gd.ART_META, QUALITY_FILE)
    return next((p for p in sorted(vdir.iterdir()) if p.is_file() and p.stem == item and p.name not in skip), None) if vdir.is_dir() else None


def sync_debt(root, tool, feature, oid, item, level, text, version, file):
    """Keeps the art debts in step with a judgement: LATER owes the item (one open debt of kind DEBT_REVIEW),
    PASS closes every open debt of the owner's item, any other level closes the judgement's own debt."""
    debts = read_debts(root)
    ref, changed = f"{feature}/{oid}", False
    mine = [d for d in debts if d["state"] == "open" and d.get("ref") == ref and d.get("item") == item and d.get("kind") == DEBT_REVIEW]
    if level == LATER:
        reason = text or REVIEW_LEVELS[LATER]
        if mine:
            changed = mine[0].get("reason") != reason or mine[0].get("version") != version
            mine[0].update(reason=reason, version=version, file=file)
        else:
            open_debt(debts, tool, ref, feature, item, version, file, reason, DEBT_REVIEW)
            changed = True
    else:
        closing = mine if level != PASS else [d for d in debts if d["state"] == "open" and d.get("owner") == feature and d.get("item") == item]
        for d in closing:
            d.update(state="closed", closedBy=f"chấm lại: {level_name(level)}", closedAt=now())
            changed = True
    if changed:
        write_debts(root, debts)


def save_review(root, ref, item, level, text=""):
    """Stores the judgement of one delivered item (level 0-3, or None for "Chưa duyệt") and its feedback text.
    Used by `review` and by the management page; it only stores, it tells nobody. Returns (tool, feature, id,
    folder, entry or None); raises ValueError."""
    tool, feature, oid, folder = find_order(root, ref)
    data = parse_order(read_text(folder / ORDER_FILE))
    if item not in dict(data["items"]):
        raise ValueError(f"order không có item '{item}'. Có: {', '.join(i for i, _ in data['items'])}")
    if level is not None and (not isinstance(level, int) or isinstance(level, bool) or not 0 <= level < len(REVIEW_LEVELS)):
        raise ValueError(f"mức chấm là số 0-{len(REVIEW_LEVELS) - 1} (" + ", ".join(
            f"{n} {name}" for n, name in enumerate(REVIEW_LEVELS)) + ")")
    text = str(text or "").replace("\r", "").strip()
    if len(text) > REVIEW_TEXT_MAX:
        raise ValueError(f"phản hồi dài quá {REVIEW_TEXT_MAX} kí tự")
    status = read_status(folder)
    if status["version"] < 1:
        raise ValueError(f"order {ref} chưa có bản giao nào để chấm")
    if status["state"] != "delivered":
        raise ValueError(f"order {ref} đang được làm lại: chờ bản giao mới rồi chấm")
    vdir = version_dir(root, tool, feature, oid, status["version"])
    hit = item_file(vdir, item)
    reviews = read_reviews(folder)
    before = reviews.get(item) or {"level": None, "text": ""}
    if level is None and not text:
        reviews.pop(item, None)
        entry = None
    else:
        entry = {"level": level, "text": text, "version": status["version"], "at": now()}
        reviews[item] = entry
    write_json(folder / REVIEW_FILE, reviews)
    if before["level"] != level:
        log(folder, "review", item=item, level=level, version=status["version"])
    sync_debt(root, tool, feature, oid, item, level, text, status["version"],
              hit.relative_to(Path(root)).as_posix() if hit else "")
    if level != PASS and DESIGN_RX.match(feature):      # only a picture that passes is an anchor
        path = studio_dir(root, "codex") / gd.ANCHORS_FILE
        anchors = [x for x in (gd._json(path) or []) if isinstance(x, dict)]
        kept = [x for x in anchors if (x.get("ref"), x.get("version"), x.get("item")) != (f"{feature}/{oid}", status["version"], item)]
        if len(kept) != len(anchors):
            write_json(path, kept)
    return tool, feature, oid, folder, entry


def copy_judged(root, tool, feature, oid, folder, data):
    """A feature's order: puts every item judged 1-3 into FeatureAssets/<deliver>/Generated/<id>/ (same name, so
    Unity keeps the .meta) and writes QUALITY.md for what is used with a flaw. Returns the folder."""
    status = read_status(folder)
    vdir = version_dir(root, tool, feature, oid, status["version"])
    dest = (Path(root) / "UnityProject" / "Assets" / "Features" / Path(*feature.split("/")) / "FeatureAssets"
            / data["fields"]["deliver"] / "Generated" / oid)
    reviews, flawed = read_reviews(folder), []
    quality = gd._json(vdir / QUALITY_FILE) or {}
    for item, _ in data["items"]:
        r = reviews.get(item)
        src = item_file(vdir, item)
        if not r or r["level"] in (None, REDO) or src is None:
            continue
        dest.mkdir(parents=True, exist_ok=True)
        for old in dest.glob(f"{item}.*"):      # the same item under another extension (its .meta goes with it)
            base = old.name[:-5] if old.name.endswith(".meta") else old.name
            if old.is_file() and base != src.name:
                old.unlink()
        shutil.copy2(src, dest / src.name)
        q = quality.get(item) if isinstance(quality.get(item), dict) else {}
        notes = []
        if r["level"] != PASS:      # a judgement of "Đạt" stands above the tool's own check
            notes = [level_name(r["level"]) + (f": {r['text']}" if r["text"] else "")] \
                + ([f"tool tự kiểm chưa đạt: {q.get('note')}"] if q and not q.get("ok") else [])
        if notes:
            flawed.append(f"- {item}: " + "; ".join(notes) + "\n")
    note = dest / "QUALITY.md"
    if flawed:
        note.write_text(f"# Hàng dùng tạm của order {feature}/{oid} v{status['version']}\n\n" + "".join(flawed), encoding="utf-8")
    elif note.is_file():
        note.unlink()
    return dest


def cmd_review(root, ref, item, level, text=""):
    if not re.fullmatch(r"[0-9]", level or "") or int(level) >= len(REVIEW_LEVELS):
        raise ValueError(f"mức chấm là số 0-{len(REVIEW_LEVELS) - 1}: " + ", ".join(f"{n} {name}" for n, name in enumerate(REVIEW_LEVELS)))
    tool, feature, oid, folder, entry = save_review(root, ref, item, int(level), text)
    data = parse_order(read_text(folder / ORDER_FILE))
    print(f"Đã chấm {ref} · {item}: {level_name(entry['level'])}" + (f" ({entry['text']})" if entry["text"] else "") + ".")
    kind = data["fields"].get("kind")
    if not DESIGN_RX.match(feature) and kind != "consult":
        dest = copy_judged(root, tool, feature, oid, folder, data)
        if entry["level"] != REDO:
            print(f"Đã chép vào {dest.relative_to(Path(root)).as_posix()}; refresh Unity để có .meta.")
    if entry["level"] == REDO:
        b = budget(folder, item)
        if b["left"]:
            print(f"Item được làm lại {RUNS_REDO} lượt. Nhắn @{tool.capitalize()} bằng SendMessage (to: \"{tool.capitalize()}\") "
                  f"để làm lại {ref} · {item}; phản hồi tự vào prompt.")
        else:
            print(f"CHƯA làm lại được: {b['why']}.")
    left = [i for i, _ in data["items"] if (read_reviews(folder).get(i) or {}).get("level") is None]
    if left:
        print(f"Còn chưa chấm: {', '.join(left)}.")
    return 0


# ---------------------------------------------------------------- quality and art debts
def self_check(work, item):
    """(ok, note) from work/<item>.check.md: first line 'đạt' or 'chưa đạt: <why>'; no file is not passing."""
    f = work / f"{item}.check.md"
    if not f.is_file() or not read_text(f).strip():
        return False, "chưa tự kiểm (thiếu work/" + f.name + ")"
    first = read_text(f).strip().split("\n")[0].strip()
    if first.lower() == CHECK_PASS:
        return True, ""
    return False, first.split(":", 1)[1].strip() if ":" in first else first


def read_debts(root):
    value = gd._json(studio_dir(root) / DEBT_FILE)
    return [d for d in value if isinstance(d, dict)] if isinstance(value, list) else []


def write_debts(root, debts):
    write_json(studio_dir(root) / DEBT_FILE, debts)


def open_debt(debts, tool, ref, owner, item, version, file, reason, kind):
    n = 1 + max([int(str(d.get("id", "n-0"))[2:] or 0) for d in debts if str(d.get("id", "")).startswith("n-")] or [0])
    debt = {"id": f"n-{n}", "tool": tool, "ref": ref, "owner": owner, "item": item, "version": version, "file": file,
            "reason": reason, "kind": kind, "state": "open", "at": now(), "closedBy": "", "closedAt": ""}
    debts.append(debt)
    return debt


def cmd_debt_close(root, did):
    debts = read_debts(root)
    hit = next((d for d in debts if d.get("id") == did), None)
    if hit is None:
        raise ValueError(f"không có phiếu nợ {did}")
    if hit["state"] != "open":
        raise ValueError(f"phiếu {did} đã đóng ({hit.get('closedBy')})")
    hit.update(state="closed", closedBy="người chấp nhận nợ", closedAt=now())
    write_debts(root, debts)
    print(f"Đã đóng phiếu nợ {did} ({hit['owner']} · {hit['item']}).")
    return 0


def cmd_pick(root, ref, item, n):
    """Puts run n of an item back into work/out: no tool runs, no run is spent."""
    _, feature, oid, folder = find_order(root, ref)
    work = folder / "work"
    hits = sorted((work / "attempts").glob(f"{item}-{n}.*")) if n.isdigit() else []
    if not hits:
        have = sorted(p.name for p in (work / "attempts").glob(f"{item}-*")) if (work / "attempts").is_dir() else []
        raise ValueError(f"không có lượt {n} của item '{item}'. Có: {', '.join(have) or 'chưa có lượt nào'}")
    (work / "out").mkdir(parents=True, exist_ok=True)
    set_aside([p for p in (work / "out").glob(f"{item}.*")], work, "_old")
    dest = work / "out" / f"{item}{hits[0].suffix}"
    shutil.copy2(hits[0], dest)
    log(folder, "pick", item=item, n=int(n))
    print(f"Đã chọn lượt {n}: work/out/{dest.name}. Ghi work/{item}.check.md rồi deliver.")
    return 0


def design_order(root, ref):
    tool, feature, oid, folder = find_order(root, ref, "codex")
    if not DESIGN_RX.match(feature):
        raise ValueError(f"{ref} không phải order art của node thiết kế")
    return feature, oid, folder


def cmd_anchor(root, ref, item):
    """Marks (or unmarks) one picture judged PASS as an anchor of its styles: later art runs of those styles see it."""
    feature, oid, folder = design_order(root, ref)
    status = read_status(folder)
    if status["state"] != "delivered" or (read_reviews(folder).get(item) or {}).get("level") != PASS:
        raise ValueError(f"chỉ ảnh được chấm '{REVIEW_LEVELS[PASS]}' mới làm mốc được")
    version = status["version"]
    vdir = Path(root) / RESOURCES / Path(*feature.split("/")) / "codex" / oid / f"v{version}"
    hit = next((p for p in vdir.iterdir() if p.is_file() and p.stem == item and p.name != gd.ART_META), None) if vdir.is_dir() else None
    if hit is None:
        raise ValueError(f"bản v{version} không có ảnh '{item}'")
    meta = gd._json(vdir / gd.ART_META) or {}
    path = studio_dir(root, "codex") / gd.ANCHORS_FILE
    anchors = [a for a in (gd._json(path) or []) if isinstance(a, dict)]
    key = (f"{feature}/{oid}", version, item)
    kept = [a for a in anchors if (a.get("ref"), a.get("version"), a.get("item")) != key]
    on = len(kept) == len(anchors)
    if on:
        kept.append({"ref": key[0], "version": version, "item": item, "styles": sorted(meta.get("styles") or {}),
                     "file": hit.relative_to(Path(root)).as_posix(), "at": now()})
    write_json(path, kept)
    log(folder, "anchor" if on else "unanchor", version=version, item=item)
    print(f"{'Đã đặt' if on else 'Đã bỏ'} ảnh {item} (v{version}) làm mốc style.")
    return 0


# ---------------------------------------------------------------- reading
def item_rows(folder, data):
    """[{id, text, level, feedback, used, allowed, left, round, why, redo}] of an order's items. redo: the item may be
    made again now (judged REDO with a feedback text, rounds left) or is being made again."""
    reviews = read_reviews(folder)
    delivered = read_status(folder)["version"] > 0
    rows = []
    for i, text in data["items"]:
        b = budget(folder, i)
        r = reviews.get(i) or {"level": None, "text": ""}
        rows.append({"id": i, "text": text, "level": r["level"] if delivered else None, "feedback": r["text"] if delivered else "",
                     "used": b["used"], "allowed": b["allowed"], "left": b["left"], "round": b["round"], "why": b["why"],
                     "redo": delivered and b["left"] > 0})
    return rows


def summary(root, tool=None):
    """[(ref, tool, state, version, count, title, problems, items)] of the orders; state as view_state gives it,
    items as item_rows gives them."""
    out = []
    for t, feature, oid, folder in all_orders(root):
        if tool and t != tool:
            continue
        data, problems = check_order(root, t, feature, oid, folder)
        status = read_status(folder)
        state = view_state(status, [i for i, _ in data["items"]], read_reviews(folder))
        out.append((f"{feature}/{oid}", t, state, status["version"], len(data["items"]), data["title"],
                    problems, item_rows(folder, data)))
    return out


def pending_line(root, tool):
    """One line for the tool chat's prompt: the orders with work for it (not delivered yet, or with an item that may
    be made again)."""
    try:
        rows = [r for r in summary(root, tool) if r[2] in ("new", "doing") or any(i["redo"] for i in r[7])]
    except OSError:
        return ""
    if not rows:
        return " Không có order nào đang chờ."
    def one(r):
        redo = [i["id"] for i in r[7] if i["redo"]]
        return f"{r[0]} ({VIEW_STATES[r[2]]}, " + (f"làm lại: {', '.join(redo)}" if redo and r[3] else f"{r[4]} item") + ")"
    return f" {len(rows)} order đang chờ: " + "; ".join(one(r) for r in rows[:8])


def art_line(root):
    """One line for @GameDesigner's prompt: where its art orders stand, and what it should tell @Codex or the human."""
    try:
        rows = [r for r in summary(root, "codex") if DESIGN_RX.match(r[0].rsplit("/", 1)[0])]
    except OSError:
        return ""
    parts = []
    for ref, _, state, version, _, _, _, items in rows:
        redo = [i["id"] for i in items if i["redo"]]
        mute = [i["id"] for i in items if version and i["level"] == REDO and not i["feedback"]]
        stuck = [i["id"] for i in items if version and state == "redo" and i["level"] == REDO and i["feedback"] and not i["redo"]]
        if state in ("new", "doing") and not version:
            parts.append(f"{ref}: chưa giao (đã nhắn @Codex chưa?)")
        elif state == "doing":
            parts.append(f"{ref}: @Codex đang làm lại")
        elif redo or mute or stuck:
            parts.append(f"{ref}: " + "; ".join(([f"làm lại được: {', '.join(redo)}"] if redo else [])
                                                + ([f"'{REVIEW_LEVELS[REDO]}' nhưng THIẾU PHẢN HỒI: {', '.join(mute)}"] if mute else [])
                                                + ([f"'{REVIEW_LEVELS[REDO]}' nhưng HẾT LƯỢT làm lại: {', '.join(stuck)}"] if stuck else [])))
        elif state == "review":
            parts.append(f"{ref}: chờ người chấm")
    return (" Order art: " + " | ".join(parts[:8]) + ".") if parts else ""


def snapshot(root):
    """Everything the Studio tab shows: every order (state, round, runs per item, quality of its last delivery) and
    the art debts. Reads only."""
    orders = []
    for tool, feature, oid, folder in all_orders(root):
        try:
            data, problems = check_order(root, tool, feature, oid, folder)
        except (OSError, ValueError) as ex:
            data, problems = {"items": [], "title": oid, "fields": {}}, [str(ex)]
        status = read_status(folder)
        vdir = Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{status['version']}"
        quality = gd._json(vdir / QUALITY_FILE) or {} if status["version"] else {}
        items = []
        for row in item_rows(folder, data):
            q = quality.get(row["id"]) if isinstance(quality.get(row["id"]), dict) else None
            items.append({**row, "levelName": level_name(row["level"]) if status["version"] else "",
                          "ok": q.get("ok") if q else None, "note": q.get("note", "") if q else "",
                          "file": f"{vdir.relative_to(Path(root)).as_posix()}/{q['file']}" if q and q.get("file") else ""})
        design = bool(DESIGN_RX.match(feature))
        state = view_state(status, [i["id"] for i in items], read_reviews(folder))
        orders.append({"ref": f"{feature}/{oid}", "tool": tool, "kind": data["fields"].get("kind", ""), "title": data["title"],
                       "owner": feature, "design": design, "node": node_of(feature) if design else "",
                       "state": state, "stateName": VIEW_STATES[state], "version": status["version"],
                       "maxRedos": MAX_REDOS, "items": items, "problems": problems, "at": status["at"]})
    return {"orders": orders, "debts": read_debts(root), "levels": list(REVIEW_LEVELS), "levelNone": REVIEW_NONE,
            "states": VIEW_STATES, "limits": {"first": RUNS_FIRST, "redo": RUNS_REDO, "maxRedos": MAX_REDOS,
                                              "artDaily": ART_DAILY_MAX}, "generatedAt": now()}


def cmd_status(root, arg=""):
    if arg and arg not in TOOLS:
        tool, feature, oid, folder = find_order(root, arg)
        rows = [r for r in summary(root, tool) if r[0] == f"{feature}/{oid}"]
    else:
        rows = summary(root, arg or None)
    if not rows:
        print("Chưa có order nào.")
    debts = [d for d in read_debts(root) if d["state"] == "open"]
    if debts and not (arg and arg not in TOOLS):
        print(f"Phiếu nợ đang mở ({len(debts)}):")
        for d in debts:
            print(f"- {d['id']} {d['tool']} {d['owner']} · {d['item']} (v{d['version']}): {d['kind']}: {d['reason']}")
    for ref, tool, state, version, count, title, problems, items in rows:
        print(f"- {tool} {ref}: {VIEW_STATES[state]}" + (f", đã giao v{version}" if version else "") + f", {count} item · {title}")
        for i in items:
            line = f"    {i['id']}: đã chạy {i['used']} lượt, còn {i['left']}, làm lại {i['round']}/{MAX_REDOS}"
            if version:
                line += f" · {level_name(i['level'])}" + (f" · phản hồi: {i['feedback']}" if i["feedback"] else "")
                if i["redo"]:
                    line += " · LÀM LẠI ĐƯỢC (1 lượt)"
                elif i["level"] == REDO:
                    line += f" · CHƯA LÀM LẠI ĐƯỢC: {i['why']}"
            print(line)
        for p in problems:
            print(f"    LỖI ORDER: {p}")
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
        if len(args) in (4, 5) and args[0] == "review":
            return cmd_review(root, args[1], args[2], args[3], args[4] if len(args) == 5 else "")
        if args[:1] and args[0] in ("take", "reject", "approve", "accept"):
            raise ValueError(f"lệnh '{args[0]}' đã bỏ: chấm từng item bằng `studio.py review <F>/<id> <item> <0-3> "
                             "\"<phản hồi>\"` (ảnh concept: người chấm trên tab Game design)")
        if len(args) == 3 and args[0] == "anchor":
            return cmd_anchor(root, args[1], args[2])
        if len(args) == 4 and args[0] == "pick":
            return cmd_pick(root, args[1], args[2], args[3])
        if len(args) == 3 and args[:2] == ["debt", "close"]:
            return cmd_debt_close(root, args[2])
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
