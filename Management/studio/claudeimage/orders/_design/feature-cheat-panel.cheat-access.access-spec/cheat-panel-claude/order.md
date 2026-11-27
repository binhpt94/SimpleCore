---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-cheat-panel/cheat-access/access-spec
---
# Claude concept: nút và panel cheat

## Mô tả
Hình tham chiếu mức IDEA cho nút Cheat và panel cheat đang mở, vẽ theo style `ui-flat`. Bố cục lấy đúng frame `wide` của node, đọc bằng `python Management/tools/gd_frame.py live/feature-cheat-panel/cheat-access/access-spec`.

Vẽ mọi thành phần node này tự định (nhóm "của node" trong `gd_frame.py`):
- Nút Cheat 160x96 px ở mép trên, ở trạng thái panel đang mở. Đây là nút công cụ của người test, nhỏ hơn hẳn các dòng.
- Khung panel 1840x880 px.
- Nền danh sách 1760x728 px (state-list) bên trong panel. Phần more-rows 1760x312 px là phần tiếp của nền danh sách: không vẽ dòng nào ở đó.
- Dòng "Chơi bình thường" (normal-row) 1760x120 px, trải hết bề ngang. Đây là dòng đặc biệt, nhìn khác các dòng state một chút nhưng cùng họ.
- Dòng state (state-row) 872x120 px ở cột trái, và một dòng state kế (next-row) 872x120 px ở cột phải, vẽ giống hệt state-row.
Tiêu đề panel và nhãn "Bản này không có state cheat" là chữ, tool tự vẽ. Nền che phủ màn thì bỏ qua. Không vẽ chữ hay số. Không chép chữ chú thích của frame vào ảnh.

## Danh sách
- wide: nút Cheat, panel cheat với dòng Chơi bình thường và hai dòng state xếp hai cột, đúng vị trí và cỡ của frame `wide`

## Tiêu chí nhận
- Ảnh đúng 1920x1080. Mỗi thành phần đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Không có chữ, số hay kí tự nào trong ảnh.
- Các dòng đọc rõ là nút bấm được, nổi hơn nền danh sách; nền danh sách tách rõ với khung panel.
- Dòng Chơi bình thường phân biệt được với dòng state; hai dòng state giống hệt nhau.
- Nút Cheat nhìn rõ là đang bật (trạng thái panel đang mở), cùng họ với các dòng.
- Cùng bán kính bo cho các nút, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px. Không gradient, không glow.
