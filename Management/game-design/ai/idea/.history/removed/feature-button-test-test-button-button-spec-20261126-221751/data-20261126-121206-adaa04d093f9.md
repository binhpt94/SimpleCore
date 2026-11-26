---
based-on: e1ced593e091
refs: feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c, feature-popup/popup-layer/stack-rules@fa85847e35ee
---
# Nút "Thử" và dòng "Đã bấm: N"

Chốt chữ, cách đếm và các trường hợp biên của nút thử trên màn trận.

## Ý cốt lõi

### button-text | AI 8 | Nút thử ghi chữ "Thử"
Một từ, đọc là biết đó là nút để thử.

### counter-text | AI 8 | Dòng số đếm ghi "Đã bấm: N", N là số nguyên, bắt đầu "Đã bấm: 0"
Ghi đủ số, không rút gọn (ví dụ "Đã bấm: 1250").

### tap-adds-one | AI 8 | Mỗi lần chạm rồi nhả trên nút thử thì N tăng 1 ngay trong khung hình đó
Chạm rồi kéo ra ngoài nút mới nhả thì không tính, như mọi nút của game.

### no-hud-overlap | AI 7 | Nút thử và dòng số đếm không che phần nào của HUD
Chỗ cụ thể do @UIDesigner xếp trong frame; nút có cạnh chạm tối thiểu như các nút HUD ở `feature-hud/hud-layout/layout-spec`.

### bottom-left | AI 8 | Nút thử nằm ở góc trái dưới màn trận, ngay trên thanh dưới của HUD
Người quyết ngày 2026-11-26. Nút nằm trong dải chiến trường, sát mép trên của mép dưới HUD (`feature-hud/hud-layout/layout-spec`) và sát mép trái, không đè lên thanh dưới; chỗ cụ thể do @UIDesigner xếp trong frame. Đã loại: sát mép phải (bản trước), mép trái giữa màn (bản frame đầu).

### counter-beside | AI 7 | Dòng số đếm nằm ngay bên phải nút thử, cùng hàng với nút
Số dài ra thì chữ giãn sang phải, nút không phải dời chỗ; đọc nút rồi số theo chiều đọc. Đã loại: dòng số đếm phía trên nút (cao thêm một hàng, xa thanh dưới hơn).

### blocked-by-popup | AI 7 | Khi có popup mở, chạm vào chỗ nút thử không tính
Lớp popup chặn mọi chạm phía dưới theo `feature-popup/popup-layer/stack-rules`; nút thử không có luật riêng.

### works-when-still | AI 7 | Nút thử vẫn đếm khi trận đứng yên mà không có popup che
Nút không đọc trạng thái trận, nên không có gì để khoá. Thực tế lúc tạm dừng và lúc chờ hồi sinh đều có popup che nên không chạm được.
