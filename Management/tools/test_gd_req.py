#!/usr/bin/env python3
"""Tests for the requirement book (reqbook.py, gd_req.py), on a copy of the Wallet design in a temp folder.

    python Management/tools/test_gd_req.py
"""
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import reqbook as rb  # noqa: E402

REAL = HERE.parent.parent
results = []
NODE = "feature-wallet/balance-spend/wallet-rules"
TESTS = """using NUnit.Framework;
namespace Game.Wallet.Tests
{
    public class WalletLogicTests
    {
        [Test]
        public void StartsWithConfiguredCoins() { }

        [TestCase(1, 2)] [TestCase(3, 4)]
        public void Adds(int a, int b) { }

        // [Test] public void Commented() { }
        public void NotATest() { }
    }
}
"""


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def make_root():
    root = Path(tempfile.mkdtemp(prefix="gdreq-"))
    for table in ("live", "idea"):
        src = gd.table_dir(REAL, table) / "feature-wallet"
        shutil.copytree(src, gd.table_dir(root, table) / "feature-wallet",
                        ignore=shutil.ignore_patterns(gd.PROGRESS_DIR, gd.REVIEW_DIR, gd.HISTORY_DIR))
    tests = root / "UnityProject/Assets/Features/Wallet/Tests"
    tests.mkdir(parents=True)
    (tests / "WalletLogicTests.cs").write_text(TESTS, encoding="utf-8")
    return root


def live_file(root, name, path=NODE):
    return gd.table_dir(root, "live").joinpath(*path.split("/"), name)


def edit(root, name, old, new, path=NODE):
    f = live_file(root, name, path)
    text = f.read_text(encoding="utf-8")
    assert old in text, old
    f.write_text(text.replace(old, new, 1), encoding="utf-8")


def state(root, rid, path=NODE):
    v = rb.view(root, "Wallet")
    for n in v["nodes"]:
        for r in n["reqs"]:
            if r["ref"] == f"{path}#{rid}":
                return r["state"]
    return None


def refused(fn):
    try:
        fn()
        return False
    except ValueError:
        return True


def ids(root, path=NODE):
    n = [n for n in rb.view(root, "Wallet")["nodes"] if n["path"] == path][0]
    return [r["id"] for r in n["reqs"]]


def cli(root, *args):
    p = subprocess.run([sys.executable, str(HERE / "gd_req.py"), *args, "--root", str(root)], capture_output=True,
                       text=True, encoding="utf-8")
    return p.returncode, p.stdout + p.stderr


def run_list(root):
    got = ids(root)
    check("wallet-rules: 7 ideas + 7 edge cases + params = 15 requirements", len(got), 15)
    check("ideas keep their ids", got[:3], ["start-50", "integer-non-negative", "spend-equal-ok"])
    check("an edge case gets a slug of its bold title", "edge-tien-vua-du-dung-bang-gia" in got)
    check("params is the last one", got[-1], "params")
    check("everything starts todo", {r["state"] for n in rb.view(root, "Wallet")["nodes"] for r in n["reqs"]}, {"todo"})
    check("test names: [Test] and [TestCase], not commented, not plain methods",
          rb.test_names([root / "UnityProject/Assets/Features/Wallet"]),
          {"WalletLogicTests.StartsWithConfiguredCoins", "WalletLogicTests.Adds"})
    check("edge lines without bold: title is the text before ':'",
          rb.edge_items("- Mua khi thành bị phá: không nhận lệnh.\n  - chi tiết\n- **Đậm**: x"),
          [("Mua khi thành bị phá", "Mua khi thành bị phá: không nhận lệnh.\n- chi tiết"), ("Đậm", "**Đậm**: x")])


def run_flow(root):
    W = "Wallet"
    check("done refuses a test that does not exist",
          refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["WalletLogicTests.Nope"])))
    check("done refuses no evidence", refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=[])))
    check("doing needs a note", refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "doing", W, note="")))
    rb.mark(root, W, "wallet-rules#start-50", "doing", W, note="viết Reset")
    check("doing is saved with its note", state(root, "start-50"), "doing")
    check("the line for the chat names what it is doing", "wallet-rules#start-50 (viết Reset)" in rb.brief(root, W))
    check("verify refuses a requirement that is not done",
          refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "verify", "@CodeLead")))
    for bad in ("Game..WalletLogicTests.StartsWithConfiguredCoins", "Game.1x.WalletLogicTests.StartsWithConfiguredCoins"):
        check("done refuses a malformed name: " + bad, refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=[bad])))
    rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["Game.Wallet.Tests.WalletLogicTests.StartsWithConfiguredCoins"])
    check("done with a real test: waiting for @CodeLead", state(root, "start-50"), "done")
    check("done drops the namespace: the book keeps Lớp.Hàm",
          [(r["saved"] or {}).get("tests") for n in rb.view(root, W)["nodes"] for r in n["reqs"] if r["ref"].endswith("#start-50")],
          [["WalletLogicTests.StartsWithConfiguredCoins"]])
    rb.mark(root, W, "feature-wallet/balance-spend/wallet-rules#start-50", "verify", "@CodeLead")
    check("verify (full path) makes it met", state(root, "start-50"), "verified")
    check("unknown requirement is refused", refused(lambda: rb.mark(root, W, "wallet-rules#nope", "doing", W, note="x")))
    rb.mark(root, W, "wallet-rules#cap-max", "done", W, play="cộng vượt trần, số dừng ở 999.999.999")
    rb.mark(root, W, "wallet-rules#cap-max", "reject", "@CodeLead", note="chưa có test trần")
    check("reject sends it back with the reason", state(root, "cap-max"), "rejected")
    boot = root / "UnityProject/Assets/_Composition/Tests"
    boot.mkdir(parents=True, exist_ok=True)
    (boot / "BootTests.cs").write_text("using NUnit.Framework;\nnamespace Game.Composition.Tests\n{\n    public class BootTests\n    {\n"
                                       "        [Test]\n        public void GameScene_JoinsItsFeatures() { }\n    }\n}\n", encoding="utf-8")
    rb.mark(root, W, "wallet-rules#cap-max", "done", W, tests=["BootTests.GameScene_JoinsItsFeatures"])
    check("done: a wiring requirement may cite a test in _Composition/Tests (BootTests, @CodeLead's)", state(root, "cap-max"), "done")
    try:
        rb.mark(root, W, "wallet-rules#cap-max", "done", W, tests=["BootTests.Nope"])
        msg = ""
    except ValueError as ex:
        msg = str(ex)
    check("done: a test found nowhere is refused, and the message names both places",
          ("Features/Wallet/Tests" in msg, "_Composition/Tests" in msg), (True, True))
    (boot / "BootTests.cs").unlink()
    check("a _Composition test that is gone makes the requirement 'mất test'", state(root, "cap-max"), "no-test")
    rb.mark(root, W, "wallet-rules#cap-max", "reject", "@CodeLead", note="chưa có test trần")
    import dispatch
    import gd_req
    said = gd_req.reject_notices(root, W, "wallet-rules#cap-max", "chưa có test trần")
    again = gd_req.reject_notices(root, W, "wallet-rules#cap-max", "chưa có test trần")
    ms = dispatch.messages(root, W)
    check("reject: a message in the feature's inbox, with the full ref, the reason and a check; run twice, one message",
          (len(ms), ms[0]["source"], ms[0]["key"], "chưa có test trần" in ms[0]["text"], ms[0]["check"].startswith("req-rejected"),
           "Đã đưa tin" in said[0], "đã có tin này" in again[0]),
          (1, "gd_req", "reject:" + NODE + "#cap-max", True, True, True, True))


ROW_TESTS = """using NUnit.Framework;
namespace Game.Wallet.Tests
{
    public class EnemyTableTests
    {
        [Test]
        public void Soldier_HasItsStats() { var e = Find("soldier"); }

        [Test]
        public void Wolf_IsFasterThanSoldier() { }

        [Test]
        public void Unrelated() { }

        [Test]
        public void ShotRange_IsSix() { }

        [Test]
        public void ReadsBoss() { var e = Find("big_boss"); }
    }
}
"""


def put_table(root, sheet, kind, key, columns, rows, path=NODE):
    """Writes one table (and its Index row) into the .excel of a live node: the csv side is what the book reads."""
    gx = gd.gd_excel
    d = gd.table_dir(root, "live").joinpath(*path.split("/"), gx.EXCEL_DIR)
    d.mkdir(parents=True, exist_ok=True)
    index = gx.read_csv(d / "Index.csv") if (d / "Index.csv").is_file() else gx.index_head()
    index = [r for r in index if r[1] != sheet or r in index[:gx.HEAD_ROWS]]
    index.append([str(len(index) - gx.HEAD_ROWS + 1), sheet, kind, key, str(len(rows)), "Bảng " + sheet])
    gx.write_csv(d / "Index.csv", index)
    head = [["index"] + [c[0] for c in columns], ["int"] + [c[1] for c in columns], ["Số thứ tự"] + [c[0] for c in columns]]
    gx.write_csv(d / f"{sheet}.csv", head + [[str(n)] + list(r) for n, r in enumerate(rows, start=1)])


def run_tables(root):
    """The config tables of a node (.excel): a row of a list, params or formula table is a requirement of its own."""
    W = "Wallet"
    (root / "UnityProject/Assets/Features/Wallet/Tests/EnemyTableTests.cs").write_text(ROW_TESTS, encoding="utf-8")
    before = {r["id"]: r["hash"] for n in rb.view(root, W)["nodes"] for r in n["reqs"]}
    cols = (("id", "string"), ("name", "string"), ("stage", "enum:idea|prototype|full"), ("max_health", "int"))
    soldier, wolf = ("soldier", "Lính", "idea", "40"), ("wolf", "Sói", "idea", "25")
    put_table(root, "Enemies", "list", "id", cols, [soldier])
    put_table(root, "ShotParams", "params", "key", (("key", "string"), ("name", "string"), ("unit", "string"), ("idea", "string"),
                                                    ("prototype", "string"), ("full", "string"), ("reason", "string")),
              [("shot_range", "Tầm", "u", "6", "6", "6", "x"), ("bullet_visual", "Hình đạn", "", "1 sprite", "", "", "x")])
    put_table(root, "WaveLevels", "levels", "level", (("level", "int"), ("count", "int")), [("1", "8"), ("2", "10")])
    put_table(root, "Counters", "map", "", (("from", "string"), ("to", "string"), ("value", "float")), [("soldier", "wolf", "1.5")])
    v = rb.view(root, W)
    node = next(n for n in v["nodes"] if n["path"] == NODE)
    new = {r["id"]: r for r in node["reqs"] if r["id"] not in before}
    check("tables: a row of a list or params table is one requirement, a levels or map table is one for the whole table",
          sorted((k, r["kind"]) for k, r in new.items()),
          [("counters", "table"), ("enemies-soldier", "row"), ("shotparams-bullet-visual", "row"), ("shotparams-shot-range", "row"),
           ("wavelevels", "table")])
    check("tables: the requirements the node had keep their hash",
          {r["id"]: r["hash"] for n in v["nodes"] for r in n["reqs"] if r["id"] in before}, before)
    check("tables: a row names its key and holds its own values",
          (new["enemies-soldier"]["key"], "max_health (int): 40" in new["enemies-soldier"]["text"], new["enemies-soldier"]["title"]),
          ("soldier", True, "Enemies: Lính (soldier)"))

    T = "EnemyTableTests."
    check("done: a test that does not name the key is refused",
          refused(lambda: rb.mark(root, W, "wallet-rules#enemies-soldier", "done", W, tests=[T + "Unrelated", T + "Wolf_IsFasterThanSoldier".replace("Soldier", "X")])))
    check("done: an old test of another thing is refused", refused(lambda: rb.mark(root, W, "wallet-rules#enemies-soldier", "done", W, tests=[T + "Unrelated"])))
    check("done: a row of a list is not proven by play alone",
          refused(lambda: rb.mark(root, W, "wallet-rules#enemies-soldier", "done", W, play="thấy Lính đi ra")))
    rb.mark(root, W, "wallet-rules#enemies-soldier", "done", W, tests=[T + "Soldier_HasItsStats", T + "Unrelated"])
    rb.mark(root, W, "wallet-rules#enemies-soldier", "verify", "@CodeLead")
    check("done: one test naming the key is enough; then verified", state(root, "enemies-soldier"), "verified")
    rb.mark(root, W, "wallet-rules#shotparams-shot-range", "done", W, tests=[T + "ShotRange_IsSix"])
    check("done: the key is found however it is written (shot_range, ShotRange)", state(root, "shotparams-shot-range"), "done")
    rb.mark(root, W, "wallet-rules#shotparams-bullet-visual", "done", W, play="viên đạn bay thẳng 0.15s")
    check("done: a row of a params table may still be shown by play", state(root, "shotparams-bullet-visual"), "done")
    check("done: but its tests, when given, must name the key",
          refused(lambda: rb.mark(root, W, "wallet-rules#shotparams-bullet-visual", "done", W, tests=[T + "Unrelated"])))
    rb.mark(root, W, "wallet-rules#wavelevels", "done", W, tests=[T + "Unrelated"])
    check("done: a whole-table requirement keeps the old rule", state(root, "wavelevels"), "done")

    put_table(root, "Enemies", "list", "id", cols, [soldier, wolf])
    check("a new row is a new requirement to do; the rows that were there keep their state",
          (state(root, "enemies-wolf"), state(root, "enemies-soldier"), state(root, "shotparams-shot-range")), ("todo", "verified", "done"))
    line = rb.render(rb.view(root, W))
    check("the book tells the feature which key the test must name", "test phải nhắc tới khoá 'wolf'; done cần test" in line)
    put_table(root, "Enemies", "list", "id", cols, [wolf, soldier])
    check("moving a row (another index) changes nothing", (state(root, "enemies-soldier"), state(root, "enemies-wolf")), ("verified", "todo"))
    rb.mark(root, W, "wallet-rules#enemies-wolf", "done", W, tests=[T + "Wolf_IsFasterThanSoldier"])
    put_table(root, "Enemies", "list", "id", cols, [("wolf", "Sói", "idea", "30"), soldier])
    check("a changed cell changes only its row", (state(root, "enemies-wolf"), state(root, "enemies-soldier")), ("changed", "verified"))
    put_table(root, "Enemies", "list", "id", cols, [soldier])
    v = rb.view(root, W)
    check("a removed row is a dropped requirement",
          (state(root, "enemies-wolf"), any(d["id"] == "enemies-wolf" for d in v["dropped"])), (None, True))
    put_table(root, "Enemies", "list", "id", cols + (("damage", "int"),), [soldier + ("5",)])
    check("a new column changes every row of the table", state(root, "enemies-soldier"), "changed")
    put_table(root, "Enemies", "list", "id", cols, [soldier, ("big_boss", "Trùm", "full", "900")])
    rb.mark(root, W, "wallet-rules#enemies-big-boss", "done", W, tests=[T + "ReadsBoss"])
    check("a key with an underscore: its id uses a hyphen, a test that holds the key in a string proves it", state(root, "enemies-big-boss"), "done")
    (root / "UnityProject/Assets/Features/Wallet/Tests/EnemyTableTests.cs").write_text(
        ROW_TESTS.replace('Find("big_boss")', 'Find("other")'), encoding="utf-8")
    check("verify: refused when the test no longer names the key",
          refused(lambda: rb.mark(root, W, "wallet-rules#enemies-big-boss", "verify", "@CodeLead")))


def run_stage(root):
    """The phase the project is in: the requirements of a node (or of a table row) that is for a later phase wait."""
    W = "Wallet"
    idea = gd.table_dir(root, "idea")
    score = lambda path, n: (idea.joinpath(*path.split("/")) / "priority.json").write_text(json.dumps({"ai": n, "why": "x"}), encoding="utf-8")      # noqa: E731
    OTHER = "feature-wallet/income/income-amounts"
    score(NODE, 9), score(OTHER, 3)
    rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["WalletLogicTests.StartsWithConfiguredCoins"])
    first = next(r["id"] for n in rb.view(root, W)["nodes"] if n["path"] == OTHER for r in n["reqs"])
    rb.mark(root, W, f"income-amounts#{first}", "done", W, tests=["WalletLogicTests.Adds"])
    v = rb.view(root, W)
    total = v["total"]
    check("no stage set: nothing waits", (v["later"], v["stage"], any(r["later"] for n in v["nodes"] for r in n["reqs"])), (0, "", False))
    gd.write_stage(root, "idea")
    v = rb.view(root, W)
    other = next(n for n in v["nodes"] if n["path"] == OTHER)
    check("stage IDEA: the requirements of the FULL node that nobody started wait and are in no count; the one already done stays",
          (v["later"] - 2, v["total"] - 1, other["later"], len(other["open"]), [r["state"] for r in other["reqs"] if not r["later"]],
           {r["deferred"] for r in other["reqs"]}), (len(other["reqs"]) - 1, total - (len(other["reqs"]) - 2), len(other["reqs"]) - 1, 1, ["done"], {"full"}))
    mine = next(n for n in v["nodes"] if n["path"] == NODE)
    check("the node of this stage is untouched", {r["later"] for r in mine["reqs"] if r["kind"] != "stage"}, {False})
    # the table '## Thông số theo giai đoạn': one requirement per phase, from the phase the project is in onwards
    cols = {r["id"]: r for r in mine["reqs"] if r["kind"] == "stage"}
    check("phase columns: the column of the stage is work, the later ones wait; only what a phase asks anew is in it",
          ({k: (r["later"], r["deferred"]) for k, r in cols.items()},
           "Tiền đầu lượt: 50" in cols["stage-idea"]["text"] and "Số dư lớn nhất: 999.999.999" in cols["stage-idea"]["text"],
           cols["stage-prototype"]["text"].split("\n")[1:], cols["stage-full"]["text"].split("\n")[1:]),
          ({"stage-idea": (False, ""), "stage-prototype": (True, "prototype"), "stage-full": (True, "full")}, True,
           ["- Tiền đầu lượt: 50, chỉnh theo chơi thử"], ["- Tiền đầu lượt: Chốt theo `feature-balance`"]))
    check("phase columns: a node whose columns are all the same asks only once (its first phase)",
          [r["id"] for r in other["reqs"] if r["kind"] == "stage"], ["stage-idea"])
    check("phase columns: a waiting one takes no done", refused(lambda: rb.mark(root, W, "wallet-rules#stage-prototype", "doing", W, note="x")))
    S = rb.stage_requirements
    table = "| Thông số | IDEA | PROTOTYPE | FULL |\n|---|---|---|---|\n"
    check("phase columns: empty, 'Không có', 'như IDEA' and the same value ask for nothing",
          ([r["id"] for r in S(table + "| Quảng cáo | Không có | Nhà cung cấp giả | như PROTOTYPE |\n| Log | | Có | Có |\n")],
           [r["id"] for r in S(table + "| A | không kiểm | Như IDEA | chốt |\n")], S("Ba cột của bảng `ShotParams`."), S("")),
          (["stage-prototype"], ["stage-full"], [], []))
    h = S(table + "| A | 1 | 2 | 3 |\n")
    check("phase columns: a changed cell changes only its phase's requirement",
          [a["hash"] == b["hash"] for a, b in zip(h, S(table + "| A | 1 | 5 | 3 |\n"))], [True, False, True])
    later = next(r["id"] for r in other["reqs"] if r["later"])
    check("a requirement that waits takes no doing and no done",
          (refused(lambda: rb.mark(root, W, f"income-amounts#{later}", "doing", W, note="x")),
           refused(lambda: rb.mark(root, W, f"income-amounts#{later}", "done", W, tests=["WalletLogicTests.Adds"]))), (True, True))
    check("one of a later phase that already has code is work: the book takes its doing and done again",
          (rb.mark(root, W, f"income-amounts#{first}", "doing", W, note="thiết kế đổi, sửa lại")[0][:7],
           rb.mark(root, W, f"income-amounts#{first}", "done", W, tests=["WalletLogicTests.Adds"])[0][:7],
           next(r["later"] for n in rb.view(root, W)["nodes"] if n["path"] == OTHER for r in n["reqs"] if r["id"] == first)),
          ("Đã ghi ", "Đã ghi ", False))
    rb.mark(root, W, f"income-amounts#{first}", "verify", "@CodeLead")
    check("what was done before can still be verified", next(r["state"] for r in rb.view(root, W)["nodes"][1]["reqs"] if r["id"] == first)
          if rb.view(root, W)["nodes"][1]["path"] == OTHER else state(root, first, OTHER), "verified")
    text = rb.render(rb.view(root, W))
    check("the book prints them apart, and the counts say how many wait",
          ("## Để sau (thuộc giai đoạn sau; project đang làm IDEA" in text, f"- [FULL] {OTHER}#{later}" in text, f"để sau {len(other['reqs']) - 1}" in text,
           "Giai đoạn đang làm: IDEA" in rb.status_all(root)), (True, True, True, True))
    check("a node that waits and was only defined (not at the last round) gives no requirement",
          (rb.node_view(root, "feature-wallet/income", gd.table_dir(root, "live") / "feature-wallet" / "income", set(), "full", "idea")["reqs"],
           bool(rb.node_view(root, "feature-wallet/income", gd.table_dir(root, "live") / "feature-wallet" / "income", set(), "idea", "idea")["reqs"])),
          ([], True))
    cols = (("id", "string"), ("name", "string"), ("stage", "enum:idea|prototype|full"))
    put_table(root, "Enemies", "list", "id", cols, [("soldier", "Lính", "idea"), ("boss", "Trùm", "full")])
    rows = {r["id"]: r for n in rb.view(root, W)["nodes"] if n["path"] == NODE for r in n["reqs"]}
    check("a row of a list whose stage is later waits, in a node that does not",
          (rows["enemies-soldier"]["later"], rows["enemies-boss"]["later"], rows["enemies-boss"]["deferred"]), (False, True, "full"))
    gd.write_stage(root, "prototype")
    rb.mark(root, W, "wallet-rules#stage-prototype", "done", W, tests=["WalletLogicTests.StartsWithConfiguredCoins"])
    ids_now = [r["id"] for n in rb.view(root, W)["nodes"] if n["path"] == NODE for r in n["reqs"] if r["kind"] == "stage"]
    check("phase columns: the column of a phase the project left is no requirement, unless the book holds it",
          (ids_now, state(root, "stage-prototype")), (["stage-prototype", "stage-full"], "done"))
    gd.write_stage(root, "full")
    check("phase columns: what was done in its phase stays in the book afterwards",
          [(r["id"], r["state"]) for n in rb.view(root, W)["nodes"] if n["path"] == NODE for r in n["reqs"] if r["kind"] == "stage"],
          [("stage-prototype", "done"), ("stage-full", "todo")])
    v = rb.view(root, W)
    check("the stage moved on: everything is work again", (v["later"], rb.mark(root, W, f"income-amounts#{later}", "doing", W, note="làm")[0][:7]), (0, "Đã ghi "))


def run_progress_gate(root):
    W = "Wallet"
    check("a phase cannot be marked done while requirements are open",
          refused(lambda: gd.save_progress(root, NODE, "idea", "done", "@CodeLead")))
    for r in ids(root):
        rb.mark(root, W, f"wallet-rules#{r}", "waive", "người", note="thử")
    gd.save_progress(root, NODE, "idea", "done", "@CodeLead")
    check("and can once every requirement is met", gd.read_progress(gd.table_dir(root, "idea").joinpath(*NODE.split("/")))["idea"]["state"], "done")
    check("doing is never gated", refused(lambda: gd.save_progress(root, "feature-wallet/income/income-amounts", "idea", "doing", "@CodeLead")), False)


def run_changes(root):
    W = "Wallet"
    rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["WalletLogicTests.StartsWithConfiguredCoins"])
    rb.mark(root, W, "wallet-rules#start-50", "verify", "@CodeLead")
    rb.mark(root, W, "wallet-rules#one-at-a-time", "done", W, tests=["WalletLogicTests.Adds"])
    rb.mark(root, W, "wallet-rules#one-at-a-time", "verify", "@CodeLead")
    rb.mark(root, W, "wallet-rules#edge-tien-vua-du-dung-bang-gia", "done", W, tests=["WalletLogicTests.Adds"])
    rb.mark(root, W, "wallet-rules#params", "done", W, tests=["WalletLogicTests.Adds"])
    rb.mark(root, W, "wallet-rules#cap-max", "done", W, tests=["WalletLogicTests.Adds"])
    rb.mark(root, W, "wallet-rules#edge-tien-vua-du-dung-bang-gia,wallet-rules#params,wallet-rules#cap-max", "verify", "@CodeLead")
    gd.save_progress(root, NODE, "idea", "done", "@CodeLead")      # every requirement verified or waived (main)
    before = {r: state(root, r) for r in ids(root)}

    # order: moving ideas, edge cases and sections around changes nothing
    data = live_file(root, gd.DATA_FILE)
    text = data.read_text(encoding="utf-8")
    head, _, items = text.partition("## Ý cốt lõi\n")
    blocks = ["### " + b for b in items.split("### ") if b.strip()]
    data.write_text(head + "## Ý cốt lõi\n\n" + "\n".join(reversed(blocks)), encoding="utf-8")
    doc = live_file(root, gd.DOC_FILE)
    dt = doc.read_text(encoding="utf-8")
    start = dt.index("## Trường hợp biên\n")
    end = dt.index("\n## ", start + 5)
    lines = [l for l in dt[start:end].split("\n")[1:] if l.startswith("- ")]
    doc.write_text(dt[:start] + "## Trường hợp biên\n" + "\n".join(reversed(lines)) + "\n" + dt[end:], encoding="utf-8")
    check("reordering ideas and edge cases changes no state", {r: state(root, r) for r in ids(root)}, before)

    # an idea's text changes
    edit(root, gd.DATA_FILE, "Đủ mua ngay một cấp hero", "Đủ mua ngay MỘT cấp hero")
    check("an idea whose detail changed is 'changed', not verified", state(root, "start-50"), "changed")
    out = rb.render(rb.view(root, W))
    check("the book shows the old and the new text", "bản lúc ghi:" in out and "MỘT cấp hero" in out)
    check("a leaf marked done with an open requirement says so", "IDEA done → cần làm lại" in out)
    check("progress done cannot be marked again", refused(lambda: gd.save_progress(root, NODE, "idea", "done", "@CodeLead")))
    check("verify refuses a changed requirement", refused(lambda: rb.mark(root, W, "wallet-rules#start-50", "verify", "@CodeLead")))
    rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["WalletLogicTests.StartsWithConfiguredCoins"])
    check("writing done again takes the new text", state(root, "start-50"), "done")

    # an edge case: same title, new text -> changed; new title -> old dropped, new todo with a hint
    edit(root, gd.DOC_FILE, "**Tiền vừa đủ đúng bằng giá**: trừ được", "**Tiền vừa đủ đúng bằng giá**: vẫn trừ được")
    check("edge case text changed: 'changed'", state(root, "edge-tien-vua-du-dung-bang-gia"), "changed")
    edit(root, gd.DOC_FILE, "**Tiền vừa đủ đúng bằng giá**", "**Số dư bằng đúng giá**")
    v = rb.view(root, W)
    check("edge case renamed: the old id is dropped", any(d["ref"].endswith("#edge-tien-vua-du-dung-bang-gia") for d in v["dropped"]))
    new = [r for n in v["nodes"] for r in n["reqs"] if r["id"] == "edge-so-du-bang-dung-gia"][0]
    check("the new id is todo", new["state"], "todo")
    check("with no hint when its text changed too", new.get("hint"), None)

    # params: a number in the table changes, no idea changes
    edit(root, gd.DOC_FILE, "| Tiền đầu lượt | 50 (dự kiến) |", "| Tiền đầu lượt | 60 (dự kiến) |")
    check("a number of the Thông số table makes #params 'changed'", state(root, "params"), "changed")

    # an idea is deleted
    t = data.read_text(encoding="utf-8")
    i = t.index("### cap-max")
    j = t.find("### ", i + 4)
    data.write_text(t[:i] + (t[j:] if j >= 0 else ""), encoding="utf-8")      # only that idea (the order was reversed above)
    v = rb.view(root, W)
    check("a deleted idea is no longer counted", "cap-max" in ids(root), False)
    check("and is listed as dropped", any(d["ref"] == f"{NODE}#cap-max" for d in v["dropped"]))

    # a test that was verified disappears
    tests = root / "UnityProject/Assets/Features/Wallet/Tests/WalletLogicTests.cs"
    tests.write_text(TESTS.replace("public void Adds(", "public void AddsRenamed("), encoding="utf-8")
    check("a verified requirement whose test is gone is 'no-test'", state(root, "one-at-a-time"), "no-test")
    tests.write_text(TESTS, encoding="utf-8")
    check("and comes back once the test is there", state(root, "one-at-a-time"), "verified")

    # a new idea in a node that was done
    data.write_text(data.read_text(encoding="utf-8").rstrip() + "\n\n### new-rule | AI 7 | Luật mới\n", encoding="utf-8")
    check("a new idea starts todo", state(root, "new-rule"), "todo")

    # the node moves: every old requirement is dropped, the moved one is todo with a hint naming the old one
    live = gd.table_dir(root, "live")
    shutil.move(str(live / "feature-wallet/balance-spend/wallet-rules"), str(live / "feature-wallet/balance-spend/balance-rules"))
    idea = gd.table_dir(root, "idea")
    shutil.copytree(live / "feature-wallet/balance-spend/balance-rules", idea / "feature-wallet/balance-spend/balance-rules")
    moved = "feature-wallet/balance-spend/balance-rules"
    v = rb.view(root, W)
    check("moved node: old requirements are dropped", any(d["ref"] == f"{NODE}#one-at-a-time" for d in v["dropped"]))
    check("moved node: new ones start todo", state(root, "one-at-a-time", moved), "todo")
    hint = [r for n in v["nodes"] for r in n["reqs"] if r["ref"] == f"{moved}#one-at-a-time"][0].get("hint", "")
    check("with a hint naming the old requirement and its test", f"{NODE}#one-at-a-time" in hint and "WalletLogicTests.Adds" in hint)


def run_cli(root):
    rc, out = cli(root, "Wallet")
    check("CLI: prints the book", (rc, "# Sổ yêu cầu của Wallet" in out), (0, True))
    rc, out = cli(root, "status")
    check("CLI: status lists the feature", (rc, "- Wallet:" in out), (0, True))
    rc, _ = cli(root, "Wallet", "frobnicate", "x#y")
    check("CLI: unknown action", rc, 2)
    rc, out = cli(root, "Hero")
    check("CLI: a folder live does not name", (rc, "live không có feature" in out), (1, True))


def main():
    for fn in (run_list, run_flow, run_tables, run_stage, run_progress_gate, run_changes, run_cli):
        root = make_root()
        try:
            if fn in (run_changes,):
                for r in ids(root):
                    rb.mark(root, "Wallet", f"wallet-rules#{r}", "waive", "người", note="thử")
            fn(root)
        except Exception as ex:      # one broken group must not hide the others
            import traceback
            traceback.print_exc()
            results.append((False, f"{fn.__name__} crashed: {ex}", "", ""))
        finally:
            shutil.rmtree(root, ignore_errors=True)
    failed = [x for x in results if not x[0]]
    for ok, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
