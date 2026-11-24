#!/usr/bin/env python3
"""The game-design tree: the one place that knows its layout and its rules. Used by sync_page.py, setup_server.py,
gd_delete.py, gd_status.py and (for the write gate) .claude/hooks/guard.py.

    Management/game-design/
      mode.json   {"mode": "auto" | "user"}; written only by the page (setup_server.py, with the PIN)
      ai/         where @GameDesigner works (it reads and writes here)
        idea/     the table it proposes
      live/       the fixed table; changes only through promote() (the Chốt button) and sync_live() (Sync)

Both tables have the same shape. A folder is a node; its depth is its round (ROUNDS). One node:
    data.md                    the core data: title, short summary, and the core ideas, each with the AI's score
    CLAUDE.md                  the detailed explanation; points to data.md; changing it never forces a redo
    .images/                   pictures plus index.md (`- <file>: <what it is for>`)
    priority.json              how important the node is, 0-10, scored by the AI on leaf nodes (see PHASES)
    code.json                  round-2 nodes only: {"unity": ["Wallet"]}, the feature folders that implement it
    .progress/progress.json    leaf nodes only: todo | doing | done for each phase (by @CodeLead or the page)
    .ai-need-user-choose/      questions.json (by @GameDesigner), answers.json (by the page)
    .user-review/review.json   the human's score and feedback per idea (by the page)
    .history/                  log.jsonl and a copy of every version of data.md (by this code only)
    .frame-concept/<name>.frame round-4 nodes: the layout as text (see parse_frame), in the data hash
Folders starting with "." are not nodes. live holds only data.md, CLAUDE.md, code.json, .images and .frame-concept.
The root's data.md also says `screen: landscape | portrait` (the screen frames are drawn on); a round-4 node with no
layout says `frame: none`. Neither is in the data hash.

data.md:
    ---
    based-on: <data hash of the parent it was written against>      (not on the root)
    refs: <node path>@<data hash>, glossary#<term id>@<idea hash>    (other branches it builds on; optional)
    ---
    # <title>

    <summary, 1-3 sentences>

    ## Ý cốt lõi

    ### <idea-id> | AI <0-10> | <the idea in one short line: what the human reads and scores>
    <detail, optional: shown when the human opens the idea>
The data hash covers title, summary, every idea's id, title and detail, and the frames; not the scores, not based-on. A child whose
based-on differs from its parent's data hash is stale: it must be redone against the new data.
refs: the nodes of OTHER branches a node builds on, each with the data hash it was checked against (a glossary term
with the hash of its idea). Not in the data hash. A node must redo when a ref's hash differs from the node's
current one, or when it mentions in backticks a round-3/4 node path (or `glossary#<id>`) outside its own lineage
without declaring it (ref_issues). Its own ancestors and descendants are never refs (based-on covers them).
The glossary: the node `glossary` beside the features (round 2) is the one place terms are defined, one idea per
term titled "<term>: <meaning>". It is not a feature: no code.json, no children, no importance, no progress.

Scores: 8-10 good, 5-7 acceptable, 2-4 bad (redo), 0-1 off (avoid). A score passes when it is PASS_MIN or more.
The human's score is absolute; the AI's counts only where the human has not scored. A human score belongs to the
exact text it was given to: when the idea is rewritten it becomes "previous" and the idea is unscored again.
A node PASSES when it has ideas, is not stale, and
    mode auto: every idea's effective score passes;
    mode user: the human scored every idea, every score passes, and no question is unanswered.
A node may be written only when every node above it is approved: it passes and, in mode user, is the same in live.
Nothing reaches live by itself: the human presses Chốt on a node, or Sync for every passing node (sync_live).
The human may score an idea in either table; the score belongs to the idea's text, so one low score on a
fixed idea makes the proposed node fail wherever the text is still the same.
Progress: a leaf is done when the cell of the phase its own score means is "done". A done leaf keeps its score
but no longer lifts its parents: a parent's importance is the highest among what is NOT done below it, and a
parent whose leaves are all done is done.
Code gate: a feature folder may be coded only when a node of the LIVE table names it in code.json and that node's
branch has reached round 4 (code_allowed, asked by guard.py for Features and Services sessions).
To add a round: add its name to ROUNDS. Standard library only.
"""
import hashlib
import json
import os
import re
import shutil
import time
from pathlib import Path

ROUNDS = ["Ý tưởng sơ khai", "Feature (tóm tắt)", "Quan hệ và định nghĩa", "Quyết định chi tiết"]
BASE = ("Management", "game-design")
WORK_DIR = "ai"
TABLES = {"idea": (WORK_DIR, "idea"), "live": ("live",)}
DATA_FILE = "data.md"
DOC_FILE = "CLAUDE.md"
NODE_FILES = (DATA_FILE, DOC_FILE)
IMAGES_DIR = ".images"
IMAGES_INDEX = "index.md"
IMAGE_EXT = (".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg")
QUESTIONS_DIR = ".ai-need-user-choose"
QUESTIONS_FILE = "questions.json"
ANSWERS_FILE = "answers.json"
REVIEW_DIR = ".user-review"
REVIEW_FILE = "review.json"
HISTORY_DIR = ".history"
LOG_FILE = "log.jsonl"
STATE_FILE = "state.json"
PROGRESS_DIR = ".progress"
PROGRESS_FILE = "progress.json"
WORK_STATES = ("todo", "doing", "done")
CODE_FILE = "code.json"      # on a round-2 node: the Unity feature folders that implement it
CODE_ROUND = 2
CODE_NAME_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")      # the same shape guard.py takes as a FEATURE
# Round 2 always has these infrastructure features: key of the node -> the code folder its code.json names.
INFRA_FEATURES = {"feature-scene-flow": "SceneFlow", "feature-session-scene": "_Services/SessionScene", "feature-popup": "_Services/Popup"}
NOT_COPIED = (QUESTIONS_DIR, REVIEW_DIR, HISTORY_DIR, PROGRESS_DIR, "priority.json")      # stay in the idea table
MODE_FILE = "mode.json"
MODES = ("auto", "user")
DEFAULT_MODE = "auto"
MAX_TEXT = 2000       # characters of a free-text answer or feedback
PASS_MIN = 5
BANDS = [
    {"key": "good", "min": 8, "max": 10, "label": "Ý tưởng tốt, gần như chốt"},
    {"key": "fine", "min": 5, "max": 7, "label": "Chấp nhận ở hiện tại, sẽ cập nhật nếu có yêu cầu"},
    {"key": "bad", "min": 2, "max": 4, "label": "Ý tưởng xấu, cần làm lại"},
    {"key": "off", "min": 0, "max": 1, "label": "Ý tưởng lạc, nên tránh"},
]
# How important a node is, 0-10, and the development phase that needs it. Kept beside the node, outside every
# hash: changing it never forces a redo, never makes a node differ from live, never locks anything.
#   <node>/priority.json               {"ai": 0-10, "why": "..."}   by @GameDesigner, on leaf nodes only
#   <node>/.user-review/priority.json  {"score": 0-10, "at": ...}   by the page (the human)
PRIORITY_FILE = "priority.json"
PHASES = [
    {"key": "idea", "name": "IDEA", "min": 8, "max": 10, "label": "Chạy đủ core game, art siêu cơ bản"},
    {"key": "prototype", "name": "PROTOTYPE", "min": 5, "max": 7, "label": "Đủ tính năng quan trọng, art đủ cho phần core game"},
    {"key": "full", "name": "FULL", "min": 2, "max": 4, "label": "Đủ tính năng, đủ art; giai đoạn điều chỉnh"},
    {"key": "optional", "name": "TUỲ CHỌN", "min": 0, "max": 1, "label": "Có cũng được, không có cũng được"},
]
ITEMS_TITLE = "Ý cốt lõi"
KEY_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
ITEM_RX = re.compile(r"^###\s+(\S+)\s*\|\s*AI\s+(\d+)\s*(?:\|\s*(.*\S))?\s*$")
FIRST_SENTENCE_RX = re.compile(r"^(.*?[.!?])(\s|$)", re.S)
IMAGE_LINE_RX = re.compile(r"^\s*[-*]\s*`?([^\s`:]+)`?\s*:\s*(.*)$")
HASH_LEN = 12
GLOSSARY_KEY = "glossary"     # the round-2 node holding the shared terms (not a feature)
_SEG = r"[a-z0-9]+(?:-[a-z0-9]+)*"
REF_RX = re.compile(rf"^({_SEG}(?:/{_SEG})*)(?:#({_SEG}))?@([0-9a-f]{{{HASH_LEN}}})$")
MENTION_RX = re.compile(r"`([^`\n]+)`")
MENTION_PATH_RX = re.compile(rf"^{_SEG}(?:/{_SEG})+$")
MENTION_TERM_RX = re.compile(rf"^{GLOSSARY_KEY}#({_SEG})$")
DEFINITIONS_TITLE = "Định nghĩa"
DEF_LINE_RX = re.compile(r"^\s*[-*]\s*\*\*(.+?)\*\*\s*:\s*(.*\S)?\s*$")
# Frames: the layout of a round-4 node as text (.frame-concept/<name>.frame), drawn by the page, checked here.
FRAME_DIR = ".frame-concept"
ART_DIR = ".art-concept"          # generated art concepts (the next step; nothing reads it yet)
FRAME_EXT = ".frame"
FRAME_NONE = "none"               # `frame: none` in data.md: a round-4 node with no layout
FRAME_ROUND = 4
# The cheat node: <feature>/cheat, one per feature, at the round-3 place with no children. Each idea is one state
# to build at once in a dev build (the values to force in its detail), with exactly one frame named after the idea;
# the only node art may be ordered for, one picture per state. Required by a feature with a `kind: ui` frame.
CHEAT_KEY = "cheat"
CHEAT_MAX = 3
CHEAT_ROUND = 3
SCREENS = {"landscape": (1920, 1080), "portrait": (1080, 1920)}      # `screen:` of the root data.md, in px
TAP_MIN = 96                      # px: the shortest side of a button (dự kiến)
FRAME_KINDS = ("ui", "field")     # ui: px on the screen, y down · field: the battlefield in u, y up
BOX_SHAPES = ("rect", "round-rect", "button", "label", "icon", "bar")
ROUND_SHAPES = ("circle", "ring")
LINE_SHAPES = ("line", "arrow")
FRAME_SHAPES = BOX_SHAPES + ROUND_SHAPES + LINE_SHAPES + ("grid",)
FRAME_NUM_RX = re.compile(r"^(-?\d+(?:\.\d+)?)(u?)$")
FRAME_NAME_RX = re.compile(r"^[a-z][a-z0-9-]*$")
FRAME_TOKEN_RX = re.compile(r'(\w[\w-]*)="([^"]*)"|(\w[\w-]*)=(\S+)|(\S+)')
# Styles: the art direction of the game, decided in round 1 (.art-style/ of the root, in its data hash).
STYLE_DIR = ".art-style"
STYLE_IMAGE_EXT = (".png", ".jpg", ".jpeg", ".webp")
# Art concepts: images @Codex makes from a round-4 node's frames (orders of the owner _design, see studio.py).
DESIGN_OWNER = "_design"
ART_TOOL = "codex"
# The human's judgement of one delivered picture (index = level; the same list as studio.py REVIEW_LEVELS, which
# cannot be imported here: studio imports this module).
ART_LEVELS = ("Làm lại ngay", "Lỗi nhưng chưa cần làm ngay", "Đúng phần lớn chi tiết", "Đạt")
ART_LEVEL_NONE = "Chưa duyệt"
ART_META = "meta.json"
ANCHORS_FILE = "anchors.json"
HISTORY_SHOWN = 30
STATES = ("new", "changed", "same")      # an idea node against live


def table_dir(root, table):
    if table not in TABLES:
        raise ValueError(f"unknown table '{table}'")
    return Path(root).joinpath(*BASE, *TABLES[table])


def read_text(path):
    return path.read_text(encoding="utf-8-sig", errors="replace").replace("\r", "")


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def read_json(path):
    """The JSON value of a file; None when it is missing; raises ValueError when it is not JSON."""
    if not path.is_file():
        return None
    return json.loads(read_text(path))


def read_dict(path, key):
    """The dict stored under `key` in a JSON file; {} when the file is missing, broken or shaped differently."""
    try:
        data = read_json(path)
    except ValueError:
        return {}
    value = data.get(key) if isinstance(data, dict) else None
    return value if isinstance(value, dict) else {}


def now():
    return time.strftime("%Y-%m-%d %H:%M:%S")


def short_hash(*parts):
    h = hashlib.sha1()
    for p in parts:
        h.update(p.encode("utf-8") + b"\0")
    return h.hexdigest()[:HASH_LEN]


def tidy(text):
    """Text as it is hashed: no trailing spaces, no blank lines at the ends."""
    return "\n".join(line.rstrip() for line in text.strip().split("\n"))


# ---------------------------------------------------------------- mode
def read_mode(root):
    try:
        data = read_json(Path(root).joinpath(*BASE, MODE_FILE))
    except ValueError:
        data = None
    mode = data.get("mode") if isinstance(data, dict) else None
    return mode if mode in MODES else DEFAULT_MODE


def write_mode(root, mode):
    if mode not in MODES:
        raise ValueError(f"mode phải là một trong {MODES}")
    write_json(Path(root).joinpath(*BASE, MODE_FILE), {"mode": mode, "at": now()})


# ---------------------------------------------------------------- data.md
def parse_data(text):
    """{based_on, refs, title, summary, items: [{id, text, ai, hash}], hash, errors} of a data.md."""
    errors, based_on, refs, screen, frame_mode = [], "", [], "", ""
    text = text.strip("\n")
    if text.startswith("---\n"):
        head, sep, rest = text[4:].partition("\n---")
        if sep:
            text = rest.lstrip("-").lstrip("\n")
            for line in head.split("\n"):
                key, _, value = line.partition(":")
                if key.strip() == "based-on":
                    based_on = (value.split() or [""])[0]
                elif key.strip() == "screen":
                    screen = value.strip()
                elif key.strip() == "frame":
                    frame_mode = value.strip()
                elif key.strip() == "refs":
                    for entry in re.split(r"[,\s]+", value.strip().strip("[]")):
                        m = REF_RX.match(entry.strip("\"'`")) if entry else None
                        if m:
                            refs.append({"path": m.group(1), "term": m.group(2) or "", "hash": m.group(3)})
                        elif entry:
                            errors.append(f"refs: '{entry}' phải có dạng <đường dẫn node>@<hash> hoặc "
                                          f"{GLOSSARY_KEY}#<id>@<hash>")
        else:
            errors.append("phần đầu '---' chưa đóng")
    lines = text.strip("\n").split("\n")
    title = ""
    if lines and lines[0].startswith("# "):
        title, lines = lines[0][2:].strip(), lines[1:]
    else:
        errors.append(f"{DATA_FILE} phải có dòng tiêu đề '# <tên>' ngay sau phần đầu")

    intro, items, section, current = [], [], None, None      # section: None (intro) | "items" | "other"
    for line in lines:
        if line.startswith("## "):
            name = line[3:].strip()
            section, current = ("items" if name.lower() == ITEMS_TITLE.lower() else "other"), None
            if section == "other":
                errors.append(f"mục '## {name}' không thuộc {DATA_FILE}: chỉ gồm tóm tắt và '## {ITEMS_TITLE}'; "
                              f"phần khác đưa sang {DOC_FILE}")
            continue
        if section is None:
            intro.append(line)
        elif section == "items":
            if line.startswith("### "):
                m = ITEM_RX.match(line)
                current = {"id": m.group(1) if m else "", "ai": int(m.group(2)) if m else None,
                           "title": (m.group(3) or "").strip() if m else "", "lines": [], "head": line}
                items.append(current)
            elif current is not None:
                current["lines"].append(line)
            elif line.strip():
                errors.append(f"chữ nằm ngoài một ý trong '## {ITEMS_TITLE}': {line.strip()[:40]}")
    summary = " ".join(s.strip() for s in re.split(r"\n\s*\n", "\n".join(intro).strip(), maxsplit=1)[0].split("\n")).strip()

    out, seen = [], set()
    for it in items:
        body = tidy("\n".join(it["lines"]))
        if not it["id"] or it["ai"] is None:
            errors.append(f"dòng ý phải có dạng '### <id> | AI <0-10>': {it['head'][:50]}")
            continue
        if not KEY_RX.match(it["id"]) or it["id"] in seen:
            errors.append(f"id ý '{it['id']}' phải là chữ thường, số, gạch nối và không trùng")
            continue
        if not 0 <= it["ai"] <= 10:
            errors.append(f"ý '{it['id']}': điểm AI phải từ 0 tới 10")
            continue
        if not body and not it["title"]:
            errors.append(f"ý '{it['id']}' chưa có tiêu đề hay nội dung")
            continue
        seen.add(it["id"])
        # title: what the human reads and scores; detail: shown when the idea is opened. An idea written without a
        # title (the first layout) is titled by its first sentence, and hashed as before so its scores still apply.
        titled = bool(it["title"])
        if titled:
            head, detail, parts = it["title"], body, (it["id"], it["title"], body)
        else:
            line = body.split("\n")[0]
            first = FIRST_SENTENCE_RX.match(line)
            head = (first.group(1) if first else line).strip()
            detail, parts = ("" if head == body else body), (it["id"], body)
        out.append({"id": it["id"], "title": head, "detail": detail, "titled": titled,
                    "text": head + ("\n" + body if body else "") if titled else body,
                    "ai": it["ai"], "hash": short_hash(*parts), "parts": parts})
    data_hash = short_hash(title, summary, *[p for it in out for p in it.pop("parts")])
    if screen and screen not in SCREENS:
        errors.append(f"screen: '{screen}' phải là " + " hoặc ".join(SCREENS))
    if frame_mode and frame_mode != FRAME_NONE:
        errors.append(f"frame: chỉ nhận '{FRAME_NONE}' (node không có bố cục)")
    return {"based_on": based_on, "refs": refs, "screen": screen, "frameMode": frame_mode, "title": title,
            "summary": summary, "items": out, "hash": data_hash, "errors": errors}


def frame_files(folder):
    """[(name, text)] of the node's .frame-concept/*.frame, by name; text as it is hashed."""
    d = Path(folder) / FRAME_DIR
    if not d.is_dir():
        return []
    return [(p.name, tidy(read_text(p).replace("\r", ""))) for p in sorted(d.iterdir())
            if p.is_file() and p.suffix == FRAME_EXT]


def read_data(folder):
    """parse_data of the node's data.md; the data hash also covers its frames (a node without frames keeps the
    hash of its data.md alone)."""
    path = folder / DATA_FILE
    if not path.is_file():
        return None
    data = parse_data(read_text(path))
    frames = frame_files(folder)
    if frames:
        data["hash"] = short_hash(data["hash"], *[part for name, text in frames for part in (name, text)])
    styles = style_parts(folder)
    if styles:
        data["hash"] = short_hash(data["hash"], STYLE_DIR, *styles)
    return data


def style_parts(folder):
    """What of .art-style/ goes into the data hash: every file's name and content (images by their sha1)."""
    d = Path(folder) / STYLE_DIR
    if not d.is_dir():
        return []
    parts = []
    for p in sorted(x for x in d.iterdir() if x.is_file()):
        if p.suffix == ".md":
            parts += [p.name, tidy(read_text(p).replace("\r", ""))]
        elif p.suffix.lower() in STYLE_IMAGE_EXT:
            parts += [p.name, hashlib.sha1(p.read_bytes()).hexdigest()]
    return parts


def read_styles(folder):
    """([{id, title, text, images, hash}], errors) of the root's .art-style/: one <id>.md per style (first line
    '# <name>', then what it is for and how it looks), reference pictures named <id>-<anything>.<png|jpg|webp>."""
    d = Path(folder) / STYLE_DIR
    if not d.is_dir():
        return [], []
    files = sorted(x for x in d.iterdir() if x.is_file())
    out, errors = [], []
    for p in files:
        if p.suffix != ".md":
            continue
        sid = p.stem
        text = tidy(read_text(p).replace("\r", ""))
        lines = text.split("\n")
        if not KEY_RX.match(sid):
            errors.append(f"{STYLE_DIR}/{p.name}: id style phải là chữ thường, số, gạch nối")
            continue
        if not lines[0].startswith("# ") or not "\n".join(lines[1:]).strip():
            errors.append(f"{STYLE_DIR}/{p.name}: dòng đầu '# <tên style>', dưới đó mô tả style (dùng cho gì, góc nhìn, màu, nét)")
            continue
        images = [x.name for x in files if x.suffix.lower() in STYLE_IMAGE_EXT and x.name.startswith(sid + "-")]
        h = short_hash(text, *[hashlib.sha1((d / n).read_bytes()).hexdigest() for n in images])
        out.append({"id": sid, "title": lines[0][2:].strip(), "text": "\n".join(lines[1:]).strip(), "images": images, "hash": h})
    ids = {s["id"] for s in out}
    for p in files:
        if p.suffix.lower() in STYLE_IMAGE_EXT and not any(p.name.startswith(i + "-") for i in ids):
            errors.append(f"{STYLE_DIR}/{p.name}: ảnh phải đặt tên <id style>-<tên>, theo một style có file .md")
        elif p.suffix != ".md" and p.suffix.lower() not in STYLE_IMAGE_EXT:
            errors.append(f"{STYLE_DIR}/{p.name}: chỉ chứa <id>.md và ảnh tham chiếu")
    return out, errors


def score_of(value):
    """value when it is a whole score 0-10, otherwise None."""
    return value if isinstance(value, int) and not isinstance(value, bool) and 0 <= value <= 10 else None


def phase_of(score):
    return next((p["key"] for p in PHASES if score is not None and p["min"] <= score <= p["max"]), "")


def read_priority(folder):
    """What is stored about a node's importance: the AI's score and reason, the human's score."""
    try:
        ai = read_json(folder / PRIORITY_FILE)
    except ValueError:
        ai = None
    try:
        user = read_json(folder / REVIEW_DIR / PRIORITY_FILE)
    except ValueError:
        user = None
    ai = ai if isinstance(ai, dict) else {}
    user = user if isinstance(user, dict) else {}
    return {"ai": score_of(ai.get("ai")), "why": ai.get("why") if isinstance(ai.get("why"), str) else "",
            "user": score_of(user.get("score"))}


def mark_priority(tree):
    """Fills every node's `priority` with what follows from the stored scores:
    leaf, ignoredAi (an AI score on a node that has children counts for nothing), base, effective, offset, from, phase.
    Up from the leaves: a leaf is worth the human's score, else the AI's; a parent's base is the highest worth below.
    Down from the root: a node the human scored has exactly that score, and the difference from its base is the
    offset its unscored descendants get on top of their own base (the nearest scored ancestor counts), within 0-10."""
    def ranked(node):      # the glossary is not a piece of the game to build: it has no importance
        return [c for c in node["children"] if not (c["round"] == CODE_ROUND and c["key"] == GLOSSARY_KEY)]

    def unranked(node):
        node["priority"].update({"leaf": False, "ignoredAi": False, "base": None, "done": False, "effective": None,
                                 "offset": 0, "from": "", "phase": "", "glossary": True})

    def up(node):
        """(what the node is worth to its parent, or None when it is done or unranked; whether it is done)."""
        p = node["priority"]
        for c in node["children"]:
            if c not in ranked(node):
                unranked(c)
        below = [up(c) for c in ranked(node)]
        p["leaf"] = not node["children"]
        p["ignoredAi"] = not p["leaf"] and p["ai"] is not None
        if p["leaf"]:
            own = p["user"] if p["user"] is not None else p["ai"]
            # Done is judged on the leaf's own score (the human's, else the AI's), not on what a scored parent
            # shifts it to: otherwise done-ness and the parents' ranking would each depend on the other.
            cell = node.get("progress", {}).get(phase_of(own))
            p["base"], p["done"] = p["ai"], bool(cell) and cell["state"] == "done"
            return (None if p["done"] else own), p["done"]
        worth = [w for w, _ in below if w is not None]
        p["base"] = max(worth) if worth else None
        p["done"] = all(done for _, done in below)
        if p["done"]:
            return None, True
        return (p["user"] if p["user"] is not None else p["base"]), False

    def down(node, offset, source):
        p = node["priority"]
        if p["user"] is not None:
            p["effective"], p["offset"], p["from"] = p["user"], 0, ""
            offset, source = (p["user"] - p["base"] if p["base"] is not None else 0), node["title"] or node["path"] or "(gốc)"
        elif p["base"] is None:
            p["effective"], p["offset"], p["from"] = None, 0, ""
        else:
            p["effective"] = max(0, min(10, p["base"] + offset))
            p["offset"], p["from"] = offset, source if offset else ""
        p["phase"] = phase_of(p["effective"])
        for child in ranked(node):
            down(child, offset, source)

    if tree:
        up(tree)
        down(tree, 0, "")


# ---------------------------------------------------------------- progress (leaf nodes)
def progress_phases():
    """The phases a leaf can be worked on for: every phase but the optional one."""
    return [ph["key"] for ph in PHASES if ph["min"] > 1]


def read_progress(folder):
    """{phase: {state, by, at, note}} of a node; "todo" for whatever is not stored."""
    saved = read_dict(folder / PROGRESS_DIR / PROGRESS_FILE, "phases")
    out = {}
    for key in progress_phases():
        cell = saved.get(key) if isinstance(saved.get(key), dict) else {}
        out[key] = {"state": cell.get("state") if cell.get("state") in WORK_STATES else "todo",
                    "by": str(cell.get("by") or ""), "at": str(cell.get("at") or ""),
                    "note": cell.get("note") if isinstance(cell.get("note"), str) else ""}
    return out


def save_progress(root, path, phase, state, by, note=None):
    """Marks one phase of the idea leaf at `path` as todo, doing or done. Only leaves have progress.
    note: None keeps the note already there. Not in any hash, not in the history."""
    parts = check_path(path)
    folder = table_dir(root, "idea").joinpath(*parts)
    if not (folder / DATA_FILE).is_file():
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    if any(c.is_dir() and not c.name.startswith(".") for c in folder.iterdir()):
        raise ValueError(f"node '{path or '(gốc)'}' có node con: tiến độ chỉ ghi ở node lá")
    if parts == [GLOSSARY_KEY]:
        raise ValueError(f"node {GLOSSARY_KEY} là bảng thuật ngữ, không có tiến độ")
    if phase not in progress_phases():
        raise ValueError(f"giai đoạn phải là một trong {progress_phases()}")
    if state not in WORK_STATES:
        raise ValueError(f"trạng thái phải là một trong {WORK_STATES}")
    if note is not None and (not isinstance(note, str) or len(note) > MAX_TEXT):
        raise ValueError(f"ghi chú phải là chữ, tối đa {MAX_TEXT} kí tự")
    target = folder / PROGRESS_DIR / PROGRESS_FILE
    saved = read_dict(target, "phases")
    old = saved.get(phase) if isinstance(saved.get(phase), dict) else {}
    saved[phase] = {"state": state, "by": str(by), "at": now(),
                    "note": note.strip() if note is not None else (old.get("note") if isinstance(old.get("note"), str) else "")}
    write_json(target, {"phases": saved})


# ---------------------------------------------------------------- code.json and the code gate
def read_code(folder, where, depth, problems):
    """The Unity feature folders a node says it is implemented by (code.json); only round-2 nodes may say so."""
    path = folder / CODE_FILE
    if not path.is_file():
        return []
    try:
        data = read_json(path)
    except ValueError:
        problems.append({"path": where, "text": f"{CODE_FILE} không phải JSON hợp lệ"})
        return []
    names = data.get("unity") if isinstance(data, dict) else None
    if not isinstance(names, list) or any(not isinstance(n, str) or not CODE_NAME_RX.match(n) for n in names):
        problems.append({"path": where, "text": f'{CODE_FILE} phải có dạng {{"unity": ["Wallet", "_Services/Popup"]}}'})
        return []
    if depth != CODE_ROUND:
        problems.append({"path": where, "text": f"{CODE_FILE} chỉ đặt ở node feature (vòng {CODE_ROUND})"})
        return []
    if folder.name == GLOSSARY_KEY:
        problems.append({"path": where, "text": f"node {GLOSSARY_KEY} không phải feature: bỏ {CODE_FILE}"})
        return []
    return names


def code_allowed(root, feature):
    """(ok, reason): may the Unity feature folder `feature` be coded? Only when a round-2 node of the LIVE table
    names it in code.json and that node's branch has a round-4 node. Read straight from the files."""
    live = table_dir(root, "live")
    owners = []
    for node in sorted(c for c in live.iterdir() if c.is_dir() and not c.name.startswith(".")) if live.is_dir() else []:
        if feature.lower() in (n.lower() for n in read_code(node, node.name, CODE_ROUND, [])):
            owners.append(node)
    if not owners:
        return False, (f"chưa có node thiết kế nào trong live khai thư mục code '{feature}' "
                       f"(file {CODE_FILE} ở node feature, do @GameDesigner viết và người chốt)")
    deepest = CODE_ROUND
    for node in owners:
        for data in node.rglob(DATA_FILE):
            below = data.parent.relative_to(node).parts
            if not any(part.startswith(".") for part in below):
                deepest = max(deepest, CODE_ROUND + len(below))
    if deepest < len(ROUNDS):
        return False, (f"thiết kế của '{feature}' ({', '.join(n.name for n in owners)}) mới được chốt tới vòng {deepest}; "
                       f"phải có quyết định chi tiết (vòng {len(ROUNDS)}) trong live mới được code")
    return True, ""


def save_priority(root, path, score):
    """Stores the human's importance score (0-10) for the idea node at `path`; None removes it. Not in the history."""
    parts = check_path(path)
    folder = table_dir(root, "idea").joinpath(*parts)
    if not (folder / DATA_FILE).is_file():
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    target = folder / REVIEW_DIR / PRIORITY_FILE
    if score is None:
        if target.is_file():
            target.unlink()
        return
    if score_of(score) is None:
        raise ValueError("độ quan trọng phải là số nguyên từ 0 tới 10")
    write_json(target, {"score": score, "at": now()})


def band_of(score):
    return next((b["key"] for b in BANDS if b["min"] <= score <= b["max"]), "")


# ---------------------------------------------------------------- the human's review
def review_versions(saved, item_id):
    """{text hash: {score, feedback, at}} the human gave to one idea, one entry per text it has had.
    Also reads the first layout of review.json, which kept a single {score, feedback, hash, at} per idea."""
    r = saved.get(item_id)
    if not isinstance(r, dict):
        return {}
    if isinstance(r.get("hash"), str):
        return {r["hash"]: r}
    return {h: e for h, e in r.items() if isinstance(e, dict)}


def merge_review(folder, items):
    """Adds to every idea the human's side, from the review kept in `folder` (always an idea node, also when the
    ideas are those of the live node: a score belongs to a text, not to a table): user (score for this exact
    text, or None), feedback, at, and previous ({score, feedback, at} given to another text of the same idea,
    the latest one, or None)."""
    saved = read_dict(folder / REVIEW_DIR / REVIEW_FILE, "items")

    def clean(e):
        score = e.get("score") if isinstance(e.get("score"), int) and not isinstance(e.get("score"), bool) else None
        return {"score": score, "feedback": e.get("feedback") if isinstance(e.get("feedback"), str) else "", "at": str(e.get("at") or "")}

    for it in items:
        versions = review_versions(saved, it["id"])
        current = clean(versions[it["hash"]]) if it["hash"] in versions else None
        others = sorted((clean(e) for h, e in versions.items() if h != it["hash"]), key=lambda e: e["at"])
        it["user"] = current["score"] if current else None
        it["feedback"] = current["feedback"] if current else ""
        it["at"] = current["at"] if current else ""
        it["previous"] = others[-1] if others and not current else None
        it["effective"] = it["user"] if it["user"] is not None else it["ai"]


def verdict(items, pending, stale, mode, refs=None):
    """(passed, reasons) of a node: see the module docstring. refs: what ref_issues found, or None."""
    reasons = []
    if stale:
        reasons.append("data của node cha đã đổi: rà lại node theo data mới rồi ghi lại based-on")
    for r in (refs or {}).get("refs", []):
        if r["ok"] is False:
            reasons.append(f"ref '{r['label']}' đã đổi (hash {r['hash']} -> {r['current']}): đọc lại node đó, "
                           f"rà các ý dùng nó rồi ghi hash mới vào refs")
    for r in (refs or {}).get("missing", []):
        reasons.append(f"nhắc '{r['label']}' mà chưa khai trong refs: đọc node đó rồi khai '{r['label']}@{r['current']}'")
    if not items:
        reasons.append(f"chưa có ý nào trong '## {ITEMS_TITLE}'")
    for it in items:
        if mode == "user" and it["user"] is None:
            reasons.append(f"ý '{it['id']}': người chưa chấm")
        elif it["effective"] < PASS_MIN:
            who = "người" if it["user"] is not None else "AI"
            reasons.append(f"ý '{it['id']}': {who} chấm {it['effective']}, cần từ {PASS_MIN} trở lên")
    if mode == "user" and pending:
        reasons.append(f"còn {pending} câu hỏi người chưa trả lời")
    return not reasons, reasons


# ---------------------------------------------------------------- images and questions
def read_images(folder, where, problems):
    """[{file, note}] of a node's .images folder; pictures and index.md must name each other."""
    images = folder / IMAGES_DIR
    if not images.is_dir():
        return [], ""
    files = sorted(p.name for p in images.iterdir() if p.is_file() and p.suffix.lower() in IMAGE_EXT)
    index = images / IMAGES_INDEX
    text = read_text(index) if index.is_file() else ""
    notes = {}
    for line in text.split("\n"):
        m = IMAGE_LINE_RX.match(line)
        if m:
            notes[m.group(1)] = m.group(2).strip()
    if files and not index.is_file():
        problems.append({"path": where, "text": f"{IMAGES_DIR} có ảnh nhưng thiếu {IMAGES_INDEX}"})
    for name in files:
        if index.is_file() and not notes.get(name):
            problems.append({"path": where, "text": f"ảnh {name} chưa được ghi trong {IMAGES_DIR}/{IMAGES_INDEX}"})
    for name in notes:
        if name not in files:
            problems.append({"path": where, "text": f"{IMAGES_INDEX} nhắc tới ảnh {name} nhưng không có file"})
    return [{"file": name, "note": notes.get(name, "")} for name in files], text


# ---------------------------------------------------------------- frames (.frame-concept/<name>.frame)
# kind: ui | field                     ui: px on the screen of the root's `screen:` · field: the battlefield in u
# title: <text>
# view: x=-20u..20u y=-2u..8u          field only: the part of the battlefield drawn
# let <name> = <number>[u]             a constant of this node, usable here and by other nodes (`use`)
# use <name> = <node path>#<let>       a constant of another node's frames; that node goes into refs
# - <id> <shape> key=value ... [overlap]
#   box shapes (rect round-rect button label icon bar): x y w h        grid: x y cols rows cell|cw ch [gap]
#   circle ring: cx cy r                line arrow: from=a,b to=c,d    any: label="..." parent=<id>; bar: fill=0..1
# Positions are the lowest corner (ui: top-left, y down; field: bottom-left, y up), relative to `parent`'s.
def frame_comment(line):
    """The line without its '# comment' (a '#' after a space, outside quotes)."""
    quoted = False
    for i, ch in enumerate(line):
        if ch == '"':
            quoted = not quoted
        elif ch == "#" and not quoted and (i == 0 or line[i - 1].isspace()):
            return line[:i].rstrip()
    return line.rstrip()


def parse_frame(text):
    """{kind, title, view, lets, uses, shapes, errors} of one .frame file, values still as written."""
    f = {"kind": "", "title": "", "view": "", "lets": {}, "uses": {}, "shapes": [], "errors": []}
    err = f["errors"].append
    ids = set()
    for n, raw in enumerate((text or "").split("\n"), 1):
        line = frame_comment(raw).strip()
        if not line:
            continue
        at = f"dòng {n}"
        if line.startswith("- "):
            params, bare = {}, []
            for m in FRAME_TOKEN_RX.finditer(line[2:]):
                if m.group(1):
                    params[m.group(1)] = ("text", m.group(2))
                elif m.group(3):
                    params[m.group(3)] = ("raw", m.group(4))
                else:
                    bare.append(m.group(5))
            if len(bare) < 2:
                err(f"{at}: hình phải có dạng '- <id> <hình> key=value ...'")
                continue
            sid, shape, flags = bare[0], bare[1], bare[2:]
            if not KEY_RX.match(sid) or sid in ids:
                err(f"{at}: id '{sid}' phải là chữ thường, số, gạch nối và không trùng")
                continue
            if shape not in FRAME_SHAPES:
                err(f"{at}: hình '{shape}' không có trong thư viện ({', '.join(FRAME_SHAPES)})")
                continue
            bad = [x for x in flags if x != "overlap"]
            if bad:
                err(f"{at}: không hiểu '{bad[0]}' (chỉ có cờ 'overlap')")
            ids.add(sid)
            f["shapes"].append({"id": sid, "shape": shape, "params": params, "overlap": "overlap" in flags,
                                "line": n, "src": line})
            continue
        key, sep, value = line.partition(":")
        word = line.split(None, 1)[0]
        if word in ("let", "use"):
            name, eq, val = line[len(word):].partition("=")
            name, val = name.strip(), val.strip()
            if not eq or not FRAME_NAME_RX.match(name) or not val:
                err(f"{at}: phải có dạng '{word} <tên> = <giá trị>'")
            elif name in f["lets"] or name in f["uses"]:
                err(f"{at}: tên '{name}' đã khai")
            elif word == "let":
                if not FRAME_NUM_RX.match(val):
                    err(f"{at}: let '{name}' phải là một số (thêm u với chiến trường)")
                else:
                    f["lets"][name] = val
            else:
                path, hash_, ref = val.partition("#")
                if not hash_ or not MENTION_PATH_RX.match(path) or not FRAME_NAME_RX.match(ref):
                    err(f"{at}: use '{name}' phải có dạng <đường dẫn node>#<tên let>")
                else:
                    f["uses"][name] = (path, ref)
        elif sep and key.strip() in ("kind", "title", "view"):
            f[key.strip()] = value.strip()
        else:
            err(f"{at}: không hiểu dòng '{line[:40]}'")
    if f["kind"] not in FRAME_KINDS:
        err(f"kind: phải là {' hoặc '.join(FRAME_KINDS)}")
    if f["kind"] == "ui" and f["view"]:
        err("view: chỉ dùng cho kind: field (ui vẽ theo màn chuẩn)")
    return f


def frame_lets(frames):
    """{name: (number, kind)} of the `let`s of a node's parsed frames; a name given two values maps to None."""
    out = {}
    for f in frames:
        for name, val in f["lets"].items():
            num = float(FRAME_NUM_RX.match(val).group(1))
            out[name] = (num, f["kind"]) if out.get(name, (num, f["kind"])) == (num, f["kind"]) else None
    return out


def resolve_frame(f, screen, lookup):
    """The frame with every value as a number and every position absolute, plus what is measurably wrong:
        {kind, title, unit, view: [x1, y1, x2, y2], values, shapes: [{id, shape, label, parent, overlap, bbox, ...}],
         errors, warnings}
    screen: (w, h) in px. lookup(path, name) -> ((number, kind) | None, reason)."""
    out = {"kind": f["kind"], "title": f["title"], "unit": "u" if f["kind"] == "field" else "px", "values": {},
           "shapes": [], "errors": list(f["errors"]), "warnings": []}
    err, warn = out["errors"].append, out["warnings"].append
    field = f["kind"] == "field"
    names = {}
    for name, val in f["lets"].items():
        m = FRAME_NUM_RX.match(val)
        if m.group(2) and not field:
            err(f"let {name}: đơn vị u chỉ dùng cho kind: field")
        names[name] = float(m.group(1))
    for name, (path, ref) in f["uses"].items():
        hit, reason = lookup(path, ref)
        if hit is None:
            err(f"use {name}: {reason}")
        elif hit[1] != f["kind"]:
            err(f"use {name}: '{path}#{ref}' là số của frame {hit[1]}, frame này là {f['kind']}")
        else:
            names[name] = hit[0]
    out["values"] = dict(names)

    def num(raw, label):
        m = FRAME_NUM_RX.match(raw)
        if m:
            if m.group(2) and not field:
                raise ValueError(f"{label}: đơn vị u chỉ dùng cho kind: field")
            return float(m.group(1))
        if raw in names:
            return names[raw]
        raise ValueError(f"{label}: '{raw}' không phải số hay tên let/use đã khai")

    if field:
        m = re.match(r"^x=(\S+)\.\.(\S+)\s+y=(\S+)\.\.(\S+)$", f["view"] or "")
        try:
            if not m:
                raise ValueError("view: kind: field cần 'view: x=<từ>..<tới> y=<từ>..<tới>'")
            view = [num(m.group(1), "view"), num(m.group(3), "view"), num(m.group(2), "view"), num(m.group(4), "view")]
            if view[0] >= view[2] or view[1] >= view[3]:
                raise ValueError("view: khoảng phải từ nhỏ tới lớn")
        except ValueError as e:
            err(str(e))
            view = [0.0, 0.0, 1.0, 1.0]
    else:
        view = [0.0, 0.0, float(screen[0]), float(screen[1])]
    out["view"] = view

    placed = {}
    for s in f["shapes"]:
        p, label = s["params"], f"{s['id']}"
        r = {"id": s["id"], "shape": s["shape"], "label": p.get("label", ("", ""))[1], "parent": "",
             "overlap": s["overlap"], "src": s["src"]}

        def get(key, need=True, default=None):
            if key not in p:
                if need:
                    raise ValueError(f"{label}: hình {s['shape']} cần {key}=")
                return default
            kind_, raw = p[key]
            if kind_ == "text":
                raise ValueError(f"{label}: {key} phải là số, không có ngoặc kép")
            return num(raw, f"{label} {key}")

        def pair(key):
            kind_, raw = p.get(key, ("raw", ""))
            parts = raw.split(",")
            if key not in p or len(parts) != 2:
                raise ValueError(f"{label}: hình {s['shape']} cần {key}=<x>,<y>")
            return num(parts[0], f"{label} {key}"), num(parts[1], f"{label} {key}")

        try:
            ox = oy = 0.0
            if "parent" in p:
                parent = placed.get(p["parent"][1])
                if parent is None or parent["shape"] not in BOX_SHAPES + ("grid",):
                    raise ValueError(f"{label}: parent '{p['parent'][1]}' phải là một hình hộp hoặc lưới khai ở trên")
                r["parent"], ox, oy = parent["id"], parent["x"], parent["y"]
            if s["shape"] in BOX_SHAPES:
                r.update(x=get("x") + ox, y=get("y") + oy, w=get("w"), h=get("h"))
                if s["shape"] == "bar":
                    fill = get("fill", False, 1.0)
                    if not 0 <= fill <= 1:
                        raise ValueError(f"{label}: fill phải từ 0 tới 1")
                    r["fill"] = fill
                r["bbox"] = [r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"]]
            elif s["shape"] == "grid":
                cols, rows = get("cols"), get("rows")
                if cols < 1 or rows < 1 or cols != int(cols) or rows != int(rows):
                    raise ValueError(f"{label}: cols, rows phải là số nguyên dương")
                cell = get("cell", False)
                cw, ch = get("cw", cell is None, cell), get("ch", cell is None, cell)
                gap = get("gap", False, 0.0)
                r.update(x=get("x") + ox, y=get("y") + oy, cols=int(cols), rows=int(rows), cw=cw, ch=ch, gap=gap,
                         w=cols * cw + (cols - 1) * gap, h=rows * ch + (rows - 1) * gap)
                r["bbox"] = [r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"]]
            elif s["shape"] in ROUND_SHAPES:
                r.update(cx=get("cx") + ox, cy=get("cy") + oy, r=get("r"))
                r["bbox"] = [r["cx"] - r["r"], r["cy"] - r["r"], r["cx"] + r["r"], r["cy"] + r["r"]]
            else:
                (x1, y1), (x2, y2) = pair("from"), pair("to")
                r.update(x1=x1 + ox, y1=y1 + oy, x2=x2 + ox, y2=y2 + oy,
                         length=round(((x2 - x1) ** 2 + (y2 - y1) ** 2) ** 0.5, 3))
                r["bbox"] = [min(r["x1"], r["x2"]), min(r["y1"], r["y2"]), max(r["x1"], r["x2"]), max(r["y1"], r["y2"])]
            for key in ("w", "h", "r", "cw", "ch"):
                if key in r and r[key] <= 0:
                    raise ValueError(f"{label}: {key} phải lớn hơn 0")
        except ValueError as e:
            err(f"dòng {s['line']}: {e}")
            continue
        placed[s["id"]] = r
        out["shapes"].append(r)

    def inside(a, b):
        return a[0] >= b[0] - 1e-9 and a[1] >= b[1] - 1e-9 and a[2] <= b[2] + 1e-9 and a[3] <= b[3] + 1e-9

    def fmt(v):
        return f"{v:g}{out['unit'] if out['unit'] == 'u' else ''}"

    for r in out["shapes"]:
        if r["shape"] == "ring":
            continue      # a reach is a measure, it may go past anything
        if not inside(r["bbox"], view):
            warn(f"{r['id']}: tràn ra ngoài {'khung nhìn' if field else 'màn hình'}")
        elif r["parent"] and not inside(r["bbox"], placed[r["parent"]]["bbox"]):
            warn(f"{r['id']}: tràn ra ngoài '{r['parent']}'")
        if r["shape"] == "button" and not field and min(r["w"], r["h"]) < TAP_MIN:
            warn(f"{r['id']}: nút {fmt(r['w'])}x{fmt(r['h'])} px, cạnh ngắn nhỏ hơn cỡ bấm tối thiểu {TAP_MIN} px")
    solid = [r for r in out["shapes"] if r["shape"] not in ROUND_SHAPES[1:] + LINE_SHAPES]
    for i, a in enumerate(solid):
        for b in solid[i + 1:]:
            if a["parent"] != b["parent"] or a["overlap"] or b["overlap"]:
                continue
            ba, bb = a["bbox"], b["bbox"]
            if min(ba[2], bb[2]) - max(ba[0], bb[0]) > 1e-9 and min(ba[3], bb[3]) - max(ba[1], bb[1]) > 1e-9:
                warn(f"{a['id']} và {b['id']} chồng lên nhau (thêm cờ overlap nếu cố ý)")
    return out


def read_frames(folder, where, problems, screen, lookup):
    """[{file, text, ...resolve_frame}] of the node's frames. Syntax errors go to `problems` (they block Chốt like
    any structure problem); measured warnings stay on each frame."""
    out = []
    for name, text in frame_files(folder):
        f = parse_frame(text)
        r = resolve_frame(f, screen or SCREENS["landscape"], lookup)
        if f["kind"] == "ui" and not screen:
            r["warnings"].insert(0, "node gốc chưa khai screen: nên vẽ theo màn ngang 1920x1080")
        for e in r["errors"]:
            problems.append({"path": where, "text": f"{FRAME_DIR}/{name}: {e}"})
        r.update(file=name, text=text, uses=[f"{p}#{n}" for p, n in f["uses"].values()])
        r["svg"] = frame_svg(r)
        out.append(r)
    return out


def _esc(text):
    return (str(text).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;"))


def frame_svg(fr):
    """The resolved frame drawn as one <svg> (classes fr-*: the page and gd_frame.py --html give the colours).
    ui is drawn in px, y down; field in u, y up, scaled to px here. Every shape carries a <title> with its numbers
    and data-* attributes with them (in the frame's own unit)."""
    x1, y1, x2, y2 = fr["view"]
    field = fr["kind"] == "field"
    k = 1920 / (x2 - x1) if field else 1.0      # px per unit of the frame
    vw, vh = round((x2 - x1) * k, 3), round((y2 - y1) * k, 3)
    u = "u" if field else ""

    def X(x):
        return round((x - x1) * k, 3)

    def Y(y):
        return round(((y2 - y) if field else (y - y1)) * k, 3)

    def L(v):
        return round(v * k, 3)

    fs = 34      # px of the 1920-wide drawing: about 13 px on a panel 760 px wide
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" class="fr-svg" viewBox="0 0 {vw:g} {vh:g}" '
             f'preserveAspectRatio="xMidYMid meet" data-kind="{fr["kind"]}">',
             '<defs><marker id="fr-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="5" markerHeight="5" '
             'orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" class="fr-head"/></marker></defs>',
             f'<rect class="fr-screen" x="0" y="0" width="{vw:g}" height="{vh:g}"/>']
    if field and y1 < 0 < y2:
        parts.append(f'<line class="fr-ground" x1="0" y1="{Y(0)}" x2="{vw:g}" y2="{Y(0)}"/>')
    parents = {r["parent"] for r in fr["shapes"]}
    for r in fr["shapes"]:
        tip = f"{r['id']} ({r['shape']})" + (f" {r['label']}" if r["label"] else "")
        data = f'data-id="{_esc(r["id"])}" data-shape="{r["shape"]}"'
        cls = "fr-" + r["shape"]
        if "w" in r:
            tip += f": x={r['x']:g}{u} y={r['y']:g}{u} w={r['w']:g}{u} h={r['h']:g}{u}"
            left, top, w, h = X(r["x"]), Y(r["y"] + r["h"]) if field else Y(r["y"]), L(r["w"]), L(r["h"])
            data += f' data-x="{r["x"]:g}" data-y="{r["y"]:g}" data-w="{r["w"]:g}" data-h="{r["h"]:g}"'
            body = []
            if r["shape"] == "grid":
                tip += f" ({r['cols']}x{r['rows']} ô {r['cw']:g}x{r['ch']:g}, cách {r['gap']:g})"
                for i in range(r["cols"]):
                    for j in range(r["rows"]):
                        cx, cy = left + L(i * (r["cw"] + r["gap"])), top + L(j * (r["ch"] + r["gap"]))
                        body.append(f'<rect class="fr-cell" x="{cx:g}" y="{cy:g}" width="{L(r["cw"]):g}" height="{L(r["ch"]):g}"/>')
            else:
                rx = f' rx="{min(w, h) * 0.12:g}"' if r["shape"] in ("round-rect", "button") else ""
                body.append(f'<rect class="{cls}" x="{left:g}" y="{top:g}" width="{w:g}" height="{h:g}"{rx}/>')
                if r["shape"] == "bar":
                    body.append(f'<rect class="fr-fill" x="{left:g}" y="{top:g}" width="{w * r["fill"]:g}" height="{h:g}"/>')
                    tip += f" fill={r['fill']:g}"
            text = (r["label"] + " · " if r["label"] else "") + f"{r['w']:g}x{r['h']:g}{u}"
            tx, ty = left + w / 2, (top + fs if r["id"] in parents else top + h / 2)
        elif "r" in r:
            tip += f": cx={r['cx']:g}{u} cy={r['cy']:g}{u} r={r['r']:g}{u}"
            data += f' data-cx="{r["cx"]:g}" data-cy="{r["cy"]:g}" data-r="{r["r"]:g}"'
            body = [f'<circle class="{cls}" cx="{X(r["cx"])}" cy="{Y(r["cy"])}" r="{L(r["r"]):g}"/>']
            ring = r["shape"] == "ring"
            if ring:
                body.append(f'<circle class="fr-dot" cx="{X(r["cx"])}" cy="{Y(r["cy"])}" r="5"/>')
            text = (r["label"] + " · " if r["label"] else "") + f"r={r['r']:g}{u}"
            tx, ty = X(r["cx"]), Y(r["cy"]) - (L(r["r"]) + fs * 0.6 if ring else 0)
        else:
            tip += f": {r['x1']:g},{r['y1']:g} → {r['x2']:g},{r['y2']:g} (dài {r['length']:g}{u})"
            data += f' data-x1="{r["x1"]:g}" data-y1="{r["y1"]:g}" data-x2="{r["x2"]:g}" data-y2="{r["y2"]:g}"'
            marker = ' marker-end="url(#fr-arrow)"' if r["shape"] == "arrow" else ""
            body = [f'<line class="{cls}" x1="{X(r["x1"])}" y1="{Y(r["y1"])}" x2="{X(r["x2"])}" y2="{Y(r["y2"])}"{marker}/>']
            text = (r["label"] + " · " if r["label"] else "") + f"{r['length']:g}{u}"
            tx, ty = (X(r["x1"]) + X(r["x2"])) / 2, (Y(r["y1"]) + Y(r["y2"])) / 2 - fs * 0.7
        parts.append(f'<g class="fr-shape" {data}><title>{_esc(tip)}</title>' + "".join(body)
                     + f'<text class="fr-text" x="{tx:g}" y="{ty:g}" font-size="{fs:g}" text-anchor="middle" '
                       f'dominant-baseline="middle">{_esc(text)}</text></g>')
    parts.append("</svg>")
    return "".join(parts)


FRAME_CSS = """.fr-svg { width: 100%; height: auto; display: block; }
.fr-screen { fill: var(--fr-bg, #f6f7f9); stroke: var(--fr-line, #9aa3ad); stroke-width: 4; }
.fr-ground { stroke: var(--fr-line, #9aa3ad); stroke-width: 4; stroke-dasharray: 20 12; }
.fr-rect, .fr-round-rect, .fr-icon, .fr-label, .fr-bar, .fr-circle, .fr-cell { fill: var(--fr-box, #e9eef5); stroke: var(--fr-ink, #44546a); stroke-width: 4; }
.fr-label { fill: none; stroke-dasharray: 12 8; }
.fr-icon { stroke-dasharray: 6 6; }
.fr-button { fill: var(--fr-accent-soft, #d6e6fa); stroke: var(--fr-accent, #2878dc); stroke-width: 5; }
.fr-fill { fill: var(--fr-accent, #2878dc); opacity: .5; }
.fr-ring { fill: none; stroke: var(--fr-warn, #d07a18); stroke-width: 4; stroke-dasharray: 16 10; }
.fr-dot, .fr-head { fill: var(--fr-warn, #d07a18); }
.fr-line, .fr-arrow { stroke: var(--fr-ink, #44546a); stroke-width: 5; }
.fr-text { fill: var(--fr-ink, #44546a); font-family: system-ui, sans-serif; pointer-events: none; }
.fr-shape:hover > :not(text) { stroke-width: 8; }"""


def frame_mentions(folder):
    """The node paths the frames `use`, in backticks: ref_issues then asks for them in refs like any mention."""
    return "\n".join(f"`{path}`" for _, text in frame_files(folder) for path, _ in parse_frame(text)["uses"].values())


def frame_brief(fr):
    """Lines of one resolved frame, for an AI to read: every shape absolute, with its unit."""
    unit = fr["unit"]
    head = f"frame {fr['file']} ({fr['kind']}, {unit}" + (f", màn {fr['view'][2]:g}x{fr['view'][3]:g}" if unit == "px" else
                                                         f", khung x {fr['view'][0]:g}..{fr['view'][2]:g} y {fr['view'][1]:g}..{fr['view'][3]:g}") + ")"
    lines = [head + (f": {fr['title']}" if fr["title"] else "")]
    if fr["values"]:
        lines.append("  số: " + ", ".join(f"{k}={v:g}" for k, v in fr["values"].items()))
    for r in fr["shapes"]:
        if "w" in r:
            pos = f"x={r['x']:g} y={r['y']:g} w={r['w']:g} h={r['h']:g}"
            if r["shape"] == "grid":
                pos += f" ({r['cols']}x{r['rows']} ô {r['cw']:g}x{r['ch']:g}, cách {r['gap']:g})"
            if r["shape"] == "bar":
                pos += f" fill={r['fill']:g}"
        elif "r" in r:
            pos = f"cx={r['cx']:g} cy={r['cy']:g} r={r['r']:g}"
        else:
            pos = f"từ {r['x1']:g},{r['y1']:g} tới {r['x2']:g},{r['y2']:g} (dài {r['length']:g})"
        extra = (f" trong {r['parent']}" if r["parent"] else "") + (f" \"{r['label']}\"" if r["label"] else "")
        lines.append(f"  - {r['id']} {r['shape']} {pos}{extra}")
    lines += [f"  ! {w}" for w in fr["warnings"]]
    lines += [f"  LỖI {e}" for e in fr["errors"]]
    return lines


def read_questions(folder, where, problems):
    """The questions of a node with the human's answers merged in:
    [{id, text, why, multi, options: [{id, label, detail}], chosen: [option id], other, at, answered}]."""
    base = folder / QUESTIONS_DIR
    name = f"{QUESTIONS_DIR}/{QUESTIONS_FILE}"
    try:
        data = read_json(base / QUESTIONS_FILE)
    except ValueError:
        problems.append({"path": where, "text": f"{name} không phải JSON hợp lệ"})
        return []
    if data is None:
        return []
    raw = data.get("questions") if isinstance(data, dict) else None
    if not isinstance(raw, list):
        problems.append({"path": where, "text": f'{name} phải có dạng {{"questions": [...]}}'})
        return []
    saved = read_dict(base / ANSWERS_FILE, "answers")

    out, seen = [], set()
    for i, q in enumerate(raw):
        label = f"{name} câu {i + 1}"
        if not isinstance(q, dict):
            problems.append({"path": where, "text": f"{label}: phải là một object"})
            continue
        qid, text = q.get("id"), q.get("text")
        if not isinstance(qid, str) or not KEY_RX.match(qid) or qid in seen:
            problems.append({"path": where, "text": f"{label}: id phải là chữ thường, số, gạch nối và không trùng"})
            continue
        if not isinstance(text, str) or not text.strip():
            problems.append({"path": where, "text": f"{label} ({qid}): thiếu text"})
            continue
        options, option_ids = [], set()
        for o in q.get("options") if isinstance(q.get("options"), list) else []:
            oid = o.get("id") if isinstance(o, dict) else None
            if (isinstance(oid, str) and KEY_RX.match(oid) and oid not in option_ids
                    and isinstance(o.get("label"), str) and o["label"].strip()):
                option_ids.add(oid)
                options.append({"id": oid, "label": o["label"].strip(), "detail": str(o.get("detail") or "").strip()})
            else:
                problems.append({"path": where, "text": f"{label} ({qid}): mỗi đề xuất cần id (không trùng) và label"})
        if not options:
            problems.append({"path": where, "text": f"{label} ({qid}): cần ít nhất một đề xuất"})
            continue
        seen.add(qid)
        answer = saved.get(qid) if isinstance(saved.get(qid), dict) else {}
        chosen = [c for c in answer.get("chosen") or [] if c in option_ids] if isinstance(answer.get("chosen"), list) else []
        other = answer.get("other") if isinstance(answer.get("other"), str) else ""
        out.append({"id": qid, "text": text.strip(), "why": str(q.get("why") or "").strip(), "multi": q.get("multi") is True,
                    "options": options, "chosen": chosen, "other": other, "at": str(answer.get("at") or ""),
                    "answered": bool(chosen) or bool(other.strip())})
    return out


# ---------------------------------------------------------------- history (written by this code only)
def log(folder, event, **fields):
    """Appends one event to the node's .history/log.jsonl."""
    path = folder / HISTORY_DIR / LOG_FILE
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "a", encoding="utf-8") as f:
        f.write(json.dumps({"at": now(), "event": event, **fields}, ensure_ascii=False) + "\n")


def read_history(folder):
    """Every event of the node, oldest first; lines that are not JSON are skipped."""
    path = folder / HISTORY_DIR / LOG_FILE
    out = []
    for line in read_text(path).split("\n") if path.is_file() else []:
        try:
            entry = json.loads(line)
        except ValueError:
            continue
        if isinstance(entry, dict):
            out.append(entry)
    return out


def rejected(folder):
    """What the human turned down at this node: [{at, item, text, score, feedback}], oldest first."""
    return [e for e in read_history(folder) if e.get("event") == "review" and e.get("rejected")]


def record_versions(root):
    """Notes every change of an idea node's data.md since the last call: a copy of the new version in .history and
    an event saying which ideas were added, removed or rewritten (with the old text). Returns the changed paths."""
    changed = []

    def visit(folder, path):
        data = read_data(folder)
        if data:
            state_path = folder / HISTORY_DIR / STATE_FILE
            try:
                state = read_json(state_path)
            except ValueError:
                state = None
            state = state if isinstance(state, dict) else {}
            if state.get("hash") != data["hash"]:
                old = state.get("items") if isinstance(state.get("items"), dict) else {}
                new = {it["id"]: {"hash": it["hash"], "text": it["text"]} for it in data["items"]}
                history = folder / HISTORY_DIR
                history.mkdir(parents=True, exist_ok=True)
                shutil.copy2(folder / DATA_FILE, history / f"data-{time.strftime('%Y%m%d-%H%M%S')}-{data['hash']}.md")
                log(folder, "data", hash=data["hash"], previous=state.get("hash") or "",
                    added=[i for i in new if i not in old],
                    removed=[{"id": i, "text": old[i].get("text", "")} for i in old if i not in new],
                    rewritten=[{"id": i, "old": old[i].get("text", ""), "new": new[i]["text"]}
                               for i in new if i in old and old[i].get("hash") != new[i]["hash"]])
                write_json(state_path, {"hash": data["hash"], "items": new})
                changed.append(path or "(gốc)")
        for child in sorted(p for p in folder.iterdir() if p.is_dir() and not p.name.startswith(".")):
            visit(child, f"{path}/{child.name}" if path else child.name)

    top = table_dir(root, "idea")
    if top.is_dir():
        visit(top, "")
    return changed


# ---------------------------------------------------------------- refs and the glossary
def related(a, b):
    """Whether node paths a and b are the same node, or one is above the other (based-on covers that)."""
    return a == b or a.startswith(b + "/") or b.startswith(a + "/") or not a or not b


def ref_label(path, term):
    return f"{path}#{term}" if term else path


def normalize_mention(text):
    """(node path, term id) a backtick span names, or None. Accepts the table prefixes and a trailing file name."""
    t = text.strip()
    for prefix in ("Management/game-design/", "ai/idea/", "live/"):
        if t.startswith(prefix):
            t = t[len(prefix):]
    for suffix in ("/" + DATA_FILE, "/" + DOC_FILE, "/"):
        if t.endswith(suffix):
            t = t[:-len(suffix)]
    m = MENTION_TERM_RX.match(t)
    if m:
        return GLOSSARY_KEY, m.group(1)
    return (t, "") if MENTION_PATH_RX.match(t) else None


def glossary_terms(data):
    """([{id, term, meaning, detail, hash}], errors) of the glossary node's data: one idea per term,
    titled "<term>: <meaning>"."""
    out, errors, seen = [], [], {}
    for it in data["items"] if data else []:
        term, sep, meaning = it["title"].partition(":")
        term, meaning = term.strip(), meaning.strip()
        if not it["titled"] or not sep or not term or not meaning:
            errors.append(f"ý '{it['id']}': tiêu đề thuật ngữ phải có dạng '<từ>: <nghĩa>'")
            continue
        key = " ".join(term.lower().split())
        if key in seen:
            errors.append(f"từ '{term}' có ở cả ý '{seen[key]}' và ý '{it['id']}'")
            continue
        seen[key] = it["id"]
        out.append({"id": it["id"], "term": term, "meaning": meaning, "detail": it["detail"], "hash": it["hash"]})
    return out, errors


def ref_issues(table, path, data, text, cache=None):
    """What the refs of the node at `path` (data: its parsed data.md, text: data.md and CLAUDE.md as written)
    say against the files of the same table (`table`: the table folder):
        refs     [{path, term, label, hash, current, ok, title}]   ok None when the target does not exist
        missing  [{path, term, label, current}]   mentioned in backticks, outside its lineage, not declared
        unknown  [text]    a ref that names no node or term, or names its own lineage (a structure problem)
        notes    [text]    a mention of a node that does not exist (a reminder)
    cache: {path: data or None}, shared across calls."""
    cache = {} if cache is None else cache

    def target(p):
        if p not in cache:
            cache[p] = read_data(Path(table).joinpath(*p.split("/")))
        return cache[p]

    def current(p, term):
        """(hash, title) the ref would point to now, or None."""
        d = target(p)
        if d is None:
            return None
        if not term:
            return d["hash"], d["title"]
        if p != GLOSSARY_KEY:
            return None
        hit = next((t for t in glossary_terms(d)[0] if t["id"] == term), None)
        return (hit["hash"], hit["term"]) if hit else None

    out = {"refs": [], "missing": [], "unknown": [], "notes": []}
    declared = set()
    for r in data.get("refs", []) if data else []:
        label = ref_label(r["path"], r["term"])
        declared.add((r["path"], r["term"]))
        now_ = None if (related(r["path"], path) and not r["term"]) else current(r["path"], r["term"])
        if now_ is None:
            out["unknown"].append(f"refs: '{label}' " + ("là chính node này, tổ tiên hay con cháu của nó (based-on đã lo)"
                                                       if related(r["path"], path) and not r["term"] else "không có node hay từ này"))
        out["refs"].append({"path": r["path"], "term": r["term"], "label": label, "hash": r["hash"],
                            "current": now_[0] if now_ else "", "title": now_[1] if now_ else "",
                            "ok": None if now_ is None else now_[0] == r["hash"]})
    seen = set()
    for span in MENTION_RX.findall(text or ""):
        hit = normalize_mention(span)
        if not hit or hit in seen or hit in declared:
            continue
        seen.add(hit)
        p, term = hit
        if not term and (related(p, path) or p.split("/")[0] == GLOSSARY_KEY):
            continue
        now_ = current(p, term)
        if now_ is None:
            out["notes"].append(f"nhắc '{ref_label(p, term)}' nhưng không có node hay từ này")
        else:
            out["missing"].append({"path": p, "term": term, "label": ref_label(p, term), "current": now_[0]})
    return out


def ref_line(issues):
    """The refs line to write, every declared or missing ref at its current hash."""
    entries = [f"{r['label']}@{r['current']}" for r in issues["refs"] if r["ok"] is not None]
    entries += [f"{r['label']}@{r['current']}" for r in issues["missing"]]
    return "refs: " + ", ".join(entries) if entries else ""


def definitions(doc):
    """[(term, meaning)] of the '## Định nghĩa' section of a CLAUDE.md, lines '- **term**: meaning'."""
    out, inside = [], False
    for line in (doc or "").split("\n"):
        if line.startswith("## "):
            inside = line[3:].strip().lower() == DEFINITIONS_TITLE.lower()
            continue
        m = DEF_LINE_RX.match(line) if inside else None
        if m:
            out.append((m.group(1).strip(), (m.group(2) or "").strip()))
    return out


def glossary_view(tree):
    """(glossary, notes) of a table: glossary is None or {path, terms: [{id, term, meaning, detail, hash,
    usedBy: [node path]}]}; notes [{path, text}] are reminders that block nothing (no glossary node yet, a term
    defined outside it or redefined, a term written out without a ref)."""
    if not tree:
        return None, []
    gnode = next((c for c in tree["children"] if c["key"] == GLOSSARY_KEY), None)
    notes = []
    if gnode is None:
        notes.append({"path": GLOSSARY_KEY, "text": "chưa có node glossary: thuật ngữ chung định nghĩa ở đó, "
                                                    "mỗi từ một ý '### <id> | AI n | <từ>: <nghĩa>'"})
    terms = [dict(t, usedBy=[]) for t in (gnode["terms"] if gnode else [])]
    by_id = {t["id"]: t for t in terms}
    by_word = {" ".join(t["term"].lower().split()): t for t in terms}
    outside = {}
    for n in walk(tree):
        if n["path"] == GLOSSARY_KEY:
            continue
        used = {r["term"] for r in n.get("refs", []) if r["path"] == GLOSSARY_KEY and r["term"]}
        for tid in used:
            if tid in by_id:
                by_id[tid]["usedBy"].append(n["path"] or "(gốc)")
        for word, meaning in definitions(n["body"]):
            key = " ".join(word.lower().split())
            if key in by_word:
                notes.append({"path": n["path"] or "(gốc)", "text": f"định nghĩa lại '{word}', từ đã có ở "
                              f"`{GLOSSARY_KEY}#{by_word[key]['id']}`: bỏ định nghĩa, nhắc từ đó và khai refs"})
            else:
                outside.setdefault(key, []).append((n["path"] or "(gốc)", word, meaning))
        text = n.get("refText", "")
        for t in terms:
            if t["id"] in used or not t["term"]:
                continue
            if re.search(r"(?<!\w)" + re.escape(t["term"]) + r"(?!\w)", text, re.I):
                notes.append({"path": n["path"] or "(gốc)", "text": f"có dùng từ '{t['term']}' nhưng chưa khai "
                              f"`{GLOSSARY_KEY}#{t['id']}` trong refs"})
    for key, defs in sorted(outside.items(), key=lambda kv: (-len(kv[1]), kv[0])):      # duplicates first
        where = ", ".join(p for p, _, _ in defs)
        differ = len({" ".join(m.lower().split()) for _, _, m in defs}) > 1
        notes.append({"path": defs[0][0], "text": f"'{defs[0][1]}' còn định nghĩa ngoài glossary ({where})"
                      + (f": {len(defs)} nơi, nghĩa khác nhau, chọn một" if differ else "") + "; chuyển sang glossary"})
    return ({"path": gnode["path"], "terms": terms} if gnode else None), notes


def brief(node):
    """Title, summary and idea titles of a node: the context a reader needs from it."""
    return {"path": node["path"] or "(gốc)", "round": node["round"], "title": node["title"],
            "summary": node["summary"], "items": [it["title"] for it in node["items"]]}


def ancestors(tree, path):
    """The nodes above `path`, from the root down to its parent; [] for the root or a path not in the tree."""
    out, node = [], tree
    for key in path.split("/") if path else []:
        if node is None:
            return []
        out.append(node)
        node = next((c for c in node["children"] if c["key"] == key), None)
    return out if node is not None else []


# ---------------------------------------------------------------- one node
def own_hash(folder):
    """Hash of what belongs to the node itself (data.md, CLAUDE.md, .images, .frame-concept): what promote copies
    into live."""
    h = hashlib.sha1()
    items = [folder / name for name in NODE_FILES + (CODE_FILE,)]
    for extra in (IMAGES_DIR, FRAME_DIR, STYLE_DIR):
        d = folder / extra
        if d.is_dir():
            items += sorted(p for p in d.iterdir() if p.is_file())
    for p in items:
        h.update(p.name.encode("utf-8") + b"\0")
        if p.is_file():
            data = p.read_bytes()
            h.update(data.replace(b"\r", b"") if p.suffix.lower() == ".md" else data)
        h.update(b"\0")
    return h.hexdigest()


def cheat_problems(folder, where, depth, data, items, frames, problems, notes):
    """The rules of a cheat node: right under a feature, no children, at most CHEAT_MAX states, one frame per state
    named after it; a frame with no `use` gets a reminder."""
    def bad(text):
        problems.append({"path": where, "text": f"{CHEAT_KEY}: {text}"})
    if depth != CHEAT_ROUND:
        bad("chỉ đặt ngay dưới node feature (vòng 2)")
    if any(p.is_dir() and not p.name.startswith(".") for p in folder.iterdir()):
        bad("không có node con")
    if len(items) > CHEAT_MAX:
        bad(f"tối đa {CHEAT_MAX} state (ý), đang có {len(items)}")
    if data["frameMode"] == FRAME_NONE:
        bad(f"không khai 'frame: {FRAME_NONE}': mỗi state cần một frame")
    names = {Path(fr["file"]).stem: fr for fr in frames}
    ids = [it["id"] for it in items]
    for i in ids:
        if i not in names:
            bad(f"state '{i}' chưa có frame {FRAME_DIR}/{i}{FRAME_EXT}")
    for n in names:
        if n not in ids:
            bad(f"frame '{n}{FRAME_EXT}' không trùng id của state nào ({', '.join(ids) or 'chưa có state'})")
    for n, fr in names.items():
        if not fr["uses"]:
            notes.append(f"frame cheat '{n}' chưa use số nào: use số từ node sở hữu (kèm refs) để không lệch")


def read_node(folder, path, depth, problems, mode=DEFAULT_MODE, parent_hash=None, review=None, cache=None):
    """One node and everything below it. parent_hash: the data hash of the node above (None for the root).
    review: the folder holding the human's review of this node when it is not `folder` (a live node is reviewed
    in the idea node of the same path). cache: shared by ref_issues across the tree."""
    cache = {} if cache is None else cache
    where = path or "(gốc)"
    data = read_data(folder)
    if data is None:
        problems.append({"path": where, "text": f"thiếu {DATA_FILE}"})
        data = parse_data("")
        data["errors"] = []
    for text in data["errors"]:
        problems.append({"path": where, "text": f"{DATA_FILE}: {text}"})
    doc_file = folder / DOC_FILE
    doc = read_text(doc_file).strip("\n") if doc_file.is_file() else ""
    if not doc_file.is_file():
        problems.append({"path": where, "text": f"thiếu {DOC_FILE}"})
    elif DATA_FILE not in doc:
        problems.append({"path": where, "text": f"{DOC_FILE} phải trỏ tới {DATA_FILE}"})
    if depth > len(ROUNDS):
        problems.append({"path": where, "text": f"sâu hơn vòng {len(ROUNDS)}"})
    images, images_note = read_images(folder, where, problems)
    questions = read_questions(folder, where, problems)
    pending = sum(1 for q in questions if not q["answered"])
    items = data["items"]
    merge_review(review or folder, items)
    stale = parent_hash is not None and data["based_on"] != parent_hash
    table = folder.parents[len(path.split("/")) - 1] if path else folder
    raw = read_text(folder / DATA_FILE) if (folder / DATA_FILE).is_file() else ""
    screen_key = ("screen", str(table))
    if screen_key not in cache:
        top = data if not path else read_data(table)
        cache[screen_key] = (top or {}).get("screen", "")
    screen = SCREENS.get(cache[screen_key])

    def lookup(p, name):
        key = ("lets", str(table), p)
        if key not in cache:
            target_dir = Path(table).joinpath(*p.split("/"))
            cache[key] = (frame_lets([parse_frame(t) for _, t in frame_files(target_dir)])
                          if (target_dir / DATA_FILE).is_file() else None)
        lets = cache[key]
        if lets is None:
            return None, f"không có node '{p}'"
        if name not in lets:
            return None, f"node '{p}' không có let '{name}' trong frame"
        if lets[name] is None:
            return None, f"node '{p}' khai let '{name}' hai giá trị khác nhau"
        return lets[name], ""

    frames = read_frames(folder, where, problems, screen, lookup)
    refs = ref_issues(table, path, data, raw + "\n" + doc + "\n" + frame_mentions(folder), cache)
    frame_notes = []
    if not path and not data["screen"]:
        frame_notes.append(f"chưa khai hướng màn hình: thêm 'screen: landscape' hoặc 'screen: portrait' vào phần đầu {DATA_FILE}")
    if depth == FRAME_ROUND and not frames and data["frameMode"] != FRAME_NONE:
        frame_notes.append(f"chưa có frame: node có bố cục (popup, màn hình, HUD, vị trí hay tầm trên chiến trường) "
                           f"thì viết {FRAME_DIR}/<tên>{FRAME_EXT}; không có bố cục thì khai 'frame: {FRAME_NONE}'")
    cheat = bool(path) and folder.name == CHEAT_KEY
    if depth != FRAME_ROUND and frames and not cheat:
        frame_notes.append(f"frame chỉ dùng ở node vòng {FRAME_ROUND} và node {CHEAT_KEY}")
    if cheat:
        cheat_problems(folder, where, depth, data, items, frames, problems, frame_notes)
    elif depth == FRAME_ROUND and any(fr["kind"] == "ui" for fr in frames) \
            and not (folder.parent.parent / CHEAT_KEY / DATA_FILE).is_file():
        feature = path.split("/")[0]
        problems.append({"path": where, "text": f"feature '{feature}' có frame giao diện mà chưa có node {CHEAT_KEY}: "
                                                f"thêm {feature}/{CHEAT_KEY}/ (tối đa {CHEAT_MAX} state, mỗi state một frame)"})
    styles, style_errors = read_styles(folder)
    if path and (folder / STYLE_DIR).is_dir():
        problems.append({"path": where, "text": f"{STYLE_DIR} chỉ đặt ở node gốc (style là quyết định của vòng 1)"})
    problems.extend({"path": where, "text": e} for e in style_errors)
    for text in refs["unknown"]:
        problems.append({"path": where, "text": text})
    passed, reasons = verdict(items, pending, stale, mode, refs)
    # redo: @GameDesigner has work to do here. Not passing without it means the node only waits for the human.
    low = [it for it in items if (it["user"] if mode == "user" else it["effective"]) is not None
           and (it["user"] if mode == "user" else it["effective"]) < PASS_MIN]
    ref_redo = any(r["ok"] is False for r in refs["refs"]) or bool(refs["missing"])
    redo = stale or not items or bool(low) or ref_redo
    terms = []
    if path == GLOSSARY_KEY:
        terms, errors = glossary_terms(data)
        problems.extend({"path": where, "text": f"{GLOSSARY_KEY}: {t}"} for t in errors)
        if any(p.is_dir() and not p.name.startswith(".") for p in folder.iterdir()):
            problems.append({"path": where, "text": f"node {GLOSSARY_KEY} không có node con"})
    node = {"key": folder.name if path else "", "redo": redo, "path": path, "round": depth, "title": data["title"] or (folder.name if path else ""),
            "summary": data["summary"], "body": doc, "items": items, "dataHash": data["hash"], "basedOn": data["based_on"],
            "parentHash": parent_hash or "", "stale": stale, "passed": passed, "reasons": reasons,
            "images": images, "imagesNote": images_note, "questions": questions, "pending": pending,
            "history": read_history(folder)[-HISTORY_SHOWN:][::-1], "hash": own_hash(folder),
            "priority": read_priority(folder), "progress": read_progress(folder),
            "code": read_code(folder, where, depth, problems), "refs": refs["refs"],
            "refMissing": refs["missing"], "refNotes": refs["notes"], "refLine": ref_line(refs),
            "refStale": [r["label"] for r in refs["refs"] if r["ok"] is False], "terms": terms,
            "frames": frames, "frameNotes": frame_notes, "frameNone": data["frameMode"] == FRAME_NONE, "cheat": cheat,
            "screen": data["screen"], "styles": styles,
            "refText": raw + "\n" + doc, "children": []}
    for child in sorted(p for p in folder.iterdir() if p.is_dir() and not p.name.startswith(".")):
        child_path = f"{path}/{child.name}" if path else child.name
        if not KEY_RX.match(child.name):
            problems.append({"path": child_path, "text": "tên thư mục phải là chữ thường, số, gạch nối (ví dụ feature-hero)"})
        node["children"].append(read_node(child, child_path, depth + 1, problems, mode, data["hash"],
                                          review / child.name if review else None, cache))
    return node


def read_tree(root, table, mode=None):
    """(tree, problems) of one table; tree is None when the table has no folder yet."""
    folder = table_dir(root, table)
    problems = []
    if not folder.is_dir():
        return None, problems
    if not (folder / DATA_FILE).is_file() and not any(p.is_dir() and not p.name.startswith(".") for p in folder.iterdir()):
        return None, problems      # an empty table, not a broken one
    review = table_dir(root, "idea") if table != "idea" else None
    tree = read_node(folder, "", 1, problems, mode or read_mode(root), None, review)
    claimed = {}
    for n in walk(tree):      # one feature folder, one design node
        for name in n["code"]:
            if name.lower() in claimed:
                problems.append({"path": n["path"], "text": f"{CODE_FILE}: thư mục code '{name}' đã được node '{claimed[name.lower()]}' khai"})
            claimed.setdefault(name.lower(), n["path"])
    return tree, problems


def walk(node):
    if node:
        yield node
        for child in node["children"]:
            yield from walk(child)


def mark_states(idea, live, mode=DEFAULT_MODE):
    """Sets on every idea node `state` (new | changed | same, its own content against live), `dirty` (the node or
    something below it differs from live) and `approved` (it passes and, in mode user, is the same as live)."""
    in_live = {n["path"]: n for n in walk(live)}

    def visit(node):
        other = in_live.get(node["path"])
        node["state"] = "new" if other is None else ("same" if other["hash"] == node["hash"] else "changed")
        node["approved"] = node["passed"] and (mode != "user" or node["state"] == "same")
        below = [visit(c) for c in node["children"]]
        live_keys = {c["key"] for c in other["children"]} if other else set()
        gone = bool(live_keys - {c["key"] for c in node["children"]})      # live has a child that idea dropped
        node["dirty"] = node["state"] != "same" or any(below) or gone
        return node["dirty"]

    if idea:
        visit(idea)


def check_path(path):
    """A node path as the page sends it ("" is the root): keys joined by "/". Raises ValueError otherwise."""
    if not isinstance(path, str):
        raise ValueError("path must be text")
    parts = [p for p in path.split("/")] if path else []
    if len(parts) >= len(ROUNDS) or any(not KEY_RX.match(p) for p in parts):
        raise ValueError(f"not a node path: '{path}'")
    return parts


# ---------------------------------------------------------------- the write gate
def node_approved(root, parts, mode, parent_hash):
    """(approved, reason, data hash) of the idea node at `parts`, read straight from the files."""
    name = "/".join(parts) or "(gốc)"
    src = table_dir(root, "idea").joinpath(*parts)
    data = read_data(src)
    if data is None:
        return False, f"node cha '{name}' chưa có {DATA_FILE}", ""
    merge_review(src, data["items"])
    pending = sum(1 for q in read_questions(src, name, []) if not q["answered"])
    stale = parent_hash is not None and data["based_on"] != parent_hash
    doc = read_text(src / DOC_FILE) if (src / DOC_FILE).is_file() else ""
    refs = ref_issues(table_dir(root, "idea"), "/".join(parts), data,
                      read_text(src / DATA_FILE) + "\n" + doc + "\n" + frame_mentions(src))
    passed, reasons = verdict(data["items"], pending, stale, mode, refs)
    if not passed:
        return False, f"node cha '{name}' chưa đạt: " + "; ".join(reasons[:4]), data["hash"]
    if mode == "user":
        dst = table_dir(root, "live").joinpath(*parts)
        if not (dst / DATA_FILE).is_file():
            return False, f"node cha '{name}' chưa được người chốt sang live", data["hash"]
        if own_hash(src) != own_hash(dst):
            return False, f"node cha '{name}' đang khác bản đã chốt trong live; chờ người chốt lại", data["hash"]
    return True, "", data["hash"]


def can_write_node(root, parts):
    """(ok, reason): may @GameDesigner write inside the idea node at `parts` (its keys; [] is the root)?
    Only when every node above it is approved. The node itself may always be written (that is how it is redone)."""
    mode = read_mode(root)
    parent_hash = None
    for i in range(len(parts)):
        ok, reason, parent_hash = node_approved(root, parts[:i], mode, parent_hash)
        if not ok:
            return False, reason
    return True, ""


# ---------------------------------------------------------------- what the human saves from the page
def save_answers(root, path, answers):
    """Stores the human's answers for the idea node at `path`: {question id: {chosen: [option id], other: text}}.
    Every answer must name an existing question, choose existing options (one unless the question is multi) or
    give other text. Returns the number of answers written."""
    parts = check_path(path)
    folder = table_dir(root, "idea").joinpath(*parts)
    if not folder.is_dir():
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    questions = {q["id"]: q for q in read_questions(folder, path, [])}
    if not isinstance(answers, dict) or not answers:
        raise ValueError("không có câu trả lời nào")
    clean = {}
    for qid, a in answers.items():
        q = questions.get(qid)
        if not q or not isinstance(a, dict):
            raise ValueError(f"không có câu hỏi '{qid}' ở node này")
        chosen, other = a.get("chosen") or [], a.get("other") or ""
        valid = {o["id"]: o["label"] for o in q["options"]}
        if not isinstance(chosen, list) or any(c not in valid for c in chosen) or len(set(chosen)) != len(chosen):
            raise ValueError(f"câu '{qid}': lựa chọn không có trong các đề xuất")
        if len(chosen) > 1 and not q["multi"]:
            raise ValueError(f"câu '{qid}' chỉ được chọn một")
        if not isinstance(other, str) or len(other) > MAX_TEXT:
            raise ValueError(f"câu '{qid}': ý kiến khác phải là chữ, tối đa {MAX_TEXT} kí tự")
        if not chosen and not other.strip():
            raise ValueError(f"câu '{qid}' chưa có lựa chọn hay ý kiến khác")
        clean[qid] = {"chosen": chosen, "other": other.strip(), "at": now()}
    target = folder / QUESTIONS_DIR / ANSWERS_FILE
    merged = read_dict(target, "answers")
    merged.update(clean)
    write_json(target, {"answers": merged})
    for qid, a in clean.items():
        q = questions[qid]
        labels = {o["id"]: o["label"] for o in q["options"]}
        log(folder, "answer", question=qid, text=q["text"], chosen=[labels[c] for c in a["chosen"]], other=a["other"])
    return len(clean)


def save_review(root, path, items, table="idea"):
    """Stores the human's score and feedback for ideas of the node at `path` of `table` (idea or live):
    {idea id: {score: 0-10 or None, feedback: text}}. A score belongs to the text the idea has in that table, so
    it shows in the other table too wherever the text is the same. The review is kept in the idea node.
    Returns the number of ideas written."""
    parts = check_path(path)
    if table not in TABLES:
        raise ValueError(f"không có bảng '{table}'")
    folder = table_dir(root, "idea").joinpath(*parts)
    source = table_dir(root, table).joinpath(*parts)
    data = read_data(source) if source.is_dir() else None
    if data is None:
        raise ValueError(f"{table} không có node '{path or '(gốc)'}'")
    if not (folder / DATA_FILE).is_file():
        raise ValueError(f"node '{path or '(gốc)'}' không còn ở bảng đề xuất nên không lưu điểm được")
    known = {it["id"]: it for it in data["items"]}
    if not isinstance(items, dict) or not items:
        raise ValueError("không có điểm hay phản hồi nào")
    clean = {}
    for iid, r in items.items():
        it = known.get(iid)
        if not it or not isinstance(r, dict):
            raise ValueError(f"không có ý '{iid}' ở node này")
        score, feedback = r.get("score"), r.get("feedback") or ""
        if score is not None and (not isinstance(score, int) or isinstance(score, bool) or not 0 <= score <= 10):
            raise ValueError(f"ý '{iid}': điểm phải là số nguyên từ 0 tới 10")
        if not isinstance(feedback, str) or len(feedback) > MAX_TEXT:
            raise ValueError(f"ý '{iid}': phản hồi phải là chữ, tối đa {MAX_TEXT} kí tự")
        if score is None and not feedback.strip():
            raise ValueError(f"ý '{iid}' chưa có điểm hay phản hồi")
        clean[iid] = {"score": score, "feedback": feedback.strip(), "at": now()}
    target = folder / REVIEW_DIR / REVIEW_FILE
    saved = read_dict(target, "items")
    merged = {iid: review_versions(saved, iid) for iid in saved}
    for iid, r in clean.items():
        merged.setdefault(iid, {})[known[iid]["hash"]] = r
    write_json(target, {"items": merged})
    for iid, r in clean.items():
        log(folder, "review", table=table, item=iid, text=known[iid]["text"], ai=known[iid]["ai"], score=r["score"],
            feedback=r["feedback"], rejected=r["score"] is not None and r["score"] < PASS_MIN)
    return len(clean)


# ---------------------------------------------------------------- idea -> live
def promote(root, path, deep, by="người"):
    """Copies the idea node at `path` into live. deep: the whole branch replaces live's branch; otherwise only the
    node's own data.md, CLAUDE.md and .images, live's children stay. Returns the list of node paths written.
    The parent must already be in live; every node copied must have no structure problem and must pass.
    Questions, reviews and history stay in the idea table."""
    parts = check_path(path)
    src = table_dir(root, "idea").joinpath(*parts)
    dst = table_dir(root, "live").joinpath(*parts)
    if not src.is_dir():
        raise ValueError(f"idea không có node '{path or '(gốc)'}'")
    if parts and not (dst.parent / DATA_FILE).is_file():
        raise ValueError("node cha chưa được chốt trong live; chốt node cha trước")
    parent = read_data(src.parent) if parts else None
    problems = []
    node = read_node(src, path, len(parts) + 1, problems, read_mode(root), parent["hash"] if parent else None)
    if not deep:
        node["children"] = []
        problems = [p for p in problems if p["path"] == (path or "(gốc)")]
    if problems:
        raise ValueError("còn lỗi cấu trúc: " + "; ".join(f"{p['path']}: {p['text']}" for p in problems[:5]))
    failing = [n for n in walk(node) if not n["passed"]]
    if failing:
        raise ValueError("chưa đạt: " + "; ".join(f"{n['path'] or '(gốc)'}: {n['reasons'][0]}" for n in failing[:5]))

    work = Path(root).joinpath(*BASE)
    tmp, old = work / ".promote-new", work / ".promote-old"
    for leftover in (tmp, old):
        shutil.rmtree(leftover, ignore_errors=True)
    if deep:
        shutil.copytree(src, tmp, ignore=shutil.ignore_patterns(*NOT_COPIED))
    else:
        tmp.mkdir(parents=True)
        for name in NODE_FILES:
            shutil.copy2(src / name, tmp / name)
        if (src / CODE_FILE).is_file():
            shutil.copy2(src / CODE_FILE, tmp / CODE_FILE)
        for extra in (IMAGES_DIR, FRAME_DIR, STYLE_DIR):
            if (src / extra).is_dir():
                shutil.copytree(src / extra, tmp / extra)
        if dst.is_dir():      # keep what live already has below this node
            for child in dst.iterdir():
                if child.is_dir() and not child.name.startswith("."):
                    shutil.copytree(child, tmp / child.name)
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        os.replace(dst, old)
    os.replace(tmp, dst)
    shutil.rmtree(old, ignore_errors=True)
    for n in walk(node):
        log(table_dir(root, "idea").joinpath(*(n["path"].split("/") if n["path"] else [])), "promote", by=by, hash=n["dataHash"])
    return [n["path"] or "(gốc)" for n in walk(node)]


def sync_live(root):
    """The Sync button, either mode: copies into live every passing idea node that differs from live, from the
    root down, and does not go below a node that does not pass. Returns the node paths written."""
    mode = read_mode(root)
    written = []

    def visit(node):
        if not node["passed"]:
            return
        if node["state"] != "same":
            try:
                promote(root, node["path"], False, by="người (Sync)")
            except (ValueError, OSError):
                return      # a structure problem: leave it and everything below for the human to see
            written.append(node["path"] or "(gốc)")
        for child in node["children"]:
            visit(child)

    live, _ = read_tree(root, "live", mode)
    idea, _ = read_tree(root, "idea", mode)
    mark_states(idea, live, mode)
    if idea:
        visit(idea)
    return written


def missing_infra(idea):
    """What the proposal still owes of INFRA_FEATURES, as [{"path", "text"}]. A reminder: it blocks nothing.
    Empty until round 1 passes, since nothing may be written under it before that."""
    if not idea or not idea["passed"]:
        return []
    nodes = {n["key"]: n for n in idea["children"]}
    out = []
    for key, folder in INFRA_FEATURES.items():
        want = json.dumps({"unity": [folder]}, ensure_ascii=False)
        if key not in nodes:
            out.append({"path": key, "text": f"chưa có node feature hạ tầng này ({CODE_FILE}: {want})"})
        elif folder.lower() not in [name.lower() for name in nodes[key]["code"]]:
            out.append({"path": key, "text": f"{CODE_FILE} phải khai {want}"})
    return out


FOLDER_NAME_RX = re.compile(r"^(_Services/)?[A-Z][A-Za-z0-9]*$")      # a feature folder validate.py accepts


def suggested_code_name(key):
    """The code folder a round-2 node names in code.json, made from its key: 'feature-popup-lose' -> 'PopupLose'.
    The infrastructure features keep their fixed folders. None when the key gives no usable folder name."""
    if key in INFRA_FEATURES:
        return INFRA_FEATURES[key]
    words = key[len("feature-"):] if key.startswith("feature-") else key
    name = "".join(w[:1].upper() + w[1:] for w in words.split("-") if w)
    return name if FOLDER_NAME_RX.match(name) else None


def missing_code(idea):
    """Round-2 nodes of the proposal without a (valid) code.json, with the code.json to write, as [{"path", "text"}].
    A reminder: it blocks nothing. The infrastructure features are reported by missing_infra."""
    if not idea or not idea["passed"]:
        return []
    out = []
    for n in idea["children"]:
        if n["code"] or n["key"] in INFRA_FEATURES or n["key"] == GLOSSARY_KEY:
            continue
        name = suggested_code_name(n["key"])
        text = (f"ghi {CODE_FILE}: " + json.dumps({"unity": [name]}, ensure_ascii=False)) if name else (
            f"key không cho ra tên thư mục hợp lệ; hỏi người tên thư mục rồi ghi {CODE_FILE}")
        out.append({"path": n["path"], "text": text})
    return out


def live_code_names(root):
    """[(folder, node key)] for every code folder a round-2 node of the LIVE table names in code.json, sorted by
    folder; a folder named twice keeps its first node. Read straight from the files, like code_allowed."""
    live = table_dir(root, "live")
    seen = {}
    for node in sorted(c for c in live.iterdir() if c.is_dir() and not c.name.startswith(".")) if live.is_dir() else []:
        for name in read_code(node, node.name, CODE_ROUND, []):
            seen.setdefault(name, node.name)
    return sorted(seen.items(), key=lambda kv: kv[0].lower())


def live_feature_names(root):
    """[(folder, node key, declared)] for every feature node (round 2) of the LIVE table: the folders its code.json
    names (declared=True), or, when it has none yet, the folder GD will name from its key (suggested_code_name,
    declared=False). Used to prepare chats and folders ahead of time; the code gate still needs code.json."""
    out = {name: (node, True) for name, node in live_code_names(root)}
    live = table_dir(root, "live")
    for node in sorted(c for c in live.iterdir() if c.is_dir() and not c.name.startswith(".")) if live.is_dir() else []:
        if (not (node / DATA_FILE).is_file() or node.name == GLOSSARY_KEY
                or read_code(node, node.name, CODE_ROUND, []) or (node / CODE_FILE).is_file()):
            continue      # not a node, the glossary, already declared, or a broken code.json (a problem gd_status shows)
        name = suggested_code_name(node.name)
        if name and name not in out:
            out[name] = (node.name, False)
    return sorted(((n, k, d) for n, (k, d) in out.items()), key=lambda x: x[0].lower())


# ---------------------------------------------------------------- art concepts (read here, written by studio.py)
def design_owner(path):
    """The order owner of a design node: _design/<path with '.' for '/'>."""
    return f"{DESIGN_OWNER}/{path.replace('/', '.')}"


def art_orders_dir(root):
    return Path(root) / "Management" / "studio" / ART_TOOL / "orders" / DESIGN_OWNER


def _json(path):
    try:
        value = json.loads(read_text(path))
    except (OSError, ValueError):
        return None
    return value


def frame_text_hash(text):
    return short_hash(tidy(text.replace("\r", "")))


def read_art(root, live):
    """What art each design node has, and the orders not delivered yet:
        ({node path: [{frame, file, ref, version, state, level, feedback, label, stale, usable, anchor, at}]}, [pending order])
    file is relative to the project root. One entry per frame: its newest delivery. level: the human's judgement of
    the newest delivery (0-3, None for "Chưa duyệt"; review.json of the order, studio.py). state: "mới nhất" or "cũ
    hơn". label: the level's name | CŨ · node đã đổi | CŨ · style đã đổi (stale wins). usable: newest, not stale and
    not judged "Làm lại ngay".
    live: the live tree (read_tree), for the node hashes and the styles they are checked against."""
    base = art_orders_dir(root)
    nodes = {n["path"]: n for n in walk(live)} if live else {}
    styles = {s["id"]: s["hash"] for s in (nodes.get("", {}) or {}).get("styles", [])}
    anchors = _json(Path(root) / "Management" / "studio" / ART_TOOL / ANCHORS_FILE) or []
    anchored = {(a.get("ref"), a.get("version"), a.get("item")) for a in anchors if isinstance(a, dict)}
    art, pending = {}, []
    if not base.is_dir():
        return art, pending
    for order in sorted(base.glob("*/*/order.md")):
        folder = order.parent
        dotted, oid = folder.parent.name, folder.name
        path = dotted.replace(".", "/")
        ref = f"{DESIGN_OWNER}/{dotted}/{oid}"
        status = _json(folder / "status.json") or {}
        state, version = status.get("state") or "new", int(status.get("version") or 0)
        if state not in ("new", "doing", "delivered"):      # a status written before judging replaced accept/reject
            state = "delivered" if version else "new"
        reviews = _json(folder / "review.json")
        reviews = reviews if isinstance(reviews, dict) else {}
        if state in ("new", "doing") or not version:      # being made (the first time, or again)
            text = read_text(order)
            title = next((l[2:].strip() for l in text.split("\n") if l.startswith("# ")), oid)
            items = [m.group(1) for m in re.finditer(r"(?m)^\s*-\s*`?([a-z0-9]+(?:-[a-z0-9]+)*)`?\s*:", text)]
            style = next((l.split(":", 1)[1].strip() for l in text.split("\n") if l.startswith("style:")), "")
            pending.append({"ref": ref, "node": path, "title": title, "items": items, "style": style,
                            "state": "doing" if state == "doing" else "new", "version": version})
        for v in range(1, version + 1):
            vdir = Path(root) / "Resources" / DESIGN_OWNER / dotted / ART_TOOL / oid / f"v{v}"
            meta = _json(vdir / ART_META)
            if not isinstance(meta, dict):
                continue
            node = nodes.get(path)
            label_state = "cũ hơn" if v < version else "mới nhất"
            stale = []
            if not node or meta.get("nodeHash") != node["dataHash"]:
                stale.append("node đã đổi")
            if any(styles.get(sid) != h for sid, h in (meta.get("styles") or {}).items()):
                stale.append("style đã đổi")
            quality = _json(vdir / "quality.json") or {}
            for name in sorted(x.name for x in vdir.iterdir() if x.is_file() and x.name not in (ART_META, "quality.json")):
                item = Path(name).stem
                r = reviews.get(item) if v == version and isinstance(reviews.get(item), dict) else {}
                level = r.get("level")
                level = level if isinstance(level, int) and not isinstance(level, bool) and 0 <= level < len(ART_LEVELS) else None
                redoing = v == version and state == "doing"     # @Codex is remaking it: nothing to judge until the next delivery
                label = ("cũ hơn" if v < version else "CŨ · " + stale[0] if stale else "đang làm lại" if redoing
                         else (ART_LEVEL_NONE if level is None else ART_LEVELS[level]))
                entry = {"frame": item, "file": (vdir / name).relative_to(Path(root)).as_posix(), "ref": ref,
                         "version": v, "state": label_state, "label": label, "stale": stale,
                         "level": level, "feedback": str(r.get("text") or ""), "reviewable": v == version and state == "delivered", "redoing": redoing,
                         "usable": v == version and not stale and level != 0,
                         "anchor": (ref, v, item) in anchored, "at": meta.get("at", ""), "stamp": meta.get("stamp", 0),
                         "styles": sorted(meta.get("styles") or {})}
                q = quality.get(item) if isinstance(quality.get(item), dict) else None
                entry["below"] = (q.get("note") or "chưa đạt") if q and not q.get("ok") else ""
                art.setdefault(path, []).append(entry)
    for path, entries in art.items():      # newest delivery of each frame first, the older ones after it
        entries.sort(key=lambda e: (e["frame"], e["at"], e["stamp"]))
        newest = {}
        for e in entries:
            newest[e["frame"]] = e
        art[path] = sorted(newest.values(), key=lambda e: e["frame"]) + [e for e in entries if newest[e["frame"]] is not e]
    return art, pending


def snapshot(root):
    """Everything the page shows: the mode, the score scale, both trees (idea marked against live) and their
    structure problems. Reads only."""
    mode = read_mode(root)
    live, live_problems = read_tree(root, "live", mode)
    idea, idea_problems = read_tree(root, "idea", mode)
    mark_states(idea, live, mode)
    mark_priority(idea)
    mark_priority(live)
    ranked = {n["path"]: n for n in walk(idea)}
    for n in walk(live):      # importance and progress are kept with the proposed node; the fixed node shows them
        if n["path"] in ranked:
            n["priority"], n["progress"] = ranked[n["path"]]["priority"], ranked[n["path"]]["progress"]
    for n in walk(live):      # whether each named feature folder may be coded now (the gate guard.py applies)
        n["codeGate"] = [{"unity": name, "ok": ok, "reason": reason} for name in n["code"] for ok, reason in [code_allowed(root, name)]]
    for n in walk(idea):
        n["codeGate"] = [{"unity": name, "ok": ok, "reason": reason} for name in n["code"] for ok, reason in [code_allowed(root, name)]]
    art, art_pending = read_art(root, live)
    for name, tree in (("idea", idea), ("live", live)):
        for n in walk(tree):
            n["art"] = art.get(n["path"], [])
    glossary, term_notes = {}, {}
    for name, tree in (("idea", idea), ("live", live)):
        glossary[name], term_notes[name] = glossary_view(tree)
        for n in walk(tree):
            n.pop("refText", None)      # read by glossary_view only; the page does not need a second copy
    return {"rounds": ROUNDS, "mode": mode, "frameCss": FRAME_CSS, "screens": SCREENS, "artPending": art_pending, "artLevels": list(ART_LEVELS), "artLevelNone": ART_LEVEL_NONE, "glossary": glossary, "termNotes": term_notes, "bands": BANDS, "passMin": PASS_MIN, "phases": PHASES, "workStates": list(WORK_STATES), "live": live, "idea": idea,
            "problems": {"live": live_problems, "idea": idea_problems}, "missing": missing_infra(idea),
            "missingCode": missing_code(idea)}
