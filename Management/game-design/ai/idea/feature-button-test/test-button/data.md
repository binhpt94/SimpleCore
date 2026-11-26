---
based-on: 8cf3be942238
---
# Nút thử và số lần bấm

Màn trận có một nút thử và một dòng chữ đếm số lần đã bấm. Mỗi lần chạm nút, số đếm tăng một.

## Ý cốt lõi

### one-button | AI 8 | Có đúng một nút thử
Một nút là đủ để thử trọn quy trình. Đã loại: nhiều nút, mỗi nút một kiểu phản hồi.

### counter-feedback | AI 8 | Phản hồi là số lần bấm hiện cạnh nút, mỗi lần chạm tăng một
Số đếm kiểm được bằng mắt và bằng test: chạm N lần thì thấy N. Đã loại: chỉ nháy nút (khó chụp ảnh để kiểm, không đếm được).

### counter-per-screen | AI 7 | Số đếm về 0 mỗi lần màn trận mở, không lưu lại
Không lưu gì thì feature không phải đụng tới dữ liệu lưu của game.

### shown-with-battle | AI 7 | Nút và số đếm hiện khi màn trận hiện, ẩn theo màn trận
