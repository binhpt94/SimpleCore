#!/usr/bin/env python3
"""Tests for validate.py.

    python Management/tools/test_validate.py

Builds a clean sample project in a temp folder, checks it has 0 violations, then breaks one
rule at a time and checks that rule is reported. The real project is not touched.
"""
import copy
import json
import os
import shutil
import subprocess
import sys
import tempfile
from fnmatch import fnmatchcase
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True   # no __pycache__ in the tools folder
sys.path.insert(0, str(HERE))
import validate as V  # noqa: E402

# The tests' own rules, fixed here: approving a package or an exception in the project's rules.json
# must not change what these cases expect. Same shape as rules.json.
RULES = {
    "extraAssetRoots": [],
    "allowedPackages": {
        "*": ["UniTask"],
        "View": ["UnityEngine.UI", "Unity.TextMeshPro"],
        "Tests": ["UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    },
    "allowedDlls": {"Tests": ["nunit.framework.dll"]},
    "bannedApis": [
        {"id": "reflection", "pattern": r"\bSystem\.Reflection\b|\bType\.GetType\s*\(|\bActivator\.CreateInstance\b",
         "reason": "reflection"},
        {"id": "send-message", "pattern": r"\b(SendMessage|SendMessageUpwards|BroadcastMessage)\s*\(", "reason": "by name"},
        {"id": "find-by-name", "pattern": r"\bGameObject\.Find\w*\s*\(", "reason": "by name"},
        {"id": "resources-load", "pattern": r"\bResources\.Load\w*\b", "reason": "by path"},
    ],
    "exceptions": [],
}
RULES_FILE = None  # the same rules written to the temp folder, for the command-line cases
A = "UnityProject/Assets"
F = A + "/Features"
G = {  # fixed GUIDs of the sample assets
    "walletIcon": "a0000000000000000000000000000001", "walletCfg": "a0000000000000000000000000000002",
    "walletView": "a0000000000000000000000000000003", "shopPrefab": "b0000000000000000000000000000001",
    "shopView": "b0000000000000000000000000000002", "popupFrame": "c0000000000000000000000000000001",
}
TESTS_EXTRA = {"defineConstraints": ["UNITY_INCLUDE_TESTS"], "precompiledReferences": ["nunit.framework.dll"],
               "overrideReferences": True}
IVT = '[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(%s)]'


def put(root, rel, text):
    p = Path(root) / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")


def put_meta(root, rel, guid):
    put(root, rel + ".meta", f"fileFormatVersion: 2\nguid: {guid}\n")


def put_asmdef(root, folder, name, refs, **extra):
    put(root, f"{folder}/{name}.asmdef", json.dumps({"name": name, "references": refs, "autoReferenced": False, **extra}, indent=2))


def yaml(*guids):
    return "%YAML 1.1\n--- !u!1 &1\nGameObject:\n" + "".join(f"  m_Ref: {{fileID: 11500000, guid: {g}, type: 3}}\n" for g in guids)


def set_json(root, rel, **changes):
    p = Path(root) / rel
    j = json.loads(p.read_text(encoding="utf-8"))
    j.update(changes)
    p.write_text(json.dumps(j), encoding="utf-8")


def add_ref(root, rel, ref):
    p = Path(root) / rel
    j = json.loads(p.read_text(encoding="utf-8"))
    j["references"].append(ref)
    p.write_text(json.dumps(j), encoding="utf-8")


# Things in here must NOT be flagged: text in comments and strings, public members and
# public nested types of internal types, generic constraints.
WALLET_LOGIC = r'''using UnityEngine;
namespace Game.Wallet
{
    // public class FakeInComment : MonoBehaviour {}
    /* public class AlsoFake {}  Resources.Load("x")  new PopupId("bad id") */
    internal sealed class WalletLogic : IWalletReader
    {
        public int Coins { get; private set; }
        public class Nested { }
        public enum Kind { A, B }
        const string Note = "public class InString : MonoBehaviour { GameObject.Find(\"x\") }";
        const string Verbatim = @"public class InVerbatim { } ""quoted"" SendMessage(";
        public bool TrySpend<T>(T x) where T : class { return true; }
    }
    internal class WalletConfig : ScriptableObject { public int startCoins; }
    internal record Entry(int Id);
}
'''


# Valid asset ids (SRC-05): explicit and target-typed `new`, a dotted name, a full type name, and the
# same string under another id type.
SHOP_IDS = f"{F}/Shop/Scripts/Internal/ShopPopups.cs"
SHOP_IDS_TEXT = '''namespace Game.Shop
{
    internal static class ShopPopups
    {
        public static readonly PopupId Bought = new PopupId("Shop/Bought");
        public static readonly PopupId Confirm = new("Shop/Buy.Confirm");
        public static PopupId Help => new Game.Services.Popup.PopupId( "Shop/Help" );
        public static readonly SoundId BoughtSound = new SoundId("Shop/Bought");
    }
}
'''


def build_clean(r):
    put(r, "CLAUDE.md", "# root\n" + "line\n" * 199)  # exactly at the 200-line limit: must stay clean
    put(r, ".claude/rules/unity.md", "line\n" * 40)  # exactly at the limit: must stay clean
    # Core
    put_asmdef(r, f"{A}/Core", "Game.Core", [])
    put(r, f"{A}/Core/IServiceRegistry.cs", "namespace Game.Core { public interface IServiceRegistry { void Register<T>(T s) where T : class; } }")
    put(r, f"{A}/Core/AssemblyInfo.cs", IVT % '"Game.Core.Tests"')
    # Wallet
    w = f"{F}/Wallet"
    put(r, f"{w}/CLAUDE.md", "# Wallet\nTầng: 6 · Features\n## Contracts\n### IWalletReader\n### SpendResult\n")
    put_asmdef(r, f"{w}/Scripts/Contracts", "Game.Wallet.Contracts", ["Game.Core"], noEngineReferences=True)
    put(r, f"{w}/Scripts/Contracts/IWalletReader.cs", "namespace Game.Wallet { public interface IWalletReader { int Coins { get; } } public readonly struct SpendResult { } }")
    put_asmdef(r, f"{w}/Scripts/Internal", "Game.Wallet.Internal", ["Game.Core", "Game.Wallet.Contracts", "Game.Services.Popup.Contracts", "Game.Shop.Contracts"])
    put(r, f"{w}/Scripts/Internal/AssemblyInfo.cs", 'using System.Runtime.CompilerServices;\n[assembly: InternalsVisibleTo("Game.Wallet.View")]\n[assembly: InternalsVisibleTo("Game.Wallet.Tests")]\n')
    put(r, f"{w}/Scripts/Internal/WalletInstaller.cs", "namespace Game.Wallet {\n  public static class WalletInstaller { public static void Install(Game.Core.IServiceRegistry r) { } }\n}")
    put(r, f"{w}/Scripts/Internal/WalletLogic.cs", WALLET_LOGIC)
    put_asmdef(r, f"{w}/Scripts/View", "Game.Wallet.View", ["Game.Core", "Game.Wallet.Internal", "Game.Wallet.Contracts", "UnityEngine.UI"])
    put(r, f"{w}/Scripts/View/WalletView.cs", "using UnityEngine;\nnamespace Game.Wallet { public class WalletView : MonoBehaviour { } }")
    put_meta(r, f"{w}/Scripts/View/WalletView.cs", G["walletView"])
    put_asmdef(r, f"{w}/Tests", "Game.Wallet.Tests", ["Game.Core", "Game.Wallet.Contracts", "Game.Wallet.Internal", "Game.Wallet.View", "Game.Shop.Contracts", "UnityEngine.TestRunner", "UnityEditor.TestRunner"], **TESTS_EXTRA)
    put(r, f"{w}/Tests/WalletTests.cs", "using System.Reflection;\nnamespace Game.Wallet.Tests { public class WalletTests { } }")
    put(r, f"{w}/Tests/IdTests.cs", 'namespace Game.Wallet.Tests { class IdTests { object X() { return new PopupId("not an id"); } } }')
    put(r, f"{w}/FeatureAssets/Public/Icon.prefab", yaml(G["walletView"]))
    put_meta(r, f"{w}/FeatureAssets/Public/Icon.prefab", G["walletIcon"])
    put(r, f"{w}/FeatureAssets/Private/Config.asset", yaml())
    put_meta(r, f"{w}/FeatureAssets/Private/Config.asset", G["walletCfg"])
    # Shop
    s = f"{F}/Shop"
    put(r, f"{s}/CLAUDE.md", "# Shop\nTầng: 6 · Features\n## Contracts\n### IShop\n")
    put_asmdef(r, f"{s}/Scripts/Contracts", "Game.Shop.Contracts", ["Game.Core"], noEngineReferences=True)
    put(r, f"{s}/Scripts/Contracts/IShop.cs", "namespace Game.Shop { public interface IShop { } }")
    put_asmdef(r, f"{s}/Scripts/Internal", "Game.Shop.Internal", ["Game.Core", "Game.Shop.Contracts", "Game.Wallet.Contracts"])
    put(r, f"{s}/Scripts/Internal/ShopInstaller.cs", "namespace Game.Shop;\npublic sealed class ShopInstaller { }\ninternal class ShopLogic { }")
    put(r, SHOP_IDS, SHOP_IDS_TEXT)
    put_asmdef(r, f"{s}/Scripts/View", "Game.Shop.View", ["Game.Core", "Game.Shop.Internal", "Game.Shop.Contracts"])
    put(r, f"{s}/Scripts/View/ShopView.cs", "using UnityEngine;\nnamespace Game.Shop { public class ShopView : MonoBehaviour { } }")
    put_meta(r, f"{s}/Scripts/View/ShopView.cs", G["shopView"])
    put(r, f"{s}/FeatureAssets/Public/Shop.prefab", yaml(G["shopView"], G["walletIcon"]))  # own script + Wallet's PUBLIC asset
    put_meta(r, f"{s}/FeatureAssets/Public/Shop.prefab", G["shopPrefab"])
    # Popup service
    p = f"{F}/_Services/Popup"
    put(r, f"{p}/CLAUDE.md", "# Popup\nTầng: 5 · Services\n## Contracts\n### IPopupService\n### PopupClosed\n### PopupId\n### PopupIds\n")
    put_asmdef(r, f"{p}/Scripts/Contracts", "Game.Services.Popup.Contracts", ["Game.Core"], noEngineReferences=True)
    put(r, f"{p}/Scripts/Contracts/IPopupService.cs", "namespace Game.Services.Popup { public interface IPopupService { } public delegate void PopupClosed(int id); }")
    put(r, f"{p}/Scripts/Contracts/PopupId.cs", "namespace Game.Services.Popup {\n"
        "  public readonly struct PopupId { public PopupId(string value) { Value = value; } public string Value { get; } }\n"
        '  public static class PopupIds { public static readonly PopupId Error = new PopupId("_Services/Popup/Error"); }\n}')
    put_asmdef(r, f"{p}/Scripts/Internal", "Game.Services.Popup.Internal", ["Game.Core", "Game.Services.Popup.Contracts"])
    put(r, f"{p}/Scripts/Internal/PopupInstaller.cs", "namespace Game.Services.Popup { public static class PopupInstaller { } internal class PopupQueue { } }")
    put(r, f"{p}/FeatureAssets/Private/Frame.prefab", yaml())
    put_meta(r, f"{p}/FeatureAssets/Private/Frame.prefab", G["popupFrame"])
    # Composition
    put_asmdef(r, f"{A}/_Composition", "Game.Composition", ["Game.Core", "Game.Wallet.Internal", "Game.Shop.Internal", "Game.Services.Popup.Internal"])
    put(r, f"{A}/_Composition/Boot.cs", "namespace Game.Composition { public static class Boot { } }")
    put(r, f"{A}/_Composition/Main.unity", yaml(G["shopPrefab"], G["walletIcon"]))


WI = f"{F}/Wallet/Scripts/Internal"
WI_ASM = f"{WI}/Game.Wallet.Internal.asmdef"
WC_ASM = f"{F}/Wallet/Scripts/Contracts/Game.Wallet.Contracts.asmdef"
WV_ASM = f"{F}/Wallet/Scripts/View/Game.Wallet.View.asmdef"
WT_ASM = f"{F}/Wallet/Tests/Game.Wallet.Tests.asmdef"
POPUP_I_ASM = f"{F}/_Services/Popup/Scripts/Internal/Game.Services.Popup.Internal.asmdef"
POPUP_C_ASM = f"{F}/_Services/Popup/Scripts/Contracts/Game.Services.Popup.Contracts.asmdef"
COMP_ASM = f"{A}/_Composition/Game.Composition.asmdef"
NS = "namespace Game.Wallet { %s }"


def core_base(d):
    put(d, f"{A}/Core/CoreBehaviour.cs", "namespace Game.Core { public abstract class CoreBehaviour : UnityEngine.MonoBehaviour { } }")
    put(d, f"{WI}/Bad.cs", NS % "internal class Bad : Game.Core.CoreBehaviour { }")


# (rule, what is broken, how to break it, optional pattern the message must match)
CASES = [
    ("STR-01", "Internal asmdef missing", lambda d: (d / WI_ASM).unlink()),
    ("STR-02", ".cs in FeatureAssets", lambda d: put(d, f"{F}/Wallet/FeatureAssets/Private/Hack.cs", "class Hack {}")),
    ("STR-02", ".cs at feature root", lambda d: put(d, f"{F}/Wallet/Loose.cs", "class Loose {}")),
    ("STR-03", "extra asmdef in a subfolder", lambda d: put(d, f"{WI}/Sub/Extra.asmdef", '{"name":"Extra"}')),
    ("STR-03", "asmdef file wrongly named", lambda d: (d / f"{F}/Shop/Scripts/View/Game.Shop.View.asmdef").rename(d / f"{F}/Shop/Scripts/View/ShopView.asmdef")),
    ("STR-04", ".asmref into another feature", lambda d: put(d, f"{WI}/Steal/x.asmref", '{"reference":"Game.Shop.Internal"}')),
    ("STR-05", "Shared folder beside features", lambda d: put(d, f"{F}/Shared/Scripts/Internal/U.cs", "class U {}")),
    ("STR-05", "Shared folder inside a feature", lambda d: put(d, f"{WI}/Shared/U.cs", "class U {}")),
    ("STR-06", "unknown folder in Assets root", lambda d: put(d, f"{A}/Stuff/readme.txt", "x")),
    ("STR-06", "feature name not PascalCase", lambda d: put(d, f"{F}/bad_name/CLAUDE.md", "x")),
    ("STR-06", "reserved feature name", lambda d: put(d, f"{F}/Core/CLAUDE.md", "x")),
    ("STR-07", "unexpected folder in a feature", lambda d: put(d, f"{F}/Wallet/Extra/a.txt", "x")),
    ("STR-07", "unexpected folder under Scripts", lambda d: put(d, f"{F}/Wallet/Scripts/Utils/a.txt", "x")),
    ("DOC-01", "feature CLAUDE.md over 200 lines", lambda d: put(d, f"{F}/Wallet/CLAUDE.md", "line\n" * 201)),
    ("DOC-01", "root CLAUDE.md over 200 lines", lambda d: put(d, "CLAUDE.md", "line\n" * 201)),
    ("DOC-01", ".claude/rules md over 200 lines", lambda d: put(d, ".claude/rules/unity.md", "line\n" * 201), "*unity.md*"),
    ("DOC-02", "feature with Contracts and no CLAUDE.md", lambda d: (d / f"{F}/Shop/CLAUDE.md").unlink(), "*no CLAUDE.md*IShop*"),
    ("DOC-02", "CLAUDE.md without the tier line", lambda d: put(d, f"{F}/Shop/CLAUDE.md", "# Shop\n### IShop\n"), "*no tier line*"),
    ("DOC-02", "service CLAUDE.md with the feature tier", lambda d: put(d, f"{F}/_Services/Popup/CLAUDE.md",
        "# Popup\nTầng: 6 · Features\n### IPopupService PopupClosed PopupId PopupIds\n"), "*this folder is 'Tầng: 5 · Services'*"),
    ("DOC-02", "a public Contracts type (struct) not described", lambda d: put(d, f"{F}/Wallet/CLAUDE.md",
        "# Wallet\nTầng: 6 · Features\n### IWalletReader\n"), "*types: SpendResult"),
    ("DOC-02", "a public delegate not described", lambda d: put(d, f"{F}/_Services/Popup/CLAUDE.md",
        "# Popup\nTầng: 5 · Services\n### IPopupService PopupId PopupIds\n"), "*types: PopupClosed"),

    ("ASM-00", "asmdef is not JSON", lambda d: put(d, WI_ASM, "{ broken")),
    ("ASM-01", "wrong assembly name", lambda d: set_json(d, WI_ASM, name="Game.Shop.Internal")),
    ("ASM-02", "autoReferenced true", lambda d: set_json(d, WI_ASM, autoReferenced=True)),
    ("ASM-02", "autoReferenced missing", lambda d: put(d, WI_ASM, '{"name":"Game.Wallet.Internal","references":[]}')),
    ("ASM-03", "reference by GUID", lambda d: add_ref(d, WI_ASM, "GUID:0123456789abcdef0123456789abcdef")),
    ("ASM-04", "Internal -> other Internal", lambda d: add_ref(d, WI_ASM, "Game.Shop.Internal")),
    ("ASM-04", "Internal -> other View", lambda d: add_ref(d, WI_ASM, "Game.Shop.View")),
    ("ASM-04", "Internal -> own View", lambda d: add_ref(d, WI_ASM, "Game.Wallet.View"), "*View -> Internal*"),
    ("ASM-04", "Contracts -> other Contracts", lambda d: add_ref(d, WC_ASM, "Game.Shop.Contracts")),
    ("ASM-04", "Contracts -> own Internal", lambda d: add_ref(d, WC_ASM, "Game.Wallet.Internal")),
    ("ASM-04", "View -> other Contracts", lambda d: add_ref(d, WV_ASM, "Game.Shop.Contracts")),
    ("ASM-04", "View -> other Internal", lambda d: add_ref(d, WV_ASM, "Game.Shop.Internal")),
    ("ASM-04", "Tests -> other Internal", lambda d: add_ref(d, WT_ASM, "Game.Shop.Internal")),
    ("ASM-04", "service -> feature Contracts", lambda d: add_ref(d, POPUP_I_ASM, "Game.Wallet.Contracts"), "*service may not know*"),
    ("ASM-04", "_Composition -> Contracts", lambda d: add_ref(d, COMP_ASM, "Game.Wallet.Contracts")),
    ("ASM-04", "_Composition -> View", lambda d: add_ref(d, COMP_ASM, "Game.Wallet.View")),
    ("ASM-04", "Core -> feature Contracts", lambda d: add_ref(d, f"{A}/Core/Game.Core.asmdef", "Game.Wallet.Contracts")),
    ("ASM-04", "package not on the allow-list", lambda d: add_ref(d, WI_ASM, "Some.Unlisted.Package"), "*allowedPackages*"),
    ("ASM-04", "UI package in Internal", lambda d: add_ref(d, WI_ASM, "UnityEngine.UI"), "*allowedPackages*"),
    ("ASM-05", "Contracts with engine references", lambda d: set_json(d, WC_ASM, noEngineReferences=False)),
    ("ASM-06", "precompiled DLL in Internal", lambda d: set_json(d, WI_ASM, precompiledReferences=["Evil.dll"])),
    ("ASM-07", "Tests without UNITY_INCLUDE_TESTS", lambda d: set_json(d, WT_ASM, defineConstraints=[])),

    ("SRC-01", "public non-installer class", lambda d: put(d, f"{WI}/Leak.cs", NS % "public class Leak { }")),
    ("SRC-01", "public interface, file-scoped ns", lambda d: put(d, f"{WI}/Leak.cs", "namespace Game.Wallet;\npublic interface ILeak { }")),
    ("SRC-01", "public struct, no namespace", lambda d: put(d, f"{WI}/Leak.cs", "public readonly struct Leak { }")),
    ("SRC-01", "public enum", lambda d: put(d, f"{WI}/Leak.cs", NS % "public enum E { A }"), "*'E'*"),
    ("SRC-01", "public record", lambda d: put(d, f"{WI}/Leak.cs", NS % "public record R(int X);"), "*'R'*"),
    ("SRC-01", "public record struct", lambda d: put(d, f"{WI}/Leak.cs", NS % "public record struct RS(int X);"), "*'RS'*"),
    ("SRC-01", "public delegate", lambda d: put(d, f"{WI}/Leak.cs", NS % "public delegate void D(int x);"), "*'D'*"),
    ("SRC-01", "second public installer", lambda d: put(d, f"{WI}/Second.cs", NS % "public static class OtherInstaller { }"), "*second public installer*"),
    ("SRC-01", "public type nested in installer", lambda d: put(d, f"{WI}/WalletInstaller.cs", NS % "public static class WalletInstaller { public class Hidden { } }"), "*'Hidden'*"),
    ("SRC-01", "public partial sealed class", lambda d: put(d, f"{WI}/Leak.cs", NS % "public sealed partial class Leak { }")),
    ("SRC-02", "MonoBehaviour in Internal", lambda d: put(d, f"{WI}/Bad.cs", NS % "internal class Bad : UnityEngine.MonoBehaviour { }")),
    ("SRC-02", "MonoBehaviour with interfaces", lambda d: put(d, f"{WI}/Bad.cs", NS % "internal sealed class Bad<T> : MonoBehaviour, IWalletReader where T : class { }")),
    ("SRC-02", "indirect, base in same Internal", lambda d: put(d, f"{WI}/Bad.cs", NS % "internal class Base : MonoBehaviour { } internal class Bad : Base { }"), "*'Bad'*"),
    ("SRC-02", "indirect, base in Core", core_base, "*'Bad'*"),
    ("SRC-03", "InternalsVisibleTo other feature", lambda d: put(d, f"{WI}/AssemblyInfo.cs", IVT % '"Game.Shop.Internal"')),
    ("SRC-03", "InternalsVisibleTo _Composition", lambda d: put(d, f"{WI}/AssemblyInfo.cs", IVT % '"Game.Composition"')),
    ("SRC-03", "InternalsVisibleTo in another file", lambda d: put(d, f"{WI}/Sneaky.cs", IVT % '"Game.Wallet.Tests"')),
    ("SRC-03", "InternalsVisibleTo in Contracts", lambda d: put(d, f"{F}/Wallet/Scripts/Contracts/AssemblyInfo.cs", IVT % '"Game.Wallet.Tests"')),
    ("SRC-03", "InternalsVisibleTo via constant", lambda d: put(d, f"{WI}/AssemblyInfo.cs", IVT % "Names.Other")),
    ("SRC-04", "System.Reflection", lambda d: put(d, f"{WI}/R.cs", "using System.Reflection;\n" + NS % "internal class R { }")),
    ("SRC-04", "Type.GetType", lambda d: put(d, f"{WI}/R.cs", NS % 'internal class R { object X() { return System.Type.GetType("Game.Shop.ShopLogic"); } }')),
    ("SRC-04", "SendMessage in View", lambda d: put(d, f"{F}/Wallet/Scripts/View/S.cs", NS % 'class S : UnityEngine.MonoBehaviour { void X() { SendMessage("Buy"); } }')),
    ("SRC-04", "GameObject.Find in View", lambda d: put(d, f"{F}/Wallet/Scripts/View/S.cs", NS % 'class S : UnityEngine.MonoBehaviour { void X() { var g = UnityEngine.GameObject.Find("Shop"); } }')),
    ("SRC-04", "Resources.Load in _Composition", lambda d: put(d, f"{A}/_Composition/L.cs", 'namespace Game.Composition { class L { void X() { UnityEngine.Resources.Load("p"); } } }')),
    ("SRC-05", "id created in View", lambda d: put(d, f"{F}/Wallet/Scripts/View/I.cs", NS % 'class I { object X = new PopupId("Wallet/Low"); }'), "*only in Scripts/Contracts or Scripts/Internal*"),
    ("SRC-05", "target-typed id in View", lambda d: put(d, f"{F}/Wallet/Scripts/View/I.cs", NS % 'class I { PopupId X = new("Wallet/Low"); }'), "*only in Scripts/Contracts or Scripts/Internal*"),
    ("SRC-05", "id created in _Composition", lambda d: put(d, f"{A}/_Composition/I.cs", 'namespace Game.Composition { class I { object X = new PopupId("Shop/Bought"); } }'), "*only in Scripts/Contracts or Scripts/Internal*"),
    ("SRC-05", "id created in Core", lambda d: put(d, f"{A}/Core/I.cs", 'namespace Game.Core { class I { object X = new PopupId("Shop/Bought"); } }'), "*only in Scripts/Contracts or Scripts/Internal*"),
    ("SRC-05", "id from a variable", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X(string s) { return new PopupId(s); } }'), "*plain string literal*"),
    ("SRC-05", "id from interpolation", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X(int n) { return new PopupId($"Wallet/Low{n}"); } }'), "*plain string literal*"),
    ("SRC-05", "id from concatenation", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId("Wallet/" + "Low"); }'), "*plain string literal*"),
    ("SRC-05", "id from verbatim string", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId(@"Wallet/Low"); }'), "*plain string literal*"),
    ("SRC-05", "id with lowercase name", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId("Wallet/low"); }'), "*not a valid asset id*"),
    ("SRC-05", "id without owner", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId("Low"); }'), "*not a valid asset id*"),
    ("SRC-05", "id with two slashes", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId("Wallet/Low/Coins"); }'), "*not a valid asset id*"),
    ("SRC-05", "target-typed id, bad string", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { static readonly PopupId X = new("bad"); }'), "*not a valid asset id*"),
    ("SRC-05", "id of another feature", lambda d: put(d, f"{WI}/I.cs", NS % 'class I { object X = new PopupId("Shop/Low"); }'), "*belongs to another feature*"),
    ("SRC-05", "service id without _Services/", lambda d: put(d, f"{F}/_Services/Popup/Scripts/Internal/I.cs", 'namespace Game.Services.Popup { class I { object X = new PopupId("Popup/Error"); } }'), "*belongs to another feature*"),
    ("SRC-05", "same id created twice", lambda d: put(d, f"{F}/Shop/Scripts/Internal/Again.cs", 'namespace Game.Shop { class Again { object X = new PopupId("Shop/Bought"); } }'), "*'Shop/Bought' is already created at*"),

    ("AST-01", "prefab -> other Private asset", lambda d: put(d, f"{F}/Shop/FeatureAssets/Public/Shop.prefab", yaml(G["walletCfg"]))),
    ("AST-01", "prefab -> other View script", lambda d: put(d, f"{F}/Shop/FeatureAssets/Private/P.prefab", yaml(G["walletView"]))),
    ("AST-01", "scene in _Composition -> Private", lambda d: put(d, f"{A}/_Composition/Main.unity", yaml(G["walletCfg"]))),
    ("AST-01", "Core asset -> feature Public", lambda d: put(d, f"{A}/Core/Thing.asset", yaml(G["walletIcon"])), "*Core and services*"),
    ("AST-01", "service asset -> feature Public", lambda d: put(d, f"{F}/_Services/Popup/FeatureAssets/Private/Frame.prefab", yaml(G["walletIcon"])), "*Core and services*"),
    ("AST-01", "feature -> service Private", lambda d: put(d, f"{F}/Shop/FeatureAssets/Private/P.prefab", yaml(G["popupFrame"]))),
    ("AST-01", "reference hidden in a .meta", lambda d: put(d, f"{F}/Shop/FeatureAssets/Public/Shop.prefab.meta", f"fileFormatVersion: 2\nguid: {G['shopPrefab']}\nextra: {{fileID: 1, guid: {G['walletCfg']}, type: 2}}\n")),
    ("AST-02", "binary-serialized prefab", lambda d: put(d, f"{F}/Shop/FeatureAssets/Private/Bin.prefab", "BIN\0\0data")),
]

results = []


def report(name, ok, detail=""):
    results.append((ok, name, detail))


def rules_of(result):
    return [v["rule"] for v in result["violations"]]


def run_cli(root, *args, env_feature=None):
    env = dict(os.environ)
    env.pop("FEATURE", None)
    if env_feature is not None:
        env["FEATURE"] = env_feature
    p = subprocess.run([sys.executable, str(HERE / "validate.py"), "--root", str(root), "--rules", str(RULES_FILE), "--json", *args],
                       capture_output=True, text=True, env=env)
    try:
        return p.returncode, json.loads(p.stdout)
    except ValueError:
        return p.returncode, None


def main():
    global RULES_FILE
    base = Path(tempfile.mkdtemp(prefix="valfx-"))
    RULES_FILE = base / "rules.json"
    RULES_FILE.write_text(json.dumps(RULES), encoding="utf-8")
    try:
        clean = base / "clean"
        build_clean(clean)

        r = V.validate(clean, RULES)
        report("clean project: 0 violations, 3 features", not r["violations"] and len(r["features"]) == 3,
               "; ".join(f"{v['rule']} {v['file']}" for v in r["violations"]))

        for rule, name, breaker, *like in CASES:
            d = base / "case"
            if d.exists():
                shutil.rmtree(d)
            shutil.copytree(clean, d)
            breaker(d)
            r = V.validate(d, RULES)
            hits = [v for v in r["violations"] if v["rule"] == rule and fnmatchcase(v["message"], like[0] if like else "*")]
            others = sorted({x for x in rules_of(r) if x != rule})
            report(f"{rule}  {name}", bool(hits), f"hits={len(hits)}" + (f" also:{','.join(others)}" if others else ""))

        # ----- exceptions and scope
        d = base / "case"
        shutil.rmtree(d)
        shutil.copytree(clean, d)
        set_json(d, POPUP_C_ASM, noEngineReferences=False)
        put(d, f"{F}/Shop/Scripts/Internal/Leak.cs", "public class Leak { }")

        ok_rules = copy.deepcopy(RULES)
        ok_rules["exceptions"] = [{"rule": "ASM-05", "path": f"{F}/_Services/Popup/Scripts/Contracts/*",
                                   "reason": "popup takes a prefab", "approvedBy": "tester", "date": "2026-01-01"}]
        r = V.validate(d, ok_rules)
        report("approved exception moves ASM-05 to 'excepted'", "ASM-05" not in rules_of(r) and len(r["excepted"]) == 1)

        bad_rules = copy.deepcopy(RULES)
        bad_rules["exceptions"] = [{"rule": "ASM-05", "path": "*", "reason": "", "approvedBy": ""}]
        r = V.validate(d, bad_rules)
        report("exception without reason is ignored (CFG-01)", rules_of(r).count("CFG-01") == 1 and "ASM-05" in rules_of(r))

        r = V.validate(d, RULES, "Wallet")
        report("feature Wallet: clean, 2 hidden", not r["violations"] and r["hiddenCount"] == 2)
        r = V.validate(d, RULES, "Shop")
        report("feature Shop: sees only its own SRC-01", rules_of(r) == ["SRC-01"])
        r = V.validate(d, RULES, "_Services/Popup")
        report("feature _Services/Popup: sees only its ASM-05", rules_of(r) == ["ASM-05"])

        # ----- command line: exit codes and scope; the FEATURE environment variable is ignored
        code, j = run_cli(clean, "--all")
        report("CLI clean project -> exit 0", code == 0 and j is not None and not j["violations"], f"exit={code}")
        code, j = run_cli(d, "--feature", "Shop")
        report("CLI --feature Shop -> its own violation", code == 1 and j and j["scope"] == "Shop", f"exit={code}")
        code, j = run_cli(d, "--feature", "Wallet")
        report("CLI --feature Wallet -> exit 0", code == 0 and j and j["hiddenCount"] == 2, f"exit={code}")
        code, j = run_cli(d)
        report("CLI no --feature -> whole project", code == 1 and j and j["scope"] == "all" and len(j["violations"]) == 2, f"exit={code}")
        code, j = run_cli(d, env_feature="Wallet")
        report("CLI FEATURE=Wallet in the environment is ignored", code == 1 and j and j["scope"] == "all", f"exit={code}")
        code, j = run_cli(d, "--all", "--feature", "Wallet")
        report("CLI --all with --feature -> usage error", code == 2 and j is None, f"exit={code}")
        code, j = run_cli(base / "nope", "--all")
        report("CLI missing project -> exit 2, not 'clean'", code == 2, f"exit={code}")
    finally:
        shutil.rmtree(base, ignore_errors=True)

    failed = [r for r in results if not r[0]]
    for ok, name, detail in results:
        print(f"{'ok  ' if ok else 'FAIL'}  {name:<52} {detail}")
    print(f"\nTOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
