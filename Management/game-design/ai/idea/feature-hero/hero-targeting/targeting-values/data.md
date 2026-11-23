---
based-on: 49b5ce7a72b8
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-hero/hero-list/hero-roster-values@8a2346250094, feature-castle/castle-core/castle-body-values@a89edb2f03ba, feature-wave/spawning/spawn-order@5884f63cfa93
---
# Thông số nhắm và di chuyển của hero

Hero chọn quái gần mình nhất trên màn (tâm quái trong x=-16u tới +16u), đi tới với tốc độ chạy của mình, dừng khi mục tiêu vào tầm và đánh. Hero không bao giờ ra quá x=±15u; hết quái thì về điểm xuất phát ở mép thành.

## Ý cốt lõi

### target-on-screen | AI 8 | Hero chỉ nhắm quái còn sống có tâm trong x=-16u tới +16u, chọn con có khoảng cách nhỏ nhất
Quái vừa sinh ở x=±17u chưa bị nhắm cho tới khi vào màn. Bằng khoảng cách thì chọn quái gần thành hơn, vẫn bằng thì chọn quái ra trước. Khoảng cách theo `feature-hero/hero-stats/hero-level-values`.

### walk-limit | AI 8 | Tâm hero luôn trong x=-15u tới +15u; mục tiêu ngoài đó thì hero đứng ở mốc ±15u chờ
Đứng ở mốc mà mục tiêu vào tầm thì đánh. Hero bắn xa tầm 6-7u luôn với tới quái trên màn; Hiệp sĩ ở x=15u với tới quái có mép trong tới 16.7u.

### retarget-check | AI 7 | Khi đang đi, mỗi khung hình chọn lại quái gần nhất; khi đang đánh, giữ mục tiêu tới khi nó bị hạ hoặc khoảng cách lớn hơn tầm
Hero cận chiến đang đi về bên trái mà một con Sói tới gần hơn ở bên phải thì quay sang Sói ngay.

### idle-return | AI 7 | Không còn quái nào trên màn thì hero đi về điểm xuất phát của chỗ mình (x=-1.5u hoặc +1.5u) và đứng chờ
Đi với tốc độ chạy bình thường. Quái mới vào màn khi hero đang về thì hero quay ra ngay.
