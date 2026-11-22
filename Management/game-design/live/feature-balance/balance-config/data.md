---
based-on: 700960590953
---
# Nơi đặt số liệu

Mỗi số liệu của game nằm trong cấu hình của đúng một feature sở hữu nó; chỉnh số không cần sửa code. Feature này giữ danh sách số nào ở đâu và là nơi quyết giá trị của chúng.

## Ý cốt lõi

### config-owner | AI 8 | Mỗi số liệu nằm trong cấu hình của đúng một feature sở hữu, không có bảng số dùng chung
Giữ các feature độc lập theo luật của project. Đã loại: một bảng số chung cho cả game.

### config-no-code | AI 7 | Đổi số liệu chỉ cần sửa cấu hình, không cần sửa code
Đã loại: số viết thẳng trong code.

### config-map | AI 7 | Feature này giữ danh sách: số liệu nào, nằm ở feature nào, quan hệ với số nào khác
Để khi đổi một số biết phải xem lại số nào. Đã loại: mỗi feature tự chỉnh số không đối chiếu.

### config-playtest | AI 6 | Đối chiếu số liệu bằng bảng so sánh và chơi thử, không có công cụ mô phỏng trong game
Giữ trong ý tưởng vòng 1. Đã loại: mô phỏng tự động lượt chơi.
