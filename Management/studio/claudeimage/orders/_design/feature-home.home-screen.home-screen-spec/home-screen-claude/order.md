---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-home/home-screen/home-screen-spec
---
# Claude concept: màn bắt đầu

## Mô tả
Hình tham chiếu mức IDEA cho màn bắt đầu, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-home/home-screen/home-screen-spec`.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`): nút "Chơi" 400x120 px. Tên game và dòng kỉ lục là chữ, tool tự in. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: màn bắt đầu với tên game, dòng kỉ lục và nút "Chơi", đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; nút "Chơi" đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Nút là nút chính: sáng, nổi, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow. Phần nút vẽ 9-slice được nếu hợp.
