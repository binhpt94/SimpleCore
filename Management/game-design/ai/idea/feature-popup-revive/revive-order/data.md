---
based-on: 53c6e52aecb5
refs: feature-popup-revive/revive-popup@9564590ae47a
---
# Thứ tự từ lúc thành bị phá tới lúc chơi tiếp hoặc thua

Thành bị phá lần đầu trong lượt thì trận dừng, popup hồi sinh mở, người chơi chọn, popup đóng, rồi lượt chơi tiếp hoặc chốt thua. Node này chốt thứ tự đó và cách nó gặp tạm dừng, dọn đợt, điểm.

## Ý cốt lõi

### order-steps | AI 7 | Thứ tự: thành bị phá, trận dừng, popup hồi sinh mở, người chơi chọn, popup đóng, lượt chơi xử lí lựa chọn
Hồi sinh: thành có lại máu rồi trận chạy tiếp. Không hồi sinh hay quảng cáo lỗi: chốt điểm, popup thua hiện.

### frozen-throughout | AI 7 | Từ lúc trận dừng tới lúc chơi tiếp, mọi thứ của trận đứng yên như lúc tạm dừng
Quái, hero, đợt, hồi chiêu, đồng hồ hồi sinh hero giữ nguyên; không mua, không bấm kĩ năng.

### before-pause | AI 6 | Thành bị phá lần đầu cùng lúc có yêu cầu tạm dừng thì popup hồi sinh mở, yêu cầu tạm dừng bị bỏ
Lúc popup hồi sinh mở, rời game không mở popup tạm dừng.

### once-per-run | AI 7 | Mỗi lượt chỉ một lần đề nghị; thành bị phá lần hai vào thẳng popup thua
Chỉ có lần hai khi lần đầu đã hồi sinh; chọn "Kết thúc lượt" hay quảng cáo lỗi thì lượt đã kết thúc.

### same-tick-clear | AI 6 | Đợt dọn sạch cùng lúc thành bị phá được giữ chờ: hồi sinh thành công thì tính, không thì bỏ
Khớp luật "thua trước": lượt chốt thua thì điểm dừng ở các đợt đã dọn trước lúc thành bị phá.

### score-carries | AI 7 | Hồi sinh không đặt lại điểm; kỉ lục chỉ xét khi lượt chốt thua
Theo nhánh điểm và kỉ lục của `feature-run`.
