#!/usr/bin/env python3
"""Prints the frames (.frame-concept/*.frame) of one design node, resolved: every shape absolute, with its unit,
and what is measurably wrong. Reads only, unless --html is given.

    python Management/tools/gd_frame.py ai/idea/<node>            the proposed node
    python Management/tools/gd_frame.py live/<node>               the fixed node
    python Management/tools/gd_frame.py <node path> --html <file> also writes one self-contained HTML page
                                                                  (the drawing, data-* numbers on every shape)
    python Management/tools/gd_frame.py <node path> --png         also draws every frame into PNGs to look at:
        Management/game-design/.pack/frames/<node, '/' as '.'>/<frame>.png          as a concept shows it (no numbers)
                                                              .../<frame>.measure.png  with the numbers and notes
The frame syntax is at the top of the frames part of gamedesign.py and in .claude/rules/gamedesigner.round4.md.
"""
import html
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402

TABLE_PREFIXES = {"/".join(parts): name for name, parts in gd.TABLES.items()}


def find(root, arg):
    """(table name, node) for 'ai/idea/<path>' or 'live/<path>'; raises ValueError otherwise."""
    arg = arg.replace("\\", "/").strip("/")
    for prefix, table in TABLE_PREFIXES.items():
        if arg == prefix or arg.startswith(prefix + "/"):
            path = arg[len(prefix) + 1:]
            gd.check_path(path)
            tree, _ = gd.read_tree(root, table)
            node = next((n for n in gd.walk(tree) if n["path"] == path), None) if tree else None
            if node is None:
                raise ValueError(f"{prefix} không có node '{path or '(gốc)'}'")
            return table, node
    raise ValueError("đường dẫn phải có dạng " + " hoặc ".join(f"{p}/<node>" for p in TABLE_PREFIXES))


def text_report(table, node):
    out = [f"# Frame của {'/'.join(gd.TABLES[table])}/{node['path']}: {node['title']}"]
    if not node["frames"]:
        out.append(f"(không có frame: khai 'frame: {gd.FRAME_NONE}')" if node["frameNone"] else "(chưa có frame)")
    for fr in node["frames"]:
        out.append("")
        out += gd.frame_brief(fr)
        out += gd.frame_roles_brief(fr)
    out += [f"- nhắc: {t}" for t in node["frameNotes"]]
    return "\n".join(out)


def html_page(table, node):
    title = f"Frame: {node['title']}"
    blocks = []
    for fr in node["frames"]:
        notes = "".join(f"<li>{html.escape(w)}</li>" for w in fr["warnings"] + fr["errors"])
        blocks.append(f"<section><h2>{html.escape(fr['file'])}" + (f" · {html.escape(fr['title'])}" if fr["title"] else "")
                      + f"</h2>{fr['svg']}" + (f"<ul class='warn'>{notes}</ul>" if notes else "")
                      + f"<details><summary>Nguồn</summary><pre>{html.escape(fr['text'])}</pre></details></section>")
    style = (gd.font_css(embed=True, weights=(400,)) + "\n" +      # the page stands alone: the font goes inside it
             "body{font-family:system-ui,sans-serif;margin:16px;max-width:1100px;background:#fff;color:#222}"
             "section{margin:0 0 28px}h2{font-size:16px}.warn{color:#a0520c}pre{background:#f3f4f6;padding:8px;overflow:auto}"
             "@media (prefers-color-scheme: dark){body{background:#16181c;color:#ddd}pre{background:#22252b}"
             ":root{--fr-bg:#1e2126;--fr-ink:#c9d1db;--fr-line:#5c6570;--fr-box:#262a31;--fr-accent-soft:#1f2d40}}\n" + gd.FRAME_CSS)
    return ("<!doctype html><html lang='vi'><head><meta charset='utf-8'>"
            "<meta name='viewport' content='width=device-width,initial-scale=1'>"
            f"<title>{html.escape(title)}</title><style>{style}</style></head><body><h1>{html.escape(title)}</h1>"
            f"<p>{html.escape('/'.join(gd.TABLES[table]) + '/' + node['path'])}</p>" + "".join(blocks) + "</body></html>")


PNG_DIR = ("Management", "game-design", ".pack", "frames")


def write_pngs(root, table, node):
    """Draws every frame of the node twice (as a concept, and with the numbers); returns the printed lines."""
    import studio      # the SVG to PNG ways of the studio
    dest = Path(root).joinpath(*PNG_DIR, (table + "." + node["path"].replace("/", ".")) if node["path"] else table)
    out, failed = [], False
    for fr in node["frames"]:
        stem = Path(fr["file"]).stem
        for name, concept in ((stem, True), (stem + ".measure", False)):
            png = studio.frame_png_file(fr, dest, name, concept=concept)
            if png is None:
                failed = True
                continue
            out.append(f"- {png.relative_to(Path(root)).as_posix()}")
    if failed:
        out.append("Không đổi được SVG sang PNG trên máy này: cần rsvg-convert, inkscape, magick, Chrome hoặc Edge.")
    return (["", "Ảnh để xem (Read từng file; .measure có số):"] + out) if out else out


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    args = argv[1:]
    out_file, png = None, "--png" in args
    args = [a for a in args if a != "--png"]
    if "--html" in args:
        i = args.index("--html")
        if i + 1 >= len(args):
            print("--html cần tên file", file=sys.stderr)
            return 2
        out_file, args = args[i + 1], args[:i] + args[i + 2:]
    if len(args) != 1:
        print(__doc__.strip(), file=sys.stderr)
        return 2
    try:
        table, node = find(root, args[0])
    except ValueError as e:
        print(f"Không đọc được: {e}", file=sys.stderr)
        return 2
    print(text_report(table, node))
    if png:
        print("\n".join(write_pngs(root, table, node)))
    if out_file:
        Path(out_file).write_text(html_page(table, node), encoding="utf-8")
        print(f"\nĐã ghi {out_file}")
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):      # a Windows console is not UTF-8 by itself
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
