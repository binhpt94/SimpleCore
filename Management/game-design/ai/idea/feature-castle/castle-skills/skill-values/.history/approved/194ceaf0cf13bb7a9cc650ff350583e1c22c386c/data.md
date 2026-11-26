---
based-on: d5a006594b09
refs: feature-enemy/enemy-behavior/enemy-move-values@820f944e332f, feature-castle/castle-upgrade-effects@2c1e443d0ced, feature-enemy/enemy-reward@1fbbced31e0f, feature-castle/castle-attack/auto-shot-values@73b7dbaf8425, feature-castle/castle-core/castle-body-values@a09073799e25
---
# Thông số ba kĩ năng thành

Giá trị, vùng tác động, hồi chiêu và hiệu ứng của Sấm sét, Băng giá, Cổ vũ. Vào trận cả ba sẵn sàng; bấm là có hiệu lực ngay trong cùng khung hình.

## Ý cốt lõi

### thunder-values | AI 8 | Sấm sét: trừ 80 máu mọi quái cách mép thành không quá 6u ở cả hai bên, hồi chiêu 20s
Vùng trùng tầm tự bắn (x 1.5→7.5u mỗi bên) nên người chơi học một vùng nguy hiểm duy nhất. 80 sát thương hạ ngay Lính tới đợt 10 (76 máu). Quái trong vùng tính lúc bấm; quái vào vùng sau đó không bị trúng.

### frost-values | AI 7 | Băng giá: mọi quái đang sống chậm 50% tốc độ đi và tốc độ đánh trong 5s, hồi chiêu 35s
Chậm 50% tốc độ đánh nghĩa là giây mỗi đòn nhân 2. Tính cả quái đã sinh nhưng chưa vào màn; quái sinh sau lúc bấm không bị chậm. Không gây sát thương.

### rally-values | AI 7 | Cổ vũ: mọi hero đang đứng trên chiến trường hồi 30% máu tối đa của chính nó, hồi chiêu 25s
Hồi một lần ngay lúc bấm, không hồi theo thời gian. Máu không vượt máu tối đa. Hero đang gục không được hồi và không hồi sinh sớm.

### skill-ready-rule | AI 8 | Hồi chiêu tính từ lúc bấm; còn hồi chiêu thì nút không bấm được; vào trận cả ba đã sẵn sàng
Ba hồi chiêu chạy độc lập. Bấm khi không có mục tiêu nào (ví dụ Sấm sét lúc vùng trống) vẫn tính là dùng và bắt đầu hồi chiêu. Đã loại: chặn bấm khi không có mục tiêu (người chơi không hiểu vì sao nút "sẵn sàng" mà bấm không được).

### frost-refresh | AI 7 | Quái đang chậm mà trúng Băng giá lần nữa thì thời gian chậm đặt lại về đủ, không cộng dồn mức chậm
Mức chậm luôn là 50%. Chỉ xảy ra khi thời gian chậm (có nâng cấp) dài hơn hồi chiêu, bình thường không gặp.

### skill-feedback | AI 7 | Mỗi kĩ năng có một hiệu ứng ngắn: Sấm sét 0.3s trên mỗi quái trúng, Băng giá dấu chậm trên quái suốt thời gian chậm, Cổ vũ 0.5s trên mỗi hero được hồi
Người chơi thấy kĩ năng trúng ai. Dấu chậm xuất hiện lúc trúng, biến mất lúc hết chậm hoặc lúc quái bị hạ.
