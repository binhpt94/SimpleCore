---
based-on: 24fd307bbdcd
---
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
