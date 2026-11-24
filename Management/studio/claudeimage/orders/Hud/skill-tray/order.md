---
tool: claudeimage
kind: image
count: 1
deliver: Private
format: png 460x140, nền trong suốt
references: Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v1/mid-battle.png
design: feature-hud/hud-skill-button/skill-button-spec
---
# Sprite khay nền cụm kĩ năng (IDEA)
## Mô tả
Khay nền nằm dưới cụm 3 nút kĩ năng ở góc dưới bên phải HUD trận, hiện đúng 460x140 px trên màn chuẩn 1920x1080 (ba nút 140 px cách nhau 20 px nằm phủ lên). Là phần tray trong bảng phần claude-concept của Hud (`gd_pack.py Hud`), 9-slice viền 24 px mỗi cạnh. Phong cách `ui-flat` như ảnh tham chiếu: màu phẳng, viền tối rõ, không gradient, không glow.
## Danh sách
- tray: khay bo góc màu tối bán trong suốt, viền tối mảnh, không vẽ ô hay nút bên trong
## Tiêu chí nhận
- Đúng 460x140, nền trong suốt ở bốn góc.
- Góc bo và viền nằm trọn trong 24 px mỗi cạnh; phần giữa là một màu phẳng để kéo giãn được.
- Không chữ, không số, không vẽ nút.
