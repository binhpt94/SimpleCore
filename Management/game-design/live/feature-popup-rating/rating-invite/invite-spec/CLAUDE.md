# Popup mời: chữ, nút và cách mở Google Play
Dữ liệu cốt lõi: [data.md](data.md) · Phần tử giao diện: [ui.md](ui.md)

## Làm gì
- Đăng kí và mở popup `PopupRating/Main` qua dịch vụ popup (`feature-popup/popup-request/request-rules`). Lúc nào mở là việc của nhánh `rating-timing`.
- Điền dòng mời và chữ 2 nút theo bảng `InviteParams`.
- Chạm "Đánh giá": nhờ dịch vụ đóng popup, rồi mở trang của game trên Google Play: thử ứng dụng Google Play trước, không được thì mở trang web của game trên Google Play. Cả hai dùng mã gói của bản game đang chạy.
- Chạm "Để sau" hoặc nút quay lại của máy: nhờ dịch vụ đóng popup.
- Chỉ xử lí lần chạm đầu của mỗi lần mở (luật `caller-one-press` của dịch vụ).

## Thông số
- Bảng `InviteParams` (`params`) trong `.excel/`: `invite_text` dòng mời, `rate_label` chữ nút chính, `later_label` chữ nút phụ, `store_open_order` thứ tự nơi mở trang Google Play.
- Chữ hiện bằng font chung của game (ý `art-ui-text-font` của node gốc). Art ở IDEA siêu cơ bản: khung và nút đơn giản, không có hình minh hoạ.

## Thông số theo giai đoạn
- Cột `idea`, `prototype`, `full` của bảng `InviteParams`. Mọi dòng giữ như IDEA: popup mời không đổi theo giai đoạn.

## Trường hợp biên
- **Chạm 2 nút gần như cùng lúc:** chỉ lần chạm đầu tính.
- **Người chơi rời game khi popup đang mở (không chạm gì):** popup vẫn mở khi quay lại; không tính là đã chọn.
- **Chạm "Đánh giá" rồi quay lại game:** popup đã đóng; người chơi về đúng chỗ bên dưới popup.
- **Máy không có ứng dụng Google Play, hoặc chạy trong Unity Editor:** mở trang web của game trên Google Play bằng trình duyệt.
- **Không mở được cả hai:** popup đã đóng; không báo lỗi cho người chơi, chỉ ghi lỗi trong log.
- **Game chưa phát hành trên Google Play (trang chưa tồn tại):** vẫn mở theo mã gói; Google Play báo không tìm thấy trang. Không phải việc của game.
- **Scene bị huỷ khi popup đang mở:** dịch vụ huỷ popup và không báo lần chạm nào (`dispose-silent`); lần mời đó coi như xong.

## Chưa rõ
- Không còn. Lúc mời nằm ở `feature-popup-rating/rating-timing/timing-spec`; id `PopupRating/Main` đã có trong danh sách của `feature-popup/popup-request/request-rules` (người quyết 2026-11-27).

## Đã tự quyết
- **Nút quay lại của máy tính như "Để sau".** Lời mời không bắt buộc trả lời, nên cho người chơi thoát bằng thao tác quen tay. Đã loại: bỏ qua nút quay lại như popup thua.
- **Đóng popup trước rồi mới mở Google Play.** Quay lại game không còn thấy lời mời, và không phụ thuộc vào việc Google Play có mở được hay không.
- **Không ghi cứng đường dẫn trang Google Play.** Dùng mã gói của bản đang chạy, nên đổi tên gói không phải sửa thiết kế.
