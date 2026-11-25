---
tool: codex
kind: image
count: 3
deliver: Private
format: png, nền trong suốt, kích thước từng item ghi ở Danh sách
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/_design/feature-popup-revive.revive-popup.revive-spec/claudeimage/revive-spec-claude/v2/main.png
design: feature-popup-revive/revive-popup/revive-spec
---
# Sprite popup hồi sinh theo style Colorful (PROTOTYPE): khung và hai nút

## Mô tả
Sprite cho popup hồi sinh ở giai đoạn PROTOTYPE, thay các ô màu đặc vẽ tạm. Giữ nguyên bố cục đã chốt (frame `main` của `feature-popup-revive/revive-popup/revive-spec`, màn chuẩn 1920x1080 ngang, ảnh tham chiếu thứ hai chỉ để lấy bố cục, không lấy màu): khung 800x700; nút chính "Xem quảng cáo" 400x120 bên trái, nút phụ "Kết thúc lượt" 260x120 bên phải, cùng hàng ở dưới khung. Mọi chữ do game vẽ bằng font, nên không ảnh nào có chữ.

Phong cách theo ảnh tham chiếu thứ nhất: concept Colorful của popup thua, đã giao, popup đi ngay trước popup thua nên phải cùng một bộ. Màu tươi, bóng như kẹo, viền là sắc đậm của chính màu thân, không viền đen. Theo style `ui-flat`: nút xem quảng cáo luôn màu hồng (thân #F24FA0, vùng sáng #FF9ACB, bóng #C42A78, viền #8E1556), kể cả khi là nút chính; nút trung tính xanh dương (thân #22A0F0, vùng sáng #7CD0FF, bóng #1270C8, viền #0B4C94); khung kem (#FFF1E4) viền vàng (#FFC61F, viền ngoài #C26A00).

Trạng thái chờ quảng cáo do game làm mờ chính hai nút này, không cần ảnh riêng. Chỉ vẽ từng mảnh rời, không ghép thành cả popup. Ruy băng tiêu đề, emoji, nơ, lá, sao trang trí không thuộc order này.

## Danh sách
- panel: khung popup 800x700, thân kem sáng, viền vàng cam dày có viền ngoài cam đậm, mép trên sáng nhất tối dần về mép dưới, góc bo tròn mềm, giữa trống một màu kem; 9-slice viền trái/trên/phải/dưới 48/48/48/56
- btn-watch: nền nút chính Xem quảng cáo 400x120, viên thuốc hồng bóng như kẹo, vệt sáng ở nửa trên, viền hồng đậm, không icon; 9-slice viền trái/trên/phải/dưới 40/36/40/40
- btn-end: nền nút phụ Kết thúc lượt 260x120, viên thuốc xanh dương bóng như kẹo, vệt sáng ở nửa trên, viền xanh dương đậm, không icon; 9-slice viền trái/trên/phải/dưới 40/36/40/40

## Tiêu chí nhận
- Đúng kích thước từng item: panel 800x700, btn-watch 400x120, btn-end 260x120.
- Nền trong suốt ở ngoài hình (bốn góc ngoài phần bo, ngoài viên thuốc).
- Không có chữ, số, icon, emoji, sao, nơ, lá, đường đứt hay ô giữ chỗ.
- Kéo 9-slice được: góc nằm gọn trong viền ghi ở từng dòng; phần giữa chỉ là mảng màu và vệt sáng nằm ngang, kéo dài không méo.
- Viền là sắc đậm của chính màu thân, không viền đen; panel viền vàng cam dày tối thiểu 14 px, nút viền tối thiểu 4 px.
- Màu: btn-watch hồng, không phải xanh lá; btn-end xanh dương; panel kem viền vàng cam như ảnh tham chiếu thứ nhất. Nút hồng nổi hơn nút xanh dương.
