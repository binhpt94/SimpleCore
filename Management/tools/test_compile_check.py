#!/usr/bin/env python3
"""Tests of compile_check.py on a small made-up Unity project. Needs the dotnet SDK and the Unity Editor of the real
project's version (for netstandard and the engine DLLs); without them the compile cases are skipped, said so."""
import io
import json
import shutil
import sys
import tempfile
from contextlib import redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import compile_check as cc      # noqa: E402

RESULTS = []


def check(label, got, want=True):
    ok = got == want
    RESULTS.append(ok)
    print(("ok    " if ok else "FAIL  ") + label + ("" if ok else f": expected {want!r}, got {got!r}"))


def asmdef(path, name, refs=(), **more):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(dict({"name": name, "references": list(refs), "autoReferenced": False}, **more)), encoding="utf-8")


def put(root, rel, text):
    p = root / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")
    return p


def project(root, version):
    up = root / "UnityProject"
    put(up, "ProjectSettings/ProjectVersion.txt", f"m_EditorVersion: {version}\n")
    put(up, "Packages/packages-lock.json", json.dumps({"dependencies": {}}))
    a = up / "Assets/Features/Alpha/Scripts"
    asmdef(a / "Contracts/Game.Alpha.Contracts.asmdef", "Game.Alpha.Contracts")
    put(a, "Contracts/Game.Alpha.Contracts.asmdef.meta", "fileFormatVersion: 2\nguid: " + "a" * 32 + "\n")
    put(a, "Contracts/IAlpha.cs", "namespace Game.Alpha { public interface IAlpha { int Value { get; } } }\n")
    asmdef(a / "Internal/Game.Alpha.Internal.asmdef", "Game.Alpha.Internal", ["Game.Alpha.Contracts"])
    put(a, "Internal/Alpha.cs", "using UnityEngine;\nnamespace Game.Alpha { internal sealed class Alpha : IAlpha "
                                "{ public int Value => Mathf.Max(1, 2); } }\n")
    b = up / "Assets/Features/Beta/Scripts"
    asmdef(b / "Internal/Game.Beta.Internal.asmdef", "Game.Beta.Internal", ["GUID:" + "a" * 32])
    put(b, "Internal/Beta.cs", "namespace Game.Beta { internal static class Beta { static int Get(Game.Alpha.IAlpha a) => a.Value; } }\n")
    asmdef(up / "Assets/Features/Gamma/Scripts/Internal/Game.Gamma.Internal.asmdef", "Game.Gamma.Internal",
           defineConstraints=["SOME_SYMBOL_NOBODY_SETS"])
    put(up, "Assets/Features/Gamma/Scripts/Internal/G.cs", "this does not compile")
    return up


def run(root, name, draft=False):
    ch = cc.Checker(root)
    folder = ch.folder_of(name)
    if draft:
        ch.apply_draft(name, folder)
    targets = sorted(ch.dependents(ch.in_folder(folder)))
    buf = io.StringIO()
    with redirect_stdout(buf):
        code = cc.report(ch, targets, quiet=False)
    return code, buf.getvalue(), ch


def pure_cases():
    check("range: plain version means that or newer", (cc.in_range("1.2.0", "1.0.0"), cc.in_range("0.9.0", "1.0.0")), (True, False))
    check("range: [a,b) and (a,b]", (cc.in_range("2022.3.20f1", "[2022.3.16f1,2023.1)"), cc.in_range("2023.1.0f1", "[2022.3.16f1,2023.1)"),
                                     cc.in_range("1.0.0", "(1.0.0,2.0.0]"), cc.in_range("2.0.0", "(1.0.0,2.0.0]")), (True, False, False, True))
    check("range: exact [a]", (cc.in_range("1.0.0", "[1.0.0]"), cc.in_range("1.0.1", "[1.0.0]")), (True, False))
    check("range: Unity 6 is newer than 2023.3.0b4", cc.in_range("6000.3.10f1", "2023.3.0b4"), True)
    a = cc.Asm("X", Path("."), {"defineConstraints": ["A || B", "!C"]}, True)
    check("constraints: alternatives and negation", (cc.constraints_met(a, ["B"]), cc.constraints_met(a, ["A", "C"]), cc.constraints_met(a, [])),
          (True, False, False))
    defs = cc.editor_defines("6000.3.10f1", Path("/nonexistent"))
    check("defines: the Editor's", all(d in defs for d in ("UNITY_EDITOR", "UNITY_6000_3_OR_NEWER", "UNITY_2023_3_OR_NEWER",
                                                            "UNITY_INCLUDE_TESTS", "UNITY_6000_3_10")), True)
    check("main: no arguments prints the usage", cc.main(["compile_check.py"]), 2)


def compile_cases():
    try:
        version = cc.unity_version(cc.ROOT / cc.UNITY_PROJECT)
        cc.unity_data(version)
        cc.csc_command()
    except cc.ToolMissing as ex:
        print(f"skip  compile cases: {ex}")
        return
    root = Path(tempfile.mkdtemp(prefix="cc-"))
    cache = Path(tempfile.mkdtemp(prefix="cc-cache-"))
    real_cache = cc.CACHE
    cc.CACHE = cache
    try:
        up = project(root, version)
        code, out, _ = run(root, "Alpha")
        check("clean: Alpha and what depends on it (Beta, by GUID) compile", (code, "3 OK" in out, "Game.Beta.Internal" in out), (0, True, True))
        code, out, _ = run(root, "Gamma")
        check("defineConstraints not met: the assembly is left out, its bad code too", (code, "BỎ   Game.Gamma.Internal" in out), (0, True))
        built = sorted(cache.rglob("result.json"))
        run(root, "Alpha")
        check("cache: a second run compiles nothing again", sorted(cache.rglob("result.json")), built)
        put(up, "Assets/Features/Alpha/Scripts/Internal/Alpha.cs", "namespace Game.Alpha { class Alpha { int x = \"a\"; } }\n")
        code, out, _ = run(root, "Alpha")
        check("an error of its own: LỖI with file:line: code", (code, "LỖI  Game.Alpha.Internal" in out,
              "UnityProject/Assets/Features/Alpha/Scripts/Internal/Alpha.cs:1: CS0029" in out), (1, True, True))
        put(up, "Assets/Features/Alpha/Scripts/Internal/Alpha.cs", "namespace Game.Alpha { class Alpha { } }\n")
        put(up, "Assets/Features/Alpha/Scripts/Contracts/IAlpha.cs", "namespace Game.Alpha { public interface IAlpha { int Value { get; } oops } }\n")
        code, out, _ = run(root, "Beta")
        check("a broken dependency outside the checked set: CHẶN, exit 3, its errors shown",
              (code, "CHẶN Game.Beta.Internal (phụ thuộc Game.Alpha.Contracts lỗi)" in out, "(ngoài phần kiểm)" in out), (3, True, True))
        put(up, "Assets/Features/Alpha/Scripts/Contracts/IAlpha.cs", "namespace Game.Alpha { public interface IAlpha { int Value { get; } } }\n")
        put(up, "Drafts/Alpha/Scripts/Internal/Alpha.cs", "namespace Game.Alpha { class Alpha { int y = \"b\"; } }\n")
        code, out, _ = run(root, "Alpha", draft=True)
        check("--draft: the draft is compiled instead of the file in Assets", (code, "Drafts/Alpha/Scripts/Internal/Alpha.cs:1" in out), (1, True))
        put(up, "Drafts/Alpha/Scripts/Internal/Alpha.cs", "namespace Game.Alpha { class Alpha { } }\n")
        put(up, "Drafts/Alpha/Scripts/Internal/Extra.cs", "namespace Game.Alpha { class Extra { int z = \"c\"; } }\n")
        code, out, _ = run(root, "Alpha", draft=True)
        check("--draft: a new file joins the assembly of its folder", (code, "Drafts/Alpha/Scripts/Internal/Extra.cs:1" in out), (1, True))
        put(up, "Drafts/Alpha/.draft.json", json.dumps({"delete": ["Scripts/Internal/Alpha.cs"]}))
        (up / "Drafts/Alpha/Scripts/Internal/Extra.cs").write_text("namespace Game.Alpha { class Extra { } }\n", encoding="utf-8")
        put(up, "Assets/Features/Alpha/Scripts/Internal/Alpha.cs", "broken in Assets but deleted by the draft")
        code, out, _ = run(root, "Alpha", draft=True)
        check("--draft: a file the draft deletes is left out", code, 0)
        try:
            cc.Checker(root).apply_draft("Beta", up / "Assets/Features/Beta")
            no_draft = False
        except ValueError:
            no_draft = True
        check("--draft without a draft folder is refused", no_draft, True)
        check("a name that is no feature is refused", _refused(lambda: cc.Checker(root).folder_of("../x")), True)
    finally:
        cc.CACHE = real_cache
        shutil.rmtree(root, ignore_errors=True)
        shutil.rmtree(cache, ignore_errors=True)


def _refused(fn):
    try:
        fn()
    except ValueError:
        return True
    return False


def main():
    pure_cases()
    compile_cases()
    failed = RESULTS.count(False)
    print(f"TOTAL: {RESULTS.count(True)} ok, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
