#!/usr/bin/env python3
"""Tests for vscode_groups.py, on a fake VS Code state database in a temp folder (the real one is never touched).

    python Management/tools/test_vscode_groups.py
"""
import json
import shutil
import sqlite3
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import vscode_groups as vg  # noqa: E402

results = []
ROOT = "/work/SimpleCore"
NOT_RUNNING, RUNNING = (lambda: False), (lambda: True)


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def make_db(folder, data, as_blob=False):
    db = Path(folder) / "state.vscdb"
    con = sqlite3.connect(db)
    with con:
        con.execute("create table ItemTable (key text unique on conflict replace, value blob)")
        con.execute("insert into ItemTable values ('other.key', 'keep me')")
        text = json.dumps(data, separators=(",", ":"), ensure_ascii=False)
        con.execute("insert into ItemTable values (?, ?)", (vg.STATE_KEY, text.encode() if as_blob else text))
    con.close()
    return db


def read(db, key=vg.STATE_KEY):
    con = sqlite3.connect(db)
    try:
        return con.execute("select value from ItemTable where key = ?", (key,)).fetchone()[0]
    finally:
        con.close()


def groups_of(db, ws=ROOT):
    raw = read(db)
    return json.loads(raw if isinstance(raw, str) else raw.decode())[vg.GROUPS_PREFIX + ws]


def refused(fn, *args, **kw):
    try:
        fn(*args, **kw)
        return False
    except ValueError:
        return True


ASSIGN = [("s-human", "Master"), ("s-mgr", "Leaders"), ("s-lead", "Leaders"), ("s-pop", "Services"), ("s-hero", "Features")]


def run_plan():
    out = vg.plan_groups([], ASSIGN)
    check("tier groups in tier order, empty tiers dropped", [g["name"] for g in out],
          ["1 · Master", "2 · Leaders", "4 · Services", "5 · Features"])
    check("sessions in setup order", out[1]["sessionIds"], ["s-mgr", "s-lead"])
    check("new groups are open", all(g["collapsed"] is False for g in out))
    mine = {"id": "g-mine", "name": "Việc riêng", "collapsed": True, "sessionIds": ["s-other", "s-hero"]}
    old = {"id": "g-lead", "name": "2 · Leaders", "collapsed": True, "sessionIds": ["s-lead", "s-gone"]}
    out = vg.plan_groups([mine, old], ASSIGN)
    by = {g["name"]: g for g in out}
    check("an existing tier group keeps its id and collapsed state", (by["2 · Leaders"]["id"], by["2 · Leaders"]["collapsed"]),
          ("g-lead", True))
    check("its order is kept, newcomers appended, sessions no longer in the setup removed", by["2 · Leaders"]["sessionIds"],
          ["s-lead", "s-mgr"])
    check("your own group is kept after the tiers, without the sessions moved into a tier",
          (out[-1]["name"], out[-1]["sessionIds"], out[-1]["collapsed"]), ("Việc riêng", ["s-other"], True))
    check("running the plan again changes nothing", vg.plan_groups(out, ASSIGN), out)


def run_keys():
    data = {"sessionUnread:/work/SimpleCore": [], "sessionGroups:/elsewhere": []}
    check("the workspace key the extension already uses", vg.workspace_key(data, "/work/SimpleCore/"), "/work/SimpleCore")
    check("the project root when the extension has no key yet", vg.workspace_key({}, ROOT), ROOT)
    if vg.WINDOWS:
        check("Windows: any letter case and separator", vg.workspace_key({"sessionUnread:d:\\AI\\X": []}, "D:/AI/X"), "d:\\AI\\X")
    else:
        check("POSIX: letter case matters", vg.workspace_key({"sessionUnread:/work/simplecore": []}, ROOT), ROOT)
    for bad in ({"sessionGroups:/x": {}}, {"sessionGroups:/x": [{"id": "1", "name": "a", "sessionIds": []}]},
                {"sessionGroups:/x": [{"id": "1", "name": "a", "collapsed": False, "sessionIds": [1]}]}, []):
        check(f"an unexpected format is refused: {json.dumps(bad)[:40]}", refused(vg.check_format, bad))
    check("the known format passes", vg.check_format({"sessionGroups:/x": [{"id": "1", "name": "a", "collapsed": False,
                                                                             "sessionIds": ["s"]}]}), None)


def run_apply(folder):
    data = {"hiddenSessionIds": ["s-old"], "sessionUnread:" + ROOT: ["s-hero"], "nested": {"a": [1, 2.5, "ư"]},
            vg.GROUPS_PREFIX + "/other/project": [{"id": "x", "name": "Keep", "collapsed": False, "sessionIds": ["z"]}]}
    db = make_db(folder, data)
    before = read(db)
    key, planned = vg.apply(ROOT, ASSIGN, db=db, dry_run=True, running=RUNNING)
    check("dry run plans without writing, even while VS Code runs", (key, len(planned), read(db)), (vg.GROUPS_PREFIX + ROOT, 4, before))
    check("refused while VS Code runs, nothing written", (refused(vg.apply, ROOT, ASSIGN, db=db, running=RUNNING), read(db)), (True, before))
    vg.apply(ROOT, ASSIGN, db=db, running=NOT_RUNNING)
    after = json.loads(read(db))
    check("groups written for this project", [g["name"] for g in groups_of(db)], ["1 · Master", "2 · Leaders", "4 · Services", "5 · Features"])
    check("every other field unchanged", {k: v for k, v in after.items() if k != vg.GROUPS_PREFIX + ROOT}, data)
    check("written the compact way the extension writes", read(db), json.dumps(after, separators=(",", ":"), ensure_ascii=False))
    check("other rows untouched", read(db, "other.key"), "keep me")
    check("a backup of the state before the write", read(vg.backup_path(db)), before)
    second = read(db)
    vg.apply(ROOT, ASSIGN, db=db, running=NOT_RUNNING)
    check("a second run writes the same groups", read(db), second)
    check("current_groups shows each session's group", vg.current_groups(ROOT, db)["s-lead"], "2 · Leaders")
    check("restore refused while VS Code runs", refused(vg.restore, db, running=RUNNING))
    vg.restore(db, running=NOT_RUNNING)
    check("restore brings back the backup (the state before the last write)", read(db), second)
    (Path(folder) / "blob").mkdir()
    blob = make_db(Path(folder) / "blob", {}, as_blob=True)
    vg.apply(ROOT, ASSIGN, db=blob, running=NOT_RUNNING)
    check("a value stored as bytes stays bytes", isinstance(read(blob), bytes))
    broken = Path(folder) / "broken"
    broken.mkdir()
    bad = make_db(broken, {vg.GROUPS_PREFIX + "/x": "not a list"})
    raw = read(bad)
    check("an unexpected format writes nothing and makes no backup",
          (refused(vg.apply, ROOT, ASSIGN, db=bad, running=NOT_RUNNING), read(bad), vg.backup_path(bad).exists()), (True, raw, False))
    empty = Path(folder) / "empty"
    empty.mkdir()
    con = sqlite3.connect(empty / "state.vscdb")
    with con:
        con.execute("create table ItemTable (key text, value blob)")
    con.close()
    check("no extension key yet: refused", refused(vg.apply, ROOT, ASSIGN, db=empty / "state.vscdb", running=NOT_RUNNING))
    check("no database: refused", refused(vg.apply, ROOT, ASSIGN, db=Path(folder) / "none.vscdb", running=NOT_RUNNING))


def main():
    run_plan()
    run_keys()
    folder = tempfile.mkdtemp(prefix="vsgroups-")
    try:
        run_apply(folder)
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    failed = [x for x in results if not x[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
