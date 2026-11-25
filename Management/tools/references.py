#!/usr/bin/env python3
"""The reference pictures the human uploads on the management page (tab Tham chiếu): screens of other games to lay
out like, art to aim the style at. Each picture has the human's note under it, a kind and tags. @UIDesigner reads
the layout ones before it lays a screen out (.claude/rules/uidesigner.md); @GameDesigner the art ones when it writes
the styles of round 1. Only the page (setup_server.py) writes here; every chat reads.

    Management/references/
      index.json                {"items": [{id, file, note, kind, tags, at}]}, newest first
      <id>.<png|jpg|webp>       the picture

    python Management/tools/references.py            the list, one line per picture (what a chat reads)
"""
import base64
import json
import re
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent

DIR = ("Management", "references")
INDEX = "index.json"
KINDS = ("bố cục", "style art")
MAX_BYTES = 8 * 1024 * 1024
NOTE_MAX, TAG_MAX, TAGS_MAX = 2000, 40, 8
# The first bytes of each kind of picture: the name of a file says nothing, its content does.
SIGNATURES = ((b"\x89PNG\r\n\x1a\n", ".png"), (b"\xff\xd8\xff", ".jpg"))
ID_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")


def folder(root):
    return Path(root).joinpath(*DIR)


def kind_of(raw):
    """The file extension of a picture from its bytes, or None when it is not a png, jpg or webp."""
    for magic, ext in SIGNATURES:
        if raw.startswith(magic):
            return ext
    if raw[:4] == b"RIFF" and raw[8:12] == b"WEBP":
        return ".webp"
    return None


def slug(name):
    """A short id part from a file name: lower case ascii, digits, '-'."""
    import unicodedata
    stem = Path(str(name or "")).stem.replace("đ", "d").replace("Đ", "D")      # đ has no accent to strip
    text = unicodedata.normalize("NFD", stem).encode("ascii", "ignore").decode().lower()
    text = re.sub(r"[^a-z0-9]+", "-", text).strip("-")
    return text[:40].strip("-") or "anh"


def read(root):
    """[{id, file, note, kind, tags, at}] of the index, newest first; a broken index reads as empty, and pictures
    without a line are listed with an empty note so nothing uploaded is lost from sight."""
    d = folder(root)
    try:
        items = json.loads((d / INDEX).read_text(encoding="utf-8")).get("items")
    except (OSError, ValueError, AttributeError):
        items = []
    out = [clean(x) for x in items if isinstance(x, dict)] if isinstance(items, list) else []
    out = [x for x in out if x and (d / x["file"]).is_file()]
    known = {x["file"] for x in out}
    for p in sorted(d.iterdir()) if d.is_dir() else []:
        if p.is_file() and p.suffix.lower() in (".png", ".jpg", ".webp") and p.name not in known and ID_RX.match(p.stem):
            out.append({"id": p.stem, "file": p.name, "note": "", "kind": KINDS[0], "tags": [], "at": ""})
    return sorted(out, key=lambda x: x["at"], reverse=True)


def clean(x):
    """One index line as the page may trust it, or None."""
    rid, file = str(x.get("id") or ""), str(x.get("file") or "")
    if not ID_RX.match(rid) or Path(file).stem != rid or Path(file).suffix not in (".png", ".jpg", ".webp"):
        return None
    tags = [str(t)[:TAG_MAX] for t in x.get("tags") or [] if str(t).strip()][:TAGS_MAX] if isinstance(x.get("tags"), list) else []
    return {"id": rid, "file": file, "note": str(x.get("note") or "")[:NOTE_MAX],
            "kind": x.get("kind") if x.get("kind") in KINDS else KINDS[0], "tags": tags, "at": str(x.get("at") or "")}


def write(root, items):
    d = folder(root)
    d.mkdir(parents=True, exist_ok=True)
    tmp = d / (INDEX + ".tmp")
    tmp.write_text(json.dumps({"items": items}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    tmp.replace(d / INDEX)


def check_fields(note, kind, tags):
    if not isinstance(note, str) or len(note) > NOTE_MAX:
        raise ValueError(f"nhận định phải là chữ, tối đa {NOTE_MAX} kí tự")
    if kind not in KINDS:
        raise ValueError(f"loại phải là {' hoặc '.join(KINDS)}")
    if not isinstance(tags, list) or len(tags) > TAGS_MAX or any(not isinstance(t, str) or len(t) > TAG_MAX for t in tags):
        raise ValueError(f"tag: tối đa {TAGS_MAX} tag, mỗi tag tối đa {TAG_MAX} kí tự")
    return note.strip(), kind, [t.strip() for t in tags if t.strip()]


def upload(root, name, data_b64, note="", kind=KINDS[0], tags=None):
    """Stores one picture sent by the page as base64; returns its line."""
    note, kind, tags = check_fields(note, kind, tags or [])
    try:
        raw = base64.b64decode(str(data_b64 or ""), validate=True)
    except ValueError:
        raise ValueError("dữ liệu ảnh hỏng (không phải base64)")
    if not raw:
        raise ValueError("ảnh rỗng")
    if len(raw) > MAX_BYTES:
        raise ValueError(f"ảnh quá {MAX_BYTES // (1024 * 1024)} MB")
    ext = kind_of(raw)
    if ext is None:
        raise ValueError("chỉ nhận ảnh png, jpg hoặc webp")
    items = read(root)
    rid = time.strftime("%Y%m%d-%H%M%S") + "-" + slug(name)
    taken = {x["id"] for x in items}
    base, n = rid, 2
    while rid in taken or (folder(root) / (rid + ext)).exists():
        rid, n = f"{base}-{n}", n + 1
    d = folder(root)
    d.mkdir(parents=True, exist_ok=True)
    (d / (rid + ext)).write_bytes(raw)
    line = {"id": rid, "file": rid + ext, "note": note, "kind": kind, "tags": tags, "at": time.strftime("%Y-%m-%dT%H:%M:%S")}
    write(root, [line] + items)
    return line


def update(root, rid, note, kind, tags):
    """The human's note, kind and tags of one picture."""
    note, kind, tags = check_fields(note, kind, tags)
    items = read(root)
    hit = next((x for x in items if x["id"] == rid), None)
    if hit is None:
        raise ValueError(f"không có ảnh '{rid}'")
    hit.update(note=note, kind=kind, tags=tags)
    write(root, items)
    return hit


def delete(root, rid):
    items = read(root)
    hit = next((x for x in items if x["id"] == rid), None)
    if hit is None:
        raise ValueError(f"không có ảnh '{rid}'")
    (folder(root) / hit["file"]).unlink(missing_ok=True)
    write(root, [x for x in items if x is not hit])
    return hit


def snapshot(root):
    """What the page shows: the pictures with their notes, the kinds, and the folder relative to Management/."""
    return {"items": read(root), "kinds": list(KINDS), "dir": "references/"}


def main(argv):
    root = HERE.parent.parent
    items = read(root)
    if not items:
        print("Chưa có ảnh tham chiếu (người nạp ở tab Tham chiếu của trang quản lý).")
        return 0
    print(f"# Ảnh tham chiếu ({len(items)}), thư mục {'/'.join(DIR)}/ (Read từng ảnh để xem)")
    for x in items:
        print(f"- {x['id']} · {x['kind']}" + (f" · {', '.join(x['tags'])}" if x["tags"] else "") + f" · {'/'.join(DIR)}/{x['file']}")
        if x["note"]:
            print("  " + x["note"].replace("\n", "\n  "))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
