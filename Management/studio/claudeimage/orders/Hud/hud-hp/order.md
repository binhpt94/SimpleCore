---
tool: claudeimage
kind: image
count: 1
deliver: Private
format: png 600x60, nền trong suốt
references: Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v1/mid-battle.png
design: feature-hud/hud-status/status-spec
---
# Sprite khung thanh máu thành (IDEA)
## Mô tả
Khung thanh máu thành ở giữa mép trên HUD trận, hiện đúng 600x60 px trên màn chuẩn 1920x1080. Là phần hp trong bảng phần claude-concept của Hud (`gd_pack.py Hud`), 9-slice viền 24 px mỗi cạnh. Game tự phủ phần đỏ theo lượng máu bên trong khung và tự in số "hiện tại/tối đa" ở giữa. Phong cách `ui-flat` như ảnh tham chiếu: màu phẳng, viền tối rõ, bóng đổ cứng, không gradient, không glow.
## Danh sách
- hp: khung thanh máu: viền tối dày, góc bo, lòng thanh màu đỏ thẫm rất tối (chỗ máu đã mất)
## Tiêu chí nhận
- Đúng 600x60, nền trong suốt ở bốn góc.
- Viền dày tối thiểu 3 px; góc bo và viền nằm trọn trong 24 px mỗi cạnh, phần giữa là một màu phẳng để kéo giãn được.
- Không chữ, không số, không vẽ phần đỏ đầy máu.
