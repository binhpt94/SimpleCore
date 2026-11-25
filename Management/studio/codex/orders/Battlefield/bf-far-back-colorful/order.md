---
tool: codex
kind: image
count: 1
deliver: Private
format: png 1920x146, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Battlefield/claudeimage/bf-far/v1/far.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite dải cảnh xa sau (núi xa) của nền chiến trường theo style Colorful (PROTOTYPE)
## Mô tả
Dải cảnh xa phía sau của nền màn trận, style Colorful `world-cartoon`. Ở PROTOTYPE nền có hai dải cảnh xa tạo chiều sâu; đây là dải sau, xa hơn, ló lên sau dải trước.
- Trong game kéo phủ x −21…+21u, chân ở mặt đất y=0, đỉnh y=3.2u (tỉ lệ khoảng 13:1, đúng tỉ lệ ảnh). Thành cao 4u đứng giữa, nên dải này luôn thấp hơn thành.
- Hình: dãy núi đồi tròn mềm. Cỡ hình nhỏ bằng 0.6 lần đồi của dải trước, để đọc là xa hơn.
- Màu gốc là Xanh dương #22A0F0 của bảng màu chung, **đã pha 45% về màu trời #7CD0FF** (vẽ sẵn trong ảnh): nhạt và ít tương phản, nhạt hơn dải trước rõ một bậc.
- Style như ảnh tham chiếu thứ nhất: khối tròn trịa, mép trên mỗi đỉnh sáng nhất, tối dần xuống; viền mảnh là bản đậm của màu thân, không đen tuyền. Ảnh tham chiếu thứ hai là dải cảnh xa IDEA để thấy bố cục dải: phần trên đường núi trong suốt, chân phủ kín.
- Đứng yên. Không chữ, không số, không vẽ trời, thành, unit, cây đơn lẻ hay vật đứng.
## Danh sách
- far-back: dãy núi đồi xa tròn mềm màu xanh dương đã pha nhạt về màu trời, đỉnh nhấp nhô nhẹ không đều, phần trên đường núi trong suốt, dải màu liền kín tới mép dưới; lặp ngang được
## Tiêu chí nhận
- Đúng 1920x146; phía trên đường núi alpha 0; hàng px dưới cùng không có điểm trong suốt.
- Đỉnh cao nhất chạm hoặc gần chạm mép trên (cách không quá 10 px); chỗ thấp nhất của đường núi không thấp hơn nửa ảnh.
- Cột px trái nhất và phải nhất cùng màu và cùng độ cao đường núi (lặp ngang không thấy đường nối).
- Nhạt rõ hơn bản dải trước (`bf-far-front-colorful`) khi đặt cạnh nhau; không chữ, số hay kí tự nào.
