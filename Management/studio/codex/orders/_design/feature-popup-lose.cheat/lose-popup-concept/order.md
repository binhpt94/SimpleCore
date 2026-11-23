---
tool: codex
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-lose/cheat
---
# Art concept: popup thua (phá kỉ lục)

## Mô tả
Ảnh concept màn hình điện thoại cầm ngang 1920x1080 lúc popup thua đang mở, lượt vừa phá kỉ lục. Dùng để duyệt hình dáng popup thua theo style `ui-flat` trước khi làm sprite thật. Game thủ thành 2D nhìn ngang: phía sau popup là trận đứng yên (thành ở giữa, dải đất hai bên), bị nền che làm tối khoảng 60%.

Bố cục lấy đúng frame `new-record` của node cheat (toạ độ px trên 1920x1080, góc trên trái):
- Nền che phủ toàn màn: x 0, y 0, 1920x1080.
- Khung popup bo góc: x 560, y 190, 800x700.
- Vùng tiêu đề ("Thành đã bị phá"): x 610, y 230, 700x90.
- Vùng dòng điểm ("Trụ được 16 đợt"): x 610, y 350, 700x70.
- Vùng dòng kỉ lục ("Kỉ lục: 16 đợt"): x 610, y 440, 700x70.
- Vùng dòng "Kỉ lục mới!" (điểm nhấn vui nhất của popup): x 760, y 530, 400x70.
- Nút chính "Chơi lại": x 610, y 730, 400x120.
- Nút phụ "Về màn bắt đầu": x 1050, y 730, 260x120.

Style `ui-flat` cấm vẽ chữ vào ảnh: các vùng chữ chỉ là chỗ trống đúng vị trí và cỡ trên (chữ do game hiển thị). Dòng "Kỉ lục mới!" có thể có một dải hoặc huy hiệu nền nổi bật làm chỗ cho chữ.

## Danh sách
- new-record: popup thua lúc phá kỉ lục, đúng bố cục frame `new-record` của `feature-popup-lose/cheat`, trận đứng yên mờ phía sau

## Tiêu chí nhận
- Đúng 1920x1080, nhìn thẳng, phẳng, không phối cảnh.
- Khung popup và hai nút nằm đúng vị trí, cỡ trên (lệch không quá 20 px mỗi cạnh).
- Không có chữ, số hay kí tự nào trong ảnh.
- Tối đa 6 màu chính cộng trắng và đen; không gradient, không texture, không glow, không bóng đổ mềm.
- Viền khung popup và nút dày tối thiểu 3 px; mọi nút cùng bán kính bo.
- Bóng đổ cứng dưới popup và nút, lệch xuống 4–8 px.
- Nút "Chơi lại" và "Về màn bắt đầu" khác nhau rõ về độ sáng, nút "Chơi lại" nổi hơn.
- Vùng "Kỉ lục mới!" nổi bật hơn các dòng điểm, kỉ lục.
- Trận phía sau tối hẳn, popup đọc rõ trên nền.
