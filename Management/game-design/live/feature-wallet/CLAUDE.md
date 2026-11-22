# Tiền

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Thấy số tiền tăng khi hạ quái và biết ngay mình mua được gì.
- Phần thưởng cụ thể cho mỗi đợt trụ được.

## Cốt lõi
- Một số tiền, không âm, luôn hiện.
- Cộng khi hạ quái và dọn sạch đợt.
- Trừ khi mua nâng cấp, kể cả đang đánh.
- Đặt lại mỗi lượt.

## Liên quan
- `feature-enemy`: tiền rơi khi quái bị hạ.
- `feature-wave`: thưởng dọn sạch đợt.
- `feature-upgrade`: tiêu tiền.
- `feature-run`: đặt lại tiền khi bắt đầu lượt.

## Đã tự quyết
- **Một loại tiền.** Đủ cho vòng lặp vòng 1, dễ hiểu. Đã loại: đá quý hay tiền lâu dài (không có tiến trình lâu dài).
- **Key `feature-wallet`.** Trùng tên feature Wallet đã có trong project, để phiên code dễ nối. Đã loại: `feature-coin`.
- **Tiền vào ví ngay khi quái bị hạ, không cần nhặt.** Người chơi đã bận bấm kĩ năng và nâng cấp. Đã loại: chạm để nhặt tiền.

## Chưa rõ
- Không có điểm chặn.
