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
    rb.mark(root, W, "wallet-rules#start-50", "done", W, tests=["WalletLogicTests.StartsWithConfiguredCoins"])
    check("done with a real test: waiting for @CodeLead", state(root, "start-50"), "done")
    rb.mark(root, W, "feature-wallet/balance-spend/wallet-rules#start-50", "verify", "@CodeLead")
    check("verify (full path) makes it met", state(root, "start-50"), "verified")
    check("unknown requirement is refused", refused(lambda: rb.mark(root, W, "wallet-rules#nope", "doing", W, note="x")))
    rb.mark(root, W, "wallet-rules#cap-max", "done", W, play="cộng vượt trần, số dừng ở 999.999.999")
    rb.mark(root, W, "wallet-rules#cap-max", "reject", "@CodeLead", note="chưa có test trần")
    check("reject sends it back with the reason", state(root, "cap-max"), "rejected")
    import dispatch
    import gd_req
    said = gd_req.reject_notices(root, W, "wallet-rules#cap-max", "chưa có test trần")
    again = gd_req.reject_notices(root, W, "wallet-rules#cap-max", "chưa có test trần")
    ms = dispatch.messages(root, W)
    check("reject: a message in the feature's inbox, with the full ref, the reason and a check; run twice, one message",
          (len(ms), ms[0]["source"], ms[0]["key"], "chưa có test trần" in ms[0]["text"], ms[0]["check"].startswith("req-rejected"),
           "Đã đưa tin" in said[0], "đã có tin này" in again[0]),
          (1, "gd_req", "reject:" + NODE + "#cap-max", True, True, True, True))
    v = rb.view(root, W)
    check("counts: 1 met of 29", (v["met"], v["total"]), (1, 29))


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
    rc, out = cli(root, "Wallet", "doing", "income-amounts#kill-once", "viết EnemyReward")
    check("CLI: doing", (rc, "đang làm" in out), (0, True))
    rc, out = cli(root, "Wallet", "done", "income-amounts#kill-once", "WalletLogicTests.Missing")
    check("CLI: done with a missing test fails", (rc, "không thấy test" in out), (1, True))
    rc, out = cli(root, "Wallet", "done", "income-amounts#kill-once", "WalletLogicTests.Adds")
    rc2, out2 = cli(root, "Wallet", "verify", "income-amounts#kill-once")
    check("CLI: done then verify", (rc, rc2, "đã kiểm" in out2), (0, 0, True))
    rc, out = cli(root, "status")
    check("CLI: status lists the feature", (rc, "- Wallet:" in out), (0, True))
    rc, _ = cli(root, "Wallet", "frobnicate", "x#y")
    check("CLI: unknown action", rc, 2)
    rc, out = cli(root, "Hero")
    check("CLI: a folder live does not name", (rc, "live không có feature" in out), (1, True))


def main():
    for fn in (run_list, run_flow, run_progress_gate, run_changes, run_cli):
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
