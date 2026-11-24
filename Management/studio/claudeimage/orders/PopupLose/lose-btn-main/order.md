---
tool: claudeimage
kind: image
count: 1
deliver: Public
format: png 400x120, nền trong suốt
references: Resources/_design/feature-popup-lose.lose-actions.actions-spec/claudeimage/lose-buttons-claude/v1/parts/main/btn-main.png
design: feature-popup-lose/lose-actions/actions-spec
---
# Sprite nút chính "Chơi lại" của popup thua (IDEA)
## Mô tả
Sprite thay ô xanh tạm của nút `Replay` trong `LoseMain.prefab` (400x120 px trên màn chuẩn 1920x1080 ngang). Là phần `btn-main` trong bảng phần claude-concept của PopupLose, dùng 9-slice với viền trái/trên/phải/dưới 40, 36, 40, 40 px. Phong cách `ui-flat` như ảnh tham chiếu: nút bo góc nền xanh lá sáng, viền đen dày, bóng đổ cứng ở đáy, màu phẳng, không gradient, không glow. Là nút chính nên nổi hơn nút phụ.
Không vẽ chữ: game tự in "Chơi lại".
## Danh sách
- btn-main: nút bo góc nền xanh lá sáng, viền đen dày, bóng đổ cứng ở đáy, giữa trống
## Tiêu chí nhận
- Đúng 400x120, nền trong suốt ở bốn góc.
- Không có chữ hay số.
- Bo góc và viền nằm gọn trong viền 9-slice 40, 36, 40, 40 px; phần giữa một màu phẳng.
- Viền dày tối thiểu 5 px.
