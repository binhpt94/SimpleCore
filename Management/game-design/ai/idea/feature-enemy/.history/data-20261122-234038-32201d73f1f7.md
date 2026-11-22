---
based-on: cf568e2870fe
---
# Quái

Kẻ địch đi từ mép trái và mép phải màn hình vào phía thành. Quái đánh hero chắn đường và đánh thành nếu tới được; bị hạ thì cho người chơi tiền.

## Ý cốt lõi

### enemy-stats | AI 7
Mỗi quái có máu, sức đánh, tốc độ đánh, tốc độ đi và số tiền rơi khi bị hạ.

### enemy-path | AI 8
Quái đi thẳng về phía thành trên dải mặt đất. Gặp hero trong tầm thì dừng lại đánh hero; hero gục thì đi tiếp. Tới thành thì đứng đánh thành.

### enemy-reward | AI 8
Quái bị hạ (bởi hero, đòn bắn hay kĩ năng của thành) thì rơi tiền vào `feature-wallet` và báo `feature-wave` là đợt bớt một quái.

### enemy-types | AI 6
Có vài loại quái để đợt sau khác đợt trước; bản đầu dự kiến 3 loại: thường, nhanh nhưng yếu, chậm nhưng trâu. Chưa có quái bắn xa và quái trùm.
