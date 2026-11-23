---
based-on: 91d4f017d2fb
refs: feature-popup/popup-request@ff4b78bc4b2c, feature-popup/popup-request/request-rules@9e6b3fbcf68a, feature-run/run-pause@f8e9d49f5079, feature-popup-pause/pause-popup@25d160950007, feature-popup-pause/pause-popup/pause-popup-spec@e65c33624e19
---
# Luật xếp chồng và chặn bấm

Chốt số popup chồng tối đa, độ tối nền che, việc bấm vào nền che và nút quay lại của điện thoại, và việc popup vẫn chạy khi trận đứng yên.

## Ý cốt lõi

### stack-max | AI 7 | Tối đa 3 popup chồng cùng lúc; mở cái thứ 4 thì bị từ chối và ghi lỗi nêu id
Game hiện chỉ mở một popup mỗi lúc (popup tạm dừng hoặc popup thua; xác nhận bỏ lượt nằm trong popup tạm dừng); dư chỗ cho popup sau này. Đã loại: không giới hạn (lỗi mở lặp không bị phát hiện).

### same-id-once | AI 8 | Một id popup chỉ mở một bản mỗi lúc; xin mở id đang mở thì bị từ chối, popup đang mở giữ nguyên
Bấm nút tạm dừng hai lần nhanh không ra hai popup.

### backdrop-dim | AI 7 | Nền che làm tối phần bên dưới 60% (dự kiến); mỗi popup một nền, nền chồng thì tối thêm
Khi có popup chồng lên, popup bên dưới cũng bị tối đi, người chơi biết đang ở popup nào.

### backdrop-tap-nothing | AI 8 | Bấm vào nền che không làm gì: không đóng popup, không xuống phần bên dưới

### back-key-nothing | AI 7 | Khi có popup mở, nút quay lại của điện thoại chỉ chuyển cho feature của popup trên cùng; dịch vụ không tự đóng popup và không chuyển xuống phần bên dưới
Feature của popup tự quyết nút quay lại làm gì (ví dụ `feature-popup-pause/pause-popup/pause-popup-spec`); không khai thì nút không có tác dụng. Popup vẫn chỉ đóng khi feature gọi yêu cầu (`feature-popup/popup-request`).

### input-instant | AI 7 | Popup hiện và đóng tức thì; popup trên cùng nhận bấm ngay khung hình nó hiện

### runs-while-frozen | AI 8 | Popup hiện, nhận bấm và đóng được cả khi trận đang đứng yên
Popup tạm dừng và popup thua đều mở lúc trận đã dừng.
