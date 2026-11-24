#!/usr/bin/env python3
"""PreToolUse hook. Enforces the permission table in /CLAUDE.md.

Who a session is comes from the human, never from the agent: the binding for this chat (session_id),
made when the human types `FEATURE=<value> <PIN>` in the chat; see bind_session.py. Bindings and the
PIN live outside the project, in STORE_DIR. The FEATURE environment variable is not used.
The value, by group:
    Master    @human                      no feature limits and no questions: every edit passes (HUMAN_ASK_ZONES)
    Leaders   @Manager                    coordinator: edits nothing but new files in Management/proposals; runs
                                          scaffold.py, which makes the folders the live design names (no code)
              @CodeLead                   checks code, settles conflicts and owns the SKELETON: edits only the
                                          skeleton (below), of a feature once its code gate is open, of Core always; and
                                          all of Assets/_Composition (the wiring: Boot, scenes, catalogs; LEAD_AREA), its
                                          assets and scenes through unity-mcp too; reads all source of Basic/Features/Services; compiles,
                                          tests and plays through unity-mcp (UNITY_RULES; holds the Editor lock),
                                          deletes skeleton files there; writes the progress of a design leaf through
                                          gd_progress.py
              @GameDesigner               designs, never codes: reads only Management/game-design (and its own rules
                                          file), writes only in Management/game-design/ai, deletes there with
                                          gd_delete.py, reads its status with gd_status.py; live, mode.json, the
                                          answers, the scores and the history are not its to write. A node is
                                          writable only when every node above it is approved (gamedesign.py)
    Tools     @Codex / @Blender /         take orders from the features (Management/studio, studio.py): write only their
              @ClaudeImage                lessons.md and the work folder of an order; run Codex / Blender through
                                          studio.py; blender MCP (@Blender) and unity-mcp custom tools are a soft gate.
                                          @ClaudeImage draws each picture itself as an SVG in the work folder and
                                          studio.py makes the PNG
              @DesignAdvisor              asks GPT (Codex CLI, through gd_advise.py) about the design when the human or
                                          @GameDesigner asks it to; reads Management/game-design, writes only its questions
                                          and notes in Management/game-design/advice (<node>/<id>.q.md, <id>.note.md); the
                                          answers are written by gd_advise.py. Its advice binds nobody
    Basic     @Core                       edits only Assets/Core
    Features  <Name>                      edits only its feature
    Services  _Services/<Name>            edits only its service
    unset / invalid                       every edit is denied
Basic, Features, Services and Tools read the public parts of features, Core, _Composition, Management, .claude and
the root CLAUDE.md. Every bound chat (@GameDesigner too) reads Management/studio and Resources; a feature creates
the order.md of its own orders there, once, and judges what was delivered (studio.py review; art concepts are
judged by the human on the management page). rules.json and deleting folders ask the human
(@CodeLead deletes its own folders itself: skeleton folders, and folders inside _Composition).
unity-mcp (UNITY_RULES): code sessions read through it, change assets of their own area (write), compile
(shared) and change the open scene or a prefab stage (state); play, tests and project settings are @CodeLead's.
state/play/lead take the Editor lock (LOCK_FILE in STORE_DIR): one chat at a time, renewed on every call, freed
by the Stop hook (stop_unlock.py) or after LOCK_SECONDS. Writing code through unity-mcp is denied: Edit/Write.
The skeleton (the public gates and base classes; only @CodeLead edits it, the owner reads it and codes subclasses):
    in a feature or service   Scripts/Contracts/**, Scripts/Internal/Base/**, Scripts/View/Base/**, every *.asmdef,
                              AssemblyInfo.cs (the one place for InternalsVisibleTo: other files may not write it)
    in Core, _Composition     every *.asmdef, AssemblyInfo.cs, the interface files I<Name>.cs outside Tests
                              (_Composition is @CodeLead's as a whole, so the split matters only for Core)
Features and Services may edit their own folder only once its design has reached round 4 in the live table
(Management/tools/gamedesign.py, code_allowed); reading is not gated, and neither are @Core and @CodeLead's _Composition.

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
import time
import sys

# Shell commands an agent may run. Each entry is a regex that must match the WHOLE command.
# The command must be run from the project root. `python3` is accepted too (Linux/macOS).
PY = r"python3? "
ALLOWED_COMMANDS = [
    PY + r"Management/tools/validate\.py( --feature ((_Services/)?[A-Za-z][A-Za-z0-9]*|_Composition))?",
    # the fixed design of one code folder (reads Management/game-design/live; a long pack is written to
    # Management/game-design/.pack/, which every session that reads the design may read, and the command prints its path)
    PY + r"Management/tools/gd_pack\.py (_Services/)?[A-Za-z][A-Za-z0-9]*",
    # the frames of one design node, resolved (reads only; --html writes a file, so it is left to the human)
    PY + r"Management/tools/gd_frame\.py (ai/idea|live)(/[a-z0-9]+(-[a-z0-9]+)*)+",
]

# One more command, for @GameDesigner only: delete a file or a node inside Management/game-design/ai.
DESIGNER_COMMANDS = [
    PY + r"Management/tools/gd_delete\.py ai(/[A-Za-z0-9._-]+)+",
    PY + r"Management/tools/gd_status\.py( ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)*)?",
]
# For @DesignAdvisor only: ask GPT one question about a design node (the question is the file
# Management/game-design/advice/<node>/<id>.q.md it wrote; the answer lands beside it), and look at the design's status.
ADVICE_ID = r"[a-z0-9]+(-[a-z0-9]+)*"
ADVICE_NODE = r"(_general|_root|" + ADVICE_ID + r"(\." + ADVICE_ID + r")*)"
ADVISOR_COMMANDS = [
    PY + r"Management/tools/gd_advise\.py " + ADVICE_NODE + " " + ADVICE_ID,
    PY + r"Management/tools/gd_status\.py( ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)*)?",
]
# For @CodeLead only: mark the progress of a design leaf after checking the code, and look at the design's status.
CODELEAD_COMMANDS = [
    PY + r"Management/tools/gd_progress\.py ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)* (idea|prototype|full) (todo|doing|done)",
    PY + r"Management/tools/gd_status\.py( ai/idea(/[a-z0-9]+(-[a-z0-9]+)*)*)?",
]
# For @Manager only: create the feature, service, Core and _Composition folders the live design asks for and that
# are missing (folders and .gitkeep only, never code; see scaffold.py).
MANAGER_COMMANDS = [
    PY + r"Management/tools/scaffold\.py( --dry-run)?",
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
DESIGN_ADVICE = "Management/game-design/advice"      # what GPT said about the design: @DesignAdvisor's, binds nobody
DESIGN_ZONES = {DESIGN_AI, DESIGN_LIVE, DESIGN_BASE, DESIGN_ADVICE}
READ_ALL_ZONES = {"Management/studio", "Resources"}      # the orders and what was delivered: every bound chat reads
# Inside ai: the proposed tree; the human's answers file that sits beside each node's questions; and the folders
# only the page and the history code write (the human's scores, the node's history).
DESIGN_TREE, DESIGN_CHOOSE, DESIGN_ANSWERS = "idea", ".ai-need-user-choose", "answers.json"
DESIGN_KEPT = {".user-review", ".history", ".progress"}
DESIGNER_RULES_DIR = ".claude/rules"      # the shared rules and one file per round
HUMAN_ASK_ZONES = set()      # zone names where the human chat is still asked before an edit; the human emptied it
# FEATURE values that are not feature folders.
ROLES = ("@human", "@Core", "@Manager", "@CodeLead", "@GameDesigner", "@DesignAdvisor", "@Codex", "@Blender",
         "@ClaudeImage")
# Tools: they take orders from the features and make images (Codex; ClaudeImage draws them itself as SVG) or 3D
# models (Blender); they never code.
TOOL_ROLES = {"@Codex": "codex", "@Blender": "blender", "@ClaudeImage": "claudeimage"}
ART_ORDER_TOOLS = ("codex", "claudeimage")      # whose orders/_design/ @GameDesigner writes art orders into
STUDIO_ZONE, RESOURCES_ZONE = "Management/studio", "Resources"
# What studio.py alone writes in an order folder; the one who orders writes order.md, once.
STUDIO_SCRIPT_FILES = {"status.json", "log.jsonl", ".run.lock", "review.json", "anchors.json"}
STUDIO_FEATURE_FILE_RX = re.compile(r"^order\.md$")
STUDIO_ID = r"[a-z0-9]+(-[a-z0-9]+)*"
# An order's owner: a feature folder, or a design node (art concepts, ordered by @GameDesigner: _design/<a.b.c>).
DESIGN_OWNER_RX = re.compile(r"^_design/[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)*$")
STUDIO_REF = r"((_Services/)?[A-Za-z][A-Za-z0-9]*|_design/[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)*)/" + STUDIO_ID
# The studio (orders to the tool chats; see studio.py). Reading it: any chat. Running a tool: that tool's chat.
STUDIO = PY + r"Management/tools/studio\.py "
ALLOWED_COMMANDS.append(STUDIO + r"measure " + STUDIO_REF + "( " + STUDIO_ID + ")?")
ALLOWED_COMMANDS.append(STUDIO + r"(check|status( (codex|blender|claudeimage|" + STUDIO_REF + "))?)")
# A Leader lets @ClaudeImage draw one frame of a delivered claude concept again (one run, with the reason in double
# quotes). Drawing after a delivery is never the tool chat's own decision; the human runs it too.
LEADER_REDO = (STUDIO + r"redo _design/[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)*/" + STUDIO_ID + " " + STUDIO_ID
               + r' "[^"\\]+"')
for _commands in (DESIGNER_COMMANDS, CODELEAD_COMMANDS, MANAGER_COMMANDS):
    _commands.append(LEADER_REDO)
TOOL_COMMANDS = {
    # @ClaudeImage writes work/<item>.svg itself; `claude` checks it and makes the PNG (no outside tool is run).
    "claudeimage": [STUDIO + r"claude " + STUDIO_REF + " " + STUDIO_ID, STUDIO + r"deliver claudeimage " + STUDIO_REF,
                    STUDIO + r"pick " + STUDIO_REF + " " + STUDIO_ID + r" [1-9][0-9]*"],
    "codex": [STUDIO + r"(codex|consult) " + STUDIO_REF + " " + STUDIO_ID, STUDIO + r"deliver codex " + STUDIO_REF,
              STUDIO + r"pick " + STUDIO_REF + " " + STUDIO_ID + r" [1-9][0-9]*"],
    "blender": [STUDIO + r"blender " + STUDIO_REF + " " + STUDIO_ID, STUDIO + r"deliver blender " + STUDIO_REF,
                STUDIO + r"pick " + STUDIO_REF + " " + STUDIO_ID + r" [1-9][0-9]*"],
}
# The one who ordered judges one delivered item: a level 0-3 and an optional feedback in double quotes (no quote
# and no backslash inside; SHELL_META_RX refuses the shell characters). Only the feature that ordered runs it.
FEATURE_STUDIO_RX = re.compile(r"^python3? Management/tools/studio\.py (review) (" + STUDIO_REF + r") " + STUDIO_ID
                               + r' [0-3]( "[^"\\]*")?$')
ROLE_AREAS = {"@Core": "Core"}
# The wiring (Boot, scenes, asset catalogs): @CodeLead edits all of it, not only its skeleton; no code gate.
LEAD_AREA = "_Composition"
# unity-mcp: every tool and action, by what it touches (kind). A tool or action missing here is denied.
#   read    looks only; any path it names must be readable by the session           every code session, @CodeLead
#   write   changes asset files; every path it names must be the session's own       the owner (@CodeLead: _Composition; delete: also skeleton files)
#   shared  compiles; touches the one Editor but holds nothing                       every code session, @CodeLead; not while
#                                                                                    another session holds the Editor lock
#   state   changes the open scene, a prefab stage or the console                    every code session, @CodeLead; takes the lock
#   play    play mode and tests                                                      @CodeLead; takes the lock
#   lead    project-wide settings, packages, menus, cameras, graphics...             @CodeLead; takes the lock
#   script  writes code: code is written with Edit/Write only                        nobody (@human passes)
# "Code session": Basic, Features, Services. @human passes everything; @Manager, @GameDesigner and an unset chat get nothing.
UNITY_PREFIX = "mcp__unityMCP__"
BLENDER_PREFIX = "mcp__blender__"
TOOL_UNITY = {"execute_custom_tool", "execute_menu_item"}      # the indirect way of a tool chat (soft gate), with the lock
UNITY_TOOL_ROLES = ("@Codex", "@Blender")      # the tool chats that have an outside tool to run that way
UNITY_ASSET_TOOL = UNITY_PREFIX + "manage_asset"
UNITY_SERVER = "unityMCP"
UNITY_RESOURCE_TOOLS = {"ReadMcpResourceTool", "ListMcpResourcesTool", "ReadMcpResourceDirTool"}
ANY = "*"
_READ_ONLY = ("find_gameobjects", "get_test_job", "unity_docs", "unity_reflect", "manage_script_capabilities",
              "debug_request_context")
_LEAD_TOOLS = ("execute_menu_item", "execute_custom_tool", "set_active_instance", "manage_packages", "manage_camera",
               "manage_graphics", "manage_physics", "manage_probuilder", "manage_profiler")
_SCRIPT_TOOLS = ("create_script", "script_apply_edits", "apply_text_edits", "delete_script", "manage_script")
UNITY_RULES = {
    **{t: {ANY: "read"} for t in _READ_ONLY},
    **{t: {ANY: "read"} for t in ("find_in_file", "get_sha", "validate_script")},      # need a readable uri
    **{t: {ANY: "lead"} for t in _LEAD_TOOLS},
    **{t: {ANY: "script"} for t in _SCRIPT_TOOLS},
    "read_console": {"": "read", "get": "read", "clear": "state"},
    "refresh_unity": {ANY: "shared"},
    "run_tests": {ANY: "play"},
    "manage_scene": {"get_hierarchy": "read", "get_active": "read", "get_loaded_scenes": "read",
                     "get_build_settings": "read", "create": "state", "load": "state", "save": "state",
                     "scene_view_frame": "lead"},
    "manage_asset": {"search": "read", "get_info": "read", "get_components": "read", "create": "write",
                     "create_folder": "write", "duplicate": "write", "move": "write", "rename": "write",
                     "modify": "write", "import": "write", "delete": "write"},
    "manage_prefabs": {"get_info": "read", "get_hierarchy": "read", "modify_contents": "write",
                       "create_from_gameobject": "state"},
    "manage_material": {"get_material_info": "read", "create": "write", "set_material_shader_property": "write",
                        "set_material_color": "write", "assign_material_to_renderer": "state", "set_renderer_color": "state"},
    "manage_scriptable_object": {"create": "write", "modify": "write"},      # config and installer assets of a feature
    "manage_texture": {"create": "write", "apply_pattern": "write", "apply_gradient": "write"},
    "manage_ui": {"read": "read", "get_visual_tree": "read", "create": "write", "update": "write",
                  "create_panel_settings": "write", "attach_ui_document": "state"},
    "manage_gameobject": {ANY: "state"},
    "manage_components": {ANY: "state"},
    "manage_editor": {"play": "play", "pause": "play", "stop": "play", "open_prefab_stage": "state",
                      "save_prefab_stage": "state", "close_prefab_stage": "state", "add_tag": "lead", "remove_tag": "lead",
                      "add_layer": "lead", "remove_layer": "lead", "set_active_tool": "lead", "deploy_package": "lead",
                      "restore_package": "lead", "telemetry_status": "lead", "telemetry_ping": "lead"},
}
UNITY_CHANGE_KINDS = {"write", "state", "lead"}      # what stop_report counts as a change that needs a report
# Parameters that name an asset; each one present is checked. The pairs below must be present.
UNITY_PATH_KEYS = ("path", "destination", "prefab_path", "material_path", "source_asset", "panel_settings", "uri")
UNITY_NEEDS_PATH = {("manage_asset", ANY): "path", ("manage_prefabs", "get_info"): "prefab_path",
                    ("manage_prefabs", "get_hierarchy"): "prefab_path", ("manage_prefabs", "modify_contents"): "prefab_path",
                    ("manage_prefabs", "create_from_gameobject"): "prefab_path",
                    ("manage_material", "get_material_info"): "material_path", ("manage_material", "create"): "material_path",
                    ("manage_material", "set_material_shader_property"): "material_path",
                    ("manage_material", "set_material_color"): "material_path",
                    ("manage_texture", ANY): "path", ("manage_ui", "read"): "path", ("manage_ui", "create"): "path",
                    ("manage_ui", "update"): "path", ("manage_ui", "create_panel_settings"): "path",
                    ("find_in_file", ANY): "uri", ("get_sha", ANY): "uri", ("validate_script", ANY): "uri",
                    ("manage_scene", "load"): "path", ("manage_scene", "create"): "path",
                    ("manage_editor", "open_prefab_stage"): "prefab_path"}
UNITY_CODE_EXT = (".cs", ".asmdef", ".asmref")      # code and assembly files are written with Edit/Write only
URI_PREFIX = "mcpforunity://path/"
BATCH_MAX = 25
# The Editor lock: one session at a time changes the shared Editor (scene, prefab stage, play, tests, settings).
LOCK_FILE = "editor-lock.json"
LOCK_SECONDS = 600
FEATURE_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
SHELL_META_RX = re.compile(r"[;&|<>`$(){}\r\n]")
# Per-chat bindings and the PIN. Outside the project, so no agent session can read or write it.
STORE_DIR = os.path.join(os.path.expanduser("~"), ".coresimple-guard")
SESSION_RX = re.compile(r"^[A-Za-z0-9_-]{1,128}$")

# --- the file system this hook runs on
WINDOWS = os.name == "nt"
CASE = re.I if WINDOWS else 0                    # regex flag for zones that grant something
DESIGNER_RULES_RX = re.compile(r"^designadvisor\.md$|^gamedesigner(\.round[1-4])?\.md$", CASE)
ADVISOR_RULES_RX = re.compile(r"^(designadvisor|gamedesigner(\.round[1-4])?)\.md$", CASE)
# What @DesignAdvisor writes below advice/<node>/: its question and its own note. The answer is gd_advise.py's.
ADVICE_FILE_RX = re.compile(r"^" + ADVICE_ID + r"\.(q|note)\.md$")
ADVICE_NODE_RX = re.compile(r"^" + ADVICE_NODE + r"$")
# The skeleton: the public gates and base classes of the code, edited by @CodeLead only.
SKELETON_DIRS_RX = re.compile(r"^Scripts/(Contracts|Internal/Base|View/Base)(/|$)", CASE)
# An interface file (I<Name>.cs) or a base class file (<Name>Base.cs) of Core / _Composition.
SKELETON_INTERFACE_RX = re.compile(r"^(I[A-Z][A-Za-z0-9]*|[A-Z][A-Za-z0-9]*Base)\.cs$")
SKELETON_ASSEMBLY_INFO = "assemblyinfo.cs"
AREA_FOLDERS = ("_Composition", "Core")
SKELETON_MSG = ("{name}: this file belongs to the skeleton (Contracts, *.asmdef, AssemblyInfo.cs, Scripts/Internal/Base, Scripts/View/Base; "
                "in Core/_Composition also the I*.cs interfaces and the *Base.cs base classes): only @CodeLead edits it. Write your code in a subclass in "
                "the rest of Internal/View. To change the skeleton, send @CodeLead the context, the file and your proposal.")
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
    feature: the session's own feature; area: "Core" for @Core, "_Composition" for @CodeLead;
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
    if low[0] == "resources":      # deliveries of the tool chats; only studio.py writes there
        return RESOURCES_ZONE, same(seg[0], "Resources"), "deny"
    if low[0] == "management":
        if not same(seg[0], "Management"):
            return "Management", True, "deny"
        if n >= 2 and low[1] == "proposals":
            if exact(seg[:2], "Management/proposals"):
                return "Management/proposals", True, "create"
            return "Management", True, "deny"
        if n >= 2 and low[1] == "studio":
            if exact(seg[:2], STUDIO_ZONE):
                return STUDIO_ZONE, True, "studio"
            return "Management", True, "deny"
        if n >= 2 and low[1] == "game-design":
            if n >= 3 and low[2] == "ai" and exact(seg[:3], DESIGN_AI):
                return DESIGN_AI, True, "design"
            if n >= 3 and low[2] == "live":
                return DESIGN_LIVE, True, "deny"
            if n >= 3 and low[2] == "advice" and exact(seg[:3], DESIGN_ADVICE):
                return DESIGN_ADVICE, True, "advice"
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

    # Only @Core edits Core and only @CodeLead edits _Composition; a feature session asks them. The skeleton
    # (asmdef, AssemblyInfo, interface files) is @CodeLead's; the zone keeps the area's name, so the human is still asked.
    for folder in AREA_FOLDERS:
        if low[2] == folder.lower():
            if same(seg[2], folder) and area_skeleton(seg[3:]):
                return folder, True, "skeleton"
            if area == folder and same(seg[2], folder):
                return "own " + folder, True, "allow"
            return folder, True, "deny"
    if not same(seg[2], "Features"):
        return "unlisted", False, "deny"
    if n < 4:      # the folder of all features: only @CodeLead searches across them (it reads every layer anyway)
        return "Features", read_all, "deny"

    name, i = seg[3], 4
    if low[3] == "_services":
        if n < 5:
            return "Features/_Services", read_all and same(seg[3], "_Services"), "deny"
        name, i = seg[3] + "/" + seg[4], 5      # keeps the spelling on disk; same() decides if it is ours
    rest = "/".join(seg[i:])
    own = bool(feature) and same(name, feature)
    # The CLAUDE.md at a feature's root describes it to the others: readable by all, like its Contracts.
    public = bool(re.match(r"^(Scripts/Contracts|FeatureAssets/Public)(/|$)", rest, CASE)) or same(rest, "CLAUDE.md")

    # The skeleton (Contracts, asmdef, AssemblyInfo, Scripts/*/Base) is @CodeLead's, even in the session's own feature.
    if rest and feature_skeleton(rest):
        return f"skeleton of feature {name}", own or public or read_all, "skeleton"
    if own:
        return "own feature", True, "allow"
    if public:
        return f"public part of feature {name}", True, "deny"
    return f"private part of feature {name}", read_all, "deny"


def is_skeleton_file(name):
    """asmdef and AssemblyInfo.cs, in any letter case: they draw the assembly boundary."""
    low = name.lower()
    return low.endswith(".asmdef") or low == SKELETON_ASSEMBLY_INFO


def feature_skeleton(rest):
    """True for the part of a feature (path below its folder, '/'-separated) that only @CodeLead edits."""
    return bool(SKELETON_DIRS_RX.match(rest)) or is_skeleton_file(rest.split("/")[-1])


def area_skeleton(sub):
    """True for the part of Core / _Composition (segments below it) that only @CodeLead edits: asmdef, AssemblyInfo
    the interface files (I<Name>.cs) and the base class files (<Name>Base.cs), tests excepted."""
    if not sub:
        return False
    if is_skeleton_file(sub[-1]):
        return True
    return bool(SKELETON_INTERFACE_RX.match(sub[-1])) and "tests" not in [s.lower() for s in sub[:-1]]


def feature_of(full, root):
    """The feature id ('Wallet', '_Services/Popup') a canonical path lies in, spelled as on disk; None outside
    Assets/Features."""
    if not inside(full, root):
        return None
    seg = rel_segments(full, root)
    if len(seg) < 4 or not exact(seg[:3], "UnityProject/Assets/Features"):
        return None
    if seg[3].lower() == "_services":
        return seg[3] + "/" + seg[4] if len(seg) >= 5 else None
    return seg[3]


def text_of(value):
    return value if isinstance(value, str) else ""


def is_asmref(full):
    return full.lower().endswith(".asmref")


def decide_unity_delete(ti, mode, feature, root, area=None, who=""):
    """Deleting through unity-mcp: one existing file inside the session's own feature or area, outside the skeleton;
    for @CodeLead, one existing skeleton file (of an unlocked feature, or of Core) or a file of _Composition."""
    if text_of(ti.get("action")) != "delete":
        return "deny", "unity-mcp manage_asset: a feature session may use only action 'delete'"
    if mode == "none":
        return "deny", "FEATURE is not set or is not an existing feature folder; deleting is blocked"
    if mode in ("manager", "gamedesigner", "advisor"):
        return "deny", f"{who} does not change the project; ask the session that owns the file"
    path = text_of(ti.get("path")).replace("\\", "/")      # a Unity asset path: '/' on every OS
    if not re.match(r"^Assets/[^/]", path):
        return "deny", "unity-mcp delete: write the path from the Unity project root, starting with 'Assets/'"
    full = resolve_target("UnityProject/" + path, root)
    if not inside(full, under(root, "UnityProject", "Assets")):
        return "deny", "unity-mcp delete: the path leaves UnityProject/Assets"
    if goes_through_link(full, root):
        return "deny", "path goes through a link or junction"
    if mode == "codelead":
        name, _, edit = zone(full, root, None, LEAD_AREA, True)
        if edit not in ("skeleton", "allow"):
            return "deny", (f"@CodeLead deletes only skeleton files and files of {LEAD_AREA}, not in: {name}. "
                            "Ask the session that owns it.")
        locked = codelead_gate(full, root)
        if locked:
            return "deny", locked
    else:
        name, _, edit = zone(full, root, feature, area)
        if edit == "skeleton":
            return "deny", SKELETON_MSG.format(name=name)
        if edit != "allow":
            return "deny", f"FEATURE={who} may not delete in: {name}"
    if is_asmref(full):
        return "deny", ".asmref files are forbidden"
    if mode == "feature":
        locked = design_gate(root, feature)
        if locked:
            return "deny", locked
    if not os.path.isfile(full):
        # @CodeLead owns the skeleton folders and everything below _Composition: it deletes a folder there itself.
        if mode == "codelead" and os.path.isdir(full) and not same(full, under(root, "UnityProject", "Assets", LEAD_AREA)):
            return "allow", f"@CodeLead deletes a folder of its own inside {name}"
        return "ask", "deleting a folder (or a path that is not a file) needs human approval"
    return "allow", f"delete inside {name}"


def gamedesign_module(root):
    tools = os.path.join(root, "Management", "tools")
    if tools not in sys.path:
        sys.path.insert(0, tools)
    import gamedesign
    return gamedesign


def decide_studio_edit(tool, full, root, mode, feature, tool_name):
    """Edits inside Management/studio. The tool chat: its own lessons.md and the work folder of its orders.
    A feature (and @GameDesigner for an art order): creates the order.md of its own order, once. studio.py and the
    page write the rest (the judgement of a delivery too: studio.py review)."""
    seg = rel_segments(full, under(root, "Management", "studio"))
    if mode == "tool":
        if len(seg) == 2 and seg[0] == tool_name and seg[1] == "lessons.md":
            return "allow", f"lessons of the {tool_name} chat"
        if len(seg) >= 5 and seg[0] == tool_name and seg[1] == "orders" and "work" in seg[3:-1]:
            at = seg.index("work", 3)
            owner = "/".join(seg[2:at - 1])
            if at >= 4 and (FEATURE_RX.match(owner) or DESIGN_OWNER_RX.match(owner)) and re.fullmatch(STUDIO_ID, seg[at - 1]):
                return "allow", "the work folder of an order"
        return "deny", (f"in Management/studio a tool chat writes only {tool_name}/lessons.md and "
                        f"{tool_name}/orders/<F>/<id>/work/**; order.md, review.json, status.json, log.jsonl and refs are "
                        "not its to write")
    if mode == "feature" and len(seg) in (5, 6) and seg[0] in TOOL_ROLES.values() and seg[1] == "orders":
        ordered, oid, name = "/".join(seg[2:-2]), seg[-2], seg[-1]
        if not same(ordered, feature):
            return "deny", f"FEATURE={feature} orders only for itself (orders/{feature}/<id>/)"
        if not re.fullmatch(STUDIO_ID, oid) or not STUDIO_FEATURE_FILE_RX.match(name):
            return "deny", ("a feature writes only order.md in an order folder (id: lower case, digits, '-'); it judges "
                            "a delivery with: python Management/tools/studio.py review <F>/<id> <item> <0-3> \"<feedback>\"")
        if tool != "Write" or os.path.lexists(full):
            return "deny", "order.md is written once: a change is a new order"
        locked = design_gate(root, feature)
        if locked:
            return "deny", locked
        return "allow", f"new {name} for {seg[0]}"
    if mode == "gamedesigner" and len(seg) == 6 and seg[0] in ART_ORDER_TOOLS and seg[1:3] == ["orders", "_design"]:
        owner, oid, name = "/".join(seg[2:4]), seg[4], seg[5]
        if not DESIGN_OWNER_RX.match(owner) or not re.fullmatch(STUDIO_ID, oid) or not STUDIO_FEATURE_FILE_RX.match(name):
            return "deny", ("an art order lives in <codex|claudeimage>/orders/_design/<node path with '.' for '/'>/<id>/ and "
                            "holds only order.md from you; the human judges the pictures on the management page")
        if tool != "Write" or os.path.lexists(full):
            return "deny", "order.md is written once: a change is a new order"
        return "allow", f"new {name} of an art order"
    return "deny", ("Management/studio: the tool chats write their lessons and work, the features their orders, "
                    "@GameDesigner the art orders (codex/orders/_design, claudeimage/orders/_design), studio.py and "
                    "the page the rest")


def decide_advice_edit(full, root):
    """@DesignAdvisor writing inside Management/game-design/advice: only <node>/<id>.q.md (the question it asks GPT)
    and <node>/<id>.note.md (its own remarks). <node> is a node path with '.' for '/', _root or _general."""
    seg = rel_segments(full, root)[3:]      # below Management/game-design/advice
    if len(seg) == 2 and ADVICE_NODE_RX.match(seg[0]) and ADVICE_FILE_RX.match(seg[1]):
        return "allow", "a question or a note of @DesignAdvisor"
    return "deny", ("in Management/game-design/advice you write only <node>/<id>.q.md and <node>/<id>.note.md (<node>: "
                    "the node path with '.' for '/', _root for the round-1 node, _general for no node; id: lower case, "
                    "digits, '-'). The answer <id>.md is written by: python Management/tools/gd_advise.py <node> <id>")


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


def codelead_gate(full, root):
    """None when @CodeLead may change this skeleton file now, otherwise why not: a feature's skeleton follows the same
    code gate as the feature (its design reached round 4 in live); Core and _Composition have no gate."""
    fid = feature_of(full, root)
    if not fid:
        return None
    ok, reason = gamedesign_module(root).code_allowed(root, fid)
    return None if ok else (f"the skeleton of '{fid}' follows its code gate, which is locked: {reason}. Stop and tell "
                            "the human; the design is @GameDesigner's work and the human fixes it into live.")


# ---------------------------------------------------------------- unity-mcp
def lock_path(store):
    return os.path.join(store or STORE_DIR, LOCK_FILE)


def read_lock(store):
    """The Editor lock as stored, or None when nobody holds it (missing, broken or expired)."""
    try:
        with open(lock_path(store), encoding="utf-8") as f:
            lock = json.load(f)
    except (OSError, ValueError):
        return None
    if not isinstance(lock, dict) or not isinstance(lock.get("until"), (int, float)) or lock["until"] < time.time():
        return None
    return lock


def write_lock(store, lock):
    path = lock_path(store)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + f".{os.getpid()}.tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(lock, f)
    os.replace(tmp, path)


def release_lock(store, session=None):
    """Drops the Editor lock: the one this session holds, or any lock when session is None. True when dropped."""
    lock = read_lock(store)
    if lock is None or (session is not None and lock.get("session") != session):
        return False
    try:
        os.remove(lock_path(store))
    except OSError:
        return False
    return True


def unity_kind(tool, ti):
    """(short tool name, action, kind) of a unity-mcp call; kind None when the guard does not know it."""
    short = tool[len(UNITY_PREFIX):] if tool.startswith(UNITY_PREFIX) else tool
    action = text_of(ti.get("action"))
    actions = UNITY_RULES.get(short)
    if actions is None:
        return short, action, None
    return short, action, actions.get(action, actions.get(ANY))


def unity_commands(tool, ti):
    """[(tool, params)] a unity-mcp call runs: itself, or the commands of a batch_execute."""
    if tool != UNITY_PREFIX + "batch_execute":
        return [(tool, ti)]
    cmds = ti.get("commands")
    if not isinstance(cmds, list):
        raise ValueError("batch_execute without a list of commands")
    out = []
    for c in cmds:
        name = text_of(c.get("tool")) if isinstance(c, dict) else ""
        params = c.get("params") if isinstance(c, dict) else None
        out.append((name if name.startswith(UNITY_PREFIX) else UNITY_PREFIX + name, params if isinstance(params, dict) else {}))
    return out


def unity_changes(tool, ti):
    """True when a unity-mcp call changes something (write, state or lead), so the turn needs a report."""
    try:
        return any(unity_kind(t, p)[2] in UNITY_CHANGE_KINDS for t, p in unity_commands(tool, ti))
    except ValueError:
        return False


def unity_full(value, root, allow_assets_root=False):
    """(canonical path, None) of a Unity asset path ('Assets/...', or a mcpforunity://path/ uri), or (None, reason)."""
    path = text_of(value).replace("\\", "/")
    if path.startswith(URI_PREFIX):
        path = path[len(URI_PREFIX):]
    if not (re.match(r"^Assets/[^/]", path) or (allow_assets_root and path.rstrip("/") == "Assets")):
        return None, f"unity-mcp: write '{path}' from the Unity project root, starting with 'Assets/'"
    full = resolve_target("UnityProject/" + path, root)
    if not (inside(full, under(root, "UnityProject", "Assets")) or same(full, under(root, "UnityProject", "Assets"))):
        return None, "unity-mcp: the path leaves UnityProject/Assets"
    if goes_through_link(full, root):
        return None, "path goes through a link or junction"
    return full, None


def unity_paths(short, action, ti):
    """[(key, value)] of the asset paths a call names; raises ValueError when a needed one is missing."""
    if short == "manage_scriptable_object":      # names its asset in its own way: folder + name, or target {path}
        if action == "create":
            folder, name = text_of(ti.get("folder_path")).replace("\\", "/").rstrip("/"), text_of(ti.get("asset_name"))
            if not folder or not re.match(r"^[A-Za-z0-9_. -]+$", name) or ".." in name:
                raise ValueError("unity-mcp manage_scriptable_object create: give 'folder_path' (starting with 'Assets/') "
                                 "and 'asset_name' (letters, digits, _ . - space)")
            return [("folder_path", folder + "/" + name + ".asset")]
        target = ti.get("target")
        path = text_of(target.get("path")) if isinstance(target, dict) else ""
        if not path or len(target) != 1:      # a guid cannot be checked against the session's area
            raise ValueError("unity-mcp manage_scriptable_object modify: give 'target' as {\"path\": \"Assets/...\"} only, "
                             "not a guid")
        return [("target", path)]
    need = UNITY_NEEDS_PATH.get((short, action)) or UNITY_NEEDS_PATH.get((short, ANY))
    if need and not text_of(ti.get(need)):
        raise ValueError(f"unity-mcp {short} {action}: give '{need}' (an asset path starting with 'Assets/')")
    return [(k, ti.get(k)) for k in UNITY_PATH_KEYS if text_of(ti.get(k))]


def unity_own(full, root, mode, feature, area, who):
    """None when the session may change the asset at `full` (its own area, not the skeleton, past its code gate),
    otherwise the reason it may not."""
    name, _, edit = zone(full, root, feature, area)
    if edit == "skeleton":
        return SKELETON_MSG.format(name=name)
    if edit != "allow":
        return f"FEATURE={who} may not change assets in: {name}"
    if full.lower().endswith(UNITY_CODE_EXT):
        return "unity-mcp does not write code or assembly files: use Edit/Write (the guard checks them there)"
    if mode == "feature":
        return design_gate(root, feature)
    return None


def decide_unity_one(tool, ti, ctx, work):
    """(decision, reason, needs lock) of one unity-mcp command. work: the lock this session would hold after the
    commands before it ({scene, prefab}); updated in place."""
    mode, feature, area, who, root = ctx["mode"], ctx["feature"], ctx["area"], ctx["who"], ctx["root"]
    short, action, kind = unity_kind(tool, ti)
    lead = mode == "codelead"
    if kind is None:
        return "deny", f"unity-mcp {short} {action or ''}: not on the guard's list (guard.py UNITY_RULES); ask the human", False
    if kind == "script":
        return "deny", f"unity-mcp {short}: code is written with Edit/Write only, where the guard checks it", False
    if mode == "tool":
        if kind == "read" or (kind == "lead" and short in TOOL_UNITY and who in UNITY_TOOL_ROLES):
            pass      # reads, and the indirect way to run Codex or Blender (it takes the Editor lock below)
        elif kind == "lead" and short in TOOL_UNITY:
            return "deny", f"unity-mcp {short}: {who} draws its pictures itself and runs no outside tool; it only reads", False
        else:
            return "deny", (f"unity-mcp {short} {action}: a tool chat only reads and, when its tool cannot run, "
                            f"calls {' or '.join(sorted(TOOL_UNITY))}"), False
    elif kind in ("play", "lead") and not lead:
        return "deny", f"unity-mcp {short} {action}: only @CodeLead (play, tests, project settings); ask @CodeLead", False
    paths = unity_paths(short, action, ti)
    if kind == "read":
        for key, value in paths:
            full, why = unity_full(value, root, allow_assets_root=lead)
            if why:
                return "deny", why, False
            name, readable, _ = zone(full, root, feature, area, ctx["read_all"])
            if lead and same(full, under(root, "UnityProject", "Assets")):
                readable = True      # @CodeLead may search the whole project
            if not readable:
                return "deny", f"unity-mcp {short}: may not read {key} in: {name}", False
        return "allow", f"unity-mcp {short} {action}: reads", False
    if kind == "shared":
        return "allow", f"unity-mcp {short}: compiles (not while another session holds the Editor)", False
    if short == "manage_asset" and action == "delete":
        return (*decide_unity_delete(ti, mode, feature, root, area, who), False)
    if kind == "write":      # @CodeLead: its area is _Composition, so everything else is refused below
        for key, value in paths:
            full, why = unity_full(value, root)
            if why:
                return "deny", why, False
            why = unity_own(full, root, mode, feature, area, who)
            if why:
                return "deny", f"unity-mcp {short} {action} ({key}): {why}", False
        return "allow", f"unity-mcp {short} {action}: inside {who}'s area", False
    # state, play, lead: the Editor lock, plus what the scene or prefab stage may be
    for key, value in paths:
        full, why = unity_full(value, root)
        if why:
            return "deny", why, False
        opens = (short, action) in (("manage_scene", "load"), ("manage_editor", "open_prefab_stage"))
        if lead and opens:
            name, readable, _ = zone(full, root, None, None, True)
            if not readable:
                return "deny", f"unity-mcp {short}: may not open {name}", False
        else:
            why = unity_own(full, root, mode, feature, area, who)
            if why:
                return "deny", f"unity-mcp {short} {action} ({key}): {why}", False
        if short == "manage_scene" and action in ("load", "save") and key == "path":
            work["scene"] = text_of(value)
        if short == "manage_scene" and action == "create" and key == "path":      # path is the folder, name the scene
            name = text_of(ti.get("name"))
            if not re.match(r"^[A-Za-z0-9_. -]+$", name) or ".." in name:
                return "deny", "unity-mcp manage_scene create: give the scene 'name' (letters, digits, _ . - space)", False
            work["scene"] = text_of(value).replace("\\", "/").rstrip("/") + "/" + name + ".unity"
        if short == "manage_editor" and action == "open_prefab_stage":
            work["prefab"] = text_of(value)
    if (short, action) in (("manage_scene", "save"), ("manage_editor", "save_prefab_stage")):
        held = work.get("scene" if short == "manage_scene" else "prefab")
        if not held:
            return "deny", (f"unity-mcp {short} {action}: open the {'scene' if short == 'manage_scene' else 'prefab'} "
                            "through unity-mcp first (load/create, open_prefab_stage), so the guard knows what is saved"), True
        full, why = unity_full(held, root)
        why = why or unity_own(full, root, mode, feature, area, who)
        if why:
            return "deny", f"unity-mcp {short} {action}: the open one is '{held}': {why}", True
    if short == "manage_editor" and action == "close_prefab_stage":
        work["prefab"] = ""
    if kind == "state" and not lead and short in ("manage_gameobject", "manage_components", "manage_material", "manage_ui",
                                                     "manage_prefabs") and not (work.get("scene") or work.get("prefab")):
        return "deny", (f"unity-mcp {short} {action}: changes the open scene; load or create a scene of your own area "
                        "first (manage_scene), or edit the prefab headless (manage_prefabs modify_contents)"), True
    return "allow", f"unity-mcp {short} {action}: holds the Editor", True


def decide_unity(tool, ti, mode, feature, area, who, root, session, store):
    """unity-mcp for every session but @human: see UNITY_RULES. The Editor lock is taken only when every command
    of the call is allowed."""
    if mode in ("none", "manager", "gamedesigner", "advisor"):
        return "deny", (f"{who or 'this chat'} does not use unity-mcp" if mode != "none"
                        else "FEATURE is not set or is not an existing feature folder; unity-mcp is blocked")
    if tool in UNITY_RESOURCE_TOOLS:
        if text_of(ti.get("server")) not in ("", UNITY_SERVER):
            return "deny", f"tool '{tool}' is allowed only for the {UNITY_SERVER} server"
        return "allow", "reads a unity-mcp resource"
    cmds = unity_commands(tool, ti)
    if not cmds or len(cmds) > BATCH_MAX:
        return "deny", f"batch_execute takes 1 to {BATCH_MAX} commands"
    session = session if isinstance(session, str) else ""
    held = read_lock(store)
    other = held if held and held.get("session") != session else None
    mine = held if held and not other else {}
    work = {"scene": text_of(mine.get("scene")), "prefab": text_of(mine.get("prefab"))}
    ctx = {"mode": mode, "feature": feature, "area": area, "who": who, "root": root, "read_all": mode == "codelead"}
    decisions, reasons, needs_lock, shared = [], [], False, False
    for t, p in cmds:
        if t == UNITY_PREFIX + "batch_execute":
            return "deny", "batch_execute may not hold another batch_execute"
        d, why, lock = decide_unity_one(t, p, ctx, work)
        decisions.append(d)
        reasons.append(why)
        needs_lock = needs_lock or lock
        shared = shared or unity_kind(t, p)[2] == "shared"
    if "deny" in decisions:
        return "deny", reasons[decisions.index("deny")]
    if (needs_lock or shared) and other:
        until = time.strftime("%H:%M", time.localtime(other["until"]))
        return "deny", (f"the Unity Editor is held by {other.get('who') or 'another chat'} until {until} (scene, play, "
                        "tests or settings); wait until that chat is done. The human can free it: "
                        "python .claude/hooks/stop_unlock.py --release")
    if needs_lock:
        write_lock(store, {"session": session, "who": who, "until": time.time() + LOCK_SECONDS, **work})
    if "ask" in decisions:
        return "ask", reasons[decisions.index("ask")]
    return "allow", reasons[0] if len(reasons) == 1 else f"batch of {len(reasons)} unity-mcp commands"


def edit_texts(ti):
    """Every piece of text an edit tool would write."""
    texts = [text_of(ti.get("content")), text_of(ti.get("new_string")), text_of(ti.get("new_source"))]
    edits = ti.get("edits")
    if isinstance(edits, list):
        texts += [text_of(e.get("new_string")) for e in edits if isinstance(e, dict)]
    return texts


def decide(data, session_value, root, store=None):
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
        mode, area = "codelead", LEAD_AREA
    elif role == "@GameDesigner":
        mode = "gamedesigner"
    elif role == "@DesignAdvisor":
        mode = "advisor"
    elif role in ROLE_AREAS:
        mode, area = "area", ROLE_AREAS[role]
    elif role in TOOL_ROLES:
        mode = "tool"
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
            ADVISOR_COMMANDS if mode == "advisor" else []) + (
            CODELEAD_COMMANDS if mode == "codelead" else []) + (MANAGER_COMMANDS if mode == "manager" else []) + (
            TOOL_COMMANDS[TOOL_ROLES[role]] if mode == "tool" else [])
        m = FEATURE_STUDIO_RX.match(cmd)
        # review: only the feature that ordered. The feedback text may hold "..", the part before it may not.
        if m and not SHELL_META_RX.search(cmd) and ".." not in cmd.split(' "')[0]:
            ordered = m.group(2).rpartition("/")[0]
            if mode == "feature" and ordered == feature:
                return "allow", f"studio: {m.group(1)} an order of {feature}"
            if ordered.startswith("_design/"):
                return "deny", "art concepts are judged by the human on the management page (tab Game design)"
            return "deny", f"only the chat of {ordered} judges its own orders"
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
        # Files that define the boundary itself.
        if is_asmref(full):
            return "deny", ".asmref files are forbidden: they put code into another assembly"
        if mode == "codelead":
            name, _, edit = zone(full, root, None, LEAD_AREA, True)
            if edit == "allow":      # Assets/_Composition, outside its skeleton
                if any("InternalsVisibleTo" in s for s in edit_texts(ti)):
                    return "deny", "InternalsVisibleTo belongs in AssemblyInfo.cs only"
                return "allow", f"@CodeLead edits {name}"
            if edit != "skeleton":
                return "deny", (f"@CodeLead edits only the skeleton (Contracts, *.asmdef, AssemblyInfo.cs, Scripts/*/Base, "
                                f"I*.cs of Core) and Assets/{LEAD_AREA}, not {name}. Send the owning session exactly "
                                "what to fix (file, change, why). See .claude/rules/codelead.md.")
            locked = codelead_gate(full, root)
            if locked:
                return "deny", locked
            return "allow", f"@CodeLead edits {name}"
        name, _, edit = zone(full, root, feature, area)
        if edit == "studio":
            return decide_studio_edit(tool, full, root, mode, feature, TOOL_ROLES.get(role))
        if mode == "tool":
            return "deny", (f"{who} writes only inside Management/studio/{TOOL_ROLES[role]} (lessons.md, the work folder "
                            "of an order) and never codes; results reach the feature through studio.py deliver")
        if mode == "gamedesigner":
            if edit == "design":
                return decide_designer_edit(full, root, name)
            return "deny", ("@GameDesigner writes only inside Management/game-design/ai. live is fixed by the human "
                            "(the promote button of the management page). See .claude/rules/gamedesigner.md.")
        if mode == "advisor":
            if edit == "advice":
                return decide_advice_edit(full, root)
            return "deny", ("@DesignAdvisor writes only its questions and notes in Management/game-design/advice; it "
                            "never writes the design (ai is @GameDesigner's, live is the human's). "
                            "See .claude/rules/designadvisor.md.")
        if edit == "advice":
            return "deny", (f"FEATURE={who} may not edit {name}: only @DesignAdvisor writes there (questions and "
                            "notes); the answers are written by gd_advise.py.")
        if edit == "design":
            return "deny", (f"FEATURE={who} may not edit {name}: only @GameDesigner writes there. Ask @GameDesigner "
                            "(or the human) for a design change.")
        if mode == "manager" and edit != "create":
            return "deny", ("@Manager does not edit code, assets or settings; it may only create new files in "
                            "Management/proposals. Ask the session that owns the area.")
        if edit == "skeleton":
            return "deny", SKELETON_MSG.format(name=name)
        if edit == "allow" and any("InternalsVisibleTo" in s for s in edit_texts(ti)):
            return "deny", ("InternalsVisibleTo belongs in AssemblyInfo.cs, which is the skeleton: only @CodeLead "
                            "writes it. Ask @CodeLead.")
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
        if mode == "gamedesigner":      # sees the design tree, its own rules and the studio, nothing else
            readable = name in DESIGN_ZONES or name in READ_ALL_ZONES and readable or (
                same(posixpath.dirname(full), under(root, DESIGNER_RULES_DIR))
                and bool(DESIGNER_RULES_RX.match(posixpath.basename(full))))
        if mode == "advisor":      # the design tree, the studio, the root CLAUDE.md, its rules and the designer's
            readable = name in DESIGN_ZONES or name in READ_ALL_ZONES and readable or name == "root CLAUDE.md" or (
                same(posixpath.dirname(full), under(root, DESIGNER_RULES_DIR))
                and bool(ADVISOR_RULES_RX.match(posixpath.basename(full))))
        if readable:
            return "allow", f"read inside {name}"
        return "deny", f"may not read: {name}. Search and read only inside your own area or a readable zone."

    if tool.startswith(BLENDER_PREFIX):      # soft gate: the guard cannot read the Python it runs (.claude/rules/studio.md)
        if mode == "tool" and TOOL_ROLES[role] == "blender":
            return "allow", "@Blender uses blender MCP when headless Blender fails"
        return "deny", "blender MCP is @Blender's; order a model from @Blender (.claude/rules/studio.md)"

    if tool.startswith(UNITY_PREFIX) or tool in UNITY_RESOURCE_TOOLS:
        return decide_unity(tool, ti, mode, feature, area, who, root, data.get("session_id"), store)

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
            result = decide(data, session_feature(data.get("session_id"), store), root, store)
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

