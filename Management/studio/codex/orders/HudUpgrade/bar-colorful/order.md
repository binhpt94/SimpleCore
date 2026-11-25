---
tool: codex
kind: image
count: 11
deliver: Private
format: png, kích thước theo từng dòng Danh sách, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Hero/codex/hero-sprites/v1/hero-knight.png, Resources/Hero/codex/hero-sprites/v1/hero-axeman.png, Resources/Hero/codex/hero-sprites/v1/hero-swordsman.png, Resources/Hero/codex/hero-sprites/v1/hero-archer.png, Resources/Hero/codex/hero-sprites/v1/hero-mage.png
design: feature-hud-upgrade/button-state/button-display
---
# Thanh nâng cấp style Colorful (PROTOTYPE): chân dung hero, biểu tượng thành, xu, nền nút và nền nhóm

## Mô tả
Sprite cho thanh nâng cấp ở mép dưới màn trận (màn 1920x1080, điện thoại cầm ngang). Thay bộ hình cũ (order HudUpgrade/bar-icons, vẽ phẳng) theo cột PROTOTYPE của `button-display`: chân dung riêng cho từng hero, biểu tượng riêng cho máu, sức đánh và kĩ năng thành.

Style Colorful theo ảnh tham chiếu đầu tiên (popup thua Colorful đã giao): màu bão hoà tươi, khối phồng bóng như kẹo, sáng ở mép trên rồi tối dần về mép dưới, một vệt bóng sáng nhỏ ở chỗ cao nhất, viền đậm là sắc đậm hơn của chính màu thân (không viền đen tuyền), góc bo tròn. Phần giao diện (nền nút, nền nhóm) theo kiểu UI phẳng gọn của style đó: mảng màu sạch, không hoạ tiết, để chữ trắng đè lên đọc rõ.

Kích thước thật trong game:
- mỗi nút 200x140; trong nút, hình của mục ở ô 70x70 góc trên trái (x=10 y=10), chữ "Cấp N" bên phải hình, hàng dưới là xu 40x40 (x=10 y=90) và giá; chữ do game vẽ bằng font trắng đậm;
- 6 nút chia hai nhóm 3 nút (hero, thành), mỗi nhóm nằm trên một nền nhóm 620x140;
- nút mờ khi không mua được: game tự giảm độ đục, ảnh chỉ cần bản sáng.

Năm chân dung phải giống đúng hero tương ứng trong 5 ảnh sprite trận (cùng màu áo, vũ khí, mũ), nhưng vẽ lại theo style Colorful.

## Danh sách
- hero-knight: chân dung Hiệp sĩ 256x256, đầu và vai nhìn ba phần tư quay sang phải, mũ sắt bạc có chỏm lông xanh dương, giáp bạc, khiên tròn viền vàng ló rõ sau vai, giống hero-knight
- hero-axeman: chân dung Đấu sĩ rìu 256x256, đầu và vai nhìn ba phần tư quay sang phải, vạm vỡ, râu rậm cam, rìu hai lưỡi vác trên vai, tông cam nâu, giống hero-axeman
- hero-swordsman: chân dung Kiếm khách 256x256, đầu và vai nhìn ba phần tư quay sang phải, khăn buộc đầu và áo đỏ tươi, chuôi kiếm mảnh sau vai, giống hero-swordsman
- hero-archer: chân dung Cung thủ 256x256, đầu và vai nhìn ba phần tư quay sang phải, mũ trùm xanh lá tươi, cung gỗ và đầu mũi tên sau vai, giống hero-archer
- hero-mage: chân dung Pháp sư 256x256, đầu và vai nhìn ba phần tư quay sang phải, mũ nhọn tím sáng điểm sao vàng, đầu gậy phép có đá vàng sáng, giống hero-mage
- castle-health: biểu tượng máu thành 256x256, trái tim đỏ bóng như kẹo đặt trước một tháp thành đá nhỏ
- castle-attack: biểu tượng đòn tự bắn của thành 256x256, mũi tên lửa vàng cam bay chéo lên phải từ lỗ châu mai của tường đá
- castle-skill: biểu tượng gộp ba kĩ năng thành 256x256, tia sét vàng, bông tuyết xanh trời và lá cờ cổ vũ đỏ xếp chung một hình
- coin: đồng xu vàng 128x128 tròn nhìn thẳng, khối phồng bóng, vệt sáng ở mép trên trái
- btn: nền một nút nâng cấp 400x280 (gấp đôi cỡ 200x140 trong game), khối bo tròn xanh lam đậm vừa phải, sáng ở mép trên tối dần xuống dưới, viền là sắc đậm của màu thân; ô 140x140 ở góc trên trái (x=20 y=20) nhạt hơn một chút để đặt hình
- group: nền một nhóm 3 nút 620x140, khay bo tròn tối mờ hơn nền nút, viền mảnh sáng; dùng 9-slice với viền 24 px mỗi cạnh

## Tiêu chí nhận
- Đúng kích thước từng dòng (lệch không quá 4 px mỗi chiều): chân dung và biểu tượng thành 256x256, coin 128x128, btn 400x280, group 620x140.
- Nền trong suốt hoàn toàn ngoài vật; không có chữ, số hay kí tự nào trên ảnh.
- Thu nhỏ còn 70x70 px vẫn nhận ra từng hero và từng biểu tượng; coin đọc rõ ở 40x40 px.
- Chân dung và biểu tượng chiếm 80 đến 95 phần trăm khung, căn giữa; viền ngoài liền, dày tối thiểu 6 px ở 256 px, không đen tuyền.
- Năm chân dung cùng hướng nhìn, cùng cỡ đầu, cùng ánh sáng; nhận ra từng hero khi đặt cạnh sprite trận của hero đó; mỗi hero một màu chủ đạo khác nhau.
- btn và group: mảng giữa phẳng, không hoạ tiết, chữ trắng đậm cỡ 30 px đè lên đọc rõ; group giữ đúng góc bo khi kéo giãn theo 9-slice viền 24 px.
- Cùng phong cách giữa 11 item và với ảnh tham chiếu đầu tiên: màu tươi, khối phồng có vệt bóng sáng trên, không xám đục.
- Mỗi item đúng một hình, một vật.
