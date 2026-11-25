#!/usr/bin/env python3
"""Tests for references.py on a throw-away folder. Run: python Management/tools/test_references.py"""
import base64
import io
import json
import shutil
import sys
import tempfile
from contextlib import redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import references as rf  # noqa: E402

results = []
PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 32
JPG = b"\xff\xd8\xff\xe0" + b"\x00" * 32
WEBP = b"RIFF\x24\x00\x00\x00WEBPVP8 " + b"\x00" * 24


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def refused(fn, *args):
    try:
        fn(*args)
        return False
    except ValueError:
        return True


def b64(raw):
    return base64.b64encode(raw).decode()


def main():
    root = Path(tempfile.mkdtemp(prefix="refs-"))
    try:
        check("nothing yet: an empty list", rf.read(root), [])
        a = rf.upload(root, "HUD Trận Đẹp.png", b64(PNG), "Nút tạm dừng nhỏ ở góc, tiền giữa trên", "bố cục", ["HUD trận"])
        d = rf.folder(root)
        check("upload: the file by its content, an id from the time and the name, the note kept",
              (a["file"].endswith(".png"), a["id"].endswith("-hud-tran-dep"), (d / a["file"]).read_bytes() == PNG, a["note"], a["tags"]),
              (True, True, True, "Nút tạm dừng nhỏ ở góc, tiền giữa trên", ["HUD trận"]))
        j = rf.upload(root, "x.png", b64(JPG))
        w = rf.upload(root, "x.png", b64(WEBP), "", "style art")
        check("upload: jpg and webp by their bytes, whatever the name says; same name gets another id",
              (j["file"].endswith(".jpg"), w["file"].endswith(".webp"), j["id"] != w["id"], w["kind"]), (True, True, True, "style art"))
        check("upload: newest first", [x["id"] for x in rf.read(root)][0], w["id"])
        for label, args in (("not a picture", ("a.png", b64(b"GIF89a" + b"\x00" * 20))), ("not base64", ("a.png", "%%%")),
                            ("empty", ("a.png", "")), ("too big", ("a.png", b64(PNG + b"\x00" * rf.MAX_BYTES))),
                            ("unknown kind", ("a.png", b64(PNG), "", "màu")), ("note too long", ("a.png", b64(PNG), "x" * (rf.NOTE_MAX + 1))),
                            ("too many tags", ("a.png", b64(PNG), "", "bố cục", ["t"] * (rf.TAGS_MAX + 1)))):
            check("upload refused: " + label, refused(rf.upload, root, *args))
        check("nothing was written for the refused ones", len(rf.read(root)), 3)
        rf.update(root, a["id"], "  Nút chính to, nằm dưới phải  ", "bố cục", ["HUD trận", " ", "thưởng"])
        hit = next(x for x in rf.read(root) if x["id"] == a["id"])
        check("update: note trimmed, empty tags dropped", (hit["note"], hit["tags"]), ("Nút chính to, nằm dưới phải", ["HUD trận", "thưởng"]))
        check("update refused: an unknown id", refused(rf.update, root, "nope", "", "bố cục", []))
        rf.delete(root, j["id"])
        check("delete: the file and its line go", ((d / j["file"]).exists(), [x["id"] for x in rf.read(root)].count(j["id"])), (False, 0))
        check("delete refused: an unknown id", refused(rf.delete, root, j["id"]))
        (d / rf.INDEX).write_text("{ broken", encoding="utf-8")
        check("a broken index still lists the pictures on disk, with empty notes", sorted(x["note"] for x in rf.read(root)), ["", ""])
        (d / rf.INDEX).write_text(json.dumps({"items": [{"id": "../x", "file": "../x.png"}, {"id": "ok", "file": "ok.exe"}]}), encoding="utf-8")
        check("a line pointing outside, or at a file that is no picture, is dropped", all(x["id"] not in ("../x", "ok") for x in rf.read(root)))
        snap = rf.snapshot(root)
        check("snapshot: items, kinds, folder", (len(snap["items"]), snap["kinds"], snap["dir"]), (2, list(rf.KINDS), "references/"))
        out = io.StringIO()
        with redirect_stdout(out):
            rf.HERE = root / "Management" / "tools"
            rf.main(["references.py"])
        check("the command lists every picture with its path", out.getvalue().count("Management/references/"), 3)
    finally:
        shutil.rmtree(root, ignore_errors=True)
    failed = [r for r in results if not r[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
