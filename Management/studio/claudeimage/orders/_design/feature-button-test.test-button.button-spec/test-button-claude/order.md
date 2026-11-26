---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-button-test/test-button/button-spec
---
# Claude concept: nút "Thử" và dòng "Đã bấm: N"

## Mô tả
Hình tham chiếu mức IDEA cho nút thử trên màn trận, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-button-test/test-button/button-spec`.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`): nút `test-button` 200x120 px ở góc trái dưới dải chiến trường. Dòng `test-counter` là chữ, tool tự vẽ. Dải chiến trường là hình cơ bản. Để trống chỗ chữ trên nút (game vẽ chữ "Thử" bằng font). Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: nút "Thử" ở góc trái dưới màn trận, ngay trên thanh dưới HUD, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; nút đúng hộp 200x120 của frame.
- Nút đọc rõ là nút bấm được, viền là sắc đậm hơn của màu thân.
- Nền phần trong suốt.
