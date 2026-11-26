#!/usr/bin/env python3
"""Pins design nodes again to what they were checked against: based-on (the data hash of the node above) and refs
(the data hash of the nodes of other branches they build on), after showing what changed there. @GameDesigner.

    python Management/tools/gd_pin.py ai/idea/<node> [--chain | --all] [--hide <id,...>] [--drop-dead] [--write <code>]
    python Management/tools/gd_pin.py --all [--hide <id,...>] [--drop-dead] [--write <code>]

Which nodes: the node alone; --chain: the node and every node below it; ai/idea/<node> --all: every node the node
pulls along (the node and the nodes below it, every node that refers to one of those and the nodes below that, again
until nothing is added); --all with no node: the whole tree.
Without --write: what changed, one block per source change (the node above or the node referred to, old hash -> new),
printed once with the nodes it touches, whatever their number: ideas added, removed, rewritten (old text -> new),
title or summary, the frames. The old version is the copy in that node's .history (data-<time>-<hash>.md). Each block
has a short id that stays the same from one run to the next; --hide <id,...> prints the blocks already read as one
line. A ref that names no node or term any more (a deleted node) is listed as dead; --drop-dead takes it off the refs
line. The last line is the command that pins, with a code made of what it will write (--hide does not change it).
--write <code>: writes those hashes into based-on and refs, only when the code is still the same (nothing changed
since the diff was read). based-on and refs are not part of the data hash: pinning never sends the nodes below to
"cần làm lại". Pin only after reading the diff and changing the node where it no longer holds.
"""
import hashlib
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
LIVE_PREFIX = "/".join(gd.TABLES["live"])
USAGE = (f"Cách dùng: python Management/tools/gd_pin.py [{PREFIX}/<node>] [--chain | --all] [--hide <id,...>] "
         "[--drop-dead] [--write <mã>] (không có node thì phải có --all)")


def node_path(arg):
    """The node path inside ai/idea ('' for the root); ValueError for anything else."""
    arg = arg.replace("\\", "/").strip().rstrip("/")
    if arg == LIVE_PREFIX or arg.startswith(LIVE_PREFIX + "/"):
        raise ValueError("live là bảng đã chốt: chỉ người chốt, không ghim ở đó")
    if arg == PREFIX:
        return ""
    if not arg.startswith(PREFIX + "/"):
        raise ValueError(f"đường dẫn phải có dạng {PREFIX}/<node>")
    return arg[len(PREFIX) + 1:]


def folder_of(root, path):
    return gd.table_dir(root, "idea").joinpath(*(path.split("/") if path else []))


def old_version(folder, digest):
    """The parsed data.md of `folder` at data hash `digest`, from its .history; None when no copy is left."""
    copies = sorted((folder / gd.HISTORY_DIR).glob(f"data-*-{digest}.md"))
    if not copies:
        return None
    return gd.parse_data(copies[-1].read_text(encoding="utf-8"))


def short(text, n=140):
    text = " ".join((text or "").split())
    return text if len(text) <= n else text[:n - 1] + "…"


def data_diff(old, new):
    """Lines saying what changed from the parsed data `old` to `new`."""
    out = []
    if old["title"] != new["title"]:
        out.append(f"  - tiêu đề: {short(old['title'])} -> {short(new['title'])}")
    if old["summary"] != new["summary"]:
        out.append(f"  - tóm tắt: {short(old['summary'])} -> {short(new['summary'])}")
    was = {it["id"]: it for it in old["items"]}
    now = {it["id"]: it for it in new["items"]}
    for i, it in now.items():
        if i not in was:
            out.append(f"  - thêm [{i}] {short(it['title'])}")
    for i, it in was.items():
        if i not in now:
            out.append(f"  - bỏ [{i}] {short(it['title'])}")
    for i, it in now.items():
        if i in was and was[i]["hash"] != it["hash"]:
            if was[i]["title"] != it["title"]:
                out.append(f"  - viết lại [{i}] {short(was[i]['title'])} -> {short(it['title'])}")
            else:
                out.append(f"  - viết lại chi tiết [{i}] {short(it['title'])}: {short(was[i].get('detail'))} -> "
                           f"{short(it.get('detail'))}")
    if not out:
        out.append("  - ý, tiêu đề, tóm tắt như cũ: frame (.frame-concept) đã đổi")
    return out


def target_diff(root, path, term, pinned):
    """Lines of what changed in the node `path` (or its glossary term `term`) since its version `pinned`."""
    folder = folder_of(root, path)
    old = old_version(folder, pinned) if not term else None
    if term:      # a term's hash is not a data hash: find the version of the glossary that had it
        for copy in sorted((folder / gd.HISTORY_DIR).glob("data-*.md"), reverse=True):
            hit = next((t for t in gd.glossary_terms(gd.parse_data(copy.read_text(encoding="utf-8")))[0]
                        if t["id"] == term and t["hash"] == pinned), None)
            if hit:
                old = hit
                break
        if old is None:
            return [f"  - không còn bản {pinned} của từ {term} trong .history: Read {PREFIX}/{path} để rà"]
        new = next((t for t in gd.glossary_terms(gd.read_data(folder))[0] if t["id"] == term), None)
        return [f"  - nghĩa: {short(old['meaning'])} -> {short(new['meaning'] if new else '(không còn)')}"]
    if old is None:
        return [f"  - không còn bản {pinned} trong .history: Read {PREFIX}/{path or ''} để rà"]
    return data_diff(old, gd.read_data(folder))


def ref_entries(node, drop_dead=False):
    """The refs line entries once pinned: every declared ref at its current hash, a dead one (it names nothing, or
    names the node's own lineage) kept as written unless drop_dead, and the missing ones added."""
    out = [f"{r['label']}@{r['current'] if r['ok'] is not None else r['hash']}" for r in node["refs"]
           if not (drop_dead and r["ok"] is None)]
    return out + [f"{r['label']}@{r['current']}" for r in node["refMissing"]]


def sources(node):
    """[(key, title)] of what the node has to be pinned again for. A key names one source change, the same for every
    node it touches: ("based", parent, old, new), ("ref", label, old, new), ("add", label, new) or ("dead", label)."""
    out = []
    if node["path"] and node["stale"]:
        parent = node["path"].rpartition("/")[0]
        out.append((("based", parent, node["basedOn"] or "", node["parentHash"]),
                    f"based-on {node['basedOn'] or '(chưa có)'} -> {node['parentHash']}: node cha {parent or '(gốc)'} đã đổi"))
    for r in node["refs"]:
        if r["ok"] is False:
            out.append((("ref", r["label"], r["hash"], r["current"]), f"ref {r['label']} {r['hash']} -> {r['current']}"))
        elif r["ok"] is None:
            out.append((("dead", r["label"]), f"ref chết {r['label']}: không còn node hay từ này, hoặc là dòng họ của "
                                               "chính node (based-on đã lo)"))
    for r in node["refMissing"]:
        out.append((("add", r["label"], r["current"]), f"thêm ref {r['label']}@{r['current']} (node nhắc tới mà chưa khai)"))
    return out


def source_id(key):
    return hashlib.sha1("|".join(key).encode("utf-8")).hexdigest()[:4]


def source_diff(root, key, ref):
    """Lines of what the source change `key` is about. ref: the ref of a node that has it (for its path, term)."""
    if key[0] == "based":
        return target_diff(root, key[1], "", key[2]) if key[2] else ["  - chưa từng ghim: Read node cha"]
    if key[0] == "ref":
        return target_diff(root, ref["path"], ref["term"], key[2])
    if key[0] == "dead":
        return ["  - gd_impact.py cho biết còn chỗ nào nhắc tới nó; --drop-dead gỡ nó khỏi dòng refs"]
    return []


def under(p, q):
    """Whether node path p is q or below it ('' is the root, above everything)."""
    return p == q or not q or p.startswith(q + "/")


def scope(snap, path, mode):
    """The nodes to look at, top down. mode: "" (the node), "chain" (its branch), "all" (what it pulls along;
    path None: the whole tree)."""
    every = list(gd.walk(snap["idea"]))
    if mode == "all" and path is None:
        return every
    top = next((n for n in every if n["path"] == path), None)
    if top is None:
        raise ValueError(f"không có node {PREFIX}/{path}")
    if mode == "":
        return [top]
    if mode == "chain":
        return list(gd.walk(top))
    tops = {path}
    while True:      # a node referring to anything already pulled along is pulled along, with its branch
        more = {n["path"] for n in every if not any(under(n["path"], t) for t in tops)
                and any(under(r["path"], t) for r in n["refs"] + n["refMissing"] for t in tops)}
        if not more:
            break
        tops |= more
    return [n for n in every if any(under(n["path"], t) for t in tops)]


def work(root, path, mode, drop_dead=False):
    """(todo, groups): todo [node] of the nodes to pin, top down; groups [{key, id, title, lines, nodes}] of the
    source changes, in the order first met. A node whose only source is a dead ref writes something new only with
    drop_dead, so it is in todo only then."""
    snap = gd.snapshot(root)
    todo, groups = [], {}
    for n in scope(snap, path, mode):
        found = sources(n)
        if not found:
            continue
        if drop_dead or any(k[0] != "dead" for k, _ in found):
            todo.append(n)
        refs = {r["label"]: r for r in n["refs"]}
        for key, title in found:
            g = groups.get(key)
            if g is None:
                g = groups[key] = {"key": key, "id": source_id(key), "title": title,
                                   "lines": source_diff(root, key, refs.get(key[1])), "nodes": []}
            g["nodes"].append(n["path"] or "(gốc)")
    return todo, list(groups.values())


def code_of(todo, drop_dead=False):
    h = hashlib.sha1(b"drop-dead\n" if drop_dead else b"")
    for n in todo:
        h.update(f"{n['path']}|{n['parentHash'] if n['path'] else ''}|{','.join(ref_entries(n, drop_dead))}\n".encode("utf-8"))
    return h.hexdigest()[:8]


def pin(root, node, drop_dead=False):
    """Writes the current based-on and refs into the node's data.md header; nothing else changes."""
    file = folder_of(root, node["path"]) / gd.DATA_FILE
    raw = file.read_bytes().decode("utf-8")
    nl = "\r\n" if "\r\n" in raw else "\n"
    text = raw.replace("\r\n", "\n")
    head, body = [], text
    if text.startswith("---\n") and "\n---" in text[4:]:
        end = text.index("\n---", 4)
        head, body = text[4:end].split("\n"), text[end + 4:].lstrip("-")
        body = body[1:] if body.startswith("\n") else body
    refs = ref_entries(node, drop_dead)
    if node["path"]:
        line = f"based-on: {node['parentHash']}"
        at = next((i for i, h in enumerate(head) if h.split(":")[0].strip() == "based-on"), None)
        if at is None:
            head.insert(0, line)
        else:
            head[at] = line
    at = next((i for i, h in enumerate(head) if h.split(":")[0].strip() == "refs"), None)
    if refs:
        line = "refs: " + ", ".join(refs)
        if at is None:
            after = next((i for i, h in enumerate(head) if h.split(":")[0].strip() == "based-on"), -1)
            head.insert(after + 1, line)
        else:
            head[at] = line
    elif at is not None:      # every ref was dead and dropped
        del head[at]
    new = ("---\n" + "\n".join(head) + "\n---\n" + body) if head else body
    file.write_bytes(new.replace("\n", nl).encode("utf-8"))


def take(args, flag):
    """(the value after flag or None, args without both); ValueError when the value is missing."""
    if flag not in args:
        return None, args
    i = args.index(flag)
    if i + 1 >= len(args):
        raise ValueError(USAGE)
    return args[i + 1], args[:i] + args[i + 2:]


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":      # test-only override
        root, args = Path(args[-1]), args[:-2]
    flags = {f: f in args for f in ("--chain", "--all", "--drop-dead")}
    args = [a for a in args if a not in flags]
    try:
        code, args = take(args, "--write")
        hide, args = take(args, "--hide")
    except ValueError:
        print(USAGE, file=sys.stderr)
        return 2
    hide = {x.strip().strip("[]") for x in (hide or "").split(",") if x.strip()}
    drop = flags["--drop-dead"]
    mode = "all" if flags["--all"] else ("chain" if flags["--chain"] else "")
    if (flags["--all"] and flags["--chain"]) or len(args) > 1 or (not args and mode != "all"):
        print(USAGE, file=sys.stderr)
        return 2
    try:
        path = node_path(args[0]) if args else None
        todo, groups = work(root, path, mode, drop)
        name = (f"{PREFIX}/{path}" if path else PREFIX) if path is not None else ""
        opts = " ".join(x for x in (name, {"chain": "--chain", "all": "--all"}.get(mode, ""),
                                    "--drop-dead" if drop else "") if x)
        dead = [g for g in groups if g["key"][0] == "dead"]
        dead_n = sum(len(g["nodes"]) for g in dead)
        if not groups:
            print(f"{name or 'Cả cây'}: based-on và refs đều khớp" + (" (cả phần được xét)" if mode else "") +
                  ", không có gì để ghim.")
            return 0
        now = code_of(todo, drop)
        if code is None:
            for g in groups:
                many = f" ({len(g['nodes'])} node)"
                if g["id"] in hide:
                    print(f"đã rà: [{g['id']}] {g['title']}{many}")
                    continue
                print(f"### [{g['id']}] {g['title']}{many}")
                if g["lines"]:
                    print("\n".join(g["lines"]))
                print("  node: " + ", ".join(g["nodes"]))
            print(f"\n{len(groups)} thay đổi nguồn, {len(todo)} node cần ghim.")
            if dead and not drop:
                print(f"Còn {dead_n} ref chết ({len(dead)} đích): thêm --drop-dead để gỡ chúng khỏi dòng refs "
                      "(mã đổi theo cờ).")
            if not todo:
                return 0
            print("Rà xong (sửa node nếu thay đổi ở trên làm nó không còn đúng; --hide <id,...> để ẩn phần đã rà), "
                  f"rồi ghim: python Management/tools/gd_pin.py {opts} --write {now}")
            return 0
        if code != now:
            print(f"Mã {code} không còn khớp ({now}): thiết kế hay cờ đã đổi từ lần rà. Chạy lại không --write để xem "
                  "diff mới.", file=sys.stderr)
            return 1
        if not todo:
            print("Không có gì để ghim: chỉ còn ref chết. Chạy lại với --drop-dead để gỡ chúng.")
            return 0
        for n in todo:
            pin(root, n, drop)
        print("Đã ghim: " + ", ".join(n["path"] or "(gốc)" for n in todo))
        if dead and not drop:
            print(f"Chưa gỡ {dead_n} ref chết: " + ", ".join(g["key"][1] for g in dead) +
                  ". Chạy lại với --drop-dead để gỡ.")
        return 0
    except (ValueError, OSError) as ex:
        print(f"Lỗi: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
