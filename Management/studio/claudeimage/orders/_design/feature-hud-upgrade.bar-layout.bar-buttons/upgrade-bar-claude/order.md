---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud-upgrade/bar-layout/bar-buttons
---
# Claude concept: thanh nâng cấp sáu nút

## Mô tả
Hình tham chiếu mức IDEA cho thanh nâng cấp: 6 nút chia hai nhóm, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud-upgrade/bar-layout/bar-buttons`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Sáu nút cùng kiểu dùng chung một phần khung. Hình "phụ" tool giữ hình cơ bản; "nền phụ" bỏ qua; chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: thanh nâng cấp sáu nút hai nhóm, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Sáu nút cùng khung, cùng bán kính bo, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow.
