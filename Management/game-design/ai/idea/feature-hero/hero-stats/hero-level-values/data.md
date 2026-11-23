---
based-on: 2748621645f8
refs: feature-enemy/enemy-stats/enemy-stat-values@213bae407954, feature-castle/castle-attack/auto-shot-values@73b7dbaf8425, feature-hero/hero-list/hero-roster-values@8a2346250094, feature-hero/hero-respawn/respawn-values@0ece6d5f17b9
frame: none
---
# Cách tính chỉ số và cấp hero

Cách đo khoảng cách, nhịp đánh, sát thương dùng chung cho hero, quái và thành, và công thức cấp hero. Mỗi cấp hero cộng 15% máu và sát thương lv1, cộng 3% tốc độ đánh (tối đa 60%).

## Ý cốt lõi

### distance-gap | AI 8 | Khoảng cách giữa hai bên là khoảng trống theo trục x giữa hai mép hộp, tính bằng u; chồng lên nhau thì là 0u
Mọi tầm đánh (hero, quái, thành) và vùng kĩ năng dùng cách đo này. Hiệp sĩ tầm 1.2u đánh được quái khi mép hai hộp cách nhau không quá 1.2u. Đã loại: đo từ tâm (unit to như Trùm 3u sẽ đánh hụt).

### attack-interval | AI 8 | "Giây mỗi đòn" là thời gian chờ giữa hai đòn; đòn đầu ra ngay khi có mục tiêu trong tầm nếu đã chờ đủ
Bộ đếm chạy cả lúc đi. Ví dụ Cung thủ 0.8s: đòn 1 ở giây 0, đòn 2 ở giây 0.8. Áp cho hero, quái và thành.

### damage-instant | AI 8 | Sát thương trừ ngay lúc ra đòn, đúng bằng sức đánh; hình đạn bay 0.15s chỉ để nhìn
Không trượt, không giáp. Đòn đánh xa không bay thật, nên không có đạn trúng quái đã chết.

### level-formula | AI 8 | Cấp n: máu và sát thương = lv1 × (1 + 0.15×(n-1)); tốc độ đánh = lv1 × (1 + min(0.03×(n-1), 0.6))
Giây mỗi đòn ở cấp n = giây mỗi đòn lv1 / (1 + min(0.03×(n-1), 0.6)). Hiệp sĩ cấp 5: 480 máu, 19.2 sát thương, 0.893s mỗi đòn. Tốc độ đánh chạm trần ở cấp 21. Tầm và tốc độ chạy không đổi.

### level-hp-gain | AI 8 | Lên cấp khi đang đứng: máu hiện tại cộng đúng phần máu tối đa vừa tăng
Hiệp sĩ 100/300 lên cấp 2 thành 145/345. Lên cấp khi đang gục: hồi sinh với đầy máu của cấp mới.

### decimal-stats | AI 7 | Máu và sát thương giữ số thực; máu hiện ra làm tròn lên, sát thương hiện ra làm tròn xuống
Còn 0.4 máu thì hiện 1 và chưa gục; máu ≤ 0 mới là gục hay bị hạ.
