---
based-on: 04fa62031f0e
---
# Lớp popup: che, chặn bấm và xếp chồng

Popup luôn nằm trên mọi màn của scene, có nền che chặn bấm bên dưới. Nhiều popup mở thì xếp chồng; chỉ popup trên cùng nhận bấm.

## Ý cốt lõi

### popup-on-top | AI 8 | Popup luôn hiện trên mọi màn của scene, kể cả HUD
Đã loại: popup nằm chung lớp với HUD (dễ bị HUD đè).

### popup-backdrop | AI 7 | Mỗi popup có nền che phía sau, chặn mọi lần bấm xuống phần bên dưới
Nền làm tối phần bên dưới để người chơi biết đang có popup.

### popup-stack | AI 7 | Nhiều popup cùng mở thì xếp chồng, popup mở sau nằm trên
Đã loại: chỉ một popup một lúc, popup sau phải chờ (người chơi không thấy popup mới khi cần).

### top-only-input | AI 7 | Chỉ popup trên cùng nhận bấm; đóng nó thì popup bên dưới nhận bấm lại
