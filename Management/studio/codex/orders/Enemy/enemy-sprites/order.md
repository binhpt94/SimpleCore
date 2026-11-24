---
tool: codex
kind: image
count: 5
deliver: Private
format: png nền trong suốt, cạnh dài 512 px, tỉ lệ rộng x cao đúng theo hộp của từng item (ghi trong Danh sách)
design: feature-enemy/enemy-list/enemy-type-values
---
# Sprite toàn thân của 5 loại quái trên chiến trường
## Mô tả
Sprite 2D của từng loại quái đi trên dải đất của trận (game thủ thành nhìn ngang, điện thoại cầm ngang). Trong game mỗi quái là một hộp có rộng x cao riêng (đơn vị u, 1u khoảng 50 px trên màn 1920x1080), chân đặt trên mặt đất; game kéo sprite cho vừa đúng hộp, nên tỉ lệ ảnh phải khớp tỉ lệ hộp. Game lật ngang sprite khi quái đi sang trái, nên mọi quái vẽ nhìn ngang, quay mặt sang phải.
Cùng phong cách với sprite hero (order Hero/hero-sprites): phẳng (flat), viền tối rõ, không chữ, không nền, không bóng đổ dưới đất. Quái là phe địch: tông màu tối và gắt hơn hero (đỏ sẫm, xám, xanh rêu, tím), dáng hung dữ, đọc ra được ở cỡ nhỏ. Cùng phong cách cho cả 5 loại.
## Danh sách
- enemy-soldier: Lính quái cận chiến, toàn thân nhìn ngang quay phải, giáp sắt sứt mẻ màu xám đen với áo đỏ sẫm, mũ sừng nhỏ, cầm kiếm cong; hộp 1x1.2, ảnh 427x512
- enemy-wolf: Sói cận chiến, cả con nhìn ngang quay phải, thấp và dài, lông xám tro, mắt đỏ, đang chạy với miệng nhe nanh; hộp 1.2x0.8, ảnh 512x341
- enemy-giant: Khổng lồ cận chiến, toàn thân nhìn ngang quay phải, to bè, da xanh rêu, đóng khố da, cầm chuỳ gỗ lớn; hộp 2x2.5, ảnh 410x512
- enemy-archer: Cung thủ quái bắn xa, toàn thân nhìn ngang quay phải, áo trùm đầu tím sẫm che mặt, giương cung đen về phía phải; hộp 1x1.2, ảnh 427x512
- enemy-boss: Trùm cận chiến, toàn thân nhìn ngang quay phải, chiến binh quỷ to lớn giáp đen viền đỏ, sừng lớn, mắt phát sáng, cầm rìu khổng lồ; hộp 3x3.5, ảnh 439x512
## Tiêu chí nhận
- Đúng kích thước ghi ở từng item (sai lệch không quá 4 px mỗi chiều), nền trong suốt hoàn toàn, không chữ, không nền, không bóng dưới đất.
- Chân chạm mép dưới (khoảng trống dưới chân không quá 8 px), thân lấp gần kín khung theo cả hai chiều (khoảng trống mỗi mép không quá 24 px), căn giữa theo chiều ngang; vũ khí không bị cắt.
- Nhìn ngang, quay mặt sang phải, cả 5 loại cùng hướng.
- Thu nhỏ còn 50 px cạnh dài vẫn phân biệt được 5 loại bằng dáng và màu; nét viền tối tối thiểu 3 px ở 512 px.
- Cùng phong cách với sprite hero, cùng phong cách giữa 5 loại; nhìn là biết phe địch.
- Mỗi item đúng một hình, một con.
