# Tiền

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Thấy tiền tăng khi hạ quái và biết ngay mình mua được gì.
- Phần thưởng cụ thể cho mỗi đợt trụ được.

## Liên quan
- `feature-enemy`: tiền khi quái bị hạ.
- `feature-wave`: thưởng dọn sạch đợt.
- `feature-upgrade`: tiêu tiền.
- `feature-run`: đặt lại tiền khi bắt đầu lượt.
- `feature-hud`: hiện số tiền.

## Đã tự quyết
- **Một loại tiền.** Đủ cho vòng lặp vòng 1, dễ hiểu.
- **Key `feature-wallet`.** Có thể khớp với thư mục code `Wallet` đã có trong project; tên thư mục chính xác chờ người xác nhận trước khi ghi `code.json`.
- **Bỏ "luôn hiện trên màn hình" khỏi ý.** Hiển thị là việc của `feature-hud`.

## Chưa rõ
- Tên thư mục code để ghi `code.json`: chờ người xác nhận.
