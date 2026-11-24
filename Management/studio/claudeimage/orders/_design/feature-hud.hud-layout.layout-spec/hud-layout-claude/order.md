---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud/hud-layout/layout-spec
---
# Claude concept: bố cục HUD trận

## Mô tả
Hình tham chiếu mức IDEA cho bố cục HUD trận, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud/hud-layout/layout-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Hình "phụ" tool giữ hình cơ bản; "nền phụ" bỏ qua; chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: bố cục HUD trận, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px; không gradient, không glow.
- Hình cùng kiểu gom vào một phần dùng chung; phần khung kéo giãn được thì khai 9-slice.
