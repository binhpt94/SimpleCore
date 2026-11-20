#!/usr/bin/env python3
"""PreToolUse hook. Enforces the permission table in /CLAUDE.md.

FEATURE environment variable (set by the human who starts the session, not by the agent):
    <Name> or _Services/<Name>  -> agent bound to that feature
    @human                      -> no feature limits; still asks for .claude, _Composition, Core
    unset / invalid             -> every edit is denied

Default is deny: unknown tools, unknown zones and any error in this script are blocked.
Reads the hook JSON on stdin, writes the decision JSON on stdout. Standard library only.
Tests: python .claude/hooks/test_guard.py
"""
import json
import ntpath
import os
import re
import stat
import sys

# Shell commands an agent may run. Each entry is a regex that must match the WHOLE command.
# The command must be run from the project root.
ALLOWED_COMMANDS = [
    r"python Management/tools/validate\.py",
]

EDIT_TOOLS = {"Edit", "Write", "NotebookEdit", "MultiEdit"}
READ_TOOLS = {"Read", "Grep", "Glob"}
SHELL_TOOLS = {"Bash", "PowerShell"}
# Tools that do not touch project files.
PASS_TOOLS = {"AskUserQuestion", "TodoWrite", "TaskCreate", "TaskUpdate", "TaskList", "TaskGet", "Task", "Agent",
              "Skill", "ToolSearch", "WebSearch", "WebFetch", "EnterPlanMode", "ExitPlanMode", "ListAgents",
              "SendMessage"}
FORBIDDEN_DIRS = {"library", "temp", "logs", "obj", "usersettings"}
HUMAN_ASK_ZONES = {".claude", "_Composition", "Core"}
FEATURE_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
SHELL_META_RX = re.compile(r"[;&|<>`$(){}\r\n]")


def resolve_target(p, cwd):
    """Absolute, normalised Windows path. Raises ValueError on anything that could dodge the zone check."""
    if not p or not p.strip():
        p = cwd
    m = re.match(r"^/([a-zA-Z])(?:/(.*))?$", p)          # /d/x -> d:\x
    if m:
        p = m.group(1) + ":\\" + (m.group(2) or "")
    p = p.replace("/", "\\")
    if p.startswith("\\\\"):
        raise ValueError(f"UNC or device path: {p}")
    if not re.match(r"^[a-zA-Z]:\\", p):
        if p.startswith("\\") or re.match(r"^[a-zA-Z]:", p):
            raise ValueError(f"drive-relative path: {p}")
        p = ntpath.join(cwd, p)
    if ":" in p[2:]:
        raise ValueError(f"alternate data stream: {p}")
    for seg in p[3:].split("\\"):
        if seg in ("", ".", ".."):
            continue
        if seg[-1] in ". ":
            raise ValueError(f"segment ends with dot or space: {p}")
        if re.search(r"~\d", seg):
            raise ValueError(f"8.3 short name: {p}")
        if re.search(r'[*?"<>|]', seg):
            raise ValueError(f"wildcard or illegal character: {p}")
    return ntpath.normpath(p).rstrip("\\")


def inside(full, root):
    return full.lower().startswith(root.lower() + "\\")


def goes_through_link(full, root):
    """True when any existing folder/file between the root and the target is a link or junction."""
    if not inside(full, root):
        return False
    cur = root
    for seg in full[len(root) + 1:].split("\\"):
        cur = cur + "\\" + seg
        try:
            attrs = os.lstat(cur).st_file_attributes
        except FileNotFoundError:
            return False
        if attrs & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            return True
    return False


def zone(full, root, feature):
    """(name, readable, edit) for a path; edit is 'allow' | 'ask' | 'create' | 'deny'."""
    if full.lower() == root.lower():
        return "project root", False, "deny"
    if not inside(full, root):
        return "outside the project", False, "deny"
    seg = full[len(root) + 1:].split("\\")
    low = [s.lower() for s in seg]
    n = len(seg)

    if low[0] == ".claude":
        return ".claude", True, "deny"
    if low[0] == "claude.md" and n == 1:
        return "root CLAUDE.md", True, "deny"
    if low[0] == "management":
        if n >= 2 and low[1] == "proposals":
            return "Management/proposals", True, "create"
        return "Management", True, "deny"
    if low[0] != "unityproject" or n < 2:
        return "unlisted", False, "deny"

    if low[1] in FORBIDDEN_DIRS:
        return seg[1], False, "deny"
    if low[1] in ("packages", "projectsettings"):
        return seg[1], True, "deny"
    if low[1] != "assets" or n < 3:
        return "unlisted", False, "deny"

    if low[2] == "_composition":
        return "_Composition", True, "ask"
    if low[2] == "core":
        return "Core", True, "ask"
    if low[2] != "features" or n < 4:
        return "unlisted", False, "deny"

    name, i = seg[3], 4
    if low[3] == "_services":
        if n < 5:
            return "unlisted", False, "deny"
        name, i = "_Services/" + seg[4], 5
    rest = "/".join(seg[i:])

    if feature and name.lower() == feature.lower():
        if re.match(r"^Scripts/Contracts(/|$)", rest, re.I):
            return "own Contracts", True, "ask"
        return "own feature", True, "allow"
    if re.match(r"^(Scripts/Contracts|FeatureAssets/Public)(/|$)", rest, re.I):
        return f"public part of feature {name}", True, "deny"
    return f"private part of feature {name}", False, "deny"


def text_of(value):
    return value if isinstance(value, str) else ""


def decide(data, feature_env, root):
    """Returns (decision, reason), or None to leave the call to the normal permission flow."""
    tool = text_of(data.get("tool_name"))
    if not tool:
        raise ValueError("no tool_name in hook input")
    ti = data.get("tool_input")
    if not isinstance(ti, dict):
        ti = {}
    root = ntpath.normpath(root).rstrip("\\")
    cwd = resolve_target(text_of(data.get("cwd")), root) if data.get("cwd") else root

    # --- who is this session?
    raw = (feature_env or "").strip().replace("\\", "/")
    mode, feature = "none", None
    if raw == "@human":
        mode = "human"
    elif FEATURE_RX.match(raw) and os.path.isdir(ntpath.join(root, "UnityProject", "Assets", "Features", *raw.split("/"))):
        mode, feature = "feature", raw

    edit_path = text_of(ti.get("file_path")) or text_of(ti.get("notebook_path"))

    # --- human session: only the three sensitive zones ask
    if mode == "human":
        if tool in EDIT_TOOLS:
            name = zone(resolve_target(edit_path, cwd), root, None)[0]
            if name in HUMAN_ASK_ZONES:
                return "ask", f"human session editing {name}"
        return None

    if tool in PASS_TOOLS:
        return None

    if tool in SHELL_TOOLS:
        cmd = text_of(ti.get("command")).strip()
        if any(re.fullmatch(rx, cmd) for rx in ALLOWED_COMMANDS) and not SHELL_META_RX.search(cmd):
            return "allow", "shell command is on the allow-list"
        return "deny", "shell command is not on the allow-list in guard.py. Stop and tell the human."

    if tool in EDIT_TOOLS:
        if not edit_path:
            return "deny", "edit tool without a file path"
        full = resolve_target(edit_path, cwd)
        if goes_through_link(full, root):
            return "deny", "path goes through a link or junction"
        if mode == "none":
            return "deny", "FEATURE is not set or is not an existing feature folder; all edits are blocked"
        name, _, edit = zone(full, root, feature)
        # Files that define the boundary itself.
        if full.lower().endswith(".asmref"):
            return "deny", ".asmref files are forbidden: they put code into another assembly"
        if edit == "allow":
            new = text_of(ti.get("content")) + text_of(ti.get("new_string")) + text_of(ti.get("new_source"))
            edits = ti.get("edits")
            for e in edits if isinstance(edits, list) else []:
                if isinstance(e, dict):
                    new += text_of(e.get("new_string"))
            if full.lower().endswith(".asmdef"):
                return "ask", "editing an .asmdef changes what this feature may reference; needs human approval"
            if ntpath.basename(full).lower() == "assemblyinfo.cs" or re.search("InternalsVisibleTo", new, re.I):
                return "ask", "InternalsVisibleTo opens internal types to another assembly; needs human approval"
            return "allow", f"edit inside {name}"
        if edit == "ask":
            return "ask", (f"editing {name} needs human approval. Say what changes and why it cannot be done "
                           "inside the feature.")
        if edit == "create":
            if tool == "Write" and not os.path.lexists(full):
                return "allow", "new proposal file"
            return "deny", "Management/proposals is create-only: write a new file, do not change an existing one"
        return "deny", f"FEATURE={feature} may not edit: {name}"

    if tool in READ_TOOLS:
        if tool == "Read":
            target = text_of(ti.get("file_path"))
            if not target:
                return "deny", "Read without a file path"
        else:
            target = text_of(ti.get("path"))
            # Glob.pattern and Grep.glob are file globs; Grep.pattern is a regex and is not a path.
            glob = text_of(ti.get("pattern") if tool == "Glob" else ti.get("glob"))
            if ".." in glob or re.match(r"^([a-zA-Z]:|[\\/])", glob):
                return "deny", 'glob may not be absolute or contain ".."; set the search folder with path instead'
        full = resolve_target(target, cwd)
        if goes_through_link(full, root):
            return "deny", "path goes through a link or junction"
        name, readable, _ = zone(full, root, feature)
        if readable:
            return "allow", f"read inside {name}"
        return "deny", f"may not read: {name}. Search and read only inside your feature or a readable zone."

    return "deny", f"tool '{tool}' is not on the guard's list of known tools"


def main(argv):
    try:
        try:
            root = ntpath.dirname(ntpath.dirname(ntpath.dirname(ntpath.abspath(__file__))))
            if len(argv) == 3 and argv[1] == "--root":      # test-only override; the real hook never passes it
                root = ntpath.abspath(argv[2])
            data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
            if not isinstance(data, dict):
                raise ValueError("hook input is not a JSON object")
            result = decide(data, os.environ.get("FEATURE", ""), root)
        except Exception as ex:      # fail closed
            result = ("deny", f"blocked on error: {ex}")
        if result:
            out = {"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": result[0],
                                          "permissionDecisionReason": "guard: " + result[1]}}
            sys.stdout.buffer.write(json.dumps(out).encode("utf-8"))
            sys.stdout.buffer.flush()
        return 0
    except BaseException as ex:      # could not even answer: exit code 2 blocks the tool call
        sys.stderr.write(f"guard: blocked on error: {ex}\n")
        return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
