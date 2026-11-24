---
tool: claudeimage
kind: image
count: 1
deliver: Public
format: png 800x700, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/claudeimage/lose-popup-claude/v3/parts/new-record/1-panel.png
design: feature-popup-lose/cheat
---
# Sprite khung popup thua (IDEA)
## Mô tả
Sprite thay ô trắng tạm ở gốc `LoseMain.prefab` (khung popup thua, 800x700 px trên màn chuẩn 1920x1080 ngang). Là phần `1-panel` trong bảng phần claude-concept của PopupLose (`gd_pack.py PopupLose`), dùng 9-slice với viền trái/trên/phải/dưới 48, 48, 48, 56 px. Phong cách `ui-flat` như ảnh tham chiếu: khung bo góc nền xanh đậm, viền đen dày, bóng đổ cứng ở đáy, màu phẳng, không gradient, không glow.
Không vẽ chữ, không vẽ ô giữ chỗ: game tự in tiêu đề, điểm, kỉ lục và đặt nút lên trên.
## Danh sách
- panel: khung popup bo góc nền xanh đậm, viền đen dày, bóng đổ cứng ở đáy, giữa trống
## Tiêu chí nhận
- Đúng 800x700, nền trong suốt ở bốn góc.
- Không có chữ, số, đường đứt hay ô giữ chỗ.
- Bo góc và viền nằm gọn trong viền 9-slice 48, 48, 48, 56 px; phần giữa một màu phẳng để kéo giãn không lộ.
- Viền dày tối thiểu 6 px.
