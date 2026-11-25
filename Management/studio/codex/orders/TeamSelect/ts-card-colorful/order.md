---
tool: codex
kind: image
count: 7
deliver: Private
format: png, kích thước theo từng dòng Danh sách, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png, Resources/Hero/codex/hero-sprites/v1/hero-knight.png, Resources/Hero/codex/hero-sprites/v1/hero-axeman.png, Resources/Hero/codex/hero-sprites/v1/hero-swordsman.png, Resources/Hero/codex/hero-sprites/v1/hero-archer.png, Resources/Hero/codex/hero-sprites/v1/hero-mage.png
design: feature-team-select/hero-cards/card-spec
---
# Thẻ hero màn chọn đội, style Colorful (PROTOTYPE)

## Mô tả
Sprite cho 5 thẻ hero ở màn chọn đội (màn 1920x1080, điện thoại cầm ngang). Theo cột PROTOTYPE của `card-spec`, mỗi thẻ có: hình hero, tên, dòng cách đánh và dấu đã chọn. Chưa cần biểu tượng vai trò vì ở giai đoạn này nó chỉ là chữ. Tên và dòng cách đánh do game tự vẽ bằng font, nên ảnh không có chữ.

Style Colorful theo ảnh tham chiếu đầu tiên (popup thua Colorful đã giao): khối nổi bóng như kẹo, sáng ở mép trên và tối dần về mép dưới, viền là sắc đậm hơn của chính màu thân (không dùng viền đen), góc bo tròn.

Bố cục thẻ 300x400 (toạ độ tính từ góc trên trái của thẻ):
- hình hero x=30 y=20, cỡ 240x200;
- tên ở y=235, cao 50;
- dòng cách đánh ở y=290, cao 40;
- dấu đã chọn x=232 y=12, cỡ 56x56, chồm lên góc trên phải.

Hình hero phải giống đúng hero tương ứng trong 5 ảnh sprite trận (cùng màu áo, vũ khí, mũ), nhưng vẽ lại theo style Colorful.

## Danh sách
- card: nền thẻ hero 300x400, thân kem sáng, viền vàng cam dày bo tròn như khung popup Colorful; có một ô lõm 240x200 ở vị trí hình hero, nền xanh trời nhạt; nửa dưới để trống, phẳng, đủ sáng để chữ tím đậm đọc rõ
- picked: dấu đã chọn 56x56, huy hiệu tròn xanh lá bóng như kẹo, dấu tích trắng ở giữa, viền xanh lá đậm
- portrait-knight: chân dung Hiệp sĩ 240x200 (đầu và vai, nhìn hơi nghiêng sang phải), mũ sắt, giáp bạc, khiên, giống hero-knight
- portrait-axeman: chân dung Đấu sĩ rìu 240x200 (đầu và vai), râu rậm, rìu hai lưỡi, tông cam nâu, giống hero-axeman
- portrait-swordsman: chân dung Kiếm khách 240x200 (đầu và vai), khăn buộc đầu, kiếm mảnh, giống hero-swordsman
- portrait-archer: chân dung Cung thủ 240x200 (đầu và vai), mũ trùm xanh lá, cung và mũi tên, giống hero-archer
- portrait-mage: chân dung Pháp sư 240x200 (đầu và vai), mũ nhọn tím, gậy phép có đá vàng, giống hero-mage

## Tiêu chí nhận
- Đúng kích thước từng dòng: card 300x400, picked 56x56, portrait-* 240x200.
- Nền trong suốt ở ngoài khối của card và picked. Portrait có nền trong suốt để đặt lên ô lõm của card.
- Không có chữ, số hay kí tự nào trên ảnh.
- card: nửa dưới (y từ 230 tới 390) là mảng màu phẳng, không có hoạ tiết, để chữ đè lên đọc rõ.
- picked đọc rõ ở 56 px: dấu tích nhìn thấy ngay.
- Năm portrait cùng cỡ đầu, cùng góc nhìn, cùng ánh sáng; nhận ra từng hero khi đặt cạnh sprite trận của hero đó.
- Màu và độ bóng hợp với popup thua Colorful: sáng ở mép trên, viền là sắc đậm của màu thân.
