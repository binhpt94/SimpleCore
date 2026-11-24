#!/usr/bin/env python3
"""Tests for guard.py.

    python .claude/hooks/test_guard.py

Feeds fake hook input to guard.py and compares the decision. Works on a throw-away copy of the
folder layout in a temp folder; the real project is not touched.
"""
import importlib.util
import json
import os
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


def check(group, feature, expect, label, got):
    results.append((got == expect, group, feature or "(unset)", expect, got, label.replace(FX, "")))


def t(group, feature, expect, tool, ti, cwd=None, session="test-session"):
    """In-process: calls guard.decide exactly as main() does, including fail-closed on errors. The Editor lock
    lives in STORE, never in the real store."""
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


def run_cases():
    W = "Wallet"

    # ---- one group per row of the permission table
    g = "1 own feature"
    r(g, W, "allow", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, W, "allow", F + "/Wallet/Scripts/Internal/W.cs")
    w(g, W, "allow", F + "/Wallet/Scripts/View/V.cs")
    w(g, W, "allow", F + "/Wallet/FeatureAssets/Private/cfg.asset")
    w(g, W, "allow", F + "/Wallet/Tests/T.cs")
    w(g, W, "allow", F + "/Wallet/CLAUDE.md")

    g = "2 own Contracts"
    r(g, W, "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g, W, "deny", F + "/Wallet/Scripts/Contracts/IWallet.cs")       # the skeleton: only @CodeLead edits it
    w(g, W, "deny", F + "/Wallet/Scripts/Contracts/New.cs")

    g = "3 other public"
    r(g, W, "allow", F + "/Shop/Scripts/Contracts/IShop.cs")
    e(g, W, "deny", F + "/Shop/Scripts/Contracts/IShop.cs")
    r(g, W, "allow", F + "/Shop/FeatureAssets/Public/icon.png")
    w(g, W, "deny", F + "/Shop/FeatureAssets/Public/icon.png")
    r(g, W, "allow", F + "/_Services/Popup/Scripts/Contracts/IPopup.cs")
    e(g, W, "deny", F + "/_Services/Popup/Scripts/Contracts/IPopup.cs")

    g = "4 other private"
    r(g, W, "deny", F + "/Shop/Scripts/Internal/S.cs")
    e(g, W, "deny", F + "/Shop/Scripts/Internal/S.cs")
    r(g, W, "deny", F + "/Shop/Scripts/View/V.cs")
    w(g, W, "deny", F + "/Shop/Scripts/View/V.cs")
    r(g, W, "deny", F + "/Shop/FeatureAssets/Private/cfg.asset")
    w(g, W, "deny", F + "/Shop/FeatureAssets/Private/cfg.asset")
    r(g, W, "deny", F + "/_Services/Popup/Scripts/Internal/P.cs")
    e(g, W, "deny", F + "/_Services/Popup/Scripts/Internal/P.cs")

    g = "5 _Composition/Core"
    r(g, W, "allow", A + "/_Composition/Boot.cs")
    e(g, W, "deny", A + "/_Composition/Boot.cs")
    r(g, W, "allow", A + "/Core/Base.cs")
    w(g, W, "deny", A + "/Core/Base.cs")

    g = "6 Packages/ProjectSettings"
    r(g, W, "allow", FX + "/UnityProject/Packages/manifest.json")
    e(g, W, "deny", FX + "/UnityProject/Packages/manifest.json")
    r(g, W, "allow", FX + "/UnityProject/ProjectSettings/ProjectVersion.txt")
    w(g, W, "deny", FX + "/UnityProject/ProjectSettings/ProjectVersion.txt")

    g = "7 proposals"
    r(g, W, "allow", FX + "/Management/proposals/old.md")
    w(g, W, "allow", FX + "/Management/proposals/new.md")
    w(g, W, "deny", FX + "/Management/proposals/old.md")
    e(g, W, "deny", FX + "/Management/proposals/old.md")

    g = "8 Management"
    r(g, W, "allow", FX + "/Management/data/idea.js")
    e(g, W, "deny", FX + "/Management/data/idea.js")
    w(g, W, "deny", FX + "/Management/data/new.js")
    r(g, W, "allow", FX + "/Management/tools/validate.py")
    w(g, W, "deny", FX + "/Management/tools/validate.py")
    r(g, W, "allow", FX + "/Management/tools/rules.json")
    e(g, W, "ask", FX + "/Management/tools/rules.json")
    e(g, "", "deny", FX + "/Management/tools/rules.json")
    e(g, W, "deny", FX + "/Management/tools/sub/rules.json")

    g = "other feature's CLAUDE.md"
    r(g, W, "allow", F + "/Shop/CLAUDE.md")
    r(g, W, "allow" if WIN else "deny", F + "/Shop/claude.md")      # the same file as CLAUDE.md only on Windows
    r(g, W, "allow", F + "/_Services/Popup/CLAUDE.md")
    r(g, "", "allow", F + "/Shop/CLAUDE.md")
    e(g, W, "deny", F + "/Shop/CLAUDE.md")
    w(g, W, "deny", F + "/_Services/Popup/CLAUDE.md")
    r(g, W, "deny", F + "/Shop/Scripts/Internal/CLAUDE.md")
    r(g, W, "deny", F + "/Shop/NOTES.md")
    t(g, W, "deny", "mcp__unityMCP__manage_asset", {"action": "delete", "path": "Assets/Features/Shop/CLAUDE.md"})
    r(g, W, "allow", FX + "/Management/index.html")
    e(g, W, "deny", FX + "/Management/index.html")

    g = "9 .claude"
    r(g, W, "allow", FX + "/.claude/settings.json")
    e(g, W, "deny", FX + "/.claude/settings.json")
    w(g, W, "deny", FX + "/.claude/hooks/guard.py")
    w(g, W, "deny", FX + "/.claude/settings.local.json")

    g = "10 Library etc"
    for d in ("Library", "Temp", "Logs", "obj", "UserSettings"):
        r(g, W, "deny", FX + f"/UnityProject/{d}/x.txt")
        w(g, W, "deny", FX + f"/UnityProject/{d}/x.txt")

    # ---- FEATURE variants
    g, P = "service agent", "_Services/Popup"
    e(g, P, "allow", F + "/_Services/Popup/Scripts/Internal/P.cs")
    e(g, P, "deny", F + "/_Services/Popup/Scripts/Contracts/IPopup.cs")      # the skeleton
    r(g, P, "deny", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, P, "deny", F + "/Wallet/Scripts/Internal/W.cs")
    r(g, P, "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g, r"_Services\Popup", "allow", F + "/_Services/Popup/Scripts/Internal/P.cs")

    g = "FEATURE unset"
    e(g, "", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    w(g, "", "deny", FX + "/Management/proposals/new.md")
    e(g, "", "deny", A + "/Core/Base.cs")
    r(g, "", "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    r(g, "", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    t(g, "", "deny", "Bash", {"command": "ls"})

    g = "FEATURE invalid"
    e(g, "Nope", "deny", F + "/Nope/Scripts/Internal/N.cs")
    e(g, "../Shop", "deny", F + "/Shop/Scripts/Internal/S.cs")
    e(g, "_Services", "deny", F + "/_Services/Popup/Scripts/Internal/P.cs")
    e(g, "Wallet/Scripts", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, "*", "deny", F + "/Wallet/Scripts/Internal/W.cs")
    e(g, "Wallet\n", "allow", F + "/Wallet/Scripts/Internal/W.cs")      # surrounding whitespace is trimmed
    e(g, "Wallet x", "deny", F + "/Wallet/Scripts/Internal/W.cs")

    # ---- unlisted zones
    g = "unlisted"
    r(g, W, "deny", F + "/Shop/Tests/T.cs")
    r(g, W, "allow", FX + "/CLAUDE.md")
    e(g, W, "deny", FX + "/CLAUDE.md")
    w(g, W, "deny", FX + "/notes.md")
    w(g, W, "deny", A + "/Shared/Util.cs")
    w(g, W, "deny", F + "/NewFeature/Scripts/Internal/N.cs")
    r(g, W, "deny", r"C:\Windows\win.ini")
    w(g, W, "deny", tempfile.gettempdir() + "/guard-out.txt")
    r(g, W, "deny", FX)
    r(g, W, "deny", FX + "2/CLAUDE.md")     # sibling folder that shares the root's prefix

    # ---- attempts to get around the guard
    g = "evasion"
    e(g, W, "deny", F + "/Wallet/../Shop/Scripts/Internal/S.cs")
    r(g, W, "deny", F + "/Wallet/../Shop/Scripts/Internal/S.cs")
    r(g, W, "deny", (F + "/Shop/Scripts/Internal/S.cs").lower())
    e(g, W, "deny", (F + "/Shop/Scripts/Internal/S.cs").replace("\\", "/"))
    e(g, W, "allow", (F + "/Wallet/Scripts/Internal/W.cs").replace("\\", "/"))
    t(g, W, "allow", "Edit", {"file_path": "Scripts/Internal/W.cs"}, F + "/Wallet")
    t(g, W, "deny", "Edit", {"file_path": "../Shop/Scripts/Internal/S.cs"}, F + "/Wallet")
    t(g, W, "deny", "Read", {"file_path": "~/x.txt"})                           # home-relative
    t(g, W, "deny", "Glob", {"pattern": "~/**", "path": F + "/Wallet"})
    t(g, W, "deny", "Read", {"file_path": F + "/Wallet/Scripts/Internal/W\x00.cs"})                # NUL
    if WIN:      # case-insensitive file system; backslash, drive, UNC, ADS, trailing dot/space, Git Bash paths
        e(g, W, "allow", F + "/WALLET/scripts/internal/W.cs")
        e(g, W, "allow", F + "/wallet/SCRIPTS/VIEW/V.cs")
        e(g, W, "deny", F + "/wallet/SCRIPTS/CONTRACTS/I.cs")      # still the skeleton, in any letter case
        t(g, W, "allow", "Edit", {"file_path": r"Scripts\Internal\W.cs"}, F + "/Wallet")
        t(g, W, "deny", "Edit", {"file_path": r"..\Shop\Scripts\Internal\S.cs"}, F + "/Wallet")
        msys = "/" + FX[0].lower() + FX[2:].replace("\\", "/")
        e(g, W, "deny", msys + "/UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs")
        e(g, W, "allow", msys + "/UnityProject/Assets/Features/Wallet/Scripts/Internal/W.cs")
        r(g, W, "deny", F + "/Shop/Scripts/Internal/S.cs:stream")
        r(g, W, "deny", F + "/Wallet/Scripts/Internal/W.cs::$DATA")
        r(g, W, "deny", F + "\\Shop\\Scripts\\Internal.\\S.cs")
        r(g, W, "deny", F + "\\Shop\\Scripts\\Internal \\S.cs")
        r(g, W, "deny", F + "/Wallet/Scripts/Intern~1/W.cs")                   # 8.3 short name
        r(g, W, "deny", "\\\\localhost\\" + FX[0] + "$" + FX[2:] + "/UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs")
        r(g, W, "deny", "\\\\?\\" + F + "/Shop/Scripts/Internal/S.cs")
        r(g, W, "deny", FX[:2] + F[3:] + "/Shop/Scripts/Internal/S.cs")     # drive-relative: D:Users\...
        r(g, W, "deny", F[2:] + "/Shop/Scripts/Internal/S.cs")              # root-relative: \Users\...
    else:        # case-sensitive file system; a backslash is a file-name character, not a separator
        e(g, W, "deny", F + "/WALLET/scripts/internal/W.cs")                 # another folder, not Wallet
        e(g, W, "deny", F + "/wallet/Scripts/Internal/W.cs")
        e(g, W, "deny", A.replace("/Assets", "/assets") + "/Features/Wallet/Scripts/Internal/W.cs")
        e(g, W, "deny", F.replace("/Features", "/features") + "/Wallet/Scripts/Internal/W.cs")
        e(g, W, "deny", FX + "/management/proposals/new.md")                    # not Management/proposals
        e(g, W, "deny", FX + "/Management/tools/RULES.json")                    # not rules.json: no ask
        r(g, W, "deny", (F + "/Shop/Scripts/Internal/S.cs").upper())
        r(g, W, "deny", F + "/Shop/scripts/contracts/IShop.cs")               # private, not Contracts
        w(g, W, "deny", FX + "/.CLAUDE/settings.json")                          # deny zones match any case
        w(g, W, "deny", FX + "/UnityProject/LIBRARY/x.txt")
        t(g, W, "deny", "Edit", {"file_path": r"Scripts\Internal\W.cs"}, F + "/Wallet")
        t(g, W, "deny", "Edit", {"file_path": r"..\Shop\Scripts\Internal\S.cs"}, F + "/Wallet")
        r(g, W, "deny", F + "\\Shop\\Scripts\\Internal\\S.cs")
        r(g, W, "deny", "//" + F.lstrip("/") + "/Shop/Scripts/Internal/S.cs")
        e(g, W, "allow", "//" + F.lstrip("/") + "/Wallet/Scripts/Internal/W.cs")       # '//x' is '/x'
        r(g, W, "allow", F + "/Wallet/Scripts/Internal/W.cs::$DATA")         # ':' is a normal character here
        r(g, W, "deny", F + "/Shop/Scripts/Internal/S.cs:stream")
        e(g, W, "allow", F + "/Wallet/Scripts/Internal/Intern~1.cs")         # '~1' is a normal name here
        r(g, W, "deny", "/d/" + F.lstrip("/") + "/Shop/Scripts/Internal/S.cs")   # no Git Bash drive mapping
    r(g, W, "deny", F + "/Wallet/link/S.cs")
    w(g, W, "deny", F + "/Wallet/link/New.cs")
    t(g, W, "deny", "NotebookEdit", {"notebook_path": F + "/Shop/Scripts/Internal/n.ipynb"})
    t(g, W, "deny", "MultiEdit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"})
    t(g, W, "deny", "Write", {"file_path": ["not", "a", "string"]})
    t(g, W, "deny", "Write", {})

    # ---- search tools
    g = "search"
    t(g, W, "deny", "Grep", {"pattern": "class", "path": FX})
    t(g, W, "deny", "Grep", {"pattern": "class"})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F + "/Shop"})
    t(g, W, "allow", "Grep", {"pattern": "class", "path": F + "/Wallet"})
    t(g, W, "allow", "Grep", {"pattern": r"a\.\.b", "path": F + "/Wallet"})
    t(g, W, "allow", "Grep", {"pattern": "class", "path": F + "/Shop/Scripts/Contracts"})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F + "/Wallet", "glob": "../Shop/**"})
    t(g, W, "allow", "Glob", {"pattern": "**/*.cs", "path": F + "/Wallet"})
    t(g, W, "allow", "Glob", {"pattern": "**/*.cs"}, F + "/Wallet")
    t(g, W, "deny", "Glob", {"pattern": "**/*.cs"})
    t(g, W, "deny", "Glob", {"pattern": "../Shop/**/*.cs", "path": F + "/Wallet"})
    t(g, W, "deny", "Glob", {"pattern": (F + "/Shop/**/*.cs").replace("\\", "/"), "path": F + "/Wallet"})
    t(g, W, "deny", "Glob", {"pattern": "*", "path": FX + "/UnityProject/Library"})
    t(g, W, "allow", "Glob", {"pattern": "*", "path": A + "/Core"})

    # ---- shell and other tools
    g = "shell"
    t(g, W, "deny", "Bash", {"command": "ls"})
    t(g, W, "deny", "Bash", {"command": "cat UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs"})
    t(g, W, "deny", "Bash", {"command": "rm -rf UnityProject"})
    t(g, W, "deny", "PowerShell", {"command": "Get-ChildItem"})
    t(g, W, "deny", "PowerShell", {"command": '$env:FEATURE = "@human"'})
    t(g, W, "allow", "PowerShell", {"command": VALIDATE})
    t(g, W, "allow", "Bash", {"command": VALIDATE})
    t(g, W, "allow", "Bash", {"command": "  " + VALIDATE + "\n"})
    t(g, W, "deny", "Bash", {"command": VALIDATE + "; rm x"})
    t(g, W, "deny", "Bash", {"command": VALIDATE + "\nrm x"})
    t(g, W, "deny", "Bash", {"command": VALIDATE + r" --root C:\x"})
    t(g, W, "deny", "Bash", {"command": "python Management/tools/test_validate.py"})
    t(g, W, "deny", "Bash", {"command": "python Management/tools/validateXpy"})
    t(g, W, "deny", "Bash", {"command": 'python -c "print(1)"'})
    t(g, "", "allow", "PowerShell", {"command": VALIDATE})      # read-only script, harmless without FEATURE

    g = "tools"
    t(g, W, "deny", "mcp__unity__manage_script", {"path": "x"})
    t(g, W, "deny", "Monitor", {"command": "ls"})
    t(g, W, "deny", "Artifact", {"file_path": F + "/Shop/Scripts/Internal/S.cs"})
    t(g, W, "pass", "TodoWrite", {"todos": []})
    t(g, W, "pass", "AskUserQuestion", {"questions": []})

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
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal")
    d(W, "ask", "Assets/Features/Wallet")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal/Missing.cs")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/AssemblyInfo.cs")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    d(W, "allow", "Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    d("", "deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    d("@human", "pass", "Assets/Features/Shop/Scripts/Internal/S.cs")
    t(g, W, "deny", "mcp__unityMCP__manage_script", {"action": "delete", "name": "Old", "path": "Assets/Features/Wallet/"})
    t(g, W, "deny", "mcp__unityMCP__delete_script", {"uri": "Assets/Features/Wallet/Scripts/Internal/Old.cs"})
    t(g, W, "deny", "mcp__unityMCP__execute_menu_item", {"menu_path": "Assets/Delete"})

    # ---- boundary files inside the agent's own feature
    g = "boundary files (the skeleton: only @CodeLead)"
    w(g, W, "deny", F + "/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    e(g, W, "deny", F + "/Wallet/Tests/Game.Wallet.Tests.asmdef")
    e(g, W, "deny", F + "/Wallet/Scripts/Internal/game.wallet.internal.ASMDEF")      # any letter case
    w(g, W, "deny", F + "/Wallet/Scripts/Internal/x.asmref")
    w(g, W, "deny", F + "/Wallet/Scripts/Internal/X.ASMREF")
    e(g, W, "deny", F + "/Wallet/Scripts/Internal/AssemblyInfo.cs")
    other = F + "/Wallet/Scripts/Internal/Other.cs"
    t(g, W, "deny", "Write", {"file_path": other, "content": '[assembly: InternalsVisibleTo("Game.Shop.Internal")]'})
    t(g, W, "deny", "Edit", {"file_path": other, "old_string": "a", "new_string": 'InternalsVisibleTo("X")'})
    t(g, W, "deny", "MultiEdit", {"file_path": other, "edits": [{"old_string": "a", "new_string": "InternalsVisibleTo"}]})
    t(g, W, "allow", "Write", {"file_path": other, "content": "internal class Other {}"})
    w(g, W, "deny", F + "/Shop/Scripts/Internal/Game.Shop.Internal.asmdef")

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
    cli(g, W, "allow", "Edit own feature", payload("Edit", {"file_path": F + "/Wallet/Scripts/Internal/W.cs"}))
    cli(g, W, "deny", "Edit other feature", payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}))
    cli(g, W, "deny", "Edit Core", payload("Edit", {"file_path": A + "/Core/Base.cs"}))
    cli(g, W, "pass", "TodoWrite", payload("TodoWrite", {}))
    cli(g, "", "deny", "Edit, FEATURE unset", payload("Edit", {"file_path": F + "/Wallet/Scripts/Internal/W.cs"}))
    cli(g, "@human", "pass", "Edit, @human", payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}))
    cli(g, "@human", "pass", "Edit .claude, @human", payload("Edit", {"file_path": FX + "/.claude/settings.json"}))
    cli(g, W, "deny", "malformed JSON", "{ not json")
    cli(g, W, "deny", "empty input", " ")
    cli(g, W, "deny", "JSON array instead of object", "[1,2]")
    cli(g, W, "deny", "no tool_name", "{}")
    cli(g, W, "allow", "UTF-8 path", payload("Write", {"file_path": F + "/Wallet/Scripts/Internal/Ví.cs"}))
    cli(g, "", "deny", "Edit, unbound, FEATURE=@human in the environment is ignored",
        payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}), env_feature="@human")
    cli(g, "", "deny", "Edit, unbound, FEATURE=Wallet in the environment is ignored",
        payload("Edit", {"file_path": F + "/Wallet/Scripts/Internal/W.cs"}), env_feature=W)
    cli(g, W, "deny", "Edit, bound to Wallet, FEATURE=@human in the environment is ignored",
        payload("Edit", {"file_path": F + "/Shop/Scripts/Internal/S.cs"}), env_feature="@human")
    run_binding_cases(payload)
    run_role_cases(payload)
    run_report_cases()
    run_codelead_cases()
    run_designer_cases()
    run_advisor_cases()
    run_progress_cases()
    run_unity_cases()
    run_tool_cases()
    real = str(HERE.parent.parent)
    cli("real root", "", "deny", "Write <real root>\\x.txt, no --root",
        json.dumps({"tool_name": "Write", "tool_input": {"file_path": real + "/x.txt"}, "cwd": real}), root=None)
    cli("real root", "", "allow", "Read <real root>\\CLAUDE.md, no --root",
        json.dumps({"tool_name": "Read", "tool_input": {"file_path": real + "/CLAUDE.md"}, "cwd": real}), root=None)


def run_advisor_cases():
    """@DesignAdvisor asks GPT about the design: reads the design tree, the studio, the root CLAUDE.md and the rules
    of the design roles; writes only its questions and notes in Management/game-design/advice; runs gd_advise.py and
    gd_status.py. It never writes the design, and nobody else writes in advice."""
    import bind_session
    W, D, V = "Wallet", "@GameDesigner", "@DesignAdvisor"
    base = FX + "/Management/game-design"
    idea, live, advice = base + "/ai/idea", base + "/live", base + "/advice"
    for d in (idea, live, advice + "/feature-hero"):
        os.makedirs(d, exist_ok=True)

    g = "advisor: writes only its questions and notes"
    for p in (advice + "/feature-hero/q1.q.md", advice + "/feature-hero/q1.note.md", advice + "/feature-hero.hero-list/round-2.q.md",
              advice + "/_root/g1.q.md", advice + "/_general/g1.note.md"):
        w(g, V, "allow", p)
        e(g, V, "allow", p)
    for p in (advice + "/feature-hero/q1.md", advice + "/feature-hero/q1.v1.md", advice + "/feature-hero/q1.rejected.md",
              advice + "/q1.q.md", advice + "/feature-hero/sub/q1.q.md", advice + "/feature-hero/q1.q.txt",
              advice + "/_other/q1.q.md", advice + "/feature-hero/hero-list/q1.q.md", advice + "/feature_hero/q1.q.md"):
        w(g + " (the answers are the command's)", V, "deny", p)
    for p in (idea + "/data.md", idea + "/feature-hero/data.md", idea + "/notes.md", live + "/data.md", base + "/mode.json",
              base + "/x.md", FX + "/Management/proposals/new.md", FX + "/Management/studio/codex/lessons.md",
              FX + "/Management/studio/codex/orders/_design/feature-hero.cheat/a/order.md", FX + "/CLAUDE.md",
              FX + "/.claude/rules/designadvisor.md", F + "/Wallet/Scripts/Internal/W.cs", A + "/Core/Base.cs"):
        w(g + " (never the design or anything else)", V, "deny", p)
    t(g, V, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, V, "deny", "mcp__unityMCP__refresh_unity", {})
    t(g, V, "deny", "mcp__unityMCP__read_console", {"action": "get"})
    t(g, V, "pass", "SendMessage", {"to": "GameDesigner", "message": "x"})

    g = "advisor: the others do not write in advice"
    for who in (W, D, "@Manager", "@CodeLead", "@Core", "@Codex", ""):
        w(g, who, "deny", advice + "/feature-hero/q1.q.md")
        w(g, who, "deny", advice + "/feature-hero/q1.md")
    for who in (W, D, "@Manager", "@CodeLead", "@Codex"):
        r(g + " (they read it)", who, "allow", advice + "/feature-hero/q1.md")

    g = "advisor: reads the design, not the code"
    for p in (idea + "/data.md", live + "/data.md", advice + "/feature-hero/q1.md", base, base + "/mode.json",
              FX + "/Management/studio/codex/lessons.md", FX + "/CLAUDE.md", FX + "/.claude/rules/designadvisor.md",
              FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/rules/gamedesigner.round3.md"):
        r(g, V, "allow", p)
    t(g, V, "allow", "Grep", {"pattern": "hero", "path": base})
    for p in (FX + "/.claude/rules/codelead.md", FX + "/.claude/rules/studio.md", FX + "/.claude/hooks/guard.py",
              FX + "/Management/proposals/old.md", FX + "/Management/tools/gd_advise.py", FX + "/Management",
              F + "/Wallet/Scripts/Contracts/IWallet.cs", F + "/Wallet/CLAUDE.md", A + "/Core/Base.cs", "C:/Windows/x"):
        r(g, V, "deny", p)
    t(g, V, "deny", "Grep", {"pattern": "x"})

    g = "advisor: commands"
    ADVISE, STATUS = "python Management/tools/gd_advise.py", "python Management/tools/gd_status.py"
    for cmd in (ADVISE + " feature-hero q1", ADVISE + " feature-hero.hero-list.hero-roster round-2", ADVISE + " _root g1",
                ADVISE + " _general g1", "python3 Management/tools/gd_advise.py feature-hero q1", STATUS, STATUS + " ai/idea/feature-hero",
                "python Management/tools/gd_pack.py Wallet", "python Management/tools/gd_frame.py live/feature-hero/a/b",
                "python Management/tools/studio.py status"):
        t(g, V, "allow", "Bash", {"command": cmd})
    for cmd in (ADVISE, ADVISE + " feature-hero", ADVISE + " feature-hero q1 extra", ADVISE + " feature-hero/hero-list q1",
                ADVISE + " ../x q1", ADVISE + " Feature-Hero q1", ADVISE + " feature-hero Q1", ADVISE + " feature-hero q1; ls",
                ADVISE + " feature-hero q1 --root C:/x", ADVISE + " _other q1", "codex exec -s read-only x",
                "python Management/tools/studio.py consult Wallet/a b", "python Management/tools/studio.py codex Wallet/a b",
                "python Management/tools/gd_delete.py ai/idea/feature-hero",
                'python Management/tools/studio.py redo _design/feature-hero.cheat/a b "x"',
                "python Management/tools/gd_progress.py ai/idea/a idea done", "ls"):
        t(g, V, "deny", "Bash", {"command": cmd})
    for who in (W, D, "@Manager", "@CodeLead", "@Core", "@Codex", ""):
        t(g + " (only the advisor asks GPT)", who, "deny", "Bash", {"command": ADVISE + " feature-hero q1"})

    g = "advisor: bind"
    say(g, "block, bound=@DesignAdvisor", "bind @DesignAdvisor", f"FEATURE=@designadvisor {PIN}", session="role-adv")
    told = bind_session.context({"session_id": "role-adv"}, FX, STORE)
    check(g, V, True, "the context names the advice folder, the command, who may ask and the rules",
          all(s in told for s in ("FEATURE=@DesignAdvisor", "game-design/advice", "gd_advise.py", "người hoặc @GameDesigner",
                                  ".claude/rules/designadvisor.md", "Full Auto", "điểm NGƯỜI chấm", "không được bỏ")))
    check(g, V, False, "the context never has the PIN", PIN in told)
    told = bind_session.context({"session_id": "role-gd"}, FX, STORE)
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
    say(g, "block, bound=Wallet", "right PIN", f"FEATURE=Wallet {PIN}")
    say(g, "block, bound=_Services/Popup", "right PIN, wrapped in client context", f"{ide}\nFEATURE=_Services/Popup {PIN}",
        session="chat-ide")
    say(g, "block, bound=Wallet", "status of a bound chat", "feature")
    say(g, "pass, bound=Wallet", "next ordinary prompt tells the model its binding", "Làm tiếp đi")
    say(g, "block, bound=Wallet", "wrong PIN keeps the old binding", "FEATURE=@human 0000")
    say(g, "block, bound=_Services/Popup", "service, backslash, spaces, lower case",
        f"  feature = _Services\\Popup   {PIN}\n", session="chat2")
    say(g, "block, bound=@human", "human", f"FEATURE=@human {PIN}", session="chat3")
    say(g, "block, bound=Shop", "the human may rebind", f"FEATURE=Shop {PIN}", session="chat3")
    say(g, "block, bound=@human", "and bind back", f"FEATURE=@human {PIN}", session="chat3")
    say(g, "pass, bound=@human", "after a rebind the context follows the new binding", "tiếp", session="chat3")

    # ---- the guard reads the binding of the chat that makes the call
    g = "bind: guard uses it"
    b(g, "allow", "chat1", "Edit", {"file_path": own})
    b(g, "deny", "chat1", "Edit", {"file_path": S})
    b(g, "deny", "chat1", "Bash", {"command": "ls"})
    b(g, "allow", "chat2", "Edit", {"file_path": F + "/_Services/Popup/Scripts/Internal/P.cs"})
    b(g, "deny", "chat2", "Edit", {"file_path": own})
    b(g, "pass", "chat3", "Edit", {"file_path": S})
    b(g, "pass", "chat3", "Edit", {"file_path": FX + "/.claude/hooks/guard.py"})
    b(g, "deny", "other-chat", "Edit", {"file_path": own})           # a binding belongs to one chat only
    b(g, "deny", None, "Edit", {"file_path": own})
    b(g, "deny", "..\\chat1", "Edit", {"file_path": own})
    Path(guard.binding_path("broken", STORE)).write_text("{ not json")
    b(g, "deny", "broken", "Edit", {"file_path": own})
    Path(guard.binding_path("gone", STORE)).write_text('{"feature": "Nope"}')
    b(g, "deny", "gone", "Edit", {"file_path": own})                 # a binding to a missing feature
    b(g, "allow", "chat1", "Bash", {"command": VALIDATE + " --feature Wallet"})
    b(g, "allow", "chat1", "Bash", {"command": VALIDATE + " --feature _Services/Popup"})
    b(g, "deny", "chat1", "Bash", {"command": VALIDATE + " --feature ../x"})
    b(g, "deny", "chat1", "Bash", {"command": VALIDATE + " --feature Wallet --root x"})

    # ---- the real guard process reads the store it is given, and none when it is given none
    g = "bind: process"
    with_session = lambda s: json.dumps({"tool_name": "Edit", "tool_input": {"file_path": own}, "cwd": FX,  # noqa: E731
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
        check(g, "", expect, f"Edit own feature, session={session}, store={'yes' if store else 'no'}", got)

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
        w(g, role, "allow", FX + "/Management/proposals/new.md")
        e(g, role, "ask", FX + "/Management/tools/rules.json")
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
    w(g, M, "allow", FX + "/Management/proposals/plan.md")
    e(g, M, "deny", FX + "/Management/proposals/old.md")
    r(g, M, "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    r(g, M, "allow", A + "/Core/Old.cs")
    r(g, M, "deny", own_w)
    t(g, M, "allow", "Bash", sh(VALIDATE))
    t(g, M, "allow", "Bash", sh(VALIDATE + " --feature Shop"))
    t(g, M, "deny", guard.UNITY_ASSET_TOOL, delete("Assets/Core/Old.cs"))
    t(g, M, "pass", "SendMessage", {"to": "Shop", "message": "x"})
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
        for who in ("Wallet", M, "@GameDesigner", "@DesignAdvisor", "@Codex", "@CodeLead", "@Core", ""):
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
        for who in ("Wallet", M, "@GameDesigner", "@Codex"):
            t(g + ": writes its scratchpad", who, "allow", "Write", {"file_path": mine[2], "content": "x"}, session=ME)
        t(g + ": unbound writes nothing", "", "deny", "Write", {"file_path": mine[2], "content": "x"}, session=ME)
        for p in (mine[0], mine[1], os.path.join(temp_of(OTHER), "scratchpad", "note.py"), os.path.join(temp_of(ME), "scratchpad"),
                  os.path.join(temp_of(ME), "other", "x.txt")):
            t(g + ": writes only inside its scratchpad", "Wallet", "deny", "Write", {"file_path": p, "content": "x"}, session=ME)
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
    for who in ("Wallet", "@CodeLead", "@GameDesigner", "@Core", ""):
        t(g + " (others)", who, "deny", "Bash", sh(SCAFFOLD))
    for who in ("@CodeLead", "@GameDesigner"):      # looking is every Leader's, creating is @Manager's
        t(g + " (dry run: every Leader)", who, "allow", "Bash", sh(SCAFFOLD + " --dry-run"))
    for who in ("Wallet", "@Core", "@DesignAdvisor", "@Codex", ""):
        t(g + " (dry run: others)", who, "deny", "Bash", sh(SCAFFOLD + " --dry-run"))

    g = "leaders: a chain of allowed commands"
    CHAIN = "python Management/tools/studio.py status; python Management/tools/validate.py; python Management/tools/scaffold.py --dry-run"
    for who in (M, "@CodeLead", "@GameDesigner"):
        for tool in ("Bash", "PowerShell"):
            t(g, who, "allow", tool, sh(CHAIN))
        t(g, who, "allow", "Bash", sh("python Management/tools/validate.py && python Management/tools/gd_status.py"))
        t(g, who, "allow", "Bash", sh("python Management/tools/validate.py;python Management/tools/studio.py check"))
    for bad in (CHAIN + "; ls", CHAIN + "; rm -rf UnityProject", "ls; " + CHAIN, CHAIN + " | more", CHAIN + " > out.txt",
                CHAIN + "; $(ls)", CHAIN + ";", "; " + CHAIN, CHAIN.replace("; ", " & "), CHAIN.replace("; ", " || "),
                "; ".join(["python Management/tools/validate.py"] * 7),
                'python Management/tools/validate.py; python Management/tools/studio.py redo _design/feature-hero.cheat/a b "x"',
                "python Management/tools/validate.py; python Management/tools/../tools/validate.py"):
        t(g + " (refused)", M, "deny", "Bash", sh(bad))
    t(g + " (refused)", "@CodeLead", "deny", "Bash", sh("python Management/tools/validate.py; python Management/tools/scaffold.py"))
    for who in ("Wallet", "@Core", "@DesignAdvisor", "@Codex", ""):      # the others run one command a call
        t(g + " (others)", who, "deny", "Bash", sh("python Management/tools/validate.py; python Management/tools/studio.py status"))
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
        t(g, "Wallet", "deny", "Bash", sh(cmd))
    w(g, M, "deny", A + "/../UnityProject/Assets/Features/Hero/Scripts/Internal/.gitkeep")      # still no file edits

    g = "any chat: design frame command"
    FRAME = "python Management/tools/gd_frame.py"
    for who in ("Wallet", "_Services/Popup", "@CodeLead", "@GameDesigner", M, "@Codex"):
        for cmd in (FRAME + " ai/idea/feature-hero/hero-list/hero-roster-values", FRAME + " live/feature-popup-lose/lose-actions/actions-spec",
                    "python3 Management/tools/gd_frame.py ai/idea/feature-hero"):
            t(g, who, "allow", "Bash", sh(cmd))
    for cmd in (FRAME, FRAME + " ai/idea", FRAME + " ai/idea/feature-hero --html x.html", FRAME + " ai/idea/../x",
                FRAME + " ai/other/feature-hero", FRAME + " ai/idea/feature-hero && ls", FRAME + " ai/idea/Feature-Hero"):
        t(g, "Wallet", "deny", "Bash", sh(cmd))
    t(g + " (html)", "@human", "pass", "Bash", sh(FRAME + " ai/idea/feature-hero --html x.html"))

    g = "role spelling"
    e(g, "@core", "deny", A + "/Core/Old.cs")          # stored values are exact; bind_session stores "@Core"
    e(g, "@MANAGER", "deny", A + "/Core/Old.cs")
    e(g, "@Cor", "deny", A + "/Core/Old.cs")
    cli(g, "@Core", "allow", "process: @Core edits Core", payload("Edit", {"file_path": A + "/Core/Old.cs"}))
    cli(g, "@Manager", "deny", "process: @Manager edits Core", payload("Edit", {"file_path": A + "/Core/Old.cs"}))

    g = "bind: roles"
    say(g, "block, bound=@Core", "bind @Core", f"FEATURE=@Core {PIN}", session="role-core")
    say(g, "pass, bound=@Core", "@Core is told its area", "làm đi", session="role-core")
    say(g, "block, bound=None", "@Composition is no role any more", f"FEATURE=@Composition {PIN}", session="role-comp")
    say(g, "block, bound=@Manager", "bind @Manager", f"FEATURE=@Manager {PIN}", session="role-mgr")
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

    def rep(expect, label, session, entries, active=False, transcript=True, keep_pending=False):
        pending = stop_report.pending_path(session, STORE)
        if not keep_pending and os.path.exists(pending):
            os.remove(pending)
        path = FX + f"/transcript-{len(results)}.jsonl"
        if transcript:
            Path(path).write_text("\n".join(json.dumps(x, ensure_ascii=False) for x in entries) + "\n", encoding="utf-8")
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

    saved = sorted(os.listdir(reports))
    text = Path(reports, saved[0]).read_text(encoding="utf-8")
    check("stop report", "", True, "saved file holds the block and who wrote it",
          "## Báo cáo cho @Manager" in text and "FEATURE=Wallet" in text and "Xong rồi" not in text)
    first = bind_session.context({"session_id": "rep-mgr"}, FX, STORE)
    check("stop report", "", True, "@Manager is told about every new report",
          f"{len(saved)} báo cáo mới" in first and all(n in first for n in saved))
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

    g = "codelead: edits nothing outside the skeleton"
    for p in (internal, F + "/Wallet/Scripts/View/V.cs", F + "/Wallet/Tests/T.cs", F + "/Wallet/FeatureAssets/Private/c.asset",
              F + "/Wallet/FeatureAssets/Public/Icon.prefab", F + "/Wallet/CLAUDE.md", A + "/Core/Old.cs",
              A + "/Core/Tests/IFake.cs",
              FX + "/Management/proposals/decision.md", FX + "/Management/tools/rules.json",
              FX + "/Management/data/x.js", FX + "/.claude/settings.json", FX + "/CLAUDE.md"):
        w(g, L, "deny", p)
    t(g, L, "deny", "MultiEdit", {"file_path": internal, "edits": []})
    t(g, L, "deny", "NotebookEdit", {"notebook_path": internal})
    t(g, L, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Core/Old.cs"})
    t(g, L, "allow", "Bash", {"command": VALIDATE})
    t(g, L, "deny", "Bash", {"command": "dotnet build"})
    t(g, L, "pass", "SendMessage", {"to": W, "message": "Sửa giúp ..."})

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
    t(g, L, "ask", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/_Composition"})
    if not WIN:
        w(g + ": case-sensitive", L, "deny", A + "/_composition/Boot.cs")
    for who in ("Wallet", "@Core", "@Manager", "@GameDesigner", "@Codex", ""):
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
        for who in (W, "@Core", "_Services/Popup", "@Manager", "@GameDesigner", "@Codex", ""):
            t(g + " (others still may not)", who, "deny", "Grep", {"pattern": "x", "path": p})
    for p in (A, FX + "/UnityProject", FX):
        t(g, L, "deny", "Grep", {"pattern": "x", "path": p})
    e(g, L, "deny", F + "/New.cs")
    for p in (FX + "/UnityProject/Library/x", FX + "/UnityProject/Temp/x", FX + "/secret.txt", r"C:\Windows\x"):
        r(g, L, "deny", p)
    r(g + " (others still may not)", W, "deny", F + "/Shop/Scripts/Internal/S.cs")
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
                         ("manage_packages", "add"), ("manage_camera", "screenshot"), ("manage_gameobject", "create"),
                         ("manage_components", "add"), ("read_console", "clear")):
        u(name, expect="allow", action=action)      # project settings, menus and the scene: @CodeLead, holding the lock
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
    t(g + " (others)", W, "allow", "mcp__unityMCP__refresh_unity", {})
    t(g + " (others)", W, "allow", "ReadMcpResourceTool", {"server": "unityMCP", "uri": "x"})
    for who in ("@Manager", "@GameDesigner", ""):
        t(g + " (others)", who, "deny", "mcp__unityMCP__read_console", {"action": "get"})
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
    for p in (F + "/Wallet/Scripts/Internal/Basement.cs", F + "/Wallet/Scripts/Internal/Sub/Base/X.cs",
              F + "/Wallet/Scripts/Contracts.cs", A + "/Core/Icon.png", A + "/Core/ServiceRegistry.cs", A + "/Core/Ix.cs"):
        e(g + ": not skeleton", L, "deny", p)
    e(g, L, "deny", F + "/Wallet/Scripts/Internal/x.asmref")
    if not WIN:
        e(g + ": case-sensitive", L, "deny", F + "/Wallet/scripts/contracts/IWallet.cs")
        e(g + ": case-sensitive", L, "deny", F + "/Wallet/Scripts/Internal/base/B.cs")
    dl = lambda expect, path: t(g, L, expect, guard.UNITY_ASSET_TOOL, {"action": "delete", "path": path})  # noqa: E731
    dl("allow", "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs")
    dl("allow", "Assets/Features/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    dl("deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    dl("deny", "Assets/Features/Shop/Scripts/Internal/S.cs")
    dl("allow", "Assets/Features/Wallet/Scripts/Contracts")            # a skeleton folder is @CodeLead's own: no question
    os.makedirs(F + "/Wallet/Scripts/Internal/Base", exist_ok=True)
    os.makedirs(A + "/_Composition/Scenes", exist_ok=True)
    dl("allow", "Assets/Features/Wallet/Scripts/Internal/Base")
    dl("allow", "Assets/_Composition/Scenes")
    dl("ask", "Assets/_Composition")                                   # the whole wiring area: the human decides
    dl("ask", "Assets/Features/Wallet/Scripts/Contracts/Missing")      # nothing there: not a folder it can be sure of
    dl("deny", "Assets/Features/Wallet/Scripts/Internal")              # not skeleton: the owner's
    dl("deny", "Assets/Features/Wallet")
    dl("deny", "Assets/Features/Shop/Scripts/Internal/Base")           # Shop's code gate is closed
    dl("deny", "Assets/Core")
    t(g, L, "deny", guard.UNITY_ASSET_TOOL, {"action": "modify", "path": "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs"})

    g = "feature: skeleton read-only"
    r(g, W, "allow", F + "/Wallet/Scripts/Internal/Base/HeroBase.cs")
    e(g, W, "deny", F + "/Wallet/Scripts/Internal/Base/HeroBase.cs")
    w(g, W, "deny", F + "/Wallet/Scripts/View/Base/New.cs")
    w(g, W, "allow", F + "/Wallet/Scripts/Internal/HeroLogic.cs")          # the subclass
    w(g, W, "allow", F + "/Wallet/Scripts/View/HeroView.cs")
    r(g, "_Services/Popup", "deny", F + "/Wallet/Scripts/Internal/Base/HeroBase.cs")      # another feature's base is private
    r(g, "_Services/Popup", "allow", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    r(g, "@Core", "allow", A + "/Core/IServiceRegistry.cs")
    e(g, "@Core", "deny", A + "/Core/IServiceRegistry.cs")
    e(g, "@Core", "allow", A + "/Core/ServiceRegistry.cs")
    for who in ("@Manager", "@GameDesigner", ""):
        e(g + " (others)", who, "deny", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g + " (human)", "@human", "pass", F + "/Wallet/Scripts/Contracts/IWallet.cs")
    e(g + " (human)", "@human", "pass", A + "/Core/IServiceRegistry.cs")
    for p in (A + "/Core/Old.cs", A + "/_Composition/Old.cs"):
        e(g + ": a feature never edits Core/_Composition", W, "deny", p)
    e(g, W, "ask", FX + "/Management/tools/rules.json")
    e(g, W, "deny", FX + "/Management/approvals/a.json")          # the old approval folder means nothing now

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
    mgr = bind_session.context({"session_id": "rep-mgr"}, FX, STORE)
    check(g, "@Manager", True, "@Manager keeps its own read list", "20990101-000000-Shop.md" in mgr)


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
              F + "/Wallet/Scripts/Internal/W.cs", A + "/Core/Base.cs", FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/rules/gamedesigner.round2.md",
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
    t(g, D, "deny", "mcp__unityMCP__refresh_unity", {})
    t(g, D, "pass", "SendMessage", {"to": "@Manager", "message": "x"})

    g = "designer: reads only the design tree"
    for p in (idea + "/data.md", live + "/data.md", idea + "/feature-hero/.images/a.png", base, base + "/mode.json",
              idea + choose + "/answers.json", idea + "/.user-review/review.json", idea + "/.history/log.jsonl",
              FX + "/.claude/rules/gamedesigner.md", FX + "/.claude/rules/gamedesigner.round1.md",
              FX + "/.claude/rules/gamedesigner.round4.md", FX + "/.claude/rules/designadvisor.md"):
        r(g, D, "allow", p)
    r(g, D, "allow" if WIN else "deny", FX + "/.claude/rules/GameDesigner.Round4.md")      # same file only on Windows
    for p in (FX + "/.claude/rules/codelead.md", FX + "/.claude/rules/gamedesigner.round5.md", FX + "/.claude/rules/gamedesigner.round2.md.bak",
              FX + "/.claude/rules/sub/gamedesigner.md", FX + "/.claude/hooks/gamedesigner.md"):
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
    w(g + " (auto)", D, "allow", hero_dir + "/skills/.frame-concept/main.frame")
    node(hero_dir, based="")
    w(g + " (auto)", D, "deny", skills)                         # no based-on at all
    w(g + " (auto)", D, "deny", hero_dir + "/skills/.frame-concept/main.frame")      # a frame follows its node's lock
    node(hero_dir, based=new_hash)
    os.remove(idea + "/data.md")
    w(g + " (auto)", D, "deny", hero)                           # the root has no data.md
    w(g + " (auto)", D, "allow", idea + "/data.md")
    node(idea, ai=9, idea_text="Viết lại ý.")
    # ---- mode user: the human scored every idea, no open question, and the node is the same in live
    Path(base + "/mode.json").write_text('{"mode": "user"}')
    w(g + " (user)", D, "deny", hero)                           # the human has not scored the root
    score(idea, 8)
    w(g + " (user)", D, "deny", hero)                           # scored, but live differs
    shutil.copy2(idea + "/data.md", live + "/data.md")
    w(g + " (user)", D, "allow", hero)
    os.makedirs(idea + choose)
    Path(questions).write_text('{"questions": [{"id": "q1", "text": "?", "options": [{"id": "a", "label": "A"}]}]}')
    w(g + " (user)", D, "deny", hero)                           # an unanswered question
    w(g + " (user)", D, "allow", questions)
    Path(idea + choose + "/answers.json").write_text('{"answers": {"q1": {"chosen": ["a"], "other": ""}}}')
    w(g + " (user)", D, "allow", hero)
    w(g + " (user)", D, "deny", skills)                         # feature-hero: unscored and not in live
    score(hero_dir, 6)
    for name in ("data.md", "CLAUDE.md"):
        shutil.copy2(hero_dir + "/" + name, live + "/feature-hero/" + name)
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
    shutil.copy2(idea + "/CLAUDE.md", live + "/CLAUDE.md")
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


def run_progress_cases():
    """The code gate for Features and Services sessions, and the one thing @CodeLead writes: progress."""
    import bind_session
    W, S, L = "Wallet", "Shop", "@CodeLead"
    live = FX + "/Management/game-design/live"
    own = F + "/Shop/Scripts/Internal/S.cs"

    g = "code gate"
    e(g, W, "allow", F + "/Wallet/Scripts/Internal/Old.cs")                 # live design names Wallet and reached round 4
    # any letter case of the bound name on Windows; on a case-sensitive system 'wallet' is not the Wallet folder
    e(g, "wallet", "allow" if WIN else "deny", F + "/Wallet/Scripts/Internal/Old.cs")
    e(g, "_Services/Popup", "allow", F + "/_Services/Popup/Scripts/Internal/P.cs")
    for p in (own, F + "/Shop/Scripts/Contracts/IShop.cs", F + "/Shop/Tests/T.cs", F + "/Shop/CLAUDE.md"):
        w(g + " (no design names Shop)", S, "deny", p)
    e(g + " (no design names Shop)", S, "deny", own)
    t(g + " (no design names Shop)", S, "deny", "MultiEdit", {"file_path": own, "edits": []})
    t(g + " (no design names Shop)", S, "deny", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Features/Shop/Scripts/Internal/S.cs"})
    r(g + ": reading is not gated", S, "allow", own)
    w(g + ": proposals are not gated", S, "allow", FX + "/Management/proposals/shop-gate.md")
    t(g + ": validate is not gated", S, "allow", "Bash", {"command": VALIDATE})
    os.makedirs(live + "/feature-shop/detail")
    for d in ("/feature-shop", "/feature-shop/detail"):
        Path(live + d + "/data.md").write_text("# x\n")
    Path(live + "/feature-shop/code.json").write_text('{"unity": ["Shop"]}')
    e(g + " (design stops at round 3)", S, "deny", own)
    os.makedirs(live + "/feature-shop/detail/tech")
    Path(live + "/feature-shop/detail/tech/data.md").write_text("# x\n")
    e(g + " (round 4 in live)", S, "allow", own)
    t(g + " (round 4 in live)", S, "allow", guard.UNITY_ASSET_TOOL, {"action": "delete", "path": "Assets/Features/Shop/Scripts/Internal/S.cs"})
    Path(live + "/feature-shop/code.json").write_text("{ broken")
    e(g + " (unreadable code.json)", S, "deny", own)
    Path(live + "/feature-shop/code.json").write_text('{"unity": ["Shop"]}')
    e(g + ": others", "@Core", "allow", A + "/Core/Old.cs")
    e(g + ": others", "@CodeLead", "allow", A + "/_Composition/Old.cs")
    e(g + ": others", "@human", "pass", F + "/Shop/Scripts/Internal/S.cs")
    cli(g + " (real process)", S, "allow", "Edit own code once the design reached round 4",
        json.dumps({"tool_name": "Edit", "tool_input": {"file_path": own}, "cwd": FX}))
    os.rename(FX + "/Management/tools/gamedesign.py", FX + "/Management/tools/gamedesign.off")
    sys.modules.pop("gamedesign", None)
    e(g + " (gate code missing: closed)", W, "deny", F + "/Wallet/Scripts/Internal/Old.cs")
    e(g + " (gate code missing: closed)", "@Core", "allow", A + "/Core/Old.cs")
    os.rename(FX + "/Management/tools/gamedesign.off", FX + "/Management/tools/gamedesign.py")
    importlib.invalidate_caches()      # this process just failed to import it; a real hook is a new process each time
    shutil.rmtree(live + "/feature-shop")
    cli(g + " (real process)", S, "deny", "Edit own code with no design",
        json.dumps({"tool_name": "Edit", "tool_input": {"file_path": own}, "cwd": FX}))
    for session, value in (("gate-wallet", W), ("gate-shop", S)):
        Path(guard.binding_path(session, STORE)).write_text(json.dumps({"feature": value}))
    check(g, W, True, "an open feature is told it may code", "được code" in bind_session.context({"session_id": "gate-wallet"}, FX, STORE))
    told = bind_session.context({"session_id": "gate-shop"}, FX, STORE)
    check(g, S, True, "a locked feature is told so, and why", "ĐANG KHOÁ" in told and "chưa có node thiết kế nào" in told)

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



def run_tool_cases():
    """@Codex and @Blender: take orders from the features, write only their lessons and the work of an order, run
    their tool through studio.py; everybody reads Management/studio and Resources."""
    X, B, W = "@Codex", "@Blender", "Wallet"
    studio, res = FX + "/Management/studio", FX + "/Resources"
    order = studio + "/codex/orders/Wallet/coin-icons"
    os.makedirs(order + "/work", exist_ok=True)
    os.makedirs(res + "/Wallet/codex/coin-icons/v1", exist_ok=True)
    Path(order + "/order.md").write_text("x")
    Path(res + "/Wallet/codex/coin-icons/v1/coin.png").write_text("x")
    P = "python Management/tools/studio.py"

    g = "tools: everybody reads the studio and Resources"
    for who in (W, "@Core", "@Manager", "@GameDesigner", "@CodeLead", X, B, "_Services/Popup"):
        r(g, who, "allow", order + "/order.md")
        r(g, who, "allow", res + "/Wallet/codex/coin-icons/v1/coin.png")
        t(g, who, "allow", "Grep", {"pattern": "x", "path": studio})
        t(g, who, "allow", "Glob", {"pattern": "**/*.png", "path": res})
    r(g + " (still not the rest)", "@GameDesigner", "deny", FX + "/Management/proposals/old.md")
    r(g + " (still not the rest)", X, "deny", F + "/Shop/Scripts/Internal/S.cs")
    r(g + " (still not the rest)", X, "deny", F + "/Wallet/FeatureAssets/Private/c.asset")
    r(g + " (still not the rest)", X, "allow", F + "/Wallet/FeatureAssets/Public/Icon.prefab")
    r(g + " (still not the rest)", X, "allow", order + "/refs/ref.png")

    g = "tools: what a tool chat writes"
    w(g, X, "allow", studio + "/codex/lessons.md")
    w(g, X, "allow", order + "/work/coin.md")
    e(g, X, "allow", order + "/work/sub/draft.md")
    w(g, X, "allow", studio + "/codex/orders/_Services/Popup/pop-bg/work/bg.md")
    for p in (order + "/order.md", order + "/feedback-1.md", order + "/status.json", order + "/log.jsonl",
              order + "/refs/ref.png", studio + "/blender/lessons.md", studio + "/blender/orders/Wallet/m/work/a.py",
              studio + "/codex/other.md", res + "/Wallet/codex/coin-icons/v1/coin.png", F + "/Wallet/FeatureAssets/Private/x.png",
              F + "/Wallet/Scripts/Internal/Old.cs", FX + "/Management/proposals/new.md", FX + "/CLAUDE.md"):
        w(g, X, "deny", p)
    w(g, B, "allow", studio + "/blender/orders/Wallet/coin-model/work/coin.py")
    w(g, B, "deny", order + "/work/coin.md")

    g = "tools: a feature orders for itself"
    w(g, W, "allow", studio + "/codex/orders/Wallet/new-icons/order.md")
    w(g, W, "allow", studio + "/blender/orders/Wallet/coin-model/order.md")
    w(g, W, "deny", order + "/feedback-1.md")      # no feedback files any more: studio.py review
    w(g, W, "deny", order + "/order.md")      # exists: written once
    e(g, W, "deny", studio + "/codex/orders/Wallet/new-icons/order.md")
    w(g, W, "deny", studio + "/codex/orders/Shop/x/order.md")
    w(g, W, "deny", order + "/work/coin.md")
    w(g, W, "deny", order + "/status.json")
    w(g, W, "deny", order + "/feedback-0.md")
    w(g, W, "deny", studio + "/codex/orders/Wallet/Bad_Id/order.md")
    w(g, W, "deny", studio + "/codex/lessons.md")
    w(g, W, "deny", studio + "/paint/orders/Wallet/x/order.md")
    w(g, W, "deny", res + "/Wallet/codex/coin-icons/v2/coin.png")
    w(g, "_Services/Popup", "allow", studio + "/codex/orders/_Services/Popup/pop-bg/order.md")
    w(g, "Shop", "deny", studio + "/codex/orders/Shop/x/order.md")      # its code gate is locked
    w(g, "@Manager", "deny", studio + "/codex/orders/Wallet/m/order.md")
    w(g, "@Core", "deny", studio + "/codex/orders/Core/m/order.md")

    g = "tools: @GameDesigner orders art for design nodes"
    art = studio + "/codex/orders/_design/feature-popup-lose.lose-actions.actions-spec/lose-art"
    D = "@GameDesigner"
    w(g, D, "allow", art + "/order.md")
    w(g, D, "deny", art + "/feedback-1.md")      # the human judges the pictures on the page
    for p_ in (art + "/approval.json", art + "/status.json", art + "/work/main.md", studio + "/codex/anchors.json",
               studio + "/codex/orders/Wallet/x/order.md", studio + "/blender/orders/_design/feature-a/x/order.md",
               studio + "/codex/orders/_design/Feature.A/x/order.md", studio + "/codex/lessons.md",
               res + "/_design/feature-a/codex/x/v1/main.png"):
        w(g, D, "deny", p_)
    os.makedirs(art, exist_ok=True)
    Path(art + "/order.md").write_text("x")
    w(g + " (written once)", D, "deny", art + "/order.md")
    w(g, W, "deny", art + "/feedback-2.md")
    w(g, X, "allow", art + "/work/main.md")
    for who in (X, W, "@human"):
        w(g + " (the page writes the judgement)", who, "deny" if who != "@human" else "pass", art + "/review.json")
    for cmd in (P + " codex _design/feature-popup-lose.lose-actions.actions-spec/lose-art main",
                P + " deliver codex _design/feature-popup-lose.lose-actions.actions-spec/lose-art"):
        t(g, X, "allow", "Bash", {"command": cmd})
        t(g, D, "deny", "Bash", {"command": cmd})
    for who in (D, X, W):
        t(g + " (status for all)", who, "allow", "Bash", {"command": P + " status _design/feature-a.a1/lose-art"})
        for cmd in (P + " approve _design/feature-a.a1/lose-art", P + " accept _design/feature-a.a1/lose-art",
                    P + " anchor _design/feature-a.a1/lose-art main", P + " take _design/feature-a.a1/lose-art",
                    P + " review _design/feature-a.a1/lose-art main 3",
                    P + ' review _design/feature-a.a1/lose-art main 0 "làm lại"'):
            t(g + " (the human judges and anchors, on the page)", who, "deny", "Bash", {"command": cmd})

    g = "tools: studio commands"
    for who in (W, X, B, "@Manager", "@GameDesigner", "@CodeLead"):
        for cmd in (P + " check", P + " status", P + " status codex", P + " status Wallet/coin-icons",
                    P + " measure Wallet/coin-icons", P + " measure Wallet/coin-icons coin",
                    P + " measure _design/feature-a.a1/lose-art main"):
            t(g, who, "allow", "Bash", {"command": cmd})
        for cmd in (P + " measure", P + " measure Wallet/coin-icons coin extra", P + " measure ../x coin",
                    P + " measure Wallet/coin-icons coin; rm x"):
            t(g + " (measure: an order and at most one item)", who, "deny", "Bash", {"command": cmd})
    for cmd in (P + " codex Wallet/coin-icons coin", P + " consult Wallet/coin-icons coin", P + " deliver codex Wallet/coin-icons"):
        t(g, X, "allow", "Bash", {"command": cmd})
        t(g, B, "deny", "Bash", {"command": cmd})
        t(g, W, "deny", "Bash", {"command": cmd})
    for cmd in (P + " blender Wallet/coin-model coin", P + " deliver blender Wallet/coin-model"):
        t(g, B, "allow", "Bash", {"command": cmd})
        t(g, X, "deny", "Bash", {"command": cmd})
    for cmd in (P + " review Wallet/coin-icons coin 3", P + ' review Wallet/coin-icons gem 0 "gem bị mờ ở 64px... làm nét hơn, viền 2px"',
                P + ' review Wallet/coin-icons gem 1 ""', P + ' review _Services/Popup/pop-bg bg 2 "tạm được"'):
        t(g, W if "Wallet" in cmd else "_Services/Popup", "allow", "Bash", {"command": cmd})
        t(g, "Shop", "deny", "Bash", {"command": cmd})
        t(g, X, "deny", "Bash", {"command": cmd})
        t(g, "@GameDesigner", "deny", "Bash", {"command": cmd})
    for cmd in (P + " review Wallet/coin-icons coin 4", P + " review Wallet/coin-icons coin", P + " review Wallet/coin-icons coin 0 mờ quá",
                P + ' review Wallet/coin-icons coin 0 "a" "b"', P + ' review Wallet/coin-icons coin 0 "x"; rm -rf y',
                P + ' review Wallet/coin-icons coin 0 "$(whoami)"', P + ' review Wallet/coin-icons coin 0 "a`b`"',
                P + ' review Wallet/coin-icons coin 0 "a | b"', P + ' review Wallet/../Shop/x coin 3',
                P + ' review Wallet/coin-icons coin 0 "a\\"', P + ' review Wallet/coin-icons Coin 3',
                P + " take Wallet/coin-icons", P + " reject Wallet/coin-icons"):
        t(g + " (review: refused forms)", W, "deny", "Bash", {"command": cmd})
    for cmd in (P + " codex Wallet/coin-icons coin; rm -rf x", P + " codex ../x coin", P + " codex Wallet/coin-icons",
                P + " review Wallet/coin-icons coin 3 --root /tmp", "codex exec -s danger-full-access x", "blender -b -P x.py"):
        t(g, X, "deny", "Bash", {"command": cmd})
        t(g, B, "deny", "Bash", {"command": cmd})

    g = "tools: limits and art debts"
    for who in (X, B):
        t(g, who, "allow", "Bash", {"command": P + " pick Wallet/coin-icons coin 2"})
    for cmd in (P + " pick Wallet/coin-icons coin 0", P + " pick Wallet/coin-icons coin x", P + " pick Wallet/coin-icons coin 1 2"):
        t(g, X, "deny", "Bash", {"command": cmd})
    for who in (W, X, B, "@GameDesigner", "@Manager"):
        t(g, who, "deny", "Bash", {"command": P + " pick Wallet/coin-icons coin 1"} if who not in (X, B) else
          {"command": P + " debt close n-1"})
        t(g, who, "deny", "Bash", {"command": P + " debt close n-1"})
        w(g, who, "deny", studio + "/debt.json")
        w(g, who, "deny", res + "/Wallet/codex/coin-icons/v1/quality.json")
    t(g, "@human", "pass", "Bash", {"command": P + " debt close n-1"})

    g = "tools: MCP"
    t(g, B, "allow", "mcp__blender__execute_blender_code", {"code": "import bpy"})
    t(g, B, "allow", "mcp__blender__get_scene_info", {})
    for who in (X, W, "@CodeLead", "@GameDesigner"):
        t(g, who, "deny", "mcp__blender__execute_blender_code", {"code": "import bpy"})
    t(g, "@human", "pass", "mcp__blender__execute_blender_code", {"code": "import bpy"})
    guard.release_lock(STORE)
    for who in (X, B):
        t(g, who, "allow", "mcp__unityMCP__read_console", {"action": "get"}, session="t-" + who)
        t(g, who, "deny", "mcp__unityMCP__manage_asset", {"action": "create", "path": "Assets/Features/Wallet/FeatureAssets/Private/x.png"},
          session="t-" + who)
        t(g, who, "deny", "mcp__unityMCP__manage_scene", {"action": "load", "path": "Assets/Features/Wallet/FeatureAssets/Private/s.unity"},
          session="t-" + who)
        t(g, who, "deny", "mcp__unityMCP__run_tests", {"mode": "EditMode"}, session="t-" + who)
        t(g, who, "allow", "mcp__unityMCP__execute_custom_tool", {"tool_name": "codex_exec"}, session="t-" + who)
        guard.release_lock(STORE)
    t(g, X, "allow", "mcp__unityMCP__execute_menu_item", {"menu_path": "Tools/Codex"}, session="t-x")
    t(g, B, "deny", "mcp__unityMCP__execute_custom_tool", {"tool_name": "blender_run"}, session="t-b")      # @Codex holds the Editor
    guard.release_lock(STORE)
    t(g, W, "deny", "mcp__unityMCP__execute_custom_tool", {"tool_name": "codex_exec"})

    g = "tools: @ClaudeImage draws SVG and runs only its own studio commands"
    CI = "@ClaudeImage"
    corder = studio + "/claudeimage/orders/Wallet/coin-icons"
    cart = studio + "/claudeimage/orders/_design/feature-a.cheat/a-claude"
    for p in (studio + "/claudeimage/lessons.md", corder + "/work/coin.svg", corder + "/work/coin.check.md",
              cart + "/work/main.svg"):
        w(g, CI, "allow", p)
    for p in (corder + "/order.md", corder + "/status.json", corder + "/review.json", corder + "/log.jsonl",
              studio + "/codex/lessons.md", order + "/work/coin.md", studio + "/blender/orders/Wallet/m/work/a.py",
              res + "/Wallet/claudeimage/coin-icons/v1/coin.png", F + "/Wallet/FeatureAssets/Private/x.png",
              F + "/Wallet/Scripts/Internal/Old.cs", FX + "/CLAUDE.md"):
        w(g, CI, "deny", p)
    w(g, X, "deny", corder + "/work/coin.svg")
    w(g, B, "deny", studio + "/claudeimage/lessons.md")
    r(g, CI, "allow", order + "/order.md")
    r(g, CI, "allow", res + "/Wallet/codex/coin-icons/v1/coin.png")
    for cmd in (P + " claude Wallet/coin-icons coin", P + " claude _design/feature-a.cheat/a-claude main",
                P + " deliver claudeimage Wallet/coin-icons", P + " pick Wallet/coin-icons coin 2", P + " status claudeimage",
                P + " measure Wallet/coin-icons coin", P + " check"):
        t(g, CI, "allow", "Bash", {"command": cmd})
    for cmd in (P + " codex Wallet/coin-icons coin", P + " consult Wallet/coin-icons coin", P + " blender Wallet/coin-model coin",
                P + " deliver codex Wallet/coin-icons", P + " deliver blender Wallet/coin-model",
                P + " review Wallet/coin-icons coin 3", P + " anchor _design/feature-a.cheat/a-claude main",
                P + " claude Wallet/coin-icons coin extra", P + " claude Wallet/coin-icons coin; rm x",
                "python Management/studio/claudeimage/orders/Wallet/coin-icons/work/x.py", "chrome --headless x.svg"):
        t(g, CI, "deny", "Bash", {"command": cmd})
    for who in (X, B, W, "@GameDesigner", "@CodeLead", "@Manager"):
        t(g + " (only @ClaudeImage)", who, "deny", "Bash", {"command": P + " claude Wallet/coin-icons coin"})
        t(g + " (only @ClaudeImage)", who, "deny", "Bash", {"command": P + " deliver claudeimage Wallet/coin-icons"})
        t(g + " (status for all)", who, "allow", "Bash", {"command": P + " status claudeimage"})
    w(g + " (a feature orders from it)", W, "allow", studio + "/claudeimage/orders/Wallet/new-claude/order.md")
    w(g + " (a feature orders from it)", W, "deny", studio + "/claudeimage/orders/Shop/x/order.md")
    w(g + " (a feature orders from it)", W, "deny", studio + "/claudeimage/orders/Wallet/new-claude/work/a.svg")
    w(g + " (@GameDesigner orders a claude concept)", "@GameDesigner", "allow", cart + "/order.md")
    for p in (cart + "/work/main.svg", cart + "/review.json", studio + "/claudeimage/orders/Wallet/x/order.md",
              studio + "/claudeimage/lessons.md", studio + "/claudeimage/orders/_design/Feature.A/x/order.md"):
        w(g + " (@GameDesigner orders a claude concept)", "@GameDesigner", "deny", p)
    t(g, CI, "deny", "mcp__blender__execute_blender_code", {"code": "import bpy"})
    guard.release_lock(STORE)
    t(g, CI, "allow", "mcp__unityMCP__read_console", {"action": "get"}, session="t-ci")
    for tool_, ti_ in (("mcp__unityMCP__execute_custom_tool", {"tool_name": "codex_exec"}),
                       ("mcp__unityMCP__execute_menu_item", {"menu_path": "Tools/Codex"}),
                       ("mcp__unityMCP__manage_asset", {"action": "create", "path": "Assets/Features/Wallet/FeatureAssets/Private/x.png"}),
                       ("mcp__unityMCP__run_tests", {"mode": "EditMode"})):
        t(g + " (no outside tool through unity-mcp)", CI, "deny", tool_, ti_, session="t-ci")
    guard.release_lock(STORE)
    for p in (studio + "/claudeimage/.run-1.lock", studio + "/claudeimage/.run-5.lock", corder + "/.coin.lock", cart + "/.main.lock"):
        w(g + " (the run locks are studio.py's)", CI, "deny", p)
    redo = P + ' redo _design/feature-a.a1.a-spec/a-claude main "nút chưa rõ, vẽ lại viền"'
    for who in ("@GameDesigner", "@CodeLead", "@Manager"):
        t(g + " (a Leader lets it draw again)", who, "allow", "Bash", {"command": redo})
    for who in (CI, X, B, W, "@Core"):
        t(g + " (drawing again is not the tool chat's decision)", who, "deny", "Bash", {"command": redo})
    for cmd in (P + " redo _design/feature-a.a1.a-spec/a-claude main", P + ' redo Wallet/coin-icons coin "x"',
                P + ' redo _design/feature-a.a1.a-spec/a-claude main "x"; rm y', P + ' redo _design/feature-a.a1.a-spec/a-claude main ""',
                P + ' redo _design/feature-a.a1.a-spec/a-claude main "a" "b"'):
        t(g + " (redo: a design order, a frame, a reason)", "@GameDesigner", "deny", "Bash", {"command": cmd})
    check(g, CI, True, "the role is known and names its studio folder",
          guard.role_of(CI) == CI and guard.TOOL_ROLES[CI] == "claudeimage")

    g = "tools: no report needed"
    os.makedirs(STORE + "/sessions", exist_ok=True)
    for who, sid in ((X, "rep-codex"), (B, "rep-blender"), (CI, "rep-claudeimage")):
        Path(guard.binding_path(sid, STORE)).write_text(json.dumps({"feature": who}))
        tr = FX + f"/{sid}.jsonl"
        entries = [{"type": "user", "message": {"content": "làm order"}},
                   {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": "Write",
                                                                  "input": {"file_path": order + "/work/a.md"}}]}},
                   {"type": "assistant", "message": {"content": [{"type": "text", "text": "Xong."}]}}]
        Path(tr).write_text("\n".join(json.dumps(x) for x in entries))
        p = subprocess.run([sys.executable, "-I", str(HERE / "stop_report.py"), "--root", FX, "--store", STORE],
                           input=json.dumps({"session_id": sid, "transcript_path": tr}).encode(), capture_output=True)
        check(g, who, True, "a tool chat stops without a report block", p.returncode == 0 and b'"block"' not in p.stdout)


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
    u(g, W, "allow", "read_console", action="get", types=["error"])
    u(g, W, "allow", "read_console")
    u(g, W, "allow", "find_gameobjects", search_term="Coin")
    u(g, W, "allow", "manage_scene", action="get_hierarchy")
    u(g, W, "allow", "manage_asset", action="get_info", path=own + "/c.asset")
    u(g, W, "allow", "manage_asset", action="search", path="Assets/Features/Wallet", search_pattern="*.prefab")
    u(g, W, "allow", "manage_asset", action="get_info", path="Assets/Features/Shop/FeatureAssets/Public/x.prefab")
    u(g, W, "deny", "manage_asset", action="get_info", path="Assets/Features/Shop/FeatureAssets/Private/s.asset")
    u(g, W, "deny", "manage_asset", action="search", search_pattern="*.asset")      # no scope: the whole project
    u(g, W, "deny", "manage_asset", action="search", path="Assets")
    u(g, W, "deny", "manage_asset", action="get_info", path="Assets/../../Management/tools/rules.json")
    u(g, W, "deny", "manage_asset", action="get_info", path="Assets/Features/Wallet/link/S.cs")
    u(g, W, "allow", "find_in_file", uri="mcpforunity://path/Assets/Features/Wallet/Scripts/Internal/Old.cs", pattern="x")
    u(g, W, "deny", "find_in_file", uri="mcpforunity://path/Assets/Features/Shop/Scripts/Internal/S.cs", pattern="x")
    u(g, W, "deny", "find_in_file", uri="mcpforunity://path/Library/x", pattern="x")
    u(g, W, "allow", "get_sha", uri="mcpforunity://path/Assets/Core/Old.cs")
    u(g, W, "allow", "manage_prefabs", action="get_hierarchy", prefab_path="Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    u(g, W, "deny", "manage_prefabs", action="get_info")
    u(g, W, "allow", "unity_docs", query="Rigidbody")
    u(g, W, "deny", "manage_asset", action="frobnicate", path=own + "/c.asset")
    u(g, W, "deny", "no_such_tool")

    g = "unity: write (own assets)"
    u(g, W, "allow", "manage_asset", action="create", path=own + "/Config.asset", asset_type="ScriptableObject")
    u(g, W, "allow", "manage_asset", action="create_folder", path=own + "/Prefabs")
    u(g, W, "allow", "manage_prefabs", action="modify_contents", prefab_path="Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    u(g, W, "allow", "manage_material", action="create", material_path=own + "/Coin.mat")
    u(g, W, "allow", "manage_texture", action="create", path=own + "/Coin.png")
    u(g, W, "allow", "manage_ui", action="create", path=own + "/Hud.uxml", contents="<ui:UXML/>")
    u(g, W, "allow", "manage_asset", action="move", path=own + "/a.asset", destination=own + "/Prefabs/a.asset")
    u(g, W, "deny", "manage_asset", action="move", path=own + "/a.asset", destination="Assets/Features/Shop/FeatureAssets/Private/a.asset")
    u(g, W, "deny", "manage_asset", action="create", path="Assets/Features/Shop/FeatureAssets/Private/n.asset")
    u(g, W, "deny", "manage_asset", action="create", path="Assets/Core/n.asset")
    SO = "manage_scriptable_object"      # config and installer assets: the owner, inside its own area
    u(g, W, "allow", SO, action="create", type_name="Game.Wallet.WalletConfig", folder_path=own, asset_name="WalletConfig")
    u(g, W, "allow", SO, action="create", type_name="Game.Wallet.WalletInstaller", asset_name="WalletInstaller",
      folder_path="Assets/Features/Wallet/FeatureAssets/Public/")
    u(g, W, "allow", SO, action="modify", target={"path": own + "/c.asset"}, patches=[])
    u(g, W, "deny", SO, action="create", type_name="X", folder_path="Assets/Features/Shop/FeatureAssets/Private", asset_name="n")
    u(g, W, "deny", SO, action="create", type_name="X", folder_path="Assets/Core", asset_name="n")
    u(g, W, "deny", SO, action="create", type_name="X", folder_path=own, asset_name="../../Shop/FeatureAssets/Private/n")
    u(g, W, "deny", SO, action="create", type_name="X", folder_path=own + "/../../../Shop/FeatureAssets/Private", asset_name="n")
    u(g, W, "deny", SO, action="create", type_name="X", folder_path=own)                       # no name
    u(g, W, "deny", SO, action="create", type_name="X", asset_name="n")                        # no folder
    u(g, W, "deny", SO, action="modify", target={"path": "Assets/Features/Shop/FeatureAssets/Private/s.asset"})
    u(g, W, "deny", SO, action="modify", target={"guid": "0123456789abcdef0123456789abcdef"})      # cannot be checked
    u(g, W, "deny", SO, action="modify", target={"path": own + "/c.asset", "guid": "0123456789abcdef0123456789abcdef"})
    u(g, W, "deny", SO, action="modify", target=own + "/c.asset")
    u(g, W, "deny", SO, action="delete", target={"path": own + "/c.asset"})
    for who in ("@Manager", "@GameDesigner", "@Codex", ""):
        u(g, who, "deny", SO, action="create", type_name="X", folder_path=own, asset_name="n")
    u(g, "@CodeLead", "deny", SO, action="create", type_name="X", folder_path=own, asset_name="n")      # not its area
    u(g, "@CodeLead", "allow", SO, action="create", type_name="X", folder_path="Assets/_Composition", asset_name="Catalog")
    u(g, W, "deny", "manage_asset", action="create", path="Assets/Features/Wallet/Scripts/Contracts/n.asset")      # skeleton
    u(g, W, "deny", "manage_asset", action="create", path="Assets/Features/Wallet/Scripts/Internal/N.cs")
    u(g, W, "deny", "manage_asset", action="create", path="Assets/Features/Wallet/Scripts/Internal/W.asmdef")
    u(g, W, "deny", "manage_asset", action="rename", path=own + "/a.asset", destination=own + "/A.cs")
    u(g, W, "deny", "manage_asset", action="create")
    u(g, S, "deny", "manage_asset", action="create", path="Assets/Features/Shop/FeatureAssets/Private/n.asset")      # code gate
    u(g, C, "allow", "manage_asset", action="create", path="Assets/_Composition/Catalog.asset")
    u(g, C, "deny", "manage_asset", action="create", path=own + "/x.asset")
    u(g, W, "allow", "manage_asset", action="delete", path="Assets/Features/Wallet/Scripts/Internal/Old.cs")      # unchanged
    u(g, W, "deny", "manage_asset", action="delete", path="Assets/Features/Wallet/Scripts/Contracts/IWallet.cs")
    u(g, L, "deny", "manage_asset", action="create", path=own + "/x.asset")
    for name in ("create_script", "script_apply_edits", "apply_text_edits", "delete_script", "manage_script"):
        u(g + " (code only through Edit/Write)", W, "deny", name, uri="mcpforunity://path/Assets/Features/Wallet/Scripts/Internal/Old.cs")
        u(g + " (code only through Edit/Write)", L, "deny", name, uri="mcpforunity://path/Assets/Features/Wallet/Scripts/Internal/Old.cs")
    t(g, "@human", "pass", "mcp__unityMCP__create_script", {"path": "Assets/x.cs"})

    g = "unity: play and lead are @CodeLead's"
    for name, ti in (("run_tests", {"mode": "EditMode"}), ("manage_editor", {"action": "play"}),
                     ("manage_editor", {"action": "add_tag", "tag_name": "x"}), ("execute_menu_item", {"menu_path": "File/Save"}),
                     ("manage_packages", {"action": "add"}), ("manage_camera", {"action": "screenshot"})):
        u(g, W, "deny", name, **ti)
        u(g, "@Core", "deny", name, **ti)
    for who in ("@Manager", "@GameDesigner", ""):
        u(g + " (leaders without code, unset)", who, "deny", "read_console", action="get")
        u(g + " (leaders without code, unset)", who, "deny", "refresh_unity")

    g = "unity: the Editor lock"
    guard.release_lock(STORE)
    u(g, W, "allow", "refresh_unity", session="a")
    u(g, W, "deny", "manage_gameobject", session="a", action="create", name="X")      # no scene of its own open
    u(g, W, "allow", "manage_scene", session="a", action="create", path=own + "/Scenes", name="WalletTest")
    lock = guard.read_lock(STORE)
    check(g, W, True, "the lock names the chat, its role and the scene it opened",
          (lock["session"], lock["who"], lock["scene"]) == ("a", W, own + "/Scenes/WalletTest.unity"))
    u(g, W, "allow", "manage_gameobject", session="a", action="create", name="Coin")
    u(g, W, "allow", "manage_scene", session="a", action="save")
    u(g, C, "deny", "manage_scene", session="b", action="load", path="Assets/_Composition/Scenes/Game.unity")
    u(g, C, "deny", "refresh_unity", session="b")      # compiling waits too
    u(g, C, "allow", "manage_asset", session="b", action="create", path="Assets/_Composition/Catalog.asset")      # assets do not
    u(g, C, "allow", "read_console", session="b", action="get")
    u(g, L, "deny", "manage_editor", session="c", action="play")
    u(g, W, "deny", "manage_scene", session="a", action="load", path="Assets/_Composition/Scenes/Game.unity")      # not its scene
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
    u(g, W, "deny", "read_console", session="a", action="clear")      # clearing the console needs the lock too
    p = subprocess.run([sys.executable, "-I", str(HERE / "stop_unlock.py"), "--release", "--store", STORE], capture_output=True)
    check(g, "", True, "the human frees any lock", p.returncode == 0 and guard.read_lock(STORE) is None)
    Path(guard.lock_path(STORE)).write_text("{ broken")
    u(g, W, "allow", "read_console", session="a", action="clear")      # a broken lock file is no lock

    g = "unity: batch_execute"
    guard.release_lock(STORE)
    B = "batch_execute"
    u(g, W, "allow", B, commands=[{"tool": "read_console", "params": {"action": "get"}},
                                  {"tool": "manage_asset", "params": {"action": "get_info", "path": own + "/c.asset"}}])
    check(g, W, True, "a batch of reads takes no lock", guard.read_lock(STORE) is None)
    u(g, W, "deny", B, commands=[{"tool": "read_console", "params": {"action": "get"}},
                                 {"tool": "manage_asset", "params": {"action": "create", "path": "Assets/Core/x.asset"}}])
    u(g, W, "allow", B, commands=[{"tool": "manage_scene", "params": {"action": "load", "path": own + "/Scenes/W.unity"}},
                                  {"tool": "mcp__unityMCP__manage_gameobject", "params": {"action": "create", "name": "Coin"}},
                                  {"tool": "manage_scene", "params": {"action": "save"}}])
    check(g, W, True, "a batch that opens a scene takes the lock", (guard.read_lock(STORE) or {}).get("session") == "a")
    u(g, C, "deny", B, session="b", commands=[{"tool": "refresh_unity", "params": {}}])
    u(g, W, "deny", B, commands=[{"tool": "batch_execute", "params": {"commands": []}}])
    u(g, W, "deny", B, commands=[])
    u(g, W, "deny", B, commands=[{"tool": "read_console", "params": {"action": "get"}}] * 26)
    u(g, W, "deny", B, commands="x")
    u(g, W, "deny", B, commands=[{"tool": "create_script", "params": {"path": own + "/a.cs"}}])
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


def main():
    try:
        build_fixture()
        run_cases()
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
