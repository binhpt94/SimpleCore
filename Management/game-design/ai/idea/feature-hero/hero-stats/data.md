---
based-on: c5b904d13f66
refs: feature-hero/hero-list@1bf8998ab5a4, feature-hero/hero-respawn@2ae1008b33cd, feature-enemy/enemy-stats@504e5ffe01a8, feature-castle/castle-attack@639d373f52f2, feature-castle/castle-core@e1440a2a0de0, feature-castle/castle-upgrade-effects@2c1e443d0ced, glossary#hp@5433e9791fe3, glossary#attack@8196b706c38a, glossary#attack-speed@e3b0f90feef6, glossary#range@e47b0e576df3, glossary#hero-level@58786657874d
---
# Chỉ số và cấp hero

Mọi bên chiến đấu trong game (hero, quái, thành) dùng chung một bộ chỉ số: máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển. Node này định nghĩa bộ chỉ số đó và cách cấp hero làm hero mạnh lên.

## Ý cốt lõi

### shared-stats | AI 9 | Cả game dùng chung năm chỉ số: máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển
Hero, quái và thành cùng một bộ từ nên so sánh được với nhau. Thành không có tốc độ di chuyển. Đã loại: thêm giáp, chí mạng, né (khó đọc với người chơi phổ thông).

### stat-damage | AI 8 | Một đòn trúng thì trừ máu mục tiêu đúng bằng sức đánh của bên đánh
Không có giáp hay giảm sát thương. Đã loại: công thức giáp trừ sát thương.

### hero-stats-differ | AI 8 | Mỗi hero có bộ giá trị chỉ số riêng, hợp với cách đánh của nó
Ví dụ hero cận chiến máu nhiều, tầm ngắn; hero bắn xa máu ít, tầm xa. Danh sách hero ở `feature-hero/hero-list`.

### hero-level | AI 8 | Mỗi hero trong đội có một cấp; lên cấp thì mọi chỉ số chiến đấu của hero đó tăng
Một mục "cấp hero" cho mỗi hero, ít nút. Tốc độ di chuyển không tăng để đội hình không đổi dáng. Đã loại: nâng từng chỉ số riêng (nhiều nút), nâng theo loại hero.

### hero-level-kept | AI 8 | Cấp hero giữ suốt lượt chơi, kể cả khi hero gục
Gục không mất cấp (`feature-hero/hero-respawn`). Lượt mới thì về cấp ban đầu do `feature-run`.
