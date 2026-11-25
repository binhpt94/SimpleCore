---
based-on: cf9290ce16e6
refs: feature-hero/hero-list/hero-roster-values@8a2346250094, feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-enemy/enemy-stats/enemy-stat-values@7016e0fe183b, feature-castle/castle-skills/skill-values@ad45ef4fdcc9, feature-castle/castle-core/castle-body-values@b648ec300023, feature-castle/castle-attack/auto-shot-values@73b7dbaf8425, feature-wave/spawning/spawn-order@5884f63cfa93, feature-enemy/enemy-list/enemy-type-values@96f77fbbafff
---
# Thông số đi và đánh của quái

Quái sinh ở x=-17u hoặc x=+17u, đi thẳng về thành theo tốc độ của loại. Có hero trong tầm thì đứng đánh hero gần nhất; khoảng cách tới mép thành không quá tầm thì đứng đánh thành. Bị Băng giá thì tốc độ đi ×0.5 và giây mỗi đòn ×2.

## Ý cốt lõi

### spawn-walk | AI 8 | Quái bên trái sinh ở x=-17u đi sang phải, bên phải sinh ở x=+17u đi sang trái, theo tốc độ của loại (0.6-3.0u/s)
Bên do `feature-wave` chọn. Lính 1.5u/s đi từ x=17u tới lúc chạm tầm thành (mép cách mép thành 1.0u) mất khoảng (17-0.5-1.5-1.0)/1.5 ≈ 9.3s.

### hero-first | AI 8 | Có hero đang đứng trong tầm thì quái dừng lại đánh hero gần nhất, kể cả khi thành cũng trong tầm
Giữ hero đó tới khi hero gục, hết trong tầm, hoặc rời trận; rồi xét lại. Hero trong 0.3s hồi sinh không tính.

### castle-stop | AI 8 | Khoảng cách tới mép thành ≤ tầm của quái thì quái đứng yên đánh thành, không đi thêm
Lính (tầm 1.0u) dừng với mép cách mép thành 1.0u; Cung thủ quái (tầm 5u) dừng cách 5u, tức tâm ở x=±7.0u, vẫn trong tầm tự bắn 6u của thành.

### slow-values | AI 7 | Bị Băng giá: tốc độ đi ×0.5 và giây mỗi đòn ×2 trong suốt thời gian chậm, hết chậm thì trở lại ngay
Lần đánh đang chờ lúc trúng chậm: thời gian còn lại nhân 2; lúc hết chậm thì thời gian còn lại chia 2.

### enemy-feedback | AI 7 | Quái đi thì nhún 0.1u mỗi 0.4s, ra đòn thì nhích 0.2u về phía mục tiêu trong 0.15s, bị đánh thì nháy 0.1s
Cùng bộ chuyển động với hero (`feature-hero/hero-list/hero-roster-values`) để mọi unit đọc giống nhau.
