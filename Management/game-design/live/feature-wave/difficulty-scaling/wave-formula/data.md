---
based-on: fed0ac86d57c
refs: feature-wave/spawning/spawn-order@5884f63cfa93, feature-enemy/enemy-list@10b291cde6dc, feature-enemy/enemy-stats/enemy-stat-values@7016e0fe183b, feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-enemy/enemy-list/enemy-type-values@1782de2d557d
frame: none
---
# Công thức độ khó theo đợt

Mọi giá trị của một đợt tính từ số đợt w (đợt đầu w = 1) bằng các công thức dưới đây; không có bảng viết tay từng đợt. Số quái, nhịp thả, hệ số máu và sức đánh tăng đều; loại quái mở dần theo mốc đợt và phần loại khó tăng theo bậc.

## Ý cốt lõi

### count-formula | AI 8 | Số quái của đợt w là 6 + 2×w con (đợt 1: 8 con, đợt 10: 26 con, đợt 20: 46 con)
Luôn là số chẵn, chia đúng một nửa cho mỗi bên. Trùm, nếu có, tính trong số này.

### interval-formula | AI 8 | Nhịp thả của đợt w là 1.0 − 0.05×(w − 1) giây, thấp nhất 0.3 giây (chạm sàn từ đợt 15)
Đợt 1: 1.0 s, đợt 5: 0.8 s, đợt 10: 0.55 s, đợt 15 trở đi: 0.3 s.

### stat-multiplier | AI 7 | Máu và sức đánh của quái đợt w nhân hệ số 1 + 0.10×(w − 1) (đợt 10: ×1.9, đợt 20: ×2.9)
Áp cho chỉ số gốc của `feature-enemy`. Tốc độ đánh, tầm đánh, tốc độ di chuyển và tiền khi hạ không nhân.

### type-unlock | AI 7 | Lính có từ đợt 1, Sói từ đợt 3, Khổng lồ từ đợt 5, Cung thủ quái từ đợt 7
Loại đã mở giữ mở tới hết lượt. Đợt mở loại mới luôn có ít nhất 1 con loại đó.

### boss-wave | AI 6 | Đợt có số chia hết cho 10 (10, 20, 30…) có đúng 1 Trùm, chiếm 1 chỗ trong số quái
Các đợt khác không có Trùm. Trùm cũng nhân hệ số máu và sức đánh của đợt.

### type-mix | AI 7 | Phần trăm từng loại theo bậc đợt; đợt 15 trở đi cố định Lính 35%, Sói 25%, Khổng lồ 20%, Cung thủ quái 20%
Bậc: đợt 1–2 Lính 100%; đợt 3–4 Lính 70%, Sói 30%; đợt 5–6 Lính 55%, Sói 30%, Khổng lồ 15%; đợt 7–9 Lính 45%, Sói 25%, Khổng lồ 15%, Cung thủ quái 15%; đợt 10–14 Lính 40%, Sói 25%, Khổng lồ 20%, Cung thủ quái 15%. Phần trăm tính trên số chỗ còn lại sau Trùm; mỗi loại không phải Lính lấy phần nguyên của (phần trăm × số chỗ), tối thiểu 1; Lính lấy phần còn lại.

### reward-by-mix | AI 6 | Tiền mỗi con giữ theo loại; tiền trung bình mỗi con tăng theo đợt nhờ phần loại trâu tăng (5 xu ở đợt 1, khoảng 9 xu ở đợt 20)
Không có hệ số tiền theo đợt. Thu của đợt tăng theo số quái và phần Khổng lồ tăng; ngoại lệ có chủ đích: đợt ngay sau đợt Trùm thu ít hơn đợt Trùm (đợt 10: 272 xu, đợt 11: 187 xu) vì 100 xu của Trùm là phần thưởng riêng.
