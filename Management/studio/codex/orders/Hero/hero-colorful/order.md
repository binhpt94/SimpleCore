---
tool: codex
kind: image
count: 13
deliver: Private
format: png nền trong suốt, cạnh dài 512 px, tỉ lệ rộng x cao đúng theo từng item (ghi trong Danh sách)
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-hero/hero-list/hero-roster-values
---
# Sprite hero, tư thế đánh và hiệu ứng đòn theo style Colorful (PROTOTYPE)
## Mô tả
Sprite 2D cho feature Hero trong game thủ thành nhìn ngang, màn ngang 1920x1080. Thay bộ sprite cũ (order Hero/hero-sprites, vẽ phẳng) bằng style mới `world-cartoon` Colorful cho thế giới trận, cùng tông với ảnh tham chiếu (popup thua Colorful: màu bão hoà tươi, khối phồng như kẹo, vệt bóng sáng ở mép trên, viền đậm cùng tông). Theo cột PROTOTYPE của bảng StageParams: mỗi hero một sprite đứng và một sprite khi đánh, cộng hiệu ứng đòn.
- chibi: đầu to, thân ngắn (khoảng 1 đầu : 2 thân), mắt to, mặt biểu cảm rõ, vui và dũng cảm; dáng đọc được khi chỉ nhìn bóng đen;
- hero là phe ta: màu chủ đạo vàng, cam, đỏ, xanh dương sáng, xanh lá tươi; quái phe địch dùng tím, xanh lá đậm, xanh lam tối nên hero không dùng các màu đó làm màu chính, trừ Pháp sư giữ áo tím sáng pha vàng;
- một nguồn sáng từ trên: mép trên mỗi khối sáng nhất, tối dần xuống dưới, chuyển mềm trong cùng sắc màu, một vệt bóng sáng nhỏ ở chỗ cao nhất;
- viền ngoài đậm, đều, là bản đậm của màu thân sát viền (không đen tuyền); chi tiết bên trong viền mảnh hơn;
- không texture chi tiết, không phối cảnh 3D, không vẽ nền, không bóng đổ dưới chân (game tự vẽ bóng).

Trong game mỗi hero là một hộp rộng 1u cao 1.5u (màn thấy 32u bề ngang, 1u khoảng 60 px trên 1080p, hero cao khoảng 90 px), chân trên mặt đất; game kéo sprite vừa hộp nên ảnh phải đúng tỉ lệ 2:3. Game lật ngang sprite khi hero quay sang trái, nên mọi hero quay mặt sang phải. Sprite đánh hiện trong lúc ra đòn (game nhích hero 0.2u về phía mục tiêu trong 0.15 giây); cùng nhân vật, cùng cỡ và cùng chỗ đặt chân với sprite đứng để đổi qua lại không bị giật.

Hiệu ứng: vệt chém của Đấu sĩ rìu phủ đoạn 1.5u phía trước hero, hiện 0.2 giây. Mũi tên của Cung thủ bay tới mục tiêu trong 0.15 giây, dài khoảng 0.4u. Vòng nổ của Pháp sư hiện quanh mục tiêu bán kính 1.5u, hiện 0.3 giây, nhìn nghiêng nên game vẽ dẹt gấp đôi bề ngang so với bề cao.

## Danh sách
- hero-knight: Hiệp sĩ cận chiến chibi đứng, toàn thân nhìn ngang quay phải, mũ sắt bạc có chỏm lông xanh dương, giáp bạc, áo choàng xanh dương sáng, khiên tròn lớn viền vàng phía trước, kiếm ngắn; ảnh 341x512
- hero-knight-attack: Hiệp sĩ đó lúc đánh, nhìn ngang quay phải, lao khiên về trước và chém kiếm xuống phía phải; ảnh 341x512
- hero-axeman: Đấu sĩ rìu cận chiến chibi đứng, toàn thân nhìn ngang quay phải, vạm vỡ, râu rậm cam, áo da cam nâu, rìu hai lưỡi lớn cầm hai tay; ảnh 341x512
- hero-axeman-attack: Đấu sĩ rìu đó lúc đánh, nhìn ngang quay phải, vung rìu ngang về phía phải ở cuối nhát chém; ảnh 341x512
- hero-swordsman: Kiếm khách cận chiến chibi đứng, toàn thân nhìn ngang quay phải, dáng thon nhanh nhẹn, khăn buộc đầu và áo đỏ tươi, thanh kiếm mảnh dài chĩa về trước; cùng tỉ lệ đầu và thân với bốn hero kia; ảnh 341x512
- hero-swordsman-attack: Kiếm khách đó lúc đánh, nhìn ngang quay phải, đâm kiếm thẳng về phía phải, người nghiêng tới; ảnh 341x512
- hero-archer: Cung thủ bắn xa chibi đứng, toàn thân nhìn ngang quay phải, mũ trùm và áo xanh lá tươi, cầm cung gỗ, ống tên sau lưng; ảnh 341x512
- hero-archer-attack: Cung thủ đó lúc bắn, nhìn ngang quay phải, kéo căng dây cung nhắm về phía phải; ảnh 341x512
- hero-mage: Pháp sư bắn xa chibi đứng, toàn thân nhìn ngang quay phải, mũ nhọn và áo choàng tím sáng điểm sao vàng, gậy phép có viên đá vàng sáng; ảnh 341x512
- hero-mage-attack: Pháp sư đó lúc phóng phép, nhìn ngang quay phải, giơ gậy về phía phải, viên đá loé sáng vàng; ảnh 341x512
- cleave-trail: vệt chém hình vòng cung nằm ngang của rìu, sáng trắng vàng ở mép trước nhạt dần về sau, cong về phía phải; ảnh 512x256, vệt phủ gần kín bề ngang
- hero-arrow: mũi tên của Cung thủ nằm ngang, đầu nhọn bạc chỉ sang phải, thân gỗ sáng, lông đuôi xanh lá; ảnh 512x128, mũi tên phủ gần kín bề ngang, căn giữa dọc
- blast-ring: vòng nổ phép nhìn nghiêng, hình bầu dục dẹt gấp đôi bề ngang so với bề cao, viền lửa phép vàng cam sáng với tia nhỏ toả ra, giữa trong suốt; ảnh 512x256, vòng chạm gần bốn mép

## Tiêu chí nhận
- Đúng kích thước ghi ở từng item (lệch không quá 4 px mỗi chiều), nền trong suốt hoàn toàn (alpha 0 ngoài vật), không chữ, số hay kí tự, không nền, không bóng đổ dưới chân.
- 10 sprite hero: chân chạm mép dưới (khoảng trống dưới chân không quá 8 px), đầu cách mép trên 8 đến 40 px, căn giữa ngang; vũ khí không bị cắt; tất cả nhìn ngang quay phải.
- Mỗi cặp đứng và đánh: cùng nhân vật, cùng màu, cùng cỡ đầu và thân, chỗ đặt chân lệch không quá 16 px.
- Cùng tỉ lệ đầu và thân cho cả 5 hero (đầu khoảng một phần ba chiều cao nhân vật).
- Viền ngoài liền, dày tối thiểu 8 px ở 512 px, không đen tuyền, không mờ hay nhạt hơn thân.
- Thu nhỏ còn 60x90 px vẫn phân biệt được 5 hero bằng dáng và màu chủ đạo; không hero nào dùng xanh lá đậm hay xanh lam tối làm màu chính.
- Cùng phong cách giữa 13 item và với ảnh tham chiếu: màu tươi, khối phồng có vệt bóng sáng trên, không xám đục, không phẳng màu đặc.
- hero-arrow đọc ra là mũi tên chỉ sang phải khi thu còn 32 px bề ngang; cleave-trail đọc ra là vệt chém cong về phải khi thu còn 90 px bề ngang; blast-ring đọc ra là vòng tròn khi thu còn 180x90 px.
- Mỗi item đúng một hình, một vật.
