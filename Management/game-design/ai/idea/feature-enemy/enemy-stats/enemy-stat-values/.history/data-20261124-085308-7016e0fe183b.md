---
based-on: 504e5ffe01a8
refs: feature-enemy/enemy-reward@1fbbced31e0f, feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-balance/balance-difficulty-curve/break-forecast@38dc08b97bc3, feature-wave/difficulty-scaling/wave-formula@4ec2cc7f93ed
frame: none
---
# Chỉ số gốc và hệ số đợt của quái

Bộ năm chỉ số gốc của từng loại quái ở đợt 1. Lúc quái ra, máu và sát thương nhân hệ số đợt mà `feature-wave` tính và đưa vào; tốc độ, tầm và tiền không nhân. Máu về 0 thì quái bị hạ, thu nhỏ biến mất trong 0.3s.

## Ý cốt lõi

### base-stats | AI 8 | Chỉ số gốc (máu / sát thương / giây mỗi đòn / tầm u / đi u/s): Lính 40/5/1.0/1.0/1.5, Sói 20/3/0.8/1.0/3.0, Khổng lồ 150/15/1.5/1.2/0.8, Cung thủ quái 25/6/1.2/5/1.5, Trùm 400/40/2.0/1.5/0.6
Sói nhanh gấp đôi Lính, Khổng lồ gần gấp 4 máu Lính, Trùm 10 lần máu Lính. Đợt có Trùm khó vọt hơn đợt trước là ngoại lệ có chủ đích.

### wave-multiplier | AI 8 | Lúc ra, máu tối đa và sát thương nhân hệ số đợt do `feature-wave` đưa vào; quái ra rồi thì không đổi nữa
Quái không biết số đợt và không giữ công thức: `feature-wave/difficulty-scaling/wave-formula` giữ tham số, tính hệ số (hiện là 1 + 0.10×(đợt−1)) và đưa vào lúc sinh. Lính với hệ số 1.9 (đợt 10): 76 máu, 9.5 sát thương. Tốc độ đi, giây mỗi đòn và tầm không nhân.

### defeated-values | AI 8 | Máu ≤ 0 thì quái bị hạ ngay khung hình đó: không đánh, không bị nhắm, cho tiền, thu nhỏ về 0 trong 0.3s rồi biến mất
0.3s thu nhỏ chỉ để nhìn; quái không chắn đòn khác trong lúc đó. Đòn vùng không tính quái đã bị hạ.

### no-regen-enemy | AI 7 | Quái không tự hồi máu, máu chỉ giảm
Đã loại: quái tự hồi (người chơi phải hạ dứt điểm sẽ khó đọc).
