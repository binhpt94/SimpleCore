#!/usr/bin/env python3
"""Checks the feature boundaries of UnityProject/Assets without starting Unity.

    python Management/tools/validate.py [--feature <id>] [--all] [--json]

Scope: --feature <id> shows only that feature. With neither --feature nor --all, FEATURE from
the environment is used when it names a feature; otherwise the whole project is shown.
Exit code: 0 = no violation in scope, 1 = violations, 2 = the validator itself failed.

Rule groups
  STR  structure      01 missing asmdef   02 .cs outside a layer   03 unexpected asmdef   04 .asmref
                      05 Shared folder    06 Assets root / feature name   07 feature layout
  ASM  asmdef content 00 bad JSON   01 name   02 autoReferenced   03 GUID reference
                      04 reference against the matrix   05 Contracts uses the engine
                      06 precompiled DLL   07 Tests without UNITY_INCLUDE_TESTS
  SRC  C# source      01 public type in Internal   02 MonoBehaviour in Internal
                      03 InternalsVisibleTo   04 banned API
  AST  assets         01 reference to another feature's private asset   02 not text-serialized
  DOC  01 CLAUDE.md too long        CFG  01 invalid exception in rules.json

C# is scanned as text (regex), not parsed. Standard library only.
"""
import argparse
import fnmatch
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path

NAME_RX = re.compile(r"^[A-Z][A-Za-z0-9]*$")
FEATURE_ID_RX = re.compile(r"^(_Services/)?[A-Za-z][A-Za-z0-9]*$")
RESERVED = ("Services", "Core", "Composition")
OWN_ROOTS = ("Features", "Core", "_Composition")
ROOT_MD_MAX, FEATURE_MD_MAX = 30, 20

# ---------------------------------------------------------------- C# text helpers
STRIP_RX = re.compile(
    r'''//[^\n]*|/\*.*?\*/|"""[\s\S]*?"""|\$?@\$?"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])+' ''',
    re.S | re.X,
)


def strip_cs(text, keep_strings):
    """Blank out comments (and string contents unless keep_strings); line numbers stay the same."""
    def repl(m):
        s = m.group(0)
        newlines = "\n" * s.count("\n")
        if s.startswith("//") or s.startswith("/*"):
            return newlines
        return s if keep_strings else '""' + newlines
    return STRIP_RX.sub(repl, text)


MODS = r"(?:\b(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|ref|unsafe|new|file)\s+)*"
TOKEN_RX = re.compile(
    r"(?P<ns>\bnamespace\s+[\w.]+\s*\{)"
    r"|(?P<del>(?P<dmods>" + MODS + r")\bdelegate\s+[^;(){}]*?\b(?P<dname>\w+)\s*(?:<[^>(]*>)?\s*\()"
    r"|(?P<type>(?P<mods>" + MODS + r")\b(?:record\s+(?:class|struct)|class|struct|interface|enum|record)\s+(?P<name>[A-Za-z_]\w*))"
    r"|(?P<open>\{)|(?P<close>\})|(?P<semi>;)"
)


def exported_types(code):
    """(name, index) of types visible outside the assembly: public, with only public types around them."""
    out, stack, pending = [], [], None
    for m in TOKEN_RX.finditer(code):
        exported = "int" not in stack and "block" not in stack
        if m.group("ns"):
            stack.append("ns")
            pending = None
        elif m.group("del"):
            if exported and re.search(r"\bpublic\b", m.group("dmods")):
                out.append((m.group("dname"), m.start()))
        elif m.group("type"):
            public = bool(re.search(r"\bpublic\b", m.group("mods")))
            if exported and public:
                out.append((m.group("name"), m.start()))
            pending = "pub" if public else "int"
        elif m.group("open"):
            stack.append(pending or "block")
            pending = None
        elif m.group("close"):
            if stack:
                stack.pop()
        else:
            pending = None
    return out


CLASS_RX = re.compile(r"\bclass\s+(?P<name>\w+)\s*(?:<[^>{]*>)?\s*:\s*(?P<bases>[^{]+)\{")


def class_bases(code):
    """(class name, [base identifiers], index) for every class that has a base list."""
    out = []
    for m in CLASS_RX.finditer(code):
        bases = re.split(r"\bwhere\b", m.group("bases"))[0]
        bases = re.sub(r"<[^>]*>", "", bases)
        names = [b.strip().split(".")[-1].strip() for b in bases.split(",")]
        out.append((m.group("name"), [n for n in names if n], m.start()))
    return out


def line_of(text, index):
    return text.count("\n", 0, index) + 1


def read_text(path):
    return path.read_text(encoding="utf-8-sig", errors="replace")


# ---------------------------------------------------------------- reference matrix
def asm_kind(name):
    if name == "Game.Core":
        return {"scope": "core", "layer": "Core"}
    if name == "Game.Core.Tests":
        return {"scope": "core", "layer": "Tests"}
    if name == "Game.Composition":
        return {"scope": "composition", "layer": "Composition"}
    if name == "Game.Composition.Tests":
        return {"scope": "composition", "layer": "Tests"}
    m = re.match(r"^Game\.Services\.(\w+)\.(Contracts|Internal|View|Tests)$", name)
    if m:
        return {"scope": "feature", "feature": "_Services/" + m.group(1), "layer": m.group(2)}
    m = re.match(r"^Game\.(\w+)\.(Contracts|Internal|View|Tests)$", name)
    if m:
        return {"scope": "feature", "feature": m.group(1), "layer": m.group(2)}
    return {"scope": "package", "layer": ""}


def per_layer(table, layer):
    """Values of a per-layer table in rules.json, plus its '*' entry."""
    table = table or {}
    return list(table.get(layer, [])) + list(table.get("*", []))


def check_reference(me, ref, cfg):
    """None when assembly `me` may reference `ref`, else the reason it may not."""
    r = asm_kind(ref)
    layer = me["layer"]
    if r["scope"] == "package":
        if ref in per_layer(cfg.get("allowedPackages"), layer):
            return None
        return f"package '{ref}' is not on allowedPackages for layer {layer} in rules.json"

    is_core = ref == "Game.Core"
    own = r["scope"] == "feature" and me["scope"] == "feature" and r["feature"] == me["feature"]
    other_contracts = r["scope"] == "feature" and not own and r["layer"] == "Contracts"

    if me["scope"] == "core":
        if layer == "Tests" and is_core:
            return None
        return "Core may not reference any game assembly"
    if me["scope"] == "composition":
        if is_core or (r["scope"] == "feature" and r["layer"] == "Internal"):
            return None
        if layer == "Tests" and ref == "Game.Composition":
            return None
        return "_Composition may reference only Internal assemblies (installers) and Game.Core"
    if layer == "Contracts":
        return None if is_core else "Contracts may reference only Game.Core"
    if layer == "Internal":
        if is_core or (own and r["layer"] == "Contracts"):
            return None
        if other_contracts:
            if me["is_service"]:
                return "a service may not know other features: only its own Contracts and Game.Core"
            return None
        if own and r["layer"] == "View":
            return "Internal may not reference View (dependency goes View -> Internal)"
        return "Internal may reference only its own Contracts, other features' Contracts and Game.Core"
    if layer == "View":
        if is_core or (own and r["layer"] in ("Internal", "Contracts")):
            return None
        return "View may reference only its own Internal, its own Contracts and Game.Core"
    if layer == "Tests":
        if is_core or (own and r["layer"] != "Tests"):
            return None
        if other_contracts and not me["is_service"]:
            return None
        return "Tests may reference only their own feature, other features' Contracts and Game.Core"
    return "unknown layer"


# ---------------------------------------------------------------- the scan
SKIP_EXT = {
    ".cs", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".gif", ".bmp", ".exr", ".hdr", ".tif", ".tiff", ".fbx", ".obj",
    ".blend", ".wav", ".mp3", ".ogg", ".aif", ".mp4", ".mov", ".webm", ".ttf", ".otf", ".dll", ".so", ".a", ".bytes",
    ".zip", ".pdf",
}
YAML_EXT = {".prefab", ".unity", ".asset", ".mat", ".controller", ".overridecontroller", ".anim", ".playable",
            ".spriteatlas", ".preset", ".mixer"}
GUID_RX = re.compile(r"guid[:=]\s*([0-9a-f]{32})")
META_GUID_RX = re.compile(r"^guid:\s*([0-9a-f]{32})", re.M)
IVT_RX = re.compile(r'\bInternalsVisibleTo(?:Attribute)?\s*\(\s*"([^"]*)"\s*\)')


class Scan:
    def __init__(self, root, cfg):
        self.root = Path(root).resolve()
        self.assets = self.root / "UnityProject" / "Assets"
        self.cfg = cfg
        self.violations = []
        self.features = []       # dicts: id, name, dir, prefix, is_service
        self.expected = []       # dicts: dir, name, scope, feature, layer, is_service, asmdef
        self.files = []
        self.dirs = []

    # ----- helpers
    def rel(self, path):
        return Path(path).relative_to(self.root).as_posix()

    def owner(self, path):
        try:
            parts = Path(path).relative_to(self.assets).parts
        except ValueError:
            return "(project)"
        if not parts:
            return "(project)"
        head = parts[0].lower()
        if head == "core":
            return "Core"
        if head == "_composition":
            return "_Composition"
        if head == "features" and len(parts) >= 2:
            fid = parts[1]
            if parts[1].lower() == "_services" and len(parts) >= 3:
                fid = "_Services/" + parts[2]
            for f in self.features:
                if f["id"].lower() == fid.lower():
                    return f["id"]
        return "(project)"

    def add(self, rule, path, line, message):
        self.violations.append({"rule": rule, "owner": self.owner(path), "file": self.rel(path), "line": line,
                                "message": message})

    def assembly_of(self, path):
        """The expected assembly a file belongs to; the deepest matching folder wins (Core/Tests before Core)."""
        best = None
        parents = Path(path).parents
        for e in self.expected:
            if e["dir"] in parents and (best is None or len(e["dir"].parts) > len(best["dir"].parts)):
                best = e
        return best

    # ----- steps
    def run(self):
        if not self.assets.is_dir():
            raise FileNotFoundError(f"Assets folder not found: {self.assets}")
        self.discover()
        self.check_roots_and_names()
        self.check_layout()
        self.check_md_size()
        self.check_assemblies()
        self.check_sources()
        self.check_assets()
        return self

    def discover(self):
        feat_root = self.assets / "Features"
        if feat_root.is_dir():
            for d in sorted(p for p in feat_root.iterdir() if p.is_dir()):
                if d.name == "_Services":
                    for s in sorted(p for p in d.iterdir() if p.is_dir()):
                        self.features.append({"id": "_Services/" + s.name, "name": s.name, "dir": s,
                                              "prefix": "Game.Services." + s.name, "is_service": True})
                else:
                    self.features.append({"id": d.name, "name": d.name, "dir": d, "prefix": "Game." + d.name,
                                          "is_service": False})
        for r in OWN_ROOTS:   # third-party roots (extraAssetRoots) are not scanned
            for base, dirnames, filenames in os.walk(self.assets / r):
                self.dirs += [Path(base) / d for d in dirnames]
                self.files += [Path(base) / f for f in filenames]

        for f in self.features:
            for layer in ("Contracts", "Internal", "View"):
                self.expected.append({"dir": f["dir"] / "Scripts" / layer, "name": f"{f['prefix']}.{layer}",
                                      "scope": "feature", "feature": f["id"], "layer": layer,
                                      "is_service": f["is_service"]})
            self.expected.append({"dir": f["dir"] / "Tests", "name": f["prefix"] + ".Tests", "scope": "feature",
                                  "feature": f["id"], "layer": "Tests", "is_service": f["is_service"]})
        for sub, name, scope, layer in (("Core", "Game.Core", "core", "Core"),
                                        ("Core/Tests", "Game.Core.Tests", "core", "Tests"),
                                        ("_Composition", "Game.Composition", "composition", "Composition"),
                                        ("_Composition/Tests", "Game.Composition.Tests", "composition", "Tests")):
            self.expected.append({"dir": self.assets / sub, "name": name, "scope": scope, "feature": None,
                                  "layer": layer, "is_service": False})
        for e in self.expected:
            e["asmdef"] = e["dir"] / (e["name"] + ".asmdef")

    def check_roots_and_names(self):
        known = list(OWN_ROOTS) + list(self.cfg.get("extraAssetRoots") or [])
        for item in sorted(self.assets.iterdir()):
            if item.is_dir():
                if item.name not in known:
                    self.add("STR-06", item, 0, f"unexpected folder in Assets root; allowed: {', '.join(known)} "
                             "(extend extraAssetRoots in rules.json for third-party folders)")
            elif item.suffix != ".meta":
                self.add("STR-06", item, 0, "unexpected file in Assets root")
        for f in self.features:
            if not NAME_RX.match(f["name"]) or f["name"] in RESERVED:
                self.add("STR-06", f["dir"], 0, f"invalid feature name '{f['name']}': use PascalCase letters and "
                         f"digits; reserved: {', '.join(RESERVED)}")
        for folder in (self.assets / "Features", self.assets / "Features" / "_Services"):
            if folder.is_dir():
                for item in sorted(folder.iterdir()):
                    if item.is_file() and item.suffix != ".meta":
                        self.add("STR-06", item, 0, f"unexpected file in {folder.name}")
        for d in self.dirs:
            if d.name.lower() == "shared":
                self.add("STR-05", d, 0, "Shared folders are not allowed; duplicate the code inside each feature instead")

    def check_layout(self):
        def layout(folder, allowed_dirs, allowed_files):
            if not folder.is_dir():
                return
            for item in sorted(folder.iterdir()):
                if item.is_dir():
                    if item.name not in allowed_dirs:
                        self.add("STR-07", item, 0, f"unexpected folder; allowed here: {', '.join(allowed_dirs)}")
                elif item.suffix != ".meta" and item.name not in allowed_files:
                    self.add("STR-07", item, 0, "unexpected file here; code goes in Scripts/<layer> or Tests, "
                             "assets in FeatureAssets/Public|Private")
        for f in self.features:
            layout(f["dir"], ("Scripts", "FeatureAssets", "Tests"), ("CLAUDE.md",))
            layout(f["dir"] / "Scripts", ("Contracts", "Internal", "View"), ())
            layout(f["dir"] / "FeatureAssets", ("Public", "Private"), ())

    def check_md_size(self):
        targets = [(self.root / "CLAUDE.md", ROOT_MD_MAX)] + [(f["dir"] / "CLAUDE.md", FEATURE_MD_MAX) for f in self.features]
        for path, limit in targets:
            if path.is_file():
                n = len(read_text(path).splitlines())
                if n > limit:
                    self.add("DOC-01", path, n, f"CLAUDE.md has {n} lines; the limit is {limit}")

    def check_assemblies(self):
        cs_files = [f for f in self.files if f.suffix.lower() == ".cs"]
        with_code = {id(a) for a in (self.assembly_of(c) for c in cs_files) if a}
        expected_paths = {str(e["asmdef"]).lower(): e for e in self.expected}

        for e in self.expected:
            if id(e) in with_code and not e["asmdef"].is_file():
                self.add("STR-01", e["dir"], 0, f"folder has .cs files but no '{e['name']}.asmdef'")
        for c in cs_files:
            if not self.assembly_of(c):
                self.add("STR-02", c, 0, ".cs file outside Scripts/Contracts, Scripts/Internal, Scripts/View or Tests")
        for f in self.files:
            ext = f.suffix.lower()
            if ext == ".asmref":
                self.add("STR-04", f, 0, ".asmref is forbidden: it compiles this folder into another assembly")
            elif ext == ".asmdef" and str(f).lower() not in expected_paths:
                e = self.assembly_of(f)
                hint = f"; expected file name '{e['name']}.asmdef'" if e and f.parent == e["dir"] else ""
                self.add("STR-03", f, 0, f"unexpected .asmdef: each layer has exactly one, at the root of its folder{hint}")

        for e in self.expected:
            path = e["asmdef"]
            if not path.is_file():
                continue
            try:
                j = json.loads(read_text(path))
                if not isinstance(j, dict):
                    raise ValueError("not an object")
            except ValueError:
                self.add("ASM-00", path, 0, "asmdef is not valid JSON")
                continue
            if j.get("name") != e["name"]:
                self.add("ASM-01", path, 0, f"name is '{j.get('name')}', expected '{e['name']}'")
            if j.get("autoReferenced") is not False:
                self.add("ASM-02", path, 0, "autoReferenced must be false")
            for ref in j.get("references") or []:
                if str(ref).upper().startswith("GUID:"):
                    self.add("ASM-03", path, 0, f"reference '{ref}' uses a GUID; reference by assembly name (untick 'Use GUIDs')")
                    continue
                why = check_reference(e, ref, self.cfg)
                if why:
                    self.add("ASM-04", path, 0, f"reference to '{ref}' not allowed: {why}")
            if e["layer"] == "Contracts" and j.get("noEngineReferences") is not True:
                self.add("ASM-05", path, 0, "Contracts must set noEngineReferences: true (plain C#, no UnityEngine types)")
            ok_dlls = per_layer(self.cfg.get("allowedDlls"), e["layer"])
            for dll in j.get("precompiledReferences") or []:
                if dll not in ok_dlls:
                    self.add("ASM-06", path, 0, f"precompiled reference '{dll}' is not on allowedDlls for layer {e['layer']} in rules.json")
            if e["layer"] == "Tests" and "UNITY_INCLUDE_TESTS" not in (j.get("defineConstraints") or []):
                self.add("ASM-07", path, 0, 'Tests asmdef must have defineConstraints: ["UNITY_INCLUDE_TESTS"] so tests never ship')

    def check_sources(self):
        banned = [(re.compile(b["pattern"]), b.get("reason", "")) for b in self.cfg.get("bannedApis") or []]
        exported = {}   # Internal assembly name -> [(type name, file, line)]
        classes = {}    # Internal / Core assembly name -> [(class name, bases, file, line)]

        for c in sorted(f for f in self.files if f.suffix.lower() == ".cs"):
            e = self.assembly_of(c)
            if not e:
                continue
            text = read_text(c).replace("\r", "")
            no_comments = strip_cs(text, keep_strings=True)
            code = strip_cs(text, keep_strings=False)
            layer = e["layer"]

            # SRC-03
            total = len(re.findall(r"\bInternalsVisibleTo\b", code))
            if total:
                if layer == "Internal":
                    targets = [e["name"][: -len("Internal")] + "View", e["name"][: -len("Internal")] + "Tests"]
                elif layer in ("Core", "Composition"):
                    targets = [e["name"] + ".Tests"]
                else:
                    targets = []
                if not targets or c.name != "AssemblyInfo.cs" or c.parent != e["dir"]:
                    self.add("SRC-03", c, 0, "InternalsVisibleTo is allowed only in Scripts/Internal/AssemblyInfo.cs "
                             "(or AssemblyInfo.cs at the root of Core / _Composition)")
                else:
                    literal = list(IVT_RX.finditer(no_comments))
                    if len(literal) != total:
                        self.add("SRC-03", c, 0, "InternalsVisibleTo must take a plain string literal so it can be checked")
                    for m in literal:
                        if m.group(1) not in targets:
                            self.add("SRC-03", c, line_of(no_comments, m.start()),
                                     f'InternalsVisibleTo("{m.group(1)}") not allowed; only: {", ".join(targets)}')

            # SRC-04
            if layer != "Tests":
                for rx, reason in banned:
                    for m in rx.finditer(code):
                        self.add("SRC-04", c, line_of(code, m.start()), f"banned API '{m.group(0).strip()}': {reason}")

            if layer == "Internal":
                exported.setdefault(e["name"], []).extend((n, c, line_of(code, i)) for n, i in exported_types(code))
            if layer in ("Internal", "Core"):
                classes.setdefault(e["name"], []).extend((n, b, c, line_of(code, i)) for n, b, i in class_bases(code))

        # SRC-01: exactly one public type per Internal, and it is the installer
        for types in exported.values():
            installer = None
            for name, path, line in types:
                if not name.endswith("Installer"):
                    self.add("SRC-01", path, line, f"public type '{name}' in Internal: everything here is internal "
                             "except one class named *Installer")
                elif installer is None:
                    installer = name
                elif name != installer:
                    self.add("SRC-01", path, line, f"second public installer '{name}' (already have '{installer}'): "
                             "Internal exposes exactly one")

        # SRC-02: no MonoBehaviour in Internal, directly or through a base class in the same Internal or in Core
        core_classes = classes.get("Game.Core", [])
        for asm, own in classes.items():
            if asm == "Game.Core":
                continue
            mono = {"MonoBehaviour"}
            grew = True
            while grew:
                grew = False
                for name, bases, _, _ in own + core_classes:
                    if name not in mono and mono.intersection(bases):
                        mono.add(name)
                        grew = True
            for name, _, path, line in own:
                if name in mono:
                    self.add("SRC-02", path, line, f"class '{name}' derives from MonoBehaviour; MonoBehaviours belong in Scripts/View")

    def check_assets(self):
        guid_owner = {}
        for meta in (f for f in self.files if f.suffix.lower() == ".meta"):
            m = META_GUID_RX.search(read_text(meta))
            if m:
                target = meta.with_suffix("")   # drops ".meta"
                rel = self.rel(target)
                guid_owner[m.group(1)] = {"owner": self.owner(target), "file": rel,
                                          "public": "/FeatureAssets/Public/" in rel + "/"}
        feature_ids = {f["id"] for f in self.features}
        service_ids = {f["id"] for f in self.features if f["is_service"]}

        for a in sorted(self.files):
            ext = a.suffix.lower()
            if ext in SKIP_EXT:
                continue
            text = read_text(a)
            if ext in YAML_EXT and not text.startswith("%YAML"):
                self.add("AST-02", a, 0, "asset is not text-serialized, so its references cannot be checked "
                         "(set Asset Serialization to Force Text)")
                continue
            source = self.owner(a.with_suffix("") if ext == ".meta" else a)
            seen = set()
            for m in GUID_RX.finditer(text):
                g = m.group(1)
                if g in seen or g not in guid_owner:
                    continue
                seen.add(g)
                t = guid_owner[g]
                if t["owner"] == source or t["owner"] not in feature_ids:
                    continue
                if source == "Core" or source in service_ids:
                    self.add("AST-01", a, line_of(text, m.start()),
                             f"references '{t['file']}': Core and services may not reference any feature's assets")
                elif not t["public"]:
                    self.add("AST-01", a, line_of(text, m.start()),
                             f"references '{t['file']}', which is private to feature {t['owner']}; "
                             "only FeatureAssets/Public may be used from outside")


# ---------------------------------------------------------------- public entry points
def validate(root, cfg, feature=None):
    """Scan the project. Returns a dict: scope, features, violations, excepted, hiddenCount, countByOwner."""
    scan = Scan(root, cfg).run()
    active, excepted, valid = [], [], []
    for x in cfg.get("exceptions") or []:
        if all(x.get(k) for k in ("rule", "path", "reason", "approvedBy")):
            valid.append(x)
        else:
            active.append({"rule": "CFG-01", "owner": "(project)", "file": "Management/tools/rules.json", "line": 0,
                           "message": f"exception for rule '{x.get('rule')}' path '{x.get('path')}' is ignored: "
                                      "rule, path, reason and approvedBy are all required"})
    for v in scan.violations:
        hit = next((x for x in valid if x["rule"] == v["rule"] and fnmatch.fnmatchcase(v["file"], x["path"])), None)
        if hit:
            excepted.append(dict(v, reason=hit["reason"], approvedBy=hit["approvedBy"]))
        else:
            active.append(v)

    ids = [f["id"] for f in scan.features]
    in_scope = (lambda v: v["owner"].lower() == feature.lower()) if feature else (lambda v: True)
    shown = sorted((v for v in active if in_scope(v)), key=lambda v: (v["owner"], v["rule"], v["file"], v["line"]))
    return {
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "scope": feature or "all",
        "features": ids,
        "violations": shown,
        "excepted": [v for v in excepted if in_scope(v)],
        "hiddenCount": len(active) - len(shown),
        "countByOwner": None if feature else {o: sum(1 for v in active if v["owner"] == o)
                                              for o in ids + ["Core", "_Composition", "(project)"]},
    }


def main(argv=None):
    here = Path(__file__).resolve().parent
    ap = argparse.ArgumentParser(description="Check feature boundaries.")
    ap.add_argument("--root", default=str(here.parent.parent), help="project root (default: two levels above this file)")
    ap.add_argument("--rules", default=str(here / "rules.json"))
    ap.add_argument("--feature", help="show only this feature, e.g. Wallet or _Services/Popup")
    ap.add_argument("--all", action="store_true", help="whole project, ignoring the FEATURE environment variable")
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args(argv)

    feature = args.feature
    env = os.environ.get("FEATURE", "").strip().replace("\\", "/")
    if not feature and not args.all and FEATURE_ID_RX.match(env):
        feature = env
    if feature:
        feature = feature.replace("\\", "/")

    try:
        cfg = json.loads(Path(args.rules).read_text(encoding="utf-8-sig"))
        result = validate(args.root, cfg, feature)
    except Exception as ex:   # a crash must never look like "clean"
        print(f"validate.py failed: {type(ex).__name__}: {ex}", file=sys.stderr)
        return 2

    if args.json:
        print(json.dumps(result, indent=2))
    else:
        for v in result["violations"]:
            loc = f"{v['file']}:{v['line']}" if v["line"] else v["file"]
            print(f"{v['rule']}  [{v['owner']}]  {loc}\n        {v['message']}")
        scope = f"feature {feature}" if feature else "whole project"
        print(f"\n{len(result['violations'])} violation(s) in {scope}; {len(result['excepted'])} approved "
              f"exception(s); {len(result['features'])} feature(s) scanned.")
        if result["hiddenCount"]:
            print(f"{result['hiddenCount']} violation(s) belong to other areas and are not shown.")
    return 1 if result["violations"] else 0


if __name__ == "__main__":
    sys.exit(main())
