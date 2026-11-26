---
based-on: a2ebbbaf3ca9
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-castle/castle-skills/skill-values@ad45ef4fdcc9, feature-hero/hero-list/hero-roster-values@b91d8c9d12d4
frame: none
---
# Thông số nhịp giao chiến

Một nhịp giao chiến ứng với một bước nhịp chung của trận, 40 bước mỗi giây. Node này chốt thứ tự xử lí trong nhịp và cách làm tròn sát thương.

## Ý cốt lõi

### fixed-step | AI 7 | Mỗi bước nhịp chung của trận (0.025 giây, 40 bước mỗi giây) chạy đúng một nhịp giao chiến
Bước, trần số bước mỗi khung và cách chạy do `feature-sim-clock` giữ; giao chiến không có đồng hồ riêng. Người yêu cầu nhịp chung 40 lần mỗi giây (2026-11-25).

### tick-order | AI 7 | Trong một nhịp: đi và nhắm, ra đòn, gom đòn, lọc, cộng, chủ trừ máu, báo hết máu, báo nhịp xong
Mọi bên ra đòn trong bước dựa trên vị trí và trạng thái đầu bước.

### damage-real | AI 7 | Sát thương giữ số thực khi cộng và trừ, không làm tròn trong nhịp
Hiện số do HUD (máu thành làm tròn xuống).

### skill-is-hit | AI 7 | Sấm sét tạo một đòn trúng cho mỗi quái trong vùng, ở nhịp người chơi bấm
Bên ra đòn ghi là thành.

### kill-credit | AI 6 | Quái hết máu do tổng của nhiều bên thì vẫn cho tiền đúng một lần, không chia theo bên
Tiền vào ví như mọi lần hạ quái.

### pause-no-tick | AI 7 | Lúc tạm dừng, lúc chờ hồi sinh và sau khi lượt kết thúc không có nhịp nào chạy
