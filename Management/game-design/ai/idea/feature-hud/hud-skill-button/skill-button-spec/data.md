---
based-on: dda5759077eb
---
# Ba nút kĩ năng thành

HUD có 3 nút kĩ năng theo thứ tự Sấm sét, Băng giá, Cổ vũ. Nút sẵn sàng thì bấm được; đang hồi thì hiện số giây còn lại và lớp phủ vơi dần; hồi xong thì nút nảy một lần.

## Ý cốt lõi

### three-buttons-order | AI 8 | Có 3 nút theo thứ tự cố định: Sấm sét, Băng giá, Cổ vũ, mỗi nút một biểu tượng riêng
Biểu tượng đại diện cho kĩ năng: Sấm sét (sét đánh quanh thành), Băng giá (làm chậm mọi quái), Cổ vũ (hồi máu mọi hero).

### ready-tap | AI 8 | Nút sẵn sàng: chạm 1 lần là báo `feature-castle` dùng kĩ năng đó
Một chạm chỉ dùng một lần; chạm liền nhiều lần trong cùng lúc cũng chỉ dùng một lần.

### cooldown-seconds | AI 8 | Nút đang hồi hiện số giây còn lại làm tròn lên: Sấm sét bắt đầu từ 20, Băng giá 35, Cổ vũ 25
Còn 0.3 giây thì hiện "1". Hồi xong thì số biến mất. Thời gian lấy từ `feature-castle`, HUD không tự đếm.

### cooldown-overlay | AI 7 | Nút đang hồi có lớp phủ vơi dần theo tỉ lệ thời gian còn lại trên tổng hồi chiêu
Vừa bấm thì phủ kín; còn một nửa thời gian thì phủ một nửa; hết thì không còn phủ.

### cooldown-tap-nothing | AI 8 | Chạm nút đang hồi thì không có gì xảy ra và không báo `feature-castle`

### ready-bounce | AI 6 | Lúc vừa hồi xong, nút phóng to lên 1.2 lần rồi về cỡ cũ trong 0.3 giây, một lần
Người chơi đang nhìn chiến trường vẫn nhận ra kĩ năng dùng lại được.

### buttons-locked-off-battle | AI 7 | Lúc tạm dừng hoặc lượt đã kết thúc, cả 3 nút không nhận chạm và số giây đứng yên
