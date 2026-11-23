---
paths:
  - "Management/game-design/ai/idea/*/*/*/*"
  - "Management/game-design/live/*/*/*/*"
---
# GD vòng 4: Quyết định chi tiết
Luật chung: `.claude/rules/gamedesigner.md`. File này chỉ thêm phần riêng của vòng 4 (`ai/idea/<feature>/<detail>/<tech>/`).

## Mục tiêu
Sau vòng 4, phiên code làm được game chơi được ở **mức IDEA** mà không phải hỏi lại thiết kế.

## Viết gì
- Nên làm gì.
- Làm với thông số nào.
- Thông số theo giai đoạn phát triển (xem mục riêng bên dưới).
- Trường hợp biên và ngoại lệ của các quyết định ở vòng 3 (ví dụ: hero HP = 0 nhưng còn thuốc hồi sinh).

## Cụ thể tới mức nào
- **Popup**: có bao nhiêu cái; mỗi cái có bao nhiêu nút và UI component nào bắt buộc phải có để chơi.
- **HUD**: hiện cái gì, lúc nào, mỗi icon đại diện cho gì.
- **Unit**:
  - loại nào;
  - 3D hay 2D, bounding box bao nhiêu;
  - bao nhiêu con trong từng màn;
  - chỉ số từng unit;
  - skill và chỉ số từng skill;
  - danh sách effect, lúc xuất hiện và lúc biến mất;
  - hành vi di chuyển.
- **Balance**: dự đoán và đưa ra bộ chỉ số cùng cách tính.

Con số ghi kèm đơn vị và lí do; con số phỏng đoán ghi là "dự kiến".
Con số dựa trên node khác (chỉ số quái, giá, sát thương của thành...) thì nêu đường dẫn node đó trong backtick và khai trong refs: nhánh kia đổi số thì node này phải làm lại, nhờ vậy số giữa các nhánh không lệch nhau.

## Frame: bố cục ước lượng (`.frame-concept/<tên>.frame`)
Frame là bố cục viết bằng chữ: các hình cơ bản mang kích thước thật. Mục đích là ước lượng chỉ số, không phải vẽ đẹp: nút có đủ to để bấm không, các ô có vừa màn hình không, tầm bắn so với khoảng quái ra.
- **Bắt buộc** với node có bố cục: popup, màn hình, HUD, hoặc vị trí, tầm, khoảng cách trên chiến trường. Node không có bố cục thì khai `frame: none` ở phần đầu `data.md`. Thiếu cả hai thì `gd_status.py` nhắc (không chặn chốt).
- Mỗi node một hay nhiều file, ví dụ `main.frame`, `confirm.frame`. Frame tính vào hash data: sửa frame là đổi quyết định, như sửa ý.
- Trang Game design vẽ frame, kèm số và cảnh báo. Xem bản đã giải: `python Management/tools/gd_frame.py ai/idea/<node>`.

```
kind: ui                     # ui: px trên màn của vòng 1 (y đi xuống) · field: chiến trường, đơn vị u (y đi lên)
title: Popup thua
let tap = 120                # hằng số; node khác dùng được qua use
use reach = feature-enemy/enemy-stats/enemy-stat-values#archer-range   # hằng của node khác; khai node đó trong refs
- panel   round-rect x=560 y=190 w=800 h=700 label="Thua"
- replay  button     x=60  y=560 w=320 h=tap parent=panel label="Chơi lại"
- slots   grid       x=40  y=100 cols=3 rows=1 cell=200 gap=20 parent=panel
```
```
kind: field
view: x=-20u..20u y=-2u..8u  # phần chiến trường được vẽ; y=0 là mặt đất
- castle  rect  x=-1.5u y=0 w=3u h=4u label="Thành"
- spawn-l arrow from=-17u,0 to=-2u,0 label="quái ra trái"
- archer  ring  cx=-6u cy=1u r=reach label="tầm cung thủ"
```
- Hình có sẵn: `rect`, `round-rect`, `button`, `label`, `icon`, `bar` (x y w h; `bar` thêm `fill=0..1`), `grid` (x y cols rows, `cell` hoặc `cw` `ch`, `gap`), `circle`, `ring` (cx cy r), `line`, `arrow` (`from=x,y to=x,y`). Thêm được `label="..."`, `parent=<id>` (toạ độ tính từ góc của hình cha) và cờ `overlap` (cố ý chồng lên hình khác).
- Toạ độ là góc thấp nhất của hình: ui là góc trên trái, field là góc dưới trái.
- Giá trị là số, `<số>u` (chỉ field), hoặc tên `let`/`use`. Không có phép tính: số tính ra thì ghi kết quả và cách tính ở `CLAUDE.md`.
- Một con số đã có ở frame node khác thì `use`, không chép lại. Node có `use` phải khai node kia trong refs; node kia đổi thì node này phải làm lại.
- Cảnh báo tự động: hình tràn màn hoặc tràn khung nhìn; hình con tràn hình cha; hai hình cùng cấp chồng nhau mà không có `overlap`; nút có cạnh ngắn dưới 96 px. Vòng tầm (`ring`) không bị kiểm tràn. Cảnh báo không chặn chốt nhưng phải xử lý hoặc giải thích ở `CLAUDE.md`. Lỗi cú pháp là lỗi cấu trúc: phải sửa.
- Frame chỉ ghi kích thước và vị trí ước lượng. Màu, hình dáng, phong cách vẫn không ghi.

## Không cụ thể tới mức này (có chuyên gia riêng)
- Giao diện tương tác: màu, kiểu dáng. Vị trí và kích thước chỉ ở mức ước lượng, và chỉ nằm trong frame. Sai: "Nút có màu xanh".
- Hình ảnh: Sai: "Unit sói có mắt đỏ khi dùng skill", "effect màu đỏ hình lưỡi đao".
- Cách viết code: lớp, hàm, kiến trúc là việc của phiên code và @CodeLead.

## Không mơ hồ
Mỗi ý phải làm theo được và kiểm được. Sai: "Unit sói hay vẫy đuôi" (bao lâu một lần, lúc nào, để làm gì?).

## Thông số theo giai đoạn
Vòng 4 là vòng duy nhất được ghi dữ liệu theo giai đoạn, và chỉ ở một chỗ:
- `CLAUDE.md` của node có mục `## Thông số theo giai đoạn`: bảng mỗi hàng một thông số, ba cột IDEA, PROTOTYPE, FULL.
- Ý cốt lõi trong `data.md` nêu giá trị của mức IDEA và không nhắc tới giai đoạn nào.

## Mức IDEA là gì
- Các tính năng đủ để thấy được gameplay.
- Unit 2D: hiển thị được hình, sprite có nghĩa (ảnh gen bằng AI); anim đơn giản bằng scale, move, rotate hoặc đổi màu.
- Unit 3D: model low poly (làm qua blender mcp); anim cơ bản bằng scale, move, rotate, đổi màu.

## Mở khoá code
Guard chỉ cho phiên feature sửa code khi nhánh của nó có ít nhất một node vòng 4 đã chốt trong `live`. Vì vậy vòng 4 phải đủ rõ để code theo; đừng viết sơ sài chỉ để mở khoá.

## `CLAUDE.md` của node
Các mục: Làm gì, Thông số, Thông số theo giai đoạn, Trường hợp biên, Chưa rõ.
