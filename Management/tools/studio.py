#!/usr/bin/env python3
"""Orders from the features to the tool chats (@Codex: images and GPT consults; @Blender: 3D models), and the one
way Codex and Blender are run. Every run, delivery and refusal is kept, so a mistake is learnt once.

    python Management/tools/studio.py check                          every order is well formed (any chat)
    python Management/tools/studio.py status [codex|blender|<F>/<id>] what waits, what is done (any chat)
    python Management/tools/studio.py measure <F>/<id> [<item>]       size and transparency of the results (any chat)
    python Management/tools/studio.py codex <F>/<id> <item>           @Codex: one image for one item
    python Management/tools/studio.py consult <F>/<id> <item>         @Codex: ask GPT one question
    python Management/tools/studio.py blender <F>/<id> <item>         @Blender: run work/<item>.py headless
    python Management/tools/studio.py claude <F>/<id> <item>          @ClaudeImage: work/<item>.svg (written by the chat) into a PNG
    python Management/tools/studio.py redo <_design/...>/<id> <frame> "<why>"   a Leader: one more drawing run of a delivered claude concept
    python Management/tools/studio.py deliver <codex|blender|claudeimage> <F>/<id>  the tool chat: hand the order over
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

Claude concepts and Claude's images (tool: claudeimage, the @ClaudeImage chat): the chat draws each item as an SVG in
work/<item>.svg; `claude` checks it (one drawing, nothing outside its file, no script), counts one run and makes
work/out/<item>.png at the size of the order's format line, background transparent (svg_to_png). kind: image for a
feature (enough for the IDEA phase), kind: art for a cheat node (the claude concept: same gates, same judging and
meta.json as an art concept, kept apart from it under .../claudeimage/...; no daily limit, no anchors).
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

CLAUDE = gd.CLAUDE_TOOL            # "claudeimage": the @ClaudeImage chat draws an SVG, this code makes the PNG
TOOLS = {"codex": ("image", "consult", "art"), "blender": ("model",), CLAUDE: ("image", "art")}
TOOL_CHATS = {"codex": "Codex", "blender": "Blender", CLAUDE: "ClaudeImage"}      # the chat that works for a tool
SVG_MAX = 2 * 1024 * 1024
SVG_BANNED = ("script", "foreignObject", "iframe", "object", "embed", "audio", "video")
BROWSERS = ("chrome", "google-chrome", "chromium", "chromium-browser", "msedge")
BROWSER_PATHS = (r"C:\Program Files\Google\Chrome\Application\chrome.exe",
                 r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                 r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                 r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                 "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
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
           "vị trí và tỉ lệ của frame, khung hình và hướng màn; chỉ thay hình cơ bản bằng art theo style. Giữ nguyên "
           "các chữ hiển thị trong frame: đúng từng chữ và dấu tiếng Việt, đúng vị trí, kiểu chữ không chân gọn như "
           "Roboto, màu chữ theo style. Không thêm chữ nào khác, không thêm số đo hay đường kẻ.")
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
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")      # its own name: several runs may write at once
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


def check_order(root, tool, feature, oid, folder, live=None):
    """The problems of one order (folder, front matter, content). `live`: the live tree already read (live_tree), for a
    caller that checks many orders; without it the tree is read here."""
    data = parse_order(read_text(folder / ORDER_FILE))
    problems = list(data["errors"])
    if data["fields"].get("tool") and data["fields"]["tool"] != tool:
        problems.append(f"order nằm trong thư mục {tool} nhưng ghi tool: {data['fields']['tool']}")
    design = bool(DESIGN_RX.match(feature))
    kind = data["fields"].get("kind", "")
    if design:
        problems += check_art(root, tool, feature, oid, folder, data, live)
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


def live_tree(root):
    """The live tree, read the way the page reads it: (root node or None, {path: node})."""
    tree, _ = gd.read_tree(root, "live")
    return tree, ({n["path"]: n for n in gd.walk(tree)} if tree else {})


def live_node(root, path, live=None):
    """(node, root node) of the live tree; node None when live lacks it. `live`: a tree already read (live_tree)."""
    _, nodes = live or live_tree(root)
    return nodes.get(path), nodes.get("")


def order_styles(data):
    return [s.strip() for s in re.split(r"[,\s]+", data["fields"].get("style", "")) if s.strip()]


def check_art(root, tool, feature, oid, folder, data, live=None):
    """The problems of an art order: a cheat node in live with frames, one item per state (its frame), known styles, and no frame
    that already has a picture still good from the same tool (unless this order is the one that made it). Art
    concepts (codex) and claude concepts (claudeimage) are apart: one does not stand in the other's way."""
    problems = []
    if tool not in gd.ART_TOOLS or data["fields"].get("kind") not in ("", "art"):
        problems.append(f"order của node thiết kế chỉ là kind: art, tool: {' hoặc '.join(gd.ART_TOOLS)}")
    if data["fields"].get("deliver") and data["fields"]["deliver"] != ART_DELIVER:
        problems.append(f"order art ghi deliver: {ART_DELIVER} (ảnh không vào Assets)")
    path = node_of(feature)
    cheat = is_cheat(path)
    if not cheat and tool != CLAUDE:      # an art concept of Codex: the cheat node only; a claude concept: any frame
        return problems + [f"order art chỉ đặt cho node cheat (<feature>/{gd.CHEAT_KEY}) đã ở live, không cho '{path}'"]
    node, top = live_node(root, path, live)
    if node is None:
        return problems + [f"node '{path}' chưa có trong live: chỉ gen art cho node đã chốt"]
    frames = {Path(fr["file"]).stem for fr in node["frames"]}
    if not frames:
        problems.append(f"node '{path}' trong live chưa có frame: " + ("viết frame cho từng state và chốt trước" if cheat else
                                                                    "claude-concept chỉ đặt cho node có frame"))
    for item, _ in data["items"]:
        if frames and item not in frames:
            problems.append((f"item '{item}' phải là id một state của node cheat" if cheat else f"item '{item}' phải là tên một frame của node")
                            + f" ({', '.join(sorted(frames))})")
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
        tree = (live or live_tree(root))[0]
        art, _ = gd.read_art(root, tree)
        good = {e["frame"]: e["ref"] for e in art.get(path, []) if e["usable"] and e["tool"] == tool}
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
        opened = sum(1 for e in log_[last + 1:] if mine(e, "redo") or e.get("event") == "reject")      # a Leader may open more than one
        left = max(0, opened * RUNS_REDO - since)
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


def browser_exe():
    """A Chrome or Edge that can draw a page without a window (STUDIO_BROWSER overrides), or ''."""
    override = os.environ.get("STUDIO_BROWSER")
    if override is not None:
        return override
    return next((shutil.which(n) for n in BROWSERS if shutil.which(n)), "") or next((p for p in BROWSER_PATHS if Path(p).is_file()), "")


def svg_box(text):
    """(width, height) of an SVG's viewBox, or None."""
    m = re.search(r"<svg\b[^>]*?\bviewBox\s*=\s*[\"']\s*([-+.\deE]+)[\s,]+([-+.\deE]+)[\s,]+([-+.\deE]+)[\s,]+([-+.\deE]+)\s*[\"']", text)
    try:
        w, h = float(m.group(3)), float(m.group(4))
    except (AttributeError, ValueError):
        return None
    return (w, h) if w > 0 and h > 0 else None


def svg_converters():
    """The names of the ways this machine has to draw an SVG into a PNG, in the order svg_to_png tries them."""
    return ([n for n, ok in (("STUDIO_SVG2PNG", os.environ.get("STUDIO_SVG2PNG")), ("rsvg-convert", shutil.which("rsvg-convert")),
                             ("inkscape", shutil.which("inkscape")), ("magick", shutil.which("magick"))) if ok]
            + (["browser"] if browser_exe() else []))


def svg_to_png(svg_path, png_path, size=None):
    """Draws an SVG into a PNG with whatever the machine has: STUDIO_SVG2PNG (a command or a .py taking <in> <out>
    [<width> <height>]), rsvg-convert, inkscape, magick, then Chrome or Edge without a window. size: (width, height)
    in px; without it the PNG is 1920 px wide and keeps the shape of the viewBox. The background stays
    transparent. Returns the converter used, or '' when none worked."""
    svg_path, png_path = Path(svg_path), Path(png_path)
    box = svg_box(read_text(svg_path)) or (16, 9)
    w, h = size or (1920, max(1, round(1920 * box[1] / box[0])))
    override = os.environ.get("STUDIO_SVG2PNG")
    tries = []
    if override:
        tries.append(([sys.executable, override] if override.endswith(".py") else [override]) + [str(svg_path), str(png_path)]
                     + ([str(w), str(h)] if size else []))
    if shutil.which("rsvg-convert"):
        tries.append(["rsvg-convert", "-w", str(w), "-h", str(h), "-b", "none", "-o", str(png_path), str(svg_path)])
    if shutil.which("inkscape"):
        tries.append(["inkscape", str(svg_path), "-o", str(png_path), "-w", str(w), "-h", str(h)])
    if shutil.which("magick"):      # not `convert`: on Windows that name is a system command
        tries.append(["magick", "-background", "none", str(svg_path), "-resize", f"{w}x{h}!", str(png_path)])
    for cmd in tries:
        code, _ = run(cmd)
        if code == 0 and png_path.is_file():
            return cmd[0]
    browser = browser_exe()
    if browser:
        # The SVG is shown as a picture (scripts and outside files are not loaded), at a fixed size at the top left.
        page = png_path.with_name(png_path.name + ".page.html")
        page.write_text("<!doctype html><html><head><meta charset=\"utf-8\"><style>html,body{margin:0;padding:0;"
                        f"background:transparent;overflow:hidden}}img{{display:block;width:{w}px;height:{h}px}}</style></head>"
                        f"<body><img src=\"{svg_path.resolve().as_uri()}\"></body></html>", encoding="utf-8")
        try:
            if png_path.exists():
                png_path.unlink()
            code, _ = run(([sys.executable, browser] if browser.endswith(".py") else [browser])
                          + ["--headless", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--force-device-scale-factor=1",
                             f"--window-size={w},{h}", "--default-background-color=00000000",
                             f"--screenshot={png_path.resolve()}", page.resolve().as_uri()])
        finally:
            try:
                page.unlink()
            except OSError:
                pass
        if png_path.is_file():
            return "browser"
    return ""


def check_svg(text):
    """The problems of an SVG the @ClaudeImage chat wrote: it must be one drawing that needs nothing outside its
    file and runs nothing."""
    import xml.etree.ElementTree as ET
    if len(text.encode("utf-8")) > SVG_MAX:
        return [f"file SVG lớn quá {SVG_MAX // 1024} KB"]
    if re.search(r"<!(DOCTYPE|ENTITY)", text, re.I):
        return ["SVG không được có DOCTYPE hay ENTITY"]
    try:
        top = ET.fromstring(text)
    except ET.ParseError as ex:
        return [f"SVG không phải XML hợp lệ ({ex})"]
    name = lambda tag: tag.rsplit("}", 1)[-1] if isinstance(tag, str) else ""      # noqa: E731
    if name(top.tag) != "svg" or not top.tag.startswith("{http://www.w3.org/2000/svg}"):
        return ["gốc phải là <svg xmlns=\"http://www.w3.org/2000/svg\" ...>"]
    problems = []
    if not svg_box(text):
        problems.append("thẻ <svg> thiếu viewBox=\"0 0 <rộng> <cao>\" (rộng, cao lớn hơn 0)")
    for el in top.iter():
        tag = name(el.tag)
        if tag in SVG_BANNED:
            problems.append(f"không dùng thẻ <{tag}>")
        if tag == "style" and re.search(r"@import|url\(\s*[\"']?(?!#)", el.text or "", re.I):
            problems.append("<style> không được @import hay url() trỏ ra ngoài file")
        for key, value in el.attrib.items():
            attr, value = name(key), str(value).strip()
            if attr.lower().startswith("on"):
                problems.append(f"không dùng thuộc tính sự kiện {attr}")
            elif attr == "href" and not (value.startswith("#") or value.startswith("data:image/")):
                problems.append(f"<{tag}> href trỏ ra ngoài file ({value[:40]}): chỉ '#id' hoặc data:image/")
            elif re.search(r"url\(\s*[\"']?(?!#)", value, re.I) or re.match(r"(?i)\s*javascript:", value):
                problems.append(f"<{tag}> {attr} trỏ ra ngoài file: chỉ url(#id)")
    return list(dict.fromkeys(problems))[:8]


def frame_css():
    """The colours of a drawn frame without the page's variables, with the font inside (a picture loads no file)."""
    return gd.font_css(embed=True) + "\n" + re.sub(r"var\(--[\w-]+, ((?:[^()]|\([^()]*\))*)\)", r"\1", gd.FRAME_CSS).split("\n", 1)[1]


def with_font(svg_path, dest):
    """The SVG to draw: itself, or a copy at dest with the project's font put inside when its text asks for it."""
    text = read_text(svg_path)
    css = gd.font_css(embed=True) if gd.FONT_NAME in text else ""
    m = re.search(r"<svg\b[^>]*>", text) if css else None
    if not m:
        return Path(svg_path)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(text[:m.end()] + f"<style>{css}</style>" + text[m.end():], encoding="utf-8")
    return dest


def frame_texts(fr):
    """The texts the player reads in a frame, in the order of its shapes."""
    return [t for t in (gd.display_text(r) for r in fr["shapes"]) if t]


def frame_png(root, path, frame, dest):
    """The frame of the live node drawn into dest/<frame>.frame.png (colours fixed, no page variables)."""
    node, _ = live_node(root, path)
    fr = next((f for f in (node or {}).get("frames", []) if Path(f["file"]).stem == frame), None)
    if fr is None:
        return None
    # As a concept shows it: the texts the player reads in the project's font, no numbers and no notes to copy.
    return frame_png_file(fr, dest, f"{frame}.frame", concept=True)


def frame_png_file(fr, dest, stem, concept=True):
    """One resolved frame drawn into dest/<stem>.png (and the .svg beside it), colours fixed and the font inside.
    concept: only what the player reads (how a concept shows it); otherwise with the numbers and the notes.
    Returns the PNG path, or None when this machine cannot draw an SVG into a PNG."""
    svg = gd.frame_svg(fr, concept=concept).replace("</defs>", f"</defs><style>{frame_css()}</style>", 1)
    dest = Path(dest)
    dest.mkdir(parents=True, exist_ok=True)
    svg_path, png_path = dest / f"{stem}.svg", dest / f"{stem}.png"
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
        notes.append("không đổi được frame sang PNG (cần rsvg-convert, inkscape, magick, Chrome hoặc Edge): ảnh sẽ không bám bố cục")
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
    node, _ = live_node(root, path)
    shown = frame_texts(frame_named(node, item) or {"shapes": []})
    if shown:      # written out, so the model does not have to read them off the picture
        text += ("\n\n" if text else "") + "Chữ phải hiện trong ảnh, đúng từng chữ và dấu, ở đúng chỗ của frame: " + "; ".join(f"\"{t}\"" for t in shown)
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


def view_state(status, items, reviews, unjudged=False):
    """What an order waits for, from its status and the judgement of its items:
    new | doing | review (an item is not judged yet) | redo (an item is judged REDO) | done."""
    if status["state"] != "delivered":
        return status["state"]
    if unjudged:      # a claude concept: nobody judges it, a delivery is the end
        return "done"
    levels = [(reviews.get(i) or {}).get("level") for i in items]
    return "redo" if REDO in levels else "review" if None in levels else "done"


def no_judging(tool, feature):
    """A claude concept (an order of a design node made by the @ClaudeImage chat) is not judged by anybody."""
    return tool == CLAUDE and bool(DESIGN_RX.match(feature))


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


CLAUDE_RUNS = 5      # runs of `claude` at the same time: the @ClaudeImage chat may work with that many agents


def take_lock(path):
    """True when this process now holds the lock file (one left by a run that died is dropped after RUN_LOCK_HOURS)."""
    path.parent.mkdir(parents=True, exist_ok=True)
    try:
        if path.exists() and time.time() - path.stat().st_mtime > RUN_LOCK_HOURS * 3600:
            path.unlink()
        fd = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except OSError:
        return False
    os.write(fd, str(os.getpid()).encode())
    os.close(fd)
    return True


class ClaudeLock:
    """A run of `claude`: one of CLAUDE_RUNS places of the tool, and the item itself (no two runs of one item)."""
    def __init__(self, root, folder, item):
        self.slots = [studio_dir(root, CLAUDE) / f".run-{n}.lock" for n in range(1, CLAUDE_RUNS + 1)]
        self.item = folder / f".{item}.lock"      # beside order.md: not a file the tool chat writes
        self.held = []

    def __enter__(self):
        if not take_lock(self.item):
            raise ValueError("item này đang được một lần chạy khác làm; mỗi agent một frame hoặc một item")
        self.held.append(self.item)
        slot = next((s for s in self.slots if take_lock(s)), None)
        if slot is None:
            self.__exit__()
            raise ValueError(f"đang có {CLAUDE_RUNS} lần chạy của ClaudeImage (tối đa {CLAUDE_RUNS} cùng lúc); chờ một lần xong rồi chạy lại")
        self.held.append(slot)
        return self

    def __exit__(self, *exc):
        for p in self.held:
            try:
                p.unlink()
            except OSError:
                pass
        self.held = []


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
    if DESIGN_RX.match(feature) and tool == gd.ART_TOOL:      # the daily limit is on Codex runs; a claude concept has none
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


# ---------------------------------------------------------------- claude concepts: parts of a frame
PARTS_DIR, PARTS_FILE = "parts", "parts.json"
PART_RATIO = 0.02      # how far the shape of a picture that is not 9-sliced may be from the shape it stands for
PART_MAX = 4096


def frame_named(node, name):
    return next((f for f in (node or {}).get("frames", []) if Path(f["file"]).stem == name), None)


def is_cheat(path):
    parts = path.split("/")
    return len(parts) == gd.CHEAT_ROUND - 1 and parts[-1] == gd.CHEAT_KEY


def delivered_parts(root, tree, path):
    """{frame: {ref, version, dir, parts}} of the claude concepts of a node that are still good (newest delivery,
    not stale, not judged "Làm lại ngay"); dir holds <part>.png and <part>.svg."""
    art, _ = gd.read_art(root, tree)
    out = {}
    for e in art.get(path, []):
        if e["tool"] == CLAUDE and e["usable"] and e["frame"] not in out:
            folder = (Path(root) / e["file"]).parent / PARTS_DIR / e["frame"]
            table = gd._json(folder / PARTS_FILE)
            if isinstance(table, dict) and isinstance(table.get("parts"), list):
                out[e["frame"]] = {"ref": e["ref"], "version": e["version"], "dir": folder, "parts": table["parts"]}
    return out


fits = gd.part_fits      # '' when a picture can stand in a place, else why not (the page's reading uses the same rule)


def read_parts(root, tree, node, fr, frame, work):
    """(parts, problems) of work/<frame>.parts.json, the table of pictures the @ClaudeImage chat draws for a frame:
        {"parts": [{"id", "w", "h", "slice": [left, top, right, bottom] | null, "shapes": [shape ids]},
                   {"id", "same-as": "<node path>#<part id>", "shapes": [...]}]}
    Every shape the node itself talks about (role own) that a picture can stand for is in exactly one part; a part
    with several shapes is one picture used in several places. Each part comes back with svg and png (paths; png
    None for a part still to be made) and `from` (where a same-as part was taken)."""
    raw = gd._json(work / f"{frame}.parts.json")
    if not isinstance(raw, dict) or not isinstance(raw.get("parts"), list):
        return [], [f"thiếu work/{frame}.parts.json, hoặc nó không có dạng {{\"parts\": [...]}} (bảng phần của frame; xem "
                    ".claude/rules/studio.md)"]
    shapes = {r["id"]: r for r in fr["shapes"]}
    need = [r["id"] for r in fr["shapes"] if r["role"] == gd.ROLE_OWN and r["shape"] in gd.PART_SHAPES]
    parts, problems, taken, cache = [], [], {}, {}
    for n, p in enumerate(raw["parts"], 1):
        if not isinstance(p, dict) or not ID_RX.match(str(p.get("id") or "")):
            problems.append(f"phần thứ {n}: thiếu id (chữ thường, số, gạch nối)")
            continue
        pid = p["id"]
        if any(x["id"] == pid for x in parts):
            problems.append(f"phần '{pid}' khai hai lần")
            continue
        part = {"id": pid, "w": 0, "h": 0, "slice": None, "shapes": [], "svg": None, "png": None, "from": None,
                "text": p.get("text") is not False}      # false: the picture has its text drawn in, the tool adds none
        ids = p.get("shapes") if isinstance(p.get("shapes"), list) else []
        if not ids:
            problems.append(f"phần '{pid}': 'shapes' phải nêu ít nhất một hình của frame")
        if p.get("same-as"):
            path, _, other = str(p["same-as"]).partition("#")
            if path not in cache:
                cache[path] = delivered_parts(root, tree, path)
            hit = next(((fname, d, q) for fname, d in sorted(cache[path].items()) for q in d["parts"] if q.get("id") == other), None)
            if not hit or path == node["path"]:
                problems.append(f"phần '{pid}': same-as '{p['same-as']}' không trỏ tới phần nào trong claude-concept còn dùng "
                                "được của node khác (dạng <đường dẫn node>#<id phần>)")
                continue
            fname, d, q = hit
            part.update(w=q["w"], h=q["h"], slice=q.get("slice"), svg=d["dir"] / f"{other}.svg", png=d["dir"] / f"{other}.png",
                        **{"from": {"node": path, "frame": fname, "part": other, "ref": d["ref"], "version": d["version"]}})
        else:
            w, h, sl = p.get("w"), p.get("h"), p.get("slice")
            whole = lambda v: isinstance(v, int) and not isinstance(v, bool)      # noqa: E731
            if not (whole(w) and whole(h) and 0 < w <= PART_MAX and 0 < h <= PART_MAX):
                problems.append(f"phần '{pid}': w, h là số nguyên px từ 1 tới {PART_MAX}")
                continue
            if sl is not None and not (isinstance(sl, list) and len(sl) == 4 and all(whole(v) and v >= 0 for v in sl)
                                       and sl[0] + sl[2] < w and sl[1] + sl[3] < h):
                problems.append(f"phần '{pid}': slice là null hoặc [trái, trên, phải, dưới] (px, trái + phải nhỏ hơn w, trên + dưới nhỏ hơn h)")
                continue
            src = work / frame / f"{pid}.svg"
            part.update(w=w, h=h, slice=sl, svg=src)
            if not src.is_file():
                problems.append(f"phần '{pid}': chưa có work/{frame}/{pid}.svg")
            else:
                problems += [f"phần '{pid}': {x}" for x in check_svg(read_text(src))]
        for sid in ids:
            r = shapes.get(sid)
            if r is None:
                problems.append(f"phần '{pid}': frame không có hình '{sid}'")
            elif r["role"] != gd.ROLE_OWN:
                problems.append(f"phần '{pid}': hình '{sid}' là {gd.ROLE_NAMES[r['role']]} của frame này, không vẽ ở đây")
            elif not gd.shape_slots(fr, r):
                problems.append(f"phần '{pid}': hình '{sid}' ({r['shape']}) là nét đo, không có ảnh thay")
            elif sid in taken:
                problems.append(f"hình '{sid}' nằm trong hai phần ('{taken[sid]}' và '{pid}')")
            else:
                taken[sid] = pid
                part["shapes"].append(sid)
                why = next((x for x in (fits(part, s) for s in gd.shape_slots(fr, r)) if x), "")
                if why:
                    problems.append(f"phần '{pid}' cho hình '{sid}': {why}")
        parts.append(part)
    left = [sid for sid in need if sid not in taken]
    if left:
        problems.append("các hình của node chưa có phần: " + ", ".join(
            f"{sid} ({gd.shape_slots(fr, shapes[sid])[0][2]:g}x{gd.shape_slots(fr, shapes[sid])[0][3]:g}px)" for sid in left))
    return parts, problems[:12]


def compose_frame(fr, placed):
    """(svg text, (width, height)) of the picture of a whole frame: every shape in `placed` ({shape id: part with
    png}) is its picture at the place and size the frame gives (a 9-sliced one keeps its corners, edges and middle
    stretch), the backdrop is left out, every other shape stays the basic shape of the frame."""
    import base64
    k = gd.frame_scale(fr)
    vw, vh = round((fr["view"][2] - fr["view"][0]) * k, 3), round((fr["view"][3] - fr["view"][1]) * k, 3)
    css = frame_css()
    defs, body, seen = [], [], {}
    for r in fr["shapes"]:
        if r["role"] == gd.ROLE_BACKDROP:
            continue
        part = placed.get(r["id"])
        if part is None:      # kept as the frame has it: its basic shape, and the text the player reads
            layer = gd.frame_svg(fr, only={r["id"]}, bare=True, concept=True)
            body.append(layer.replace('class="fr-svg"', f'x="0" y="0" width="{vw:g}" height="{vh:g}"', 1))
            continue
        key = str(part["png"])
        if key not in seen:
            seen[key] = f"part-{len(seen) + 1}"
            data = base64.b64encode(Path(part["png"]).read_bytes()).decode("ascii")
            defs.append(f'<image id="{seen[key]}" width="{part["w"]}" height="{part["h"]}" preserveAspectRatio="none" '
                        f'href="data:image/png;base64,{data}"/>')
        pw, ph = part["w"], part["h"]
        for x, y, w, h in gd.shape_slots(fr, r):
            if part["slice"]:
                l, t, rr, b = part["slice"]
                cols = [(0, l, x, l), (l, pw - l - rr, x + l, w - l - rr), (pw - rr, rr, x + w - rr, rr)]
                rows = [(0, t, y, t), (t, ph - t - b, y + t, h - t - b), (ph - b, b, y + h - b, b)]
            else:
                cols, rows = [(0, pw, x, w)], [(0, ph, y, h)]
            for sx, sw, dx, dw in cols:
                for sy, sh, dy, dh in rows:
                    if sw > 0 and sh > 0 and dw > 0 and dh > 0:
                        body.append(f'<svg x="{dx:g}" y="{dy:g}" width="{dw:g}" height="{dh:g}" viewBox="{sx:g} {sy:g} {sw:g} {sh:g}" '
                                    f'preserveAspectRatio="none"><use href="#{seen[key]}"/></svg>')
            text = gd.display_text(r) if part.get("text", True) else ""
            if text:      # the text the player reads, over the picture: readable on any colour
                size = gd.fit_text(text, w, h)
                body.append(f'<text class="pt-text{" fr-strong" if r["shape"] == "button" else ""}" data-over="{r["id"]}" x="{x + w / 2:g}" '
                            f'y="{y + h / 2:g}" font-size="{size:g}" stroke-width="{size * 0.14:g}" text-anchor="middle" '
                            f'dominant-baseline="middle">{gd._esc(text)}</text>')
    css += ("\n.pt-text { font-family: Roboto, system-ui, sans-serif; fill: #1f2a37; stroke: #ffffff; paint-order: stroke; "
            "stroke-linejoin: round; }")
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw:g} {vh:g}"><style>{css}</style><defs>{"".join(defs)}</defs>'
            + "".join(body) + "</svg>"), (max(1, round(vw)), max(1, round(vh)))


def parts_summary(parts, fr):
    """Lines about a frame's parts: how many pictures, which are 9-sliced, which are used in several places."""
    shapes = {r["id"]: r for r in fr["shapes"]}
    lines = [f"{len(parts)} phần (vẽ mới {sum(1 for p in parts if not p['from'])}, lấy lại {sum(1 for p in parts if p['from'])})"]
    for p in parts:
        places = sum(len(gd.shape_slots(fr, shapes[s])) for s in p["shapes"] if s in shapes)
        lines.append(f"- {p['id']}: {p['w']}x{p['h']}px, " + (f"9-slice viền {p['slice']}" if p["slice"] else "không 9-slice")
                     + f", dùng ở {places} chỗ ({', '.join(p['shapes'])})"
                     + (f", lấy lại từ {p['from']['node']}#{p['from']['part']} ({p['from']['ref']} v{p['from']['version']})" if p["from"] else ""))
    return lines


def write_parts(work, frame, parts, fr, extra=None):
    """Puts every part's png and svg and the table into work/parts/<frame>/ (what deliver hands over)."""
    dest = work / PARTS_DIR / frame
    if dest.exists():
        set_aside([dest], work, "_old")
    dest.mkdir(parents=True, exist_ok=True)
    table = []
    for p in parts:
        for ext in ("png", "svg"):
            src = p[ext]
            if src and Path(src).is_file() and Path(src) != dest / f"{p['id']}.{ext}":
                shutil.copy2(src, dest / f"{p['id']}.{ext}")
        p["png"] = dest / f"{p['id']}.png"
        table.append({"id": p["id"], "w": p["w"], "h": p["h"], "slice": p["slice"], "shapes": p["shapes"], "from": p["from"],
                      "text": p.get("text", True)})
    kept = [r["id"] for r in fr["shapes"] if r["role"] != gd.ROLE_BACKDROP and not any(r["id"] in p["shapes"] for p in parts)]
    write_json(dest / PARTS_FILE, dict({"frame": frame, "parts": table, "kept": kept,
                                        "skipped": [r["id"] for r in fr["shapes"] if r["role"] == gd.ROLE_BACKDROP]}, **(extra or {})))
    return dest


def claude_frame(root, ref, item, folder, feature):
    """One frame of a node as a claude concept: the chat's parts become pictures and the whole frame is put together."""
    path = node_of(feature)
    tree, _ = gd.read_tree(root, "live")
    node = next((n for n in gd.walk(tree) if n["path"] == path), None) if tree else None
    fr = frame_named(node, item)
    if fr is None:
        raise ValueError(f"node '{path}' trong live không có frame '{item}'")
    work = folder / "work"
    parts, problems = read_parts(root, tree, node, fr, item, work)
    if problems:
        raise ValueError(f"bảng phần của frame '{item}' chưa dùng được (không tính lượt): " + "; ".join(problems))
    if not svg_converters():
        raise ValueError("máy này không đổi được SVG sang PNG: cần rsvg-convert, inkscape, magick, Chrome hoặc Edge "
                         "(hoặc đặt STUDIO_SVG2PNG). Dừng và báo người")
    oid = folder.name
    version = read_status(folder)["version"]
    fresh = fresh_parts(version_dir(root, CLAUDE, feature, oid, version) / PARTS_DIR / item, parts) if version else [p["id"] for p in parts]
    if version and not fresh:
        # Only the layout changed (or nothing): the delivered parts are put together again. Nothing is drawn, so no
        # run is counted and nobody has to order it.
        data, problems = check_order(root, CLAUDE, feature, oid, folder)
        if problems:
            raise ValueError("order chưa hợp lệ: " + "; ".join(problems[:4]))
        if item not in dict(data["items"]):
            raise ValueError(f"order không có item '{item}'. Có: {', '.join(i for i, _ in data['items'])}")
        save_status(folder, state="doing")
        attempt, event = 0, "build"
    else:
        if version and budget(folder, item)["left"] <= 0:
            raise ValueError(f"frame '{item}' đã giao (v{version}); vẽ mới sau khi giao cần lệnh của Leader (@GameDesigner, @CodeLead, "
                             f"@Manager hoặc người): python Management/tools/studio.py redo {ref} {item} \"<lí do>\". Phần mới hoặc đã "
                             f"đổi: {', '.join(fresh)}. Ghép lại từ các phần đã giao (bảng phần và SVG giữ nguyên) thì không cần lệnh")
        feature, oid, folder, data, text, work, refs = prepare(root, CLAUDE, ref, item, ("art",))
        attempt, event = start_attempt(folder, item), "run"
    stamp = time.strftime("%Y%m%d-%H%M%S")
    made = work / "_made" / item
    if made.exists():
        shutil.rmtree(made)
    made.mkdir(parents=True)
    for p in parts:
        if p["from"]:
            continue
        p["png"] = made / f"{p['id']}.png"
        if not svg_to_png(with_font(p["svg"], made / f"{p['id']}.font.svg"), p["png"], (p["w"], p["h"])):
            log(folder, "run", tool=CLAUDE, item=item, mode="none", exit=1, result=f"không đổi được phần {p['id']} sang PNG", files=[])
            print(f"Không đổi được work/{item}/{p['id']}.svg sang PNG (lượt {attempt} đã tính). Dừng và báo người.", file=sys.stderr)
            return 1
    write_parts(work, item, parts, fr)
    code = finish_frame(folder, work, item, fr, parts, attempt, stamp, event=event, ref=ref)
    if code == 0 and not attempt:
        print("Ghép lại từ các phần đã giao: không vẽ gì mới, không tính lượt. Deliver để có bản mới.")
    return code


def fresh_parts(delivered, parts):
    """The ids of the parts that are not what was delivered last: a new one, another size or border, or another
    drawing. A part taken over from another node is never new here."""
    table = gd._json(delivered / PARTS_FILE)
    old = {p.get("id"): p for p in (table or {}).get("parts", []) if isinstance(p, dict)} if isinstance(table, dict) else {}
    out = []
    for p in parts:
        if p["from"]:
            continue
        d, was = old.get(p["id"]), delivered / f"{p['id']}.svg"
        same = (d is not None and not d.get("from") and (d.get("w"), d.get("h"), d.get("slice")) == (p["w"], p["h"], p["slice"])
                and was.is_file() and was.read_bytes() == Path(p["svg"]).read_bytes())
        if not same:
            out.append(p["id"])
    return out


def cmd_redo(root, ref, item, text):
    """A Leader (or the human) lets the @ClaudeImage chat draw one frame of a delivered claude concept again: one
    run. Drawing after a delivery is never the tool chat's own decision."""
    tool, feature, oid, folder = find_order(root, ref, CLAUDE)
    if not DESIGN_RX.match(feature) or is_cheat(node_of(feature)):
        raise ValueError("redo chỉ dùng cho claude-concept của một node có frame (node cheat do tool dựng lại, không vẽ)")
    data = parse_order(read_text(folder / ORDER_FILE))
    if item not in dict(data["items"]):
        raise ValueError(f"order không có item '{item}'. Có: {', '.join(i for i, _ in data['items'])}")
    text = str(text or "").replace("\r", "").strip()
    if not text or len(text) > REVIEW_TEXT_MAX:
        raise ValueError(f"cần lí do (cần sửa gì), tối đa {REVIEW_TEXT_MAX} kí tự")
    status = read_status(folder)
    if status["version"] < 1:
        raise ValueError(f"order {ref} chưa giao lần nào: @ClaudeImage còn lượt đầu, không cần redo")
    b = budget(folder, item)
    if b["left"] > 0:
        raise ValueError(f"frame '{item}' đang có một lượt làm lại chưa dùng")
    log(folder, "redo", item=item, round=b["round"] + 1, text=text, version=status["version"])
    save_status(folder, state="doing")
    print(f"Đã mở 1 lượt vẽ lại cho frame '{item}' của {ref}. Nhắn @ClaudeImage bằng SendMessage (to: \"ClaudeImage\"): "
          f"\"Order {ref}, frame {item}: được vẽ lại 1 lượt. Lí do: {text}\"")
    return 0


def write_check(work, item, ref, what, sizes):
    """The tool's own check of what `claude` made, written to work/<item>.check.md (deliver reads its first line):
    every PNG has the size asked for, and the file says which order, item and shapes it is, so nothing is taken for
    another order's. The content of the pictures is not looked at. sizes: [(name, path, (w, h))]. True when it passes."""
    bad, lines = [], []
    for name, path, want in sizes:
        try:
            m = measure_png(path)
            got = (m["width"], m["height"])
        except (ValueError, OSError):
            got = None
        ok = got == tuple(want)
        lines.append(f"- {name}: " + (f"{got[0]}x{got[1]}px" if got else "không đọc được PNG") + f", cần {want[0]}x{want[1]}px" + ("" if ok else ": LỆCH"))
        if not ok:
            bad.append(name)
    head = CHECK_PASS if not bad else f"chưa đạt: sai kích thước hoặc không đọc được: {', '.join(bad)}"
    (work / f"{item}.check.md").write_text("\n".join(
        [head, f"Tự kiểm bằng script (studio.py claude), không xem nội dung ảnh. Order {ref}, item {item}."] + list(what) + lines) + "\n",
        encoding="utf-8")
    print(f"    tự kiểm: {head} (work/{item}.check.md)")
    return not bad


def finish_frame(folder, work, item, fr, parts, attempt, stamp, event="run", ref=""):
    """Puts the frame together into work/out/<item>.png and prints what was made."""
    placed = {sid: p for p in parts for sid in p["shapes"]}
    svg, size = compose_frame(fr, placed)
    (work / "_log").mkdir(parents=True, exist_ok=True)
    (work / "out").mkdir(parents=True, exist_ok=True)
    whole = work / "_log" / f"{item}-{stamp}.compose.svg"
    whole.write_text(svg, encoding="utf-8")
    set_aside(list((work / "out").glob(f"{item}.*")), work, "_old")
    dest = work / "out" / f"{item}.png"
    used = svg_to_png(whole, dest, size)
    log(folder, event, tool=CLAUDE, item=item, mode=used or "none", exit=0 if used else 1, parts=[p["id"] for p in parts],
        result=f"đã có work/out/{dest.name}" if used else "không ghép được ảnh", files=[dest.name] if used else [])
    if not used:
        print(f"Không ghép được ảnh của frame '{item}'. Dừng và báo người.", file=sys.stderr)
        return 1
    if attempt:
        keep_attempt(work, item, attempt)
    print(f"ClaudeImage: đã ghép work/out/{dest.name} ({size[0]}x{size[1]}px, bằng {used}" + (f", lượt {attempt}" if attempt else "") + ")")
    for line in parts_summary(parts, fr):
        print(f"    {line}")
    kept = [r["id"] for r in fr["shapes"] if r["role"] != gd.ROLE_BACKDROP and r["id"] not in placed]
    if kept:
        print(f"    giữ hình cơ bản của frame: {', '.join(kept)}")
    write_check(work, item, ref, [f"Claude-concept của frame '{item}'" + (f" ({fr['title']})" if fr["title"] else "") + ".",
                                  "Hình đã thay bằng phần: " + (", ".join(sorted(placed)) or "không có") + ".",
                                  "Hình giữ như frame (hình cơ bản, không thuộc tiêu chí của order): " + (", ".join(kept) or "không có") + ".",
                                  "Chữ tool in (font Roboto): " + ("; ".join(frame_texts(fr)) or "không có") + "."],
                [(f"phần {p['id']}", p["png"], (p["w"], p["h"])) for p in parts] + [(dest.name, dest, size)])
    return 0


def cheat_owner(nodes, fr, r):
    """(node path, frame name, shape) of the shape of a node that a cheat shape stands for: among the nodes whose
    numbers it uses, a shape the node itself talks about with the same id, else of the same kind and size."""
    slot = gd.shape_slots(fr, r)[0]
    same_id, same_size = [], []
    for path in r["sources"]:
        for f in (nodes.get(path) or {}).get("frames", []):
            for s in f["shapes"]:
                if s["role"] != gd.ROLE_OWN or s["shape"] != r["shape"]:
                    continue
                hit = (path, Path(f["file"]).stem, s)
                o = gd.shape_slots(f, s)[0]
                if s["id"] == r["id"]:
                    same_id.append(hit)
                elif abs(o[2] - slot[2]) <= 0.51 and abs(o[3] - slot[3]) <= 0.51:
                    same_size.append(hit)
    pool = same_id or [h for h in same_size if h[2]["label"] == r["label"]] or same_size
    return pool[0] if pool else None


def cheat_plan(root, tree, fr):
    """(parts, missing, notes) to put a cheat frame together from the claude concepts of the nodes it borrows from.
    missing: [{shape, w, h, node, frame, of, why}]: a shape whose node has no good claude concept yet."""
    nodes = {n["path"]: n for n in gd.walk(tree)} if tree else {}
    _, pending = gd.read_art(root, tree)
    waiting = {o["node"]: o["ref"] for o in pending if o.get("tool") == CLAUDE}
    cache, parts, missing, notes = {}, {}, [], []
    for r in fr["shapes"]:
        if r["role"] == gd.ROLE_BACKDROP or r["shape"] not in gd.PART_SHAPES:
            continue
        if not r["sources"]:
            notes.append(f"{r['id']}: không `use` số của node nào nên không biết lấy phần ở đâu (giữ hình cơ bản; @GameDesigner sửa frame cheat)")
            continue
        owner = cheat_owner(nodes, fr, r)
        if owner is None:
            notes.append(f"{r['id']}: các node nguồn ({', '.join(r['sources'])}) không có hình nào cùng id hoặc cùng loại và cỡ "
                         "(giữ hình cơ bản; @GameDesigner xem lại frame)")
            continue
        path, fname, shape = owner
        if path not in cache:
            cache[path] = delivered_parts(root, tree, path)
        have = cache[path].get(fname)
        q = next((q for q in have["parts"] if shape["id"] in q.get("shapes", [])), None) if have else None
        if q is None:
            slot = gd.shape_slots(fr, r)[0]
            why = (f"order {waiting[path]} chưa giao: làm và giao order đó trước" if path in waiting else
                   "node chưa có claude-concept còn dùng được: nhắn @GameDesigner đặt order claude-concept cho node này")
            missing.append({"shape": r["id"], "w": slot[2], "h": slot[3], "node": path, "frame": fname, "of": shape["id"], "why": why})
            continue
        key = (path, fname, q["id"])
        if key not in parts:
            parts[key] = {"id": f"{len(parts) + 1}-{q['id']}", "w": q["w"], "h": q["h"], "slice": q.get("slice"), "shapes": [],
                          "svg": have["dir"] / f"{q['id']}.svg", "png": have["dir"] / f"{q['id']}.png", "text": q.get("text", True),
                          "from": {"node": path, "frame": fname, "part": q["id"], "ref": have["ref"], "version": have["version"]}}
        why = next((x for x in (fits(parts[key], s) for s in gd.shape_slots(fr, r)) if x), "")
        if why:
            notes.append(f"{r['id']}: phần {path}#{q['id']} không vừa ({why}); giữ hình cơ bản")
        else:
            parts[key]["shapes"].append(r["id"])
    return [p for p in parts.values() if p["shapes"]], missing, notes


def needs_file(folder, item):
    return folder / "work" / f"{item}.needs.json"


def claude_cheat(root, ref, item, folder, feature, oid):
    """A state of a cheat node: nothing is drawn. The frame is put together from the parts of the nodes it borrows
    its numbers from; when one of them has no claude concept yet, nothing is made and the command says which."""
    data, problems = check_order(root, CLAUDE, feature, oid, folder)
    if problems:
        raise ValueError("order chưa hợp lệ: " + "; ".join(problems[:4]))
    if item not in dict(data["items"]):
        raise ValueError(f"order không có item '{item}'. Có: {', '.join(i for i, _ in data['items'])}")
    if not svg_converters():
        raise ValueError("máy này không đổi được SVG sang PNG: cần rsvg-convert, inkscape, magick, Chrome hoặc Edge "
                         "(hoặc đặt STUDIO_SVG2PNG). Dừng và báo người")
    tree, _ = gd.read_tree(root, "live")
    fr = frame_named(next((n for n in gd.walk(tree) if n["path"] == node_of(feature)), None), item)
    parts, missing, notes = cheat_plan(root, tree, fr)
    work = folder / "work"
    work.mkdir(parents=True, exist_ok=True)
    if missing:
        write_json(needs_file(folder, item), missing)
        log(folder, "wait", item=item, needs=[f"{m['node']}:{m['frame']}:{m['of']}" for m in missing])
        print(f"Chưa dựng được state '{item}': còn {len(missing)} hình thiếu phần. Cheat không tự vẽ; các frame sau phải có "
              "claude-concept trước:", file=sys.stderr)
        for m in missing:
            print(f"- hình {m['shape']} ({m['w']:g}x{m['h']:g}px) cần phần của hình '{m['of']}' ở {m['node']} (frame {m['frame']}): {m['why']}",
                  file=sys.stderr)
        return 1
    if needs_file(folder, item).exists():
        needs_file(folder, item).unlink()
    save_status(folder, state="doing")
    write_parts(work, item, parts, fr)
    code = finish_frame(folder, work, item, fr, parts, 0, time.strftime("%Y%m%d-%H%M%S"), event="build", ref=ref)
    for n in notes:
        print(f"    lưu ý: {n}")
    if code == 0:
        print("Tool tự dựng, không tính lượt. Đủ mọi state của order thì deliver.")
    return code


def order_size(data):
    """(width, height) in px written in the order's format line, or None."""
    m = re.search(r"(\d+)\s*x\s*(\d+)", data["fields"].get("format", ""))
    return (int(m.group(1)), int(m.group(2))) if m and 0 < int(m.group(1)) <= 8192 and 0 < int(m.group(2)) <= 8192 else None


def cmd_claude(root, ref, item):
    """@ClaudeImage: makes work/out/<item>.png from the SVG the chat wrote in work/<item>.svg. One run of the item.
    Nothing is counted when the SVG is refused or the machine cannot draw it."""
    _, owner, oid, folder = find_order(root, ref, CLAUDE)
    if not ID_RX.match(item):
        raise ValueError(f"item '{item}' phải là chữ thường, số, gạch nối")
    with ClaudeLock(root, folder, item):
        if DESIGN_RX.match(owner):      # a claude concept: the parts of a frame, or a cheat state put together
            if is_cheat(node_of(owner)):
                return claude_cheat(root, ref, item, folder, owner, oid)
            return claude_frame(root, ref, item, folder, owner)
        src = folder / "work" / f"{item}.svg"
        size = order_size(parse_order(read_text(folder / ORDER_FILE)))
        if not size:
            raise ValueError("order không ghi kích thước trong format (ví dụ 'png 256x256, nền trong suốt'): bên đặt order phải ghi")
        if not src.is_file():
            raise ValueError(f"chưa có work/{item}.svg: viết hình SVG của item này trước (Write), rồi chạy lại")
        problems = check_svg(read_text(src))
        if problems:
            raise ValueError(f"work/{item}.svg chưa dùng được (không tính lượt): " + "; ".join(problems))
        if not svg_converters():
            raise ValueError("máy này không đổi được SVG sang PNG: cần rsvg-convert, inkscape, magick, Chrome hoặc Edge "
                             "(hoặc đặt STUDIO_SVG2PNG). Dừng và báo người")
        feature, oid, folder, data, text, work, refs = prepare(root, CLAUDE, ref, item, ("image",))
        attempt = start_attempt(folder, item)
        stamp = time.strftime("%Y%m%d-%H%M%S")
        shutil.copy2(src, work / "_log" / f"{item}-{attempt}-{stamp}.svg")      # what this run drew
        set_aside(list((work / "out").glob(f"{item}.*")), work, "_old")      # the previous draft
        dest = work / "out" / f"{item}.png"
        used = svg_to_png(with_font(src, work / "_log" / f"{item}-{attempt}-{stamp}.font.svg"), dest, size)
        log(folder, "run", tool=CLAUDE, item=item, mode=used or "none", exit=0 if used else 1,
            result=f"đã có work/out/{dest.name}" if used else "không đổi được SVG sang PNG", files=[dest.name] if used else [])
        if not used:
            print(f"Không đổi được work/{item}.svg sang PNG (lượt {attempt} đã tính). Dừng và báo người.", file=sys.stderr)
            return 1
        keep_attempt(work, item, attempt)
        print(f"ClaudeImage (SVG → PNG bằng {used}, lượt {attempt}): đã có work/out/{dest.name}")
        try:
            for line in measure_lines(dest, data["fields"].get("format", "")):
                print(f"    {line}")
        except ValueError as ex:
            print(f"    không đo được: {ex}")
        write_check(work, item, f"{feature}/{oid}", [f"Order của feature {feature}: một ảnh cho item '{item}'."], [(dest.name, dest, size)])
        return 0


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
    part_dirs = {}
    if meta and tool == CLAUDE:      # a claude concept hands over its parts too; a cheat state must have been put together
        waiting = [i for i in ids if needs_file(folder, i).exists()]
        part_dirs = {i: folder / "work" / PARTS_DIR / i for i in ids}
        lacking = [i for i in ids if not (part_dirs[i] / PARTS_FILE).is_file()]
        if waiting or lacking:
            raise ValueError("chưa giao được: " + "; ".join(
                ([f"state còn thiếu phần từ node khác: {', '.join(waiting)}"] if waiting else [])
                + ([f"chưa có bảng phần (chạy `studio.py claude {feature}/{oid} <item>` trước): {', '.join(lacking)}"] if lacking else [])))
        tables = {i: gd._json(part_dirs[i] / PARTS_FILE) or {} for i in ids}
        meta["sources"] = sorted({(p["from"]["ref"], p["from"]["version"]) for t in tables.values() for p in t.get("parts", []) if p.get("from")})
    dest.mkdir(parents=True)
    for i in ids:
        shutil.copy2(by_item[i][0], dest / by_item[i][0].name)
    for i, src in part_dirs.items():
        shutil.copytree(src, dest / PARTS_DIR / i)
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
    remade = {e.get("item") for e in log_[last + 1:] if e.get("event") in ("attempt", "pick", "build")}
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
    if no_judging(tool, feature):
        raise ValueError("claude-concept không chấm: muốn vẽ lại một frame thì Leader chạy `studio.py redo <ref> <frame> \"<lí do>\"`")
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


# ---------------------------------------------------------------- measuring a result
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
PNG_KINDS = {0: ("xám", 1), 2: ("RGB", 3), 3: ("bảng màu", 1), 4: ("xám + alpha", 2), 6: ("RGBA", 4)}


def png_rows(raw, stride, height, step):
    """The scanlines of a PNG that is not interlaced, with their filters undone."""
    if len(raw) < (stride + 1) * height:
        raise ValueError("file PNG thiếu dữ liệu điểm ảnh")
    prev, pos = bytearray(stride), 0
    for _ in range(height):
        kind, row = raw[pos], bytearray(raw[pos + 1:pos + 1 + stride])
        pos += 1 + stride
        if kind == 1:
            for i in range(step, stride):
                row[i] = (row[i] + row[i - step]) & 255
        elif kind == 2:
            for i in range(stride):
                row[i] = (row[i] + prev[i]) & 255
        elif kind == 3:
            for i in range(stride):
                row[i] = (row[i] + (((row[i - step] if i >= step else 0) + prev[i]) >> 1)) & 255
        elif kind == 4:
            for i in range(stride):
                a, b, c = (row[i - step], prev[i], prev[i - step]) if i >= step else (0, prev[i], 0)
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - c - c)
                row[i] = (row[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        elif kind:
            raise ValueError("file PNG hỏng (bộ lọc dòng lạ)")
        yield row
        prev = row


def measure_png(path):
    """Size and transparency of a PNG, read with the standard library: {width, height, kind, depth, alpha}.
    alpha is None when the file has no alpha channel, a text when it cannot be measured, else {clear: share of the
    pixels with alpha 0, corners: the four corner pixels have alpha 0, box: (left, top, right, bottom) of the
    pixels with alpha above 0, or None when there are none}. Raises ValueError for a file that is no PNG."""
    import zlib
    blob = Path(path).read_bytes()
    if blob[:8] != PNG_SIGNATURE:
        raise ValueError("không phải file PNG (chỉ đo được PNG)")
    pos, head, data, palette_alpha = 8, None, [], False
    while pos + 8 <= len(blob):
        size, name = int.from_bytes(blob[pos:pos + 4], "big"), blob[pos + 4:pos + 8]
        body = blob[pos + 8:pos + 8 + size]
        pos += 12 + size
        if name == b"IHDR" and len(body) >= 13:
            head = (int.from_bytes(body[0:4], "big"), int.from_bytes(body[4:8], "big"), body[8], body[9], body[12])
        elif name == b"IDAT":
            data.append(body)
        elif name == b"tRNS":
            palette_alpha = True
        elif name == b"IEND":
            break
    if not head or head[3] not in PNG_KINDS or not head[0] or not head[1]:
        raise ValueError("file PNG hỏng (thiếu phần đầu)")
    width, height, depth, colour, interlace = head
    out = {"width": width, "height": height, "kind": PNG_KINDS[colour][0], "depth": depth, "alpha": None}
    if colour not in (4, 6):
        out["alpha"] = "có màu trong suốt khai theo bảng màu, không đo từng điểm" if palette_alpha else None
        return out
    if interlace or depth not in (8, 16):
        out["alpha"] = "có kênh alpha nhưng file lưu kiểu interlace, không đo từng điểm"
        return out
    try:
        raw = zlib.decompress(b"".join(data))
    except zlib.error:
        raise ValueError("file PNG hỏng (không giải nén được)")
    size = depth // 8
    step = PNG_KINDS[colour][1] * size
    clear, box, corners = 0, None, []
    for y, row in enumerate(png_rows(raw, width * step, height, step)):
        alpha = bytes(row[step - size::step])
        if size == 2:      # 16 bit: zero only when both bytes are
            alpha = bytes(a | b for a, b in zip(alpha, row[step - 1::step]))
        clear += alpha.count(0)
        if y in (0, height - 1):
            corners += [alpha[0], alpha[-1]]
        if any(alpha):
            left, right = width - len(alpha.lstrip(b"\0")), len(alpha.rstrip(b"\0")) - 1
            box = (left, y, right, y) if box is None else (min(box[0], left), box[1], max(box[2], right), y)
    out["alpha"] = {"clear": clear / (width * height), "corners": not any(corners), "box": box}
    return out


def measure_lines(path, wanted=""):
    """What `measure` prints about one result; wanted is the order's format line."""
    size = f"{max(1, round(path.stat().st_size / 1024))} KB"
    if path.suffix.lower() != ".png":
        return [f"file {path.suffix.lower()}, {size}: chỉ đo được PNG"]
    m = measure_png(path)
    w, h, alpha = m["width"], m["height"], m["alpha"]
    lines = [f"{w}x{h} px, PNG {m['kind']} {m['depth']} bit, {size}"]
    clear_corners = isinstance(alpha, dict) and alpha["corners"]
    if alpha is None:
        lines.append("nền: không có kênh alpha, nền KHÔNG trong suốt")
    elif isinstance(alpha, str):
        lines.append(f"nền: {alpha}")
    else:
        lines.append(f"nền: {alpha['clear'] * 100:.0f}% điểm trong suốt hoàn toàn; 4 góc "
                     + ("trong suốt" if alpha["corners"] else "KHÔNG trong suốt"))
        if alpha["box"]:
            l, t, r, b = alpha["box"]
            bw, bh = r - l + 1, b - t + 1
            dx, dy = (l + r + 1) / 2 - w / 2, (t + b + 1) / 2 - h / 2
            lines.append(f"hình (điểm có alpha trên 0): {bw}x{bh} px = {bw * 100 / w:.0f}% x {bh * 100 / h:.0f}% khung; "
                         f"tâm lệch ngang {dx:+.0f} px, dọc {dy:+.0f} px; cách mép trái {l}, trên {t}, phải {w - 1 - r}, dưới {h - 1 - b} px")
        else:
            lines.append("hình: không có điểm nào hiện (ảnh trong suốt toàn bộ)")
    verdict = []
    want = re.search(r"(\d+)\s*x\s*(\d+)", wanted or "")
    if want:
        ok = (w, h) == (int(want.group(1)), int(want.group(2)))
        verdict.append(f"kích thước {'khớp' if ok else 'LỆCH'} ({want.group(1)}x{want.group(2)})")
    if "trong suốt" in (wanted or "").lower():
        verdict.append("nền trong suốt: " + ("4 góc trong suốt" if clear_corners else "KHÔNG ĐẠT hoặc không đo được"))
    if verdict:
        lines.append(f"so với format của order: {'; '.join(verdict)}")
    return lines


def cmd_measure(root, ref, item=""):
    """Prints size and transparency of an order's results: those waiting in work/out, else the last delivery.
    It only reads; what it prints is for the tool chat's own check and for the one who judges."""
    tool, feature, oid, folder = find_order(root, ref)
    data = parse_order(read_text(folder / ORDER_FILE))
    items = [i for i, _ in data["items"]]
    if item and item not in items:
        raise ValueError(f"order không có item '{item}'. Có: {', '.join(items)}")
    version = read_status(folder)["version"]
    wanted = data["fields"].get("format", "")
    print(f"{tool} {feature}/{oid} · format của order: {wanted or '(không ghi)'}")
    for i in ([item] if item else items):
        hits = sorted(p for p in (folder / "work" / "out").glob(f"{i}.*") if p.is_file())
        where = "work/out"
        if not hits and version:
            delivered = item_file(version_dir(root, tool, feature, oid, version), i)
            hits, where = ([delivered] if delivered else []), f"bản giao v{version}"
        if not hits:
            print(f"- {i}: chưa có kết quả")
            continue
        for p in hits:
            try:
                lines = measure_lines(p, wanted)
            except ValueError as ex:
                lines = [f"không đo được: {ex}"]
            print(f"- {p.name} ({where}): {lines[0]}")
            for line in lines[1:]:
                print(f"    {line}")
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
        if delivered and not r["text"] and b["left"] > 0:      # a Leader opened the round (redo): its reason is the feedback
            said = [e.get("text", "") for e in events(folder) if e.get("event") == "redo" and e.get("item") == i]
            r = dict(r, text=said[-1] if said else "")
        rows.append({"id": i, "text": text, "level": r["level"] if delivered else None, "feedback": r["text"] if delivered else "",
                     "used": b["used"], "allowed": b["allowed"], "left": b["left"], "round": b["round"], "why": b["why"],
                     "redo": delivered and b["left"] > 0})
    return rows


def summary(root, tool=None):
    """[(ref, tool, state, version, count, title, problems, items)] of the orders; state as view_state gives it,
    items as item_rows gives them."""
    out = []
    live = live_tree(root)      # once for all orders
    for t, feature, oid, folder in all_orders(root):
        if tool and t != tool:
            continue
        data, problems = check_order(root, t, feature, oid, folder, live)
        status = read_status(folder)
        state = view_state(status, [i for i, _ in data["items"]], read_reviews(folder), no_judging(t, feature))
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
    """One line for @GameDesigner's prompt: where its art orders stand (art concepts of @Codex, claude concepts of
    @ClaudeImage), and what it should tell the tool chat or the human."""
    try:
        rows = [r for r in summary(root) if r[1] in gd.ART_TOOLS and DESIGN_RX.match(r[0].rsplit("/", 1)[0])]
    except OSError:
        return ""
    parts = []
    for ref, tool, state, version, _, _, _, items in rows:
        chat = "@" + TOOL_CHATS[tool]
        try:
            folder = find_order(root, ref, tool)[3]
            needs = sorted({m["node"] for i in items for m in (gd._json(needs_file(folder, i["id"])) or []) if isinstance(m, dict)})
        except (ValueError, OSError, KeyError):
            needs = []
        if needs:
            parts.append(f"{ref}: cheat đang chờ claude-concept của: {', '.join(needs)} (đặt order cho các node đó)")
            continue
        redo = [i["id"] for i in items if i["redo"]]
        mute = [i["id"] for i in items if version and i["level"] == REDO and not i["feedback"]]
        stuck = [i["id"] for i in items if version and state == "redo" and i["level"] == REDO and i["feedback"] and not i["redo"]]
        if state in ("new", "doing") and not version:
            parts.append(f"{ref}: chưa giao (đã nhắn {chat} chưa?)")
        elif state == "doing":
            parts.append(f"{ref}: {chat} đang làm lại")
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
    live = live_tree(root)      # once for all orders: reading it again for each art order took seconds
    for tool, feature, oid, folder in all_orders(root):
        try:
            data, problems = check_order(root, tool, feature, oid, folder, live)
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
        state = view_state(status, [i["id"] for i in items], read_reviews(folder), no_judging(tool, feature))
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
                line += (" · đã giao, không chấm" if no_judging(tool, ref.rsplit("/", 1)[0]) else f" · {level_name(i['level'])}") + (
                    f" · phản hồi: {i['feedback']}" if i["feedback"] else "")
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
        if len(args) == 4 and args[0] == "redo":
            return cmd_redo(root, args[1], args[2], args[3])
        if args[:1] == ["measure"] and len(args) in (2, 3):
            return cmd_measure(root, args[1], args[2] if len(args) == 3 else "")
        if len(args) == 3 and args[0] in ("codex", "consult", "blender", "claude"):
            return {"codex": cmd_codex, "consult": cmd_consult, "blender": cmd_blender, "claude": cmd_claude}[args[0]](root, args[1], args[2])
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
