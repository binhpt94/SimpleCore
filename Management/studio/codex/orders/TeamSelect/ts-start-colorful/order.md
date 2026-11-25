---
tool: codex
kind: image
count: 1
deliver: Private
format: png 400x120, nền trong suốt, 9-slice viền trái/trên/phải/dưới 40/36/40/40
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-team-select/team-slots/slots-spec
---
# Nút Vào trận màn chọn đội, style Colorful (PROTOTYPE)

## Mô tả
Sprite nền cho nút chính "Vào trận" ở màn chọn đội (màn 1920x1080, nút 400x120 nằm giữa, dưới ba chỗ trong đội). Chữ "Vào trận" do game vẽ bằng font, nên ảnh không có chữ. Khi đội chưa đủ 3 hero, game tự làm xám nút này, nên chỉ cần một hình.

Style Colorful theo ảnh tham chiếu: giống nút chính "Chơi lại" của popup thua Colorful. Nút xanh lá dạng viên thuốc, bóng như kẹo, sáng ở mép trên và tối dần về mép dưới, viền xanh lá đậm hơn màu thân.

## Danh sách
- btn-start: nền nút Vào trận 400x120, viên thuốc xanh lá bóng như kẹo, vệt sáng ở nửa trên, viền xanh lá đậm, không có icon và không có chữ

## Tiêu chí nhận
- Đúng 400x120, nền trong suốt ở ngoài viên thuốc.
- Không có chữ, icon hay kí tự nào.
- Kéo 9-slice được: bốn góc nằm gọn trong viền trái 40, trên 36, phải 40, dưới 40 px; phần giữa là mảng màu và vệt sáng nằm ngang, kéo dài không bị méo.
- Màu và độ bóng khớp nút "Chơi lại" của popup thua Colorful.
