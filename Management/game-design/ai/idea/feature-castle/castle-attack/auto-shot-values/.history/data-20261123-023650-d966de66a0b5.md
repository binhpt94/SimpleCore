---
based-on: 639d373f52f2
---
# Thông số đòn tự bắn

Thành tự bắn 1 phát mỗi 1.0s vào quái gần mép thành nhất trong tầm 6u, xét cả hai bên, mỗi phát 10 sát thương vào đúng một quái. Sát thương tính ngay lúc bắn.

## Ý cốt lõi

### shot-values | AI 8 | Mỗi phát trừ 10 máu, 1 phát mỗi 1.0s, tầm 6u tính từ mép thành
Tầm 6u phủ x từ 1.5u tới 7.5u mỗi bên, chưa tới một nửa khoảng đất 14.5u mỗi bên, nên quái ở xa vẫn là việc của hero. 10 sát thương hạ Lính đợt 1 (40 máu) sau 4 phát.

### shot-target-rule | AI 8 | Mục tiêu là quái còn sống có khoảng cách tới mép thành nhỏ nhất, không quá 6u, xét cả hai bên
Khoảng cách theo định nghĩa chung ở `feature-hero/hero-stats/hero-level-values` (khoảng trống giữa hai mép hộp). Bằng nhau thì chọn quái ra trước. Mỗi lần sắp bắn thì chọn lại, nên quái mới tới gần hơn được bắn trước. Đã loại: giữ mục tiêu tới khi chết (quái sát thành bị bỏ mặc).

### shot-instant | AI 7 | Sát thương tính ngay lúc bắn; hình viên đạn bay tới mục tiêu trong 0.15s chỉ để nhìn
Không có đạn trượt, không có đạn bay tới quái đã chết. Đã loại: đạn bay thật có thời gian (thêm trường hợp mục tiêu chết giữa đường).

### shot-timer | AI 7 | Bộ đếm 1.0s chạy liên tục; có mục tiêu và đã đủ 1.0s kể từ phát trước thì bắn ngay
Phát đầu tiên của trận bắn ngay khi có quái đầu tiên vào tầm. Không có quái trong tầm thì không bắn và bộ đếm dừng ở mức sẵn sàng.
