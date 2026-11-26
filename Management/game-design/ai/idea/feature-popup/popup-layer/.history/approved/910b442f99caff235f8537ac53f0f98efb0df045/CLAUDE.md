# Lớp popup: che, chặn bấm và xếp chồng

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một scene có một lớp popup, nằm trên mọi view của scene (màn chơi, `feature-hud`).
- Một lớp popup chứa không, một hoặc nhiều popup xếp chồng.
- Mỗi popup có một nền che riêng ngay phía sau nó.
- Việc mở, đóng từng popup: `feature-popup/popup-request`.

## Định nghĩa
- `glossary#backdrop`

## Danh sách
- Thứ tự từ dưới lên: màn chơi → HUD → (nền che → popup) lặp lại theo số popup đang mở.

## Luật cơ bản
- Có popup mở thì mọi thứ dưới nền che của nó không nhận bấm.
- Chỉ popup trên cùng nhận bấm.
- Đóng popup trên cùng thì popup ngay dưới thành popup trên cùng.
- Không có popup nào mở thì lớp popup không chặn gì.

## Đã tự quyết
- **Xếp chồng thay vì một popup một lúc.** Quy tắc chung đơn giản, không cần hàng chờ. Đã loại: một popup một lúc có hàng chờ; popup mới thay popup cũ.
- **Nền che làm tối.** Người chơi phổ thông thấy rõ đang có popup. Đã loại: nền trong suốt.
- **Popup trên cả HUD.** Popup là thứ cần người chơi chọn ngay; HUD không được đè.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ popup tạm dừng khỏi lí do xếp chồng.

## Chưa rõ
- Độ tối của nền che: vòng 4.
