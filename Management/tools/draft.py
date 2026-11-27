#!/usr/bin/env python3
"""Drafts of a feature's code (người quyết 2026-11-27): a feature writes its code in UnityProject/Drafts/<Tên>/,
where Unity does not read it, so a draft may not compile yet (it may use a contract still asked for). Only the chat
that holds the Editor ticket (editor_ticket.py) loads its draft into Assets: then it compiles, fixes, makes assets and
runs tests. Outside a turn with the ticket, Assets holds the last clean code.

    python Management/tools/draft.py start  <Tên> <đường dẫn>...    copy files of Assets into the draft, to change them
    python Management/tools/draft.py new    <Tên> <đường dẫn>...    new files (not in Assets yet)
    python Management/tools/draft.py delete <Tên> <đường dẫn>...    the draft removes these files of Assets
    python Management/tools/draft.py status <Tên>                   what the draft changes; files of Assets changed since (lệch gốc)
    python Management/tools/draft.py diff   <Tên> [<đường dẫn>]     the changes as a diff
    python Management/tools/draft.py rebase <Tên> <đường dẫn>...    "I merged the newer file of Assets into my draft": take it as the base
    python Management/tools/draft.py drop   <Tên> [<đường dẫn>...]  throw away the draft (or some files of it)
    python Management/tools/draft.py apply  <Tên>                   load the draft into Assets (the ticket holder only)

<Tên>: Wallet, _Services/Popup, Core (@Core), _Composition. <đường dẫn>: inside that folder, as in the draft
(Scripts/Internal/Wallet.cs, Tests/WalletTests.cs). Only .cs files. The draft mirrors the folder:
Drafts/Wallet/Scripts/Internal/Wallet.cs is Assets/Features/Wallet/Scripts/Internal/Wallet.cs.
Drafts/<Tên>/.draft.json (written by this script only): {"base": {path: sha1 of the Assets file when the draft
started, or null for a new file}, "delete": [paths]}. apply refuses when a file of Assets changed since its base
(someone, CodeLead, changed it): merge it into the draft, then rebase that path and apply again.
Exit: 0 ok, 1 apply refused (lệch gốc) or nothing to do, 2 a wrong command.
"""
import difflib
import hashlib
import json
import os
import re
import shutil
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
DRAFTS = ("UnityProject", "Drafts")
META = ".draft.json"
APPLIED = ".applied"
NAME_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
PATH_RX = re.compile(r"^(?!/)(?!.*(^|/)\.\.?(/|$))[A-Za-z0-9_./ -]+\.cs$")


def folder_of(root, name):
    """(name as stored, the feature's folder in Assets)."""
    name = (name or "").strip().replace("\\", "/")
    assets = Path(root) / "UnityProject" / "Assets"
    if name in ("@Core", "Core"):
        return "Core", assets / "Core"
    if name in ("_Composition", "Composition"):
        return "_Composition", assets / "_Composition"
    if not NAME_RX.match(name):
        raise ValueError(f"'{name}' không phải feature, service, Core hay _Composition")
    return name, assets / "Features" / name


def draft_dir(root, name):
    return Path(root).joinpath(*DRAFTS) / name


def sha(path):
    try:
        return hashlib.sha1(Path(path).read_bytes()).hexdigest()
    except OSError:
        return None


def clean_path(p):
    p = (p or "").strip().replace("\\", "/")
    if not PATH_RX.match(p):
        raise ValueError(f"đường dẫn '{p}' phải là file .cs bên trong thư mục của feature (không có '..', không bắt đầu bằng /)")
    return p


def load(root, name):
    m = draft_dir(root, name) / META
    try:
        data = json.loads(m.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        data = {}
    data.setdefault("base", {})
    data.setdefault("delete", [])
    return data


def save(root, name, data):
    d = draft_dir(root, name)
    d.mkdir(parents=True, exist_ok=True)
    tmp = d / (META + f".{os.getpid()}.tmp")
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=1, sort_keys=True), encoding="utf-8")
    os.replace(tmp, d / META)


def start(root, name, paths):
    name, folder = folder_of(root, name)
    data, out = load(root, name), []
    for p in map(clean_path, paths):
        src = folder / p
        if not src.is_file():
            raise ValueError(f"{p}: chưa có trong Assets, dùng draft.py new")
        dst = draft_dir(root, name) / p
        if dst.exists():
            out.append(f"{p}: đã có trong nháp, giữ nguyên")
            continue
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)
        data["base"][p] = sha(src)
        data["delete"] = [x for x in data["delete"] if x != p]
        out.append(f"{p}: đã chép vào nháp")
    save(root, name, data)
    return out


def new(root, name, paths):
    name, folder = folder_of(root, name)
    data, out = load(root, name), []
    for p in map(clean_path, paths):
        if (folder / p).exists():
            raise ValueError(f"{p}: đã có trong Assets, dùng draft.py start")
        dst = draft_dir(root, name) / p
        if not dst.exists():
            dst.parent.mkdir(parents=True, exist_ok=True)
            dst.write_text("", encoding="utf-8")
        data["base"][p] = None
        out.append(f"{p}: file mới trong nháp")
    save(root, name, data)
    return out


def delete(root, name, paths):
    name, folder = folder_of(root, name)
    data, out = load(root, name), []
    for p in map(clean_path, paths):
        if not (folder / p).is_file():
            raise ValueError(f"{p}: không có trong Assets")
        f = draft_dir(root, name) / p
        if f.exists():
            f.unlink()
        data["base"][p] = sha(folder / p)
        if p not in data["delete"]:
            data["delete"].append(p)
        out.append(f"{p}: nháp xoá file này")
    save(root, name, data)
    return out


def entries(root, name):
    """[(path, kind: sửa | mới | xoá, lệch gốc: bool)] of the draft."""
    name, folder = folder_of(root, name)
    data = load(root, name)
    out = []
    for p, base in sorted(data["base"].items()):
        now = sha(folder / p)
        if p in data["delete"]:
            out.append((p, "xoá", now != base))
        elif base is None:
            out.append((p, "mới", now is not None))      # someone made that file in Assets meanwhile
        else:
            out.append((p, "sửa", now != base))
    return out


def status(root, name):
    rows = entries(root, name)
    if not rows:
        return [f"Nháp {name}: trống"]
    out = [f"Nháp {name}: {len(rows)} file" + (f", {sum(1 for r in rows if r[2])} lệch gốc" if any(r[2] for r in rows) else "")]
    for p, kind, drift in rows:
        out.append(f"  {kind:4} {p}" + ("  LỆCH GỐC: file trong Assets đã đổi từ lúc bắt đầu nháp (trộn vào nháp rồi rebase)" if drift else ""))
    return out


def diff(root, name, only=None):
    name, folder = folder_of(root, name)
    out = []
    for p, kind, _ in entries(root, name):
        if only and p != clean_path(only):
            continue
        a = (folder / p).read_text(encoding="utf-8", errors="replace").splitlines() if (folder / p).is_file() else []
        b = [] if kind == "xoá" else (draft_dir(root, name) / p).read_text(encoding="utf-8", errors="replace").splitlines()
        out += list(difflib.unified_diff(a, b, f"Assets/{p}", f"Drafts/{p}", lineterm=""))
    return out or ["(không khác)"]


def rebase(root, name, paths):
    name, folder = folder_of(root, name)
    data, out = load(root, name), []
    for p in map(clean_path, paths):
        if p not in data["base"]:
            raise ValueError(f"{p}: không có trong nháp")
        data["base"][p] = sha(folder / p)
        out.append(f"{p}: lấy bản hiện tại của Assets làm gốc")
    save(root, name, data)
    return out


def drop(root, name, paths=None):
    name, _ = folder_of(root, name)
    d = draft_dir(root, name)
    if not paths:
        if d.exists():
            shutil.rmtree(d)
        return [f"Đã bỏ cả nháp {name}"]
    data, out = load(root, name), []
    for p in map(clean_path, paths):
        data["base"].pop(p, None)
        data["delete"] = [x for x in data["delete"] if x != p]
        if (d / p).exists():
            (d / p).unlink()
        out.append(f"{p}: đã bỏ khỏi nháp")
    save(root, name, data)
    return out


def apply(root, name, now=None):
    """Loads the draft into Assets. (lines, ok). Refused, nothing written, when a file drifted from its base."""
    name, folder = folder_of(root, name)
    rows = entries(root, name)
    if not rows:
        return [f"Nháp {name} trống: không có gì để nạp"], False
    drift = [p for p, _, d in rows if d]
    if drift:
        return ([f"KHÔNG NẠP: {len(drift)} file trong Assets đã đổi từ lúc bắt đầu nháp: {', '.join(drift)}.",
                 "Xem draft.py diff, trộn thay đổi đó vào nháp, rồi draft.py rebase <Tên> <file> và apply lại."], False)
    d = draft_dir(root, name)
    data = load(root, name)
    done = []
    for p, kind, _ in rows:
        target = folder / p
        if kind == "xoá":
            for f in (target, Path(str(target) + ".meta")):
                if f.exists():
                    f.unlink()
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(d / p, target)      # Unity makes the .meta of a new file when it imports it
        done.append({"path": p, "kind": kind})
    stamp = time.strftime("%Y%m%d-%H%M%S", time.localtime(now))
    log = d / APPLIED / f"{stamp}.json"
    log.parent.mkdir(parents=True, exist_ok=True)
    log.write_text(json.dumps({"at": stamp, "files": done}, ensure_ascii=False, indent=1), encoding="utf-8")
    for p, kind, _ in rows:      # the draft is now in Assets: empty it, keep the log
        f = d / p
        if f.exists():
            f.unlink()
    data["base"], data["delete"] = {}, []
    save(root, name, data)
    for sub in sorted((x for x in d.rglob("*") if x.is_dir() and APPLIED not in x.parts), key=lambda x: -len(x.parts)):
        try:
            sub.rmdir()      # only empty folders go
        except OSError:
            pass
    return [f"Đã nạp nháp {name} vào Assets: {len(done)} file ({', '.join(x['kind'] + ' ' + x['path'] for x in done[:8])}"
            + (" …" if len(done) > 8 else "") + "). Tiếp: refresh_unity, đọc console, sửa lỗi nếu có."], True


def main(argv):
    root = ROOT
    args = argv[1:]
    if len(args) >= 2 and args[-2] == "--root":      # tests
        root, args = Path(args[-1]), args[:-2]
    try:
        cmd, name, rest = (args + [None, None])[0], (args + [None, None])[1], args[2:]
        if cmd in ("start", "new", "delete", "rebase") and name and rest:
            lines = {"start": start, "new": new, "delete": delete, "rebase": rebase}[cmd](root, name, rest)
        elif cmd == "status" and name and not rest:
            lines = status(root, name)
        elif cmd == "diff" and name and len(rest) <= 1:
            lines = diff(root, name, rest[0] if rest else None)
        elif cmd == "drop" and name:
            lines = drop(root, name, rest or None)
        elif cmd == "apply" and name and not rest:
            lines, ok = apply(root, name)
            print("\n".join(lines))
            return 0 if ok else 1
        else:
            print(__doc__, file=sys.stderr)
            return 2
    except ValueError as ex:
        print(f"draft.py: {ex}", file=sys.stderr)
        return 2
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
