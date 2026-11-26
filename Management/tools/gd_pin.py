#!/usr/bin/env python3
"""Pins a design node again to what it was checked against: based-on (the data hash of the node above) and refs
(the data hash of the nodes of other branches it builds on), after showing what changed there. @GameDesigner.

    python Management/tools/gd_pin.py ai/idea/<node> [--chain]              shows the diff, writes nothing
    python Management/tools/gd_pin.py ai/idea/<node> [--chain] --write <code>

Without --write: for the node (--chain: the node and every node below it, top down) whose based-on or refs no longer
match, what changed in the node above or the node it refers to since the pinned version: ideas added, removed,
rewritten (old text -> new), title or summary, the frames. The old version is the copy in that node's .history
(data-<time>-<hash>.md). The last line is the command that pins, with a code made of the hashes it will write.
--write <code>: writes those hashes into based-on and refs, only when the code is still the same (nothing changed
since the diff was read). based-on and refs are not part of the data hash: pinning never sends the nodes below to
"cần làm lại". Pin only after reading the diff and changing the node where it no longer holds.
"""
import hashlib
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
LIVE_PREFIX = "/".join(gd.TABLES["live"])
USAGE = f"Cách dùng: python Management/tools/gd_pin.py {PREFIX}/<node> [--chain] [--write <mã>]"


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


def ref_entries(node):
    """The refs line entries once pinned: every declared ref at its current hash (one that names nothing stays as
    written, gd_status shows it), and the missing ones added."""
    out = [f"{r['label']}@{r['current'] if r['ok'] is not None else r['hash']}" for r in node["refs"]]
    return out + [f"{r['label']}@{r['current']}" for r in node["refMissing"]]


def work(root, path, chain):
    """[(node, lines)] of the nodes to pin, top down."""
    snap = gd.snapshot(root)
    top = next((n for n in gd.walk(snap["idea"]) if n["path"] == path), None)
    if top is None:
        raise ValueError(f"không có node {PREFIX}/{path}")
    out = []
    for n in (gd.walk(top) if chain else [top]):
        lines = []
        if n["path"] and n["stale"]:
            parent = n["path"].rpartition("/")[0]
            lines.append(f"- based-on {n['basedOn'] or '(chưa có)'} -> {n['parentHash']}: node cha "
                         f"{parent or '(gốc)'} đã đổi:")
            lines += target_diff(root, parent, "", n["basedOn"]) if n["basedOn"] else ["  - chưa từng ghim: Read node cha"]
        for r in n["refs"]:
            if r["ok"] is False:
                lines.append(f"- ref {r['label']} {r['hash']} -> {r['current']}:")
                lines += target_diff(root, r["path"], r["term"], r["hash"])
        for r in n["refMissing"]:
            lines.append(f"- thêm ref {r['label']}@{r['current']} (node nhắc tới mà chưa khai)")
        if lines:
            out.append((n, lines))
    return out


def code_of(todo):
    h = hashlib.sha1()
    for n, _ in todo:
        h.update(f"{n['path']}|{n['parentHash'] if n['path'] else ''}|{','.join(ref_entries(n))}\n".encode("utf-8"))
    return h.hexdigest()[:8]


def pin(root, node):
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
    refs = ref_entries(node)
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
    new = ("---\n" + "\n".join(head) + "\n---\n" + body) if head else body
    file.write_bytes(new.replace("\n", nl).encode("utf-8"))


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":      # test-only override
        root, args = Path(args[-1]), args[:-2]
    chain = "--chain" in args
    args = [a for a in args if a != "--chain"]
    code = None
    if "--write" in args:
        i = args.index("--write")
        if i + 1 >= len(args):
            print(USAGE, file=sys.stderr)
            return 2
        code = args[i + 1]
        args = args[:i] + args[i + 2:]
    if len(args) != 1:
        print(USAGE, file=sys.stderr)
        return 2
    try:
        path = node_path(args[0])
        todo = work(root, path, chain)
        name = f"{PREFIX}/{path}" if path else PREFIX
        if not todo:
            print(f"{name}: based-on và refs đều khớp" + (" (cả nhánh)" if chain else "") + ", không có gì để ghim.")
            return 0
        now = code_of(todo)
        if code is None:
            for n, lines in todo:
                print(f"## {n['path'] or '(gốc)'}")
                print("\n".join(lines))
            print(f"\nRà xong (sửa node nếu thay đổi ở trên làm nó không còn đúng), rồi ghim: python Management/tools/gd_pin.py "
                  f"{name}{' --chain' if chain else ''} --write {now}")
            return 0
        if code != now:
            print(f"Mã {code} không còn khớp ({now}): thiết kế đã đổi từ lần rà. Chạy lại không --write để xem diff mới.",
                  file=sys.stderr)
            return 1
        for n, _ in todo:
            pin(root, n)
        print("Đã ghim: " + ", ".join(n["path"] or "(gốc)" for n, _ in todo))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Lỗi: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
