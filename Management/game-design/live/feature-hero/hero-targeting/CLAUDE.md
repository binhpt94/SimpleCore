# Hero tự nhắm và tự đánh

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT hero có MỘT mục tiêu tại một lúc.
- NHIỀU hero có thể cùng nhắm MỘT quái.
- MỘT quái có thể bị NHIỀU hero và thành cùng đánh.
- Đòn trúng một hay nhiều quái là theo cách đánh của từng hero (`feature-hero/hero-list`).

## Định nghĩa
- **Mục tiêu**: quái còn sống mà hero đang đi tới hoặc đang đánh.
- **Gần nhất**: khoảng cách trên dải đất từ hero tới quái nhỏ nhất, không phân biệt bên trái hay bên phải.
- **Trong tầm**: khoảng cách tới mục tiêu không quá tầm đánh của hero (`feature-hero/hero-stats`).
- **Đứng chờ**: hero không có mục tiêu, đi về và đứng ở thành.

## Danh sách
Trạng thái của hero trên chiến trường:
- Đứng chờ: không có quái, đứng ở thành.
- Đi tới: có mục tiêu ngoài tầm, đi về phía nó.
- Đánh: mục tiêu trong tầm, đứng yên và đánh theo tốc độ đánh.
- Gục: rời trận (`feature-hero/hero-respawn`).

## Luật cơ bản
- Có quái thì mỗi hero chọn quái gần mình nhất làm mục tiêu.
- Đang đi tới mà có quái khác gần hơn thì đổi mục tiêu sang con đó.
- Đang đánh thì giữ mục tiêu tới khi nó bị hạ hoặc ra khỏi tầm, rồi chọn lại.
- Hero không chắn nhau, đi qua nhau được trên một làn.
- Hero đi được tới hai mép chiến trường, không ra ngoài.
- Hết quái thì hero đi về thành và đứng chờ.
- Hero hạ quái thì quái cho tiền như mọi quái (`feature-enemy/enemy-reward`).
- Thưởng-phạt: hero dồn về bên có quái gần hơn, nên bên kia chỉ còn thành bắn; người chơi bù bằng kĩ năng thành và nâng cấp.

## Đã tự quyết
- **Nhắm quái gần nhất ở cả hai bên, không chia bên cố định.** Đúng quyết định chung; không thêm thao tác cho người chơi. Đã loại: mỗi hero giữ một bên, người chơi chọn bên cho hero.
- **Đổi mục tiêu khi đang đi, giữ mục tiêu khi đang đánh.** Tránh hero quay qua quay lại liên tục mà vẫn phản ứng với quái mới đến gần.
- **Hết quái thì về thành.** Thành ở giữa nên là chỗ đứng chờ cân bằng cho cả hai bên. Đã loại: đứng tại chỗ.
- **Hero không chắn nhau.** Một làn mà hero chắn nhau thì hero bắn xa bị kẹt sau hero cận chiến; đi qua nhau giữ luật đơn giản. Việc "chắn quái" nằm ở hành vi quái dừng lại đánh hero (`feature-enemy/enemy-behavior`).

## Chưa rõ
- Không có điểm chặn.
