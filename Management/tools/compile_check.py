#!/usr/bin/env python3
"""Compiles the project's C# assemblies outside the Unity Editor, as Unity would, to catch compile errors before code
reaches the shared Editor (người quyết 2026-11-27: soft gate; a draft may fail).

    python Management/tools/compile_check.py <Tên> [--draft] [--quiet]
    python Management/tools/compile_check.py --all [--quiet]

<Tên>: a feature (Wallet), a service (_Services/Popup), Core (@Core), _Composition. It checks every assembly in that
folder and every project assembly that depends on one of them (a broken contract shows where it is used). --draft
reads the feature's drafts (UnityProject/Drafts/<Tên>/, same paths as inside its folder; .draft.json "delete" lists
files the draft removes) over the files in Assets. --all: every project assembly.

How: the .asmdef / .asmref of Assets, Packages/, Library/PackageCache (read only: the one tool allowed to, người quyết
2026-11-27) and the Editor's built-in packages; references by name or GUID; Unity's own netstandard 2.1 and engine
module DLLs; the core UI package (UnityEngine.UI) referenced implicitly, as Unity 6 does; defines of the Editor, the
project's scripting define symbols and each asmdef's versionDefines; defineConstraints drop an assembly. Each assembly
is compiled with the Roslyn compiler of the dotnet SDK (csc), independent ones in parallel, and the result is cached in
~/.cache/coresimple-compile (keyed by its sources, references and defines): only what changed is compiled again.

One line per checked assembly: OK, LỖI (its own errors, listed as file:line: code: message, paths from the project
root), CHẶN (a dependency has errors, so it was not compiled), BỎ (its defineConstraints are not met in the Editor).
Exit: 0 every checked assembly OK; 1 a checked assembly has errors; 3 no errors of its own but blocked by a dependency
outside the checked set; 2 the tools are missing (no dotnet SDK, no Unity Editor of the project's version): not an
error of the code. UNITY_EDITOR_PATH (the Editor folder holding Data/) and DOTNET override where they are found.
"""
import concurrent.futures
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
UNITY_PROJECT = "UnityProject"
DRAFTS = "Drafts"
DRAFT_FILE = ".draft.json"
CACHE = Path(os.environ.get("CORESIMPLE_COMPILE_CACHE") or Path.home() / ".cache" / "coresimple-compile")
LANG_VERSION = "9.0"
IMPLICIT = ("UnityEngine.UI",)      # the core UI package: Unity 6 references it from every assembly with engine references
MAX_LINES = 20
SKIP_DIR_RX = re.compile(r"(~$|^\.)")
ERROR_RX = re.compile(r"^(?P<path>.+?)\((?P<line>\d+),(?P<col>\d+)\): error (?P<code>\w+): (?P<msg>.*)$")
OLDER = ["5_3", "5_4", "5_5", "5_6", "2017_1", "2017_2", "2017_3", "2017_4", "2018_1", "2018_2", "2018_3", "2018_4",
         "2019_1", "2019_2", "2019_3", "2019_4", "2020_1", "2020_2", "2020_3", "2021_1", "2021_2", "2021_3", "2022_1",
         "2022_2", "2022_3", "2023_1", "2023_2", "2023_3"]


class ToolMissing(Exception):
    """No dotnet SDK or no Unity Editor: the check cannot run (exit 2), which says nothing about the code."""


# ---------------------------------------------------------------- where things are
def unity_version(up):
    text = (up / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8", errors="replace")
    m = re.search(r"m_EditorVersion:\s*(\S+)", text)
    if not m:
        raise ToolMissing("ProjectSettings/ProjectVersion.txt không ghi phiên bản Unity")
    return m.group(1)


def unity_data(version):
    """The Editor's Data folder (Managed/, NetStandard/, Resources/PackageManager/BuiltInPackages)."""
    env = os.environ.get("UNITY_EDITOR_PATH")
    tries = [Path(env)] if env else []
    home = Path.home()
    tries += [home / "Unity" / "Hub" / "Editor" / version / "Editor",
              Path("C:/Program Files/Unity/Hub/Editor") / version / "Editor",
              Path("/Applications/Unity/Hub/Editor") / version / "Unity.app" / "Contents"]
    for t in tries:
        for data in (t / "Data", t):
            if (data / "Managed" / "UnityEngine").is_dir():
                return data
    raise ToolMissing(f"không thấy Unity Editor {version} (đặt UNITY_EDITOR_PATH tới thư mục Editor)")


def csc_command():
    """[dotnet, path of csc.dll] of the newest SDK."""
    dotnet = os.environ.get("DOTNET") or shutil.which("dotnet")
    if not dotnet:
        raise ToolMissing("không có dotnet SDK (cài .NET SDK, hoặc đặt DOTNET)")
    try:
        out = subprocess.run([dotnet, "--list-sdks"], capture_output=True, text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError) as ex:
        raise ToolMissing(f"dotnet không chạy được: {ex}") from None
    sdks = [re.match(r"^(\S+) \[(.+)\]$", line.strip()) for line in out.splitlines()]
    sdks = [Path(m.group(2)) / m.group(1) for m in sdks if m]
    for sdk in sorted(sdks, key=lambda p: version_key(p.name), reverse=True):
        csc = sdk / "Roslyn" / "bincore" / "csc.dll"
        if csc.is_file():
            return [dotnet, str(csc)]
    raise ToolMissing("dotnet SDK không có Roslyn (Roslyn/bincore/csc.dll)")


def version_key(v):
    """Sort key of a version like 6000.3.10f1, 2.0.3, 1.0.0-pre.2: numbers as numbers, a < b < f < p."""
    return [(0, int(x)) if x.isdigit() else (1, x) for x in re.findall(r"\d+|[A-Za-z]+", str(v or ""))]


# ---------------------------------------------------------------- packages and assemblies
def package_versions(up, data):
    """{package name: version} from packages-lock.json (a git package: its package.json in the cache)."""
    out = {}
    try:
        lock = json.loads((up / "Packages" / "packages-lock.json").read_text(encoding="utf-8"))["dependencies"]
    except (OSError, ValueError, KeyError):
        lock = {}
    for name, info in lock.items():
        out[name] = str(info.get("version") or "")
    for folder in package_folders(up, data):
        try:
            p = json.loads((folder / "package.json").read_text(encoding="utf-8"))
            if p.get("name") and (not out.get(p["name"]) or "://" in out[p["name"]] or out[p["name"]].startswith("file:")):
                out[p["name"]] = str(p.get("version") or "")
        except (OSError, ValueError):
            pass
    return out


def package_folders(up, data):
    """The folders of the packages the project uses: embedded (Packages/<x>/), resolved (Library/PackageCache), and
    built-in ones of the Editor named by the lock file."""
    out = []
    for d in sorted((up / "Packages").iterdir()) if (up / "Packages").is_dir() else []:
        if d.is_dir() and (d / "package.json").is_file():
            out.append(d)
    cache = up / "Library" / "PackageCache"
    for d in sorted(cache.iterdir()) if cache.is_dir() else []:
        if d.is_dir() and (d / "package.json").is_file():
            out.append(d)
    have = set()
    for d in out:
        try:
            have.add(json.loads((d / "package.json").read_text(encoding="utf-8")).get("name"))
        except (OSError, ValueError):
            pass
    try:
        lock = json.loads((up / "Packages" / "packages-lock.json").read_text(encoding="utf-8"))["dependencies"]
    except (OSError, ValueError, KeyError):
        lock = {}
    builtin = data / "Resources" / "PackageManager" / "BuiltInPackages"
    for name in sorted(lock):
        if name not in have and (builtin / name / "package.json").is_file():
            out.append(builtin / name)
    return out


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return None


def meta_guid(path):
    try:
        m = re.search(r"^guid:\s*([0-9a-f]{32})", Path(str(path) + ".meta").read_text(encoding="utf-8", errors="replace"), re.M)
    except OSError:
        return None
    return m.group(1) if m else None


def walk(root):
    """(folder, files) under root, skipping folders Unity ignores (name~ and .name)."""
    for dirpath, dirs, files in os.walk(root):
        dirs[:] = sorted(d for d in dirs if not SKIP_DIR_RX.search(d))
        yield Path(dirpath), files


class Asm:
    def __init__(self, name, folder, data, project):
        self.name, self.folder, self.data, self.project = name, folder, data, project
        self.extra_folders = []      # folders an .asmref adds
        self.sources = []

    def get(self, key, default=None):
        value = self.data.get(key)
        return default if value is None else value


def discover(up, data):
    """{name: Asm} of every assembly definition in Assets and the packages, with its sources, and the plugin DLLs
    ({file name: path})."""
    roots = [(up / "Assets", True)] + [(p, False) for p in package_folders(up, data)]
    asms, by_guid, refs, plugins = {}, {}, [], {}
    owners = {}      # folder -> assembly name (asmdef or asmref)
    for root, project in roots:
        for folder, files in walk(root):
            for f in files:
                path = folder / f
                if f.endswith(".asmdef"):
                    d = read_json(path)
                    if isinstance(d, dict) and d.get("name") and d["name"] not in asms:
                        a = Asm(d["name"], folder, d, project)
                        asms[a.name] = a
                        owners[folder] = a.name
                        g = meta_guid(path)
                        if g:
                            by_guid[g] = a.name
                elif f.endswith(".asmref"):
                    d = read_json(path)
                    if isinstance(d, dict) and d.get("reference"):
                        refs.append((folder, d["reference"]))
                elif f.lower().endswith(".dll"):
                    plugins.setdefault(f, path)
    for folder, ref in refs:
        name = by_guid.get(ref[5:]) if ref.startswith("GUID:") else ref
        if name in asms:
            asms[name].extra_folders.append(folder)
            owners[folder] = name
    for a in asms.values():
        a.refs = [x for x in (by_guid.get(r[5:]) if str(r).startswith("GUID:") else r for r in a.get("references", [])) if x]
    for root, project in roots:
        for folder, files in walk(root):
            owner = None
            probe = folder
            while True:
                if probe in owners:
                    owner = owners[probe]
                    break
                if probe == root or probe.parent == probe:
                    break
                probe = probe.parent
            if owner:
                asms[owner].sources += [folder / f for f in files if f.endswith(".cs")]
            elif project:
                loose = [folder / f for f in files if f.endswith(".cs")]
                if loose:
                    editor = "Editor" in folder.relative_to(root).parts
                    key = "Assembly-CSharp-Editor" if editor else "Assembly-CSharp"
                    if key not in asms:
                        asms[key] = Asm(key, root, {"name": key, "includePlatforms": ["Editor"] if editor else []}, True)
                        asms[key].refs, asms[key].predefined = [], True
                    asms[key].sources += loose
    for a in asms.values():
        a.sources.sort()
        if getattr(a, "predefined", False):      # the loose scripts see every auto-referenced assembly
            a.refs = [n for n, b in asms.items() if b.get("autoReferenced", True) and not getattr(b, "predefined", False)]
            if a.name == "Assembly-CSharp-Editor" and "Assembly-CSharp" in asms:
                a.refs.append("Assembly-CSharp")
    return asms, plugins


# ---------------------------------------------------------------- defines
def editor_defines(version, up):
    m = re.match(r"^(\d+)\.(\d+)\.(\d+)", version)
    major, minor, patch = (m.groups() if m else ("6000", "0", "0"))
    out = [f"UNITY_{major}", f"UNITY_{major}_{minor}", f"UNITY_{major}_{minor}_{patch}"]
    out += [f"UNITY_{v}_OR_NEWER" for v in OLDER]
    if int(major) >= 6000:
        out += [f"UNITY_6000_{n}_OR_NEWER" for n in range(0, int(minor) + 1)]
    system = platform.system()
    host = {"Linux": ("LINUX", "STANDALONE_LINUX"), "Darwin": ("OSX", "STANDALONE_OSX"), "Windows": ("WIN", "STANDALONE_WIN")}
    editor_os, standalone = host.get(system, ("LINUX", "STANDALONE_LINUX"))
    out += ["UNITY_EDITOR", "UNITY_EDITOR_64", f"UNITY_EDITOR_{editor_os}", "UNITY_STANDALONE", f"UNITY_{standalone}",
            "UNITY_64", "UNITY_INCLUDE_TESTS", "UNITY_ASSERTIONS", "UNITY_ANALYTICS", "UNITY_SERVER" if False else "",
            "ENABLE_MONO", "ENABLE_UNITYEVENTS", "ENABLE_PROFILER", "ENABLE_RUNTIME_GI", "ENABLE_UNITYWEBREQUEST",
            "ENABLE_INPUT_SYSTEM" if False else "ENABLE_LEGACY_INPUT_MANAGER", "ENABLE_UNITY_COLLECTIONS_CHECKS",
            "NET_STANDARD_2_1", "NET_STANDARD", "NETSTANDARD2_1", "NETSTANDARD", "NET_4_6" if False else "",
            "CSHARP_7_3_OR_NEWER", "CSHARP_7_OR_LATER", "PLATFORM_STANDALONE", "TEXTCORE_1_0_OR_NEWER"]
    out += project_symbols(up)
    return sorted(set(x for x in out if x))


def project_symbols(up):
    """The scripting define symbols of the project (ProjectSettings.asset), for the Standalone target."""
    try:
        text = (up / "ProjectSettings" / "ProjectSettings.asset").read_text(encoding="utf-8", errors="replace")
    except OSError:
        return []
    m = re.search(r"scriptingDefineSymbols:\s*\n((?:\s{4,}\S.*\n)+)", text)
    if not m:
        return []
    out = []
    for line in m.group(1).splitlines():
        k, _, v = line.strip().partition(":")
        if k.strip() in ("Standalone", "1") and v.strip():
            out += [s for s in v.strip().split(";") if s]
    return out


def in_range(version, expression):
    """Unity's version range of versionDefines: '1.0.0' (that or newer), '[a,b]', '[a,b)', '(a,b)', '[a]'."""
    if not version:
        return False
    expression = expression.strip()
    v = version_key(version)
    if not expression or expression[0] not in "[(":
        return v >= version_key(expression)
    m = re.match(r"^([\[(])\s*([^,\])]*)\s*(?:,\s*([^\])]*))?\s*([\])])$", expression)
    if not m:
        return False
    lo_in, lo, hi, hi_in = m.group(1) == "[", m.group(2), m.group(3), m.group(4) == "]"
    if hi is None:      # [a]
        return v == version_key(lo)
    if lo and (v < version_key(lo) or (v == version_key(lo) and not lo_in)):
        return False
    if hi and (v > version_key(hi) or (v == version_key(hi) and not hi_in)):
        return False
    return True


def asm_defines(a, base, versions, unity):
    out = list(base)
    for vd in a.get("versionDefines", []) or []:
        if not isinstance(vd, dict) or not vd.get("define"):
            continue
        have = unity if vd.get("name") == "Unity" else versions.get(vd.get("name", ""), "")
        if in_range(have, str(vd.get("expression") or "")):
            out.append(vd["define"])
    return sorted(set(out))


def constraints_met(a, defines):
    have = set(defines)
    for c in a.get("defineConstraints", []) or []:
        alternatives = [x.strip() for x in str(c).split("||") if x.strip()]
        if not any((x[1:] not in have) if x.startswith("!") else (x in have) for x in alternatives):
            return False
    return True


# ---------------------------------------------------------------- compiling
class Checker:
    def __init__(self, root=ROOT, jobs=None):
        self.root = Path(root)
        self.up = self.root / UNITY_PROJECT
        self.version = unity_version(self.up)
        self.data = unity_data(self.version)
        self.csc = csc_command()
        self.asms, self.plugins = discover(self.up, self.data)
        self.versions = package_versions(self.up, self.data)
        self.base_defines = editor_defines(self.version, self.up)
        self.jobs = jobs or max(1, min(8, (os.cpu_count() or 2)))
        self.overlay = {}      # assembly name -> (sources, draft digest)
        managed = self.data / "Managed"
        net = self.data / "NetStandard"
        self.framework = [net / "ref" / "2.1.0" / "netstandard.dll"] + sorted((net / "compat" / "2.1.0" / "shims" / "netfx").glob("*.dll"))
        self.engine = sorted((managed / "UnityEngine").glob("*.dll"))
        self.search = [managed, self.data / "Tools" / "BuildPipeline" / "Compilation" / "ApiUpdater"]
        self.results = {}      # name -> (status, errors, dll)

    # which assemblies
    def folder_of(self, name):
        name = name.strip().replace("\\", "/")
        if name in ("@Core", "Core"):
            return self.up / "Assets" / "Core"
        if name in ("_Composition", "Composition", "@CodeLead"):
            return self.up / "Assets" / "_Composition"
        if not re.match(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$", name):
            raise ValueError(f"tên '{name}' không phải feature, service, Core hay _Composition")
        return self.up / "Assets" / "Features" / name

    def in_folder(self, folder):
        return [a.name for a in self.asms.values() if a.project and (a.folder == folder or folder in a.folder.parents)]

    def dependents(self, names):
        """Project assemblies that reference one of names, transitively."""
        out, todo = set(names), list(names)
        while todo:
            n = todo.pop()
            for a in self.asms.values():
                if a.project and a.name not in out and n in a.refs:
                    out.add(a.name)
                    todo.append(a.name)
        return out

    def apply_draft(self, name, folder):
        """Puts the drafts of feature `name` over the sources of its assemblies (self.overlay)."""
        draft = self.up / DRAFTS / name
        if not draft.is_dir():
            raise ValueError(f"chưa có nháp: {draft.relative_to(self.root)}")
        meta = read_json(draft / DRAFT_FILE) or {}
        deleted = {str(x).replace("\\", "/") for x in meta.get("delete", []) or []}
        files = {}
        for d, fs in walk(draft):
            for f in fs:
                if f.endswith(".cs"):
                    files[(d / f).relative_to(draft).as_posix()] = d / f
        mine = [self.asms[n] for n in self.in_folder(folder)]
        for a in mine:
            srcs = []
            for s in a.sources:
                rel = s.relative_to(folder).as_posix()
                if rel in deleted:
                    continue
                srcs.append(files.pop(rel, s))
            self.overlay[a.name] = srcs
        for rel, path in files.items():      # new files: the assembly whose folder holds that path
            target = folder / rel
            owner = max((a for a in mine if a.folder in target.parents), key=lambda a: len(a.folder.parts), default=None)
            if owner is None:
                raise ValueError(f"nháp {rel} không nằm trong thư mục asmdef nào của {name}")
            self.overlay.setdefault(owner.name, list(owner.sources)).append(path)

    # one assembly
    def plan(self, name, seen=None):
        """Every assembly `name` needs, dependencies first."""
        order, state = [], {}

        def visit(n):
            if state.get(n) == 2 or n not in self.asms:
                return
            if state.get(n) == 1:
                return      # a cycle: Unity refuses it too; compiled with what is there
            state[n] = 1
            for r in self.refs_of(self.asms[n]):
                visit(r)
            state[n] = 2
            order.append(n)
        visit(name)
        return order

    def refs_of(self, a):
        refs = [r for r in a.refs if r in self.asms]
        if not a.get("noEngineReferences", False):
            for imp in IMPLICIT:
                if imp in self.asms and imp != a.name and a.name not in self.closure(imp):
                    refs.append(imp)
        return refs

    def closure(self, name, acc=None):
        acc = set() if acc is None else acc
        if name in acc or name not in self.asms:
            return acc
        acc.add(name)
        for r in self.asms[name].refs:
            self.closure(r, acc)
        return acc

    def precompiled(self, a):
        names = a.get("precompiledReferences", []) if a.get("overrideReferences", False) else []
        out = []
        for n in names:
            path = self.plugins.get(n) or next((d / n for d in self.search if (d / n).is_file()), None)
            if path:
                out.append(path)
        return out

    def compile(self, name):
        a = self.asms[name]
        defines = asm_defines(a, self.base_defines, self.versions, self.version)
        if not constraints_met(a, defines):
            return ("BỎ", [], None)
        deps = self.refs_of(a)
        dlls = []
        for d in deps:
            st, _, dll = self.results.get(d, ("CHẶN", [], None))
            if st == "BỎ":
                continue
            if st != "OK":
                return ("CHẶN", [d], None)
            dlls.append(dll)
        refs = list(self.framework) + ([] if a.get("noEngineReferences", False) else list(self.engine)) + self.precompiled(a) + dlls
        sources = self.overlay.get(name, a.sources)
        if not sources:
            return ("OK", [], self.empty_dll(name, refs, defines))
        key = hashlib.sha1()
        for s in sorted(map(str, sources)):
            st = os.stat(s)
            key.update(f"{s}|{st.st_size}|{st.st_mtime_ns}\n".encode())
        for r in refs:
            key.update(f"{r}|{self.digest(r)}\n".encode())
        key.update(("|".join(defines) + f"|{a.get('allowUnsafeCode', False)}|{LANG_VERSION}").encode())
        out_dir = CACHE / self.version / name.replace("/", "_") / key.hexdigest()[:20]
        dll = out_dir / f"{name}.dll"
        done = out_dir / "result.json"
        cached = read_json(done)
        if isinstance(cached, dict) and (cached.get("ok") is False or dll.is_file()):
            return ("OK", [], dll) if cached.get("ok") else ("LỖI", cached.get("errors", []), None)
        out_dir.mkdir(parents=True, exist_ok=True)
        rsp = out_dir / "build.rsp"
        args = ["-noconfig", "-nostdlib+", "-nologo", "-target:library", f"-langversion:{LANG_VERSION}", "-deterministic",
                "-warn:0", "-nowarn:CS1701,CS1702,CS8632", f"-out:{dll}", "-define:" + ";".join(defines)]
        if a.get("allowUnsafeCode", False):
            args.append("-unsafe")
        args += [f'-r:"{r}"' for r in refs] + [f'"{s}"' for s in sources]
        rsp.write_text("\n".join(args), encoding="utf-8")
        try:
            p = subprocess.run(self.csc + [f"@{rsp}"], capture_output=True, text=True, timeout=600)
        except (OSError, subprocess.SubprocessError) as ex:
            raise ToolMissing(f"không chạy được trình biên dịch: {ex}") from None
        errors = []
        for line in (p.stdout + "\n" + p.stderr).splitlines():
            m = ERROR_RX.match(line.strip())
            if m:
                errors.append(f"{self.rel(m.group('path'))}:{m.group('line')}: {m.group('code')}: {m.group('msg').strip()}")
            elif ": error " in line:
                errors.append(line.strip())
        ok = p.returncode == 0 and dll.is_file()
        if not ok and not errors:
            errors = [(p.stdout + p.stderr).strip()[:400] or f"csc thoát với mã {p.returncode}"]
        done.write_text(json.dumps({"ok": ok, "errors": errors[:200], "at": time.time()}), encoding="utf-8")
        return ("OK", [], dll) if ok else ("LỖI", errors, None)

    def empty_dll(self, name, refs, defines):
        """An assembly with no sources (an empty View folder): nothing to compile; a tiny stand-in for its dependents."""
        out_dir = CACHE / self.version / name.replace("/", "_") / "empty"
        dll = out_dir / f"{name}.dll"
        if not dll.is_file():
            out_dir.mkdir(parents=True, exist_ok=True)
            src = out_dir / "Empty.cs"
            src.write_text("internal static class __CompileCheckEmpty {}\n", encoding="utf-8")
            subprocess.run(self.csc + ["-noconfig", "-nostdlib+", "-nologo", "-target:library", f"-out:{dll}",
                                       f"-r:{self.framework[0]}", str(src)], capture_output=True, text=True, timeout=300)
        return dll

    _digests = {}

    def digest(self, path):
        path = str(path)
        st = os.stat(path)
        key = (path, st.st_size, st.st_mtime_ns)
        if key not in self._digests:
            self._digests[key] = f"{st.st_size}-{st.st_mtime_ns}"
        return self._digests[key]

    def rel(self, path):
        p = Path(path)
        for base in (self.root, self.data):
            try:
                return p.resolve().relative_to(base.resolve()).as_posix()
            except (ValueError, OSError):
                continue
        return str(path)

    def run(self, targets):
        """Compiles what `targets` need; independent assemblies in parallel. Returns {name: (status, errors)}."""
        needed = []
        for t in targets:
            for n in self.plan(t):
                if n not in needed:
                    needed.append(n)
        pending = list(needed)
        with concurrent.futures.ThreadPoolExecutor(max_workers=self.jobs) as pool:
            running = {}
            while pending or running:
                ready = [n for n in pending if all(d in self.results or d not in needed for d in self.refs_of(self.asms[n]))]
                for n in ready:
                    pending.remove(n)
                    running[pool.submit(self.compile, n)] = n
                if not running:
                    for n in pending:      # a cycle: what is left cannot be ordered
                        self.results[n] = ("LỖI", ["vòng tham chiếu giữa các asmdef"], None)
                    break
                done, _ = concurrent.futures.wait(running, return_when=concurrent.futures.FIRST_COMPLETED)
                for f in done:
                    n = running.pop(f)
                    self.results[n] = f.result()
        return {n: self.results[n][:2] for n in targets}


# ---------------------------------------------------------------- command line
def report(checker, targets, quiet):
    res = checker.run(targets)
    lines, budget = [], MAX_LINES
    counts = {"OK": 0, "LỖI": 0, "CHẶN": 0, "BỎ": 0}
    outside_block = False
    for n in sorted(targets):
        st, info = res[n]
        counts[st] += 1
        if st == "OK" and quiet:
            continue
        if st == "CHẶN":
            dep = info[0] if info else "?"
            outside_block = outside_block or dep not in targets
            lines.append(f"CHẶN {n} (phụ thuộc {dep} lỗi)")
        elif st == "LỖI":
            lines.append(f"LỖI  {n}: {len(info)} lỗi")
            for e in info[:max(1, budget)]:
                lines.append(f"      {e}")
            budget -= len(info)
        else:
            lines.append(f"{st:4} {n}")
    blocked_by = sorted({res[n][1][0] for n in targets if res[n][0] == "CHẶN" and res[n][1] and res[n][1][0] not in targets})
    for b in blocked_by:      # the dependency's own errors, so the reader sees why
        st, errs, _ = checker.results.get(b, ("?", [], None))
        if st == "LỖI":
            lines.append(f"LỖI  {b} (ngoài phần kiểm): {len(errs)} lỗi")
            lines += [f"      {e}" for e in errs[:5]]
    head = (f"compile_check: {counts['OK']} OK, {counts['LỖI']} LỖI, {counts['CHẶN']} CHẶN, {counts['BỎ']} BỎ "
            f"({len(targets)} assembly, Unity {checker.version})")
    print(head)
    for line in lines:
        print(line)
    if counts["LỖI"]:
        return 1
    if counts["CHẶN"]:
        return 3 if outside_block else 1
    return 0


def main(argv):
    args = argv[1:]
    quiet = "--quiet" in args
    draft = "--draft" in args
    rest = [a for a in args if a not in ("--quiet", "--draft")]
    if not rest or (rest[0] == "--all" and (len(rest) > 1 or draft)) or (rest[0] != "--all" and len(rest) != 1):
        print(__doc__, file=sys.stderr)
        return 2
    started = time.time()
    try:
        checker = Checker()
        if rest[0] == "--all":
            targets = sorted(a.name for a in checker.asms.values() if a.project)
        else:
            folder = checker.folder_of(rest[0])
            own = checker.in_folder(folder)
            if not own:
                print(f"compile_check: {rest[0]} chưa có asmdef nào trong {folder.relative_to(checker.root)}", file=sys.stderr)
                return 2
            if draft:
                checker.apply_draft(rest[0].strip().lstrip("@") if rest[0].strip() in ("@Core",) else rest[0].strip(), folder)
            targets = sorted(checker.dependents(own))
        code = report(checker, targets, quiet)
    except ToolMissing as ex:
        print(f"compile_check: KHÔNG KIỂM ĐƯỢC: {ex}", file=sys.stderr)
        return 2
    except ValueError as ex:
        print(f"compile_check: {ex}", file=sys.stderr)
        return 2
    if not quiet:
        print(f"({time.time() - started:.1f}s)")
    return code


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
