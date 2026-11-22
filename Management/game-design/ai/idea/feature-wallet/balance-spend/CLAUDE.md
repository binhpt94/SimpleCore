# Số dư và chi tiêu

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT lượt chơi có MỘT số dư.
- NHIỀU nguồn thu cộng vào MỘT số dư (xem `feature-wallet/income`).
- MỘT nơi tiêu: `feature-upgrade` (xem `feature-upgrade/upgrade-purchase`).
- `feature-run` ra lệnh đặt lại số dư khi bắt đầu lượt.
- `feature-hud`, `feature-hud-upgrade` theo dõi số dư để hiển thị.

## Định nghĩa
- **Số dư**: số tiền người chơi đang có, số nguyên, không âm.
- **Số tiền ban đầu**: số dư lúc lượt bắt đầu; là số cấu hình của ví.
- **Trừ tiền**: một yêu cầu kèm giá; kết quả là "được" (đã trừ) hoặc "không đủ" (không đổi gì).

## Danh sách
- Thao tác lên số dư: cộng, trừ (khi đủ), đặt lại về số tiền ban đầu.

## Luật cơ bản
- Cộng: số dư tăng đúng bằng lượng được cộng.
- Trừ: số dư lớn hơn hoặc bằng giá thì trừ và báo "được"; nhỏ hơn thì báo "không đủ", số dư giữ nguyên.
- Đặt lại: số dư về số tiền ban đầu.
- Sau mỗi thao tác làm số dư đổi, báo số mới cho bên theo dõi.

## Đã tự quyết
- **Ví không biết món mua.** Giữ ví độc lập; giá và món hàng là của `feature-upgrade`. Đã loại: ví giữ bảng giá nâng cấp.
- **Ví báo khi số đổi, không để các bên hỏi liên tục.** Hai HUD đều cần cập nhật ngay khi tiền vào hay ra.
- **Đặt lại do `feature-run` ra lệnh.** Theo quyết định chung: `feature-run` giữ việc đặt lại mọi thứ đầu lượt. Đã loại: ví tự phát hiện lượt mới.
- **Số nguyên.** Tiền hiện trên màn hình nên không có phần lẻ.

## Chưa rõ
- Không có điểm chặn.
