#!/usr/bin/env python3
"""Draws an SVG into a PNG with whatever this machine has, and draws a frame of a design node into a picture.
Used by gd_frame.py (--png) and art.py (svg: a worker's Simple art). Standard library only; Windows and Linux.

    RENDER_SVG2PNG   a command or a .py taking <in> <out> [<width> <height>]: tried first
    RENDER_BROWSER   a Chrome or Edge drawing a page without a window: tried last ('' turns it off)
    RENDER_TIMEOUT   seconds one converter may take (default 300)
"""
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

BROWSERS = ("chrome", "google-chrome", "chromium", "chromium-browser", "msedge")
BROWSER_PATHS = (r"C:\Program Files\Google\Chrome\Application\chrome.exe",
                 r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                 r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                 r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                 "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
SVG_MAX = 2 * 1024 * 1024
SVG_BANNED = ("script", "foreignObject", "iframe", "object", "embed", "audio", "video")
TIMEOUT = int(os.environ.get("RENDER_TIMEOUT") or 300)


def read_text(path):
    return Path(path).read_text(encoding="utf-8-sig", errors="replace").replace("\r", "")


def run(cmd):
    """(exit code, tail of stderr)."""
    try:
        p = subprocess.run(cmd, capture_output=True, timeout=TIMEOUT)
        return p.returncode, (p.stderr or b"").decode("utf-8", "replace")[-1500:]
    except subprocess.TimeoutExpired:
        return -1, f"quá {TIMEOUT}s"
    except OSError as ex:
        return -2, str(ex)


def browser_exe():
    """A Chrome or Edge that can draw a page without a window (RENDER_BROWSER overrides), or ''."""
    override = os.environ.get("RENDER_BROWSER")
    if override is not None:
        return override
    return next((shutil.which(n) for n in BROWSERS if shutil.which(n)), "") or next((p for p in BROWSER_PATHS if Path(p).is_file()), "")


def svg_box(text):
    """(width, height) of an SVG's viewBox, or None."""
    m = re.search(r"<svg\b[^>]*?\bviewBox\s*=\s*[\"']\s*([-+.\deE]+)[\s,]+([-+.\deE]+)[\s,]+([-+.\deE]+)[\s,]+([-+.\deE]+)\s*[\"']", text)
    try:
        w, h = float(m.group(3)), float(m.group(4))
    except (AttributeError, ValueError):
        return None
    return (w, h) if w > 0 and h > 0 else None


def svg_converters():
    """The names of the ways this machine has to draw an SVG into a PNG, in the order svg_to_png tries them."""
    return ([n for n, ok in (("RENDER_SVG2PNG", os.environ.get("RENDER_SVG2PNG")), ("rsvg-convert", shutil.which("rsvg-convert")),
                             ("inkscape", shutil.which("inkscape")), ("magick", shutil.which("magick"))) if ok]
            + (["browser"] if browser_exe() else []))


def svg_to_png(svg_path, png_path, size=None):
    """Draws an SVG into a PNG: RENDER_SVG2PNG, rsvg-convert, inkscape, magick, then Chrome or Edge without a window.
    size: (width, height) in px; without it the PNG is 1920 px wide and keeps the shape of the viewBox. The
    background stays transparent. Returns the converter used, or '' when none worked."""
    svg_path, png_path = Path(svg_path), Path(png_path)
    box = svg_box(read_text(svg_path)) or (16, 9)
    w, h = size or (1920, max(1, round(1920 * box[1] / box[0])))
    override = os.environ.get("RENDER_SVG2PNG")
    tries = []
    if override:
        tries.append(([sys.executable, override] if override.endswith(".py") else [override]) + [str(svg_path), str(png_path)]
                     + ([str(w), str(h)] if size else []))
    if shutil.which("rsvg-convert"):
        tries.append(["rsvg-convert", "-w", str(w), "-h", str(h), "-b", "none", "-o", str(png_path), str(svg_path)])
    if shutil.which("inkscape"):
        tries.append(["inkscape", str(svg_path), "-o", str(png_path), "-w", str(w), "-h", str(h)])
    if shutil.which("magick"):      # not `convert`: on Windows that name is a system command
        tries.append(["magick", "-background", "none", str(svg_path), "-resize", f"{w}x{h}!", str(png_path)])
    for cmd in tries:
        code, _ = run(cmd)
        if code == 0 and png_path.is_file():
            return cmd[0]
    browser = browser_exe()
    if browser:
        # The SVG is shown as a picture (scripts and outside files are not loaded), at a fixed size at the top left.
        page = png_path.with_name(png_path.name + ".page.html")
        page.write_text("<!doctype html><html><head><meta charset=\"utf-8\"><style>html,body{margin:0;padding:0;"
                        f"background:transparent;overflow:hidden}}img{{display:block;width:{w}px;height:{h}px}}</style></head>"
                        f"<body><img src=\"{svg_path.resolve().as_uri()}\"></body></html>", encoding="utf-8")
        try:
            if png_path.exists():
                png_path.unlink()
            run(([sys.executable, browser] if browser.endswith(".py") else [browser])
                + ["--headless", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--force-device-scale-factor=1",
                   f"--window-size={w},{h}", "--default-background-color=00000000",
                   f"--screenshot={png_path.resolve()}", page.resolve().as_uri()])
        finally:
            try:
                page.unlink()
            except OSError:
                pass
        if png_path.is_file():
            return "browser"
    return ""


def check_svg(text):
    """The problems of an SVG: it must be one drawing that needs nothing outside its file and runs nothing."""
    import xml.etree.ElementTree as ET
    if len(text.encode("utf-8")) > SVG_MAX:
        return [f"file SVG lớn quá {SVG_MAX // 1024} KB"]
    if re.search(r"<!(DOCTYPE|ENTITY)", text, re.I):
        return ["SVG không được có DOCTYPE hay ENTITY"]
    try:
        top = ET.fromstring(text)
    except ET.ParseError as ex:
        return [f"SVG không phải XML hợp lệ ({ex})"]
    name = lambda tag: tag.rsplit("}", 1)[-1] if isinstance(tag, str) else ""      # noqa: E731
    if name(top.tag) != "svg" or not top.tag.startswith("{http://www.w3.org/2000/svg}"):
        return ["gốc phải là <svg xmlns=\"http://www.w3.org/2000/svg\" ...>"]
    problems = []
    if not svg_box(text):
        problems.append("thẻ <svg> thiếu viewBox=\"0 0 <rộng> <cao>\" (rộng, cao lớn hơn 0)")
    for el in top.iter():
        tag = name(el.tag)
        if tag in SVG_BANNED:
            problems.append(f"không dùng thẻ <{tag}>")
        if tag == "style" and re.search(r"@import|url\(\s*[\"']?(?!#)", el.text or "", re.I):
            problems.append("<style> không được @import hay url() trỏ ra ngoài file")
        for key, value in el.attrib.items():
            attr, value = name(key), str(value).strip()
            if attr.lower().startswith("on"):
                problems.append(f"không dùng thuộc tính sự kiện {attr}")
            elif attr == "href" and not (value.startswith("#") or value.startswith("data:image/")):
                problems.append(f"<{tag}> href trỏ ra ngoài file ({value[:40]}): chỉ '#id' hoặc data:image/")
            elif re.search(r"url\(\s*[\"']?(?!#)", value, re.I) or re.match(r"(?i)\s*javascript:", value):
                problems.append(f"<{tag}> {attr} trỏ ra ngoài file: chỉ url(#id)")
    return list(dict.fromkeys(problems))[:8]


def png_size(path):
    """(width, height) of a PNG, or None."""
    head = Path(path).read_bytes()[:24]
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return int.from_bytes(head[16:20], "big"), int.from_bytes(head[20:24], "big")


def frame_css():
    """The colours of a drawn frame without the page's variables, with the font inside (a picture loads no file)."""
    import gamedesign as gd
    return gd.font_css(embed=True) + "\n" + re.sub(r"var\(--[\w-]+, ((?:[^()]|\([^()]*\))*)\)", r"\1", gd.FRAME_CSS).split("\n", 1)[1]


def frame_png_file(fr, dest, stem, concept=True, width=None):
    """One resolved frame drawn into dest/<stem>.png (and the .svg beside it), colours fixed and the font inside.
    concept: only what the player reads; otherwise with the numbers and the notes. width: px (the height keeps the
    shape of the frame); without it 1920. Returns the PNG path, or None when this machine cannot draw an SVG."""
    import gamedesign as gd
    svg = gd.frame_svg(fr, concept=concept).replace("</defs>", f"</defs><style>{frame_css()}</style>", 1)
    dest = Path(dest)
    dest.mkdir(parents=True, exist_ok=True)
    svg_path, png_path = dest / f"{stem}.svg", dest / f"{stem}.png"
    svg_path.write_text(svg, encoding="utf-8")
    size = None
    if width:
        box = svg_box(svg) or (16, 9)
        size = (width, max(1, round(width * box[1] / box[0])))
    return png_path if svg_to_png(svg_path, png_path, size) else None
