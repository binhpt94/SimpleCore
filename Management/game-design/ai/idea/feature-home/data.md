---
based-on: ca84fa282d14
by: home-return-from-loss=gpt@r2-1#home-return-from-loss, home-record=gpt@r2-1#home-score-record
---
# Màn bắt đầu

Màn hình đầu tiên khi mở game: cho thấy tên game và kỉ lục, và đưa người chơi vào lượt chơi bằng một nút.

## Ý cốt lõi

### home-start | AI 7 | Màn đầu tiên khi mở game có nút chơi, bấm là sang màn chọn đội

### home-record | AI 7 | Màn bắt đầu hiện kỉ lục điểm do `feature-run` lưu
Người chơi biết thành tích cần vượt ngay khi mở game.

### home-return-from-loss | AI 7 | Màn bắt đầu là đích đến khi người chơi chọn về từ `feature-popup-lose`
Rời màn kết quả thì xem kỉ lục và tự chọn lúc bắt đầu lượt tiếp theo.
