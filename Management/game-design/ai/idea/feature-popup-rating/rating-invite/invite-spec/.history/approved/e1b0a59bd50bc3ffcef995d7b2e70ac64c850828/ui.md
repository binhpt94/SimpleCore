## Phần tử
- invite-text | nhãn | chính | dòng mời "Đánh giá game trên Google Play"
- rate | nút | chính | "Đánh giá": đóng popup, mở trang của game trên Google Play | trạng thái: bật, khoá sau lần chạm đầu
- later | nút | phụ | "Để sau": đóng popup | trạng thái: bật, khoá sau lần chạm đầu

## Tương tác
- rate chạm: đóng popup, rồi mở trang của game trên Google Play (ứng dụng; không được thì trình duyệt)
- later chạm: đóng popup
- nút quay lại của máy: như later chạm
- chạm nền che, chạm ngoài popup: không làm gì
- chạm thêm sau lần chạm đầu: không làm gì

## Flow
- Popup mời mở chồng lên màn đang hiện, có nền che của dịch vụ popup
- rate → popup đóng → game sang Google Play; quay lại game thì về đúng chỗ dưới popup
- later hoặc nút quay lại → popup đóng → về đúng chỗ dưới popup
