---
based-on: 91d4f017d2fb
---
# Luật xếp chồng và chặn bấm

Chốt số popup chồng tối đa, độ tối nền che, việc bấm vào nền che và nút quay lại của điện thoại, và việc popup vẫn chạy khi trận đứng yên.

## Ý cốt lõi

### stack-max | AI 7 | Tối đa 3 popup chồng cùng lúc; mở cái thứ 4 thì bị từ chối và ghi lỗi nêu id
Game cần nhiều nhất 2 (popup tạm dừng và hộp xác nhận bỏ lượt chồng lên). Đã loại: không giới hạn (lỗi mở lặp không bị phát hiện).

### same-id-once | AI 8 | Một id popup chỉ mở một bản mỗi lúc; xin mở id đang mở thì bị từ chối, popup đang mở giữ nguyên
Bấm nút tạm dừng hai lần nhanh không ra hai popup.

### backdrop-dim | AI 7 | Nền che làm tối phần bên dưới 60% (dự kiến); mỗi popup một nền, nền chồng thì tối thêm
Hộp xác nhận chồng lên popup tạm dừng thì popup tạm dừng cũng bị tối đi, người chơi biết đang ở hộp nào.

### backdrop-tap-nothing | AI 8 | Bấm vào nền che không làm gì: không đóng popup, không xuống phần bên dưới

### back-key-nothing | AI 6 | Nút quay lại của điện thoại không đóng popup và không xuống phần bên dưới khi có popup mở
Popup chỉ đóng khi feature gọi yêu cầu (`feature-popup/popup-request`).

### input-instant | AI 7 | Popup hiện và đóng tức thì; popup trên cùng nhận bấm ngay khung hình nó hiện

### runs-while-frozen | AI 8 | Popup hiện, nhận bấm và đóng được cả khi trận đang đứng yên
Popup tạm dừng và popup thua đều mở lúc trận đã dừng.
