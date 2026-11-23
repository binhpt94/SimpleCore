---
based-on: 6230c29b8b00
refs: feature-run/run-score@6f1909d90f98, feature-run/run-flow@db634f5dac0b, feature-popup-lose/lose-actions@664d10d83337, glossary#score@6f4bf7c75284, glossary#record@d05567cd8426, glossary#new-record@526898f81890
---
# Kết quả lượt

Phần trên của popup thua: điểm của lượt vừa xong, kỉ lục, và lời báo khi vừa phá kỉ lục.

## Ý cốt lõi

### result-score | AI 8 | Popup thua hiện điểm của lượt: số đợt đã dọn sạch
Điểm lấy từ `feature-run/run-score`, đã chốt lúc lượt kết thúc. Popup không tính điểm.

### result-record | AI 7 | Popup thua hiện kỉ lục ngay dưới điểm
Kỉ lục là giá trị sau khi đã cập nhật với lượt vừa xong. Để người chơi so điểm mình với mục tiêu.

### result-new-record | AI 7 | Lượt vừa phá kỉ lục thì popup báo rõ "kỉ lục mới" và trông vui hơn hẳn
Dựa vào cờ phá kỉ lục của `feature-run/run-score`. Không phá kỉ lục thì không có lời báo. Đã loại: luôn hiện câu động viên (làm nhạt lúc phá kỉ lục thật).

### result-title | AI 7 | Popup có tiêu đề cho biết thành đã bị phá
Người chơi hiểu ngay vì sao lượt kết thúc.
