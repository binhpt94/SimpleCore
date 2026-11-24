---
based-on: 53af9956f851
by: pause-freeze=gpt@r2-1#pause-preserve-state, pause-run-ownership=gpt@r2-1#pause-run-ownership
---
# Popup tạm dừng

Popup hiện khi người chơi bấm tạm dừng: trận đứng yên tới khi tiếp tục, hoặc bỏ lượt về màn bắt đầu.

## Ý cốt lõi

### pause-freeze | AI 7 | Tạm dừng giữ nguyên trạng thái trận cho tới khi người chơi tiếp tục hoặc bỏ lượt
Rời mắt rồi quay lại vẫn đúng thế trận lúc bấm dừng; popup vẫn nhận lựa chọn.

### pause-run-ownership | AI 7 | Popup chuyển lựa chọn tiếp tục hoặc bỏ lượt cho `feature-run` xử lí
Popup là nơi người chơi chọn; kết thúc lượt, tính điểm và xét kỉ lục thuộc `feature-run`.
