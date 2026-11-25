---
tool: claudeimage
kind: image
count: 5
deliver: Private
format: png, cỡ ghi ở từng dòng Danh sách (rộng x cao px); sky, far, ground phủ kín khung, grass và stone nền trong suốt
references: Resources/_design/feature-battlefield.battlefield-layers.layer-values/claudeimage/battlefield-claude/v1/layers.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite nền chiến trường mức IDEA
## Mô tả
Bốn lớp nền của màn trận (game thủ thành nhìn ngang, cầm ngang): trời, cảnh xa, dải đất, decor. Mỗi item là đúng một phần trong bảng phần claude-concept của Battlefield (`gd_pack.py Battlefield`, mục "Các hình feature này cần"), vẽ đúng cỡ và tỉ lệ của phần đó; trong game sky, far, ground kéo phủ ngang 42u (x −21…+21u), decor giữ đúng tỉ lệ. Phong cách như ảnh tham chiếu: màu phẳng, ít chi tiết, không gradient mạnh, không glow. Không chữ, không số, không vẽ thành, hero, quái hay HUD.
Nền chỉ để nhìn: màu dịu hơn unit để thành, hero, quái nổi lên trên.
## Danh sách
- sky: 1920x640, trời phủ kín khung (không trong suốt), xanh nhạt, có thể chuyển nhẹ từ trên đậm xuống chân trời nhạt; không mây, không mặt trời
- far: 1920x114, dải cảnh xa đồi xanh thẫm liền một dải đứng trên mép dưới ảnh, phần trên đường đồi trong suốt; hai mép trái phải nối liền được
- ground: 1920x183, dải đất: mép trên là một dải cỏ phẳng, bên dưới là đất nâu; lặp ngang được (mép trái và mép phải khớp nhau)
- grass: 160x60, một khóm cỏ phẳng thấp, chân chạm mép dưới, nền trong suốt
- stone: 120x60, một hòn đá dẹt thấp xám, chân chạm mép dưới, nền trong suốt
## Tiêu chí nhận
- Mỗi PNG đúng cỡ ghi ở dòng của nó.
- sky và ground: không có điểm trong suốt. far: mép dưới ảnh kín màu, bốn góc trên trong suốt.
- ground và far: cột px trái nhất và phải nhất cùng màu ở cùng hàng (lặp ngang không thấy đường nối).
- grass và stone: bốn góc trong suốt; hình chạm mép dưới (chân ở y=60), chiếm 70 tới 95% bề rộng khung, căn giữa ngang.
- Không chữ, không số trên mọi sprite.
