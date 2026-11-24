---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud/hud-status/status-spec
---
# Claude concept: ba ô thông tin trận và thông báo đợt

## Mô tả
Hình tham chiếu mức IDEA cho ba ô thông tin trận và dải thông báo đợt, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud/hud-status/status-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Các ô cùng kiểu dùng chung một phần (9-slice nếu khác cỡ). Hình "phụ" tool giữ hình cơ bản; "nền phụ" bỏ qua; chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: ba ô thông tin trận và thông báo đợt, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Ô nền đủ tương phản để chữ trắng hoặc đen đọc rõ trên trận; viền dày tối thiểu 3 px.
- Không gradient, không glow.
