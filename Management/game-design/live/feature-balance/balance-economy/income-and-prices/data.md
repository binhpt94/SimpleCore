---
based-on: 5fe05cebab72
---
# Thu theo đợt và giá nâng cấp

Cách tính tiền thu được mỗi đợt và tổng tiền tới một đợt, bảng giá từng cấp, và lời của mỗi lần nâng. Theo bộ số chuẩn, người chơi có khoảng 393 xu ở đầu đợt 5, 1282 xu ở đầu đợt 10 và 4685 xu ở đầu đợt 20.

## Ý cốt lõi

### income-formula | AI 7 | Thu của đợt w = tổng tiền gốc các quái trong đợt + (20 + 5×w) xu thưởng dọn đợt
Tiền quái không nhân theo đợt; thu tăng nhờ số quái và phần loại trâu tăng. Đợt 1: 40 + 25 = 65 xu; đợt 10: 272 + 70 = 342 xu; đợt 20: 413 + 120 = 533 xu.

### income-total | AI 7 | Tiền có ở đầu đợt w = 50 xu đầu lượt + tổng thu các đợt trước (đầu đợt 5: 393 xu, đầu đợt 20: 4685 xu)
Tính khi người chơi hạ hết quái mỗi đợt; dùng để dự đoán sức đội mua được.

### kill-over-bonus | AI 8 | Ở mọi đợt, tiền hạ quái lớn hơn thưởng dọn đợt (đợt 1: 40 so với 25 xu, đợt 20: 413 so với 120 xu)
Giữ tiền hạ quái là nguồn chính.

### price-ladder | AI 7 | Giá cấp tiếp theo tăng theo hệ số: hero ×1.35, máu và sức đánh thành ×1.4, kĩ năng ×1.5 mỗi cấp, làm tròn lên
Hero: 30, 41, 55, 74, 100, 135, 182, 246, 331 xu; máu hoặc sức đánh thành: 40, 56, 79, 110, 154, 216, 302 xu; kĩ năng: 60, 90, 135, 203, 304, 456 xu.

### buy-each-wave | AI 7 | Đợt nào cũng thu đủ ít nhất 1 lần nâng rẻ nhất đang có (đợt 1 thu 65 xu so với giá rẻ nhất 30 xu)
Kiểm ở đợt 20: thu 533 xu, các mục theo cách mua đều có giá từ 135 tới 331 xu.

### never-max | AI 8 | Tổng tiền tới đợt 20 (khoảng 5200 xu) chỉ đủ đưa 3 hero lên khoảng cấp 9 và thành lên khoảng cấp 5–7
Đưa mọi mục (3 hero, 2 mục thành, 3 kĩ năng) lên cấp 10 cần khoảng 21 000 xu, gấp 4 lần.

### value-drop | AI 6 | Lời mỗi xu giảm theo cấp: nâng hero cấp 1→2 cho khoảng 0.1 sát thương mỗi giây mỗi xu, cấp 4→5 còn khoảng 0.05
Nâng sức đánh thành cấp 1→2 cho 0.05, Sấm sét cấp 1→2 khoảng 0.06; nên mua rải đều là cách chơi tốt.
