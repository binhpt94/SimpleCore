---
based-on: 469d9009792c
---
# Lượt chơi

Một lượt chơi từ lúc bấm chơi, qua chọn đội, vào trận, tới khi thành bị phá. Feature này nối các màn hình theo thứ tự, đặt lại mọi thứ khi bắt đầu lượt, tính điểm và giữ kỉ lục.

## Ý cốt lõi

### run-flow | AI 8 | Một lượt đi theo thứ tự: màn bắt đầu, chọn đội, trận, báo thua
Các màn là `feature-home`, `feature-team-select`, trận, `feature-popup-lose`.

### run-reset | AI 8 | Mỗi lượt bắt đầu lại từ đầu: thành, tiền, nâng cấp, đợt đều về ban đầu

### run-score | AI 8 | Điểm của lượt là số đợt đã dọn sạch
Đã loại: điểm theo số quái hạ.

### run-record | AI 7 | Kỉ lục được giữ lại cả khi tắt game; đây là thứ duy nhất giữ qua các lượt

### run-replay | AI 7 | Chơi lại thì về màn chọn đội để đổi đội nếu muốn
Đã loại: vào thẳng trận với đội cũ.

### run-pause | AI 6 | Người chơi tạm dừng được trận đang đánh
Chơi trên điện thoại hay bị ngắt. Popup là `feature-popup-pause`.
