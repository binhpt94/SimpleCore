---
based-on: 6181a7c21bf0
---
# Khối thử 3D

Vài khối lập phương 3D đặt trên chiến trường của màn trận, để xem model 3D hiển thị ra sao giữa các sprite 2D của game. Feature này dùng để thử, không phải tính năng chơi của game.

## Ý cốt lõi

### cube-purpose | AI 8 | Feature này để thử model 3D hiển thị thế nào trong game, không thêm gì vào lối chơi
Người yêu cầu ngày 2026-11-26. Kết quả là ảnh và lần chơi thử cho người xem, không phải thứ người chơi dùng.

### cube-on-battle | AI 8 | Khối thử nằm trên chiến trường của màn trận, đứng trên mặt đất như hero và quái
Màn trận có đủ thứ để đặt cạnh: nền, thành, hero, quái, hiệu ứng, HUD và popup. Đã loại: một màn riêng chỉ có khối thử (không có gì để so).

### cube-cases | AI 8 | Mỗi trường hợp hiển thị đáng thử là một ý chấm được ở node bên dưới
Ví dụ: thứ tự vẽ trước và sau thành, hero, quái; ánh sáng và bóng; viền và bảng màu; cỡ ở màn 16:9 và 21:9; nằm dưới popup.

### cube-no-effect | AI 8 | Khối thử không đổi gì trong trận và không gửi, không đọc gì của feature khác
Quái và hero đi xuyên qua khối, không bị chặn; chạm vào khối không làm gì. Bỏ feature này đi thì trận vẫn y như cũ.
