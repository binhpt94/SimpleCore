---
based-on: c53d76edf8b3
refs: feature-upgrade/upgrade-list/item-set@fd6c29c2b1b5, feature-wallet/balance-spend/wallet-rules@1367fdb78322, feature-upgrade/upgrade-effect/effect-numbers@687e3349a36c, feature-hud-upgrade/button-state/button-display@79f973592f0d
frame: none
---
# Giá và luật mua

Bảng giá theo cấp của từng mục, điều kiện mua được, và cách xử lí bấm nhanh, thua giữa lúc mua. Giá là số nguyên, làm tròn lên.

## Ý cốt lõi

### hero-price | AI 8 | Giá lên cấp hero = 30 × 1,35^(cấp hiện tại − 1) tiền, làm tròn lên
Cấp 1→2 giá 30, 2→3 giá 41, 3→4 giá 55, 5→6 giá 100. Mỗi hero tính giá riêng theo cấp của chính nó.

### castle-hp-attack-price | AI 8 | Giá máu thành và sức đánh thành = 40 × 1,4^(cấp hiện tại − 1) tiền, làm tròn lên
Cấp 1→2 giá 40, 2→3 giá 56, 3→4 giá 79, 5→6 giá 154. Hai mục tính giá riêng.

### castle-skill-price | AI 8 | Giá kĩ năng thành = 60 × 1,5^(cấp hiện tại − 1) tiền, làm tròn lên
Cấp 1→2 giá 60, 2→3 giá 90, 3→4 giá 135, 5→6 giá 304.

### buy-when | AI 8 | Mua được khi trận đang chạy, kể cả lúc nghỉ giữa đợt; không mua được khi tạm dừng hoặc lượt đã kết thúc
Lượt kết thúc là từ khung hình thành về 0 máu.

### buy-sequence | AI 8 | Mỗi chạm là một yêu cầu mua đúng một cấp, xử lí lần lượt; yêu cầu sau dùng số dư và giá mới
Bấm hai lần rất nhanh khi chỉ đủ tiền một lần: lần đầu mua được, lần sau bị từ chối.

### no-cap-no-refund | AI 7 | Mục nào cũng mua tiếp được không giới hạn cấp, và cấp đã mua hoàn lại 0 tiền
Không bán lại. Mua thành công thì cấp và giá mới được báo ngay trong khung hình đó.
