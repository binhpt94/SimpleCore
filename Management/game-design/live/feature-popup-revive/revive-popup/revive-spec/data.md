---
based-on: 9564590ae47a
refs: feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@ebefd7f8b116, feature-castle/castle-core/castle-body-values@b648ec300023
---
# Chữ, nút và trạng thái của popup hồi sinh

Popup hồi sinh dùng khung chuẩn của dịch vụ popup, có tiêu đề, một dòng nói thành hồi 30% máu, dòng chờ quảng cáo, và hai nút cùng hàng với nút của popup thua.

## Ý cốt lõi

### title-text | AI 7 | Tiêu đề là "Thành đã bị phá"
Giống tiêu đề popup thua: người chơi biết ngay chuyện gì vừa xảy ra.

### gain-text | AI 7 | Dòng hồi sinh ghi "Xem quảng cáo để thành hồi 30% máu và chơi tiếp"
Số 30% lấy từ nhánh thân thành; đổi số ở đó thì sửa chữ ở đây.

### watch-button | AI 7 | Nút chính "Xem quảng cáo" rộng 400 px; nút "Kết thúc lượt" rộng 260 px; cùng hàng y=540 trong khung
Cùng chỗ với hai nút của popup thua nên tay người chơi quen vị trí.

### wait-state | AI 7 | Chờ quảng cáo: hiện dòng "Đang chờ quảng cáo..." ở y=340 trong khung, hai nút mờ và không nhận chạm
Hết chờ (có kết quả) thì popup đóng ngay, không quay về trạng thái chờ chọn.

### one-tap-each | AI 7 | Mỗi lần popup mở, chỉ lần chạm nút đầu tiên có tác dụng
Chạm "Xem quảng cáo" sang trạng thái chờ; chạm "Kết thúc lượt" đóng popup.

### back-key-nothing | AI 7 | Nút quay lại của máy không làm gì khi popup hồi sinh mở
Không có lối thoát thứ ba; tránh thua vì bấm nhầm.

### popup-id | AI 7 | Id popup là `PopupRevive/Main`, hiện qua dịch vụ popup
