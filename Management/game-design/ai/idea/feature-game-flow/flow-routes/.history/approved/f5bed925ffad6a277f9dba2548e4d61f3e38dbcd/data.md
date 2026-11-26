---
based-on: 98a7977ed8e0
refs: glossary#screen@939e5a4868a2
---
# Các màn chính và đường đi giữa chúng

Scene Game có ba màn chính: màn bắt đầu, màn chọn đội, màn trận; mỗi lúc hiện đúng một. Node này chốt ai gửi yêu cầu đổi màn, yêu cầu nào dẫn tới màn nào, và khoá trong lúc đổi.

## Ý cốt lõi

### three-screens | AI 7 | Scene Game có đúng ba màn chính: bắt đầu, chọn đội, trận; mỗi lúc hiện đúng một
Popup thua không phải màn chính; nó mở trên màn trận.

### first-is-home | AI 7 | Scene Game sẵn sàng thì màn đầu tiên là màn bắt đầu, mở một lần cho mỗi lần tải Game

### direct-senders | AI 7 | Màn bắt đầu và màn chọn đội gửi yêu cầu thẳng cho điều hướng màn
"Chơi", "Vào trận" và "Quay lại" là ba yêu cầu này.

### leave-via-run | AI 7 | Lựa chọn rời trận (chơi lại, về màn bắt đầu) tới điều hướng màn qua lượt chơi, sau khi lượt chơi đã dọn trận
Popup thua báo lượt chơi; lượt chơi kết thúc lượt, dọn trận rồi chuyển lựa chọn.

### battle-handover | AI 7 | "Vào trận" giao đội đã xác nhận cho lượt chơi; màn trận chỉ hiện khi lượt chơi báo trận đã dựng xong
Dựng lỗi thì ở lại màn chọn đội với đội vừa xác nhận, cho bấm lại.

### one-change-at-a-time | AI 7 | Trong lúc đang đổi màn, mọi yêu cầu đổi màn mới bị bỏ qua
Khoá tới khi màn đích đã hiện, hoặc tới khi đã ở lại màn nguồn sau lỗi.

### back-keeps-memory | AI 6 | "Quay lại" từ màn chọn đội về màn bắt đầu không đổi đội được nhớ
Đội được nhớ chỉ đổi khi bấm "Vào trận".

### takes-over-run-routes | AI 6 | Bảng đường đi giữa các màn chính chuyển từ nhánh luồng màn của lượt chơi về đây
Lượt chơi giữ phần trong trận: dựng, dừng, kết thúc, dọn trận, điểm.
