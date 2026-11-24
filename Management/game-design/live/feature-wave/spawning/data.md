---
based-on: 34c92f9b0c18
refs: feature-wave/difficulty-scaling@fed0ac86d57c, feature-wave/wave-clear@b7789c127bb4, glossary#wave@7184fb6d0668, glossary#active-wave@cc772cb923dd
---
# Thả quái

Cách quái của một đợt được đưa vào trận: một đợt là danh sách quái, mỗi con có loại, bên ra và lúc ra. Quái ra rải từ mép trái và mép phải rồi tự đi thẳng vào thành.

## Ý cốt lõi

### wave-list | AI 8 | Một đợt là một danh sách quái, mỗi con ghi loại quái, bên ra và lúc ra
Đây là "nội dung đợt" mà các node khác dùng: độ khó quyết danh sách dài bao nhiêu, gồm loại nào; node này quyết chia bên và chia lúc ra. Đã loại: thả theo thời gian không có danh sách (không biết lúc nào đã ra hết).

### spawn-edges | AI 8 | Quái chỉ ra ở mép trái và mép phải của dải đất, rồi tự đi thẳng vào thành
Ra xong thì cách đi, cách đánh là của `feature-enemy`. Đã loại: quái rơi từ trên xuống, ra giữa đường.

### spawn-both-sides | AI 7 | Đợt nào cũng có quái ở cả hai bên, chia gần đều
Giữ cảm giác bị kẹp ở mọi đợt; người chơi không phải đoán bên nào nguy hơn. Đã loại: cả đợt dồn về một bên; lệch bên theo đợt.

### spawn-spread | AI 7 | Quái của đợt ra rải từng con trong suốt đợt, không ra cả đợt một lúc
Màn hình điện thoại nhìn rõ từng con, hero có lúc đánh xong con trước. Đã loại: cả đợt ra cùng lúc; ra theo cụm lớn.

### spawn-shuffle | AI 6 | Số quái và loại quái cố định theo số đợt, chỉ thứ tự và bên ra là ngẫu nhiên
Cùng một đợt thì độ khó như nhau giữa các lượt, nhưng không lặp y hệt. Đã loại: thứ tự cố định (dễ thuộc lòng); số quái ngẫu nhiên (độ khó khó đoán).
