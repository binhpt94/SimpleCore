#!/usr/bin/env python3
"""Groups this project's chat threads by tier in the Claude Code session list of VS Code.

    python Management/tools/vscode_groups.py [--dry-run | --restore]

Close VS Code completely (every window) first and run this from a terminal outside VS Code; open VS Code again to
see the groups. For the human only: guard.py lets no agent run it.

The extension (2.1.229+) has named session groups, saved per workspace folder. Where it saves them is not
documented: today the VS Code state database (globalStorage/state.vscdb), table ItemTable, key
'Anthropic.claude-code', a JSON object whose field 'sessionGroups:<workspace folder>' is a list of
{id, name, collapsed, sessionIds}. VS Code keeps that whole key in memory and writes it back whenever the extension
changes anything, so a write made while it runs is lost: this script refuses to write then.

What it does: every thread of the setup (setup_server: named after a FEATURE value, not archived, shown in VS Code)
goes into the group of its tier, "1 · Master" ... "5 · Features", and out of any other group. Groups of your own and
sessions outside the setup are kept. A tier group left empty is dropped. Running it again changes nothing.
Safety: the format is checked first (anything unexpected: nothing is written), the database is copied to
state.vscdb.coresimple-bak before each write, and --restore copies that back.
"""
import json
import os
import re
import shutil
import sqlite3
import subprocess
import sys
import uuid
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent

STATE_KEY = "Anthropic.claude-code"
GROUPS_PREFIX = "sessionGroups:"
WORKSPACE_PREFIXES = (GROUPS_PREFIX, "sessionUnread:", "sessionSectionCollapseState:")
TIERS = ["Master", "Leaders", "Tools", "Testers", "Basic", "Services", "Features"]      # the order of CLAUDE.md, top to bottom
TIER_NAMES = {tier: f"{i} · {tier}" for i, tier in enumerate(TIERS, 1)}
TIER_NAME_RX = re.compile(r"^\d+ · (" + "|".join(TIERS) + r")$")      # also a tier group of an older numbering


def tier_of(name):
    """The tier a group name stands for ('3 · Basic' and '4 · Basic' both: Basic), or None for your own groups."""
    m = TIER_NAME_RX.match(name or "")
    return m.group(1) if m else None
BACKUP_SUFFIX = ".coresimple-bak"
WINDOWS = os.name == "nt"


def vscode_state():
    """The VS Code user state database (globalStorage/state.vscdb) where this OS keeps it."""
    if WINDOWS:
        base = Path(os.environ.get("APPDATA") or Path.home() / "AppData" / "Roaming")
    elif sys.platform == "darwin":
        base = Path.home() / "Library" / "Application Support"
    else:
        base = Path(os.environ.get("XDG_CONFIG_HOME") or Path.home() / ".config")
    return base / "Code" / "User" / "globalStorage" / "state.vscdb"


def vscode_running():
    """True while a VS Code process runs (its state would overwrite ours)."""
    try:
        if WINDOWS:
            out = subprocess.run(["tasklist", "/FO", "CSV", "/NH"], capture_output=True, text=True, timeout=15).stdout
            return '"code.exe"' in out.lower()
        out = subprocess.run(["ps", "-A", "-o", "comm="], capture_output=True, text=True, timeout=15).stdout
    except (OSError, subprocess.SubprocessError):
        return True      # cannot tell: assume it runs, so nothing is written
    for line in out.splitlines():
        line = line.strip()
        if os.path.basename(line) == "code" or "Visual Studio Code.app" in line:
            return True
    return False


def norm(path):
    p = str(path).replace("\\", "/").rstrip("/")
    return p.lower() if WINDOWS else p


def workspace_key(data, root):
    """The workspace folder as the extension spells it in its keys; the project root when it has none yet."""
    want = norm(root)
    for key in data:
        for prefix in WORKSPACE_PREFIXES:
            if key.startswith(prefix) and norm(key[len(prefix):]) == want:
                return key[len(prefix):]
    return str(root)


def check_format(data):
    """Raises ValueError unless every sessionGroups field looks like the format this script writes."""
    if not isinstance(data, dict):
        raise ValueError(f"{STATE_KEY} không phải JSON object")
    for key, groups in data.items():
        if not key.startswith(GROUPS_PREFIX):
            continue
        ok = isinstance(groups, list) and all(
            isinstance(g, dict) and isinstance(g.get("id"), str) and isinstance(g.get("name"), str)
            and isinstance(g.get("collapsed"), bool) and isinstance(g.get("sessionIds"), list)
            and all(isinstance(s, str) for s in g["sessionIds"]) for g in groups)
        if not ok:
            raise ValueError(f"'{key}' không còn đúng định dạng {{id, name, collapsed, sessionIds}}: extension đã đổi "
                             "cách lưu. Không ghi gì; gom nhóm tay trong VS Code (chuột phải vào session).")


def plan_groups(existing, assign):
    """The new group list. existing: the current list; assign: [(session id, tier)] in the order of the setup.
    Tier groups come first, in tier order; your other groups follow, without the sessions moved into a tier."""
    moved = {sid for sid, _ in assign}
    by_tier = {}
    for g in existing:      # the current name first, then a group of an older numbering
        t = tier_of(g["name"])
        if t and (t not in by_tier or g["name"] == TIER_NAMES[t]):
            by_tier[t] = g
    out = []
    for tier in TIERS:
        name = TIER_NAMES[tier]
        old = by_tier.get(tier)
        want = [sid for sid, t in assign if t == tier]
        if not want:
            continue
        keep = [sid for sid in (old["sessionIds"] if old else []) if sid in want]
        ids = keep + [sid for sid in want if sid not in keep]
        out.append({"id": old["id"] if old else str(uuid.uuid4()), "name": name,
                    "collapsed": old["collapsed"] if old else False, "sessionIds": ids})
    for g in existing:
        if tier_of(g["name"]):
            continue
        out.append(dict(g, sessionIds=[sid for sid in g["sessionIds"] if sid not in moved]))
    return out


def read_state(db):
    """(raw value, parsed object) of the extension's key; raises ValueError when there is none."""
    con = sqlite3.connect(db)
    try:
        row = con.execute("select value from ItemTable where key = ?", (STATE_KEY,)).fetchone()
    finally:
        con.close()
    if not row:
        raise ValueError(f"chưa có {STATE_KEY} trong {db}: mở extension Claude Code trong VS Code một lần trước")
    raw = row[0]
    return raw, json.loads(raw if isinstance(raw, str) else raw.decode("utf-8"))


def current_groups(root, db=None):
    """{session id: group name} of this project's groups, for the Setup page; {} when it cannot be read."""
    try:
        _, data = read_state(db or vscode_state())
        groups = data.get(GROUPS_PREFIX + workspace_key(data, root)) or []
        return {sid: g.get("name", "") for g in groups if isinstance(g, dict) for sid in g.get("sessionIds") or []}
    except Exception:
        return {}


def backup_path(db):
    return Path(str(db) + BACKUP_SUFFIX)


def copy_db(src, dst):
    """Consistent copy of an SQLite database (the backup API, not a file copy)."""
    a, b = sqlite3.connect(src), sqlite3.connect(dst)
    try:
        a.backup(b)
    finally:
        a.close()
        b.close()


def apply(root, assign, db=None, dry_run=False, running=vscode_running):
    """Writes the tier groups; returns (workspace key, new group list). Raises ValueError when refused."""
    db = Path(db or vscode_state())
    if not db.is_file():
        raise ValueError(f"không thấy {db}")
    raw, data = read_state(db)
    check_format(data)
    key = GROUPS_PREFIX + workspace_key(data, root)
    groups = plan_groups(data.get(key) or [], assign)
    if dry_run:
        return key, groups
    if running():
        raise ValueError("VS Code đang chạy: nó sẽ ghi đè thay đổi. Tắt hẳn VS Code (mọi cửa sổ) rồi chạy lại "
                         "python Management/tools/vscode_groups.py trong terminal ngoài VS Code.")
    data[key] = groups
    text = json.dumps(data, separators=(",", ":"), ensure_ascii=False)      # the way the extension writes it
    copy_db(db, backup_path(db))
    con = sqlite3.connect(db)
    try:
        with con:
            con.execute("update ItemTable set value = ? where key = ?",
                        (text if isinstance(raw, str) else text.encode("utf-8"), STATE_KEY))
    finally:
        con.close()
    return key, groups


def _without(value, sid):
    """value with every mention of sid taken out: list items equal to it, dict keys equal to it, at any depth."""
    if isinstance(value, list):
        return [_without(v, sid) for v in value if v != sid]
    if isinstance(value, dict):
        return {k: _without(v, sid) for k, v in value.items() if k != sid}
    return value


def forget_session(sid, db=None, running=vscode_running):
    """Takes one session id out of the extension's state (its groups, hiddenSessionIds, unread lists...), for a
    thread that is deleted. Returns the names of the groups it was in. Refused (ValueError) while VS Code runs; no
    database or no extension key yet: nothing to forget. A backup is made before the write, as apply does."""
    db = Path(db or vscode_state())
    if running():
        raise ValueError("VS Code đang chạy: nó sẽ ghi đè thay đổi. Tắt hẳn VS Code (mọi cửa sổ) rồi bấm lại.")
    if not db.is_file():
        return []
    try:
        raw, data = read_state(db)
    except ValueError:
        return []
    check_format(data)
    was_in = [g["name"] for key, groups in data.items() if key.startswith(GROUPS_PREFIX)
              for g in groups if sid in g["sessionIds"]]
    new = _without(data, sid)
    if new == data:
        return was_in
    text = json.dumps(new, separators=(",", ":"), ensure_ascii=False)
    copy_db(db, backup_path(db))
    con = sqlite3.connect(db)
    try:
        with con:
            con.execute("update ItemTable set value = ? where key = ?",
                        (text if isinstance(raw, str) else text.encode("utf-8"), STATE_KEY))
    finally:
        con.close()
    return was_in


def restore(db=None, running=vscode_running):
    db = Path(db or vscode_state())
    bak = backup_path(db)
    if not bak.is_file():
        raise ValueError(f"không có bản sao lưu {bak}")
    if running():
        raise ValueError("VS Code đang chạy: tắt hẳn VS Code rồi khôi phục.")
    copy_db(bak, db)
    return bak


def setup_assignments():
    """[(session id, tier)] for the threads of the full setup, in setup order (read through setup_server)."""
    import setup_server
    sets = setup_server.sets()
    order = {name: i for i, name in enumerate(sets["full"])}
    threads = [t for t in setup_server.threads() if t["feature"] in order and not t["archived"] and not t["unlisted"]]
    threads.sort(key=lambda t: (order[t["feature"]], t["modified"]))
    return [(t["id"], sets["groups"][t["feature"]]) for t in threads if sets["groups"].get(t["feature"]) in TIERS]


def describe(groups, titles):
    lines = []
    for g in groups:
        lines.append(f"{g['name']}: " + (", ".join(titles.get(s, s[:8]) for s in g["sessionIds"]) or "(trống)"))
    return lines


def main(argv):
    sys.path.insert(0, str(HERE))
    root = HERE.parent.parent
    args = argv[1:]
    if args not in ([], ["--dry-run"], ["--restore"]):
        print("Cách dùng: python Management/tools/vscode_groups.py [--dry-run | --restore]", file=sys.stderr)
        return 2
    try:
        if args == ["--restore"]:
            print(f"Đã khôi phục từ {restore()}. Mở lại VS Code.")
            return 0
        import setup_server
        titles = {t["id"]: t["title"] for t in setup_server.threads()}
        key, groups = apply(root, setup_assignments(), dry_run=bool(args))
    except (ValueError, OSError, sqlite3.Error) as ex:
        print(f"Không gom nhóm: {ex}", file=sys.stderr)
        return 1
    head = "Sẽ ghi" if args else "Đã ghi (sao lưu: " + str(backup_path(vscode_state())) + "). Mở lại VS Code để thấy"
    print(f"{head} nhóm cho {key[len(GROUPS_PREFIX):]}:")
    print("\n".join("- " + line for line in describe(groups, titles)))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
