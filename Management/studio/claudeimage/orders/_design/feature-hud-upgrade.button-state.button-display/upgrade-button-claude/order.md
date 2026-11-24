---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud-upgrade/button-state/button-display
---
# Claude concept: một nút nâng cấp

## Mô tả
Hình tham chiếu mức IDEA cho một nút nâng cấp gồm hình, cấp và giá, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud-upgrade/button-state/button-display`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Khung nút nếu là hình "phụ" (mượn số của `bar-buttons`) thì tool giữ hình cơ bản. Chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: một nút nâng cấp với hình, cấp và giá, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Hình của mục nâng cấp đọc rõ ở cỡ thật; viền dày tối thiểu 3 px.
- Không gradient, không glow.
