# Điểm và kỉ lục

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt có một điểm. Cả game có một kỉ lục.
- `feature-wave` báo mỗi lần dọn sạch một đợt; feature này đếm thành điểm.
- `feature-run/run-flow` báo lượt kết thúc; feature này chốt điểm và cập nhật kỉ lục.
- Bên đọc: `feature-home` (kỉ lục), `feature-popup-lose` (điểm, kỉ lục, cờ phá kỉ lục).
- Số đợt HUD hiện (`feature-hud`) là đợt đang đánh, lấy từ `feature-wave`; khác với điểm.

## Định nghĩa
- **Đợt đã dọn sạch**: đợt mà `feature-wave` đã báo dọn sạch (`feature-wave/wave-clear`).
- **Điểm**: số đợt đã dọn sạch trong lượt.
- **Kỉ lục**: điểm cao nhất của mọi lượt từng chơi trên máy này.
- **Phá kỉ lục**: điểm vừa chốt lớn hơn hẳn kỉ lục trước lượt đó.

## Danh sách
- Điểm của lượt.
- Kỉ lục.
- Cờ phá kỉ lục của lượt vừa xong.

## Luật cơ bản
- Lượt mới bắt đầu thì điểm về không.
- Điểm chỉ tăng, không giảm trong lượt.
- Điểm chốt khi lượt kết thúc, cả khi thua lẫn khi bỏ lượt.
- Kỉ lục chỉ đổi khi điểm chốt lớn hơn nó.
- Kỉ lục được lưu ngay khi đổi.

## Đã tự quyết
- **Bằng kỉ lục không tính là phá kỉ lục.** Báo "phá kỉ lục" chỉ khi vượt thật.
- **Lưu kỉ lục ngay khi đổi.** Người chơi điện thoại hay tắt ngang; lưu lúc tắt game dễ mất.
- **Một kỉ lục cho cả game, không theo đội.** Vòng 1 chỉ nói lưu kỉ lục. Đã loại: kỉ lục riêng cho từng đội.

## Chưa rõ
- Không có điểm chặn.
