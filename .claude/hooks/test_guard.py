#!/usr/bin/env python3
"""Tests for guard.py.

    python .claude/hooks/test_guard.py

Feeds fake hook input to guard.py and compares the decision. Works on a throw-away copy of the
folder layout in a temp folder; the real project is not touched.
"""
import importlib.util
import json
import time
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

WIN = os.name == "nt"      # Windows: case-insensitive, backslash paths, drives, junctions

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
import guard  # noqa: E402

FX = tempfile.mkdtemp(prefix="guardfx-")
A = FX + "/UnityProject/Assets"
F = A + "/Features"
VALIDATE = "python Management/tools/validate.py"
STORE = FX + "/guard-store"      # stands in for guard.STORE_DIR; the real one is never touched
PIN = "tr0ng-test"

results = []


# Tiers 5-7 are no chat any more (người quyết 2026-11-27: features, services and @Core are workers, worker.py): a case
# run as one of them is not run. The guard gives such a binding no mode at all ("bind: guard uses it" checks that).
GONE_RX = re.compile(r"^((_Services[/\\])?[A-Za-z][A-Za-z0-9]*|@Core)$")


def gone(feature):
    return isinstance(feature, str) and bool(GONE_RX.match(feature.strip()))


def check(group, feature, expect, label, got):
    if gone(feature):
        return
    results.append((got == expect, group, feature or "(unset)", expect, got, label.replace(FX, "")))


def t(group, feature, expect, tool, ti, cwd=None, session="test-session"):
    """In-process: calls guard.decide exactly as main() does, including fail-closed on errors. The Editor lock
    lives in STORE, never in the real store."""
    if gone(feature):
        return
    data = {"tool_name": tool, "tool_input": ti, "cwd": cwd or FX, "session_id": session}
    try:
        result = guard.decide(data, feature, FX, STORE)
    except Exception:
        result = ("deny", "error")
    got = result[0] if result else "pass"
    check(group, feature, expect, tool + " " + " ".join(str(v) for v in ti.values()), got)


def r(g, f, e, p): t(g, f, e, "Read", {"file_path": p})
def e(g, f, x, p): t(g, f, x, "Edit", {"file_path": p})
def w(g, f, x, p): t(g, f, x, "Write", {"file_path": p})


def cli(group, feature, expect, label, stdin_text, root=FX, env_feature=None):
    """Real process: stdin JSON in, stdout JSON out. FEATURE comes from a chat binding in STORE;
    env_feature is put in the environment only to show that the guard ignores it."""
    session = "cli-" + "".join(c for c in feature if c.isalnum()) if feature else "cli-unbound"
    if feature:
        os.makedirs(STORE + "/sessions", exist_ok=True)
        Path(guard.binding_path(session, STORE)).write_text(json.dumps({"feature": feature}))
    try:
        data = json.loads(stdin_text)
        if isinstance(data, dict):
            data["session_id"] = session
            stdin_text = json.dumps(data)
    except ValueError:
        pass
    env = dict(os.environ)
    env.pop("FEATURE", None)
    if env_feature:
        env["FEATURE"] = env_feature
    args = [sys.executable, "-I", str(HERE / "guard.py")] + (["--root", root, "--store", STORE] if root else [])
    p = subprocess.run(args, input=stdin_text.encode("utf-8"), capture_output=True, env=env)
    if p.returncode != 0:
        got = f"exit{p.returncode}"
    elif not p.stdout.strip():
        got = "pass"
    else:
        try:
            out = json.loads(p.stdout)["hookSpecificOutput"]
            got = out["permissionDecision"] if out["permissionDecisionReason"].startswith("guard: ") else "garbage"
        except Exception:
            got = "garbage"
    check(group, feature, expect, label, got)


def build_fixture():
    for d in ("/.claude/hooks", "/Management/data", "/Management/proposals", "/Management/tools"):
        os.makedirs(FX + d)
    for d in ("/_Composition", "/Core", "/Features/Wallet/Scripts/Internal", "/Features/Wallet/Scripts/Contracts",
              "/Features/Shop/Scripts/Internal", "/Features/_Services/Popup/Scripts/Internal"):
        os.makedirs(A + d)
    Path(FX + "/Management/proposals/old.md").write_text("x")
    Path(F + "/Shop/Scripts/Internal/S.cs").write_text("x")
    os.makedirs(F + "/Wallet/FeatureAssets/Public")
    for f in ("/Scripts/Internal/Old.cs", "/Scripts/Internal/AssemblyInfo.cs", "/Scripts/Contracts/IWallet.cs",
              "/Scripts/Internal/Game.Wallet.Internal.asmdef", "/FeatureAssets/Public/Icon.prefab"):
        Path(F + "/Wallet" + f).write_text("x")
    if WIN:
        import _winapi
        _winapi.CreateJunction(F + "/Shop/Scripts/Internal", F + "/Wallet/link")
    else:
        os.symlink(F + "/Shop/Scripts/Internal", F + "/Wallet/link", target_is_directory=True)
    # The code gate (gamedesign.code_allowed): a feature is coded only when the live design names its folder and
    # has reached round 4. Wallet and _Services/Popup have that here; Shop has no design at all.
    live = FX + "/Management/game-design/live"
    os.makedirs(live + "/feature-code/detail/tech")
    for d in ("", "/feature-code", "/feature-code/detail", "/feature-code/detail/tech"):
        Path(live + d + "/data.md").write_text("# x\n")
    Path(live + "/feature-code/code.json").write_text('{"unity": ["Wallet", "_Services/Popup"]}')
    shutil.copy2(HERE.parent.parent / "Management" / "tools" / "gamedesign.py", FX + "/Management/tools/gamedesign.py")
    shutil.copy2(HERE.parent.parent / "Management" / "tools" / "reqbook.py", FX + "/Management/tools/reqbook.py")
    shutil.copy2(HERE.parent.parent / "Management" / "tools" / "gd_excel.py", FX + "/Management/tools/gd_excel.py")      # gamedesign.py imports it


def run_cases():
    W = "Wallet"

    # ---- one group per row of the permission table
    g = "1 own feature"

    g = "2 own Contracts"

    g = "3 other public"

    g = "4 other private"

    g = "5 _Composition/Core"

    g = "6 Packages/ProjectSettings"

    g = "7 proposals"

    g = "8 Management"
    e(g, "", "deny", FX + "/Management/tools/rules.json")

    g = "other feature's CLAUDE.md"
    r(g, "", "allow", F + "/Shop/CLAUDE.md")

    g = "9 .claude"

    g = "10 Library etc"
    for d in ("Library", "Temp", "Logs", "obj", "UserSettings"):
        pass

    # ---- FEATURE variants
    g, P = "service agent", "_Services/Popup"
    e(g, r"_Services\Popup", "allow", F + "/_Services/Popup/Scripts/Internal/P.cs")

    g = "FEATURE unset"
    e(g, "", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    w(g, "", "deny", FX + "/Management/proposals/new.md")
    e(g, "", "deny", A + "/Core/Base.cs")
    r(g, "", "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    r(g, "", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    t(g, "", "deny", "Bash", {"command": "ls"})

    g = "FEATURE invalid"
    e(g, "../Shop", "deny", F + "/Shop/Scripts/Internal/S.cs")
    e(g, "_Services", "deny", F + "/_Services/Popup/Scripts/Internal/P.cs")
    e(g, "Wallet/Scripts", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, "*", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, "Wallet x", "deny", F + "/Wallet/Scripts/Internal/W.cs")

    # ---- unlisted zones
    g = "unlisted"

    # ---- attempts to get around the guard
    g = "evasion"
    if WIN:      # case-insensitive file system; backslash, drive, UNC, ADS, trailing dot/space, Git Bash paths
        msys = "/" + FX[0].lower() + FX[2:].replace("\\", "/")
    else:        # case-sensitive file system; a backslash is a file-name character, not a separator
        pass

    # ---- search tools
    g = "search"

    # ---- read-only search commands (this CLI has no Glob and Grep tools: ls, find, grep, rg in Bash)
    g = "search: shell"
    def sh(f, x, cmd, cwd=None): t(g, f, x, "Bash", {"command": cmd}, cwd)
    WF = "UnityProject/Assets/Features/Wallet"
    sh("@GameDesigner", "allow", "ls Management/game-design/ai")
    sh("@GameDesigner", "deny", "ls UnityProject/Assets")
    sh("@CodeLead", "allow", "grep -rn 'IWallet' UnityProject/Assets/Features")
    sh("@CodeLead", "allow", "find UnityProject/Assets/Features/Shop/Scripts/Internal -name '*.cs'")
    sh("@UnityAutoPlay", "allow", "ls UnityProject/Assets/Features/Shop/Scripts/Internal")
    sh("@UnityAutoPlay", "allow", "ls Tester/UnityAutoPlay/Archive")
    sh("@CodeLead", "deny", "ls Tester/UnityAutoPlay/Archive")
    sh("", "allow", "ls Management/tools")

    # ---- shell and other tools
    g = "shell"
    t(g, "", "allow", "PowerShell", {"command": VALIDATE})      # read-only script, harmless without FEATURE

    g = "tools"
    # unknown tools pass (human decided 2026-11-26): a subagent hands back its result, a lead reads its tasks
    for who in (W, "@CodeLead", "@Manager", "@Core", "@UnityAutoPlay", "@GameDesigner", ""):
        for tool in ("SubagentHandback", "TaskOutput", "TaskStop", "ScheduleWakeup", "SomeNewTool"):
            t(g + " (unknown tools pass)", who, "pass", tool, {"result": "x"})

    # ---- unity-mcp: only deleting a file of the session's own feature
    g, U = "unity-mcp delete", "mcp__unityMCP__manage_asset"
    d = lambda feature, expect, path, action="delete": t(g, feature, expect, U, {"action": action, "path": path})  # noqa: E731
    d(W, "allow", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    d(W, "allow", "Assets\\Features\\Wallet\\Scripts\\Internal\\Old.cs")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs", action="modify")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs", action="")
    d(W, "deny", "Features/Wallet/Scripts/Internal/Old.cs")
    d(W, "deny", FX + "/UnityProject/Assets/Features/Wallet/Scripts/Internal/Old.cs")
    d(W, "deny", "Assets/Features/Shop/Scripts/Internal/S.cs")
    d(W, "deny", "Assets/Features/Wallet/../Shop/Scripts/Internal/S.cs")
    d(W, "deny", "Assets/Features/Wallet/link/S.cs")
    d(W, "deny", "Assets/../../Management/tools/rules.json")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/*.cs")
    d(W, "deny", "Assets/Core/Base.cs")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs")      # the skeleton
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal")
    d(W, "deny", "Assets/Features/Wallet")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Missing.cs")
    # an empty folder of its own (files deleted one by one first) goes without asking; the layout folders stay
    cheat = F + "/Wallet/Scripts/Internal/Cheat"
    os.makedirs(cheat + "/Sub", exist_ok=True)
    Path(cheat + "/Sub.meta").write_text("x")
    Path(cheat + "/Gone.cs.meta").write_text("x")      # a .meta left without its file is not empty
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Cheat")
    os.remove(cheat + "/Gone.cs.meta")
    d(W, "allow", "Assets/Features/Wallet/Scripts/Internal/Cheat")
    d(W, "allow", "Assets/Features/Wallet/Scripts/Internal/Cheat/Sub")
    Path(cheat + "/Sub/C.cs").write_text("x")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Cheat")
    os.makedirs(F + "/Wallet/Tests/Empty", exist_ok=True)
    d(W, "allow", "Assets/Features/Wallet/Tests/Empty")
    os.makedirs(F + "/Wallet/FeatureAssets/Private", exist_ok=True)
    os.makedirs(F + "/Wallet/Empty", exist_ok=True)
    for layout in ("Assets/Features/Wallet/FeatureAssets/Private", "Assets/Features/Wallet/FeatureAssets",
                   "Assets/Features/Wallet/Empty", "Assets/Features/Wallet/Tests/Empty/../../Tests"):
        d(W, "deny", layout)
    os.makedirs(F + "/Shop/Scripts/Internal/Empty", exist_ok=True)
    d(W, "deny", "Assets/Features/Shop/Scripts/Internal/Empty")      # not its own
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/AssemblyInfo.cs")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    d(W, "allow", "Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    d("", "deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    d("@human", "pass", "Assets/Features/Shop/Scripts/Internal/S.cs")

    # ---- boundary files inside the agent's own feature
    g = "boundary files (the skeleton: only @CodeLead)"
    other = F + "/Wallet/Scripts/Internal/Other.cs"

    # ---- human session
    g, H = "human", "@human"
    e(g, H, "pass", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, H, "pass", F + "/Shop/Scripts/Internal/S.cs")
    w(g, H, "pass", FX + "/Management/data/idea.js")
    e(g, H, "pass", A + "/Core/Base.cs")                # code: the human edits it without being asked
    e(g, H, "pass", A + "/_Composition/Boot.cs")
    e(g, H, "pass", FX + "/.claude/hooks/guard.py")      # the human chat is not asked anywhere (HUMAN_ASK_ZONES is empty)
    t(g, H, "pass", "Bash", {"command": "ls"})
    e("human case-sensitive", "@Human", "deny", F + "/Wallet/Scripts/Internal/W.cs")

    # ---- the real process: wire format, binding, fail-closed
    g = "process"
    payload = lambda tool, ti: json.dumps({"tool_name": tool, "tool_input": ti, "cwd": FX})  # noqa: E731
    cli(g, "", "deny", "Edit, FEATURE unset", payload("Edit", {"file_path": F + "/Wallet/Scripts/Internal/W.cs"}))
    cli(g, "@human", "pass", "Edit, @human", payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}))
    cli(g, "@human", "pass", "Edit .claude, @human", payload("Edit", {"file_path": FX + "/.claude/settings.json"}))
    cli(g, "", "deny", "Edit, unbound, FEATURE=@human in the environment is ignored",
        payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}), env_feature="@human")
    cli(g, "", "deny", "Edit, unbound, FEATURE=Wallet in the environment is ignored",
        payload("Edit", {"file_path": F + "/Wallet/Scripts/Internal/W.cs"}), env_feature=W)
    run_binding_cases(payload)
    run_role_cases(payload)
    run_report_cases()
    run_codelead_cases()
    run_designer_cases()
    run_uidesigner_cases()
    run_advisor_cases()
    run_progress_cases()
    run_req_cases()
    run_unity_cases()
    run_tester_cases()
    run_inbox_cases()
    run_unity_leader_cases()
    run_fail_cases()
    real = str(HERE.parent.parent)
    cli("real root", "", "deny", "Write <real root>\\x.txt, no --root",
        json.dumps({"tool_name": "Write", "tool_input": {"file_path": real + "/x.txt"}, "cwd": real}), root=None)
    cli("real root", "", "allow", "Read <real root>\\CLAUDE.md, no --root",
        json.dumps({"tool_name": "Read", "tool_input": {"file_path": real + "/CLAUDE.md"}, "cwd": real}), root=None)


def run_uidesigner_cases():
    """@UIDesigner lays the UI out: reads like @GameDesigner (design tree, references, its rules and the designer's), writes
    frames only (the write gate is tested with @GameDesigner's), runs gd_status, gd_frame, deletes its frames, chains
    commands and may ask the tester to act one thing out, like the other Leaders; no unity-mcp, no code."""
    U = "@UIDesigner"
    base = FX + "/Management/game-design"
    idea = base + "/ai/idea"
    os.makedirs(idea + "/feature-hero", exist_ok=True)
    g = "uidesigner: reads the design, its rules, never the code"
    for p in (idea + "/feature-hero/data.md", base + "/live/data.md", FX + "/Management/references/a.png",
              FX + "/.claude/rules/uidesigner.md", FX + "/.claude/rules/gamedesigner.md",
              FX + "/.claude/skills/ui-frame-syntax/SKILL.md", FX + "/.claude/skills/ui-frame-syntax/button-rects.png",
              FX + "/.claude/skills/gd-round-4/SKILL.md", FX + "/.claude/skills/ui-layout-screen/SKILL.md"):
        r(g, U, "allow", p)
    for p in (F + "/Wallet/Scripts/Internal/W.cs", FX + "/.claude/rules/codelead.md", FX + "/Management/tools/guard.py",
              FX + "/.claude/skills/gd-round-1/SKILL.md", FX + "/.claude/skills/codelead-scaffold-feature/SKILL.md",
              FX + "/.claude/skills/advisor-ask-gpt/SKILL.md", FX + "/.claude/skills/ui-frame-syntax",
              FX + "/.claude/skills/ui_x/SKILL.md", FX + "/.claude/rules/ui-frame-syntax/SKILL.md"):
        r(g + " (of the skills only its own)", U, "deny", p)
    for p in (FX + "/.claude/skills/ui-frame-syntax/SKILL.md", FX + "/.claude/skills/new-skill/SKILL.md"):
        w(g + " (the skills are the human's)", U, "deny", p)
    g = "skills: every other bound chat reads them, none writes them"
    for who in ("Wallet", "@Core", "@CodeLead", "@Manager", "@UnityAutoPlay"):
        r(g, who, "allow", FX + "/.claude/skills/advisor-ask-gpt/SKILL.md")
        w(g, who, "deny", FX + "/.claude/skills/advisor-ask-gpt/SKILL.md")
    g = "uidesigner: commands"
    for cmd in ("python Management/tools/gd_status.py", "python Management/tools/gd_status.py ai/idea/feature-hero",
                "python Management/tools/gd_status.py --brief", "python Management/tools/gd_status.py ai/idea/feature-hero --brief",
                "python Management/tools/gd_frame.py ai/idea/feature-hero/hero-list/x --png",
                "python Management/tools/gd_frame.py ai/idea/feature-hero/hero-list/x --png-full",
                "python Management/tools/gd_frame.py ai/idea/feature-hero/hero-list/x",
                "python Management/tools/gd_delete.py ai/idea/feature-hero/hero-list/.frame-concept/main.frame",
                "python Management/tools/gd_status.py; python Management/tools/gd_frame.py live/feature-hero",
                "python Management/tools/scaffold.py --dry-run"):
        t(g, U, "allow", "Bash", {"command": cmd})
    for cmd in ("python Management/tools/gd_delete.py ai/idea/feature-hero", "python Management/tools/gd_delete.py ai/idea/feature-hero/data.md",
                "python Management/tools/gd_delete.py ai/idea/feature-hero/.frame-concept/../data.md",
                "python Management/tools/gd_advise.py feature-hero q1", "python Management/tools/scaffold.py"):
        t(g, U, "deny", "Bash", {"command": cmd})
    t(g, "@GameDesigner", "allow", "Bash", {"command": "python Management/tools/gd_delete.py ai/idea/feature-hero"})
    for who in (U, "@GameDesigner", "Wallet", "@CodeLead"):
        t(g + " (frames drawn into PNGs to look at)", who, "allow", "Bash",
          {"command": "python Management/tools/gd_frame.py ai/idea/feature-hero/hero-list/x --png"})
    t(g, U, "deny", "Bash", {"command": "python Management/tools/gd_frame.py ai/idea/feature-hero --png --html x.html"})
    g = "uidesigner: its lessons, the reference pictures"
    w(g, U, "allow", base + "/ai/ui-lessons.md")
    e(g, U, "allow", base + "/ai/ui-lessons.md")
    for who in ("@GameDesigner", "Wallet", "@DesignAdvisor"):
        w(g, who, "deny", base + "/ai/ui-lessons.md")
    for p in (base + "/ai/idea/ui-lessons.md", base + "/ai/lessons.md"):
        w(g, U, "deny", p)
    refs = FX + "/Management/references"
    for who in (U, "@GameDesigner", "@DesignAdvisor", "@CodeLead", "Wallet", "@Manager"):
        r(g + " (everyone reads the references)", who, "allow", refs + "/hud-1.png")
        t(g + " (everyone reads the references)", who, "allow", "Grep", {"pattern": "x", "path": refs})
    for who in (U, "@GameDesigner", "@Manager", "@CodeLead", "Wallet"):
        w(g + " (only the page writes them)", who, "deny", refs + "/index.json")
    g = "uidesigner: unity-mcp reads, no code, may message the tester"
    t(g, U, "allow", "mcp__unityMCP__read_console", {"action": "get"})
    t(g, U, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, U, "pass", "SendMessage", {"to": "UnityAutoPlay", "message": "XÁC NHẬN · Feature: Hud · Thao tác: bấm II"})
    t(g, U, "pass", "SendMessage", {"to": "GameDesigner", "message": "ui.md thiếu nút đóng"})
    for p in (F + "/Wallet/Scripts/Internal/W.cs", FX + "/Management/proposals/new.md", FX + "/CLAUDE.md"):
        w(g, U, "deny", p)


def run_advisor_cases():
    """@DesignAdvisor asks GPT about the design: reads the design tree, the references, the root CLAUDE.md and the rules
    of the design roles; writes only its questions and notes in Management/game-design/advice; runs gd_advise.py and
    gd_status.py. It never writes the design, and nobody else writes in advice."""
    import bind_session
    W, D, V = "Wallet", "@GameDesigner", "@DesignAdvisor"
    base = FX + "/Management/game-design"
    idea, live, advice = base + "/ai/idea", base + "/live", base + "/advice"
    for d in (idea, live, advice + "/feature-hero"):
        os.makedirs(d, exist_ok=True)

    g = "advisor: writes only its questions, picks and notes"
    for p in (advice + "/feature-hero/q1.q.md", advice + "/feature-hero/q1.note.md", advice + "/feature-hero.hero-list/round-2.q.md",
              advice + "/feature-hero/q1.pick.md", advice + "/_general/g1.pick.md",
              advice + "/_root/g1.q.md", advice + "/_general/g1.note.md"):
        w(g, V, "allow", p)
        e(g, V, "allow", p)
    for p in (advice + "/feature-hero/q1.md", advice + "/feature-hero/q1.v1.md", advice + "/feature-hero/q1.rejected.md",
              advice + "/feature-hero/q1.pick.json",
              advice + "/q1.q.md", advice + "/feature-hero/sub/q1.q.md", advice + "/feature-hero/q1.q.txt",
              advice + "/_other/q1.q.md", advice + "/feature-hero/hero-list/q1.q.md", advice + "/feature_hero/q1.q.md"):
        w(g + " (the answers are the command's)", V, "deny", p)
    for p in (idea + "/data.md", idea + "/feature-hero/data.md", idea + "/notes.md", live + "/data.md", base + "/mode.json",
              base + "/x.md", FX + "/Management/proposals/new.md", FX + "/Management/references/a.png", FX + "/CLAUDE.md",
              FX + "/.claude/rules/designadvisor.md", F + "/Wallet/Scripts/Internal/W.cs", A + "/Core/Base.cs"):
        w(g + " (never the design or anything else)", V, "deny", p)
    t(g, V, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, V, "allow", "mcp__unityMCP__refresh_unity", {})
    t(g, V, "allow", "mcp__unityMCP__read_console", {"action": "get"})
    t(g, V, "pass", "SendMessage", {"to": "GameDesigner", "message": "x"})

    g = "advisor: the others do not write in advice"
    for who in (W, D, "@Manager", "@CodeLead", "@Core", ""):
        w(g, who, "deny", advice + "/feature-hero/q1.q.md")
        w(g, who, "deny", advice + "/feature-hero/q1.md")
    for who in (W, D, "@Manager", "@CodeLead"):
        r(g + " (they read it)", who, "allow", advice + "/feature-hero/q1.md")

    g = "advisor: reads the design, not the code"
    for p in (idea + "/data.md", live + "/data.md", advice + "/feature-hero/q1.md", base, base + "/mode.json",
              FX + "/Management/references/a.png", FX + "/CLAUDE.md", FX + "/.claude/rules/designadvisor.md",
              FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/skills/gd-round-3/SKILL.md",
              FX + "/.claude/skills/advisor-ask-gpt/SKILL.md", FX + "/.claude/skills/gd-integrate-advice/SKILL.md"):
        r(g, V, "allow", p)
    t(g, V, "allow", "Grep", {"pattern": "hero", "path": base})
    for p in (FX + "/.claude/rules/codelead.md", FX + "/.claude/rules/worker.md", FX + "/.claude/hooks/guard.py",
              FX + "/.claude/skills/ui-frame-syntax/SKILL.md", FX + "/.claude/skills/manager-startup-check/SKILL.md",
              FX + "/Management/proposals/old.md", FX + "/Management/tools/gd_advise.py", FX + "/Management",
              F + "/Wallet/Scripts/Contracts/IWallet.cs", F + "/Wallet/CLAUDE.md", A + "/Core/Base.cs", "C:/Windows/x"):
        r(g, V, "deny", p)
    t(g, V, "deny", "Grep", {"pattern": "x"})

    g = "advisor: commands"
    ADVISE, STATUS = "python Management/tools/gd_advise.py", "python Management/tools/gd_status.py"
    for cmd in (ADVISE + " feature-hero q1", ADVISE + " feature-hero.hero-list.hero-roster round-2", ADVISE + " _root g1",
                ADVISE + " _general g1", "python3 Management/tools/gd_advise.py feature-hero q1", STATUS, STATUS + " ai/idea/feature-hero",
                "python Management/tools/gd_pack.py Wallet", "python Management/tools/gd_frame.py live/feature-hero/a/b",
                "python Management/tools/art.py list"):
        t(g, V, "allow", "Bash", {"command": cmd})
    for cmd in (ADVISE, ADVISE + " feature-hero", ADVISE + " feature-hero q1 extra", ADVISE + " feature-hero/hero-list q1",
                ADVISE + " ../x q1", ADVISE + " Feature-Hero q1", ADVISE + " feature-hero Q1", ADVISE + " feature-hero q1; ls",
                ADVISE + " feature-hero q1 --root C:/x", ADVISE + " _other q1", "codex exec -s read-only x",
                "python Management/tools/gd_delete.py ai/idea/feature-hero", "python Management/tools/art.py choose Hero/a simple",
                "python Management/tools/gd_progress.py ai/idea/a idea done", "ls"):
        t(g, V, "deny", "Bash", {"command": cmd})
    for who in (W, D, "@Manager", "@CodeLead", "@Core", ""):
        t(g + " (only the advisor asks GPT)", who, "deny", "Bash", {"command": ADVISE + " feature-hero q1"})

    g = "advisor: bind"
    say(g, "block, bound=@DesignAdvisor", "bind @DesignAdvisor", f"FEATURE=@designadvisor {PIN}", session="role-adv")
    told = bind_session.context({"session_id": "role-adv"}, FX, STORE)
    check(g, V, True, "the context names the advice folder, the command, who may ask and the rules",
          all(s in told for s in ("FEATURE=@DesignAdvisor", "game-design/advice", "gd_advise.py", "người hoặc @GameDesigner",
                                  ".claude/rules/designadvisor.md", "Full Auto", "điểm NGƯỜI chấm", "không được bỏ")))
    check(g, V, False, "the context never has the PIN", PIN in told)
    check(g, V, True, "a headless chat is told not to ask, and whom to tell", "KHÔNG HỎI NGƯỜI" in told and "GameDesigner" in told)
    told = bind_session.context({"session_id": "role-gd"}, FX, STORE)
    check(g, D, True, "a Leader asks the human through Manager (2026-11-27)",
          "KHÔNG HỎI NGƯỜI" in told and "ask.py add --from GameDesigner" in told and "BÁO CHẶN" not in told.split("KHÔNG HỎI NGƯỜI")[1])
    mgr = bind_session.context({"session_id": "role-mgr"}, FX, STORE) if os.path.exists(guard.binding_path("role-mgr", STORE) or "") else ""
    check(g, "@Manager", False, "Manager asks the human itself", "KHÔNG HỎI NGƯỜI" in mgr)

    g = "guard line: the fixed text once"
    gd = {"session_id": "role-gd"}
    bind_session.forget_shown("role-gd", STORE)
    first = bind_session.prompt_context(gd, FX, STORE)
    again = bind_session.prompt_context(gd, FX, STORE)
    check(g, D, True, "the first prompt has the whole text, with the skills", "NGANG HÀNG" in first and "Skill của vai" in first)
    check(g, D, True, "the next one: the first line and what changes only",
          again.startswith("guard: chat này gán FEATURE=@GameDesigner (tầng 2 · Leaders)") and "NGANG HÀNG" not in again
          and "Skill của vai" not in again and len(again) < len(first) // 3)
    check(g, D, True, "the mode is part of the fixed text: a change sends the whole line again",
          "Mode hiện tại" not in again and "Mode hiện tại" in first)
    bind_session.forget_shown("role-gd", STORE)      # a compaction or /clear
    check(g, D, True, "after a compaction the whole text again", "NGANG HÀNG" in bind_session.prompt_context(gd, FX, STORE))
    check(g, D, True, "no marks left in either", all(c not in x for x in (first, again) for c in (chr(1), chr(2))))
    check(g, D, True, "@GameDesigner is told about the advisor and the labels",
          all(s in told for s in ("DesignAdvisor", "game-design/advice", "by: <id ý>=gpt@<id lần hỏi>#<id góp ý>", "gpt-dropped:",
                                  "node-by:", "KHÔNG ĐƯỢC BỎ", "NGANG HÀNG")))


def say(group, expect, label, prompt, session="chat1", store=None):
    """Real bind_session.py process. got = 'pass' (prompt goes on, with a context line naming this chat's
    binding and never the PIN) or 'block', plus the binding it left."""
    env = dict(os.environ)
    env.pop("FEATURE", None)
    args = [sys.executable, "-I", str(HERE / "bind_session.py"), "--root", FX, "--store", store or STORE]
    stdin = json.dumps({"hook_event_name": "UserPromptSubmit", "session_id": session, "prompt": prompt})
    p = subprocess.run(args, input=stdin.encode("utf-8"), capture_output=True, env=env)
    bound = guard.bound_feature(session, store or STORE) if isinstance(session, str) else None
    if p.returncode != 0:
        got = f"exit{p.returncode}"
    else:
        try:
            out = json.loads(p.stdout)
            if "decision" in out:
                hidden = out["hookSpecificOutput"]["suppressOriginalPrompt"] is True
                leaked = PIN in out["reason"]
                got = out["decision"] if hidden and not leaked else "garbage"
            else:
                ctx = out["hookSpecificOutput"]["additionalContext"]
                names = (f"FEATURE={bound}" in ctx and "không hợp lệ" not in ctx) if bound else "chưa gán" in ctx
                got = "pass" if names and PIN not in ctx else "garbage: " + ctx
        except Exception:
            got = "garbage"
    check(group, "", expect, label, f"{got}, bound={bound}")


def run_binding_cases(payload):
    import bind_session
    W, S = "Wallet", F + "/Shop/Scripts/Internal/S.cs"
    own = F + "/Wallet/Scripts/Internal/W.cs"
    bind_session.write_pin(STORE, PIN)

    def b(group, expect, session, tool, ti):
        """Resolves the session exactly as guard.main() does, then decides."""
        data = {"tool_name": tool, "tool_input": ti, "cwd": FX, "session_id": session}
        try:
            result = guard.decide(data, guard.session_feature(session, STORE), FX)
        except Exception:
            result = ("deny", "error")
        check(group, "", expect, f"session={session} {tool} " + " ".join(str(v) for v in ti.values()),
              result[0] if result else "pass")

    # ---- the human binds a chat by typing FEATURE=<value> <PIN>
    g = "bind: typed command"
    say(g, "pass, bound=None", "ordinary prompt", "Sửa lỗi trong Wallet giúp tôi")
    say(g, "pass, bound=None", "prompt that only mentions the word", "feature flags là gì?")
    say(g, "block, bound=None", "status of an unbound chat", "FEATURE?")
    say(g, "block, bound=None", "no PIN", "FEATURE=Wallet")
    say(g, "block, bound=None", "wrong PIN", "FEATURE=Wallet 0000")
    say(g, "block, bound=None", "unknown feature, right PIN", f"FEATURE=Nope {PIN}")
    say(g, "block, bound=None", "path trick, right PIN", f"FEATURE=../Shop {PIN}")
    say(g, "block, bound=None", "extra words after the PIN", f"FEATURE=Wallet {PIN} làm ơn")
    say(g, "block, bound=None", "session id unusable as a file name", f"FEATURE=Wallet {PIN}", session="..\\x")
    ide = "<ide_opened_file>The user opened d:\\x\\FEATURE=1.cs\nline two</ide_opened_file>"
    say(g, "block, bound=None", "status, wrapped in client context", ide + "FEATURE?")
    say(g, "block, bound=None", "status, context after it", "FEATURE?\n" + ide + "\n<a b='c'>x</a>")
    say(g, "block, bound=None", "wrong PIN, wrapped in client context", ide + "\nFEATURE=Wallet 0000\n")
    say(g, "pass, bound=None", "the word only inside client context", ide + "\nSửa giúp tôi")
    say(g, "pass, bound=None", "a command-like line in the middle of a pasted text", "Xem đoạn này:\nFEATURE = 1\nđúng không?")
    say(g, "block, bound=None", "a feature is a worker's, not a chat's (người quyết 2026-11-27)", f"FEATURE=Wallet {PIN}")
    say(g, "block, bound=None", "so is a service", f"{ide}\nFEATURE=_Services/Popup {PIN}", session="chat-ide")
    say(g, "block, bound=None", "and @Core, and the tester", f"FEATURE=@Core {PIN}", session="chat-core")
    say(g, "block, bound=None", "the tester is bound by the server only", f"FEATURE=@UnityAutoPlay {PIN}", session="chat-core")
    say(g, "block, bound=@CodeLead", "right PIN", f"FEATURE=@CodeLead {PIN}")
    say(g, "block, bound=@CodeLead", "status of a bound chat", "feature")
    say(g, "pass, bound=@CodeLead", "next ordinary prompt tells the model its binding", "Làm tiếp đi")
    say(g, "block, bound=@CodeLead", "wrong PIN keeps the old binding", "FEATURE=@human 0000")
    say(g, "block, bound=@DesignAdvisor", "spaces, lower case", f"  feature = @designadvisor   {PIN}\n", session="chat2")
    say(g, "block, bound=@human", "human", f"FEATURE=@human {PIN}", session="chat3")
    say(g, "block, bound=@GameDesigner", "the human may rebind", f"FEATURE=@GameDesigner {PIN}", session="chat3")
    say(g, "block, bound=@human", "and bind back", f"FEATURE=@human {PIN}", session="chat3")
    say(g, "pass, bound=@human", "after a rebind the context follows the new binding", "tiếp", session="chat3")

    # ---- the guard reads the binding of the chat that makes the call
    g = "bind: guard uses it"
    b(g, "allow", "chat1", "Edit", {"file_path": A + "/_Composition/Boot.cs"})
    b(g, "deny", "chat1", "Edit", {"file_path": FX + "/.claude/hooks/guard.py"})
    b(g, "deny", "chat2", "Edit", {"file_path": own})
    b(g, "deny", "chat-ide", "Edit", {"file_path": F + "/_Services/Popup/Scripts/Internal/P.cs"})
    b(g, "pass", "chat3", "Edit", {"file_path": S})
    b(g, "pass", "chat3", "Edit", {"file_path": FX + "/.claude/hooks/guard.py"})
    b(g, "deny", "other-chat", "Edit", {"file_path": own})           # a binding belongs to one chat only
    b(g, "deny", None, "Edit", {"file_path": own})
    b(g, "deny", "..\\chat1", "Edit", {"file_path": own})
    Path(guard.binding_path("broken", STORE)).write_text("{ not json")
    b(g, "deny", "broken", "Edit", {"file_path": own})
    Path(guard.binding_path("gone", STORE)).write_text('{"feature": "Nope"}')
    b(g, "deny", "gone", "Edit", {"file_path": own})                 # a binding to a missing feature
    Path(guard.binding_path("old-feature", STORE)).write_text('{"feature": "Wallet"}')
    b(g, "deny", "old-feature", "Edit", {"file_path": own})          # a chat bound to a feature before 2026-11-27: inert
    b(g, "allow", "chat1", "Bash", {"command": VALIDATE + " --feature Wallet"})

    # ---- the real guard process reads the store it is given, and none when it is given none
    g = "bind: process"
    with_session = lambda s: json.dumps({"tool_name": "Edit", "tool_input": {"file_path": A + "/_Composition/Boot.cs"}, "cwd": FX,  # noqa: E731
                                         "session_id": s})
    for expect, session, store in (("allow", "chat1", STORE), ("deny", "chat2", STORE), ("deny", "chat1", None)):
        args = [sys.executable, "-I", str(HERE / "guard.py"), "--root", FX] + (["--store", store] if store else [])
        env = dict(os.environ)
        env.pop("FEATURE", None)
        p = subprocess.run(args, input=with_session(session).encode("utf-8"), capture_output=True, env=env)
        try:
            got = json.loads(p.stdout)["hookSpecificOutput"]["permissionDecision"]
        except Exception:
            got = "garbage"
        check(g, "", expect, f"Edit _Composition (@CodeLead), session={session}, store={'yes' if store else 'no'}", got)

    # ---- guessing the PIN locks binding; a missing PIN binds nothing
    g = "bind: PIN lock"
    for i in range(bind_session.MAX_FAILURES):
        say(g, "block, bound=None", f"wrong PIN #{i + 1}", "FEATURE=@human 0000", session="thief")
    say(g, "block, bound=None", "right PIN after the lock", f"FEATURE=@human {PIN}", session="thief")
    bind_session.write_pin(STORE, PIN)
    say(g, "block, bound=@human", "right PIN after the human set it again", f"FEATURE=@human {PIN}", session="thief")
    say(g, "block, bound=None", "no PIN was ever set", f"FEATURE=Wallet {PIN}", store=FX + "/empty-store")
    for expect, label, stdin in ((2, "broken input that may carry the PIN is blocked", '{"prompt": "FEATURE=Wallet ' + PIN),
                                 (0, "broken input of an ordinary prompt is let through", '{"prompt": "xin chào')):
        args = [sys.executable, "-I", str(HERE / "bind_session.py"), "--root", FX, "--store", STORE]
        p = subprocess.run(args, input=stdin.encode("utf-8"), capture_output=True)
        check(g, "", f"exit{expect}, no output", label, f"exit{p.returncode}, {'output' if p.stdout.strip() else 'no output'}")

    # ---- role rules are not loaded into every chat: the chat of that role is told to Read them, once per context
    g = "bind: role rules reminder"
    sid = "rules-mgr"
    bind_session.write_json(guard.binding_path(sid, STORE), {"feature": "@Manager"})
    bind_session.forget_rules_read(sid, STORE)

    def told(*extra):
        args = [sys.executable, "-I", str(HERE / "bind_session.py"), *extra, "--root", FX, "--store", STORE]
        p = subprocess.run(args, input=json.dumps({"session_id": sid, "prompt": "xin chào"}).encode("utf-8"),
                           capture_output=True)
        return "Read toàn bộ .claude/rules/manager.md" in p.stdout.decode("utf-8", "replace")
    check(g, "", True, "first prompt after binding: told to Read its role rules", told())
    check(g, "", False, "next prompt: not told again", told())
    check(g, "", False, "SessionStart (compact, clear) adds nothing to the context", told("--session-start"))
    check(g, "", True, "first prompt after a compaction: told again", told())
    bind_session.write_json(guard.binding_path(sid, STORE), {"feature": "@CodeLead"})
    check(g, "", ["CLAUDE.md", ".claude/rules/report.md", ".claude/rules/codelead.md", ".claude/rules/unity.md"],
          "rule files of @CodeLead (what a reread names)", bind_session.rule_files("@CodeLead", FX))
    check(g, "", ([], [".claude/rules/manager.md"]), "a feature is a worker (no chat, nothing to read); a Leader reads only its own rules",
          (bind_session.must_read("Wallet"), bind_session.must_read("@Manager")))
    check(g, "", ("tầng 0 · Master", "tầng 7 · Features"), "tiers are numbered top to bottom", (bind_session.tier_label("Master"), bind_session.tier_label("Features")))
    check(g, "", [".claude/rules/gamedesigner.md", ".claude/rules/designadvisor.md"],
          "@GameDesigner reads its rules itself: they no longer load with the design files", bind_session.must_read("@GameDesigner"))
    check(g, "", [".claude/rules/designadvisor.md", ".claude/rules/gamedesigner.md"],
          "@DesignAdvisor reads its rules itself", bind_session.must_read("@DesignAdvisor"))
    excludes = json.loads((HERE.parent / "settings.json").read_text(encoding="utf-8")).get("claudeMdExcludes", [])
    for name in ("manager.md", "codelead.md", "gamedesigner.md", "designadvisor.md", "uidesigner.md", "tester.md", "worker.md"):
        check(g, "", True, f"{name} never loads by itself into another role's chat (claudeMdExcludes)",
              f"**/.claude/rules/{name}" in excludes)
    check(g, "", True, "the node files of the design tree (CLAUDE.md) do not load near a Read",
          "**/Management/game-design/**/CLAUDE.md" in excludes)
    check(g, "", False, "report.md and unity.md still load (shared rules)",
          any(e.endswith(("report.md", "unity.md")) for e in excludes))
    for name in ("manager.md", "codelead.md", "tester.md", "unity.md"):
        head = (HERE.parent / "rules" / name).read_text(encoding="utf-8").split("\n", 2)[:2]
        check(g, "", ["---", "paths:"], f"{name} is path-scoped, not loaded into every chat", head)


def run_role_cases(payload):
    """@Core: like a feature session whose feature is Core. @Manager: edits nothing."""
    Path(A + "/Core/Old.cs").write_text("x")
    Path(A + "/_Composition/Old.cs").write_text("x")
    own_w = F + "/Wallet/Scripts/Internal/Old.cs"
    delete = lambda p: {"action": "delete", "path": p}       # noqa: E731
    sh = lambda c: {"command": c}                            # noqa: E731

    for role, own, other, owner in (("@Core", "/Core", "/_Composition", "Core"),):
        g = "role " + role
        e(g, role, "allow", A + own + "/Old.cs")
        w(g, role, "allow", A + own + "/New.cs")
        w(g, role, "allow", A + own + "/Tests/T.cs")
        e(g, role, "deny", A + own + "/Game.Thing.asmdef")      # the skeleton: only @CodeLead
        w(g, role, "deny", A + own + "/AssemblyInfo.cs")
        w(g, role, "deny", A + own + "/IThing.cs")
        w(g, role, "allow", A + own + "/Tests/IFake.cs")       # interfaces in tests are no skeleton
        w(g, role, "deny", A + own + "/SessionParticipantBase.cs")      # a base class is skeleton, as in a feature
        e(g, role, "deny", A + own + "/Sub/ThingBase.cs")
        w(g, role, "allow", A + own + "/Tests/FakeBase.cs")
        w(g, role, "allow", A + own + "/Database.cs")      # the name must end in an upper-case Base
        w(g, role, "allow", A + own + "/Base.cs")
        w(g, role, "allow", A + own + "/Thing.cs")
        w(g, role, "deny", A + own + "/x.asmref")
        e(g, role, "deny", A + other + "/Old.cs")
        e(g, role, "deny", own_w)
        r(g, role, "deny", own_w)
        r(g, role, "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
        r(g, role, "allow", F + "/Wallet/CLAUDE.md")
        r(g, role, "allow", A + other + "/Old.cs")
        w(g, role, "deny", FX + "/Management/proposals/new.md")
        e(g, role, "deny", FX + "/Management/tools/rules.json")
        e(g, role, "deny", FX + "/.claude/settings.json")
        t(g, role, "allow", "Bash", sh(VALIDATE + " --feature " + owner))
        t(g, role, "deny", "Bash", sh("dotnet build"))
        t(g, role, "allow", guard.UNITY_ASSET_TOOL, delete("Assets" + own.replace("\\", "/") + "/Old.cs"))
        t(g, role, "deny", guard.UNITY_ASSET_TOOL, delete("Assets" + other.replace("\\", "/") + "/Old.cs"))
        t(g, role, "deny", guard.UNITY_ASSET_TOOL, delete("Assets/Features/Wallet/Scripts/Internal/Old.cs"))

    g = "role @Manager"
    M = "@Manager"
    for p in (own_w, A + "/Core/Old.cs", A + "/_Composition/Old.cs", F + "/Wallet/Scripts/Contracts/IWallet.cs",
              FX + "/Management/tools/rules.json", FX + "/Management/data/x.js", FX + "/.claude/settings.json"):
        e(g, M, "deny", p)
    w(g, M, "deny", FX + "/Management/proposals/plan.md")
    e(g, M, "deny", FX + "/Management/proposals/old.md")
    r(g, M, "allow", FX + "/Management/proposals/old.md")
    TW = "python Management/tools/toolwork.py "
    t(g + ": tool work the human decided", M, "allow", "Bash", sh(TW + 'add "Thêm lệnh huỷ order" --from Manager --decided "Người chọn A ngày 2026-11-26"'))
    t(g + ": names itself", M, "deny", "Bash", sh(TW + 'add "Thêm lệnh huỷ order" --from CodeLead --decided "Người chọn A"'))
    t(g + ": needs the decision", M, "deny", "Bash", sh(TW + 'add "Thêm lệnh huỷ order" --from Manager'))
    t(g + ": reads the list", M, "allow", "Bash", sh(TW + "list"))
    t(g + ": only the human closes", M, "deny", "Bash", sh(TW + 'done w20261126-ab12 "xong"'))
    t(g + ": only the human drops", M, "deny", "Bash", sh(TW + 'drop w20261126-ab12 "thôi"'))
    t(g + ": a Leader adds", "@CodeLead", "allow", "Bash", sh(TW + 'add "Sổ nhận test chéo" --from CodeLead --decided "Người chọn B"'))
    r(g, M, "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    r(g, M, "allow", A + "/Core/Old.cs")
    r(g, M, "deny", own_w)
    t(g, M, "allow", "Bash", sh(VALIDATE))
    t(g, M, "allow", "Bash", sh(VALIDATE + " --feature Shop"))
    t(g, M, "deny", guard.UNITY_ASSET_TOOL, delete("Assets/Core/Old.cs"))
    t(g, M, "deny", "SendMessage", {"to": "Shop", "message": "x"})      # runs on demand: through the inbox
    t(g, M, "pass", "SendMessage", {"to": "CodeLead", "message": "x"})
    t(g, M, "pass", "ListAgents", {})

    g = "own session folders outside the project"
    home, temp = tempfile.mkdtemp(prefix="guard-home-"), tempfile.mkdtemp(prefix="guard-temp-")
    guard.OWN_HOME, guard.OWN_TEMP = home, temp
    ME, OTHER = "11111111-2222-3333-4444-555555555555", "99999999-2222-3333-4444-555555555555"
    store_of = lambda sid: os.path.join(home, ".claude", "projects", "d--proj", sid)      # noqa: E731
    temp_of = lambda sid: os.path.join(temp, "claude", "abc123", sid)      # noqa: E731
    for d in (os.path.join(store_of(ME), "tool-results"), os.path.join(temp_of(ME), "tasks"), os.path.join(temp_of(ME), "scratchpad"),
              os.path.join(store_of(OTHER), "tool-results"), os.path.join(temp_of(OTHER), "scratchpad"),
              os.path.join(home, ".claude", "projects", "d--proj", "memory")):
        os.makedirs(d)
    mine = [os.path.join(store_of(ME), "tool-results", "toolu_1.txt"), os.path.join(temp_of(ME), "tasks", "b1.output"),
            os.path.join(temp_of(ME), "scratchpad", "note.py")]
    try:
        for who in ("Wallet", M, "@GameDesigner", "@DesignAdvisor", "@CodeLead", "@Core", ""):
            for p in mine:
                t(g + ": reads its own", who, "allow", "Read", {"file_path": p}, session=ME)
        t(g + ": searches its own", M, "allow", "Grep", {"pattern": "x", "path": os.path.join(store_of(ME), "tool-results")}, session=ME)
        for p in (os.path.join(store_of(OTHER), "tool-results", "toolu_1.txt"), os.path.join(temp_of(OTHER), "scratchpad", "note.py"),
                  os.path.join(home, ".claude", "projects", "d--proj", ME + ".jsonl"),
                  os.path.join(home, ".claude", "projects", "d--proj", "memory", "MEMORY.md"),
                  os.path.join(home, ".claude", "settings.json"), os.path.join(temp, "other.txt"),
                  os.path.join(store_of(ME), "..", OTHER, "tool-results", "toolu_1.txt"),
                  os.path.join(home, ".claude", "projects", "d--proj")):
            t(g + ": nothing else out there", M, "deny", "Read", {"file_path": p}, session=ME)
        for p in mine:      # the id must be this session's, and a real one
            t(g + ": not another session's", M, "deny", "Read", {"file_path": p}, session=OTHER)
            t(g + ": not without a real id", M, "deny", "Read", {"file_path": p})
        for who in ("Wallet", M, "@GameDesigner"):
            t(g + ": writes its scratchpad", who, "allow", "Write", {"file_path": mine[2], "content": "x"}, session=ME)
        t(g + ": unbound writes nothing", "", "deny", "Write", {"file_path": mine[2], "content": "x"}, session=ME)
        if not WIN:      # a link in its own folders: followed only when it stays in this session's folders, read only
            sub = os.path.join(store_of(ME), "subagents")
            os.makedirs(sub)
            with open(os.path.join(sub, "agent-1.jsonl"), "w") as f:
                f.write("x")
            inner = os.path.join(temp_of(ME), "tasks", "a1.output")
            os.symlink(os.path.join(sub, "agent-1.jsonl"), inner)
            out = os.path.join(temp_of(ME), "tasks", "a2.output")
            os.symlink(os.path.join(store_of(OTHER), "tool-results"), out)
            pad = os.path.join(temp_of(ME), "scratchpad", "l.output")
            os.symlink(os.path.join(sub, "agent-1.jsonl"), pad)
            t(g + ": link to its subagent", "@CodeLead", "allow", "Read", {"file_path": inner}, session=ME)
            t(g + ": link out of the session", "@CodeLead", "deny", "Read", {"file_path": out}, session=ME)
        if True:      # the CLI puts the temp folders in claude-<uid> (claude-0 on Windows)
            uid_pad = os.path.join(temp, f"claude-{os.getuid() if hasattr(os, 'getuid') else 0}", "abc123", ME, "scratchpad")
            os.makedirs(uid_pad)
        for p in (mine[0], mine[1], os.path.join(temp_of(OTHER), "scratchpad", "note.py"), os.path.join(temp_of(ME), "scratchpad"),
                  os.path.join(temp_of(ME), "other", "x.txt")):
            pass

        # @CodeLead and @Manager read (only) the folders of a session bound to a feature or service.
        FEAT, SERV, LEAD, CORE = ("aaaaaaaa-2222-3333-4444-555555555555", "bbbbbbbb-2222-3333-4444-555555555555",
                                  "cccccccc-2222-3333-4444-555555555555", "dddddddd-2222-3333-4444-555555555555")
        os.makedirs(os.path.join(STORE, "sessions"), exist_ok=True)
        bound = []
        for sid, value in ((FEAT, "Wallet"), (SERV, "_Services/Popup"), (LEAD, "@CodeLead"), (CORE, "@Core")):
            bound.append(os.path.join(STORE, "sessions", sid + ".json"))
            with open(bound[-1], "w", encoding="utf-8") as f:
                json.dump({"feature": value}, f)
        for sid in (FEAT, SERV, LEAD, CORE):
            for d in (os.path.join(store_of(sid), "subagents"), os.path.join(temp_of(sid), "tasks"),
                      os.path.join(temp_of(sid), "scratchpad")):
                os.makedirs(d)
        peer = lambda sid: [os.path.join(store_of(sid), "subagents", "agent-1.jsonl"),      # noqa: E731
                            os.path.join(temp_of(sid), "tasks", "a1.output"), os.path.join(temp_of(sid), "scratchpad", "n.md")]
        try:
            for sid in (FEAT, SERV):      # @Manager keeps to the Leaders since 2026-11-26
                for p in peer(sid):
                    t(g + ": @Manager no longer reads a feature session's", M, "deny", "Read", {"file_path": p}, session=ME)
            for who in ("@CodeLead",):
                for sid in (FEAT, SERV):
                    for p in peer(sid):
                        t(g + ": leads read a feature session's", who, "allow", "Read", {"file_path": p}, session=ME)
                    t(g + ": but never write there", who, "deny", "Write", {"file_path": peer(sid)[2], "content": "x"}, session=ME)
                for sid in (LEAD, CORE, OTHER):      # not a feature: a role, or no binding
                    t(g + ": only feature sessions'", who, "deny", "Read", {"file_path": peer(sid)[1]}, session=ME)
                t(g + ": not the transcript", who, "deny", "Read",
                  {"file_path": os.path.join(home, ".claude", "projects", "d--proj", FEAT + ".jsonl")}, session=ME)
            for who in ("Shop", "@GameDesigner", "@Core", "@UnityAutoPlay", ""):
                t(g + ": nobody else reads a feature session's", who, "deny", "Read", {"file_path": peer(FEAT)[1]}, session=ME)
        finally:
            for b in bound:
                os.remove(b)
    finally:
        guard.OWN_HOME, guard.OWN_TEMP = None, None
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(temp, ignore_errors=True)

    g = "manager: scaffold command"
    SCAFFOLD = "python Management/tools/scaffold.py"
    for cmd in (SCAFFOLD, SCAFFOLD + " --dry-run", "python3 Management/tools/scaffold.py"):
        t(g, M, "allow", "Bash", sh(cmd))
    for cmd in (SCAFFOLD + " --root /tmp/x", SCAFFOLD + " Hero", SCAFFOLD + "; rm -rf UnityProject",
                SCAFFOLD + " --dry-run && ls", "python Management/tools/../tools/scaffold.py", SCAFFOLD + " --dry-run --dry-run"):
        t(g, M, "deny", "Bash", sh(cmd))
    t(g + " (@CodeLead creates the folders too, người quyết 2026-11-26)", "@CodeLead", "allow", "Bash", sh(SCAFFOLD))
    for who in ("@CodeLead", "@GameDesigner"):      # looking is every Leader's, creating is @Manager's
        t(g + " (dry run: every Leader)", who, "allow", "Bash", sh(SCAFFOLD + " --dry-run"))
    for who in ("Wallet", "@Core", "@DesignAdvisor", ""):
        t(g + " (dry run: others)", who, "deny", "Bash", sh(SCAFFOLD + " --dry-run"))

    g = "leaders: a chain of allowed commands"
    CHAIN = "python Management/tools/art.py list; python Management/tools/validate.py; python Management/tools/scaffold.py --dry-run"
    for who in (M, "@CodeLead", "@GameDesigner"):
        for tool in ("Bash", "PowerShell"):
            t(g, who, "allow", tool, sh(CHAIN))
        t(g, who, "allow", "Bash", sh("python Management/tools/validate.py && python Management/tools/gd_status.py"))
        t(g, who, "allow", "Bash", sh("python Management/tools/validate.py;python Management/tools/art.py list"))
    for bad in (CHAIN + "; ls", CHAIN + "; rm -rf UnityProject", "ls; " + CHAIN, CHAIN + " | more", CHAIN + " > out.txt",
                CHAIN + "; $(ls)", CHAIN + ";", "; " + CHAIN, CHAIN.replace("; ", " & "), CHAIN.replace("; ", " || "),
                "; ".join(["python Management/tools/validate.py"] * 7),
                "python Management/tools/validate.py; python Management/tools/../tools/validate.py"):
        t(g + " (refused)", M, "deny", "Bash", sh(bad))
    t(g, "@CodeLead", "allow", "Bash", sh("python Management/tools/validate.py; python Management/tools/scaffold.py"))
    # every bound chat chains since 2026-11-26; an unbound one does not
    for who in ("Wallet", "@Core", "@DesignAdvisor", "@UnityAutoPlay"):
        t(g + " (every role)", who, "allow", "Bash", sh("python Management/tools/validate.py; python Management/tools/art.py list"))
    t(g + " (unbound)", "", "deny", "Bash", sh("python Management/tools/validate.py; python Management/tools/art.py list"))
    # a chain runs every gated part through its own check
    stage_file = Path(FX + "/Management/game-design/stage.json")
    stage_file.parent.mkdir(parents=True, exist_ok=True)
    stage_file.write_text('{"stage": "idea", "at": "2026-11-25 22:00"}')
    SEND = 'python Management/tools/dispatch.py send '
    t(g + " (sends chained)", "@CodeLead", "allow", "Bash",
      sh(SEND + 'Wallet --from CodeLead "a" ; ' + SEND + '@Core --from CodeLead "b"; python Management/tools/validate.py'))
    t(g + " (send as another role)", "@CodeLead", "deny", "Bash", sh("python Management/tools/validate.py; " + SEND + 'Wallet --from Manager "a"'))
    t(g + " (send not its part)", "@GameDesigner", "deny", "Bash", sh("python Management/tools/validate.py; " + SEND + 'Wallet --from GameDesigner "a"'))
    t(g + " (';' inside the text cuts it)", "@CodeLead", "deny", "Bash", sh(SEND + 'Wallet --from CodeLead "a; python Management/tools/validate.py"'))
    stage_file.unlink()
    t(g + " (phase gate holds in a chain)", "@CodeLead", "deny", "Bash", sh("python Management/tools/validate.py; " + SEND + 'Wallet --from CodeLead "a"'))
    REQB = "python Management/tools/gd_req.py "
    t(g + " (others)", "@human", "pass", "Bash", sh(SCAFFOLD))

    g = "manager: reads the design's status"
    STATUS = "python Management/tools/gd_status.py"
    for cmd in (STATUS, STATUS + " ai/idea/feature-hero", STATUS + " ai/idea/feature-hero/hero-list"):
        t(g, M, "allow", "Bash", sh(cmd))
    for cmd in (STATUS + " live", STATUS + " ai/idea/../x", STATUS + " && ls", "python Management/tools/gd_delete.py ai/idea/feature-hero",
                "python Management/tools/gd_progress.py ai/idea/feature-hero idea done"):
        t(g, M, "deny", "Bash", sh(cmd))

    g = "any chat: design pack command"
    PACK = "python Management/tools/gd_pack.py"
    for who in ("Wallet", "_Services/Popup", "@CodeLead", "@GameDesigner", M, "@Core"):
        for cmd in (PACK + " Hero", PACK + " _Services/Popup", "python3 Management/tools/gd_pack.py Wallet"):
            t(g, who, "allow", "Bash", sh(cmd))
    for cmd in (PACK, PACK + " ../x", PACK + " Hero; rm -rf UnityProject", PACK + " Hero && ls", PACK + " Hero --root /tmp",
                PACK + " Hero Wallet", "python Management/tools/../tools/gd_pack.py Hero"):
        pass
    w(g, M, "deny", A + "/../UnityProject/Assets/Features/Hero/Scripts/Internal/.gitkeep")      # still no file edits

    g = "any chat: design frame command"
    FRAME = "python Management/tools/gd_frame.py"
    for who in ("Wallet", "_Services/Popup", "@CodeLead", "@GameDesigner", M):
        for cmd in (FRAME + " ai/idea/feature-hero/hero-list/hero-roster-values", FRAME + " live/feature-popup-lose/lose-actions/actions-spec",
                    "python3 Management/tools/gd_frame.py ai/idea/feature-hero"):
            t(g, who, "allow", "Bash", sh(cmd))
    for cmd in (FRAME, FRAME + " ai/idea", FRAME + " ai/idea/feature-hero --html x.html", FRAME + " ai/idea/../x",
                FRAME + " ai/other/feature-hero", FRAME + " ai/idea/feature-hero && ls", FRAME + " ai/idea/Feature-Hero"):
        pass
    t(g + " (html)", "@human", "pass", "Bash", sh(FRAME + " ai/idea/feature-hero --html x.html"))

    g = "role spelling"
    e(g, "@core", "deny", A + "/Core/Old.cs")          # stored values are exact; bind_session stores "@Core"
    e(g, "@MANAGER", "deny", A + "/Core/Old.cs")
    e(g, "@Cor", "deny", A + "/Core/Old.cs")
    cli(g, "@Manager", "deny", "process: @Manager edits Core", payload("Edit", {"file_path": A + "/Core/Old.cs"}))

    g = "bind: roles"
    say(g, "block, bound=None", "@Core is a worker now", f"FEATURE=@Core {PIN}", session="role-core")
    say(g, "block, bound=None", "@Composition is no role any more", f"FEATURE=@Composition {PIN}", session="role-comp")
    say(g, "block, bound=@Manager", "bind @Manager", f"FEATURE=@Manager {PIN}", session="role-mgr")
    import bind_session
    check(g, "@Manager", True, "the guard line has the scope soft gate",
          "SOFT GATE phạm vi" in bind_session.context({"session_id": "role-mgr"}, FX, STORE))
    say(g, "block, bound=None", "unknown role", f"FEATURE=@Boss {PIN}", session="role-x")


REPORT_OK = """Xong rồi.

## Báo cáo cho @Manager
- Phiên: FEATURE=Wallet
- Việc: Thêm số xu ban đầu vào config
- Trạng thái: xong
- Đã đổi: Wallet/Scripts/Internal/WalletConfig.cs
- Công khai đổi: không
- Cần bên khác: không
- Kiểm tra: validate.py 0 vi phạm; cần người chạy Game.Wallet.Tests
- Ghi chú: không
"""


def run_report_cases():
    import bind_session
    import stop_report
    reports = FX + "/Management/reports"
    human = {"type": "user", "message": {"role": "user", "content": "làm đi"}}
    meta = {"type": "user", "isMeta": True, "message": {"role": "user", "content": "<context>"}}
    result = {"type": "user", "message": {"content": [{"type": "tool_result", "content": "ok"}]}}
    def use(name, **ti): return {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": name, "input": ti}]}}
    def say_text(text): return {"type": "assistant", "message": {"content": [{"type": "text", "text": text}]}}
    edit = use("Edit", file_path="x")
    count = lambda: len(os.listdir(reports)) if os.path.isdir(reports) else 0     # noqa: E731
    for session, value in (("rep-wallet", "Wallet"), ("rep-core", "@Core"), ("rep-human", "@human"), ("rep-mgr", "@Manager"),
                           ("rep-lead", "@CodeLead"), ("rep-gd", "@GameDesigner")):
        Path(guard.binding_path(session, STORE)).write_text(json.dumps({"feature": value}))

    def rep(expect, label, session, entries, active=False, transcript=True, keep_pending=False, same=False):
        pending = stop_report.pending_path(session, STORE)
        if not keep_pending and os.path.exists(pending):
            os.remove(pending)
        path = FX + f"/transcript-{len(results)}.jsonl"
        if transcript:
            lines = "\n".join(json.dumps(x, ensure_ascii=False) for x in entries) + "\n"
            if not same:      # each case its own report: an identical block is not saved twice
                lines = lines.replace("- Ghi chú: không", f"- Ghi chú: ca {len(results)}")
            Path(path).write_text(lines, encoding="utf-8")
        before = count()
        stdin = json.dumps({"session_id": session, "transcript_path": path, "stop_hook_active": active})
        p = subprocess.run([sys.executable, "-I", str(HERE / "stop_report.py"), "--root", FX, "--store", STORE],
                           input=stdin.encode("utf-8"), capture_output=True)
        try:
            got = json.loads(p.stdout)["decision"] if p.stdout.strip() else "pass"
        except Exception:
            got = "garbage"
        check("stop report", "", expect, label, f"{got}, +{count() - before} report")

    rep("pass, +0 report", "turn without edits", "rep-wallet", [human, say_text("đây là câu trả lời")])
    rep("block, +0 report", "edited, no report", "rep-wallet", [human, edit, result, say_text("Xong.")])
    rep("block, +0 report", "edited, report missing lines", "rep-wallet",
        [human, edit, result, say_text("## Báo cáo cho @Manager\n- Phiên: FEATURE=Wallet\n- Việc: x")])
    rep("block, +0 report", "edited, unknown Trạng thái", "rep-wallet",
        [human, edit, result, say_text(REPORT_OK.replace("Trạng thái: xong", "Trạng thái: ổn"))])
    rep("pass, +1 report", "edited, full report", "rep-wallet", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +1 report", "edited, report split over two messages, context entries between", "rep-wallet",
        [human, meta, say_text("Bắt đầu."), edit, result, say_text(REPORT_OK)])
    rep("pass, +1 report", "second stop still without report: let go, note saved", "rep-wallet",
        [human, edit, result, say_text("Xong.")], active=True)
    rep("pass, +0 report", "edit was in an earlier turn", "rep-wallet", [human, edit, result, say_text("x"), human, say_text("y")])
    rep("pass, +0 report", "unity-mcp search is not a change", "rep-wallet",
        [human, use(guard.UNITY_ASSET_TOOL, action="search"), result, say_text("x")])
    rep("block, +0 report", "unity-mcp delete is a change", "rep-wallet",
        [human, use(guard.UNITY_ASSET_TOOL, action="delete", path="Assets/x"), result, say_text("x")])
    rep("pass, +1 report", "@Core with report", "rep-core", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +0 report", "@human is not asked", "rep-human", [human, edit, result, say_text("Xong.")])
    rep("pass, +0 report", "@Manager is not asked", "rep-mgr", [human, edit, result, say_text("Xong.")])
    rep("block, +0 report", "@CodeLead edited the skeleton without a report", "rep-lead",
        [human, edit, result, say_text("Xong.")])
    rep("pass, +1 report", "@CodeLead with report", "rep-lead", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +0 report", "@CodeLead turn without edits", "rep-lead", [human, say_text("Đã kiểm, không lỗi.")])
    rep("block, +0 report", "@GameDesigner edited the design without a report", "rep-gd",
        [human, edit, result, say_text("Xong.")])
    rep("pass, +1 report", "@GameDesigner with report", "rep-gd", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +0 report", "@GameDesigner turn without edits", "rep-gd", [human, say_text("Đã xem gd_status, chưa sửa gì.")])
    Path(guard.binding_path("rep-tester", STORE)).write_text(json.dumps({"feature": "@UnityAutoPlay"}))
    rep("block, +0 report", "a tester wrote a run without a report", "rep-tester", [human, edit, result, say_text("Xong.")])
    rep("pass, +1 report", "a tester with report", "rep-tester", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +0 report", "a tester that only played is not asked", "rep-tester",
        [human, use("mcp__unityMCP__manage_editor", action="play"), result, use("mcp__unityMCP__execute_code", code="x"), result, say_text("Đã chơi.")])
    rep("pass, +0 report", "unbound chat is not asked", "rep-none", [human, edit, result, say_text("Xong.")])
    rep("pass, +0 report", "missing transcript lets the agent stop", "rep-wallet", [], transcript=False)
    # The hook's own message may show up as a new user entry: the pending mark keeps the check on.
    rep("block, +0 report", "sent back for a report", "rep-wallet", [human, edit, result, say_text("Xong.")])
    rep("pass, +1 report", "after a hook message that looks like a prompt, the report is still taken", "rep-wallet",
        [human, edit, result, say_text("Xong."), human, say_text(REPORT_OK)], keep_pending=True)
    rep("block, +0 report", "sent back again", "rep-wallet", [human, edit, result, say_text("Xong.")])
    rep("pass, +1 report", "and when it still writes none, the note is saved", "rep-wallet",
        [human, edit, result, say_text("Xong."), human, say_text("Đã xong rồi mà.")], keep_pending=True)
    rep("pass, +0 report", "the mark is gone after that", "rep-wallet", [human, say_text("hỏi đáp")], keep_pending=True)
    # A message from another session (SendMessage) starts a new turn: the report of the turn before is not taken again.
    peer = {"type": "user", "isMeta": True, "origin": {"kind": "peer"},
            "message": {"role": "user", "content": "Another Claude session sent a message: <cross-session-message>x"}}
    rep("pass, +1 report", "@CodeLead report before a Leader's message", "rep-lead", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +0 report", "turn opened by a peer message, no edits: the old report is not saved again", "rep-lead",
        [human, edit, result, say_text(REPORT_OK), peer, say_text("Đã nhận, chưa sửa gì.")])
    rep("pass, +1 report", "turn opened by a peer message, with edits and its own report", "rep-lead",
        [human, edit, result, say_text(REPORT_OK), peer, edit, result, say_text(REPORT_OK.replace("Thêm số xu", "Sửa số xu"))])
    rep("pass, +1 report", "a report", "rep-lead", [human, edit, result, say_text(REPORT_OK)], same=True)
    rep("pass, +0 report", "the identical block again is not saved twice", "rep-lead",
        [human, edit, result, say_text(REPORT_OK)], same=True)

    # The requirement book: a feature chat that edited its code must have written doing / done (asked once).
    code_edit = use("Edit", file_path=F + "/Wallet/Scripts/Internal/Old.cs")
    req_cmd = use("Bash", command='python Management/tools/gd_req.py Wallet doing wallet-rules#start-50 "viết"')
    rep("block, +1 report", "edited its code, no book write: report saved, asked to write the book", "rep-wallet",
        [human, code_edit, result, say_text(REPORT_OK)])
    rep("pass, +1 report", "asked once only (second stop)", "rep-wallet", [human, code_edit, result, say_text(REPORT_OK)], active=True)
    rep("pass, +1 report", "edited its code and wrote the book", "rep-wallet",
        [human, code_edit, result, req_cmd, result, say_text(REPORT_OK)])
    check("stop report", "", True, "the saved report lists the book writes of the turn",
          any("- Sổ yêu cầu: Wallet doing wallet-rules#start-50" in Path(reports, n).read_text(encoding="utf-8")
              for n in os.listdir(reports)))
    rep("pass, +1 report", "@Core is not asked for a book", "rep-core", [human, edit, result, say_text(REPORT_OK)])
    rep("pass, +1 report", "@CodeLead is not asked for a book", "rep-lead",
        [human, use("Edit", file_path=F + "/Wallet/Scripts/Contracts/IWallet.cs"), result, say_text(REPORT_OK)])
    saved = sorted(os.listdir(reports))
    text = Path(reports, saved[0]).read_text(encoding="utf-8")
    check("stop report", "", True, "saved file holds the block and who wrote it",
          "## Báo cáo cho @Manager" in text and "FEATURE=Wallet" in text and "Xong rồi" not in text)
    first = bind_session.context({"session_id": "rep-mgr"}, FX, STORE)
    leads = [n for n in saved if bind_session.report_role(n) in bind_session.MANAGER_REPORT_ROLES]
    check("stop report", "", True, "@Manager is told about the Leaders' new reports only (2026-11-26)",
          f"{len(leads)} báo cáo mới" in first and all(n in first for n in leads)
          and not any(n in first for n in saved if n not in leads))
    lead_first = bind_session.context({"session_id": "rep-lead"}, FX, STORE)
    check("stop report", "", True, "@CodeLead is told about every new report", all(n in lead_first for n in saved))
    again = bind_session.context({"session_id": "rep-mgr"}, FX, STORE)
    check("stop report", "", True, "and not again on its next prompt", "Không có báo cáo mới" in again)


def run_codelead_cases():
    """@CodeLead edits only the skeleton (Contracts, Base, asmdef, AssemblyInfo of unlocked features; asmdef,
    AssemblyInfo and I*.cs of Core) and all of _Composition, reads every layer of Basic/Features/Services, and uses
    unity-mcp to compile, test, play, look, delete skeleton files and change the assets and scenes of _Composition.
    The owners only read their skeleton and code subclasses."""
    import bind_session
    W, L = "Wallet", "@CodeLead"
    internal = F + "/Wallet/Scripts/Internal/Old.cs"
    u = lambda name, **ti: t(g, L, ti.pop("expect"), "mcp__unityMCP__" + name, ti)      # noqa: E731

    g = "codelead: fixes feature and Core code (soft gate, 2026-11-27)"
    for p in (internal, F + "/Wallet/Scripts/View/V.cs", F + "/Wallet/Tests/T.cs", A + "/Core/Old.cs", A + "/Core/Tests/IFake.cs"):
        w(g, L, "allow", p)
    t(g, L, "allow", "MultiEdit", {"file_path": internal, "edits": []})
    t(g, L, "allow", "NotebookEdit", {"notebook_path": internal})
    reason = guard.decide({"tool_name": "Write", "tool_input": {"file_path": internal, "content": "x"}, "cwd": FX,
                           "session_id": "test-session"}, L, FX, STORE)[1]
    check(g, L, True, "the allow reminds it is for compile fixes", "lỗi biên dịch" in reason)
    w(g + ": not a locked feature's code", L, "deny", F + "/Shop/Scripts/Internal/S.cs")
    g = "codelead: edits nothing else outside the skeleton"
    for p in (F + "/Wallet/FeatureAssets/Private/c.asset",
              F + "/Wallet/FeatureAssets/Public/Icon.prefab", F + "/Wallet/CLAUDE.md",
              FX + "/Management/proposals/decision.md", FX + "/Management/tools/rules.json",
              FX + "/Management/data/x.js", FX + "/.claude/settings.json", FX + "/CLAUDE.md"):
        w(g, L, "deny", p)
    t(g, L, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, L, "allow", "Bash", {"command": VALIDATE})
    t(g, L, "deny", "Bash", {"command": "dotnet build"})
    t(g, L, "deny", "SendMessage", {"to": W, "message": "Sửa giúp ..."})      # the feature runs on demand: dispatch.py send

    g = "codelead: owns _Composition (the wiring)"
    for p in ("/Old.cs", "/Boot.cs", "/Tests/BootTests.cs", "/Tests/IFake.cs", "/Game.Popups.asset",
              "/Game.Composition.asmdef", "/AssemblyInfo.cs", "/IBoot.cs"):
        w(g, L, "allow", A + "/_Composition" + p)
    e(g, L, "allow", A + "/_Composition/Old.cs")
    w(g, L, "deny", A + "/_Composition/x.asmref")
    t(g, L, "deny", "Write", {"file_path": A + "/_Composition/Boot.cs",
                              "content": '[assembly: InternalsVisibleTo("Game.Wallet.View")]'})
    t(g, L, "allow", "Write", {"file_path": A + "/_Composition/AssemblyInfo.cs",
                               "content": '[assembly: InternalsVisibleTo("Game.Composition.Tests")]'})
    t(g, L, "allow", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/_Composition/Old.cs"})
    t(g, L, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/_Composition"})      # a Leader asks through Manager (2026-11-27)
    if not WIN:
        w(g + ": case-sensitive", L, "deny", A + "/_composition/Boot.cs")
    for who in ("Wallet", "@Core", "@Manager", "@GameDesigner", ""):
        w(g + " (others)", who, "deny", A + "/_Composition/Boot.cs")
    e(g + " (others)", "@human", "pass", A + "/_Composition/Old.cs")

    g = "codelead: reads every layer of Basic, Features, Services"
    for p in (internal, F + "/Shop/Scripts/View/V.cs", F + "/Wallet/Tests/T.cs", F + "/Wallet/FeatureAssets/Private/c.asset",
              F + "/_Services/Popup/Scripts/Internal/P.cs", A + "/Core/Old.cs", A + "/_Composition/Old.cs",
              FX + "/Management/reports/r.md"):
        r(g, L, "allow", p)
    t(g, L, "allow", "Grep", {"pattern": "x", "path": F + "/Shop/Scripts/Internal"})
    pack = FX + "/Management/game-design/.pack/pack-Wallet-0123456789ab.md"      # written by gd_pack.py / gd_status.py
    for who in (W, L, "@Core", "_Services/Popup", "@GameDesigner", "@Manager"):
        r("design packs: read by line range", who, "allow", pack)
        t("design packs: read by line range", who, "allow", "Grep", {"pattern": "vòng 4", "path": os.path.dirname(pack)})
        w("design packs: only the tools write them", who, "deny", pack)
        e("design packs: only the tools write them", who, "deny", pack)
    for p in (F, F + "/_Services"):      # one search across every feature, e.g. who uses a contract
        t(g, L, "allow", "Grep", {"pattern": "IPopupService", "path": p})
        t(g, L, "allow", "Glob", {"pattern": "**/*.cs", "path": p})
        for who in (W, "@Core", "_Services/Popup", "@Manager", "@GameDesigner", ""):
            t(g + " (others still may not)", who, "deny", "Grep", {"pattern": "x", "path": p})
    for p in (A, FX + "/UnityProject", FX):
        t(g, L, "deny", "Grep", {"pattern": "x", "path": p})
    e(g, L, "deny", F + "/New.cs")
    for p in (FX + "/UnityProject/Library/x", FX + "/UnityProject/Temp/x", FX + "/secret.txt", r"C:\Windows\x"):
        r(g, L, "deny", p)
    r(g + " (others still may not)", "@Manager", "deny", internal)

    g = "codelead: unity-mcp"
    for name in ("refresh_unity", "read_console", "get_test_job", "find_gameobjects"):
        u(name, expect="allow")
    u("validate_script", expect="allow", uri="mcpforunity://path/Assets/Features/Shop/Scripts/Internal/S.cs")
    u("validate_script", expect="deny")      # needs a uri
    for action in ("play", "pause", "stop"):
        u("manage_editor", expect="allow", action=action)
    u("run_tests", expect="allow", mode="EditMode")
    u("manage_scene", expect="allow", action="get_hierarchy")
    u("manage_asset", expect="allow", action="search", path="Assets")
    u("manage_asset", expect="allow", action="get_info", path="Assets/Features/Shop/FeatureAssets/Private/c.asset")
    u("manage_prefabs", expect="allow", action="get_info", prefab_path="Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    t(g, L, "allow", "ReadMcpResourceTool", {"server": "unityMCP", "uri": "mcpforunity://editor/state"})
    t(g, L, "deny", "ReadMcpResourceTool", {"server": "other", "uri": "x"})
    for name, action in (("manage_editor", "add_tag"), ("manage_editor", "deploy_package"), ("execute_menu_item", ""),
                         ("manage_packages", "add"), ("manage_camera", "list_cameras"), ("manage_gameobject", "create"),
                         ("manage_components", "add"), ("read_console", "clear")):
        u(name, expect="allow", action=action)      # project settings, menus and the scene: @CodeLead, holding the lock
    # screenshots: only into UnityProject/Captures (outside Assets); without a folder they would land in Assets/Screenshots
    u("manage_camera", expect="deny", action="screenshot")
    u("manage_camera", expect="deny", action="screenshot", output_folder="")
    u("manage_camera", expect="deny", action="screenshot_multiview")
    for folder in ("Captures", "Captures/run1", FX + "/UnityProject/Captures", FX + "/UnityProject/Captures/a"):
        u("manage_camera", expect="allow", action="screenshot", output_folder=folder, include_image=True)
    u("manage_camera", expect="allow", action="screenshot_multiview", output_folder="Captures")
    for folder in ("Assets/Screenshots", "Assets/_Composition/Screenshots", "Captures/../Assets", "../Captures",
                   "Assets", "CapturesX", FX + "/Captures", "~/Captures"):
        u("manage_camera", expect="deny", action="screenshot", output_folder=folder)
    t(g, L, "deny", "mcp__unityMCP__batch_execute", {"commands": [
        {"tool": "read_console", "params": {"action": "get"}}, {"tool": "manage_camera", "params": {"action": "screenshot"}}]})
    t(g, L, "allow", "mcp__unityMCP__batch_execute", {"commands": [
        {"tool": "manage_camera", "params": {"action": "screenshot", "output_folder": "Captures"}}]})
    u("manage_scene", expect="allow", action="load", path="Assets/_Composition/Scenes/Game.unity")
    u("manage_scene", expect="allow", action="save")      # a scene of _Composition: its own
    u("manage_asset", expect="allow", action="create", path="Assets/_Composition/Catalog.asset")
    u("manage_prefabs", expect="allow", action="modify_contents", prefab_path="Assets/_Composition/Boot.Game.prefab")
    u("manage_asset", expect="deny", action="create", path="Assets/_Composition/N.cs")      # code: Edit/Write
    u("manage_asset", expect="deny", action="create", path="Assets/Core/n.asset")
    u("manage_scene", expect="allow", action="load", path="Assets/Features/Wallet/FeatureAssets/Private/T.unity")
    u("manage_scene", expect="deny", action="save")      # looks at a feature's scene, never saves it
    for name, action in (("manage_scene", "create"), ("manage_asset", "modify"),
                         ("manage_asset", "create"), ("manage_prefabs", "modify_contents"), ("create_script", ""),
                         ("apply_text_edits", ""), ("execute_code", "execute"), ("script_apply_edits", ""),
                         ("manage_editor", "nonsense"), ("manage_scene", "")):
        u(name, expect="deny", action=action)
    u("manage_asset", expect="deny", action="create", path="Assets/Features/Wallet/FeatureAssets/Private/n.asset")
    for who in ("@Manager", "@GameDesigner"):      # every role uses unity-mcp (người quyết 2026-11-26)
        t(g + " (others)", who, "allow", "mcp__unityMCP__read_console", {"action": "get"})
    t(g + " (others)", "", "deny", "mcp__unityMCP__read_console", {"action": "get"})
    guard.release_lock(STORE)

    g = "codelead: skeleton"
    skeleton = (F + "/Wallet/Scripts/Contracts/IWallet.cs", F + "/Wallet/Scripts/Contracts/New.cs",
                F + "/Wallet/Scripts/Internal/Base/HeroBase.cs", F + "/Wallet/Scripts/View/Base/ViewBase.cs",
                F + "/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef", F + "/Wallet/Tests/Game.Wallet.Tests.asmdef",
                F + "/Wallet/Scripts/Internal/AssemblyInfo.cs", F + "/_Services/Popup/Scripts/Contracts/IPopup.cs",
                F + "/_Services/Popup/Scripts/Internal/Base/PopupBase.cs",
                A + "/Core/Game.Core.asmdef", A + "/Core/AssemblyInfo.cs", A + "/Core/IServiceRegistry.cs",
                A + "/Core/SessionParticipantBase.cs", A + "/Core/ISessionScene.cs",
                A + "/Core/Tests/Game.Core.Tests.asmdef", A + "/_Composition/AssemblyInfo.cs", A + "/_Composition/IBoot.cs")
    for p in skeleton:
        e(g, L, "allow", p)
    t(g, L, "allow", "Write", {"file_path": F + "/Wallet/Scripts/Internal/AssemblyInfo.cs",
                               "content": '[assembly: InternalsVisibleTo("Game.Wallet.View")]'})
    for p in (F + "/Shop/Scripts/Contracts/IShop.cs", F + "/Shop/Scripts/Internal/Base/B.cs",
              F + "/Shop/Scripts/Internal/Game.Shop.Internal.asmdef"):
        e(g + ": locked feature (code gate)", L, "deny", p)
    for p in (F + "/Wallet/Scripts/Contracts.cs", A + "/Core/Icon.png"):
        e(g + ": not skeleton", L, "deny", p)
    for p in (F + "/Wallet/Scripts/Internal/Basement.cs", F + "/Wallet/Scripts/Internal/Sub/Base/X.cs",
              A + "/Core/ServiceRegistry.cs", A + "/Core/Ix.cs"):
        e(g + ": not skeleton but code (soft gate, 2026-11-27)", L, "allow", p)
    e(g, L, "deny", F + "/Wallet/Scripts/Internal/x.asmref")
    if not WIN:
        e(g + ": case-sensitive", L, "deny", F + "/Wallet/scripts/contracts/IWallet.cs")
        e(g + ": case-sensitive, code of Internal (soft gate)", L, "allow", F + "/Wallet/Scripts/Internal/base/B.cs")
    dl = lambda expect, path: t(g, L, expect, guard.UNITY_ASSET_TOOL, {"action": "delete", "path": path})  # noqa: E731
    dl("allow", "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs")
    dl("allow", "Assets/Features/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    # any file or folder of any feature or service, the whole feature too, no code gate (decided 2026-11-27)
    os.makedirs(F + "/Shop/Scripts/Internal", exist_ok=True)
    os.makedirs(F + "/_Services/Popup", exist_ok=True)
    Path(F + "/Wallet/Scripts/Internal/Old.cs").write_text("x")
    Path(F + "/Shop/Scripts/Internal/S.cs").write_text("x")
    dl("allow", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    dl("allow", "Assets/Features/Shop/Scripts/Internal/S.cs")         # Shop's code gate is closed: deleting still goes
    dl("allow", "Assets/Features/_Services/Popup")
    dl("deny", "Assets/Features/Wallet/Scripts/Internal/x.asmref")
    dl("allow", "Assets/Features/Wallet/Scripts/Contracts")            # a skeleton folder is @CodeLead's own: no question
    os.makedirs(F + "/Wallet/Scripts/Internal/Base", exist_ok=True)
    os.makedirs(A + "/_Composition/Scenes", exist_ok=True)
    dl("allow", "Assets/Features/Wallet/Scripts/Internal/Base")
    dl("allow", "Assets/_Composition/Scenes")
    dl("deny", "Assets/_Composition")                                   # the whole wiring area: the human decides
    dl("deny", "Assets/Features/Wallet/Scripts/Contracts/Missing")      # nothing there: not a folder it can be sure of
    dl("allow", "Assets/Features/Wallet/Scripts/Internal")
    dl("allow", "Assets/Features/Wallet")
    dl("deny", "Assets/Features/Shop/Scripts/Internal/Base")            # nothing there: not a folder it can be sure of
    for top in ("Assets/Features", "Assets/Features/_Services"):
        dl("deny", top)
    dl("deny", "Assets/Core")
    t(g, L, "deny", guard.UNITY_ASSET_TOOL, {"action": "modify", "path": "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs"})

    g = "feature: skeleton read-only"
    for who in ("@Manager", "@GameDesigner", ""):
        e(g + " (others)", who, "deny", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g + " (human)", "@human", "pass", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g + " (human)", "@human", "pass", A + "/Core/IServiceRegistry.cs")
    for p in (A + "/Core/Old.cs", A + "/_Composition/Old.cs"):
        pass

    g = "codelead: bind and reports"
    say(g, "block, bound=@CodeLead", "bind @CodeLead", f"FEATURE=@codelead {PIN}", session="role-lead")
    say(g, "pass, bound=@CodeLead", "@CodeLead is told its job", "làm đi", session="role-lead")
    reports = FX + "/Management/reports"
    os.makedirs(reports, exist_ok=True)
    Path(reports, "20990101-000000-Shop.md").write_text("## Báo cáo cho @Manager\n- Phiên: FEATURE=Shop\n- Việc: x\n"
                                                        "- Trạng thái: xong\n", encoding="utf-8")
    lead = bind_session.context({"session_id": "role-lead"}, FX, STORE)
    check(g, L, True, "@CodeLead is told about new reports", "20990101-000000-Shop.md" in lead)
    again = bind_session.context({"session_id": "role-lead"}, FX, STORE)
    check(g, L, True, "and not again", "Không có báo cáo mới" in again)
    Path(reports, "20990101-000001-GameDesigner.md").write_text("## Báo cáo cho @Manager\n- Phiên: FEATURE=@GameDesigner\n"
                                                                "- Việc: x\n- Trạng thái: xong\n", encoding="utf-8")
    mgr = bind_session.context({"session_id": "rep-mgr"}, FX, STORE)
    check(g, "@Manager", True, "@Manager keeps its own read list, of the Leaders' reports only",
          "20990101-000001-GameDesigner.md" in mgr and "20990101-000000-Shop.md" not in mgr)
    check(g, "", ["BÁO CHẶN"] * 3, "the Leaders are told to tell @Manager when they are blocked, and @Manager to plan again",
          [x for told in (lead, mgr, bind_session.context({"session_id": "rep-gd"}, FX, STORE)) for x in ["BÁO CHẶN"] if x in told])


def run_designer_cases():
    """@GameDesigner reads only Management/game-design and its own rules file, writes only in .../ai, deletes
    there with gd_delete.py, never writes the human's answers. Nobody else writes in ai; live is the human's
    (a human session is asked). In mode user a node is writable only when every node above it is approved."""
    import bind_session
    W, D = "Wallet", "@GameDesigner"
    base = FX + "/Management/game-design"
    ai, live = base + "/ai", base + "/live"
    idea = ai + "/idea"
    choose = "/.ai-need-user-choose"
    for d in (idea + "/feature-hero/.images", live + "/feature-hero", FX + "/Management/tools"):
        os.makedirs(d, exist_ok=True)
    # The gate is gamedesign.can_write_node; the guard loads it from the project it guards. The test loads the
    # same file under another name, only to compute hashes for its fixture.
    source = HERE.parent.parent / "Management" / "tools" / "gamedesign.py"
    shutil.copy2(source, FX + "/Management/tools/gamedesign.py")
    shutil.copy2(source.with_name("gd_excel.py"), FX + "/Management/tools/gd_excel.py")
    spec = importlib.util.spec_from_file_location("gamedesign_for_fixture", source)
    gd = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gd)

    def node(folder, ai=8, based="", idea_text="Một ý."):
        """Writes data.md (one idea with the AI's score) and CLAUDE.md; returns the data hash."""
        head = f"---\nbased-on: {based}\n---\n" if based else ""
        os.makedirs(folder, exist_ok=True)
        Path(folder + "/data.md").write_text(f"{head}# x\n\nTóm tắt.\n\n## Ý cốt lõi\n\n### one | AI {ai}\n{idea_text}\n", encoding="utf-8")
        Path(folder + "/CLAUDE.md").write_text("# x\n\ndata.md\n", encoding="utf-8")
        return gd.read_data(Path(folder))["hash"]

    def score(folder, value):
        """The human's score for the idea, as the page would store it."""
        item = gd.read_data(Path(folder))["items"][0]
        os.makedirs(folder + "/.user-review", exist_ok=True)
        Path(folder + "/.user-review/review.json").write_text(json.dumps({"items": {"one": {"score": value, "hash": item["hash"]}}}))

    root_hash = node(idea)
    node(idea + "/feature-hero", based=root_hash)
    node(live)
    DELETE = "python Management/tools/gd_delete.py "
    STATUS = "python Management/tools/gd_status.py"

    g = "designer: writes only in ai"
    e(g, D, "allow", idea + "/data.md")
    e(g, D, "allow", idea + "/CLAUDE.md")
    w(g, D, "allow", idea + "/feature-hero/skills/data.md")
    w(g, D, "allow", idea + "/feature-hero/.images/index.md")
    w(g, D, "allow", idea + choose + "/questions.json")
    w(g, D, "allow", idea + "/feature-hero/priority.json")      # its own importance score of a node
    w(g, D, "deny", idea + "/feature-hero/.user-review/priority.json")      # the human's importance score
    w(g, D, "allow", ai + "/notes.md")
    for p in (live + "/node.md", live + "/feature-new/node.md", base + "/readme.md", base + "/other/node.md",
              base + "/mode.json", base + "/idea/node.md",
              FX + "/Management/proposals/gd.md", FX + "/Management/data/idea.js", FX + "/Management/tools/rules.json",
              F + "/Wallet/Scripts/Internal/W.cs", A + "/Core/Base.cs", FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/skills/gd-round-2/SKILL.md",
              FX + "/CLAUDE.md", idea + "/../../live/node.md", FX + "/Management/game-design-x/ai/node.md"):
        w(g, D, "deny", p)
    w(g, D, "deny", idea + "/x.asmref")
    for p in (idea + choose + "/answers.json", idea + "/feature-hero" + choose + "/answers.json",
              idea + choose + "/ANSWERS.JSON", idea + "/.user-review/review.json", idea + "/feature-hero/.user-review/review.json",
              idea + "/.USER-REVIEW/x.json", idea + "/.history/log.jsonl", idea + "/.history/state.json",
              idea + "/feature-hero/.history/removed/x/log.jsonl", ai + "/.history/log.jsonl",
              idea + "/feature-hero/.progress/progress.json", idea + "/.PROGRESS/x.json"):
        w(g + " (never the human's files or the history)", D, "deny", p)
        e(g + " (never the human's files or the history)", D, "deny", p)
    t(g, D, "deny", "MultiEdit", {"file_path": live + "/node.md", "edits": []})
    t(g, D, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, D, "allow", "mcp__unityMCP__refresh_unity", {})
    t(g, D, "pass", "SendMessage", {"to": "@Manager", "message": "x"})

    g = "designer: the config tables of a node (.excel)"
    X = "python Management/tools/gd_excel.py "
    for p in (idea + "/feature-hero/.excel/Heroes.csv", idea + "/feature-hero/.excel/Index.csv"):
        w(g, D, "allow", p)
    for p in (idea + "/feature-hero/.excel/feature-hero.xlsx", idea + "/feature-hero/.excel/.sync.json",
              idea + "/feature-hero/.excel/notes.md", idea + "/feature-hero/.excel/sub/Heroes.csv", live + "/.excel/Heroes.csv"):
        w(g + " (only the csv of a table, only in ai)", D, "deny", p)
    for who in ("Wallet", "@CodeLead", "@UIDesigner", "@Manager"):
        w(g + " (nobody else)", who, "deny", idea + "/feature-hero/.excel/Heroes.csv")
    for cmd in (X + 'new ai/idea/feature-hero/hero-list/hero-roster-values Heroes list "Danh sách hero"', X + "sync ai/idea/feature-hero",
                X + "sync ai/idea/feature-hero --from xlsx", X + "reindex ai/idea/feature-hero Heroes", X + "sync ai/idea"):
        t(g, D, "allow", "Bash", {"command": cmd})
        for who in ("Wallet", "@CodeLead", "@UIDesigner", "@Manager"):
            t(g + " (only @GameDesigner changes them)", who, "deny", "Bash", {"command": cmd})
    for cmd in (X + 'new live/feature-hero Heroes list "x"', X + 'new ai/idea/feature-hero Heroes list "x"; ls', X + "sync live/feature-hero",
                X + 'new ai/idea/feature-hero Heroes list "x $(rm -rf .)"', X + "sync ai/idea/../live", X + "sync ai/idea/feature-hero --from both",
                X + "reindex ai/idea/feature-hero ../x"):
        t(g, D, "deny", "Bash", {"command": cmd})
    for who in (D, "Wallet", "@CodeLead", "@UIDesigner", "@Manager", "@Core"):
        for cmd in (X + "show live/feature-hero/hero-list/hero-roster-values", X + "show ai/idea/feature-hero Heroes", X + "check",
                    X + "check ai/idea/feature-hero"):
            t(g + " (every chat looks)", who, "allow", "Bash", {"command": cmd})
        t(g + " (every chat looks)", who, "deny", "Bash", {"command": X + "show ../x"})

    g = "designer: reads only the design tree"
    for p in (idea + "/data.md", live + "/data.md", idea + "/feature-hero/.images/a.png", base, base + "/mode.json",
              idea + choose + "/answers.json", idea + "/.user-review/review.json", idea + "/.history/log.jsonl",
              FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/skills/gd-round-1/SKILL.md",
              FX + "/.claude/skills/gd-round-4/SKILL.md", FX + "/.claude/skills/gd-rework-node/notes/a.md",
              FX + "/.claude/skills/gd-integrate-advice/SKILL.md",
              FX + "/.claude/rules/designadvisor.md"):
        r(g, D, "allow", p)
    r(g, D, "allow" if WIN else "deny", FX + "/.claude/rules/GameDesigner.md")      # same file only on Windows
    r(g, D, "allow" if WIN else "deny", FX + "/.claude/Skills/gd-round-1/SKILL.md")
    for p in (FX + "/.claude/rules/codelead.md", FX + "/.claude/rules/gamedesigner.round5.md", FX + "/.claude/rules/gamedesigner.md.bak",
              FX + "/.claude/rules/sub/gamedesigner.md", FX + "/.claude/hooks/gamedesigner.md",
              FX + "/.claude/skills/ui-frame-syntax/SKILL.md", FX + "/.claude/skills/gd-round-1",
              FX + "/.claude/skills/gd-round-1/../codelead-x/SKILL.md", FX + "/.claude/skills/gd-/SKILL.md",
              FX + "/.claude/skills/x/gd-round-1/SKILL.md", FX + "/.claude/skills/advisor-ask-gpt/SKILL.md",
              FX + "/.claude/skills/codelead-resolve-issue/SKILL.md"):
        r(g + " (of the rules only its own)", D, "deny", p)
    t(g, D, "allow", "Glob", {"pattern": "**/node.md", "path": base})
    t(g, D, "allow", "Grep", {"pattern": "hero", "path": live})
    for p in (FX + "/Management/proposals/old.md", FX + "/Management/reports/r.md", FX + "/Management/tools/validate.py",
              FX + "/Management", FX + "/CLAUDE.md", FX + "/.claude/rules/codelead.md", FX + "/.claude/hooks/guard.py",
              F + "/Wallet/Scripts/Contracts/IWallet.cs", F + "/Wallet/CLAUDE.md", A + "/Core/Base.cs",
              FX + "/UnityProject/Library/x", r"C:\Windows\x", base + "/../proposals/old.md"):
        r(g, D, "deny", p)
    t(g, D, "deny", "Grep", {"pattern": "x"})
    t(g, D, "deny", "Glob", {"pattern": "**/*.md", "path": FX + "/Management"})

    g = "designer: the others"
    for who in (W, "@Manager", "@CodeLead", "@Core", ""):
        w(g, who, "deny", idea + "/feature-x/data.md")
        e(g, who, "deny", idea + "/data.md")
        w(g, who, "deny", idea + choose + "/answers.json")
        w(g, who, "deny", idea + "/.user-review/review.json")
        w(g, who, "deny", live + "/data.md")
        w(g, who, "deny", base + "/mode.json")
    for who in (W, "@Manager", "@CodeLead"):
        r(g, who, "allow", live + "/data.md")
        r(g, who, "allow", idea + "/data.md")
    w(g, "@human", "pass", idea + "/data.md")
    w(g, "@human", "pass", live + "/data.md")
    e(g, "@human", "pass", live + "/feature-hero/data.md")

    g = "designer: delete command"
    for path in ("ai/idea/feature-hero", "ai/idea/feature-hero/node.md", "ai/idea/feature-hero/.images/Hero_01.png", "ai/notes.md"):
        t(g, D, "allow", "Bash", {"command": DELETE + path})
    t(g, D, "allow", "PowerShell", {"command": DELETE + "ai/idea/feature-hero"})
    for path in ("ai", "ai/", "idea/feature-hero", "live/node.md", "ai/../live/node.md", "ai/a/../../live", "ai/..", "../ai/a",
                 "ai/a b", "ai/a; rm -rf .", "ai/a && del x", "ai/$(x)", "ai/a\nrm x", "ai\\a",
                 "ai/a --root C:/x", "C:/x", "ai/a ai/b"):
        t(g, D, "deny", "Bash", {"command": DELETE + path})
    t(g, D, "deny", "Bash", {"command": "python Management/tools/gd_delete.py"})
    t(g, D, "deny", "Bash", {"command": "python Management/tools/gamedesign.py ai/a"})
    t(g, D, "deny", "Bash", {"command": "rm -rf Management/game-design/ai"})
    t(g, D, "allow", "Bash", {"command": VALIDATE})
    for who in (W, "@Manager", "@CodeLead", "@Core", ""):
        t(g + " (others)", who, "deny", "Bash", {"command": DELETE + "ai/idea/feature-hero"})

    g = "designer: status command"
    for cmd in (STATUS, STATUS + " ai/idea", STATUS + " ai/idea/feature-hero", STATUS + " ai/idea/feature-hero/skills/cool-down"):
        t(g, D, "allow", "Bash", {"command": cmd})
    for tail in (" live", " ai", " ai/idea/..", " ai/idea/../live", " ai/idea/Feature", " ai/idea/a b", " ai/idea/a; ls",
                 " --root C:/x", " ai/idea/.history", " ai/idea ai/idea"):
        t(g, D, "deny", "Bash", {"command": STATUS + tail})
    for who in (W, "@Core", ""):      # @CodeLead and @Manager may run it too: see "codelead: progress command", "manager: reads the design's status"
        t(g + " (others)", who, "deny", "Bash", {"command": STATUS})

    g = "designer: write gate"
    hero_dir = idea + "/feature-hero"
    hero, skills = hero_dir + "/data.md", hero_dir + "/skills/data.md"
    questions = idea + choose + "/questions.json"
    # ---- mode auto: every node above must pass (AI's score unless the human scored; based-on = the parent's hash)
    w(g + " (auto)", D, "allow", skills)
    node(idea, ai=3)
    w(g + " (auto)", D, "deny", hero)                           # the root's idea has an AI score of 3
    w(g + " (auto)", D, "deny", skills)
    w(g + " (auto)", D, "allow", idea + "/data.md")            # the failing node itself is redone in place
    w(g + " (auto)", D, "allow", idea + "/CLAUDE.md")
    score(idea, 7)
    w(g + " (auto)", D, "allow", hero)                          # the human's 7 overrides the AI's 3
    score(idea, 2)
    node(idea, ai=9)
    w(g + " (auto)", D, "deny", hero)                           # the human's 2 overrides the AI's 9
    new_hash = node(idea, ai=9, idea_text="Viết lại ý.")
    w(g + " (auto)", D, "allow", hero)                          # rewritten idea: the old human score no longer applies
    w(g + " (auto)", D, "deny", skills)                         # feature-hero was written against the old root data
    w(g + " (auto)", D, "allow", hero_dir + "/CLAUDE.md")
    node(hero_dir, based=new_hash)
    w(g + " (auto)", D, "allow", skills)
    w(g + " (auto)", D, "allow", hero_dir + "/skills/.images/index.md")
    U = "@UIDesigner"
    w(g + " (frames are @UIDesigner's)", D, "deny", hero_dir + "/skills/.frame-concept/main.frame")
    w(g + " (auto)", D, "allow", hero_dir + "/skills/ui.md")      # the list of UI is @GameDesigner's
    w(g + " (frames are @UIDesigner's)", U, "allow", hero_dir + "/skills/.frame-concept/main.frame")
    e(g + " (frames are @UIDesigner's)", U, "allow", hero_dir + "/skills/.frame-concept/pause-popup.frame")
    for p in (skills, hero_dir + "/skills/ui.md", hero_dir + "/skills/CLAUDE.md", hero_dir + "/skills/.frame-concept/Main.frame",
              hero_dir + "/skills/.frame-concept/main.txt", hero_dir + "/skills/.frame-concept/sub/main.frame",
              hero_dir + "/skills/.images/index.md", hero_dir + "/skills/.user-review/review.json",
              live + "/feature-hero/.frame-concept/main.frame", ai + "/idea/.frame-concept/../data.md"):
        w(g + " (@UIDesigner: frames only)", U, "deny", p)
    node(hero_dir, based="")
    w(g + " (auto)", D, "deny", skills)                         # no based-on at all
    w(g + " (auto)", U, "deny", hero_dir + "/skills/.frame-concept/main.frame")      # a frame follows its node's lock
    node(hero_dir, based=new_hash)
    os.remove(idea + "/data.md")
    w(g + " (auto)", D, "deny", hero)                           # the root has no data.md
    w(g + " (auto)", D, "allow", idea + "/data.md")
    node(idea, ai=9, idea_text="Viết lại ý.")
    def chot(node_dir, path):      # the human's Chốt (người quyết 2026-11-26): approved.json holds the content hash
        gdm = importlib.util.module_from_spec(importlib.util.spec_from_file_location("gd_for_test", FX + "/Management/tools/gamedesign.py"))
        gdm.__spec__.loader.exec_module(gdm)
        held = gdm.read_approvals(FX)
        held[path] = {"hash": gdm.own_hash(Path(node_dir)), "at": "", "by": "test"}
        gdm.write_approvals(FX, held)
    # ---- mode user: the human scored every idea, no open question, and the node is the same in live
    Path(base + "/mode.json").write_text('{"mode": "user"}')
    w(g + " (user)", D, "deny", hero)                           # the human has not scored the root
    score(idea, 8)
    w(g + " (user)", D, "deny", hero)                           # scored, but live differs
    chot(idea, "")
    w(g + " (user)", D, "allow", hero)
    os.makedirs(idea + choose)
    Path(questions).write_text('{"questions": [{"id": "q1", "text": "?", "options": [{"id": "a", "label": "A"}]}]}')
    w(g + " (user)", D, "deny", hero)                           # an unanswered question
    w(g + " (user)", D, "allow", questions)
    Path(idea + choose + "/answers.json").write_text('{"answers": {"q1": {"chosen": ["a"], "other": ""}}}')
    w(g + " (user)", D, "allow", hero)
    w(g + " (user)", D, "deny", skills)                         # feature-hero: unscored and not in live
    score(hero_dir, 6)
    chot(hero_dir, "feature-hero")
    w(g + " (user)", D, "allow", skills)
    score(hero_dir, 4)
    w(g + " (user)", D, "deny", skills)                         # a human 4 does not pass
    score(hero_dir, 6)
    Path(idea + "/CLAUDE.md").write_text("# x\n\ndata.md\n\nSửa.\n", encoding="utf-8")
    w(g + " (user)", D, "deny", hero)                           # the root now differs from live
    w(g + " (user)", D, "deny", skills)
    w(g + " (user)", D, "allow", idea + "/data.md")
    w(g + " (user)", D, "allow", ai + "/notes.md")             # outside the tree: not a node
    w(g + " (the human is not gated)", "@human", "pass", skills)
    chot(idea, "")
    w(g + " (user)", D, "allow", skills)
    os.rename(FX + "/Management/tools/gamedesign.py", FX + "/Management/tools/gamedesign.off")
    sys.modules.pop("gamedesign", None)
    w(g + " (gate code missing: closed)", D, "deny", skills)
    w(g + " (gate code missing: closed)", D, "deny", idea + "/data.md")
    os.rename(FX + "/Management/tools/gamedesign.off", FX + "/Management/tools/gamedesign.py")
    importlib.invalidate_caches()      # this process just failed to import it; a real hook is a new process each time
    score(idea, 1)
    cli(g + " (real process)", D, "deny", "Write a round-2 node while the root fails",
        json.dumps({"tool_name": "Write", "tool_input": {"file_path": hero}, "cwd": FX}))
    cli(g + " (real process)", D, "allow", "Write the root node",
        json.dumps({"tool_name": "Write", "tool_input": {"file_path": idea + "/data.md"}, "cwd": FX}))

    g = "designer: bind"
    say(g, "block, bound=@GameDesigner", "bind @GameDesigner", f"FEATURE=@gamedesigner {PIN}", session="role-gd")
    say(g, "pass, bound=@GameDesigner", "@GameDesigner is told its job", "làm đi", session="role-gd")
    told = bind_session.context({"session_id": "role-gd"}, FX, STORE)
    check(g, D, True, "the context names ai, live, the rules file, both commands and mode user",
          all(s in told for s in ("game-design/ai", "live", "gamedesigner.md", "gd_delete.py", "gd_status.py", "User control")))
    os.remove(base + "/mode.json")
    told = bind_session.context({"session_id": "role-gd"}, FX, STORE)
    check(g, D, True, "without a mode file the context says Full Auto", "Full Auto" in told and "User control" not in told)


def run_unity_leader_cases():
    """Every bound role uses unity-mcp (người quyết 2026-11-26), with no check of the Editor instance a call names;
    the zones and the Editor lock still hold. A chat with no FEATURE does not."""
    g = "unity: every role"
    for who in ("@Manager", "@GameDesigner", "@UIDesigner", "@DesignAdvisor", "@CodeLead"):
        t(g + " (reads, no instance named)", who, "allow", "mcp__UnityMCP__read_console", {"action": "get"})
        t(g + " (any instance named)", who, "allow", "mcp__UnityMCP__read_console", {"action": "get", "unity_instance": "Other@1234abcd"})
    for who in ("@Manager", "@GameDesigner", "@UIDesigner", "@DesignAdvisor"):
        t(g + " (no area in Assets: no write)", who, "deny", "mcp__UnityMCP__manage_asset",
          {"action": "create", "path": "Assets/Features/Wallet/FeatureAssets/Public/x.mat", "asset_type": "Material"})
    t(g + " (no FEATURE)", "", "deny", "mcp__UnityMCP__read_console", {"action": "get"})


def run_fail_cases():
    """Người quyết 2026-11-26: unity-mcp lỗi kết nối, hay Codex lỗi: dừng, không thử lại trong lượt, báo một lần.
    tool_fail.py flags the session from the failed result; guard refuses the retries; a new turn clears the flags."""
    import subprocess
    g = "fail: stop, no retry"
    hook = str(HERE / "tool_fail.py")

    def post(event, tool, ti, result, session):
        data = {"hook_event_name": event, "tool_name": tool, "tool_input": ti, "session_id": session}
        data["error" if event == "PostToolUseFailure" else "tool_response"] = result
        p = subprocess.run([sys.executable, "-I", hook, "--store", STORE], input=json.dumps(data).encode("utf-8"),
                           capture_output=True)
        return p.stdout.decode("utf-8")

    def bind(session, value):
        os.makedirs(os.path.join(STORE, "sessions"), exist_ok=True)
        with open(guard.binding_path(session, STORE), "w", encoding="utf-8") as f:
            json.dump({"feature": value}, f)
    read = "mcp__UnityMCP__read_console"
    for s, v in (("fail-w", "Wallet"), ("fail-lead", "@CodeLead"), ("fail-adv", "@DesignAdvisor"),
                 ("fail-human", "@human")):
        bind(s, v)
        guard.clear_fails(s, STORE)
    out = post("PostToolUseFailure", read, {"action": "get"}, "Invalid value for 'count': must be a number", "fail-w")
    out = post("PostToolUseFailure", read, {"action": "get"},
               "Instance 'UnityProject@638c2683' not found. Available: Hitsort@a573ff35.", "fail-w")
    out = post("PostToolUse", read, {"action": "get"}, {"success": False, "error": "Connection refused"}, "fail-lead")
    check(g, "@CodeLead", True, "a result with success false and a connection error flags too",
          guard.read_fail("fail-lead", STORE, "unity") is not None)
    t(g + " (CodeLead's retry refused)", "@CodeLead", "deny", "mcp__UnityMCP__refresh_unity", {}, session="fail-lead")
    post("PostToolUseFailure", read, {"action": "get"}, "Connection refused", "fail-human")
    check(g, "@human", None, "the human's chat is never flagged", guard.read_fail("fail-human", STORE, "unity"))

    advise = "python Management/tools/gd_advise.py feature-hero q1"
    out = post("PostToolUse", "Bash", {"command": advise},
               {"stdout": "CODEX LỖI: Codex không trả lời (mã 1): stream error. DỪNG: không chạy lại"}, "fail-adv")
    check(g, "@DesignAdvisor", (True, True), "a command that printed CODEX LỖI: flagged, told to stop",
          (guard.read_fail("fail-adv", STORE, "codex") is not None, "CODEX LỖI" in out))
    t(g + " (gd_advise.py again refused)", "@DesignAdvisor", "deny", "Bash", {"command": advise}, session="fail-adv")
    out = post("PostToolUse", "Bash", {"command": "python Management/tools/validate.py"}, {"stdout": "ok"}, "fail-w")

    for s in ("fail-w", "fail-adv"):
        p = subprocess.run([sys.executable, "-I", str(HERE / "bind_session.py"), "--root", FX, "--store", STORE],
                           input=json.dumps({"session_id": s, "prompt": "Unity đã mở lại"}).encode("utf-8"), capture_output=True)


def run_inbox_cases():
    """The inbox of the chats that run on demand (dispatch.py): who sends to whom, and SendMessage to such a chat."""
    g = "inbox: dispatch.py"
    D = 'python Management/tools/dispatch.py '

    def sh(f, x, cmd): t(g, f, x, "Bash", {"command": cmd})
    # @CodeLead hands out development work only once the human has set the phase the project is in (stage.json);
    # @Manager sends to no inbox since 2026-11-26
    stage_file = Path(FX + "/Management/game-design/stage.json")
    stage_file.parent.mkdir(parents=True, exist_ok=True)
    for text in (None, "{}", '{"stage": "later"}', "not json"):
        if text is None:
            stage_file.unlink(missing_ok=True)
        else:
            stage_file.write_text(text)
        t(g + " (no stage set: no development work)", "@CodeLead", "deny", "Bash", {"command": D + 'send Wallet --from CodeLead "x"'})
        t(g + " (no stage set: no development work)", "@CodeLead", "deny", "Bash", {"command": D + 'send @Core --from CodeLead "x"'})
        t(g + " (no stage set: the tester still gets work)", "@CodeLead", "allow", "Bash", {"command": D + 'send @UnityAutoPlay --from CodeLead "x"'})
    stage_file.write_text('{"stage": "idea", "at": "2026-11-25 22:00"}')
    t(g + " (stage set)", "@CodeLead", "allow", "Bash", {"command": D + 'send @Core --from CodeLead "x"'})
    for who in ("Wallet", "@Core", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@DesignAdvisor", "@UnityAutoPlay"):
        sh(who, "allow", D + "list")
        sh(who, "allow", D + "list Wallet")
    for target in ("Wallet", "_Services/Popup", "@Core", "@DesignAdvisor", "@UnityAutoPlay"):
        sh("@Manager", "deny", D + 'send ' + target + ' --from Manager "x"')      # @Manager keeps to the Leaders
    sh("@CodeLead", "allow", D + 'send Wallet --from CodeLead "Làm yêu cầu feature-wallet/x#y"')
    sh("@CodeLead", "allow", D + 'send _Services/Popup --from @CodeLead "Việc ... với dấu chấm"')
    sh("@CodeLead", "allow", D + 'send Shop --from CodeLead "Sửa lỗi biên dịch ở Shop"')
    sh("@CodeLead", "allow", D + 'send @Core --from CodeLead "x"')
    sh("@CodeLead", "deny", D + 'send @DesignAdvisor --from CodeLead "x"')      # @DesignAdvisor is @GameDesigner's
    sh("@GameDesigner", "allow", D + 'send @DesignAdvisor --from GameDesigner "Hỏi GPT về node feature-hero"')
    sh("@GameDesigner", "deny", D + 'send Wallet --from GameDesigner "x"')
    sh("@UIDesigner", "deny", D + 'send @DesignAdvisor --from UIDesigner "x"')
    sh("@CodeLead", "deny", D + 'send Wallet --from Manager "x"')             # --from must be itself
    sh("@Manager", "deny", D + 'send @CodeLead --from Manager "x"')           # a Leader is messaged with SendMessage
    for lead in ("CodeLead", "GameDesigner", "UIDesigner"):                   # these Leaders open the tester through its inbox
        sh("@" + lead, "allow", D + 'send @UnityAutoPlay --from ' + lead + ' "Chơi tiếp R1, B1"')
        sh("@" + lead, "allow", D + "list @UnityAutoPlay")
    for who in ("Wallet", "_Services/Popup", "@Core", "@DesignAdvisor", "@UnityAutoPlay"):
        sh(who, "deny", D + 'send @UnityAutoPlay --from ' + who.lstrip("@") + ' "x"')
    sh("@CodeLead", "deny", D + 'send Wallet --from CodeLead "x $(rm -rf .)"')
    sh("@CodeLead", "deny", D + 'send ../Wallet --from CodeLead "x"')
    sh("@CodeLead", "deny", D + 'send Wallet --from CodeLead "x"; rm -rf Assets')
    sh("@CodeLead", "allow", D + "cancel m20261125-174601-ecb94c")
    sh("@Manager", "deny", D + "cancel m20261125-174601-ecb94c")
    g = "inbox: SendMessage to a chat that runs on demand"
    for who in ("@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "Wallet", "@DesignAdvisor"):
        for to in ("Wallet", "_Services/Popup", "Core", "DesignAdvisor"):
            t(g, who, "deny", "SendMessage", {"to": to, "message": "x"})
        for to in ("CodeLead", "GameDesigner", "UIDesigner"):
            t(g + " (Leaders as before)", who, "pass", "SendMessage", {"to": to, "message": "x"})
        leader = who in ("@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner")
        t(g + " (only the Leaders reach Manager)", who, "pass" if leader else "deny", "SendMessage", {"to": "Manager", "message": "x"})
    t(g + " (the human, and the page's unbound relay helper)", "@human", "pass", "SendMessage", {"to": "Wallet", "message": "x"})
    t(g + " (the human, and the page's unbound relay helper)", "", "pass", "SendMessage", {"to": "Wallet", "message": "x"})


def run_tester_cases():
    """The testers (@UnityAutoPlay): read all the code, edit nothing in the project, write only Tester/<name> (a README,
    runs named by their time, data under images/), play through unity-mcp and change nothing there. Tester/ is read
    by the Leaders and the code tiers; an Archive only by the testers."""
    import bind_session
    T, W, L, M = "@UnityAutoPlay", "Wallet", "@CodeLead", "@Manager"
    home = FX + "/Tester/UnityAutoPlay"
    run = home + "/20261125-143000"
    old = home + "/Archive/20261120-101500"
    for d in (run + "/images", old + "/images", FX + "/Tester/Other/20261125-143000", F + "/Shop/FeatureAssets/Private"):
        os.makedirs(d, exist_ok=True)
    for f in (FX + "/Tester/README.md", home + "/README.md", run + "/README.md", run + "/images/hud.png", old + "/README.md",
              home + "/Archive/README.md"):
        Path(f).write_text("x")
    guard.release_lock(STORE)

    g = "tester: reads all the code, edits none of it"
    for path in (F + "/Shop/Scripts/Internal/S.cs", F + "/Wallet/Scripts/Internal/Old.cs", F + "/Shop/FeatureAssets/Private/s.asset",
                 A + "/Core/Old.cs", FX + "/UnityProject/Packages/manifest.json", FX + "/Management/game-design/live/data.md",
                 FX + "/CLAUDE.md", FX + "/.claude/rules/tester.md"):
        r(g, T, "allow", path)
    t(g, T, "allow", "Grep", {"pattern": "IPopupService", "path": F})
    for path in (FX + "/UnityProject/Library/x", FX + "/secret.txt"):
        r(g, T, "deny", path)
    for path in (F + "/Shop/Scripts/Internal/S.cs", F + "/Wallet/Scripts/Contracts/I.cs", A + "/Core/Old.cs", A + "/_Composition/Boot.cs",
                 F + "/Wallet/FeatureAssets/Private/c.asset", FX + "/Management/proposals/new.md", FX + "/Management/tools/rules.json",
                 FX + "/Management/game-design/ai/idea/data.md", FX + "/.claude/rules/tester.md", FX + "/CLAUDE.md",
                 FX + "/Management/data/x.js", FX + "/UnityProject/ProjectSettings/x.asset"):
        w(g, T, "deny", path)
        e(g, T, "deny", path)

    g = "tester: writes only its own folder"
    for path in (home + "/README.md", run + "/README.md", run + "/notes.md", run + "/images/hud.png", run + "/images/state.json",
                 run + "/images/sub/a.jpg", home + "/20261126-090000/README.md"):
        w(g, T, "allow", path)
    for path in (FX + "/Tester/README.md", home + "/notes.md", home + "/run1/README.md", run + "/script.cs", run + "/shot.png",
                 run + "/images/tool.exe", run + "/images/a.cs", run + "/other/a.png", old + "/README.md", home + "/Archive/README.md",
                 home + "/Archive/20261120-101500/images/a.png", FX + "/Tester/Other/README.md",
                 FX + "/Tester/Other/20261125-143000/README.md", FX + "/tester/UnityAutoPlay/README.md" if os.name != "nt" else FX + "/Tester/Nope/x.md"):
        w(g, T, "deny", path)
    for who in (W, L, M, "@GameDesigner", "@Core", "@DesignAdvisor", ""):
        w(g + " (nobody else writes there)", who, "deny", run + "/README.md")
        w(g + " (nobody else writes there)", who, "deny", run + "/images/x.png")

    g = "tester folder: who reads it"
    for who in (T, M, L, "@GameDesigner", "@Core", W, "_Services/Popup"):
        for path in (FX + "/Tester/README.md", home + "/README.md", run + "/README.md", run + "/images/hud.png"):
            r(g, who, "allow", path)
        t(g, who, "allow", "Grep", {"pattern": "Kết quả", "path": home})
    for who in ("@DesignAdvisor", ""):
        r(g + " (not the tool chats)", who, "deny", run + "/README.md")
    g = "tester archive: the testers only"
    for path in (old + "/README.md", home + "/Archive/README.md", old + "/images/a.png", home + "/Archive"):
        r(g, T, "allow", path)
        for who in (M, L, "@GameDesigner", "@Core", W, "@DesignAdvisor", ""):
            r(g + " (others)", who, "deny", path)
    t(g + " (others)", M, "deny", "Grep", {"pattern": "x", "path": home + "/Archive"})

    g = "tester: shell commands"
    TOOL = "python Management/tools/tester.py "
    for cmd in (TOOL + 'new UnityAutoPlay "Chơi thử Hud mức IDEA"', TOOL + "archive UnityAutoPlay 20261120-101500",
                TOOL + "archive UnityAutoPlay --keep 10", TOOL + "status", TOOL + "status UnityAutoPlay", TOOL + "check",
                TOOL + "shot UnityAutoPlay 20261125-131408 screenshot-1.png", TOOL + "shot UnityAutoPlay 20261125-131408 a.PNG 01-pause.jpg",
                "python Management/tools/gd_pack.py Hud", "python Management/tools/validate.py"):
        t(g, T, "allow", "Bash", {"command": cmd})
    for cmd in (TOOL + 'new Other "x"', TOOL + 'new UnityAutoPlay "x"; ls', TOOL + "new UnityAutoPlay x", TOOL + "archive Other --keep 1",
                TOOL + "archive UnityAutoPlay ../x", TOOL + "archive UnityAutoPlay --keep", TOOL + "status; ls",
                "python Management/tools/scaffold.py", "python Management/tools/gd_status.py", "git status",
                TOOL + "shot Other 20261125-131408 a.png", TOOL + "shot UnityAutoPlay 20261125-131408 ../x.png",
                TOOL + "shot UnityAutoPlay 20261125-131408 a/b.png", TOOL + "shot UnityAutoPlay 20261125-131408 .x.png",
                TOOL + "shot UnityAutoPlay 20261125-131408 x.cs", TOOL + "shot UnityAutoPlay x a.png",
                TOOL + "shot UnityAutoPlay 20261125-131408 a.png; ls", TOOL + "shot UnityAutoPlay 20261125-131408 a.png b.png c.png"):
        t(g, T, "deny", "Bash", {"command": cmd})
    for cmd in (TOOL + "archive UnityAutoPlay 20261120-101500", TOOL + "archive UnityAutoPlay --keep 3", TOOL + "status", TOOL + "check",
                TOOL + "status; " + TOOL + "check"):
        t(g + " (@CodeLead archives and looks)", L, "allow", "Bash", {"command": cmd})
    t(g + " (@Manager no longer archives)", M, "deny", "Bash", {"command": TOOL + "archive UnityAutoPlay --keep 3"})
    t(g + " (@Manager starts no run)", M, "deny", "Bash", {"command": TOOL + 'new UnityAutoPlay "x"'})
    t(g + " (@Manager moves no picture)", M, "deny", "Bash", {"command": TOOL + "shot UnityAutoPlay 20261125-131408 a.png"})
    for who in (M, "@GameDesigner", "@Core", W):
        t(g + " (readers look)", who, "allow", "Bash", {"command": TOOL + "status"})
        t(g + " (readers look)", who, "allow", "Bash", {"command": TOOL + "check"})
        t(g + " (readers do not archive)", who, "deny", "Bash", {"command": TOOL + "archive UnityAutoPlay --keep 3"})
        t(g + " (readers start no run)", who, "deny", "Bash", {"command": TOOL + 'new UnityAutoPlay "x"'})
    for who in ("@DesignAdvisor", ""):
        t(g + " (not the tool chats)", who, "deny", "Bash", {"command": TOOL + "status"})

    g = "tester: unity-mcp plays and reads"
    u = lambda expect, name, who=T, session="tester-a", **ti: t(g, who, expect, "mcp__unityMCP__" + name, ti, session=session)      # noqa: E731
    for name in ("read_console", "find_gameobjects", "get_test_job", "refresh_unity"):
        u("allow", name)
    u("allow", "manage_scene", action="get_hierarchy")
    u("allow", "manage_asset", action="get_info", path="Assets/Features/Shop/FeatureAssets/Private/s.asset")
    u("allow", "validate_script", uri="mcpforunity://path/Assets/Features/Shop/Scripts/Internal/S.cs")
    for action in ("play", "pause", "stop"):
        u("allow", "manage_editor", action=action)
    u("allow", "run_tests", mode="PlayMode")
    u("allow", "manage_scene", action="load", path="Assets/_Composition/Scenes/Game.unity")
    u("allow", "execute_menu_item", menu_path="Cheat/Hud/Mid battle")
    u("allow", "manage_camera", action="screenshot", output_folder="Captures")
    u("deny", "manage_camera", action="screenshot")      # would land in Assets/Screenshots
    u("deny", "manage_camera", action="screenshot", output_folder="Assets/Screenshots")
    u("allow", "execute_code", code="GameObject.Find(\"Play\")")
    held = guard.read_lock(STORE) or {}
    check(g, T, ("tester-a", T), "playing holds the Editor lock", (held.get("session"), held.get("who")))
    t(g + " (the lock)", L, "deny", "mcp__unityMCP__manage_editor", {"action": "play"}, session="lead-b")
    t(g + " (the lock)", T, "allow", "mcp__unityMCP__read_console", {}, session="tester-b")
    guard.release_lock(STORE)

    g = "tester: unity-mcp changes nothing"
    for name, ti in (("manage_scene", {"action": "save"}), ("manage_scene", {"action": "create", "path": "Assets/_Composition/Scenes", "name": "X"}),
                     ("manage_gameobject", {"action": "create", "name": "x"}), ("manage_components", {"action": "add"}),
                     ("manage_asset", {"action": "create", "path": "Assets/Features/Shop/FeatureAssets/Private/n.asset"}),
                     ("manage_asset", {"action": "delete", "path": "Assets/Features/Shop/FeatureAssets/Private/s.asset"}),
                     ("manage_prefabs", {"action": "modify_contents", "prefab_path": "Assets/Features/Shop/FeatureAssets/Private/p.prefab"}),
                     ("manage_scriptable_object", {"action": "modify", "target": {"path": "Assets/Features/Shop/FeatureAssets/Private/s.asset"}}),
                     ("manage_material", {"action": "create", "material_path": "Assets/Features/Shop/FeatureAssets/Private/m.mat"}),
                     ("manage_editor", {"action": "add_tag"}), ("manage_editor", {"action": "open_prefab_stage", "prefab_path": "Assets/x.prefab"}),
                     ("manage_packages", {"action": "add"}), ("manage_graphics", {}), ("manage_physics", {}), ("execute_custom_tool", {}),
                     ("read_console", {"action": "clear"}), ("create_script", {"path": "Assets/x.cs"}), ("script_apply_edits", {}),
                     ("manage_script", {"action": "create"})):
        t(g, T, "deny", "mcp__unityMCP__" + name, ti, session="tester-a")
    check(g, T, None, "a refused call takes no lock", guard.read_lock(STORE))
    for who in (W, L, M, "@GameDesigner", "@Core", "@DesignAdvisor", ""):
        t("execute_code: the testers only", who, "deny", "mcp__unityMCP__execute_code", {"code": "x"}, session="other")
    guard.release_lock(STORE)

    g = "tester: who may message it"
    for to in ("UnityAutoPlay", "unityautoplay", "@UnityAutoPlay", " UnityAutoPlay "):
        for who in (L, "@GameDesigner", "@UIDesigner"):
            t(g, who, "pass", "SendMessage", {"to": to, "message": "x"})
        for who in (M, W, "_Services/Popup", "@Core", "@DesignAdvisor", T, ""):
            t(g + " (the others ask @CodeLead)", who, "deny", "SendMessage", {"to": to, "message": "x"})
        t(g, "@human", "pass", "SendMessage", {"to": to, "message": "x"})
    for who in (W, "@Core", T, M):
        for to in ("CodeLead", ""):
            t(g + " (other chats as before)", who, "pass", "SendMessage", {"to": to, "message": "x"})
        t(g + " (only the Leaders reach Manager)", who, "pass" if who == M else "deny", "SendMessage", {"to": "Manager", "message": "x"})
        for to in ("Shop", "UnityAutoPlayer"):      # not a Leader or a tester: a chat that runs on demand
            t(g + " (on-demand chats only through the inbox)", who, "deny", "SendMessage", {"to": to, "message": "x"})

    g = "tester: is told its job, and reports"
    Path(guard.binding_path("role-tester", STORE)).write_text(json.dumps({"feature": T}))
    told = bind_session.context({"session_id": "role-tester"}, FX, STORE)
    check(g, T, [], "the guard line names the folder, the commands, the soft gate and the report",
          [x for x in ("tầng 4 · Testers", "Tester/UnityAutoPlay/", "tester.py new UnityAutoPlay", "execute_code", "cổng mềm", "XÁC NHẬN",
                       "Báo cáo cho @Manager", ".claude/rules/tester.md") if x not in told])


def run_progress_cases():
    """The code gate for Features and Services sessions, and the one thing @CodeLead writes: progress."""
    import bind_session
    W, S, L = "Wallet", "Shop", "@CodeLead"
    live = FX + "/Management/game-design/live"
    own = F + "/Shop/Scripts/Internal/S.cs"

    g = "code gate"
    # any letter case of the bound name on Windows; on a case-sensitive system 'wallet' is not the Wallet folder
    for p in (own, F + "/Shop/Scripts/Contracts/IShop.cs", F + "/Shop/Tests/T.cs", F + "/Shop/CLAUDE.md"):
        pass
    os.makedirs(live + "/feature-shop/detail")
    for d in ("/feature-shop", "/feature-shop/detail"):
        Path(live + d + "/data.md").write_text("# x\n")
    Path(live + "/feature-shop/code.json").write_text('{"unity": ["Shop"]}')
    os.makedirs(live + "/feature-shop/detail/tech")
    Path(live + "/feature-shop/detail/tech/data.md").write_text("# x\n")
    Path(live + "/feature-shop/code.json").write_text("{ broken")
    Path(live + "/feature-shop/code.json").write_text('{"unity": ["Shop"]}')
    e(g + ": others", "@CodeLead", "allow", A + "/_Composition/Old.cs")
    e(g + ": others", "@human", "pass", F + "/Shop/Scripts/Internal/S.cs")
    os.rename(FX + "/Management/tools/gamedesign.py", FX + "/Management/tools/gamedesign.off")
    sys.modules.pop("gamedesign", None)
    os.rename(FX + "/Management/tools/gamedesign.off", FX + "/Management/tools/gamedesign.py")
    importlib.invalidate_caches()      # this process just failed to import it; a real hook is a new process each time
    shutil.rmtree(live + "/feature-shop")
    for session, value in (("gate-wallet", W), ("gate-shop", S)):
        Path(guard.binding_path(session, STORE)).write_text(json.dumps({"feature": value}))
    told = bind_session.context({"session_id": "gate-shop"}, FX, STORE)

    g = "codelead: progress command"
    PROGRESS = "python Management/tools/gd_progress.py "
    for tail in ("ai/idea/feature-hero idea done", "ai/idea/feature-wave/spawning/spawn-timer prototype doing", "ai/idea/feature-hero full todo"):
        t(g, L, "allow", "Bash", {"command": PROGRESS + tail})
    t(g, L, "allow", "PowerShell", {"command": PROGRESS + "ai/idea/feature-hero idea done"})
    for tail in ("ai/idea/feature-hero idea", "ai/idea/feature-hero optional done", "ai/idea/feature-hero idea finished", "live/feature-hero idea done",
                 "ai/idea/../live idea done", "ai/idea/Feature idea done", "ai/idea/feature-hero idea done; ls", "ai/idea/feature-hero idea done --root C:/x",
                 "ai/idea/feature-hero/.progress idea done", "ai/idea idea done x", ""):
        t(g, L, "deny", "Bash", {"command": (PROGRESS + tail).strip()})
    for cmd in ("python Management/tools/gd_status.py", "python Management/tools/gd_status.py ai/idea/feature-hero"):
        t(g, L, "allow", "Bash", {"command": cmd})
    t(g, L, "deny", "Bash", {"command": "python Management/tools/gd_delete.py ai/idea/feature-hero"})
    for who in (W, "@Manager", "@GameDesigner", "@Core", ""):
        t(g + " (others)", who, "deny", "Bash", {"command": PROGRESS + "ai/idea/feature-hero idea done"})
    w(g, L, "deny", FX + "/Management/game-design/ai/idea/feature-hero/.progress/progress.json")      # only through the command



def run_req_cases():
    """The requirement book (gd_req.py): reading for all; doing / done only for the chat of that feature; verify /
    reject for @CodeLead; status for the Leaders."""
    REQ = "python Management/tools/gd_req.py "
    g = "requirement book"
    for who in ("Wallet", "_Services/Popup", "@CodeLead", "@Manager", "@GameDesigner", "@Core"):
        t(g + ": read", who, "allow", "Bash", {"command": REQ + "Wallet"})
    t(g + ": read (like gd_pack)", "", "allow", "Bash", {"command": REQ + "Wallet"})
    own = ('Wallet doing wallet-rules#start-50 "đang viết WalletLogic.Reset"',
           "Wallet doing feature-code/detail/tech#cap-max \"x\"".replace("\\", ""),
           "Wallet done wallet-rules#start-50 WalletLogicTests.StartsWithConfiguredCoins",
           "Wallet done wallet-rules#start-50 WalletLogicTests.A,WalletLogicTests.B_c",
           'Wallet done wallet-rules#edge-tien-vua-du play "mua khi đủ đúng giá, số về 0"')
    for tail in own:
        pass
    for tail in own:
        t(g + ": doing / done are the feature's", "@CodeLead", "deny", "Bash", {"command": REQ + tail})
        t(g + ": doing / done are the feature's", "@Manager", "deny", "Bash", {"command": REQ + tail})
    for tail in ('Wallet doing wallet-rules#start-50 "a $(rm -rf x)"', 'Wallet doing wallet-rules#start-50 "a"; ls',
                 "Wallet doing wallet-rules#start-50", "Wallet done wallet-rules#start-50", "Wallet done wallet-rules#start-50 Tests",
                 "Wallet done ../x#y A.B", "Wallet done wallet-rules#start-50 A.B --root /tmp", 'Wallet doing Wallet-Rules#x "y"',
                 "Wallet verify wallet-rules#start-50", 'Wallet reject wallet-rules#start-50 "x"'):
        pass
    for tail in ("Wallet verify wallet-rules#start-50", "Wallet verify wallet-rules#start-50,income-amounts#kill-once",
                 'Wallet reject wallet-rules#start-50 "test không kiểm trần số dư"', "status"):
        t(g + ": @CodeLead", "@CodeLead", "allow", "Bash", {"command": REQ + tail})
    for tail in ('Wallet reject wallet-rules#start-50', 'Wallet reject wallet-rules#start-50 "a|b"', "Wallet verify wallet-rules"):
        t(g + ": @CodeLead refused shapes", "@CodeLead", "deny", "Bash", {"command": REQ + tail})
    for who in ("@Manager", "@GameDesigner"):
        t(g + ": status for Leaders", who, "allow", "Bash", {"command": REQ + "status"})
        t(g + ": verify is @CodeLead's", who, "deny", "Bash", {"command": REQ + "Wallet verify wallet-rules#start-50"})
    for who in ("Wallet", "@Core"):
        t(g + ": status is the Leaders'", who, "deny", "Bash", {"command": REQ + "status"})
    t(g + ": a Leader may chain status", "@Manager", "allow", "Bash", {"command": REQ + "status; " + VALIDATE})
    w(g + ": the book file only through the command", "@CodeLead", "deny",
      FX + "/Management/game-design/ai/idea/feature-code/detail/tech/.progress/requirements.json")


def run_unity_cases():
    """unity-mcp by what each command touches: read, write (own assets), shared (compile), state (the open scene,
    with the Editor lock), play and lead (@CodeLead), script (never)."""
    W, S, C, L = "Wallet", "Shop", "@CodeLead", "@CodeLead"      # C: @CodeLead as the owner of _Composition
    os.makedirs(F + "/Wallet/FeatureAssets/Private", exist_ok=True)
    os.makedirs(F + "/Shop/FeatureAssets/Private", exist_ok=True)
    os.makedirs(A + "/_Composition/Scenes", exist_ok=True)
    os.makedirs(F + "/Wallet/Scripts/Internal/Base", exist_ok=True)
    Path(F + "/Shop/FeatureAssets/Private/s.asset").write_text("x")
    own = "Assets/Features/Wallet/FeatureAssets/Private"
    guard.release_lock(STORE)

    def u(g, who, expect, tool_, session="a", **ti):
        t(g, who, expect, "mcp__unityMCP__" + tool_, ti, session=session)

    g = "unity: read"
    # the same server under the name of its stdio registration (UnityMCP, local scope of the CLI)
    t(g, "@Manager", "allow", "mcp__UnityMCP__read_console", {"action": "get"})

    g = "unity: write (own assets)"
    SO = "manage_scriptable_object"      # config and installer assets: the owner, inside its own area
    for who in ("@Manager", "@GameDesigner", ""):
        u(g, who, "deny", SO, action="create", type_name="X", folder_path=own, asset_name="n")
    u(g, "@CodeLead", "deny", SO, action="create", type_name="X", folder_path=own, asset_name="n")      # not its area
    u(g, "@CodeLead", "allow", SO, action="create", type_name="X", folder_path="Assets/_Composition", asset_name="Catalog")
    u(g, C, "allow", "manage_asset", action="create", path="Assets/_Composition/Catalog.asset")
    u(g, C, "deny", "manage_asset", action="create", path=own + "/x.asset")
    u(g, L, "deny", "manage_asset", action="create", path=own + "/x.asset")
    for name in ("create_script", "script_apply_edits", "apply_text_edits", "delete_script", "manage_script"):
        u(g + " (code only through Edit/Write)", L, "deny", name, uri="mcpforunity://path/Assets/Features/Wallet/Scripts/Internal/Old.cs")
    t(g, "@human", "pass", "mcp__unityMCP__create_script", {"path": "Assets/x.cs"})

    g = "unity: play and lead are @CodeLead's"
    for name, ti in (("run_tests", {"mode": "EditMode"}), ("manage_editor", {"action": "play"}),
                     ("manage_editor", {"action": "add_tag", "tag_name": "x"}), ("execute_menu_item", {"menu_path": "File/Save"}),
                     ("manage_packages", {"action": "add"}), ("manage_camera", {"action": "screenshot"})):
        pass
    for who in ("@Manager", "@GameDesigner"):      # they read and compile, play and lead stay @CodeLead's
        u(g + " (leaders without code)", who, "allow", "read_console", action="get")
        u(g + " (leaders without code)", who, "allow", "refresh_unity")
        u(g + " (leaders without code)", who, "deny", "run_tests")
    u(g + " (unset)", "", "deny", "read_console", action="get")
    u(g + " (unset)", "", "deny", "refresh_unity")

    g = "unity: the Editor lock"
    guard.release_lock(STORE)
    u(g, C, "allow", "manage_scene", session="a", action="load", path="Assets/_Composition/Scenes/Game.unity")
    lock = guard.read_lock(STORE)
    check(g, C, True, "the lock names the chat, its role and the scene it opened",
          (lock["session"], lock["who"], lock["scene"]) == ("a", C, "Assets/_Composition/Scenes/Game.unity"))
    u(g, C, "deny", "manage_scene", session="b", action="load", path="Assets/_Composition/Scenes/Game.unity")
    u(g, C, "deny", "refresh_unity", session="b")      # compiling waits too
    u(g, C, "allow", "manage_asset", session="b", action="create", path="Assets/_Composition/Catalog.asset")      # assets do not
    u(g, C, "allow", "read_console", session="b", action="get")
    u(g, L, "deny", "manage_editor", session="c", action="play")
    check(g, "", True, "Stop of another chat keeps the lock", guard.release_lock(STORE, "b") is False and guard.read_lock(STORE) is not None)
    p = subprocess.run([sys.executable, "-I", str(HERE / "stop_unlock.py"), "--store", STORE],
                       input=json.dumps({"session_id": "a"}).encode(), capture_output=True)
    check(g, "", True, "Stop of the holder frees it", p.returncode == 0 and guard.read_lock(STORE) is None)
    u(g, C, "allow", "manage_scene", session="b", action="load", path="Assets/_Composition/Scenes/Game.unity")
    u(g, C, "allow", "manage_scene", session="b", action="save")
    u(g, C, "allow", "manage_editor", session="b", action="open_prefab_stage", prefab_path="Assets/_Composition/Boot.prefab")
    u(g, C, "allow", "manage_editor", session="b", action="save_prefab_stage")
    u(g, C, "allow", "manage_editor", session="b", action="close_prefab_stage")
    u(g, C, "deny", "manage_editor", session="b", action="save_prefab_stage")      # nothing open any more
    stale = guard.read_lock(STORE)
    stale["until"] = 1
    guard.write_lock(STORE, stale)
    u(g, L, "allow", "manage_editor", session="c", action="play")      # an expired lock is free
    u(g, L, "allow", "manage_scene", session="c", action="load", path="Assets/Features/Shop/Scenes/S.unity")
    u(g, L, "deny", "manage_scene", session="c", action="save")      # @CodeLead looks and plays, saves no scene
    u(g, L, "allow", "execute_menu_item", session="c", menu_path="Assets/Refresh")
    p = subprocess.run([sys.executable, "-I", str(HERE / "stop_unlock.py"), "--release", "--store", STORE], capture_output=True)
    check(g, "", True, "the human frees any lock", p.returncode == 0 and guard.read_lock(STORE) is None)
    Path(guard.lock_path(STORE)).write_text("{ broken")

    g = "tester plays: no writes into Assets"
    guard.write_lock(STORE, {"session": "tester-1", "who": "@UnityAutoPlay", "until": time.time() + 600})
    t(g + ": @CodeLead too", L, "deny", "Edit", {"file_path": A + "/_Composition/Boot.cs"}, session="c")
    t(g + ": outside Assets is not held", "@GameDesigner", "allow", "Write", {"file_path": FX + "/Management/game-design/ai/idea/data.md"}, session="e")
    t(g + ": the human is never held", "@human", "pass", "Write", {"file_path": F + "/Wallet/Scripts/Internal/New.cs"}, session="h")
    t(g + ": a worker writes in its own worktree, never held", "@Worker", "allow", "Write", {"file_path": F + "/Hero/Tests/HeroStageIdeaTests.cs"}, session="wk")
    marks = sorted(p.name for p in Path(STORE, guard.TESTER_BLOCKED_DIR).glob("*.json"))
    check(g, "", True, "the refused chats are marked for the server to wake", marks == ["c.json"])
    guard.write_lock(STORE, {"session": "b", "who": "@CodeLead", "until": time.time() + 600})
    guard.write_lock(STORE, {"session": "tester-1", "who": "@UnityAutoPlay", "until": time.time() - 1})
    Path(guard.lock_path(STORE)).unlink()
    shutil.rmtree(Path(STORE, guard.TESTER_BLOCKED_DIR), ignore_errors=True)

    g = "unity: reading the lock file"
    for who in (W, "@Manager", "@GameDesigner", "@DesignAdvisor", "@UnityAutoPlay", None):
        r(g, who, "allow", guard.lock_path(STORE))

    g = "unity: batch_execute"
    guard.release_lock(STORE)
    B = "batch_execute"
    comp = "Assets/_Composition"
    u(g, C, "allow", B, commands=[{"tool": "read_console", "params": {"action": "get"}},
                                  {"tool": "manage_asset", "params": {"action": "get_info", "path": comp + "/Catalog.asset"}}])
    check(g, C, True, "a batch of reads takes no lock", guard.read_lock(STORE) is None)
    u(g, C, "deny", B, commands=[{"tool": "read_console", "params": {"action": "get"}},
                                 {"tool": "manage_asset", "params": {"action": "create", "path": "Assets/Core/x.asset"}}])
    u(g, C, "allow", B, commands=[{"tool": "manage_scene", "params": {"action": "load", "path": comp + "/Scenes/Game.unity"}},
                                  {"tool": "manage_editor", "params": {"action": "play"}}])
    check(g, C, True, "a batch that opens a scene takes the lock", (guard.read_lock(STORE) or {}).get("session") == "a")
    u(g, C, "deny", B, session="b", commands=[{"tool": "refresh_unity", "params": {}}])
    u(g, C, "deny", B, commands=[{"tool": "batch_execute", "params": {"commands": []}}])
    u(g, C, "deny", B, commands=[])
    u(g, C, "deny", B, commands=[{"tool": "read_console", "params": {"action": "get"}}] * 26)
    u(g, C, "deny", B, commands="x")
    u(g, C, "deny", B, commands=[{"tool": "create_script", "params": {"path": comp + "/a.cs"}}])
    guard.release_lock(STORE)

    g = "unity: what needs a report"
    for name, ti, want in (("manage_asset", {"action": "create", "path": "Assets/x"}, True),
                           ("manage_scene", {"action": "save"}, True), ("execute_menu_item", {"menu_path": "x"}, True),
                           ("read_console", {"action": "get"}, False), ("refresh_unity", {}, False),
                           ("manage_editor", {"action": "play"}, False), ("manage_asset", {"action": "search"}, False),
                           ("batch_execute", {"commands": [{"tool": "read_console", "params": {}},
                                                           {"tool": "manage_asset", "params": {"action": "delete"}}]}, True),
                           ("batch_execute", {"commands": [{"tool": "read_console", "params": {}}]}, False)):
        check(g, "", want, f"{name} {ti}", guard.unity_changes("mcp__unityMCP__" + name, ti))


def run_headless_cases():
    """Tiers 2-6 run headless: they never ask the human (2026-11-26)."""
    g = "headless: no asking"
    low = ("@UnityAutoPlay", "@DesignAdvisor")
    for who in low:
        t(g, who, "deny", "AskUserQuestion", {"questions": []})
    for who in ("@human", "@Manager"):
        t(g + " (only the human and Manager ask)", who, "pass", "AskUserQuestion", {"questions": []})
    for who in ("@CodeLead", "@GameDesigner", "@UIDesigner"):
        t(g + " (a Leader asks through Manager, 2026-11-27)", who, "deny", "AskUserQuestion", {"questions": []})
    data = {"tool_name": "AskUserQuestion", "tool_input": {}, "cwd": FX, "session_id": "test-session"}
    for who, leader in (("@UnityAutoPlay", "CodeLead"),
                        ("@DesignAdvisor", "GameDesigner")):
        reason = guard.decide(data, who, FX, STORE)[1]
        check(g + " (names the Leader)", who, True, "reason names " + leader, leader in reason and "BÁO CHẶN" in reason)
    edit = {"tool_name": "Edit", "tool_input": {"file_path": FX + "/Management/tools/rules.json"}, "cwd": FX,
            "session_id": "test-session"}
    deny = {"tool_name": "Bash", "tool_input": {"command": "dotnet build"}, "cwd": FX, "session_id": "test-session"}
    got = guard.decide(deny, "@CodeLead", FX, STORE)
    check(g + " (Leaders: not BÁO CHẶN)", "@CodeLead", True, "dotnet build", got[0] == "deny" and "BÁO CHẶN" not in got[1])


def run_worker_cases():
    """@Worker (worker.py, người quyết 2026-11-27): one big job in a worktree, every feature at once; the safe zones hold."""
    g = "worker"
    A = FX + "/UnityProject/Assets"
    w(g + " (a feature's code)", "@Worker", "allow", F + "/Wallet/Scripts/Internal/W.cs")
    w(g + " (another feature's private code)", "@Worker", "allow", F + "/Shop/Scripts/View/S.cs")
    w(g + " (a contract)", "@Worker", "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    w(g + " (an asmdef)", "@Worker", "allow", F + "/Wallet/Wallet.asmdef")
    w(g + " (Core)", "@Worker", "allow", A + "/Core/Thing.cs")
    w(g + " (_Composition)", "@Worker", "allow", A + "/_Composition/Boot.cs")
    w(g + " (its result.json)", "@Worker", "allow", FX + "/result.json")
    w(g + " (SimpleCoreTools)", "@Worker", "deny", A + "/SimpleCoreTools/Editor/X.cs")
    w(g + " (.asmref)", "@Worker", "deny", F + "/Wallet/x.asmref")
    w(g + " (.claude)", "@Worker", "deny", FX + "/.claude/rules/worker.md")
    w(g + " (root CLAUDE.md)", "@Worker", "deny", FX + "/CLAUDE.md")
    w(g + " (the design)", "@Worker", "deny", FX + "/Management/game-design/live/x/data.md")
    w(g + " (Management)", "@Worker", "deny", FX + "/Management/tools/rules.json")
    w(g + " (Packages)", "@Worker", "deny", FX + "/UnityProject/Packages/manifest.json")
    w(g + " (ProjectSettings)", "@Worker", "deny", FX + "/UnityProject/ProjectSettings/x.asset")
    sh = lambda cmd: {"command": cmd}      # noqa: E731
    t(g + " (any shell)", "@Worker", "allow", "Bash", sh("python Management/tools/compile_check.py --all"))
    t(g + " (reads the book)", "@Worker", "allow", "Bash", sh("python Management/tools/gd_req.py Wallet"))
    t(g + " (design pack)", "@Worker", "allow", "Bash", sh("python Management/tools/gd_pack.py Wallet"))
    t(g + " (writes the book)", "@Worker", "deny", "Bash", sh("python Management/tools/gd_req.py Wallet done a#b T.M"))
    t(g + " (verifies)", "@Worker", "deny", "Bash", sh("python Management/tools/gd_req.py Wallet verify a#b"))
    t(g + " (in a chain)", "@Worker", "deny", "Bash", sh("ls; python Management/tools/dispatch.py send Wallet --from CodeLead \"x\""))
    t(g + " (ask.py)", "@Worker", "deny", "Bash", sh("python Management/tools/ask.py add --from Worker \"x\""))
    t(g + " (editor ticket)", "@Worker", "deny", "Bash", sh("python Management/tools/editor_ticket.py add --session @Worker --common refresh"))
    t(g + " (AskUserQuestion)", "@Worker", "deny", "AskUserQuestion", {"questions": []})
    t(g + " (SendMessage)", "@Worker", "deny", "SendMessage", {"to": "CodeLead", "message": "x"})
    t(g + " (unity-mcp)", "@Worker", "deny", "mcp__unityMCP__refresh_unity", {})
    t(g + " (unity-mcp read)", "@Worker", "deny", "mcp__unityMCP__read_console", {})
    t(g + " (reads another feature's private code)", "@Worker", "allow", "Read", {"file_path": F + "/Shop/Scripts/Internal/S.cs"})


def run_ticket_cases():
    """Editor tickets (người quyết 2026-11-27): the Editor goes to the ticket holder, or to the server's runner of a
    load or a test run; the human using the Editor holds every chat off."""
    import shutil
    g = "editor tickets"
    et = guard.ticket_module()
    tdir = os.path.join(FX, "Management", "editor-tickets")
    shutil.rmtree(tdir, ignore_errors=True)
    refresh = "mcp__unityMCP__refresh_unity"
    sh = lambda cmd: {"command": cmd}      # noqa: E731
    try:
        t(g + " (Editor free: use it)", "@CodeLead", "allow", refresh, {})
        guard.release_lock(STORE)
        et.add(FX, "@Worker", ["refresh", "clean"], [], now=1, job="w20261127-000001-run")
        kind, group = et.grant_next(FX, now=2)
        for x in group:
            x["runner"] = "@UnityAutoPlay"
            et.save(FX, x)
        got = guard.decide({"tool_name": refresh, "tool_input": {}, "cwd": FX, "session_id": "test-session"}, "@CodeLead", FX, STORE)
        check(g, "@CodeLead", True, "a load granted to the server's runner: CodeLead is refused with how to queue",
              got[0] == "deny" and "editor_ticket.py add --session @CodeLead" in got[1])
        t(g + " (the runner of the load uses it)", "@UnityAutoPlay", "allow", refresh, {})
        guard.release_lock(STORE)
        t(g + " (reads stay open)", "@CodeLead", "allow", "mcp__unityMCP__read_console", {})
        et.release(FX, now=3)
        with open(os.path.join(STORE, guard.HUMAN_EDITOR_FILE), "w", encoding="utf-8") as f:
            json.dump({"since": 1, "reason": "play"}, f)
        got = guard.decide({"tool_name": refresh, "tool_input": {}, "cwd": FX, "session_id": "test-session"}, "@CodeLead", FX, STORE)
        check(g, "@CodeLead", True, "the human uses the Editor: refused", got[0] == "deny" and "người đang dùng Unity Editor" in got[1])
        os.remove(os.path.join(STORE, guard.HUMAN_EDITOR_FILE))
        t(g + " (CodeLead: its own ticket)", "@CodeLead", "allow", "Bash", sh("python Management/tools/editor_ticket.py add --session @CodeLead --common clean"))
        t(g + " (a Leader may not touch another's ticket)", "@GameDesigner", "deny", "Bash",
          sh("python Management/tools/editor_ticket.py drop --session @CodeLead"))
        t(g + " (draft.py is gone)", "@CodeLead", "allow", "Bash", sh("python Management/tools/editor_ticket.py list"))
        et.add(FX, "@Worker", ["refresh"], ["x"], now=5, job="w20261127-000002-wave")
        et.grant_next(FX, now=6)
        import bind_session
        check(g, "@CodeLead", True, "CodeLead's guard line shows who holds the Editor and what waits",
              "Phiếu Editor:" in bind_session.editor_line(FX, None) and "@Worker" in bind_session.editor_line(FX, None))
    finally:
        try:
            os.remove(os.path.join(STORE, guard.HUMAN_EDITOR_FILE))
        except OSError:
            pass
        guard.release_lock(STORE)
        shutil.rmtree(tdir, ignore_errors=True)


def run_leader_ask_cases():
    """Only @Manager asks the human (người quyết 2026-11-27): a Leader writes the question in ask.py and messages
    Manager; a permission the human gives through Manager lets the same call through, as many times as given."""
    import shutil
    g = "leader ask"
    ask = guard.ask_module(FX)
    asks = os.path.join(FX, "Management", "asks")
    shutil.rmtree(asks, ignore_errors=True)
    try:
        q = {"tool_name": "AskUserQuestion", "tool_input": {}, "cwd": FX, "session_id": "test-session"}
        got = guard.decide(q, "@GameDesigner", FX, STORE)
        check(g, "@GameDesigner", True, "AskUserQuestion points to ask.py and Manager",
              got[0] == "deny" and "ask.py add --from GameDesigner" in got[1] and "HỎI NGƯỜI" in got[1])
        edit = {"tool_name": guard.UNITY_ASSET_TOOL, "tool_input": {"action": "delete", "path": "Assets/_Composition"},
                "cwd": FX, "session_id": "test-session"}
        raw = guard.decide_raw(edit, "@CodeLead", FX, STORE)
        got = guard.decide(edit, "@CodeLead", FX, STORE)
        check(g, "@CodeLead", ("ask", "deny", True), "an ask of the guard becomes a deny to Manager",
              (raw[0], got[0], "ask.py add --from CodeLead" in got[1]))
        check(g, "@human", True, "the human's chat is not stopped", (guard.decide(edit, "@human", FX, STORE) or ("pass",))[0] != "deny")
        late = ask.add(FX, "@CodeLead", "xoá cũ", "x", "A·B", "A", "CodeLead")
        ask.answer(FX, late["id"], "ok", allow="Assets/_Composition", now=0)
        wrong = ask.add(FX, "@CodeLead", "khác", "x", "A·B", "A", "CodeLead")
        ask.answer(FX, wrong["id"], "ok", allow="UnityProject/Packages/manifest.json")
        other = ask.add(FX, "@GameDesigner", "việc khác", "x", "A·B", "A", "GD")
        ask.answer(FX, other["id"], "ok", allow="Assets/_Composition")
        check(g, "@CodeLead", "deny", "a permission out of time, for another path or of another Leader does not pass",
              guard.decide(edit, "@CodeLead", FX, STORE)[0])
        item = ask.add(FX, "@CodeLead", "xoá _Composition", "guard hỏi", "A xoá·B thôi", "A", "CodeLead")
        ask.answer(FX, item["id"], "cho xoá", allow="Assets/_Composition", uses=2)
        got = [guard.decide(edit, "@CodeLead", FX, STORE)[0] for _ in range(3)]
        check(g, "@CodeLead", ["allow", "allow", "deny"], "the permission passes the call as many times as given", got)
        sh = lambda cmd: {"command": cmd}      # noqa: E731
        t(g + " (add naming itself)", "@CodeLead", "allow", "Bash",
          sh('python Management/tools/ask.py add --from CodeLead "việc" --blocker "x" --options "A·B" --suggest "A" --waiting "x"'))
        t(g + " (add naming another)", "@CodeLead", "deny", "Bash",
          sh('python Management/tools/ask.py add --from GameDesigner "việc" --blocker "x" --options "A·B" --suggest "A" --waiting "x"'))
        for who in ("Wallet", "@Core", "@Manager"):
            t(g + " (only a Leader adds)", who, "deny", "Bash",
              sh(f'python Management/tools/ask.py add --from {who.lstrip("@")} "v" --blocker "x" --options "A" --suggest "A" --waiting "x"'))
        t(g + " (Manager answers)", "@Manager", "allow", "Bash", sh(f'python Management/tools/ask.py answer {item["id"]} "ok"'))
        for who in ("@CodeLead", "@GameDesigner", "Wallet"):
            t(g + " (only Manager answers)", who, "deny", "Bash", sh(f'python Management/tools/ask.py answer {item["id"]} "ok"'))
        t(g + " (only Manager answers, in a chain too)", "@CodeLead", "deny", "Bash",
          sh(f'echo hi; python Management/tools/ask.py answer {item["id"]} "ok"'))
        mine = ask.add(FX, "@GameDesigner", "câu của GD", "x", "A·B", "A", "GD")
        t(g + " (the asker cancels)", "@GameDesigner", "allow", "Bash", sh(f'python Management/tools/ask.py cancel {mine["id"]} "thôi"'))
        t(g + " (another Leader does not)", "@UIDesigner", "deny", "Bash", sh(f'python Management/tools/ask.py cancel {mine["id"]} "thôi"'))
        t(g + " (Manager cancels)", "@Manager", "allow", "Bash", sh(f'python Management/tools/ask.py cancel {mine["id"]} "thôi"'))
    finally:
        shutil.rmtree(asks, ignore_errors=True)


def run_free_shell_cases():
    """Free shell (người quyết 2026-11-26): every bound chat, and its subagents, runs any shell command and any
    Python; the gates of dispatch send, gd_req doing / done, art choose still hold; a chat with no FEATURE does not."""
    g = "free shell"
    guard.FREE_SHELL = True
    try:
        for who in ("Wallet", "_Services/Popup", "@Core", "@UnityAutoPlay",
                    "@DesignAdvisor", "@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner"):
            for cmd in ('python -c "print(1)"', "python Management/tools/test_validate.py", "ls", "git status",
                        "python Management/tools/validate.py; git diff | head -5"):
                t(g, who, "allow", "Bash", {"command": cmd})
            t(g, who, "allow", "PowerShell", {"command": "Get-ChildItem"})
            t(g, who, "allow", "Monitor", {"command": "ls"})
        t(g + " (no FEATURE: allow-list)", "", "deny", "Bash", {"command": 'python -c "print(1)"'})
        for who in ("@Manager", "@CodeLead", "@GameDesigner", "@UIDesigner", "@DesignAdvisor", "@UnityAutoPlay", "@Worker"):
            t(g + " (art: the tier the game uses is the human's)", who, "deny", "Bash",
              {"command": "python Management/tools/art.py choose Hero/hero-knight simple"})
            t(g + " (art: anyone lists)", who, "allow", "Bash", {"command": "python Management/tools/art.py list Hero"})
        t(g + " (art: anyone lists)", "", "allow", "Bash", {"command": "python Management/tools/art.py list"})
        g2 = "PowerShell: a bare @Name is splatting"
        send = 'python Management/tools/dispatch.py send {} --from GameDesigner "hỏi"'
        for cmd in (send.format("@DesignAdvisor"), "python Management/tools/dispatch.py list @Core",
                    "ls; python x.py @Core"):
            t(g2, "@GameDesigner", "deny", "PowerShell", {"command": cmd})
        for cmd in (send.format("'@DesignAdvisor'"), send.format("DesignAdvisor"), "$a = @(1, 2)", "$h = @{a = 1}",
                    'Write-Host "mail @x"', "git log --format=%H@x"):
            t(g2 + " (quoted, no @, arrays)", "@GameDesigner", "allow", "PowerShell", {"command": cmd})
        t(g2 + " (Bash keeps @)", "@GameDesigner", "allow", "Bash", {"command": send.format("@DesignAdvisor")})
        for cmd in (send.format("'@DesignAdvisor'"), send.format("DesignAdvisor")):
            t(g2 + " (the gate still reads the quoted or bare name)", "@UIDesigner", "deny", "PowerShell", {"command": cmd})
    finally:
        guard.FREE_SHELL = False


def run_free_read_cases():
    """Free read (người quyết 2026-11-26): every bound chat reads anywhere in the project with Read / Grep / Glob,
    but for Unity's generated folders, the testers' Archive and outside the project."""
    g = "free read"
    guard.FREE_READ = True
    try:
        for who in ("Wallet", "_Services/Popup", "@Core", "@UnityAutoPlay", "@DesignAdvisor", "@Manager",
                    "@CodeLead", "@GameDesigner", "@UIDesigner"):
            for path in ("/Management/tools/dispatch.py", "/UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs",
                         "/.claude/rules/worker.md", "/CLAUDE.md"):
                t(g, who, "allow", "Read", {"file_path": FX + path})
            t(g, who, "allow", "Grep", {"pattern": "x", "path": FX + "/Management"})
            t(g, who, "allow", "Glob", {"pattern": "**/*.cs", "path": FX + "/UnityProject/Assets/Features"})
            t(g + " (generated folders)", who, "deny", "Read", {"file_path": FX + "/UnityProject/Library/x.txt"})
            t(g + " (outside the project)", who, "deny", "Read", {"file_path": os.path.dirname(FX) + "/other.txt"})
        t(g + " (no FEATURE: read zones)", "", "deny", "Read",
          {"file_path": FX + "/UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs"})
        t(g + " (edit zones hold)", "@GameDesigner", "deny", "Edit", {"file_path": FX + "/Management/tools/dispatch.py"})
    finally:
        guard.FREE_READ = False


def main():
    guard.FREE_SHELL = False      # the cases below check the allow-lists (a chat with no FEATURE, or FREE_SHELL off)
    guard.FREE_READ = False       # and the read zones of each role
    try:
        build_fixture()
        run_cases()
        run_headless_cases()
        run_leader_ask_cases()
        run_ticket_cases()
        run_worker_cases()
        run_free_shell_cases()
        run_free_read_cases()
    finally:
        link = F + "/Wallet/link"
        if os.path.lexists(link):
            os.rmdir(link) if WIN else os.unlink(link)      # remove the link itself, never what it points to
        shutil.rmtree(FX, ignore_errors=True)

    failed = [x for x in results if not x[0]]
    for ok, group, feature, expect, got, label in failed:
        print(f"FAIL  [{group}] FEATURE={feature} expected {expect}, got {got}: {label}")
    groups = {}
    for x in results:
        groups.setdefault(x[1], []).append(x[0])
    for name, oks in groups.items():
        bad = oks.count(False)
        print(f"{name:<28} {len(oks):>3} cases  {'all ok' if not bad else f'{bad} FAILED'}")
    print(f"\nTOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
