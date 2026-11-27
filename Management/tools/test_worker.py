#!/usr/bin/env python3
"""Tests of worker.py on a made-up git project: snapshot, gates, apply (clean, merged, drift), one fix round, ticket.
No claude turn runs: worker.turn is replaced by a function that edits the worktree the way a worker would."""
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / ".claude" / "hooks"))
import worker as w      # noqa: E402
import editor_ticket as et      # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def write(root, rel, text):
    p = Path(root, rel)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")


def make_project(root):
    write(root, ".claude/rules/worker.md", "# Worker rules\n")
    write(root, ".claude/settings.json", json.dumps({"claudeMdExcludes": ["**/x.md"]}))
    write(root, "CLAUDE.md", "# long project rules\n")
    write(root, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1\nline2\nline3\nline4\nline5\n")
    write(root, "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs", "interface IWave {}\n")
    write(root, "UnityProject/Assets/Features/Old/Gone.cs", "old\n")
    write(root, "UnityProject/Assets/Features/Old/Gone.cs.meta", "guid: 1\n")
    write(root, "UnityProject/Assets/Features/Old.meta", "guid: 2\n")
    write(root, "UnityProject/Assets/Features.meta", "guid: 3\n")
    write(root, ".gitignore", "/Management/workers/\n")
    subprocess.run(["git", "init", "-q"], cwd=root, check=True)
    subprocess.run(["git", *w.GIT_ID, "add", "-A"], cwd=root, check=True)
    subprocess.run(["git", *w.GIT_ID, "commit", "-q", "-m", "init"], cwd=root, check=True)


def worker_edits(wt, outside=False, status="done"):
    """What a worker does: edits Run (two lines), changes a contract, adds a test, deletes a file, writes result.json."""
    write(wt, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1 worker\nline2\nline3\nline4\nline5 human\n")
    write(wt, "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs", "interface IWave { void End(); }\n")
    write(wt, "UnityProject/Assets/Features/Run/Tests/RunTests.cs", "class RunTests {}\n")
    Path(wt, "UnityProject/Assets/Features/Old/Gone.cs").unlink(missing_ok=True)
    if outside:
        write(wt, "Management/game-design/live/x.md", "sửa thiết kế\n")
    write(wt, "result.json", json.dumps({"status": status, "summary": "làm Run",
                                         "reqs": [{"feature": "Run", "ref": "run-rules#start", "tests": ["RunTests.Starts"]}]}))


def _refused(fn):
    try:
        fn()
    except ValueError:
        return True
    return False


def main():
    tmp = Path(tempfile.mkdtemp(prefix="worker-test-"))
    root, home = tmp / "main", tmp / "work"
    real = (w.WORK_HOME, w.turn, w.run_gates, w.bind, w.open_requirements, w.editor_busy, w.SLEEP)
    try:
        w.editor_busy = lambda r: None      # never the real guard store of this machine
        root.mkdir()
        make_project(root)
        w.WORK_HOME = home
        w.run_gates = lambda wt: [("validate.py", True, []), ("compile_check.py --all", True, [])]
        w.bind = lambda: "00000000-0000-0000-0000-000000000001"

        # 1. snapshot: the uncommitted state of the main checkout is in the worktree
        write(root, "UnityProject/Assets/Features/Run/Scripts/Internal/New.cs", "chưa commit\n")
        write(root, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1\nline2\nline3\nline4\nline5 human\n")
        job = {"id": "w20261127-100000-run", "feature": "Run", "model": "opus", "effort": "medium", "state": "running",
               "worktree": str(home / "w20261127-100000-run"), "session": "s1"}
        w.save(root, job)
        job["base"] = w.snapshot(root, Path(job["worktree"]))
        wt = Path(job["worktree"])
        check("snapshot: an untracked file of the main checkout is in the worktree", (wt / "UnityProject/Assets/Features/Run/Scripts/Internal/New.cs").is_file())
        check("snapshot: an uncommitted edit is in the worktree",
              (wt / "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs").read_text(encoding="utf-8").endswith("line5 human\n"))
        check("snapshot: CLAUDE.md of the worktree is the worker's rules", (wt / "CLAUDE.md").read_text(encoding="utf-8"), "# Worker rules\n")
        check("snapshot: no rule file loads by itself in the worktree",
              "**/.claude/rules/*.md" in json.loads((wt / ".claude/settings.json").read_text(encoding="utf-8"))["claudeMdExcludes"])
        check("snapshot: nothing changed against base yet", w.changes(wt, job["base"]), {})

        # 2. gates: a file outside Assets fails, and the result is read
        worker_edits(wt, outside=True)
        ok, lines = w.check(root, job)
        check("gate: a file outside UnityProject/Assets fails the job", (ok, any("NGOÀI VÙNG" in x for x in lines)), (False, True))
        Path(wt, "Management/game-design/live/x.md").unlink()
        ok, lines = w.check(root, job)
        check("gate: inside Assets only, result valid: passes", ok)
        write(wt, "result.json", "{not json")
        check("gate: a broken result.json fails", w.check(root, job)[0], False)
        worker_edits(wt)

        # 3a. the Editor in use (a play test): nothing is written until it is free
        busy, slept = ["phiếu t1 (@UnityAutoPlay)", "phiếu t1 (@UnityAutoPlay)"], []
        w.editor_busy = lambda r: busy.pop(0) if busy else None
        w.SLEEP = lambda s: slept.append(
            (root / "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs").read_text(encoding="utf-8"))
        before_wave = (root / "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs").read_text(encoding="utf-8")
        w.editor_busy = lambda r: "khoá Editor của @UnityAutoPlay"
        ok, lines = w.apply(root, job, now=1, wait=0)
        check("apply: the Editor still busy at the end of the wait: nothing written, the job waits",
              (ok, job["state"], (root / "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs").read_text(encoding="utf-8"),
               "CHƯA NẠP" in lines[0]), (False, "apply-waiting", before_wave, True))
        job["state"] = "running"
        w.editor_busy = lambda r: busy.pop(0) if busy else None
        # 3. apply: the main copy still the base copy -> written; the human edited line 5 meanwhile -> merged
        write(root, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1\nline2\nline3\nline4\nline5 human again\n")
        ok, lines = w.apply(root, job, now=1)
        check("apply: it waited while a play test held the Editor and wrote nothing meanwhile", slept, [before_wave, before_wave])
        w.editor_busy = lambda r: None
        run_cs = (root / "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs").read_text(encoding="utf-8")
        check("apply: a file the human changed meanwhile is merged three ways (both edits kept)",
              (ok, run_cs), (True, "line1 worker\nline2\nline3\nline4\nline5 human again\n"))
        check("apply: a contract change and a new test are written",
              ((root / "UnityProject/Assets/Features/Wave/Scripts/Contracts/IWave.cs").read_text(encoding="utf-8"),
               (root / "UnityProject/Assets/Features/Run/Tests/RunTests.cs").is_file()), ("interface IWave { void End(); }\n", True))
        check("apply: a deleted file takes its .meta along",
              ((root / "UnityProject/Assets/Features/Old/Gone.cs").exists(), (root / "UnityProject/Assets/Features/Old/Gone.cs.meta").exists()),
              (False, False))
        check("apply: a folder the deletions left empty goes, with its .meta, up to Assets",
              ((root / "UnityProject/Assets/Features/Old").exists(), (root / "UnityProject/Assets/Features/Old.meta").exists(),
               (root / "UnityProject/Assets/Features").is_dir(), any("thư mục rỗng" in x for x in lines)), (False, False, True, True))
        check("apply: result.json is not written into the main project", (root / "result.json").exists(), False)
        check("apply: the worktree is the new base, nothing left to apply", w.changes(wt, job["base"]), {})

        # 4. drift: both edit the same line -> nothing is applied; resume puts the main copy in .worker-merge
        write(wt, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1 worker 2\nline2\nline3\nline4\nline5 human again\n")
        write(wt, "UnityProject/Assets/Features/Run/Tests/RunTests.cs", "class RunTests { void B() {} }\n")
        write(root, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1 human\nline2\nline3\nline4\nline5 human again\n")
        before = (root / "UnityProject/Assets/Features/Run/Tests/RunTests.cs").read_text(encoding="utf-8")
        ok, lines = w.apply(root, job, now=2)
        check("drift: a conflict applies nothing, not even the files without one",
              (ok, job["state"], (root / "UnityProject/Assets/Features/Run/Tests/RunTests.cs").read_text(encoding="utf-8")),
              (False, "drift", before))
        msg = w.drift_message(root, job)
        check("drift: the main copy is put in .worker-merge for the worker, and named in the message",
              ((wt / w.MERGE_DIR / "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs").read_text(encoding="utf-8").startswith("line1 human"),
               "Run.cs" in msg), (True, True))
        check("drift: .worker-merge is not a change of the worker", any(p.startswith(w.MERGE_DIR) for p in w.changes(wt, job["base"])), False)
        write(wt, "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs", "line1 human worker 2\nline2\nline3\nline4\nline5 human again\n")
        ok, lines = w.apply(root, job, now=3)
        check("drift: after the worker merged, the accepted main copy lets the apply through",
              (ok, (root / "UnityProject/Assets/Features/Run/Scripts/Internal/Run.cs").read_text(encoding="utf-8").splitlines()[0]),
              (True, "line1 human worker 2"))

        # 5. the whole flow: a failing gate is sent back once, then apply and the ticket
        job2 = {"id": "w20261127-110000-run", "feature": "Run", "model": "opus", "effort": "medium", "state": "running",
                "worktree": str(home / "w20261127-110000-run"), "session": "s2"}
        w.save(root, job2)
        job2["base"] = w.snapshot(root, Path(job2["worktree"]))
        wt2 = Path(job2["worktree"])
        gates = iter([[("validate.py", True, []), ("compile_check.py --all", False, ["Game.Run: LỖI", "Run.cs:1: CS1002: ; expected"])],
                      [("validate.py", True, []), ("compile_check.py --all", True, [])]])
        w.run_gates = lambda wt: next(gates)
        prompts = []
        w.turn = lambda r, j, prompt, resume=False: prompts.append((prompt, resume)) or {"result": "ok"}
        write(wt2, "UnityProject/Assets/Features/Run/Scripts/Internal/Fix.cs", "class Fix {}\n")
        write(wt2, "result.json", json.dumps({"status": "done", "summary": "x",
                                              "reqs": [{"feature": "Run", "ref": "run-rules#start", "tests": ["RunTests.Starts"]},
                                                       {"feature": "Wave", "ref": "wave-rules#end", "tests": ["WaveTests.Ends"]}]}))
        code, lines = w.finish(root, job2, {"result": "ok"})
        check("flow: a failed gate is sent back once, with its lines", (len(prompts), prompts[0][1], "CS1002" in prompts[0][0]), (1, True, True))
        ticket = et.find(root, "@Worker")
        check("flow: then applied, and one Editor ticket names each requirement with its feature",
              (code, job2["state"], ticket.get("job"), sorted((r["feature"], r["ref"]) for r in ticket["reqs"])),
              (0, "applied", job2["id"], [("Run", "run-rules#start"), ("Wave", "wave-rules#end")]))
        w.run_gates = lambda wt: [("validate.py", False, ["STR-01 x"]), ("compile_check.py --all", True, [])]
        job2["fixed"] = False
        write(wt2, "UnityProject/Assets/Features/Run/Scripts/Internal/Fix2.cs", "class Fix2 {}\n")
        prompts.clear()
        code, lines = w.finish(root, job2, {"result": "ok"})
        check("flow: still failing after the one fix round: stops, nothing applied",
              (code, job2["state"], len(prompts), (root / "UnityProject/Assets/Features/Run/Scripts/Internal/Fix2.cs").exists()),
              (1, "checks-failed", 1, False))
        check("flow: a report for CodeLead is written for each end", len(list((root / "Management" / "reports").glob("*-Worker-Run*.md"))) >= 2)
        report = sorted((root / "Management" / "reports").glob("*-Worker-Run*.md"))[0].read_text(encoding="utf-8")
        check("flow: the report has the 8 lines of report.md", all(f"- {k}:" in report for k in
              ("Phiên", "Việc", "Trạng thái", "Đã đổi", "Công khai đổi", "Cần bên khác", "Kiểm tra", "Ghi chú")))

        # 6. art: a worker makes Simple; a Detail and the choice of tier are the human's
        art = "UnityProject/Assets/Features/Run/FeatureAssets/Private/Art/Run/coin/"
        check("art: a Simple is applied, a Detail and ArtChoice.json are not",
              (w.in_zone(art + "Simple.png"), w.in_zone(art + "Simple.png.meta"), w.in_zone(art + "Detail.png"),
               w.in_zone(art + "Detail.fbx.meta"), w.in_zone("UnityProject/Assets/_Composition/ArtChoice.json")),
              (True, True, False, False, False))
        job3 = {"id": "w20261127-120000-run", "feature": "Run", "worktree": str(home / "w20261127-120000-run")}
        w.save(root, job3)
        job3["base"] = w.snapshot(root, Path(job3["worktree"]), "Run")

        # 7. a question goes to Manager's list; queue and play jobs
        qid = w.ask_human(root, job3, {"summary": "Run", "question": "A hay B?", "options": "A·B", "suggest": "A"})
        import ask
        q = ask.get(root, qid)
        check("ask: the question is in Manager's list, from @Worker, naming the job", (q["source"], q["waiting"], q["options"]),
              ("@Worker", "worker " + job3["id"], "A·B"))
        w.open_requirements = lambda r, f, refs=None: (None, [])
        check("queue: a job with no open requirement and no task is refused", _refused(lambda: w.queue(root, "Run")))
        jq = w.queue(root, "Run", task="sửa một dòng", now=5)
        check("queue: a queued job waits for the server", (jq["state"], jq["kind"]), ("queued", "code"))
        check("defaults: with no file a job takes opus · medium", (jq["model"], jq["effort"]), ("opus", "medium"))
        w.set_defaults(root, "sonnet", "high")
        jd = w.queue(root, "Run", task="việc theo mặc định của trang", now=5)
        check("defaults: a job that names no model takes the page's default", (jd["model"], jd["effort"]), ("sonnet", "high"))
        jn = w.queue(root, "Run", task="việc ghi model riêng", model="opus", now=5)
        check("defaults: a model named on the job wins, the effort comes from the default", (jn["model"], jn["effort"]), ("opus", "high"))
        check("defaults: a model outside the list is refused", _refused(lambda: w.set_defaults(root, "gpt", "high")))
        for j in (jd, jn):
            w.drop(root, j["id"])
            shutil.rmtree(w.jobs_dir(root) / j["id"], ignore_errors=True)
        (w.jobs_dir(root) / w.DEFAULTS).unlink()
        jp = w.queue_play(root, "chơi thử đợt 1", source="@CodeLead", now=6)
        t = et.find(root, "@UnityAutoPlay", job=jp["id"])
        check("play: a play job waits for an Editor ticket of its own", (jp["state"], jp["kind"], t["tasks"]), ("waiting-editor", "play", ["chơi thử"]))
        g = et.grant_next(root, now=7)
        check("play: the play ticket is granted on its own", (g[0], g[1][0]["job"]), ("own", jp["id"]))
        out = w.end_play_ticket(root, jp["id"])
        check("play: after the turn the Editor is free and nothing is queued again (no second play turn)",
              (len(out["released"]), out["requeued"], [x for x in et.queue(root) if x.get("job") == jp["id"]], et.holder(root)),
              (1, [], [], []))
        # the real order of a turn: the Stop hook runs inside claude, before worker.py ends the play ticket
        import stop_unlock
        jq2 = w.queue_play(root, "chơi thử đợt 2", source="@CodeLead", now=8)
        held = et.grant_next(root, now=9)[1][0]
        held["used"] = True
        et.save(root, held)
        real_who, stop_unlock.guard.session_feature = stop_unlock.guard.session_feature, lambda s, store: w.PLAY_ROLE
        try:
            early = stop_unlock.release_ticket("play-session", str(tmp), root=str(root))
        finally:
            stop_unlock.guard.session_feature = real_who
        w.end_play_ticket(root, jq2["id"])
        check("play: the Stop hook leaves a job's ticket to worker.py, so the play is not queued again",
              (early, [x for x in et.queue(root) if x.get("job") == jq2["id"]], et.holder(root)), (False, [], []))
        w.drop(root, jq2["id"])
        shutil.rmtree(w.jobs_dir(root) / jq2["id"], ignore_errors=True)
        check("status: a line per job", len(w.status_lines(root)), 5)
        check("main: a wrong command is refused", w.main(["worker.py", "nope"]), 2)
        check("main: a bad job id is refused", w.main(["worker.py", "status", "../x"]), 2)
    finally:
        (w.WORK_HOME, w.turn, w.run_gates, w.bind, w.open_requirements, w.editor_busy, w.SLEEP) = real
        subprocess.run(["git", "worktree", "prune"], cwd=root, capture_output=True)
        shutil.rmtree(tmp, ignore_errors=True)
    failed = RESULTS.count(False)
    print(f"TOTAL: {RESULTS.count(True)} ok, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
