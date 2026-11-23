---
based-on: 1fbbced31e0f
---
# Mức tiền khi hạ quái

Tiền mỗi loại quái: Lính 5, Sói 4, Khổng lồ 15, Cung thủ quái 6, Trùm 100; không nhân theo đợt. Tiền vào ví ngay khung hình quái bị hạ, đúng một lần, bất kể ai hạ.

## Ý cốt lõi

### reward-table | AI 8 | Tiền mỗi quái bị hạ: Lính 5, Sói 4, Khổng lồ 15, Cung thủ quái 6, Trùm 100
Đợt 1 có 8 Lính: 40 tiền, cộng thưởng dọn đợt 25 (`feature-wave`) là 65, đủ mua 2 cấp hero đầu (30 và 41).

### reward-no-wave-scale | AI 7 | Tiền mỗi quái không nhân theo đợt
Đợt sau nhiều tiền hơn nhờ đông quái và loại trâu hơn, còn giá nâng cấp tăng theo hàm mũ, nên càng về sau càng phải chọn. Đã loại: tiền nhân hệ số đợt như máu.

### reward-once-instant | AI 8 | Quái bị hạ thì tiền vào `feature-wallet` ngay trong khung hình đó, đúng 1 lần
Nhiều đòn cùng hạ một quái vẫn chỉ cho 1 lần. Quái bị gỡ khi hết lượt mà không bị hạ thì không cho tiền.

### reward-popup-text | AI 6 | Hiện số tiền nhận (ví dụ "+5") ở chỗ quái bị hạ, bay lên 1u trong 0.6s rồi biến mất
Người chơi thấy tiền đến từ đâu mà không phải nhìn ví.
