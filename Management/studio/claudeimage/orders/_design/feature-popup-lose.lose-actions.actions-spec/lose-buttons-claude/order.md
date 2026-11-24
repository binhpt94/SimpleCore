---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-lose/lose-actions/actions-spec
---
# Claude concept: hai nút của popup thua

## Mô tả
Hình tham chiếu mức IDEA cho hai nút của popup thua, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `main` của node, đọc bằng `python Management/tools/gd_frame.py live/feature-popup-lose/lose-actions/actions-spec`.

Chỉ vẽ thành phần hai nút, vì đó là phần node này tự định:
- Nút chính "Chơi lại": 400x120 px.
- Nút phụ "Về màn bắt đầu": 260x120 px.

Khung popup và ba dòng chữ là số mượn của node khác: giữ dạng hình cơ bản đúng vị trí. Nền che phủ màn thì bỏ qua. Không vẽ chữ hay số. Không chép chữ chú thích của frame vào ảnh.

## Danh sách
- main: hai nút "Chơi lại" và "Về màn bắt đầu" của popup thua, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh đúng 1920x1080. Hai nút đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Không có chữ, số hay kí tự nào trong ảnh.
- Nút "Chơi lại" sáng hơn và nổi hơn hẳn nút "Về màn bắt đầu".
- Hai nút cùng bán kính bo, viền dày tối thiểu 3 px, có bóng đổ cứng lệch xuống 4 đến 8 px.
- Không gradient, không glow.
