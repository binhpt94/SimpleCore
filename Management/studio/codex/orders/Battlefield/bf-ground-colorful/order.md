---
tool: codex
kind: image
count: 1
deliver: Private
format: png 1920x183, phủ kín khung (không trong suốt)
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Battlefield/claudeimage/bf-ground/v1/ground.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite dải đất của nền chiến trường theo style Colorful (PROTOTYPE)
## Mô tả
Thay sprite dải đất IDEA bằng bản style Colorful, `world-cartoon` cho thế giới trận. Đây là mặt đứng của thành, hero và quái.
- Style: cùng tông Colorful với ảnh tham chiếu thứ nhất (popup thua): màu tươi, khối mềm, nguồn sáng từ trên. Mép trên sáng nhất, tối dần về mép dưới. Nền chiến trường nhạt và ít bão hoà hơn nhân vật một bậc.
- Ảnh tham chiếu thứ hai là dải đất bản IDEA đã đạt: giữ đúng bố cục và tỉ lệ (dải cỏ mỏng ở trên, đất ở dưới), chỉ đổi sang style Colorful.
- Trong game kéo phủ x −21…+21u, y −4…0u; mép trên ảnh là mặt đất. Ảnh lặp ngang được. Khoảng 1/3 trên của ảnh luôn thấy được; phần dưới nằm sau thanh nâng cấp.
- Không vật đứng nào trên mặt cỏ (decor là sprite riêng). Không chữ, không số.
## Danh sách
- ground: mép trên là một dải cỏ xanh lá tươi mỏng, mép cỏ lượn nhẹ và sáng nhất ở trên; bên dưới là đất nâu ấm có vài viên sỏi tròn nhỏ cùng tông, tối dần về mép dưới; lặp ngang được, mép trái và mép phải khớp nhau
## Tiêu chí nhận
- Đúng 1920x183; không có điểm trong suốt.
- Dải cỏ dày 15 tới 30% chiều cao ảnh, nằm sát mép trên.
- Cột px trái nhất và phải nhất cùng màu ở cùng hàng (lặp ngang không thấy đường nối).
- Không có vật nhô lên khỏi mép trên ảnh; không chữ, số hay kí tự nào.
