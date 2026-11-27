#!/usr/bin/env python3
"""Workers (người quyết 2026-11-27): one big job, done alone by one headless claude turn in its own git worktree,
instead of many feature sessions messaging each other. The architecture is checked on the result, not by who may
edit what: the worker edits all of UnityProject/Assets; worker.py takes back only Assets files, after the gates.

    python Management/tools/worker.py run <F> [--reqs <ref>,...] [--task "<việc thêm>"] [--model opus] [--effort medium]
    python Management/tools/worker.py resume <id> ["<lời nhắn>"]     the same session goes on (after a stop, a test failed)
    python Management/tools/worker.py check <id>                     the gates again, nothing applied
    python Management/tools/worker.py apply <id>                     gates, then take the work into the main project
    python Management/tools/worker.py status [<id>]
    python Management/tools/worker.py drop <id>                      remove the worktree (the job's folder stays)

run:
  1. snapshot: `git worktree add <home>/<id> HEAD --detach`, then everything the main checkout has not committed
     (changed, deleted, untracked; not ignored) is copied in, so the worker sees what the human sees. CLAUDE.md of
     the worktree is .claude/rules/worker.md and the rule files are not loaded: a short context. Committed as `base`.
  2. one turn: `claude -p` bound to @Worker in the worktree, no MCP server, no skill. The packet (the open
     requirements of <F>, the task) is the prompt; the worker writes result.json at the worktree's root.
  3. gates, in the worktree: every changed file inside UnityProject/Assets (not SimpleCoreTools); validate.py with
     no violation; compile_check.py --all clean. A failed gate is sent back once (resume with the lines).
  4. apply (automatic, người quyết): each changed file whose main copy is still the base copy is written; one that
     changed since is merged three ways (git merge-file); a conflict applies nothing (state drift). The worktree is
     then committed again as the new base, so a later apply takes only what came after.
  5. one Editor ticket (session @Worker, the job's id, each requirement with its feature and tests): the server
     loads, compiles, runs the tests at the end of the round and writes done; what it says goes to events.log.
     A requirement seen only in play is written done by this script after the apply.

Files: Management/workers/<id>/ job.json, result.json, checks.txt, applied.json, events.log, cost.json (not
committed). Worktrees: ~/.coresimple-work/<id> (CORESIMPLE_WORK to move them).
Exit codes: 0 ok, 1 the job stopped (gates, drift, blocked), 2 refused.
"""
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import uuid
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / ".claude" / "hooks"))
import reqbook  # noqa: E402

USAGE = __doc__.split("\n\n")[1]
ROLE = "@Worker"
JOBS_DIR = ("Management", "workers")
WORK_HOME = Path(os.environ.get("CORESIMPLE_WORK") or Path.home() / ".coresimple-work")
ID_RX = re.compile(r"^w\d{8}-\d{6}-[a-z0-9-]+$")
APPLY_ZONE = "UnityProject/Assets/"
NOT_APPLIED = ("UnityProject/Assets/SimpleCoreTools/",)
RESULT = "result.json"
MERGE_DIR = ".worker-merge"      # in the worktree: the main project's copy of each file that drifted, to merge from
WORK_STATES = ("rejected", "changed", "no-test", "doing", "todo")      # the book's states that are work now
TURN_SECONDS = 3 * 3600
DEFAULT_MODEL, DEFAULT_EFFORT = "opus", "medium"
GIT_ID = ["-c", "user.name=worker", "-c", "user.email=worker@localhost", "-c", "commit.gpgsign=false"]
# The tools a worker does not need; the guard refuses them anyway (AskUserQuestion, SendMessage, unity-mcp).
OFF_TOOLS = "Artifact,Workflow,ScheduleWakeup,ReportFindings,AskUserQuestion,SendMessage,EnterWorktree,EnterPlanMode"


# ---------------------------------------------------------------- small helpers
def git(cwd, *args, check=True, data=None):
    p = subprocess.run(["git", *GIT_ID, *args], cwd=str(cwd), capture_output=True, input=data,
                       timeout=600)
    if check and p.returncode != 0:
        raise RuntimeError(f"git {' '.join(args[:3])}: " + (p.stderr or p.stdout).decode("utf-8", "replace").strip()[:300])
    return p


def out(cwd, *args):
    return git(cwd, *args).stdout.decode("utf-8", "replace")


def jobs_dir(root):
    return Path(root).joinpath(*JOBS_DIR)


def job_dir(root, job_id):
    if not ID_RX.match(job_id or ""):
        raise ValueError(f"mã việc không hợp lệ: '{job_id}'")
    return jobs_dir(root) / job_id


def load(root, job_id):
    p = job_dir(root, job_id) / "job.json"
    if not p.is_file():
        raise ValueError(f"không có việc {job_id}")
    return json.loads(p.read_text(encoding="utf-8"))


def save(root, job):
    d = job_dir(root, job["id"])
    d.mkdir(parents=True, exist_ok=True)
    job["updated"] = time.time()
    tmp = d / "job.json.tmp"
    tmp.write_text(json.dumps(job, ensure_ascii=False, indent=1), encoding="utf-8")
    os.replace(tmp, d / "job.json")
    return job


def log(root, job, text):
    with open(job_dir(root, job["id"]) / "events.log", "a", encoding="utf-8") as f:
        f.write(json.dumps({"at": time.time(), "key": "worker", "text": text}, ensure_ascii=False) + "\n")


def new_id(feature, now=None):
    slug = re.sub(r"[^a-z0-9]+", "-", feature.lower()).strip("-") or "job"
    return "w" + time.strftime("%Y%m%d-%H%M%S", time.localtime(now or time.time())) + "-" + slug


def in_zone(path):
    return path.startswith(APPLY_ZONE) and not path.startswith(NOT_APPLIED)


def z_paths(text):
    return [p for p in text.split("\0") if p]


# ---------------------------------------------------------------- 1. snapshot
def uncommitted(root):
    """(changed or untracked paths, deleted paths) of the main checkout, relative, '/' separators; ignored files out."""
    changed, deleted = set(), set()
    for line in z_paths(out(root, "status", "--porcelain=v1", "-z", "--untracked-files=all", "--no-renames")):
        code, path = line[:2], line[3:]
        if "D" in code:
            deleted.add(path)
        else:
            changed.add(path)
    return sorted(changed), sorted(deleted)


def worker_context(root, wt):
    """The worktree's context: CLAUDE.md is the worker's rules, no rule file loads by itself."""
    rules = Path(root, ".claude", "rules", "worker.md").read_text(encoding="utf-8")
    (wt / "CLAUDE.md").write_text(rules, encoding="utf-8")
    settings = wt / ".claude" / "settings.json"
    if settings.is_file():
        data = json.loads(settings.read_text(encoding="utf-8"))
        data["claudeMdExcludes"] = sorted(set(data.get("claudeMdExcludes") or []) | {"**/.claude/rules/*.md"})
        settings.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def studio_folders(root, feature):
    """The feature's studio orders and the files delivered for them (images, models), ignored files included: the
    worker reads them to judge what was delivered and to see what is already ordered."""
    found = []
    if not feature:
        return found
    for tool in sorted(Path(root, "Management", "studio").glob("*")):
        d = tool / "orders" / feature
        if d.is_dir():
            found.append(d.relative_to(root))
    d = Path(root, "Resources", feature)
    if d.is_dir():
        found.append(d.relative_to(root))
    return found


def snapshot(root, wt, feature=None):
    """A worktree at HEAD with the main checkout's uncommitted state on top, committed as `base`. Returns its sha."""
    wt.parent.mkdir(parents=True, exist_ok=True)
    git(root, "worktree", "add", "--detach", str(wt), "HEAD")
    changed, deleted = uncommitted(root)
    for rel in changed:
        src, dst = Path(root, rel), wt / rel
        if src.is_file() and not src.is_symlink():
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
    for rel in deleted:
        try:
            (wt / rel).unlink()
        except FileNotFoundError:
            pass
    for rel in studio_folders(root, feature):
        shutil.copytree(Path(root, rel), wt / rel, dirs_exist_ok=True, ignore=shutil.ignore_patterns(".run.lock"))
    worker_context(root, wt)
    return commit(wt, "base: ảnh chụp project chính")


def commit(wt, message):
    git(wt, "add", "-A")
    git(wt, "commit", "-q", "--no-verify", "--allow-empty", "-m", message)
    return out(wt, "rev-parse", "HEAD").strip()


# ---------------------------------------------------------------- 2. the packet and the turn
def open_requirements(root, feature, refs=None):
    """The requirements of <feature> that are work now (or the ones named), as lines for the packet."""
    try:
        v = reqbook.view(root, feature)
    except ValueError:      # no book (Core, a folder live has no node for): only the task of the job
        return None, []
    wanted = {r.strip() for r in (refs or []) if r.strip()}
    lines = []
    for n in v["nodes"]:
        for r in n["reqs"]:
            if wanted:
                if r["ref"] not in wanted and r["ref"].split("#")[-1] not in wanted and not any(r["ref"].endswith(w) for w in wanted):
                    continue
            elif r["state"] not in WORK_STATES or r.get("later"):
                continue
            lines.append("\n".join(reqbook.req_line(r)))
    return v, lines


def packet(root, job, lines):
    feature = job["feature"]
    reqs = "\n".join(lines) if lines else "(không có yêu cầu mở: chỉ làm việc thêm bên dưới)"
    extra = f"\n\nViệc thêm của người: {job['task']}" if job.get("task") else ""
    return (f"Việc của bạn (worker {job['id']}): làm hết các yêu cầu đang mở của {feature} dưới đây, kể cả khi phải sửa "
            f"feature khác, contract, Core hay _Composition. Luật: CLAUDE.md của worktree (đã nạp).\n\n"
            f"Yêu cầu ({len(lines)}):\n{reqs}{extra}\n\n"
            f"Bắt đầu: python Management/tools/gd_pack.py {feature} (gói thiết kế), python Management/tools/gd_req.py {feature} "
            "(sổ, chỉ đọc), UnityProject/Assets/Features/" + feature + "/CLAUDE.md nếu có. Xong: compile_check.py --all sạch, "
            f"validate.py 0 vi phạm, rồi ghi {RESULT} ở gốc worktree theo khuôn trong CLAUDE.md và kết thúc.")


def claude_args(job, prompt, resume=False):
    import setup_server      # its CLI helpers; importing it starts nothing
    folder = jobs_dir(ROOT) / ".profile"
    folder.mkdir(parents=True, exist_ok=True)
    settings, mcp = folder / "worker.settings.json", folder / "worker.mcp.json"
    settings.write_text(json.dumps(setup_server.role_settings(ROLE), ensure_ascii=False, indent=1), encoding="utf-8")
    mcp.write_text(json.dumps({"mcpServers": {}}), encoding="utf-8")
    head = ["--resume", job["session"]] if resume else ["--session-id", job["session"]]
    args = (["--no-chrome", "-p"] + head + ["--model", job["model"], "--effort", job["effort"], "--output-format", "json",
            "--settings", str(settings), "--disallowedTools", OFF_TOOLS, "--strict-mcp-config", "--mcp-config", str(mcp),
            "--", prompt])
    return setup_server.claude_command(args)


def run_turn(job, prompt, resume=False):
    """One headless turn of the worker in its worktree. Returns the CLI's JSON (result, usage, total_cost_usd)."""
    import setup_server
    command, shell = claude_args(job, prompt, resume)
    env = dict(os.environ, **setup_server.CLAUDE_ENV, WORKER_MAIN=str(ROOT))
    p = subprocess.run(command, cwd=job["worktree"], capture_output=True, text=True, encoding="utf-8", errors="replace",
                       env=env, timeout=TURN_SECONDS, shell=shell)
    text = (p.stdout or "").strip()
    try:
        return json.loads(text.splitlines()[-1]) if text else {"is_error": True, "result": (p.stderr or "")[-500:]}
    except ValueError:
        return {"is_error": True, "result": text[-500:] or (p.stderr or "")[-500:]}


def bind():
    import bind_session
    import guard
    sid = str(uuid.uuid4())
    bind_session.write_json(guard.binding_path(sid, guard.STORE_DIR),
                            {"feature": ROLE, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S"), "by": "worker.py"})
    return sid


def turn(root, job, prompt, resume=False):
    started = time.time()
    try:
        answer = run_turn(job, prompt, resume)
    except subprocess.TimeoutExpired:
        answer = {"is_error": True, "result": f"hết giờ ({TURN_SECONDS // 60} phút)"}
    usage = answer.get("usage") or {}
    job.setdefault("turns", []).append({"at": started, "seconds": round(time.time() - started), "resume": resume,
                                        "cost_usd": answer.get("total_cost_usd"), "usage": usage,
                                        "num_turns": answer.get("num_turns"), "error": bool(answer.get("is_error"))})
    (job_dir(root, job["id"]) / "cost.json").write_text(json.dumps(job["turns"], ensure_ascii=False, indent=1), encoding="utf-8")
    save(root, job)
    return answer


# ---------------------------------------------------------------- 3. gates
def changes(wt, base):
    """{path: 'M'|'A'|'D'} of the worktree against `base` (the working tree, commits of the worker included)."""
    found = {}
    lines = z_paths(out(wt, "diff", "--name-status", "-z", "--no-renames", base))
    for code, path in zip(lines[0::2], lines[1::2]):
        found[path] = code[0]
    for path in z_paths(out(wt, "ls-files", "--others", "--exclude-standard", "-z")):
        found[path] = "A"
    found.pop(RESULT, None)
    return {p: c for p, c in found.items() if not p.startswith(MERGE_DIR + "/")}


def run_gates(wt):
    """[(gate, ok, lines)] of validate.py and compile_check.py --all, run in the worktree."""
    env = dict(os.environ, WORKER_MAIN=str(ROOT), PYTHONIOENCODING="utf-8")
    found = []
    for gate, cmd in (("validate.py", [sys.executable, "Management/tools/validate.py"]),
                      ("compile_check.py --all", [sys.executable, "Management/tools/compile_check.py", "--all", "--quiet"])):
        try:
            p = subprocess.run(cmd, cwd=str(wt), capture_output=True, text=True, encoding="utf-8", errors="replace",
                               env=env, timeout=1800)
            lines = [x for x in (p.stdout + p.stderr).splitlines() if x.strip()]
            found.append((gate, p.returncode == 0, lines[-40:]))
        except (OSError, subprocess.SubprocessError) as ex:
            found.append((gate, False, [f"không chạy được: {ex}"]))
    return found


def read_result(wt):
    """(result, problem): the worker's result.json, checked."""
    p = Path(wt) / RESULT
    if not p.is_file():
        return None, f"thiếu {RESULT} ở gốc worktree"
    try:
        r = json.loads(p.read_text(encoding="utf-8"))
    except ValueError as ex:
        return None, f"{RESULT} không phải JSON hợp lệ: {ex}"
    if not isinstance(r, dict) or r.get("status") not in ("done", "partial", "blocked"):
        return None, f"{RESULT}: status phải là done, partial hoặc blocked"
    reqs = r.get("reqs") or []
    if not isinstance(reqs, list) or not all(isinstance(x, dict) and x.get("ref") and x.get("feature") for x in reqs):
        return None, f"{RESULT}: mỗi mục reqs cần feature và ref"
    return r, None


def check(root, job):
    """Runs every gate. Returns (ok, lines) and writes checks.txt."""
    wt = Path(job["worktree"])
    lines, ok = [], True
    result, problem = read_result(wt)
    if problem:
        ok = False
        lines.append(f"KẾT QUẢ: {problem}")
    found = changes(wt, job["base"])
    outside = sorted(p for p in found if not in_zone(p))
    if outside:
        ok = False
        lines.append("NGOÀI VÙNG (worker chỉ sửa UnityProject/Assets, trừ SimpleCoreTools): " + ", ".join(outside[:30]))
    for gate, passed, gate_lines in run_gates(wt):
        lines.append(f"{gate}: {'qua' if passed else 'HỎNG'}")
        if not passed:
            ok = False
            lines += ["  " + x for x in gate_lines]
    lines.insert(0, f"{len(found)} file đổi, {'qua mọi cổng' if ok else 'chưa qua'}")
    (job_dir(root, job["id"]) / "checks.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    job["checked"] = {"at": time.time(), "ok": ok, "files": len(found)}
    if result:
        (job_dir(root, job["id"]) / RESULT).write_text(json.dumps(result, ensure_ascii=False, indent=1), encoding="utf-8")
        job["result"] = {k: result.get(k) for k in ("status", "summary", "question")}
    save(root, job)
    return ok, lines


# ---------------------------------------------------------------- 4. apply
def blob(wt, rev, path):
    p = git(wt, "show", f"{rev}:{path}", check=False)
    return p.stdout if p.returncode == 0 else None


def read_bytes(path):
    try:
        return Path(path).read_bytes()
    except (FileNotFoundError, IsADirectoryError):
        return None


def digest(data):
    return "" if data is None else hashlib.sha1(data).hexdigest()


def merge3(mine, base, theirs):
    """git merge-file: (merged bytes, clean) of the main copy, the base and the worker's."""
    with tempfile.TemporaryDirectory(prefix="worker-merge-") as d:
        paths = []
        for name, data in (("main", mine), ("base", base), ("worker", theirs)):
            p = Path(d, name)
            p.write_bytes(data)
            paths.append(str(p))
        r = subprocess.run(["git", "merge-file", "-p", *paths], capture_output=True, timeout=60)
        return r.stdout, r.returncode == 0


def plan_apply(root, job):
    """([(path, bytes or None)], conflicts, merged): what applying writes; nothing is written here."""
    wt = Path(job["worktree"])
    writes, conflicts, merged = [], [], []
    for path, code in sorted(changes(wt, job["base"]).items()):
        if not in_zone(path):
            continue
        base = blob(wt, job["base"], path)
        new = None if code == "D" else read_bytes(wt / path)
        main = read_bytes(Path(root, path))
        accepted = (job.get("accept") or {}).get(path)
        if main == base or main == new or (accepted is not None and digest(main) == accepted):
            writes.append((path, new))
        elif main is not None and base is not None and new is not None:
            data, clean = merge3(main, base, new)
            if clean:
                writes.append((path, data))
                merged.append(path)
            else:
                conflicts.append(path)
        else:
            conflicts.append(path)
    for path, data in list(writes):      # a deleted asset takes its .meta along
        meta = path + ".meta"
        if data is None and not path.endswith(".meta") and not any(p == meta for p, _ in writes) and Path(root, meta).is_file():
            writes.append((meta, None))
    return writes, conflicts, merged


def apply(root, job, now=None):
    """Takes the worker's Assets files into the main project, all or nothing. Returns (ok, lines)."""
    writes, conflicts, merged = plan_apply(root, job)
    if conflicts:
        job["state"] = "drift"
        save(root, job)
        return False, ["LỆCH: file ở project chính đã đổi từ lúc chụp và không trộn được: " + ", ".join(conflicts),
                       f"Chạy lại trên ảnh mới: worker.py resume {job['id']} (worker trộn lại), hoặc sửa tay rồi worker.py apply {job['id']}."]
    for path, data in writes:
        target = Path(root, path)
        if data is None:
            try:
                target.unlink()
            except FileNotFoundError:
                pass
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    stamp = now or time.time()
    entry = {"at": stamp, "written": [p for p, d in writes if d is not None], "deleted": [p for p, d in writes if d is None],
             "merged": merged}
    applied = job_dir(root, job["id"]) / "applied.json"
    history = json.loads(applied.read_text(encoding="utf-8")) if applied.is_file() else []
    applied.write_text(json.dumps(history + [entry], ensure_ascii=False, indent=1), encoding="utf-8")
    job["base"] = commit(Path(job["worktree"]), f"applied {time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(stamp))}")
    job["state"] = "applied"
    job.setdefault("applied", []).append({"at": stamp, "files": len(writes)})
    job["applied_files"] = sorted(set(job.get("applied_files") or []) | {p for p, _ in writes})
    save(root, job)
    return True, [f"Đã nạp {len(entry['written'])} file, xoá {len(entry['deleted'])}" + (f", trộn {len(merged)}" if merged else "")]


# ---------------------------------------------------------------- 5. after the apply: book and Editor
def after_apply(root, job, result):
    """The requirements of the result: a ticket for the server's load and test run; play ones written done now."""
    import editor_ticket
    lines, tested = [], []
    for r in result.get("reqs") or []:
        if r.get("tests"):
            tested.append({"ref": r["ref"], "tests": list(r["tests"]), "feature": r["feature"]})
        elif r.get("play"):
            try:
                lines += reqbook.mark(root, r["feature"], r["ref"], "done", r["feature"], play=str(r["play"]))
            except ValueError as ex:
                lines.append(f"không ghi được done (play) {r['ref']}: {ex}")
    if tested or writes_code(job):
        t = editor_ticket.add(root, ROLE, ["refresh", "clean"], [], tested, job=job["id"])
        job["ticket"] = t["id"]
        lines.append(f"Phiếu Editor {t['id']}: server nạp, biên dịch" + (f", chạy test của {len(tested)} yêu cầu" if tested else ""))
    lines += studio_actions(root, job, result)
    if result.get("editor"):
        lines.append("Việc cần Editor (chưa tự động ở bước này): " + "; ".join(map(str, result["editor"])))
    save(root, job)
    return lines


STUDIO_TOOLS = ("claudeimage", "codex", "blender")
ORDER_ID_RX = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
OWNER_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")


def job_features(job, result):
    """The features a job may order for or judge: its own, those of its requirements and those whose files it wrote."""
    names = {job["feature"]} | {r.get("feature") for r in result.get("reqs") or []}
    for a in job.get("applied_files") or []:
        m = re.match(r"^UnityProject/Assets/Features/((?:_Services/)?[A-Za-z][A-Za-z0-9]*)/", a)
        if m:
            names.add(m.group(1))
    return {n for n in names if n}


def studio_run(root, *args):
    p = subprocess.run([sys.executable, str(Path(root, "Management", "tools", "studio.py")), *args], cwd=str(root),
                       capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=600)
    text = [x for x in (p.stdout + p.stderr).splitlines() if x.strip()]
    return p.returncode == 0, text[-6:]


def studio_actions(root, job, result):
    """The worker's orders and judgements (result.json orders, reviews), run in the main project after the apply:
    only for the features of the job. The studio's own rules (one order.md, written once; review 0-3) still hold."""
    lines, owners = [], job_features(job, result)
    made = 0
    for o in result.get("orders") or []:
        tool, oid, feature = str(o.get("tool") or ""), str(o.get("id") or ""), str(o.get("feature") or job["feature"])
        if tool not in STUDIO_TOOLS or not ORDER_ID_RX.match(oid) or not OWNER_RX.match(feature) or feature not in owners:
            lines.append(f"order bỏ qua (tool, id hay feature sai, hoặc không thuộc việc này): {tool} {feature}/{oid}")
            continue
        path = Path(root, "Management", "studio", tool, "orders", *feature.split("/"), oid, "order.md")
        if path.exists():
            lines.append(f"order {feature}/{oid} đã có, không ghi đè (đặt order mới với id khác)")
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(str(o.get("order") or "").rstrip() + "\n", encoding="utf-8")
        made += 1
        lines.append(f"Đã đặt order {tool} {feature}/{oid}")
    if made:
        ok, text = studio_run(root, "check")
        lines.append("studio.py check: " + ("qua, order vào hộp thư của tool" if ok else "LỖI: " + " / ".join(text)))
    for r in result.get("reviews") or []:
        order, item = str(r.get("order") or ""), str(r.get("item") or "")
        feature = order.rpartition("/")[0]
        try:
            score = int(r.get("score"))
        except (TypeError, ValueError):
            score = -1
        if feature not in owners or not 0 <= score <= 3 or not item:
            lines.append(f"chấm bỏ qua (order không thuộc việc này, hay mức ngoài 0-3): {order} {item}")
            continue
        ok, text = studio_run(root, "review", order, item, str(score), str(r.get("feedback") or "")[:600])
        lines.append(f"Chấm {order} {item}: {score}" + ("" if ok else " · LỖI: " + " / ".join(text)))
    return lines


# ---------------------------------------------------------------- reports and questions (the server's, for CodeLead)
def write_report(root, job, state, lines):
    """One report in Management/reports, the 8 lines of report.md: CodeLead sees it at the head of its next message."""
    import stop_report
    result = job.get("result") or {}
    full = {}
    try:
        full = json.loads((job_dir(root, job["id"]) / RESULT).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        pass
    refs = ", ".join(r.get("ref", "") for r in full.get("reqs") or [])
    files = job.get("applied_files") or []
    need = "; ".join(([str(full["question"])] if full.get("question") else []) + [f"Editor: {x}" for x in full.get("editor") or []])
    cost = sum(t.get("cost_usd") or 0 for t in job.get("turns") or [])
    body = "\n".join([
        "## Báo cáo cho @Manager",
        f"- Phiên: FEATURE=@Worker ({job['feature']}, việc {job['id']})",
        f"- Việc: {result.get('summary') or job.get('task') or 'việc worker'}" + (f" · yêu cầu: {refs}" if refs else ""),
        f"- Trạng thái: {state}",
        f"- Đã đổi: {', '.join(files[:25]) + (' …' if len(files) > 25 else '') if files else 'không'}",
        f"- Công khai đổi: {'; '.join(map(str, full.get('contracts') or [])) or 'không'}",
        f"- Cần bên khác: {need or 'không'}",
        f"- Kiểm tra: {' · '.join(x for x in lines if x and not x.startswith('  '))[:600] or 'không'}",
        f"- Ghi chú: {len(job.get('turns') or [])} lượt, ${cost:.2f}; chi tiết Management/workers/{job['id']}/",
    ])
    return stop_report.save(str(root), "Worker-" + job["feature"].replace("/", "-"), body)


def ask_human(root, job, result):
    """A question of the worker goes to @Manager's list (ask.py); the server resumes the job with the answer."""
    import ask
    q = ask.add(root, ROLE, f"worker {job['id']} ({job['feature']}): {str(result.get('summary') or '')[:200]}",
                str(result.get("question") or "")[:1500], str(result.get("options") or "xem câu hỏi")[:600],
                str(result.get("suggest") or "chưa có đề nghị")[:600], f"worker {job['id']}")
    job.setdefault("asks", [])
    if q["id"] not in job["asks"]:
        job["asks"].append(q["id"])
    save(root, job)
    return q["id"]


def writes_code(job):
    return any(a.get("files") for a in job.get("applied") or [])


# ---------------------------------------------------------------- the flow
def finish(root, job, answer):
    """Gates (one fix round), apply, ticket. Returns (exit code, lines)."""
    lines = []
    if answer.get("is_error"):
        lines.append("Lượt worker lỗi: " + str(answer.get("result") or "")[:300])
    ok, gate_lines = check(root, job)
    if not ok and not job.get("fixed"):
        job["fixed"] = True
        save(root, job)
        log(root, job, "cổng kiểm hỏng, gửi lại một lần")
        answer = turn(root, job, "Cổng kiểm của worker.py chưa qua. Sửa đúng các lỗi sau rồi ghi lại result.json:\n"
                      + "\n".join(gate_lines[:80]), resume=True)
        ok, gate_lines = check(root, job)
    lines += gate_lines
    if not ok:
        job["state"] = "checks-failed"
        save(root, job)
        write_report(root, job, "bị chặn", lines)
        return 1, lines + [f"Dừng: xem Management/workers/{job['id']}/checks.txt; worker.py resume {job['id']} \"<lời nhắn>\" để sửa tiếp."]
    result, _ = read_result(Path(job["worktree"]))
    done, apply_lines = apply(root, job)
    lines += apply_lines
    if not done:
        return 1, lines
    lines += after_apply(root, job, result)
    if result.get("question") and result["status"] != "done":
        lines.append(f"Câu hỏi cho người: {ask_human(root, job, result)} (Manager hỏi; có trả lời thì server chạy tiếp việc)")
    if result["status"] != "done":
        job["state"] = "applied-" + result["status"]
        save(root, job)
        write_report(root, job, "bị chặn" if result["status"] == "blocked" else "dở", lines)
        lines.append(f"Worker báo {result['status']}: {result.get('question') or result.get('summary') or ''}")
        return 1 if result["status"] == "blocked" else 0, lines
    write_report(root, job, "xong (chờ lô test của server)" if job.get("ticket") else "xong", lines)
    return 0, lines


def queue(root, feature, refs=None, task=None, model=DEFAULT_MODEL, effort=DEFAULT_EFFORT, now=None, kind="code",
          messages=(), source=""):
    """A job waiting for a free worker slot (the server starts it: start()). kind: code | play."""
    if kind == "code" and not task:
        lines = open_requirements(root, feature, refs)[1]
        if not lines:
            raise ValueError(f"{feature} không có yêu cầu mở nào và không có việc thêm")
    job_id = new_id(feature, now)
    while (jobs_dir(root) / job_id).exists():      # two in the same second
        job_id += "x"
    job = {"id": job_id, "feature": feature, "kind": kind, "refs": list(refs or []), "task": task or "",
           "model": model, "effort": effort, "state": "queued", "created": now or time.time(), "messages": list(messages),
           "source": source}
    return save(root, job)


def start(root, job_id):
    """Runs a queued code job to its end: snapshot, the turn, gates, apply, ticket. Returns (exit code, lines)."""
    job = load(root, job_id)
    lines = open_requirements(root, job["feature"], job.get("refs"))[1]
    job["requirements"] = len(lines)
    job["worktree"] = str(WORK_HOME / job["id"])
    job["state"] = "starting"
    save(root, job)
    job["base"] = snapshot(root, Path(job["worktree"]), job["feature"])
    job["session"] = bind()
    job["state"] = "running"
    save(root, job)
    answer = turn(root, job, packet(root, job, lines))
    return finish(root, job, answer)


def run(root, feature, refs=None, task=None, model=DEFAULT_MODEL, effort=DEFAULT_EFFORT, now=None):
    return start(root, queue(root, feature, refs, task, model, effort, now)["id"])


# ---------------------------------------------------------------- play jobs: the tester as a worker
PLAY_ROLE = "@UnityAutoPlay"
PLAY_MODEL = ("sonnet", "medium")
PLAY_TASK = "chơi thử"


def queue_play(root, task, source="", messages=(), now=None):
    """A play test: it needs the Editor, so it waits for an Editor ticket of its own; the server starts it on the grant."""
    import editor_ticket
    job = queue(root, "UnityAutoPlay", task=task, model=PLAY_MODEL[0], effort=PLAY_MODEL[1], now=now, kind="play",
                messages=messages, source=source)
    t = editor_ticket.add(root, PLAY_ROLE, [], [PLAY_TASK], now=now, job=job["id"])
    job["ticket"], job["state"] = t["id"], "waiting-editor"
    return save(root, job)


def play_prompt(job):
    return ("Việc chơi thử của bạn (việc " + job["id"] + ", server đã cấp Unity Editor cho lượt này): " + job["task"] +
            "\n\nLàm theo skill tester-playtest-session. Kết thúc lượt bằng báo cáo theo .claude/rules/report.md (server lưu nó "
            "cho CodeLead). Không chờ tin trả lời: lượt này kết thúc là xong việc.")


def end_play_ticket(root, job_id):
    """Gives the Editor back after a play turn, its ticket emptied first: the turn is the whole job (played or failed),
    so nothing is left for release() to queue again; a ticket left with its task bit would start the job once more."""
    import editor_ticket
    try:
        t = editor_ticket.find(root, PLAY_ROLE, ("granted",), job_id)
    except ValueError:
        t = None
    if t is not None:
        t["code"] = 0
        editor_ticket.save(root, t)
    return editor_ticket.release(root, PLAY_ROLE)


def start_play(root, job_id):
    """The play turn, in the main project (the Editor is there), bound to the tester role; the Editor is given back after."""
    import bind_session
    import editor_ticket
    import guard
    import setup_server
    job = load(root, job_id)
    sid = str(uuid.uuid4())
    bind_session.write_json(guard.binding_path(sid, guard.STORE_DIR),
                            {"feature": PLAY_ROLE, "bound_at": time.strftime("%Y-%m-%d %H:%M:%S"), "by": "worker.py"})
    job.update(session=sid, state="running", worktree=str(root))
    save(root, job)
    command, shell = setup_server.claude_command(
        ["--no-chrome", "-p", "--session-id", sid, "--model", job["model"], "--effort", job["effort"], "--output-format", "json"]
        + setup_server.profile_args(PLAY_ROLE) + ["--", play_prompt(job)])
    started = time.time()
    try:
        p = subprocess.run(command, cwd=str(root), capture_output=True, text=True, encoding="utf-8", errors="replace",
                           env=dict(os.environ, **setup_server.CLAUDE_ENV), timeout=TURN_SECONDS, shell=shell)
        text = (p.stdout or "").strip()
        answer = json.loads(text.splitlines()[-1]) if text else {"is_error": True, "result": (p.stderr or "")[-300:]}
    except (subprocess.TimeoutExpired, ValueError) as ex:
        answer = {"is_error": True, "result": str(ex)[:300]}
    finally:
        end_play_ticket(root, job_id)
    job.setdefault("turns", []).append({"at": started, "seconds": round(time.time() - started), "cost_usd": answer.get("total_cost_usd"),
                                        "usage": answer.get("usage") or {}, "num_turns": answer.get("num_turns"),
                                        "error": bool(answer.get("is_error"))})
    job["state"] = "played" if not answer.get("is_error") else "play-failed"
    job["result"] = {"status": job["state"], "summary": str(answer.get("result") or "")[:600]}
    save(root, job)
    return (0 if job["state"] == "played" else 1), [job["result"]["summary"]]


def resume(root, job_id, message=""):
    job = load(root, job_id)
    if job["state"] == "drift":
        message = drift_message(root, job) + (("\n" + message) if message else "")
    job["fixed"] = False
    job["state"] = "running"
    save(root, job)
    answer = turn(root, job, message or "Làm tiếp việc đang dở, rồi ghi lại result.json.", resume=True)
    return finish(root, job, answer)


def drift_message(root, job):
    """A drifted job: the main project's copy of each conflicting file goes to <worktree>/.worker-merge/<path>, for the
    worker to merge into its own; that main copy (by its sha1) is then accepted as the base of that file."""
    wt = Path(job["worktree"])
    conflicts = plan_apply(root, job)[1]
    accept = dict(job.get("accept") or {})
    for path in conflicts:
        data = read_bytes(Path(root, path))
        accept[path] = digest(data)
        if data is not None:
            target = wt / MERGE_DIR / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    job["accept"] = accept
    save(root, job)
    return ("Project chính đã đổi các file sau từ lúc chụp (thường là người hay phiên khác sửa cùng lúc). Bản hiện tại của "
            f"project chính nằm ở {MERGE_DIR}/<đường dẫn> trong worktree (file không có ở đó là đã bị xoá bên chính). Trộn "
            "thay đổi đó vào bản của bạn, giữ ý của cả hai, rồi kiểm lại và ghi lại result.json.\nFile lệch: " + ", ".join(conflicts))


def status_lines(root, job_id=None):
    if job_id:
        job = load(root, job_id)
        cost = sum(t.get("cost_usd") or 0 for t in job.get("turns") or [])
        lines = [f"{job['id']} · {job.get('kind', 'code')} · {job['feature']} · {job.get('state', '?')} · {job.get('requirements', 0)} yêu cầu · "
                 f"{len(job.get('turns') or [])} lượt · ${cost:.2f}"]
        if job.get("result"):
            lines.append(f"kết quả: {job['result'].get('status')} · {job['result'].get('summary') or ''}")
        checks = job_dir(root, job_id) / "checks.txt"
        if checks.is_file():
            lines += checks.read_text(encoding="utf-8").splitlines()[:20]
        events = job_dir(root, job_id) / "events.log"
        if events.is_file():
            lines += ["sự kiện: " + json.loads(x)["text"][:200] for x in events.read_text(encoding="utf-8").splitlines()[-8:] if x.strip()]
        return lines
    d = jobs_dir(root)
    found = sorted(p.name for p in d.iterdir() if ID_RX.match(p.name)) if d.is_dir() else []
    return [status_lines(root, j)[0] for j in found] or ["chưa có việc nào"]


def drop(root, job_id):
    job = load(root, job_id)
    if job.get("kind", "code") == "code" and job.get("worktree"):
        git(root, "worktree", "remove", "--force", job["worktree"], check=False)
    git(root, "worktree", "prune", check=False)
    job["state"] = "dropped"
    save(root, job)
    return [f"Đã gỡ worktree của {job_id}"]


def main(argv):
    ap = argparse.ArgumentParser(prog="worker.py", add_help=False)
    ap.add_argument("command", nargs="?")
    ap.add_argument("arg", nargs="?")
    ap.add_argument("message", nargs="?", default="")
    ap.add_argument("--reqs", default="")
    ap.add_argument("--task", default="")
    ap.add_argument("--model", default=DEFAULT_MODEL)
    ap.add_argument("--effort", default=DEFAULT_EFFORT)
    try:
        a = ap.parse_args(argv[1:])
    except SystemExit:
        print(USAGE)
        return 2
    try:
        if a.command == "run" and a.arg:
            code, lines = run(ROOT, a.arg, [r for r in a.reqs.split(",") if r], a.task, a.model, a.effort)
        elif a.command == "resume" and a.arg:
            code, lines = resume(ROOT, a.arg, a.message)
        elif a.command == "check" and a.arg:
            ok, lines = check(ROOT, load(ROOT, a.arg))
            code = 0 if ok else 1
        elif a.command == "apply" and a.arg:
            job = load(ROOT, a.arg)
            ok, lines = check(ROOT, job)
            if ok:
                ok, more = apply(ROOT, job)
                lines += more
                if ok:
                    lines += after_apply(ROOT, job, read_result(Path(job["worktree"]))[0])
            code = 0 if ok else 1
        elif a.command == "status":
            code, lines = 0, status_lines(ROOT, a.arg)
        elif a.command == "drop" and a.arg:
            code, lines = 0, drop(ROOT, a.arg)
        else:
            print(USAGE)
            return 2
    except (ValueError, RuntimeError) as ex:
        print(f"worker.py: {ex}")
        return 2
    print("\n".join(lines))
    return code


if __name__ == "__main__":
    sys.exit(main(sys.argv))
