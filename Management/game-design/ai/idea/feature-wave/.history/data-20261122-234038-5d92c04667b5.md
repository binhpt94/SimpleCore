---
based-on: cf568e2870fe
---
# Đợt quái

Điều phối nhịp trận: thả quái của từng đợt từ hai bên, nhận biết khi đợt đã dọn sạch, nghỉ vài giây rồi tự sang đợt sau mạnh hơn. Chơi vô tận, không có đợt cuối.

## Ý cốt lõi

### wave-number | AI 8
Đợt có số thứ tự, bắt đầu từ đợt 1; số đợt hiện tại luôn hiện trên màn hình.

### wave-content | AI 7
Mỗi đợt có danh sách quái (loại, số lượng) và nhịp thả, chia ra hai bên trái và phải.

### wave-scaling | AI 7
Đợt sau đông hơn và quái mạnh hơn đợt trước, tính bằng công thức theo số đợt (không viết tay từng đợt) để chơi vô tận.

### wave-clear | AI 8
Đợt kết thúc khi đã thả hết và hạ hết quái của đợt. Dọn sạch thì được thưởng thêm tiền và số đợt đã dọn tăng 1 (điểm ở `feature-run`).

### wave-gap | AI 6
Giữa hai đợt có khoảng nghỉ ngắn tự đếm ngược (dự kiến 5 giây) rồi đợt sau tự bắt đầu; người chơi có thể bấm để vào sớm.

### wave-stop-on-lose | AI 8
Thành hết máu giữa đợt thì dừng thả quái ngay và báo `feature-run`.
