---
based-on: 09ba60c454c5
---
# Thông tin trận

Ba thông tin luôn hiện trong trận: máu thành, số tiền đang có và đợt đang đánh. HUD chỉ hiện, không tính.

## Ý cốt lõi

### status-castle-hp | AI 8 | Máu thành hiện bằng một thanh máu: phần còn lại so với lúc đầy
Liếc là biết an toàn hay sắp nguy. Thanh ngắn lại khi thành mất máu. Đã loại: chỉ hiện con số (khó đọc nhanh).

### status-money | AI 8 | Số tiền hiện bằng một con số kèm biểu tượng tiền, đổi ngay khi nhận hay tiêu
Để người chơi biết mua được gì; trạng thái từng nút mua là việc của `feature-hud-upgrade`.

### status-wave | AI 7 | HUD hiện số thứ tự của đợt đang đánh
Đợt đang đánh là đợt thứ mấy trong lượt, lấy từ `feature-wave`. Khác với điểm (số đợt đã dọn, `feature-run/run-score`). Đã loại: hiện điểm thay số đợt (người chơi quen nhìn "đợt mấy").

### status-read-only | AI 8 | Mọi thông tin HUD đọc từ feature chủ của nó và đổi ngay khi feature đó đổi
Máu từ `feature-castle`, tiền từ `feature-wallet`, đợt từ `feature-wave`. HUD không giữ hay tính số nào.
