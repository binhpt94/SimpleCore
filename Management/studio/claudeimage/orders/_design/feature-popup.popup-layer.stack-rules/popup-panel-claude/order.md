---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup/popup-layer/stack-rules
---
# Claude concept: khung popup chuẩn

## Mô tả
Hình tham chiếu mức IDEA cho khung popup chuẩn của dịch vụ popup. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-popup/popup-layer/stack-rules`.

- Chỉ vẽ khung popup bo góc, 800x700 px. Đây là hình duy nhất node này tự định.
- Nên làm khung thành phần 9-slice, vì mọi popup của game đều dùng khung này (popup thua, popup tạm dừng, popup mẫu của dịch vụ).
- Bỏ qua lớp màn chơi và nền che phủ toàn màn.
- Không vẽ chữ hay số. Bên trong khung để trống, vì nội dung do feature gọi điền.

## Danh sách
- main: khung popup chuẩn, đúng vị trí và cỡ của frame `main`

## Tiêu chí nhận
- Ảnh đúng 1920x1080, khung đúng vị trí x 560, y 190, cỡ 800x700 px, lệch không quá 20 px mỗi cạnh.
- Không có chữ, số hay kí tự nào.
- Theo style `ui-flat`: màu đặc, viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px, không gradient, không glow.
- Phần 9-slice có bốn góc bo nằm gọn trong viền, kéo giãn không méo góc.
