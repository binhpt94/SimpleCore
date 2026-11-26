---
based-on: 53af9956f851
by: revive-offer=gpt@r2-1#nut-ket-thuc-luot, revive-ad-result=gpt@r2-1#moc-chot-thua, stop-after-break-tick=gpt@r2-1#dung-sau-nhip-vo-thanh, freeze-while-waiting=gpt@r2-1#giu-tran-dung, revive-before-pause=gpt@r2-1#uu-tien-hoi-sinh, close-then-report=gpt@r2-1#dong-popup-truoc-khi-bao, lock-while-ad=gpt@r2-1#khoa-nut-khi-cho-quang-cao, score-continues=gpt@r2-1#diem-tiep-noi, same-tick-clear=gpt@r2-1#don-dot-cung-nhip, record-on-final-lose=gpt@r2-1#xet-ki-luc-khi-thua
---
# Popup hồi sinh

Popup hiện khi thành bị phá lần đầu trong lượt: đề nghị người chơi xem quảng cáo để hồi sinh thành và chơi tiếp, hoặc chấp nhận thua.

## Ý cốt lõi

### revive-offer | AI 7 | Popup có hai nút: xem quảng cáo để hồi sinh thành, và "Kết thúc lượt"
Người chơi biết ngay nút từ chối dẫn tới kết quả ở `feature-popup-lose`.

### revive-shows-gain | AI 7 | Popup nói rõ hồi sinh được gì: thành có lại một phần máu, trận chạy tiếp từ chỗ đang dừng
Người chơi biết đổi lấy gì trước khi xem quảng cáo.

### revive-ad-result | AI 7 | Sau lần thành vỡ đầu, lượt chỉ chốt thua khi người chơi bỏ qua hoặc quảng cáo báo không xem được
Quảng cáo do `feature-ads`; popup chỉ báo lựa chọn và kết quả cho `feature-run`.

### revive-once-shown | AI 7 | Mỗi lượt popup chỉ hiện một lần; thành bị phá lần hai thì vào thẳng popup thua

### stop-after-break-tick | AI 6 | Trận dừng ngay sau nhịp giao chiến làm thành bị phá, trước khi popup hồi sinh mở

### freeze-while-waiting | AI 7 | Trận đứng yên suốt lúc chờ người chơi chọn và chờ kết quả quảng cáo
Vị trí, máu, tiến độ đợt, hồi chiêu và đồng hồ hồi sinh hero giữ nguyên.

### revive-before-pause | AI 6 | Thành vỡ lần đầu đúng nhịp có yêu cầu tạm dừng thì popup hồi sinh được ưu tiên
Lượt chơi xử lí cơ hội hồi sinh trước yêu cầu mở popup tạm dừng.

### close-then-report | AI 6 | Popup hồi sinh đóng xong rồi mới gửi kết quả cuối cho `feature-run`

### lock-while-ad | AI 7 | Cả hai nút bị khoá từ lúc gửi yêu cầu xem quảng cáo tới khi có kết quả
Tránh chốt thua vì bấm "Kết thúc lượt" khi quảng cáo đang trả kết quả.

### score-continues | AI 7 | Sau khi hồi sinh, các đợt dọn sạch tiếp tục cộng vào điểm của cùng lượt

### same-tick-clear | AI 6 | Đợt đủ điều kiện dọn sạch cùng nhịp thành vỡ chỉ được ghi nhận nếu thành hồi sinh thành công
Lượt chốt thua thì điểm dừng ở đợt đã ghi nhận trước nhịp thành vỡ, theo luật thua trước.

### record-on-final-lose | AI 7 | Lượt có đề nghị hồi sinh thì kỉ lục chỉ được xét khi lượt đã chốt thua
