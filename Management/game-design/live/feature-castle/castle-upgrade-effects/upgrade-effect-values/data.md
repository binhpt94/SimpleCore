---
based-on: 2c1e443d0ced
refs: feature-castle/castle-core/castle-body-values@13107fd51941, feature-castle/castle-attack/auto-shot-values@d966de66a0b5, feature-castle/castle-skills/skill-values@6240af7903a9
---
# Mức tăng mỗi cấp nâng thành

Mỗi cấp của ba mục nâng thành cộng một lượng cố định, tính theo giá trị lv1, cộng dồn tuyến tính. Mua xong có hiệu lực trong cùng khung hình. Giá mỗi cấp là của `feature-upgrade`.

## Ý cốt lõi

### hp-per-level | AI 8 | Mỗi cấp máu cộng 150 vào máu tối đa và 150 vào máu hiện tại
Cấp n có máu tối đa = 1000 + 150×(n-1). Không hồi phần đã mất: thành đang 400/1000 mua 1 cấp thành 550/1150.

### attack-per-level | AI 8 | Mỗi cấp sức đánh cộng 2 sát thương vào mỗi phát tự bắn (20% của 10)
Cấp n có sát thương = 10 × (1 + 0.2×(n-1)). Tốc độ bắn 1.0s và tầm 6u không đổi.

### skill-per-level | AI 7 | Mỗi cấp kĩ năng cộng 20% hiệu lực lv1 cho cả ba: Sấm sét +16 máu, Băng giá +1s chậm, Cổ vũ +6% máu tối đa
Cấp n: Sấm sét 80×(1+0.2×(n-1)) máu; Băng giá chậm 5×(1+0.2×(n-1)) giây, mức chậm vẫn 50%; Cổ vũ hồi 30%×(1+0.2×(n-1)) máu tối đa. Hồi chiêu không đổi.

### no-cap | AI 7 | Ba mục không có cấp tối đa, cấp bắt đầu là 1 mỗi lượt
Giá tăng theo cấp (`feature-upgrade`) tự giới hạn số lần mua. Cấp về 1 khi lượt mới bắt đầu (`feature-run`).
