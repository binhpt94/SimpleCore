---
based-on: 53af9956f851
by: balance-recovery=gpt@r2-1#balance-recovery, balance-team-fair=gpt@r2-1#balance-team-matchups, balance-run-pressure=gpt@r2-1#balance-run-pressure, balance-shared-tuning=gpt@r2-1#balance-shared-tuning
---
# Cân bằng số liệu

Nhìn mọi số liệu của game (sức hero, sức quái, độ khó đợt, giá nâng cấp, tiền nhận) như một tổng thể, để lượt chơi khó dần đúng nhịp và lượt nào rồi cũng kết thúc.

## Ý cốt lõi

### balance-run-pressure | AI 5 | Cân đối sức ép từ quái với sức phòng thủ, để chọn đội và chi tiền tốt thì trụ lâu hơn
Xét sức ép của đợt cùng sức mạnh mua được và hư hại thành mang sang đợt sau; không ấn định cách tăng chỉ số quái.

### balance-shared-tuning | AI 6 | Cân đối các số liệu tác động qua lại giữa các feature trong cùng một lượt
Đối chiếu tiền kiếm được, giá mua và sức mạnh nhận được, để mỗi lựa chọn nâng cấp đều đáng cân nhắc.

### balance-team-fair | AI 7 | Các đội hero khác nhau phát huy thế mạnh trước những kiểu sức ép khác nhau
So sánh đội theo tình huống phòng thủ: chốt đội trước lượt là chọn được gì và đánh đổi gì. Không đội nào luôn thắng mọi đội khác.

### balance-recovery | AI 7 | Khi thành bị dồn ép, một quyết định đúng lúc vẫn xoay chuyển được thế thủ
Một lần bấm kĩ năng thành hay mua nâng cấp kịp lúc phải tạo khác biệt người chơi nhận thấy; theo cảm giác chủ đạo ở vòng 1.
