# Cheat: dịch vụ popup

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `popup-over-battle` thì game mở popup `PopupPause/Main` giữa trận qua đúng đường của dịch vụ (`feature-popup/popup-request/request-rules`), để xem nền che, khung popup và việc chặn bấm (`feature-popup/popup-layer/stack-rules`).
- Code cheat nằm trong thư mục `_Services/Popup`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state: một popup trên trận.** Game chỉ mở một popup mỗi lúc (popup tạm dừng hoặc popup thua), nên state chồng hai popup không có thật trong game. Một popup đã có đủ mọi thành phần của dịch vụ: lớp dưới, nền che, khung. Đã loại: state hai popup chồng nhau.
- **Dùng `PopupPause/Main`.** Là popup có thật mở giữa trận; nội dung của nó thuộc node cheat của `feature-popup-pause`, ở đây chỉ vẽ khung.
- **Frame `use` khung popup từ `stack-rules`.** Lớp dưới và nền che phủ toàn màn 1920x1080 (màn của vòng 1), không có số nào khác.
- **Ảnh art-concept của dịch vụ popup (khung, nền che) chỉ đặt ở node này**, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
