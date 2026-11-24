---
based-on: 47e9a9467dc9
refs: glossary#scene@c92d4e8713a9, glossary#screen@939e5a4868a2
---
# Danh sách scene và đường đi

Game có hai scene: Loading lúc mở game và Game nơi diễn ra mọi màn chơi. Mở game luôn đi Loading rồi sang Game; sau đó người chơi ở lại scene Game.

## Ý cốt lõi

### two-scenes | AI 8 | Game có đúng hai scene: Loading và Game
Loading chỉ lo tải lúc mở game; Game chứa mọi thứ người chơi thao tác. Đã loại: mỗi màn một scene (thêm chờ tải giữa các màn, trái với lượt chơi ngắn).

### boot-path | AI 8 | Mở game thì vào scene Loading trước, tải xong tự sang scene Game
Người chơi không bấm gì ở Loading. Đã loại: màn Loading có nút "chạm để chơi" (thừa, màn bắt đầu đã có nút chơi).

### game-holds-screens | AI 8 | Màn bắt đầu, chọn đội, trận và các popup cùng nằm trong scene Game
Đổi màn là đổi trong scene, không chuyển scene. Chơi lại nhanh, không màn tải giữa các lượt. Đã loại: tách trận thành scene riêng.

### no-return-loading | AI 7 | Đã vào scene Game thì không quay lại scene Loading
Về màn bắt đầu hay chơi lại đều là đổi màn trong scene Game.
