---
based-on: d8d13ced1680
---
# Kiểm tra dọn sạch và chơi lại nhiều lượt

Cách kiểm được rằng rời scene không để lại gì và chơi lại nhiều lượt trong scene Game không dồn rác: số view, số việc đang chạy trước và sau phải bằng nhau.

## Ý cốt lõi

### after-dispose-zero | AI 8 | Sau bước huỷ, scene không còn view nào, không còn việc hẹn giờ hay việc đang chạy nào của feature tham gia
Ngoại lệ duy nhất là lớp che của `feature-scene-flow`, vì nó chỉ sống trong lúc chuyển.

### replay-no-growth | AI 7 | Chơi 10 lượt liền trong scene Game, số view và số việc đang chạy ở đầu lượt thứ 10 bằng ở đầu lượt thứ 1
Kiểm cho đặt lại lượt (`feature-run/run-start`) không để lại quái, hero, hiệu ứng hay popup của lượt trước.

### popup-cleared-on-reset | AI 7 | Đầu lượt mới không còn popup nào đang mở
Popup thua đã đóng khi bấm nút; nếu vì lỗi còn popup mở thì đó là lỗi cần báo, không tự đóng âm thầm.

### only-record-saved | AI 7 | Kỉ lục là thứ duy nhất được ghi lại để còn sau khi tắt game
Kỉ lục do `feature-run` lưu. Đội đã nhớ (`feature-team-select/team-memory`) chỉ sống trong scene Game lúc game đang mở. Dịch vụ này không ghi gì.
