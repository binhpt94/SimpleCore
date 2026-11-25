#!/usr/bin/env python3
"""The requirement book of a feature: what the fixed design (live) asks of its code, one line at a time, and how far
the feature session has got with each. Read by gd_req.py, gd_pack.py, gd_status.py, gd_progress.py (through
gamedesign.save_progress), the page and the guard hooks.

Requirements are never written by hand: every leaf of the feature's branch in live (the round-4 nodes and the cheat
node) gives
    <node path>#<idea id>     each idea of its data.md                     hash: the idea's own hash (title and detail)
    <node path>#edge-<slug>   each line under '## Trường hợp biên' of CLAUDE.md   hash: the line and its sub-lines
    <node path>#params        the sections '## Làm gì', '## Thông số', '## Thông số theo giai đoạn' together
Ids are names, never positions: moving an idea or a node's children around changes nothing.

What the sessions did is kept beside the leaf's progress, in the PROPOSED table (live is a copy and is replaced on
every sync):  Management/game-design/ai/idea/<node>/.progress/requirements.json
    {"items": {"<id>": {state, hash, text, note, tests, play, by, at}}, "log": [{at, id, state, by, note}]}
Only this module writes it (gd_req.py for the sessions, the page for the human).

Nothing derived is stored. Every read builds the list from live again, compares each requirement with what is
saved, and looks for its tests again, so the book never says "done" for something the design or the code no longer
has:
    saved hash != hash now        -> changed   (đổi thiết kế: not done; the old text is shown next to the new one)
    a test named is gone          -> no-test   (mất test: not done, even when it was verified)
    saved id no longer in live    -> dropped   (đã bỏ: not counted; shown so its code or test is removed)
    new id                        -> todo      (with a hint when a dropped requirement has the same name or text)
A requirement counts as met only when it is verified (by @CodeLead) or waived (by the human), its hash still matches
and its tests are still there.
"""
import json
import re
import sys
import unicodedata
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

BOOK_FILE = "requirements.json"
LOG_MAX = 300
EDGE_TITLE = "Trường hợp biên"
PARAM_TITLES = ("Làm gì", "Thông số", "Thông số theo giai đoạn")
PARAMS_ID = "params"
SLUG_WORDS = 8

# saved states (what a session wrote) ...
SAVED = ("todo", "doing", "done", "verified", "rejected", "waived")
# ... and the states a read gives (saved state, or what the check found)
STATE_NAMES = {
    "doing": "đang làm", "rejected": "bị trả", "changed": "đổi thiết kế", "no-test": "mất test", "todo": "chưa làm",
    "done": "chờ kiểm", "verified": "đã kiểm", "waived": "bỏ qua (người)", "dropped": "đã bỏ",
}
ORDER = ("doing", "rejected", "changed", "no-test", "todo", "done", "verified", "waived")
MET = ("verified", "waived")
TEST_RX = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*$")
REF_RX = re.compile(r"^([a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*)*)#([a-z0-9]+(?:-[a-z0-9]+)*)$")


# ---------------------------------------------------------------- the list, from live
def slug(text, words=SLUG_WORDS):
    text = text.replace("đ", "d").replace("Đ", "D")
    text = "".join(c for c in unicodedata.normalize("NFD", text) if unicodedata.category(c) != "Mn").lower()
    parts = re.findall(r"[a-z0-9]+", text)
    return "-".join(parts[:words]) or "x"


def sections(doc):
    """{title: text} of the '## ' sections of a CLAUDE.md (text without the heading line)."""
    out, name, lines = {}, None, []
    for line in doc.replace("\r", "").split("\n"):
        if line.startswith("## "):
            if name is not None:
                out[name] = "\n".join(lines)
            name, lines = line[3:].strip(), []
        elif name is not None:
            lines.append(line)
    if name is not None:
        out[name] = "\n".join(lines)
    return out


def edge_items(text):
    """[(title, whole text)] of the top-level '- ' lines of an edge-case section; indented lines belong to the line above."""
    items = []
    for line in text.split("\n"):
        if line.startswith("- "):
            items.append([line[2:].strip()])
        elif items and line.strip() and line[:1] in (" ", "\t"):
            items[-1].append(line.strip())
    out = []
    for lines in items:
        head = lines[0]
        m = re.match(r"\*\*(.+?)\*\*", head)
        if m:
            title = m.group(1).strip().rstrip(":")
        else:
            title = re.split(r":\s", head, maxsplit=1)[0].strip()
        out.append((title, "\n".join(lines)))
    return out


def node_requirements(data, doc):
    """[{id, kind, title, text, hash}] of one leaf, from its parsed data.md and its CLAUDE.md text."""
    out, seen = [], set()
    for it in data["items"]:
        text = it["title"] + ("\n" + it["detail"] if it.get("detail") else "")
        out.append({"id": it["id"], "kind": "idea", "title": it["title"], "text": text, "hash": it["hash"]})
        seen.add(it["id"])
    secs = sections(doc)
    for title, text in edge_items(secs.get(EDGE_TITLE, "")):
        rid = "edge-" + slug(title)
        base, n = rid, 1
        while rid in seen:
            n += 1
            rid = f"{base}-{n}"
        seen.add(rid)
        out.append({"id": rid, "kind": "edge", "title": title, "text": text, "hash": gd.short_hash(gd.tidy(text))})
    found = [(t, gd.tidy(secs[t])) for t in PARAM_TITLES if t in secs and secs[t].strip()]
    if found:
        rid = PARAMS_ID if PARAMS_ID not in seen else PARAMS_ID + "-node"
        out.append({"id": rid, "kind": "params", "title": "Số và việc của node (" + ", ".join(t for t, _ in found) + ")",
                    "text": "\n\n".join(f"## {t}\n{x}" for t, x in found),
                    "hash": gd.short_hash(*[p for t, x in found for p in (t, x)])})
    return out


def is_node(folder):
    return folder.is_dir() and not folder.name.startswith(".") and (folder / gd.DATA_FILE).is_file()


def leaves(table_root, key):
    """[(path, folder)] of the leaf nodes under the feature node `key` of a table (the feature node itself when it
    has no children)."""
    top = table_root / key
    if not is_node(top):
        return []
    out = []

    def visit(folder, path):
        kids = sorted(c for c in folder.iterdir() if is_node(c))
        if not kids:
            out.append((path, folder))
        for c in kids:
            visit(c, f"{path}/{c.name}")
    visit(top, key)
    return out


def feature_keys(root, feature):
    """The live feature nodes of a code folder (name compared without case), and the code folders each names."""
    keys = [key for folder, key, _ in gd.live_feature_names(root) if folder.lower() == feature.lower()]
    if not keys:
        names = ", ".join(f for f, _, _ in gd.live_feature_names(root))
        raise ValueError(f"live không có feature nào cho thư mục '{feature}'. Có: {names}")
    return keys


def code_folders(root, key, feature):
    live = gd.table_dir(root, "live")
    names = gd.read_code(live / key, key, gd.CODE_ROUND, []) or [feature]
    return [Path(root, "UnityProject", "Assets", "Features", *n.split("/")) for n in names]


# ---------------------------------------------------------------- tests
CLASS_RX = re.compile(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)")
METHOD_RX = re.compile(r"((?:\[[^\]\n]*\]\s*)+)(?:(?:public|private|internal|protected|static|async|override|virtual)\s+)*"
                       r"[A-Za-z_][\w<>\[\],.]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
TEST_ATTR_RX = re.compile(r"\b(Test|UnityTest|TestCase|TestCaseSource)\b")


def strip_comments(text):
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    return re.sub(r"//[^\n]*", "", text)


def test_names(folders):
    """{"Class.Method"} of every NUnit test under <folder>/Tests of the given code folders."""
    out = set()
    for folder in folders:
        tests = folder / "Tests"
        for f in sorted(tests.rglob("*.cs")) if tests.is_dir() else []:
            try:
                text = strip_comments(f.read_text(encoding="utf-8-sig", errors="replace"))
            except OSError:
                continue
            classes = [(m.start(), m.group(1)) for m in CLASS_RX.finditer(text)]
            for m in METHOD_RX.finditer(text):
                if not TEST_ATTR_RX.search(m.group(1)):
                    continue
                owner = [name for at, name in classes if at < m.start()]
                if owner:
                    out.add(f"{owner[-1]}.{m.group(2)}")
    return out


# ---------------------------------------------------------------- the saved book
def book_path(root, path):
    return gd.table_dir(root, "idea").joinpath(*path.split("/"), gd.PROGRESS_DIR, BOOK_FILE)


def read_book(root, path):
    try:
        data = gd.read_json(book_path(root, path))
    except ValueError:
        data = None
    items = data.get("items") if isinstance(data, dict) else None
    log = data.get("log") if isinstance(data, dict) else None
    return {"items": items if isinstance(items, dict) else {}, "log": log if isinstance(log, list) else []}


# ---------------------------------------------------------------- reading: the book as it stands now
def check_one(req, saved, tests):
    """The state a read gives one requirement, and why."""
    if not isinstance(saved, dict) or saved.get("state") not in SAVED or saved.get("state") == "todo":
        return "todo", ""
    state = saved["state"]
    if saved.get("hash") != req["hash"]:
        return "changed", "thiết kế đổi sau khi ghi '" + STATE_NAMES[state] + "'"
    if state in ("done", "verified"):
        gone = [t for t in saved.get("tests") or [] if t not in tests]
        if gone:
            return "no-test", "không còn test: " + ", ".join(gone)
    return state, ""


def node_view(root, path, folder, tests):
    """One leaf of live: its requirements, each with the state a read gives."""
    data = gd.parse_data(gd.read_text(folder / gd.DATA_FILE))
    doc = gd.read_text(folder / gd.DOC_FILE) if (folder / gd.DOC_FILE).is_file() else ""
    book = read_book(root, path)
    reqs = []
    for r in node_requirements(data, doc):
        saved = book["items"].get(r["id"])
        state, why = check_one(r, saved, tests)
        row = dict(r, ref=f"{path}#{r['id']}", state=state, why=why,
                   saved=saved if isinstance(saved, dict) else None)
        reqs.append(row)
    current = {r["id"] for r in reqs}
    dropped = [{"ref": f"{path}#{rid}", "id": rid, "saved": s} for rid, s in sorted(book["items"].items())
               if rid not in current and isinstance(s, dict) and s.get("state") not in (None, "todo")]
    progress = gd.read_progress(gd.table_dir(root, "idea").joinpath(*path.split("/")))
    return {"path": path, "title": data["title"], "cheat": folder.name == gd.CHEAT_KEY, "reqs": reqs,
            "dropped": dropped, "progress": progress, "log": book["log"], "hasBook": book_path(root, path).is_file()}


def view(root, feature):
    """The whole book of a code folder: {feature, keys, nodes, dropped, counts, allowed, reason}."""
    root = Path(root)
    keys = feature_keys(root, feature)
    live, idea = gd.table_dir(root, "live"), gd.table_dir(root, "idea")
    nodes, dropped = [], []
    for key in keys:
        tests = test_names(code_folders(root, key, feature))
        live_leaves = leaves(live, key)
        for path, folder in live_leaves:
            nodes.append(node_view(root, path, folder, tests))
        known = {p for p, _ in live_leaves}
        # books of nodes that are no longer leaves of live (deleted, moved, or split into children)
        top = idea / key
        for f in sorted(top.rglob(f"{gd.PROGRESS_DIR}/{BOOK_FILE}")) if top.is_dir() else []:
            path = "/".join(f.parent.parent.relative_to(idea).parts)
            if path in known:
                continue
            for rid, s in sorted(read_book(root, path)["items"].items()):
                if isinstance(s, dict) and s.get("state") not in (None, "todo"):
                    dropped.append({"ref": f"{path}#{rid}", "id": rid, "saved": s})
    for n in nodes:
        dropped += n["dropped"]
    hint_dropped(nodes, dropped)
    counts = {k: 0 for k in ORDER}
    for n in nodes:
        for r in n["reqs"]:
            counts[r["state"]] += 1
        n["met"] = sum(1 for r in n["reqs"] if r["state"] in MET)
        n["open"] = [r["ref"] for r in n["reqs"] if r["state"] not in MET]
    ok, reason = gd.code_allowed(root, feature)
    return {"feature": feature, "keys": keys, "nodes": nodes, "dropped": dropped, "counts": counts,
            "total": sum(len(n["reqs"]) for n in nodes), "met": sum(counts[k] for k in MET),
            "allowed": ok, "reason": reason}


def hint_dropped(nodes, dropped):
    """A new requirement (todo) that looks like a dropped one (same id, or same text) gets a hint naming it and its tests."""
    for n in nodes:
        for r in n["reqs"]:
            if r["state"] != "todo":
                continue
            for d in dropped:
                s = d["saved"]
                if d["id"] == r["id"] or (s.get("text") and gd.tidy(s["text"]) == gd.tidy(r["text"])):
                    r["hint"] = (f"giống {d['ref']} (đã bỏ, từng '{STATE_NAMES.get(s.get('state'), s.get('state'))}'"
                                 + (f", test: {', '.join(s['tests'])}" if s.get("tests") else "") + ")")
                    break


def leaf_status(root, path):
    """(requirements of the live leaf at `path`, the refs not met yet); ([], []) when live has no such leaf.
    Used by gamedesign.save_progress: a phase is marked done only when nothing is left open."""
    root = Path(root)
    parts = path.split("/") if path else []
    if not parts:
        return [], []
    live = gd.table_dir(root, "live")
    folder = live.joinpath(*parts)
    if not is_node(folder) or any(is_node(c) for c in folder.iterdir()):
        return [], []
    key = parts[0]
    names = gd.read_code(live / key, key, gd.CODE_ROUND, []) or [gd.suggested_code_name(key) or key]
    tests = test_names([Path(root, "UnityProject", "Assets", "Features", *n.split("/")) for n in names])
    n = node_view(root, path, folder, tests)
    return n["reqs"], [r["ref"] for r in n["reqs"] if r["state"] not in MET]


# ---------------------------------------------------------------- writing
def find_req(v, ref):
    """The requirement `ref` names: '<node path>#<id>', or '<last key of the node>#<id>' when that is unique."""
    m = REF_RX.match(ref or "")
    if not m:
        raise ValueError(f"yêu cầu phải có dạng <node>#<id>, ví dụ wallet-rules#start-50: '{ref}'")
    node, rid = m.group(1), m.group(2)
    hits = [(n, r) for n in v["nodes"] for r in n["reqs"]
            if r["id"] == rid and (n["path"] == node or n["path"].endswith("/" + node))]
    if not hits:
        raise ValueError(f"sổ của {v['feature']} không có yêu cầu '{ref}': chạy python Management/tools/gd_req.py {v['feature']}")
    if len(hits) > 1:
        raise ValueError(f"'{ref}' trùng nhiều node: " + ", ".join(r["ref"] for _, r in hits) + "; ghi đủ đường dẫn node")
    return hits[0]


def write(root, node, req, entry, by, note):
    target = book_path(root, node["path"])
    if not target.parent.parent.is_dir():
        raise ValueError(f"bảng đề xuất (ai/idea) không có node '{node['path']}' để ghi sổ")
    book = read_book(root, node["path"])
    entry = dict(entry, hash=req["hash"], text=req["text"], title=req["title"], by=str(by), at=gd.now())
    book["items"][req["id"]] = entry
    book["log"] = (book["log"] + [{"at": entry["at"], "id": req["id"], "state": entry["state"], "by": str(by),
                                   "note": note or ""}])[-LOG_MAX:]
    gd.write_json(target, book)


def check_note(note, need):
    if note is None or not isinstance(note, str) or not note.strip():
        if need:
            raise ValueError("cần ghi chú (trong ngoặc kép)")
        return ""
    if len(note) > gd.MAX_TEXT:
        raise ValueError(f"ghi chú tối đa {gd.MAX_TEXT} kí tự")
    return note.strip()


def mark(root, feature, refs, action, by, note=None, tests=None, play=None):
    """One write to the book. action: doing | done | verify | reject | waive | todo. Returns the lines to print.
    doing, done: the feature's own session. verify, reject: @CodeLead. waive, todo: the human (page)."""
    root = Path(root)
    v = view(root, feature)
    if action in ("doing", "done", "verify", "reject") and not v["allowed"]:
        raise ValueError(f"{feature} đang khoá code: {v['reason']}")
    out = []
    for ref in [r for r in (refs or "").split(",") if r]:
        node, req = find_req(v, ref.strip())
        state = req["state"]
        saved = req["saved"] or {}
        if action == "doing":
            write(root, node, req, {"state": "doing", "note": check_note(note, True), "tests": saved.get("tests") or [],
                                    "play": saved.get("play") or ""}, by, note)
        elif action == "done":
            names = [t.strip() for t in (tests or []) if t.strip()]
            play_text = check_note(play, False) if play is not None else ""
            if not names and not play_text:
                raise ValueError("done cần tên test (Lớp.Hàm, cách nhau bằng dấu phẩy) hoặc play \"<cách thấy khi Play>\"")
            bad = [t for t in names if not TEST_RX.match(t)]
            if bad:
                raise ValueError("tên test phải có dạng Lớp.Hàm: " + ", ".join(bad))
            have = set().union(*(test_names(code_folders(root, k, feature)) for k in v["keys"]))
            gone = [t for t in names if t not in have]
            if gone:
                raise ValueError(f"không thấy test [Test] nào tên {', '.join(gone)} trong UnityProject/Assets/Features/"
                                 f"{feature}/Tests; viết test trước rồi mới ghi done")
            write(root, node, req, {"state": "done", "note": saved.get("note") or "", "tests": names, "play": play_text},
                  by, play_text or ", ".join(names))
        elif action == "verify":
            if state != "done":
                raise ValueError(f"{req['ref']} đang '{STATE_NAMES[state]}': chỉ xác nhận được yêu cầu 'chờ kiểm' "
                                 "(feature đã ghi done, thiết kế chưa đổi, test còn đủ)")
            write(root, node, req, dict(saved, state="verified"), by, note)
        elif action == "reject":
            if state not in ("done", "verified", "no-test"):
                raise ValueError(f"{req['ref']} đang '{STATE_NAMES[state]}': chỉ trả được yêu cầu đã ghi done")
            write(root, node, req, dict(saved, state="rejected", note=check_note(note, True)), by, note)
        elif action == "waive":
            write(root, node, req, dict(saved, state="waived", note=check_note(note, True)), by, note)
        elif action == "todo":
            write(root, node, req, dict(saved, state="todo"), by, note)
        else:
            raise ValueError(f"không có thao tác '{action}'")
        out.append(f"Đã ghi {req['ref']}: {STATE_NAMES['verified' if action == 'verify' else 'rejected' if action == 'reject' else 'waived' if action == 'waive' else action]}")
    if not out:
        raise ValueError("chưa nêu yêu cầu nào")
    return out


def mark_by_ref(root, ref, action, by, note=None):
    """A write from the page (the human): the feature is the code folder of the node's feature (its first key)."""
    m = REF_RX.match(ref or "")
    if not m:
        raise ValueError(f"yêu cầu phải có dạng <node>#<id>: '{ref}'")
    key = m.group(1).split("/")[0]
    folders = [f for f, k, _ in gd.live_feature_names(root) if k == key]
    if not folders:
        raise ValueError(f"live không có feature '{key}'")
    return mark(root, folders[0], ref, action, by, note=note)


# ---------------------------------------------------------------- printing
def counts_line(v):
    c = v["counts"]
    parts = [f"{v['met']}/{v['total']} đã kiểm"]
    for k in ("done", "doing", "rejected", "changed", "no-test", "todo"):
        if c[k]:
            parts.append(f"{STATE_NAMES[k]} {c[k]}")
    if v["dropped"]:
        parts.append(f"đã bỏ {len(v['dropped'])}")
    return " · ".join(parts)


def progress_note(node):
    """'IDEA done → cần làm lại (3 yêu cầu)' for a leaf whose stored progress says done while requirements are open."""
    names = {x["key"]: x["name"] for x in gd.PHASES}
    cells = []
    for k, c in node["progress"].items():
        if c["state"] == "done" and node["open"] and not node["hasBook"]:
            cells.append(f"{names[k]} done (chưa đối chiếu sổ yêu cầu: {len(node['open'])} yêu cầu chưa gắn test)")
        elif c["state"] == "done" and node["open"]:
            cells.append(f"{names[k]} done → cần làm lại ({len(node['open'])} yêu cầu)")
        else:
            cells.append(f"{names[k]} {c['state']}")
    return " · ".join(cells)


def req_line(r):
    s = r["saved"] or {}
    line = f"- [{STATE_NAMES[r['state']]}] {r['ref']}: {r['title']}"
    extra = []
    if r["why"]:
        extra.append(r["why"])
    if s.get("note") and r["state"] in ("doing", "rejected", "changed", "waived"):
        extra.append("ghi chú: " + s["note"])
    if s.get("tests") and r["state"] not in ("todo",):
        extra.append("test: " + ", ".join(s["tests"]))
    if s.get("play") and r["state"] not in ("todo",):
        extra.append("Play: " + s["play"])
    if r.get("hint"):
        extra.append(r["hint"])
    out = [line] + [f"    {x}" for x in extra]
    if r["state"] == "changed" and s.get("text"):
        out.append("    bản lúc ghi: " + s["text"].replace("\n", " / ")[:400])
        out.append("    bản bây giờ: " + r["text"].replace("\n", " / ")[:400])
    return out


def render(v):
    out = [f"# Sổ yêu cầu của {v['feature']} (theo live: {', '.join(v['keys'])})",
           "Code: " + ("ĐƯỢC CODE" if v["allowed"] else "ĐANG KHOÁ, " + v["reason"]),
           "Tổng: " + counts_line(v),
           "Ghi: gd_req.py <F> doing <node>#<id> \"<đang làm gì>\" · gd_req.py <F> done <node>#<id> <Lớp.Hàm>[,...] "
           "(hoặc play \"<cách thấy khi Play>\") · @CodeLead: verify | reject",
           ""]
    open_rows = [r for n in v["nodes"] for r in n["reqs"] if r["state"] in ("doing", "rejected", "changed", "no-test")]
    if open_rows:
        out.append("## Cần làm trước (đang làm, bị trả, đổi thiết kế, mất test)")
        for r in sorted(open_rows, key=lambda r: ORDER.index(r["state"])):
            out += req_line(r)
        out.append("")
    for n in v["nodes"]:
        out.append(f"## {n['path']}" + ("  ·  CHEAT" if n["cheat"] else "") + f"  ·  {n['title']}  ·  {n['met']}/{len(n['reqs'])}")
        out.append("Tiến độ node: " + progress_note(n))
        for r in sorted(n["reqs"], key=lambda r: ORDER.index(r["state"])):
            out += req_line(r)
        out.append("")
    if v["dropped"]:
        out.append("## Đã bỏ (không còn trong live; xoá code hoặc test chỉ phục vụ nó, hoặc gắn lại test cho yêu cầu mới)")
        for d in v["dropped"]:
            s = d["saved"]
            out.append(f"- {d['ref']}: {s.get('title', '')} (từng '{STATE_NAMES.get(s.get('state'), s.get('state'))}'"
                       + (f", test: {', '.join(s['tests'])}" if s.get("tests") else "") + ")")
    return "\n".join(out).rstrip() + "\n"


def brief(root, feature):
    """One line for the start of every message of a feature chat (bind_session.py)."""
    v = view(root, feature)
    doing = [r for n in v["nodes"] for r in n["reqs"] if r["state"] == "doing"]
    line = f"Sổ yêu cầu {feature}: {counts_line(v)}."
    if doing:
        line += " Đang làm: " + "; ".join(f"{r['ref']} ({(r['saved'] or {}).get('note', '')[:80]})" for r in doing[:3])
        line += " …" if len(doing) > 3 else "."
    line += (f" Xem đủ: python Management/tools/gd_req.py {feature}. Bắt đầu một yêu cầu thì ghi doing kèm ghi chú; "
             "viết test xong thì ghi done kèm tên test; @CodeLead kiểm rồi mới tính xong.")
    return line


def status_all(root):
    """One line per code folder of live: met/total and what waits."""
    out = ["# Sổ yêu cầu theo feature (đã kiểm/tổng; chờ kiểm là việc của @CodeLead, bị trả và đổi thiết kế là việc của feature)"]
    for folder, key, declared in gd.live_feature_names(root):
        try:
            v = view(root, folder)
        except (ValueError, OSError) as ex:
            out.append(f"- {folder}: không đọc được ({ex})")
            continue
        if not v["total"]:
            continue
        gate = "" if v["allowed"] else " (ĐANG KHOÁ)"
        out.append(f"- {folder}{gate}: {counts_line(v)}")
    return "\n".join(out) + "\n"
