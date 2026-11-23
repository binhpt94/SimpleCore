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
    python Management/tools/studio.py approve <_design/...>/<id>      the human: let an art order run (spends Codex)
    python Management/tools/studio.py accept <_design/...>/<id>       the human: the art delivered is what the node shows
    python Management/tools/studio.py anchor <_design/...>/<id> <item> the human: an accepted art guides its style (toggle)
    python Management/tools/studio.py pick <F>/<id> <item> <n>         the tool chat: take run n of an item as its result
    python Management/tools/studio.py debt close <n-k>                 the human: accept an art debt as it is

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
Runs are counted (log.jsonl, event attempt): an item has RUNS_FIRST runs, one more (RUNS_REDO) after each time the order
is sent back, and the order is sent back at most MAX_REDOS times; the next reject closes it ("closed") with art
debts. Every run's result is kept in work/attempts/<item>-<n>.<ext>; `pick` puts one back into work/out. The tool
chat writes work/<item>.check.md (first line "đạt" or "chưa đạt: <why>"); deliver marks the other items "chưa
chuẩn" in quality.json and opens an art debt for each (Management/studio/debt.json), which a later delivery that
passes the same owner and item closes, or the human. A feature may take what is "chưa chuẩn": take writes QUALITY.md.
Exactly one result per item: a run that makes more takes none of them. A rejected order is run again only after
lessons.md names its feedback file. Standard library only; Windows and Linux.

Art concepts (kind: art, @Codex): the owner is a design node, `_design/<node path with '.' for '/'>`, and
@GameDesigner writes the order. One item per frame of the node in live; style: names styles of the root's
.art-style/. Nothing runs until the human approves the order (approval.json, written by the page through
setup_server.py, or by `approve`); the approval holds the order's text and the node's live hash, so changing either
asks again; each round of an art order (the first, and each after a reject) needs it. All art runs at most
ART_DAILY_MAX times a day.
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
APPROVAL_FILE = "approval.json"
RUNS_FIRST = 2                   # runs of an item before the first delivery
RUNS_REDO = 1                    # more runs after each time the order is sent back
MAX_REDOS = 2                    # times an order may be sent back and made again; the next reject closes it
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
        good = {e["frame"]: e["ref"] for e in art.get(path, []) if e["label"] in ("chờ nhận", "đang dùng")}
        mine = f"{feature}/{oid}"
        for item, _ in data["items"]:
            if good.get(item) and good[item] != mine:
                problems.append(f"frame '{item}' đã có ảnh còn dùng được ({good[item]}); chỉ xin lại khi ảnh đó cũ hoặc bị trả")
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


def budget(folder, item):
    """{used, allowed, left, round} of one item: RUNS_FIRST, plus RUNS_REDO for every time the order was sent back."""
    log_ = events(folder)
    rounds = sum(1 for e in log_ if e.get("event") == "reject")
    used = sum(1 for e in log_ if e.get("event") == "attempt" and e.get("item") == item)
    allowed = RUNS_FIRST + RUNS_REDO * min(rounds, MAX_REDOS)
    return {"used": used, "allowed": allowed, "left": max(0, allowed - used), "round": rounds}


def start_attempt(folder, item, **extra):
    """Logs one run before the tool starts; returns its number for the item (1, 2, ...)."""
    n = budget(folder, item)["used"] + 1
    log(folder, "attempt", item=item, n=n, **extra)
    return n


def keep_attempt(work, item, n):
    """Copies the result now in work/out into work/attempts/<item>-<n>.<ext>."""
    for p in (work / "out").glob(f"{item}.*"):
        dest = work / "attempts" / f"{item}-{n}{p.suffix}"
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, dest)


def runs_since_approval(folder, item):
    """The art runs of `item` logged after the order's last approval (the log keeps their order)."""
    events = []
    for line in read_text(folder / LOG_FILE).split("\n") if (folder / LOG_FILE).is_file() else []:
        try:
            e = json.loads(line)
        except ValueError:
            continue
        if isinstance(e, dict):
            events.append(e)
    last = max([i for i, e in enumerate(events) if e.get("event") == "approve"] or [-1])
    return [e for e in events[last + 1:] if e.get("event") == "attempt" and e.get("item") == item]


def art_gate(root, feature, oid, folder, item):
    """Raises ValueError unless the human's approval lets this item run once more now."""
    approval = None
    try:
        approval = json.loads(read_text(folder / APPROVAL_FILE))
    except (OSError, ValueError):
        pass
    ref = f"{feature}/{oid}"
    if not isinstance(approval, dict):
        raise ValueError(f"order {ref} chưa được người duyệt gen: chờ người bấm Duyệt gen ở tab Game design")
    node, _ = live_node(root, node_of(feature))
    if approval.get("order") != order_hash(folder):
        raise ValueError("order.md đã đổi sau khi người duyệt: chờ người duyệt lại")
    if not node or approval.get("node") != node["dataHash"]:
        raise ValueError("node trong live đã đổi sau khi người duyệt: chờ người duyệt lại")
    if item not in (approval.get("items") or []):
        raise ValueError(f"item '{item}' không nằm trong phần người đã duyệt")
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
    if status["state"] in ("taken", "accepted", "closed"):
        raise ValueError("order đã được nhận hoặc đã đóng; việc mới thì đặt order mới")
    missing = unlearnt(root, tool, feature, oid, folder)
    if missing:
        raise ValueError(f"order đã bị trả: ghi bài học vào Management/studio/{tool}/{LESSONS} trước, mỗi mục ghi "
                         f"đường dẫn phản hồi: " + ", ".join(feedback_key(tool, feature, oid, f) for f in missing))
    b = budget(folder, item)
    if b["left"] <= 0:
        raise ValueError(f"item '{item}' hết lượt ({b['used']}/{b['allowed']}, vòng {b['round']}): không chạy thêm. Chọn bản "
                         f"gần order nhất bằng `studio.py pick {feature}/{oid} {item} <n>` (các bản ở work/attempts/), ghi "
                         f"work/{item}.check.md ('chưa đạt: <lý do>') rồi deliver; item sẽ mang nhãn chưa chuẩn kèm phiếu nợ")
    if DESIGN_RX.match(feature):
        art_gate(root, feature, oid, folder, item)
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
    debts = read_debts(root)
    rel_dest = dest.relative_to(Path(root)).as_posix()
    opened, closed = [], []
    for i, q in quality.items():
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
        print(f"Đã giao bản v{version}: {rel} ({len(ids)} ảnh). Ảnh hiện ở tab Game design (node {meta['node']}) với nhãn "
              f"'chờ nhận'; người nhận hoặc trả ở đó. Nhắn @GameDesigner bằng SendMessage rằng order {feature}/{oid} đã giao.")
        return 0
    below = "; ".join(f"{i}: {q['note']}" for i, q in quality.items() if not q["ok"])
    below_text = f" Chưa chuẩn (hết lượt, có phiếu nợ): {below}." if below else ""
    print(f"Đã giao bản v{version}: {rel} ({len(ids)} file).\nNhắn phiên {feature} bằng SendMessage: "
          f"\"Order {feature}/{oid} đã giao bản v{version} ở {rel}. Nhận: python Management/tools/studio.py take "
          f"{feature}/{oid}; chưa đạt thì viết feedback-<n>.md trong thư mục order rồi chạy reject." + below_text + "\"")
    return 0


def cmd_take(root, ref):
    tool, feature, oid, folder = find_order(root, ref)
    data = parse_order(read_text(folder / ORDER_FILE))
    status = read_status(folder)
    if status["version"] < 1 or status["state"] == "rejected":
        raise ValueError(f"order {ref} chưa có bản giao nào đang chờ nhận (trạng thái: {status['state']})")
    if data["fields"].get("kind") == "art":
        raise ValueError("ảnh art của node thiết kế không chép vào Assets: người nhận trên tab Game design (studio.py accept)")
    if data["fields"].get("kind") == "consult":
        raise ValueError(f"order tham vấn không chép vào Assets: đọc câu trả lời ở {RESOURCES}/{feature}/{tool}/{oid}/v{status['version']}/")
    src = Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{status['version']}"
    dest = (Path(root) / "UnityProject" / "Assets" / "Features" / Path(*feature.split("/")) / "FeatureAssets"
            / data["fields"]["deliver"] / "Generated" / oid)
    dest.mkdir(parents=True, exist_ok=True)
    names = {p.name for p in src.iterdir() if p.is_file()}
    for p in dest.iterdir():      # what an earlier version had and this one does not (the .meta goes with it)
        base = p.name[:-5] if p.name.endswith(".meta") else p.name
        if p.is_file() and base not in names and base != "QUALITY.md":
            p.unlink()
    quality = gd._json(src / QUALITY_FILE) or {}
    below = {i: q for i, q in quality.items() if isinstance(q, dict) and not q.get("ok")}
    names.discard(QUALITY_FILE)
    for name in sorted(names):
        shutil.copy2(src / name, dest / name)      # same name: Unity keeps the .meta and its GUID
    note = dest / "QUALITY.md"
    if below:
        ids = {d["item"]: d["id"] for d in read_debts(root) if d["state"] == "open" and d["ref"] == f"{feature}/{oid}"}
        note.write_text(f"# Hàng chưa chuẩn của order {feature}/{oid} v{status['version']}\n\n" + "".join(
            f"- {i}: {q.get('note')} (phiếu nợ {ids.get(i, '?')})\n" for i, q in below.items()), encoding="utf-8")
        print(f"Lưu ý: {len(below)} item chưa chuẩn ({', '.join(below)}), xem {note.relative_to(Path(root)).as_posix()}; "
              f"phiếu nợ ở tab Studio.")
    elif note.is_file():
        note.unlink()
    save_status(folder, state="taken")
    log(folder, "take", version=status["version"], to=dest.relative_to(Path(root)).as_posix())
    print(f"Đã chép bản v{status['version']} vào {dest.relative_to(Path(root)).as_posix()}; refresh Unity để có .meta.")
    return 0


def cmd_reject(root, ref):
    tool, feature, oid, folder = find_order(root, ref)
    status = read_status(folder)
    if status["state"] not in ("delivered", "taken", "accepted"):
        raise ValueError(f"order {ref} chưa giao nên chưa trả được (trạng thái: {status['state']})")
    fresh = sorted((p.name for p in folder.iterdir() if FEEDBACK_RX.match(p.name) and p.name not in status["feedback"]),
                   key=lambda n: int(FEEDBACK_RX.match(n).group(1)))
    if not fresh:
        raise ValueError("viết feedback-<n>.md mới trong thư mục order trước: sai ở đâu, item nào, cần thế nào")
    rejects = sum(1 for e in events(folder) if e.get("event") == "reject")
    if rejects >= MAX_REDOS:      # sent back once more than allowed: close it, owe what it should have been
        data = parse_order(read_text(folder / ORDER_FILE))
        vdir = Path(root) / RESOURCES / Path(*feature.split("/")) / tool / oid / f"v{status['version']}"
        reason = " ".join(read_text(folder / fresh[-1]).split())[:300]
        debts = read_debts(root)
        opened = []
        for i, _ in data["items"]:
            hit = next((p for p in vdir.iterdir() if p.stem == i), None) if vdir.is_dir() else None
            for d in debts:
                if d["state"] == "open" and d["ref"] == f"{feature}/{oid}" and d["item"] == i:
                    d.update(state="closed", closedBy="thay bằng phiếu trả quá số lần", closedAt=now())
            opened.append(open_debt(debts, tool, f"{feature}/{oid}", feature, i, status["version"],
                                    hit.relative_to(Path(root)).as_posix() if hit else "", reason, "trả quá số lần")["id"])
        write_debts(root, debts)
        save_status(folder, state="closed", feedback=status["feedback"] + fresh)
        log(folder, "close", version=status["version"], feedback=fresh, debts=opened)
        if (folder / APPROVAL_FILE).is_file():
            os.replace(folder / APPROVAL_FILE, folder / f"approval-v{status['version']}.json")
        print(f"Order {ref} đã bị trả {rejects + 1} lần (tối đa {MAX_REDOS} lần làm lại): đóng, không chạy thêm. Mở phiếu nợ "
              f"{', '.join(opened)} (tab Studio). Vẫn ghi bài học từ {', '.join(fresh)}; cần thì đặt order mới.")
        return 0
    save_status(folder, state="rejected", feedback=status["feedback"] + fresh)
    log(folder, "reject", version=status["version"], feedback=fresh)
    if (folder / APPROVAL_FILE).is_file():      # the next run needs the human again
        os.replace(folder / APPROVAL_FILE, folder / f"approval-v{status['version']}.json")
    if DESIGN_RX.match(feature):      # a picture sent back is no anchor any more
        path = studio_dir(root, "codex") / gd.ANCHORS_FILE
        anchors = [x for x in (gd._json(path) or []) if isinstance(x, dict)]
        kept = [x for x in anchors if not (x.get("ref") == ref.strip("/") and x.get("version") == status["version"])]
        if len(kept) != len(anchors):
            write_json(path, kept)
    print(f"Đã trả order {ref} ({', '.join(fresh)}). Nhắn @{tool.capitalize()} bằng SendMessage để làm lại.")
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


def cmd_approve(root, ref):
    """The human lets an art order run: every item once (twice with a retry), for this text of the order and this
    hash of the node in live."""
    feature, oid, folder = design_order(root, ref)
    data, problems = check_order(root, "codex", feature, oid, folder)
    if problems:
        raise ValueError("order chưa hợp lệ, chưa duyệt được: " + "; ".join(problems[:4]))
    status = read_status(folder)
    if status["state"] in ("delivered", "accepted"):
        raise ValueError(f"order {ref} đã giao (trạng thái {status['state']}): nhận hoặc trả trước")
    missing = unlearnt(root, "codex", feature, oid, folder)
    if missing:
        raise ValueError("@Codex chưa ghi bài học cho phản hồi: " + ", ".join(missing))
    node, _ = live_node(root, node_of(feature))
    approval = {"order": order_hash(folder), "node": node["dataHash"], "items": [i for i, _ in data["items"]],
                "runs": {i: budget(folder, i)["left"] for i, _ in data["items"]}, "at": now(), "by": "người"}
    write_json(folder / APPROVAL_FILE, approval)
    save_status(folder, state="approved")
    log(folder, "approve", items=approval["items"], node=approval["node"])
    print(f"Đã duyệt gen {ref}: {len(approval['items'])} ảnh, mỗi ảnh còn {max(approval['runs'].values() or [0])} lượt. "
          f"Nhắn @Codex bằng SendMessage để làm.")
    return 0


def cmd_accept(root, ref):
    feature, oid, folder = design_order(root, ref)
    status = read_status(folder)
    if status["state"] != "delivered":
        raise ValueError(f"order {ref} không có bản giao nào chờ nhận (trạng thái: {status['state']})")
    save_status(folder, state="accepted")
    log(folder, "accept", version=status["version"])
    print(f"Đã nhận bản v{status['version']} của {ref}.")
    return 0


def cmd_anchor(root, ref, item):
    """Marks (or unmarks) one accepted picture as an anchor of its styles: later art runs of those styles see it."""
    feature, oid, folder = design_order(root, ref)
    status = read_status(folder)
    if status["state"] != "accepted":
        raise ValueError("chỉ ảnh đã nhận mới làm mốc được")
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
        rows = [r for r in summary(root, tool) if r[2] in ("new", "doing", "rejected", "approved")
                and not (DESIGN_RX.match(r[0].rsplit("/", 1)[0]) and r[2] in ("new", "rejected"))]      # art waits for the human
    except OSError:
        return ""
    if not rows:
        return " Không có order nào đang chờ."
    return f" {len(rows)} order đang chờ: " + "; ".join(f"{r[0]} ({r[2]}, {r[4]} item)" for r in rows[:8])


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
        for i, text in data["items"]:
            b = budget(folder, i)
            q = quality.get(i) if isinstance(quality.get(i), dict) else None
            items.append({"id": i, "text": text, "used": b["used"], "allowed": b["allowed"], "left": b["left"],
                          "ok": q.get("ok") if q else None, "note": q.get("note", "") if q else "",
                          "file": f"{vdir.relative_to(Path(root)).as_posix()}/{q['file']}" if q and q.get("file") else ""})
        design = bool(DESIGN_RX.match(feature))
        orders.append({"ref": f"{feature}/{oid}", "tool": tool, "kind": data["fields"].get("kind", ""), "title": data["title"],
                       "owner": feature, "design": design, "node": node_of(feature) if design else "",
                       "state": status["state"], "version": status["version"], "round": budget(folder, "")["round"],
                       "maxRedos": MAX_REDOS, "items": items, "problems": problems,
                       "waitsHuman": design and status["state"] in ("new", "rejected") and not (folder / APPROVAL_FILE).is_file(),
                       "unlearnt": unlearnt(root, tool, feature, oid, folder), "at": status["at"]})
    return {"orders": orders, "debts": read_debts(root), "limits": {"first": RUNS_FIRST, "redo": RUNS_REDO,
            "maxRedos": MAX_REDOS, "artDaily": ART_DAILY_MAX}, "generatedAt": now()}


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
    for ref, tool, state, version, count, title, problems, missing in rows:
        print(f"- {tool} {ref}: {state}" + (f", đã giao v{version}" if version else "") + f", {count} item · {title}"
              + (" · CHỜ NGƯỜI DUYỆT GEN" if DESIGN_RX.match(ref.rsplit("/", 1)[0]) and state in ("new", "rejected") else ""))
        try:
            _, f_, o_, folder_ = find_order(root, ref, tool)
            for item_id, _ in parse_order(read_text(folder_ / ORDER_FILE))["items"]:
                b = budget(folder_, item_id)
                print(f"    {item_id}: {b['used']}/{b['allowed']} lượt (còn {b['left']}), vòng {b['round']}/{MAX_REDOS}")
        except (ValueError, OSError):
            pass
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
        if len(args) == 2 and args[0] in ("take", "reject", "approve", "accept"):
            return {"take": cmd_take, "reject": cmd_reject, "approve": cmd_approve, "accept": cmd_accept}[args[0]](root, args[1])
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
