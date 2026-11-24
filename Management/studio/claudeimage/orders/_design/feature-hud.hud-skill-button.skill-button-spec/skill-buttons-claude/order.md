---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-hud/hud-skill-button/skill-button-spec
---
# Claude concept: ba nút kĩ năng thành

## Mô tả
Hình tham chiếu mức IDEA cho ba nút kĩ năng thành ở góc dưới bên phải, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-hud/hud-skill-button/skill-button-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Ba nút cùng kiểu nên dùng chung một phần khung; biểu tượng của từng kĩ năng là phần riêng nếu frame có. Hình "phụ" tool giữ hình cơ bản; "nền phụ" bỏ qua; chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: ba nút kĩ năng thành ở góc dưới bên phải, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; các nút đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Ba nút cùng khung, cùng bán kính bo, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow.
