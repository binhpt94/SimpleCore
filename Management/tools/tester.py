#!/usr/bin/env python3
"""The test runs of the tester chats (group Testers; .claude/rules/tester.md).

    Tester/
      README.md                      the structure, who reads what
      <Tester>/                      one folder per tester chat (UnityAutoPlay)
        README.md                    what this tester tests, how to read a run
        <yyyymmdd-hhmmss>/           one test run
          README.md                  what was tested and why, the steps, the result (the sections in SECTIONS)
          images/                    pictures and other data of the run
        Archive/                     old runs, moved here by `archive`; only the testers read them
          README.md                  the list of archived runs (written here)

Commands (run from the project root; the guard decides who may run which):
    python Management/tools/tester.py new <Tester> "<title>"       start a run: the folder, its README, images/
    python Management/tools/tester.py archive <Tester> <run>       move one run to Archive
    python Management/tools/tester.py archive <Tester> --keep <N>  move all but the N newest runs to Archive
    python Management/tools/tester.py status [<Tester>]            the open runs, their purpose and result
    python Management/tools/tester.py check                        every folder has its README, runs keep the structure
"""
import re
import shutil
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent

BASE = "Tester"
TESTERS = ("UnityAutoPlay",)
ARCHIVE, IMAGES, README = "Archive", "images", "README.md"
RUN_RX = re.compile(r"^\d{8}-\d{6}$")
# The sections every run README has, in this order.
SECTIONS = ("Mục đích", "Test gì", "Môi trường", "Các bước", "Kết quả", "Lỗi tìm thấy", "Cấu trúc thư mục")
EMPTY = "(chưa ghi)"


def now():
    return time.strftime("%Y%m%d-%H%M%S")


def tester_dir(root, name):
    if name not in TESTERS:
        raise ValueError(f"không có tester '{name}'. Có: {', '.join(TESTERS)}")
    return Path(root) / BASE / name


def runs(folder):
    """The run folders directly inside `folder`, oldest first."""
    return sorted(p for p in folder.iterdir() if p.is_dir() and RUN_RX.match(p.name)) if folder.is_dir() else []


def sections(text):
    """{section title: its text} of a run README."""
    out, title = {}, None
    for line in text.split("\n"):
        m = re.match(r"^## (.+?)\s*$", line)
        if m:
            title = m.group(1)
            out[title] = []
        elif title is not None:
            out[title].append(line)
    return {k: "\n".join(v).strip() for k, v in out.items()}


def first_line(text):
    lines = [x.strip() for x in (text or "").split("\n") if x.strip()]
    return lines[0] if lines else EMPTY


def run_readme(name, run, title):
    hints = {
        "Mục đích": "<vì sao chạy đợt này, ai nhờ, muốn biết điều gì>",
        "Test gì": "<feature, node thiết kế (live/...), state cheat, scene>",
        "Môi trường": "<commit git, scene mở, bản dev hay không>",
        "Các bước": "1. <thao tác>\n2. <thao tác>",
        "Kết quả": "<đạt | không đạt | dở>: <một câu>",
        "Lỗi tìm thấy": "- <lỗi>: <thấy ở bước nào, ảnh nào, thuộc feature nào>\n(hoặc: không)",
        "Cấu trúc thư mục": f"- {README}: file này\n- {IMAGES}/: ảnh chụp và dữ liệu phụ; mỗi file một dòng: <tên file>: <là gì>",
    }
    body = "\n\n".join(f"## {s}\n{hints[s]}" for s in SECTIONS)
    return f"# {title}\nTester: {name} · Đợt: {run}\n\n{body}\n"


def cmd_new(root, name, title):
    folder = tester_dir(root, name)
    title = (title or "").strip()
    if not title:
        raise ValueError("thiếu tên đợt test")
    run = now()
    target = folder / run
    if target.exists():
        raise ValueError(f"đã có đợt {run}; chờ một giây rồi chạy lại")
    (target / IMAGES).mkdir(parents=True)
    (target / README).write_text(run_readme(name, run, title), encoding="utf-8")
    print(f"Đã tạo đợt test: {BASE}/{name}/{run}/")
    print(f"- Ghi {BASE}/{name}/{run}/{README}: đủ các mục {', '.join(SECTIONS)}.")
    print(f"- Ảnh và dữ liệu phụ để trong {BASE}/{name}/{run}/{IMAGES}/, mỗi file một dòng ở mục 'Cấu trúc thư mục'.")
    print("- Xong thì chạy: python Management/tools/tester.py check")
    return 0


def archive_index(folder):
    """Rewrites Archive/README.md: one line per archived run."""
    archive = folder / ARCHIVE
    lines = [f"# Các đợt test đã lưu trữ của {folder.name}",
             "Đợt cũ được dời vào đây bằng `python Management/tools/tester.py archive`. Chỉ nhóm Testers (và người) đọc "
             "thư mục này; các phiên khác không đọc để không loãng ngữ cảnh.", "",
             "## Cấu trúc thư mục", f"- {README}: file này (lệnh tự ghi)", "- <yyyymmdd-hhmmss>/: một đợt test, nguyên vẹn như lúc dời", "",
             "## Danh mục"]
    for run in reversed(runs(archive)):
        text = (run / README).read_text(encoding="utf-8") if (run / README).is_file() else ""
        title = first_line(text).lstrip("# ").strip()
        got = sections(text)
        lines.append(f"- {run.name}: {title} · {first_line(got.get('Kết quả'))}")
    archive.mkdir(parents=True, exist_ok=True)
    (archive / README).write_text("\n".join(lines) + "\n", encoding="utf-8")


def cmd_archive(root, name, what, count=None):
    folder = tester_dir(root, name)
    open_runs = runs(folder)
    if what == "--keep":
        if count is None or not str(count).isdigit():
            raise ValueError("--keep cần một số (giữ lại bấy nhiêu đợt mới nhất)")
        keep = int(count)
        moving = open_runs[:len(open_runs) - keep] if keep < len(open_runs) else []
    else:
        if not RUN_RX.match(what or ""):
            raise ValueError("tên đợt có dạng yyyymmdd-hhmmss")
        moving = [r for r in open_runs if r.name == what]
        if not moving:
            raise ValueError(f"{BASE}/{name} không có đợt '{what}' đang mở")
    for run in moving:
        target = folder / ARCHIVE / run.name
        if target.exists():
            raise ValueError(f"{BASE}/{name}/{ARCHIVE} đã có đợt {run.name}")
        (folder / ARCHIVE).mkdir(parents=True, exist_ok=True)
        shutil.move(str(run), str(target))
    if moving or (folder / ARCHIVE).is_dir():
        archive_index(folder)
    print(f"Đã dời {len(moving)} đợt vào {BASE}/{name}/{ARCHIVE}/" + (": " + ", ".join(r.name for r in moving) if moving else "."))
    return 0


def check(root):
    """The problems of the Tester tree, as text lines."""
    out = []
    base = Path(root) / BASE
    if not base.is_dir():
        return out
    if not (base / README).is_file():
        out.append(f"{BASE}: thiếu {README}")
    for name in TESTERS:
        folder = base / name
        if not folder.is_dir():
            continue
        where = f"{BASE}/{name}"
        if not (folder / README).is_file():
            out.append(f"{where}: thiếu {README}")
        for item in sorted(folder.iterdir()):
            if item.name == README or (item.is_dir() and (item.name == ARCHIVE or RUN_RX.match(item.name))):
                continue
            out.append(f"{where}/{item.name}: không thuộc cấu trúc (chỉ {README}, các đợt yyyymmdd-hhmmss, {ARCHIVE})")
        if (folder / ARCHIVE).is_dir() and not (folder / ARCHIVE / README).is_file():
            out.append(f"{where}/{ARCHIVE}: thiếu {README}")
        for run in runs(folder):
            at = f"{where}/{run.name}"
            if not (run / README).is_file():
                out.append(f"{at}: thiếu {README}")
                continue
            got = sections((run / README).read_text(encoding="utf-8"))
            for s in SECTIONS:
                if s not in got:
                    out.append(f"{at}/{README}: thiếu mục '## {s}'")
                elif not got[s] or re.search(r"<[^<>\n]+>", got[s]):      # empty, or still the hint of the skeleton
                    out.append(f"{at}/{README}: mục '## {s}' chưa ghi")
            for item in sorted(run.iterdir()):
                if item.is_dir() and item.name != IMAGES:
                    out.append(f"{at}/{item.name}: dữ liệu phụ để trong {IMAGES}/")
                elif item.is_file() and item.suffix.lower() != ".md":
                    out.append(f"{at}/{item.name}: ảnh và dữ liệu phụ để trong {IMAGES}/")
            listed = got.get("Cấu trúc thư mục", "")
            for item in sorted((run / IMAGES).rglob("*")) if (run / IMAGES).is_dir() else []:
                if item.is_file() and item.name not in listed:
                    out.append(f"{at}/{IMAGES}/{item.name}: chưa có dòng mô tả trong mục 'Cấu trúc thư mục'")
    return out


def cmd_check(root):
    problems = check(root)
    for p in problems:
        print(p)
    print(f"{len(problems)} lỗi cấu trúc." if problems else "Thư mục Tester đúng cấu trúc.")
    return 1 if problems else 0


def cmd_status(root, name=None):
    names = [name] if name else list(TESTERS)
    for n in names:
        folder = tester_dir(root, n)
        open_runs = runs(folder)
        archived = len(runs(folder / ARCHIVE))
        print(f"# {n}: {len(open_runs)} đợt đang mở, {archived} đợt đã lưu trữ")
        for run in reversed(open_runs):
            text = (run / README).read_text(encoding="utf-8") if (run / README).is_file() else ""
            got = sections(text)
            print(f"- {BASE}/{n}/{run.name}: {first_line(text).lstrip('# ').strip()}")
            print(f"    Mục đích: {first_line(got.get('Mục đích'))}")
            print(f"    Kết quả: {first_line(got.get('Kết quả'))}")
    return 0


def main(argv, root=None):
    root = Path(root) if root else HERE.parent.parent
    args = argv[1:]
    try:
        if len(args) == 3 and args[0] == "new":
            return cmd_new(root, args[1], args[2])
        if len(args) == 3 and args[0] == "archive":
            return cmd_archive(root, args[1], args[2])
        if len(args) == 4 and args[0] == "archive" and args[2] == "--keep":
            return cmd_archive(root, args[1], "--keep", args[3])
        if args and args[0] == "status" and len(args) <= 2:
            return cmd_status(root, args[1] if len(args) == 2 else None)
        if args == ["check"]:
            return cmd_check(root)
    except (ValueError, OSError) as ex:
        print(f"Không làm được: {ex}", file=sys.stderr)
        return 2
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
