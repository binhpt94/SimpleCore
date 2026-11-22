---
based-on: cf568e2870fe
---
# Lượt chơi

Một lượt chơi từ lúc bấm chơi, qua màn chọn đội, tới lúc thành hết máu. Feature này lo các màn hình đầu và cuối, đặt lại mọi thứ khi bắt đầu lượt, tính điểm và lưu kỉ lục.

## Ý cốt lõi

### run-flow | AI 8
Thứ tự màn hình: màn bắt đầu (tên game, kỉ lục, nút chơi) → màn chọn đội (`feature-hero`) → trận → màn thua.

### run-reset | AI 8
Bắt đầu lượt: máu thành, tiền, mọi cấp nâng cấp, số đợt đều về ban đầu; đội hero là đội vừa chọn.

### run-score | AI 8
Điểm của lượt là số đợt đã dọn sạch; đợt đang đánh dở không tính.

### run-record | AI 7
Kỉ lục là điểm cao nhất, lưu lại sau khi tắt game. Đây là thứ duy nhất giữ qua các lượt.

### run-lose-screen | AI 7
Thành còn 0 máu: trận dừng, hiện màn thua với điểm lượt này, kỉ lục, nút chơi lại (về màn chọn đội) và nút về màn bắt đầu.

### run-pause | AI 6
Trong trận có nút tạm dừng: mọi thứ đứng yên, không bấm kĩ năng hay mua nâng cấp được cho tới khi tiếp tục.
