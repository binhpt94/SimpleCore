---
tool: codex
kind: image
count: 4
deliver: Private
format: png, nền trong suốt, kích thước từng item ghi ở Danh sách
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Management/references/20261125-225339-lose.png, Resources/_design/feature-popup-lose.cheat/claudeimage/lose-popup-claude/v3/parts/new-record/1-panel.png
design: feature-popup-lose/lose-actions/actions-spec
---
# Sprite popup thua theo style Colorful (PROTOTYPE): khung, dải tiêu đề, hai nút

## Mô tả
Sprite cho popup thua ở giai đoạn PROTOTYPE, thay các hình phẳng của IDEA. Giữ nguyên bố cục đã chốt (frame `new-record` của `feature-popup-lose/cheat`, màn chuẩn 1920x1080 ngang): khung 800x700; tiêu đề "Thành đã bị phá" ở ô 700x90 cách mép trái khung 50 px, cách mép trên 40 px; nút chính "Chơi lại" 400x120 bên trái, nút phụ "Về màn bắt đầu" 260x120 bên phải, cùng hàng ở dưới khung. Mọi chữ do game vẽ bằng font, nên không ảnh nào có chữ.

Phong cách theo ảnh tham chiếu thứ nhất (concept Colorful của chính popup này, đã giao) và ảnh thứ hai (ảnh người chọn ở tab Tham chiếu, style Colorful): màu tươi, bóng như kẹo, viền là sắc đậm của chính màu thân, không viền đen. Ảnh thứ ba chỉ để lấy cỡ và chỗ viền 9-slice của khung, không lấy màu.

Chỉ vẽ từng mảnh rời, không ghép thành cả popup. Emoji, nơ, lá, sao trang trí không thuộc order này.

## Danh sách
- panel: khung popup 800x700, thân kem sáng, viền vàng cam dày có viền ngoài cam đậm, mép trên sáng nhất tối dần về mép dưới, góc bo tròn mềm, giữa trống một màu kem; 9-slice viền trái/trên/phải/dưới 48/48/48/56
- title-band: dải ruy băng tiêu đề 700x90, tím tươi bóng, viền tím đậm, hai đầu dải hơi vểnh trong khung ảnh, giữa trống để đặt chữ; 9-slice viền trái/trên/phải/dưới 60/30/60/30
- btn-main: nền nút chính Chơi lại 400x120, viên thuốc xanh lá bóng như kẹo, vệt sáng ở nửa trên, viền xanh lá đậm, không icon; 9-slice viền trái/trên/phải/dưới 40/36/40/40
- btn-sub: nền nút phụ Về màn bắt đầu 260x120, viên thuốc xanh dương bóng như kẹo, vệt sáng ở nửa trên, viền xanh dương đậm, không icon; 9-slice viền trái/trên/phải/dưới 40/36/40/40

## Tiêu chí nhận
- Đúng kích thước từng item: panel 800x700, title-band 700x90, btn-main 400x120, btn-sub 260x120.
- Nền trong suốt ở ngoài hình (bốn góc ngoài phần bo, ngoài viên thuốc, ngoài dải ruy băng).
- Không có chữ, số, icon, emoji, sao, nơ, lá, đường đứt hay ô giữ chỗ.
- Kéo 9-slice được: góc và đầu dải nằm gọn trong viền ghi ở từng dòng; phần giữa chỉ là mảng màu và vệt sáng nằm ngang, kéo dài không méo.
- Viền là sắc đậm của chính màu thân, không viền đen; panel viền vàng cam dày tối thiểu 14 px.
- Màu khớp ảnh tham chiếu thứ nhất: khung kem viền vàng cam, dải tiêu đề tím, nút chính xanh lá, nút phụ xanh dương. Nút chính nổi hơn nút phụ.
