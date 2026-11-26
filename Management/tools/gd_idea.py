#!/usr/bin/env python3
"""Removes or rewrites ideas of design nodes by <node>#<id>, for @GameDesigner, instead of editing data.md by hand.

    python Management/tools/gd_idea.py rm ai/idea/<node>#<id> [ai/idea/<node>#<id> ...]
    python Management/tools/gd_idea.py replace ai/idea/<node>#<id> [...] --old "<text>" --new "<text>"

rm: takes the idea's block out of data.md and its id out of by:; an idea of GPT's moves to gpt-dropped: (one entry
per point of GPT's advice it stood for). In mode user an idea of GPT's goes only after the human scored it below 5
(gd_status refuses the drop otherwise, see mark_sources).
replace: <text> must be found exactly once in the idea's title and detail (case as written); the id, the AI score
and by: stay. The new title stays under the length limits and data.md must still parse.
Both refuse the human's ideas (the user: line: only the human edits them, on the page) and a data.md with errors.
Every target is checked before anything is written: one refusal writes nothing. Each change is logged in the node's
.history/log.jsonl (ai-remove, ai-edit).
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

PREFIX = "/".join(gd.TABLES["idea"])
USAGE = (f"Cách dùng: python Management/tools/gd_idea.py rm {PREFIX}/<node>#<id> [...]\n"
         f"           python Management/tools/gd_idea.py replace {PREFIX}/<node>#<id> [...] --old \"<chuỗi>\" --new \"<chuỗi>\"")


def target(arg):
    """(node path, idea id) of '<ai/idea/...>#<id>'; ValueError otherwise."""
    where, sep, item = arg.replace("\\", "/").strip().partition("#")
    where = where.rstrip("/")
    if not sep or not item:
        raise ValueError(f"'{arg}': cần dạng {PREFIX}/<node>#<id>")
    if where == PREFIX:
        path = ""
    elif where.startswith(PREFIX + "/"):
        path = where[len(PREFIX) + 1:]
    else:
        raise ValueError(f"'{arg}': chỉ sửa được ý trong bảng đề xuất {PREFIX}")
    gd.check_path(path)
    return path, item


def split(text):
    """(head lines, body) of a data.md (newlines already \\n)."""
    if text.startswith("---\n") and "\n---" in text[4:]:
        head, _, body = text[4:].partition("\n---")
        return head.split("\n"), body
    return [], "\n" + text


def join(head, body):
    head = [line for line in head if line.strip()]
    return ("---\n" + "\n".join(head) + "\n---" + body) if head else body.lstrip("\n")


def block(lines, item_id):
    """(start, end) of the idea's block among the body lines."""
    start = next(i for i, line in enumerate(lines) if (m := gd.ITEM_RX.match(line)) and m.group(1) == item_id)
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith("#")), len(lines))
    return start, end


def rewrite(text, item_id, old, new):
    """data.md with `old` replaced by `new` in the idea's title or detail; ValueError unless it is there once."""
    head, body = split(text)
    lines = body.split("\n")
    start, end = block(lines, item_id)
    m = gd.ITEM_RX.match(lines[start])
    title = m.group(3) or ""
    prefix = lines[start][:m.start(3)] if m.group(3) else lines[start].rstrip() + " | "
    detail = "\n".join(lines[start + 1:end])
    found = title.count(old) + detail.count(old)
    if found != 1:
        raise ValueError(f"chuỗi --old khớp {found} lần trong ý '{item_id}' (phải đúng một lần)")
    if old in title:
        title = title.replace(old, new)
        if "\n" in title or "|" in title:
            raise ValueError(f"tiêu đề mới của ý '{item_id}' không được có xuống dòng hay dấu |")
    else:
        detail = detail.replace(old, new)
        if any(line.startswith("#") or line.strip() == "---" for line in detail.split("\n")):
            raise ValueError(f"chi tiết mới của ý '{item_id}' không được có dòng bắt đầu bằng # hay dòng ---")
    lines[start:end] = [prefix + title] + detail.split("\n")
    return join(head, "\n".join(lines))


def plan(root, action, targets, old="", new=""):
    """[(path, folder, new text, [(item, new item or None)])], every target checked; ValueError on the first refusal."""
    idea_root = gd.table_dir(root, "idea")
    mode = gd.read_mode(root)
    by_node = {}
    for arg in targets:
        path, item = target(arg)
        if item not in by_node.setdefault(path, []):
            by_node[path].append(item)
    out = []
    for path, items in by_node.items():
        name = path or "(gốc)"
        folder = idea_root.joinpath(*(path.split("/") if path else []))
        if not (folder / gd.DATA_FILE).is_file():
            raise ValueError(f"bảng đề xuất không có node '{name}'")
        text = gd.read_text(folder / gd.DATA_FILE).replace("\r\n", "\n")
        data = gd.parse_data(text)
        if data["errors"]:
            raise ValueError(f"{gd.DATA_FILE} của '{name}' đang có lỗi, sửa trước: " + "; ".join(data["errors"][:2]))
        known = {it["id"]: it for it in data["items"]}
        turned_down = {e.get("item") for e in gd.rejected(folder) + gd.human_deleted(folder)}
        done = []
        for item in items:
            it = known.get(item)
            if it is None:
                raise ValueError(f"node '{name}' không có ý '{item}'")
            if item in data.get("user", []):
                raise ValueError(f"'{name}#{item}' là ý của người: chỉ người sửa hay xoá nó, trên trang")
            if action == "rm":
                if it["sources"] and mode == "user" and item not in turned_down:
                    raise ValueError(f"'{name}#{item}' là ý của GPT, mode user: chỉ bỏ được sau khi người chấm nó dưới "
                                     f"{gd.PASS_MIN} (lịch sử chưa có). Để nó lại và chờ người chấm")
                text = gd._drop_idea_text(text, item, it["sources"], data["user"])
            else:
                text = rewrite(text, item, old, new)
            done.append(it)
        check = gd.parse_data(text)
        if check["errors"]:
            raise ValueError(f"không ghi được '{name}': " + "; ".join(check["errors"][:2]))
        now = {it["id"]: it for it in check["items"]}
        for it in done:
            got = now.get(it["id"])
            if got and (len(got["title"]) > gd.IDEA_TITLE_MAX or len(got["detail"]) > gd.IDEA_DETAIL_MAX):
                raise ValueError(f"ý '{name}#{it['id']}' mới dài quá: tiêu đề tối đa {gd.IDEA_TITLE_MAX} kí tự, chi tiết "
                                 f"tối đa {gd.IDEA_DETAIL_MAX}. Dài hơn thì là hai ý")
        out.append((path, folder, text, [(it, now.get(it["id"])) for it in done]))
    return out


def apply(root, action, targets, old="", new=""):
    """Checks every target, then writes. Returns the lines to print."""
    writes = plan(root, action, targets, old, new)
    lines = []
    for path, folder, text, done in writes:
        file = folder / gd.DATA_FILE
        nl = "\r\n" if b"\r\n" in file.read_bytes() else "\n"
        file.write_bytes(text.replace("\n", nl).encode("utf-8"))
        for it, got in done:
            if action == "rm":
                gd.log(folder, "ai-remove", item=it["id"], text=it["text"], by=it["by"])
                lines.append(f"Đã bỏ {path or '(gốc)'}#{it['id']}" + (" (ý của GPT: chuyển sang gpt-dropped)" if it["sources"] else ""))
            else:
                gd.log(folder, "ai-edit", item=it["id"], old=it["text"], text=got["text"] if got else "")
                lines.append(f"Đã sửa {path or '(gốc)'}#{it['id']}")
    return lines


def main(argv):
    root = HERE.parent.parent
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":      # test-only override
        root, args = Path(args[-1]), args[:-2]
    if not args or args[0] not in ("rm", "replace"):
        print(USAGE, file=sys.stderr)
        return 2
    action, args = args[0], args[1:]
    old = new = ""
    if action == "replace":
        for flag in ("--old", "--new"):
            if flag not in args or args.index(flag) + 1 >= len(args):
                print(USAGE, file=sys.stderr)
                return 2
            i = args.index(flag)
            if flag == "--old":
                old = args[i + 1]
            else:
                new = args[i + 1]
            args = args[:i] + args[i + 2:]
        if not old:
            print("--old không được rỗng", file=sys.stderr)
            return 2
    if not args or any(a.startswith("--") for a in args):
        print(USAGE, file=sys.stderr)
        return 2
    try:
        print("\n".join(apply(root, action, args, old, new)))
        return 0
    except (ValueError, OSError) as ex:
        print(f"Không sửa gì: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
