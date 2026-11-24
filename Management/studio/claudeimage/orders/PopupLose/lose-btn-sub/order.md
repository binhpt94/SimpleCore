---
tool: claudeimage
kind: image
count: 1
deliver: Public
format: png 260x120, nền trong suốt
references: Resources/_design/feature-popup-lose.lose-actions.actions-spec/claudeimage/lose-buttons-claude/v1/parts/main/btn-sub.png
design: feature-popup-lose/lose-actions/actions-spec
---
# Sprite nút phụ "Về màn bắt đầu" của popup thua
## Mô tả
Sprite của nút "Về màn bắt đầu" trong `LoseMain.prefab` (260x120 px trên màn chuẩn 1920x1080 ngang; nút này có từ PROTOTYPE, đặt trước để cùng bộ với nút chính). Là phần `btn-sub` trong bảng phần claude-concept của PopupLose, dùng 9-slice với viền trái/trên/phải/dưới 40, 36, 40, 40 px. Phong cách `ui-flat` như ảnh tham chiếu: nút bo góc nền xám xanh, viền đen, bóng đổ cứng ở đáy, màu phẳng, không gradient, không glow. Là nút phụ nên kém nổi hơn nút chính (màu trầm hơn).
Không vẽ chữ: game tự in "Về màn bắt đầu".
## Danh sách
- btn-sub: nút bo góc nền xám xanh, viền đen, bóng đổ cứng ở đáy, giữa trống
## Tiêu chí nhận
- Đúng 260x120, nền trong suốt ở bốn góc.
- Không có chữ hay số.
- Bo góc và viền nằm gọn trong viền 9-slice 40, 36, 40, 40 px; phần giữa một màu phẳng.
- Cùng bán kính bo và độ dày viền với nút chính `lose-btn-main`.
