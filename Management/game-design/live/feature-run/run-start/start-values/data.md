---
based-on: d386ccae4d88
refs: feature-run/run-score@3d29bb683b7c, feature-castle/castle-skills@8bc45d0edc8d, feature-team-select/team-memory/memory-rules@e36a9ec522a1, feature-wallet/income/income-amounts@f36980378e51
frame: none
---
# Giá trị đầu lượt

Bộ giá trị mọi feature phải có ngay khi trận bắt đầu, và thứ tự việc cần xong trước khi đợt 1 ra quái.

## Ý cốt lõi

### start-castle-1000 | AI 8 | Đầu lượt thành có 1000 máu trên 1000 máu tối đa, cấp mọi nâng cấp của thành là 1
Máu tối đa về 1000 dù lượt trước đã nâng.

### start-money-50 | AI 8 | Đầu lượt tiền là 50, không cộng dồn từ lượt trước

### start-levels-1 | AI 8 | Đầu lượt mọi hero trong đội ở cấp 1

### start-wave-1 | AI 8 | Đầu lượt đợt hiện tại là 1, điểm là 0

### start-skills-ready | AI 8 | Đầu lượt cả ba kĩ năng thành Sấm sét, Băng giá, Cổ vũ đều sẵn sàng, không có hồi chiêu

### start-team-3 | AI 8 | Đầu lượt cả 3 hero của đội đã chọn sinh ra ở thành cùng lúc, đủ máu
Đội 3 hero không trùng; trong trận không đổi đội.

### start-revive-1 | AI 7 | Đầu lượt còn đúng 1 lần hồi sinh thành, kể cả khi lượt trước đã dùng

### start-before-wave | AI 7 | Mọi giá trị trên đã đặt xong và HUD đã hiện đúng trước khi quái đầu tiên của đợt 1 ra
