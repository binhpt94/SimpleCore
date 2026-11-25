## Phần tử
- pause | nút | chính | tạm dừng trận, biểu tượng hai vạch đứng, không có chữ | trạng thái: bật (trận đang đánh), vô tác dụng (tạm dừng hoặc lượt kết thúc)

## Tương tác
- pause chạm khi bật: báo "đã bấm tạm dừng" cho `feature-run/run-pause` đúng một lần.
- pause chạm khi vô tác dụng: không báo gì.
- pause giữ hoặc kéo: không có thao tác riêng.

## Flow
- HUD trận → chạm pause → `feature-run/run-pause` dừng trận → popup tạm dừng (`feature-popup-pause`) chồng lên, che nút.
- Đóng popup tạm dừng bằng nút của popup → trận đánh tiếp, nút bật lại.
