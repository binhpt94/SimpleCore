---
tool: codex
kind: image
count: 7
deliver: Private
format: png nền trong suốt, cạnh dài 512 px, tỉ lệ rộng x cao đúng theo từng item (ghi trong Danh sách)
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-enemy/enemy-list/enemy-type-values
---
# Sprite quái, mũi tên và đồng xu theo style Colorful (PROTOTYPE)
## Mô tả
Sprite 2D cho feature Enemy trong game thủ thành nhìn ngang, màn ngang 1920x1080. Thay bộ sprite cũ (order Enemy/enemy-sprites, vẽ phẳng tông xám tối) bằng style mới `world-cartoon` Colorful, cùng tông với ảnh tham chiếu (popup thua Colorful: màu bão hoà tươi, khối phồng như kẹo, vệt bóng sáng ở mép trên, viền đậm cùng tông):
- chibi: đầu to, thân ngắn (khoảng 1 đầu : 2 thân), mắt to, mặt biểu cảm rõ; dáng đọc được khi chỉ nhìn bóng đen;
- quái là phe địch: dáng khom, tai nhọn hoặc sừng; màu chủ đạo tím, xanh lá đậm, xanh lam tối, vẫn tươi, không xám đục, không tối toàn thân; hero phe ta dùng vàng, cam, đỏ, xanh dương sáng nên quái không dùng các màu đó làm màu chính;
- một nguồn sáng từ trên: mép trên mỗi khối sáng nhất, tối dần xuống dưới, chuyển mềm trong cùng sắc màu, một vệt bóng sáng nhỏ ở chỗ cao nhất;
- viền ngoài đậm, đều, là bản đậm của màu thân sát viền (không đen tuyền); chi tiết bên trong viền mảnh hơn;
- không texture chi tiết, không phối cảnh 3D, không vẽ nền, không bóng đổ dưới chân (game tự vẽ bóng bầu dục).

Trong game mỗi quái là một hộp rộng x cao (đơn vị u, màn thấy 32u bề ngang, 1u khoảng 60 px trên 1080p), chân đặt trên mặt đất; game kéo sprite vừa đúng hộp nên tỉ lệ ảnh phải khớp tỉ lệ hộp. Game lật ngang sprite khi quái đi sang trái, nên mọi quái quay mặt sang phải. Chuyển động (nhún, nhích, nháy, thu nhỏ, phóng to lúc Trùm vào màn) do game làm bằng scale, move và đổi màu.

Mũi tên bay từ Cung thủ quái tới mục tiêu trong 0.15 giây, dài khoảng 0.4u. Đồng xu hiện cạnh chữ "+5" (chữ do game vẽ) ở chỗ quái bị hạ, bay lên 1u trong 0.6 giây, cao khoảng 0.5u.

## Danh sách
- enemy-soldier: Lính quái cận chiến chibi, toàn thân nhìn ngang quay phải, da xanh lá đậm, mũ sắt có hai sừng nhỏ, giáp tím, cầm kiếm cong; hộp 1x1.2, ảnh 427x512
- enemy-wolf: Sói cận chiến chibi, cả con nhìn ngang quay phải, thấp và dài, lông xanh lam tối với bụng sáng hơn, mắt to dữ, đang chạy nhe nanh; hộp 1.2x0.8, ảnh 512x341
- enemy-giant: Khổng lồ cận chiến chibi, toàn thân nhìn ngang quay phải, to bè, da xanh lá đậm, đóng khố tím, cầm chuỳ gỗ lớn; hộp 2x2.5, ảnh 410x512
- enemy-archer: Cung thủ quái bắn xa chibi, toàn thân nhìn ngang quay phải, áo trùm đầu tím có tai nhọn lộ ra, giương cung về phía phải; khác lính quái rõ bằng áo trùm và cung; hộp 1x1.2, ảnh 427x512
- enemy-boss: Trùm cận chiến chibi, toàn thân nhìn ngang quay phải, chiến binh quỷ to nhất, giáp tím đậm viền xanh lam tối, sừng lớn cong, mắt phát sáng, cầm rìu lớn; nhìn là biết mạnh nhất; hộp 3x3.5, ảnh 439x512
- enemy-arrow: mũi tên của Cung thủ quái nằm ngang, đầu nhọn chỉ sang phải, thân gỗ, lông đuôi tím; ảnh 512x128, mũi tên phủ gần kín bề ngang, căn giữa dọc
- coin: đồng xu vàng tròn kiểu kẹo bóng, nhìn thẳng, có ngôi sao nổi ở giữa và vệt sáng ở mép trên, màu theo bảng màu chung Vàng (thân #FFC61F, vùng sáng #FFE780, bóng #F29A0E, viền #C26A00); ảnh 512x512, đồng xu căn giữa chiếm khoảng 80% ảnh

## Tiêu chí nhận
- Đúng kích thước ghi ở từng item (lệch không quá 4 px mỗi chiều), nền trong suốt hoàn toàn (alpha 0 ngoài vật), không chữ, số hay kí tự, không nền, không bóng đổ dưới chân.
- 5 quái: chân chạm mép dưới (khoảng trống dưới chân không quá 8 px), thân lấp gần kín khung theo cả hai chiều (khoảng trống mỗi mép không quá 24 px), căn giữa ngang; vũ khí không bị cắt; cả 5 nhìn ngang quay phải.
- Viền ngoài liền, dày tối thiểu 8 px ở 512 px (tương đương 4 px khi sprite cao 256 px), không đen tuyền, không mờ hay nhạt hơn thân.
- Thu nhỏ còn 60 px cạnh dài vẫn phân biệt được 5 loại bằng dáng và màu; Trùm to và nổi nhất; không loại nào dùng vàng, cam, đỏ hay xanh dương sáng làm màu chính.
- Cùng phong cách giữa 7 item và với ảnh tham chiếu: màu tươi, khối phồng có vệt bóng sáng trên, không xám đục, không phẳng màu đặc.
- enemy-arrow đọc ra là mũi tên chỉ sang phải khi thu còn 32 px bề ngang; coin đọc ra là đồng xu khi thu còn 32 px.
- Mỗi item đúng một hình, một vật.
