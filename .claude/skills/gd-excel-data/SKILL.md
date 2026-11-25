---
name: gd-excel-data
description: Chỉ phiên FEATURE=@GameDesigner (phiên khác đọc để hiểu bảng). Làm dữ liệu config của một node thiết kế bằng bảng Excel trong <node>/.excel - chọn loại bảng (params, list, levels, formula, map), khuôn ba dòng đầu, cột index và sheet Index, sửa csv rồi gd_excel.py sync, xử lý khi người đã sửa file xlsx, chuyển một node cũ từ bảng markdown. Dùng khi viết hay sửa số liệu, danh sách, công thức của node vòng 4 hoặc node cheat.
---
# @GameDesigner: dữ liệu config bằng Excel
Số liệu config của một node (thông số, danh sách hero, quái, đợt, nâng cấp, công thức) nằm trong bảng, không viết tay thành bảng markdown. Bảng là **thiết kế**: phiên feature đọc qua `gd_pack.py` rồi tự nhập vào config asset của mình; không có gì tự chảy sang Unity.
Luật chung của node vẫn ở `.claude/rules/gamedesigner.md`; vòng 4 ở skill `gd-round-4`.

## Node nào có `.excel`
- Chỉ node có số liệu: node vòng 4 và node `<feature>/cheat`. Node không có số thì không tạo thư mục.
- Node mới viết, và node cũ **khi bạn sửa số của nó**: dùng bảng. Node cũ còn bảng markdown trong `CLAUDE.md` vẫn hợp lệ cho tới lúc đó; không chuyển hàng loạt.
- Dữ liệu theo giai đoạn vẫn chỉ ở vòng 4: cột `idea`, `prototype`, `full` của bảng `params`, cột `stage` của bảng `list`.

## Thư mục
```
<node>/.excel/
  <tên-node>.xlsx   người mở bằng Excel; chỉ gd_excel.py ghi
  Index.csv         mục lục: mỗi bảng một dòng
  <Sheet>.csv       bản chữ của từng bảng: bạn đọc và sửa file này
  .sync.json        gd_excel.py ghi
```
Bạn không đọc được `.xlsx` (nhị phân) và guard không cho ghi nó: bạn làm việc trên csv, người làm việc trên xlsx, lệnh `sync` đưa bên vừa đổi sang bên kia.

## Khuôn của một bảng
- **Dòng 1** tên cột: chữ thường `snake_case`, tiếng Anh, không trùng. **Dòng 2** kiểu. **Dòng 3** mô tả tiếng Việt, kèm đơn vị. Dữ liệu từ **dòng 4**.
- **Cột A luôn là `index`** (kiểu `int`): 1, 2, 3… liên tục, không trùng, không nhảy số. Thêm, xoá, đổi chỗ dòng xong chạy `reindex`.
- Kiểu: `int`, `float`, `string`, `bool` (`true`/`false`), `formula` (công thức dạng chữ, ví dụ `6 + 2*w`), `enum:a|b|c`, `ref:<Sheet>.<cột>` (giá trị phải có thật ở cột đó, cùng node).
- Số viết trần, không kèm đơn vị (đơn vị ở dòng 3 hoặc cột `unit`). Ô chưa có giá trị để trống. Giá trị là mô tả ("theo số đã chỉnh") thì cột phải là `string`.
- Một ô một giá trị: không gộp "1×1.5" (tách `box_width`, `box_height`), không ghi danh sách trong một ô (tách bảng, nối bằng `ref`).
- **Sheet `Index`** luôn là sheet đầu: `index | sheet | kind | key | rows | note`. `gd_excel.py new` tự thêm dòng; `rows` do `sync` điền; `note` là một câu bảng chứa gì.
- Tên sheet: PascalCase tiếng Anh, tối đa 31 kí tự (`Heroes`, `WaveMix`, `ShotParams`).

## Năm loại bảng
| kind | Dùng khi | Cột bắt buộc (sau `index`) | Khoá |
|---|---|---|---|
| `params` | thông số đơn lẻ của node | `key`, `name`, `unit`, `idea`, `prototype`, `full`, `reason` | `key` |
| `list` | danh sách thực thể: hero, quái, vật phẩm, hiệu ứng | `id`, `name`, `stage` (`idea`, `prototype` hoặc `full`: có từ giai đoạn nào), rồi mỗi thuộc tính một cột | `id` |
| `levels` | giá trị theo cấp, theo đợt | `level` (tăng dần), rồi mỗi giá trị một cột | `level` |
| `formula` | công thức | `key`, `formula`, `vars`, `example`, `reason` | `key` |
| `map` | quan hệ hai chiều: khắc chế, tỉ lệ rơi | `from`, `to`, `value` | cặp `from` + `to` |
- Thêm cột tuỳ ý sau cột bắt buộc; không bỏ, không đổi tên cột bắt buộc.
- `params`: giai đoạn giữ nguyên giá trị của IDEA thì ghi `như IDEA`; `reason` nêu vì sao chọn số đó ("dự kiến" nếu là ước lượng).
- `id`, `key`: chữ thường `snake_case`; đã có trong code thì dùng đúng mã đó (xem `gd_pack.py <Tên>`), đừng đặt mã thứ hai cho cùng một thứ.
- Số lấy từ node khác: không chép sang bảng mình; khai `refs` trong `data.md` và ghi tên node ở `reason` hay `note`.

## Mỗi dòng là một yêu cầu phải code và test
Khi node được chốt sang `live`, sổ yêu cầu của feature (`gd_req.py`) sinh yêu cầu từ bảng:
- Bảng `list`, `params`, `formula`: **mỗi dòng một yêu cầu**, tên `<node>#<sheet>-<khoá>` (`Enemies` + `wolf` → `#enemies-wolf`). Bảng `levels`, `map`: cả bảng một yêu cầu `<node>#<sheet>`.
- **Thêm một dòng là thêm một việc**: phiên feature phải code và viết test nhắc đúng khoá của dòng đó; dòng của bảng `list` không được `done` bằng `play`. Chỉ thêm dòng khi thứ đó thật sự cần có trong game ở giai đoạn ghi trong cột `stage`.
- Sửa một ô chỉ làm yêu cầu của dòng đó báo "đổi thiết kế". Thêm hay bỏ một cột làm mọi dòng của bảng báo đổi. Đổi chỗ dòng, đánh lại `index` không làm đổi gì.
- **Không đổi khoá (`id`, `key`) khi không cần**: đổi khoá là bỏ yêu cầu cũ (cùng test đã kiểm) và sinh yêu cầu mới chưa làm. Xoá dòng là bỏ yêu cầu của nó.
- Thứ chỉ để nhìn, không test được bằng code (một hiệu ứng): đặt trong bảng `params`, không đặt trong `list`.
- Báo cáo cuối lượt nêu khoá các dòng đã thêm, sửa, xoá ở mục "Công khai đổi".

## Các bước
Lệnh chạy từ gốc project; `<node>` là `ai/idea/<feature>/…`.
1. Xem đã có gì: `python Management/tools/gd_excel.py show <node>`.
2. Bảng mới: `python Management/tools/gd_excel.py new <node> <Sheet> <kind> "<bảng chứa gì>"`. Lệnh tạo `.excel/` (nếu chưa có), sheet theo khuôn và dòng trong `Index`.
3. Sửa `<Sheet>.csv` bằng Edit/Write: thêm cột (đủ ba dòng đầu), điền dữ liệu từ dòng 4. Ô có dấu phẩy hay ngoặc kép thì bọc trong ngoặc kép theo luật csv.
4. `python Management/tools/gd_excel.py sync <node>`: dựng lại xlsx từ csv, điền `rows`, rồi in lỗi khuôn còn lại. Sửa tới khi không còn dòng lỗi.
5. Thêm, xoá, đổi chỗ dòng: `python Management/tools/gd_excel.py reindex <node> <Sheet>`.
6. Trong `CLAUDE.md` của node: mục `## Thông số` và `## Thông số theo giai đoạn` chỉ còn tên bảng, cột nào là gì và phần lí giải; **không chép số hai nơi**. Ý trong `data.md` vẫn nêu giá trị IDEA như cũ.
7. `python Management/tools/gd_status.py <node>`: lỗi bảng hiện ở "Lỗi cấu trúc"; còn lỗi hoặc chưa `sync` thì node không chốt được.

## Khi người đã sửa file xlsx
- `gd_status.py` hoặc `gd_excel.py check` báo "`<tên>.xlsx` đã đổi sau lần đồng bộ cuối": chạy `sync` **trước khi** đụng csv; csv nhận số của người. Đọc lại csv rồi mới sửa tiếp.
- `sync` báo cả hai bên cùng đổi: đừng tự chọn. Số người sửa trong Excel là quyết định của người: chạy `sync <node> --from xlsx`, rồi làm lại phần bạn vừa sửa trên csv mới. Chỉ dùng `--from csv` khi người bảo bỏ bản Excel.
- "không ghi được … đang mở trong Excel": người đang mở file; báo người đóng, không tìm đường vòng.
- Số người sửa trong Excel cũng làm yêu cầu của dòng đó báo "đổi thiết kế" sau khi node được chốt: ghi rõ trong báo cáo (mục "Công khai đổi") bảng nào, khoá nào đổi.

## Chuyển một node cũ từ bảng markdown
1. Mỗi bảng markdown trong `## Thông số` thành một bảng: thông số đơn lẻ → `params` (gộp luôn bảng "theo giai đoạn" vào ba cột `idea`, `prototype`, `full`); bảng mỗi dòng một thực thể → `list`; bảng theo cấp, theo đợt → `levels`; công thức → `formula`.
2. Giữ nguyên số và lí do; không nhân dịp này đổi thiết kế. Thứ bảng cũ không nói (ví dụ giá trị PROTOTYPE của một thông số) thì ghi `như IDEA` nếu chắc, không chắc thì hỏi người.
3. Xoá bảng markdown khỏi `CLAUDE.md`, để lại dòng trỏ tới bảng (bước 6 ở trên).
Mẫu đã chuyển: `feature-castle/castle-attack/auto-shot-values` (`params`), `feature-hero/hero-list/hero-roster-values` (`list` + `params`).
