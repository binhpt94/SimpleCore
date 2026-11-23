---
based-on: df494e209c1e
frame: none
---
# Luật số dư

Con số cụ thể và cách xử lí trường hợp biên của số dư: tiền đầu lượt, cộng, trừ, đặt lại và báo số mới. Ví xử lí từng yêu cầu một, theo thứ tự tới.

## Ý cốt lõi

### start-50 | AI 8 | Đầu mỗi lượt số dư là 50 tiền
Đủ mua ngay một cấp hero (giá 30) nhưng chưa đủ mua thêm một mục thành (giá 40), nên lựa chọn đầu tiên đã có ý nghĩa.

### integer-non-negative | AI 8 | Số dư là số nguyên từ 0 tiền trở lên, không có phần lẻ
Mọi khoản cộng và mọi giá đều là số nguyên.

### spend-equal-ok | AI 8 | Số dư bằng đúng giá thì trừ được, số dư về 0 tiền
Điều kiện trừ là số dư lớn hơn hoặc bằng giá.

### one-at-a-time | AI 8 | Mỗi yêu cầu trừ được xử lí xong mới tới yêu cầu sau, nên hai lần trừ liền nhau không bao giờ làm số dư dưới 0 tiền
Ví dụ số dư 50, hai yêu cầu trừ 30 tới gần như cùng lúc: lần đầu được (còn 20), lần sau "không đủ".

### reject-bad-amount | AI 7 | Cộng 0 tiền thì bỏ qua; cộng hay trừ một số âm là lỗi và bị từ chối, số dư giữ nguyên
Lỗi được ghi ra log để phiên code thấy, không làm dừng trận.

### notify-on-change | AI 7 | Ví báo số dư mới ngay trong khung hình số dư đổi, và luôn báo một lần khi đặt lại về 50 tiền
Số dư không đổi (cộng 0, trừ bị từ chối) thì không báo.

### cap-max | AI 6 | Số dư không vượt quá 999.999.999 tiền; cộng vượt thì dừng ở mức đó
Trong chơi thường không chạm tới; chỉ để tránh tràn số.
