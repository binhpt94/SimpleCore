---
based-on: e712e2fbe1f0
---
# Hiệu lực nâng cấp

Mua một cấp xong thì hero hoặc thành mạnh lên ngay, theo đúng mục đã mua. Feature này nói mỗi mục làm mạnh cái gì; con số do từng feature giữ chỉ số và `feature-balance` quyết.

## Ý cốt lõi

### effect-instant | AI 8 | Cấp mới có hiệu lực ngay lúc mua, không chờ hết đợt
Đã loại (người loại ở vòng 1): chỉ nâng cấp giữa hai đợt.

### effect-hero-all-stats | AI 8 | Lên cấp hero thì mọi chỉ số của hero đó cùng tăng
Ví dụ máu và sức đánh cùng tăng. Hero nào tăng chỉ số nào bao nhiêu là của `feature-hero`.

### effect-fallen-hero | AI 7 | Hero đang gục vẫn lên cấp, hồi sinh là mang cấp mới
Không phải chờ hero đứng dậy mới mua. Gục không làm mất cấp (`feature-hero`, `hero-respawn`).

### effect-castle-health | AI 8 | Lên cấp máu thành thì máu tối đa tăng và máu hiện tại tăng thêm đúng phần đó
Mua máu vừa làm thành bền hơn vừa cứu nguy ngay. Đã loại: chỉ tăng máu tối đa (mua lúc nguy không thấy tác dụng).

### effect-castle-attack | AI 8 | Lên cấp sức đánh thì đòn tự bắn của thành mạnh hơn
Không đổi kĩ năng thành.

### effect-castle-skill | AI 7 | Lên cấp kĩ năng thì cả ba kĩ năng thành cùng mạnh hơn, hồi chiêu không đổi
Mỗi kĩ năng mạnh hơn theo kiểu riêng do `feature-castle/castle-upgrade-effects` quyết. Đã loại: giảm hồi chiêu.
