---
based-on: 10b291cde6dc
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-enemy/enemy-behavior@cf9290ce16e6, feature-enemy/enemy-stats/enemy-stat-values@7016e0fe183b, feature-enemy/enemy-reward/reward-values@30a4a6c88e50, feature-enemy/enemy-behavior/enemy-move-values@820f944e332f, feature-hero/hero-list/hero-roster-values@8a2346250094, feature-wave/difficulty-scaling/wave-formula@4ec2cc7f93ed, feature-wave/spawning/spawn-order@5884f63cfa93
---
# Năm loại quái: hộp và kiểu đánh

Hộp 2D, kiểu đánh và hình của Lính, Sói, Khổng lồ, Cung thủ quái, Trùm. Loại nào ra ở đợt nào, đợt nào có Trùm và Trùm ra lúc nào là luật của nhánh đợt quái.

## Ý cốt lõi

### type-boxes | AI 8 | Hộp quái (rộng×cao, u): Lính 1×1.2, Sói 1.2×0.8, Khổng lồ 2×2.5, Cung thủ quái 1×1.2, Trùm 3×3.5
Kích thước nói lên vai: Sói thấp dài, Khổng lồ và Trùm to hơn hẳn hero (1×1.5u). Mọi quái chân trên mặt đất y=0.

### type-melee-ranged | AI 8 | Bốn loại cận chiến tầm 1.0-1.5u; Cung thủ quái bắn xa tầm 5u, hình đạn bay 0.15s
Cung thủ quái là quái duy nhất đánh được hero và thành từ ngoài tầm hero cận chiến. Sát thương tức thì như mọi đòn xa (`feature-hero/hero-stats/hero-level-values`).

### boss-no-skill | AI 7 | Trùm không có đòn riêng; đi và đánh theo cùng luật như mọi quái
Chỉ khác ở hộp, chỉ số và tiền (`feature-enemy/enemy-behavior`, `feature-enemy/enemy-stats/enemy-stat-values`).

### waves-elsewhere | AI 8 | Đợt đầu có mặt của từng loại, đợt có Trùm và việc Trùm ra cuối đợt do `feature-wave` quyết, quái không giữ
Mốc mở loại và chu kì Trùm ở `feature-wave/difficulty-scaling/wave-formula`; Trùm là con cuối của đợt ở `feature-wave/spawning/spawn-order`. Mỗi lần sinh, quái chỉ nhận loại, bên và hệ số.
