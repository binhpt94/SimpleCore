---
based-on: 6181a7c21bf0
---
# Nhân vật thử Spine

Một nhân vật hoạt hình xương Spine đứng trên chiến trường của màn trận, để xem animation Spine hiển thị và chạy ra sao giữa các sprite 2D của game. Feature này dùng để thử, không phải tính năng chơi của game.

## Ý cốt lõi

### spine-purpose | AI 8 | Feature này để thử nhân vật Spine hiện và chạy animation thế nào trong game, không thêm gì vào lối chơi
Người yêu cầu ngày 2026-11-26. Kết quả là ảnh và lần chơi thử cho người xem, trước khi quyết có dùng Spine cho hero, quái hay không.

### spine-on-battle | AI 8 | Nhân vật thử đứng trên chiến trường của màn trận, chân chạm đất như hero và quái
Màn trận có đủ thứ để đặt cạnh: nền, thành, hero, quái, hiệu ứng, HUD và popup. Đã loại: một màn riêng chỉ có nhân vật thử (không có gì để so).

### spine-checks | AI 8 | Mỗi điều cần kiểm về Spine là một ý chấm được ở node bên dưới
Ví dụ: nhân vật hiện đúng hình; đổi animation; animation lặp và animation chạy một lần rồi quay về; chuyển mượt giữa hai animation; quay mặt trái phải; thứ tự vẽ cạnh hero, quái; cỡ ở màn rộng; nằm dưới HUD và popup.

### spine-switch | AI 8 | Người thử tự đổi animation ngay lúc đang xem, không phải mở lại màn trận
Thấy được chuyển từ animation này sang animation khác, là chỗ Spine hay lỗi nhất.

### spine-sample-asset | AI 7 | Nhân vật thử là một nhân vật Spine mẫu có sẵn, không đặt vẽ mới
Studio của project không dựng được bộ xương Spine. Thử runtime trước, có kết quả mới tính tới art Spine riêng.

### spine-no-effect | AI 8 | Nhân vật thử không đổi gì trong trận và không gửi, không đọc gì của feature khác
Quái và hero đi xuyên qua nhân vật, không đánh nó, không bị chặn. Bỏ feature này đi thì trận vẫn y như cũ.
