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
    .ai-need-user-choose/      questions.json (by @GameDesigner), answers.json (by the page)
    .user-review/review.json   the human's score and feedback per idea (by the page)
    .history/                  log.jsonl and a copy of every version of data.md (by this code only)
Folders starting with "." are not nodes. live holds only data.md, CLAUDE.md and .images.

data.md:
    ---
    based-on: <data hash of the parent it was written against>      (not on the root)
    ---
    # <title>

    <summary, 1-3 sentences>

    ## Ý cốt lõi

    ### <idea-id> | AI <0-10> | <the idea in one short line: what the human reads and scores>
    <detail, optional: shown when the human opens the idea>
The data hash covers title, summary and every idea's id, title and detail; not the scores, not based-on. A child whose
based-on differs from its parent's data hash is stale: it must be redone against the new data.

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
To add a round: add its name to ROUNDS. Standard library only.
"""
import hashlib
import json
import os
import re
import shutil
import time
from pathlib import Path

ROUNDS = ["Ý tưởng sơ khai", "Feature (tóm tắt)", "Tính năng chi tiết", "Yêu cầu kĩ thuật"]
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
NOT_COPIED = (QUESTIONS_DIR, REVIEW_DIR, HISTORY_DIR)      # stay in the idea table
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
    """{based_on, title, summary, items: [{id, text, ai, hash}], hash, errors} of a data.md."""
    errors, based_on = [], ""
    text = text.strip("\n")
    if text.startswith("---\n"):
        head, sep, rest = text[4:].partition("\n---")
        if sep:
            text = rest.lstrip("-").lstrip("\n")
            for line in head.split("\n"):
                key, _, value = line.partition(":")
                if key.strip() == "based-on":
                    based_on = (value.split() or [""])[0]
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
    return {"based_on": based_on, "title": title, "summary": summary, "items": out, "hash": data_hash, "errors": errors}


def read_data(folder):
    path = folder / DATA_FILE
    return parse_data(read_text(path)) if path.is_file() else None


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
    def up(node):
        p = node["priority"]
        worth = [w for w in (up(c) for c in node["children"]) if w is not None]
        p["leaf"] = not node["children"]
        p["ignoredAi"] = not p["leaf"] and p["ai"] is not None
        p["base"] = p["ai"] if p["leaf"] else (max(worth) if worth else None)
        return p["user"] if p["user"] is not None else p["base"]

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
        for child in node["children"]:
            down(child, offset, source)

    if tree:
        up(tree)
        down(tree, 0, "")


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


def verdict(items, pending, stale, mode):
    """(passed, reasons) of a node: see the module docstring."""
    reasons = []
    if stale:
        reasons.append("data của node cha đã đổi: rà lại node theo data mới rồi ghi lại based-on")
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


# ---------------------------------------------------------------- one node
def own_hash(folder):
    """Hash of what belongs to the node itself (data.md, CLAUDE.md, .images): what promote copies into live."""
    h = hashlib.sha1()
    items = [folder / name for name in NODE_FILES]
    images = folder / IMAGES_DIR
    if images.is_dir():
        items += sorted(p for p in images.iterdir() if p.is_file())
    for p in items:
        h.update(p.name.encode("utf-8") + b"\0")
        if p.is_file():
            data = p.read_bytes()
            h.update(data.replace(b"\r", b"") if p.suffix.lower() == ".md" else data)
        h.update(b"\0")
    return h.hexdigest()


def read_node(folder, path, depth, problems, mode=DEFAULT_MODE, parent_hash=None, review=None):
    """One node and everything below it. parent_hash: the data hash of the node above (None for the root).
    review: the folder holding the human's review of this node when it is not `folder` (a live node is reviewed
    in the idea node of the same path)."""
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
    passed, reasons = verdict(items, pending, stale, mode)
    # redo: @GameDesigner has work to do here. Not passing without it means the node only waits for the human.
    low = [it for it in items if (it["user"] if mode == "user" else it["effective"]) is not None
           and (it["user"] if mode == "user" else it["effective"]) < PASS_MIN]
    redo = stale or not items or bool(low)
    node = {"key": folder.name if path else "", "redo": redo, "path": path, "round": depth, "title": data["title"] or (folder.name if path else ""),
            "summary": data["summary"], "body": doc, "items": items, "dataHash": data["hash"], "basedOn": data["based_on"],
            "parentHash": parent_hash or "", "stale": stale, "passed": passed, "reasons": reasons,
            "images": images, "imagesNote": images_note, "questions": questions, "pending": pending,
            "history": read_history(folder)[-HISTORY_SHOWN:][::-1], "hash": own_hash(folder),
            "priority": read_priority(folder), "children": []}
    for child in sorted(p for p in folder.iterdir() if p.is_dir() and not p.name.startswith(".")):
        child_path = f"{path}/{child.name}" if path else child.name
        if not KEY_RX.match(child.name):
            problems.append({"path": child_path, "text": "tên thư mục phải là chữ thường, số, gạch nối (ví dụ feature-hero)"})
        node["children"].append(read_node(child, child_path, depth + 1, problems, mode, data["hash"],
                                          review / child.name if review else None))
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
    return read_node(folder, "", 1, problems, mode or read_mode(root), None, review), problems


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
    passed, reasons = verdict(data["items"], pending, stale, mode)
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
        if (src / IMAGES_DIR).is_dir():
            shutil.copytree(src / IMAGES_DIR, tmp / IMAGES_DIR)
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


def snapshot(root):
    """Everything the page shows: the mode, the score scale, both trees (idea marked against live) and their
    structure problems. Reads only."""
    mode = read_mode(root)
    live, live_problems = read_tree(root, "live", mode)
    idea, idea_problems = read_tree(root, "idea", mode)
    mark_states(idea, live, mode)
    mark_priority(idea)
    mark_priority(live)
    ranked = {n["path"]: n["priority"] for n in walk(idea)}
    for n in walk(live):      # importance is kept with the proposed node; the fixed node of the same path shows it
        n["priority"] = ranked.get(n["path"], n["priority"])
    return {"rounds": ROUNDS, "mode": mode, "bands": BANDS, "passMin": PASS_MIN, "phases": PHASES, "live": live, "idea": idea,
            "problems": {"live": live_problems, "idea": idea_problems}}
