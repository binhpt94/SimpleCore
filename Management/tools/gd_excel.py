#!/usr/bin/env python3
"""The config data of a design node as Excel tables: <node>/.excel/ (a dot folder: a plain one would be read as a
child node).

    <node>/.excel/
      <node key>.xlsx    what the human opens in Excel (named after the node: Excel opens no two files of one name)
      Index.csv          the text copy of the sheet Index
      <sheet>.csv        the text copy of each table (UTF-8)
      .sync.json         what both sides were at the last sync (written here)

An .xlsx is binary, so a chat can neither read nor write it: every sheet has a .csv beside it. @GameDesigner edits
the csv, the human edits the xlsx, `sync` carries the side that changed to the other one. The csv is what git diffs
and what the requirement book, gd_pack.py and the page read (no openpyxl needed for those).

A table: row 1 the column names (snake_case), row 2 their types, row 3 what each holds (Vietnamese, with the unit),
data from row 4. Column A is always `index`: 1, 2, 3... with no gap. The first sheet is always `Index`: one row per
table (index | sheet | kind | key | rows | note).
Types: int, float, string, bool, formula (text), enum:a|b|c, ref:<sheet>.<column> (a value that exists there).
Kinds and the columns each must have: see KINDS.

    python Management/tools/gd_excel.py new <node> <sheet> <kind> "<what the table holds>"
    python Management/tools/gd_excel.py sync <node> [--from xlsx|csv]
    python Management/tools/gd_excel.py check [<node>]
    python Management/tools/gd_excel.py show <node> [<sheet>]
    python Management/tools/gd_excel.py reindex <node> <sheet>

<node>: ai/idea[/<feature>/...] (show and check also take live/...), from Management/game-design.
The xlsx side needs openpyxl (python -m pip install openpyxl); reading the csv side does not.
"""
import csv
import hashlib
import io
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent

BASE = ("Management", "game-design")
EXCEL_DIR = ".excel"
INDEX = "Index"
SYNC_FILE = ".sync.json"
HEAD_ROWS = 3      # names, types, descriptions
INDEX_COLUMNS = ("index", "sheet", "kind", "key", "rows", "note")
INDEX_TYPES = ("int", "string", "", "string", "int", "string")      # kind: an enum of KINDS, filled below
INDEX_NOTES = ("Số thứ tự", "Tên sheet", "Loại bảng", "Cột khoá của bảng", "Số dòng dữ liệu (tool tự điền)", "Bảng chứa gì")
# kind -> (columns it must have after `index`, with the type a new table gives them; the key column; what it is for)
KINDS = {
    "params": ((("key", "string", "Mã thông số"), ("name", "string", "Tên thông số"), ("unit", "string", "Đơn vị"),
                ("idea", "string", "Giá trị ở IDEA"), ("prototype", "string", "Giá trị ở PROTOTYPE"),
                ("full", "string", "Giá trị ở FULL"), ("reason", "string", "Lí do")), "key", "thông số đơn lẻ"),
    "list": ((("id", "string", "Mã"), ("name", "string", "Tên hiển thị"),
              ("stage", "enum:idea|prototype|full", "Có từ giai đoạn nào")), "id", "danh sách thực thể"),
    "levels": ((("level", "int", "Cấp hoặc đợt"),), "level", "giá trị theo cấp, theo đợt"),
    "formula": ((("key", "string", "Mã công thức"), ("formula", "formula", "Công thức"), ("vars", "string", "Các biến"),
                 ("example", "string", "Ví dụ đã tính"), ("reason", "string", "Lí do")), "key", "công thức"),
    "map": ((("from", "string", "Từ"), ("to", "string", "Tới"), ("value", "float", "Giá trị")), "", "quan hệ hai chiều"),
}
SHEET_RX = re.compile(r"^[A-Za-z][A-Za-z0-9_]{0,30}$")      # Excel: at most 31 characters
COLUMN_RX = re.compile(r"^[a-z][a-z0-9_]*$")
PLAIN_TYPES = ("int", "float", "string", "bool", "formula")
ENUM_RX = re.compile(r"^enum:([^|\s]+(\|[^|\s]+)*)$")
REF_RX = re.compile(r"^ref:([A-Za-z][A-Za-z0-9_]{0,30})\.([a-z][a-z0-9_]*)$")
NOTE_MAX = 200
NEED_OPENPYXL = "cần thư viện openpyxl để đọc ghi .xlsx: python -m pip install openpyxl"


class ExcelError(ValueError):
    pass


def folder_of(node):
    return Path(node) / EXCEL_DIR


def xlsx_path(node):
    return folder_of(node) / (Path(node).name + ".xlsx")


def data_files(node):
    """The files of .excel that belong to the node (what promote copies and own_hash hashes): not Excel's lock file."""
    d = folder_of(node)
    return sorted(p for p in d.iterdir() if p.is_file() and not p.name.startswith("~$")) if d.is_dir() else []


# ---------------------------------------------------------------- the csv side (standard library only)
def cell_text(value):
    """One cell as the csv holds it: 10, 1.5, true, text."""
    if value is None:
        return ""
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, float):
        return str(int(value)) if value.is_integer() else repr(value)
    return str(value).replace("\r", "").strip()


def tidy_rows(rows):
    """Rows of text, all as wide as the header, without the empty rows and columns at the end."""
    rows = [[cell_text(c) for c in r] for r in rows]
    while rows and not any(rows[-1]):
        rows.pop()
    width = max((max((i + 1 for i, c in enumerate(r) if c), default=0) for r in rows), default=0)
    return [(r + [""] * width)[:width] for r in rows]


def read_csv(path):
    with open(path, encoding="utf-8-sig", newline="") as f:
        return tidy_rows(list(csv.reader(f)))


def write_csv(path, rows):
    out = io.StringIO()
    csv.writer(out, lineterminator="\n").writerows(rows)
    Path(path).write_text(out.getvalue(), encoding="utf-8", newline="")


def read_tables(node):
    """{sheet: rows} of the csv side, Index first, then the tables in the order Index lists them, then any other."""
    d = folder_of(node)
    found = {p.stem: read_csv(p) for p in sorted(d.glob("*.csv"))} if d.is_dir() else {}
    order = [INDEX] if INDEX in found else []
    order += [r[1] for r in found.get(INDEX, [])[HEAD_ROWS:] if len(r) > 1 and r[1] in found and r[1] != INDEX]
    order = list(dict.fromkeys(order))
    return {name: found[name] for name in order + sorted(n for n in found if n not in order)}


def content_hash(tables):
    h = hashlib.sha1()
    for name, rows in tables.items():
        h.update(json.dumps([name, rows], ensure_ascii=False).encode("utf-8"))
    return h.hexdigest()


def index_rows(tables):
    """[{index, sheet, kind, key, rows, note}] of the sheet Index."""
    rows = tables.get(INDEX, [])
    return [dict(zip(INDEX_COLUMNS, (r + [""] * len(INDEX_COLUMNS))[:len(INDEX_COLUMNS)])) for r in rows[HEAD_ROWS:]]


def index_head():
    types = list(INDEX_TYPES)
    types[2] = "enum:" + "|".join(KINDS)
    return [list(INDEX_COLUMNS), types, list(INDEX_NOTES)]


def canonical(rows):
    """The rows with every number of an int or float column written one way (1.0 and 1 are both 1): Excel gives a
    number back without its zeros, and the csv must not change for that."""
    types = rows[1] if len(rows) > 1 else []
    out = [list(r) for r in rows[:HEAD_ROWS]]
    for r in rows[HEAD_ROWS:]:
        r = list(r)
        for i, kind in enumerate(types[:len(r)]):
            if kind in ("int", "float") and r[i] != "":
                try:
                    r[i] = cell_text(float(r[i]))
                except ValueError:
                    pass
        out.append(r)
    return out


def with_counts(tables):
    """The tables with the `rows` column of Index set to what each table really holds."""
    out = {name: canonical(rows) for name, rows in tables.items()}
    if INDEX in out:
        rows = [list(r) for r in out[INDEX]]
        for r in rows[HEAD_ROWS:]:
            r += [""] * (len(INDEX_COLUMNS) - len(r))
            if r[1] in tables:
                r[4] = str(max(0, len(tables[r[1]]) - HEAD_ROWS))
        out[INDEX] = rows
    return out


def type_problem(kind, value, lookup):
    """Why `value` does not fit a column type, or ''. An empty cell fits every type."""
    if value == "" or kind in ("string", "formula"):
        return ""
    if kind == "int":
        return "" if re.fullmatch(r"-?\d+", value) else "không phải số nguyên"
    if kind == "float":
        try:
            float(value)
            return ""
        except ValueError:
            return "không phải số"
    if kind == "bool":
        return "" if value in ("true", "false") else "phải là true hoặc false"
    m = ENUM_RX.match(kind)
    if m:
        return "" if value in m.group(1).split("|") else f"không thuộc {m.group(1)}"
    m = REF_RX.match(kind)
    if m:
        return "" if value in lookup(m.group(1), m.group(2)) else f"không có trong {m.group(1)}.{m.group(2)}"
    return ""


def table_problems(name, rows, entry, tables):
    """The problems of one table against the standard shape and its Index row."""
    out = []
    where = f"{EXCEL_DIR}/{name}"
    if len(rows) < HEAD_ROWS:
        return [f"{where}: thiếu ba dòng đầu (tên cột, kiểu, mô tả)"]
    names, types = rows[0], rows[1]
    if not names or names[0] != "index" or types[0] != "int":
        out.append(f"{where}: cột A phải là index, kiểu int")
    for i, (col, kind) in enumerate(zip(names, types)):
        if not COLUMN_RX.match(col):
            out.append(f"{where}: tên cột '{col}' (cột {i + 1}) phải là chữ thường snake_case")
        elif names.index(col) != i:
            out.append(f"{where}: cột '{col}' trùng tên")
        m = REF_RX.match(kind)
        if kind not in PLAIN_TYPES and not ENUM_RX.match(kind) and not m:
            out.append(f"{where}: cột '{col}' có kiểu '{kind}' không hợp lệ (int, float, string, bool, formula, enum:a|b, ref:<sheet>.<cột>)")
        elif m and (m.group(1) not in tables or m.group(2) not in (tables[m.group(1)] or [[]])[0]):
            out.append(f"{where}: cột '{col}' trỏ tới {m.group(1)}.{m.group(2)} không có")
    kind = entry.get("kind", "")
    if kind in KINDS:
        missing = [c for c, _, _ in KINDS[kind][0] if c not in names]
        if missing:
            out.append(f"{where}: bảng loại {kind} thiếu cột {', '.join(missing)}")
    key = entry.get("key", "")
    if key and key not in names:
        out.append(f"{where}: cột khoá '{key}' (ghi ở Index) không có trong bảng")

    def lookup(sheet, col):
        t = tables.get(sheet) or [[]]
        return {r[t[0].index(col)] for r in t[HEAD_ROWS:]} if col in t[0] else set()

    data = rows[HEAD_ROWS:]
    seen, last_level = {}, None
    for n, r in enumerate(data, start=1):
        line = n + HEAD_ROWS
        if r[0] != str(n):
            out.append(f"{where}: dòng {line} có index '{r[0]}', phải là {n} (index chạy 1, 2, 3... liên tục: gd_excel.py reindex)")
        for col, kind_, value in zip(names, types, r):
            why = type_problem(kind_, value, lookup)
            if why:
                out.append(f"{where}: dòng {line}, cột {col}: '{value}' {why}")
        ident = None
        if key and key in names:
            ident = r[names.index(key)]
        elif kind == "map" and "from" in names and "to" in names:
            ident = r[names.index("from")] + " → " + r[names.index("to")]
        if ident is not None:
            if not ident.strip(" →"):
                out.append(f"{where}: dòng {line} để trống khoá")
            elif ident in seen:
                out.append(f"{where}: dòng {line} trùng khoá '{ident}' với dòng {seen[ident]}")
            seen.setdefault(ident, line)
        if kind == "levels" and "level" in names and re.fullmatch(r"-?\d+", r[names.index("level")]):
            level = int(r[names.index("level")])
            if last_level is not None and level <= last_level:
                out.append(f"{where}: dòng {line}: level {level} không tăng so với dòng trên")
            last_level = level
    return out


def sync_state(node):
    """'' when the xlsx and the csv are what the last sync left, else why not (no openpyxl needed)."""
    x = xlsx_path(node)
    try:
        saved = json.loads((folder_of(node) / SYNC_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        saved = {}
    if not x.is_file():
        return f"chưa có {x.name}"
    if saved.get("xlsx") != hashlib.sha1(x.read_bytes()).hexdigest():
        return f"{x.name} đã đổi sau lần đồng bộ cuối"
    if saved.get("csv") != content_hash(read_tables(node)):
        return "csv đã đổi sau lần đồng bộ cuối"
    return ""


def problems(node, rel=""):
    """Every problem of a node's .excel ([] when it has none, or no .excel at all). rel: how to name the node in the
    command a message gives."""
    if not folder_of(node).is_dir():
        return []
    tables = read_tables(node)
    out = []
    if INDEX not in tables:
        return [f"{EXCEL_DIR}: thiếu {INDEX}.csv (mọi thư mục {EXCEL_DIR} có sheet Index liệt kê các bảng)"]
    if tables[INDEX][:1] != [list(INDEX_COLUMNS)] or len(tables[INDEX]) < HEAD_ROWS:
        out.append(f"{EXCEL_DIR}/{INDEX}: ba dòng đầu phải theo khuôn, dòng 1 là {' | '.join(INDEX_COLUMNS)}")
    entries = index_rows(tables)
    listed = {}
    for n, e in enumerate(entries, start=1):
        line = n + HEAD_ROWS
        if e["index"] != str(n):
            out.append(f"{EXCEL_DIR}/{INDEX}: dòng {line} có index '{e['index']}', phải là {n}")
        if not SHEET_RX.match(e["sheet"]) or e["sheet"] == INDEX:
            out.append(f"{EXCEL_DIR}/{INDEX}: dòng {line}: tên sheet '{e['sheet']}' không hợp lệ (chữ, số, _, tối đa 31 kí tự)")
            continue
        if e["sheet"] in listed:
            out.append(f"{EXCEL_DIR}/{INDEX}: sheet '{e['sheet']}' ghi hai lần")
            continue
        listed[e["sheet"]] = e
        if e["kind"] not in KINDS:
            out.append(f"{EXCEL_DIR}/{INDEX}: sheet '{e['sheet']}' có loại '{e['kind']}', phải là một trong {', '.join(KINDS)}")
        if not e["note"]:
            out.append(f"{EXCEL_DIR}/{INDEX}: sheet '{e['sheet']}' chưa ghi bảng chứa gì (cột note)")
        if e["sheet"] not in tables:
            out.append(f"{EXCEL_DIR}/{INDEX}: sheet '{e['sheet']}' có trong Index nhưng không có {e['sheet']}.csv")
        elif e["rows"] != str(max(0, len(tables[e["sheet"]]) - HEAD_ROWS)):
            out.append(f"{EXCEL_DIR}/{INDEX}: số dòng của '{e['sheet']}' chưa khớp (chạy sync)")
    for name, rows in tables.items():
        if name == INDEX:
            continue
        if name not in listed:
            out.append(f"{EXCEL_DIR}/{name}.csv: bảng chưa có dòng trong Index")
        out += table_problems(name, rows, listed.get(name, {}), tables)
    why = sync_state(node)
    if why:
        out.append(f"{EXCEL_DIR}: {why}: chạy python Management/tools/gd_excel.py sync {rel or '<node>'}")
    return out


def view(node):
    """[{sheet, kind, key, note, rows}] of the tables (rows: all of them, the three head rows first), for the page,
    gd_pack.py and the requirement book; [] without .excel."""
    tables = read_tables(node) if folder_of(node).is_dir() else {}
    listed = {e["sheet"]: e for e in index_rows(tables)}
    return [{"sheet": name, "kind": listed.get(name, {}).get("kind", ""), "key": listed.get(name, {}).get("key", ""),
             "note": listed.get(name, {}).get("note", ""), "rows": rows} for name, rows in tables.items() if name != INDEX]


def text_of(node):
    """The tables as text ('' without .excel): what a change of any number changes."""
    out = []
    for t in view(node):
        out.append(f"### {t['sheet']} ({t['kind']}): {t['note']}")
        out += [" | ".join(r) for r in t["rows"][:1] + t["rows"][HEAD_ROWS:]]
    return "\n".join(out)


def markdown(tables, level="###"):
    """The tables of view() as markdown lines (gd_pack.py): names with their type and description, then the data."""
    out = []
    for t in tables:
        rows = t["rows"]
        if len(rows) < HEAD_ROWS:
            continue
        out += [f"{level} Bảng {t['sheet']} ({t['kind']}, khoá {t['key'] or '—'}): {t['note']}", "",
                "| " + " | ".join(rows[0]) + " |", "|" + "---|" * len(rows[0]),
                "| " + " | ".join((f"*{a}*" + (f" {b}" if b else "")).replace("|", "\\|") for a, b in zip(rows[1], rows[2])) + " |"]
        out += ["| " + " | ".join(c.replace("|", "\\|") for c in r) + " |" for r in rows[HEAD_ROWS:]]
        out.append("")
    return out


# ---------------------------------------------------------------- the xlsx side (openpyxl)
def _openpyxl():
    try:
        import openpyxl
        return openpyxl
    except ImportError:
        raise ExcelError(NEED_OPENPYXL)


def read_xlsx(path):
    """{sheet: rows of text} of a workbook, as the human saved it (a formula gives the value Excel computed)."""
    wb = _openpyxl().load_workbook(path, data_only=True)
    try:
        return {ws.title: tidy_rows([list(r) for r in ws.iter_rows(values_only=True)]) for ws in wb.worksheets}
    finally:
        wb.close()


def typed(kind, value):
    """A csv cell as the value Excel should hold: a number stays a number there."""
    try:
        if kind == "int" and re.fullmatch(r"-?\d+", value):
            return int(value)
        if kind == "float" and value != "":
            return float(value)
    except ValueError:
        pass
    if kind == "bool" and value in ("true", "false"):
        return value == "true"
    return value if value != "" else None


def write_xlsx(path, tables):
    """Builds the workbook from the tables, in the standard look: a coloured head, the three head rows and the
    index column frozen, a choice list on every enum column, a link from each Index row to its sheet."""
    openpyxl = _openpyxl()
    from openpyxl.styles import Alignment, Font, PatternFill
    from openpyxl.utils import get_column_letter
    from openpyxl.worksheet.datavalidation import DataValidation
    wb = openpyxl.Workbook()
    wb.remove(wb.active)
    looks = ((Font(bold=True, color="FFFFFF"), PatternFill("solid", fgColor="2F5597")),
             (Font(italic=True, color="555555"), PatternFill("solid", fgColor="DDE6F3")),
             (Font(color="333333"), PatternFill("solid", fgColor="F2F2F2")))
    for name, rows in tables.items():
        ws = wb.create_sheet(name)
        types = rows[1] if len(rows) > 1 else []
        for y, row in enumerate(rows, start=1):
            for x, value in enumerate(row, start=1):
                kind = types[x - 1] if y > HEAD_ROWS and x <= len(types) else "string"
                cell = ws.cell(row=y, column=x, value=typed(kind, value))
                if y <= HEAD_ROWS:
                    cell.font, cell.fill = looks[y - 1]
                    cell.alignment = Alignment(wrap_text=y == HEAD_ROWS, vertical="top")
        for x in range(1, max((len(r) for r in rows), default=0) + 1):
            widest = max((len(r[x - 1]) for r in rows if x <= len(r)), default=0)
            ws.column_dimensions[get_column_letter(x)].width = min(60, max(8, widest + 2))
            m = ENUM_RX.match(types[x - 1]) if x <= len(types) else None
            if m and len(m.group(1)) < 250:
                dv = DataValidation(type="list", formula1='"' + m.group(1).replace("|", ",") + '"', allow_blank=True)
                ws.add_data_validation(dv)
                dv.add(f"{get_column_letter(x)}{HEAD_ROWS + 1}:{get_column_letter(x)}1000")
        ws.freeze_panes = ws.cell(row=HEAD_ROWS + 1, column=2)
        if name == INDEX:
            for y in range(HEAD_ROWS + 1, len(rows) + 1):
                cell = ws.cell(row=y, column=2)
                if cell.value in tables:
                    cell.hyperlink = f"#'{cell.value}'!A1"
                    cell.font = Font(color="0563C1", underline="single")
    if not wb.worksheets:
        wb.create_sheet(INDEX)
    try:
        wb.save(path)
    except PermissionError:
        raise ExcelError(f"không ghi được {Path(path).name}: file đang mở trong Excel, đóng lại rồi chạy lại")


def write_tables(node, tables):
    """Writes the csv side: one file per table, and no csv of a table that is gone."""
    d = folder_of(node)
    d.mkdir(parents=True, exist_ok=True)
    for old in d.glob("*.csv"):
        if old.stem not in tables:
            old.unlink()
    for name, rows in tables.items():
        write_csv(d / f"{name}.csv", rows)


def save_state(node):
    x = xlsx_path(node)
    state = {"xlsx": hashlib.sha1(x.read_bytes()).hexdigest(), "csv": content_hash(read_tables(node))}
    (folder_of(node) / SYNC_FILE).write_text(json.dumps(state, indent=1) + "\n", encoding="utf-8")


def ordered(tables):
    """Index first, then the tables as Index lists them."""
    names = [INDEX] if INDEX in tables else []
    names += [e["sheet"] for e in index_rows(tables) if e["sheet"] in tables and e["sheet"] != INDEX]
    names = list(dict.fromkeys(names))
    return {n: tables[n] for n in names + [n for n in tables if n not in names]}


def sync(node, source=None):
    """Carries the side that changed since the last sync to the other one, and says what it did. Both changed: an
    ExcelError naming them, unless `source` ('xlsx' or 'csv') says which side wins."""
    d, x = folder_of(node), xlsx_path(node)
    if not d.is_dir():
        raise ExcelError(f"node chưa có {EXCEL_DIR}: tạo bảng đầu tiên bằng gd_excel.py new")
    try:
        saved = json.loads((d / SYNC_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        saved = {}
    csv_side = read_tables(node)
    if source not in (None, "xlsx", "csv"):
        raise ExcelError("--from phải là xlsx hoặc csv")
    if not x.is_file():
        if not csv_side or source == "xlsx":
            raise ExcelError(f"không có {x.name}" + ("" if csv_side else " lẫn csv nào"))
        source = "csv"
    xlsx_side = ordered(read_xlsx(x)) if x.is_file() else {}
    x_changed = bool(xlsx_side) and saved.get("xlsx") != hashlib.sha1(x.read_bytes()).hexdigest()
    c_changed = saved.get("csv") != content_hash(csv_side)
    if xlsx_side and with_counts(xlsx_side) == with_counts(csv_side) and not c_changed and not source:
        save_state(node)
        return "đã đồng bộ sẵn"
    if source is None:
        if not csv_side:
            source = "xlsx"
        elif x_changed and c_changed and with_counts(xlsx_side) != with_counts(csv_side):
            differ = sorted(n for n in set(xlsx_side) | set(csv_side) if xlsx_side.get(n) != csv_side.get(n))
            raise ExcelError(f"cả {x.name} lẫn csv đều đổi từ lần đồng bộ cuối (khác nhau ở: {', '.join(differ)}): "
                             "chọn bên giữ lại bằng --from xlsx hoặc --from csv")
        else:
            source = "xlsx" if x_changed and not c_changed else "csv"
    tables = with_counts(ordered(xlsx_side if source == "xlsx" else csv_side))
    write_tables(node, tables)
    write_xlsx(x, tables)
    save_state(node)
    return (f"đã lấy {x.name} làm gốc: ghi lại csv" if source == "xlsx" else f"đã lấy csv làm gốc: dựng lại {x.name}") \
        + f" ({len(tables) - (INDEX in tables)} bảng)"


def new_table(node, sheet, kind, note):
    """Adds one table of a standard kind (and .excel with its Index when the node has none yet)."""
    if not SHEET_RX.match(sheet) or sheet == INDEX:
        raise ExcelError(f"tên sheet '{sheet}' không hợp lệ: chữ, số, _, bắt đầu bằng chữ, tối đa 31 kí tự, khác {INDEX}")
    if kind not in KINDS:
        raise ExcelError(f"loại bảng phải là một trong: {', '.join(f'{k} ({v[2]})' for k, v in KINDS.items())}")
    note = " ".join((note or "").split())
    if not note or len(note) > NOTE_MAX:
        raise ExcelError(f"phải ghi bảng chứa gì (tối đa {NOTE_MAX} kí tự)")
    if folder_of(node).is_dir() and sync_state(node) and read_tables(node):
        sync(node)      # never build on a side that is behind; refuses when both changed
    tables = read_tables(node) if folder_of(node).is_dir() else {}
    if sheet.lower() in (n.lower() for n in tables):
        raise ExcelError(f"đã có sheet '{sheet}'")
    tables.setdefault(INDEX, index_head())
    columns = (("index", "int", "Số thứ tự"),) + KINDS[kind][0]
    tables[sheet] = [[c[i] for c in columns] for i in range(3)]
    tables[INDEX] = tables[INDEX] + [[str(len(tables[INDEX]) - HEAD_ROWS + 1), sheet, kind, KINDS[kind][1], "0", note]]
    tables = with_counts(ordered(tables))
    write_tables(node, tables)
    write_xlsx(xlsx_path(node), tables)
    save_state(node)


def reindex(node, sheet):
    """Numbers column A of one table 1..n again (after rows were added, removed or moved) and syncs."""
    if sync_state(node):
        sync(node)
    tables = read_tables(node)
    if sheet not in tables:
        raise ExcelError(f"không có sheet '{sheet}'")
    rows = [list(r) for r in tables[sheet]]
    for n, r in enumerate(rows[HEAD_ROWS:], start=1):
        r[0] = str(n)
    tables[sheet] = rows
    tables = with_counts(tables)
    write_tables(node, tables)
    write_xlsx(xlsx_path(node), tables)
    save_state(node)
    return max(0, len(rows) - HEAD_ROWS)


# ---------------------------------------------------------------- command line
NODE_RX = re.compile(r"^(ai/idea|live)(/[a-z0-9][a-z0-9-]*)*$")


def node_folder(root, arg, write=False):
    arg = (arg or "").strip().strip("/")
    if not NODE_RX.match(arg) or (write and not arg.startswith("ai/idea")):
        raise ExcelError(f"node phải là ai/idea[/<feature>/...]" + ("" if write else " hoặc live/...") + f", không phải '{arg}'")
    folder = Path(root).joinpath(*BASE, *arg.split("/"))
    if not (folder / "data.md").is_file():
        raise ExcelError(f"không có node {arg}")
    return folder, arg


def nodes_with_excel(root):
    base = Path(root).joinpath(*BASE, "ai", "idea")
    return sorted(p.parent for p in base.rglob(EXCEL_DIR) if p.is_dir()) if base.is_dir() else []


def main(argv, root=None):
    root = Path(root) if root else HERE.parent.parent
    args = argv[1:]
    try:
        if args[:1] == ["new"] and len(args) == 5:
            folder, rel = node_folder(root, args[1], write=True)
            new_table(folder, args[2], args[3], args[4])
            print(f"Đã thêm bảng {args[2]} ({args[3]}) vào {rel}/{EXCEL_DIR}/. Điền dữ liệu vào {args[2]}.csv từ dòng 4 "
                  f"(cột index chạy 1, 2, 3...), rồi chạy: python Management/tools/gd_excel.py sync {rel}")
            return 0
        if args[:1] == ["sync"] and len(args) in (2, 4) and (len(args) == 2 or args[2] == "--from"):
            folder, rel = node_folder(root, args[1], write=True)
            print(f"{rel}: " + sync(folder, args[3] if len(args) == 4 else None))
            left = [p for p in problems(folder, rel)]
            for p in left:
                print("- " + p)
            return 1 if left else 0
        if args[:1] == ["check"] and len(args) <= 2:
            if len(args) == 2:
                folder, rel = node_folder(root, args[1])
                found = {rel: problems(folder, rel)}
                if not folder_of(folder).is_dir():
                    print(f"{rel}: không có {EXCEL_DIR}")
            else:
                base = Path(root).joinpath(*BASE)
                found = {f.relative_to(base).as_posix(): problems(f, f.relative_to(base).as_posix()) for f in nodes_with_excel(root)}
            for rel, ps in found.items():
                print(f"{rel}: " + (f"{len(ps)} lỗi" if ps else "đạt"))
                for p in ps:
                    print("- " + p)
            return 1 if any(found.values()) else 0
        if args[:1] == ["show"] and len(args) in (2, 3):
            folder, rel = node_folder(root, args[1])
            tables = [t for t in view(folder) if len(args) == 2 or t["sheet"] == args[2]]
            if not tables:
                raise ExcelError(f"{rel}: không có bảng" + (f" '{args[2]}'" if len(args) == 3 else f" ({EXCEL_DIR} trống hoặc chưa có)"))
            for t in tables:
                print(f"## {t['sheet']} ({t['kind']}, khoá {t['key'] or '—'}): {t['note']}")
                for r in t["rows"]:
                    print(" | ".join(r))
                print()
            return 0
        if args[:1] == ["reindex"] and len(args) == 3:
            folder, rel = node_folder(root, args[1], write=True)
            print(f"{rel}: đã đánh lại index của {args[2]}: {reindex(folder, args[2])} dòng")
            return 0
    except ExcelError as ex:
        print(f"Lỗi: {ex}", file=sys.stderr)
        return 1
    print(__doc__.split("\n\n")[4] +"\n\nLoại bảng: " + "; ".join(
        f"{k} ({v[2]}): {', '.join(c for c, _, _ in v[0])}" for k, v in KINDS.items()), file=sys.stderr)
    return 2


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
