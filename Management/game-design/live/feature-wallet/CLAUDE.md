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
- **Key `feature-wallet`, `code.json` là `Wallet` (2026-11-23).** Tên theo luật vòng 2 (bỏ `feature-`, viết hoa chữ đầu), trùng thư mục code `Wallet` đã có trong project.
- **Bỏ "luôn hiện trên màn hình" khỏi ý.** Hiển thị là việc của `feature-hud`.

## Chưa rõ
- Không có điểm chặn.
