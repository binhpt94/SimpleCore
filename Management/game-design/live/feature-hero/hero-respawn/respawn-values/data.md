---
based-on: 2ae1008b33cd
refs: feature-castle/castle-core/castle-body-values@3e635d304ab3, feature-hero/hero-targeting@49b5ce7a72b8, feature-hero/hero-list/hero-roster-values@b91d8c9d12d4
---
# Thông số vào trận, gục và hồi sinh

Lúc vào trận 3 hero sinh cùng lúc ở mép thành theo chỗ trong đội. Hero máu về 0 thì gục, biến mất, chờ 8s rồi hồi sinh ở đúng điểm xuất phát của chỗ đó, đầy máu, giữ cấp.

## Ý cốt lõi

### enter-spawn | AI 8 | Vào trận cả 3 hero sinh trong cùng khung hình: chỗ 1 và 3 ở x=-1.5u, chỗ 2 ở x=+1.5u, đầy máu, cấp 1
Điểm xuất phát lấy từ `feature-castle/castle-core/castle-body-values`. Sinh xong hero đứng chờ tới khi có quái (`feature-hero/hero-targeting`).

### down-rule | AI 8 | Máu hiện tại ≤ 0 thì hero gục: thu nhỏ về 0 trong 0.3s rồi biến mất, không đánh, không bị đánh, không bị nhắm
Ngay khung hình gục, hero thôi là mục tiêu của quái và không nhận đòn nào nữa; 0.3s thu nhỏ chỉ để nhìn.

### respawn-time | AI 7 | Hồi sinh sau 8s tính từ lúc gục, ở điểm xuất phát của chỗ mình, đầy máu theo cấp hiện tại
8s bằng khoảng 1/4 thời gian một đợt đầu (dự kiến 25-35s), đủ thấy đội thiếu người nhưng không mất cả đợt. Như nhau với mọi hero, mọi cấp.

### respawn-entry | AI 7 | Hồi sinh thì phóng to từ 0 lên đủ trong 0.3s; trong 0.3s đó hero chưa đánh và chưa bị đánh
Tránh hero bị đánh gục lại ngay lúc vừa hiện khi quái đứng sát thành. Hết 0.3s hero nhắm và đánh như bình thường.
