---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-scene-flow/scene-list/boot-loading
---
# Claude concept: màn Loading

## Mô tả
Hình tham chiếu mức IDEA cho màn Loading lúc mở game, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-scene-flow/scene-list/boot-loading`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in, trong đó có thanh tiến độ (vỏ và phần đầy, 9-slice để kéo theo tiến độ). Chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: màn Loading với thanh tiến độ, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; thanh tiến độ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Phần đầy và phần trống của thanh khác màu rõ; viền dày tối thiểu 3 px.
- Không gradient, không glow.
