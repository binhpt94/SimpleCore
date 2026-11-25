---
tool: codex
kind: image
count: 1
deliver: Private
format: png 1920x640, phủ kín khung (không trong suốt)
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Battlefield/claudeimage/bf-sky/v1/sky.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite trời của nền chiến trường theo style Colorful (PROTOTYPE)
## Mô tả
Thay sprite trời IDEA bằng bản style Colorful, `world-cartoon` cho thế giới trận. Game thủ thành 2D nhìn ngang, màn ngang 1920x1080.
- Style: cùng tông Colorful với ảnh tham chiếu thứ nhất (popup thua): màu tươi, chuyển sáng tối mềm. Nền chiến trường nhạt và ít bão hoà hơn nhân vật một bậc để hero, quái nổi lên.
- Ảnh tham chiếu thứ hai là trời bản IDEA đã đạt: giữ đúng bố cục và tỉ lệ, chỉ đổi sang style Colorful.
- Trong game kéo phủ x −21…+21u, y 0…14u (tỉ lệ 3:1, đúng tỉ lệ ảnh). Mép dưới ảnh là chân trời, nằm sau cảnh xa và dải đất. Phần trên khoảng 16% chiều cao nằm sau dải HUD trên nên không đặt gì đáng chú ý ở đó.
- Trời đứng yên; không mây, không mặt trời (mây trôi để FULL). Không chữ, không số, không vẽ thành, unit, cảnh xa hay HUD.
## Danh sách
- sky: trời xanh sáng Colorful, chuyển mềm từ xanh dương tươi ở mép trên xuống xanh rất nhạt gần trắng ở chân trời; không mây, không mặt trời, không vật gì khác
## Tiêu chí nhận
- Đúng 1920x640; không có điểm trong suốt.
- Mép trên đậm hơn mép dưới; không có dải màu hay đường kẻ cứng ngang qua ảnh.
- Cột px trái nhất và phải nhất cùng màu ở cùng hàng (đặt nối ngang không thấy đường nối).
- Không chữ, số hay kí tự nào; không mây, không mặt trời.
