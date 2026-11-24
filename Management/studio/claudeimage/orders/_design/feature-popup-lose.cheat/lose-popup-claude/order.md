---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat, world-cartoon
design: feature-popup-lose/cheat
---
# Claude concept: popup thua (phá kỉ lục)

## Mô tả
Hình tham chiếu mức IDEA cho phiên PopupLose: màn điện thoại cầm ngang 1920x1080 lúc popup thua đang mở và lượt vừa phá kỉ lục. Popup vẽ theo style `ui-flat`. Trận đứng yên phía sau vẽ đơn giản theo `world-cartoon`: thành ở giữa, dải đất hai bên, vài quái và hero. Nền che làm tối trận khoảng 60%.

Bố cục lấy đúng frame `new-record` của node cheat. Đọc bằng `python Management/tools/gd_frame.py live/feature-popup-lose/cheat`. Toạ độ tính bằng px trên màn 1920x1080, gốc ở góc trên trái:
- Nền che phủ toàn màn: x 0, y 0, 1920x1080. Đây là một lớp phẳng tối riêng nằm trên toàn bộ cảnh trận.
- Khung popup bo góc: x 560, y 190, 800x700.
- Vùng tiêu đề ("Thành đã bị phá"): x 610, y 230, 700x90.
- Vùng dòng điểm ("Trụ được 16 đợt"): x 610, y 350, 700x70.
- Vùng dòng kỉ lục ("Kỉ lục: 16 đợt"): x 610, y 440, 700x70.
- Vùng dòng "Kỉ lục mới!": x 760, y 530, 400x70. Đây là điểm nhấn của popup.
- Nút chính "Chơi lại": x 610, y 730, 400x120.
- Nút phụ "Về màn bắt đầu": x 1050, y 730, 260x120.

Không vẽ chữ hay số. Mỗi vùng chữ là một ô trống đúng vị trí và cỡ ở trên, vì chữ do game hiển thị. Không chép chữ chú thích hay số đo của frame vào ảnh. Dòng "Kỉ lục mới!" có một dải hoặc huy hiệu nền nổi bật để làm chỗ cho chữ.

## Danh sách
- new-record: popup thua lúc phá kỉ lục, đúng bố cục frame `new-record` của `feature-popup-lose/cheat`, trận đứng yên phía sau bị nền che làm tối

## Tiêu chí nhận
- Ảnh đúng 1920x1080, nhìn thẳng, phẳng, không phối cảnh.
- Khung popup và hai nút đúng vị trí và cỡ ở trên, lệch không quá 20 px mỗi cạnh.
- Không có chữ, số hay kí tự nào trong ảnh.
- Phần popup có tối đa 6 màu chính, cộng thêm trắng và đen. Không gradient, không glow, không bóng đổ mềm.
- Viền khung popup và viền nút dày tối thiểu 3 px. Mọi nút cùng bán kính bo.
- Bóng đổ cứng dưới popup và dưới nút, lệch xuống 4 đến 8 px.
- Nút "Chơi lại" sáng hơn và nổi hơn hẳn nút "Về màn bắt đầu".
- Vùng "Kỉ lục mới!" nổi bật hơn hai dòng điểm và kỉ lục.
- Nền che là một hình chữ nhật tối phủ toàn màn, dễ thấy là một lớp riêng. Trận phía sau tối hẳn, popup đọc rõ trên nền.
