---
tool: codex
kind: image
count: 1
deliver: Private
format: png 400x120, nền trong suốt, 9-slice viền trái/trên/phải/dưới 40/36/40/40
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/_design/feature-home.home-screen.home-screen-spec/claudeimage/home-screen-claude/v2/parts/main/btn-main.png
design: feature-home/home-screen/home-screen-spec
---
# Nút Chơi màn bắt đầu, style Colorful (PROTOTYPE)

## Mô tả
Sprite nền cho nút "Chơi" duy nhất của màn bắt đầu (màn chuẩn 1920x1080 ngang, nút 400x120 ở x=760 y=620, dưới dòng tên game và dòng kỉ lục). Giai đoạn PROTOTYPE màn bắt đầu chỉ có 3 thành phần dạng chữ và một nút đơn giản: tên game và kỉ lục là chữ do game vẽ bằng font, không cần ảnh; chữ "Chơi" trên nút cũng do game vẽ, nên ảnh không có chữ. Chỉ cần một hình.

Phong cách giao diện ui-flat trong bảng màu Colorful, theo ảnh tham chiếu thứ nhất (concept Colorful đã giao của popup thua): giống nút chính "Chơi lại" của popup đó. Nút xanh lá dạng viên thuốc, bóng như kẹo, sáng ở mép trên và tối dần về mép dưới, viền là sắc xanh lá đậm hơn màu thân, không viền đen. Ảnh tham chiếu thứ hai chỉ để lấy cỡ, dáng và chỗ viền 9-slice, không lấy màu.

## Danh sách
- btn-play: nền nút Chơi 400x120, viên thuốc xanh lá bóng như kẹo, vệt sáng ở nửa trên, viền xanh lá đậm, không có icon và không có chữ

## Tiêu chí nhận
- Đúng 400x120, nền trong suốt ở ngoài viên thuốc.
- Không có chữ, số, icon hay kí tự nào.
- Dạng viên thuốc: hai đầu bo tròn hết chiều cao, không phải hộp chữ nhật bo góc nhỏ.
- Kéo 9-slice được: bốn góc nằm gọn trong viền trái 40, trên 36, phải 40, dưới 40 px; phần giữa chỉ là mảng màu và vệt sáng nằm ngang, kéo dài không méo.
- Viền là sắc đậm của chính màu thân, không viền đen; màu và độ bóng khớp nút "Chơi lại" ở ảnh tham chiếu thứ nhất.
