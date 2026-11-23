---
based-on: 24fd307bbdcd
refs: feature-castle/castle-skills@8bc45d0edc8d, feature-hero/hero-stats@2748621645f8, feature-castle/castle-core@e1440a2a0de0, feature-enemy/enemy-reward@1fbbced31e0f, feature-castle/castle-upgrade-effects@2c1e443d0ced, feature-hero/hero-targeting@49b5ce7a72b8, glossary#range@f6d3cd6778ab, glossary#target@f51ab44b30ff---
# Thành tự bắn

Thành tự bắn quái gần nhất trong tầm ở cả hai bên, không cần người chơi bấm. Đòn tự bắn dùng chung bộ chỉ số chiến đấu của game, trừ tốc độ di chuyển vì thành đứng yên.

## Ý cốt lõi

### castle-shoot-nearest | AI 8 | Thành tự bắn quái gần thành nhất trong tầm, xét cả hai bên
Quái gần thành nhất là quái nguy nhất. Đã loại: người chơi chạm vào quái để thành bắn; thành bắn luân phiên hai bên.

### castle-single-target | AI 7 | Mỗi phát bắn của thành trúng một quái
Đòn đánh nhiều quái là việc của kĩ năng thành (`feature-castle/castle-skills`), để kĩ năng có giá trị riêng. Đã loại: đòn thường nổ vùng.

### castle-attack-stats | AI 8 | Đòn tự bắn có sức đánh, tốc độ đánh và tầm đánh; thành không di chuyển
Dùng chung định nghĩa chỉ số với hero và quái (`feature-hero/hero-stats`), để mọi thứ đọc được bằng một bộ từ.

### castle-short-range | AI 7 | Tầm bắn của thành ngắn hơn khoảng đất mỗi bên, quái ở xa là việc của hero
Thành là tuyến chặn cuối, đánh quái đã lọt qua hàng hero. Đã loại: thành bắn khắp chiến trường (hero mất vai trò).
