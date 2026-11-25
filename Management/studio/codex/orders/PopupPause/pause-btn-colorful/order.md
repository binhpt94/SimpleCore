---
tool: codex
kind: image
count: 1
deliver: Private
format: png 320x120, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/_Services/Popup/codex/popup-panel-colorful/v1/panel.png
design: feature-popup-pause/pause-popup/pause-popup-spec
---
# Nền nút chính của popup tạm dừng theo style Colorful (PROTOTYPE)

## Mô tả
Nền nút chính "Tiếp tục" của popup tạm dừng ở giai đoạn PROTOTYPE (cột PROTOTYPE của `pause-popup-spec`: popup chỉ có nút "Tiếp tục"). Trong frame `main` (màn chuẩn 1920x1080 ngang) nút rộng 320, cao 120, nằm ở hàng dưới của khung popup 800x700. Game kéo 9-slice với viền trái/trên/phải/dưới 40/36/40/40 px và vẽ chữ "Tiếp tục" lên bằng font, nên ảnh không có chữ.

Phong cách theo ảnh tham chiếu thứ nhất: concept Colorful của popup thua đã giao; lấy đúng nút xanh lá "Chơi lại" trong đó (nút chính của style: viên thuốc xanh lá bóng như kẹo, vệt sáng nửa trên, viền xanh lá đậm). Ảnh tham chiếu thứ hai là khung popup Colorful đã giao, nút sẽ nằm trên khung này: chỉ để khớp độ bóng, độ dày viền và tông màu, không vẽ lại khung.

Chỉ vẽ một nền nút rời, không ghép khung popup, không có icon hay trang trí.

## Danh sách
- btn-main: nền nút chính 320x120, viên thuốc xanh lá bóng như kẹo (thân #4CC21E, vùng sáng #9BEA5C ở mép trên, bóng #2E9414 ở mép dưới), viền xanh lá đậm #1D6B0C, vệt sáng nằm ngang ở nửa trên, bóng đổ ngắn ngay dưới thân nằm trong khung ảnh, không chữ, không icon; 9-slice viền trái/trên/phải/dưới 40/36/40/40

## Tiêu chí nhận
- Đúng 320x120, nền trong suốt ngoài viên thuốc (bốn góc ảnh trong suốt).
- Không có chữ, số, icon, emoji, sao, nơ, lá, đường đứt hay ô giữ chỗ.
- Bo hai đầu bằng nửa chiều cao (viên thuốc); phần bo và viền nằm gọn trong viền 9-slice 40/36/40/40. Phần giữa (từ 40 tới 280 theo chiều ngang) chỉ là mảng xanh lá và vệt sáng nằm ngang, kéo giãn không méo.
- Viền là xanh lá đậm của chính màu thân, dày tối thiểu 4 px, không viền đen; không chi tiết mảnh hơn 3 px.
- Màu và độ bóng khớp nút xanh lá trong ảnh tham chiếu thứ nhất.
