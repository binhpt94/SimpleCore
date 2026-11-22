# Thủ thành anh hùng (tên tạm)

Game thủ thành màn hình ngang: một tòa thành đứng giữa màn hình, đội hero chọn trước sinh ra từ thành và tự đánh quái kéo tới theo từng đợt từ hai bên. Người chơi bấm kĩ năng của thành và nâng cấp hero, thành ngay trong trận để trụ lâu nhất có thể; thành hết máu là thua.

## Ý cốt lõi

### target-player | AI 7
Người chơi phổ thông trên điện thoại cầm ngang, mỗi lượt chơi khoảng 10 đến 20 phút (dự kiến). Thao tác trong trận vừa phải: bấm kĩ năng của thành và mua nâng cấp; cái hay nằm ở chọn đội hero, chọn nâng cấp và canh lúc dùng kĩ năng.

### layout | AI 8
Màn hình ngang, một tòa thành đứng yên ở giữa, một dải mặt đất chạy ngang hai bên thành.

### enemy-two-sides | AI 8
Quái kéo tới theo từng đợt, đi từ mép trái và mép phải màn hình vào phía thành.

### hero-auto-spawn | AI 7
Trước mỗi lượt chơi, người chơi chọn sẵn một đội hero với số lượng cố định (dự kiến 4 hero). Vào trận, cả đội sinh ra ở thành; trong trận không có thêm hero. Hero gục thì hồi sinh ở thành sau một khoảng thời gian.

### hero-auto-fight | AI 7
Người chơi không điều khiển hero. Hero tự tìm và đánh quái gần nhất; người chơi tác động qua nâng cấp và kĩ năng của thành.

### hero-types | AI 6
Có một danh sách hero với cách đánh khác nhau (ví dụ cận chiến chắn quái, bắn xa đứng sau). Người chơi chọn đội từ danh sách này, mỗi hero chọn một lần. Bản đầu dự kiến 6 hero, mở sẵn hết.

### castle-role | AI 7
Thành tự đánh quái trong tầm. Thành có kĩ năng (dự kiến 1 đến 2 cho bản đầu) có thời gian hồi chiêu; người chơi bấm để kích hoạt. Quái tới được thành thì đánh trừ máu thành.

### upgrade-between-waves | AI 7
Hạ quái được tiền. Nâng cấp được bất kì lúc nào, kể cả đang đánh; trận không dừng khi mua, nâng cấp có hiệu lực ngay.

### upgrade-targets | AI 7
Nâng cấp được hai thứ: chỉ số của từng hero trong đội, và thành (máu, sức đánh, kĩ năng).

### lose-condition | AI 9
Thành còn 0 máu là thua lượt chơi.

### endless-score | AI 6
Chơi vô tận, không có đợt cuối; đợt sau đông hơn và mạnh hơn. Điểm là số đợt đã dọn sạch; lưu kỉ lục điểm cao nhất.

### run-reset | AI 6
Mỗi lượt chơi bắt đầu lại từ đầu: tiền và mọi nâng cấp mất khi thua; lượt sau chọn lại đội hero. Chỉ kỉ lục được giữ.

### single-map | AI 6
Bản đầu chỉ có một bản đồ.
