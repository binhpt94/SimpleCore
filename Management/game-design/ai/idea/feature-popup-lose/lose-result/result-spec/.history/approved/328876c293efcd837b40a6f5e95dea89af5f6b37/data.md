---
based-on: 4b9f46cc241a
refs: feature-run/run-score@691534d6dde7, feature-run/run-score/score-rules@2ff455851b82, feature-popup-lose/lose-actions/actions-spec@a6b5a02909a8, feature-popup/popup-layer/stack-rules@5f909869fc3d, feature-hud/hud-layout/layout-spec@aa79f655e44b
---
# Chữ kết quả trên popup thua

Popup thua có 3 dòng chữ luôn hiện (tiêu đề, điểm, kỉ lục) và 1 dòng "Kỉ lục mới!" chỉ hiện khi phá kỉ lục. Mọi số đọc từ `feature-run/run-score`.

## Ý cốt lõi

### result-title-text | AI 7 | Tiêu đề popup là "Thành đã bị phá"

### result-score-text | AI 8 | Dòng điểm hiện "Trụ được N đợt", N là điểm đã chốt của lượt
Thua ở đợt 1 thì "Trụ được 0 đợt".

### result-record-text | AI 7 | Dòng kỉ lục hiện "Kỉ lục: M đợt", M là kỉ lục đã cập nhật với lượt vừa xong

### result-new-record-line | AI 7 | Cờ kỉ lục mới bật thì có thêm dòng "Kỉ lục mới!" và dòng này phóng to 1.0 → 1.15 → 1.0 lặp lại mỗi 1 giây
Dòng này nằm trên một dải nền nổi bật cùng cỡ (400x70 px), để lượt phá kỉ lục trông vui hơn hẳn. Cờ tắt thì không có cả dòng lẫn dải nền, không có câu nào thay vào.

### result-read-only | AI 8 | Popup chỉ đọc điểm, kỉ lục, cờ một lần lúc mở; không đổi số nào
