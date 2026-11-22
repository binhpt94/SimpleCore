---
based-on: 9e167e1aa89f
---
# Xin chuyển scene

Cách một feature xin chuyển scene và việc feature này làm khi chuyển: huỷ scene cũ, tải scene mới, che màn hình trong lúc chờ.

## Ý cốt lõi

### request-by-name | AI 7 | Feature khác xin chuyển bằng tên scene đích, không tự tải scene
Feature xin không cần biết scene được tải thế nào. Đã loại: mỗi feature tự gọi tải scene (dễ tải chồng, bỏ sót bước huỷ).

### switch-order | AI 8 | Chuyển scene luôn theo thứ tự: huỷ xong scene cũ rồi mới tải scene mới
Hai bước nhờ `feature-session-scene` làm. Huỷ trước để không có hai scene cùng chạy.

### one-switch-at-a-time | AI 7 | Mỗi lúc chỉ có một lần chuyển scene
Đang chuyển thì không bắt đầu lần chuyển khác.

### switch-cover | AI 7 | Trong lúc chuyển, màn hình được che, người chơi không thấy scene dở dang
Che bằng một lớp chung của feature này. Đã loại: để màn đen trống.
