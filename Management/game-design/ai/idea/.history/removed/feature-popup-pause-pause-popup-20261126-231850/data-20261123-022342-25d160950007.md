---
based-on: 27ce83b88e6d
---
# Nội dung popup tạm dừng

Popup tạm dừng có tiêu đề và hai nút: tiếp tục và bỏ lượt. Popup mở suốt lúc trận đứng yên; luật đứng yên nằm ở `feature-run/run-pause`.

## Ý cốt lõi

### popup-open-close | AI 7 | Popup mở đúng lúc trận vào tạm dừng và đóng đúng lúc trận chạy lại
Mở khi bấm nút tạm dừng (`feature-hud/hud-pause-button`) hoặc khi rời khỏi game giữa trận. Popup không tự làm trận đứng yên; nó là mặt hiển thị của trạng thái tạm dừng.

### popup-two-buttons | AI 7 | Popup có hai nút: tiếp tục và bỏ lượt
Đã loại: cài đặt âm thanh, chơi lại từ đầu (vòng 1 không nhắc; chơi lại đi qua bỏ lượt rồi màn bắt đầu).

### popup-resume-main | AI 7 | Tiếp tục là nút chính, đóng popup và trận chạy tiếp từ chỗ đã dừng
Bị ngắt rồi quay lại là việc hay gặp nhất.

### popup-quit-confirm | AI 6 | Bỏ lượt phải bấm xác nhận một lần nữa
Bỏ lượt mất cả lượt đang chơi; một lần xác nhận tránh bấm nhầm. Xác nhận ngay trong popup, không mở popup thứ hai. Đã loại: bỏ lượt ngay khi chạm.

### popup-quit-result | AI 7 | Bỏ lượt thì lượt kết thúc, tính điểm như thua, rồi về màn bắt đầu
Đợt dở không tính (`feature-run/run-score`). Popup chỉ báo lựa chọn cho `feature-run/run-pause`.
