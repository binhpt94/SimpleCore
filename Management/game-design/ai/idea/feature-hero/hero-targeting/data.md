---
based-on: 46623bac3830
refs: feature-hero/hero-list@1bf8998ab5a4, feature-hero/hero-stats@2748621645f8, feature-hero/hero-respawn@2ae1008b33cd, feature-enemy/enemy-reward@1fbbced31e0f, feature-enemy/enemy-behavior@cf9290ce16e6, glossary#target@f51ab44b30ff
---
# Hero tự nhắm và tự đánh

Hero tự chọn mục tiêu, tự đi tới và tự đánh, người chơi không điều khiển. Mục tiêu luôn là quái gần hero nhất, xét cả hai bên thành; trên dải đất một làn, hero chỉ đi qua lại giữa hai mép.

## Ý cốt lõi

### target-nearest | AI 8 | Hero nhắm quái gần mình nhất, xét cả hai bên thành
Luật một câu, người chơi đoán được hero sẽ làm gì. Đã loại: mỗi hero giữ cố định một bên (người chơi phải chia đội, thêm thao tác); nhắm quái máu thấp nhất.

### move-then-attack | AI 8 | Mục tiêu ngoài tầm thì hero đi về phía nó; vào tầm thì dừng lại đánh
Hero cận chiến vì vậy đứng trước, hero bắn xa đứng sau một cách tự nhiên, không cần luật xếp hàng.

### retarget | AI 7 | Mục tiêu bị hạ thì hero chọn lại quái gần nhất
Hero không bám mục tiêu cũ khi có quái khác đến gần hơn lúc đang đi; khi đã đứng đánh thì đánh tới khi mục tiêu bị hạ hoặc ra khỏi tầm.

### idle-at-castle | AI 7 | Không còn quái trên chiến trường thì hero đi về đứng ở thành
Giữa hai đợt cả đội tụ lại ở thành, sẵn sàng cho cả hai bên. Đã loại: đứng yên tại chỗ (đội có thể kẹt ở một mép khi đợt mới vào từ mép kia).

### no-player-control | AI 8 | Người chơi không ra lệnh, kéo thả hay chọn mục tiêu cho hero
Người chơi tác động qua nâng cấp và kĩ năng thành. Đã loại (vòng 2): kéo thả hero, chọn mục tiêu.
