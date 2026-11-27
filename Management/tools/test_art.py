#!/usr/bin/env python3
"""Tests of art.py on a temp project: the art folders found, the tier the game shows, the human's choice, and
drawing an SVG into a PNG of the asked size; and of render.check_svg.

    python Management/tools/test_art.py
"""
import contextlib
import io
import os
import shutil
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import art  # noqa: E402
import render  # noqa: E402

RESULTS = []
PNG_1x1 = bytes.fromhex("89504e470d0a1a0a0000000d4948445200000001000000010806000000")


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def call(root, *args):
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = art.main(["art.py", *args, "--root", str(root)])
    return code, out.getvalue()


def put(root, rel, data=PNG_1x1):
    path = Path(root, rel)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return path


def main():
    root = Path(tempfile.mkdtemp(prefix="art-"))
    try:
        hero = "UnityProject/Assets/Features/Hero/FeatureAssets/Private/Resources/Art/Hero"
        popup = "UnityProject/Assets/Features/_Services/Popup/FeatureAssets/Public/Resources/Art/_Services/Popup"
        check("list: nothing yet means GreyBox", ("GreyBox" in call(root, "list")[1], art.items(root)), (True, []))
        put(root, f"{hero}/hero-knight/Simple.png")
        put(root, f"{hero}/hero-knight/Simple.png.meta", b"meta")
        put(root, f"{hero}/hero-knight/Detail.png")
        put(root, f"{hero}/hero-mage/Simple.png")
        put(root, f"{hero}/castle-3d/Simple.fbx", b"fbx")
        put(root, f"{hero}/castle-3d/Simple.preview.png")
        put(root, f"{popup}/panel/Simple.png")
        put(root, f"{hero}/Bad_Id/Simple.png")
        put(root, f"{hero}/hero-note/readme.txt", b"x")
        rows = {r["key"]: r for r in art.items(root)}
        check("items: every art folder with a tier, bad ids and folders without a tier left out",
              sorted(rows), ["Hero/castle-3d", "Hero/hero-knight", "Hero/hero-mage", "_Services/Popup/panel"])
        check("items: Detail wins when there is one", (rows["Hero/hero-knight"]["use"], rows["Hero/hero-mage"]["use"]), ("detail", "simple"))
        check("items: a model shows its preview", (rows["Hero/castle-3d"]["kind"], rows["Hero/castle-3d"]["simple"]["preview"].endswith("Simple.preview.png")),
              ("model", True))
        check("items: a feature under _Services", rows["_Services/Popup/panel"]["feature"], "_Services/Popup")
        check("items: by feature", [r["key"] for r in art.items(root, "_Services/Popup")], ["_Services/Popup/panel"])
        check("choose: keep Simple although there is a Detail", (call(root, "choose", "Hero/hero-knight", "simple")[0],
              art.read_choice(root), {r["key"]: r["use"] for r in art.items(root)}["Hero/hero-knight"]), (0, ["Hero/hero-knight"], "simple"))
        check("choose: Detail again clears the choice", (call(root, "choose", "Hero/hero-knight", "detail")[0], art.read_choice(root)), (0, []))
        check("choose: Simple of an art with no Detail writes nothing", (call(root, "choose", "Hero/hero-mage", "simple")[0], art.read_choice(root)), (0, []))
        check("choose: a tier the art does not have is refused", call(root, "choose", "Hero/hero-mage", "detail")[0], 1)
        check("choose: an unknown art is refused", call(root, "choose", "Hero/nope", "simple")[0], 1)
        check("choose: a wrong tier is refused", call(root, "choose", "Hero/hero-knight", "greybox")[0], 1)
        check("list: says what the game uses", "Hero/hero-knight (ảnh): có Simple+Detail, đang dùng Detail" in call(root, "list")[1])
        check("main: a wrong command", call(root, "nope")[0], 2)

        good = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" fill="red"/></svg>'
        check("check_svg: a plain drawing passes", render.check_svg(good), [])
        check("check_svg: a script is refused", bool(render.check_svg(good.replace("<rect", "<script>x</script><rect"))))
        check("check_svg: an outside href is refused", bool(render.check_svg(good.replace("<rect", '<image href="http://x/y.png"/><rect'))))
        check("check_svg: no viewBox is refused", bool(render.check_svg(good.replace(' viewBox="0 0 10 10"', ""))))
        svg = Path(root, "a.svg")
        svg.write_text(good, encoding="utf-8")
        check("svg: a wrong size is refused", call(root, "svg", str(svg), str(root / "a.png"), "big")[0], 1)
        check("svg: a bad drawing is refused", (svg.with_name("b.svg").write_text("<svg/>", encoding="utf-8"),
                                                call(root, "svg", str(svg.with_name("b.svg")), str(root / "b.png"), "8x8")[0])[1], 1)
        fake = Path(root, "fake_svg2png.py")      # writes a PNG header of the asked size, as a converter would
        fake.write_text("import sys\nw,h=int(sys.argv[3]),int(sys.argv[4])\nopen(sys.argv[2],'wb').write(bytes.fromhex("
                        "'89504e470d0a1a0a0000000d49484452')+w.to_bytes(4,'big')+h.to_bytes(4,'big')+bytes(5))\n", encoding="utf-8")
        keep = {k: os.environ.get(k) for k in ("RENDER_SVG2PNG", "PATH", "RENDER_BROWSER")}
        os.environ.update(RENDER_SVG2PNG=str(fake), PATH="", RENDER_BROWSER="")
        try:
            out = root / "out/Simple.png"
            code, text = call(root, "svg", str(svg), str(out), "64x32")
            check("svg: drawn at the asked size", (code, render.png_size(out)), (0, (64, 32)))
        finally:
            for k, v in keep.items():
                if v is None:
                    os.environ.pop(k, None)
                else:
                    os.environ[k] = v
    finally:
        shutil.rmtree(root, ignore_errors=True)
    print(f"TOTAL: {sum(RESULTS)} ok, {len(RESULTS) - sum(RESULTS)} failed")
    return 0 if all(RESULTS) else 1


if __name__ == "__main__":
    sys.exit(main())
