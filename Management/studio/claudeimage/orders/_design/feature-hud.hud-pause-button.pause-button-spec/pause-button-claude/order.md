---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud/hud-pause-button/pause-button-spec
---
# Claude concept: nút tạm dừng trên HUD

## Mô tả
Hình tham chiếu mức IDEA cho nút tạm dừng ở góc trên bên trái, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud/hud-pause-button/pause-button-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Hình "phụ" tool giữ hình cơ bản; "nền phụ" bỏ qua; chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: nút tạm dừng ở góc trên bên trái, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; nút đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Nút có biểu tượng tạm dừng (hai vạch đứng) đọc rõ ở cỡ thật; viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow.
