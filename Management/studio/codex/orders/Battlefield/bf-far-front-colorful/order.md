---
tool: codex
kind: image
count: 1
deliver: Private
format: png 1920x91, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Battlefield/claudeimage/bf-far/v1/far.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite dải cảnh xa trước (đồi cây) của nền chiến trường theo style Colorful (PROTOTYPE)
## Mô tả
Dải cảnh xa phía trước của nền màn trận, style Colorful `world-cartoon`. Ở PROTOTYPE nền có hai dải cảnh xa tạo chiều sâu; đây là dải trước, gần hơn, che chân dải núi xa phía sau.
- Trong game kéo phủ x −21…+21u, chân ở mặt đất y=0, đỉnh y=2u (tỉ lệ 21:1, đúng tỉ lệ ảnh). Thành cao 4u đứng giữa; hero và quái đứng trước dải này.
- Hình: dãy đồi tròn có tán cây tròn như kẹo. Đây là cỡ hình mốc (1 lần); đồi của dải sau nhỏ bằng 0.6 lần.
- Màu gốc là Xanh lá #4CC21E của bảng màu chung, **đã pha 20% về màu trời #7CD0FF** (vẽ sẵn trong ảnh): tươi nhưng nhạt hơn nhân vật một bậc để viền thành, hero, quái đủ tương phản.
- Style như ảnh tham chiếu thứ nhất: khối tròn trịa, sáng ở mép trên, tối dần xuống, có vệt bóng sáng nhỏ ở đỉnh tán; viền là bản đậm của màu thân, không đen tuyền. Ảnh tham chiếu thứ hai là dải cảnh xa IDEA để thấy bố cục dải: phần trên đường đồi trong suốt, chân phủ kín.
- Đứng yên. Không chữ, không số, không vẽ trời, thành, unit, cờ hay hàng rào (là sprite riêng).
## Danh sách
- far-front: dãy đồi cây xanh lá tròn mềm đã pha nhạt 20% về màu trời, tán cây tròn nhấp nhô, phần trên đường đồi trong suốt, dải màu liền kín tới mép dưới; lặp ngang được
## Tiêu chí nhận
- Đúng 1920x91; phía trên đường đồi alpha 0; hàng px dưới cùng không có điểm trong suốt.
- Đỉnh cao nhất chạm hoặc gần chạm mép trên (cách không quá 8 px); chỗ thấp nhất của đường đồi không thấp hơn nửa ảnh.
- Cột px trái nhất và phải nhất cùng màu và cùng độ cao đường đồi (lặp ngang không thấy đường nối).
- Không chữ, số hay kí tự nào.
