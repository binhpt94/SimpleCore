---
based-on: f1f04ceb7020
refs: feature-balance/balance-config/number-table@3f8b1fde7f49, feature-wave/spawning/spawn-order@d71ff27687b4, feature-balance/balance-economy/income-and-prices@295cfe59ac28, feature-balance/balance-team/hero-compare@d356532cffe9, feature-balance/balance-config@6934878b1b70, feature-enemy/enemy-stats/enemy-stat-values@213bae407954, feature-wave/difficulty-scaling@fed0ac86d57c
---
# Dự đoán đợt gãy

Cách đo áp lực của một đợt bằng tổng máu quái so với sát thương đội làm ra trong lúc quái được thả, và kết quả tính thử theo bộ số chuẩn. Người chơi bình thường dự kiến gãy ở khoảng đợt 15–19; đợt Trùm 10 và 20 là hai chỗ dễ gãy nhất.

## Ý cốt lõi

### pressure-formula | AI 7 | Áp lực P của đợt = tổng máu quái ÷ (sát thương mỗi giây của đội × thời gian thả đợt tính bằng giây)
P dưới 1: đội hạ kịp nhịp thả. P từ 1 tới 2: quái dồn lại, thành bắt đầu mất máu. P từ 2.5 trở lên trong vài đợt liền: thành không trụ được.

### team-dps | AI 7 | Sát thương mỗi giây của đội = tổng của 3 hero (đòn vùng tính trúng 2 con) + đòn tự bắn + Sấm sét chia 20 giây
Sấm sét tính trúng 4 con ở đợt 1–5, 5 con ở đợt 6–15, 6 con từ đợt 16. Hero cấp L nhân (1 + 0.15×(L−1)) × (1 + 0.03×(L−1)).

### early-easy-check | AI 8 | Đợt 1–9 có P dưới 1.1 (đợt 1: 0.6, đợt 5: 0.9, đợt 9: 1.1)
Khớp luật "các đợt đầu trụ được mà chưa cần nâng cấp nhiều".

### mid-pressure | AI 7 | Từ đợt 11 tới đợt 15, P tăng từ khoảng 1.5 lên khoảng 2.5, thành mất máu đều mỗi đợt
Nhịp thả chạm sàn 0.3 s ở đợt 15 làm quái dồn nhanh hơn.

### normal-break | AI 7 | Người chơi bình thường (đội trộn, mua rải đều) gãy ở khoảng đợt 15–19, lượt dài khoảng 7–9 phút
Đợt 20 (Trùm, P khoảng 2.9) gần như chắc gãy với cách chơi này.

### skill-gap | AI 6 | Chơi vụng gãy ở khoảng đợt 10 (Trùm đầu tiên), chơi khéo kéo tới khoảng đợt 20–25
Khác nhau ở: đội có hero hợp loại quái, Sấm sét bắn lúc quái dồn đông, Băng giá dùng khi Trùm tới, không để tiền nằm yên.
