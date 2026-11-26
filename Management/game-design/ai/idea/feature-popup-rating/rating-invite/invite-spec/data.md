---
based-on: f84c671897ff
refs: feature-popup/popup-request/request-rules@f36090b274ea, feature-popup/popup-layer/stack-rules@5f909869fc3d, feature-run/run-flow/flow-rules@593e3d21d6c1, feature-hud/hud-layout/layout-spec@aa79f655e44b, feature-popup-rating/rating-timing/timing-spec@9c59db0047bc
---
# Popup mời: chữ, nút và cách mở Google Play

Popup `PopupRating/Main` có một dòng mời và 2 nút. Chỉ lần chạm đầu có tác dụng; nút quay lại của máy tính như Để sau; popup đóng trước khi mở Google Play.

## Ý cốt lõi

### popup-id | AI 8 | Popup mời có id `PopupRating/Main`
Theo cách đặt id của `feature-popup/popup-request/request-rules`.

### invite-text | AI 8 | Dòng mời ghi "Đánh giá game trên Google Play"
Chữ trung tính: không hỏi có thích game không, không nhắc số sao.

### two-buttons | AI 8 | Có 2 nút: "Đánh giá" là nút chính, "Để sau" là nút phụ
Nút chính đặt trước và nổi hơn nút phụ; cách làm nổi do chuyên gia giao diện quyết.

### rate-flow | AI 8 | Chạm "Đánh giá": nhờ dịch vụ đóng popup, rồi mở trang của game trên ứng dụng Google Play
Trang mở theo mã gói (package) của chính bản game đang chạy, không ghi cứng trong thiết kế.

### store-web-fallback | AI 7 | Máy không mở được ứng dụng Google Play thì mở trang của game trên Google Play bằng trình duyệt
Ví dụ máy không cài Google Play, hoặc đang chạy trong Unity Editor.

### later-flow | AI 8 | Chạm "Để sau": nhờ dịch vụ đóng popup, không làm gì khác

### first-tap-only | AI 7 | Chỉ lần chạm nút đầu tiên có tác dụng; sau đó cả 2 nút không nhận chạm tới khi popup đóng
Theo luật `caller-one-press` của dịch vụ popup.

### back-key-later | AI 7 | Nút quay lại của máy khi popup mời ở trên cùng tính như chạm "Để sau"
Dịch vụ chuyển nút quay lại cho feature của popup trên cùng (`feature-popup/popup-layer/stack-rules`). Đã loại: bỏ qua nút quay lại như popup thua (lời mời không phải lựa chọn bắt buộc).

### backdrop-no-close | AI 7 | Chạm nền che hay chạm ngoài popup không đóng popup mời
Giữ luật chung của dịch vụ; người chơi chọn bằng nút hoặc nút quay lại của máy.

### open-fail-silent | AI 6 | Không mở được cả ứng dụng lẫn trình duyệt thì không báo lỗi cho người chơi, chỉ ghi lỗi trong log
Popup đã đóng trước đó, người chơi vẫn ở trong game như sau khi chạm "Để sau".
