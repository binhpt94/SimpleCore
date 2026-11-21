#!/usr/bin/env python3
"""Tests for guard.py.

    python .claude/hooks/test_guard.py

Feeds fake hook input to guard.py and compares the decision. Works on a throw-away copy of the
folder layout in a temp folder; the real project is not touched.
"""
import _winapi
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
import guard  # noqa: E402

FX = tempfile.mkdtemp(prefix="guardfx-")
A = FX + r"\UnityProject\Assets"
F = A + r"\Features"
VALIDATE = "python Management/tools/validate.py"
STORE = FX + r"\guard-store"      # stands in for guard.STORE_DIR; the real one is never touched
PIN = "tr0ng-test"

results = []


def check(group, feature, expect, label, got):
    results.append((got == expect, group, feature or "(unset)", expect, got, label.replace(FX, "")))


def t(group, feature, expect, tool, ti, cwd=None):
    """In-process: calls guard.decide exactly as main() does, including fail-closed on errors."""
    data = {"tool_name": tool, "tool_input": ti, "cwd": cwd or FX}
    try:
        result = guard.decide(data, feature, FX)
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
        os.makedirs(STORE + r"\sessions", exist_ok=True)
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
    for d in (r"\.claude\hooks", r"\Management\data", r"\Management\proposals", r"\Management\tools"):
        os.makedirs(FX + d)
    for d in (r"\_Composition", r"\Core", r"\Features\Wallet\Scripts\Internal", r"\Features\Wallet\Scripts\Contracts",
              r"\Features\Shop\Scripts\Internal", r"\Features\_Services\Popup\Scripts\Internal"):
        os.makedirs(A + d)
    Path(FX + r"\Management\proposals\old.md").write_text("x")
    Path(F + r"\Shop\Scripts\Internal\S.cs").write_text("x")
    os.makedirs(F + r"\Wallet\FeatureAssets\Public")
    for f in (r"\Scripts\Internal\Old.cs", r"\Scripts\Internal\AssemblyInfo.cs", r"\Scripts\Contracts\IWallet.cs",
              r"\Scripts\Internal\Game.Wallet.Internal.asmdef", r"\FeatureAssets\Public\Icon.prefab"):
        Path(F + r"\Wallet" + f).write_text("x")
    _winapi.CreateJunction(F + r"\Shop\Scripts\Internal", F + r"\Wallet\link")


def run_cases():
    W = "Wallet"

    # ---- one group per row of the permission table
    g = "1 own feature"
    r(g, W, "allow", F + r"\Wallet\Scripts\Internal\W.cs")
    e(g, W, "allow", F + r"\Wallet\Scripts\Internal\W.cs")
    w(g, W, "allow", F + r"\Wallet\Scripts\View\V.cs")
    w(g, W, "allow", F + r"\Wallet\FeatureAssets\Private\cfg.asset")
    w(g, W, "allow", F + r"\Wallet\Tests\T.cs")
    w(g, W, "allow", F + r"\Wallet\CLAUDE.md")

    g = "2 own Contracts"
    r(g, W, "allow", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
    e(g, W, "ask", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
    w(g, W, "ask", F + r"\Wallet\Scripts\Contracts\New.cs")

    g = "3 other public"
    r(g, W, "allow", F + r"\Shop\Scripts\Contracts\IShop.cs")
    e(g, W, "deny", F + r"\Shop\Scripts\Contracts\IShop.cs")
    r(g, W, "allow", F + r"\Shop\FeatureAssets\Public\icon.png")
    w(g, W, "deny", F + r"\Shop\FeatureAssets\Public\icon.png")
    r(g, W, "allow", F + r"\_Services\Popup\Scripts\Contracts\IPopup.cs")
    e(g, W, "deny", F + r"\_Services\Popup\Scripts\Contracts\IPopup.cs")

    g = "4 other private"
    r(g, W, "deny", F + r"\Shop\Scripts\Internal\S.cs")
    e(g, W, "deny", F + r"\Shop\Scripts\Internal\S.cs")
    r(g, W, "deny", F + r"\Shop\Scripts\View\V.cs")
    w(g, W, "deny", F + r"\Shop\Scripts\View\V.cs")
    r(g, W, "deny", F + r"\Shop\FeatureAssets\Private\cfg.asset")
    w(g, W, "deny", F + r"\Shop\FeatureAssets\Private\cfg.asset")
    r(g, W, "deny", F + r"\_Services\Popup\Scripts\Internal\P.cs")
    e(g, W, "deny", F + r"\_Services\Popup\Scripts\Internal\P.cs")

    g = "5 _Composition/Core"
    r(g, W, "allow", A + r"\_Composition\Boot.cs")
    e(g, W, "ask", A + r"\_Composition\Boot.cs")
    r(g, W, "allow", A + r"\Core\Base.cs")
    w(g, W, "ask", A + r"\Core\Base.cs")

    g = "6 Packages/ProjectSettings"
    r(g, W, "allow", FX + r"\UnityProject\Packages\manifest.json")
    e(g, W, "deny", FX + r"\UnityProject\Packages\manifest.json")
    r(g, W, "allow", FX + r"\UnityProject\ProjectSettings\ProjectVersion.txt")
    w(g, W, "deny", FX + r"\UnityProject\ProjectSettings\ProjectVersion.txt")

    g = "7 proposals"
    r(g, W, "allow", FX + r"\Management\proposals\old.md")
    w(g, W, "allow", FX + r"\Management\proposals\new.md")
    w(g, W, "deny", FX + r"\Management\proposals\old.md")
    e(g, W, "deny", FX + r"\Management\proposals\old.md")

    g = "8 Management"
    r(g, W, "allow", FX + r"\Management\data\idea.js")
    e(g, W, "deny", FX + r"\Management\data\idea.js")
    w(g, W, "deny", FX + r"\Management\data\new.js")
    r(g, W, "allow", FX + r"\Management\tools\validate.py")
    w(g, W, "deny", FX + r"\Management\tools\validate.py")
    r(g, W, "allow", FX + r"\Management\tools\rules.json")
    e(g, W, "ask", FX + r"\Management\tools\rules.json")
    e(g, "", "deny", FX + r"\Management\tools\rules.json")
    e(g, W, "deny", FX + r"\Management\tools\sub\rules.json")

    g = "other feature's CLAUDE.md"
    r(g, W, "allow", F + r"\Shop\CLAUDE.md")
    r(g, W, "allow", F + r"\Shop\claude.md")
    r(g, W, "allow", F + r"\_Services\Popup\CLAUDE.md")
    r(g, "", "allow", F + r"\Shop\CLAUDE.md")
    e(g, W, "deny", F + r"\Shop\CLAUDE.md")
    w(g, W, "deny", F + r"\_Services\Popup\CLAUDE.md")
    r(g, W, "deny", F + r"\Shop\Scripts\Internal\CLAUDE.md")
    r(g, W, "deny", F + r"\Shop\NOTES.md")
    t(g, W, "deny", "mcp__unityMCP__manage_asset", {"action": "delete", "path": "Assets/Features/Shop/CLAUDE.md"})
    r(g, W, "allow", FX + r"\Management\index.html")
    e(g, W, "deny", FX + r"\Management\index.html")

    g = "9 .claude"
    r(g, W, "allow", FX + r"\.claude\settings.json")
    e(g, W, "deny", FX + r"\.claude\settings.json")
    w(g, W, "deny", FX + r"\.claude\hooks\guard.py")
    w(g, W, "deny", FX + r"\.claude\settings.local.json")

    g = "10 Library etc"
    for d in ("Library", "Temp", "Logs", "obj", "UserSettings"):
        r(g, W, "deny", FX + rf"\UnityProject\{d}\x.txt")
        w(g, W, "deny", FX + rf"\UnityProject\{d}\x.txt")

    # ---- FEATURE variants
    g, P = "service agent", "_Services/Popup"
    e(g, P, "allow", F + r"\_Services\Popup\Scripts\Internal\P.cs")
    e(g, P, "ask", F + r"\_Services\Popup\Scripts\Contracts\IPopup.cs")
    r(g, P, "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    e(g, P, "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    r(g, P, "allow", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
    e(g, r"_Services\Popup", "allow", F + r"\_Services\Popup\Scripts\Internal\P.cs")

    g = "FEATURE unset"
    e(g, "", "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    w(g, "", "deny", FX + r"\Management\proposals\new.md")
    e(g, "", "deny", A + r"\Core\Base.cs")
    r(g, "", "allow", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
    r(g, "", "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    t(g, "", "deny", "Bash", {"command": "ls"})

    g = "FEATURE invalid"
    e(g, "Nope", "deny", F + r"\Nope\Scripts\Internal\N.cs")
    e(g, "../Shop", "deny", F + r"\Shop\Scripts\Internal\S.cs")
    e(g, "_Services", "deny", F + r"\_Services\Popup\Scripts\Internal\P.cs")
    e(g, "Wallet/Scripts", "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    e(g, "*", "deny", F + r"\Wallet\Scripts\Internal\W.cs")
    e(g, "Wallet\n", "allow", F + r"\Wallet\Scripts\Internal\W.cs")      # surrounding whitespace is trimmed
    e(g, "Wallet x", "deny", F + r"\Wallet\Scripts\Internal\W.cs")

    # ---- unlisted zones
    g = "unlisted"
    r(g, W, "deny", F + r"\Shop\Tests\T.cs")
    r(g, W, "allow", FX + r"\CLAUDE.md")
    e(g, W, "deny", FX + r"\CLAUDE.md")
    w(g, W, "deny", FX + r"\notes.md")
    w(g, W, "deny", A + r"\Shared\Util.cs")
    w(g, W, "deny", F + r"\NewFeature\Scripts\Internal\N.cs")
    r(g, W, "deny", r"C:\Windows\win.ini")
    w(g, W, "deny", os.environ["TEMP"] + r"\guard-out.txt")
    r(g, W, "deny", FX)
    r(g, W, "deny", FX + "2\\CLAUDE.md")     # sibling folder that shares the root's prefix

    # ---- attempts to get around the guard
    g = "evasion"
    e(g, W, "deny", F + r"\Wallet\..\Shop\Scripts\Internal\S.cs")
    r(g, W, "deny", F + r"\Wallet\..\Shop\Scripts\Internal\S.cs")
    r(g, W, "deny", (F + r"\Shop\Scripts\Internal\S.cs").lower())
    e(g, W, "allow", F + r"\WALLET\scripts\internal\W.cs")
    e(g, W, "ask", F + r"\wallet\SCRIPTS\CONTRACTS\I.cs")
    e(g, W, "deny", (F + r"\Shop\Scripts\Internal\S.cs").replace("\\", "/"))
    e(g, W, "allow", (F + r"\Wallet\Scripts\Internal\W.cs").replace("\\", "/"))
    t(g, W, "allow", "Edit", {"file_path": r"Scripts\Internal\W.cs"}, F + r"\Wallet")
    t(g, W, "deny", "Edit", {"file_path": r"..\Shop\Scripts\Internal\S.cs"}, F + r"\Wallet")
    msys = "/" + FX[0].lower() + FX[2:].replace("\\", "/")
    e(g, W, "deny", msys + "/UnityProject/Assets/Features/Shop/Scripts/Internal/S.cs")
    e(g, W, "allow", msys + "/UnityProject/Assets/Features/Wallet/Scripts/Internal/W.cs")
    r(g, W, "deny", F + r"\Shop\Scripts\Internal\S.cs:stream")
    r(g, W, "deny", F + r"\Wallet\Scripts\Internal\W.cs::$DATA")
    r(g, W, "deny", F + "\\Shop\\Scripts\\Internal.\\S.cs")
    r(g, W, "deny", F + "\\Shop\\Scripts\\Internal \\S.cs")
    r(g, W, "deny", "\\\\localhost\\" + FX[0] + "$" + FX[2:] + r"\UnityProject\Assets\Features\Shop\Scripts\Internal\S.cs")
    r(g, W, "deny", "\\\\?\\" + F + r"\Shop\Scripts\Internal\S.cs")
    r(g, W, "deny", FX[:2] + F[3:] + r"\Shop\Scripts\Internal\S.cs")     # drive-relative: D:Users\...
    r(g, W, "deny", F[2:] + r"\Shop\Scripts\Internal\S.cs")              # root-relative: \Users\...
    r(g, W, "deny", F + r"\Wallet\link\S.cs")
    w(g, W, "deny", F + r"\Wallet\link\New.cs")
    t(g, W, "deny", "NotebookEdit", {"notebook_path": F + r"\Shop\Scripts\Internal\n.ipynb"})
    t(g, W, "deny", "MultiEdit", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"})
    t(g, W, "deny", "Write", {"file_path": ["not", "a", "string"]})
    t(g, W, "deny", "Write", {})

    # ---- search tools
    g = "search"
    t(g, W, "deny", "Grep", {"pattern": "class", "path": FX})
    t(g, W, "deny", "Grep", {"pattern": "class"})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F + r"\Shop"})
    t(g, W, "allow", "Grep", {"pattern": "class", "path": F + r"\Wallet"})
    t(g, W, "allow", "Grep", {"pattern": r"a\.\.b", "path": F + r"\Wallet"})
    t(g, W, "allow", "Grep", {"pattern": "class", "path": F + r"\Shop\Scripts\Contracts"})
    t(g, W, "deny", "Grep", {"pattern": "class", "path": F + r"\Wallet", "glob": "../Shop/**"})
    t(g, W, "allow", "Glob", {"pattern": "**/*.cs", "path": F + r"\Wallet"})
    t(g, W, "allow", "Glob", {"pattern": "**/*.cs"}, F + r"\Wallet")
    t(g, W, "deny", "Glob", {"pattern": "**/*.cs"})
    t(g, W, "deny", "Glob", {"pattern": "../Shop/**/*.cs", "path": F + r"\Wallet"})
    t(g, W, "deny", "Glob", {"pattern": (F + r"\Shop\**\*.cs").replace("\\", "/"), "path": F + r"\Wallet"})
    t(g, W, "deny", "Glob", {"pattern": "*", "path": FX + r"\UnityProject\Library"})
    t(g, W, "allow", "Glob", {"pattern": "*", "path": A + r"\Core"})

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
    t(g, W, "deny", "Artifact", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"})
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
    d(W, "deny", FX + r"\UnityProject\Assets\Features\Wallet\Scripts\Internal\Old.cs")
    d(W, "deny", "Assets/Features/Shop/Scripts/Internal/S.cs")
    d(W, "deny", "Assets/Features/Wallet/../Shop/Scripts/Internal/S.cs")
    d(W, "deny", "Assets/Features/Wallet/link/S.cs")
    d(W, "deny", "Assets/../../Management/tools/rules.json")
    d(W, "deny", "Assets/Features/Wallet/Scripts/Internal/*.cs")
    d(W, "ask", "Assets/Core/Base.cs")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Contracts/IWallet.cs")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal")
    d(W, "ask", "Assets/Features/Wallet")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal/Missing.cs")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal/AssemblyInfo.cs")
    d(W, "ask", "Assets/Features/Wallet/Scripts/Internal/Game.Wallet.Internal.asmdef")
    d(W, "ask", "Assets/Features/Wallet/FeatureAssets/Public/Icon.prefab")
    d("", "deny", "Assets/Features/Wallet/Scripts/Internal/Old.cs")
    d("@human", "pass", "Assets/Features/Shop/Scripts/Internal/S.cs")
    t(g, W, "deny", "mcp__unityMCP__manage_script", {"action": "delete", "name": "Old", "path": "Assets/Features/Wallet/"})
    t(g, W, "deny", "mcp__unityMCP__delete_script", {"uri": "Assets/Features/Wallet/Scripts/Internal/Old.cs"})
    t(g, W, "deny", "mcp__unityMCP__execute_menu_item", {"menu_path": "Assets/Delete"})

    # ---- boundary files inside the agent's own feature
    g = "boundary files"
    w(g, W, "ask", F + r"\Wallet\Scripts\Internal\Game.Wallet.Internal.asmdef")
    e(g, W, "ask", F + r"\Wallet\Tests\Game.Wallet.Tests.asmdef")
    w(g, W, "deny", F + r"\Wallet\Scripts\Internal\x.asmref")
    w(g, W, "deny", F + r"\Wallet\Scripts\Internal\X.ASMREF")
    e(g, W, "ask", F + r"\Wallet\Scripts\Internal\AssemblyInfo.cs")
    other = F + r"\Wallet\Scripts\Internal\Other.cs"
    t(g, W, "ask", "Write", {"file_path": other, "content": '[assembly: InternalsVisibleTo("Game.Shop.Internal")]'})
    t(g, W, "ask", "Edit", {"file_path": other, "old_string": "a", "new_string": 'InternalsVisibleTo("X")'})
    t(g, W, "ask", "MultiEdit", {"file_path": other, "edits": [{"old_string": "a", "new_string": "InternalsVisibleTo"}]})
    t(g, W, "allow", "Write", {"file_path": other, "content": "internal class Other {}"})
    w(g, W, "deny", F + r"\Shop\Scripts\Internal\Game.Shop.Internal.asmdef")

    # ---- human session
    g, H = "human", "@human"
    e(g, H, "pass", F + r"\Wallet\Scripts\Internal\W.cs")
    e(g, H, "pass", F + r"\Shop\Scripts\Internal\S.cs")
    w(g, H, "pass", FX + r"\Management\data\idea.js")
    e(g, H, "ask", A + r"\Core\Base.cs")
    e(g, H, "ask", A + r"\_Composition\Boot.cs")
    e(g, H, "ask", FX + r"\.claude\hooks\guard.py")
    t(g, H, "pass", "Bash", {"command": "ls"})
    e("human case-sensitive", "@Human", "deny", F + r"\Wallet\Scripts\Internal\W.cs")

    # ---- the real process: wire format, binding, fail-closed
    g = "process"
    payload = lambda tool, ti: json.dumps({"tool_name": tool, "tool_input": ti, "cwd": FX})  # noqa: E731
    cli(g, W, "allow", "Edit own feature", payload("Edit", {"file_path": F + r"\Wallet\Scripts\Internal\W.cs"}))
    cli(g, W, "deny", "Edit other feature", payload("Edit", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"}))
    cli(g, W, "ask", "Edit Core", payload("Edit", {"file_path": A + r"\Core\Base.cs"}))
    cli(g, W, "pass", "TodoWrite", payload("TodoWrite", {}))
    cli(g, "", "deny", "Edit, FEATURE unset", payload("Edit", {"file_path": F + r"\Wallet\Scripts\Internal\W.cs"}))
    cli(g, "@human", "pass", "Edit, @human", payload("Edit", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"}))
    cli(g, "@human", "ask", "Edit .claude, @human", payload("Edit", {"file_path": FX + r"\.claude\settings.json"}))
    cli(g, W, "deny", "malformed JSON", "{ not json")
    cli(g, W, "deny", "empty input", " ")
    cli(g, W, "deny", "JSON array instead of object", "[1,2]")
    cli(g, W, "deny", "no tool_name", "{}")
    cli(g, W, "allow", "UTF-8 path", payload("Write", {"file_path": F + "\\Wallet\\Scripts\\Internal\\Ví.cs"}))
    cli(g, "", "deny", "Edit, unbound, FEATURE=@human in the environment is ignored",
        payload("Edit", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"}), env_feature="@human")
    cli(g, "", "deny", "Edit, unbound, FEATURE=Wallet in the environment is ignored",
        payload("Edit", {"file_path": F + r"\Wallet\Scripts\Internal\W.cs"}), env_feature=W)
    cli(g, W, "deny", "Edit, bound to Wallet, FEATURE=@human in the environment is ignored",
        payload("Edit", {"file_path": F + r"\Shop\Scripts\Internal\S.cs"}), env_feature="@human")
    run_binding_cases(payload)
    run_role_cases(payload)
    run_report_cases()
    real = str(HERE.parent.parent)
    cli("real root", "", "deny", "Write <real root>\\x.txt, no --root",
        json.dumps({"tool_name": "Write", "tool_input": {"file_path": real + r"\x.txt"}, "cwd": real}), root=None)
    cli("real root", "", "allow", "Read <real root>\\CLAUDE.md, no --root",
        json.dumps({"tool_name": "Read", "tool_input": {"file_path": real + r"\CLAUDE.md"}, "cwd": real}), root=None)


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
                names = f"FEATURE={bound}" in ctx if bound else "chưa gán" in ctx
                got = "pass" if names and PIN not in ctx else "garbage: " + ctx
        except Exception:
            got = "garbage"
    check(group, "", expect, label, f"{got}, bound={bound}")


def run_binding_cases(payload):
    import bind_session
    W, S = "Wallet", F + r"\Shop\Scripts\Internal\S.cs"
    own = F + r"\Wallet\Scripts\Internal\W.cs"
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
    b(g, "allow", "chat2", "Edit", {"file_path": F + r"\_Services\Popup\Scripts\Internal\P.cs"})
    b(g, "deny", "chat2", "Edit", {"file_path": own})
    b(g, "pass", "chat3", "Edit", {"file_path": S})
    b(g, "ask", "chat3", "Edit", {"file_path": FX + r"\.claude\hooks\guard.py"})
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
    say(g, "block, bound=None", "no PIN was ever set", f"FEATURE=Wallet {PIN}", store=FX + r"\empty-store")
    for expect, label, stdin in ((2, "broken input that may carry the PIN is blocked", '{"prompt": "FEATURE=Wallet ' + PIN),
                                 (0, "broken input of an ordinary prompt is let through", '{"prompt": "xin chào')):
        args = [sys.executable, "-I", str(HERE / "bind_session.py"), "--root", FX, "--store", STORE]
        p = subprocess.run(args, input=stdin.encode("utf-8"), capture_output=True)
        check(g, "", f"exit{expect}, no output", label, f"exit{p.returncode}, {'output' if p.stdout.strip() else 'no output'}")


def run_role_cases(payload):
    """@Core, @Composition: like a feature session whose feature is Core / _Composition. @Manager: edits nothing."""
    Path(A + r"\Core\Old.cs").write_text("x")
    Path(A + r"\_Composition\Old.cs").write_text("x")
    own_w = F + r"\Wallet\Scripts\Internal\Old.cs"
    delete = lambda p: {"action": "delete", "path": p}       # noqa: E731
    sh = lambda c: {"command": c}                            # noqa: E731

    for role, own, other, owner in (("@Core", r"\Core", r"\_Composition", "Core"),
                                    ("@Composition", r"\_Composition", r"\Core", "_Composition")):
        g = "role " + role
        e(g, role, "allow", A + own + r"\Old.cs")
        w(g, role, "allow", A + own + r"\New.cs")
        w(g, role, "allow", A + own + r"\Tests\T.cs")
        e(g, role, "ask", A + own + r"\Game.Thing.asmdef")
        w(g, role, "ask", A + own + r"\AssemblyInfo.cs")
        w(g, role, "deny", A + own + r"\x.asmref")
        e(g, role, "deny", A + other + r"\Old.cs")
        e(g, role, "deny", own_w)
        r(g, role, "deny", own_w)
        r(g, role, "allow", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
        r(g, role, "allow", F + r"\Wallet\CLAUDE.md")
        r(g, role, "allow", A + other + r"\Old.cs")
        w(g, role, "allow", FX + r"\Management\proposals\new.md")
        e(g, role, "ask", FX + r"\Management\tools\rules.json")
        e(g, role, "deny", FX + r"\.claude\settings.json")
        t(g, role, "allow", "Bash", sh(VALIDATE + " --feature " + owner))
        t(g, role, "deny", "Bash", sh("dotnet build"))
        t(g, role, "allow", guard.UNITY_ASSET_TOOL, delete("Assets" + own.replace("\\", "/") + "/Old.cs"))
        t(g, role, "deny", guard.UNITY_ASSET_TOOL, delete("Assets" + other.replace("\\", "/") + "/Old.cs"))
        t(g, role, "deny", guard.UNITY_ASSET_TOOL, delete("Assets/Features/Wallet/Scripts/Internal/Old.cs"))

    g = "role @Manager"
    M = "@Manager"
    for p in (own_w, A + r"\Core\Old.cs", A + r"\_Composition\Old.cs", F + r"\Wallet\Scripts\Contracts\IWallet.cs",
              FX + r"\Management\tools\rules.json", FX + r"\Management\data\x.js", FX + r"\.claude\settings.json"):
        e(g, M, "deny", p)
    w(g, M, "allow", FX + r"\Management\proposals\plan.md")
    e(g, M, "deny", FX + r"\Management\proposals\old.md")
    r(g, M, "allow", F + r"\Wallet\Scripts\Contracts\IWallet.cs")
    r(g, M, "allow", A + r"\Core\Old.cs")
    r(g, M, "deny", own_w)
    t(g, M, "allow", "Bash", sh(VALIDATE))
    t(g, M, "allow", "Bash", sh(VALIDATE + " --feature Shop"))
    t(g, M, "deny", guard.UNITY_ASSET_TOOL, delete("Assets/Core/Old.cs"))
    t(g, M, "pass", "SendMessage", {"to": "Shop", "message": "x"})
    t(g, M, "pass", "ListAgents", {})

    g = "role spelling"
    e(g, "@core", "deny", A + r"\Core\Old.cs")          # stored values are exact; bind_session stores "@Core"
    e(g, "@MANAGER", "deny", A + r"\Core\Old.cs")
    e(g, "@Cor", "deny", A + r"\Core\Old.cs")
    cli(g, "@Core", "allow", "process: @Core edits Core", payload("Edit", {"file_path": A + r"\Core\Old.cs"}))
    cli(g, "@Manager", "deny", "process: @Manager edits Core", payload("Edit", {"file_path": A + r"\Core\Old.cs"}))

    g = "bind: roles"
    say(g, "block, bound=@Core", "bind @Core", f"FEATURE=@Core {PIN}", session="role-core")
    say(g, "pass, bound=@Core", "@Core is told its area", "làm đi", session="role-core")
    say(g, "block, bound=@Composition", "bind @composition, any case", f"FEATURE=@composition {PIN}", session="role-comp")
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
    reports = FX + r"\Management\reports"
    human = {"type": "user", "message": {"role": "user", "content": "làm đi"}}
    meta = {"type": "user", "isMeta": True, "message": {"role": "user", "content": "<context>"}}
    result = {"type": "user", "message": {"content": [{"type": "tool_result", "content": "ok"}]}}
    def use(name, **ti): return {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": name, "input": ti}]}}
    def say_text(text): return {"type": "assistant", "message": {"content": [{"type": "text", "text": text}]}}
    edit = use("Edit", file_path="x")
    count = lambda: len(os.listdir(reports)) if os.path.isdir(reports) else 0     # noqa: E731
    for session, value in (("rep-wallet", "Wallet"), ("rep-core", "@Core"), ("rep-human", "@human"), ("rep-mgr", "@Manager")):
        Path(guard.binding_path(session, STORE)).write_text(json.dumps({"feature": value}))

    def rep(expect, label, session, entries, active=False, transcript=True, keep_pending=False):
        pending = stop_report.pending_path(session, STORE)
        if not keep_pending and os.path.exists(pending):
            os.remove(pending)
        path = FX + rf"\transcript-{len(results)}.jsonl"
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


def main():
    try:
        build_fixture()
        run_cases()
    finally:
        link = F + r"\Wallet\link"
        if os.path.lexists(link):
            os.rmdir(link)
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
