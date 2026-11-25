---
tool: codex
kind: image
count: 5
deliver: Private
format: png 512x512, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Castle/codex/castle-sprites/v1/castle.png
design: feature-castle/castle-core/castle-body-values
---
# Sprite thành, đạn, chớp trúng và Sấm sét theo style Colorful (PROTOTYPE)

## Mô tả
Thay bộ sprite IDEA của Castle bằng bản style Colorful mới, `world-cartoon` cho thế giới trận. Game thủ thành 2D nhìn ngang, màn ngang 1920x1080.
- Style: hoạt hình 2D Colorful cùng tông giao diện ở ảnh tham chiếu thứ nhất (popup thua): màu bão hoà và tươi, khối tròn trịa, chuyển sáng tối mềm trong cùng sắc màu, một vệt bóng sáng nhỏ như kẹo ở chỗ cao nhất, nguồn sáng từ trên.
- Viền ngoài đậm, đều, dày tối thiểu 4 px ở 512 px, màu viền là bản đậm của màu thân sát viền, không đen tuyền.
- Ảnh tham chiếu thứ hai là thành bản IDEA đã đạt: giữ đúng bố cục, tỉ lệ và chỗ đặt trong khung, chỉ đổi sang style Colorful.
- Mỗi file một vật, nền trong suốt, không bóng đổ dưới chân. Mọi chuyển động như nháy, rung, thu nhỏ, bay do code làm.

Kích thước thật trong game:
- Thành rộng 3u, cao 4u, đứng giữa màn; màn thấy 32u bề ngang nên thành cao khoảng 270 px trên 1080p. Thân thành ở tầm cao của hero, khoảng nửa dưới, là mảng màu liền ở hai mép, không hoa văn rối, để hero đứng sát thành vẫn rõ hình.
- Đạn khoảng 0.3u, chớp trúng khoảng 0.6u, Sấm sét khoảng 1.5u hiện trên đầu quái bị trúng.
- Sấm sét dùng màu chủ đạo của nút Sấm sét trên HUD: vàng sáng và xanh trắng.

## Danh sách
- castle: tòa thành đá Colorful vẽ chính diện, đối xứng trái phải, tỉ lệ rộng:cao đúng 3:4, chân chạm mép dưới ảnh, căn giữa; cổng ở giữa, hai tháp hai bên, tường đá màu kem ấm, mái tháp đỏ tươi bóng như kẹo, cờ vàng nhỏ trên đỉnh tháp
- castle-damaged: đúng tòa thành của item castle, cùng tỉ lệ, cùng bố cục và chỗ đặt từng pixel lớn, nhưng bị hư dưới nửa máu: vài viên đá vỡ mẻ ở mép tường và tháp, một vết nứt lớn trên tường, cờ rách, một mái tháp sứt; vẫn màu tươi, không cháy đen toàn thân
- shot: viên đạn năng lượng tròn màu vàng cam Colorful, có vệt bóng sáng nhỏ và viền đậm, căn giữa ảnh, chiếm khoảng 60% ảnh
- shot-hit: chớp trúng nhỏ khi đạn chạm quái, một ngôi sao nổ vàng cam bốn đến sáu cánh kèm vài chấm lấp lánh, có viền đậm, căn giữa ảnh, chiếm khoảng 70% ảnh
- thunder: tia sét đánh xuống, khối rõ, màu vàng sáng lõi xanh trắng, viền đậm, kèm chớp nổ hình sao nhỏ ở chân tia và vài lấp lánh; căn giữa ảnh, chiếm khoảng 80% chiều cao

## Tiêu chí nhận
- Đúng 512x512, nền trong suốt với alpha 0 ngoài vật, không bóng đổ dưới chân, không nền phía sau.
- castle và castle-damaged: phần không trong suốt có tỉ lệ rộng:cao 3:4, lệch không quá 5 phần trăm; chân thành cách mép dưới ảnh không quá 8 px; hai ảnh trùng khung bao, lệch không quá 8 px mỗi cạnh, để đổi hình không làm thành nhảy cỡ; đối xứng trái phải ở castle; nhìn chính diện, không phối cảnh.
- Viền ngoài liền, dày tối thiểu 4 px ở 512 px, không viền mờ hay nhạt hơn thân.
- shot, shot-hit đọc rõ khi thu xuống 32 px; thunder nhận ra là tia sét khi thu xuống 96 px.
- Cùng tông màu Colorful với ảnh tham chiếu popup thua.
- Không chữ, số hay kí tự nào trong ảnh.
