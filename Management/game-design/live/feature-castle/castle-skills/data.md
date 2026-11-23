---
based-on: 24fd307bbdcd
refs: feature-castle/castle-core@e1440a2a0de0, feature-castle/castle-upgrade-effects@2c1e443d0ced, feature-castle/castle-attack@639d373f52f2, feature-enemy/enemy-reward@1fbbced31e0f, glossary#castle-skill@1c6e2774b71d, glossary#castle-zone@dbff38bfeb23
---
# Kĩ năng thành

Thành có một bộ kĩ năng cố định, dùng được ngay từ đầu trận. Người chơi bấm là kĩ năng dùng ngay, không chọn vị trí; dùng xong kĩ năng đó phải chờ hồi chiêu mới dùng lại. Đây là thao tác chủ động chính của người chơi trong trận.

## Ý cốt lõi

### skill-list | AI 7 | Thành có ba kĩ năng: Sấm sét, Băng giá, Cổ vũ
Ba kĩ năng ba việc khác nhau: gây sát thương, làm chậm, hồi máu hero. Đủ để người chơi phải chọn lúc nào dùng cái nào, không quá nhiều nút. Đã loại: một kĩ năng (không có lựa chọn), kĩ năng gọi thêm hero (người đã loại thành tự sinh hero).

### skill-thunder | AI 7 | Sấm sét: đánh trúng mọi quái trong một vùng quanh thành ở cả hai bên
Giải vây khi quái dồn sát thành. Vùng quanh thành nên không cần chọn vị trí.

### skill-frost | AI 7 | Băng giá: làm chậm di chuyển và đòn đánh của mọi quái trên chiến trường trong một lúc
Mua thời gian cho hero và thành dọn quái. Không gây sát thương.

### skill-rally | AI 6 | Cổ vũ: hồi một phần máu cho mọi hero đang trên chiến trường
Cứu hero sắp gục. Không hồi máu thành (máu thành không tự hồi, `feature-castle/castle-core`) và không làm hero đang gục hồi sinh sớm. Đã loại: hồi máu thành.

### skill-cooldown | AI 8 | Mỗi kĩ năng có hồi chiêu riêng; kĩ năng mạnh hơn thì hồi lâu hơn
Vào trận mọi kĩ năng đều sẵn sàng. Kĩ năng không tốn tiền. Đã loại: dùng kĩ năng tốn tiền (tranh tiền với nâng cấp làm người chơi ngại bấm).

### skill-press-instant | AI 8 | Bấm là dùng ngay, không chọn vị trí, không chọn mục tiêu
Hợp thao tác một chạm trên điện thoại. Lúc tạm dừng không bấm được. Đã loại: kéo thả để nhắm vùng.
