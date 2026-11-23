---
based-on: d2e78e0c80dc
refs: feature-wallet/balance-spend/wallet-rules@1367fdb78322
---
# Mức tiền thu

Bảng tiền hạ từng loại quái và công thức thưởng dọn đợt, cùng các trường hợp quái gục mà có hay không có tiền.

## Ý cốt lõi

### kill-table | AI 8 | Tiền hạ quái: Lính 5, Sói 4, Cung thủ quái 6, Khổng lồ 15, Trùm 100 tiền
Mức tiền không nhân theo số đợt; đợt sau cho nhiều tiền hơn vì có nhiều quái hơn. Mức tiền là số liệu của từng loại quái trong `feature-enemy`, ví chỉ cộng.

### kill-once | AI 8 | Mỗi con quái cho tiền đúng một lần, lúc máu của nó về 0
Nhiều đòn trúng cùng lúc làm nó gục vẫn chỉ một lần cộng. Ai ra đòn cuối (hero, đòn tự bắn, kĩ năng thành) cũng như nhau.

### kill-only-defeat | AI 7 | Quái biến mất mà không bị hạ thì cho 0 tiền
Ví dụ quái bị xoá khi lượt kết thúc.

### wave-formula | AI 8 | Thưởng dọn đợt = 20 + 5 × số đợt vừa dọn, tính bằng tiền
Đợt 1 được 25, đợt 2 được 30, đợt 10 được 70. Đợt đánh số từ 1.

### wave-once | AI 8 | Mỗi đợt cho thưởng đúng một lần, lúc con quái cuối cùng của đợt gục
Tiền của con quái cuối cộng trước, rồi tới thưởng dọn đợt.

### income-same-frame | AI 7 | Tiền vào số dư trong cùng khung hình quái gục hoặc đợt được dọn
Không có độ trễ, không có tiền rơi phải nhặt.
