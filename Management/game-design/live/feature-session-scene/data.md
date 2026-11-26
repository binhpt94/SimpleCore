---
based-on: ca84fa282d14
by: session-lifecycle=gpt@r2-1#lifecycle-scope
gpt-dropped: r2-1#cleanup-scope=cleanup-scope
---
# Vòng đời scene

Dịch vụ hạ tầng lo vòng đời của một scene: tải đủ thứ cần, cho các feature chạy, rồi dọn sạch khi rời scene.

## Ý cốt lõi

### session-lifecycle | AI 7 | Điều phối việc chuẩn bị, chạy và kết thúc phần của các feature gắn với một scene
Ba bước: tải, chạy, huỷ. Chuyển giữa các scene là việc của `feature-scene-flow`.

### session-clean | AI 7 | Rời scene thì mọi thứ của scene đó được dọn sạch, lượt sau bắt đầu sạch sẽ
Khớp với việc mỗi lượt chơi bắt đầu lại từ đầu (vòng 1).
