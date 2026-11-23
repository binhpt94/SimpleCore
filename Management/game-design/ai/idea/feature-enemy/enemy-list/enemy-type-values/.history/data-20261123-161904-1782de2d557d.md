---
based-on: 10b291cde6dc
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-enemy/enemy-behavior@cf9290ce16e6, feature-enemy/enemy-stats/enemy-stat-values@213bae407954, feature-enemy/enemy-reward/reward-values@30a4a6c88e50, feature-enemy/enemy-behavior/enemy-move-values@f5adc15dbc17
---
# Năm loại quái: hộp, kiểu đánh, đợt xuất hiện

Hộp 2D, kiểu đánh và đợt đầu tiên của Lính, Sói, Khổng lồ, Cung thủ quái, Trùm. Lính có từ đợt 1, Sói từ đợt 3, Khổng lồ từ đợt 5, Cung thủ quái từ đợt 7, Trùm một con ở mỗi đợt chia hết cho 10.

## Ý cốt lõi

### type-boxes | AI 8 | Hộp quái (rộng×cao, u): Lính 1×1.2, Sói 1.2×0.8, Khổng lồ 2×2.5, Cung thủ quái 1×1.2, Trùm 3×3.5
Kích thước nói lên vai: Sói thấp dài, Khổng lồ và Trùm to hơn hẳn hero (1×1.5u). Mọi quái chân trên mặt đất y=0.

### type-first-wave | AI 7 | Đợt đầu có mặt: Lính đợt 1, Sói đợt 3, Khổng lồ đợt 5, Cung thủ quái đợt 7, Trùm đợt 10, 20, 30...
Mỗi 2 đợt có một loại mới tới đợt 7, người chơi thấy trận thay đổi. Tỉ lệ từng loại trong đợt là việc của `feature-wave`.

### type-melee-ranged | AI 8 | Bốn loại cận chiến tầm 1.0-1.5u; Cung thủ quái bắn xa tầm 5u, hình đạn bay 0.15s
Cung thủ quái là quái duy nhất đánh được hero và thành từ ngoài tầm hero cận chiến. Sát thương tức thì như mọi đòn xa (`feature-hero/hero-stats/hero-level-values`).

### boss-single | AI 7 | Trùm: mỗi đợt có Trùm thì có đúng 1 con, ra cuối đợt, không có đòn riêng
Ra cuối để là đỉnh căng thẳng của đợt. Luật đi và đánh như mọi quái (`feature-enemy/enemy-behavior`).
