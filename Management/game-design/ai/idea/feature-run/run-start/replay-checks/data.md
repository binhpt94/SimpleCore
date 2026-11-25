---
based-on: d386ccae4d88
refs: feature-run/run-start/start-values@04f1a45bba47, feature-run/run-score/score-rules@54a8417a7ccd, feature-popup/popup-request/request-rules@9e6b3fbcf68a, feature-team-select/team-memory@4a24db16dcc6
frame: none
---
# Kiểm tra chơi lại nhiều lượt

Cách kiểm được rằng chơi lại nhiều lượt liền trong scene Game không dồn rác: đầu mỗi lượt không còn gì của lượt trước, và chỉ kỉ lục được lưu khi tắt game.

## Ý cốt lõi

### replay-no-growth | AI 7 | Chơi 10 lượt liền trong scene Game, số view và số việc đang chạy ở đầu lượt thứ 10 bằng ở đầu lượt thứ 1
Kiểm cho việc đặt lại lượt (`feature-run/run-start/start-values`) không để lại quái, hero, hiệu ứng hay popup của lượt trước. Số đếm lấy từ bộ đếm theo feature của `feature-session-scene`.

### popup-cleared-on-reset | AI 7 | Đầu lượt mới không còn popup nào đang mở
Popup thua đã đóng khi bấm nút; nếu vì lỗi còn popup mở thì lượt chơi ghi lỗi nêu id popup (`feature-popup/popup-request/request-rules`), không tự đóng âm thầm.

### only-record-saved | AI 7 | Kỉ lục là thứ duy nhất được ghi lại để còn sau khi tắt game
Kỉ lục do `feature-run/run-score/score-rules` lưu. Đội đã nhớ (`feature-team-select/team-memory`) chỉ sống lúc game đang mở.
