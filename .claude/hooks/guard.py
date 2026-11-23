#!/usr/bin/env python3
"""PreToolUse hook. Enforces the permission table in /CLAUDE.md.

Who a session is comes from the human, never from the agent: the binding for this chat (session_id),
made when the human types `FEATURE=<value> <PIN>` in the chat; see bind_session.py. Bindings and the
PIN live outside the project, in STORE_DIR. The FEATURE environment variable is not used.
The value, by group:
    Master    @human                      no feature limits; still asks for .claude, _Composition, Core
    Leaders   @Manager                    coordinator: edits nothing but new files in Management/proposals
              @CodeLead                   checks code and settles conflicts: edits NOTHING; reads all source of
                                          Basic/Features/Services; compiles, tests and plays through a read-only
                                          subset of unity-mcp (CODELEAD_UNITY); writes one thing, the progress
                                          of a design leaf, through gd_progress.py
              @GameDesigner               designs, never codes: reads only Management/game-design (and its own rules
                                          file), writes only in Management/game-design/ai, deletes there with
                                          gd_delete.py, reads its status with gd_status.py; live, mode.json, the
                                          answers, the scores and the history are not its to write. A node is
                                          writable only when every node above it is approved (gamedesign.py)
    Basic     @Core / @Composition        edits only Assets/Core / Assets/_Composition
    Features  <Name>                      edits only its feature
    Services  _Services/<Name>            edits only its service
    unset / invalid                       every edit is denied
Basic, Features and Services read the public parts of features, Core, _Composition, Management, .claude and
the root CLAUDE.md. Changing their own Contracts, an .asmdef or InternalsVisibleTo is allowed here; the rules
(.claude/rules/codelead.md) make them agree it with @CodeLead first. rules.json and deleting folders ask the human.
Features and Services may edit their own folder only once its design has reached round 4 in the live table
(Management/tools/gamedesign.py, code_allowed); reading is not gated, and neither are @Core and @Composition.

Paths (Windows and Linux/macOS):
    Every path is turned into one canonical form before any check: absolute, normalised, '/' as separator
    ('D:/Proj/x' on Windows, '/home/u/Proj/x' elsewhere). resolve_target does it; nothing else splits paths.
    Windows: case-insensitive; '/d/x' (Git Bash) means 'd:/x'; UNC, drive-relative, ADS, 8.3 names, trailing
             dot/space are refused.
    POSIX:   case-sensitive; a backslash is refused (it is a file-name character there, not a separator).
    Zone names that only restrict (deny) match in any letter case; zones that grant (allow/create/design/ask)
    match the exact spelling on a case-sensitive system, so 'assets/core' on Linux is not Assets/Core.

Default is deny: unknown tools, unknown zones and any error in this script are blocked.
Reads the hook JSON on stdin, writes the decision JSON on stdout. Standard library only.
Tests: python .claude/hooks/test_guard.py
"""
import json
import ntpath
import os
import posixpath
import re
import stat
import sys

# Shell commands an agent may run. Each entry is a regex that must match the WHOLE command.
# The command must be run from the project root. `python3` is accepted too (Linux/macOS).
PY = r"python3? "
ALLOWED_COMMANDS = [
    PY + r"Management/tools/validate\.py( --feature ((_Services/)?[A-Za-z][A-Za-z0-9]*|_Composition))?",
]

# One more command, for @GameDesigner only: delete a file or a node inside Management/game-design/ai.
DESIGNER_COMMANDS = [
    PY + r"Management/tools/gd_delete\.py ai(/[A-Za-z0-9._-]+)+",
    PY + r"Management/tools/gd_status\.py( ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)*)?",
]
# For @CodeLead only: mark the progress of a design leaf after checking the code, and look at the design's status.
CODELEAD_COMMANDS = [
    PY + r"Management/tools/gd_progress\.py ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)* (idea|prototype|full) (todo|doing|done)",
    PY + r"Management/tools/gd_status\.py( ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)*)?",
]

EDIT_TOOLS = {"Edit", "Write", "NotebookEdit", "MultiEdit"}
READ_TOOLS = {"Read", "Grep", "Glob"}
SHELL_TOOLS = {"Bash", "PowerShell"}
# Tools that do not touch project files.
PASS_TOOLS = {"AskUserQuestion", "TodoWrite", "TaskCreate", "TaskUpdate", "TaskList", "TaskGet", "Task", "Agent",
              "Skill", "ToolSearch", "WebSearch", "WebFetch", "EnterPlanMode", "ExitPlanMode", "ListAgents",
              "SendMessage"}
FORBIDDEN_DIRS = {"library", "temp", "logs", "obj", "usersettings"}
DESIGN_AI, DESIGN_LIVE, DESIGN_BASE = ("Management/game-design/ai", "Management/game-design/live",
                                       "Management/game-design")
DESIGN_ZONES = {DESIGN_AI, DESIGN_LIVE, DESIGN_BASE}
# Inside ai: the proposed tree; the human's answers file that sits beside each node's questions; and the folders
# only the page and the history code write (the human's scores, the node's history).
DESIGN_TREE, DESIGN_CHOOSE, DESIGN_ANSWERS = "idea", ".ai-need-user-choose", "answers.json"
DESIGN_KEPT = {".user-review", ".history", ".progress"}
DESIGNER_RULES_DIR = ".claude/rules"      # the shared rules and one file per round
HUMAN_ASK_ZONES = {".claude", "_Composition", "Core", DESIGN_LIVE}
# FEATURE values that are not feature folders.
ROLES = ("@human", "@Core", "@Composition", "@Manager", "@CodeLead", "@GameDesigner")
ROLE_AREAS = {"@Core": "Core", "@Composition": "_Composition"}
# The one unity-mcp call a Basic/Features/Services session may make: manage_asset with action "delete".
UNITY_ASSET_TOOL = "mcp__unityMCP__manage_asset"
# What @CodeLead may call in unity-mcp: compile, test, play and look. None = every action of that tool.
CODELEAD_UNITY = {
    "mcp__unityMCP__refresh_unity": None,
    "mcp__unityMCP__read_console": None,
    "mcp__unityMCP__run_tests": None,
    "mcp__unityMCP__get_test_job": None,
    "mcp__unityMCP__find_gameobjects": None,
    "mcp__unityMCP__find_in_file": None,
    "mcp__unityMCP__get_sha": None,
    "mcp__unityMCP__validate_script": None,
    "mcp__unityMCP__unity_docs": None,
    "mcp__unityMCP__unity_reflect": None,
    "mcp__unityMCP__manage_script_capabilities": None,
    "mcp__unityMCP__debug_request_context": None,
    "mcp__unityMCP__manage_editor": {"play", "pause", "stop", "telemetry_status", "telemetry_ping"},
    "mcp__unityMCP__manage_scene": {"get_hierarchy", "get_active", "get_loaded_scenes", "get_build_settings"},
    "mcp__unityMCP__manage_asset": {"search", "get_info", "get_components"},
    "mcp__unityMCP__manage_prefabs": {"get_info", "get_hierarchy"},
    "ReadMcpResourceTool": None,
    "ListMcpResourcesTool": None,
    "ReadMcpResourceDirTool": None,
}
FEATURE_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
SHELL_META_RX = re.compile(r"[;&|<>`$(){}\r\n]")
# Per-chat bindings and the PIN. Outside the project, so no agent session can read or write it.
STORE_DIR = os.path.join(os.path.expanduser("~"), ".coresimple-guard")
SESSION_RX = re.compile(r"^[A-Za-z0-9_-]{1,128}$")

# --- the file system this hook runs on
WINDOWS = os.name == "nt"
CASE = re.I if WINDOWS else 0                    # regex flag for zones that grant something
DESIGNER_RULES_RX = re.compile(r"^gamedesigner(\.round[1-4])?\.md$", CASE)
ILLEGAL_SEG_RX = re.compile(r'[*?"<>|]')


# ---------------------------------------------------------------- paths
def fold(s):
    """Path text as the file system compares it."""
    return s.lower() if WINDOWS else s


def same(a, b):
    return fold(a) == fold(b)


def exact(segs, path):
    """True when the path segments are exactly `path` ('A/B/C'), compared as the file system does."""
    want = path.split("/")
    return len(segs) == len(want) and all(same(a, b) for a, b in zip(segs, want))


def under(base, *parts):
    """Canonical path of parts below a canonical base."""
    return "/".join([base] + [p.strip("/") for p in parts])


def canonical_root(p):
    """Canonical form of the project root (or any trusted absolute folder)."""
    p = os.path.abspath(p)
    if WINDOWS:
        return ntpath.normpath(p).replace("\\", "/").rstrip("/")
    p = posixpath.normpath(p)
    if p.startswith("//"):
        p = "/" + p.lstrip("/")
    return p.rstrip("/")


def _resolve_windows(p, cwd):
    m = re.match(r"^/([a-zA-Z])(?:/(.*))?$", p)          # Git Bash: /d/x -> d:\x
    if m:
        p = m.group(1) + ":\\" + (m.group(2) or "")
    p = p.replace("/", "\\")
    if p.startswith("\\\\"):
        raise ValueError(f"UNC or device path: {p}")
    if not re.match(r"^[a-zA-Z]:\\", p):
        if p.startswith("\\") or re.match(r"^[a-zA-Z]:", p):
            raise ValueError(f"drive-relative path: {p}")
        p = cwd.replace("/", "\\") + "\\" + p
    if ":" in p[2:]:
        raise ValueError(f"alternate data stream: {p}")
    for seg in p[3:].split("\\"):
        if seg in ("", ".", ".."):
            continue
        if seg[-1] in ". ":
            raise ValueError(f"segment ends with dot or space: {p}")
        if re.search(r"~\d", seg):
            raise ValueError(f"8.3 short name: {p}")
        if ILLEGAL_SEG_RX.search(seg):
            raise ValueError(f"wildcard or illegal character: {p}")
    return ntpath.normpath(p).replace("\\", "/").rstrip("/")


def _resolve_posix(p, cwd):
    if "\\" in p:
        raise ValueError(f"backslash in path (not a separator on this system; use '/'): {p}")
    if not p.startswith("/"):
        p = cwd + "/" + p
    for seg in p.split("/"):
        if ILLEGAL_SEG_RX.search(seg):
            raise ValueError(f"wildcard or illegal character: {p}")
    p = posixpath.normpath(p)
    if p.startswith("//"):
        p = "/" + p.lstrip("/")
    return p.rstrip("/")


def resolve_target(p, cwd):
    """Canonical path (absolute, normalised, '/' separators) of p, relative paths taken from the canonical cwd.
    Raises ValueError on anything that could dodge the zone check."""
    if not p or not p.strip():
        return cwd
    if "\0" in p:
        raise ValueError("NUL character in path")
    if p.startswith("~"):
        raise ValueError(f"home-relative path: {p}")
    return _resolve_windows(p, cwd) if WINDOWS else _resolve_posix(p, cwd)


def inside(full, root):
    return fold(full).startswith(fold(root) + "/")


def rel_segments(full, root):
    """Segments of a canonical path below root (call only when inside(full, root))."""
    return full[len(root) + 1:].split("/")


def is_link(path):
    """True for a symlink (any OS) or a junction/reparse point (Windows); None when the path does not exist."""
    try:
        st = os.lstat(path)
    except (FileNotFoundError, NotADirectoryError):
        return None
    if stat.S_ISLNK(st.st_mode):
        return True
    reparse = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)
    return bool(getattr(st, "st_file_attributes", 0) & reparse)


def goes_through_link(full, root):
    """True when any existing folder/file between the root and the target is a link or junction."""
    if not inside(full, root):
        return False
    cur = root
    for seg in rel_segments(full, root):
        cur = cur + "/" + seg
        link = is_link(cur)
        if link is None:
            return False
        if link:
            return True
    return False


# ---------------------------------------------------------------- sessions
def binding_path(session_id, store):
    """File that holds the binding of one chat, or None when the session id is not usable as a file name."""
    if not store or not isinstance(session_id, str) or not SESSION_RX.match(session_id):
        return None
    return os.path.join(store, "sessions", session_id + ".json")


def bound_feature(session_id, store):
    """FEATURE value the human bound to this chat, or None when the chat has no binding."""
    path = binding_path(session_id, store)
    if not path:
        return None
    try:
        with open(path, encoding="utf-8") as f:
            value = json.load(f).get("feature")
    except (OSError, ValueError, AttributeError):
        return None
    return value if isinstance(value, str) else None


def session_feature(session_id, store):
    """The raw FEATURE value of a session: the chat's binding, or "" when it has none."""
    return bound_feature(session_id, store) or ""


def valid_feature(raw, root):
    """True when raw names an existing feature folder."""
    return bool(FEATURE_RX.match(raw)) and os.path.isdir(
        os.path.join(root, "UnityProject", "Assets", "Features", *raw.split("/")))


def role_of(raw):
    """The role a stored FEATURE value names (exact spelling), or None."""
    raw = (raw or "").strip()
    return raw if raw in ROLES else None


def canonical_role(typed):
    """The role a human typed, in any letter case, as stored; None when it is not a role."""
    return next((r for r in ROLES if r.lower() == (typed or "").strip().lower()), None)


# ---------------------------------------------------------------- zones
def zone(full, root, feature, area=None, read_all=False):
    """(name, readable, edit) for a canonical path; edit is 'allow' | 'ask' | 'create' | 'design' | 'deny'.
    'design': only @GameDesigner edits there.
    feature: the session's own feature; area: "Core" or "_Composition" for @Core / @Composition;
    read_all: @CodeLead, who may read the private parts of every feature as well.
    Checks that only deny match in any letter case; checks that grant match as the file system compares."""
    if same(full, root):
        return "project root", False, "deny"
    if not inside(full, root):
        return "outside the project", False, "deny"
    seg = rel_segments(full, root)
    low = [s.lower() for s in seg]
    n = len(seg)

    if low[0] == ".claude":
        return ".claude", True, "deny"
    if low[0] == "claude.md" and n == 1:
        return "root CLAUDE.md", True, "deny"
    if low[0] == "management":
        if not same(seg[0], "Management"):
            return "Management", True, "deny"
        if n >= 2 and low[1] == "proposals":
            if exact(seg[:2], "Management/proposals"):
                return "Management/proposals", True, "create"
            return "Management", True, "deny"
        if n >= 2 and low[1] == "game-design":
            if n >= 3 and low[2] == "ai" and exact(seg[:3], DESIGN_AI):
                return DESIGN_AI, True, "design"
            if n >= 3 and low[2] == "live":
                return DESIGN_LIVE, True, "deny"
            return DESIGN_BASE, True, "deny"
        if low[1:] == ["tools", "rules.json"]:
            if exact(seg, "Management/tools/rules.json"):
                return "Management/tools/rules.json", True, "ask"
            return "Management", True, "deny"
        return "Management", True, "deny"
    if low[0] != "unityproject" or n < 2:
        return "unlisted", False, "deny"

    if low[1] in FORBIDDEN_DIRS:
        return seg[1], False, "deny"
    if low[1] in ("packages", "projectsettings"):
        return seg[1], True, "deny"
    if not exact(seg[:2], "UnityProject/Assets") or n < 3:
        return "unlisted", False, "deny"

    # Only @Core / @Composition edit Core / _Composition; a feature session asks them.
    if low[2] == "_composition":
        if area == "_Composition" and same(seg[2], "_Composition"):
            return "own _Composition", True, "allow"
        return "_Composition", True, "deny"
    if low[2] == "core":
        if area == "Core" and same(seg[2], "Core"):
            return "own Core", True, "allow"
        return "Core", True, "deny"
    if not same(seg[2], "Features") or n < 4:
        return "unlisted", False, "deny"

    name, i = seg[3], 4
    if low[3] == "_services":
        if n < 5:
            return "unlisted", False, "deny"
        name, i = seg[3] + "/" + seg[4], 5      # keeps the spelling on disk; same() decides if it is ours
    rest = "/".join(seg[i:])

    if feature and same(name, feature):
        if re.match(r"^Scripts/Contracts(/|$)", rest, CASE):
            return "own Contracts", True, "allow"
        return "own feature", True, "allow"
    # The CLAUDE.md at a feature's root describes it to the others: readable by all, like its Contracts.
    if re.match(r"^(Scripts/Contracts|FeatureAssets/Public)(/|$)", rest, CASE) or same(rest, "CLAUDE.md"):
        return f"public part of feature {name}", True, "deny"
    return f"private part of feature {name}", read_all, "deny"


def text_of(value):
    return value if isinstance(value, str) else ""


def is_asmref(full):
    return full.lower().endswith(".asmref")


def decide_unity_delete(ti, mode, feature, root, area=None, who=""):
    """Deleting through unity-mcp: one existing file inside the session's own feature or area, nothing else."""
    if text_of(ti.get("action")) != "delete":
        return "deny", "unity-mcp manage_asset: a feature session may use only action 'delete'"
    if mode == "none":
        return "deny", "FEATURE is not set or is not an existing feature folder; deleting is blocked"
    if mode in ("manager", "codelead", "gamedesigner"):
        return "deny", f"{who} does not change the project; ask the session that owns the file"
    path = text_of(ti.get("path")).replace("\\", "/")      # a Unity asset path: '/' on every OS
    if not re.match(r"^Assets/[^/]", path):
        return "deny", "unity-mcp delete: write the path from the Unity project root, starting with 'Assets/'"
    full = resolve_target("UnityProject/" + path, root)
    if not inside(full, under(root, "UnityProject", "Assets")):
        return "deny", "unity-mcp delete: the path leaves UnityProject/Assets"
    if goes_through_link(full, root):
        return "deny", "path goes through a link or junction"
    name, _, edit = zone(full, root, feature, area)
    if edit != "allow":
        return "deny", f"FEATURE={who} may not delete in: {name}"
    if is_asmref(full):
        return "deny", ".asmref files are forbidden"
    if mode == "feature":
        locked = design_gate(root, feature)
        if locked:
            return "deny", locked
    if not os.path.isfile(full):
        return "ask", "deleting a folder (or a path that is not a file) needs human approval"
    return "allow", f"delete inside {name}"


def gamedesign_module(root):
    tools = os.path.join(root, "Management", "tools")
    if tools not in sys.path:
        sys.path.insert(0, tools)
    import gamedesign
    return gamedesign


def decide_designer_edit(full, root, name):
    """@GameDesigner writing inside Management/game-design/ai: never the human's answers or scores, never the
    history; and inside the proposed tree only where the write gate lets it (Management/tools/gamedesign.py, the
    same code the page uses)."""
    seg = rel_segments(full, root)[3:]      # below Management/game-design/ai
    low = [s.lower() for s in seg]
    if low and low[-1] == DESIGN_ANSWERS and DESIGN_CHOOSE in low:
        return "deny", (f"{DESIGN_ANSWERS} holds the human's answers; only the management page writes it. "
                        "Read it, then update the node from it.")
    if DESIGN_KEPT.intersection(low):
        return "deny", (".user-review (the human's scores and feedback), .history (the node's history) and "
                        ".progress (what is built) are written only by the management page and its tools. "
                        "Read them; do not write them.")
    if seg[:1] and same(seg[0], DESIGN_TREE):
        keys = []
        for s in seg[1:-1]:      # the node folders between the tree root and the file
            if s.startswith("."):
                break
            keys.append(s)
        ok, reason = gamedesign_module(root).can_write_node(root, keys)
        if not ok:
            return "deny", ("this node is locked because a node above it is not approved yet "
                            f"({reason}). Redo that node if it is yours to redo, otherwise stop and tell the human; "
                            "do not work on this round until then.")
    elif low[:1] == [DESIGN_TREE]:      # 'Idea' on a case-sensitive system: not the tree, refuse
        return "deny", f"'{seg[0]}' is not the design tree '{DESIGN_TREE}'"
    return "allow", f"edit inside {name}"


def design_gate(root, feature):
    """None when the Features/Services session `feature` may code now, otherwise the reason it may not: its design
    must have reached round 4 in the live table (Management/tools/gamedesign.py, the same code the page uses)."""
    ok, reason = gamedesign_module(root).code_allowed(root, feature)
    return None if ok else ("this feature may not be coded yet: " + reason + ". Stop and tell the human; the design "
                            "is @GameDesigner's work and the human fixes it into live.")


def decide_codelead_unity(tool, ti):
    """@CodeLead in unity-mcp: compile, test, play and look; never change the project."""
    actions = CODELEAD_UNITY[tool]
    action = text_of(ti.get("action"))
    if actions is None or action in actions:
        return "allow", f"@CodeLead may use {tool}" + (f" ({action})" if action else "")
    return "deny", (f"@CodeLead may not use {tool} action '{action}': it compiles, tests, plays and reads, but "
                    "changes nothing. Ask the session that owns the area to make the change.")


def decide(data, session_value, root):
    """Returns (decision, reason), or None to leave the call to the normal permission flow."""
    tool = text_of(data.get("tool_name"))
    if not tool:
        raise ValueError("no tool_name in hook input")
    ti = data.get("tool_input")
    if not isinstance(ti, dict):
        ti = {}
    root = canonical_root(root)
    cwd = resolve_target(text_of(data.get("cwd")), root) if data.get("cwd") else root

    # --- who is this session?
    raw = (session_value or "").strip().replace("\\", "/")
    role = role_of(raw)
    mode, feature, area = "none", None, None
    if role == "@human":
        mode = "human"
    elif role == "@Manager":
        mode = "manager"
    elif role == "@CodeLead":
        mode = "codelead"
    elif role == "@GameDesigner":
        mode = "gamedesigner"
    elif role in ROLE_AREAS:
        mode, area = "area", ROLE_AREAS[role]
    elif valid_feature(raw, root):
        mode, feature = "feature", raw
    who = role or raw
    read_all = mode == "codelead"

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
        commands = ALLOWED_COMMANDS + (DESIGNER_COMMANDS if mode == "gamedesigner" else []) + (
            CODELEAD_COMMANDS if mode == "codelead" else [])
        if any(re.fullmatch(rx, cmd) for rx in commands) and not SHELL_META_RX.search(cmd) and ".." not in cmd:
            return "allow", "shell command is on the allow-list"
        return "deny", "shell command is not on the allow-list in guard.py. Stop and tell the human."

    if tool in EDIT_TOOLS:
        if not edit_path:
            return "deny", "edit tool without a file path"
        full = resolve_target(edit_path, cwd)
        if goes_through_link(full, root):
            return "deny", "path goes through a link or junction"
        if mode == "none":
            return "deny", ("this chat has no valid FEATURE, so all edits are blocked. Only the human can set it "
                            "(by typing FEATURE=<name> <PIN> in the chat). Stop and tell the human.")
        if mode == "codelead":
            return "deny", ("@CodeLead edits nothing. Send the owning session exactly what to fix (file, change, "
                            "why); it makes the change. See .claude/rules/codelead.md.")
        name, _, edit = zone(full, root, feature, area)
        # Files that define the boundary itself.
        if is_asmref(full):
            return "deny", ".asmref files are forbidden: they put code into another assembly"
        if mode == "gamedesigner":
            if edit == "design":
                return decide_designer_edit(full, root, name)
            return "deny", ("@GameDesigner writes only inside Management/game-design/ai. live is fixed by the human "
                            "(the promote button of the management page). See .claude/rules/gamedesigner.md.")
        if edit == "design":
            return "deny", (f"FEATURE={who} may not edit {name}: only @GameDesigner writes there. Ask @GameDesigner "
                            "(or the human) for a design change.")
        if mode == "manager" and edit != "create":
            return "deny", ("@Manager does not edit code, assets or settings; it may only create new files in "
                            "Management/proposals. Ask the session that owns the area.")
        if edit == "allow":
            if mode == "feature":
                locked = design_gate(root, feature)
                if locked:
                    return "deny", locked
            return "allow", f"edit inside {name}"
        if edit == "ask":
            return "ask", (f"editing {name} needs human approval. Say what changes and why it cannot be done "
                           "inside your own area.")
        if edit == "create":
            if tool == "Write" and not os.path.lexists(full):
                return "allow", "new proposal file"
            return "deny", "Management/proposals is create-only: write a new file, do not change an existing one"
        return "deny", f"FEATURE={who} may not edit: {name}"

    if tool in READ_TOOLS:
        if tool == "Read":
            target = text_of(ti.get("file_path"))
            if not target:
                return "deny", "Read without a file path"
        else:
            target = text_of(ti.get("path"))
            # Glob.pattern and Grep.glob are file globs; Grep.pattern is a regex and is not a path.
            glob = text_of(ti.get("pattern") if tool == "Glob" else ti.get("glob"))
            if ".." in glob or re.match(r"^([a-zA-Z]:|[\\/~])", glob):
                return "deny", 'glob may not be absolute or contain ".."; set the search folder with path instead'
        full = resolve_target(target, cwd)
        if goes_through_link(full, root):
            return "deny", "path goes through a link or junction"
        name, readable, _ = zone(full, root, feature, area, read_all)
        if mode == "gamedesigner":      # sees the design tree and its own rules, nothing else
            readable = name in DESIGN_ZONES or (
                same(posixpath.dirname(full), under(root, DESIGNER_RULES_DIR))
                and bool(DESIGNER_RULES_RX.match(posixpath.basename(full))))
        if readable:
            return "allow", f"read inside {name}"
        return "deny", f"may not read: {name}. Search and read only inside your own area or a readable zone."

    if mode == "codelead" and tool in CODELEAD_UNITY:
        return decide_codelead_unity(tool, ti)

    if tool == UNITY_ASSET_TOOL:
        return decide_unity_delete(ti, mode, feature, root, area, who)

    return "deny", f"tool '{tool}' is not on the guard's list of known tools"


def main(argv):
    try:
        try:
            root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
            store = STORE_DIR
            if len(argv) in (3, 5) and argv[1] == "--root":      # test-only override; the real hook never passes it
                root = os.path.abspath(argv[2])
                store = argv[4] if len(argv) == 5 and argv[3] == "--store" else None
            data = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
            if not isinstance(data, dict):
                raise ValueError("hook input is not a JSON object")
            result = decide(data, session_feature(data.get("session_id"), store), root)
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

