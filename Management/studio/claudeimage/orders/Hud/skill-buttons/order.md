---
tool: claudeimage
kind: image
count: 3
deliver: Private
format: png 140x140, nền trong suốt
references: Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v1/mid-battle.png
design: feature-hud/hud-skill-button/skill-button-spec
---
# Sprite ba nút kĩ năng thành (IDEA)
## Mô tả
Sprite thay ô màu tạm của 3 nút kĩ năng ở góc dưới bên phải HUD trận (game thủ thành nhìn ngang, màn chuẩn 1920x1080, cầm ngang). Mỗi nút hiện đúng 140x140 px trên màn chuẩn; là các phần skill-thunder, skill-frost, skill-rally trong bảng phần claude-concept của Hud (`gd_pack.py Hud`). Phong cách `ui-flat` như ảnh tham chiếu (claude-concept state `mid-battle`): màu phẳng, viền tối rõ, bóng đổ cứng, không gradient, không glow.
Không vẽ chữ hay số: game tự in tên kĩ năng ở nửa dưới nút, số giây hồi chiêu và lớp phủ tối khi đang hồi.
## Danh sách
- skill-thunder: nút Sấm sét: khung nút bo góc nền xanh dương, biểu tượng tia sét vàng ở giữa nửa trên
- skill-frost: nút Băng giá: cùng khung nút, biểu tượng bông tuyết xanh nhạt ở giữa nửa trên
- skill-rally: nút Cổ vũ: cùng khung nút, biểu tượng lá cờ đỏ ở giữa nửa trên
## Tiêu chí nhận
- Đúng 140x140, nền trong suốt ở bốn góc.
- Không có chữ hay số.
- Ba nút cùng khung, cùng bán kính bo, viền dày tối thiểu 3 px; biểu tượng nằm trong y từ 10 tới 85 px để chừa chỗ tên kĩ năng ở dưới.
- Ba biểu tượng phân biệt được ngay ở 140 px bằng hình và màu.
