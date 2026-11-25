---
tool: codex
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-lose/cheat
---
# Art concept: popup thua phong cách Colorful (phá kỉ lục)

## Mô tả
Ảnh concept màn hình điện thoại cầm ngang 1920x1080 lúc popup thua đang mở, lượt vừa phá kỉ lục. Vẽ lại theo style Colorful mới (thay ảnh cũ theo style phẳng, đã CŨ). Dùng để người duyệt hình thức popup thua trước khi làm sprite thật.

Cảnh: game thủ thành 2D nhìn ngang; phía sau popup là trận đứng yên (thành ở giữa, dải đất cỏ xanh hai bên, trời xanh sáng), bị nền che làm tối khoảng 60%.

Bố cục giữ đúng frame `new-record` (ảnh frame đi kèm prompt). Hình thức lấy từ ảnh tham chiếu của style:
- Khung popup: thân kem sáng, viền vàng cam dày có viền ngoài đậm hơn, góc trên uốn vành; nơ hồng có sao vàng ở giữa đáy khung, lá xanh ở hai góc dưới.
- Tiêu đề "Thành đã bị phá" nằm trên một ruy băng tím vắt ngang đỉnh khung, hai đầu quặt xuống, sao vàng hai bên; chữ vàng to, tròn, viền tím đậm, có bóng. Phía trên ruy băng có icon emoji mặt buồn màu vàng, tia sáng phía sau.
- Dòng điểm và dòng kỉ lục: chữ tím đậm trên một mảng hồng nhạt bo tròn.
- Dòng "Kỉ lục mới!": huy hiệu hoặc dải vàng nổi bật nhất trong thân popup, có sao và lấp lánh quanh.
- Nút "Chơi lại": nút chính, xanh lá, dạng viên thuốc, bóng như kẹo, có icon vòng lặp.
- Nút "Về màn bắt đầu": nút phụ, xanh dương, dạng viên thuốc, nhỏ hơn, có icon ngôi nhà.
- Nội dung chỉ theo frame: không có ô thưởng, không có nút hồi sinh hay quảng cáo, không có thanh tài nguyên trên cùng.

## Danh sách
- new-record: popup thua lúc phá kỉ lục theo style Colorful, đúng bố cục frame `new-record` của `feature-popup-lose/cheat`, trận đứng yên tối mờ phía sau

## Tiêu chí nhận
- Đúng 1920x1080, nhìn thẳng, không phối cảnh.
- Khung popup và hai nút nằm đúng vị trí, cỡ của frame (lệch không quá 20 px mỗi cạnh); ruy băng, emoji, nơ, lá được tràn ra ngoài khung tối đa 80 px.
- Chữ đúng nguyên văn, đúng dấu tiếng Việt, đúng vị trí: "Thành đã bị phá", "Trụ được 16 đợt", "Kỉ lục: 16 đợt", "Kỉ lục mới!", "Chơi lại", "Về màn bắt đầu". Không thêm chữ, số hay kí tự nào khác.
- Màu theo bảng màu chung của style: khung vàng, ruy băng tím, nút chính xanh lá, nút phụ xanh dương, thân popup kem; viền mỗi khối là sắc đậm của chính màu đó, không viền đen.
- Mọi khối nổi sáng ở mép trên, tối dần về mép dưới; nút có vệt bóng sáng như kẹo.
- Nút "Chơi lại" to và nổi hơn nút "Về màn bắt đầu"; dòng "Kỉ lục mới!" nổi bật hơn hai dòng điểm và kỉ lục.
- Trận phía sau tối hẳn, popup đọc rõ trên nền.
