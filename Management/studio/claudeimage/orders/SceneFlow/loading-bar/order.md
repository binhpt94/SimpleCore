---
tool: claudeimage
kind: image
count: 2
deliver: Private
format: png 1000x48, nền trong suốt
references: Resources/_design/feature-scene-flow.scene-list.boot-loading/claudeimage/loading-claude/v1/parts/main/bar.png
design: feature-scene-flow/scene-list/boot-loading
---
# Thanh tiến độ màn Loading

## Mô tả
Thanh tiến độ của màn Loading (SceneFlow), giai đoạn IDEA. Trong game thanh rộng 1000x48 px trên màn 1920x1080, nằm dưới tên game.
Phần claude-concept `bar` (ảnh tham chiếu) vẽ sẵn cả nền lẫn phần đã đầy. Unity cần hai lớp riêng: lớp nền nằm dưới, lớp đầy nằm trên và được cắt theo tiến độ (Image Filled, cắt ngang từ trái). Vì vậy một phần `bar` tách thành hai ảnh, cùng khung 1000x48.
Phong cách giữ đúng ảnh tham chiếu: hình phẳng, viền đen dày, góc bo tròn.

## Danh sách
- bar-track: nền của thanh, giống ảnh tham chiếu nhưng không có phần vàng: viền đen dày bo tròn bọc lòng xám xanh đậm #3D4160 kín cả chiều dài
- bar-fill: phần đã đầy của thanh khi đầy 100%: lòng vàng #FFB800 bo tròn, kín chiều dài, nằm gọn bên trong viền của bar-track (chừa đúng độ dày viền ở bốn phía), không có viền đen

## Tiêu chí nhận
- Mỗi ảnh đúng 1000x48 px, nền ngoài hình trong suốt (4 góc trong suốt).
- bar-track: viền đen dày 4 đến 6 px, bo góc bán kính khoảng 20 px; lòng một màu, không có vạch hay chữ.
- bar-fill: chồng lên bar-track thì phần vàng nằm vừa trong viền, không đè lên viền đen; một màu, không vạch, không chữ.
- Bốn góc bo nằm gọn trong 20 px mỗi phía để cắt 9-slice viền [20, 20, 20, 20].
