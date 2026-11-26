---
based-on: 691534d6dde7
frame: none
---
# Cách đếm điểm và lưu kỉ lục

Điểm đếm từ 0, cộng 1 mỗi lần dọn sạch một đợt; chốt một lần khi lượt kết thúc. Kỉ lục là số nguyên do `feature-save` cất trên máy, giao ghi ngay khi bị vượt.

## Ý cốt lõi

### score-start-zero | AI 8 | Đầu lượt điểm bằng 0; mỗi lần `feature-wave` báo dọn sạch một đợt thì điểm cộng 1
Dọn sạch đợt 1 thì điểm 1; thua giữa đợt 4 thì điểm 3.

### score-lock-once | AI 8 | Điểm chốt đúng một lần, ngay lúc lượt kết thúc (thành bị phá)
Sau khi chốt, báo dọn sạch tới muộn không làm đổi điểm.

### record-update | AI 8 | Chốt điểm xong: điểm lớn hơn kỉ lục thì kỉ lục bằng điểm và cờ "kỉ lục mới" bật
Bằng hoặc nhỏ hơn thì kỉ lục giữ nguyên, cờ tắt. Điểm 0 không bao giờ bật cờ.

### record-default-zero | AI 7 | `feature-save` báo chưa có kỉ lục thì kỉ lục là 0
Lần mở game đầu tiên, hoặc dữ liệu lưu đọc không được, `feature-save` đều báo chưa có; lượt chơi coi là 0.

### record-save-now | AI 7 | Kỉ lục mới được giao `feature-save` ghi xuống máy ngay khi đổi, trước khi popup thua hiện
Tắt game ngay trên popup thua vẫn giữ kỉ lục mới.

### record-keep-on-fail | AI 7 | `feature-save` báo ghi thất bại thì kỉ lục mới vẫn giữ trong bộ nhớ tới khi tắt game
Màn bắt đầu và popup thua vẫn thấy kỉ lục mới; không báo lỗi cho người chơi.

### record-retry | AI 5 | Kỉ lục chưa ghi được thì thử ghi lại khi game trở lại màn hình, và ở lần chốt lượt kế tiếp
Mỗi dịp thử một lần; ghi được thì thôi. Lượt sau không phá kỉ lục vẫn thử.

### record-read-anytime | AI 7 | Điểm, kỉ lục và cờ kỉ lục mới đọc được bất cứ lúc nào, không cần chờ
`feature-home` đọc kỉ lục; `feature-popup-lose` đọc cả ba.
