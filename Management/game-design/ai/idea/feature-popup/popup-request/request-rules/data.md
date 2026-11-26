---
based-on: ff4b78bc4b2c
refs: feature-session-scene/session-steps/participant-order@3cfb8fc5bd71, feature-popup/popup-layer/stack-rules@5f909869fc3d, feature-popup-lose/lose-result@4b9f46cc241a
frame: none
---
# Luật mở, đóng popup và danh sách id

Chốt danh sách id popup của scene Game, việc popup được chuẩn bị sẵn ở bước tải để mở không phải chờ, và cách xử lí các yêu cầu sai: id lạ, đóng popup đã đóng, rời scene khi đang có popup.

## Ý cốt lõi

### id-list | AI 8 | Scene Game có 2 id popup: `PopupLose/Main` (popup thua) và `PopupRating/Main` (popup mời đánh giá)
Tên `Name` do feature sở hữu đặt; đây là tên dự kiến.

### preload | AI 8 | Mọi popup có id trong danh sách của scene được chuẩn bị ở bước tải, nên xin mở là popup hiện ngay trong khung hình đó

### unknown-id | AI 8 | Xin mở id không có trong danh sách của scene thì không hiện gì và ghi lỗi nêu id đó
Không âm thầm bỏ qua.

### close-twice | AI 6 | Xin đóng một popup đã đóng thì bỏ qua, không lỗi
Hai đường đóng cùng lúc (ví dụ bấm nút và lượt kết thúc) không gây lỗi.

### dispose-silent | AI 7 | Rời scene thì mọi popup đang mở bị huỷ, không popup nào báo bấm nút cho feature gọi

### caller-one-press | AI 7 | Feature gọi chỉ xử lí lần bấm nút đầu tiên của mỗi lần mở popup, các lần bấm sau tới khi đóng đều bỏ qua
Chặn bấm đúp làm chơi lại hai lần. Dịch vụ chỉ chuyển lần bấm, việc bỏ qua là của feature gọi.
