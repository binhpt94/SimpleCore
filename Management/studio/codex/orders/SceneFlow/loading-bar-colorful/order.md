---
tool: codex
kind: image
count: 2
deliver: Private
format: png 1000x48, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-scene-flow/scene-list/boot-loading
---
# Thanh tiến độ màn Loading theo style Colorful (PROTOTYPE)

## Mô tả
Thanh tiến độ của màn Loading (SceneFlow), giai đoạn PROTOTYPE. Theo bảng giai đoạn của node `boot-loading`, màn Loading ở PROTOTYPE vẫn chỉ có tên game (chữ do game vẽ) và thanh tiến độ, nên thanh là hình duy nhất của màn. Trong game thanh rộng 1000x48 px trên màn 1920x1080, nằm giữa màn, dưới tên game, trên nền tối.
Bản IDEA (`claudeimage/orders/SceneFlow/loading-bar`) có viền đen và lòng xám, không hợp style `ui-flat` mới (Colorful: không viền đen tuyền, không màu xám làm màu chính). Order này thay cả hai ảnh.
Unity dùng hai lớp: lớp nền nằm dưới, lớp đầy nằm trên và được cắt ngang từ trái theo tiến độ. Vì vậy cần hai ảnh cùng khung 1000x48.
Phong cách theo ảnh tham chiếu (popup thua Colorful đã giao) và style `ui-flat`: khối phồng như kẹo, vệt sáng ở mép trên, tối dần về mép dưới, viền là sắc đậm hơn của chính màu thân. Dải vàng "Kỉ lục mới!" trong ảnh tham chiếu là mẫu gần nhất cho phần đầy; ô hồng nhạt bo tròn là mẫu cho dáng viên thuốc. Không vẽ chữ.
Màu lấy từ bảng màu chung trong `ui-flat`: tím cho rãnh, vàng cho phần đầy.

## Danh sách
- bar-track: rãnh nền của thanh, dáng viên thuốc kín cả khung, lòng tím đậm #6E1E99 lõm vào (bóng tối ở mép trên lòng, sáng hơn một chút ở mép dưới), viền tím #4A1070 dày 6 px bọc quanh
- bar-fill: phần đã đầy của thanh khi đầy 100 phần trăm, dáng viên thuốc vàng #FFC61F phồng như kẹo, vệt sáng #FFE780 ở mép trên, bóng #F29A0E ở mép dưới, nằm gọn bên trong viền của bar-track (chừa đúng 6 px ở bốn phía), không có viền riêng

## Tiêu chí nhận
- Mỗi ảnh đúng 1000x48 px, nền ngoài hình trong suốt (4 góc trong suốt).
- Dáng viên thuốc: hai đầu bo bằng nửa chiều cao của chính hình; toàn bộ phần bo nằm gọn trong 24 px mỗi phía để cắt 9-slice viền [24, 24, 24, 24].
- bar-track: viền tím đậm dày 6 px (đo ở giữa cạnh trên), không có viền đen tuyền, không màu xám; lòng một sắc tím, không vạch, không chữ, không hoa văn.
- bar-fill: chồng lên bar-track cùng gốc toạ độ thì phần vàng nằm vừa trong viền, không đè lên viền tím; mép trên sáng hơn mép dưới; không vạch, không chữ, không sao hay trang trí bên trong.
- Không chi tiết nào mảnh hơn 3 px.
- Đặt cạnh ảnh tham chiếu thì cùng một bộ: cùng độ bão hoà, cùng kiểu bóng kẹo.
