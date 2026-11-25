---
tool: codex
kind: image
count: 8
deliver: Private
format: png, kích thước theo từng dòng Danh sách, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/_design/feature-hud.cheat/claudeimage/hud-cheat-claude/v2/mid-battle.png
design: feature-hud/hud-layout/layout-spec
---
# HUD trận style Colorful (PROTOTYPE): dải nền, khung máu, xu, khay và ba nút kĩ năng, nút tạm dừng

## Mô tả
Sprite cho HUD trận của game thủ thành nhìn ngang, màn chuẩn 1920x1080, điện thoại cầm ngang. Thay bộ hình cũ vẽ phẳng (order claudeimage Hud/hud-band, hud-hp, hud-coin, skill-tray, skill-buttons) theo style Colorful mới của node gốc (`ui-flat`, nay là giao diện Colorful kiểu casual mobile), và thêm nút tạm dừng vì từ PROTOTYPE HUD có nút này.

Style theo ảnh tham chiếu đầu tiên (popup thua Colorful đã giao): màu bão hoà tươi, khối phồng bóng như kẹo, sáng ở mép trên rồi tối dần về mép dưới, một vệt bóng sáng nhỏ ở chỗ cao nhất, viền là sắc đậm hơn của chính màu thân (không viền đen tuyền), bo tròn lớn, bóng đổ lệch xuống dưới. Ảnh tham chiếu thứ hai chỉ để biết chỗ đặt và cỡ từng phần trên màn (bố cục cũ, vẽ phẳng): không lấy style của nó.

Mã màu lấy theo bảng màu chung của style (sRGB):
- vàng thân #FFC61F, sáng #FFE780, bóng #F29A0E, viền #C26A00;
- xanh dương thân #22A0F0, sáng #7CD0FF, bóng #1270C8, viền #0B4C94;
- đỏ thân #F0383C, sáng #FF8A80, bóng #B81E28, viền #7E1018;
- tím thân #9B30C8, sáng #C77BEA, bóng #6E1E99, viền #4A1070.

Mỗi kĩ năng thành một màu chủ đạo, dùng lại trên hiệu ứng trong trận: Sấm sét vàng, Băng giá xanh dương, Cổ vũ đỏ.

Kích thước thật trong game và cách game dùng:
- band dùng cho dải mép trên cao 140 và dải mép dưới cao 180, kéo giãn 9-slice viền 24 px;
- hp là khung thanh máu ở giữa mép trên; game tự vẽ phần đỏ theo lượng máu nằm lọt trong lòng khung, cách mép ảnh 6 px, và tự in số "hiện tại/tối đa" ở giữa bằng chữ trắng viền tối;
- tray là khay dưới cụm 3 nút kĩ năng ở góc dưới phải, luôn rộng 460;
- mỗi nút kĩ năng 140x140; game in tên kĩ năng ở nửa dưới nút, số giây hồi chiêu ở giữa, và tự phủ một lớp tối khi kĩ năng đang hồi, nên ảnh chỉ cần bản sẵn sàng;
- nút tạm dừng 120x120 ở góc trên trái.

## Danh sách
- band: dải nền HUD 1920x140, khay bo tròn tím đậm bán trong suốt, độ đục phần giữa khoảng 55 phần trăm, mép trên có một vệt sáng mảnh, viền mảnh tím đậm; 9-slice viền 24 px mỗi cạnh
- hp: khung thanh máu 600x60, khung bo tròn kiểu viên thuốc viền vàng dày bóng như kẹo, lòng khung là một mảng đỏ thẫm rất tối phẳng (chỗ máu đã mất), không vẽ phần đỏ đầy máu; 9-slice viền 24 px mỗi cạnh
- coin: đồng xu vàng 60x60 tròn nhìn thẳng, khối phồng bóng, vệt sáng ở mép trên trái, viền vàng đậm
- tray: khay dưới cụm kĩ năng 460x140, khay bo tròn tím đậm hơn band một bậc, vành vàng mảnh, mặt trong phẳng; 9-slice viền 24 px mỗi cạnh
- skill-thunder: nút Sấm sét 140x140, nút tròn bo góc lớn thân vàng bóng như kẹo, biểu tượng tia sét trắng viền vàng đậm ở giữa nửa trên
- skill-frost: nút Băng giá 140x140, cùng khung với skill-thunder nhưng thân xanh dương, biểu tượng bông tuyết trắng viền xanh đậm ở giữa nửa trên
- skill-rally: nút Cổ vũ 140x140, cùng khung với skill-thunder nhưng thân đỏ, biểu tượng lá cờ trắng viền đỏ đậm ở giữa nửa trên
- pause: nút tạm dừng 120x120, nút tròn thân xanh dương bóng như kẹo, hai vạch đứng trắng bo tròn ở giữa, không chữ

## Tiêu chí nhận
- Đúng kích thước từng dòng, lệch không quá 4 px mỗi chiều: band 1920x140, hp 600x60, coin 60x60, tray 460x140, ba nút kĩ năng 140x140, pause 120x120.
- Nền trong suốt hoàn toàn ngoài vật; bốn góc của band, hp, tray trong suốt theo góc bo.
- Không có chữ, số hay kí tự nào trên ảnh.
- band, hp, tray: góc bo, viền và vệt sáng nằm trọn trong 24 px mỗi cạnh; phần giữa là mảng màu phẳng, không hoạ tiết, để kéo giãn không méo.
- band: nhìn xuyên thấy cảnh trận phía sau ở phần giữa; độ đục phần giữa từ 45 tới 65 phần trăm.
- hp: lòng khung bắt đầu cách mép ảnh không quá 6 px để phần đỏ game vẽ nằm lọt trong viền.
- Ba nút kĩ năng cùng khung, cùng bán kính bo, cùng ánh sáng; biểu tượng nằm trong y từ 10 tới 85 px để chừa chỗ tên kĩ năng ở dưới; ba nút phân biệt được ngay bằng màu và hình khi thu còn 70x70 px.
- Viền ngoài nút và xu liền, dày tối thiểu 4 px ở cỡ trong game, là sắc đậm của màu thân, không đen tuyền.
- coin đọc rõ ở 40x40 px.
- Cùng phong cách giữa 8 item và với ảnh tham chiếu đầu tiên: màu tươi, khối phồng có vệt bóng sáng trên, không xám đục, không phẳng màu đặc.
- Mỗi item đúng một hình, một vật.
