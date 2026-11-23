---
screen: landscape
---
# Thủ thành anh hùng (tên tạm)

Game thủ thành nhìn ngang: một tòa thành đứng giữa màn hình, đội hero chọn trước sinh ra từ thành và tự đánh quái kéo tới theo từng đợt từ hai bên. Người chơi bấm kĩ năng của thành và nâng cấp hero, thành ngay trong trận để trụ lâu nhất có thể; thành bị phá là thua. Khác game thủ thành thường gặp: thành bị kẹp từ hai phía, và người chơi không điều khiển hero hay xây tháp mà chỉ chọn đội, canh kĩ năng thành và mua nâng cấp giữa trận.

## Ý cốt lõi

### target-player | AI 7 | Người chơi phổ thông, điện thoại cầm ngang, lượt chơi ngắn
Thao tác trong trận ít: bấm kĩ năng của thành và mua nâng cấp. Cái hay nằm ở chọn đội hero, chọn nâng cấp và canh lúc dùng kĩ năng. Đã loại: game nặng tính toán, thao tác nhanh tay.

### layout | AI 8 | Nhìn ngang từ bên cạnh, thành đứng yên giữa màn hình, chiến trường là dải đất hai bên
Mọi trận đánh diễn ra trên một dải đất chạy ngang qua thành. Đã loại: nhìn từ trên xuống, thành ở một mép màn hình.

### enemy-two-sides | AI 8 | Quái kéo tới theo đợt từ mép trái và mép phải
Đã loại: bốn phía (khó nhìn trên màn hình thấp), một phía (mất cảm giác bị kẹp).

### hero-auto-spawn | AI 7 | Đội hero cố định, chọn trước mỗi lượt, gục thì hồi sinh ở thành
Vào trận cả đội sinh ra ở thành; trong trận không có thêm hero. Hero gục thì sau một lúc hồi sinh ở thành. Đã loại (người loại): thành tự sinh hero theo thời gian.

### hero-auto-fight | AI 7 | Hero tự đánh, người chơi không điều khiển hero
Người chơi tác động vào trận qua nâng cấp và kĩ năng của thành. Đã loại: kéo thả hero, chọn mục tiêu.

### hero-types | AI 6 | Có nhiều hero với cách đánh khác nhau để chọn vào đội
Ví dụ cận chiến chắn quái, bắn xa đứng sau. Có nhiều hero hơn số chỗ trong đội để mỗi lượt phải chọn; mọi hero chọn được ngay. Đã loại: mở khoá hero dần.

### castle-role | AI 7 | Thành tự đánh quái và có kĩ năng hồi chiêu do người chơi bấm
Thành vừa là thứ phải giữ vừa là một chiến binh. Kĩ năng của thành là lúc người chơi tự tay cứu nguy. Đã loại (người loại): thành không đánh.

### upgrade-between-waves | AI 7 | Hạ quái được tiền, dùng tiền nâng cấp ngay trong trận, trận không dừng
Mua lúc nào cũng được, kể cả đang đánh; mạnh lên liền. Đã loại (người loại): chỉ nâng cấp giữa hai đợt.

### upgrade-targets | AI 7 | Nâng cấp được từng hero trong đội và nâng cấp được thành

### lose-condition | AI 9 | Thua khi thành bị phá

### endless-score | AI 6 | Không có màn thắng: chơi vô tận, trụ càng nhiều đợt càng cao điểm, lưu kỉ lục
Đợt sau khó hơn đợt trước. Đã loại: thắng khi qua đủ một số đợt.

### run-reset | AI 6 | Mỗi lượt chơi bắt đầu lại từ đầu, chỉ giữ kỉ lục
Tiền và nâng cấp mất khi thua; lượt sau chọn lại đội hero. Đã loại: nâng cấp vĩnh viễn giữa các lượt.

### single-map | AI 6 | Game có một bản đồ duy nhất
Đã loại: nhiều màn, nhiều bản đồ.
