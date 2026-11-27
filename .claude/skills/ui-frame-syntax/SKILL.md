---
name: ui-frame-syntax
description: Chỉ phiên FEATURE=@UIDesigner. Cú pháp file frame .frame-concept/*.frame - kind ui/field, let/use, các hình và tham số, cảnh báo tự động, trang trí (outline, shadow, aura, cut) và các rect cảm nhận, vùng chạm, dùng, an toàn. Dùng trước khi viết hoặc sửa một frame.
---
# Cú pháp frame
Luật bố cục (thang, thứ bậc, luật chung) vẫn ở `.claude/rules/uidesigner.md`; skill này là cú pháp để viết frame cho đúng.

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
- Hình có sẵn: `rect`, `round-rect`, `button`, `label`, `icon`, `bar` (x y w h; `bar` thêm `fill=0..1`), `grid` (x y cols rows, `cell` hoặc `cw` `ch`, `gap`), `circle`, `ring` (cx cy r), `line`, `arrow` (`from=x,y to=x,y`). Thêm được `label="..."`, `parent=<id>` (toạ độ tính từ góc của hình cha) và cờ `overlap` (cố ý chồng lên hình khác). Label của hình `label` và `button` là **chữ người chơi đọc**, được in lên ảnh concept bằng font Roboto: viết đúng chữ trong game, ghi chú thì để trong ngoặc đơn ở cuối (`label="Chơi lại (nút chính)"` hiện "Chơi lại"). Label của hình khác là chú thích, không lên ảnh concept.
- Toạ độ là góc thấp nhất của hình: ui là góc trên trái, field là góc dưới trái.
- Giá trị là số, `<số>u` (chỉ field), hoặc tên `let`/`use`. Không có phép tính: số tính ra thì ghi kết quả và cách tính ở `CLAUDE.md`.
- Một con số đã có ở frame node khác thì `use`, không chép lại. Node có `use` phải khai node kia trong refs; node kia đổi thì node này phải làm lại.
- Cảnh báo tự động: hình tràn màn hoặc tràn khung nhìn; hình con tràn hình cha; hai hình cùng cấp chồng nhau mà không có `overlap`; nút có cạnh ngắn dưới 96 px. Vòng tầm (`ring`) không bị kiểm tràn. Cảnh báo không chặn chốt nhưng phải xử lý hoặc giải thích ở `CLAUDE.md`. Lỗi cú pháp là lỗi cấu trúc: phải sửa.
- Frame chỉ ghi kích thước và vị trí ước lượng. Màu, hình dáng, phong cách vẫn không ghi, trừ phần trang trí dưới đây.

## Trang trí và các rect của một phần tử (chỉ kind: ui, hình hộp)
Ảnh minh hoạ các rect: `.claude/skills/ui-frame-syntax/button-rects.png` (Read khi cần nhìn).

Nút (hay khung) có viền, bóng, aura, hoa văn góc thì `x y w h` là **rect cảm nhận**: mặt chính mắt đọc là nút, không gồm viền, bóng. Chữ căn giữa rect này, bấm thì cả nhóm scale quanh tâm của nó. Khai thêm (px, số hoặc tên `let`/`use`):
```
- play button x=150 y=300 w=300 h=90 label="Chơi" outline=3,0,10 shadow=0,14,7,0.4 aura=150,top cut=40,22 outline-color=#7a3410 aura-color=#ffd65a>#ffaa00
```
- `outline=<dày>[,<dx>,<dy>]`: viền nở quanh mặt, lệch dx,dy (lệch dưới: `3,0,10`).
- `shadow=<dx>,<dy>,<mờ>[,<alpha 0..1>]`: bóng dưới mặt và viền (alpha mặc định 0.3).
- `aura=<bán kính>[,top|center|bottom]`: quầng sau nút, tâm ở giữa mép trên, tâm hay mép dưới của mặt.
- `cut=<rộng>,<cao>`: hoa văn cắt ở góc mặt; chữ phải tránh.
- `outline-color=`, `shadow-color=`, `aura-color=`: `#rrggbb` (đặc) hoặc `#rrggbb>#rrggbb` (gradient trên xuống). Không ghi thì theo style vòng 1.

Tool tự tính (in ở `gd_frame.py`, `gd_pack.py`; ảnh `.measure.png` vẽ nét đứt):
- **vùng chạm** = mặt + viền (không gồm bóng, aura): cỡ bấm tối thiểu đo trên rect này; phần thừa ra ngoài mặt là raycast padding của nút.
- **rect dùng** = mọi thứ được vẽ (ảnh sau khi trim): ảnh art của phần tử đứng ở rect này; pivot ảnh = tâm cảm nhận trong rect dùng.
- **an toàn max X** (rộng hết mặt, thụt trên dưới theo `cut`), **an toàn max Y** (cao hết mặt, thụt trái phải), **an toàn trong** (thụt cả hai). Cả ba cùng tâm với rect cảm nhận. Chữ đạt khi lọt max X hoặc max Y; cỡ chữ mặc định chọn theo an toàn trong.
- Cảnh báo: vùng chạm của hai nút chồng nhau, aura hoặc bóng đè lên hình cùng cấp (cờ `overlap` nếu cố ý), vùng chạm tràn màn. Lỗi: `cut` chiếm hết mặt, số sai dạng.
- Khoảng cách giữa các nút (thang 8/16/24/40/64) vẫn đo giữa các rect cảm nhận.

## Xem lại
`python Management/tools/gd_frame.py ai/idea/<node> [--png]`: frame đã giải ra số tuyệt đối kèm cảnh báo; `--png` vẽ mỗi frame ra `<tên>.png` (960 px) và `<tên>.measure.png` (1280 px) để Read; `--png-full` vẽ 1920 px khi cần xem chi tiết nhỏ (tốn token hơn khi Read).
