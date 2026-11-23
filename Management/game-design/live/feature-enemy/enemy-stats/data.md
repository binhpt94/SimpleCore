---
based-on: b74c0b7d272a
refs: feature-hero/hero-stats@2748621645f8, feature-enemy/enemy-list@10b291cde6dc, feature-enemy/enemy-reward@1fbbced31e0f, feature-castle/castle-skills@8bc45d0edc8d
---
# Chỉ số quái

Quái dùng chung bộ năm chỉ số của game: máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển. Mỗi loại quái có bộ giá trị riêng; quái không có cấp và không mạnh lên trong lúc sống.

## Ý cốt lõi

### enemy-shared-stats | AI 8 | Quái có đủ năm chỉ số chung, cùng định nghĩa với hero
Định nghĩa ở `feature-hero/hero-stats`; một đòn trúng trừ máu đúng bằng sức đánh. Đã loại: quái có giáp hay chỉ số riêng.

### enemy-stats-per-type | AI 8 | Mỗi loại quái có bộ giá trị chỉ số riêng
Các loại khác nhau chủ yếu ở máu, tốc độ di chuyển và tầm đánh (`feature-enemy/enemy-list`).

### enemy-no-level | AI 7 | Quái không có cấp, không mạnh lên trong lúc đang sống
Độ khó theo đợt là việc của `feature-wave`: đợt sau ra nhiều quái hơn, loại trâu hơn, hoặc quái mạnh hơn. Đã loại: quái lên cấp theo thời gian sống.

### enemy-defeated | AI 8 | Quái máu về 0 thì bị hạ và biến mất khỏi chiến trường
Bị hạ là lúc quái cho tiền (`feature-enemy/enemy-reward`). Quái không hồi sinh.
