#!/usr/bin/env python3
"""Tests for gd_excel.py on a throw-away tree in a temp folder. Run: python Management/tools/test_gd_excel.py"""
import io
import shutil
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import gd_excel as gx  # noqa: E402

results = []


def check(label, got, expect=True):
    results.append((got == expect, label, expect, got))


def call(root, *args):
    out, err = io.StringIO(), io.StringIO()
    with redirect_stdout(out), redirect_stderr(err):
        code = gx.main(["gd_excel.py", *args], root)
    return code, out.getvalue() + err.getvalue()


def has(problems, text):
    return any(text in p for p in problems)


def main():
    import openpyxl
    root = Path(tempfile.mkdtemp(prefix="gd-excel-"))
    rel = "ai/idea/feature-hero/hero-list/hero-values"
    node = root.joinpath(*gx.BASE, *rel.split("/"))
    node.mkdir(parents=True)
    (node / "data.md").write_text("# Hero\n", encoding="utf-8")
    d = node / gx.EXCEL_DIR
    try:
        check("no .excel: no problem, nothing to show", (gx.problems(node), gx.view(node), gx.text_of(node), call(root, "show", rel)[0]), ([], [], "", 1))
        check("new: a bad node, sheet, kind or an empty note is refused",
              [call(root, "new", *a)[0] for a in (("ai/idea/nope", "Heroes", "list", "x"), ("live/feature-hero", "Heroes", "list", "x"),
                                                  ("../x", "Heroes", "list", "x"), (rel, "1bad", "list", "x"), (rel, "Index", "list", "x"),
                                                  (rel, "Heroes", "wild", "x"), (rel, "Heroes", "list", " "))], [1] * 7)
        check("nothing was made by the refused calls", d.exists(), False)

        for sheet, kind in (("Heroes", "list"), ("Params", "params"), ("Levels", "levels"), ("Formulas", "formula"), ("Counters", "map")):
            check(f"new: a table of kind {kind}", call(root, "new", rel, sheet, kind, f"Bảng {kind}")[0], 0)
        check("new: the workbook is named after the node, with a csv for Index and each table, and passes the check",
              (sorted(p.name for p in d.iterdir()), gx.problems(node, rel)),
              (sorted([".sync.json", "hero-values.xlsx", "Index.csv", "Heroes.csv", "Params.csv", "Levels.csv", "Formulas.csv", "Counters.csv"]), []))
        check("new: the same sheet twice is refused", call(root, "new", rel, "heroes", "list", "x")[0], 1)
        wb = openpyxl.load_workbook(d / "hero-values.xlsx")
        check("xlsx: Index is the first sheet, every sheet starts with the index column, the head is frozen",
              (wb.sheetnames, [ws["A1"].value for ws in wb.worksheets], wb["Heroes"].freeze_panes, wb["Index"]["B4"].hyperlink is not None),
              (["Index", "Heroes", "Params", "Levels", "Formulas", "Counters"], ["index"] * 6, "B4", True))
        check("xlsx: an enum column has a choice list", len(wb["Heroes"].data_validations.dataValidation), 1)
        wb.close()
        index = gx.index_rows(gx.read_tables(node))
        check("Index: one numbered row per table with its kind, key and row count",
              [(e["index"], e["sheet"], e["kind"], e["key"], e["rows"]) for e in index],
              [("1", "Heroes", "list", "id", "0"), ("2", "Params", "params", "key", "0"), ("3", "Levels", "levels", "level", "0"),
               ("4", "Formulas", "formula", "key", "0"), ("5", "Counters", "map", "", "0")])

        # the designer edits the csv
        rows = gx.read_csv(d / "Heroes.csv")
        rows[0] += ["max_health", "damage", "ranged"]
        rows[1] += ["int", "float", "bool"]
        rows[2] += ["Máu tối đa", "Sát thương mỗi đòn", "Đánh xa"]
        rows += [["1", "knight", "Hiệp sĩ", "idea", "300", "12", "false"], ["2", "archer", "Cung thủ", "idea", "180", "9.5", "true"]]
        gx.write_csv(d / "Heroes.csv", rows)
        check("an edited csv is reported as not synced", has(gx.problems(node, rel), "csv đã đổi sau lần đồng bộ cuối"))
        code, out = call(root, "sync", rel)
        wb = openpyxl.load_workbook(d / "hero-values.xlsx")
        check("sync: the csv that changed rebuilds the xlsx, numbers stay numbers there, Index counts the rows",
              (code, "csv làm gốc" in out, wb["Heroes"]["E4"].value, wb["Heroes"]["F5"].value, wb["Heroes"]["G5"].value, wb["Index"]["E4"].value,
               gx.problems(node, rel)), (0, True, 300, 9.5, True, 2, []))
        # the human edits the xlsx
        wb["Heroes"]["E4"] = 320
        wb["Heroes"].append([3, "mage", "Pháp sư", "prototype", 150, 20, True])
        wb.save(d / "hero-values.xlsx")
        wb.close()
        check("an edited xlsx is reported as not synced", has(gx.problems(node, rel), "hero-values.xlsx đã đổi"))
        code, out = call(root, "sync", rel)
        rows = gx.read_csv(d / "Heroes.csv")
        check("sync: the xlsx that changed rewrites the csv",
              (code, "hero-values.xlsx làm gốc" in out, rows[3][4], rows[5], gx.index_rows(gx.read_tables(node))[0]["rows"], gx.problems(node, rel)),
              (0, True, "320", ["3", "mage", "Pháp sư", "prototype", "150", "20", "true"], "3", []))
        check("sync: nothing changed", "đã đồng bộ sẵn" in call(root, "sync", rel)[1])

        # both sides changed
        wb = openpyxl.load_workbook(d / "hero-values.xlsx")
        wb["Heroes"]["E4"] = 999
        wb.save(d / "hero-values.xlsx")
        wb.close()
        rows[3][4] = "111"
        gx.write_csv(d / "Heroes.csv", rows)
        code, out = call(root, "sync", rel)
        check("sync: both sides changed: refused, naming the sheet and how to choose", (code, "Heroes" in out, "--from" in out), (1, True, True))
        check("sync --from csv: the csv wins", (call(root, "sync", rel, "--from", "csv")[0], gx.read_xlsx(d / "hero-values.xlsx")["Heroes"][3][4]), (0, "111"))
        check("sync --from: only xlsx or csv", call(root, "sync", rel, "--from", "both")[0], 1)

        # every rule of the shape
        def broken(sheet, change):
            keep = {p.name: p.read_bytes() for p in d.iterdir()}
            rows = gx.read_csv(d / f"{sheet}.csv")
            change(rows)
            gx.write_csv(d / f"{sheet}.csv", rows)
            found = gx.problems(node, rel)
            for name, raw in keep.items():
                (d / name).write_bytes(raw)
            return found

        def set_cell(y, x, v):
            return lambda rows: rows[y].__setitem__(x, v)

        check("check: the index must run 1, 2, 3", has(broken("Heroes", set_cell(4, 0, "5")), "dòng 5 có index '5', phải là 2"))
        check("check: column A must be index", has(broken("Heroes", set_cell(0, 0, "no")), "cột A phải là index"))
        check("check: a key twice", has(broken("Heroes", set_cell(4, 1, "knight")), "trùng khoá 'knight'"))
        check("check: an empty key", has(broken("Heroes", set_cell(4, 1, "")), "để trống khoá"))
        check("check: a value of the wrong type", has(broken("Heroes", set_cell(3, 4, "nhiều")), "'nhiều' không phải số nguyên"))
        check("check: a value outside the enum", has(broken("Heroes", set_cell(3, 3, "beta")), "không thuộc idea|prototype|full"))
        check("check: a bool", has(broken("Heroes", set_cell(3, 6, "yes")), "phải là true hoặc false"))
        check("check: an unknown type", has(broken("Heroes", set_cell(1, 4, "number")), "kiểu 'number' không hợp lệ"))
        check("check: a column name that is not snake_case", has(broken("Heroes", set_cell(0, 4, "Max Health")), "snake_case"))
        check("check: a column the kind needs is missing", has(broken("Heroes", set_cell(0, 3, "phase")), "thiếu cột stage"))
        check("check: a table Index does not list", has(broken("Index", lambda rows: rows.pop(4)), "Params.csv: bảng chưa có dòng trong Index"))
        check("check: an unknown kind in Index", has(broken("Index", set_cell(3, 2, "wild")), "có loại 'wild'"))
        check("check: a table without its note", has(broken("Index", set_cell(3, 5, "")), "chưa ghi bảng chứa gì"))

        def levels(rows):
            rows[0].append("hero")
            rows[1].append("ref:Heroes.id")
            rows[2].append("Hero mở ở cấp này")
            rows += [["1", "1", "knight"], ["2", "3", "archer"], ["3", "2", "nobody"]]
        found = broken("Levels", levels)
        check("check: levels must rise, and a ref must name a value that exists",
              (has(found, "level 2 không tăng"), has(found, "'nobody' không có trong Heroes.id"), has(found, "'knight'")), (True, True, False))
        check("check: a ref to a column that is not there",
              has(broken("Levels", lambda rows: (rows[0].append("hero"), rows[1].append("ref:Heroes.code"), rows[2].append("x"))), "trỏ tới Heroes.code không có"))

        def pairs(rows):
            rows += [["1", "knight", "archer", "1.5"], ["2", "knight", "archer", "2"]]
        check("check: a map has each pair once", has(broken("Counters", pairs), "trùng khoá 'knight → archer'"))
        check("check: the broken copies were put back", gx.problems(node, rel), [])

        # reindex
        rows = gx.read_csv(d / "Heroes.csv")
        rows.pop(4)
        gx.write_csv(d / "Heroes.csv", rows)
        code, out = call(root, "reindex", rel, "Heroes")
        check("reindex: column A runs 1..n again and the node passes",
              (code, [r[0] for r in gx.read_csv(d / "Heroes.csv")[3:]], gx.index_rows(gx.read_tables(node))[0]["rows"], gx.problems(node, rel)),
              (0, ["1", "2"], "2", []))
        check("reindex: an unknown sheet", call(root, "reindex", rel, "Nope")[0], 1)

        # what the other tools read
        code, out = call(root, "show", rel, "Heroes")
        check("show: one table as text", (code, "## Heroes (list, khoá id): Bảng list" in out, "1 | knight | Hiệp sĩ" in out), (0, True, True))
        check("check: one node and every node", (call(root, "check", rel)[0], f"{rel}: đạt" in call(root, "check")[1]), (0, True))
        text = gx.text_of(node)
        md = "\n".join(gx.markdown(gx.view(node)))
        check("text and markdown: every table with its data",
              ("### Heroes (list): Bảng list" in text, "1 | knight | Hiệp sĩ | idea | 111" in text, "| 1 | knight | Hiệp sĩ" in md, "*int* Máu tối đa" in md),
              (True, True, True, True))
        (d / "~$hero-values.xlsx").write_bytes(b"x")
        check("data files: the csv, the xlsx and the sync state, never the lock file Excel leaves",
              "~$hero-values.xlsx" in [p.name for p in gx.data_files(node)], False)
        (d / "~$hero-values.xlsx").unlink()
        (d / "hero-values.xlsx").unlink()
        check("a missing xlsx is reported, and sync builds it again from the csv",
              (has(gx.problems(node, rel), "chưa có hero-values.xlsx"), call(root, "sync", rel)[0], gx.problems(node, rel)), (True, 0, []))
        check("a wrong call prints the usage with the kinds", (call(root, "sync")[0], "params (thông số đơn lẻ)" in call(root, "sync")[1]), (2, True))
    finally:
        shutil.rmtree(root, ignore_errors=True)
    failed = [r for r in results if not r[0]]
    for _, label, expect, got in failed:
        print(f"FAIL  {label}: expected {expect!r}, got {got!r}")
    print(f"TOTAL: {len(results) - len(failed)} ok, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main())
