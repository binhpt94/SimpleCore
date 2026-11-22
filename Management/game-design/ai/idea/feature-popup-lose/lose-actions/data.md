---
based-on: 6230c29b8b00
---
# Nút của popup thua

Popup thua có hai nút: chơi lại (về màn chọn đội) và về màn bắt đầu. Không có cách nào khác để đóng popup.

## Ý cốt lõi

### actions-two | AI 8 | Popup thua có đúng hai nút: chơi lại và về màn bắt đầu
Đã loại: hồi sinh thành, xem quảng cáo để chơi tiếp (không có quảng cáo hay tiền lâu dài).

### actions-replay-main | AI 7 | Nút chơi lại là nút chính, to và nổi hơn nút về màn bắt đầu
Vòng lặp vòng 1 là thua rồi chơi lại ngay với đội khác.

### actions-relay | AI 8 | Bấm nút thì popup đóng và `feature-run/run-flow` chuyển màn
Chơi lại sang màn chọn đội; về màn bắt đầu sang màn bắt đầu. Popup không tự chuyển màn.

### actions-only-exit | AI 7 | Popup thua chỉ đóng bằng một trong hai nút
Chạm ra ngoài popup hay nút quay lại của máy không đóng popup, vì phía sau là trận đã kết thúc.
