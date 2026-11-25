---
based-on: 6181a7c21bf0
by: lose-score-consistency=gpt@r2-1#lose-score-consistency
---
# Popup thua

Popup hiện khi thành bị phá: báo kết quả lượt chơi và cho chơi lại.

## Ý cốt lõi

### lose-score-consistency | AI 7 | Popup thua hiện điểm lượt và kỉ lục do `feature-run` cấp, và báo khi phá kỉ lục
Điểm và kỉ lục cùng một thước đo, đúng thứ `feature-run` dùng để xét kỉ lục.

### lose-actions | AI 8 | Popup thua cho chơi lại (về màn chọn đội) hoặc về màn bắt đầu
Hồi sinh thành bằng quảng cáo đã được đề nghị trước đó ở `feature-popup-revive`; popup thua chỉ hiện khi lượt đã thật sự thua.
