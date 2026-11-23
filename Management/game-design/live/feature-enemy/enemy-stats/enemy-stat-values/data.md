---
based-on: 504e5ffe01a8
---
# Chỉ số gốc và hệ số đợt của quái

Bộ năm chỉ số gốc của từng loại quái ở đợt 1. Lúc quái ra, máu và sát thương nhân hệ số đợt (1 + 0.10×(đợt-1)); tốc độ, tầm và tiền không nhân. Máu về 0 thì quái bị hạ, thu nhỏ biến mất trong 0.3s.

## Ý cốt lõi

### base-stats | AI 8 | Chỉ số gốc (máu / sát thương / giây mỗi đòn / tầm u / đi u/s): Lính 40/5/1.0/1.0/1.5, Sói 20/3/0.8/1.0/3.0, Khổng lồ 150/15/1.5/1.2/0.8, Cung thủ quái 25/6/1.2/5/1.5, Trùm 400/40/2.0/1.5/0.6
Sói nhanh gấp đôi Lính, Khổng lồ gần gấp 4 máu Lính, Trùm 10 lần máu Lính. Đợt có Trùm khó vọt hơn đợt trước là ngoại lệ có chủ đích.

### wave-multiplier | AI 8 | Lúc ra, máu tối đa và sát thương nhân (1 + 0.10×(đợt-1)); quái ra rồi thì không đổi nữa
Lính đợt 10: 76 máu, 9.5 sát thương. Hệ số đợt do `feature-wave` đưa vào lúc sinh. Tốc độ đi, giây mỗi đòn và tầm không nhân.

### defeated-values | AI 8 | Máu ≤ 0 thì quái bị hạ ngay khung hình đó: không đánh, không bị nhắm, cho tiền, thu nhỏ về 0 trong 0.3s rồi biến mất
0.3s thu nhỏ chỉ để nhìn; quái không chắn đòn khác trong lúc đó. Đòn vùng không tính quái đã bị hạ.

### no-regen-enemy | AI 7 | Quái không tự hồi máu, máu chỉ giảm
Đã loại: quái tự hồi (người chơi phải hạ dứt điểm sẽ khó đọc).
