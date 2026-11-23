---
based-on: b74c0b7d272a
refs: feature-enemy/enemy-list@10b291cde6dc, glossary#killer@7156ca2d53fe
---
# Tiền khi hạ quái

Mỗi quái bị hạ cho người chơi tiền, vào thẳng ví mà không cần nhặt, bất kể ai hạ. Loại quái trâu hơn cho nhiều tiền hơn. Thưởng dọn sạch đợt là việc của `feature-wave`.

## Ý cốt lõi

### reward-on-defeat | AI 8 | Mỗi quái bị hạ cho tiền đúng một lần
Quái không bị hạ thì không cho tiền. Hạ quái không cho điểm; điểm là số đợt đã dọn (`feature-run`).

### reward-any-killer | AI 8 | Quái cho tiền như nhau dù bị hero, đòn tự bắn của thành hay kĩ năng thành hạ
Người chơi không phải lo ai hạ. Đã loại: thành hạ thì ít tiền hơn.

### reward-per-type | AI 7 | Loại quái khó hạ hơn cho nhiều tiền hơn
Trùm nhiều nhất, Khổng lồ nhiều hơn Lính (`feature-enemy/enemy-list`). Đã loại: mọi quái cùng một giá.

### reward-instant | AI 8 | Tiền vào ví ngay lúc quái bị hạ, không cần nhặt
Gửi cho `feature-wallet`. Người chơi đã bận bấm kĩ năng và mua nâng cấp. Đã loại (vòng 2): chạm để nhặt tiền.
