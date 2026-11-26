# Giao diện Colorful kiểu casual mobile

## Dùng cho
UI: HUD (thanh máu thành, ô tiền, ô đợt), nút kĩ năng, thanh nâng cấp, popup, màn bắt đầu, màn chọn đội, icon.
Ảnh tham chiếu người chọn: `Management/references/20261125-225339-lose.png` (popup thua, ghi chú "Tôi dùng phong cách này · Colorfull").

## Góc nhìn
Nhìn thẳng, không phối cảnh. Khối có độ phồng (bóng sáng, bóng tối bên trong) như kẹo, nhưng bố cục vẫn phẳng trên màn.

## Bảng màu
- Màu bão hoà cao và tươi: tím, vàng, hồng, xanh lá, xanh dương, cam. Không dùng màu xám đục cho phần chính.
- Mỗi vai trò một màu, giữ ở mọi màn:
  - nút chính (chơi, chơi lại, mua): xanh lá;
  - nút về màn trước hoặc nút trung tính: xanh dương;
  - ruy băng tiêu đề: tím; viền khung, sao, tiền: vàng.
- Thân popup là nền sáng ấm (kem, hồng nhạt) để chữ tối đọc rõ; ô thông tin bên trong là một mảng màu nhạt hơn, bo tròn.
- Nền phía sau popup bị tối đi để popup nổi lên.
- Trạng thái "không bấm được" là bản mờ của chính nút đó (giảm bão hoà và độ sáng), không đổi sang màu khác.

## Bảng màu chung (sRGB, dùng chung với `world-cartoon`; mã dự kiến, lấy theo ảnh tham chiếu)
| Màu | Thân | Vùng sáng | Bóng | Viền |
|---|---|---|---|---|
| Tím (ruy băng, nhãn, chữ phụ) | #9B30C8 | #C77BEA | #6E1E99 | #4A1070 |
| Vàng (khung, sao, tiền, chữ tiêu đề) | #FFC61F | #FFE780 | #F29A0E | #C26A00 |
| Xanh lá (nút chính) | #4CC21E | #9BEA5C | #2E9414 | #1D6B0C |
| Xanh dương (nút về, nút trung tính) | #22A0F0 | #7CD0FF | #1270C8 | #0B4C94 |
| Hồng (quảng cáo, hồi sinh, nơ) | #F24FA0 | #FF9ACB | #C42A78 | #8E1556 |
| Đỏ (máu, tim) | #F0383C | #FF8A80 | #B81E28 | #7E1018 |
| Kem (thân popup) | #FFF1E4 | #FFFFFF | #F6D7C6 | #E0A27A |
| Hồng nhạt (ô thông tin) | #F7D5E8 | #FDEBF5 | #E6B3D0 | #C98AB0 |
| Chữ tối | #3B1E4F | | | |
- Vùng sáng chiếm mép trên khối, bóng ở mép dưới; viền là cột "Viền" của chính màu thân.
- Màu mới (ví dụ màu kĩ năng, màu phe) thêm vào bảng này, không tự chọn khi vẽ.

## Nét viền
- Khung popup: viền vàng dày, có viền ngoài đậm hơn một bậc; tối thiểu 8 px ở 1080p.
- Nút và icon: viền màu đậm hơn thân một bậc (không dùng đen tuyền), tối thiểu 4 px ở 1080p.
- Chữ tiêu đề và chữ trên nút: viền chữ đậm cùng tông, cộng một lớp bóng đổ dưới chữ.
- Bo tròn lớn: nút dạng viên thuốc (bo bằng nửa chiều cao); khung popup bo góc rộng, có thể có vành uốn ở góc trên.

## Độ chi tiết
- Icon kiểu emoji: một hình tròn trịa có mặt hoặc khối rõ, đọc được ở 64x64 px; tối đa 2 chi tiết phụ.
- Chữ to, nét tròn và dày; chữ trên nút cao tối thiểu 36 px ở 1080p, tiêu đề popup to nhất màn.
- Một font tròn, đậm, có đủ dấu tiếng Việt cho mọi chữ và số của game. Ảnh và SVG để trống chỗ chữ; game vẽ chữ lên.
- Chữ và số trên HUD đạt tương phản tối thiểu 4,5:1 với nền ngay sau nó, kiểm trên khung trận ghép (trời, cảnh xa, mặt đất, hiệu ứng). Không đạt thì thêm mảng nền hoặc viền chữ.
- Được trang trí quanh khung popup và tiêu đề: sao, nơ, lá, tia sáng, lấp lánh. Trang trí nằm ở viền và góc, không đè lên chữ hay nút.

## Ánh sáng
- Một nguồn sáng từ trên. Mỗi khối có một vệt bóng sáng ở mép trên (bóng như kẹo) và tối dần về mép dưới.
- Gradient dọc nhẹ trong một khối được phép (cùng sắc màu, sáng trên tối dưới).
- Bóng đổ dưới nút và popup lệch xuống 4 đến 8 px ở 1080p.
- Tiêu đề và icon chính được một quầng sáng hoặc tia sáng phía sau.

## Làm
- Tươi, vui, thân thiện; mỗi màn có một thứ nổi nhất (tiêu đề hoặc nút chính).
- Cùng một icon cho cùng một thứ ở mọi nơi (ví dụ icon tiền ở HUD và ở nút nâng cấp).
- Nút chính to hơn nút phụ và nằm trên; nút phụ xếp hàng ngang bên dưới.
- Popup lấy hình thức (khung, ruy băng, nút, trang trí) từ ảnh tham chiếu; chữ, số, số nút lấy từ thiết kế của chính popup đó, không chép nội dung ảnh (ô "Reward", nút "Revive" trong popup thua).
- Mỗi kĩ năng thành một màu chủ đạo trong bảng màu chung; icon nút kĩ năng trên HUD và hiệu ứng trong trận dùng cùng màu đó.
- Đối chiếu mọi asset UI với khung trận mẫu đã duyệt (xem `world-cartoon`, mục Làm).

## Không làm
- Không phong cách phẳng màu đặc không bóng, không tối giản kiểu thiết kế web.
- Không màu xám, nâu đục làm màu chính; không bảng màu tối toàn màn.
- Không hiệu ứng kính mờ, không texture ảnh thật.
- Không chi tiết mảnh hơn 3 px ở 1080p.
- Không vẽ chữ vào ảnh; chữ do game hiển thị (concept được vẽ chữ để xem bố cục).
