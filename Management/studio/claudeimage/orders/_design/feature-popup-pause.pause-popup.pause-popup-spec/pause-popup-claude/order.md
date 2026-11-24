---
tool: claudeimage
kind: art
count: 2
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-pause/pause-popup/pause-popup-spec
---
# Claude concept: hai bước của popup tạm dừng

## Mô tả
Hình tham chiếu mức IDEA cho popup tạm dừng, vẽ theo style `ui-flat`. Bố cục lấy đúng hai frame `main` và `confirm`, đọc bằng `python Management/tools/gd_frame.py live/feature-popup-pause/pause-popup/pause-popup-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Khung popup chuẩn đã có ở claude-concept của `feature-popup/popup-layer/stack-rules` (phần `panel`): dùng lại bằng `same-as` nếu vừa, không vẽ lại. Nút cùng kiểu dùng chung một phần giữa hai frame. Nền che phủ màn bỏ qua; chữ tool tự in. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: bước chính của popup tạm dừng, đúng vị trí và cỡ của frame `main`
- confirm: bước hỏi xác nhận của popup tạm dừng, đúng vị trí và cỡ của frame `confirm`

## Tiêu chí nhận
- Mỗi ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Nút chính sáng và nổi hơn nút phụ; mọi nút cùng bán kính bo, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow.
