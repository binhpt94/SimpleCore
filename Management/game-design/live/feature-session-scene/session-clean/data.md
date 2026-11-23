---
based-on: 43be0f08eadd
refs: feature-session-scene/session-steps@70f3b17acc2e
---
# Dọn sạch và bắt đầu sạch

Rời scene thì không còn gì của scene đó sống sót, trừ dữ liệu đã lưu như kỉ lục. Đặt lại một lượt chơi mới bên trong scene Game là việc của `feature-run`, không phải tải lại scene.

## Ý cốt lõi

### nothing-survives | AI 8 | Sau bước huỷ, không view hay trạng thái nào của scene còn lại
Chuyển scene xong như mới mở. Đã loại: giữ lại một phần view cho scene sau dùng (khó biết ai dọn).

### saved-data-only | AI 7 | Thứ duy nhất đi qua scene là dữ liệu đã lưu (kỉ lục)
Kỉ lục do `feature-run` lưu, không phải dịch vụ này giữ.

### run-reset-in-scene | AI 7 | Lượt mới trong scene Game được đặt lại bởi các feature chơi, không bằng cách tải lại scene
Chơi lại không phải chờ tải. `feature-run` báo bắt đầu lượt; từng feature tự về trạng thái ban đầu. Đã loại: tải lại scene Game mỗi lượt (chậm, mất màn chọn đội liền mạch).
