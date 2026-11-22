---
based-on: 24fd307bbdcd
---
# Hiệu ứng nâng cấp thành

Thành có ba mục nâng cấp: máu, sức đánh, kĩ năng. Mỗi mục có cấp riêng; mỗi lần mua lên một cấp và thành mạnh lên ngay. Node này nói mỗi mục làm thành mạnh lên thế nào; mua và giá là của `feature-upgrade`.

## Ý cốt lõi

### castle-upgrade-three | AI 8 | Thành có đúng ba mục nâng cấp: máu, sức đánh, kĩ năng
Mỗi mục ứng với một vai của thành: chịu đòn, tự bắn, cứu nguy. Đã loại: nâng tốc độ đánh, tầm bắn riêng (thêm nút mà ít khác biệt).

### castle-upgrade-level | AI 7 | Mỗi mục có cấp riêng, mỗi lần mua lên một cấp
Ba mục độc lập; nâng mục này không đổi mục kia. Cấp giữ tới hết lượt.

### upgrade-hp-effect | AI 8 | Nâng máu tăng máu tối đa, và máu hiện tại tăng thêm đúng phần đó
Đây là cách duy nhất thành có thêm máu trong trận. Không hồi phần máu đã mất trước đó. Đã loại: nâng máu hồi đầy máu (biến nâng cấp thành nút hồi máu).

### upgrade-attack-effect | AI 8 | Nâng sức đánh làm mỗi phát tự bắn của thành trừ nhiều máu quái hơn
Chỉ tăng sức đánh của đòn tự bắn (`feature-castle/castle-attack`), không đổi tốc độ đánh hay tầm.

### upgrade-skill-effect | AI 7 | Nâng kĩ năng làm cả ba kĩ năng thành mạnh hơn, hồi chiêu không đổi
Sấm sét trừ nhiều máu hơn, Băng giá làm chậm lâu hơn, Cổ vũ hồi nhiều máu hơn (`feature-castle/castle-skills`). Đã loại: nâng riêng từng kĩ năng (thêm nút), giảm hồi chiêu (người chơi phổ thông khó thấy rõ).
