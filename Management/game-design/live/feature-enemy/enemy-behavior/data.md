---
based-on: b74c0b7d272a
refs: feature-castle/castle-core@e1440a2a0de0, feature-enemy/enemy-stats@504e5ffe01a8, feature-castle/castle-skills@8bc45d0edc8d, feature-hero/hero-targeting@49b5ce7a72b8
---
# Quái đi và đánh

Quái ra ở mép của bên mình rồi đi thẳng vào thành trên dải đất một làn. Gặp hero trong tầm thì dừng lại đánh hero; tới thành thì đứng đánh thành. Quái không quay đầu và không bao giờ sang bên kia thành.

## Ý cốt lõi

### walk-to-castle | AI 8 | Quái đi thẳng từ mép bên mình vào thành, không quay đầu
Một làn, một hướng, người chơi đọc được ngay quái nào sắp tới thành. Đã loại: quái đi vòng, rút lui.

### stop-for-hero | AI 8 | Có hero trong tầm thì quái dừng lại đánh hero gần nhất
Cho hero cận chiến vai chắn quái. Hero hơn thành: có cả hero và thành trong tầm thì đánh hero. Đã loại (vòng 2): quái bỏ qua hero, chỉ nhắm thành.

### resume-walk | AI 7 | Hero trong tầm bị hạ hoặc đi khỏi tầm thì quái đi tiếp vào thành
Quái không đuổi theo hero đi ra xa.

### attack-castle | AI 8 | Tới thành thì quái đứng đánh thành tới khi bị hạ
Mỗi đòn trừ máu thành (`feature-castle/castle-core`). Quái không đi qua thành.

### enemy-no-block | AI 7 | Quái không chắn nhau, không đánh nhau, đi qua nhau được
Quái dồn cụm tự nhiên khi bị hero chắn, hợp với đòn vùng của hero và kĩ năng thành.
