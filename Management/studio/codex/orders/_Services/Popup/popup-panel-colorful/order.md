---
tool: codex
kind: image
count: 1
deliver: Private
format: png 800x700, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/_design/feature-popup.popup-layer.stack-rules/claudeimage/popup-panel-claude/v2/parts/main/panel.png
design: feature-popup/popup-layer/stack-rules
---
# Sprite khung popup chuẩn theo style Colorful (PROTOTYPE)
## Mô tả
Khung popup chuẩn của dịch vụ popup: phần `panel` của frame `main` ở `feature-popup/popup-layer/stack-rules` (800x700 px trên màn chuẩn 1920x1080 ngang). Gắn vào gốc `PopupDevSample.prefab` (popup mẫu của dịch vụ, bản dev), thay ô bo góc vẽ tạm. Dùng 9-slice với viền trái/trên/phải/dưới 48, 48, 48, 56 px, giống phần `panel` của claude-concept (ảnh tham chiếu thứ hai, chỉ để lấy cỡ và vị trí viền).

Phong cách lấy từ ảnh tham chiếu thứ nhất (concept Colorful của popup thua, đã giao): chỉ lấy phần khung, gồm thân kem sáng, viền vàng cam dày có viền ngoài đậm hơn, mép trên sáng nhất và tối dần về mép dưới, góc bo tròn mềm, vệt bóng sáng nhẹ ở mép trên viền.

Chỉ là khung trống: không ruy băng tiêu đề, không emoji, không nơ, không lá, không sao. Mấy thứ đó thuộc popup của từng feature, feature tự đặt lên trên. Không vẽ chữ, không vẽ ô giữ chỗ.
## Danh sách
- panel: khung popup bo góc Colorful, thân kem sáng, viền vàng cam dày có viền ngoài cam đậm, giữa trống một màu kem
## Tiêu chí nhận
- Đúng 800x700, nền trong suốt ở bốn góc ngoài phần bo.
- Không có chữ, số, ruy băng, emoji, nơ, lá, sao, đường đứt hay ô giữ chỗ.
- Bo góc, viền vàng cam và viền ngoài đậm nằm gọn trong viền 9-slice 48, 48, 48, 56 px; phần giữa (từ 48 tới 752 theo chiều ngang, từ 48 tới 644 theo chiều dọc) chỉ là màu kem, được phép chuyển rất nhẹ từ trên xuống dưới, không có chi tiết, để kéo giãn không lộ.
- Viền vàng cam dày tối thiểu 14 px, viền ngoài cam đậm tối thiểu 4 px; viền là sắc đậm của chính màu vàng cam, không viền đen.
- Màu khớp ảnh tham chiếu thứ nhất: thân kem, viền vàng cam.
