# Nút, panel và công tắc cheat
Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Config → nút: một config, một nút. Config bật thì có nút; tắt thì không có nút, không có panel.
- Nút → panel: một nút mở và đóng một panel.
- Panel → state: nội dung panel là danh sách state ở `feature-cheat-panel/cheat-states`.
- Nút và panel nằm trên mọi màn, mọi popup.

## Định nghĩa
- Không có từ mới cần đưa vào glossary. Config cheat chỉ dùng trong feature này.

## Danh sách
- Hai thứ người test thấy: nút cheat, panel cheat.
- Một thứ người làm game chỉnh: config bật tắt cheat.

## Luật cơ bản
- Config tắt: game giống hệt khi không có feature này.
- Config bật: nút luôn chạm được; panel mở thì chặn chạm xuống dưới.

## Chưa rõ
- Không còn điểm nào chưa rõ ở vòng này.

## Đã tự quyết
- Đọc config một lần lúc khởi động: đơn giản, không cần chỗ bật tắt thứ hai. Đã loại: bật tắt ngay trong game.
- Không dừng trận khi mở panel: panel chỉ để chọn state, chọn xong game chuyển state ngay. Đã loại: dừng trận qua `feature-sim-clock`.
- Nút nằm trên cả popup: người test thường cần đổi state đúng lúc popup đang mở.
