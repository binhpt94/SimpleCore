---
based-on: a2ebbbaf3ca9
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-castle/castle-skills/skill-values@ad45ef4fdcc9, feature-hero/hero-list/hero-roster-values@8a2346250094
frame: none
---
# Thông số nhịp giao chiến

Một nhịp giao chiến ứng với một bước cập nhật cố định của trận, 50 bước mỗi giây. Node này chốt bước đó, thứ tự xử lí trong nhịp, và cách làm tròn sát thương.

## Ý cốt lõi

### fixed-step | AI 6 | Trận cập nhật theo bước cố định 0.02 giây (50 bước mỗi giây); mỗi bước một nhịp giao chiến
Bước cố định cho kết quả giống nhau trên máy nhanh và máy chậm. Khung hình chậm thì chạy nhiều bước trong một khung, tối đa 5 bước.

### tick-order | AI 7 | Trong một nhịp: đi và nhắm, ra đòn, gom đòn, lọc, cộng, chủ trừ máu, báo hết máu, báo nhịp xong
Mọi bên ra đòn trong bước dựa trên vị trí và trạng thái đầu bước.

### damage-real | AI 7 | Sát thương giữ số thực khi cộng và trừ, không làm tròn trong nhịp
Hiện số do HUD (máu thành làm tròn xuống).

### skill-is-hit | AI 7 | Sấm sét tạo một đòn trúng cho mỗi quái trong vùng, ở nhịp người chơi bấm
Bên ra đòn ghi là thành.

### kill-credit | AI 6 | Quái hết máu do tổng của nhiều bên thì vẫn cho tiền đúng một lần, không chia theo bên
Tiền vào ví như mọi lần hạ quái.

### pause-no-tick | AI 7 | Lúc tạm dừng, lúc chờ hồi sinh và sau khi lượt kết thúc không có nhịp nào chạy
