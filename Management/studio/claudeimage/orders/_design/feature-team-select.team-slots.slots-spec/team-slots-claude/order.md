---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-team-select/team-slots/slots-spec
---
# Claude concept: màn chọn đội

## Mô tả
Hình tham chiếu mức IDEA cho màn chọn đội: ba chỗ trong đội và nút vào trận, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-team-select/team-slots/slots-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Ba chỗ cùng kiểu dùng chung một phần. Thẻ hero là hình "phụ" (mượn số của `card-spec`), tool giữ hình cơ bản. Chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- main: màn chọn đội với ba chỗ trong đội và nút vào trận, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Nút vào trận là nút chính: sáng, nổi; viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px.
- Chỗ trống trong đội nhìn rõ là chỗ chờ điền; không gradient, không glow.
