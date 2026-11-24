---
tool: claudeimage
kind: image
count: 1
deliver: Private
format: png 1920x140, nền trong suốt
references: Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v1/mid-battle.png
design: feature-hud/hud-layout/layout-spec
---
# Sprite nền dải mép trên và mép dưới HUD (IDEA)
## Mô tả
Nền của dải mép trên (sau tiền, máu thành, đợt) và dải mép dưới của HUD trận, màn chuẩn 1920x1080 cầm ngang. Là phần band trong bảng phần claude-concept của Hud (`gd_pack.py Hud`): một hình dùng cho cả hai dải, game kéo giãn bằng 9-slice viền 24 px mỗi cạnh (dải dưới cao 180 px). Phong cách `ui-flat` như ảnh tham chiếu: màu phẳng, không gradient, không glow. Không chữ, không số, không vẽ nút hay ô nào lên dải.
## Danh sách
- band: dải nền tối bán trong suốt (độ đục khoảng 45%), góc bo nhẹ, viền trên hoặc dưới mảnh sáng hơn một chút
## Tiêu chí nhận
- Đúng 1920x140; bốn góc ảnh trong suốt hoặc gần trong suốt (góc bo).
- Góc bo và viền nằm trọn trong 24 px mỗi cạnh; phần giữa là một màu phẳng để kéo giãn được.
- Độ đục phần giữa từ 35 tới 55%: nhìn xuyên thấy cảnh trận phía sau.
- Không chữ, không số.
