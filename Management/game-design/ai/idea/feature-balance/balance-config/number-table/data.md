---
based-on: 6934878b1b70
refs: feature-balance/balance-team/hero-compare@d356532cffe9, feature-wave/difficulty-scaling/wave-formula@4ec2cc7f93ed, feature-balance/balance-economy/income-and-prices@295cfe59ac28, feature-balance/balance-difficulty-curve/break-forecast@3d3abad89eb9, feature-castle/castle-skills/skill-values@ad45ef4fdcc9, feature-castle/castle-upgrade-effects/upgrade-effect-values@b16439440823, feature-upgrade/upgrade-effect/effect-numbers@687e3349a36c
frame: none
---
# Bảng số chuẩn và nơi đặt

Bộ số chuẩn của cả game gom thành một bảng đối chiếu: mỗi số ghi giá trị, đơn vị, feature sở hữu và các số có quan hệ. Mỗi số chỉ nằm trong cấu hình của feature sở hữu; bảng này là bản đối chiếu, không phải nơi code đọc số.

## Ý cốt lõi

### table-hero | AI 7 | Chỉ số lv1 của 5 hero nằm ở cấu hình `feature-hero`, ví dụ Hiệp sĩ 300 máu, 12 sát thương mỗi 1.0 s, tầm 1.2u, đi 2.0 u/s
Gồm máu, sát thương, giây mỗi đòn, tầm, tốc độ (mỗi hero một mức, 2.0 tới 3.5 u/s), kiểu đòn; hồi sinh 8 s; mỗi cấp +15% máu và sát thương của lv1, +3% tốc độ đánh tới tối đa +60%.

### table-castle | AI 7 | Chỉ số thành nằm ở cấu hình `feature-castle`: 1000 máu, 10 sát thương mỗi 1.0 s, tầm 6u từ mép thành
Cùng 3 kĩ năng: Sấm sét 80 sát thương hồi 20 s, Băng giá chậm 50% trong 5 s hồi 35 s, Cổ vũ hồi 30% máu hero hồi 25 s; mức tăng mỗi cấp của máu (+150), sức đánh (+20% lv1), kĩ năng (+20% hiệu lực lv1).

### table-enemy | AI 7 | Chỉ số gốc và tiền của 5 loại quái nằm ở cấu hình `feature-enemy`, ví dụ Lính 40 máu, 5 sát thương, 5 xu
Hệ số nhân theo đợt không nằm ở đây mà ở `feature-wave`.

### table-wave | AI 7 | Tham số đợt nằm ở cấu hình `feature-wave`: số quái 6 + 2×đợt, nhịp 1.0 s giảm 0.05 s mỗi đợt tới 0.3 s, hệ số +10% mỗi đợt
Cùng mốc mở loại, chu kì Trùm 10 đợt, bảng phần trăm loại, giới hạn 40 quái sống, lúc thở 3 s, thưởng dọn đợt 20 + 5×đợt xu.

### table-upgrade | AI 7 | Giá nâng cấp nằm ở cấu hình `feature-upgrade`: hero 30×1.35^(cấp−1) xu, máu và sức đánh thành 40×1.4^(cấp−1) xu, kĩ năng 60×1.5^(cấp−1) xu
Làm tròn lên, không cấp tối đa.

### table-wallet | AI 7 | Tiền đầu lượt 50 xu nằm ở cấu hình `feature-wallet`

### table-links | AI 7 | Mỗi số có danh sách số quan hệ phải xem lại khi đổi, ví dụ đổi tiền quái thì xem lại giá nâng cấp và đợt gãy dự kiến
Danh sách quan hệ ghi ở cột "Xem lại khi đổi" của bảng.
