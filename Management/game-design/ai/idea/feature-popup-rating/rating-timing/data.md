---
based-on: 4ddfae0df143
refs: feature-run/run-score@691534d6dde7
---
# Lúc mời

Lời mời hiện chồng lên popup thua khi lượt vừa phá kỉ lục, mỗi lần mở game nhiều nhất một lần. Feature chỉ nhớ trong lúc game đang mở, không lưu gì xuống máy.

## Ý cốt lõi

### trigger-new-record | AI 8 | Lượt vừa chốt có cờ "kỉ lục mới" bật thì lượt đó được mời
Cờ do `feature-run/run-score` cấp. Lượt không phá kỉ lục thì không mời.

### show-over-lose | AI 8 | Lời mời mở chồng lên popup thua đang hiện; popup thua nằm dưới, không nhận bấm tới khi lời mời đóng

### back-to-lose | AI 8 | Lời mời đóng thì popup thua nhận bấm lại, người chơi chọn Chơi lại hay Về màn bắt đầu như thường

### once-per-launch | AI 8 | Mỗi lần mở game có nhiều nhất một lần mời; tắt game mở lại thì được mời lại
"Lần mời" tính khi popup mời đã hiện, dù người chơi chọn gì.

### memory-only | AI 8 | Việc đã mời chỉ nhớ trong lúc game đang mở, không lưu xuống máy
Người quyết 2026-11-27.
