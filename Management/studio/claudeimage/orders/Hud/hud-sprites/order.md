---
tool: claudeimage
kind: image
count: 7
deliver: Private
format: png, cỡ ghi ở từng dòng Danh sách (rộng x cao px), nền trong suốt
references: Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v1/mid-battle.png
design: feature-hud/hud-layout/layout-spec
---
# Sprite HUD trận mức IDEA
## Mô tả
Sprite thay ô màu tạm của HUD trận (game thủ thành nhìn ngang, màn chuẩn 1920x1080, cầm ngang). Mỗi item là đúng một phần trong bảng phần của claude-concept Hud (`gd_pack.py Hud`, mục "Các hình feature này cần"), vẽ đúng cỡ của phần đó; trong game sprite hiện đúng cỡ này trên màn chuẩn. Phong cách `ui-flat` như ảnh tham chiếu (claude-concept state `mid-battle`): màu phẳng, viền tối rõ, bóng đổ cứng, không gradient, không glow.
Không vẽ chữ hay số lên sprite: game tự in tên kĩ năng, số máu, số tiền, "Đợt N" và số giây hồi chiêu. Phần 9-slice: bốn góc nằm gọn trong viền ghi ở dòng, cạnh và giữa là màu phẳng để kéo giãn được.
Không vẽ nút tạm dừng, dải chiến trường hay thanh nâng cấp (không thuộc IDEA của Hud).
## Danh sách
- band: 1920x140, nền dải mép trên (dưới tiền, máu, đợt) và dải mép dưới, 9-slice viền 24 px mỗi cạnh, tối mờ bán trong suốt khoảng 45%
- hp: 600x60, khung thanh máu thành (nền tối bên trong, viền), 9-slice viền 24 px mỗi cạnh; phần đỏ đầy máu game tự phủ bên trong
- coin: 60x60, đồng xu vàng tròn nhìn thẳng, viền nâu đậm, cạnh trái ô tiền
- tray: 460x140, khay nền dưới cụm 3 nút kĩ năng, 9-slice viền 24 px mỗi cạnh
- skill-thunder: 140x140, nút kĩ năng Sấm sét: khung nút bo góc nền xanh dương, biểu tượng tia sét vàng ở giữa nửa trên
- skill-frost: 140x140, nút kĩ năng Băng giá: cùng khung nút, biểu tượng bông tuyết xanh nhạt ở giữa nửa trên
- skill-rally: 140x140, nút kĩ năng Cổ vũ: cùng khung nút, biểu tượng lá cờ đỏ ở giữa nửa trên
## Tiêu chí nhận
- Mỗi PNG đúng cỡ ghi ở dòng của nó; nền trong suốt ở bốn góc.
- Không có chữ hay số trên mọi sprite.
- Ba nút kĩ năng cùng khung, cùng bán kính bo, viền dày tối thiểu 3 px; biểu tượng nằm trong nửa trên (y từ 10 tới 85 px) để chừa chỗ tên kĩ năng ở dưới.
- Phần 9-slice (band, hp, tray): góc bo và viền nằm trọn trong 24 px mỗi cạnh; ngoài viền chỉ có màu phẳng.
- Xu đọc rõ ở 60 px, hình chiếm 80 đến 95% khung, căn giữa.
