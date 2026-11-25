---
tool: codex
kind: image
count: 1
deliver: Private
format: png 240x240, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-team-select/team-slots/slots-spec
---
# Chỗ trong đội màn chọn đội, style Colorful (PROTOTYPE)

## Mô tả
Sprite khung cho 3 chỗ trong đội ở màn chọn đội (màn 1920x1080, ba ô 240x240 xếp ngang và cách nhau 40 px, nằm dưới hàng thẻ hero). Theo cột PROTOTYPE của `slots-spec`, mỗi chỗ hiện hình của hero đang ở đó, hoặc để trống. Hình hero do game đặt lên ở vùng x=20 y=10, cỡ 200x170, tính từ góc trên trái của ô. Nếu có tên hero thì game vẽ bằng font ở phần dưới ô.

Style Colorful theo ảnh tham chiếu (popup thua Colorful đã giao): khối nổi bóng như kẹo, sáng ở mép trên và tối dần về mép dưới, viền là sắc đậm hơn của chính màu thân (không viền đen), góc bo tròn.

## Danh sách
- slot: khung một chỗ trong đội 240x240, ô vuông bo tròn, lòng lõm màu tím nhạt, viền tím đậm dày, mép trên có vệt sáng; dùng được cả khi trống lẫn khi có hero

## Tiêu chí nhận
- Đúng 240x240, nền trong suốt ở ngoài khối.
- Không có chữ, số, dấu cộng hay hình hero trên ảnh.
- Lòng ô (vùng x 20 tới 220, y 10 tới 220) là mảng màu phẳng sáng vừa, để hình hero và chữ đè lên vẫn rõ.
- Cùng độ bóng và cách viền với popup thua Colorful; khác màu với thẻ hero (thẻ kem viền vàng cam) để người chơi phân biệt thẻ với chỗ.
