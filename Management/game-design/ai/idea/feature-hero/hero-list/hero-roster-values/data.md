---
based-on: 1bf8998ab5a4
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-enemy/enemy-reward@1fbbced31e0f, feature-castle/castle-core/castle-body-values@a89edb2f03ba
---
# Năm hero: chỉ số, kích thước, kiểu đòn

Chỉ số cấp 1, hộp 2D và kiểu đòn của Hiệp sĩ, Đấu sĩ rìu, Kiếm khách, Cung thủ, Pháp sư. Mỗi đội 3 hero, chọn từ 5, không trùng; mọi hero là sprite 2D có hộp rộng 1u cao 1.5u.

## Ý cốt lõi

### team-size | AI 8 | Đội có 3 chỗ, chọn từ danh sách 5 hero, không chọn trùng
Mỗi lượt bỏ ra 2 hero nên luôn có lựa chọn. Màn chọn là `feature-team-select`.

### knight-values | AI 8 | Hiệp sĩ: 300 máu, 12 sát thương mỗi 1.0s, tầm 1.2u, chạy 2.5u/s, một mục tiêu
Máu nhiều nhất, đứng chắn. 12 sát thương/s.

### axeman-values | AI 7 | Đấu sĩ rìu: 240 máu, 14 sát thương mỗi 1.4s, tầm 1.3u, chạy 2.5u/s, chém trúng mọi quái trong 1.5u phía trước
Vùng chém tính từ mép trước của hero về phía mục tiêu, cùng bên với mục tiêu. 10 sát thương/s mỗi quái, thấp hơn Hiệp sĩ nhưng trúng nhiều con.

### swordsman-values | AI 7 | Kiếm khách: 180 máu, 16 sát thương mỗi 0.7s, tầm 1.2u, chạy 3.5u/s, một mục tiêu
Đi nhanh nhất, 22.9 sát thương/s cao nhất; máu ít nên dễ gục.

### archer-values | AI 8 | Cung thủ: 120 máu, 14 sát thương mỗi 0.8s, tầm 7u, chạy 2.5u/s, một mục tiêu
Tầm xa nhất, hơn tầm Cung thủ quái (5u), 17.5 sát thương/s.

### mage-values | AI 7 | Pháp sư: 100 máu, 18 sát thương mỗi 1.6s, tầm 6u, chạy 2.2u/s, nổ trúng mục tiêu và mọi quái cách tâm mục tiêu không quá 1.5u
11.25 sát thương/s mỗi quái, chậm nhất nhưng dọn cụm.

### hero-feedback | AI 7 | Hero đi thì nhún 0.1u mỗi 0.4s, ra đòn thì nhích 0.2u về phía mục tiêu trong 0.15s, bị đánh thì nháy 0.1s
Người chơi phân biệt được hero đang đi, đang đánh, đang bị đánh mà không cần thanh trạng thái.
