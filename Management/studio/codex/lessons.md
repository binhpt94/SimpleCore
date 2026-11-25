# Bài học của @Codex
studio.py chèn toàn bộ file này vào đầu mọi prompt. Mỗi mục một dòng: luật làm được, kèm nguồn (đường dẫn feedback hoặc log). Xem .claude/rules/studio.md.

- Popup/overlay: khung popup sáng hơn hẳn nền phía sau; nền phía sau vẽ cảnh sáng bình thường rồi phủ một lớp tối khoảng 60% thấy rõ, không chỉ dựa vào viền để tách (codex/orders/_design/feature-popup-lose.cheat/lose-popup-concept/feedback-1.md)
- Nhân vật nền: vẽ đúng kiểu lính, quái chiến đấu của game (giáp, vũ khí), không giống thú nuôi (codex/orders/_design/feature-popup-lose.cheat/lose-popup-concept/feedback-1.md)
- Trần màu: khi Tiêu chí giới hạn số màu, liệt kê sẵn bảng màu trong prompt và dùng chung màu cho nền, nhân vật, nút; cảnh nền phủ tối dùng 1–2 tông (codex/orders/_design/feature-popup-lose.cheat/lose-popup-concept/work/new-record.check.md: lượt 2 ra ~9 màu)
- Vũ khí "dài" hay "lớn": ghi kích thước bằng px so với chiều cao nhân vật và hướng đặt (chéo lên để vừa khung), nếu không model vẽ ngắn như dao găm (codex/orders/Hero/hero-sprites/work/hero-swordsman.check.md: lượt 1)
- Nền phủ tối sau popup: chữ "phủ tối 60%" trong prompt chưa đủ, model chỉ làm tối trời, giữ lính, quái, cỏ sáng gần như gốc; ngay lượt đầu ghi rõ từng thứ ngoài popup (trời, mây, thành, lính, quái, cỏ) chỉ còn khoảng 40% độ sáng gốc, không điểm sáng hay màu rực nào ngoài popup (codex/orders/_design/feature-popup-lose.cheat/lose-popup-colorful/work/new-record.check.md: lượt 1, 2)
- Popup trong ảnh concept: ghi toạ độ px của khung và từng phần tử theo frame, ghi "đối xứng qua trục x = <tâm>", trang trí (emoji, nơ, lá, sao) đặt ở tâm hoặc thành cặp soi gương; chữ dài trong nút nhỏ thì cho nhỏ lại hoặc xuống hai dòng, cấm nén hẹp (codex/orders/_design/feature-popup-lose.cheat/lose-popup-colorful: phản hồi v1 "popup bị méo, không cân đối")
- Chân dung hiệp sĩ hay nhân vật đội mũ giáp: ghi rõ "mũ hở mặt hoặc kính che lật lên, thấy rõ mắt và mặt", nếu không model vẽ mũ đóng kín che mặt; và nhắc ảnh cuối phải đúng kích thước order (lượt có lúc ra ảnh lớn gấp 5 lần) (codex/orders/TeamSelect/ts-card-colorful/work/portrait-knight.check.md)
- Khung ngang thấp (con vật dài, ảnh rộng hơn cao): ghi toạ độ y của đỉnh đầu/tai và chân, và tả dáng ngẩng cao (đầu ngẩng, tai, đuôi vểnh) để hình lấp đủ chiều cao, nếu không sẽ thừa khoảng trống phía trên (codex/orders/Enemy/enemy-sprites/work/enemy-wolf.check.md: lượt 1)

