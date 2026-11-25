---
based-on: 63aec954598c
refs: feature-combat/combat-tick/tick-values@0b863ec78d17
frame: none
---
# Thông số nhịp chung

Nhịp chung chạy 40 bước mỗi giây, mỗi bước 0.025 giây, tối đa 5 bước một khung hình. Chơi thường và test cùng chạy theo bước cố định. Bản dev có tua nhanh cho tester.

## Ý cốt lõi

### tick-40 | AI 8 | Bước nhịp dài 0.025 giây, 40 bước mỗi giây
Người yêu cầu (2026-11-25). Nhịp đòn nhanh nhất của game là 0.7 giây nên 40 bước mỗi giây đủ mịn.

### default-fixed | AI 8 | Cách chạy mặc định là bước cố định, cho cả chơi thường lẫn test; theo khung hình chỉ là lựa chọn trong config
Người quyết (2026-11-25): chơi thường và test cùng một nhịp.

### cap-5 | AI 7 | Một khung hình chạy tối đa 5 bước; thời gian dư quá 0.125 giây bị bỏ, trận chậm lại chứ không nhảy
Khung hình 0.1 giây vẫn theo kịp đủ 4 bước. Máy chậm hơn mức đó thì trận chạy chậm hơn giờ thật.

### frame-mode-clamp | AI 7 | Ở cách theo khung hình, mỗi khung là một bước dài bằng thời gian khung, tối đa 0.125 giây
Cùng trần với cách bước cố định để hai cách cho cảm giác gần nhau trên máy chậm.

### steps-counted | AI 7 | Nhịp đếm số bước chung và số bước gameplay bằng số nguyên; giờ của trận bằng số bước gameplay nhân 0.025 giây
Số bước chung tăng ở mọi bước; số bước gameplay chỉ tăng ở bước lượt cho trận chạy. Thao tác ghi theo số bước chung; mọi chờ giờ đếm theo số bước gameplay nên đứng yên lúc tạm dừng. Không cộng dồn số lẻ, chạy lâu không lệch.

### timer-in-steps | AI 7 | Một chờ giờ t giây của gameplay hết ở bước đầu tiên mà giờ đã đếm từ lúc bắt đầu chờ đạt hoặc vượt t
Ví dụ hồi chiêu 20 giây hết sau đúng 800 bước; chờ 0.7 giây hết sau 28 bước.

### input-next-step | AI 7 | Thao tác của người chơi trong một khung hình được áp vào đầu bước nhịp đầu tiên chạy sau khung đó
Khung không chạy bước nào thì thao tác chờ sang bước kế tiếp. Nhiều thao tác cùng chờ thì áp theo thứ tự bấm.

### dev-speed | AI 7 | Bản dev cho tester chọn tốc độ tua 1, 2, 4, 8 hoặc 16 lần; trần số bước mỗi khung nhân theo tốc độ
Tốc độ 16 lần: tối đa 80 bước một khung.

### dev-run-steps | AI 7 | Bản dev có lệnh chạy ngay N bước không chờ khung hình, lệnh đóng băng và lệnh chạy một bước
Chạy N bước nhanh cho đúng kết quả như chạy N bước theo giờ thật ở cách bước cố định.

### view-latest | AI 6 | Màn hình vẽ trạng thái sau bước cuối cùng của khung, không nội suy giữa hai bước
40 bước trên khung 60 hình có thể hơi giật; làm mượt là việc của phần nhìn, ghi ở bảng giai đoạn.
