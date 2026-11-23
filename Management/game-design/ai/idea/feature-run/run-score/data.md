---
based-on: d8a90fd4adc0
refs: feature-run/run-flow@db634f5dac0b, feature-wave/wave-clear@b7789c127bb4, glossary#wave-clear@89a01d398d04, glossary#score@6f4bf7c75284, glossary#record@d05567cd8426, glossary#new-record@526898f81890
---
# Điểm và kỉ lục

Điểm của một lượt là số đợt đã dọn sạch; kỉ lục là điểm cao nhất từng đạt và được lưu lại khi tắt game.

## Ý cốt lõi

### score-def | AI 8 | Điểm của lượt bằng số đợt đã dọn sạch; đợt đang đánh dở không tính
Mỗi lần `feature-wave` báo dọn sạch một đợt thì điểm tăng một bậc. Đã loại: điểm theo số quái hạ, điểm theo thời gian trụ.

### score-final | AI 8 | Điểm được chốt lúc lượt kết thúc, dù thua hay bỏ lượt
Hai cách kết thúc tính điểm giống nhau. Đã loại: bỏ lượt thì mất điểm.

### record-def | AI 8 | Kỉ lục là điểm cao nhất trong mọi lượt đã chơi
Chốt điểm xong, điểm lớn hơn kỉ lục cũ thì điểm đó thành kỉ lục mới; bằng hoặc thấp hơn thì kỉ lục giữ nguyên.

### record-save | AI 7 | Kỉ lục được lưu lại, tắt game mở lại vẫn còn
Đây là thứ duy nhất lưu khi tắt game. Đã loại: chỉ nhớ trong lúc game đang mở.

### record-new-flag | AI 7 | Lượt vừa kết thúc biết mình có phá kỉ lục hay không
Cờ "phá kỉ lục" để `feature-popup-lose` báo cho người chơi.
